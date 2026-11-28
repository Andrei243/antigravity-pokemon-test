# Stejaron (No. 10098, Carpathia) sculpted from nothing on the skeleton and clips of the game's own export of it:
#   blender --background --factory-startup --python sculpts/stejaron.py -- <the export's Stejaron.glb> <out.glb>
#
# Stejarel grown (stejarel.py and ghindel.py through sibling): the line's rough acorn cap, its lobed oak leaves in a
# crown, its eye cut by a brow sloping to the nose. From its picture (10098-Stejaron.jpg) and its brief
# (CP-dex-2b-mountains-more.md, 98), largest first: a wide crown of dark oak leaves over its back and shoulders, some
# 0.85 of the height across; a hunched giant of dark gnarled bark, a barrel chest and great shoulders the head sinks
# between, a pinched waist; long thick arms hanging to the knees ending in root claws; thick bent legs set wide, each
# spreading into roots; bands of riveted steel over it: a breastplate, pauldrons on the shoulders, a belt plate, bracers
# on the forearms and plates on the knees; acorns set along its limbs like rivets. The head, about a third of the
# height with its beard: a lumpy acorn cap of hair, a lighter bark face with a heavy dark brow over two big amber eyes,
# a broad nose, a frown, a beard of thick bark locks down over the chest.
# Skeleton: body (0, 0, 0.905), arms (+-0.36, 0, 1.145), head (0, -0.08, 1.245), crown (0, 0.1, 1.405),
# legs (+-0.17, 0, 0.605).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, sheet, outward, axes, ring_band, srgb, inside, smoothstep, unit, eye_look, \
    args, sibling

G = sibling("ghindel")
SJ = sibling("stejarel")

BARK, BARK_D, FACE, BEARD = "#5E4A3C", "#3E3129", "#A2835F", "#4A3A30"
STEEL, STEEL_HI, RIVET = "#8E959E", "#AEB4BC", "#B9BEC5"
OAK, OAK_D, OAK_CORE = "#36643A", "#27502C", "#1F4424"
AMBER, AMBER_INK = "#F2A62E", "#5A3410"

HC = np.array([0.0, -0.2, 1.24])
HR = np.array([0.19, 0.17, 0.2])
CHEST = np.array([0.0, -0.03, 1.15]); CHEST_R = np.array([0.44, 0.29, 0.21])
HIPS = np.array([0.0, 0.0, 0.62]); HIPS_R = np.array([0.31, 0.23, 0.15])
SHOULDER = np.array([0.42, 0.0, 1.2]); SHOULDER_R = 0.17
EYE = np.array([0.088, -0.347, 1.276])
EYE_HALF = (0.05, 0.043)
CANOPY_AT = np.array([0.0, 0.24, 1.36])
CANOPY_CLUMPS = [(0.0, 0.02, 0.16, 0.24), (-0.29, 0.0, 0.1, 0.21), (0.3, -0.02, 0.11, 0.22), (-0.44, 0.06, -0.04, 0.18),
                 (0.44, 0.05, -0.03, 0.175), (0.0, 0.16, 0.0, 0.23), (-0.24, 0.14, -0.1, 0.19), (0.25, 0.14, -0.09, 0.19)]
VOXEL = 0.0162


def ell_plate(c, r, a0, a1, e0, e1, off=0.012, thick=0.022, nu=41, nv=9):
    """A curved steel plate lying over an ellipsoid: azimuth a (degrees, nought at the front, toward +X positive) from
    a0 to a1, elevation e from e0 to e1, off its surface."""
    c, r = np.array(c, float), np.array(r, float)
    def at(u, v):
        a = np.radians(a0 + (a1 - a0) * u); e = np.radians(e0 + (e1 - e0) * v)
        d = np.stack([np.sin(a) * np.cos(e), -np.cos(a) * np.cos(e), np.sin(e)], axis=-1)
        n = np.linalg.norm(d / r, axis=-1, keepdims=True)
        return c + d / n + d * off
    return outward(sheet(at, nu, nv, thick))


def plate_point(c, r, a, e, off=0.012):
    c, r = np.array(c, float), np.array(r, float)
    a, e = math.radians(a), math.radians(e)
    d = np.array([math.sin(a) * math.cos(e), -math.cos(a) * math.cos(e), math.sin(e)])
    p = c + d / np.linalg.norm(d / r) + d * off
    n = (p - c) / r ** 2
    return p, n / np.linalg.norm(n)


