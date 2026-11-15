# Vileplume sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/vileplume.py -- <the export's Vileplume.glb> <out.glb>
#
# Grown from Oddish's script (sibling("oddish")), as Gloom is: the same blue pear of a body, legs
# and arms, the line's small red eyes. From the references: an enormous red flower worn like a hat, far wider than the
# body, of five great rounded petals like cushions (two to the front, one at each side, one behind), their rims
# turned down a little, spotted all over with white; in its middle a raised orange ring round a dark hole; the flower
# leans forward so its top shows. Under its rim small red eyes peer out and a little smile; the arms stand out to the
# sides, the feet turn out and forward.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from mathutils import Euler
from sculptkit import Kit, ell, tube, lathe, srgb, unit, eye_look, args, sibling

od = sibling("oddish")

BLUE = od.BLUE
PETAL, SPOT = "#DE5466", "#F8EEF0"
RING, HOLE = "#F2A032", "#4E2C16"
SMILE = srgb("#1C2440")

B = np.array([0.0, 0.0, 0.2])            # the pear's middle
BR = np.array([0.15, 0.136, 0.15])
FC = np.array([0.0, 0.01, 0.405])        # the flower's middle
TILT = np.array(Euler((math.radians(12), 0, 0)).to_matrix())   # the flower leans forward
PET_OUT, PET_R = 0.2, np.array([0.158, 0.172, 0.084])          # a petal's middle out from the axis, its radii
DROOP = math.radians(9)
ANGLES = [36 + 72 * i for i in range(5)]                         # from the front toward +X: two in front, one behind


def petal_frame(t):
    """A petal's turn: its rim drooped about x, then turned about z to its place, then the flower's lean."""
    rx = np.array(Euler((DROOP, 0, 0)).to_matrix())
    rz = np.array(Euler((0, 0, math.radians(t))).to_matrix())
    return TILT @ rz @ rx


def cushion(V):
    """A petal: narrower toward the middle of the flower, domed above, flatter beneath."""
    V = V.copy()
    V[:, 0] *= 1 - 0.3 * np.clip(V[:, 1], 0, 1)
    V[:, 2] = np.where(V[:, 2] < 0, V[:, 2] * 0.65, V[:, 2])
    return V


def vileplume_body(k):
    k.add("bulb", ell(B, BR, deform=od.bulb(flat=0.85, slim=0.2)), BLUE, "body")
    k.add("leg", tube([(0.06, 0.0, 0.12), (0.076, -0.01, 0.05), (0.12, -0.04, 0.026), (0.162, -0.064, 0.026)],
                      [0.035, 0.031, 0.028, 0.028]), BLUE, "leg{s}", mirror=True)
    k.add("arm", tube([(0.12, -0.02, 0.215), (0.178, -0.034, 0.192), (0.216, -0.042, 0.168)],
                      [0.034, 0.031, 0.031]), BLUE, "arm{s}", mirror=True)


def vileplume_flower(k):
    for i, t in enumerate(ANGLES):
        M = petal_frame(t)
        V, F = ell((0, -PET_OUT, 0), PET_R, deform=cushion)
        k.add(f"petal{i}", (FC + V @ M.T, F), PETAL, "flower", surface="leaf")
        # white spots over its top: (across -1..1, along 0 inner .. 1 outer, size)
        for a, b, s in ((0.0, 0.62, 0.024), (-0.42, 0.42, 0.018), (0.4, 0.5, 0.02), (0.18, 0.86, 0.016),
                        (-0.25, 0.8, 0.019), (0.5, 0.18, 0.013), (-0.5, 0.1, 0.014), (0.05, 0.3, 0.014)):
            x = a * PET_R[0] * (1 - 0.3 * max(0.0, 1 - 2 * b)) * 0.9
            y = -PET_OUT + (0.5 - b) * 2 * PET_R[1] * 0.88
            q = np.array([x, y + PET_OUT, 0.0]) / PET_R
            z = PET_R[2] * math.sqrt(max(0.02, 1 - (q[:2] ** 2).sum()))
            p = np.array([x, y, z])
            nl = np.array([x / PET_R[0] ** 2, (y + PET_OUT) / PET_R[1] ** 2, z / PET_R[2] ** 2])
            nl = M @ (nl / np.linalg.norm(nl))
            k.decal(FC + M @ p, nl, (s, s * 0.8), SPOT, eps=0.003, rings=6, segs=24)
    # the middle, filled between the petals' inner ends
    k.add("middle", ell(FC, (0.12, 0.12, 0.07), rot=(12, 0, 0)), PETAL, "flower", surface="leaf")
    # the raised orange ring round its dark hole
    up = TILT @ np.array([0, 0, 1.0])
    base = FC + up * 0.05
    k.add("ring", lathe(base, up, [(0, 0.0), (0.07, 0.0), (0.08, 0.022), (0.077, 0.04), (0.066, 0.047),
                                   (0.052, 0.044), (0.046, 0.034), (0.044, 0.012), (0, 0.012)], seg=48),
          RING, "flower", fuse=False, surface="leaf")
    k.add("hole", lathe(base, up, [(0, 0.012), (0.0445, 0.012), (0.0445, 0.032), (0, 0.032)], seg=48),
          HOLE, "flower", fuse=False, surface="leaf")


def smile(k):
    p, n = od.on_ell(B, BR, (0, -1, -0.2))
    lid = srgb(BLUE)
    def look(u, v):
        c = np.tile(lid, (len(u), 1))
        c[(np.abs(v - (-0.35 + 0.75 * u * u)) < 0.22) & (np.abs(u) < 0.88)] = SMILE
        return c
    k.decal(p, n, (0.032, 0.016), look, eps=0.004, rings=16, segs=56)


def vileplume(src, out):
    k = Kit(src, coat="fur")
    vileplume_body(k)
    vileplume_flower(k)
    smile(k)
    k.build(voxel=0.006, smooth=4, band=0.004)

    lid = srgb(BLUE)
    def eyes(state):
        od.lay_eyes(k, B, BR, (0.36, -0.9, 0.1), (0.017, 0.02), od.round_eye(), lid, od.INK, state)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    vileplume(*args())
