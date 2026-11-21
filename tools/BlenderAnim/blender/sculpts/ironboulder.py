# Iron Boulder sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/ironboulder.py -- <the export's Iron Boulder.glb> <out.glb>
#
# Terrakion's future, from the references: a heavy machine of a bull, smooth pale grey metal. A great round body, and at
# its front a huge dark brown collar like a hood, out of which the grey face looks: yellow angular eyes under a dark
# brow of jagged spikes, a broad dark open mouth over a jutting chin. The brown runs back over the top in two humps.
# Round the head a frame of glowing orange crescent blades, one pair, sweeping out from the collar's top and down its
# sides beside two dark horn plates. Four thick legs of grey segments: a ball at the hip, a short thigh, a ball of a
# knee, then a boot flaring to the ground, a glowing orange cuff round its foot over a black split hoof, a short fin at
# the boot's top behind. A small orange block on each flank, and a short grey spike of a tail.
# (Terrakion's shapes are built anew here: the collar and the round body are this design's own, not its forebear's.)
# Bones: body (0, 0, 0.42); head (0, -0.2, 0.5); legs (+-0.1, -0.13, 0.32); hind legs (+-0.1, 0.16, 0.32);
# tail (0, 0.25, 0.48).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, hull, lathe, outward, srgb, inside, unit, polygon, eye_look, args, sibling)

r = sibling("raikou")
sui = sibling("suicune")

GREY = "#B6BAC2"
FACE = "#C2C6CD"
BROWN = "#4A3B35"
SPIKE = "#352A27"
ORANGE = "#FFA530"
BAR = "#E8622A"
HOOF = "#26262C"
MOUTH = "#2A2228"
EYE, INK = "#F4C832", "#1A1412"

BODY_C, BODY_R = np.array([0.0, 0.03, 0.5]), np.array([0.2, 0.27, 0.215])
COLLAR_C, COLLAR_R = np.array([0.0, -0.2, 0.525]), np.array([0.215, 0.15, 0.215])
FACE_C, FACE_R = np.array([0.0, -0.31, 0.52]), np.array([0.13, 0.075, 0.135])
CHIN_C, CHIN_R = np.array([0.0, -0.355, 0.445]), np.array([0.085, 0.05, 0.06])
LEG_X = 0.125
FORE_Y, HIND_Y = -0.14, 0.17
HIP_Z = 0.35
KNEE_Z = 0.205


def join(*shapes):
    """Several shapes as one piece (one glowing material for a pair)."""
    V, F, off = [], [], 0
    for v, f in shapes:
        V.append(np.asarray(v)); F.extend([[i + off for i in g] for g in f]); off += len(v)
    return np.vstack(V), F


def mirrored(shape):
    V, F = shape
    return V * np.array([-1, 1, 1.0]), [f[::-1] for f in F]


def add_body(k):
    k.add("body", ell(BODY_C, BODY_R), GREY, "body")
    k.add("belly", ell((0, 0.0, 0.36), (0.15, 0.2, 0.09)), GREY, "body")
    # the brown hood over the top: two humps, the back one higher, with a saddle between
    k.add("humpback", ell((0, 0.1, 0.69), (0.12, 0.14, 0.11)), BROWN, "body")
    k.add("humpfront", ell((0, -0.1, 0.7), (0.13, 0.12, 0.1)), BROWN, "head")
    k.add("collar", ell(COLLAR_C, COLLAR_R), BROWN, "head")
    k.add("face", ell(FACE_C, FACE_R), FACE, "head")
    k.add("chin", ell(CHIN_C, CHIN_R), FACE, "head")


