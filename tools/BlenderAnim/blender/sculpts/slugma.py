# Slugma sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/slugma.py -- <the export's Slugma.glb> <out.glb>
# Written to be grown from: magcargo.py imports it (sibling("slugma")) and keeps its frame: the colours, the puddle of
# lava with its craters, the drips, the flames, the way of making eyes.
#
# From the references: a slug of molten lava, red-orange all over. A lumpy puddle on the ground with round craters in
# it, a narrow neck of lava rising out of it to a broad head; two great yellow eyes in a dark rim with a tiny black pupil,
# on bulges at the front corners of the head, nearly touching; the lower front of the head sags into a chin from which
# two thick drips hang, each a thin strand ending in a round drop; two flames of lava rise from the back of the head and
# fan out to both sides, each with two or three ragged points, a little lighter at the tips. No mouth. (The pictures
# overrule the brief's "orange-yellow jaw of drips": the drips are the body's own red-orange.)
# The skeleton: body (0, 0, 0.1), head (0, -0.03, 0.24).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, eye_look, outward, args

LAVA, DEEP, LIGHT = "#F25A36", "#C93C26", "#FF8C55"
YELLOW, RIM, PUPIL = "#F7E77C", "#3A160E", "#1A0C08"
FLAME_GLOW, HEART_GLOW = "#FF6A34", "#FFA060"


# --- the line's makes ---------------------------------------------------------------------------------------------
def puddle(k, name, c, rx, ry, h, bone, colour=LAVA, n=7, amp=0.12, phase=0.6):
    """The lumpy puddle of lava: its rim in soft lobes, its sole flat on the ground."""
    def f(V):
        V = V.copy()
        th = np.arctan2(V[:, 1], V[:, 0])
        s = 1 + amp * np.sin(n * th + phase) * smoothstep(0.4, -0.6, V[:, 2]) \
            + 0.5 * amp * np.sin((n - 3) * th + 2.1) * smoothstep(0.4, -0.6, V[:, 2])
        V[:, 0] *= s; V[:, 1] *= s
        V[:, 2] = np.maximum(V[:, 2], -0.7)
        return V
    k.add(name, ell(c, (rx, ry, h), deform=f), colour, bone)


def drip(k, name, top, length, strand, drop, bone, colour=LAVA):
    """A drip of lava hanging from a point: a thin strand swelling to a round drop at its end."""
    top = np.array(top, float)
    end = top + (0, -0.004, -length)
    k.add(name, tube([top, top + (0, -0.002, -length * 0.5), end + (0, 0, drop * 0.6)],
                     [strand * 1.4, strand, strand * 1.2], seg=14), colour, bone)
    k.add(name + "drop", ell(end, (drop, drop, drop * 1.15),
                             deform=lambda V: V * (1 + 0.25 * np.clip(V[:, 2:3], 0, 1) * -0.6)), colour, bone)


def crater(k, at, normal, r, deep=DEEP, light=LIGHT, base=LAVA, up=(0, 0, 1)):
    """A round crater in the lava: a darker rim, the hollow a shade lighter where the light catches it."""
    D, Lc, B = srgb(deep), srgb(light), srgb(base)
    def look(u, v):
        rho = np.sqrt(u * u + v * v)
        c = np.tile(B, (len(u), 1))
        c[rho < 0.95] = D
        c[(u + 0.12) ** 2 + (v + 0.15) ** 2 < 0.62 ** 2] = Lc
        c[(u + 0.05) ** 2 + (v + 0.05) ** 2 < 0.5 ** 2] = B
        return c
    k.decal(at, normal, (r, r), look, up=up, eps=0.004, rings=8, segs=36)


def frame(d, side):
    d = np.array(d, float); d /= np.linalg.norm(d)
    s = np.array(side, float); s -= d * np.dot(s, d); s /= np.linalg.norm(s)
    return np.stack([s, np.cross(d, s), d], axis=1)


class Flames:
    """Tongues of flame gathered into one loose glowing piece a bone and colour (ponyta.py's tongue)."""

    def __init__(self):
        self.groups = {}
        self.count = 0

    def tongue(self, base, d, length, width, side=(0, 1, 0), flick=0.35, thick=0.45, rag=0.22, waves=1.4, root=0.6):
        self.count += 1
        ph = 2.3 * self.count
        def shape(V):
            V = V.copy()
            t = np.clip((V[:, 2] + 1) / 2, 0, 1)
            w = (1 - t ** 1.1) ** 1.15 * (root + (1.55 - root) * np.sqrt(t))
            s = np.where(V[:, 0] >= 0, 1.0, -1.0)
            edge = 1 + rag * np.sin(2 * math.pi * waves * t + ph + s * 2.1) * smoothstep(0.1, 0.35, t)
            V[:, 0] *= w * edge; V[:, 1] *= w
            V[:, 0] += flick * (2.1 * t ** 2.4 - 0.55 * np.sin(math.pi * t))
            return V
        V, F = ell((0, 0, 0), (width, width * thick, length / 2), deform=shape, sub=4 if length > 0.1 else 3)
        M = frame(d, side)
        return outward((V @ M.T + np.array(base, float) + M[:, 2] * length / 2, F))

    def flame(self, base, d, length, width, bone, side=(0, 1, 0), flick=0.35, colour=LAVA, glow=FLAME_GLOW,
              heart=LIGHT, heart_glow=HEART_GLOW):
        b = np.array(base, float); dd = np.array(d, float) / np.linalg.norm(d)
        self.groups.setdefault((bone, colour, glow), []).append(self.tongue(b, dd, length, width, side, flick))
        if heart:
            ny = frame(dd, side)[:, 1]
            for sg in (1, -1):
                self.groups.setdefault((bone, heart, heart_glow), []).append(
                    self.tongue(b + dd * length * 0.3 + ny * sg * width * 0.3, dd, length * 0.6, width * 0.55, side,
                                flick * 0.6, thick=0.3, rag=0.14, root=0.25))

    def add(self, k, name="flame"):
        for i, ((bone, colour, glow), shapes) in enumerate(self.groups.items()):
            V, F, off = [], [], 0
            for sv, sf in shapes:
                V.append(sv); F.extend([[j + off for j in f] for f in sf]); off += len(sv)
            k.add(f"{name}{i}", (np.vstack(V), F), colour, bone, fuse=False, glow=glow)
        self.groups = {}


