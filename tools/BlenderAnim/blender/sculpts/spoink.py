# Spoink sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/spoink.py -- <the export's Spoink.glb> <out.glb>
# Written to be grown from: grumpig.py imports its frame (sibling("spoink")): the snout, the nostrils, the spring and
# the pearl.
#
# The design, from the references: a grey pig that is all one egg of a body, wider below, with two small pointed ears
# at its top corners; on the lower face a big flat oval pig's snout with two dark nostrils; small black eyes with a
# glint above it, pink blushes on the cheeks; two small flippers of arms out from the sides and down; a big pink pearl
# sitting on its crown, a little to the right; under it a grey coiled spring of three turns that is its tail, its end
# trailing out along the ground.
# The export's joints: body (0, 0, 0.22), head (0, 0, 0.3), arms (+-0.06, -0.02, 0.2), ears (+-0.05, 0, 0.28); no legs,
# no tail bone: the spring goes on the body.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, args, outward, blade, boxy

GREY, SNOUT, NOSTRIL = "#A2A2AA", "#8E8E98", "#3A3440"
PINK, BLUSH, PEARL = "#F2A2B6", "#F2A0B4", "#F4AABE"
INK, WHITE = "#1C1820", "#FFFFFF"

# --- the frame -------------------------------------------------------------------------------------------------------
B = np.array([0, 0.0, 0.212])                 # the egg's middle
BR = np.array([0.09, 0.082, 0.104])
TOP = B[2] + BR[2]


def egg(V):
    """Wider below its middle, narrower above."""
    V = V.copy()
    s = 1 + 0.1 * np.clip(-V[:, 2], 0, 1) - 0.1 * np.clip(V[:, 2], 0, 1) ** 1.5
    V[:, 0] *= s; V[:, 1] *= s
    return V


def on_egg(x, z, c=B, r=BR, deform_s=None):
    """A point on the egg's front at x, z (the deform's widening counted) and its outward normal."""
    t = (z - c[2]) / r[2]
    s = 1 + 0.1 * max(-t, 0) - 0.1 * max(t, 0) ** 1.5
    rx, ry = r[0] * s, r[1] * s
    q = 1 - t * t - (x / rx) ** 2
    y = c[1] - ry * math.sqrt(max(q, 0.0))
    n = unit(x / rx ** 2, (y - c[1]) / ry ** 2, t / r[2] * 0.6)
    return np.array([x, y, z]), n


def spring(k, base, radius, turns, pitch, r, color, bone, start=0.0, trail=None, name="spring", surface=None):
    """
    A coiled spring of a tube r thick rising from base (its bottom's middle), turns of radius each pitch high, its
    top running in to the middle under the body; trail: how many turns its bottom end runs on along the ground.
    """
    base = np.array(base, float)
    n = int(turns * 28) + 1
    th = start + np.linspace(0, 2 * math.pi * turns, n)
    z = base[2] + r + pitch * (th - start) / (2 * math.pi)
    pts = np.stack([base[0] + radius * np.sin(th), base[1] - radius * np.cos(th), z], 1)
    top = pts[-1]
    pts = np.vstack([pts, top * 0.4 + np.array([base[0], base[1], top[2] + pitch * 0.9]) * 0.6,
                     [base[0], base[1], top[2] + pitch * 2.0]])
    rads = [r] * len(pts)
    if trail:
        # the bottom end runs on round the coil the other way, out and away along the ground, thinning to its tip
        m = 12
        t = np.linspace(1, 0, m, endpoint=False)[::-1]          # 0 at the tip .. near 1 at the coil
        a = start - trail * 2 * math.pi * (1 - t)
        rr = radius * (1 + 1.3 * (1 - t) ** 1.3)
        tz = base[2] + r * (0.6 + 0.4 * t)
        tail = np.stack([base[0] + rr * np.sin(a), base[1] - rr * np.cos(a), tz], 1)
        pts = np.vstack([tail, pts])
        rads = list(r * (0.68 + 0.32 * t)) + rads
    k.add(name, outward(tube(pts, rads, seg=14, per=4)), color, bone, fuse=False, surface=surface)
    return pts


