# Annihilape sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/annihilape.py -- <the export's Annihilape.glb> <out.glb>
#
# Grown from mankey.py and primeape.py (their pig's snout with two nostrils, their anger mark of four arcs, their
# black iron cuffs: mankey's shackle), on a skeleton of its own (no head bone: the face is on the body, as Mankey's).
# From the references (they overrule the C# sculpt's purple ghost fire, glowing rings and cat's ears, none of which the
# pictures show): a tall shaggy grey mass that is body and head in one, darker toward its ragged bottom fringe; on top
# a great shock of white fur standing up in fat flame-like locks that curl outward at their tips, the largest mass of
# all; two long white locks hanging down over the face like a curtain, parting over a black band across the eyes with
# two furious red eyes in it, slanting down to the middle; a pink pig's snout under the band; a dark anger mark on the
# brow; white spiky tufts at the arm roots. Long thick slate-grey arms out of the sides, bent at the elbow, big hands
# low beside the knees; a black iron cuff on the right forearm; short bowed slate legs with big feet, a black cuff on
# each ankle. The art's thin floating wisps are left out (they would set the height and shrink the body); the locks'
# tips stand in for them.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, inside, smoothstep, unit, eye_look, polygon, outward, sibling, args

m = sibling("mankey")

GREY, DARKGREY, WHITE_FUR, SHADE = "#A4A4AE", "#6E6E78", "#ECECF2", "#C4C4CE"
SLATE, IRON, BAND, RED, PINK = "#4E5666", "#202024", "#26242C", "#E0303A", "#EC8AAE"
NOSTRIL, VEIN, INK = "#6A2E40", "#2E2D36", "#121014"

C, R = np.array([0, 0.0, 0.45]), np.array([0.245, 0.215, 0.265])     # the grey body and face


def body(k):
    def shag(V):
        th = np.arctan2(V[:, 1], V[:, 0]); ph = np.arccos(np.clip(V[:, 2], -1, 1))
        face = smoothstep(0.3, 0.7, -V[:, 1]) * smoothstep(-0.5, 0.0, V[:, 2])
        V = V * (1 + 0.04 * (1 - face) * np.abs(np.sin(9 * th) * np.sin(6 * ph)))[:, None]
        V[:, 2] = np.where(V[:, 2] < 0, V[:, 2] * 0.92, V[:, 2])
        return V
    k.add("body", ell(C, R, deform=shag), GREY, "body")
    # the ragged fringe round its bottom: shaggy points hanging down and out
    rs = np.random.RandomState(979)
    for i in range(26):
        a = 2 * math.pi * (i + 0.5) / 26 + rs.uniform(-0.08, 0.08)
        d = np.array([math.cos(a), math.sin(a), 0])
        base = C + d * R * np.array([0.82, 0.82, 0]) + np.array([0, 0, -0.17])
        L = rs.uniform(0.07, 0.1)
        tip = base + d * 0.04 + np.array([0, 0, -L])
        k.add(f"fringe{i}", tube([base, (base + tip) / 2 + d * 0.012, tip], [0.05, 0.03, 0.012]), GREY, "body")
    # white spiky tufts at the arm roots
    for j, (dy, dz) in enumerate(((-0.05, 0.05), (0.03, 0.07), (-0.01, -0.02))):
        base = np.array([0.215, dy, 0.5 + dz])
        tip = base + unit(1, dy * 2, 0.3 + dz * 3) * 0.1
        k.add(f"tuft{j}", tube([base, (base + tip) / 2, tip], [0.03, 0.017, 0.006]), WHITE_FUR, "body", mirror=True)
    # darker toward the bottom
    k.paint(lambda P: smoothstep(0.38, 0.22, P[:, 2]), DARKGREY, on=["body", "fringe"])


