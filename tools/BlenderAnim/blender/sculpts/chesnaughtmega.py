# Mega Chesnaught sculpted on the skeleton and clips of the game's own export of the form (sculptkit.py), grown from
# Chesnaught's script (chesnaught.py, imported: its torso and chest fur, its arms with the green sleeve and spikes, the
# way its face is painted and its eyes made):
#   blender --background --factory-startup --python sculpts/chesnaughtmega.py -- <the export's Chesnaught-Mega.glb> <out.glb>
# From the two pictures, the masses by size:
#   1. a great round shield on the right arm (+X), as wide as 0.6 of the figure is tall: a white dome with a cone in
#      its middle, a grey line round it, green spots near its rim, a great white spike up and out from its top rim;
#   2. the shell grown into a white hood over the head, spotted green, lined dark red round the face, running forward
#      into a long spike over the face's -X side and a short spike up behind;
#   3. a shaggy cape of dark red fur hanging behind the hips to the ground, green thorns along it;
#   4. the arms: the left forward with its green spiked sleeve; three yellow claws on each hand and foot;
#   5. green plates over the front of each thigh, with thorns; white legs with grey segment lines, set wide.
# The face: a gold mask with three points over the brow and the eyes, the eyes black slits in it, a black mark between
# them; a white beard of fur round the face's sides and below; a small frown.
# (The pictures overrule the game's sculptor: its dome on the back is the hood and the shield, the "skirt" at the hips
# is a cape behind, and the claws are yellow, not brown.)
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, args, axes, polygon, hull, lathe, sibling

ch = sibling("chesnaught")
cp = ch.cp
INK, FUR = ch.INK, ch.FUR
SHELL = "#F1EEE4"
BODY = "#E6E2D6"
GREEN = "#5F9438"
RED = "#8E1F2C"
CAPE = "#9A2633"
GOLD = "#E8B828"
YELLOW = "#EEC02E"
LINE = "#A6A094"
RIM = "#B8B2A4"
MOUTH = ch.MOUTH

# The base's own functions read these as they run
ch.CREAM = BODY
ch.LINE = LINE
ch.CLAW = YELLOW
ch.GREEN = GREEN

HEAD, HEADR = np.array([0.0, -0.14, 0.552]), np.array([0.086, 0.07, 0.08])
HOOD = np.array([0.0, 0.02, 0.588])
HOODR = np.array([0.16, 0.18, 0.145])
SH_C = np.array([0.31, -0.115, 0.44])               # the shield's middle
SH_N = unit(0.74, -0.64, 0.12)                       # the way its face looks
SH_R = 0.245


def spike(base, d, length, w):
    return ch.spike(base, d, length, w)


def hood(k):
    def taper(V):
        V = V.copy()
        t = np.clip(-V[:, 1], 0, 1)
        V[:, 2] *= 1 - 0.25 * t
        return V
    k.add("hood", ell(HOOD, HOODR, deform=taper), SHELL, "head", surface="shell")
    # the long spike forward over the face's -X side, and a short one up behind
    k.add("visor", spike((-0.09, -0.1, 0.68), (-0.55, -0.8, 0.05), 0.2, 0.045), SHELL, "head", fuse=False,
          surface="shell")
    k.add("crest", spike((0.0, 0.08, 0.7), (0.0, 0.55, 1.0), 0.09, 0.035), SHELL, "head", fuse=False, surface="shell")
    # the dark red lining round the face, and the green spots
    def ring(P):
        return np.hypot(P[:, 0] / 0.105, (P[:, 2] - 0.56) / 0.1)
    k.paint(lambda P: inside(P[:, 1] + 0.09, 0.003) * inside(ring(P) - 1.55, 0.01) * (P[:, 2] > 0.45), RED, on=["hood"])
    spots = [(0.11, 0.0, 0.69), (-0.12, 0.02, 0.68), (0.0, 0.14, 0.68), (0.13, 0.12, 0.6), (-0.13, 0.12, 0.6),
             (0.0, -0.08, 0.72), (0.08, 0.17, 0.55), (-0.08, 0.17, 0.55)]
    def spotmask(P):
        m = np.zeros(len(P))
        for s in spots:
            m = np.maximum(m, inside(np.linalg.norm(P - np.array(s), axis=1) - 0.034, 0.004))
        return m
    k.paint(spotmask, GREEN, on=["hood"])


