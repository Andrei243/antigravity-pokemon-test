# Honedge sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/honedge.py -- <the export's Honedge.glb> <out.glb>
# The base of its line: doublade.py and aegislash.py import this frame with sibling("honedge") (a sword's frame, its
# scabbard, its guard, its three rings with the eye on one, its grip and pommel, its mouth and teeth, and its cloth).
#
# From the references: a sword in its scabbard, hovering point down and leaning a little to the right. The scabbard is
# a broad, flat, brown blade-shaped sheath, its left edge bowed, its point rounded; on its face a pale tan knot (a long
# pointed lens with a loop over its top) with dark grey in the lens's lower half. Over the scabbard's mouth the hilt,
# pale khaki: a flat crossguard with pointed ends rising a little, and in front of it three round rings set like
# a trefoil, the two lower ones white with a black screw slot across, the top one the eye, blue with a black slit
# pupil. Between the lower rings a dark mouth with two white teeth. A short grip, a collar and a small blue cap at the
# pommel, from which a blue cloth rises, arcs over to the right and hangs down beside the sword, darkest at its root,
# a lighter curl on its lower part, and ending in four light blue tassels. The brief's "one eye on its hilt" is the top
# ring; the two lower rings are drawn as screw heads in both pictures.
# Skeleton: root (0, 0, 0) and body (0, 0, 0.311) alone: every part is on the body.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, sheet, slab, hull, outward, srgb, inside, unit, eye_look, polygon, args

SCAB, KNOT, HOLLOW = "#86705A", "#CDB884", "#54504E"
HILT, POMMEL = "#D2C290", "#2E6AAE"
BOLT, INK, MOUTH, TOOTH = "#F2F1EC", "#1A1716", "#3A3436", "#F4F3EE"
IRIS = "#3EA8E0"
CLOTH = ("#2E6AAE", "#3C8ED4", "#7CC8F0")      # the cloth's root, its middle (with the curl) and its end
CURL = "#8ED4F4"

L = 0.5             # the sword's length from point to pommel; every place below is for this length


# --- a sword's frame ------------------------------------------------------------------------------------------------
class Frame:
    """A sword's own axes: s along it from its point (d), t across it (e), y through it toward the back (j);
    e x j = d, so a shape made in (t, y, s) keeps its winding. f scales every length of the sword."""
    def __init__(self, point, d, f=1.0):
        self.o = np.array(point, float)
        self.d = unit(*d)
        j = np.array([0, 1.0, 0]) - self.d * self.d[1]
        self.j = j / np.linalg.norm(j)
        self.e = np.cross(self.j, self.d)
        self.f = f
        self.M = np.stack([self.e, self.j, self.d])

    def at(self, t, y, s):
        return self.o + (t * self.e + y * self.j + s * self.d) * self.f

    def put(self, shape):
        V, F = shape
        return self.o + (V * self.f) @ self.M, F

    def local(self, P):
        q = (P - self.o) / self.f
        return q @ self.e, q @ self.j, q @ self.d


def puck(c, r, h, n=6):
    """A ring of the hilt: a round flat-faced puck in the sword's plane (local y its axis)."""
    def f(V):
        rho = np.sqrt(V[:, 0] ** 2 + V[:, 2] ** 2)
        return V / ((rho ** n + np.abs(V[:, 1]) ** n) ** (1 / n))[:, None]
    return ell(c, (r, h, r), deform=f)


# --- the scabbard ---------------------------------------------------------------------------------------------------
SC, SH, SW, ST = 0.155, 0.155, 0.068, 0.018       # its middle along the sword, half its length, width and thickness


def scabbard_shape(bow=0.22, point=0.8):
    def f(V):
        V = V / ((np.abs(V) ** 4).sum(1, keepdims=True)) ** 0.25
        w = np.sqrt(np.clip((V[:, 2] + 1) / point, 0, 1))
        V = V.copy()
        V[:, 0] = V[:, 0] * w - bow * (1 - V[:, 2] ** 2)
        V[:, 1] = V[:, 1] * (0.55 + 0.45 * w)
        return V
    return ell((0, 0, SC), (SW, ST, SH), deform=f)


