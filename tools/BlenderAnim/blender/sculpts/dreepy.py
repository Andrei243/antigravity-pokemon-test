# Dreepy sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/dreepy.py -- <the export's Dreepy.glb> <out.glb>
# From the references: a small ghost of a dragon hovering level, its head the largest mass: a flat dark-teal plate over
# the top like an arrowhead, pointed in front over a short mint snout and broad behind, its two rear corners pink; huge
# round pale-yellow eyes bulging from the sides of the head under the plate's edge, a small black pupil, a red rim; a
# little pink crest over each eye; two pink fins trailing back under the plate; a slim mint neck and body running back
# into a thin tail that curls up at its end, a dark band round the tail's middle with one spike up and one down; four
# thin mint legs hanging under the chest, pointed at their ends.
# Written to be grown from: Drakloak and Dragapult carry Dreepy (sibling("dreepy")): dreepy() lays a whole one at any
# place, turn and size, and gives back where its eyes go.
# Bones: root 0; body (0, 0.04, 0.11); head (0, -0.04, 0.11); tail (0, 0.06, 0.11); legR (0.03, -0.02, 0.09);
# hindR (0.03, 0.1, 0.09).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, ring_band, srgb, unit, eye_look, args

# --- the line's colours ----------------------------------------------------------------------------------------------
MINT = "#86D4B2"
PLATE = "#3F7266"
PINK = "#EE5A7C"
EYE, RIM, PUPIL, INK = "#F4EC8E", "#C2384E", "#14161A", "#14161A"

J = np.array([0.0, -0.04, 0.11])          # the head's joint: what a placed Dreepy is turned and scaled about
BONES = dict(head="head", body="body", tail="tail", legR="legR", legL="legL", hindR="hindR", hindL="hindL")

# --- the frame (Dreepy's own measures, Blender's axes) ---------------------------------------------------------------
HEAD_C, HEAD_R = (0.0, -0.088, 0.124), (0.05, 0.088, 0.032)
EYE_C, EYE_R = (0.052, -0.113, 0.131), 0.031
EYE_D = unit(0.78, -0.58, 0.18)            # an eye looks out and forward
BODY = [(0.0, -0.03, 0.124), (0.0, 0.03, 0.104), (0.0, 0.095, 0.1)]
TAIL = [(0.0, 0.095, 0.1), (0.0, 0.16, 0.11), (0.008, 0.21, 0.122), (0.024, 0.248, 0.138), (0.042, 0.27, 0.158)]
TAIL_R = [0.022, 0.017, 0.013, 0.01, 0.0075]


def snout(V):
    """The unit sphere drawn to a point in front (-Y) and a little down: a wedge of a head."""
    V = V.copy()
    t = np.clip(-V[:, 1], 0, 1)
    s = 1 - 0.72 * t ** 1.5
    V[:, 0] *= s
    V[:, 2] = V[:, 2] * s - 0.25 * t ** 2
    return V


def frame(O, F, U, s):
    """The map from Dreepy's own measures to a place: its head's joint to O, its front to F, its up to U, scaled by s."""
    F = unit(*F)
    U = np.array(U, float); U = U - F * (U @ F); U /= np.linalg.norm(U)
    R = np.cross(U, F)
    M = np.stack([R, -F, U], axis=1)       # its x to R, its y (backward) to -F, its z to U: a proper turn
    O = np.array(O, float)

    def T(P):
        return O + s * ((np.asarray(P, float) - J) @ M.T)
    return T, M


