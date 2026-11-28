# Baxcalibur-Mega-Mmax: Mega Baxcalibur's own sculpt (baxcaliburmega.py), whole and unchanged in shape, with what its
# Mega Dynamax form lays over it added to the Mega's kit just before it is meshed (sculptkit.py):
#   blender --background --factory-startup --python sculpts/baxcaliburmegammax.py -- <the export's Baxcalibur-Mega-Mmax.glb> <out.glb>
#
# From the form's picture (docs/art/reference/gemini/megadynamax/Mega-Dynamax-Baxcalibur.jpg) and the game's sculptor
# (PokemonModels.MegaMax1.cs, Baxcalibur): (1) the largest addition, the blade on the back grown into a great sword of
# glowing ice, longer than the body is tall: the Mega's own blade, frame, plate, bar and spikes keep their shapes and
# turn to glowing ice, and a broader, longer plate of crystal is laid in their plane round them, out to a point; (2)
# fans of ice crystal at the shoulders and a row of shards along the back before the sword's root; (3) the Key Stone's
# emblem glowing cyan on the chest; (4) Max cloud in mounds about the feet, open behind where the tail lies, and puffs
# of crimson cloud drifting by the shoulders.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from mathutils import Euler
import sculptkit
from sculptkit import Kit, ell, lathe, ring_band, slab, srgb, unit, sibling, args

CRIMSON, DEEP, VIOLET, DEEPV, CYAN = "#AC223E", "#701630", "#6C3EA0", "#42246E", "#4EE2EC"
SWORD, SWORD_DK, SWORD_LT = "#7CEAF4", "#34BCD6", "#D2FAFF"
ICE, ICE_LT = "#78ECF4", "#B0E2F0"
DISC, RING = "#DCFAFF", "#4EE2EC"


def crystal(c, d, L, r):
    d = unit(*d)
    return lathe(np.asarray(c, float) - d * r * 0.8, d,
                 [(0, 0), (r * 0.75, 0), (r, r * 0.7), (r, L * 0.7), (0, L)], seg=6)


def cluster(k, name, at, d, n, L, r, bone, spread=0.5, seed=0.0, cols=(ICE, ICE_LT)):
    d = unit(*d)
    a, b = sculptkit.axes(d)
    for i in range(n):
        ang = i * 2.4 + seed
        lean = 0.0 if i == 0 else spread * (0.45 + 0.55 * i / max(n - 1, 1))
        off = (a * math.cos(ang) + b * math.sin(ang))
        dd = unit(*(d + off * lean))
        ln = L * (1.0 if i == 0 else 0.78 - 0.28 * i / max(n - 1, 1))
        col = cols[i % len(cols)]
        k.add(f"{name}_{i}", crystal(np.asarray(at, float) + off * r * 0.7 * (i > 0), dd, ln, r * (1 if i == 0 else 0.8)),
              col, bone, fuse=False, glow=col, surface="shell")


def emblem(k, c, d, r, bone):
    d = unit(*d)
    c = np.asarray(c, float)
    k.add("emblem", lathe(c - d * 0.004, d, [(0, 0), (r, 0), (r, r * 0.25), (0, r * 0.25)]), DISC, bone,
          fuse=False, glow=DISC)
    k.add("emblemring", ring_band(c + d * r * 0.1, d, r * 1.22, r * 0.45, thick=r * 0.38), RING, bone,
          fuse=False, glow=RING)


def hit(c, r, rot, o, d):
    """Where the ray from o along d first meets the ellipsoid (c, r, turned by rot degrees) as ell() makes it."""
    M = np.array(Euler([math.radians(a) for a in rot]).to_matrix())
    o2 = (M.T @ (np.asarray(o, float) - c)) / r
    d2 = (M.T @ np.asarray(d, float)) / r
    A, B, C = d2 @ d2, 2 * o2 @ d2, o2 @ o2 - 1
    t = (-B - math.sqrt(B * B - 4 * A * C)) / (2 * A)
    q = o2 + d2 * t                                   # the point in the unit sphere's frame, and the surface's normal
    n = M @ (q / r)
    return np.asarray(o, float) + np.asarray(d, float) * t, n / np.linalg.norm(n)


def clouds(k, cx, cy, rx, ry, n, size, seed, avoid=(), skip=None, name="cloud", z0=0.0, bone="root",
           cols=(CRIMSON, VIOLET, DEEP, DEEPV), curls=True, phase=0.0):
    """A ring of fused mounds about (cx, cy), each a clump of four round puffs; a mound within `gap` of a foot
    (avoid: (x, y, gap)) is pushed out along its ray until it is clear; skip(a) leaves a mound out."""
    rnd = np.random.default_rng(seed)
    for i in range(n):
        a = phase + 2 * math.pi * (i + 0.5) / n
        if skip is not None and skip(a):
            continue
        col = cols[i % len(cols)]
        r = 1.0 + 0.06 * math.sin(3 * a + seed)
        pts = []
        for j in range(4):
            s = size * (1.0, 0.8, 0.68, 0.52)[j] * (1 + 0.12 * rnd.uniform(-1, 1))
            arc = (0.0, 1.35, -1.25, 0.35)[j] * size
            rad = (0.0, 0.35, 0.25, -0.3)[j] * size
            t = a + arc / max(rx, ry)
            pts.append([cx + (rx * r + rad) * math.cos(t), cy + (ry * r + rad) * math.sin(t),
                        z0 + s * (0.78, 0.85, 0.8, 1.9)[j], s])
        pts = np.array(pts)
        out = np.array([math.cos(a) * rx, math.sin(a) * ry]); out /= np.linalg.norm(out)
        for _ in range(40):
            if not any(np.hypot(p[0] - x, p[1] - y) < p[3] + g for p in pts for (x, y, g) in avoid):
                break
            pts[:, :2] += out * 0.006
        for j, p in enumerate(pts):
            s = p[3]
            k.add(f"{name}{i}_{j}", ell(p[:3], (s, s, s * 0.88)), col if j != 2 else cols[(i + 1) % len(cols)], bone)
        if curls and i % 3 == 0:
            q = pts[0, :3] + np.array([0, 0, size * 1.05])
            k.add(f"{name}curl{i}", ell(q, (size * 0.32, size * 0.32, size * 0.28)), CYAN, bone, fuse=False, glow=CYAN)


