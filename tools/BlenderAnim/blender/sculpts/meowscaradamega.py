# Mega Meowscarada (a Mega of our own, plan 06 · R20b) sculpted on the skeleton and clips of the game's own export of
# the form (sculptkit.py):
#   blender --background --factory-startup --python sculpts/meowscaradamega.py -- <the export's Meowscarada-Mega.glb> <out.glb>
# Grown from meowscarada.py through sibling (its head, hood, locks, eyes, mask, marks, body, legs and skirt, called as
# they are), with what PokemonModels.Ours.Megas.cs's MeowscaradaMega says the Mega changes, largest mass first:
#   1. its cape of leaves grown great enough to fall to the ground: four fronds from the shoulders to a hem of four
#      points on the ground, the two inner ones mid green and the outer two dark, lined pink, two dark leaf blades at
#      each point (the cloak of the base, to the knees, goes);
#   2. its white turned to a dark leaf green (the hood, hands and legs keep the base's darker green);
#   3. the ruff of petals at its throat opened into a full bloom of nine petals, pink and violet by turns, round a
#      deep pink heart (the base's collar goes);
#   4. a flower bomb spinning on one fingertip of its raised right paw: a stalk, a disc and six pale olive petals
#      round it, tilted toward the front camera so its face shows (the sculptor lays it flat).
# The rest is the base's. No pictures of a Mega of our own exist: the export's own sheet is the design.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, sheet, outward, unit, polygon, args, sibling

M = sibling("meowscarada")
sp = M.sp
BODY = "#466E4A"          # the Mega's body: its white turned to a dark leaf green
EYERIM = "#B9E6A8"        # the eye's rim kept pale, so the eyes stay the brightest thing on the face
CAPE, CAPEOUT = "#4C9854", "#285C36"
LINING = "#EC6EA0"
BLOOM, BLOOMV, HEART = "#EC6EA0", "#AA64C8", "#C83C5A"
BOMB, BOMBPETAL = "#96965A", "#B0B87C"

M.WHITE = BODY            # every part the base made white is the Mega's green (read at call time)

# The raised right paw (+X, armR): shoulder, elbow, wrist
SH, EL, WR = np.array([0.07, 0.0, 0.628]), np.array([0.142, -0.03, 0.69]), np.array([0.152, -0.055, 0.795])
SPIN = np.array([0.154, -0.075, 0.915])


def along(c, d, across, r, sub=3):
    """An ellipsoid laid along d: r is (half width across, half thickness, half length along d)."""
    d = unit(*d)
    a = np.array(across, float); a = a - d * (a @ d); a /= np.linalg.norm(a)
    n = np.cross(d, a)
    V, F = ell((0, 0, 0), r, sub=sub)
    return outward((V @ np.stack([a, n, d]) + np.array(c, float), F))


def add_arms(k):
    """The base's arms, the right one then raised to hold the flower bomb up at the head's side."""
    M.add_arms(k)
    k.parts = [p for p in k.parts if not (p["bone"] == "armR")]
    k.add("upperarmR", tube([SH, EL], [0.04, 0.042]), BODY, "armR")
    k.add("sleeveR", ell((0.118, -0.018, 0.655), (0.07, 0.072, 0.078), rot=(-20, 30, 0)), BODY, "armR")
    k.add("forearmR", tube([EL + np.array([0.0, -0.01, 0.01]), WR], [0.03, 0.027]), BODY, "armR")
    k.add("handR", ell(WR + np.array([0, -0.006, 0.02]), (0.03, 0.03, 0.032)), M.HOOD, "armR")
    for j, dx in enumerate((-0.016, 0.0, 0.016)):
        base = WR + np.array([dx, -0.02, 0.045])
        k.add(f"clawR{j}", sp.spike(base, unit(0, -0.4, 1), 0.02, 0.007, 0.007), M.CLAW, "armR", fuse=False)
    k.add("palmR", outward(ell(WR + np.array([0, -0.034, 0.018]), (0.016, 0.008, 0.014), sub=3)), M.PINK, "armR",
          fuse=False)
    # the flower bomb spinning on the middle claw's tip: a stalk, a disc, six petals, tilted toward the front
    k.add("stalk", tube([WR + np.array([0, -0.01, 0.05]), SPIN], [0.014, 0.012]), M.HOOD, "armR", fuse=False,
          surface="leaf")
    tilt = unit(0, -0.55, 1)                          # the disc's face looks up and to the front
    a = unit(1, 0, 0)
    b = np.cross(tilt, a)
    k.add("bombdisc", along(SPIN, tilt, a, (0.024, 0.024, 0.011)), BOMB, "armR", fuse=False, surface="leaf")
    for i in range(6):
        t = i * math.pi / 3
        d = a * math.cos(t) + b * math.sin(t)
        k.add(f"bombpetal{i}", along(SPIN + d * 0.048, d, np.cross(tilt, d), (0.02, 0.009, 0.036)), BOMBPETAL,
              "armR", fuse=False, surface="leaf")


