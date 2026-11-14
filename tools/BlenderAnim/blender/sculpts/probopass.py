# Probopass sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/probopass.py -- <the export's Probopass.glb> <out.glb>
# Grown from nosepass.py: its frame (hard stone as convex hulls drawn flat, the black mask over the face with a thin
# fused plate inside its front for the eyes' patches, the blocks with black holes, the nose's red) kept as it is.
#
# The design, from the references: a far larger block of deeper blue stone, wider at its foot, sitting on its base
# with no legs. On top a red cap of two drums, a broad low one overhanging the head and a small one on it. Under the
# cap's front the black mask, and in it two round open eyes: white, a blue iris, a black pupil. The nose has grown into
# a great red pyramid: its bridge narrow between the eyes under the cap, widening down and forward to a broad flat
# base. Under the base a thick black moustache of iron filings, a cloud of small hard cubes. At each side a Mini-Nose
# (on the arm bones): a pale block of Nosepass's stone with a black "0" on its front and a blue wedge of a nose
# pointing out and down; a third Mini-Nose on the back.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import bmesh
import numpy as np
from sculptkit import Kit, ell, srgb, unit, boxy, eye_look, args

# --- the line's colours (nosepass.py's, the body's stone a deeper blue) -------------------------------------------
STONE, RED, REDDARK, MASK, SLIT, HOLE = "#82A4DA", "#E2505A", "#A22E38", "#1C1E26", "#A8CCF2", "#16181E"
DEEP = "#6890CC"                       # the grown body's stone
IRON = ("#3A3C42", "#2E3036", "#4A4D54", "#34363C")
SCLERA, IRIS, PUPIL, RIM = "#F4F6F8", "#5C86C4", "#14161C", "#2C3850"


def shade(h, f):
    """A colour a little lighter or darker (f above or below 1), still written as on screen."""
    c = np.clip(np.array([int(h[i:i + 2], 16) for i in (1, 3, 5)]) * f, 0, 255).astype(int)
    return "#%02X%02X%02X" % tuple(c)


def hull(V):
    """Points made into a solid: their convex hull, its coplanar faces merged, every face wound outward."""
    bm = bmesh.new()
    for v in np.asarray(V).tolist():
        bm.verts.new(v)
    h = bmesh.ops.convex_hull(bm, input=bm.verts[:])
    bmesh.ops.delete(bm, geom=list({g for g in h["geom_interior"] + h["geom_unused"] if isinstance(g, bmesh.types.BMVert)}), context="VERTS")
    bmesh.ops.dissolve_limit(bm, angle_limit=math.radians(3), verts=bm.verts[:], edges=bm.edges[:])
    bm.verts.index_update()
    P = np.array([v.co[:] for v in bm.verts])
    F = [[v.index for v in f.verts] for f in bm.faces]
    bm.free()
    mid = P.mean(axis=0)
    out = []
    for f in F:
        q = P[f]
        n = np.zeros(3)
        for i in range(len(q)):                      # Newell's normal
            a, b = q[i], q[(i + 1) % len(q)]
            n += np.cross(a, b)
        out.append(f if np.dot(n, q.mean(axis=0) - mid) > 0 else f[::-1])
    return P, out


def frame(axis):
    """Two directions square to an axis, right-handed: e1 x e2 = axis (e1 level where it can be)."""
    a = np.array(axis, float); a /= np.linalg.norm(a)
    ref = np.array([0, 0, 1.0]) if abs(a[2]) < 0.9 else np.array([1.0, 0, 0])
    e1 = np.cross(ref, a); e1 /= np.linalg.norm(e1)
    e2 = np.cross(a, e1)
    return a, e1, e2


def octagon(c, e1, e2, a, b, ch):
    """A rectangle of half sizes a (along e1) and b (along e2) about c with its corners cut by ch of the smaller."""
    cut = ch * min(a, b)
    return [c + e1 * x + e2 * y for x, y in ((a - cut, b), (a, b - cut), (a, -b + cut), (a - cut, -b), (-a + cut, -b),
                                              (-a, -b + cut), (-a, b - cut), (-a + cut, b))]


