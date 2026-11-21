# Arcanine-Hisui sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/arcaninehisui.py -- <the export's Arcanine-Hisui.glb> <out.glb>
# Grown from arcanine.py through `sibling` (its trunk, legs, paws, chevrons, flank stripes, the band round its eyes, its
# ears, its mane and its brow-cut eyes); the open mouth is made as Growlithe-Hisui's (growlithehisui.py), copied here,
# since one script may import only one script that makes a Kit. From the references: a red-orange dog with black
# stripes on a deep trunk and stout legs; the head, the huge shaggy mane and the tail DARK GREY (the pictures overrule
# the task's "cream"), the mane in long pointed locks sweeping back over the shoulders and down over the chest, many of
# them with a red-orange streak; a rock horn standing on the crown and a smaller point behind it; a red-orange mask
# round each eye running back into the ears (red-orange rims, silver-grey insides); fierce eyes; a black nose and an
# open mouth with a pink tongue and fangs; grey cloud cuffs round the hind ankles; a
# huge grey cloud of a tail rising and sweeping back to a curling point, with a swirl on its side.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import ell, tube, blade, hull, srgb, inside, unit, lumpy, sibling

a = sibling("arcanine")
k = a.k

# --- colours ----------------------------------------------------------------------------------------------------------
RED_O = "#D24E2E"
GREY = "#65655F"           # the head, the mane, the tail
HORN = "#8E8C86"           # the rock: a little lighter, so its flat faces read against the mane
EAR_IN = "#B8B4BC"
MOUTH = "#8E2630"
TONGUE = "#E07A88"
FANG = "#F7F5F0"
SWIRL = "#4C4C48"
STRIPE = a.STRIPE

H, HR = a.H, a.HR          # (0, -0.29, 0.7), (0.118, 0.108, 0.108)
a.CREAM = GREY             # arcanine's tufts() reads this at run time: the mane's chest tufts grey
a.LID = RED_O              # the eyes lie in the red-orange mask
a.EYE_R = 0.031            # bolder than the base's, so the eyes read before the mask
a.LEG_R = (0.076, 0.068, 0.05)  # stouter legs than the base's (the pictures' short thick legs)


# --- the head: grey, the snout over a dropped jaw (growlithehisui.py's open mouth, grown) -----------------------------
def add_head():
    k.add("head", ell(H, HR, deform=a.head_shape), GREY, "head")
    k.add("neck", tube([(0, -0.15, 0.5), (0, -0.21, 0.58), (0, -0.25, 0.65)], [0.1, 0.09, 0.085]), GREY, "head")
    k.add("muzzle", ell(H + (0, -0.112, -0.03), (0.06, 0.066, 0.04)), GREY, "head")
    k.add("mouthin", ell(H + (0, -0.086, -0.078), (0.05, 0.046, 0.034)), MOUTH, "head")
    k.add("jaw", ell(H + (0, -0.098, -0.112), (0.054, 0.052, 0.024), rot=(-12, 0, 0)), GREY, "head")
    k.add("tongue", ell(H + (0, -0.108, -0.095), (0.034, 0.034, 0.013), rot=(-12, 0, 0)), TONGUE, "head")


def add_fangs():
    for s in (1, -1):
        c = H + np.array((0.027 * s, -0.158, -0.066))
        pts = [c + (-0.008, 0.005, 0.0), c + (0.008, 0.005, 0.0), c + (0, 0.014, 0.0), c + (0, -0.002, 0.0),
               c + (0.0, 0.005, -0.024)]
        k.add(f"fang{'R' if s > 0 else 'L'}", hull(pts), FANG, "head", fuse=False, surface="shell")


def add_nose():
    k.add("nose", ell(H + (0, -0.176, -0.008), (0.026, 0.02, 0.019), rot=(15, 0, 0)), a.NOSE, "head", fuse=False,
          surface="shell")


def add_horns():
    """A rock horn on the crown, leaning back a little, and a smaller point behind it: flat-faced loose pieces."""
    for name, at, r, tip in (("horn", (0, -0.03, 0.095), 0.045, (0, -0.01, 0.2)),
                             ("horn2", (0, 0.07, 0.1), 0.03, (0, 0.06, 0.09))):
        c = H + np.array(at)
        base = [c + (r * math.cos(t), r * math.sin(t), -0.02) for t in np.linspace(0, 2 * math.pi, 7)[:-1]]
        k.add(name, hull(base + [c + np.array(tip)]), HORN, "head", fuse=False, surface="shell")


# --- the mane's long locks: pointed, flattened, sweeping back over the shoulders and down the sides; a red-orange
#     streak down the outer face of most --------------------------------------------------------------------------------
NECK = np.array([0, -0.22, 0.6])
# (offset of the root from NECK, direction, length, root radius, streak?)
LOCKS = [((0.12, 0.02, 0.1), (0.5, 0.6, 0.55), 0.17, 0.05, True),
         ((0.15, 0.05, 0.03), (0.6, 0.7, 0.1), 0.2, 0.055, True),
         ((0.15, 0.04, -0.05), (0.55, 0.55, -0.5), 0.19, 0.055, False),
         ((0.13, -0.02, -0.11), (0.45, 0.25, -0.85), 0.19, 0.052, True),
         ((0.09, -0.06, -0.15), (0.3, -0.15, -0.95), 0.18, 0.05, True),
         ((0.1, 0.11, 0.06), (0.45, 0.85, 0.3), 0.18, 0.05, True),
         ((0.11, 0.12, -0.04), (0.5, 0.8, -0.25), 0.17, 0.048, False)]


