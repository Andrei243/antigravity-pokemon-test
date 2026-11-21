# Conkeldurr sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown from
# Timburr's script (timburr.py, imported: its feet, hands, the way an eye is cut along its top line) as Gurdurr is:
#   blender --background --factory-startup --python sculpts/conkeldurr.py -- <the export's Conkeldurr.glb> <out.glb>
# From the references: the line grown old and hunched, its skin now tan. A round head carried low and forward with one
# thick knob on a stalk standing up from the crown; a big red bulb of a nose; heavy-lidded eyes, a thick dark lid
# straight across over the white and a black pupil just under it; a grey pointed beard under the nose; a smirk off to
# one side. Huge shoulders and arms wound round with raised pink-purple veins in coils, pink lines on the thighs, short
# bowed legs on small pointed feet. A grey concrete pillar in each hand, broken at both ends, the big fingers hooked
# over its top. The pillars stand a little forward and in from where the pictures hold them, so the model is no wider
# than it is tall.
# Reference ratios (3D picture, knob to feet): head without the knob / height 0.33, the knob / height 0.14, head width /
# height 0.3, shoulders and arms across / height 0.83, a pillar's height / height 0.83, a pillar's width / height 0.24.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, slab, srgb, inside, unit, axes, eye_look, outward, args, sibling

t = sibling("timburr")

TAN, VEIN, RED, BEARD = "#C8966E", "#C8709E", "#D0404E", "#A0A0A6"
STONE, INK, MOUTH, CRACK = "#AEB0B6", "#2A2220", "#3E2A26", "#5C5E66"

H = np.array([0.0, -0.105, 0.528])           # the head's middle (the head bone is at 0, -0.08, 0.46)
HR = np.array([0.1, 0.086, 0.088])


NECK = np.array([0.0, -0.08, 0.44])           # the head is drawn HS times larger about this point, then sunk
HS, SINK = 1.35, 0.022


def hp(p):
    return NECK + (np.array(p, float) - NECK) * HS - np.array([0, 0, SINK])


def hs(shape):
    V, F = shape
    return hp(V), F


def coil(k, name, a, b, ra, rb, turns, bone, start=0.0, lift=0.006, r=0.011):
    """A raised vein wound round the part from a to b (radii ra to rb), fused."""
    a, b = np.array(a, float), np.array(b, float)
    d = b - a
    e1, e2 = axes(d)
    pts = []
    for i in range(int(turns * 24) + 1):
        s = i / (turns * 24)
        th = start + 2 * math.pi * turns * s
        rr = ra + (rb - ra) * s + lift
        pts.append(a + d * s + rr * (math.cos(th) * e1 + math.sin(th) * e2))
    k.add(name, tube(pts, [r] * len(pts), seg=12, per=3), VEIN, bone, mirror=True)


