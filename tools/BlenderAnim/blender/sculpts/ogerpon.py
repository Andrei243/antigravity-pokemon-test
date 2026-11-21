# Ogerpon (the Teal Mask) sculpted from nothing on the skeleton and clips of the game's own export of it
# (sculptkit.py):
#   blender --background --factory-startup --python sculpts/ogerpon.py -- <the export's Ogerpon.glb> <out.glb>
# Written to be grown from (the other masks are ogerpon(src, out, mask=...) with another mask function). From the
# references: a little ogre in a dark hood with two pointed ears and a green sprout on its crown; a round orange face
# in the hood's opening, big yellow eyes with a white four-pointed star in each, three dark freckles on each cheek,
# a small mouth with one white fang; a green leafy tunic falling to a hem of leaf points, a yellow button high on its
# right and another low, a white flower on its left; two great rounded green flaps from the hood's sides drooping out
# and down like sleeves, pale green along their lower edges; long dark legs with two little green leaves at each foot;
# dark arms, the left raised to hold the mask beside its head. The mask: a black face with sparkling crescent eyes and
# a grin of cream teeth, a brown beard under it, a crown of teal spikes with a white star, green leaves round it.
# The pictures overrule the task's text: the mask is held up beside the head on its left (-X), not worn on the face,
# and the face is Ogerpon's own (the game's sculptor tips it over that side too).
# Reference ratios (3D picture, to the ears' tips): hood head (without ears) / height 0.37 (0.36 here); face / head
# width 0.72 (0.7 here); mask height / head height 1.0 (0.95 here, its spikes added); legs under the hem / height
# 0.34 (0.33 here); flaps reach out to x +-0.27 (the picture's right one to 0.4 of the height out, drawn in here so
# the model is not wider than it is tall).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, hull, slab, leaf, outward, srgb, inside, unit, axes, boxy, polygon, eye_look,
                       args, sibling)

lt = sibling("litten")

DARK = "#2F3236"
GREEN = "#4EA63A"
LIGHT = "#86CE5E"
FLAP, FLAPLIGHT = "#3E9C3C", "#8AD27C"
ORANGE = "#F29A2C"
FRECKLE = "#7A4A1E"
YELLOW = "#F4D43A"
IRIS, INK = "#E8A21E", "#2A1A10"
WHITE = "#F6F6F2"
TEAL, TEALDEEP = "#2EA8A0", "#1E7C78"
MASKFACE, TEETH, BEARD = "#1C1C21", "#F0E2A4", "#C47E40"

H = np.array([0.0, 0.0, 0.555])
HR = np.array([0.1, 0.092, 0.118])
FACE = np.array([0.0, -0.05, 0.51])
FR = np.array([0.074, 0.05, 0.074])
MASK = np.array([-0.215, -0.075, 0.585])      # the mask's middle, held up beside the head on the left


def add_body(k):
    k.add("tunic", ell((0, 0.0, 0.37), (0.1, 0.084, 0.102)), GREEN, "body")
    k.add("skirt", ell((0, 0.0, 0.31), (0.112, 0.094, 0.05)), GREEN, "body")
    for i in range(10):
        a = 2 * math.pi * (i + 0.5) / 10
        s_, c_ = math.sin(a), math.cos(a)
        root = np.array([0.096 * s_, -0.08 * c_, 0.3])
        out = np.array([s_, -c_, 0.0])
        k.add(f"hem{i}", lt.spike(root, out * 0.35 + (0, 0, -1.0), 0.05, 0.032, 0.014), GREEN if i % 2 else LIGHT,
              "body", fuse=False, surface="leaf")
    k.add("button", outward(ell((0.045, -0.082, 0.4), (0.016, 0.01, 0.016), sub=3)), YELLOW, "body", fuse=False,
          surface="plastic")
    k.add("button2", outward(ell((0.08, -0.055, 0.33), (0.013, 0.01, 0.013), sub=3)), YELLOW, "body", fuse=False,
          surface="plastic")
    # the white flower on its left breast: five petals
    star = [(0.9 * math.cos(math.pi / 2 + i * math.pi / 5) * (1 if i % 2 == 0 else 0.42),
             0.9 * math.sin(math.pi / 2 + i * math.pi / 5) * (1 if i % 2 == 0 else 0.42)) for i in range(10)]
    k.decal((-0.042, -0.084, 0.37), unit(-0.35, -1, 0.05), (0.022, 0.022), WHITE, eps=0.0018,
            outline=polygon(star), rings=8, segs=60)


def add_legs(k):
    k.add("leg", tube([(0.042, 0.0, 0.32), (0.048, -0.004, 0.17), (0.052, -0.008, 0.05)], [0.034, 0.03, 0.027]),
          DARK, "leg{s}", mirror=True)
    k.add("foot", ell((0.054, -0.025, 0.026), (0.032, 0.05, 0.027)), DARK, "leg{s}", mirror=True)
    for j, (d, col) in enumerate((((0.4, -0.6, 0.7), GREEN), ((-0.3, -0.7, 0.65), LIGHT))):
        k.add(f"footleaf{j}", outward(leaf((0.054, -0.05, 0.045), d, 0.04, 0.014, thick=0.008)), col, "leg{s}",
              fuse=False, mirror=True, surface="leaf")


