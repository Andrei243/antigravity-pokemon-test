# Nidoran (female) sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/nidoranf.py -- <the export's Nidoran♀.glb> <out.glb>
# From the references: a small pale periwinkle rabbit-like animal on four short legs, a round body wider at the rump,
# a big round head carried forward and a little up, the snout long and blunt; the mouth open and pink under the snout,
# two white buck teeth hanging from the upper lip; red eyes (a dark pupil, a white glint) on the sides of the face;
# white whiskers on the cheeks; a small horn on the forehead; two great round ears standing up and out, their faces a
# teal lining with a violet band round the outer rim and small points along their top edge; small spines down the back
# and on the haunches, the body's own colour; dark blue-violet spots on the back and the flanks; two white claws a foot.
#
# Written to be grown from (Nidorina imports it with sibling("nidoranf")): the line's colours, its spines, claws, ears,
# spots and eyes are the functions below; the calls that build Nidoran are under __main__.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from mathutils import Euler
from sculptkit import Kit, ell, tube, hull, slab, srgb, inside, smoothstep, unit, polygon, eye_look, args

# --- the line's colours -------------------------------------------------------------------------------------------
SKIN, SPOT, SPINE_TIP = "#B6C2EC", "#7279C6", "#929FD2"
LINING, RIM = "#3C9EB4", "#6560B8"
CLAW, TOOTH = "#F4F4F6", "#FFFFFF"
MOUTH, TONGUE = "#B8455C", "#EE8CA0"
WHISKER = "#F2F4FC"
RED, PUPIL = srgb("#D8303E"), srgb("#3A0A10")
INK, WHITE = srgb("#1E1C2A"), np.array([1.0, 1.0, 1.0])


def rot(r):
    return np.array(Euler([math.radians(a) for a in r]).to_matrix())


def on_ell(c, r, d, rr=(0, 0, 0)):
    """The point of an ellipsoid (centre c, radii r, turned by rr) along the direction d from its centre."""
    m = rot(rr)
    dl = m.T @ unit(*d)
    return np.array(c, float) + m @ (dl / math.sqrt(((dl / np.array(r, float)) ** 2).sum()))


def spine(base, d, length, width, sink=0.012):
    """A spine: a flat-faced cone from a hexagon round its root (sunk into the body) to its point along d."""
    d = unit(*d)
    a = np.cross(d, (0, 0, 1.0) if abs(d[2]) < 0.9 else (1.0, 0, 0)); a /= np.linalg.norm(a)
    b = np.cross(d, a)
    base = np.array(base, float)
    pts = []
    for j in range(6):
        t = 2 * math.pi * j / 6
        off = (math.cos(t) * a + math.sin(t) * b * 0.8) * width / 2
        pts.append(base - d * sink + off)
        pts.append(base + d * length * 0.3 + off * 0.62)
    pts.append(base + d * length)
    return hull(pts)


def claw(base, d, length, width, up=(0, 0, 1)):
    """A small claw: a flat-faced solid from a kite at its root to its point along d, curving down."""
    d = unit(*d)
    a = np.cross(d, np.array(up, float)); a /= np.linalg.norm(a)
    b = np.cross(a, d)
    base = np.array(base, float)
    mid = base + d * length * 0.45
    pts = [base + a * width / 2, base - a * width / 2, base + b * width * 0.4, base - b * width * 0.4 - d * 0.003,
           mid + a * width * 0.28, mid - a * width * 0.28, mid + b * width * 0.25,
           base + d * length - b * length * 0.3]
    return hull(pts)


# The ear's outline in its own plane: a across (-1 the inner edge, toward the head; +1 the outer), b along its length
# (0 the base, sunk in the head; 1 the blunt point), broad at the base, three scallops along the outer, back rim
EAR_OUTLINE = [(-0.8, 0.0), (-0.98, 0.25), (-0.95, 0.55), (-0.72, 0.8), (-0.38, 0.96), (-0.05, 1.0), (0.22, 0.94),
               (0.36, 0.84), (0.55, 0.83), (0.66, 0.7), (0.86, 0.67), (0.92, 0.52), (1.06, 0.42), (1.02, 0.2),
               (0.9, 0.0)]


