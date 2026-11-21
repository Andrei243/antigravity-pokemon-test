# Mega Magearna sculpted from nothing on the skeleton and clips of the game's own export of the form (sculptkit.py):
#   blender --background --factory-startup --python sculpts/magearnamega.py -- <the export's Magearna-Mega.glb> <out.glb>
# Mega Magearna's Original Color (magearnaoriginalmega.py) is this script called with original=True.
#
# Grown from magearna.py (which builds under __main__: its on_ell is imported, the rest copied here and changed, since
# its functions name its own colours and ear bones, which the Mega's skeleton has not): the same ball of a gown with
# gold smiles round its upper half and two lobes over two pointed legs, the same thin neck, round shoulders and pointed
# hand, the same round head with lens eyes in frames, the same ear plates rimmed in gold.
#
# What the Mega changes, from the pictures, largest mass first:
#   1. two great cogs at its sides, each a C of some 270 degrees with six square teeth, its gap turned up and in toward
#      the head; each some 0.38 of the height across (picture), their outer edges 1.15 of the height apart; dark grey
#      (Mega), white (the Original Color's);
#   2. the gown turns red with two rows of gold smiles, its lower half and lobes dark (Mega) or white (Original), a
#      dark red band between, and a dark (or white) lid on its top round the neck; the legs are longer, gold;
#   3. the ears become red crescents: a U of two prongs pointing up, the outer one taller and curling in, rimmed gold,
#      rising some 0.23 of the height over the head;
#   4. a crown of five silver points on the head;
#   5. the body turns gold (head, neck, arms); the chest is a round medallion (a red ring on Mega, gold on the
#      Original), a white ring inside and a shield of blue (viewer's left) and red (right), and a red sash hangs from
#      it down the gown's front on the +X side;
#   6. the -X hand holds a gold wheel of six pointed spokes round a red cone; the +X arm hangs with a pointed hand;
#   7. the eyes are pink lenses with a white crosshair in thin dark red frames.
# The art holds the wheel's arm straight out; here it is carried forward and down, the wheel in front of the gown's -X
# side facing the front three-quarter. The cogs are drawn a little nearer the gown than the picture (centres at 0.36
# of the height from the middle against 0.42) and yawed 20 degrees back at their outer edges, so the model is about
# 1.1 times as wide as it is tall rather than 1.15.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, hull, slab, lathe, ring_band, sheet, srgb, inside, unit, eye_look, args, sibling,
                       outward)

base = sibling("magearna")
on_ell = base.on_ell

RED, RED_DK, GOLD, SMILE = "#CC2438", "#8E1626", "#F0D26C", "#F6D23A"
SILVER, BLUE, WHITE, INK = "#D8DCE6", "#3C86D0", "#F2F0F2", "#5A1A2A"
PINK, PINK_DK, FRAME = "#F6A6C6", "#B0204E", "#9A1C3C"

GC, GR = np.array([0.0, 0.0, 0.235]), np.array([0.158, 0.158, 0.13])     # the gown
CH = np.array([0.0, -0.004, 0.425])                                        # the chest
MED = np.array([0.0, -0.038, 0.418])                                       # the medallion's front
H, HR = np.array([0.0, -0.006, 0.525]), np.array([0.074, 0.066, 0.074])   # the head


def offset(p, d):
    """A polygon's outline moved out by d (mitred)."""
    p = np.array(p, float)
    ccw = (p[:, 0] * np.roll(p[:, 1], -1) - np.roll(p[:, 0], -1) * p[:, 1]).sum() > 0
    out = []
    n = len(p)
    for i in range(n):
        a, b, c = p[i - 1], p[i], p[(i + 1) % n]
        def nrm(e):
            e = e / np.linalg.norm(e)
            return np.array([e[1], -e[0]]) if ccw else np.array([-e[1], e[0]])
        n1, n2 = nrm(b - a), nrm(c - b)
        m = n1 + n2
        m /= np.linalg.norm(m)
        k = min(2.0, 1 / max(0.3, float(m @ n1)))
        out.append(b + m * d * k)
    return [tuple(q) for q in out]


