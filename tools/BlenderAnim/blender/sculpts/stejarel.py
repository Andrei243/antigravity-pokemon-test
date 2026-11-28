# Stejarel (No. 10097, Carpathia) sculpted from nothing on the skeleton and clips of the game's own export of it:
#   blender --background --factory-startup --python sculpts/stejarel.py -- <the export's Stejarel.glb> <out.glb>
#
# Ghindel grown (ghindel.py, sibling("ghindel")): its palette, its rough acorn cap, its leaves, its small acorn and its
# dot eye. From its picture (10097-Stejarel.jpg) and its brief (CP-dex-2b-mountains-more.md, 97), largest first: a crown
# of big lobed oak leaves some 0.46 of the height and 0.7 of it across, two greens, its leaves laid like shingles and
# their tips lifting clear; a ridged trunk that is the body and the head, golden bark with dark grain, widening into a
# fan of roots for feet; a band of rough acorn cap round its top (where the trunk forks into the crown's branches) like
# a beanie over a paler bark face whose lower edge comes to a point; a glowering face (dark eyes cut by brows sloping
# toward the nose, a set little smirk); two branch arms, carried forward and a little down here where the picture
# raises them, twig fingers with small leaves; one acorn hanging from the +X arm.
# Skeleton: body (0, 0, 0.325), crown (0, 0, 0.605), arms (+-0.06, 0, 0.365), legs (+-0.05, 0, 0.125).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, outward, srgb, inside, smoothstep, unit, eye_look, polygon, args, sibling

G = sibling("ghindel")

BARK, BARK_D, FACE = "#C08A48", "#86592C", "#E6BC72"
OAK, OAK_D, OAK_CORE = "#5DAA3C", "#3E8A2C", "#367A28"
BROW = "#4A2E18"

# --- Stejarel's frame -------------------------------------------------------------------------------------------
TRUNK = [(0, 0.004, 0.135), (0, 0.002, 0.19), (0, 0.0, 0.27), (0, 0.0, 0.38), (0, 0.0, 0.47)]
TRUNK_R = [0.112, 0.096, 0.088, 0.092, 0.094]
CROWN = np.array([0.0, 0.03, 0.625])
CROWN_R = np.array([0.15, 0.135, 0.085])
# the canopy: rounded clumps of uneven size about its middle, the highest in the middle, rounded underneath
CROWN_AT = np.array([0.0, 0.02, 0.68])
CROWN_CLUMPS = [(0.0, 0.01, 0.085, 0.115), (-0.12, -0.07, 0.02, 0.1), (0.13, -0.06, 0.0, 0.105),
                (-0.11, 0.09, 0.03, 0.11), (0.1, 0.1, 0.05, 0.1), (-0.175, 0.01, -0.03, 0.085),
                (0.18, 0.0, -0.02, 0.08), (0.0, -0.11, -0.01, 0.09)]
EYE_X, EYE_Z = 0.034, 0.388
EYE_HALF = (0.021, 0.024)
VOXEL = 0.0062


def ang(P):
    """The angle round the upright axis, nought at the front (-Y), positive toward +X."""
    return np.arctan2(P[:, 0], -P[:, 1])


def oak_crown(k, centre, radii, bone, rings, length, width, thick, colours, seed, core=OAK_CORE, name="oak", lift=0.5, keep=None):
    """A crown of lobed oak leaves over a fused green core: rings of (count, elevation in degrees, length share);
    each leaf from the core's surface outward and up, its face turned up and out like a shingle, tips lifting clear."""
    centre, radii = np.array(centre, float), np.array(radii, float)
    k.add(name + "core", ell(centre, radii, deform=G.rough(seed, n=40, amp=0.12, s=0.4, under=0.7)), core, bone,
          surface="leaf")
    rs = np.random.RandomState(seed)
    j = 0
    for count, el, share in rings:
        for i in range(count):
            az = 2 * math.pi * (i + 0.5 * (j % 2) + 0.25 * rs.rand()) / count
            e = math.radians(el + 8 * (rs.rand() - 0.5))
            out = np.array([math.cos(az) * math.cos(e), math.sin(az) * math.cos(e), math.sin(e)])
            base = centre + out * radii * 0.72
            d = unit(*(out + np.array([0, 0, lift])))
            across = np.array([-math.sin(az), math.cos(az), 0.0])
            L = length * share * (0.88 + 0.24 * rs.rand())
            if keep is not None and not keep(az, el):
                j += 1
                continue
            k.add(f"{name}{j}", G.flat_leaf(base, d, across, L, width * share, thick, lobes=3),
                  colours[j % len(colours)], bone, fuse=False, surface="leaf")
            j += 1


