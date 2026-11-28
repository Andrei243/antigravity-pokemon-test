# Aricorn sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/aricorn.py -- <the export's Aricorn.glb> <out.glb>
# Carpathia No. 10020 (plan 46), the project's own design: docs/art/reference/gemini/carpathia/10020-Aricorn.jpg and
# CP-dex-1-plateau.md, "20. Aricorn: the orchard hedgehog". Arichip grown (arichip.py's hedgehog(), on a skeleton
# exactly 1.6 times Arichip's): from the picture, the same round brown hedgehog, its mound of leaves now mostly green
# plain pointed leaves with a few orange and yellow oak leaves left among them; fruit all over it like a small orchard
# (red apples with a leaf, small yellow plums, yellow-green pears with stems), several on the back for the back camera;
# a grey muzzle over the snout's front; kindly brows over the eyes; dark claws on its paws.
import os, sys
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from sculptkit import args, sibling

A = sibling("arichip")

GREENS = ["#4E8E3A", "#3C7A30", "#68A548", "#58963E", "#467F34"]
APPLE, PLUM, PEAR = "#C8352C", "#EAC83A", "#B8C640"

ARICORN = dict(
    name="Aricorn", K=1.6, leaves=GREENS, plain_share=0.78, mound="#4A7E34", count=1.35, size=0.86,
    fruit=[("apple", (0.45, 0.1, 0.88), APPLE), ("apple", (-0.75, -0.25, 0.55), APPLE),
           ("apple", (0.8, 0.45, 0.15), APPLE), ("apple", (-0.35, 0.85, 0.35), APPLE),
           ("plum", (-0.2, -0.3, 0.95), PLUM), ("plum", (0.35, -0.55, 0.75), PLUM),
           ("plum", (-0.6, 0.55, 0.6), PLUM), ("plum", (0.25, 0.9, 0.45), PLUM),
           ("pear", (0.95, 0.05, 0.25), PEAR), ("pear", (-0.85, 0.4, 0.1), PEAR),
           ("pear", (0.55, 0.75, 0.25), PEAR)],
    muzzle="#9A9A9E", brows=True, claws=True, seed=1020, fruit_size=0.92,
)

if __name__ == "__main__":
    A.hedgehog(*args(), ARICORN)
