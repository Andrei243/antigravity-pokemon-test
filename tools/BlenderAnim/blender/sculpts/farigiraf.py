# Farigiraf sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/farigiraf.py -- <the export's Farigiraf.glb> <out.glb>
#
# Grown from girafarig.py's way of making the body (a barrel, a deep chest, a rump, legs full at the top), its eyes
# and its colours by masks. From the references (they overrule the C# sculpt, whose tail head sits over the rump):
# a compact orange body on long legs, orange above and pale yellow from the knees down, dark grey hooves; a long thick
# neck, dark brown with round orange spots, a row of pale yellow triangular spikes down its back to the withers;
# brown spots on the orange shoulders. The tail's head has come up over the giraffe's own head as a great black hood:
# a round black dome behind and over the face, its open front rimmed with big white fangs above and below, a yellow
# ringed eye on each side, two black knobs on stalks on its top and the giraffe's own brown knob behind them. The
# face comes out of the hood's mouth: pale yellow over the eye, orange cheeks with brown spots, a big pink nose
# standing forward, a dark eye half under a heavy lid. A short brown tail at the rump (the hood is on the head bone,
# so the tail bone carries only that stub).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, inside, smoothstep, unit, eye_look, args

ORANGE, PALE, BROWN, HOOF = "#E8822E", "#F2E4A0", "#5A381E", "#4A4850"
HOOD, FANG, RING, CREAM, PINK = "#1E1A22", "#F4F2EC", "#F2CE2E", "#F6E4A0", "#EE9AA6"
KNOB2, INK = "#6E4626", "#1A1418"


def on_ell(c, r, d):
    """The point of an ellipsoid's surface along a direction from its centre."""
    c, r, d = np.array(c, float), np.array(r, float), unit(*d)
    return c + d / math.sqrt(((d / r) ** 2).sum())


# --- the body ------------------------------------------------------------------------------------------------------
def body(k):
    k.add("barrel", ell((0, 0.0, 0.5), (0.122, 0.2, 0.132)), ORANGE, "body")
    k.add("chest", ell((0, -0.12, 0.52), (0.122, 0.1, 0.14)), ORANGE, "body")
    k.add("rump", ell((0, 0.14, 0.505), (0.118, 0.096, 0.132)), ORANGE, "body")
    # legs: full at the top where shoulder and thigh fuse into the body, tapering, a hoof each
    k.add("shoulder", ell((0.07, -0.14, 0.47), (0.068, 0.08, 0.11)), ORANGE, "leg{s}", mirror=True)
    k.add("fore", tube([(0.072, -0.15, 0.46), (0.074, -0.156, 0.3), (0.072, -0.15, 0.14), (0.072, -0.148, 0.05)],
                       [0.062, 0.044, 0.035, 0.034]), ORANGE, "leg{s}", mirror=True)
    k.add("forehoof", ell((0.072, -0.155, 0.028), (0.038, 0.043, 0.03)), HOOF, "leg{s}", mirror=True,
          surface="shell")
    k.add("thigh", ell((0.07, 0.16, 0.45), (0.07, 0.095, 0.115)), ORANGE, "hind{s}", mirror=True)
    k.add("hind", tube([(0.072, 0.165, 0.43), (0.074, 0.2, 0.29), (0.072, 0.195, 0.16), (0.072, 0.17, 0.05)],
                       [0.058, 0.043, 0.035, 0.034]), ORANGE, "hind{s}", mirror=True)
    k.add("hindhoof", ell((0.072, 0.165, 0.028), (0.038, 0.043, 0.03)), HOOF, "hind{s}", mirror=True,
          surface="shell")
    # a short brown tail
    k.add("tail", tube([(0, 0.215, 0.56), (0, 0.262, 0.53), (0, 0.285, 0.47)], [0.026, 0.022, 0.02]), BROWN, "tail")
    # pale yellow from the knees down, hooves grey
    k.paint(lambda P: inside(P[:, 2] - 0.3, 0.004) * inside(-P[:, 2] + 0.05, 0.003), PALE, on=["fore", "hind"])
    k.paint(lambda P: inside(P[:, 2] - 0.05, 0.0025), HOOF)


# --- the neck ------------------------------------------------------------------------------------------------------
NECK = [(0, -0.075, 0.55), (0, -0.115, 0.66), (0, -0.15, 0.76), (0, -0.17, 0.84)]
NECK_R = [0.1, 0.082, 0.072, 0.066]


def neck_at(t):
    """A point on the neck's centre line and its radius, t from 0 (foot) to 1 (top), by straight pieces."""
    s = t * (len(NECK) - 1)
    i = min(int(s), len(NECK) - 2)
    f = s - i
    p = np.array(NECK[i]) * (1 - f) + np.array(NECK[i + 1]) * f
    return p, NECK_R[i] * (1 - f) + NECK_R[i + 1] * f


