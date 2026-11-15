# Nuzleaf sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/nuzleaf.py -- <the export's Nuzleaf.glb> <out.glb>
#
# Seedot grown (seedot.py: its brown and its pale tan), from the references: an upright brown imp with a big round head;
# a pale band of a mask across the eyes that wraps round the head's sides, and out of its middle a long pale pointed
# nose; sly half-shut eyes (white, a black pupil against the flat top lid, a black outline); a small open pink mouth
# under the nose; one big green leaf from the top of the head, out to the side and back. A slim brown body, thin arms
# with big round fists, puffed pale shorts like two balls with grey stripes round them, short brown legs on wide feet
# with three toes.
# The skeleton: body (0, 0, 0.32), arms (+-0.07, 0, 0.36), head (0, -0.01, 0.4), legs (+-0.06, 0, 0.18).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, srgb, inside, smoothstep, unit, eye_look, polygon, ring_band, outward, leaf,
                       slab, sibling, args)

seed = sibling("seedot")
BROWN, INK = "#A87650", seed.INK                      # Seedot's brown grown warmer, the same ink
MASK, SHORTS, STRIPE = "#E2D6C4", "#E6DED0", "#8E8A86"
GREEN, GREEN_D, PINK, MOUTH, WHITE = "#4FB054", "#3A8E42", "#E89098", "#5A2028", "#FAFAF6"

HEAD_C = np.array([0.0, -0.012, 0.492])
HEAD_R = np.array([0.162, 0.148, 0.15])
EYE_Z = 0.508                          # the mask's middle, where the eyes are


def head(k):
    k.add("head", ell(HEAD_C, HEAD_R, sub=5), BROWN, "head")
    k.add("neck", tube([(0, 0.0, 0.33), (0, -0.006, 0.37)], [0.045, 0.05]), BROWN, "head")

    # the mask: a band round the head at the eyes, a little wider in front, its lower edge dropping to the nose
    def mask(P):
        d = P - HEAD_C
        az = np.arctan2(d[:, 0], -d[:, 1])               # 0 straight ahead
        front = np.cos(az).clip(0, 1)
        top = EYE_Z + 0.046 + 0.006 * front
        bottom = EYE_Z - 0.044 - 0.022 * np.exp(-(az / 0.45) ** 2)
        # it ends behind the ears' place in a point: the band narrows to nothing toward the back
        back = smoothstep(0.3, -0.25, np.cos(az))
        mid = (top + bottom) / 2
        half = (top - bottom) / 2 * (1 - back)
        return inside(np.abs(P[:, 2] - mid) - half, 0.003) * smoothstep(-0.3, -0.15, np.cos(az))
    k.paint(mask, MASK, on=["head"])

    # the nose: a long pale cone out of the mask's middle, forward and a little up
    root = np.array([0, HEAD_C[1] - HEAD_R[1] + 0.026, EYE_Z - 0.012])
    d = unit(0, -1, 0.42)
    k.add("nose", tube([root, root + d * 0.07, root + d * 0.14], [0.046, 0.03, 0.009]), MASK, "head")

    # the mouth: a small open oval, pink in a dark rim, under the nose
    CB, CM, CP = srgb(BROWN), srgb(MOUTH), srgb(PINK)
    def mouth(u, v):
        r = u * u + v * v
        c = np.tile(CB, (len(u), 1))
        c[r < 1] = CM
        c[(u * u + ((v + 0.25) / 0.7) ** 2) < 0.45] = CP
        return c
    k.decal((0, -0.15, 0.415), unit(0, -1, -0.3), (0.026, 0.03), mouth, eps=0.004, rings=14, segs=48)


def the_leaf(k):
    """One big leaf out of the top of the head, up and out to +X and back: a thick plate, a darker rib on its face."""
    base = HEAD_C + np.array([0.025, 0.035, 0.135])
    d = unit(0.82, 0.38, 0.42)
    side = unit(*np.cross(d, (0, 0, 1)))
    ez = np.cross(side, d)                              # in the leaf's plane, across it, toward the sky
    ez = np.cross(unit(0.1, -1, 0.35), d)
    L, W = 0.26, 0.062
    out = []
    for t in np.linspace(0, 1, 24):
        out.append((t * L, W * math.sin(math.pi * t ** 0.8) * (1 - 0.15 * t)))
    for t in np.linspace(1, 0, 24)[1:-1]:
        out.append((t * L, -W * math.sin(math.pi * t ** 0.8) * (1 - 0.15 * t)))
    k.add("leaf", slab(out, base, d, ez, 0.018), GREEN, "head", fuse=False, surface="leaf")
    rib = [(0.01, 0.004), (0.24, 0.0015), (0.24, -0.0015), (0.01, -0.004)]
    k.add("leafrib", slab(rib, base, d, ez, 0.022), GREEN_D, "head", fuse=False, surface="leaf")
    k.add("stem", tube([HEAD_C + (0.0, 0.01, 0.12), base + d * 0.02], [0.02, 0.018]), GREEN_D, "head")


