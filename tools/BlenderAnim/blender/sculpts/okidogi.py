# Okidogi sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/okidogi.py -- <the export's Okidogi.glb> <out.glb>
# The first of the Loyal Three: its toxic chain (a ring a link, a link with a bar for the end) is the one the other two
# take through sibling("okidogi"). From the references: a hulking dog standing like a wrestler, a great shaggy black
# chest wider than anything else, a green spade down its front, a green band at the waist under black tatters of fur
# over the hips; green arms from the elbow down with huge green hands, a pink pad on each palm (the black upper arms
# shaggy); short green legs with a black band at the ankle and broad green feet; a big round head, black above with
# pointed cat's ears and green below, a pink nose on the snout, a grin of white teeth, fierce yellow eyes cut by the
# black brow, a pink curl round the right eye; a thick pink collar of chain round its neck whose links trail up and
# back over its right shoulder to an end with a bar; a big bushy black tail behind.
# Reference ratios (3D picture, to the ears' tips): head crown to chin / height 0.23 (0.25 here); head width / height
# 0.32 (0.29 here, the cheeks' points add); chest width / height 0.57 (0.55 here with the shoulder fur); hand / height
# 0.16 (0.15 here); legs under the tatters / height 0.22 (0.24 here). Arms forward and down: hands at x +-0.185 inside
# the chest's half-width 0.215, y -0.22 before the belly's front (-0.13); elbows bent out to x +-0.275.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, hull, lathe, ring_band, outward, srgb, inside, unit, axes, boxy, polygon,
                       eye_look, args, sibling)

lt = sibling("litten")

BLACK = "#2B2B32"
GREEN = "#71B83E"
PINK = "#E04DB9"
PINKDARK = "#A8338A"
NOSE = "#D2508F"
YELLOW, AMBER = "#F6D43A", "#E8A626"
INK = "#14141A"
WHITE = "#F4F4F0"
MOUTH = "#3A1A24"
SHUTLINE = "#9BD25E"

H = np.array([0.0, -0.075, 0.785])
HR = np.array([0.13, 0.115, 0.12])
SH = np.array([0.2, -0.02, 0.6])
EL = np.array([0.272, -0.075, 0.47])
WR = np.array([0.21, -0.18, 0.38])
HAND = np.array([0.188, -0.22, 0.34])


# --- the toxic chain: what the Loyal Three all wear ------------------------------------------------------------------
def torus(c, axis, R, r, seg=36, ring=14):
    """A ring: a closed lathe of a circle of radius r at R from the axis."""
    prof = [(R + r * math.cos(t), r * math.sin(t)) for t in np.linspace(0, 2 * math.pi, ring + 1)]
    return lathe(c, axis, prof, seg=seg)


def link_end(c, d, axis, R, r):
    """The chain's end: a ring, a stub out of it along d and a bar across the stub's end (Okidogi's glyph)."""
    d = unit(*d)
    parts = [torus(c, axis, R, r)]
    a = np.cross(d, unit(*axis)); a /= np.linalg.norm(a)
    stem0 = np.array(c) + d * (R + r * 0.3)
    stem1 = stem0 + d * R * 0.75
    parts.append(lathe(stem0, d, [(0, 0), (r * 1.05, 0), (r * 1.05, R * 0.75), (0, R * 0.75)], seg=16))
    parts.append(lathe(stem1 - a * R * 0.6, a, [(0, 0), (r * 1.1, 0), (r * 1.1, R * 1.2), (0, R * 1.2)], seg=16))
    V, F = [], []
    for v, f in parts:
        o = sum(len(x) for x in V)
        V.append(v); F.extend([[i + o for i in q] for q in f])
    return np.vstack(V), F


def chain(k, name, centres, R, r, colour, bone, face=(0, -1, 0), end=True, twist=0.0):
    """Links at the given centres, each a ring facing face (turned twist degrees about the chain every other link);
    the last one the end piece when end is set."""
    C = [np.array(p, float) for p in centres]
    for i, c in enumerate(C):
        d = C[min(i + 1, len(C) - 1)] - C[max(i - 1, 0)]
        d = unit(*d)
        f = np.array(face, float)
        f = f - d * (f @ d); f = unit(*f)
        if twist and i % 2:
            g = np.cross(d, f)
            ang = math.radians(twist)
            f = unit(*(f * math.cos(ang) + g * math.sin(ang)))
        if end and i == len(C) - 1:
            shape = link_end(c, d, f, R * 1.1, r)
        else:
            shape = torus(c, f, R, r)
        k.add(f"{name}{i}", outward(shape), colour, bone, fuse=False, surface="metal")