def neck(k):
    k.add("neck", tube(NECK, NECK_R), BROWN, "head")
    # round orange spots on the brown, both sides and the front
    for j, (t, ax, ay) in enumerate(((0.3, 1, -0.2), (0.48, -1, -0.3), (0.62, 1, 0.1), (0.78, -1, -0.1),
                                     (0.42, 0.25, -1), (0.7, -0.2, -1), (0.2, -1, 0.2), (0.88, 1, -0.3))):
        p, r = neck_at(t)
        d = unit(ax, ay, 0)
        c = p + d * r
        size = 0.024 + 0.006 * (1 - t)
        k.paint(lambda P, c=c, s=size: inside(np.linalg.norm((P - c) * np.array([1, 1, 0.8]), axis=1) - s, 0.003),
                ORANGE, on=["neck"])
    # brown spots on the orange shoulders and back
    for c, s in (((0.11, -0.06, 0.58), 0.026), ((-0.11, -0.05, 0.57), 0.026), ((0.09, 0.05, 0.61), 0.022),
                 ((-0.085, 0.06, 0.615), 0.022), ((0.122, -0.15, 0.55), 0.022), ((-0.122, -0.15, 0.545), 0.022),
                 ((0.0, 0.02, 0.632), 0.024), ((0.06, -0.12, 0.63), 0.02), ((-0.06, -0.12, 0.63), 0.02)):
        c = np.array(c)
        k.paint(lambda P, c=c, s=s: inside(np.linalg.norm(P - c, axis=1) - s, 0.003), BROWN,
                on=["barrel", "chest", "rump", "shoulder"])


def spikes(k):
    """Pale yellow triangular spikes down the back of the neck to the withers, solid, laid back."""
    for j, t in enumerate(np.linspace(0.96, 0.0, 9)):
        p, r = neck_at(t)
        back = unit(0, 1, 0.25)
        base = p + back * r * 0.85
        L = 0.05 + 0.02 * t
        along = unit(0, 0.35, 1)
        tip = base + unit(0, 1, 0.65) * L
        pts = [base + along * 0.024 + np.array([0.017, 0, 0]), base + along * 0.024 - np.array([0.017, 0, 0]),
               base - along * 0.024 + np.array([0.017, 0, 0]), base - along * 0.024 - np.array([0.017, 0, 0]),
               base - back * 0.015, tip]
        k.add(f"spike{j}", hull(pts), PALE, "head" if t > 0.25 else "body", fuse=False, surface="shell")
    for j, y in enumerate((-0.03, 0.03)):
        base = np.array([0, y, 0.625 - 0.04 * j])
        tip = base + unit(0, 1, 0.5) * (0.04 - 0.01 * j)
        pts = [base + (0.016, 0.022, 0), base + (-0.016, 0.022, 0), base + (0.016, -0.022, 0),
               base + (-0.016, -0.022, 0), base - (0, 0, 0.015), tip]
        k.add(f"withers{j}", hull(pts), PALE, "body", fuse=False, surface="shell")


# --- the head: the black hood with the face coming out of its mouth --------------------------------------------------
HC, HR = np.array([0, -0.18, 0.925]), np.array([0.128, 0.128, 0.13])
FC, FR = np.array([0, -0.3, 0.885]), np.array([0.086, 0.108, 0.084])


def rim():
    """Points of the hood's mouth: where the face's surface leaves the hood, by angle round the face's front axis."""
    pts = []
    for th in np.linspace(0, 2 * math.pi, 72, endpoint=False):
        # walk along the face's surface from its front back until it is inside the hood
        best = None
        for phi in np.linspace(0.05, 2.8, 400):
            d = np.array([math.sin(phi) * math.cos(th), -math.cos(phi), math.sin(phi) * math.sin(th)])
            q = FC + d * FR
            if (((q - HC) / HR) ** 2).sum() < 1:
                best = q
                break
        pts.append((th, best))
    return pts


