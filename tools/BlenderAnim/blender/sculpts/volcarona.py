# Volcarona sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/volcarona.py -- <the export's Volcarona.glb> <out.glb>
#
# Grown from larvesta.py (sibling("larvesta")): its white fluff with the same jagged tufts, its red, its blue eye with
# the dark pupil and glint, its black face gone to a small black head.
#
# From the references: a great moth. A white fluffy thorax, its lower edge a jagged grey fringe; six short black stubs
# of legs on its front, three a side; a small black head on its top front, pointed down in front, with blue eyes low
# on its sides; two red horns rising from beside the eyes and curving up and in round the head like brackets. Below,
# a pale blue-green abdomen in segments curving down and forward, a black plate down its front ending in a black
# point. Six wings fanned round the body like a sun's rays, three a side: the upper pair up and out, red with orange
# toward the root; the middle pair straight out, orange with a red tip; the lower pair down and out, orange going red;
# each a broad flame-shaped plate with a curling point and black spots. The wings stand in a plane behind the thorax
# facing the cameras, swept a little back as a mirror pair, so both of the game's cameras see their faces.
# Bones: root, body (0, 0, 0.323), wing{s} (0.03, 0.05, 0.343), head (0, -0.03, 0.383).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, outward, srgb, inside, unit, eye_look, args, sibling

lv = sibling("larvesta")

WHITE, FRINGE, HEAD, HORN = lv.WHITE, "#BDBCC4", "#2C2A32", lv.HORN
ORANGE, RED, SPOT = "#F2782C", "#D63230", "#2A2228"
BELLY, SEG, PLATE, STUB = "#B4DDE2", "#6E9EA8", "#2E2C34", "#26242C"

T = np.array([0.0, -0.01, 0.33])             # the thorax's middle
TR = np.array([0.1, 0.078, 0.086])
H = np.array([0.0, -0.058, 0.405])           # the head's middle
HR = np.array([0.046, 0.041, 0.046])


def thorax(k):
    k.add("thorax", ell(T, TR, deform=lv.fluff(teeth=10, rows=2.6, amp=0.22), sub=5), WHITE, "body")
    k.add("fringe", ell(T + (0, 0.008, -0.07), (0.084, 0.07, 0.036), deform=lv.fluff(teeth=12, rows=1.6, amp=0.45,
                                                                                   keep_front=False)), FRINGE, "body")
    # six black stubs of legs on the front, three a side
    for i, (x, z, a) in enumerate(((0.032, 0.36, 0.012), (0.058, 0.33, 0.013), (0.04, 0.297, 0.012))):
        y = T[1] - TR[1] * math.sqrt(max(0.0, 1 - (x / TR[0]) ** 2 - ((z - T[2]) / TR[2]) ** 2)) - 0.002
        k.add(f"stub{i}", ell((x, y, z), (a, 0.012, 0.009), rot=(0, -20, 0)), STUB, "body", fuse=False,
              mirror=True, surface="shell")


ABDOMEN = [((0, 0.016, 0.258), (0.062, 0.06, 0.05)), ((0, -0.004, 0.208), (0.055, 0.054, 0.046)),
           ((0, -0.026, 0.163), (0.045, 0.046, 0.04)), ((0, -0.047, 0.123), (0.033, 0.035, 0.032))]


def abdomen(k):
    for i, (c, r) in enumerate(ABDOMEN):
        k.add(f"abdomen{i}", ell(c, r), BELLY, "body")
    k.add("tip", ell((0, -0.066, 0.088), (0.017, 0.02, 0.03), rot=(-25, 0, 0)), PLATE, "body")
    zs = np.array([c[2] for c, _ in ABDOMEN])[::-1]
    ys = np.array([c[1] for c, _ in ABDOMEN])[::-1]

    def plate(P):
        yc = np.interp(P[:, 2], zs, ys)
        w = 0.007 + 0.03 * np.clip((P[:, 2] - 0.09) / 0.17, 0, 1)
        return inside(np.abs(P[:, 0]) - w, 0.0015) * inside(P[:, 1] - (yc - 0.012), 0.002) * inside(P[:, 2] - 0.285, 0.002)

    def segs(P):
        m = np.zeros(len(P))
        for z in (0.232, 0.186, 0.143):
            m = np.maximum(m, inside(np.abs(P[:, 2] - z) - 0.0024, 0.0015))
        return m * inside(P[:, 2] - 0.27, 0.002)
    k.paint(segs, SEG, on=["abdomen"])
    k.paint(plate, PLATE, on=["abdomen", "tip"])


