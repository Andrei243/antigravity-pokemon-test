# Mega Raichu X sculpted from nothing on the skeleton and clips of the game's own export of the form (sculptkit.py),
# grown from Raichu's script (raichu.py, which builds as it is read: its way of making the head, the body's pear, the
# belly, the back stripes, the cheeks, the nose and the eyes is copied here and changed where the pictures change it).
#   blender --background --factory-startup --python sculpts/raichumegax.py -- <the export's Raichu-Mega-X.glb> <out.glb>
#
# The design, from the two pictures: a bright yellow Raichu, hovering; the largest mass is two great trails of lightning
# that fall from behind its legs in a V, black and toothed at the top, then broad yellow bolts with a step on the outer
# edge, orange toward their points; tall pointed ears, black from the root to about half way and yellow above, curving
# out; a small black spiral on a stem beside each ear's root; boxer's mitts of brown on short yellow arms; a big white
# belly; short legs with brown shins and feet; red cheeks; black eyes under angry brows and an open mouth with a fang.
#
# Also the frame Mega Raichu Y (raichumegay.py) is grown from: on_head, clip_b, bolt, curl, brow_outline, brow_eyes.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, slab, srgb, inside, unit, eye_look, polygon, args

# --- the frame both Megas share --------------------------------------------------------------------------------------

def on_head(H, HR, d, lift=0.0):
    """The point of the head's ellipsoid (centre H, radii HR) in direction d, lifted off it."""
    d = unit(*d)
    s = 1 / math.sqrt(((d / HR) ** 2).sum())
    return H + d * (s + lift)


def pear(V):
    V = V.copy(); s = 1 - 0.12 * V[:, 2]
    V[:, 0] *= s; V[:, 1] *= s
    return V


def clip_b(poly, lo, hi):
    """A polygon of (a, b) points cut to lo <= b <= hi."""
    def cut(P, keep, at):
        out_, n = [], len(P)
        for i in range(n):
            p, q = P[i], P[(i + 1) % n]
            kp, kq = keep(p), keep(q)
            if kp:
                out_.append(p)
            if kp != kq:
                t = (at - p[1]) / (q[1] - p[1])
                out_.append((p[0] + (q[0] - p[0]) * t, at))
        return out_
    P = cut(list(poly), lambda p: p[1] >= lo, lo)
    return cut(P, lambda p: p[1] <= hi, hi)


def bolt(k, name, outline, scale, origin, ex, ez, bone, bands, mirror=True):
    """
    A flat bolt of lightning (a slab) in bands of colour along its outline's second coordinate: bands are
    (colour, lo, hi, thickness), each a loose slab of the outline cut to lo..hi (a thicker band covers a thinner one
    where they overlap, on both faces).
    """
    for i, (col, lo, hi, thick) in enumerate(bands):
        part = clip_b(outline, lo, hi)
        if len(part) < 3:
            continue
        pts = [(a * scale, b * scale) for a, b in part]
        k.add(f"{name}{i}", slab(pts, origin, ex, ez, thick), col, bone, fuse=False, mirror=mirror)


def curl(c, e1, e2, r0, r1, turns, start=0.0, n=40):
    """Points of a flat spiral about c in the plane (e1, e2), from radius r0 at angle start inward to r1."""
    c, e1, e2 = np.array(c, float), np.array(e1, float), np.array(e2, float)
    pts = []
    for t in np.linspace(0, 1, n):
        a = start + 2 * math.pi * turns * t
        r = r0 + (r1 - r0) * t
        pts.append(c + r * (math.cos(a) * e1 + math.sin(a) * e2))
    return pts


def brow_outline(slope, top, inner=1.0):
    """An eye's oval cut along its top by a straight brow, v = top + slope * u (u along the decal's own +u)."""
    pts = []
    for t in np.linspace(0, 2 * math.pi, 64, endpoint=False):
        u, v = math.cos(t), math.sin(t)
        pts.append((u, min(v, top + slope * u)))
    return polygon(pts)


def brow_eyes(k, E, de, half, look, slope, top, eps=0.0025):
    """
    A pair of eyes cut by brows that slope down to the nose: laid by hand, since a mirrored patch keeps its outline
    unmirrored. E, de are the +X eye's centre and normal; look(u, v) has u outward on both.
    """
    m = np.array([-1, 1, 1.0])
    k.decal(E, de, half, look, eps=eps, outline=brow_outline(slope, top), rings=20, segs=56)
    k.decal(np.array(E) * m, np.array(de) * m, half, lambda u, v: look(-u, v), eps=eps,
            outline=brow_outline(-slope, top), rings=20, segs=56)


