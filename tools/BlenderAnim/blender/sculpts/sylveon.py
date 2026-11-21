# Sylveon sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/sylveon.py -- <the export's Sylveon.glb> <out.glb>
# Grown from eevee.py's frame as espeon.py is (its named trunk, shoulders, thighs, neck and head, its way of making the
# eyes and mouth), refitted to Sylveon's skeleton. From the references: a slender white fox-cat; the top and back of
# its head pink down to a fringe between the eyes, with small pink tufts at its cheeks; large pink ears lined with a
# dark-blue strip; big round sky-blue eyes with a deep-blue pupil and two glints and a little open smile; a white
# bow with a pink knot at the base of its right ear (the viewer's right) and a pink bow at its throat; from each bow
# two long white ribbons ending in a pink band, a blue band and a forked pale-blue tip (one of the ear's arching over
# the head, the other streaming back to the side; the throat's two swept back along the flanks); pink socks on its
# feet, a blue ring on each hind leg; and a flat pink tail curving up behind.
#
# The ribbons are loose flat bands (a sheet along a curve, some 0.4 as thick as wide) carried clear of the body, so
# their faces show to both of the game's cameras; the throat's pair is a mirror pair, the ear's two spring from one
# side as the design has them.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, blade, sheet, ring_band, srgb, inside, unit, eye_look, polygon, args,
                       outward)

# --- colours ----------------------------------------------------------------------------------------------------------
WHITE = "#F7F2F5"          # the coat: a warm white
PINK = "#F29DBA"
PINK_D = "#E07197"         # the knots, the socks' darker toes
LINING = "#3F64B4"         # the strip inside the ears
BLUE, PALE = "#4F86D8", "#9AD2F4"     # the ribbons' band and their forked tips
EYE, PUPIL, RIM = "#86CCF4", "#2E62B6", "#2A4A86"
INK = "#3A2E44"

# --- the frame (the skeleton's joints: body (0, 0.02, 0.2), head (0, -0.06, 0.28), ears (+-0.035, -0.08, 0.385),
#     forelegs (+-0.04, -0.07, 0.17), hind legs (+-0.04, 0.09, 0.17), tail (0, 0.11, 0.22)) --------------------------
# Measured on the drawing (reference / this sculpt): the head's height (chin to the top of the skull) against the
# height to the ears' tips 0.31 / 0.31; the head's width against the chest's 1.45 / 1.5; legs under the belly against
# the height 0.24 / 0.23.
H = np.array([0, -0.092, 0.31])
HR = np.array([0.088, 0.08, 0.08])
LEG_X = 0.042
FORE_Y, HIND_Y = -0.07, 0.092
BODY_Z = 0.185


def frame(c, a1, a2):
    """A right-handed frame (a1, a2, a1 x a2) as a matrix of columns."""
    a1 = unit(*a1); a2 = np.array(a2, float); a2 = a2 - a1 * (a2 @ a1); a2 /= np.linalg.norm(a2)
    return np.stack([a1, a2, np.cross(a1, a2)], axis=1)


def ell_in(c, r, M, sub=3):
    V, F = ell((0, 0, 0), r, sub=sub)
    return outward((V @ M.T + np.array(c, float), F))


def along(P, pts, n=200):
    """Nearest place on a polyline (|x| taken, so a mirrored part's both sides): its share of the length and the offset."""
    pts = np.array(pts, float)
    seg = np.linalg.norm(np.diff(pts, axis=0), axis=1)
    cum = np.concatenate([[0], np.cumsum(seg)]); L = cum[-1]
    s = np.linspace(0, L, n)
    line = np.stack([np.interp(s, cum, pts[:, i]) for i in range(3)], axis=1)
    Q = P.copy(); Q[:, 0] = np.abs(Q[:, 0])
    j = np.concatenate([((Q[a:a + 4000, None, :] - line[None]) ** 2).sum(2).argmin(1) for a in range(0, len(Q), 4000)])
    return s[j] / L, Q - line[j]


def add_body(k):
    k.add("chest", ell((0, -0.048, BODY_Z + 0.012), (0.058, 0.066, 0.074)), WHITE, "body")
    k.add("middle", ell((0, 0.02, BODY_Z), (0.052, 0.086, 0.062)), WHITE, "body")
    k.add("rump", ell((0, 0.088, BODY_Z - 0.004), (0.056, 0.058, 0.068)), WHITE, "body")


