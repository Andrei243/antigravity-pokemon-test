# Lillipup sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/lillipup.py -- <the export's Lillipup.glb> <out.glb>
# From the references: a small tan puppy behind a huge spiky cream starburst of fur that is its whole face (as tall as
# the ears' roots, as wide as two thirds of the height); big round black eyes with a white rim and a glint, low on the
# face; a small red nose and a tiny round mouth; big pointed tan ears up and out, a darker pink-brown inside; a short
# trunk low on four stubby legs with round paws; a jagged navy patch on its back behind the face; a short tufted tail
# up and back.
# Written to be grown from (herdier.py and stoutland.py keep this frame: the fur tuft, the ears and their lining, the
# legs and paws, the nose, the eyes).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, hull, srgb, inside, tri, unit, eye_look, polygon, args

TAN, CREAM, NAVY = "#D8915C", "#F2D59C", "#34407A"
LINING, NOSE, MOUTH = "#B86A5A", "#E0404A", "#4A2A22"
PUPIL, WHITE, INK = "#18161A", "#F8F6F0", "#3A2418"
CLAW = "#F2EEE6"


# --- shared by the line -----------------------------------------------------------------------------------------------
def tuft(k, name, base, d, length, r, col, bone, flat=(0, 1, 0), f=0.5, fuse=True):
    """A pointed tuft of fur (fused): a blade from base along d, r thick at its root, flattened across `flat`."""
    base = np.array(base, float)
    d = unit(*d)
    fl = np.array(flat, float); fl = fl - d * (fl @ d)
    if np.linalg.norm(fl) < 1e-3:
        fl = np.cross(d, (1.0, 0, 0))
    k.add(name, blade([base, base + d * length * 0.5, base + d * length], [r, r * 0.62, 0.004], fl, f), col, bone,
          fuse=fuse)


def spike(base, d, length, w, t, side=None):
    """A solid tapering point (a claw, a lock of fur that must keep its edge): a flat-faced hull."""
    base = np.array(base, float)
    d = unit(*d)
    if side is None:
        side = np.cross(d, (0, 0, 1.0))
        if np.linalg.norm(side) < 1e-3:
            side = np.array([1.0, 0, 0])
    side = np.array(side, float); side = side - d * (side @ d); side /= np.linalg.norm(side)
    face = np.cross(d, side)
    pts = [base + side * w, base - side * w, base + face * t, base - face * t,
           base + d * length * 0.45 + side * w * 0.55, base + d * length * 0.45 - side * w * 0.55, base + d * length]
    return hull(pts)


def ear(k, base, tip, r, col, flat=(0, 1, 0), f=0.42):
    """A big pointed ear, flat front to back (mirrored)."""
    base, tip = np.array(base, float), np.array(tip, float)
    mid = base + (tip - base) * 0.5 + np.array((0.004, 0, 0))
    k.add("ear", blade([base, mid, tip], [r, r * 0.68, 0.005], flat, f), col, "ear{s}", mirror=True)


def ear_lining(k, base, tip, width, col, front=(0, -1, 0)):
    """The inside of an ear: a narrower point painted on its front face, crisp (no on)."""
    base, tip = np.array(base, float), np.array(tip, float)
    d = tip - base
    L = np.linalg.norm(d); d /= L
    fr = np.array(front, float); fr /= np.linalg.norm(fr)
    def mask(P, sx):
        b = base * np.array([sx, 1, 1]); dd = d * np.array([sx, 1, 1]); ff = fr * np.array([sx, 1, 1])
        q = P - b
        t = q @ dd
        across = q - np.outer(t, dd)
        depth = across @ ff
        side = across - np.outer(depth, ff)
        rad = np.linalg.norm(side, axis=1)
        w = width * (1 - np.clip(t / (L * 0.92), 0, 1))
        return (inside(rad - w, 0.0025) * (t > L * 0.1) * (depth > 0.002) * (depth < 0.03)
                * (np.sign(P[:, 0]) == sx))
    k.paint(lambda P: np.clip(mask(P, 1) + mask(P, -1), 0, 1), col)


