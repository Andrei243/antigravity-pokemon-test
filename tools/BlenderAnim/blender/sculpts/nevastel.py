# Nevastel (Carpathia No. 10051) sculpted from nothing on the skeleton and clips of the game's own export of it
# (sculptkit.py):
#   blender --background --factory-startup --python sculpts/nevastel.py -- <the export's Nevastel.glb> <out.glb>
# Written to be grown from: nevastuica.py imports its frame (sibling("nevastel")) and builds the same body at its own
# scale (its joints are this skeleton's times 1.45 exactly) with a veil, a wreath and one paw raised.
#
# The design, from the project's picture (docs/art/reference/gemini/carpathia/10051-Nevastel.jpg): a young weasel
# standing up on its hind legs; a long supple body, fullest low at the hips, narrowing up a thick neck to a wedge of a
# head carried a little forward; warm rust-brown above, a clean cream-white throat and belly from the chin down between
# the legs, the lower face white under the eyes round the snout; small round ears with a darker lining, a little tuft
# of three points on the crown; bold black eyes with a glint, a small dark nose on the snout's tip and a little smile;
# short forearms held before the chest with dark paws; large flat dark feet; a long tail out behind in an S, ending in a
# bushy dark brown tip.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from sculptkit import Kit, ell, tube, blade, hull, srgb, inside, unit, tri, eye_look, args

BROWN, DARK, WHITE, LINING = "#B65F2E", "#6A3720", "#F5EDDB", "#7E4026"
INK, GLINT, NOSE, MOUTH = "#17100D", "#FFFFFF", "#4E281A", "#5A2E22"


# --- helpers ---------------------------------------------------------------------------------------------------------
def hit(shape, o, d):
    """Where a ray from o along d first meets a part's own mesh: a place on a part before the body is fused."""
    V, F = shape
    h = BVHTree.FromPolygons(V.tolist(), F).ray_cast(Vector(o), Vector(unit(*d)))
    return np.array(h[0])


def spline(pts, per=40):
    p = np.array(pts, float)
    e = np.vstack([2 * p[0] - p[1], p, 2 * p[-1] - p[-2]])
    out_ = []
    for i in range(len(p) - 1):
        p0, p1, p2, p3 = e[i], e[i + 1], e[i + 2], e[i + 3]
        for j in range(per):
            t = j / per
            out_.append(0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t
                               + (-p0 + 3 * p1 - 3 * p2 + p3) * t ** 3))
    out_.append(p[-1])
    return np.array(out_)


def share_along(line):
    """For points Q, the share of the way along a dense line (0..1) of their nearest sample."""
    L = np.array(line)
    cum = np.concatenate([[0], np.cumsum(np.linalg.norm(np.diff(L, axis=0), axis=1))])
    cum /= cum[-1]
    def f(Q):
        res = []
        for a in range(0, len(Q), 3000):
            res.append(cum[((Q[a:a + 3000, None, :] - L[None]) ** 2).sum(2).argmin(1)])
        return np.concatenate(res)
    return f


def spike(k, name, base, d, length, width, bone, color, surface="fur"):
    """A small hard point (a tuft of fur): a flat-faced wedge from base along d."""
    base, d = np.array(base, float), unit(*d)
    a = np.cross(d, (0, 1.0, 0))
    if np.linalg.norm(a) < 1e-3:
        a = np.array([1.0, 0, 0])
    a /= np.linalg.norm(a)
    b = np.cross(a, d)
    k.add(name, hull([base + a * width + b * width * 0.6, base - a * width + b * width * 0.6,
                      base + a * width - b * width * 0.6, base - a * width - b * width * 0.6,
                      base - d * width * 0.5, base + d * length]), color, bone, fuse=False, surface=surface)


def black_eye(u, v):
    """A solid black eye with a white glint high on its outer side (u outward, v up)."""
    c = np.tile(srgb(INK), (len(u), 1))
    c[((u - 0.3) / 0.3) ** 2 + ((v - 0.34) / 0.3) ** 2 < 1] = srgb(GLINT)
    c[((u + 0.28) / 0.13) ** 2 + ((v + 0.3) / 0.13) ** 2 < 1] = srgb(GLINT)
    return c


def smile(u, v):
    c = np.tile(srgb(WHITE), (len(u), 1))
    c[(np.abs(v - (-0.3 + 0.95 * u * u)) < 0.32) & (np.abs(u) < 0.86)] = srgb(MOUTH)
    return c


