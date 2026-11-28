# Fluturapol (Carpathia No. 10075, the mountain butterfly) sculpted from nothing on the skeleton and clips of the
# game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/fluturapol.py -- <the export's Fluturapol.glb> <out.glb>
#
# Grown from fluturel.py (sibling("fluturel")): the line's near-black, its orange spots (red-orange now, down the
# abdomen's sides), its soft white tufts (the thorax's fluff), its face made the same way (on_ell, light lines on
# dark: the little smile, the cheeks gone).
# From its picture (docs/art/reference/gemini/carpathia/10075-Fluturapol.jpg) and its brief (CP-dex-2-mountains.md,
# 75): a large white Apollo butterfly hovering. Its wings are its largest mass: the upper pair broad, reaching up and
# out, the lower pair smaller and rounded; white, a dark grey band along the outer margin (the grey dusting at the root left out),
# round black spots, and bright red eyespots ringed in black with an orange heart (one on each upper wing, two on each
# lower). A furry grey body: a round dark grey head with big dark eyes and a glint and a small smile, two long black
# antennae ending in white fluffy knobs, a thorax ruffed with pale fluff, a banded grey abdomen hanging below with a
# row of red-orange dots down each side, and short furry grey legs tucked before it. The pattern is stacked slabs,
# each colour thicker than the one under it, so it shows on both faces; the wings are a mirror pair in the plane of
# the body swept a little back, their faces to both of the game's cameras.
# Bones: body (0, 0, 0.323), wing{s} (0.05, 0.04, 0.383), head (0, -0.02, 0.453), leg{s} (0.04, -0.04, 0.343).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, lumpy, outward, srgb, inside, smoothstep, unit, eye_look, args, sibling

fl = sibling("fluturel")

HEAD, BODY, BAND, FLUFF, LEG = "#3C3C46", "#7C7C86", "#5E5E68", "#C8C8CE", "#55555E"
WHITE, BORDER, DUST, SPOT, RED, HEART = "#F2F1EC", "#4A4A54", "#D9D8DA", "#1E1E24", "#E43A2A", "#F8922E"
IRIS, PUPIL, GLINT, LINE = "#6E3A2C", "#17121A", "#FFFFFF", "#A8A6B2"
DOT, KNOB = "#EC5A2C", fl.TUFT

HC, HR = np.array([0.0, -0.038, 0.472]), np.array([0.071, 0.066, 0.068])
TC, TR = np.array([0.0, 0.0, 0.385]), np.array([0.066, 0.062, 0.07])

# --- the wings: fans round the wing's root, each colour a slab thicker than the one under it -------------------
ORIGIN = np.array([0.03, 0.035, 0.385])
EX = unit(1, 0.32, 0)                        # out, swept a little back
EZ = np.array([0, 0, 1.0])
UPPER = [(3, 0.31), (12, 0.38), (22, 0.44), (32, 0.5), (42, 0.54), (50, 0.57), (55, 0.585), (59, 0.57), (62, 0.53), (64, 0.46)]
LOWER = [(-4, 0.2), (-14, 0.28), (-26, 0.33), (-38, 0.345), (-50, 0.335), (-62, 0.3), (-72, 0.24), (-81, 0.16),
         (-87, 0.09)]


def radius(table, a):
    A = np.array([t[0] for t in table]); R = np.array([t[1] for t in table])
    if A[1] < A[0]:
        A, R = A[::-1], R[::-1]
    return float(np.interp(a, A, R))


def fan(table, inner, outer, a0, a1, n=60):
    """The outline of the wing between two shares of its outline, from angle a0 to a1: a pie when inner is nought."""
    angs = np.linspace(a0, a1, n)
    out_ = [(outer * radius(table, a) * math.cos(math.radians(a)), outer * radius(table, a) * math.sin(math.radians(a)))
            for a in angs]
    if inner == 0:
        return np.array([(0.0, 0.0)] + out_)
    in_ = [(inner * radius(table, a) * math.cos(math.radians(a)), inner * radius(table, a) * math.sin(math.radians(a)))
           for a in angs[::-1]]
    return np.array(out_ + in_)


def disc(table, a, f, r, n=28):
    """A round spot on a wing: at angle a and share f of the outline, r across its half."""
    R = f * radius(table, a)
    cx, cy = R * math.cos(math.radians(a)), R * math.sin(math.radians(a))
    return np.array([(cx + r * math.cos(t), cy + r * math.sin(t)) for t in np.linspace(0, 2 * math.pi, n, endpoint=False)])


def plate(k, name, outline, colour, thick):
    k.add(name, slab(outline, ORIGIN, EX, EZ, thick), colour, "wing{s}", fuse=False, mirror=True, surface="leaf")


def wing(k, tag, table, lo, hi, spots, eyes):
    plate(k, f"{tag}border", fan(table, 0, 1.0, lo, hi, 120), BORDER, 0.008)
    plate(k, f"{tag}white", fan(table, 0, 0.88, lo + 1.2, hi - 1.2, 90), WHITE, 0.012)
    for i, (a, f, r) in enumerate(spots):
        plate(k, f"{tag}spot{i}", disc(table, a, f, r), SPOT, 0.016)
    for i, (a, f, r) in enumerate(eyes):
        plate(k, f"{tag}ring{i}", disc(table, a, f, r, 40), SPOT, 0.017)
        plate(k, f"{tag}red{i}", disc(table, a, f, r * 0.72, 40), RED, 0.02)
        plate(k, f"{tag}heart{i}", disc(table, a, f, r * 0.36, 28), HEART, 0.023)


