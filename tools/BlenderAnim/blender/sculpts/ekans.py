# Ekans sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/ekans.py -- <the export's Ekans.glb> <out.glb>
# From the references: a purple snake coiled twice on the ground, the inner coil stacked on the outer, a thin yellow
# line along the underside of the coils, faint darker seams across them; its neck rises out of the coil and arches
# forward to a round, flat-topped head with its jaws open wide, pink inside; round yellow eyes with a black slit on top
# of the head; a broad yellow band round the neck; the tail rises behind at the right to a yellow rattle of beads,
# largest at the root.
# The body is laid as milotic.py's: one smooth line through the chain of joints, a tube a segment on its own bone, the
# belly and the seams painted by arc length and by the angle round the line. Written to be grown from (arbok.py): the
# line, its frame, the paints and the eyes are functions taking the line.
# The export's chain: body and tail at (0.12, 0.3, 0.2) behind, seg1 (0.142, -0.155, 0.06) round the outer coil on the
# ground to seg6, seg7 to seg10 the inner coil over it, seg11 (0, -0.08, 0.33) up the neck, head (0, -0.07, 0.42).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from mathutils import Euler
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, eye_look, args, ring_band, outward

PURPLE, SEAM, YELLOW, MOUTH, LIP = "#A870C0", "#82509A", "#EED456", "#E88A9A", "#B8506A"
EYE_YELLOW, INK = "#F2E266", "#1C1420"


# --- the line of the body and the frame carried along it ------------------------------------------------------------
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
    forward where the body rises above `rise`)."""
    def __init__(self, pts, rads, rise=(0.2, 0.3)):
        self.P, self.R = np.array(pts, float), np.array(rads, float)
        self.C, self.S = spline(self.P, per=80)
        self.cum = np.concatenate([[0], np.cumsum(np.linalg.norm(np.diff(self.C, axis=0), axis=1))])
        T = np.gradient(self.C, axis=0); self.T = T / np.linalg.norm(T, axis=1, keepdims=True)
        w = smoothstep(rise[0], rise[1], self.C[:, 2])[:, None]
        D = (1 - w) * np.array([0, 0, -1.0]) + w * np.array([0, -1.0, 0])
        B = D - self.T * (D * self.T).sum(1, keepdims=True)
        self.bel = B / np.linalg.norm(B, axis=1, keepdims=True)

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
        """For points Q: the nearest sample, the arc length there, and how far their way round faces the belly."""
        j = self.near(Q)
        rad = Q - self.C[j]
        t = (rad * self.T[j]).sum(1)
        rad = rad - self.T[j] * t[:, None]
        rad /= np.linalg.norm(rad, axis=1, keepdims=True) + 1e-9
        return j, self.cum[j] + t, (rad * self.bel[j]).sum(1)

    def at(self, s):
        """Point, tangent and radius at the line's parameter s (0 at the first point, len - 1 at the last)."""
        i = int(np.argmin(np.abs(self.S - s)))
        return self.C[i], self.T[i], float(np.interp(s, np.arange(len(self.P)), self.R))


def belly(line, s0, s1, width=0.62, edge=0.003):
    """The belly's stripe between the line's parameters s0 and s1: the points facing the belly by more than width."""
    def mask(Q):
        j, s, d = line.place(Q)
        return inside(width - d, edge) * (line.S[j] > s0) * (line.S[j] < s1)
    return mask


def seams(line, s0, s1, every, half=0.0035, start=0.0):
    """Thin seams across the body every so far along it, between the line's parameters s0 and s1."""
    def mask(Q):
        j, s, d = line.place(Q)
        f = ((s - start) / every) % 1.0
        g = np.minimum(f, 1 - f) * every
        return inside(g - half, 0.0018) * (line.S[j] > s0) * (line.S[j] < s1)
    return mask


# --- the eyes: a round yellow eye with a black slit, a dark rim ------------------------------------------------------
def slit_eye(iris, ink, slit=0.2, rim=0.82):
    I, K = srgb(iris), srgb(ink)
    def look(u, v):
        c = np.tile(I, (len(u), 1))
        c[u * u + v * v > rim * rim] = K
        if slit:
            c[(u / slit) ** 2 + (v / 0.8) ** 2 < 1] = K
        c[((u + 0.36) / 0.13) ** 2 + ((v - 0.36) / 0.15) ** 2 < 1] = srgb("#FFFFFF")
        return c
    return look


def lay_slit(k, centre, normal, half, ink, slit=0.24, mirror=True):
    """The slit pupil, a patch of its own over the eye, so that its edge is a clean lens at any size."""
    k.decal(centre, normal, (half[0] * slit, half[1] * 0.8), ink, eps=0.0075, rings=6, segs=40, mirror=mirror)


