# Mega Garchomp Z sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/garchompmegaz.py -- <the export's Garchomp-Mega-Z.glb> <out.glb>
# Built by garchompmega.py's garchomp_mega(z=True): Mega Garchomp's frame in a darker navy, two yellow chevrons on an
# orange chest, a red lance out of each horn's red end, the arms raised under great orange wing-blades edged with
# white spikes, navy fins off the shoulders, gold bands round the shins and the tail.
import os, sys
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from sculptkit import sibling, args

src, dst = args()
sibling("garchompmega").garchomp_mega(src, dst, z=True)
