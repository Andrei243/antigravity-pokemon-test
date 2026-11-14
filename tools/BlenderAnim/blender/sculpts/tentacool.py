# Tentacool sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/tentacool.py -- <the export's Tentacool.glb> <out.glb>
#
# From the references: a jellyfish. A smooth blue dome with a large red crystal on each upper side and a smaller red
# crystal set lower between them, small round dark eyes low and wide apart, a short pale grey beak at the bottom
# middle between two rounded lower flaps; under the dome a blue skirt that flares out at the sides and back, dark
# underneath; two long grey-brown tentacles hanging from under it, each ending in a flat spoon of a tip.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, sheet, polygon, srgb, inside, smoothstep, unit, eye_look, args

src, out = args()
k = Kit(src, coat="scales")

BLUE, RED, REDHI, DARK, GREY, BEAK, INK = ("#43B2EA", "#DE2A40", "#F58A98", "#1F2A3A", "#8E8076", "#9DA3AD",
                                           "#1A1E28")
H = np.array([0.0, 0.0, 0.64])           # the dome's centre
HR = np.array([0.15, 0.148, 0.16])


# --- the dome, its lower face and the flat underside -------------------------------------------------------------
k.add("dome", ell(H, HR), BLUE, "body")
# The lower body under the dome, where the face is, one mass with it; flat underneath
def flatbase(V):
    V = V.copy()
    V[:, 2] = np.maximum(V[:, 2], -0.55)
    return V
k.add("lower", ell((0, -0.004, 0.535), (0.147, 0.144, 0.1), deform=flatbase), BLUE, "body")
# Two rounded lower flaps at the bottom of the front, the beak between them
for s in (1, -1):
    k.add(f"flap{'R' if s > 0 else 'L'}", ell((0.042 * s, -0.122, 0.505), (0.04, 0.03, 0.03)), BLUE, "body")
# The underside, dark
k.paint(lambda P: inside(P[:, 2] - 0.488, 0.004), DARK, on=["lower", "dome"])

# --- the skirt: a flared cape round the bottom, widest behind, dark on its inner side -----------------------------
def skirt(inner):
    def at(u, v):
        th = 2 * math.pi * u                          # the seam at the back
        back = 0.5 + 0.5 * np.cos(th)
        v = 0.015 + 0.97 * v
        r0 = 0.125
        r1 = (0.155 + 0.11 * back) * (1 + 0.035 * np.cos(6 * th)) - (0.014 if inner else 0)
        z0, z1 = 0.5, 0.43 - 0.025 * back
        f = v ** 0.8
        r = r0 + (r1 - r0) * f - (0.006 if inner else 0) * (1 - f)
        z = z0 + (z1 - z0) * (0.2 * v + 0.8 * v * v)
        return np.stack([r * np.sin(th), r * np.cos(th), z], -1)
    return at
k.add("skirt", sheet(skirt(False), 96, 14, 0.016), BLUE, "body", fuse=False)
k.add("skirtin", sheet(skirt(True), 96, 14, 0.01), DARK, "body", fuse=False)

# --- the crystals: a big one on each upper side, a smaller one at the front, each with a soft shine on its face ---
def shine(c, R, h, r):
    """A flat disc of light lying on a crystal of radius R about c, toward h."""
    h = h / np.linalg.norm(h)
    a = np.cross(h, (0, 0, 1.0)) if abs(h[2]) < 0.95 else np.cross(h, (1.0, 0, 0))
    a /= np.linalg.norm(a); b = np.cross(h, a)
    V, F = ell((0, 0, 0), (r, r * 0.8, r * 0.3))
    return V @ np.stack([a, b, h], axis=0) + c + h * R * 0.95, F
for s in (1, -1):
    d = unit(0.66 * s, -0.36, 0.66)
    c = H + d * HR * 0.86
    k.add(f"gem{s}", ell(c, (0.083, 0.083, 0.083)), RED, "body", fuse=False, surface="shell")
    k.add(f"gemhi{s}", shine(c, 0.083, d + unit(-0.35 * s, -0.4, 0.5) * 0.7, 0.03), REDHI, "body", fuse=False,
          surface="shell")
d = unit(0, -0.93, 0.37)
c = H + d * HR * 0.95
k.add("gemfront", ell(c, (0.038, 0.038, 0.038)), RED, "body", fuse=False, surface="shell")
k.add("gemfronthi", shine(c, 0.038, d + np.array([0.25, -0.1, 0.45]), 0.013), REDHI, "body", fuse=False,
      surface="shell")

# --- the beak: a pale point hanging under the eyes ----------------------------------------------------------------
k.add("beak", blade([(0, -0.128, 0.512), (0, -0.15, 0.49), (0, -0.155, 0.468)], [0.026, 0.02, 0.009], (0, 1, 0),
                    0.7), BEAK, "body", fuse=False, surface="shell")

# --- the tentacles: long, hanging and curving out, each a flat spoon at the end -----------------------------------
for s, bone in ((1, "tentacleR"), (-1, "tentacleL")):
    p = [(0.06 * s, 0.0, 0.47), (0.125 * s, -0.02, 0.38), (0.165 * s, -0.03, 0.27), (0.155 * s, -0.01, 0.16),
         (0.195 * s, 0.01, 0.08)]
    k.add(f"tentacle{s}", tube(p, [0.021, 0.019, 0.016, 0.014, 0.013]), GREY, bone, fuse=False, surface="skin")
    tip = [(0.185 * s, 0.01, 0.085), (0.22 * s, 0.015, 0.055), (0.255 * s, 0.02, 0.047), (0.283 * s, 0.025, 0.06)]
    k.add(f"spoon{s}", blade(tip, [0.014, 0.027, 0.028, 0.007], (0, 0, 1), 0.38), GREY, bone, fuse=False,
          surface="skin")

# --- the eyes: small white almonds low on the front, a black pupil, the brow's line slanting down to the inside ---
CB, CW, CK = srgb(BLUE), np.array([1.0, 1.0, 1.0]), srgb(INK)
def eye(u, v):
    # a small round dark eye with one glint, no brow
    c = np.tile(CK, (len(u), 1))
    c[((u + 0.3) ** 2 + (v - 0.32) ** 2) < 0.3 ** 2] = CW
    return c
def eyes(state):
    dn = unit(0.6, -0.79, 0.04)
    e = np.array([0, -0.004, 0.565]) + dn * 0.135
    k.decal(e, dn, (0.024, 0.026), eye_look(state, eye, CB, CK, line=0.2), eps=0.004, rings=20, segs=56, mirror=True)

k.build(voxel=0.0055, smooth=3)
k.eye_states(eyes)
k.export(out)
