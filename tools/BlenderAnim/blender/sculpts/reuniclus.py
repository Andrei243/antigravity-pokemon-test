# Reuniclus sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown from
# Solosis's and Duosion's scripts (sibling: the jelly's colours, the jelly fused round the creature with the crisp line
# where the creature stands out of it, the dark oval eyes, the seam down the head, the glint):
#   blender --background --factory-startup --python sculpts/reuniclus.py -- <the export's Reuniclus.glb> <out.glb>
#
# From the references: a round body of darker green jelly with two round ears rising from its top, a little apart and
# leaning out, each with a pale yellow-green triangle in its front; in the middle of the body a small pale green
# creature sitting: a round head with the seam down its front, two dark green oval eyes and a wide open smile with a
# pink tongue (the line's red diamond is gone), tiny arms on its chest and little feet under it; two great jelly arms
# from the body's sides ending in big round hands with stubby fingers, each hand holding a red ball in its palm, with
# a tan oval and a pale green dot along each arm. The pictures hold the arms out level; here they are carried forward
# and a little down, so the model is not twice as wide as it is tall.
# The skeleton: root (0, 0, 0), body (0, 0, 0.178), armL (-0.1, 0, 0.178), armR (0.1, 0, 0.178),
# head (0, -0.04, 0.178). It hovers.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, polygon, sibling, args

so = sibling("solosis")
du = sibling("duosion")
EARIN, RED, REDGLINT, TAN, DOT = "#DCEBA6", "#D9505A", "#F6C8C8", "#D7B477", "#C2DE8E"
MOUTH_INK, TONGUE = "#6E2A36", "#E9888C"

# --- the frame ------------------------------------------------------------------------------------------------------
JC, JR = np.array([0.0, 0.012, 0.165]), np.array([0.122, 0.104, 0.135])     # the jelly body
HC, HR = np.array([0.0, -0.074, 0.207]), np.array([0.064, 0.056, 0.056])     # the creature's head
TC, TR = np.array([0.0, -0.078, 0.132]), np.array([0.05, 0.046, 0.05])       # its body
EYE_X, EYE_Z, EYE_HALF = 0.03, 0.218, (0.0115, 0.019)
MOUTH_Z, MOUTH_HALF = 0.19, (0.026, 0.02)
HAND, HAND_R, HAND_TURN = np.array([0.24, -0.105, 0.152]), np.array([0.078, 0.052, 0.074]), -32
VOXEL = 0.0036


def turn(v, deg):
    a = math.radians(deg)
    return np.array([v[0] * math.cos(a) - v[1] * math.sin(a), v[0] * math.sin(a) + v[1] * math.cos(a), v[2]])


def ear(k):
    """The two round ears rising from the body's top and leaning out, the pale triangle in each one's front."""
    k.add("ear", tube([(0.045, 0.006, 0.255), (0.06, 0.0, 0.3), (0.074, -0.004, 0.322)], [0.045, 0.04, 0.032]),
          so.JELLY, "body", mirror=True)
    tri = polygon([(0.0, 1.0), (0.85, -0.75), (-0.85, -0.75)])
    for s in (1, -1):
        c = np.array([0.064 * s, -0.04, 0.305])
        k.decal(c, unit(0.25 * s, -1, 0.05), (0.02, 0.026), EARIN, up=unit(0.3 * s, 0, 1), eps=0.0022,
                outline=tri, rings=10, segs=72)


