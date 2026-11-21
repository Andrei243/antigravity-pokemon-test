# Solosis sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/solosis.py -- <the export's Solosis.glb> <out.glb>
#
# Written to be grown from (Duosion, Reuniclus: sibling("solosis")). From the references: a pale green cell made of
# two round lobes side by side (two humps on top, a cleft between two round lobes at the bottom), held in a ball of
# darker green jelly a good deal larger than it; on the cell's front two upright dark green oval eyes set wide and a
# red diamond between them, tilted, for a mouth (the task's text spoke of a yellow diamond: the art's is red, the 3D
# model's brown; red it is); a yellow horn curling up and over on the cell's upper right (+X); a pale glint on the
# jelly's upper left.
#
# The jelly: nothing in the game is see-through, so the jelly is one opaque mass FUSED with the cell, the cell set
# forward in it so that its whole front stands out of the jelly's surface: from the front the jelly is a dark rim round
# the pale cell (as the pictures show it), from behind a green ball. Fused rather than a loose shell so that the edge
# between the two is the crease where they meet (the nearest part's colour, a narrow band), the jelly takes the eyes'
# patches nowhere, and the evolutions' jelly arms and lobes blend into one smooth mass.
# The skeleton: root (0, 0, 0), body and head (0, 0, 0.161). It hovers.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, polygon, args

JELLY, CELL, EYE, MOUTH, MOUTH_RIM, HORN, GLINT = ("#57A97C", "#D2E5A6", "#26402F", "#DE6A6C", "#8E3442",
                                                   "#EED27C", "#C9E8CF")

# --- the frame ------------------------------------------------------------------------------------------------------
JC, JR = np.array([0.0, 0.016, 0.163]), np.array([0.13, 0.124, 0.128])     # the jelly
CC, CR = np.array([0.0, -0.072, 0.152]), np.array([0.096, 0.07, 0.074])      # the cell's main body
EYE_X, EYE_Z, EYE_HALF = 0.056, 0.158, (0.0135, 0.022)
MOUTH_Z, MOUTH_HALF, MOUTH_TILT = 0.15, (0.019, 0.031), -14
VOXEL = 0.003
EDGE = 0.0012                    # the jelly's edge round the cell: under a voxel, so it is the crisp line
OFF = 0.004                      # drawn this far out of the jelly's surface, past the fillet the remesh leaves


def shell_distance(P, c, r):
    """About how far each point lies outside an ellipsoid's surface (below nought inside)."""
    q = (P - np.asarray(c, float)) / np.asarray(r, float)
    rho = np.sqrt((q * q).sum(1))
    return (rho - 1) * np.asarray(r, float).min()


def crisp_edge(k, jc, jr, region_c, region_r, colour=None):
    """
    The line between the cell and the jelly drawn where the cell leaves the jelly's own surface: the jelly's colour
    wherever a point lies on or inside the jelly's ellipsoid, the cell's wherever it stands out of it near the cell.
    Nearest-part colouring alone smears it over the shallow fillet the remesh leaves there.
    """
    def cell(P):
        near = shell_distance(P, region_c, region_r) < 0
        return inside(OFF - shell_distance(P, jc, jr), EDGE) * near * (P[:, 1] < jc[1])
    def jelly(P):
        near = shell_distance(P, region_c, region_r) < 0
        return inside(shell_distance(P, jc, jr) - OFF, EDGE) * near
    k.paint(cell, colour or CELL, on=["cell"])
    k.paint(jelly, JELLY)


def on_ell(c, r, d):
    """The point of an ellipsoid (centre c, radii r) in a direction d from its centre."""
    d = unit(*d)
    return np.array(c, float) + d / math.sqrt(((d / np.array(r, float)) ** 2).sum())


def normal_at(c, r, p):
    """The ellipsoid's outward normal at its point p."""
    return unit(*((np.array(p, float) - np.array(c, float)) / np.array(r, float) ** 2))


def eye_open(u, v):
    """An upright oval of dark green, all of it: the design's eye has no white and no glint."""
    return np.tile(srgb(EYE), (len(u), 1))


