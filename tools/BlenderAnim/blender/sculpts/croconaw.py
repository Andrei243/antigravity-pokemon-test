# Croconaw sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/croconaw.py -- <the export's Croconaw.glb> <out.glb>
# Totodile grown stocky (built by totodile.py's functions: its blue, red and cream, its jaw, fangs, spines, hands and
# feet): a great round belly over short legs, the head carried forward on hardly a neck, the mouth shut, the big lower
# jaw cream under a blue upper one and a white fang standing up at each back corner of it; the eyes under a heavy
# black lid (a red-brown iris); a red crest of four points laid back along the top of the head; a cream belly from
# the chest down, torn into by blue patches; thick arms forward and a little down, four short fingers a hand; three
# red spines down the back; the tail curving out to the +X side to a red diamond at its end.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from types import SimpleNamespace
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, inside, unit, args, sibling

T = sibling("totodile")
BLUE, CREAM, RED = T.BLUE, T.CREAM, T.RED
IRIS = srgb("#B4361E")
VOXEL = 0.0062

L = SimpleNamespace(**vars(T.L))
L.H, L.HR = np.array([0.0, -0.035, 0.56]), np.array([0.143, 0.137, 0.116])
L.cheek_at, L.cheek_r = np.array([0.0, -0.05, -0.07]), (0.125, 0.105, 0.09)
L.hinge = (0.0, -0.09, 0.5)
L.gape_up, L.gape_down = 6, 3
L.open = False
L.upper_len, L.upper_r, L.upper_tip = 0.26, (0.1, 0, 0.05), 0.72
L.lower_len, L.lower_r, L.lower_tip, L.lower_colour = 0.25, (0.112, 0, 0.068), 0.7, CREAM
L.nostril_x, L.nostril_r = 0.028, 0.009
L.eye_dir, L.eye_shift, L.eye_h = (0.72, -0.4, 0.5), (0, 0, 0), (0.036, 0.034)
L.eye_outline = T.brow_cut(0.62, -0.25)
L.eye_look = T.lidded_eye(IRIS, L.eye_outline, band=0.34)
L.lid, L.glint = BLUE, False


