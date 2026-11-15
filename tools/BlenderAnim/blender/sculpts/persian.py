# Persian sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown from
# Meowth's script (meowth.py, imported: its ears, whiskers, curled tail and way of laying a patch on the head):
#   blender --background --factory-startup --python sculpts/persian.py -- <the export's Persian.glb> <out.glb>
# A sleek pale cream cat on four long legs: broad shoulders and thighs over tapering lower legs, a deep chest, the
# head carried up on a short thick neck, rounded ears rimmed dark and lined tan, a red gem standing on the brow (the
# coin grown into a jewel), narrow scowling red eyes cut by a straight dark lid, a small black nose, two little fangs,
# short whiskers, and the long tail curled up over its back into a round spiral.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, unit, polygon, eye_look, args, sibling

m = sibling("meowth")

# --- colours ----------------------------------------------------------------------------------------------------------
CREAM = "#F2E2B6"
EAR, LINING = "#3E3438", "#B48C72"
GEM = "#D8283A"
EYE_W, IRIS, PUPIL, INK = "#FFFFFF", "#D02A3A", "#3A1018", "#2A2024"
NOSE, FANG = "#222024", "#FFFFFF"

# --- the frame (the skeleton's joints: body (0, 0.01, 0.31), head (0, -0.16, 0.38), ears (+-0.065, -0.19, 0.52),
#     forelegs (+-0.06, -0.14, 0.28), hind legs (+-0.06, 0.15, 0.28), tail (0, 0.21, 0.33)) ------------------------------
H = np.array([0, -0.222, 0.47])          # the head's middle, up and forward of its joint
HR = np.array([0.122, 0.095, 0.09])
LEG_X = 0.062
FORE_Y, HIND_Y = -0.14, 0.15
EAR_BASE = np.array([0.058, -0.2, 0.52])
EAR_TIP = np.array([0.118, -0.18, 0.632])
EAR_R = [0.052, 0.054, 0.038, 0.014]


def on_head(x, z, lift=0.0):
    return m.on_head(x, z, lift, h=H, hr=HR)


def chin(V):
    """A broad flat face narrowing a little to the chin."""
    V = V.copy()
    V[:, 0] *= 1 - 0.1 * np.clip(-V[:, 2], 0, 1) ** 2
    return V


def add_head(k):
    k.add("head", ell(H, HR, deform=chin), CREAM, "head")
    k.add("cheek", ell(H + (0.058, -0.035, -0.035), (0.072, 0.06, 0.052)), CREAM, "head", mirror=True)
    k.add("muzzle", ell(H + (0, -0.085, -0.03), (0.042, 0.034, 0.032)), CREAM, "head")
    # A short thick neck, the head carried up out of the chest
    k.add("neck", tube([(0, -0.1, 0.33), (0, -0.15, 0.39), (0, -0.19, 0.44)], [0.072, 0.066, 0.06]), CREAM, "head")


def add_body(k):
    # A deep chest, a long sleek middle drawn up a little at the belly, a rounded rump
    k.add("chest", ell((0, -0.11, 0.315), (0.086, 0.1, 0.1)), CREAM, "body")
    k.add("middle", ell((0, 0.02, 0.32), (0.076, 0.14, 0.08)), CREAM, "body")
    k.add("rump", ell((0, 0.14, 0.31), (0.082, 0.09, 0.094)), CREAM, "body")


def add_legs(k):
    # Broad shoulders and thighs fused over the top half of each leg, the lower leg tapering to a round paw
    k.add("shoulder", ell((LEG_X - 0.004, FORE_Y + 0.01, 0.255), (0.05, 0.062, 0.085)), CREAM, "leg{s}", mirror=True)
    k.add("leg", tube([(LEG_X, FORE_Y, 0.24), (LEG_X + 0.002, FORE_Y - 0.008, 0.13), (LEG_X + 0.004, FORE_Y - 0.016, 0.036)],
                      [0.042, 0.031, 0.027]), CREAM, "leg{s}", mirror=True)
    k.add("paw", ell((LEG_X + 0.005, FORE_Y - 0.034, 0.022), (0.034, 0.046, 0.024)), CREAM, "leg{s}", mirror=True)
    k.add("thigh", ell((LEG_X, HIND_Y + 0.005, 0.255), (0.056, 0.08, 0.098)), CREAM, "hind{s}", mirror=True)
    k.add("hind", tube([(LEG_X + 0.002, HIND_Y + 0.02, 0.2), (LEG_X + 0.004, HIND_Y + 0.04, 0.11),
                        (LEG_X + 0.004, HIND_Y + 0.012, 0.036)], [0.042, 0.03, 0.027]), CREAM, "hind{s}", mirror=True)
    k.add("hpaw", ell((LEG_X + 0.005, HIND_Y - 0.008, 0.022), (0.034, 0.046, 0.024)), CREAM, "hind{s}", mirror=True)


