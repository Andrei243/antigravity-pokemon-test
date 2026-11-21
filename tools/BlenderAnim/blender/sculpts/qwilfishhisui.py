# Qwilfish-Hisui sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/qwilfishhisui.py -- <the export's Qwilfish-Hisui.glb> <out.glb>
#
# Qwilfish's Hisuian form, from the pictures: the same round puffer, but dark navy over its top and back and a warm
# cream under it, the line between them running a little lower than Qwilfish's. The face is angrier: white eyes with
# a small black pupil, set in a magenta rim under a dark brow line sloping down to the middle; a big magenta pout
# standing out of the front with a frowning line between its lips. The spikes over the top and sides are navy with
# bright magenta tips, those under the body cream. The tail is a round paddle on a navy stalk, cream with a navy rim,
# and on it a navy "9". Built by qwilfish.py's frame (sibling): its ball, its line, its eye's outline and Voltorb's
# way of laying a patch on the ball; the colours, the spikes' tips, the eyes and the tail's mark are this form's own.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, lathe, srgb, inside, unit, eye_look, args, sibling

qw = sibling("qwilfish")
vo = qw.vo
NAVY, CREAM, MAGENTA, LIPLINE, WHITE, INK = "#262B48", "#EEEBCB", "#D648C6", "#7E2470", "#F6F6F2", "#12131C"
C, R = qw.C, qw.R


def below_line(P):
    """Over (above nought) or under the line between the navy and the cream: a little lower than Qwilfish's."""
    return ((P[:, 2] - C[2]) + 0.2 * R + 0.2 * (P[:, 1] - C[1])) / math.sqrt(1 + 0.2 ** 2)


LIPS_N = unit(0, -1, 0.05)
LIPS = C + LIPS_N * (R - 0.004) + np.array([0, 0, -0.008])
LIPS_R = (0.058, 0.04, 0.04)


def ball(k):
    k.add("ball", ell(C, (R * 1.03, R, R), sub=5), NAVY, "body")
    k.paint(lambda P: inside(below_line(P), 0.0025), CREAM)
    k.add("lips", ell(LIPS, LIPS_R), MAGENTA, "body")
    k.paint(lambda P: inside(((((P - LIPS) / np.array(LIPS_R)) ** 2).sum(1)) - 1.25, 0.03), MAGENTA)
    def line(u, v):
        c = np.tile(srgb(MAGENTA), (len(u), 1))
        c[(np.abs(v - (0.2 - 0.5 * u * u)) < 0.2) & (np.abs(u) < 0.85)] = srgb(LIPLINE)
        return c
    k.decal(LIPS + LIPS_N * LIPS_R[1], LIPS_N, (0.05, 0.03), line, eps=0.004, rings=12, segs=48)


def spike(k, name, d, length, r, bone="body"):
    """A solid cone out of the ball along d; over the line navy with a magenta tip, under it cream."""
    d = unit(*d)
    base = C + d * (R - 0.025)
    pale = below_line((C + d * R)[None])[0] < 0
    h0 = 0.035 + length * 0.4
    tip = 0.025 + length
    if pale:
        k.add(name, lathe(base, d, [(0, 0), (r, 0), (r * 0.55, h0), (0, tip)], seg=20), CREAM, bone, fuse=False,
              surface="shell")
        return
    k.add(name, lathe(base, d, [(0, 0), (r, 0), (r * 0.6, h0), (0, h0)], seg=20), NAVY, bone, fuse=False,
          surface="shell")
    k.add(name + "tip", lathe(base, d, [(0, h0 - 0.006), (r * 0.68, h0 - 0.006), (0, tip)], seg=20), MAGENTA, bone,
          fuse=False, surface="shell")


SPIKES = [((0.5, -0.12, 0.86), 0.085), ((-0.5, -0.12, 0.86), 0.085), ((0.0, 0.5, 0.86), 0.09),
          ((0.78, 0.4, 0.48), 0.085), ((-0.78, 0.4, 0.48), 0.085), ((0.0, -0.3, 0.95), 0.07),
          ((0.98, -0.05, 0.12), 0.08), ((-0.98, -0.05, 0.12), 0.08),
          ((0.72, -0.45, -0.52), 0.06), ((-0.72, -0.45, -0.52), 0.06), ((0.0, -0.55, -0.83), 0.055),
          ((0.6, 0.45, -0.65), 0.055), ((-0.6, 0.45, -0.65), 0.055)]