def cog_outline():
    """A C of a cog in its own plane (a out, b up): 270 degrees, six teeth, its gap up and in (toward -a)."""
    A0, A1, RO, T, RI = 170.0, 440.0, 0.104, 0.028, 0.062
    centres = [A0 + 22.5 + 45 * i for i in range(6)]
    half = 9.0
    pts = []
    angs = sorted(set(list(np.arange(A0, A1 + 0.01, 3.0)) + [c + s * half for c in centres for s in (-1, 1)]))
    for t in angs:
        tooth = [c for c in centres if abs(t - c) <= half + 1e-6]
        if tooth and abs(abs(t - tooth[0]) - half) < 1e-6:
            # the tooth's flank: both radii at the same angle, in the order the walk needs
            r1, r2 = (RO, RO + T) if t < tooth[0] else (RO + T, RO)
            pts += [(r1 * math.cos(math.radians(t)), r1 * math.sin(math.radians(t))),
                    (r2 * math.cos(math.radians(t)), r2 * math.sin(math.radians(t)))]
            continue
        r = RO + T if tooth else RO
        pts.append((r * math.cos(math.radians(t)), r * math.sin(math.radians(t))))
    for t in np.arange(A1, A0 - 0.01, -6.0):
        pts.append((RI * math.cos(math.radians(t)), RI * math.sin(math.radians(t))))
    return pts


def add_gown(k, lower, lid):
    k.add("gown", ell(GC, GR, sub=5), RED, "body")
    for s in (1, -1):
        k.add(f"lobe{'R' if s > 0 else 'L'}", ell((0.058 * s, 0.004, 0.138), (0.068, 0.085, 0.046)), lower, "body")
    k.paint(lambda P: inside(P[:, 2] - GC[2] + 0.048, 0.003), lower, on=["gown", "lobe"])
    # the dark red band between the halves
    band_z = GC[2] - 0.038
    band_r = GR[0] * math.sqrt(1 - ((band_z - GC[2]) / GR[2]) ** 2)
    k.add("band", ring_band((0, 0, band_z), (0, 0, 1), band_r - 0.002, 0.02, thick=0.007), RED_DK, "body",
          fuse=False, surface="metal")
    # the lid on the gown's top round the neck
    k.add("lid", lathe((0, 0, GC[2] + GR[2] - 0.012), (0, 0, 1), [(0, 0.0), (0.072, 0.0), (0.07, 0.008),
                                                                    (0, 0.012)], seg=48), lid, "body", fuse=False,
          surface="metal")
    # two rows of gold smiles: larger ones above the band, small ones near the top between them
    for row, (el0, wide, sag, rmax, n0) in enumerate(((18, 0.3, 0.13, 0.012, 0.0), (50, 0.26, 0.1, 0.009, 22.5))):
        for i in range(8):
            az = math.radians(n0 + 45 * i)
            el = math.radians(el0)
            out_ = np.array([math.sin(az) * math.cos(el), -math.cos(az) * math.cos(el), math.sin(el)])
            t = np.array([math.cos(az), math.sin(az), 0.0])
            up = np.array([-math.sin(az) * math.sin(el), math.cos(az) * math.sin(el), math.cos(el)])
            pts = []
            for s in np.linspace(-1, 1, 7):
                d = out_ + t * s * wide - up * sag * (1 - s * s)
                p, n = on_ell(GC, GR, d)
                pts.append(p + n * 0.004)
            r = [rmax * f for f in (0.4, 0.7, 0.92, 1.0, 0.92, 0.7, 0.4)]
            k.add(f"smile{row}{i}", tube(pts, r, seg=12, per=4), SMILE, "body", fuse=False, surface="metal")
    # the long pointed gold legs under the lobes
    k.add("leg", tube([(0.042, -0.01, 0.13), (0.048, -0.015, 0.07), (0.05, -0.02, 0.003)], [0.02, 0.015, 0.004]),
          GOLD, "leg{s}", mirror=True)


