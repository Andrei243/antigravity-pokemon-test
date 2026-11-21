# Emboar sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown from
# Tepig's and Pignite's scripts (tepig.py, pignite.py, imported: the line's colours, snout, eye, tusks, curly tail and
# the way the cap is painted):
#   blender --background --factory-startup --python sculpts/emboar.py -- <the export's Emboar.glb> <out.glb>
# From the references: a massive boar wrestler. A smallish orange head sunk between orange shoulders; a black crest:
# a stripe down the forehead to the brows and two black prongs swept up and out from the crown; fierce eyes, white
# with a red pupil under heavy dark brows; a red-pink snout with two nostrils; white tusks curving up beside it; a
# beard of flames round the neck, a spiky yellow-orange bib on the chest and two tall plumes of flame up beside the
# head; a huge round black belly with a band of yellow swirls across it, black fur in spikes along its top and bottom
# edges; red trunks; short thick black legs, the black's top edge a zigzag lined yellow, white claws on the feet;
# great black arms hanging forward, a yellow band at the shoulder, a red cuff at the wrist, a spike at the elbow,
# enormous hands with two big white claws and a thumb; a black tail ending in a spiky tuft.
# (The pictures overrule the task's text: the flames are a beard round the neck AND two plumes up beside the head; the
# cuffs are red bands at the wrists, the arms themselves black.)
# Reference ratios (3D picture, feet to the flames' tops): head (crown to chin) / height 0.21; the crest above the
# crown / height 0.08; belly width / height 0.58; arm thickness / height 0.17; width over the hands / height 1.0;
# legs showing under the trunks / height 0.15.
# The export's joints: body (0, 0, 0.42), arms (+-0.16, 0, 0.54), head (0, -0.04, 0.62), ears (+-0.07, -0.06, 0.73),
# legs (+-0.09, 0, 0.24), tail (0, 0.11, 0.3).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, boxy, eye_look, args, outward, ring_band, \
    hull, axes, tri, sibling

tp = sibling("tepig")

ORANGE, YELLOW, INK, WHITE = tp.ORANGE, tp.YELLOW, tp.INK, tp.WHITE
BLACK = "#2E2727"
RED = "#D4443A"            # the trunks and the cuffs
SNOUT = "#D8524C"
CLAW = "#ECECF0"
IRIS = "#D8282C"
FLAME_RED, FLAME, FLAME_CORE = "#E8452A", "#F68A2A", "#FFC83A"

# --- the frame ----------------------------------------------------------------------------------------------------------
H = np.array([0.0, -0.075, 0.672])
HR = np.array([0.133, 0.12, 0.103])
BEL = np.array([0.0, 0.0, 0.37])            # the black belly
BELR = np.array([0.24, 0.205, 0.155])
BAND = (0.312, 0.432)                       # the yellow band's bottom and top
EYE_X, EYE_Z, EYE_HALF = 0.066, 0.692, (0.03, 0.03)
SH, EL, WR = np.array((0.205, 0.0, 0.555)), np.array((0.285, 0.015, 0.45)), np.array((0.295, -0.055, 0.335))
HAND = np.array((0.297, -0.085, 0.27))


def tongue(base, d, length, width, depth=None, bend=0.0):
    """A flame's tongue: a solid round at its root tapering to a point along d, its tip bent up by bend."""
    d = unit(*d)
    depth = depth or width * 0.8
    def shape(V):
        V = V.copy()
        t = np.clip((V[:, 2] + 1) / 2, 0, 1)
        w = 1 - 0.9 * t ** 1.3
        V[:, 0] *= w; V[:, 1] *= w
        return V
    V, F = ell((0, 0, 0), (width, depth, length / 2), deform=shape, sub=3)
    a, b = axes(d)
    t = (V[:, 2] / length + 0.5)
    V = V[:, :1] * a + V[:, 1:2] * b + V[:, 2:] * d + np.outer(t ** 2 * bend * length, (0, 0, 1.0))
    return V + np.array(base, float) + d * length / 2, F


