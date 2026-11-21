# Enamorus (Incarnate Forme) sculpted from nothing on the skeleton and clips of the game's own export of it
# (sculptkit.py), with the three forces of nature's cloud and solid points taken from Tornadus's script
# (sibling("tornadus"): cloud(), spike()). The forces' genie() is not called: its crossed muscular arms, its boxy
# head and its great moustache are not this design's.
#   blender --background --factory-startup --python sculpts/enamorus.py -- <the export's Enamorus.glb> <out.glb>
#
# The design, from the references: a slim pink genie rising out of a white cloud that trails a wisp below; its hands
# on its hips, the elbows out; a pink head with long pointed ears out to the sides, a few small red horns on the
# brow, sly yellow eyes, a white moustache curling out from the mouth's corners, white hair over the crown rising in
# a heart of two loops; a thick mauve tail wound twice round its shoulders, its end rising over the +X shoulder in a
# round club, red hearts edged black along it; a long thin mauve tail hanging from the hips down past the cloud on the
# -X side; red hearts on the chest and belly, red spots on the arms.
# The joints: body (0, 0, 0.531), tail (0, 0.04, 0.431), arms (+-0.07, 0, 0.601), head (0, -0.01, 0.661).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, hull, srgb, inside, unit, eye_look, polygon, args, sibling

T = sibling("tornadus")
PINK, MAUVE, RED, HEARTINK, WHITE, HORN = "#F28BB8", "#A25F6E", "#E0203E", "#1C1014", "#F6F6F8", "#D8203C"
EYEYEL, INK, MOUTH = "#F4C430", "#1A1216", "#5A1A2E"

H = np.array([0.0, -0.025, 0.725])          # the head's middle
HR = np.array([0.074, 0.07, 0.075])
CLOUD = np.array([0.0, 0.0, 0.235])


def heart_outline(size, n=32):
    pts = []
    for i in range(n):
        q = 2 * math.pi * i / n
        x = 16 * math.sin(q) ** 3
        y = 13 * math.cos(q) - 5 * math.cos(2 * q) - 2 * math.cos(3 * q) - math.cos(4 * q)
        pts.append((x / 17 * size, (y + 2) / 17 * size))
    return pts


def heart(k, name, c, n, size, bone):
    """A red heart edged black, a loose plate laid on a surface: its face along n, its point down."""
    n = unit(*n)
    ex = np.cross((0, 0, 1.0), n); ex /= np.linalg.norm(ex)
    ez = np.cross(n, ex)
    k.add(name + "ink", slab(heart_outline(size * 1.22), c - n * 0.002, ex, ez, 0.01), HEARTINK, bone, fuse=False)
    k.add(name, slab(heart_outline(size), c + n * 0.002, ex, ez, 0.012), RED, bone, fuse=False)


def torso(k):
    k.add("hips", ell((0, 0.0, 0.4), (0.075, 0.062, 0.085)), PINK, "body")
    k.add("waist", ell((0, 0.0, 0.48), (0.058, 0.05, 0.07)), PINK, "body")
    k.add("chest", ell((0, 0.0, 0.565), (0.078, 0.058, 0.07)), PINK, "body")
    k.add("neck", tube([(0, 0.0, 0.6), (0, -0.012, 0.67)], [0.034, 0.032]), PINK, "head")
    # hands on the hips, the elbows out
    for s, bone in ((1, "armR"), (-1, "armL")):
        m = np.array([s, 1, 1.0])
        SH, EL, HA = np.array((0.075, 0.0, 0.6)), np.array((0.15, 0.015, 0.5)), np.array((0.085, -0.005, 0.425))
        k.add("upper" + bone[-1], tube([SH * m, EL * m], [0.03, 0.026]), PINK, bone)
        k.add("fore" + bone[-1], tube([EL * m, HA * m], [0.026, 0.024]), PINK, bone)
        k.add("hand" + bone[-1], ell(HA * m + (0.006 * s, -0.004, 0), (0.026, 0.024, 0.03)), PINK, bone)
    # red spots on the arms and hips, a red heart on the chest and one on the belly
    spots = [((0.11, -0.022, 0.56), 0.012), ((-0.11, -0.022, 0.56), 0.012), ((0.13, -0.012, 0.47), 0.01),
             ((-0.13, -0.012, 0.47), 0.01), ((0.07, -0.05, 0.38), 0.011), ((-0.07, -0.05, 0.38), 0.011)]
    k.paint(T.blob_mask(spots), RED, on=["upper", "fore", "hips"])
    for c, n, sz in (((0, -0.058, 0.545), (0, -1, 0.1), 0.03), ((0, -0.062, 0.42), (0, -1, 0.0), 0.034)):
        o = polygon([(x / sz, y / sz) for x, y in heart_outline(sz)])
        k.decal(c, n, (sz, sz), RED, eps=0.003, outline=o, rings=12, segs=64)