def add_gem(k, z=0.05, r=0.021):
    """A red jewel standing proud of the brow: a faceted solid, a loose piece so its edges stay crisp."""
    c, n = on_head(0.0, H[2] + z, 0.004)
    up = np.array([0, 0, 1.0]) - n * n[2]; up /= np.linalg.norm(up)
    side = np.cross(up, n)
    pts = []
    for i in range(8):
        a = 2 * math.pi * (i + 0.5) / 8
        pts.append(c + side * r * math.cos(a) + up * r * math.sin(a) - n * 0.004)
        pts.append(c + side * r * 0.6 * math.cos(a) + up * r * 0.6 * math.sin(a) + n * r * 0.75)
    k.add("gem", hull(pts), GEM, "head", fuse=False, surface="metal")


def add_tail(k):
    # Out of the rump, back and up, curled round over the back into a spiral that faces the model's +X side
    pts, spiral = m.curled_tail((0, 0.21, 0.33), [(0.02, 0.28, 0.32), (0.05, 0.33, 0.36), (0.07, 0.35, 0.42)],
                                (0.07, 0.29, 0.46), (0.95, -0.3, 0.0), 0.085, 0.022, 0.014, turns=1.3,
                                start=0.98)
    k.add("tail", tube(pts + spiral, np.concatenate([[0.026, 0.024, 0.022, 0.021],
                                                      np.linspace(0.021, 0.014, len(spiral))])), CREAM, "tail")


def add_face(k):
    # A small black nose on the muzzle's tip, two little fangs under it
    tip = H + np.array([0, -0.118, -0.018])
    k.decal(tip, unit(0, -1, 0.3), (0.011, 0.008), NOSE, eps=0.003,
            outline=polygon([(-1, 0.7), (1, 0.7), (0, -1)]), rings=6, segs=24)
    for s in (1, -1):
        top = H + np.array([0.022 * s, -0.112, -0.05])
        k.add(f"fang{'R' if s > 0 else 'L'}", hull([top + (0.006, 0.003, 0), top + (-0.006, 0.003, 0),
                                                    top + (0, -0.004, 0), top + (0.0, 0.0, -0.016)]),
              FANG, "head", fuse=False, surface="shell")


WHISKERS = [((0.045, -0.31, 0.44), (0.13, -0.315, 0.465)),
            ((0.047, -0.31, 0.43), (0.135, -0.31, 0.425)),
            ((0.045, -0.305, 0.42), (0.125, -0.3, 0.385))]


# --- the eyes: narrow and scowling, their top edge a straight dark lid sloping down to the nose; a red iris with a dark
#     pupil. The outline is not symmetric, so each is a patch of its own with u outward (the left one flipped) -------
OUTLINE = [(-1, -0.05), (-0.4, 0.3), (0.4, 0.62), (1, 0.7), (0.9, 0.05), (0.35, -0.55), (-0.35, -0.62), (-0.85, -0.38)]
_RIM = polygon(OUTLINE)
CC, CW, CIR, CPU, CI = srgb(CREAM), srgb(EYE_W), srgb(IRIS), srgb(PUPIL), srgb(INK)
EYE_HALF = (0.034, 0.025)


def eye(u, v):
    c = np.tile(CW, (len(u), 1))
    c[np.hypot((u + 0.05) / 0.48, (v + 0.02) / 0.62) < 1] = CIR
    c[np.hypot((u + 0.05) / 0.17, (v + 0.02) / 0.44) < 1] = CPU
    rr = np.hypot(u, v) / np.array([_RIM(t) for t in np.arctan2(v, u)])
    c[(rr > 0.72) & (v > 0.0 + 0.35 * u)] = CI           # the heavy upper lid along the top edge
    c[(rr > 0.85)] = CI                                   # a dark rim the whole way round
    return c


def lay_eyes(k, state):
    p, n = on_head(0.052, H[2] + 0.004)
    look = eye_look(state, eye, CC, CI)
    k.decal(p, n, EYE_HALF, look, eps=0.004, outline=polygon(OUTLINE), rings=20, segs=56)
    q = np.array([-1, 1, 1.0])
    k.decal(p * q, n * q, EYE_HALF, lambda u, v: look(-u, v), eps=0.004,
            outline=polygon([(-x, y) for x, y in OUTLINE]), rings=20, segs=56)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="fur")
    add_body(k)
    add_legs(k)
    add_head(k)
    m.add_ears(k, base=EAR_BASE, tip=EAR_TIP, radii=EAR_R, outer=EAR, lining=LINING, inset=0.62, coat=CREAM, root=0.28)
    add_tail(k)
    add_gem(k)
    m.add_whiskers(k, color="#F4F0E2", side=WHISKERS, top=False)
    add_face(k)
    k.build(voxel=0.0058, band=0.006)
    k.eye_states(lambda s: lay_eyes(k, s))
    k.export(out)
