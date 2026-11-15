# Butterfree sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/butterfree.py -- <the export's Butterfree.glb> <out.glb>
# A round dark purple-blue head on a round body, great red eyes over a little pale blue snout with two white fangs,
# two black feelers with clubbed tips, small blue hands at its chest and short blue legs hanging. Its wings are its
# character: white, rimmed and veined in black, the upper pair broad with a black crescent near its top, the lower
# pair smaller. Each wing is a black plate with its white cells laid in it as thicker plates, so that the veins and
# the rim show on the front and the back alike; the pairs stand swept back in a V, the lower pair further.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, eye_look, slab, hull, args

src, out = args()
k = Kit(src, coat="fur")

PURPLE, BLUE, BLACK, WHITE, FANG = "#5E5894", "#5CC4DC", "#24222C", "#F2F2F6", "#FAFAFA"

# --- the body ---------------------------------------------------------------------------------------------------
BC = np.array([0.0, 0.0, 0.255])
k.add("body", ell(BC, (0.066, 0.062, 0.07)), PURPLE, "body")
H = np.array([0.0, -0.012, 0.386])          # the head's middle
HR = 0.078
k.add("head", ell(H, (HR, HR * 0.94, HR * 0.96)), PURPLE, "head")
k.add("neck", ell((0, -0.006, 0.322), (0.048, 0.044, 0.03)), PURPLE, "body")

# The little pale blue snout under the eyes, and two white fangs hanging from it
SNOUT = np.array([0.0, -0.083, 0.352])
k.add("snout", ell(SNOUT, (0.026, 0.017, 0.019)), BLUE, "head")
for s in (1, -1):
    x = 0.011 * s
    k.add(f"fang{'R' if s > 0 else 'L'}", hull([(x - 0.006, -0.09, 0.338), (x + 0.006, -0.09, 0.338), (x, -0.08, 0.338),
                                                  (x, -0.094, 0.324)]), FANG, "head", fuse=False, surface="shell")

# Arms from the shoulders to small blue hands held at the chest, three round fingers each
k.add("arm", tube([(0.05, -0.03, 0.289), (0.048, -0.055, 0.28), (0.036, -0.072, 0.272)], [0.015, 0.014, 0.013]),
      PURPLE, "arm{s}", mirror=True)
k.add("hand", ell((0.032, -0.08, 0.27), (0.019, 0.015, 0.017)), BLUE, "arm{s}", mirror=True)
for j, (dx, dz) in enumerate(((-0.012, 0.012), (-0.016, 0.0), (-0.012, -0.012))):
    k.add(f"finger{j}", ell((0.032 + dx, -0.086, 0.27 + dz), (0.008, 0.008, 0.008)), BLUE, "arm{s}", mirror=True)
# Short blue legs hanging under the body, a little apart
k.add("leg", tube([(0.034, -0.02, 0.2), (0.04, -0.032, 0.16), (0.042, -0.04, 0.128)], [0.017, 0.016, 0.015]), BLUE,
      "leg{s}", mirror=True)
# Blue back from the purple at the hands' and legs' roots: a crisp line
k.paint(lambda P: inside(P[:, 2] - 0.168, 0.002) * inside(np.abs(P[:, 0]) - 0.07, 0.002), BLUE)

# The feelers: black stalks up and out from the crown, each ending in a round club
k.add("feeler", tube([(0.02, -0.02, 0.44), (0.032, -0.03, 0.5), (0.046, -0.032, 0.56), (0.056, -0.028, 0.6)],
                     [0.0062, 0.0058, 0.0055, 0.0055]), BLACK, "ear{s}", fuse=False, mirror=True)
k.add("club", ell((0.058, -0.027, 0.612), (0.011, 0.011, 0.017), rot=(0, 14, 0)), BLACK, "ear{s}", fuse=False,
      mirror=True)


# --- the wings ----------------------------------------------------------------------------------------------------
def polar(anchors, lo, hi, n=None):
    """A wing's outline as its radius from the root by angle (degrees): anchors smoothed over a dense grid."""
    th = np.arange(lo, hi + 0.5, 1.0)
    a = np.array(anchors, float)
    R = np.interp(th, a[:, 0], a[:, 1])
    for _ in range(6):
        R[1:-1] = (R[:-2] + 2 * R[1:-1] + R[2:]) / 4
    return lambda t: np.interp(t, th, R)


UPPER = polar([(-2, 0.24), (0, 0.262), (11, 0.324), (24, 0.37), (37, 0.411), (46, 0.43), (56, 0.415), (69, 0.33),
               (80, 0.25), (88, 0.19)], -2, 88)
LOWER = polar([(-96, 0.07), (-92, 0.09), (-84, 0.195), (-72, 0.227), (-52, 0.236), (-23, 0.196), (-4, 0.159),
               (-1, 0.14)], -96, -1)


