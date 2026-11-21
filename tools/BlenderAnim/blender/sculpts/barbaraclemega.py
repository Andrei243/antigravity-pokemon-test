# Mega Barbaracle sculpted from nothing on the skeleton and clips of the game's own export of the form (sculptkit.py):
#   blender --background --factory-startup --python sculpts/barbaraclemega.py -- <the export's Barbaracle-Mega.glb> <out.glb>
#
# From the references (10298): Barbaracle grown into a totem with eight arms. The largest masses, in order: eight
# orange arms banded black spraying round the body like a wheel, each ending in a big black hand of a cup with four
# curved black claws (a red eye with a white rim on the palms of the top pair and the level pair); a great rock for a
# chest, pale grey on its flat top and down its front, slate below, cracked black, a black cone of rock at each
# corner, narrowing to a point under it; a smaller rock below it on an orange waist, black cones on its rim; one
# column of a leg (the two pressed together), orange zigzagged with black, on white pointed toes; on top a black head
# with two black horns tipped white, tall white eyes with black pupils, an orange jagged mouth and a white curl on the
# viewer's left cheek.
#
# The pictures overrule the game's sculptor where it differs: three stacked rocks there, two here (with the waist);
# the arms orange banded black (not black banded orange), the legs one column.
#
# The art spreads the arms like a wheel; carried here a third forward and drawn in, hands in front of the body's
# plane, so the model is narrower than it is tall (the design's largest mass stays held out, as Necrozma's hands do).
#
# Grown from barbaracle.py and binacle.py (sibling): the barnacle hand and its Frame, the rock and its pits, the
# segment and its band.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, unit, eye_look, args, outward, hull, ring_band, polygon, \
    sibling, tri, boxy

bar = sibling("barbaracle")
bi = bar.bi
Frame = bi.Frame

BLACK, ORANGE, CLAW, WHITE = "#2C2C34", "#EE7A40", "#34343E", "#EEF0F4"
ROCKTOP, ROCKSIDE, CRACK = "#E6E8EE", "#8E96AC", "#26262E"
RED, PUPIL, LIDINK = "#D82C30", "#141418", "#A8A8B6"
Z = np.array([0, 0, 1.0])

UPPER_C, UPPER_R = np.array([0.0, 0.0, 0.5]), np.array([0.21, 0.145, 0.175])
LOWER_C, LOWER_R = np.array([0.0, 0.0, 0.285]), np.array([0.105, 0.085, 0.075])
HEAD_C, HEAD_R = np.array([0.0, -0.006, 0.765]), np.array([0.07, 0.062, 0.085])
LIMB_R = 0.028


# ---- the body ---------------------------------------------------------------------------------------------------
def rock(k, c, r, seed, n, name, taper):
    """Barbaracle's rock (binacle.rock's way: slate hull, pale cap), drawn in toward its bottom by taper."""
    P = bi.rock_points(c, r, n, seed, flat_bottom=1.0)
    t = np.clip((P[:, 2] - c[2]) / r[2], -1, 1)
    P[:, :2] *= (1 - taper * np.clip(-t, 0, 1))[:, None]
    k.add(name, hull(P, merge=5.0), ROCKSIDE, "body", fuse=False, surface="shell")
    up = P[P[:, 2] > c[2] - r[2] * 0.1]
    cap = (up - c) * np.array([1.015, 1.015, 1.0]) + c + Z * 0.004
    lip = up.copy(); lip[:, 2] = c[2] - r[2] * 0.1
    rs = np.random.RandomState(seed + 1)
    lip[:, 2] += rs.rand(len(lip)) * r[2] * 0.35
    shape = hull(np.vstack([cap, (lip - c) * 1.014 + c]), merge=5.0)
    k.add(name + "top", shape, ROCKTOP, "body", fuse=False, surface="shell")
    return shape


def cone(k, base, d, r, L, name):
    d = unit(*d); a = np.cross(d, (0, 1.0, 0.1)); a /= np.linalg.norm(a); b = np.cross(d, a)
    pts = [base + r * (math.cos(t) * a + math.sin(t) * b) for t in np.linspace(0, 2 * math.pi, 6, endpoint=False)]
    pts.append(base + d * L)
    k.add(name, hull(pts), BLACK, "body", fuse=False, surface="shell")