ROCKS = []              # the loose pieces drawn flat-faced


def k_add_rock(k, name, shape, color, bone):
    k.add(name, shape, color, bone, fuse=False, surface="shell")
    ROCKS.append(name)


def block(k, name, p0, p1, half0, half1, bone, color, ch=0.3):
    """A block of stone from p0 to p1, its section a chamfered rectangle: half0 at p0, half1 at p1."""
    p0, p1 = np.array(p0, float), np.array(p1, float)
    a, e1, e2 = frame(p1 - p0)
    pts = octagon(p0, e1, e2, *half0, ch) + octagon(p1, e1, e2, *half1, ch)
    k_add_rock(k, name, hull(np.array(pts)), color, bone)


def disc(k, name, c, n, a, b, color, bone, up=(0, 0, 1), t=0.003, lift=0.0):
    """A flat loose oval lying on a face (a hole, a mark): a squashed ball in a right-handed frame (e1 x e2 = n)."""
    n = unit(*n)
    e2 = np.array(up, float) - n * np.dot(up, n); e2 /= np.linalg.norm(e2)
    e1 = np.cross(e2, n)
    V, F = ell((0, 0, 0), (a, b, t), sub=3)
    V = V @ np.stack([e1, e2, n]) + np.array(c, float) + n * (t * 0.4 + lift)
    k.add(name, (V, F), color, bone, fuse=False, surface="shell")


def lathe(profile, base, axis, seg=20):
    """A turned piece (magnezone.py's): profile (radius, height) from the bottom up the outside, each step a band of
    its own points so its edges stay hard."""
    a, e1, e2 = frame(axis)
    base = np.array(base, float)
    ang = np.linspace(0, 2 * math.pi, seg, endpoint=False) + math.pi / seg
    V, F = [], []
    for (r0, h0), (r1, h1) in zip(profile[:-1], profile[1:]):
        o = len(V)
        for r, h in ((r0, h0), (r1, h1)):
            V.extend((base + a * h + r * (math.cos(t) * e1 + math.sin(t) * e2)).tolist() for t in ang)
        for j in range(seg):
            jj = (j + 1) % seg
            F.append([o + j, o + jj, o + seg + jj, o + seg + j])
    return np.array(V), F


# --- the frame: the grown head, wider at its foot, sitting on its base ----------------------------------------------
PROFILE = [(0.0, 0.27, -0.2, 0.2), (0.2, 0.275, -0.205, 0.205), (0.45, 0.235, -0.175, 0.18),
           (0.61, 0.205, -0.15, 0.155)]          # squat: as wide as it is tall under the cap, wider at its foot
CHAMFER = 0.27
CAP = [(0, 0.585), (0.22, 0.585), (0.25, 0.6), (0.258, 0.772), (0.244, 0.79), (0.12, 0.79), (0.118, 0.838),
       (0.106, 0.852), (0, 0.852)]      # the red cap: a broad low drum, a small drum on it
MASK_Z = (0.44, 0.6)
EYE_Z, EYE_X, EYE_H = 0.538, 0.09, (0.038, 0.04)
LINE = 0.28                            # the closed eyes' line, as Nosepass's
NOSE_TOP, NOSE_TIP = (0, -0.158, 0.62), (0, -0.43, 0.25)
NOSE_BASE_Z, NOSE_BACK_X, NOSE_SIDE = 0.2, 0.19, (0.12, -0.36)
UNIT0, UNIT1 = (0.2, -0.02, 0.322), (0.335, -0.035, 0.305)    # a side Mini-Nose, along the arm bone
UNIT_HALF = ((0.075, 0.085), (0.08, 0.09))
BEARD_C, BEARD_R = np.array([0.0, -0.335, 0.12]), np.array([0.24, 0.1, 0.09])
CAP_DROP = 0.018                       # the cap sits low enough to cut the eyes' tops


def section(z, profile=PROFILE):
    P = np.array(profile)
    return [float(np.interp(z, P[:, 0], P[:, i])) for i in (1, 2, 3)]