def canopy(k, centre, clumps, bone, per, length, width, thick, colours, core, seed, name="canopy", keep=None):
    """A tree's canopy: fused rounded clumps of uneven size ((dx, dy, dz, radius) about centre), higher in the middle
    and rounded underneath too, and lobed leaves laid along its surface, overlapping like shingles down its slopes,
    tips lifting a little, some hanging over the rim. per: leaves a clump for a radius of 1 (scaled by its area)."""
    centre = np.array(centre, float)
    C = [centre + np.array(c[:3], float) for c in clumps]
    R = [float(c[3]) for c in clumps]
    for i, (c, r) in enumerate(zip(C, R)):
        k.add(f"{name}clump{i}", ell(c, (r, r, r * 0.88), deform=G.rough(seed + i, n=30, amp=0.1, s=0.42, under=1.0)),
              core, bone, surface="leaf")
    rs = np.random.RandomState(seed)
    up = np.array([0, 0, 1.0])
    j = 0
    rmax = max(R)
    for i, (c, r) in enumerate(zip(C, R)):
        n = max(3, int(round(per * (r / rmax) ** 2)))
        for q in range(n):
            # a direction over the clump, most of them upward, a few low on its side to hang over the rim
            az = 2 * math.pi * (q + rs.rand() * 0.6) / n
            el = math.radians(-35 + 115 * (1 - rs.rand() ** 1.6))
            d = np.array([math.cos(az) * math.cos(el), math.sin(az) * math.cos(el), math.sin(el)])
            p = c + d * r * np.array([1, 1, 0.88]) * 0.97
            # leave out points buried in another clump
            if any(np.linalg.norm((p - C[m]) / (R[m] * np.array([1, 1, 0.88]))) < 0.93 for m in range(len(C))
                   if m != i):
                rs.rand(); rs.rand()
                continue
            if keep is not None and not keep(p):
                rs.rand(); rs.rand()
                continue
            down = -(up - d * (up @ d))
            if np.linalg.norm(down) < 0.15:
                t0 = rs.rand() * 2 * math.pi
                down = np.array([math.cos(t0), math.sin(t0), 0.0]); down = down - d * (down @ d)
            else:
                rs.rand()
            down /= np.linalg.norm(down)
            L = length * (0.85 + 0.3 * rs.rand())
            ld = unit(*(down + d * 0.28))                       # along the surface, its tip lifting clear
            across = np.cross(d, down)
            base = p - down * L * 0.35 + d * thick * 0.3
            k.add(f"{name}{j}", sculpt_leaf(base, ld, across, L, width * L / length, thick),
                  colours[j % len(colours)], bone, fuse=False, surface="leaf")
            j += 1


def sculpt_leaf(base, d, across, length, width, thick):
    """A lobed oak leaf as a loose plate (an outline of fewer points than the line's leaf, for a crown of many)."""
    from sculptkit import slab
    d = unit(*d)
    a = np.array(across, float); a = a - d * (a @ d); a /= np.linalg.norm(a)
    return slab(G.leaf_outline(length, width, 3, n=18), base, a, d, thick)


