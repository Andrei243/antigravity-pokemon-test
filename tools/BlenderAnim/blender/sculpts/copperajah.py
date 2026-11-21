# Copperajah sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/copperajah.py -- <the export's Copperajah.glb> <out.glb>
#
# Cufant grown (cufant.py's frame, by sibling: its blotches, its zigzag socks, its way of laying the face). From the
# references: a great dark teal elephant of weathered copper. Its trunk is the largest mass: out of the face under the
# brow it arches up over the head and forward, then hangs down far in front, broad and round, its end a flat club with
# four long white claws down its front and a pale cyan band with a wavy upper edge round its tip. The head is a high
# block with a flat top, copper orange over the crown and down the trunk's upper side, patched with teal; small sleepy
# eyes under heavy lids at its sides; a short square white tusk out of each side of the face beside the trunk's root,
# forward and a little out; an open pink mouth under the trunk. Small square ears flat against the head's sides, toward
# the back. A barrel of a body, the back sloping down from the head to the rump, pale cyan down the chest and under the
# belly with a wavy edge, orange curling strokes on the flanks. Four thick legs, each foot an orange cuff with a zigzag
# top over three great white nails. A thin tail with a cyan ball at its end.
# (The pictures overrule the brief: the trunk is no block but a broad round arch with a club at its end, the toes are
# white nails under an orange zigzag cuff, and the front legs stand on the ground: the 3D picture's raised forefoot is
# a pose, and the skeleton is a quadruped's.)
# Bones: body (0, 0.02, 0.42), head (0, -0.22, 0.48), ears (+-0.11, -0.27, 0.54), legs (+-0.12, -0.17, 0.28), hind
# legs (+-0.12, 0.2, 0.28), tail (0, 0.29, 0.47).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, hull, srgb, inside, unit, eye_look, args, sibling, polygon, tri, boxy,
                       outward)

cu = sibling("cufant")
ph = cu.ph
TEAL, CYAN, ORANGE, WHITE = "#176469", "#45C9BF", "#E4872E", "#E9EFEE"
INK, SCLERA, MOUTH, TONGUE = "#1A1A1E", "#E8DC96", "#8E2E3C", "#E8909A"
E_ = cu.E_

H, HR = np.array([0.0, -0.3, 0.765]), np.array([0.155, 0.14, 0.15])        # the head's block
TRUNK = [(0.0, -0.37, 0.8), (0.0, -0.45, 0.915), (0.0, -0.555, 0.965), (0.0, -0.645, 0.905), (0.0, -0.69, 0.785),
         (0.0, -0.695, 0.635), (0.0, -0.68, 0.5)]
TRUNK_R = [0.1, 0.12, 0.135, 0.145, 0.155, 0.162, 0.165]
EYE_D = unit(0.8, -0.55, 0.2)


def centre_z(y):
    """The trunk's middle line's height over a place along y, on its rising half (from the face to its top)."""
    P = np.array(TRUNK)[:3]
    return np.interp(-y, -P[:, 1], P[:, 2])


