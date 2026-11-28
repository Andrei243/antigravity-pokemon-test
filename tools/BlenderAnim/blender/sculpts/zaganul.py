# Zaganul sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/zaganul.py -- <the export's Zaganul.glb> <out.glb>
#
# From its picture (docs/art/reference/gemini/carpathia/10105-Zaganul.jpg) and its brief ("105. Zaganul"): a bearded
# vulture standing upright. A pale cream head, shaggy, its feathers standing up and back in ragged points over the
# crown and nape; a black mask running from the beak's root through the eye and back along the side of the head; a red
# ring round each pale eye; a long hooked grey beak and a black beard of stiff bristles hanging under it; a great
# shaggy ruff of cream round the neck down onto the chest; a rust-orange breast and rust thighs; long dark slate-grey
# wings held raised and half open, their long flight feathers hanging down behind to the ground, plates of grey rock
# over their shoulders (the Rock type); a dark grey patch of rock plates on its right thigh; a long dark wedge of a
# tail; scaly red-brown legs, black talons gripping the bone it carries. The bone is the brief's ("it carries a bone
# in its talons") and stays, lying on the ground under its feet; the perch of the picture is that bone.
import os, sys, math
from types import SimpleNamespace
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, srgb, inside, tri, unit, eye_look, polygon, args, sibling, hull

g = sibling("pidgey")            # spike, poly_mask, mirrored, hooked, pointed
r_ = sibling("rufflet")          # tufts

# --- colours ------------------------------------------------------------------------------------------------------
CREAM, CREAM_SH = "#EFE4CA", "#D9CBA8"
RUST, RUST_DK = "#B4532C", "#8E3E22"
WING, WING_DK, WING_LT = "#3E4341", "#2B2F2E", "#5A605D"
ROCK, ROCK_LT = "#6C6B66", "#8A8882"
BLACK = "#1C1B1E"
BEAK, BEAK_DK = "#8C8884", "#5E5A58"
LEG, CLAW = "#9A4A3A", "#1E1C1E"
BONE, BONE_SH = "#E2D6BC", "#B8AC92"
RED, IRIS, INKH = "#C8322A", "#F2E8D2", "#141214"


def frame():
    F = SimpleNamespace()
    F.H = np.array([0, -0.085, 0.6])                # the head's centre, forward of its joint (0, -0.04, 0.527)
    F.HR = np.array([0.098, 0.1, 0.09])
    F.R = 0.1
    F.RUFF = ((0, -0.045, 0.49), (0.125, 0.11, 0.105))
    F.BODY = ((0, 0.01, 0.33), (0.135, 0.13, 0.175), (-10, 0, 0))
    F.CHEST = ((0, -0.06, 0.38), (0.12, 0.095, 0.12))
    F.THIGH = ((0.072, -0.03, 0.195), (0.066, 0.068, 0.08))
    F.SHOULDER = np.array([0.125, 0.035, 0.47])
    F.ELEV, F.SWEEP = 42, 36
    F.WS = 0.66                                     # the wing's outline in acviltor's units, scaled
    F.TAILBASE = np.array([0, 0.13, 0.24])
    F.LEGX, F.LEGY, F.LEGTOP, F.ANKLE = 0.068, -0.045, 0.14, 0.075
    F.LEGR = (0.024, 0.021)
    F.TOE, F.HEEL, F.TOER, F.CLAWLEN = 0.058, 0.04, 0.0135, 0.035
    F.BONE = (np.array([-0.17, -0.05, 0.028]), np.array([0.17, -0.06, 0.028]), 0.024)
    F.BEAK = ((0, -0.218, 0.585), (0.03, 0.068, 0.033), 10)
    F.BEAKLOW = ((0, -0.19, 0.556), (0.024, 0.042, 0.012), -6)
    F.EYEDIR = unit(0.6, -0.78, 0.16)
    F.EYE = (0.023, 0.021)
    F.VOXEL = 0.0062
    # the black mask in (angle round the head x R, z - the eye's height), right half from the top middle round to the
    # bottom middle: a band from the beak's root through the eye and back along the head's side
    F.MASK = [(0.0, 0.0), (0.04, 0.012), (0.09, 0.03), (0.15, 0.03), (0.2, 0.012), (0.23, -0.012), (0.17, -0.016),
              (0.11, -0.024), (0.06, -0.03), (0.02, -0.035), (0.0, -0.04)]
    return F


def eye_centre(F):
    return F.H + F.EYEDIR / np.linalg.norm(F.EYEDIR / F.HR)


def round_head(F, P):
    return np.arctan2(P[:, 0], -(P[:, 1] - F.H[1])) * F.R


