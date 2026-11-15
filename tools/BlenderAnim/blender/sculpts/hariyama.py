# Hariyama sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown from
# Makuhita's script (makuhita.py, imported: its legs and feet, the way a patch is laid on the head, the paint masks):
#   blender --background --factory-startup --python sculpts/hariyama.py -- <the export's Hariyama.glb> <out.glb>
# From the references: a huge sumo. A pale grey-cream chest and shoulders with the head sunk into them, no neck; a
# white topknot lying front to back on the crown, a dark blue band round its foot that comes down the brow to a point
# between the eyes; narrow fierce eyes, white with a small dark pupil, their top edge a straight lid sloping down to
# the nose and cut by the blue; a small round snout with two nostrils, a short stern mouth; grey upper arms and orange
# forearms ending in enormous open orange hands, a broad palm with two thick fingers in a V and a thumb; below the
# chest a skirt of broad yellow panels hanging all round from the waist over a great round navy lower body and two
# navy haunches; short navy legs on orange feet with toes. (The pictures overrule the task's text: the lower body is
# navy under a yellow skirt, the upper body grey-cream; nothing of it is "blue-and-cream" as one garment.)
# Reference ratios (3D picture, bun to feet): head with the bun / height 0.38; head width / height 0.35; lower body
# width / height 0.98; a hand across / height 0.3; the skirt's depth / height 0.4.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, sheet, srgb, inside, unit, boxy, eye_look, polygon, outward, args, sibling

m = sibling("makuhita")

# --- colours ----------------------------------------------------------------------------------------------------------
GREY = "#CAC4B8"
WHITE = "#F6F4EE"
NAVY = "#2C4E8C"
YELLOW = "#F6D44C"
ORANGE = "#EE8248"
INK = "#1E1E28"

# --- the frame (the skeleton's joints: body (0, 0, 0.4), arms (+-0.16, 0, 0.46), head (0, -0.04, 0.54), legs
#     (+-0.1, 0, 0.2)) ----------------------------------------------------------------------------------------------------
H = np.array([0.0, -0.085, 0.635])        # the head's middle, up and forward of its joint
HR = np.array([0.145, 0.124, 0.116])
BUN = np.array([0.0, -0.065, 0.75])


def add_body(k):
    # the great round lower body, heaviest low, navy under the skirt
    k.add("hips", ell((0, 0.01, 0.245), (0.29, 0.225, 0.215)), NAVY, "body")
    # the grey chest and the shoulders the head sinks into
    k.add("chest", ell((0, -0.01, 0.48), (0.24, 0.19, 0.15)), GREY, "body")
    # one smooth slope from the head out to the shoulders
    k.add("yoke", ell((0, 0.0, 0.56), (0.2, 0.15, 0.085)), GREY, "body")
    # navy below the waist (a crisp line, the skirt hangs over it)
    k.paint(lambda P: inside(P[:, 2] - 0.375, 0.003), NAVY, on=["chest", "hips"])


def add_head(k):
    def wedge(V):
        V = boxy(2.3)(V)
        V[:, 0] *= 1 + 0.07 * np.clip(-V[:, 2], 0, 1)        # fuller at the jowls
        return V
    k.add("head", ell(H, HR, deform=wedge), GREY, "head")
    k.add("jowl", ell(H + (0, 0.01, -0.075), (0.13, 0.11, 0.065)), GREY, "head")
    k.add("snout", ell(H + (0, -0.118, -0.032), (0.042, 0.032, 0.032)), GREY, "head")
    # the white topknot lying front to back on the crown, a loose piece so its edge stays crisp
    k.add("bun", outward(ell(BUN, (0.078, 0.118, 0.052))), WHITE, "head", fuse=False, surface="hair")


R_HEAD = 0.14
# the blue on the head (angle round it x radius, height): a band round the bun's foot, its front come down to a point
# between the eyes
BLUE = [(0.0, 0.652), (0.02, 0.672), (0.058, 0.697), (0.095, 0.715), (0.14, 0.72), (0.24, 0.725),
        (0.24, 0.85), (0.0, 0.85)]
