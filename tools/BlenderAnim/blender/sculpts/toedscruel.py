# Toedscruel sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/toedscruel.py -- <the export's Toedscruel.glb> <out.glb>
#
# From the references: Toedscool grown into a tall jellyfish-mushroom. A wide black cap rising to a peak with a black
# knob, its brim wavy, pale cream underneath; on its top two great pink domes, one each side, with a wavy yellow line
# where they meet the black, and a small pink ball at the front of the ridge between them. Under the cap a pale cream
# face with heavy-lidded eyes and a black spout hanging forward from its middle, yellow round its open end. It stands
# on seven long cream tentacles that turn yellow toward the ground and curl out at their ends; two arms of the same:
# its left raised forward and curled over at its yellow pad, its right short, hanging with a yellow paddle.
#
# Grown from toedscool.py (its eye, tentacle, pad and point-on-a-bulb functions, and its colours).
# Skeleton: body (0, 0, 0.64), head (0, 0, 0.72), armL/armR (-+0.06, -0.02, 0.59), seven legs round (0, 0, 0.58):
# legR, legR.001, legL, legL.001, hindL, hindR, hindR.001.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, lathe, ring_band, srgb, inside, smoothstep, unit, sibling, args

ts = sibling("toedscool")

BLACK, CAPUNDER, CREAM, YELLOW, PINK, LINE = "#25222A", "#EDE6C2", "#F2EDCB", "#F0DA3A", ts.PINK, "#E9D23A"
LIDLINE = "#3A3236"

FACE = np.array([0.0, -0.012, 0.665])
FACER = np.array([0.1, 0.098, 0.095])
CAP = np.array([0.0, 0.03, 0.775])
CAPR = np.array([0.29, 0.225, 0.205])     # the cap's radii; its underside is a quarter of its top's height
CAPLOW = 0.04
WAVES = 7
VOXEL = 0.0062

LEGS = {"legR": (0.048, -0.015), "legR.001": (0.018, -0.047), "legL": (-0.025, -0.043), "legL.001": (-0.049, -0.007),
        "hindL": (-0.037, 0.034), "hindR": (0.004, 0.05), "hindR.001": (0.041, 0.028)}


def cap_shape(V):
    """The unit sphere made a cap: its top a peak with hollow sides, its underside flat, its brim waving."""
    V = V.copy()
    z = V[:, 2]
    rho = np.sqrt(np.clip(1 - z * z, 0, 1))
    top = 1 - rho
    V[:, 2] = np.where(z > 0, top ** 1.25, z * CAPLOW / CAPR[2])
    th = np.arctan2(V[:, 1], V[:, 0])
    wave = 1 + 0.07 * np.cos(WAVES * th + 0.4) * np.clip(1 - np.abs(z) * 2.5, 0, 1)
    V[:, 0] *= wave; V[:, 1] *= wave
    return V


def cap_top(x, y):
    """The cap's top height over a place (the wave left out)."""
    rho = math.sqrt((x / CAPR[0]) ** 2 + ((y - CAP[1]) / CAPR[1]) ** 2)
    if rho >= 1:
        return CAP[2]
    return CAP[2] + CAPR[2] * (1 - rho) ** 1.25


def dome_line(c, r, n=72, wave=4, amp=0.005):
    """Where a dome meets the cap's top, all round, with a little wave: the points of the yellow line."""
    pts = []
    for i in range(n + 1):
        th = 2 * math.pi * i / n + math.pi / 2          # starting behind
        lo, hi = -1.2, 1.2
        def gap(e):
            p = c + np.array([math.cos(th) * math.cos(e) * r[0], math.sin(th) * math.cos(e) * r[1], math.sin(e) * r[2]])
            return p[2] - cap_top(p[0], p[1]), p
        for _ in range(40):
            m = (lo + hi) / 2
            if gap(m)[0] > 0:
                hi = m
            else:
                lo = m
        _, p = gap((lo + hi) / 2)
        p = p + np.array([0, 0, amp * math.sin(wave * th)])
        pts.append(p)
    return pts


