# Mawile sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/mawile.py -- <the export's Mawile.glb> <out.glb>
#
# From the references: a small pale yellow figure in a skirt of fur flaring to the ground, pleated, its hem in points,
# little black feet under it; tiny arms held forward with black ragged hands; a round head with large red eyes (dark
# rim, black pupil, a glint), the crown and back of the head black where a great black jaw grows out of it: the jaw
# arches up and back behind the head and comes down to the ground far behind, open along its length, pink inside, with
# white triangular teeth on both jaws and a big yellow oval spot on each flank near the top of its arch; a thick black
# strand hangs out and down from each side of the head. The jaw runs back and some 35 degrees out to the model's +X
# side, so that the game's cameras see its flank.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, hull, lathe, srgb, inside, smoothstep, unit, polygon, eye_look, args

YELLOW, BLACK, PINK, TEETH, SPOT = "#F3DE92", "#45454D", "#C2477E", "#F6F4EE", "#F1DA86"
RED, INK = srgb("#D52A3C"), srgb("#1A1418")

HC = np.array([0.0, -0.012, 0.37])                  # the head's centre and radii
HR = np.array([0.092, 0.086, 0.088])
D = unit(math.sin(math.radians(35)), math.cos(math.radians(35)), 0)     # the jaw's way: back and out to +X
LAT = unit(*np.cross(D, (0, 0, 1.0)))              # across the jaw (its flanks face +-LAT)
ROOT = np.array([0.0, 0.035, 0.40])


def jaw_pt(s, z, lat=0.0):
    return ROOT + D * s + np.array([0, 0, z - ROOT[2]]) + LAT * lat


def claw(base, d, length, width, thick, up=(0, 0, 1)):
    d = unit(*d)
    a = np.cross(d, np.array(up, float)); a /= np.linalg.norm(a)
    b = np.cross(a, d)
    base = np.array(base, float)
    mid = base + d * length * 0.45
    pts = [base + a * width / 2, base - a * width / 2, base + b * thick / 2, base - b * thick / 2 - d * 0.004,
           mid + a * width * 0.28, mid - a * width * 0.28, mid + b * thick * 0.28, mid - b * thick * 0.28,
           base + d * length]
    return hull(pts)


def skirt():
    """The skirt: a closed solid of turning flaring from the waist to the ground, pleated, its hem cut into points."""
    prof = [(0.0, 0.3), (0.045, 0.3), (0.06, 0.25), (0.078, 0.18), (0.1, 0.1), (0.126, 0.012),
            (0.0, 0.012)]
    V, F = lathe((0, 0, 0), (0, 0, 1), prof, seg=96)
    V = V.copy()
    r = np.hypot(V[:, 0], V[:, 1])
    th = np.arctan2(V[:, 1], V[:, 0])
    low = np.clip((0.2 - V[:, 2]) / 0.19, 0, 1)
    pleat = 1 + 0.06 * low * np.abs(np.cos(4 * th))                 # eight folds, deeper toward the hem
    k = np.where(r > 1e-6, pleat, 1.0)
    V[:, 0] *= k; V[:, 1] *= k
    hem = (V[:, 2] < 0.02) & (r > 1e-6)
    V[hem, 2] += 0.03 * (1 - np.abs(np.cos(4 * th[hem])))           # the hem rises between the points
    return V, F


