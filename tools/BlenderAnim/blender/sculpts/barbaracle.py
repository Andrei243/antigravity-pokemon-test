# Barbaracle sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/barbaracle.py -- <the export's Barbaracle.glb> <out.glb>
#
# From the references: Binacle grown into seven hands joined as one. Its body is two flat slabs of rock one over the
# other (the upper the wider), pale grey on top, slate blue down their broken sides, pitted dark, joined by a short
# salmon waist with a frowning line of a mouth on it. On the upper slab its head: a great brown fist, four fingers
# curled over its top and front, a pale claw hanging from each fingertip, a thumb out to the viewer's left with a
# salmon band and a claw; under the fingers an angry face (white eyes, black pupils, brows slanting to the middle) on
# a salmon patch. A grey point of rock juts down from the upper slab's front under the face. Four arms, brown banded
# salmon, each ending in a hand that is a Binacle's cup: brown, three pale claws, one angry eye on its outer side
# (both pictures show an eye on the outer side of the hands turned to them; the feet show none). Two thick legs banded
# salmon, each ending in a cup of a foot whose claws reach forward on the ground.
#
# The art raises the upper arms; carried here forward and a little down (the brief), elbows bent and out, hands in
# front of the body and inside the upper slab's half-width.
#
# Grown from binacle.py (sibling): its barnacle `hand` and its Frame, its angry eye, its rock and pits.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, unit, eye_look, args, outward, hull, ring_band, polygon, \
    sibling

bi = sibling("binacle")
Frame = bi.Frame

BROWN, BROWNDEEP, SALMON, CLAW = bi.BROWN, bi.BROWNDEEP, "#EA8A72", bi.CLAW
ROCKTOP, ROCKSIDE, PIT = "#C4CCDE", "#7E8EBC", "#4A5474"
SPIKE, MOUTH = "#B8BECC", "#4A2A22"
Z = np.array([0, 0, 1.0])

UPPER_C, UPPER_R = np.array([0.0, 0.0, 0.5]), np.array([0.245, 0.155, 0.085])
LOWER_C, LOWER_R = np.array([0.0, 0.0, 0.285]), np.array([0.17, 0.12, 0.075])
HEAD_C, HEAD_R = np.array([0.0, -0.005, 0.67]), np.array([0.108, 0.09, 0.128])
LIMB_R = 0.034


# ---- the body: two slabs of rock and the waist --------------------------------------------------------------------
def body(k):
    up = bi.rock(k, UPPER_C, UPPER_R, seed=11, top=ROCKTOP, side=ROCKSIDE, name="upper", n=40, flat_bottom=0.55)
    low = bi.rock(k, LOWER_C, LOWER_R, seed=3, top=ROCKTOP, side=ROCKSIDE, name="lower", n=34, flat_bottom=0.55)
    for i, (x, y, r) in enumerate(((-0.12, -0.06, 0.016), (0.1, 0.05, 0.013), (0.15, -0.07, 0.011),
                                   (-0.06, 0.08, 0.011))):
        bi.pit(k, up, x, y, r, color=PIT, name=f"pitu{i}")
    for i, (x, y, r) in enumerate(((0.07, -0.05, 0.012), (-0.08, 0.03, 0.011))):
        bi.pit(k, low, x, y, r, color=PIT, name=f"pitl{i}")
    k.add("waist", tube([(0, 0, 0.27), (0, 0, 0.39), (0, 0, 0.52)], [0.056, 0.05, 0.056], seg=20), SALMON, "body")
    # the rock point jutting down under the face
    k.add("spike", hull([(0.032, -0.118, 0.565), (-0.032, -0.118, 0.565), (0.0, -0.1, 0.59), (0.0, -0.152, 0.55),
                         (0.0, -0.13, 0.465), (0.012, -0.11, 0.52), (-0.012, -0.11, 0.52)]), SPIKE, "body",
          fuse=False, surface="shell")


def mouth(k):
    """A frowning line across the waist's front."""
    # the outline holds the patch's middle (a polygon that doesn't answers 9 and floods)
    k.decal((0, -0.054, 0.4), (0, -1, 0), (0.026, 0.008), MOUTH, eps=0.003, rings=12, segs=64,
            outline=polygon([(-1, -1.0), (-0.5, -0.2), (0, 0.25), (0.5, -0.2), (1, -1.0), (0.5, -0.55), (0, -0.25),
                             (-0.5, -0.55)]))


# ---- the head: a fist with a face -----------------------------------------------------------------------------------
FINGERS = (-0.068, -0.023, 0.023, 0.068)


def head(k):
    k.add("head", ell(HEAD_C, HEAD_R), BROWN, "head")
    for i, x in enumerate(FINGERS):
        pts = [(x, 0.045, 0.75), (x * 1.04, -0.02, 0.805), (x * 1.06, -0.085, 0.785), (x * 1.06, -0.112, 0.725)]
        k.add(f"finger{i}", tube(pts, [0.03, 0.031, 0.03, 0.027], seg=18, per=6), BROWN, "head")
        tip = np.array(pts[-1])
        k.add(f"nail{i}", outward(blade([tip + (0, -0.004, 0.006), tip + (0, -0.022, -0.03), tip + (0, -0.024, -0.062)],
                                        [0.015, 0.011, 0.003], (0, 1, 0), 0.6)), CLAW, "head", fuse=False,
              surface="shell")
    # the thumb, out to the viewer's left (-X), banded salmon, its claw up and out
    a, b = np.array([-0.085, -0.03, 0.62]), np.array([-0.158, -0.06, 0.675])
    k.add("thumb", tube([a, (a + b) / 2, b], [0.033, 0.03, 0.026], seg=18, per=6), BROWN, "head")
    k.add("thumbband", ring_band(a + (b - a) * 0.45, b - a, 0.03 - 0.003, 0.025, thick=0.006), SALMON, "head",
          fuse=False)
    d = unit(*(b - a))
    k.add("thumbnail", outward(blade([b - d * 0.01, b + d * 0.03 + Z * 0.02, b + d * 0.055 + Z * 0.05],
                                     [0.016, 0.011, 0.003], (0, 1, 0), 0.6)), CLAW, "head", fuse=False,
          surface="shell")


