# Delcatty sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown from
# Skitty's script (skitty.py, imported: the way the ears and their linings are made, the paint masks, the face's
# nose and mouth, the legs' and tail's frame):
#   blender --background --factory-startup --python sculpts/delcatty.py -- <the export's Delcatty.glb> <out.glb>
# From the references: an elegant cream cat on four long legs. A small cream face, broad at the cheeks, which end in
# a fur point each side; over it a great purple cap that is the ears and the back of the head in one, sweeping out
# to each side and up into spiky points, the ears' black insides showing under its edges either side of the face;
# large dark almond eyes with a white glint; a tiny nose and a small smile; a purple ruff of two round lobes on the
# chest, joined round the neck, with two purple beads on short dark stalks standing out from each side; a slim cream
# body on long legs that taper to small paws; a long thin tail curving up behind to a purple tuft cut into points like
# a tulip.
# Reference ratios (3D picture, cap to feet): face cap's edge to chin / height 0.21; face width with its points /
# height 0.45; the cap across / height 0.7; ruff across / height 0.34; legs under the ruff and body / height 0.3;
# the tail's tuft's top / height 0.74.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, unit, eye_look, polygon, outward, args, sibling

s_ = sibling("skitty")

# --- colours ----------------------------------------------------------------------------------------------------------
CREAM = "#F7E2A4"
PURPLE = "#AE78D0"
INNER = "#2A2228"
INK = "#1E1A20"
STALK = "#2E2830"

# --- the frame (the skeleton's joints: body (0, 0, 0.28), ruff (0, -0.07, 0.33), tail (0, 0.12, 0.3), head
#     (0, -0.1, 0.38), ears (+-0.06, -0.1, 0.48), forelegs (+-0.045, -0.08, 0.24), hind legs (+-0.045, 0.1, 0.24)) -----
H = np.array([0.0, -0.13, 0.418])         # the face's middle
HR = np.array([0.095, 0.085, 0.082])
LEG_X = 0.05
FORE_Y, HIND_Y = -0.085, 0.105
BODY_Z = 0.29
# the cap's wings: out from the crown and up, flat front to back (the ears)
EAR = [(0.05, -0.12, 0.5), (0.115, -0.115, 0.532), (0.18, -0.105, 0.565), (0.24, -0.095, 0.6)]
EAR_R = [0.07, 0.066, 0.05, 0.016]


def add_body(k):
    k.add("chest", ell((0, -0.07, BODY_Z), (0.072, 0.085, 0.078)), CREAM, "body")
    k.add("middle", ell((0, 0.02, BODY_Z - 0.004), (0.064, 0.1, 0.066)), CREAM, "body")
    k.add("rump", ell((0, 0.1, BODY_Z), (0.072, 0.072, 0.078)), CREAM, "body")
    k.add("neck", tube([(0, -0.09, 0.32), (0, -0.12, 0.37)], [0.05, 0.046]), CREAM, "head")


def add_legs(k):
    """Long legs but no sticks: broad shoulders and thighs fused over their top halves, tapering to small paws."""
    k.add("shoulder", ell((LEG_X - 0.005, FORE_Y, 0.245), (0.047, 0.055, 0.08)), CREAM, "leg{s}", mirror=True)
    k.add("leg", tube([(LEG_X, FORE_Y, 0.24), (LEG_X + 0.002, FORE_Y - 0.004, 0.13), (LEG_X + 0.003, FORE_Y - 0.008, 0.03)],
                      [0.036, 0.027, 0.023]), CREAM, "leg{s}", mirror=True)
    k.add("paw", ell((LEG_X + 0.004, FORE_Y - 0.022, 0.018), (0.027, 0.036, 0.019)), CREAM, "leg{s}", mirror=True)
    k.add("thigh", ell((LEG_X, HIND_Y + 0.01, 0.25), (0.054, 0.075, 0.09)), CREAM, "hind{s}", mirror=True)
    k.add("hind", tube([(LEG_X + 0.002, HIND_Y + 0.025, 0.2), (LEG_X + 0.003, HIND_Y + 0.035, 0.11),
                        (LEG_X + 0.003, HIND_Y + 0.008, 0.03)], [0.036, 0.026, 0.023]), CREAM, "hind{s}", mirror=True)
    k.add("hpaw", ell((LEG_X + 0.004, HIND_Y - 0.01, 0.018), (0.027, 0.036, 0.019)), CREAM, "hind{s}", mirror=True)


