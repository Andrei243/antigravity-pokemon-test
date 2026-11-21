# Rolycoly sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/rolycoly.py -- <the export's Rolycoly.glb> <out.glb>
#
# The design, from the references: a lump of black coal, longer than it is tall, cut in flat facets and studded all
# over with smaller blocks, the ones on top lighter grey flat plates. At its front a block like an octagonal prism
# lying along the body, its nose a little down: on that nose the one eye, a pointed orange hexagon with a dark red rim
# and a white upright slit. On each side two round faceted knobs low on the flank. No wheel shows in the pictures.
#
# Written to be grown from (carkol.py and coalossal.py import it with sibling): the line's colours, a lump of coal
# (lump), a heap of coals over a glowing core (heap), the loose pieces gathered by bone and colour (Lot) and the
# coal-eye's look are here; the build is under the main guard. The stone itself is roggenrola.py's (planes cut a box).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, hull, slab, srgb, unit, eye_look, polygon, args, sibling, axes

rg = sibling("roggenrola")

# --- the line's colours -------------------------------------------------------------------------------------------
COAL, COAL_DK, COAL_LT, FLECK = "#403E44", "#2E2C32", "#4C4A52", "#6C6A72"     # coal, its darker and lighter blocks
FACE = "#38363C"                                                                # Rolycoly's nose block
EYE_FILL, EYE_RIM, EYE_PUPIL = "#F7801C", "#A8241A", "#F6F2EA"
SHUT_INK = "#E8601E"                                                            # a closed eye's line on dark coal
LAVA, LAVA_GLOW = "#D4301C", "#FF3A1C"                                          # the core showing between coals
STONE, STONE_LT, STONE_DK = "#68666C", "#7A787E", "#545258"                     # Carkol's cart


def rot_x(deg):
    a = math.radians(deg)
    return np.array([[1, 0, 0], [0, math.cos(a), -math.sin(a)], [0, math.sin(a), math.cos(a)]])


def rand_turn(rs):
    return rg.turned(rs.normal(size=3), roll=rs.rand() * 360)


class Lot:
    """Loose pieces gathered by bone, colour and light, each lot added as one piece (a glowing piece is a material
    and a primitive of its own; a hundred coals are one piece a colour)."""
    def __init__(self):
        self.lots = {}

    def put(self, shape, colour, bone, glow=None):
        self.lots.setdefault((bone, colour, glow or ""), []).append(shape)

    def flush(self, k, name):
        for i, ((bone, colour, glow), shapes) in enumerate(sorted(self.lots.items())):
            V, F, off = [], [], 0
            for sv, sf in shapes:
                V.append(sv); F.extend([[j + off for j in f] for f in sf]); off += len(sv)
            k.add(f"{name}{i}", (np.vstack(V), F), colour, bone, fuse=False, surface="shell", glow=glow or None)
        self.lots = {}


def lump(c, half, seed, rs=None):
    """A block of coal: a jittered box turned at random (flat faces, hard edges)."""
    rs = rs or np.random.RandomState(seed)
    return rg.chunk(c, half, seed, n=2, rot=rand_turn(rs))


def knob(c, r, seed):
    """A round faceted boulder (a stud on the coal)."""
    rs = np.random.RandomState(seed)
    pl = rg.ball_planes(seed, jitter=0.04, turn=tuple(rs.rand(3) * 90))
    return rg.stone(c, (r, r * (0.9 + 0.2 * rs.rand()), r * 0.92), pl)


def plate(c, n, r, thick, turn):
    """A flat hexagonal plate lying on a surface whose normal is n."""
    a, b = axes(n)
    pts = []
    for i in range(6):
        th = math.radians(turn + 60 * i)
        e = a * math.cos(th) + b * math.sin(th)
        pts.append(c + e * r + n * thick * 0.5)
        pts.append(c + e * r * 1.05 - n * thick * 1.5)
    return hull(pts)


def heap(k, lot, base, base_r, apex, n, size, seed, bone, coals=(COAL_DK, COAL, COAL_DK, COAL_LT), fat=0.8,
         shrink=0.5, rim=1.0, core=LAVA, glow=LAVA_GLOW, clear=None):
    """A heap of coal blocks over a core that glows red in the cracks between them: level rings from base (a disc of
    base_r) up to apex, the radius (1 - t) ** fat; n blocks of half size size spread by area over it, smaller near the
    top. The core is a loose glowing hull a little inside the blocks."""
    base, apex = np.array(base, float), np.array(apex, float)
    rs = np.random.RandomState(seed)
    def radius(t):
        return base_r * (1 - t) ** fat
    pts = []
    for t in (0.0, 0.2, 0.42, 0.62, 0.8, 0.92):
        r = radius(t) * 0.9 * (rim if t == 0 else 1)
        for i in range(12):
            th = 2 * math.pi * i / 12 + 1.7 * t
            pts.append(base * (1 - t) + apex * t + r * np.array([math.cos(th), math.sin(th), 0]))
    pts.append(base * 0.03 + apex * 0.97)
    lot.put(hull(pts), core, bone, glow)
    golden = math.pi * (3 - 5 ** 0.5)
    L = np.linalg.norm(apex - base)
    for i in range(n):
        q = (i + 0.5) / n
        t = 1 - math.sqrt(1 - q * 0.97)                 # even by area: more blocks round the broad base
        th = i * golden + rs.rand() * 0.4
        radial = np.array([math.cos(th), math.sin(th), 0])
        if clear is not None:
            # (direction, degrees, up to t): a sector left bare (where a head comes out of the heap)
            cd, deg, tmax = clear
            if t < tmax and radial @ unit(*cd) > math.cos(math.radians(deg)):
                continue
        nrm = unit(*(radial + np.array([0, 0, base_r * fat / L * 1.2])))
        s = size * (1 - shrink * t) * (0.8 + 0.4 * rs.rand())
        c = base * (1 - t) + apex * t + radial * radius(t) + nrm * s * 0.22
        half = (s * (0.85 + 0.3 * rs.rand()), s * (0.85 + 0.3 * rs.rand()), s * (0.8 + 0.3 * rs.rand()))
        lot.put(lump(c, half, seed * 1000 + i, rs), coals[i % len(coals)], bone)
    lot.put(lump(apex - np.array([0, 0, size * 0.7]), (size * 0.62,) * 3, seed * 1000 + n, rs), coals[0], bone)


