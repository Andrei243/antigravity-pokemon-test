# Brute Bonnet sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/brutebonnet.py -- <the export's Brute Bonnet.glb> <out.glb>
# A paradox of Amoonguss's line, built anew (foongus.py's on_ell and button are borrowed through sibling("foongus")).
#
# From the references: the largest mass is a great flat cap like a drum, as wide as the whole creature is tall, its
# top barely domed: red over most of it, a pale grey wedge at the -X side, a dark brown band along both edges of the
# wedge running from a white button ringed brown at the top's middle down the side to the rim, a brown rim. Thick
# cones stand round the top near its edge, red on the red, grey on the grey. Under the rim hangs a fringe of gills as
# long pointed teeth: a brown zigzag at the top, light green teeth, darker green ones behind them, open at the front
# where the face looks out. Under it a broad pale grey barrel of a stem, dark purple in the cap's shade where the face
# is: yellow eyes with a black upright slit, wide pink lips open on white teeth. A brown band low round the stem, short
# brown feet. A grey Poke Ball held as a shield at each side (red above, grey below, brown band, ringed button). A
# grey tail with a brown underside behind, its tip turned up. (The task's text expected a small dinosaur's body; the
# pictures show a barrel of a stem on stubby feet with a tail: the pictures are followed.)
# Bones: body (0, 0, 0.22), head (0, 0, 0.36), arm{s} (0.14, -0.02, 0.26), leg{s} (0.1, -0.07, 0.12),
# hind{s} (0.1, 0.08, 0.12), tail (0, 0.15, 0.2).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, hull, lathe, outward, srgb, inside, smoothstep, unit, eye_look, args, sibling, tri,
                       polygon)

fg = sibling("foongus")

RED, GREY, BROWN, STEM = "#D8333B", "#DCDDD3", "#5E3226", "#D3D6CC"
GREEN, DEEP, PURPLE, FOOT = "#82D873", "#3F9A48", "#4E3352", "#6A3A2B"
YELLOW, INK, LIPS, MOUTH, TOOTH = "#F3D23A", "#1C1418", "#EE8CB4", "#3A1C2A", "#F6F2EC"

CAP = np.array([0.0, 0.0, 0.565])
CAPR = np.array([0.37, 0.36, 0.105])
STEMC = np.array([0.0, 0.0, 0.285])
STEMR = np.array([0.2, 0.19, 0.255])
FACE = np.array([0.0, -0.095, 0.41])
FACER = np.array([0.155, 0.095, 0.085])
GREY_AZ, GREY_HALF = math.radians(-154), math.radians(57)       # the grey wedge's middle and half width
GAP_AZ, GAP_HALF = math.radians(-74), math.radians(36)          # the fringe's opening for the face
VOXEL = 0.0072


def puck(V):
    """The unit sphere as a drum: round sides nearly upright, a top barely domed, a flat underside."""
    V = V.copy()
    rho = np.sqrt(V[:, 0] ** 2 + V[:, 1] ** 2)
    s = (rho ** 4 + np.abs(V[:, 2]) ** 4) ** -0.25
    V *= s[:, None]
    rho = np.clip(rho * s, 0, 1)
    z = V[:, 2]
    V[:, 2] = np.where(z > 0, z + 0.22 * (1 - rho ** 2), z * 0.62)
    return V


def barrel(V):
    V = V.copy()
    z = V[:, 2]
    s = 1 + 0.06 * (1 - z * z)
    V[:, 0] *= s; V[:, 1] *= s
    return V


def azimuth(P, c=CAP):
    return np.arctan2(P[:, 1] - c[1], P[:, 0] - c[0])


def angdiff(a, b):
    return (a - b + math.pi) % (2 * math.pi) - math.pi


def cap_paint(k):
    """Red, the grey wedge, the brown bands along its edges and up to the button, the brown rim and underside."""
    def local(P):
        return P - CAP
    def grey(P):
        return inside(np.abs(angdiff(azimuth(P), GREY_AZ)) - GREY_HALF, 0.004)
    def bands(P):
        q = local(P)
        rho = np.sqrt(q[:, 0] ** 2 + q[:, 1] ** 2)
        d = np.abs(np.abs(angdiff(azimuth(P), GREY_AZ)) - GREY_HALF) * np.maximum(rho, 0.07)
        return inside(d - 0.013, 0.0025) * (rho > 0.06)
    def rim(P):
        q = local(P)
        return inside(q[:, 2] + 0.035, 0.003)
    def ring(P):
        q = local(P)
        rho = np.sqrt(q[:, 0] ** 2 + q[:, 1] ** 2)
        return inside(np.abs(rho - 0.072) - 0.013, 0.0025) * (q[:, 2] > 0.06)
    def button(P):
        q = local(P)
        rho = np.sqrt(q[:, 0] ** 2 + q[:, 1] ** 2)
        return inside(rho - 0.059, 0.0025) * (q[:, 2] > 0.06)
    k.paint(lambda P: np.ones(len(P)), RED, on=["cap"])
    k.paint(grey, GREY, on=["cap"])
    k.paint(bands, BROWN, on=["cap"])
    k.paint(ring, BROWN, on=["cap"])
    k.paint(button, GREY, on=["cap"])
    k.paint(rim, BROWN, on=["cap"])


