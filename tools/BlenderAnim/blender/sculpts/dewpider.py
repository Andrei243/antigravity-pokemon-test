# Dewpider sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/dewpider.py -- <the export's Dewpider.glb> <out.glb>
#
# From the references: a little water spider whose head lives in a bubble of water carried on top. The bubble is a
# big pale sphere (some two thirds of the height), made here as an opaque pale shell with a round window at the front
# through which the head shows. The head inside: round and blue, lighter above, with a dark slate band round its
# middle that carries the two pale almond eyes (a dark upright pupil each) and dips at the front to a lime-green ring
# of a nose; two blue knobs on its crown. Under the bubble a lime-green star of a collar, and hanging below it a dark
# slate teardrop of an abdomen with a small point. Three legs a side, thick and lime-green: the front pair rises up the
# bubble's sides and hooks in over it like fingers holding it, the other two go out and step down to points.
#
# Written to be grown from (araquanid.py imports it): the bubble, the head in it, the eyes, the nose and the legs are
# functions of the frame below.
# Skeleton: body (0, 0, 0.1), head (0, 0, 0.12), legs at (+-0.02, 0.01 / 0.04 / 0.07, 0.09) on leg, leg.001, hind.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, lathe, hull, srgb, inside, unit, eye_look, outward, args

LIME, BLUE, BLUE_LO, BAND = "#C0D43A", "#58B6DC", "#3F8FBA", "#2C5470"
WATER, GLINT, SLATE, SLATE_TIP = "#CBE4EE", "#FBFDFF", "#505A6A", "#323A46"
EYE_W, PUPIL, NOSE_RIM, NOSE_IN = "#BEEAF6", "#1C3448", "#A8D24E", "#3C6A3C"


# --- helpers -----------------------------------------------------------------------------------------------------
def on_ell(c, r, d):
    """The point of an ellipsoid (centre c, radii r) in direction d from its centre, and its outward normal."""
    c, r, d = np.array(c, float), np.array(r, float), unit(*d)
    p = c + d / math.sqrt(((d / r) ** 2).sum())
    n = (p - c) / r ** 2
    return p, n / np.linalg.norm(n)


def lens(c, n, a, b, t, up=(0, 0, 1)):
    """A small flattened ellipsoid lying on a surface at c (normal n): a glint, a scale."""
    n = unit(*n)
    e1 = np.cross(up, n); e1 /= np.linalg.norm(e1)
    e2 = np.cross(n, e1)
    V, F = ell((0, 0, 0), (a, b, t), sub=3)
    V = V[:, :1] * e1 + V[:, 1:2] * e2 + V[:, 2:] * n + np.asarray(c, float)
    return outward((V, F))


def shell(k, c, R, axis, open_deg, thick, colour, bone="head", name="bubble"):
    """The bubble: a round shell of turning about axis with a round window of open_deg (half angle) at axis' end."""
    th = math.radians(open_deg)
    outer = [(R * math.sin(p), R * math.cos(p)) for p in np.linspace(math.pi, th, 40)]
    ri = R - thick
    inner = [(ri * math.sin(p), ri * math.cos(p)) for p in np.linspace(th, math.pi, 40)]
    prof = [(0.0, -R)] + outer[1:] + inner[:-1] + [(0.0, -ri)]
    k.add(name, lathe(c, axis, prof, seg=72), colour, bone, fuse=False, surface="shell")


def glints(k, c, R, colour=GLINT, bone="head"):
    """A curved white glint on the bubble's upper left, as the pictures draw it (three lenses along an arc)."""
    for el, az, a in ((62, -62, 0.24), (48, -76, 0.2), (32, -86, 0.15)):
        d = np.array([math.sin(math.radians(az)) * math.cos(math.radians(el)),
                      -math.cos(math.radians(az)) * math.cos(math.radians(el)), math.sin(math.radians(el))])
        k.add("glint", lens(np.asarray(c) + d * (R + 0.0008), d, R * a, R * 0.055, R * 0.02), colour, bone,
              fuse=False, surface="shell")


