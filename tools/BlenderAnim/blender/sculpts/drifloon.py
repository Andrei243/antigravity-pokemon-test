# Drifloon sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/drifloon.py -- <the export's Drifloon.glb> <out.glb>
#
# From the references: a round purple balloon; a white tuft of cloud on its top, sat over to its -X side and a little
# back, with a thin white curl rising from the cloud's +X end; a chunky yellow plus (not an X) on the face, a little
# below the middle, tilted some 20 degrees clockwise; two small black upright oval eyes set wide either side of it; a
# lavender paper frill of a knot under the balloon; two thin black strings from the knot that bow out and down and end
# in small yellow hearts hung by their points.
# The skeleton: body (0, 0, 0.72), armR/armL at (+-0.05, -0.02, 0.58), the knot under the balloon.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, eye_look, polygon, args

src, out = args()
k = Kit(src, coat="fur")

PURPLE, KNOT, CLOUD, SHADE, YELLOW, INK, STRING = ("#A48ACD", "#BFAAE4", "#F7F8FB", "#CDD6E6", "#F5CB3A",
                                                  "#18141E", "#26222C")
C = np.array([0.0, 0.0, 0.72])
R = np.array([0.152, 0.148, 0.152])

# --- the balloon -------------------------------------------------------------------------------------------------
k.add("balloon", ell(C, R), PURPLE, "body")

# --- the cloud: fused puffs on the top, over to -X and a little back ------------------------------------------------
CC = np.array([-0.055, 0.02, 0.87])
PUFFS = [(-0.07, 0.0, -0.005, 0.042), (-0.035, -0.01, 0.03, 0.046), (0.01, -0.005, 0.042, 0.048),
         (0.055, 0.0, 0.02, 0.042), (0.075, 0.005, -0.02, 0.034), (0.03, -0.02, -0.03, 0.04),
         (-0.02, -0.025, -0.03, 0.042), (-0.075, 0.01, -0.04, 0.032), (0.0, 0.035, 0.005, 0.05),
         (-0.045, 0.03, 0.0, 0.042), (0.045, 0.03, -0.01, 0.04), (0.0, 0.0, 0.0, 0.055)]
for i, (dx, dy, dz, r) in enumerate(PUFFS):
    k.add(f"cloud{i}_", ell(CC + np.array([dx, dy, dz]), (r, r * 0.9, r * 0.85)), CLOUD, "body")
# a pale blue-grey shade along the cloud's underside, as the pictures have it
k.paint(lambda P: smoothstep(CC[2] - 0.0, CC[2] - 0.045, P[:, 2]) * 0.8, SHADE, on=["cloud"])
# the curl rising from the cloud's +X end
k.add("curl", blade([(0.015, 0.03, 0.9), (0.045, 0.03, 0.935), (0.062, 0.025, 0.968), (0.068, 0.02, 0.99)],
                    [0.016, 0.012, 0.007, 0.002], (0, 1, 0), 0.45), CLOUD, "body", fuse=False)

# --- the knot: a little neck and a frill of paper points hanging under the balloon -----------------------------------
KB = np.array([0.0, -0.01, 0.585])
k.add("knot", ell(KB + (0, 0, 0.0), (0.04, 0.036, 0.03)), KNOT, "body")
for i in range(6):
    a = math.radians(30 + 60 * i)
    d = np.array([math.sin(a), -math.cos(a), 0])
    p0 = KB + d * 0.022 + (0, 0, -0.005)
    p1 = KB + d * 0.05 + (0, 0, -0.042)
    k.add(f"frill{i}", blade([p0, (p0 + p1) / 2, p1], [0.02, 0.014, 0.002], np.cross(d, (0, 0, 1)) * 0 + d, 0.35),
          KNOT, "body", fuse=False)

