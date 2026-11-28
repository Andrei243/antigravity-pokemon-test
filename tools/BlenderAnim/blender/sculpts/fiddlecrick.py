# Fiddlecrick sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/fiddlecrick.py -- <the export's Fiddlecrick.glb> <out.glb>
#
# Chirpet grown up (chirpet.py's colours, eyes, mouth, antennae and legs). From the project's picture
# (10016-Fiddlecrick.jpg) and brief (CP-dex-1-plateau.md, 16): the carefree cricket of the fable, standing upright on
# two long bent legs, playing the fiddle. A long green head with a forward chin, big dark brown eyes in a pale sclera,
# a wide open grin with a pink tongue; a ragged straw hat tilted back on its crown; long antennae out from under the
# brim arcing back. A long upright abdomen, its front yellow-green and ringed with segment lines; orange-brown wing
# cases folded down its back. A violin of orange varnish held under its chin on the +X shoulder, with f-holes, a
# black fingerboard, a pale bridge and strings and a scroll at the neck's end; a long bow drawn across it below the
# chin by the -X hand.
# Where the picture and the brief parted: the brief makes the wing cases the violin and a hind leg the bow; the
# picture holds a real violin and a bow in its hands. The picture is built, and the brief's meaning is kept for the
# back camera: the wing cases are cut to the two halves of a violin's waisted body, varnished, purfled dark, an
# f-hole each. The picture's violin is on the viewer's right, +X, which the game's front camera sees.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from mathutils import Euler
from sculptkit import Kit, ell, tube, slab, hull, lathe, sheet, ring_band, srgb, inside, unit, eye_look, outward, tri, \
    args, sibling

c = sibling("chirpet")
GREEN, GREEN_D, BELLY, LINE, BROWN = c.GREEN, c.GREEN_D, c.BELLY, c.LINE, c.BROWN
ANT = c.ANT
SCLERA = "#EEF2B4"
VARNISH, DEEP, EBONY, PALE = "#D86C26", "#8A3A16", "#2A1810", "#EFE4C8"
CASE, CASE_D = "#C0601E", "#7A3214"
STICK, TIP = "#7A3A1C", "#B8402A"
STRAW, STRAW_D = "#D8B06A", "#B08646"
E = 0.003

HC, HR = np.array([0.0, -0.055, 0.745]), np.array([0.092, 0.085, 0.094])     # the head
SC, SR = np.array([0.0, -0.118, 0.695]), np.array([0.06, 0.052, 0.053])      # the chin and muzzle
EYE_D, EYE_HALF = (0.44, -0.84, 0.18), (0.039, 0.044)

# The violin's frame: from its chin end O along D, its top facing N, across it Bv
O = np.array([0.025, -0.105, 0.635])
D = unit(1, -0.55, -0.25)
N = unit(0, -0.5, 0.85); N = N - D * (N @ D); N /= np.linalg.norm(N)
Bv = np.cross(N, D); Bv /= np.linalg.norm(Bv)
VL = 0.2                                    # the body's length


def violin_half(t):
    """The violin body's half width at t (0 the chin end, 1 the neck end): two bouts and a waist."""
    return np.interp(t, [0, 0.05, 0.2, 0.4, 0.55, 0.68, 0.84, 0.95, 1],
                     [0.026, 0.05, 0.066, 0.054, 0.04, 0.045, 0.053, 0.042, 0.022])


def violin_outline(L, scale=1.0, inset=0.0):
    t = np.linspace(0, 1, 31)
    a = inset + t * (L - 2 * inset)
    h = violin_half(t) * scale
    return [(x, y) for x, y in zip(a, h)] + [(x, -y) for x, y in zip(a, h)][::-1]


def bar(a0, b0, a1, b1, w):
    """A thin bar in a slab's plane from (a0, b0) to (a1, b1), w half wide."""
    p0, p1 = np.array([a0, b0]), np.array([a1, b1])
    e = (p1 - p0) / np.linalg.norm(p1 - p0); o = np.array([-e[1], e[0]]) * w
    return [tuple(p0 + o), tuple(p1 + o), tuple(p1 - o), tuple(p0 - o)]


