# Crawdaunt sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/crawdaunt.py -- <the export's Crawdaunt.glb> <out.glb>
#
# Corphish grown (corphish.py's frame, by sibling): the same upright egg of a body, the same way of painting its front
# by the angle round the body and of making eyes and the segmented tail. From the references: red now, taller, its
# head drawn up into a rounded snout leaning forward with a big yellow five-pointed star on top; small white eyes with a
# black pupil in a thick black ring at the sides of the head; a cream face whose top the red snout points down into,
# crossed by a blue chevron of two stripes pointing down and ending below in a jagged edge; a red band, then the cream
# belly coming up in teeth. Two huge open pincers, red over a cream lower jaw with the dark inside showing: the one on
# +X held up and pointed at the viewer, the one on -X held low and out, longer, its red upper finger hooked down. Thick
# red legs like boots with two white claw points each; a segmented tail behind.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, args, outward, hull, slab, tri, sibling

co = sibling("corphish")
RED, CREAM, BLUE, BLUE2 = "#D8433E", "#EADCC6", "#5AA6E0", "#3F86C8"
YELLOW, DARK, WHITE, INK = "#F2D46A", "#2A2228", "#FAFAF6", "#16100F"
X = np.array([1.0, 0, 0]); Z = np.array([0, 0, 1.0])

BODY_C, BODY_R = np.array([0.0, -0.01, 0.29]), np.array([0.118, 0.112, 0.205])


def head(k):
    k.add("snout", ell((0, -0.058, 0.465), (0.064, 0.064, 0.075), rot=(-22, 0, 0)), RED, "head")


def star(k):
    """A thick five-pointed star on the crown, its face turned to the front and up."""
    pts = []
    for i in range(10):
        a = math.radians(90 + 36 * i)
        r = 0.078 if i % 2 == 0 else 0.036
        pts.append((r * math.cos(a), r * math.sin(a)))
    ex = X
    ez = unit(0, 0.42, 1)
    k.add("star", slab(pts, (0.0, -0.035, 0.585), ex, ez, 0.034), YELLOW, "head", fuse=False, surface="shell")
    k.add("starfoot", outward(tube([(0, -0.03, 0.51), (0, -0.034, 0.55)], [0.022, 0.02], seg=12, per=4)), YELLOW,
          "head", fuse=False, surface="shell")


def face_marks(k):
    c = BODY_C
    half = 1.0
    def near(P):
        return (np.hypot(P[:, 0], P[:, 1] - c[1]) < 0.155).astype(float)
    def phi(P):
        return co.phi(P, c)
    def face(P):
        a = np.clip(np.abs(phi(P)) / half, 0, 1.6)
        z = P[:, 2]
        top = 0.43 + 0.05 * a
        bottom = 0.252 + 0.03 * a + 0.018 * (tri(phi(P) * 5 / (2 * math.pi) + 0.25) + 1) / 2
        return inside(a - 1, 0.003) * inside(z - top, 0.0025) * inside(bottom - z, 0.0025) * (P[:, 1] < 0.02) * near(P)
    k.paint(face, CREAM)
    def chevron(z0):
        def m(P):
            a = np.clip(np.abs(phi(P)) / half, 0, 1.6)
            z = P[:, 2]
            return inside(np.abs(z - (z0 + 0.055 * a)) - 0.012, 0.002) * inside(a - 1.0, 0.003) * (P[:, 1] < 0.02) * near(P)
        return m
    k.paint(chevron(0.352), BLUE)
    k.paint(chevron(0.318), BLUE2)
    def belly(P):
        a = np.clip(np.abs(phi(P)) / half, 0, 1.6)
        z = P[:, 2]
        top = 0.205 - 0.03 * a + 0.022 * (tri(phi(P) * 5 / (2 * math.pi)) + 1) / 2
        front = inside(a - 1.05, 0.003) * inside(z - top, 0.0025)
        return np.maximum(front, inside(z - 0.13, 0.004)) * near(P)
    k.paint(belly, CREAM)
    for s in (1, -1):
        sc = np.array([0.1 * s, -0.06, 0.295])
        k.paint(lambda P, sc=sc: inside(np.linalg.norm(P - sc, axis=1) - 0.05, 0.004), RED)


