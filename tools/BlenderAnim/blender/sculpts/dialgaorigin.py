# Dialga's Origin Forme sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py),
# grown from dialga.py (its colours' family, its eyes and its way of making blades as solid kites):
#   blender --background --factory-startup --python sculpts/dialgaorigin.py -- <the export's Dialga-Origin.glb> <out.glb>
#
# From the references: Dialga remade in Arceus's image, a tall crystalline quadruped of deep navy blue carried high in
# front; a deep faceted chest the largest mass, a short trunk sloping down and narrowing to small hips; a thick neck
# rising almost upright to a small head with a long crest swept up and back, two thin horns forward and up off its
# brow, a sharp muzzle and red eyes; across the chest a great angular block of bright blue standing proud like a
# breastplate; long legs whose lower halves flare into broad faceted skirts fading pale and ending in points; a long
# thin pale spur forward off each foreleg; from the middle of the back four or five great crystal blades fanning up
# and back like a wheel's spokes, navy fading to pale blue-white at their ends; bold pale cyan cracks of light: a
# branching star across each flank, a line up each side of the neck, one along the crest.
# The export's joints: body (0, 0.04, 0.5), tail (0, 0.32, 0.46), head (0, -0.3, 1.02), legs (+-0.13, -0.14, 0.44),
# hinds (+-0.13, 0.2, 0.44).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, boxy, eye_look, polygon, args, hull

src, out = args()
k = Kit(src, coat="metal")

NAVY, NAVY2, SKIRT, PALE, LINE, GEM = "#2B4C88", "#25427A", "#41659E", "#A9C2E4", "#7FE3F0", "#2F7CF2"
SKIRT_LO, TIP = "#B4CAEA", "#C2D6F0"
FACE = "#3A5C96"
EYE_RED, PUPIL, INK = "#E0303A", "#1A0A0E", "#1C2230"


def kite(name, base, d, wdir, length, width, thick, bone, color=NAVY, mid=0.55, midw=0.8, mirror=False,
         surface="metal"):
    """One solid blade (dialga.py's spike): a kite in section from base along d, width along wdir, to a point."""
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
    k.add(name, hull(pts), color, bone, fuse=False, mirror=mirror, surface=surface)


def shard(name, base, d, wdir, length, width, thick, bone, from_t, color, tip, mid=0.35, midw=1.0):
    """A great crystal blade: widest a third of the way out, its outer part a paler hull a little fuller over it."""
    base, d = np.array(base, float), unit(*d)
    kite(name, base, d, wdir, length, width, thick, bone, color, mid=mid, midw=midw)
    # its width and thickness where the pale part begins
    f = (1 - (1 - midw) * from_t / mid) if from_t <= mid else midw * (1 - (from_t - mid) / (1 - mid))
    th = 1 - 0.35 * min(from_t, mid)
    kite(name + "tip", base + d * (length * from_t - 0.006), d, wdir, length * (1 - from_t) + 0.008,
         width * f * 1.1, thick * th * 1.2, bone, tip, mid=0.04, midw=0.97)


def solid(name, pts, bone, color=NAVY):
    """A faceted mass fused into the body: a hull of its corners, remeshed with the rest (hard planes, soft seams)."""
    k.add(name, hull(pts), color, bone)


# --- the trunk: a deep faceted chest the largest mass, a short trunk sloping down and narrowing to small hips -------
solid("chest", [(0, -0.34, 0.74), (0, -0.31, 0.55), (0, -0.18, 0.42), (0, -0.12, 0.88),
                (0.17, -0.22, 0.78), (-0.17, -0.22, 0.78), (0.18, -0.2, 0.58), (-0.18, -0.2, 0.58),
                (0.12, -0.26, 0.46), (-0.12, -0.26, 0.46), (0.12, -0.04, 0.84), (-0.12, -0.04, 0.84),
                (0.15, 0.0, 0.7), (-0.15, 0.0, 0.7), (0.1, -0.04, 0.45), (-0.1, -0.04, 0.45)], "body")
