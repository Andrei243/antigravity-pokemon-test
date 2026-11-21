# Coalossal sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/coalossal.py -- <the export's Coalossal.glb> <out.glb>
# Grown from rolycoly.py and carkol.py (sibling): the line's coal, its faceted stones and its heap of coals red-hot in
# the cracks; Carkol's slanting white eyes and orange mouth grown into a furnace's jaw.
#
# The design, from the references: a giant of dark grey faceted stone standing upright. Its body a broad pear, the belly
# in large plates, coming down between two great round thighs; short thick shins and wide feet with three claws. Arms
# of faceted boulders, bent, the fists forward and down. The head low in front of the shoulders, a block with a flat
# face: two white eyes under heavy brows slanting to the nose, a wide jaw glowing orange with jagged teeth; a round
# stone like an ear on each side of the head. Out of the top of the head the heap of coal, red-hot in its cracks, a
# tall cone (a third of the height). A tall pointed pillar of rock rises out of the back behind its left shoulder.
# The body is the largest mass (the pictures overrule the brief's "the hump the largest mass").
# The skeleton: body (0, 0, 0.45), arms (+-0.19, 0, 0.56), head (0, 0.02, 0.636), legs (+-0.1, 0, 0.24).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, hull, slab, srgb, unit, eye_look, polygon, args, sibling

ro = sibling("rolycoly")
rg = ro.rg

GREY, GREY_LT, GREY_DK = "#4E4C52", "#5E5C64", "#3E3C42"
EYE_W, EYE_INK, PUPIL = "#F4F2EE", "#1C1A20", "#2A2830"
JAW, JAW_HI, JAW_RIM = "#FF7A1E", "#FFC23C", "#9A2A16"

HEAD_C, HEAD_R = np.array([0.0, -0.1, 0.625]), (0.135, 0.125, 0.11)
FRONT_N, FRONT_D = unit(0, -1, 0.04), 0.86
FACE_HALF = (0.112, 0.08)
EYE_OFF = np.array([0.05, 0.0, 0.032])
EYE_HALF = (0.035, 0.025)
EYE_PTS = [(1.0, 0.7), (-1.0, 0.1), (-0.9, -0.45), (-0.35, -0.9), (0.4, -0.88), (0.92, -0.35)]
JAW_OFF = np.array([0.0, 0.0, -0.034])
JAW_HALF = (0.08, 0.036)


def stone(c, r, seed):
    rs = np.random.RandomState(seed)
    pl = rg.ball_planes(seed, jitter=0.04, turn=tuple(rs.rand(3) * 90))
    return rg.stone(c, r, pl)


def eye_open(u, v):
    out_r = polygon(EYE_PTS)
    rr = np.sqrt(u * u + v * v)
    lim = np.array([out_r(math.atan2(b, a)) for a, b in zip(u, v)])
    c = np.tile(srgb(EYE_W), (len(u), 1))
    c[((u + 0.28) / 0.3) ** 2 + ((v + 0.12) / 0.42) ** 2 < 1] = srgb(PUPIL)
    c[rr > lim * 0.8] = srgb(EYE_INK)
    return c


def jaw(u, v):
    r = np.sqrt(u * u + v * v)
    c = np.tile(srgb(JAW), (len(u), 1))
    c[(u / 0.62) ** 2 + ((v + 0.05) / 0.55) ** 2 < 1] = srgb(JAW_HI)
    c[r > 0.84] = srgb(JAW_RIM)
    return c