def add_legs(k):
    # Broad shoulders and full thighs over the top of each leg; the lower legs taper to small round paws
    k.add("shoulder", ell((LEG_X - 0.004, FORE_Y + 0.004, 0.15), (0.031, 0.037, 0.058)), WHITE, "leg{s}", mirror=True)
    k.add("leg", tube([(LEG_X - 0.002, FORE_Y + 0.005, 0.16), (LEG_X, FORE_Y, 0.08), (LEG_X + 0.002, FORE_Y - 0.003, 0.022)],
                      [0.028, 0.023, 0.021]), WHITE, "leg{s}", mirror=True)
    k.add("paw", ell((LEG_X + 0.002, FORE_Y - 0.012, 0.015), (0.023, 0.029, 0.016)), WHITE, "leg{s}", mirror=True)
    k.add("thigh", ell((LEG_X - 0.003, HIND_Y + 0.005, 0.15), (0.035, 0.048, 0.064)), WHITE, "hind{s}", mirror=True)
    k.add("hind", tube([(LEG_X, HIND_Y + 0.01, 0.13), (LEG_X + 0.002, HIND_Y + 0.008, 0.07),
                        (LEG_X + 0.002, HIND_Y - 0.002, 0.022)], [0.027, 0.022, 0.021]), WHITE, "hind{s}", mirror=True)
    k.add("hpaw", ell((LEG_X + 0.002, HIND_Y - 0.012, 0.015), (0.023, 0.029, 0.016)), WHITE, "hind{s}", mirror=True)
    # Pink socks, darker at the toes
    k.paint(lambda P: inside(P[:, 2] - 0.062, 0.002) * (np.abs(P[:, 0]) > 0.015), PINK)
    k.paint(lambda P: inside(P[:, 2] - 0.02, 0.0015) * (np.abs(P[:, 0]) > 0.015), PINK_D)
    # A blue ring round each hind leg just over its sock
    k.add("ring", ring_band((LEG_X + 0.0015, HIND_Y + 0.004, 0.069), unit(0.02, -0.03, 1), 0.0215, 0.011, thick=0.006),
          BLUE, "hind{s}", fuse=False, mirror=True)


def add_head(k):
    k.add("head", ell(H, HR), WHITE, "head")
    k.add("muzzle", ell(H + (0, -0.064, -0.03), (0.034, 0.028, 0.025)), WHITE, "head")
    k.add("neck", tube([(0, -0.045, 0.21), (0, -0.068, 0.25), (0, -0.085, 0.285)], [0.05, 0.047, 0.046]), WHITE, "head")
    # Small pink tufts at the sides of the head under the ears, pointing out and back
    for i, (z, d) in enumerate(((0.33, unit(1, 0.35, 0.3)), (0.305, unit(1, 0.4, -0.2)))):
        base = H + (0.07, -0.01, z - H[2])
        k.add(f"cheek{i}", blade([base, base + d * 0.02, base + d * 0.036], [0.017, 0.011, 0.003], (0, 0, 1), 0.5),
              PINK, "head", mirror=True)
    # The pink cap: the top and back of the head, its front edge over the eyes with a fringe of three points dipping
    # between them; the back of the head pink down to the neck
    def cap(P):
        q = (P - H) / (HR * 1.35)
        near = np.linalg.norm(q, axis=1) < 1
        f = np.clip(-(P[:, 1] - H[1]) / HR[1], 0, 1)
        x = np.abs(P[:, 0])
        fringe = 0.024 * np.clip(1 - x / 0.022, 0, 1) + 0.012 * np.clip(1 - np.abs(x - 0.026) / 0.008, 0, 1) * 0
        edge = H[2] + 0.03 - 0.075 * (1 - f) ** 1.5 - fringe * f
        return inside(edge - P[:, 2], 0.0018) * near
    k.paint(cap, PINK)


EAR = [(0.034, -0.098, 0.37), (0.063, -0.093, 0.42), (0.092, -0.087, 0.47), (0.116, -0.081, 0.515)]
EAR_R = [0.043, 0.043, 0.029, 0.004]


