# Stakataka sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/stakataka.py -- <the export's Stakataka.glb> <out.glb>
#
# The design, from the references: a tower of grey stone bricks, a square in plan, a little narrower at its top and
# flaring at its foot, laid in courses of running bond with dark mortar between. Down one side near the front corner a
# zigzag seam of glowing blue light, and here and there a brick missing, a black block in the hole with one round eye in
# it: a cyan ring round a black middle. The underside is black, ringed with blue light and dotted with the same eyes.
# Four crooked legs of stacked cubes come out from under the tower's corners, grey stones and black cubes with an eye
# each, the front pair splayed out, the back pair more nearly straight down.
# The brief expected red eyes: the pictures' eyes are cyan rings in black blocks, and the blue lines are cyan light.
# Skeleton: root, body (0.5), legL/legR (front) and hindL/hindR (back) at (+-0.11, -+0.07, 0.26).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from mathutils import Euler
from sculptkit import Kit, ell, srgb, unit, boxy, eye_look, args, hull, outward

# --- colours ----------------------------------------------------------------------------------------------------------
STONE, STONE_DARK, MORTAR = "#8E8F94", "#74757B", "#3A3B40"
BLACK, CYAN, CYAN_DARK = "#17181C", "#4CD4F4", "#1E7C98"

# --- the frame --------------------------------------------------------------------------------------------------------
Z_BOT, Z_FLARE, Z_TOP = 0.315, 0.405, 0.965     # the tower's foot, where the flare ends, its top
COURSES = 8                                      # above the flare, each some 0.07 tall
DEPTH, GAP = 0.034, 0.0055                       # a brick's depth into the tower, half the mortar's width
SEAM_Y = (-0.115, -0.09, -0.115, -0.1, -0.075, -0.1, -0.12, -0.095)     # the blue seam's place on +X, by course


def half(z):
    """The tower's half width at height z: flared at its foot, narrowing a little to its top."""
    z = np.asarray(z, float)
    return np.where(z < Z_FLARE, 0.282 - (z - Z_BOT) / (Z_FLARE - Z_BOT) * 0.044,
                    0.238 - (z - Z_FLARE) / (Z_TOP - Z_FLARE) * 0.022)


def shade(h, f):
    c = np.clip(np.array([int(h[i:i + 2], 16) for i in (1, 3, 5)]) * f, 0, 255).astype(int)
    return "#%02X%02X%02X" % tuple(c)


def join(shapes):
    """Several shapes as one (one material for a glow)."""
    V, F, off = [], [], 0
    for v, f in shapes:
        V.append(np.asarray(v)); F.extend([[i + off for i in q] for q in f]); off += len(v)
    return np.vstack(V), F


def course_z(i):
    """The bottom and top of course i (0 and 1 are the flare's two, then COURSES above)."""
    if i < 2:
        zs = np.linspace(Z_BOT, Z_FLARE, 3)
        return zs[i], zs[i + 1]
    zs = np.linspace(Z_FLARE, Z_TOP, COURSES + 1)
    return zs[i - 2], zs[i - 1]


def face_point(face, t, z, inset):
    """A point of face (0 +X, 1 -Y front, 2 -X, 3 +Y back) at t across it (along the face, -1..1 of the half width
    measured at that height) and height z, inset that far into the tower."""
    h = float(half(z))
    w = h - inset
    if face == 0:
        return np.array([w, t * h, z])
    if face == 1:
        return np.array([t * h, -w, z])
    if face == 2:
        return np.array([-w, -t * h, z])
    return np.array([-t * h, w, z])


def brick(face, a, b, z0, z1, push=0.0):
    """A brick of face across from a to b (in half widths) between heights z0 and z1: a hull with its outer face on the
    tower's slope."""
    pts = []
    for t in (a, b):
        for z in (z0 + GAP, z1 - GAP):
            pts.append(face_point(face, t, z, -push))
            pts.append(face_point(face, t, z, DEPTH))
    return hull(pts)


