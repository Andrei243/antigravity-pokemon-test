# Castoroi sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/castoroi.py -- <the export's Castoroi.glb> <out.glb>
#
# Carpathia No. 10150, a lodge-building beaver (CP-dex-3b-river-and-caves-more.md, 150; picture 10150-Castoroi.jpg).
# Grown from castorel.py (sibling("castorel")): the line's eye (teal iris, black pupil, two glints, here smaller and
# half-lidded, patient), its orange chisel teeth, its dark teal nose, webbed feet, scaled paddle tail and flank scales;
# the russet grown dark wet red-brown, the belly and muzzle tan.
# The masses, largest first (picture): the heavy hunched pear of a body; the lodge of woven branches on its back
# (0.9 of the body's width, its top the model's top) with green shoots and leaves; the great haunches and broad flat
# dark teal hind feet with claws; the head (0.31 of the height, 0.48 of the body's width) carried forward and low with
# a tan muzzle, big dark nose and huge orange teeth; the flat paddle tail resting on the ground behind (back and out to
# +X); the forelimbs hanging before the belly with dark teal clawed hands.
# Skeleton: body (0, 0.02, 0.34), head (0, -0.22, 0.44), front legs (+-0.16, -0.16, 0.2), hind (+-0.16, 0.16, 0.2),
# tail (0, 0.28, 0.14). Export 0.81 tall. The front legs carry the forearms and hands, which hang clear of the ground
# as drawn (the upper arm is on `body`).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, leaf, srgb, inside, unit, eye_look, polygon, outward, args, sibling

ca = sibling("castorel")
RED = "#7E3226"
TAN = "#C48E6A"
MUZZLE = "#C8946E"
EAR_IN = "#5A2218"
TEAL, TEAL_DARK, SCALE, TOOTH, NOSE = ca.TEAL, ca.TEAL_DARK, ca.SCALE, ca.TOOTH, ca.NOSE
INK = ca.INK
LODGE = "#4A3424"
WOODS = ("#7A5638", "#8E6844", "#5E4430", "#6E4C32")
LEAF, LEAF_DARK, STEM = "#5E9E44", "#3F7A34", "#4E7A30"

FACE = unit(0, -1, 0.0)
HUP = np.array([0, 0, 1.0])
X = np.array([1.0, 0, 0])
HC = np.array([0.0, -0.3, 0.525])
HR = np.array([0.158, 0.145, 0.145])


def hd(a, f, u):
    return a * X + f * FACE + u * HUP


MC = HC + FACE * 0.112 - HUP * 0.052
MR = np.array([0.092, 0.07, 0.066])


def claw_row(k, name, centre, forward, across, n, r, bone, colour=TEAL_DARK):
    forward = unit(*forward); across = unit(*across)
    up = np.cross(forward, across)
    for i in range(n):
        p = np.array(centre, float) + across * (i - (n - 1) / 2) * r * 2.4
        pts = [p + across * r, p - across * r, p + up * r * 0.8, p - up * r * 0.8, p + forward * r * 2.4 - up * r * 0.5]
        k.add(f"{name}{i}", hull(pts), colour, bone, fuse=False, surface="shell")


