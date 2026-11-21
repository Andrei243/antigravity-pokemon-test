# Zeraora-Mega sculpted on the skeleton and clips of the game's own export of the form (sculptkit.py):
#   blender --background --factory-startup --python sculpts/zeraoramega.py -- <the export's Zeraora-Mega.glb> <out.glb>
# Grown from zeraora.py (sibling("zeraora")): its head, face, eyes, ruff, arms and fists are the base's own functions,
# called with the base's colour names set to the Mega's; its legs and tail are written here.
#
# From the references (10319): Zeraora gone dark. Its masses, largest first: (1) TWO long tails from the small of the
# back, rising up and back in a V behind the shoulders, dark, each ending in a broad jagged yellow plume streaked black,
# their tips well over the head; (2) the coat charcoal where the base is yellow (head, ruff, forearms and fists), the
# chest and limbs a lighter charcoal; (3) electric blue for the base's blacks of the lower limbs: the lower thighs and
# knees light blue under a zigzag edged in yellow, the claws blue, blue shards at the elbows and shoulders, two blue
# chevrons down the chest; (4) a yellow crest of lightning on the crown and yellow streaks under the eyes. The blue
# crescents beside the head, the ears with orange insides, the fierce blue eyes and the open mouth are the base's.
# The game's sculptor (PokemonModels.Alola3.cs) streams yellow fronds from the head and has one tail: the pictures
# overrule it (two tails, the yellow at their ends and a short crest on the crown).
# The two tails are both on the one tail bone. Bones as the base's: body (0, 0, 0.4), head (0, -0.03, 0.52),
# arms (+-0.07, 0, 0.48), legs (+-0.05, 0.01, 0.3), tail (0, 0.05, 0.32).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, hull, slab, srgb, inside, tri, unit, sibling, args

Z = sibling("zeraora")
spike = Z.spike

COAT, LIMB, FACE = "#2F3039", "#464852", "#4E505B"
BLUE, YELLOW, GOLD_DK = "#55C4F2", "#F4D42A", "#2A2A30"

# the base's colour names, read by its functions as they run
Z.YELLOW, Z.BLACK, Z.FACE, Z.WHITE, Z.BLUE = COAT, LIMB, FACE, BLUE, BLUE


def crown(k):
    """The yellow crest of lightning on the crown, laid back, and the yellow streaks under the eyes."""
    for i, (x, d, ln) in enumerate(((0.0, (0, 0.35, 0.94), 0.075), (0.028, (0.35, 0.4, 0.85), 0.06),
                                    (-0.028, (-0.35, 0.4, 0.85), 0.06), (0.0, (0.0, 0.85, 0.5), 0.065))):
        k.add(f"crest{i}", spike((x, -0.045 + 0.02 * (i == 3), 0.672), d, ln, 0.036, 0.024), YELLOW, "head", fuse=False)
    # a black stripe up the middle spike: a thin dark hull over it
    k.add("creststripe", spike((0.0, -0.05, 0.676), (0, 0.33, 0.94), 0.06, 0.012, 0.03), GOLD_DK, "head", fuse=False)
    for s in (1, -1):
        bolt = [(0.0, 0.006), (0.024, 0.01), (0.02, 0.002), (0.044, 0.004), (0.02, -0.01), (0.022, -0.002),
                (0.0, -0.006)]
        o = np.array([0.05 * s, -0.098, 0.585])
        ex = unit(0.75 * s, 0.45, -0.25)
        k.add(f"streak{'R' if s > 0 else 'L'}", slab(bolt, o, ex, (0, 0, 1), 0.012), YELLOW, "head", fuse=False,
              surface="shell")


def blue_parts(k):
    # a second blue chevron lower on the chest
    for side in (1, -1):
        poly = [(side * u, v) for u, v in ((-1, -0.6), (-1, -0.1), (1, 0.65), (1, 0.15))]
        if side < 0:
            poly = poly[::-1]
        k.decal((0.022 * side, -0.06, 0.44), unit(0.3 * side, -1, 0.0), (0.026, 0.024), BLUE, eps=0.003,
                outline=Z.polygon(poly), rings=8, segs=48)
    # blue shards at the shoulders and the elbows, pointing back and out
    for s in (1, -1):
        m = np.array([s, 1, 1])
        k.add(f"shard{'R' if s > 0 else 'L'}0", spike(Z.SHOULDER * m + (0.02 * s, 0.01, 0.025), (0.7 * s, 0.3, 0.65),
                                                      0.05, 0.03, 0.022), BLUE, f"arm{'R' if s > 0 else 'L'}", fuse=False)
        k.add(f"shard{'R' if s > 0 else 'L'}1", spike(Z.ELBOW * m + (0.02 * s, 0.02, 0.0), (0.75 * s, 0.6, 0.1),
                                                      0.055, 0.032, 0.022), BLUE, f"arm{'R' if s > 0 else 'L'}", fuse=False)