def body(k):
    k.add("chest", ell((0, -0.004, 0.3), (0.08, 0.064, 0.08)), BROWN, "body")
    k.add("waist", ell((0, 0.0, 0.235), (0.082, 0.064, 0.05)), BROWN, "body")


def arms(k):
    """Thin arms held forward and down, ending in big round fists."""
    sh = np.array([0.07, 0.0, 0.36])
    el = np.array([0.124, -0.055, 0.315])
    wr = np.array([0.136, -0.115, 0.29])
    k.add("arm", tube([sh, el, wr], [0.035, 0.029, 0.027]), BROWN, "arm{s}", mirror=True)
    k.add("fist", ell((0.142, -0.148, 0.285), (0.054, 0.056, 0.054), rot=(0, 0, 20)), BROWN, "arm{s}", mirror=True)


def legs(k):
    """The puffed shorts (two pale balls with grey stripes), short brown legs, wide feet with three toes."""
    SC = np.array([0.078, -0.006, 0.142])
    SR = (0.098, 0.092, 0.085)
    k.add("shorts", ell(SC, SR), SHORTS, "leg{s}", mirror=True)
    k.add("seat", ell((0, 0.0, 0.2), (0.11, 0.075, 0.055)), SHORTS, "body")
    for i, dz in enumerate((0.03, -0.032)):
        r = SR[0] * math.sqrt(1 - (dz / SR[2]) ** 2)
        k.add(f"stripe{i}", ring_band(SC + (0, 0, dz), (0, 0, 1), r - 0.0015, 0.006, thick=0.004), STRIPE, "leg{s}",
              fuse=False, mirror=True, surface="cloth")
    k.add("shin", tube([(0.082, -0.012, 0.075), (0.084, -0.02, 0.04)], [0.042, 0.038]), BROWN, "leg{s}", mirror=True)
    k.add("foot", ell((0.086, -0.045, 0.022), (0.052, 0.07, 0.022)), BROWN, "leg{s}", mirror=True)
    for j, dx in enumerate((-0.022, 0.0, 0.022)):
        k.add(f"toe{j}", ell((0.086 + dx * 1.3, -0.11, 0.019), (0.019, 0.024, 0.019)), BROWN, "leg{s}", mirror=True)


# The eyes: half shut, white with a black pupil pressed up to the flat top lid at the inner side, outlined in black
EYE_HALF = (0.046, 0.029)
CM, CW, CI = srgb(MASK), srgb(WHITE), srgb(INK)


def eye_open(u, v):
    c = np.tile(CI, (len(u), 1))
    top = 0.55 - 0.1 * u
    bottom = -0.75 + 0.35 * u * u
    white = (v < top - 0.2) & (v > bottom + 0.18) & (np.abs(u) < 0.84)
    c[white] = CW
    c[white & (((u + 0.12) / 0.34) ** 2 + ((v - 0.15) / 0.62) ** 2 < 1)] = CI
    outside = (v > top) | (v < bottom) | (np.abs(u) > 0.98)
    c[outside] = CM
    return c


def eyes(k, state):
    p = np.array([0.074, -0.145, EYE_Z + 0.008])
    n = unit(0.5, -1, 0.05)
    k.decal(p, n, EYE_HALF, eye_look(state, eye_open, CM, CI, line=0.2), eps=0.004, rings=20, segs=56, mirror=True)


def nuzleaf(src, out):
    k = Kit(src, coat="fur")
    head(k)
    the_leaf(k)
    body(k)
    arms(k)
    legs(k)
    k.paint(lambda P: inside(np.abs(P[:, 2] - 0.142) - 0.085, 0.003) * inside(np.abs(P[:, 0]) - 0.18), SHORTS,
            on=["shorts"])
    k.build(voxel=0.0055, smooth=4, band=0.006)
    k.eye_states(lambda state: eyes(k, state))
    k.export(out)


if __name__ == "__main__":
    nuzleaf(*args())
