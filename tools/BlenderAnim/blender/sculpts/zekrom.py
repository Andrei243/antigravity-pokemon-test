# Zekrom sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/zekrom.py -- <the export's Zekrom.glb> <out.glb>
#
# From the references: a black armoured dragon standing upright, broad and heavy; dark grey hide in plates edged in
# black, black ovals on the shoulders, the chest's top, the thighs and the knees, thin glowing blue lines down the
# chest's middle, the arms and the legs; a blocky head with a short muzzle and a pointed chin, small red eyes under a
# heavy brow, a great crest rising off the crown and swept back over the head in a wave, ending in a glowing blue point;
# angular wings raised up and back, each a hand of three long dark fingers with paler plates between; thick arms
# hanging forward to great fists, the fingertips blue; huge thighs, thick shins, broad three-toed feet with blue tips;
# and the generator: a great round drum on a short thick tail behind the hips, its back a dark face of rings, black
# spikes up off its top.
# The export's joints: body (0, 0, 0.42), head (0, -0.04, 0.6), arms (+-0.11, -0.03, 0.56), wings (+-0.06, 0.04, 0.56),
# tail (0, 0.06, 0.32), legs (+-0.08, 0, 0.3).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, boxy, ell, tube, blade, hull, slab, lathe, srgb, inside, unit, eye_look, polygon, args

GREY, BLACK, PALE, BLUE = "#4A4C55", "#1E1F25", "#6A6D78", "#46C2F2"
FACE, RING, EYER, INKC = "#141519", "#33353D", "#E2263A", "#0E0E12"

H = np.array([0.0, -0.09, 0.79])           # the skull's middle
HR = np.array([0.074, 0.078, 0.072])
NECK = np.array([(0, -0.02, 0.58), (0, -0.05, 0.67), (0, -0.075, 0.75)])
J, HS = np.array([0.0, -0.06, 0.72]), 1.33   # the head grown about its root, a third larger
HG, HRG = J + (H - J) * HS, HR * HS


def grown(p):
    return J + (np.array(p, float) - J) * HS


def on_ell(c, r, d):
    d = unit(*d)
    t = 1.0 / math.sqrt(((d / np.array(r)) ** 2).sum())
    return np.array(c, float) + d * t


def spike(k, name, base, d, length, width, thick, bone, color=BLACK, up=None, sink=0.02, mirror=False, surface="scales"):
    """A solid pointed piece: a kite in section, wide across up and thick along it."""
    base, d = np.array(base, float), unit(*d)
    if up is None:
        up = np.cross(d, (0, 0, 1.0)) if abs(d[2]) < 0.9 else np.array([1.0, 0, 0])
    up = np.array(up, float); up = up - d * (up @ d); up /= np.linalg.norm(up)
    side = np.cross(d, up)
    b = base - d * sink
    pts = [b + up * width / 2, b - up * width / 2, b + side * thick / 2, b - side * thick / 2,
           base + d * length * 0.45 + up * width * 0.32, base + d * length * 0.45 - up * width * 0.32,
           base + d * length * 0.45 + side * thick * 0.32, base + d * length * 0.45 - side * thick * 0.32,
           base + d * length]
    k.add(name, hull(pts), color, bone, fuse=False, mirror=mirror, surface=surface)


def oval(c, r):
    """A paint mask: inside an ellipsoid."""
    c, r = np.array(c, float), np.array(r, float)
    return lambda P: inside(np.linalg.norm((P - c) / r, axis=1) - 1, 0.012)


def body(k):
    # an inverted triangle: the chest and shoulders broad, the waist pinched over the heavy thighs
    k.add("torso", ell((0, 0.0, 0.43), (0.1, 0.1, 0.13)), GREY, "body")
    k.add("chest", ell((0, -0.03, 0.55), (0.185, 0.115, 0.105), deform=boxy(2.4)), GREY, "body")
    k.add("hips", ell((0, 0.02, 0.32), (0.115, 0.105, 0.09)), GREY, "body")
    k.add("neck", tube(NECK, [0.085, 0.07, 0.06]), GREY, "head")
    # black ovals: the chest's top, the shoulders
    k.paint(oval((0, -0.12, 0.6), (0.045, 0.05, 0.04)), BLACK, on=["chest", "neck", "torso"])
    for s in (1, -1):
        k.paint(oval((0.18 * s, -0.04, 0.6), (0.07, 0.08, 0.055)), BLACK, on=["chest", "shoulder", "torso"])
    # the belly's plates: a paler middle edged by the hide
    k.paint(lambda P: inside(np.abs(P[:, 0]) - 0.05, 0.004) * inside(P[:, 1] + 0.06, 0.004)
            * inside(P[:, 2] - 0.52, 0.004) * inside(0.3 - P[:, 2], 0.004), "#5E616C", on=["torso", "chest", "hips"])


