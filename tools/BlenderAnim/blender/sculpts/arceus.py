# Arceus (its Normal look, no plate) sculpted from nothing on the skeleton and clips of the game's own export of it
# (sculptkit.py):
#   blender --background --factory-startup --python sculpts/arceus.py -- <the export's Arceus.glb> <out.glb>
#
# From the references: a white horse-like quadruped of centaur's proportions, grey along its belly and down the front
# of its neck; a long neck carried upright with pointed fins down its back; a long narrow head with its muzzle pointing
# forward and down, a gold stripe down the face to a gold tip, green eyes with a red middle, two short pointed ears on
# top and a great white crest streaming back from the head to a point, grey beneath; round its waist a golden wheel,
# a hoop with four long spokes on its diagonals reaching past it, each with a side prong, a green gem where each spoke
# crosses the hoop; long legs with broad white shoulders and thighs, a pointed spur back from each lower leg, grey on
# the inner side below the knee, each ending in a long pointed gold hoof; a long white streamer of a tail, grey beneath.
# The export's joints: body (0, 0.02, 0.4), head (0, -0.18, 0.73), tail (0, 0.2, 0.43), legs (+-0.065, -0.13, 0.36),
# hinds (+-0.065, 0.15, 0.36).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, blade, srgb, inside, unit, eye_look, polygon, args, hull, ring_band, outward)

src, out = args()
k = Kit(src, coat="fur")

WHITE, GREY, GOLD, GOLD2, GEM = "#F0F0EC", "#6E727C", "#E2BC48", "#CFA638", "#4CE07A"
EYE_GREEN, EYE_RED, INK = "#3EC070", "#D0303A", "#1E2A26"


def kite(name, base, d, wdir, length, width, thick, bone, color, mid=0.55, midw=0.8, surface="metal"):
    """One solid blade: a kite in section (thin edges, a ridge down both faces), from base along d to a point."""
    base, d = np.array(base, float), unit(*d)
    w = np.array(wdir, float); w = w - d * (w @ d); w /= np.linalg.norm(w)
    n = np.cross(w, d)
    pts = []
    for t, ww in ((0.0, 1.0), (mid, midw)):
        c = base + d * length * t
        for a in (-1, 1):
            pts.append(c + w * a * width / 2 * ww)
            pts.append(c + n * a * thick / 2 * (1 - 0.35 * t))
    pts.append(base + d * length)
    k.add(name, hull(pts), color, bone, fuse=False, surface=surface)


# --- the trunk: a barrel, a deep chest, a rump --------------------------------------------------------------------
k.add("body", ell((0, 0.02, 0.43), (0.094, 0.18, 0.087)), WHITE, "body")
k.add("chest", ell((0, -0.11, 0.46), (0.09, 0.1, 0.094)), WHITE, "body")
k.add("rump", ell((0, 0.13, 0.44), (0.09, 0.1, 0.086)), WHITE, "body")

# --- the legs: broad shoulders and thighs fused over long tapering legs, grey inside below the knee, gold hooves ----
LX = 0.085
LEGS = (("F", -0.13, "leg{s}", 0), ("H", 0.15, "hind{s}", 1))
for tag, y, bone, hind in LEGS:
    sg = 1 if hind else -1
    if hind:
        k.add("thigh", ell((0.074, y + 0.01, 0.37), (0.07, 0.1, 0.13)), WHITE, bone, mirror=True)
    else:
        k.add("shoulder", ell((0.072, y - 0.005, 0.375), (0.064, 0.085, 0.125)), WHITE, bone, mirror=True)
    knee = (LX + 0.002, y + sg * 0.035, 0.215)
    ankle = (LX + 0.002, y + sg * 0.005, 0.1)
    k.add("leg" + tag, tube([(LX - 0.006, y, 0.36), knee, ankle], [0.05 + 0.006 * hind, 0.024, 0.015]), WHITE, bone,
          mirror=True)
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        a = np.array(ankle) * m
        # the long pointed gold hoof over the lowest third of the lower leg, down and forward to a point on the ground
        tip = np.array((s * LX, y - 0.045, 0.0))
        top = [a + (0.021 * math.cos(q), 0.021 * math.sin(q), 0.03) for q in np.linspace(0, 2 * math.pi, 7, endpoint=False)]
        midr = [a + (0.014 * math.cos(q), -0.018 + 0.014 * math.sin(q), -0.045) for q in np.linspace(0, 2 * math.pi, 6, endpoint=False)]
        k.add(f"hoof{tag}{side}", hull(top + midr + [tip]), GOLD, bone.format(s=side), fuse=False, surface="metal")
        # a gold edge up the front of the shin from the hoof toward the knee
        kn0 = np.array(knee) * m
        lo = a + (0, -0.016, 0.02)
        hi = kn0 + (0, -0.022, -0.01)
        kite(f"shin{tag}{side}", lo, hi - lo, (1, 0, 0), float(np.linalg.norm(hi - lo)), 0.016, 0.02,
             bone.format(s=side), GOLD, mid=0.5, midw=0.8)
        # a white spur back from the lower leg
        kn = np.array(knee) * m
        kite(f"spur{tag}{side}", kn + (0, 0.01, -0.035), (0, 1, -0.5), (s, 0, 0), 0.09, 0.036, 0.032,
             bone.format(s=side), WHITE, surface="fur")
