# Wooper-Paldea sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/wooperpaldea.py -- <the export's Wooper-Paldea.glb> <out.glb>
#
# Grown from wooper.py (which builds as it is read, so its frame is copied here): the same small mudfish on two stubby
# legs with no arms, a big round head wider than tall, small black eyes with a glint set far apart, a narrow body and a
# thick flat paddle of a tail. What the Paldean pictures change: the whole body a greyish brown; the mouth open far
# wider, a broad oval taking most of the lower face, salmon pink inside with a darker red roof; each gill a pale lilac
# pair of thick bones splayed out from the head's side in a "<", one up and one down, each ending in a double knob
# (Kanto's is a stalk with thin branches); a dark brown fishbone down the belly, a spine with three ribs, in place of
# Kanto's three blue bars. The task's "dark gills like bars and a darker back" is not in the pictures: the gills are
# pale lilac, the dark bars are the belly's fishbone, and the back is the body's own brown.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, eye_look, outward, args


def wooper_paldea(src, out_path):
    k = Kit(src, coat="fur")
    BROWN, MARK, LILAC = "#7E6E6C", "#4A3836", "#DCCDEA"

    HC = np.array([0, -0.005, 0.298])         # the head's centre
    HR = np.array([0.14, 0.112, 0.115])

    def dome(V):
        V = V.copy()
        V[:, 0] *= 1 + 0.06 * np.clip(V[:, 2], 0, 1)
        return V
    k.add("head", ell(HC, HR, deform=dome), BROWN, "head")

    def pear(V):
        V = V.copy()
        s = 1 + 0.18 * np.clip(-V[:, 2], 0, 1) - 0.18 * np.clip(V[:, 2], 0, 1)
        V[:, 0] *= s; V[:, 1] *= s
        return V
    k.add("body", ell((0, 0.0, 0.128), (0.062, 0.058, 0.08), deform=pear), BROWN, "body")
    k.add("neck", tube([(0, 0.0, 0.17), (0, -0.002, 0.205)], [0.045, 0.066]), BROWN, "head")

    # two stubby legs, apart, with round feet
    k.add("leg", tube([(0.034, 0.0, 0.09), (0.038, -0.006, 0.03)], [0.025, 0.023]), BROWN, "leg{s}", mirror=True)
    k.add("foot", ell((0.039, -0.012, 0.02), (0.026, 0.031, 0.02)), BROWN, "leg{s}", mirror=True)

    # the tail: a thick root out of the rump and a broad flat paddle rising behind, swept to one side
    k.add("tailroot", tube([(0, 0.035, 0.1), (0.01, 0.075, 0.11), (0.022, 0.1, 0.12)], [0.036, 0.032, 0.03]), BROWN,
          "tail")
    k.add("paddle", ell((0.045, 0.15, 0.135), (0.068, 0.074, 0.03), rot=(28, 0, -28)), BROWN, "tail")

    # the fishbone down the belly: a spine with a knob at its top and three ribs, the middle one longest
    def fishbone(P):
        a = np.arctan2(P[:, 0], -P[:, 1]) * 0.058          # along the belly, in metres round the body's axis
        z = P[:, 2]
        # the spine
        dz = np.maximum(np.maximum(0.088 - z, z - 0.182), 0)
        m = inside(np.hypot(np.abs(a), dz) - 0.0075, 0.0025)
        for zc, w in ((0.176, 0.012), (0.152, 0.026), (0.126, 0.031), (0.1, 0.027)):
            dx = np.maximum(np.abs(a) - w, 0)
            m = np.maximum(m, inside(np.hypot(dx, z - zc) - 0.0072, 0.0025))
        return m * (P[:, 1] < 0)
    k.paint(fishbone, MARK, on=["body", "neck"])

    # the gills: on each side a pair of thick lilac bones splayed out from the head's side, each with a double knob
    for s in (1, -1):
        sd = "R" if s > 0 else "L"
        root = np.array([0.118 * s, 0.012, 0.305])
        for j, up in enumerate((1, -1)):
            d = unit(0.78 * s, 0.3, 0.62 * up)
            tip = root + d * 0.085
            k.add(f"gill{sd}{j}", outward(tube([root - d * 0.01, (root + tip) / 2, tip], [0.019, 0.017, 0.018],
                                                  seg=12)), LILAC, f"ear{sd}", fuse=False)
            across = unit(-0.62 * up * s, 0.0, 0.78)      # in the gills' plane, across the bone
            for side in (1, -1):
                k.add(f"knob{sd}{j}{side}", outward(ell(tip + d * 0.006 + across * side * 0.014,
                                                        (0.019, 0.019, 0.019), sub=3)), LILAC, f"ear{sd}",
                      fuse=False)

    # the mouth: a broad oval open over most of the lower face, the roof dark red, salmon pink below
    ROOF, PINK = srgb("#9C3A3C"), srgb("#E47C74")

    def mouth(u, v):
        c = np.tile(PINK, (len(u), 1))
        c[v > 0.42 - 0.25 * u * u] = ROOF
        return c
    k.decal((0, -0.108, 0.262), unit(0, -1, -0.32), (0.067, 0.046), mouth, eps=0.003, rings=40, segs=96)

    k.build(voxel=0.004)

    # the eyes: small and black with a white glint, far apart on the face
    INK, LID = srgb("#121216"), srgb(BROWN)

    def eye(u, v):
        c = np.tile(INK, (len(u), 1))
        c[(u / 0.34) ** 2 + ((v - 0.05) / 0.34) ** 2 < 1] = (1, 1, 1)
        return c
    de = unit(0.52, -0.8, 0.34)

    def eyes(state):
        k.decal(HC + de * 0.1, de, (0.0135, 0.0145), eye_look(state, eye, LID, INK, line=0.22), eps=0.004,
                rings=20, segs=56, mirror=True)
    k.eye_states(eyes)
    k.export(out_path)


if __name__ == "__main__":
    src, out = args()
    wooper_paldea(src, out)