def head(k):
    def shape(V):
        V = V.copy()
        V[:, 0] *= 1 - 0.18 * np.clip(-V[:, 2], 0, 1) ** 1.5
        return V
    k.add("head", ell(H, HR, deform=shape), PINK, "head")
    k.add("muzzle", ell(H + (0, -0.05, -0.035), (0.038, 0.03, 0.03)), PINK, "head")
    # long pointed ears out to the sides, a little up
    for s, side in ((1, "R"), (-1, "L")):
        k.add(f"ear{side}", T.spike((0.06 * s, -0.01, 0.735), (0.155 * s, 0.01, 0.775), 0.026, n=6, flat=0.55), PINK,
              "head", fuse=False, surface="fur")
    # small red horns on the brow: one in the middle and one each side
    for i, (b, tp) in enumerate((((0, -0.06, 0.77), (0, -0.075, 0.805)), ((0.04, -0.045, 0.78), (0.055, -0.05, 0.815)),
                                 ((-0.04, -0.045, 0.78), (-0.055, -0.05, 0.815)))):
        k.add(f"horn{i}", T.spike(b, tp, 0.012, n=6), HORN, "head", fuse=False, surface="shell")
    # the white moustache: a mass under the nose curling out at the corners, the hair over the crown
    k.add("tache", ell(H + (0, -0.072, -0.045), (0.036, 0.016, 0.014)), WHITE, "head")
    for s, side in ((1, "R"), (-1, "L")):
        k.add(f"curl{side}", tube([H + (0.02 * s, -0.075, -0.05), H + (0.048 * s, -0.062, -0.058),
                                   H + (0.058 * s, -0.05, -0.04), H + (0.05 * s, -0.052, -0.028)],
                                  [0.012, 0.011, 0.009, 0.008]), WHITE, "head", fuse=False, surface="fur")
    k.add("hair", ell(H + (0, 0.025, 0.05), (0.06, 0.055, 0.035)), WHITE, "head")
    # the heart of hair: two short thick loops rising from the crown and meeting at its point
    for s, side in ((1, "R"), (-1, "L")):
        P = [H + (0.0, 0.03, 0.08), H + (0.035 * s, 0.035, 0.13), H + (0.065 * s, 0.035, 0.16),
             H + (0.05 * s, 0.035, 0.19), H + (0.018 * s, 0.035, 0.18), H + (0.004 * s, 0.035, 0.16)]
        k.add(f"heart{side}", tube(P, [0.016, 0.016, 0.016, 0.016, 0.015, 0.014]), WHITE, "head", fuse=False,
              surface="fur")
    # a small sly grin under the moustache
    CM, CP = srgb(MOUTH), srgb(PINK)
    def grin(u, v):
        c = np.tile(CP, (len(u), 1))
        c[(np.abs(v - (0.35 * u * u + 0.15 * u - 0.1)) < 0.3) & (np.abs(u) < 0.85)] = CM
        return c
    k.decal(H + (0, -0.08, -0.068), unit(0, -1, -0.25), (0.024, 0.012), grin, eps=0.004, rings=8, segs=40)