def rivets(k, name, c, r, a0, a1, e, n, bone, off=0.024, size=0.022):
    """A row of round rivets along a plate's edge."""
    for i in range(n):
        a = a0 + (a1 - a0) * (i + 0.5) / n
        p, nn = plate_point(c, r, a, e, off)
        k.add(f"{name}{i}", outward(ell(p, (size, size, size), sub=2)), RIVET, bone, fuse=False, surface="metal")


def plate(k, name, c, r, a0, a1, e0, e1, bone, rows=True, nr=5):
    k.add(name, ell_plate(c, r, a0, a1, e0, e1), STEEL, bone, fuse=False, surface="metal")
    if rows:
        for j, e in enumerate((e0 + (e1 - e0) * 0.16, e1 - (e1 - e0) * 0.16)):
            rivets(k, f"{name}r{j}", c, r, a0, a1, e, nr, bone)


def stud_acorn(k, name, p, n, size, bone, down=(0, 0, -1)):
    """An acorn set in the bark like a rivet: half sunk at p (n the bark's normal there), lying along the bark with
    its nut's point toward down and its rough cap above, so it reads as an acorn from the side."""
    d = np.array(down, float); n = np.array(n, float)
    d = d - n * (d @ n); d /= np.linalg.norm(d)
    a = np.cross(n, d)
    M = np.stack([a, n, -d])          # local x, y, z: across, out of the bark, up the acorn
    centre = np.array(p, float) + n * size * 0.35
    nut = ell((0, 0, 0), (size * 0.72, size * 0.72, size * 0.95), deform=G.pointed, sub=2)
    k.add(name + "nut", outward((nut[0] @ M + centre - (-d) * 0.0, nut[1])), G.NUT, bone, fuse=False,
          surface="shell")
    cap = ell((0, 0, 0), (size * 0.85, size * 0.85, size * 0.5), deform=G.rough(5, n=20, amp=0.08, s=0.4), sub=2)
    k.add(name + "cap", outward((cap[0] @ M + centre - d * size * 0.62, cap[1])), G.CAPC, bone, fuse=False,
          surface="shell")


def limb_point(a, b, t, r, side_dir):
    """A point on a limb tube from a to b at share t, on its surface toward side_dir, and the normal there."""
    a, b = np.array(a, float), np.array(b, float)
    d = unit(*(b - a)); s = np.array(side_dir, float); s = s - d * (s @ d); s /= np.linalg.norm(s)
    return a + (b - a) * t + s * r, s


