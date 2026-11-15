# Grimer sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/grimer.py -- <the export's Grimer.glb> <out.glb>
#
# From the references: a lavender heap of sludge spreading into a flat puddle with a wavy rim; a soft column rising out
# of it to a round head lump that leans forward over a great open mouth, a tall oval, dark within with a pale grey-blue
# floor and two thick drips hanging from its lower lip; two round white eyes with small black pupils on bumps at the
# head's front-top; two thick arms held forward with stubby fingers, ooze dripping from them; dark purple wavy bands
# across the body, round the arms and over the head (the drawing's; the 3D model has none).
# The skeleton: body (0, 0, 0.2), armR/armL (+-0.12, -0.05, 0.24).
# Muk is grown from this script (sibling("grimer")): the colours, the eye, the drip, the hand, the stripes, the mouth.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, eye_look, args

PURPLE, STRIPE, MOUTH, FLOOR = "#9C86C2", "#634E86", "#2A2034", "#A9B1CB"
WHITE, PUPIL, INK = "#F7F6FA", "#141018", "#221A2C"


# --- the line's makes ---------------------------------------------------------------------------------------------
def puddle(k, name, c, rx, ry, h, bone, n=9, amp=0.09, phase=0.4):
    """The flat puddle the heap spreads into: its rim in soft lobes, its sole flat on the ground."""
    def f(V):
        V = V.copy()
        th = np.arctan2(V[:, 1], V[:, 0])
        s = 1 + amp * np.sin(n * th + phase) * smoothstep(0.3, -0.6, V[:, 2])
        V[:, 0] *= s; V[:, 1] *= s
        V[:, 2] = np.maximum(V[:, 2], -0.7)
        return V
    k.add(name, ell(c, (rx, ry, h), deform=f), PURPLE, bone)


def drip(k, name, top, length, r, bone, colour=PURPLE):
    """A thick drip of ooze hanging from a point: a short neck swelling to a round drop."""
    top = np.array(top, float)
    k.add(name, tube([top, top + (0, -0.002, -length * 0.55), top + (0, -0.004, -length)],
                     [r * 0.8, r * 0.75, r * 1.15], seg=14), colour, bone)


def hand(k, s, H, size, bone, fingers=4, spread=34):
    """A soft hand of ooze at H with stubby fingers fanned forward and out (s the side)."""
    m = np.array([s, 1, 1.0])
    k.add(f"hand{'R' if s > 0 else 'L'}", ell(H * m, (size * 1.0, size * 0.95, size * 0.62)), PURPLE, bone)
    for j in range(fingers):
        a = math.radians(-spread + 2 * spread * j / max(fingers - 1, 1) + 18)
        d = np.array([math.sin(a), -math.cos(a), -0.12])
        d /= np.linalg.norm(d)
        b = H + d * size * 0.6
        k.add(f"finger{'R' if s > 0 else 'L'}{j}", tube([b * m, (b + d * size * 0.55) * m],
                                                         [size * 0.36, size * 0.32], seg=14), PURPLE, bone)


def arm(k, s, path, radii, bone):
    m = np.array([s, 1, 1.0])
    k.add(f"arm{'R' if s > 0 else 'L'}", tube(np.array(path) * m, radii), PURPLE, bone)


def bands(zs, amp, width, n=3, keep=None):
    """A paint mask of wavy dark bands round the heap at heights zs."""
    def mask(P):
        th = np.arctan2(P[:, 1], P[:, 0])
        m = np.zeros(len(P))
        for i, z0 in enumerate(zs):
            zc = z0 + amp * np.sin(n * th + 1.7 * i) + 0.5 * amp * np.sin((n + 2) * th + 0.9 * i)
            m = np.maximum(m, inside(np.abs(P[:, 2] - zc) - width, 0.003))
        if keep is not None:
            m = m * keep(P)
        return m
    return mask


def arm_bands(s, a, b, ts, width, reach):
    """A paint mask of bands round an arm from a to b, at shares ts of its length."""
    a = np.array(a, float) * (s, 1, 1); b = np.array(b, float) * (s, 1, 1)
    d = b - a; L = np.linalg.norm(d); d /= L
    def mask(P):
        t = (P - a) @ d / L
        r = np.linalg.norm((P - a) - np.outer(t * L, d), axis=1)
        m = np.zeros(len(P))
        for i, t0 in enumerate(ts):
            tc = t0 + 0.04 * np.sin(9 * P[:, 2] + 3 * i)
            m = np.maximum(m, inside(np.abs(t - tc) - width / L, 0.003))
        return m * inside(r - reach, 0.004)
    return mask


def eye_open(u, v):
    """Round and white with a dark rim and a small black pupil."""
    c = np.tile(srgb(WHITE), (len(u), 1))
    rr = u * u + v * v
    c[rr > 0.8 ** 2] = srgb(INK)
    c[(u + 0.04) ** 2 + (v + 0.02) ** 2 < 0.27 ** 2] = srgb(PUPIL)
    return c