def head_in_bubble(k, C, RR, knobs=0.3, top=BLUE, low=BLUE_LO, band=BAND, band_h=0.3):
    """The round blue head: lighter above, darker below, a dark band round its middle (band_h of its height half)
    dipping at the front, two blue knobs on the crown (knobs: their size against the head)."""
    C, RR = np.asarray(C, float), np.asarray(RR, float)
    k.add("head", ell(C, RR), top, "head")
    for s in (1, -1):
        kc = C + np.array([0.48 * RR[0] * s, -0.12 * RR[1], 0.78 * RR[2]])
        k.add("knob", ell(kc, RR * knobs, rot=(0, s * 22, 0), deform=lambda V: V * np.array([1, 1, 1.0]) +
                          np.array([0, 0, 0.35]) * np.clip(V[:, 2:], 0, 1) ** 2), top, "head")
    h = band_h * RR[2]
    zc = C[2] + 0.06 * RR[2]
    def band_mask(P):
        x, y, z = P[:, 0] - C[0], P[:, 1] - C[1], P[:, 2]
        front = np.clip(-y / RR[1], 0, 1)
        dip = 0.35 * RR[2] * np.exp(-(x / (0.28 * RR[0])) ** 2) * front
        return inside(np.abs(z - zc + dip * 0.5) - h - dip * 0.5, 0.0012) * inside(np.abs(x) - RR[0] * 1.2)
    k.paint(lambda P: inside(P[:, 2] - (zc - h) + 0.002, 0.0015) * inside(np.abs(P[:, 0]) - RR[0] * 1.2), low, on=["head"])
    k.paint(band_mask, band, on=["head"])
    return zc


def nose(k, C, RR, zc, rim=NOSE_RIM, hole=NOSE_IN, size=0.2):
    """The lime-green ring of a nose at the band's front dip."""
    p, n = on_ell(C, RR, (0, -1, (zc - C[2] - 0.42 * RR[2]) / RR[2]))
    r = size * RR[0]
    k.decal(p, n, (r, r * 1.1), rim, eps=0.0016, rings=12, segs=48)
    k.decal(p, n, (r * 0.5, r * 0.62), hole, eps=0.0024, rings=10, segs=40)


def almond_eye(white=EYE_W, pupil=PUPIL, rim=PUPIL, rim_w=0.14):
    CW, CP, CR = srgb(white), srgb(pupil), srgb(rim)
    def eye(u, v):
        r = np.sqrt(u * u + v * v)
        c = np.tile(CW, (len(u), 1))
        c[r > 1 - rim_w] = CR
        c[((u + 0.08) / 0.24) ** 2 + (v / 0.62) ** 2 < 1] = CP
        return c
    return eye




def eyes_on(k, state, C, RR, zc, half, az=0.48, lid=BAND, line="#9ED4EA", look=None):
    lk = eye_look(state, look or almond_eye(), srgb(lid), srgb(line), line=0.18)
    p, n = on_ell(C, RR, (az, -1.0, (zc - C[2]) / RR[2] + 0.06))
    k.decal(p, n, half, lk, eps=0.0018, rings=20, segs=56, mirror=True)


def leg(k, name, bone, pts, radii, colour=LIME, mirror=True):
    """A leg as one loose tube on +X, mirrored."""
    k.add(name, outward(tube(pts, radii, seg=18)), colour, bone, fuse=False, mirror=mirror, surface="shell")


def arch_leg(k, name, bone, c, R, az, els, out, radii, colour=LIME, curl=0.0):
    """A leg that rises up a bubble's side outside it: points at azimuth az (degrees from the front toward +X) and
    the elevations els, out off the surface; the last points curl inward toward the front by curl degrees."""
    pts = []
    n = len(els)
    for i, el in enumerate(els):
        a = math.radians(az - curl * max(0, i - (n - 3)) / 2)
        e = math.radians(el)
        d = np.array([math.sin(a) * math.cos(e), -math.cos(a) * math.cos(e), math.sin(e)])
        pts.append(np.asarray(c) + d * (R + out[i]))
    leg(k, name, bone, pts, radii, colour)


