# Mandibuzz sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/mandibuzz.py -- <the export's Mandibuzz.glb> <out.glb>
#
# Grown from vullaby.py (its beak, feathers, feet and colours). From the references: a tall vulture some 0.7 tall. A
# bald pink head, longer than round, with a grey hooked beak, red eyes under a heavy black lid that flicks back like
# eyeliner, a pink knob on the crown; a thick pink neck running down into a pale tan ruff and chest whose lower edge
# is ragged; the body dark brown; great folded wings like a cloak at the sides, dark brown at the shoulder with long
# grey primaries and dark feathers under them hanging nearly to the feet; a skirt of pale bones hanging from the
# waist all round the front; dark tail feathers behind; pink legs, three toes forward and one back, grey claws. A tuft
# of dark feathers sweeps back from the back of the head with a knobbed bone tied across it.
# The joints: body (0, 0, 0.32), head (0, -0.02, 0.47), wingR/L (+-0.08, 0, 0.42), legR/L (+-0.05, 0, 0.18).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, blade, slab, hull, outward, srgb, inside, unit, tri, lumpy, polygon, eye_look,
                       sibling, args)

v = sibling("vullaby")
PINK, BEAK, CLAW, INK, EYEW, RED = v.PINK, v.BEAK, v.CLAW, v.INK, v.EYEW, v.RED
BROWN, DARK, TAN, BONE, GREY, KNOB = "#625249", "#4A3E39", "#D6C29C", "#EADBB0", "#A39A94", "#E7A2AE"

HEAD_C, HEAD_R = np.array([0.0, -0.056, 0.594]), np.array([0.075, 0.09, 0.075])
EYE_C = np.array([0.054, -0.098, 0.612])
EYE_N = unit(0.62, -0.74, 0.18)


def feather_slab(k, name, origin, along, across, length, width, color, bone, thick=0.022, mirror=True):
    """A long flat feather from origin along `along`, `width` across, pointed at its end: a loose plate."""
    L, w = length, width
    out = [(0.0, -w), (0.55 * L, -w), (0.88 * L, -0.6 * w), (L, 0.0), (0.88 * L, 0.6 * w), (0.55 * L, w), (0.0, w)]
    k.add(name, slab(out, origin, along, across, thick), color, bone, fuse=False, mirror=mirror)