# Holes in the wall where a brick is missing and a black block looks out: (face, course, t of its middle)
WINDOWS = [(1, 3, -0.3), (0, 7, -0.05), (2, 5, 0.25), (3, 4, 0.1)]


def tower(k, rs):
    # the mortar: a dark core just inside the bricks
    # (two hulls: one over the flare and one over the rest, since one hull of the whole would bulge over the flare's
    # hollow and cover the bricks)
    for j, (za, zb) in enumerate(((Z_BOT, Z_FLARE), (Z_FLARE, Z_TOP - 0.004))):
        core = []
        for z in (za, zb):
            h = float(half(z)) - 0.012
            core += [(sx * h, sy * h, z) for sx in (-1, 1) for sy in (-1, 1)]
        k.add(f"core{j}", hull(core), MORTAR, "body", fuse=False, surface="shell")
    # the top: flat stones laid over it, a grid of four by four with mortar between
    ht = float(half(Z_TOP)) - 0.004
    step = 2 * ht / 4
    for a in range(4):
        for b in range(4):
            x0, y0 = -ht + a * step + GAP, -ht + b * step + GAP
            k.add(f"top{a}{b}", hull([(x, y, z) for x in (x0, x0 + step - 2 * GAP) for y in (y0, y0 + step - 2 * GAP)
                                      for z in (Z_TOP - 0.03, Z_TOP + rs.uniform(0, 0.004))]),
                  shade(STONE, rs.uniform(0.95, 1.1)), "body", fuse=False)
    windows = {}
    n = 0
    for i in range(COURSES + 2):
        z0, z1 = course_z(i)
        for face in range(4):
            full = (face % 2) == (i % 2)              # corners interleave course by course
            lo, hi = (-1.0, 1.0)
            # a face that doesn't own the corners stops at the other face's bricks
            hz = float(half((z0 + z1) / 2))
            if not full:
                lo, hi = -1 + DEPTH / hz, 1 - DEPTH / hz
            # running bond: joints at random lengths, shifted course by course
            cuts = [lo]
            t = lo + (0.18 if i % 2 else 0.42) * rs.uniform(0.7, 1.2)
            while t < hi - 0.25:
                cuts.append(t)
                t += rs.uniform(0.5, 0.78)
            cuts.append(hi)
            # the seam on +X splits its course where the blue light runs
            if face == 0 and i >= 2:
                sy = SEAM_Y[i - 2] / hz
                cuts = sorted([c for c in cuts if abs(c - sy) > 0.12] + [sy - 0.03, sy + 0.03])
            hole = next((w for w in WINDOWS if w[0] == face and w[1] == i), None)
            if hole is not None:
                m = hole[2]
                cuts = sorted([c for c in cuts if abs(c - m) > 0.32 or c in (lo, hi)] + [m - 0.2, m + 0.2])
                windows[(face, i)] = (m, z0, z1)
            for a, b in zip(cuts, cuts[1:]):
                if face == 0 and i >= 2 and abs((a + b) / 2 - SEAM_Y[i - 2] / hz) < 0.035:
                    continue                          # the seam's own gap
                if hole is not None and abs((a + b) / 2 - hole[2]) < 0.01:
                    continue                          # the hole a brick is missing from
                col = shade(STONE_DARK if rs.rand() < 0.2 else STONE, rs.uniform(0.92, 1.07))
                k.add(f"brick{n}", brick(face, a, b, z0, z1, rs.uniform(0, 0.004)), col, "body", fuse=False)
                n += 1
    # the seam's light, one piece: a strip in each course's gap and a short run across between courses
    strips = []
    for i in range(2, COURSES + 2):
        z0, z1 = course_z(i)
        y = SEAM_Y[i - 2]
        h0 = float(half(z0)); h1 = float(half(z1))
        strips.append(hull([(h - 0.016, y + dy, z) for h, z in ((h0, z0), (h1, z1)) for dy in (-0.008, 0.008)]
                           + [(h - 0.004, y + dy, z) for h, z in ((h0, z0), (h1, z1)) for dy in (-0.005, 0.005)]))
        if i < COURSES + 1:
            y2 = SEAM_Y[i - 1]
            a, b = min(y, y2) - 0.008, max(y, y2) + 0.008
            strips.append(hull([(h1 - dx, yy, z1 + dz) for dx in (0.004, 0.016) for yy in (a, b) for dz in (-0.005, 0.005)]))
    k.add("seam", join(strips), CYAN, "body", fuse=False, glow=CYAN, surface="shell")
    return windows


