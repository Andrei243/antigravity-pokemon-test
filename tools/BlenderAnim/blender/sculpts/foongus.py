# Foongus sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/foongus.py -- <the export's Foongus.glb> <out.glb>
#
# From the references: a mushroom whose great cap is a Poke Ball: a broad dome, mostly white, a red part at the +X side
# and the back, a dark brown band between them running over its top through a brown-ringed white button, and a brown
# rim all round its edge; pale beneath. Under it a small white stem like an egg, fullest low, with a face low on its
# front: a round pink nose with a dark dot, two small dark upright ovals for eyes beside its top; two stubby little
# arms out and down. (The 3D model stands the stem under the cap's middle; the drawing leans it: the stem is centred.)
#
# Written to be grown from (amoonguss.py imports it with sibling("foongus")): the cap and its Poke Ball paint, the nose
# and the line's colours are functions here; the calls that build Foongus are under __main__.
# The skeleton: body (0, 0, 0.07), head (0, 0, 0.13), armR/armL (+-0.05, -0.01, 0.06).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, args

# --- the line's colours ---------------------------------------------------------------------------------------------
WHITE, RED, BROWN, UNDER = "#EFEBE3", "#D55B57", "#5A4A3C", "#D9D0C3"
STEM, NOSE, DOT, INK = "#F3F0EB", "#E99CC6", "#2A1E24", "#1E1A1C"

# --- Foongus's frame ------------------------------------------------------------------------------------------------
CAP = np.array([0.0, 0.0, 0.13])
CAPR = np.array([0.138, 0.132, 0.135])
STEMC = np.array([0.0, 0.0, 0.058])
STEMR = np.array([0.054, 0.051, 0.06])
VOXEL = 0.0028


def on_ell(c, r, d, deform=None):
    """A point of an ellipsoid (centre c, radii r, unturned, perhaps deformed) along d from its middle, and its
    normal (near enough for a patch)."""
    u = unit(*d)[None]
    if deform is not None:
        u = deform(u)
    c, r = np.array(c, float), np.array(r, float)
    p = c + (u * r)[0]
    n = (p - c) / r ** 2
    return p, n / np.linalg.norm(n)


def dome(V):
    """The unit sphere made a cap: its upper half the dome, its underside drawn up nearly flat."""
    V = V.copy()
    V[:, 2] = np.where(V[:, 2] < 0, V[:, 2] * 0.28, V[:, 2])
    return V


def egg(V):
    V = V.copy()
    z = V[:, 2]
    s = 1 + 0.12 * np.clip(-z, 0, 1) - 0.25 * np.clip(z, 0, 1) ** 2
    V[:, 0] *= s; V[:, 1] *= s
    return V


def cap(k, C, R, bone="head", colour=WHITE):
    k.add("cap", ell(C, R, deform=dome), colour, bone)


def ball_paint(k, C, R, N, d0, ca, cb, band, under, bw, rim, on=("cap",)):
    """The cap painted as a Poke Ball: ca where (p - C).N > d0, cb elsewhere, a band of half width bw between them,
    the underside in its own colour and a brown rim round the edge rim tall."""
    N = unit(*N)
    C, R = np.array(C, float), np.array(R, float)
    def local(P):
        return P - C
    def side(P):
        return inside(d0 - local(P) @ N, 0.0012)
    def bandm(P):
        return inside(np.abs(local(P) @ N - d0) - bw, 0.0012) * (local(P)[:, 2] > -0.002)
    def rad(P):
        q = local(P)
        return np.sqrt((q[:, 0] / R[0]) ** 2 + (q[:, 1] / R[1]) ** 2)
    def underm(P):
        return inside(local(P)[:, 2] + 0.003, 0.0012) * inside(rad(P) - 0.93, 0.004)
    def rimm(P):
        q = local(P)
        return inside(q[:, 2] - rim, 0.0012) * inside(0.86 - rad(P), 0.004)
    k.paint(lambda P: np.ones(len(P)), cb, on=list(on))
    k.paint(side, ca, on=list(on))
    k.paint(bandm, band, on=list(on))
    k.paint(underm, under, on=list(on))
    k.paint(rimm, band, on=list(on))


def button(k, centre, normal, outer, inner, ring=BROWN, face=WHITE, up=(0, 1, 0)):
    """The ball's button: a brown ring round a white middle."""
    CR_, CF = srgb(ring), srgb(face)
    t = inner / outer
    def look(u, v):
        c = np.tile(CR_, (len(u), 1))
        c[u * u + v * v < t * t] = CF
        return c
    k.decal(centre, normal, (outer, outer), look, up=up, eps=0.003, rings=20, segs=64)


def nose(k, centre, normal, r, bone):
    """A round pink nose standing out of the face, with a dark dot."""
    n = np.array(normal, float); n /= np.linalg.norm(n)
    c = np.array(centre, float)
    k.add("nose", ell(c + n * r * 0.35, (r, r * 0.85, r), sub=3), NOSE, bone, fuse=False, surface="skin")
    dd = n * 0.92 + np.array([0.28, 0, -0.3]); dd /= np.linalg.norm(dd)
    k.add("nosedot", ell(c + n * r * 0.35 + dd * r * 0.9, (r * 0.2, r * 0.2, r * 0.2), sub=2),
          DOT, bone, fuse=False, surface="skin")


def dash(u, v):
    return np.tile(srgb(INK), (len(u), 1))


def foongus(src, out):
    k = Kit(src, coat="fur")

    # ---- the stem: a small white egg, fullest low, its top going up into the cap
    k.add("stem", ell(STEMC, STEMR, deform=egg), STEM, "body")
    k.add("neck", tube([(0, 0, 0.085), (0, 0, 0.125)], [0.036, 0.033]), STEM, "head")

    # ---- the arms: stubby flaps out and down
    k.add("arm", ell((0.06, -0.012, 0.038), (0.024, 0.013, 0.012), rot=(0, 28, -12)), STEM, "arm{s}", mirror=True)

    # ---- the cap: a broad dome painted as a Poke Ball, red at +X and the back
    cap(k, CAP, CAPR)
    ball_paint(k, CAP, CAPR, (0.6, 0.8, 0.0), 0.0, RED, WHITE, BROWN, UNDER, 0.0068, 0.011)
    tp, tn = on_ell(CAP, CAPR, (0.0, 0.1, 1.0))
    button(k, tp, tn, 0.043, 0.033)

    # ---- the nose: low on the stem's front
    np_, nn = on_ell(STEMC, STEMR, (0, -1, -0.22), egg)
    nose(k, np_, nn, 0.0165, "body")

    k.build(voxel=VOXEL, smooth=4, band=0.003, wband=0.03)

    # ---- the eyes: small dark upright ovals beside the nose's top
    ep, en = on_ell(STEMC, STEMR, (0.5, -0.86, 0.08), egg)
    CK, CS = srgb(INK), srgb(STEM)
    def eyes(state):
        k.decal(ep, en, (0.0075, 0.0115), eye_look(state, dash, CS, CK, line=0.3), eps=0.002, rings=20, segs=56,
                mirror=True)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    foongus(*args())
