# Corphish sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/corphish.py -- <the export's Corphish.glb> <out.glb>
#
# From the references: a small crayfish standing up. An upright egg of a body, orange over its top, sides and back;
# its front a cream shield whose top rises to a point between the eyes and curves down at the sides, with a smile line
# and two curved segment lines across it; cream underneath. Three orange spikes on the crown, the middle one tallest,
# the two beside it leaning out. Two big round white eyes with a thin dark rim and a small black pupil, on bumps at the
# upper sides of the face. Two big pincers held up at the sides as tall rounded mittens, cream below and an orange cap
# over the top with a jagged edge, on short thin grey arms. Six short orange legs a side pair each as arches with
# pointed feet; a short segmented tail with a fan behind, low.
#
# Written to be grown from (crawdaunt.py imports it with sibling): the frame is in the constants and the functions that
# take the kit; the build is under the main guard. Bones: root, body, head, arm{s}, leg{s}, hind{s}, tail.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, args, outward, hull, slab, tri
from mathutils import Euler

ORANGE, CREAM, LINE, ARMGREY = "#EC6438", "#E8DEC8", "#9A8A78", "#CFC9C2"
WHITE, INK, PUPIL = "#FAFAF6", "#2A1E1A", "#141014"

X = np.array([1.0, 0, 0]); Z = np.array([0, 0, 1.0])


def rotm(rot):
    return np.array(Euler([math.radians(a) for a in rot]).to_matrix())


# ---- the body: an upright egg, narrower at the top, the head's part on the head bone --------------------------------
BODY_C, BODY_R = np.array([0.0, 0.0, 0.19]), np.array([0.09, 0.085, 0.14])


def egg(V):
    V = V.copy()
    t = np.clip((V[:, 2] + 1) / 2, 0, 1)            # 0 at the bottom, 1 at the top
    s = 1.0 - 0.22 * t ** 1.6
    V[:, 0] *= s; V[:, 1] *= s
    return V


def body(k, c=BODY_C, r=BODY_R, color=ORANGE):
    k.add("body", ell(c, r, deform=egg), color, "body")
    k.add("headpart", ell(c + (0, -0.006, r[2] * 0.42), r * (0.78, 0.76, 0.5), deform=egg), color, "head")
    k.add("belly", ell(c + (0, -0.012, -r[2] * 0.45), r * (0.95, 0.92, 0.55)), color, "body")


# ---- the cream front shield and its lines -------------------------------------------------------------------------
def phi(P, c=BODY_C):
    return np.arctan2(P[:, 0] - c[0], -(P[:, 1] - c[1]))


def shield(k, top=0.222, side_drop=0.09, half=1.0, under=0.088, lines=(0.178, 0.135, 0.095), rise=0.028,
           color=CREAM, line_color=LINE, c=BODY_C, on=None):
    """The cream front: above it orange; its top at top in the middle, falling by side_drop at |phi| = half (radians
    round the body's upright axis from straight ahead); cream all round under `under`. lines: segment lines' heights
    in the middle, each rising by `rise` at the shield's sides (a smile)."""
    def mask(P):
        a = np.abs(phi(P, c))
        z = P[:, 2]
        line = top - side_drop * np.clip(a / half, 0, 1.4) ** 0.85
        m = inside(a - half, 0.003) * inside(z - line, 0.0025)
        return np.maximum(m, inside(z - under, 0.004))
    k.paint(mask, color, on=on)

    def lmask(P):
        a = np.abs(phi(P, c))
        z = P[:, 2]
        out = np.zeros(len(P))
        for z0 in lines:
            curve = z0 + rise * np.clip(a / half, 0, 1) ** 2
            out = np.maximum(out, inside(np.abs(z - curve) - 0.0032, 0.0018) * inside(a - half * 0.92, 0.004))
        return out
    k.paint(lmask, line_color, on=on)


# ---- the spikes on the crown: solid cones -------------------------------------------------------------------------
def cone(root, tip, r, n=7):
    root, tip = np.array(root, float), np.array(tip, float)
    d = unit(*(tip - root))
    a = np.cross(d, Z if abs(d[2]) < 0.9 else X); a /= np.linalg.norm(a)
    b = np.cross(d, a)
    pts = [root + r * (math.cos(t) * a + math.sin(t) * b) for t in np.linspace(0, 2 * math.pi, n, endpoint=False)]
    pts.append(root - d * r * 0.6)
    pts.append(tip)
    return hull(pts, merge=0)


CORPHISH_SPIKES = [  # root, tip, root radius; +X side's (mirrored where x != 0)
    ((0.0, -0.012, 0.31), (0.0, -0.03, 0.405), 0.024),
    ((0.04, -0.018, 0.295), (0.098, -0.035, 0.37), 0.02),
]


def spikes(k, specs=CORPHISH_SPIKES, color=ORANGE):
    for i, (root, tip, r) in enumerate(specs):
        if abs(root[0]) < 1e-6:
            k.add(f"spike{i}", cone(root, tip, r), color, "head", fuse=False, surface="shell")
        else:
            k.add(f"spike{i}", cone(root, tip, r), color, "head", fuse=False, surface="shell", mirror=True)


# ---- the pincers: a tall rounded mitten, cream with an orange cap whose lower edge is jagged ---------------------
CORPHISH_CLAW = dict(centre=(0.152, -0.06, 0.252), r=(0.056, 0.044, 0.08), tilt=14.0, cap=0.004, teeth=4.0,
                     tooth=0.2)
