# Iron Bundle sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/ironbundle.py -- <the export's Iron Bundle.glb> <out.glb>
# From the references: a robot Delibird, red metal. Head and body are one red egg, wide below; on its upper front a
# white box of a face, a black screen across it with two eyes of blue-lit rings round black pupils turned in, a small
# yellow beak pointing down and forward with a little white square tooth under it; a white bib under the face falling
# in three points down the chest; two fans of white spikes standing up and out from the top of the head; a glowing
# blue button low on the belly (to the model's left); the right flipper hanging at the side, red above and black below
# with blue-lit rings; the left flipper reaching back over a great pale-blue ball of ice flecked white, a black arm
# joining them, a turbine on the ball's outer side (a blue housing round a white fan); short dark legs on flat yellow
# feet. (The task's "red and white metal, a ball for a sack, screen eyes" is what the pictures show.)
# Reference ratios (3D picture, tufts to feet): the face box's height / height 0.3; body width / height 0.48; the
# ball's diameter / height 0.48; the tufts / height 0.2.
# Bones: body (0, 0, 0.17); head (0, 0, 0.29); arms (+-0.1, 0, 0.21); tail (0, 0.1, 0.15); legs (+-0.055, 0, 0.08).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, blade, hull, lathe, ring_band, srgb, inside, unit, boxy, eye_look, polygon,
                       outward, args, sibling)

p = sibling("pidgey")

RED, WHITE, BLACK, YELLOW = "#DC3438", "#F2F3F6", "#26282E", "#F2C83A"
ICE, ICE_D, FAN_BLUE, GLOW = "#6AB8EC", "#3C86C8", "#3A7CC8", "#62C4FF"
SCREEN, RING = srgb("#14161C"), srgb("#4AB4F4")

C = np.array([0.0, 0.0, 0.195])          # the egg
CR = np.array([0.122, 0.115, 0.175])
R = 0.12
FC = np.array([0.0, -0.062, 0.305])      # the face box
FR = np.array([0.098, 0.072, 0.068])


def egg(V):
    V = V.copy()
    t = (1 - V[:, 2]) / 2
    V[:, 0] *= 0.74 + 0.34 * t
    V[:, 1] *= 0.78 + 0.28 * t
    low = V[:, 2] < 0
    V[low, 2] = -(np.clip(-V[low, 2], 0, 1) ** 0.7)
    return V


def round_head(P):
    return np.arctan2(P[:, 0], -P[:, 1]) * R, P[:, 2]


def add_body(k):
    k.add("body", ell(C, CR, deform=egg), RED, "body")
    k.add("face", ell(FC, FR, deform=boxy(3)), WHITE, "head")
    # the bib: white below the face, three points down the chest
    BIB = p.mirrored([(0.0, 0.3), (0.13, 0.3), (0.13, 0.215), (0.098, 0.205), (0.088, 0.16), (0.06, 0.205),
                      (0.03, 0.2), (0.0, 0.145)])
    k.paint(lambda P: p.poly_mask(*round_head(P), BIB) * (P[:, 1] < -0.02), WHITE, on=["body"])
    # the blue button low on the belly, to the model's left (the viewer's left)
    def button(u, v):
        c = np.tile(srgb(GLOW), (len(u), 1))
        c[(u * u + v * v) > 0.8 ** 2] = srgb(ICE_D)
        c[((u + 0.3) / 0.25) ** 2 + ((v - 0.3) / 0.25) ** 2 < 1] = srgb("#DFF4FF")
        return c
    k.decal((-0.055, -0.118, 0.09), unit(-0.45, -1, -0.2), (0.022, 0.026), button, eps=0.003, rings=10, segs=40)


