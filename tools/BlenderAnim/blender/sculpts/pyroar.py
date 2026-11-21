# Pyroar (the male) sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/pyroar.py -- <the export's Pyroar.glb> <out.glb>
# Litleo grown (its colours, its paws and toes, its nose, its tail's tan curl): a lion on sturdy tan legs whose back is
# Litleo's dark grey-brown, the dark coming down over the shoulders and haunches in ragged points. Its largest mass is
# the mane: a great blaze of red round a narrow tan face and down over the chest, shaped like the fire sign: two tall
# horns of flame on the crown with a yellow V between them, flames curling out at the upper sides with yellow tips, a
# broad red bib with a yellow flame under the chin, and yellow tips curling out at its lower corners. The face is long
# and narrow, a darker stripe down its bridge, dark brows slanting over sharp teal eyes, a dark nose. A long thin dark
# tail rises high behind and ends in Litleo's tan curl, grown.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, blade, hull, outward, srgb, inside, smoothstep, tri, unit, eye_look, polygon, args)

# --- colours (Litleo's coat and tan; the mane's red and gold) -----------------------------------------------------------
DARK = "#4E4442"
TAN = "#D9BC8A"
BRIDGE = "#B8946A"         # the darker stripe down the face's bridge
BROW = "#5A4640"
RED, GOLD = "#E0302C", "#F7C93C"
NOSE, INK = "#3A2C28", "#221A18"
EYE, IRIS, PUPIL = "#F2F6F4", "#2EB6C8", "#10181C"

# --- the frame (the skeleton's joints: body (0, 0.02, 0.3), head (0, -0.14, 0.38), legs (+-0.07, -0.15, 0.26), hind
#     legs (+-0.07, 0.17, 0.26), tail (0, 0.2, 0.34)) ------------------------------------------------------------------------
BODY_Z = 0.33
LEG_X = 0.072
FORE_Y, HIND_Y = -0.15, 0.17
LEG_TOP = 0.27
LEG_R = (0.05, 0.042, 0.04)
F = np.array([0, -0.305, 0.53])          # the face's centre, carried high in the mane
FR = np.array([0.061, 0.058, 0.074])
M = np.array([0, -0.2, 0.52])            # the mane's centre


def add_body(color=TAN):
    """A deep trunk, as deep as the leg showing under it."""
    k.add("chest", ell((0, -0.1, BODY_Z), (0.1, 0.1, 0.112)), color, "body")
    k.add("middle", ell((0, 0.03, BODY_Z - 0.008), (0.09, 0.13, 0.102)), color, "body")
    k.add("rump", ell((0, 0.16, BODY_Z), (0.096, 0.085, 0.106)), color, "body")


def add_legs(color=TAN):
    """Sturdy legs: broad shoulders and thighs fused over their upper halves, short thick shins, broad paws."""
    k.add("shoulder", ell((LEG_X - 0.004, FORE_Y + 0.006, LEG_TOP), (0.056, 0.064, 0.1)), color, "leg{s}", mirror=True)
    k.add("leg", tube([(LEG_X, FORE_Y, LEG_TOP), (LEG_X, FORE_Y - 0.005, 0.13), (LEG_X, FORE_Y - 0.008, 0.045)],
                      list(LEG_R)), color, "leg{s}", mirror=True)
    k.add("paw", ell((LEG_X + 0.002, FORE_Y - 0.022, 0.026), (0.047, 0.058, 0.028)), color, "leg{s}", mirror=True)
    k.add("thigh", ell((LEG_X - 0.004, HIND_Y + 0.004, LEG_TOP + 0.01), (0.064, 0.085, 0.11)), color, "hind{s}", mirror=True)
    k.add("hind", tube([(LEG_X, HIND_Y + 0.01, LEG_TOP - 0.02), (LEG_X, HIND_Y + 0.012, 0.13), (LEG_X, HIND_Y, 0.045)],
                       list(LEG_R)), color, "hind{s}", mirror=True)
    k.add("hpaw", ell((LEG_X + 0.002, HIND_Y - 0.016, 0.026), (0.046, 0.056, 0.028)), color, "hind{s}", mirror=True)
    # Litleo's three toes along the front of each paw
    for bone, y in (("leg{s}", FORE_Y - 0.022), ("hind{s}", HIND_Y - 0.016)):
        for j, dx in enumerate((-0.6, 0.0, 0.6)):
            c = (LEG_X + 0.002 + dx * 0.047, y - 0.045, 0.022)
            k.add(f"toe{bone[0]}{j}", ell(c, (0.016, 0.017, 0.02)), color, bone, mirror=True)