CORPHISH_ARM = [(0.075, -0.025, 0.2), (0.105, -0.045, 0.2), (0.125, -0.055, 0.205)]


def claw(k, side, centre, r, tilt, cap, teeth, tooth, color=ORANGE, under=CREAM, name="claw", deform=None):
    sn = "R" if side > 0 else "L"
    c = np.array(centre, float) * np.array([side, 1, 1])
    rot = (0, tilt * side, 0)
    k.add(name + sn, ell(c, r, rot=rot, deform=deform), under, "arm" + sn)
    M = rotm(rot)
    def mask(P):
        L = (P - c) @ M            # into the claw's own frame
        a = np.arctan2(L[:, 1], L[:, 0] * side)
        edge = cap + tooth * r[2] * tri(a * teeth / (2 * math.pi))
        return inside(edge - L[:, 2], 0.0025)
    k.paint(mask, color, on=[name + sn])


def arms(k, arm=CORPHISH_ARM, claws=(CORPHISH_CLAW, CORPHISH_CLAW), color=ARMGREY, r=(0.0165, 0.0155, 0.015)):
    for side, spec in ((1, claws[0]), (-1, claws[1])):
        sn = "R" if side > 0 else "L"
        m = np.array([side, 1, 1.0])
        k.add("arm" + sn, outward(tube([np.array(p) * m for p in arm], r, seg=14, per=6)), color, "arm" + sn,
              fuse=False, surface="shell")
        claw(k, side, **spec)


# ---- the legs: short fat curved hooks from the body's underside, tips on the ground close to the body -----------
CORPHISH_LEGS = [  # bone, the hook's points from the underside to the tip (+X)
    ("leg{s}", [(0.045, -0.05, 0.072), (0.08, -0.066, 0.078), (0.102, -0.078, 0.046), (0.1, -0.082, 0.006)]),
    ("hind{s}", [(0.05, 0.0, 0.068), (0.088, -0.002, 0.074), (0.11, -0.004, 0.044), (0.108, -0.004, 0.006)]),
    ("hind{s}", [(0.045, 0.048, 0.072), (0.08, 0.062, 0.078), (0.1, 0.074, 0.046), (0.098, 0.078, 0.006)]),
]


def legs(k, specs=CORPHISH_LEGS, r=(0.026, 0.025, 0.018, 0.006), color=ORANGE):
    for i, (bone, pts) in enumerate(specs):
        k.add(f"leg{i}", outward(tube(pts, r, seg=14, per=6)), color, bone, fuse=False, surface="shell", mirror=True)


# ---- the tail: a few segments going back and low, a fan at the end, turned out to +X ---------------------------
def tail(k, base=(0.0, 0.07, 0.1), d=(0.45, 0.85, -0.25), n=3, r0=0.045, step=0.032, fan=0.05, color=ORANGE):
    base = np.array(base, float); d = unit(*d)
    p = base
    for i in range(n):
        rr = r0 * (1 - 0.18 * i)
        k.add(f"tail{i}", ell(p, (rr, rr * 0.9, rr * 0.7)), color, "tail")
        p = p + d * step
    # the fan: a flat plate of three lobes, lying back and up
    ex = unit(-d[1], d[0], 0)
    ez = unit(d[0] * 0.8, d[1] * 0.8, 0.6)
    outline = [(-0.6, 0), (-1.0, 0.7), (-0.45, 1.0), (0, 0.75), (0.45, 1.0), (1.0, 0.7), (0.6, 0)]
    outline = [(a * fan, b * fan * 1.1) for a, b in outline]
    k.add("tailfan", slab(outline, p - d * 0.01, ex, ez, 0.024), color, "tail", fuse=False, surface="shell")


# ---- the eyes: big white eyes with a thin dark rim and a small dark pupil, on bumps at the upper sides -----------
EYE_C = np.array([0.054, -0.064, 0.254])
EYE_N = unit(0.6, -1, 0.1)
EYE_HALF = (0.028, 0.029)


def eye_bumps(k, c=EYE_C, r=0.033, color=ORANGE):
    k.add("eyebump", ell(c - EYE_N * 0.014, (r, r * 0.7, r), rot=(0, 0, 31)), color, "head", mirror=True)


def open_eye(u, v, white=WHITE, rim=INK, pupil=PUPIL, size=0.3, at=(-0.12, 0.02)):
    CW, CK, CP = srgb(white), srgb(rim), srgb(pupil)
    c = np.tile(CK, (len(u), 1))
    rr = np.sqrt(u * u + v * v)
    c[rr < 0.88] = CW
    c[((u - at[0]) / size) ** 2 + ((v - at[1]) / (size * 1.05)) ** 2 < 1] = CP
    return c


def eyes(k, c=EYE_C, n=EYE_N, half=EYE_HALF, look=open_eye, lid=ORANGE):
    CL, CK = srgb(lid), srgb(INK)
    def lay(state):
        k.decal(c, n, half, eye_look(state, look, CL, CK, line=0.16), eps=0.003, rings=20, segs=56, mirror=True)
    return lay


def corphish(src, out):
    k = Kit(src, coat="shell")
    body(k)
    eye_bumps(k)
    shield(k)
    spikes(k)
    arms(k)
    legs(k)
    tail(k, base=(0.0, 0.07, 0.115), d=(0.5, 0.85, -0.1), n=3, r0=0.045, step=0.034, fan=0.064)
    k.build(voxel=0.0045, smooth=3, band=0.006)
    k.eye_states(eyes(k))
    k.export(out)


if __name__ == "__main__":
    src, out = args()
    corphish(src, out)
