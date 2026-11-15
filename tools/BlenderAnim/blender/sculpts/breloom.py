# Breloom sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/breloom.py -- <the export's Breloom.glb> <out.glb>
#
# Shroomish grown up (shroomish.py's colours' way, its lobe for the frill, its strokes): from the references, an
# upright kangaroo of a fighter. A green mushroom cap with a cream gilled underside sits tilted on a tall cream head
# that narrows to a pointed beak of a chin; large black eyes with a white glint under the cap's rim; a round red seed
# with a dark heart on the cap's side. A cream frill of rounded lobes round the neck; a round green pear of a body;
# short arms hidden under the frill, each ending in a red claw pointing down; fat green haunches, the lower legs
# thinner and going down to feet of two long red claws each. A cream tail out of the back curving up behind it, a
# cluster of four green balls at its tip. (The task's text named a red cap and a beaded tail: the pictures have a
# green cap with red seeds, and a plain cream tail with balls only at its tip.)
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, inside, unit, eye_look, args, sibling

sh = sibling("shroomish")
GREEN, CREAM, RED, SEED = "#78AE5A", "#EEE3BE", "#E2546A", "#5A1E2A"
GILL, INK = "#CFC4A6", sh.INK

H = np.array([0.0, -0.032, 0.462])          # the head's middle (its bone is at 0, -0.02, 0.42)
HR = np.array([0.084, 0.076, 0.08])
C = np.array([0.0, -0.01, 0.54])        # the cap's middle
CR = np.array([0.168, 0.158, 0.098])
CAP_TURN = (7, -8, 0)
VOXEL = 0.0055


def claw(k, name, base, d, length, width, bone, color=RED):
    """A red claw: a flat-faced wedge from base along d, thick at its root."""
    base, d = np.array(base, float), unit(*d)
    a = np.cross(d, (0, 0, 1.0))
    if np.linalg.norm(a) < 1e-3:
        a = np.array([1.0, 0, 0])
    a /= np.linalg.norm(a)
    b = np.cross(a, d)
    k.add(name, hull([base + a * width + b * width * 0.8, base - a * width + b * width * 0.8,
                      base + a * width - b * width * 0.7, base - a * width - b * width * 0.7,
                      base - d * width, base + d * length - b * width * 0.2]), color, bone, fuse=False,
          surface="shell")


def cap_point(d):
    """A point on the cap's top (its turn included) along d from its middle, and the normal there."""
    p, n = sh.on_ell((0, 0, 0), CR, d)
    from mathutils import Euler
    m = np.array(Euler([math.radians(a) for a in CAP_TURN]).to_matrix())
    return C + m @ p, m @ n