def shield(k):
    n = SH_N
    prof = [(0.0, -0.035), (SH_R - 0.01, -0.035), (SH_R, -0.022), (SH_R, -0.005), (SH_R * 0.93, 0.03),
            (SH_R * 0.75, 0.06), (SH_R * 0.45, 0.083), (0.0, 0.092)]
    k.add("shield", lathe(SH_C, n, prof, seg=64), SHELL, "armR", fuse=False, surface="shell")
    # the dark lining behind
    k.add("shieldback", lathe(SH_C, n, [(0.0, -0.041), (SH_R * 0.9, -0.041), (SH_R * 0.9, -0.033), (0.0, -0.033)],
                              seg=48), "#5C3A26", "armR", fuse=False, surface="shell")
    # the grey line round it, at three quarters of its radius
    # (on the dome's slope between its profile's points (0.228, 0.03) and (0.184, 0.06), standing a little proud)
    r0 = SH_R * 0.75
    k.add("shieldline", lathe(SH_C, n, [(r0 - 0.007, 0.062), (r0 + 0.007, 0.052), (r0 + 0.007, 0.058),
                                       (r0 - 0.007, 0.068)], seg=64), RIM, "armR", fuse=False, surface="shell")
    # the cone in its middle
    k.add("boss", lathe(SH_C + n * 0.08, n, [(0.0, -0.01), (0.06, -0.01), (0.0, 0.16)], seg=40), "#E4E0D4", "armR",
          fuse=False, surface="shell")
    # green spots near the rim
    a, b = axes(n)
    up = b if b[2] > 0 else -b
    side = np.cross(n, up)
    for i, ang in enumerate((40, 140, 215, 300)):
        t = math.radians(ang)
        d = math.cos(t) * side + math.sin(t) * up
        r = SH_R * 0.86
        hh = 0.092 * (1 - (r / SH_R) ** 2) + 0.012
        c = SH_C + d * r + n * hh
        k.add(f"spot{i}", lathe(c, n * 0.6 + d * 0.4, [(0.0, -0.01), (0.03, -0.01), (0.03, 0.004), (0.0, 0.008)],
                                seg=24), GREEN, "armR", fuse=False, surface="shell")
    # the great spike up and out from the top rim, and a small one at the outer bottom
    top = SH_C + up * SH_R * 0.9 + side * 0.03
    k.add("rimspike", spike(top, up * 0.8 + side * 0.25 + n * 0.25, 0.17, 0.05), SHELL, "armR", fuse=False,
          surface="shell")
    low = SH_C - up * SH_R * 0.6 + side * SH_R * 0.7
    k.add("rimspike2", spike(low, -up * 0.5 + side * 0.8, 0.07, 0.03), SHELL, "armR", fuse=False, surface="shell")


def head(k):
    k.add("head", ell(HEAD, HEADR), FUR, "head")
    # the white beard of fur round the face's sides and below
    for i in range(14):
        a = math.pi * 0.42 + math.pi * 1.16 * i / 13
        rx, rz = math.sin(a), math.cos(a)
        base = np.array([0.078 * rx, HEAD[1] - 0.03, HEAD[2] + 0.07 * rz])
        ln = 0.045 if rz > -0.4 else 0.065
        k.add(f"mane{i}", spike(base, (rx, -0.5, rz), ln, 0.021), FUR, "head", fuse=False, surface="fur")


MASK = [(-1, -0.25), (-1.05, 0.55), (-0.85, 1.05), (-0.48, 0.45), (0, 1.1), (0.48, 0.45), (0.85, 1.05), (1.05, 0.55),
        (1, -0.25), (0.35, -0.55), (0, -1.0), (-0.35, -0.55)]


def face(k):
    p, n = cp.on_ell(0.0, 0.575, HEAD, HEADR)
    k.decal(p, unit(0, n[1], n[2] * 0.5), (0.072, 0.042), GOLD, eps=0.0025, rings=16, segs=72, outline=polygon(MASK))
    # the black mark between the eyes
    p2, n2 = cp.on_ell(0.0, 0.59, HEAD, HEADR)
    k.decal(p2, unit(0, n2[1], n2[2] * 0.5), (0.006, 0.014), INK, eps=0.0042, rings=6, segs=32,
            outline=polygon([(0, 1), (0.7, 0.2), (0, -1), (-0.7, 0.2)]))
    # a small frown
    p, n = cp.on_ell(0.0, 0.517, HEAD, HEADR)
    M_, F_ = srgb(MOUTH), srgb(FUR)
    def frown(u, v):
        c = np.tile(F_, (len(u), 1))
        c[np.abs(v - (0.5 - 0.9 * u * u)) < 0.45] = M_
        return c
    k.decal(p, unit(0, n[1], n[2] * 0.4), (0.022, 0.008), frown, eps=0.003, rings=8, segs=48)