def add_back(color=DARK):
    """The dark of the back, its edge a row of ragged points down the flanks, longer points over shoulder and haunch."""
    def edge(P):
        y, ax = P[:, 1], np.abs(P[:, 0])
        teeth = 0.035 * (1 - np.abs(tri(y / 0.06 + 0.25)))
        over = 0.07 * np.exp(-((y - FORE_Y) / 0.05) ** 2) + 0.075 * np.exp(-((y - HIND_Y) / 0.06) ** 2)
        return 0.37 - teeth - over * (ax > 0.04)
    k.paint(lambda P: inside(edge(P) - P[:, 2], 0.0025) * (P[:, 1] > -0.2), color,
            on=["chest", "middle", "rump", "shoulder", "thigh"])


# --- the tail: long and thin, rising high behind and over, swung a little out to +X so the cameras see it --------------
TAIL = [(0.0, 0.2, 0.36), (0.005, 0.25, 0.42), (0.02, 0.29, 0.5), (0.035, 0.31, 0.57), (0.05, 0.34, 0.61), (0.07, 0.39, 0.61)]
TAIL_R = [0.022, 0.016, 0.0135, 0.0125, 0.012, 0.012]


def add_tail(color=DARK, tuft=TAN):
    k.add("tail", tube(TAIL, TAIL_R), color, "tail")
    end = np.array(TAIL[-1]); d = unit(*(end - np.array(TAIL[-2])))
    c = end + d * 0.02
    k.add("tailtuft", ell(c, (0.034, 0.042, 0.03), rot=(0, 0, -25)), tuft, "tail")
    # Litleo's curl grown: a flame of fur back and down from the tuft, and a point rising
    k.add("tailflame", blade([c + d * 0.02, c + d * 0.06 + (0, 0, -0.02), c + d * 0.08 + (0, 0, -0.06)],
                             [0.028, 0.016, 0.002], (0, 0, 1), 0.55), tuft, "tail")
    k.add("tailcurl", blade([c + (0, 0, 0.02), c + (0, -0.01, 0.045), c + (0, -0.03, 0.055)], [0.016, 0.01, 0.002],
                            (1, 0, 0), 0.6), tuft, "tail")


# --- the mane: a fused red mass round the face and over the chest, its flames as fused tongues, gold painted ----------
LOBES = {  # name: (points, radii) on +X, mirrored
    "horn": ([(0.045, -0.2, 0.62), (0.08, -0.205, 0.7), (0.105, -0.21, 0.79)], [0.075, 0.05, 0.004]),
    "upside": ([(0.11, -0.2, 0.6), (0.17, -0.205, 0.64), (0.21, -0.215, 0.61)], [0.075, 0.05, 0.004]),
    "midside": ([(0.13, -0.2, 0.5), (0.18, -0.21, 0.5), (0.205, -0.22, 0.46)], [0.08, 0.05, 0.004]),
    "lowside": ([(0.1, -0.24, 0.34), (0.16, -0.26, 0.3), (0.2, -0.27, 0.3)], [0.07, 0.045, 0.004]),
}


def add_mane(color=RED):
    k.add("manecore", ell(M + (0, 0.03, 0), (0.158, 0.15, 0.17)), color, "head")
    k.add("maneback", ell(M + (0, 0.1, -0.06), (0.135, 0.12, 0.14)), color, "head")
    k.add("bib", ell((0, -0.235, 0.37), (0.125, 0.09, 0.125)), color, "body")
    k.add("bibtip", blade([(0, -0.27, 0.32), (0, -0.285, 0.26), (0, -0.28, 0.2)], [0.06, 0.035, 0.004], (0, 1, 0), 0.5),
          color, "body")
    for nm, (pts, rr) in LOBES.items():
        bone = "body" if nm == "lowside" else "head"
        k.add(nm, blade(pts, rr, (0, 1, 0), 0.8), color, bone, mirror=True)


def add_mane_gold(color=GOLD):
    # a V between the horns on the crown, taking their inner edges
    k.paint(lambda P: inside(np.abs(P[:, 0]) - (0.012 + 0.55 * (P[:, 2] - 0.6)), 0.003) * (P[:, 2] > 0.6), color,
            on=["horn", "manecore"])
    # the tips curling out at the upper and lower sides
    k.paint(lambda P: inside(0.165 - np.abs(P[:, 0]), 0.003), color, on=["upside"])
    k.paint(lambda P: inside(0.145 - np.abs(P[:, 0]), 0.003), color, on=["lowside"])
    # a flame under the chin, pointed at both ends
    def lens(P):
        t = np.clip(1 - ((P[:, 2] - 0.385) / 0.085) ** 2, 0, None)
        return inside(np.abs(P[:, 0]) - 0.05 * t ** 0.8, 0.003) * (P[:, 1] < -0.22)
    k.paint(lens, color, on=["bib", "manecore"])


