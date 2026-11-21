# Sizzlipede sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/sizzlipede.py -- <the export's Sizzlipede.glb> <out.glb>
#
# From the references: a long red centipede, round in section. Its rear half lies on the ground with a dark maroon
# back of three plates (red down its sides), its tail's end lifting a little with an orange tuft; its front third
# rises in an S, red all round, to a head held forward, a little drooping. Thin dark lines between the segments.
# Hollow yellow rings on the sides of the risen segments. The head: a rounded wedge, big yellow eyes with a dark rim
# and a dark slit, their tops cut by an angry brow sloping down to the middle; two orange fangs forking down and out
# under the snout. Short red spikes for legs down both sides.
# (The task's text expected glowing belly rings and whiskers: the pictures show hollow yellow rings on the sides and
# orange fangs, no whiskers.)
# Skeleton: body (0.08, 0.22, 0.035), seg1 (0.1, 0.08, 0.035), seg2 (0, -0.04, 0.04), head (0, -0.13, 0.08): the
# body runs back and out to +X behind, as the game's own sculpt lays it.
#
# Written to be grown from (centiskorch.py): the centre line with its frame, the paints by arc length, the rings,
# legs and eyes are functions taking the line.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, inside, smoothstep, unit, eye_look, polygon, outward, args

RED, BACK, SEAM, BACKSEAM = "#DC3A30", "#5C2A2A", "#8E2222", "#341616"
YELLOW, INK, ORANGE, EYEY = "#F4C23A", "#1E1214", "#F5962E", "#F6D23E"


# --- the line of the body and the frame carried along it (as ekans.py's) ------------------------------------------
def spline(pts, per=12):
    pts = np.array(pts, float)
    ext = np.vstack([2 * pts[0] - pts[1], pts, 2 * pts[-1] - pts[-2]])
    C, S = [], []
    for i in range(len(pts) - 1):
        p0, p1, p2, p3 = ext[i], ext[i + 1], ext[i + 2], ext[i + 3]
        for j in range(per):
            t = j / per
            C.append(0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t
                            + (-p0 + 3 * p1 - 3 * p2 + p3) * t ** 3))
            S.append(i + t)
    C.append(pts[-1]); S.append(len(pts) - 1)
    return np.array(C), np.array(S)


class Line:
    """A body's centre line: a fine spline with arc length and the belly's way at each point (down on the ground,
    forward, -Y, where the body rises above `rise`)."""
    def __init__(self, pts, rads, rise=(0.2, 0.3)):
        self.P, self.R = np.array(pts, float), np.array(rads, float)
        self.C, self.S = spline(self.P, per=80)
        self.cum = np.concatenate([[0], np.cumsum(np.linalg.norm(np.diff(self.C, axis=0), axis=1))])
        T = np.gradient(self.C, axis=0); self.T = T / np.linalg.norm(T, axis=1, keepdims=True)
        w = smoothstep(rise[0], rise[1], self.C[:, 2])[:, None]
        D = (1 - w) * np.array([0, 0, -1.0]) + w * np.array([0, -1.0, 0])
        B = D - self.T * (D * self.T).sum(1, keepdims=True)
        self.bel = B / np.linalg.norm(B, axis=1, keepdims=True)
        self.side = np.cross(self.T, self.bel)          # +X where the line runs forward (-Y) on the ground

    def tubes(self, k, bones, color, prefix="seg"):
        crv, seg = spline(self.P, per=14)
        rad = np.interp(seg, np.arange(len(self.P)), self.R)
        for i, bone in enumerate(bones):
            sel = (seg >= i - 0.15) & (seg <= i + 1.15)
            k.add(f"{prefix}{i}", tube(crv[sel][::2], rad[sel][::2], seg=20, per=5), color, bone)

    def near(self, Q):
        return np.concatenate([((Q[a:a + 3000, None, :] - self.C[None]) ** 2).sum(2).argmin(1)
                               for a in range(0, len(Q), 3000)])

    def place(self, Q):
        """For points Q: the nearest sample, the arc length there, how far their way round faces the belly, and how far
        it faces the +side."""
        j = self.near(Q)
        rad = Q - self.C[j]
        t = (rad * self.T[j]).sum(1)
        rad = rad - self.T[j] * t[:, None]
        rad /= np.linalg.norm(rad, axis=1, keepdims=True) + 1e-9
        return j, self.cum[j] + t, (rad * self.bel[j]).sum(1), (rad * self.side[j]).sum(1)

    def at(self, s):
        """Point, tangent, belly, side and radius at the line's parameter s (0 at the first point)."""
        i = int(np.argmin(np.abs(self.S - s)))
        return self.C[i], self.T[i], self.bel[i], self.side[i], float(np.interp(s, np.arange(len(self.P)), self.R))


def top(line, s0, s1, width=-0.18, edge=0.0015):
    """The back between the line's parameters s0 and s1: the points facing away from the belly by more than width."""
    def mask(Q):
        j, s, d, e = line.place(Q)
        return inside(d - width, edge) * (line.S[j] > s0) * (line.S[j] < s1)
    return mask