def add_ears(k):
    k.add("ear", blade(EAR, EAR_R, (0, 1, 0), 0.32), PINK, "ear{s}", mirror=True)
    d = unit(*(np.array(EAR[-1]) - np.array(EAR[0])))
    across = np.cross(d, (0, 1.0, 0)); across /= np.linalg.norm(across)
    def lining(P):
        t, off = along(P, EAR)
        half = 0.013 * np.sin(np.pi * np.clip((t + 0.1) / 0.95, 0, 1)) ** 0.6
        w = off @ across
        return (inside(np.abs(w) - half, 0.002) * inside(off[:, 1] + 0.002, 0.002) * inside(t - 0.82, 0.008)
                * inside(-t + 0.06, 0.008) * (P[:, 2] > 0.36))
    k.paint(lining, LINING, on=["ear"])


def add_tail(k):
    # Flat and pink, rising from the rump in a curve, its plane turned back and some 35 degrees out to +X
    TAIL = [(0.0, 0.11, 0.215), (0.03, 0.16, 0.25), (0.065, 0.19, 0.3), (0.085, 0.195, 0.36), (0.09, 0.185, 0.41)]
    k.add("tail", blade(TAIL, [0.022, 0.03, 0.03, 0.022, 0.005], unit(0.82, -0.57, 0), 0.38), PINK, "tail")


def bow(k, name, c, face, across, loop, loop_col, knot_col, bone):
    """A bow: two flattened loops either side of a round knot, laid in the plane square to face."""
    M = frame(c, across, np.cross(face, across))           # columns: across, up in the bow's plane, face
    for side in (1, -1):
        cc = np.array(c, float) + M[:, 0] * side * loop[0] * 0.95
        Ml = frame(cc, M[:, 0] * side + M[:, 1] * 0.15, M[:, 1])
        k.add(f"{name}loop{'R' if side > 0 else 'L'}", ell_in(cc, (loop[0], loop[1], loop[2]), Ml), loop_col, bone,
              fuse=False)
    k.add(f"{name}knot", ell_in(c, (loop[1] * 1.5, loop[1] * 1.5, loop[1] * 1.3), M), knot_col, bone, fuse=False)


def catmull(pts, n):
    pts = np.array(pts, float)
    ext = np.vstack([2 * pts[0] - pts[1], pts, 2 * pts[-1] - pts[-2]])
    P = []
    for i in range(len(pts) - 1):
        p0, p1, p2, p3 = ext[i], ext[i + 1], ext[i + 2], ext[i + 3]
        for t in np.linspace(0, 1, n, endpoint=False):
            P.append(0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t
                            + (-p0 + 3 * p1 - 3 * p2 + p3) * t ** 3))
    P.append(pts[-1])
    return np.array(P)


RIBBON_BANDS = ((0.0, 0.7, WHITE), (0.7, 0.79, PINK), (0.79, 0.86, BLUE), (0.86, 1.0, PALE))


def ribbon(k, name, pts, face, bone, width=0.034, thick=0.013, mirror=False):
    """A long flat ribbon through pts, its face turned toward face, ending in its bands and a forked pale tip."""
    C = catmull(pts, 16)
    s = np.concatenate([[0], np.cumsum(np.linalg.norm(np.diff(C, axis=0), axis=1))]); s /= s[-1]
    T = np.gradient(C, axis=0); T /= np.linalg.norm(T, axis=1, keepdims=True)
    face = np.array(face, float)
    Wd = np.cross(T, face); Wd /= np.linalg.norm(Wd, axis=1, keepdims=True)
    def place(q, v):
        idx = np.clip(np.interp(q, s, np.arange(len(s))), 0, len(s) - 1.0001)
        i0 = np.floor(idx).astype(int); f = (idx - i0)[..., None]
        p = C[i0] * (1 - f) + C[i0 + 1] * f
        w = Wd[i0] * (1 - f) + Wd[i0 + 1] * f
        w /= np.linalg.norm(w, axis=-1, keepdims=True)
        return p + w * ((v - 0.5) * width)[..., None]
    for j, (a, b, col) in enumerate(RIBBON_BANDS):
        last = j == len(RIBBON_BANDS) - 1
        def at(u, v, a=a, b=b, last=last):
            vv = 0.015 + 0.97 * v
            end = b - (0.07 * (1 - np.abs(2 * vv - 1)) if last else 0)       # a V cut into the tip: forked
            return place(a + u * (end - a), vv)
        k.add(f"{name}{j}", outward(sheet(at, 14 if not last else 8, 5, thick)), col, bone, fuse=False, mirror=mirror)


