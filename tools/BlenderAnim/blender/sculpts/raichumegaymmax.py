# Mega Dynamax Raichu Y (a form of our own, plan 48): Mega Raichu Y's own sculpt (raichumegay.py), whole and unchanged,
# with what the form adds laid on its kit just before it is meshed (sculptkit.py), with the helpers of
# raichumegaxmmax.py (the emblem, the clouds, the puffs, the bolts):
#   blender --background --factory-startup --python sculpts/raichumegaymmax.py -- <the export's Raichu-Mega-Y-Mmax.glb> <out.glb>
#
# The design is one picture of the project's own (docs/art/reference/gemini/megadynamax/Mega-Dynamax-Raichu-Y.jpg). It
# draws Raichu's own body and ears with a long tail ending in a bolt; taken from it is only what it adds over the Mega:
# the largest, a ring of glass bubbles held round the body at the belly, a spark of yellow light in each and threads of
# lightning between them; glowing yellow gems on the cheeks; the Key Stone's emblem on the belly in gold; a broken ring
# of crimson cloud over the head; clouds of Max energy on the ground under it.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, lathe, unit, sibling, args

MX = sibling("raichumegaxmmax")

GLASS, SPARK, THREAD, GEM = "#DCEAF8", "#FFE45A", "#FFE680", "#FFE860"


def maxed_raichu_y(k):
    # Numbers from raichumegay.py's raichu_mega_y: DZ 0.128, the head H (0, -0.012, 0.657) with radii (0.156, 0.136,
    # 0.127), the cheeks' direction (0.74, -0.6, -0.3), the paws at (0.172, -0.112, 0.45), the tail's bolt from
    # (0.152, 0.245, 0.3) back and out along (0.6, 0.8, 0)
    H, HR = np.array([0, -0.012, 0.657]), np.array([0.156, 0.136, 0.127])
    def oh(d, lift=0.0):
        d = unit(*d)
        return H + d * (1 / math.sqrt(((d / HR) ** 2).sum()) + lift)

    # The ring of glass bubbles round the body, the back a little higher than the front, a gap of 120 degrees before
    # the belly so that the emblem shows
    n, R = 7, 0.05
    ring, glass, sparks = [], [], []
    for i in range(n):
        a = math.radians(-30 + 40 * i)
        y = 0.02 + 0.29 * math.sin(a)
        p = np.array([0.33 * math.cos(a), y, 0.46 + 0.06 * math.sin(a)])
        ring.append(p)
        glass.append(ell(p, (R, R, R), sub=3))
        o = unit(math.cos(a), math.sin(a), 0.25)
        sparks.append(ell(p + o * R * 0.55, (R * 0.6, R * 0.6, R * 0.6), sub=3))
    k.add("mxbubble", MX.join(glass), GLASS, "body", fuse=False, glow=GLASS)
    k.add("mxbubblespark", MX.join(sparks), SPARK, "body", fuse=False, glow=SPARK)
    threads = []
    for i in range(n - 1):            # none across the gap before the belly
        a, b = ring[i], ring[i + 1]
        d = unit(*(b - a))
        pts = MX.zig(a + d * R * 0.9, b - d * R * 0.9, 0.018, n=4, side=(0, 0, 1))
        threads.append(tube(pts, [0.015] * len(pts), seg=10))
    k.add("mxthread", MX.join(threads), THREAD, "body", fuse=False, glow=THREAD)

    # Glowing gems on the cheeks: low six-sided stones over the yellow cheek patches
    dc = unit(0.74, -0.6, -0.3)
    k.add("mxgem", lathe(oh(dc, -0.004), dc, [(0, 0), (0.03, 0), (0.031, 0.008), (0.018, 0.015), (0, 0.016)], seg=6),
          GEM, "head", fuse=False, glow=GEM, mirror=True)

    # The emblem on the white belly (the body's pear stands at y -0.119 there), in gold
    MX.emblem(k, (0, -0.115, 0.37), (0, -1, 0.05), 0.05, "#F2C24A", "#FFF2C8", "#E8A030", "body")

    # A broken ring of crimson cloud floating over the head, clear of the ears (their points at z 0.957)
    over = []
    for j, a0 in enumerate((20, 110, 200, 290)):
        for t, sz in ((0, 0.036), (22, 0.03), (40, 0.022)):
            a = math.radians(a0 + t)
            over.append(((0.13 * math.cos(a), 0.03 + 0.13 * math.sin(a), 1.015 + 0.01 * math.sin(2 * a)), sz))
    MX.puffs(k, "mxover", [p for p, _ in over], [s for _, s in over], "head",
             colours=(MX.CRIMSON, MX.CRIMSON, MX.DEEP))

    # The clouds on the ground under it
    MX.clouds(k, 0.0, 0.1, 0.36, 0.3, 6, 0.06, 23, rider=False)


if __name__ == "__main__":
    _build, _laid = Kit.build, []
    def build(self, *a, **kw):
        if not _laid:               # the first build is the body's; the eyes' five states build after it
            _laid.append(True)
            maxed_raichu_y(self)
        return _build(self, *a, **kw)
    Kit.build = build
    m = sibling("raichumegay")
    m.raichu_mega_y(*args())