# ---- the pincers: an open clamshell, red upper jaw hooked at its tip, cream lower jaw, dark inside -------------------
def pincer(k, sn, hinge, d, up, L, W, H, open_up=22.0, open_down=18.0, name="claw"):
    bone = "arm" + sn
    d = unit(*d); u = np.array(up, float); u = u - d * (u @ d); u /= np.linalg.norm(u)
    s = np.cross(u, d)
    M = np.stack([d, s, u])                       # local x along d, y across, z up the upper jaw's side
    hinge = np.array(hinge, float)
    def place(V):
        return hinge + V @ M

    def jaw(V, hook):
        V = V.copy()
        V[:, 2] = np.where(V[:, 2] < 0, V[:, 2] * 0.35, V[:, 2])         # flat inner face
        V[:, 2] -= hook * np.clip(V[:, 0], 0, 1) ** 2                     # the tip curving in
        return V
    def jaw_part(length, width, height, angle, hook, lower=False):
        a = math.radians(angle)
        V, F = ell((0, 0, 0), (length / 2, width, height), deform=lambda V: jaw(V, hook))
        if lower:
            V = V * np.array([1, 1, -1.0]); F = [f[::-1] for f in F]
        R = np.array([[math.cos(a), 0, -math.sin(a)], [0, 1, 0], [math.sin(a), 0, math.cos(a)]])
        off = np.array([math.cos(a), 0, math.sin(a)]) * length * 0.48
        return place(V @ R.T + off + np.array([0, 0, H * (0.25 if not lower else -0.2)])), F
    k.add(name + sn + "palm", (place(ell((0.0, 0, 0), (W * 1.05, W, H * 0.95))[0]), ell((0, 0, 0), (1, 1, 1))[1]),
          RED, bone)
    k.add(name + sn + "up", jaw_part(L, W * 0.95, H, open_up, 0.35), RED, bone)
    k.add(name + sn + "lo", jaw_part(L * 0.82, W * 0.82, H * 0.75, -open_down, 0.25, lower=True), CREAM, bone)
    k.add(name + sn + "in", (place(ell((L * 0.3, 0, 0), (L * 0.32, W * 0.62, H * 0.45))[0]),
                             ell((0, 0, 0), (1, 1, 1))[1]), DARK, bone)
    # two spikes on the upper jaw's back
    for t, ln in ((0.3, 0.05), (0.62, 0.04)):
        a = math.radians(open_up)
        base = np.array([L * t * math.cos(a), 0, L * t * math.sin(a) + H * 0.95])
        tip = base + np.array([0.012, 0, ln])
        root = [base + np.array([dx, dy, -0.012]) for dx, dy in ((0.018, 0), (-0.018, 0), (0, 0.016), (0, -0.016))]
        k.add(name + sn + f"spike{t}", hull([place(p[None])[0] for p in root + [tip]]), RED, bone, fuse=False,
              surface="shell")


def arm(k, sn, pts, r=0.024):
    k.add("arm" + sn, outward(tube(pts, [r, r * 0.95, r * 0.9], seg=14, per=6)), RED, "arm" + sn, fuse=False,
          surface="shell")
    k.add("elbow" + sn, outward(ell(pts[1], (r * 1.35,) * 3, sub=3)), RED, "arm" + sn, fuse=False, surface="shell")
    k.add("shoulder" + sn, ell(pts[0], (0.04, 0.04, 0.04)), RED, "arm" + sn)


def arms(k):
    arm(k, "R", [(0.1, -0.06, 0.3), (0.185, -0.085, 0.28), (0.235, -0.11, 0.31)])
    pincer(k, "R", (0.245, -0.12, 0.325), (0.3, -1, 0.42), (0, 0.15, 1), L=0.18, W=0.066, H=0.072, open_up=28,
           open_down=24)
    arm(k, "L", [(-0.1, -0.06, 0.29), (-0.155, -0.07, 0.22), (-0.18, -0.1, 0.165)])
    pincer(k, "L", (-0.19, -0.115, 0.16), (-0.55, -0.85, 0.05), (0, 0.1, 1), L=0.21, W=0.055, H=0.065,
           open_up=18, open_down=16)


# ---- the legs: thick red boots with two white claw points -------------------------------------------------------
LEGS = [("leg{s}", -0.06), ("hind{s}", 0.03), ("hind{s}.001", 0.11)]


def legs(k):
    for i, (bone, y) in enumerate(LEGS):
        sc = 1.0 if i == 0 else 0.85
        hip = np.array([0.075, y, 0.15]); knee = np.array([0.15, y - 0.015, 0.155]); foot = np.array([0.165, y - 0.02,
                                                                                                     0.02])
        k.add(f"leg{i}", outward(tube([hip, knee], [0.028 * sc, 0.027 * sc], seg=14, per=6)), RED, bone,
              fuse=False, surface="shell", mirror=True)
        k.add(f"boot{i}", outward(ell((knee + foot) / 2 + (0.0, 0, 0.0), (0.042 * sc, 0.047 * sc, 0.074),
                                      deform=lambda V: V * np.where(V[:, 2:3] < 0, 1.0, 0.85))), RED, bone,
              fuse=False, surface="shell", mirror=True)
        for dx in (-0.016, 0.016):
            b = foot + np.array([dx * sc, -0.03 * sc, 0.0])
            pts = [b + (0.009, 0, 0.002), b + (-0.009, 0, 0.002), b + (0, 0.01, 0.018), b + (0, -0.026, 0.0)]
            k.add(f"toe{i}{dx}", hull(pts), WHITE, bone, fuse=False, surface="shell", mirror=True)


EYE_C = np.array([0.064, -0.092, 0.448])
EYE_N = unit(0.65, -1, 0.15)
EYE_HALF = (0.026, 0.026)


def open_eye(u, v):
    c = np.tile(srgb(INK), (len(u), 1))
    rr = np.sqrt(u * u + v * v)
    c[rr < 0.66] = srgb(WHITE)
    c[((u + 0.08) / 0.3) ** 2 + ((v + 0.0) / 0.32) ** 2 < 1] = srgb(INK)
    return c


def crawdaunt(src, out):
    k = Kit(src, coat="shell")
    co.body(k, BODY_C, BODY_R, RED)
    head(k)
    k.add("eyebump", ell(EYE_C - EYE_N * 0.014, (0.032, 0.022, 0.032), rot=(0, 0, 33)), RED, "head", mirror=True)
    face_marks(k)
    star(k)
    arms(k)
    legs(k)
    co.tail(k, base=(0.0, 0.11, 0.15), d=(0.45, 0.85, -0.3), n=3, r0=0.06, step=0.05, fan=0.07, color=RED)
    k.build(voxel=0.0055, smooth=3, band=0.006)
    k.eye_states(co.eyes(k, EYE_C, EYE_N, EYE_HALF, open_eye, RED))
    k.export(out)


if __name__ == "__main__":
    src, out = args()
    crawdaunt(src, out)
