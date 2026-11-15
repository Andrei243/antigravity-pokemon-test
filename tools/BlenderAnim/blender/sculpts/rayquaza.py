# Rayquaza sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/rayquaza.py -- <the export's Rayquaza.glb> <out.glb>
#
# From the references: a long green serpent dragon coiled in the air, its thick body rising from a looped tail in an S
# to the head; yellow oval rings along its sides and black lines between its segments; flat fins out of its sides,
# green with red rims, and a red-rimmed fin of three points at the tail's end; small arms under the neck with white
# claws; a long head with a pointed snout, the jaws open (pink within, small white fangs), yellow lines along the
# snout, a small yellow eye with a black pupil, a long pointed horn swept back off each side of the crown and a
# shorter one off each cheek.
#
# The body follows the skeleton's chain (tail, body, seg1 to seg10, head), a tube a span on its bone; the coil keeps
# the model compact, not long and thin.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, slab, ring_band, srgb, inside, smoothstep, unit, eye_look, polygon, args

GREEN, YELLOW, RED, INKC = "#2E9A50", "#F2D838", "#E04858", "#14201A"
PINK, WHITE, DARK = "#E88E98", "#F4F4F0", "#1F6E3A"

TIP = np.array([0.2, 0.7, 0.06])
P = np.array([TIP, (0.12, 0.54, 0.075), (0.08, 0.42, 0.103), (0.142, 0.32, 0.176), (0.149, 0.281, 0.277),
              (0.08, 0.3, 0.383), (-0.05, 0.214, 0.439), (-0.119, 0.105, 0.42), (-0.1, 0.0, 0.343),
              (-0.003, -0.056, 0.287), (0.059, -0.108, 0.315), (0.06, -0.16, 0.423), (0.019, -0.191, 0.527),
              (0.0, -0.215, 0.575)])
R = np.array([0.02, 0.04, 0.052, 0.062, 0.068, 0.07, 0.07, 0.07, 0.068, 0.066, 0.062, 0.058, 0.052, 0.05])
BONES = ["tail", "tail", "body", "seg1", "seg2", "seg3", "seg4", "seg5", "seg6", "seg7", "seg8", "seg9", "seg10"]


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


CRV, SEG = spline(P, per=14)
RAD = np.interp(SEG, np.arange(len(P)), R)
FC, FS = spline(P, per=60)
TAN = np.gradient(FC, axis=0); TAN /= np.linalg.norm(TAN, axis=1, keepdims=True)


def at(s):
    i = int(np.argmin(np.abs(FS - s)))
    t = TAN[i]
    side = np.cross(t, (0, 0, 1.0))
    if np.linalg.norm(side) < 0.2:
        side = np.cross(t, (0, -1.0, 0))
    side /= np.linalg.norm(side)
    up = np.cross(side, t)
    return FC[i], t, side, up, float(np.interp(s, np.arange(len(P)), R))


CUM = np.concatenate([[0], np.cumsum(np.linalg.norm(np.diff(FC, axis=0), axis=1))])
SIDE = np.cross(TAN, (0, 0, 1.0))
SIDE /= np.maximum(np.linalg.norm(SIDE, axis=1, keepdims=True), 1e-6)
UPV = np.cross(SIDE, TAN)
RINGS = (1.6, 3.3, 4.8, 6.3, 7.8, 9.3)


def rings(Q):
    j = np.concatenate([((Q[a:a + 3000, None, :] - FC[None]) ** 2).sum(2).argmin(1) for a in range(0, len(Q), 3000)])
    rad = Q - FC[j]
    r = np.linalg.norm(rad, axis=1) + 1e-9
    m = np.zeros(len(Q))
    for s in RINGS:
        i0 = int(np.argmin(np.abs(FS - s)))
        rr = float(np.interp(s, np.arange(len(P)), R))
        along = CUM[j] - CUM[i0]
        for sd in (1, -1):
            ang = np.arctan2((rad * UPV[j]).sum(1), (rad * SIDE[j]).sum(1) * sd)
            q = np.sqrt((along / (0.75 * rr)) ** 2 + (ang * r / (0.62 * rr)) ** 2)
            m = np.maximum(m, inside(np.abs(q - 0.8) - 0.17, 0.04))
    return m * (FS[j] < 10.5)


