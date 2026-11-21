# Overqwil sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown from
# Qwilfish's script (qwilfish.py): the same ball with a crisp line between the dark and the pale, the same solid cones
# for spikes coloured by where they stand, the same pouting lips, a patch laid on the ball by Voltorb's way (sibling).
#   blender --background --factory-startup --python sculpts/overqwil.py -- <the export's Overqwil.glb> <out.glb>
#
# The design, from the references: a round puffer, dark navy over its top and back and cream over its lower front and
# belly, the line between them running just under the eyes in front and low at the back; violet spots over the top;
# long spikes all round it: the dark ones over the top and sides banded violet toward their points, the cream ones
# under the belly plain cream (no violet tips); small angry white eyes, a cream spike over each like a brow, sloping
# up and out; a pink-violet pouting mouth; out of the back on its +X side a stalk and a round dark tail fin marked with
# a violet Q. The skeleton: body (0, 0, 0.341), tail (0, 0.17, 0.341).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, lathe, srgb, inside, unit, eye_look, polygon, sibling, args

vo = sibling("voltorb")
NAVY, CREAM, VIOLET, SPOT, LIPS_C, LIPLINE, WHITE, INK = ("#262B4A", "#E9E2C4", "#B03FE0", "#9B3AD8", "#D85BB6",
                                                          "#7A2A6E", "#F6F6F2", "#16191C")

C = np.array([0.0, 0.0, 0.345])         # the ball's middle, on the body bone
R = 0.165


def below_line(P):
    """How far a point stands over the line between the navy and the cream (below nought: cream), across it."""
    return ((P[:, 2] - C[2]) + 0.1 * R + 0.32 * (P[:, 1] - C[1])) / math.sqrt(1 + 0.32 ** 2)


LIPS_N = unit(0, -1, -0.12)
LIPS = C + LIPS_N * (R - 0.006)
LIPS_R = (0.044, 0.03, 0.034)


def ball(k):
    k.add("ball", ell(C, (R * 1.02, R, R), sub=5), NAVY, "body")
    k.paint(lambda P: inside(below_line(P), 0.0025), CREAM)
    # violet spots over the top
    spots = [((0.05, -0.06, 0.97), 0.02), ((-0.07, -0.02, 0.96), 0.018), ((0.0, 0.4, 0.88), 0.022),
             ((0.55, 0.15, 0.8), 0.02), ((-0.55, 0.2, 0.8), 0.02), ((0.35, -0.45, 0.8), 0.015),
             ((-0.35, -0.45, 0.8), 0.015), ((0.0, 0.8, 0.55), 0.02)]
    def spot_mask(P):
        m = np.zeros(len(P))
        for d, r in spots:
            p = C + unit(*d) * R
            m = np.maximum(m, inside(np.linalg.norm(P - p, axis=1) - r, 0.0025))
        return m
    k.paint(spot_mask, SPOT)
    k.add("lips", ell(LIPS, LIPS_R), LIPS_C, "body")
    k.paint(lambda P: inside(((((P - LIPS) / np.array(LIPS_R)) ** 2).sum(1)) - 1.25, 0.03), LIPS_C)
    def line(u, v):
        c = np.tile(srgb(LIPS_C), (len(u), 1))
        c[(np.abs(v - (0.2 - 0.5 * u * u)) < 0.2) & (np.abs(u) < 0.8)] = srgb(LIPLINE)
        return c
    k.decal(LIPS + LIPS_N * LIPS_R[1], LIPS_N, (0.04, 0.026), line, eps=0.004, rings=12, segs=48)


def spike(k, name, d, length, r, color=None, bone="body"):
    """A solid cone out of the ball along d; a dark one banded violet over its outer half."""
    d = unit(*d)
    base = C + d * (R - 0.03)
    pale = below_line((C + d * R)[None])[0] < 0
    L = length + 0.03
    if pale:
        k.add(name, lathe(base, d, [(0, 0), (r, 0), (r * 0.6, L * 0.5), (0, L)], seg=20), CREAM, bone, fuse=False,
              surface="shell")
        return
    k.add(name, lathe(base, d, [(0, 0), (r, 0), (r * 0.6, L * 0.6), (0, L * 0.6)], seg=20), NAVY, bone,
          fuse=False, surface="shell")
    k.add(name + "tip", lathe(base, d, [(0, L * 0.56), (r * 0.64, L * 0.565), (0, L)], seg=20),
          VIOLET, bone, fuse=False, surface="shell")


