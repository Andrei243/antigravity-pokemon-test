# Rotom-Fan sculpted from nothing on the skeleton and clips of the game's own export of the form (sculptkit.py):
#   blender --background --factory-startup --python sculpts/rotomfan.py -- <the export's Rotom-Fan.glb> <out.glb>
#
# The design, from the references: Rotom in an electric fan. A large orange ring (the fan's guard), its face to the
# front, filled with yellow plasma, three dark rounded blades on an orange cone of a hub inside it; under it a short
# neck down to a small round orange body with a little knob beneath, which carries Rotom's eyes (a white ring round a
# yellow iris) and a small smile. A yellow aura rims the ring, the neck and the body, and two arms of plasma sweep out
# of the ring's sides and curl into spirals at their ends, one up to its left (-X, armL), one down to its right (+X,
# armR). Grown from rotom.py: the aura is opaque yellow a little BEHIND the orange (a ring of plasma round the core
# from the front), the core shows through it from behind as a loose mirrored copy, the eyes are the same patch.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, unit, eye_look, args, slab, lathe, outward

src, out = args()
k = Kit(src, coat="shell")

ORANGE, AURA, CURL, BLADE = "#F27028", "#F6D24E", "#E8B42A", "#2C2A30"
WHITE, IRIS, PUPIL, INK, MOUTH = "#FFFFFF", "#F8C21C", "#9A4A0C", "#3A1E10", "#6A2614"
C = np.array([0.0, 0.0, 0.215])            # the body's middle, on the body bone
R = np.array([0.098, 0.09, 0.09])
BACK = 0.024                               # how far the aura stands behind the orange
RC = np.array([0.0, 0.01, 0.5])            # the ring's middle (armL's joint is on its left side)
RR, RT = 0.128, 0.022                      # the ring's middle radius and its tube's


def on_ball(d, lift=0.0):
    d = unit(*d)
    s = 1 / math.sqrt(((d / R) ** 2).sum())
    return C + d * (s + lift)


def torus(c, rr, rt, n=14, seg=72):
    """A ring of round section about the Y axis (its face to the front): a lathe of a circle."""
    prof = [(rr + rt * math.cos(a), rt * math.sin(a)) for a in np.linspace(0, 2 * math.pi, n + 1)]
    return lathe(c, (0, -1, 0), prof, seg=seg)


def disc(c, r, thick, seg=64):
    return lathe(c, (0, -1, 0), [(0, -thick / 2), (r, -thick / 2), (r, thick / 2), (0, thick / 2)], seg=seg)


# --- the orange core: the body, its knob and the neck up to the ring (fused, for the face's patches) ----------
k.add("ball", ell(C, R, sub=5), ORANGE, "body")
KN = C + np.array([0.008, -0.004, -0.098])
k.add("knob", ell(KN, (0.03, 0.028, 0.026)), ORANGE, "body")
NECK = [C + np.array([0, 0.004, 0.07]), C + np.array([0, 0.006, 0.12]), RC + np.array([0, 0, -RR - 0.004])]
k.add("neck", tube(NECK, [0.048, 0.033, 0.038]), ORANGE, "body")

# --- the fan: the guard, the plasma filling it, the blades and the hub (loose: they keep their edges) ---------
k.add("ring", torus(RC, RR, RT), ORANGE, "body", fuse=False)
# the plasma inside the guard and its rim outside it: one thin disc behind the ring's middle plane, so the ring's
# back half still shows orange from behind
k.add("auradisc", disc(RC + np.array([0, 0.014, 0]), RR + RT + 0.016, 0.014), AURA, "body", fuse=False, surface="glow")


def blade_outline(rm=0.064, ra=0.052, rb=0.05, n=40):
    """A broad rounded paddle about the hub: an oval along the blade, its inner end at the hub."""
    return [(rm + ra * math.cos(t), rb * math.sin(t) * (0.82 if math.sin(t) < 0 else 1.0))
            for t in np.linspace(0, 2 * math.pi, n, endpoint=False)]


for i in range(3):
    a = math.radians(55 + 120 * i)
    ex = np.array([math.cos(a), 0, math.sin(a)])
    ez = np.array([-math.sin(a), 0, math.cos(a)])
    k.add("blade%d" % i, slab(blade_outline(), RC + np.array([0, -0.004, 0]), ex, ez, 0.014), BLADE, "body",
          fuse=False)
HUBD = unit(-0.28, -1, -0.12)
k.add("hub", lathe(RC + np.array([0, -0.004, 0]), HUBD,
                   [(0, -0.004), (0.03, -0.004), (0.027, 0.012), (0.012, 0.048), (0.004, 0.064), (0, 0.066)]),
      ORANGE, "body", fuse=False)

