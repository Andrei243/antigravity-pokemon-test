# Baxcalibur sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/baxcalibur.py -- <the export's Baxcalibur.glb> <out.glb>
#
# Arctibax grown (frigibax.py's yellow eye cut by a brow, its pale claws, its shards and hexagons of ice), from the
# references: a great dark navy dragon standing like a film monster, one pear of a body from wide hips up a long thick
# neck to a head carried forward. The face is masked in pale ice: a crest from the snout's tip back over the crown,
# the snout cased in ice, a jagged beard of ice hanging under the jaw, a small yellow eye under the crest. A column of
# pale hexagonal plates of ice down the chest and belly, a wider grey one at the groin, a plate on each knee. Strong
# arms forward and down, three pale claws and three red quills at each wrist. Thick legs, wide feet of pale claws on
# pale soles, a heavy tail curling up behind. On the back the great blade of ice rising like an axe's edge: a black
# plate in a pale frame, jagged at its edges, a white zigzag across its lower half.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, slab, srgb, inside, unit, args, sibling

fb = sibling("frigibax")
bg = fb.bg

NAVY = "#2F4A6B"
ICE = "#CBE6F1"
GREYPLATE = "#A7B3C0"
BLACK = "#1B1D25"
RED = "#D6383A"
CLAW, SOLE = fb.CLAW, "#B9D9EA"

HC, HR = np.array([0.0, -0.11, 0.925]), np.array([0.098, 0.115, 0.092])     # the skull
TC, TR = np.array([0.0, 0.0, 0.42]), np.array([0.235, 0.2, 0.26])          # the trunk
CC, CR = np.array([0.0, -0.02, 0.62]), np.array([0.18, 0.165, 0.17])       # the chest


def shrink(O, f, about=None):
    O = np.array(O, float)
    m = O.mean(axis=0) if about is None else np.array(about, float)
    return [tuple(m + (p - m) * f) for p in O]


