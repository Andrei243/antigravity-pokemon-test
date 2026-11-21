# Tatsugiri-Droopy-Mega on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/tatsugiridroopymega.py -- <the export's .glb> <out.glb>
#
# From the references: the same platter of all three looks as the Curly Mega (tatsugiricurlymega.py, which builds all
# of them), with the Droopy Form in the middle wearing the roe on its back, the Curly Form to its left and the
# Stretchy Form behind it to the right.
import os, sys
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from sculptkit import args, sibling

if __name__ == "__main__":
    sibling("tatsugiricurlymega").platter(*args(), "droopy")
