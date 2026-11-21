# Roggenrola sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/roggenrola.py -- <the export's Roggenrola.glb> <out.glb>
#
# The design, from the references: a ball of dark blue stone cut in large flat facets, as wide as it is tall, that is
# all of the body. On its front, a little under the middle, the species' signature: a hexagonal socket standing on its
# point, a dark rim round a bevel of yellow facets (the upper ones in shadow, the lower ones lit) and a dark hexagon at
# its heart (its ear; it has no eyes, so this is what the game's five eye states are drawn on, the socket's own yellow
# as the lid). On the top a brown stone, narrow at its root and broad at its top, leaning a little forward. Under the
# ball two brown stones for feet, lying slanted, their inner ends up under the ball and their outer ends out to the
# sides and on the ground.
#
# Written to be grown from (boldore.py and gigalith.py import it with sibling): the line's colours, the faceted stone
# cut by planes (stone), the crystals (crystal, Glints), the hexagonal socket's plate and its look (hex_plate, hex_eye)
# are here; the build is under the main guard.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import bmesh
import numpy as np
from sculptkit import Kit, srgb, unit, eye_look, args, hull, slab, axes, polygon

# --- the line's colours -------------------------------------------------------------------------------------------
NAVY, NAVYDK = "#283B74", "#1D2A57"                 # the stone, its underside
BROWN, BROWNDK = "#A87A50", "#8A6040"               # Roggenrola's crest and feet
YEL_LIT, YEL_MID, YEL_DIM = "#F7CF4A", "#E6AE30", "#BC7E1E"     # the socket's bevel, lit to shadowed
CORE, INK = "#1B1D27", "#13151C"                    # the socket's heart, its rim
ORANGE, REDBROWN = "#F0704E", "#A9483A"             # Boldore's crystals
RED, REDDK, GREY = "#E33A40", "#B0262E", "#A6A49E"  # Gigalith's crystals, its joints
IRIS = "#D8302E"                                    # Gigalith's eye
GLOW = True                                         # crystals give their own light (off to judge their colour)


# --- faceted stone, cut by planes -----------------------------------------------------------------------------------
PHI = (1 + 5 ** 0.5) / 2
ICOSA = [(0, a, b * PHI) for a in (1, -1) for b in (1, -1)] + [(a, b * PHI, 0) for a in (1, -1) for b in (1, -1)] + \
        [(b * PHI, 0, a) for a in (1, -1) for b in (1, -1)]
DODECA = [(a, b, c) for a in (1, -1) for b in (1, -1) for c in (1, -1)] + \
         [(0, a / PHI, b * PHI) for a in (1, -1) for b in (1, -1)] + \
         [(a / PHI, b * PHI, 0) for a in (1, -1) for b in (1, -1)] + \
         [(b * PHI, 0, a / PHI) for a in (1, -1) for b in (1, -1)]


def ball_planes(seed, pent=1.0, hexa=1.0, jitter=0.035, turn=(0, 0, 0), keep_off=None):
    """A ball's cutting planes in unit space, (normal, depth): the icosahedron's twelve (pentagons) and the
    dodecahedron's twenty (hexagons), as a soccer ball's, turned and their depths jittered so it reads as rock.
    keep_off: (normal, degrees): planes within that angle of a facet of one's own are left out."""
    rs = np.random.RandomState(seed)
    from mathutils import Euler
    m = np.array(Euler([math.radians(a) for a in turn]).to_matrix())
    out = []
    for group, d in ((ICOSA, pent), (DODECA, hexa)):
        for n in group:
            n = m @ unit(*n)
            if keep_off is not None and np.degrees(np.arccos(np.clip(n @ unit(*keep_off[0]), -1, 1))) < keep_off[1]:
                continue
            out.append((n, d + jitter * (rs.rand() * 2 - 1)))
    return out


def stone(c, radii, planes):
    """A stone of flat facets: a box cut by each plane (unit-space normal and depth), then scaled to radii about c.
    Returns the hull (flat faces, hard edges: a loose piece)."""
    import itertools
    N = np.array([n for n, _ in planes], float)
    D = np.array([d for _, d in planes], float)
    P = []
    for t in itertools.combinations(range(len(N)), 3):      # each corner is where three planes meet
        M = N[list(t)]
        if abs(np.linalg.det(M)) < 1e-6:
            continue
        x = np.linalg.solve(M, D[list(t)])
        if (N @ x <= D + 1e-9).all():
            P.append(x)
    return hull(np.array(P) * np.array(radii, float) + np.array(c, float))


def plane_world(c, radii, n, d):
    """Where a unit-space plane lies on the scaled stone: a point at its middle and its outward normal."""
    n = unit(*n)
    return np.array(c, float) + n * d * np.array(radii, float), unit(*(n / np.array(radii, float)))


