# Hategant sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/hategant.py -- <the export's Hategant.glb> <out.glb>
#
# Grown from minisaur.py (sibling("minisaur")): its body, legs, tail, neck, face and eyes at 1.3 times the size (the
# export's skeleton is Minisaur's at 1.3 to the joint), a darker olive green, the neck carrying the head further forward.
# The design, from the project's picture (docs/art/reference/gemini/carpathia/10144-Hategant.jpg) and its brief
# (CP-dex-3-river-and-caves.md, 144): the island sauropod, a giant only in its own way. Its back is a hill of dark moss
# rising almost to the head's height, with some twenty-five round grey bony scutes set in it, ferns at its edges and
# moss hanging over its flanks in short lobes; round grey knobs on its crown, the back of its head and down the top of
# its neck; the same peaceful face, smile, cream throat and belly and speckled thighs as Minisaur, with no fern.
# Reference ratios (picture): head 0.19 of the height (built 0.2), the hill's top 0.81 of the height (built 0.8),
# the hill 0.58 of the body's length.
import os, sys
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from sculptkit import sibling, args

m = sibling("minisaur")

if __name__ == "__main__":
    m.sauropod(*args(), big=True)