# --- the trunk: a great shaggy black chest, a green waist, black tatters over the hips ---------------------------------
def add_body(k):
    k.add("chest", ell((0, 0.01, 0.565), (0.215, 0.15, 0.15)), BLACK, "body")
    k.add("pecs", ell((0, -0.06, 0.575), (0.17, 0.1, 0.11)), BLACK, "body")
    k.add("belly", ell((0, -0.005, 0.43), (0.155, 0.12, 0.1)), GREEN, "body")
    k.add("hips", ell((0, 0.01, 0.345), (0.16, 0.115, 0.075)), BLACK, "body")
    k.add("shoulder", ell((0.19, 0.0, 0.615), (0.095, 0.095, 0.085)), BLACK, "body", mirror=True)
    # the black chest comes down over the green waist in a ragged edge; the green band, then the hips black
    def chestblack(P):
        x, z = P[:, 0], P[:, 2]
        edge = 0.455 + 0.012 * lt_tri(x / 0.045)
        return inside(edge - z, 0.003)
    k.paint(chestblack, BLACK, on=["belly"])
    # the green spade down the front of the chest
    poly = lt.mirrored([(0.0, 0.592), (0.04, 0.61), (0.088, 0.635), (0.074, 0.58), (0.046, 0.53), (0.0, 0.47)])
    k.paint(lambda P: lt.poly_mask(P[:, 0], P[:, 2], poly, 0.002) * (P[:, 1] < -0.06), GREEN)
    # shaggy points of fur off the shoulders, the chest's sides and the upper arm
    for i, (x, y, z, dx, dy, dz, ln) in enumerate([
            (0.25, 0.0, 0.66, 1.0, 0.1, 0.5, 0.075), (0.24, 0.06, 0.6, 1.0, 0.4, 0.1, 0.07),
            (0.21, -0.07, 0.66, 0.6, -0.5, 0.6, 0.06), (0.16, 0.1, 0.66, 0.4, 0.5, 0.7, 0.07),
            (0.2, 0.05, 0.5, 0.8, 0.3, -0.3, 0.065), (0.12, -0.12, 0.66, 0.3, -0.6, 0.6, 0.05)]):
        k.add(f"shag{i}", lt.spike((x, y, z), (dx, dy, dz), ln, 0.034, 0.022), BLACK, "body", fuse=False,
              mirror=True, surface="fur")
    # the tatters of black fur over the hips, pointed, hanging down and out
    for i in range(14):
        a = 2 * math.pi * (i + 0.5) / 14
        s_, c_ = math.sin(a), math.cos(a)
        root = np.array([0.15 * s_, 0.01 - 0.11 * c_, 0.37])
        out = np.array([s_, -c_, 0.0])
        ln = 0.085 + 0.02 * (i % 3 == 0)
        k.add(f"tatter{i}", lt.spike(root, out * 0.45 + (0, 0, -1.0), ln, 0.034, 0.02), BLACK, "body", fuse=False,
              surface="fur")


def lt_tri(t):
    return 2 * np.abs(2 * (t - np.floor(t + 0.5))) - 1


# --- the tail: a big bushy black mass behind, a little to the right -------------------------------------------------
def add_tail(k):
    c, r = np.array((0.05, 0.19, 0.5)), np.array((0.12, 0.1, 0.18))
    k.add("tail", ell(c, r, rot=(-25, 0, 10), deform=lt_lumps(5, 4, 0.14)), BLACK, "body")
    # points of fur all over its back and sides, the camera behind sees them as the tail's ragged outline
    rs = np.random.RandomState(14)
    for i in range(16):
        a = 2 * math.pi * i / 16
        d = np.array([math.cos(a) * 1.0, 0.55 + 0.25 * rs.rand(), math.sin(a) * 1.0])
        root = c + np.array([math.cos(a) * r[0] * 0.85, 0.05, math.sin(a) * r[2] * 0.85])
        k.add(f"tailshag{i}", lt.spike(root, d, 0.065 + 0.02 * rs.rand(), 0.036, 0.022), BLACK, "body", fuse=False,
              surface="fur")


def lt_lumps(n, m, amp):
    def f(V):
        th = np.arctan2(V[:, 1], V[:, 0]); ph = np.arccos(np.clip(V[:, 2], -1, 1))
        return V * (1 + amp * np.abs(np.sin(n * th) * np.sin(m * ph)))[:, None]
    return f