def coalossal(src, out):
    k = Kit(src, coat="shell")
    lot = ro.Lot()
    # legs: great round thighs fused into the hips, short thick shins, wide feet with three claws
    for s, bone in ((1, "legR"), (-1, "legL")):
        lot.put(stone((s * 0.135, -0.005, 0.25), (0.135, 0.135, 0.145), 11 + (s > 0)), GREY, bone)
        lot.put(stone((s * 0.13, -0.01, 0.1), (0.078, 0.08, 0.075), 21 + (s > 0)), GREY_DK, bone)
        lot.put(rg.chunk((s * 0.135, -0.035, 0.032), (0.085, 0.1, 0.034), 31 + (s > 0), n=6, squash_bottom=0.0),
                GREY_LT, bone)
        for j, dx in enumerate((-0.05, 0.0, 0.05)):
            b = np.array([s * 0.135 + dx * 1.1, -0.12, 0.02])
            lot.put(hull([b + (-0.022, 0.03, -0.02), b + (0.022, 0.03, -0.02), b + (-0.02, 0.03, 0.022),
                          b + (0.02, 0.03, 0.022), b + (0.0, -0.035, -0.012), b + (0.0, -0.02, 0.008)]),
                    GREY_DK, bone)
    # the body: a broad pear, the belly coming down between the thighs, its front in large lighter plates
    lot.put(stone((0, 0.005, 0.37), (0.215, 0.185, 0.2), 41), GREY, "body")
    lot.put(stone((0, 0.02, 0.56), (0.215, 0.165, 0.13), 42), GREY, "body")
    for (x, z, r, t) in ((-0.075, 0.44, 0.07, 10), (0.075, 0.44, 0.07, 35), (-0.07, 0.31, 0.068, 20),
                         (0.07, 0.31, 0.068, 5), (0.0, 0.22, 0.06, 15), (0.0, 0.52, 0.06, 0)):
        p = np.array([x, 0.0, z])
        # the belly's surface ahead of the point, as the pear's ellipsoid has it
        y = -0.185 * math.sqrt(max(0.05, 1 - (x / 0.215) ** 2 - ((z - 0.37) / 0.2) ** 2)) + 0.005
        n = unit(x / 0.215 ** 2, y / 0.185 ** 2, (z - 0.37) / 0.2 ** 2)
        lot.put(ro.plate(np.array([x, y + 0.004, z]), n, r, 0.016, t), GREY_LT, "body")
    # the arms: boulders, bent, the fists forward and down in front of the belly
    for s, bone in ((1, "armR"), (-1, "armL")):
        lot.put(stone((s * 0.2, 0.0, 0.6), (0.095, 0.09, 0.09), 51 + (s > 0)), GREY, bone)
        lot.put(stone((s * 0.265, -0.06, 0.5), (0.072, 0.075, 0.08), 53 + (s > 0)), GREY_DK, bone)
        lot.put(stone((s * 0.255, -0.15, 0.43), (0.068, 0.08, 0.066), 55 + (s > 0)), GREY, bone)
        lot.put(stone((s * 0.24, -0.215, 0.39), (0.072, 0.066, 0.07), 57 + (s > 0)), GREY_LT, bone)
        for j, dx in enumerate((-0.035, 0.0, 0.035)):
            lot.put(stone((s * (0.24 + dx), -0.27, 0.37 + 0.006 * j), (0.026, 0.026, 0.028), 60 + j + 3 * (s > 0)),
                    GREY_DK, bone)
    # the pillar of rock out of the back behind the left shoulder, leaning back and out
    b0, b1 = np.array([-0.07, 0.12, 0.5]), np.array([-0.14, 0.27, 0.9])
    rs = np.random.RandomState(7)
    pts = []
    for t, hw in ((0.0, 0.07), (0.45, 0.058), (0.85, 0.034)):
        c = b0 * (1 - t) + b1 * t
        for i in range(5):
            th = 2 * math.pi * i / 5 + 0.3 * t
            pts.append(c + hw * (0.85 + 0.3 * rs.rand()) * np.array([math.cos(th), math.sin(th), 0]))
    pts.append(b1)
    lot.put(hull(pts), GREY, "body")
    # the head: a faceted block with a flat face, a round stone like an ear on each side
    planes = rg.ball_planes(71, jitter=0.035, turn=(9, 27, 14), keep_off=(FRONT_N, 34))
    planes.append((FRONT_N, FRONT_D))
    lot.put(rg.stone(HEAD_C, HEAD_R, planes), GREY, "head")
    for s in (1, -1):
        lot.put(stone((s * 0.125, -0.05, 0.705), (0.05, 0.05, 0.058), 75 + (s > 0)), GREY, "head")
        # the brow: a heavy block over the eye, sloping down to the nose
        e = HEAD_C + np.array([s * 0.05, -0.12, 0.068])
        lot.put(hull([e + (s * 0.04, 0.03, 0.018), e + (s * 0.04, -0.01, 0.018), e + (s * 0.04, -0.012, -0.004),
                       e + (-s * 0.036, 0.03, -0.004), e + (-s * 0.036, -0.012, -0.016),
                       e + (-s * 0.036, -0.012, 0.004), e + (s * 0.04, 0.03, -0.01), e + (-s * 0.036, 0.03, -0.022)]),
                GREY_DK, "head")
    # the heap of red-hot coal out of the top of the head
    ro.heap(k, lot, (0.0, -0.04, 0.715), 0.128, (0.0, -0.01, 0.99), 58, 0.03, 23, "head", fat=1.0, shrink=0.45)
    lot.flush(k, "coal")
    # a thin fused plate on the face's facet for the eyes and the jaw
    sc, sn = rg.plane_world(HEAD_C, HEAD_R, FRONT_N, FRONT_D)
    up = np.array([0, 0, 1.0]) - sn * sn[2]; up /= np.linalg.norm(up)
    ex = np.cross(up, sn)
    outline = [(FACE_HALF[0] * math.cos(a) / max(abs(math.cos(a)), abs(math.sin(a))) ** 0.25,
                FACE_HALF[1] * math.sin(a) / max(abs(math.cos(a)), abs(math.sin(a))) ** 0.25)
               for a in np.linspace(0, 2 * math.pi, 24, endpoint=False)]
    k.add("face", slab(outline, sc - sn * 0.004, ex, up, 0.012), GREY, "head", surface="shell")
    front = sc + sn * 0.002
    # the jaw glowing orange, its teeth hanging from its top edge (in every state)
    jc = front + ex * JAW_OFF[0] + up * JAW_OFF[2]
    k.decal(jc, sn, JAW_HALF, jaw, up=up, eps=0.002, rings=14, segs=64)
    for j, du in enumerate((-0.56, -0.19, 0.19, 0.56)):
        tc = jc + ex * du * JAW_HALF[0] + up * 0.62 * JAW_HALF[1]
        k.decal(tc, sn, (0.014, 0.016), GREY, up=up, eps=0.0035, rings=6, segs=24,
                outline=polygon([(-1, 1), (1, 1), (0, -1)]))
    k.build(voxel=0.004, smooth=2)
    def lay(state):
        look = eye_look(state, eye_open, srgb(GREY), srgb(EYE_INK))
        for s in (1, -1):
            c = front + ex * s * EYE_OFF[0] + up * EYE_OFF[2]
            if s > 0:
                k.decal(c, sn, EYE_HALF, look, up=up, eps=0.0022, rings=20, segs=56, outline=polygon(EYE_PTS))
            else:
                k.decal(c, sn, EYE_HALF, lambda u, v: look(-u, v), up=up, eps=0.0022, rings=20, segs=56,
                        outline=polygon([(-a, b) for a, b in EYE_PTS]))
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    src, out = args()
    coalossal(src, out)
