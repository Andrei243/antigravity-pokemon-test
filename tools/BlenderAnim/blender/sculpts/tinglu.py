# Ting-Lu sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/tinglu.py -- <the export's Ting-Lu.glb> <out.glb>
#
# From the references: a moose of ruin as heavy as a hill. A body of great rounded blocks of tan clay, banded in
# cream and rust red, highest at the shoulders; four thick block pillars of legs, each of two blocks and a dark
# foot, rust round the knees; a long thick neck carried level out of the shoulders to a small blocky face low at the
# front: small fierce red eyes, a rust line over its brow, two little red spikes at the front of its mouth, lumps of
# dark clay hanging under its jaw like a beard. On its neck and head the design's largest mass: a great ritual bowl
# of green bronze, tipped back so that its bottom hangs forward, a raised band under its rim, engraved panels on its sides, rust spots, a knob
# hanging from its bottom; from its rim rise prongs like antlers, a tall forked one at the back, a tall straight one
# at the front, and a short blocky one out of each side. A short dark stub of a tail.
# The skeleton: body (0, 0.02, 0.42), head (0, -0.3, 0.42), tail (0, 0.31, 0.48), legR/L (+-0.13, -0.18, 0.32),
# hindR/L (+-0.13, 0.2, 0.32).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, lathe, outward, srgb, inside, unit, polygon, eye_look, boxy, args

CLAY, CREAM, RUST, BROWN, DARK = "#C2A58C", "#E6D2BA", "#A63E2A", "#8C6B55", "#4E3A2E"
BRONZE, BRONZE2, BRONZEDK, RUSTSPOT = "#3F7769", "#5A968A", "#22423A", "#93402C"
EYE_R, PUPIL, INK, SPIKE = "#E8402E", "#2A0C0A", "#1E1412", "#D8382A"


def body(k):
    sq = boxy(3)
    k.add("chest", ell((0, -0.08, 0.46), (0.175, 0.17, 0.19), deform=sq), CLAY, "body")
    k.add("middle", ell((0, 0.08, 0.43), (0.165, 0.14, 0.16), deform=sq), CLAY, "body")
    k.add("rump", ell((0, 0.2, 0.43), (0.16, 0.12, 0.155), deform=sq), CLAY, "body")
    # great rounded plates over the shoulders and hips
    k.add("plate", ell((0.12, -0.12, 0.45), (0.075, 0.12, 0.16), deform=sq), CLAY, "leg{s}", mirror=True)
    k.add("plateh", ell((0.12, 0.2, 0.42), (0.07, 0.11, 0.15), deform=sq), CLAY, "hind{s}", mirror=True)
    # the bands: cream high, rust low, waving a little
    k.paint(lambda P: inside(np.abs(P[:, 2] - 0.53 - 0.012 * np.sin(P[:, 1] * 18)) - 0.022, 0.004), CREAM,
            on=["chest", "middle", "rump", "plate"])
    k.paint(lambda P: inside(np.abs(P[:, 2] - 0.4 - 0.012 * np.sin(P[:, 1] * 22 + 1)) - 0.024, 0.004), RUST,
            on=["chest", "middle", "rump", "plate"])
    k.paint(lambda P: inside(P[:, 2] - 0.335, 0.006), BROWN, on=["chest", "middle", "rump", "plate"])
    # a short dark stub of a tail
    k.add("tail", tube([(0, 0.3, 0.48), (0, 0.36, 0.45), (0, 0.39, 0.41)], [0.04, 0.032, 0.02]), DARK, "tail")


def legs(k):
    sq = boxy(3)
    for name, y, bone, back in (("fore", -0.18, "leg{s}", 0.0), ("hind", 0.2, "hind{s}", 0.025)):
        k.add(name + "up", ell((0.13, y, 0.24), (0.086, 0.092, 0.11), deform=sq), BROWN, bone, mirror=True)
        k.add(name + "low", ell((0.135, y + back, 0.11), (0.08, 0.086, 0.085), deform=sq), BROWN, bone, mirror=True)
        k.add(name + "foot", ell((0.138, y + back - 0.008, 0.032), (0.088, 0.096, 0.034), deform=sq), DARK, bone,
              mirror=True)
        k.paint(lambda P, y=y: inside(np.abs(P[:, 2] - 0.175) - 0.026, 0.004) * inside(np.abs(P[:, 1] - y) - 0.12),
                RUST, on=[name + "up", name + "low"])


