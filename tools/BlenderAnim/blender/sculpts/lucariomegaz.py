# Mega Lucario Z sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/lucariomegaz.py -- <the export's Lucario-Mega-Z.glb> <out.glb>
# Built by lucariomega.py's lucario_mega(z=True): Mega Lucario's frame with a dark grey head and body, a pale grey
# mask over blue eyes, teal limbs ringed in grey with grey discs at the knees and shins, a yellow collar of fur and a
# great yellow fall of it over the hips, two thin teal cords from the head, a long broad sash streaming from the
# right shoulder (grey, then teal with a zigzag) and a dark grey shark's tail.
import os, sys
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from sculptkit import sibling, args

src, dst = args()
sibling("lucariomega").lucario_mega(src, dst, z=True)