def ear_frame(d, n):
    """The ear's plane: ez along its length d, ex across it (outward on the +X ear), n its face (right-handed)."""
    d = unit(*d)
    n = np.array(n, float); n = n - d * (n @ d); n /= np.linalg.norm(n)
    return np.cross(d, n), d, n


def pointed_ear(k, base, d, n, length, width, thick, colour, lining_col, rim_col, bone="ear{s}", root=None):
    """
    A great ear as a hard-edged plate: EAR_OUTLINE, width across at its widest and length long, its base sunk into the
    head at base (on the +X side; mirrored), running along d with its face turned to n. Its face is two thinner inset
    plates standing just proud of it (the rim's colour, then the lining's over it toward the inner, upper side), so the
    body's colour stays a rim all round. root: a fused ball (centre, radius) that grows the ear out of the head.
    """
    ex, ez, n = ear_frame(d, n)
    base = np.array(base, float)
    P = [(a * width / 2, b * length) for a, b in EAR_OUTLINE]
    k.add("ear", slab(P, base, ex, ez, thick), colour, bone, fuse=False, mirror=True, surface="skin")
    def inset(scale, at, lift):
        c = np.array(at)
        Q = [((a - c[0]) * scale + c[0], (b - c[1]) * scale + c[1]) for a, b in EAR_OUTLINE]
        return slab([(a * width / 2, b * length) for a, b in Q], base + n * lift, ex, ez, thick)
    k.add("earrim", inset(0.8, (0.0, 0.62), 0.003), rim_col, bone, fuse=False, mirror=True, surface="skin")
    k.add("earlining", inset(0.66, (-0.12, 0.66), 0.006), lining_col, bone, fuse=False, mirror=True, surface="skin")
    if root is not None:
        k.add("earroot", ell(root[0], (root[1],) * 3), colour, bone, mirror=True)


def bold_spine(k, name, base, d, length, width, colour, tip, bone, mirror=False, sink=0.014):
    """A bold spine: a solid cone in the body's colour, its last part a shade darker (a second cone over the first)."""
    d = unit(*d)
    a = np.cross(d, (0, 0, 1.0) if abs(d[2]) < 0.9 else (1.0, 0, 0)); a /= np.linalg.norm(a)
    b = np.cross(d, a)
    base = np.array(base, float)
    def ring(t, r):
        return [base + d * t * length + (math.cos(q) * a + math.sin(q) * b) * r
                for q in np.linspace(0, 2 * math.pi, 8, endpoint=False)]
    R = width / 2
    root = ring(-sink / length, R) + ring(0.62, R * 0.38)
    point = ring(0.56, R * 0.46) + [base + d * length]
    k.add(name, hull(root), colour, bone, fuse=False, mirror=mirror, surface="shell")
    k.add(name + "tip", hull(point), tip, bone, fuse=False, mirror=mirror, surface="shell")


def eye_open(rim=0.84, iris=(0.86, 0.8), pupil=(0.36, 0.5), pupil_c=(-0.12, -0.02), white_back=True):
    """
    The open eye: a red iris filling the patch inside a dark rim, a sliver of white at its back corner, a tall dark
    pupil toward the nose (the glint is a patch of its own).
    """
    def eye(u, v):
        r = np.sqrt(u * u + v * v)
        c = np.tile(RED, (len(u), 1))
        if white_back:
            c[(u > 0.5) & (r < rim)] = WHITE
            c[((u - 0.0) / iris[0]) ** 2 + (v / iris[1]) ** 2 < 1] = RED
        c[((u - pupil_c[0]) / pupil[0]) ** 2 + ((v - pupil_c[1]) / pupil[1]) ** 2 < 1] = PUPIL
        c[r >= rim] = INK
        return c
    return eye


def glint(k, E, de, half, at=(-0.25, 0.4), size=0.26, eps=0.007):
    """A round white glint over the eye, toward the nose and up, on both eyes."""
    right = np.cross((0, 0, 1.0), de); right /= np.linalg.norm(right)
    up = np.cross(de, right)
    g = E + right * at[0] * half[0] + up * at[1] * half[1]
    k.decal(g, de, (half[0] * size, half[0] * size * 1.1), "#FFFFFF", eps=eps, rings=5, segs=20, mirror=True)