def body(k):
    for i, bone in enumerate(BONES):
        sel = (SEG >= i - 0.15) & (SEG <= i + 1.15)
        k.add(f"seg{i:02d}", tube(CRV[sel][::2], RAD[sel][::2], seg=24, per=5), GREEN, bone)
    # Black lines round the body between its segments
    for j, s in enumerate((2.5, 4.0, 5.5, 7.0, 8.5, 10.0)):
        c, t, side, up, r = at(s)
        k.add(f"band{j}", ring_band(c, t, r - 0.001, 0.008, thick=0.006), INKC, BONES[int(s)], fuse=False)
    # Yellow oval rings on both sides along the body, painted in the body's own frame (patches cast on a coil slip
    # past its sides and streak onto the next stretch)
    k.paint(rings, YELLOW, on=["seg"])


FIN = [(0.0, -0.05), (0.07, -0.06), (0.13, -0.035), (0.13, 0.035), (0.07, 0.06), (0.0, 0.05)]


def fins(k):
    # Flat fins out of the body's sides, green with a red rim, banked so that their faces show
    for j, (s, size) in enumerate(((3.0, 0.9), (6.0, 1.05), (9.0, 0.85))):
        c, t, side, up, r = at(s)
        for sd in (1, -1):
            out = side * sd
            ex = unit(*(out * 0.9 + up * 0.35))
            ez = t
            o = c + out * r * 0.6
            k.add(f"fin{j}{'R' if sd > 0 else 'L'}", slab(np.array(FIN) * size, o, ex, ez, 0.024), RED,
                  BONES[int(s)], fuse=False, surface="scales")
            inner = np.array(FIN) * size * np.array([0.82, 0.72]) + np.array([0.004, 0.0])
            k.add(f"finin{j}{'R' if sd > 0 else 'L'}", slab(inner, o, ex, ez, 0.032), GREEN, BONES[int(s)],
                  fuse=False, surface="scales")
    # The tail's fin of three points, red-rimmed, flat and turned to face up and back
    c, t, side, up, r = at(0.05)
    d = -t
    TF = [(0.0, -0.03), (0.06, -0.09), (0.05, -0.03), (0.13, 0.0), (0.05, 0.03), (0.06, 0.09), (0.0, 0.03)]
    ez = unit(*(side * 0.8 + up * 0.6))
    k.add("tailfin", slab(TF, TIP - d * 0.02, d, ez, 0.024), RED, "tail", fuse=False, surface="scales")
    inner = [(0.012, -0.018), (0.05, -0.06), (0.045, -0.02), (0.1, 0.0), (0.045, 0.02), (0.05, 0.06), (0.012, 0.018)]
    k.add("tailfinin", slab(inner, TIP - d * 0.02, d, ez, 0.032), GREEN, "tail", fuse=False, surface="scales")


AC = np.array([0.06, -0.16, 0.42])          # the arms' joint


def arms(k):
    for s, sd in ((1, "R"), (-1, "L")):
        sh = AC + np.array([0.05 * s, -0.01, 0.0])
        el = AC + np.array([0.1 * s, -0.04, -0.03])
        hd = AC + np.array([0.11 * s, -0.09, -0.01])
        k.add(f"arm{sd}", tube([sh, el, hd], [0.024, 0.02, 0.02]), GREEN, f"arm{sd}")
        k.add(f"hand{sd}", ell(hd + np.array([0, -0.01, 0]), (0.024, 0.024, 0.02)), GREEN, f"arm{sd}")
        for j, dx in enumerate((-0.014, 0.0, 0.014)):
            b = hd + np.array([dx * s, -0.028, -0.008 + abs(dx) * 0.3])
            dd = unit(dx * 2 * s, -1, -0.6)
            sideways = np.array([1.0, 0, 0])
            w, th, ln = 0.012, 0.011, 0.035
            k.add(f"claw{sd}{j}", hull([b + sideways * w, b - sideways * w, b + (0, 0, th), b - (0, 0, th),
                                        b + dd * ln]), WHITE, f"arm{sd}", fuse=False, surface="shell")


HJ = np.array([0.0, -0.215, 0.575])          # the head's joint
H, HR = np.array([0.0, -0.255, 0.6]), np.array([0.062, 0.085, 0.055])
HS = 1.25                                     # the head grown about its joint
HG, HRG = HJ + (H - HJ) * HS, HR * HS