def chunk(c, half, seed, n=10, squash_bottom=None, rot=None):
    """An irregular block of stone (a foot, a lump): points jittered round a box, their hull. half: (x, y, z) half
    sizes; rot: a 3x3 turn."""
    rs = np.random.RandomState(seed)
    P = []
    for sx in (1, -1):
        for sy in (1, -1):
            for sz in (1, -1):
                q = np.array([sx, sy, sz]) * (0.72 + 0.28 * rs.rand(3))
                P.append(q)
    for _ in range(n):
        q = rs.normal(size=3); q /= np.linalg.norm(q)
        P.append(q * (0.95 + 0.1 * rs.rand()))
    P = np.array(P) * np.array(half, float)
    if rot is not None:
        P = P @ np.array(rot).T
    P = P + np.array(c, float)
    if squash_bottom is not None:
        P[:, 2] = np.maximum(P[:, 2], squash_bottom)
    return hull(P)


def turned(d, roll=0.0):
    """A turn taking +X along d (a 3x3 matrix whose first column is d), rolled about d by degrees."""
    d = unit(*d)
    a, b = axes(d)
    r = math.radians(roll)
    a, b = a * math.cos(r) + b * math.sin(r), -a * math.sin(r) + b * math.cos(r)
    return np.stack([d, a, b], axis=1)


# --- crystals ---------------------------------------------------------------------------------------------------
def crystal(base, direction, length, r, sides=6, tip=0.32, flat=1.0, turn=0.0, up=None):
    """A crystal: a prism of sides faces from base along direction, its end cut to a point (tip of the length),
    flattened across by flat (1 round). Its root starts a radius inside whatever it stands in. A loose hull."""
    d = unit(*direction)
    if up is None:
        a, b = axes(d)
    else:
        b = np.array(up, float); b = b - d * (b @ d); b /= np.linalg.norm(b)
        a = np.cross(b, d)
    base = np.array(base, float)
    P = [base + d * length]
    for i in range(sides):
        th = 2 * math.pi * i / sides + math.radians(turn)
        off = r * (math.cos(th) * a * flat + math.sin(th) * b)
        P.append(base - d * r + off)
        P.append(base + d * length * (1 - tip) + off)
    return hull(P)


class Glints:
    """Crystals gathered by bone and colour, each lot added as one loose piece (a piece with glow= is a material
    and a primitive of its own, so they are joined as ponyta.py's flames are)."""
    def __init__(self):
        self.lots = {}

    def put(self, shape, color, bone):
        self.lots.setdefault((bone, color), []).append(shape)

    def flush(self, k, name="crystal"):
        for i, ((bone, color), shapes) in enumerate(sorted(self.lots.items())):
            V, F, off = [], [], 0
            for sv, sf in shapes:
                V.append(sv); F.extend([[j + off for j in f] for f in sf]); off += len(sv)
            k.add(f"{name}{i}", (np.vstack(V), F), color, bone, fuse=False, surface="shell",
                  glow=color if GLOW else None)
        self.lots = {}


# --- the hexagonal socket -----------------------------------------------------------------------------------------
def hex_pts(turn):
    """A hexagon's corners at radius 1, the first at turn degrees (90: standing on its point)."""
    return [(math.cos(math.radians(turn + 60 * i)), math.sin(math.radians(turn + 60 * i))) for i in range(6)]


def hex_plate(k, name, centre, normal, half, bone, turn=90.0, up=(0, 0, 1), thick=0.01, grow=1.06):
    """A thin fused plate in the hexagon's shape standing just proud of a facet, for the socket's patch to lie on.
    Returns the patch's centre (on the plate's front)."""
    n = unit(*normal)
    u = np.array(up, float); u = u - n * (u @ n); u /= np.linalg.norm(u)
    ex = np.cross(u, n)
    out = np.array(hex_pts(turn)) * np.array(half) * grow
    k.add(name, slab(out, np.array(centre, float) - n * thick * 0.25, ex, u, thick), CORE, bone)
    return np.array(centre, float) + n * thick * 0.25


def hexd(u, v, turn):
    """How far a point is toward the hexagon's edge (1 on it) and which of the six edges it is nearest."""
    a = np.radians(turn + 30 + 60 * np.arange(6))
    proj = (u[:, None] * np.cos(a)[None] + v[:, None] * np.sin(a)[None]) / math.cos(math.radians(30))
    return proj.max(1), np.sin(a)[proj.argmax(1)]


CY = [srgb(c) for c in (YEL_LIT, YEL_MID, YEL_DIM)]
CC, CK = srgb(CORE), srgb(INK)
RIM = 0.825                # the dark rim, a sixth of the socket's half size (between the patch's rings)