def blue_line(k, pts, nrms, w=0.012, mirror=True):
    """A thin glowing blue line of patches along points."""
    RECT = polygon([(-1, -1), (1, -1), (1, 1), (-1, 1)])
    pts = np.array(pts, float)
    for i in range(len(pts) - 1):
        a, b = pts[i], pts[i + 1]
        n = unit(*(np.array(nrms[i], float) + np.array(nrms[i + 1], float)))
        d = b - a
        L = np.linalg.norm(d)
        k.decal((a + b) / 2, n, (w / 2, L / 2 + w * 0.3), BLUE, up=d, eps=0.004, outline=RECT, rings=3, segs=32,
                mirror=mirror)


def legs(k):
    for s, sd in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        bone = f"leg{sd}"
        k.add(f"thigh{sd}", ell(np.array((0.115, 0.02, 0.27)) * m, (0.1, 0.12, 0.135)), GREY, bone)
        k.add(f"shin{sd}", tube(np.array([(0.13, 0.03, 0.19), (0.14, 0.01, 0.11), (0.14, 0.0, 0.06)]) * m,
                                [0.072, 0.062, 0.056]), GREY, bone)
        k.add(f"foot{sd}", ell(np.array((0.14, -0.035, 0.032)) * m, (0.065, 0.09, 0.034)), GREY, bone)
        for j, dx in enumerate((-0.036, 0.0, 0.036)):
            c = np.array((0.14 + dx, -0.11, 0.028)) * m
            k.add(f"toe{sd}{j}", ell(c, (0.022, 0.03, 0.024)), GREY, bone)
            spike(k, f"toetip{sd}{j}", c + np.array((0, -0.02, -0.004)), np.array((dx * 0.8, -1, -0.3)) * m, 0.03,
                  0.03, 0.022, bone, color=BLUE, up=(0, 0, 1), sink=0.015)
        # black ovals on the thigh's front and the knee
        k.paint(oval(np.array((0.13, -0.08, 0.29)) * m, (0.05, 0.05, 0.06)), BLACK, on=["thigh"])
        k.paint(oval(np.array((0.14, -0.05, 0.14)) * m, (0.035, 0.04, 0.035)), BLACK, on=["shin", "thigh"])


def arms(k):
    for s, sd in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        bone = f"arm{sd}"
        k.add(f"shoulder{sd}", ell(np.array((0.18, -0.035, 0.575)) * m, (0.08, 0.08, 0.075)), GREY, bone)
        # carried forward and down, the forearm thick
        k.add(f"arm{sd}", tube(np.array([(0.17, -0.04, 0.55), (0.21, -0.11, 0.47), (0.2, -0.22, 0.4)]) * m,
                               [0.058, 0.062, 0.072]), GREY, bone)
        k.add(f"fist{sd}", ell(np.array((0.2, -0.265, 0.385)) * m, (0.072, 0.068, 0.068)), GREY, bone)
        for j, dz in enumerate((0.03, 0.0, -0.03)):
            c = np.array((0.2 + 0.02, -0.325, 0.385 + dz * 1.2)) * m
            spike(k, f"finger{sd}{j}", c, np.array((0.3, -1, -0.15)) * m, 0.035, 0.028, 0.024, bone, color=BLUE,
                  up=(0, 0, 1), sink=0.015)
        k.paint(oval(np.array((0.215, -0.11, 0.47)) * m, (0.045, 0.045, 0.045)), BLACK, on=["arm"])


# --- the wings: a hand of three long fingers raised up and back, paler plates between -------------------------------
ROOT = np.array([0.08, 0.07, 0.6])
NW = unit(0.35, -0.85, 0.25)          # the wing's face: to the front and a little out
WS = 1.5                              # half as large again as first built


