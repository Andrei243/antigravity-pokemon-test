# Stonjourner sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/stonjourner.py -- <the export's Stonjourner.glb> <out.glb>
#
# The design, from the references: a standing stone come to life, a trilithon. Two great pillars of pale grey stone
# stand side by side, broad and thick at their feet (nearly touching there) and narrower at their tops, which slope up
# toward the middle; their faces are flat with rounded edges and flecked with dark grey spots. The lower third of each is
# brown earth, its top edge ragged where the grey runs down into it, and on the front of the brown a raised pale rune
# of two zigzag strokes crossed twice (an X, a diamond, an X). Across the pillars' tops lies the lintel, a long slab with
# chamfered ends, spotted as they are, and on its front the face: two upright pale slits outlined in black and a short
# pale dash of a mouth between and a little below them. On the lintel's top a small stepped dark block; at each of its
# ends a small dark cube, and above and out from it larger floating cubes, pale and dark (two on the -X end, three on
# the +X end), its "arms".
# Where the pictures overruled the brief: nothing; the slabs are fused with rounded edges as the 3D picture's are (the
# ragged earth line and the spots need a fused surface), the rune and the cubes are hard-edged loose pieces.
# Skeleton: root, body (0, 0, 0.66), head (0, 0, 0.72), legL/legR (+-0.13, 0, 0.55). No arm bones: the cubes are on the
# head, as the game's own sculptor has them; the lintel and its face on the body, each pillar on its leg.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from mathutils import Euler
from sculptkit import Kit, ell, srgb, unit, boxy, eye_look, args, hull, polygon, inside

# --- colours ----------------------------------------------------------------------------------------------------------
GREY, SPOT, EARTH = "#B4B4BA", "#6C6C74", "#9A7556"
RUNE, PALE, DARK = "#CACACF", "#C2C2C8", "#5C5C64"
INK, SLIT = "#232328", "#F2F2F4"

# --- the frame --------------------------------------------------------------------------------------------------------
P_CX = (0.19, 0.165)        # a pillar's middle x at its foot and at its top
P_W = (0.17, 0.105)         # its half width at its foot and its top
P_D = (0.12, 0.085)         # its half depth
P_TOP = (0.548, 0.588)      # its top at the outer and the inner side
EARTH_Z = 0.205             # the earth's top, on average
RUNE_Z, RUNE_H, RUNE_A, RUNE_W = 0.13, 0.085, 0.036, 0.025   # the rune's middle, half height, half width, stroke

L_C, L_R = np.array([0.0, 0.0, 0.632]), np.array([0.218, 0.082, 0.062])   # the lintel
FACE_Y = -0.081
EYE_X, EYE_Z, EYE_HALF = 0.054, 0.644, (0.022, 0.04)
MOUTH_Z, MOUTH_HALF = 0.613, (0.024, 0.0115)


def pillar_shape(s):
    """The unit sphere made a pillar on side s (+1 +X, -1 -X): a rounded box, broad at its foot, its top sloping up
    toward the middle."""
    def f(V):
        V = boxy(6.0)(V)
        t = (V[:, 2] + 1) / 2
        cx = P_CX[0] + (P_CX[1] - P_CX[0]) * t
        w = P_W[0] + (P_W[1] - P_W[0]) * t
        d = P_D[0] + (P_D[1] - P_D[0]) * t
        xl = V[:, 0] * w                      # across, from the pillar's middle
        inner = -xl * s / P_W[1]              # 1 at the inner edge of the top, -1 at the outer
        top = (P_TOP[0] + P_TOP[1]) / 2 + (P_TOP[1] - P_TOP[0]) / 2 * np.clip(inner, -1, 1)
        z = t * top
        return np.stack([s * cx + xl, V[:, 1] * d, z], 1)
    return f


def pillar_at(z):
    """A pillar's middle x (on +X), half width and half depth at height z."""
    t = np.clip(z / ((P_TOP[0] + P_TOP[1]) / 2), 0, 1)
    return (P_CX[0] + (P_CX[1] - P_CX[0]) * t, P_W[0] + (P_W[1] - P_W[0]) * t, P_D[0] + (P_D[1] - P_D[0]) * t)