def head(k):
    sq = boxy(3)
    k.add("neck", tube([(0, -0.18, 0.52), (0, -0.3, 0.5), (0, -0.4, 0.46)], [0.11, 0.095, 0.08], seg=20), CLAY, "head")
    k.paint(lambda P: inside(np.abs(P[:, 0]) - 0.03, 0.004) * inside(0.53 - P[:, 2], 0.004)
            * inside(P[:, 1] + 0.16, 0.004) * inside(-0.44 - P[:, 1], 0.004), RUST, on=["neck"])
    k.add("face", ell((0, -0.475, 0.41), (0.09, 0.095, 0.083), deform=sq), CLAY, "head")
    k.add("snout", ell((0, -0.545, 0.385), (0.07, 0.046, 0.057), deform=sq), CLAY, "head")
    # the rust line over its brow
    k.paint(lambda P: inside(np.abs(P[:, 2] - 0.452) - 0.01, 0.003) * inside(P[:, 1] + 0.51, 0.004), RUST,
            on=["face", "snout"])
    # lumps of dark clay hanging under the jaw
    for i, (x, y, z, r) in enumerate([(0, -0.47, 0.33, 0.038), (0.04, -0.45, 0.335, 0.03), (-0.04, -0.45, 0.335, 0.03),
                                      (0.0, -0.43, 0.3, 0.028), (0.02, -0.5, 0.31, 0.025), (-0.02, -0.5, 0.31, 0.025)]):
        k.add(f"lump{i}", ell((x, y, z), (r, r * 0.9, r * 1.2)), BROWN, "head")
    # the mouth's line and two little red spikes at its front
    k.decal((0, -0.59, 0.37), unit(0, -1, 0), (0.05, 0.005), INK, eps=0.003, rings=4, segs=32)
    for s in (1, -1):
        b = np.array([0.045 * s, -0.585, 0.365])
        k.add("spike" + ("R" if s > 0 else "L"), hull([b + (0.012, 0.006, 0), b + (-0.012, 0.006, 0), b + (0, -0.006, 0),
                                                       b + (0, 0.008, 0), b + (0.002 * s, -0.004, 0.035)]),
              SPIKE, "head", fuse=False, surface="shell")


# --- the bowl --------------------------------------------------------------------------------------------------------
VC = np.array([0.0, -0.28, 0.74])
VD = unit(0, 0.3, 0.95)              # its axis, tipped back: its bottom and front hang lower
VE = np.cross(VD, (1.0, 0, 0))       # the back of the bowl's own frame (b): x, b and the axis right-handed
VE = unit(*VE)


def V(a, b, s):
    return VC + a * np.array([1.0, 0, 0]) + b * VE + s * VD


def bowl_r(s):
    return float(np.interp(s, [-0.15, -0.1, 0.0, 0.08, 0.12], [0.08, 0.17, 0.215, 0.23, 0.235]))


