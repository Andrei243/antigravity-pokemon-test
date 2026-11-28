# Floricolt (No. 10078, Carpathia) sculpted from nothing on the skeleton and clips of the game's own export of it:
#   blender --background --factory-startup --python sculpts/floricolt.py -- <the export's Floricolt.glb> <out.glb>
#
# From its picture (docs/art/reference/gemini/carpathia/10078-Floricolt.jpg): a tiny edelweiss, its head a great
# felted white star (five broad pointed petals with five greyer ones between, behind) round a pale yellow woolly disc
# that is its face: round black eyes with two glints, a small open smile, pink cheeks; under it a short sage-green
# woolly egg of a body on two stub feet, two fuzzy stub arms each with a smooth darker leaf. The head is as wide as
# three quarters of the whole height. Behind the flower, a green star of sepals where the stem holds it.
# Coltarina (coltarina.py) is grown from this frame: star_flower, the palette and the eye.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, lumpy, outward, leaf, args

PETAL, PETAL_BACK, PETAL_GREY = "#F4F3EC", "#E2E1D8", "#D8D7CD"
DISC, DISC_DEEP = "#EEE59A", "#DCCF78"
STEM, STEM_DARK, LEAF, SEPAL = "#8FA595", "#7D9483", "#76957F", "#7E9A84"
INK, GLINT, MOUTH, TONGUE, BLUSH = "#1E1A18", "#FFFFFF", "#7E2C2C", "#E07A7A", "#EFB2A0"


def petal(base, d, n, length, width, thick, cup=0.16):
    """A felted pointed petal from base along d, its face to n: broad near its root, a point at its end, the tip
    turned a little toward n (a shallow bowl). Loose and wound outward."""
    d = np.array(d, float); d /= np.linalg.norm(d)
    n = np.array(n, float); n = n - d * (n @ d); n /= np.linalg.norm(n)
    t_ = np.cross(n, d)
    def shape(V):
        V = V.copy()
        t = (V[:, 2] + 1) / 2
        w = np.sqrt(np.clip(t, 0, 1)) * (1 - np.clip(t, 0, 1) ** 2.2) * 1.55
        V[:, 0] *= w
        V[:, 1] = V[:, 1] * (0.4 + 0.6 * w)
        return V
    V, F = ell((0, 0, 0), (width, thick, length / 2), deform=shape, sub=3)
    # local (x, y, z) -> (t, n, d); y was lifted by the cup in units of thick: scale it in metres
    V = V.copy()
    M = np.stack([t_, n, d], axis=1)
    P = V @ M.T + np.array(base, float) + d * length / 2
    tt = np.clip(((P - np.array(base, float)) @ d) / length, 0, 1)
    P = P + np.outer(tt * tt, n) * cup * length
    return outward((P, F))


def star_flower(k, prefix, c, nrm, bone, big=5, L=0.15, W=0.056, T=0.013, small_L=0.12, small_W=0.042,
                disc=(0.072, 0.04), turn=90.0, fused_disc=True, back=True):
    """An edelweiss star facing nrm: big petals in front, greyer smaller ones between and behind them, a woolly
    yellow disc in the middle, the flower's back white and a star of green sepals behind (back)."""
    c = np.array(c, float)
    n = unit(*nrm)
    up = np.array([0, 0, 1.0]) - n * n[2]; up /= np.linalg.norm(up)
    right = np.cross(up, n)
    for i in range(big * 2):
        a = math.radians(turn + 180.0 * i / big)
        o = up * math.sin(a) + right * math.cos(a)
        front = i % 2 == 0
        ln, wd = (L, W) if front else (small_L, small_W)
        d = o - n * (0.12 if front else 0.05)
        base = c + o * disc[0] * 0.35 + n * (-0.004 if front else -0.014)
        k.add(f"{prefix}petal{i}", petal(base, d, n, ln, wd, T), PETAL if front else PETAL_GREY, bone,
              fuse=False, surface="cloth")
    rd, depth = disc
    M = np.stack([right, up, n], axis=1)
    def turned(r, at, deform=None, sub=4):
        V, F = ell((0, 0, 0), r, deform=deform, sub=sub)
        return outward((V @ M.T + at, F))
    if fused_disc:
        k.add(f"{prefix}disc", turned((rd, rd, depth), c + n * depth * 0.25, lumpy(9, 7, 0.035)), DISC, bone,
              surface="fur")
    else:
        k.add(f"{prefix}disc", turned((rd, rd, depth), c + n * depth * 0.25, lumpy(5, 4, 0.05), 3), DISC, bone,
              fuse=False, surface="fur")
    if back:
        k.add(f"{prefix}hold", turned((rd * 0.9, rd * 0.9, depth * 0.9), c - n * depth * 0.9), PETAL_BACK, bone,
              surface="cloth")
        for i in range(5):
            a = math.radians(turn + 36 + 72 * i)
            o = up * math.sin(a) + right * math.cos(a)
            k.add(f"{prefix}sepal{i}", petal(c - n * depth * 1.4 + o * rd * 0.2, o - n * 0.6, -n, rd * 1.5, rd * 0.42,
                                             T * 0.9), SEPAL, bone, fuse=False, surface="leaf")
    return c, n, up, right


