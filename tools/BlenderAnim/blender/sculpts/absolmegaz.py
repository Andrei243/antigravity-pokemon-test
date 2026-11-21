# Mega Absol Z sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/absolmegaz.py -- <the export's Absol-Mega-Z.glb> <out.glb>
# Built by absolmega.py's absol_mega(z=True): Mega Absol's frame in navy, a lighter slate face with an open mouth and
# a fang, four great crimson plumes a side rising from navy roots, an angular bent horn, the lock with a red tip, a
# spiny pale-blue tail, thin blue spurs on the legs, the paws set wide in a crouch.
import os, sys
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from sculptkit import sibling, args

src, dst = args()
sibling("absolmega").absol_mega(src, dst, z=True, voxel=0.0085)
