# Crabrawler sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/crabrawler.py -- <the export's Crabrawler.glb> <out.glb>
#
# From the references: a boxing crab. A round purple body, a little narrower at the top, the face on its upper front with
# no head of its own: two small white eyes looking up, each under a thick blue brow, and a raised mouth plate of two dark
# lines running down from between them onto a cream bib. The bib is a tall rounded cream plate down the front with a few
# dark segment lines across it; a round purple boss either side of it; a blue stripe round the lower body. On the crown
# a tall curved cream horn (its tuft) rising from a cream cap. Two short purple arms, a cream cuff at the elbow, ending in
# huge cyan boxing gloves, each nearly as big as the body (the largest masses with it), held forward and a little low.
# Four short purple legs, two a side, splayed out to pointed cyan tips. (The pictures overrule the task's text: the
# "tuft of yellow hair" is one pale cream horn, and the "blue antenna-like eyes" are blue brows over small white eyes.)
#
# Reference ratios (3D picture): horn above the crown / body height 0.7; a glove across / body width 0.92; body width /
# body height 0.8; the eyes' middle at 0.82 of the body's height.
#
# Written to be grown from (crabominable.py imports it with sibling): the frame is in the constants and the functions
# that take the kit; the build is under the main guard. Bones: root, body, arm{s}, leg{s}, hind{s}.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, args, outward, hull, ring_band, polygon

PURPLE, DEEP, CREAM, CYAN = "#8A44C2", "#4A2470", "#F2E8A8", "#22B6E2"
STRIPE, WHITE, INK, PUPIL = "#2A9AD2", "#FAFAF6", "#2A1E30", "#141018"

X = np.array([1.0, 0, 0]); Z = np.array([0, 0, 1.0])

# ---- the body: a round ball a little narrower at the top ------------------------------------------------------------
BODY_C, BODY_R = np.array([0.0, 0.0, 0.112]), np.array([0.07, 0.064, 0.074])
FACE_C, FACE_R = np.array([0.0, -0.016, 0.158]), np.array([0.05, 0.046, 0.036])


def egg(V):
    V = V.copy()
    t = np.clip((V[:, 2] + 1) / 2, 0, 1)
    s = 1.0 - 0.14 * t ** 1.8
    V[:, 0] *= s; V[:, 1] *= s
    return V


def body(k, c=BODY_C, r=BODY_R, face_c=FACE_C, face_r=FACE_R, color=PURPLE, bosses=0.028):
    k.add("body", ell(c, r, deform=egg), color, "body")
    k.add("face", ell(face_c, face_r), color, "body")
    k.add("under", ell(c + (0, -0.004, -r[2] * 0.4), r * (0.95, 0.94, 0.6)), color, "body")
    if bosses:
        k.add("boss", ell(c + np.array((0.056, -0.036, 0.008)) * r / BODY_R, (bosses, bosses * 0.9, bosses)), color,
              "body", mirror=True)


def phi(P, c=BODY_C):
    return np.arctan2(P[:, 0] - c[0], -(P[:, 1] - c[1]))


def stripe(k, z0=0.056, z1=0.068, c=BODY_C, reach=0.085, color=STRIPE):
    """A band round the lower body (not the legs or bosses: kept to the body's own radius)."""
    def mask(P):
        x, y, z = P[:, 0] - c[0], P[:, 1] - c[1], P[:, 2]
        return inside(z - z1, 0.0018) * inside(z0 - z, 0.0018) * inside(np.hypot(x, y) - reach, 0.004)
    k.paint(mask, color)


# ---- the cream bib down the front with its segment lines ------------------------------------------------------------
BIB = [(-0.015, 0.152), (0.015, 0.152), (0.027, 0.14), (0.034, 0.118), (0.032, 0.095), (0.022, 0.078), (0.0, 0.07),
       (-0.022, 0.078), (-0.032, 0.095), (-0.034, 0.118), (-0.027, 0.14)]
BIB_LINES = (0.132, 0.114, 0.096)


def in_poly(x, y, pts):
    pts = np.array(pts, float)
    res = np.zeros(len(x), bool)
    for (x1, y1), (x2, y2) in zip(pts, np.roll(pts, -1, axis=0)):
        res ^= ((y1 > y) != (y2 > y)) & (x < (x2 - x1) * (y - y1) / (y2 - y1 + 1e-12) + x1)
    return res


