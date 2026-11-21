# Naganadel sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/naganadel.py -- <the export's Naganadel.glb> <out.glb>
#
# Poipole grown (sibling("poipole")): its purple, magenta and pale lavender, its pale needles and its angular cyan
# eyes. From the references: a purple wasp of a dragon in flight. Its largest mass is a great purple abdomen lying
# behind, an egg with a magenta socket at its rear end from which a long pale lavender stinger runs back and down to
# a dark hollow tip; a pale needle with a dark tip on each side of the abdomen. From the abdomen's front a thin purple
# body rises in an S to a small dragon's head carried forward: a pointed snout, a magenta jaw, cyan visor eyes and a
# crest of three purple blades swept back, tipped magenta. Two great pale needles stand out from the shoulders; small
# thin arms end in magenta pincers. Two great purple wings, broad at the root, scalloped and hanging to a long point at
# their outer foot, a magenta streak along the top and magenta four-point stars on them, a few purple spines along the
# top edge. Here the wings are a mirror pair raised in a V with their faces to the game's cameras, the stinger straight
# back (the art turns the whole beast).
# Skeleton: body (0, 0, 0.174), wings (+-0.02, -0.09, 0.294), arms (+-0.03, -0.12, 0.264), head (0, -0.12, 0.304).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, slab, lathe, srgb, inside, unit, eye_look, polygon, args, sibling

pp = sibling("poipole")
PURPLE, MAGENTA, NEEDLE, CYAN, INK = pp.PURPLE, pp.MAGENTA, pp.NEEDLE, pp.CYAN, pp.INK
TIP = "#2A2034"
VOXEL = 0.0045

AB_C = np.array([0.0, 0.085, 0.185])       # the abdomen, the largest mass
AB_R = np.array([0.098, 0.145, 0.098])
ST_DIR = unit(0, 1, -0.28)                  # the stinger: back and a little down
ST_BASE = AB_C + ST_DIR * 0.13
HC = np.array([0.0, -0.155, 0.342])          # the head, carried forward of its joint
HR = np.array([0.046, 0.064, 0.043])


def spike(base, d, length, w, thick, up=(0, 0, 1)):
    """A solid spike or blade: a kite at its root, a point at its tip (a flat-faced hull)."""
    d = unit(*d)
    a = np.cross(d, up); a = a / np.linalg.norm(a) if np.linalg.norm(a) > 1e-3 else np.array([1.0, 0, 0])
    b = np.cross(a, d)
    base = np.array(base, float)
    pts = [base + a * w, base - a * w, base + b * thick, base - b * thick, base - d * 0.01,
           base + d * length * 0.55 + a * w * 0.45, base + d * length * 0.55 - a * w * 0.45, base + d * length]
    return hull(pts)


def tipped_needle(k, name, base, d, length, r, bone, mirror=False):
    """A pale needle (Poipole's) ending in a dark hollow tip."""
    d = unit(*d)
    base = np.array(base, float)
    k.add(name, lathe(base - d * 0.01, d, [(0, 0), (r, 0), (r * 0.92, length * 0.4), (r * 0.6, length * 0.85),
                                          (0.0, length * 0.86)]), NEEDLE, bone, fuse=False, mirror=mirror,
          surface="shell")
    k.add(name + "tip", lathe(base + d * length * 0.8, d, [(0, 0), (r * 0.66, 0), (r * 0.5, length * 0.12),
                                                          (0.0, length * 0.2)]), TIP, bone, fuse=False, mirror=mirror,
          surface="shell")


def abdomen(k):
    k.add("abdomen", ell(AB_C, AB_R, sub=5), PURPLE, "body")
    # the magenta socket at the rear, where the stinger leaves
    k.add("socket", ell(AB_C + ST_DIR * 0.118, (0.06, 0.04, 0.06), rot=(-16, 0, 0)), MAGENTA, "body")
    k.paint(lambda P: inside(np.abs((P - AB_C) @ ST_DIR - 0.096) - 0.012, 0.002), MAGENTA, on=["abdomen"])
    k.add("stinger", lathe(ST_BASE, ST_DIR, [(0, 0), (0.036, 0), (0.03, 0.05), (0.022, 0.2), (0.012, 0.31),
                                             (0.0, 0.312)]), NEEDLE, "body", fuse=False, surface="shell")
    k.add("stingertip", lathe(ST_BASE + ST_DIR * 0.285, ST_DIR, [(0, 0), (0.0148, 0), (0.0128, 0.03), (0.0, 0.05)]),
          TIP, "body", fuse=False, surface="shell")
    for s in (1, -1):
        tipped_needle(k, "sideneedle" + ("R" if s > 0 else "L"), AB_C + (0.088 * s, 0.03, -0.02),
                      (0.55 * s, 0.25, -0.8), 0.07, 0.011, "body")
    # small spines on the abdomen's back
    for i, y in enumerate((0.02, 0.1)):
        k.add(f"backspine{i}", spike(AB_C + (0, y - 0.085, AB_R[2] * 0.95 - abs(y - 0.085) * 0.25), (0, 0.6, 1),
                                     0.04, 0.012, 0.008, up=(1, 0, 0)), PURPLE, "body", fuse=False)


