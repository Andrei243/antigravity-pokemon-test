# Vibrava sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/vibrava.py -- <the export's Vibrava.glb> <out.glb>
# From the references: a pale yellow-green dragonfly larva grown wings. A round cream head with two great green
# bulging eyes (a black pupil, a white glint, a darker green rim) on its sides, two white domes on its crown, two long
# cream mandibles reaching forward and down. A short segmented cream thorax and a long segmented tail rising behind,
# two small green diamonds at its end in a V. Four large diamond wings, green in a thick dark grey rim, raised in a V
# and swept apart (front pair up and forward, hind pair out and back). Six thin dark grey legs bent high at the knee,
# standing on the ground, each foot a little fork.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, slab, srgb, inside, smoothstep, unit, eye_look, args

CREAM, CREAMDARK, GREEN, GREENDARK, RIM, WHITE, INK = \
    "#E6E2A2", "#B9B676", "#4FB23C", "#2C7A2A", "#3E4440", "#F4F6F2", "#141414"


def wing_axes(s, sweep, lift, twist):
    """yanmega.py's: the span (ex) out to side s, swept forward by sweep, raised by lift; ez across it."""
    sw, li = math.radians(sweep), math.radians(lift)
    ex = np.array([s * math.cos(li) * math.cos(sw), -math.cos(li) * math.sin(sw), math.sin(li)])
    ez = np.array([0.0, 1.0, math.tan(math.radians(twist))])
    ez = ez - ex * np.dot(ez, ex)
    return ex, ez / np.linalg.norm(ez)


def diamond(L, W, at=0.45):
    return [(0.0, 0.0), (at * L, W), (L, 0.0), (at * L, -W)]


def inset(outline, f):
    p = np.array(outline, float)
    c = p.mean(axis=0)
    return (c + (p - c) * f).tolist()


def diamond_wing(k, name, root, bone, L, W, sweep, lift, twist, rim=RIM, fill=GREEN, thick=0.007, at=0.45, f=0.7):
    """A diamond wing a side: a dark plate, a thicker green plate inset in it so that the green shows on both faces."""
    for s, side in ((1, "R"), (-1, "L")):
        o = np.array(root, float) * (s, 1, 1)
        ex, ez = wing_axes(s, sweep, lift, twist)
        b = bone.format(s=side)
        out = diamond(L, W, at)
        k.add(f"{name}{side}", slab(out, o, ex, ez, thick), rim, b, fuse=False, surface="shell")
        k.add(f"{name}in{side}", slab(inset(out, f), o, ex, ez, thick * 1.6), fill, b, fuse=False, surface="shell")


# --- the tail ------------------------------------------------------------------------------------------------------
TAIL = np.array([(0, 0.035, 0.215), (0, 0.12, 0.235), (0, 0.2, 0.27), (0.018, 0.265, 0.318), (0.045, 0.305, 0.362)])
TAIL_R = [0.036, 0.03, 0.024, 0.018, 0.014]


def along(P, pts):
    best_d = np.full(len(P), 9.0); best_s = np.zeros(len(P)); run = 0.0
    for a, b in zip(pts[:-1], pts[1:]):
        e = b - a; ln = np.linalg.norm(e)
        t = np.clip(((P - a) @ e) / (ln * ln), 0, 1)
        d = np.linalg.norm(P - (a + t[:, None] * e), axis=1)
        better = d < best_d
        best_d[better] = d[better]; best_s[better] = run + t[better] * ln
        run += ln
    return best_s, best_d


HP, HS = np.array([0.0, -0.06, 0.237]), 1.15       # the head grown about its joint


def hp(p):
    return HP + (np.array(p, float) - HP) * HS


H = hp([0.0, -0.085, 0.245])
HR = np.array([0.046, 0.048, 0.044]) * HS
EYE_C = hp([0.044, -0.098, 0.24])      # the bulging eye's middle (+X; mirrored)
EYE_R = 0.032 * HS
EYE_D = unit(0.78, -0.56, 0.12)


