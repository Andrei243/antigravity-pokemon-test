# Meowth sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/meowth.py -- <the export's Meowth.glb> <out.glb>
# A cream cat standing on two legs, mostly head: a wide round head fuller at the cheeks, black ears lined brown, a gold
# oval coin with three ridges standing proud on the brow, very large white eyes with a slit pupil, a wide open mouth
# with two fangs, short thick whiskers (three a side and two over the brow), a slim body, thin arms with round paws,
# brown feet, and a long thin tail curling up beside the body into a brown spiral. Written to be grown from
# (persian.py keeps this frame: the coat, the ears, the eyes' way, the whiskers, the curled tail).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, hull, srgb, inside, unit, eye_look, args

# --- colours ----------------------------------------------------------------------------------------------------------
CREAM = "#F4EACB"
BROWN = "#A86A44"            # the feet and the tail's curl
EAR, LINING = "#33292E", "#9C6546"
GOLD, RIDGE = "#FFD430", "#C98E14"
EYE_W, INK = "#FFFFFF", "#221C20"
MOUTH, MOUTH_IN = "#7E2C3C", "#E88C9C"
FANG = "#FFFFFF"

# --- the frame (the skeleton's joints: body (0, 0, 0.33), head (0, -0.01, 0.44), ears (+-0.1, 0, 0.66), arms
#     (+-0.08, -0.01, 0.41), legs (+-0.05, 0, 0.26), tail (0, 0.06, 0.27)) --------------------------------------------
# The head is nearly half the figure (with its ears) and as wide as the body is tall: everything on it is laid out
# at the first sculpt's size (HEAD0) and grown by GROW about its middle, so the face keeps its arrangement
H0 = np.array([0, -0.02, 0.58])
GROW = 1.25
H = np.array([0, -0.02, 0.6])            # the head's middle
HR = np.array([0.168, 0.13, 0.136]) * GROW


def grown(p):
    """A place laid out on the first sculpt's head, on the grown head."""
    return H + GROW * (np.array(p, float) - H0)


EAR_BASE = grown((0.092, 0.0, 0.655))
EAR_TIP = grown((0.17, 0.022, 0.825))
EAR_R = [r * GROW for r in (0.07, 0.054, 0.028, 0.006)]
EYE_X, EYE_Z = 0.074 * GROW, 0.0         # an eye's middle out from the face's middle, and up from H
EYE_HALF = (0.044 * GROW, 0.058 * GROW)


def on_head(x, z, lift=0.0, h=H, hr=HR):
    """The head's front point at (x, z), lifted along its normal."""
    zn = (z - h[2]) / hr[2]
    q = max(0.0, 1 - zn * zn - (x / hr[0]) ** 2)
    p = np.array([x, h[1] - hr[1] * math.sqrt(q), z])
    n = (p - h) / hr ** 2; n /= np.linalg.norm(n)
    return p + n * lift, n


def add_head(k):
    k.add("head", ell(H, HR), CREAM, "head")
    # Full cheeks: the head is widest low down, round the mouth
    k.add("cheek", ell(H + GROW * np.array([0.075, -0.03, -0.05]), np.array([0.095, 0.08, 0.075]) * GROW), CREAM,
          "head", mirror=True)
    k.add("neck", tube([(0, 0.0, 0.39), (0, -0.01, 0.45)], [0.05, 0.056]), CREAM, "head")


def add_body(k):
    # A short pear: a narrow chest over a round belly that hangs low, so little of the legs shows under it
    k.add("body", ell((0, 0.0, 0.365), (0.072, 0.064, 0.07)), CREAM, "body")
    k.add("belly", ell((0, -0.006, 0.272), (0.1, 0.086, 0.088)), CREAM, "body")


def add_legs(k):
    k.add("leg", tube([(0.045, 0.0, 0.26), (0.056, 0.0, 0.15), (0.062, -0.008, 0.055)], [0.05, 0.038, 0.034]),
          CREAM, "leg{s}", mirror=True)
    k.add("foot", ell((0.066, -0.045, 0.03), (0.052, 0.078, 0.032)), BROWN, "leg{s}", mirror=True)
    # The brown runs up the lower leg; a crisp line across it
    k.paint(lambda P: inside(P[:, 2] - 0.105, 0.0025) * (np.abs(P[:, 0]) > 0.02) * (P[:, 1] < 0.04), BROWN)


def add_arms(k):
    # Arms down along the sides, the elbows bent so the forearms come forward, the round paws at the hips in front of
    # the belly's edge (never out level in a T)
    k.add("arm", tube([(0.065, -0.01, 0.405), (0.11, 0.0, 0.34), (0.125, -0.04, 0.29), (0.12, -0.085, 0.28)],
                      [0.034, 0.032, 0.031, 0.031]), CREAM, "arm{s}", mirror=True)
    k.add("paw", ell((0.118, -0.1, 0.279), (0.04, 0.038, 0.037)), CREAM, "arm{s}", mirror=True)