def add_face(k):
    # the yellow beak pointing down and forward, the white tooth under it
    def beak(V):
        t = np.clip(-V[:, 2], 0, 1)
        V = V.copy(); V[:, 0] *= 1 - 0.7 * t; V[:, 1] *= 1 - 0.3 * t; V[:, 2] *= 1 + 0.4 * t
        return V
    k.add("beak", outward(ell((0, -0.14, 0.278), (0.03, 0.03, 0.026), rot=(-20, 0, 0), deform=beak)), YELLOW,
          "head", fuse=False, surface="metal")
    k.add("tooth", hull([(x, y, z) for x in (-0.011, 0.011) for y in (-0.128, -0.138) for z in (0.243, 0.26)]),
          WHITE, "head", fuse=False, surface="metal")
    # the tufts: two fans of white spikes up and out from the top of the head
    for s in (1, -1):
        b = np.array([0.05 * s, -0.02, 0.36])
        k.add("tuftbase", ell(b, (0.036, 0.032, 0.03)), WHITE, "head")
        for tip, w in (((0.07, -0.02, 0.455), 0.05), ((0.13, -0.02, 0.44), 0.05), ((0.165, -0.015, 0.4), 0.045),
                       ((0.02, -0.02, 0.45), 0.04)):
            t = np.array([tip[0] * s, tip[1], tip[2]])
            k.add("tuft", p.spike(b, t, w, 0.03, side=(0, 1, 0)), WHITE, "head", fuse=False, surface="metal")


def add_arms(k):
    # the right flipper hanging at the side: red above, black below with blue-lit rings
    k.add("armR", blade([(0.1, 0.0, 0.26), (0.135, 0.0, 0.2), (0.15, -0.01, 0.15)], [0.045, 0.042, 0.036],
                        (1, 0.1, 0), 0.6), RED, "armR")
    k.add("handR", outward(tube([(0.15, -0.01, 0.155), (0.155, -0.02, 0.1), (0.15, -0.035, 0.07)],
                                [0.033, 0.03, 0.022])), BLACK, "armR", fuse=False, surface="metal")
    for j, z in enumerate((0.13, 0.095)):
        k.add(f"lightR{j}", ring_band((0.153, -0.016 - 0.012 * j, z), unit(0.05, -0.3, -1), 0.03 - 0.003 * j, 0.012,
                                      thick=0.006), GLOW, "armR", fuse=False, glow=GLOW, surface="metal")
    # the left flipper reaching back and out over the ball, a black arm down to the ball's top
    k.add("armL", blade([(-0.1, 0.0, 0.26), (-0.15, 0.04, 0.245), (-0.2, 0.08, 0.235)], [0.045, 0.04, 0.03],
                        (0.3, 0, 1), 0.5), RED, "armL")
    k.add("strapL", outward(tube([(-0.19, 0.075, 0.23), (-0.21, 0.085, 0.215)], [0.028, 0.026])), BLACK, "armL",
          fuse=False, surface="metal")


def add_ball(k):
    BC, BR = np.array([-0.215, 0.085, 0.118]), 0.112
    k.add("ball", ell(BC, (BR, BR, BR), sub=5), ICE, "tail")
    # white flecks over it
    rs = np.random.RandomState(7)
    D = rs.normal(size=(26, 3)); D /= np.linalg.norm(D, axis=1, keepdims=True)
    D = D[D[:, 0] < 0.6]
    def flecks(P):
        d = (P - BC) / np.linalg.norm(P - BC, axis=1, keepdims=True)
        return np.clip((d @ D.T).max(1) - 0.995, 0, 1) * 400 * (np.linalg.norm(P - BC, axis=1) < BR + 0.02)
    k.paint(flecks, "#F4FAFF", on=["ball"])
    # the turbine on its outer side, turned a little forward
    n = unit(-1, -0.3, 0.05)
    c = BC + n * (BR - 0.02)
    k.add("housing", lathe(c, n, [(0, 0.0), (0.065, 0.0), (0.068, 0.02), (0.06, 0.032), (0.048, 0.032),
                                  (0.048, 0.022), (0, 0.022)], seg=48), FAN_BLUE, "tail", fuse=False,
          surface="metal")
    k.add("fan", lathe(c, n, [(0, 0.0), (0.047, 0.0), (0.047, 0.026), (0, 0.03)], seg=40), WHITE, "tail",
          fuse=False, surface="metal")
    a, b = np.cross(n, (0, 0, 1.0)), None
    a /= np.linalg.norm(a); b = np.cross(n, a)
    for i in range(6):
        th = 2 * math.pi * i / 6
        e1 = math.cos(th) * a + math.sin(th) * b
        e2 = math.cos(th + 0.9) * a + math.sin(th + 0.9) * b
        q0 = c + n * 0.031 + e1 * 0.008
        q1 = c + n * 0.031 + e2 * 0.04
        side = np.cross(n, q1 - q0); side /= np.linalg.norm(side)
        k.add(f"vane{i}", hull([q0 + side * 0.004, q0 - side * 0.004, q1 + side * 0.004, q1 - side * 0.004,
                                q0 + side * 0.004 - n * 0.004, q1 - side * 0.004 - n * 0.004]),
              "#9AB2CA", "tail", fuse=False, surface="metal")


