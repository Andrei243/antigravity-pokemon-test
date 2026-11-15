# Nidorino sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/nidorino.py -- <the export's Nidorino.glb> <out.glb>
# Grown from nidoranm.py (its colours, its ears with their teal lining and notched edges, its horn, its spines as
# solid blades, its claws, fangs and spots, its scowling eye), by its functions. From the references: Nidoran
# stood up on longer legs, the head carried low and forward; the ears larger and more upright; three tall spines on
# the back behind the ears and smaller ones down to the rump; a longer horn; the eyes white with a dark pupil under
# the scowl (not red); two white fangs at the corners of the mouth; many long dark spots on the body and the legs.
import os, sys
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from types import SimpleNamespace
import numpy as np
from sculptkit import Kit, ell, tube, blade, unit, sibling, args

N = sibling("nidoranm")
LILAC, SPOT = N.LILAC, N.SPOT

L = SimpleNamespace(
    H=np.array([0.0, -0.265, 0.265]),                  # the head bone is at 0, -0.125, 0.337: the head carried low
    HR=np.array([0.118, 0.118, 0.108]),
    ear_base=(0.068, -0.2, 0.34), ear_d=(0.4, 0.42, 0.82), ear_n=(0.6, -1.0, 0.05),
    ear_len=0.35, ear_w=0.215, ear_t=0.036, ear_point=1.4, notches=3, notch=0.45,
    lining_at=0.48, lining_w=0.76, lining_h=0.37,
    horn_base=(0, -0.33, 0.345), horn_d=(0, -0.82, 0.57), horn_len=0.13, horn_r=0.036,
    iris=N.SCLERA, pupil="#1A0C10", pupil_w=0.42, brow=0.28, brow_slope=0.95,
    eye_c=None, eye_n=None, eye_h=(0.04, 0.036), eye_up=(0, 0.15, 1),
)
L.eye_n = unit(0.6, -0.78, 0.14)
L.eye_c = L.H + np.array([0.0, -0.015, 0.0]) + L.eye_n * 0.112


def nidorino(src, out):
    k = Kit(src, coat="fur")
    H, HR = L.H, L.HR

    # The body: longer than Nidoran's and carried higher on its legs, fuller behind
    def pear(V):
        V = V.copy()
        V[:, 0] *= 1 + 0.1 * V[:, 1]; V[:, 2] *= 1 + 0.08 * V[:, 1]
        return V
    k.add("torso", ell((0, 0.05, 0.255), (0.15, 0.265, 0.135), deform=pear), LILAC, "body")
    k.add("tail", ell((0, 0.32, 0.27), (0.026, 0.035, 0.026)), LILAC, "tail")
    k.add("neck", ell((0, -0.15, 0.27), (0.11, 0.11, 0.115)), LILAC, "head")

    # The head, low and forward; the snout turned a little down
    k.add("head", ell(H, HR), LILAC, "head")
    k.add("snout", ell(H + (0, -0.1, -0.045), (0.062, 0.09, 0.056), rot=(14, 0, 0)), LILAC, "head")
    k.add("brow", ell(H + (0.055, -0.065, 0.04), (0.045, 0.055, 0.022), rot=(-15, 0, -12)), LILAC, "head", mirror=True)
    k.add("cheek", ell(H + (0.075, -0.02, -0.035), (0.058, 0.058, 0.058)), LILAC, "head", mirror=True)
    # a short tuft of fur before each ear
    for j, (a, b) in enumerate([((0.08, -0.25, 0.33), (0.13, -0.285, 0.39)), ((0.095, -0.24, 0.32), (0.155, -0.26, 0.355))]):
        k.add(f"tuft{j}", blade([a, b], [0.02, 0.004], (0, 1, 0), 0.6), LILAC, "head", mirror=True)

    # The legs: longer, the shoulders and thighs broad masses over their tops, the shins tapering to stout paws
    k.add("shoulder", ell((0.11, -0.12, 0.22), (0.075, 0.09, 0.11)), LILAC, "leg{s}", mirror=True)
    k.add("leg", tube([(0.118, -0.13, 0.19), (0.122, -0.155, 0.1), (0.12, -0.17, 0.045)], [0.058, 0.05, 0.044]),
          LILAC, "leg{s}", mirror=True)
    k.add("paw", ell((0.12, -0.2, 0.03), (0.054, 0.066, 0.032)), LILAC, "leg{s}", mirror=True)
    k.add("thigh", ell((0.112, 0.2, 0.2), (0.085, 0.125, 0.12)), LILAC, "hind{s}", mirror=True)
    k.add("hind", tube([(0.122, 0.2, 0.15), (0.125, 0.19, 0.08), (0.124, 0.18, 0.045)], [0.056, 0.048, 0.044]),
          LILAC, "hind{s}", mirror=True)
    k.add("hpaw", ell((0.124, 0.15, 0.03), (0.054, 0.066, 0.032)), LILAC, "hind{s}", mirror=True)
    for side, m in (("R", 1), ("L", -1)):
        N.claws(k, "f" + side, (0.12 * m, -0.257, 0.028), f"leg{side}", spread=0.026, size=0.014)
        N.claws(k, "h" + side, (0.124 * m, 0.093, 0.028), f"hind{side}", spread=0.026, size=0.014)

    # The ears, the horn, the fangs at the mouth's corners
    N.ear(k, L)
    N.horn(k, L)
    for m, side in ((1, "R"), (-1, "L")):
        N.fang(k, "fang" + side, (0.04 * m, -0.405, 0.18), (0.12 * m, -0.2, -1), 0.036, 0.011)

    # Three tall spines behind the ears, smaller ones down to the rump
    for j, (y, z, ln, lean) in enumerate([(-0.07, 0.37, 0.22, 1), (0.02, 0.385, 0.26, -1), (0.11, 0.375, 0.2, 1),
                                          (0.2, 0.35, 0.12, -1), (0.28, 0.31, 0.08, 1)]):
        N.spine(k, f"spine{j}", (0.012 * lean, y, z), (0.1 * lean, 0.38, 0.92), ln, 0.1 if j < 3 else 0.075, 0.045,
                lean=(0, 1, -0.2))

    # Long dark spots on the body and the legs
    for c, n, h in [((0.14, -0.04, 0.32), (0.8, -0.1, 0.55), (0.022, 0.04)),
                    ((0.16, 0.07, 0.28), (0.95, 0.0, 0.3), (0.02, 0.042)),
                    ((0.15, 0.18, 0.31), (0.8, 0.2, 0.5), (0.02, 0.036)),
                    ((0.17, 0.26, 0.2), (0.9, 0.35, 0.0), (0.02, 0.04)),
                    ((0.15, 0.08, 0.17), (0.85, 0.0, -0.4), (0.018, 0.03)),
                    ((0.165, -0.13, 0.11), (1.0, -0.3, 0.0), (0.018, 0.034)),
                    ((0.18, 0.2, 0.1), (1.0, 0.1, 0.0), (0.018, 0.034)),
                    ((0.05, -0.27, 0.37), (0.35, -0.35, 0.85), (0.024, 0.018))]:
        N.spot(k, c, unit(*n), h)

    k.build(voxel=0.0065, smooth=4)
    k.eye_states(N.eyes_of(k, L))
    k.export(out)


if __name__ == "__main__":
    nidorino(*args())
