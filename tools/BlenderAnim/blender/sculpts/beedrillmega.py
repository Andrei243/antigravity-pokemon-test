# Beedrill-Mega sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/beedrillmega.py -- <the export's Beedrill-Mega.glb> <out.glb>
#
# Beedrill's way copied by hand (beedrill.py builds its Kit as it is read): the same yellow, black bands as loose
# rings, round red eyes with a pink glint and a dark rim, white stingers as lathes, pale wings with grey veins as
# slabs. From the two pictures, what the Mega changes, largest mass first: the forearms become two huge white lances,
# thick at the root and longer than the abdomen; the hind legs become yellow limbs banded black ending in long white
# stingers, so four great stingers in all, carried forward and down; the abdomen long and tapering, three black bands
# round it, a pale cream stinger at its tip; a bigger round head with larger red eyes; two pairs of long dark purple
# antennae; a yellow thorax black behind; two pairs of long narrow wings raised high in a V. The skeleton hovers it
# (every joint 0.179 over Beedrill's).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, lathe, slab, ring_band, outward, srgb, inside, unit, eye_look, args

YELLOW, BLACK, LIMB, STING, TAIL = "#F6C42E", "#24222E", "#4C4878", "#E2E6EE", "#F2E0A2"
WING, VEIN, RED, PINK, RIM = "#E8ECF2", "#A2AABC", "#E0303E", "#FF9CAA", "#5A141E"

H, HR = np.array([0.0, -0.05, 0.82]), np.array([0.104, 0.1, 0.098])          # the head, larger than Beedrill's
TH_C, TH_R = np.array([0.0, 0.0, 0.69]), (0.075, 0.078, 0.07)              # the thorax
AB_TOP = np.array([0.0, 0.035, 0.63])
AB_AX = unit(0, -0.22, -1)                                                    # the abdomen hangs down, a little forward
AB_LEN, AB_R = 0.4, 0.124


def ab_radius(s):
    """The abdomen's radius at a distance s along its axis from its top: swelling, then tapering to its tip."""
    t = np.clip(s / AB_LEN, 0, 1)
    return AB_R * np.clip(np.sin(np.pi * np.clip(0.12 + 0.88 * t, 0, 1) ** 0.75), 0.05, 1) ** 0.8 * (1 - 0.3 * t)


def body(k):
    k.add("head", ell(H, HR), YELLOW, "head")
    k.add("neck", tube([(0, -0.01, 0.71), (0, -0.03, 0.76)], [0.04, 0.04]), YELLOW, "head")
    k.add("thorax", ell(TH_C, TH_R), YELLOW, "body")
    # black behind the thorax's middle: a collar across its back
    k.paint(lambda P: inside(TH_C[1] - 0.01 - P[:, 1], 0.004) * inside(P[:, 2] - 0.745, 0.004)
            * inside(0.64 - P[:, 2], 0.004), BLACK, on=["thorax"])
    s = np.linspace(0, AB_LEN, 12)
    pts = [AB_TOP + AB_AX * x for x in s]
    k.add("abdomen", tube(pts, [float(ab_radius(x)) for x in s]), YELLOW, "body")


def bands(k):
    """Three black bands round the abdomen, loose rings so their edges stay crisp."""
    for i, (s0, w) in enumerate(((0.12, 0.05), (0.2, 0.048), (0.28, 0.042))):
        k.add(f"band{i}", ring_band(AB_TOP + AB_AX * s0, AB_AX,
                                    lambda v, s0=s0, w=w: ab_radius(s0 + (v - 0.5) * w) + 0.003, w, thick=0.007),
              BLACK, "body", fuse=False)


def tail_stinger(k):
    base = AB_TOP + AB_AX * (AB_LEN - 0.07)
    r0 = float(ab_radius(AB_LEN - 0.07))
    k.add("tailsting", lathe(base, AB_AX, [(0, 0), (r0 + 0.004, 0.0), (r0 * 0.85, 0.07), (r0 * 0.4, 0.15), (0.0, 0.22)]),
          TAIL, "body", fuse=False, surface="shell")


SHOULDER = np.array([0.06, -0.02, 0.7])
ELBOW = np.array([0.125, -0.06, 0.66])
LANCE_D = unit(0.55, -0.5, -0.67)                                         # forward, out and down


def lances(k):
    """The forearms: a short dark upper arm, then a huge white lance, thick at its root."""
    k.add("upper", tube([SHOULDER, (SHOULDER + ELBOW) / 2 + (0, 0, 0.006), ELBOW], [0.02, 0.018, 0.018]), LIMB,
          "arm{s}", fuse=False, mirror=True)
    k.add("lance", lathe(ELBOW - LANCE_D * 0.02, LANCE_D,
                         [(0, -0.03), (0.04, -0.028), (0.066, 0.0), (0.07, 0.04), (0.06, 0.13), (0.042, 0.25),
                          (0.022, 0.36), (0.0, 0.45)]), STING, "arm{s}", fuse=False, mirror=True, surface="shell")


HIP = np.array([0.075, 0.05, 0.6])
KNEE = np.array([0.14, 0.0, 0.44])
LEG_D = unit(0.12, -0.25, -1)