# --- the shock of white fur on top: a core and fat locks rising like flames, curling out at their tips ---------------
LOCKS = [  # (points, radii, bone)
    ([(0, 0.02, 0.84), (0, 0.0, 0.98), (-0.01, -0.02, 1.1), (0.03, -0.02, 1.18)], [0.12, 0.1, 0.06, 0.022], "body"),
    ([(0.1, 0.0, 0.84), (0.16, 0.0, 0.96), (0.22, 0.0, 1.05), (0.28, 0.01, 1.08)], [0.1, 0.085, 0.05, 0.02], "body"),
    ([(0.17, 0.02, 0.76), (0.27, 0.02, 0.85), (0.33, 0.03, 0.92), (0.38, 0.04, 0.93)], [0.095, 0.075, 0.045, 0.018],
     "ear{s}"),
    ([(0.2, 0.02, 0.66), (0.3, 0.03, 0.69), (0.36, 0.04, 0.75), (0.39, 0.05, 0.74)], [0.08, 0.06, 0.035, 0.017],
     "ear{s}"),
    ([(0.05, 0.14, 0.8), (0.07, 0.24, 0.92), (0.06, 0.29, 1.0), (0.1, 0.32, 1.02)], [0.1, 0.075, 0.045, 0.019], "body"),
]


def crown(k):
    k.add("crown", ell((0, 0.03, 0.8), (0.26, 0.23, 0.18), deform=m.fur(6, 4, 0.06)), WHITE_FUR, "body")
    for i, (pts, radii, bone) in enumerate(LOCKS):
        mirror = abs(pts[0][0]) > 1e-6
        k.add(f"lock{i}", tube(pts, radii), WHITE_FUR, bone, mirror=mirror)
    # a soft grey shade where the shock meets the body, as the pictures have it
    k.paint(lambda P: smoothstep(0.74, 0.66, P[:, 2]) * (P[:, 2] > 0.6), SHADE, on=["crown", "lock"])


def face_locks(k):
    """The two long locks hanging over the face like a curtain, parting over the band: loose, so no eye lands on
    them, and curling outward at their ends beside the snout."""
    radii = [0.062, 0.056, 0.046, 0.034, 0.02]
    xz = [(0.035, 0.78), (0.085, 0.68), (0.125, 0.59), (0.155, 0.51), (0.2, 0.47)]
    for s in (1, -1):
        pts = []
        for (x, z), r in zip(xz, radii):
            q = 1 - (x / R[0]) ** 2 - ((z - C[2]) / R[2]) ** 2
            y = -R[1] * math.sqrt(max(q, 0.05)) - r * 0.45     # lying close over the face, not hanging off it
            pts.append((s * x, y, z))
        k.add(f"facelock{'R' if s > 0 else 'L'}", outward(tube(pts, radii)), WHITE_FUR,
              "body", fuse=False, surface="hair")


# --- the face: the black band, the snout, the anger mark --------------------------------------------------------------
BZ = 0.555                     # the band's middle height


def face(k):
    def band(P):
        a = np.arctan2(P[:, 0], -P[:, 1]) * 0.22            # across the front, in metres
        dz = np.abs(P[:, 2] - BZ - 0.12 * a * a)
        d = np.maximum(np.abs(a) - 0.11, 0)
        return inside(np.sqrt(d * d + np.maximum(dz - 0.029, 0) ** 2) - 0.006, 0.0025) * (P[:, 1] < -0.1)
    k.paint(band, BAND)
    Sn = np.array([0, -0.205, 0.485])
    sr = np.array((0.058, 0.04, 0.044))
    k.add("snout", m.ell(Sn, sr, deform=m.boxy(2.8)), PINK, "body")
    front = Sn[1] - sr[1]
    for side in (1, -1):
        k.decal((side * sr[0] * 0.33, front, Sn[2] + 0.004), (side * 0.1, -1, 0), (0.01, 0.016), NOSTRIL, eps=0.003,
                rings=8, segs=32)
    # the anger mark: four dark arcs on the brow, off to one side, between the parted locks
    d = unit(0.0, -0.8, 0.62)
    at = C + d / math.sqrt(((d / R) ** 2).sum())
    V = srgb(VEIN)
    def mark(u, v):
        c = np.tile(srgb(GREY), (len(u), 1))
        for sx in (1, -1):
            for sy in (1, -1):
                dd = np.sqrt((u - sx * 1.0) ** 2 + (v - sy * 1.0) ** 2)
                c[(np.abs(dd - 0.72) < 0.26) & (u * sx > 0.1) & (v * sy > 0.1)] = V
        return c
    k.decal(at, d, (0.024, 0.024), mark, eps=0.003, rings=24, segs=72)


