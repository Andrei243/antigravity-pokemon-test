# Runerigus sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/runerigus.py -- <the export's Runerigus.glb> <out.glb>
#
# Cofagrigus's Galarian line grown old: a shadow bound to a stone tablet. From the references, what each shape IS:
# - the head: a thick hexagonal slab, top left, its front painted with a red mask round ONE purple eye (a slit pupil in
#   a dark almond socket) and a row of red fangs under it;
# - the body: thick slabs of the same stone bent into a jagged S under the head (a column down the right, a bar back to
#   the left, a block down and a foot turning right), red spirals and arcs painted on their fronts;
# - the arms: two stone blocks, one from the head's left reaching out and down, one up on the right (an inverted L
#   joined to the column by a black tendril), each with four black shadow fingers hanging from its end;
# - the tail: a small rounded stone slab with a red spiral at the lower right, on a black rope that trails from under
#   the body;
# - the shadow: black, showing in the gaps (a dark mouth with white fangs under the head slab), the tendril, the
#   fingers and the rope.
# The slabs are hard-edged: a darker stone core and a paler front plate standing just proud of it; the red runes are
# loose strips standing proud of the plates (patches would not land on loose pieces). The eye lies on its dark socket,
# the one thin fused plate in the front. It hovers: nothing need reach the ground.
# The skeleton: body (0, 0, 0.463), armL (-0.15, 0.03, 0.643), armR (0.11, 0.03, 0.303), tail (-0.14, 0.02, 0.263).
# There is no head bone: the head slab rides the body. The right arm block, up at the right as the pictures have it,
# is on the end of a long shadow arm rising from armR's joint; the tail slab and rope are on tail.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, hull, srgb, unit, eye_look, polygon, outward, args

STONE, RIM, RED = "#BDB5B1", "#6E696B", "#D8333A"
SHADOW, SOCKET, IRIS, PUPIL, TEETH = "#3A3A40", "#26222B", "#A879D8", "#2A1E36", "#ECE8E2"
THICK = 0.085                 # a slab's depth


def flat(y):
    return lambda x: y


def tilt(x0, k):
    """A slab whose outer end comes forward: y = -k * (|x| - x0) past x0."""
    return lambda x: -k * max(0.0, abs(x) - x0)


def slab_piece(k, name, outline, bone, yoff=flat(0.0), thick=THICK):
    """A hard-edged stone slab: a dark core with a paler front plate inset and standing just proud of it."""
    P = np.array(outline, float)
    pts = []
    for x, z in P:
        y = yoff(x)
        pts += [(x, y - thick / 2, z), (x, y + thick / 2, z)]
    k.add(name, hull(pts), RIM, bone, fuse=False, surface="shell")
    m = P.mean(axis=0)
    Q = m + (P - m) * (1 - 0.016 / np.maximum(np.linalg.norm(P - m, axis=1), 1e-6))[:, None]
    front = []
    for x, z in Q:
        y = yoff(x) - thick / 2
        front += [(x, y - 0.005, z), (x, y + 0.012, z)]
    k.add(name + "face", hull(front), STONE, bone, fuse=False, surface="shell")


def rune(k, name, pts, bone, yoff=flat(0.0), r=0.0085, thick=THICK):
    """A red line painted on a slab's front: a flat loose strip standing proud of the plate."""
    P = [(x, yoff(x) - thick / 2 - 0.009, z) for x, z in pts]
    k.add(name, outward(blade(P, [r] * len(P), (0, 1, 0), 0.4)), RED, bone, fuse=False, surface="shell")


def spiral(cx, cz, r0, turns, n=40, start=0.0):
    out_ = []
    for i in range(n):
        t = i / (n - 1)
        a = start + 2 * math.pi * turns * t
        rr = r0 * (0.25 + 0.75 * t)
        out_.append((cx + rr * math.cos(a), cz + rr * math.sin(a)))
    return out_


