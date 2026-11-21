# Mega Meowstic (the female) sculpted on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/meowsticfemalemega.py -- <the export's Meowstic-Female-Mega.glb> <out.glb>
# It has no picture of its own: the two filed under its name are the male Mega's. Its design is therefore the game's
# own sculptor's female branch (PokemonModels.Kalos1.cs, MeowsticBuild) laid on the male Mega's build
# (meowsticmalemega.py): white where the male is navy, navy marks (a forelock down the brow, the feet, the hands'
# tips, the tails' ends), both ears navy with a white band below the paddle and the yellow eye-ring, red eyes (shut,
# as the male's), and the hood of locks in navy so that it still reads against a white head. The male's split down
# the middle is the male's alone.
import os, sys
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from sculptkit import args, sibling

mm = sibling("meowsticmalemega")

if __name__ == "__main__":
    src, dest = args()
    mm.meowsticmega(src, dest, female=True)