BLUE = BLUE + [(-a, z) for a, z in BLUE[::-1][1:-1]]


def paint_head(k):
    def mask(P):
        a = np.arctan2(P[:, 0], -(P[:, 1] - H[1])) * R_HEAD
        front = m.poly_mask(a, P[:, 2], BLUE, 0.0025) * (P[:, 1] < H[1] + 0.02)
        back = inside(0.72 - P[:, 2], 0.0025) * (P[:, 1] >= H[1] + 0.02)
        return np.maximum(front, back)
    k.paint(mask, NAVY, on=["head"])


def add_arms(k):
    """Grey shoulders and upper arms, orange forearms held forward, enormous open hands facing forward."""
    for side, s in (("R", 1), ("L", -1)):
        q = np.array([s, 1, 1.0])
        SH, EL, WR = np.array((0.215, -0.01, 0.49)) * q, np.array((0.29, -0.09, 0.43)) * q, np.array((0.315, -0.2, 0.43)) * q
        k.add(f"shoulder{side}", ell(SH, (0.085, 0.085, 0.08)), GREY, f"arm{side}")
        k.add(f"upper{side}", tube([SH, EL], [0.07, 0.062]), GREY, f"arm{side}")
        k.add(f"fore{side}", tube([EL, WR], [0.062, 0.056]), ORANGE, f"arm{side}")
        # the palm: broad and flat, facing forward, tilted a little out
        P = np.array((0.325, -0.24, 0.47)) * q
        k.add(f"palm{side}", ell(P, (0.092, 0.04, 0.086), rot=(0, 14 * s, 0)), ORANGE, f"arm{side}")
        # two thick fingers in a V, and a thumb out to the inner side
        for i, (a, b) in enumerate((((0.3, -0.24, 0.52), (0.27, -0.245, 0.625)),
                                    ((0.36, -0.24, 0.515), (0.415, -0.245, 0.6)))):
            k.add(f"finger{i}{side}", tube([np.array(a) * q, np.array(b) * q], [0.05, 0.043]), ORANGE, f"arm{side}")
        k.add(f"thumb{side}", tube([np.array((0.27, -0.24, 0.45)) * q, np.array((0.21, -0.25, 0.48)) * q],
                                   [0.043, 0.036]), ORANGE, f"arm{side}")
    # a crisp line at the elbow: orange beyond it, grey before
    d = unit(0.025, -0.11, 0.0)
    def forearm(P):
        Q = P.copy(); Q[:, 0] = np.abs(Q[:, 0])
        rel = Q - np.array((0.29, -0.09, 0.43))
        line = np.linalg.norm(rel - np.outer(rel @ d, d), axis=1)
        return inside(-(rel @ d) - 0.012, 0.0025) * (Q[:, 0] > 0.2) * (P[:, 2] > 0.33) * inside(line - 0.09, 0.004)
    k.paint(forearm, ORANGE)
    k.paint(lambda P: (1 - forearm(P)) * (np.abs(P[:, 0]) > 0.17) * (P[:, 2] > 0.4) * (P[:, 1] > -0.16)
            * inside(np.abs(P[:, 0]) - 0.36, 0.003), GREY)


def add_skirt(k, n=9):
    """Broad yellow panels hanging all round from the waist over the hips, a little gap between each."""
    gap = 0.06
    for i in range(n):
        a0 = 2 * math.pi * i / n + gap / 2 + math.pi / n
        a1 = 2 * math.pi * (i + 1) / n - gap / 2 + math.pi / n
        back = 0.5 * (1 - math.cos(0.5 * (a0 + a1)))             # 1 behind, 0 in front

        def at(u, v, a0=a0, a1=a1, back=back):
            a = a0 + (a1 - a0) * u
            rx, ry = 0.255 + 0.115 * v, 0.205 + 0.09 * v
            z = 0.415 - (0.205 - 0.03 * back) * v
            return np.stack([np.sin(a) * rx, -np.cos(a) * ry + 0.01, z + 0 * u], -1)
        k.add(f"skirt{i}", outward(sheet(at, 9, 6, 0.014)), YELLOW, "body", fuse=False, surface="cloth")
    # the belt the panels hang from
    def belt(u, v):
        a = 2 * math.pi * u
        return np.stack([np.sin(a) * 0.252, np.cos(a) * 0.202 + 0.01, 0.42 + (v - 0.5) * 0.04 + 0 * u], -1)
    k.add("belt", outward(sheet(belt, 97, 3, 0.012)), YELLOW, "body", fuse=False, surface="cloth")


