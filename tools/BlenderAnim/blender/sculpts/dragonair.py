# Dragonair sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/dragonair.py -- <the export's Dragonair.glb> <out.glb>
# Dratini grown long and graceful: a bright blue serpent lying in a sweep, the white underside along the lower flank and
# up the front of a long slim neck; a slim head with a smooth tapering snout (no white bulb now), a short white horn on
# the forehead, small dark eyes with a magenta iris and a glint, and for ears small white wings of three feathers rising
# from a white disc on each side of the head; a dark blue orb on the throat under the chin, and the tail rising behind
# at the left to two dark blue orbs and a thin spike.
# Built by Dratini's functions (sibling("dratini")): the line, its tubes, the belly and the eye's way.
# The export's chain: tail and body (-0.34, 0.44, 0.07) behind on the left, seg1 to seg6 round the sweep on the ground,
# seg7 to seg11 up the neck, head (0.02, -0.08, 0.72), ears (-0.03 and 0.07, -0.08, 0.8): the head stands at x 0.02,
# so its pairs are mirrored about that by hand.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, slab, srgb, inside, unit, eye_look, args, outward, sibling

dr = sibling("dratini")
BLUE, WHITE, INK = "#3D98E4", dr.WHITE, dr.INK
ORB, ORB_GLOW = "#2B3A9E", "#3E52C8"
IRIS = "#B8336E"

LINE = [(-0.465, 0.465, 0.36), (-0.44, 0.46, 0.25), (-0.4, 0.452, 0.15), (-0.34, 0.44, 0.07),
        (-0.22, 0.419, 0.05), (-0.045, 0.376, 0.05), (0.14, 0.3, 0.056), (0.217, 0.139, 0.06), (0.19, -0.004, 0.062),
        (0.08, -0.1, 0.07), (-0.025, -0.123, 0.131), (-0.066, -0.11, 0.246), (-0.04, -0.08, 0.4),
        (-0.013, -0.063, 0.54), (0.009, -0.064, 0.65), (0.02, -0.075, 0.72)]
RADS = [0.01, 0.015, 0.024, 0.034, 0.044, 0.052, 0.058, 0.062, 0.064, 0.064, 0.062, 0.057, 0.05, 0.044, 0.04,
        0.04]
BONES = ["tail", "tail", "tail", "body", "seg1", "seg2", "seg3", "seg4", "seg5", "seg6", "seg7", "seg8", "seg9",
         "seg10", "seg11"]
LINE = [(x, y, max(z, r + 0.003)) for (x, y, z), r in zip(LINE, RADS)]          # nothing under the ground
X0 = 0.02                                       # the head's middle line
H = np.array([X0, -0.105, 0.775])                 # the head's middle
HR = np.array([0.086, 0.096, 0.081])


def side(p, s):
    """A point given for the +X side about the head's middle line, on side s."""
    p = np.array(p, float)
    return np.array([X0 + s * (p[0] - X0), p[1], p[2]])


def vec(d, s):
    d = np.array(d, float)
    return np.array([s * d[0], d[1], d[2]])


def cone(base, d, length, r, n=8):
    """A short solid spike: a ring of points round base and a point along d."""
    a = np.cross(d, (0, 1.0, 0)); a /= np.linalg.norm(a); b = np.cross(d, a)
    ring = [base + r * (math.cos(t) * a + math.sin(t) * b) for t in np.linspace(0, 2 * math.pi, n, endpoint=False)]
    return hull(ring + [base + d * length])


def lens(a0, a1, width, n=12):
    """A feather's outline from a0 to a1 in its plane, widest a little past its middle."""
    a0, a1 = np.array(a0, float), np.array(a1, float)
    d = a1 - a0; L = np.linalg.norm(d); d /= L
    sd = np.array([-d[1], d[0]])
    top = [a0 + d * L * t + sd * width * math.sin(math.pi * t ** 0.8) for t in np.linspace(0, 1, n)]
    bot = [a0 + d * L * t - sd * width * math.sin(math.pi * t ** 0.8) for t in np.linspace(1, 0, n)[1:-1]]
    return [tuple(p) for p in top + bot]