def pearl(k, c, r, color, bone, glow=True, name="pearl", surface="shell"):
    """A round pearl, a loose glowing ball, with a small white glint on its upper front."""
    c = np.array(c, float)
    k.add(name, ell(c, (r, r, r), sub=4), color, bone, fuse=False, glow=color if glow else None, surface=surface)
    g = c + unit(-0.45, -0.6, 0.65) * r * 0.86
    k.add(name + "glint", ell(g, (r * 0.26, r * 0.18, r * 0.2), rot=(40, 0, -30), sub=2), WHITE, bone, fuse=False,
          surface=surface)


def snout(k, c, r, color, bone, nostril=NOSTRIL, tilt=12, name="snout"):
    """A pig's snout: a flat round-ended oval standing out of the face, its front a little up; two dark nostrils."""
    c = np.array(c, float)
    k.add(name, ell(c, r, rot=(tilt, 0, 0), deform=boxy(2.6)), color,
          bone)
    d = unit(0, -math.cos(math.radians(tilt)), math.sin(math.radians(tilt)))
    front = c + d * r[1] * 0.95
    for s in (1, -1):
        k.decal(front + np.array([0.36 * r[0] * s, 0, 0]), d, (r[0] * 0.16, r[2] * 0.36), nostril, eps=0.004,
                rings=6, segs=28)
    return front, d


def round_eye(ink, glint=WHITE):
    """A small solid black eye with a white glint at its upper outer side."""
    K, G = srgb(ink), srgb(glint)
    def look(u, v):
        c = np.tile(K, (len(u), 1))
        c[((u - 0.12) / 0.34) ** 2 + ((v - 0.32) / 0.34) ** 2 < 1] = G
        return c
    return look


def spoink(src, out):
    k = Kit(src, coat="fur")
    # The egg of a body; the upper half on the head's bone so that the pearl and the face go with it
    k.add("body", ell(B, BR, deform=egg), GREY, "body")
    k.add("crown", ell(B + (0, 0.004, 0.05), (0.07, 0.064, 0.055)), GREY, "head")
    # Two small pointed ears at the top corners, out and up
    for s, bone in ((1, "earR"), (-1, "earL")):
        a = np.array([0.054 * s, 0.006, 0.282]); t = np.array([0.08 * s, 0.01, 0.328])
        k.add("ear" + bone[-1], blade([a, a + (t - a) * 0.5, t], [0.026, 0.017, 0.004], (0, 1, 0), 0.6), GREY, bone)
    # Two small flippers out from the sides, down and a little forward
    for s, bone in ((1, "armR"), (-1, "armL")):
        a = np.array([0.075 * s, -0.012, 0.205]); t = np.array([0.128 * s, -0.03, 0.17])
        k.add("arm" + bone[-1], blade([a, a + (t - a) * 0.5, t], [0.026, 0.02, 0.012], (0, 0, 1), 0.55), GREY, bone)

    # The snout and the blushes
    sc, sd = snout(k, (0, -0.07, 0.196), (0.042, 0.026, 0.028), SNOUT, "body")
    for s in (1, -1):
        p, n = on_egg(0.064 * s, 0.214)
        k.decal(p, n, (0.019, 0.012), BLUSH, eps=0.004, rings=8, segs=32)

    # The pearl on the crown, a little to the right; the spring under the body
    pearl(k, (0.006, 0.006, TOP + 0.044), 0.05, PEARL, "head")
    spring(k, (0, 0.006, 0.0), 0.044, 2.9, 0.03, 0.0135, GREY, "body", start=3.9,
           trail=0.45, surface="metal")

    k.build(voxel=0.0042, band=0.005)

    # The eyes: small and black above the snout's corners
    p, n = on_egg(0.04, 0.255)
    def eyes(state):
        look = eye_look(state, round_eye(INK), srgb(GREY), srgb(INK), line=0.2)
        k.decal(p, n, (0.012, 0.016), look, eps=0.005, rings=20, segs=56, mirror=True)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    spoink(*args())