def legs(k):
    """The hind legs: thick yellow limbs banded black, each ending in a long white stinger."""
    k.add("thigh", tube([HIP, (HIP + KNEE) / 2 + (0.01, 0, 0), KNEE], [0.04, 0.045, 0.04]), YELLOW, "hind{s}",
          fuse=False, mirror=True)
    ax = unit(*(KNEE - HIP)); L = np.linalg.norm(KNEE - HIP)
    for i, t in enumerate((0.45, 0.78)):
        k.add(f"legband{i}", ring_band(HIP + ax * L * t, ax, 0.045 if i == 0 else 0.043, 0.032, thick=0.007), BLACK,
              "hind{s}", fuse=False, mirror=True)
    k.add("legsting", lathe(KNEE - LEG_D * 0.01, LEG_D, [(0, -0.02), (0.044, -0.01), (0.048, 0.03), (0.036, 0.12),
                                                           (0.018, 0.22), (0.0, 0.3)]), STING, "hind{s}", fuse=False,
          mirror=True, surface="shell")
    # small dark front legs tucked under the thorax
    k.add("leg", tube([(0.04, -0.03, 0.66), (0.07, -0.06, 0.62), (0.07, -0.07, 0.59)], [0.016, 0.015, 0.012]), LIMB,
          "leg{s}", fuse=False, mirror=True)


def antennae(k):
    """Two pairs of long dark purple antennae, curving up and out."""
    for name, pts in (("antA", [(0.025, -0.1, 0.9), (0.04, -0.13, 0.96), (0.06, -0.14, 1.0), (0.1, -0.12, 1.03)]),
                      ("antB", [(0.055, -0.06, 0.9), (0.09, -0.07, 0.95), (0.13, -0.06, 0.99), (0.17, -0.02, 1.0)])):
        k.add(name, tube(pts, [0.017, 0.016, 0.015, 0.013]), LIMB, "ear{s}", fuse=False, mirror=True)


def wing_outline(L, W, n=26):
    t = np.linspace(0, 1, n)
    w = W * np.sqrt(np.clip(1 - ((t - 0.6) / 0.6) ** 2, 0, 1)) * (0.4 + 0.6 * t)
    a = 0.004 + t * L
    return np.vstack([np.stack([a, np.maximum(w, 0.003)], 1), np.stack([a, -np.maximum(w, 0.003)], 1)[::-1]])


def wing_axes(sweep, lift, twist):
    sw, li = math.radians(sweep), math.radians(lift)
    ex = np.array([math.cos(li) * math.cos(sw), -math.cos(li) * math.sin(sw), math.sin(li)])
    ez = np.array([0.0, 1.0, math.tan(math.radians(twist))])
    ez = ez - ex * np.dot(ez, ex)
    return ex, ez / np.linalg.norm(ez)


def wing(k, name, root, L, W, sweep, lift, twist):
    ex, ez = wing_axes(sweep, lift, twist)
    o = np.array(root, float)
    k.add(name, slab(wing_outline(L, W), o, ex, ez, 0.012), WING, "wing{s}", fuse=False, mirror=True)
    veins = [[(0.03 * L, -0.01 * W), (0.92 * L, 0.04 * W), (0.92 * L, 0.16 * W), (0.03 * L, 0.09 * W)]]
    for t in (0.38, 0.62, 0.84):
        w = W * math.sqrt(max(0.0, 1 - ((t - 0.6) / 0.6) ** 2)) * (0.4 + 0.6 * t) * 0.92
        veins.append([(t * L, -w), (t * L + 0.01, -w), (t * L + 0.01, w), (t * L, w)])
    for i, vv in enumerate(veins):
        k.add(f"{name}vein{i}", slab(vv, o, ex, ez, 0.016), VEIN, "wing{s}", fuse=False, mirror=True)


def wings(k):
    wing(k, "wingU", (0.04, 0.05, 0.72), 0.34, 0.085, -14, 52, 45)
    wing(k, "wingD", (0.04, 0.08, 0.69), 0.27, 0.07, -46, 34, -25)


CR, CP, CRIM, CW = srgb(RED), srgb(PINK), srgb(RIM), np.array([1.0, 1.0, 1.0])
def eye_open(u, v):
    c = np.tile(CR, (len(u), 1))
    c[((u + 0.2) / 0.42) ** 2 + ((v - 0.26) / 0.32) ** 2 < 1] = CP
    c[((u + 0.28) / 0.16) ** 2 + ((v - 0.34) / 0.14) ** 2 < 1] = CW
    c[u * u + v * v > 0.7] = CRIM
    return c


def eye_frame(side, d=(0.62, -0.74, 0.18)):
    de = unit(side * d[0], d[1], d[2])
    p = H + de / math.sqrt(((de / HR) ** 2).sum())
    n = (p - H) / HR ** 2
    return p, n / np.linalg.norm(n)


EYE_HALF = (0.046, 0.056)
def eyes(k):
    def lay(state):
        look = eye_look(state, eye_open, CR, CRIM, line=0.17)
        for side in (1, -1):
            c, n = eye_frame(side)
            col = look if side == 1 else (lambda a, b, f=look: f(-a, b))
            k.decal(c, n, EYE_HALF, col, up=(side * 0.35, 0, 1), eps=0.003, rings=20, segs=56)
    return lay


def beedrillmega(src, out):
    k = Kit(src, coat="fur")
    body(k)
    bands(k)
    tail_stinger(k)
    lances(k)
    legs(k)
    antennae(k)
    wings(k)
    k.build(voxel=0.0058, smooth=4)
    k.eye_states(eyes(k))
    k.export(out)


if __name__ == "__main__":
    beedrillmega(*args())