def runerigus(src, out_path):
    k = Kit(src, coat="shell")

    # --- the S of stone ------------------------------------------------------------------------------------------------
    HEAD = [(-0.215, 0.66), (-0.15, 0.765), (0.045, 0.77), (0.105, 0.69), (0.065, 0.565), (-0.155, 0.555)]
    slab_piece(k, "head", HEAD, "body")
    COLUMN = [(0.005, 0.59), (0.105, 0.665), (0.145, 0.43), (0.04, 0.405)]
    slab_piece(k, "column", COLUMN, "body", flat(0.012), 0.08)
    BAR = [(-0.125, 0.405), (0.11, 0.445), (0.085, 0.335), (-0.105, 0.315)]
    slab_piece(k, "bar", BAR, "body", flat(-0.004), 0.082)
    DOWN = [(-0.145, 0.34), (-0.03, 0.335), (-0.005, 0.175), (-0.15, 0.2)]
    slab_piece(k, "down", DOWN, "body", flat(0.006), 0.08)
    FOOT = [(-0.15, 0.215), (0.0, 0.19), (0.085, 0.115), (-0.07, 0.085)]
    slab_piece(k, "foot", FOOT, "body", flat(-0.006), 0.078)

    # the shadow behind and between the slabs: one dark core along the S, a mouth under the head with white fangs
    k.add("core", tube([(-0.07, 0.0, 0.66), (0.06, 0.0, 0.6), (0.09, 0.0, 0.47), (-0.02, 0.0, 0.37),
                        (-0.08, 0.0, 0.26), (0.0, 0.0, 0.15)], [0.03, 0.03, 0.03, 0.03, 0.03, 0.026]),
          SHADOW, "body", surface="skin")
    k.add("mouth", ell((-0.06, -0.01, 0.53), (0.075, 0.04, 0.038)), SHADOW, "body", surface="skin")
    for i, x in enumerate((-0.11, -0.075, -0.04, -0.005)):
        k.add(f"fang{i}", hull([(x - 0.011, -0.062, 0.556), (x + 0.011, -0.062, 0.556), (x, -0.06, 0.522),
                                (x - 0.011, -0.04, 0.556), (x + 0.011, -0.04, 0.556), (x, -0.042, 0.522)]),
              TEETH, "body", fuse=False)

    # --- the arms ------------------------------------------------------------------------------------------------------
    # the left: a block from the head's left out and down, coming forward at its end
    yL = tilt(0.2, 0.55)
    ARM_L = [(-0.2, 0.725), (-0.17, 0.62), (-0.31, 0.53), (-0.345, 0.635)]
    slab_piece(k, "armL", ARM_L, "armL", yL, 0.08)
    # the right: an inverted L up at the right, on the end of a long black shadow arm that rises from armR's joint
    # (0.11, 0.03, 0.303) at the column's foot up its right side: so the block turns about its own arm's root
    yR = tilt(0.17, 0.5)
    ARM_R1 = [(0.155, 0.735), (0.3, 0.72), (0.29, 0.645), (0.16, 0.655)]
    ARM_R2 = [(0.225, 0.725), (0.31, 0.725), (0.325, 0.52), (0.245, 0.53)]
    slab_piece(k, "armRa", ARM_R1, "armR", yR, 0.075)
    slab_piece(k, "armRb", ARM_R2, "armR", yR, 0.075)
    k.add("tendril", tube([(0.11, 0.02, 0.31), (0.165, 0.015, 0.39), (0.19, 0.0, 0.5), (0.205, -0.01, 0.6),
                           (0.185, -0.005, 0.67)], [0.02, 0.019, 0.019, 0.019, 0.02]), SHADOW, "armR",
          surface="skin")

    def fingers(name, root, along, bone, s):
        """Four black fingers hanging from a block's end, curled a little in and forward."""
        root = np.array(root, float)
        along = np.array(along, float)
        for j in range(4):
            b = root + along * (j - 1.5) * 0.032
            ln = 0.1 + 0.02 * (1 - abs(j - 1.5) / 1.5)
            pts = [b + np.array([0, 0, 0.02]), b + np.array([0.004 * s, -0.01, -ln * 0.5]),
                   b + np.array([-0.012 * s, -0.035, -ln])]
            k.add(f"{name}{j}", outward(tube(pts, [0.017, 0.015, 0.012])), SHADOW, bone, fuse=False, surface="skin")
    fingers("fingerL", (-0.33, yL(-0.33), 0.555), unit(0.45, 0.25, 0.85), "armL", -1)
    fingers("fingerR", (0.287, yR(0.287), 0.53), unit(1, -0.5, 0), "armR", 1)

    # --- the tail: a rounded slab with a spiral on a black rope trailing from under the body ---------------------------
    TC = np.array([0.215, 0.19])
    TAIL = [tuple(TC + 0.075 * np.array([math.cos(a), math.sin(a)])) for a in np.radians([90, 145, 205, 265, 330, 30])]
    yT = flat(-0.02)
    slab_piece(k, "tailslab", TAIL, "tail", yT, 0.07)
    k.add("rope", tube([(-0.14, 0.02, 0.24), (-0.2, 0.0, 0.12), (-0.12, -0.02, 0.05), (0.06, -0.03, 0.035),
                        (0.17, -0.03, 0.07), (0.2, -0.02, 0.12)], [0.02, 0.019, 0.019, 0.019, 0.019, 0.02]),
          SHADOW, "tail", surface="skin")

    # --- the red runes ---------------------------------------------------------------------------------------------
    # the mask on the head: a brow arching over the eye and hooking down at both ends, a mouth line of fangs under it
    rune(k, "maskbrow", [(-0.18, 0.62), (-0.175, 0.685), (-0.13, 0.73), (-0.05, 0.742), (0.03, 0.725),
                         (0.065, 0.69), (0.055, 0.655)], "body")
    rune(k, "maskhookL", [(-0.18, 0.625), (-0.155, 0.61), (-0.13, 0.62)], "body")
    rune(k, "maskhookR", [(0.055, 0.655), (0.035, 0.64), (0.01, 0.645)], "body")
    teeth = [(-0.14, 0.63)]
    for i in range(6):
        x = -0.125 + i * 0.026
        teeth += [(x, 0.6), (x + 0.013, 0.625)]
    teeth += [(0.03, 0.6)]
    rune(k, "maskteeth", teeth, "body", r=0.0075)
    # the body: an arc down the column, the bar's line, a spiral on the lower block and the foot
    rune(k, "colarc", [(0.075, 0.6), (0.1, 0.55), (0.11, 0.48), (0.08, 0.44)], "body", flat(0.012), thick=0.08)
    rune(k, "barline", [(-0.1, 0.36), (-0.02, 0.38), (0.06, 0.39), (0.085, 0.37)], "body", flat(-0.004),
         thick=0.082)
    rune(k, "spiral", spiral(-0.075, 0.25, 0.058, 1.35, start=math.radians(100)), "body", flat(0.006), thick=0.08)
    rune(k, "footline", [(-0.085, 0.145), (-0.04, 0.13), (0.04, 0.125), (0.06, 0.135)], "body", flat(-0.006),
         thick=0.078)
    # the arms: a line along each block, hooked at its end
    rune(k, "armLline", [(-0.2, 0.685), (-0.25, 0.65), (-0.3, 0.615), (-0.315, 0.585), (-0.295, 0.575)], "armL",
         yL, thick=0.08)
    rune(k, "armRline", [(0.18, 0.7), (0.24, 0.695), (0.275, 0.68), (0.285, 0.62), (0.29, 0.56)], "armR", yR,
         thick=0.075)
    rune(k, "tailspiral", spiral(TC[0], TC[1], 0.05, 1.3, start=math.radians(70)), "tail", yT, thick=0.07)

    # --- the eye's dark almond socket: the one thin fused plate in the front, just proud of the head's plate ----------
    EC = np.array([-0.055, 0.0, 0.676])
    front = -THICK / 2 - 0.005                          # the head plate's front face
    k.add("socket", ell((EC[0], front + 0.0085, EC[2]), (0.066, 0.01, 0.036), deform=lambda V: V * np.array(
        [1, 1, 1.0]) * (1 - 0.25 * np.abs(V[:, :1]) ** 2)), SOCKET, "body", surface="skin")

    k.build(voxel=0.0055, smooth=4, band=0.005)

    # --- the eye: a purple disc with an upright slit pupil, in the socket; light lines when shut ----------------------
    CI, CP, CS, LINE = srgb(IRIS), srgb(PUPIL), srgb(SOCKET), srgb("#9A8AA8")
    def eye(u, v):
        c = np.tile(CS, (len(u), 1))
        c[(u / 0.62) ** 2 + (v / 0.95) ** 2 < 1] = CI
        c[(u / 0.22) ** 2 + (v / 0.82) ** 2 < 1] = CP
        c[(u - 0.24) ** 2 + (v - 0.3) ** 2 < 0.012] = (1, 1, 1)
        return c
    def eyes(state):
        k.decal((EC[0], front - 0.0015, EC[2]), (0, -1, 0), (0.041, 0.03), eye_look(state, eye, CS, LINE, line=0.16),
                eps=0.002, rings=20, segs=56)
    k.eye_states(eyes)
    k.export(out_path)


if __name__ == "__main__":
    runerigus(*args())