def spots(k, places, colour, eps=0.003):
    """Spots on the body: (centre on the surface, outward normal, half sizes, up) each, on both sides."""
    for c, n, half, up in places:
        k.decal(c, n, half, colour, up=up, eps=eps, rings=8, segs=32, mirror=True)


# --- Nidoran's frame -----------------------------------------------------------------------------------------------
TORSO_C, TORSO_R = np.array([0.0, 0.035, 0.17]), np.array([0.126, 0.14, 0.125])
HEAD_C, HEAD_R = np.array([0.0, -0.12, 0.322]), np.array([0.118, 0.108, 0.104])
SNOUT_C, SNOUT_R, SNOUT_T = np.array([0.0, -0.205, 0.302]), np.array([0.082, 0.08, 0.064]), (12, 0, 0)


def pear(V):
    """Wider toward the rump (+y)."""
    V = V.copy()
    back = np.clip(V[:, 1], 0, 1)
    V[:, 0] *= 1 + 0.16 * back
    V[:, 2] *= 1 + 0.1 * back
    return V


def nidoranf(src, out):
    k = Kit(src, coat="fur")

    # The body: an egg wider at the rump, deep to within a few hundredths of the ground, a chest under the head
    k.add("torso", ell(TORSO_C, TORSO_R, deform=pear), SKIN, "body")
    k.add("belly", ell((0, 0.01, 0.11), (0.112, 0.15, 0.085)), SKIN, "body")
    k.add("chest", ell((0, -0.07, 0.19), (0.1, 0.088, 0.11)), SKIN, "body")
    k.add("tailnub", ell((0, 0.2, 0.17), (0.035, 0.03, 0.03)), SKIN, "tail")

    # The head: a round crown, a long blunt snout tipped a little up, full cheeks
    k.add("head", ell(HEAD_C, HEAD_R), SKIN, "head")
    k.add("snout", ell(SNOUT_C, SNOUT_R, rot=SNOUT_T), SKIN, "head")
    k.add("cheek", ell((0, -0.15, 0.285), (0.088, 0.07, 0.06)), SKIN, "head")

    # The legs: squat, fat shoulders and haunches fused into the flanks, a short stub of shin, a wide foot, two white
    # claws a foot
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"shoulder{side}", ell(np.array((0.078, -0.08, 0.11)) * m, (0.062, 0.07, 0.085)), SKIN, f"leg{side}")
        k.add(f"leg{side}", tube(np.array([(0.082, -0.095, 0.07), (0.085, -0.1, 0.03)]) * m, [0.05, 0.047]), SKIN,
              f"leg{side}")
        k.add(f"paw{side}", ell(np.array((0.085, -0.115, 0.02)) * m, (0.054, 0.062, 0.022)), SKIN, f"leg{side}")
        k.add(f"haunch{side}", ell(np.array((0.084, 0.12, 0.11)) * m, (0.08, 0.098, 0.1)), SKIN, f"hind{side}")
        k.add(f"hind{side}", tube(np.array([(0.09, 0.13, 0.06), (0.092, 0.125, 0.03)]) * m, [0.05, 0.047]), SKIN,
              f"hind{side}")
        k.add(f"hpaw{side}", ell(np.array((0.092, 0.105, 0.02)) * m, (0.054, 0.062, 0.022)), SKIN, f"hind{side}")
        for foot, y, bone in (("f", -0.17, f"leg{side}"), ("h", 0.05, f"hind{side}")):
            for j, dx in enumerate((-0.016, 0.016)):
                b = np.array((0.087 + dx, y, 0.016))
                k.add(f"claw{foot}{side}{j}", claw(b * m, np.array((dx * 0.8, -1, -0.5)) * m, 0.022, 0.015), CLAW,
                      bone, fuse=False, surface="shell")

    # The ears: broad at the base and low on the sides of the head, swept back and out to a blunt point, the face
    # turned forward and out; a teal lining inside a violet rim, the body's colour round both
    EB = on_ell(HEAD_C, HEAD_R, (0.78, 0.12, 0.62))
    d = unit(0.62, 0.55, 0.75)
    pointed_ear(k, EB - d * 0.03, d, (0.55, -1.0, 0.25), 0.24, 0.21, 0.022, SKIN, LINING, RIM,
                root=(EB - d * 0.02, 0.032))

    # The horn on the forehead, the body's colour
    hb = on_ell(HEAD_C, HEAD_R, (0, -0.42, 1))
    bold_spine(k, "horn", hb, (0, -0.15, 1), 0.06, 0.038, SKIN, SPINE_TIP, "head")

    # A few bold spines: two down the back, one on each haunch, a shade darker at the tip
    for j, (dd, lean, length) in enumerate([((0, 0.2, 1), (0, 0.55, 1), 0.06), ((0, 0.8, 0.7), (0, 1, 0.6), 0.055)]):
        bold_spine(k, f"spine{j}", on_ell(TORSO_C, TORSO_R, dd), lean, length, 0.042, SKIN, SPINE_TIP, "body")
    bold_spine(k, "flankspine", on_ell(TORSO_C + (0, 0.02, 0.0), TORSO_R * (1.1, 1.0, 1.05), (0.7, 0.62, 0.45)),
               (0.8, 0.6, 0.55), 0.05, 0.038, SKIN, SPINE_TIP, "body", mirror=True)

    # Spots on the back and the flanks
    S = []
    for d, half in [((0.25, 0.1, 1), (0.024, 0.02)), ((0.75, 0.25, 0.55), (0.026, 0.02)),
                    ((0.85, 0.6, 0.0), (0.028, 0.022)), ((0.55, 0.55, 0.65), (0.02, 0.016)),
                    ((0.9, -0.2, 0.15), (0.018, 0.014))]:
        n = unit(*d)
        S.append((on_ell(TORSO_C, TORSO_R, n), n, half, (0, 0, 1)))
    spots(k, S, SPOT)

    # The mouth: open under the snout's tip, dark pink with a lighter tongue low in it
    dm = unit(0, -0.72, -0.7)
    MC = on_ell(SNOUT_C, SNOUT_R, dm, SNOUT_T)
    dark, tongue = srgb(MOUTH), srgb(TONGUE)
    def mouth(u, v):
        c = np.tile(dark, (len(u), 1))
        c[(u / 0.7) ** 2 + ((v + 0.55) / 0.55) ** 2 < 1] = tongue
        return c
    k.decal(MC, dm, (0.044, 0.038), mouth, eps=0.003, outline=polygon([(-1, 0.75), (1, 0.75), (0.75, -0.3),
                                                                       (0.3, -0.85), (-0.3, -0.85), (-0.75, -0.3)]),
            rings=10, segs=48)
    # Two white buck teeth hanging from the upper lip
    tip = on_ell(SNOUT_C, SNOUT_R, unit(0, -1, -0.32), SNOUT_T)
    for s in (-1, 1):
        c = tip + (s * 0.0105, 0.006, -0.008)
        k.add(f"tooth{'R' if s > 0 else 'L'}", hull([c + (sx * 0.0085, sy * 0.004, sz) for sx in (-1, 1)
                                                     for sy in (-1, 1) for sz in (0.008, -0.016)]), TOOTH, "head",
              fuse=False, surface="shell")

    # Whiskers: two short white ones a cheek
    for j, (dz, length) in enumerate([(0.12, 0.05), (-0.2, 0.045)]):
        b = on_ell(SNOUT_C, SNOUT_R, unit(1, -0.3, 0.05 + dz), SNOUT_T) - np.array([0.008, 0, 0])
        d = unit(1, 0.35, dz)
        k.add(f"whisker{j}", tube([b, b + d * length * 0.6, b + d * length], [0.0055, 0.005, 0.0035], seg=10, per=4),
              WHISKER, "head", fuse=False, mirror=True, surface="shell")

    # The eyes: red, on the sides of the face above the snout, turned forward
    de = unit(0.6, -0.72, 0.3)
    E = on_ell(HEAD_C, HEAD_R, de)
    half = (0.034, 0.029)
    eye = eye_open()
    LID = srgb(SKIN)
    def eyes(state):
        k.decal(E, de, half, eye_look(state, eye, LID, INK, line=0.15), up=(0, 0.25, 1), eps=0.004, rings=20,
                segs=56, mirror=True)
        if state == "Open":
            glint(k, E, de, half, at=(-0.3, 0.38), size=0.28, eps=0.006)

    k.build(voxel=0.0048, smooth=4)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    nidoranf(*args())