def honedge_knot(k, fr, name):
    """The pale knot on Honedge's scabbard: a long pointed lens, dark in its lower half, with a loop over its top."""
    def lens(P):
        t, y, s = fr.local(P)
        ss = s - (SC - 0.01)
        hl, hw = 0.12, 0.046
        return np.abs(t) - hw * (1 - (ss / hl) ** 2) + 9 * (np.abs(ss) > hl), ss, t, y
    def dark(P):
        d, ss, t, y = lens(P)
        return inside(d + 0.006, 0.0015) * inside(ss, 0.002)
    def band(P):
        d, ss, t, y = lens(P)
        return inside(np.abs(d) - 0.0055, 0.0015) * inside(np.abs(ss) - 0.118, 0.002)
    def loop(P):
        t, y, s = fr.local(P)
        q = np.hypot(t, s - (SC + 0.05))
        return inside(np.abs(q - 0.036) - 0.006, 0.0015)
    for m, col in ((dark, HOLLOW), (band, KNOT), (loop, KNOT)):
        k.paint(m, col, on=[name])


# --- the hilt -------------------------------------------------------------------------------------------------------
RING_R, RING_H = 0.04, 0.013
RING_Y = -ST - 0.006                     # the rings stand in front of the scabbard's face
LOWER = ((0.056, 0.292), (-0.056, 0.292))
TOP = (0.0, 0.362)
GUARD = [(-0.138, 0.348), (-0.1, 0.306), (-0.045, 0.284), (0.045, 0.284), (0.1, 0.306), (0.138, 0.348),
         (0.06, 0.329), (-0.06, 0.329)]


def rings(k, fr, colour, top=TOP, lower=LOWER, name="ring"):
    """The three rings, each a fused puck; returns their centres' fronts and the sword's front normal."""
    fronts = []
    for i, (t, s) in enumerate([top, *lower]):
        k.add(f"{name}{i}", fr.put(puck((t, RING_Y, s), RING_R, RING_H)), colour, "body")
        fronts.append(fr.at(t, RING_Y - RING_H, s))
    return fronts


def bolt(k, fr, centre, turn, half=0.029):
    """A white ring's face: a white disc in a dark rim with a black screw slot across it."""
    CW, CK = srgb(BOLT), srgb(INK)
    a = math.radians(turn)
    def look(u, v):
        r = np.hypot(u, v)
        c = np.tile(CW, (len(u), 1))
        c[r > 0.8] = CK
        w = -u * math.sin(a) + v * math.cos(a)
        c[(np.abs(w) < 0.17) & (r < 0.8)] = CK
        return c
    k.decal(centre, -fr.j, (half * fr.f, half * fr.f), look, up=fr.d, eps=0.003, rings=16, segs=56)


def eye_open(iris=IRIS, lid=HILT):
    """The open eye: a coloured disc in a dark rim, a black slit pupil upright, a white glint."""
    CI, CK, CL, CW = srgb(iris), srgb(INK), srgb(lid), srgb("#FFFFFF")
    def look(u, v):
        r = np.hypot(u, v)
        c = np.tile(CI, (len(u), 1))
        c[r > 0.76] = CK
        c[r > 0.94] = CL
        c[(np.abs(u) < 0.15 * np.sqrt(np.clip(1 - (v / 0.7) ** 2, 0, 1)) + 0.02) & (np.abs(v) < 0.68)] = CK
        c[((u - 0.32) / 0.15) ** 2 + ((v - 0.34) / 0.15) ** 2 < 1] = CW
        return c
    return look


def lay_eye(k, fr, centre, state, iris=IRIS, lid=HILT, half=0.033):
    look = eye_look(state, eye_open(iris, lid), srgb(lid), srgb(INK), line=0.16)
    k.decal(centre, -fr.j, (half * fr.f, half * fr.f), look, up=fr.d, eps=0.004, rings=20, segs=56)


