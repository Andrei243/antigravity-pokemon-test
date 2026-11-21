# Fidough sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/fidough.py -- <the export's Fidough.glb> <out.glb>
# Written to be grown from: dachsbun.py keeps this frame (sibling("fidough")): the rolled ears, the ring round the
# neck, the little buns on the brow, the short muzzle and its nose, the eye, the stubby legs, the blotches of glaze.
#
# From the references: a pale cream puppy of dough; a great loaf of a head, as big as the body and set forward on it;
# a short round muzzle low on the face with a small cream nose bump on top and a tiny pink mouth under it; big round
# black eyes with a white glint, wide apart above the muzzle; three little round buns in a row over the brow; two
# great ears rolled up like buns at the head's sides, glazed yellow in blotches; a thick ring of dough round the neck
# under the head, yellow over its top with a scalloped lower edge; a short round body low on four stubby legs; a round
# puff of a tail, yellow over its top; yellow blotches over the head's crown and the back.
# The pictures overrule the task's text: the ears are rolled buns (coils), not lumps; there is a ring round the neck,
# and the dough is glazed yellow in blotches.
# Reference ratios (3D picture): the head 0.55 of the height tall and 1.15 times as wide as the body; the ears 0.4 of
# the head's height; the legs showing 0.13 of the height; the eyes 0.07 of the height across.
# The export's joints: body (0, 0.05, 0.12), head (0, -0.03, 0.17), ears (+-0.07, -0.06, 0.235), legs (+-0.06, -0.02,
# 0.07), hind legs (+-0.06, 0.12, 0.07), tail (0, 0.16, 0.15).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, boxy, eye_look, args

CREAM = "#F4EBCB"
YELLOW = "#F4D43A"
EYE = "#1C1A20"
MOUTH = "#E8848C"
WHITE = "#FFFFFF"

HC = np.array([0.0, -0.07, 0.2])           # the head's middle
HR = np.array([0.108, 0.088, 0.086])
VOXEL = 0.0034


def on_front(c, r, x, z, lift=0.0):
    """The front point of an ellipsoid at (x, z) and its outward normal."""
    c, r = np.array(c, float), np.array(r, float)
    q = max(0.0, 1 - ((x - c[0]) / r[0]) ** 2 - ((z - c[2]) / r[2]) ** 2)
    p = np.array([x, c[1] - r[1] * math.sqrt(q), z])
    n = (p - c) / r ** 2
    n /= np.linalg.norm(n)
    return p + n * lift, n


def blotch(k, centre, radius, col, on, lobes=5, wob=0.22, seed=0, squash=(1, 1, 1)):
    """A blotch of glaze painted round a point: a ball's reach with a scalloped edge."""
    c = np.array(centre, float); sq = np.array(squash, float)
    def m(P):
        q = (P - c) / sq
        d = np.linalg.norm(q, axis=1)
        a = np.arctan2(q[:, 2], q[:, 0] + q[:, 1])
        rr = radius * (1 + wob * np.sin(lobes * a + seed))
        return inside(d - rr, 0.0025)
    k.paint(m, col, on=on)


def roll_ear(k, c, r, out, col, bone, name="ear"):
    """An ear rolled up like a bun at the head's side (mirrored): a fat round roll and its coiled inner turn showing on
    the outer face. Returns the outer face's middle."""
    c = np.array(c, float); out = unit(*out)
    k.add(name, ell(c, r), col, bone, mirror=True)
    inner = c + out * r[0] * 0.55 + np.array([0, -r[1] * 0.12, r[2] * 0.12])
    k.add(name + "coil", ell(inner, (r[0] * 0.55, r[1] * 0.58, r[2] * 0.55)), col, bone, mirror=True)
    tip = c + out * r[0] * 0.75 + np.array([0, r[1] * 0.25, -r[2] * 0.3])
    k.add(name + "curl", ell(tip, (r[0] * 0.4, r[1] * 0.42, r[2] * 0.4)), col, bone, mirror=True)
    return c + out * r[0]


def neck_ring(k, c, R, r, tilt, col, bone, lumps=0, amp=0.0, name="ring", n=40):
    """A thick ring round the neck (a doughnut, or with lumps a ring of buns), tilted forward by tilt degrees."""
    c = np.array(c, float)
    t = math.radians(tilt)
    th = np.linspace(0, 2 * math.pi, n + 1)
    pts = np.stack([R * np.sin(th), -R * np.cos(th) * math.cos(t), -R * np.cos(th) * math.sin(t)], axis=1) + c
    rad = r * (1 + amp * np.cos(lumps * th)) if lumps else np.full(len(th), r)
    pts = np.vstack([pts, pts[1:3]])
    rad = np.concatenate([rad, rad[1:3]])
    k.add(name, tube(pts, rad, seg=18, per=4), col, bone)


def ring_glaze(k, c, R, tilt, col, on, top=0.0, scallops=8, depth=0.006):
    """Glaze over a ring's top with a scalloped lower edge (in the ring's own frame)."""
    c = np.array(c, float); t = math.radians(tilt)
    up = np.array([0, -math.sin(t), math.cos(t)])
    def m(P):
        q = P - c
        h = q @ up
        a = np.arctan2(q[:, 0], -q[:, 1])
        edge = top - depth * (0.5 + 0.5 * np.cos(scallops * a))
        return inside(edge - h, 0.0025)
    k.paint(m, col, on=on)


