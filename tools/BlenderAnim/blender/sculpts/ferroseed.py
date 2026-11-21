# Ferroseed sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/ferroseed.py -- <the export's Ferroseed.glb> <out.glb>
#
# The design, from the references: a seed of iron, an egg taller than wide and a little narrower at the top, steel
# grey with three dark bands round it (one high on the crown, one just over the eyes, one low). Green spikes stand off
# it all round, one of them low on the brow pointing forward and up; small dark chips are scattered over the steel.
# Two half-lidded eyes in the middle of its front, yellow-orange with a black upright pupil, their top edge cut flat,
# each in a green six-sided patch. No mouth.
#
# Written to be grown from (ferrothorn.py imports it): the colours, a spike, a chip, an eye in its patch.
# Skeleton: one bone, body (0, 0, 0.16): everything is on it.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, lathe, srgb, inside, unit, eye_look, polygon, args

STEEL, BAND, CHIP = "#ADB0B5", "#3C3E44", "#4A4C52"
GREEN, GREEN_D, EYE, INK = "#48A874", "#2E7E54", "#F2B230", "#141416"

C = np.array([0.0, 0.0, 0.2])          # the egg's middle
RXY, RZ = 0.112, 0.145                   # its radii, round in plan
TAPER = 0.09                             # how much narrower its top is than its bottom
VOXEL = 0.0035


def egg(V):
    V = V.copy()
    V[:, :2] *= (1 - TAPER * V[:, 2:3])
    return V


def surf(az, el):
    """The egg's surface at azimuth az (degrees from the front toward +X) and elevation el, and its outward way."""
    a, e = math.radians(az), math.radians(el)
    z = math.sin(e)
    w = math.cos(e) * (1 - TAPER * z)
    d = np.array([math.sin(a) * w * RXY, -math.cos(a) * w * RXY, z * RZ])
    n = np.array([math.sin(a) * math.cos(e) / RXY, -math.cos(a) * math.cos(e) / RXY, z / RZ])
    return C + d, n / np.linalg.norm(n)


# --- the line's pieces ---------------------------------------------------------------------------------------------
def spike(k, name, root, d, length, radius, bone="body", colour=GREEN, surface="shell", seg=10):
    """A solid cone from a base sunk behind root out along d, its sides a little full."""
    d = unit(*d)
    root = np.array(root, float)
    k.add(name, lathe(root, d, [(0, -radius * 0.9), (radius, -radius * 0.4), (radius * 0.72, length * 0.4),
                                (radius * 0.3, length * 0.78), (0, length)], seg=seg), colour, bone, fuse=False,
          surface=surface)


CHIP_SHAPES = [polygon([(1, 0.2), (0.1, 1), (-0.9, 0.4), (-0.4, -0.9), (0.7, -0.7)]),
               polygon([(1, -0.3), (0.5, 0.9), (-1, 0.5), (-0.3, -1)]),
               polygon([(0.9, 0.6), (-0.5, 1), (-1, -0.4), (0.4, -1)])]


def chip(k, p, n, half, i, colour=CHIP, eps=0.002):
    """A small dark chip in the steel, an uneven four- or five-sided patch."""
    k.decal(p, n, (half, half * 0.85), colour, up=unit(math.sin(i * 1.7), 0.2, math.cos(i * 1.7)) if abs(n[2]) < 0.9
            else (0, 1, 0), eps=eps, outline=CHIP_SHAPES[i % 3], rings=6, segs=30)


# the eye (u outward, v up): its top edge cut flat by the lid, sloping a little down to the inner corner
EYE_OUT = [(1.0, 0.5), (1.0, 0.0), (0.62, -0.78), (0.0, -1.0), (-0.62, -0.8), (-1.0, -0.15), (-1.0, 0.3)]
# the green patch round it: six sides, a flat top, pointed under
PATCH_OUT = [(1.0, 0.75), (1.0, -0.15), (0.4, -1.0), (-0.45, -1.0), (-1.0, -0.25), (-1.0, 0.62)]


def eye_open(u, v):
    c = np.tile(srgb(EYE), (len(u), 1))
    c[((u + 0.04) / 0.24) ** 2 + ((v + 0.12) / 0.72) ** 2 < 1] = srgb(INK)
    return c


def flipped(f):
    return lambda u, v: f(-u, v)


def patches(k, places, half, eps=0.002):
    """The green patches, one a side, which never blink: places is [(point, normal)] on +X then -X."""
    for s, (p, n) in zip((1, -1), places):
        out = PATCH_OUT if s > 0 else [(-a, b) for a, b in PATCH_OUT]
        k.decal(p, n, half, GREEN, eps=eps, outline=polygon(out), rings=10, segs=48)


def eyes(k, state, places, half, eps=0.0034):
    for s, (p, n) in zip((1, -1), places):
        look = eye_look(state, eye_open, srgb(GREEN), srgb(INK), line=0.17)
        out = EYE_OUT if s > 0 else [(-a, b) for a, b in EYE_OUT]
        k.decal(p, n, half, look if s > 0 else flipped(look), eps=eps, outline=polygon(out), rings=20, segs=56)


# --- Ferroseed -----------------------------------------------------------------------------------------------------
SPIKES = [  # (azimuth, elevation) on the egg, the spike pointing straight out
    (0, 88), (60, 62), (180, 64), (-70, 60), (120, 50),
    (35, 30), (-35, 30), (95, 28), (-100, 26), (150, 22), (-150, 30),
    (75, -6), (-75, -4), (130, -12), (-130, -10), (180, -6),
    (40, -42), (-40, -40), (110, -45), (-110, -48), (180, -46),
    (0, -78), (120, -75), (-120, -76),
]
BANDS = (0.78, 0.21, -0.4)              # the bands' middles, in shares of RZ above C
BAND_W = 0.0115                         # their half width
EYE_EL, EYE_AZ = -2.0, 25.0
CHIPS = [(-12, 55), (24, 44), (-48, 43), (70, 40), (-50, -36), (55, -24), (-62, -20), (14, -58), (-20, -66),
         (100, 8), (-110, 10), (160, 40), (-160, -20), (140, -55)]


def ferroseed(src, out):
    k = Kit(src, coat="metal")
    k.add("egg", ell(C, (RXY, RXY, RZ), deform=egg, sub=5), STEEL, "body")
    for zb in BANDS:
        k.paint(lambda P, zb=zb: inside(np.abs(P[:, 2] - (C[2] + zb * RZ)) - BAND_W, 0.0018), BAND)
    for i, (az, el) in enumerate(SPIKES):
        p, n = surf(az, el)
        spike(k, f"spike{i}", p, n, 0.06, 0.022)
    # the one on the brow, low, pointing forward and up
    p, n = surf(0, 50)
    spike(k, "brow", p, n + np.array([0, -0.35, 0.2]), 0.05, 0.02)
    for i, (az, el) in enumerate(CHIPS):
        p, n = surf(az, el)
        chip(k, p, n, 0.0095, i)
    places = [surf(s * EYE_AZ, EYE_EL) for s in (1, -1)]
    places = [(p, unit(*(n + np.array([0, -0.4, 0])))) for p, n in places]
    patches(k, places, (0.036, 0.031))
    k.build(voxel=VOXEL, smooth=3, band=0.005)
    k.eye_states(lambda state: eyes(k, state, places, (0.026, 0.018)))
    k.export(out)


if __name__ == "__main__":
    ferroseed(*args())