def conkeldurr(src, out):
    k = Kit(src, coat="fur")

    # The head: round, carried low and forward, a thick knob on a stalk standing up from the crown
    # (the whole head, its nose, beard, knob, eyes and mouth, drawn at HS about the neck and sunk between the
    # shoulders: the game's shot showed the face a few pixels under a tiny knob)
    k.add("head", hs(ell(H, HR)), TAN, "head")
    k.add("jowl", hs(ell(H + (0, -0.03, -0.045), (0.085, 0.06, 0.045))), TAN, "head")
    k.add("stalk", hs(tube([(0.0, -0.1, 0.59), (0.004, -0.088, 0.64), (0.01, -0.078, 0.668)], [0.026, 0.02, 0.019])),
          TAN, "head")
    k.add("knob", hs(ell((0.012, -0.074, 0.685), (0.032, 0.03, 0.034))), TAN, "head")
    k.add("nose", outward(hs(ell((0.0, -0.205, 0.505), (0.043, 0.037, 0.039)))), RED, "head", fuse=False,
          surface="skin")
    # the grey beard under the nose, a pointed wedge
    k.add("beard", hull([hp(p) for p in [(0.042, -0.178, 0.485), (-0.042, -0.178, 0.485), (0.036, -0.145, 0.48),
                                         (-0.036, -0.145, 0.48), (0.0, -0.172, 0.418), (0.0, -0.148, 0.422),
                                         (0.024, -0.186, 0.455), (-0.024, -0.186, 0.455)]]),
          BEARD, "head", fuse=False, surface="hair")

    # The body: a hunched heavy chest, a round belly, wide hips
    k.add("chest", ell((0, -0.035, 0.415), (0.15, 0.115, 0.11), rot=(15, 0, 0)), TAN, "body")
    k.add("hips", ell((0, 0.01, 0.215), (0.12, 0.088, 0.068)), TAN, "body")
    k.add("trunk", ell((0, -0.012, 0.31), (0.138, 0.104, 0.17)), TAN, "body")        # one pear, no rolls

    # The arms: huge shoulders, arms out and forward to the pillars' tops, wound with veins
    SH, EL, WR = np.array([0.15, -0.04, 0.445]), np.array([0.235, -0.09, 0.43]), np.array([0.25, -0.14, 0.468])
    k.add("shoulder", ell((0.158, -0.04, 0.455), (0.086, 0.082, 0.082)), TAN, "arm{s}", mirror=True)
    k.add("upper", tube([SH, (SH + EL) / 2, EL], [0.075, 0.072, 0.064]), TAN, "arm{s}", mirror=True)
    k.add("fore", tube([EL, WR], [0.062, 0.05]), TAN, "arm{s}", mirror=True)
    coil(k, "veinup", (0.12, -0.03, 0.452), EL + (0.01, 0, 0), 0.08, 0.064, 2.6, "arm{s}", start=1.0)
    coil(k, "veinfore", EL + (0.005, -0.01, 0.005), WR, 0.06, 0.05, 1.2, "arm{s}", start=2.5, r=0.01)
    # the hand on the pillar's top, three thick fingers hooked over its front edge, a thumb on its inner side
    k.add("palm", ell((0.262, -0.158, 0.49), (0.054, 0.052, 0.03)), TAN, "arm{s}", mirror=True)
    for i, x in enumerate((0.302, 0.265, 0.228)):
        k.add(f"finger{i}", tube([(x, -0.19, 0.49), (x, -0.232, 0.48), (x + 0.002, -0.24, 0.448)],
                                 [0.021, 0.019, 0.016]), TAN, "arm{s}", mirror=True)
    k.add("thumb", tube([(0.198, -0.15, 0.482), (0.176, -0.168, 0.455)], [0.02, 0.016]), TAN, "arm{s}", mirror=True)

    # The pillars: grey concrete, squared with bevelled corners, broken at both ends, a crack or two as dark grooves
    rs = np.random.RandomState(534)
    def crack(line, w):
        """A groove's outline from its middle line (a, b points), w wide."""
        line = np.array(line, float)
        L, R = [], []
        for i in range(len(line)):
            d = line[min(i + 1, len(line) - 1)] - line[max(i - 1, 0)]
            n = np.array([-d[1], d[0]]) / np.linalg.norm(d) * w / 2
            L.append(line[i] + n); R.append(line[i] - n)
        return L + R[::-1]
    for s, side in ((1, "R"), (-1, "L")):
        cx, cy, hx, hy, b = 0.265 * s, -0.15, 0.078, 0.07, 0.017
        front = np.array([cx, cy - hy - 0.0015, 0.0])
        k.add(f"crackfront{side}", slab(crack([(0.012 * s, 0.4), (-0.016 * s, 0.34), (0.006 * s, 0.29),
                                               (-0.01 * s, 0.24), (0.004 * s, 0.2)], 0.011),
                                        front, (1, 0, 0), (0, 0, 1), 0.004), CRACK, f"arm{side}", fuse=False,
              surface="shell")
        k.add(f"crackfront2{side}", slab(crack([(-0.02 * s, 0.13), (0.004 * s, 0.09), (-0.008 * s, 0.05)], 0.01),
                                         front, (1, 0, 0), (0, 0, 1), 0.004), CRACK, f"arm{side}", fuse=False,
              surface="shell")
        outer = np.array([cx + s * (hx + 0.0015), cy, 0.0])
        k.add(f"crackside{side}", slab(crack([(0.02, 0.36), (-0.008, 0.3), (0.012, 0.25), (-0.004, 0.19)], 0.011),
                                       outer, (0, 1, 0), (0, 0, 1), 0.004), CRACK, f"arm{side}", fuse=False,
              surface="shell")
        ring = [(hx - b, hy), (hx, hy - b), (hx, -hy + b), (hx - b, -hy), (-hx + b, -hy), (-hx, -hy + b),
                (-hx, hy - b), (-hx + b, hy)]
        pts = []
        for i, (x, y) in enumerate(ring):
            pts.append((cx + x, cy + y, 0.004 + 0.03 * rs.rand()))
            pts.append((cx + x, cy + y, 0.43 + 0.035 * rs.rand()))
        k.add(f"pillar{side}", hull(pts), STONE, f"arm{side}", fuse=False, surface="shell")

    # The legs: short, bowed, thick thighs with pink lines on them, small feet with pointed toes
    k.add("thigh", ell((0.095, 0.0, 0.15), (0.076, 0.07, 0.082)), TAN, "leg{s}", mirror=True)
    k.add("veinthigh", tube([(0.06, -0.06, 0.17), (0.11, -0.055, 0.15), (0.155, -0.02, 0.15)], [0.01, 0.011, 0.009],
                            seg=12), VEIN, "leg{s}", mirror=True)
    k.add("shin", tube([(0.105, 0.0, 0.09), (0.116, -0.014, 0.035)], [0.048, 0.042]), TAN, "leg{s}", mirror=True)
    t.foot(k, (0.122, -0.04, 0.018), (0.048, 0.062, 0.018), TAN, "leg{s}")

    # The smirk, off toward +X beside the beard
    CS, CM = srgb(TAN), srgb(MOUTH)
    def smirk(u, v):
        c_ = np.tile(CS, (len(u), 1))
        c_[(np.abs(v - (-0.2 + 0.5 * (u + 1) ** 2 / 4 * 1.6)) < 0.32) & (np.abs(u) < 0.92)] = CM
        return c_
    MP, _ = t.front_of(H + (0, -0.03, -0.045), (0.085, 0.06, 0.045), 0.05, 0.478)
    k.decal(hp(MP), unit(0.5, -1, -0.2), (0.024 * HS, 0.012 * HS), smirk, eps=0.003, rings=10, segs=56)

    k.build(voxel=0.0052, smooth=4, band=0.004)

    # The eyes: heavy-lidded, a thick dark lid straight across the top, white below, a black pupil under the lid
    CK = srgb(INK)
    def top(u):
        return 0.55 + 0.06 * u
    def open_eye(u, v):
        c_ = np.tile(np.array([1.0, 1.0, 1.0]), (len(u), 1))
        c_[u * u + v * v > 0.86 ** 2] = CK
        c_[((u + 0.12) / 0.3) ** 2 + ((v - 0.0) / 0.42) ** 2 < 1] = CK
        c_[v > top(u) - 0.55] = CK
        return c_
    E, EN = t.front_of(H, HR, 0.046, 0.553)
    E = hp(E)
    EN = unit(EN[0], EN[1], EN[2] + 0.05)
    def lay(state):
        look = eye_look(state, open_eye, CS, CK, line=0.16)
        for s in (1, -1):
            t.eye_patch(k, E, EN, (0.028 * HS, 0.024 * HS), look, CS, CK, top, s)

    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    conkeldurr(*args())