def magenta_eye(u, v):
    K, I, G = srgb(INK), srgb(IRIS), srgb("#FFFFFF")
    c = np.tile(K, (len(u), 1))
    c[u * u + v * v < 0.82 ** 2] = I
    c[(u / 0.4) ** 2 + ((v - 0.08) / 0.52) ** 2 < 1] = K
    c[((u - 0.28) / 0.24) ** 2 + ((v - 0.36) / 0.26) ** 2 < 1] = G
    return c


def dragonair(src, out):
    k = Kit(src, coat="scales")
    line = dr.Line(LINE, RADS)
    line.tubes(k, BONES, BLUE)
    k.paint(dr.belly(line, 3.0, 14.8), WHITE, on=["seg"])

    # The head: slim, a smooth snout tapering forward and a little down
    k.add("nape", tube([LINE[-2], LINE[-1], H + (0, 0.03, -0.02)], [0.04, 0.041, 0.05]), BLUE, "head")
    k.add("skull", ell(H, HR), BLUE, "head")
    def taper(V):
        V = V.copy()
        t = np.clip(-V[:, 1], 0, 1)
        V[:, 0] *= 1 - 0.25 * t
        V[:, 2] *= 1 - 0.2 * t
        return V
    k.add("snout", ell(H + (0, -0.085, -0.028), (0.058, 0.088, 0.05), rot=(8, 0, 0), deform=taper), BLUE, "head")

    # The horn: short and white, up and a little forward from the forehead
    hd = unit(0, -0.4, 1)
    k.add("horn", cone(H + (0, -0.06, 0.05), hd, 0.06, 0.016), WHITE, "head", fuse=False, surface="shell")

    # The ears: a white disc on each side of the head, and three white feathers rising from it, up, out and back
    for s, bone in ((1, "earR"), (-1, "earL")):
        disc = side((X0 + 0.074, -0.09, 0.8), s)
        k.add(f"disc{bone[-1]}", outward(ell(disc, (0.014, 0.026, 0.026), rot=(0, 0, 10 * s), sub=3)), WHITE, bone,
              fuse=False)
        along = unit(0.25 * s, 0.35, 1.0)
        face = vec(unit(0.6, -0.8, 0.05), s)
        ex, ez = dr.ear_frame(along, face)
        ez = -ez if ez @ vec((1, 0.6, 0), s) < 0 else ez          # b out and back
        for j, (tip, w) in enumerate([((0.155, 0.024), 0.023), ((0.125, 0.06), 0.02), ((0.086, 0.082), 0.018)]):
            o = disc + vec((0.004, 0.004, 0.0), s) + (ex * 0.004 + ez * 0.002) * j
            k.add(f"feather{bone[-1]}{j}", slab(lens((0.0, 0.0), tip, w), o, ex, ez, 0.016 - 0.002 * j), WHITE,
                  bone, fuse=False)

    # The orb on the throat under the chin, and the two on the tail with the spike above them
    c, t, r = line.at(13.6)
    j = line.near(c[None])[0]
    k.add("orbneck", ell(c + line.bel[j] * (r + 0.002), (0.03, 0.03, 0.03), sub=3), ORB, "seg10", fuse=False,
          glow=ORB_GLOW)
    for n_, (s_, rr) in enumerate([(1.05, 0.031), (0.4, 0.027)]):
        c, t, r = line.at(s_)
        k.add(f"orbtail{n_}", ell(c, (rr, rr, rr * 0.92), sub=3), ORB, "tail", fuse=False, glow=ORB_GLOW)
    tip = np.array(LINE[0])
    k.add("spike", cone(tip - (0, 0, 0.01), unit(-0.1, 0.05, 1), 0.07, 0.008), BLUE, "tail", fuse=False)

    # The eyes: small and dark, a magenta iris, set on the sides of the head looking forward
    de = unit(0.62, -0.72, 0.22)
    lid, ink = srgb(BLUE), srgb(INK)
    for_eye = H + np.array([0.0, -0.012, 0.0])
    def eyes(state):
        look = eye_look(state, magenta_eye, lid, ink, line=0.17)
        for s in (1, -1):
            d = vec(de, s)
            E = for_eye + d * HR
            k.decal(E, d, (0.028, 0.034), (lambda u, v, s=s: look(s * u, v)), eps=0.005, rings=20, segs=56)

    k.build(voxel=0.0055, smooth=5)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    dragonair(*args())
