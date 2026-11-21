# Minccino sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/minccino.py -- <the export's Minccino.glb> <out.glb>
# From the references: a little grey chinchilla, a big round head on a small pear of a body; great rounded ears up and
# out at some 43 degrees, cupped, pink inside with a grey fold low in the cup and white at their tips; one curl of fur
# up from the crown; big oval dark eyes with a glint, a tiny nose and a small smile; a pale tuft of fur at the chest;
# short arms a little out and down; little feet; a great fluffy tail out behind to the right and up, white at its end.
# Written to be grown from (cinccino.py): the colours, the ears, the eyes and the smile are names and functions here.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, eye_look, args

GREY, LIGHT, WHITE, PINK = "#B4B3BA", "#DCDBE0", "#F8F8FA", "#EE9AAC"
EYE, EYELOW, INK, MOUTH = "#4A3430", "#7A5A50", "#2A1E1E", "#4A3434"


# --- the line's own parts, shared with Cinccino ---------------------------------------------------------------------
def ear(k, base, tilt, length, width, thick, bone, turn=15, colour=GREY, tip=WHITE, lining=PINK, fold=True):
    """A great cupped ear up and out from base (on +X; mirrored), tilt degrees from upright: grey, pink inside, a white
    tip, a grey fold low in its cup."""
    base = np.array(base, float)
    d = np.array([math.sin(math.radians(tilt)), 0, math.cos(math.radians(tilt))])
    C = base + d * length * 0.5
    def cup(V):
        V = V.copy()
        V[:, 1] += 0.6 * np.clip(1 - V[:, 0] ** 2 - V[:, 2] ** 2, 0, 1)
        return V
    k.add("ear", ell(C, (width, thick, length * 0.5), rot=(0, tilt, turn), deform=cup), colour, bone, mirror=True)
    k.add("earstem", tube([base - d * 0.01, base + d * length * 0.18], [width * 0.45, width * 0.5]), colour, bone,
          mirror=True)
    m = np.array(__import__("mathutils").Euler([0, math.radians(tilt), math.radians(turn)]).to_matrix())
    n = m @ np.array([0, -1.0, 0])
    if fold:
        F = C - d * length * 0.24 - (m @ np.array([1.0, 0, 0])) * width * 0.25 + n * thick * 0.1
        k.add("earfold", ell(F, (width * 0.36, thick * 0.9, length * 0.17), rot=(0, tilt - 12, turn)), colour, bone,
              mirror=True)
    k.decal(C + d * length * 0.05 + (m @ np.array([1.0, 0, 0])) * width * 0.08 + n * thick * 0.4, n,
            (width * 0.62, length * 0.33), lining, up=d, eps=0.0025, rings=16, segs=48, mirror=True)
    def tips(P):
        Q = P.copy(); Q[:, 0] = np.abs(Q[:, 0])
        s = (Q - base) @ d
        side = np.linalg.norm((Q - base) - np.outer(s, d), axis=1)
        return inside(length * 0.83 - s, 0.002) * (side < width * 1.3) * (Q[:, 0] > base[0])
    k.paint(tips, tip, on=["ear"])
    return C, d, n


def oval_eye(eye, low, white):
    E, L, W = srgb(eye), srgb(low), srgb(white)
    def look(u, v):
        c = np.tile(E, (len(u), 1))
        c[(u / 0.8) ** 2 + ((v + 0.75) / 0.42) ** 2 < 1] = L
        c[((u - 0.22) / 0.3) ** 2 + ((v - 0.4) / 0.26) ** 2 < 1] = W
        return c
    return look


def lay_eyes(k, state, E, n, half, look, lid, ink):
    k.decal(E, n, half, eye_look(state, look, srgb(lid), srgb(ink), line=0.15), eps=0.003, rings=20, segs=56,
            mirror=True)


def smile(fur, ink, w=0.26):
    F, K = srgb(fur), srgb(ink)
    def look(u, v):
        c = np.tile(F, (len(u), 1))
        c[(np.abs(v - (-0.3 + 0.75 * u * u)) < w) & (np.abs(u) < 0.88)] = K
        return c
    return look