def violin(k, bone):
    at = lambda a, n=0.0, b=0.0: O + D * a + N * n + Bv * b
    k.add("ribs", slab(violin_outline(VL), O, D, Bv, 0.044), DEEP, bone, fuse=False, surface="shell")
    k.add("top", slab(violin_outline(VL, 0.9, 0.008), O, D, Bv, 0.05), VARNISH, bone, fuse=False, surface="shell")
    for s in (1, -1):
        k.add(f"fhole{s}", slab(bar(0.07, 0.032 * s, 0.13, 0.026 * s, 0.0058), O + N * 0.025, D, Bv, 0.006), EBONY,
              bone, fuse=False)
    k.add("tailpiece", slab(bar(0.004, 0, 0.05, 0, 0.011), O + N * 0.027, D, Bv, 0.008), EBONY, bone, fuse=False)
    k.add("bridge", slab([(-0.022, 0), (0.022, 0), (0.017, 0.015), (-0.017, 0.015)], at(0.07, 0.023), Bv, N, 0.007),
          PALE, bone, fuse=False)
    k.add("fingerboard", slab([(0.085, 0.014), (0.31, 0.011), (0.31, -0.011), (0.085, -0.014)], O + N * 0.03, D, Bv,
                              0.012), EBONY, bone, fuse=False)
    k.add("strings", slab(bar(0.03, 0, 0.3, 0, 0.0055), O + N * 0.0375, D, Bv, 0.004), PALE, bone, fuse=False)
    pts = [at(a, n, b) for a in (0.19, 0.3) for n in (0.0, 0.026) for b in (-0.012, 0.012)]
    k.add("neck", hull(pts), DEEP, bone, fuse=False)
    k.add("pegbox", ell(at(0.315, 0.014), (0.019, 0.018, 0.02)), DEEP, bone, fuse=False)
    k.add("scroll", ell(at(0.335, 0.016), (0.016, 0.016, 0.016)), VARNISH, bone, fuse=False)


def bow(k, bone):
    """The bow: its stick in front, its pale hair just over the strings, an ebony frog, a red tip."""
    B = O + D * 0.065 + N * 0.048
    d = unit(0.79, 0.0, 0.62)
    F, T = B - d * 0.26, B + d * 0.24
    k.add("bowstick", tube([F, (F + T) / 2 + N * 0.006, T], [0.0105, 0.0105, 0.0095], seg=12), STICK, bone,
          fuse=False)
    off = -N * 0.016
    k.add("bowhair", tube([F + off + d * 0.03, T + off - d * 0.02], [0.006, 0.006], seg=10), PALE, bone, fuse=False)
    s1 = np.cross(d, N); s1 /= np.linalg.norm(s1)
    pts = [F + d * da + N * dn + s1 * ds for da in (-0.005, 0.04) for dn in (-0.026, 0.006) for ds in (-0.01, 0.01)]
    k.add("frog", hull(pts), EBONY, bone, fuse=False)
    k.add("bowtip", ell(T + d * 0.006 - N * 0.006, (0.014, 0.014, 0.014)), TIP, bone, fuse=False)
    return F, T


def hat(k):
    """The straw hat, tilted back and to one side: a brim with a ragged edge, a round crown, a darker band."""
    m = np.array(Euler([math.radians(-14), math.radians(7), 0]).to_matrix())
    Hc = np.array([0.0, -0.055, 0.822])
    up = m @ np.array([0, 0, 1.0])
    def brim(u, v):
        th = 2 * math.pi * u
        R = 0.138 + 0.007 * tri(u * 23 + 0.25)
        r = 0.062 + v * (R - 0.062)
        P = np.stack([r * np.cos(th), r * np.sin(th), -0.014 * v * v], axis=-1)
        return P @ m.T + Hc
    k.add("brim", outward(sheet(brim, 97, 6, 0.01)), STRAW, "head", fuse=False, surface="fur")
    k.add("crown", lathe(Hc - up * 0.006, up, [(0, 0), (0.073, 0), (0.075, 0.034), (0.066, 0.062), (0.04, 0.075),
                                               (0, 0.078)]), STRAW, "head", fuse=False, surface="fur")
    k.add("hatband", ring_band(Hc + up * 0.016, up, 0.0775, 0.014, thick=0.005), STRAW_D, "head", fuse=False,
          surface="fur")


