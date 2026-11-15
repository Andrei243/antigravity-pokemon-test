# Politoed sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown from
# Poliwag's script (poliwag.py): the line's spiral laid as one polar patch, its eyes on swellings, its round eye.
#   blender --background --factory-startup --python sculpts/politoed.py -- <the export's Politoed.glb> <out.glb>
#
# The design, from the references: a green frog standing upright, head and body one mass with no neck, the head
# nearly as wide as the body; a wide mouth line across the face, everything under it (the jaw and the throat) yellow;
# round eyes on swellings at the head's upper corners, white in a dark rim with a big red-brown iris, a black pupil
# and a glint; a pink blush on each cheek under the eye; one long BLUE hair rising from the back of the crown and
# curling over into a loop (blue in both pictures, not green); a yellow oval on the belly with a green spiral of some
# one and a half turns, its outer end running into the green at the left; green arms ending in yellow hands of three
# round fingers; thick crouching frog legs, long green feet with three round yellow toes.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, outward, sibling, args

pw = sibling("poliwag")

GREEN, YELLOW, CURL, BLUSH = "#6CC24E", "#F2E26A", "#5E8FC8", "#F29AB2"
INK, IRIS, SCLERA = "#1A1416", "#8E2228", "#FFFFFF"

# --- the frame ------------------------------------------------------------------------------------------------------
B = np.array([0.0, 0.0, 0.30])            # the body's middle (the body bone is at 0, 0, 0.3)
BR = np.array([0.168, 0.148, 0.2])
HC = np.array([0.0, -0.03, 0.53])         # the head's middle (the head bone is at 0, -0.02, 0.42)
HR = np.array([0.152, 0.136, 0.118])
JAW_C, JAW_R = np.array([0.0, -0.07, 0.47]), np.array([0.138, 0.1, 0.078])
BUMP_C, BUMP_R = np.array([0.086, -0.07, 0.6]), np.array([0.062, 0.056, 0.056])
EYE_DIR = unit(0.42, -0.86, 0.3)
EYE_HALF = (0.044, 0.048)
CHEEK_DIR = unit(0.8, -0.56, -0.18)
BELLY_DIR = unit(0, -1, -0.28)
BELLY_HALF = (0.106, 0.122)
SPIRAL = dict(turns=1.5, w=0.24, spin=1, start=0.06, end=1.02)
SHOULDER, ELBOW, WRIST = np.array([0.12, -0.02, 0.405]), np.array([0.2, -0.075, 0.335]), np.array([0.215, -0.14, 0.285])
ARM_R = (0.054, 0.047, 0.042)
PALM, PALM_R = np.array([0.218, -0.168, 0.272]), np.array([0.044, 0.038, 0.042])
HIP, KNEE, ANKLE = np.array([0.1, -0.005, 0.19]), np.array([0.18, -0.06, 0.12]), np.array([0.165, -0.05, 0.04])
FOOT_C, FOOT_R, FOOT_TURN = np.array([0.172, -0.095, 0.022]), np.array([0.058, 0.08, 0.022]), 14
VOXEL = 0.006


def mouth_line(x):
    """The mouth's line across the face: the jaw's yellow below it, rising a little toward the cheeks."""
    return 0.502 + 0.9 * x * x


def throat_line(x):
    return 0.405 + 4.5 * x * x


def egg(V, below=0.07, above=0.05):
    V = V.copy()
    s = 1 + below * np.clip(-V[:, 2], 0, 1) - above * np.clip(V[:, 2], 0, 1) ** 2
    V[:, 0] *= s; V[:, 1] *= s
    return V


def politoed_eye(rim=0.15, iris=0.68, pupil=0.34, glint=(0.12, 0.3), glint_r=0.2):
    """A round eye: white in a dark rim, a big red-brown iris, a black pupil and a white glint."""
    SC_, K, R, W = srgb(SCLERA), srgb(INK), srgb(IRIS), np.ones(3)
    E = 0.035
    def eye(u, v):
        r = np.sqrt(u * u + v * v)
        c = pw.soft(SC_, K, r - (1 - rim), E)
        ri = np.sqrt((u + 0.04) ** 2 + (v - 0.02) ** 2)
        m = np.clip(0.5 + (iris - ri) / (2 * E), 0, 1)[:, None]
        c = c * (1 - m) + R[None] * m
        m = np.clip(0.5 + (pupil - ri) / (2 * E), 0, 1)[:, None]
        c = c * (1 - m) + K[None] * m
        gr = np.sqrt((u - glint[0]) ** 2 + (v - glint[1]) ** 2)
        m = np.clip(0.5 + (glint_r - gr) / (2 * E), 0, 1)[:, None]
        return c * (1 - m) + W[None] * m
    return eye


