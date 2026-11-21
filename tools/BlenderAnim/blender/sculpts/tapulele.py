# Tapu Lele sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/tapulele.py -- <the export's Tapu Lele.glb> <out.glb>
#
# The guardians' frame is tapukoko.py's (sibling): the small dark figure, its eyes, its arms. From the references,
# Tapu Lele: the figure sits in the lower half of its shell, a big pink egg of a pod open at the top with a white rim,
# a lobe of it rising behind on the +X side; on the pod a white diamond in a dark line (front left, back) and a dark
# hexagon filled red (front right). Black feet over the pod's front rim, pink puffs at the wrists, black hands on the
# knees. The other half of the shell is a pink pointed cap over the head, leaning back, its brim white, a white
# diamond on its front. Long pink locks fall from under the cap at each side, outside the pod, and curl outward at
# their ends; a small curl at each temple. A black face with big teal eyes, a dark pupil, a pink line over each.
# The skeleton: root, body (0, 0, 0.316), head (0, -0.02, 0.406), armR/L (+-0.03, -0.02, 0.386).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from mathutils import Euler
from sculptkit import Kit, ell, tube, ring_band, outward, srgb, inside, unit, polygon, args, sibling

T = sibling("tapukoko")

BLACK, PINK, HAIR, PUFF = "#2A2830", "#F4A8C8", "#E8506E", "#F7B4D2"
WHITE, LINE, HEXFILL, DARK = "#FAF6F4", "#5A2838", "#D8486A", "#2A262C"
TEAL, PUPIL, LASH = "#3CCAC2", "#14262A", "#F06A90"

LELE = dict(
    body=BLACK,
    torso=[((0, -0.015, 0.338), (0.034, 0.029, 0.05))],
    neck=((0, -0.018, 0.376), (0, -0.024, 0.408)), neck_r=0.025,
    head_c=(0, -0.03, 0.452), head_r=(0.055, 0.05, 0.054),
    arm=((0.03, -0.02, 0.386), (0.055, -0.05, 0.35), (0.034, -0.09, 0.31)), arm_r=0.0115, hand_r=0.0145,
    eye_c=(0.024, -0.077, 0.449), eye_n=unit(0.42, -0.9, 0.05), eye_half=(0.019, 0.0215),
)

POD_C, POD_R = np.array([0.0, 0.005, 0.19]), np.array([0.135, 0.122, 0.155])
POD_TOP = 0.7                        # the pod's open top, on the unit sphere
CAP_C, CAP_R, CAP_TILT = np.array([0.0, -0.022, 0.508]), np.array([0.059, 0.056, 0.105]), -18


def on_ell(c, r, d):
    """The point of an ellipsoid along d from its centre, and the surface's normal there."""
    d = np.array(d, float); d /= np.linalg.norm(d)
    p = d / math.sqrt(((d / r) ** 2).sum())
    n = p / r ** 2
    return c + p, n / np.linalg.norm(n)


def flat_top(V):
    V = V.copy()
    V[:, 2] = np.minimum(V[:, 2], POD_TOP)
    return V


def cone(V):
    """The cap: a cone with a flat foot, narrowing to a rounded point."""
    V = V.copy()
    V[:, 2] = np.maximum(V[:, 2], -0.3)
    t = np.clip((V[:, 2] + 0.3) / 1.3, 0, 1)
    s = 1 - 0.86 * t ** 1.1
    V[:, 0] *= s; V[:, 1] *= s
    return V


def cap_frame():
    return np.array(Euler([math.radians(CAP_TILT), 0, 0]).to_matrix())