def bun_eye(ink, white="#FFFFFF", glint=(0.28, 0.32, 0.3), small=(-0.25, -0.3, 0.13)):
    """A big round dark eye with a white glint up and out and a small one low and in."""
    I_, W_ = srgb(ink), srgb(white)
    gu, gv, gr = glint
    su, sv, sr = small
    def look(u, v):
        c = np.tile(I_, (len(u), 1))
        c[((u - gu) / gr) ** 2 + ((v - gv) / gr) ** 2 < 1] = W_
        c[((u - su) / sr) ** 2 + ((v - sv) / sr) ** 2 < 1] = W_
        return c
    return look


def lay_eyes(k, state, E, n, half, look, lid, ink, line=0.17):
    f = eye_look(state, look, srgb(lid), srgb(ink), line=line)
    for side in (1, -1):
        col = f if side == 1 else (lambda a, b, g=f: g(-a, b))
        k.decal(np.array(E) * np.array([side, 1, 1.0]), np.array(n) * np.array([side, 1, 1.0]), half, col,
                eps=0.0018, rings=20, segs=56)


def stubby_legs(k, col, xs, ys, top, r, paw, bone_front="leg{s}", bone_hind="hind{s}"):
    """Four stubby legs under the body, each a short tube ending in a round paw (mirrored)."""
    for nm, y, bone in (("leg", ys[0], bone_front), ("hind", ys[1], bone_hind)):
        k.add(nm, tube([(xs, y, top), (xs, y - 0.003, paw[2] + 0.012)], [r * 1.1, r]), col, bone, mirror=True)
        k.add(nm + "paw", ell((xs, y - 0.008, paw[2]), paw), col, bone, mirror=True)


def fidough(src, out):
    k = Kit(src, coat="fur")
    # the body, low and round; the legs
    k.add("body", ell((0, 0.07, 0.094), (0.078, 0.112, 0.064)), CREAM, "body")
    stubby_legs(k, CREAM, 0.05, (-0.005, 0.13), 0.06, 0.026, (0.028, 0.032, 0.013))
    # the ring round the neck
    RC, RR, RT = np.array([0.0, -0.035, 0.106]), 0.072, 18
    neck_ring(k, RC, RR, 0.03, RT, CREAM, "body")
    # the head: a loaf, flat underneath
    def loaf(V):
        V = boxy(2.6)(V)
        V[:, 2] = np.where(V[:, 2] < 0, V[:, 2] * 0.85, V[:, 2])
        return V
    k.add("head", ell(HC, HR, deform=loaf), CREAM, "head")
    MC = np.array([0.0, -0.148, 0.165])
    k.add("muzzle", ell(MC, (0.047, 0.036, 0.034)), CREAM, "head")
    k.add("nose", ell(MC + (0, -0.03, 0.022), (0.017, 0.013, 0.012)), CREAM, "head")
    # three little buns over the brow
    for x, z in ((-0.034, 0.268), (0.0, 0.274), (0.034, 0.268)):
        p, n = on_front(HC, HR * np.array([1, 0.95, 1.05]), x, z)
        k.add("bun", ell(p + n * 0.002, (0.023, 0.02, 0.019)), CREAM, "head")
    # the rolled ears
    roll_ear(k, (0.108, -0.03, 0.215), (0.048, 0.062, 0.068), (1, 0.15, 0), CREAM, "ear{s}")
    # the tail: a round puff up behind
    k.add("tail", ell((0, 0.185, 0.16), (0.038, 0.036, 0.036)), CREAM, "tail")
    # glaze: the crown, the ears, the ring's top, the tail's top, a blotch on the back
    blotch(k, (0, -0.07, 0.3), 0.075, YELLOW, ["head"], lobes=7, wob=0.12, squash=(1.15, 1, 1))
    for s in (1, -1):
        blotch(k, (0.165 * s, -0.035, 0.225), 0.052, YELLOW, ["ear"], lobes=5, wob=0.2, seed=1.0 * s)
    ring_glaze(k, RC, RR, RT, YELLOW, ["ring"], top=0.004, scallops=10, depth=0.008)
    blotch(k, (0, 0.19, 0.205), 0.03, YELLOW, ["tail"], lobes=6, wob=0.18)
    blotch(k, (0.03, 0.1, 0.175), 0.035, YELLOW, ["body"], lobes=5, wob=0.2, seed=2.0)
    # the tiny pink mouth under the nose
    mp, mn = on_front(MC, (0.047, 0.036, 0.034), 0.0, MC[2] - 0.012)
    k.decal(mp, unit(0, -1, -0.2), (0.008, 0.006), MOUTH, eps=0.0015, rings=8, segs=24,
            outline=lambda th: 1.0 if math.sin(th) < 0 else 0.35)
    k.build(voxel=VOXEL, smooth=4, band=0.004, wband=0.03)
    E, en = on_front(HC, HR, 0.058, 0.208)
    en = unit(en[0] * 0.8, en[1], en[2] * 0.5)
    k.eye_states(lambda s: lay_eyes(k, s, E, en, (0.019, 0.021), bun_eye(EYE), CREAM, EYE))
    k.export(out)


if __name__ == "__main__":
    fidough(*args())