class Pieces:
    """Several shapes joined into one loose piece (one material each for the flames)."""
    def __init__(self):
        self.V, self.F = [], []

    def put(self, shape):
        V, F = outward(shape)
        o = sum(len(v) for v in self.V)
        self.V.append(V); self.F.extend([[i + o for i in f] for f in F])

    def shape(self):
        return np.vstack(self.V), self.F


def add_body(k):
    k.add("belly", ell(BEL, BELR, deform=boxy(2.3)), BLACK, "body")
    k.add("chest", ell((0, -0.015, 0.535), (0.205, 0.15, 0.075)), ORANGE, "body")
    k.add("yoke", ell((0, 0.01, 0.585), (0.15, 0.12, 0.06)), ORANGE, "body")
    k.add("hips", ell((0, 0.01, 0.215), (0.205, 0.165, 0.085)), RED, "body", surface="cloth")
    # orange over the chest and shoulders, a crisp line at the belly's top
    k.paint(lambda P: inside(0.497 - P[:, 2], 0.003) * (P[:, 2] < 0.64), ORANGE, on=["belly", "chest", "yoke"])
    k.paint(lambda P: inside(P[:, 2] - 0.497, 0.003) * (P[:, 2] > 0.3), BLACK, on=["belly", "chest"])
    k.paint(lambda P: inside(P[:, 2] - 0.245, 0.003), RED, on=["belly", "hips"])
    # the yellow band and its swirls: a wave of black through it and a coil at each side of the front
    CY = srgb(YELLOW); CB = srgb(BLACK)
    def band(P):
        x, y, z = P[:, 0], P[:, 1], P[:, 2]
        a = np.arctan2(x, -(y - BEL[1]))
        return inside(BAND[0] - z, 0.003) * inside(z - BAND[1], 0.003) * inside(np.abs(a) - 1.75, 0.01)
    k.paint(band, YELLOW, on=["belly"])
    def swirl_lines(P):
        x, y, z = P[:, 0], P[:, 1], P[:, 2]
        a = np.arctan2(x, -(y - BEL[1])) * 0.24          # along the band, in metres
        zc = 0.5 * (BAND[0] + BAND[1])
        wave = np.abs(z - (zc + 0.036 * np.sin(a / 0.13 * math.pi))) - 0.0065
        m = inside(wave, 0.0025) * inside(np.abs(a) - 0.26, 0.004)
        # a coil round each crest of the wave's ends
        for s in (1, -1):
            cx = 0.33 * s
            r = np.hypot(a - cx, z - zc)
            ang = np.arctan2(z - zc, (a - cx) * s)
            sp = np.abs(((r - 0.012 - 0.03 * ((ang + math.pi) / (2 * math.pi))) + 0.015) % 0.03 - 0.015) - 0.0055
            m = np.maximum(m, inside(sp, 0.0025) * inside(r - 0.05, 0.003))
        return m * inside(BAND[0] + 0.004 - z, 0.002) * inside(z - BAND[1] + 0.004, 0.002)
    k.paint(swirl_lines, BLACK, on=["belly"])


def add_fur_edges(k):
    """Black fur in spikes along the belly's top and bottom edges: solid hulls."""
    for row, (z, up, n, ln) in enumerate(((0.49, 1.0, 14, 0.07), (0.255, -1.0, 13, 0.06))):
        for i in range(n):
            a = 2 * math.pi * (i + 0.5 * row) / n
            c, s_ = math.cos(a), math.sin(a)
            t = (z - BEL[2]) / BELR[2]
            rr = math.sqrt(max(0.0, 1 - t * t * 0.55))
            p = np.array([BELR[0] * rr * s_, BEL[1] - BELR[1] * rr * c, z])
            out = unit(s_ / BELR[0], -c / BELR[1], 0)
            tip = p + out * ln * 0.55 + np.array([0, 0, up * ln])
            side = np.cross(out, (0, 0, 1.0))
            w = 0.034
            k.add(f"fur{row}_{i}", hull([p - out * 0.02 + side * w, p - out * 0.02 - side * w, p + out * 0.012 + side * w * 0.6,
                                        p + out * 0.012 - side * w * 0.6, p - (0, 0, up * 0.02), tip]),
                  BLACK, "body", fuse=False, surface="fur")


