# Stiucon sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/stiucon.py -- <the export's Stiucon.glb> <out.glb>
#
# Carpathia No. 10152, an ambush pike, grown from Stiucuta (stiucuta.py: its squashed tube of a body, its duck-bill, its
# eye of an iris with a dark rim, a pupil and a glint, its teal fins with darker streaks). From its picture: a long thick
# pike bent in an S, ready to strike: the back dark green-black from the crown to the tail, the flanks olive with rows
# of pale yellow spots, the belly cream from the lower jaw to the tail. A long head, its wide duck-bill jaws open on a
# pink mouth with big white needle teeth up and down; a cold yellow eye under the dark cap, which cuts its top on a
# slope down toward the snout. Ragged teal fins with dark streaks: a long dorsal crest along the back, great pectorals
# behind the head, ragged fins under the belly and a ragged tail fan.
#
# The picture coils the body upright in an S; on the export's level fish skeleton it is an S seen from above (the
# middle swung to +X, the tail back to -X) with the head carried high, so both of the game's cameras see a flank.
# Joints: body (0, 0, 0.279), head (0, -0.24, 0.339), jaw (0, -0.3, 0.299), dorsal (0.04, 0.1, 0.379),
# tail (0.03, 0.42, 0.239), tailFin (-0.03, 0.78, 0.309), finR/L (+-0.08, -0.12, 0.239).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, hull, srgb, inside, unit, eye_look, args, sibling

st = sibling("stiucuta")

OLIVE, BACK, CREAM, SPOT, FIN, STREAK, YELLOW, INK, PINK, TOOTH = (
    "#7E9046", "#22301E", "#E8E4C2", "#E4E28C", "#7CBCA2", "#3E7A62", "#F2E27A", "#1A1C14", "#9C4A58", "#F6F4EA")

# the spine: (x, y, z) from the neck to the tail's root: long and nearly straight, level, one gentle bend whose rear
# half swings toward +X (the side the front camera sees), so the whole flank shows as a long torpedo
SPINE = np.array([(0.0, -0.3, 0.37), (0.0, -0.15, 0.342), (0.01, 0.0, 0.318), (0.04, 0.17, 0.302),
                  (0.085, 0.34, 0.296), (0.14, 0.5, 0.3), (0.19, 0.64, 0.31)])
RADII = [0.108, 0.122, 0.124, 0.114, 0.094, 0.068, 0.04]


def centre(y):
    """The spine's x and z at y."""
    y = np.asarray(y, float)
    return np.interp(y, SPINE[:, 1], SPINE[:, 0]), np.interp(y, SPINE[:, 1], SPINE[:, 2])


def body(k):
    k.add("trunk", st.body_tube(SPINE[:5].tolist(), RADII[:5], flat=0.84), OLIVE, "body")
    k.add("tailpart", st.body_tube(SPINE[3:].tolist(), RADII[3:], flat=0.84), OLIVE, "tail")
    # the head: a long wedge ending in the flat upper bill
    k.add("head", st.body_tube([(0, -0.5, 0.37), (0, -0.42, 0.382), (0, -0.33, 0.385), (0, -0.25, 0.375)],
                               [0.04, 0.066, 0.086, 0.096], flat=0.86), OLIVE, "head")
    k.add("bill", ell((0, -0.565, 0.355), (0.05, 0.085, 0.024), rot=(-8, 0, 0)), OLIVE, "head")
    k.add("cheek", ell((0.045, -0.39, 0.39), (0.034, 0.06, 0.044)), OLIVE, "head", mirror=True)
    # the mouth's inside, behind the gape, joining the jaws at their back
    k.add("gullet", ell((0, -0.41, 0.322), (0.05, 0.11, 0.036)), PINK, "head")
    # the lower jaw, dropped open
    k.add("jaw", st.body_tube([(0, -0.61, 0.262), (0, -0.52, 0.262), (0, -0.42, 0.275), (0, -0.32, 0.295)],
                              [0.018, 0.034, 0.048, 0.06], flat=0.95), CREAM, "jaw")


