# Crabominable sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/crabominable.py -- <the export's Crabominable.glb> <out.glb>
#
# Crabrawler grown (crabrawler.py's frame, by sibling: its legs with pointed tips, its round eyes looking up, its bands
# painted round the lower body). From the references: a big white egg of a body, the face at its top front: a wide blue
# patch with two small white eyes looking up, under a pale yellow fringe of hair with two strands curling up from the
# crown; under the face a wide open mouth, dark red inside a purple rim, two white buck teeth above and three sharp
# teeth below. A dark purple wavy band across the chest; the lower body purple with two cyan stripes round it. Two huge
# white furry fists, the largest masses, held forward and low: each a ball whose face is a cyan disc with a dark purple
# paw print (a big pad and three toes), ringed by a jagged ruff of white fur. Thick white arms with a purple patch at the
# elbow. Four short white legs splayed out to pointed cyan tips. (The pictures overrule the task's text: the face is a
# blue patch in the white with the tuft over it, not a face of blue fur; the fists' faces are cyan with purple prints.)
#
# Reference ratios (3D picture): body width / body height 0.86; a fist with its ruff across / body width 0.9; the face
# patch's top at 0.9 of the body's height; the mouth's height / body height 0.22; the hair's strands above the crown
# / body height 0.2. Bones: root, body, arm{s}, leg{s}, hind{s}.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, args, outward, hull, polygon, sibling, axes

cr = sibling("crabrawler")

WHITE, SHADE, PURPLE, CYAN, FACE = "#F4F4F8", "#E2E4EE", "#4B3F7E", "#36C2E8", "#3AA6E0"
HAIR, MOUTH, LIP, TOOTH = "#F2E294", "#6A1C22", "#4B3F7E", "#FAFAF6"
CW, CK, CP = srgb("#FAFAF6"), srgb("#22203A"), srgb("#141018")

# ---- the body: a big white egg, a flatter face at its top front -----------------------------------------------------
BODY_C, BODY_R = np.array([0.0, 0.0, 0.188]), np.array([0.11, 0.1, 0.128])
FACE_C, FACE_R = np.array([0.0, -0.035, 0.285]), np.array([0.078, 0.06, 0.05])


def body(k):
    k.add("body", ell(BODY_C, BODY_R, deform=cr.egg), WHITE, "body")
    k.add("face", ell(FACE_C, FACE_R), WHITE, "body")
    k.add("under", ell(BODY_C + (0, -0.004, -0.068), BODY_R * (0.94, 0.92, 0.66)), WHITE, "body")


def bands(k):
    """The lower body purple with two cyan stripes round it, and the wavy purple band across the chest."""
    c = BODY_C
    def low(P):
        x, y, z = P[:, 0] - c[0], P[:, 1] - c[1], P[:, 2]
        return inside(z - 0.132, 0.002) * inside(np.hypot(x, y) - 0.13, 0.004)
    k.paint(low, PURPLE)
    cr.stripe(k, z0=0.106, z1=0.117, c=c, reach=0.13, color=CYAN)
    cr.stripe(k, z0=0.083, z1=0.093, c=c, reach=0.13, color=CYAN)
    # the chest's wavy band: paint kept to the body's own radius (a patch cast from the front lands on the fists,
    # which stand before the chest)
    xs = np.linspace(-0.09, 0.09, 25)
    top = [(x, 0.212 + 0.007 * math.cos(x / 0.09 * 2.2 * math.pi) - 0.01 * (x / 0.09) ** 2) for x in xs]
    bot = [(x, 0.17 - 0.006 * math.cos(x / 0.09 * 2.2 * math.pi) + 0.008 * (x / 0.09) ** 2) for x in xs[::-1]]
    band = top + bot
    k.paint(lambda P: cr.poly_mask(P[:, 0], P[:, 2], band, 0.0018) * (P[:, 1] < -0.03)
            * inside(np.hypot(P[:, 0] - c[0], P[:, 1] - c[1]) - 0.118, 0.003), PURPLE)