def add_arms(k):
    # the right arm down at its side under the flap, the left raised to hold the mask
    s, e, w = np.array((0.065, -0.005, 0.455)), np.array((0.1, -0.03, 0.38)), np.array((0.095, -0.06, 0.31))
    k.add("armR", tube([s, e, w], [0.022, 0.02, 0.019]), DARK, "armR")
    k.add("handR", ell(w + (0, -0.008, -0.012), (0.022, 0.022, 0.022)), DARK, "armR")
    s, e, w = np.array((-0.065, -0.005, 0.455)), np.array((-0.125, -0.04, 0.43)), np.array((-0.165, -0.075, 0.5))
    k.add("armL", tube([s, e, w], [0.022, 0.02, 0.019]), DARK, "armL")
    k.add("handL", ell(w, (0.022, 0.022, 0.022)), DARK, "armL")


def add_head(k):
    k.add("hood", ell(H, HR), DARK, "head")
    k.add("face", ell(FACE, FR), ORANGE, "head")
    # the hood's two pointed ears and the green sprout on its crown
    for s in (1, -1):
        b = np.array((0.05 * s, 0.0, 0.645))
        tip = np.array((0.085 * s, 0.01, 0.775 if s > 0 else 0.745))
        k.add("hoodear" + ("R" if s > 0 else "L"), hull([b + (0.032 * s, 0, 0), b + (-0.03 * s, 0, 0.012),
                                                          b + (0, -0.03, 0), b + (0, 0.03, 0), tip]), DARK, "head",
              fuse=False, surface="cloth")
    k.add("stem", tube([(-0.01, 0.0, 0.66), (-0.016, -0.004, 0.715)], [0.011, 0.01]), GREEN, "head")
    k.add("sprout", outward(ell((-0.018, -0.006, 0.73), (0.02, 0.016, 0.024), sub=3)), GREEN, "head", fuse=False,
          surface="leaf")
    # three dark freckles on each cheek
    for s in (1, -1):
        for x, z in ((0.04, 0.488), (0.052, 0.498), (0.03, 0.494)):
            p, n = lt.on_head(x * s, z, FACE, FR)
            k.decal(p, n, (0.0055, 0.0055), FRECKLE, eps=0.0015, rings=4, segs=20)
    # a small mouth with one white fang
    CI, CW = srgb(INK), srgb(WHITE)
    def mouth(u, v):
        c = np.tile(CI, (len(u), 1))
        c[(u > 0.1) & (u < 0.55) & (v < 0.1 - 1.5 * np.abs(u - 0.32))] = CW
        return c
    p, n = lt.on_head(0.0, 0.462, FACE, FR)
    k.decal(p, n, (0.024, 0.012), mouth, eps=0.0015, rings=8, segs=40,
            outline=polygon([(-1, 0.6), (1, 0.6), (0.8, -0.2), (0.3, -0.9), (-0.3, -0.7), (-0.8, -0.2)]))


def add_flaps(k):
    """The two great rounded green flaps from the hood's sides, drooping out and down, pale along their lower edges."""
    for s in (1, -1):
        c = np.array((0.165 * s, 0.035, 0.47))
        k.add("flap" + ("R" if s > 0 else "L"), ell(c, (0.115, 0.05, 0.085), rot=(0, -28 * s, 22 * s),
                                                    deform=boxy(2.4)), FLAP, "head")
        k.add("flaproot" + ("R" if s > 0 else "L"), ell((0.085 * s, 0.025, 0.53), (0.05, 0.05, 0.06)), FLAP, "head")
    def pale(P):
        x, y, z = np.abs(P[:, 0]), P[:, 1], P[:, 2]
        edge = 0.42 - 0.35 * (x - 0.12)
        return inside(z - edge, 0.003) * (x > 0.08)
    k.paint(pale, FLAPLIGHT, on=["flap"])


# --- the mask, held up beside the head on its left -------------------------------------------------------------------
MN = unit(0.35, -1, 0.0)                     # the way the mask looks: forward and a little in


MS = 1.22                                    # the mask as large as the head, as the pictures draw it