def mouth(k, fr, s=0.268):
    """The dark mouth between the lower rings, two white teeth rising from its lower edge."""
    c = fr.at(0, -ST, s)
    k.decal(c, -fr.j, (0.022 * fr.f, 0.03 * fr.f), MOUTH, up=fr.d, eps=0.003, rings=8, segs=48,
            outline=polygon([(-1, -0.75), (1, -0.75), (0.32, 1), (-0.32, 1)]))
    for dt in (-0.0095, 0.0095):
        tc = fr.at(dt, -ST, s - 0.012)
        k.decal(tc, -fr.j, (0.0085 * fr.f, 0.011 * fr.f), TOOTH, up=fr.d, eps=0.0045, rings=6, segs=36,
                outline=polygon([(-1, -1), (1, -1), (0, 1)]))


def guard(k, fr, outline, colour, thick=0.022, y=0.004, name="guard"):
    """The crossguard behind the rings: a flat hard-edged plate in the sword's plane, outline in (t, s)."""
    pts = [(a * fr.f, b * fr.f) for a, b in outline]
    k.add(name, slab(pts, fr.at(0, y, 0), fr.e, fr.d, thick * fr.f), colour, "body", fuse=False)


def grip(k, fr, top=0.465, cap=POMMEL):
    k.add("grip", fr.put(tube([(0, 0, 0.35), (0, 0, top)], [0.017, 0.016])), HILT, "body")
    k.add("collar", fr.put(ell((0, 0, top), (0.023, 0.023, 0.012))), HILT, "body")
    k.add("pommel", fr.put(ell((0, 0, top + 0.018), (0.018, 0.018, 0.016))), cap, "body")
    return fr.at(0, 0, top + 0.03)


# --- the cloth ------------------------------------------------------------------------------------------------------
def curve(pts, n=200):
    """Points along a Catmull-Rom curve through pts, and their unit tangents."""
    p = np.array(pts, float)
    ext = np.vstack([2 * p[0] - p[1], p, 2 * p[-1] - p[-2]])
    P = []
    for i in range(len(p) - 1):
        p0, p1, p2, p3 = ext[i], ext[i + 1], ext[i + 2], ext[i + 3]
        for t in np.linspace(0, 1, n // (len(p) - 1), endpoint=False):
            P.append(0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t
                            + (-p0 + 3 * p1 - 3 * p2 + p3) * t ** 3))
    P.append(p[-1])
    P = np.array(P)
    T = np.gradient(P, axis=0)
    T /= np.linalg.norm(T, axis=1, keepdims=True)
    return P, T


