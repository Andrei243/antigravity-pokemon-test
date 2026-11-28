# Mega Dynamax Gardevoir (a form of this project's own) as Mega Gardevoir's own sculpt (gardevoirmega.py, which builds
# as it is read), whole and unchanged, with what the form lays over it put on the Mega's kit just before it is meshed:
#   blender --background --factory-startup --python sculpts/gardevoirmegammax.py -- <the export's Gardevoir-Mega-Mmax.glb> <out.glb>
# What the picture (docs/art/reference/gemini/megadynamax/Mega-Dynamax-Gardevoir.jpg) adds to the Mega, largest first:
#   1. a great dome of stained glass over the ball gown, from the ground up to the waist and some 0.7 of the height
#      across: panels of lavender, pink and pale cyan between pale ribs, two rings round it and a rose window on its
#      front (turned a little toward +X, the side the game's front camera sees);
#   2. the clouds of Max energy: a ring of crimson and violet clumps round the dome's foot with cyan curls, two clouds
#      hugging its sides and a wisp over each shoulder (loose puffs: the Mega is meshed at voxel 0.005, where fused
#      clouds would cost some sixty thousand vertices);
#   3. the Key Stone's emblem on the red bow through the chest: a glowing disc in a ring with a four-pointed star, on
#      both faces of the bow's front lobe.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, sheet, lathe, slab, ring_band, unit, outward, sibling

CRIMSON, DEEP, VIOLET, DVIOLET, CYAN = "#AC223E", "#701630", "#6C3EA0", "#42246E", "#4EE2EC"
LAV, PINK, TEAL, RIB = "#C49AF0", "#F2A2DC", "#96E4F2", "#FBE6FB"
ROSE, ROSEIN = "#F87AC8", "#8CF0F4"
EM_RING, EM_DISC, EM_STAR = "#FF5A6E", "#FFD4DA", "#FFFFFF"

R, HD, P = 0.35, 0.73, 2.6               # the dome: its radius on the ground, its height were it closed, its shape
ZTOP = 0.69                              # where it is open round the waist


def rad(z):
    return R * np.sqrt(np.clip(1 - np.clip(z / HD, 0, 1) ** P, 0, 1))


def join(*shapes):
    """Several closed shapes as one piece: a glowing piece is a material of its own, so like pieces go as one."""
    Vs, Fs, n = [], [], 0
    for V, F in shapes:
        Vs.append(np.asarray(V)); Fs += [[int(i) + n for i in f] for f in F]; n += len(V)
    return np.vstack(Vs), Fs


NA, NZ = 96, 48
TAB = None                               # the dome's radius by angle and height, once the gown has been measured


def measure(gown):
    """The dome's radius at each angle and height: the round dome's, or out past the gown wherever the gown reaches
    further (its train behind on +X), so that nothing of the gown shows through; smoothed round and kept from
    widening upward."""
    global TAB
    V = gown["V"]
    th = np.arctan2(V[:, 0], -V[:, 1]) % (2 * math.pi)
    rr = np.hypot(V[:, 0], V[:, 1])
    ia = np.minimum((th / (2 * math.pi) * NA).astype(int), NA - 1)
    zs = np.linspace(0, ZTOP, NZ)
    T = np.tile(rad(zs)[None, :], (NA, 1))
    reach = np.zeros((NA, NZ))
    for j, z in enumerate(zs):
        near = V[:, 2] >= z - 0.02                            # the gown above this height bounds it from above too
        for a in range(NA):
            sel = near & (ia == a)
            if sel.any():
                reach[a, j] = rr[sel].max()
    reach = np.maximum.accumulate(reach[:, ::-1], axis=1)[:, ::-1]
    T = np.maximum(T, reach + 0.022)
    for _ in range(4):                                       # smooth round, keeping it outside
        T = np.maximum(T, (np.roll(T, 1, 0) + T + np.roll(T, -1, 0)) / 3)
        T = (np.roll(T, 1, 0) + 2 * T + np.roll(T, -1, 0)) / 4
    TAB = np.maximum(T, rad(zs)[None, :])