def board(ob):
    """The game's board: a square of 1.3 x max(height, 0.85 x max(width, length)); four corners show 0.853 w + 0.522 l."""
    V = np.array([v.co[:] for v in ob.data.vertices])
    h, w, l = V[:, 2].max(), np.ptp(V[:, 0]), np.ptp(V[:, 1])
    sq = 1.3 * max(h, 0.85 * max(w, l))
    s = 0.853 * w + 0.522 * l
    print(f"BOARD: height {h:.3f} width {w:.3f} length {l:.3f}; corners {s:.3f} of square {sq:.3f} = {s / sq:.2f}")


# --- the weasel ------------------------------------------------------------------------------------------------------
# Everything is written for Nevastel's skeleton and multiplied by K (Nevastuica's is the same times 1.45).
SPINE = [(0, 0.012, 0.07), (0, -0.004, 0.165), (0, -0.022, 0.26), (0, -0.038, 0.345), (0, -0.05, 0.42)]
SPR = [0.07, 0.062, 0.052, 0.045, 0.04]
TAIL = [(0, 0.04, 0.1), (0.035, 0.11, 0.06), (0.085, 0.175, 0.075), (0.12, 0.215, 0.14), (0.13, 0.215, 0.215),
        (0.12, 0.19, 0.28)]
TR = [0.028, 0.027, 0.027, 0.029, 0.033, 0.036]
HC = (0, -0.066, 0.482)                 # the head's middle (its bone at 0, -0.04, 0.42)
HRAD = (0.07, 0.07, 0.059)