def add_head(k):
    def cheeky(V):
        V = V.copy()
        V[:, 0] *= 1 + 0.12 * np.clip(-V[:, 2], 0, 1) * (1 - np.clip(-V[:, 2], 0, 1)) * 2
        return V
    k.add("head", ell(H, HR, deform=cheeky), CREAM, "head")
    k.add("muzzle", ell(H + (0, -0.062, -0.034), (0.04, 0.032, 0.028)), CREAM, "head")
    # a point of fur at each cheek, out and a little down
    base = H + np.array((0.072, -0.03, -0.018))
    d = unit(1, 0.05, -0.22)
    k.add("cheekfur", blade([base, base + d * 0.045, base + d * 0.078], [0.036, 0.024, 0.005], (0, 1, 0), 0.6), CREAM,
          "head", mirror=True)


def add_cap(k):
    """The cap: a purple dome over the crown and the back of the head, out into a wing each side (the ears), each
    ending in spiky points, the black inside of the ear on the wing's front low near the face."""
    k.add("cap", ell(H + (0, 0.03, 0.087), (0.128, 0.1, 0.076)), PURPLE, "head")
    k.add("capback", ell(H + (0, 0.06, 0.02), (0.09, 0.07, 0.075)), PURPLE, "head")
    k.add("ear", blade(EAR, EAR_R, (0, 1, 0), 0.42), PURPLE, "ear{s}", mirror=True)
    d = unit(*(np.array(EAR[-1]) - np.array(EAR[0])))
    across = np.cross(d, (0, 1.0, 0)); across /= np.linalg.norm(across)

    def inner(P):
        """The black inside: the lower front of each wing, near the face, a triangle narrowing outward."""
        t, off = s_.along(P, EAR)
        w = off @ across                         # +: up the wing's face
        half = 0.05 * np.clip(1 - t / 0.6, 0, 1)
        return (inside(np.abs(w + 0.022) - half, 0.0025) * inside(off[:, 1] + 0.004, 0.002)
                * inside(t - 0.6, 0.004) * (np.abs(P[:, 0]) > 0.075))
    k.paint(inner, INNER)
    # the points at each wing's end: one up, one out, one out and down; and one standing on the wing's top
    tip = np.array(EAR[-1])
    for i, (dd, ln, r) in enumerate(((unit(0.3, 0, 1), 0.06, 0.022), (unit(1, 0, 0.35), 0.065, 0.022),
                                     (unit(1, 0, -0.35), 0.05, 0.018))):
        base = tip - d * 0.02
        k.add(f"capspike{i}", blade([base, base + dd * ln * 0.5, base + dd * ln], [r, r * 0.65, 0.004], (0, 1, 0), 0.55),
              PURPLE, "ear{s}", mirror=True)
    top = np.array(EAR[1]) + np.array((0.0, 0.0, 0.045))
    k.add("capspike3", blade([top - (0, 0, 0.02), top + unit(0.4, 0, 1) * 0.03, top + unit(0.4, 0, 1) * 0.055],
                             [0.024, 0.014, 0.004], (0, 1, 0), 0.55), PURPLE, "ear{s}", mirror=True)


RUFF_C = np.array((0.0, -0.152, 0.296))


def add_ruff(k):
    """Two round purple lobes on the chest, joined round the neck, two beads on stalks out of each side."""
    for s, side in ((1, "R"), (-1, "L")):
        k.add(f"lobe{side}", ell(RUFF_C + (0.056 * s, 0.0, 0.0), (0.066, 0.056, 0.074), rot=(0, 0, 0)), PURPLE, "ruff")
    k.add("collar", ell((0, -0.1, 0.355), (0.075, 0.07, 0.04)), PURPLE, "ruff")
    for i, (z, ang) in enumerate(((0.345, 0.25), (0.27, -0.4))):
        root = RUFF_C + np.array((0.108, 0.01, z - RUFF_C[2]))
        d = unit(1, 0.15, ang)
        end = root + d * 0.04
        k.add(f"beadstalk{i}", tube([root - d * 0.015, end], [0.012, 0.011]), STALK, "ruff", fuse=False, mirror=True,
              surface="hair")
        k.add(f"bead{i}", outward(ell(end + d * 0.016, (0.022, 0.022, 0.022))), PURPLE, "ruff", fuse=False, mirror=True)


