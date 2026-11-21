# Klawf sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/klawf.py -- <the export's Klawf.glb> <out.glb>
#
# From the references: a crab whose great round shell is the body, pale cream above and a little darker tan below,
# its front wide and flat; across the upper front a broad dark maroon mouth whose top and bottom edges are blocky
# steps, a pink cheek each side of it, and low in the middle of the lower shell an arch like a doorway. Two small
# white eyes with dark pupils on the top rim, each sitting in an orange-brown cup. Clumps of dark olive moss, spiky:
# two on the top rim between the eyes, a big one round each claw's wrist, one on a leg each side. Two big orange
# pincers raised high beside the shell, each a thick palm with a long outer finger curving over toward a short inner
# one; three thick red-orange legs a side, splayed out and down to points. Kept from the shell's width: the claws stand
# a little forward of the shoulders and the feet are drawn in fore and aft, so it fits its cell (the board check).
# Skeleton: body (0, 0, 0.2), head (0, -0.05, 0.28), arm{s} (0.15, -0.04, 0.22), leg{s}, hind{s}, hind{s}.001
# (0.13, -0.06 / 0.02 / 0.09, 0.16).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, blade, hull, srgb, inside, unit, eye_look, args, outward, polygon, boxy,
                       axes)

CREAM, TAN, ARCH = "#F4E1B2", "#E2C796", "#C7A673"
MOUTH, TONGUE, CHEEK = "#78303A", "#A64452", "#F2A096"
CLAW, LEG, SOCKET = "#F2803A", "#E2602E", "#C8622E"
MOSS, MOSS_DK = "#5C6232", "#454A24"
WHITE, PUPIL = "#FAFAF4", "#1C1A1C"

BODY_C, BODY_R = np.array([0.0, 0.006, 0.2]), np.array([0.168, 0.122, 0.142])


def body(k):
    k.add("body", ell(BODY_C, BODY_R, deform=boxy(2.5)), CREAM, "body", surface="shell")
    k.add("under", ell(BODY_C + (0, 0.01, -0.05), BODY_R * (0.9, 0.9, 0.62)), TAN, "body", surface="shell")


def lower_shell(k):
    """The lower half of the front a shade darker, under a level line a little below the mouth."""
    k.paint(lambda P: inside(P[:, 2] - 0.168, 0.003), TAN, on=["body", "under"])


# ---- the eyes: white balls in orange-brown cups on the top rim -----------------------------------------------------
EYE_C = np.array([0.088, -0.05, 0.328])
EYE_R = 0.031
SOCKET_C = np.array([0.088, -0.04, 0.305])


def eyes_and_sockets(k):
    k.add("socket", ell(SOCKET_C, (0.042, 0.038, 0.03)), SOCKET, "head", mirror=True, surface="shell")
    k.add("eyeball", ell(EYE_C, (EYE_R, EYE_R, EYE_R)), WHITE, "head", mirror=True, surface="shell")


# ---- the face: a broad mouth with blocky steps, pink cheeks, the arch below -----------------------------------------
def mouth_outline():
    """Blocky steps along the top and the bottom edge (u, v in the patch's frame)."""
    top = [(1.0, 0.25), (0.86, 0.62), (0.62, 0.62), (0.62, 0.9), (0.2, 0.9), (0.2, 0.66), (-0.16, 0.66),
           (-0.16, 0.92), (-0.56, 0.92), (-0.56, 0.64), (-0.84, 0.64), (-1.0, 0.25)]
    bottom = [(-1.0, -0.2), (-0.84, -0.6), (-0.5, -0.6), (-0.5, -0.9), (0.08, -0.9), (0.08, -0.66),
              (0.46, -0.66), (0.46, -0.88), (0.84, -0.88), (1.0, -0.2)]
    return polygon(top + bottom)


CM, CT = srgb(MOUTH), srgb(TONGUE)


def mouth_look(u, v):
    c = np.tile(CM, (len(u), 1))
    c[(v < -0.35) & (np.abs(u) < 0.7)] = CT
    return c


def face(k):
    k.decal((0.0, -0.118, 0.228), unit(0, -1, 0.12), (0.118, 0.036), mouth_look, eps=0.003,
            outline=mouth_outline(), rings=16, segs=200)
    k.decal((0.128, -0.083, 0.205), unit(0.62, -1, 0.05), (0.024, 0.019), CHEEK, eps=0.003, mirror=True)
    arch = polygon([(-1, -1), (1, -1), (1, 0.1)] + [(math.cos(t), 0.1 + 0.9 * math.sin(t))
                                                   for t in np.linspace(0.15, math.pi - 0.15, 9)] + [(-1, 0.1)])
    k.decal((0.0, -0.108, 0.105), unit(0, -1, -0.35), (0.036, 0.042), ARCH, eps=0.003, outline=arch, segs=80)


