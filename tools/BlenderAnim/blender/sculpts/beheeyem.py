# Beheeyem sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/beheeyem.py -- <the export's Beheeyem.glb> <out.glb>
#
# Elgyem grown (its brain strokes, its broad round hands and their three lights, from elgyem.py), in tan brown. From
# the references: a head like a tall bell or hood, its top rounded and leaning back, flaring at its bottom into a wide
# brim; over its front a dark brain mark (a Y with a long stem, a stroke each side slanting in and then down, and a
# short bar further out on each side); on the brim's front a green band of two eyes joined like goggles, dark pupils.
# A pale high collar under the head, open at the front; a robe flaring to a hem with a dark hollow under it and two
# dark feet showing; four pale buttons in two rows down its front; two arms reaching forward and down to broad round
# hands, the one on -X held up with red, green and yellow lights at its fingers, the other lower with two lights.
# The skeleton: body (0, 0, 0.22), head (0, -0.01, 0.36), armR/L (+-0.06, 0, 0.3).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, sheet, lathe, srgb, inside, smoothstep, unit, eye_look, polygon, outward, sibling, args

e = sibling("elgyem")

TAN, COLLAR, BRAIN, PALE, DARK = "#C99A82", "#D9B4A2", "#34282A", "#EAD3C6", "#4E363C"
GREEN, GRIM, PUPIL = "#3CC47C", "#1C7044", "#141414"

HC, HRAD = np.array([0.0, 0.01, 0.525]), np.array([0.1, 0.1, 0.195])
VOXEL = 0.0058
HOOD_Z = [-1, -0.97, -0.84, -0.6, -0.4, 0.0, 0.5, 0.8, 0.95, 1.0]
HOOD_R = [0.0, 0.136, 0.15, 0.155, 0.128, 0.108, 0.1, 0.088, 0.058, 0.0]
ROBE_Z = [-1, -0.94, -0.75, -0.4, 0.0, 0.5, 0.88, 1.0]
ROBE_R = [0.0, 0.1, 0.098, 0.09, 0.078, 0.064, 0.05, 0.0]
GOGGLE_Z, GOGGLE_HALF = 0.37, (0.1, 0.033)


def turned(zs, rs, lean=0.0, depth=0.9):
    """A solid of turning by a profile of radius (metres) against the unit sphere's height, a little deeper than wide
    by depth, its top leaning back by lean."""
    def f(V):
        V = V.copy()
        zz = np.clip(V[:, 2], -1, 1)
        rho = np.sqrt(np.clip(1 - zz * zz, 1e-6, None))
        R = np.interp(zz, zs, rs)
        V[:, 0] *= R / rho
        V[:, 1] *= R / rho * depth
        V[:, 1] += lean * smoothstep(0.2, 1.0, zz)
        return V
    return f


# the brain mark, in (arc, height above the head's middle)
BRAIN_SEGS = [((0, -0.1), (0, 0.01)), ((0, 0.01), (0.038, 0.1)), ((0, 0.01), (-0.038, 0.1)),
              ((0.088, 0.09), (0.056, 0.03)), ((0.056, 0.03), (0.056, -0.098)),
              ((-0.088, 0.09), (-0.056, 0.03)), ((-0.056, 0.03), (-0.056, -0.098)),
              ((0.094, -0.035), (0.098, -0.1)), ((-0.094, -0.035), (-0.098, -0.1))]

# the goggles' outline (u across the whole band, v up), two eyes joined by a narrow bridge
GOGGLES = [(0, 0.3), (0.16, 0.42), (0.42, 0.78), (0.76, 0.74), (1.0, 0.28), (0.95, -0.38), (0.62, -0.82),
           (0.3, -0.78), (0.12, -0.38), (0, -0.3)]
GOGGLES = GOGGLES + [(-u, v) for u, v in GOGGLES[::-1]][1:-1]


def goggles(state):
    lid, ink = srgb(GREEN), srgb(GRIM)
    r = polygon(GOGGLES)

    def eye(u, v):
        c = np.tile(srgb(GREEN), (len(u), 1))
        c[((u + 0.12) / 0.4) ** 2 + (v / 0.4) ** 2 < 1] = srgb(PUPIL)
        return c
    look = eye_look(state, eye, lid, ink, line=0.18)

    def col(u, v):
        uu = (np.abs(u) - 0.56) / 0.4
        vv = v / 0.72
        c = np.tile(srgb(GREEN), (len(u), 1))
        within = uu ** 2 + vv ** 2 < 1.1
        c[within] = look(uu[within], vv[within])
        rho = np.array([math.hypot(a, b) / r(math.atan2(b, a)) for a, b in zip(u, v)])
        c[rho > 0.83] = srgb(GRIM)
        return c
    return col