def leg(k, x, y, top, r, paw, col, bone, name, toes=True, shoulder=None):
    """A short thick leg: a haunch fused over its top, a stub of shin, a round paw with three toe bumps."""
    if shoulder is not None:
        k.add(name + "top", ell((x - 0.004, y + 0.004, top), shoulder), col, bone, mirror=True)
    pr = np.array(paw, float)
    k.add(name, tube([(x, y, top), (x + 0.002, y - 0.002, (top + pr[2]) / 2), (x + 0.003, y - 0.004, pr[2] + 0.01)],
                     [r * 1.1, r, r * 0.95]), col, bone, mirror=True)
    pc = np.array((x + 0.003, y - 0.012, pr[2]))
    k.add(name + "paw", ell(pc, pr), col, bone, mirror=True)
    if toes:
        for j, dx in enumerate((-0.55, 0.0, 0.55)):
            k.add(f"{name}toe{j}", ell(pc + (dx * pr[0] * 0.8, -pr[1] * 0.72, -pr[2] * 0.15),
                                       (pr[0] * 0.38, pr[1] * 0.38, pr[2] * 0.82)), col, bone, mirror=True)
    return pc


def toe_lines(k, paws, pr, col, depth=0.5):
    """Two short dark grooves on the front of each paw, between the toe bumps (paws: centres on +X)."""
    def mask(P):
        m = np.zeros(len(P))
        q = P.copy(); q[:, 0] = np.abs(q[:, 0])
        for pc in paws:
            for dx in (-0.28, 0.28):
                gx = pc[0] + dx * pr[0] * 0.8
                m = np.maximum(m, inside(np.abs(q[:, 0] - gx) - 0.0022, 0.0015) * (q[:, 1] < pc[1] - pr[1] * 0.55)
                               * (q[:, 2] > 0.003) * (q[:, 2] < pc[2] + pr[2] * depth)
                               * (np.abs(q[:, 1] - pc[1]) < pr[1] * 1.3))
        return m
    k.paint(mask, col)


def nose(k, at, r, col, bone="head"):
    k.add("nose", ell(at, r, rot=(12, 0, 0)), col, bone, fuse=False, surface="shell")


def round_eye(white, pupil, rim=0.2, glint=(0.22, 0.34, 0.3)):
    """A big round black eye: a white rim round it, a white glint up and out."""
    W, P = srgb(white), srgb(pupil)
    def look(u, v):
        c = np.tile(W, (len(u), 1))
        r = np.sqrt(u * u + v * v)
        c[r < 1 - rim] = P
        c[((u - glint[0]) / glint[2]) ** 2 + ((v - glint[1]) / glint[2]) ** 2 < 1] = W
        return c
    return look


def lay_eyes(k, state, E, n, half, look, lid, line_col, outline=None, line=0.14):
    """Both eyes laid by hand, a side each, u outward on both."""
    f = eye_look(state, look, srgb(lid), srgb(line_col), line=line)
    for side in (1, -1):
        nn = np.array(n) * np.array([side, 1, 1.0])
        out_ = polygon([(side * a, b) for a, b in outline]) if outline is not None and state in ("Open", "Fierce") else None
        col = f if side == 1 else (lambda a, b, g=f: g(-a, b))
        k.decal(np.array(E) * np.array([side, 1, 1.0]), nn, half, col, eps=0.003, outline=out_, rings=20, segs=56)


def on_ell(c, r, x, z, front=True):
    """The point of an ellipsoid's front (or back) at a given x and z: where a patch's centre goes."""
    c, r = np.array(c, float), np.array(r, float)
    s = 1 - ((x - c[0]) / r[0]) ** 2 - ((z - c[2]) / r[2]) ** 2
    y = c[1] + (-1 if front else 1) * r[1] * math.sqrt(max(s, 0.0))
    return np.array((x, y, z))


# --- Lillipup ---------------------------------------------------------------------------------------------------------
# The frame (joints: body (0, 0, 0.14), head (0, -0.07, 0.18), ears (+-0.05, -0.08, 0.26), legs (+-0.05, -0.06, 0.09),
# hinds (+-0.05, 0.06, 0.09), tail (0, 0.1, 0.15))
FC = np.array([0.0, -0.1, 0.212])       # the face's middle: the cream disc of fur
FR = np.array([0.118, 0.072, 0.124])
HC = np.array([0.0, -0.065, 0.232])       # the tan head behind it
HR = np.array([0.098, 0.085, 0.098])
LX, FY, HY = 0.05, -0.058, 0.066
BZ = 0.11