def front(line, s0, s1, width=0.3, edge=0.002):
    """The belly between s0 and s1: the points facing the belly by more than width."""
    def mask(Q):
        j, s, d, e = line.place(Q)
        return inside(width - d, edge) * (line.S[j] > s0) * (line.S[j] < s1)
    return mask


def seams(line, s0, s1, every, half=0.0016, start=0.0, edge=0.0008):
    """Thin seams across the body every so far along it, between the line's parameters s0 and s1."""
    def mask(Q):
        j, s, d, e = line.place(Q)
        f = ((s - start) / every) % 1.0
        g = np.minimum(f, 1 - f) * every
        return inside(g - half, edge) * (line.S[j] > s0) * (line.S[j] < s1)
    return mask


def ring_look(ground, ring, inner=0.55, outer=0.95):
    G, Y = srgb(ground), srgb(ring)
    def look(u, v):
        r = np.sqrt(u * u + v * v)
        c = np.tile(G, (len(u), 1))
        c[(r > inner) & (r < outer)] = Y
        return c
    return look


def side_mark(k, line, s, half, look, lift=0.002, tilt=0.0, both=True):
    """A patch on the side of the body at the line's parameter s (on both sides), tilted toward the belly by tilt."""
    c, t, b, sd, r = line.at(s)
    for sg in ((1, -1) if both else (1,)):
        n = sd * sg + b * tilt
        n /= np.linalg.norm(n)
        k.decal(c + n * r * 0.95, n, half, look, up=-b if abs(np.dot(-b, n)) < 0.9 else t, eps=lift, rings=14,
                segs=48)


def spike(k, name, base, tip, w, bone, color, along=(0, 0, 1.0)):
    """A short solid spike from a small base round base to tip (a leg, a fang): a flat-faced hull."""
    base, tip = np.array(base, float), np.array(tip, float)
    d = tip - base; d /= np.linalg.norm(d)
    a = np.cross(d, along)
    if np.linalg.norm(a) < 1e-3:
        a = np.cross(d, (1.0, 0, 0))
    a /= np.linalg.norm(a)
    b = np.cross(d, a)
    pts = [base + a * w, base - a * w, base + b * w * 0.8, base - b * w * 0.8, base - d * w * 0.5, tip]
    k.add(name, hull(pts), color, bone, fuse=False, surface="shell")


def legs(k, line, params, bone_of, color, length, w, out=0.75, down=0.55, back=0.25):
    """A pair of short spikes down the sides at each parameter, from under the side pointing down, out and back."""
    for i, s in enumerate(params):
        c, t, b, sd, r = line.at(s)
        for sg, nm in ((1, "R"), (-1, "L")):
            base = c + sd * sg * r * 0.7 + b * r * 0.55
            d = sd * sg * out + b * down - t * back
            d /= np.linalg.norm(d)
            spike(k, f"leg{i}{nm}", base, base + d * length, w, bone_of(s), color, along=t)


# --- the eyes: yellow, a dark rim, a dark slit toward the nose, the top cut by a brow sloping to the middle -----------
def cut_eye(low=0.08, slope=0.34):
    pts = []
    for th in np.linspace(0, 2 * math.pi, 96, endpoint=False):
        u, v = math.cos(th), math.sin(th)
        lim = low + slope * u
        if v > lim:
            q = v - slope * u
            tt = low / q if q > 1e-6 else 1.0
            u, v = u * tt, v * tt
        pts.append((u, v))
    r_, l_ = polygon(pts), polygon([(-a, b) for a, b in pts[::-1]])
    return (lambda th: min(r_(th), 1.0)), (lambda th: min(l_(th), 1.0))


EYER, EYEL = cut_eye()


def eye_open(outline, iris=EYEY, ink=INK, rim=0.8):
    I, K = srgb(iris), srgb(ink)
    def look(u, v):
        reach = np.array([outline(t) for t in np.arctan2(v, u)])
        rho = np.sqrt(u * u + v * v) / reach
        c = np.tile(I, (len(u), 1))
        c[rho > rim] = K
        return c
    return look


