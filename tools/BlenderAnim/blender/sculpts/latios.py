# Latios sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/latios.py -- <the export's Latios.glb> <out.glb>
#
# Built by latias.py's lati() (through sibling): the same jet of a dragon, a little larger and sharper. From the
# references: blue where Latias is red; the neck, chest and the back of the head white; the front of the head a blue
# nose and brow with a white oval round each red eye (where Latias has a red band over a white head); a red triangle
# on the belly; the wings longer, with more teeth at their tips; jagged fins low on the hind body's flanks; a sharper
# three-pointed tail fin.
# Bones: root 0; body (0, 0, 0.159); head (0, -0.139, 0.159); armR (0.06, -0.1, 0.119); wingR (0.06, 0, 0.219);
# tail (0, 0.18, 0.099).
import os, sys
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from sculptkit import sibling, args

lat = sibling("latias")

LATIOS = dict(lat.LATIAS)
LATIOS.update(
    MAIN="#4A90DA", WHITE="#EEF1F8", MARK="#DE3D4A", IRIS="#D8323E", IRIS_DEEP="#86141F",
    GROW=1.08,
    LATIOS_FACE=True,
    WING=[(0, -0.08), (0.15, 0.02), (0.255, 0.135), (0.305, 0.215), (0.265, 0.208), (0.27, 0.238), (0.228, 0.222),
          (0.23, 0.248), (0.185, 0.228), (0.14, 0.235), (0.0, 0.18)],
    FIN_LEN=1.15,
    TAIL_FIN=[(0, -0.03), (0.05, 0.025), (0.085, 0.115), (0.03, 0.06), (0, 0.13), (-0.03, 0.06), (-0.085, 0.115),
              (-0.05, 0.025)],
    FLANK_SPIKES=True, LIFT=0.045,
)

if __name__ == "__main__":
    src, out = args()
    lat.lati(src, out, LATIOS)