# --- the face: long and narrow, tan, standing out of the mane -----------------------------------------------------------
def add_face(color=TAN):
    k.add("neck", ell((0, -0.19, 0.43), (0.07, 0.08, 0.08)), color, "head")
    k.add("face", ell(F, FR), color, "head")
    k.add("muzzle", ell(F + (0, -0.045, -0.048), (0.034, 0.036, 0.032)), color, "head")
    k.add("chin", ell(F + (0, -0.03, -0.072), (0.026, 0.026, 0.018)), color, "head")


def add_face_marks():
    # a darker stripe down the bridge, from the forehead to the nose
    k.paint(lambda P: inside(np.abs(P[:, 0]) - (0.012 + 0.08 * np.clip(P[:, 2] - F[2], 0, None)), 0.002)
            * (P[:, 2] > F[2] - 0.042) * (P[:, 1] < F[1] - 0.02), BRIDGE, on=["face", "muzzle"])


def nose_shape(V):
    V = V.copy()
    t = np.clip((V[:, 2] + 1) / 2, 0, 1)
    V[:, 0] *= 0.3 + 0.85 * t
    return V


def add_nose():
    k.add("nose", ell(F + (0, -0.083, -0.03), (0.019, 0.01, 0.012), rot=(12, 0, 0), deform=nose_shape), NOSE, "head",
          fuse=False, surface="skin")


CT, CI = srgb(TAN), srgb(INK)


def mouth(u, v):
    c = np.tile(CT, (len(u), 1))
    c[(np.abs(v - (0.1 - 0.5 * u * u)) < 0.16) & (np.abs(u) < 0.85)] = CI
    c[(np.abs(u) < 0.09) & (v > 0.05)] = CI
    return c


def add_mouth():
    k.decal(F + (0, -0.074, -0.064), unit(0, -1, -0.35), (0.022, 0.014), mouth, eps=0.0035, rings=16, segs=64)


# --- the eyes: sharp almonds, a teal iris and a black pupil, their tops cut by a dark brow sloping down to the nose ----
EYE_DIR = unit(0.5, -0.85, 0.16)
EH = 0.022
E = F + EYE_DIR / np.linalg.norm(EYE_DIR / FR) * 0.995
BROW_CUT = (0.3, 0.45)       # the cut: v = a + b u (u outward)


def clipped(rx=1.0, ry=0.62, top=lambda u: BROW_CUT[0] + BROW_CUT[1] * u, n=96):
    pts = []
    for th in np.linspace(0, 2 * math.pi, n, endpoint=False):
        du, dv = rx * math.cos(th), ry * math.sin(th)
        t = 1.0
        if dv > top(du):
            lo, hi = 0.0, 1.0
            for _ in range(40):
                t = (lo + hi) / 2
                if t * dv > top(t * du):
                    hi = t
                else:
                    lo = t
            t = lo
        pts.append((t * du, t * dv))
    return pts


OUT = clipped()
CW, CIR, CP, CB = srgb(EYE), srgb(IRIS), srgb(PUPIL), srgb(BROW)


def eye(u, v):
    c = np.tile(CW, (len(u), 1))
    r = np.hypot((u + 0.12) / 0.56, (v + 0.02) / 0.6)
    c[r < 1] = CIR
    c[r < 0.5] = CP
    c[np.hypot(u - 0.05, v - 0.18) < 0.13] = (1, 1, 1)
    return c


def eyes(state):
    look = eye_look(state, eye, srgb(TAN), srgb(INK), line=0.17)
    for side in (1, -1):
        n = EYE_DIR * np.array([side, 1, 1.0])
        c = E * np.array([side, 1, 1.0])
        if state in ("Open", "Fierce"):
            k.decal(c, n, (EH * 1.25, EH), look if side == 1 else (lambda a, b, f=look: f(-a, b)), eps=0.004,
                    outline=polygon([(side * a / 1.0, b / 1.0) for a, b in OUT]), rings=20, segs=56)
        else:
            k.decal(c, n, (EH * 1.25, EH * 0.62), look if side == 1 else (lambda a, b, f=look: f(-a, b)), eps=0.004,
                    rings=20, segs=56)
        # the brow: a dark bar along the eye's cut, rising out to the temple
        bc = c + np.array([side * 0.004, -0.002, 0.012])
        k.decal(bc, n, (EH * 1.35, EH * 0.28), CB, up=unit(-side * 0.35, 0, 1), eps=0.0045, rings=8, segs=40)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="fur")
    add_body()
    add_legs()
    add_back()
    add_tail()
    add_mane()
    add_mane_gold()
    add_face()
    add_face_marks()
    add_nose()
    add_mouth()
    k.build(voxel=0.0072, smooth=4, band=0.006, wband=0.06)
    k.eye_states(eyes)
    k.export(out)
