# Kyurem sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/kyurem.py -- <the export's Kyurem.glb> <out.glb>
#
# From the references: a grey dragon on four legs, hunched, its back high over the shoulders, grey hide in plates with
# darker seams; a long neck reaching forward and down to a head sheathed in pale blue ice, the ice a helmet of facets
# over the brow and the snout, its grey jaw under it with a row of white teeth, a yellow eye under the ice's brow, a
# yellow crest along each side of the helmet rising back, shards of ice up off the crown; wings of jagged ice raised up
# and back off the shoulders, each ending in two white cones with a yellow mark at their roots; ice over the shoulders
# and along the spine; slim grey legs, the hind ones with heavy thighs, three white claws a foot; a short tail with
# three shards of ice.
# The export's joints: body (0, 0.02, 0.36), head (0, -0.2, 0.44), wings (+-0.07, -0.02, 0.44), tail (0, 0.25, 0.38),
# legs (+-0.1, -0.16, 0.32), hinds (+-0.1, 0.18, 0.32).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, lathe, srgb, inside, unit, eye_look, polygon, boxy, args

GREY, DARK, ICE, ICE2 = "#7C818C", "#50545E", "#B4DCF0", "#93C4E0"
YELLOW, CLAW, INKC, MOUTHC = "#F2D21C", "#ECEEF2", "#16171C", "#2A2C34"

H = np.array([0.0, -0.405, 0.43])           # the head's middle
HR = np.array([0.082, 0.097, 0.076])


def on_ell(c, r, d):
    d = unit(*d)
    t = 1.0 / math.sqrt(((d / np.array(r)) ** 2).sum())
    return np.array(c, float) + d * t


def spike(k, name, base, d, length, width, thick, bone, color=ICE, up=None, sink=0.02, mirror=False, surface="shell"):
    """A solid pointed piece (a shard of ice, a claw): a kite in section, wide across up and thick along it."""
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


def body(k):
    k.add("torso", ell((0, 0.02, 0.385), (0.125, 0.2, 0.125)), GREY, "body")
    k.add("shoulders", ell((0, -0.12, 0.44), (0.125, 0.11, 0.11)), GREY, "body")
    k.add("hips", ell((0, 0.16, 0.4), (0.115, 0.1, 0.1)), GREY, "body")
    k.add("neck", tube([(0, -0.16, 0.47), (0, -0.25, 0.47), (0, -0.33, 0.44)], [0.072, 0.06, 0.055]), GREY, "head")
    # dark seams across the back between the plates
    def seams(P):
        m = np.zeros(len(P))
        for y0 in (-0.08, 0.04, 0.14):
            m = np.maximum(m, inside(np.abs(P[:, 1] - y0) - 0.005, 0.002) * (P[:, 2] > 0.42))
        return m
    k.paint(seams, DARK, on=["torso", "hips", "shoulders"])
    # ice over the shoulders and along the spine
    for s, sd in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        P = np.array([(0.08, -0.2, 0.5), (0.13, -0.14, 0.5), (0.14, -0.2, 0.42), (0.1, -0.24, 0.44),
                      (0.11, -0.12, 0.42), (0.07, -0.15, 0.54), (0.15, -0.17, 0.47)]) * m
        k.add(f"shoulderice{sd}", hull(P), ICE, "body", fuse=False, surface="shell")
    for j, (y, z, L) in enumerate(((-0.02, 0.5, 0.1), (0.08, 0.49, 0.09), (0.17, 0.47, 0.08))):
        spike(k, f"spine{j}", (0, y, z), (0, 0.55, 0.85), L, 0.06, 0.035, "body" if j < 2 else "tail", up=(0, 1, 0),
              color=ICE if j % 2 == 0 else ICE2)


def claws(k, sd, c, bone, m):
    for j, dx in enumerate((-0.025, 0.0, 0.025)):
        spike(k, f"{bone}claw{j}", c + np.array((dx, -0.03, 0.0)) * m, np.array((dx * 1.2, -1, -0.5)) * m, 0.045,
              0.03, 0.024, bone, color=CLAW, up=(0, 0, 1), sink=0.015)


