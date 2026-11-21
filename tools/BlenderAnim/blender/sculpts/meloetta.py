# Meloetta (Aria Forme) sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/meloetta.py -- <the export's Meloetta.glb> <out.glb>
#
# From the references: a little floating sprite. A big round white head with large pale teal eyes, a dark teal pupil
# and a glint; a small open mouth; a teal gem at the front of a pale green cap of hair over the crown. A dark grey
# headset: two leaf-shaped prongs standing up from the top of the head, and on the viewer's left (-X) a line down
# the side of the head ending in a curl with a bulb by the cheek, like a treble clef. From the back of the crown on
# the viewer's right (+X) a long broad ribbon of pale green hair falls in waves nearly to the ground, lined like a
# stave with teal notes on it. A thin dark neck, a little pale green chest, a dark grey teardrop of a dress; thin
# dark grey arms with round paddle hands, carried forward and down (the art flings them up); thin white legs to
# points under the dress. (The pictures overrule the old C# sculpt, whose arms were white; and its hair a tube.)
# Bones: root, body (0, 0, 0.2), arm{s} (0.03, 0, 0.26), head (0, -0.005, 0.26), leg{s} (0.02, 0, 0.15).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, blade, sheet, outward, srgb, inside, smoothstep, unit, eye_look, args)

WHITE, GREY, GREEN, STAVE, NOTE = "#F4F5F8", "#5E5658", "#C3E6A4", "#7DAE6E", "#58BCD6"
IRIS, PUPIL, GLINT, MOUTHC, TONGUE = "#8ED8E6", "#2C7A92", "#FFFFFF", "#7A2A34", "#E0707A"

H = np.array([0.0, -0.01, 0.33])
HR = np.array([0.061, 0.055, 0.057])


def body(k):
    k.add("neck", tube([(0, 0.0, 0.29), (0, 0.0, 0.255)], [0.011, 0.011]), GREY, "head")
    k.add("chest", ell((0, 0.0, 0.243), (0.027, 0.022, 0.018)), GREEN, "body")

    def drop(V):
        V = V.copy()
        t = np.clip((V[:, 2] + 1) / 2, 0, 1)
        s = 0.12 + 0.88 * t ** 0.75 * (1.15 - 0.15 * t)
        V[:, 0] *= s; V[:, 1] *= s
        return V
    k.add("dress", ell((0, 0.002, 0.175), (0.041, 0.033, 0.072), deform=drop), GREY, "body")
    for s, side in ((1, "R"), (-1, "L")):
        k.add(f"leg{side}", tube([(0.017 * s, 0.004, 0.16), (0.024 * s, -0.002, 0.09), (0.022 * s, -0.01, 0.02),
                                  (0.02 * s, -0.012, 0.009)], [0.013, 0.0115, 0.0085, 0.0075]), WHITE, f"leg{side}")


def head(k):
    k.add("head", ell(H, HR), WHITE, "head")
    k.add("face", ell(H + (0, -0.012, -0.006), (0.054, 0.046, 0.051)), WHITE, "head")

    def cap(P):
        thr = H[2] + 0.024 - 0.55 * (P[:, 1] - H[1])
        return inside(thr - P[:, 2], 0.0015) * inside(0.29 - P[:, 2], 0.002)
    k.paint(cap, GREEN)


def arms(k):
    for s, side in ((1, "R"), (-1, "L")):
        pts = [(0.024 * s, 0.0, 0.262), (0.058 * s, -0.012, 0.238), (0.072 * s, -0.03, 0.208)]
        k.add(f"arm{side}", outward(tube(pts, [0.009, 0.0085, 0.008])), GREY, f"arm{side}", fuse=False,
              surface="cloth")
        k.add(f"hand{side}", ell((0.08 * s, -0.04, 0.19), (0.017, 0.009, 0.021), rot=(-15, 18 * s, 0)), GREY,
              f"arm{side}", fuse=False, surface="cloth")


def headset(k):
    for s in (1, -1):
        pts = [(0.018 * s, 0.006, 0.374), (0.025 * s, 0.008, 0.396), (0.031 * s, 0.01, 0.416), (0.035 * s, 0.011, 0.431)]
        k.add(f"prong{'R' if s > 0 else 'L'}", outward(blade(pts, [0.007, 0.014, 0.0125, 0.005], (0, 1, 0), 0.42)),
              GREY, "head", fuse=False, surface="plastic")
    # the clef on -X: down the side of the head, curling out by the cheek into a bulb
    pts = [(-0.034, 0.004, 0.368), (-0.056, -0.004, 0.345), (-0.064, -0.01, 0.315), (-0.07, -0.016, 0.292),
           (-0.083, -0.018, 0.284), (-0.092, -0.017, 0.296), (-0.086, -0.016, 0.307)]
    k.add("clef", outward(tube(pts, [0.0075] * 6 + [0.007])), GREY, "head", fuse=False, surface="plastic")
    k.add("bulb", ell((-0.078, -0.017, 0.304), (0.0105, 0.0095, 0.0105)), GREY, "head", fuse=False, surface="plastic")


# --- the ribbon of hair: a centre line from the back of the crown on +X, waving down to near the ground
LINE = np.array([(0.012, 0.036, 0.376), (0.054, 0.048, 0.358), (0.08, 0.052, 0.31), (0.097, 0.05, 0.255),
                 (0.088, 0.058, 0.195), (0.106, 0.055, 0.135), (0.096, 0.066, 0.078), (0.104, 0.066, 0.03)])
