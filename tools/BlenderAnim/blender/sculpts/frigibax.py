# Frigibax sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/frigibax.py -- <the export's Frigibax.glb> <out.glb>
# From the references: a small grey dragon child that is one bean of a body and head, standing on two stubby legs.
# A great pale bulb of a snout stands forward high on the bean with a dark nostril each side, a wide open mouth under
# it dark slate inside; big angry yellow eyes with a black pupil toward the snout, their tops cut by a heavy brow line
# sloping down to the snout. A little white cube of ice on the chest, a pale blue belly whose top edge is scalloped,
# short arms with round fists held forward, a long yellow quill from the back of each wrist sweeping back and out, a
# dark navy rounded fin on the upper back, a little spike of a tail low behind, stubby feet of three round toes.
#
# Written to be grown from (arctibax.py and baxcalibur.py import it with sibling("frigibax")): the line's colours,
# its yellow eye cut by a brow, its quills, claws, cubes and hexagons of ice are the functions below; the calls that
# build Frigibax are under __main__.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, hull, slab, srgb, inside, smoothstep, unit, polygon, eye_look, args, sibling

bg = sibling("bagon")           # on_ell, the flat-faced fang, the eye cut by a brow

# --- the line's colours ---------------------------------------------------------------------------------------------
GREY = "#9AA1B0"
SNOUT = "#E8EBF1"
FIN = "#3D5373"
BELLY = "#BAD6E2"
ICE = "#DCEEF6"
QUILL = "#F3C734"
MOUTH = "#3F4E66"
NOSTRIL = "#3E4152"
CLAW = "#E2ECF3"
INK = srgb("#15161C")
EYE_YELLOW = srgb("#F6CD22")


def ribbon(origin, ex, ez, length, width, bend, thick, n=9):
    """A flat curved quill: a strip from origin along ex, bowing toward ez by bend (a share of its length), wide
    across ez, narrowing to a rounded point; a slab, so its edges stay crisp."""
    t = np.linspace(0, 1, n)
    cx = t * length
    cz = bend * length * 4 * t * (1 - t) * 0.6 + bend * length * t * t
    w = width * (1 - 0.75 * t ** 1.4) / 2
    upper = [(x, z + ww) for x, z, ww in zip(cx, cz, w)]
    lower = [(x, z - ww) for x, z, ww in zip(cx, cz, w)]
    tip = [(length + width * 0.25, cz[-1])]
    return slab(lower + tip + upper[::-1], origin, ex, ez, thick)


def cube(c, size, turn=(0, 45, 20)):
    """A little cube of ice, hard edged, turned."""
    from mathutils import Euler
    m = np.array(Euler([math.radians(a) for a in turn]).to_matrix())
    P = np.array([(x, y, z) for x in (-1, 1) for y in (-1, 1) for z in (-1, 1)], float) * size / 2
    return hull(P @ m.T + np.array(c, float))


def hexagon(stretch=1.0):
    """A decal's outline: a hexagon with points left and right, stretched across by stretch."""
    return polygon([(math.cos(i * math.pi / 3) * stretch, math.sin(i * math.pi / 3)) for i in range(6)])


def yellow_eye(BA=0.32, BB=0.5, rim=0.8, pupil_c=(-0.18, -0.12), pupil_r=0.52):
    """The line's eye: yellow round a big black pupil toward the snout, its top cut straight by the brow (u toward
    the back), a dark rim that is heavy along the cut."""
    cut = bg.brow_cut(BA, BB)
    return cut, bg.angry_eye(cut, rim=rim, pupil_c=pupil_c, pupil_r=pupil_r, white=EYE_YELLOW)


def eyes_under_brow(k, E, de, half, look_open, cut, lid, eps=0.002, line=0.16):
    """bagon's eyes, the lift scaled to the model: the right as it is, the left turned over."""
    m = np.array([-1, 1, 1.0])
    def eyes(state):
        look = eye_look(state, look_open, lid, INK, line=line)
        k.decal(E, de, half, look, eps=eps, rings=20, segs=56, outline=cut)
        k.decal(E * m, de * m, half, lambda u, v: look(-u, v), eps=eps, rings=20, segs=56,
                outline=lambda th: cut(math.pi - th))
    return eyes


def shard(base, d, length, width, thick, across=(1, 0, 0)):
    """A broad spike of ice: a flat-faced hull wide at its base, keeping most of its width to past its middle, thick
    across `across` (the way its faces look)."""
    base, d = np.array(base, float), unit(*d)
    a = np.array(across, float); a = a - d * (a @ d); a /= np.linalg.norm(a)
    side = np.cross(d, a)
    P = []
    for t, w, th in ((-0.03, 1.0, 1.0), (0.55, 0.62, 0.7)):
        c = base + d * length * t
        for s in (1, -1):
            P.append(c + side * s * width * w / 2)
            P.append(c + a * s * thick * th / 2)
    P.append(base + d * length)
    return hull(P)


def claw(base, d, length, width, up=(0, 0, 1)):
    """A pale claw: a flat-faced point (bagon's fang)."""
    return bg.fang(base, d, length, width, up=up)


