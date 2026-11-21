# Blacephalon sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/blacephalon.py -- <the export's Blacephalon.glb> <out.glb>
#
# From the references: a clown from beyond with no face. Its largest mass is its head, a white ball dotted pink and
# blue all over, with a jagged star on each side (blue, a pink star inside it, a yellow dot in the middle); the ball
# floats a little over a broad white collar like a cloud, ruffled at its rim, with a dark hole in its top under the
# ball. Under the collar a thin neck striped yellow and blue, then a round body like balloon trousers, striped
# yellow, blue and pink on a slant, a white pompom at its side; thin arms, the one on its right (viewer's left, -X)
# magenta, the other blue, ending in white puffed gloves (flung wide in the art; here forward and down); thin white
# legs flaring into white boots with pointed toes curling up, a blue ball (on -X) and a pink ball (on +X) hanging at
# the knees.
# Skeleton: body (0, 0, 0.32), arms (+-0.05, 0, 0.38), head (0, 0, 0.53), legs (+-0.03, 0, 0.26).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, ring_band, srgb, inside, smoothstep, unit, eye_look, polygon, args

WHITE, PINK, BLUE, YELLOW, HOLE = "#F4F3F1", "#E2449A", "#1F9DCB", "#F1E04A", "#3A3438"
SHADE = "#D9D8D6"
HC, HRAD = np.array([0.0, -0.005, 0.622]), 0.104   # the head ball, clear of the collar
CC = np.array([0.0, 0.0, 0.47])                    # the collar
BODY_C, BODY_R = np.array([0.0, 0.0, 0.33]), np.array([0.084, 0.072, 0.074])
VOXEL = 0.0055


def collar_shape(V):
    """A flat cloud: a disc whose rim is ruffled in lobes, drooping a little at the edge."""
    V = V.copy()
    th = np.arctan2(V[:, 1], V[:, 0])
    rxy = np.hypot(V[:, 0], V[:, 1])
    s = 1 + 0.1 * np.sin(7 * th + 0.3) + 0.04 * np.sin(13 * th)
    V[:, 0] *= s; V[:, 1] *= s
    V[:, 2] = V[:, 2] - 0.55 * rxy ** 3 + 0.25 * np.sin(7 * th + 1.2) * rxy ** 4
    return V


def head(k):
    k.add("ball", ell(HC, (HRAD, HRAD, HRAD), sub=5), WHITE, "head")
    k.add("collar", ell(CC, (0.185, 0.165, 0.03), deform=collar_shape, sub=5), WHITE, "head")
    # the hole in the collar's top, just in front of the ball's foot (only its top side: z over the collar's middle)
    k.paint(lambda P: inside(np.hypot(P[:, 0] / 0.03, (P[:, 1] + 0.122) / 0.016) - 1, 0.08)
            * inside(CC[2] + 0.012 - P[:, 2], 0.003), HOLE, on=["collar"])


def head_marks(k):
    """Dots over the ball by a golden spiral, kept off the stars and the ball's underside."""
    golden = math.pi * (3 - math.sqrt(5))
    stars = [unit(s * 0.82, -0.42, 0.25) for s in (1, -1)]
    n = 46
    for i in range(n):
        zz = 1 - 2 * (i + 0.5) / n
        if zz < -0.18:
            continue
        r = math.sqrt(1 - zz * zz)
        a = i * golden
        d = np.array([r * math.cos(a), r * math.sin(a), zz])
        if any(np.dot(d, s) > 0.8 for s in stars):
            continue
        k.decal(HC + d * HRAD, d, (0.011, 0.011), PINK if i % 2 else BLUE, eps=0.002, rings=5, segs=24)
    for s, d in zip((1, -1), stars):
        star = lambda r1, r2, m: [(r1 * math.cos(math.pi / 2 + j * math.pi / m) if j % 2 == 0 else
                                   r2 * math.cos(math.pi / 2 + j * math.pi / m),
                                   r1 * math.sin(math.pi / 2 + j * math.pi / m) if j % 2 == 0 else
                                   r2 * math.sin(math.pi / 2 + j * math.pi / m)) for j in range(2 * m)]
        c = HC + d * HRAD
        k.decal(c, d, (0.045, 0.045), BLUE, eps=0.002, outline=polygon(star(1.0, 0.5, 6)), rings=10, segs=96)
        k.decal(c, d, (0.03, 0.03), PINK, eps=0.003, outline=polygon(star(1.0, 0.5, 6)), rings=8, segs=96)
        k.decal(c, d, (0.011, 0.011), YELLOW, eps=0.004, rings=5, segs=32)


