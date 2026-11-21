# Vullaby sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/vullaby.py -- <the export's Vullaby.glb> <out.glb>
#
# From the references: a small vulture chick some 0.43 tall. A bald pink round head with fierce eyes (white, a red
# iris round a black pupil, a dark brow slanting down to the beak), a small grey hooked beak, and a tuft of four dark
# feathers rising from the back of the crown, leaning to its +X side. A grey-brown ragged ruff round the neck; a dark
# brown body sitting in the bottom half of a broken cream skull worn like a nappy: its top rim jagged, a heart-shaped
# hole in front, the fat dark brown thighs coming out of it at the front. Short pink legs, three toes forward and one
# back, grey claws. Short wings held out and down, dark brown with pale grey feather tips.
# The joints: body (0, 0, 0.14), head (0, -0.01, 0.23), wingR/L (+-0.07, 0, 0.2), legR/L (+-0.04, 0, 0.08).
#
# Mandibuzz (mandibuzz.py) is grown from this frame: its beak, eye, feather, foot and colours are these.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, blade, slab, hull, outward, srgb, inside, unit, tri, lumpy, polygon, eye_look,
                       args)

PINK, BROWN, RUFF, BONE, GREY, BEAK, CLAW = "#D98794", "#4E4341", "#8A817B", "#EADFC0", "#B4ACA6", "#8E8C92", "#B8B6BC"
EYEW, RED, INK = "#F8F6F2", "#D8303A", "#16121A"

HEAD_C, HEAD_R = np.array([0.0, -0.014, 0.304]), np.array([0.074, 0.067, 0.06])
EYE_N = unit(0.46, -0.88, 0.12)


def beak(k, c, length, r, bone="head"):
    """A small grey hooked beak from c forward along -Y, its tip turned down: a loose piece."""
    c = np.array(c, float)
    pts = [c, c + np.array([0, -0.55 * length, -0.04 * length]), c + np.array([0, -0.9 * length, -0.2 * length]),
           c + np.array([0, -1.0 * length, -0.5 * length])]
    k.add("beak", tube(pts, [r, 0.75 * r, 0.42 * r, 0.12 * r]), BEAK, bone, fuse=False, surface="shell")


def feather(k, name, base, tip, r, color, bone, flat=(0, 1, 0), f=0.4):
    """A thick tapering feather from base to tip, flattened along `flat`: a fan of these is a tuft."""
    base, tip = np.array(base, float), np.array(tip, float)
    mid = base + (tip - base) * 0.5 + np.array([0, 0.006, 0])
    k.add(name, blade([base, mid, tip], [r, 0.85 * r, 0.12 * r], flat, f), color, bone, fuse=False)


def foot(k, side, x, top, y=-0.02, r=0.014, toe=0.045, color=PINK, claw=CLAW, scale=1.0):
    """A short pink leg from top to the ground, three toes forward and one back, a grey claw on each."""
    bone = f"leg{side}"
    k.add(f"shin{side}", tube([(x, y + 0.005, top), (x * 1.03, y, 0.5 * top), (x * 1.05, y - 0.003, 0.9 * r)],
                              [r, r * 0.95, r * 0.95]), color, bone, surface="skin")
    base = np.array([x * 1.05, y - 0.003, 0.8 * r])
    for i, ang in enumerate((-32, 0, 32, 180)):
        a = math.radians(ang)
        d = np.array([math.sin(a), -math.cos(a), 0])
        L = toe if ang != 180 else toe * 0.6
        tip = base + d * L + np.array([0, 0, -0.35 * r])
        k.add(f"toe{side}{i}", tube([base, (base + tip) / 2 + np.array([0, 0, 0.1 * r]), tip],
                                    [0.78 * r, 0.7 * r, 0.62 * r]), color, bone, surface="skin")
        k.add(f"claw{side}{i}", tube([tip + d * 0.3 * r, tip + (d + np.array([0, 0, -0.6])) * 0.85 * r * 1.6 * scale],
                                     [0.5 * r, 0.12 * r]), claw, bone, fuse=False, surface="shell")


def fierce_eye(white, iris, pupil, lid, ink, slope=0.35, at=0.22, ring=0.0):
    """White with a red iris round a black pupil, a dark brow slanting down toward the beak (u inward is negative)."""
    CW, CI, CP, CL, CK = srgb(white), srgb(iris), srgb(pupil), srgb(lid), srgb(ink)

    def eye(u, v):
        c = np.tile(CW, (len(u), 1))
        r = np.sqrt((u + 0.1) ** 2 + (v + 0.1) ** 2)
        c[r < 0.56] = CI
        c[r < 0.26] = CP
        if ring:
            c[u * u + v * v > (1 - ring) ** 2] = CK
        brow = at + slope * u
        c[v > brow] = CL
        c[np.abs(v - brow) < 0.15] = CK
        return c
    return eye


