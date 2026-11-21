# Oranguru sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/oranguru.py -- <the export's Oranguru.glb> <out.glb>
#
# The design, from the references (the pictures overrule the task's text where they differ): a white orangutan sitting
# hunched, its legs folded in front of it with hand-like grey feet; the head a white hood that is the head itself, the
# grey face set into its front: a big grey muzzle, heavy-lidded yellow eyes, a light-blue line under each eye bordering
# a black cheek; a yellow four-pointed star on the hood's brow. A purple cape of hair falls from under the hood over the
# shoulders and down both sides in wavy lobes, streaked with orange and light blue. Thick white arms with shaggy white
# cuffs and grey hands: its right arm (-X) crosses the chest and holds the leaf fan's handle up at the +X shoulder with
# the index finger raised (the hair wraps the handle there); the left arm (+X) reaches down forward, a long-fingered
# grey hand on the ground. The fan is seven long green leaves spread up and back over the +X shoulder.
# Height 0.69 as the export; a single-stage line, built under __main__.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, slab, srgb, inside, unit, boxy, eye_look, polygon, args, sibling

ted = sibling("teddiursa")
hit, poly_mask, round_about, mirrored = ted.hit, ted.poly_mask, ted.round_about, ted.mirrored

WHITE, GREY, DARK, BLACK = "#EEEEEA", "#8A8C96", "#5E606A", "#24252B"
PURPLE, PURPLE_D, ORANGE, BLUE = "#7B3F9C", "#5E2E7C", "#EE9A3C", "#9CC4E8"
STAR, IRIS, INK = "#F2B53A", "#F0B030", "#1C1C22"
LEAF, LEAF2, STEM = "#33A55A", "#2A9150", "#7A5A3A"

H = np.array([0.0, -0.12, 0.545])             # the head's middle (its bone, the neck, is at 0, -0.04, 0.46)
HR = np.array([0.122, 0.112, 0.128])
M = np.array([0.0, -0.205, 0.48])             # the muzzle's middle
MR = np.array([0.057, 0.066, 0.072])
B = np.array([0.0, 0.03, 0.3])                # the body's middle
BR = np.array([0.175, 0.155, 0.17])
GRIP = np.array([0.16, -0.17, 0.5])           # where the fan's handle is held


def fur(amp, n=6, m=4):
    """A deform: soft shaggy lumps all round the unit sphere."""
    def f(V):
        th = np.arctan2(V[:, 1], V[:, 0]); ph = np.arccos(np.clip(V[:, 2], -1, 1))
        return V * (1 + amp * np.abs(np.sin(n * th) * np.sin(m * ph)))[:, None]
    return f


def cuff(k, name, c, d, r, n, length, bone):
    """A shaggy white cuff: n cones round a limb at c, leaning along d (toward the hand)."""
    c, d = np.array(c, float), unit(*d)
    a = np.cross(d, (0, 0, 1.0))
    if np.linalg.norm(a) < 1e-3:
        a = np.array([1.0, 0, 0])
    a /= np.linalg.norm(a)
    b = np.cross(d, a)
    for i in range(n):
        th = 2 * math.pi * (i + 0.5) / n
        o = math.cos(th) * a + math.sin(th) * b
        base = c + o * r * 0.75
        tip = c + o * r * 1.25 + d * length
        k.add(f"{name}{i}", tube([base, (base + tip) / 2 + o * 0.005, tip], [0.02, 0.013, 0.005]), WHITE, bone)


def hand_down(k, bone, W, s):
    """A long-fingered grey hand flat on the ground in front, the fingers splayed forward."""
    P = W + np.array([0.0, -0.05, -0.045])
    k.add("palm" + bone[-1], ell(P, (0.05, 0.05, 0.026)), GREY, bone)
    for i, ang in enumerate((-30, -10, 10, 30)):
        d = unit(math.sin(math.radians(ang)) * s, -math.cos(math.radians(ang)), 0)
        p0 = P + d * 0.03
        p1 = P + d * 0.07 + np.array([0, 0, 0.004])
        p2 = P + d * 0.1 + np.array([0, 0, -0.012])
        k.add(f"finger{bone[-1]}{i}", tube([p0, p1, p2], [0.016, 0.015, 0.013]), GREY, bone)
    k.add("thumb" + bone[-1], tube([P + np.array([-0.035 * s, -0.01, 0]), P + np.array([-0.07 * s, -0.04, -0.01])],
                                   [0.016, 0.013]), GREY, bone)


