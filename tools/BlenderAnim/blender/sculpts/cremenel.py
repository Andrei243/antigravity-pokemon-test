# Cremenel sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/cremenel.py -- <the export's Cremenel.glb> <out.glb>
#
# From its picture (docs/art/reference/gemini/carpathia/10106-Cremenel.jpg) and its brief (CP-dex-2b-mountains-more.md,
# "106. Cremenel"): a round pebble of grey-black flint, nearly as wide as it is tall, cut all over in broad flat
# chipped facets, the upper ones lighter grey and glassy, the lower ones darker; across its middle a dark slot like a
# domino mask, its rim chipped lighter, and in it two upright glowing orange ovals for eyes; two stubby angular feet
# under it. The sparks drawn round it are an effect and are left out.
#
# Written to be grown from: cremenor.py imports it (sibling("cremenel")) for the line's colours, the flint's facets,
# the eye slot and the glowing eyes. The build is under the main guard.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, srgb, inside, unit, eye_look, polygon, args, facets, hull

# --- the line's colours -------------------------------------------------------------------------------------------
FLINT, FLINT_DK, FLINT_LT = "#3C3D43", "#2A2B30", "#5C5E66"     # the flint, its lower facets, its lit facets
GLINT = "#8C909A"                                              # the glassiest facets near the top
SLOT, SLOT_RIM = "#18181C", "#6E7078"                          # the eye slot and its chipped rim
EYE_HOT, EYE_MID, EYE_EDGE = "#FFE47E", "#FFAE2A", "#E8701A"    # the glowing eye: its heart, its body, its edge
SHUT = "#F08A24"                                               # a closed eye's line on the dark slot

# --- the frame ----------------------------------------------------------------------------------------------------
C = np.array([0.0, 0.0, 0.182])                 # the ball's centre, over the body's joint (0, 0, 0.2)
R = np.array([0.18, 0.168, 0.184])             # its radii: the picture's ball is 0.97 as wide as it is tall
SEED, NFACETS = 106, 30
EYE_X, EYE_Z = 0.04, 0.192                    # the eyes' places on the front
EYE = (0.0155, 0.026)                           # the eyes' half sizes: upright ovals
SLOT_HALF = (0.095, 0.047)
VOXEL = 0.0036

# the slot in (u, v) of its patch: two rounded lobes for the eyes, pinched a little over and under the nose
SLOT_OUT = [(-1.0, 0.05), (-0.94, 0.5), (-0.7, 0.86), (-0.38, 0.92), (-0.12, 0.66), (0.0, 0.6), (0.12, 0.66),
            (0.38, 0.92), (0.7, 0.86), (0.94, 0.5), (1.0, 0.05), (0.92, -0.5), (0.66, -0.88), (0.32, -0.9),
            (0.1, -0.62), (0.0, -0.58), (-0.1, -0.62), (-0.32, -0.9), (-0.66, -0.88), (-0.92, -0.5)]


DEPTH = (0.74, 0.93)


def facet_planes(n, seed, keep_front=None, depth=DEPTH):
    """The planes kit.facets cuts with (the same draws), so the paint can tell one facet from another."""
    rs = np.random.RandomState(seed)
    N = rs.normal(size=(n, 3)); N /= np.linalg.norm(N, axis=1, keepdims=True)
    if keep_front is not None:
        N = N[N[:, 1] > -keep_front]
    d = depth[0] + (depth[1] - depth[0]) * rs.rand(len(N))
    return N, d


def facet_of(P, c, r, N, d):
    """Which facet each point lies on: the plane its direction from the centre meets first."""
    D = (P - c) / r
    D /= np.linalg.norm(D, axis=1, keepdims=True)
    return np.argmax((D @ N.T) / d[None], axis=1)


def stone(k, name, c, r, seed, n, bone, colour=FLINT, keep_front=None, amount=0.92, sub=5, rot=(0, 0, 0)):
    """A chipped flint ball fused into the body: broad flat facets, the front kept round where asked."""
    k.add(name, ell(c, r, rot=rot, deform=facets(n, amount, seed, keep_front=keep_front, depth=DEPTH),
                    sub=sub), colour, bone, surface="shell")


def shade_facets(k, c, r, seed, n, on, keep_front=None, tones=(FLINT_DK, FLINT, FLINT_LT, GLINT), rot=None):
    """Each facet a tone of its own, as the picture's chipped planes are: glassy and light above, dark below."""
    N, d = facet_planes(n, seed, keep_front)
    rs = np.random.RandomState(seed + 7)
    up = N[:, 2] + 0.3 * (rs.rand(len(N)) - 0.5)
    mix = rs.rand(len(N))
    tone = np.where(up > 0.6, np.where(mix < 0.45, 3, 2),
                    np.where(up > 0.1, np.where(mix < 0.4, 2, 1), np.where(up > -0.45, np.where(mix < 0.2, 2, 1), 0)))
    M = np.eye(3) if rot is None else rot
    for t, col in enumerate(tones):
        sel = tone == t
        if t == 1 or not sel.any():
            continue
        k.paint(lambda P, sel=sel: sel[facet_of(c + (P - c) @ M, c, r, N, d)].astype(float), col, on=on)