def wings(k):
    n = NW
    ex = np.array([0.48, 0.35, 0.8]); ex = ex - n * (ex @ n); ex /= np.linalg.norm(ex)   # out and up: a V
    ez = np.cross(n, ex)                                                                # up and in
    k.add("wingarm", tube([ROOT - ex * 0.02, ROOT + ex * 0.16 * WS], [0.06, 0.05]), GREY, "wing{s}", mirror=True)
    # the membrane: a pale plate in the angle of the fingers
    mem = [(a * WS, b * WS) for a, b in [(0.0, -0.03), (0.38, -0.1), (0.36, 0.06), (0.28, 0.16), (0.14, 0.22),
                                         (0.04, 0.12)]]
    k.add("membrane", slab(mem, ROOT, ex, ez, 0.026), PALE, "wing{s}", fuse=False, mirror=True, surface="scales")
    # the fingers: long angular plates, dark, a little thicker, fanned from the wrist
    FING = [((0.14, 0.0), -8, 0.27, 0.075), ((0.13, 0.03), 28, 0.26, 0.07), ((0.1, 0.05), 62, 0.22, 0.065)]
    for j, ((a0, b0), ang, L, w) in enumerate(FING):
        a = math.radians(ang)
        d = math.cos(a) * ex + math.sin(a) * ez
        wd = -math.sin(a) * ex + math.cos(a) * ez
        a0, b0, L, w = a0 * WS, b0 * WS, L * WS, w * WS
        o = ROOT + ex * a0 + ez * b0 + n * 0.004
        out = [(-0.12 * WS, -w * 0.5), (L * 0.6, -w * 0.5), (L, -w * 0.1), (L * 0.85, w * 0.5), (-0.12 * WS, w * 0.5)]
        k.add(f"wfinger{j}", slab(out, o, d, wd, 0.034), BLACK if j % 2 else GREY, "wing{s}", fuse=False,
              mirror=True)


def generator(k):
    k.add("tailroot", tube([(0, 0.05, 0.35), (0, 0.13, 0.33), (0, 0.2, 0.32)], [0.1, 0.09, 0.085]), GREY, "tail")
    c0, d = np.array([0, 0.17, 0.34]), unit(0, 1, 0.08)
    L = 0.2
    k.add("drum", lathe(c0, d, [(0, 0.0), (0.1, 0.0), (0.15, 0.04), (0.17, 0.1), (0.167, 0.16), (0.15, L),
                               (0, L)], seg=56), GREY, "tail", fuse=False, surface="scales")
    k.add("face", lathe(c0, d, [(0, L - 0.01), (0.14, L - 0.01), (0.134, L + 0.006), (0, L + 0.006)], seg=56),
          FACE, "tail", fuse=False, surface="metal")
    for j, (r0, r1) in enumerate(((0.085, 0.104), (0.035, 0.053))):
        k.add(f"facering{j}", lathe(c0, d, [(r0, L + 0.004), (r1, L + 0.004), (r1, L + 0.012), (r0, L + 0.012)],
                                    seg=48), RING, "tail", fuse=False, surface="metal")
    # a black band round the drum's widest part, and black spikes up off its top and back
    k.add("drumband", lathe(c0, d, [(0.168, 0.085), (0.176, 0.09), (0.176, 0.13), (0.168, 0.135),
                                    (0.162, 0.13), (0.162, 0.09)], seg=56), BLACK, "tail", fuse=False)
    for j, (x, t, L2) in enumerate(((0.0, 0.17, 0.13), (0.05, 0.12, 0.1), (-0.05, 0.12, 0.1))):
        base = c0 + d * t + np.array((x, 0, 0.15))
        spike(k, f"tailspike{j}", base, (x * 3, 0.7, 0.75), L2, 0.06, 0.03, "tail", up=(0, -0.7, 0.7), sink=0.03)


