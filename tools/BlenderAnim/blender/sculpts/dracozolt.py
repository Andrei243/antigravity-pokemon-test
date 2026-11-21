# Dracozolt sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/dracozolt.py -- <the export's Dracozolt.glb> <out.glb>
#
# From the references: the front half of a small yellow bird stuck on the back half of a great dinosaur, the lower
# half by far the largest mass. The dinosaur: a heavy dark green trunk, deep and long, on two great thighs fused into
# it, short thick shins and broad feet of three pink claws and a spur; its long tail rising a little as it runs back
# to a point. A red band runs along the whole top of the back and the tail, sending sharp red teeth down the green
# sides; a row of six pink and rose plates stands up and back along the back. Two red bands across each thigh. On
# the front of the trunk, under the bird, a target: a cream middle in an orange ring in a wide peach ring. The bird:
# a short yellow chest and neck rising from the top of the trunk's front, a small yellow head with a spray of pale
# yellow feathers fanning up and back, a long yellow beak open on an orange mouth, the eyes shut in a happy arc and a
# pink cheek; two small pale yellow wings out and back from its shoulders; two short yellow arms bent forward, three
# yellow claws a hand hanging over the target.
# Written to be grown from: arctozolt.py takes the bird (`bird_head`, `bird_arm`, `zolt_eyes`), dracovish.py the
# dinosaur (`dino_leg`, `paint_top`, `spike`), each placed by its own frame.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, hull, slab, srgb, inside, unit, eye_look, tri, args)

GREEN = "#1F6C58"
RED = "#E0475B"
PINK, ROSE = "#F49BA4", "#DA697C"
CLAW = "#F2909A"
YELLOW = "#F4CF2A"
PALE = "#FBEFA0"            # the crest's feathers
HCLAW = "#E2A628"           # the bird's claws
MOUTH = "#D2683A"
CHEEK = "#EE5A64"
WING = "#FAF0B4"
CREAM, ORANGE, PEACH = "#FCF0B0", "#F2A05A", "#F8D6A4"
INK = "#2A2018"


# --- shared shapes ---------------------------------------------------------------------------------------------------

def spike(k, name, base, d, length, width, color, bone, up=(0, 0, 1), thick=None, surface="shell"):
    """A flat-faced point (a plate, a claw, a feather): a rhombus at its root, wide across up x d, and a tip."""
    base = np.array(base, float)
    d = unit(*d)
    side = np.cross(d, np.array(up, float))
    if np.linalg.norm(side) < 1e-3:
        side = np.cross(d, (1.0, 0, 0))
    side /= np.linalg.norm(side)
    face = np.cross(side, d)
    t = thick if thick is not None else width * 0.45
    mid = base + d * length * 0.45
    pts = [base + side * width / 2, base - side * width / 2, base + face * t / 2, base - face * t / 2,
           mid + side * width * 0.3, mid - side * width * 0.3, mid + face * t * 0.32, mid - face * t * 0.32,
           base + d * length]
    k.add(name, hull(pts), color, bone, fuse=False, surface=surface)


def on_ell(c, r, d):
    """The point of an ellipsoid (centre c, radii r) along a direction d from its centre."""
    d = unit(*d)
    return np.array(c, float) + d / math.sqrt(((d / np.array(r, float)) ** 2).sum())