def add_legs(k):
    k.add("thigh", ell((0.13, 0.0, 0.17), (0.1, 0.11, 0.1)), RED, "leg{s}", mirror=True, surface="cloth")
    k.add("shin", tube([(0.13, -0.01, 0.13), (0.134, -0.02, 0.085)], [0.078, 0.074]), BLACK, "leg{s}", mirror=True)
    k.add("foot", ell((0.135, -0.045, 0.034), (0.085, 0.105, 0.036), deform=boxy(2.4)), BLACK, "leg{s}", mirror=True)
    # the black's top a zigzag, lined yellow
    def zz(P):
        Q = P.copy(); Q[:, 0] = np.abs(Q[:, 0])
        phi = np.arctan2(Q[:, 1] + 0.01, Q[:, 0] - 0.13)
        return 0.12 + 0.028 * tri(phi * 4 / (2 * math.pi))
    leg = lambda P: (np.abs(np.abs(P[:, 0]) - 0.13) < 0.13)
    k.paint(lambda P: inside(P[:, 2] - zz(P) - 0.014, 0.0025) * leg(P), YELLOW, on=["thigh", "shin"])
    k.paint(lambda P: inside(P[:, 2] - zz(P), 0.0025) * leg(P), BLACK, on=["thigh", "shin", "foot"])
    # white claws on the foot's front
    for i, dx in enumerate((-0.04, 0.0, 0.04)):
        b = np.array((0.135 + dx, -0.135, 0.02))
        k.add(f"claw{i}", hull([b + (0.016, 0.02, -0.016), b + (-0.016, 0.02, -0.016), b + (0.014, 0.02, 0.02),
                                b + (-0.014, 0.02, 0.02), b + (0, -0.03, -0.012)]), CLAW, "leg{s}", fuse=False,
              mirror=True, surface="shell")


def add_arms(k):
    k.add("shoulder", ell(SH, (0.085, 0.085, 0.08)), BLACK, "arm{s}", mirror=True)
    k.add("upper", tube([SH, EL], [0.078, 0.074]), BLACK, "arm{s}", mirror=True)
    k.add("fore", tube([EL, WR], [0.076, 0.074]), BLACK, "arm{s}", mirror=True)
    k.add("hand", ell(HAND, (0.088, 0.082, 0.08), deform=boxy(2.4)), BLACK, "arm{s}", mirror=True)
    du, dl = unit(*(EL - SH)), unit(*(WR - EL))
    k.add("shoulderband", ring_band(SH + du * 0.035, du, 0.08, 0.026, thick=0.008), YELLOW, "arm{s}", fuse=False,
          mirror=True, surface="cloth")
    k.add("cuff", ring_band(EL + dl * 0.085, dl, 0.076, 0.03, thick=0.008), RED, "arm{s}", fuse=False, mirror=True,
          surface="cloth")
    # the spike at the elbow, back and up
    side = unit(1, 0, 0)
    k.add("elbowspike", hull([EL + (0.04, 0.035, 0.02), EL + (0.04, 0.035, -0.035), EL + (0.07, 0.02, -0.01),
                               EL + (0.015, 0.05, -0.01), EL + (0.07, 0.14, 0.07)]), BLACK, "arm{s}", fuse=False,
          mirror=True, surface="fur")
    # two big white claws down the hand's front and a thumb claw on its inner side, curving under
    for i, dx in enumerate((-0.03, 0.03)):
        b = HAND + np.array((dx, -0.07, -0.02))
        k.add(f"finger{i}", hull([b + (0.024, 0.02, 0.03), b + (-0.024, 0.02, 0.03), b + (0.024, -0.02, 0.02),
                                  b + (-0.024, -0.02, 0.02), b + (0.02, 0.01, -0.03), b + (-0.02, 0.01, -0.03),
                                  b + (0, -0.04, -0.06)]), CLAW, "arm{s}", fuse=False, mirror=True, surface="shell")
    b = HAND + np.array((-0.075, -0.035, -0.0))
    k.add("thumb", hull([b + (0.02, 0.02, 0.025), b + (0.02, -0.02, 0.025), b + (0.02, 0.02, -0.02),
                         b + (0.02, -0.02, -0.02), b + (-0.045, -0.025, -0.045)]), CLAW, "arm{s}", fuse=False,
          mirror=True, surface="shell")