def add_legs(k):
    for bone, y in (("leg{s}", FORE_Y), ("hind{s}", HIND_Y)):
        # a ball of a hip fused into the body, the thigh, a ball of a knee
        k.add(f"hip{bone[0]}", ell((LEG_X, y, HIP_Z), (0.085, 0.09, 0.085)), GREY, bone, mirror=True)
        k.add(f"thigh{bone[0]}", tube([(LEG_X, y, HIP_Z), (LEG_X + 0.005, y, KNEE_Z)], [0.072, 0.066]), GREY, bone,
              mirror=True)
        k.add(f"knee{bone[0]}", ell((LEG_X + 0.005, y, KNEE_Z), (0.06, 0.062, 0.055)), GREY, bone, mirror=True)
        # the boot: a lathe flaring to the ground
        c = np.array((LEG_X + 0.005, y, 0.0))
        k.add(f"boot{bone[0]}", lathe(c, (0, 0, 1), [(0, 0.04), (0.088, 0.04), (0.082, 0.1), (0.066, 0.2),
                                                       (0.06, 0.225), (0, 0.225)], seg=40),
              GREY, bone, fuse=False, mirror=True, surface="metal")
        # the short pointed tab on the boot's top, outside, leaning out
        b = c + np.array((0.045, 0.0, 0.15))
        k.add(f"fin{bone[0]}", hull([b + (0.0, -0.04, 0.0), b + (0.035, -0.04, 0.0), b + (0.0, 0.04, 0.0),
                                     b + (0.035, 0.04, 0.0), b + (0.0, -0.03, 0.06), b + (0.0, 0.03, 0.06),
                                     b + (0.055, 0.0, 0.11)]),
              GREY, bone, fuse=False, mirror=True, surface="metal")
        # the hoof: black, split in two toes at the front
        for j, dx in enumerate((-0.032, 0.032)):
            h = c + np.array((dx, -0.03, 0.0))
            k.add(f"hoof{bone[0]}{j}", hull([h + (-0.03, 0.05, 0.0), h + (0.03, 0.05, 0.0), h + (-0.03, 0.05, 0.05),
                                             h + (0.03, 0.05, 0.05), h + (-0.022, -0.06, 0.0), h + (0.022, -0.06, 0.0),
                                             h + (0.0, -0.065, 0.022), h + (-0.02, -0.02, 0.05), h + (0.02, -0.02, 0.05)]),
                  HOOF, bone, fuse=False, mirror=True, surface="metal")


def add_cuffs(k):
    """The glowing orange cuff round each foot, one piece a leg."""
    for bone, y in (("leg", FORE_Y), ("hind", HIND_Y)):
        for s, side in ((1, "R"), (-1, "L")):
            c = np.array((s * (LEG_X + 0.005), y, 0.0))
            ring = lathe(c, (0, 0, 1), [(0.075, 0.05), (0.094, 0.05), (0.092, 0.128), (0.073, 0.128)], seg=48)
            k.add(f"cuff{bone[0]}{side}", ring, ORANGE, bone + side, fuse=False, glow=ORANGE, surface="metal")


def add_crescents(k):
    """The frame of glowing crescents round the head: one pair, from the collar's top out and down its sides."""
    f = FACE_C
    path = sui.smooth_path([f + (0.05, 0.06, 0.2), f + (0.15, 0.03, 0.18), f + (0.22, -0.02, 0.1),
                            f + (0.25, -0.04, -0.02), f + (0.225, -0.03, -0.11)])
    across = [unit(0.15, 0, 1), unit(0.55, 0, 0.85), unit(0.85, 0, 0.5), unit(1, 0, 0.0), unit(0.95, 0, -0.3)]
    right = sui.ribbon(path, lambda u: 0.05 * (1 - 0.55 * u ** 2), 0.03, across)
    k.add("crescent", join(right, mirrored(right)), ORANGE, "head", fuse=False, glow=ORANGE, surface="metal")


def add_horns(k):
    """Two dark horn plates hanging from the collar's sides, out and down, behind the crescents."""
    f = FACE_C
    b = f + np.array((0.17, 0.08, 0.1))
    pts = [b + (0, -0.05, 0.04), b + (0, 0.05, 0.04), b + (0.0, -0.04, -0.04), b + (0, 0.05, -0.04),
           b + (0.045, -0.02, 0.0), b + (0.045, 0.03, 0.0), b + (0.12, 0.0, -0.13), b + (0.1, 0.03, -0.16)]
    k.add("horn", hull(pts), SPIKE, "head", fuse=False, mirror=True, surface="metal")


