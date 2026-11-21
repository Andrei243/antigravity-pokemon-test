# Sandygast sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/sandygast.py -- <the export's Sandygast.glb> <out.glb>
#
# From the references: a pale sand heap shaped like a rounded cone, standing on a wide flat puddle of sand with a wavy
# rim and a stubby knob raised at each end of it; a few pale grey-blue pebbles on the puddle. Over the face a dark
# domino mask (two lobes joined, dipped at the middle) holding two pale grey-blue oval eyes; under it a great arched
# mouth that is a tunnel: dark within, and through it a rounded triangle of daylight. A red child's spade stuck in the
# top, its blade facing the front and its white handle leaning to the viewer's right (+X).
# The skeleton: root (0, 0, 0), body (0, 0, 0.12). The export stands 0.477 tall.
# Palossand is grown from this script (sibling("sandygast")): the puddle, the pebbles, the spade's blade, the carved
# arch of a mouth and the way of making eyes.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, slab, lathe, outward, srgb, inside, smoothstep, unit, eye_look, args

SAND, MASK, HOLLOW, EYE = "#E3DABD", "#46372E", "#3E3029", "#BFCCD6"
PEBBLE, RED, HANDLE = "#A9B9C6", "#E4464D", "#F2F2F4"


# --- the line's makes ---------------------------------------------------------------------------------------------
def puddle(k, name, c, rx, ry, h, colour, bone, n=7, amp=0.08, phase=0.6):
    """The flat puddle of sand: its rim in soft lobes, its sole flat on the ground."""
    def f(V):
        V = V.copy()
        th = np.arctan2(V[:, 1], V[:, 0])
        s = 1 + amp * (np.sin(n * th + phase) + 0.5 * np.sin((n + 3) * th + 2.1)) * smoothstep(0.4, -0.5, V[:, 2])
        V[:, 0] *= s; V[:, 1] *= s
        V[:, 2] = np.maximum(V[:, 2], -0.6)
        return V
    k.add(name, ell(c, (rx, ry, h), deform=f), colour, bone)


def mound(profile, c, squash):
    """A heap turned about the upright axis from (radius, height) pairs, squashed front to back by squash."""
    V, F = lathe(c, (0, 0, 1), [(0.0, profile[0][1])] + list(profile) + [(0.0, profile[-1][1] + 0.0001)], seg=56)
    V = V.copy()
    V[:, 1] = c[1] + (V[:, 1] - c[1]) * squash
    return outward((V, F))


def pebbles(k, spots, colour, half=(0.013, 0.008)):
    """Flat pale pebbles lying on the puddle: (x, y, z) on its surface each, laid from above and a little in front."""
    for x, y, z in spots:
        k.decal((x, y, z), unit(x * 0.6, y * 0.8 - 0.25, 1.0), half, colour, up=unit(x, y, 0.0), eps=0.0025,
                rings=8, segs=32)


BLADE = [(0.0, -0.065), (0.032, -0.042), (0.052, -0.01), (0.058, 0.02), (0.05, 0.042), (0.027, 0.047),
         (0.01, 0.043), (0.0, 0.04)]


def spade_blade(k, name, origin, tilt, scale, colour, bone, thick, front=(0.0, -1.0, 0.0)):
    """The spade's blade: a shield with lobed shoulders, a notch where the handle goes in and a point to dig with,
    facing front, its axis tilted by tilt degrees toward +X. Returns the handle's direction."""
    t = math.radians(tilt)
    ez = np.array([math.sin(t), 0.0, math.cos(t)])
    f = np.array(front, float); f /= np.linalg.norm(f)
    ex = np.cross(ez, f); ex /= np.linalg.norm(ex)
    half = [(x * scale, z * scale) for x, z in BLADE]
    outline = half + [(-x, z) for x, z in reversed(half[1:-1])]
    k.add(name, slab(outline, origin, ex, ez, thick), colour, bone, fuse=False, surface="shell")
    return ez


def arch(c, half, rot, flat=-0.78, depth=1.0):
    """A cutter for an arched opening: an ellipsoid with its bottom flattened, its top tilted back by rot degrees."""
    def f(V):
        V = V.copy()
        V[:, 2] = np.maximum(V[:, 2], flat)
        return V
    return ell(c, (half[0], half[1] * depth, half[2]), rot=(rot, 0, 0), deform=f)


def opening_mask(c, half, rot, flat=-0.78, grow=1.06):
    """1 inside the arch's outline seen from the front (x, z about its middle, the tilt allowed for)."""
    def mask(P):
        x = (P[:, 0] - c[0]) / (half[0] * grow)
        dz = (P[:, 2] - c[2]) / math.cos(math.radians(rot))
        z = dz / (half[2] * grow)
        r = np.sqrt(x * x + np.maximum(z, flat) ** 2)
        return inside(r - 1, 0.03) * (z > flat - 0.05)
    return mask


def oval_eye(fill, rim=None, rim_at=0.84):
    def look(u, v):
        c = np.tile(srgb(fill), (len(u), 1))
        if rim:
            c[u * u + v * v > rim_at ** 2] = srgb(rim)
        return c
    return look