def spikes(k):
    """Thick cones standing round the top near its edge, leaning out, each the colour of the cap under it."""
    for i in range(9):
        a = math.radians(-60 + i * 40 + (8 if i % 2 else 0))
        d = np.array([math.cos(a), math.sin(a), 0.0])
        rho = 0.27 + 0.02 * (i % 2)
        top = CAP[2] + CAPR[2] + 0.22 * CAPR[2] * (1 - (rho / CAPR[0]) ** 2) - 0.012
        base = CAP + d * rho * np.array([1, CAPR[1] / CAPR[0], 1]) + (0, 0, top - CAP[2])
        up = unit(d[0] * 0.38, d[1] * 0.38, 1)
        h = 0.085 + 0.012 * (i % 3 == 0)
        grey = abs(angdiff(a, GREY_AZ)) < GREY_HALF - 0.05
        k.add(f"spike{i}", lathe(base, up, [(0, -0.01), (0.036, -0.01), (0.03, h * 0.35), (0.014, h * 0.75), (0, h)]),
              GREY if grey else RED, "head", fuse=False, surface="shell")


def fringe(k):
    """The gills under the rim: a brown zigzag, light green teeth and darker ones behind, open at the front."""
    rings = [("zig", BROWN, 0.368, 24, (0.05, 0.065), 0.0),
             ("gill", GREEN, 0.352, 18, (0.13, 0.2), 0.0),
             ("gilldeep", DEEP, 0.33, 18, (0.17, 0.23), 0.5)]
    top = CAP[2] - 0.2 * CAPR[2]
    for name, col, R, n, (l0, l1), off in rings:
        step = 2 * math.pi / n
        for i in range(n):
            a = (i + off) * step + 0.13
            if abs(angdiff(a, GAP_AZ)) < GAP_HALF + (0.1 if name == "zig" else 0):
                continue
            L = l0 + (l1 - l0) * (0.5 + 0.5 * math.sin(i * 2.3 + off * 4))
            th = 0.013 if name != "zig" else 0.011
            pts = []
            for s_ in (-1, 1):
                b = a + s_ * step * 0.52
                for dr in (-th, th):
                    pts.append(((R + dr) * math.cos(b), (R + dr) * math.sin(b) * CAPR[1] / CAPR[0], top))
            tipr = R + 0.022
            for dr in (-0.003, 0.003):
                pts.append(((tipr + dr) * math.cos(a), (tipr + dr) * math.sin(a) * CAPR[1] / CAPR[0], top - L))
            # a ridge down the middle so the tooth has a lit face and a shaded one
            pts.append(((R + th * 1.6) * math.cos(a), (R + th * 1.6) * math.sin(a) * CAPR[1] / CAPR[0], top - L * 0.35))
            k.add(f"{name}{i}", hull(pts), col, "head", fuse=False, surface="leaf")


def shield(k, s):
    """A Poke Ball held as a shield low at the side, its face turned out and forward: red above, grey below."""
    side = "R" if s > 0 else "L"
    S = np.array([0.285 * s, -0.035, 0.18])
    A = unit(s, -0.55, 0.05)
    e1 = np.cross((0, 0, 1.0), A); e1 /= np.linalg.norm(e1)
    e2 = np.cross(A, e1)
    M = np.stack([e1, e2, A], axis=1)
    V, F = ell((0, 0, 0), (0.1, 0.1, 0.05))
    k.add(f"shield{side}", (V @ M.T + S, F), GREY, f"arm{side}")
    def near(P):
        return inside(np.linalg.norm(P - S, axis=1) - 0.11, 0.004)
    k.paint(lambda P: inside(0.011 - (P - S) @ e2, 0.0015) * near(P), RED, on=[f"shield{side}"])
    k.paint(lambda P: inside(np.abs((P - S) @ e2) - 0.011, 0.0015) * near(P), BROWN, on=[f"shield{side}"])
    fg.button(k, S + A * 0.05, A, 0.036, 0.022, ring=BROWN, face=GREY, up=tuple(e2))
    k.add(f"stalk{side}", tube([(0.14 * s, -0.02, 0.26), (0.2 * s, -0.03, 0.22), (0.25 * s, -0.035, 0.19)],
                               [0.035, 0.03, 0.03]), STEM, f"arm{side}")


