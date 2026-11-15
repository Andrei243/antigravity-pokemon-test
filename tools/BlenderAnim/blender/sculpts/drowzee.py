# Drowzee sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/drowzee.py -- <the export's Drowzee.glb> <out.glb>
# Written to be grown from: hypno.py imports its frame (sibling("drowzee")): the colours, the tired eyes, the hands
# and feet, the ray onto a part and the crisp paints.
#
# The design, from the references: a stout tapir of a biped, pear-shaped, yellow above and dark brown below, the two
# meeting in a wavy line round the belly a little under its middle (rounded yellow lobes hanging into the brown); a big
# round head sunk on the shoulders with no neck, a short thick trunk of a snout standing out and drooping at its end,
# two wrinkles over its root; small half-shut eyes on either side of the trunk's root; a wide open mouth under the
# trunk, dark red with a pink tongue; small round brown ears behind the crown's sides; short thick arms with big
# three-fingered hands and a thumb; short thick brown legs, yellow feet.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, args

YELLOW, BROWN, EAR = "#F3CB3A", "#6A4532", "#7A5A48"
INKH, WHITE, PUPILH = "#2A1E16", "#F8F5EC", "#1C1612"
MOUTHH, TONGUE, WRINKLE = "#7C2634", "#E88C9A", "#A07A26"


def hit(shape, o, d):
    """Where a ray from o along d first meets a part's own mesh: places on a part before the body is fused."""
    V, F = shape
    h = BVHTree.FromPolygons(V.tolist(), F).ray_cast(Vector(o), Vector(unit(*d)))
    return np.array(h[0])


def round_angle(P, c):
    """The angle round the upright axis through c, nought at the front (-Y)."""
    return np.arctan2(P[:, 0] - c[0], -(P[:, 1] - c[1]))


def hand(k, c, d, side, r, bone, color, fingers=3, name="hand"):
    """A big hand at c: a round palm, short thick fingers fanned along d, a thumb up toward side x up. Fused."""
    c, d, side = np.array(c, float), unit(*d), unit(*side)
    up = np.cross(side, d)
    up = up if up[2] > 0 else -up
    k.add(name + "palm", ell(c, (r, r * 0.9, r)), color, bone)
    for i in range(fingers):
        o = (i - (fingers - 1) / 2) * r * 0.62
        a = c + d * r * 0.55 + up * o
        b = c + d * r * 1.45 + up * o * 1.25 - side * r * 0.12
        k.add(f"{name}finger{i}", tube([a, b], [r * 0.34, r * 0.26]), color, bone)
    a = c - side * r * 0.5 + up * r * 0.3
    k.add(name + "thumb", tube([a, a + up * r * 0.75 + d * r * 0.4], [r * 0.33, r * 0.25]), color, bone)


def foot(k, c, r, bone, color, toes=0, name="foot"):
    """A rounded foot at c, its length along -Y; toes: that many short toes over its front, fused."""
    c = np.array(c, float)
    k.add(name, ell(c, r), color, bone)
    for i in range(toes):
        o = (i - (toes - 1) / 2) * r[0] * 0.62
        a = c + np.array([o, -r[1] * 0.6, -r[2] * 0.15])
        k.add(f"{name}toe{i}", tube([a, a + np.array([o * 0.25, -r[1] * 0.38, -r[2] * 0.15])],
                                    [r[0] * 0.34, r[0] * 0.3]), color, bone)


def tired_eye(lid, ink=srgb(INKH), white=srgb(WHITE), pupil=srgb(PUPILH), lidline=0.1, slant=0.0):
    """
    A small half-shut eye (u outward, v up): the lid's colour over its top, a heavy dark line along the lid's edge, the
    white below with a dark pupil hanging from the lid toward the inner corner, a thin dark line round the lower edge.
    slant tips the lid line down toward the nose (a scowl) or up.
    """
    def look(u, v):
        c = np.tile(white, (len(u), 1))
        top = lidline + slant * u
        c[u * u + v * v > 0.7] = ink                             # the lower rim, a sixth of the eye
        c[((u + 0.18) / 0.3) ** 2 + ((v - top + 0.12) / 0.42) ** 2 < 1] = pupil
        c[np.abs(v - top) < 0.17] = ink
        c[v > top + 0.17] = lid
        return c
    return look


def tired_eyes(k, E, de, half, lid, slant=0.0, up=(0, 0, 1)):
    """The lay function for eye_states: eye_look's states from the tired eye, laid mirrored."""
    def lay(state):
        if state == "Fierce":
            look = tired_eye(lid, slant=slant - 0.45, lidline=0.05)
        else:
            look = eye_look(state, tired_eye(lid, slant=slant), lid, srgb(INKH), line=0.18)
        k.decal(E, de, half, look, eps=0.005, rings=20, segs=56, mirror=True, up=up)
    return lay