def dino_leg(k, hip, G=1.0, green=GREEN, bone="leg{s}", stripes=True):
    """
    One of the dinosaur's legs (mirrored): a great thigh fused into the trunk down to just above the ankle, a short
    thick shin, a broad foot of three pink claws and a spur. hip is the thigh's middle on +X; G grows it.
    """
    hip = np.array(hip, float)
    x = hip[0]
    k.add("thigh", ell(hip, np.array([0.1, 0.14, 0.15]) * G, rot=(-10, 0, 0)), green, bone, mirror=True)
    k.add("shin", tube([(x + 0.005, hip[1] + 0.02 * G, 0.15 * G), (x + 0.01, hip[1] - 0.005, 0.055 * G)],
                       [0.068 * G, 0.06 * G]), green, bone, mirror=True)
    foot = np.array([x + 0.01, hip[1] - 0.075 * G, 0.032 * G])
    k.add("foot", ell(foot, np.array([0.068, 0.1, 0.034]) * G), green, bone, mirror=True)
    for s in (1, -1):
        sd = "R" if s > 0 else "L"
        for j, dx in enumerate((-0.04, 0.0, 0.04)):
            spike(k, f"toe{sd}{j}", (s * (foot[0] + dx * G), foot[1] - 0.075 * G, 0.03 * G), (s * dx * 4, -1, -0.32),
                  0.06 * G, 0.04 * G, CLAW, bone.format(s=sd), thick=0.028 * G)
        spike(k, f"spur{sd}", (s * foot[0], foot[1] + 0.085 * G, 0.03 * G), (0, 1, -0.3), 0.045 * G, 0.034 * G, CLAW,
              bone.format(s=sd), thick=0.026 * G)
    if stripes:
        # two red bands slanting across the outer face of the thigh
        def bands(P, hip=hip, G=G):
            q = P - hip * np.array([0, 1, 1])
            ax = np.abs(P[:, 0])
            t = (q[:, 2] + 0.55 * q[:, 1]) / G
            m = inside(np.abs(t - 0.045) - 0.02, 0.004) + inside(np.abs(t + 0.035) - 0.02, 0.004)
            return np.clip(m, 0, 1) * inside(hip[0] - 0.03 * G - ax, 0.01) * inside(q[:, 2] / G - 0.13, 0.01)
        k.paint(bands, RED, on=["thigh"])


def paint_top(k, line, on, period=0.11, low=-0.25, high=0.38):
    """
    The red band along the top of a body, sending sharp teeth down the green sides. line(P) gives each point's height
    across its part (-1 at the bottom, 1 at the top), so the band follows the back however it bends.
    """
    def mask(P):
        h, along = line(P)
        w = (tri(along / period) + 1) / 2                   # 1 at a tooth's point
        v = np.clip((w - 0.55) / 0.45, 0, 1)
        return inside(high - (high - low) * v - h, 0.045)
    k.paint(mask, RED, on=on)


# --- the bird --------------------------------------------------------------------------------------------------------

def bird_head(k, C, G=1.0, cheek=CHEEK, tilt=0.0):
    """
    The bird's head about its middle C, looking along -Y (tilt bends the beak down, in degrees): a yellow skull, a
    long beak open on an orange mouth, a spray of pale feathers fanning up and back. Returns a point maker h(p).
    """
    C = np.array(C, float)
    a = math.radians(-tilt)
    R = np.array([[1, 0, 0], [0, math.cos(a), math.sin(a)], [0, -math.sin(a), math.cos(a)]])

    def h(p):
        return C + (np.array(p, float) * G) @ R.T

    def taper(V):
        V = V.copy()
        f = 1 - 0.8 * np.clip(-V[:, 1], 0, 1) ** 0.8
        V[:, 0] *= f; V[:, 2] *= f
        return V

    SK = np.array([0.072, 0.076, 0.066]) * G
    k.add("skull", ell(h((0, 0, 0)), SK), YELLOW, "head")
    k.add("upbeak", ell(h((0, -0.1, 0.014)), np.array([0.034, 0.078, 0.026]) * G, rot=(-8 + tilt, 0, 0),
                        deform=taper), YELLOW, "head")
    k.add("lowbeak", ell(h((0, -0.08, -0.044)), np.array([0.027, 0.058, 0.016]) * G, rot=(22 + tilt, 0, 0),
                         deform=taper), YELLOW, "head")
    k.add("mouth", ell(h((0, -0.066, -0.012)), np.array([0.03, 0.05, 0.022]) * G, rot=(tilt, 0, 0)), MOUTH, "head",
          surface="skin")
    k.add("crestbase", ell(h((0, 0.026, 0.044)), np.array([0.052, 0.048, 0.034]) * G), YELLOW, "head")
    # The crest: thick pale feathers fanning up and back from the crown
    back = np.array([0, -1.0, 0]) @ R.T          # so each feather's face turns to the front
    crest = [(0.0, -0.01, 0.05, (0, -0.25, 1), 0.105), (0.0, 0.04, 0.045, (0, 0.55, 1), 0.115),
             (0.0, 0.06, 0.01, (0, 1, 0.35), 0.1), (0.035, -0.0, 0.045, (0.45, -0.2, 1), 0.095),
             (0.04, 0.045, 0.035, (0.55, 0.6, 0.8), 0.1), (0.045, 0.05, -0.005, (0.7, 0.75, 0.05), 0.08)]
    for i, (x, y, z, d, ln) in enumerate(crest):
        for s in ((1, -1) if x else (1,)):
            sd = "R" if s > 0 else "L"
            dd = np.array([d[0] * s, d[1], d[2]], float) @ R.T
            spike(k, f"crest{i}{sd}", h((x * s, y, z)), dd, ln * G, 0.042 * G, PALE, "head", up=back,
                  thick=0.026 * G)
    # the cheek, under and behind the eye, in every state
    cn = unit(0.78, -0.5, -0.3) @ R.T
    k.decal(on_ell(h((0, 0, 0)), SK, cn), cn, (0.019 * G, 0.016 * G), cheek, eps=0.003, rings=10, segs=40, mirror=True)
    return h, SK, R