def add_head(k):
    k.add("head", ell(H, HR, deform=boxy(2.2)), ORANGE, "head")
    k.add("jowl", ell(H + (0, -0.015, -0.05), (0.11, 0.095, 0.06)), ORANGE, "head")
    tp.snout(k, H + (0, -0.127, -0.03), (0.048, 0.028, 0.034), "head", color=SNOUT, tilt=6)
    # the crest's two prongs: black, up and out from the crown, a smaller prong off each
    for side, s in (("R", 1), ("L", -1)):
        b = H + np.array((0.035 * s, -0.035, 0.07))
        pts = [b, b + (0.035 * s, 0.0, 0.045), b + (0.075 * s, 0.01, 0.085), b + (0.115 * s, 0.02, 0.11)]
        k.add("prong" + side, blade(pts, [0.03, 0.03, 0.02, 0.006], (0, 1, 0), 0.5), BLACK, "ear" + side)
        q = b + np.array((0.045 * s, 0.0, 0.05))
        k.add("prongb" + side, blade([q, q + (0.045 * s, 0.01, 0.01), q + (0.08 * s, 0.015, 0.0)],
                                     [0.02, 0.014, 0.005], (0, 1, 0), 0.5), BLACK, "ear" + side)
    # the tusks: white, curving up beside the snout
    for s in (1, -1):
        b = H + np.array((0.072 * s, -0.098, -0.047))
        k.add("tusk" + ("R" if s > 0 else "L"),
              hull([b + (0.016, 0.01, 0), b + (-0.014, 0.01, 0), b + (0, -0.016, 0), b + (0, 0.022, 0.0),
                    b + (0.012 * s, -0.012, 0.03), b + (0.02 * s, -0.016, 0.055)]), CLAW, "head", fuse=False,
              surface="shell")


def paint_head(k):
    """The black stripe down the forehead to the brows, and the brows slanting down to it."""
    def crest(P):
        x, y, z = P[:, 0], P[:, 1], P[:, 2]
        rel = P - H
        ax = np.abs(x)
        stripe = inside(ax - 0.022 - 0.45 * np.clip(rel[:, 2] - 0.02, 0, 1), 0.003) * inside(0.008 - rel[:, 2], 0.003)
        crown = inside(0.062 - rel[:, 2], 0.003) * inside(ax - 0.07, 0.003)
        return np.maximum(stripe, crown) * (rel[:, 1] < 0.06)
    k.paint(crest, BLACK, on=["head", "jowl", "prong"])