def body(k):
    up = rock(k, UPPER_C, UPPER_R, seed=11, n=44, name="upper", taper=0.62)
    low = rock(k, LOWER_C, LOWER_R, seed=3, n=30, name="lower", taper=0.15)
    for i, (x, y, r) in enumerate(((-0.1, -0.06, 0.016), (0.08, 0.05, 0.013), (0.12, -0.04, 0.011))):
        bi.pit(k, up, x, y, r, color=CRACK, name=f"pitu{i}")
    # black cracks down the front: thin slabs laid on the rock's face, as the pits are
    for i, (x, z, ang, L) in enumerate(((-0.04, 0.56, 70, 0.07), (0.05, 0.5, 110, 0.06), (0.0, 0.45, 95, 0.05),
                                        (-0.08, 0.47, 50, 0.045))):
        crack(k, up, x, z, ang, L, f"crack{i}")
    k.add("waist", tube([(0, 0, 0.28), (0, 0, 0.36), (0, 0, 0.44)], [0.045, 0.034, 0.05], seg=20), ORANGE, "body")
    # black cones of rock at the corners of the chest and round the lower rock's rim
    for i, (x, y, z, d) in enumerate(((0.15, -0.07, 0.59, (0.3, -0.5, 1)), (-0.15, -0.07, 0.59, (-0.3, -0.5, 1)),
                                      (0.165, 0.06, 0.585, (0.5, 0.3, 1)), (-0.165, 0.06, 0.585, (-0.5, 0.3, 1)),
                                      (0.0, -0.1, 0.42, (0, -1, -0.4)))):
        cone(k, np.array([x, y, z]), d, 0.024, 0.06, f"cone{i}")
    for i, (x, y) in enumerate(((0.045, -0.04), (-0.045, -0.04), (0.0, 0.05))):
        cone(k, np.array([x, y, 0.33]), (x * 3, y, 1), 0.017, 0.045, f"lcone{i}")


def crack(k, shape, x, z, ang, L, name):
    """A dark crack laid on the rock's front where a ray from before it meets it."""
    from mathutils import Vector
    from mathutils.bvhtree import BVHTree
    V, F = shape
    tree = BVHTree.FromPolygons(np.asarray(V).tolist(), F)
    hit = tree.ray_cast(Vector((x, -1.0, z)), Vector((0, 1.0, 0)))
    if hit[0] is None:
        return
    p, n = np.array(hit[0]), np.array(hit[1])
    a = np.array([math.cos(math.radians(ang)), 0, math.sin(math.radians(ang))])
    a = a - n * (a @ n); a /= np.linalg.norm(a)
    b = np.cross(n, a)
    out_ = [(-L / 2, -0.004), (-L / 6, 0.004), (L / 6, -0.003), (L / 2, 0.004), (L / 2, 0.0), (L / 6, -0.008),
            (-L / 6, -0.001), (-L / 2, -0.008)]
    k.add(name, bi.slab(out_, p + n * 0.001, a, b, 0.003), CRACK, "body", fuse=False, surface="shell")


# ---- the leg: one column, zigzagged -----------------------------------------------------------------------------
def legs(k):
    for s, side in ((1, "R"), (-1, "L")):
        k.add(f"leg{side}", tube([(0.03 * s, 0, 0.3), (0.034 * s, -0.005, 0.15), (0.036 * s, -0.01, 0.06)],
                                 [0.05, 0.053, 0.055], seg=20), ORANGE, "leg" + side)
        # white pointed toes, two a foot, forward and out
        for j, dx in enumerate((0.015, 0.05)):
            b = np.array([dx * s, -0.05, 0.03])
            k.add(f"toe{side}{j}", hull([b + (0.014, 0, 0.012), b + (-0.014, 0, 0.012), b + (0, 0.02, 0.026),
                                         b + (0, 0.01, -0.016), b + (0.006 * s, -0.045, -0.012)]), WHITE,
                  "leg" + side, fuse=False, surface="shell")
    # black chevrons down the column's front and sides
    def chevrons(P):
        f = P[:, 2] / 0.085 + 0.45 * tri(P[:, 0] / 0.11 + 0.5)
        return inside((f % 1) - 0.42, 0.02) * inside(P[:, 2] - 0.27, 0.01) * inside(0.02 - P[:, 2], 0.01)
    k.paint(chevrons, BLACK, on=["leg"])


# ---- the head -----------------------------------------------------------------------------------------------------
def head(k):
    k.add("head", ell(HEAD_C, HEAD_R, deform=boxy(3)), BLACK, "head")
    k.add("neck", tube([(0, 0, 0.62), (0, -0.004, 0.7)], [0.05, 0.05]), BLACK, "head")
    for s, side in ((1, "R"), (-1, "L")):
        a = np.array([0.042 * s, 0.0, 0.83])
        pts = [a, a + (0.012 * s, 0.002, 0.04), a + (0.03 * s, 0.006, 0.078)]
        k.add(f"horn{side}", outward(blade(pts, [0.02, 0.015, 0.004], (0, 1, 0), 0.7)), BLACK, "head", fuse=False,
              surface="shell")
        tip = [a + (0.022 * s, 0.004, 0.06), a + (0.03 * s, 0.006, 0.08)]
        k.add(f"horntip{side}", outward(blade(tip, [0.0095, 0.003], (0, 1, 0), 0.75)), WHITE, "head", fuse=False,
              surface="shell")


FACE_Y = HEAD_C[1] - HEAD_R[1] * 0.98


