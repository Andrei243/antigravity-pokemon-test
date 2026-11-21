# Steenee sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/steenee.py -- <the export's Steenee.glb> <out.glb>
#
# Grown from Bounsweet's script (sibling("bounsweet")): the same magenta, white and lime green, the same cap and curling
# stalk, the same round-ended leaves (now long pigtails) and the same open mouth. From the references: a dancing fruit
# girl. A round white head, a little fuller at the cheeks, a lime cap on it with a short stalk curling to its left; two
# great green leaves out of the cap's sides arching out and falling to below the waist like pigtails, their hollow
# sides turned in, two yellow spots near the root of the left one; tiny magenta points of ears on the head's upper
# sides; big magenta eyes with a dark rim and white glints, an open mouth; a little magenta frill under the chin on a
# slim magenta neck; thin white arms; a big white bulb of four lobes for a skirt under a magenta top whose points come
# down onto the white; one long magenta point of legs under it, two halves pressed together.
# (The task called the body a white skirt: the pictures agree; their legs show as ONE point with a seam down it, built
# as two halves pressed together, one on each leg bone.)
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, lathe, hull, srgb, inside, unit, tri, eye_look, args, outward, sibling

b = sibling("bounsweet")
MAGENTA, MAGENTA_DARK, WHITE, GREEN = b.MAGENTA, b.MAGENTA_DARK, b.WHITE, b.GREEN
YELLOW = "#F2EE4A"
EYE_RIM, IRIS, IRIS_LIGHT, PUPIL = srgb("#3E0A22"), srgb("#C8287A"), srgb("#EE7AB4"), srgb("#5E0A30")
INK = srgb(b.INK)
WHITE_ = np.array([1.0, 1.0, 1.0])

# --- Steenee's frame (joints: body (0, 0, 0.2), arms (+-0.022, 0, 0.27), head (0, 0, 0.285), legs (+-0.025, 0,
#     0.13); 0.6 tall) ---------------------------------------------------------------------------------------------
BC = np.array([0.0, 0.0, 0.198])          # the bulb's middle
HC = np.array([0.0, -0.004, 0.408])       # the head's middle
HR = np.array([0.1, 0.09, 0.082])
CAP = np.array([0.0, 0.006, 0.462])
EYE_D = (0.43, -0.9, 0.06)
EYE_HALF = (0.027, 0.032)
LEAF_ROOT = np.array([0.066, 0.012, 0.512])
LEAF_TIP = np.array([0.215, -0.01, 0.21])
LEAF_FACE = (0.75, 0.05, 1.0)
LEAF_W, LEAF_ARCH, LEAF_THICK, LEAF_CUP = 0.062, 0.115, 0.03, 0.7


def leaf_point(base, tip, facing, t, arch, thick, x=0.0, width=0.0, cup=0.0):
    """The point on round_leaf's outer face at t along it and x across it (-1..1 of width, toward the front for x > 0),
    and the face's way there."""
    base, tip = np.array(base, float), np.array(tip, float)
    L = np.linalg.norm(tip - base); d = (tip - base) / L
    n = np.array(facing, float); n = n - d * np.dot(n, d); n /= np.linalg.norm(n)
    s = np.cross(n, d)
    if s[1] > 0:
        s = -s                                       # x runs toward the front
    z = thick / 2 + arch * math.sin(math.pi * t * 0.85) - cup * width * x * x
    dz = arch * math.pi * 0.85 * math.cos(math.pi * t * 0.85) / L
    nn = n - d * dz + s * (2 * cup * x); nn /= np.linalg.norm(nn)
    return base + d * L * t + s * x * width * 0.95 + n * z, nn


def disc(c, n, r, thick):
    """A flat round spot lying on a surface: a loose flattened ball along n."""
    n = unit(*n)
    a = np.cross(n, (0, 0, 1.0))
    if np.linalg.norm(a) < 1e-3:
        a = np.array([1.0, 0, 0])
    a /= np.linalg.norm(a)
    bb = np.cross(n, a)
    V, F = ell((0, 0, 0), (r, r, thick), sub=3)
    return outward((V @ np.stack([a, bb, n], axis=1).T + np.array(c, float), F))


