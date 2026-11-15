# Swellow sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/swellow.py -- <the export's Swellow.glb> <out.glb>
# Taillow grown (sibling("taillow"): its frame, colours and functions): a taller, sleeker swallow on longer red legs;
# the red face and throat run down into a red bib crossed by a navy chevron, white at the belly's sides; narrow
# fierce eyes under a navy brow; a crest of three spiky feathers swept back from the back of the head; the folded
# wings' long tips trailing low behind, and two very long tail streamers rising steeply behind, tipped red.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, srgb, inside, unit, eye_look, sibling, args

t = sibling("taillow")
LEGRED, CLAW = "#D3445A", "#3A3A46"


def frame():
    F = t.frame()
    F.H = np.array([0, -0.1, 0.44])                  # over the head's joint (0, -0.08, 0.36)
    F.HR = np.array([0.092, 0.095, 0.088])
    F.BODY = ((0, 0.01, 0.255), (0.12, 0.15, 0.172), (-22, 0, 0))
    F.NECK = ((0, -0.07, 0.37), (0.088, 0.088, 0.085))
    F.WING = ([(0.088, -0.05, 0.37), (0.118, 0.04, 0.3), (0.11, 0.15, 0.21), (0.08, 0.25, 0.15)],
              [0.045, 0.08, 0.07, 0.028], 0.34)
    F.PRIMARY = ((0.08, 0.16, 0.2), (0.05, 0.48, 0.08), 0.08, 0.032)
    F.TAILBASE = np.array([0.012, 0.13, 0.23])
    F.PRONG = (np.array([0.085, 0.47, 0.6]), 0.04, 0.03)
    F.LEGX, F.LEGTOP, F.ANKLE, F.LEGY = 0.042, 0.17, 0.022, -0.01
    F.LEGR = (0.022, 0.019)
    F.TOE, F.HEEL, F.TOER = 0.056, 0.032, 0.012
    F.BEAK = ((0, -0.218, 0.43), (0.03, 0.06, 0.022), (6, 0, 0))
    F.EYEDIR = unit(0.6, -0.76, 0.24)
    F.EYE = (0.03, 0.026)
    F.LIDLINE = 0.25
    F.R = 0.09
    F.VOXEL = 0.0058
    # the red face, throat and bib down to a point on the breast
    F.REDMASK = [(0.0, 0.52), (0.028, 0.5), (0.05, 0.47), (0.07, 0.45), (0.085, 0.42), (0.11, 0.39),
                 (0.15, 0.33), (0.155, 0.27), (0.11, 0.22), (0.0, 0.16)]
    F.WHITEMASK = [(0.0, 0.33), (0.16, 0.33), (0.18, 0.2), (0.22, 0.1), (0.3, -0.05), (0.0, -0.05)]
    return F


def chevron(k, F):
    """The navy chevron across the red breast: a band in a V, its point down."""
    def m(P):
        a, z = t.round_head(F, P)
        mid = 0.3 + 0.45 * np.abs(a)
        return inside(np.abs(z - mid) - 0.012, 0.0025) * (np.abs(a) < 0.16) * (P[:, 1] < -0.02)
    k.paint(m, t.NAVY)


def crest(k, F):
    """Three spiky navy feathers swept back from the back of the head, splayed a little."""
    back = F.H + np.array([0, F.HR[1] * 0.7, F.HR[2] * 0.35])
    for i, (x, dz, ln) in enumerate(((0.0, 0.05, 0.13), (0.028, 0.0, 0.11), (-0.028, 0.0, 0.11))):
        tip = back + np.array([x * 2.4, ln, dz - 0.02])
        k.add(f"crest{i}", t.spike(back + np.array([x, -0.05, 0.02]), tip, 0.05, 0.03, side=(1, 0, 0.3 * np.sign(x))),
              t.NAVY, "head", fuse=False)


def streamers(k, F):
    """The two long streamers, navy with their last part red."""
    m = np.array([-1, 1, 1.0])
    tip, w, th = F.PRONG
    for s, side in ((1, "R"), (-1, "L")):
        b = F.TAILBASE * (m if s < 0 else 1)
        e = tip * (m if s < 0 else 1)
        mid = b + (e - b) * 0.68
        k.add(f"prong{side}", t.spike(b, mid + (e - b) * 0.12, w, th, at=0.85), t.NAVY, "tail", fuse=False)
        k.add(f"prongtip{side}", t.spike(mid, e, w * 1.05, th * 1.2, at=0.001), t.RED, "tail", fuse=False)


def eye_open(F, lid):
    """Narrow: a white almond, a black pupil toward the front, the navy brow slanting down toward the beak."""
    def look(u, v):
        c = np.tile(t.WHITEC, (len(u), 1))
        c[((u + 0.12) / 0.55) ** 2 + ((v + 0.12) / 0.85) ** 2 < 1] = t.INK
        c[((u + 0.3) / 0.16) ** 2 + ((v - 0.05) / 0.18) ** 2 < 1] = t.WHITEC
        edge = F.LIDLINE + 0.35 * u
        c[v > edge] = lid
        c[np.abs(v - edge) < 0.13] = t.INK
        return c
    return look


def swellow(src, out):
    F = frame()
    k = Kit(src, coat="fur")
    t.body(k, F)
    t.wings(k, F)
    t.face(k, F)
    chevron(k, F)
    t.beak(k, F)
    crest(k, F)
    streamers(k, F)
    t.legs(k, F, foot=LEGRED, claw=CLAW)
    k.build(voxel=F.VOXEL)
    LID = srgb(t.NAVY)
    k.eye_states(t.eyes(k, F, open_look=eye_open(F, LID)))
    k.export(out)


if __name__ == "__main__":
    swellow(*args())
