# Alolan Graveler sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/graveleralola.py -- <the export's Graveler-Alola.glb> <out.glb>
# Graveler's frame (graveler.py, which builds as it is read, so its way is copied here; the skeleton is the same):
# the boulder ringed round its face with squared teeth like a cog, four arms, stubby legs; grown from Geodude-Alola
# (geodudealola.py, through sibling) in its pale grey-green stone, its two black brows meeting in a V, black rods of
# iron sand along the tops of its upper arms, amber crystals set in the rock, and great blunt claws for hands.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, inside, smoothstep, unit, boxy, facets, eye_look, args, sibling

g = sibling("geodudealola")

GREY, DARK, CRACK, UNDER = "#ADB4AD", "#8E958F", "#6E746F", "#8E9496"
C = np.array([0.0, 0.0, 0.4])            # the boulder's middle, on the body bone
R = np.array([0.27, 0.235, 0.25])        # its radii


def shade(h, f):
    """A colour a little lighter or darker (f above or below 1), still written as on screen."""
    c = np.clip(np.array([int(h[i:i + 2], 16) for i in (1, 3, 5)]) * f, 0, 255).astype(int)
    return "#%02X%02X%02X" % tuple(c)


def stone(n, seed, depth, jitter=0.2):
    """A deform cutting a sphere into rock: n planes spread round it, each at a depth of its own (graveler.py)."""
    rs = np.random.RandomState(seed)
    i = np.arange(n) + 0.5
    ph = np.arccos(1 - 2 * i / n); th = math.pi * (1 + 5 ** 0.5) * i
    N = np.stack([np.cos(th) * np.sin(ph), np.sin(th) * np.sin(ph), np.cos(ph)], 1) + rs.normal(size=(n, 3)) * jitter
    N /= np.linalg.norm(N, axis=1, keepdims=True)
    q, _ = np.linalg.qr(rs.normal(size=(3, 3)))
    N = N @ q
    d = depth[0] + (depth[1] - depth[0]) * rs.rand(n)
    def f(V):
        return V / np.maximum((V @ N.T / d[None]).max(1), 0.3)[:, None]
    return f


def on_body(d, lift=0.0):
    """The point of the boulder's ellipsoid along direction d from its middle, lifted along d."""
    d = unit(*d)
    t = 1 / np.sqrt(((d / R) ** 2).sum())
    return C + d * (t + lift)


