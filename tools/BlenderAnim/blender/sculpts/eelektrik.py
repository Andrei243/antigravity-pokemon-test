# Eelektrik sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/eelektrik.py -- <the export's Eelektrik.glb> <out.glb>
#
# From the references: a thick dark blue eel hanging in an S: its head end level at the top, facing forward, the body
# running back along the top, down behind, forward and down again and back at the bottom to the tail; the head end is
# cut flat, a broad pinkish-red rim round a flat face that is four white plates split by a black X (the pictures show
# no fangs: they overrule the task's "sucker mouth with fangs"); a round white eye with a small black pupil high on
# each side of the head, set in a yellow ring that runs back into a round lobe; round yellow spots along the upper
# side of the back curve; a cream stripe down the inner side of the S (its belly); a cream paddle of a fin with ribs
# standing up from the top behind the eye, and a cream paddle under the head on each side (a left and right pair: the
# art and the 3D model each show one hanging below the head, the other hidden behind it); the tail ending in a curl
# with a big red paddle of a fin with ribs.
# The body is laid as dratini.py's Line (by sibling), its belly turned to one side of the S's plane all along. Written
# to be grown from (eelektross.py imports it with sibling): colours, the belly's frame, the spots, the paddles and the
# mouth's rim are functions taking their numbers.
# The skeleton: head (0, -0.06, 0.434), body (0, 0, 0.294), tail (0, 0.17, 0.124).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, srgb, inside, unit, eye_look, polygon, args, outward, sibling

NAVY, CREAM, CREAM2, RED, RED2 = "#3C5487", "#EEE3A8", "#DCCF8E", "#DE6466", "#C9504F"
YELLOW, WHITE, INK = "#F4D23A", "#F8F8F4", "#16161C"

dr = sibling("dratini")

# The line, the head first: along the top, down behind, forward, down and back to the tail's curl
LINE = [(0.0, -0.13, 0.462), (0.0, -0.05, 0.482), (0.0, 0.04, 0.478), (0.0, 0.105, 0.44), (0.0, 0.118, 0.36),
        (0.0, 0.07, 0.295), (0.0, 0.01, 0.245), (0.0, -0.025, 0.175), (0.0, -0.005, 0.11), (0.0, 0.07, 0.082),
        (0.0, 0.145, 0.105), (0.0, 0.175, 0.14)]
RADS = [0.083, 0.082, 0.073, 0.062, 0.056, 0.051, 0.047, 0.043, 0.039, 0.035, 0.03, 0.026]
BONES = ["head", "head", "head", "body", "body", "body", "body", "body", "tail", "tail", "tail"]
HC = np.array([0.0, -0.1, 0.466])           # the middle of the head's end
HRAD = 0.083


def side_belly(line, sign=1.0):
    """Turn the line's belly to one side of the plane the body lies in (x = 0) all along: down where the body runs
    back, to the front where it runs down, the inner side of each curve of the S."""
    T = line.T
    B = np.stack([np.zeros(len(T)), T[:, 2], -T[:, 1]], 1) * sign
    line.bel = B / (np.linalg.norm(B, axis=1, keepdims=True) + 1e-9)


def spot(k, line, s, side, r, color=YELLOW, up=0.55, eps=0.003):
    """A round spot on the side `side` (+1 or -1) of the body at the line's parameter s, turned up off the side by up
    (toward the back of the curve, away from the belly)."""
    p, t, rad = line.at(s)
    j = int(np.argmin(np.abs(line.S - s)))
    away = -line.bel[j]
    n = unit(*(np.array([side, 0.0, 0.0]) + away * up))
    k.decal(p + n * rad, n, (r, r), color, eps=eps, rings=10, segs=40)


def paddle(k, name, root, d, across, length, width, colours, bone, thick=0.016, turn=0.0, ribs=4):
    """A paddle of a fin on a stalk, from root along d, its face across `across`, turned `turn` degrees about d toward
    +X: wedges of the colours in turn fanning from the root, so the ribs are the seams between them."""
    d = unit(*d)
    a = np.array(across, float); a = a - d * (a @ d); a /= np.linalg.norm(a)
    if turn:
        x = np.array([1.0, 0, 0]) - d * d[0]
        x /= np.linalg.norm(x)
        t = math.radians(turn)
        a = math.cos(t) * a + math.sin(t) * x
        a = a - d * (a @ d); a /= np.linalg.norm(a)
    c, rx, ry = 0.6 * length, 0.4 * length, width / 2
    # the widest angle a ray from the root meets the paddle's oval
    A = math.asin(min(0.999, ry / math.sqrt(c * c + ry * ry + 1e-12))) * 0.98

    def far(ang):
        ca, sa = math.cos(ang), math.sin(ang)
        qa = (ca / rx) ** 2 + (sa / ry) ** 2
        qb = -2 * c * ca / rx ** 2
        qc = (c / rx) ** 2 - 1
        disc = max(qb * qb - 4 * qa * qc, 0.0)
        return (-qb + math.sqrt(disc)) / (2 * qa)

    angs = np.linspace(-A, A, ribs + 1)
    for i in range(ribs):
        arc = [(far(a_) * math.cos(a_), far(a_) * math.sin(a_)) for a_ in np.linspace(angs[i], angs[i + 1], 7)]
        pts = [(0.0, 0.0)] + arc
        k.add(f"{name}{i}", slab(pts, root, d, a, thick + 0.002 * (i % 2)), colours[i % len(colours)], bone,
              fuse=False, surface="scales")