# ---- moss: a dark core with short spikes radiating, loose so its points stay sharp --------------------------------
def moss(k, name, c, r, bone, n=16, seed=1, flat=(1.0, 1.0, 0.8)):
    rs = np.random.RandomState(seed)
    c = np.array(c, float)
    k.add(name, outward(ell(c, np.array(flat) * r * 0.9, sub=3)), MOSS, bone, fuse=False, surface="leaf")
    g = (math.sqrt(5) - 1) / 2
    for i in range(n):
        zz = 1 - 2 * (i + 0.5) / n
        th = 2 * math.pi * g * i + rs.rand() * 0.5
        d = np.array([math.sqrt(1 - zz * zz) * math.cos(th), math.sqrt(1 - zz * zz) * math.sin(th), zz])
        d = d * np.array(flat); d /= np.linalg.norm(d)
        a, b = axes(d)
        base = c + d * r * 0.6
        w = r * 0.5
        tip = c + d * r * (1.3 + 0.15 * rs.rand())
        pts = [base + a * w, base - 0.5 * a * w + b * w * 0.87, base - 0.5 * a * w - b * w * 0.87, tip]
        k.add(f"{name}sp{i}", hull(pts), MOSS_DK if i % 3 == 0 else MOSS, bone, fuse=False, surface="leaf")


def top_moss(k):
    moss(k, "mosstopa", (-0.035, 0.035, 0.336), 0.04, "body", seed=3)
    moss(k, "mosstopb", (0.035, 0.05, 0.334), 0.036, "body", seed=5)


# ---- the claws: thick arm, a big palm, a long outer finger curving over toward a short inner one -------------------
ARM = [(0.13, -0.035, 0.205), (0.205, -0.07, 0.235), (0.218, -0.085, 0.3)]


def claw(k, side):
    sn = "R" if side > 0 else "L"
    bone = "arm" + sn
    m = np.array([side, 1, 1.0])
    P = [np.array(p) * m for p in ARM]
    k.add("arm" + sn, outward(tube(P, [0.03, 0.03, 0.03], seg=16, per=6)), CLAW, bone, fuse=False, surface="shell")
    wrist = P[-1]
    palm = wrist + np.array([0.004 * side, -0.004, 0.06])
    k.add("palm" + sn, outward(ell(palm, (0.05, 0.042, 0.068), rot=(0, -10 * side, 0))), CLAW, bone, fuse=False,
          surface="shell")
    o = np.array([side, 1, 1.0])
    outer = [palm + o * (0.026, -0.002, 0.04), palm + o * (0.036, -0.004, 0.095), palm + o * (0.022, -0.006, 0.14),
             palm + o * (-0.006, -0.006, 0.158)]
    k.add("finger" + sn, outward(blade(outer, [0.033, 0.03, 0.021, 0.008], (0, 1, 0), 0.7)), CLAW, bone,
          fuse=False, surface="shell")
    inner = [palm + o * (-0.03, -0.002, 0.04), palm + o * (-0.036, -0.004, 0.082), palm + o * (-0.026, -0.006, 0.112)]
    k.add("thumb" + sn, outward(blade(inner, [0.024, 0.018, 0.007], (0, 1, 0), 0.7)), CLAW, bone, fuse=False,
          surface="shell")
    moss(k, "mosswrist" + sn, wrist + np.array([0, 0.0, 0.004]), 0.05, bone, n=18, seed=7 + side,
         flat=(1.0, 1.0, 0.7))


# ---- the legs: thick, splayed out and down to points ---------------------------------------------------------------
LEGS = [  # bone, hip y, foot y
    ("leg", -0.06, -0.105),
    ("hind", 0.02, 0.025),
    ("hind{s}.001", 0.09, 0.12),
]


def legs(k):
    for side, sn in ((1, "R"), (-1, "L")):
        m = np.array([side, 1, 1.0])
        for i, (bone, hy, fy) in enumerate(LEGS):
            b = bone.format(s=sn) if "{s}" in bone else bone + sn
            P = [np.array(p) * m for p in [(0.125, hy, 0.15), (0.2, (hy + fy) * 0.5, 0.165),
                                           (0.245, fy, 0.08), (0.258, fy * 1.05, 0.006)]]
            k.add(f"leg{i}{sn}", outward(tube(P, [0.03, 0.027, 0.019, 0.006], seg=16, per=6)), LEG, b, fuse=False,
                  surface="shell")
            if i == 1:
                moss(k, f"mossleg{sn}", P[1] + np.array([0.012 * side, 0, 0.012]), 0.034, b, seed=11 + side,
                     flat=(1.0, 1.0, 0.75))


# ---- the eyes' looks ------------------------------------------------------------------------------------------------
CW, CP, CS = srgb(WHITE), srgb(PUPIL), srgb(SOCKET)
EYE_N = unit(0.3, -1, 0.42)
EYE_HALF = (0.025, 0.025)


def klawf_eye(u, v):
    c = np.tile(CW, (len(u), 1))
    c[((u - 0.08) / 0.5) ** 2 + ((v + 0.02) / 0.56) ** 2 < 1] = CP
    c[((u + 0.08) / 0.16) ** 2 + ((v - 0.2) / 0.16) ** 2 < 1] = CW
    return c


def lay(k):
    def eyes(state):
        k.decal(EYE_C + EYE_N * EYE_R * 0.98, EYE_N, EYE_HALF, eye_look(state, klawf_eye, CS, CP, line=0.18),
                eps=0.0025, rings=20, segs=56, mirror=True)
    return eyes


def klawf(src, out):
    k = Kit(src, coat="shell")
    body(k)
    eyes_and_sockets(k)
    lower_shell(k)
    face(k)
    top_moss(k)
    for side in (1, -1):
        claw(k, side)
    legs(k)
    k.build(voxel=0.005, smooth=3, band=0.006)
    k.eye_states(lay(k))
    k.export(out)


if __name__ == "__main__":
    src, out = args()
    klawf(src, out)