def stejaron(src, out_path):
    k = Kit(src, coat="leather")

    # ---- the trunk: a barrel chest and great shoulders, a pinched waist, the hips
    k.add("chest", ell(CHEST, CHEST_R), BARK, "body")
    k.add("belly", ell((0, -0.01, 0.86), (0.3, 0.23, 0.24)), BARK, "body")
    k.add("hips", ell(HIPS, HIPS_R), BARK, "body")
    k.add("shoulder", ell(SHOULDER, (SHOULDER_R, SHOULDER_R, SHOULDER_R * 0.95)), BARK, "arm{s}", mirror=True)
    k.add("neck", ell((0, -0.1, 1.24), (0.18, 0.16, 0.12)), BARK, "head")
    # bark cracks down the belly and back, in the body's own frame
    for a, z0, z1 in ((0.35, 0.72, 1.0), (-0.3, 0.7, 1.02), (2.2, 0.7, 1.3), (-2.4, 0.72, 1.28), (3.1, 0.75, 1.3),
                      (0.0, 0.7, 0.96)):
        def crack(P, a=a, z0=z0, z1=z1):
            th = np.arctan2(P[:, 0], -P[:, 1])
            d = np.abs((th - a - 0.06 * np.sin(P[:, 2] * 18) + math.pi) % (2 * math.pi) - math.pi) * 0.3
            return inside(d - 0.009, 0.004) * (P[:, 2] > z0) * (P[:, 2] < z1) * inside(np.abs(P[:, 0]) - 0.36)
        k.paint(crack, BARK_D, on=["chest", "belly"])

    # ---- the legs: thick, bent, set wide, spreading into roots
    for s in (1, -1):
        side = "R" if s > 0 else "L"
        hip, knee, ankle = np.array([0.2 * s, 0.0, 0.62]), np.array([0.3 * s, -0.07, 0.34]), np.array(
            [0.34 * s, 0.0, 0.14])
        k.add("thigh" + side, tube([hip, (hip + knee) / 2 + (0.01 * s, -0.02, 0), knee], [0.16, 0.15, 0.13]), BARK,
              "leg" + side)
        k.add("shin" + side, tube([knee, ankle], [0.13, 0.145]), BARK, "leg" + side)
        for j, a in enumerate((-70, -25, 20, 70, 140)):
            ar = math.radians(a)
            d = np.array([math.sin(ar) * s, -math.cos(ar), 0])
            pts = [ankle * (1, 1, 0) + (0, 0, 0.12), ankle * (1, 1, 0) + d * 0.17 + (0, 0, 0.07), ankle * (1, 1, 0) + d * 0.28 + (0, 0, 0.038)]
            k.add(f"root{side}{j}", tube(pts, [0.075, 0.05, 0.034]), BARK, "leg" + side)
        # the knee plate and the shin's acorns
        plate(k, "knee" + side, knee, (0.14, 0.14, 0.13), -75, 75, -45, 45, "leg" + side, nr=4)
        for t, sd in ((0.45, (s * 0.8, -0.6, 0)), (0.75, (s * 0.9, 0.4, 0))):
            p, n = limb_point(knee, ankle, t, 0.135, sd)
            stud_acorn(k, f"shinacorn{side}{t}", p, n, 0.034, "leg" + side)
    plate(k, "belt", HIPS, HIPS_R, -80, 80, -25, 35, "body", nr=6)

    # ---- the arms: long and thick, hanging to the knees, root claws; bracers and acorns
    for s in (1, -1):
        side = "R" if s > 0 else "L"
        sh, el, wr = np.array([0.43 * s, 0.0, 1.17]), np.array([0.58 * s, -0.05, 0.84]), np.array(
            [0.63 * s, -0.11, 0.54])
        k.add("upper" + side, tube([sh, el], [0.145, 0.12]), BARK, "arm" + side)
        k.add("fore" + side, tube([el, wr], [0.125, 0.12]), BARK, "arm" + side)
        palm = wr + np.array([0.005 * s, -0.02, -0.08])
        k.add("palm" + side, ell(palm, (0.12, 0.11, 0.1)), BARK, "arm" + side)
        for j, (dx, dy) in enumerate(((-0.07, -0.06), (0.0, -0.09), (0.07, -0.06), (0.09, 0.04))):
            f0 = palm + np.array([dx * s, dy, -0.04])
            pts = [f0, f0 + (dx * s * 0.25, dy * 0.5 - 0.03, -0.07), f0 + (dx * s * 0.4, dy * 0.6 - 0.05, -0.15)]
            k.add(f"claw{side}{j}", tube(pts, [0.05, 0.04, 0.028]), BARK_D if j == 3 else BARK, "arm" + side)
        k.add("bracer" + side, ring_band((el * 0.4 + wr * 0.6), wr - el, 0.13, 0.15, thick=0.022), STEEL,
              "arm" + side, fuse=False, surface="metal")
        for t in (0.25, 0.75):
            ax = unit(*(wr - el))
            c = el + (wr - el) * (0.4 + 0.6 * 0.5) + ax * (t - 0.5) * 0.12
            for q in range(4):
                ang = math.radians(-90 + 60 * q)
                a, b = axes(ax)
                dd = math.cos(ang) * a + math.sin(ang) * b
                if dd[1] > 0.4:
                    continue
                k.add(f"bracerriv{side}{t}{q}", outward(ell(c + dd * 0.15, (0.02, 0.02, 0.02), sub=2)), RIVET,
                      "arm" + side, fuse=False, surface="metal")
        for j, (t, sd) in enumerate(((0.3, (s, -0.3, 0.2)), (0.7, (s, -0.5, 0)), (0.5, (s * 0.3, -1, 0)))):
            p, n = limb_point(sh, el, t, 0.14, sd)
            stud_acorn(k, f"upacorn{side}{j}", p, n, 0.036, "arm" + side)
        p, n = limb_point(el, wr, 0.25, 0.125, (s, -0.4, 0))
        stud_acorn(k, f"foreacorn{side}", p, n, 0.036, "arm" + side)
        # the pauldron
        k.add("pauldron" + side, ell_plate(SHOULDER * np.array([s, 1, 1]), (0.185, 0.185, 0.175), -180, 180, 18, 84,
                                           nu=61), STEEL, "arm" + side, fuse=False, surface="metal")
        rivets(k, f"pauldronr{side}", SHOULDER * np.array([s, 1, 1]), (0.185, 0.185, 0.175), -160, 160, 30, 7,
               "arm" + side)

    # ---- the breastplate: a broad band over the chest, a V of straps up to the shoulders
    plate(k, "breast", CHEST, CHEST_R, -62, 62, -40, 12, "body", rows=False)
    for s in (1, -1):
        for j, e in enumerate((-32, 4)):
            rivets(k, f"breastr{s}{j}", CHEST, CHEST_R, 24 * s, 60 * s, e, 3, "body")
    plate(k, "backband", CHEST, CHEST_R, 125, 235, -38, -8, "body", nr=5)
    for s in (1, -1):
        for j in range(3):
            p, n = plate_point(CHEST, CHEST_R, s * (20 + 16 * j), 22 + 10 * j)
            k.add(f"strap{s}{j}", outward(ell(p, (0.075, 0.03, 0.04), rot=(0, -s * 35, s * 20))), STEEL, "body",
                  fuse=False, surface="metal")
    for s in (1, -1):
        p, n = plate_point(CHEST, CHEST_R, s * 80, 0)
        stud_acorn(k, f"sideacorn{s}", p, n, 0.036, "body")

    # ---- the head: a lighter bark face, a broad nose, the acorn cap of hair, a beard of bark locks
    k.add("head", ell(HC, HR), FACE, "head")
    k.add("jaw", ell(HC + (0, -0.03, -0.12), (0.17, 0.14, 0.1)), FACE, "head")
    k.add("nose", ell(HC + (0, -0.172, -0.05), (0.048, 0.05, 0.075), rot=(-10, 0, 0)), FACE, "head")
    G.acorn_cap(k, "hair", HC + (0, 0.02, 0.15), (0.225, 0.215, 0.12), (8, 0, 0), "head", seed=17, fuse=True,
                flecks=0)
    for j, x in enumerate((-0.12, -0.07, -0.024, 0.024, 0.07, 0.12)):
        top = HC + np.array([x, -0.13 + 0.25 * abs(x) ** 1.3, -0.15 + 0.3 * x * x])
        end = top + np.array([x * 0.2, -0.04, -0.2 + 0.4 * abs(x)])
        k.add(f"beard{j}", tube([top, (top + end) / 2 + (0, -0.02, 0), end], [0.045, 0.036, 0.016]), BEARD, "head")
    # the hair's knobs darker in places; the brow a dark heavy band, cut over the eyes by their own patches
    def hair_flecks(P):
        q = P - (HC + (0, 0.02, 0.15))
        return inside(np.sin(q[:, 0] * 60) * np.sin(q[:, 1] * 55) * np.sin(q[:, 2] * 70) - 0.55, 0.06)
    k.paint(hair_flecks, G.CAP_D, on=["hair"])
    CF, CB = srgb(FACE), srgb(BARK_D)
    def frown(u, v):
        c = np.tile(CF, (len(u), 1))
        c[(np.abs(v - (-0.45 * u * u + 0.2)) < 0.3) & (np.abs(u) < 0.95)] = CB
        return c
    k.decal(HC + (0, -0.192, -0.115), unit(0, -1, -0.15), (0.05, 0.014), frown, eps=0.003, rings=10, segs=48)

    # ---- the crown: wide and dark, over the back and shoulders, boughs up into it, none of it before the face
    for j, (x, y) in enumerate(((0.26, 0.12), (-0.26, 0.12), (0.0, 0.2))):
        k.add(f"bough{j}", tube([(x * 0.6, y * 0.6, 1.22), (x, y, 1.36), (x * 1.6, y * 1.3, 1.48)],
                                [0.075, 0.06, 0.045]), BARK, "crown")
    # a domed canopy of rounded clumps behind the head and over the shoulders, its front edge behind the brow
    SJ.canopy(k, CANOPY_AT, CANOPY_CLUMPS, "crown", 24, 0.26, 0.1, 0.02, [OAK, OAK_D], OAK_CORE, seed=9,
              keep=lambda p: p[1] > 0.0 or abs(p[0]) > 0.3)

    body = k.build(voxel=VOXEL, smooth=4, band=0.012, wband=0.08)
    G.board(body)

    # ---- the eyes: big amber, their top third cut by the dark brow sloping toward the nose
    core = G.dot_eye(iris=AMBER, ink=AMBER_INK, hi_at=(0.28, -0.08), hi_r=0.24, ring=0.3)
    def brow_look(state):
        inner = eye_look(state, core, CF, CB, line=0.17)
        def look(u, v):
            c = inner(u, v)
            edge = 0.2 + 0.45 * u
            c[v > edge] = CB
            c[v > edge + 0.42] = CF
            return c
        return look
    def lay(state):
        for s in (1, -1):
            n = unit(0.42 * s, -0.9, 0.13)
            p = EYE * np.array([s, 1, 1])
            look = brow_look(state)
            col = look if s > 0 else (lambda u, v, look=look: look(-u, v))
            k.decal(p, n, EYE_HALF, col, eps=0.003, rings=24, segs=56)
    k.eye_states(lay)
    k.export(out_path)


if __name__ == "__main__":
    stejaron(*args())