# --- the eyes: red and furious, slanting down to the middle, in the black band -----------------------------------------
LID, EINK = srgb(BAND), srgb("#8C8C96")


def eye(u, v):
    c = np.tile(LID, (len(u), 1))
    half = 0.62 * np.sqrt(np.clip(1 - u * u, 0, 1))
    inner = (np.abs(v - 0.3 * u + 0.05) < half) & (np.abs(u) < 0.95)
    c[inner] = srgb(RED)
    c[inner & ((u + 0.05) ** 2 / 0.05 + (v + 0.02) ** 2 / 0.12 < 1)] = srgb(INK)
    return c


def eyes(k, state):
    for s in (1, -1):
        d = unit(s * 0.28, -0.95, 0.06)
        at = C + d / math.sqrt(((d / R) ** 2).sum())
        at[2] = BZ + 0.004
        look = eye_look(state, eye, LID, EINK, line=0.17)
        col = look if s > 0 else (lambda u, v, look=look: look(-u, v))
        k.decal(at, d, (0.044, 0.025), col, eps=0.002, rings=20, segs=56)


# --- the limbs ------------------------------------------------------------------------------------------------------
def limbs(k):
    S, E, W = np.array([0.18, 0.0, 0.47]), np.array([0.3, -0.05, 0.42]), np.array([0.255, -0.16, 0.28])
    k.add("arm", tube([S, (S + E) / 2 + np.array([0, 0, 0.012]), E, (E + W) / 2, W], [0.062, 0.06, 0.056, 0.052,
                                                                                     0.05]), SLATE, "arm{s}",
          mirror=True)
    fw = unit(*(W - E))
    H = W + fw * 0.06
    k.add("palm", ell(H, (0.062, 0.058, 0.066), rot=(-20, 0, 0)), SLATE, "arm{s}", mirror=True)
    for j, dx in enumerate((-0.032, 0.0, 0.032)):
        p0 = H + np.array([dx, -0.03, -0.03])
        k.add(f"finger{j}", tube([p0, p0 + np.array([dx * 0.3, -0.035, -0.035]),
                                  p0 + np.array([dx * 0.4, -0.03, -0.075])], [0.024, 0.022, 0.019]), SLATE,
              "arm{s}", mirror=True)
    k.add("thumb", tube([H + np.array([-0.05, -0.02, 0.01]), H + np.array([-0.07, -0.06, -0.02])], [0.024, 0.02]),
          SLATE, "arm{s}", mirror=True)
    # the black iron cuff on the right forearm alone (+X, the viewer's right in the art)
    cuff_c = E + (W - E) * 0.55
    k.add("cuff", m.shackle(cuff_c, fw, 0.07, 0.085), IRON, "armR", fuse=False, surface="metal")
    # short bowed legs, big feet, a cuff on each ankle
    Hp, K, A = np.array([0.09, 0.0, 0.3]), np.array([0.145, -0.02, 0.17]), np.array([0.14, -0.03, 0.075])
    k.add("leg", tube([Hp, K, A], [0.064, 0.058, 0.052]), SLATE, "leg{s}", mirror=True)
    F = np.array([0.15, -0.075, 0.035])
    k.add("foot", ell(F, (0.065, 0.095, 0.038), rot=(0, 0, -10)), SLATE, "leg{s}", mirror=True)
    for j, dx in enumerate((-0.035, 0.0, 0.035)):
        k.add(f"toe{j}", ell(F + np.array([dx, -0.08, -0.004]), (0.024, 0.03, 0.028)), SLATE, "leg{s}", mirror=True)
    k.add("anklecuff", m.shackle(A + np.array([0, 0.005, 0.02]), unit(0.05, 0.1, 1), 0.066, 0.055), IRON, "leg{s}",
          fuse=False, mirror=True, surface="metal")


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="fur")
    body(k)
    crown(k)
    face_locks(k)
    limbs(k)
    face(k)
    k.build(voxel=0.008, smooth=4, band=0.006)
    k.eye_states(lambda s: eyes(k, s))
    k.export(out)