solid("trunk", [(0.14, -0.06, 0.76), (-0.14, -0.06, 0.76), (0.14, -0.06, 0.48), (-0.14, -0.06, 0.48),
                (0, -0.06, 0.8), (0, -0.04, 0.44),
                (0.095, 0.26, 0.63), (-0.095, 0.26, 0.63), (0.085, 0.27, 0.44), (-0.085, 0.27, 0.44),
                (0, 0.3, 0.6), (0, 0.24, 0.41)], "body")

# --- the neck: thick at its root, an S rising almost upright to the head ---------------------------------------------
NECK = np.array([(0, -0.17, 0.72), (0, -0.25, 0.82), (0, -0.29, 0.9), (0, -0.285, 0.97), (0, -0.3, 1.03)])
NECK_R = np.array([0.13, 0.102, 0.088, 0.08, 0.075])
k.add("necklo", tube(NECK[:3], NECK_R[:3]), NAVY, "body")
k.add("neckhi", tube(NECK[1:], NECK_R[1:]), NAVY, "head")

# --- the legs: faceted shoulders and narrow thighs, each leg flaring into a faceted skirt fading pale to its points -
LX = 0.135
LEGS = (("fore", -0.14, "leg{s}", 0), ("hind", 0.2, "hind{s}", 1))
for name, y, bone, hind in LEGS:
    sg = 1 if hind else -1
    if hind:
        # the thigh: a narrow faceted block from the hip down into the skirt
        top = [(0.07, 0.12, 0.6), (0.17, 0.14, 0.58), (0.07, 0.28, 0.6), (0.16, 0.28, 0.56),
               (0.1, 0.17, 0.33), (0.17, 0.18, 0.33), (0.1, 0.25, 0.33), (0.17, 0.25, 0.33)]
    else:
        # the shoulder: a faceted block from the chest's side down the foreleg
        top = [(0.06, -0.24, 0.72), (0.17, -0.2, 0.7), (0.07, -0.06, 0.68), (0.17, -0.07, 0.64),
               (0.1, -0.2, 0.36), (0.17, -0.19, 0.36), (0.1, -0.11, 0.36), (0.17, -0.11, 0.36)]
    k.add("thigh" if hind else "shoulder", hull(top), NAVY, bone, mirror=True)
    knee = (LX + 0.012, y + sg * 0.02, 0.29)
    k.add(name, tube([(LX, y, 0.45), knee, (LX + 0.018, y + sg * 0.025, 0.2)], [0.07, 0.056, 0.05]),
          NAVY, bone, mirror=True)
    # the skirt: a six-sided cone of facets from the knee, its lower third a paler cone over it
    sc = np.array((LX + 0.018, y + sg * 0.02, 0.0))

    def ring(r, z, turn=0.0):
        return [sc + (r * math.cos(math.radians(30 + 60 * i) + turn), r * math.sin(math.radians(30 + 60 * i) + turn), z)
                for i in range(6)]
    k.add(name + "skirt", hull(ring(0.058, 0.36) + ring(0.108, 0.085, 0.3)), SKIRT, bone, fuse=False, mirror=True,
          surface="metal")
    # the paler lower part: the same cone's facets from a third of the way up, a little fuller
    k.add(name + "skirtlo", hull(ring(0.098, 0.18, 0.15) + ring(0.113, 0.08, 0.3)), SKIRT_LO, bone, fuse=False,
          mirror=True, surface="metal")
    for j in range(5):
        a = math.radians(-90 + 72 * j + (0 if hind else 36))
        rim = sc + (0.095 * math.cos(a), 0.095 * math.sin(a), 0.1)
        kite(f"{name}point{j}", rim, (0.25 * math.cos(a), 0.25 * math.sin(a), -1), (-math.sin(a), math.cos(a), 0),
             0.11, 0.07, 0.034, bone, TIP if j % 2 == 0 else PALE, mid=0.4, midw=0.75, mirror=True)
    if not hind:
        kite("spur", (LX + 0.03, y - 0.04, 0.36), (0.12, -1, -0.55), (0, 0.5, -1), 0.19, 0.034, 0.03, bone, LINE,
             mid=0.35, midw=0.7, mirror=True)

# --- a short tail buried under the fan --------------------------------------------------------------------------------
k.add("tail", tube([(0, 0.25, 0.55), (0, 0.29, 0.54), (0, 0.32, 0.53)], [0.045, 0.035, 0.015]), NAVY, "tail")