# --- the legs: short, green, a black band at the ankle, broad green feet -------------------------------------------
def add_legs(k):
    knee, ankle = np.array((0.13, -0.012, 0.17)), np.array((0.15, -0.02, 0.07))
    k.add("thigh", ell((0.115, 0.0, 0.245), (0.088, 0.088, 0.1)), GREEN, "leg{s}", mirror=True)
    k.add("shin", tube([knee, ankle], [0.066, 0.058]), GREEN, "leg{s}", mirror=True)
    k.add("foot", ell((0.156, -0.05, 0.04), (0.072, 0.098, 0.04)), GREEN, "leg{s}", mirror=True)
    for j, dx in enumerate((-0.04, 0.0, 0.04)):
        k.add(f"toe{j}", ell((0.156 + dx, -0.135, 0.028), (0.024, 0.03, 0.026)), GREEN, "leg{s}", mirror=True)
    k.add("ankle", ring_band((0.148, -0.019, 0.095), (0.02, -0.008, 1), 0.061, 0.04, thick=0.008), BLACK, "leg{s}",
          fuse=False, mirror=True, surface="fur")


# --- the arms: black shaggy upper arms, green forearms, huge green hands carried forward and down ------------------
def add_arms(k):
    k.add("upper", tube([SH, EL], [0.078, 0.07]), BLACK, "arm{s}", mirror=True)
    k.add("fore", tube([EL + (WR - EL) * 0.12, WR], [0.066, 0.062]), GREEN, "arm{s}", mirror=True)
    k.add("hand", ell(HAND, (0.07, 0.068, 0.072), deform=boxy(2.4)), GREEN, "arm{s}", mirror=True)
    for j, dx in enumerate((-0.042, -0.014, 0.014, 0.04)):
        b = HAND + np.array((dx, -0.05, -0.035))
        k.add(f"finger{j}", tube([b, b + (0, -0.02, -0.035)], [0.02, 0.018]), GREEN, "arm{s}", mirror=True)
    k.add("thumb", tube([HAND + (-0.055, -0.02, 0.0), HAND + (-0.08, -0.06, -0.02)], [0.022, 0.019]), GREEN,
          "arm{s}", mirror=True)
    # the black fur stops in a crisp line round the elbow
    a, b = EL - (EL - SH) * 0.05, WR
    ab = b - a
    def green_fore(P):
        Q = P.copy(); Q[:, 0] = np.abs(Q[:, 0])
        t = ((Q - a) @ ab) / (ab @ ab)
        d = np.linalg.norm(Q - (a + np.clip(t, 0, 1)[:, None] * ab), axis=1)
        return inside(d - 0.09, 0.003) * inside(0.08 - t, 0.003) * (t > -0.3)
    k.paint(green_fore, BLACK, on=["fore", "upper"])
    # a pink pad on each palm (the hand's front and inner side)
    def pad(P):
        Q = P.copy(); Q[:, 0] = np.abs(Q[:, 0])
        return inside(np.linalg.norm((Q - (HAND + (-0.035, -0.045, 0.015))) / (1, 1, 1.25), axis=1) - 0.034, 0.003)
    k.paint(pad, PINK, on=["hand"])


# --- the head: black above with cat's ears, green below with a snout, a pink nose, a grin --------------------------
def add_head(k):
    k.add("head", ell(H, HR), BLACK, "head")
    k.add("neck", tube([(0, -0.03, 0.64), (0, -0.06, 0.72)], [0.1, 0.095]), BLACK, "head")
    k.add("muzzle", ell(H + (0, -0.088, -0.045), (0.094, 0.072, 0.07)), GREEN, "head")
    k.add("jaw", ell(H + (0, -0.06, -0.098), (0.105, 0.08, 0.05)), GREEN, "head")
    # green below the eyes, black above: the brow's edge runs just under the eyes, rising a little at the bridge
    def face(P):
        x, y, z = P[:, 0], P[:, 1], P[:, 2]
        edge = H[2] - 0.006 + 0.02 * inside(np.abs(x) - 0.018, 0.004) - 0.03 * np.clip(np.abs(x) - 0.1, 0, 1) * 3
        return inside(z - edge, 0.0025) * (y < H[1] + 0.03)
    k.paint(face, GREEN, on=["head", "neck"])
    # the pink curl round the right eye: an arc over it and down its outer side
    E = eye_at(1)[0]
    def curl(P):
        d = np.hypot(P[:, 0] - E[0], P[:, 2] - E[2])
        ang = np.degrees(np.arctan2(P[:, 2] - E[2], P[:, 0] - E[0]))
        return (inside(np.abs(d - 0.043) - 0.0075, 0.0025) * (ang < 140) * (ang > -75) * (P[:, 0] > 0)
                * (P[:, 1] < H[1] - 0.03))
    k.paint(curl, PINK, on=["head"])
    # green points of fur off the cheeks
    for i, (dz, ln) in enumerate(((-0.03, 0.06), (-0.075, 0.05))):
        k.add(f"cheek{i}", lt.spike(H + (0.118, -0.05, dz), (0.85, 0.1, -0.35), ln, 0.03, 0.018), GREEN, "head",
              fuse=False, mirror=True, surface="fur")
    # the pink nose on the snout's tip
    k.add("nose", outward(ell(H + (0, -0.16, -0.012), (0.03, 0.022, 0.024), sub=3)), NOSE, "head", fuse=False)
    # the ears: black points on the head's top corners, a pink lining
    for s in (1, -1):
        side = "R" if s > 0 else "L"
        b = np.array([0.075 * s, H[1] + 0.015, 0.865])
        tip = np.array([0.108 * s, H[1] + 0.03, 0.985])
        k.add("ear" + side, hull([b + (0.045 * s, 0.0, 0), b + (-0.04 * s, 0.0, 0.01), b + (0, -0.03, 0),
                                  b + (0, 0.032, 0), tip]), BLACK, "ear" + side, fuse=False, surface="fur")
        lb = b + np.array([0, -0.026, 0.02])
        k.add("lining" + side, hull([lb + (0.026 * s, 0.0, 0), lb + (-0.022 * s, 0, 0.004), lb + (0, 0.012, 0),
                                     lb + (0, -0.004, 0), tip + (-0.008 * s, -0.012, -0.03)]), PINK, "ear" + side,
              fuse=False, surface="fur")


