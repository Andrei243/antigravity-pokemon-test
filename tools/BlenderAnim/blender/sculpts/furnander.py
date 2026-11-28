# Furnander sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/furnander.py -- <the export's Furnander.glb> <out.glb>
# Carpathia's Fire starter's second stage (No. 10005), from its picture (10005-Furnander.jpg) and its brief
# (CP-dex-1-plateau.md, "5. Furnander"): Salamint grown long and half upright, a heavy salamander leaning forward over
# fat haunches fused into its hips, short bent shins and broad clawed feet; armoured in soot-black scales like the
# bricks of a village hearth, a row of raised brick blocks down its back, Salamint's blotches become glowing orange
# cracks (on the shoulders, the back, the arm, the thighs and the tail, carried round the back); a long flat head with
# a heavy brow over small amber eyes, a long shut mouth with a lighter taupe lower jaw and throat; a smith's apron of
# leather-brown skin hanging from a fold round its neck to its shins, its hem ragged; vents along its back with smoke
# curling from them; short forearms held bent before the apron with dark claws; a long tail sweeping back along the
# ground and curling up on the +X side to a great glowing coal. Grown from salamint.py (its colours, its eye, its coal,
# its marks and the board check); Fierander is this build at 1.25 with the smith's iron laid over it (fierander.py).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, sheet, lathe, boxy, srgb, inside, smoothstep, unit, tri, args, sibling, outward

sm = sibling("salamint")
BLACK = "#2E2A2D"           # soot-black scales
BRICK = "#403A3D"           # the raised brick blocks
TAUPE = "#5E504C"           # the lower jaw and throat
LEATHER = "#7C5236"         # the apron
LEATHER_D = "#5E3C28"       # its fold and hem
CLAW = "#4C4649"
SMOKE = "#9A9698"
INK = sm.INK
AMBER = srgb("#C07E36")
VOXEL = 0.0075


def crack(c, dirs, w, rmax, rmin=0.0):
    """A crack painted round a part's centre c: a polyline of directions from c (great-circle strokes), w its half
    width in radians; only points between rmin and rmax from c (so another part in that direction is left alone)."""
    c = np.array(c, float)
    D = np.array([unit(*d) for d in dirs])
    def m(P):
        q = P - c
        r = np.linalg.norm(q, axis=1)
        u = q / np.maximum(r, 1e-9)[:, None]
        best = np.full(len(P), 9.0)
        for a, b in zip(D[:-1], D[1:]):
            ab = b - a
            t = np.clip(((u - a) @ ab) / (ab @ ab), 0, 1)
            best = np.minimum(best, np.linalg.norm(u - (a + t[:, None] * ab), axis=1))
        return inside(best - w, w * 0.25) * (r < rmax) * (r > rmin)
    return m


def branch(d0, spread, seed, n=3, length=0.32):
    """A branching crack's strokes from a direction: n arms, each two bends, kept to a cone (lists of directions)."""
    rs = np.random.RandomState(seed)
    d0 = unit(*d0)
    a = np.cross(d0, (0, 0, 1.0))
    if np.linalg.norm(a) < 1e-3:
        a = np.array([1.0, 0, 0])
    a /= np.linalg.norm(a); b = np.cross(d0, a)
    arms = []
    for i in range(n):
        th = 2 * math.pi * i / n + rs.uniform(-0.4, 0.4)
        e = math.cos(th) * a + math.sin(th) * b
        p1 = d0 + e * length * 0.5 * spread + (rs.normal(size=3) * 0.05)
        p2 = d0 + e * length * spread + (rs.normal(size=3) * 0.08)
        arms.append([tuple(d0), tuple(p1), tuple(p2)])
    return arms


def glow_crack(k, c, d0, spread, seed, w, rmax, rmin=0.0, n=3):
    """A glowing crack: an orange line with a gold heart along it, branching from d0."""
    for arm in branch(d0, spread, seed, n):
        k.paint(crack(c, arm, w, rmax, rmin), sm.EMBER)
    for arm in branch(d0, spread, seed, n):
        k.paint(crack(c, arm, w * 0.42, rmax, rmin), sm.GOLD)



def scale_block(c, n, r):
    """A raised scale like a brick: a rounded box fused into the skin, its face turned along n."""
    n = unit(*n)
    th = -math.asin(np.clip(n[1], -1, 1))
    ph = math.atan2(n[0], n[2])
    return ell(c, r, rot=(math.degrees(th), math.degrees(ph), 0), deform=boxy(4))


def disc(c, n, r, t):
    """A flat round plate centred at c facing n (a vent's glowing throat, a rivet's head)."""
    n = unit(*n)
    th = -math.asin(np.clip(n[1], -1, 1))
    ph = math.atan2(n[0], n[2])
    return outward(ell(c, (r, r, t), rot=(math.degrees(th), math.degrees(ph), 0), sub=3))


