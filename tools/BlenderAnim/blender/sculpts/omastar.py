# Omastar sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/omastar.py -- <the export's Omastar.glb> <out.glb>
# Grown from omanyte.py (sibling("omanyte")): the same spiral shell swept along its coil with the same growth lines and
# dark mouth, the same blue body, horns and stubs, the same way of laying the eyes.
#
# From the references: the shell grown larger and rounder, cone spikes of the shell's cream standing out along its
# ridges (one over the face, a pair on the top, a pair behind them, one at the back). Under it a broad blue mantle,
# pointed at its lower corners; two long tentacles rise from its upper corners and curl out at their tips, two more run
# out sideways and down to the ground, short ones fan under the front. Two big round eyes just under the shell's lip,
# pale yellow in a black rim with a black upright slit for a pupil; under them a round cream beak of four fangs that
# meet in a dark cross.
# Skeleton: body (0, 0, 0.282), head (0, -0.06, 0.242).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, unit, hull, lathe, sibling, args

o = sibling("omanyte")
BLUE, SHELL = o.BLUE, o.SHELL
BEAK, MAW, YELLOW = "#EEDCA6", "#3A1620", "#F2E8A6"

CC = np.array([0.0, 0.07, 0.37])
SH = dict(r_end=0.2, th_end=110.0, grow=2.3, hc=0.42, wx=(0.13, 0.42))
FACE_C, FACE_R = np.array([0.0, -0.165, 0.16]), (0.165, 0.11, 0.135)
EYE = (0.088, -0.268, 0.22)
EYE_HALF = 0.05
BEAK_C, BEAK_R = np.array([0.0, -0.28, 0.105]), 0.05


def spike(k, th, phi, length, r0, name, lift=0.0):
    """A cone of the shell's cream standing out from the whorl at (th, phi), its root sunk in the wall."""
    p = o.whorl_at(CC, th, phi, **SH)
    q = o.whorl_at(CC, th, phi, scale=0.6, **SH)
    d = unit(*(unit(*(p - q)) + np.array([0, 0, lift])))
    k.add(name, lathe(p - d * 0.006, d, [(0, -0.02), (r0, -0.02), (r0, 0.0), (r0 * 0.45, length * 0.55),
                                          (0.003, length), (0, length + 0.002)], seg=28),
          SHELL, "body", fuse=False, surface="shell")


def beak(k):
    """Four fangs, each a quarter of a dome, meeting in a dark cross over a dark mouth."""
    k.add("maw", ell(BEAK_C + (0, 0.008, 0), (BEAK_R * 0.93, BEAK_R * 0.8, BEAK_R * 0.93), sub=3), MAW, "head",
          fuse=False, surface="shell")
    gap = 0.0055
    for i, (sx, sz) in enumerate(((1, 1), (-1, 1), (1, -1), (-1, -1))):
        pts = []
        for a in np.linspace(0, math.pi / 2, 9):
            for b in np.linspace(0, math.pi / 2, 9):
                v = np.array([math.cos(b) * math.cos(a), -math.sin(b), math.cos(b) * math.sin(a)])
                pts.append(v * (BEAK_R, BEAK_R * 0.9, BEAK_R))
        pts = np.array(pts)
        pts[:, 0] = np.maximum(pts[:, 0], 0) + gap
        pts[:, 2] = np.maximum(pts[:, 2], 0) + gap
        pts = np.vstack([pts, pts * (1, 0, 1) + (0, 0.02, 0)])
        k.add(f"fang{i}", hull(BEAK_C + pts * (sx, 1, sz), merge=0.5), BEAK, "head", fuse=False, surface="shell")


def omastar(src, out):
    k = Kit(src, coat="scales")
    o.shell(k, CC, rib_step=30.0, rib_w=0.009, **SH)
    spike(k, 74, 0.0, 0.11, 0.038, "spikefront", lift=0.9)
    for s, side in ((1, "R"), (-1, "L")):
        spike(k, 28, s * 0.55, 0.14, 0.042, f"spiketop{side}")
        spike(k, -35, s * 0.6, 0.12, 0.038, f"spikemid{side}")
        spike(k, -95, s * 0.95, 0.08, 0.03, f"spikeside{side}")
    spike(k, -110, 0.0, 0.085, 0.032, "spikeback")

    o.body(k, FACE_C, FACE_R)
    # the mantle's pointed lower corners
    k.add("corner", tube([(0.12, -0.17, 0.1), (0.19, -0.18, 0.055), (0.235, -0.18, 0.03)], [0.06, 0.04, 0.012]),
          BLUE, "head", mirror=True)
    # the long tentacles: up from the upper corners, their tips curled out
    o.horns(k, (0.12, -0.19, 0.22), [(0.17, -0.2, 0.26), (0.2, -0.205, 0.32), (0.205, -0.2, 0.38),
                                     (0.225, -0.19, 0.42), (0.255, -0.18, 0.43)],
            [0.038, 0.034, 0.028, 0.022, 0.017, 0.013])
    # out sideways and down to the ground, a little forward
    k.add("arm", tube([(0.14, -0.13, 0.12), (0.22, -0.17, 0.08), (0.25, -0.23, 0.03), (0.28, -0.25, 0.022),
                       (0.3, -0.255, 0.045)], [0.034, 0.027, 0.02, 0.016, 0.012]), BLUE, "head", mirror=True)
    o.stubs(k, FACE_C, 7, 60, 0.07, 0.02, 0.028, 0.016)
    beak(k)
    k.build(voxel=0.006, smooth=4, band=0.008)
    look = o.eye_open(sclera=YELLOW, rim=0.8, px=0.0, slit=0.2)
    k.eye_states(lambda st: o.eyes(k, st, at=EYE, half=EYE_HALF, d=(0.32, -1.0, 0.12), look=look))
    k.export(out)


if __name__ == "__main__":
    omastar(*args())