def graveler_alola(src, dest):
    k = Kit(src, coat="shell")

    def boulder(name, at, r, seed, bone, tone=1.0, mirror=False, planes=9, rot=(0, 0, 0)):
        V, _ = ell(at, r, rot=rot, deform=stone(planes, seed, (0.68, 0.82)), sub=3)
        k.add(name, hull(V), shade(GREY, tone), bone, fuse=False, mirror=mirror, surface="shell")

    # The boulder
    k.add("body", ell(C, R, deform=facets(16, 1.0, 75, keep_front=0.5, depth=(0.82, 0.94))), GREY, "body")

    # The ring of squared rock teeth round the face, like a cog (graveler.py)
    F = unit(0, -1, 0.32)
    A = unit(1, 0, 0); Bv = np.cross(F, A)
    for i, ang in enumerate(np.linspace(-35, 215, 9)):
        a = math.radians(ang)
        d = unit(*(F * math.cos(math.radians(43)) + math.sin(math.radians(43)) * (A * math.cos(a) + Bv * math.sin(a))))
        p = on_body(d, 0.004)
        along = unit(*(-A * math.sin(a) + Bv * math.cos(a)))
        side = np.cross(d, along)
        M = np.stack([along, side, d], axis=1)
        V, Fc = ell((0, 0, 0), (0.048, 0.038, 0.05), deform=boxy(6.0), sub=4)
        k.add(f"tooth{i}", (V @ M.T + p, Fc), GREY, "body")

    # the stone the brows sit on, and the cheeks under the eyes and round the mouth
    k.add("ridge", ell((0.07, -0.222, 0.475), (0.08, 0.036, 0.026), rot=(-14, -16, 9)), GREY, "body", mirror=True)
    k.add("cheek", ell((0.105, -0.2, 0.37), (0.07, 0.045, 0.06), deform=facets(8, 0.5, 11)), GREY, "body", mirror=True)

    # Rocks over its back, top and sides, as on Graveler but fewer: the Alolan stone is smoother
    rs = np.random.RandomState(7)
    dirs = [(0.3, 0.6, 0.75), (-0.35, 0.55, 0.75), (0.0, 0.9, 0.4), (0.7, 0.55, 0.3), (-0.7, 0.5, 0.35),
            (0.55, 0.8, -0.1), (-0.5, 0.85, -0.15), (0.0, 0.95, -0.3), (0.9, 0.2, -0.2), (-0.9, 0.25, -0.15),
            (0.15, 0.4, 0.9), (-0.2, 0.25, 0.95)]
    for i, d in enumerate(dirs):
        r = 0.05 + 0.025 * rs.rand()
        boulder(f"lump{i}", on_body(d, -0.014), (r, r * 0.85, r * 0.8), 20 + i, "body", tone=0.94 + 0.1 * rs.rand(),
                rot=tuple((rs.rand(3) * 90 - 45).tolist()))

    # The upper arms out and down to great blunt claws, iron sand bristling along their tops
    SH = np.array([0.2, -0.04, 0.48]); EL = np.array([0.36, -0.12, 0.46]); WR = np.array([0.46, -0.2, 0.35])
    k.add("arm", tube([SH, EL, WR], [0.058, 0.058, 0.05]), GREY, "arm{s}", mirror=True)
    for j, (t, r) in enumerate([(0.3, 0.058), (0.85, 0.056)]):
        p = SH + (EL - SH) * min(t * 2, 1) if t < 0.5 else EL + (WR - EL) * (t - 0.5) * 2
        boulder(f"armrock{j}", p + np.array([0.0, 0.012, -0.012]), (r, r * 0.9, r * 0.85), 50 + j, "arm{s}",
                mirror=True, tone=0.96, rot=(15 * j, 30 * j, 10))
    HC = np.array([0.5, -0.235, 0.28])
    k.add("palm", ell(HC, (0.075, 0.068, 0.06), rot=(0, 20, 0), deform=boxy(3.2)), GREY, "arm{s}", mirror=True)
    # two thick blunt fingers hooking forward and down, and a thumb hooked in under them: a pincer
    for j, (a, b, rr) in enumerate([((0.03, -0.045, -0.03), (0.035, -0.12, -0.1), 0.038),
                                    ((-0.025, -0.05, -0.035), (-0.03, -0.125, -0.105), 0.038),
                                    ((-0.055, -0.01, -0.04), (-0.06, -0.05, -0.12), 0.03)]):
        p0 = HC + np.array(a); p1 = HC + np.array(b)
        k.add(f"finger{j}", tube([HC + np.array(a) * 0.3, p0, p1], [rr, rr, rr * 0.8]), GREY, "arm{s}", mirror=True)
    # the iron sand: black rods standing up off the forearm's top, leaning back toward the shoulder
    rods = []
    # a row along the arm from just inside the elbow to near the wrist, each rod its own, fanning a little
    for j, t in enumerate(np.linspace(-0.45, 0.7, 6)):
        p = EL + (EL - SH) * t if t < 0 else EL + (WR - EL) * t
        lift = 0.05 if t < 0 else 0.05 - 0.012 * t
        root = p + np.array([0.0, 0.008 * (1 if j % 2 else -1), lift])
        d = unit(0.2 + 0.25 * t, 0.18 * (1 if j % 2 else -1) + 0.1, 1.0)
        rods.append((root, d, 0.07 + 0.016 * ((j * 7) % 3), 0.026, 0.4 * j))
    g.iron_sand(k, "sand", rods, "arm{s}", mirror=True)

    # The lower arms: short, curling round to the front of the belly, two fists meeting under the mouth
    LS = np.array([0.2, -0.1, 0.3]); LE = np.array([0.18, -0.22, 0.25]); LF = np.array([0.08, -0.26, 0.26])
    k.add("lowarm", tube([LS, LE, LF + np.array([0.02, 0.02, 0])], [0.046, 0.043, 0.04]), GREY, "lowArm{s}",
          mirror=True)
    boulder("lowrock", LE + np.array([0.01, -0.01, 0.01]), (0.045, 0.04, 0.04), 70, "lowArm{s}", mirror=True)
    boulder("lowfist", LF, (0.05, 0.042, 0.045), 72, "lowArm{s}", mirror=True, planes=10, rot=(0, 0, 20))

    # Stubby legs on flat blocky feet
    k.add("leg", tube([(0.12, 0.0, 0.2), (0.13, -0.01, 0.1), (0.13, -0.02, 0.08)], [0.085, 0.08, 0.072]), GREY,
          "leg{s}", mirror=True)
    k.add("foot", ell((0.135, -0.04, 0.05), (0.09, 0.1, 0.045), deform=boxy(3.0)), GREY, "leg{s}", mirror=True)
    for j, x in enumerate((0.1, 0.17)):
        boulder(f"toe{j}", (x, -0.12, 0.045), (0.028, 0.03, 0.026), 80 + j, "leg{s}", mirror=True, planes=12)

    # Amber crystals set in the rock: on the body round the face and over the back, and one on each upper arm
    gems = []
    for d, r in [((0.78, -0.4, 0.5), 0.03), ((-0.62, -0.55, 0.6), 0.026), ((0.6, -0.62, -0.42), 0.028),
                 ((-0.55, -0.62, -0.5), 0.025), ((0.3, 0.45, 0.85), 0.03), ((-0.65, 0.55, 0.4), 0.028),
                 ((0.85, 0.4, -0.15), 0.026), ((0.0, 0.92, 0.1), 0.03), ((-0.9, -0.1, 0.05), 0.026)]:
        n = unit(*d)
        gems.append((on_body(d, -0.004), n, r, 0.016, 0.3))
    g.crystals(k, "gem", gems, "body")
    g.crystals(k, "armgem", [(EL + np.array([0.0, -0.05, 0.02]), unit(0.1, -1, 0.4), 0.026, 0.014, 0.2)], "arm{s}",
               mirror=True)

    # The underside darker where the boulder curves in under itself
    k.paint(lambda P: smoothstep(0.27, 0.19, P[:, 2]) * inside(np.linalg.norm(P[:, :2], axis=1) - 0.3, 0.01),
            UNDER, on=["body", "cheek"])
    # The shade under the brows
    k.paint(lambda P: inside(np.abs(P[:, 2] - 0.452) - 0.012, 0.01) * smoothstep(-0.17, -0.21, P[:, 1]) *
            inside(np.abs(P[:, 0]) - 0.16), DARK, on=["body", "ridge"])

    # The two black brows of iron sand meeting in a V over the nose
    for s, nm in ((1, "browR"), (-1, "browL")):
        g.brow_block(k, nm, (0.008 * s, -0.246, 0.452), (0.135 * s, -0.205, 0.5), 0.05, 0.06,
                     (0.32 * s, -0.94, 0.1), "body")

    # The mouth: a wide hollow gaping open under the eyes, pink within
    def gape(V):
        V = V.copy()
        V[:, 2] -= 0.25 * V[:, 0] ** 2
        return V
    k.add("mouth", ell((0, -0.245, 0.36), (0.085, 0.06, 0.03), rot=(-12, 0, 0), deform=gape), "#E27884", "body",
          carve=True)

    # Eyes under the brows: Geodude-Alola's
    G = srgb(GREY)
    look = g.eye_open(G)
    de = unit(0.4, -0.91, 0.08)
    E = np.array([0.074, -0.215, 0.436])
    def eyes(state):
        k.decal(E, de, (0.038, 0.029), eye_look(state, look, G, srgb("#2A2C34")), eps=0.004, rings=20, segs=56,
                mirror=True)

    k.build(voxel=0.008, smooth=1)
    k.eye_states(eyes)
    k.export(dest)


if __name__ == "__main__":
    src_, dest_ = args()
    graveler_alola(src_, dest_)