def add_ears(k, base=EAR_BASE, tip=EAR_TIP, radii=EAR_R, outer=EAR, lining=LINING, inset=0.58, bone="ear{s}",
             coat=CREAM, root=0.11):
    """Wide pointed ears from the top corners of the head, flattened front to back, their front faces lined."""
    pts = [base, base + (tip - base) * 0.4, base + (tip - base) * 0.8, tip]
    k.add("ear", blade(pts, radii, (0, 1, 0), 0.4), outer, bone, mirror=True)
    d = unit(*(tip - base))
    L = np.linalg.norm(tip - base)
    across = np.cross(d, (0, 1.0, 0)); across /= np.linalg.norm(across)
    rr = np.array(radii, float)

    def mask(P):
        Q = P.copy(); Q[:, 0] = np.abs(Q[:, 0])
        rel = Q - base
        t = rel @ d / L
        w = rel @ across
        half = inset * np.interp(t, [0, 0.4, 0.8, 1.0], rr)
        fr = rel[:, 1] - (tip[1] - base[1]) * t
        return (inside(np.abs(w) - half, 0.0025) * inside(fr + 0.006, 0.002) * inside(0.1 - t, 0.004)
                * inside(t - 0.82, 0.004) * (P[:, 2] > base[2] - 0.03))
    k.paint(mask, lining)

    def ear_root(P):
        """The coat back up to the ear's root, so the black doesn't wash down over the head."""
        Q = P.copy(); Q[:, 0] = np.abs(Q[:, 0])
        rel = Q - base
        t = rel @ d / L
        return inside(t - root, 0.002) * inside(np.linalg.norm(rel - np.outer(t * L, d), axis=1) - 0.11, 0.01)
    k.paint(ear_root, coat)


def coin(c, n, up, half=(0.04, 0.062), thick=0.022, seg=40):
    """A gold oval: two rings of corners, the front one a little smaller, made one hard-edged solid."""
    n = np.array(n, float); up = np.array(up, float)
    side = np.cross(up, n); side /= np.linalg.norm(side)
    pts = []
    for s, depth in ((1.0, -thick * 0.5), (0.86, thick * 0.5)):
        for i in range(seg):
            a = 2 * math.pi * i / seg
            pts.append(c + n * depth + side * half[0] * s * math.cos(a) + up * half[1] * s * math.sin(a))
    return hull(pts)


COIN_HALF = (0.066, 0.09)                # a third of the head's width across


def add_coin(k, z=0.128, half=COIN_HALF, thick=0.032):
    c, n = on_head(0.0, H[2] + z, 0.012)
    up = np.array([0, 0, 1.0]) - n * n[2]; up /= np.linalg.norm(up)
    side = np.cross(up, n)
    k.add("coin", coin(c, n, up, half=half, thick=thick), GOLD, "head", fuse=False, surface="metal")
    # Three bold ridges across its lower half, darker gold, standing on its front face
    for i, s in enumerate((-0.2, -0.45, -0.7)):
        v = s * half[1]
        w = half[0] * 0.86 * math.sqrt(max(0.0, 1 - s * s)) * 0.82
        p = c + n * (thick * 0.5 + 0.003) + up * v
        corners = [p + side * sx * w + up * sz * 0.006 + n * sn * 0.004
                   for sx in (-1, 1) for sz in (-1, 1) for sn in (-1, 1)]
        k.add(f"ridge{i}", hull(corners), RIDGE, "head", fuse=False, surface="metal")


def add_whiskers(k, color=CREAM, side=None, top=True):
    """Short thick whiskers: three a side fanning out from the cheeks, two rising over the brow."""
    side = side or [tuple(map(grown, w)) for w in [((0.15, -0.075, 0.545), (0.245, -0.07, 0.585)),
                    ((0.155, -0.075, 0.525), (0.255, -0.065, 0.525)),
                    ((0.15, -0.07, 0.505), (0.24, -0.06, 0.465))]]
    for i, (a, b) in enumerate(side):
        a, b = np.array(a), np.array(b)
        k.add(f"whisker{i}", tube([a, (a + b) / 2, b], [0.008, 0.006, 0.0035], seg=8, per=6), color, "head",
              fuse=False, mirror=True, surface="hair")
    if top:
        for i, (a, b) in enumerate([(grown((0.062, -0.05, 0.7)), grown((0.088, -0.062, 0.79)))]):
            a, b = np.array(a), np.array(b)
            k.add(f"brow{i}", tube([a, (a + b) / 2, b], [0.008, 0.006, 0.0035], seg=8, per=6), color, "head",
                  fuse=False, mirror=True, surface="hair")


