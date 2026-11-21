# Minior (its Meteor Form) sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/minior.py -- <the export's Minior.glb> <out.glb>
#
# From the references: a hovering ball of pinkish-brown rock, a little uneven, cracked across by a few fine dark lines,
# scattered with dark red-brown triangles. On its face two very large round holes, dark grey-green, set far apart; a
# small maroon knob on the lower outer edge of the viewer's right hole (+X). Short fat pale cones stick out of dark
# collars all round it, the face left clear.
# The pictures overrule the task's text: no swirl of a face; the face is the two holes. The picture turns the ball;
# here it faces the front, its holes an equal pair.
# The skeleton: root (0, 0, 0), body (0, 0, 0.223). The export stands 0.40 tall, its lowest point 0.043.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, lathe, lumpy, polygon, srgb, inside, unit, eye_look, args

ROCK, CRACK, MARK, HOLE, EYE = "#CF9585", "#8A564E", "#8F3F3A", "#25302D", "#33403C"
KNOB, SPIKE, COLLAR, LINE = "#74292A", "#EEEDF1", "#4A3230", "#E2B2A4"

C = np.array([0.0, 0.0, 0.223])
R = 0.125
EYE_DIR = [unit(math.sin(math.radians(33)) * s, -math.cos(math.radians(33)), -0.09) for s in (1, -1)]
HOLE_HALF, EYE_HALF = 0.045, 0.034
KNOB_DIR = unit(0.72, -0.6, -0.35)
SPIKES = [unit(-0.35, 0.05, 0.94), unit(0.75, 0.1, 0.65), unit(-0.97, -0.1, -0.2), unit(0.97, 0.05, -0.25),
          unit(0.25, -0.35, -0.9), unit(-0.5, 0.15, -0.85), unit(0.0, 0.95, 0.3), unit(-0.3, 0.85, -0.45)]
TRIANGLE = polygon([(0.0, 1.0), (-0.87, -0.5), (0.87, -0.5)])


def on(d, lift=0.0):
    return C + np.array(d) * (R + lift)


def spikes(k):
    for i, d in enumerate(SPIKES):
        k.add(f"collar{i}", lathe(C + d * (R - 0.014), d, [(0.0, 0.0), (0.032, 0.0), (0.031, 0.019), (0.023, 0.024),
                                                            (0.0, 0.024)], seg=32), COLLAR, "body", fuse=False)
        k.add(f"spike{i}", lathe(C + d * (R - 0.004), d, [(0.0, 0.0), (0.026, 0.0), (0.024, 0.012), (0.012, 0.034),
                                                           (0.0, 0.046)], seg=32), SPIKE, "body", fuse=False,
              surface="shell")


def crack(m, d0, wob, phase, span, width=0.0032):
    """A fine crack: where the ball meets a plane through its middle (normal m), wavering, within span of d0."""
    m = unit(*m); d0 = unit(*d0)
    def mask(P):
        Q = P - C
        r = np.linalg.norm(Q, axis=1) + 1e-9
        D = Q / r[:, None]
        t = np.arctan2(D @ np.cross(m, d0), D @ d0)
        off = Q @ m - wob * np.sin(5 * t + phase) - 0.5 * wob * np.sin(11 * t + 2 * phase)
        return inside(np.abs(off) - width, 0.0018) * inside(np.abs(t) - span, 0.02)
    return mask


def marks(k):
    """The dark triangles scattered over the rock, each pointing its own way, none on the holes or the collars."""
    rs = np.random.RandomState(774)
    n, placed = 160, 0
    for i in range(n):
        y = 1 - 2 * (i + 0.5) / n
        a = i * math.pi * (3 - math.sqrt(5))
        d = np.array([math.cos(a) * math.sqrt(1 - y * y), math.sin(a) * math.sqrt(1 - y * y), y])
        if any(d @ e > math.cos(math.radians(30)) for e in EYE_DIR):
            continue
        if any(d @ s > math.cos(math.radians(19)) for s in SPIKES) or d @ KNOB_DIR > math.cos(math.radians(14)):
            continue
        if rs.rand() > 0.3:
            continue
        t = np.cross(d, (0, 0, 1.0))
        if np.linalg.norm(t) < 1e-3:
            t = np.array([1.0, 0, 0])
        t /= np.linalg.norm(t)
        b = np.cross(d, t)
        ang = rs.rand() * 2 * math.pi
        up = math.cos(ang) * t + math.sin(ang) * b
        k.decal(on(d), d, (0.017, 0.017), MARK, up=up, outline=TRIANGLE, eps=0.003, rings=7, segs=36)
        placed += 1
    print("KIT: minior marks", placed)


def eye_open(u, v):
    return np.tile(srgb(EYE), (len(u), 1))


def minior(k):
    k.add("rock", ell(C, (R, R * 0.98, R * 0.99), deform=lumpy(3, 2, 0.03)), ROCK, "body")
    for m, d0, wob, ph, span in [((1, 0.05, 0.0), (0, -0.3, 1), 0.006, 0.4, 2.2), ((0.05, 0.2, 1), (0.4, -1, 0), 0.006,
                                 1.3, 1.6), ((0.6, 0.7, -0.3), (-0.6, 0.4, -0.2), 0.005, 2.0, 1.2)]:
        k.paint(crack(m, d0, wob, ph, span), CRACK)
    # the two holes: in every state
    for d in EYE_DIR:
        k.decal(on(d), d, (HOLE_HALF, HOLE_HALF), HOLE, eps=0.003, rings=20, segs=64)
    marks(k)
    k.add("knob", ell(on(KNOB_DIR, -0.004), (0.019, 0.019, 0.019)), KNOB, "body", fuse=False)
    spikes(k)


def minior_eyes(k, state):
    look = eye_look(state, eye_open, srgb(HOLE), srgb(LINE), line=0.17)
    for s, d in zip((1, -1), EYE_DIR):
        lk = look if s > 0 else (lambda f: (lambda u, v: f(-u, v)))(look)
        k.decal(on(d), d, (EYE_HALF, EYE_HALF), lk, eps=0.0048, rings=20, segs=56)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="fur")
    minior(k)
    k.build(voxel=0.0036, smooth=3, band=0.005, wband=0.05)
    k.eye_states(lambda state: minior_eyes(k, state))
    k.export(out)