def mane(k, col=CREAM):
    """The face: a flat cream disc and a starburst of pointed tufts round its rim, leaning back a little."""
    k.add("face", ell(FC, FR), col, "head")
    k.add("muzzle", ell(FC + (0, -0.052, -0.052), (0.044, 0.03, 0.034)), col, "head")
    n = 12
    for i in range(n):
        th = math.pi / 2 + 2 * math.pi * i / n          # from straight up, round
        cx, cz = math.cos(th), math.sin(th)
        up = max(cz, 0)
        length = (0.058 + 0.016 * up - 0.01 * abs(cx)) * (1.15 if i % 2 == 0 else 0.9)
        base = FC + np.array((0.86 * FR[0] * cx, 0.012, 0.86 * FR[2] * cz))
        tuft(k, f"mane{i}", base, (cx, 0.3, cz), length, 0.042, col, "head")
    # A second ring, shorter, between the first's points and a little forward: the fur's fullness
    for i in range(n):
        th = math.pi / 2 + 2 * math.pi * (i + 0.5) / n
        cx, cz = math.cos(th), math.sin(th)
        base = FC + np.array((0.72 * FR[0] * cx, -0.004, 0.72 * FR[2] * cz))
        tuft(k, f"maneb{i}", base, (cx, 0.1, cz), 0.06, 0.036, col, "head")


def lillipup(src, out):
    k = Kit(src, coat="fur")

    # A small trunk low behind the face, its belly near the ground, the haunches fused into it
    k.add("chest", ell((0, -0.03, BZ + 0.01), (0.068, 0.07, 0.072)), TAN, "body")
    k.add("middle", ell((0, 0.03, BZ), (0.066, 0.08, 0.068)), TAN, "body")
    k.add("rump", ell((0, 0.075, BZ + 0.004), (0.066, 0.056, 0.068)), TAN, "body")

    # Stubby legs with round paws
    PR = (0.03, 0.034, 0.02)
    pf = leg(k, LX, FY, 0.085, 0.027, PR, TAN, "leg{s}", "leg", shoulder=(0.036, 0.042, 0.05))
    ph = leg(k, LX, HY, 0.085, 0.027, PR, TAN, "hind{s}", "hind", shoulder=(0.04, 0.048, 0.055))
    toe_lines(k, [pf, ph], PR, "#9A5A3A")

    # The tan head behind the face, the face's disc of cream fur and its starburst
    k.add("head", ell(HC, HR), TAN, "head")
    mane(k)

    # Big pointed ears up and out from behind the face, a pink-brown lining
    EB, ET = np.array([0.062, -0.052, 0.285]), np.array([0.142, -0.036, 0.405])
    ear(k, EB, ET, 0.046, TAN)
    ear_lining(k, EB + (0.004, 0, 0.012), ET, 0.03, LINING, front=(0, -1, 0))

    # The jagged navy patch over the back behind the face
    def patch(P):
        y = P[:, 1]
        line = 0.15 + 0.012 * tri(np.abs(P[:, 0]) * 22) + 0.006 * tri(y * 30)
        front = -0.005 + 0.008 * tri(np.abs(P[:, 0]) * 18)
        back = 0.045 + 0.01 * tri(P[:, 0] * 20)
        return inside(line - P[:, 2], 0.003) * inside(front - y, 0.003) * inside(y - back, 0.003)
    k.paint(patch, NAVY, on=["chest", "middle", "rump"])

    # A short tufted tail up and back, a little out to +X, two points at its end
    T = [(0, 0.11, 0.14), (0.008, 0.15, 0.17), (0.018, 0.175, 0.215)]
    k.add("tail", tube(T, [0.022, 0.026, 0.022]), TAN, "tail")
    tip = np.array(T[-1])
    tuft(k, "tailtip0", tip, (0.2, 0.35, 1), 0.05, 0.022, TAN, "tail", flat=(1, 0, 0))
    tuft(k, "tailtip1", tip + (0, 0.004, -0.01), (0.25, 1, 0.35), 0.042, 0.02, TAN, "tail", flat=(1, 0, 0))

    # The face: a small red nose, a tiny round mouth under it
    nose(k, FC + (0, -0.09, -0.054), (0.013, 0.009, 0.009), NOSE)
    CC, CM = srgb(CREAM), srgb(MOUTH)
    def mouth(u, v):
        c = np.tile(CM, (len(u), 1))
        c[u * u + v * v > 1.0] = CC
        return c
    k.decal(on_ell(FC + (0, -0.052, -0.052), (0.044, 0.03, 0.034), 0, FC[2] - 0.078), unit(0, -1, -0.3),
            (0.009, 0.01), mouth, eps=0.003, rings=8, segs=32)

    k.build(voxel=0.0045, smooth=3, band=0.005)

    # The eyes: big, round and black with a white rim and a glint, low on the face and set wide
    ex, ez = 0.05, FC[2] - 0.026
    E = on_ell(FC, FR, ex, ez) + (0, -0.001, 0)
    n = unit(0.22, -1, 0.05)
    look = round_eye(WHITE, PUPIL)
    k.eye_states(lambda s: lay_eyes(k, s, E, n, (0.028, 0.036), look, CREAM, INK))
    k.export(out)


if __name__ == "__main__":
    lillipup(*args())