def curl(k):
    """The one long blue hair: out of the back of the crown, up, over to the model's left and curling down into a
    loop. Its plane is turned some 30 degrees from the front's so its face shows to both of the game's cameras."""
    A = unit(0.86, 0.5, 0)                    # the plane's horizontal
    base = np.array([0.0, 0.035, 0.0])
    pts = [(0.0, 0.6), (0.01, 0.66), (0.022, 0.72), (0.024, 0.765)]
    c, r0 = np.array([-0.026, 0.768]), 0.05
    for th in np.linspace(0.25, 2 * math.pi * 1.08, 14):
        r = r0 * (1 - 0.5 * th / (2 * math.pi * 1.08))
        pts.append((c[0] + r * math.cos(th), c[1] + r * math.sin(th)))
    P = [base + A * a + np.array([0, 0, b]) for a, b in pts]
    radii = list(np.linspace(0.019, 0.016, len(P)))
    k.add("curl", outward(tube(P, radii, seg=14, per=5)), CURL, "head", fuse=False, surface="skin")


def arm(k):
    k.add("arm", tube([SHOULDER, ELBOW, WRIST], ARM_R), GREEN, "arm{s}", mirror=True)
    k.add("hand", ell(PALM, PALM_R), YELLOW, "arm{s}", mirror=True)
    # three round fingers spread forward and down
    for i, (sx, d) in enumerate([(-1, unit(-0.45, -0.75, -0.35)), (0, unit(0.0, -0.85, -0.45)),
                                 (1, unit(0.45, -0.75, -0.4))]):
        k.add(f"finger{i}", ell(PALM + d * 0.045, (0.022, 0.022, 0.022)), YELLOW, "arm{s}", mirror=True)
    # the glove's edge at the wrist, crisp both ways
    a = unit(*(WRIST - ELBOW))
    cut = WRIST + a * 0.0
    def near(P, r):
        Q = np.stack([np.abs(P[:, 0]), P[:, 1], P[:, 2]], axis=1)
        return (np.linalg.norm(Q - PALM, axis=1) < r).astype(float)
    def side(P):
        Q = np.stack([np.abs(P[:, 0]), P[:, 1], P[:, 2]], axis=1)
        return (Q - cut) @ a
    k.paint(lambda P: inside(-side(P), 0.0025) * near(P, 0.11), YELLOW)
    k.paint(lambda P: inside(side(P), 0.0025) * near(P, 0.11), GREEN)


def leg(k):
    k.add("thigh", ell((0.12, -0.01, 0.16), (0.1, 0.108, 0.095)), GREEN, "leg{s}", mirror=True)
    k.add("leg", tube([HIP, KNEE, ANKLE], (0.07, 0.058, 0.046)), GREEN, "leg{s}", mirror=True)
    k.add("foot", ell(FOOT_C, FOOT_R, rot=(0, 0, FOOT_TURN)), GREEN, "leg{s}", mirror=True)
    # three round yellow toes at the foot's front, spread
    for i, ang in enumerate((-30, 0, 30)):
        t = math.radians(ang + FOOT_TURN)
        d = np.array([math.sin(t), -math.cos(t), 0])
        c = FOOT_C + d * 0.078 + np.array([0, 0, 0.004])
        k.add(f"toe{i}", outward(ell(c, (0.026, 0.026, 0.023))), YELLOW, "leg{s}", fuse=False, mirror=True,
              surface="skin")


def politoed(src, out):
    k = Kit(src, coat="scales")
    k.add("body", ell(B, BR, deform=egg), GREEN, "body")
    k.add("head", ell(HC, HR), GREEN, "head")
    k.add("jaw", ell(JAW_C, JAW_R), YELLOW, "head")
    k.add("bump", ell(BUMP_C, BUMP_R), GREEN, "head", mirror=True)
    arm(k)
    leg(k)
    curl(k)

    # The face: green over the mouth line, yellow under it down the throat, both edges crisp
    front = lambda P: inside(P[:, 1] + 0.015, 0.01)
    k.paint(lambda P: inside(mouth_line(P[:, 0]) - P[:, 2], 0.0025) * (P[:, 2] < 0.62), GREEN, on=["jaw", "head", "body"])
    k.paint(lambda P: inside(P[:, 2] - mouth_line(P[:, 0]), 0.0025) * inside(throat_line(P[:, 0]) - P[:, 2], 0.0025)
            * front(P), YELLOW)
    k.paint(lambda P: inside(P[:, 2] - throat_line(P[:, 0]), 0.0025) * (P[:, 2] > 0.36) * (np.abs(P[:, 0]) < 0.17)
            * (P[:, 1] < 0.05), GREEN, on=["jaw", "head", "body"])

    # The pink blush on each cheek, under the eye
    cp, cn = pw.on_ell(HC, HR, CHEEK_DIR)
    k.decal(cp, cn, (0.03, 0.022), BLUSH, eps=0.004, rings=10, segs=40, mirror=True)

    # The yellow belly and its green spiral
    bp, bn = pw.on_ell(B, BR, BELLY_DIR)
    k.decal(bp, bn, BELLY_HALF, pw.spiral_look(ground=YELLOW, ink=GREEN, **SPIRAL), eps=0.004, rings=64, segs=224)

    k.build(voxel=VOXEL, band=0.004)

    ep, en = pw.on_ell(BUMP_C, BUMP_R, EYE_DIR)
    eye = politoed_eye()
    def eyes(state):
        pw.eye_patch(k, state, ep + en * 0.01, en, EYE_HALF, eye, lid=GREEN)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    politoed(*args())
