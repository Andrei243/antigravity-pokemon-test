# Mega Greninja sculpted on the skeleton and clips of the game's own export of the form (sculptkit.py), grown from
# Greninja's script (greninja.py, imported: its colours, its webs and fanned digits with round pads, the almond eye cut
# straight along its brow, the tongue wound round the neck as a scarf):
#   blender --background --factory-startup --python sculpts/greninjamega.py -- <the export's Greninja-Mega.glb> <out.glb>
# The form hangs head down (the export's skeleton is upside down: the legs' joints at 0.816 over the arms' at 0.596),
# so everything here is written where it hangs, not built standing and turned over.
# From the two pictures, the masses by size:
#   1. the great shuriken of water it hangs from, as wide as the whole figure is tall: a star of eight points, four
#      long and four short, pale blue, a dark blue ring at its middle where the feet cling. It is two stars crossed at
#      a shallow angle (one tilted toward each of the game's cameras), since one level star is an edge to both;
#   2. the body, near black with a blue cast, hanging straight down from the feet: long legs, a cream bib, the arms
#      bent out at the head's level with blue three-fingered hands and cream webs;
#   3. the pink tongue wound round the neck and one end dangling far below the head, curling at its tip;
#   4. cyan crossed blades at the outer side of each knee, a big white spot on the front of each thigh;
#   5. the head at the bottom: a cyan blade sweeping down and out from the crown and a dark horn beside it, narrow red
#      eyes in white, the brow's straight cut toward the crown.
# (The pictures overrule the game's sculptor where it differs: the shuriken is pale water with a dark ring, not a glow
# of six even blades, and the body is near black, not navy and pale blue.)
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, slab, lathe, polygon, srgb, unit, eye_look, args, outward, sibling

gr = sibling("greninja")
fk = gr.fk
CREAM, PINK, WHITE, RED, INK = gr.CREAM, gr.PINK, gr.WHITE, gr.RED, gr.INK
BLACK = "#252A40"
BLUE = "#2F4AB0"
CYAN = "#58C6F0"
WATER = "#8ED6F2"
DEEP = "#3A84CE"
LINE = "#7C8CC0"            # the closed eye's line, light on the dark lid

SC = np.array([0.0, 0.0, 0.985])      # the shuriken's middle
SR, SR2, SIN = 0.37, 0.2, 0.14        # its long points, short points and the inner corners
TILT = 17.0


def rx(v, deg):
    a = math.radians(deg)
    x, y, z = v
    return np.array([x, y * math.cos(a) - z * math.sin(a), y * math.sin(a) + z * math.cos(a)])


def shuriken(k):
    pts = []
    for i in range(16):
        a = math.pi * i / 8 + math.radians(8)
        r = (SR if i % 4 == 0 else SR2) if i % 2 == 0 else SIN
        a2 = a + (math.radians(7) if i % 2 == 0 else 0.0)          # the points swept round a little, a pinwheel
        pts.append((r * math.cos(a2), r * math.sin(a2)))
    for j, tilt in enumerate((TILT, -TILT)):
        ex = rx((1, 0, 0), tilt)
        ez = rx((0, 1, 0), tilt)
        P = pts if j == 0 else [(0.88 * r * math.cos(math.atan2(y, x) + math.pi / 4), 0.88 * r * math.sin(math.atan2(y, x) + math.pi / 4))
                                for x, y in pts for r in [math.hypot(x, y)]]
        k.add(f"shuriken{j}", slab(P, SC, ex, ez, 0.026), WATER, "body", fuse=False, surface="plastic")
    # the dark ring at its middle, where the feet cling
    k.add("hub", lathe(SC + np.array([0, 0, -0.012]), (0, 0, 1),
                       [(0.0, -0.035), (0.1, -0.035), (0.115, -0.02), (0.115, 0.02), (0.1, 0.032), (0.0, 0.032)], seg=48),
          DEEP, "body", fuse=False, surface="plastic")
    k.add("hubin", lathe(SC + np.array([0, 0, -0.012]), (0, 0, 1),
                         [(0.0, -0.039), (0.06, -0.039), (0.06, 0.036), (0.0, 0.036)], seg=40), WATER, "body",
          fuse=False, surface="plastic")


