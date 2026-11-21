# Palafin (Zero Form) sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/palafin.py -- <the export's Palafin.glb> <out.glb>
#
# From the references: Finizen itself, a tenth larger (its skeleton is Finizen's times 1.1), with a small pink heart on
# the lower chest between the pectoral fins. Grown from finizen.py, whose frame is kept whole.
import os, sys
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from sculptkit import sibling, args

fz = sibling("finizen")

if __name__ == "__main__":
    src, dst = args()
    fz.dolphin(src, dst, 1.1, heart_mark=True)