# grey on the inner side of each lower leg
k.paint(lambda P: inside(P[:, 2] - 0.2, 0.004) * inside(np.abs(P[:, 0]) - (LX - 0.006), 0.003), GREY, on=["legF", "legH"])

# --- the neck: thick at its root, carried upright, grey down its front, fins down its back -------------------------
NECK = np.array([(0, -0.1, 0.47), (0, -0.15, 0.58), (0, -0.175, 0.67), (0, -0.185, 0.74)])
NECK_R = np.array([0.088, 0.064, 0.052, 0.046])
k.add("necklo", tube(NECK[:3], NECK_R[:3]), WHITE, "body")
k.add("neckhi", tube(NECK[1:], NECK_R[1:]), WHITE, "head")


def throat(P):
    """One band down the front: narrow under the chin, widening down the throat onto the chest, into the belly."""
    z = P[:, 2]
    cy = np.interp(z, [0.3, 0.45, 0.58, 0.67, 0.74], [-0.11, -0.11, -0.15, -0.175, -0.185])
    r = np.interp(z, [0.3, 0.45, 0.58, 0.67, 0.74], [0.09, 0.09, 0.064, 0.052, 0.046])
    w = np.interp(z, [0.3, 0.38, 0.47, 0.56, 0.66, 0.74], [0.07, 0.065, 0.052, 0.034, 0.022, 0.016])
    front = inside((P[:, 1] - cy) + 0.35 * r, 0.008)
    under = inside(z - 0.4, 0.006) * inside(P[:, 1] - 0.0, 0.01)
    return np.maximum(front, under) * inside(np.abs(P[:, 0]) - w, 0.006) * inside(z - 0.76, 0.006)


k.paint(throat, GREY, on=["neck", "chest", "body"])
for i, z in enumerate((0.55, 0.62, 0.69)):
    cy = np.interp(z, NECK[:, 2], NECK[:, 1]); r = np.interp(z, NECK[:, 2], NECK_R)
    kite(f"fin{i}", (0, cy + r * 0.6, z), (0, 1, 0.45), (1, 0, 0), 0.085 - 0.008 * i, 0.048, 0.036,
         "head" if z > 0.66 else "body", WHITE, surface="fur")
# grey along the belly, from the chest back under the barrel
k.paint(lambda P: inside(P[:, 2] - 0.37, 0.004) * inside(np.abs(P[:, 0]) - 0.07, 0.006), GREY,
        on=["body", "chest", "rump"])

# --- the head: a narrow skull, a long muzzle pointing forward and down, a gold stripe to a gold tip ----------------
H = np.array([0, -0.2, 0.785])
HEAD_FIRST = len(k.parts)
k.add("cranium", ell(H, (0.045, 0.055, 0.05)), WHITE, "head")
MZ = H + (0, -0.06, -0.03)
MANG = 32                    # the muzzle's tilt down at its front, degrees


def taper(V):
    V = V.copy()
    t = np.clip(-V[:, 1], 0, 1)
    V[:, 0] *= 1 - 0.45 * t
    V[:, 2] *= 1 - 0.35 * t
    return V


k.add("muzzle", ell(MZ, (0.031, 0.058, 0.032), rot=(MANG, 0, 0), deform=taper), WHITE, "head")
MTIP = MZ + np.array([0, -math.cos(math.radians(MANG)), -math.sin(math.radians(MANG))]) * 0.058
k.add("nosetip", hull([MTIP + (0, 0.022, 0.012), MTIP + (0.012, 0.018, -0.004), MTIP + (-0.012, 0.018, -0.004),
                       MTIP + (0, 0.02, -0.016), MTIP + (0, -0.008, -0.006)]), GOLD, "head", fuse=False, surface="metal")

# two short pointed ears on top of the head
for s, side in ((1, "R"), (-1, "L")):
    kite(f"ear{side}", H + (s * 0.024, 0.005, 0.03), (s * 0.18, 0.12, 1), (0, 1, 0), 0.075, 0.034, 0.03, "head", WHITE,
         surface="fur", mid=0.4, midw=0.9)

# the great crest: streaming back from the back of the head to a point, laid back, a little out to +X at its end
CREST = np.array([(0, -0.175, 0.815), (0, -0.11, 0.845), (0, -0.03, 0.862), (0.012, 0.05, 0.876), (0.03, 0.13, 0.886),
                  (0.05, 0.2, 0.892)])
k.add("crest", blade(CREST, [0.046, 0.046, 0.04, 0.03, 0.019, 0.009], (1, 0, 0), 0.62), WHITE, "head")

# the head, crest and ears grown about the head's joint (the head was some 25 pixels across in the game)
J, HS = np.array([0, -0.18, 0.73]), 1.35
for p in k.parts[HEAD_FIRST:]:
    p["V"] = J + (p["V"] - J) * HS


