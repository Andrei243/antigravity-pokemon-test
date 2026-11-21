# Heliolisk sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/heliolisk.py -- <the export's Heliolisk.glb> <out.glb>
# Helioptile grown tall (built by helioptile.py's functions: the line's yellow and dark grey, its blue eye, its dark
# toes and its tail with a dark tip). From the references: an upright yellow lizard whose head and neck are dark grey:
# the frill is folded down round the neck as a bell-shaped hood to the shoulders, its rim set all round with orange
# points. A dark head carried forward with a short pointed snout, the mouth open (the upper jaw dark, the lower jaw
# yellow, pink inside), a round blue eye on each side of the head and two short points at the back of the crown. A
# yellow body, thin yellow arms forward and down with dark three-fingered hands; great yellow haunches fused into the
# hips, each with an orange band with a zigzag of darker orange, short shins with another such band at the ankle,
# dark feet; a long yellow tail back and out to the +X side with an orange band and a dark tip.
# The pictures overrule the task's text: the frill is folded down as a hood, not a disc spread round the neck.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, lathe, srgb, inside, tri, unit, args, sibling

P = sibling("helioptile")
YELLOW, GREY, CLAW = P.YELLOW, P.GREY, P.CLAW
ORANGE, ZIG = "#F29436", "#B85A22"
PINK = "#E98A98"
VOXEL = 0.0055

HC = np.array([0.0, -0.035, 0.515])          # the head
HR = np.array([0.058, 0.066, 0.056])
HOOD_C, HOOD_R = np.array([0.0, 0.0, 0.405]), np.array([0.094, 0.086, 0.078])
RIM = HOOD_C[2] - HOOD_R[2] * 0.82


def band(k, centre, axis, z0, z1, n, region):
    """An orange band round a limb between heights z0 and z1 (along the up axis), a zigzag of darker orange round it
    (n points), kept to the limb by region(P)."""
    c = np.array(centre, float)
    def ang(P_):
        return np.arctan2(P_[:, 1] - c[1], P_[:, 0] - c[0])
    k.paint(lambda P_: inside(P_[:, 2] - z1, 0.002) * inside(z0 - P_[:, 2], 0.002) * region(P_), ORANGE)
    mid, amp = (z0 + z1) / 2, (z1 - z0) * 0.32
    k.paint(lambda P_: inside(np.abs(P_[:, 2] - (mid + amp * tri(ang(P_) * n / (2 * math.pi)))) - 0.0032, 0.0018)
            * region(P_), ZIG)