def bird_arm(k, shoulder, elbow, wrist, G=1.0, bone="arm{s}"):
    """A short yellow arm bent forward (mirrored), three yellow claws hanging from the hand."""
    k.add("arm", tube([shoulder, elbow, wrist], [0.024 * G, 0.021 * G, 0.019 * G]), YELLOW, bone, mirror=True)
    w = np.array(wrist, float)
    k.add("hand", ell(w + np.array([0, -0.006, -0.008]) * G, np.array([0.024, 0.022, 0.02]) * G), YELLOW, bone,
          mirror=True)
    for s in (1, -1):
        sd = "R" if s > 0 else "L"
        for j, dx in enumerate((-0.016, 0.0, 0.016)):
            spike(k, f"hclaw{sd}{j}", (s * (w[0] + dx * G), w[1] - 0.012 * G, w[2] - 0.02 * G),
                  (s * dx * 3, -0.45, -1), 0.046 * G, 0.022 * G, HCLAW, bone.format(s=sd), thick=0.018 * G)


def zolt_eyes(k, h, SK, R, G=1.0, kind="happy", lid=YELLOW):
    """
    The bird's eyes, always shut: a happy arc (Dracozolt) or a squeeze (Arctozolt) as the open state, Fierce a
    tilted line written by hand. Returns lay(state).
    """
    L, I = srgb(lid), srgb(INK)
    de = unit(0.62, -0.72, 0.28) @ R.T
    E = on_ell(h((0, 0, 0)), SK, de)

    def arc(u, v):
        c = np.tile(L, (len(u), 1))
        c[(np.abs(u) < 0.86) & (np.abs(v - (-0.3 + 0.55 * (1 - u * u))) < 0.24)] = I
        return c

    def squeeze(u, v):
        c = np.tile(L, (len(u), 1))
        c[(u > -0.85) & (u < 0.86) & (np.abs(np.abs(v) - 0.48 * (u + 0.85) / 1.71) < 0.21)] = I
        return c

    open_look = arc if kind == "happy" else squeeze

    def fierce(u, v):
        c = np.tile(L, (len(u), 1))
        c[(np.abs(u) < 0.86) & (np.abs(v - (0.5 * u + 0.0)) < 0.24)] = I
        return c

    def look(state):
        if state == "Open":
            return open_look
        if state == "Fierce":
            return fierce
        if state == "Smile" and kind == "happy":
            return arc
        return eye_look(state, open_look, L, I, line=0.2)

    def lay(state):
        k.decal(E, de, (0.03 * G, 0.024 * G), look(state), eps=0.0025, rings=20, segs=56, mirror=True)
    return lay


# --- Dracozolt -------------------------------------------------------------------------------------------------------

TRUNK_C, TRUNK_R = np.array([0.0, 0.07, 0.39]), np.array([0.165, 0.25, 0.2])
FRONT_C, FRONT_R = np.array([0.0, -0.1, 0.35]), np.array([0.155, 0.14, 0.175])
TAIL = [(0, 0.22, 0.44), (0, 0.38, 0.49), (0, 0.53, 0.53), (0, 0.665, 0.56)]
TAIL_R = [0.15, 0.105, 0.065, 0.03]
HEAD_C = np.array([0.0, -0.17, 0.635])
HEAD_G = 1.15