def socket_look(turn, core=0.425, iris=None):
    """The socket open: a dark rim, a bevel of yellow facets shaded by which way each faces (lit below, in shadow
    above, as a hollow is), a dark hexagonal heart; or with iris, a round red iris and a dark pupil in its middle."""
    def look(u, v):
        d, up = hexd(u, v, turn)
        t = np.clip((up + 1) / 2, 0, 1)[:, None]          # 0 for a facet facing down (lit), 1 facing up
        lo = np.where(t < 0.5, CY[0] + (CY[1] - CY[0]) * (t / 0.5), CY[1] + (CY[2] - CY[1]) * ((t - 0.5) / 0.5))
        c = lo.copy()
        if iris is None:
            c[d < core] = CC
        else:
            r2 = u * u + v * v
            c[r2 < iris[0] ** 2] = srgb(IRIS)
            c[r2 < iris[1] ** 2] = CC
            g = ((u - iris[0] * 0.3) ** 2 + (v - iris[0] * 0.38) ** 2) < (iris[0] * 0.28) ** 2
            c[g] = srgb("#FFD8C8")
        c[d > RIM] = CK
        return c
    return look


def hex_eye(state, turn, core=0.425, iris=None, lid=YEL_MID):
    """The socket in one of the game's five states: the socket's own yellow is the lid, its rim kept in all."""
    open_look = socket_look(turn, core, iris)
    if state == "Open":
        return open_look
    inner = eye_look(state, open_look, srgb(lid), CK, line=0.16)
    def look(u, v):
        c = inner(u, v)
        d, _ = hexd(u, v, turn)
        c[d > RIM] = CK
        return c
    return look


def lay_socket(k, state, centre, normal, half, turn=90.0, up=(0, 0, 1), core=0.425, iris=None, mirror=False):
    k.decal(centre, normal, half, hex_eye(state, turn, core, iris), up=up, eps=0.003, rings=20, segs=56,
            outline=polygon(hex_pts(turn)), mirror=mirror)


# --- Roggenrola ---------------------------------------------------------------------------------------------------
BALL_C, BALL_R = np.array([0.0, 0.0, 0.162]), (0.133, 0.126, 0.131)
FRONT_N, FRONT_D = unit(0, -1, -0.16), 0.8          # the socket's facet, in the ball's unit space
SOCKET_HALF = (0.058, 0.058)


def roggenrola(src, out):
    k = Kit(src, coat="shell")
    # the ball: a soccer ball's facets turned and jittered into rock, with a facet of its own for the socket
    planes = ball_planes(5, pent=1.0, hexa=0.985, jitter=0.03, turn=(14, 9, 23), keep_off=(FRONT_N, 36))
    planes.append((FRONT_N, FRONT_D))
    k.add("ball", stone(BALL_C, BALL_R, planes), NAVY, "body", fuse=False, surface="shell")
    sc, sn = plane_world(BALL_C, BALL_R, FRONT_N, FRONT_D)
    front = hex_plate(k, "plate", sc, sn, SOCKET_HALF, "body", turn=90)
    # the crest: a brown stone, narrow at its root inside the ball's top, broad at its top, leaning a little forward
    rs = np.random.RandomState(11)
    pts = []
    for z, hx, hy, dy, n in ((0.262, 0.024, 0.022, 0.0, 6), (0.315, 0.033, 0.027, -0.006, 6),
                             (0.368, 0.05, 0.034, -0.014, 6), (0.394, 0.046, 0.031, -0.018, 5)):
        for i in range(n):
            th = 2 * math.pi * i / n + 0.4 * z * 10
            j = 0.85 + 0.3 * rs.rand()
            pts.append((hx * j * math.cos(th), dy + hy * j * math.sin(th), z))
    pts += [(-0.016, -0.026, 0.418), (0.024, -0.014, 0.412), (0.004, 0.004, 0.404)]
    k.add("crest", hull(pts), BROWN, "head", fuse=False, surface="shell")
    # the feet: two brown stones lying slanted, the inner end up under the ball, the outer end out on the ground
    for s, side in ((1, "R"), (-1, "L")):
        rot = turned((s * 0.92, -0.25, -0.3), roll=-20 * s)
        k.add("foot" + side, chunk((s * 0.078, -0.012, 0.034), (0.058, 0.03, 0.026), 30 + (s > 0), rot=rot,
                                   squash_bottom=0.0), BROWN, "leg" + side, fuse=False,
              surface="shell")
    k.build(voxel=0.0028, smooth=2)
    k.eye_states(lambda state: lay_socket(k, state, front, sn, SOCKET_HALF, turn=90))
    k.export(out)


if __name__ == "__main__":
    src, out = args()
    roggenrola(src, out)