def heliolisk(src, out):
    k = Kit(src, coat="scales")

    # The body: a yellow trunk, narrower at the waist, under the hood
    k.add("hips", ell((0, 0.018, 0.25), (0.07, 0.06, 0.06)), YELLOW, "body")
    k.add("waist", ell((0, 0.004, 0.31), (0.062, 0.052, 0.06)), YELLOW, "body")
    k.add("chest", ell((0, -0.004, 0.355), (0.066, 0.056, 0.05)), YELLOW, "body")

    # The folded frill: a dark bell from under the head to the shoulders
    def bell(V):
        V = V.copy()
        t = np.clip((V[:, 2] + 1) / 2, 0, 1)
        g = 1.0 - 0.56 * t ** 1.1
        V[:, 0] *= g; V[:, 1] *= g
        V[:, 2] = np.maximum(V[:, 2], -0.82)
        return V
    k.add("hood", lathe((0, HOOD_C[1], RIM), (0, 0, 1), [(0, 0), (0.09, 0), (0.088, 0.03), (0.076, 0.07), (0.058, 0.105),
                                                      (0.044, 0.135), (0, 0.15)], seg=48), GREY, "body")
    # its rim kept crisp down to the points: the fused edge would curl yellow up under it
    k.paint(lambda P_: inside(RIM + 0.004 - P_[:, 2], 0.0015) * inside(P_[:, 2] - 0.47, 0.01)
            * inside(np.hypot(P_[:, 0], P_[:, 1] - HOOD_C[1]) / 0.1 - 1, 0.02), GREY)

    # The head: dark, the snout short and pointed, the mouth open: a dark upper jaw, a yellow lower jaw, pink between
    k.add("head", ell(HC, HR), GREY, "head")
    k.add("upper", ell(HC + (0, -0.062, 0.006), (0.04, 0.056, 0.026), rot=(-14, 0, 0)), GREY, "head")
    k.add("inner", ell(HC + (0, -0.05, -0.024), (0.033, 0.052, 0.017)), PINK, "head")
    k.add("lower", ell(HC + (0, -0.052, -0.044), (0.04, 0.052, 0.018), rot=(8, 0, 0)), YELLOW, "head")
    # Two short points at the back of the crown
    for s in (1, -1):
        b = HC + np.array([0.026 * s, 0.022, 0.042])
        tip = b + np.array([0.012 * s, 0.03, 0.034])
        k.add(f"horn{'R' if s > 0 else 'L'}", hull([b + (0.015, 0, 0), b - (0.015, 0, 0), b + (0, 0.017, 0),
                                                     b - (0, 0.017, 0), b + (0, 0, -0.01), tip]),
              GREY, "head", fuse=False)

    # The orange points all round the hood's rim, out and a little down
    for i in range(13):
        a = 2 * math.pi * (i + 0.5) / 13
        d = np.array([math.sin(a), -math.cos(a), 0.0])
        r = 0.09
        base = HOOD_C + np.array([0, 0, RIM - HOOD_C[2] + 0.001]) + d * (r - 0.006)
        t = np.cross(d, (0, 0, 1.0))
        tip = base + d * 0.064 + np.array([0, 0, -0.026])
        k.add(f"point{i}", hull([base + t * 0.024, base - t * 0.024, base + (0, 0, 0.013), base - (0, 0, 0.011),
                                 base - d * 0.012, tip]), ORANGE, "body", fuse=False)

    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        # Legs: great haunches fused into the hips, short thick shins, dark feet with three toes
        k.add(f"thigh{side}", ell(np.array((0.062, 0.006, 0.17)) * m, (0.052, 0.06, 0.085)), YELLOW, f"leg{side}")
        k.add(f"shin{side}", tube(np.array([(0.068, -0.002, 0.11), (0.072, -0.01, 0.035)]) * m, [0.032, 0.027]),
              YELLOW, f"leg{side}")
        k.add(f"foot{side}", ell(np.array((0.073, -0.03, 0.014)) * m, (0.03, 0.045, 0.014)), GREY, f"leg{side}",
              fuse=False)
        P.toes(k, f"toe{side}", f"leg{side}", np.array((0.074, -0.066, 0.01)) * m, (0.2 * s, -1, 0), 0.032, 0.0095)
        # Arms: thin, forward and down, dark hands of three fingers
        sh, el, wr = np.array((0.045, -0.01, 0.398)), np.array((0.108, -0.05, 0.3)), np.array((0.084, -0.128, 0.276))
        k.add(f"arm{side}", tube(np.array([sh, (0.074, -0.02, 0.345), el, wr]) * m, [0.018, 0.017, 0.016, 0.015]), YELLOW, f"arm{side}")
        k.add(f"hand{side}", ell((wr + (0, -0.014, -0.004)) * m, (0.019, 0.021, 0.015)), GREY, f"arm{side}", fuse=False)
        P.toes(k, f"finger{side}", f"arm{side}", (wr + (0, -0.026, -0.006)) * m, (0.15 * s, -1, -0.35), 0.03, 0.0092,
               spread=0.6)

    # The bands on the haunches and above the ankles
    for s in (1, -1):
        cx = 0.062 * s
        band(k, (cx, 0.006), None, 0.15, 0.195, 7,
             lambda P_, cx=cx: inside(-(P_[:, 0] * np.sign(cx)) + 0.012, 0.004) * inside(P_[:, 2] - 0.23, 0.01) * inside(np.abs(P_[:, 1] - 0.006) - 0.08, 0.004))
        band(k, (0.07 * s, -0.006), None, 0.048, 0.076, 6,
             lambda P_, cx=cx: inside(-(P_[:, 0] * np.sign(cx)) + 0.03, 0.004) * inside(P_[:, 2] - 0.09, 0.01) * inside(np.abs(P_[:, 1]) - 0.05, 0.004))

    # The tail: long, back and out to the +X side, low, rising at its end: an orange band, a dark tip
    C = P.tail(k, [(0, 0.04, 0.24), (0.012, 0.1, 0.18), (0.05, 0.18, 0.12), (0.11, 0.27, 0.095), (0.17, 0.35, 0.11),
                   (0.205, 0.41, 0.16)], [0.044, 0.034, 0.026, 0.02, 0.015, 0.011], 0.86)
    i0, i1 = int(0.66 * 59), int(0.77 * 59)
    a0, a1 = C[i0], C[i1]
    d0, d1 = unit(*(C[i0 + 1] - C[i0])), unit(*(C[i1 + 1] - C[i1]))
    k.paint(lambda P_: inside(-((P_ - a0) @ d0), 0.0015) * inside((P_ - a1) @ d1, 0.0015)
            * inside(np.linalg.norm(P_ - (a0 + a1) / 2, axis=1) - 0.06, 0.003), ORANGE)

    k.build(voxel=VOXEL, smooth=4, band=0.005, wband=0.04)

    de = unit(0.8, -0.42, 0.32)
    E = P.on_ell(HC, HR, de)
    look = P.blue_eye(iris=0.58, iris_c=(-0.12, -0.04), pupil=0.24, glint=(0.08, 0.2, 0.15))
    k.eye_states(lambda state: P.lay_eyes(k, state, E, de, (0.024, 0.026), look, srgb(GREY), srgb(YELLOW)))
    k.export(out)


if __name__ == "__main__":
    heliolisk(*args())
