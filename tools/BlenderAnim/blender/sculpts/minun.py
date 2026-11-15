# Minun sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/minun.py -- <the export's Minun.glb> <out.glb>
#
# Plusle's body (sibling("plusle")): blue for red, a minus for a plus on the cheeks and the tail; the pictures give it
# ears rounded at their ends (not pointed) and a small closed smile (not an open mouth).
import os, sys
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from sculptkit import sibling, args

p = sibling("plusle")
BLUE = "#3FA6E6"

if __name__ == "__main__":
    src, out = args()
    p.mouse(src, out, tint=BLUE, sign=p.MINUS, ear=p.ROUND_EAR, mouth=p.smile, tail_sign=p.UPRIGHT_MINUS)