def lintel_shape(V):
    """A long slab, its ends chamfered: narrower and lower toward the tips."""
    V = boxy(6.0)(V)
    e = np.clip((np.abs(V[:, 0]) - 0.72) / 0.28, 0, 1)
    V = V.copy()
    V[:, 1] *= 1 - 0.3 * e
    V[:, 2] *= 1 - 0.38 * e
    return V


def cube(c, h, rot, cham=0.22):
    """A cube with its edges cut back a little: a hull."""
    pts = []
    for x in (-1, 0, 1):
        for y in (-1, 0, 1):
            for z in (-1, 0, 1):
                n = abs(x) + abs(y) + abs(z)
                if n == 0:
                    continue
                pts.append(np.array([x, y, z], float) * (1 - cham * (n - 1) / 2))
    P = np.array(pts) * h
    m = np.array(Euler([math.radians(a) for a in rot]).to_matrix())
    return hull(P @ m.T + np.array(c))


def blob(rs):
    """A spot's outline: a jittered hexagon round its middle."""
    th = np.linspace(0, 2 * math.pi, 7)[:-1] + rs.uniform(0, 1)
    r = rs.uniform(0.7, 1.0, 6)
    return polygon(np.stack([np.cos(th) * r, np.sin(th) * r], 1))


def pillars(k):
    for s, side in ((1, "R"), (-1, "L")):
        k.add(f"pillar{side}", ell((0, 0, 0), (1, 1, 1), deform=pillar_shape(s), sub=5), GREY, f"leg{side}")

    # the earth: the lower third brown, its top edge ragged where the grey runs down into it
    def earth(P):
        out = np.zeros(len(P))
        for s in (1, -1):
            cx, w, d = pillar_at(P[:, 2])
            lx = P[:, 0] - s * cx
            th = np.arctan2(P[:, 1] / d, lx / w)
            edge = (EARTH_Z + 0.018 * np.sin(5 * th + 1.3 * s) + 0.011 * np.sin(11 * th + 0.4)
                    - 0.035 * np.clip(np.sin(7 * th + 2.0 * s) - 0.55, 0, 1) / 0.45)
            near = inside(np.abs(lx) - w - 0.03, 0.002) * (np.sign(P[:, 0]) == s)
            out = np.maximum(out, inside(P[:, 2] - edge, 0.003) * near)
        return out
    k.paint(earth, EARTH, on=["pillar"])


def rune(k, s, side):
    """Two zigzag strokes crossed twice (an X, a diamond, an X), raised on the brown front."""
    cx, w, d = pillar_at(RUNE_Z)
    for j, sgn in enumerate((1, -1)):
        pts = [(-sgn * RUNE_A, RUNE_Z + RUNE_H), (sgn * RUNE_A, RUNE_Z), (-sgn * RUNE_A, RUNE_Z - RUNE_H)]
        for i in range(2):
            (x0, z0), (x1, z1) = pts[i], pts[i + 1]
            a = np.array([s * cx + x0, 0, z0]); b = np.array([s * cx + x1, 0, z1])
            along = unit(*(b - a)); across = np.cross(along, (0, 1.0, 0))
            corners = []
            for p in (a - along * RUNE_W * 0.35, b + along * RUNE_W * 0.35):
                fy = -pillar_at(p[2])[2]
                for c in (-1, 1):
                    for dy in (0.004, -0.011):
                        q = p + across * c * RUNE_W / 2
                        corners.append((q[0], fy + dy, q[2]))
            k.add(f"rune{side}{j}{i}", hull(corners), RUNE, f"leg{side}", fuse=False)


def lintel(k):
    k.add("lintel", ell(L_C, L_R, deform=lintel_shape, sub=5), GREY, "body")
    # the stepped dark block on its top
    top = L_C[2] + L_R[2]
    k.add("cap0", hull([(x, y, z) for x in (-0.068, 0.068) for y in (-0.048, 0.048) for z in (top - 0.012, top + 0.03)]),
          DARK, "head", fuse=False)
    k.add("cap1", hull([(x, y, z) for x in (-0.036, 0.036) for y in (-0.03, 0.03) for z in (top + 0.02, top + 0.056)]),
          DARK, "head", fuse=False)