def breloom(src, out):
    k = Kit(src, coat="fur")

    # ---- the body: a round green pear, fullest low
    def pear(V):
        V = V.copy()
        s = 1 + 0.1 * np.clip(-V[:, 2], 0, 1) - 0.1 * np.clip(V[:, 2], 0, 1)
        V[:, 0] *= s; V[:, 1] *= s
        return V
    k.add("body", ell((0, 0.01, 0.262), (0.108, 0.098, 0.108), deform=pear), GREEN, "body")

    # ---- the legs: fat haunches fused into the body's sides, the lower legs going down and a little back
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        bone = f"leg{side}"
        k.add(f"thigh{side}", ell(np.array((0.078, 0.0, 0.2)) * m, (0.064, 0.074, 0.08), rot=(10, 0, 0)), GREEN, bone)
        k.add(f"shin{side}", tube(np.array([(0.088, 0.02, 0.16), (0.094, 0.045, 0.085), (0.094, 0.03, 0.03)]) * m,
                                  [0.038, 0.028, 0.024]), GREEN, bone)
        k.add(f"foot{side}", ell(np.array((0.094, 0.0, 0.022)) * m, (0.03, 0.045, 0.022)), GREEN, bone)
        for j, dx in enumerate((-0.016, 0.016)):
            claw(k, f"toe{side}{j}", np.array((0.094 + dx, -0.03, 0.02)) * m, (0.25 * s * np.sign(dx), -1, -0.18),
                 0.062, 0.015, bone)
        claw(k, f"heel{side}", np.array((0.094, 0.03, 0.022)) * m, (0.1 * s, 1, -0.3), 0.03, 0.015, bone)

    # ---- the arms: short, forward and down under the frill, a red claw each pointing down
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"arm{side}", tube(np.array([(0.06, -0.02, 0.345), (0.098, -0.05, 0.31), (0.108, -0.065, 0.29)]) * m,
                                 [0.034, 0.03, 0.028]), GREEN, f"arm{side}")
        claw(k, f"hand{side}", np.array((0.11, -0.07, 0.29)) * m, (0.15 * s, -0.45, -1), 0.068, 0.022, f"arm{side}")

    # ---- the tail: cream, from the tail bone back and out to +X, curving up; four green balls at its tip
    TAIL = [(0.0, 0.06, 0.235), (0.045, 0.16, 0.218), (0.11, 0.245, 0.245), (0.165, 0.29, 0.31), (0.185, 0.305, 0.39)]
    k.add("tail", tube(TAIL, [0.042, 0.033, 0.027, 0.024, 0.021]), CREAM, "tail")
    T = np.array(TAIL[-1]) + (0.0, 0.0, 0.03)
    for i, o in enumerate(((0.0, 0.0, 0.03), (0.036, 0.0, -0.006), (-0.034, 0.008, -0.004), (0.0, -0.032, -0.01))):
        k.add(f"tailball{i}", ell(T + np.array(o), (0.03, 0.03, 0.029)), GREEN, "tail")

    # ---- the frill round the neck: cream lobes, out and down, one hanging in front
    k.add("neck", tube([(0, 0.0, 0.33), (0, -0.015, 0.39), (0, -0.025, 0.43)], [0.05, 0.044, 0.042]), CREAM, "head")
    n = 7
    for i in range(n):
        a = 2 * math.pi * i / n
        d = (math.sin(a), -math.cos(a), 0)
        root = np.array([0.04 * d[0], 0.035 * d[1] - 0.005, 0.37])
        sh.lobe(k, f"frill{i}", root, d, 0.095 if i else 0.08, 0.062, 0.026, CREAM, "body", droop=32)

    # ---- the head: tall, cream, narrowing to a pointed chin; the cap's underside filled by it from below
    k.add("head", ell(H, HR), CREAM, "head")
    k.add("chin", tube([(0, -0.045, 0.455), (0, -0.078, 0.41), (0, -0.096, 0.382)], [0.05, 0.028, 0.013]), CREAM,
          "head")

    # ---- the cap: a green dome, flat beneath, tilted; cream gills under it
    def capshape(V):
        V = V.copy()
        V[:, 2] = np.where(V[:, 2] < 0, V[:, 2] * 0.42, V[:, 2])
        return V
    k.add("cap", ell(C, CR, rot=CAP_TURN, deform=capshape), GREEN, "head")
    from mathutils import Euler
    mt = np.array(Euler([math.radians(a) for a in CAP_TURN]).to_matrix())
    def gills(P):
        local = (P - C) @ mt
        return inside(local[:, 2] + 0.006, 0.003)
    k.paint(gills, GILL, on=["cap"])

    # ---- the seeds: a round red seed on each side of the cap, a dark heart
    for i, (d, r) in enumerate((((0.62, -0.62, 0.35), 0.03), ((-0.7, 0.45, 0.4), 0.024))):
        p, nrm = cap_point(d)
        k.add(f"seed{i}", ell(p - nrm * r * 0.15, (r, r, r), sub=3), RED, "head", fuse=False)
        k.add(f"seedheart{i}", ell(p + nrm * r * 0.62, (r * 0.45, r * 0.45, r * 0.45), sub=2), SEED, "head",
              fuse=False)

    k.build(voxel=VOXEL, smooth=4, band=0.004)

    # ---- the eyes: large black ovals with a white glint, under the cap's rim
    CK, CC = srgb(INK), srgb(CREAM)
    def eye(u, v):
        c = np.tile(CK, (len(u), 1))
        c[((u - 0.2) / 0.32) ** 2 + ((v - 0.32) / 0.3) ** 2 < 1] = (1, 1, 1)
        return c
    ep, en = sh.on_ell(H, HR, (0.48, -0.86, 0.08))
    def eyes(state):
        k.decal(ep, en, (0.017, 0.024), eye_look(state, eye, CC, CK, line=0.2), eps=0.004, rings=20, segs=56,
                mirror=True)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    breloom(*args())
