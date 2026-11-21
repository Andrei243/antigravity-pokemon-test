# Glimmora sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/glimmora.py -- <the export's Glimmora.glb> <out.glb>
#
# The design, from the references: a great closed bud of deep blue crystal petals lying along its length, smooth and
# rounded, the petals overlapping lengthwise and closing at the rear in a slit edged teal, where a lilac lip shows;
# a few pale streaks run along the petals. From its front a long steel-blue cone of crystal points forward, with two
# plain yellow oval eyes side by side on its root. It hovers. (The petals are spread only in its Mega form.)
#
# Grown from Glimmet (sibling("glimmet")): its cone, the way its eyes are laid on it, its blues and lilac.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, srgb, inside, smoothstep, unit, args, sibling

G = sibling("glimmet")

BLUE, BLUE_DK, TEAL = "#1A4CC6", "#1638A0", "#2AA6B4"
STREAK = "#62C6E6"
CONE = "#8AA2BA"
LILAC = G.LILAC

# --- the frame ----------------------------------------------------------------------------------------------------
BUD_C, BUD_R = np.array([0.0, 0.05, 0.255]), np.array([0.17, 0.22, 0.16])
CONE_C, CONE_R, CONE_L = np.array([0.0, -0.155, 0.258]), 0.066, 0.23
VOXEL = 0.0055


def closing(V):
    """A petal's deform of the unit sphere: narrowing toward the front (y -1) where the petals meet the cone."""
    V = V.copy()
    t = np.clip(-V[:, 1], 0, 1)
    k = 1 - 0.55 * t ** 2
    V[:, 0] *= k; V[:, 2] *= k
    return V


def glimmora(src, out):
    k = Kit(src, coat="shell")

    # --- the bud: five long petals round its length over a core -------------------------------------------------------
    k.add("core", ell(BUD_C, BUD_R * 0.9, deform=closing), BLUE, "body", surface="shell")
    for i in range(5):
        a = 2 * math.pi * i / 5 + 0.35
        off = np.array([math.cos(a), 0.0, math.sin(a)])
        k.add(f"petal{i}", ell(BUD_C + off * 0.045 + np.array([0, 0.004 * (i % 2), 0]),
                               (0.13, 0.215 - 0.008 * (i % 2), 0.115), rot=(0, -math.degrees(a), 0),
                               deform=closing), BLUE, "body", surface="shell")
    # the lilac lip in the rear slit
    k.add("lip", ell(BUD_C + np.array([0.0, 0.2, 0.0]), (0.075, 0.03, 0.026)), LILAC, "body", surface="shell")

    # teal toward the rear, where the petals close; darker below
    k.paint(lambda P: 0.7 * smoothstep(0.13, 0.27, P[:, 1] - BUD_C[1] + 0.05), TEAL, on=["petal", "core"])
    k.paint(lambda P: 0.5 * smoothstep(0.0, -0.12, P[:, 2] - BUD_C[2]) * (P[:, 1] < 0.17), BLUE_DK,
            on=["petal", "core"])
    # pale streaks along the petals: a few short arcs round the length
    def streaks(P):
        q = P - BUD_C
        th = np.arctan2(q[:, 2], q[:, 0])
        m = np.zeros(len(P))
        for a0, y0, y1 in ((1.15, -0.06, 0.08), (0.55, -0.02, 0.12), (1.9, 0.0, 0.11), (2.55, -0.08, 0.04),
                           (-0.45, -0.04, 0.09)):
            m = np.maximum(m, inside(np.abs(th - a0 - 0.6 * q[:, 1]) - 0.025, 0.008)
                           * inside(y0 - q[:, 1], 0.01) * inside(q[:, 1] - y1, 0.01))
        return m
    k.paint(streaks, STREAK, on=["petal", "core"])

    # --- the face: the steel-blue cone pointing forward ----------------------------------------------------------------
    G.cone(k, CONE_C, CONE_R, CONE_L, CONE, "head")

    k.build(voxel=VOXEL, smooth=4, band=0.005, wband=0.04)
    k.eye_states(G.cone_eyes(k, CONE_C, CONE_R, CONE_L, (0.03, 0.022), CONE, up_angle=28, back=0.32))
    k.export(out)


if __name__ == "__main__":
    glimmora(*args())
