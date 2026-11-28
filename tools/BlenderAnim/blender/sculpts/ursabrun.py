# Ursabrun (Carpathia No. 10054) sculpted from nothing on the skeleton and clips of the game's own export of it
# (sculptkit.py), grown from Urslet (sibling("urslet")): its round ear with the dark lining, its claw, its honey
# crescent painted round the body's axis, its eye's colours and glints, its palette's cream and honey.
#   blender --background --factory-startup --python sculpts/ursabrun.py -- <the export's Ursabrun.glb> <out.glb>
#
# The design, from the project's picture (docs/art/reference/gemini/carpathia/10054-Ursabrun.jpg): a massive brown bear
# on four legs, the head big and carried low and forward; a great hump of muscle over the shoulders and over it a
# grizzled silver mane from behind the ears down the back, swept back in shaggy locks; the legs darker brown from the
# elbow and the hock down, thick and short, the shoulders and haunches broad masses fused into the body; long curved
# stone-grey claws, five on each forepaw, four behind; a pale tan muzzle running up the bridge between the eyes, a big
# rounded russet nose, a straight mouth; small round ears; deep-set calm eyes under a heavy dark brow; a pale honey
# crescent on the chest under the chin; patches of green moss and a few pine sprigs on the shoulders, back and a
# foreleg; a small round tail.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, blade, outward, polygon, srgb, inside, unit, boxy, tri, eye_look, args, sibling

u_ = sibling("urslet")

BROWN, LEGS, BROW, TAN, MOON, NOSE = "#7B5032", "#47291A", "#432a1b", "#E9D3A6", "#E7C688", "#A85E34"
GREY, SILVER, SHADE, CLAW = "#8C8A88", "#C4C2BE", "#6E6A67", "#9A9A9F"
MOSS, MOSSDARK, PINE = "#6F8F34", "#536F27", "#3C6A2A"
INK, MOUTH = u_.INK, "#2E1C12"

H = np.array([0.0, -0.47, 0.62])           # the head's middle (its bone at 0, -0.32, 0.62)
HR = np.array([0.154, 0.146, 0.138])
BODYPARTS = ["trunk", "belly", "hump", "chest", "rump", "shoulder", "fore", "haunch", "shin", "neck"]


def body(k):
    k.add("trunk", ell((0, 0.0, 0.5), (0.235, 0.42, 0.25)), BROWN, "body")
    k.add("belly", ell((0, 0.0, 0.35), (0.205, 0.33, 0.175)), BROWN, "body")
    k.add("hump", ell((0, -0.15, 0.69), (0.215, 0.23, 0.17)), BROWN, "body")
    k.add("chest", ell((0, -0.28, 0.48), (0.21, 0.165, 0.21)), BROWN, "body")
    k.add("rump", ell((0, 0.3, 0.52), (0.2, 0.16, 0.2)), BROWN, "body")
    k.add("neck", ell((0, -0.34, 0.6), (0.16, 0.14, 0.15)), BROWN, "head")
    k.add("tail", ell((0, 0.45, 0.56), (0.05, 0.045, 0.045)), BROWN, "tail")
    # The pale honey crescent on the chest under the chin, its horns up
    k.paint(u_.crescent(np.array([0, -0.28, 0]), 0.2, 0.41, 0.085, 0.82, 0.04, -0.36), MOON, on=["chest", "trunk"])


MC_, MR_ = np.array([0.0, -0.18, 0.78]), np.array([0.17, 0.22, 0.085])     # the mane's main mass