def face(k, HC, HR, fur, nose_at, mouth_at, size=1.0):
    """The tiny nose and the small smile under it."""
    k.decal(HC + nose_at, unit(0, -1, 0.1), (0.0055 * size, 0.0045 * size), MOUTH, eps=0.0025, rings=5, segs=20)
    k.decal(HC + mouth_at, unit(0, -1, -0.15), (0.017 * size, 0.0095 * size), smile(fur, MOUTH), eps=0.0025,
            rings=10, segs=40)


# --- Minccino ----------------------------------------------------------------------------------------------------------
HC = np.array([0.0, -0.01, 0.205])
HR = np.array([0.092, 0.08, 0.08])
TAIL = [(0, 0.04, 0.07), (0.04, 0.12, 0.06), (0.1, 0.18, 0.085), (0.14, 0.205, 0.155), (0.15, 0.195, 0.235)]
TRAD = [0.02, 0.034, 0.045, 0.048, 0.036]


def minccino(src, out):
    k = Kit(src, coat="fur")

    # The big round head on a small pear of a body
    k.add("head", ell(HC, HR), GREY, "head")
    k.add("cheek", ell(HC + (0.035, -0.022, -0.032), (0.05, 0.05, 0.045)), GREY, "head", mirror=True)
    k.add("body", ell((0, 0.002, 0.08), (0.056, 0.05, 0.062)), GREY, "body")

    # One curl of fur up from the crown
    k.add("curl", blade([(0.004, 0.0, 0.26), (0.008, -0.006, 0.3), (-0.004, -0.004, 0.33), (-0.024, 0.006, 0.338)],
                        [0.03, 0.022, 0.016, 0.009], (0, 1, 0), 0.6), GREY, "head")

    # The great ears
    ear(k, (0.05, 0.004, 0.258), 43, 0.18, 0.06, 0.02, "ear{s}")

    # The pale tuft of fur at the chest
    for i, (x, z, a) in enumerate(((0.0, 0.118, 0), (0.016, 0.108, 30), (-0.016, 0.108, -30))):
        k.add(f"ruff{i}", ell((x, -0.044, z), (0.014, 0.012, 0.022), rot=(10, a, 0)), LIGHT, "body")

    # Short arms a little out and down, little round paws
    k.add("arm", tube([(0.04, -0.02, 0.135), (0.07, -0.032, 0.118), (0.088, -0.04, 0.106)], [0.019, 0.017, 0.016]),
          GREY, "arm{s}", mirror=True)
    k.add("paw", ell((0.094, -0.043, 0.102), (0.019, 0.017, 0.015)), GREY, "arm{s}", mirror=True)

    # Short legs and little feet
    k.add("thigh", ell((0.032, -0.008, 0.036), (0.03, 0.03, 0.03)), GREY, "leg{s}", mirror=True)
    k.add("foot", ell((0.034, -0.03, 0.012), (0.022, 0.032, 0.012)), GREY, "leg{s}", mirror=True)

    # The great fluffy tail out behind to the right and up, white at its end
    k.add("tail", tube(TAIL, TRAD, seg=24), GREY, "tail")
    k.add("tailtip", ell((0.152, 0.19, 0.258), (0.034, 0.034, 0.04)), WHITE, "tail")
    k.paint(lambda P: inside(0.24 - P[:, 2], 0.002) * (P[:, 0] > 0.09) * (P[:, 1] > 0.12), WHITE, on=["tail"])

    # The nose and the smile
    face(k, HC, HR, GREY, (0, -0.08, -0.022), (0, -0.078, -0.036))

    # Big oval dark eyes, wide apart
    de = unit(0.48, -0.87, -0.08)
    E = HC + de / np.linalg.norm(de / HR)
    look = oval_eye(EYE, EYELOW, WHITE)
    def eyes(state):
        lay_eyes(k, state, E, de, (0.021, 0.029), look, GREY, INK)

    k.build(voxel=0.0034)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    minccino(*args())