# --- the parts ----------------------------------------------------------------------------------------------------
def body(k, F):
    c, r, rot = F.BODY
    k.add("body", ell(c, r, rot=rot, deform=r_.tufts(50, 0.32, 0.025, seed=5), sub=5), RUST, "body")
    k.add("chest", ell(*F.CHEST), RUST, "body")
    c, r = F.RUFF
    front = lambda D: (D[:, 1] < -0.6) & (D[:, 2] > 0.3)
    k.add("ruff", ell(c, r, deform=r_.tufts(40, 0.36, 0.16, skip=front, seed=9), sub=5), CREAM, "head")
    face = lambda D: (D[:, 1] < -0.45) & (D[:, 2] < 0.55) & (D[:, 2] > -0.6)
    k.add("head", ell(F.H, F.HR, deform=r_.tufts(40, 0.34, 0.1, skip=face, seed=7), sub=5), CREAM, "head")
    c, r = F.THIGH
    for s, b in ((1, "legR"), (-1, "legL")):
        k.add("thigh", ell((c[0] * s, c[1], c[2]), r, deform=r_.tufts(24, 0.4, 0.14, seed=11 + (s > 0)), sub=4),
              RUST, b)


def colours(k, F):
    """The ruff's cream down onto the chest in ragged points; the black mask through the eye; the back dark."""
    def ruff(P):
        a = np.arctan2(P[:, 0], -(P[:, 1] - 0.0))
        low = 0.43 - 0.04 * np.cos(a) + 0.018 * tri(a * 3.5)
        return inside(low - P[:, 2], 0.004)
    k.paint(ruff, CREAM, on=["chest", "body", "ruff"])
    k.paint(lambda P: inside(0.44 - P[:, 2], 0.004) * inside(-P[:, 1] + 0.03, 0.01) * inside(P[:, 2] - 0.62, 0.01),
            WING, on=["body"])
    E = eye_centre(F)
    poly = g.mirrored(F.MASK)
    k.paint(lambda P: g.poly_mask(round_head(F, P), P[:, 2] - E[2], poly, edge=0.0025)
            * (P[:, 1] < F.H[1] + 0.06), BLACK, on=["head"])
    # the rock patch on the right thigh
    k.paint(lambda P: inside(np.hypot((P[:, 0] - 0.1) / 0.05, (P[:, 2] - 0.22) / 0.06) - 1, 0.006) * (P[:, 0] > 0.06),
            ROCK, on=["thigh"])


def crest(k, F):
    """Ragged cream points standing up and back over the crown and down the nape: a shaggy head."""
    for j in range(14):
        a = math.radians(-80 + 160 * j / 13)
        up = 0.86 - 0.35 * abs(math.sin(a))
        base = F.H + np.array([math.sin(a) * F.HR[0] * 0.8, 0.02 + math.cos(a) * 0.03, F.HR[2] * up])
        tip = base + unit(math.sin(a) * 0.7, 0.75, 0.75 - 0.4 * abs(math.sin(a))) * (0.075 - 0.02 * abs(math.sin(a)))
        k.add("crest", g.spike(base, tip, 0.04, 0.02, side=(math.cos(a), -math.sin(a), 0), at=0.3), CREAM, "head",
              fuse=False)
    for j in range(8):
        a = math.radians(-70 + 140 * j / 7)
        base = np.array([math.sin(a) * 0.1, 0.03 + math.cos(a) * 0.06, 0.5])
        tip = base + unit(math.sin(a) * 0.8, 0.5 + 0.3 * math.cos(a), -0.6) * 0.07
        k.add("ruffpt", g.spike(base, tip, 0.045, 0.02, side=(math.cos(a), -math.sin(a), 0), at=0.3), CREAM_SH,
              "head", fuse=False)


def beak(k, F):
    c, r, rot = F.BEAK
    k.add("beak", ell(c, r, rot=(rot, 0, 0), deform=g.hooked), BEAK, "head", fuse=False, surface="shell")
    c, r, rot = F.BEAKLOW
    k.add("beaklow", ell(c, r, rot=(rot, 0, 0), deform=g.pointed), BEAK_DK, "head", fuse=False, surface="shell")
    # the beard: stiff black bristles hanging under the beak's root, a little forward
    for x, ln in ((-0.016, 0.06), (0.0, 0.075), (0.016, 0.06), (0.03, 0.045), (-0.03, 0.045)):
        base = np.array([x, -0.165, 0.548])
        tip = base + unit(x * 3, -0.35, -1.0) * ln
        k.add("beard", g.spike(base, tip, 0.024, 0.016, side=(1, 0, 0), at=0.25), BLACK, "head", fuse=False)


