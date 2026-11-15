# Skiploom sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/skiploom.py -- <the export's Skiploom.glb> <out.glb>
#
# Grown from Hoppip's script (sibling("hoppip")): the same round body, the same round eyes with their rim and glint,
# the same stubs. From the references: a green bun of a body, wider than tall; two big flat rounded ears out from the
# top of its sides, hanging out to either side and a little back; small red eyes with a white glint, set wide and
# low; a wavy mouth line between them; four little stubs of feet (the front pair out at the sides, the hind pair
# under it); and on its head a yellow flower of ten round thick petals in a ring about a pale, sunken middle.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, srgb, unit, args, lathe, outward, sibling

hp = sibling("hoppip")

GREEN = "#76C24E"
PETAL = "#F6CC3A"
MIDDLE = "#EEF2DC"
RED, RED_DARK, RIM = srgb("#E8404C"), srgb("#B8283A"), srgb("#4A1A22")
LINE = srgb("#2E5A26")                  # the mouth and a shut eye's line

# --- Skiploom's frame (joints: body (0, 0, 0.13), flower (0, 0, 0.23), legs (+-0.08, -0.06, 0.07),
#     hind (+-0.08, 0.06, 0.07)) ---------------------------------------------------------------------------------------
C = np.array([0.0, 0.0, 0.135])
R = np.array([0.142, 0.128, 0.112])
FC = np.array([0.0, 0.004, 0.252])      # the flower's middle, on the crown
RING = 0.094                            # the petals' middles from it
PETALS = 10
EYE_D = (0.5, -0.86, -0.02)
EYE_HALF = (0.019, 0.021)
MOUTH_D = (0.0, -1.0, -0.2)
MOUTH_HALF = (0.05, 0.017)


def ear(k, color=GREEN):
    """A big flat rounded ear out from the top of each side, hanging out and a little back, thick at its end."""
    k.add("ear", ell((0.152, 0.05, 0.162), (0.096, 0.074, 0.04), rot=(0, 30, 38)), color, "body", mirror=True,
          surface="leaf")


def wavy_mouth(k):
    """A wavy line: up at its corners, dipping twice, a small rise in the middle."""
    p, n = hp.on_ell(C, R, MOUTH_D)
    CG = srgb(GREEN)
    def look(u, v):
        c = np.tile(CG, (len(u), 1))
        f = 0.36 * np.cos(2 * math.pi * u) * np.where(np.abs(u) > 0.5, 1.0, 0.75)
        c[(np.abs(v - f) < 0.34) & (np.abs(u) < 0.94)] = LINE
        return c
    k.decal(p, n, MOUTH_HALF, look, eps=0.004, rings=10, segs=64)


def flower(k):
    """Ten round thick petals in a ring, tipped up a little at their ends, about a pale sunken middle."""
    for i in range(PETALS):
        a = 2 * math.pi * (i + 0.5) / PETALS
        out = np.array([math.sin(a), -math.cos(a), 0.0])
        c = FC + out * RING + np.array([0, 0, 0.006])
        turn = math.degrees(math.atan2(out[0], -out[1]))
        k.add(f"petal{i}", ell(c, (0.037, 0.054, 0.018), rot=(-12, 0, turn), sub=3), PETAL, "flower", fuse=False,
              surface="leaf")
    # the middle: a low drum, sunken in its top
    k.add("middle", lathe(FC + np.array([0, 0, -0.012]), (0, 0, 1),
                          [(0.0, 0.0), (0.066, 0.0), (0.068, 0.018), (0.056, 0.024), (0.034, 0.016), (0.0, 0.014)],
                          seg=48), MIDDLE, "flower", fuse=False, surface="leaf")
    # a green knob under it, fused into the crown
    k.add("knob", ell(FC + np.array([0, 0, -0.04]), (0.06, 0.06, 0.024)), GREEN, "flower")


def skiploom(src, out):
    k = Kit(src, coat="leaf")
    k.add("body", ell(C, R, deform=hp.round_body(square=2.2, full=0.05)), GREEN, "body")
    ear(k)
    # four little stubs: the front pair out at the sides and forward, the hind pair under the body
    hp.stub(k, "front", (0.128, -0.07, 0.06), (0.036, 0.042, 0.03), GREEN, "leg{s}", rot=(0, 0, 30))
    hp.stub(k, "hind", (0.08, 0.055, 0.03), (0.034, 0.04, 0.03), GREEN, "hind{s}")
    flower(k)
    wavy_mouth(k)
    k.build(voxel=0.0055, smooth=4, band=0.006)

    look = hp.round_eye(fill=RED, rim=RIM, dark=RED_DARK)
    def eyes(state):
        hp.lay_eyes(k, C, R, EYE_D, EYE_HALF, look, GREEN, LINE, state)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    skiploom(*args())
