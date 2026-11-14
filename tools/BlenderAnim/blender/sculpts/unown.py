# Unown (its letter A) sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/unown.py -- <the export's Unown.glb> <out.glb>
#
# The design, from the references (the 3D model shows the A; the drawing is another letter, with a triangle of an arm
# and a forked foot, and is only used for the colours and the eye): a glyph of round black strokes floating upright,
# facing forward. One large round eye fills a thick ring: a white disc set a little back in the ring, a small solid
# dark pupil a little up and to the viewer's left. Over the ring a short thick point, leaning a little to +X; under it
# two straight legs, the -X one splayed out, the +X one nearly upright and longer; between them, under the ring, a
# small closed loop of a thinner stroke touching the ring and both legs. All ends rounded. From behind it is the same
# black glyph with no eye (the C# sculpt's black disc behind the white).
#
# Written to be grown into the other 27 letters (Unown-B ... Unown-Z, Unown-Exclamation, Unown-Question): a letter is
# a description (see UNOWN_A at the end) that letter() turns into fused strokes, and eye() lays its eye in the five
# states. A letter's own script imports this file and calls letter() and eye() on its own description.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, srgb, eye_look, polygon, args

BODY = "#2C3039"            # a very dark blue-grey, not black, so the strokes' edges read at the game's size
WHITE, PUPIL = "#F2F3F5", "#121419"
DEPTH = 0.8                 # a stroke's depth (along Y) as a share of its width: round strokes, as the 3D model's
BONE = "body"
Y_AXIS = np.array([0.0, 1.0, 0.0])


# --- strokes -----------------------------------------------------------------------------------------------------
def sweep(pts, width, depth=DEPTH, ends=("round", "round"), closed=False, seg=24):
    """
    A stroke through points (x, z) in the letter's plane (y = 0): a cross-section `width` across in the plane and
    width * depth along Y. width: one number, or (first, last) eased along the stroke. ends: "round" (a half
    spheroid) or "flat" (cut square across) at each end; closed: a loop, no ends.
    """
    P2 = np.array(pts, float)
    P = np.stack([P2[:, 0], np.zeros(len(P2)), P2[:, 1]], axis=1)
    n = len(P)
    w = np.full(n, float(width)) if np.isscalar(width) else np.linspace(width[0], width[1], n)
    if closed:
        T = np.roll(P, -1, axis=0) - np.roll(P, 1, axis=0)
    else:
        T = np.gradient(P, axis=0)
    T /= np.linalg.norm(T, axis=1, keepdims=True)
    N = np.cross(Y_AXIS, T)                     # in the plane, square to the stroke
    ang = np.linspace(0, 2 * math.pi, seg, endpoint=False)
    ca, sa = np.cos(ang), np.sin(ang)

    def ring(c, t, nn, r, d):
        return c + np.outer(ca, nn * r) + np.outer(sa, Y_AXIS * d)

    rings = [ring(P[i], T[i], N[i], w[i] / 2, w[i] / 2 * depth) for i in range(n)]
    tips = []
    if not closed:
        for end, i, s in ((ends[0], 0, -1), (ends[1], n - 1, 1)):
            if end == "round":
                caps = [ring(P[i] + s * T[i] * w[i] / 2 * math.sin(math.radians(a)), T[i], N[i],
                             w[i] / 2 * math.cos(math.radians(a)), w[i] / 2 * depth * math.cos(math.radians(a)))
                        for a in (30, 55, 75)]
                tip = P[i] + s * T[i] * w[i] / 2
            else:
                caps = []
                tip = P[i]
            if s < 0:
                rings = caps[::-1] + rings
            else:
                rings = rings + caps
            tips.append(tip)
    V = np.vstack(rings)
    F = []
    k = len(rings)
    for r in range(k if closed else k - 1):
        a, b = r * seg, ((r + 1) % k) * seg
        for j in range(seg):
            jj = (j + 1) % seg
            F.append([a + j, a + jj, b + jj, b + j])
    if not closed:
        s0, s1 = len(V), len(V) + 1
        V = np.vstack([V, tips[0], tips[1]])
        last = (k - 1) * seg
        for j in range(seg):
            jj = (j + 1) % seg
            F.append([s0, jj, j])
            F.append([s1, last + j, last + jj])
    return V, F