def wings(k):
    wing(k, "upper", UPPER, 3, 64,
         [(47, 0.64, 0.03), (38, 0.76, 0.032), (25, 0.68, 0.028), (13, 0.8, 0.022), (49, 0.4, 0.022),
          (28, 0.44, 0.024), (15, 0.52, 0.02)],
         [(35, 0.57, 0.055)])
    wing(k, "lower", LOWER, -87, -4,
         [(-20, 0.7, 0.024), (-68, 0.66, 0.022), (-46, 0.76, 0.02)],
         [(-30, 0.52, 0.05), (-58, 0.45, 0.042)])


def body(k):
    k.add("thorax", ell(TC, TR, sub=5), BODY, "body")
    # the ruff of pale fluff round the thorax
    for i in range(12):
        t = 2 * math.pi * i / 12
        p = TC + np.array([0.055 * math.cos(t), 0.05 * math.sin(t), 0.035])
        k.add(f"ruff{i:02d}", ell(p, (0.026, 0.026, 0.024)), FLUFF, "body")
    # the abdomen: banded segments hanging down and a little back
    for i, (z, y, r) in enumerate(((0.33, 0.012, 0.064), (0.285, 0.02, 0.062), (0.24, 0.028, 0.058),
                                   (0.197, 0.036, 0.052), (0.158, 0.043, 0.044), (0.124, 0.05, 0.033))):
        k.add(f"abd{i}", ell((0, y, z), (r, r * 0.95, 0.03)), BODY, "body")
    for z in (0.307, 0.262, 0.218, 0.177, 0.14):
        k.paint(lambda P, z=z: inside(np.abs(P[:, 2] - z) - 0.005, 0.0025) * inside(P[:, 2] - 0.34, 0.01), BAND,
                on=["abd"])
    # short furry legs tucked before the abdomen
    for j, (z, dy) in enumerate(((0.35, -0.045), (0.32, -0.035))):
        pts = [(0.03, dy, z), (0.05, dy - 0.03, z - 0.03), (0.052, dy - 0.04, z - 0.07)]
        k.add(f"leg{j}", tube(pts, [0.016, 0.014, 0.012]), LEG, "leg{s}", mirror=True)
    # the head, its smile and the antennae with their white knobs
    k.add("skull", ell(HC, HR, sub=5), HEAD, "head")
    k.add("neck", ell((0, -0.02, 0.43), (0.045, 0.042, 0.03)), HEAD, "head")
    Hd, Ln = srgb(HEAD), srgb(LINE)
    def smile(u, v):
        c = np.tile(Hd, (len(u), 1))
        c[(np.abs(v - (-0.1 + 0.55 * (u * u - 0.5))) < 0.24) & (np.abs(u) < 0.85)] = Ln
        return c
    p, n = fl.on_ell(HC, HR, 0, -30)
    k.decal(p, n, (0.018, 0.009), smile, eps=0.0016, rings=10, segs=48)
    for s in (1, -1):
        b0 = HC + np.array([0.022 * s, -0.01, 0.055])
        pts = [b0, b0 + (0.02 * s, -0.012, 0.11), b0 + (0.052 * s, -0.018, 0.22), b0 + (0.085 * s, -0.012, 0.27)]
        k.add(f"antenna{'R' if s > 0 else 'L'}", outward(tube(pts, [0.0125, 0.012, 0.0115, 0.011])), SPOT, "head",
              fuse=False)
        k.add(f"knob{'R' if s > 0 else 'L'}", outward(ell(pts[-1] + (0.008 * s, -0.004, 0.012), (0.024, 0.022, 0.021),
                                                          deform=lumpy(5, 3, 0.18), sub=3)), KNOB, "head", fuse=False)
    # red-orange dots down each side of the abdomen
    for z, y, r in ((0.3, 0.018, 0.062), (0.255, 0.026, 0.059), (0.21, 0.034, 0.054), (0.17, 0.041, 0.046)):
        for s in (1, -1):
            nn = unit(0.85 * s, -0.5, 0.0)
            k.decal(np.array([0, y, z]) + nn * r * 0.98, nn, (0.012, 0.012), DOT, eps=0.0018, rings=6, segs=24)


def eye_open(u, v, I=srgb(IRIS), K=srgb(PUPIL), W=srgb(GLINT), L=srgb(LINE)):
    c = np.tile(I, (len(u), 1))
    c[((u + 0.08) / 0.5) ** 2 + ((v + 0.05) / 0.55) ** 2 < 1] = K
    c[((u + 0.3) / 0.22) ** 2 + ((v - 0.3) / 0.22) ** 2 < 1] = W
    c[u * u + v * v > 0.86 ** 2] = L
    return c


def fluturapol(src, out):
    k = Kit(src, coat="fur")
    body(k)
    wings(k)
    k.build(voxel=0.0045, smooth=3, band=0.005, wband=0.035)
    V = np.array([v.co[:] for v in k.ob.data.vertices])
    a = math.radians(31.5)
    proj = V[:, 0] * math.cos(a) + V[:, 1] * math.sin(a)
    h, w, l = V[:, 2].max(), np.ptp(V[:, 0]), np.ptp(V[:, 1])
    print("KIT: board: shows %.3f in a square of %.3f" % (np.ptp(proj), 1.3 * max(h, 0.85 * max(w, l))))
    def lay(state):
        p, n = fl.on_ell(HC, HR, 34, 6)
        k.decal(p, n, (0.025, 0.028), eye_look(state, eye_open, srgb(HEAD), srgb(LINE), line=0.16), eps=0.0016,
                rings=20, segs=56, mirror=True)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    fluturapol(*args())