def spikes(k):
    for i, (d, ln) in enumerate(SPIKES):
        spike(k, f"spike{i}", d, ln, 0.033)


def tail(k):
    d = unit(0.55, 0.8, -0.18)              # back and some 35 degrees out to +X
    root = np.array([0.03, 0.15, 0.17])
    k.add("stalk", tube([(0.0, 0.1, 0.19), root, root + d * 0.05], [0.045, 0.032, 0.026]), NAVY, "tail")
    o = root + d * 0.035
    ez = (0.0, 0.0, 1.0)
    s = 1.25
    paddle = [(a * s, b * s) for a, b in [(0.0, 0.012), (0.04, 0.045), (0.09, 0.066), (0.14, 0.062), (0.178, 0.03),
                                          (0.185, -0.014), (0.165, -0.054), (0.115, -0.068), (0.06, -0.052),
                                          (0.02, -0.02)]]
    k.add("paddle", slab(paddle, o, d, ez, 0.018), NAVY, "tail", fuse=False, surface="scales")
    mid = 0.115 * s
    inner = [(mid + (a - mid) * 0.8, b * 0.78 - 0.002) for a, b in paddle]
    k.add("paddlein", slab(inner, o, d, ez, 0.024), CREAM, "tail", fuse=False, surface="scales")
    # The "9": a loop (a navy oval with a cream one inside it, standing proud of both) and its stem down the right
    cc = (mid + 0.004, 0.014)
    k.add("nine", slab(qw.oval(cc, 0.04, 0.034), o, d, ez, 0.03), NAVY, "tail", fuse=False, surface="scales")
    k.add("ninein", slab(qw.oval(cc, 0.019, 0.015), o, d, ez, 0.036), CREAM, "tail", fuse=False, surface="scales")
    stem = [(cc[0] + 0.019, cc[1] - 0.004), (cc[0] + 0.041, cc[1] - 0.004), (cc[0] + 0.034, cc[1] - 0.05),
            (cc[0] + 0.012, cc[1] - 0.07), (cc[0] - 0.01, cc[1] - 0.066), (cc[0] + 0.012, cc[1] - 0.044)]
    k.add("ninestem", slab(stem, o, d, ez, 0.03), NAVY, "tail", fuse=False, surface="scales")


# --- the eyes: Qwilfish's outline, white with a small black pupil, in a magenta rim, the brow's edge dark -------------
EYE_X, EYE_Z = 0.42, 0.4
EYE_HALF = (0.05, 0.042)
CW, CK, CM, CN, CL = srgb(WHITE), srgb(INK), srgb(MAGENTA), srgb(NAVY), srgb("#B6A8D8")


def eye_open(u, v):
    f = qw.EYE_FRAC(u, v)
    c = np.tile(CW, (len(u), 1))
    rim = f > 0.66
    c[rim] = CM
    c[rim & (v > 0.31 * u + 0.05)] = CK          # the brow's dark line along the top edge
    return c


def eyes(k, state):
    ink = CK if state in ("Open", "Fierce") else CL          # a light line on the dark navy when shut
    for s in (1, -1):
        vo.patch(k, C, R, EYE_X, EYE_Z, EYE_HALF, eye_look(state, eye_open, CN, ink, line=0.16), outline=qw.EYE,
                 side=s)
        if state in ("Open", "Fierce"):
            x = EYE_X + (-0.05) * EYE_HALF[0] / R
            z = EYE_Z + (-0.28) * EYE_HALF[1] / R
            vo.patch(k, C, R, x, z, (0.012, 0.013), INK, side=s, eps=0.0045, rings=8, segs=32)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="scales")
    ball(k)
    spikes(k)
    tail(k)
    k.build(voxel=0.005, smooth=3, band=0.005)
    k.eye_states(lambda state: eyes(k, state))
    k.export(out)