def line(k, name, pts, width, ends=("round", "round")):
    """A polyline stroke: a straight piece each segment, joined round at the corners (corners stay sharp-ish where a
    joint is a small round ball inside the strokes)."""
    pts = [np.array(p, float) for p in pts]
    for i, (a, b) in enumerate(zip(pts[:-1], pts[1:])):
        if np.isscalar(width):
            wa = wb = width
        else:
            t0, t1 = i / (len(pts) - 1), (i + 1) / (len(pts) - 1)
            wa = width[0] + (width[1] - width[0]) * t0
            wb = width[0] + (width[1] - width[0]) * t1
        e0 = ends[0] if i == 0 else "round"
        e1 = ends[1] if i == len(pts) - 2 else "round"
        steps = np.linspace(0, 1, 6)
        k.add(f"{name}{i}", sweep([a + (b - a) * t for t in steps], (wa, wb), ends=(e0, e1)), BODY, BONE)


def arc(k, name, at, r, a0, a1, width, ends=("round", "round")):
    """An arc of a circle about `at` of centre-line radius r, from angle a0 to a1 in degrees (0 is +X, 90 up,
    counter-clockwise as seen from the front); 360 degrees is a closed loop. r may be (rx, rz) for an oval."""
    rx, rz = (r, r) if np.isscalar(r) else r
    full = abs(a1 - a0) >= 360
    m = max(12, int(abs(a1 - a0) / 6))
    th = np.radians(np.linspace(a0, a1, m, endpoint=not full))
    pts = np.stack([at[0] + rx * np.cos(th), at[1] + rz * np.sin(th)], axis=1)
    k.add(name, sweep(pts, width, ends=ends, closed=full), BODY, BONE)


def dot(k, name, at, r):
    """A dot apart from the body (the !'s and the ?'s): a round blob as deep as a stroke."""
    k.add(name, ell((at[0], 0, at[1]), (r, r * DEPTH, r)), BODY, BONE)


# --- the eye -----------------------------------------------------------------------------------------------------
PUPIL_R = 0.25                  # the pupil, a share of the white's radius
PUPIL_AT = (-0.12, 0.1)         # where it looks: a little up and to the viewer's left, as both pictures have it
GAP = 0.004                     # between the white's edge and the ring's inner edge


def ring(k, eye):
    """The eye's ring of body and the disc it holds, set a little back in the ring (black behind, as the C# sculpt)."""
    x, z = eye["at"]
    w = eye["ring"]
    rc = eye["white"] + GAP + w / 2
    arc(k, "ring", (x, z), rc, 0, 360, w)
    k.add("disc", ell((x, 0, z), (rc, w * DEPTH * 0.3, rc)), BODY, BONE)


CW, CP, CB = srgb(WHITE), srgb(PUPIL), srgb(BODY)


def brow(u):
    """Fierce: the body's own colour comes down over the white in a V toward the middle, a frown on one eye."""
    return 0.22 + 0.55 * np.abs(u)


def eye_white(u, v):
    return np.tile(CW, (len(u), 1))


def white_outline(fierce):
    """The white's outline (a unit circle), cut by the brow in the Fierce state: the patch's own edge is the brow's
    line, so it stays crisp (a line drawn in the colour function steps at the patch's rings), and the dark disc under
    the patch shows above it."""
    th = np.linspace(0, 2 * math.pi, 112, endpoint=False)
    pts = []
    for t in th:
        u, v = math.cos(t), math.sin(t)
        if fierce and v > brow(u):
            # slide along the ray from the middle to where it meets the V
            s = 0.22 / (v - 0.55 * abs(u)) if v - 0.55 * abs(u) > 1e-6 else 1.0
            u, v = u * min(s, 1.0), v * min(s, 1.0)
        pts.append((u, v))
    return polygon(pts)


def pupil_outline(fierce):
    """The pupil's outline in its own patch's units (a unit circle), cut by the brow in the Fierce state."""
    th = np.linspace(0, 2 * math.pi, 72, endpoint=False)
    pts = []
    for t in th:
        pu, pv = math.cos(t), math.sin(t)
        if fierce:
            u = PUPIL_AT[0] + PUPIL_R * pu
            top = (brow(u) - 0.03 - PUPIL_AT[1]) / PUPIL_R
            pv = min(pv, top)
        pts.append((pu, pv))
    return polygon(pts)