def coil(k):
    """The thick mauve tail wound twice round the shoulders, its end rising over the +X shoulder in a club."""
    pts, rad = [], []
    n = 24
    for i in range(n + 1):
        a = 4 * math.pi * i / n - math.pi * 1.5             # from the +X side round the front, twice, back to +X
        z = 0.53 + 0.085 * i / n
        pts.append((math.sin(a) * 0.11, -math.cos(a) * 0.092, z))
        rad.append(0.034)
    pts = [np.array(p) for p in pts]
    end = pts[-1]
    rise = [end + (0.04, 0.0, 0.03), np.array((0.19, 0.02, 0.69)), np.array((0.215, 0.01, 0.77))]
    k.add("coil", tube(pts + rise, rad + [0.034, 0.033, 0.034]), MAUVE, "body")
    k.add("club", ell((0.215, 0.0, 0.8), (0.046, 0.04, 0.05)), MAUVE, "body")
    # hearts along the coil's outer side, facing out and a little forward where both cameras see them
    for i in (2, 6, 9, 13, 17, 21):
        p = pts[i]
        o = np.array([p[0], p[1], 0.0]); o /= np.linalg.norm(o)
        if o[1] > 0.55:
            continue
        heart(k, f"coilheart{i}", p + o * 0.035, o, 0.022, "body")
    heart(k, "clubheart", np.array((0.215, -0.04, 0.805)), unit(0.25, -1, 0.1), 0.026, "body")


def tail(k):
    """The long thin tail hanging from the hips down past the cloud on the -X side."""
    P = [(0.0, 0.05, 0.42), (-0.08, 0.1, 0.39), (-0.18, 0.11, 0.34), (-0.26, 0.08, 0.27), (-0.3, 0.04, 0.18),
         (-0.3, 0.0, 0.11)]
    k.add("tail", tube(P, [0.03, 0.028, 0.025, 0.021, 0.018, 0.016]), MAUVE, "tail")


# --- the eyes: sly yellow almonds slanting up to the outer corner, a dark lid over them ---------------------------------
CPK, CY, CK, CW = srgb(PINK), srgb(EYEYEL), srgb(INK), srgb(WHITE)


def top(u):
    return 0.15 + 0.38 * u


def bottom(u):
    return -0.6 + 0.2 * u + 0.25 * u * u


def eye(u, v):
    c = np.tile(CPK, (len(u), 1))
    inner = (v < top(u)) & (v > bottom(u)) & (np.abs(u) < 0.96)
    c[inner] = CY
    c[inner & (((u - 0.0) / 0.16) ** 2 + ((v + 0.15) / 0.34) ** 2 < 1)] = CK
    c[(np.abs(v - top(u) - 0.03) < 0.15) & (np.abs(u) < 0.99)] = CK
    c[inner & (np.abs(v - bottom(u)) < 0.1)] = CK
    return c


def eyes(k):
    de = unit(0.5, -0.86, 0.1)
    E = H + de * np.array([HR[0], HR[1], HR[2]]) * 0.96 + (0, 0, 0.006)
    def lay(state):
        ink = CK if state in ("Open", "Fierce") else srgb("#9A3A62")
        k.decal(E, de, (0.03, 0.025), eye_look(state, eye, CPK, ink, line=0.17), eps=0.004, rings=20, segs=56,
                mirror=True)
    return lay


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="fur")
    T.CLOUD = CLOUD
    T.CLOUD_GROW = 1.25
    T.cloud(k)
    torso(k)
    head(k)
    coil(k)
    tail(k)
    k.build(voxel=0.0058, band=0.005)
    k.eye_states(eyes(k))
    k.export(out)