class Cloth:
    """A ribbon along a path, its face to the front (-Y), its width a function of u along it."""
    def __init__(self, pts, width, thick=0.017):
        self.P, self.T = curve(pts)
        self.n = len(self.P)
        self.width, self.thick = width, thick
        face = np.array([0, -1.0, 0])
        A = np.cross(face, self.T)
        self.A = A / np.linalg.norm(A, axis=1, keepdims=True)
        self.face = face

    def _at(self, u):
        x = np.clip(u, 0, 1) * (self.n - 1)
        i = np.clip(np.floor(x).astype(int), 0, self.n - 2)
        f = (x - i)[..., None]
        P = self.P[i] * (1 - f) + self.P[i + 1] * f
        A = self.A[i] * (1 - f) + self.A[i + 1] * f
        T = self.T[i] * (1 - f) + self.T[i + 1] * f
        return P, A / np.linalg.norm(A, axis=-1, keepdims=True), T / np.linalg.norm(T, axis=-1, keepdims=True)

    def piece(self, u0, u1, nu=40):
        def at(u, v):
            uu = u0 + (u1 - u0) * u
            P, A, T = self._at(uu)
            return P + A * ((v - 0.5) * self.width(uu))[..., None]
        return outward(sheet(at, nu, 5, self.thick))

    def curl(self, u, r, turns=1.6, tube_r=0.0095, back=False):
        """A spiral lying on the cloth's face at u (and on its back), its outer end open toward the cloth's root."""
        P, A, T = self._at(np.array([u]))
        P, A, T = P[0], A[0], T[0]
        side = 1 if not back else -1
        lift = self.face * side * (self.thick / 2 + tube_r * 0.55)
        th = np.linspace(0, turns * 2 * math.pi, 40)
        rr = r * (1 - th / (turns * 2 * math.pi) * 0.82)
        pts = [P + lift + rho * (math.cos(a) * A - math.sin(a) * T) for a, rho in zip(th, rr)]
        return outward(tube(pts, [tube_r] * len(pts), seg=10, per=3))

    def tassels(self, n, length, spread=0.0):
        """n tassels hanging from the cloth's end, side by side across it."""
        P, A, T = self._at(np.array([1.0]))
        P, A, T = P[0], A[0], T[0]
        W = self.width(1.0)
        w = W / n * 0.4
        out = []
        for i in range(n):
            x = (i + 0.5) / n - 0.5
            d = unit(*(T + A * x * spread))
            ex = np.cross(d, self.face); ex /= np.linalg.norm(ex)
            L = length * (1 - 0.12 * abs(x) * 2)
            o = P + A * x * W - T * 0.003
            out.append(slab([(-w, 0), (w, 0), (w * 0.9, L), (0, L + w * 0.6), (-w * 0.9, L)], o, ex, d, self.thick))
        return out


def cloth(k, cl, cuts, colours, curl_u, curl_r, tassels, length, spread=0.0, curl_colour=CURL, tassel_colour=None):
    """A cloth in pieces of colour cut at u = cuts, a curl on its middle piece, tassels at its end."""
    edges = [0.0, *cuts, 1.0]
    for i, col in enumerate(colours):
        k.add(f"cloth{i}", cl.piece(edges[i], edges[i + 1]), col, "body", fuse=False, surface="cloth")
    k.add("curl", cl.curl(curl_u, curl_r), curl_colour, "body", fuse=False, surface="cloth")
    k.add("curlback", cl.curl(curl_u, curl_r, back=True), curl_colour, "body", fuse=False, surface="cloth")
    for i, sl in enumerate(cl.tassels(tassels, length, spread)):
        k.add(f"tassel{i}", sl, tassel_colour or colours[-1], "body", fuse=False, surface="cloth")


# --- Honedge ----------------------------------------------------------------------------------------------------------
def honedge(src, out):
    k = Kit(src, coat="metal")
    fr = Frame((-0.03, 0.0, 0.075), (math.sin(math.radians(8)), 0, math.cos(math.radians(8))))

    k.add("scab", fr.put(scabbard_shape()), SCAB, "body")
    honedge_knot(k, fr, "scab")
    fronts = rings(k, fr, HILT)
    top = grip(k, fr)
    guard(k, fr, GUARD, HILT)

    w = lambda u: 0.03 + 0.11 * np.clip(u, 0, 1) ** 1.5
    p = top
    cl = Cloth([p + (0, 0.014, -0.012), p + (0.006, 0.016, 0.03), p + (0.06, 0.022, 0.05), p + (0.125, 0.028, -0.005),
                p + (0.155, 0.032, -0.11), p + (0.155, 0.034, -0.22), p + (0.15, 0.034, -0.3)], w)
    cloth(k, cl, (0.58, 0.84), CLOTH, 0.72, 0.04, 4, 0.14, spread=0.2)

    for c, turn in zip(fronts[1:], (20, -15)):
        bolt(k, fr, c, turn)
    mouth(k, fr)
    k.build(voxel=0.004, smooth=3, band=0.005)

    def lay(state):
        lay_eye(k, fr, fronts[0], state)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    honedge(*args())