def legs(k):
    for s, sd in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        # the forelegs: a broad shoulder, a slim leg, a small foot
        bone = f"leg{sd}"
        k.add(f"foreupper{sd}", ell(np.array((0.115, -0.15, 0.34)) * m, (0.072, 0.078, 0.1)), GREY, bone)
        k.add(f"foreleg{sd}", tube(np.array([(0.125, -0.16, 0.3), (0.16, -0.2, 0.18), (0.14, -0.21, 0.05)]) * m,
                                   [0.058, 0.047, 0.04]), GREY, bone)
        k.add(f"forefoot{sd}", ell(np.array((0.14, -0.235, 0.025)) * m, (0.04, 0.055, 0.026)), GREY, bone)
        claws(k, sd, np.array((0.14, -0.265, 0.022)) * m, bone, m)
        # an ice cuff round the elbow
        P = (np.array([(0.15, -0.2, 0.22), (0.17, -0.22, 0.18), (0.13, -0.25, 0.2), (0.16, -0.17, 0.2),
                      (0.12, -0.23, 0.24), (0.165, -0.2, 0.25)]) + np.array((0.03, 0, 0))) * m
        k.add(f"elbowice{sd}", hull(P), ICE2, bone, fuse=False, surface="shell")
        # the hind legs: a heavy thigh fused into the hips, a lean shin to the hock, a foot
        bone = f"hind{sd}"
        k.add(f"thigh{sd}", ell(np.array((0.105, 0.17, 0.31)) * m, (0.075, 0.105, 0.115)), GREY, bone)
        k.add(f"hindleg{sd}", tube(np.array([(0.12, 0.2, 0.25), (0.13, 0.24, 0.13), (0.13, 0.19, 0.045)]) * m,
                                   [0.055, 0.042, 0.037]), GREY, bone)
        k.add(f"hindfoot{sd}", ell(np.array((0.13, 0.155, 0.025)) * m, (0.042, 0.06, 0.026)), GREY, bone)
        claws(k, sd, np.array((0.13, 0.12, 0.022)) * m, bone, m)


def wings(k):
    """Jagged plates of ice raised up and back off the shoulders, two white cones at their ends."""
    for s, sd in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        bone = f"wing{sd}"
        root = np.array((0.08, -0.05, 0.47))
        tip = np.array((0.27, 0.12, 0.71))
        # the main slab: broad at the root, thick, its faces turned up and out
        P = [root + (0, -0.04, 0), root + (0.0, 0.08, 0.0), root + (0.03, 0.02, -0.03), root + (-0.01, 0.03, 0.04),
             tip + (0.02, -0.05, 0.0), tip + (-0.03, 0.04, -0.02), tip + (0.0, 0.0, 0.04), tip + (0.04, 0.03, -0.03),
             (root + tip) / 2 + (0.06, 0.04, -0.06), (root + tip) / 2 + (-0.04, -0.06, 0.05)]
        k.add(f"wingice{sd}", hull(np.array(P) * m), ICE, bone, fuse=False, surface="shell")
        # jagged shards off its trailing edge, down and back
        for j, (t, d, L) in enumerate(((0.35, (0.3, 0.8, -0.3), 0.11), (0.65, (0.4, 0.8, -0.1), 0.12))):
            b = root + (tip - root) * t + np.array((0.03, 0.03, -0.03))
            spike(k, f"wingshard{sd}{j}", b * m, np.array(d) * m, L, 0.07, 0.035, bone, color=ICE2, sink=0.04)
        # two white cones out of the end, a yellow mark at each root
        for j, (off, d) in enumerate((((0.0, -0.02, 0.03), (0.75, -0.25, 0.55)), ((0.02, 0.04, -0.02), (0.8, 0.25, 0.25)))):
            c = (tip + np.array(off)) * m
            dd = unit(*(np.array(d) * m))
            k.add(f"cone{sd}{j}", lathe(c, dd, [(0, -0.01), (0.03, -0.01), (0.026, 0.03), (0.0, 0.1)], seg=24),
                  CLAW, bone, fuse=False, surface="shell")
            k.add(f"conemark{sd}{j}", lathe(c - dd * 0.012, dd, [(0, -0.006), (0.036, -0.006), (0.036, 0.004),
                                                                  (0, 0.004)], seg=24), YELLOW, bone, fuse=False,
                  surface="shell")


def tail(k):
    T = [(0, 0.22, 0.4), (0, 0.31, 0.42), (0, 0.39, 0.4)]
    k.add("tail", tube(T, [0.07, 0.05, 0.035]), GREY, "tail")
    for j, (d, L) in enumerate((((0, 0.5, 0.85), 0.11), ((0.7, 0.6, 0.25), 0.09), ((-0.7, 0.6, 0.25), 0.09))):
        spike(k, f"tailshard{j}", (0, 0.38, 0.41), d, L, 0.06, 0.035, "tail", color=ICE, sink=0.03)


