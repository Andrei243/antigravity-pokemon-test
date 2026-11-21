# Sawk sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/sawk.py -- <the export's Sawk.glb> <out.glb>
# From the references: Throh's counterpart, taller and leaner but still a solid karateka: a tall blue head like a
# bullet, leaning forward, rising out of a dark collar that stands up behind it and runs down in a V (blue chest in
# the V) to a black belt knotted in front; on the face a dark mask, a brow bar over both eyes whose underside slopes to
# the middle and runs on down the nose as a long wedge; white eyes with dark pupils under it; a pale aqua gi (the
# art's colour, lightened toward the 3D model's near-white), thick sleeves, big blue hands, the model's left open and
# raised before the chest, its right a fist; a wide stance on bent knees, dark knee patches, blue feet.
# Built with throh.py's helpers (frame_ell, hand, belt, knot), everything handed to them as arguments.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from mathutils import Euler
from sculptkit import (Kit, ell, tube, outward, polygon, srgb, inside, smoothstep, unit, boxy, eye_look, args,
                       sibling)

th = sibling("throh")

GI, COLLAR, BELT, BLUE, PATCH = "#BCE4DE", "#4A4A52", "#34343A", "#2F6FCC", "#4E5258"
INK, WHITE = "#14161C", "#F6F6F2"

HC, HR, HROT = np.array([0.0, -0.05, 0.612]), (0.08, 0.082, 0.132), (12, 0, 0)


def head_front(x, z):
    """The point of the tilted head's front at (x, z), found along -Y."""
    R = np.array(Euler([math.radians(a) for a in HROT]).to_matrix())
    best = None
    for y in np.linspace(0.05, -0.2, 2001):
        q = R.T @ (np.array([x, y, z]) - HC)
        if (q[0] / HR[0]) ** 2 + (q[1] / HR[1]) ** 2 + (q[2] / HR[2]) ** 2 <= 1:
            best = y
    return np.array([x, best, z])