# --- Grimer -------------------------------------------------------------------------------------------------------
HEAD = np.array([0.0, -0.035, 0.425])
HEADR = np.array([0.12, 0.11, 0.08])
EYE = np.array([0.052, -0.098, 0.468])          # the +X eye bump's middle
EYER = 0.034
MOUTHC = np.array([0.0, -0.13, 0.305])
MOUTHR = (0.07, 0.055, 0.082)
FLOORC = np.array([0.0, -0.112, 0.263])        # a second cutter in the mouth's lower part: its pale floor
FLOORR = (0.052, 0.045, 0.031)
SHOULDER = (0.12, -0.05, 0.24)
ARM = [SHOULDER, (0.175, -0.12, 0.215), (0.215, -0.19, 0.175)]
HAND = np.array([0.228, -0.235, 0.155])


def mouth_box(P):
    """1 away from the mouth's front: keeps bands off the open mouth."""
    q = ((P[:, 0] / 0.1) ** 2 + ((P[:, 2] - MOUTHC[2]) / 0.12) ** 2)
    return 1 - inside(q - 1, 0.1) * (P[:, 1] < -0.06)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="scales")

    # The heap: a puddle on the ground, a broad low mound, a column and the head lump leaning forward
    puddle(k, "puddle", (0, 0.01, 0.028), 0.25, 0.23, 0.04, "body")
    k.add("mound", ell((0, 0.02, 0.105), (0.195, 0.175, 0.1)), PURPLE, "body")
    k.add("torso", ell((0, 0.0, 0.215), (0.158, 0.14, 0.13)), PURPLE, "body")
    k.add("upper", ell((0, -0.005, 0.32), (0.13, 0.118, 0.11)), PURPLE, "body")
    k.add("head", ell(HEAD, HEADR), PURPLE, "body")
    k.add("eyebump", ell(EYE, (EYER, EYER * 0.9, EYER)), PURPLE, "body", mirror=True)
    # Lumps of sludge on the mound and the column, so the heap is no smooth bell
    for i, (x, y, z, r) in enumerate([(0.15, -0.06, 0.12, 0.05), (-0.16, -0.04, 0.11, 0.055), (0.12, 0.12, 0.13, 0.055),
                                      (-0.1, 0.13, 0.15, 0.05), (0.0, 0.15, 0.1, 0.06), (0.13, 0.02, 0.24, 0.045),
                                      (-0.13, 0.03, 0.25, 0.045), (0.0, 0.1, 0.3, 0.05)]):
        k.add(f"lump{i}", ell((x, y, z), (r, r, r * 0.85)), PURPLE, "body")
    # Drips down the sides into the puddle
    for i, (x, y, z) in enumerate([(0.165, -0.05, 0.13), (-0.17, 0.0, 0.14), (0.1, 0.15, 0.15)]):
        drip(k, f"sidedrip{i}", (x, y, z), 0.08, 0.022, "body")

    # The arms: thick, forward and a little down, a hand of four stubby fingers, a drip under each forearm
    for s in (1, -1):
        b = "armR" if s > 0 else "armL"
        arm(k, s, ARM, [0.058, 0.05, 0.046], b)
        hand(k, s, HAND, 0.05, b)
        drip(k, f"armdrip{'R' if s > 0 else 'L'}", np.array([0.19, -0.15, 0.165]) * (s, 1, 1), 0.05, 0.017, b)

    # The mouth: a tall oval carved in the front under the head, its top leaning forward, a pale floor within
    k.add("mouth", ell(MOUTHC, MOUTHR, rot=(-14, 0, 0)), MOUTH, "body", carve=True)
    k.add("floor", ell(FLOORC, FLOORR), FLOOR, "body", carve=True)
    # Two drips hanging from the lower lip
    for x, ln in ((0.03, 0.06), (-0.022, 0.045)):
        drip(k, f"lipdrip{x:+.2f}", (x, -0.142, 0.225), ln, 0.016, "body")

    # The dark bands: round the heap, the head's back and the arms
    body = ["puddle", "mound", "torso", "upper", "head", "lump", "sidedrip"]
    k.paint(bands([0.06, 0.13, 0.2, 0.275, 0.355], 0.012, 0.0075, keep=mouth_box), STRIPE, on=body)
    k.paint(lambda P: inside(np.abs(P[:, 2] - (0.44 + 0.04 * (P[:, 1] + 0.03) / 0.1)) - 0.007, 0.003)
            * (P[:, 1] > -0.05), STRIPE, on=["head"])
    for s in (1, -1):
        k.paint(arm_bands(s, SHOULDER, HAND, (0.35, 0.6, 0.85), 0.008, 0.075), STRIPE,
                on=["arm", "hand"])

    k.build(voxel=0.0055, smooth=3)

    CL, CI = srgb(PURPLE), srgb(INK)

    def eyes(state):
        for s in (1, -1):
            n = unit(0.42 * s, -0.8, 0.42)
            look = eye_look(state, eye_open, CL, CI, line=0.16)
            lk = look if s > 0 else (lambda f: (lambda u, v: f(-u, v)))(look)
            k.decal(EYE * (s, 1, 1) + n * EYER * 0.92, n, (0.031, 0.031), lk, eps=0.004, rings=20, segs=56)

    k.eye_states(eyes)
    k.export(out)