def face_marks(k):
    # the jagged orange mouth: a band whose top edge is a row of points
    mouth = [(-1, -0.6), (-1, 0.2), (-0.66, 1.0), (-0.33, 0.2), (0, 1.0), (0.33, 0.2), (0.66, 1.0), (1, 0.2),
             (1, -0.6), (0.5, -1.0), (-0.5, -1.0)]
    k.decal((0, FACE_Y, 0.725), (0, -1, 0), (0.045, 0.02), ORANGE, eps=0.004, rings=16, segs=96,
            outline=polygon(mouth))
    # the white curl of the barnacle on the viewer's left cheek (-X): a ring with an open side
    def curl(u, v):
        c = np.tile(srgb(WHITE), (len(u), 1))
        c[u * u + v * v < 0.3] = srgb(BLACK)
        c[(u > 0.25) & (np.abs(v) < 0.35)] = srgb(BLACK)
        return c
    k.decal((-0.05, -0.05, 0.745), unit(-0.55, -0.85, 0), (0.016, 0.016), curl, eps=0.004, rings=12, segs=48)


EYE_SHAPE = [(-1.0, -0.6), (-0.7, -1.0), (0.7, -1.0), (1.0, -0.5), (1.0, 0.6), (0.6, 1.0), (-0.6, 1.0),
             (-1.0, 0.6)]


def head_open(u, v):
    c = np.tile(srgb(WHITE), (len(u), 1))
    c[((u + 0.05) / 0.32) ** 2 + ((v + 0.1) / 0.55) ** 2 < 1] = srgb(PUPIL)
    return c


def head_eyes(k, state):
    look = eye_look(state, head_open, srgb(BLACK), srgb(LIDINK), line=0.16)
    for s in (1, -1):
        col = look if s > 0 else (lambda u, v, f=look: f(-u, v))
        k.decal((0.028 * s, FACE_Y, 0.79), unit(0.25 * s, -1, 0.0), (0.02, 0.028), col, outline=polygon(EYE_SHAPE),
                eps=0.0045, rings=20, segs=56)


# ---- the eight arms ---------------------------------------------------------------------------------------------
ARMS = {  # bone: shoulder, elbow, wrist, the way the hand opens (on +X; mirrored by hand), an eye on its palm
    "arm": ((0.06, -0.01, 0.66), (0.115, -0.05, 0.79), (0.16, -0.075, 0.86), (0.35, -0.25, 1.0), True),
    "arm2": ((0.12, -0.02, 0.63), (0.21, -0.07, 0.7), (0.26, -0.1, 0.74), (0.75, -0.35, 0.6), False),
    "arm3": ((0.16, -0.03, 0.52), (0.235, -0.09, 0.53), (0.285, -0.13, 0.53), (1.0, -0.45, 0.06), True),
    "arm4": ((0.1, -0.03, 0.44), (0.18, -0.07, 0.38), (0.225, -0.1, 0.33), (0.7, -0.35, -0.7), False),
}
HANDS = []          # (frame, side) of each hand with an eye


def arms(k):
    for bone, (sh, el, wr, opens, eyed) in ARMS.items():
        for s, side in ((1, "R"), (-1, "L")):
            m = np.array([s, 1, 1.0])
            a, e, w = np.array(sh) * m, np.array(el) * m, np.array(wr) * m
            nm = f"{bone}{side}"
            k.add(nm + "limb", tube([a, e, w], [LIMB_R * 1.1, LIMB_R, LIMB_R * 0.95], seg=18, per=6), ORANGE,
                  bone + side)
            for t, L in ((0.42, 0.035), (0.78, 0.03)):
                p = a + (e - a) * t if t < 0.5 else e + (w - e) * (t - 0.5) * 2
                d = (e - a) if t < 0.5 else (w - e)
                k.add(nm + f"band{t}", ring_band(p, d, LIMB_R * (1.05 if t < 0.5 else 0.97) - 0.002, L, thick=0.008), BLACK, bone + side, fuse=False)
            up = np.array(opens) * m
            fr = Frame(w + unit(*up) * 0.05, up, (0, -1, 0.15), 1.45)
            bi.hand(k, fr, bone + side, nm + "hand", claws=4, color=BLACK, flesh=None, claw=CLAW, rim=False,
                    spread=1.25, claw_len=1.0)
            if eyed:
                HANDS.append(fr)


def hand_eye(k, fr, state):
    def open_(u, v):
        c = np.tile(srgb(WHITE), (len(u), 1))
        c[u * u + v * v < 0.55] = srgb(RED)
        c[(np.abs(u) < 0.14) & (np.abs(v) < 0.55)] = srgb(PUPIL)
        return c
    look = eye_look(state, open_, srgb(BLACK), srgb(LIDINK), line=0.18)
    p, n = bi.face_point(fr, 0.0, 0.004)
    k.decal(p, n, (0.016 * fr.s, 0.016 * fr.s), look, eps=0.004, rings=20, segs=56, up=fr.up)


def barbaracle_mega(src, out):
    k = Kit(src, coat="shell")
    body(k)
    legs(k)
    head(k)
    arms(k)
    face_marks(k)
    k.build(voxel=0.007, smooth=3, band=0.005, wband=0.05)

    def lay(state):
        head_eyes(k, state)
        for fr in HANDS:
            hand_eye(k, fr, state)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    src, out = args()
    barbaracle_mega(src, out)