def beheeyem(src, out):
    k = Kit(src, coat="fur")
    # --- the hood of a head, the neck, the robe ----------------------------------------------------------------------
    k.add("hood", ell(HC, (1.0, 1.0, HRAD[2]), deform=turned(HOOD_Z, HOOD_R, lean=0.05)), TAN, "head")
    k.add("neck", ell((0, 0.004, 0.31), (0.045, 0.042, 0.05)), TAN, "body")
    k.add("robe", ell((0, 0.005, 0.1725), (1.0, 1.0, 0.1325), deform=turned(ROBE_Z, ROBE_R, depth=0.92)), TAN, "body")
    k.paint(e.strokes(BRAIN_SEGS, 0.0095, c=HC, r=HRAD, tilt=0.0), BRAIN, on=["hood"])
    # four pale buttons in two rows
    for x, z in ((0.027, 0.205), (0.034, 0.145)):
        for s in (1, -1):
            R = np.interp((z - 0.1725) / 0.1325, ROBE_Z, ROBE_R) * 0.92
            th = math.asin(min(x * s / R, 0.99))
            k.decal((x * s, 0.005 - R * math.cos(th), z), unit(math.sin(th), -math.cos(th), 0.25), (0.013, 0.013), PALE,
                    eps=0.003, rings=8, segs=36)

    # --- the hem: a flared ring, dark under it, two dark feet ---------------------------------------------------------
    up = np.array([0, 0, 1.0])
    hem = [(0.118, 0.0), (0.138, 0.0), (0.1, 0.08), (0.085, 0.08), (0.118, 0.0)]
    k.add("hem", lathe((0, 0.005, 0.0), up, hem, seg=72), TAN, "body", fuse=False)
    k.add("hollow", lathe((0, 0.005, 0.0), up, [(0, 0.035), (0.118, 0.004), (0.1, 0.06), (0, 0.06)], seg=72), DARK,
          "body", fuse=False)
    for s in (1, -1):
        k.add(f"foot{s}", tube([(0.032 * s, 0.0, 0.05), (0.034 * s, -0.008, 0.024)], [0.019, 0.021]), DARK, "body",
              fuse=False)

    # --- the collar, high and open at the front -------------------------------------------------------------------------
    def collar(u, v):
        th = math.radians(28) + (2 * math.pi - math.radians(56)) * u      # round from the front's left to its right
        v = 0.02 + 0.96 * v
        fold = np.abs(np.cos(0.5 * (th - math.pi)))                         # tallest at the back
        r = 0.048 + 0.03 * v ** 1.2
        z = 0.268 + (0.07 + 0.02 * fold) * v
        return np.stack([r * np.sin(th), -r * np.cos(th) + 0.004, z], -1)
    k.add("collar", outward(sheet(collar, 90, 8, 0.016)), COLLAR, "body", fuse=False)

    # --- the arms and the hands with their lights ----------------------------------------------------------------------
    HAND = (0.05, 0.021, 0.045)
    for s, bone, hc, n, spots in (
            (-1, "armL", np.array([-0.15, -0.085, 0.245]), (-0.35, -0.8, 0.45),
             [("red", 0.5, 0.42), ("green", -0.12, 0.22), ("yellow", 0.3, -0.42)]),
            (1, "armR", np.array([0.15, -0.035, 0.15]), (0.5, -0.7, 0.2),
             [("green", -0.1, -0.85), ("red", 0.38, -0.78)])):
        sh = np.array([0.055 * s, 0.0, 0.29])
        wrist = hc - unit(*n) * 0.0 - np.array([0.03 * s, 0.0, 0.0])
        mid = (sh + wrist) / 2 + np.array([0.012 * s, 0.0, 0.012])
        k.add("arm" + bone[-1], tube([sh, mid, wrist], [0.022, 0.02, 0.021]), TAN, bone)
        fr = e.hand_frame(hc, n)
        e.hand(k, "hand" + bone[-1], bone, fr, HAND, TAN, s)
        c, ex, nn, ez = fr
        ex = ex * (1 if (ex[0] * s) > 0 else -1)
        e.lights(k, bone, (c, ex, nn, ez), HAND, spots, 0.0115)

    k.build(voxel=VOXEL, smooth=4, band=0.005)

    # --- the goggles: one band across the brim's front -------------------------------------------------------------------
    zz = (GOGGLE_Z - HC[2]) / HRAD[2]
    R = np.interp(zz, HOOD_Z, HOOD_R) * 0.9

    def eyes(state):
        k.decal((0, HC[1] - R, GOGGLE_Z), unit(0, -1, -0.12), GOGGLE_HALF, goggles(state), eps=0.0035,
                outline=polygon(GOGGLES), rings=24, segs=120)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    beheeyem(*args())
