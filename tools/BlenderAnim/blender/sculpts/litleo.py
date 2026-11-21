# Litleo sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/litleo.py -- <the export's Litleo.glb> <out.glb>
# A dark grey-brown lion cub with a big round head: a tan face that runs up round both big round eyes under a dark
# forehead which comes down to a point between them, a small tan muzzle with a red triangular nose and an open mouth
# with its pink tongue out; huge round ears, tan with a dark hollow; a crest of three red flame points on the crown,
# orange at its root; a short stout body on short legs with big tan paws; a thin dark tail rising behind with a tan
# curl of fur at its end.
#
# Written to be grown from (Pyroar keeps its colours, its paws, its nose, its tail's tuft and its way of painting the
# tan of the face). Each part is a constant or a small function below, the calls at the end.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, blade, hull, outward, srgb, inside, smoothstep, unit, eye_look, polygon, args)

# --- colours ------------------------------------------------------------------------------------------------------------
DARK = "#4E4442"           # the coat: a warm dark grey-brown
TAN = "#DCC08E"            # the face, the ears, the paws, the tail's tuft
EARIN = "#3E3634"          # the ears' hollow
RED, ORANGE = "#E2362A", "#F5902A"
NOSE = "#E8343A"
MOUTH, TONGUE = "#7A2A2E", "#E4878E"
INK = "#1C1818"
EYE, PUPIL = "#FAFAF6", "#16141A"

# --- the frame (the skeleton's joints: body (0, 0, 0.14), head (0, -0.07, 0.2), legs (+-0.045, -0.07, 0.12),
#     hind legs (+-0.045, 0.07, 0.12), tail (0, 0.08, 0.17)) ---------------------------------------------------------------
H = np.array([0, -0.116, 0.226])         # the head's centre, over and in front of its joint
HR = np.array([0.099, 0.088, 0.089])       # a big round cub's head, a little wider than tall
LEG_X = 0.046
FORE_Y, HIND_Y = -0.066, 0.07
BODY_Z = 0.112
LEG_TOP = 0.12
LEG_R = (0.027, 0.023, 0.022)


def add_body(color=DARK):
    """A short stout body, its belly low over the legs."""
    k.add("chest", ell((0, -0.045, BODY_Z + 0.004), (0.062, 0.058, 0.064)), color, "body")
    k.add("middle", ell((0, 0.02, BODY_Z - 0.002), (0.058, 0.07, 0.06)), color, "body")
    k.add("rump", ell((0, 0.07, BODY_Z), (0.06, 0.052, 0.062)), color, "body")
    k.add("neck", ell((0, -0.08, 0.18), (0.048, 0.045, 0.05)), color, "head")


def add_legs(color=DARK, paw=TAN):
    """Short thick legs, a shoulder and a haunch fused over the top of each, big round tan paws."""
    for bone, y, nm in (("leg{s}", FORE_Y, "fore"), ("hind{s}", HIND_Y, "hind")):
        k.add(f"{nm}top", ell((LEG_X - 0.004, y + 0.004, LEG_TOP - 0.012), (0.03, 0.036, 0.042)), color, bone, mirror=True)
        k.add(f"{nm}leg", tube([(LEG_X, y, LEG_TOP), (LEG_X, y, 0.07), (LEG_X, y, 0.03)], list(LEG_R)), color, bone,
              mirror=True)
        k.add(f"{nm}paw", ell((LEG_X + 0.002, y - 0.01, 0.018), (0.031, 0.037, 0.019)), paw, bone, mirror=True)


def add_paw_toes(color=TAN, x=LEG_X + 0.002, ys=(FORE_Y, HIND_Y), z=0.018, r=(0.031, 0.037, 0.019), bones=("leg{s}", "hind{s}")):
    """Three toes bulging along the front of each paw (a paw reads as a paw from the front)."""
    for bone, y in zip(bones, ys):
        for j, dx in enumerate((-0.6, 0.0, 0.6)):
            c = (x + dx * r[0], y - 0.01 - r[1] * 0.78, z - r[2] * 0.15)
            k.add(f"toe{bone[0]}{j}", ell(c, (r[0] * 0.33, r[1] * 0.3, r[2] * 0.72)), color, bone, mirror=True)