def grow(p):
    return J + (np.asarray(p, float) - J) * HS


H, MZ, MTIP, CREST = grow(H), grow(MZ), grow(MTIP), grow(CREST)
TAN = math.tan(math.radians(MANG))


def face_stripe(P):
    above = P[:, 2] - (MZ[2] + TAN * (P[:, 1] - MZ[1]))
    return (inside(np.abs(P[:, 0]) - 0.011 * HS, 0.0025) * inside(-above + 0.008 * HS, 0.003)
            * inside(P[:, 1] - (H[1] - 0.03 * HS), 0.003))


def lower_face(P):
    """The lower half of the face grey, from under the eye down the jaw into the throat."""
    above = P[:, 2] - (MZ[2] + 0.016 + TAN * (P[:, 1] - MZ[1]))
    return inside(above, 0.003) * inside(P[:, 1] - (H[1] + 0.01), 0.006)


k.paint(lower_face, GREY, on=["cranium", "muzzle"])
k.paint(face_stripe, GOLD, on=["cranium", "muzzle"])
k.paint(lambda P: inside(P[:, 2] - np.interp(P[:, 1], CREST[:, 1], CREST[:, 2]) + 0.008, 0.003)
        * inside(-P[:, 1] - 0.13, 0.003), GREY, on=["crest"])

# --- the tail: a long white streamer, back, up and out to +X, grey beneath ---------------------------------------
TAIL = np.array([(0, 0.2, 0.44), (0, 0.3, 0.45), (0.025, 0.4, 0.47), (0.06, 0.48, 0.52), (0.09, 0.52, 0.6)])
k.add("tail", blade(TAIL, [0.052, 0.042, 0.032, 0.02, 0.009], (1, 0, 0), 0.65), WHITE, "tail")
k.paint(lambda P: inside(P[:, 2] - np.interp(P[:, 1], TAIL[:, 1], TAIL[:, 2]) + 0.006, 0.003)
        * inside(0.27 - P[:, 1], 0.004), GREY, on=["tail"])

# --- the golden wheel round the waist: a hoop facing front and back, four spokes past it, a gem at each crossing --
WC = np.array([0, 0.06, 0.425])
WN = unit(math.sin(math.radians(16)), math.cos(math.radians(16)), -0.17)
WA = unit(*np.cross(WN, (0, 0, 1.0)) * -1)          # in the plane, toward +X
WB = unit(*np.cross(WA, WN))                         # in the plane, up
if WB[2] < 0:
    WB = -WB
WR = 0.185
k.add("hoop", ring_band(WC, WN, WR, 0.036, thick=0.026), GOLD, "body", fuse=False, surface="metal")
for i, th in enumerate((45, 135, 225, 315)):
    t = math.radians(th)
    d = math.cos(t) * WA + math.sin(t) * WB
    w = np.cross(WN, d)
    kite(f"spoke{i}", WC + d * 0.085, d, w, 0.31, 0.052, 0.042, "body", GOLD, mid=0.6, midw=0.75)
    # a short prong off each spoke past the hoop, all turned the same way round the wheel
    kite(f"prong{i}", WC + d * 0.26, unit(*(d * 0.55 + w * 0.85)), d, 0.075, 0.03, 0.03, "body", GOLD2, mid=0.5,
         midw=0.8)
    # the gem set into the spoke where it crosses the hoop: a smooth oval along the spoke, just proud of both faces
    g = WC + d * WR
    V, F = ell((0, 0, 0), (0.0125, 0.02, 0.0255), sub=3)
    k.add(f"gem{i}", outward((g + V @ np.stack([w, d, WN]), F)), GEM, "body", fuse=False, surface="metal", glow=GEM)

body = k.build(voxel=0.0068, smooth=3, band=0.005)

# --- the eyes: green with a red middle, almond-shaped, slanting up toward the back ---------------------------------
CG, CR, CK, LID = srgb(EYE_GREEN), srgb(EYE_RED), srgb(INK), srgb(WHITE)
OUT = polygon([(-1, -0.15), (-0.45, -0.7), (0.4, -0.75), (1, -0.1), (0.85, 0.45), (0.2, 0.75), (-0.6, 0.45)])


def eye(u, v):
    c = np.tile(CG, (len(u), 1))
    c[((u + 0.03) / 0.3) ** 2 + ((v - 0.0) / 0.39) ** 2 < 1] = CR
    frac = np.array([math.hypot(a, b) / OUT(math.atan2(b, a)) for a, b in zip(u, v)])
    c[frac > 0.85] = CK
    return c


EYE_DIR = unit(0.7, -0.62, 0.2)
E = H + np.array([0, -0.025, 0.0]) + EYE_DIR * 0.06


def eyes(state):
    k.decal(E, EYE_DIR, (0.03, 0.023), eye_look(state, eye, LID, CK, line=0.16), eps=0.005, rings=20, segs=56,
            outline=OUT, up=(0, 0.35, 1), mirror=True)


k.eye_states(eyes)
k.export(out)