# The locks of the ruff: root, the way it points, its length, its radius at the root. The front ones rise behind the
# ears and fall to either side of the neck, the ones over the hump are the longest, all swept well back.
LOCKS = [((0.0, -0.41, 0.75), (0.0, 0.7, 0.7), 0.19, 0.06),
         ((0.075, -0.4, 0.73), (0.45, 0.7, 0.55), 0.17, 0.056),
         ((-0.075, -0.4, 0.73), (-0.45, 0.7, 0.55), 0.17, 0.056),
         ((0.125, -0.33, 0.69), (0.45, 0.55, -0.7), 0.13, 0.058),
         ((-0.125, -0.33, 0.69), (-0.45, 0.55, -0.7), 0.13, 0.058),
         ((0.0, -0.27, 0.82), (0.0, 0.8, 0.6), 0.27, 0.07),
         ((0.09, -0.24, 0.8), (0.4, 0.8, 0.45), 0.25, 0.066),
         ((-0.09, -0.24, 0.8), (-0.4, 0.8, 0.45), 0.25, 0.066),
         ((0.045, -0.12, 0.83), (0.15, 0.88, 0.45), 0.24, 0.064),
         ((-0.045, -0.12, 0.83), (-0.15, 0.88, 0.45), 0.24, 0.064),
         ((0.14, -0.15, 0.76), (0.7, 0.65, -0.2), 0.18, 0.058),
         ((-0.14, -0.15, 0.76), (-0.7, 0.65, -0.2), 0.18, 0.058),
         ((0.06, 0.02, 0.79), (0.3, 0.92, 0.2), 0.18, 0.056),
         ((-0.06, 0.02, 0.79), (-0.3, 0.92, 0.2), 0.18, 0.056)]


def mane(k):
    """The grizzled silver ruff: a darker fused mass over the crown and the hump, and fourteen thick tapering locks
    standing out of it, dark at the roots and pale at the tips, each bending up clear of the mass at its point."""
    k.add("mane", ell(MC_, MR_), SHADE, "body")
    k.add("manenape", ell((0, -0.38, 0.73), (0.13, 0.08, 0.075)), SHADE, "body")
    for i, (root, d, length, r) in enumerate(LOCKS):
        root, d = np.array(root, float), unit(*d)
        lift = unit(*(np.array([0, 0, 1.0]) - d * d[2]))       # the way the tip curls up off the mass
        at = lambda t: root + d * length * t + lift * length * 0.22 * t * t
        dark = [at(t) for t in (-0.1, 0.15, 0.35, 0.55)]
        pale = [at(t) for t in (0.48, 0.65, 0.82, 1.0)]
        side = np.cross(d, lift)
        k.add(f"lockroot{i}", outward(blade(dark, [r * 0.85, r, r * 0.9, r * 0.72], lift, 0.45)), GREY, "body",
              fuse=False, surface="fur")
        k.add(f"locktip{i}", outward(blade(pale, [r * 0.76, r * 0.56, r * 0.3, r * 0.05], lift, 0.45)),
              SILVER, "body", fuse=False, surface="fur")


def head(k):
    HEAD = ell(H, HR, deform=boxy(2.3))
    k.add("head", HEAD, BROWN, "head")
    MUZ = ell(H + (0, -0.125, -0.045), (0.072, 0.08, 0.06))
    k.add("muzzle", MUZ, TAN, "head")
    k.add("jaw", ell(H + (0, -0.095, -0.09), (0.066, 0.075, 0.042)), TAN, "head")
    # heavy brows over deep-set eyes
    for s, nm in ((1, "R"), (-1, "L")):
        k.add("brow" + nm, ell(H + (0.066 * s, -0.13, 0.04), (0.058, 0.03, 0.026), rot=(0, 18 * s, 0)), BROWN, "head")
    # the tan running up the bridge between the eyes, the cheeks tan below
    def bridge(P):
        q = P - H
        w = 0.024 + 0.25 * np.clip(0.02 - q[:, 2], 0, 1)
        return inside(np.abs(q[:, 0]) - w, 0.004) * inside(q[:, 2] - 0.07, 0.004) * (q[:, 1] < -0.09)
    k.paint(bridge, TAN, on=["head", "muzzle", "brow"])
    # a dark socket under each brow, round each eye
    def socket(P):
        q = P - H
        d = np.hypot((np.abs(q[:, 0]) - 0.064) / 0.052, (q[:, 2] - 0.022) / 0.034)
        return inside(d - 1, 0.08) * (q[:, 1] < -0.06)
    k.paint(socket, BROW, on=["head", "brow"])
    # small round ears high on the head, darker inside
    u_.round_ear(k, (0.102, -0.41, 0.72), (0.047, 0.027, 0.044), (0.3, -1, 0.15), "ear{s}", BROWN, u_.LINING,
                 (0.025, 0.025))
    # the big rounded russet nose, a straight mouth under it
    NC = u_.hit(MUZ, H + (0, -0.6, 0.0), (0, 1, 0))
    k.add("nose", ell(NC + (0, 0.008, 0.002), (0.036, 0.024, 0.026)), NOSE, "head", fuse=False, surface="skin")
    def mouth(uu, v):
        c = np.tile(srgb(TAN), (len(uu), 1))
        c[(np.abs(v - (0.1 - 0.35 * uu * uu)) < 0.3) & (np.abs(uu) < 0.88)] = srgb(MOUTH)
        return c
    MC = u_.hit(MUZ, H + (0, -0.6, -0.085), (0, 1, 0))
    k.decal(MC, unit(0, -1, -0.2), (0.05, 0.016), mouth, eps=0.004, rings=10, segs=56)
    return HEAD


