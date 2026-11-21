# Trumbeak sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/trumbeak.py -- <the export's Trumbeak.glb> <out.glb>
# Pikipek grown (its frame's names, its bill, wings, feet and way of making the eyes, imported from pikipek.py): from
# the references, an upright black bird with an egg of a body on visible blue-grey legs; a black head whose white face
# runs round the eye and down the throat; a stern blue eye under a black brow; a long straight orange bill like a
# trumpet, a darker band near its root and a black tip; black feathers swept back from the head in a spiky crest; a
# red-orange collar in a V across the throat with a zigzag lower edge over a grey bib; wings raised up and back, a
# black leading feather over long white ones; a fan of spiky black tail feathers raised behind.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, slab, srgb, inside, unit, args, sibling

p = sibling("pikipek")
g = p.g
BLACK, WHITE, COLLAR, BIBC = "#2E2F36", "#F4F4F1", "#E0482C", "#9C9EA6"
ORANGE, BAND, TIP, LEGC, CLAW = "#F2702C", "#D2452A", "#26262B", "#6E92B8", "#3A3C46"


def frame():
    F = p.frame()
    F.H = np.array([0, -0.044, 0.256])              # the head's centre, over its joint (0, -0.037, 0.2)
    F.HR = np.array([0.046, 0.049, 0.046])
    F.BODY = ((0, 0.012, 0.13), (0.061, 0.068, 0.079), (12, 0, 0))
    F.NECK = ((0, -0.03, 0.205), (0.042, 0.042, 0.042))
    F.LEGX, F.LEGTOP, F.ANKLE, F.LEGY = 0.03, 0.075, 0.014, -0.004
    F.LEGR = (0.0072, 0.0064)
    F.TOE, F.HEEL, F.TOER = 0.04, 0.025, 0.0052
    F.BILL = (np.array([0, -0.08, 0.25]), unit(0, -1, 0.1), 0.132, 0.0175)
    F.TIPAT = 0.74
    F.BANDS = [(0.28, 0.4, BAND)]
    F.EYEDIR = unit(0.64, -0.72, 0.06)
    F.EYE = (0.0165, 0.0175)
    F.VOXEL = 0.0033
    F.BAND, F.WBAND = 0.004, 0.035
    # the white face in (y, z) about the head's centre, seen from the side
    F.FACE = [(-0.07, 0.002), (-0.03, 0.018), (-0.005, 0.03), (0.016, 0.024), (0.026, 0.004), (0.026, -0.03),
              (0.016, -0.06), (0.01, -0.09), (-0.1, -0.09), (-0.1, 0.0)]
    # the collar and the bib, right halves in (x, z) from the top middle round to the bottom middle
    F.COLLAR = [(0.0, 0.207), (0.048, 0.236), (0.052, 0.214), (0.032, 0.2), (0.017, 0.205), (0.0, 0.186)]
    F.BIB = [(0.0, 0.188), (0.017, 0.206), (0.032, 0.202), (0.046, 0.19), (0.04, 0.162), (0.02, 0.15), (0.0, 0.142)]
    # the crest: spikes back from the crown (base, tip as offsets from the head's centre, width, thickness)
    F.SPIKES = [((0.0, 0.0, 0.032), (0.012, 0.104, 0.058), 0.032, 0.014),
                ((0.0, 0.02, 0.016), (-0.01, 0.11, 0.02), 0.032, 0.014),
                ((0.0, 0.026, -0.004), (0.014, 0.09, -0.022), 0.028, 0.013),
                ((0.0, 0.024, -0.02), (-0.008, 0.066, -0.048), 0.022, 0.012)]
    F.SHOULDER = np.array([0.044, -0.002, 0.18])
    F.WROOT = (0.008, 0.012, 0.012)
    F.ARM = unit(0.55, 0.5, 0.67)
    F.BACK = unit(0.74, 0.66, -0.1)
    F.FEATHERS = [(0.0, 0.12, 0.034, BLACK), (0.3, 0.132, 0.042, WHITE), (0.6, 0.12, 0.04, WHITE),
                  (0.9, 0.098, 0.034, WHITE)]
    F.REACH = 0.032
    F.FTHICK = 0.0095
    F.TAILBASE = np.array([0, 0.07, 0.1])
    return F


POINTED = [(0.0, 0.36), (0.25, 0.8), (0.55, 1.0), (0.8, 0.7), (1.0, 0.0)]


def face(k, F, colour=WHITE):
    k.paint(lambda P: p.side_mask(F, P, F.FACE), colour)


def collar(k, F, colour=COLLAR, bib=BIBC):
    C, B = g.mirrored(F.COLLAR), g.mirrored(F.BIB)
    front = lambda P: inside(P[:, 1] + 0.022, 0.004)
    k.paint(lambda P: g.poly_mask(P[:, 0], P[:, 2], B, edge=0.0016) * front(P), bib)
    k.paint(lambda P: g.poly_mask(P[:, 0], P[:, 2], C, edge=0.0016) * front(P), colour)


def crest(k, F, colour=BLACK):
    for i, (b, t, w, th) in enumerate(F.SPIKES):
        k.add(f"crest{i}", g.spike(F.H + np.array(b), F.H + np.array(t), w, th, side=(0.45, 0, 1)), colour, "head",
              fuse=False)


def tail_fan(k, F, feathers, colour=BLACK, pitch=34, thick=0.009):
    """Pointed black feathers from the rump raised up and back, the outer ones banked: (yaw, length, width) each."""
    pr = math.radians(pitch)
    for i, (yaw, ln, w) in enumerate(feathers):
        y = math.radians(yaw)
        d = np.array([math.sin(y) * math.cos(pr), math.cos(y) * math.cos(pr), math.sin(pr)])
        side = np.array([math.cos(y), -math.sin(y), 0])
        r = math.radians(1.4 * yaw + 20)
        up = np.cross(side, d)
        side = side * math.cos(r) + up * math.sin(r)
        b = F.TAILBASE + np.array([math.sin(y) * 0.012, 0, 0])
        k.add(f"tail{i}", slab(p.feather_outline(ln, w, POINTED), b, d, side, thick), colour, "tail", fuse=False)


def trumbeak(src, out_path):
    F = frame()
    k = Kit(src, coat="fur")
    p.body(k, F, body=BLACK, head=BLACK, neck=BLACK)
    face(k, F)
    collar(k, F)
    crest(k, F)
    p.bill(k, F, colour=ORANGE, tip=TIP, bands=F.BANDS)
    p.wings(k, F, shape=POINTED)
    tail_fan(k, F, [(0, 0.1, 0.034), (24, 0.088, 0.03), (-24, 0.088, 0.03), (44, 0.07, 0.026), (-44, 0.07, 0.026)])
    p.legs(k, F, foot=LEGC, claw=CLAW)
    k.build(voxel=F.VOXEL, band=F.BAND, wband=F.WBAND)
    k.eye_states(p.eyes(k, F, cut=lambda u, v: v > 0.5 - 0.12 * u, cutcol=BLACK))
    k.export(out_path)


if __name__ == "__main__":
    trumbeak(*args())