# --- the head: a small boxy skull, a sharp muzzle forward, made at a first size and grown about its joint -----------
J, HS = np.array([0, -0.3, 1.02]), 1.61
HEAD_FIRST = len(k.parts)
H = J + np.array([0, -0.035, 0.05])
k.add("cranium", ell(H, (0.068, 0.082, 0.066), deform=boxy(2.6)), NAVY, "head")


def wedge(V):
    V = boxy(3.0)(V)
    t = np.clip(-V[:, 1], 0, 1)
    V[:, 0] *= 1 - 0.55 * t
    V[:, 2] *= 1 - 0.45 * t
    return V


k.add("snout", ell(H + (0, -0.1, -0.025), (0.047, 0.1, 0.042), deform=wedge), FACE, "head")
k.add("jaw", ell(H + (0, -0.075, -0.06), (0.042, 0.08, 0.024), rot=(6, 0, 0), deform=boxy(2.4)), FACE, "head")
SN = H + np.array((0, -0.19, -0.03))
k.add("muzzletip", hull([SN + (0.02, 0.02, 0.018), SN + (-0.02, 0.02, 0.018), SN + (0.024, 0.03, -0.022),
                         SN + (-0.024, 0.03, -0.022), SN + (0, -0.045, -0.008)]), NAVY2, "head", fuse=False,
      surface="metal")
# the crest: one long blade from the top of the skull, up and back, its end pale, a bright cyan line along it
CB, CD = H + (0, -0.03, 0.045), unit(0, 1, 0.3)
shard("crest", CB, CD, (0, -0.3, 1), 0.26, 0.075, 0.036, "head", 0.6, NAVY, TIP, mid=0.45, midw=0.85)
kite("crestline", CB + CD * 0.03 + (0, 0, 0.004), CD, (0, -0.3, 1), 0.19, 0.02, 0.046, "head", LINE, mid=0.5,
     midw=0.9)
for s, side in ((1, "R"), (-1, "L")):
    kite(f"horn{side}", H + (s * 0.03, -0.06, 0.045), (s * 0.2, -0.85, 1), (s, 0, 0), 0.11, 0.034, 0.03,
         "head", NAVY2, mid=0.45, midw=0.75)
    kite(f"cheek{side}", H + (s * 0.058, -0.005, 0.0), (s * 0.4, 0.8, 0.3), (0, 0, 1), 0.1, 0.035, 0.03, "head",
         NAVY2)
    kite(f"jawpoint{side}", H + (s * 0.042, -0.02, -0.06), (s * 0.3, 0.45, -0.8), (0, 1, 0), 0.075, 0.032, 0.03,
         "head", NAVY2)

for p in k.parts[HEAD_FIRST:]:
    p["V"] = J + (p["V"] - J) * HS
H = J + (H - J) * HS

# --- the gem: a great angular block of bright blue standing proud of the chest like a breastplate -------------------
GC = np.array([0, -0.315, 0.71])
GW, GH = 0.18, 0.085                       # its half width (some 1.6 heads across) and half height
gem = [GC + (0, -0.085, 0.0), GC + (0, -0.07, GH), GC + (0, -0.07, -GH)]
for s in (1, -1):
    gem += [GC + (s * 0.11, -0.065, GH * 0.95), GC + (s * 0.11, -0.065, -GH * 0.95),
            GC + (s * GW, -0.025, GH * 0.5), GC + (s * GW, -0.025, -GH * 0.45),       # the bevelled ends
            GC + (s * (GW - 0.02), 0.05, GH * 0.5), GC + (s * (GW - 0.02), 0.05, -GH * 0.45),
            GC + (s * 0.11, 0.06, GH * 0.95), GC + (s * 0.11, 0.06, -GH * 0.95)]
gem += [GC + (0, 0.07, GH), GC + (0, 0.07, -GH)]
k.add("gem", hull(gem), GEM, "body", fuse=False, surface="metal", glow=GEM)