SPIKES = [((0.0, 0.1, 1.0), 0.165),
          ((0.65, -0.2, 0.7), 0.15), ((-0.65, -0.2, 0.7), 0.15), ((0.5, 0.6, 0.62), 0.11), ((-0.5, 0.6, 0.62), 0.11),
          ((0.0, 0.72, 0.7), 0.11), ((0.0, -0.55, 0.85), 0.08),
          ((1.0, -0.05, 0.08), 0.135), ((-1.0, -0.05, 0.08), 0.135), ((0.62, 0.78, 0.12), 0.09),
          ((-0.62, 0.78, 0.12), 0.09),
          ((0.0, -0.1, -1.0), 0.145), ((0.72, -0.25, -0.62), 0.13), ((-0.72, -0.25, -0.62), 0.13),
          ((0.38, -0.62, -0.68), 0.09), ((-0.38, -0.62, -0.68), 0.09), ((0.55, 0.55, -0.62), 0.1),
          ((-0.55, 0.55, -0.62), 0.1), ((0.0, 0.62, -0.78), 0.09), ((0.0, -0.85, -0.45), 0.06)]


def spikes(k):
    for i, (d, ln) in enumerate(SPIKES):
        spike(k, f"spike{i}", d, ln, 0.034)
    # the cream brows: a spike over each eye, up and out from between them
    for s in (1, -1):
        b = C + unit(0.16 * s, -0.95, 0.3) * (R - 0.01)
        tip = b + np.array([0.085 * s, -0.035, 0.05])
        k.add(f"brow{'R' if s > 0 else 'L'}", lathe(b, tip - b, [(0, 0), (0.016, 0), (0.009, 0.05), (0, 0.105)],
                                                   seg=16), CREAM, "body", fuse=False, surface="shell")


def ring_piece(c, n, r_in, r_out, h):
    return lathe(c, n, [(r_in, -h), (r_out, -h), (r_out, h), (r_in, h), (r_in, -h * 0.999)], seg=40)


def tail(k):
    d = unit(0.55, 0.83, 0.0)                # back and some 35 degrees out to +X
    root = np.array([0.04, 0.15, 0.33])
    k.add("stalk", tube([(0.0, 0.1, 0.34), root, root + d * 0.05], [0.05, 0.034, 0.028]), NAVY, "tail")
    fc = root + d * 0.1
    n = unit(0.83, -0.55, 0.0)               # the disc's face turned out to +X, square to the stalk
    k.add("finbody", lathe(fc, n, [(0, -0.014), (0.072, -0.014), (0.072, 0.014), (0, 0.014)], seg=40), NAVY,
          "tail", fuse=False, surface="scales")
    # the Q: a violet ring with its tail, a little thicker than the disc so it shows on both faces
    k.add("qring", ring_piece(fc, n, 0.026, 0.044, 0.018), VIOLET, "tail", fuse=False, surface="scales")
    ex = np.cross(n, (0, 0, 1.0)); ex /= np.linalg.norm(ex)
    k.add("qtail", slab([(0.018, -0.028), (0.05, -0.058), (0.06, -0.046), (0.03, -0.016)], fc, ex, (0, 0, 1), 0.036),
          VIOLET, "tail", fuse=False, surface="scales")


# --- the eyes: small, white, angry, their top sloping down to the middle; a black pupil --------------------------------
EYE_X, EYE_Z = 0.3, 0.3
EYE_HALF = (0.03, 0.026)
EYE = [(1.0, 0.65), (1.0, -0.35), (0.55, -0.9), (0.0, -0.96), (-0.6, -0.78), (-1.0, -0.3), (-0.95, -0.05)]
EYE_FRAC = vo.radial(EYE)
CW, CK, CN, CL = srgb(WHITE), srgb(INK), srgb(NAVY), srgb("#8A90B8")


def eye_open(u, v):
    c = np.tile(CW, (len(u), 1))
    c[EYE_FRAC(u, v) > 0.8] = CK
    return c


def eyes(k, state):
    ink = CK if state in ("Open", "Fierce") else CL          # a light line on the navy when shut
    for s in (1, -1):
        vo.patch(k, C, R, EYE_X, EYE_Z, EYE_HALF, eye_look(state, eye_open, CN, ink, line=0.16), outline=EYE, side=s)
        if state in ("Open", "Fierce"):
            x = EYE_X + (-0.15) * EYE_HALF[0] / R
            z = EYE_Z + (-0.35) * EYE_HALF[1] / R
            vo.patch(k, C, R, x, z, (0.011, 0.012), INK, side=s, eps=0.0045, rings=8, segs=32)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="scales")
    ball(k)
    spikes(k)
    tail(k)
    k.build(voxel=0.005, smooth=3, band=0.005)
    k.eye_states(lambda state: eyes(k, state))
    k.export(out)