# --- Frigibax's frame --------------------------------------------------------------------------------------------------
BC, BR = np.array([0.0, 0.005, 0.152]), np.array([0.075, 0.07, 0.124])     # the bean
SC, SR = np.array([0.0, -0.084, 0.166]), np.array([0.043, 0.05, 0.041])    # the snout
SROT = (14, 0, 0)                                                         # its front tipped down


def frigibax(src, out):
    k = Kit(src, coat="scales")

    # The bean of a body and head, its forehead bulging forward over the snout
    k.add("bean", ell(BC, BR), GREY, "body")
    k.add("brow", ell((0, -0.03, 0.212), (0.06, 0.048, 0.048)), GREY, "head")
    k.add("crown", ell((0, 0.0, 0.215), (0.068, 0.064, 0.07)), GREY, "head")
    # The great pale snout, forward and high
    k.add("snout", ell(SC, SR, rot=SROT), SNOUT, "head", surface="skin")
    # the forehead running down into the snout's top, so it is no ball stuck on
    k.add("bridge", ell((0, -0.06, 0.196), (0.042, 0.042, 0.028), rot=(25, 0, 0)), GREY, "head")
    # The dark navy fin on the upper back, a soft shark's fin swept up and back
    k.add("fin", blade([(0, 0.045, 0.17), (0, 0.09, 0.212), (0, 0.118, 0.252)], [0.036, 0.024, 0.008], (1, 0, 0), 0.42),
          FIN, "body", surface="shell")
    # A little spike of a tail low behind
    k.add("tail", tube([(0, 0.055, 0.08), (0, 0.09, 0.06), (0, 0.115, 0.046)], [0.02, 0.013, 0.006]), GREY, "tail")

    # Stubby legs: fat thighs fused into the bean, broad feet of three round toes
    k.add("thigh", ell((0.042, 0.004, 0.06), (0.036, 0.04, 0.04)), GREY, "leg{s}", mirror=True)
    k.add("foot", ell((0.046, -0.016, 0.017), (0.031, 0.042, 0.018)), GREY, "leg{s}", mirror=True)
    for j, dx in enumerate((-0.017, 0.0, 0.017)):
        k.add(f"toe{j}", ell((0.046 + dx, -0.054, 0.013), (0.011, 0.012, 0.011)), GREY, "leg{s}", mirror=True)

    # Short arms forward and down to round fists in front of the belly
    k.add("arm", tube([(0.058, -0.01, 0.136), (0.074, -0.045, 0.118), (0.071, -0.074, 0.103)], [0.017, 0.016, 0.015]),
          GREY, "arm{s}", mirror=True)
    k.add("fist", ell((0.069, -0.088, 0.1), (0.021, 0.02, 0.021)), GREY, "arm{s}", mirror=True)
    k.add("thumb", ell((0.056, -0.1, 0.108), (0.01, 0.01, 0.01)), GREY, "arm{s}", mirror=True)
    # The yellow quill from the back of each wrist, sweeping back, out and a little up
    d = unit(0.55, 0.72, 0.28)
    up = np.array([0, 0, 1.0]) - d * d[2]
    k.add("quill", ribbon((0.078, -0.07, 0.108), d, up, 0.085, 0.021, 0.28, 0.009), QUILL, "arm{s}", fuse=False,
          mirror=True, surface="shell")

    # The cube of ice on the chest
    k.add("cube", cube((0.006, -0.062, 0.084), 0.013), ICE, "body", fuse=False, surface="shell")

    # The pale blue belly, its top edge scalloped
    def belly(P):
        a = np.arctan2(P[:, 0], -(P[:, 1] - 0.0))
        top = 0.058 + 0.012 * np.abs(np.cos(a * 2.6))
        return inside(P[:, 2] - top, 0.002) * inside(np.abs(a) - 1.25, 0.04) * inside(0.03 - P[:, 2], 0.003) * \
            inside(P[:, 1] + 0.012, 0.004)
    k.paint(belly, BELLY, on=["bean"])

    # The nostrils on the snout, the wide open mouth under it
    for s in (1, -1):
        n = unit(0.62 * s, -0.72, 0.05)
        k.decal(bg.on_ell(SC, SR * np.array([1, 0.9, 1]), n) + np.array([0, -0.006, -0.006]), n, (0.005, 0.0055),
                NOSTRIL, eps=0.0015, rings=8, segs=32)

    def mouth(u, v):
        c = np.tile(srgb(MOUTH), (len(u), 1))
        c[v > 0.45] = srgb("#2C3749")
        return c
    mn = unit(0, -1, -0.32)
    k.decal(bg.on_ell(BC, BR, unit(0, -1, -0.43)), mn, (0.026, 0.024), mouth, eps=0.0015, rings=14, segs=56,
            outline=lambda th: 1.0 if math.sin(th) < 0 else 1.0 - 0.18 * math.sin(th))

    k.build(voxel=0.003, smooth=3, band=0.004, wband=0.02)

    # The eyes: big, yellow, a black pupil toward the snout, cut by the brow sloping down to it
    cut, look = yellow_eye()
    de = unit(0.78, -0.56, 0.26)
    E = bg.on_ell(BC, BR, de) + np.array([0, -0.004, 0.0])
    eyes = eyes_under_brow(k, E, de, (0.024, 0.022), look, cut, srgb(GREY), eps=0.0018)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    frigibax(*args())
