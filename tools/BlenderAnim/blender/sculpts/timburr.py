# Timburr sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/timburr.py -- <the export's Timburr.glb> <out.glb>
# From the references: a small grey-beige imp with a big head whose crown sweeps back into one broad rounded lobe; a
# pink vein bulging on one side of the head (the model's -X side, as the pictures show it on the viewer's left);
# big round eyes, white with a large grey iris and a black pupil, a heavy dark upper line sloping down to the nose; a
# big dark bulb of a nose; a toothy grin under it. A small body, pink muscle bulging round on each shoulder with a
# rounded point standing up and out behind it, short arms with three-fingered hands, stubby splayed legs with pink on
# the outer thighs and flat feet with pointed toes. A squared wooden log, end grain pale, carried in both hands across
# the front (the pictures hold it under one arm; held so in the rest pose it would make the model wide).
# Reference ratios (3D picture, crown to feet, log not counted): head / height 0.46, head width (no lobe) / height 0.44,
# body width / height 0.38, legs showing / height 0.2, log length / height 1.15 (0.88 here), log depth / height 0.26.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, slab, srgb, inside, unit, eye_look, polygon, outward, args

GREY, PINK, NOSE, INK = "#C4BBAD", "#EE7C94", "#45403F", "#2A2426"
IRIS, WOOD, WOODDARK, GRAIN = "#7E7C80", "#8C5C38", "#5E3A22", "#E4AC6C"
MOUTH = "#3A2A2C"

H = np.array([0.0, -0.022, 0.272])           # the head's middle (the head bone is at 0, -0.01, 0.21)
HR = np.array([0.082, 0.074, 0.074])


def front_of(c, r, x, z):
    """The point of an ellipsoid's front (-Y) with these x and z, and its outward normal."""
    c, r = np.array(c, float), np.array(r, float)
    q = 1 - ((x - c[0]) / r[0]) ** 2 - ((z - c[2]) / r[2]) ** 2
    y = c[1] - r[1] * math.sqrt(max(q, 0.0))
    p = np.array([x, y, z])
    n = (p - c) / r ** 2
    return p, n / np.linalg.norm(n)


def foot(k, at, r, colour, bone, toes=2, toe_len=0.6):
    """A flat foot with pointed toes splayed forward: the line's foot."""
    at = np.array(at, float)
    k.add("foot", ell(at, r), colour, bone, mirror=True)
    for i in range(toes):
        a = (i - (toes - 1) / 2) * 0.6 + 0.25
        d = unit(math.sin(a), -math.cos(a), 0)
        b = at + np.array([d[0] * r[0] * 0.5, d[1] * r[1] * 0.55, -r[2] * 0.15])
        k.add(f"toe{i}", tube([b, b + d * r[1] * toe_len], [r[2] * 0.95, r[2] * 0.45]), colour, bone, mirror=True)


def hand(k, palm, pr, dirs, length, radius, colour, bone):
    """A palm and short tapered fingers along dirs (on +X; mirrored)."""
    palm = np.array(palm, float)
    k.add("palm", ell(palm, pr), colour, bone, mirror=True)
    for i, d in enumerate(dirs):
        d = unit(*d)
        a = palm + d * pr[0] * 0.6
        k.add(f"finger{i}", tube([a, a + d * length * 0.6, a + d * length], [radius, radius * 0.9, radius * 0.6]),
              colour, bone, mirror=True)


def eye_patch(k, E, nrm, half, open_eye, lid, ink, top, s, line=0.16):
    """One eye laid by hand on side s (+1 +X, -1 -X): its outline an ellipse cut along top(u) (u outward)."""
    th = np.linspace(0, 2 * math.pi, 48, endpoint=False)
    pts = [(math.cos(t), min(0.86 * math.sin(t), float(top(math.cos(t))))) for t in th]
    shape = polygon([(u * s, v) for u, v in pts])
    look = open_eye
    col = look if s > 0 else (lambda u, v, f=look: f(-u, v))
    k.decal(E * np.array([s, 1, 1]), nrm * np.array([s, 1, 1]), half, col, up=(0, 0.1, 1), eps=0.003,
            outline=shape, rings=20, segs=56)