def eye_patch(k, centre, normal, half, state, lid=CELL, look=None):
    """One eye as a patch in a state; u outward on both sides (the left's colour function mirrored)."""
    f = eye_look(state, look or eye_open, srgb(lid), srgb(EYE), line=0.18)
    col = f if centre[0] > 0 else (lambda u, v, f=f: f(-u, v))
    k.decal(centre, normal, half, col, eps=0.0022, rings=20, segs=56)


DIAMOND = [(0, 1), (1, 0), (0, -1), (-1, 0)]


def diamond(k, centre, normal, half, tilt, rim=0.22):
    """The red diamond: a dark rim and the red over it, tilted in the face's plane by tilt degrees."""
    t = math.radians(tilt)
    up = np.array([math.sin(t), 0, math.cos(t)])
    k.decal(centre, normal, (half[0] * (1 + rim), half[1] * (1 + rim * 0.6)), MOUTH_RIM, up=up, eps=0.0022,
            outline=polygon(DIAMOND), rings=10, segs=96)
    k.decal(centre, normal, half, MOUTH, up=up, eps=0.0032, outline=polygon(DIAMOND), rings=10, segs=96)


def glint(k, c, r, d, half, up=(0, 0, 1)):
    """A pale glint on the jelly, laid where the light would catch it."""
    p = on_ell(c, r, d)
    k.decal(p, normal_at(c, r, p), half, GLINT, up=up, eps=0.0022, rings=10, segs=40)


def horn(k, base, scale, bone):
    """The yellow horn: rising from the cell's upper right, curling out and over to a point that turns up again."""
    b = np.array(base, float)
    pts = [(0.0, 0.0, -0.006), (0.004, -0.004, 0.02), (0.018, -0.006, 0.036), (0.036, -0.006, 0.038),
           (0.046, -0.004, 0.026), (0.044, -0.002, 0.012), (0.05, -0.002, 0.004)]
    radii = [0.016, 0.015, 0.0135, 0.012, 0.0105, 0.0095, 0.009]
    k.add("horn", tube([b + np.array(p) * scale for p in pts], [r * scale for r in radii]), HORN, bone,
          surface="shell")


def solosis(src, out):
    k = Kit(src, coat="shell")
    k.add("jelly", ell(JC, JR, sub=5), JELLY, "body")
    # The cell: one round body made of two round halves side by side, a soft dip between them on top and a cleft
    # between two round lobes under it
    k.add("cell", ell(CC, CR, sub=5), CELL, "head", surface="fur")
    for s in (1, -1):
        k.add("cellside" + ("R" if s > 0 else "L"), ell((0.04 * s, CC[1] + 0.008, CC[2] + 0.01),
                                                         (0.064, 0.06, 0.07)), CELL, "head", surface="fur")
        k.add("celllobe" + ("R" if s > 0 else "L"), ell((0.044 * s, CC[1] + 0.008, CC[2] - 0.036),
                                                         (0.054, 0.054, 0.046)), CELL, "head", surface="fur")
    horn(k, (0.058, CC[1] - 0.002, CC[2] + 0.07), 1.0, "head")
    crisp_edge(k, JC, JR, CC, CR * 1.5)
    # The mouth and the glint go in before build(): they are in every eye state
    mc = on_ell(CC, CR, (0, -1, (MOUTH_Z - CC[2]) / CR[2] * 1.0))
    diamond(k, mc, normal_at(CC, CR, mc), MOUTH_HALF, MOUTH_TILT)
    glint(k, JC, JR, (-0.62, -0.42, 0.66), (0.03, 0.017), up=unit(0.55, 0, 0.84))
    k.build(voxel=VOXEL, smooth=3, band=0.004)

    def eyes(state):
        for s in (1, -1):
            p = on_ell(CC, CR, (EYE_X * s / CR[0], -1.0, (EYE_Z - CC[2]) / CR[2]))
            n = normal_at(CC, CR, p)
            n = unit(n[0] * 0.7, n[1], n[2] * 0.7)
            eye_patch(k, p, n, EYE_HALF, state)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    solosis(*args())
