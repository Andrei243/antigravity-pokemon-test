# Aromatisse sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/aromatisse.py -- <the export's Aromatisse.glb> <out.glb>
#
# Grown from Spritzee's script (sibling("spritzee")): the same red upright eyes with their black pupil, glint and rim,
# the same pale mask and beak, the same black lashes. From the references: the design's largest mass a great round
# skirt of pink fluff, wider than tall, its edge torn into soft points, a deeper pink below; over it a purple (the 3D
# model's more magenta) furry body: a large round head with a collar of purple spiky fur over the skirt's top; on the
# crown two great tufts like horns, rising and curling out, each with a yellow streak up its inner front, and little
# spikes of fur between and beside them; a pale mask across the eyes narrowing down round a small white beak pointing
# down; two large red eyes close beside the beak, black lashes flaring out from their outer corners; thin purple arms
# hanging forward over the skirt, tipped with small yellow claws; a purple lobe of a leg down the skirt's front and two
# small purple feet under it.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, inside, unit, lumpy, polygon, args, outward, sibling

sp = sibling("spritzee")

PURPLE = "#A457AE"
SKIRT, SKIRT_LOW = "#F2A2B4", "#D27C94"
MASK = sp.MASK
YELLOW = "#F2C24E"
CLAW = "#F4CC5A"

# --- Aromatisse's frame (joints: body (0, 0, 0.2), head (0, -0.01, 0.29), arms (+-0.045, -0.02, 0.27); the export
#     stands 0.01 to 0.5) --------------------------------------------------------------------------------------------
HC = np.array([0.0, -0.016, 0.358])      # the head's middle
HR = np.array([0.086, 0.074, 0.078])
SC = np.array([0.0, 0.0, 0.158])         # the skirt of fluff
SR = np.array([0.172, 0.142, 0.13])
EYE_D = (0.42, -0.88, 0.14)
EYE_HALF = (0.024, 0.028)
BEAK = [(0.0, -0.082, 0.35), (0.0, -0.094, 0.33), (0.0, -0.094, 0.312)]
BEAK_R = [0.016, 0.011, 0.004]


def head_mask(P):
    """The pale mask on the face's front: a wide lens across the eyes and a point down round the beak."""
    x = np.abs(P[:, 0]); z = P[:, 2]
    lens = (x / 0.07) ** 4 + ((z - 0.368) / 0.032) ** 4 - 1
    point = x - 0.034 * np.clip((z - 0.306) / 0.05, 0, 1.2)
    point = np.maximum(point, 0.306 - z)
    front = P[:, 1] < HC[1] - 0.03
    return inside(np.minimum(lens * 0.02, point), 0.0025) * front


def horn(k, side):
    """A tuft like a horn on the crown: up and out, its tip turned out; a yellow streak up its inner front."""
    s = side
    pts = [(0.04 * s, 0.0, 0.41), (0.062 * s, -0.002, 0.446), (0.082 * s, 0.0, 0.478), (0.104 * s, 0.006, 0.498),
           (0.126 * s, 0.01, 0.494)]
    rad = [0.032, 0.026, 0.019, 0.012, 0.007]
    nm = "R" if s > 0 else "L"
    k.add("horn" + nm, tube(pts, rad), PURPLE, "head")
    # the streak: a loose cord laid along the horn's front, a little inward
    st = [(0.05 * s, -0.022, 0.43), (0.068 * s, -0.019, 0.458), (0.084 * s, -0.012, 0.482), (0.098 * s, -0.006, 0.494)]
    k.add("streak" + nm, outward(tube(st, [0.0075, 0.0075, 0.0065, 0.005])), YELLOW, "head", fuse=False)
    # a small spike of fur beside it, out over the ear
    k.add("side" + nm, tube([(0.07 * s, 0.0, 0.4), (0.1 * s, 0.004, 0.41), (0.118 * s, 0.008, 0.414)],
                            [0.018, 0.011, 0.006]), PURPLE, "head")


def claws(k, hand, side):
    """Two small yellow claws at a hand's tip, pointing down and forward."""
    hand = np.array(hand)
    for j, dx in enumerate((-0.008, 0.008)):
        b = hand + np.array([dx * side, -0.006, -0.004])
        tip = b + np.array([dx * 0.4 * side, -0.01, -0.022])
        pts = [b + np.array([0.006, 0, 0]), b - np.array([0.006, 0, 0]), b + np.array([0, 0.006, 0.002]),
               b + np.array([0, -0.004, 0.004]), tip]
        k.add(f"claw{'R' if side > 0 else 'L'}{j}", hull(pts), CLAW, "arm" + ("R" if side > 0 else "L"), fuse=False)