def fan(R, t0, t1, inset=0.0, r0=0.0, step=1.5):
    """The plate's outline: the edge from t0 to t1 at R(theta) less inset, closed at the root (r0 out)."""
    t = np.arange(t0, t1 + 1e-6, step)
    ang = np.radians(t)
    r = R(t) - inset
    edge = np.stack([r * np.cos(ang), r * np.sin(ang)], 1)
    if r0 <= 0:
        return np.vstack([[[0.0, 0.0]], edge])
    back = np.stack([r0 * np.cos(ang[::-1]), r0 * np.sin(ang[::-1])], 1)
    return np.vstack([edge, back])


VEIN = 0.0058          # half the black between two cells


def cell(R, t0, t1, rin, rout, n=14):
    """A white cell between two veins at t0 and t1, from rin(theta) out to rout(theta); its sides stand off each vein
    by VEIN, whatever the radius."""
    pts = []
    tt = np.linspace(t0, t1, n)
    for t in tt:
        r = rout(t)
        pts.append((r, t))
    out_ = []
    for r, t in pts:
        d = math.degrees(VEIN / max(r, 1e-3))
        tc = min(max(t, t0 + d), t1 - d)
        out_.append((r * math.cos(math.radians(tc)), r * math.sin(math.radians(tc))))
    for t in tt[::-1]:
        r = rin(t)
        d = math.degrees(VEIN / max(r, 1e-3))
        tc = min(max(t, t0 + d), t1 - d)
        out_.append((r * math.cos(math.radians(tc)), r * math.sin(math.radians(tc))))
    # Points the clamp to a vein laid on one another make the triangulation fold: keep one of each
    keep = [out_[0]]
    for p in out_[1:]:
        if math.dist(p, keep[-1]) > 2e-3:
            keep.append(p)
    if math.dist(keep[0], keep[-1]) < 2e-3:
        keep.pop()
    return np.array(keep)


def upper_border(t):
    return 0.017 + 0.016 * np.exp(-((np.asarray(t) - 42) / 16) ** 2)


def lower_border(t):
    return 0.015 + 0.0 * np.asarray(t)


def wing(name, R, t0, t1, veins, border, origin, ex, ez, crescent=None):
    base = fan(R, t0, t1)
    k.add(name, slab(base, origin, ex, ez, 0.006), BLACK, "wing{s}", fuse=False, mirror=True)
    rout = lambda t: R(t) - border(t)
    rin = lambda t: 0.034
    for i, (a, b) in enumerate(zip(veins[:-1], veins[1:])):
        parts = [(rin, rout)]
        if crescent is not None:
            c0, c1, w, at = crescent
            lo_, hi_ = max(a, c0), min(b, c1)
            if hi_ > lo_:
                # the black crescent: a band across these cells, widest in its middle and tapering to its ends
                def gap(t, c0=c0, c1=c1, w=w):
                    return VEIN + w * np.sin(np.pi * np.clip((t - c0) / (c1 - c0), 0, 1)) ** 1.2
                mid = lambda t, at=at: R(t) * at
                parts = [(rin, lambda t, mid=mid, gap=gap: mid(t) - gap(t)),
                         (lambda t, mid=mid, gap=gap: mid(t) + gap(t), rout)]
        for j, (ri, ro) in enumerate(parts):
            P = cell(R, a, b, ri, ro)
            k.add(f"{name}_cell{i}_{j}", slab(P, origin, ex, ez, 0.0115), WHITE, "wing{s}", fuse=False, mirror=True)


SWEEP_UP, SWEEP_LO = 24, 36
EXU = (math.cos(math.radians(SWEEP_UP)), math.sin(math.radians(SWEEP_UP)), 0.0)
EXL = (math.cos(math.radians(SWEEP_LO)), math.sin(math.radians(SWEEP_LO)), 0.0)
EZ = (0.0, 0.14, 1.0)
WO = np.array([0.034, 0.036, 0.31])
wing("upper", UPPER, -2, 88, [-2, 12, 26, 40, 54, 68, 88], upper_border, WO, EXU, EZ,
     crescent=(46, 86, 0.012, 0.66))
wing("lower", LOWER, -96, -1, [-96, -76, -56, -36, -17, -1], lower_border, WO + np.array([0, 0.014, -0.004]),
     EXL, EZ)
# A purple root where the wings meet the back
k.add("wingroot", ell((0.03, 0.04, 0.3), (0.02, 0.022, 0.03)), PURPLE, "body", mirror=True)

# --- the eyes: great red ovals over the front of the head ----------------------------------------------------------
RED, DEEP, GLINT, LID, INK = srgb("#E0404E"), srgb("#A8222E"), srgb("#FFE6E8"), srgb(PURPLE), srgb("#1C1A24")
def eye(u, v):
    c = np.tile(RED, (len(u), 1))
    c[(v < -0.25) & (u * u + v * v > 0.35)] = DEEP
    c[((u + 0.25) / 0.24) ** 2 + ((v - 0.38) / 0.18) ** 2 < 1] = GLINT
    c[u * u + v * v > 0.84] = INK
    return c
de = unit(0.5, -0.8, 0.3)
def eyes(state):
    k.decal(H + de * HR * 0.96, de, (0.036, 0.042), eye_look(state, eye, LID, INK), eps=0.006, rings=20, segs=56,
            mirror=True)

k.build(voxel=0.005, smooth=3, band=0.005)
k.eye_states(eyes)
k.export(out)