def add_flames(k):
    """The beard of flames round the neck, a spiky bib on the chest and two tall plumes up beside the head."""
    red, mid, core = Pieces(), Pieces(), Pieces()
    # the ring round the neck: tongues out and up, the front ones lying down over the chest
    n = 13
    for i in range(n):
        a = math.pi * (i / (n - 1) - 0.5) * 1.6         # from one side round the front to the other
        s_, c = math.sin(a), math.cos(a)
        base = np.array([0.125 * s_, -0.05 - 0.09 * c, 0.6])
        front = c > 0.6
        d = (s_ * 0.8, -c * 0.6, -0.55) if front else (s_ * 0.9, -c * 0.3, 0.35)
        ln = 0.1 if front else 0.12
        (mid if i % 2 else red).put(tongue(base, d, ln, 0.04, bend=0.0 if front else 0.15))
    # the bib: yellow tongues pointing down over the upper chest
    for i, x in enumerate((-0.07, -0.035, 0.0, 0.035, 0.07)):
        core.put(tongue((x, -0.135, 0.59), (x * 1.5, -0.35, -1.0), 0.085 - 0.012 * abs(i - 2), 0.03))
    # the plumes: a great tongue up beside each side of the head, smaller ones round it
    for s in (1, -1):
        b = np.array((0.14 * s, -0.02, 0.6))
        red.put(tongue(b, (0.28 * s, 0.12, 1.0), 0.23, 0.06, depth=0.045, bend=0.0))
        mid.put(tongue(b + (0.0, -0.01, 0.02), (0.55 * s, 0.05, 1.0), 0.15, 0.045))
        mid.put(tongue(b + (-0.02 * s, 0.02, 0.02), (0.05 * s, 0.25, 1.0), 0.17, 0.04))
        core.put(tongue(b + (0.0, -0.035, 0.0), (0.3 * s, 0.0, 1.0), 0.13, 0.035, depth=0.03))
    k.add("flamered", red.shape(), FLAME_RED, "head", fuse=False, glow=FLAME_RED)
    k.add("flamemid", mid.shape(), FLAME, "head", fuse=False, glow=FLAME)
    k.add("flamecore", core.shape(), FLAME_CORE, "head", fuse=False)


def add_tail(k):
    end = tp.tail(k, (0.0, 0.2, 0.26), 0.045, unit(0.55, 0.83, 0), color=BLACK, r=0.017,
                  end=(0.13, 0.36, 0.2))
    end = np.array(end)
    k.add("tuft", outward(ell(end, (0.04, 0.04, 0.04), sub=3)), BLACK, "tail", fuse=False, surface="fur")
    rs = np.random.RandomState(5)
    for i in range(10):
        d = unit(*rs.normal(size=3)) * np.array([1, 1, 0.8])
        d = unit(*d)
        a, b = axes(d)
        p = end + d * 0.025
        k.add(f"tuftspike{i}", hull([p + a * 0.022, p - a * 0.022, p + b * 0.022, p - b * 0.022, p + d * 0.06]), BLACK,
              "tail", fuse=False, surface="fur")


def lay_eyes(k, state):
    CW, CR, CI, CB, CO = srgb(WHITE), srgb(IRIS), srgb(INK), srgb(BLACK), srgb(ORANGE)
    def eye(u, v):
        c = np.tile(CW, (len(u), 1))
        c[((u + 0.15) / 0.48) ** 2 + ((v + 0.08) / 0.62) ** 2 < 1] = CR
        c[((u + 0.15) / 0.22) ** 2 + ((v + 0.08) / 0.3) ** 2 < 1] = CI
        c[v > 0.42 + 0.45 * u] = CB                # the heavy brow, lower toward the nose
        return c
    if state == "Fierce":
        def look(u, v):
            c = eye(u, v)
            c[v > 0.2 + 0.6 * u] = CB
            return c
    else:
        look = eye_look(state, eye, CO, CI, line=0.17)
    p, n = tp.on_ell(EYE_X, EYE_Z, H, HR)
    n = unit(n[0] * 1.1, n[1], n[2] * 0.6)
    k.decal(p, n, EYE_HALF, look, eps=0.004, rings=20, segs=56, mirror=True)


def add_mouth(k):
    CM, CO = srgb("#3A2422"), srgb(ORANGE)
    def mouth(u, v):
        c = np.tile(CO, (len(u), 1))
        c[(np.abs(v - (0.15 * u * u)) < 0.3) & (np.abs(u) < 0.95)] = CM
        return c
    k.decal(H + (0, -0.108, -0.076), unit(0, -1, -0.2), (0.05, 0.011), mouth, eps=0.004, rings=8, segs=56)


def emboar(src, out):
    k = Kit(src, coat="fur")
    add_body(k)
    add_fur_edges(k)
    add_legs(k)
    add_arms(k)
    add_head(k)
    paint_head(k)
    add_flames(k)
    add_tail(k)
    add_mouth(k)
    k.build(voxel=0.0075, band=0.005)
    k.eye_states(lambda s: lay_eyes(k, s))
    k.export(out)


if __name__ == "__main__":
    emboar(*args())
