# Flapple sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/flapple.py -- <the export's Flapple.glb> <out.glb>
#
# Grown from Applin's script (sibling("applin")): the line's red, cream, lime and green, its on_ell and its pointed
# band (points), the same way of making an eye. From the references: a small green wyvern rising out of the bottom
# half of an apple. The husk is a red cup, cream inside, its rim cut in six sharp points; out of it a thick green
# body curves up into an S of a neck, pale yellow down its front; a round green knob at each shoulder; the head long,
# its snout pointing forward and down; over its top a red hood of apple skin, cream under its edge, running forward to
# a point over the snout; behind the hood a great red shard of skin standing up and back, its top edge jagged; two
# green horns out of the head's top, a pair; on each side of the head a big yellow almond eye with a small dark pupil;
# two wings of apple FLESH, cream and scalloped along their trailing edge, red skin on their back faces; a thin green
# arm from each shoulder lies along the wing's root, its three-fingered hand spread on the wing's face.
# (The task called the wings "the two halves of a red apple skin": the pictures have cream flesh-coloured wings with
# the red only on their backs; and a red hood and a great red crest it did not name.)
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, sheet, slab, srgb, inside, unit, eye_look, args, outward, sibling

a = sibling("applin")
RED, RED_DARK, CREAM, CREAM_DARK = a.RED, a.RED_DARK, a.CREAM, a.CREAM_DARK
GREEN = "#78B04E"
BELLY = "#E4E88E"
YELLOW, YELLOW_RIM = "#F4D23E", "#B88A1E"
INK = a.INK

# --- Flapple's frame (joints: body (0, 0, 0.376), arms (+-0.04, -0.02, 0.386), wings (+-0.025, 0.02, 0.396),
#     head (0, -0.02, 0.436); about 0.62 tall, hovering) -----------------------------------------------------------
CUP_C = np.array([0.0, 0.02, 0.215])     # the husk's sphere's middle (its equator); the bottom 0.145 under it
CUP_R = 0.145
SPINE = [(0.0, 0.03, 0.15), (0.012, 0.04, 0.25), (0.0, 0.02, 0.33), (0.0, -0.025, 0.39), (0.0, -0.045, 0.44)]
SPINE_R = [0.054, 0.05, 0.043, 0.037, 0.035]
SKULL = np.array([0.0, -0.065, 0.446])
SKULL_R = np.array([0.056, 0.064, 0.05])
SNOUT = np.array([0.0, -0.14, 0.416])
SNOUT_R = np.array([0.038, 0.062, 0.03])
SHOULDER = np.array([0.048, -0.03, 0.372])
WING_ROOT = np.array([0.03, 0.025, 0.4])


def husk(k):
    """The red bottom half of an apple, its rim cut in six points, cream inside, with a cream floor under the rim."""
    def wall(radius):
        def at(u, v):
            th = math.pi + 2 * math.pi * u                     # u 0 at the back: the seam behind
            top = 1.42 + 0.42 * a.points(th, 6, 0.0, 1.0, 0.34)
            ph = 0.2 + v * (top - 0.2)
            return np.stack([CUP_C[0] + radius * np.sin(ph) * np.sin(th),
                             CUP_C[1] - radius * np.sin(ph) * np.cos(th),
                             CUP_C[2] - radius * np.cos(ph)], axis=-1)
        return at
    k.add("husk", outward(sheet(wall(CUP_R), 73, 16, 0.012)), RED, "body", fuse=False, surface="shell")
    k.add("huskin", outward(sheet(wall(CUP_R - 0.0115), 73, 16, 0.009)), CREAM, "body", fuse=False, surface="shell")
    k.add("huskfoot", ell(CUP_C - (0, 0, CUP_R * 0.97), (0.038, 0.038, 0.012)), RED, "body", fuse=False, surface="shell")
    k.add("huskfloor", ell(CUP_C - (0, 0, 0.055), (0.12, 0.12, 0.012)), CREAM_DARK, "body", fuse=False, surface="shell")


def wing_frame():
    """The right wing's plane: out along its span, down its chord, and its face (toward the front)."""
    span = unit(0.84, 0.26, 0.48)
    chord = np.array([0.0, 0.42, -1.0]); chord = chord - span * (chord @ span); chord /= np.linalg.norm(chord)
    face = np.cross(span, chord)
    if face[1] > 0:
        face = -face
    return span, chord, face


WING = [(0.0, -0.012), (0.08, -0.04), (0.17, -0.058), (0.25, -0.052), (0.3, -0.026), (0.315, 0.02), (0.295, 0.068),
        (0.262, 0.104), (0.236, 0.092), (0.212, 0.136), (0.172, 0.14), (0.148, 0.17), (0.104, 0.158), (0.074, 0.118),
        (0.036, 0.074), (0.0, 0.034)]


def rounded(outline, rounds=2):
    """An outline with its corners cut round (Chaikin's corner cutting): the flesh's edge is soft, its notches kept."""
    p = np.array(outline, float)
    for _ in range(rounds):
        q = np.roll(p, -1, axis=0)
        p = np.stack([0.75 * p + 0.25 * q, 0.25 * p + 0.75 * q], axis=1).reshape(-1, 2)
    return [tuple(x) for x in p]


WING = rounded(WING)