def add_legs(k):
    # huge navy haunches bulging out at the sides under the skirt, short legs, orange feet with toes
    k.add("thigh", ell((0.19, 0.0, 0.17), (0.14, 0.15, 0.15)), NAVY, "leg{s}", mirror=True)
    m.add_legs(k, color=NAVY, x=0.18, top=0.13, r=0.08, foot=(0.075, 0.095, 0.036), foot_color=ORANGE, ankle=0.06)
    k.paint(lambda P: inside(P[:, 2] - 0.064, 0.0025) * inside(np.hypot((np.abs(P[:, 0]) - 0.188) / 0.09, (P[:, 1] + 0.055) / 0.11) - 1, 0.003), ORANGE)


# --- the face: patches on the fused head, laid before the body's build ----------------------------------------------
CG, CW, CI = srgb(GREY), srgb("#FFFFFF"), srgb(INK)


def add_nose_mouth(k):
    tip = H + np.array((0, -0.152, -0.028))
    for s in (1, -1):
        k.decal(tip + (0.013 * s, 0, -0.002), unit(0.3 * s, -1, 0.1), (0.007, 0.006), INK, eps=0.003, rings=6, segs=24)
    def mouth(u, v):
        c = np.tile(CG, (len(u), 1))
        c[(np.abs(v - (0.2 - 0.35 * u * u)) < 0.24) & (np.abs(u) < 0.85)] = CI
        return c
    k.decal(H + np.array((0, -0.14, -0.078)), unit(0, -1, -0.1), (0.03, 0.012), mouth, eps=0.003, rings=12, segs=56)


# the eye: a narrow almond whose top edge is a straight lid sloping down toward the nose (u outward)
OUTLINE = [(-1.0, -0.1), (-0.5, 0.18), (0.3, 0.52), (1.0, 0.72), (0.95, 0.1), (0.4, -0.5), (-0.3, -0.62),
           (-0.85, -0.42)]
EYE_HALF = (0.04, 0.028)


def eye(u, v):
    c = np.tile(CW, (len(u), 1))
    c[np.hypot((u + 0.05) / 0.26, (v + 0.02) / 0.42) < 1] = CI
    c[np.hypot((u - 0.05) / 0.1, (v - 0.12) / 0.12) < 1] = (1, 1, 1)
    c[v > 0.08 + 0.48 * u] = CI                       # the heavy upper lid along the top edge
    return c


def lay_eyes(k, state):
    p, n = m.on_head(0.064, H[2] + 0.004, 0.0, H, HR)
    n = unit(n[0] * 1.1, n[1], n[2] + 0.05)
    look = eye_look(state, eye, CG, CI, line=0.18)
    k.decal(p, n, EYE_HALF, look, eps=0.004, outline=polygon(OUTLINE), rings=20, segs=56)
    q = np.array([-1, 1, 1.0])
    k.decal(p * q, n * q, EYE_HALF, lambda u, v: look(-u, v), eps=0.004,
            outline=polygon([(-x, y) for x, y in OUTLINE]), rings=20, segs=56)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="fur")
    add_body(k)
    add_legs(k)
    add_arms(k)
    add_head(k)
    paint_head(k)
    add_skirt(k)
    add_nose_mouth(k)
    k.build(voxel=0.0075, band=0.006)
    k.eye_states(lambda s: lay_eyes(k, s))
    k.export(out)