def round_eye(u, v):
    """A round black eye with a big glint high on its inner side and a small one low."""
    c = np.tile(srgb(INK), (len(u), 1))
    c[((u + 0.25) / 0.34) ** 2 + ((v - 0.3) / 0.34) ** 2 < 1] = srgb(GLINT)
    c[((u - 0.3) / 0.15) ** 2 + ((v + 0.35) / 0.15) ** 2 < 1] = srgb(GLINT)
    return c


def floricolt(src, out):
    k = Kit(src, coat="fur")
    FC = np.array([0.0, -0.02, 0.272])
    FN = (0, -1, 0.14)
    c, n, up, right = star_flower(k, "", FC, FN, "head")
    # --- the woolly egg of a body, its stub feet and arms ---
    k.add("body", ell((0, 0.0, 0.092), (0.072, 0.062, 0.088), deform=lumpy(8, 6, 0.03)), STEM, "body")
    k.add("neck", tube([(0, 0.0, 0.16), (0, 0.0, 0.22)], [0.03, 0.028]), STEM, "head")
    for s, sd in ((1, "R"), (-1, "L")):
        k.add(f"foot{sd}", ell((0.04 * s, -0.012, 0.02), (0.03, 0.034, 0.022)), STEM, f"leg{sd}")
        k.add(f"arm{sd}", tube([(0.05 * s, 0.0, 0.125), (0.08 * s, -0.018, 0.098), (0.094 * s, -0.03, 0.08)],
                               [0.024, 0.02, 0.017]), STEM, f"arm{sd}")
        k.add(f"leaf{sd}", outward(leaf((0.07 * s, 0.012, 0.115), (0.85 * s, 0.3, 0.42), 0.115, 0.05, thick=0.009)),
              LEAF, f"arm{sd}", fuse=False, surface="leaf")
    # the body's fuzz a little darker low down
    k.paint(lambda P: inside(P[:, 2] - 0.04, 0.02), STEM_DARK, on=["body", "foot"])
    # --- the face on the disc: a small open smile and pink cheeks ---
    front = c + n * 0.04 * 1.25
    def on_disc(x, z):
        return front + right * x + up * z - n * (0.04 * (x * x + z * z) / 0.072 ** 2) * 0.9
    def smile(u, v):
        col = np.tile(srgb(MOUTH), (len(u), 1))
        col[(u / 0.55) ** 2 + ((v + 0.45) / 0.45) ** 2 < 1] = srgb(TONGUE)
        return col
    D_SMILE = lambda th: 1.0 if math.sin(th) <= 0 else 0.35
    k.decal(on_disc(0, -0.026), n, (0.013, 0.012), smile, eps=0.0016, outline=D_SMILE, rings=10, segs=48)
    k.decal(on_disc(0.046, -0.02), n + right * 0.4, (0.012, 0.007), BLUSH, eps=0.0012, rings=8, segs=36, mirror=True)
    k.build(voxel=0.0034, smooth=4, band=0.004, wband=0.03)

    EYE_P = on_disc(0.03, 0.004)
    EYE_N = n + right * 0.35
    def eyes(state):
        look = round_eye if state == "Open" else eye_look(state, round_eye, srgb(DISC), srgb(INK), line=0.2)
        k.decal(EYE_P, EYE_N, (0.0125, 0.0145), look, eps=0.0018, rings=20, segs=56, mirror=True)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    floricolt(*args())