FACE_N = unit(0, -1, 0.05)
EYE_X, EYE_Z = 0.036, 0.636


def face_marks(k):
    k.decal(HEAD_C + np.array([0, -HEAD_R[1] * 0.95, -0.04]), FACE_N, (0.072, 0.04), SALMON, eps=0.003, rings=14,
            segs=48)
    for s in (1, -1):
        k.decal((EYE_X * s + 0.002 * s, -0.08, EYE_Z + 0.024), unit(0.3 * s, -1, 0.05), (0.026, 0.0065), BROWNDEEP,
                eps=0.0055, rings=8, segs=32, up=unit(-0.42 * s, 0, 1))


def head_eyes(k, state):
    look = eye_look(state, bi.open_eye, srgb(SALMON), srgb(bi.INK), line=0.18)
    for s in (1, -1):
        n = unit(0.3 * s, -1, 0.04)
        # u runs along up x normal, which is toward +X here: on +X it is outward
        o = s
        col = look if o > 0 else (lambda u, v, f=look: f(-u, v))
        shape = bi.EYE_SHAPE if o > 0 else [(-u, v) for u, v in bi.EYE_SHAPE]
        k.decal((EYE_X * s, -0.085, EYE_Z), n, (0.021, 0.018), col, outline=polygon(shape), eps=0.0045, rings=20,
                segs=56)


# ---- the limbs ----------------------------------------------------------------------------------------------------
def segment(k, a, b, bone, name, r=LIMB_R, band=0.5, band_len=0.3):
    a, b = np.array(a, float), np.array(b, float)
    k.add(name, tube([a, (a + b) / 2, b], [r, r * 0.98, r * 0.95], seg=18, per=6), BROWN, bone)
    if band:
        L = np.linalg.norm(b - a)
        k.add(name + "band", ring_band(a + (b - a) * band, b - a, r * 0.97 - 0.003, L * band_len, thick=0.007),
              SALMON, bone, fuse=False)


ARMS = {  # bone: shoulder, elbow, wrist (on +X; mirrored by hand)
    "arm": ((0.19, 0.0, 0.52), (0.262, -0.075, 0.46), (0.2, -0.205, 0.405)),
    "arm2": ((0.08, 0.0, 0.41), (0.228, -0.05, 0.35), (0.19, -0.18, 0.245)),
}
HANDS = []          # each hand's frame, for its eye


def arms(k):
    for bone, (sh, el, wr) in ARMS.items():
        for s, side in ((1, "R"), (-1, "L")):
            m = np.array([s, 1, 1.0])
            a, e, w = np.array(sh) * m, np.array(el) * m, np.array(wr) * m
            segment(k, a, e, bone + side, f"{bone}{side}up")
            segment(k, e, w, bone + side, f"{bone}{side}fore", band=0.45)
            k.add(f"{bone}{side}elbow", ell(e, (LIMB_R * 1.05,) * 3), BROWN, bone + side)
            d = unit(*(w - e))
            fr = Frame(w + d * 0.048, d, (s, 0.15, 0.25), 1.25)
            bi.hand(k, fr, bone + side, f"{bone}{side}hand", flesh=None, rim=False, claw_len=0.9)
            HANDS.append(fr)


def legs(k):
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        hip, knee, ankle = (np.array(p) * m for p in ((0.075, 0.0, 0.25), (0.145, -0.025, 0.155),
                                                      (0.155, -0.045, 0.075)))
        segment(k, hip, knee, "leg" + side, f"thigh{side}", r=0.056, band=0.55, band_len=0.35)
        segment(k, knee, ankle, "leg" + side, f"shin{side}", r=0.05, band=0.5, band_len=0.4)
        k.add(f"knee{side}", ell(knee, (0.057,) * 3), BROWN, "leg" + side)
        fr = Frame(np.array([0.16 * s, -0.09, 0.06]), (0.12 * s, -1.0, -0.12), (0, 0, 1.0), 1.2)
        bi.hand(k, fr, "leg" + side, f"foot{side}", flesh=None, rim=False, claw_len=0.85, spread=1.3)


def barbaracle(src, out):
    k = Kit(src, coat="shell")
    body(k)
    head(k)
    arms(k)
    legs(k)
    mouth(k)
    face_marks(k)
    k.build(voxel=0.0065, smooth=3, band=0.005, wband=0.05)

    def lay(state):
        head_eyes(k, state)
        for fr in HANDS:
            bi.hand_eyes(k, fr, state, xs=(0.0,), z=0.002, half=(0.013, 0.012), mirror=False)
    k.eye_states(lay)
    k.export(out)
    for bone, (sh, el, wr) in ARMS.items():
        print("ARMS:", bone, "hand x", wr[0], "inside the upper slab's half-width", UPPER_R[0], "; hand y", wr[1],
              "before the belly", -UPPER_R[1], "; elbow", el)


if __name__ == "__main__":
    src, out = args()
    barbaracle(src, out)