def baxcalibur(src, out):
    k = Kit(src, coat="scales")

    # --- the body: a pear from wide hips up a long thick neck ---------------------------------------------------------
    k.add("trunk", ell(TC, TR), NAVY, "body")
    k.add("chest", ell(CC, CR), NAVY, "body")
    k.add("neck", tube([(0, 0.0, 0.66), (0, -0.04, 0.79), (0, -0.08, 0.88)], [0.135, 0.1, 0.085]), NAVY, "head")

    # --- the head carried forward, masked in ice ----------------------------------------------------------------------
    k.add("skull", ell(HC, HR), NAVY, "head")
    k.add("snout", ell((0, -0.215, 0.89), (0.078, 0.1, 0.064), rot=(10, 0, 0)), ICE, "head", surface="shell")
    k.add("jaw", ell((0, -0.19, 0.845), (0.07, 0.085, 0.035), rot=(14, 0, 0)), ICE, "head", surface="shell")
    # the crest: from the snout's tip back over the crown, its wings over the eyes
    k.add("crest", hull([(0, -0.33, 0.925), (0, -0.31, 0.9), (0.075, -0.2, 0.955), (-0.075, -0.2, 0.955),
                         (0.08, -0.22, 0.92), (-0.08, -0.22, 0.92), (0.06, -0.08, 1.0), (-0.06, -0.08, 1.0),
                         (0, -0.03, 1.03), (0, -0.14, 1.02), (0.035, -0.12, 0.93), (-0.035, -0.12, 0.93)]),
          ICE, "head", fuse=False, surface="shell")
    # two spikes of ice standing on the snout
    for s in (1, -1):
        k.add(f"nosespike{'R' if s > 0 else 'L'}", fb.shard((s * 0.035, -0.25, 0.93), (s * 0.25, -0.1, 1), 0.07, 0.04,
                                                         0.022, across=(0, 1, 0)), ICE, "head", fuse=False,
              surface="shell")
    # the beard: jagged shards of ice hanging under the jaw
    beard = [(0.0, -0.25, 0.835, 0.1), (0.045, -0.23, 0.835, 0.09), (-0.045, -0.23, 0.835, 0.09),
             (0.065, -0.17, 0.84, 0.075), (-0.065, -0.17, 0.84, 0.075), (0.0, -0.17, 0.83, 0.08)]
    for i, (x, y, z, ln) in enumerate(beard):
        k.add(f"beard{i}", fb.shard((x, y, z), (x * 2, 0.15, -1), ln, 0.05, 0.022, across=(0, 1, 0)), ICE, "head",
              fuse=False, surface="shell")

    # --- the arms: strong, forward and down, three pale claws and three red quills at each wrist --------------------
    k.add("arm", tube([(0.16, -0.02, 0.68), (0.235, -0.1, 0.6), (0.22, -0.235, 0.53)], [0.062, 0.052, 0.046]), NAVY,
          "arm{s}", mirror=True)
    k.add("hand", ell((0.215, -0.275, 0.505), (0.05, 0.052, 0.05)), NAVY, "arm{s}", mirror=True)
    for s in (1, -1):
        sd = "R" if s > 0 else "L"
        for j, dx in enumerate((-0.028, 0.0, 0.028)):
            k.add(f"hclaw{sd}{j}", fb.claw((s * (0.215 + dx), -0.3, 0.47), (s * dx * 3, -0.45, -0.9), 0.07, 0.034,
                                           up=(0, 1, 0)), CLAW, f"arm{sd}", fuse=False, surface="shell")
        for j, (d, ln) in enumerate([((0.35, 0.35, -0.85), 0.13), ((0.45, 0.6, -0.55), 0.12), ((0.4, 0.8, -0.2), 0.1)]):
            k.add(f"quill{sd}{j}", fb.shard((s * 0.245, -0.2, 0.56), (d[0] * s, d[1], d[2]), ln, 0.055, 0.026,
                                            across=(0, 0, 1) if j == 2 else (0, 1, 0)), RED, f"arm{sd}", fuse=False,
                  surface="shell")

    # --- the legs: thick, fused into the hips, wide feet of pale claws on pale soles ---------------------------------
    k.add("thigh", ell((0.145, 0.0, 0.26), (0.125, 0.15, 0.165)), NAVY, "leg{s}", mirror=True)
    k.add("shin", tube([(0.155, 0.02, 0.14), (0.16, 0.0, 0.09)], [0.085, 0.075]), NAVY, "leg{s}", mirror=True)
    k.add("foot", ell((0.16, -0.05, 0.038), (0.09, 0.125, 0.042)), NAVY, "leg{s}", mirror=True)
    for s in (1, -1):
        sd = "R" if s > 0 else "L"
        for j, dx in enumerate((-0.05, -0.017, 0.017, 0.05)):
            k.add(f"toe{sd}{j}", fb.claw((s * (0.16 + dx), -0.16, 0.042), (s * dx * 4, -1, -0.25), 0.06, 0.038),
                  CLAW, f"leg{sd}", fuse=False, surface="shell")
    k.paint(lambda P: inside(P[:, 2] - 0.012, 0.004), SOLE, on=["foot"])

    # --- the tail: heavy, out behind and curling up at its end, a small fin on it --------------------------------------
    k.add("tail", tube([(0, 0.14, 0.3), (0, 0.3, 0.2), (0.05, 0.43, 0.17), (0.1, 0.52, 0.23)],
                       [0.13, 0.085, 0.05, 0.022]), NAVY, "tail")
    k.add("tailfin", fb.shard((0.05, 0.42, 0.21), (0.3, 0.5, 1), 0.1, 0.07, 0.03, across=(1, 0, 0)), NAVY, "tail",
          fuse=False, surface="scales")

    # --- the blade on the back: a black plate in a pale jagged frame, banked a little to +X --------------------------
    ez = unit(math.sin(math.radians(16)), 0, math.cos(math.radians(16)))
    FRAME = [(0.08, 0.6), (0.06, 0.75), (0.03, 0.8), (0.07, 0.84), (0.06, 0.95), (0.1, 0.92), (0.14, 1.0),
             (0.2, 0.95), (0.27, 0.99), (0.3, 0.92), (0.37, 0.93), (0.36, 0.84), (0.41, 0.78), (0.37, 0.72),
             (0.4, 0.62), (0.35, 0.58), (0.36, 0.5), (0.27, 0.46), (0.2, 0.42), (0.12, 0.48)]
    k.add("blade", slab(FRAME, (0, 0, 0), (0, 1, 0), ez, 0.034), ICE, "body", fuse=False, surface="shell")
    BLK = [(0.12, 0.62), (0.11, 0.85), (0.3, 0.88), (0.33, 0.58), (0.25, 0.53)]
    k.add("blackplate", slab(BLK, (0, 0, 0), (0, 1, 0), ez, 0.046), BLACK, "body", fuse=False, surface="shell")
    ZIG = [(0.13, 0.66), (0.19, 0.72), (0.2, 0.64), (0.27, 0.71), (0.29, 0.62), (0.32, 0.66), (0.32, 0.6),
           (0.29, 0.57), (0.27, 0.63), (0.21, 0.56), (0.19, 0.62), (0.13, 0.6)]
    k.add("zigzag", slab(ZIG, (0, 0, 0), (0, 1, 0), ez, 0.056), ICE, "body", fuse=False, surface="shell")
    k.add("bladeroot", tube([(0, 0.06, 0.78), (0, 0.15, 0.62), (0, 0.17, 0.5)], [0.06, 0.07, 0.06]), NAVY, "body")

    # --- the hexagonal plates of ice: a column down the chest and belly, a grey one at the groin, one on each knee ---
    for z, half, col in ((0.735, (0.055, 0.05), ICE), (0.62, (0.075, 0.062), ICE), (0.48, (0.095, 0.075), ICE),
                         (0.335, (0.12, 0.05), GREYPLATE)):
        c, r = (CC, CR) if z > 0.55 else (TC, TR)
        dz = (z - c[2]) / r[2]
        y = c[1] - r[1] * math.sqrt(max(0.05, 1 - dz * dz))
        n = unit(0, -1, 0.45 * dz)
        k.decal((0, y, z), n, half, col, up=(0, 0, 1), eps=0.004, outline=fb.hexagon(1.0), rings=10, segs=60)
    for s in (1, -1):
        d = unit(0.3 * s, -1, -0.15)
        k.decal(bg.on_ell((0.145 * s, 0.0, 0.26), (0.125, 0.15, 0.165), d), d, (0.05, 0.045), ICE, eps=0.004,
                outline=fb.hexagon(1.0), rings=10, segs=60)

    k.build(voxel=0.008, smooth=3, band=0.008, wband=0.07)

    # --- the eyes: small, yellow, cut by the crest --------------------------------------------------------------------
    cut, look = fb.yellow_eye(BA=0.3, BB=0.45, pupil_c=(-0.2, -0.08), pupil_r=0.48)
    de = unit(0.8, -0.5, 0.25)
    E = bg.on_ell(HC, HR, de)
    eyes = fb.eyes_under_brow(k, E, de, (0.034, 0.03), look, cut, srgb(NAVY), eps=0.004)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    baxcalibur(*args())
