# Hatzlet (Hațeglet, Carpathia No. 10141) sculpted from nothing on the skeleton and clips of the game's own export:
#   blender --background --factory-startup --python sculpts/hatzlet.py -- <the export's Hatzlet.glb> <out.glb>
# From its picture: a young pterosaur standing on all fours, sandy brown: a big head on a short thick neck, a long
# pale toothless beak pointing forward and down with a mouth line running back from it, a lump of pale stone for a
# crest on the back of the skull, a large round eye with a dark olive iris; pale stone patches on the head, shoulders
# and back; thick wing-arms it walks on, down to clawed hands, the folded wing a dark brown blade rising behind each
# arm to a point over the hips; short hind legs with clawed feet; a small pointed tail.
# Written to be grown from: Hatzegon is the same frame scaled by its skeleton (1.4) with what the picture changes
# (pterosaur(src, out, big=True), through sibling("hatzlet")).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, slab, blade, outward, srgb, inside, smoothstep, unit, eye_look, \
    polygon, args

# --- the line's colours (the chick's; the giant's are darker, ochre) ----------------------------------------------
CHICK = dict(SAND="#CBA373", SHADE="#B38A5C", WING="#9B7450", STONE="#DCD3C0", BEAK="#D3C8B1",
             IRIS="#7E6A45", CLAW="#4E4034", MOUTH="#6B5240")
GIANT = dict(SAND="#C69855", SHADE="#A77A42", WING="#7C5434", STONE="#CEC3A6", BEAK="#CDBFA2",
             IRIS="#E4D9A6", CLAW="#3E3228", MOUTH="#5E432C")
INK, GLINT = "#16110D", "#FFFFFF"


def claw(base, d, length, r, up=(0, 0, 1)):
    d = unit(*d); up = np.array(up, float)
    a = np.cross(d, up)
    if np.linalg.norm(a) < 1e-3:
        a = np.cross(d, (1, 0, 0))
    a /= np.linalg.norm(a); b = np.cross(a, d)
    base = np.array(base, float)
    return hull([base + a * r, base - a * r, base + b * r, base - b * r, base + d * length])


def plate(pts, thick, hint=None):
    """A flat plate with a hard rim through three or more points, its face toward hint."""
    P = np.array(pts, float)
    o = P.mean(axis=0)
    ex = P[1] - P[0]; ex /= np.linalg.norm(ex)
    n = np.cross(P[1] - P[0], P[2] - P[0]); n /= np.linalg.norm(n)
    if hint is not None and n @ np.array(hint, float) < 0:
        n = -n
    ez = np.cross(n, ex)
    return slab([((p - o) @ ex, (p - o) @ ez) for p in P], o, ex, ez, thick)


def rock(c, r, seed, n=14, rough=0.25):
    """A lump of stone: a hull of jittered points on an ellipsoid."""
    rs = np.random.RandomState(seed)
    D = rs.normal(size=(n, 3)); D /= np.linalg.norm(D, axis=1, keepdims=True)
    D *= (1 - rough + rough * rs.rand(n))[:, None]
    return hull(np.array(c, float) + D * np.array(r, float))


def shard(base, d, length, r, seed, flat=0.55):
    """A shard of stone: a flat-faced blade, irregular at its root, from base along d."""
    rs = np.random.RandomState(seed)
    d = unit(*d)
    a = np.cross(d, (0, 0, 1.0))
    if np.linalg.norm(a) < 1e-3:
        a = np.array([1.0, 0, 0])
    a /= np.linalg.norm(a); b = np.cross(a, d)
    base = np.array(base, float)
    pts = []
    for t in range(6):
        th = 2 * math.pi * t / 6 + rs.rand() * 0.4
        pts.append(base + (a * math.cos(th) + b * math.sin(th) * flat) * r * (0.8 + 0.3 * rs.rand()))
    mid = base + d * length * 0.55
    for t in range(4):
        th = 2 * math.pi * t / 4 + 0.5
        pts.append(mid + (a * math.cos(th) + b * math.sin(th) * flat) * r * 0.55)
    pts.append(base + d * length + a * r * 0.15 * (rs.rand() - 0.5))
    return hull(pts)