def body(k):
    k.add("neck", tube([(0, 0.0, 0.47), (0, 0.0, 0.44), (0, 0.0, 0.39)], [0.024, 0.026, 0.034]), YELLOW, "body")
    k.add("trunk", ell(BODY_C, BODY_R, rot=(0, 10, 0)), YELLOW, "body")
    k.add("hip", ell((0, 0.0, 0.285), (0.074, 0.062, 0.048)), YELLOW, "body")
    # the stripes, slanting
    def band(lo, hi):
        return lambda P: inside(lo - (P[:, 2] + 0.35 * P[:, 0]), 0.002) * inside((P[:, 2] + 0.35 * P[:, 0]) - hi, 0.002)
    for lo, hi, col in ((0.43, 0.455, BLUE), (0.255, 0.283, BLUE), (0.0, 0.24, PINK)):
        k.paint(band(lo, hi), col, on=["neck", "trunk", "hip"])
    # the trunk's two broad stripes are loose rings on its slant, crisp where paint hatches
    ax = unit(0.35, 0, 1)
    for h, w, col in ((0.042, 0.028, BLUE), (-0.006, 0.03, PINK)):
        rad = lambda v, h=h, w=w: BODY_R[0] * np.sqrt(np.clip(1 - ((h + (v - 0.5) * w) / BODY_R[2]) ** 2, 0.05, 1)) + 0.0035
        V, F = ring_band(BODY_C + ax * h, ax, rad, w, thick=0.005)
        V = BODY_C + (V - BODY_C) * np.array([1, BODY_R[1] / BODY_R[0], 1])
        k.add(f"stripe{h}", (V, F), col, "body", fuse=False)
    k.add("pompom", ell((-0.072, -0.03, 0.4), (0.03, 0.03, 0.03)), WHITE, "body")


def arms(k):
    for s, col in ((1, BLUE), (-1, PINK)):
        sd = "R" if s > 0 else "L"
        sh = np.array([0.03 * s, -0.005, 0.42])
        el = np.array([0.085 * s, -0.05, 0.385])
        wr = np.array([0.065 * s, -0.115, 0.34])
        k.add("arm" + sd, tube([sh, el, wr], [0.014, 0.0125, 0.0125]), col, "arm" + sd)
        # the glove: a puffed cuff and a soft hand of three lumps
        k.add("cuff" + sd, ell(wr + (0.008 * s, -0.012, -0.004), (0.034, 0.03, 0.03)), WHITE, "arm" + sd)
        for j, off in enumerate(((0.012, -0.045, -0.006), (-0.012, -0.042, 0.004), (0.022, -0.03, 0.016))):
            k.add(f"glove{j}" + sd, ell(wr + np.array([off[0] * s, off[1], off[2]]), (0.022, 0.026, 0.02)), WHITE,
                  "arm" + sd)


def legs(k):
    for s, col in ((1, PINK), (-1, BLUE)):
        sd = "R" if s > 0 else "L"
        bone = "leg" + sd
        hip = np.array([0.03 * s, 0.0, 0.28])
        knee = np.array([0.038 * s, 0.0, 0.17])
        ankle = np.array([0.045 * s, 0.01, 0.06])
        k.add("thigh" + sd, tube([hip, (hip + knee) / 2, knee], [0.02, 0.015, 0.0145]), WHITE, bone)
        k.add("boot" + sd, tube([knee, (knee + ankle) / 2 + (0, 0, 0.005), ankle], [0.0145, 0.019, 0.026]), WHITE, bone)
        k.add("foot" + sd, ell(ankle + (0.004 * s, -0.03, -0.035), (0.03, 0.045, 0.024)), WHITE, bone)
        k.add("toe" + sd, tube([ankle + (0.004 * s, -0.06, -0.035), ankle + (0.006 * s, -0.095, -0.03),
                                ankle + (0.006 * s, -0.112, -0.008)], [0.02, 0.013, 0.0105]), WHITE, bone)
        # the ball at the knee, on a short white tie
        ball = knee + (0.04 * s, -0.016, -0.004)
        k.add("tie" + sd, tube([knee + (0.01 * s, 0, 0.0), ball], [0.0105, 0.0105]), WHITE, bone)
        k.add("kneeball" + sd, ell(ball + (0.004 * s, -0.002, -0.004), (0.031, 0.031, 0.031)), col, bone,
              fuse=False, surface="shell")


def eyes(k, state):
    """No face: the five states are patches of the ball's own white on its front, clear of the dots."""
    look = eye_look(state, lambda u, v: np.tile(srgb(WHITE), (len(u), 1)), srgb(WHITE), srgb(WHITE))
    for s in (1, -1):
        d = unit(0.2 * s, -1, -0.35)
        k.decal(HC + d * HRAD, d, (0.008, 0.008), look, eps=0.0012, rings=20, segs=56)


if __name__ == "__main__":
    src, dst = args()
    k = Kit(src, coat="fur")
    head(k)
    head_marks(k)
    body(k)
    arms(k)
    legs(k)
    k.build(voxel=VOXEL, smooth=5, band=0.006, wband=0.04)
    k.eye_states(lambda state: eyes(k, state))
    k.export(dst)