def hearth(src, out, K=1.0, smith=None):
    """The hearth salamander at scale K (Furnander 1, Fierander 1.25); smith lays the iron over it (fierander.py)."""
    def S(*v):
        return np.array(v, float) * K
    k = Kit(src, coat="scales")
    F = {}

    # --- the body: leaning forward, a deep chest, a pear of a belly low between fat haunches --------------------
    k.add("torso", ell(S(0, -0.02, 0.53), S(0.2, 0.17, 0.2), rot=(14, 0, 0)), BLACK, "body")
    k.add("chest", ell(S(0, -0.06, 0.62), S(0.19, 0.15, 0.12), rot=(10, 0, 0)), BLACK, "body")
    k.add("belly", ell(S(0, 0.03, 0.36), S(0.205, 0.19, 0.16)), BLACK, "body")
    k.add("neck", tube([S(0, -0.02, 0.58), S(0, -0.07, 0.67), S(0, -0.1, 0.73)], [0.115 * K, 0.105 * K, 0.095 * K]),
          BLACK, "head")

    # --- the head: long and flat, a heavy brow, the snout tapering to a blunt end, a full lower jaw --------------
    H, HR = S(0, -0.14, 0.785), S(0.138, 0.16, 0.088)
    F["H"], F["HR"] = H, HR
    def taper(V):
        V = V.copy()
        t = np.clip(-V[:, 1], 0, 1)
        V[:, 0] *= 1 - 0.18 * t
        V[:, 2] *= 1 - 0.15 * t
        return V
    k.add("head", ell(H, HR), BLACK, "head")
    k.add("snout", ell(S(0, -0.265, 0.77), S(0.108, 0.11, 0.066), deform=taper), BLACK, "head")
    k.add("jaw", ell(S(0, -0.205, 0.722), S(0.12, 0.155, 0.053), rot=(-4, 0, 0)), BLACK, "head")
    k.add("brow", ell(S(0.072, -0.165, 0.832), S(0.052, 0.07, 0.028), rot=(0, 0, -8)), BLACK, "head", mirror=True)

    # --- the legs: haunches as wide as the head fused into the hips, a short bent shin, broad feet, dark claws ---
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"thigh{side}", ell(S(0.15, -0.0, 0.27) * m, S(0.115, 0.15, 0.135), rot=(-20, 0, 0)), BLACK, f"leg{side}")
        k.add(f"shin{side}", tube([S(0.16, -0.06, 0.17) * m, S(0.168, -0.05, 0.1) * m, S(0.17, -0.04, 0.06) * m],
                                  [0.07 * K, 0.064 * K, 0.06 * K]), BLACK, f"leg{side}")
        k.add(f"foot{side}", ell(S(0.172, -0.075, 0.034) * m, S(0.075, 0.1, 0.034)), BLACK, f"leg{side}")
        for j, dx in enumerate((-0.045, -0.015, 0.015, 0.045)):
            b = S(0.172 + dx, -0.155, 0.022)
            k.add(f"toe{side}{j}", sibling_claw(b * m, np.array((dx * 1.2, -1, -0.5)) * m, 0.045 * K, 0.026 * K,
                                                0.02 * K), CLAW, f"leg{side}", fuse=False, surface="shell")

    # --- the arms: short and strong, the upper arm down the side, the forearm bent forward before the apron ------
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        elbow, wrist, hand = S(0.215, -0.08, 0.5), S(0.165, -0.22, 0.47), S(0.155, -0.26, 0.455)
        k.add(f"shoulder{side}", ell(S(0.17, -0.05, 0.62) * m, S(0.075, 0.08, 0.08)), BLACK, f"arm{side}")
        k.add(f"upper{side}", tube([S(0.17, -0.05, 0.62) * m, elbow * m], [0.065 * K, 0.056 * K]), BLACK, f"arm{side}")
        k.add(f"fore{side}", tube([elbow * m, wrist * m], [0.054 * K, 0.047 * K]), BLACK, f"arm{side}")
        k.add(f"hand{side}", ell(hand * m, S(0.05, 0.048, 0.044)), BLACK, f"arm{side}")
        for j, dx in enumerate((-0.028, 0.0, 0.028)):
            b = hand + S(dx, -0.035, -0.02)
            k.add(f"claw{side}{j}", sibling_claw(b * m, np.array((dx * 1.5, -0.7, -1)) * m, 0.045 * K, 0.024 * K,
                                                 0.019 * K, up=(0, 1, 0)), CLAW, f"arm{side}", fuse=False, surface="shell")
        F[f"elbow{side}"], F[f"wrist{side}"], F[f"hand{side}"] = elbow * m, wrist * m, hand * m

    # --- the tail: thick at the hips, sweeping back low along the ground and curling up on the +X side ----------
    TAIL = [S(0, 0.15, 0.3), S(0.05, 0.3, 0.185), S(0.15, 0.43, 0.145), S(0.26, 0.48, 0.22), S(0.31, 0.45, 0.375),
            S(0.3, 0.4, 0.52)]
    TR = [0.13 * K, 0.1 * K, 0.08 * K, 0.066 * K, 0.058 * K, 0.053 * K]
    k.add("tail", tube(TAIL, TR), BLACK, "tail")
    F["tail"] = (TAIL, TR)
    end = S(0.295, 0.392, 0.6)
    if smith is None:
        sm.coal(k, end, 0.085 * K, "tail", seed=11)
    else:
        smith.tail_end(k, K, TAIL, TR)

    # --- the raised bricks down the back, two rows from the shoulders to the hips, and on along the tail --------
    back = [(0.68, 0.125, 0.56), (0.6, 0.16, 0.5), (0.52, 0.18, 0.42), (0.44, 0.205, 0.36), (0.36, 0.215, 0.3)]
    if smith is None:
        # a ridge of blocks down the spine and courses of bricks beside it, staggered as a hearth's are: fused, so
        # they stand from the skin as raised scales, not as boxes stuck on
        for i, (z, y, ny) in enumerate(back):
            k.add(f"ridge{i}", scale_block(S(0, y - 0.004, z), (0, 1, ny), S(0.042, 0.036, 0.028)), BRICK, "body")
        for i, (z, y, ny) in enumerate(back[:-1]):
            for s in (-1, 1):
                p = S(0.1 * s, y - 0.03 - 0.004 * i, z - 0.04)
                k.add(f"brick{i}{'R' if s > 0 else 'L'}", scale_block(p, (0.6 * s, 1, ny), S(0.04, 0.03, 0.03)),
                      BRICK, "body")
        for i, t in enumerate((0.3, 0.5, 0.68)):
            j = int(t * (len(TAIL) - 1)); f = t * (len(TAIL) - 1) - j
            p = TAIL[j] * (1 - f) + TAIL[j + 1] * f
            r = TR[j] * (1 - f) + TR[j + 1] * f
            k.add(f"tailbrick{i}", scale_block(p + np.array([0, 0, r * 0.82]), (0, 0, 1), S(0.034, 0.032, 0.026)),
                  BRICK, "tail")
    else:
        smith.back(k, K, back, TAIL, TR)

    # --- the vents along the back: a raised rim with the fire glowing in its throat (the picture's smoke is left
    # out: as thick curls it read as pipes) ---------------------------------------------------------------------
    for i, (z, y) in enumerate(((0.64, 0.095), (0.54, 0.135), (0.44, 0.16))):
        p = S(0.135, y, z)
        n = unit(0.55, 1, 0.55 - 0.15 * i)
        k.add(f"vent{i}", ell(p, S(0.032, 0.032, 0.032)), BRICK, "body")
        k.add(f"ventfire{i}", disc(p + n * 0.028 * K, n, 0.02 * K, 0.008 * K), sm.CHIP, "body", fuse=False,
              glow=sm.CHIP_GLOW)

    # --- the apron: a fold of leather round the neck and a broad flap hanging to the shins, its hem ragged -------
    top, bottom, h0, hg = getattr(smith, "APRON", (0.655, 0.11, 0.13, 0.045))
    def apron(u, v):
        x = (u - 0.5) * 2
        half = (h0 + hg * v) * K
        rag = (0.035 * tri(u * 3.3 + 0.2) + 0.015 * tri(u * 7.1)) * K
        z = top * K - ((top - bottom) * K + rag) * v
        y = (-0.21 - 0.03 * np.sin(math.pi * np.clip(v * 1.3, 0, 1)) + 0.03 * v) * K + 1.1 * (x * half) ** 2 / K
        return np.stack([x * half, y, z], axis=-1)
    k.add("apron", outward(sheet(apron, 30, 40, 0.014 * K)), LEATHER, "body", fuse=False, surface="leather")
    k.add("fold", tube([S(-0.135, -0.13, 0.69), S(-0.08, -0.205, 0.67), S(0, -0.23, 0.66), S(0.08, -0.205, 0.67),
                        S(0.135, -0.13, 0.69)], [0.028 * K] * 5), LEATHER_D, "body", fuse=False, surface="leather")

    # --- the glowing cracks: where Salamint's blotches were, thick and branching, carried round the back --------
    w = 0.07
    T = S(0, -0.02, 0.53)
    for d, seed in (((0.8, -0.3, 0.55), 1), ((-0.75, 0.1, 0.6), 2), ((0.35, 0.85, 0.35), 3), ((-0.4, 0.85, 0.1), 4),
                    ((0.75, 0.55, -0.25), 5)):
        glow_crack(k, T, d, 1.0, seed, w, 0.3 * K, 0.12 * K)
    Bc = S(0, 0.03, 0.36)
    for d, seed in (((0.6, 0.75, 0.2), 6), ((-0.7, 0.65, 0.1), 7)):
        glow_crack(k, Bc, d, 0.9, seed, w, 0.26 * K, 0.12 * K)
    for s, seed in ((1, 8), (-1, 9)):
        glow_crack(k, S(0.15 * s, 0.0, 0.27), (0.9 * s, 0.1, 0.4), 0.8, seed, w * 1.1, 0.17 * K, 0.08 * K)
        glow_crack(k, S(0.19 * s, -0.065, 0.56), (0.9 * s, 0.1, 0.4), 0.7, seed + 20, w * 1.3, 0.09 * K, 0.04 * K, n=2)
    glow_crack(k, H, (0.6, 0.55, 0.6), 0.7, 12, w * 0.9, 0.17 * K, 0.06 * K, n=2)
    for i, t in enumerate((0.25, 0.48, 0.72)):
        j = int(t * (len(TAIL) - 1)); f = t * (len(TAIL) - 1) - j
        p = TAIL[j] * (1 - f) + TAIL[j + 1] * f
        r = TR[j] * (1 - f) + TR[j + 1] * f
        glow_crack(k, p, (0.8, -0.3 + 0.5 * i, 0.6), 0.9, 30 + i, w * 1.4, r * 1.6, r * 0.5)

    # --- the face: the lower jaw and throat in taupe under a long shut mouth curving up at its back; nostrils ---
    def mouth_z(y):
        t = np.clip((y - (-0.33 * K)) / (0.27 * K), 0, 1)
        return (0.748 + 0.03 * t * t) * K
    def lower(P):
        return (inside(P[:, 2] - mouth_z(P[:, 1]), 0.002 * K) * inside(P[:, 1] + 0.11 * K, 0.02 * K)
                * (P[:, 2] > 0.6 * K) * inside(np.abs(P[:, 0]) - 0.13 * K, 0.004 * K))
    k.paint(lower, TAUPE)
    def mouth(P):
        return (inside(np.abs(P[:, 2] - mouth_z(P[:, 1])) - 0.0045 * K, 0.0015 * K) * (P[:, 1] < -0.06 * K)
                * (P[:, 1] > -0.4 * K) * (P[:, 2] > 0.65 * K))
    k.paint(mouth, "#140F12" if smith is None else smith.MOUTH)
    k.decal(S(0.032, -0.352, 0.785), unit(0.25, -0.8, 0.55), (0.009 * K, 0.007 * K), TAUPE, eps=0.003 * K,
            rings=4, segs=16, mirror=True)

    if smith is not None:
        smith.armour(k, K, F)

    # --- the eyes: Salamint's eye, smaller against the head, under the heavy brow ------------------------------
    de = unit(*getattr(smith, "EYE_DIR", (0.74, -0.46, 0.5)))
    E = sm.on_ell(H, HR, de) + S(0, -0.012, 0.0)
    cut = sm.brow_cut(*getattr(smith, "BROW", (0.34, 0.32)))
    if smith is None:
        look = sm.eye_open(AMBER, cut=cut, pupil=0.36, glint=(-0.32, 0.18, 0.24))
        lid = srgb(BLACK)
    else:
        look, lid = smith.eye(cut)
    lay = sm.lay_eyes(k, E, de, (0.036 * K, 0.034 * K), look, lid, eps=0.004 * K, cut=cut,
                      fierce=None if smith is None else smith.fierce(cut))

    ob = k.build(voxel=VOXEL * K, smooth=4, band=0.008 * K, wband=0.05 * K)
    sm.board(ob)
    k.eye_states(lay)
    k.export(out)


def sibling_claw(base, d, length, width, thick=None, up=(0, 0, 1)):
    """A claw: a flat-faced solid from a kite at its root to its point along d (charmander.py's way)."""
    d = unit(*d)
    a = np.cross(d, np.array(up, float)); a /= np.linalg.norm(a)
    b = np.cross(a, d)
    thick = thick if thick is not None else width * 0.8
    base = np.array(base, float)
    mid = base + d * length * 0.45 - b * length * 0.06
    pts = [base + a * width / 2, base - a * width / 2, base + b * thick / 2, base - b * thick / 2 - d * 0.004,
           mid + a * width * 0.28, mid - a * width * 0.28, mid + b * thick * 0.3,
           base + d * length - b * length * 0.22]
    return hull(pts)


if __name__ == "__main__":
    hearth(*args())
