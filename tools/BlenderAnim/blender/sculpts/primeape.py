# Primeape sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/primeape.py -- <the export's Primeape.glb> <out.glb>
#
# Grown from mankey.py: its ball of fur, tufts, ears, snout, eyes and limbs, built by its ape() with Primeape's own
# numbers. From the references: a bigger, shaggier ball with longer spikes; the same pig's snout; eyes white with a
# small black pupil under a heavier black brow; an anger mark of four arcs on the brow; the ears lined pink; arms and
# legs brown all the way, thicker, with big brown fists and big rounded feet; black iron cuffs on the wrists and
# ankles (black in both pictures, not grey); no tail.
import os, sys
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from sculptkit import Kit, sibling, args

m = sibling("mankey")

PRIMEAPE = dict(m.MANKEY)
PRIMEAPE.update(
    C=(0.0, 0.0, 0.42), R=(0.205, 0.193, 0.195), seed=57,
    tufts=120, tuft_len=0.075, tuft_r=(0.04, 0.019, 0.003), bumps=0.06,
    ear_base=(0.13, 0.0, 0.575), ear_tip=(0.2, 0.006, 0.69), ear_w=0.058, lining="#E6B0AA",
    eye_dir=(0.36, -0.932, 0.0), eye_half=(0.036, 0.03), iris=False,
    snout_z=-0.34, snout_r=(0.062, 0.048, 0.042),
    shoulder=(0.16, 0.0, 0.43), elbow=(0.295, -0.06, 0.38), wrist=(0.31, -0.18, 0.365),
    arm_r=(0.042, 0.036, 0.034), fist_r=(0.068, 0.072, 0.07), cuff=0.0, brown_arms=True,
    hip=(0.09, 0.0, 0.28), knee=(0.225, -0.05, 0.19), ankle=(0.215, -0.06, 0.085),
    leg_r=(0.052, 0.042, 0.036), brown_from=0.0, feet="round", shackles=True,
    tail=False, vein=True,
)

if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="fur")
    m.ape(k, PRIMEAPE)
    k.build(voxel=0.007, smooth=3, band=0.004)
    k.eye_states(lambda s: m.eyes(k, PRIMEAPE, s))
    k.export(out)