def tail(k):
    pts = np.array([(0.0, 0.13, 0.2), (0.05, 0.25, 0.15), (0.11, 0.34, 0.13), (0.16, 0.4, 0.15), (0.19, 0.43, 0.2)])
    k.add("tail", tube(pts, [0.07, 0.054, 0.04, 0.028, 0.016]), STEM, "tail")
    zs, ys = pts[:, 2], pts[:, 1]
    def under(P):
        zc = np.interp(P[:, 1], ys, zs)
        return inside(P[:, 2] - zc + 0.004, 0.003) * (P[:, 1] > 0.17)
    k.paint(under, BROWN, on=["tail"])


def body(k):
    k.add("stem", ell(STEMC, STEMR, deform=barrel), STEM, "body")
    k.add("face", ell(FACE, FACER), STEM, "head")
    k.add("cap", ell(CAP, CAPR, deform=puck, sub=5), RED, "head")
    for bone, y in (("leg{s}", -0.07), ("hind{s}", 0.08)):
        k.add("foot" + bone[0], ell((0.1, y, 0.05), (0.075, 0.08, 0.052)), FOOT, bone, mirror=True)
    # the brown band low round the stem; the dark of the cap's shade where the face is
    k.paint(lambda P: inside(np.abs(P[:, 2] - 0.105) - 0.016, 0.003), BROWN, on=["stem"])
    k.paint(lambda P: smoothstep(0.345, 0.37, P[:, 2]), PURPLE, on=["stem", "face"])


# --- the face: yellow eyes with an upright black slit, wide pink lips open on white teeth
CY, CK, CP_ = srgb(YELLOW), srgb(INK), srgb(PURPLE)
CL, CM, CT = srgb(LIPS), srgb(MOUTH), srgb(TOOTH)


def eye_open(u, v):
    c = np.tile(CY, (len(u), 1))
    c[(u / 0.27) ** 2 + (v / 0.95) ** 2 < 1] = CK
    return c


def mouth(u, v):
    c = np.tile(CL, (len(u), 1))
    inner = (u / 0.84) ** 2 + (v / 0.55) ** 2 < 1
    c[inner] = CM
    teeth_top = inner & (v > 0.55 * np.sqrt(np.clip(1 - (u / 0.84) ** 2, 0, 1)) - 0.32 * (0.5 + 0.5 * tri(u * 3 + 0.5)))
    teeth_bot = inner & (v < -0.55 * np.sqrt(np.clip(1 - (u / 0.84) ** 2, 0, 1)) + 0.28 * (0.5 + 0.5 * tri(u * 3)))
    c[teeth_top | teeth_bot] = CT
    return c


EYE_CUT = polygon([(-1.0, -0.05), (-0.6, -0.62), (0.0, -0.8), (0.7, -0.55), (1.0, 0.1), (0.6, 0.7), (0.0, 0.62), (-0.7, 0.28)])


def brutebonnet(src, out):
    k = Kit(src, coat="fur")
    body(k)
    cap_paint(k)
    for s in (1, -1):
        shield(k, s)
    tail(k)
    spikes(k)
    fringe(k)
    mp, mn = fg.on_ell(FACE, FACER, (0, -1, -0.62))
    k.decal(mp, mn, (0.07, 0.034), mouth, eps=0.003, rings=18, segs=64)
    k.build(voxel=VOXEL, smooth=4, band=0.005, wband=0.05)

    ep, en = fg.on_ell(FACE, FACER, (0.44, -0.86, 0.18))
    def eyes(state):
        if state == "Fierce":
            def look(u, v):
                c = eye_open(u, v)
                c[v > 0.15 - 0.5 * u] = CP_
                c[np.abs(v - (0.15 - 0.5 * u)) < 0.13] = CK
                return c
        else:
            look = eye_look(state, eye_open, CP_, srgb("#C9A6C8") if state != "Open" else CK, line=0.17)
        for side in (1, -1):
            p = ep * np.array([side, 1, 1]); n = en * np.array([side, 1, 1])
            col = look if side == 1 else (lambda a, b, f=look: f(-a, b))
            cut = (lambda th: min(1.0, EYE_CUT(th))) if side == 1 else (lambda th: min(1.0, EYE_CUT(math.pi - th)))
            k.decal(p, n, (0.039, 0.034), col, eps=0.002, rings=20, segs=56, outline=cut)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    brutebonnet(*args())