def legs(k):
    k.add("haunch", ell((0.1, 0.0, 0.2), (0.08, 0.078, 0.075)), BODY, "leg{s}", mirror=True)
    k.add("leg", tube([(0.11, -0.005, 0.18), (0.15, -0.015, 0.1), (0.165, -0.02, 0.045)], [0.064, 0.059, 0.056]),
          BODY, "leg{s}", mirror=True)
    k.add("foot", ell((0.168, -0.045, 0.028), (0.066, 0.08, 0.03)), BODY, "leg{s}", mirror=True)
    for z in (0.085, 0.135):
        k.paint(lambda P, z=z: inside(np.abs(P[:, 2] - z) - 0.0035, 0.0015) * (np.abs(P[:, 0]) > 0.07), LINE,
                on=["leg", "haunch"])
    for i, dx in enumerate((-0.035, 0.0, 0.035)):
        b = np.array((0.168 + dx, -0.115, 0.022))
        k.add(f"toe{i}", hull([b + (0.015, 0.03, -0.018), b + (-0.015, 0.03, -0.018), b + (0.013, 0.03, 0.022),
                               b + (-0.013, 0.03, 0.022), b + (0, -0.03, -0.014)]), YELLOW, "leg{s}", fuse=False,
              mirror=True, surface="shell")
    # the green plates over the front of the thighs, with thorns
    k.add("plate", ell((0.115, -0.075, 0.215), (0.078, 0.035, 0.068), rot=(-10, 0, -18)), GREEN, "leg{s}",
          mirror=True, surface="leaf")
    for i, (dx, dz) in enumerate(((0.05, 0.03), (0.0, -0.02))):
        k.add(f"thorn{i}", spike((0.125 + dx, -0.1, 0.215 + dz), (0.6, -0.5, 0.2), 0.05, 0.016), GREEN, "leg{s}",
              fuse=False, mirror=True, surface="leaf")


def cape(k):
    # one shaggy mass falling from the waist down and back to the ground, its ends uneven tufts
    for i, (c, r) in enumerate((((0, 0.11, 0.27), (0.135, 0.075, 0.085)), ((0.01, 0.17, 0.18), (0.125, 0.075, 0.095)),
                                ((0.03, 0.225, 0.1), (0.105, 0.07, 0.085)))):
        k.add(f"cape{i}", ell(c, r), CAPE, "body", surface="fur")
    rs = np.random.RandomState(7)
    for i in range(11):
        t = i / 10
        a = math.pi * (0.1 + 0.8 * t)                     # round the drape's lower edge, side to side behind
        x = 0.1 * math.cos(a) + 0.03
        base = np.array([x, 0.235 + 0.05 * math.sin(a), 0.05 + 0.1 * abs(math.cos(a))])
        ln = 0.06 + 0.07 * rs.rand()
        d = (math.cos(a) * 0.7, 0.8 + rs.uniform(-0.2, 0.2), -0.35 - 0.3 * abs(math.cos(a)))
        k.add(f"capefur{i}", spike(base, d, ln, 0.035), CAPE, "body", fuse=False, surface="fur")
    for i in range(6):
        s = 1 if i % 2 else -1
        base = np.array([s * 0.125, 0.12 + 0.02 * i, 0.26 - 0.035 * i])
        k.add(f"capeside{i}", spike(base, (s, 0.3, -0.6), 0.05 + 0.02 * (i % 3), 0.03), CAPE, "body", fuse=False,
              surface="fur")
    for i, (x, z) in enumerate(((0.07, 0.27), (-0.08, 0.26), (0.0, 0.3))):
        k.add(f"capethorn{i}", spike((x, 0.17, z), (x * 2, 0.8, 0.5), 0.06, 0.018), GREEN, "body", fuse=False,
              surface="leaf")


def lay_eyes(k, state):
    G_, I_ = srgb(GOLD), srgb(INK)
    def slit(u, v):
        c = np.tile(I_, (len(u), 1))
        c[((u - 0.25) / 0.2) ** 2 + ((v - 0.15) / 0.35) ** 2 < 1] = (0.85, 0.85, 0.85)
        return c
    if state == "Open":
        look = slit
    elif state == "Fierce":
        def look(u, v):
            c = slit(u, v)
            c[v > 0.05 + 0.7 * u] = G_
            return c
    else:
        look = eye_look(state, slit, G_, I_, line=0.24)
    # an almond slanting up toward the outside
    outline = polygon([(1.0, 0.55), (0.3, 0.6), (-0.6, 0.15), (-1.0, -0.35), (-0.3, -0.6), (0.6, -0.4)])
    p, n = cp.on_ell(0.034, 0.567, HEAD, HEADR)
    nn = unit(n[0] * 0.8, n[1], n[2] * 0.5)
    k.decal(p, nn, (0.019, 0.013), look, eps=0.0045, rings=20, segs=56, outline=outline)
    pl, _ = cp.on_ell(-0.034, 0.567, HEAD, HEADR)
    outl = polygon([(-u, v) for u, v in [(1.0, 0.55), (0.3, 0.6), (-0.6, 0.15), (-1.0, -0.35), (-0.3, -0.6),
                                         (0.6, -0.4)]][::-1])
    k.decal(pl, nn * np.array([-1, 1, 1]), (0.019, 0.013), lambda u, v: look(-u, v), eps=0.0045, rings=20, segs=56,
            outline=outl)


def chesnaught_mega(src, out):
    k = Kit(src, coat="shell")
    hood(k)
    head(k)
    face(k)
    ch.add_body(k)
    legs(k)
    ch.add_arms(k)
    shield(k)
    cape(k)
    k.build(voxel=0.0065, smooth=4, band=0.006, wband=0.05)
    k.eye_states(lambda s: lay_eyes(k, s))
    k.export(out)


if __name__ == "__main__":
    chesnaught_mega(*args())