def add_chest(k, ring):
    k.add("neck", tube([(0, -0.002, 0.36), (0, -0.004, 0.47)], [0.016, 0.016]), GOLD, "head")
    k.add("chest", ell(CH, (0.05, 0.036, 0.046)), GOLD, "body")
    # the medallion: a disc fused to the chest for its patches
    # (gold, its face painted in the ring's colour with a crisp edge: fused in red it washed red over the chest)
    k.add("medal", ell(MED + np.array([0, 0.012, 0]), (0.046, 0.016, 0.046)), GOLD, "body")
    k.paint(lambda P: inside(np.hypot(P[:, 0], P[:, 2] - MED[2]) - 0.041, 0.0015) * inside(P[:, 1] + 0.03, 0.0015),
            ring)

    def shield(u, v):
        c = np.tile(srgb(BLUE), (len(u), 1))
        c[u > 0] = srgb(RED)
        return c
    k.decal(MED + np.array([0, -0.004, 0]), (0, -1, 0), (0.031, 0.031), WHITE, eps=0.002, rings=10, segs=48)
    k.decal(MED + np.array([0, -0.004, 0]), (0, -1, 0), (0.022, 0.024), shield, eps=0.0034, rings=10, segs=48,
            outline=base_shield)
    # the red sash from the medallion down the gown's front on the +X side
    def sash(u, v):
        az = np.radians(14 + 8 * u)
        el = np.radians(72 - 92 * u)
        D = np.stack([np.sin(az) * np.cos(el), -np.cos(az) * np.cos(el), np.sin(el)], axis=-1)
        T = np.stack([np.cos(az), np.sin(az), np.zeros_like(az)], axis=-1)
        s = 1 / np.sqrt(((D / GR) ** 2).sum(-1))
        P = GC + D * s[..., None]
        N = (P - GC) / GR ** 2
        N /= np.linalg.norm(N, axis=-1, keepdims=True)
        w = 0.017 - 0.004 * u
        return P + N * 0.007 + T * ((v - 0.5) * 2 * w)[..., None]
    k.add("sash", outward(sheet(sash, 24, 4, 0.007)), RED_DK, "body", fuse=False, surface="cloth")
    # round shoulders, thin arms
    k.add("shoulder", ell((0.062, -0.004, 0.43), (0.03, 0.03, 0.03)), GOLD, "arm{s}", mirror=True)
    # +X: hanging down and forward to a pointed hand in front of the gown
    k.add("armR", tube([(0.078, -0.01, 0.426), (0.108, -0.04, 0.39), (0.126, -0.072, 0.35)], [0.013, 0.012, 0.013]),
          GOLD, "armR")
    k.add("handR", tube([(0.124, -0.07, 0.352), (0.134, -0.098, 0.322), (0.14, -0.124, 0.29), (0.143, -0.146, 0.262)],
                        [0.018, 0.022, 0.014, 0.005]), GOLD, "armR")
    # -X: carried forward and down, holding the wheel
    k.add("armL", tube([(-0.078, -0.01, 0.426), (-0.104, -0.05, 0.405), (-0.122, -0.1, 0.385)], [0.013, 0.012, 0.013]),
          GOLD, "armL")
    k.add("handL", ell((-0.126, -0.112, 0.382), (0.02, 0.02, 0.02)), GOLD, "armL")
    add_wheel(k)


def base_shield(th):
    return SHIELD(th)


SHIELD = None


def add_wheel(k):
    """The wheel in the -X hand: six pointed gold spokes round a red cone, facing the front three-quarter."""
    c = np.array([-0.14, -0.14, 0.38])
    n = unit(-0.3, -1.0, 0.12)
    a = np.cross(n, (0, 0, 1.0)); a /= np.linalg.norm(a)
    b = np.cross(a, n)
    for i in range(6):
        t = math.radians(15 + 60 * i)
        e = math.cos(t) * a + math.sin(t) * b
        w = math.cos(t + math.pi / 2) * a + math.sin(t + math.pi / 2) * b
        L, W, Th = 0.08, 0.014, 0.011
        pts = [c + e * 0.006 + w * s * W * 0.7 + n * h * Th for s in (1, -1) for h in (1, -1)]
        pts += [c + e * L * 0.62 + w * s * W + n * h * Th for s in (1, -1) for h in (1, -1)]
        pts += [c + e * L]
        k.add(f"spoke{i}", hull(pts), GOLD, "armL", fuse=False, surface="metal")
    k.add("hub", lathe(c, -n, [(0, -0.012), (0.022, -0.012), (0.02, 0.006), (0.009, 0.03), (0, 0.042)], seg=20),
          RED, "armL", fuse=False, surface="metal")


