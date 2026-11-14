# Darkrai sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/darkrai.py -- <the export's Darkrai.glb> <out.glb>
# A black wraith that floats. From the references: a small black head with one bright cyan eye showing (the other
# under the hair), under a great plume of white hair like a billow of smoke that rises from the crown, sweeps up and
# back to the model's +X side to a ragged point and falls over the face's other side; a red collar standing round the
# neck like a jagged bowl, its teeth pointing up round the head and its front dipping to points over the chest; broad
# black shoulders with ragged tatters streaming up and back from them like torn smoke; long arms with clawed hands
# carried forward and down; a chest that narrows to a wasp waist and flares into a black bell of a skirt with a ragged
# hem of long points. The art stands it on thin legs; the skeleton has none, so the body ends in the hem.
# The skeleton: body (0, 0, 0.555), head (0, -0.01, 0.725), arms (+-0.07, 0, 0.665).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, blade, sheet, hull, outward, srgb, unit, tri, eye_look, polygon, args)

src, out = args()
k = Kit(src, coat="fur")

BLACK, SMOKE, RED, WHITE = "#30323C", "#282A33", "#D8343C", "#ECEDF1"
CYAN, PALE, PUPIL, GLINT = "#2CCDE8", "#BDF1FA", "#0C1E34", "#F2FDFF"


def ribbon(name, pts, widths, thicks, tdir, color, bone, mirror=False, surface=None):
    """A solid jagged strip: a flat-faced hull for each stretch between points, its width across the strip, thick
    along tdir at its root and thinner on to a point (a tatter, a spike of a hem, a claw)."""
    pts = [np.array(p, float) for p in pts]
    tdir = unit(*tdir)
    for i in range(len(pts) - 1):
        a, b = pts[i], pts[i + 1]
        d = b - a
        w = np.cross(d, tdir); w /= np.linalg.norm(w)
        t = np.cross(w, d); t /= np.linalg.norm(t)
        P = []
        for p, wi, ti in ((a, widths[i], thicks[i]), (b, widths[i + 1], thicks[i + 1])):
            if wi < 1e-4:
                P.append(p)
                continue
            for sw in (-0.5, 0.5):
                for st in (-0.5, 0.5):
                    P.append(p + w * wi * sw + t * ti * st)
        k.add(f"{name}{i}", hull(P), color, bone, fuse=False, mirror=mirror, surface=surface)


# --- the body: broad chest narrowing to a wasp waist, a bell of a skirt below ------------------------------------
def taper(V):
    V = V.copy()
    t = (V[:, 2] + 1) / 2
    s = 0.58 + 0.42 * t
    V[:, 0] *= s; V[:, 1] *= s
    return V

def bell(V):
    V = V.copy()
    t = np.clip((1 - V[:, 2]) / 2, 0, 1)
    s = 0.4 + 0.8 * t ** 0.9
    V[:, 0] *= s; V[:, 1] *= s
    V[:, 2] = np.where(V[:, 2] < 0, V[:, 2] * 0.55, V[:, 2])
    return V

k.add("chest", ell((0, 0.01, 0.585), (0.14, 0.095, 0.11), deform=taper), BLACK, "body")
k.add("waist", ell((0, 0.012, 0.462), (0.056, 0.05, 0.07)), BLACK, "body")
BELL_C, BELL_R = np.array([0.0, 0.015, 0.305]), np.array([0.19, 0.16, 0.15])
k.add("skirt", ell(BELL_C, BELL_R, deform=bell), BLACK, "body")
k.add("neck", tube([(0, 0.0, 0.62), (0, -0.012, 0.73)], [0.044, 0.04]), BLACK, "head")

# The shoulders: broad round masses under the collar's sides
k.add("shoulder", ell((0.165, 0.015, 0.588), (0.11, 0.1, 0.088)), BLACK, "arm{s}", mirror=True)
# one broad chest across the shoulders' roots, so they meet as one mass and not as two lumps
k.add("breast", ell((0, 0.012, 0.615), (0.17, 0.1, 0.075)), BLACK, "body")

