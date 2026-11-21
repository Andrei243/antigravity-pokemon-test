# Indeedee (the male) sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/indeedee.py -- <the export's Indeedee.glb> <out.glb>
#
# The design, from the references: a butler in dark navy purple. A round head the size of a bob of hair, the lower face
# white from just under the brows to the chin, the dark coming down to a point between the eyes where a small cyan mark
# sits; half-lidded brown eyes with a dark pupil and a glint, their top a dark lid line; a white wisp curling up over
# each eye's outer corner; a small stern mouth. On each side of the head's top a great lavender-grey curl like a ram's
# horn, ribbed, its broad lobe against the side of the head and its point curling up and in over the crown. A narrow
# chest with a white V from the collar to a point at mid-chest; below the waist the body swells into one great round
# bell (as wide as the head with its curls nearly), on short dark legs with white feet. White mittens of hands: the -X
# hand set on the hip, the +X hand held forward, palm up, as a butler shows the way (the art holds it out sideways).
# Where the pictures overruled the brief: the curls are lavender grey, not purple; the brief's "swept up" horns are
# the pictures' ram-horn curls, broad at the side of the head.
# Skeleton: root, body (0, 0, 0.2), head (0, -0.01, 0.33), armL/armR (+-0.05, 0, 0.3), tail (0, 0.08, 0.13),
# legL/legR (+-0.04, -0.01, 0.07).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, unit, eye_look, args, polygon, inside, smoothstep

# --- colours ----------------------------------------------------------------------------------------------------------
DARK, WHITE, HORN, HORN_D = "#4A4478", "#F4F2F8", "#9C97BE", "#7570A0"
IRIS, PUPIL, LIDLINE, CYAN, INK = "#8A5A42", "#24161A", "#22203A", "#46C6E4", "#2A2238"

# --- the frame --------------------------------------------------------------------------------------------------------
H, HR = np.array([0.0, -0.012, 0.418]), np.array([0.097, 0.086, 0.095])
BELL_C, BELL_R = np.array([0.0, 0.005, 0.152]), np.array([0.122, 0.104, 0.098])
CURL = [(0.07, 0.012, 0.47), (0.105, 0.01, 0.425), (0.145, 0.004, 0.428), (0.162, 0.0, 0.47), (0.148, -0.002, 0.515),
        (0.118, -0.002, 0.545), (0.084, 0.0, 0.556)]
CURL_R = [0.042, 0.052, 0.054, 0.048, 0.036, 0.021, 0.008]
CURL_FILL = ((0.122, 0.008, 0.468), (0.046, 0.042, 0.044))     # the curl's middle, filled: one conch of a mass
EYE = np.array([0.048, -0.082, 0.392])
EYE_N = unit(0.42, -0.9, -0.12)


def on_head(x, z):
    """A point on the head's front at x, z, and the outward normal there."""
    d = 1 - (x / HR[0]) ** 2 - ((z - H[2]) / HR[2]) ** 2
    y = H[1] - HR[1] * math.sqrt(max(d, 0.0))
    p = np.array([x, y, z])
    g = (p - H) / HR ** 2
    return p, g / np.linalg.norm(g)


def along(P, pts):
    """Each point's share along a polyline and its distance from it."""
    pts = np.array(pts, float)
    seg = np.diff(pts, axis=0)
    L = np.linalg.norm(seg, axis=1)
    cum = np.concatenate([[0], np.cumsum(L)])
    best_t, best_d = np.zeros(len(P)), np.full(len(P), 9.0)
    for i in range(len(seg)):
        t = np.clip(((P - pts[i]) @ seg[i]) / L[i] ** 2, 0, 1)
        d = np.linalg.norm(P - (pts[i] + t[:, None] * seg[i]), axis=1)
        m = d < best_d
        best_d[m] = d[m]; best_t[m] = (cum[i] + t * L[i])[m] / cum[-1]
    return best_t, best_d


