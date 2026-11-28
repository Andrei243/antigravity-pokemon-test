# Aurifer sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/aurifer.py -- <the export's Aurifer.glb> <out.glb>
#
# Aurel grown (the line's gold, from aurel.py), on the golem's frame and plated masses of cremenor.py: from its
# picture (docs/art/reference/gemini/carpathia/10109-Aurifer.jpg) and its brief ("109. Aurifer"), a heavy slow guardian
# of pale cream quartz in knapped plates, bright gold in every seam between them (the brief's gold veins); a big
# blocky head carried low and forward, a heavy stone brow over small amber eyes deep in shadow, a gold streak across
# its brow on its right; dark slate pauldrons on both shoulders, gold crystals jutting up out of them, more crystals
# out of its back and up the outsides of its forearms; slate bands round its wrists, a slate belt over its hips and
# slate plates on its knees; fists of gold nuggets; short thick legs on broad quartz feet. The sparkles drawn on it are
# an effect: the gold itself is metal, so it glints in the light.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, srgb, inside, unit, eye_look, polygon, args, hull, ring_band, boxy, sibling

g_ = sibling("cremenor")
a_ = sibling("aurel")
c_ = g_.c_
GOLD, GOLD_LT, GOLD_DK = a_.GOLD, a_.GOLD_LT, a_.GOLD_DK
QUARTZ, QUARTZ_SH, QUARTZ_LT = "#ECE3C8", "#D6C8A2", "#FAF6EA"
SLATE, SLATE_DK = "#62646C", "#4A4C54"
SOCKET = "#7E6E4E"                       # the shadow under the brow the eyes sit in
AMBER, AMBER_LT, PUPIL = "#E8922A", "#FFC45A", "#3A2210"
K = 1.08                                 # the golem's frame grown to Aurifer's height


def frame():
    G = g_.frame()
    for key, v in list(vars(G).items()):
        if isinstance(v, np.ndarray):
            setattr(G, key, v * K)
    G.TRUNK = [(n, tuple(np.array(c) * K), tuple(np.array(r) * K), rot, m + 2, s) for n, c, r, rot, m, s in G.TRUNK]
    G.FIST = (G.FIST[0] * K, G.FIST[1] * K * 1.08)
    G.FOOT = (G.FOOT[0] * K, G.FOOT[1] * K)
    G.UPPER, G.FORE = tuple(np.array(G.UPPER) * K * 1.05), tuple(np.array(G.FORE) * K * 1.05)
    G.THIGH, G.SHIN = tuple(np.array(G.THIGH) * K), tuple(np.array(G.SHIN) * K)
    # the head larger than Cremenor's, as the picture's is: its width 0.37 of the height
    G.H = np.array([0.0, -0.175, 0.645])
    G.HR = np.array([0.165, 0.15, 0.145])
    G.EYE_X, G.EYE_Z = 0.066, 0.672
    G.EYE = (0.026, 0.017)
    G.SLOT = (0.125, 0.04)
    G.VOXEL = 0.008
    G.TONES = {"belly": slate_tone, "pelvis": slate_tone, "fist": gold_tone}
    return G


def quartz_tone(mid, draw):
    if mid[2] > 0.4 and draw < 0.4:
        return QUARTZ_LT
    if draw < 0.2 or mid[2] < -0.6:
        return QUARTZ_SH
    return QUARTZ


def slate_tone(mid, draw):
    return SLATE_DK if draw < 0.3 else SLATE


def gold_tone(mid, draw):
    return GOLD_LT if draw > 0.65 else GOLD


def crystal(base, tip, w):
    """A gold crystal: a six-sided prism from base, its point at tip."""
    base, tip = np.array(base, float), np.array(tip, float)
    d = tip - base; L = np.linalg.norm(d); d /= L
    a, b = g_.axes(d)
    pts = []
    for t, k in ((0.0, 1.0), (0.72, 0.92)):
        for i in range(6):
            th = math.pi / 3 * i
            pts.append(base + d * L * t + (a * math.cos(th) + b * math.sin(th)) * w * k)
    pts.append(tip)
    return hull(pts)


def pauldrons(lot, G):
    """Slate slabs over each shoulder in knapped pieces, three gold crystals jutting up and out of each."""
    for s, side in ((1, "R"), (-1, "L")):
        c = np.array([0.255 * s, 0.0, 0.62])
        out = unit(0.62 * s, 0.0, 0.78)
        g_.mass(lot, c, (0.16, 0.17, 0.16), (0, 0, 0), 9, 51 + (s > 0), f"arm{side}", slate_tone, gap=0.05, thick=0.22,
                skip=lambda d, out=out: d @ out < 0.45)
        for dx, dy, h, lean, w in ((0.0, 0.03, 0.24, 0.25, 0.042), (0.06, -0.02, 0.17, 0.35, 0.036),
                                   (-0.065, 0.05, 0.13, 0.1, 0.03)):
            base = c + np.array([(0.04 + dx) * s, dy, 0.11])
            tip = base + unit(lean * s, 0.25, 1.0) * h
            lot.put(crystal(base, tip, w), GOLD, f"arm{side}", surface="metal")


