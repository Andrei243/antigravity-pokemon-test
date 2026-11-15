# Claydol sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/claydol.py -- <the export's Claydol.glb> <out.glb>
#
# Baltoy grown (baltoy.py's frame, by sibling: its cones and the way it paints a ring and a band by distance), now of
# black clay. From the references: a wide head like an inverted bowl, wider than the body, rising at the top to a short
# blunt horn; round its widest part a ring of eight raised pink discs, each arched over by a white band whose scallops
# run all round the head, and a yellow fang hanging between each two; the front disc is the open eye with a small dark
# dot, the two beside it shut lines. Under it a round black body with two white rings on its front, each with a yellow
# middle and a short white tick below, and three short pointed stubs under it. Two round hands with a blunt point out
# to the side float apart from the body at its shoulders.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, args, hull, sibling

ba = sibling("baltoy")
cone = ba.cone
BLACK, PINK, WHITE, YELLOW, INK = "#3D3C42", "#EC6A8A", "#F2F0EC", "#F0CC5A", "#2A1A22"

HEAD_C, HEAD_R = np.array([0.0, 0.0, 0.47]), np.array([0.2, 0.172, 0.118])
BODY_C, BODY_R = np.array([0.0, 0.0, 0.3]), np.array([0.118, 0.105, 0.11])
EYE_Z = 0.468          # the discs' middles
DISC = 0.052           # a disc's radius
N_EYES = 8


def bowl(V):
    """The head: broad and round below, its top a dome drawn up toward the horn."""
    V = V.copy()
    top = np.clip(V[:, 2], 0, 1)
    V[:, 2] += 0.25 * top ** 4
    return V


def disc_frame(i):
    a = math.radians(360.0 * i / N_EYES)
    out = np.array([math.sin(a), -math.cos(a), 0.0])
    # where the head's surface is at that angle, at the discs' height
    zz = (EYE_Z - HEAD_C[2]) / HEAD_R[2]
    k = math.sqrt(max(1 - zz * zz, 0.05))
    rr = 1 / math.sqrt((out[0] / HEAD_R[0]) ** 2 + (out[1] / HEAD_R[1]) ** 2) * k
    p = HEAD_C + out * rr
    p[2] = EYE_Z
    n = unit(out[0] / HEAD_R[0] ** 2 * k, out[1] / HEAD_R[1] ** 2 * k, zz / HEAD_R[2] * 0.6)
    return p, n, out


def head(k):
    k.add("head", ell(HEAD_C, HEAD_R, deform=bowl), BLACK, "head")
    k.add("horn", tube([HEAD_C + (0, 0, 0.1), HEAD_C + (0, 0, 0.165), HEAD_C + (0, 0, 0.195)], [0.055, 0.036, 0.03]),
          BLACK, "head")
    for i in range(N_EYES):
        p, n, out = disc_frame(i)
        # a raised pink disc, flat in the direction it faces
        a = math.degrees(math.atan2(out[0], -out[1]))
        k.add(f"disc{i}", ell(p - n * 0.006, (DISC, 0.016, DISC * 0.95), rot=(0, 0, a)), PINK, "head")


def head_marks(k):
    """White arches over the discs, run together all round; the discs' pink kept crisp below them."""
    frames = [disc_frame(i) for i in range(N_EYES)]
    def white(P):
        m = np.zeros(len(P))
        for p, n, out in frames:
            d = P - p
            r = np.linalg.norm(d - np.outer(d @ n, n), axis=1)
            near = (d @ n) > -0.04
            arch = inside(np.abs(r - (DISC + 0.014)) - 0.011, 0.0025) * (P[:, 2] > EYE_Z - 0.004) * near
            m = np.maximum(m, arch)
        return m
    k.paint(white, WHITE)


