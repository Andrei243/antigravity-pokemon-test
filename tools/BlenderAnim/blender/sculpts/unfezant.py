# Unfezant (the male) sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/unfezant.py -- <the export's Unfezant.glb> <out.glb>
# Tranquill grown (built by pidove.py's own functions, on a frame of its own joints): a tall pheasant with a thick
# dark grey neck and breast rising to a small head; a rose-red mask over the forehead and round both eyes, the eye's
# top cut by it, so it looks half-lidded; two long red wattles from the crown, one looping up and forward and falling
# beside the face in front of the breast, the other arching up and back and falling over the right wing; a small
# yellow hooked bill; the belly and thighs green, with green spots up the dark breast; folded wings pale grey with a
# dark scalloped bar and dark feathers behind; a tall fan of dark tail feathers held upright, crossed by pale chevrons;
# long grey shins with a black band below the green thighs, a spur behind each, grey toes and dark claws.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, sheet, hull, ring_band, srgb, inside, unit, outward, args, sibling

p = sibling("pidove")
g = p.g
DARK, BREAST, GREEN, WING, BAR = "#575962", "#3F414A", "#3E9C64", "#B9BBC2", "#4B4D56"
RED, BILL, TAILC, TAILBAR = "#DC4A68", "#F2C231", "#4A4C55", "#A9ABB3"
LEG, ANKLET, CLAW = "#8E9099", "#2C2D33", "#3A3B41"


def frame():
    F = p.frame()
    F.H = np.array([0, -0.088, 0.556])               # over the head's joint (0, -0.07, 0.42), on a long neck
    F.HR = np.array([0.058, 0.068, 0.058])
    F.BODY = ((0, 0.04, 0.29), (0.132, 0.17, 0.135), (8, 0, 0))
    F.BREAST = ((0, -0.068, 0.35), (0.102, 0.094, 0.13))
    F.NECK = ((0, -0.084, 0.46), (0.066, 0.066, 0.1))
    F.WING = ([(0.094, -0.06, 0.39), (0.124, 0.01, 0.355), (0.124, 0.11, 0.295), (0.088, 0.2, 0.245)],
              [0.05, 0.082, 0.07, 0.014], 0.34)
    F.BARS = [(0.035, 0.07)]
    F.TIPT = 0.125
    F.SCALLOP = 0.008
    F.LEGX, F.LEGTOP, F.ANKLE, F.LEGY = 0.044, 0.17, 0.024, -0.012
    F.LEGR = (0.025, 0.021)
    F.TOE, F.HEEL, F.TOER = 0.06, 0.032, 0.0105
    F.BILL = ((0, -0.163, 0.548), (0.014, 0.028, 0.012), 18)
    F.BILLLOW = ((0, -0.155, 0.535), (0.009, 0.016, 0.006), -10)
    F.EYEDIR = unit(0.62, -0.72, 0.16)
    F.EYE = (0.0175, 0.016)
    F.VOXEL = 0.0058
    F.TAILBASE = np.array([0, 0.15, 0.31])
    return F


def mask(k, F):
    """The red mask over the forehead and round the eyes, its lower edge rising from the bill's root to behind the eye."""
    E = p.eye_centre(F)
    low = lambda P: E[2] - 0.022 + 0.22 * (P[:, 1] - E[1])
    k.paint(lambda P: inside(low(P) - P[:, 2], 0.0018) * inside(P[:, 1] - (F.H[1] + 0.038 - 0.4 * (P[:, 2] - E[2])),
                                                                0.0025) * (P[:, 2] > F.H[2] - 0.05), RED)


def belly(k, F):
    """The green belly and thighs under a wavy edge, rising toward the front, green spots up the dark breast."""
    zg = lambda P: 0.245 + 0.04 * np.clip(-(P[:, 1] + 0.02) / 0.12, 0, 1) + 0.008 * np.abs(np.sin(P[:, 0] * 60))
    k.paint(lambda P: inside(P[:, 2] - zg(P), 0.003), GREEN, on=["body", "breast", "thigh"])
    for c, r, col in [((0.03, -0.16, 0.318), 0.013, GREEN), ((-0.035, -0.158, 0.311), 0.012, GREEN),
                      ((0.0, -0.165, 0.338), 0.01, GREEN), ((0.066, -0.132, 0.323), 0.011, GREEN),
                      ((-0.068, -0.13, 0.328), 0.011, GREEN), ((0.02, -0.164, 0.272), 0.009, BREAST),
                      ((-0.028, -0.162, 0.27), 0.009, BREAST)]:
        k.paint(lambda P, c=c, r=r: inside(np.linalg.norm(P - np.array(c), axis=1) - r, 0.002), col,
                on=["body", "breast"])