def drowzee(src, out):
    k = Kit(src, coat="fur")
    H = np.array([0, -0.045, 0.665])             # the head's middle
    HR = np.array([0.14, 0.125, 0.12])
    B = np.array([0, 0.0, 0.36])                 # the belly's middle

    # The pear of a body: a broad belly, a narrower chest under the head, hips over the legs
    def pear(V):
        V = V.copy()
        s = 1 + 0.1 * np.clip(-V[:, 2], 0, 1) - 0.06 * np.clip(V[:, 2], 0, 1)
        V[:, 0] *= s; V[:, 1] *= s
        return V
    k.add("belly", ell(B, (0.205, 0.175, 0.2), deform=pear), YELLOW, "body")
    k.add("chest", ell((0, -0.005, 0.51), (0.155, 0.13, 0.11)), YELLOW, "body")
    k.add("hips", ell((0, 0.01, 0.25), (0.17, 0.14, 0.1)), BROWN, "body")

    # The head, sunk onto the shoulders
    HEAD = ell(H, HR)
    k.add("head", HEAD, YELLOW, "head")
    k.add("jowl", ell((0, -0.05, 0.6), (0.12, 0.1, 0.07)), YELLOW, "head")
    # The trunk: out from the face's middle, drooping at its rounded end
    k.add("trunk", tube([(0, -0.12, 0.69), (0, -0.19, 0.705), (0, -0.24, 0.692), (0, -0.258, 0.664)],
                        [0.058, 0.05, 0.044, 0.04]), YELLOW, "head")
    k.add("trunktip", ell((0, -0.255, 0.66), (0.046, 0.04, 0.036)), YELLOW, "head")
    # two wrinkles over its root
    for y0 in (-0.158, -0.196):
        k.paint(lambda P, y0=y0: inside(np.abs(P[:, 1] - y0 + 0.02 * (P[:, 0] / 0.05) ** 2) - 0.0038, 0.0022)
                * (P[:, 2] > 0.715) * (np.abs(P[:, 0]) < 0.042), WRINKLE)

    # Small round ears behind the crown's sides, brown, loose
    k.add("ear", ell((0.105, 0.06, 0.745), (0.044, 0.022, 0.042), rot=(0, 0, -45)), EAR, "head", fuse=False,
          mirror=True)

    # Short thick arms carried forward and a little down, big hands
    k.add("arm", tube([(0.13, -0.01, 0.53), (0.21, -0.06, 0.49), (0.255, -0.12, 0.455)], [0.065, 0.055, 0.048]),
          YELLOW, "arm{s}", mirror=True)
    for s, bone in ((1, "armR"), (-1, "armL")):
        hand(k, (0.27 * s, -0.155, 0.45), (0.15 * s, -0.9, -0.2), (s, 0.1, 0), 0.052, bone, YELLOW,
             name="hand" + bone[-1])

    # Short thick legs, yellow feet
    k.add("leg", tube([(0.095, 0.0, 0.27), (0.112, -0.01, 0.16), (0.118, -0.02, 0.075)], [0.1, 0.085, 0.064]),
          BROWN, "leg{s}", mirror=True)
    for s, bone in ((1, "legR"), (-1, "legL")):
        foot(k, (0.12 * s, -0.055, 0.036), (0.062, 0.088, 0.038), bone, YELLOW, name="foot" + bone[-1])

    # Yellow above and brown below, meeting in a wavy line round the belly: rounded yellow lobes hanging into the
    # brown; and the feet yellow again under a crisp line at the ankle
    def brown(P):
        a = round_angle(P, B)
        edge = 0.372 - 0.05 * np.abs(np.sin(3.5 * a)) ** 0.6
        return (inside(P[:, 2] - edge, 0.0045) * inside(0.082 - P[:, 2], 0.0025)
                * (np.hypot(P[:, 0], P[:, 1] - B[1]) < 0.235))
    k.paint(brown, BROWN)
    k.paint(lambda P: (1 - brown(P)) * inside(P[:, 2] - 0.42, 0.0025) * inside(0.082 - P[:, 2], 0.0025)
            * (np.hypot(P[:, 0], P[:, 1] - B[1]) < 0.235), YELLOW)
    k.paint(lambda P: inside(P[:, 2] - 0.082, 0.0025), YELLOW)

    # The wide open mouth under the trunk: a dark red opening with a pink tongue in its lower half
    CM, CT, CY = srgb(MOUTHH), srgb(TONGUE), srgb(YELLOW)
    def mouth(u, v):
        c = np.tile(CM, (len(u), 1))
        c[((u / 0.7) ** 2 + ((v + 0.55) / 0.55) ** 2) < 1] = CT
        return c
    dm = unit(0, -0.86, -0.5)
    MC = np.array([0, -0.15, 0.588])
    def lips(th):
        return 1.0 if math.sin(th) < 0 else 0.55 + 0.45 * abs(math.cos(th))  # flat on top under the trunk
    k.decal(MC, dm, (0.055, 0.038), mouth, eps=0.004, rings=12, segs=56, outline=lips, up=(0, -0.5, 0.86))

    # Small half-shut eyes either side of the trunk's root
    de = unit(0.6, -0.76, 0.24)
    E = hit(HEAD, H + np.array([0.072, 0, 0.055]) + de * 0.4, -de)
    lay = tired_eyes(k, E, de, (0.03, 0.021), srgb(YELLOW))

    k.build(voxel=0.0075)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    drowzee(*args())