def back_crystals(lot, G):
    for x, y, z, h, lean, w in ((0.06, 0.12, 0.78, 0.2, 0.25, 0.042), (-0.07, 0.13, 0.76, 0.15, -0.3, 0.036),
                                (0.0, 0.19, 0.68, 0.13, 0.0, 0.032)):
        base = np.array([x, y, z])
        lot.put(crystal(base, base + unit(lean, 0.55, 1.0) * h, w), GOLD, "body", surface="metal")


def forearms(lot, G):
    """Two crystals up the outside of each forearm, a slate band round each wrist."""
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        el, wr = G.ELBOW * m, G.WRIST * m
        for t, h, w in ((0.25, 0.15, 0.036), (0.55, 0.11, 0.03)):
            p = el + (wr - el) * t + np.array([0.085 * s, 0.02, 0.0])
            lot.put(crystal(p, p + unit(0.32 * s, 0.4, 0.85) * h, w), GOLD, f"arm{side}", surface="metal")
        d = wr - el
        c = el + d * 0.92
        lot.put(ring_band(c, d, G.FORE[0] * 1.0, 0.085, thick=0.06), SLATE, f"arm{side}")


def knees(lot, G):
    for s, side in ((1, "R"), (-1, "L")):
        kn = G.KNEE * np.array([s, 1, 1.0]) + np.array([0, -G.THIGH[1] * 0.95, 0.01])
        pts = []
        for i in range(6):
            th = math.pi / 3 * i + 0.3
            for dy in (0.0, -0.035):
                pts.append(kn + np.array([math.cos(th) * 0.045, dy - 0.004 * math.sin(th), math.sin(th) * 0.04]))
        lot.put(hull(pts), SLATE, f"leg{side}")


def toes(lot, G):
    for s, side in ((1, "R"), (-1, "L")):
        fc, fr = G.FOOT
        for t in (-1, 0, 1):
            c = fc * np.array([s, 1, 1.0]) + np.array([0.055 * t * s, -fr[1] * 0.85, -0.008])
            lot.put(g_.hull([c + np.array([dx, dy, dz]) for dx in (-0.022, 0.022) for dy in (-0.025, 0.02)
                             for dz in (-0.022, 0.018)]), QUARTZ_SH, f"leg{side}")


def eye_open(u, v):
    """A small narrowed amber eye with a dark pupil and a glint."""
    c = np.tile(srgb(AMBER), (len(u), 1))
    c[(v > 0.1)] = srgb(AMBER_LT)
    c[np.hypot(u + 0.05, v + 0.05) < 0.42] = srgb(PUPIL)
    c[np.hypot(u + 0.25, v - 0.25) < 0.16] = srgb("#FFFFFF")
    c[np.hypot(u, v) > 0.86] = srgb(PUPIL)
    return c


def aurifer(src, out_path):
    G = frame()
    k = Kit(src, coat="shell")
    g_.head(k, G, colour=QUARTZ, muzzle=QUARTZ_SH)
    lot = g_.golem(k, G, quartz_tone, (GOLD, None, "metal"))
    pauldrons(lot, G)
    back_crystals(lot, G)
    forearms(lot, G)
    knees(lot, G)
    toes(lot, G)
    g_.brow(lot, G, colour=QUARTZ)
    lot.flush(k, "plates")
    # the gold streak across the brow on its right, and the deep shadow the eyes sit in
    k.paint(lambda P: inside(np.abs((P[:, 2] - 0.7) - 0.45 * (P[:, 0] - 0.1)) - 0.008, 0.003)
            * inside(np.abs(P[:, 0] - 0.1) - 0.045, 0.004) * (P[:, 1] < -0.2), GOLD, on=["head"])
    sc = g_.front(G, 0.0, G.EYE_Z)
    c_.slot(k, sc, (0, -1, 0.0), G.SLOT, eps=0.004, colour=SOCKET, rim_colour=QUARTZ)
    mz = G.H + np.array([0, -0.085 * K - 0.072, -0.085])
    k.decal(mz, (0, -1, 0.1), (0.055, 0.016), "#6E5E40", eps=0.004, outline=polygon(g_.MOUTH_OUT), rings=8, segs=48)
    ob = k.build(voxel=G.VOXEL, smooth=3, band=0.008)
    c_.board(ob)
    k.eye_states(g_.eyes(k, G, g_.front(G, G.EYE_X, G.EYE_Z), unit(0.25, -1, 0.0), lid=SOCKET, ink=QUARTZ_LT,
                         open_look=eye_open))
    k.export(out_path)


if __name__ == "__main__":
    aurifer(*args())