def legs(k):
    for s, nm in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add("shoulder" + nm, ell(np.array((0.17, -0.22, 0.45)) * m, (0.12, 0.15, 0.21)), BROWN, "leg" + nm)
        k.add("fore" + nm, tube([np.array(p) * m for p in [(0.18, -0.25, 0.36), (0.2, -0.21, 0.23),
                                                           (0.225, -0.31, 0.11), (0.232, -0.345, 0.075)]],
                                [0.098, 0.09, 0.084, 0.08]),
              BROWN, "leg" + nm)
        k.add("pawf" + nm, ell(np.array((0.235, -0.37, 0.046)) * m, (0.088, 0.105, 0.045)), LEGS, "leg" + nm)
        for i in range(5):
            o = (i - 2) * 0.034
            base = np.array((0.235 + o, -0.455 + 0.012 * abs(i - 2), 0.042)) * m
            u_.claw(k, f"clawf{nm}{i}", base, (0.12 * (i - 2) * s, -1, -0.28), 0.075, 0.013, "leg" + nm, CLAW,
                    curl=0.22)
    for s, nm in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add("haunch" + nm, ell(np.array((0.165, 0.25, 0.4)) * m, (0.13, 0.18, 0.23)), BROWN, "hind" + nm)
        k.add("shin" + nm, tube([np.array(p) * m for p in [(0.18, 0.27, 0.27), (0.186, 0.25, 0.095)]], [0.094, 0.086]),
              BROWN, "hind" + nm)
        k.add("pawh" + nm, ell(np.array((0.187, 0.2, 0.044)) * m, (0.084, 0.115, 0.043)), LEGS, "hind" + nm)
        for i in range(4):
            o = (i - 1.5) * 0.034
            base = np.array((0.187 + o, 0.09 + 0.01 * abs(i - 1.5), 0.04)) * m
            u_.claw(k, f"clawh{nm}{i}", base, (0.1 * (i - 1.5) * s, -1, -0.3), 0.05, 0.012, "hind" + nm, CLAW,
                    curl=0.2)
    # darker from the elbow and the hock down: a ragged line round each leg, by distance from its axis
    def dark(P):
        res = np.zeros(len(P))
        for (x, y), z0 in (((0.21, -0.27), 0.4), ((0.183, 0.26), 0.36)):
            d = np.hypot(np.abs(P[:, 0]) - x, P[:, 1] - y)
            ang = np.arctan2(P[:, 1] - y, np.abs(P[:, 0]) - x)
            edge = z0 + 0.018 * tri(ang * 9 / (2 * math.pi))
            res = np.maximum(res, inside(P[:, 2] - edge, 0.004) * inside(d - 0.16, 0.01))
        return res
    k.paint(dark, LEGS, on=["fore", "shin", "shoulder", "haunch", "pawf", "pawh"])