def stejarel(src, out_path):
    k = Kit(src, coat="leaf")

    # ---- the trunk, flaring at its foot, its grain dark
    k.add("trunk", tube(TRUNK, TRUNK_R), BARK, "body", surface="leather")
    k.add("foot", ell((0, 0.004, 0.085), (0.118, 0.11, 0.07)), BARK, "root", surface="leather")
    def grain(P):
        th = ang(P); r = np.hypot(P[:, 0], P[:, 1])
        lines = np.zeros(len(P))
        for a in np.linspace(-math.pi, math.pi, 14, endpoint=False):
            w = 0.006 + 0.003 * math.sin(3 * a)
            d = np.abs((th - a - 0.05 * np.sin(P[:, 2] * 40) + math.pi) % (2 * math.pi) - math.pi) * r
            lines = np.maximum(lines, inside(d - w, 0.0025))
        return lines * inside(P[:, 2] - 0.46) * inside(r - 0.16)
    k.paint(grain, BARK_D, on=["trunk"])
    # the pale face: the trunk's front from under the cap band down to a point
    def face(P):
        th = ang(P); r = np.hypot(P[:, 0], P[:, 1])
        bottom = 0.292 + 0.07 * np.clip(np.abs(th) / 1.0, 0, 1)
        return inside(np.abs(th) - 1.0, 0.01) * inside(bottom - P[:, 2], 0.003) * inside(P[:, 2] - 0.45) \
            * inside(r - 0.13) * (P[:, 1] < 0)
    k.paint(face, FACE, on=["trunk"])

    # ---- the roots: a fan, three a side, one before and one behind
    for s, bone in ((1, "legR"), (-1, "legL")):
        for j, a in enumerate((-40, 20, 80)):
            ar = math.radians(a)
            d = np.array([math.cos(ar) * s, -math.sin(ar) if a < 60 else 0.6, 0])
            d = d / np.linalg.norm(d)
            pts = [np.array([0.05 * s, 0, 0.13]) + d * 0.02, d * 0.12 + (0, 0, 0.085), d * 0.18 + (0, 0, 0.04),
                   d * 0.215 + (0, 0, 0.02)]
            k.add(f"root{s}{j}", tube(pts, [0.05, 0.04, 0.028, 0.02]), BARK, bone, surface="leather")
    for j, d in enumerate((unit(0.05, -1, 0), unit(-0.1, 1, 0))):
        pts = [(0, 0, 0.12), d * 0.1 + (0, 0, 0.07), d * 0.17 + (0, 0, 0.03), d * 0.2 + (0, 0, 0.02)]
        k.add(f"rootc{j}", tube(pts, [0.05, 0.038, 0.026, 0.02]), BARK, "root", surface="leather")

    # ---- the acorn-cap band round the trunk's top, and the branches forking up into the crown
    G.acorn_cap(k, "band", (0, 0.002, 0.472), (0.13, 0.124, 0.066), (0, 0, 0), "body", seed=11, fuse=True,
                flecks=14)
    for j, (x, y) in enumerate(((0.06, 0.0), (-0.06, 0.01), (0.0, 0.05), (0.02, -0.05))):
        k.add(f"bough{j}", tube([(x * 0.3, y * 0.3, 0.48), (x, y, 0.56), (x * 1.6, y * 1.4, 0.62)], [0.032, 0.026, 0.02]),
              BARK, "crown", surface="leather")

    # ---- the crown
    canopy(k, CROWN_AT, CROWN_CLUMPS, "crown", 15, 0.13, 0.05, 0.011, [OAK, OAK_D], OAK_CORE, seed=4)

    # ---- the branch arms: forward and a little down, two twig fingers with small leaves each
    for s in (1, -1):
        side = "R" if s > 0 else "L"
        sh = np.array([0.07 * s, -0.01, 0.352]); el = np.array([0.145 * s, -0.06, 0.33])
        hand = np.array([0.18 * s, -0.125, 0.31])
        k.add("arm" + side, tube([sh, el, hand], [0.027, 0.021, 0.017]), BARK, "arm" + side, surface="leather")
        for j, tip in enumerate((hand + (0.025 * s, -0.035, 0.05), hand + (0.06 * s, -0.01, -0.02))):
            k.add(f"twig{j}{side}", tube([hand, (hand + tip) / 2 + (0, 0, 0.008), tip], [0.016, 0.014, 0.013]), BARK,
                  "arm" + side, surface="leather")
            d = unit(*(tip - hand + np.array([0, 0, 0.03])))
            k.add(f"twigleaf{j}{side}", G.flat_leaf(tip - d * 0.004, d, (0.2, 1.0, 0.3) if j else (1.0, 0.3, 0.0),
                                                     0.055, 0.02, 0.01, lobes=2), OAK, "arm" + side, fuse=False,
                  surface="leaf")
    # the acorn hanging from the +X arm, below the face and clear of it
    hang = np.array([0.158, -0.085, 0.322])
    k.add("acornstalk", outward(tube([hang, hang + (0, 0, -0.022)], [0.008, 0.008])), BARK_D, "armR", fuse=False,
          surface="leather")
    G.acorn(k, "acorn", hang + (0, 0, -0.018), 0.03, "armR")

    # ---- the face's set little smirk (in every state)
    CF, CB = srgb(FACE), srgb(BROW)
    def smirk(u, v):
        c = np.tile(CF, (len(u), 1))
        c[(np.abs(v - (0.25 * u - 0.15 + 0.35 * u * u)) < 0.3) & (np.abs(u) < 0.95)] = CB
        return c
    k.decal((0.004, -0.091, 0.334), (0, -1, 0), (0.024, 0.009), smirk, eps=0.002, rings=10, segs=48)

    body = k.build(voxel=VOXEL, smooth=4, band=0.006, wband=0.04)
    G.board(body)

    # ---- the eyes: the line's dot eye, its top third cut by a brow sloping down toward the nose
    core = G.dot_eye(hi_at=(0.25, -0.05), hi_r=0.28)
    def brow_look(state):
        inner = eye_look(state, core, CF, CB, line=0.17)
        def look(u, v):
            c = inner(u, v)
            edge = 0.22 + 0.42 * u
            c[v > edge] = CB
            c[v > edge + 0.3] = CF
            return c
        return look
    oval = lambda th: 1.0
    def lay(state):
        for s in (1, -1):
            n = unit(0.36 * s, -0.93, 0.02)
            p = np.array([EYE_X * s, -0.0855, EYE_Z])
            look = brow_look(state)
            col = look if s > 0 else (lambda u, v, look=look: look(-u, v))
            k.decal(p, n, EYE_HALF, col, eps=0.0018, rings=24, segs=56)
    k.eye_states(lay)
    k.export(out_path)


if __name__ == "__main__":
    stejarel(*args())
