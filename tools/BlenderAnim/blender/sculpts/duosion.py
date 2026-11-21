# Duosion sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown from
# Solosis's script (sibling("solosis"): its colours, its jelly fused round the creature with the crisp line where the
# creature stands out of it, its eyes, its red diamond and its glint):
#   blender --background --factory-startup --python sculpts/duosion.py -- <the export's Duosion.glb> <out.glb>
#
# From the references: a pale green creature like an onion turned over, a broad round head tapering below into a body
# that ends in a point curving a little to its right (-X), two small round hands held together under the chin; a seam
# runs down the head's front from its crown to the red diamond; two upright dark green oval eyes set wide on the head,
# the red diamond between them; all of it in a much larger lump of darker green jelly, wider at the top and the upper
# right, with round bulges at the lower left and lower right, and a small flat round lump floating in it near the top
# right. (The task's text spoke of a two-lobed creature: the pictures show one body, its head split only by the seam.)
# The skeleton: root (0, 0, 0), body (0, 0, 0.251), head (0, -0.04, 0.301). It hovers.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, sibling, args

so = sibling("solosis")
SEAM, LUMP = "#93B47A", "#86C07A"

# --- the frame ------------------------------------------------------------------------------------------------------
JC, JR = np.array([0.0, 0.02, 0.262]), np.array([0.168, 0.15, 0.2])          # the jelly, before its bulges
BULGES = [((0.55, 0.15, 0.75), 0.14, 0.42),       # the upper right, where the lump floats
          ((-0.6, 0.2, 0.55), 0.06, 0.4),         # the upper left
          ((-0.7, 0.05, -0.6), 0.18, 0.36),       # the round bulge at the lower left
          ((0.75, 0.0, -0.55), 0.16, 0.34),        # and the lower right
          ((0.0, 0.7, -0.4), 0.06, 0.5)]          # fuller behind and below
HC, HR = np.array([0.0, -0.08, 0.335]), np.array([0.1, 0.082, 0.082])     # the head
BODY = [(0.0, -0.088, 0.3), (0.0, -0.104, 0.245), (-0.006, -0.116, 0.185), (-0.017, -0.122, 0.138)]
BODY_R = [0.08, 0.068, 0.046, 0.02]
EYE_X, EYE_Z, EYE_HALF = 0.056, 0.338, (0.0135, 0.022)
MOUTH_Z, MOUTH_HALF, MOUTH_TILT = 0.33, (0.017, 0.028), -14
VOXEL = 0.004


def jelly_shape(V):
    """The jelly's lump: the unit sphere swelled in round bulges, none on the front, where the creature stands out."""
    n = V / np.linalg.norm(V, axis=1, keepdims=True)
    s = np.ones(len(V))
    for d, a, w in BULGES:
        s += a * np.exp(-((n - unit(*d)) ** 2).sum(1) / w ** 2)
    return V * s[:, None]


def seam(P):
    """The seam down the head's front, from the crown to just above the diamond, bowing a little to +X."""
    z = P[:, 2]
    t = np.clip((z - (MOUTH_Z + MOUTH_HALF[1])) / (HC[2] + HR[2] - MOUTH_Z - MOUTH_HALF[1]), 0, 1)
    x = 0.006 * np.sin(math.pi * t)
    band = inside(np.abs(P[:, 0] - x) - 0.003, 0.0016)
    return band * (z > MOUTH_Z + MOUTH_HALF[1] + 0.004) * (P[:, 1] < HC[1] + 0.03)


def duosion(src, out):
    k = Kit(src, coat="shell")
    k.add("jelly", ell(JC, JR, deform=jelly_shape, sub=5), so.JELLY, "body")
    k.add("cellhead", ell(HC, HR, sub=5), so.CELL, "head", surface="fur")
    k.add("cellbody", tube(BODY, BODY_R), so.CELL, "body", surface="fur")
    # the two little hands held together under the chin
    k.add("cellhand", ell((0.036, -0.176, 0.262), (0.022, 0.02, 0.026), rot=(0, -15, 0)), so.CELL, "body",
          mirror=True, surface="fur")
    # the small flat lump floating in the jelly near its top right, half out of the jelly's surface
    lp = np.array([0.095, -0.075, 0.43])
    k.add("lump", ell(lp, (0.042, 0.03, 0.022), rot=(-38, 24, 0), sub=3), LUMP, "body", fuse=False)
    so.crisp_edge(k, JC, JR, np.array([0.0, -0.1, 0.25]), np.array([0.14, 0.11, 0.2]))
    k.paint(seam, SEAM, on=["cellhead"])
    mc = so.on_ell(HC, HR, (0, -1, (MOUTH_Z - HC[2]) / HR[2]))
    so.diamond(k, mc, so.normal_at(HC, HR, mc), MOUTH_HALF, MOUTH_TILT)
    so.glint(k, JC, JR, (-0.62, -0.5, 0.6), (0.036, 0.02), up=unit(0.55, 0, 0.84))
    k.build(voxel=VOXEL, smooth=3, band=0.004)

    def eyes(state):
        for s in (1, -1):
            p = so.on_ell(HC, HR, (EYE_X * s / HR[0], -1.0, (EYE_Z - HC[2]) / HR[2]))
            n = so.normal_at(HC, HR, p)
            n = unit(n[0] * 0.7, n[1], n[2] * 0.7)
            so.eye_patch(k, p, n, EYE_HALF, state)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    duosion(*args())