def lock_frame(i):
    off, d, ln, r, _ = LOCKS[i]
    d = unit(*d)
    root = NECK + np.array(off)
    out = unit(off[0], off[1] * 0.3, off[2] * 0.3)        # the lock's face looks outward from the neck
    flat = out - d * np.dot(out, d); flat /= np.linalg.norm(flat)
    return root, d, ln * 1.3, r * 1.2, flat


def add_locks():
    for i, (off, d, ln, r, streak) in enumerate(LOCKS):
        root, d, ln, r, flat = lock_frame(i)
        pts = [root, root + d * ln * 0.45, root + d * ln * 0.8, root + d * ln]
        k.add(f"lock{i}", blade(pts, [r, r * 0.85, r * 0.45, 0.004], flat, 0.42), GREY, "body", mirror=True,
              surface="hair")


def add_streaks():
    for i, (off, d0, ln0, r0, streak) in enumerate(LOCKS):
        if not streak:
            continue
        root, d, ln, r, flat = lock_frame(i)

        def mask(P, root=root, d=d, ln=ln, flat=flat):
            Q = P.copy(); Q[:, 0] = np.abs(Q[:, 0])
            rel = Q - root
            t = rel @ d / ln
            face = rel @ flat
            across = rel - np.outer(rel @ d, d) - np.outer(face, flat)
            w = np.linalg.norm(across, axis=1)
            return (inside(w - 0.015, 0.002) * inside(np.abs(t - 0.5) - 0.24, 0.02) * (face > 0.0))
        k.paint(mask, RED_O, on=[f"lock{i}"])


# --- a deeper trunk: a low belly, and the shoulders and thighs reaching further down, so the legs are no stilts ------
def add_depth():
    k.add("middlelow", ell((0, 0.02, 0.34), (0.11, 0.2, 0.09)), RED_O, "body")
    k.add("shoulderlow", ell((a.LEG_X, a.FORE_Y + 0.005, 0.3), (0.074, 0.08, 0.11)), RED_O, "leg{s}", mirror=True)
    k.add("thighlow", ell((a.LEG_X, a.HIND_Y + 0.01, 0.3), (0.08, 0.09, 0.12)), RED_O, "hind{s}", mirror=True)
    # The mane's chest hanging lower, between the forelegs
    k.add("manelow", ell((0, -0.26, 0.37), (0.13, 0.09, 0.1), deform=lumpy(5, 3, 0.07)), GREY, "body", surface="hair")


# The flank stripes behind the locks only: a patch cast from the side in front of them lands on the locks
FLANK = [s for s in a.FLANK if s[0][1] > 0.1]


# --- grey cloud cuffs round the hind ankles ----------------------------------------------------------------------------
def add_cuffs():
    k.add("cuff", ell((a.LEG_X + 0.005, a.HIND_Y + 0.01, 0.1), (0.078, 0.085, 0.06), deform=lumpy(5, 2, 0.08)), GREY,
          "hind{s}", mirror=True, surface="hair")


# --- the tail: a huge grey cloud rising from the rump and sweeping back to a curling point, swung out to +X ----------
TAIL = [(0.0, 0.29, 0.54), (0.04, 0.37, 0.62), (0.09, 0.45, 0.69), (0.14, 0.53, 0.72), (0.18, 0.6, 0.71),
        (0.2, 0.66, 0.76), (0.21, 0.68, 0.84)]
TAIL_R = [0.05, 0.11, 0.14, 0.14, 0.11, 0.06, 0.012]


def add_tail():
    side = unit(1, -0.6, 0)
    k.add("tail", blade(TAIL, TAIL_R, side, 0.75), GREY, "tail", surface="hair")
    k.add("tailroot", ell(TAIL[0], (0.055, 0.05, 0.05)), RED_O, "tail")


def add_tail_swirl():
    c = np.array([0.24, 0.48, 0.7])
    n = unit(1, -0.55, 0.15)
    cs, cg = srgb(SWIRL), srgb(GREY)

    def look(u, v):
        col = np.tile(cg, (len(u), 1))
        r = np.hypot(u, v); th = np.arctan2(v, u)
        turn = (th % (2 * math.pi)) / (2 * math.pi)
        best = np.full(len(u), 9.0)
        for m in range(2):
            best = np.minimum(best, np.abs(r - (0.12 + 0.5 * (turn + m))))
        col[(best < 0.08) & (r < 0.92)] = cs
        return col
    k.decal(c, n, (0.07, 0.07), look, eps=0.003, rings=20, segs=72)


if __name__ == "__main__":
    a.add_body(RED_O)
    a.add_legs(RED_O)
    add_depth()
    add_head()
    a.add_band(RED_O)
    a.add_ears(color=RED_O, inner=EAR_IN)
    a.add_mane(GREY)
    add_locks()
    add_streaks()
    add_cuffs()
    add_tail()
    a.add_leg_stripes(zs=(0.08, 0.15), color=STRIPE)
    a.add_flank_stripes(FLANK, color=STRIPE)
    add_tail_swirl()
    add_horns()
    add_fangs()
    add_nose()

    k.build(voxel=0.0085, band=0.004)
    k.eye_states(a.eyes)
    k.export(a.out)
