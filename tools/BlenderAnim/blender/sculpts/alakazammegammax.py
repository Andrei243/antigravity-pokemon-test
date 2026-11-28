# Mega Dynamax Alakazam (a form of our own, plan 48): Mega Alakazam's own sculpt (alakazammega.py, which builds as it
# is read), whole and unchanged, with what the form adds laid on its kit just before it is meshed (sculptkit.py), with
# the helpers of raichumegaxmmax.py (the emblem, the clouds, the puffs, the bolts):
#   blender --background --factory-startup --python sculpts/alakazammegammax.py -- <the export's Alakazam-Mega-Mmax.glb> <out.glb>
#
# The design is one picture of the project's own (docs/art/reference/gemini/megadynamax/Mega-Dynamax-Alakazam.jpg):
# the five spoons over the crown grown into long six-sided prisms of crystal, violet, pink, orange, cyan and white
# from the model's left to its right, the largest thing added; the red gem on the brow become the Key Stone's emblem, a
# violet star in a pale ring; clouds of Max energy coiled about it under the lap with sparks of violet and cyan
# lightning, and a curl of crimson cloud beyond each end of the crystals' arc.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, tube, lathe, unit, srgb, sibling

MX = sibling("raichumegaxmmax")

CRYSTALS = ["#A06EE6", "#F482BE", "#FAAA64", "#64E2F0", "#E2E8F4"]   # the sculptor's, as the picture orders them


def maxed_alakazam(k):
    # Numbers from alakazammega.py (it builds as it is read): the spoons' bowls at (0, 0.01, 0.8) + d * 0.3, d at
    # -64 + 32 i degrees from straight up in the xz plane, each a handle from the bowl - d * 0.15 (radius 0.016) and a
    # bowl 0.036 by 0.052 by 0.016; the gem on the brow at (0, -0.112, 0.755), radii (0.016, 0.012, 0.028); the head
    # (0, -0.025, 0.71) with radii (0.11, 0.1, 0.105)

    # Each spoon sealed in a long six-sided prism of crystal pointing at the head (the spoon inside it unchanged)
    R = 0.05
    for i in range(5):
        a = math.radians(-64 + 32 * i)
        d = np.array([math.sin(a), 0, math.cos(a)])
        bowl = np.array([0, 0.01, 0.8]) + d * 0.3
        c0 = bowl - d * 0.215
        L = 0.31
        prof = [(0, 0), (R, 0.06), (R, L - 0.065), (0, L)]
        k.add(f"mxcrystal{i}", lathe(c0, d, prof, seg=6),
              CRYSTALS[i], "head", fuse=False, glow=CRYSTALS[i])

    # The emblem over the gem on the brow: the gem itself turned pale glowing violet, and the emblem's dome over it
    for p in k.parts:
        if p["name"] == "gem":
            p["color"] = srgb("#F2EEFF")
            p["glow"] = "#F2EEFF"
    n = unit(0, -0.89, 0.46)
    MX.emblem(k, np.array([0, -0.112, 0.757]) - n * 0.004, n, 0.038, "#B8A8FF", "#F4F0FF", "#8A48E8", "head")

    # The clouds coiled under it: it floats cross-legged, its lowest point (the feet) at z 0.13, so the clouds keep to
    # the ground under the knees (x 0.22) and the sleeves (x 0.52)
    MX.clouds(k, 0.0, -0.05, 0.44, 0.32, 6, 0.07, 31, rider=False)
    sparks = [tube(MX.zig((0.34, -0.24, 0.06), (0.41, -0.27, 0.22), 0.02, side=(0, 1, 0)), [0.014] * 6, seg=10),
              tube(MX.zig((-0.36, 0.1, 0.06), (-0.43, 0.12, 0.22), 0.02, side=(0, 1, 0)), [0.014] * 6, seg=10)]
    k.add("mxsparkc", MX.join(sparks[:1]), MX.CYAN, "root", fuse=False, glow=MX.CYAN)
    k.add("mxsparkv", MX.join(sparks[1:]), "#C890FF", "root", fuse=False, glow="#C890FF")

    # A curl of crimson cloud beyond each end of the crystals' arc, behind the raised hands
    for s in (1, -1):
        pts = [(s * 0.40, 0.07, 0.84), (s * 0.45, 0.09, 0.88), (s * 0.43, 0.1, 0.93)]
        MX.puffs(k, f"mxcurl{'R' if s > 0 else 'L'}", pts, [0.04, 0.034, 0.026], "head",
                 colours=(MX.CRIMSON, MX.DEEP))


_build, _laid = Kit.build, []
def build(self, *a, **kw):
    if not _laid:                   # the first build is the body's; the eyes' five states build after it
        _laid.append(True)
        maxed_alakazam(self)
    return _build(self, *a, **kw)
Kit.build = build

sibling("alakazammega")             # builds as it is read, with this script's own arguments
