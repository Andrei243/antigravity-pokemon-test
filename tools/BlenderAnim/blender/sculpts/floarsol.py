# Floarsol sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/floarsol.py -- <the export's Floarsol.glb> <out.glb>
#
# Carpathia No. 10040 (plan 46, CP-dex-1b, "40. Floarsol: the sunflower sun"), grown from Soarel's script
# (sibling("soarel")): the line's greens, its disc and petal rings, its leaf, its smile. From the project's picture:
# a great golden face like a small sun, ringed by two rows of long pointed petals, deep orange behind and
# orange-gold in front, green sepals peeping round them; open brown eyes with a glint under brown brows, a few dark
# seeds on the brow and freckles of seeds on the cheeks, a small nose and a warm smile; a thick stalk of twisted green
# strands; two broad leaves for arms, their edges scorched orange (carried out, forward and down here, where the
# picture lifts one); splayed roots for feet; wisps of heat by the head.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, unit, eye_look, inside, args, outward, sibling

sl = sibling("soarel")

GREEN, GREEN_DEEP = sl.GREEN, sl.GREEN_DEEP
STALK, STRAND = "#62A64A", "#4C8C3A"
FACE, FACE_RIM = "#F4C640", "#E2A430"
PETAL_BACK, PETAL_FRONT = "#E2701C", "#F29A28"
SCORCH, HEAT = "#F0A030", "#F7A65A"
BROWN, IRIS = srgb("#6A3E1C"), srgb("#8A5428")
SEED = "#5A3618"
BLACK, WHITE = srgb("#1E120A"), np.array([1.0, 1.0, 1.0])

# --- Floarsol's frame (joints: body (0, 0, 0.398), arms (+-0.04, 0, 0.548), head (0, 0, 0.698), legs (+-0.06, 0, 0.118))
FC = np.array([0.0, -0.062, 0.84])        # the face's middle
FN = unit(0.0, -1.0, 0.16)
DISC_R, DISC_T = 0.142, 0.055


def stalk(k):
    k.add("stalk", tube([(0, 0.0, 0.14), (0, 0.0, 0.42), (0, 0.004, 0.72)], [0.08, 0.062, 0.056]), STALK, "body")
    for strand in range(2):
        pts = []
        for i in range(9):
            t = i / 8
            a = strand * math.pi + t * 2.2 * math.pi
            pts.append((math.cos(a) * 0.045, math.sin(a) * 0.04, 0.16 + 0.56 * t))
        k.add(f"strand{strand}", tube(pts, [0.05 - 0.012 * (i / 8) for i in range(9)]), STALK if strand else STRAND,
              "body")
    # where the stalk meets the head, the calyx
    k.add("calyx", sl.oriented(FC - FN * 0.05, FN, 0.1, 0.06), GREEN_DEEP, "head")


def roots(k):
    """Splayed roots, three a side, thick at the stalk and tapering out over the ground."""
    for s, nm in ((1, "R"), (-1, "L")):
        for i, (ax, ay, L) in enumerate(((0.95, -0.3, 0.26), (0.45, -0.9, 0.22), (0.55, 0.85, 0.2))):
            d = np.array([ax * s, ay, 0.0]); d /= np.linalg.norm(d)
            p0 = np.array([0.03 * s, 0.0, 0.2])
            pts = [p0, p0 + d * L * 0.4 + (0, 0, -0.12), p0 + d * L * 0.8 + (0, 0, -0.165), p0 + d * L + (0, 0, -0.17)]
            k.add(f"root{nm}{i}", tube(pts, [0.06, 0.042, 0.026, 0.018]), STRAND, f"leg{nm}")


def arms(k):
    """Broad leaves, a scorched orange rim round each: a larger thinner orange plate under the green."""
    for s, nm in ((1, "R"), (-1, "L")):
        base = (0.06 * s, -0.01, 0.56)
        d = (0.86 * s, -0.36, -0.2)
        n = (0.05 * s, -0.55, 0.85)
        sl.add_leaf(k, f"arm{nm}", base, d, n, 0.34, 0.1, f"arm{nm}", droop=0.16, fold=0.18, thick=0.03,
                    colour=STALK, vein=sl.VEIN)
        k.add(f"arm{nm}rim", sl.leaf_plate(base, d, n, 0.365, 0.112, droop=0.16, fold=0.18, thick=0.02), SCORCH,
              f"arm{nm}", fuse=False, surface="leaf")