def add_head(k, cog):
    k.add("head", ell(H, HR), GOLD, "head")
    # the crown of five silver points
    for x in (-0.03, -0.015, 0.0, 0.015, 0.03):
        root = H + np.array([x, 0.008, HR[2] * 0.82])
        d = unit(x * 7, 0.18, 1)
        L, r = 0.066 - abs(x) * 0.6, 0.011
        k.add(f"crown{int(round(x * 1000))}", lathe(root, d, [(0, -r * 0.6), (r, -r * 0.1), (r * 0.62, L * 0.45),
                                                               (0, L)], seg=12), SILVER, "head", fuse=False,
              surface="metal")
    # the ears: red crescents, a U of two prongs up, the outer taller and curling in, rimmed in gold
    ear = [(0.03, 0.10), (0.045, 0.06), (0.06, 0.042), (0.08, 0.04), (0.097, 0.056), (0.105, 0.1), (0.101, 0.138),
           (0.09, 0.172), (0.118, 0.16), (0.137, 0.126), (0.144, 0.08), (0.132, 0.035), (0.1, 0.005), (0.06, -0.008),
           (0.024, 0.0), (0.005, 0.035), (0.012, 0.075)]
    ex, ez, o = unit(0.95, 0.2, 0), unit(0, 0.25, 1), (0.034, 0.012, 0.552)
    k.add("ear", slab(ear, o, ex, ez, 0.026), RED, "head", fuse=False, mirror=True, surface="metal")
    k.add("earrim", slab(offset(ear, 0.006), o, ex, ez, 0.016), GOLD, "head", fuse=False, mirror=True,
          surface="metal")
    # the cogs at its sides
    k.add("cog", slab(cog_outline(), (0.255, 0.06, 0.27), unit(0.94, 0.34, 0), (0, 0, 1), 0.042), cog, "body",
          fuse=False, mirror=True, surface="metal")
    # the lens frames (in every state, under the eyes' own patches) and the tiny mouth
    for side in (1, -1):
        p, n = on_ell(H, HR, (0.5 * side, -0.84, 0.0))
        k.decal(p, n, (0.029, 0.031), FRAME, eps=0.002, rings=16, segs=56,
                outline=lambda th: 1.0 + 0.22 * max(0.0, math.sin(th)) ** 24)
    p, n = on_ell(H, HR, (0, -0.9, -0.46))
    k.decal(p, n, (0.007, 0.006), INK, eps=0.002, rings=6, segs=32)


def open_eye(u, v):
    r = u * u + v * v
    c = np.tile(srgb(PINK), (len(u), 1))
    c[r > 0.82 ** 2] = srgb(PINK_DK)
    cross = ((np.abs(u) < 0.1) | (np.abs(v) < 0.1)) & (r < 0.64 ** 2)
    c[cross] = (1, 1, 1)
    c[r < 0.16 ** 2] = srgb(PINK_DK)
    return c


def eyes(k, state):
    look = eye_look(state, open_eye, srgb(GOLD), srgb(INK), line=0.16)
    for side in (1, -1):
        p, n = on_ell(H, HR, (0.5 * side, -0.84, 0.0))
        col = look if side == 1 else (lambda a, b, g=look: g(-a, b))
        k.decal(p, n, (0.025, 0.027), col, eps=0.0034, rings=20, segs=56)


def magearna_mega(src, out, original=False):
    global SHIELD
    from sculptkit import polygon
    SHIELD = polygon([(-0.8, 0.85), (0.8, 0.85), (0.82, 0.05), (0.0, -0.95), (-0.82, 0.05)])
    lower = "#F0F0F2" if original else "#3A3C44"
    lid = "#F4F4F6" if original else "#46484F"
    ring = "#D2A43A" if original else "#D22C40"
    cog = "#E4E6EC" if original else "#4A4C54"
    k = Kit(src, coat="metal")
    add_gown(k, lower, lid)
    add_chest(k, ring)
    add_head(k, cog)
    k.build(voxel=0.006, smooth=4, band=0.005, wband=0.04)
    V = np.array([v.co[:] for v in k.ob.data.vertices])
    a = math.radians(31.5)
    proj = V[:, 0] * math.cos(a) + V[:, 1] * math.sin(a)
    h, w, l = V[:, 2].max(), np.ptp(V[:, 0]), np.ptp(V[:, 1])
    sq = 1.3 * max(h, 0.85 * max(w, l))
    print("KIT: board: shows %.3f in a square of %.3f (%.2f); the four-corner sum %.3f (%.2f)"
          % (np.ptp(proj), sq, np.ptp(proj) / sq, 0.853 * w + 0.522 * l, (0.853 * w + 0.522 * l) / sq))
    k.eye_states(lambda s: eyes(k, s))
    k.export(out)


if __name__ == "__main__":
    magearna_mega(*args())