def mandibuzz(src, out):
    k = Kit(src, coat="fur")

    # --- the body: dark brown, a tan chest and ruff ------------------------------------------------------------
    k.add("torso", ell((0.0, 0.012, 0.345), (0.124, 0.108, 0.175)), BROWN, "body")
    k.add("ruff", ell((0.0, -0.01, 0.47), (0.09, 0.084, 0.045), deform=lumpy(7, 2, 0.12)), BROWN, "body")
    k.add("chest", ell((0.0, -0.05, 0.42), (0.09, 0.06, 0.1)), BROWN, "body")

    def chest(P):
        edge = 0.285 + 0.028 * (tri(P[:, 0] * 14 + 0.5) + 1) / 2
        return (inside(edge - P[:, 2], 0.003) * inside(P[:, 1] + 0.01, 0.01)
                * inside(np.abs(P[:, 0]) - 0.105, 0.006) * inside(P[:, 2] - 0.53, 0.01))
    k.paint(chest, TAN, on=["torso", "ruff", "chest"])
    # the ruff's ragged points round the base of the neck, tan in front and brown behind
    for i in range(16):
        a = 2 * math.pi * (i + 0.5) / 16
        d = np.array([math.sin(a), -math.cos(a), 0.0])
        base = np.array([0.0, -0.01, 0.47]) + d * np.array([0.078, 0.072, 0])
        L = 0.045 + 0.014 * (i % 2)
        tip = base + d * L * 0.45 + np.array([0, 0, -L])
        side = np.cross(d, (0, 0, 1.0))
        if math.cos(a) > 0.95:
            continue                                         # the pink wattle runs down the front there
        col = TAN if math.cos(a) > -0.2 else BROWN          # a = 0 is the front
        v.feather(k, f"ruffpt{i}", base, tip, 0.026, col, "body", flat=np.cross(side, tip - base), f=0.3)

    # --- the neck and head: bare and pink -----------------------------------------------------------------------
    k.add("neck", tube([(0.0, 0.0, 0.46), (0.0, -0.025, 0.52), (0.0, -0.045, 0.565)], [0.042, 0.039, 0.04]), PINK,
          "head", surface="skin")
    k.add("wattle", ell((0.0, -0.078, 0.465), (0.04, 0.034, 0.075)), PINK, "head", surface="skin")
    k.add("head", ell(HEAD_C, HEAD_R), PINK, "head", surface="skin")
    k.add("knob", ell((0.0, -0.025, 0.668), (0.022, 0.024, 0.02)), KNOB, "head", surface="skin")
    v.beak(k, HEAD_C + np.array([0.0, -0.075, 0.004]), 0.09, 0.034)

    # The tuft at the back of the head, sweeping back and up, a knobbed bone tied across it
    for i, (tip, r) in enumerate((((0.0, 0.18, 0.672), 0.028), ((-0.05, 0.16, 0.655), 0.026),
                                  ((0.05, 0.155, 0.648), 0.024))):
        v.feather(k, f"tuft{i}", (0.0, 0.01, 0.625), tip, r, DARK, "head", flat=(1, 0, 0), f=0.34)
    k.add("bone", tube([(-0.05, 0.105, 0.652), (0.05, 0.105, 0.652)], [0.012, 0.012]), BONE, "head", fuse=False,
          surface="shell")
    for x in (-0.056, 0.056):
        for dz in (-0.011, 0.011):
            k.add(f"boneknob{x}{dz}", ell((x, 0.105, 0.652 + dz), (0.014, 0.013, 0.013)), BONE, "head", fuse=False,
                  surface="shell")

    # --- the wings: a cloak at each side, brown at the shoulder, long grey primaries and dark feathers under ------
    k.add("shoulder", ell((0.122, 0.02, 0.385), (0.052, 0.118, 0.158), rot=(-8, 0, -4)), BROWN, "wing{s}", mirror=True)
    k.add("mantle", ell((0.0, 0.07, 0.4), (0.13, 0.065, 0.14)), BROWN, "body")
    for i in range(4):
        o = (0.152 + 0.005 * i, -0.075 + 0.034 * i, 0.37)
        feather_slab(k, f"dark{i}", (o[0] - 0.006, o[1] + 0.01, o[2]), unit(0.1 + 0.03 * i, 0.22 + 0.08 * i, -1), unit(0, 1, 0.3),
                     0.3 - 0.012 * i, 0.03, DARK, "wing{s}", thick=0.02)
        feather_slab(k, f"prim{i}", o, unit(0.1 + 0.03 * i, 0.18 + 0.08 * i, -1), unit(0, 1, 0.25), 0.235 - 0.014 * i, 0.034, GREY,
                     "wing{s}", thick=0.022)

    # --- the waist: a band of bone and a skirt of pointed bones hanging all round the front --------------------
    k.add("waist", ell((0.0, 0.012, 0.222), (0.13, 0.115, 0.028)), BONE, "body", surface="shell")
    n = 15
    for i in range(n):
        a = math.radians(-125 + 250 * i / (n - 1))
        d = np.array([math.sin(a), -math.cos(a), 0.0])
        side = np.cross((0, 0, 1.0), d)
        top = np.array([0.0, 0.012, 0.228]) + d * np.array([0.122, 0.109, 0])
        L = 0.12 + 0.03 * (i % 2) - 0.02 * abs(math.sin(a)) ** 3
        tip = top + d * 0.03 + np.array([0, 0, -L])
        w, t = 0.018, 0.011
        pts = [top + side * w + d * t, top - side * w + d * t, top + side * w - d * t, top - side * w - d * t,
               top + side * w * 0.7 + d * 0.012 + np.array([0, 0, -0.55 * L]),
               top - side * w * 0.7 + d * 0.012 + np.array([0, 0, -0.55 * L]), tip]
        k.add(f"skirt{i}", hull(pts), BONE, "body", fuse=False, surface="shell")

    # --- the tail: dark feathers fanned behind, low --------------------------------------------------------------
    for i, x in enumerate((-0.05, 0.0, 0.05)):
        feather_slab(k, f"tail{i}", (x * 0.6, 0.08, 0.21), unit(x * 1.4, 0.55, -1), unit(1, 0, 0), 0.17, 0.03, DARK,
                     "body", thick=0.022, mirror=False)

    # --- the legs ----------------------------------------------------------------------------------------------
    for s, side in ((1, "R"), (-1, "L")):
        v.foot(k, side, 0.054 * s, 0.19, y=-0.01, r=0.026, toe=0.064, scale=1.1)

    # --- the eyes: red under a heavy black lid flicking back like eyeliner ---------------------------------------
    CW, CR, CK, CP = srgb(EYEW), srgb(RED), srgb(INK), srgb(PINK)
    ALMOND = [(-1.0, -0.05), (-0.6, -0.5), (0.0, -0.62), (0.6, -0.45), (0.95, -0.08), (1.3, 0.55), (0.7, 0.42),
              (0.0, 0.6), (-0.6, 0.45)]

    def open_eye(u, v_, top=0.12):
        c = np.tile(CK, (len(u), 1))
        win = ((u / 0.82) ** 2 + ((v_ + 0.12) / 0.42) ** 2 < 1) & (v_ < top)
        c[win] = CW
        r = np.sqrt((u + 0.12) ** 2 + (v_ + 0.12) ** 2)
        c[win & (r < 0.38)] = CR
        c[win & (r < 0.16)] = CK
        return c

    def state_look(state):
        if state == "Fierce":
            return lambda u, v_: open_eye(u, v_, top=-0.05 - 0.2 * u)
        if state == "Open":
            return open_eye
        base = eye_look(state, open_eye, CP, CK, line=0.2)

        def closed(u, v_):
            c = base(u, v_)
            c[(v_ > 0.25) | (u > 0.95)] = CK          # the eyeliner's flick stays
            return c
        return closed

    def eyes(state):
        look = state_look(state)
        for s in (1, -1):
            outline = polygon([(s * a, b) for a, b in ALMOND])
            col = look if s == 1 else (lambda u, v_, f=look: f(-u, v_))
            k.decal(EYE_C * np.array([s, 1, 1]), EYE_N * np.array([s, 1, 1]), (0.032, 0.022), col, outline=outline,
                    eps=0.003, rings=20, segs=56)

    k.build(voxel=0.006, smooth=4, band=0.005)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    mandibuzz(*args())