def windows(k, holes):
    """A black block set back in each hole (fused: its front carries the eye's patch). Returns where each eye goes."""
    eyes = []
    for (face, i), (m, z0, z1) in holes.items():
        zc = (z0 + z1) / 2
        p = face_point(face, m, zc, 0.006)
        nrm = face_point(face, m, zc, -1.0) - face_point(face, m, zc, 0.0)
        nrm = nrm / np.linalg.norm(nrm)
        hz = float(half(zc))
        w = 0.2 * hz - 0.004
        hh = (z1 - z0) / 2 - GAP
        tang = face_point(face, 1, zc, 0) - face_point(face, 0, zc, 0)
        tang = tang / np.linalg.norm(tang)
        # a box with rounded edges, fused, on the body
        c = p - nrm * 0.015
        r = np.abs(np.array([nrm[0] * 0.015 + tang[0] * w, nrm[1] * 0.015 + tang[1] * w, hh]))
        r = np.maximum(r, 0.015)
        k.add(f"window{face}{i}", ell(c, r, deform=boxy(6), sub=4), BLACK, "body")
        eyes.append((p, nrm, min(w, hh) * 0.82))
        # a thin blue line round the hole's mouth: four strips joined
        edge = []
        for dt, dz, lt, lz in ((0, hh + 0.002, w, 0.0025), (0, -hh - 0.002, w, 0.0025), (w + 0.002, 0, 0.0025, hh), (-w - 0.002, 0, 0.0025, hh)):
            q = p + tang * dt + np.array([0, 0, dz]) + nrm * 0.004
            edge.append(hull([q + tang * a + np.array([0, 0, b]) + nrm * e for a in (-lt, lt) for b in (-lz, lz) for e in (-0.012, 0.0)]))
        k.add(f"glint{face}{i}", join(edge), CYAN, "body", fuse=False, glow=CYAN, surface="shell")
    return eyes


def underside(k):
    """The black underside, set just inside the flare's foot, ringed with blue light."""
    h = float(half(Z_BOT)) - 0.02
    k.add("under", ell((0, 0, Z_BOT + 0.012), (h, h, 0.02), deform=boxy(8), sub=5), BLACK, "body")
    ring = []
    hb = float(half(Z_BOT))
    for s in (1, -1):
        ring.append(hull([(x, s * (hb - dy), Z_BOT + dz) for x in (-hb + 0.01, hb - 0.01) for dy in (0.012, 0.022) for dz in (-0.004, 0.004)]))
        ring.append(hull([(s * (hb - dy), y, Z_BOT + dz) for y in (-hb + 0.01, hb - 0.01) for dy in (0.012, 0.022) for dz in (-0.004, 0.004)]))
    k.add("underglow", join(ring), CYAN, "body", fuse=False, glow=CYAN, surface="shell")
    return [(np.array([x, y, Z_BOT - 0.008]), np.array([0, 0, -1.0])) for x, y in
            ((-0.15, -0.16), (-0.03, -0.17), (0.09, -0.15), (0.17, -0.02), (-0.17, 0.05), (0.02, 0.0), (0.1, 0.13), (-0.06, 0.15))]


