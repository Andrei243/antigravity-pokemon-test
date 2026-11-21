# Mega Slowbro sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/slowbromega.py -- <the export's Slowbro-Mega.glb> <out.glb>
# Grown from slowbro.py and slowpoke.py: the line's head (slow_head and its eyes), pink, cream and white claws, and
# slowbro.py's pale grey-blue shell with its darker spots. From the references: the Shellder has swallowed it whole and
# it hangs in the air inside a spiral shell of three whorls stacked point down, the top whorl widest, a ring of big
# white teeth round its opening with the head looking out of it, the bill resting on the rim; the middle whorl spotted
# with dark grey diamonds; the lowest whorl a darker grey bristling with solid spikes; the arms out of holes in the top
# whorl's sides, each ending in white claws and a white point; the tail hanging out of the point below, its tip cream.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, lathe, srgb, inside, smoothstep, unit, polygon, args, sibling

sp = sibling("slowpoke")
PINK, CREAM = sp.PINK, "#EEE0B2"
SHELL, LOWER, SPOT = "#B2C8D6", "#8E959E", "#69707A"
DARK, TEETH = "#3A2E3C", "#F4F4F2"

# --- the frame -------------------------------------------------------------------------------------------------------
HC = np.array([0, -0.03, 0.955])         # the head's middle, sunk in the opening
HS = 1.15                                # the head's scale against Slowpoke's
TOP_Z = (0.7, 0.905)                     # the top whorl, a ring from its foot to its rim
RIM_R = 0.215                            # the opening's radius at the rim
MID_C, MID_R = np.array([0.008, 0.0, 0.575]), np.array([0.31, 0.28, 0.135])
LOW_C, LOW_R = np.array([0.0, 0.0, 0.39]), np.array([0.235, 0.215, 0.105])