def eye_open(u, v):
    """A great yellow eye in a dark rim, a small black pupil a little toward the inner corner."""
    c = np.tile(srgb(YELLOW), (len(u), 1))
    c[u * u + v * v > 0.8 ** 2] = srgb(RIM)
    c[(u + 0.06) ** 2 + (v + 0.02) ** 2 < 0.26 ** 2] = srgb(PUPIL)
    return c


def lay_eyes(k, state, E, n, half, lid=LAVA, look_open=eye_open):
    """Both eyes, mirrored by hand so +u is outward on each; the closed states drawn in the rim's dark on the lava."""
    lk = eye_look(state, look_open, srgb(lid), srgb(RIM), line=0.16)
    for s in (1, -1):
        col = lk if s > 0 else (lambda f: (lambda u, v: f(-u, v)))(lk)
        k.decal(np.array(E) * (s, 1, 1), np.array(n) * (s, 1, 1), half, col, eps=0.005, rings=20, segs=56)


# --- Slugma -------------------------------------------------------------------------------------------------------
H = np.array([0.0, -0.035, 0.37])          # the head's centre
HR = np.array([0.15, 0.12, 0.095])
BULGE = np.array([0.085, -0.105, 0.385])     # the +X eye bulge
BULGER = 0.062
EN = unit(0.34, -0.92, 0.18)                # the +X eye's normal
EHALF = (0.056, 0.05)


def slugma(src, out):
    k = Kit(src, coat="scales")

    # The puddle of lava, a mound on it, lumps round its rim
    puddle(k, "puddle", (0, 0.02, 0.03), 0.235, 0.215, 0.042, "body")
    k.add("mound", ell((0, 0.025, 0.085), (0.17, 0.155, 0.075)), LAVA, "body")
    for i, (x, y, z, r) in enumerate([(0.15, -0.08, 0.06, 0.05), (-0.16, -0.05, 0.06, 0.052), (0.13, 0.13, 0.07, 0.05),
                                      (-0.12, 0.14, 0.07, 0.05), (0.02, 0.18, 0.06, 0.05), (0.04, -0.15, 0.055, 0.045)]):
        k.add(f"lump{i}", ell((x, y, z), (r, r, r * 0.8)), LAVA, "body")

    # The neck of lava: narrow, leaning a little forward, flaring into the head
    k.add("neck", tube([(0, 0.03, 0.13), (0, 0.005, 0.19), (0, -0.02, 0.27), (0, -0.03, 0.32)],
                       [0.118, 0.074, 0.072, 0.095]), LAVA, "head")
    # The broad head, its eye bulges at the front corners, the chin sagging under them
    k.add("head", ell(H, HR), LAVA, "head")
    k.add("back", ell(H + (0, 0.05, 0.02), (0.12, 0.08, 0.08)), LAVA, "head")
    k.add("bulge", ell(BULGE, (BULGER, BULGER * 0.95, BULGER * 0.92)), LAVA, "head", mirror=True)
    k.add("chin", ell((0, -0.1, 0.31), (0.1, 0.06, 0.055)), LAVA, "head")

    # Two drips hanging from the chin: a long one under the +X eye, a fatter one near the middle
    drip(k, "dripA", (0.075, -0.125, 0.3), 0.085, 0.013, 0.026, "head")
    drip(k, "dripB", (-0.015, -0.14, 0.285), 0.065, 0.015, 0.03, "head")

    # Craters in the puddle and the mound
    for (x, y, z), n, r in [((0.12, -0.12, 0.07), unit(0.45, -0.7, 0.55), 0.034),
                            ((-0.085, -0.15, 0.06), unit(-0.3, -0.75, 0.6), 0.026),
                            ((0.0, 0.17, 0.085), unit(0.0, 0.7, 0.7), 0.032)]:
        crater(k, (x, y, z), n, r)

    # Two flames of lava rising from the back of the head and fanning out, each with ragged points
    fl = Flames()
    for s in (1, -1):
        m = np.array([s, 1, 1.0])
        b = np.array([0.065, 0.0, 0.43]) * m
        fl.flame(b, np.array([0.5, 0.4, 1]) * m, 0.16, 0.068, "head", side=(0, 1, 0), flick=0.3)
        fl.flame(b + np.array([0.03, 0.01, -0.01]) * m, np.array([1, 0.35, 0.65]) * m, 0.1, 0.046, "head",
                 side=(0, 0, 1), flick=0.35, heart=None)
        fl.flame(b + np.array([-0.02, 0.02, 0.0]) * m, np.array([0.12, 0.7, 1]) * m, 0.095, 0.044, "head",
                 side=(s, 0, 0), flick=0.3, heart=None)
    fl.add(k)

    k.build(voxel=0.006, smooth=3)

    E = BULGE + EN * BULGER * 0.92
    k.eye_states(lambda st: lay_eyes(k, st, E, EN, EHALF))
    k.export(out)


if __name__ == "__main__":
    slugma(*args())
