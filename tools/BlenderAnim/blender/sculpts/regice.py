# Regice sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/regice.py -- <the export's Regice.glb> <out.glb>
#
# The design, from the references: a golem of clear pale blue ice, every part a crystal with flat faces. Its body is
# its head: a great eight-sided prism, its sides straight, its foot cut back and its top a faceted point. High on its
# front the face, seven yellow dots in a plus with a long cross-bar (five across, one above and one below the middle).
# From behind each shoulder a long thin crystal rises out and up; below it the arm is a big six-sided crystal block
# hanging beside the body (carried a little forward here), three small pointed crystals for fingers at its foot. It
# stands on two flat six-sided plates, each with a crystal spike under it pointing to the ground.
# Built as Regirock is: the crystals are loose hulls, and the dots' patches lie on a thin fused plate just proud of
# the body's flat front (a hard-edged cell with a face, Combee's way). The eyes' five states are laid on the dots:
# open they shine, shut they dim with a dark line across (regigigas.py's gems).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, srgb, unit, boxy, eye_look, args, hull, axes

# --- colours ----------------------------------------------------------------------------------------------------------
ICE, ICEDEEP, ICEPALE = "#94D2F0", "#6CB8E6", "#B6E2F6"
DOT, DOTRIM = "#F6DA4E", "#B8961E"

# --- the frame --------------------------------------------------------------------------------------------------------
BODY_A, BODY_B, BODY_CUT = 0.215, 0.165, 0.065       # the body's half width, half depth and the cut of its corners
BODY_Y = 0.01                                        # its middle, a little back of the bones
FRONT = BODY_Y - BODY_B                              # its flat front
FACE_Y = FRONT - 0.0015                              # the face plate's front, where the dots are cast
FACE_Z = 0.7
DOT_STEP, DOT_HALF = 0.047, 0.021
PLUS = [(-2, 0), (-1, 0), (0, 0), (1, 0), (2, 0), (0, 1), (0, -1)]


def octagon(a, b, c, z, y0=0.0):
    return [(sx * a, y0 + sy * (b - c), z) for sx in (-1, 1) for sy in (-1, 1)] + \
           [(sx * (a - c), y0 + sy * b, z) for sx in (-1, 1) for sy in (-1, 1)]


def crystal(p0, p1, prof, n=6, turn=0.0):
    """
    A crystal along p0 -> p1: rings of n sides at shares t of the way, (t, radius) each (a radius of nought a point),
    made a hull, so its faces are flat.
    """
    p0, p1 = np.array(p0, float), np.array(p1, float)
    d = p1 - p0
    a, b = axes(d)
    pts = []
    for t, r in prof:
        c = p0 + d * t
        if r <= 0:
            pts.append(c)
            continue
        for j in range(n):
            th = 2 * math.pi * j / n + math.radians(turn)
            pts.append(c + r * (math.cos(th) * a + math.sin(th) * b))
    return hull(np.array(pts))


def body(k):
    pts = (octagon(BODY_A - 0.05, BODY_B - 0.04, BODY_CUT - 0.02, 0.25, BODY_Y)
           + octagon(BODY_A, BODY_B, BODY_CUT, 0.33, BODY_Y)
           + octagon(BODY_A, BODY_B, BODY_CUT, 0.8, BODY_Y)
           + octagon(BODY_A - 0.06, BODY_B - 0.045, BODY_CUT - 0.02, 0.9, BODY_Y)
           + [(0, BODY_Y, 1.0)])
    k.add("body", hull(pts), ICE, "body", fuse=False)
    # the thin plate the face's patches lie on, just proud of the flat front
    k.add("face", ell((0, FACE_Y + 0.0125, FACE_Z), (0.14, 0.012, 0.085), deform=boxy(3.0)), ICE, "body")


def arms(k):
    for s, side in ((1, "R"), (-1, "L")):
        bone = f"arm{side}"
        # the long thin crystal rising out and up from behind the shoulder
        k.add(f"spar{side}", crystal((0.15 * s, 0.07, 0.7), (0.37 * s, 0.11, 0.93),
                                     [(0, 0.04), (0.8, 0.045), (1.0, 0)], turn=30),
              ICEPALE, bone, fuse=False)
        # the forearm: a big six-sided block hanging beside the body, its foot out a little and forward
        top, foot = np.array([0.265 * s, -0.03, 0.67]), np.array([0.345 * s, -0.08, 0.38])
        k.add(f"forearm{side}", crystal(top, foot, [(-0.1, 0.035), (0.15, 0.1), (0.85, 0.1), (1.07, 0.035)], turn=30),
              ICE, bone, fuse=False)
        # three fingers: small pointed crystals fanning down from its foot
        d = unit(*(foot - top))
        for j, (dx, dy) in enumerate([(0.05, -0.05), (0.065, 0.0), (0.05, 0.05)]):
            root = foot + np.array([(dx - 0.04) * s, dy - 0.01, 0.01])
            tip = root + unit(dx * 1.6 * s, dy * 1.2 - 0.02, -1) * 0.105
            k.add(f"finger{j}{side}", crystal(root, tip, [(0, 0.03), (0.55, 0.03), (1.0, 0)]), ICEDEEP, bone,
                  fuse=False)


def legs(k):
    for s, side in ((1, "R"), (-1, "L")):
        bone = f"leg{side}"
        k.add(f"plate{side}", crystal((0.12 * s, 0.0, 0.205), (0.12 * s, 0.0, 0.26),
                                      [(0, 0.11), (0.25, 0.125), (1, 0.125)], turn=0),
              ICEPALE, bone, fuse=False)
        k.add(f"spike{side}", crystal((0.12 * s, 0.0, 0.22), (0.13 * s, -0.01, 0.0),
                                      [(0, 0.07), (0.2, 0.075), (1, 0)], turn=30),
              ICEDEEP, bone, fuse=False)


# --- the dots, which are the eyes -------------------------------------------------------------------------------------
def dot_look():
    c0, rim, hi = srgb(DOT), srgb(DOTRIM), np.array([1.0, 1.0, 0.85])
    def look(u, v):
        c = np.tile(c0, (len(u), 1))
        c[u * u + v * v > 0.8 ** 2] = rim
        c[(u + 0.25) ** 2 + (v - 0.3) ** 2 < 0.24 ** 2] = hi
        return c
    return look


def dots(k, state):
    c0 = srgb(DOT)
    look = eye_look(state, dot_look(), c0 * 0.45, c0 * 0.12, line=0.2)
    for col, row in PLUS:
        k.decal((col * DOT_STEP, FACE_Y, FACE_Z + row * DOT_STEP), (0, -1.0, 0), (DOT_HALF, DOT_HALF), look,
                eps=0.003, rings=20, segs=56)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="shell")
    body(k)
    arms(k)
    legs(k)
    k.build(voxel=0.006, band=0.004)
    k.eye_states(lambda state: dots(k, state))
    k.export(out)