def lay_eyes(k, state):
    look = eye_look(state, c.eye_open(sclera=SCLERA, iris_r=0.8, lift=0.04), srgb(GREEN), srgb(BROWN), line=0.16)
    for s in (1, -1):
        p, n = c.on_ell(HC, HR, (EYE_D[0] * s, EYE_D[1], EYE_D[2]))
        col = look if s > 0 else (lambda u, v, f=look: f(-u, v))
        k.decal(p, n, EYE_HALF, col, up=(-0.12 * s, 0, 1), eps=0.0024, rings=20, segs=56)


def fiddlecrick(src, out):
    k = Kit(src, coat="shell")
    # ---- the trunk: a long upright abdomen leaning a little forward, a broad chest, a short neck
    k.add("abdomen", ell((0, 0.01, 0.44), (0.09, 0.08, 0.175), rot=(8, 0, 0)), GREEN, "body")
    k.add("thorax", ell((0, -0.015, 0.6), (0.088, 0.072, 0.075)), GREEN, "body")
    k.add("neck", ell((0, -0.04, 0.66), (0.046, 0.046, 0.042)), GREEN, "head")
    def belly(P):
        e = (P[:, 0] / 0.064) ** 2 + ((P[:, 2] - 0.425) / 0.185) ** 2
        return inside(np.sqrt(e) - 1, E) * (P[:, 1] < -0.015)
    k.paint(belly, BELLY, on=["abdomen", "thorax"])
    def segments(P):
        m = np.zeros(len(P))
        for z in (0.3, 0.35, 0.4, 0.45, 0.5, 0.55):
            m = np.maximum(m, inside(np.abs(P[:, 2] - z) - 0.0026, 0.0012))
        e = (P[:, 0] / 0.062) ** 2 + ((P[:, 2] - 0.425) / 0.18) ** 2
        return m * inside(np.sqrt(e) - 1, E) * (P[:, 1] < -0.02)
    k.paint(segments, LINE, on=["abdomen"])
    # ---- the head: round above, a long chin forward, the grin carved... as a patch on the fused muzzle
    k.add("head", ell(HC, HR, sub=5), GREEN, "head")
    k.add("muzzle", ell(SC, SR, rot=(-15, 0, 0)), GREEN, "head")
    p, n = c.on_ell(SC, SR, (0, -1, -0.22))
    k.decal(p, n, (0.032, 0.018), c.mouth_look, eps=0.0024, outline=c.smile_outline, rings=12, segs=56)
    hat(k)
    c.antenna(k, [(0.034, -0.098, 0.826), (0.05, -0.15, 0.878), (0.078, -0.15, 0.925), (0.12, -0.08, 0.952),
                  (0.17, 0.02, 0.932)], 0.0115, 0.0085)
    # ---- the legs: long, bent at the knee, thick thighs fused into the hips, spined shins, long feet
    for s, side, knee, ankle in ((1, "R", (0.105, -0.075, 0.175), (0.098, -0.005, 0.04)),
                                 (-1, "L", (-0.1, -0.095, 0.19), (-0.095, -0.03, 0.04))):
        hip = (0.065 * s, 0.02, 0.31)
        c.leg(k, f"thigh{side}", [hip, ((hip[0] + knee[0]) / 2, -0.03, 0.245), knee], [0.036, 0.031, 0.024],
              f"leg{side}", mirror=False)
        c.leg(k, f"shin{side}", [knee, ((knee[0] + ankle[0]) / 2, (knee[1] + ankle[1]) / 2, 0.11), ankle],
              [0.022, 0.019, 0.017], f"leg{side}", mirror=False)
        k.add(f"foot{side}", ell((ankle[0] * 1.02, ankle[1] - 0.045, 0.014), (0.025, 0.052, 0.014)), GREEN_D,
              f"leg{side}")
        a, b = np.array(knee), np.array(ankle)
        for i in range(2):
            q = a + (b - a) * (i + 1) / 3
            ax = unit(*(b - a)); dd = unit(0.2 * s, 1, -0.3)
            s1 = np.cross(ax, dd); s1 /= np.linalg.norm(s1)
            k.add(f"spine{side}{i}", hull([q + ax * 0.009, q - ax * 0.009, q + s1 * 0.008, q - s1 * 0.008,
                                            q + dd * 0.032]), GREEN_D, f"leg{side}", fuse=False)
    # ---- the arms: +X holds the violin's neck, -X draws the bow; the middle pair below
    c.leg(k, "armR", [(0.075, -0.03, 0.6), (0.165, -0.07, 0.5), (0.235, -0.2, 0.565)], [0.021, 0.019, 0.017],
          "armR", mirror=False)
    k.add("handR", ell((0.238, -0.208, 0.57), (0.024, 0.022, 0.022)), GREEN_D, "armR")
    c.leg(k, "armL", [(-0.075, -0.03, 0.6), (-0.155, -0.09, 0.52), (-0.1, -0.17, 0.522)], [0.021, 0.019, 0.017],
          "armL", mirror=False)
    k.add("handL", ell((-0.097, -0.172, 0.522), (0.024, 0.022, 0.022)), GREEN_D, "armL")
    c.leg(k, "midR", [(0.07, -0.02, 0.47), (0.13, -0.07, 0.42), (0.11, -0.13, 0.4)], [0.017, 0.016, 0.015], "body",
          mirror=False)
    c.leg(k, "midL", [(-0.07, -0.02, 0.47), (-0.13, -0.08, 0.44), (-0.12, -0.155, 0.47)], [0.017, 0.016, 0.015],
          "body", mirror=False)
    # ---- the wing cases down the back: the two halves of a violin's body, purfled dark, an f-hole each
    WO, WX, WZ = (0.006, 0.07, 0.645), unit(0, 0.02, -1), unit(1, 0.42, 0)
    t = np.linspace(0, 1, 26)
    half = np.interp(t, [0, 0.08, 0.25, 0.48, 0.6, 0.8, 0.94, 1], [0.03, 0.058, 0.07, 0.046, 0.05, 0.084, 0.07, 0.03])
    L = 0.36
    out_ = [(x * L, h) for x, h in zip(t, half)] + [(x * L, 0.0) for x in t[::-1]]
    in_ = [(0.012 + x * (L - 0.024), h * 0.86) for x, h in zip(t, half)] + [(0.012 + x * (L - 0.024), 0.0)
                                                                            for x in t[::-1]]
    k.add("case", slab(out_, WO, WX, WZ, 0.02), CASE_D, "body", fuse=False, mirror=True, surface="shell")
    k.add("caseface", slab(in_, WO, WX, WZ, 0.026), CASE, "body", fuse=False, mirror=True, surface="shell")
    k.add("casefhole", slab(bar(0.19, 0.04, 0.26, 0.05, 0.0062), WO, WX, WZ, 0.032), EBONY, "body", fuse=False,
          mirror=True)
    # ---- the violin under the chin and the bow across it
    violin(k, "armR")
    bow(k, "armL")
    k.build(voxel=0.0062, smooth=3, band=0.006, wband=0.04)
    k.eye_states(lambda state: lay_eyes(k, state))
    k.export(out)


if __name__ == "__main__":
    fiddlecrick(*args())