def add_mask(k):
    a, b = axes(MN)                          # a across the mask, b up it
    def at(x, y, z):
        return MASK + np.array((x, y, z)) * MS
    def r(*v):
        return tuple(np.array(v) * MS)
    k.add("maskface", ell(MASK, r(0.066, 0.03, 0.068), rot=(0, 0, -19)), MASKFACE, "armL")
    k.add("beard", ell(at(0.005, 0.01, -0.07), r(0.04, 0.025, 0.04)), BEARD, "armL")
    k.add("maskback", ell(at(0.0, 0.022, 0.0), r(0.07, 0.025, 0.074), rot=(0, 0, -19)), TEAL, "armL")
    # the crown of teal spikes, a fan over the top
    for i, ang in enumerate(np.radians(np.linspace(-70, 70, 6))):
        d = unit(math.sin(ang) * 0.9 + 0.1, 0.05, math.cos(ang))
        root = at(0.0, 0.01, 0.045) + d * 0.03 * MS
        col = TEAL if i % 2 == 0 else TEALDEEP
        k.add(f"crown{i}", lt.spike(root, d, (0.085 + 0.02 * (i in (2, 3))) * MS, 0.032 * MS, 0.02 * MS), col, "armL",
              fuse=False, surface="shell")
    k.add("crownbase", outward(ell(at(0, 0.008, 0.055), r(0.068, 0.034, 0.04), sub=3)), TEAL, "armL",
          fuse=False, surface="shell")
    # the white star on the crown
    star = [(math.cos(math.pi / 2 + i * math.pi / 5) * (1 if i % 2 == 0 else 0.45),
             math.sin(math.pi / 2 + i * math.pi / 5) * (1 if i % 2 == 0 else 0.45)) for i in range(10)]
    sc = at(0.0, -0.03, 0.075)
    k.add("maskstar", slab([(x * 0.024 * MS, y * 0.024 * MS) for x, y in star], sc - MN * 0.012, a,
                           np.array((0, 0, 1.0)), 0.01), WHITE, "armL", fuse=False, surface="shell")
    # green leaves round the mask's sides and below, each with a pale middle
    for i, ang in enumerate(np.radians((-115, -150, 175, 140, 105, 60))):
        d = unit(math.cos(ang) * 0.95, 0.15, math.sin(ang))
        root = at(math.cos(ang) * 0.055, 0.015, math.sin(ang) * 0.055)
        k.add(f"maskleaf{i}", outward(leaf(root, d, 0.09 * MS, 0.03 * MS, thick=0.012)), FLAP, "armL", fuse=False,
              surface="leaf")
        k.add(f"maskvein{i}", outward(leaf(root + d * 0.012 - MN * 0.005, d, 0.06 * MS, 0.009 * MS, thick=0.014)),
              FLAPLIGHT, "armL", fuse=False, surface="leaf")


def mask_face(k):
    """Sparkling crescent eyes and a grin of cream teeth on the mask's black face."""
    CW, CY, CM = srgb(WHITE), srgb("#E9EF9C"), srgb(MASKFACE)
    def eye(u, v):
        c = np.tile(CY, (len(u), 1))
        c[(np.abs(u - 0.2 * v) < 0.16)] = CW
        return c
    crescent = polygon([(-1, -0.3), (-0.5, 0.38), (0.5, 0.38), (1, -0.3), (0.5, -0.06), (-0.5, -0.06)])
    for s in (1, -1):
        c = MASK + np.array((0.04 * s, -0.03, 0.02)) * MS
        k.decal(c, MN, (0.032, 0.032), eye, eps=0.0018, outline=crescent, rings=8, segs=48, up=(0, 0, 1))
    def grin(u, v):
        c = np.tile(srgb(TEETH), (len(u), 1))
        zig = 0.05 + 0.4 * lt_tri(u * 1.6)
        c[np.abs(v - zig) < 0.2] = CM
        return c
    k.decal(MASK + np.array((0.0, -0.03, -0.024)) * MS, MN, (0.064, 0.028), grin, eps=0.0018, rings=10, segs=60,
            outline=polygon([(-1, 0.7), (1, 0.7), (0.85, -0.6), (-0.85, -0.6)]))


def lt_tri(t):
    return 2 * np.abs(2 * (t - np.floor(t + 0.5))) - 1


# --- the eyes: big and yellow, a white star in each --------------------------------------------------------------------
def eye_colour():
    CY, CI, CA, CW = srgb(YELLOW), srgb(INK), srgb(IRIS), srgb(WHITE)
    def eye(u, v):
        r = np.hypot(u, v)
        t = np.clip((0.3 - v) / 1.2, 0, 1)[:, None]
        c = CY[None] * (1 - t) + CA[None] * t
        # a four-pointed star
        star = (np.abs(u) ** 0.6 + np.abs(v) ** 0.6) < 0.62 ** 0.6 * 1.25
        c[star] = CW
        c[r > 0.84] = CI
        return c
    return eye


def lay_eyes(k, state):
    look = eye_look(state, eye_colour(), srgb(ORANGE), srgb(INK), line=0.17)
    for s in (1, -1):
        p, n = lt.on_head(0.031 * s, FACE[2] + 0.014, FACE, FR)
        n = unit(n[0] * 1.15, n[1], n[2])
        k.decal(p, n, (0.025, 0.026), look if s > 0 else (lambda u, v: look(-u, v)), eps=0.0015, rings=20, segs=56)


def ogerpon(src, out_path, mask=None, face=None):
    k = Kit(src, coat="leaf")
    add_body(k)
    add_legs(k)
    add_arms(k)
    add_head(k)
    add_flaps(k)
    (mask or add_mask)(k)
    (face or mask_face)(k)
    k.build(voxel=0.0055, smooth=4, band=0.004, wband=0.04)
    k.eye_states(lambda s: lay_eyes(k, s))
    k.export(out_path)


if __name__ == "__main__":
    ogerpon(*args())