def eyes(k, hc, hr, d, half, lid, slit=(0.26, 0.62), eps=0.0022, outline=(EYER, EYEL)):
    """The two eyes on the ellipsoid (hc, hr) in the way d (for +X; mirrored), with the slit a patch of its own."""
    def lay(state):
        for sg in (1, -1):
            m = np.array([sg, 1, 1.0])
            dd = unit(*(np.array(d) * m))
            p = hc + dd / math.sqrt(((dd / hr) ** 2).sum())
            n = (p - hc) / hr ** 2; n /= np.linalg.norm(n)
            ol = outline[0] if sg > 0 else outline[1]
            look = eye_look(state, eye_open(outline[0]), srgb(lid), srgb(INK), line=0.17)
            col = look if sg > 0 else (lambda a, b, f=look: f(-a, b))
            k.decal(p, n, half, col, eps=eps, rings=20, segs=56, outline=ol)
            if state in ("Open", "Fierce"):
                r_ = np.cross((0, 0, 1.0), n); r_ /= np.linalg.norm(r_)
                pc = p - r_ * half[0] * 0.18 * sg - np.array((0, 0, half[1] * (0.12 if state == "Open" else 0.3)))
                h = (half[0] * slit[0], half[1] * (slit[1] if state == "Open" else slit[1] * 0.55))
                k.decal(pc, n, h, INK, eps=eps * 1.8, rings=6, segs=32)
    return lay


# --- Sizzlipede ---------------------------------------------------------------------------------------------------
LINE = [(0.05, 0.33, 0.052), (0.075, 0.27, 0.04), (0.092, 0.2, 0.037), (0.1, 0.125, 0.037), (0.088, 0.055, 0.038),
        (0.045, -0.005, 0.046), (0.012, -0.045, 0.08), (0.0, -0.08, 0.118), (0.0, -0.118, 0.126)]
RADS = [0.022, 0.031, 0.036, 0.038, 0.038, 0.037, 0.035, 0.033, 0.032]
BONES = ["body", "body", "body", "seg1", "seg1", "seg2", "seg2", "head"]
HC, HR = np.array([0.0, -0.152, 0.122]), np.array([0.05, 0.062, 0.039])


def bone_of(s):
    return "body" if s < 3 else "seg1" if s < 5 else "seg2" if s < 7 else "head"


def head(k):
    def droop(V):
        V = V.copy()
        V[:, 2] -= 0.18 * np.clip(-V[:, 1], 0, 1) ** 2      # the snout a little lower
        V[:, 0] *= 1 - 0.12 * np.clip(-V[:, 1], 0, 1)
        return V
    k.add("head", ell(HC, HR, deform=droop), RED, "head")


def fangs(k):
    """Two orange fangs under the snout, each forking down and out (a long prong and a short one inside it)."""
    for sg, nm in ((1, "R"), (-1, "L")):
        m = np.array([sg, 1, 1.0])
        P = np.array([(0.014, -0.196, 0.1), (0.03, -0.207, 0.083), (0.044, -0.204, 0.064)]) * m
        k.add(f"fang{nm}", outward(tube(P, [0.008, 0.0065, 0.0025], seg=12, per=6)), ORANGE, "head", fuse=False,
              surface="shell")
        Q = np.array([(0.02, -0.203, 0.09), (0.018, -0.214, 0.074), (0.012, -0.214, 0.062)]) * m
        k.add(f"prong{nm}", outward(tube(Q, [0.006, 0.005, 0.002], seg=12, per=6)), ORANGE, "head", fuse=False,
              surface="shell")


def tuft(k):
    """The orange tuft at the tail's end: three short tongues up and back."""
    tip = np.array(LINE[0])
    for i, d in enumerate([(0.0, 0.6, 0.8), (0.5, 0.5, 0.7), (-0.4, 0.6, 0.7)]):
        d = unit(*d)
        a = np.cross(d, (1.0, 0, 0)); a /= np.linalg.norm(a)
        b = np.cross(d, a)
        base = tip + d * 0.008
        pts = [base + a * 0.011, base - a * 0.011, base + b * 0.008, base - b * 0.008, base + d * 0.042]
        k.add(f"tuft{i}", hull(pts), ORANGE, "body", fuse=False, surface="shell")


def sizzlipede(src, out_path):
    k = Kit(src, coat="shell")
    line = Line(LINE, RADS, rise=(0.07, 0.11))
    line.tubes(k, BONES, RED)
    head(k)
    # the dark back of the rear half, its plates split by darker seams; thin dark seams between the risen segments
    k.paint(top(line, -1, 4.6), BACK, on=["seg"])
    k.paint(lambda Q: seams(line, 0.6, 4.6, 0.075, start=0.02)(Q) * top(line, -1, 4.6, width=-0.12)(Q), BACKSEAM,
            on=["seg"])
    k.paint(seams(line, 4.6, 7.3, 0.05, start=0.02), SEAM, on=["seg"])
    # hollow yellow rings on the sides of the risen segments
    for s in (5.35, 6.35):
        side_mark(k, line, s, (0.019, 0.019), ring_look(RED, YELLOW), tilt=0.0)
    legs(k, line, (0.9, 1.7, 2.5, 3.3, 4.1, 4.9, 5.6), bone_of, RED, 0.034, 0.0085)
    fangs(k)
    tuft(k)
    k.build(voxel=0.0022, smooth=3, band=0.003, wband=0.02)
    k.eye_states(eyes(k, HC, HR, (0.5, -0.78, 0.36), (0.02, 0.017), RED))
    k.export(out_path)


if __name__ == "__main__":
    sizzlipede(*args())