def weasel(src, out, K=1.0, extras=None, raise_right=False):
    k = Kit(src, coat="fur")
    P = lambda *a: np.array(a, float) * K
    H, HR = P(*HC), P(*HRAD)

    # The long body standing up: fullest at the hips, up a thick neck into the head
    k.add("body", tube([P(*p) for p in SPINE], [r * K for r in SPR], seg=24), BROWN, "body")
    k.add("belly", ell(P(0, -0.012, 0.13), P(0.064, 0.062, 0.075)), BROWN, "body")
    k.add("neck", tube([P(0, -0.04, 0.36), P(0, -0.052, 0.43), H + P(0, 0.01, -0.02)], [0.043 * K, 0.04 * K, 0.04 * K]),
          BROWN, "head")

    # The head: a wedge, broad behind and narrowing to a snout
    def head_shape(V):
        V = V.copy()
        V[:, 0] *= 1 + 0.12 * np.clip(V[:, 1], 0, 1)
        return V
    HEAD = ell(H, HR, deform=head_shape)
    k.add("head", HEAD, BROWN, "head")
    SNOUT = ell(H + P(0, -0.062, -0.02), P(0.035, 0.05, 0.03))
    k.add("snout", SNOUT, BROWN, "head")

    # Small round ears high on the head's sides, a darker lining in each
    ear_c, ear_r = H + P(0.046, 0.012, 0.036), P(0.024, 0.013, 0.023)
    k.add("ear", ell(ear_c, ear_r, rot=(0, -18, 0)), BROWN, "ear{s}", mirror=True)
    en = unit(0.35, -1, 0.15)
    k.decal(hit(ell(ear_c, ear_r, rot=(0, -18, 0)), ear_c + en * 0.3, -en), en, (0.013 * K, 0.013 * K), LINING,
            eps=0.003 * K, rings=10, segs=48, mirror=True)
    # The little tuft of three points on the crown, swept back
    for i, (x, d) in enumerate(((0.0, (0, 0.45, 1)), (0.014, (0.35, 0.6, 1)), (-0.014, (-0.35, 0.6, 1)))):
        spike(k, f"tuft{i}", H + P(x, 0.006, HRAD[2] - 0.012), d, 0.022 * K, 0.0085 * K, "head", BROWN)

    # Short forearms held before the chest, dark paws; one may be raised to the chin
    for s, nm in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        if raise_right and s > 0:
            pts = [P(0.033, -0.05, 0.335), P(0.05, -0.088, 0.36), P(0.03, -0.112, 0.4)]
            paw = P(0.022, -0.118, 0.412)
        else:
            pts = [P(0.033, -0.05, 0.335), P(0.047, -0.09, 0.3), P(0.04, -0.106, 0.272)]
            paw = P(0.039, -0.112, 0.258)
        k.add("arm" + nm, tube([p * m for p in pts], [0.02 * K, 0.018 * K, 0.016 * K]), BROWN, "arm" + nm)
        k.add("paw" + nm, ell(paw * m, P(0.018, 0.02, 0.021)), DARK, "arm" + nm)

    # Hind legs: the thighs fused into the hips, short shins, large flat dark feet pointing forward
    for s, nm in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add("thigh" + nm, ell(P(0.04, 0.0, 0.085) * m, P(0.036, 0.052, 0.058)), BROWN, "leg" + nm)
        k.add("shin" + nm, tube([P(0.042, -0.002, 0.06) * m, P(0.043, -0.016, 0.026) * m], [0.025 * K, 0.021 * K]),
              BROWN, "leg" + nm)
        k.add("foot" + nm, ell(P(0.044, -0.042, 0.015) * m, P(0.024, 0.05, 0.015), rot=(0, 0, 8 * s)), DARK,
              "leg" + nm)

    # The tail: down behind the hips, out and back toward +X in an S, up into a bushy dark brown tip
    tp = [P(*p) for p in TAIL]
    k.add("tail", tube(tp, [r * K for r in TR], seg=20), BROWN, "tail")
    tip = tp[-1]
    # the brush: swelling past the tail's end and tapering to a point, ragged tufts swept along it
    td = unit(*(tp[-1] - tp[-2]))
    brush = [tip, tip + td * 0.035 * K, tip + td * 0.075 * K]
    k.add("tailbrush", tube(brush, [0.04 * K, 0.044 * K, 0.014 * K], seg=20), BROWN, "tail")
    side = unit(*np.cross(td, (0, 1.0, 0)))
    back = unit(*np.cross(side, td))
    for i, (a, b, at) in enumerate(((1, 0.2, 0.3), (-1, 0.3, 0.45), (0.8, -0.6, 0.65), (-0.7, -0.5, 0.2),
                                    (0.2, 1, 0.55))):
        o = unit(*(side * a + back * b))
        base = tip + td * at * 0.07 * K + o * 0.022 * K
        d = unit(*(td * 1.0 + o * 0.38))
        k.add(f"tailtuft{i}", blade([base - d * 0.01 * K, base + d * 0.028 * K, base + d * 0.05 * K],
                                    [0.021 * K, 0.014 * K, 0.005 * K], np.cross(d, o), 0.6), BROWN, "tail")
    line = spline(tp + [tip + td * 0.075 * K])
    along = share_along(line)
    k.paint(lambda Q: smoothstep_mask(along(Q), 0.74, 0.015), DARK, on=["tail"])

    # The cream-white front: the lower face round the snout under the eyes, the throat, the chest and belly down to
    # between the legs, its edge a little ragged as fur is
    def face(Q):
        q = (Q - H) / K
        top = -0.016 + 0.13 * np.clip(-(q[:, 1] + 0.005), 0, 1) - 0.004 * tri(q[:, 1] * 60)
        front = inside(q[:, 1] - 0.008, 0.004)
        return inside(q[:, 2] - top, 0.0025 * K) * front
    k.paint(face, WHITE, on=["head", "snout", "neck"])
    sp = np.array([P(*p) for p in SPINE])
    def belly(Q):
        z = Q[:, 2]
        yc = np.interp(z, sp[:, 2], sp[:, 1])
        ang = np.abs(np.arctan2(Q[:, 0], -(Q[:, 1] - yc)))
        zz = z / K
        w = 0.62 + 0.18 * np.clip((zz - 0.12) / 0.2, 0, 1) - 0.12 * np.clip((zz - 0.36) / 0.08, 0, 1)
        w = w + 0.05 * tri(zz * 30)
        return inside(ang - w, 0.004) * inside(zz - 0.47, 0.003) * inside(0.045 - zz, 0.003)
    k.paint(belly, WHITE, on=["body", "belly", "neck"])

    # The nose on the snout's tip, a little smile under it
    NC = hit(SNOUT, H + P(0, -0.3, -0.006), (0, 1, 0))
    k.decal(NC, unit(0, -1, 0.25), (0.0115 * K, 0.0085 * K), NOSE, eps=0.003 * K, rings=10, segs=48)
    MC = hit(SNOUT, H + P(0, -0.3, -0.03), (0, 1, 0))
    k.decal(MC, unit(0, -1, -0.3), (0.019 * K, 0.008 * K), smile, eps=0.003 * K, rings=8, segs=48)

    if extras is not None:
        extras(k, K, H, HR, HEAD)

    # Bold black eyes high on the head, a little apart, on brown
    de = unit(0.46, -0.86, 0.16)
    E = hit(HEAD, H + P(0.029, -0.3, 0.012), (0, 1, 0))
    def lay(state):
        k.decal(E, de, (0.0145 * K, 0.0165 * K), eye_look(state, black_eye, srgb(BROWN), srgb(INK), line=0.18),
                eps=0.002 * K, rings=20, segs=56, mirror=True)

    k.build(voxel=0.0052 * K, band=0.005 * K, wband=0.025 * K)
    board(k.ob)
    k.eye_states(lay)
    k.export(out)


def smoothstep_mask(s, a, edge):
    return 1 - inside(s - a, edge)


if __name__ == "__main__":
    weasel(*args())
