# Moltres sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/moltres.py -- <the export's Moltres.glb> <out.glb>
# A long slender orange-gold bird on articuno.py's frame (its wing axes, legs and eyes cut along a brow): a long neck,
# a small head with a fierce eye under a dark brow and a long straight brown beak, narrow gold wings raised in a V and
# swept back with great tongues of fire rising from their upper edges, a flame for a crest laid back over the nape, a
# tail of fire streaming back, and short thin brown legs tucked under. The flames are Rapidash's way (ponyta.py's ragged
# tongues), each wound outward, an orange-red tongue with a bright yellow core on both its faces.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, srgb, smoothstep, unit, outward, sibling, args

a = sibling("articuno")
sp = a.sp

GOLD, GOLD2 = "#F6C33C", "#EFB12E"
FLAME, CORE = "#F8501C", "#FFB42A"
BEAK, LEG, CLAW = "#C27A46", "#8E6656", "#4A3A36"
WHITE, PUPIL, INK = "#FBFBF8", "#1A1210", "#4A2416"

H = np.array([0, -0.2, 0.54])           # the head's centre, carried forward of the head's joint on a long neck
HR = np.array([0.062, 0.072, 0.06])


# --- the flames (ponyta.py's tongue) -----------------------------------------------------------------------------------
def frame(d, side):
    d = np.array(d, float); d /= np.linalg.norm(d)
    s = np.array(side, float); s -= d * np.dot(s, d); s /= np.linalg.norm(s)
    return np.stack([s, np.cross(d, s), d], axis=1)


count = [0]


def tongue(base, d, length, width, side, flick=0.3, thick=0.4, rag=0.24, waves=1.5, root=0.5):
    count[0] += 1
    ph = 2.3 * count[0]
    def shape(V):
        V = V.copy()
        t = np.clip((V[:, 2] + 1) / 2, 0, 1)
        w = (1 - t ** 1.1) ** 1.15 * (root + (1.55 - root) * np.sqrt(t))
        s = np.where(V[:, 0] >= 0, 1.0, -1.0)
        edge = 1 + rag * np.sin(2 * math.pi * waves * t + ph + s * 2.1) * smoothstep(0.1, 0.35, t)
        V[:, 0] *= w * edge; V[:, 1] *= w
        V[:, 0] += flick * (2.1 * t ** 2.4 - 0.55 * np.sin(math.pi * t))
        return V
    M = frame(d, side)
    V, F = ell((0, 0, 0), (width, width * thick, length / 2), deform=shape, sub=3)
    return outward((V @ M.T + np.array(base, float) + M[:, 2] * length / 2, F))


groups = {}


def flame(base, d, length, width, bone, side, flick=0.3):
    """A thick tongue of fire, orange-red, with a bright core showing on both its faces."""
    b = np.array(base, float)
    groups.setdefault((bone, FLAME), []).append(tongue(b, d, length, width, side, flick))
    ny = frame(d, side)[:, 1]
    for sg in (1, -1):
        groups.setdefault((bone, CORE), []).append(
            tongue(b + ny * sg * width * 0.25, d, length * 0.5, width * 0.5, side, flick * 0.5, thick=0.3, rag=0.15,
                   root=0.25))


def add_flames(k):
    for i, ((bone, colour), shapes) in enumerate(groups.items()):
        V, F, off = [], [], 0
        for sv, sf in shapes:
            V.append(sv); F.extend([[j + off for j in f] for f in sf]); off += len(sv)
        k.add(f"flame{i}", (np.vstack(V), F), colour, bone, fuse=False, mirror="{s}" in bone,
              glow="#FF7A2A" if colour == FLAME else "#FFD860")


# --- Moltres -----------------------------------------------------------------------------------------------------------
ELEV, SWEEP = 34, 26
SPAN = 0.4
ARM = [(0.0, -0.025), (SPAN * 0.5, -0.03), (SPAN, -0.012), (SPAN + 0.03, 0.02), (SPAN * 0.7, 0.07), (0.14, 0.1),
       (0.0, 0.07)]