def dracozolt(src, out):
    k = Kit(src, coat="scales")

    # --- the dinosaur: trunk, tail, legs -----------------------------------------------------------------------------
    k.add("trunk", ell(TRUNK_C, TRUNK_R, rot=(-6, 0, 0)), GREEN, "body")
    k.add("front", ell(FRONT_C, FRONT_R), GREEN, "body")
    k.add("hips", ell((0, 0.16, 0.4), (0.15, 0.13, 0.17)), GREEN, "body")
    k.add("tail", tube(TAIL, TAIL_R), GREEN, "tail")
    dino_leg(k, (0.13, 0.05, 0.27))

    # the red band along the top of the back and the tail, teeth down the sides
    ty = np.array([p[1] for p in TAIL]); tz = np.array([p[2] for p in TAIL]); tr = np.array(TAIL_R)

    def line(P):
        y = P[:, 1]
        on_tail = y > 0.2
        zc = np.where(on_tail, np.interp(y, ty, tz), TRUNK_C[2] + 0.02 - 0.06 * (y - TRUNK_C[1]))
        rr = np.where(on_tail, np.interp(y, ty, tr), 0.2)
        return (P[:, 2] - zc) / rr, y + 0.02

    paint_top(k, line, on=["trunk", "hips", "tail", "front"], period=0.105, low=-0.15, high=0.75)
    # keep the front of the trunk green, where the bird sits
    k.paint(lambda P: inside(P[:, 1] + 0.14, 0.02) * inside(0.5 - P[:, 2], 0.02), GREEN, on=["front"])

    # the target on the front of the trunk: cream, orange, peach
    tc = np.array([0.0, 0.34])

    def ring(r0, r1):
        def m(P):
            d = np.hypot(P[:, 0] / 1.0, (P[:, 2] - tc[1]) / 1.05)
            return inside(d - r1, 0.004) * inside(r0 - d, 0.004) * inside(P[:, 1] + 0.12, 0.01)
        return m
    k.paint(ring(0.0, 0.135), PEACH, on=["front", "trunk", "thigh"])
    k.paint(ring(0.0, 0.088), ORANGE, on=["front", "trunk"])
    k.paint(ring(0.0, 0.05), CREAM, on=["front", "trunk"])

    # the plates along the back: pink and rose, standing up and back
    plates = [(-0.12, 0.555, 0.13), (-0.03, 0.585, 0.15), (0.06, 0.6, 0.16), (0.15, 0.6, 0.155), (0.24, 0.59, 0.14),
              (0.33, 0.565, 0.12)]
    for i, (y, z, ln) in enumerate(plates):
        spike(k, f"plate{i}", (0, y, z), (0.18, 0.55, 1), ln, 0.085, PINK if i % 2 == 0 else ROSE, "body",
              up=(1, 0, 0), thick=0.032)

    # --- the bird: chest, neck, wings, arms, head --------------------------------------------------------------------
    k.add("chest", ell((0, -0.165, 0.47), (0.078, 0.075, 0.09)), YELLOW, "body")
    k.add("neck", tube([(0, -0.165, 0.5), (0, -0.168, 0.56), (0, -0.165, 0.6)], [0.05, 0.046, 0.045]), YELLOW, "head")
    # the wings: small pale triangles out and back from the shoulders, raised in a V
    WO = [(0.0, -0.035), (0.13, -0.02), (0.17, 0.0), (0.1, 0.025), (0.0, 0.035)]
    k.add("wing", slab(WO, (0.06, -0.14, 0.52), unit(0.5, 0.72, 0.48), unit(0.15, -0.45, 0.85), 0.024), WING, "body",
          fuse=False, mirror=True)
    bird_arm(k, (0.06, -0.19, 0.47), (0.09, -0.245, 0.43), (0.075, -0.275, 0.385))
    h, SK, R = bird_head(k, HEAD_C, G=HEAD_G)
    lay = zolt_eyes(k, h, SK, R, G=HEAD_G)

    k.build(voxel=0.0068, smooth=4, band=0.006, wband=0.06)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    dracozolt(*args())
