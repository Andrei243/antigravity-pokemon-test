# Dedenne sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/dedenne.py -- <the export's Dedenne.glb> <out.glb>
#
# From the references: a small round orange mouse whose head and body are one mass, a gumdrop wider at the bottom,
# cream over the lower belly. Two big round ears up and out at the top's sides, thick black rims round pale yellow
# insides. Black oval eyes with a white glint, a tiny dark nose, an open smile, pink, with one white tooth; round
# orange-red cheeks, and from each cheek a long black whisker out and up like an antenna, three short bars across its
# outer half. Little orange arms hold the thin black tail across the belly: it comes from behind round the +X side,
# passes under the paws and out to the -X side, ending in a black spiky tuft. Tiny orange feet.
# The export's joints: body (0, 0, 0.09), head (0, -0.01, 0.13), ears (+-0.05, -0.005, 0.21), arms (+-0.05, -0.03,
# 0.09), tail (0, 0.05, 0.05), legs (+-0.035, 0, 0.04). About 0.31 tall.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, outward, srgb, inside, smoothstep, unit, eye_look, args

ORANGE, CREAM, PALE, BLACK = "#F2A033", "#F6EBB4", "#F4F1B8", "#38363C"
CHEEK, MOUTH, TONGUE, INK, WHITE = "#E9672C", "#9C2C34", "#EE8A96", "#1A181C", "#FFFFFF"

C = np.array([0.0, 0.0, 0.112])             # the body's centre: head and body are one
R = np.array([0.094, 0.082, 0.112])


def gumdrop(V):
    V = V.copy()
    s = 1 - 0.13 * V[:, 2]
    V[:, 0] *= s; V[:, 1] *= s
    V[:, 2] = np.where(V[:, 2] < -0.6, -0.6 + (V[:, 2] + 0.6) * 0.75, V[:, 2])     # a flatter seat
    return V


def stick(a, b, r):
    return outward(tube([a, b], [r, r * 0.9], seg=10))