def ribbon(pts, width, thick, face=None):
    """
    A flat wattle along a smooth path through pts, tapering to a point. Its width lies across the model (x) where it
    can, or, given face, square to both the path and face, so that its faces look along face (outward).
    """
    P = np.array(pts, float)
    ext = np.vstack([2 * P[0] - P[1], P, 2 * P[-1] - P[-2]])
    def curve(t):
        t = np.asarray(t, float) * (len(P) - 1)
        i = np.clip(np.floor(t).astype(int), 0, len(P) - 2)
        s = (t - i)[..., None]
        p0, p1, p2, p3 = ext[i], ext[i + 1], ext[i + 2], ext[i + 3]
        return 0.5 * (2 * p1 + (-p0 + p2) * s + (2 * p0 - 5 * p1 + 4 * p2 - p3) * s * s
                      + (-p0 + 3 * p1 - 3 * p2 + p3) * s ** 3)
    def at(u, v):
        u = 0.015 + 0.97 * u
        c = curve(u)
        T = curve(np.minimum(u + 0.01, 1)) - curve(np.maximum(u - 0.01, 0))
        T /= np.linalg.norm(T, axis=-1, keepdims=True)
        if face is None:
            X = np.array([1.0, 0, 0]) - T * T[..., :1]
        else:
            X = np.cross(T, np.array(face, float))
        X /= np.linalg.norm(X, axis=-1, keepdims=True)
        w = width * (1 - 0.75 * u ** 3)
        return c + X * ((v - 0.5) * w)[..., None]
    return outward(sheet(at, 60, 4, thick))


def wattles(k, F):
    H = F.H
    # a mirror pair: each leaves the mask at the brow's side, lifts a little over the crown's level and streams back
    # and down close along the side of the head and the neck to the shoulder over the wing, its faces turned outward
    w = [(0.03, -0.112, 0.592), (0.052, -0.085, 0.616), (0.064, -0.055, 0.585), (0.07, -0.04, 0.53),
         (0.076, -0.03, 0.47), (0.09, -0.012, 0.41)]
    k.add("wattle", ribbon(w, 0.03, 0.02, face=(1, 0, 0)), RED, "head", fuse=False, mirror=True)


def legs(k, F):
    p.legs(k, F, foot=LEG, claw=CLAW)
    for s in (1, -1):
        x = F.LEGX * s
        # the black band where the green thigh ends, and a spur behind the shin
        k.add("anklet", ring_band((x * 1.01, F.LEGY - 0.002, 0.098), (0, 0, 1), F.LEGR[0] * 0.95 + 0.004, 0.016,
                                  thick=0.007), ANKLET, "leg" + ("R" if s > 0 else "L"), fuse=False)
        root = np.array([x * 1.04, F.LEGY + 0.012, 0.062])
        k.add("spur", hull([root + (0.006, -0.004, 0.008), root + (-0.006, -0.004, 0.008), root + (0, -0.004, -0.008),
                            root + (0.0, 0.008, 0.0), root + (0, 0.03, -0.012)]), LEG,
              "leg" + ("R" if s > 0 else "L"), fuse=False)


def thighs(k, F):
    """
    Green haunches fused into the belly: a round mass a side over the top of each leg, reaching down to the knee's
    black band, so that only a short grey shin shows above the toes; a low fill between them closes the crotch.
    """
    def taper(V):
        t = np.clip(-V[:, 2], 0, 1)
        V = V.copy(); V[:, 0] *= 1 - 0.25 * t; V[:, 1] *= 1 - 0.2 * t
        return V
    k.add("thigh", ell((F.LEGX + 0.004, F.LEGY + 0.012, 0.18), (0.064, 0.082, 0.085), deform=taper), GREEN,
          "leg{s}", mirror=True)
    k.add("thighfill", ell((0, F.LEGY + 0.012, 0.19), (0.075, 0.085, 0.07)), GREEN, "body")


def unfezant(src, out):
    F = frame()
    k = Kit(src, coat="fur")
    c, r, rot = F.BODY
    k.add("body", ell(c, r, rot=rot), DARK, "body")
    k.add("breast", ell(*F.BREAST), BREAST, "body")
    k.add("neck", ell(*F.NECK), BREAST, "head")
    k.add("head", ell(F.H, F.HR), DARK, "head")
    thighs(k, F)
    belly(k, F)
    mask(k, F)
    p.wings_folded(k, F, colour=WING, bar=BAR)
    p.bill(k, F, upper=BILL, lower=BILL, mouth=None, cere=None)
    wattles(k, F)
    # the side feathers plain, the broad middle one (all the camera behind sees whole) crossed by pale chevrons
    p.tail(k, F, [(20, 0.25, 0.088, TAILC), (-20, 0.25, 0.088, TAILC)], pitch=50, thick=0.013,
           bars=[(0.36, 0.03), (0.58, 0.03)], barcol=TAILBAR)
    p.tail(k, F, [(0, 0.3, 0.12, TAILC)], pitch=63, thick=0.013, bars=[(0.3, 0.032), (0.5, 0.032), (0.68, 0.028)],
           barcol=TAILBAR)
    legs(k, F)
    k.build(voxel=F.VOXEL, band=0.005)
    k.eye_states(p.eyes(k, F, lid=RED, cut=lambda u, v: v > 0.35, cutcol=RED))
    k.export(out)


if __name__ == "__main__":
    unfezant(*args())
