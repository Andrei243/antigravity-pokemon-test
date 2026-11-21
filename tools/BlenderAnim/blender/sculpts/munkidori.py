# Munkidori sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/munkidori.py -- <the export's Munkidori.glb> <out.glb>
# The second of the Loyal Three, its toxic chain Okidogi's (sibling("okidogi")). From the references: a monkey in a
# black hood of fur that falls round it like a long cloak to a ragged hem just over its feet; a bun of black hair on
# its crown with three points rising from it; a big pale blue face, oval, with a broad muzzle standing out under it,
# two nostrils and a sly smirk; heavy-lidded yellow eyes, the black lid lying over their top half; a pink chain round
# its brow like a circlet, ending at its right temple in a ring with a bar; long black sleeves, the right arm bent up
# with its blue hand at its temple, the left folded across its belly with the blue hand at the middle; a pale blue
# point on its chest under the chin; short blue legs with magenta cuffs and broad blue feet.
# The skeleton has one arm bone (armR); the left arm, folded across the body, is on the body's bone.
# Reference ratios (3D picture, to the hair's points): head with its bun / height 0.38 (0.39 here); face (blue)
# height / height 0.29 (0.28 here); cloak width / height 0.37 (0.37 here); head width / cloak width 0.76 (0.75 here).
# Arms: the right hand at the temple (x 0.11, y -0.06), the left hand at the middle of the belly (x 0.0, y -0.15,
# before the cloak's front at -0.11): neither held out.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, hull, ring_band, outward, srgb, inside, unit, boxy, polygon, eye_look, args,
                       sibling)

ok = sibling("okidogi")
lt = ok.lt

BLACK = "#2A2A33"
BLUE = "#8EA9E6"
PALE = "#B6C8F2"
PINK = "#DE4BBA"
CUFF = "#A9338F"
YELLOW, AMBER = "#F4D838", "#E0A825"
INK = "#15151C"
NOSTRIL = "#43507A"

H = np.array([0.0, -0.01, 0.565])
HR = np.array([0.112, 0.104, 0.124])
FACE = np.array([0.0, -0.058, 0.522])
FR = np.array([0.095, 0.066, 0.102])


def add_cloak(k):
    """The hood's fur falling round the body to a ragged hem just over the feet."""
    k.add("cloak", ell((0, 0.012, 0.31), (0.125, 0.1, 0.24)), BLACK, "body")
    k.add("skirt", ell((0, 0.015, 0.17), (0.142, 0.112, 0.15)), BLACK, "body")
    k.add("mantle", ell((0, 0.02, 0.47), (0.12, 0.1, 0.09)), BLACK, "body")
    k.add("hoodback", ell((0, 0.05, 0.5), (0.1, 0.075, 0.15)), BLACK, "head")
    # the pale blue point under the chin
    poly = lt.mirrored([(0.0, 0.415), (0.04, 0.425), (0.052, 0.4), (0.03, 0.33), (0.0, 0.26)])
    k.paint(lambda P: lt.poly_mask(P[:, 0], P[:, 2], poly, 0.002) * (P[:, 1] < -0.06), BLUE, on=["cloak", "mantle"])
    # the ragged hem: points hanging down all round
    for i in range(13):
        a = 2 * math.pi * (i + 0.3) / 13
        s_, c_ = math.sin(a), math.cos(a)
        root = np.array([0.122 * s_, 0.015 - 0.096 * c_, 0.085])
        out = np.array([s_, -c_, 0.0])
        k.add(f"hem{i}", lt.spike(root, out * 0.12 + (0, 0, -1.0), 0.055 + 0.015 * (i % 2), 0.03, 0.017), BLACK,
              "body", fuse=False, surface="fur")