def curled_tail(root, way, centre, normal, radius, r0, r1, turns=1.4, start=0.0):
    """A thin tail along way's points to a flat spiral in the plane square to normal, wound inward round centre."""
    centre, normal = np.array(centre, float), unit(*normal)
    e1 = np.array([0, 0, -1.0]) - normal * (-normal[2]); e1 /= np.linalg.norm(e1)
    e2 = np.cross(normal, e1)
    pts = [np.array(root, float)] + [np.array(p, float) for p in way]
    spiral = []
    for t in np.linspace(0, 1, 24):
        a = start + t * 2 * math.pi * turns
        r = radius * (1 - 0.78 * t)
        spiral.append(centre + r * (math.cos(a) * e1 + math.sin(a) * e2))
    return pts, spiral


def add_tail(k):
    pts, spiral = curled_tail((0, 0.055, 0.27), [(0.03, 0.13, 0.2), (0.1, 0.2, 0.17), (0.17, 0.24, 0.19)],
                              (0.225, 0.25, 0.26), (0.92, -0.38, 0), 0.07, 0.02, 0.012, turns=1.35)
    k.add("tail", tube(pts + spiral[:1], [0.022, 0.019, 0.017, 0.016, 0.016]), CREAM, "tail")
    k.add("tailcurl", tube(spiral, np.linspace(0.02, 0.013, len(spiral))), BROWN, "tail")


# --- the face: patches on the fused head, laid before the body's build ----------------------------------------------
CC, CI, CW = srgb(CREAM), srgb(INK), srgb(EYE_W)


def mouth(u, v):
    """A wide open grin: a dark top lip curving up at the corners, the inside pink below."""
    c = np.tile(CC, (len(u), 1))
    top = 0.32 + 0.3 * u * u
    m = (v < top) & (np.hypot(u / 0.94, (v - 0.22) / 0.98) < 1)          # its foot well inside the patch
    c[m] = srgb(MOUTH_IN)
    c[m & (v > top - 0.24)] = srgb(MOUTH)
    return c


def add_mouth(k, z=-0.075 * GROW, half=(0.07, 0.046)):
    p, n = on_head(0.0, H[2] + z)
    p = p + np.array([0, -0.03, 0])
    k.decal(p, unit(0, -1, -0.15), half, mouth, eps=0.003, rings=16, segs=64)
    # Two little fangs hanging from the top lip's corners
    for s in (1, -1):
        top = p + np.array([0.036 * s, -0.004, half[1] * 0.42])
        k.add(f"fang{'R' if s > 0 else 'L'}", hull([top + (0.008, 0.002, 0), top + (-0.008, 0.002, 0),
                                                    top + (0, -0.006, 0), top + (0.0, -0.001, -0.02)]),
              FANG, "head", fuse=False, surface="shell")


def eye(u, v):
    """A tall white oval with a dark rim and a dark slit pupil."""
    c = np.tile(CI, (len(u), 1))
    c[np.hypot(u, v) < 0.8] = CW
    c[np.abs(u + 0.04) < 0.17 * np.sqrt(np.clip(1 - (v / 0.7) ** 2, 0, 1))] = CI
    return c


def white(u, v):
    """The open eye under its pupil: the white and its rim alone."""
    c = np.tile(CI, (len(u), 1))
    c[np.hypot(u, v) < 0.8] = CW
    return c


def lay_eyes(k, state, half=EYE_HALF, x=EYE_X, z=EYE_Z, look=eye, pupil=(0.0106, 0.05), up=unit(0.14, 0, 1)):
    """The open eye's slit is a patch of its own over the white, so its edge is crisp (cut in the white's rings it
    came out ragged); the other states draw from the eye with the slit in it."""
    p, n = on_head(x, H[2] + z, 0.0)
    if state == "Open":
        k.decal(p, n, half, white, eps=0.004, rings=20, segs=56, mirror=True, up=up)
        k.decal(p, n, pupil, CI, eps=0.0065, rings=8, segs=40, mirror=True, up=up)
        return
    k.decal(p, n, half, eye_look(state, look, CC, CI), eps=0.004, rings=20, segs=56, mirror=True, up=up)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="fur")
    add_body(k)
    add_legs(k)
    add_arms(k)
    add_head(k)
    add_ears(k)
    add_tail(k)
    add_coin(k)
    add_whiskers(k)
    add_mouth(k)
    k.build(voxel=0.0058, band=0.006)
    k.eye_states(lambda s: lay_eyes(k, s))
    k.export(out)