def rd(th, z):
    """The dome's radius at angle th (from the front, -Y, toward +X) and height z."""
    if TAB is None:
        return rad(z)
    th = np.asarray(th, float) % (2 * math.pi)
    a = th / (2 * math.pi) * NA - 0.5
    a0 = np.floor(a).astype(int); fa = a - a0
    zz = np.clip(np.asarray(z, float) / ZTOP * (NZ - 1), 0, NZ - 1.001)
    z0 = np.floor(zz).astype(int); fz = zz - z0
    g = lambda i, j: TAB[i % NA, j]
    return ((1 - fa) * ((1 - fz) * g(a0, z0) + fz * g(a0, z0 + 1)) + fa * ((1 - fz) * g(a0 + 1, z0) + fz * g(a0 + 1, z0 + 1)))


def on_dome(th, z, out=0.0):
    """A point on the dome's surface at angle th from the front (-Y) toward +X and height z, lifted out by out."""
    r = float(rd(th, z)) + out
    return np.array([r * math.sin(th), -r * math.cos(th), z])


def dome(k):
    N = 16
    groups = {LAV: [], PINK: [], TEAL: []}
    order = (LAV, PINK, LAV, TEAL)
    for i in range(N):
        a0, a1 = 2 * math.pi * i / N, 2 * math.pi * (i + 1) / N
        def at(u, v, a0=a0, a1=a1):
            th = a0 + (a1 - a0) * u
            z = 0.006 + (ZTOP - 0.006) * v
            r = rd(th, z)
            return np.stack([r * np.sin(th), -r * np.cos(th), z], axis=-1)
        groups[order[i % 4]].append(outward(sheet(at, 6, 22, 0.01)))
    for col, shapes in groups.items():
        k.add(f"dome{col[1:]}", join(*shapes), col, "body", fuse=False, glow=col)
    # pale ribs from the ground to the top along every other border, two rings round it, a lip at its foot and its top
    ribs = []
    for i in range(0, N, 2):
        th = 2 * math.pi * i / N
        zs = np.linspace(0.01, ZTOP, 12)
        ribs.append(tube([on_dome(th, z, 0.004) for z in zs], [0.013] * 12, seg=10))
    for z, rr in ((0.24, 0.012), (0.5, 0.012), (0.02, 0.016), (ZTOP - 0.005, 0.014)):
        pts = [on_dome(2 * math.pi * j / 48, z, 0.004) for j in range(49)]
        ribs.append(tube(pts, [rr] * 49, seg=10))
    k.add("domeribs", join(*ribs), RIB, "body", fuse=False, glow=RIB)
    # the rose window on the front, turned a little toward +X
    th, zc = math.radians(22), 0.32
    c = on_dome(th, zc, 0.012)
    dz = 0.01
    n = unit(*(on_dome(th, zc, 0.0) - np.array([0, 0, zc]) + np.array([0, 0, (rad(zc - dz) - rad(zc + dz)) / (2 * dz)
                                                                          * rad(zc) * 0.0])))
    # the dome's slope at zc tilts the window's face up a little
    slope = (rad(zc - dz) - rad(zc + dz)) / (2 * dz)
    n = unit(*(n + np.array([0, 0, slope * 0.5])))
    ex = np.cross((0, 0, 1.0), n); ex /= np.linalg.norm(ex)
    ez = np.cross(n, ex)
    pts = [c + 0.105 * (math.cos(t) * ex + math.sin(t) * ez) for t in np.linspace(0, 2 * math.pi, 41)]
    k.add("roseframe", tube(pts, [0.013] * 41, seg=10), ROSE, "body", fuse=False, glow=ROSE)
    petals = []
    for j in range(8):
        t = 2 * math.pi * j / 8
        d = math.cos(t) * ex + math.sin(t) * ez
        petals.append(slab([(0.0, 0.0), (0.03, 0.018), (0.075, 0.0), (0.03, -0.018)], c + n * 0.004 + d * 0.022, d,
                           np.cross(n, d), 0.01))
    k.add("rosepetals", join(*petals), RIB, "body", fuse=False, glow=RIB)
    k.add("rosecore", lathe(c + n * 0.002, n, [(0, 0), (0.028, 0), (0.028, 0.012), (0, 0.014)]), ROSEIN, "body",
          fuse=False, glow=ROSEIN)


