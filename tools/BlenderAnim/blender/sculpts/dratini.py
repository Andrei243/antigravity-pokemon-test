# Dratini sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/dratini.py -- <the export's Dratini.glb> <out.glb>
# From the references: a small pale-blue serpent lying in a sweep on the ground, its tail running out thin behind to
# the left, its neck rising from the front of the sweep to a big round head held up; the whole underside white, meeting
# the blue in a clean line along the flank and running up the front of the neck; a large round white snout bulging
# from the lower front of the head; big dark eyes, purple below, a white glint; a small white dot on the forehead; two
# white fins for ears standing out from the sides of the head, swept up and back, their back edges cut into three points.
# The body is laid as ekans.py's: one smooth line through the chain of joints, a tube a stretch on its own bone, the belly
# painted by the angle round the line. Written to be grown from (dragonair.py imports it with sibling): the line, its
# frame, the belly and the eye are functions taking their numbers.
# The export's chain: body (-0.3, 0.3, 0.02) at the tail's root behind on the left, seg1 to seg6 round the sweep on the
# ground to the front, seg7 to seg10 up the neck, head (0, -0.06, 0.42), ears (+-0.06, -0.06, 0.5).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, srgb, inside, smoothstep, unit, eye_look, args, outward

BLUE, WHITE, INK, PURPLE = "#93AEEA", "#ECEEF4", "#1A1424", "#6E4A8E"


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
    def __init__(self, pts, rads, rise=(0.1, 0.2)):
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


def belly(line, s0, s1, ground=-0.25, risen=0.05, rise=(0.1, 0.2), edge=0.0045):
    """The white underside between the line's parameters s0 and s1: the points facing the belly by more than `ground`
    where the body lies (so the lower half of the flank is white) and `risen` up the neck (the front half)."""
    def mask(Q):
        j, s, d = line.place(Q)
        w = np.clip((line.C[j, 2] - rise[0]) / (rise[1] - rise[0]), 0, 1)
        width = ground + (risen - ground) * w
        return inside(width - d, edge) * (line.S[j] > s0) * (line.S[j] < s1)
    return mask


# --- the eyes: big and dark, purple low down, a white glint high on the outer side ------------------------------------
def dark_eye(ink=INK, low=PURPLE, glint="#FFFFFF", deep=0.1):
    K, L, G = srgb(ink), srgb(low), srgb(glint)
    def look(u, v):
        c = np.tile(K, (len(u), 1))
        c[(v < -deep) & (u * u + v * v < 0.8 ** 2)] = L
        c[((u - 0.28) / 0.26) ** 2 + ((v - 0.34) / 0.28) ** 2 < 1] = G
        return c
    return look


def ear_frame(along, face):
    """A plate's frame: along its length, and the way its face is turned (the plate's normal)."""
    face = np.array(face, float) - along * (np.array(face, float) @ along)
    face /= np.linalg.norm(face)
    return along, np.cross(face, along)


def fin_ear(k, root, ex, ez, scale, color, bone, thick=0.02, name="ear", mirror=True):
    """A fin standing out from the head: a plate from root along ex, its back edge (ez) cut into three points."""
    OUT = [(0.0, -0.02), (0.05, -0.036), (0.1, -0.048), (0.132, -0.055), (0.108, -0.02), (0.122, 0.002), (0.094, 0.016),
           (0.1, 0.042), (0.066, 0.034), (0.03, 0.028), (0.0, 0.02)]
    k.add(name, slab([(a * scale, b * scale) for a, b in OUT], root, ex, ez, thick), color, bone, fuse=False,
          mirror=mirror)


# --- Dratini --------------------------------------------------------------------------------------------------------
LINE = [(-0.4, 0.266, 0.017), (-0.3, 0.3, 0.02), (-0.19, 0.337, 0.026), (-0.043, 0.316, 0.034), (0.1, 0.22, 0.04),
        (0.144, 0.07, 0.044), (0.115, -0.052, 0.046), (0.04, -0.12, 0.05), (0.008, -0.127, 0.134),
        (-0.003, -0.109, 0.227), (0.0, -0.08, 0.32), (0.0, -0.064, 0.396), (0.0, -0.065, 0.43)]
RADS = [0.007, 0.016, 0.023, 0.03, 0.036, 0.04, 0.044, 0.046, 0.046, 0.045, 0.044, 0.044, 0.045]
BONES = ["body", "body", "seg1", "seg2", "seg3", "seg4", "seg5", "seg6", "seg7", "seg8", "seg9", "seg10"]
H = np.array([0.0, -0.088, 0.49])          # the head's middle
HR = np.array([0.093, 0.09, 0.087])          # its radii: a ball twice the neck's breadth


def on_head(d, h=H, r=HR):
    d = np.array(d, float); d /= np.linalg.norm(d)
    return h + d * r


def dratini(src, out):
    k = Kit(src, coat="scales")
    line = Line(LINE, RADS)
    line.tubes(k, BONES, BLUE)
    k.paint(belly(line, 0.0, 11.6), WHITE, on=["seg"])

    # The head: a round ball, a little fuller behind, held level on the neck
    k.add("nape", tube([LINE[-2], LINE[-1], H + (0, 0.03, -0.02)], [0.044, 0.046, 0.052]), BLUE, "head")
    k.add("skull", ell(H, HR), BLUE, "head")
    k.add("back", ell(H + (0, 0.032, -0.008), (0.083, 0.078, 0.078)), BLUE, "head")
    # The snout: a big round white bulb on the lower front, bulging forward and a little down
    k.add("snout", ell(H + (0, -0.085, -0.035), (0.07, 0.058, 0.054), rot=(-8, 0, 0)), WHITE, "head")

    # The ears: white fins out from the sides, up and swept back, their faces turned to the front three-quarter
    ex, ez = ear_frame(unit(0.6, 0.18, 0.78), unit(0.4, -0.9, 0.05))
    fin_ear(k, (0.06, -0.075, 0.525), ex, ez, 1.1, WHITE, "ear{s}", thick=0.022)

    # The dot on the forehead
    k.decal(on_head((0, -0.55, 0.85)), unit(0, -0.55, 0.85), (0.015, 0.015), WHITE, eps=0.004, rings=6, segs=32)

    # The eyes: big and dark, above the snout, looking forward and out
    de = unit(0.55, -0.78, 0.28)
    E = on_head(de) + np.array([-0.004, 0.0, 0.0])
    lid, ink = srgb(BLUE), srgb(INK)
    look = dark_eye()
    def eyes(state):
        k.decal(E, de, (0.03, 0.037), eye_look(state, look, lid, ink, line=0.17), eps=0.005, rings=20, segs=56,
                mirror=True)

    k.build(voxel=0.0048, smooth=5)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    dratini(*args())