def eye_open(u, v):
    """An upright glowing oval: a hot yellow heart, orange round it, a darker orange edge."""
    rr = np.hypot(u, v)
    c = np.tile(srgb(EYE_MID), (len(u), 1))
    c[np.hypot(u + 0.08, v - 0.12) < 0.42] = srgb(EYE_HOT)
    c[rr > 0.8] = srgb(EYE_EDGE)
    return c


def slot(k, centre, normal, half, outline=SLOT_OUT, rim=1.16, eps=0.0026, colour=SLOT, rim_colour=SLOT_RIM):
    """The dark slot the eyes glow from, with a chipped lighter rim a little larger under it."""
    k.decal(centre, normal, (half[0] * rim, half[1] * rim * 1.12), rim_colour, eps=eps * 0.7,
            outline=polygon(outline), rings=10, segs=60)
    k.decal(centre, normal, half, colour, eps=eps, outline=polygon(outline), rings=10, segs=60)


def eyes(k, at, normal, half, lid=SLOT, ink=SHUT, eps=0.0036, fierce=None):
    """The two glowing eyes; closed, the slot's dark with an orange line, so a blink still shows."""
    m = np.array([-1, 1, 1.0])
    at, normal = np.array(at, float), np.array(normal, float)
    LID, INK = srgb(lid), srgb(ink)
    def lay(state):
        look = eye_look(state, eye_open, LID, INK, line=0.22)
        if state == "Fierce" and fierce is not None:
            look = fierce
        k.decal(at, normal, half, look, eps=eps, rings=20, segs=56)
        k.decal(at * m, normal * m, half, lambda u, v, look=look: look(-u, v), eps=eps, rings=20, segs=56)
    return lay


def front(x, z, c=C, r=R):
    """A point on the ball's round front at x and z."""
    q = 1 - (x / r[0]) ** 2 - ((z - c[2]) / r[2]) ** 2
    return np.array([x, c[1] - r[1] * math.sqrt(max(q, 0.0)), z])


def feet(k, x=0.066, colour=FLINT_DK):
    """Two stubby angular feet, a toe forward: chipped stones of their own under the ball."""
    for s, side in ((1, "R"), (-1, "L")):
        k.add(f"foot{side}", ell((x * s, -0.022, 0.028), (0.036, 0.046, 0.03), rot=(0, 0, 8 * s),
                                 deform=facets(9, 0.7, 30 + (s > 0), depth=(0.82, 0.95)), sub=4), colour,
              f"leg{side}", surface="shell")
        k.add(f"leg{side}", ell((x * s * 0.96, -0.006, 0.05), (0.03, 0.03, 0.03)), colour, f"leg{side}",
              surface="shell")


def board(ob):
    V = np.array([v.co[:] for v in ob.data.vertices])
    h, w, l = V[:, 2].max(), np.ptp(V[:, 0]), np.ptp(V[:, 1])
    a = math.radians(31.5)
    proj = V[:, 0] * math.cos(a) + V[:, 1] * math.sin(a)
    print("KIT: board: corners %.3f (0.853 w + 0.522 l), shows %.3f, in a square of %.3f"
          % (0.853 * w + 0.522 * l, np.ptp(proj), 1.3 * max(h, 0.85 * max(w, l))))


def cremenel(src, out_path):
    k = Kit(src, coat="shell")
    stone(k, "ball", C, R, SEED, NFACETS, "body", keep_front=0.55, amount=0.8)
    feet(k)
    shade_facets(k, C, R, SEED, NFACETS, on=["ball"], keep_front=0.55)
    # the round front round the slot the plain flint, so the slot's rim reads against it
    k.paint(lambda P: inside(np.hypot(P[:, 0] / 0.12, (P[:, 2] - EYE_Z) / 0.075) - 1, 0.02) * (P[:, 1] < -0.1),
            FLINT, on=["ball"])
    sc = front(0.0, EYE_Z)
    slot(k, sc, (0, -1, 0.0), SLOT_HALF)
    ob = k.build(voxel=VOXEL, smooth=1, band=0.005)
    board(ob)
    ea = front(EYE_X, EYE_Z)
    k.eye_states(eyes(k, ea, unit(0.22, -1, 0.0), EYE))
    k.export(out_path)


if __name__ == "__main__":
    cremenel(*args())
