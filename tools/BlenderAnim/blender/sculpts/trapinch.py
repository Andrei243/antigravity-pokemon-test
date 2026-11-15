# Trapinch sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/trapinch.py -- <the export's Trapinch.glb> <out.glb>
# From the references: an orange ant-lion larva that is mostly head. A great egg of a head held tilted up and forward
# on a short neck, the seam of its closed jaws a dark zigzag running from the top of its front down each side to a
# small black eye with a white four-pointed sparkle near the back of the head. A small low orange body (not white:
# the pictures overrule the brief), a pale grey-white band with a scalloped upper edge round its lower rim, four short
# stubby legs splayed out at the corners.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from mathutils import Euler
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, tri, eye_look, args

ORANGE, BAND, SEAM, INK = "#E9824A", "#E2E2E6", "#5A2A18", "#141014"

# The head: an egg tilted up toward the front, its length along HA
HC = np.array([0.0, -0.075, 0.215])
HR = np.array([0.118, 0.148, 0.118])          # side, along, up (in the head's own frame)
HPITCH = 38                                  # degrees the long axis is raised toward the front
HROT = (-HPITCH, 0, 0)                       # Blender's turn: -x tips the front (-Y) up
HM = np.array(Euler([math.radians(a) for a in HROT]).to_matrix())
HA = HM @ np.array([0, -1.0, 0])             # forward along the egg
HU = HM @ np.array([0, 0, 1.0])              # the egg's own up


def on_head(d):
    """The head's surface point along a direction d (in the head's frame: x side, y back, z up) and its normal."""
    d = np.array(d, float); d /= np.linalg.norm(d)
    p = d / math.sqrt(((d / HR) ** 2).sum())
    n = p / HR ** 2
    return HC + HM @ p, unit(*(HM @ n))


def head_frame(P):
    """Points in the head's frame: along (forward), up and side, each in metres."""
    q = P - HC
    return q @ HA, q @ HU, q[:, 0]


def trapinch(k):
    # --- the body: small and low, a short dome over the legs ----------------------------------------------------------
    k.add("body", ell((0, 0.045, 0.072), (0.078, 0.088, 0.06)), ORANGE, "body")
    k.add("hump", ell((0, 0.015, 0.11), (0.056, 0.055, 0.04)), ORANGE, "body")
    # The neck: short and thick, from the body's front up under the head
    k.add("neck", tube([(0, 0.01, 0.1), (0, -0.015, 0.135), (0, -0.03, 0.16)], [0.042, 0.04, 0.045]), ORANGE, "head")

    # The band round the body's lower rim: pale, its top edge scalloped (four lobes a side)
    def band(P):
        ang = np.arctan2(P[:, 0], P[:, 1] - 0.045)
        top = 0.058 + 0.009 * np.abs(np.sin(ang * 3.0))
        return inside(P[:, 2] - top, 0.0018) * inside(0.03 - P[:, 2], 0.002) * \
            inside(np.abs(P[:, 1] - 0.045) - 0.11, 0.004) * inside(np.abs(P[:, 0]) - 0.1, 0.004)
    k.paint(band, BAND, on=["body"])

    # --- the legs: four stubby pegs splayed out at the corners, the front pair forward ---------------------------------
    for name, bone, hip, foot in (("leg", "leg{s}", (0.05, 0.005, 0.07), (0.084, -0.022, 0.016)),
                                  ("hind", "hind{s}", (0.05, 0.095, 0.07), (0.084, 0.118, 0.016))):
        hip, foot = np.array(hip), np.array(foot)
        knee = hip * 0.45 + foot * 0.55 + (0.012, 0, 0.014)
        k.add(name, tube([hip, knee, foot], [0.033, 0.026, 0.02]), ORANGE, bone, mirror=True)
        k.add(name + "toe", ell(foot + (0.003, 0, -0.002), (0.022, 0.024, 0.016)), ORANGE, bone, mirror=True)

    # --- the head -----------------------------------------------------------------------------------------------------
    k.add("head", ell(HC, HR, rot=HROT), ORANGE, "head")

    # The seam of the jaws: a dark zigzag from the top of the head's front down each side to just before the eye
    def seam(P):
        t, h, x = head_frame(P)
        s = t + (HR[0] - np.abs(x)) * 0.8                 # a rough length along the surface, round the front too
        line = 0.06 + 0.28 * t                   # rises toward the front
        zig = 0.019 * tri(s / 0.066)
        d = np.abs(h - line - zig) - 0.0042
        return inside(d, 0.0012) * smoothstep(-0.035, -0.02, t)
    k.paint(seam, SEAM, on=["head"])


# --- the eyes: small black ovals near the back of the head with a white four-pointed sparkle; bold for the game ------
EYE_D = (0.74, -0.1, 0.03)                       # in the head's frame: out, a little back, level
EYE_HALF = (0.023, 0.029)


def eye(u, v):
    c = np.tile(srgb(INK), (len(u), 1))
    star = np.sqrt(np.abs(u) / 0.5) + np.sqrt(np.abs(v + 0.02) / 0.62)
    c[star < 1.0] = 1.0
    return c


def lay_eyes(k, state):
    look = eye_look(state, eye, srgb(ORANGE), srgb(INK), line=0.17)
    for s in (1, -1):
        p, n = on_head((EYE_D[0] * s, EYE_D[1], EYE_D[2]))
        col = look if s == 1 else (lambda a, b, f=look: f(-a, b))
        k.decal(p, n, EYE_HALF, col, eps=0.003, rings=20, segs=56)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="shell")
    trapinch(k)
    k.build(voxel=0.0034, smooth=4, band=0.005)
    k.eye_states(lambda st: lay_eyes(k, st))
    k.export(out)
