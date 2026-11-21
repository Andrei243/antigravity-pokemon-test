# Slither Wing sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/slitherwing.py -- <the export's Slither Wing.glb> <out.glb>
# A paradox of Volcarona's line: larvesta.py's white fluff (sibling("larvesta")) and volcarona.py's flame-shaped wing
# plates (sibling("volcarona")) are borrowed; the body is built anew.
#
# From the references: a great white shaggy body crouched low and long, jagged tufts all over, its belly on the
# ground; short furry legs with dark navy pointed claws peeping out under the fur. The head is carried low in front:
# a navy face pointing down like a beak, two navy prongs of a jaw under it, hooded in red-orange plates with points
# sweeping back; one big yellow eye a side ringed black with a black X across it. Six wings rise from the back, three
# a side: a tall flame-shaped one up and back, a middle one out and back, a short low one; red at the root going
# orange then yellow at the tip, lime diamonds on them. Behind, a teal tail of segments with green spines along its top.
# (The task's text expected a biped on thick legs with the wings behind: the pictures show this crouched, many-legged
# beast with the head low in front, which the skeleton also is: the pictures are followed.)
# Bones: body (0, 0, 0.4), head (0, -0.12, 0.41), leg{s} (0.07, -0.08, 0.36), hind{s} (0.07, 0.064, 0.36),
# wing{s} (0.04, 0.02, 0.49), tail (0, 0.11, 0.39).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, slab, outward, srgb, inside, unit, eye_look, args, sibling

lv = sibling("larvesta")
vc = sibling("volcarona")

WHITE, NAVY, HOOD = "#F1F0EC", "#2A3366", "#E2462E"
RED, ORANGE, AMBER, LIME = "#DD3E28", "#EE7A2A", "#F3C24A", "#CFE668"
TEAL, BAND, SPINE = "#2E8C7C", "#1F5E58", "#8CC456"
YELLOW, INK = "#F3D63C", "#12121A"

BODY = np.array([0.0, 0.03, 0.235])
BODYR = np.array([0.215, 0.27, 0.215])
FACE = np.array([0.0, -0.29, 0.215])
FACER = np.array([0.084, 0.082, 0.1])
VOXEL = 0.0068


def body(k):
    k.add("fur", ell(BODY, BODYR, deform=lv.fluff(teeth=14, rows=3.2, amp=0.34, keep_front=False), sub=5), WHITE,
          "body")
    # the fur of the shoulders hunched over the neck, coming forward round the hood
    k.add("ruff", ell((0, -0.17, 0.3), (0.15, 0.1, 0.12), deform=lv.fluff(teeth=9, rows=2.4, amp=0.3,
                                                                          keep_front=False)), WHITE, "body")
    for bone, y, x in (("leg{s}", -0.16, 0.14), ("hind{s}", 0.17, 0.15)):
        k.add("leg" + bone[0], ell((x, y, 0.075), (0.075, 0.08, 0.085),
                                   deform=lv.fluff(teeth=7, rows=2.0, amp=0.25, keep_front=False)), WHITE, bone,
              mirror=True)
        for j, dx in enumerate((-0.022, 0.022)):
            base = np.array([x + dx, y - 0.04, 0.03])
            tip = base + (0.006 if dx > 0 else -0.004, -0.06, -0.026)
            pts = [base + (-0.016, 0.0, 0.0), base + (0.016, 0.0, 0.0), base + (0, 0.012, 0.022),
                   base + (0, -0.01, -0.012), tip]
            k.add(f"claw{bone[0]}{j}", hull(pts), NAVY, bone, fuse=False, mirror=True, surface="shell")


def head(k):
    k.add("face", ell(FACE, FACER, rot=(-28, 0, 0)), NAVY, "head")
    k.add("snout", ell(FACE + (0, -0.045, -0.07), (0.052, 0.05, 0.062), rot=(-30, 0, 0)), NAVY, "head")
    # the red-orange hood over the top and back of the head
    k.add("hood", ell(FACE + (0, 0.035, 0.05), (0.113, 0.1, 0.098), rot=(-20, 0, 0)), HOOD, "head")
    # the jaw's two navy prongs
    for s in (1, -1):
        b = FACE + (0.028 * s, -0.07, -0.105)
        k.add(f"jaw{'R' if s > 0 else 'L'}", hull([b + (-0.016, 0.01, 0.02), b + (0.016, 0.01, 0.02),
                                                   b + (0, 0.03, 0.025), b + (0, -0.012, 0.012),
                                                   b + (0.012 * s, -0.035, -0.05)]), NAVY, "head", fuse=False,
              surface="shell")
    # the hood's points sweeping back: a crest down the middle and three a side
    for i, (o, d, L) in enumerate(((np.array([0.0, 0.02, 0.135]), unit(0, 0.75, 0.66), 0.09),
                                   (np.array([0.075, 0.0, 0.1]), unit(0.5, 0.6, 0.62), 0.085),
                                   (np.array([0.095, 0.03, 0.04]), unit(0.7, 0.68, 0.2), 0.08),
                                   (np.array([0.085, 0.03, -0.03]), unit(0.62, 0.7, -0.35), 0.07))):
        mirror = i > 0
        base = FACE + o
        a = np.cross(d, (0, 0, 1.0)) if abs(d[2]) < 0.9 else np.array([1.0, 0, 0])
        a /= np.linalg.norm(a)
        up = np.cross(a, d)
        pts = [base + a * 0.024, base - a * 0.024, base + up * 0.016, base - up * 0.012, base + d * L]
        k.add(f"point{i}", hull(pts), HOOD, "head", fuse=False, mirror=mirror, surface="shell")