def add_legs(k):
    k.add("leg", tube([(0.05, 0.005, 0.12), (0.062, -0.005, 0.04)], [0.032, 0.03]), BLUE, "leg{s}", mirror=True)
    k.add("foot", ell((0.066, -0.035, 0.022), (0.04, 0.06, 0.024)), BLUE, "leg{s}", mirror=True)
    for j, dx in enumerate((-0.022, 0.0, 0.022)):
        k.add(f"toe{j}", ell((0.066 + dx, -0.088, 0.018), (0.015, 0.02, 0.016)), BLUE, "leg{s}", mirror=True)
    k.add("cuff", ring_band((0.06, -0.003, 0.06), (0.012, -0.01, 1), 0.031, 0.026, thick=0.008), CUFF, "leg{s}",
          fuse=False, mirror=True, surface="fur")


def add_arms(k):
    """Long black sleeves: the right arm bent up to the temple, the left folded across the belly."""
    s0, e0, w0 = np.array((0.105, -0.01, 0.45)), np.array((0.158, -0.045, 0.325)), np.array((0.135, -0.085, 0.52))
    k.add("sleeveR", tube([s0, (s0 + e0) / 2 + (0.012, 0, 0), e0], [0.05, 0.047, 0.044]), BLACK, "armR")
    k.add("foreR", tube([e0, w0], [0.044, 0.036]), BLACK, "armR")
    hand = np.array((0.112, -0.083, 0.565))
    k.add("handR", ell(hand, (0.03, 0.028, 0.034), deform=boxy(2.3)), BLUE, "armR")
    for j, dz in enumerate((0.018, 0.0, -0.018)):
        b = hand + np.array((-0.018, -0.01, dz))
        k.add(f"fingerR{j}", tube([b, b + (-0.018, 0.012, 0.004)], [0.0115, 0.0105]), BLUE, "armR")
    # the left: down the side, then across the front of the belly
    s1, e1, w1 = np.array((-0.105, -0.01, 0.45)), np.array((-0.13, -0.075, 0.31)), np.array((-0.035, -0.125, 0.3))
    k.add("sleeveL", tube([s1, (s1 + e1) / 2 + (-0.01, 0, 0), e1], [0.05, 0.047, 0.044]), BLACK, "body")
    k.add("foreL", tube([e1, w1], [0.044, 0.038]), BLACK, "body")
    hand = np.array((0.002, -0.135, 0.302))
    k.add("handL", ell(hand, (0.034, 0.024, 0.03), deform=boxy(2.3)), BLUE, "body")
    for j, dz in enumerate((0.016, 0.0, -0.016)):
        b = hand + np.array((0.025, -0.006, dz))
        k.add(f"fingerL{j}", tube([b, b + (0.016, 0.006, -0.006)], [0.0105, 0.0095]), BLUE, "body")
    # the black sleeves stop at the wrists: paint the forearms' ends back black (the hands' blue bleeds a little)
    for c in (w0, w1):
        k.paint(lambda P, c=c: inside(np.linalg.norm(P - c, axis=1) - 0.035, 0.003), BLACK, on=["fore", "sleeve"])


def add_head(k):
    k.add("hood", ell(H, HR), BLACK, "head")
    k.add("face", ell(FACE, FR), BLUE, "head")
    k.add("muzzle", ell((0, -0.1, 0.462), (0.068, 0.05, 0.05)), PALE, "head")
    k.add("chin", ell((0, -0.085, 0.43), (0.05, 0.04, 0.03)), PALE, "head")
    k.add("bun", ell((0, 0.015, 0.69), (0.055, 0.05, 0.045)), BLACK, "head")
    for i, (x, h, lean) in enumerate(((-0.022, 0.055, -0.4), (0.008, 0.07, 0.1), (0.032, 0.045, 0.6))):
        b = np.array((x, 0.018, 0.715))
        k.add(f"tuft{i}", hull([b + (0.018, 0, 0), b + (-0.018, 0, 0), b + (0, 0.014, 0), b + (0, -0.014, 0),
                                b + (lean * 0.02, 0.006, h)]), BLACK, "head", fuse=False, surface="fur")
    # the muzzle's upper edge a crisp line across the blue
    def pale(P):
        d = np.linalg.norm((P - np.array((0, -0.1, 0.462))) / (0.072, 0.054, 0.054), axis=1)
        return inside(d - 1.0, 0.03) * (P[:, 1] < -0.05)
    k.paint(pale, PALE, on=["face", "muzzle", "chin"])