def head(k):
    k.add("head", ell(H, HR), HEAD, "head")
    k.add("snout", ell(H + (0, -0.024, -0.03), (0.014, 0.013, 0.022), rot=(-15, 0, 0)), HEAD, "head")


def horns(k):
    pts = np.array([(0.026, -0.06, 0.41), (0.05, -0.058, 0.44), (0.058, -0.046, 0.48), (0.046, -0.034, 0.515),
                    (0.022, -0.028, 0.532)])
    k.add("horn", outward(tube(pts, [0.012, 0.012, 0.011, 0.0085, 0.005])), HORN, "head", fuse=False, mirror=True,
          surface="shell")


# --- the wings: broad flame-shaped plates, an orange base and a red panel thicker than it, black spots through both
def petal(L, W, curl, t0=0.0, t1=0.997, n=28):
    """The outline (along, across) of a flame-shaped wing, from t0 to t1 of its length."""
    t = np.linspace(t0, t1, n)
    w = W * np.sin(np.pi * np.clip(t, 0.0, 1.0)) ** 0.7 * (1 - 0.2 * t) + 0.004 * (t < 0.02)
    off = curl * L * t ** 2.2
    upper = np.stack([t * L, off + w], axis=1)
    lower = np.stack([t * L, off - w], axis=1)[::-1]
    return np.vstack([upper, lower]), (lambda tt, s: (tt * L, curl * L * tt ** 2.2
                                                        + s * W * math.sin(math.pi * tt) ** 0.7 * (1 - 0.2 * tt)))


# angle out from straight out (degrees, up positive), length, half width, curl, where the red begins, spots (t, s)
WINGS = [("upper", 56, 0.315, 0.062, -0.14, 0.4, [(0.5, 0.3), (0.66, -0.35), (0.8, 0.15)], 0.352),
         ("middle", 6, 0.3, 0.058, -0.1, 0.68, [(0.46, 0.25), (0.62, -0.3), (0.8, 0.1)], 0.338),
         ("lower", -50, 0.29, 0.056, 0.12, 0.5, [(0.5, 0.2), (0.68, -0.3), (0.83, 0.1)], 0.322)]


def wings(k):
    for name, ang, L, W, curl, t0, spots, z in WINGS:
        a = math.radians(ang)
        d = unit(math.cos(a), 0.3, math.sin(a))
        across = unit(-math.sin(a), 0, math.cos(a))
        origin = (0.03, 0.055, z)
        base, at = petal(L, W, curl)
        k.add(name, slab(base, origin, d, across, 0.009), ORANGE, "wing{s}", fuse=False, mirror=True, surface="leaf")
        tip, _ = petal(L, W * 1.0, curl, t0, 0.999)
        k.add(name + "red", slab(tip, origin, d, across, 0.013), RED, "wing{s}", fuse=False, mirror=True,
              surface="leaf")
        for i, (t, s) in enumerate(spots):
            ca, cb = at(t, s)
            r = 0.0085 + 0.003 * (i % 2)
            th = np.linspace(0, 2 * math.pi, 24, endpoint=False)
            oval = np.stack([ca + r * 1.3 * np.cos(th), cb + r * np.sin(th)], axis=1)
            k.add(f"{name}spot{i}", slab(oval, origin, d, across, 0.017), SPOT, "wing{s}", fuse=False, mirror=True,
                  surface="leaf")


# --- the eyes: Larvesta's blue eye, low on the black head's sides
def eye_frame(side, d=(0.56, -0.8, -0.12)):
    de = unit(side * d[0], d[1], d[2])
    p = H + de / math.sqrt(((de / HR) ** 2).sum())
    n = (p - H) / HR ** 2
    return p, n / np.linalg.norm(n)


def eye_cut(th):
    return 1.0


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="fur")
    thorax(k)
    abdomen(k)
    head(k)
    horns(k)
    wings(k)
    k.build(voxel=0.0055, smooth=3, band=0.006)
    k.eye_states(lv.eyes(k, frame=eye_frame, half=(0.014, 0.016), cut=eye_cut, lid=srgb(HEAD)))
    k.export(out)