def mawile(src, out):
    k = Kit(src, coat="fur")

    # --- the head: round, the crown and the back black where the jaw grows -------------------------------------
    k.add("head", ell(HC, HR), YELLOW, "head")
    k.add("cheek", ell(HC + (0, -0.01, -0.03), (0.084, 0.076, 0.06)), YELLOW, "head")
    k.paint(lambda P: inside(0.03 - 0.75 * (P[:, 2] - HC[2]) - (P[:, 1] - HC[1]), 0.003)
            * inside(np.linalg.norm((P - HC) / (HR * 1.25), axis=1) - 1, 0.01) * (P[:, 2] > HC[2] - 0.05), BLACK)
    # the strands hanging out and down from the sides of the head
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"strand{side}", tube(np.array([(0.06, 0.01, 0.425), (0.105, 0.02, 0.42), (0.128, 0.025, 0.375),
                                              (0.133, 0.025, 0.32)]) * m, [0.02, 0.02, 0.02, 0.021]), BLACK, "head")
        k.add(f"strandtip{side}", ell(np.array([0.134, 0.025, 0.305]) * m, (0.022, 0.016, 0.028)), BLACK, "head")

    # --- the jaw: up and back from the crown, down to the ground far behind, open along its length --------------
    UP = [(-0.01, 0.45, 0.06), (0.06, 0.515, 0.08), (0.15, 0.518, 0.09), (0.25, 0.457, 0.087), (0.34, 0.366, 0.08),
          (0.42, 0.264, 0.072), (0.48, 0.172, 0.064), (0.52, 0.108, 0.053)]
    LOW = [(0.06, 0.385, 0.03), (0.15, 0.358, 0.036), (0.25, 0.288, 0.038), (0.34, 0.198, 0.036), (0.42, 0.115, 0.034),
           (0.47, 0.06, 0.032), (0.495, 0.035, 0.03)]
    MID = [(0.06, 0.425, 0.03), (0.15, 0.405, 0.042), (0.25, 0.345, 0.048), (0.34, 0.258, 0.045), (0.42, 0.17, 0.038),
           (0.47, 0.108, 0.03)]
    k.add("jawtop", tube([jaw_pt(s, z) for s, z, r in UP], [r for s, z, r in UP]), BLACK, "maw")
    k.add("jawlow", tube([jaw_pt(s, z) for s, z, r in LOW], [r for s, z, r in LOW]), BLACK, "maw")
    k.add("palate", blade([jaw_pt(s, z) for s, z, r in MID], [r for s, z, r in MID], LAT, 0.74), PINK, "maw")

    def along(rows, s):
        S = [q[0] for q in rows]
        return float(np.interp(s, S, [q[1] for q in rows])), float(np.interp(s, S, [q[2] for q in rows]))
    # teeth: white points down from the upper jaw and up from the lower, on both flanks
    for side in (1, -1):
        for j, s in enumerate((0.13, 0.23, 0.32, 0.4)):
            z, r = along(UP, s)
            b = jaw_pt(s, z - r * 0.9, side * 0.024)
            dn = unit(*(jaw_pt(s + 0.02, z - r * 0.9 - 0.06, side * 0.028) - b))
            k.add(f"tooth{side}{j}", claw(b, dn, 0.05, 0.034, 0.028, up=LAT), TEETH, "maw", fuse=False,
                  surface="shell")
        for j, s in enumerate((0.27, 0.38)):
            z, r = along(LOW, s)
            b = jaw_pt(s, z + r * 0.8, side * 0.022)
            upd = unit(*(jaw_pt(s - 0.015, z + r * 0.8 + 0.05, side * 0.024) - b))
            k.add(f"fang{side}{j}", claw(b, upd, 0.036, 0.03, 0.024, up=LAT), TEETH, "maw", fuse=False,
                  surface="shell")
    # the yellow spots on the flanks near the top of the arch
    for side in (1, -1):
        n = LAT * side + np.array([0, 0, 0.3])
        k.decal(jaw_pt(0.14, 0.535, side * 0.07), n, (0.068, 0.05), SPOT, up=tuple(D + np.array([0, 0, 0.4])),
                eps=0.004, rings=16, segs=56)

    # --- the body: a slim yellow body in a skirt flaring to its feet --------------------------------------------
    k.add("torso", ell((0, 0.0, 0.25), (0.05, 0.045, 0.065)), YELLOW, "body")
    k.add("neck", ell((0, -0.005, 0.3), (0.042, 0.04, 0.04)), YELLOW, "body")
    k.add("skirt", skirt(), YELLOW, "body", fuse=False)
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"foot{side}", ell(np.array([0.045, -0.03, 0.018]) * m, (0.03, 0.04, 0.02)), BLACK, f"leg{side}")
        k.add(f"leg{side}", tube(np.array([(0.04, 0.0, 0.1), (0.045, -0.015, 0.03)]) * m, [0.025, 0.024]), YELLOW,
              f"leg{side}")
        # the arms: tiny, forward and a little down, black ragged hands
        k.add(f"arm{side}", tube(np.array([(0.035, 0.0, 0.26), (0.075, -0.03, 0.235), (0.1, -0.06, 0.215)]) * m,
                                 [0.02, 0.018, 0.018]), YELLOW, f"arm{side}")
        Hd = np.array([0.112, -0.075, 0.205])
        k.add(f"hand{side}", ell(Hd * m, (0.024, 0.026, 0.022)), BLACK, f"arm{side}")
        for j, d in enumerate([(0.6, -0.6, 0.2), (0.3, -0.85, -0.3), (-0.1, -0.8, -0.55)]):
            d = np.array(d)
            b = Hd + unit(*d) * 0.014
            k.add(f"finger{side}{j}", claw(b * m, d * m, 0.034, 0.03, 0.022), BLACK, f"arm{side}", fuse=False)
    k.paint(lambda P: inside(np.abs(P[:, 0]) - 0.098, 0.003) * (P[:, 2] > 0.17) * (P[:, 2] < 0.27), YELLOW,
            on=["arm"])

    # --- the eyes: large and red, a dark rim, a black pupil, a glint ------------------------------------------
    def eye(u, v):
        r = np.sqrt(u * u + v * v)
        c = np.tile(RED, (len(u), 1))
        c[((u + 0.05) / 0.42) ** 2 + ((v + 0.02) / 0.5) ** 2 < 1] = INK
        c[r > 0.82] = INK
        return c
    de = unit(0.42, -0.9, 0.08)
    E = HC + de / math.sqrt(((de / HR) ** 2).sum())
    half = (0.029, 0.034)
    LID = srgb(YELLOW)
    right = np.cross((0, 0, 1.0), de); right /= np.linalg.norm(right)
    upv = np.cross(de, right)

    def eyes(state):
        k.decal(E, de, half, eye_look(state, eye, LID, INK, line=0.15), eps=0.006, rings=20, segs=56, mirror=True)
        if state == "Open":
            g = E + right * (-0.25 * half[0]) + upv * (0.42 * half[1])
            k.decal(g, de, (0.0075, 0.008), "#FFFFFF", eps=0.010, rings=5, segs=20, mirror=True)

    k.build(voxel=0.0055, smooth=4, band=0.006)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    mawile(*args())