def add_head(color=DARK, muzzle=TAN):
    k.add("head", ell(H, HR), color, "head")
    # a small rounded muzzle low on the face
    k.add("muzzle", ell(H + (0, -0.066, -0.032), (0.04, 0.028, 0.029)), muzzle, "head")


def round_head(P):
    """The head's own map: the angle round its upright axis (times its radius, metres) and the height."""
    return np.arctan2(P[:, 0], -(P[:, 1] - H[1])) * HR[0], P[:, 2]


def tan_top(s):
    """The top of the face's tan at a place across the face: up round each eye, a dark point coming down between."""
    a = np.abs(s)
    top = H[2] + 0.058 - 0.068 * np.clip(1 - a / 0.03, 0, 1) ** 1.2
    return top - 0.75 * np.clip(a - 0.085, 0, None)


def add_face_tan(color=TAN, coat=DARK):
    front = lambda P: (np.abs(np.arctan2(P[:, 0], -(P[:, 1] - H[1]))) < 1.5) * (P[:, 1] < H[1] + 0.01)
    k.paint(lambda P: inside(P[:, 2] - tan_top(round_head(P)[0]), 0.0018) * front(P) * (P[:, 2] > H[2] - 0.11), color,
            on=["head", "muzzle"])
    # the chin and throat stay dark under the jaw
    k.paint(lambda P: inside(P[:, 2] - (H[2] - 0.058), 0.002) * (P[:, 1] > H[1] - 0.06) * (P[:, 2] > 0.1), coat,
            on=["head"])


# --- the ears: huge round discs at the head's upper corners, turned a little out, tan with a dark hollow --------------
EC = np.array([0.093, -0.096, 0.31])
ER = (0.049, 0.018, 0.049)
EROT = (-8, 0, 22)


def add_ears(color=TAN, hollow=EARIN):
    k.add("ear", ell(EC, ER, rot=EROT), color, "head", mirror=True)
    m = np.array(__import__("mathutils").Euler([math.radians(a) for a in EROT]).to_matrix())
    n = m @ np.array([0, -1.0, 0]); up = m @ np.array([0, 0, 1.0])
    c = EC + n * ER[1] * 0.9 - up * 0.006 + m @ np.array([-0.006, 0, 0])
    k.decal(c, n, (0.029, 0.029), hollow, up=up, eps=0.0025, rings=12, segs=48, mirror=True)


# --- the crest: three red flame points on the crown, the middle the tallest, orange at the root ----------------------
CREST = [((0.0, 0.0), (0.0, 0.35, 1.0), 0.064, 0.024), ((0.01, 0.008), (0.18, 0.4, 0.9), 0.046, 0.019),
         ((-0.01, 0.008), (-0.18, 0.4, 0.9), 0.05, 0.019)]


def add_crest(color=RED, root=ORANGE, spikes=CREST, at=None):
    top = H + (0, 0.004, HR[2] * 0.9) if at is None else np.array(at)
    k.add("crestroot", ell(top + (0, 0.004, 0.004), (0.022, 0.024, 0.014)), root, "head")
    for i, ((x, y), d, ln, r) in enumerate(spikes):
        d = unit(*d)
        base = top + (x, y, 0.0)
        k.add(f"crest{i}", outward(blade([base, base + d * ln * 0.55, base + d * ln], [r, r * 0.6, 0.0015],
                                         (0, 1, 0), 0.55)),
              color, "head", fuse=False, surface="hair")
        k.add(f"crestin{i}", outward(blade([base + d * 0.004 - (0, 0.004, 0), base + d * ln * 0.3 - (0, 0.004, 0)],
                                           [r * 0.75, r * 0.35], (0, 1, 0), 0.55)),
              root, "head", fuse=False, surface="hair")