# --- Sandygast ----------------------------------------------------------------------------------------------------
HEAP_C = np.array([0.0, 0.012, 0.0])
SQUASH = 0.86
HEAP = [(0.19, 0.03), (0.186, 0.06), (0.168, 0.1), (0.148, 0.14), (0.122, 0.19), (0.104, 0.225), (0.084, 0.255),
        (0.063, 0.278), (0.038, 0.294), (0.0, 0.302)]

MOUTH_C = np.array([0.0, -0.118, 0.132])
MOUTH_HALF = (0.072, 0.06, 0.074)
MOUTH_ROT = -22
LIGHT = [(-0.04, 0.09), (0.04, 0.09), (0.0, 0.172)]        # the daylight's triangle, x and z

MASK_C = np.array([0.0, -0.071, 0.249])
MASK_N = unit(0.0, -1.0, 0.5)
MASK_HALF = (0.058, 0.031)
EYE_X, EYE_HALF = 0.027, (0.0195, 0.014)

SPADE_O = np.array([0.02, 0.012, 0.343])
SPADE_TILT = 19


def light_bore(tri, y0, y1, r=0.012):
    """The tunnel through the heap: a rounded triangle swept from y0 to y1 (a hull of small rounded corners)."""
    pts = []
    for x, z in tri:
        for a in np.linspace(0, 2 * math.pi, 10, endpoint=False):
            for y in (y0, y1):
                pts.append((x + r * math.cos(a), y, z + r * math.sin(a)))
    return hull(pts)


def daylight_mask(tri, grow=1.12):
    """1 inside the triangle's outline seen from the front (grown a little): the tunnel's walls."""
    T = np.array(tri, float); m = T.mean(axis=0)
    T = m + (T - m) * grow
    def mask(P):
        q = P[:, [0, 2]]
        out = np.zeros(len(P))
        for a, b in zip(T, np.roll(T, -1, axis=0)):
            e = b - a; nrm = np.array([e[1], -e[0]]); nrm /= np.linalg.norm(nrm)
            if np.dot(m - a, nrm) > 0:
                nrm = -nrm
            out = np.maximum(out, (q - a) @ nrm)
        return inside(out, 0.006)
    return mask


def sandygast(k):
    puddle(k, "puddle", (0, 0.0, 0.03), 0.282, 0.215, 0.042, SAND, "body")
    k.add("heap", mound(HEAP, HEAP_C, SQUASH), SAND, "body")
    # The two stubby knobs at the puddle's ends, a rounded cap on each
    for s in (1, -1):
        side = "R" if s > 0 else "L"
        k.add(f"knob{side}", tube([(0.2 * s, 0.0, 0.052), (0.218 * s, -0.008, 0.07), (0.236 * s, -0.012, 0.092)],
                                  [0.056, 0.04, 0.03], seg=20), SAND, "body")
        k.add(f"knobcap{side}", ell((0.241 * s, -0.012, 0.1), (0.035, 0.031, 0.027), rot=(0, 22 * s, 0)), SAND,
              "body")

    # The mouth: an arch carved into the front, and the tunnel of daylight right through
    k.add("mouth", arch(MOUTH_C, MOUTH_HALF, MOUTH_ROT), HOLLOW, "body", carve=True)
    k.add("bore", light_bore(LIGHT, -0.25, 0.25), HOLLOW, "body", carve=True)
    k.paint(lambda P: opening_mask(MOUTH_C, MOUTH_HALF, MOUTH_ROT)(P) * (P[:, 1] < 0.02), HOLLOW)
    k.paint(lambda P: daylight_mask(LIGHT)(P) * inside(np.abs(P[:, 1]) - 0.2), HOLLOW)

    # The mask over the face: two lobes joined, dipped at the middle above and below
    k.decal(MASK_C, MASK_N, MASK_HALF, MASK, outline=lambda th: 1 - 0.2 * abs(math.sin(th)) ** 3, eps=0.0035,
            rings=16, segs=64)

    pebbles(k, [(-0.205, -0.12, 0.03), (-0.08, -0.19, 0.03), (0.15, -0.16, 0.03)], PEBBLE)

    # The spade: its red blade stuck in the top and its white handle leaning to +X
    d = spade_blade(k, "blade", SPADE_O, SPADE_TILT, 1.0, RED, "body", 0.022)
    a, b = SPADE_O + d * 0.03, SPADE_O + d * 0.124
    k.add("handle", outward(tube([a, (a + b) / 2, b], [0.011, 0.011, 0.0115], seg=18)), HANDLE, "body", fuse=False,
          surface="shell")


def sandygast_eyes(k, state):
    look = eye_look(state, oval_eye(EYE), srgb(MASK), srgb(EYE), line=0.17)
    for s in (1, -1):
        c = MASK_C + np.array([EYE_X * s, 0.0, 0.002])
        lk = look if s > 0 else (lambda f: (lambda u, v: f(-u, v)))(look)
        k.decal(c, unit(0.25 * s, -1.0, 0.5), EYE_HALF, lk, eps=0.0058, rings=20, segs=56)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="fur")
    sandygast(k)
    k.build(voxel=0.0048, smooth=3, band=0.006, wband=0.05)
    k.eye_states(lambda state: sandygast_eyes(k, state))
    k.export(out)