def arm(k):
    """A great jelly arm carried forward and a little down, its round hand with four stubby fingers round its edge."""
    k.add("arm", tube([(0.1, -0.008, 0.172), (0.15, -0.035, 0.166), (0.19, -0.07, 0.158), (0.215, -0.09, 0.154)],
                      [0.056, 0.052, 0.052, 0.056]), so.JELLY, "arm{s}", mirror=True)
    k.add("hand", ell(HAND, HAND_R, rot=(0, 0, HAND_TURN)), so.JELLY, "arm{s}", mirror=True)
    for off, r in (((-0.026, 0, 0.076), 0.03), ((0.04, 0, 0.066), 0.03), ((0.078, 0, 0.004), 0.029),
                   ((0.054, 0, -0.06), 0.027)):
        k.add("finger", ell(HAND + turn(off, HAND_TURN), (r, r * 0.9, r)), so.JELLY, "arm{s}", mirror=True)
    # the red ball held in the palm, standing out of the hand's front, and the spots along the arm
    n = turn((0, -1, 0), HAND_TURN)
    ball = HAND + n * (HAND_R[1] * 0.75)
    k.add("ball", ell(ball, (0.038, 0.034, 0.038), sub=3), RED, "arm{s}", fuse=False, mirror=True, surface="skin")
    k.add("ballglint", ell(ball + n * 0.026 + np.array([-0.012, 0, 0.013]), (0.009, 0.005, 0.007), sub=2), REDGLINT,
          "arm{s}", fuse=False, mirror=True, surface="skin")
    k.add("spottan", ell((0.152, -0.1, 0.163), (0.024, 0.009, 0.016), rot=(0, 0, -40), sub=3), TAN, "arm{s}",
          fuse=False, mirror=True)
    k.add("spotdot", ell((0.112, -0.074, 0.168), (0.014, 0.007, 0.01), rot=(0, 0, -28), sub=2), DOT, "arm{s}",
          fuse=False, mirror=True)


def creature(k):
    """The small pale green creature sitting in the middle of the jelly: head, body, arms on its chest, feet."""
    k.add("cellhead", ell(HC, HR, sub=5), so.CELL, "head", surface="fur")
    k.add("cellbody", ell(TC, TR, sub=4), so.CELL, "body", surface="fur")
    k.add("cellarm", ell((0.032, -0.118, 0.146), (0.014, 0.014, 0.018)), so.CELL, "body", mirror=True, surface="fur")
    k.add("cellfoot", ell((0.026, -0.11, 0.093), (0.02, 0.022, 0.016)), so.CELL, "body", mirror=True, surface="fur")


def seam(P):
    """The seam down the head's front from its crown to just above the eyes' line."""
    z = P[:, 2]
    t = np.clip((z - (EYE_Z - 0.006)) / (HC[2] + HR[2] - EYE_Z + 0.006), 0, 1)
    x = 0.004 * np.sin(math.pi * t)
    band = inside(np.abs(P[:, 0] - x) - 0.0026, 0.0014)
    return band * (z > EYE_Z - 0.006) * (P[:, 1] < HC[1] + 0.02)


MOUTH = ([(u, 0.75 - 0.25 * u * u) for u in np.linspace(1, -1, 13)]
         + [(math.cos(a), 0.45 + 1.3 * math.sin(a) * (0.9 - 0.1 * math.cos(a) ** 2)) for a in np.linspace(math.pi, 2 * math.pi, 25)[1:-1]])


def mouth(k):
    """The wide open smile: a dark shape, flat on top and round below, the pink tongue in its bottom."""
    c = so.on_ell(HC, HR, (0, -1, (MOUTH_Z - HC[2]) / HR[2]))
    n = so.normal_at(HC, HR, c)
    k.decal(c, n, MOUTH_HALF, MOUTH_INK, eps=0.0022, outline=polygon(MOUTH), rings=10, segs=96)
    k.decal(c + np.array([0, 0, -0.008]), n, (0.016, 0.009), TONGUE, eps=0.0032, rings=8, segs=48)


def reuniclus(src, out):
    k = Kit(src, coat="shell")
    k.add("jelly", ell(JC, JR, sub=5), so.JELLY, "body")
    ear(k)
    arm(k)
    creature(k)
    so.crisp_edge(k, JC, JR, np.array([0.0, -0.09, 0.16]), np.array([0.085, 0.07, 0.12]))
    k.paint(seam, du.SEAM, on=["cellhead"])
    mouth(k)
    so.glint(k, JC, JR, (-0.55, -0.45, 0.7), (0.026, 0.015), up=unit(0.55, 0, 0.84))
    k.build(voxel=VOXEL, smooth=3, band=0.004)

    def eyes(state):
        for s in (1, -1):
            p = so.on_ell(HC, HR, (EYE_X * s / HR[0], -1.0, (EYE_Z - HC[2]) / HR[2]))
            n = so.normal_at(HC, HR, p)
            n = unit(n[0] * 0.7, n[1], n[2] * 0.7)
            so.eye_patch(k, p, n, EYE_HALF, state)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    reuniclus(*args())