def body(k):
    def bell(V):
        V = V.copy()
        z = V[:, 2]
        V[:, :2] *= (1 - 0.22 * np.clip(z, 0, 1) ** 2 + 0.04 * np.clip(-z, 0, 1))[:, None]
        V[:, 2] = np.where(z < -0.55, -0.55 + (z + 0.55) * 0.6, z)
        return V
    k.add("bell", ell(BELL_C, BELL_R, deform=bell), DARK, "body")
    k.add("waist", ell((0, 0.0, 0.245), (0.06, 0.052, 0.05)), DARK, "body")
    k.add("chest", ell((0, -0.002, 0.292), (0.05, 0.043, 0.05)), DARK, "body")
    k.add("neck", tube([(0, 0.0, 0.305), (0, -0.006, 0.345)], [0.03, 0.032]), DARK, "head")
    k.add("tail", ell((0, 0.098, 0.14), (0.026, 0.022, 0.024)), DARK, "tail")
    # the white V from the collar to a point at mid-chest
    def vee(P):
        w = 0.05 * np.clip((P[:, 2] - 0.238) / 0.085, 0, 1)
        return (inside(np.abs(P[:, 0]) - w, 0.0015) * inside(P[:, 1] + 0.012, 0.002)
                * inside(P[:, 2] - 0.338, 0.002) * smoothstep(0.236, 0.24, P[:, 2]))
    k.paint(vee, WHITE, on=["chest", "waist", "neck"])
    # short dark legs and white feet
    k.add("leg", tube([(0.042, 0.0, 0.085), (0.045, -0.006, 0.03)], [0.024, 0.022]), DARK, "leg{s}", mirror=True)
    k.add("foot", ell((0.046, -0.02, 0.017), (0.026, 0.036, 0.017)), WHITE, "leg{s}", mirror=True)
    def feet(P):
        return inside(P[:, 2] - 0.034, 0.002) * inside(np.abs(P[:, 0]) - 0.09, 0.002)
    k.paint(feet, WHITE, on=["leg", "foot"])


def arms(k):
    # -X: the hand set on the hip, the elbow out
    k.add("armL", tube([(-0.048, 0.0, 0.302), (-0.1, 0.0, 0.262), (-0.088, -0.045, 0.228)], [0.016, 0.015, 0.014]),
          DARK, "armL")
    k.add("handL", ell((-0.082, -0.062, 0.222), (0.022, 0.017, 0.02), rot=(0, 20, 30)), WHITE, "armL")
    # +X: held forward, palm up, showing the way
    k.add("armR", tube([(0.048, 0.0, 0.302), (0.092, -0.035, 0.262), (0.098, -0.095, 0.268)], [0.016, 0.015, 0.014]),
          DARK, "armR")
    k.add("handR", ell((0.1, -0.118, 0.272), (0.023, 0.027, 0.012), rot=(0, -12, 10)), WHITE, "armR")
    for i, (dx, dy) in enumerate(((-0.012, -0.022), (0.006, -0.026), (0.022, -0.012))):
        k.add(f"finger{i}", ell((0.1 + dx, -0.118 + dy, 0.276), (0.008, 0.012, 0.007)), WHITE, "armR")
    def wrists(P):
        return (inside(np.linalg.norm(P - (-0.086, -0.052, 0.225), axis=1) - 0.022, 0.002)
                + inside(np.linalg.norm(P - (0.099, -0.104, 0.27), axis=1) - 0.022, 0.002))
    k.paint(wrists, WHITE, on=["arm", "hand", "finger"])