def ring(z, profile=PROFILE, ch=CHAMFER, grow=0.0):
    hx, yf, yb = section(z, profile)
    cy = (yf + yb) / 2
    pts = octagon(np.array([0, cy, z]), np.array([1.0, 0, 0]), np.array([0, 1.0, 0]), hx, (yb - yf) / 2, ch)
    if grow:
        pts = [p + np.array([*(unit(p[0], p[1] - cy, 0)[:2] * grow), 0]) for p in pts]
    return pts


def head(k, name="head", profile=PROFILE, ch=CHAMFER, color=DEEP):
    pts = [p for z, *_ in profile for p in ring(z, profile, ch)]
    k_add_rock(k, name, hull(np.array(pts)), color, "body")


def mask(k, z0, z1, profile=PROFILE, ch=CHAMFER, plate_half=0.15):
    """nosepass.py's band and the fused plate inside its front for the eyes."""
    pts = []
    for z in (z0, z1):
        oct_out = ring(z, profile, ch, grow=0.005)
        oct_side = ring(z, profile, ch, grow=-0.002)
        oct_in = ring(z, profile, ch, grow=-0.03)
        for i in (2, 3, 4, 5):
            pts += [(oct_side if i in (2, 5) else oct_out)[i], oct_in[i]]
    k_add_rock(k, "mask", hull(np.array(pts)), MASK, "body")
    zm = (z0 + z1) / 2
    hx, yf, yb = section(zm, profile)
    front = yf - 0.004
    k.add("plate", ell((0, front + 0.006, zm), (plate_half, 0.006, (z1 - z0) / 2 * 0.8), deform=boxy(6)), MASK, "body")
    return front


def cap(k):
    """The red cap, and the small peak on its front rim pointing down to the nose's bridge."""
    k_add_rock(k, "cap", lathe(CAP, (0, 0.012, -CAP_DROP), (0, 0, 1)), RED, "body")
    z0 = CAP[0][1] - CAP_DROP
    y0 = 0.012 - CAP[2][0] * 0.97
    pts = [(s * 0.05, y0 + dy, z0 + 0.03) for s in (1, -1) for dy in (0, 0.04)]
    pts += [(0, y0 + 0.012 + dy, z0 - 0.06) for dy in (0, 0.03)]
    k_add_rock(k, "peak", hull(np.array(pts)), RED, "body")


def nose(k):
    """The grown nose: a red pyramid, its bridge narrow between the eyes, its base broad and flat."""
    top, tip = np.array(NOSE_TOP), np.array(NOSE_TIP)
    zb = NOSE_BASE_Z
    pts = [top + (s * 0.016, 0, 0) for s in (1, -1)] + [top + (s * 0.012, 0.02, -0.01) for s in (1, -1)]
    pts += [tip, tip + (0, 0.015, zb - tip[2])]
    for s in (1, -1):
        pts += [(s * NOSE_BACK_X, -0.15, zb), (s * NOSE_BACK_X, -0.15, zb + 0.03),
                (s * NOSE_SIDE[0], NOSE_SIDE[1], zb), (s * NOSE_SIDE[0], NOSE_SIDE[1], zb + 0.035)]
    k_add_rock(k, "nose", hull(np.array(pts)), RED, "body")


def beard(k, n=92, seed=476):
    """The moustache of iron filings: small hard cubes turned every way, crowded in a wide low cloud."""
    rs = np.random.RandomState(seed)
    made = 0
    while made < n:
        p = rs.uniform(-1, 1, 3)
        r = np.linalg.norm(p)
        if r > 1 or r < 0.45:
            continue
        c = BEARD_C + p * BEARD_R
        s = 0.017 + 0.009 * rs.rand()
        axes = np.linalg.qr(rs.normal(size=(3, 3)))[0]
        if np.linalg.det(axes) < 0:
            axes[:, 0] *= -1
        pts = [c + axes @ (np.array([x, y, z]) * s) for x in (1, -1) for y in (1, -1) for z in (1, -1)]
        k_add_rock(k, f"filing{made}", hull(np.array(pts)), IRON[made % len(IRON)], "body")
        made += 1