def pebble(c, n, r, seed):
    """A flat stone set into the skin: an irregular low hull round c, its face along n."""
    rs = np.random.RandomState(seed)
    n = unit(*n)
    a = np.cross(n, (0, 0, 1.0))
    if np.linalg.norm(a) < 1e-3:
        a = np.array([1.0, 0, 0])
    a /= np.linalg.norm(a); b = np.cross(n, a)
    c = np.array(c, float) - n * r * 0.15
    pts = []
    for t in range(8):
        th = 2 * math.pi * t / 8 + rs.rand() * 0.5
        q = (a * math.cos(th) + b * math.sin(th)) * r * (0.65 + 0.4 * rs.rand())
        pts += [c + q - n * r * 0.3, c + q * 0.75 + n * r * 0.32]
    return hull(pts)


def on_ell(c, r, d):
    d = np.array(d, float); r = np.array(r, float)
    return np.array(c, float) + d / np.linalg.norm(d / r)


def blob(seed, n=9, rough=0.35):
    """An irregular outline for a stone patch, star-shaped about its middle."""
    rs = np.random.RandomState(seed)
    th = np.sort(rs.rand(n)) * 2 * math.pi
    rr = 1 - rough * rs.rand(n)
    return polygon(list(zip(rr * np.cos(th), rr * np.sin(th))))


def round_eye(iris, rim=0.86):
    I, K, G = srgb(iris), srgb(INK), srgb(GLINT)
    def look(u, v):
        c = np.tile(I, (len(u), 1))
        rr = u * u + v * v
        c[rr > rim * rim] = K
        c[(u / 0.4) ** 2 + (v / 0.46) ** 2 < 1] = K
        c[((u - 0.3) / 0.22) ** 2 + ((v - 0.34) / 0.22) ** 2 < 1] = G
        return c
    return look