# --- the tail: thin and dark, up and back from the rump, a tan curl of fur at its end ----------------------------------
TAIL = [(0.0, 0.085, 0.15), (0.0, 0.125, 0.16), (0.0, 0.16, 0.19), (0.0, 0.178, 0.235)]
TAIL_R = [0.011, 0.0085, 0.008, 0.0075]


def add_tail(color=DARK, tuft=TAN, tail=TAIL, radii=TAIL_R, size=1.0):
    k.add("tail", tube(tail, radii), color, "tail")
    end = np.array(tail[-1])
    d = unit(*(end - np.array(tail[-2])))
    c = end + d * 0.012 * size
    k.add("tailtuft", ell(c, np.array((0.019, 0.02, 0.022)) * size), tuft, "tail")
    # the curl: a point of fur rising out of the tuft and turning forward
    tip = [c + np.array((0, 0.004, 0.014)) * size, c + np.array((0, -0.004, 0.032)) * size,
           c + np.array((0, -0.016, 0.04)) * size]
    k.add("tailcurl", blade(tip, np.array([0.012, 0.008, 0.002]) * size, (1, 0, 0), 0.6), tuft, "tail")


# --- the face's small parts: a red nose, an open mouth with its tongue out -----------------------------------------------
def nose_shape(V):
    V = V.copy()
    t = np.clip((V[:, 2] + 1) / 2, 0, 1)
    V[:, 0] *= 0.3 + 0.85 * t
    return V


def add_nose(at=H + (0, -0.096, -0.016), r=(0.017, 0.009, 0.011), color=NOSE):
    k.add("nose", ell(at, r, rot=(12, 0, 0), deform=nose_shape), color, "head", fuse=False, surface="skin")


CT, CM, CG = srgb(TAN), srgb(MOUTH), srgb(TONGUE)


def mouth(u, v):
    """An open mouth: a dark lens, the pink tongue filling its lower half and coming out over the lip."""
    c = np.tile(CT, (len(u), 1))
    inner = (u / 0.8) ** 2 + ((v - 0.1) / 0.75) ** 2 < 1
    c[inner] = CM
    c[inner & (v < 0.05)] = CG
    c[((u / 0.46) ** 2 + ((v + 0.45) / 0.52) ** 2 < 1)] = CG
    return c


def add_mouth(at=H + (0, -0.09, -0.052), half=(0.02, 0.017)):
    k.decal(at, unit(0, -1, -0.45), half, mouth, eps=0.0025, rings=16, segs=64)


# --- the eyes: big round whites with a bold black ring and a big black pupil looking ahead, a white glint -------------
EYE_DIR = unit(0.43, -0.89, 0.1)
EYE_R = 0.031
E = H + EYE_DIR / np.linalg.norm(EYE_DIR / HR)
CW, CK = srgb(EYE), srgb(PUPIL)


def eye(u, v):
    c = np.tile(CW, (len(u), 1))
    r = np.hypot(u, v)
    c[r > 0.82] = CK
    p = np.hypot((u + 0.05) / 0.48, (v + 0.02) / 0.52)
    c[p < 1] = CK
    c[np.hypot(u - 0.12, v - 0.18) < 0.15] = CW
    return c


def eyes(state):
    look = eye_look(state, eye, srgb(TAN), srgb(INK), line=0.16)
    for side in (1, -1):
        col = look if side == 1 else (lambda a, b, f=look: f(-a, b))
        k.decal(E * np.array([side, 1, 1.0]), EYE_DIR * np.array([side, 1, 1.0]), (EYE_R, EYE_R), col, eps=0.0028,
                rings=20, segs=56)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="fur")
    add_body()
    add_legs()
    add_paw_toes()
    add_head()
    add_face_tan()
    add_ears()
    add_crest()
    add_tail()
    add_nose()
    add_mouth()
    k.build(voxel=0.0034, smooth=4, band=0.003, wband=0.03)
    k.eye_states(eyes)
    k.export(out)