# ---- the hair: a pale fringe over the face and two strands curling up -----------------------------------------------
def hair(k):
    k.add("hair", outward(ell((0.0, -0.045, 0.312), (0.064, 0.048, 0.022), rot=(-30, 0, 0))), HAIR, "body", fuse=False,
          surface="hair")
    for i, (x, l) in enumerate(((0.0, 1.0), (0.034, 0.85), (-0.034, 0.85), (0.058, 0.6), (-0.058, 0.6))):
        root = np.array((x, -0.075 + 0.012 * abs(x) / 0.058, 0.32))
        tip = root + np.array((x * 0.15, -0.012, -0.03 * l))
        pts = [root + (0.016, 0.0, 0), root + (-0.016, 0, 0), root + (0, 0.012, 0.006), root + (0, -0.008, 0.004),
               tip]
        k.add(f"lock{i}", hull(pts), HAIR, "body", fuse=False, surface="hair")
    for s in (1, -1):
        pts = [(0.012 * s, -0.04, 0.32), (0.026 * s, -0.042, 0.356), (0.046 * s, -0.036, 0.376), (0.062 * s, -0.026, 0.371)]
        k.add("strand" + ("R" if s > 0 else "L"), outward(tube(pts, [0.009, 0.008, 0.007, 0.006], seg=12, per=6)),
              HAIR, "body", fuse=False, surface="hair")


# ---- the face: the blue patch, the eyes, the mouth -------------------------------------------------------------------
FACE_PATCH = dict(c=(0.0, -0.094, 0.302), n=unit(0, -1, 0.35), half=(0.056, 0.026))
EYE_C = np.array([0.022, -0.096, 0.303])
EYE_N = unit(0.3, -1, 0.35)
EYE_HALF = (0.0145, 0.0155)
MOUTH_C, MOUTH_N = np.array([0.0, -0.1, 0.25]), unit(0, -1, 0.08)


def face(k):
    rect = polygon([(-1, -0.7), (-0.88, -1), (0.88, -1), (1, -0.7), (1, 0.7), (0.88, 1), (-0.88, 1), (-1, 0.7)])
    k.decal(FACE_PATCH["c"], FACE_PATCH["n"], FACE_PATCH["half"], FACE, eps=0.0025, outline=rect, rings=12, segs=96)
    # the mouth: a purple rim, the dark red inside, two buck teeth above and three teeth below
    mo = polygon([(-1, 0.6), (-0.7, 1), (0.7, 1), (1, 0.6), (0.6, -1), (-0.6, -1)])
    k.decal(MOUTH_C, MOUTH_N, (0.056, 0.036), LIP, eps=0.0036, outline=mo, rings=12, segs=96)
    k.decal(MOUTH_C, MOUTH_N, (0.046, 0.027), MOUTH, eps=0.005, outline=mo, rings=12, segs=96)
    for x in (-0.009, 0.009):
        k.decal(MOUTH_C + (x, -0.001, 0.018), MOUTH_N, (0.0075, 0.0085), TOOTH, eps=0.0065,
                outline=polygon([(-1, 1), (1, 1), (0.8, -1), (-0.8, -1)]), rings=6, segs=32)
    for x in (-0.024, 0.0, 0.024):
        k.decal(MOUTH_C + (x, -0.001, -0.017), MOUTH_N, (0.009, 0.01), TOOTH, eps=0.0065,
                outline=polygon([(-1, -1), (1, -1), (0, 1)]), rings=6, segs=36)


def eyes(k):
    lid = srgb(FACE)
    def lay(state):
        k.decal(EYE_C, EYE_N, EYE_HALF, eye_look(state, cr.open_eye, lid, CK, line=0.18), eps=0.0045, rings=20,
                segs=56, mirror=True)
    return lay


# ---- the arms and the fists --------------------------------------------------------------------------------------------
ARM = [(0.1, -0.02, 0.27), (0.148, -0.065, 0.235), (0.14, -0.11, 0.185)]
ARM_R = [0.034, 0.031, 0.03]
FIST_C, FIST_R = np.array([0.12, -0.158, 0.158]), np.array([0.078, 0.064, 0.078])
FIST_N = unit(0.45, -1, 0.12)


