# Ciocanor sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/ciocanor.py -- <the export's Ciocanor.glb> <out.glb>
# Ciocbit grown (its frame, colours, cap, crest, headphones, speaker, eyes, wings, tail and feet, imported from
# ciocbit.py): from its picture (docs/art/reference/gemini/carpathia/10067-Ciocanor.jpg), a large drummer woodpecker
# leaning forward; a great scarlet crest of flame-like feathers swept back from the crown; over its brow a riveted
# steel plate, under which a fierce, heavy-lidded brown eye; its beak a polished steel shaft ending in a hammer's
# block; the headphones of its line, larger; on its breast a polished steel speaker plate with a sound wave; the white
# front barred in bold black bands low down, the back black spotted grey; black wings with steel-grey bars; a long
# stiff black tail tipped with steel; dark grey legs and toes with black claws.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, lathe, srgb, inside, tri, unit, args, sibling

c_ = sibling("ciocbit")
a_ = c_.a_
BLACK, WHITE, RED, GREYW, PHONES, CUP = c_.BLACK, c_.WHITE, c_.RED, c_.GREYW, c_.PHONES, c_.CUP
STEEL, STEELDK, RIVET, LEG, CLAW = "#A7AFBB", "#7A818D", "#5F6570", "#50535D", "#202128"
LIDC = "#8E909B"


def frame():
    F = c_.frame()
    F.H = np.array([0, -0.05, 0.725])               # the head's centre, over its joint (0, 0, 0.62)
    F.HR = np.array([0.132, 0.128, 0.124])
    F.BODY = ((0, 0.02, 0.435), (0.172, 0.165, 0.24), (-16, 0, 0))
    F.NECK = ((0, -0.035, 0.6), (0.118, 0.112, 0.1))
    F.BELLY = ((0, -0.02, 0.28), (0.14, 0.14, 0.125))
    F.BACKSPOTS = [(0.0, 0.58), (0.07, 0.53), (-0.07, 0.53), (0.0, 0.46), (0.08, 0.42), (-0.08, 0.42), (0.0, 0.36)]
    F.CAPZ = 0.04
    F.CREST = [((0.0, 0.0), 0.26, 44), ((0.05, 0.03), 0.22, 36), ((-0.05, 0.03), 0.22, 36), ((0.0, 0.07), 0.24, 26),
               ((0.075, 0.08), 0.18, 18), ((-0.075, 0.08), 0.18, 18)]
    F.CRESTW = (0.095, 0.042)
    F.EYEDIR = unit(0.54, -0.83, 0.02)
    F.EYE = (0.035, 0.032)
    F.PHONE = 0.018
    F.CUP = (0.056, 0.036)
    F.SPEAK = (np.array([0.05, -0.15, 0.48]), unit(0.35, -1.0, 0.1), 0.08)
    F.SHOULDER = np.array([0.15, 0.0, 0.56])
    F.TAILBASE = np.array([0.0, 0.145, 0.27])
    F.TAIL = (unit(0.32, 0.78, -0.54), 0.33, 0.11)
    F.LEGX, F.LEGY, F.LEGTOP, F.ANKLE = 0.08, 0.0, 0.2, 0.03
    F.LEGR = (0.022, 0.02)
    F.TOE, F.HEEL, F.TOER, F.CLAWLEN = 0.068, 0.042, 0.014, 0.036
    F.VOXEL = 0.0078
    F.WS = 1.75
    F.FRONT = (0.66, 0.5)
    return F


def bars(k, F):
    """Bold black bands across the white front, low down, waving a little."""
    def m(P):
        front = inside(np.arctan2(np.abs(P[:, 0]), -(P[:, 1] - 0.02)) - 1.25, 0.03)
        z = P[:, 2] + 0.007 * tri(P[:, 0] / 0.1)
        on = np.zeros(len(P))
        for z0 in (0.38, 0.31, 0.24):
            on = np.maximum(on, inside(np.abs(z - z0) - 0.017, 0.003))
        return on * front
    k.paint(m, BLACK, on=["body", "belly"])