def pink_eye(u, v):
    """Steenee's big eye: a dark rim, a magenta iris lighter below, a dark pupil and two white glints."""
    c = np.tile(IRIS, (len(u), 1))
    c[v < -0.25] = IRIS_LIGHT
    c[(u / 0.36) ** 2 + ((v + 0.02) / 0.46) ** 2 < 1] = PUPIL
    c[((u + 0.2) / 0.3) ** 2 + ((v - 0.34) / 0.3) ** 2 < 1] = WHITE_
    c[((u - 0.3) / 0.16) ** 2 + ((v + 0.4) / 0.16) ** 2 < 1] = WHITE_
    c[u * u + v * v > 0.82 ** 2] = EYE_RIM
    return c


def bulb(k, c, sx=1.0, sz=1.0, front=False, upto=0.316, on=(), bone="body"):
    """
    The line's skirt: a white bulb of four lobes round its middle (and one at the front for Tsareena), drawn in below,
    under a magenta top whose six points come down onto the white. sx and sz widen and heighten it; upto is where the
    top's paint stops; on names the caller's parts the paint may colour beside the bulb's own.
    """
    c = np.array(c, float)
    k.add("bulb", ell(c, (0.1 * sx, 0.092, 0.07 * sz)), WHITE, bone)
    lobes = [(45 + 90 * i, 1.0) for i in range(4)] + ([(0, 0.7)] if front else [])
    for i, (a, f) in enumerate(lobes):
        t = math.radians(a)
        k.add(f"lobe{i}", ell(c + np.array([math.sin(t) * 0.044 * sx * f, -math.cos(t) * 0.04 * f, -0.004 * sz]),
                              (0.068 * sx ** 0.7, 0.066, 0.074 * sz)), WHITE, bone)
    k.add("bottom", ell(c + (0, 0, -0.06 * sz), (0.036 * sx, 0.036, 0.03)), WHITE, bone)
    def top(P):
        th = np.arctan2(P[:, 0], -P[:, 1])
        edge = c[2] + 0.054 * sz - 0.018 * sz * (0.5 + 0.5 * tri(th * 6 / (2 * math.pi)))   # six points down
        return inside(edge - P[:, 2], 0.003) * inside(P[:, 2] - upto, 0.003)
    k.paint(top, MAGENTA, on=["bulb", "lobe"] + list(on))