def hand_up(k, bone, Wr):
    """The grey hand that holds the fan's handle, the index finger raised."""
    P = Wr + np.array([0.025, -0.02, 0.03])
    k.add("palm" + bone[-1], ell(P, (0.04, 0.034, 0.042), rot=(0, -30, 0)), GREY, bone)
    for i, dz in enumerate((0.0, -0.022)):           # two fingers curled round the handle
        k.add(f"curl{bone[-1]}{i}", ell(P + np.array([0.03, -0.025, dz + 0.01]), (0.02, 0.018, 0.016)), GREY, bone)
    k.add("index" + bone[-1], tube([P + np.array([0.01, -0.02, 0.03]), P + np.array([0.02, -0.035, 0.07]),
                                    P + np.array([0.022, -0.04, 0.1])], [0.017, 0.015, 0.013]), GREY, bone)
    k.add("thumb" + bone[-1], tube([P + np.array([-0.02, -0.02, 0.0]), P + np.array([-0.005, -0.045, 0.02])],
                                   [0.016, 0.013]), GREY, bone)


def lock(k, name, pts, radii, f, bone="body"):
    """A lobe of the purple cape: a flattened tube whose broad face looks forward and back."""
    k.add(name, blade(pts, radii, (0, 1, 0), f), PURPLE, bone)


def streak(k, pts, width, colour, on, lift=0.0):
    """A bold stripe along a lock: paint within width of a line in the xz plane (both faces of the lobe)."""
    pts = np.array(pts, float)
    line = np.vstack([pts[i] + (pts[i + 1] - pts[i]) * t for i in range(len(pts) - 1)
                      for t in np.linspace(0, 1, 30)])
    def mask(P):
        d = np.sqrt(((P[:, None, [0, 2]] - line[None, :, [0, 2]]) ** 2).sum(2)).min(1)
        return inside(d - width, 0.003)
    k.paint(mask, colour, on=on)


def leaf_outline(length, width):
    """A long pointed leaf, its stalk at a = 0."""
    t = np.linspace(0, 1, 15)
    w = width * np.sin(np.pi * np.clip(t * 0.92 + 0.04, 0, 1)) ** 0.7 * (1 - 0.35 * t)
    top = [(length * a, b) for a, b in zip(t, w)]
    bot = [(length * a, -b) for a, b in zip(t[::-1], w[::-1])]
    return top + [(length * 1.02, 0.0)] + bot[1:-1]


def fan(k, bone):
    """The fan: seven thick leaves spread up and back over the +X shoulder, in a plane facing forward and out."""
    X = unit(0.62, 0.42, 0)
    Z = np.array([0, 0, 1.0])
    n = np.cross(X, Z)                               # the fan's face: forward and to +X
    stem_top = GRIP + unit(0.25, 0.15, 1) * 0.05
    k.add("stem", tube([GRIP - unit(0.25, 0.15, 1) * 0.04, stem_top], [0.014, 0.014]), STEM, bone, fuse=False)
    for i, a in enumerate(np.linspace(10, 112, 7)):
        d = X * math.cos(math.radians(a)) + Z * math.sin(math.radians(a))
        side = np.cross(n, d)
        L = 0.245 - 0.04 * abs(i - 3) / 3
        o = stem_top + n * (0.004 * (i % 2))          # alternate leaves stand a little apart: no shared plane
        k.add(f"leaf{i}", slab(leaf_outline(L, 0.03), o, d, side, 0.02), LEAF if i % 2 else LEAF2, bone,
              fuse=False, surface="leaf")