def lodge(k, centre, radii, seed=11):
    """The lodge on the back: a dark mound fused into the back, woven over with loose branches, green shoots on it."""
    c, r = np.array(centre, float), np.array(radii, float)
    k.add("lodge", ell(c, r), LODGE, "body", surface="leaf")
    rs = np.random.RandomState(seed)
    def on(th, el, lift):
        d = np.array([math.cos(el) * math.cos(th), math.cos(el) * math.sin(th), math.sin(el)])
        p, n = ca.on_ell(c, r, d)
        return p + n * lift
    # Branches: arcs laid over the mound, each crossing it from side to side, their ends sticking out past it
    for i in range(34):
        th = rs.uniform(0, 2 * math.pi)
        span = rs.uniform(1.5, 2.6)
        el0, el1 = rs.uniform(-0.5, 0.3), rs.uniform(-0.5, 0.3)
        pts = [on(th + span * t, el0 + (el1 - el0) * t + 0.9 * math.sin(math.pi * t) * rs.uniform(0.15, 0.8),
                  0.012 + 0.004 * (i % 3)) for t in np.linspace(0, 1, 7)]
        a, b = pts[0], pts[-1]
        da, db = a - c, b - c
        da[2] = 0; db[2] = 0
        pts = [a + unit(*da) * 0.03 - np.array([0, 0, 0.02])] + pts + [b + unit(*db) * 0.03 - np.array([0, 0, 0.02])]
        rad = rs.uniform(0.012, 0.015)
        k.add(f"branch{i}", tube(pts, [rad] * len(pts), seg=10, per=5), WOODS[i % 4], "body", fuse=False,
              surface="leaf")
    # Over the top: shorter branches laid across the crown, so no bare mound shows
    for i in range(10):
        th = rs.uniform(0, 2 * math.pi)
        el = rs.uniform(0.75, 1.25)
        pts = [on(th + 1.1 * (t - 0.5), el + 0.35 * math.sin(math.pi * t) - 0.2, 0.014) for t in np.linspace(0, 1, 6)]
        k.add(f"crown{i}", tube(pts, [0.0135] * len(pts), seg=10, per=5), WOODS[(i + 1) % 4], "body", fuse=False,
              surface="leaf")
    # Green shoots: a stem with leaves in pairs, standing up and out
    for i, (th, el) in enumerate(((0.4, 0.9), (1.9, 0.75), (3.0, 0.95), (4.2, 0.7), (5.4, 0.85), (2.5, 0.45),
                                  (5.9, 0.35), (1.1, 0.3))):
        base = on(th, el, -0.01)
        n = unit(*(base - c))
        d = unit(*(n * 0.6 + np.array([0, 0, 0.9])))
        ln = 0.08 + 0.02 * (i % 2)
        tip = base + d * ln
        k.add(f"stem{i}", tube([base, base + d * ln * 0.5, tip], [0.01, 0.009, 0.007], seg=10), STEM, "body",
              fuse=False, surface="leaf")
        side = unit(*np.cross(d, (0, 0, 1.0))) if abs(d[2]) < 0.98 else X
        for j, (t, s) in enumerate(((0.55, 1), (0.75, -1), (1.0, 0))):
            p = base + d * ln * t
            ld = unit(*(d * (1.0 if s == 0 else 0.5) + side * s * 0.9 + np.array([0, 0, 0.2])))
            k.add(f"leaf{i}_{j}", outward(leaf(p, ld, 0.058, 0.022, thick=0.011)), LEAF if (i + j) % 2 else LEAF_DARK,
                  "body", fuse=False, surface="leaf")