def legs(k):
    """The thighs charcoal over light blue, the edge a zigzag trimmed yellow; shins and feet dark; claws blue."""
    k.add("thigh", ell((0.064, -0.014, 0.258), (0.062, 0.07, 0.102)), BLUE, "leg{s}", mirror=True)
    k.add("shin", tube([(0.07, -0.02, 0.2), (0.074, -0.004, 0.12), (0.072, 0.008, 0.055)], [0.04, 0.035, 0.031]),
          LIMB, "leg{s}", mirror=True)
    k.add("foot", ell((0.072, -0.03, 0.026), (0.036, 0.058, 0.026)), COAT, "leg{s}", mirror=True)
    for i, (x, d) in enumerate(((0.06, (-0.3, -0.5, -0.8)), (0.09, (0.4, -0.5, -0.8)), (0.075, (0, -0.7, -0.7)))):
        k.add(f"knee{i}", spike((x, -0.066, 0.192), d, 0.045, 0.034, 0.024), BLUE, "leg{s}", fuse=False, mirror=True)
    for i, t in enumerate((-1, 0, 1)):
        k.add(f"toe{i}", spike((0.072 + 0.016 * t, -0.08, 0.016), (0.1 * t, -1, -0.35), 0.026, 0.02, 0.021), BLUE,
              "leg{s}", fuse=False, mirror=True, surface="shell")

    def edge(P):
        x = np.abs(P[:, 0]) - 0.064
        a = np.arctan2(x, -(P[:, 1] + 0.014))
        return 0.232 + 0.02 * tri(a * 1.6 / math.pi)
    k.paint(lambda P: inside(edge(P) - 0.012 - P[:, 2], 0.002) * inside(P[:, 2] - 0.29, 0.004), YELLOW, on=["thigh"])
    k.paint(lambda P: inside(edge(P) - P[:, 2], 0.002), COAT, on=["thigh"])


def tails(k):
    """Two tails up and back in a V, each ending in a broad jagged yellow plume."""
    for s in (1, -1):
        side = "R" if s > 0 else "L"
        m = np.array([s, 1, 1.0])
        path = np.array([(0.01, 0.05, 0.34), (0.05, 0.14, 0.4), (0.1, 0.22, 0.48), (0.15, 0.27, 0.55),
                         (0.19, 0.3, 0.6)]) * m
        k.add(f"tail{side}", tube(path, [0.03, 0.026, 0.022, 0.02, 0.02]), COAT, "tail")
        plume = np.array([(0.19, 0.3, 0.59), (0.24, 0.33, 0.65), (0.29, 0.35, 0.68), (0.33, 0.36, 0.68),
                          (0.36, 0.365, 0.655)]) * m
        k.add(f"plume{side}", blade(plume, [0.026, 0.05, 0.056, 0.04, 0.012], unit(0.25 * s, -0.95, 0.2), 0.4), YELLOW,
              "tail")
        # the jagged notches: spikes off the plume's outer and lower edge, yellow and black in turn
        for i, (t, d, ln, col) in enumerate(((0.3, (0.35, 0.2, -0.9), 0.06, YELLOW), (0.55, (0.5, 0.2, -0.85), 0.065, GOLD_DK),
                                             (0.8, (0.7, 0.1, -0.7), 0.055, YELLOW), (0.5, (0.4, 0.2, 0.9), 0.05, YELLOW))):
            j = t * (len(plume) - 1)
            b = plume[int(j)] + (plume[min(int(j) + 1, len(plume) - 1)] - plume[int(j)]) * (j - int(j))
            k.add(f"notch{side}{i}", spike(b, (d[0] * s, d[1], d[2]), ln, 0.034, 0.02), col, "tail", fuse=False)


def zeraoramega(src, out):
    k = Kit(src, coat="fur")
    Z.add_head(k)
    crown(k)
    Z.add_body(k)
    Z.add_arms(k)
    blue_parts(k)
    legs(k)
    tails(k)
    k.build(voxel=0.0062, smooth=4, band=0.006, wband=0.05)
    k.eye_states(lambda s: Z.eyes(k, s))
    k.export(out)


if __name__ == "__main__":
    zeraoramega(*args())
