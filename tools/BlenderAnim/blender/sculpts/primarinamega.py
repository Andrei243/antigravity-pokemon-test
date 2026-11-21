# Mega Primarina (a Mega of our own, which no game has) sculpted from nothing on the skeleton and clips of the game's
# own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/primarinamega.py -- <the export's Primarina-Mega.glb> <out.glb>
#
# No pictures: the design is the game's own sculptor (PokemonModels.Ours.Megas.cs, PrimarinaMega) and the look of its
# export. Primarina's body as primarina.py builds it (its head, muzzle, eyes, nose, flippers, ruff, skirt, cap and tail
# are copied from that script, which builds them in one function), with what the Mega changes:
# - her hair grown into a long train falling to the ground behind her like a gown: two broad teal masses from the
#   crown rising behind the head and falling back to the ground, a pink strand down the outer side of each, strung
#   with pale pearls (the largest new mass, replacing the one mass of hair Primarina carries to +X);
# - a tiara of pink coral where her star was (the star goes): five branching spikes on the cap's front, pearls along
#   the brow under them;
# - her tail's fluke spread into a wide fan of five blue lobes with pale tips;
# - a ring of floating spheres of water round her, lit pale, on a thread held to her by two strands.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, lathe, slab, srgb, inside, smoothstep, unit, lumpy, outward, args, sibling

p = sibling("popplio")
pr = sibling("primarina")

WHITE, BLUE, HAIR, FRILL, PINK, PEARL = pr.WHITE, pr.BLUE, pr.HAIR, pr.FRILL, pr.PINK, pr.PEARL
STRIPE, EYEBLUE, EYEDEEP = pr.STRIPE, pr.EYEBLUE, pr.EYEDEEP
CORAL, STRAND, WATER, FANTIP = "#F0829E", "#EE88BE", "#D2F0FC", "#C8E2FA"

H, HR, MZ, TAIL, TRAD = pr.H, pr.HR, pr.MZ, pr.TAIL, pr.TRAD


def join(shapes):
    V, F, off = [], [], 0
    for v, f in shapes:
        V.append(np.asarray(v)); F.extend([[i + off for i in q] for q in f]); off += len(v)
    return np.vstack(V), F


def train_path(s):
    """The train of hair on side s: from the crown up behind the head and falling back to the ground."""
    return [np.array(q, float) * (s, 1, 1) for q in
            [(0.025, 0.012, 0.372), (0.058, 0.07, 0.392), (0.088, 0.14, 0.352), (0.104, 0.215, 0.258),
             (0.112, 0.295, 0.15), (0.112, 0.37, 0.07), (0.104, 0.445, 0.036)]]


TRAIN_R = [0.03, 0.038, 0.043, 0.045, 0.045, 0.041, 0.034]


def nearest_on(path, P):
    """The nearest point of a polyline (densely sampled) to each of the points P, and its index's share along it."""
    path = np.array(path)
    dense = np.vstack([path[i] * (1 - t) + path[i + 1] * t for i in range(len(path) - 1)
                       for t in np.linspace(0, 1, 12, endpoint=False)] + [path[-1:]])
    d = np.linalg.norm(P[:, None, :] - dense[None, :, :], axis=2)
    j = d.argmin(axis=1)
    return dense[j], j / (len(dense) - 1)