def add_legs(k):
    k.add("leg", outward(tube([(0.055, -0.0, 0.06), (0.058, -0.01, 0.025)], [0.032, 0.028])), BLACK, "leg{s}",
          fuse=False, mirror=True, surface="metal")
    # flat yellow feet, the left turned out and forward
    k.add("footR", hull([(0.06 + x, y, z) for x in (-0.035, 0.035) for y in (0.02, -0.075) for z in (0.0, 0.018)]
                        + [(0.06, -0.09, 0.006)]), YELLOW, "legR", fuse=False, surface="metal")
    pts = []
    for x, y in ((-0.035, 0.02), (0.035, 0.02), (-0.035, -0.08), (0.035, -0.08)):
        rx, ry = x * math.cos(0.4) - y * math.sin(0.4), x * math.sin(0.4) + y * math.cos(0.4)
        for z in (0.0, 0.018):
            pts.append((-0.062 + rx, ry, z))
    k.add("footL", hull(pts), YELLOW, "legL", fuse=False, surface="metal")


# --- the eyes: a black screen across the face, blue-lit rings round black pupils turned in --------------------------
def add_screen(k):
    k.decal(FC + np.array([0, -FR[1] * 0.96, 0.012]), (0, -1, 0.0), (0.088, 0.034), SCREEN, eps=0.003,
            outline=polygon([(-1, 0.75), (-0.92, 1), (0.92, 1), (1, 0.75), (1, -0.6), (0.85, -1), (-0.85, -1),
                             (-1, -0.6)]), rings=12, segs=72)


def eye_open(u, v):
    c = np.tile(SCREEN, (len(u), 1))
    r = u * u + v * v
    c[r < 0.92 ** 2] = RING
    c[r < 0.62 ** 2] = SCREEN
    c[((u + 0.18) / 0.36) ** 2 + (v / 0.4) ** 2 < 1] = srgb("#0A0A10")
    c[((u + 0.36) / 0.12) ** 2 + ((v - 0.2) / 0.12) ** 2 < 1] = RING
    return c


def eyes(k, state):
    look = eye_look(state, eye_open, SCREEN, RING, line=0.18)
    for s in (1, -1):
        col = look if s == 1 else (lambda a, b, g=look: g(-a, b))
        k.decal((0.043 * s, -0.135, 0.318), unit(0.25 * s, -1, 0.0), (0.03, 0.03), col, eps=0.0045, rings=20, segs=56)


def iron_bundle(src, out):
    k = Kit(src, coat="metal")
    add_body(k)
    add_face(k)
    add_arms(k)
    add_ball(k)
    add_legs(k)
    add_screen(k)
    k.build(voxel=0.0045, smooth=4, band=0.004, wband=0.04)
    k.eye_states(lambda s: eyes(k, s))
    k.export(out)


if __name__ == "__main__":
    iron_bundle(*args())