def add_mouth(k):
    """Two nostrils high on the muzzle and a sly smirk across it, rising to the right."""
    for s in (1, -1):
        k.decal((0.017 * s, -0.146, 0.48), unit(0.15 * s, -1, 0.5), (0.008, 0.006), NOSTRIL, eps=0.0018, rings=6,
                segs=24)
    CI = srgb(INK)
    def smirk(u, v):
        c = np.tile(srgb(PALE), (len(u), 1))
        line = -0.1 + 0.35 * u + 0.35 * u * u
        c[np.abs(v - line) < 0.3] = CI
        return c
    k.decal((0.01, -0.148, 0.447), unit(0.05, -1, -0.15), (0.034, 0.014), smirk, eps=0.0018, rings=8, segs=48,
            outline=polygon([(-1, 0.5), (1, 1.0), (1, 0.2), (-1, -0.5)]))


# --- the eyes: yellow, the heavy black lid lying over their top half, a black pupil low ---------------------------------
OUTLINE = [(-1.0, 0.25), (-0.6, 0.55), (0.0, 0.7), (0.6, 0.6), (1.0, 0.2), (0.75, -0.45), (0.2, -0.72),
           (-0.4, -0.65), (-0.85, -0.3)]


def eye_colour():
    CY, CA, CI = srgb(YELLOW), srgb(AMBER), srgb(INK)
    rim = polygon(OUTLINE)
    def eye(u, v):
        t = np.clip((0.0 - v) / 0.8, 0, 1)[:, None]
        c = CY[None] * (1 - t) + CA[None] * t
        c[np.hypot((u + 0.05) / 0.2, (v + 0.22) / 0.3) < 1] = CI
        lid = 0.1 - 0.12 * u
        c[v > lid] = CI
        rr = np.hypot(u, v) / np.array([rim(t_) for t_ in np.arctan2(v, u)])
        c[rr > 0.86] = CI
        return c
    return eye


def eye_at(s):
    p, n = lt.on_head(0.043 * s, FACE[2] + 0.02, FACE, FR)
    return p, unit(n[0] * 1.15, n[1], n[2])


def lay_eyes(k, state):
    look = eye_look(state, eye_colour(), srgb(BLUE), srgb(INK), line=0.17)
    for s in (1, -1):
        p, n = eye_at(s)
        if s > 0:
            k.decal(p, n, (0.037, 0.029), look, eps=0.0018, outline=polygon(OUTLINE), rings=20, segs=56)
        else:
            k.decal(p, n, (0.037, 0.029), lambda u, v: look(-u, v), eps=0.0018,
                    outline=polygon([(-a, b) for a, b in OUTLINE]), rings=20, segs=56)


def add_chain(k):
    """The pink circlet round the brow, from behind the left ear across the forehead to a ring with a bar at the right
    temple."""
    z = 0.615
    pts = []
    for a in np.radians(np.linspace(-105, 55, 7)):
        pts.append((math.sin(a) * 0.119, H[1] - math.cos(a) * 0.111, z + 0.012 * math.sin(a)))
    for i, c in enumerate(pts):
        n = unit(c[0], c[1] - H[1], 0.25)
        k.add(f"band{i}", outward(ok.torus(c, n, 0.024, 0.0115)), PINK, "head", fuse=False, surface="metal")
    end = np.array((0.146, -0.03, 0.6))
    k.add("bandend", outward(ok.link_end(end, unit(0.25, -0.15, -1), unit(0.9, -0.4, 0.1), 0.03, 0.012)), PINK,
          "head", fuse=False, surface="metal")


def munkidori(src, out_path):
    k = Kit(src, coat="fur")
    add_cloak(k)
    add_legs(k)
    add_arms(k)
    add_head(k)
    add_chain(k)
    add_mouth(k)
    k.build(voxel=0.0055, smooth=4, band=0.004, wband=0.045)
    k.eye_states(lambda s: lay_eyes(k, s))
    k.export(out_path)


if __name__ == "__main__":
    munkidori(*args())