# --- the wings: flame-shaped plates, red at the root, orange, yellow at the tip, lime diamonds
WINGS = [  # name, along, the face's leaning, length, half width, curl, diamonds (t, s)
    ("upper", unit(0.28, 0.42, 0.86), unit(0.9, 0.45, 0), 0.48, 0.118, -0.16, [(0.42, 0.2), (0.58, -0.3), (0.72, 0.15)]),
    ("middle", unit(0.62, 0.55, 0.56), unit(0.2, 0.9, 0.3), 0.34, 0.09, -0.12, [(0.45, 0.2), (0.65, -0.25)]),
    ("lower", unit(0.82, 0.55, 0.15), unit(0.2, 0.9, 0.3), 0.26, 0.072, 0.1, [(0.5, 0.1)]),
]


def diamond(ca, cb, r):
    return np.array([(ca - r, cb), (ca, cb - r * 0.62), (ca + r, cb), (ca, cb + r * 0.62)])


def wings(k):
    for name, d, n0, L, W, curl, spots in WINGS:
        n = n0 - d * (d @ n0); n /= np.linalg.norm(n)
        across = np.cross(n, d)
        origin = np.array([0.055, 0.04, 0.43]) + d * 0.02
        base, at = vc.petal(L, W, curl)
        k.add(name, slab(base, origin, d, across, 0.012), RED, "wing{s}", fuse=False, mirror=True, surface="leaf")
        mid, _ = vc.petal(L, W, curl, 0.42, 0.999)
        k.add(name + "orange", slab(mid, origin, d, across, 0.016), ORANGE, "wing{s}", fuse=False, mirror=True,
              surface="leaf")
        tip, _ = vc.petal(L, W, curl, 0.74, 0.999)
        k.add(name + "amber", slab(tip, origin, d, across, 0.02), AMBER, "wing{s}", fuse=False, mirror=True,
              surface="leaf")
        for i, (t, s) in enumerate(spots):
            ca, cb = at(t, s)
            k.add(f"{name}spot{i}", slab(diamond(ca, cb, 0.026), origin, d, across, 0.024), LIME, "wing{s}",
                  fuse=False, mirror=True, surface="leaf")


# --- the tail: teal segments going back, green spines along its top
TAIL = [((0.0, 0.27, 0.13), 0.075), ((0.02, 0.34, 0.115), 0.066), ((0.045, 0.41, 0.105), 0.056),
        ((0.07, 0.47, 0.1), 0.045), ((0.09, 0.52, 0.098), 0.034)]


def tail(k):
    for i, (c, r) in enumerate(TAIL):
        k.add(f"tail{i}", ell(c, (r, r * 0.85, r * 0.92)), TEAL, "tail")
    cs = np.array([c for c, _ in TAIL])
    def bands(P):
        m = np.zeros(len(P))
        for a, b in zip(cs[:-1], cs[1:]):
            mid = (a + b) / 2
            d = b - a; d /= np.linalg.norm(d)
            m = np.maximum(m, inside(np.abs((P - mid) @ d) - 0.004, 0.002) * inside(np.linalg.norm(P - mid, axis=1) - 0.085, 0.003))
        return m
    k.paint(bands, BAND, on=["tail"])
    for i, (c, r) in enumerate(TAIL[1:]):
        base = np.array(c) + (0, 0, r * 0.82)
        d = unit(0.1, 0.5, 0.86)
        pts = [base + (0.014, 0, -0.01), base + (-0.014, 0, -0.01), base + (0, 0.022, -0.01), base + (0, -0.014, -0.01),
               base + d * (0.06 - 0.008 * i)]
        k.add(f"spine{i}", hull(pts), SPINE, "tail", fuse=False, surface="shell")


# --- the eye: yellow, ringed black, a black X across it
CY, CK, CN = srgb(YELLOW), srgb(INK), srgb(NAVY)


def eye_open(u, v):
    c = np.tile(CY, (len(u), 1))
    rr = u * u + v * v
    c[(np.abs(u - v) < 0.15) | (np.abs(u + v) < 0.15)] = CK
    c[rr > 0.8 ** 2] = CK
    return c


def slitherwing(src, out):
    k = Kit(src, coat="fur")
    body(k)
    head(k)
    wings(k)
    tail(k)
    k.build(voxel=VOXEL, smooth=4, band=0.004, wband=0.05)

    de = unit(0.7, -0.7, -0.04)
    R = np.array(FACER)
    ep = FACE + de / math.sqrt(((de / R) ** 2).sum())
    en = unit(0.6, -0.8, 0.02)

    def eyes(state):
        if state == "Fierce":
            def look(u, v):
                c = eye_open(u, v)
                e = 0.1 + 0.45 * u
                c[v > e + 0.1] = CN
                c[np.abs(v - e) <= 0.12] = CK
                return c
        else:
            look = eye_look(state, eye_open, CN, srgb("#8C96C8") if state != "Open" else CK, line=0.16)
        k.decal(ep, en, (0.046, 0.046), look, eps=0.002, rings=20, segs=56, mirror=True)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    slitherwing(*args())