def body(k):
    k.add("waist", ell(AB_C + (0, -0.12, 0.03), (0.05, 0.05, 0.05)), PURPLE, "body")
    k.add("neck", tube([(0, -0.055, 0.225), (0, -0.09, 0.27), (0, -0.105, 0.3), (0, -0.13, 0.322)],
                       [0.034, 0.029, 0.024, 0.022]), PURPLE, "head")
    k.add("chest", ell((0, -0.085, 0.272), (0.034, 0.032, 0.036)), PURPLE, "body")
    k.paint(lambda P: inside(np.abs(P[:, 2] - 0.247) - 0.008, 0.002) * inside(P[:, 1] + 0.05, 0.01), MAGENTA,
            on=["neck", "chest", "waist"])
    for s in (1, -1):
        sd = "R" if s > 0 else "L"
        # the great pale needles out of the shoulders
        k.add("shoulderneedle" + sd, lathe((0.025 * s, -0.08, 0.282), unit(0.85 * s, 0.35, 0.3),
                                           [(0, 0), (0.014, 0), (0.012, 0.035), (0.006, 0.08), (0.0, 0.1)]),
              NEEDLE, "wing" + sd, fuse=False, surface="shell")
        # small arms forward and down, magenta pincers
        k.add("arm" + sd, tube([(0.026 * s, -0.11, 0.262), (0.045 * s, -0.135, 0.235), (0.04 * s, -0.16, 0.215)],
                               [0.011, 0.01, 0.009]), PURPLE, "arm" + sd)
        for j, dz in enumerate((0.012, -0.012)):
            k.add(f"pincer{j}" + sd, spike((0.04 * s, -0.162, 0.215 + dz * 0.4), (0.15 * s, -1, dz * 3), 0.032, 0.008,
                                           0.006, up=(1, 0, 0)), MAGENTA, "arm" + sd, fuse=False, surface="shell")


def head(k):
    k.add("skull", ell(HC, HR, rot=(-8, 0, 0)), PURPLE, "head")
    k.add("snout", ell(HC + (0, -0.058, -0.01), (0.026, 0.044, 0.024), rot=(-10, 0, 0)), PURPLE, "head")
    k.add("jaw", ell(HC + (0, -0.04, -0.028), (0.028, 0.042, 0.015), rot=(-12, 0, 0)), MAGENTA, "head")
    # the crest: three purple blades swept back, tipped magenta
    k.add("crestmid", spike(HC + (0, -0.01, 0.026), (0, 0.85, 0.5), 0.085, 0.012, 0.008, up=(1, 0, 0)), PURPLE,
          "head", fuse=False)
    k.add("crestmidtip", spike(HC + (0, -0.01, 0.026) + unit(0, 0.85, 0.5) * 0.05, (0, 0.85, 0.5), 0.042, 0.007,
                               0.0095, up=(1, 0, 0)), MAGENTA, "head", fuse=False)
    for s in (1, -1):
        sd = "R" if s > 0 else "L"
        d = unit(0.6 * s, 0.75, 0.32)
        root = HC + (0.022 * s, 0.0, 0.016)
        k.add("crest" + sd, spike(root, d, 0.08, 0.012, 0.008), PURPLE, "head", fuse=False)
        k.add("cresttip" + sd, spike(root + d * 0.045, d, 0.04, 0.007, 0.0095), MAGENTA, "head", fuse=False)