# --- the arms: long, carried forward and down, clawed hands -------------------------------------------------------
SH, EL, WR = np.array([0.17, 0.0, 0.6]), np.array([0.245, -0.03, 0.51]), np.array([0.24, -0.11, 0.425])
k.add("arm", tube([(0.13, 0.01, 0.62), SH, EL, WR], [0.074, 0.064, 0.044, 0.03]), BLACK, "arm{s}", mirror=True)
HC = np.array([0.236, -0.14, 0.395])
k.add("hand", ell(HC, (0.032, 0.03, 0.036)), BLACK, "arm{s}", mirror=True)
for j, dx in enumerate((-0.017, 0.0, 0.017)):
    b0 = HC + (dx, -0.015, -0.02)
    ribbon(f"claw{j}_", [b0, b0 + (dx * 0.6, -0.03, -0.035), b0 + (dx * 0.9, -0.045, -0.07)],
           [0.02, 0.014, 0.0], [0.016, 0.012, 0.0], (1, 0, 0), SMOKE, "arm{s}", mirror=True)
# a ragged point at the back of the elbow
ribbon("elbow", [EL + (-0.005, 0.02, 0.005), EL + (0.03, 0.06, 0.0), EL + (0.055, 0.1, -0.01)],
       [0.05, 0.03, 0.0], [0.03, 0.02, 0.0], (0, 0, 1), BLACK, "arm{s}", mirror=True)

# --- the tatters: ragged smoke streaming up, out and back from each shoulder, kinked like lightning ----------------
TATTER = [(0.13, 0.05, 0.64), (0.26, 0.11, 0.69), (0.24, 0.15, 0.77), (0.33, 0.19, 0.81), (0.35, 0.23, 0.91)]
ribbon("tatter", TATTER, [0.14, 0.115, 0.1, 0.075, 0.0], [0.05, 0.042, 0.036, 0.03, 0.0], (0.15, 1, -0.35),
       SMOKE, "body", mirror=True)
# barbs off the tatter's kinks
for i, (at, to) in enumerate([((0.26, 0.11, 0.69), (0.33, 0.13, 0.64)), ((0.24, 0.15, 0.77), (0.19, 0.17, 0.85)),
                              ((0.33, 0.19, 0.81), (0.4, 0.21, 0.78))]):
    a, b = np.array(at), np.array(to)
    ribbon(f"barb{i}_", [a - (b - a) * 0.3, b], [0.05, 0.0], [0.03, 0.0], (0.15, 1, -0.35), SMOKE, "body",
           mirror=True)

# --- the hem: long points round the skirt's foot, out and down ----------------------------------------------------
N_HEM = 14
for i in range(N_HEM):
    th = 2 * math.pi * (i + 0.5) / N_HEM
    rad = np.array([math.sin(th), -math.cos(th), 0.0])
    root = BELL_C + np.array([0.84 * 0.172 * rad[0], 0.84 * 0.145 * rad[1], -0.055])
    long_ = i % 2 == 0
    length = 0.17 if long_ else 0.12
    jitter = 0.025 * math.sin(3.7 * i)
    tip = root + rad * (0.075 + jitter) + np.array([0, 0, -length])
    mid = root * 0.5 + tip * 0.5 + rad * 0.012
    tang = np.array([-rad[1], rad[0], 0.0])
    ribbon(f"hem{i}_", [root, mid, tip], [0.08 if long_ else 0.065, 0.045, 0.0], [0.045, 0.03, 0.0],
           tuple(rad), SMOKE if long_ else BLACK, "body")

# --- the collar: a red bowl standing round the neck, its rim of teeth pointing up, its front dipping to points -----
def collar_at(u, v):
    th = -math.pi + 0.01 + (2 * math.pi - 0.02) * u
    front = np.clip(np.cos(th), 0, None) ** 2
    zb = 0.645 - 0.02 * front - 0.016 * front * (tri(4 * th / (2 * math.pi) + 0.5) + 1) / 2
    zt = 0.69 + 0.064 * (tri(7 * th / (2 * math.pi)) + 1) / 2 + 0.01 * (1 - front)
    rx = 0.125 + 0.05 * v + 0.012 * (1 - front)
    ry = 0.088 + 0.04 * v
    z = zb + v * (zt - zb)
    return np.stack([np.sin(th) * rx, 0.005 - np.cos(th) * ry, z], axis=-1)
k.add("collar", outward(sheet(collar_at, 181, 7, 0.036)), RED, "body", fuse=False, surface="cloth")
# a dark red floor inside the bowl just under the rim's lowest notches, so its opening is red and not see-through
k.add("collarfloor", outward(ell((0, 0.005, 0.688), (0.15, 0.11, 0.008))), "#8E1E26", "body", fuse=False,
      surface="cloth")

# --- the head: small and black, a pointed chin into the collar, a flat face for the eye ----------------------------
def chin(V):
    V = V.copy()
    lo = V[:, 2] < 0
    s = np.where(lo, 1 + 0.45 * V[:, 2], 1.0)
    V[:, 0] *= s; V[:, 1] *= np.where(lo, 1 + 0.2 * V[:, 2], 1.0)
    return V