def pterosaur(src, out, big=False):
    C = GIANT if big else CHICK
    K = 1.4 if big else 1.0
    HS = 1.15 if big else 1.0                 # the giant's head is heavier for its size
    NECK = np.array([0.0, -0.12, 0.5])

    def P(*p):
        return np.array(p, float) * K

    def Hd(*p):
        """A point of the head: scaled about the neck for the giant, then by the skeleton."""
        return (NECK + (np.array(p, float) - NECK) * HS) * K

    k = Kit(src, coat="scales")
    SAND, SHADE, WING, STONE = C["SAND"], C["SHADE"], C["WING"], C["STONE"]

    # The body: a short trunk, the chest raised and full where the wing-arms meet it
    TC, TR = P(0, 0.03, 0.335), np.array([0.118 if not big else 0.13, 0.165, 0.13]) * K
    CC, CR = P(0, -0.08, 0.42), np.array([0.108 if not big else 0.125, 0.105, 0.105]) * K
    k.add("torso", ell(TC, TR), SAND, "body")
    k.add("chest", ell(CC, CR), SAND, "body")
    if big:
        for s in (1, -1):
            k.add(f"pec{'R' if s > 0 else 'L'}", ell(P(0.055 * s, -0.135, 0.4), np.array([0.06, 0.05, 0.065]) * K),
                  SAND, "body")
    nr = 0.085 if big else 0.068
    k.add("neck", tube([P(0, -0.09, 0.45), P(0, -0.14, 0.54), Hd(0, -0.165, 0.6)],
                       [nr * K, (nr - 0.006) * K, (nr - 0.006) * K * HS]), SAND, "head")

    # The head: a round skull, the cheeks full under it
    HC, HR = Hd(0, -0.175, 0.625), np.array([0.095, 0.108, 0.09]) * K * HS
    k.add("skull", ell(HC, HR), SAND, "head")
    k.add("cheek", ell(Hd(0, -0.2, 0.592), np.array([0.08, 0.09, 0.06]) * K * HS), SAND, "head")

    # The beak: the chick's long and pointed, forward and down; the giant's deep, shorter, hooked at the tip
    if big:
        bp = [Hd(0, -0.24, 0.615), Hd(0, -0.33, 0.588), Hd(0, -0.41, 0.56), Hd(0, -0.455, 0.522),
              Hd(0, -0.465, 0.48)]
        br = np.array([0.088, 0.075, 0.056, 0.036, 0.016]) * K * HS
        k.add("beak", outward(blade(bp, br, (1, 0, 0), 0.62)), C["BEAK"], "head", fuse=False, surface="shell")
    else:
        bp = [Hd(0, -0.24, 0.617), Hd(0, -0.35, 0.578), Hd(0, -0.45, 0.533), Hd(0, -0.525, 0.497)]
        br = np.array([0.058, 0.043, 0.027, 0.012]) * K * HS
        k.add("beak", outward(blade(bp, br, (1, 0, 0), 0.78)), C["BEAK"], "head", fuse=False, surface="shell")

    # The crest: a lump of pale stone on the chick's skull, a crown of shards on the giant's
    if big:
        crown = [((0.0, -0.17, 0.705), (0, 0.45, 1), 0.15, 0.04), ((0.04, -0.14, 0.7), (0.35, 0.55, 1), 0.13, 0.035),
                 ((-0.04, -0.14, 0.7), (-0.35, 0.55, 1), 0.13, 0.035), ((0.02, -0.21, 0.695), (0.2, 0.1, 1), 0.11, 0.033),
                 ((-0.025, -0.205, 0.695), (-0.25, 0.15, 1), 0.1, 0.03), ((0.0, -0.1, 0.69), (0, 0.9, 0.8), 0.12, 0.035),
                 ((0.055, -0.1, 0.675), (0.6, 0.8, 0.6), 0.09, 0.03), ((-0.055, -0.1, 0.675), (-0.6, 0.8, 0.6), 0.09, 0.03)]
        for j, (b, d, L_, r) in enumerate(crown):
            k.add(f"crown{j}", shard(Hd(*b), d, L_ * 0.92 * K * HS, r * 1.6 * K * HS, 20 + j, flat=0.7), STONE, "head", fuse=False,
                  surface="shell")
        back = [((0.07, -0.03, 0.47), (0.35, 0.4, 1), 0.12, 0.035), ((-0.07, -0.03, 0.47), (-0.35, 0.4, 1), 0.12, 0.035),
                ((0.0, 0.02, 0.47), (0, 0.55, 1), 0.13, 0.04), ((0.1, 0.05, 0.43), (0.6, 0.5, 0.8), 0.09, 0.03),
                ((-0.1, 0.05, 0.43), (-0.6, 0.5, 0.8), 0.09, 0.03), ((0.04, 0.1, 0.46), (0.3, 0.8, 0.8), 0.09, 0.03),
                ((-0.04, 0.1, 0.46), (-0.3, 0.8, 0.8), 0.09, 0.03)]
        for j, (b, d, L_, r) in enumerate(back):
            k.add(f"plate{j}", shard(P(*b), d, L_ * K, r * 1.6 * K, 40 + j, flat=0.7), STONE, "body", fuse=False, surface="shell")
    else:
        for j, (c, r) in enumerate([((0.0, -0.15, 0.71), (0.055, 0.065, 0.045)), ((0.015, -0.12, 0.745), (0.04, 0.042, 0.035)),
                                    ((-0.02, -0.19, 0.725), (0.035, 0.035, 0.03))]):
            k.add(f"crest{j}", rock(Hd(*c), np.array(r) * K, 5 + j), STONE, "head", fuse=False, surface="shell")
        for j, (b, d, L_, r) in enumerate([((0.035, 0.0, 0.465), (0.3, 0.3, 1), 0.04, 0.022),
                                           ((-0.03, 0.04, 0.46), (-0.3, 0.4, 1), 0.035, 0.02)]):
            k.add(f"nub{j}", rock(P(*b) + np.array(d) * 0.01, np.array([r, r, L_ * 0.6]) * K, 9 + j), STONE,
                  "body", fuse=False, surface="shell")

    # The wing-arms walked on: thick, down to clawed hands; the folded wing a blade rising behind to a point
    SH, EL, WR = P(0.105, -0.09, 0.42), P(0.19, -0.04, 0.27), P(0.205, -0.175, 0.07)
    HAND = P(0.205, -0.19, 0.028)
    TIP = P(0.26, 0.4, 0.49) if not big else P(0.265, 0.43, 0.55)
    ar = 1.12 if big else 1.0
    k.add("upperarm", tube([SH, EL], [0.056 * K * ar, 0.044 * K * ar]), SAND, "leg{s}", mirror=True)
    k.add("forearm", tube([EL, WR, HAND + P(0, 0, 0.022)], [0.044 * K * ar, 0.036 * K * ar, 0.032 * K * ar]),
          SAND, "leg{s}", mirror=True)
    if big:
        k.add("biceps", ell(P(0.15, -0.075, 0.35), np.array([0.05, 0.05, 0.075]) * K), SAND, "leg{s}", mirror=True)
    k.add("hand", ell(HAND, np.array([0.042, 0.05, 0.026]) * K * ar), SAND, "leg{s}", mirror=True)
    for j, dx in enumerate((-0.026, 0.0, 0.026)):
        k.add(f"hclaw{j}", claw(HAND + P(dx, -0.042, -0.006), (dx * 12, -1, -0.5), 0.032 * K, 0.0115 * K), C["CLAW"],
              "leg{s}", fuse=False, mirror=True, surface="shell")
    k.add("wing", plate([EL + P(0.01, 0.02, 0.0), WR + P(0.012, 0.05, 0.03), TIP], 0.026 * K, hint=(1, 0, 0.2)),
          WING, "leg{s}", fuse=False, mirror=True, surface="leather")
    k.add("finger", outward(tube([WR + P(0.012, 0.05, 0.03), (WR + TIP) / 2 + P(0.012, 0, 0), TIP],
                                 [0.022 * K, 0.018 * K, 0.012 * K], seg=12, per=6)),
          C["SHADE"], "leg{s}", fuse=False, mirror=True, surface="leather")
    k.add("underwing", plate([SH + P(0.0, 0.04, -0.05), EL + P(-0.01, 0.01, 0), P(0.1, 0.13, 0.22)], 0.02 * K,
                             hint=(1, 0, 0)), WING, "leg{s}", fuse=False, mirror=True, surface="leather")

    # The hind legs: short, thighs fused into the flanks, clawed feet
    k.add("thigh", ell(P(0.085, 0.09, 0.27), np.array([0.062, 0.078, 0.088]) * K), SAND, "hind{s}", mirror=True)
    k.add("shin", tube([P(0.1, 0.1, 0.2), P(0.112, 0.08, 0.05)], [0.039 * K, 0.033 * K]), SAND, "hind{s}",
          mirror=True)
    k.add("foot", ell(P(0.112, 0.045, 0.024), np.array([0.036, 0.052, 0.022]) * K), SAND, "hind{s}", mirror=True)
    for j, dx in enumerate((-0.022, 0.0, 0.022)):
        k.add(f"toe{j}", claw(P(0.112 + dx, 0.005, 0.02), (dx * 12, -1, -0.45), 0.028 * K, 0.0105 * K), C["CLAW"],
              "hind{s}", fuse=False, mirror=True, surface="shell")

    # The tail: a short point back and out to +X (the giant's longer)
    tl = [P(0, 0.15, 0.35), P(0.04, 0.27, 0.39), P(0.09, 0.35, 0.42)] if not big else \
        [P(0, 0.15, 0.35), P(0.04, 0.28, 0.37), P(0.09, 0.38, 0.38), P(0.14, 0.44, 0.38)]
    k.add("tail", tube(tl, (np.array([0.042, 0.026, 0.013]) if not big else np.array([0.05, 0.036, 0.024, 0.013])) * K),
          SAND, "tail")

    # The underside a little darker
    k.paint(lambda Q: smoothstep(TC[2] - 0.02 * K, TC[2] - 0.11 * K, Q[:, 2]) * inside(np.abs(Q[:, 0]) - 0.1 * K, 0.01)
            * inside(Q[:, 2] - 0.3 * K, 0.01), SHADE, on=["torso", "chest"])

    # The mouth line running back from the beak along the cheek
    md = unit(0.75, -0.55, -0.25)
    mc = on_ell(Hd(0, -0.2, 0.592), np.array([0.08, 0.09, 0.06]) * K * HS, md)
    k.decal(mc, md, (0.045 * K * HS, 0.006 * K * HS), C["MOUTH"], eps=0.003, rings=8, segs=40, mirror=True)

    # Stone patches, pale and irregular, on the head, the shoulders and the back (the far side and the back too)
    spots_head = [((0.62, -0.25, 0.55), 0.02), ((0.2, -0.55, 0.8), 0.014), ((0.85, 0.22, 0.42), 0.038),
                  ((0.55, 0.35, 0.75), 0.018)]
    for j, (d, r) in enumerate(spots_head):
        dd = unit(*d)
        k.decal(on_ell(HC, HR, dd), dd, (r * K * HS, r * 0.8 * K * HS), STONE, outline=blob(60 + j), eps=0.003,
                rings=8, segs=36, mirror=True)
    spots_body = [((0.7, -0.35, 0.6), 0.03, CC, CR), ((0.35, -0.1, 1.0), 0.032, TC, TR), ((-0.4, 0.25, 0.9), 0.028, TC, TR),
                  ((0.6, 0.3, 0.75), 0.024, TC, TR), ((0.1, 0.55, 0.85), 0.026, TC, TR), ((-0.7, -0.1, 0.7), 0.022, TC, TR),
                  ((0.85, 0.05, 0.35), 0.02, TC, TR), ((0.3, -0.75, 0.5), 0.018, CC, CR),
                  ((-0.25, 0.6, 0.75), 0.022, TC, TR), ((-0.75, 0.3, 0.55), 0.02, TC, TR), ((0.0, 0.85, 0.5), 0.018, TC, TR)]
    for j, (d, r, c, rr) in enumerate(spots_body):
        dd = unit(*d)
        k.add(f"stone{j}", pebble(on_ell(c, rr, dd), dd, r * 1.35 * K, 80 + j), STONE, "body", fuse=False, surface="shell")

    k.build(voxel=0.0065 * K, smooth=4, band=0.006 * K, wband=0.05 * K)

    # The eyes: the chick's large and round, olive with a black pupil; the giant's narrow and pale under a heavy
    # brow that cuts their top on a slope toward the beak
    LID, INKC = srgb(SAND), srgb(INK)
    ED = unit(0.8, -0.38, 0.32)
    E = on_ell(HC, HR, ED)
    if not big:
        def eyes(state):
            k.decal(E, ED, (0.03, 0.03), eye_look(state, round_eye(C["IRIS"]), LID, INKC, line=0.17), rings=20,
                    segs=56, eps=0.004, mirror=True)
    else:
        # +u runs toward the back on these sideways patches: the brow's line rises from the beak to the back
        EYE = [(-1.0, -0.15), (-0.75, -0.05), (0.0, 0.32), (1.0, 0.62), (1.0, -0.35), (0.3, -0.75), (-0.6, -0.55)]
        reach = polygon(EYE)
        I, G = srgb(C["IRIS"]), srgb(GLINT)
        def eye(u, v):
            c = np.tile(INKC, (len(u), 1))
            r = np.sqrt(u * u + v * v) / np.array([reach(t) for t in np.arctan2(v, u)])
            c[r < 0.8] = I
            c[((u + 0.02) / 0.26) ** 2 + ((v + 0.05) / 0.42) ** 2 < 1] = INKC
            c[((u - 0.22) / 0.13) ** 2 + ((v - 0.12) / 0.13) ** 2 < 1] = G
            return c
        def eyes(state):
            look = eye_look(state, eye, LID, INKC, line=0.2)
            half = (0.034 * K * HS, 0.024 * K * HS)
            k.decal(E, ED, half, look, eps=0.004, rings=20, segs=56, outline=reach)
            m = np.array([-1, 1, 1.0])
            k.decal(E * m, ED * m, half, lambda u, v: look(-u, v), eps=0.004, rings=20, segs=56,
                    outline=lambda t: reach(math.pi - t))
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    pterosaur(*args())