def pod(k):
    k.add("pod", ell(POD_C, POD_R, deform=flat_top, sub=5), PINK, "body")
    k.add("lobe", ell((0.055, 0.1, 0.25), (0.05, 0.05, 0.08), rot=(-25, 0, -20)), PINK, "body")
    top = POD_C[2] + POD_R[2] * POD_TOP
    k.paint(lambda P: inside(top - 0.022 - P[:, 2], 0.002), WHITE, on=["pod"])
    k.paint(lambda P: inside(top - 0.006 - P[:, 2], 0.002)
            * inside((P[:, 0] / 0.084) ** 2 + ((P[:, 1] - 0.0) / 0.076) ** 2 - 1, 0.03), DARK, on=["pod"])
    # the feet over the front rim
    k.add("foot", ell((0.033, -0.094, top + 0.004), (0.02, 0.024, 0.016)), BLACK, "body", mirror=True)
    # the marks
    DIAMOND = polygon([(0, 1), (1, 0), (0, -1), (-1, 0)])
    HEX = polygon([(math.cos(a), math.sin(a)) for a in np.linspace(math.pi / 2, math.pi / 2 + 2 * math.pi, 6,
                                                                     endpoint=False)])
    for d, half in (((-0.55, -0.85, -0.1), (0.032, 0.05)), ((0.0, 1.0, 0.15), (0.034, 0.052))):
        p, n = on_ell(POD_C, POD_R, d)
        k.decal(p, n, half, LINE, outline=DIAMOND, eps=0.002, rings=10, segs=48)
        k.decal(p, n, (half[0] * 0.62, half[1] * 0.7), WHITE, outline=DIAMOND, eps=0.0035, rings=10, segs=48)
    p, n = on_ell(POD_C, POD_R, (0.55, -0.82, -0.35))
    k.decal(p, n, (0.05, 0.056), LINE, outline=HEX, eps=0.002, rings=12, segs=60)
    k.decal(p, n, (0.038, 0.043), HEXFILL, outline=HEX, eps=0.0035, rings=12, segs=60)
    for a in (0.0, math.pi / 3, -math.pi / 3):
        k.decal(p, n, (0.0045, 0.04), LINE, eps=0.005, rings=6, segs=28,
                up=(math.sin(a), 0, math.cos(a)))


def cap(k):
    M = cap_frame()
    k.add("cap", ell(CAP_C, CAP_R, rot=(CAP_TILT, 0, 0), deform=cone, sub=5), PINK, "head")
    axis = M @ np.array([0, 0, 1.0])
    foot = CAP_C + M @ np.array([0, 0, -0.3 * CAP_R[2]])
    k.add("brim", ring_band(foot + axis * 0.008, axis, 0.0605, 0.02, thick=0.008), WHITE, "head", fuse=False)
    # the white diamond in a dark line on the cap's front
    p = CAP_C + M @ np.array([0, -0.039, 0.03])
    n = M @ unit(0, -1, 0.3)
    DIAMOND = polygon([(0, 1), (1, 0), (0, -1), (-1, 0)])
    k.decal(p, n, (0.022, 0.034), LINE, outline=DIAMOND, eps=0.002, rings=10, segs=48, up=tuple(axis))
    k.decal(p, n, (0.013, 0.022), WHITE, outline=DIAMOND, eps=0.0035, rings=10, segs=48, up=tuple(axis))


def hair(k):
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        lock = [(0.04, -0.02, 0.468), (0.068, -0.034, 0.432), (0.094, -0.06, 0.37), (0.11, -0.076, 0.3),
                (0.118, -0.082, 0.248), (0.132, -0.084, 0.214), (0.152, -0.08, 0.214), (0.16, -0.076, 0.236),
                (0.148, -0.072, 0.252)]
        k.add(f"lock{side}", outward(tube(np.array(lock) * m, [0.024, 0.025, 0.024, 0.022, 0.02, 0.018, 0.016, 0.014,
                                                                0.013])), HAIR, "head", fuse=False, surface="hair")
        curl = [(0.046, -0.04, 0.476), (0.066, -0.044, 0.482), (0.08, -0.048, 0.47), (0.078, -0.052, 0.453),
                (0.066, -0.052, 0.458)]
        k.add(f"curl{side}", outward(tube(np.array(curl) * m, [0.011, 0.0105, 0.0100, 0.0095, 0.009])), HAIR,
              "head", fuse=False, surface="hair")


def lele_extras(k, F):
    pod(k)
    cap(k)
    hair(k)
    S, E, H = (np.array(p, float) for p in F["arm"])
    k.add("puff", ell(E + (H - E) * 0.72, (0.019, 0.018, 0.017)), PUFF, "arm{s}", mirror=True)


def lele_eyes(k, F):
    IRIS, PUP, CL, CB = srgb(TEAL), srgb(PUPIL), srgb(LASH), srgb(BLACK)

    def open_eye(u, v):
        c = np.tile(CB, (len(u), 1))
        r = (u / 0.92) ** 2 + (v / 0.95) ** 2
        c[r < 1] = srgb("#1E5A5E")
        c[r < 0.72] = IRIS
        c[((u + 0.02) / 0.3) ** 2 + ((v + 0.05) / 0.34) ** 2 < 1] = PUP
        c[((u + 0.24) / 0.14) ** 2 + ((v - 0.24) / 0.14) ** 2 < 1] = (1, 1, 1)
        c[(np.abs(v - (0.86 - 0.12 * u * u)) < 0.13) & (np.abs(u) < 1.0)] = CL
        return c
    return T.eye_lay(k, F, open_eye, BLACK, LASH)


if __name__ == "__main__":
    src, out = args()
    T.guardian(src, out, LELE, lele_extras, lele_eyes, voxel=0.0038)