def head(k):
    k.add("hood", ell(HC, HR), HOOD, "head")
    k.add("face", ell(FC, FR, rot=(-8, 0, 0)), CREAM, "head")
    k.add("cheek", ell((0, -0.32, 0.852), (0.08, 0.09, 0.06)), ORANGE, "head")
    k.add("nose", ell((0, -0.398, 0.866), (0.044, 0.04, 0.04), rot=(-15, 0, 0)), PINK, "head")
    # brown spots on the orange cheek
    for c in ((0.07, -0.335, 0.85), (-0.07, -0.335, 0.85), (0.06, -0.37, 0.872), (-0.06, -0.37, 0.872)):
        c = np.array(c)
        k.paint(lambda P, c=c: inside(np.linalg.norm(P - c, axis=1) - 0.011, 0.002), "#9A4A20", on=["cheek"])
    # the fangs round the hood's mouth: big white triangles, above pointing down, below pointing up
    # each a flat triangle lying over the face from the hood's rim toward the face's middle, standing a little proud
    def face_point(th, phi, lift):
        d = np.array([math.sin(phi) * math.cos(th), -math.cos(phi), math.sin(phi) * math.sin(th)])
        q = FC + d * FR
        n = unit(*((q - FC) / FR ** 2))
        return q + n * lift, n
    R = dict(rim())
    upper = [90, 64, 116]
    lower = [252, 288]
    for j, deg in enumerate(upper + lower):
        th = math.radians(deg)
        # the rim's angle from the face's front axis at this angle
        q = min(R.items(), key=lambda e: abs((e[0] - th + math.pi) % (2 * math.pi) - math.pi))[1]
        phi0 = math.acos(np.clip(-(q - FC)[1] / FR[1], -1, 1)) if q is not None else 1.0
        # rooted on the black hood just outside its mouth (white on black, as the pictures have them), the point
        # reaching in over the face's edge
        # a plate facing forward, laid over the rim: its base on the hood, its point over the face's edge
        if q is None:
            continue
        rho = math.hypot(q[0] - FC[0], q[2] - FC[2])
        def front_y(c, r, x, z):
            s = 1 - ((x - c[0]) / r[0]) ** 2 - ((z - c[2]) / r[2]) ** 2
            return c[1] - r[1] * math.sqrt(max(s, 0.0))
        def at(rr, a, which):
            x, z = FC[0] + rr * math.cos(a), FC[2] + rr * math.sin(a)
            c, r = (HC, HR) if which == "hood" else (FC, FR)
            return np.array([x, front_y(c, r, x, z) - 0.01, z])
        big = deg == 90
        out_r, in_r, da = rho + 0.03, rho - (0.026 if big else 0.018), (0.2 if big else 0.17)
        b1, b2, tp = at(out_r, th - da, "hood"), at(out_r, th + da, "hood"), at(in_r, th, "face")
        t = 0.011
        fwd = np.array([0, 1.0, 0])
        pts = [b1 + fwd * t, b1 - fwd * t, b2 + fwd * t, b2 - fwd * t, tp + fwd * t * 0.6, tp - fwd * t * 0.6]
        k.add(f"fang{j}", hull(pts), FANG, "head", fuse=False, surface="shell")
    # knobs on stalks on the hood's top: a black pair and the giraffe's own brown one behind
    top = HC + np.array([0, 0, HR[2]])
    k.add("stalk", tube([top + (0.045, 0.005, -0.03), top + (0.058, 0.012, 0.05), top + (0.064, 0.015, 0.09)],
                        [0.022, 0.019, 0.018]), HOOD, "head", mirror=True)
    k.add("knob", ell(top + (0.066, 0.015, 0.11), (0.038, 0.038, 0.036), sub=3), HOOD, "head", fuse=False,
          mirror=True, surface="shell")
    k.add("stalkb", tube([top + (0, 0.07, -0.04), top + (0, 0.085, 0.03), top + (0, 0.09, 0.065)],
                         [0.022, 0.019, 0.018]), KNOB2, "head")
    k.add("knobb", ell(top + (0, 0.09, 0.085), (0.034, 0.034, 0.032), sub=3), KNOB2, "head", fuse=False,
          surface="shell")
    # the hood's own eyes: a yellow ring open toward the front on each side, in every state
    Y, D = srgb(RING), srgb(HOOD)
    def ring(u, v):
        c = np.tile(D, (len(u), 1))
        rr = np.sqrt(u * u + v * v)
        band = (rr > 0.52) & (rr < 0.86) & ~((u < -0.2) & (np.abs(v) < 0.32))
        c[band] = Y
        return c
    rd = unit(0.95, 0.2, 0.25)
    k.decal(on_ell(HC, HR, rd), rd, (0.04, 0.04), ring, eps=0.003, rings=20, segs=64, mirror=True)


# --- the giraffe's eyes: dark, half under a heavy lid, slanting to the nose -----------------------------------------
CW, CIR, CP, CK, CL = srgb("#FAFAF4"), srgb("#3A2418"), srgb("#100C0C"), srgb(INK), srgb(CREAM)


def eye(u, v):
    c = np.tile(CW, (len(u), 1))
    c[((u + 0.15) / 0.5) ** 2 + ((v + 0.05) / 0.65) ** 2 < 1] = CIR
    c[((u + 0.15) / 0.24) ** 2 + ((v + 0.05) / 0.32) ** 2 < 1] = CP
    c[((u + 0.3) / 0.13) ** 2 + ((v - 0.18) / 0.13) ** 2 < 1] = (1, 1, 1)
    lid = 0.42 + 0.25 * u
    c[v > lid] = CL
    c[np.abs(v - lid) < 0.13] = CK
    return c


ED = unit(0.72, -0.62, 0.3)


def eyes(k, state):
    k.decal(on_ell(FC, FR, ED), ED, (0.028, 0.03), eye_look(state, eye, CL, CK, line=0.15), eps=0.002, rings=20,
            segs=56, mirror=True)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="fur")
    body(k)
    neck(k)
    spikes(k)
    head(k)
    k.build(voxel=0.0075, smooth=4, band=0.006)
    k.eye_states(lambda s: eyes(k, s))
    k.export(out)