def markings(k):
    def dz(P):
        return P[:, 2] - centre(P[:, 1])[1]
    # the cream belly, the lower jaw's top kept cream too
    k.paint(lambda P: inside(dz(P) + 0.05 + 0.012 * (P[:, 1] > 0.45), 0.003) * (P[:, 1] > -0.3), CREAM,
            on=["trunk", "tailpart"])
    # the dark back from the crown to the tail
    k.paint(lambda P: inside(0.062 - dz(P) - 0.018 * np.clip((P[:, 1] - 0.4) / 0.3, 0, 1), 0.003) * (P[:, 1] > -0.3),
            BACK, on=["trunk", "tailpart"])
    # the dark cap of the head, coming down over the eye on a slope to the snout
    def cap(P):
        y = P[:, 1]
        top = np.interp(y, [-0.62, -0.5, -0.42, -0.36, -0.3, -0.2], [0.37, 0.38, 0.404, 0.41, 0.415, 0.4])
        return inside(top - P[:, 2], 0.003) * (y < -0.18)
    k.paint(cap, BACK, on=["head", "bill", "cheek"])
    # pale yellow spots in rows along the olive flanks
    def spots(P):
        y = P[:, 1]; d = dz(P)
        m = np.zeros(len(P))
        for row, (z0, off, r) in enumerate(((0.034, 0.0, 0.016), (-0.012, 0.033, 0.015))):
            for yi in np.arange(-0.24 + off, 0.64, 0.066):
                s = 1 - 0.35 * np.clip((yi - 0.35) / 0.35, 0, 1)
                m = np.maximum(m, inside(np.hypot(y - yi, d - z0 * s) - r * s, 0.002))
        return m
    k.paint(spots, SPOT, on=["trunk", "tailpart"])


def ragged(pts, depth=0.32, skip_root=1):
    """An outline with its outer edges notched: each edge after the root's gets a notch pulled toward the middle."""
    p = np.array(pts, float)
    m = p.mean(axis=0)
    out = []
    for i in range(len(p)):
        a, b = p[i], p[(i + 1) % len(p)]
        out.append(tuple(a))
        if i >= skip_root and i < len(p) - 1:
            mid = (a + b) / 2
            out.append(tuple(mid + (m - mid) * depth))
    return out


def tatters(pts, depth=0.42, push=1.06, roots=(0,), sweep=0.016):
    """Ragged as water weed: every outer point pushed out into a pointed tatter, a deep notch between each two."""
    p = np.array(pts, float)
    m = p.mean(axis=0)
    out = []
    n = len(p)
    for i in range(n):
        a = p[i] if i in roots else m + (p[i] - m) * push + np.array([sweep, 0.0])
        out.append(tuple(a))
        if i not in roots and i < n - 1 and (i + 1) not in roots:
            b = m + (p[i + 1] - m) * push + np.array([sweep, 0.0])
            mid = (a + b) / 2
            out.append(tuple(mid + (m - mid) * depth))
    return out


def grown(outline, s=1.5, split=True):
    p = [(a * s, b * s) for a, b in outline]
    if not split:
        return p
    # every edge but the root's split in two, so the weed has more tatters
    out = []
    for i in range(len(p) - 1):
        out.append(p[i]); out.append(((p[i][0] + p[i + 1][0]) / 2, (p[i][1] + p[i + 1][1]) / 2))
    out.append(p[-1])
    return out


DORSAL1 = [(0.0, 0.0), (0.03, 0.06), (0.07, 0.11), (0.12, 0.13), (0.17, 0.11), (0.2, 0.07), (0.22, 0.0)]
DORSAL2 = [(0.0, 0.0), (0.03, 0.06), (0.08, 0.1), (0.13, 0.09), (0.17, 0.05), (0.18, 0.0)]
PEC = [(0.0, 0.016), (0.06, 0.03), (0.12, 0.0), (0.16, -0.05), (0.13, -0.09), (0.07, -0.08), (0.02, -0.03)]
BELLYFIN = [(0.0, 0.0), (0.1, 0.0), (0.13, -0.05), (0.1, -0.09), (0.04, -0.07)]
TAIL = [(0.0, 0.02), (0.04, 0.08), (0.1, 0.14), (0.15, 0.16), (0.14, 0.09), (0.11, 0.02), (0.12, -0.02),
        (0.15, -0.08), (0.15, -0.15), (0.1, -0.12), (0.04, -0.07), (0.0, -0.02)]


def along(y, turn=38.0):
    """The spine's point at y, and the direction along it turned some degrees out of the body's plane toward +X."""
    x, z = centre(y)
    x2, z2 = centre(y + 0.02)
    t = np.array([x2 - x, 0.02, 0.0]); t /= np.linalg.norm(t)
    a = math.radians(turn)
    ex = np.array([t[0] * math.cos(a) + t[1] * math.sin(a), -t[0] * math.sin(a) + t[1] * math.cos(a), 0.0])
    r = np.interp(y, SPINE[:, 1], RADII)
    return np.array([float(x), y, float(z)]), ex, r


