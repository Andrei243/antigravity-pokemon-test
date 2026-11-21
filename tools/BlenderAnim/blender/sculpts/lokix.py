# Lokix sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/lokix.py -- <the export's Lokix.glb> <out.glb>
#
# Nymble grown up (nymble.py's on_ell, its folded hind legs as lobes with a white ring, its slate and white). From the
# references: a grasshopper standing like a fighter. A tall black helm of a head, narrowing to its chin, slate panels
# on its lower cheeks either side of a black ridge, two orange eyes like slanted triangles with their inner corners
# dark (a hard scowl), two short black feelers banded white near the tip. A slate neck; a black plate over chest and
# shoulders; under it a slate trunk with a pale four-petalled X on its front; a long black abdomen hanging down between
# the thighs. Thin slate upper arms, great black forearms like clubs with orange spikes along their outer edge and two
# white claws at the end; slate thighs, black shins with orange spikes at the back of the calf and white claws for
# toes. On its back the folded hind legs: tall black lobes behind the shoulders, a white ring on each outer face and
# orange spikes along their back edge.
# The task's text spoke of red eyes and a pale collar: the pictures show orange eyes, a black collar plate and a pale
# X on the chest.
# The art holds the arms out: here they come forward and down, hands at x +-0.15 (the chest plate's half-width 0.11,
# the clubs' tips 0.04 inside it from the front), y -0.19 (in front of the belly), elbows bent.
# Skeleton: body (0, 0, 0.5), arm{s} (+-0.07, 0, 0.58), head (0, -0.01, 0.63), leg{s} (+-0.06, 0.01, 0.4).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, inside, unit, axes, eye_look, polygon, args, sibling

ny = sibling("nymble")
BLACK, SLATE, PALE, ORANGE, INK, WHITE = "#2E3039", "#56677F", "#D8DAE2", "#F29A2A", "#141418", "#F4F4F6"

HC, HR = np.array([0.0, -0.025, 0.73]), np.array([0.086, 0.088, 0.13])      # the helm
VOXEL = 0.0068


def spike(a, d, length, w, t=None, up=(0, 0, 1)):
    """A spike or claw: a kite-section root narrowing to a point length along d (a hull)."""
    a = np.array(a, float); d = unit(*d)
    t = w * 0.7 if t is None else t
    s = np.cross(d, up)
    if np.linalg.norm(s) < 1e-3:
        s = np.cross(d, (1.0, 0, 0))
    s /= np.linalg.norm(s)
    f = np.cross(s, d)
    return hull([a + s * w / 2, a - s * w / 2, a + f * t / 2, a - f * t / 2, a - d * w * 0.4,
                 a + d * length * 0.5 + s * w * 0.25, a + d * length * 0.5 - s * w * 0.25, a + d * length])


def helm(V):
    """The head narrowing toward its chin."""
    V = V.copy()
    s = 1 - 0.32 * np.clip(-V[:, 2], 0, 1) ** 1.4
    V[:, 0] *= s; V[:, 1] *= 1 - 0.18 * np.clip(-V[:, 2], 0, 1)
    return V


EYE = [(-1.0, -0.3), (1.0, 0.95), (0.72, -0.72)]       # a slanted triangle: inner corner low, outer corner up


def lay_eyes(k, state):
    def open_look(u, v):
        c = np.tile(srgb(ORANGE), (len(u), 1))
        c[(u < -0.35) & (v < -0.05 + 0.45 * (u + 0.35))] = srgb(INK)
        return c
    look = eye_look(state, open_look, srgb(BLACK), srgb("#9AA2B4"), line=0.17)
    for s in (1, -1):
        p, n = ny.on_ell(HC, HR, (0.66 * s, -1, 0.14))
        poly = polygon(EYE if s > 0 else [(-a, b) for a, b in EYE][::-1])
        col = look if s > 0 else (lambda u, v, f=look: f(-u, v))
        k.decal(p, n, (0.044, 0.036), col, eps=0.0022, outline=poly, rings=22, segs=72)


