# Palossand sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown from
# Sandygast's script (its puddle, pebbles, spade blade, carved arch and way of making eyes):
#   blender --background --factory-startup --python sculpts/palossand.py -- <the export's Palossand.glb> <out.glb>
#
# From the references: a sandcastle of warm tan sand on a wide flat puddle with a wavy rim and pale pebbles. In the
# middle a square front wall with merlons at its corners and an arched dark gateway (the mouth), an arc of five small
# shells over it (blue, pink, white, pink, blue); behind it the round keep, its top a rounded point, a ring of pink and
# blue shells round the top and four dark slot windows round its middle, the two in front its eyes; on its point the
# red spade's blade standing up like a flag's tip. Two round towers for arms rise out of the puddle and lean out, each
# crenellated at its top with dark slot windows, the viewer's left one (-X) larger and leaning further.
# The skeleton: root (0, 0, 0), body (0, 0, 0.3), armL (-0.19, 0.01, 0.08), armR (0.19, 0.01, 0.08). The export stands
# 0.67 tall.
# The pictures overrule the task's text: the spade on top is the blade's tip alone, pointing up, no dish; the eyes are
# the keep's two front windows.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, lathe, polygon, boxy, axes, sibling, srgb, inside, unit, eye_look, args

sg = sibling("sandygast")

SAND, DARK, HOLLOW = "#E9AE68", "#55311F", "#6B4431"
BLUE, PINK, WHITE, RED, PEBBLE = "#A6D3EE", "#F3B2C3", "#F7F1E4", "#E4464D", "#F4E6C8"

WALL_C, WALL_R = np.array([0.0, -0.03, 0.2]), (0.2, 0.13, 0.152)
FRONT_Y = WALL_C[1] - WALL_R[1]
KEEP_C, KEEP_R = np.array([0.0, 0.05, 0.1]), 0.13
KEEP = [(KEEP_R, 0.0), (KEEP_R, 0.36), (0.125, 0.395), (0.112, 0.43), (0.093, 0.46), (0.068, 0.483),
        (0.04, 0.497), (0.0, 0.505)]
DOOR_C, DOOR_HALF = np.array([0.0, FRONT_Y + 0.004, 0.205]), (0.062, 0.05, 0.072)
WIN_Z = 0.43
EYE_ANGLE, EYE_HALF = 12, (0.016, 0.03)
MARK_ANGLE, MARK_HALF = 33, (0.012, 0.026)
SLOT = lambda th: (abs(math.cos(th)) ** 4 + abs(math.sin(th)) ** 4) ** -0.25
TRI_DOWN = polygon([(-1.0, 0.75), (1.0, 0.75), (0.0, -1.0)])

# (base, lean out in degrees, length, base radius, top radius) of each tower, +X first
TOWERS = {"R": (np.array([0.18, 0.0, 0.065]), 20, 0.285, 0.098, 0.078),
          "L": (np.array([-0.18, 0.0, 0.065]), 31, 0.305, 0.105, 0.086)}


def keep_point(angle, z):
    """A point on the keep's wall at angle degrees round from the front (+ toward +X) and height z, its normal."""
    a = math.radians(angle)
    n = np.array([math.sin(a), -math.cos(a), 0.0])
    t = z - KEEP_C[2]
    r = np.interp(t, [p[1] for p in KEEP], [p[0] for p in KEEP])
    return KEEP_C + n * r + (0, 0, t), n


def tower(k, side):
    base, lean, length, rb, rt = TOWERS[side]
    s = 1 if side == "R" else -1
    a = unit(math.sin(math.radians(lean)) * s, -0.12, math.cos(math.radians(lean)))
    bone = "arm" + side
    k.add(f"tower{side}", lathe(base, a, [(0.0, 0.0), (rb, 0.0), (rb * 0.97, length * 0.5), (rt, length),
                                         (0.0, length)], seg=48), SAND, bone)
    e1, e2 = axes(a)
    top = base + a * length
    for i in range(4):
        th = math.radians(45 + 90 * i)
        d = math.cos(th) * e1 + math.sin(th) * e2
        k.add(f"merlon{side}{i}", ell(top + a * 0.022 + d * rt * 0.68, (0.027, 0.027, 0.03), deform=boxy(4)), SAND,
              bone)
    # the face of the tower turned to the front, and its windows there
    f = np.array([0.0, -1.0, 0.0]) - a * (-a[1]); f /= np.linalg.norm(f)
    sd = np.cross(a, f)
    wins = [(0.4, 2 * s), (0.42, 36 * s), (0.68, 18 * s)]
    for t, ang in wins:
        th = math.radians(ang)
        d = math.cos(th) * f + math.sin(th) * sd
        r = rb + (rt - rb) * t
        k.decal(base + a * length * t + d * r, d, (0.012, 0.022), DARK, up=a, outline=SLOT, eps=0.003, rings=10,
                segs=40)