def oranguru(src, out):
    k = Kit(src, coat="fur")

    # The body: a broad shaggy white mass sitting low, fullest at the bottom
    def pear(V):
        V = fur(0.04)(V)
        s = 1 + 0.12 * np.clip(-V[:, 2], 0, 1) - 0.08 * np.clip(V[:, 2], 0, 1)
        V[:, 0] *= s; V[:, 1] *= s
        return V
    k.add("body", ell(B, BR, deform=pear), WHITE, "body")
    k.add("chest", ell((0, -0.07, 0.38), (0.14, 0.1, 0.1)), WHITE, "body")

    # The legs folded in front, the shins crossing, grey hand-like feet
    for s, bone in ((1, "legR"), (-1, "legL")):
        m = np.array([s, 1, 1.0])
        k.add("thigh" + bone[-1], tube([np.array(p) * m for p in [(0.08, -0.02, 0.17), (0.15, -0.1, 0.1),
                                                                   (0.17, -0.15, 0.075)]], [0.075, 0.07, 0.062]),
              WHITE, bone)
        k.add("shin" + bone[-1], tube([np.array(p) * m for p in [(0.17, -0.16, 0.065), (0.08, -0.2, 0.055),
                                                                  (-0.02, -0.21, 0.05)]], [0.058, 0.052, 0.046]),
              WHITE, bone)
        F = np.array((-0.055, -0.225, 0.045)) * m
        k.add("foot" + bone[-1], ell(F, (0.05, 0.048, 0.04), deform=boxy(2.4)), GREY, bone)
        for i, dz in enumerate((0.022, 0.0, -0.022)):
            k.add(f"ftoe{bone[-1]}{i}", ell(F + np.array([-0.04 * s, -0.035, dz]), (0.02, 0.018, 0.015)), GREY, bone)

    # The head: a white hood that IS the head, hunched forward; the grey muzzle stands out of its front
    def hood(V):
        V = boxy(2.2)(V)
        V[:, 2] *= 1 + 0.08 * np.clip(V[:, 2], 0, 1)     # a little peaked at the crown
        return V
    HEAD = ell(H, HR, deform=hood)
    k.add("head", HEAD, WHITE, "head")
    k.add("muzzle", ell(M, MR, deform=boxy(2.3)), GREY, "head")

    # The cape: a purple mantle over the back and shoulders, wavy lobes falling down and back at both sides
    k.add("mantle", ell((0, 0.06, 0.46), (0.2, 0.13, 0.11), deform=fur(0.05, 7, 3)), PURPLE, "body")
    k.add("cloak", ell((0, 0.12, 0.33), (0.18, 0.08, 0.15), deform=fur(0.04, 7, 3)), PURPLE, "body")
    k.add("nape", ell((0, 0.0, 0.53), (0.13, 0.1, 0.08)), PURPLE, "body")
    LOCKS = []
    for s in (1, -1):
        m = np.array([s, 1, 1.0])
        for j, (y0, drop, out_, r0) in enumerate([(-0.04, 0.0, 0.0, 0.058), (0.04, 0.03, 0.02, 0.06),
                                                 (0.11, 0.06, -0.01, 0.052)]):
            pts = [(0.15, y0, 0.5), (0.21, y0 + 0.02, 0.43), (0.25 + out_, y0 + 0.05, 0.34),
                   (0.27 + out_, y0 + 0.08, 0.25 - drop), (0.3 + out_, y0 + 0.1, 0.19 - drop)]
            pts = [np.array(p) * m for p in pts]
            lock(k, f"lock{'R' if s > 0 else 'L'}{j}", pts, [r0, r0 * 0.95, r0 * 0.85, r0 * 0.7, r0 * 0.45], 0.45)
            LOCKS.append(pts)
    # The hair wraps the fan's handle at the +X shoulder
    k.add("knot", ell(GRIP + np.array([0.01, 0.02, -0.01]), (0.05, 0.045, 0.05), deform=fur(0.08, 5, 3)), PURPLE,
          "body")
    # Bold streaks down the outer lobes, orange and light blue
    for idx, colour in ((1, ORANGE), (4, ORANGE), (0, BLUE), (3, BLUE)):
        pts = np.array(LOCKS[idx][1:])
        pts[:, 0] += np.sign(pts[0, 0]) * (0.012 if colour == ORANGE else -0.012)
        streak(k, pts, 0.008, colour, on=["lock"])

    # The arms, thick and white. Its right arm (-X) crosses the chest to hold the fan's handle at the +X shoulder
    SL, EL, WL = np.array((-0.14, -0.03, 0.42)), np.array((-0.13, -0.2, 0.29)), np.array((0.2, -0.19, 0.42))
    k.add("upperL", tube([SL, (SL + EL) / 2 + np.array([-0.02, 0, 0]), EL], [0.066, 0.062, 0.058]), WHITE, "armL")
    k.add("foreL", tube([EL, (EL + WL) / 2 + np.array([0, -0.02, -0.01]), WL], [0.058, 0.054, 0.048]), WHITE, "armL")
    cuff(k, "cuffL", WL - unit(*(WL - EL)) * 0.02, WL - EL, 0.048, 7, 0.035, "armL")
    hand_up(k, "armL", WL)
    # The left arm (+X) reaches down forward, the hand on the ground
    SR, ER, WR = np.array((0.14, -0.03, 0.42)), np.array((0.205, -0.13, 0.27)), np.array((0.17, -0.235, 0.12))
    k.add("upperR", tube([SR, (SR + ER) / 2 + np.array([0.015, 0, 0]), ER], [0.066, 0.062, 0.058]), WHITE, "armR")
    k.add("foreR", tube([ER, WR], [0.058, 0.05]), WHITE, "armR")
    cuff(k, "cuffR", WR - unit(*(WR - ER)) * 0.02, WR - ER, 0.05, 7, 0.035, "armR")
    hand_down(k, "armR", WR, 1)

    # The grey face set into the hood's front; black cheeks bordered by a light-blue line under the eyes
    z0 = H[2]
    FRONT = lambda P: (P[:, 1] < H[1]) * (np.sqrt((((P - H) / HR) ** 2).sum(1)) < 1.2) * (np.abs(P[:, 0]) < 0.1)
    OFF_MUZZLE = lambda P: np.sqrt((((P - M) / (MR * 1.04)) ** 2).sum(1)) > 1
    FACE = mirrored([(0.0, z0 + 0.062), (0.04, z0 + 0.06), (0.068, z0 + 0.045), (0.086, z0 + 0.01),
                     (0.09, z0 - 0.05), (0.078, z0 - 0.1), (0.045, z0 - 0.14), (0.0, z0 - 0.15)])
    k.paint(lambda P: poly_mask(*round_about(P, H, HR[0]), FACE, edge=0.0035) * FRONT(P), GREY)
    # Black cheeks beside the muzzle, under the eyes, rimmed light blue along their top and sides: a patch a side
    CB, CK = srgb(BLUE), srgb(BLACK)
    def cheek(u, v):
        c = np.tile(CB, (len(u), 1))
        c[(v < 0.52) & (np.abs(u) < 0.7) & (v > -0.8)] = CK
        return c
    CHEEK_OUT = polygon([(-1, 0.85), (0, 1), (1, 0.85), (1, -0.3), (0.5, -0.95), (-0.5, -0.95), (-1, -0.3)])
    cn = unit(0.35, -0.93, -0.1)
    CC = hit(HEAD, np.array([0.05, H[1], z0 - 0.045]) + cn * 0.4, -cn)
    k.decal(CC, cn, (0.02, 0.04), cheek, outline=CHEEK_OUT, eps=0.0025, rings=22, segs=56, mirror=True)

    # The yellow four-pointed star on the hood's brow
    STARP = []
    for i in range(8):
        a = math.pi / 2 + i * math.pi / 4
        r = 1.0 if i % 2 == 0 else 0.34
        STARP.append((r * math.cos(a), r * math.sin(a)))
    sn = unit(0, -0.85, 0.52)
    SC = hit(HEAD, (0, -1, z0 + 0.088), (0, 1, 0))
    k.decal(SC, sn, (0.04, 0.046), srgb(STAR), outline=polygon(STARP), eps=0.003, rings=16, segs=96)

    # Nostrils on the muzzle's top front, a long mouth across its lower front
    for s in (1, -1):
        p = hit(ell(M, MR, deform=boxy(2.3)), (0.018 * s, -1, M[2] + 0.03), (0, 1, 0))
        k.decal(p, unit(0.15 * s, -1, 0.35), (0.009, 0.007), srgb(INK), eps=0.003, rings=8, segs=32)
    CG, CI = srgb(GREY), srgb(DARK)
    def mouth(u, v):
        c = np.tile(CG, (len(u), 1))
        c[np.abs(v - 0.25 * u * u) < 0.35] = CI
        return c
    MC = hit(ell(M, MR, deform=boxy(2.3)), (0, -1, M[2] - 0.035), (0, 1, 0))
    k.decal(MC, unit(0, -1, -0.2), (0.05, 0.008), mouth, eps=0.003, rings=8, segs=56)

    # Heavy-lidded yellow eyes on the face's grey, just above the blue line: the lid cuts the eye straight across
    OUT = [(-1.0, 0.18), (-0.7, -0.6), (0.0, -0.95), (0.7, -0.6), (1.0, 0.18)]
    def open_look(u, v):
        c = np.tile(srgb(INK), (len(u), 1))
        c[(u / 0.86) ** 2 + ((v + 0.35) / 0.56) ** 2 < 1] = srgb(IRIS)
        c[(u + 0.05) ** 2 + ((v + 0.3) / 1.1) ** 2 < 0.27 ** 2] = srgb(INK)
        c[v > 0.02] = srgb(GREY)
        c[np.abs(v - 0.02) < 0.14] = srgb(INK)
        return c
    de = unit(0.36, -0.92, 0.08)
    E = hit(HEAD, (0.045, -1, z0 + 0.025), (0, 1, 0))
    def lay(state):
        look = eye_look(state, open_look, srgb(GREY), srgb(INK), line=0.18)
        k.decal(E, de, (0.03, 0.026), look, outline=polygon(OUT), eps=0.003, rings=20, segs=56, mirror=True)

    fan(k, "armL")
    k.build(voxel=0.0069, smooth=3, band=0.004, wband=0.05)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    oranguru(*args())