def sawk(src, out):
    k = Kit(src, coat="cloth")

    # The gi: a broad chest a little narrower than Throh's block, a short flared hem under the belt
    k.add("trunk", ell((0, 0.012, 0.425), (0.152, 0.118, 0.13), deform=boxy(2.4)), GI, "body", surface="cloth")
    k.add("hem", ell((0, 0.012, 0.265), (0.168, 0.128, 0.06)), GI, "body", surface="cloth")
    # the blue chest showing in the collar's V
    k.paint(lambda P: inside(np.abs(P[:, 0]) - 0.062 * np.clip((P[:, 2] - 0.31) / 0.2, 0, 1), 0.003)
            * (P[:, 1] < -0.06) * (P[:, 2] > 0.31) * (P[:, 2] < 0.56), BLUE)

    # The head: a tall bullet leaning forward, its base in the collar
    def bullet(V):
        V = V.copy()
        t = np.clip((V[:, 2] + 1) / 2, 0, 1)
        V[:, 0] *= 0.82 + 0.3 * np.sin(np.pi * np.clip(t * 1.1, 0, 1))      # fullest a little below the top
        return V
    k.add("head", ell(HC, HR, rot=HROT, deform=bullet), BLUE, "head", surface="skin")
    k.add("neck", ell((0, -0.02, 0.51), (0.06, 0.06, 0.06)), BLUE, "head", surface="skin")

    # The collar: up from the belt, round behind the neck standing high, down again
    half = [(0.012, -0.112, 0.31), (0.035, -0.118, 0.39), (0.062, -0.112, 0.47), (0.08, -0.07, 0.55),
            (0.075, 0.0, 0.59), (0.04, 0.045, 0.6)]
    path = half + [(0.0, 0.056, 0.6)] + [(-x, y, z) for x, y, z in reversed(half)]
    k.add("collar", outward(tube(path, [0.022] * len(path), seg=14, per=6)), COLLAR, "body", fuse=False,
          surface="cloth")

    th.belt(k, 0.31, 0.138, 0.106, 0.012, BELT, height=0.032, thick=0.011)
    th.knot(k, (0.0, -0.118, 0.306), BELT, size=0.85, tails=(0.11, 0.12))

    # A wide stance on bent knees: thick trouser legs fused into the hem, knees out; blue feet
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"thigh{side}", tube(np.array([(0.07, 0.012, 0.26), (0.15, -0.035, 0.15), (0.172, -0.01, 0.06)]) * m,
                                   [0.064, 0.056, 0.048]), GI, f"leg{side}", surface="cloth")
        k.add(f"foot{side}", th.frame_ell((0.18 * s, -0.04, 0.032), (0.047, 0.072, 0.033), (1, 0, 0), (0, 1, 0),
                                          (0, 0, 1)), BLUE, f"leg{side}", fuse=False, surface="skin")
    KNEE = polygon([(-1, -0.8), (-0.8, -1), (0.8, -1), (1, -0.8), (1, 0.8), (0.8, 1), (-0.8, 1), (-1, 0.8)])
    k.decal((0.157, -0.079, 0.118), unit(0.25, -1, 0.1), (0.023, 0.025), PATCH, eps=0.003, outline=KNEE, rings=10,
            segs=40, mirror=True)

    # The sleeves, thick, forward and a little down; big blue hands
    ARMS = (("R", 1, (0.11, 0.0, 0.5), (0.19, -0.04, 0.42), (0.2, -0.13, 0.37)),
            ("L", -1, (0.11, 0.0, 0.5), (0.185, -0.05, 0.43), (0.17, -0.14, 0.48)))
    for side, s, sh, el, wr in ARMS:
        m = np.array([s, 1, 1.0])
        k.add(f"sleeve{side}", tube([np.array(sh) * m, np.array(el) * m, np.array(wr) * m], [0.058, 0.052, 0.048]),
              GI, f"arm{side}", surface="cloth")
    th.hand(k, "handR", "armR", (0.2, -0.165, 0.365), (0, -1, -0.1), (0, -0.1, -1), BLUE, size=0.9, fist=True)
    th.hand(k, "handL", "armL", (-0.17, -0.175, 0.485), (0.1, -0.4, 1), (0.1, -1, -0.2), BLUE, size=0.9)

    # The mask: a bar over both eyes, its underside sloping to the middle, running down the nose as a wedge
    MC = head_front(0.0, 0.642)
    MASK = polygon([(0, -1.45), (0.24, -0.2), (0.8, -0.01), (0.8, 0.38), (-0.8, 0.38), (-0.8, -0.01),
                    (-0.24, -0.2)])
    k.decal(MC, unit(0, -1, 0.15), (0.066, 0.066), INK, eps=0.007, outline=MASK, rings=16, segs=96)

    # The eyes: white with a large dark pupil, their tops cut by the bar
    CB, CW, CK = srgb(BLUE), srgb(WHITE), srgb(INK)
    def open_eye(u, v):
        c = np.tile(CW, (len(u), 1))
        c[((u + 0.12) / 0.48) ** 2 + ((v + 0.15) / 0.58) ** 2 < 1] = CK
        c[((u - 0.04) / 0.17) ** 2 + ((v - 0.05) / 0.17) ** 2 < 1] = (1, 1, 1)
        return c
    EYE_OUT = [(math.cos(a), min(math.sin(a), 0.45 + 0.35 * math.cos(a)))
               for a in np.linspace(0, 2 * math.pi, 40, endpoint=False)]
    E = head_front(0.035, 0.626)
    def lay(state):
        look = eye_look(state, open_eye, CB, CK, line=0.18)
        for s in (1, -1):
            shape = polygon([(u * s, v) for u, v in EYE_OUT])
            col = look if s > 0 else (lambda u, v, f=look: f(-u, v))
            k.decal(E * np.array([s, 1, 1]), unit(0.45 * s, -0.88, 0.15), (0.022, 0.02), col, up=(0, 0.1, 1),
                    eps=0.004, outline=shape, rings=20, segs=56)

    k.build(voxel=0.0055, smooth=4, band=0.006)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    sawk(*args())