def plate(k, F):
    """The riveted steel plate over the brow: a curved band of armour from over the bill's root up over the forehead."""
    H, HR = F.H, F.HR
    P = []
    for a in np.radians(np.linspace(-52, 52, 9)):
        # its lower edge comes down over the top third of each eye, sloping lower toward the beak
        low = -0.006 + 0.026 * abs(a) / math.radians(52)
        for z in (low, 0.06, 0.098):
            d = np.array([math.sin(a), -math.cos(a), z / 0.12])
            s = H + d / np.linalg.norm(d / HR)
            n = (s - H) / HR ** 2; n /= np.linalg.norm(n)
            P.append(s + n * 0.026)
            P.append(s - n * 0.01)
    k.add("plate", hull(P, merge=8), STEEL, "head", fuse=False, surface="metal")
    for a, z in ((-38, 0.06), (0, 0.075), (38, 0.06)):
        r = math.radians(a)
        d = np.array([math.sin(r), -math.cos(r), z / 0.12])
        s = H + d / np.linalg.norm(d / HR)
        n = (s - H) / HR ** 2; n /= np.linalg.norm(n)
        k.add("rivet", ell(s + n * 0.028, (0.014, 0.014, 0.014), sub=2), RIVET, "head", fuse=False, surface="metal")


def hammer(k, F):
    """
    The beak: a long steel shaft from the face, about as long as the head is deep, turned 20 degrees toward -X so the
    front camera sees it near profile; at its end a hammer's head, a short drum across the shaft about 0.45 of the
    head's height, its striking faces flared and plated in darker steel.
    """
    root = F.H + np.array([0, -F.HR[1] * 0.7, -0.012])
    t = math.radians(20)
    d = unit(-math.sin(t), -math.cos(t), -0.04)
    ln = 0.25
    k.add("shaft", lathe(root, d, [(0, 0), (0.036, 0.0), (0.034, ln * 0.3), (0.026, ln), (0, ln * 1.01)], seg=28),
          STEEL, "head", fuse=False, surface="metal")
    c = root + d * (ln + 0.02)
    hz = 0.45 * F.HR[2]                             # half its height: 0.45 of the head's height in all
    r = 0.038
    up = (0, 0, 1.0)
    k.add("hammer", lathe(c - np.array([0, 0, hz]), up,
                          [(0, 0.0), (r * 1.22, 0.0), (r * 1.22, hz * 0.18), (r, hz * 0.42), (r * 0.94, hz),
                           (r, hz * 1.58), (r * 1.22, hz * 1.82), (r * 1.22, hz * 2), (0, hz * 2)], seg=32),
          STEEL, "head", fuse=False, surface="metal")
    for s in (-1, 1):
        e = c + np.array([0, 0, s * (hz + 0.004)])
        k.add(f"hammerface{s}", lathe(e - np.array([0, 0, 0.006]), up,
                                      [(0, 0.0), (r * 1.06, 0.0), (r * 1.06, 0.012), (0, 0.012)], seg=32),
              STEELDK, "head", fuse=False, surface="metal")


def ciocanor(src, out_path):
    F = frame()
    k = Kit(src, coat="fur")
    c_.body(k, F)
    c_.head_marks(k, F)
    bars(k, F)
    c_.wings(k, F, spots=GREYW, bars=STEEL)
    c_.crest(k, F)
    plate(k, F)
    hammer(k, F)
    c_.headphones(k, F)
    c_.speaker(k, F, face=STEEL, surface="metal")
    c_.tail(k, F, thick=0.018, tip=0.62, tipcol=STEEL)
    a_.talons(k, F, foot=LEG, claw=CLAW)
    ob = k.build(voxel=F.VOXEL, band=0.007, wband=0.06)
    a_.board(ob)
    k.eye_states(c_.eyes(k, F, cut=lambda u, v: v > 0.42 + 0.3 * u, cutcol=LIDC))
    k.export(out_path)


if __name__ == "__main__":
    ciocanor(*args())
