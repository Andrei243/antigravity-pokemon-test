# Gloom sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/gloom.py -- <the export's Gloom.glb> <out.glb>
#
# Grown from Oddish's script (sibling("oddish")): the same blue bulb, the same way of making leaves and eyes. From the
# references: the bulb grown into a pear, wider below, on two short legs whose feet turn out and forward; two stubby
# arms hanging at its sides; drowsy eyes that are always shut, a line each curving down at the outer end; a wide sour
# mouth of lavender lips with a dark line between them drooping at the corners, a drop of nectar hanging from its +X
# corner. On its head a heavy red-brown flower of four round lobes about a smaller one on top, spotted pale; from
# under it four long orange leaves sweep out, the upper pair wide (the -X one falling and curling in, the +X one
# rising to its point), a shorter pair hanging down behind the arms.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from mathutils import Euler
from sculptkit import Kit, ell, tube, srgb, unit, eye_look, args, sibling

od = sibling("oddish")

BLUE = od.BLUE
PETAL, SPOT = "#B0614A", "#F2CEC0"
ORANGE, ORANGE_VEIN = "#EE7C34", "#F7A862"
LIP, LIP_DARK, LIP_LINE = srgb("#B2A2E2"), srgb("#7E68B8"), srgb("#3E2450")
DROOL = "#F6F6F2"

B = np.array([0.0, 0.0, 0.205])          # the pear's middle
BR = np.array([0.155, 0.14, 0.15])
FC = np.array([0.0, 0.006, 0.432])       # the flower's middle
TILT = np.array(Euler((math.radians(12), 0, 0)).to_matrix())   # the flower leans forward
BALL_R = 0.088


def tilted(p):
    """A point of the flower turned with it about its middle."""
    return FC + TILT @ (np.array(p, float) - FC)


def gloom_body(k):
    k.add("bulb", ell(B, BR, deform=od.bulb(flat=0.85, slim=0.2)), BLUE, "body")
    # short legs, the feet turning out and forward
    k.add("leg", od.tube([(0.06, 0.0, 0.12), (0.076, -0.01, 0.05), (0.118, -0.04, 0.026), (0.156, -0.062, 0.026)],
                         [0.034, 0.03, 0.027, 0.027]), BLUE, "leg{s}", mirror=True)
    # stubby arms hanging at the sides
    k.add("arm", od.tube([(0.12, -0.02, 0.225), (0.168, -0.034, 0.185), (0.188, -0.042, 0.145)],
                         [0.034, 0.031, 0.031]), BLUE, "arm{s}", mirror=True)


def gloom_flower(k):
    """Four round lobes about a smaller one on top, fused into one heavy bloom, each spotted pale."""
    lobes = []
    for i, a in enumerate((0, 90, 180, 270)):
        t = math.radians(a)
        r = BALL_R * (1.06 if a == 0 else 1.0)
        c = tilted(FC + np.array([0.088 * math.sin(t), -0.088 * math.cos(t), -0.004 if a == 0 else 0.0]))
        lobes.append((c, r, t))
        k.add(f"lobe{i}", ell(c, (r, r, r * 0.95)), PETAL, "flower", surface="leaf")
    top = tilted(FC + np.array([0.0, 0.004, 0.066]))
    k.add("lobeTop", ell(top, (0.066, 0.066, 0.06)), PETAL, "flower", surface="leaf")
    # pale spots on the lobes' upper outer faces, of a few sizes
    spots = [(0.0, 0.5, 0.019), (0.3, 0.24, 0.014), (-0.28, 0.42, 0.013), (0.08, 0.02, 0.011)]
    for c, r, t in lobes:
        out = np.array([math.sin(t), -math.cos(t), 0.0])
        side = np.array([math.cos(t), math.sin(t), 0.0])
        for j, (s, up, size) in enumerate(spots):
            d = TILT @ unit(*(out * 0.75 + side * s + np.array([0, 0, up])))
            p, n = od.on_ell(c, (r, r, r * 0.95), d)
            k.decal(p, n, (size, size * 0.85), SPOT, eps=0.003, rings=6, segs=24)
    for j, d in enumerate(((0.4, -0.6, 0.7),)):
        p, n = od.on_ell(top, (0.066, 0.066, 0.06), TILT @ unit(*d))
        k.decal(p, n, (0.014, 0.012), SPOT, eps=0.003, rings=6, segs=24)