# --- Mega Raichu X ---------------------------------------------------------------------------------------------------

YELLOW, WHITE, BROWN, BLACK, RED = "#F8D232", "#FBF8F2", "#86502E", "#26222A", "#E2302E"
BOLTY, ORANGE, MOUTH, TONGUE, NOSE = "#FFD620", "#F5961A", "#6E1A22", "#E8707A", "#2A2026"


def raichu_mega_x(src, out_path):
    k = Kit(src, coat="fur")
    H = np.array([0, -0.012, 1.0])            # the head's centre, on the head joint (0.959) and up
    HR = np.array([0.163, 0.141, 0.132])
    oh = lambda d, lift=0.0: on_head(H, HR, d, lift)

    # The head, round and smooth; the body a pear under it with no neck, the chest filling up into the head
    k.add("head", ell(H, HR), YELLOW, "head")
    k.add("body", ell((0, 0.01, 0.782), (0.132, 0.12, 0.152), deform=pear), YELLOW, "body")
    k.add("chest", ell((0, 0.012, 0.892), (0.106, 0.085, 0.066)), YELLOW, "body")

    # Short legs, hanging (it hovers): a yellow thigh fused into the hips, brown shin and foot
    k.add("thigh", ell((0.07, -0.008, 0.668), (0.06, 0.063, 0.07)), YELLOW, "leg{s}", mirror=True)
    k.add("shin", tube([(0.074, -0.014, 0.632), (0.08, -0.026, 0.56)], [0.044, 0.04]), BROWN, "leg{s}", mirror=True)
    k.add("foot", ell((0.082, -0.05, 0.528), (0.043, 0.064, 0.032), rot=(-20, 0, 0)), BROWN, "leg{s}", mirror=True)
    k.paint(lambda P: inside(P[:, 2] - 0.618, 0.003) * inside(0.035 - np.abs(P[:, 0]), 0.003), BROWN)

    # Short yellow arms held forward and a little down, ending in great brown mitts like a boxer's
    k.add("arm", tube([(0.08, -0.02, 0.895), (0.13, -0.055, 0.862), (0.155, -0.085, 0.845)], [0.04, 0.036, 0.034]),
          YELLOW, "arm{s}", mirror=True)
    k.add("mitt", ell((0.19, -0.118, 0.83), (0.08, 0.07, 0.064), rot=(0, 0, -38)), BROWN, "arm{s}", mirror=True)
    k.add("thumb", ell((0.168, -0.163, 0.855), (0.03, 0.028, 0.03)), BROWN, "arm{s}", mirror=True)

    # The big white belly (crisp: no `on`, kept to the front)
    def belly(P):
        e = (P[:, 0] / 0.096) ** 2 + ((P[:, 2] - 0.79) / 0.13) ** 2 - 1
        return inside(e * 0.05, 0.0025) * inside(P[:, 1] + 0.03, 0.01) * inside(P[:, 2] - 0.93, 0.004)
    k.paint(belly, WHITE, on=["body", "chest"])

    # The tall ears, out of the crown and curving out, flat front to back: black from the root to half way, yellow
    # above to the point
    EAR = np.array([(0.065, 0.01, 1.07), (0.098, 0.012, 1.125), (0.124, 0.014, 1.21), (0.166, 0.016, 1.32),
                    (0.236, 0.016, 1.41), (0.33, 0.016, 1.472)])
    ERS = [0.044, 0.06, 0.064, 0.05, 0.028, 0.007]
    k.add("ear", blade(EAR, ERS, (0, 1, 0), 0.45), YELLOW, "ear{s}", mirror=True)
    P0 = (EAR[2] + EAR[3]) / 2 + (EAR[3] - EAR[2]) * 0.1
    EA = unit(*(EAR[3] - EAR[2]))
    def ear_root(P):
        Q = P.copy(); Q[:, 0] = np.abs(Q[:, 0])
        near = np.linalg.norm(Q - (EAR[1] + P0) / 2, axis=1)
        return inside(-(((P0 - Q) @ EA)), 0.0025) * inside(near - 0.12, 0.003) * (P[:, 2] > 1.085)
    k.paint(ear_root, BLACK)

    # A small black spiral on a stem out of each ear's root, facing the front
    cs = np.array([0.268, 0.004, 1.1])
    sp = [np.array((0.13, 0.012, 1.1)), np.array((0.19, 0.008, 1.094))] + curl(cs, (1, 0, 0), (0, 0, 1), 0.052,
                                                                          0.012, 1.3, start=math.pi * 1.05)
    k.add("spiral", tube(sp, [0.017] * 2 + list(np.linspace(0.017, 0.012, len(sp) - 2)), seg=12), BLACK,
          "ear{s}", fuse=False, mirror=True)

    # Brown stripes across the back, as Raichu's (lens patches cast from behind)
    LENS = polygon([(-1, 0), (-0.8, 0.45), (-0.4, 0.82), (0, 1), (0.4, 0.82), (0.8, 0.45), (1, 0),
                    (0.8, -0.45), (0.4, -0.82), (0, -1), (-0.4, -0.82), (-0.8, -0.45)])
    for zc, hw in ((0.86, 0.08), (0.8, 0.092)):
        k.decal((0, 0.12, zc), unit(0, 1, 0.06), (hw, 0.016), BROWN, eps=0.003, outline=LENS, rings=10, segs=64)

    # The two great trails of lightning from behind the legs, in a V: black and toothed at the top, a broad yellow bolt
    # with a step in its outer edge below, orange toward the point. Outline in the picture's pixels: a outward, b down
    BLACKP = [(-10, 0), (14, -2), (48, 60), (54, 80), (42, 72), (36, 86), (26, 73), (16, 84), (6, 72), (-6, 82),
              (-10, 60)]
    BOLTP = [(-10, 72), (46, 72), (82, 100), (66, 114), (165, 232), (5, 186)]
    O, EX, EZ = (0.07, 0.05, 0.638), (1.0, 0, 0), unit(0, 0.2, -1)
    bolt(k, "trail", BLACKP, 0.0027, O, EX, EZ, "leg{s}", [(BLACK, -10, 100, 0.038)])
    bolt(k, "bolt", BOLTP, 0.0027, O, EX, EZ, "leg{s}", [(BOLTY, 72, 158, 0.03), (ORANGE, 158, 240, 0.03)])

    # The face: red cheek circles, a dot of a nose, an open mouth with a fang
    dc = unit(0.8, -0.55, -0.22)
    k.decal(oh(dc), dc, (0.04, 0.038), RED, eps=0.003, rings=10, segs=40, mirror=True)
    k.add("nose", ell(oh((0, -1, -0.02), -0.002), (0.009, 0.0055, 0.0055)), NOSE, "head", fuse=False, surface="skin")
    CM, CT = srgb(MOUTH), srgb(TONGUE)
    MOUTHO = polygon([(-1, 0.55), (1, 0.55), (0.9, 0.0), (0.6, -0.6), (0, -0.95), (-0.6, -0.6), (-0.9, 0.0)])
    def mouth(u, v):
        c = np.tile(CM, (len(u), 1))
        c[(u / 0.62) ** 2 + ((v + 0.75) / 0.5) ** 2 < 1] = CT
        return c
    dm = unit(0, -1, -0.42)
    k.decal(oh(dm), dm, (0.04, 0.032), mouth, eps=0.003, outline=MOUTHO, rings=18, segs=72)
    FANG = polygon([(-1, 1), (1, 1), (0, -1)])
    df = unit(0.22, -1, -0.32)
    k.decal(oh(df), df, (0.011, 0.012), WHITE, eps=0.005, outline=FANG, rings=6, segs=36)

    # Black eyes with a white glint under angry brows that slope down to the nose
    CY, CI = srgb(YELLOW), srgb(BLACK)
    def eye(u, v):
        c = np.tile(CI, (len(u), 1))
        c[((u - 0.12) / 0.32) ** 2 + ((v - 0.12) / 0.28) ** 2 < 1] = (1, 1, 1)
        return c
    de = unit(0.42, -0.88, 0.14)
    E = oh(de)
    def eyes(state):
        brow_eyes(k, E, de, (0.028, 0.031), eye_look(state, eye, CY, CI), 0.42, 0.42)

    k.build(voxel=0.0058)
    k.eye_states(eyes)
    k.export(out_path)


if __name__ == "__main__":
    src, out = args()
    raichu_mega_x(src, out)