# --- the aura round the body and the neck: opaque yellow a little larger and set back, as rotom.py's ---------
AB = np.array([0, BACK, 0])
k.add("auraball", outward(ell(C + AB, R * 1.11, sub=5)), AURA, "body", fuse=False, surface="glow")
k.add("auraneck", outward(tube([p + AB * 0.7 for p in NECK], [0.06, 0.045, 0.036])), AURA, "body", fuse=False,
      surface="glow")
k.add("auraknob", outward(ell(KN + AB * 0.8 + np.array([0, 0, -0.006]), (0.042, 0.04, 0.038))), AURA, "body",
      fuse=False, surface="glow")


def behind(shape):
    V, F = shape
    V = V.copy(); V[:, 1] = 2 * BACK - V[:, 1]
    return V, [f[::-1] for f in F]


for name, shape in (("backball", ell(C, R, sub=5)), ("backknob", ell(KN, (0.03, 0.028, 0.026)))):
    k.add(name, behind(shape), ORANGE, "body", fuse=False)


# --- the arms: a tube of plasma out of the ring's side curling into a spiral on a broad yellow disc -----------
def spiral(c, r0, r1, th0, turns, n):
    """Points of a spiral in the front's plane (x, z) about c, from radius r0 at angle th0 anticlockwise inward."""
    th = th0 + np.linspace(0, 2 * math.pi * turns, n)
    r = np.linspace(r0, r1, n)
    return [c + np.array([ri * math.cos(t), 0, ri * math.sin(t)]) for ri, t in zip(r, th)]


ARMS = {
    # bone: (the root on the ring, the way out, the spiral's middle, its first angle)
    "armL": (RC + np.array([-RR - 0.01, 0.006, 0.02]), [RC + np.array([-0.2, 0.006, 0.07])],
             np.array([-0.268, 0.006, 0.606]), math.radians(53)),
    "armR": (RC + np.array([0.09, 0.006, -0.1]), [RC + np.array([0.17, 0.006, -0.19])],
             np.array([0.262, 0.006, 0.242]), math.radians(-127)),
}
SR0, SR1, TURNS = 0.058, 0.012, 1.2
for bone, (root, via, sc, th0) in ARMS.items():
    curl = spiral(sc, SR0, SR1, th0, TURNS, 34)
    k.add(bone, outward(tube([root] + via + curl[:3], [0.02, 0.016, 0.018, 0.0175, 0.017], seg=14, per=6)), AURA, bone,
          fuse=False, surface="glow")
    k.add(bone + "curl", outward(tube(curl[1:], list(np.linspace(0.0185, 0.01, len(curl) - 1)), seg=14, per=4)), CURL, bone,
          fuse=False, surface="glow")
    k.add(bone + "pad", disc(sc + np.array([0, 0.008, 0]), SR0 + 0.026, 0.012), AURA, bone, fuse=False, surface="glow")

# --- the face: a small smile under the eyes ---------------------------------------------------------------------
CO, CM = srgb(ORANGE), srgb(MOUTH)
def smile(u, v):
    c = np.tile(CO, (len(u), 1))
    c[np.abs(v - (0.95 * u * u - 0.32)) < 0.32] = CM
    return c
dm = unit(0.06, -1, -0.36)
k.decal(on_ball(dm), dm, (0.022, 0.013), smile, eps=0.003, outline=lambda th: 1.0, rings=14, segs=56)

k.build(voxel=0.004)

# Large eyes: a white ring round a yellow iris with a solid dark pupil and a glint, a dark edge
CW, CI, CP, CK = srgb(WHITE), srgb(IRIS), srgb(PUPIL), srgb(INK)
def eye(u, v):
    c = np.tile(CW, (len(u), 1))
    r = np.sqrt(u * u + v * v)
    c[r < 0.6] = CI
    c[((u - 0.04) / 0.24) ** 2 + ((v + 0.02) / 0.3) ** 2 < 1] = CP
    c[((u + 0.14) / 0.14) ** 2 + ((v - 0.22) / 0.14) ** 2 < 1] = CW
    c[r > 0.84] = CK
    return c
de = unit(0.46, -0.85, 0.14)
E = on_ball(de)
def eyes(state):
    k.decal(E, de, (0.03, 0.033), eye_look(state, eye, CO, CK, line=0.16), eps=0.004, rings=20, segs=56,
            up=(0, 0, 1), mirror=True)

k.eye_states(eyes)
k.export(out)