def body(k):
    k.add("pelvis", ell((0, 0.01, 0.685), (0.066, 0.058, 0.052)), BLACK, "body")
    k.add("waist", ell((0, 0.0, 0.632), (0.052, 0.046, 0.05)), BLACK, "body")
    k.add("chest", ell((0, -0.012, 0.578), (0.076, 0.058, 0.05)), BLACK, "body")
    k.add("neck", ell((0, -0.014, 0.528), (0.042, 0.04, 0.03)), BLACK, "body")
    k.add("shoulder", ell((0.066, -0.01, 0.578), (0.034, 0.036, 0.034)), BLACK, "arm{s}", mirror=True)
    # the cream bib on the front, from the chest toward the hips
    k.paint(lambda P: (np.abs(P[:, 0]) < 0.035 * np.clip((0.7 - P[:, 2]) / 0.12, 0.2, 1)) * (P[:, 1] < -0.03)
            * (P[:, 2] > 0.555) * (P[:, 2] < 0.69), CREAM, on=["chest", "waist", "pelvis"])


def legs(k):
    HIP, KN, AN = np.array((0.045, 0.01, 0.69)), np.array((0.092, -0.035, 0.795)), np.array((0.07, 0.0, 0.9))
    k.add("thigh", tube([HIP, (0.075, -0.015, 0.745), KN], [0.05, 0.046, 0.036]), BLACK, "leg{s}", mirror=True)
    k.add("shin", tube([KN, (0.085, -0.02, 0.85), AN], [0.034, 0.028, 0.024]), BLACK, "leg{s}", mirror=True)
    FB = np.array((0.07, 0.0, 0.925))
    k.add("foot", ell(FB, (0.028, 0.036, 0.022)), BLUE, "leg{s}", mirror=True)
    td, ts = (0.25, -0.4, 0.75), (0.08, 0.0, 0.0)
    fk.digits(k, "toe", FB, td, ts, 0.05, 0.011, BLUE, "leg{s}", tip=BLUE, tip_r=0.017)
    gr.web(k, "footweb", FB, gr.splay(FB, td, ts, 0.05), np.cross(unit(*td), unit(*ts)), CREAM, "leg{s}")
    # the cyan crossed blades at the outer side of each knee: two plates, each a long thin diamond
    c = KN + np.array([0.035, -0.008, 0.0])
    n = unit(0.75, -0.66, 0.0)
    ex = unit(-n[1], n[0], 0.0)
    for j, ang in enumerate((40, -40)):
        a = math.radians(ang)
        e1 = ex * math.cos(a) + np.array([0, 0, 1.0]) * math.sin(a)
        e2 = np.cross(n, e1)
        L, W = 0.085, 0.017
        k.add(f"kneeblade{j}", slab([(-L, 0), (0, -W), (L, 0), (0, W)], c + n * 0.004 * j, e1, e2, 0.011), CYAN,
              "leg{s}", fuse=False, mirror=True, surface="plastic")
    # the big white spot on the front of each thigh
    k.decal((0.075, -0.06, 0.745), unit(0.35, -1, 0.0), (0.03, 0.028), WHITE, eps=0.002, rings=12, segs=48,
            mirror=True)


def arms(k):
    SH, EL, WR = np.array((0.068, -0.01, 0.585)), np.array((0.132, -0.02, 0.555)), np.array((0.152, -0.06, 0.505))
    k.add("upper", tube([SH, (0.1, -0.012, 0.575), EL], [0.026, 0.024, 0.021]), BLACK, "arm{s}", mirror=True)
    k.add("fore", tube([EL, (0.146, -0.04, 0.53), WR], [0.021, 0.019, 0.018]), BLACK, "arm{s}", mirror=True)
    HB = np.array((0.158, -0.074, 0.49))
    k.add("hand", ell(HB, (0.022, 0.022, 0.018)), BLUE, "arm{s}", mirror=True)
    fd, fs = (0.2, -0.55, -0.7), (0.065, 0.0, 0.0)
    fk.digits(k, "finger", HB, fd, fs, 0.05, 0.009, BLUE, "arm{s}", tip=BLUE, tip_r=0.015)
    gr.web(k, "palmweb", HB, gr.splay(HB, fd, fs, 0.05), np.cross(unit(*fd), unit(*fs)), CREAM, "arm{s}")


HC = np.array([0.0, -0.025, 0.455])
HR = np.array((0.078, 0.072, 0.066))