def slowbromega(src, out):
    k = Kit(src, coat="shell")
    spec = sp.slow_head(k, HC, HS)

    # The top whorl: a thick ring, widest of the three, the opening in its top. Loose, so its rim stays crisp
    ring = [(0.17, TOP_Z[0] + 0.01), (0.28, TOP_Z[0]), (0.325, TOP_Z[0] + 0.045), (0.34, 0.8), (0.325, 0.855),
            (0.285, 0.893), (0.24, TOP_Z[1]), (RIM_R - 0.01, TOP_Z[1] - 0.01), (RIM_R - 0.025, 0.86),
            (RIM_R - 0.035, 0.79)]
    k.add("topwhorl", lathe((0, 0, 0), (0, 0, 1), ring + [ring[0]], seg=64),
          SHELL, "body", fuse=False, surface="shell")
    # the dark inside, seen round the head: a floor in the opening
    k.add("floor", lathe((0, 0, 0), (0, 0, 1), [(0, 0.81), (RIM_R - 0.02, 0.81), (RIM_R - 0.02, 0.83), (0, 0.83)],
                         seg=48), DARK, "body", fuse=False, surface="shell")
    # the teeth round the rim: big white points standing up and leaning in
    for i in range(10):
        a = 2 * math.pi * (i + 0.5) / 10
        out_ = np.array([math.sin(a), -math.cos(a), 0.0])
        side = np.array([math.cos(a), math.sin(a), 0.0])
        root = out_ * (RIM_R + 0.02) + (0, 0, TOP_Z[1] - 0.01)
        tip = out_ * (RIM_R + 0.0) + (0, 0, TOP_Z[1] + 0.085)
        k.add(f"tooth{i}", hull([root + side * 0.045, root - side * 0.045, root + out_ * 0.035, root - out_ * 0.02,
                                 root + (0, 0, -0.015), tip]), TEETH, "body", fuse=False, surface="shell")

    # The middle whorl: fused, spotted; the lowest whorl darker; the point below
    k.add("mid", ell(MID_C, MID_R), SHELL, "body")
    k.add("midtop", ell((0, 0, 0.68), (0.26, 0.24, 0.06)), SHELL, "body")
    k.add("low", ell(LOW_C, LOW_R), LOWER, "body")
    k.add("lowpoint", tube([(0, 0, 0.36), (0, 0, 0.3), (0, 0.0, 0.25)], [0.15, 0.09, 0.03]), LOWER, "tail")
    # a crisp line where the middle whorl's spiral runs into the lowest
    k.paint(lambda P: inside(P[:, 2] - (0.47 + 0.03 * np.arctan2(P[:, 0], -P[:, 1]) / math.pi), 0.003)
            * (P[:, 2] > 0.2), LOWER, on=["mid", "low"])
    # the dark diamonds on the middle whorl
    rs = np.random.RandomState(71)
    for j, (az, z, w) in enumerate([(-0.35, 0.58, 0.035), (0.25, 0.55, 0.03), (0.85, 0.6, 0.032), (-0.95, 0.54, 0.03),
                                     (1.6, 0.57, 0.03), (-1.6, 0.6, 0.03), (2.3, 0.55, 0.03), (-2.4, 0.57, 0.03),
                                     (3.1, 0.6, 0.03), (0.0, 0.5, 0.026), (-1.2, 0.5, 0.026), (1.2, 0.505, 0.026)]):
        n = np.array([math.sin(az), -math.cos(az), 0.25 * (z - 0.55) / 0.1])
        p = MID_C + n / np.linalg.norm(n / MID_R)
        shape = [(0.0, 1.0), (-0.55 - 0.2 * rs.rand(), 0.05), (0.0, -1.0), (0.55 + 0.2 * rs.rand(), -0.05)]
        k.decal(p, n, (w * 1.3, w * 1.7), SPOT, eps=0.004, outline=polygon(shape), rings=8, segs=36,
                up=(math.cos(az) * 0.5, math.sin(az) * 0.5, 1.0))

    # The spikes of the lowest whorl: solid cones out and a little down
    for i in range(6):
        a = 2 * math.pi * i / 6 + 0.5
        out_ = np.array([math.sin(a), -math.cos(a), 0.0])
        root = LOW_C + out_ * np.array([LOW_R[0], LOW_R[1], 0]) * 0.8
        tip = root + out_ * 0.14 + (0, 0, -0.035)
        side = np.cross(out_, (0, 0, 1.0))
        pts = [root + 0.045 * (math.cos(q) * side + math.sin(q) * np.array([0, 0, 1.0])) for q in np.linspace(0, 2 * math.pi, 7)[:-1]]
        k.add(f"spike{i}", hull(pts + [root - out_ * 0.02, tip]), LOWER, "body", fuse=False, surface="shell")

    # The arms: out of holes in the top whorl's sides, forward and a little down, white claws and a white point
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"arm{side}", tube(np.array([(0.2, -0.04, 0.76), (0.3, -0.08, 0.735), (0.37, -0.13, 0.71)]) * m,
                                 [0.05, 0.05, 0.048]), PINK, f"arm{side}")
        k.add(f"hand{side}", ell(np.array([0.375, -0.145, 0.705]) * m, (0.05, 0.048, 0.046)), PINK, f"arm{side}")
        k.add(f"hole{side}", ell(np.array([0.33, -0.09, 0.735]) * m, (0.025, 0.065, 0.065), rot=(0, 0, -35 * s)), DARK,
              f"arm{side}", fuse=False, surface="shell")
        # the white point out of the hand's end
        tip = np.array([0.43, -0.2, 0.72]) * m
        root = np.array([0.39, -0.165, 0.71]) * m
        k.add(f"point{side}", hull([root + (0, 0, 0.022), root - (0, 0, 0.022), root + (0.02 * s, 0.02, 0),
                                   root - (0.02 * s, 0.02, 0), tip]), sp.CLAW, f"arm{side}", fuse=False, surface="shell")
    sp.claws(k, "claw", (0.37, -0.19, 0.685), (0.3, -1, -0.3), 3, "arm{s}")

    # The tail out of the point below, its tip cream
    k.add("tail", tube([(0, 0.0, 0.29), (0.01, -0.01, 0.22), (0.025, -0.03, 0.16), (0.03, -0.05, 0.115)],
                       [0.05, 0.045, 0.032, 0.006]), PINK, "tail")
    k.paint(lambda P: inside(P[:, 2] - 0.185, 0.004) * (P[:, 2] > 0.09), CREAM, on=["tail"])

    k.build(voxel=0.0068, smooth=3, band=0.006)
    k.eye_states(lambda st: sp.eyes(k, spec, st))
    k.export(out)


if __name__ == "__main__":
    slowbromega(*args())