# --- the coal eye -------------------------------------------------------------------------------------------------
EYE_PTS = [(1.0, 0.0), (0.52, 1.0), (-0.52, 1.0), (-1.0, 0.0), (-0.52, -1.0), (0.52, -1.0)]
EYE_RIM_AT = 0.76


def eye_open(u, v, pupil=(0.18, 0.0, 0.13, 0.5)):
    """The orange hexagon: a dark red rim, an orange fill, a white upright slit."""
    d = np.maximum(np.abs(v), (np.abs(u) * 2 + np.abs(v)) / 2)
    c = np.tile(srgb(EYE_FILL), (len(u), 1))
    pu, pv, a, b = pupil
    c[((u - pu) / a) ** 2 + ((v - pv) / b) ** 2 < 1] = srgb(EYE_PUPIL)
    c[d > EYE_RIM_AT] = srgb(EYE_RIM)
    return c


def coal_eye(state, lid=FACE, ink=SHUT_INK, look=eye_open):
    if state == "Open":
        return look
    inner = eye_look(state, look, srgb(lid), srgb(ink), line=0.16)
    return inner


# --- Rolycoly -------------------------------------------------------------------------------------------------------
BODY_C, BODY_R = np.array([0.0, 0.035, 0.13]), (0.142, 0.158, 0.128)
NOSE_P = np.array([0.0, -0.1, 0.122])          # the nose block's pivot
NOSE_TILT = 11.0                               # its nose down, degrees
NOSE_R = 0.072
NOSE_LEN = 0.118                               # its front's distance before the pivot
EYE_HALF = (0.056, 0.036)


def rolycoly(src, out):
    k = Kit(src, coat="shell")
    lot = Lot()
    # the body: a faceted stone with a flat underside on the ground
    planes = rg.ball_planes(8, pent=1.0, hexa=0.975, jitter=0.04, turn=(12, 31, 7))
    planes.append(((0, 0, -1.0), 0.96))
    lot.put(rg.stone(BODY_C, BODY_R, planes), COAL, "body")
    # blocks studding it all over but its front and underside, the ones on top lighter flat plates
    rs = np.random.RandomState(21)
    n = 30
    golden = math.pi * (3 - 5 ** 0.5)
    for i in range(n):
        z = 1 - 1.55 * (i + 0.5) / n
        ring = math.sqrt(max(0.0, 1 - z * z))
        a = i * golden
        d = np.array([math.cos(a) * ring, math.sin(a) * ring, z])
        if d[1] < -0.55 and z < 0.5:
            continue                            # the nose block's place
        p = BODY_C + d * np.array(BODY_R) * 0.97
        s = 0.03 + 0.016 * rs.rand()
        if z > 0.55:
            # a flat plate on top, lighter, lying on the surface
            lot.put(plate(p, unit(*(d / np.array(BODY_R))), s * 1.2, 0.012, rs.rand() * 60),
                    FLECK if i % 2 else COAL_LT, "body")
        else:
            lot.put(knob(p, s, 300 + i), COAL_DK if i % 3 else COAL, "body")
    # two round faceted knobs low on each flank
    for s_ in (1, -1):
        for j, (y, z, r) in enumerate(((-0.02, 0.1, 0.046), (0.085, 0.104, 0.043))):
            c = np.array([s_ * (0.132 + 0.004 * j), y, z])
            pl = rg.ball_planes(40 + j + (s_ > 0) * 3, jitter=0.03, turn=(20 * j, 13, 40))
            lot.put(rg.stone(c, (r * 0.85, r, r), pl), COAL_DK, "body")
    # the nose block: an octagonal prism along the body, its front chamfered, the nose a little down
    R = rot_x(NOSE_TILT)
    pts = []
    for y, r in ((0.05, NOSE_R), (-NOSE_LEN + 0.02, NOSE_R), (-NOSE_LEN, NOSE_R * 0.82)):
        for i in range(8):
            th = math.radians(22.5 + 45 * i)
            pts.append(NOSE_P + R @ np.array([r * math.cos(th), y, r * math.sin(th)]))
    lot.put(hull(pts), FACE, "body")
    n_front = R @ np.array([0, -1.0, 0])
    up = R @ np.array([0, 0, 1.0])
    front = NOSE_P + R @ np.array([0, -NOSE_LEN, 0])
    lot.flush(k, "coal")
    # a thin fused plate on the nose's front for the eye's patch, as big as the front facet
    octo = [(NOSE_R * 0.8 * math.cos(math.radians(22.5 + 45 * i)), NOSE_R * 0.8 * math.sin(math.radians(22.5 + 45 * i)))
            for i in range(8)]
    k.add("plate", slab(octo, front - n_front * 0.0035, (1, 0, 0), up, 0.01), FACE, "body", surface="shell")
    eye_c = front + n_front * 0.0015
    k.build(voxel=0.0028, smooth=2)
    def lay(state):
        k.decal(eye_c, n_front, EYE_HALF, coal_eye(state), up=up, eps=0.0025, rings=20, segs=56,
                outline=polygon(EYE_PTS))
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    src, out = args()
    rolycoly(src, out)
