# Rockruff sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/rockruff.py -- <the export's Rockruff.glb> <out.glb>
# From the references: a tan puppy with a big round head; big round blue eyes with a black pupil and a white glint;
# a round dark brown muzzle low in the middle of the face, a small pink nose on its top and a frowning mouth; ears
# up and folded forward, their tips dark brown, pink inside; pointed tufts of fur out from the cheeks; a white spiky
# ruff round the neck set with grey-brown rocks; short legs on dark brown paws with a jagged top edge; a pale belly;
# a big fluffy off-white tail up behind.
# Written to be grown from (lycanroc.py keeps this frame: the eye, the spikes, the rocks, the ears, the paws, the tail).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, hull, srgb, inside, tri, unit, eye_look, lumpy, args

TAN, DARK, WHITE, ROCK = "#D8B49E", "#6A4234", "#F2EEEA", "#6E625A"
TAILC, BELLY, PINK, NOSE = "#E0DBD6", "#EEDFD2", "#F0A8B4", "#F2A0AE"
BLUE, PUPIL, INK, MOUTH = "#48A2E6", "#141418", "#2C2A3A", "#3A2018"


# --- the line's own parts, shared with Lycanroc -----------------------------------------------------------------------
def spike(base, d, length, w, t, side=None):
    """A solid tapering point (a tuft of fur, a claw, a spike of the ruff): a flat-faced hull."""
    base = np.array(base, float)
    d = unit(*d)
    if side is None:
        side = np.cross(d, (0, 0, 1.0))
        if np.linalg.norm(side) < 1e-3:
            side = np.array([1.0, 0, 0])
    side = np.array(side, float); side = side - d * (side @ d); side /= np.linalg.norm(side)
    face = np.cross(d, side)
    return hull([base + side * w, base - side * w, base + face * t, base - face * t,
                 base + d * length * 0.45 + side * w * 0.6, base + d * length * 0.45 - side * w * 0.6,
                 base + d * length])


def rock(base, d, length, w, seed=0):
    """A rock spike: a rough flat-faced point, broad at its root (a four-sided pyramid with a jagged waist)."""
    base = np.array(base, float)
    d = unit(*d)
    a = np.cross(d, (0, 0, 1.0))
    if np.linalg.norm(a) < 1e-3:
        a = np.array([1.0, 0, 0])
    a /= np.linalg.norm(a); b = np.cross(d, a)
    rs = np.random.RandomState(seed)
    pts = []
    for i in range(5):
        th = 2 * math.pi * i / 5 + rs.rand() * 0.4
        pts.append(base + w * (math.cos(th) * a + math.sin(th) * b) * (0.85 + 0.3 * rs.rand()))
        pts.append(base + d * length * 0.4 + 0.7 * w * (math.cos(th + 0.6) * a + math.sin(th + 0.6) * b))
    pts.append(base + d * length)
    return hull(pts)


def blue_eye(blue=BLUE, pupil=PUPIL, ink=INK, rim=0.18, pr=0.42, glint=(0.12, 0.3, 0.26)):
    """A big round eye: a dark rim, a blue iris, a black pupil, one white glint up and out."""
    B, P, K = srgb(blue), srgb(pupil), srgb(ink)
    def look(u, v):
        c = np.tile(B, (len(u), 1))
        c[np.hypot(u, v) > 1 - rim] = K
        c[np.hypot((u + 0.04) / 0.9, v) < pr] = P
        c[np.hypot(u - glint[0], v - glint[1]) < glint[2]] = (1, 1, 1)
        return c
    return look


def lay_eyes(k, state, E, n, half, look, lid, ink, outline=None):
    """Both eyes by hand, u outward on each."""
    f = eye_look(state, look, srgb(lid), srgb(ink), line=0.16)
    from sculptkit import polygon
    for side in (1, -1):
        out_ = None
        if outline is not None and state in ("Open", "Fierce"):
            out_ = polygon([(side * a, b) for a, b in outline])
        col = f if side == 1 else (lambda a, b, g=f: g(-a, b))
        k.decal(np.array(E) * (side, 1, 1), np.array(n) * (side, 1, 1), half, col, eps=0.002, outline=out_,
                rings=20, segs=56)