def dedenne(src, out):
    k = Kit(src, coat="fur")

    # --- the one round mass, a little chin of cheeks ---------------------------------------------------------------
    k.add("body", ell(C, R, deform=gumdrop), ORANGE, "body")
    k.add("top", ell(C + (0, -0.008, 0.05), (0.075, 0.066, 0.06)), ORANGE, "head")

    def belly(P):
        d = P - C
        front = smoothstep(-0.01, 0.03, -d[:, 1])
        edge = -0.035 - 0.012 * (d[:, 0] / R[0]) ** 2
        return inside(d[:, 2] - edge, 0.0015) * front
    k.paint(belly, CREAM, on=["body"])

    # --- tiny feet and little arms holding the tail before the belly ---------------------------------------------------
    k.add("foot", ell((0.04, -0.048, 0.013), (0.019, 0.027, 0.013)), ORANGE, "leg{s}", mirror=True)
    k.add("arm", tube([(0.06, -0.035, 0.095), (0.058, -0.068, 0.084), (0.044, -0.085, 0.078)], [0.017, 0.015, 0.014]),
          ORANGE, "arm{s}", mirror=True)
    k.add("paw", ell((0.04, -0.091, 0.077), (0.015, 0.013, 0.015)), ORANGE, "arm{s}", mirror=True)

    # --- the ears: big round discs up and out, black rims round pale insides ---------------------------------------------
    for s in (1, -1):
        side = "R" if s > 0 else "L"
        c = np.array([0.081 * s, 0.002, 0.246])
        rot = (0, 26 * s, 14 * s)
        m = np.array(__import__("mathutils").Euler([0, math.radians(26 * s), math.radians(14 * s)]).to_matrix())
        n = m @ np.array([0, -1.0, 0])
        k.add(f"ear{side}", ell(c, (0.048, 0.009, 0.05), rot=rot, sub=3), BLACK, f"ear{side}", fuse=False)
        k.add(f"earin{side}", ell(c + n * 0.0035, (0.037, 0.0075, 0.039), rot=rot, sub=3), PALE, f"ear{side}",
              fuse=False)

    # --- the whiskers: black antennae from the cheeks, out and up, three bars across their outer half -----------------
    for s in (1, -1):
        side = "R" if s > 0 else "L"
        a = np.array([0.066 * s, -0.05, 0.137])
        d = unit(1 * s, -0.12, 0.55)
        L = 0.088
        k.add(f"whisker{side}", stick(a, a + d * L, 0.0055), BLACK, "head", fuse=False)
        across = unit(-d[2] * s, 0, d[0] * s) * s
        across = unit(-d[2], 0, d[0])
        for i, (t, w) in enumerate(((0.55, 0.016), (0.72, 0.014), (0.88, 0.011))):
            p = a + d * L * t + d * 0.006
            k.add(f"bar{side}{i}", stick(p - across * w * 0.7, p + across * w, 0.0046), BLACK, "head", fuse=False)

    # --- the thin black tail: from behind round the +X side, under the paws, out to -X and its spiky tuft -----------
    back = [(0.0, 0.07, 0.05), (0.06, 0.075, 0.042), (0.1, 0.03, 0.045), (0.085, -0.055, 0.06), (0.05, -0.094, 0.071)]
    front = [(0.05, -0.094, 0.071), (0.0, -0.1, 0.062), (-0.05, -0.094, 0.07), (-0.095, -0.07, 0.068),
             (-0.13, -0.045, 0.072), (-0.148, -0.03, 0.08)]
    k.add("tail", outward(tube(back, [0.0062] * len(back), seg=12)), BLACK, "tail", fuse=False)
    k.add("tailfront", outward(tube(front, [0.0062] * len(front), seg=12)), BLACK, "body", fuse=False)
    tip = np.array((-0.16, -0.025, 0.084))
    k.add("tuft", ell(tip, (0.014, 0.01, 0.014), sub=3), BLACK, "body", fuse=False)
    face = unit(-0.25, -1, 0)
    sidev = np.cross(face, (0, 0, 1.0)); sidev /= np.linalg.norm(sidev)
    for i, ang in enumerate(np.radians([0, 55, 110, 165, 215, 270, 320])):
        out_ = math.cos(ang) * sidev + math.sin(ang) * np.array([0, 0, 1.0])
        w = np.cross(out_, face)
        L = 0.026 if i % 2 == 0 else 0.02
        pts = [tip + w * 0.007, tip - w * 0.007, tip + face * 0.005, tip - face * 0.005, tip + out_ * L]
        k.add(f"spike{i}", hull(pts), BLACK, "body", fuse=False)

    # --- the face: cheeks, a dot of a nose, an open smile with one tooth ------------------------------------------------
    k.decal((0.074, -0.05, 0.14), unit(0.75, -0.62, 0.05), (0.021, 0.021), CHEEK, eps=0.0018, rings=10, segs=40,
            mirror=True)
    k.decal((0, -0.08, 0.159), unit(0, -1, 0.0), (0.0035, 0.0028), INK, eps=0.0018, rings=5, segs=20)
    CM, CT, CW = srgb(MOUTH), srgb(TONGUE), srgb(WHITE)

    def mouth(u, v):
        c = np.tile(CM, (len(u), 1))
        c[(u / 0.62) ** 2 + ((v + 0.8) / 0.62) ** 2 < 1] = CT
        c[(np.abs(u) < 0.2) & (v > 0.0)] = CW
        return c

    def mouth_shape(th):
        s = math.sin(th)
        return min(1.0, 0.3 / s) if s > 0 else 1.0
    k.decal((0, -0.08, 0.143), unit(0, -1, -0.15), (0.019, 0.016), mouth, eps=0.0018, outline=mouth_shape, rings=22,
            segs=72)

    k.build(voxel=0.0032, band=0.004, wband=0.03)

    # --- black oval eyes with a white glint ---------------------------------------------------------------------------
    CO, CI = srgb(ORANGE), srgb(INK)

    def eye(u, v):
        c = np.tile(CI, (len(u), 1))
        c[((u + 0.05) / 0.36) ** 2 + ((v - 0.36) / 0.3) ** 2 < 1] = CW
        return c

    def eyes(state):
        k.decal((0.037, -0.075, 0.17), unit(0.38, -0.92, 0.06), (0.012, 0.016), eye_look(state, eye, CO, CI),
                eps=0.0018, rings=20, segs=56, mirror=True)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    dedenne(*args())
