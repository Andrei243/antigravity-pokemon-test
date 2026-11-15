# Wigglytuff sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown from
# Jigglypuff's script (jigglypuff.py, imported: its functions, colours and eyes), refitted to the taller skeleton:
#   blender --background --factory-startup --python sculpts/wigglytuff.py -- <the export's Wigglytuff.glb> <out.glb>
# The ball grown into a tall egg, a smooth white belly from its middle to its foot, long rabbit's ears lined dark navy
# leaning out from the crown, the curl of hair now a round lobe on top of the head, eyes a little smaller for the
# bigger body (the same white ring, rim, teal iris and glint), a small open mouth, stubby arms and two oval feet.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, srgb, inside, boxy, sibling, args

j = sibling("jigglypuff")

WHITE = "#FCF4F6"
MOUTH, TONGUE = "#C8384C", "#F08294"

# --- the frame (the skeleton's joints: body (0, 0, 0.3), ears (+-0.09, 0, 0.48), arms (+-0.18, -0.04, 0.3),
#     legs (+-0.1, -0.02, 0.08)) -------------------------------------------------------------------------------------
j.C = np.array([0, 0, 0.29])
j.R = np.array([0.2, 0.18, 0.265])
C, R = j.C, j.R
EAR_BASE = np.array([0.095, 0.0, 0.5])
EAR_TIP = np.array([0.215, 0.018, 0.75])
EAR_R = [0.06, 0.07, 0.054, 0.014]
EYE_X, EYE_Z = 0.086, 0.092
EYE_HALF = (0.05, 0.054)
MOUTH_Z = 0.026


def egg(V):
    """Jigglypuff's ball grown tall: fuller below its middle, a little narrower over the brow."""
    V = boxy(2.15)(V)
    z = V[:, 2]
    s = 1 + 0.07 * np.clip(-z, 0, 1) * (1 - 0.5 * np.clip(-z, 0, 1)) - 0.06 * np.clip(z, 0, 1) ** 2
    V[:, 0] *= s; V[:, 1] *= s
    return V


def add_belly(k):
    """The white of the front: a smooth oval from just under the mouth to the foot of the egg, crisp edged."""
    def mask(P):
        e = (P[:, 0] / 0.148) ** 2 + ((P[:, 2] - 0.155) / 0.135) ** 2
        return inside(np.sqrt(e) - 1, 0.012) * inside(P[:, 1] + 0.04, 0.01)
    k.paint(mask, WHITE, on=["body"])


CP = srgb(j.PINK)


def open_mouth(u, v):
    """A small open mouth: round below, a gentle arc on top, a pink tongue in its foot."""
    c = np.tile(CP, (len(u), 1))
    m = (np.hypot(u / 0.95, (v - 0.25) / 1.2) < 1) & (v < 0.42 - 0.25 * u * u)
    c[m] = srgb(MOUTH)
    c[m & (np.hypot(u / 0.55, (v + 0.7) / 0.42) < 1)] = srgb(TONGUE)
    return c


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="fur")
    j.add_body(k, deform=egg)
    j.add_ears(k, base=EAR_BASE, tip=EAR_TIP, radii=EAR_R, inset=0.66)
    j.add_curl(k, at=(0.0, 0.84), radius=0.062, thick=0.044, turns=1.2, lift=(0.03, 0.01))
    j.add_arms(k, root=(0.17, -0.05, 0.29), tip=(0.262, -0.095, 0.262), radii=(0.052, 0.044, 0.032))
    j.add_feet(k, at=(0.1, -0.055, 0.026), r=(0.055, 0.078, 0.03), turn=15,
               leg=((0.1, -0.02, 0.1), (0.1, -0.04, 0.045)), leg_r=(0.056, 0.05))
    add_belly(k)
    j.add_mouth(k, z=MOUTH_Z, half=(0.032, 0.024), look=open_mouth)
    k.build(voxel=0.0058, band=0.008)
    k.eye_states(lambda s: j.lay_eyes(k, s, half=EYE_HALF, x=EYE_X, z=EYE_Z))
    k.export(out)