def lokix(src, out):
    k = Kit(src, coat="shell")
    # ---- the helm, its slate cheek panels, its feelers
    k.add("head", ell(HC, HR, deform=helm, sub=5), BLACK, "head")
    def cheeks(P):
        q = P - HC
        return (inside(q[:, 2] + 0.005, 0.003) * inside(0.03 - np.abs(q[:, 0]), 0.003) * inside(q[:, 1] - 0.03, 0.004))
    k.paint(cheeks, SLATE, on=["head"])
    k.add("antenna", tube([(0.028, -0.0, 0.845), (0.046, 0.015, 0.89), (0.062, 0.04, 0.93)], [0.016, 0.015, 0.014]),
          BLACK, "head", mirror=True)
    k.paint(lambda P: inside(np.abs(P[:, 2] - 0.9) - 0.008, 0.0015), WHITE, on=["antenna"])
    k.add("neck", tube([(0, 0.0, 0.57), (0, -0.01, 0.64)], [0.034, 0.032]), SLATE, "head")
    # ---- the trunk: a black plate over chest and shoulders, a slate trunk with a pale X, a long black abdomen
    k.add("chest", ell((0, 0.0, 0.565), (0.112, 0.078, 0.068)), BLACK, "body")
    k.add("trunk", ell((0, -0.003, 0.47), (0.088, 0.07, 0.085)), SLATE, "body")
    k.add("abdomen", ell((0, 0.02, 0.33), (0.062, 0.062, 0.13), rot=(-8, 0, 0)), BLACK, "body")
    k.decal((0, -0.072, 0.475), (0, -1, 0), (0.06, 0.06), PALE, eps=0.002,
            outline=lambda th: ny.flower(th, depth=0.78), rings=14, segs=96)
    # ---- the folded hind legs on the back: tall black lobes with a white ring, orange spikes on their back edge
    ny.lobe(k, "lobe", (0.12, 0.085, 0.5), (0.028, 0.062, 0.15), (8, 12, 0), "body", colour=BLACK, ring=WHITE,
            ring_at=(0.0, 0.6), ring_half=0.028)
    for i, z in enumerate((0.44, 0.5, 0.56)):
        k.add(f"lobespike{i}", spike((0.13 + 0.01 * (z - 0.5) / 0.06, 0.14, z), (0.2, 1, -0.35), 0.05, 0.03), ORANGE,
              "body", fuse=False, mirror=True)
    # ---- the arms: thin slate upper arms, great black forearms like clubs, orange spikes, white claws
    sh, el, hd = np.array([0.105, 0.0, 0.565]), np.array([0.175, -0.04, 0.47]), np.array([0.15, -0.19, 0.42])
    k.add("upperarm", tube([sh, (sh + el) / 2, el], [0.026, 0.024, 0.027]), SLATE, "arm{s}", mirror=True)
    k.add("forearm", tube([el, el + (hd - el) * 0.45, hd], [0.04, 0.052, 0.036]), BLACK, "arm{s}", mirror=True)
    d = unit(*(hd - el))
    for i, t in enumerate((0.25, 0.55, 0.8)):
        root = el + (hd - el) * t + np.array([0.035, 0.0, -0.03])
        k.add(f"armspike{i}", spike(root, (0.55, 0.15, -0.8), 0.045, 0.028), ORANGE, "arm{s}", fuse=False,
              mirror=True)
    for i, dx in enumerate((-0.016, 0.016)):
        k.add(f"claw{i}", spike(hd + d * 0.025 + np.array([dx, 0, -0.008]), d * 0.8 + np.array([dx * 8, 0, -0.5]),
                                0.04, 0.02), WHITE, "arm{s}", fuse=False, mirror=True)
    # ---- the legs: slate thighs, black shins, orange calf spikes, white toes
    hip, knee, ank = np.array([0.062, 0.01, 0.4]), np.array([0.095, -0.07, 0.24]), np.array([0.095, 0.02, 0.075])
    k.add("thigh", tube([hip, (hip + knee) / 2, knee], [0.042, 0.035, 0.03]), SLATE, "leg{s}", mirror=True)
    k.add("shin", tube([knee, knee + (ank - knee) * 0.45, ank], [0.036, 0.044, 0.036]), BLACK, "leg{s}", mirror=True)
    k.add("foot", ell((0.095, -0.015, 0.032), (0.034, 0.06, 0.032)), BLACK, "leg{s}", mirror=True)
    for i, t in enumerate((0.35, 0.65)):
        root = knee + (ank - knee) * t + np.array([0.0, 0.035, 0.0])
        k.add(f"calfspike{i}", spike(root, (0.15, 1, 0.35), 0.05, 0.03), ORANGE, "leg{s}", fuse=False, mirror=True)
    for i, dx in enumerate((-0.014, 0.014)):
        k.add(f"toe{i}", spike((0.095 + dx, -0.065, 0.02), (dx * 10, -1, -0.15), 0.035, 0.02), WHITE, "leg{s}",
              fuse=False, mirror=True)
    k.build(voxel=VOXEL, smooth=4, band=0.006, wband=0.045)
    k.eye_states(lambda state: lay_eyes(k, state))
    k.export(out)


if __name__ == "__main__":
    lokix(*args())