def castoroi(src, out):
    k = Kit(src, coat="fur")
    # The heavy hunched body, the head forward and low
    k.add("hips", ell((0, 0.05, 0.25), (0.25, 0.25, 0.235)), RED, "body")
    k.add("chest", ell((0, -0.09, 0.42), (0.205, 0.19, 0.175), rot=(-14, 0, 0)), RED, "body")
    k.add("head", ell(HC, HR), RED, "head")
    k.add("muzzle", ell(MC, MR), MUZZLE, "head")
    for i, (a, f, u, ln) in enumerate(((0.95, 0.2, -0.35, 0.06), (0.8, 0.25, -0.62, 0.055))):
        p, n = ca.on_ell(HC, HR, hd(a, f, u))
        d = unit(*(n + HUP * -0.4 + FACE * 0.1))
        k.add(f"tuft{i}", tube([p - n * 0.015, p + d * ln * 0.6, p + d * ln], [0.026, 0.014, 0.004]), RED, "head",
              mirror=True)
    ep, en = ca.on_ell(HC, HR, hd(0.6, 0.15, 0.74))
    k.add("ear", ell(ep - en * 0.008, (0.036, 0.026, 0.034), rot=(0, -30, 0)), RED, "head", mirror=True)
    # Great haunches and broad flat hind feet with claws
    k.add("haunch", ell((0.18, 0.12, 0.2), (0.11, 0.155, 0.155), rot=(-10, 0, 0)), RED, "hind{s}", mirror=True)
    k.add("heel", tube([(0.2, 0.1, 0.11), (0.21, 0.06, 0.065)], [0.07, 0.058]), RED, "hind{s}", mirror=True)
    k.add("foot", ell((0.215, -0.02, 0.026), (0.085, 0.145, 0.026), rot=(0, 0, 8)), TEAL, "hind{s}", mirror=True)
    for s, bone in ((1, "hindR"), (-1, "hindL")):
        claw_row(k, f"toeclaw{s}", (0.235 * s, -0.165, 0.02), (0.1 * s, -1, -0.2), (1, 0, 0), 4, 0.012, bone)
    # The forelimbs hanging before the belly, dark teal clawed hands
    k.add("shoulder", ell((0.17, -0.2, 0.4), (0.075, 0.075, 0.08)), RED, "body", mirror=True)
    k.add("forearm", tube([(0.18, -0.24, 0.34), (0.175, -0.29, 0.24), (0.165, -0.31, 0.18)], [0.055, 0.048, 0.044]),
          RED, "leg{s}", mirror=True)
    k.add("hand", ell((0.165, -0.32, 0.15), (0.05, 0.04, 0.048)), TEAL, "leg{s}", mirror=True)
    for s, bone in ((1, "legR"), (-1, "legL")):
        claw_row(k, f"handclaw{s}", (0.165 * s, -0.335, 0.112), (0, -0.3, -1), (1, 0, 0), 3, 0.011, bone)
    # The paddle tail resting on the ground, back and out to +X
    k.add("tailroot", tube([(0, 0.25, 0.15), (0.05, 0.33, 0.07)], [0.08, 0.06]), RED, "tail")
    TC, TR, TROT = np.array([0.13, 0.5, 0.048]), (0.13, 0.215, 0.03), (3, 0, -28)
    k.add("tail", ell(TC, TR, rot=TROT), TEAL, "tail", surface="scales")
    # The lodge on the back
    lodge(k, (0, 0.05, 0.575), (0.205, 0.23, 0.16))
    # The nose and the huge teeth
    npt, nn = ca.on_ell(MC, MR, hd(0, 0.8, 0.6))
    k.add("nose", ell(npt + nn * 0.002, (0.04, 0.025, 0.028), rot=(-20, 0, 0)), NOSE, "head", fuse=False, surface="skin")
    k.add("noseglint", ell(npt + nn * 0.018 + HUP * 0.008 + X * 0.01, (0.011, 0.005, 0.006)), "#5E7E86", "head",
          fuse=False, surface="skin")
    mp, mn = ca.on_ell(MC, MR, hd(0, 0.62, -0.78))
    ca.teeth(k, mp + mn * 0.004 + HUP * 0.014, unit(0, -1, -0.2), HUP, 0.021, 0.054, 0.009, "head")

    # Paint: the tan belly, the dark soles, the ears' insides, the smile, the scales
    def belly(P):
        q = P - np.array([0, -0.2, 0.25])
        return inside((q[:, 0] / 0.16) ** 2 + (q[:, 2] / 0.19) ** 2 - 1, 0.06) * inside(P[:, 1] + 0.08, 0.02)
    k.paint(belly, TAN, on=["hips", "chest"])
    k.paint(lambda P: inside(P[:, 2] - 0.012, 0.003), TEAL_DARK, on=["foot"])
    for s in (1, -1):
        m = np.array([s, 1, 1.0])
        k.decal((ep - en * 0.008) * m + unit(0, -1, 0.2) * 0.022, unit(0.3 * s, -1, 0.2), (0.017, 0.018), EAR_IN,
                eps=0.003, rings=6, segs=24)
    mouth = polygon([(-1, 0.5), (0, -0.3), (1, 0.5), (0.85, 0.85), (0, 0.2), (-0.85, 0.85)])
    for s in (1, -1):
        k.decal(mp + HUP * 0.014 + X * 0.034 * s, mn, (0.022, 0.014), INK, up=HUP, eps=0.003, outline=mouth, rings=6,
                segs=36)
    tn = unit(0.04, 0.06, 1)
    k.decal(TC + np.array([0, 0, 0.03]), tn, (0.2, 0.115), ca.scales(6, 3, SCALE, TEAL, 0.1), up=unit(0.47, 0.88, 0),
            eps=0.003, rings=18, segs=48)
    hip = np.array([0.27, 0.12, 0.22])
    k.decal(hip, unit(1, 0.25, -0.05), (0.08, 0.09), ca.scales(3, 3, SCALE, RED, 0.12,
            mask=lambda u, v: (u * u + v * v) < 0.8), up=(0, 0, 1), eps=0.003, rings=12, segs=40)
    k.build(voxel=0.0086, smooth=4, band=0.008, wband=0.05)
    ca.board(k.ob)

    # The line's eye, smaller, its top under a calm lid
    ep2, en2 = ca.on_ell(HC, HR, hd(0.44, 0.84, 0.3))
    base = ca.eye_open(1.1)
    LID = srgb(RED)
    def calm(u, v):
        c = base(u, v)
        c[v > 0.48] = LID
        c[np.abs(v - 0.48) < 0.12] = INK
        return c
    def lay(state):
        if state == "Fierce":
            def f(u, v):
                c = base(u, v)
                c[v > 0.15 + 0.45 * u] = LID
                c[np.abs(v - (0.15 + 0.45 * u)) < 0.13] = INK
                return c
            col = f
        else:
            col = eye_look(state, calm, LID, INK, line=0.18)
        k.decal(ep2, en2, (0.03, 0.033), col, up=HUP, eps=0.003, rings=20, segs=56, mirror=True)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    castoroi(*args())
