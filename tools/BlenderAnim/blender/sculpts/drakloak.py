# Drakloak sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/drakloak.py -- <the export's Drakloak.glb> <out.glb>
# From the references: a ghost dragon hovering upright, carrying a Dreepy lying on its head. Its head is the largest
# mass: a broad flat dark-navy hat like a stealth flier's wing, swept back to two points, three pink teeth along the
# trailing edge of each wing, its middle coming down to a point over the face, two dark spikes standing up on its top;
# under it a dark-navy face with slanted yellow eyes and black pupils and a wide open mouth, pink inside; the jaw, the
# neck and the body steel-teal, a cream belly from the throat down, a pink chevron edged dark red at the throat; small
# arms held before the chest, three fingers each; a teardrop of a lower body ending in two small pointed feet; a thick
# tail sweeping back, out and up, a dark band and a pale tip at its end.
# The Dreepy riding on the hat is dreepy.py's (sibling("dreepy")), at half its size, its eyes open in every state.
# Bones: root 0; body (0, 0, 0.38); tail (0, 0.03, 0.28); armR (0.07, -0.02, 0.42); head (0, -0.02, 0.5);
# legR (0.05, 0, 0.28).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, inside, unit, eye_look, polygon, args, sibling

dr = sibling("dreepy")

TEAL, NAVY, CREAM = "#5CA3B3", "#2E4659", "#EEE4A4"
PINK, DARKRED = "#EA5274", "#9A2E44"
EYE, PUPIL = "#F2D21E", "#14161A"
MOUTH, MOUTHDEEP = "#E86A84", "#8E2C42"
BAND, PALE = "#3C5568", "#A9DEE6"

HAT_SPAN = 0.29                     # how far a wing's tip reaches out
FACE_C, FACE_R = np.array([0.0, -0.05, 0.505]), np.array([0.095, 0.08, 0.066])
JAW_C, JAW_R = np.array([0.0, -0.068, 0.455]), np.array([0.084, 0.07, 0.046])
TAIL = [(0.0, 0.05, 0.2), (0.05, 0.16, 0.2), (0.1, 0.245, 0.275), (0.138, 0.285, 0.38), (0.155, 0.29, 0.47)]
TAIL_R = [0.068, 0.058, 0.046, 0.034, 0.022]


def on_ell(c, r, d):
    d = unit(*d)
    s = 1 / math.sqrt(((d / np.array(r)) ** 2).sum())
    p = np.array(c) + d * s
    n = (p - c) / np.array(r) ** 2
    return p, n / np.linalg.norm(n)


def wing(side, root_y=(-0.07, 0.1), root_x=0.08, root_z=(0.568, 0.578), tip=(0.08, 0.535), span=HAT_SPAN,
         thick_root=0.032, thick_tip=0.012):
    """One wing of the hat: a tapering plate from a root chord on the hat's middle to a point, its top face's corners."""
    A = np.array([root_x * side, root_y[0], root_z[0]])
    Bk = np.array([root_x * side, root_y[1], root_z[1]])
    C = np.array([span * side, tip[0], tip[1]])
    pts = [A, Bk, C, A - (0, 0, thick_root), Bk - (0, 0, thick_root), C - (0, 0, thick_tip)]
    return hull(pts), (A, Bk, C)


def teeth(A, Bk, C, cuts=((0.08, 0.3), (0.38, 0.6), (0.68, 0.9)), reach=0.38):
    """Pink teeth on a wing's top face along its trailing edge (Bk to C), pointing toward its leading corner A."""
    n = np.cross(C - A, Bk - A); n /= np.linalg.norm(n)
    if n[2] < 0:
        n = -n
    out = []
    for t0, t1 in cuts:
        q0, q1 = Bk + t0 * (C - Bk), Bk + t1 * (C - Bk)
        mid = (q0 + q1) / 2
        apex = mid + reach * (A + 0.5 * (C - A) * 0 - mid) * 0.6
        back = unit(*(mid - apex))
        tri = [q0 + back * 0.004, q1 + back * 0.004, apex]
        pts = [p + n * 0.004 for p in tri] + [p - n * 0.007 for p in tri]
        out.append(hull(pts))
    return out