def lashes(k, side):
    """Black lashes flaring from an eye's outer corner, two points out and up (+u outward on this patch)."""
    p, n = sp.on_ell(HC, HR, (0.78 * side, -0.58, 0.32))
    out = [(-0.9, -0.35), (-0.2, 0.25), (0.55, 0.95), (0.35, 0.25), (1.0, 0.35), (0.35, -0.15), (-0.5, -0.7)]
    if side < 0:
        out = [(-a, b) for a, b in out]
    k.decal(p, n, (0.026, 0.024), sp.BLACK, outline=polygon(out), eps=0.003, rings=16, segs=72)


def aromatisse(src, out):
    k = Kit(src, coat="fur")
    # the skirt of fluff: the largest mass, its rim torn into soft points
    k.add("skirt", ell(SC, SR, deform=lumpy(7, 3, 0.05)), SKIRT, "body")
    for i in range(16):
        a = 2 * math.pi * (i + 0.3 * math.sin(i * 1.7)) / 16
        cx, cy = math.cos(a), math.sin(a)
        z = 0.15 + 0.03 * math.sin(i * 2.3)
        k.add(f"skirtpt{i}", tube([(0.15 * cx, 0.12 * cy, z), (0.178 * cx, 0.146 * cy, z - 0.01),
                                   (0.196 * cx, 0.16 * cy, z - 0.024)], [0.03, 0.018, 0.007]), SKIRT, "body")
    for i in range(8):
        a = 2 * math.pi * (i + 0.5) / 8
        cx, cy = math.cos(a), math.sin(a)
        k.add(f"skirtlow{i}", tube([(0.12 * cx, 0.1 * cy, 0.07), (0.14 * cx, 0.118 * cy, 0.04),
                                    (0.146 * cx, 0.122 * cy, 0.03)], [0.03, 0.016, 0.007]), SKIRT, "body")
    k.paint(lambda P: inside(P[:, 2] - 0.11, 0.02), SKIRT_LOW, on=["skirt"])
    # the purple body: the head, the collar of spikes over the skirt's top
    k.add("neck", ell((0.0, -0.006, 0.29), (0.07, 0.064, 0.05)), PURPLE, "body")
    k.add("head", ell(HC, HR), PURPLE, "head")
    for i in range(14):
        a = 2 * math.pi * i / 14 + 0.2
        cx, cy = math.cos(a), math.sin(a)
        k.add(f"collar{i}", tube([(0.066 * cx, 0.062 * cy - 0.006, 0.302), (0.096 * cx, 0.088 * cy - 0.006, 0.284),
                                  (0.11 * cx, 0.1 * cy - 0.006, 0.274)], [0.022, 0.013, 0.006]), PURPLE, "body")
    horn(k, 1)
    horn(k, -1)
    for i, x in enumerate((-0.018, 0.018)):
        k.add(f"crown{i}", tube([(x, 0.0, 0.42), (x * 1.3, -0.004, 0.45), (x * 1.5, -0.006, 0.462)],
                                [0.016, 0.01, 0.005]), PURPLE, "head")
    # the beak, small and white, pointing down
    k.add("beak", tube(BEAK, BEAK_R), MASK, "head")
    # thin arms hanging forward over the skirt, small yellow claws
    for side in (1, -1):
        nm = "R" if side > 0 else "L"
        arm = [(0.05 * side, -0.03, 0.27), (0.07 * side, -0.09, 0.255), (0.078 * side, -0.13, 0.215),
               (0.072 * side, -0.145, 0.18)]
        k.add("arm" + nm, tube(arm, [0.019, 0.016, 0.014, 0.014]), PURPLE, "arm" + nm)
        claws(k, arm[-1] + np.array([0, -0.004, -0.008]), side)
    # the leg's lobe down the skirt's front and two small feet under it
    k.add("leg", tube([(0.006, -0.128, 0.15), (0.012, -0.142, 0.1), (0.004, -0.13, 0.05), (0.0, -0.11, 0.022)],
                      [0.022, 0.026, 0.024, 0.018]), PURPLE, "body")
    for side in (1, -1):
        k.add("foot" + ("R" if side > 0 else "L"), ell((0.03 * side, -0.05, 0.022), (0.022, 0.03, 0.018)),
              PURPLE, "body")
    # the mask and the beak's grey underside
    k.paint(head_mask, MASK)
    sp.beak_shade(k, BEAK, below=0.003)
    lashes(k, 1)
    lashes(k, -1)
    k.build(voxel=0.0045, smooth=4, band=0.004, wband=0.04)

    def eyes(state):
        sp.lay_eyes(k, HC, HR, EYE_D, EYE_HALF, sp.red_eye(), MASK, state)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    aromatisse(*args())