def add_bloom(k):
    """The full bloom at the throat: nine petals round a deep pink heart, facing the front."""
    c = np.array([0.0, -0.078, 0.66])
    for i in range(9):
        t = i * 2 * math.pi / 9 + math.pi / 2
        d = np.array([math.cos(t), -0.28, math.sin(t) * 0.7 - 0.12])
        d = d / np.linalg.norm(d)
        k.add(f"bloom{i}", along(c + d * 0.042, d, np.cross(d, unit(0, -1, 0)), (0.024, 0.011, 0.036)),
              BLOOM if i % 2 == 0 else BLOOMV, "body", fuse=False, surface="leaf")
    k.add("bloomheart", outward(ell(c + np.array([0, -0.012, 0.0]), (0.02, 0.014, 0.02), sub=3)), HEART, "body",
          fuse=False, surface="leaf")


def cape_surface(r_top, r_bot, z_top, z_bot, th_top, th_bot, notch, s0, s1):
    """The cape between s0 and s1 of its width (-1 front edge on -X, round the back, to 1 on +X): from the shoulders to
    a hem whose four points (at |s| 0.25 and 0.75) touch the ground."""
    def at(u, v):
        s = s0 + (s1 - s0) * (0.004 + 0.992 * u)
        a = np.abs(s)
        th = np.radians(s * (th_top + (th_bot - th_top) * v))
        bottom = z_bot + notch * (0.5 + 0.5 * np.cos(4 * math.pi * a)) + 0.03 * a
        z = z_top + (bottom - z_top) * v
        r = r_top + (r_bot - r_top) * v ** 0.75 + 0.02 * np.sin(math.pi * v)
        return np.stack([r * np.sin(th), r * np.cos(th), z], axis=-1)
    return at


def add_cape(k):
    args_ = dict(r_top=0.1, r_bot=0.33, z_top=0.665, z_bot=0.012, th_top=110, th_bot=132, notch=0.13)
    k.add("capemid", outward(sheet(cape_surface(s0=-0.51, s1=0.51, **args_), 44, 30, 0.022)), CAPE, "body",
          fuse=False, surface="leaf")
    o = dict(args_, r_top=0.104, r_bot=0.336)
    k.add("capeoutR", outward(sheet(cape_surface(s0=0.49, s1=1.0, **o), 24, 30, 0.022)), CAPEOUT, "body",
          fuse=False, surface="leaf")
    k.add("capeoutL", outward(sheet(cape_surface(s0=-1.0, s1=-0.49, **o), 24, 30, 0.022)), CAPEOUT, "body",
          fuse=False, surface="leaf")
    i = dict(args_, r_top=0.086, r_bot=0.31, z_bot=0.03, th_top=108, th_bot=130)
    k.add("capelining", outward(sheet(cape_surface(s0=-1.0, s1=1.0, **i), 80, 30, 0.016)), LINING, "body",
          fuse=False, surface="fur")
    # two dark leaf blades out of each of the hem's four points
    surf = cape_surface(s0=-1.0, s1=1.0, **o)
    for n, s in enumerate((-0.75, -0.25, 0.25, 0.75)):
        u = (s + 1) / 2
        tip = surf(np.array([[(u - 0.004) / 0.992]]), np.array([[1.0]]))[0, 0]
        out = unit(tip[0], tip[1], 0)
        side = np.cross((0, 0, 1.0), out)
        for j, t in enumerate((-1, 1)):
            root = tip + side * t * 0.028 + np.array([0, 0, 0.03])
            d = unit(*(side * t * 0.8 + out * 0.5 + np.array([0, 0, 0.25])))
            k.add(f"hemblade{n}{j}", sp.spike(root, d, 0.065, 0.022, 0.012), CAPEOUT, "body", fuse=False,
                  surface="leaf")


def meowscarada_mega(src, out_path):
    k = Kit(src, coat="fur")
    M.add_body(k)
    add_arms(k)
    M.add_legs(k)
    M.add_head(k)
    M.add_hood(k)
    M.add_locks(k)
    add_bloom(k)
    add_cape(k)
    M.add_marks(k)
    at, n = sp.on_head(M.EYE_X, M.H[2] + M.EYE_Z, M.H, M.HR)
    n = unit(n[0] * 1.1, n[1], n[2])
    rim = (M.EYE_HALF[0] * 1.07, M.EYE_HALF[1] * 1.14)
    k.decal(at, n, rim, EYERIM, eps=0.0011, outline=polygon(M.EYE_SHAPE), rings=12, segs=56)
    q = np.array([-1, 1, 1.0])
    k.decal(at * q, n * q, rim, EYERIM, eps=0.0011, outline=polygon([(-a, b) for a, b in M.EYE_SHAPE]), rings=12,
            segs=56)
    k.build(voxel=0.0075, smooth=4, band=0.006, wband=0.06)
    k.eye_states(lambda s: sp.lay_eyes(k, s, at, n, M.EYE_HALF,
                                       sp.eye_colour(iris=M.PINK, iris_dark=M.PINKDARK, iris_r=0.8, pupil_w=0.22,
                                                     pupil_h=0.44),
                                       M.HOOD, EYERIM, outline=M.EYE_SHAPE))
    k.export(out_path)


if __name__ == "__main__":
    meowscarada_mega(*args())
