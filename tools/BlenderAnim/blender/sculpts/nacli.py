# Nacli sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/nacli.py -- <the export's Nacli.glb> <out.glb>
#
# The design, from the references: a small white cube of rock salt sitting on the ground, nearly all of it hidden under
# a great lumpy brown rock that is twice and a half its width, banded like a stone cut through: a darker crown with a
# knob on its top, a pale cream band round its upper middle, salmon brown under it and a darker brown rim. Only the
# lowest strip of the cube's front shows under the rock, and there are its eyes: two upright orange bars with thick dark
# frames, side by side. From the rock's back on its right (+X) a white salt block juts up, leaning out and back.
# Where the pictures overruled the brief: there is no flat mushroom cap and there are no feet. The rock is a lump that
# covers the cube, and the cube sits straight on the ground.
#
# Written to be grown from (naclstack.py and garganacl.py import it with sibling): the line's colours, the salt block
# (box), the banded rock (rock_shape, bands), the diamond sparkle and the bar eye are here; the build is under the main
# guard.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from mathutils import Euler
from sculptkit import Kit, ell, srgb, unit, eye_look, args, hull, slab, polygon, inside, facets, axes

# --- the line's colours -------------------------------------------------------------------------------------------
SALT, SALT_GREY = "#F5F4F1", "#D9D7D4"              # the salt blocks, their shaded grey
CROWN, PALE = "#B07A60", "#EFD9CA"                  # the rock's crown, its pale band
MID, LOW = "#C08A6E", "#8E644F"                     # salmon brown under the band, the darker rim
DARK, DEEP = "#6A4C40", "#4E3830"                   # the grown forms' dark rock and its grooves
PINK = "#D9B3A2"                                    # the grown forms' pink salt
ORANGE, ORANGE_LT, FRAME = "#F2841A", "#FFB43C", "#2B2320"   # the eyes: fill, its lit top, the dark frame
SPARK = "#FBFAF7"                                   # the white diamonds on dark rock


# --- shapes -------------------------------------------------------------------------------------------------------
def box(c, half, rot=(0, 0, 0), cham=0.14):
    """A block of salt: a box of half sizes with its edges cut back a little (a hull, hard edges)."""
    pts = []
    for x in (-1, 0, 1):
        for y in (-1, 0, 1):
            for z in (-1, 0, 1):
                n = abs(x) + abs(y) + abs(z)
                if n < 2:
                    continue
                pts.append(np.array([x, y, z], float) * (1 - cham * (n - 1) / 2))
    P = np.array(pts) * np.array(half, float)
    m = np.array(Euler([math.radians(a) for a in rot]).to_matrix())
    return hull(P @ m.T + np.array(c, float))


def bar(a, d, half_w, half_t, length, roll=0.0, cham=0.12):
    """A block from a along d, length long, of half width and half thickness across it (a jutting block)."""
    d = unit(*d)
    e1, e2 = axes(d)
    r = math.radians(roll)
    e1, e2 = e1 * math.cos(r) + e2 * math.sin(r), -e1 * math.sin(r) + e2 * math.cos(r)
    a = np.array(a, float)
    pts = []
    for t in (0.0, 1.0):
        for sx in (-1, 1):
            for sy in (-1, 1):
                k = 1 - cham if t == 1.0 else 1.0
                pts.append(a + d * length * t + e1 * sx * half_w * k + e2 * sy * half_t * k)
    for sx in (-1, 1):
        for sy in (-1, 1):
            pts.append(a + d * (length + min(half_w, half_t) * cham) + e1 * sx * half_w * (1 - 2.5 * cham)
                       + e2 * sy * half_t * (1 - 2.5 * cham))
    return hull(pts)


def rock_shape(seed, amount=0.5, flat=-0.78, n=20):
    """The unit sphere made a lump of rock: large soft facets, its underside flattened at flat."""
    cut = facets(n, amount, seed, depth=(0.86, 1.0))
    def f(V):
        V = cut(V)
        V[:, 2] = np.maximum(V[:, 2], flat)
        return V
    return f