def clumps(name, places, size, cols, k, seed, bone="root"):
    """Clumps of round loose puffs of mixed sizes, nearly as high as wide, one small puff riding on top of each."""
    rnd = np.random.default_rng(seed)
    by = {}
    for i, (x, y, z) in enumerate(places):
        a = math.atan2(y, x)
        t = np.array([-math.sin(a), math.cos(a), 0])
        for j, (dt, dz, s) in enumerate(((0, 0, 1.0), (0.07, -0.01, 0.78), (-0.065, -0.01, 0.7), (0.005, 0.07, 0.52))):
            ss = size * s * (1 + 0.12 * rnd.uniform(-1, 1))
            col = cols[i % len(cols)] if j != 2 else cols[(i + 1) % len(cols)]
            by.setdefault(col, []).append(ell(np.array([x, y, z]) + t * dt * size / 0.07 + (0, 0, dz * size / 0.07),
                                              (ss, ss, ss * 0.9), sub=3))
    for col, shapes in by.items():
        k.add(f"{name}{col[1:]}", join(*shapes), col, bone, fuse=False)


def maxed(k):
    """What the Mega Dynamax form lays over Mega Gardevoir: called with the Mega's kit just before it is meshed."""
    measure(next(p for p in k.parts if p["name"] == "gown"))
    dome(k)
    # The emblem on both faces of the bow's front lobe (the Mega's hornF part)
    horn = next(p for p in k.parts if p["name"] == "hornF")
    V = horn["V"]
    yc, zc = -0.095, 0.772
    sel = V[(np.abs(V[:, 1] - yc) < 0.012) & (np.abs(V[:, 2] - zc) < 0.012)]
    xf = sel[:, 0].max()
    c = np.array([xf + 0.002, yc, zc])
    n = np.array([1.0, 0, 0])
    k.add("emblemdisc", lathe(c, n, [(0, 0), (0.022, 0), (0.022, 0.006), (0, 0.008)]), EM_DISC, "body", fuse=False,
          mirror=True, glow=EM_DISC)
    k.add("emblemring", ring_band(c + n * 0.004, n, 0.028, 0.008, thick=0.01), EM_RING, "body", fuse=False,
          mirror=True, glow=EM_RING)
    star = [(0.019, 0), (0.005, 0.005), (0, 0.019), (-0.005, 0.005), (-0.019, 0), (-0.005, -0.005), (0, -0.019),
            (0.005, -0.005)]
    k.add("emblemstar", slab(star, c + n * 0.009, (0, -1.0, 0), (0, 0, 1.0), 0.004), EM_RING, "body", fuse=False,
          mirror=True, glow=EM_RING)
    # The clouds: a ring of clumps round the dome's foot, two hugging its sides, a wisp over each shoulder
    # (none where the dome runs out over the train: there the dome itself reaches as far as the clouds would)
    ring = []
    n_ = 13
    for i in range(n_):
        a = 2 * math.pi * (i + 0.3) / n_
        r0 = float(rd(a, 0.0))
        if r0 > R + 0.04:
            continue
        rr = r0 + 0.055
        ring.append((rr * math.sin(a), -rr * math.cos(a), 0.064))
    clumps("cloud", ring, 0.062, (CRIMSON, VIOLET, DEEP, DVIOLET), k, 7)
    clumps("sidecloud", [on_dome(math.radians(-72), 0.3, 0.05), on_dome(math.radians(68), 0.27, 0.05)], 0.06,
           (DEEP, CRIMSON), k, 9, bone="body")
    curls = [ell(np.array(p) + (0, 0, 0.1), (0.022, 0.022, 0.02), sub=3) for p in ring[::3]]
    k.add("curls", join(*curls), CYAN, "root", fuse=False, glow=CYAN)
    # a curl of violet cloud rising beside each arm, off the dome's top, clear of the hair
    for s, col in ((1, VIOLET), (-1, DEEP)):
        pts = [(0.15 * s, 0.07, 0.7), (0.19 * s, 0.09, 0.74), (0.21 * s, 0.07, 0.79)]
        k.add(f"wisp{'R' if s > 0 else 'L'}", join(*[ell(p, (r, r, r * 0.9), sub=3) for p, r in
                                                     zip(pts, (0.034, 0.028, 0.02))]), col, "body", fuse=False)


_build, _laid = Kit.build, []
def build(self, *a, **kw):
    if not _laid:               # the first build is the body's; the kit builds the eyes' five states after it
        _laid.append(True)
        maxed(self)
    return _build(self, *a, **kw)
Kit.build = build

# gardevoirmega.py builds as it is read, with this script's own arguments
sibling("gardevoirmega")