def flat_end(c, r, depth, d=(0, -1.0, 0)):
    """The head's cut end: a ball flattened to a face across d."""
    def f(V):
        V = V.copy()
        V[:, 1] = np.maximum(V[:, 1], -depth)
        return V
    return ell(c, r, deform=f)


def eelektrik(src, out):
    k = Kit(src, coat="scales")
    line = dr.Line(LINE, RADS)
    side_belly(line)
    line.tubes(k, BONES, NAVY)

    # --- the head's cut end: flat in front, a broad red rim, a white face of four plates split by a black X ---------
    k.add("snout", flat_end((0.0, -0.13, 0.465), (0.085, 0.074, 0.084), 0.86), NAVY, "head")
    front = -0.13 - 0.074 * 0.86
    k.paint(lambda P: inside(P[:, 1] - (front + 0.038), 0.0022) * inside(np.abs(P[:, 2] - 0.465) - 0.11, 0.002),
            RED)
    W, K = srgb(WHITE), srgb(INK)

    def face(u, v):
        c = np.tile(W, (len(u), 1))
        r = np.sqrt(u * u + v * v)
        c[r > 0.86] = K
        return c
    fc = np.array([0.0, front, 0.465])
    k.decal(fc, (0, -1, 0), (0.052, 0.052), face, eps=0.003, rings=16, segs=64)
    X, w = [], 0.2
    for i in range(4):
        a = math.radians(45 + 90 * i)
        ca, sa = math.cos(a), math.sin(a)
        nx, ny = -sa, ca
        X += [(0.9 * ca - w * nx, 0.9 * sa - w * ny), (0.9 * ca + w * nx, 0.9 * sa + w * ny)]
        b = math.radians(90 + 90 * i)
        X.append((w * 1.41 * math.cos(b), w * 1.41 * math.sin(b)))
    # walk the outline round: tip pairs of each arm, then the notch between it and the next
    k.decal(fc, (0, -1, 0), (0.052, 0.052), INK, eps=0.0045, outline=polygon(X), rings=12, segs=120)

    # --- the yellow spots along the upper side of the back curve -------------------------------------------------------
    for s in (1, -1):
        for sv, r in ((2.0, 0.017), (2.75, 0.018), (3.35, 0.017), (3.95, 0.015)):
            spot(k, line, sv, s, r)

    # --- the eye's yellow ring and its lobe behind, in every state ----------------------------------------------------
    for s in (1, -1):
        d = unit(s * 0.74, -0.32, 0.6)
        k.decal(HC + d * HRAD, d, (0.034, 0.034), YELLOW, eps=0.003, rings=12, segs=48)
        d2 = unit(s * 0.8, 0.2, 0.56)
        k.decal(HC + np.array([0, 0.035, 0.0]) + d2 * HRAD, d2, (0.021, 0.021), YELLOW, eps=0.003, rings=8, segs=40)

    # --- the cream belly along the inner side of the S ---------------------------------------------------------------
    k.paint(dr.belly(line, 1.6, 10.2, ground=0.3, risen=0.3, edge=0.003), CREAM, on=["seg"])

    # --- fins: a paddle up from the top, a pair under the head, a red paddle at the tail -------------------------------
    paddle(k, "dorsal", (0.0, 0.0, 0.53), (0, 0.5, 1.0), (0, 1.0, -0.5), 0.14, 0.092, [CREAM, CREAM2], "head",
           turn=40)
    for s, side in ((1, "R"), (-1, "L")):
        paddle(k, "pec" + side, (s * 0.05, -0.06, 0.41), (s * 0.4, 0.3, -0.85), (0, 1.0, 0.35), 0.12, 0.072,
               [CREAM, CREAM2], "head", ribs=3)
    p, t, rad = line.at(len(LINE) - 1.0)
    k.add("curl", tube([p - t * 0.01, p + t * 0.02, p + np.array([0, 0.035, 0.02])], [0.026, 0.026, 0.022]), NAVY,
          "tail")
    paddle(k, "tailfin", p + np.array([0.0, 0.03, 0.015]), (0, 1.0, 0.25), (0, -0.25, 1.0), 0.17, 0.135, [RED, RED2],
           "tail", thick=0.018, turn=35, ribs=5)

    k.build(voxel=0.0045, smooth=5, band=0.005)

    # --- the eyes: round, white, a small black pupil, the yellow ring round them --------------------------------------
    Y, L = srgb(YELLOW), srgb(YELLOW)

    def eye(u, v):
        c = np.tile(W, (len(u), 1))
        r2 = u * u + v * v
        c[r2 > 0.86 ** 2] = K
        c[(u + 0.05) ** 2 + v * v < 0.42 ** 2] = K
        return c

    def eyes(state):
        look = eye_look(state, eye, L, K, line=0.16)
        for s in (1, -1):
            lk = look if s > 0 else (lambda f: (lambda u, v: f(-u, v)))(look)
            d = unit(s * 0.74, -0.32, 0.6)
            k.decal(HC + d * HRAD, d, (0.022, 0.022), lk, eps=0.0045, rings=20, segs=56)

    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    eelektrik(*args())