def wings(k):
    span, chord, face = wing_frame()
    k.add("wing", slab(WING, WING_ROOT, span, chord, 0.02), CREAM, "wing{s}", fuse=False, mirror=True, surface="shell")
    back = [(x * 0.97 + 0.004, y * 0.94) for x, y in WING]
    k.add("wingskin", slab(back, WING_ROOT - face * 0.011, span, chord, 0.008), RED, "wing{s}", fuse=False,
          mirror=True, surface="shell")
    # The arm along the wing's root to a three-fingered hand spread on its face
    hand = WING_ROOT + span * 0.075 + chord * 0.03 + face * 0.0125
    k.add("shoulder", ell(SHOULDER, (0.024, 0.024, 0.024)), GREEN, "arm{s}", mirror=True)
    k.add("arm", tube([SHOULDER, (SHOULDER + hand) / 2 + face * 0.01, hand], [0.013, 0.012, 0.012]), GREEN, "arm{s}",
          mirror=True)
    for j, ang in enumerate((-40, 0, 40)):
        t = math.radians(ang)
        d = span * math.cos(t) + chord * math.sin(t)
        tip = hand + d * 0.036
        k.add(f"finger{j}", tube([hand, tip], [0.0095, 0.0085]), GREEN, "arm{s}", mirror=True)
        k.add(f"fingertip{j}", ell(tip, (0.0115, 0.0115, 0.0115)), GREEN, "arm{s}", mirror=True)


def hood(k):
    """The red hood of apple skin over the head's top, running forward to a point over the snout, cream under it."""
    c = np.array([0.0, -0.082, 0.463])
    r = np.array([0.056, 0.104, 0.05])
    def shell(scale):
        def at(u, v):
            th = math.pi + 2 * math.pi * u                     # seam behind
            fwd = np.clip(np.cos(th), 0, 1)                    # 1 straight ahead
            ph = 0.1 + v * (1.6 + 0.3 * fwd - 0.25 * (1 - fwd))
            y = -np.sin(ph) * np.cos(th)
            y = np.where(y < 0, y * (1 + 0.35 * fwd ** 4 * np.clip(ph - 1.1, 0, 1)), y)
            return np.stack([c[0] + r[0] * scale * np.sin(ph) * np.sin(th) * (1 - 0.45 * fwd ** 6),
                             c[1] + r[1] * scale * y,
                             c[2] + r[2] * scale * np.cos(ph) - 0.012 * fwd ** 3 * np.clip(ph - 1.0, 0, 1)], axis=-1)
        return at
    k.add("hood", outward(sheet(shell(1.0), 49, 16, 0.011)), RED, "head", fuse=False, surface="shell")
    k.add("hoodin", outward(sheet(shell(0.9), 49, 16, 0.007)), CREAM, "head", fuse=False, surface="shell")
    k.add("hoodtop", ell(c + (0, 0, r[2] * 0.99), (0.014, 0.016, 0.0075)), RED, "head", fuse=False, surface="shell")


def crest(k):
    """The great red shard of skin behind the hood: curved across X, standing up and leaning back, its top jagged."""
    base = np.array([0.0, -0.035, 0.475])
    up = unit(0, 0.85, 1.0)
    back = np.cross(np.array([1.0, 0, 0]), up)            # behind, square to up
    def at(u, v):
        al = (u - 0.5) * 2.4
        top = 0.22 * (1 - 0.16 * a.points(u * 2 * math.pi, 2, 0.0, 1.0, 0.22, 0.0)) * (1 - 0.35 * (2 * u - 1) ** 2)
        s = (0.03 + 0.97 * v) * top
        rho = 0.05 + 0.25 * s
        return (base + up * s[..., None] + rho[..., None] * (np.sin(al)[..., None] * np.array([1.0, 0, 0])
                + (1 - np.cos(al))[..., None] * back))
    k.add("crest", outward(sheet(at, 37, 14, 0.018)), RED, "head", fuse=False, surface="shell")


def head(k):
    k.add("skull", ell(SKULL, SKULL_R), GREEN, "head")
    k.add("snout", ell(SNOUT, SNOUT_R, rot=(-16, 0, 0)), GREEN, "head")
    k.add("horn", tube([(0.03, -0.065, 0.48), (0.04, -0.06, 0.53), (0.042, -0.04, 0.575)], [0.015, 0.011, 0.004]),
          GREEN, "head", fuse=False, mirror=True)


EYE_D = (0.66, -0.7, -0.2)
EYE_HALF = (0.024, 0.031)


def eye(u, v):
    c = np.tile(srgb(YELLOW), (len(u), 1))
    c[(u / 0.86) ** 2 + (v / 0.9) ** 2 > 1] = srgb(YELLOW_RIM)
    c[((u - 0.08) / 0.2) ** 2 + ((v + 0.05) / 0.36) ** 2 < 1] = srgb(INK)
    return c


def lay_eyes(k, state):
    p, n = a.on_ell(SKULL, SKULL_R, EYE_D)
    k.decal(p, n, EYE_HALF, eye_look(state, eye, srgb(GREEN), srgb(INK)), up=(0, 0.2, 1), eps=0.002,
            rings=20, segs=56, mirror=True)


def flapple(src, out):
    k = Kit(src, coat="fur")
    husk(k)
    k.add("body", tube(SPINE, SPINE_R), GREEN, "body")
    k.add("neck", tube([SPINE[-2], SPINE[-1], (0, -0.055, 0.455)], [0.036, 0.035, 0.035]), GREEN, "head")
    def belly(P):
        zs = np.array([p[2] for p in SPINE]); ys = np.array([p[1] for p in SPINE])
        cy = np.interp(P[:, 2], zs, ys)
        return inside(P[:, 1] - (cy - 0.016), 0.003) * (P[:, 2] > 0.2) * (P[:, 2] < 0.44)
    k.paint(belly, BELLY, on=["body", "neck"])
    head(k)
    hood(k)
    crest(k)
    wings(k)
    k.build(voxel=0.0045, band=0.006, wband=0.035)
    k.eye_states(lambda state: lay_eyes(k, state))
    k.export(out)


if __name__ == "__main__":
    src, out_path = args()
    flapple(src, out_path)