def croconaw(src, out):
    k = Kit(src, coat="scales")

    # The body: a great round belly, wide low hips, a thick short neck carrying the head forward
    def pear(V):
        V = V.copy()
        low = np.clip(-V[:, 2], 0, 1)
        V[:, :2] *= (1 + 0.16 * low)[:, None]
        return V
    k.add("torso", ell((0, 0.01, 0.27), (0.175, 0.16, 0.185), deform=pear), BLUE, "body")
    k.add("hips", ell((0, 0.025, 0.15), (0.17, 0.15, 0.1)), BLUE, "body")
    k.add("neck", tube([(0, 0.0, 0.38), (0, -0.03, 0.44), (0, -0.05, 0.49)], [0.12, 0.115, 0.11]), BLUE, "head")

    # The cream belly from the chest down, torn into by blue patches
    def belly(P):
        x, y, z = P[:, 0], P[:, 1], P[:, 2]
        a = np.arctan2(x, -(y - 0.01))
        s = a * 0.17
        top = 0.35 - 0.03 * (a / 0.9) ** 2
        return inside(np.abs(a) - 1.02, 0.004) * inside(z - top, 0.003) * inside(0.075 - z, 0.003) * (y < 0.06) \
            * inside(np.sqrt(x * x + (y - 0.01) ** 2) - 0.24, 0.004)
    k.paint(belly, CREAM)
    spots = [(0.1, 0.29, 0.04, 0.034), (-0.11, 0.27, 0.038, 0.042), (0.075, 0.15, 0.034, 0.04),
             (-0.08, 0.12, 0.04, 0.03)]
    def patches(P):
        x, y, z = P[:, 0], P[:, 1], P[:, 2]
        s = np.arctan2(x, -(y - 0.01)) * 0.17
        m = np.zeros(len(P))
        for cs, cz, rs, rz in spots:
            m = np.maximum(m, inside(np.sqrt(((s - cs) / rs) ** 2 + ((z - cz) / rz) ** 2) - 1, 0.06))
        return m * (y < 0.06)
    k.paint(patches, BLUE)

    # The legs: heavy thighs fused into the hips, a stub of shin, broad feet of three round toes
    for s, side in ((1, "R"), (-1, "L")):
        T.leg(k, side, s, (0.1, -0.01, 0.13), (0.09, 0.1, 0.095),
              [(0.108, -0.02, 0.08), (0.112, -0.03, 0.04)], [0.06, 0.056],
              (0.112, -0.06, 0.025), (0.064, 0.092, 0.027),
              [(0.112 + dx, -0.14, 0.022) for dx in (-0.036, 0.0, 0.036)], (0.021, 0.026, 0.02))

    # The arms: thick, forward and a little down, four short fingers a hand
    for s, side in ((1, "R"), (-1, "L")):
        T.hand(k, side, s, (0.105, -0.027, 0.364), (0.16, -0.09, 0.335), (0.17, -0.17, 0.3), [0.052, 0.046, 0.042],
               (0.044, 0.04, 0.04), [(0.55, -1, 0.4), (0.2, -1, 0.2), (-0.1, -1, 0.0), (-0.4, -1, -0.25)], 0.032,
               0.0155)

    # The tail: thick at the hips, back and out to the +X side, a red diamond at its end turned to that side
    TAIL = np.array([(0, 0.11, 0.19), (0.03, 0.24, 0.12), (0.1, 0.35, 0.08), (0.18, 0.42, 0.075)])
    TAIL_R = [0.1, 0.075, 0.048, 0.026]
    k.add("tail", tube(TAIL, TAIL_R), BLUE, "tail")
    tip = TAIL[-1]
    d = unit(0.45, 0.35, 1.0)                                   # up, out and back
    side = unit(0.75, -0.65, 0.0)                               # its face toward the front three-quarter and back
    face = np.cross(side, d)
    c = tip + d * 0.075
    pts = [tip - d * 0.01, c + side * 0.045, c - side * 0.045, tip + d * 0.16,
           c + face * 0.016, c - face * 0.016]
    k.add("taildiamond", hull(pts), RED, "tail", fuse=False, surface="scales")
    T.spine(k, "tailspine", TAIL[1] + np.array([0, 0, TAIL_R[1] * 0.75]), unit(0.15, 0.6, 1.0), 0.06, 0.06, 0.03,
            "tail")
    # Three red spines down the back
    for j, (y, z, ln, lean) in enumerate([(0.13, 0.42, 0.08, 1), (0.16, 0.33, 0.085, -1), (0.16, 0.24, 0.07, 1)]):
        T.spine(k, f"spine{j}", (0.0, y, z), (0.12 * lean, 0.9, 0.55), ln, 0.075, 0.036, "body")

    # The head: a long blue upper jaw shut over the big cream lower one, a white fang up at each back corner
    J = T.jaws(k, L)
    for s, side in ((1, "R"), (-1, "L")):
        base = J.hinge + J.dl * L.lower_len * 0.38 + np.array([0.097 * s, 0, 0.006])
        k.add(f"fang{side}", T.fang(base, (0.12 * s, -0.15, 1.0), 0.05, 0.03, 0.024, up=(0, 1, 0)), T.TOOTH, "head",
              fuse=False, surface="shell")
    T.nostrils(k, L, J)

    # The crest: four red points laid back along the top of the head, the second the tallest
    H, HR = L.H, L.HR
    for j, (y, z, ln, w, tilt) in enumerate([(-0.07, 0.1, 0.09, 0.07, 1), (0.0, 0.112, 0.15, 0.085, -1),
                                             (0.07, 0.095, 0.12, 0.075, 1), (0.125, 0.06, 0.08, 0.06, -1)]):
        base = H + np.array([0, y, z - 0.012])
        T.spine(k, f"crest{j}", base, (0.1 * tilt, 1.15, 1.0), ln, w, 0.045, "head")

    lay, E, de = T.eyes(k, L)
    k.build(voxel=VOXEL, smooth=4, band=0.005)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    croconaw(*args())
