# Mega Magearna's Original Color sculpted on the skeleton and clips of the game's own export of the form (sculptkit.py):
#   blender --background --factory-startup --python sculpts/magearnaoriginalmega.py -- <the export's Magearna-Original-Mega.glb> <out.glb>
#
# The Original Color's own form has no Blender sculpt, so this is grown from Mega Magearna (magearnamega.py) with the
# Original Color's pictures: the same red gown, gold body, red crescent ears, silver crown, wheel and sash; the cogs
# white where Mega Magearna's are dark grey, the gown's lower half, its lobes and the lid round the neck white where
# they are dark, and the medallion's ring gold where it is red.
import os, sys
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from sculptkit import args, sibling

if __name__ == "__main__":
    sibling("magearnamega").magearna_mega(*args(), original=True)