def eye(k, state, letter_desc):
    """The letter's eye in one of the five states (no mirroring: one eye in the middle): the white, a patch on the
    disc's front, and in the Open and Fierce states the pupil as a patch of its own over it, so its edge is round."""
    e = letter_desc["eye"]
    x, z = e["at"]
    r = e["white"]
    # A line of the shut states is drawn in the colour function, which steps at the patch's rings: on an eye this
    # large the patch is finer than the usual 20 by 56, and the line thinner than eye_look's default (a seventh of a
    # small eye is a heavy bar on this one)
    look = eye_white if state in ("Open", "Fierce") else eye_look(state, eye_white, CW, CP, line=0.1)
    k.decal((x, -0.01, z), (0, -1, 0), (r, r), look, eps=0.003, rings=40, segs=112,
            outline=white_outline(state == "Fierce"))
    if state in ("Open", "Fierce"):
        px, pz = x + PUPIL_AT[0] * r, z + PUPIL_AT[1] * r
        k.decal((px, -0.01, pz), (0, -1, 0), (PUPIL_R * r, PUPIL_R * r), PUPIL, eps=0.0045, rings=8, segs=72,
                outline=pupil_outline(state == "Fierce"))


# --- a letter ----------------------------------------------------------------------------------------------------
def letter(k, desc):
    """
    A letter from its description: "eye" {at: (x, z) of its middle, white: the white's radius, ring: the ring's
    stroke width}, and "strokes", each one of
      ("line", [(x, z), ...], width or (first, last), (end, end))   ends "round" or "flat"
      ("arc", (x, z) centre, radius or (rx, rz), from degrees, to degrees, width, (end, end))
      ("dot", (x, z), radius)
    in the letter's plane: x across (+X the viewer's right from the front), z up, in the model's units. Every stroke
    is fused into one body on the body bone; the eye is laid afterwards by eye().
    """
    ring(k, desc["eye"])
    for i, s in enumerate(desc["strokes"]):
        kind = s[0]
        if kind == "line":
            line(k, f"stroke{i}_", s[1], s[2], s[3] if len(s) > 3 else ("round", "round"))
        elif kind == "arc":
            arc(k, f"stroke{i}", s[1], s[2], s[3], s[4], s[5], s[6] if len(s) > 6 else ("round", "round"))
        elif kind == "dot":
            dot(k, f"stroke{i}", s[1], s[2])
        else:
            raise ValueError(kind)


def unown(src, out, desc):
    k = Kit(src, coat="fur")
    letter(k, desc)
    k.build(voxel=0.0035, smooth=3)
    k.eye_states(lambda state: eye(k, state, desc))
    k.export(out)


# --- the letter A ----------------------------------------------------------------------------------------------
# Measured on the 3D model's picture (420 px tall): the ring's outside 0.54 of the height, a stroke 0.073, the point
# 0.15 over the ring, the legs 0.33 under it, the loop 0.21 across. The body bone stands at the eye's middle (0.366);
# the lowest point is the C# export's (0.062).
#
# Another letter is written the same way, e.g. a C would be its eye and one arc round it opening to +X:
#   UNOWN_C = {"eye": {"at": (0, 0.36), "white": 0.09, "ring": 0.037},
#              "strokes": [("arc", (0, 0.36), 0.16, 40, 320, 0.037, ("round", "round"))]}
# and an ! a line down from the ring with ("dot", (0, 0.08), 0.025) under it apart from the body.
UNOWN_A = {
    "eye": {"at": (0.0, 0.366), "white": 0.094, "ring": 0.040},
    "strokes": [
        ("line", [(0.008, 0.49), (0.022, 0.552)], (0.056, 0.044)),       # the point over the ring: short and thick
        ("line", [(-0.068, 0.272), (-0.146, 0.099)], 0.041),              # the -X leg, splayed out
        ("line", [(0.036, 0.255), (0.048, 0.084)], 0.041),                # the +X leg, nearly upright
        ("arc", (-0.028, 0.19), 0.048, 0, 360, 0.025),                    # the loop between them
    ],
}

if __name__ == "__main__":
    src, out = args()
    unown(src, out, UNOWN_A)