def head(k):
    k.add("skull", ell(H, HR, deform=None), GREY, "head")
    k.add("muzzle", ell(H + (0, -0.08, -0.024), (0.05, 0.078, 0.046), deform=boxy(3.2)), GREY, "head")
    k.add("chin", ell(H + (0, -0.075, -0.062), (0.042, 0.06, 0.03), rot=(-15, 0, 0)), PALE, "head")
    k.add("brow", ell(H + (0, -0.035, 0.035), (0.056, 0.036, 0.022), rot=(-12, 0, 0)), BLACK, "head")

    # the crest: a thick blade up off the crown, swept back over the head in a wave, a glowing blue point at its end
    C = [H + (0, -0.02, 0.05), H + (0, 0.02, 0.11), H + (0, 0.09, 0.13), H + (0.03, 0.16, 0.1),
         H + (0.06, 0.22, 0.1), H + (0.09, 0.28, 0.12)]
    k.add("crest", blade(C, [0.045, 0.045, 0.04, 0.035, 0.03, 0.024], (1, 0, 0), 0.6), GREY, "head")
    tipb = np.array(C[-1])
    spike(k, "cresttip", tipb, (0.4, 0.8, 0.1), 0.075, 0.045, 0.032, "head", color=BLUE, up=(0, 0, 1), sink=0.03,
          surface="metal")
    # a short black horn back from each side of the head
    spike(k, "horn", H + (0.05, 0.02, 0.01), (0.55, 0.75, 0.2), 0.08, 0.04, 0.026, "head", mirror=True)


def lines(k):
    # down the chest's middle, along each arm and thigh
    blue_line(k, [(0, -0.115, 0.56), (0, -0.12, 0.49), (0, -0.112, 0.42), (0, -0.1, 0.35)], [(0, -1, 0)] * 4,
              mirror=False)
    blue_line(k, [(0.24, -0.14, 0.46), (0.245, -0.21, 0.42)], [unit(1, -0.2, 0.2)] * 2)
    blue_line(k, [(0.2, -0.05, 0.36), (0.205, -0.07, 0.27), (0.195, -0.06, 0.2)], [unit(1, -0.25, 0)] * 3)


OUT = [(-1, -0.25), (-0.4, -0.7), (0.5, -0.65), (1.1, -0.1), (0.9, 0.35), (0.2, 0.5), (-0.6, 0.25)]
EYE_OUT = polygon(OUT)
CR, CK, CW, LID = srgb(EYER), srgb(INKC), srgb("#FFFFFF"), srgb(BLACK)


def eye_open(u, v):
    c = np.tile(CR, (len(u), 1))
    c[((u + 0.05) / 0.25) ** 2 + ((v + 0.05) / 0.38) ** 2 < 1] = CK
    c[((u + 0.3) / 0.15) ** 2 + ((v - 0.15) / 0.15) ** 2 < 1] = CW
    return c


DE = unit(0.6, -0.7, 0.25)


def eyes(k, state):
    E = on_ell(HG, HRG, DE) + np.array([0, -0.008, -0.02])
    look = eye_look(state, eye_open, LID, srgb(PALE), line=0.2)
    m = np.array([-1, 1, 1.0])
    k.decal(E, DE, (0.04, 0.027), look, eps=0.005, rings=20, segs=56, outline=EYE_OUT)
    k.decal(E * m, DE * m, (0.04, 0.027), lambda u, v: look(-u, v), eps=0.005, rings=20, segs=56,
            outline=lambda th: EYE_OUT(math.pi - th))


def zekrom(src, out):
    k = Kit(src, coat="scales")
    body(k)
    legs(k)
    arms(k)
    wings(k)
    generator(k)
    n0 = len(k.parts)
    head(k)
    for p in k.parts[n0:]:
        p["V"] = grown(p["V"])
    # the pale jaw under a pale line along its edge, so the face reads
    k.paint(lambda P: inside(P[:, 2] - (HG[2] - 0.05 * HS), 0.003) * inside(P[:, 1] - (HG[1] - 0.03 * HS), 0.003),
            PALE, on=["muzzle", "chin"])
    k.decal(grown(H + (0.045, -0.09, -0.044)), unit(1, -0.45, -0.15), (0.06, 0.007), "#A6AAB6", up=(0, 0, 1),
            eps=0.004, outline=polygon([(-1, -1), (1, -1), (1, 1), (-1, 1)]), rings=3, segs=32, mirror=True)
    lines(k)
    k.build(voxel=0.0075, smooth=4, band=0.006)
    k.eye_states(lambda state: eyes(k, state))
    k.export(out)


if __name__ == "__main__":
    zekrom(*args())
