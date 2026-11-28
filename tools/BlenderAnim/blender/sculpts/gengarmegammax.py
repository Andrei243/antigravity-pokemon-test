# Mega Dynamax Gengar (a form of our own, plan 48): Mega Gengar's own sculpt (gengarmega.py, which builds as it is
# read), whole and unchanged, with what the form adds laid on its kit just before it is meshed (sculptkit.py), with
# the helpers of raichumegaxmmax.py (the emblem, the clouds, the puffs):
#   blender --background --factory-startup --python sculpts/gengarmegammax.py -- <the export's Gengar-Mega-Mmax.glb> <out.glb>
#
# The design is one picture of the project's own (docs/art/reference/gemini/megadynamax/Mega-Dynamax-Gengar.jpg):
# Gengar rising out of a pool of shadow, the largest thing added, dark and swirling with motes of violet and cyan light
# in it and a rim of crimson and violet cloud; four more arms of shadow reaching up out of the pool round it with glowing
# violet claws; a crystal of cyan light on the brow; the Key Stone's emblem on the belly; a broken ring of crimson cloud
# floating over the head. The picture sets the crystal where the Mega's third eye is: the third eye is a marking of the
# Mega's face and stays, and the crystal stands on the brow just above it.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, lathe, unit, sibling

MX = sibling("raichumegaxmmax")

POOL, SWIRL, SHADOW, CLAW = "#1C1230", "#3E2868", "#2A1C44", "#B064F0"
MOTES = ("#9E6CF4", "#5EE2EC")
CRYSTAL = "#6EDEF4"


def claws(hand, up, side, n=3, length=0.085, spread=0.02):
    """Glowing claws out of a hand of shadow, along up and fanned across side."""
    up, side = unit(*up), unit(*side)
    other = np.cross(up, side)
    out = []
    for j in range(n):
        o = (j - (n - 1) / 2) * spread
        base = hand + side * o * 1.4 + up * 0.02
        tip = base + unit(*(up + side * o * 6)) * length
        w = 0.016
        out.append(hull([base + side * w, base - side * w, base + other * w, base - other * w, tip]))
    return out


def maxed_gengar(k):
    # Numbers from gengarmega.py (it builds as it is read): the body's middle C (0, 0, 0.45) with radii (0.28, 0.25,
    # 0.285); the belly an ellipsoid at (0, 0.01, 0.38) with radii (0.274, 0.245, 0.271); the head at (0, -0.005,
    # 0.48); the third eye at (0, -0.245, 0.535) facing (0, -1, 0.45), half 0.042; the grin's lower edge at z 0.253; the
    # hands at (0.37, -0.15, 0.15) with flames rising round them, the feet at (0.15, -0.05, 0.05)

    # The pool of shadow on the ground, its rim a little raised; a swirl of lighter violet on it and motes of light
    V, F = lathe((0, -0.02, 0), (0, 0, 1), [(0, 0), (0.43, 0), (0.44, 0.012), (0.41, 0.022), (0, 0.024)], seg=48)
    V = V * np.array([1, 0.92, 1])
    k.add("mxpool", (V, F), POOL, "root", fuse=False)
    swirl = []
    for arm in range(2):
        pts = []
        for t in np.linspace(0, 1, 22):
            a = arm * math.pi + 2.2 * math.pi * t
            r = 0.1 + 0.29 * t
            pts.append((r * math.cos(a), -0.02 + 0.92 * r * math.sin(a), 0.026))
        swirl.append(tube(pts, [0.014] * len(pts), seg=8))
    k.add("mxswirl", MX.join(swirl), SWIRL, "root", fuse=False)
    rnd = np.random.default_rng(5)
    motes = {c: [] for c in MOTES}
    for i in range(14):
        a = rnd.uniform(0, 2 * math.pi)
        r = rnd.uniform(0.25, 0.4)
        motes[MOTES[i % 2]].append(ell((r * math.cos(a), -0.02 + 0.92 * r * math.sin(a), 0.028),
                                       (0.014, 0.014, 0.01), sub=2))
    for j, (c, shapes) in enumerate(motes.items()):
        k.add(f"mxmote{j}", MX.join(shapes), c, "root", fuse=False, glow=c)

    # Four arms of shadow reaching up out of the pool round it, at the sides and behind, clear of its own arms: each
    # bows out at the elbow and bends in again at the wrist, the claws curled toward it
    sh, cl = [], []
    for s in (1, -1):
        side = [(s * 0.37, 0.02, 0.045), (s * 0.44, 0.03, 0.16), (s * 0.5, 0.02, 0.3), (s * 0.5, 0.0, 0.42),
                (s * 0.46, -0.03, 0.52)]
        back = [(s * 0.27, 0.32, 0.045), (s * 0.35, 0.36, 0.2), (s * 0.42, 0.36, 0.38), (s * 0.43, 0.32, 0.54),
                (s * 0.4, 0.27, 0.64)]
        for path, hand in ((side, np.array([s * 0.44, -0.05, 0.57])), (back, np.array([s * 0.38, 0.25, 0.69]))):
            sh.append(tube(path, [0.06, 0.05, 0.042, 0.036, 0.032], seg=14))
            sh.append(ell(hand, (0.045, 0.035, 0.045), sub=3))
            cl.extend(claws(hand, (-s * 0.25, -0.2, 1), (1, 0, 0)))
    k.add("mxshadowarm", MX.join(sh), SHADOW, "root", fuse=False)
    k.add("mxclaw", MX.join(cl), CLAW, "root", fuse=False, glow=CLAW)

    # A crystal of cyan light on the brow, above the third eye (whose top is at about z 0.575)
    up = unit(0, 0.41, 0.912)
    nb = unit(0, -0.82, 0.57)
    c = np.array([0, -0.205, 0.64])
    k.add("mxbrowgem", hull([c + up * 0.06, c - up * 0.035, c + np.array([0.034, 0, 0]), c - np.array([0.034, 0, 0]),
                             c + nb * 0.028 + up * 0.006, c - nb * 0.02]), CRYSTAL, "head", fuse=False, glow=CRYSTAL)

    # The emblem on the belly, under the grin
    MX.emblem(k, (0, -0.168, 0.2), (0, -0.78, -0.63), 0.045, "#8CC8FF", "#EADCFF", "#DCA0FF", "body")

    # The clouds round the pool's rim, and a broken ring of crimson cloud over the head
    MX.clouds(k, 0.0, -0.02, 0.48, 0.44, 6, 0.058, 41, rider=False)
    over = []
    for a0 in (30, 120, 210, 300):
        for t, sz in ((0, 0.034), (24, 0.028), (44, 0.02)):
            a = math.radians(a0 + t)
            over.append(((0.17 * math.cos(a), 0.08 + 0.15 * math.sin(a), 0.94), sz))
    MX.puffs(k, "mxover", [p for p, _ in over], [s for _, s in over], "head", colours=(MX.CRIMSON, MX.CRIMSON, MX.DEEP))


_build, _laid = Kit.build, []
def build(self, *a, **kw):
    if not _laid:                   # the first build is the body's; the eyes' five states build after it
        _laid.append(True)
        maxed_gengar(self)
    return _build(self, *a, **kw)
Kit.build = build

sibling("gengarmega")               # builds as it is read, with this script's own arguments