def toedscruel(src, out):
    k = Kit(src, coat="scales")
    # ---- the face and the body under the cap
    k.add("face", ell(FACE, FACER), CREAM, "body")
    k.add("neck", ell((0, 0.0, 0.6), (0.075, 0.075, 0.06)), CREAM, "body")
    # ---- the cap, its domes, ridge, knob and the pink ball at the front
    k.add("cap", ell(CAP, CAPR, deform=cap_shape, sub=5), BLACK, "head")
    k.paint(lambda P: inside(P[:, 2] - (CAP[2] - 0.004), 0.004), CAPUNDER, on=["cap"])
    domes = []
    for s in (1, -1):
        dc = np.array([0.145 * s, 0.04, 0.855])
        dr = np.array([0.11, 0.13, 0.082])
        domes.append((dc, dr))
        k.add("dome" + ("R" if s > 0 else "L"), ell(dc, dr), PINK, "head")
    k.add("knob", ell((0, 0.04, 0.985), (0.028, 0.032, 0.045)), BLACK, "head")
    k.add("ball", ell((0, -0.1, 0.855), (0.036, 0.034, 0.034)), PINK, "head")
    for s, (dc, dr) in zip((1, -1), domes):
        line = dome_line(dc, dr * 0.985)
        k.add("line" + ("R" if s > 0 else "L"), tube(line, [0.0095] * len(line), seg=10, per=3), LINE, "head",
              fuse=False, surface="shell")
    # ---- the spout: a black barrel hanging forward from the face, yellow round its open end
    root = ts.on(0, -8, FACE, FACER, 0.0)[0]
    d = unit(0, -0.38, -1.0)
    L = 0.11
    k.add("spout", lathe(root - d * 0.03, d, [(0, 0), (0.026, 0), (0.028, L * 0.6), (0.032, L), (0.022, L),
                                              (0.02, L - 0.02), (0, L - 0.02)], seg=32), BLACK, "body", fuse=False,
          surface="skin")
    k.add("spoutrim", ring_band(root - d * 0.03 + d * (L - 0.006), d, 0.033, 0.013, thick=0.008), YELLOW, "body",
          fuse=False, surface="skin")
    # ---- the legs: seven long tentacles, curling out at their ends
    for bone, (x, y) in LEGS.items():
        o = unit(x, y, 0)
        j = np.array([x, y, 0.6])
        pts = [j, o * 0.07 + np.array([0, 0, 0.45]), o * 0.09 + np.array([0, 0, 0.24]),
               o * 0.12 + np.array([0, 0, 0.07]), o * 0.16 + np.array([0, 0, 0.024]),
               o * 0.2 + np.array([0, 0, 0.04])]
        ts.tentacle(k, "leg" + bone.replace(".", ""), pts, 0.024, 0.021, bone, colour=CREAM)
    k.paint(lambda P: smoothstep(0.34, 0.2, P[:, 2]), YELLOW, on=["leg"])
    # ---- the arms: its left raised forward and curled over, its right hanging short; yellow pads
    ts.tentacle(k, "armL", [(-0.07, -0.04, 0.6), (-0.14, -0.11, 0.6), (-0.2, -0.15, 0.66), (-0.235, -0.15, 0.73),
                            (-0.26, -0.13, 0.75), (-0.275, -0.12, 0.7)], 0.022, 0.02, "armL", colour=CREAM)
    ts.pad(k, "padL", (-0.278, -0.12, 0.665), (0.026, 0.024, 0.05), 0, "armL", colour=YELLOW)
    ts.tentacle(k, "armR", [(0.07, -0.04, 0.6), (0.1, -0.1, 0.56), (0.12, -0.14, 0.5), (0.13, -0.155, 0.45)],
                0.022, 0.02, "armR", colour=CREAM)
    ts.pad(k, "padR", (0.135, -0.165, 0.42), (0.034, 0.024, 0.052), 15, "armR", colour=YELLOW)
    pads = [np.array([-0.278, -0.12, 0.665]), np.array([0.135, -0.165, 0.42])]
    k.paint(lambda P: np.maximum(*[smoothstep(0.11, 0.05, np.linalg.norm(P - q, axis=1)) for q in pads]), YELLOW,
            on=["arm"])
    k.build(voxel=VOXEL, smooth=4, band=0.006, wband=0.05)

    places = []
    for s in (1, -1):
        p, n = ts.on(38 * s, 4, FACE, FACER, 0.0)
        places.append((p, unit(*(n + np.array([0, -0.25, 0])))))
    k.eye_states(lambda state: ts.lay_eyes(k, state, places, (0.034, 0.028), CREAM, LIDLINE))
    k.export(out)


if __name__ == "__main__":
    toedscruel(*args())