# --- Ekans ----------------------------------------------------------------------------------------------------------
LINE = [(0.12, 0.315, 0.3), (0.12, 0.3, 0.2), (0.17, 0.22, 0.09), (0.215, 0.05, 0.057),
        (0.142, -0.155, 0.06), (-0.04, -0.195, 0.069), (-0.171, -0.078, 0.078), (-0.154, 0.087, 0.087),
        (-0.019, 0.164, 0.096), (0.114, 0.104, 0.104), (0.14, -0.029, 0.14), (0.055, -0.12, 0.162),
        (-0.06, -0.105, 0.178), (-0.109, -0.012, 0.188), (-0.06, -0.02, 0.245), (0.0, -0.065, 0.335),
        (0.0, -0.06, 0.42)]
RADS = [0.034, 0.042, 0.05, 0.056, 0.058, 0.058, 0.058, 0.057, 0.056, 0.055, 0.054, 0.053, 0.052, 0.051,
        0.049, 0.048, 0.048]
BONES = ["tail", "body", "body", "body", "seg1", "seg2", "seg3", "seg4", "seg5", "seg6", "seg7", "seg8", "seg9",
         "seg10", "seg10", "seg11"]
HC = np.array([0.0, -0.105, 0.47])          # the head's middle
HS = 1.18                                   # its size: the art's head is a quarter of the height and more


def head(k, hc, scale=1.0):
    """The head: a round, flat-topped skull over a snout, its jaws gaping forward (a carved maw, pink inside)."""
    def sp(p):
        return hc + (np.array(p, float)) * scale
    def flat_top(V):
        V = V.copy()
        t = np.clip(-V[:, 1], 0, 1)
        V[:, 0] *= 1 - 0.18 * t
        return V
    k.add("skull", ell(sp((0, 0, 0)), np.array((0.078, 0.1, 0.058)) * scale, rot=(-6, 0, 0), deform=flat_top),
          PURPLE, "head")
    k.add("back", ell(sp((0, 0.05, -0.03)), np.array((0.064, 0.07, 0.06)) * scale), PURPLE, "head")
    k.add("chin", ell(sp((0, -0.05, -0.075)), np.array((0.056, 0.075, 0.03)) * scale, rot=(28, 0, 0)), PURPLE, "head")
    mc = sp((0, -0.1, -0.045))
    k.add("maw", ell(mc, np.array((0.05, 0.085, 0.032)) * scale, rot=(16, 0, 0)), MOUTH, "head", carve=True)
    return mc


def build(src, out):
    k = Kit(src, coat="scales")
    line = Line(LINE, RADS)
    line.tubes(k, BONES, PURPLE)
    # the neck carried up into the back of the head
    k.add("nape", tube([LINE[-2], LINE[-1], HC + (0, 0.04, -0.02)], [0.048, 0.048, 0.05]), PURPLE, "head")
    head(k, HC, HS)

    # the yellow line under the coils and along the tail's front, faint seams across the coils
    k.paint(belly(line, 2.6, 12.9, width=0.6), YELLOW, on=["seg"])
    k.paint(seams(line, 2.6, 13.2, 0.1, start=0.03), SEAM, on=["seg"])

    # the yellow band round the neck, a loose ring
    c, t, r = line.at(14.55)
    k.add("band", ring_band(c, t, r + 0.005, 0.052, thick=0.007), YELLOW, "seg10", fuse=False)

    # the rattle: yellow beads up from the tail's end, largest at the root
    tip = np.array(LINE[0])
    z, prev = tip[2] - 0.01, None
    for i, rr in enumerate([0.034, 0.03, 0.026, 0.022, 0.018, 0.014]):
        z += rr * (1.0 if i == 0 else 1.55)
        p = np.array([tip[0], tip[1] + 0.012 * i - 0.004 * i * i * 0.2, z])
        k.add(f"bead{i}", ell(p, (rr, rr, rr * 0.82), sub=3), YELLOW, "tail", fuse=False)
        z += rr * 0.2

    # the eyes: on top of the head, looking up, out and forward
    EC = HC + np.array([0.042, -0.035, 0.04]) * HS
    de = unit(0.55, -0.4, 0.75)
    lid = srgb(PURPLE)
    half = (0.028 * HS, 0.028 * HS)
    def eyes(state):
        # open, the slit is a patch of its own; under Fierce's lid it is drawn in the eye
        look = slit_eye(EYE_YELLOW, INK, slit=0.24 if state == "Fierce" else None)
        k.decal(EC, de, half, eye_look(state, look, lid, srgb(INK), line=0.18), eps=0.005,
                rings=20, segs=56, mirror=True)
        if state == "Open":
            lay_slit(k, EC, de, half, INK)

    k.build(voxel=0.006, smooth=4)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    build(*args())
