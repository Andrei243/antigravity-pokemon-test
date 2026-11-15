# Dugtrio sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/dugtrio.py -- <the export's Dugtrio.glb> <out.glb>
#
# Grown from diglett.py: three of its Digletts (its peg, eyes, nose and shine, its earth and stones) in one wide patch
# of broken earth. From the references: the tallest stands behind in the middle, a middle-sized one before it on the
# viewer's left (the model's -X, the "left" bone) and the smallest before it on the right (+X, "right"); each a little
# more slender than Diglett, their sides pressed together; the earth ring as wide as the whole is tall.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, sibling, args

d = sibling("diglett")

src, out = args()
k = Kit(src, coat="scales")
d.burrow(k, 0.32, 0.26, 17, 83, centre=(0.0, -0.012))
specs = [
    d.diglett(k, "M", (0.0, 0.06), 0.122, 0.62, "body"),
    d.diglett(k, "L", (-0.15, -0.05), 0.117, 0.47, "left", yaw=-10),
    d.diglett(k, "R", (0.15, -0.07), 0.107, 0.40, "right", yaw=10),
]
k.build(voxel=0.006, smooth=3, band=0.004)
k.eye_states(lambda s: d.eyes(k, specs, s))
k.export(out)