def head(k):
    k.add("head", ell(HC, HR), BLACK, "head")
    k.add("snout", ell((0, -0.08, 0.478), (0.058, 0.046, 0.036)), BLACK, "head")
    # the cyan blade sweeping down and out from the crown to -X, a dark horn beside it to +X
    def plate(p1, p2, p3, th):
        p1, p2, p3 = map(np.array, (p1, p2, p3))
        n = unit(*np.cross(p2 - p1, p3 - p1))
        pts = np.array([p1, p2, p3, (p1 + p2) / 2 + (p3 - (p1 + p2) / 2) * 0.35 + n * 0.0])
        return hull(np.vstack([pts + n * th, pts - n * th]))
    k.add("crestblade", plate((-0.02, -0.07, 0.405), (0.03, 0.02, 0.392), (-0.16, -0.03, 0.29), 0.013), CYAN, "head",
          fuse=False, surface="plastic")
    k.add("horn", plate((0.015, -0.05, 0.4), (-0.015, 0.03, 0.395), (0.1, 0.0, 0.32), 0.012), BLACK, "head",
          fuse=False)


def tongue(k):
    NC = np.array([0.0, -0.016, 0.528])
    ring = []
    for a in np.linspace(0, 2 * math.pi, 19)[:-1]:
        th = math.radians(50) + a
        x, y = 0.07 * math.sin(th), NC[1] + 0.066 * math.cos(th)
        z = NC[2] - 0.016 * math.cos(th)            # lower at the back, over the snout in front
        ring.append((x, y, z))
    ring.append(ring[0]); ring.append(ring[1])
    k.add("scarf", outward(tube(ring, [0.022] * len(ring), seg=20)), PINK, "head", fuse=False, surface="skin")
    r0 = np.array(ring[1])               # from the neck's +X side, down beside the head, never before the eye
    trail = [r0, r0 + (0.03, -0.005, -0.05), r0 + (0.045, -0.005, -0.13), r0 + (0.035, 0.0, -0.22),
             r0 + (0.01, 0.0, -0.3), r0 + (0.015, 0.015, -0.36), r0 + (0.055, 0.02, -0.385), r0 + (0.085, 0.015, -0.37)]
    k.add("trail", outward(tube(trail, [0.021, 0.022, 0.021, 0.02, 0.019, 0.018, 0.016, 0.014], seg=16)), PINK, "head",
          fuse=False, surface="skin")


def lay_eyes(k, state):
    CW, CR, CK, CN = srgb(WHITE), srgb(RED), srgb(INK), srgb(BLACK)
    # the base's almond, its brow cut slanting down toward the nose; here v runs toward the crown (down)
    cut = polygon([(-1.3, -1.3), (1.3, -1.3), (1.3, 0.7), (-1.3, 0.0)])
    def almond(th):
        return min(1.0, cut(th))
    def eye(u, v):
        c = np.tile(CW, (len(u), 1))
        c[((u + 0.1) / 0.62) ** 2 + ((v + 0.05) / 0.85) ** 2 < 1] = CR
        c[((u + 0.1) / 0.26) ** 2 + ((v + 0.05) / 0.4) ** 2 < 1] = CK
        return c
    look = eye_look(state, eye, CN, srgb(LINE), line=0.16)
    up = (0, 0, -1.0)
    for s in (1, -1):
        E = np.array([0.04 * s, -0.087, 0.44])
        d = unit(0.42 * s, -1, -0.1)
        if s > 0:        # u runs inward here: the picture and its outline turned
            k.decal(E, d, (0.026, 0.02), lambda u, v: look(-u, v), up=up, eps=0.002, rings=20, segs=56,
                    outline=lambda th: almond(math.pi - th))
        else:
            k.decal(E, d, (0.026, 0.02), look, up=up, eps=0.002, rings=20, segs=56, outline=almond)


def greninja_mega(src, out):
    k = Kit(src, coat="scales")
    shuriken(k)
    body(k)
    legs(k)
    arms(k)
    head(k)
    tongue(k)
    k.build(voxel=0.0062, smooth=4, band=0.008, wband=0.04)
    k.eye_states(lambda s: lay_eyes(k, s))
    k.export(out)


if __name__ == "__main__":
    greninja_mega(*args())