def fins(k):
    fin = lambda *a, **kw: st.fin(k, *a, color=FIN, streak=STREAK, **kw)
    # the dorsal crest in two along the back, each turned out to +X along the back's own line
    for name, y, outline, bone in (("dorsal1", -0.09, DORSAL1, "dorsal"), ("dorsal2", 0.3, DORSAL2, "tail")):
        p, ex, r = along(y)
        g = grown(outline)
        fin(name, tatters(g, roots=(0, len(g) - 1)), p + np.array([0, 0, r * 0.8]), ex, (0, 0, 1),
            bone, thick=0.012, vein=0.5)
    # ragged fins under the belly and the tail, hanging
    for name, y, bone in (("belly1", 0.02, "body"), ("belly2", 0.38, "tail")):
        p, ex, r = along(y)
        fin(name, tatters(grown(BELLYFIN, split=False), roots=(0, 1)), p - np.array([0, 0, r * 0.78]), ex, (0, 0, 1), bone,
            thick=0.012, vein=0.5)
    # the great pectorals behind the head, faces to the front
    g = grown(PEC)
    fin("pec", tatters(g, roots=(0, len(g) - 1)), (0.075, -0.17, 0.27), unit(0.72, 0.62, -0.3),
        unit(0.15, 0.2, 1), "fin{s}", mirror=True, thick=0.012, vein=0.5)
    # the tail fan at the end of the bend, turned out to +X
    end = SPINE[-1] - np.array([0.012, 0.035, 0.0])          # its root inside the tail's end
    _, ex, _ = along(0.62, turn=40)
    g = grown(TAIL, 1.45)
    fin("tailfin", tatters(g, depth=0.45, roots=(0, len(g) - 1)), end, ex, (0, 0, 1), "tailFin",
        thick=0.013, vein=0.6)


def tooth(root, length, down):
    r = np.array(root, float)
    w = length * 0.36
    s = -1 if down else 1
    pts = [r + (0, -w, 0), r + (0, w, 0), r + (w * 0.7, 0, 0), r + (-w * 0.5, 0, 0), r + (w * 0.1, -w * 0.15, s * length)]
    return hull(pts)


def teeth(k):
    # big needle teeth: four a side hanging from the bill's edge, three standing up on the jaw's
    for i, y in enumerate((-0.59, -0.535, -0.48, -0.425)):
        x = 0.036 + 0.018 * (i / 3)
        k.add(f"uptooth{i}", tooth((x, y, 0.338), 0.032 - 0.002 * i, True), TOOTH, "head", fuse=False, mirror=True,
              surface="shell")
    for i, y in enumerate((-0.565, -0.505, -0.445)):
        x = 0.026 + 0.016 * (i / 2)
        k.add(f"lowtooth{i}", tooth((x, y, 0.284 + 0.007 * i), 0.03 - 0.002 * i, False), TOOTH, "jaw", fuse=False, mirror=True,
              surface="shell")


def cold_eye(u, v, cut=True):
    """Yellow with a dark rim; its top cut by the dark cap on a slope down toward the snout (+u is toward the back)."""
    c = np.tile(srgb(YELLOW), (len(u), 1))
    c[u * u + v * v > 0.84 ** 2] = srgb(INK)
    if cut:
        c[v > 0.24 + 0.36 * u] = srgb(BACK)
    return c


def fierce_eye(u, v):
    c = cold_eye(u, v, cut=False)
    c[v > 0.02 + 0.55 * u] = srgb(BACK)
    return c


EYE_AT = np.array([0.074, -0.4, 0.398])
EYE_N = unit(0.72, -0.58, 0.32)
EYE_HALF = (0.03, 0.03)


def lay(k, state):
    look = fierce_eye if state == "Fierce" else cold_eye
    s = "Open" if state == "Fierce" else state
    st.lay_eyes(k, s, EYE_AT, EYE_N, EYE_HALF, OLIVE, look, pupil=0.36, pupil_at=(-0.08, -0.12))


def stiucon(src, out):
    k = Kit(src, coat="scales")
    body(k)
    markings(k)
    fins(k)
    teeth(k)
    k.build(voxel=0.0065, smooth=4, band=0.006, wband=0.06)
    k.eye_states(lambda state: lay(k, state))
    k.export(out)


if __name__ == "__main__":
    stiucon(*args())