def arms(k):
    for side, sn in ((1, "R"), (-1, "L")):
        m = np.array([side, 1, 1.0])
        P = [np.array(p) * m for p in ARM]
        k.add("arm" + sn, tube(P, ARM_R, seg=16, per=6), WHITE, "arm" + sn)
        el = P[1]
        k.paint(lambda Q, el=el: inside(np.linalg.norm(Q - el, axis=1) - 0.027, 0.0015), PURPLE)
        fist(k, side)


def fist(k, side):
    sn = "R" if side > 0 else "L"
    m = np.array([side, 1, 1.0])
    c = FIST_C * m
    n = FIST_N * m
    # the ball, flattened along the face's normal
    a, b = axes(n)
    V, F = ell((0, 0, 0), (FIST_R[0], FIST_R[2], FIST_R[1]))
    V = V @ np.stack([a, b, n]).T
    k.add("fist" + sn, (V + c, F), WHITE, "arm" + sn)
    # the ruff: a ring of white spikes round the face, pointing out and a little back
    up = np.array([0, 0, 1.0]) - n * n[2]; up /= np.linalg.norm(up)
    rt = np.cross(up, n)
    face_c = c + n * FIST_R[1] * 0.55
    for i in range(15):
        t = 2 * math.pi * i / 15 + 0.2
        d = math.cos(t) * rt + math.sin(t) * up
        rr = 0.06 + 0.004 * (i % 2)
        root = face_c + d * rr - n * 0.004
        tip = face_c + d * (rr + 0.024 + 0.008 * (i % 2)) - n * 0.012
        w = np.cross(d, n)
        pts = [root + w * 0.014, root - w * 0.014, root + n * 0.01, root - n * 0.016, tip]
        k.add(f"ruff{sn}{i}", hull(pts), WHITE, "arm" + sn, fuse=False, surface="fur")


def fist_faces(k):
    """The cyan disc and the purple paw print on each fist's face (patches on the fused fist)."""
    for side in (1, -1):
        m = np.array([side, 1, 1.0])
        n = FIST_N * m
        up = np.array([0, 0, 1.0]) - n * n[2]; up /= np.linalg.norm(up)
        rt = np.cross(up, n)
        fc = FIST_C * m + n * FIST_R[1]
        k.decal(fc, n, (0.058, 0.058), CYAN, up=up, eps=0.0025, rings=14, segs=64)
        pad = polygon([(-1, 0.5), (-0.6, 1), (0.6, 1), (1, 0.5), (0.85, -0.6), (0.4, -1), (-0.4, -1), (-0.85, -0.6)])
        k.decal(fc - up * 0.012, n, (0.03, 0.024), PURPLE, up=up, eps=0.0045, outline=pad, rings=10, segs=64)
        for dx, dz in ((-0.026, 0.02), (0.0, 0.03), (0.026, 0.02)):
            k.decal(fc + rt * dx + up * dz, n, (0.0095, 0.0115), PURPLE, up=up, eps=0.0045, rings=6, segs=32)


# ---- the legs: Crabrawler's, white and longer ----------------------------------------------------------------------
LEGS = [
    ("leg{s}", (0.08, -0.04, 0.13), (0.145, -0.07, 0.13), (0.168, -0.092, 0.05), (0.172, -0.098, 0.0)),
    ("hind{s}", (0.08, 0.04, 0.13), (0.145, 0.07, 0.13), (0.168, 0.092, 0.05), (0.172, 0.098, 0.0)),
]


def crabominable(src, out_path):
    k = Kit(src, coat="fur")
    body(k)
    bands(k)
    hair(k)
    cr.legs(k, specs=LEGS, r=(0.025, 0.023, 0.02), tip_r=0.017, color=WHITE, tip_color=CYAN, surface="fur")
    arms(k)
    face(k)
    fist_faces(k)
    k.build(voxel=0.0042, smooth=3, band=0.005, wband=0.04)
    k.eye_states(eyes(k))
    k.export(out_path)


if __name__ == "__main__":
    src, out = args()
    crabominable(src, out)
