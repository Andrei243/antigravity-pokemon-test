# Mega Dynamax Garchomp (Garchomp-Mega-Mmax), a form of this project's own, sculpted as Garchomp-Mega's script with
# the Mega Dynamax form's additions laid on its kit just before it is meshed (sculptkit.py):
#   blender --background --factory-startup --python sculpts/garchompmegammax.py -- <the export's Garchomp-Mega-Mmax.glb> <out.glb>
# The Mega whole and unchanged in shape, and over it, from the picture (Mega-Dynamax-Garchomp.jpg), largest first: a
# great blade of violet crystal rising from the back past the head, a smaller pale one beside it; the arms' scythes
# turned to glowing violet crystal (the Mega's own parts, recoloured only); clouds of Max energy about the feet with
# cyan lightning in them and a broken ring of crimson puffs floating over the head; the Key Stone's emblem, a glowing
# disc in a ring, high on the red chest.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
import sculptkit
from sculptkit import Kit, ell, tube, lathe, ring_band, srgb, unit, sibling, args, hull

CRIMSON, DEEP, VIOLET, DVIOLET, CYAN = "#AC223E", "#701630", "#6C3EA0", "#42246E", "#4EE2EC"
AMETH, LIGHT = "#A46CEC", "#CFA2FA"
STONE, STONERING = "#FFD08A", "#7ADCFF"

# From garchompmega.py: the torso's front at z 0.70 lies at y -0.10 (tube radius 0.145, depth 0.82); the dorsal fin
# in the midline from y 0.04 to 0.4, z 0.62 to 1.0; the head's top at z about 1.1; the feet at x +-0.165, y -0.045,
# toes to y -0.2; the tail back to y 0.98 low; the scythes are the parts scytheR and scytheL; the voxel 0.008.


def shard(base, tip, wd, n, w, t):
    """A crystal blade from base to tip: its broad face square to n, w wide and t thick at its widest, a flat-faced
    hull with a kink of facets along it."""
    base, tip = np.array(base, float), np.array(tip, float)
    d = tip - base
    pts = []
    for f, ww, tt in ((0.0, 0.45, 0.6), (0.35, 1.0, 1.0), (0.7, 0.75, 0.8)):
        c = base + d * f
        for sw in (-1, 1):
            for st in (-1, 1):
                pts.append(c + wd * ww * w * sw * (1.0 if st > 0 else 0.85) + n * tt * t * st)
    pts.append(tip)
    pts.append(base - d * 0.06)
    return hull(pts)


def maxed(k):
    """What the Mega Dynamax form lays over its Mega: called with the Mega's kit just before it is meshed."""
    # ---- the scythes turned to crystal: the Mega's own parts, recoloured and lit, their shape untouched ------------
    for p in k.parts:
        if p["name"].startswith("scythe") and not p["name"].startswith("scytheback"):
            p["color"] = srgb(AMETH)
            p["glow"] = AMETH

    # ---- the back blade: rising from the upper back, up and back past the head, turned out toward +X ------------------
    wd = unit(0.85, 0.52, 0.0)
    n = unit(0.52, -0.85, 0.0)
    k.add("backblade", shard((0.05, 0.12, 0.78), (0.25, 0.34, 1.24), wd, n, 0.11, 0.032), AMETH, "body",
          fuse=False, glow=AMETH, surface="shell")
    k.add("backblade2", shard((-0.02, 0.15, 0.76), (-0.04, 0.4, 1.12), unit(-0.3, 0.95, 0), unit(0.95, 0.3, 0), 0.05,
                              0.022), LIGHT, "body", fuse=False, glow=LIGHT, surface="shell")

    # ---- the emblem high on the red chest: a glowing disc in a cyan ring ----------------------------------------------
    c = np.array([0.0, -0.106, 0.705])
    d = unit(0, -1, 0.1)
    k.add("emblem", lathe(c, d, [(0, -0.004), (0.036, -0.004), (0.036, 0.008), (0, 0.012)]), STONE, "body",
          fuse=False, glow=STONE)
    k.add("emblemring", ring_band(c + d * 0.002, d, 0.045, 0.013, thick=0.011), STONERING, "body", fuse=False,
          glow=STONERING)

    # ---- the clouds: fused mounds in a ring about the feet, clear of them ---------------------------------------------
    rnd = np.random.default_rng(3)
    nm = 10
    CX, CY, RX, RY = 0.0, -0.02, 0.37, 0.3
    for i in range(nm):
        a = 2 * math.pi * (i + 0.5) / nm
        col = (CRIMSON, VIOLET, DEEP, DVIOLET)[i % 4]
        other = (VIOLET, CRIMSON, DVIOLET, CRIMSON)[i % 4]
        for j in range(4):
            s = (0.075, 0.06, 0.05, 0.038)[j] * (1 + 0.12 * rnd.uniform(-1, 1))
            t = a + (0.0, 0.2, -0.2, 0.06)[j]
            dr = (0.0, 0.02, 0.015, -0.02)[j]
            p = (CX + (RX + dr) * math.cos(t), CY + (RY + dr) * math.sin(t), s * (0.85, 0.9, 0.85, 2.6)[j])
            k.add(f"mcloud{i}_{j}", ell(p, (s, s, s * 0.88)), other if j == 2 else col, "root")
    # cyan lightning in the clouds, front left and front right
    for s_ in (1, -1):
        a = math.radians(-90 + 55 * s_)
        q = np.array([CX + RX * math.cos(a), CY + RY * math.sin(a), 0.08])
        o = unit(math.cos(a), math.sin(a), 0)
        tg = np.cross((0, 0, 1.0), o)
        pts = [q + o * 0.03 + (0, 0, 0.1), q + tg * 0.04 + (0, 0, 0.065), q - tg * 0.02 + o * 0.03 + (0, 0, 0.035),
               q + tg * 0.05 + o * 0.06]
        for j in range(3):
            k.add(f"bolt{'RL'[s_ < 0]}{j}", tube([pts[j], pts[j + 1]], [0.017, 0.017], seg=8), CYAN, "root",
                  fuse=False, glow=CYAN)

    # ---- a broken ring of crimson puffs floating over the head, a little behind, and a wisp by the left shoulder -----
    # (behind the head and above it, so that the face stays clear from the front)
    for i, deg in enumerate((35, 80, 125, 165)):
        a = math.radians(deg)
        cc = np.array([-0.03 + 0.2 * math.cos(a), 0.13 + 0.15 * math.sin(a), 1.195 + 0.02 * math.sin(2 * a)])
        tg = np.array([-math.sin(a), math.cos(a), 0.0])
        for j, (dx, dz, r) in enumerate(((0, 0, 0.042), (0.034, 0.006, 0.033), (-0.032, 0.004, 0.03),
                                         (0.008, 0.03, 0.026))):
            k.add(f"halo{i}_{j}", ell(cc + tg * dx + np.array([0, 0, dz]), (r * 1.15, r * 1.15, r * 0.8), sub=3),
                  CRIMSON if i % 2 == 0 else DEEP, "body", fuse=False)


_build, _laid = Kit.build, []
def build(self, *a, **kw):
    if not _laid:               # the first build is the body's; the kit builds the eyes' five states after it
        _laid.append(True)
        maxed(self)
    return _build(self, *a, **kw)
Kit.build = build

m = sibling("garchompmega")
m.garchomp_mega(*args())