def moltres(src, out):
    k = Kit(src, coat="fur")

    # A long slender trunk leaning forward, a long neck carrying the small head out and up
    k.add("body", ell((0, 0.03, 0.385), (0.085, 0.15, 0.088), rot=(-14, 0, 0)), GOLD, "body")
    k.add("breast", ell((0, -0.07, 0.41), (0.075, 0.07, 0.075)), GOLD, "body")
    k.add("neck", tube([(0, -0.08, 0.43), (0, -0.14, 0.48), H + (0, 0.02, -0.02)], [0.054, 0.046, 0.046]), GOLD,
          "head")
    k.add("head", ell(H, HR), GOLD, "head")

    # The long straight brown beak
    B = H + (0, -0.058, -0.006)
    D = unit(0, -0.95, -0.28)
    k.add("beak", tube([B, B + D * 0.08, B + D * 0.17], [0.022, 0.013, 0.002], seg=20), BEAK, "head", fuse=False,
          surface="shell")

    # The narrow gold wings raised in a V and swept back, and fire rising from their upper (leading) edges
    ex, ez = a.wing_axes(ELEV, SWEEP, (0.3, 0.6, -0.75))
    root = np.array([0.07, 0.0, 0.455])
    k.add("wing", slab(ARM, root, ex, ez, 0.026), GOLD, "wing{s}", fuse=False, mirror=True)
    k.add("shoulder", ell(root + ex * 0.02, (0.04, 0.05, 0.04)), GOLD, "wing{s}", mirror=True)
    for t, L, w in ((0.08, 0.2, 0.05), (0.18, 0.24, 0.056), (0.28, 0.24, 0.056), (0.37, 0.2, 0.05)):
        d = unit(*(-0.65 * ez + 0.3 * ex + np.array([0, 0.6, 0])))
        flame(root + ex * t - ez * 0.0, d, L, w, "wing{s}", ex, flick=0.25)
    # the wing's tip a flame pointing out along the span
    flame(root + ex * (SPAN - 0.02), unit(*(ex - 0.3 * ez)), 0.16, 0.042, "wing{s}", -ez, flick=0.2)

    # The crest: a flame laid back over the nape, two smaller beside it
    for x, d, L, w in ((0.0, (0, 1, 0.75), 0.16, 0.045), (0.025, (0.35, 1, 0.5), 0.12, 0.035),
                       (-0.025, (-0.35, 1, 0.5), 0.12, 0.035)):
        flame(H + (x, 0.0, 0.05), unit(*d), L, w, "head", (1, 0, 0), flick=0.25)

    # The tail: tongues of fire streaming back
    TB = np.array([0, 0.15, 0.35])
    for d, L, w in (((0, 1, 0.05), 0.28, 0.06), ((0.4, 1, 0.2), 0.22, 0.05), ((-0.4, 1, 0.2), 0.22, 0.05),
                    ((0, 1, 0.45), 0.2, 0.045)):
        flame(TB, unit(*d), L, w, "tail", (0, 0, 1) if abs(d[0]) < 0.1 else (1, 0, 0), flick=0.3)
    k.add("rump", ell(TB + (0, -0.03, 0.0), (0.05, 0.06, 0.045)), GOLD, "tail")
    add_flames(k)

    # Short thin brown legs tucked under
    k.add("thigh", ell((0.045, 0.01, 0.32), (0.035, 0.04, 0.04)), GOLD2, "leg{s}", mirror=True)
    for side in ((1, "R"), (-1, "L")):
        sp.foot(k, side, (0.047, 0.01, 0.31), (0.05, 0.04, 0.22), 0.04, LEG, CLAW, r=0.011)

    # The eyes: small, fierce, under a heavy brow slanting down toward the beak
    de = unit(0.62, -0.72, 0.18)
    E = a.eye_at(H, HR, de)
    def look(u, v):
        c = np.tile(srgb(WHITE), (len(u), 1))
        c[((u + 0.15) / 0.45) ** 2 + ((v + 0.15) / 0.55) ** 2 < 1] = srgb(PUPIL)
        c[((u - 0.0) / 0.12) ** 2 + ((v - 0.0) / 0.12) ** 2 < 1] = srgb(WHITE)
        c[v > 0.15 + 0.5 * u - 0.24] = srgb(INK)          # the heavy dark lid along the eye's top
        return c
    def eyes(state):
        sp.lay_eyes(k, state, E, de, (0.028, 0.024), look, lambda u: 0.15 + 0.5 * u, lambda u: -0.1 + 0.6 * u,
                    srgb(GOLD), srgb(INK))
    k.build(voxel=0.0066)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    moltres(*args())