def mini_nose(k, name, p0, p1, half, bone, front_n, wedge_dir, on_end=False):
    """A Mini-Nose: a pale block of Nosepass's stone, a black "0" on its front (or, on_end, on the upper part of its
    end face, where the camera behind sees it), a blue wedge of a nose from its end."""
    p0, p1 = np.array(p0, float), np.array(p1, float)
    block(k, name, p0, p1, half[0], half[1], bone, STONE)
    a, e1, e2 = frame(p1 - p0)
    fn = unit(*front_n)
    # the "0": a black oval and the stone's colour within it
    if on_end:
        fn = a
        at = p1 + e2 * half[1][1] * 0.4
    else:
        mid = p0 + (p1 - p0) * 0.62
        depth = abs(np.dot(e1, fn)) * half[1][0] + abs(np.dot(e2, fn)) * half[1][1]
        at = mid + fn * depth + (0, 0, 0.006)
    disc(k, name + "ring", at, fn, 0.017, 0.025, HOLE, bone)
    disc(k, name + "ringin", at, fn, 0.0085, 0.015, STONE, bone, lift=0.002)
    # the wedge: from the lower part of the end face, pointing out (and down)
    w = unit(*wedge_dir)
    base = p1 - e2 * half[1][1] * (0.45 if on_end else 0.3) + (0 if on_end else fn * half[1][0] * 0.25)
    pts = [base + e1 * x + e2 * y - a * 0.01 for x in (half[1][0] * 0.7, -half[1][0] * 0.7)
           for y in (half[1][1] * 0.55, -half[1][1] * 0.6)]
    pts += [base + w * 0.075]
    k_add_rock(k, name + "nose", hull(np.array(pts)), DEEP, bone)


def mini_noses(k):
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        mini_nose(k, "unit" + side, np.array(UNIT0) * m, np.array(UNIT1) * m, UNIT_HALF, "arm" + side, (0, -1, 0),
                  (s * 0.8, -0.35, -0.5))
    mini_nose(k, "unitBack", (0, 0.17, 0.3), (0, 0.27, 0.29), UNIT_HALF, "body", (1, 0, 0), (0.0, 0.75, -0.66),
              on_end=True)


# --- the eyes: open and round in the black, Nosepass's line when they close --------------------------------------------
CM, CS = srgb(MASK), srgb(SLIT)
CW, CI, CP, CR = srgb(SCLERA), srgb(IRIS), srgb(PUPIL), srgb(RIM)


def open_eye(u, v):
    c = np.tile(CW, (len(u), 1))
    rr = u * u + v * v
    d = (u + 0.06) ** 2 + (v + 0.02) ** 2                # looking a little inward, to the nose's bridge
    c[d < 0.52 ** 2] = CI
    c[d < 0.3 ** 2] = CP
    c[rr > 0.82 ** 2] = CR
    return c


def fierce(u, v):
    """The open eye under a black lid slanting down to the nose (u outward is +), no line over it."""
    c = open_eye(u, v)
    c[v > 0.15 + 0.45 * u] = CM
    return c


def eyes(k, state, front, z=EYE_Z, x=EYE_X, half=EYE_H):
    look = fierce if state == "Fierce" else eye_look(state, open_eye, CM, CS, line=LINE)
    k.decal((x, front, z), (0, -1, 0), half, look, up=(0, 0, 1), eps=0.006, rings=20, segs=56, mirror=True)


def finish(k, voxel=0.005, smooth=2):
    loose = [(p["name"], len(p["F"])) for p in k.parts if not p["fuse"] and not p["carve"]]
    body = k.build(voxel=voxel, smooth=smooth)
    me = body.data
    flags = np.ones(len(me.polygons), bool)
    at = len(k.surfaces[k.name][1])
    for name, count in loose:
        if name in ROCKS:
            flags[at:at + count] = False
        at += count
    me.polygons.foreach_set("use_smooth", flags.tolist())
    return body


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="shell")
    head(k)
    front = mask(k, *MASK_Z)
    cap(k)
    nose(k)
    beard(k)
    mini_noses(k)
    finish(k)
    k.eye_states(lambda state: eyes(k, state, front))
    k.export(out)