FACING = unit(0.42, -1, 0)


def _curve(n=200):
    p = LINE
    ext = np.vstack([2 * p[0] - p[1], p, 2 * p[-1] - p[-2]])
    out = []
    for i in range(len(p) - 1):
        p0, p1, p2, p3 = ext[i:i + 4]
        for t in np.linspace(0, 1, 30, endpoint=False):
            out.append(0.5 * (2 * p1 + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t
                              + (-p0 + 3 * p1 - 3 * p2 + p3) * t ** 3))
    out.append(p[-1])
    out = np.array(out)
    arc = np.concatenate([[0], np.cumsum(np.linalg.norm(np.diff(out, axis=0), axis=1))])
    s = np.linspace(0, arc[-1], n)
    P = np.stack([np.interp(s, arc, out[:, j]) for j in range(3)], axis=1)
    T = np.gradient(P, axis=0); T /= np.linalg.norm(T, axis=1, keepdims=True)
    A = np.cross(FACING, T); A /= np.linalg.norm(A, axis=1, keepdims=True)
    return P, T, A


_P, _T, _A = _curve()


def half_width(u):
    return 0.024 + 0.022 * smoothstep(0.0, 0.2, u) - 0.008 * smoothstep(0.8, 1.0, u)


def frame(u, v):
    """The ribbon's point at u along (0..1) and v across (0..1), its tangent, across and normal."""
    i = np.clip(np.round(np.asarray(u) * (len(_P) - 1)).astype(int), 0, len(_P) - 1)
    p = _P[i] + _A[i] * ((np.asarray(v) - 0.5) * 2 * half_width(np.asarray(u)))[..., None]
    return p, _T[i], _A[i]


def at_range(u0, u1, v0, v1):
    def at(u, v):
        uu = u0 + (u1 - u0) * u
        vv = v0 + (v1 - v0) * v
        return frame(uu, vv)[0]
    return at


def hair(k):
    k.add("hair", outward(sheet(at_range(0.0, 1.0, 0.0, 1.0), 120, 12, 0.009)), GREEN, "head", fuse=False,
          surface="hair")
    for i, v in enumerate((0.33, 0.67)):
        k.add(f"stave{i}", outward(sheet(at_range(0.06, 0.97, v - 0.055, v + 0.055), 110, 3, 0.0125)), STAVE, "head",
              fuse=False, surface="hair")
    for i, u in enumerate((0.36, 0.7)):
        k.add(f"bar{i}", outward(sheet(at_range(u - 0.006, u + 0.006, 0.04, 0.96), 3, 12, 0.0125)), STAVE, "head",
              fuse=False, surface="hair")
    V0, F0 = ell((0, 0, 0), (1, 1, 1), sub=3)
    for i, (u, v) in enumerate(((0.22, 0.5), (0.46, 0.22), (0.6, 0.72), (0.84, 0.42))):
        p, t, a = frame(u, v)
        n = np.cross(t, a)
        M = np.stack([a * 0.0125, t * 0.0155, n * 0.0075], axis=0)
        k.add(f"note{i}", outward((V0 @ M + p, F0)), NOTE, "head", fuse=False, surface="hair")


# --- the face: large pale teal eyes, a small open mouth, the gem at the front of the cap
CI, CP, CG, CW, CM, CT = (srgb(c) for c in (IRIS, PUPIL, GLINT, WHITE, MOUTHC, TONGUE))
OUTL = srgb("#3A3E48")


def eye_open(u, v):
    c = np.tile(CI, (len(u), 1))
    o = u * u + v * v
    c[o > 0.86] = OUTL
    c[((u + 0.02) / 0.46) ** 2 + ((v + 0.06) / 0.52) ** 2 < 1] = CP
    c[((u - 0.2) / 0.22) ** 2 + ((v - 0.3) / 0.24) ** 2 < 1] = CG
    return c


def on_head(d, c=H, r=HR):
    d = unit(*d)
    p = c + d / math.sqrt(((d / r) ** 2).sum())
    n = (p - c) / r ** 2
    return p, n / np.linalg.norm(n)


def marks(k):
    p, n = on_head((0, -0.72, -0.62))
    def mouth(u, v):
        c = np.tile(CM, (len(u), 1))
        c[((u) / 0.6) ** 2 + ((v + 0.45) / 0.45) ** 2 < 1] = CT
        return c
    k.decal(p, n, (0.0075, 0.0095), mouth, eps=0.0015, rings=10, segs=36)
    p, n = on_head((0, -0.72, 0.66))
    def gem(u, v):
        c = np.tile(srgb(NOTE), (len(u), 1))
        c[((u + 0.25) / 0.3) ** 2 + ((v - 0.3) / 0.3) ** 2 < 1] = CW
        c[u * u + v * v > 0.82] = srgb("#2E7A92")
        return c
    k.decal(p, n, (0.011, 0.0085), gem, eps=0.002, rings=12, segs=40)


def eyes(state):
    look = eye_look(state, eye_open, CW, OUTL, line=0.16)
    for side in (1, -1):
        p, n = on_head((side * 0.44, -0.86, -0.02))
        col = look if side == 1 else (lambda a, b, f=look: f(-a, b))
        k.decal(p, n, (0.018, 0.0225), col, eps=0.002, rings=20, segs=56)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="fur")
    body(k)
    head(k)
    arms(k)
    headset(k)
    hair(k)
    marks(k)
    k.build(voxel=0.004, smooth=3, band=0.006)
    k.eye_states(eyes)
    k.export(out)