def collar(k, c, r, n_pts, colour=LIME, rise=0.35, thick=0.012, bone="body", phase=0.0):
    """A star of points round the bubble's foot: hulls radiating out and a little up."""
    c = np.asarray(c, float)
    k.add("collarcore", outward(ell(c, (r * 0.5, r * 0.5, thick * 0.7), sub=3)), colour, bone, fuse=False,
          surface="shell")
    for i in range(n_pts):
        a = 2 * math.pi * (i + phase) / n_pts
        d = np.array([math.sin(a), -math.cos(a), rise]); d /= np.linalg.norm(d)
        side = np.array([math.cos(a), math.sin(a), 0.0])
        root = c + d * r * 0.3
        tip = c + d * r
        w = r * 0.28
        pts = [root + side * w, root - side * w, root + (0, 0, thick * 0.5), root - (0, 0, thick * 0.5),
               c + d * r * 0.62 + side * w * 0.55, c + d * r * 0.62 - side * w * 0.55, tip]
        k.add(f"collar{i}", hull(pts), colour, bone, fuse=False, surface="shell")


# --- the frame -----------------------------------------------------------------------------------------------------
BUB_C, BUB_R = np.array([0.0, -0.004, 0.16]), 0.078
WINDOW = unit(0, -1, 0.26)                       # the window's axis: forward and a little up
HD_C, HD_R = np.array([0.0, -0.012, 0.156]), np.array([0.064, 0.056, 0.054])
AB_C, AB_R = np.array([0.0, 0.006, 0.05]), np.array([0.03, 0.031, 0.036])
COLLAR_C = np.array([0.0, 0.0, 0.08])
DOWN = [  # bone, hip, knee, ankle, tip (on +X)
    ("leg{s}.001", (0.016, -0.004, 0.086), (0.066, -0.026, 0.104), (0.088, -0.036, 0.07), (0.092, -0.04, 0.003)),
    ("hind{s}", (0.016, 0.014, 0.086), (0.062, 0.042, 0.104), (0.08, 0.058, 0.07), (0.083, 0.064, 0.003)),
]


def abdomen(k, c=AB_C, r=AB_R, colour=SLATE, tip=SLATE_TIP):
    def drop(V):
        V = V.copy()
        low = np.clip(-V[:, 2], 0, 1)
        V[:, :2] *= (1 - 0.32 * low ** 1.6)[:, None]
        return V
    k.add("abdomen", ell(c, r, deform=drop), colour, "body")
    k.add("abdtip", ell(c + np.array([0, 0.002, -r[2] * 0.95]), r * np.array([0.3, 0.3, 0.3])), tip, "body")
    k.paint(lambda P: inside(c[2] - r[2] * 0.72 - P[:, 2], 0.0015) * inside(np.abs(P[:, 0]) - r[0]), tip,
            on=["abdomen", "abdtip"])


def dewpider(src, out):
    k = Kit(src, coat="shell")
    zc = head_in_bubble(k, HD_C, HD_R)
    nose(k, HD_C, HD_R, zc)
    abdomen(k)
    shell(k, BUB_C, BUB_R, WINDOW, 58, 0.0045, WATER)
    glints(k, BUB_C, BUB_R)
    collar(k, COLLAR_C, 0.056, 6, rise=0.3, thick=0.014)
    # the front pair climbing the bubble's sides and hooking in over it
    arch_leg(k, "arm", "leg{s}", BUB_C, BUB_R, 60, (-62, -40, -12, 14, 38, 54),
             (0.0, 0.006, 0.008, 0.008, 0.006, 0.004), [0.0072, 0.0078, 0.0095, 0.0085, 0.0068, 0.0034], curl=26)
    for bone, hip, knee, ankle, tip in DOWN:
        leg(k, "mid" if "001" in bone else "hind", bone, [hip, knee, ankle, tip], [0.0074, 0.0084, 0.0076, 0.0034])
    k.build(voxel=0.0024, smooth=4, band=0.002, wband=0.02)

    def lay(state):
        eyes_on(k, state, HD_C, HD_R, zc, (0.0165, 0.0125))
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    dewpider(*args())