def gloom_leaves(k):
    """The four orange leaves from under the flower."""
    front = np.array([0.0, -1.0, 0.3])
    # the upper pair: -X falls and curls in at its end, +X rises to its point
    od.add_leaf(k, "leafL", (-0.08, 0.0, 0.372), (-1, 0.15, 0.12), front, 0.235, 0.044, ORANGE, ORANGE_VEIN, "flower",
                droop=0.55, fold=0.25, curl=0.0,
                tipcurl=lambda v: (0.0 * v, 0.16 * np.clip((v - 0.6) / 0.4, 0, 1) ** 2))
    od.add_leaf(k, "leafR", (0.08, 0.0, 0.372), (1, 0.15, 0.1), front, 0.235, 0.044, ORANGE, ORANGE_VEIN, "flower",
                droop=-0.3, fold=0.25, curl=0.0)
    # the lower pair hangs down behind the arms
    for s in (1, -1):
        od.add_leaf(k, f"leafLow{'R' if s > 0 else 'L'}", (0.09 * s, 0.02, 0.35), (0.55 * s, 0.35, -0.75),
                    (0.7 * s, -0.7, 0.2), 0.19, 0.036, ORANGE, ORANGE_VEIN, "flower", droop=0.08, fold=0.25)


def sour_mouth(k):
    """Wide lavender lips with a dark line drooping at the corners, and a drop of nectar from the +X corner."""
    p, n = od.on_ell(B, BR, (0, -1, -0.13))
    def look(u, v):
        c = np.tile(LIP, (len(u), 1))
        r = np.sqrt(u * u + v * v)
        c[r > 0.8] = LIP_DARK
        line = -0.2 + 0.36 * (1 - u * u)
        c[(np.abs(v - line) < 0.2) & (np.abs(u) < 0.8)] = LIP_LINE
        return c
    k.decal(p, n, (0.062, 0.03), look, eps=0.004, rings=22, segs=64)
    corner, cn = od.on_ell(B, BR, (0.3, -0.94, -0.16))
    start = corner + cn * 0.004
    pts = [start, start + np.array([0.002, -0.01, -0.02]), start + np.array([0.003, -0.016, -0.04])]
    k.add("drool", tube(pts, [0.007, 0.0065, 0.007]), DROOL, "body", fuse=False, surface="skin")
    k.add("drop", ell(pts[-1] + np.array([0, -0.002, -0.006]), (0.011, 0.011, 0.013)), DROOL, "body", fuse=False,
          surface="skin")


INK = od.INK
def sleepy(u, v):
    """Always shut: a line curving down at its outer end, on the blue."""
    c = np.tile(srgb(BLUE), (len(u), 1))
    c[(np.abs(v - (0.16 - 0.36 * u * u - 0.2 * u)) < 0.17) & (np.abs(u) < 0.86)] = INK
    return c


def sleepy_fierce(u, v):
    """Fierce on a shut eye: the same line tilted down toward the inner corner."""
    c = np.tile(srgb(BLUE), (len(u), 1))
    c[(np.abs(v - (0.0 + 0.45 * u)) < 0.17) & (np.abs(u) < 0.86)] = INK
    return c


def gloom(src, out):
    k = Kit(src, coat="fur")
    gloom_body(k)
    gloom_flower(k)
    gloom_leaves(k)
    sour_mouth(k)
    k.build(voxel=0.005, smooth=4, band=0.004)

    p, n = od.on_ell(B, BR, (0.4, -0.86, 0.3))
    def eyes(state):
        look = sleepy_fierce if state == "Fierce" else eye_look(state, sleepy, srgb(BLUE), INK, line=0.16)
        k.decal(p, n, (0.032, 0.022), look, eps=0.005, rings=20, segs=56, mirror=True)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    gloom(*args())