def poly_mask(x, y, pts, edge=0.002):
    pts = np.array(pts, float)
    d = np.full(len(x), 9.0)
    for (x1, y1), (x2, y2) in zip(pts, np.roll(pts, -1, axis=0)):
        ex, ey = x2 - x1, y2 - y1
        t = np.clip(((x - x1) * ex + (y - y1) * ey) / (ex * ex + ey * ey + 1e-12), 0, 1)
        d = np.minimum(d, np.hypot(x - x1 - t * ex, y - y1 - t * ey))
    return inside(np.where(in_poly(x, y, pts), -d, d), edge)


def bib(k, pts=BIB, lines=BIB_LINES, color=CREAM, line_color=DEEP, front=-0.03, width=0.0024):
    def mask(P):
        return poly_mask(P[:, 0], P[:, 2], pts, 0.0018) * (P[:, 1] < front)
    k.paint(mask, color)
    def lmask(P):
        x, y, z = P[:, 0], P[:, 1], P[:, 2]
        out_ = np.zeros(len(P))
        for z0 in lines:
            curve = z0 + 0.006 * (x / 0.04) ** 2
            out_ = np.maximum(out_, inside(np.abs(z - curve) - width, 0.0012))
        return out_ * poly_mask(x, z, pts, 0.0) * (y < front) * inside(np.abs(x) - 0.028, 0.002)
    k.paint(lmask, line_color)


# ---- the horn on the crown: a curved cream cone out of a cream cap ----------------------------------------------------
HORN = [(0.0, -0.006, 0.172), (0.0, 0.0, 0.215), (0.004, 0.012, 0.255), (0.008, 0.026, 0.282)]
HORN_R = [0.021, 0.016, 0.009, 0.002]
CAP_C, CAP_R = np.array([0.0, -0.004, 0.178]), np.array([0.034, 0.03, 0.014])


def horn(k, pts=HORN, radii=HORN_R, cap_c=CAP_C, cap_r=CAP_R, color=CREAM, bone="body"):
    k.add("cap", outward(ell(cap_c, cap_r)), color, bone, fuse=False, surface="shell")
    k.add("horn", outward(tube(pts, radii, seg=16, per=6)), color, bone, fuse=False, surface="shell")


# ---- limbs: loose purple tubes; a pointed tip is a cone hull ---------------------------------------------------------
def cone(root, tip, r, n=7):
    root, tip = np.array(root, float), np.array(tip, float)
    d = unit(*(tip - root))
    a = np.cross(d, Z if abs(d[2]) < 0.9 else X); a /= np.linalg.norm(a)
    b = np.cross(d, a)
    pts = [root + r * (math.cos(t) * a + math.sin(t) * b) for t in np.linspace(0, 2 * math.pi, n, endpoint=False)]
    pts.append(root - d * r * 0.5)
    pts.append(tip)
    return hull(pts, merge=0)


LEGS = [  # bone, hip (inside the body), knee, ankle; tip (the cyan point) (+X)
    ("leg{s}", (0.05, -0.022, 0.07), (0.09, -0.044, 0.078), (0.106, -0.058, 0.03), (0.109, -0.062, 0.0)),
    ("hind{s}", (0.05, 0.028, 0.07), (0.09, 0.048, 0.078), (0.106, 0.062, 0.03), (0.109, 0.066, 0.0)),
]


def legs(k, specs=LEGS, r=(0.0145, 0.0145, 0.0125), tip_r=0.0108, color=PURPLE, tip_color=CYAN, scale=1.0, surface="shell"):
    for i, (bone, hip, knee, ankle, tip) in enumerate(specs):
        for side, sn in ((1, "R"), (-1, "L")):
            m = np.array([side, 1, 1.0]) * scale
            P = [np.array(p) * m for p in (hip, knee, ankle)]
            b = bone.format(s=sn)
            k.add(f"leg{i}{sn}", outward(tube(P, np.array(r) * scale, seg=14, per=6)), color, b, fuse=False,
                  surface=surface)
            k.add(f"leg{i}{sn}knee", outward(ell(P[1], np.array((r[0], r[0], r[0])) * 1.18 * scale, sub=2)), color, b,
                  fuse=False, surface=surface)
            k.add(f"leg{i}{sn}tip", cone(P[2] - unit(*(np.array(tip) * m - P[2])) * 0.008 * scale,
                                         np.array(tip) * m, tip_r * scale), tip_color, b, fuse=False, surface=surface)