# --- the wings ------------------------------------------------------------------------------------------------------
COVERTS = [(0.0, -0.04), (0.15, -0.06), (0.3, -0.056), (0.42, -0.03), (0.5, 0.02), (0.48, 0.1), (0.38, 0.16),
           (0.24, 0.19), (0.1, 0.19), (0.0, 0.15)]
PRIMARIES = [(0.06, 0.13, 0.42), (0.13, 0.13, 0.46), (0.2, 0.125, 0.5), (0.27, 0.115, 0.52), (0.34, 0.1, 0.52),
             (0.4, 0.08, 0.48), (0.45, 0.05, 0.42)]


def feather(L, w):
    half = [(0.0, w * 0.36), (0.25 * L, w * 0.5), (0.72 * L, w * 0.48), (0.9 * L, w * 0.32), (L, 0.0)]
    return half + [(p, -q) for p, q in half[::-1][1:-1]]


def wing_axes(F):
    e, s = math.radians(F.ELEV), math.radians(F.SWEEP)
    d = np.array([math.cos(e) * math.cos(s), math.cos(e) * math.sin(s), math.sin(e)])
    c0 = np.array([0.12, 0.4, -0.9])
    c = c0 - d * (d @ c0); c /= np.linalg.norm(c)
    return d, c


def wings(k, F, thick=0.016):
    """Raised half open: a plate of dark coverts with grey rock over the shoulder, long dark flight feathers hanging."""
    d, c = wing_axes(F)
    n = np.cross(d, c)
    root = F.SHOULDER - d * 0.03
    W = F.WS
    k.add("wing", slab([(a * W, b * W) for a, b in COVERTS], root + n * 0.004, d, c, thick * 1.3), WING, "wing{s}",
          fuse=False, mirror=True)
    face = unit(0.3, -1.0, 0.1)
    for i, (a, b, L) in enumerate(PRIMARIES):
        f = unit(0.06 + 0.035 * i, 0.32, -1.0)
        ez = np.cross(face, f)
        o = root + d * a * W + c * b * W * 0.6 + face * (0.004 + 0.004 * i)
        k.add(f"primary{i}", slab(feather(L * W, 0.085 * W * 1.1), o, f, ez, thick), WING_DK if i % 2 else WING,
              "wing{s}", fuse=False, mirror=True)
    # grey rock over the shoulder: three flat-faced plates on the coverts
    for j, (a, b, sz) in enumerate(((0.05, 0.02, 0.05), (0.13, 0.05, 0.042), (0.08, 0.1, 0.038), (0.2, 0.03, 0.034))):
        p = root + d * a + c * b + n * 0.014
        pts = []
        for i in range(6):
            th = math.pi / 3 * i + 0.4 * j
            q = p + (d * math.cos(th) + c * math.sin(th)) * sz * (1 + 0.15 * math.sin(3 * th + j))
            pts.extend([q, q + n * 0.028])
        k.add(f"rock{j}", hull(pts), ROCK_LT if j % 2 else ROCK, "wing{s}", fuse=False, mirror=True, surface="shell")
    k.add("wingroot", ell(F.SHOULDER + np.array([0.0, 0.02, -0.01]), (0.06, 0.07, 0.07)), WING, "wing{s}",
          mirror=True)


def thigh_rocks(k, F):
    """The grey rock plates on its right thigh, where the picture has them."""
    for j, (y, z, sz) in enumerate(((-0.02, 0.24, 0.03), (0.02, 0.2, 0.026), (-0.04, 0.18, 0.022))):
        p = np.array([0.135, y, z])
        n = unit(1, -0.25, 0.1)
        a = unit(0, 1, 0.2); b = np.cross(n, a)
        pts = []
        for i in range(6):
            th = math.pi / 3 * i + j
            q = p + (a * math.cos(th) + b * math.sin(th)) * sz
            pts.extend([q - n * 0.01, q + n * 0.016])
        k.add(f"thighrock{j}", hull(pts), ROCK, "legR", fuse=False, surface="shell")


def tail(k, F, thick=0.016):
    """A long dark wedge of a tail hanging steeply down behind, swept out toward +X so its face shows."""
    base = F.TAILBASE
    for i, (yaw, ln, w) in enumerate(((0, 0.26, 0.08), (16, 0.24, 0.075), (-16, 0.24, 0.075), (30, 0.21, 0.07),
                                      (-30, 0.21, 0.07))):
        y = math.radians(yaw + 22)
        p = math.radians(-58)
        dd = np.array([math.sin(y) * math.cos(p), math.cos(y) * math.cos(p), math.sin(p)])
        side = np.array([math.cos(y), -math.sin(y), 0])
        half = [(a * ln, h * w / 2) for a, h in g.FEATHER]
        k.add(f"tail{i}", slab(half + [(a, -h) for a, h in half[::-1][1:-1]], base + np.array([0.01 * yaw / 16, 0, 0]),
                               dd, side, thick), WING_DK if i % 2 else WING, "tail", fuse=False)