def vullaby(src, out):
    k = Kit(src, coat="fur")

    # --- the head: bald and pink, round; a small hooked beak; a tuft of four feathers -------------------------------
    k.add("head", ell(HEAD_C, HEAD_R), PINK, "head", surface="skin")
    k.add("neck", ell((0.0, -0.004, 0.252), (0.05, 0.048, 0.04)), PINK, "head", surface="skin")
    beak(k, HEAD_C + np.array([0.0, -0.056, -0.006]), 0.04, 0.016)
    for i, (tip, r) in enumerate((((0.004, 0.056, 0.435), 0.024), ((0.044, 0.056, 0.42), 0.022),
                                  ((-0.028, 0.052, 0.41), 0.02), ((0.074, 0.05, 0.392), 0.019))):
        feather(k, f"tuft{i}", (0.006 * i - 0.004, 0.026, 0.338), tip, r, BROWN, "head", flat=(0, 1, 0), f=0.32)

    # --- the ruff: a ragged grey-brown collar of fur round the neck, its points hanging down and out ----------------
    k.add("ruff", ell((0.0, 0.0, 0.248), (0.09, 0.082, 0.04), deform=lumpy(7, 2, 0.12)), RUFF, "body")
    for i in range(18):
        a = 2 * math.pi * (i + 0.5) / 18
        d = np.array([math.sin(a), -math.cos(a), 0.0])
        base = np.array([0.0, 0.0, 0.25]) + d * np.array([0.074, 0.068, 0])
        L = 0.034 + 0.012 * (i % 2)
        tip = base + d * L * 0.45 + np.array([0, 0, -L])
        side = np.cross(d, (0, 0, 1.0))
        feather(k, f"ruffpt{i}", base, tip, 0.022, RUFF, "body", flat=np.cross(side, tip - base), f=0.3)

    # --- the body: dark brown, in the bottom half of a broken skull ---------------------------------------------
    k.add("body", ell((0.0, 0.004, 0.19), (0.088, 0.082, 0.062)), BROWN, "body")
    k.add("shell", ell((0.0, 0.004, 0.138), (0.116, 0.104, 0.09)), BONE, "body", surface="shell")
    k.add("thigh", ell((0.058, -0.036, 0.082), (0.05, 0.052, 0.056)), BROWN, "leg{s}", mirror=True)
    k.add("rump", ell((0.0, 0.03, 0.09), (0.07, 0.06, 0.05)), BONE, "body", surface="shell")

    # The skull's broken rim: dark brown above a jagged line round the body
    def rim(P):
        a = np.arctan2(P[:, 0], -P[:, 1] + 0.004)
        edge = 0.196 + 0.011 * tri(a * 7 / (2 * math.pi)) + 0.006 * np.sin(3 * a)
        return inside(edge - P[:, 2], 0.0025) * inside(P[:, 2] - 0.27, 0.003)
    k.paint(rim, BROWN, on=["shell", "body", "rump"])

    # The heart-shaped hole in front, dark
    HEART = polygon([(0.0, -1.0), (0.62, -0.18), (0.95, 0.35), (0.82, 0.78), (0.48, 0.95), (0.18, 0.78), (0.0, 0.5),
                     (-0.18, 0.78), (-0.48, 0.95), (-0.82, 0.78), (-0.95, 0.35), (-0.62, -0.18)])
    k.decal((0.0, -0.1, 0.158), unit(0, -1, 0.1), (0.024, 0.022), BROWN, outline=HEART, eps=0.003, rings=12, segs=80)
    # Two short cracks in the bone
    for x, z, s in ((0.07, 0.15, 1), (-0.085, 0.17, -1)):
        n = unit(0.8 * s, -0.6, 0.05)
        CRACK = polygon([(-1, 0.25), (-0.3, 0.12), (0.1, 0.5), (1, 0.35), (1, 0.05), (0.15, 0.0), (-0.2, -0.3), (-1, -0.1)])
        k.decal((x, -0.072, z), n, (0.022, 0.012), "#9E9178", outline=CRACK, eps=0.003, rings=8, segs=60)

    # --- the wings: short, held out, forward and down; dark brown with pale grey feather tips -------------------
    ex, ez = unit(1.0, -0.28, -0.4), unit(0.0, 0.35, 1.0)
    WING = [(0.0, 0.034), (0.06, 0.032), (0.11, 0.024), (0.15, 0.011), (0.168, -0.004), (0.142, -0.012),
            (0.156, -0.026), (0.126, -0.03), (0.132, -0.047), (0.098, -0.043), (0.05, -0.046), (0.0, -0.04)]
    TIPS = [(0.094, 0.026), (0.11, 0.024), (0.15, 0.011), (0.168, -0.004), (0.142, -0.012), (0.156, -0.026),
            (0.126, -0.03), (0.132, -0.047), (0.098, -0.043), (0.094, -0.0)]
    W0 = (0.062, 0.0, 0.212)
    k.add("wing", slab(WING, W0, ex, ez, 0.016), BROWN, "wing{s}", fuse=False, mirror=True)
    k.add("wingtip", slab(TIPS, W0, ex, ez, 0.021), GREY, "wing{s}", fuse=False, mirror=True)

    # --- the legs ----------------------------------------------------------------------------------------------
    for s, side in ((1, "R"), (-1, "L")):
        foot(k, side, 0.046 * s, 0.06, y=-0.03, r=0.0145, toe=0.042)

    # --- the eyes: fierce, white with a red iris, a dark brow slanting to the beak --------------------------------
    eye = fierce_eye(EYEW, RED, INK, PINK, INK)

    def eyes(state):
        if state == "Fierce":
            look = fierce_eye(EYEW, RED, INK, PINK, INK, slope=0.6, at=0.08)
        else:
            look = eye_look(state, eye, srgb(PINK), srgb(INK), line=0.16)
        k.decal(HEAD_C + np.array([0.0, 0.0, 0.01]) + EYE_N * HEAD_R * 0.99, EYE_N, (0.017, 0.019), look, eps=0.003,
                rings=20, segs=56, mirror=True)

    k.build(voxel=0.0045, smooth=4, band=0.004)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    vullaby(*args())