def arms(k):
    """The floating cubes at the lintel's ends: a small dark one at each end, and larger ones above and out."""
    k.add("cubeL0", cube((-0.238, 0.0, 0.685), 0.027, (10, 18, 25)), DARK, "head", fuse=False)
    k.add("cubeL1", cube((-0.262, -0.01, 0.79), 0.054, (12, -14, 24)), PALE, "head", fuse=False)
    k.add("cubeR0", cube((0.243, 0.0, 0.677), 0.026, (-12, 20, -15)), DARK, "head", fuse=False)
    k.add("cubeR1", cube((0.283, -0.008, 0.745), 0.034, (15, 10, 30)), PALE, "head", fuse=False)
    k.add("cubeR2", cube((0.25, 0.005, 0.85), 0.055, (-10, 16, -28)), DARK, "head", fuse=False)


def spots(k):
    rs = np.random.RandomState(874)
    def lay(c, n, a, b):
        k.decal(c, n, (a, b), SPOT, eps=0.003, outline=blob(rs), rings=6, segs=24)
    # the lintel: front (clear of the face), back, top
    for x, z in ((-0.15, 0.645), (0.125, 0.612), (0.172, 0.656), (-0.105, 0.612)):
        lay((x, FACE_Y, z), (0, -1, 0), 0.016, 0.012)
    for x, z in ((-0.12, 0.63), (0.05, 0.645), (0.16, 0.62)):
        lay((x, -FACE_Y, z), (0, 1, 0), 0.016, 0.012)
    # the pillars: fronts, outer sides, backs (never the inner sides, which face each other)
    for s in (1, -1):
        for dx, z, a, b in ((-0.06, 0.5, 0.013, 0.024), (0.06, 0.42, 0.014, 0.027), (-0.02, 0.34, 0.012, 0.022),
                            (0.08, 0.285, 0.011, 0.02), (-0.08, 0.38, 0.01, 0.018)):
            cx, w, d = pillar_at(z)
            lay((s * (cx + dx * (1 if s > 0 else 0.9)), -d, z), (0, -1, 0), a, b)
        for dy, z, a, b in ((-0.03, 0.46, 0.013, 0.025), (0.04, 0.34, 0.014, 0.024), (0.0, 0.26, 0.011, 0.018)):
            cx, w, d = pillar_at(z)
            lay((s * (cx + w), dy, z), unit(s, 0, 0.16), a, b)
        for dx, z, a, b in ((-0.04, 0.47, 0.014, 0.026), (0.05, 0.36, 0.013, 0.024), (-0.03, 0.29, 0.011, 0.02)):
            cx, w, d = pillar_at(z)
            lay((s * (cx + dx), d, z), (0, 1, 0), a, b)


def mouth(k):
    ci, cs = srgb(INK), srgb(SLIT)
    def look(u, v):
        c = np.tile(ci, (len(u), 1))
        c[(np.abs(u) < 0.7) & (np.abs(v) < 0.42)] = cs
        return c
    k.decal((0, FACE_Y, MOUTH_Z), (0, -1, 0), MOUTH_HALF, look, eps=0.0025, rings=12, segs=48,
            outline=pill(MOUTH_HALF[1] / MOUTH_HALF[0], True))


def pill(ratio, wide):
    """A rounded oblong's outline (an eye's slit, the mouth's dash) of half sizes 1 by 1 in the patch's own units."""
    def r(th):
        c, s_ = abs(math.cos(th)), abs(math.sin(th))
        n = 4.0
        return 1.0 / (c ** n + s_ ** n) ** (1 / n)
    return r


def slit_open():
    ci, cs = srgb(INK), srgb(SLIT)
    def look(u, v):
        c = np.tile(ci, (len(u), 1))
        c[(np.abs(u) < 0.5) & (np.abs(v) < 0.7)] = cs
        return c
    return look


def eyes(k, state):
    look = eye_look(state, slit_open(), srgb(GREY), srgb(INK), line=0.2)
    k.decal((EYE_X, FACE_Y, EYE_Z), (0, -1, 0), EYE_HALF, look, eps=0.0028, rings=20, segs=56,
            outline=pill(1, False), mirror=True)


def stonjourner(src, dest):
    k = Kit(src, coat="shell")
    pillars(k)
    for s, side in ((1, "R"), (-1, "L")):
        rune(k, s, side)
    lintel(k)
    arms(k)
    spots(k)
    mouth(k)
    k.build(voxel=0.0075, band=0.004)
    k.eye_states(lambda state: eyes(k, state))
    k.export(dest)


if __name__ == "__main__":
    stonjourner(*args())
