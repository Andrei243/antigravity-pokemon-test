# Oshawott sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/oshawott.py -- <the export's Oshawott.glb> <out.glb>
#
# Written to be grown from (Dewott, Samurott: sibling("oshawott")): the palette, the eye, the nose, the foot, the paddle
# tail, the ear and the ribbed scalchop are functions here. From the references: a small otter standing up, its round
# white head as tall as half of it and half again as wide as the body, a little wider at the cheeks; small rounded
# navy ears on the head's upper corners, tilted out; large black oval eyes, wide apart, each with a white glint; a big
# brown nose between them, a little lower; a small frown under it and three freckles on each cheek; a ruff of pale
# cyan lobes round the neck; a cyan pear of a body; the cream scalchop, ribbed, its hinge down, held on the belly;
# short white flipper arms held forward; short cyan legs and navy feet with three toes; a broad flat navy tail lying
# behind and out to one side.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, inside, smoothstep, unit, boxy, eye_look, args

BODY, RUFF, WHITE, NAVY, BROWN = "#6CCAD4", "#A8E6E8", "#F6F7F9", "#3A5E9E", "#9A5A2E"
SHELL, SHELLRIB, INK = "#F2DD95", "#D9BC6E", "#191B24"


def at_surface(C, R, d):
    """The point of an ellipsoid (centre C, radii R) in a direction d from its centre."""
    d = np.array(d, float)
    return np.array(C, float) + d / np.linalg.norm(d / np.array(R, float))


def eye(u, v):
    """A tall black oval eye with a white glint up and out (u outward, v up)."""
    c = np.tile(srgb(INK), (len(u), 1))
    c[((u - 0.22) / 0.3) ** 2 + ((v - 0.38) / 0.26) ** 2 < 1] = (1.0, 1.0, 1.0)
    return c


def eyes(k, spots, half, state, lid, open_look=None, line=0.16):
    """The two eyes as patches, the left one's colour function mirrored so u is outward on both."""
    look = eye_look(state, open_look or eye, srgb(lid), srgb(INK), line=line)
    for at, d, sx in spots:
        col = look if sx > 0 else (lambda u, v, look=look: look(-u, v))
        k.decal(at, d, half, col, eps=0.003, rings=20, segs=56)


def nose(k, at, d, half, color, bone, tilt=0.0):
    """A round nose standing proud of the face: a loose flattened ellipsoid facing along d."""
    d = unit(*d)
    pitch = math.degrees(math.atan2(d[2], -d[1]))
    k.add("nose", ell(np.array(at, float) + d * half[1] * 0.45, half, rot=(pitch + tilt, 0, 0)), color, bone,
          fuse=False, surface="skin")


def toes(n=3, amp=0.16):
    """A deform that lobes a foot's front into n round toes."""
    def f(V):
        ph = np.arctan2(V[:, 0], -V[:, 1])
        front = np.clip(-V[:, 1], 0, 1)
        s = 1 + amp * np.cos(n * ph) * front
        V = V.copy(); V[:, 0] *= s; V[:, 1] *= s
        return V
    return f


def foot(k, c, half, color, bone, yaw=0.0):
    """A foot flat on the ground, its front lobed into three toes (mirrored)."""
    k.add("foot", ell(c, half, rot=(0, 0, yaw), deform=toes()), color, bone, mirror=True)


def frame(d, roll=0.0):
    """A right-handed frame (side, face normal, d) round a direction, rolled about d by degrees."""
    d = unit(*d)
    s = np.cross(d, (0, 0, 1.0)); s /= np.linalg.norm(s)
    n = np.cross(s, d)
    r = math.radians(roll)
    s, n = s * math.cos(r) + n * math.sin(r), n * math.cos(r) - s * math.sin(r)
    return s, n, d


def paddle(k, name, root, d, half, color, bone, roll=0.0):
    """A broad flat tail (a paddle): a flattened ellipsoid from root along d, its face turned by roll."""
    s, n, d = frame(d, roll)
    def narrow(V):
        V = V.copy(); V[:, 0] *= 1 - 0.3 * np.clip(-V[:, 1], 0, 1) ** 2   # narrower at the root
        return V
    V, F = ell((0, 0, 0), half, deform=narrow)
    # local x across (side), y along (d), z thin (n)
    V = V @ np.stack([s, d, n]) + np.array(root, float) + d * half[1] * 0.85
    k.add(name, (V, F), color, bone)


def ear(k, C, R, d, half, color, bone, lean=40.0, sx=1):
    """A small rounded ear on the head's surface in direction d, its top leaning out by lean degrees."""
    c = at_surface(C, R, d) + unit(*d) * half[2] * 0.35
    k.add(f"ear{'RL'[sx < 0]}", ell(c, half, rot=(0, lean * sx, 0), deform=boxy(3)), color, bone)