def copperajah(src, out):
    k = Kit(src, coat="shell")
    # the body: a barrel, the chest high and full in front, the back sloping down to the rump
    k.add("torso", ell((0, 0.04, 0.45), (0.215, 0.31, 0.22)), TEAL, "body")
    k.add("chest", ell((0, -0.17, 0.51), (0.205, 0.17, 0.22)), TEAL, "body")
    k.add("rump", ell((0, 0.25, 0.47), (0.18, 0.12, 0.17)), TEAL, "body")
    k.add("neck", ell((0, -0.24, 0.66), (0.165, 0.14, 0.14)), TEAL, "head")
    # the head: a high block, a flat top, the jaw under the face
    k.add("head", ell(H, HR, deform=boxy(3.2)), TEAL, "head")
    k.add("jaw", ell((0, -0.37, 0.615), (0.1, 0.085, 0.07)), TEAL, "head")
    # the trunk: up from under the brow, over and forward, down to a broad club
    k.add("trunk", tube(TRUNK, TRUNK_R, seg=24), TEAL, "head")
    k.add("club", ell((0, -0.685, 0.47), (0.185, 0.15, 0.12)), TEAL, "head")
    # copper over the crown and along the trunk's rising top, patched with teal
    def copper(P):
        crown = inside(0.85 + 0.015 * tri(P[:, 0] * 9) - P[:, 2], E_) * (P[:, 1] < -0.15) * (P[:, 1] > -0.46)
        rising = (P[:, 1] < -0.33) * (P[:, 1] > -0.68) * inside(centre_z(P[:, 1]) + 0.015 - P[:, 2], E_) \
            * (np.abs(P[:, 0]) < 0.115)
        return np.maximum(crown, rising)
    k.paint(copper, ORANGE, on=["head", "trunk", "neck"])
    eye = cu.on_ell(H, HR, EYE_D)
    blot = []
    for s in (1, -1):
        blot += [((0.1 * s, -0.24, 0.9), 0.035), ((0.05 * s, -0.5, 0.99), 0.03), ((0.07 * s, -0.36, 0.92), 0.022)]
    cu.spots(k, blot, TEAL, on=["head", "trunk"])
    # the trunk's tip: a cyan band with a wavy upper edge
    def band(P):
        return inside(P[:, 2] - (0.43 + 0.03 * (0.5 + 0.5 * tri(P[:, 0] * 7 + 0.25))), E_) * (P[:, 1] < -0.55)
    k.paint(band, CYAN, on=["trunk", "club"])
    # the four white claws down the club's front, ending at the band
    CLAW = polygon([(0.0, -1.0), (0.7, -0.6), (0.85, 0.4), (0.5, 1.0), (-0.5, 1.0), (-0.85, 0.4), (-0.7, -0.6)])
    for x in (-0.105, -0.036, 0.036, 0.105):
        r = 0.162
        yf = -0.695 - math.sqrt(max(r * r - x * x, 0.0)) * 0.98
        n = unit(x / r, -1.0, -0.05)
        k.decal((x, yf, 0.59), n, (0.019, 0.095), WHITE, eps=0.003, outline=CLAW, rings=14, segs=72, up=(0, 0, 1))
    # pale cyan down the chest and under the belly, a wavy edge
    def underside(P):
        low = inside(P[:, 2] - (0.33 + 0.025 * tri(P[:, 1] * 6)), E_)
        chest = inside(P[:, 1] + 0.27 - 0.03 * tri(P[:, 2] * 7), E_) * inside(np.abs(P[:, 0]) - 0.13, E_) \
            * inside(P[:, 2] - 0.6, E_)
        return np.maximum(low, chest)
    k.paint(underside, CYAN, on=["torso", "chest", "rump"])
    # orange strokes curling down the flanks
    strokes = [[(0.1, 0.62), (0.07, 0.54), (0.1, 0.47), (0.16, 0.45)],
               [(0.21, 0.63), (0.19, 0.53), (0.23, 0.44), (0.28, 0.42)]]
    def flanks(P):
        m = np.zeros(len(P))
        side = inside(0.13 - np.abs(P[:, 0]), E_)
        for line in strokes:
            L = np.array(line)
            for a, b in zip(L[:-1], L[1:]):
                q = np.stack([P[:, 1], P[:, 2]], axis=1)
                e = b - a
                t = np.clip(((q - a) @ e) / (e @ e), 0, 1)
                d = np.linalg.norm(q - (a + t[:, None] * e), axis=1)
                m = np.maximum(m, inside(d - 0.016, E_) * side)
        return m
    k.paint(flanks, ORANGE, on=["torso", "rump"])
    # the open mouth under the trunk, pink within
    CM, CT = srgb(MOUTH), srgb(TONGUE)
    def mouth(u, v):
        c = np.tile(CM, (len(u), 1))
        c[(u / 0.6) ** 2 + ((v + 0.45) / 0.55) ** 2 < 1] = CT
        return c
    k.decal((0, -0.415, 0.585), unit(0, -0.3, -0.95), (0.056, 0.04), mouth, eps=0.004, rings=18, segs=64,
            up=(0, -0.95, 0.3))
    # four thick legs: fat shoulders and haunches fused in, short thick shins, an orange zigzag cuff over white nails
    for name, y, bone in (("leg", -0.17, "leg{s}"), ("hind", 0.2, "hind{s}")):
        x = 0.135
        k.add(name + "top", ell((x, y, 0.3), (0.135, 0.145, 0.15)), TEAL, bone, mirror=True)
        k.add(name, tube([(x, y, 0.3), (x + 0.004, y - 0.006, 0.19), (x + 0.004, y - 0.01, 0.12)], [0.11, 0.1, 0.098]),
              TEAL, bone, mirror=True)
        k.add(name + "foot", ell((x, y - 0.012, 0.055), (0.108, 0.118, 0.055)), TEAL, bone, mirror=True)
    feet = [(sx * 0.136, y - 0.012, 5) for sx in (1, -1) for y in (-0.17, 0.2)]
    cu.socks(k, feet, 0.1, 0.05, ORANGE, on=["leg", "hind"])
    for name, y, bone in (("nail", -0.17, "leg{s}"), ("hnail", 0.2, "hind{s}")):
        fc = np.array([0.135, y - 0.012, 0.0])
        for j, a in enumerate((-40, 0, 40)):
            t = math.radians(a)
            d = np.array([math.sin(t), -math.cos(t), 0.0])
            side = np.array([math.cos(t), math.sin(t), 0.0])
            base = fc + d * 0.1
            w, h, th = 0.032, 0.09, 0.03
            pts = [base + side * sx * w + d * dd + (0, 0, z) for sx in (-1, 1) for dd in (-th, th * 0.5)
                   for z in (0.003, h)]
            pts += [base + d * th * 0.7 + side * sx * w * 0.7 + (0, 0, h * 0.5) for sx in (-1, 1)]
            k.add(f"{name}{j}", hull(pts), WHITE, bone, fuse=False, mirror=True, surface="shell")
    # the tusks: short square white blocks out of the face beside the trunk, forward and a little out
    for s in (1,):
        d = unit(0.38, -1.0, -0.08)
        side = np.cross(d, (0, 0, 1.0)); side /= np.linalg.norm(side)
        up = np.cross(side, d)
        root = np.array([0.095, -0.39, 0.69])
        pts = []
        for t, sc in ((0.0, 1.0), (0.2, 0.92)):
            for sx in (-1, 1):
                for sz in (-1, 1):
                    pts.append(root + d * t + side * sx * 0.042 * sc + up * sz * 0.04 * sc)
        k.add("tusk", hull(pts), WHITE, "head", fuse=False, mirror=True, surface="shell")
    # small square ears flat against the head's sides toward the back
    k.add("ear", cu.frame_ell((0.168, -0.22, 0.77), (0.095, 0.032, 0.085), unit(-0.25, -1, 0), unit(1, -0.25, 0),
                              deform=boxy(3.0)), TEAL, "ear{s}", fuse=False, mirror=True, surface="shell")
    # a thin tail with a cyan ball at its end
    k.add("tail", tube([(0.0, 0.34, 0.56), (0.02, 0.41, 0.5), (0.04, 0.44, 0.42), (0.05, 0.45, 0.37)],
                       [0.026, 0.022, 0.02, 0.02]), TEAL, "tail")
    k.add("tailball", ell((0.05, 0.452, 0.345), (0.038, 0.038, 0.042)), CYAN, "tail")
    k.build(voxel=0.0088, smooth=5, band=0.008)

    # sleepy eyes: a pale almond under a heavy lid, a dark pupil looking forward
    T_, K, Sc = srgb(TEAL), srgb(INK), srgb(SCLERA)
    def sleepy(u, v):
        c = np.tile(Sc, (len(u), 1))
        c[((u + 0.25) / 0.42) ** 2 + ((v + 0.1) / 0.75) ** 2 < 1] = K
        c[v > 0.18 - 0.12 * u] = T_
        c[np.abs(v - (0.18 - 0.12 * u)) < 0.13] = K
        return c
    lid = polygon([(-1.0, 0.05), (-0.6, -0.6), (0.0, -0.8), (0.6, -0.6), (1.0, 0.05), (0.7, 0.8), (-0.7, 0.8)])
    def lay(state):
        if state == "Fierce":
            def look(u, v):
                c = sleepy(u, v)
                c[v > 0.05 + 0.4 * u] = T_
                c[np.abs(v - (0.05 + 0.4 * u)) < 0.13] = K
                return c
        else:
            look = eye_look(state, sleepy, T_, K, line=0.17)
        k.decal(eye, EYE_D, (0.044, 0.03), look, eps=0.002, rings=20, segs=56, mirror=True, outline=lid)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    copperajah(*args())