def drakloak(src, out):
    k = Kit(src, coat="scales")

    # --- the body: a teal teardrop drawn down to a point between two small feet --------------------------------------
    def lower(V):
        V = V.copy()
        t = np.clip(-V[:, 2], 0, 1)
        s = 1 - 0.35 * t ** 1.4
        V[:, 0] *= s; V[:, 1] *= s
        return V

    k.add("belly", ell((0, -0.005, 0.25), (0.1, 0.092, 0.125), deform=lower, sub=5), TEAL, "body")
    k.add("chest", ell((0, -0.025, 0.37), (0.082, 0.074, 0.09)), TEAL, "body")
    k.add("low", ell((0, 0.0, 0.14), (0.055, 0.055, 0.07)), TEAL, "body")
    k.add("neck", ell((0, -0.04, 0.43), (0.07, 0.062, 0.05)), TEAL, "head")
    k.add("foot", tube([(0.035, -0.015, 0.16), (0.07, -0.035, 0.1), (0.085, -0.04, 0.078)],
                       [0.032, 0.018, 0.009]), TEAL, "leg{s}", mirror=True)

    # the tail: thick, back, out to +X and up, a dark band and a pale tip at its end
    k.add("tail", tube(TAIL, TAIL_R), TEAL, "tail")
    end = np.array(TAIL[-1]) + unit(*(np.array(TAIL[-1]) - np.array(TAIL[-2]))) * TAIL_R[-1] * 0.8
    k.paint(lambda Q: inside(np.linalg.norm(Q - end, axis=1) - 0.085, 0.003), BAND, on=["tail"])
    k.paint(lambda Q: inside(np.linalg.norm(Q - end, axis=1) - 0.05, 0.003), PALE, on=["tail"])

    # --- the arms: short, held forward before the chest, three fingers hanging ---------------------------------------
    S, M, HD = np.array([0.07, -0.02, 0.415]), np.array([0.098, -0.075, 0.37]), np.array([0.08, -0.118, 0.325])
    k.add("arm", tube([S, M, HD], [0.026, 0.022, 0.02]), TEAL, "arm{s}", mirror=True)
    k.add("hand", ell(HD + (0, -0.004, -0.006), (0.024, 0.02, 0.022)), TEAL, "arm{s}", mirror=True)
    for i, dx in enumerate((-0.012, 0.0, 0.012)):
        k.add(f"finger{i}", tube([HD + (dx, -0.008, -0.016), HD + (dx * 1.3, -0.016, -0.04)], [0.0095, 0.0075]),
              TEAL, "arm{s}", mirror=True)

    # --- the head: a dark face over a teal jaw ------------------------------------------------------------------------
    k.add("jaw", ell(JAW_C, JAW_R), TEAL, "head")
    k.add("face", ell(FACE_C, FACE_R), NAVY, "head")

    # the cream belly from the throat down, and the chevron at the throat
    k.paint(lambda Q: inside(((Q[:, 0] / (0.052 + 0.03 * np.clip((0.4 - Q[:, 2]) / 0.2, 0, 1))) ** 2
                              + ((Q[:, 2] - 0.29) / 0.145) ** 2) - 1, 0.04) * (Q[:, 1] < -0.02), CREAM,
            on=["belly", "chest", "low"])
    CH, CN = on_ell((0, -0.025, 0.37), (0.082, 0.074, 0.09), (0, -0.97, 0.12))
    CHEV = polygon([(0, -0.7), (1, 0.85), (0.5, 0.85), (0, 0.25), (-0.5, 0.85), (-1, 0.85)])
    k.decal(CH, CN, (0.05, 0.04), DARKRED, eps=0.003, outline=CHEV, rings=12, segs=96)
    k.decal(CH + CN * 0.0005 + np.array([0, 0, 0.002]), CN, (0.036, 0.029), PINK, eps=0.0045, outline=CHEV,
            rings=12, segs=96)

    # the mouth: wide and open under the eyes, dark red above, a pink tongue below
    MP, MN = on_ell(JAW_C, JAW_R, (0, -0.96, 0.28))
    Mo, Md = srgb(MOUTH), srgb(MOUTHDEEP)

    def mouth(u, v):
        c = np.tile(Mo, (len(u), 1))
        c[v > 0.15] = Md
        return c
    k.decal(MP, MN, (0.06, 0.03), mouth, eps=0.003, rings=12, segs=60,
            outline=polygon([(-1, 0.55), (1, 0.55), (0.75, -0.25), (0.35, -0.85), (-0.35, -0.85), (-0.75, -0.25)]))

    # --- the hat: a dark delta, its middle a point over the face, two wings swept back -------------------------------
    mid = [(0, -0.152, 0.49), (0, -0.06, 0.598), (0, 0.09, 0.588), (0, -0.06, 0.53), (0, 0.09, 0.536)]
    for sd in (1, -1):
        mid += [(0.09 * sd, -0.07, 0.568), (0.09 * sd, -0.07, 0.535), (0.09 * sd, 0.1, 0.578),
                (0.09 * sd, 0.1, 0.545), (0.045 * sd, -0.115, 0.535)]
    k.add("hatmid", hull(mid), NAVY, "head", fuse=False)
    for sd in (1, -1):
        w = "R" if sd > 0 else "L"
        shape, (A, Bk, C) = wing(sd)
        k.add("wing" + w, shape, NAVY, "head", fuse=False)
        for i, t in enumerate(teeth(A, Bk, C)):
            k.add(f"tooth{w}{i}", t, PINK, "head", fuse=False)
        # a dark spike standing up on the hat's top, a little out
        k.add("spike" + w, hull([(0.05 * sd, -0.035, 0.57), (0.1 * sd, -0.03, 0.566), (0.08 * sd, 0.045, 0.575),
                                 (0.06 * sd, 0.01, 0.57), (0.088 * sd, 0.02, 0.665)]), NAVY, "head", fuse=False)

    # --- the Dreepy riding on the hat, its head turned a little to +X -----------------------------------------------
    spots = dr.dreepy(k, O=(-0.005, -0.055, 0.607), F=(0.18, -1, -0.04), U=(0, 0, 1), s=0.5,
                      bones=dict(head="head", body="head", tail="head"), legs=False, floor=0.0085, name="rider")
    dr.lay_eyes(k, spots, "Open", eps=0.0018, lid=dr.MINT)

    k.build(voxel=0.006, smooth=4, band=0.006, wband=0.05)

    # --- the eyes: slanted yellow almonds, a black pupil, under the hat's edge ---------------------------------------
    Y, P = srgb(EYE), srgb(PUPIL)

    def eye(u, v):
        c = np.tile(Y, (len(u), 1))
        c[((u + 0.05) / 0.26) ** 2 + ((v + 0.05) / 0.5) ** 2 < 1] = P
        return c
    ALMOND = [(-1, -0.3), (-0.55, -0.72), (0.25, -0.62), (1, 0.15), (0.75, 0.55), (0.1, 0.62), (-0.6, 0.35)]
    spots_own = []
    for sd in (1, -1):
        d = unit(0.55 * sd, -0.8, 0.12)
        p, n = on_ell(FACE_C, FACE_R, d)
        spots_own.append((p, n, sd))

    def eyes(state):
        f = eye_look(state, eye, srgb(NAVY), srgb("#C9A818"), line=0.2)
        for p, n, sd in spots_own:
            out_ = polygon([(a * sd, b) for a, b in ALMOND])          # +u is outward on the right, inward on the left
            col = f if sd > 0 else (lambda u, v, f=f: f(-u, v))
            k.decal(p, n, (0.036, 0.022), col, eps=0.0018, outline=out_, rings=20, segs=56,
                    up=(0, 0, 1))

    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    drakloak(*args())