def flower(k):
    k.add("disc", sl.oriented(FC, FN, DISC_R, DISC_T), FACE, "head")
    right, up, nn = sl.frame(FN)
    def rim(P):
        q = P - FC
        r = np.sqrt((q @ right) ** 2 + (q @ up) ** 2)
        return smooth(r - 0.112)
    k.paint(rim, FACE_RIM, on=["disc"])
    # green sepals behind, longest below
    sl.petal_ring(k, "sepal", FC, FN, 9, 270 + 20, 0.112, 0.17, 0.045, -12, GREEN, 0.03, back=0.04,
                  outline=sl.pointed_outline, lengths=[1.0, 0.85, 0.6, 0.5, 0.45, 0.5, 0.6, 0.85, 1.0])
    sl.petal_ring(k, "petalBack", FC, FN, 18, 0.0, 0.125, 0.155, 0.041, 4, PETAL_BACK, 0.03, back=0.022,
                  outline=sl.pointed_outline)
    sl.petal_ring(k, "petalFront", FC, FN, 16, 360 / 32, 0.133, 0.116, 0.039, 12, PETAL_FRONT, 0.03, back=0.0,
                  outline=sl.pointed_outline)


def smooth(d):
    return 1 - inside(d, 0.008)


def heat(k):
    """Wisps of heat by the head: short glowing curls at its sides and over it."""
    for i, (x, z, h) in enumerate(((0.3, 0.82, 0.09), (0.27, 0.98, 0.07))):
        pts = [(x, 0.01, z), (x + 0.025, 0.01, z + h * 0.4), (x - 0.01, 0.01, z + h * 0.75), (x + 0.01, 0.01, z + h)]
        k.add(f"heat{i}", outward(tube(pts, [0.022, 0.021, 0.018, 0.014])), SCORCH, "head", fuse=False, mirror=True,
              glow=HEAT)


def eye_open(u, v):
    """A brown eye: a dark lash over its top, a brown iris round a black pupil, a big glint high to the outside."""
    c = np.tile(WHITE, (len(u), 1))
    r = np.sqrt((u * 0.95) ** 2 + (v + 0.05) ** 2)
    c[r < 0.82] = IRIS
    c[r < 0.42] = BLACK
    c[((u - 0.22) / 0.26) ** 2 + ((v - 0.3) / 0.24) ** 2 < 1] = WHITE
    c[((u + 0.3) / 0.12) ** 2 + ((v + 0.38) / 0.12) ** 2 < 1] = WHITE
    c[v > 0.62 + 0.12 * u] = BROWN
    return c


def face(k):
    right, up, nn = sl.frame(FN)
    front = FC + nn * DISC_T * 0.97
    def at(u, v):
        return front + right * u + up * v
    # brows, curving over the eyes
    def brow(u, v):
        c = np.tile(srgb(FACE), (len(u), 1))
        c[np.abs(v - (-0.3 + 0.7 * (1 - u * u))) < 0.32] = BROWN
        return c
    k.decal(at(0.054, 0.068), FN, (0.028, 0.009), brow, up=up, eps=0.004, rings=8, segs=40, mirror=True)
    # seeds on the brow, freckles on the cheeks, the nose
    for i, (u, v, a, b) in enumerate(((0.0, 0.104, 0.009, 0.013), (-0.034, 0.094, 0.008, 0.011), (0.034, 0.096, 0.008, 0.011))):
        k.decal(at(u, v), FN, (a, b), SEED, up=up, eps=0.0042, rings=6, segs=24)
    for u, v in ((0.08, -0.014), (0.097, -0.032), (0.071, -0.039)):
        k.decal(at(u, v), FN, (0.0055, 0.0055), SEED, up=up, eps=0.0042, rings=5, segs=20, mirror=True)
    k.decal(at(0.0, -0.016), FN, (0.007, 0.006), FACE_RIM, up=up, eps=0.0042, rings=6, segs=24)
    k.decal(at(0.0, -0.057), FN, (0.036, 0.016), sl.smile_patch(FACE, 0.26), up=up, eps=0.004, rings=12, segs=48)


def floarsol(src, out):
    k = Kit(src, coat="leaf")
    stalk(k)
    roots(k)
    arms(k)
    flower(k)
    heat(k)
    face(k)
    k.build(voxel=0.008, smooth=4, band=0.008)

    right, up, nn = sl.frame(FN)
    p = FC + nn * DISC_T * 0.97 + up * 0.018 + right * 0.053
    def eyes(state):
        k.decal(p, FN, (0.026, 0.03), eye_look(state, eye_open, srgb(FACE), BROWN, line=0.17), up=up, eps=0.005,
                rings=20, segs=56, mirror=True)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    floarsol(*args())