def bands(levels, colours, wave=0.012, seed=0.0):
    """Paint masks of horizontal bands with wavy edges: levels are the edges from the top down; each mask is the
    region from its edge down (paint them in order, the lowest last)."""
    def edge(P, z0, i):
        th = np.arctan2(P[:, 1], P[:, 0])
        return z0 + wave * np.sin(3 * th + 1.7 * i + seed) + 0.5 * wave * np.sin(7 * th + 0.9 * i + 2 * seed)
    return [(lambda P, z0=z0, i=i: inside(P[:, 2] - edge(P, z0, i), 0.003), c)
            for i, (z0, c) in enumerate(zip(levels, colours))]


def diamond():
    return polygon([(1, 0), (0, 1), (-1, 0), (0, -1)])


def sparkle(k, c, n, half, up=(0, 0, 1)):
    """A small white diamond on dark rock (a patch)."""
    k.decal(c, n, half, SPARK, up=up, eps=0.0015, outline=diamond(), rings=6, segs=32)


# --- the bar eye --------------------------------------------------------------------------------------------------
CF, CO, CL = srgb(FRAME), srgb(ORANGE), srgb(ORANGE_LT)


def bar_open(rim_u=0.62, rim_v=0.8):
    """An upright orange bar in a thick dark frame, lit toward its top."""
    def look(u, v):
        t = np.clip((v + 0.8) / 1.6, 0, 1)[:, None]
        c = CO[None] * (1 - t) + CL[None] * t
        c[(np.abs(u) > rim_u) | (np.abs(v) > rim_v)] = CF
        return c
    return look


def square():
    return polygon([(1, 1), (-1, 1), (-1, -1), (1, -1)])


# --- Nacli --------------------------------------------------------------------------------------------------------
CUBE_C, CUBE_HALF = (0.0, -0.012, 0.064), (0.064, 0.062, 0.064)
CUBE_FRONT = CUBE_C[1] - CUBE_HALF[1]
ROCK_C, ROCK_R = (0.0, 0.016, 0.172), (0.172, 0.152, 0.118)
EYE_X, EYE_Z, EYE_HALF = 0.03, 0.034, (0.016, 0.027)


def nacli(src, dest):
    k = Kit(src, coat="shell")
    # the cube of salt, and a thin plate on its front for the eyes to lie on
    k.add("cube", box(CUBE_C, CUBE_HALF, rot=(0, 0, 4)), SALT, "body", fuse=False, surface="shell")
    plate = [(-0.056, 0.004), (0.056, 0.004), (0.056, 0.072), (-0.056, 0.072)]
    k.add("plate", slab(plate, (0, CUBE_FRONT - 0.0005, 0), (1, 0, 0), (0, 0, 1), 0.004), SALT, "body")
    # the rock over it, with a knob on its crown
    k.add("rock", ell(ROCK_C, ROCK_R, rot=(0, 0, 18), deform=rock_shape(7, amount=0.6, flat=-0.92), sub=5), MID, "head")
    k.add("knob", ell((-0.012, 0.022, 0.286), (0.056, 0.05, 0.034), rot=(0, 8, 10), deform=rock_shape(3, 0.4, -0.6, 12)),
          CROWN, "head")
    rocky = lambda P: inside(0.052 - P[:, 2], 0.004) * inside(np.hypot(P[:, 0], P[:, 1] - 0.01) - 0.19, 0.004)
    for mask, col in bands((0.4, 0.25, 0.198, 0.112), (CROWN, PALE, MID, LOW), wave=0.011, seed=0.6):
        k.paint(lambda P, m=mask: m(P) * rocky(P), col, on=["rock", "knob"])
    # the salt block jutting up from the rock's back, leaning out and back
    k.add("block", bar((0.05, 0.06, 0.22), (0.42, 0.33, 0.85), 0.042, 0.038, 0.16, roll=20), SALT, "head",
          fuse=False, surface="shell")
    k.build(voxel=0.0034, smooth=3, band=0.004, wband=0.03)

    def lay(state):
        look = eye_look(state, bar_open(), srgb(SALT), CF, line=0.2)
        k.decal((EYE_X, CUBE_FRONT, EYE_Z), (0, -1, 0), EYE_HALF, look, eps=0.0018, outline=square(),
                rings=20, segs=56, mirror=True)
    k.eye_states(lay)
    k.export(dest)


if __name__ == "__main__":
    nacli(*args())