# --- the wings: a mirror pair raised in a V, faces forward and back to the game's cameras --------------------------
W_ORIGIN = np.array([0.03, -0.075, 0.29])
W_EX = unit(0.85, 0.12, 0.5)           # outward and up
W_EZ = unit(0.0, 0.5, 1.0)             # up and back
W_OUT = [(0.0, 0.02), (0.07, 0.09), (0.15, 0.15), (0.22, 0.165), (0.28, 0.13), (0.33, 0.06), (0.355, -0.04),
         (0.35, -0.2), (0.3, -0.11), (0.245, -0.085), (0.19, -0.12), (0.135, -0.07), (0.08, -0.09), (0.03, -0.04),
         (0.0, -0.02)]
W_STREAK = [(0.05, 0.07), (0.15, 0.135), (0.22, 0.15), (0.21, 0.125), (0.15, 0.11), (0.06, 0.05)]


def star(cx, cy, r):
    pts = []
    for i in range(8):
        a = math.pi / 2 + i * math.pi / 4
        rr = r if i % 2 == 0 else r * 0.38
        pts.append((cx + rr * math.cos(a), cy + rr * math.sin(a)))
    return pts


WS = 0.74             # the wings' size: kept under the abdomen's and the stinger's length, so the body reads


def wings(k):
    ex = W_EX; ez = W_EZ - ex * (W_EZ @ ex); ez /= np.linalg.norm(ez)
    sc = lambda pts: [(a * WS, b * WS) for a, b in pts]
    k.add("wing", slab(sc(W_OUT), W_ORIGIN, ex, ez, 0.016), PURPLE, "wing{s}", fuse=False, mirror=True)
    k.add("wstreak", slab(sc(W_STREAK), W_ORIGIN, ex, ez, 0.022), MAGENTA, "wing{s}", fuse=False, mirror=True)
    for i, (cx, cy, r) in enumerate(((0.27, 0.02, 0.045), (0.15, 0.0, 0.038), (0.32, -0.12, 0.034))):
        k.add(f"wstar{i}", slab(sc(star(cx, cy, r)), W_ORIGIN, ex, ez, 0.022), MAGENTA, "wing{s}", fuse=False,
              mirror=True)
    n = np.cross(ex, ez)
    # spines along the top edge, pointing out of it
    for i, (a, b) in enumerate(((0.12, 0.13), (0.25, 0.155), (0.335, 0.03))):
        p = W_ORIGIN + WS * (a * ex + b * ez)
        d = ez * 0.8 + ex * 0.6
        k.add(f"wspine{i}", spike(p, d, 0.05, 0.013, 0.009, up=n), PURPLE, "wing{s}", fuse=False, mirror=True)
    # the arm along the leading edge
    k.add("warm", tube([W_ORIGIN, W_ORIGIN + WS * (0.1 * ex + 0.1 * ez), W_ORIGIN + WS * (0.22 * ex + 0.16 * ez)],
                       [0.016, 0.013, 0.01]), PURPLE, "wing{s}", fuse=False, mirror=True)


# --- the eyes: Poipole's angular cyan, set on the head's sides and turned forward ----------------------------------
EYE_HALF = (0.025, 0.015)
CM, CI = srgb(MAGENTA), srgb(INK)
CP = srgb(PURPLE)


def eyes(k, state):
    look = eye_look(state, pp.eye_open, CP, CI, line=0.18)
    for s in (1, -1):
        c = HC + (0.033 * s, -0.034, 0.014)
        n = unit(0.75 * s, -0.6, 0.25)
        col = look if s > 0 else (lambda a, b, f=look: f(-a, b))
        outline = pp.EYE_OUT if s > 0 else [(-u, v) for u, v in pp.EYE_OUT]
        if state in ("Open", "Fierce"):
            k.decal(c, n, EYE_HALF, INK, eps=0.0015, outline=polygon(outline), rings=12, segs=56)
            k.decal(c, n, (EYE_HALF[0] * pp.RIM, EYE_HALF[1] * pp.RIM), col, eps=0.0025, outline=polygon(outline),
                    rings=20, segs=56)
        else:
            k.decal(c, n, EYE_HALF, col, eps=0.0015, outline=polygon(outline), rings=20, segs=56)


if __name__ == "__main__":
    src, dst = args()
    k = Kit(src, coat="scales")
    abdomen(k)
    body(k)
    head(k)
    wings(k)
    k.build(voxel=VOXEL, smooth=5, band=0.006, wband=0.04)
    k.eye_states(lambda state: eyes(k, state))
    k.export(dst)
