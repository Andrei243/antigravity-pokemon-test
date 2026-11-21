# Stunfisk sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/stunfisk.py -- <the export's Stunfisk.glb> <out.glb>
#
# From the references: a flat flounder lying on the ground: a broad round front, the face, and a narrower hump behind
# rising to the tail; salmon-orange over the top, a thick dark brown rim all round its edge and under it; a goofy face
# on the front slope: two round white eyes set apart with small black pupils looking outward, and a brown lump of lips
# between and under them; a yellow "!" on the hump (a broad spot and a dot behind it); a salmon fin out of each side,
# raised a little, its long point out and a second point behind it; a yellow fan of a tail standing up behind the hump,
# facing front and back, with dark ribs. The face sits on the front's forward slope, so the game's low cameras see it.
# The joints: body (0, 0, 0.05), tail (0, 0.12, 0.05).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, slab, srgb, inside, unit, eye_look, args

SALMON, RIM, FIN, LIPS, YELLOW, RIB, WHITE, INK = ("#DE8A68", "#5A463A", "#CF7E62", "#8E6A54", "#F2D64A", "#7A5A2C",
                                                  "#F8F8F4", "#16161C")

FRONT_C, FRONT_R = np.array([0.0, -0.05, 0.036]), np.array([0.172, 0.148, 0.076])
HUMP_C, HUMP_R = np.array([0.0, 0.085, 0.05]), np.array([0.118, 0.112, 0.092])
FLAT = 0.5          # the underside's squash


def flat_bottom(V):
    V = V.copy()
    V[:, 2] = np.where(V[:, 2] < 0, V[:, 2] * FLAT, V[:, 2])
    return V


def on(c, r, d):
    """The point of a flat-bottomed ellipsoid along a direction of its upper unit sphere, and its outward normal."""
    d = np.array(d, float); d /= np.linalg.norm(d)
    n = d / r
    return c + d * r, n / np.linalg.norm(n)


def upper(c, r, level):
    def m(P):
        w = (P - c) / r
        shell = np.linalg.norm(w, axis=1) < 1.2
        return shell * inside(level - w[:, 2], 0.04)
    return m


def body(k):
    k.add("front", ell(FRONT_C, FRONT_R, deform=flat_bottom, sub=5), RIM, "body")
    k.add("hump", ell(HUMP_C, HUMP_R, deform=flat_bottom, sub=5), RIM, "tail")
    k.add("waist", ell((0.0, 0.03, 0.04), (0.14, 0.08, 0.085), deform=flat_bottom, sub=4), RIM, "body")
    # The salmon top over every part, the dark rim left round the edge
    k.paint(lambda P: np.maximum(np.maximum(upper(FRONT_C, FRONT_R, 0.3)(P), upper(HUMP_C, HUMP_R, 0.36)(P)),
                                 upper(np.array([0.0, 0.03, 0.04]), np.array([0.14, 0.08, 0.085]), 0.4)(P)), SALMON)
    # The lips: a brown lump between and under the eyes
    k.add("lips", ell((0.0, -0.176, 0.074), (0.036, 0.026, 0.026)), LIPS, "body")


def exclamation(k):
    for y, half in ((0.03, (0.033, 0.023)), (0.098, (0.015, 0.015))):
        dy = (y - HUMP_C[1]) / HUMP_R[1]
        p, n = on(HUMP_C, HUMP_R, (0.0, dy, math.sqrt(1 - dy * dy)))
        k.decal(p, n, half, YELLOW, up=(0, 1, 0), eps=0.004, rings=12, segs=48)


FIN_OUT = [(0.0, -0.06), (0.07, -0.042), (0.15, 0.0), (0.1, 0.028), (0.112, 0.088), (0.05, 0.066), (0.0, 0.07)]


def fins(k):
    for s in (1, -1):
        o = np.array([0.148 * s, -0.035, 0.034])
        # raised some 22 degrees and its back edge lifted, so its top face turns toward the front
        ex = unit(0.92 * s, 0.1, 0.38)
        ez = unit(0.0, 0.86, 0.5)
        k.add(f"fin{'R' if s > 0 else 'L'}", slab(FIN_OUT, o, ex, ez, 0.024), FIN, "body", fuse=False)


def fan(k):
    """The yellow fan of a tail, standing up behind the hump facing front and back, its ribs thicker dark wedges."""
    o = np.array([0.0, 0.17, 0.1])
    ex, ez = (1.0, 0.0, 0.0), unit(0.0, 0.38, 1.0)
    RX, RZ = 0.1, 0.105
    arc = [(RX * math.cos(t), RZ * math.sin(t)) for t in np.linspace(0, math.pi, 25)]
    k.add("fan", slab(arc[::-1][::-1] + [(-0.06, -0.04), (0.06, -0.04)], o, ex, ez, 0.03), YELLOW, "tail", fuse=False)
    for deg in (45, 90, 135):
        t0, t1 = math.radians(deg - 3.2), math.radians(deg + 3.2)
        rib = [(0.02 * math.cos(t0), 0.02 * math.sin(t0)), (RX * 0.99 * math.cos(t0), RZ * 0.99 * math.sin(t0)),
               (RX * 0.99 * math.cos(t1), RZ * 0.99 * math.sin(t1)), (0.02 * math.cos(t1), 0.02 * math.sin(t1))]
        k.add(f"rib{deg}", slab(rib, o, ex, ez, 0.036), RIB, "tail", fuse=False)


# --- the eyes: round and white with a dark rim, small black pupils looking outward, on the front's forward slope ------
EYE_X, EYE_DY = 0.078, -0.78
EYE_HALF = (0.029, 0.029)
CW, CK, CS = srgb(WHITE), srgb(INK), srgb(SALMON)


def eye_point(s):
    dx = EYE_X / FRONT_R[0]
    dz = math.sqrt(max(1 - dx * dx - EYE_DY * EYE_DY, 0.02))
    return on(FRONT_C, FRONT_R, (dx * s, EYE_DY, dz))


def eye_open(u, v):
    c = np.tile(CW, (len(u), 1))
    c[u * u + v * v > 0.84 ** 2] = CK
    return c


def eyes(k, state):
    for s in (1, -1):
        p, n = eye_point(s)
        look = eye_look(state, eye_open, CS, CK, line=0.15)
        k.decal(p, n, EYE_HALF, look if s > 0 else (lambda u, v, f=look: f(-u, v)), eps=0.004, rings=20, segs=56)
        if state in ("Open", "Fierce"):
            q = p + np.array([0.008 * s, -0.003, -0.002])
            k.decal(q, n, (0.0085, 0.0085), INK, eps=0.0055, rings=8, segs=32)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="scales")
    body(k)
    exclamation(k)
    fins(k)
    fan(k)
    k.build(voxel=0.0045, smooth=4, band=0.006)
    k.eye_states(lambda state: eyes(k, state))
    k.export(out)
