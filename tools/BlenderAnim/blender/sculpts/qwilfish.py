# Qwilfish sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/qwilfish.py -- <the export's Qwilfish.glb> <out.glb>
#
# The design, from the references: a round puffer, dark teal over its top and back and pale yellow-green under it and
# over the lower front, the two meeting in a crisp line that runs high at the front (just under the mouth) and low at
# the back. Two cross white eyes high on the front, their top edge sloping down toward the middle, a round black
# pupil low in each. A pink pouting mouth standing out of the front, a frowning line between its lips. Bold spikes:
# teal ones over the top and sides, pale ones on the lower body. A flat paddle of a tail out of the lower back, teal
# round its rim, pale inside, with two dark slanting spots.
#
# The ball and the way a patch is laid on it are Voltorb's (sibling).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, lathe, srgb, inside, unit, eye_look, polygon, sibling, args

vo = sibling("voltorb")
TEAL, CREAM, PINK, LIPLINE, WHITE, INK = "#21606A", "#DDEBA8", "#F090A4", "#9A3E52", "#F6F6F2", "#16191C"

C = np.array([0.0, 0.0, 0.216])         # the ball's middle, on the body bone
R = 0.165


def below_line(P):
    """How far a point stands over the line between the teal and the pale (below nought: pale), across the line."""
    return ((P[:, 2] - C[2]) + 0.1 * R + 0.25 * (P[:, 1] - C[1])) / math.sqrt(1 + 0.25 ** 2)


LIPS_N = unit(0, -1, 0.1)
LIPS = C + LIPS_N * (R - 0.004)
LIPS_R = (0.05, 0.034, 0.036)


def ball(k):
    k.add("ball", ell(C, (R * 1.03, R, R), sub=5), TEAL, "body")
    k.paint(lambda P: inside(below_line(P), 0.0025), CREAM)
    k.add("lips", ell(LIPS, LIPS_R), PINK, "body")
    k.paint(lambda P: inside(((((P - LIPS) / np.array(LIPS_R)) ** 2).sum(1)) - 1.25, 0.03), PINK)
    # The frown between the lips
    def line(u, v):
        c = np.tile(srgb(PINK), (len(u), 1))
        c[(np.abs(v - (0.25 - 0.55 * u * u)) < 0.2) & (np.abs(u) < 0.85)] = srgb(LIPLINE)
        return c
    k.decal(LIPS + LIPS_N * LIPS_R[1], LIPS_N, (0.044, 0.026), line, eps=0.004, rings=12, segs=48)


def spike(k, name, d, length, r, bone="body"):
    """A solid cone out of the ball along d, its colour that of the ball where it stands."""
    d = unit(*d)
    base = C + d * (R - 0.025)
    pale = below_line((C + d * R)[None])[0] < 0
    k.add(name, lathe(base, d, [(0, 0), (r, 0), (r * 0.55, 0.035 + length * 0.45), (0, 0.025 + length)], seg=20),
          CREAM if pale else TEAL, bone, fuse=False, surface="shell")


SPIKES = [((0.5, -0.12, 0.86), 0.07), ((-0.5, -0.12, 0.86), 0.07), ((0.0, 0.5, 0.86), 0.075),
          ((0.78, 0.4, 0.48), 0.07), ((-0.78, 0.4, 0.48), 0.07), ((0.0, -0.3, 0.95), 0.06),
          ((0.98, -0.05, 0.12), 0.065), ((-0.98, -0.05, 0.12), 0.065),
          ((0.72, -0.45, -0.52), 0.06), ((-0.72, -0.45, -0.52), 0.06), ((0.0, -0.55, -0.83), 0.055),
          ((0.6, 0.45, -0.65), 0.055), ((-0.6, 0.45, -0.65), 0.055)]


def spikes(k):
    for i, (d, ln) in enumerate(SPIKES):
        spike(k, f"spike{i}", d, ln, 0.03)


def oval(c, a, b, turn=0.0, n=32):
    th = np.linspace(0, 2 * math.pi, n, endpoint=False)
    ct, st = math.cos(math.radians(turn)), math.sin(math.radians(turn))
    x, y = a * np.cos(th), b * np.sin(th)
    return np.stack([c[0] + x * ct - y * st, c[1] + x * st + y * ct], axis=1)


def tail(k):
    d = unit(0.55, 0.8, -0.22)              # back and some 35 degrees out to +X, a little down
    root = np.array([0.03, 0.15, 0.165])
    k.add("stalk", tube([(0.0, 0.1, 0.18), root, root + d * 0.045], [0.045, 0.03, 0.024]), TEAL, "tail")
    o = root + d * 0.03
    ez = (0.0, 0.0, 1.0)
    paddle = [(0.0, 0.012), (0.04, 0.042), (0.09, 0.062), (0.14, 0.058), (0.178, 0.028), (0.185, -0.014),
              (0.165, -0.052), (0.115, -0.066), (0.06, -0.05), (0.02, -0.02)]
    k.add("paddle", slab(paddle, o, d, ez, 0.018), TEAL, "tail", fuse=False, surface="scales")
    inner = [(0.11 + (a - 0.11) * 0.74, b * 0.7 - 0.002) for a, b in paddle]
    k.add("paddlein", slab(inner, o, d, ez, 0.024), CREAM, "tail", fuse=False, surface="scales")
    for i, (cc, a, b, t) in enumerate((((0.095, 0.014), 0.026, 0.009, 25), ((0.132, -0.018), 0.026, 0.009, 25))):
        k.add(f"spot{i}", slab(oval(cc, a, b, t), o, d, ez, 0.03), TEAL, "tail", fuse=False, surface="scales")


# --- the eyes: white, cross, their top sloping down to the middle, a round black pupil low ----------------------------
EYE_X, EYE_Z = 0.42, 0.42
EYE_HALF = (0.044, 0.036)
EYE = [(1.0, 0.6), (1.0, -0.35), (0.55, -0.88), (0.0, -0.96), (-0.6, -0.78), (-1.0, -0.3), (-0.95, 0.0)]
EYE_FRAC = vo.radial(EYE)
CW, CK, CT, CL = srgb(WHITE), srgb(INK), srgb(TEAL), srgb("#A8D2CC")


def eye_open(u, v):
    c = np.tile(CW, (len(u), 1))
    c[EYE_FRAC(u, v) > 0.8] = CK
    return c


def eyes(k, state):
    ink = CK if state in ("Open", "Fierce") else CL          # a light line on the dark teal when shut
    for s in (1, -1):
        vo.patch(k, C, R, EYE_X, EYE_Z, EYE_HALF, eye_look(state, eye_open, CT, ink, line=0.16), outline=EYE, side=s)
        if state in ("Open", "Fierce"):
            x = EYE_X + (-0.12) * EYE_HALF[0] / R
            z = EYE_Z + (-0.4) * EYE_HALF[1] / R
            vo.patch(k, C, R, x, z, (0.013, 0.014), INK, side=s, eps=0.0045, rings=8, segs=32)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="scales")
    ball(k)
    spikes(k)
    tail(k)
    k.build(voxel=0.005, smooth=3, band=0.005)
    k.eye_states(lambda state: eyes(k, state))
    k.export(out)