def socks(k, legs, top, amp, col, parts, period=0.012):
    """Dark paws with a jagged top edge: below z = top + a zigzag round each leg (legs: (x, y) on +X)."""
    def mask(P):
        m = np.zeros(len(P))
        for x, y in legs:
            ang = np.arctan2(np.abs(P[:, 0]) - x, P[:, 1] - y)
            edge = top + amp * tri(ang / (2 * math.pi) * 5)
            near = np.hypot(np.abs(P[:, 0]) - x, P[:, 1] - y) < 0.05
            m = np.maximum(m, inside(P[:, 2] - edge, 0.0015) * near)
        return m
    k.paint(mask, col, on=parts)


def ruff(k, c, rx, rz, n, length, w, col, bone, tilt=0.0, down=0.35, name="ruff"):
    """A ring of white spikes round a neck at c (an ellipse rx by rz in plan), pointing out and a little down."""
    c = np.array(c, float)
    for i in range(n):
        th = 2 * math.pi * (i + 0.5) / n
        sx, sy = math.sin(th), -math.cos(th)
        base = c + np.array([rx * sx, rz * sy, tilt * sy])
        L = length * (1.0 if i % 2 == 0 else 0.75)
        k.add(f"{name}{i}", spike(base - np.array([sx, sy, 0]) * w * 0.8, (sx, sy, -down), L, w, w * 0.55), col, bone,
              fuse=False, surface="fur")


# --- Rockruff ---------------------------------------------------------------------------------------------------------
# The frame (joints: body (0, 0, 0.11), head (0, -0.05, 0.16), ears (+-0.03, -0.06, 0.23), legs (+-0.035, -0.05, 0.09),
# hinds (+-0.035, 0.05, 0.09), tail (0, 0.06, 0.13))
HC = np.array([0.0, -0.07, 0.2])          # the head's middle: big, a tenth larger than measured
HR = np.array([0.066, 0.06, 0.062])
MC = HC + np.array([0, -0.05, -0.017])    # the round dark muzzle
MR = np.array([0.031, 0.026, 0.031])
LX, FY, HY = 0.034, -0.048, 0.05