def vibrava(k):
    # --- body: a short round thorax, a waist into the tail -------------------------------------------------------------
    k.add("thorax", ell((0, -0.005, 0.22), (0.045, 0.058, 0.044)), CREAM, "body")
    k.add("tail", tube(TAIL, TAIL_R), CREAM, "tail")
    # dark rings dividing thorax and tail into segments
    def segs(P):
        s, d = along(P, TAIL)
        m = np.zeros(len(P))
        for x in (0.035, 0.085, 0.135, 0.185, 0.235):
            m = np.maximum(m, inside(np.abs(s - x) - 0.003, 0.0012))
        return m * inside(d - 0.045, 0.002)
    k.paint(segs, CREAMDARK, on=["tail"])
    k.paint(lambda P: inside(np.abs(P[:, 1] + 0.04) - 0.003, 0.0012) * inside(np.abs(P[:, 0]) - 0.05, 0.002)
            * inside(np.abs(P[:, 2] - 0.22) - 0.05, 0.002), CREAMDARK, on=["thorax"])

    # --- head: a round cream head, great bulging green eyes on its sides, two white domes on the crown ---------------
    k.add("head", ell(H, HR), CREAM, "head")
    k.add("snout", ell(hp((0, -0.125, 0.232)), np.array((0.026, 0.026, 0.022)) * HS), CREAM, "head")
    k.add("eyeball", ell(EYE_C, (EYE_R, EYE_R, EYE_R)), GREEN, "head", mirror=True, surface="shell")
    k.add("dome", ell(hp((0.024, -0.072, 0.284)), np.array((0.017, 0.02, 0.014)) * HS), WHITE, "head", fuse=False, mirror=True,
          surface="shell")
    # the mandibles: two long cream horns forward and down, a little apart
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        pts = (np.array([(0.012, -0.13, 0.225), (0.022, -0.18, 0.205), (0.03, -0.23, 0.18), (0.034, -0.27, 0.16)]) * HS + HP * (1 - HS)) * m
        k.add(f"mandible{side}", blade(pts, [0.016, 0.012, 0.009, 0.002], (0, 0, 1), 0.7), CREAM, "head",
              fuse=False, surface="shell")

    # --- wings: four great diamonds in a V; two small ones at the tail's end ------------------------------------------
    diamond_wing(k, "wingF", (0.03, -0.02, 0.252), "wing{s}", 0.29, 0.086, 30, 46, 30)
    diamond_wing(k, "wingH", (0.03, 0.03, 0.25), "wing{s}.001", 0.276, 0.08, -32, 24, -30)
    tip = TAIL[-1]
    for s, side in ((1, "R"), (-1, "L")):
        ex = unit(0.55 * s, 0.25, 0.8)
        ez = np.cross(ex, (0, 1, 0)) * s
        out = diamond(0.1, 0.034, 0.5)
        k.add(f"tailwing{side}", slab(out, tip, ex, ez, 0.007), RIM, "tail", fuse=False, surface="shell")
        k.add(f"tailwingin{side}", slab(inset(out, 0.62), tip, ex, ez, 0.011), GREEN, "tail", fuse=False,
              surface="shell")
    k.add("tailtip", ell(tip, (0.016, 0.016, 0.016)), CREAM, "tail")

    # --- legs: six short dark grey legs, each about as long as the body is deep, bent at a knee, splayed out and down,
    # ending well off the ground (the game holds the body up) in a small fork --------------------------------
    LEGS = [("legF", "leg{s}", (0.028, -0.045, 0.205), (0.07, -0.075, 0.218), (0.096, -0.1, 0.152)),
            ("legM", "body", (0.034, 0.0, 0.2), (0.08, 0.0, 0.213), (0.11, 0.0, 0.147)),
            ("legB", "body", (0.03, 0.045, 0.2), (0.075, 0.07, 0.213), (0.1, 0.096, 0.147))]
    for name, bone, hip, knee, foot in LEGS:
        hip, knee, foot = np.array(hip), np.array(knee), np.array(foot)
        k.add(name, tube([hip, knee, knee * 0.5 + foot * 0.5 + (0.008, 0, 0), foot], [0.012, 0.011, 0.0105, 0.01]),
              RIM, bone, fuse=False, mirror=True, surface="shell")
        out = unit(*(foot - hip) * (1, 1, 0))
        side = np.cross(out, (0, 0, 1.0))
        for j, sg in enumerate((1, -1)):
            d = unit(*(out + side * sg * 0.7))
            k.add(f"{name}toe{j}", tube([foot, foot + d * 0.024 + (0, 0, -0.006)], [0.0095, 0.008]), RIM, bone,
                  fuse=False, mirror=True, surface="shell")


# --- eyes: green, a darker rim, a large black pupil looking forward, a white glint ------------------------------------
CG, CD, CK, CW = srgb(GREEN), srgb(GREENDARK), srgb(INK), np.array([1.0, 1.0, 1.0])


def eye(u, v):
    c = np.tile(CG, (len(u), 1))
    r = np.sqrt(u * u + v * v)
    c[r > 0.84] = CD
    c[((u + 0.12) / 0.4) ** 2 + ((v + 0.02) / 0.46) ** 2 < 1] = CK
    c[((u + 0.0) / 0.13) ** 2 + ((v - 0.22) / 0.14) ** 2 < 1] = CW
    return c


def lay_eyes(k, state):
    look = eye_look(state, eye, CG, CD, line=0.15)
    for s in (1, -1):
        m = np.array([s, 1, 1.0])
        c = EYE_C * m + EYE_D * m * EYE_R
        col = look if s == 1 else (lambda a, b, f=look: f(-a, b))
        k.decal(c, EYE_D * m, (0.036, 0.036), col, eps=0.003, rings=20, segs=56)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="shell")
    vibrava(k)
    k.build(voxel=0.0032, smooth=4, band=0.005)
    k.eye_states(lambda st: lay_eyes(k, st))
    k.export(out)