def add_mouth(k):
    """A grin of white teeth meeting in a zigzag across the snout's lower front, the corners turned up."""
    CM, CW = srgb(MOUTH), srgb(WHITE)
    def grin(u, v):
        w = v - 0.28 * u * u
        c = np.tile(CM, (len(u), 1))
        c[np.abs(w) < 0.62] = CW
        c[np.abs(w - 0.16 * lt_tri(u * 2.6)) < 0.1] = CM
        return c
    at = H + np.array([0, -0.175, -0.075])
    k.decal(at, unit(0, -1, -0.15), (0.068, 0.03), grin, eps=0.003, rings=12, segs=64,
            outline=polygon([(-1, 0.75), (-0.5, 0.45), (0.5, 0.45), (1, 0.75), (1.0, 0.1), (0.5, -0.6),
                             (-0.5, -0.6), (-1, 0.1)]))


# --- the eyes: yellow almonds, a black pupil, cut by the black brow ---------------------------------------------------
OUTLINE = [(-1.0, -0.1), (-0.5, 0.2), (0.2, 0.45), (0.95, 0.62), (1.0, 0.2), (0.8, -0.42), (0.35, -0.7),
           (-0.25, -0.7), (-0.75, -0.48)]


def eye_at(s, x=0.052, dz=0.014):
    p, n = lt.on_head(x * s, H[2] + dz, H, HR)
    return p, unit(n[0] * 1.2, n[1], n[2])


def eye_colour():
    CY, CA, CI = srgb(YELLOW), srgb(AMBER), srgb(INK)
    rim = polygon(OUTLINE)
    def eye(u, v):
        t = np.clip((0.2 - v) / 1.0, 0, 1)[:, None]
        c = CY[None] * (1 - t) + CA[None] * t
        c[np.hypot((u + 0.02) / 0.22, (v - 0.0) / 0.38) < 1] = CI
        rr = np.hypot(u, v) / np.array([rim(t_) for t_ in np.arctan2(v, u)])
        c[rr > 0.84] = CI
        return c
    return eye


def lay_eyes(k, state):
    look = eye_look(state, eye_colour(), srgb(BLACK), srgb(SHUTLINE), line=0.17)
    for s in (1, -1):
        p, n = eye_at(s)
        if s > 0:
            k.decal(p, n, (0.036, 0.024), look, eps=0.0018, outline=polygon(OUTLINE), rings=20, segs=56)
        else:
            k.decal(p, n, (0.036, 0.024), lambda u, v: look(-u, v), eps=0.0018,
                    outline=polygon([(-a, b) for a, b in OUTLINE]), rings=20, segs=56)


def add_chain(k):
    """The thick pink collar round the neck and its links trailing up and back over the right shoulder."""
    k.add("collar", outward(torus((0, -0.03, 0.665), unit(0, -0.3, 1), 0.122, 0.024)), PINK, "body", fuse=False,
          surface="metal")
    pts = [(0.14, 0.07, 0.71), (0.185, 0.092, 0.758), (0.228, 0.112, 0.806), (0.268, 0.13, 0.856)]
    chain(k, "link", pts, 0.032, 0.0145, PINK, "body", face=(0.15, -1, 0.1))


def okidogi(src, out_path):
    k = Kit(src, coat="fur")
    add_body(k)
    add_tail(k)
    add_legs(k)
    add_arms(k)
    add_head(k)
    add_chain(k)
    add_mouth(k)
    k.build(voxel=0.0075, smooth=4, band=0.004, wband=0.06)
    k.eye_states(lambda s: lay_eyes(k, s))
    k.export(out_path)


if __name__ == "__main__":
    okidogi(*args())