def maxed(k):
    """What the Mega Dynamax form lays over its Mega: called with the Mega's kit just before it is meshed."""
    # (1) the sword: the Mega's blade turned to glowing ice (colours only; baxcaliburmega.py's blade() names them)
    for p in k.parts:
        nm = p["name"]
        new = {"blade": SWORD, "blackplate": SWORD_DK, "bar": SWORD_LT, "rootspike": SWORD_LT}.get(nm)
        if new is None and nm.startswith("bspike"):
            new = SWORD_LT
        if new is not None:
            p["color"], p["glow"] = srgb(new), new
    # and a broader, longer plate of crystal in the blade's plane round it (baxcaliburmega.py's frame: the plane yawed
    # 35 degrees out to +X, the blade rising 57 degrees up and back from (0, 0.03, 0.64), 0.68 long), thinner than the
    # Mega's frame (0.036) so the frame, plate and bar show through both its faces
    ex = unit(math.sin(math.radians(35)), math.cos(math.radians(35)), 0)
    ez = np.array([0, 0, 1.0])
    root = np.array([0.0, 0.03, 0.64])
    up = math.radians(57)
    dirv, perp = np.array([math.cos(up), math.sin(up)]), np.array([-math.sin(up), math.cos(up)])
    L = 0.68
    def ab(u, w):
        p = dirv * (u * L) + perp * w
        return (p[0], p[1])
    OUT = [ab(0.25, 0.19), ab(0.7, 0.23), ab(1.05, 0.24), ab(1.22, 0.16), ab(1.42, 0.0), ab(1.22, -0.15),
           ab(1.05, -0.22), ab(0.7, -0.2), ab(0.25, -0.17), ab(0.12, 0.0)]
    k.add("sword", slab(OUT, root, ex, ez, 0.026), SWORD, "body", fuse=False, glow=SWORD, surface="shell")
    # a pale ridge of crystal down the sword's middle, out past the Mega's blade to the point
    RIDGE = [ab(0.95, 0.03), ab(1.36, 0.0), ab(0.95, -0.03)]
    k.add("swordridge", slab(RIDGE, root, ex, ez, 0.04), SWORD_LT, "body", fuse=False, glow=SWORD_LT,
          surface="shell")
    # (2) fans of ice crystal at the shoulders, over the Mega's mantle ((0, -0.04, 0.57), radii (0.185, 0.25, 0.1))
    for s, sd in ((1, "R"), (-1, "L")):
        cluster(k, f"shoulder{sd}", (0.15 * s, -0.12, 0.63), (0.55 * s, 0.25, 1.0), 4, 0.21, 0.034, "body",
                spread=0.55, seed=0.4 * s)
    # a row of shards along the back before the sword's root
    for i, (y, z, ln) in enumerate(((-0.12, 0.66, 0.12), (-0.05, 0.69, 0.15), (0.1, 0.66, 0.13), (0.17, 0.61, 0.11))):
        cluster(k, f"dorsal{i}", (0.03 if i % 2 else -0.03, y, z), (0.25 if i % 2 else -0.25, 0.35, 1.0), 2, ln,
                0.026, "body", spread=0.5, seed=i)
    # (3) the emblem on the chest (baxcaliburmega.py's chest: (0, -0.18, 0.52), radii (0.17, 0.2, 0.15), turned -32)
    p, n = hit(np.array([0, -0.18, 0.52]), np.array([0.17, 0.2, 0.15]), (-32, 0, 0), (0, -1.0, 0.38), (0, 1.0, 0))
    n = unit(*(n + np.array([0, -0.6, 0])))           # between the chest's normal and straight ahead
    emblem(k, p + n * 0.012, n, 0.058, "body")
    # (4) the clouds about the feet, open behind where the tail lies on the ground (feet (+-0.165, -0.07), toes to
    # -0.24), and crimson puffs drifting by the shoulders
    feet = []
    for s in (1, -1):
        feet += [(0.165 * s, -0.07, 0.12), (0.165 * s, -0.2, 0.07), (0.17 * s, 0.05, 0.1)]
    clouds(k, 0.0, -0.06, 0.36, 0.33, 9, 0.075, 8, avoid=feet,
           skip=lambda a: abs(math.atan2(math.sin(a - math.pi / 2), math.cos(a - math.pi / 2))) < 0.45)
    for s in (1, -1):
        c = np.array([0.33 * s, -0.12, 0.9])
        for j, (o, sz) in enumerate((((0, 0, 0), 0.056), ((0.05 * s, 0.04, 0.03), 0.045), ((-0.045 * s, 0.05, -0.02), 0.04),
                                     ((0.085 * s, 0.08, 0.05), 0.03))):
            k.add(f"wisp{'R' if s > 0 else 'L'}{j}", ell(c + o, (sz, sz, sz * 0.85)), CRIMSON if j % 2 == 0 else DEEP,
                  "body")


_build, _laid = Kit.build, []
def build(self, *a, **kw):
    if not _laid:               # the first build is the body's; the kit builds the eyes' five states after it
        _laid.append(True)
        maxed(self)
    return _build(self, *a, **kw)
Kit.build = build

m = sibling("baxcaliburmega")
m.baxcaliburmega(*args())