def fangs(k):
    for i in range(N_EYES):
        a = math.radians(360.0 * (i + 0.5) / N_EYES)
        out = np.array([math.sin(a), -math.cos(a), 0.0])
        zz = (EYE_Z - 0.035 - HEAD_C[2]) / HEAD_R[2]
        kk = math.sqrt(max(1 - zz * zz, 0.05))
        rr = 1 / math.sqrt((out[0] / HEAD_R[0]) ** 2 + (out[1] / HEAD_R[1]) ** 2) * kk
        root = HEAD_C + out * (rr - 0.006)
        root[2] = EYE_Z - 0.028
        tip = root + out * 0.02 + np.array([0, 0, -0.058])
        k.add(f"fang{i}", cone(root, tip, 0.017, n=7, back=0.3), YELLOW, "head", fuse=False, surface="shell")


def body(k):
    k.add("body", ell(BODY_C, BODY_R), BLACK, "body")
    for i, a in enumerate((0, 120, 240)):
        t = math.radians(a)
        d = np.array([math.sin(t), -math.cos(t), 0.0])
        root = BODY_C + d * 0.06 + np.array([0, 0, -0.07])
        k.add(f"stub{i}", cone(root, root + d * 0.018 + np.array([0, 0, -0.07]), 0.03), BLACK, "body", fuse=False,
              surface="shell")


def hands(k):
    for s, sn in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        c = np.array([0.205, -0.02, 0.33]) * m
        k.add("hand" + sn, ell(c, (0.05, 0.048, 0.054)), BLACK, "arm" + sn, fuse=False, surface="shell")
        k.add("handpoint" + sn, cone(c + np.array([0.03 * s, 0, 0]), c + np.array([0.105 * s, -0.004, -0.012]),
                                     0.026), BLACK, "arm" + sn, fuse=False, surface="shell")
        k.add("handknob" + sn, ell(c + np.array([-0.035 * s, 0, -0.006]), (0.028, 0.03, 0.03), sub=3), BLACK,
              "arm" + sn, fuse=False, surface="shell")


# ---- the marks: patches on the fused surface -----------------------------------------------------------------------
CB, CW, CY, CP, CK = srgb(BLACK), srgb(WHITE), srgb(YELLOW), srgb(PINK), srgb(INK)


def body_ring(u, v):
    c = np.tile(CB, (len(u), 1))
    r = np.sqrt(u * u + (v - 0.15) ** 2)
    c[np.abs(r - 0.66) < 0.16] = CW
    c[r < 0.3] = CY
    c[(np.abs(u - 0.0) < 0.13) & (v < -0.45) & (v > -0.98)] = CW
    return c


def body_marks(k):
    k.decal(BODY_C + np.array([0.052, -0.09, 0.006]), unit(0.5, -1, 0.05), (0.047, 0.052), body_ring, eps=0.003,
            rings=20, segs=72, mirror=True)


def shut_line(u, v):
    c = np.tile(CP, (len(u), 1))
    c[(np.abs(v + 0.02) < 0.17) & (np.abs(u) < 0.58)] = CK
    return c


def side_eyes(k):
    for i in (1, N_EYES - 1):
        p, n, out = disc_frame(i)
        k.decal(p, n, (DISC * 0.95, DISC * 0.9), shut_line, eps=0.004, rings=20, segs=56)


def open_eye(u, v):
    c = np.tile(CP, (len(u), 1))
    c[u * u + (v - 0.02) ** 2 < 0.21 ** 2] = CK
    return c


def front_eye(k):
    p, n, out = disc_frame(0)
    def lay(state):
        look = eye_look(state, open_eye, CP, CK, line=0.12)
        k.decal(p, n, (DISC * 0.95, DISC * 0.9), look, eps=0.004, rings=20, segs=56)
    return lay


def claydol(src, out):
    k = Kit(src, coat="shell")
    head(k)
    head_marks(k)
    fangs(k)
    body(k)
    hands(k)
    body_marks(k)
    side_eyes(k)
    k.build(voxel=0.006, smooth=3, band=0.005)
    k.eye_states(front_eye(k))
    k.export(out)


if __name__ == "__main__":
    src, out = args()
    claydol(src, out)