def dreepy(k, O=J, F=(0, -1, 0), U=(0, 0, 1), s=1.0, bones=BONES, mirror=False, legs=True, floor=0.0, name=""):
    """
    A whole Dreepy laid in kit k (before its build): turned and scaled about its head's joint (O, its front F, its up
    U, its size s), on the bones given. floor: the least radius a part may have where it is placed (a small rider in a
    bigger model's voxels). Gives back where its eyes go: (centre, normal, half sizes, side) each.
    """
    T, M = frame(O, F, U, s)
    lo = floor / s

    def E(c, r, rot=(0, 0, 0), deform=None, sub=4):
        V, Fc = ell(c, np.maximum(np.array(r, float), lo), rot, deform, sub)
        return T(V), Fc

    def Tu(pts, radii):
        V, Fc = tube(pts, np.maximum(np.array(radii, float), lo))
        return T(V), Fc

    def H(pts):
        return hull(T(pts))

    def add(part, shape, color, bone, **kw):
        k.add(name + part, shape, color, bones[bone], mirror=mirror, **kw)

    # --- the head: a mint wedge under a dark plate ---------------------------------------------------------------------
    add("head", E(HEAD_C, HEAD_R, deform=snout, sub=5), MINT, "head")
    add("nape", E((0.0, -0.02, 0.121), (0.036, 0.05, 0.025)), MINT, "head")
    for sd in (1, -1):
        c = np.array(EYE_C) * (sd, 1, 1)
        add(f"eyeball{'R' if sd > 0 else 'L'}", E(c, (EYE_R, EYE_R * 0.95, EYE_R)), MINT, "head")

    # the plate: an arrowhead pointed over the snout, broad behind, thick along its ridge
    plate = [(0.0, -0.196, 0.129), (0.0, -0.19, 0.12), (0.0, -0.075, 0.173), (0.0, 0.03, 0.166),
             (0.0, 0.03, 0.133), (0.0, -0.06, 0.128)]
    for sd in (1, -1):
        plate += [(0.062 * sd, -0.098, 0.149), (0.062 * sd, -0.098, 0.139), (0.122 * sd, 0.066, 0.152),
                  (0.122 * sd, 0.066, 0.144), (0.04 * sd, 0.04, 0.152), (0.04 * sd, 0.04, 0.136)]
    add("plate", H(plate), PLATE, "head", fuse=False)
    # its rear corners pink: a thicker tip over each corner
    for sd in (1, -1):
        tip = [(0.134 * sd, 0.074, 0.149), (0.094 * sd, 0.04, 0.158), (0.094 * sd, 0.04, 0.138),
               (0.106 * sd, 0.083, 0.156), (0.106 * sd, 0.083, 0.14), (0.098 * sd, 0.06, 0.161)]
        add(f"tip{'R' if sd > 0 else 'L'}", H(tip), PINK, "head", fuse=False)
        # a small pink crest over the eye, laid back
        crest = [(0.036 * sd, -0.1, 0.158), (0.05 * sd, -0.1, 0.152), (0.043 * sd, -0.084, 0.16),
                 (0.044 * sd, -0.058, 0.182), (0.04 * sd, -0.07, 0.165)]
        add(f"crest{'R' if sd > 0 else 'L'}", H(crest), PINK, "head", fuse=False)
        # a pink fin trailing back and out under the plate
        fin = [(0.03 * sd, -0.015, 0.139), (0.03 * sd, 0.012, 0.137), (0.03 * sd, -0.002, 0.128),
               (0.036 * sd, 0.0, 0.134), (0.088 * sd, 0.082, 0.126), (0.06 * sd, 0.05, 0.132)]
        add(f"fin{'R' if sd > 0 else 'L'}", H(fin), PINK, "head", fuse=False)

    # --- the neck, body and tail: one slim mint line curling up at its end -------------------------------------------
    add("body", Tu(BODY, [0.031, 0.026, 0.022]), MINT, "body")
    add("tail", Tu(TAIL, TAIL_R), MINT, "tail")
    # the dark band round the tail's middle, with a spike up and a spike down
    bc, bd = np.array((0.0, 0.158, 0.11)), unit(0, 1, 0.12)
    V, Fc = ring_band(bc, bd, 0.0175, 0.016, thick=0.005)
    add("band", (T(V), Fc), PLATE, "tail", fuse=False)
    for sd, z0, z1 in ((1, 0.124, 0.162), (-1, 0.096, 0.06)):
        spike = [(0.0, 0.147, z0), (0.006, 0.164, z0), (-0.006, 0.164, z0), (0.0, 0.172, z0 - 0.004 * sd),
                 (0.004, 0.188, z1)]
        add(f"spike{'U' if sd > 0 else 'D'}", H(spike), PLATE, "tail", fuse=False)

    # --- four thin legs hanging under the chest, pointed at their ends ----------------------------------------------
    if legs:
        for sd in (1, -1):
            w = "R" if sd > 0 else "L"
            add(f"leg{w}", Tu([(0.022 * sd, -0.022, 0.1), (0.04 * sd, -0.05, 0.068), (0.042 * sd, -0.05, 0.042),
                               (0.038 * sd, -0.043, 0.026)], [0.0095, 0.0082, 0.0065, 0.0038]), MINT, "leg" + w)
            add(f"hind{w}", Tu([(0.02 * sd, 0.07, 0.092), (0.038 * sd, 0.05, 0.066), (0.04 * sd, 0.03, 0.044),
                                (0.036 * sd, 0.016, 0.03)], [0.0092, 0.008, 0.0062, 0.0036]), MINT, "hind" + w)

    # --- where the eyes go -------------------------------------------------------------------------------------------
    spots = []
    for sd in (1, -1):
        d = EYE_D * np.array([sd, 1, 1])
        c = np.array(EYE_C) * (sd, 1, 1) + d * EYE_R
        spots.append((T(c), M @ d, (EYE_R * 0.92 * s, EYE_R * 0.92 * s), sd, M @ np.array([0, 0, 1.0])))
    return spots


def dreepy_eye():
    """Pale yellow, a small black pupil a little forward, a red rim."""
    Y, Rm, P = srgb(EYE), srgb(RIM), srgb(PUPIL)

    def look(u, v):
        r = np.sqrt(u * u + v * v)
        c = np.tile(Y, (len(u), 1))
        c[((u + 0.12) / 0.27) ** 2 + ((v - 0.02) / 0.3) ** 2 < 1] = P
        c[r > 0.83] = Rm
        return c
    return look


def lay_eyes(k, spots, state="Open", mirror=False, eps=0.003, lid=MINT):
    """The eyes of a Dreepy in a state (a rider's are open in every state of the model that carries it)."""
    f = eye_look(state, dreepy_eye(), srgb(lid), srgb(INK), line=0.18)
    for c, n, half, sd, up in spots:
        col = f if sd > 0 else (lambda u, v, f=f: f(-u, v))
        k.decal(c, n, half, col, up=up, eps=eps, rings=20, segs=56, mirror=mirror)


def build(src, out):
    k = Kit(src, coat="scales")
    spots = dreepy(k)
    k.build(voxel=0.0021, smooth=4, band=0.004, wband=0.02)
    k.eye_states(lambda state: lay_eyes(k, spots, state, eps=0.0012))
    k.export(out)


if __name__ == "__main__":
    build(*args())