TAIL = [(0.0, 0.16, 0.3), (0.03, 0.225, 0.315), (0.07, 0.265, 0.37), (0.088, 0.268, 0.43), (0.09, 0.255, 0.47)]


def add_tail(k):
    """A long thin tail curving up behind, out to the +X side, to a purple tulip of a tuft."""
    k.add("tail", tube(TAIL, [0.019, 0.017, 0.016, 0.016, 0.017]), CREAM, "tail")
    tc = np.array((0.093, 0.252, 0.505))
    k.add("tuft", ell(tc, (0.04, 0.036, 0.045)), PURPLE, "tail")
    for i, (dx, dy) in enumerate(((0.0, 0.0), (0.03, 0.0), (-0.03, 0.0), (0.0, 0.028), (0.0, -0.028))):
        base = tc + np.array((dx * 0.5, dy * 0.5, 0.02))
        dd = unit(dx * 6, dy * 6, 1)
        k.add(f"tuftpoint{i}", blade([base, base + dd * 0.03, base + dd * 0.05], [0.02, 0.013, 0.003], (0, 1, 0), 0.7),
              PURPLE, "tail")


# --- the face's patches ---------------------------------------------------------------------------------------------
CC, CI = srgb(CREAM), srgb(INK)


def eye(u, v):
    """A large dark almond with a white glint up and out."""
    c = np.tile(CI, (len(u), 1))
    c[np.hypot((u - 0.25) / 0.21, (v - 0.32) / 0.23) < 1] = (1, 1, 1)
    return c


OUTLINE = [(-1.0, -0.1), (-0.8, 0.3), (-0.45, 0.62), (0.0, 0.82), (0.45, 0.85), (0.8, 0.62), (1.0, 0.2), (0.9, -0.3),
           (0.55, -0.72), (0.0, -0.9), (-0.5, -0.8),
           (-0.85, -0.5)]
EYE_HALF = (0.027, 0.03)


def lay_eyes(k, state):
    p, n = s_.on_head(0.046, H[2] + 0.004, 0.0, H, HR)
    n = unit(n[0] * 1.05, n[1], n[2])
    look = eye_look(state, eye, CC, CI, line=0.16)
    k.decal(p, n, EYE_HALF, look, eps=0.004, outline=polygon(OUTLINE), rings=20, segs=56)
    q = np.array([-1, 1, 1.0])
    k.decal(p * q, n * q, EYE_HALF, lambda u, v: look(-u, v), eps=0.004,
            outline=polygon([(-x, y) for x, y in OUTLINE]), rings=20, segs=56)


def smile(u, v):
    c = np.tile(CC, (len(u), 1))
    c[(np.abs(v - (-0.3 + 0.8 * u * u)) < 0.24) & (np.abs(u) < 0.85)] = CI
    return c


def add_nose_mouth(k):
    nose = H + np.array((0, -0.09, -0.022))
    k.decal(nose, unit(0, -1, 0.15), (0.008, 0.006), INK, eps=0.003,
            outline=polygon([(-1, 0.7), (1, 0.7), (0, -1)]), rings=6, segs=24)
    k.decal(H + np.array((0, -0.088, -0.045)), unit(0, -1, -0.2), (0.02, 0.01), smile, eps=0.003, rings=12, segs=56)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="fur")
    add_body(k)
    add_legs(k)
    add_head(k)
    add_cap(k)
    add_ruff(k)
    add_tail(k)
    add_nose_mouth(k)
    k.build(voxel=0.0055, band=0.004)
    k.eye_states(lambda s: lay_eyes(k, s))
    k.export(out)
