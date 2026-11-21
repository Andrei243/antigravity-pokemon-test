# Growlithe-Hisui sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/growlithehisui.py -- <the export's Growlithe-Hisui.glb> <out.glb>
# Grown from growlithe.py through `sibling` (its trunk, legs, paws, claws, leg chevrons, flank stripes and the way its
# ears are made and painted), from the references: a red-orange puppy with black stripes over its back and haunches;
# a great white mop of cloud-like fur over the whole head that hangs down over the eyes (no eye shows), a short grey
# rock horn standing out of its crown, round ears poking out of the mop's sides, red-orange with pink insides; a tan
# muzzle with a black nose pushing out from under the fringe and a mouth wide open (dark red inside, a pink tongue on
# the tan lower jaw, two small white fangs); round white puffs of fur over the chest and the forelegs down to the
# paws; a white tail curling up and over like a cloud's swirl; small white claws.
#
# growlithe.py makes its Kit (k) on the export it is given as it is read, so this script uses that one: one Kit only.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import ell, tube, blade, hull, outward, srgb, inside, unit, eye_look, lumpy, sibling

g = sibling("growlithe")
k = g.k

# --- colours ----------------------------------------------------------------------------------------------------------
RED_O = "#D8552E"          # the coat: a redder orange than the base's
MOP = "#E8E4DE"           # the mop, the puffs and the tail: white with a grey tinge
MUZZLE = "#E8D2AE"         # the muzzle, the lower jaw, the belly
EAR_IN = "#F2A6A0"
HORN = "#A9A49C"
MOUTH = "#9A2A34"
TONGUE = "#E57D88"
FANG = "#F7F5F0"
SWIRL = "#C9C3BB"
STRIPE, CLAW, NOSE = g.STRIPE, g.CLAW, g.NOSE

H = g.H                    # (0, -0.17, 0.405): the head's centre, as the base's
HR = g.HR


# --- the head under its mop --------------------------------------------------------------------------------------------
def add_head():
    k.add("head", ell(H, HR * 0.97, deform=g.head_shape), RED_O, "head")
    k.add("neck", tube([(0, -0.07, 0.28), (0, -0.11, 0.33), (0, -0.14, 0.37)], [0.07, 0.068, 0.066]), RED_O, "head")
    # The upper jaw: a tan snout pushing out from under the fringe
    k.add("muzzle", ell(H + (0, -0.105, -0.028), (0.054, 0.058, 0.036)), MUZZLE, "head")
    # The mouth wide open: a dark red mass set back between the snout and the dropped lower jaw
    k.add("mouthin", ell(H + (0, -0.078, -0.07), (0.044, 0.042, 0.03)), MOUTH, "head")
    k.add("jaw", ell(H + (0, -0.088, -0.1), (0.048, 0.046, 0.021), rot=(-12, 0, 0)), MUZZLE, "head")
    k.add("tongue", ell(H + (0, -0.097, -0.086), (0.03, 0.03, 0.012), rot=(-12, 0, 0)), TONGUE, "head")


def add_fangs():
    """Two small white fangs hanging from the snout's lower edge at the front corners of the open mouth."""
    for s in (1, -1):
        a = H + np.array((0.024 * s, -0.142, -0.06))
        pts = [a + (-0.007, 0.004, 0.0), a + (0.007, 0.004, 0.0), a + (0, 0.012, 0.0), a + (0, -0.002, 0.0),
               a + (0.0, 0.004, -0.02)]
        k.add(f"fang{'R' if s > 0 else 'L'}", hull(pts), FANG, "head", fuse=False, surface="shell")


def add_nose():
    k.add("nose", ell(H + (0, -0.16, -0.006), (0.023, 0.018, 0.017), rot=(15, 0, 0)), NOSE, "head", fuse=False,
          surface="shell")


# --- the mop: a white cloud of rounded lobes over the head, the fringe hanging over the eyes to the snout ------------
MOP_PUFFS = [
    # centre (from H), radii
    ((0.0, 0.01, 0.05), (0.14, 0.13, 0.108)),           # the crown
    ((0.0, 0.06, 0.0), (0.135, 0.11, 0.11)),            # the back of the head
    ((0.0, -0.1, 0.035), (0.066, 0.058, 0.062)),        # the fringe, middle
    ((0.065, -0.088, 0.03), (0.064, 0.058, 0.064)),     # the fringe, sides
    ((-0.065, -0.088, 0.03), (0.064, 0.058, 0.064)),
    ((0.105, -0.06, -0.035), (0.058, 0.058, 0.062)),    # the cheeks, beside the open mouth
    ((-0.105, -0.06, -0.035), (0.058, 0.058, 0.062)),
    ((0.12, 0.0, 0.0), (0.06, 0.08, 0.075)),            # over the sides of the head, under the ears
    ((-0.12, 0.0, 0.0), (0.06, 0.08, 0.075)),
]


def add_mop():
    for i, (c, r) in enumerate(MOP_PUFFS):
        k.add(f"mop{i}", ell(H + np.array(c), r, deform=lumpy(4, 2, 0.05)), MOP, "head", surface="hair")


def add_horn():
    """A short grey rock horn standing out of the crown: a flat-faced cone, a loose piece."""
    top = H + np.array((0, 0.0, 0.155))
    base = [top + (0.036 * math.cos(a), 0.036 * math.sin(a), -0.03) for a in np.linspace(0, 2 * math.pi, 7)[:-1]]
    k.add("horn", hull(base + [top + (0, -0.008, 0.05)]), HORN, "head", fuse=False, surface="shell")