def rockruff(src, out):
    k = Kit(src, coat="fur")

    # A short trunk low on the legs, a short neck up into the head
    k.add("chest", ell((0, -0.03, 0.112), (0.046, 0.046, 0.048)), TAN, "body")
    k.add("rump", ell((0, 0.04, 0.112), (0.044, 0.046, 0.046)), TAN, "body")
    k.add("neck", tube([(0, -0.04, 0.12), (0, -0.058, 0.16)], [0.036, 0.036]), TAN, "head")
    k.paint(lambda P: inside(P[:, 2] - 0.088, 0.003) * (P[:, 1] > -0.07) * (P[:, 1] < 0.07), BELLY,
            on=["chest", "rump"])

    # Short legs: a shoulder or thigh over each, dark brown paws with a jagged top
    for nm, y, bone in (("leg", FY, "leg{s}"), ("hind", HY, "hind{s}")):
        k.add(nm + "top", ell((LX, y, 0.092), (0.026, 0.03, 0.034)), TAN, bone, mirror=True)
        k.add(nm, tube([(LX, y, 0.09), (LX + 0.001, y - 0.003, 0.05), (LX + 0.002, y - 0.005, 0.02)],
                       [0.019, 0.017, 0.017]), TAN, bone, mirror=True)
        k.add(nm + "paw", ell((LX + 0.002, y - 0.012, 0.012), (0.02, 0.026, 0.013)), DARK, bone, mirror=True)
    socks(k, [(LX, FY), (LX, HY)], 0.034, 0.005, DARK, ["leg", "hind"])

    # The big round head, the dark muzzle low in its middle, pointed tufts out from the cheeks
    k.add("head", ell(HC, HR), TAN, "head")
    k.add("muzzle", ell(MC, MR), DARK, "head")
    for s in (1, -1):
        for j, (dz, d, ln) in enumerate(((-0.012, (1, 0.1, 0.25), 0.04), (-0.032, (1, 0.15, -0.3), 0.034))):
            base = HC + np.array([s * HR[0] * 0.88, -0.008, dz])
            k.add(f"cheek{j}{'R' if s > 0 else 'L'}", spike(base, (s * d[0], d[1], d[2]), ln, 0.014, 0.009),
                  TAN, "head", fuse=False, surface="fur")

    # Ears up from the head's top corners, their tips folded forward and dark, pink inside
    for s, nm in ((1, "R"), (-1, "L")):
        b0 = np.array([0.036 * s, -0.062, 0.245])
        top = np.array([0.044 * s, -0.068, 0.284])
        k.add("ear" + nm, blade([b0, (b0 + top) / 2, top], [0.026, 0.022, 0.014], (0, 1, 0), 0.5), TAN, "ear" + nm)
        flap = [top + (-0.018 * s, 0.006, 0.006), top + (0.018 * s, 0.006, 0.006), top + (-0.018 * s, -0.004, 0.012),
                top + (0.018 * s, -0.004, 0.012), top + (0.0, -0.036, -0.016), top + (0.008 * s, -0.03, -0.018)]
        k.add("flap" + nm, hull(flap), DARK, "ear" + nm, fuse=False, surface="fur")
    k.paint(lambda P: inside(np.hypot(np.abs(P[:, 0]) - 0.04, P[:, 2] - 0.262) - 0.012, 0.0015)
            * (P[:, 1] < -0.068), PINK, on=["ear"])

    # The white spiky ruff round the neck, its rocks
    RC = np.array([0, -0.062, 0.145])
    k.add("collar", ell(RC, (0.05, 0.044, 0.024), deform=lumpy(6, 2, 0.08)), WHITE, "body")
    ruff(k, RC, 0.046, 0.04, 12, 0.042, 0.014, WHITE, "body", down=0.3)
    for i, (dx, dy, d) in enumerate(((0.0, -0.044, (0, -1, 0.2)), (0.032, -0.03, (0.75, -0.6, 0.25)),
                                     (-0.032, -0.03, (-0.75, -0.6, 0.25)))):
        k.add(f"rock{i}", rock(RC + (dx, dy, 0.004), d, 0.04, 0.017, seed=i), ROCK, "body", fuse=False,
              surface="shell")

    # The big fluffy off-white tail up behind, swung out to +X
    TB = np.array([0.0, 0.08, 0.13])
    k.add("tailroot", tube([TB, TB + (0.01, 0.03, 0.03)], [0.022, 0.03]), TAILC, "tail")
    k.add("tail", ell(TB + (0.03, 0.045, 0.075), (0.041, 0.046, 0.068), rot=(-15, 0, -30), deform=lumpy(5, 3, 0.06)),
          TAILC, "tail")
    k.add("tailtip", ell(TB + (0.042, 0.05, 0.142), (0.03, 0.032, 0.04), rot=(-25, 0, -30)), TAILC, "tail")
    for i, (dz, ang) in enumerate(((0.04, 0.2), (0.08, 0.5), (0.115, 0.8), (0.15, 1.15))):
        base = TB + np.array([0.04, 0.085 - 0.012 * i, dz])
        k.add(f"tailfur{i}", spike(base, (0.45, math.cos(ang), math.sin(ang)), 0.036, 0.016, 0.01), TAILC, "tail",
              fuse=False, surface="fur")

    # The face: a small pink nose on the muzzle's top, a frowning mouth under it
    k.add("nose", ell(MC + (0, -MR[1] * 0.72, MR[2] * 0.62), (0.011, 0.008, 0.007), rot=(20, 0, 0)), NOSE, "head",
          fuse=False, surface="skin")
    CD, CM = srgb(DARK), srgb(MOUTH)
    def frown(u, v):
        c = np.tile(CD, (len(u), 1))
        c[(np.abs(v - (0.35 - 0.75 * u * u)) < 0.2) & (np.abs(u) < 0.9)] = CM
        c[(np.abs(u) < 0.09) & (v > 0.3)] = CM
        return c
    k.decal(MC + (0, -MR[1] * 0.97, -0.008), unit(0, -1, -0.1), (0.017, 0.011), frown, eps=0.002, rings=12, segs=56)

    k.build(voxel=0.0034, smooth=4, band=0.003, wband=0.03)

    de = unit(0.6, -0.77, 0.16)
    E = HC + de / np.linalg.norm(de / HR) + np.array([0, -0.001, 0])
    look = blue_eye()
    k.eye_states(lambda s: lay_eyes(k, s, E, de, (0.02, 0.022), look, TAN, INK))
    k.export(out)


if __name__ == "__main__":
    rockruff(*args())