def add_spikes(k):
    """The dark jagged brow: spikes on the collar's top over the face, leaning forward and out."""
    top = FACE_C + np.array((0.0, 0.06, 0.18))
    for j, (dx, tip) in enumerate(((0.0, (0.0, -0.06, 0.07)), (0.07, (0.09, -0.04, 0.06)),
                                   (-0.07, (-0.09, -0.04, 0.06)))):
        b = top + np.array((dx, 0.0, -0.01 if dx else 0.0))
        t = b + np.array(tip)
        pts = [b + (0.036, -0.02, 0), b + (-0.036, -0.02, 0), b + (0.03, 0.035, 0), b + (-0.03, 0.035, 0),
               b + (0, -0.02, -0.03), b + (0, 0.03, -0.03), t]
        k.add(f"spike{j}", hull(pts), SPIKE, "head", fuse=False, surface="metal")


def add_bars(k):
    """A small orange block on each upper flank, behind the shoulder."""
    c = np.array((0.185, 0.08, 0.6))
    pts = [c + (sx, sy, sz) for sx in (-0.012, 0.016) for sy in (-0.045, 0.045) for sz in (-0.022, 0.022)]
    pts += [c + (0.01, 0.07, 0.0)]
    k.add("bar", hull(pts), BAR, "body", fuse=False, mirror=True, surface="metal")


def add_tail(k):
    b = np.array((0.0, 0.27, 0.53))
    pts = [b + (0.025, 0, -0.025), b + (-0.025, 0, -0.025), b + (0.025, 0, 0.025), b + (-0.025, 0, 0.025),
           b + (0.03, 0.15, -0.005), b + (0.02, 0.12, 0.02)]
    k.add("tail", hull(pts), GREY, "tail", fuse=False, surface="metal")


def add_mouth(k):
    """The broad dark open mouth on the lower face, over the jutting chin."""
    p, n = r.on_ell(FACE_C, FACE_R, (0, -1, -0.42))
    k.decal(p, n, (0.058, 0.029), MOUTH, eps=0.003,
            outline=polygon([(-1, -1), (1, -1), (0.85, 0.55), (0.4, 1), (-0.4, 1), (-0.85, 0.55)]), rings=10, segs=64)


# --- the eyes: yellow, angular, the top edge sloping down toward the nose, a dark slit toward the nose -----------------
EYE_DIR = unit(0.4, -1, 0.5)
EYE_HALF = (0.04, 0.029)
SHAPE = [(-1.0, -0.2), (-0.55, -0.85), (0.7, -0.75), (1.0, 0.15), (0.65, 0.95), (-0.75, 0.35)]


def eyes(k, state):
    p, n = r.on_ell(FACE_C, FACE_R, EYE_DIR)
    Y, K, F = srgb(EYE), srgb(INK), srgb(FACE)
    def glare(u, v):
        c = np.tile(Y, (len(u), 1))
        c[((u + 0.3) / 0.22) ** 2 + ((v + 0.05) / 0.62) ** 2 < 1] = K
        return c
    if state == "Fierce":
        def look(u, v):
            c = glare(u, v)
            c[v > 0.1 + 0.55 * u] = F
            return c
    else:
        look = eye_look(state, glare, F, K, line=0.18)
    for s in (1, -1):
        m = np.array([s, 1, 1.0])
        outer = polygon([(s * a * 1.22, b * 1.22) for a, b in SHAPE])
        inner = polygon([(s * a, b) for a, b in SHAPE])
        f = look if s > 0 else (lambda u, v, lk=look: lk(-u, v))
        k.decal(p * m, n * m, EYE_HALF, INK, eps=0.0018, outline=outer, rings=12, segs=72)
        k.decal(p * m, n * m, EYE_HALF, f, eps=0.003, outline=inner, rings=20, segs=72)


if __name__ == "__main__":
    src, dst = args()
    k = Kit(src, coat="metal")
    add_body(k)
    add_legs(k)
    add_mouth(k)
    add_cuffs(k)
    add_crescents(k)
    add_horns(k)
    add_spikes(k)
    add_bars(k)
    add_tail(k)
    k.build(voxel=0.0065, band=0.004)
    k.eye_states(lambda s: eyes(k, s))
    k.export(dst)