HZ = 0.03                                             # the head carried this far over its bone, out of the collar
HD = np.array([0.0, -0.02, 0.735 + HZ])
k.add("head", ell(HD, (0.068, 0.062, 0.08), deform=chin), BLACK, "head")
k.add("face", ell((0.014, -0.058, 0.728 + HZ), (0.056, 0.03, 0.058), deform=chin), BLACK, "head")

# --- the plume: white hair billowing up and back to the +X side, a few deep lobes, falling over the face's -X side --
UP = np.array([0, 0, HZ])
PLUME = [np.array(p) + UP for p in
         [(0.0, 0.015, 0.775), (0.0, 0.035, 0.845), (0.025, 0.08, 0.915), (0.075, 0.13, 0.96), (0.14, 0.18, 0.98),
          (0.205, 0.225, 0.97), (0.265, 0.26, 0.94)]]
k.add("plume", blade(PLUME, [0.115, 0.135, 0.125, 0.1, 0.072, 0.042, 0.006], unit(0.8, -0.5, 0), 0.55), WHITE,
      "head", surface="hair")
# the billow's cap over the whole top and back of the head, and two soft lumps along it
k.add("plumecap", ell(np.array((0.0, 0.03, 0.8)) + UP, (0.085, 0.08, 0.055)), WHITE, "head", surface="hair")
for i, (c, r) in enumerate([((0.045, 0.085, 0.935), (0.07, 0.06, 0.06)), ((0.135, 0.19, 0.975), (0.06, 0.05, 0.05))]):
    k.add(f"plumelump{i}", ell(np.array(c) + UP, r), WHITE, "head", surface="hair")
# the brow over the eye, and the lock that falls over the face's other side
k.add("plumebrow", ell(np.array((0.004, -0.045, 0.79)) + UP, (0.068, 0.042, 0.036)), WHITE, "head", surface="hair")
k.add("plumelock", tube([np.array(p) + UP for p in [(-0.01, -0.03, 0.8), (-0.04, -0.06, 0.765), (-0.052, -0.068, 0.715),
                                                     (-0.048, -0.07, 0.68)]],
                        [0.048, 0.042, 0.03, 0.01]), WHITE, "head", surface="hair")
# lobes torn from the plume's trailing edge and its tip
for i, (a, b, r) in enumerate([((0.07, 0.12, 0.97), (0.07, 0.2, 0.9), 0.04), ((0.15, 0.185, 0.99), (0.17, 0.26, 0.93), 0.034),
                               ((0.215, 0.23, 0.99), (0.29, 0.24, 1.02), 0.026), ((0.22, 0.235, 0.975), (0.26, 0.3, 0.9), 0.024)]):
    a, b = np.array(a) + UP, np.array(b) + UP
    k.add(f"lobe{i}", blade([a, a * 0.5 + b * 0.5, b], [r, r * 0.65, 0.005], unit(0.8, -0.5, 0), 0.62), WHITE,
          "head", surface="hair")

k.build(voxel=0.007, smooth=3, band=0.004)

# --- the eye: one cyan almond on the face's +X side, a pale rim, a dark pupil and a glint; the plume over its top ---
EC = np.array([0.03, -0.075, 0.742 + HZ])
EN = unit(0.24, -0.97, 0.04)
TILT = math.radians(14)
EUP = (-math.sin(TILT), 0.0, math.cos(TILT))          # the outer corner (+u) raised
ALMOND = polygon([(1.0, 0.12), (0.62, 0.62), (0.05, 0.86), (-0.55, 0.66), (-1.0, 0.05), (-0.6, -0.58),
                  (0.0, -0.82), (0.6, -0.6)])
C_, P_, D_, G_ = srgb(CYAN), srgb(PALE), srgb(PUPIL), srgb(GLINT)
LID, INK = srgb(BLACK), srgb("#7CC4D2")

def open_eye(u, v):
    r = np.sqrt(u * u + v * v)
    c = np.tile(C_, (len(u), 1))
    c[r > 0.82] = P_
    c[(u - 0.05) ** 2 + (v * 1.2) ** 2 < 0.4 ** 2] = D_
    c[(u + 0.1) ** 2 + (v - 0.22) ** 2 < 0.15 ** 2] = G_
    return c

def eyes(state):
    k.decal(EC, EN, (0.034, 0.024), eye_look(state, open_eye, LID, INK, line=0.16), up=EUP, eps=0.006,
            outline=ALMOND, rings=20, segs=56)

k.eye_states(eyes)
k.export(out)