def timburr(src, out):
    k = Kit(src, coat="fur")

    # The head: big and round, its crown swept back into one broad rounded lobe
    k.add("head", ell(H, HR), GREY, "head")
    # (the crown runs up into it with no crease: a fill between, and the lobe leaning a little to +X as drawn)
    k.add("lobe", ell((0.01, 0.038, 0.312), (0.076, 0.088, 0.056), rot=(-42, 10, 0)), GREY, "head")
    k.add("cheek", ell((0.0, -0.045, 0.235), (0.07, 0.05, 0.04)), GREY, "head")
    # the pink vein on the -X side of the head, bulging, its foot split in two
    k.add("veinhead", ell((-0.07, -0.025, 0.305), (0.022, 0.03, 0.026), rot=(0, 20, 0)), PINK, "head")
    k.add("veinhead2", ell((-0.065, -0.05, 0.275), (0.016, 0.02, 0.016)), PINK, "head")
    # the dark bulb of a nose, loose so its edge stays crisp
    k.add("nose", outward(ell((0.0, -0.098, 0.25), (0.028, 0.024, 0.022))), NOSE, "head", fuse=False, surface="skin")

    # The body: a small round trunk on round hips
    k.add("neck", ell((0, -0.005, 0.2), (0.05, 0.045, 0.03)), GREY, "body")
    k.add("torso", ell((0, 0.0, 0.15), (0.06, 0.054, 0.062)), GREY, "body")
    k.add("hips", ell((0, 0.006, 0.098), (0.066, 0.056, 0.045)), GREY, "body")

    # The arms: a round pink shoulder, a rounded point standing up and out behind it, short arms down to the log
    SH = np.array([0.066, -0.004, 0.182])
    k.add("shoulder", ell(SH + (0.006, 0, 0), (0.032, 0.03, 0.03)), PINK, "arm{s}", mirror=True)
    k.add("spur", tube([SH + (0.01, 0.012, 0.012), (0.098, 0.026, 0.21), (0.11, 0.034, 0.224)], [0.02, 0.014, 0.007]),
          GREY, "arm{s}", mirror=True)
    EL, WR = np.array([0.098, -0.04, 0.155]), np.array([0.092, -0.1, 0.152])
    k.add("arm", tube([SH + (0.01, -0.01, -0.01), EL, WR], [0.02, 0.019, 0.017]), GREY, "arm{s}", mirror=True)
    hand(k, (0.09, -0.112, 0.146), (0.019, 0.017, 0.016),
         [(0.35, -0.6, -0.7), (0.0, -0.65, -0.75), (-0.35, -0.6, -0.7)], 0.028, 0.0085, GREY, "arm{s}")

    # The legs: splayed short thighs with pink on their outer sides, a stub of shin, flat feet
    k.add("thigh", ell((0.042, 0.0, 0.068), (0.036, 0.036, 0.04)), GREY, "leg{s}", mirror=True)
    k.add("veinthigh", ell((0.068, -0.012, 0.07), (0.016, 0.022, 0.022)), PINK, "leg{s}", mirror=True)
    k.add("shin", tube([(0.048, -0.002, 0.05), (0.058, -0.01, 0.02)], [0.022, 0.019]), GREY, "leg{s}", mirror=True)
    foot(k, (0.062, -0.022, 0.011), (0.03, 0.04, 0.011), GREY, "leg{s}")

    # The log: a squared beam of wood across the front, pale end grain, one dark split along its face
    c = np.array([0.0, -0.11, 0.108]); ang = math.radians(10)
    ax = np.array([math.cos(ang), math.sin(ang), 0.0]); ay = np.array([-math.sin(ang), math.cos(ang), 0.0])
    az = np.array([0, 0, 1.0])
    L, D, T = 0.152, 0.03, 0.033
    corners = [c + ax * sx * L + ay * sy * D + az * sz * T for sx in (-1, 1) for sy in (-1, 1) for sz in (-1, 1)]
    corners += [c + ax * L * 0.4 + az * (T + 0.002) + ay * 0.004, c - ax * L * 0.3 - ay * (D + 0.002) - az * 0.006]
    k.add("log", hull(corners), WOOD, "arm{s}".format(s="R"), fuse=False, surface="shell")
    for s in (1, -1):
        e = c + ax * s * (L + 0.003)
        k.add(f"grain{s}", slab([(-D * 0.94, -T * 0.94), (D * 0.94, -T * 0.94), (D * 0.94, T * 0.94),
                                 (-D * 0.94, T * 0.94)], e, ay, az, 0.006), GRAIN, "armR", fuse=False, surface="shell")
    k.add("split", slab([(-0.1, -0.003), (0.06, -0.006), (0.11, -0.002), (0.11, 0.004), (0.02, 0.004), (-0.1, 0.002)],
                        c - ay * (D + 0.0015) + az * 0.004, ax, az, 0.004), WOODDARK, "armR", fuse=False,
          surface="shell")

    # The grin: a white row of teeth in a dark rim under the nose, a little toward -X as the pictures show it
    CS, CM, CW = srgb(GREY), srgb(MOUTH), np.array([0.96, 0.95, 0.92])
    def grin(u, v):
        c_ = np.tile(CS, (len(u), 1))
        top, bot = 0.45 - 0.15 * u * u, -0.75 + 1.1 * u * u
        m = (v < top) & (v > bot) & (np.abs(u) < 0.95)
        c_[m] = CM
        c_[(v < top - 0.22) & (v > bot + 0.25) & (np.abs(u) < 0.8)] = CW
        return c_
    MP, MN = front_of(H + (0, 0, 0), HR, -0.012, 0.222)
    k.decal(MP, unit(-0.1, -1, -0.35), (0.034, 0.018), grin, eps=0.003, rings=12, segs=56)

    k.build(voxel=0.003, smooth=4, band=0.003)

    # The eyes: big and round, white, a large grey iris and a black pupil, a heavy dark line along the top sloping
    # down toward the nose
    CK, CI = srgb(INK), srgb(IRIS)
    def top(u):
        return 0.62 + 0.22 * u
    def open_eye(u, v):
        c_ = np.tile(np.array([1.0, 1.0, 1.0]), (len(u), 1))
        r = u * u + v * v
        c_[r > 0.88 ** 2] = CK
        c_[((u + 0.05) / 0.62) ** 2 + ((v + 0.05) / 0.68) ** 2 < 1] = CI
        c_[((u + 0.07) / 0.32) ** 2 + ((v + 0.06) / 0.36) ** 2 < 1] = CK
        c_[((u - 0.15) / 0.13) ** 2 + ((v - 0.18) / 0.13) ** 2 < 1] = (1, 1, 1)
        c_[v > top(u) - 0.26] = CK
        return c_
    E, EN = front_of(H, HR, 0.034, 0.272)
    EN = unit(EN[0] * 1.0, EN[1], EN[2] + 0.05)
    def lay(state):
        look = eye_look(state, open_eye, CS, CK, line=0.16)
        for s in (1, -1):
            eye_patch(k, E, EN, (0.024, 0.026), look, CS, CK, top, s)

    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    timburr(*args())
