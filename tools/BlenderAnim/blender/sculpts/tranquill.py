# Tranquill sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/tranquill.py -- <the export's Tranquill.glb> <out.glb>
# Pidove grown (built by pidove.py's own functions, on a frame of its own joints): a longer, more level grey body on
# longer pink legs with grey thighs; a black hood over the crown and nape, ending in a ruff of black feathers at the
# nape and two black spikes standing up from the crown, the front one tall; the face, throat and chest pale grey, the
# chest's patch ending in a point; a band of rose red across the forehead from the bill's root back over the eye, its
# lower edge cutting the eye's top; the same round yellow eye in its black rim; a yellow bill, open on a pink mouth;
# folded wings pale grey with a dark bar across and dark feathers behind; a long fan of tail feathers raised steeply,
# grey with dark tips. The pictures put the red on the face as a band and make the crest black (not a pink crest).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, srgb, inside, unit, lumpy, args, sibling

p = sibling("pidove")
g = p.g
HOOD, GREY, PALE, RED = "#3C3E47", "#A6A8B0", "#D2D3D8", "#D9506A"
BILL, TAILC, TIP = "#F2B232", "#A9ABB3", "#3C3E47"


def frame():
    F = p.frame()
    F.H = np.array([0, -0.085, 0.372])               # over the head's joint (0, -0.06, 0.32)
    F.HR = np.array([0.06, 0.066, 0.06])
    F.BODY = ((0, 0.03, 0.205), (0.106, 0.152, 0.106), (6, 0, 0))
    F.BREAST = ((0, -0.062, 0.238), (0.09, 0.085, 0.1))
    F.NECK = ((0, -0.072, 0.31), (0.062, 0.06, 0.07))
    F.WING = ([(0.085, -0.04, 0.29), (0.106, 0.02, 0.26), (0.106, 0.1, 0.212), (0.074, 0.18, 0.172)],
              [0.04, 0.066, 0.056, 0.012], 0.34)
    F.BARS = [(0.03, 0.072)]
    F.TIPT = 0.115
    F.LEGX, F.LEGTOP, F.ANKLE, F.LEGY = 0.037, 0.105, 0.018, -0.01
    F.LEGR = (0.0175, 0.0145)
    F.TOE, F.HEEL, F.TOER = 0.046, 0.026, 0.0086
    F.BILL = ((0, -0.172, 0.362), (0.0145, 0.031, 0.0115), 10)
    F.BILLLOW = ((0, -0.164, 0.346), (0.011, 0.023, 0.0072), -14)
    F.MOUTH = ((0, -0.159, 0.353), (0.0105, 0.018, 0.0072))
    F.EYEDIR = unit(0.6, -0.72, 0.15)
    F.EYE = (0.0185, 0.0185)
    F.VOXEL = 0.0048
    F.TAILBASE = np.array([0, 0.13, 0.225])
    return F


def hood(k, F):
    """The pale face, throat and chest seen from the front, its foot a point; the red band across the brow."""
    PF = g.mirrored([(0.0, 0.372), (0.035, 0.374), (0.058, 0.36), (0.065, 0.33), (0.062, 0.3), (0.056, 0.27),
                     (0.05, 0.222), (0.03, 0.19), (0.0, 0.162)])
    front = lambda P: np.where(P[:, 2] > 0.32, inside(P[:, 1] - (F.H[1] + 0.012), 0.004),
                               inside(P[:, 1] + 0.08, 0.006))
    k.paint(lambda P: g.poly_mask(P[:, 0], P[:, 2], PF, edge=0.002) * front(P), PALE)
    E = p.eye_centre(F)
    low = lambda P: E[2] + 0.25 * F.EYE[1] + 0.32 * (P[:, 1] - E[1])
    k.paint(lambda P: inside(low(P) - P[:, 2], 0.0018) * inside(P[:, 2] - low(P) - 0.02, 0.0018)
            * inside(P[:, 1] - (F.H[1] - 0.004), 0.003) * (P[:, 2] > F.H[2] - 0.04), RED)


def crest(k, F, colour=HOOD):
    """Two black spikes from the crown, the front one tall and leaning back a little; the ruff at the nape."""
    top = F.H + np.array([0, 0.008, F.HR[2] * 0.85])
    k.add("crest", g.spike(top + (0, -0.012, -0.008), top + (0, 0.035, 0.085), 0.046, 0.03, side=(1, 0, 0), at=0.3),
          colour, "head", fuse=False)
    k.add("crest", g.spike(top + (0, 0.03, -0.015), top + (0, 0.075, 0.035), 0.036, 0.024, side=(1, 0, 0), at=0.3),
          colour, "head", fuse=False)
    k.add("ruff", ell((0, -0.03, 0.33), (0.068, 0.05, 0.045), deform=lumpy(5, 3, 0.18)), colour, "head")


def thighs(k, F, colour=GREY, size=(0.047, 0.054, 0.062)):
    k.add("thigh", ell((F.LEGX + 0.004, F.LEGY, F.LEGTOP + 0.008), size), colour, "leg{s}", mirror=True)


def tranquill(src, out):
    F = frame()
    k = Kit(src, coat="fur")
    p.body(k, F, body=GREY, head=HOOD)
    hood(k, F)
    crest(k, F)
    p.wings_folded(k, F, colour=PALE, bar=TIP)
    p.bill(k, F, upper=BILL, lower=BILL, cere=None)
    p.tail(k, F, [(0, 0.22, 0.07, TAILC), (18, 0.195, 0.064, TAILC), (-18, 0.195, 0.064, TAILC)], pitch=46,
           thick=0.012, tip=TIP, tipshare=0.22)
    thighs(k, F)
    p.legs(k, F)
    k.build(voxel=F.VOXEL, band=0.006)
    k.eye_states(p.eyes(k, F, lid=PALE, cut=lambda u, v: v > 0.3, cutcol=RED))
    k.export(out)


if __name__ == "__main__":
    tranquill(*args())