def bowl(k):
    k.add("bowl", lathe(VC, VD, [(0, -0.15), (0.08, -0.15), (0.17, -0.1), (0.215, 0.0), (0.23, 0.08), (0.235, 0.12),
                                 (0.205, 0.12), (0.195, 0.05), (0.15, 0.03), (0, 0.03)], seg=48),
          BRONZE, "head", fuse=False, surface="metal")
    k.add("floor", lathe(VC, VD, [(0, 0.032), (0.197, 0.032), (0.199, 0.1), (0, 0.1)], seg=48), BRONZEDK, "head",
          fuse=False, surface="metal")
    k.add("band", lathe(VC, VD, [(0.22, 0.045), (0.245, 0.05), (0.248, 0.075), (0.235, 0.08)], seg=48), BRONZEDK,
          "head", fuse=False, surface="metal")
    # engraved panels on its sides: raised plates
    for i, (th, w, s0, s1) in enumerate([(-math.pi / 2, 0.42, -0.07, 0.03), (0.0, 0.38, -0.07, 0.03),
                                         (math.pi, 0.38, -0.07, 0.03), (-math.pi / 2 - 0.9, 0.3, -0.08, 0.02),
                                         (-math.pi / 2 + 0.9, 0.3, -0.08, 0.02)]):
        pts = []
        for t in np.linspace(th - w / 2, th + w / 2, 4):
            for s in (s0, s1):
                r = bowl_r(s)
                for dr in (-0.004, 0.009):
                    pts.append(V((r + dr) * math.cos(t), (r + dr) * math.sin(t), s))
        k.add(f"panel{i}", hull(pts), BRONZE2, "head", fuse=False, surface="metal")
    # rust spots
    for i, (t, s, r) in enumerate([(-1.0, -0.02, 0.022), (-2.3, 0.04, 0.018), (0.6, -0.05, 0.02), (2.4, -0.02, 0.02),
                                   (-0.2, 0.1, 0.015), (3.0, 0.09, 0.016)]):
        c = V(bowl_r(s) * math.cos(t), bowl_r(s) * math.sin(t), s)
        n = unit(*(c - V(0, 0, s)))
        a = np.cross(n, VD); a = unit(*a)
        pts = [c + n * 0.009 + a * r * math.cos(q) + VD * r * 0.8 * math.sin(q) for q in np.linspace(0, 2 * math.pi, 7, endpoint=False)]
        pts += [c - n * 0.008]
        k.add(f"rust{i}", hull(pts), RUSTSPOT, "head", fuse=False, surface="metal")
    # the knob hanging from its bottom
    b0 = V(0, -0.05, -0.14)
    k.add("knob", hull([b0 + (0.035 * math.cos(q), 0.035 * math.sin(q), 0.02) for q in np.linspace(0, 2 * math.pi, 6, endpoint=False)]
                       + [b0 + (0, -0.03, -0.075)]), BRONZE, "head", fuse=False, surface="metal")
    # prongs: a tall forked one at the back, a tall straight one at the front, a short block out of each side
    def prong(name, base, tip, w0, w1):
        base, tip = np.array(base), np.array(tip)
        d = unit(*(tip - base))
        a = unit(*np.cross(d, (0, 1.0, 0) if abs(d[1]) < 0.9 else (1.0, 0, 0))); b = np.cross(d, a)
        pts = [base + a * w0 * x + b * w0 * 0.8 * y for x in (-1, 1) for y in (-1, 1)]
        pts += [tip + a * w1 * x + b * w1 * 0.8 * y for x in (-1, 1) for y in (-1, 1)]
        k.add(name, hull(pts), BRONZE, "head", fuse=False, surface="metal")
    rb = V(-0.03, 0.2, 0.09)
    fork = rb + VD * 0.15 + VE * 0.03
    prong("backprong", rb, rb + VD * 0.25 + VE * 0.05, 0.036, 0.022)
    prong("fork", fork, fork + np.array([-0.08, 0.02, 0.07]), 0.026, 0.018)
    rf = V(0.04, -0.2, 0.09)
    prong("frontprong", rf, rf + VD * 0.2 - VE * 0.03 + np.array([0.02, 0, 0]), 0.032, 0.018)
    for s in (1, -1):
        sb = V(0.22 * s, 0.03, 0.02)
        prong("sideprong" + ("R" if s > 0 else "L"), sb, sb + np.array([0.07 * s, 0.0, 0.06]), 0.032, 0.026)


# --- the eyes: small fierce red eyes under the rust brow -------------------------------------------------------------
CR, CP, CK, CL = srgb(EYE_R), srgb(PUPIL), srgb(INK), srgb(CLAY)
OUT = [(-1, -0.4), (-0.4, 0.2), (0.4, 0.55), (1.0, 0.75), (0.8, -0.1), (0.2, -0.6), (-0.5, -0.65)]
_RIM = polygon(OUT)


def eye_open(u, v):
    c = np.tile(CR, (len(u), 1))
    c[np.hypot((u + 0.05) / 0.2, (v + 0.05) / 0.45) < 1] = CP
    rr = np.hypot(u, v) / np.array([_RIM(t) for t in np.arctan2(v, u)])
    c[rr > 0.78] = CK
    return c


def eyes(k, state):
    look = eye_look(state, eye_open, CL, CK, line=0.16)
    for s in (1, -1):
        n = unit(0.65 * s, -1, 0.25)
        c = np.array([0.056 * s, -0.545, 0.432])
        f = look if s > 0 else (lambda u, v, look=look: look(-u, v))
        k.decal(c, n, (0.03, 0.019), f, eps=0.002, outline=polygon([(x * s, y) for x, y in OUT]), rings=20, segs=56)


def tinglu(src, out):
    k = Kit(src, coat="shell")
    body(k)
    legs(k)
    head(k)
    bowl(k)
    k.build(voxel=0.008, smooth=4, band=0.008)
    k.eye_states(lambda st: eyes(k, st))
    k.export(out)


if __name__ == "__main__":
    tinglu(*args())