def head(k):
    def bob(V):
        V = V.copy()
        V[:, 0] *= 1 + 0.06 * np.clip(-V[:, 2], 0, 1)
        return V
    k.add("head", ell(H, HR, deform=bob), DARK, "head")
    # the curls, a ram's horn on each side of the crown
    k.add("curl", tube(CURL, CURL_R, seg=18), HORN, "head", mirror=True)
    k.add("curlfill", ell(*CURL_FILL), HORN, "head", mirror=True)
    def ribs(P):
        out = np.zeros(len(P))
        for s in (1, -1):
            pts = [(s * x, y, z) for x, y, z in CURL]
            t, d = along(P, pts)
            g = inside(np.abs(((t * 6.5) % 1) - 0.5) - 0.12, 0.03)
            out = np.maximum(out, g * inside(d - 0.06, 0.002) * (t > 0.08) * (t < 0.82) * (np.sign(P[:, 0]) == s))
        return out
    k.paint(ribs, HORN_D, on=["curl"])
    # the white lower face: from just under the brows to the chin, the dark coming down to a point between the eyes
    def face(P):
        top = 0.41 - 0.042 * np.exp(-(P[:, 0] / 0.014) ** 2)
        return (inside(P[:, 2] - top, 0.0015) * inside(np.abs(P[:, 0]) - 0.074, 0.002)
                * inside(P[:, 1] + 0.035, 0.003) * smoothstep(0.32, 0.33, P[:, 2]))
    k.paint(face, WHITE, on=["head"])
    # the cyan mark at the dark's point between the eyes
    p, n = on_head(0.0, 0.373)
    k.decal(p, n, (0.017, 0.01), CYAN, eps=0.0016, rings=6, segs=24,
            outline=polygon([(-1, 0.6), (1, 0.6), (0.0, -1.0)]))
    # a white wisp curling up over each eye's outer corner
    W = [(-0.8, -0.7), (0.4, -0.8), (1.0, 0.2), (0.7, 1.0), (0.1, 0.1), (-0.8, 0.0)]
    for s in (1, -1):
        p, n = on_head(s * 0.071, 0.42)
        k.decal(p, n, (0.014, 0.022), WHITE, eps=0.0016, rings=8, segs=36,
                outline=polygon([(a * s, b) for a, b in W]))
    # a small stern mouth
    ci, cw = srgb(INK), srgb(WHITE)
    def mouth(u, v):
        c = np.tile(cw, (len(u), 1))
        c[np.abs(v - (0.2 - 0.9 * (1 - u * u))) < 0.35] = ci
        return c
    p, n = on_head(0.0, 0.347)
    k.decal(p, n, (0.012, 0.007), mouth, eps=0.0016, rings=8, segs=36)


def eye_open():
    ci, cp, cl = srgb(IRIS), srgb(PUPIL), srgb(LIDLINE)
    def look(u, v):
        c = np.tile(ci, (len(u), 1))
        c[np.hypot((u + 0.1) / 0.42, (v + 0.05) / 0.55) < 1] = cp
        c[np.hypot((u - 0.25) / 0.2, (v - 0.12) / 0.18) < 1] = (1, 1, 1)
        c[v > 0.22 + 0.12 * u] = cl
        return c
    return look


def eyes(k, state):
    look = eye_look(state, eye_open(), srgb(WHITE), srgb(LIDLINE), line=0.16)
    CUT = [(-1.2, 0.5), (1.2, 0.68), (1.2, -1.2), (-1.2, -1.2)]
    out_r = lambda th, p=polygon(CUT): min(1.0, p(th))
    out_l = lambda th, p=polygon([(-a, b) for a, b in CUT]): min(1.0, p(th))
    m = np.array([-1, 1, 1.0])
    k.decal(EYE, EYE_N, (0.025, 0.025), look, eps=0.0016, rings=20, segs=56, outline=out_r)
    k.decal(EYE * m, EYE_N * m, (0.025, 0.025), lambda u, v: look(-u, v), eps=0.0016, rings=20, segs=56,
            outline=out_l)


def indeedee(src, dest):
    k = Kit(src, coat="fur")
    body(k)
    arms(k)
    head(k)
    k.build(voxel=0.0045, band=0.004, wband=0.035)
    k.eye_states(lambda state: eyes(k, state))
    k.export(dest)


if __name__ == "__main__":
    indeedee(*args())