def head(k):
    k.add("skull", ell(H, HR), GREEN, "head")
    k.add("snout", ell((0, -0.345, 0.6), (0.046, 0.1, 0.032), rot=(-4, 0, 0)), GREEN, "head")
    k.add("brow", ell((0.034, -0.3, 0.628), (0.026, 0.05, 0.016), rot=(-12, 0, -15)), GREEN, "head", mirror=True)
    # The jaws open: the lower jaw dropped, pink within
    k.add("jaw", ell((0, -0.32, 0.535), (0.04, 0.09, 0.02), rot=(24, 0, 0)), GREEN, "head")
    k.add("maw", ell((0, -0.31, 0.56), (0.036, 0.075, 0.026), rot=(12, 0, 0)), PINK, "head")
    # Yellow lines along the snout's sides, and the mouth's rim in red under them
    def lines(Q):
        Q = HJ + (Q - HJ) / HS                 # the head is grown after it is laid out
        y, z, x = Q[:, 1], Q[:, 2], np.abs(Q[:, 0])
        top = inside(np.abs(z - (0.603 + 0.12 * (y + 0.3))) - 0.0045, 0.0015) * (y < -0.27) * (y > -0.42) * (x > 0.02)
        return top * (z > 0.575)
    k.paint(lines, YELLOW, on=["snout", "skull"])
    # Small white fangs from the upper jaw's sides
    for s in (1, -1):
        a = np.array((0.028 * s, -0.4, 0.58))
        k.add(f"fang{s}", hull([a + (0.008, 0, 0), a - (0.008, 0, 0), a + (0, 0.01, 0), a - (0, 0.006, 0),
                                a + (0.002 * s, -0.004, -0.03)]), WHITE, "head", fuse=False, surface="shell")
    # The horns: a long pointed one swept back off each side of the crown, a shorter one off each cheek
    for s, sd in ((1, "R"), (-1, "L")):
        for name, b, d, ln, w, th, col in (("horn", (0.03, -0.24, 0.64), (0.35, 0.82, 0.45), 0.25, 0.045, 0.03, GREEN),
                                           ("cheek", (0.05, -0.23, 0.585), (0.75, 0.62, 0.05), 0.15, 0.036, 0.026,
                                            GREEN)):
            b = np.array(b) * np.array([s, 1, 1.0]); d = unit(*(np.array(d) * np.array([s, 1, 1.0])))
            side = np.cross(d, (0, 0, 1.0)); side /= np.linalg.norm(side)
            upv = np.cross(side, d)
            P_ = [b - d * 0.03 + upv * w / 2, b - d * 0.03 - upv * w / 2, b + side * th / 2, b - side * th / 2,
                  b + d * ln * 0.45 + upv * w * 0.35, b + d * ln * 0.45 - upv * w * 0.25,
                  b + d * ln * 0.45 + side * th * 0.35, b + d * ln * 0.45 - side * th * 0.35, b + d * ln]
            k.add(f"{name}{sd}", hull(P_), col, "head", fuse=False, surface="scales")


CG, CK, CY = srgb(GREEN), srgb(INKC), srgb(YELLOW)
DE = unit(0.6, -0.72, 0.32)
BA, BB = 0.34, 0.45


def cut(th):
    s = math.sin(th) - BB * math.cos(th)
    return min(1.0, BA / s) if s > 1e-6 else 1.0


def eye_open(u, v):
    c = np.tile(CY, (len(u), 1))
    c[((u + 0.05) / 0.3) ** 2 + (v / 0.4) ** 2 < 1] = CK
    r = np.sqrt(u * u + v * v)
    c[r > 0.86] = CK
    return c


def on_ell(c, r, d):
    d = unit(*d)
    t = 1.0 / math.sqrt(((d / np.array(r)) ** 2).sum())
    return np.array(c, float) + d * t


def eyes(k, state):
    E = on_ell(HG, HRG, DE) + np.array([0, -0.005, 0])
    look = eye_look(state, eye_open, CG, CK, line=0.2)
    m = np.array([-1, 1, 1.0])
    k.decal(E, DE, (0.024 * HS, 0.02 * HS), look, eps=0.004, rings=20, segs=56, outline=cut)
    k.decal(E * m, DE * m, (0.024 * HS, 0.02 * HS), lambda u, v: look(-u, v), eps=0.004, rings=20, segs=56,
            outline=lambda th: cut(math.pi - th))


def rayquaza(src, out):
    k = Kit(src, coat="scales")
    body(k)
    fins(k)
    arms(k)
    n0 = len(k.parts)
    head(k)
    for p in k.parts[n0:]:
        p["V"] = HJ + (p["V"] - HJ) * HS
    k.build(voxel=0.0055, smooth=4, band=0.004)
    k.eye_states(lambda state: eyes(k, state))
    k.export(out)


if __name__ == "__main__":
    rayquaza(*args())