def steenee(src, out):
    k = Kit(src, coat="scales")

    bulb(k, BC, upto=0.316, on=["neck", "sepal"])
    k.add("neck", tube([(0, 0.0, 0.262), (0, -0.002, 0.33)], [0.034, 0.022]), MAGENTA, "body")
    k.add("sepal", ell((0, 0, 0.272), (0.064, 0.058, 0.042)), MAGENTA, "body")
    # a dark magenta tip under the bulb where the legs come out
    k.paint(lambda P: inside(P[:, 2] - 0.142, 0.003) * inside(np.hypot(P[:, 0], P[:, 1]) - 0.05), MAGENTA)

    # --- the legs: two tapering halves pressed together into one long magenta point
    k.add("leg", tube([(0.011, 0.0, 0.15), (0.011, -0.002, 0.09), (0.007, -0.004, 0.03), (0.004, -0.004, 0.008)],
                      [0.02, 0.014, 0.008, 0.004]), MAGENTA, "leg{s}", mirror=True)

    # --- the frill under the chin: little upturned magenta petals round the neck
    for i in range(7):
        a = 2 * math.pi * i / 7
        d = unit(math.sin(a), -math.cos(a), 1.3)
        c = np.array([math.sin(a) * 0.022, -math.cos(a) * 0.022 - 0.002, 0.332]) + d * 0.012
        V, F = ell((0, 0, 0), (0.013, 0.006, 0.019), sub=3)
        ax = np.cross(d, (0, 0, 1.0)); ax /= np.linalg.norm(ax)
        M = np.stack([ax, np.cross(d, ax), d], axis=1)
        k.add(f"frill{i}", outward((V @ M.T + c, F)), MAGENTA_DARK if i % 2 else MAGENTA, "body", fuse=False)

    # --- the arms: thin white, forward and down, round little hands
    for sx in (1, -1):
        side = "R" if sx > 0 else "L"
        sh = np.array([0.026 * sx, 0.0, 0.3])
        el = np.array([0.06 * sx, -0.03, 0.282])
        wr = np.array([0.084 * sx, -0.066, 0.256])
        k.add("arm" + side, tube([sh, el, wr], [0.0145, 0.0135, 0.0135]), WHITE, "arm" + side)
        k.add("hand" + side, ell(wr + np.array([0.006 * sx, -0.008, -0.004]), (0.015, 0.015, 0.014)), WHITE,
              "arm" + side)

    # --- the head: round, white, a little fuller at the cheeks; tiny magenta ear points on its upper sides
    def cheeks(V):
        V = V.copy()
        s = 1 + 0.06 * np.clip(-V[:, 2], 0, 1) - 0.04 * np.clip(V[:, 2], 0, 1) ** 2
        V[:, 0] *= s
        return V
    k.add("head", ell(HC, HR, deform=cheeks, sub=5), WHITE, "head")
    for sx in (1, -1):
        p, n = b.on_ell(HC, HR, (0.76 * sx, -0.22, 0.42))
        tip = p + unit(0.6 * sx, -0.1, 1) * 0.03
        side = np.cross(n, (0, 1.0, 0)); side /= np.linalg.norm(side)
        k.add("ear" + ("R" if sx > 0 else "L"),
              hull([p - n * 0.006 + side * 0.012, p - n * 0.006 - side * 0.012, p - n * 0.004 + (0, 0.011, 0),
                    p - n * 0.004 - (0, 0.011, 0), tip]), MAGENTA, "head", fuse=False)

    # --- the cap, the stalk curling to the left (-X), and the two great leaves
    b.cap(k, CAP, 0.082, 0.082, "head")
    b.stalk(k, [(0.0, 0.01, 0.528), (-0.004, 0.012, 0.562), (-0.018, 0.012, 0.586), (-0.036, 0.01, 0.584),
                (-0.044, 0.008, 0.568)], [0.0125, 0.012, 0.0115, 0.012, 0.0125], "head")
    for sx in (1, -1):
        m = np.array([sx, 1, 1.0])
        k.add("leaf" + ("R" if sx > 0 else "L"),
              b.round_leaf(LEAF_ROOT * m, LEAF_TIP * m, LEAF_W, LEAF_ARCH, np.array(LEAF_FACE) * m, cup=LEAF_CUP,
                           thick=LEAF_THICK), GREEN, "head", fuse=False, surface="leaf")
    # two yellow spots near the left leaf's root
    m = np.array([-1, 1, 1.0])
    for t, x, r in ((0.2, 0.35, 0.026), (0.32, 0.45, 0.015)):
        p, n = leaf_point(LEAF_ROOT * m, LEAF_TIP * m, np.array(LEAF_FACE) * m, t, LEAF_ARCH, LEAF_THICK, x, LEAF_W,
                          LEAF_CUP)
        k.add(f"spot{t}", disc(p + n * 0.003, n, r, 0.006), YELLOW, "head", fuse=False, surface="leaf")

    # --- the open mouth (in every state, so before the body's build)
    mp, mn = b.on_ell(HC, HR, (0, -1, -0.5))
    b.open_mouth(k, mp + (0, 0, 0.004), mn, (0.03, 0.022))

    k.build(voxel=0.005, smooth=4, band=0.006)

    # --- the big magenta eyes
    LID = srgb(WHITE)
    ep, en = b.on_ell(HC, HR, EYE_D)
    def eyes(state):
        k.decal(ep, en, EYE_HALF, eye_look(state, pink_eye, LID, INK, line=0.15), eps=0.003, rings=20, segs=56,
                mirror=True)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    steenee(*args())