# --- the fan: five great crystal blades from the middle of the back, up and back in a plane turned 30 degrees ------
FAN_ROOT = np.array([0, 0.04, 0.74])
BACK = unit(math.sin(math.radians(30)), math.cos(math.radians(30)), 0)
UP = np.array([0, 0, 1.0])
NRM = np.cross(BACK, UP)
FAN = ((104, 0.5), (78, 0.54), (54, 0.62), (31, 0.62), (9, 0.52))
for i, (e, L) in enumerate(FAN):
    e = math.radians(e)
    d = BACK * math.cos(e) + UP * math.sin(e)
    root = FAN_ROOT + d * 0.03 + NRM * (0.02 if i % 2 else -0.02)
    wdir = np.cross(NRM, d)
    shard(f"fan{i}", root, d, wdir, L, 0.2, 0.058, "body", 0.64, NAVY if i % 2 else NAVY2, TIP)

# --- the pale lines: bold cracks of light, a branching star across each flank, a line up each side of the neck -----


def lines(segs, w, xmin, xmax):
    """A crisp bright line along points of (y, z), seen from the side: on the surface between |x| xmin and xmax."""
    segs = [np.array(s, float) for s in segs]

    def mask(P):
        q = P[:, 1:]
        dm = np.full(len(P), 9.0)
        for s in segs:
            for a, b in zip(s[:-1], s[1:]):
                ab = b - a
                t = np.clip(((q - a) @ ab) / (ab @ ab), 0, 1)
                dm = np.minimum(dm, np.linalg.norm(q - (a + t[:, None] * ab), axis=1))
        x = np.abs(P[:, 0])
        return inside(dm - w, 0.0035) * inside(xmin - x, 0.003) * inside(x - xmax, 0.003)
    return mask


# the flank's star: from a heart on the trunk's side, four jagged arms to the shoulder, the hip, the fan, the leg
STAR = (0.05, 0.6)
k.paint(lines([[STAR, (-0.03, 0.66), (-0.08, 0.64), (-0.15, 0.7)],
               [STAR, (0.12, 0.57), (0.17, 0.6), (0.25, 0.56)],
               [STAR, (0.06, 0.68), (0.03, 0.73), (0.06, 0.8)],
               [STAR, (0.1, 0.52), (0.16, 0.46), (0.19, 0.38), (0.21, 0.33)]], 0.016, 0.07, 0.4), LINE)
# up each side of the neck to the head
k.paint(lines([[(-0.2, 0.76), (-0.25, 0.84), (-0.29, 0.92), (-0.3, 1.0), (-0.31, 1.06)]], 0.015, 0.045, 0.2), LINE)

body = k.build(voxel=0.0078, smooth=2, band=0.004)

# --- the eyes: dialga.py's, red, a dark pupil, a dark rim, the top edge sloping down to the snout (a scowl) ----------
CR, CP, CK, CW, LID = srgb(EYE_RED), srgb(PUPIL), srgb(INK), srgb("#FFFFFF"), srgb(NAVY)
OUTLINE = [(-1, -0.05), (-0.5, -0.62), (0.3, -0.78), (0.85, -0.45), (1, 0.2), (0.75, 0.62), (0.1, 0.42), (-0.6, 0.15)]
EYE_OUT = polygon(OUTLINE)


def eye(u, v):
    c = np.tile(CR, (len(u), 1))
    c[((u + 0.05) / 0.24) ** 2 + ((v + 0.05) / 0.44) ** 2 < 1] = CP
    c[((u - 0.2) / 0.14) ** 2 + ((v - 0.18) / 0.14) ** 2 < 1] = CW
    frac = np.array([math.hypot(a, b) / EYE_OUT(math.atan2(b, a)) for a, b in zip(u, v)])
    c[frac > 0.84] = CK
    return c


EYE_DIR = unit(0.72, -0.62, 0.18)
E = H + (np.array([0.0, -0.035, 0.008]) + EYE_DIR * 0.07) * HS


def eyes(state):
    k.decal(E, EYE_DIR, (0.035 * HS, 0.028 * HS), eye_look(state, eye, LID, CK, line=0.18), eps=0.005, rings=20,
            segs=56, outline=EYE_OUT, mirror=True)


k.eye_states(eyes)
k.export(out)