def scalchop(k, name, c, n, up, size, bone, bend=0.08, thick=0.012, spread=62, ribs=6):
    """
    The scalchop: a ribbed fan of a shell, its hinge at the bottom (-up), lying on a surface of curvature radius bend
    across it. Edge-to-edge wedges of two creams, each a flat-faced hull, the darker ribs standing a hair proud; a
    small bar at the hinge. size: the fan's radius from the hinge.
    """
    n = unit(*n); up = np.array(up, float); up = up - n * (up @ n); up /= np.linalg.norm(up)
    side = np.cross(up, n)
    c = np.array(c, float)
    hinge = c - up * size * 0.5
    def P(a, b, depth):
        return hinge + side * a + up * b + n * (depth - a * a / (2 * bend))
    angs = np.linspace(-spread, spread, ribs + 1)
    for i in range(ribs):
        t0, t1 = math.radians(angs[i]), math.radians(angs[i + 1])
        tm = (t0 + t1) / 2
        rib = i % 2 == 1
        lift = 0.0015 if rib else 0.0
        pts = []
        for depth in (0.004 + lift, -thick):
            pts.append(P(0, size * 0.06, depth))
            for t, r in ((t0, 1.0), (tm, 1.07), (t1, 1.0)):
                pts.append(P(size * r * math.sin(t), size * 0.06 + size * r * math.cos(t), depth + (0.003 if r > 1 else 0)))
        k.add(f"{name}{i}", hull(pts), SHELLRIB if rib else SHELL, bone, fuse=False, surface="shell")
    bar = [P(a, b, dp) for a in (-size * 0.28, size * 0.28) for b in (-size * 0.02, size * 0.14) for dp in (0.002, -thick)]
    k.add(f"{name}hinge", hull(bar), SHELL, bone, fuse=False, surface="shell")


def oshawott(src, out):
    k = Kit(src, coat="fur")
    H = np.array([0, -0.012, 0.318])          # the head's centre
    HR = np.array([0.122, 0.106, 0.106])
    def egg(V):
        V = V.copy()
        s = 1 + 0.06 * np.clip(-V[:, 2], 0, 1) - 0.07 * np.clip(V[:, 2], 0, 1) ** 2
        V[:, 0] *= s
        return V
    k.add("head", ell(H, HR, deform=egg), WHITE, "head")

    # A pear of a body, wider below, sitting low on short legs
    def pear(V):
        V = V.copy()
        s = 1 + 0.1 * np.clip(-V[:, 2], 0, 1) - 0.14 * np.clip(V[:, 2], 0, 1) ** 2
        V[:, 0] *= s; V[:, 1] *= s
        return V
    B = np.array([0, 0.004, 0.128])
    BR = np.array([0.082, 0.074, 0.095])
    k.add("body", ell(B, BR, deform=pear), BODY, "body")
    k.add("leg", tube([(0.045, 0.0, 0.07), (0.047, -0.006, 0.03)], [0.036, 0.032]), BODY, "leg{s}", mirror=True)
    foot(k, (0.05, -0.028, 0.017), (0.033, 0.05, 0.018), NAVY, "leg{s}", yaw=-8)

    # The ruff: pale cyan lobes round the neck, lower at the front
    for i in range(9):
        a = 2 * math.pi * i / 9
        x, y = 0.078 * math.sin(a), -0.068 * math.cos(a)
        z = 0.212 - 0.012 * math.cos(a)
        k.add(f"ruff{i}", ell((x, y, z), (0.03, 0.028, 0.024), sub=3), RUFF, "body", fuse=False)

    # Short white flipper arms carried forward and down from the shoulder, the mitts before the belly beside the shell
    k.add("arm", tube([(0.065, -0.02, 0.175), (0.088, -0.06, 0.15), (0.085, -0.098, 0.125)], [0.033, 0.031, 0.027]),
          WHITE, "arm{s}", mirror=True)

    # The broad flat navy tail lying behind and out to +X, rising a little so both cameras see a face
    k.add("tailroot", tube([(0, 0.05, 0.08), (0.03, 0.11, 0.06)], [0.036, 0.03]), NAVY, "tail")
    paddle(k, "tail", (0.03, 0.11, 0.058), (0.57, 0.8, 0.22), (0.045, 0.072, 0.016), NAVY, "tail", roll=25)

    # Ears on the head's upper corners, tilted out
    for sx in (1, -1):
        ear(k, H, HR, unit(0.72 * sx, 0.06, 0.66), (0.03, 0.019, 0.03), NAVY, f"ear{'RL'[sx < 0]}", lean=45, sx=sx)

    # The big brown nose between the eyes, a little lower
    nd = unit(0, -1, -0.2)
    nose(k, at_surface(H, HR, nd), nd, (0.03, 0.014, 0.021), BROWN, "head")

    # The scalchop on the belly, hinge down
    sc = at_surface(B, BR, unit(0.08, -1, -0.15))
    scalchop(k, "shell", sc, unit(0.08, -1, -0.12), (0, 0, 1), 0.058, "body", bend=0.085)


    # The frown under the nose and three freckles on each cheek
    INKC, WH = srgb(INK), srgb(WHITE)
    def frown(u, v):
        c = np.tile(WH, (len(u), 1))
        arc = -0.55 + 0.95 * (1 - u * u)
        c[(np.abs(v - arc) < 0.36) & (np.abs(u) < 0.9)] = INKC
        return c
    md = unit(0, -0.88, -0.48)
    k.decal(at_surface(H, HR, md), md, (0.019, 0.011), frown, eps=0.003, rings=16, segs=56)
    def freckles(u, v):
        c = np.tile(WH, (len(u), 1))
        for du, dv in ((-0.5, 0.35), (0.45, 0.35), (0.0, -0.45)):
            c[(u - du) ** 2 + (v - dv) ** 2 < 0.3 ** 2] = INKC
        return c
    fd = unit(0.62, -0.68, -0.4)
    k.decal(at_surface(H, HR, fd), fd, (0.016, 0.014), freckles, eps=0.003, rings=14, segs=48, mirror=True)

    k.build(voxel=0.0042, smooth=4, band=0.004)
    E = [(at_surface(H, HR, d), d, sx) for sx in (1, -1) for d in [unit(0.5 * sx, -0.86, -0.04)]]
    k.eye_states(lambda st: eyes(k, E, (0.021, 0.031), st, WHITE))
    k.export(out)


if __name__ == "__main__":
    oshawott(*args())