# --- the plus on the face: a crisp patch, its arms rounded at their ends ---------------------------------------------
def plus_outline(w=0.42, n=6):
    pts = []
    for q in range(4):
        a = q * math.pi / 2
        d = np.array([math.cos(a), math.sin(a)]); s = np.array([-d[1], d[0]])
        pts.append(d * w - s * w)                              # the inner corner before this arm
        e = d * (1 - w)
        for j in range(n + 1):
            t = -math.pi / 2 + math.pi * j / n
            pts.append(e + w * (math.cos(t) * d + math.sin(t) * s))
    tilt = math.radians(-20)
    m = np.array([[math.cos(tilt), -math.sin(tilt)], [math.sin(tilt), math.cos(tilt)]])
    return polygon(np.array(pts) @ m.T)
dc = unit(0.02, -1, -0.16)
k.decal(C + dc * R, dc, (0.07, 0.07), YELLOW, eps=0.004, outline=plus_outline(0.36), rings=16, segs=120)

def heart(tip, tilt, size, thick=0.013):
    """A flat, puffy heart hung upside down by its point at tip, its face to the front, turned by tilt degrees."""
    t = np.linspace(0, 2 * np.pi, 721)
    hx = 16 * np.sin(t) ** 3
    hz = -(13 * np.cos(t) - 5 * np.cos(2 * t) - 2 * np.cos(3 * t) - np.cos(4 * t))     # point up
    hz -= 1.5
    pa = np.arctan2(hz, hx); pr = np.hypot(hx, hz) / 17.0
    o = np.argsort(pa); pa, pr = pa[o], pr[o]
    def f(V):
        V = V.copy()
        a = np.arctan2(V[:, 2], V[:, 0])
        r = np.interp(a, pa, pr, period=2 * np.pi)
        V[:, 0] *= r; V[:, 2] *= r
        V[:, 1] *= np.sqrt(np.clip(1 - 0.6 * (V[:, 0] ** 2 + V[:, 2] ** 2), 0.05, 1))
        return V
    V, F = ell((0, 0, 0), (size, thick, size), deform=f)
    # the point (top of the curve, at z = (17 - 1.5) / 17 of size) onto the string's end
    V[:, 2] -= size * 15.5 / 17
    a = math.radians(tilt)
    m = np.array([[math.cos(a), 0, math.sin(a)], [0, 1, 0], [-math.sin(a), 0, math.cos(a)]])
    return V @ m.T + np.array(tip) + (0, -0.002, 0.004), F


# --- the strings: from the knot, bowing out and down, each ending in a heart hung by its point ---------------------
for s, sd in ((1, "R"), (-1, "L")):
    pts = np.array([(0.05, -0.02, 0.578), (0.125, -0.03, 0.55), (0.178, -0.04, 0.46), (0.182, -0.045, 0.35),
                    (0.162, -0.05, 0.255), (0.128, -0.055, 0.195)]) * np.array([s, 1, 1])
    k.add(f"string{sd}", tube(pts, [0.008] * len(pts), seg=10, per=6), STRING, f"arm{sd}", fuse=False)
    # the heart, upside down: its point on the string's end, its two lobes below, turned a little inward
    tip = pts[-1]
    k.add(f"heart{sd}", heart(tip, -22 * s, 0.037), YELLOW, f"arm{sd}", fuse=False)

k.build(voxel=0.0045, smooth=3)

# --- the eyes: small black upright ovals, set wide ----------------------------------------------------------------
CI, CL = srgb(INK), srgb(PURPLE)
def eye(u, v):
    return np.tile(CI, (len(u), 1))
def eyes(state):
    for s in (1, -1):
        dn = unit(s * 0.56, -0.8, 0.05)
        look = eye_look(state, eye, CL, CI, line=0.2)
        lk = look if s > 0 else (lambda f: (lambda u, v: f(-u, v)))(look)
        k.decal(C + dn * R, dn, (0.016, 0.024), lk, eps=0.004, rings=20, segs=56)
k.eye_states(eyes)
k.export(out)
