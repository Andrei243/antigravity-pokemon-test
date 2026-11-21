# Latios's Mega Evolution sculpted on the skeleton and clips of the game's own export of the form (sculptkit.py):
#   blender --background --factory-startup --python sculpts/latiosmega.py -- <the export's Latios-Mega.glb> <out.glb>
#
# Built by latiasmega.py's mega() (through sibling): the two Megas are one design, lavender and white with great
# pods for wings, a hollow triangle on each and long blades from their tops. Latios's is a little larger, as Latios
# is beside Latias (latios.py's measure), and its eyes are red.
# Bones: root 0; body (0, 0, 0.162); head (0, -0.139, 0.162); armR (0.06, -0.1, 0.122); wingR (0.06, 0, 0.222);
# tail (0, 0.18, 0.102).
import os, sys
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from sculptkit import sibling, args

lm = sibling("latiasmega")

LATIOS_MEGA = dict(lm.LATIAS_MEGA)
LATIOS_MEGA.update(IRIS="#D8323E", IRIS_DEEP="#86141F", GROW=1.08, LIFT=0.045)

if __name__ == "__main__":
    src, out = args()
    lm.mega(src, out, LATIOS_MEGA)