def moss(k):
    """Patches of green moss on the shoulders, the back and a foreleg, most on +X (the front camera's side), and a few
    pine sprigs caught in the coat."""
    patches = [((0.215, -0.12, 0.62), 0.07), ((0.2, -0.04, 0.68), 0.05), ((0.12, 0.17, 0.72), 0.06),
               ((0.235, -0.27, 0.27), 0.05), ((0.24, 0.06, 0.55), 0.045), ((-0.2, 0.08, 0.64), 0.06),
               ((-0.12, -0.2, 0.75), 0.045), ((0.05, 0.33, 0.66), 0.045)]
    def mask(P, inner=False):
        res = np.zeros(len(P))
        for j, (c, r) in enumerate(patches):
            q = P - np.array(c)
            ang = np.arctan2(q[:, 2], q[:, 1] + 1e-9)
            rr = r * (0.75 if inner else 1.0) * (1 + 0.22 * tri(ang * 3 / (2 * math.pi) + j * 0.3)
                                                    + 0.12 * tri(ang * 7 / (2 * math.pi)))
            res = np.maximum(res, inside(np.linalg.norm(q, axis=1) - rr, 0.004))
        return res
    k.paint(mask, MOSS, on=BODYPARTS)
    k.paint(lambda P: mask(P, True) * inside(np.sin(P[:, 0] * 90) * np.sin(P[:, 1] * 80) * np.sin(P[:, 2] * 70) - 0.2,
                                             0.05), MOSSDARK, on=BODYPARTS)
    for j, (c, d) in enumerate((((0.25, -0.06, 0.64), (1, 0.2, 0.6)), ((0.16, 0.2, 0.75), (0.6, 0.6, 0.6)),
                                ((-0.23, -0.16, 0.6), (-1, 0.1, 0.5)))):
        c, d = np.array(c, float), unit(*d)
        for n, tw in enumerate((-0.45, 0.0, 0.45)):
            dd = unit(*(d + np.cross(d, (0, 0, 1.0)) * tw))
            a = unit(*np.cross(dd, (0, 0, 1.0)))
            b = unit(*np.cross(a, dd))
            w = 0.015
            k.add(f"pine{j}{n}", hull([c - dd * 0.01 + a * w + b * w * 0.5, c - dd * 0.01 - a * w + b * w * 0.5,
                                       c - dd * 0.01 + a * w - b * w * 0.5, c - dd * 0.01 - a * w - b * w * 0.5,
                                       c + dd * 0.07]), PINE, "body", fuse=False, surface="leaf")


# --- the eyes: deep-set and calm, Urslet's dark brown iris and glints, a white showing, the brow's edge flat over them
CW, CI, CK, CG, CB = srgb("#F1ECE2"), srgb("#5A3622"), srgb(INK), srgb("#FFFFFF"), srgb(BROW)
OUT = [(-1.0, 0.1), (0.0, 0.62), (1.0, 0.7), (0.95, -0.3), (0.55, -0.85), (0.0, -1.0), (-0.55, -0.85), (-0.95, -0.4)]


def open_eye(uu, v):
    c = np.tile(CW, (len(uu), 1))
    c[((uu + 0.02) / 0.6) ** 2 + ((v + 0.12) / 0.75) ** 2 < 1] = CI
    c[((uu + 0.02) / 0.3) ** 2 + ((v + 0.12) / 0.38) ** 2 < 1] = CK
    c[((uu - 0.18) / 0.17) ** 2 + ((v - 0.12) / 0.17) ** 2 < 1] = CG
    edge = 0.12 + 0.34 * uu                         # the brow's line, down toward the nose (u outward)
    c[v > edge - 0.16] = CK                         # its dark edge
    c[v > edge] = CB                                # the brow itself over the eye's top third
    return c


def eyes(k, HEAD):
    de = unit(0.44, -0.88, 0.14)
    E = u_.hit(HEAD, H + (0.063, -0.6, 0.018), (0, 1, 0))
    def lay(state):
        if state == "Fierce":
            def look(uu, v):
                c = open_eye(uu, v)
                c[v > -0.16 + 0.6 * uu] = CK
                c[v > 0.0 + 0.6 * uu] = CB
                return c
        else:
            look = eye_look(state, open_eye, CB, CK, line=0.2)
        for s in (1, -1):
            shape = polygon([(a * s, b) for a, b in OUT])
            col = look if s > 0 else (lambda uu, v, f=look: f(-uu, v))
            k.decal(E * np.array([s, 1, 1]), de * np.array([s, 1, 1]), (0.036, 0.03), col, eps=0.003,
                    outline=shape, rings=20, segs=56)
    return lay


def ursabrun(src, out):
    k = Kit(src, coat="fur")
    body(k)
    mane(k)
    HEAD = head(k)
    legs(k)
    moss(k)
    k.build(voxel=0.0098, band=0.007, wband=0.07)
    u_.board(k.ob)
    k.eye_states(eyes(k, HEAD))
    k.export(out)


if __name__ == "__main__":
    ursabrun(*args())