# ---- the arms and the gloves ----------------------------------------------------------------------------------------
ARM = [(0.055, -0.02, 0.122), (0.088, -0.055, 0.112), (0.097, -0.088, 0.104)]
ARM_R = [0.017, 0.016, 0.017]
GLOVE_C, GLOVE_R = np.array([0.104, -0.128, 0.1]), np.array([0.05, 0.056, 0.057])


def glove(k, side, c=GLOVE_C, r=GLOVE_R, color=CYAN, name="glove"):
    """A boxing glove: a big oval, a fat thumb lobe on its inner upper side, a dark seam line between them."""
    sn = "R" if side > 0 else "L"
    m = np.array([side, 1, 1.0])
    c = np.array(c, float) * m
    k.add(name + sn, outward(ell(c, r, rot=(-10, 0, 0))), color, "arm" + sn, fuse=False, surface="shell")
    th = c + np.array((-0.03 * side, -0.026, 0.026)) * r / GLOVE_R
    k.add(name + sn + "thumb", outward(ell(th, r * (0.5, 0.56, 0.54), rot=(-10, 0, -20 * side))), color, "arm" + sn,
          fuse=False, surface="shell")


def arms(k, arm=ARM, arm_r=ARM_R, color=PURPLE, cuff=CREAM, glove_c=GLOVE_C, glove_r=GLOVE_R, glove_color=CYAN,
         bone_scale=1.0):
    for side, sn in ((1, "R"), (-1, "L")):
        m = np.array([side, 1, 1.0])
        P = [np.array(p) * m for p in arm]
        k.add("arm" + sn, outward(tube(P, arm_r, seg=14, per=6)), color, "arm" + sn, fuse=False, surface="shell")
        d = unit(*(P[2] - P[0]))
        k.add("cuff" + sn, ring_band(P[1], d, arm_r[1] + 0.0035 * bone_scale, 0.012 * bone_scale,
                                     thick=0.006 * bone_scale), cuff, "arm" + sn, fuse=False, surface="shell")
        glove(k, side, glove_c, glove_r, glove_color)


# ---- the face: small white eyes looking up, blue brows, the mouth plate's two lines ---------------------------------
CW, CK, CP, CPU = srgb(WHITE), srgb(INK), srgb(PUPIL), srgb(PURPLE)
EYE_C = np.array([0.021, -0.058, 0.166])
EYE_N = unit(0.38, -1, 0.18)
EYE_HALF = (0.0145, 0.0155)


def open_eye(u, v, white=CW, rim=CK, pupil=CP, at=(-0.18, 0.34), size=0.4, ring=0.84):
    c = np.tile(rim, (len(u), 1))
    c[u * u + v * v < ring * ring] = white
    c[((u - at[0]) / size) ** 2 + ((v - at[1]) / (size * 1.08)) ** 2 < 1] = pupil
    return c


def brows(k, c=EYE_C, n=EYE_N, half=(0.016, 0.0055), lift=0.017, color=CYAN, slant=0.28, eps=0.0022):
    """A thick blue brow over each eye, falling toward the middle (in every state, so laid before the build)."""
    k.decal(c + (0.002, 0, lift), n, half, color, up=unit(slant, 0, 1), eps=eps, rings=8, segs=40, mirror=True)


def mouth(k, at=(0.0, -0.06, 0.142), lines=0.0055, half=(0.0032, 0.015), color=DEEP, eps=0.0022):
    for x in (-lines, lines):
        k.decal((at[0] + x, at[1], at[2]), unit(0, -1, 0.15), half, color, eps=eps, rings=6, segs=24)


def eyes(k, c=EYE_C, n=EYE_N, half=EYE_HALF, look=open_eye, lid=CPU, eps=0.0022):
    def lay(state):
        k.decal(c, n, half, eye_look(state, look, lid, CK, line=0.18), eps=eps, rings=20, segs=56, mirror=True)
    return lay


def crabrawler(src, out_path):
    k = Kit(src, coat="shell")
    body(k)
    horn(k)
    stripe(k)
    bib(k)
    legs(k)
    arms(k)
    brows(k)
    mouth(k)
    k.build(voxel=0.0028, smooth=3, band=0.004, wband=0.03)
    k.eye_states(eyes(k))
    k.export(out_path)


if __name__ == "__main__":
    src, out = args()
    crabrawler(src, out)