def head(k):
    # the ice helmet is the head's top and snout; the grey jaw under it
    k.add("skull", ell(H + (0, 0.01, 0.01), HR, deform=boxy(2.4)), ICE, "head", surface="shell")
    k.add("snout", ell(H + (0, -0.075, -0.008), (0.064, 0.078, 0.054), deform=boxy(2.6)), ICE, "head", surface="shell")
    k.add("jaw", ell(H + (0, -0.06, -0.055), (0.05, 0.08, 0.026), rot=(6, 0, 0)), GREY, "head")
    k.add("mouth", ell(H + (0, -0.075, -0.04), (0.054, 0.085, 0.009), rot=(4, 0, 0)), MOUTHC, "head", carve=True)
    for j, x in enumerate((0.05, -0.05)):
        for i, y in enumerate((-0.07, -0.11)):
            a = H + np.array((x * (1 - 0.3 * i), y, -0.038))
            k.add(f"tooth{j}{i}", hull([a + (0.01, 0, 0), a - (0.01, 0, 0), a + (0, 0.01, 0), a - (0, 0.01, 0),
                                        a + (0, 0, -0.02)]), CLAW, "head", fuse=False, surface="shell")
    # a heavy brow of ice over each eye, and shards up and back off the crown
    for s, sd in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        P = np.array([(0.04, -0.07, 0.055), (0.08, -0.03, 0.05), (0.07, -0.06, 0.03), (0.03, -0.1, 0.035),
                      (0.05, 0.0, 0.07), (0.075, -0.02, 0.025)]) + H * np.array([0, 1, 1])
        k.add(f"brow{sd}", hull(P * m), ICE2, "head", fuse=False, surface="shell")
        # the yellow crest along the helmet's side, rising back
        Y = np.array([(0.06, -0.07, 0.045), (0.078, -0.05, 0.035), (0.075, 0.07, 0.085), (0.058, 0.09, 0.1),
                      (0.068, -0.03, 0.07), (0.05, -0.05, 0.065), (0.085, 0.03, 0.05)]) + H * np.array([0, 1, 1])
        k.add(f"crest{sd}", hull(Y * m), YELLOW, "head", fuse=False, surface="shell")
        spike(k, f"crown{sd}", (H + (0.035, 0.03, 0.05)) * m, np.array((0.35, 0.6, 0.75)) * m, 0.12, 0.06, 0.035, "head")
        spike(k, f"cheek{sd}", (H + (0.065, 0.02, -0.01)) * m, np.array((0.7, 0.65, 0.1)) * m, 0.08, 0.05, 0.03, "head",
              color=ICE2)
    spike(k, "crownmid", H + (0, 0.04, 0.06), (0, 0.55, 0.85), 0.1, 0.06, 0.035, "head", up=(1, 0, 0))


# --- the eyes: yellow, a black slit-round pupil, under the ice's brow ---------------------------------------------
OUT = [(-1, -0.3), (-0.45, -0.75), (0.5, -0.7), (1.1, -0.1), (0.85, 0.3), (0.0, 0.35), (-0.7, 0.15)]
EYE_OUT = polygon(OUT)
CY, CK, CW, LID = srgb(YELLOW), srgb(INKC), srgb("#FFFFFF"), srgb(ICE)


def eye_open(u, v):
    c = np.tile(CY, (len(u), 1))
    c[((u + 0.05) / 0.28) ** 2 + ((v + 0.15) / 0.42) ** 2 < 1] = CK
    c[((u + 0.32) / 0.14) ** 2 + ((v - 0.02) / 0.14) ** 2 < 1] = CW
    frac = np.array([math.hypot(a, b) / EYE_OUT(math.atan2(b, a)) for a, b in zip(u, v)])
    c[frac > 0.84] = CK
    return c


DE = unit(0.75, -0.55, 0.2)


def eyes(k, state):
    E = on_ell(H + (0, 0.01, 0.01), HR * 1.0, DE) + np.array([-0.004, -0.012, -0.005])
    look = eye_look(state, eye_open, LID, CK, line=0.2)
    m = np.array([-1, 1, 1.0])
    k.decal(E, DE, (0.037, 0.027), look, eps=0.005, rings=20, segs=56, outline=EYE_OUT)
    k.decal(E * m, DE * m, (0.037, 0.027), lambda u, v: look(-u, v), eps=0.005, rings=20, segs=56,
            outline=lambda th: EYE_OUT(math.pi - th))


def kyurem(src, out):
    k = Kit(src, coat="shell")
    body(k)
    legs(k)
    wings(k)
    tail(k)
    head(k)
    k.build(voxel=0.0068, smooth=4, band=0.005)
    k.eye_states(lambda state: eyes(k, state))
    k.export(out)


if __name__ == "__main__":
    kyurem(*args())
