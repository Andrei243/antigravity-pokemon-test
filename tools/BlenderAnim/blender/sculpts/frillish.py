# Frillish (the male) sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/frillish.py -- <the export's Frillish.glb> <out.glb>
#
# From the references: a light blue jellyfish. A round head a little pointed at its top, under a small pale crown of
# points; a paler cap over the top of the head with a zigzag edge; round eyes (a navy rim, a blue iris, a red middle,
# a glint) at the head's middle, set wide; a small frown. A white ruffled collar under the head; under it a short stalk
# and five flat ribbon tentacles, deeper blue with pale edges: two held out to the sides ending in wavy paddles, two
# hanging down in front and curling in at their tips, one long down the middle.
# The skeleton: body (0, 0, 0.462), head (0, 0, 0.562) at the head's top, tentacle0..4 round (0, 0, 0.342).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, sheet, hull, lathe, srgb, inside, smoothstep, unit, eye_look, tri, outward, args)

BLUE, CAP, RIB, EDGE, COLLAR = "#6CC1EA", "#A9DDF3", "#58B0E2", "#C6E8F7", "#EEF6FB"
CROWN, NAVY, IRIS, RED, WHITE = "#D2EDF9", "#25337E", "#3F72D2", "#E5385A", "#FFFFFF"

# --- the frame ------------------------------------------------------------------------------------------------------
H, HR = np.array([0.0, 0.0, 0.47]), np.array([0.118, 0.11, 0.105])
VOXEL = 0.0052
EYE_ARC, EYE_Z, EYE_HALF = 0.054, 0.475, (0.022, 0.028)
MOUTH_Z = 0.428


def onion(V):
    """The head a little pointed at its top, under the crown."""
    V = V.copy()
    z = V[:, 2]
    V[:, :2] *= (1 - 0.2 * np.clip(z - 0.35, 0, None) ** 2)[:, None]
    V[:, 2] += 0.08 * np.clip(z - 0.6, 0, None)
    return V


def on_ell(c, r, d):
    d = unit(*d)
    return np.array(c, float) + d / math.sqrt(((d / np.array(r)) ** 2).sum())


def head_dir(arc, z, c=H, r=HR):
    th = arc / r[0]
    return unit(math.sin(th), -math.cos(th), (z - c[2]) / r[2] * 1.05)


def curve(pts, n):
    """A Catmull-Rom line through pts, resampled to n points evenly spaced along it."""
    p = np.array(pts, float)
    ext = np.vstack([2 * p[0] - p[1], p, 2 * p[-1] - p[-2]])
    out = []
    for i in range(len(p) - 1):
        p0, p1, p2, p3 = ext[i], ext[i + 1], ext[i + 2], ext[i + 3]
        for t in np.linspace(0, 1, 60, endpoint=False):
            out.append(0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t
                              + (-p0 + 3 * p1 - 3 * p2 + p3) * t ** 3))
    out.append(p[-1])
    out = np.array(out)
    s = np.concatenate([[0], np.cumsum(np.linalg.norm(np.diff(out, axis=0), axis=1))])
    t = np.linspace(0, s[-1], n)
    return np.stack([np.interp(t, s, out[:, k]) for k in range(3)], -1)


def ribbon(pts, width, normal, thick, t0=0.0, t1=1.0, scale=1.0, n=56, nv=9):
    """
    A flat ribbon along a line (a tentacle): width(t) its half width for t 0..1 along it, normal(t) or a vector the
    way its face looks. Returns the closed piece and its frame (points, side, face, half width, t) for laying spots.
    """
    t = np.linspace(t0, t1, n)
    P = curve(pts, 400)
    idx = np.clip((t * 399).round().astype(int), 0, 399)
    P = P[idx]
    T = np.gradient(P, axis=0); T /= np.linalg.norm(T, axis=1, keepdims=True)
    N = np.array([normal(x) if callable(normal) else normal for x in t], float)
    side = np.cross(T, N); side /= np.linalg.norm(side, axis=1, keepdims=True)
    face = np.cross(side, T)
    W = np.maximum(np.array([width(x) for x in t]) * scale, 0.0025)

    def at(u, v):
        return P[:, None, :] + side[:, None, :] * ((v - 0.5) * 2 * W[:, None])[..., None]
    return outward(sheet(at, n, nv, thick)), (P, side, face, W, t)


def tentacle(k, name, bone, pts, width, normal, core, edge, thick=0.02):
    """A ribbon of the core colour with a thinner, wider ribbon of the edge colour under it: a pale rim both sides."""
    piece, frame = ribbon(pts, width, normal, thick * 0.6, t0=0.0, t1=1.0)
    k.add(name + "edge", piece, edge, bone, fuse=False, surface="skin")
    piece, _ = ribbon(pts, width, normal, thick, t0=0.015, t1=0.94, scale=0.72)
    k.add(name, piece, core, bone, fuse=False, surface="skin")
    return frame


def crown(k, base, size, colour, bone="head", points=5):
    """A small cup on the head's top with points rising round its rim."""
    up = np.array([0, 0, 1.0])
    k.add("crowncup", lathe(base, up, [(0, -0.4 * size), (0.75 * size, -0.4 * size), (1.0 * size, 0.55 * size),
                                       (0.62 * size, 0.55 * size), (0, 0.2 * size)]), colour, bone, fuse=False,
          surface="skin")
    for i in range(points):
        a = 2 * math.pi * i / points
        d = np.array([math.sin(a), -math.cos(a), 0])
        s = np.array([math.cos(a), math.sin(a), 0])
        r0 = 0.82 * size
        pts = [base + d * r0 + s * w * size + up * 0.3 * size for w in (-0.36, 0.36)]
        pts += [base + d * (r0 - 0.42 * size) + up * 0.3 * size, base + d * (r0 + 0.1 * size) + up * 0.1 * size]
        pts += [base + d * (r0 + 0.25 * size) + up * 1.55 * size]
        k.add(f"crownpoint{i}", hull(pts), colour, bone, fuse=False, surface="skin")