# --- legs: crooked stacks of cubes ------------------------------------------------------------------------------------
LEGS = {
    # bone: points of the stack from under the tower to the ground (on +X; mirrored by the bone's side)
    "leg": [(0.2, -0.17, 0.27), (0.29, -0.22, 0.24), (0.36, -0.25, 0.17), (0.39, -0.27, 0.1), (0.38, -0.27, 0.052)],
    "hind": [(0.2, 0.17, 0.27), (0.25, 0.21, 0.205), (0.29, 0.24, 0.14), (0.3, 0.255, 0.08), (0.31, 0.26, 0.052)],
}
BLACK_AT = {"leg": (1, 3), "hind": (0, 2)}       # which cubes of a stack are black with an eye


def cube(c, s, rot):
    m = np.array(Euler([math.radians(a) for a in rot]).to_matrix())
    pts = [np.array([x, y, z]) * s for x in (-1, 1) for y in (-1, 1) for z in (-1, 1)]
    return hull([m @ p + c for p in pts])


def legs(k, rs):
    marks = []
    for kind, path in LEGS.items():
        for side, sgn in (("R", 1), ("L", -1)):
            bone = f"{kind}{side}"
            for j, p in enumerate(path):
                c = np.array(p) * (sgn, 1, 1)
                s = 0.04 if j < len(path) - 1 else 0.042
                rot = (rs.uniform(-14, 14), rs.uniform(-14, 14), rs.uniform(-20, 20))
                if j in BLACK_AT[kind]:
                    k.add(f"blk{kind}{j}{side}", ell(c, (s, s, s), rot=rot, deform=boxy(7), sub=4), BLACK, bone)
                    out_dir = unit(sgn * 0.8, -0.6 if kind == "leg" else 0.6, 0.05)
                    marks.append((c + out_dir * s * 0.98, out_dir, s * 0.62))
                else:
                    k.add(f"stone{kind}{j}{side}", cube(c, s, rot), shade(STONE, rs.uniform(0.9, 1.05)), bone, fuse=False)
    return marks


# --- the eyes: a cyan ring round a black middle ------------------------------------------------------------------------
def eye_open():
    ck, cc, cd = srgb(BLACK), srgb(CYAN), srgb(CYAN_DARK)
    def look(u, v):
        r = np.hypot(u, v)
        c = np.tile(ck, (len(u), 1))
        c[(r > 0.3) & (r < 0.82)] = cc
        c[(r > 0.3) & (r < 0.45)] = cd
        c[(u + 0.3) ** 2 + (v - 0.42) ** 2 < 0.14 ** 2] = (1, 1, 1)
        return c
    return look


def stakataka(src, out_path):
    rs = np.random.RandomState(805)
    k = Kit(src, coat="shell")
    holes = tower(k, rs)
    eyes = windows(k, holes)
    dots = underside(k)
    marks = legs(k, rs)
    look = eye_open()
    # the eyes of the cubes and of the underside are in every state: they are other Pokémon's
    for c, n, r in marks:
        k.decal(c, n, (r, r), look, eps=0.002, rings=12, segs=40)
    for c, n in dots:
        k.decal(c, n, (0.022, 0.022), look, eps=0.002, up=(0, 1, 0), rings=12, segs=40)
    k.build(voxel=0.006, smooth=3, band=0.004)

    def lay(state):
        lk = eye_look(state, look, srgb(BLACK), srgb(CYAN), line=0.18)
        for p, nrm, r in eyes:
            k.decal(p, nrm, (r, r), lk, eps=0.002, rings=20, segs=56)
    k.eye_states(lay)

    V = np.array([v.co[:] for v in bpy_obj_vertices(k)])
    a = math.radians(31.5)
    proj = V[:, 0] * math.cos(a) + V[:, 1] * math.sin(a)
    h, w, l = V[:, 2].max(), np.ptp(V[:, 0]), np.ptp(V[:, 1])
    print("KIT: board: shows %.3f in a square of %.3f" % (np.ptp(proj), 1.3 * max(h, 0.85 * max(w, l))))
    k.export(out_path)


def bpy_obj_vertices(k):
    import bpy
    return [v for o in bpy.data.objects if o.type == "MESH" and o.name == k.name for v in o.data.vertices]


if __name__ == "__main__":
    stakataka(*args())