def legs(k, F):
    """Scaly red-brown shins under the rust thighs, three toes forward and one back round the bone, black talons."""
    for s, side in ((1, "R"), (-1, "L")):
        x = F.LEGX * s
        top = np.array([x, F.LEGY, F.LEGTOP])
        ank = np.array([x * 1.04, F.LEGY - 0.005, F.ANKLE])
        k.add(f"shin{side}", tube([top, (top + ank) / 2, ank], [F.LEGR[0], F.LEGR[0] * 0.96, F.LEGR[1]]), LEG,
              f"leg{side}", fuse=False, surface="scales")
        for j, ang in enumerate((-30, 0, 30, 180)):
            rr = math.radians(ang + 6 * s)
            dd = np.array([math.sin(rr), -math.cos(rr), 0])
            ln = F.TOE if ang != 180 else F.HEEL
            tip = ank + dd * ln + np.array([0, 0, -F.ANKLE + F.TOER + 0.005])
            mid = (ank + tip) / 2 + np.array([0, 0, 0.012])
            k.add(f"toe{side}{j}", tube([ank, mid, tip], [F.TOER * 1.25, F.TOER * 1.05, F.TOER * 0.9]), LEG,
                  f"leg{side}", fuse=False, surface="scales")
            cd = unit(*(dd * 0.4 + np.array([0, 0, -1.0])))
            k.add(f"claw{side}{j}", g.spike(tip - dd * F.TOER * 0.3 + np.array([0, 0, F.TOER * 0.3]),
                                            tip + dd * F.CLAWLEN * 0.45 + cd * F.CLAWLEN * 0.6,
                                            F.TOER * 1.6, F.TOER * 1.4, side=np.cross(dd, (0, 0, 1.0))), CLAW,
                  f"leg{side}", fuse=False, surface="shell")


def bone(k, F):
    a, b, rad = F.BONE
    k.add("bone", tube([a, (a + b) / 2 + np.array([0, 0, 0.004]), b], [rad, rad * 0.85, rad], seg=20), BONE, "root",
          fuse=False, surface="shell")
    for e, sgn in ((a, -1), (b, 1)):
        for dy in (-1, 1):
            k.add("knob", ell(e + np.array([0.012 * sgn, 0.018 * dy, 0.002]), (0.024, 0.022, 0.024)), BONE_SH,
                  "root", fuse=False, surface="shell")


# --- the eyes: pale, a dark pupil, a red ring, the black mask round them ---------------------------------------------
INK, REDC, IRISC = srgb(INKH), srgb(RED), srgb(IRIS)


def eye_open(u, v):
    c = np.tile(IRISC, (len(u), 1))
    rr = np.hypot(u, v * 1.05)
    c[rr > 0.58] = REDC
    c[rr > 0.9] = INK
    c[np.hypot(u + 0.08, v) < 0.3] = INK
    c[np.hypot(u + 0.22, v - 0.2) < 0.12] = np.array([1.0, 1.0, 1.0])
    return c


def eyes(k, F):
    LID, LINE = srgb(BLACK), srgb(CREAM)
    E = eye_centre(F)
    m = np.array([-1, 1, 1.0])
    def lay(state):
        look = eye_look(state, eye_open, LID, LINE, line=0.18)
        k.decal(E, F.EYEDIR, F.EYE, look, eps=0.004, rings=20, segs=56)
        k.decal(E * m, F.EYEDIR * m, F.EYE, lambda u, v, look=look: look(-u, v), eps=0.004, rings=20, segs=56)
    return lay


def board(ob):
    V = np.array([v.co[:] for v in ob.data.vertices])
    h, w, l = V[:, 2].max(), np.ptp(V[:, 0]), np.ptp(V[:, 1])
    a = math.radians(31.5)
    proj = V[:, 0] * math.cos(a) + V[:, 1] * math.sin(a)
    print("KIT: board: corners %.3f (0.853 w + 0.522 l), shows %.3f, in a square of %.3f"
          % (0.853 * w + 0.522 * l, np.ptp(proj), 1.3 * max(h, 0.85 * max(w, l))))


def zaganul(src, out_path):
    F = frame()
    k = Kit(src, coat="fur")
    body(k, F)
    colours(k, F)
    crest(k, F)
    beak(k, F)
    wings(k, F)
    thigh_rocks(k, F)
    tail(k, F)
    legs(k, F)
    bone(k, F)
    ob = k.build(voxel=F.VOXEL, band=0.006)
    board(ob)
    k.eye_states(eyes(k, F))
    k.export(out_path)


if __name__ == "__main__":
    zaganul(*args())