def round_eye(rim, iris, middle, glint, ring=None):
    """A round eye: a rim, an iris, a red middle (and a white ring inside the rim where the design has one)."""
    def look(u, v):
        rho = np.hypot(u, v)
        c = np.tile(srgb(rim), (len(u), 1))
        c[rho < 0.8] = srgb(ring if ring else iris)
        if ring:
            c[rho < 0.6] = srgb(iris)
        c[(u ** 2 + (v + 0.04) ** 2) < 0.44 ** 2] = srgb(middle)
        c[((u + 0.22) ** 2 + (v - 0.3) ** 2) < 0.17 ** 2] = srgb(glint)
        return c
    return look


def frillish(src, out):
    k = Kit(src, coat="fur")
    # --- the head, its paler cap, and the stalk under it ----------------------------------------------------------
    k.add("head", ell(H, HR, deform=onion), BLUE, "body")
    k.add("stalk", ell((0, 0.005, 0.31), (0.042, 0.038, 0.09)), RIB, "body")

    def cap(P):
        q = P - H
        a = np.arctan2(q[:, 0], -q[:, 1])
        edge = H[2] + 0.064 + 0.011 * tri(a * 5 / (2 * math.pi))
        return inside(edge - P[:, 2], 0.0035)
    k.paint(cap, CAP, on=["head"])

    # --- the frown ---------------------------------------------------------------------------------------------------
    def frown(u, v):
        c = np.tile(srgb(BLUE), (len(u), 1))
        line = -0.45 + 0.95 * (1 - u * u) ** 1.5
        c[(np.abs(v - line) < 0.3) & (np.abs(u) < 0.85)] = srgb(NAVY)
        return c
    d = head_dir(0.0, MOUTH_Z)
    k.decal(on_ell(H, HR, d), d, (0.021, 0.012), frown, eps=0.003, rings=12, segs=64)

    # --- the crown ---------------------------------------------------------------------------------------------------
    crown(k, np.array([0, 0.004, H[2] + HR[2] + 0.004]), 0.024, CROWN)

    # --- the ruffled collar under the head --------------------------------------------------------------------------
    def ruff(u, v):
        th = 2 * math.pi * u                                   # 0 at the back: the seam behind
        v = 0.02 + 0.96 * v
        wave = np.cos(9 * th)
        r = 0.042 + (0.098 + 0.008 * wave - 0.042) * v ** 0.9
        z = 0.385 - 0.05 * v ** 1.3 + 0.013 * wave * v
        return np.stack([r * np.sin(th), r * np.cos(th), z], -1)
    k.add("collar", outward(sheet(ruff, 145, 10, 0.012)), COLLAR, "body", fuse=False, surface="skin")

    # --- the tentacles --------------------------------------------------------------------------------------------------
    def arm_w(t):
        w = 0.014 + 0.042 * smoothstep(0.15, 0.72, t)
        w *= 1 + 0.13 * math.sin(t * 5.5 * math.pi) * smoothstep(0.5, 0.8, t)
        return w * (math.sqrt(max(1 - ((t - 0.8) / 0.205) ** 2, 0)) if t > 0.8 else 1)

    def hang_w(t):
        return (0.016 + 0.032 * math.sin(math.pi * min(t / 0.85, 1) ** 0.8)) * (1 - smoothstep(0.7, 1.0, t) * 0.9)

    def mid_w(t):
        return (0.03 + 0.012 * math.sin(math.pi * t)) * (1 - smoothstep(0.55, 1.0, t) * 0.93)

    for s, bout, bfront in ((1, "tentacle3", "tentacle4"), (-1, "tentacle1", "tentacle0")):
        m = np.array([s, 1, 1.0])
        arm = [(0.025, 0.0, 0.335), (0.09, -0.035, 0.31), (0.155, -0.08, 0.28), (0.205, -0.115, 0.262),
               (0.235, -0.13, 0.262)]
        tentacle(k, f"arm{s}", bout, [np.array(p) * m for p in arm], arm_w, unit(-0.25 * s, -0.5, 0.83), RIB, EDGE)
        hang = [(0.02, -0.02, 0.32), (0.06, -0.045, 0.24), (0.1, -0.055, 0.15), (0.11, -0.05, 0.075),
                (0.082, -0.04, 0.025)]
        tentacle(k, f"hang{s}", bfront, [np.array(p) * m for p in hang], hang_w, unit(0.4 * s, -0.92, 0.0), RIB, EDGE)
    mid = [(0.0, 0.0, 0.3), (0.0, -0.012, 0.2), (0.005, -0.012, 0.11), (0.0, -0.002, 0.03)]
    tentacle(k, "mid", "tentacle2", mid, mid_w, unit(0, -1, 0), RIB, EDGE)

    k.build(voxel=VOXEL, smooth=3, band=0.006)

    # --- the eyes: round, set wide at the head's middle --------------------------------------------------------------
    look_open = round_eye(NAVY, IRIS, RED, WHITE)

    def eyes(state):
        for side in (1, -1):
            d = head_dir(EYE_ARC * side, EYE_Z)
            look = eye_look(state, look_open, srgb(BLUE), srgb(NAVY), line=0.17)
            col = look if side == 1 else (lambda u, v, f=look: f(-u, v))
            k.decal(on_ell(H, HR, d), d, EYE_HALF, col, eps=0.0035, rings=20, segs=56)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    frillish(*args())
