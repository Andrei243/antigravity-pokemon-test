# Wobbuffet sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/wobbuffet.py -- <the export's Wobbuffet.glb> <out.glb>
#
# The design, from the references: a tall soft blue blob with no head of its own, a dome on top, a little narrower at
# the waist and spreading at its base into round feet (two big ones in front, two smaller ones behind); a huge open
# mouth over most of its upper front, dark inside, its upper edge cut in a zigzag, a pink tongue in its bottom; above it
# two eyes always shut tight, each a bold chevron pointing in toward the middle (the zigzag of the task's text is the
# mouth's edge, not the eyes); a long flat arm on each side (raised over the head in both pictures, hanging down and a
# little forward here); a black tail out of the lower back, out to the +X side and up, ending in a round lobe with two
# white eye spots with black pupils.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, unit, eye_look, polygon, args

BLUE, BLACK, MOUTH, TONGUE = "#4DB2E2", "#2C2C36", "#6C1C2A", "#E8827E"
INK, SPOT = "#18202E", "#F6F6F4"

# --- the frame ------------------------------------------------------------------------------------------------------
B, BR = np.array([0.0, 0.0, 0.435]), np.array([0.2, 0.165, 0.39])
MOUTH_C, MOUTH_N, MOUTH_HALF = np.array([0.0, -0.16, 0.545]), unit(0, -1, 0.06), (0.135, 0.16)
EYE_X, EYE_Z, EYE_HALF = 0.078, 0.735, (0.044, 0.038)
EYE_N = unit(0.38, -0.92, 0.22)
TAIL = [(0.0, 0.1, 0.13), (0.08, 0.2, 0.1), (0.17, 0.255, 0.13), (0.235, 0.27, 0.19)]
BULB_C, BULB_R = np.array([0.258, 0.265, 0.245]), np.array([0.074, 0.068, 0.084])
SPOT_N = unit(0.78, -0.5, 0.4)          # facing the front three-quarter
VOXEL = 0.007


def blob(V):
    """The body: a dome on top, a little narrower at the waist, full again toward the base."""
    V = V.copy()
    s = 1 - 0.08 * np.exp(-((V[:, 2] + 0.25) / 0.3) ** 2) + 0.04 * np.clip(V[:, 2], 0, 1) + 0.12 * np.clip(-V[:, 2] - 0.45, 0, 1)
    V[:, 0] *= s; V[:, 1] *= s
    return V


def on_ell(c, r, d):
    d = unit(*d)
    return np.array(c, float) + d / math.sqrt(((d / np.array(r)) ** 2).sum())


def mouth(k):
    """The open mouth: a dark shape with a zigzag top edge, and the pink tongue in its bottom as a patch over it."""
    top = []
    teeth = 6
    for i in range(2 * teeth + 1):
        u = 1 - 2 * i / (2 * teeth)
        top.append((u * 0.96, 0.62 if i % 2 == 0 else 0.4))
    arc = [(math.cos(a), 0.3 + 1.3 * math.sin(a)) for a in np.linspace(math.pi, 2 * math.pi, 25)]
    outline = [(1.0, 0.5)] + top + [(-1.0, 0.5)] + arc
    k.decal(MOUTH_C, MOUTH_N, MOUTH_HALF, MOUTH, eps=0.003, outline=polygon(outline), rings=12, segs=120)
    k.decal(MOUTH_C + np.array([0, 0.0, -0.088]), MOUTH_N, (0.084, 0.058), TONGUE, eps=0.0045, rings=10, segs=60)


def chevron(u, v):
    """The shut eye: a bold chevron, its point at the inner corner (u < 0), its arms opening outward."""
    # the stroke's edge eased between the patch's rings, or it steps from ring to ring
    arm = np.abs(np.abs(v) - 0.66 * (u + 0.72) / 1.48) / 1.15
    ends = np.maximum(-0.84 - u, u - 0.8)
    d = np.maximum(arm - 0.2, ends)
    m = np.clip(0.5 - d / 0.07, 0, 1)[:, None]
    return srgb(BLUE)[None] * (1 - m) + srgb(INK)[None] * m


def look(state):
    if state == "Open":
        return chevron
    if state != "Fierce":
        return eye_look(state, chevron, srgb(BLUE), srgb(INK), line=0.2)
    def fierce(u, v):
        c = np.tile(srgb(BLUE), (len(u), 1))
        c[(np.abs(v - (0.5 * u + 0.05)) < 0.2) & (np.abs(u) < 0.86)] = srgb(INK)
        return c
    return fierce


def tail(k):
    k.add("tail", tube(TAIL, [0.046, 0.046, 0.05, 0.055]), BLACK, "tail")
    k.add("tailbulb", ell(BULB_C, BULB_R), BLACK, "tail")
    # two white eye spots side by side on the lobe's face, turned out to +X, forward and up
    for ang in (24, -24):
        a = math.radians(ang)
        n = np.array([SPOT_N[0] * math.cos(a) - SPOT_N[1] * math.sin(a),
                      SPOT_N[0] * math.sin(a) + SPOT_N[1] * math.cos(a), SPOT_N[2]])
        p = on_ell(BULB_C, BULB_R, n)
        def spot(u, v):
            c = np.tile(srgb(SPOT), (len(u), 1))
            c[u * u + v * v < 0.2] = srgb(INK)
            return c
        k.decal(p, n, (0.025, 0.025), spot, eps=0.004, rings=12, segs=40)


def wobbuffet(src, out):
    k = Kit(src, coat="scales")
    k.add("body", ell(B, BR, deform=blob), BLUE, "body")
    # the feet: two big round lobes in front, two smaller behind
    k.add("foot", ell((0.1, -0.075, 0.055), (0.095, 0.1, 0.06)), BLUE, "leg{s}", mirror=True)
    k.add("heel", ell((0.09, 0.07, 0.05), (0.08, 0.085, 0.052)), BLUE, "leg{s}", mirror=True)
    # the arms: long flat lobes from high on the sides, hanging down and a little forward, clear of the body
    n = unit(0.8, -0.6, 0)
    k.add("arm", blade([(0.168, -0.02, 0.575), (0.232, -0.05, 0.47), (0.256, -0.08, 0.36), (0.25, -0.1, 0.28)],
                       [0.05, 0.058, 0.058, 0.05], n, 0.55), BLUE, "arm{s}", mirror=True)
    tail(k)
    mouth(k)
    k.build(voxel=VOXEL, band=0.004)

    def eyes(state):
        k.decal((EYE_X, -0.15, EYE_Z), EYE_N, EYE_HALF, look(state), eps=0.004, rings=20, segs=56, mirror=True)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    wobbuffet(*args())