def sylveon(src, dest):
    k = Kit(src, coat="fur")
    add_body(k)
    add_legs(k)
    add_head(k)
    add_ears(k)
    add_tail(k)

    # The ear's bow, at the base of the right ear (+X, the viewer's right), white with a pink knot, and its two ribbons:
    # one arching up over the head to the other side, one streaming back and out
    B1 = np.array((0.076, -0.112, 0.36))
    F1 = unit(0.45, -0.85, 0.25)
    bow(k, "earbow", B1, F1, unit(*np.cross(F1, (0, 0, 1.0))) * -1, (0.027, 0.01, 0.019), WHITE, PINK_D, "head")
    ribbon(k, "ribbonA", [B1 + (0.004, 0.006, 0.01), (0.075, -0.1, 0.42), (0.045, -0.085, 0.47), (-0.01, -0.07, 0.49),
                          (-0.065, -0.06, 0.475), (-0.1, -0.05, 0.44), (-0.12, -0.04, 0.4)], (0, -1, 0.15), "head")
    ribbon(k, "ribbonB", [B1 + (0.008, 0.008, -0.004), (0.12, -0.07, 0.355), (0.16, -0.01, 0.365), (0.19, 0.05, 0.38),
                          (0.205, 0.11, 0.4)], unit(-0.5, -0.45, 0.75), "head")

    # The throat's bow, pink with a darker knot, and its two ribbons swept back along the flanks, clear of them
    B2 = np.array((0.0, -0.14, 0.235))
    bow(k, "neckbow", B2, unit(0, -1, 0.25), (1, 0, 0), (0.026, 0.011, 0.019), PINK, PINK_D, "body")
    ribbon(k, "ribbonC", [(0.022, -0.136, 0.226), (0.07, -0.11, 0.2), (0.088, -0.05, 0.2), (0.092, 0.03, 0.21),
                          (0.095, 0.11, 0.185), (0.1, 0.17, 0.13)], (1, 0, 0.1), "body", mirror=True)

    # The mouth: a little open smile under the muzzle
    CW, CI, CT = srgb(WHITE), srgb(INK), srgb("#D9637E")
    def mouth(u, v):
        c = np.tile(CW, (len(u), 1))
        curve = 0.35 - 0.9 * u * u
        inner = (v < curve) & (v > curve - 0.55 * (1 - np.abs(u) / 0.7)) & (np.abs(u) < 0.7)
        c[inner] = CI
        c[inner & (v < curve - 0.3)] = CT
        return c
    k.decal(H + (0, -0.088, -0.048), unit(0, -1, -0.25), (0.013, 0.009), mouth, eps=0.002, rings=10, segs=48)

    k.build(voxel=0.0042, band=0.005, wband=0.035)

    # The eyes: big round sky-blue eyes, a deep-blue pupil, a dark rim and two white glints; the lid is the white face
    ce, cp, cr = srgb(EYE), srgb(PUPIL), srgb(RIM)
    def eye(u, v):
        c = np.tile(ce, (len(u), 1))
        c[np.hypot(u, v) > 0.86] = cr
        c[np.hypot((u + 0.02) / 0.5, (v + 0.05) / 0.62) < 1] = cp
        c[np.hypot((u - 0.22) / 0.2, (v - 0.32) / 0.2) < 1] = (1, 1, 1)
        c[np.hypot((u + 0.22) / 0.11, (v + 0.3) / 0.11) < 1] = (1, 1, 1)
        return c
    de = unit(0.47, -0.88, -0.02)
    E = H + de * HR[0] * 0.9 + np.array([0, 0, -0.008])
    def eyes(state):
        k.decal(E, de, (0.025, 0.029), eye_look(state, eye, CW, CI), eps=0.002, rings=20, segs=56, mirror=True)
    k.eye_states(eyes)
    k.export(dest)


if __name__ == "__main__":
    src, dest = args()
    sylveon(src, dest)