def shell(k, at, n, colour, half=0.019, up=(0, 0, 1)):
    k.decal(at, n, (half, half), colour, up=up, outline=TRI_DOWN, eps=0.0032, rings=8, segs=36)


def palossand(k):
    sg.puddle(k, "puddle", (0, 0.0, 0.036), 0.39, 0.27, 0.052, SAND, "body", n=8, amp=0.07)
    k.add("mound", ell((0, 0.02, 0.085), (0.27, 0.2, 0.085)), SAND, "body")
    k.add("wall", ell(WALL_C, WALL_R, deform=boxy(6)), SAND, "body")
    for x in (0.163, -0.163):
        for y in (FRONT_Y + 0.035, WALL_C[1] + WALL_R[1] - 0.035):
            k.add(f"corner{x:+.2f}{y:+.2f}", ell((x, y, WALL_C[2] + WALL_R[2] + 0.02), (0.037, 0.037, 0.036),
                                                deform=boxy(4)), SAND, "body")
    k.add("keep", sg.mound(KEEP, KEEP_C, 1.0), SAND, "body")
    for side in ("R", "L"):
        tower(k, side)

    # The gateway, carved, dark within
    k.add("door", sg.arch(DOOR_C, DOOR_HALF, 0), HOLLOW, "body", carve=True)
    k.paint(lambda P: sg.opening_mask(DOOR_C, DOOR_HALF, 0)(P) * (P[:, 1] > FRONT_Y - 0.02)
            * (P[:, 1] < FRONT_Y + 0.07), HOLLOW)

    # Shells: an arc over the gateway, a ring round the keep's top
    for i, (a, col) in enumerate(zip((-62, -31, 0, 31, 62), (BLUE, PINK, WHITE, PINK, BLUE))):
        r = math.radians(a)
        at = (0.094 * math.sin(r), FRONT_Y, DOOR_C[2] + 0.098 * math.cos(r) + 0.004)
        shell(k, at, unit(0, -1, 0), col)
    for i, a in enumerate((-80, -54, -27, 0, 27, 54, 80)):
        at, n = keep_point(a, 0.535)
        shell(k, at, unit(n[0], n[1], 0.45), (PINK, BLUE)[i % 2], half=0.018)

    # The side windows of the keep (the front pair are its eyes)
    for s in (1, -1):
        at, n = keep_point(MARK_ANGLE * s, WIN_Z)
        k.decal(at, n, MARK_HALF, DARK, outline=SLOT, eps=0.003, rings=10, segs=40)

    sg.pebbles(k, [(-0.3, -0.13, 0.04), (-0.1, -0.24, 0.04), (0.12, -0.24, 0.04), (0.31, -0.12, 0.04)], PEBBLE,
               half=(0.014, 0.009))

    # The spade's blade on the keep's point, upside down: its tip up
    sg.spade_blade(k, "blade", (0.0, KEEP_C[1], 0.624), 180, 0.62, RED, "body", 0.03)


def slot_open(u, v):
    return np.tile(srgb(DARK), (len(u), 1))


def palossand_eyes(k, state):
    look = eye_look(state, slot_open, srgb(SAND), srgb(DARK), line=0.17)
    for s in (1, -1):
        at, n = keep_point(EYE_ANGLE * s, WIN_Z)
        lk = look if s > 0 else (lambda f: (lambda u, v: f(-u, v)))(look)
        k.decal(at, n, EYE_HALF, lk, outline=SLOT, eps=0.0045, rings=20, segs=56)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="fur")
    palossand(k)
    k.build(voxel=0.0065, smooth=3, band=0.008, wband=0.07)
    k.eye_states(lambda state: palossand_eyes(k, state))
    k.export(out)