def primarina_mega(src, out):
    k = Kit(src, coat="scales")
    k.add("head", ell(H, HR), WHITE, "head")
    def taper(V):
        V = V.copy(); f = 1 - 0.55 * np.clip(-V[:, 1], 0, 1)
        V[:, 0] *= f; V[:, 2] *= f
        return V
    k.add("muzzle", ell(MZ, (0.022, 0.036, 0.019), rot=(6, 0, 0), deform=taper), WHITE, "head")
    k.add("neck", ell((0, -0.008, 0.272), (0.03, 0.03, 0.045)), WHITE, "body")
    k.add("torso", ell((0, 0.005, 0.18), (0.046, 0.042, 0.09), rot=(-8, 0, 0)), WHITE, "body")
    k.add("hips", ell((0, 0.025, 0.07), (0.058, 0.064, 0.064)), WHITE, "body")
    k.add("arm", tube([(0.034, -0.02, 0.225), (0.056, -0.05, 0.14), (0.064, -0.075, 0.05)], [0.02, 0.019, 0.019]),
          WHITE, "leg{s}", mirror=True)
    p.flipper(k, "paddle", (0.064, -0.075, 0.048), (0.16, -0.95, -0.26), (0.056, 0.029, 0.0105), WHITE, "leg{s}",
              roll=-8, stripes=STRIPE)
    # The tail as Primarina's, curling round her -X side to the front, blue above and white below
    k.add("tail", tube(TAIL, TRAD, seg=20, per=6), WHITE, "tail")
    def blue_top(P):
        return inside(0.046 - P[:, 2], 0.004) * inside(0.05 - np.hypot(P[:, 0], P[:, 1] - 0.02), 0.01)
    k.paint(blue_top, BLUE, on=["tail"])
    # The fluke spread into a wide fan of five blue lobes, lifted a little at their ends, tipped pale
    end = np.array(TAIL[-1], float)
    ax = unit(*(np.array(TAIL[-1]) - np.array(TAIL[-2])) * (1, 1, 0))
    base_a = math.atan2(ax[1], ax[0])
    for i in range(5):
        a = base_a + math.radians(-64 + 32 * i)
        d = unit(math.cos(a), math.sin(a), 0.22)
        k.add(f"fan{i}", p.flat(end + d * 0.05, d, (0.058, 0.026, 0.0095), roll=0), BLUE, "tail")
    k.paint(lambda P: inside(np.linalg.norm((P - end) * (1, 1, 0.6), axis=1) - 0.122, 0.004)
            * smoothstep(0.078, 0.086, np.linalg.norm((P - end) * (1, 1, 0.6), axis=1)), FANTIP, on=["fan"])
    # Pink spines along the tail's top, leaning back along it
    for t in (0.5, 1.4, 2.3, 3.2):
        i = int(t); f = t - i
        c = np.array(TAIL[i]) * (1 - f) + np.array(TAIL[i + 1]) * f
        r = TRAD[i] * (1 - f) + TRAD[i + 1] * f
        nxt = np.array(TAIL[i + 1]) - np.array(TAIL[i])
        d = unit(*(np.array([0, 0, 1.0]) * 1.0 - unit(*nxt) * 0.6 + unit(c[0], c[1] - 0.02, 0) * 0.4))
        k.add(f"spine{i}", lathe(c + np.array([0, 0, r * 0.75]), d,
                                 [(0, 0), (0.011, 0), (0.007, 0.016), (0, 0.032)], seg=16),
              PINK, "tail", fuse=False, surface="shell")
    p.collar(k, "ruff", 0.272, 0.024, 0.236, 0.066, 0.22, 7, FRILL, "body", 0.007, wave=0.16)
    p.collar(k, "skirt", 0.13, 0.04, 0.085, 0.088, 0.1, 8, FRILL, "body", 0.007, wave=0.18)
    # The frilled cap on the crown
    k.add("cap", ell(H + (0, 0.018, 0.034), (0.046, 0.04, 0.024), deform=lumpy(6, 2, 0.1)), FRILL, "head",
          fuse=False, surface="hair")

    # --- the train of hair: two broad masses from the crown, over the back of the head and down to the ground ----------
    pearls = {1: [], -1: []}
    for s in (1, -1):
        path = train_path(s)
        bone = "earR" if s > 0 else "earL"
        side = "R" if s > 0 else "L"
        k.add("train" + side, tube(path, TRAIN_R, seg=22, per=7), HAIR, bone)
        # a second fall beside it, toward the middle and back, so that the train is a broad band and no pipe
        inner = [q + np.array([-0.036 * s, 0.012, -0.004]) * w for q, w in zip(path[2:], (0.6, 1, 1, 1, 0.45))]
        k.add("trainin" + side, tube(inner, [r * 0.86 for r in TRAIN_R[2:]], seg=22, per=7), HAIR, bone)
        # the pink strand down its outer side: a ridge of its own colour, so its edges are clean
        o = unit(0.8 * s, 0.0, 0.6)
        spts = [q + o * (r * 0.84) for q, r in zip(path[1:], TRAIN_R[1:])]
        srad = [r * 0.42 for r in TRAIN_R[1:]]
        k.add("strand" + side, tube(spts, srad, seg=18, per=7), STRAND, bone)
        # pearls strung along the strand
        for t in (0.6, 1.8, 3.0, 4.2):
            i = int(t); f = t - i
            c = spts[i] * (1 - f) + spts[i + 1] * f
            r = srad[i] * (1 - f) + srad[i + 1] * f
            pearls[s].append(ell(c + o * (r * 0.85), (0.0088,) * 3, sub=3))

    # --- the tiara of coral on the cap's front, pearls along the brow ----------------------------------------------
    coral = []
    for i in range(-2, 3):
        root = np.array([0.016 * i, -0.008 - 0.004 * abs(i) * 0, 0.388 - 0.004 * abs(i)])
        d = unit(0.35 * i, -0.25, 1.0)
        L = 0.042 - 0.005 * abs(i)
        coral.append(lathe(root, d, [(0, -0.004), (0.0078, 0.0), (0.0055, L * 0.55), (0, L)], seg=14))
        if i != 0:
            b0 = root + d * L * 0.45
            bd = unit(0.9 * np.sign(i), -0.2, 0.7)
            coral.append(lathe(b0, bd, [(0, -0.003), (0.0062, 0.0), (0.0045, 0.012), (0, 0.022)], seg=12))
        else:
            for sx in (1, -1):
                b0 = root + d * L * 0.5
                coral.append(lathe(b0, unit(0.8 * sx, -0.2, 0.7), [(0, -0.003), (0.006, 0.0), (0.0045, 0.011),
                                                                    (0, 0.02)], seg=12))
    k.add("coral", join(coral), CORAL, "head", fuse=False, surface="shell")
    brow = []
    for i in range(-2, 3):
        d = unit(0.42 * i, -0.78, 0.5)
        brow.append(ell(p.at_surface(H, HR, d) + d * 0.003, (0.0068,) * 3, sub=3))
    k.add("browpearls", join(brow), PEARL, "head", fuse=False, surface="shell", glow="#FFFFFF")
    k.add("trainpearlsR", join(pearls[1]), PEARL, "earR", fuse=False, surface="shell", glow="#FFFFFF")
    k.add("trainpearlsL", join(pearls[-1]), PEARL, "earL", fuse=False, surface="shell", glow="#FFFFFF")

    # --- the ring of spheres of water round her, on a thread, held to her by two strands --------------------------
    RC, RR = np.array([0.0, 0.03, 0.215]), 0.232
    th = np.linspace(0, 2 * math.pi, 49)
    circle = [RC + RR * np.array([math.cos(a), math.sin(a), 0]) for a in th[:-1]]
    ring = [tube(circle + circle[:3], [0.0068] * (len(circle) + 3), seg=10, per=3)]
    for i in range(8):
        a = i * 2 * math.pi / 8 + 0.2
        ring.append(ell(RC + RR * np.array([math.cos(a), math.sin(a), 0]), (0.026,) * 3, sub=3))
    for s in (1, -1):
        ring.append(tube([RC + (RR * s, 0, 0), (0.12 * s, 0.03, 0.212), (0.04 * s, 0.02, 0.205)], [0.0068] * 3,
                         seg=10, per=4))
    k.add("waterring", outward(join(ring)), WATER, "body", fuse=False, surface="skin", glow="#BFE6F6")

    p.nose(k, MZ + np.array([0, -0.033, 0.003]), unit(0, -1, 0.15), 0.0085, PINK, "head")
    k.build(voxel=0.0036, smooth=4, band=0.003, wband=0.025)
    E = [(p.at_surface(H, HR, d), d, sx) for sx in (1, -1) for d in [unit(0.52 * sx, -0.82, 0.18)]]
    k.eye_states(lambda st: p.eyes(k, E, (0.0125, 0.0155), st, WHITE,
                                   p.popplio_eye(pupil=EYEBLUE, iris=EYEDEEP), eps=0.0016))
    k.export(out)


if __name__ == "__main__":
    primarina_mega(*args())