# --- the ears: round, red-orange with pink insides, poking out of the mop's sides --------------------------------------
EC = np.array([0.158, -0.16, 0.452])


def add_ears():
    k.add("ear", ell(EC, (0.046, 0.02, 0.044), rot=(8, 0, -38)), RED_O, "ear{s}", mirror=True)
    n = unit(0.45, -0.89, 0.1)

    def lining(P):
        Q = P.copy(); Q[:, 0] = np.abs(Q[:, 0])
        d = Q - EC
        front = -(d @ n)
        flat = d - np.outer(d @ n, n)
        return (inside(np.linalg.norm(flat, axis=1) - 0.029, 0.0025) * inside(-front + 0.004, 0.002)
                * (np.abs(P[:, 0]) > 0.135))
    k.paint(lining, EAR_IN, on=["ear"])


# --- the chest and the forelegs under round white puffs ----------------------------------------------------------------
PUFFS = [((0.0, -0.155, 0.235), (0.095, 0.072, 0.075)),        # the chest
         ((0.066, -0.14, 0.14), (0.066, 0.072, 0.066)),      # a round puff over each foreleg, above the paw
         ((-0.066, -0.14, 0.14), (0.066, 0.072, 0.066))]


def add_puffs():
    for i, (c, r) in enumerate(PUFFS):
        bone = "body" if i < 1 else ("legR" if c[0] > 0 else "legL")
        k.add(f"puff{i}", ell(c, r, deform=lumpy(4, 2, 0.04)), MOP, bone, surface="hair")


# --- the tail: a white cloud rising from the rump and curling over to the front at its top, swung out to +X ----------
TAIL = [(0.0, 0.17, 0.29), (0.025, 0.215, 0.33), (0.06, 0.245, 0.4), (0.085, 0.24, 0.47), (0.095, 0.2, 0.51),
        (0.095, 0.16, 0.49)]
TAIL_R = [0.035, 0.068, 0.08, 0.075, 0.058, 0.03]


def add_tail():
    side = unit(1, -0.6, 0)
    k.add("tail", blade(TAIL, TAIL_R, side, 0.75), MOP, "tail", surface="hair")
    k.add("tailroot", ell(TAIL[0], (0.04, 0.038, 0.038)), RED_O, "tail")


def add_tail_swirl():
    """A grey curl on the tail's outer face: a spiral line in a patch's colour function."""
    c = np.array([0.15, 0.225, 0.45])
    n = unit(1, -0.55, 0.1)
    cs, cw = srgb(SWIRL), srgb(MOP)

    def look(u, v):
        col = np.tile(cw, (len(u), 1))
        r = np.hypot(u, v); th = np.arctan2(v, u)
        # r = a + b * turn: one and a half turns out from the middle
        turn = (th % (2 * math.pi)) / (2 * math.pi)
        best = np.full(len(u), 9.0)
        for m in range(2):
            best = np.minimum(best, np.abs(r - (0.12 + 0.5 * (turn + m))))
        col[(best < 0.09) & (r < 0.92)] = cs
        return col
    k.decal(c, n, (0.045, 0.045), look, up=(0, 0, 1), eps=0.003, rings=20, segs=72)


# --- the belly: tan under the trunk ------------------------------------------------------------------------------------
def add_belly():
    # A low belly between the legs, so that little daylight shows under the trunk
    k.add("middlelow", ell((0, 0.02, 0.19), (0.07, 0.13, 0.06)), RED_O, "body")
    k.paint(lambda P: inside(P[:, 2] - 0.185, 0.004) * inside(np.abs(P[:, 0]) - 0.07, 0.004)
            * (P[:, 1] > -0.1) * (P[:, 1] < 0.2) * (P[:, 2] > 0.12), MUZZLE, on=["chest", "middle", "rump"])


# --- stripes: black chevrons round the hind legs, and long stripes down the flanks from near the spine -------------------
FLANK = [((0.0, 0.035, 0.295), 0.016, 0.07, -0.25),
         ((0.0, 0.1, 0.295), 0.016, 0.07, -0.3)]  # none nearer the chest or the tail: their patches landed on the puffs


def add_stripes():
    for j, z in enumerate((0.085, 0.165)):
        k.add(f"stripeback{j}", g.chevron((g.LEG_X, g.HIND_Y, 0), z, lambda zz: g.leg_r(zz) - 0.003), STRIPE,
              "hind{s}", fuse=False, mirror=True, surface="fur")
    g.add_flank_stripes(FLANK, STRIPE)


# --- the eyes: hidden under the fringe, so each state is a patch of the mop's own colour laid on it -------------------
EYE_DIR = unit(0.5, -0.84, 0.1)


def eyes(state):
    cm = srgb(MOP)
    look = eye_look(state, lambda u, v: np.tile(cm, (len(u), 1)), cm, cm)
    for side in (1, -1):
        c = H + np.array((0.05 * side, -0.15, 0.03))
        k.decal(c, EYE_DIR * np.array([side, 1, 1.0]), (0.018, 0.016), look, eps=0.003, rings=20, segs=56)


if __name__ == "__main__":
    g.add_body(RED_O)
    g.add_legs(RED_O)
    add_head()
    add_mop()
    add_ears()
    add_puffs()
    add_tail()
    add_belly()
    g.add_claws(CLAW)
    add_stripes()
    add_tail_swirl()
    add_horn()
    add_fangs()
    add_nose()

    k.build(voxel=0.006, band=0.004)
    k.eye_states(eyes)
    k.export(g.out)
