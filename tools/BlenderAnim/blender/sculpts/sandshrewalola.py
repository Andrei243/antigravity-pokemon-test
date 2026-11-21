# Sandshrew-Alola sculpted on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/sandshrewalola.py -- <the export's Sandshrew-Alola.glb> <out.glb>
# Grown from sandshrew.py (its claws, ears, snout and way of making the eyes and seams). From the references: the same
# round little armadillo standing upright, its shell now of pale icy blue blocks drawn by darker blue seams, the head a
# hood of ice that comes down round the face in an arch; large dark navy eyes with a white glint just under the hood's
# rim; a pale yellow muzzle pointing down and forward; a pale yellow belly (the 3D picture's; the drawing's is grey with
# yellow low down); small hood points for ears, darker blue inside; lavender-blue feet with grey-white claws; short arms
# with three grey-white claws; a thick pointed tail of blocks behind, turned to its right.
# Written to be grown from (sandslashalola.py): the colours at module level, the build under __main__.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, eye_look, args, sibling

sh = sibling("sandshrew")

ICE, SEAM, CREAM, FOOT, CLAW = "#D4E6F8", "#6F94C8", "#F2E8AE", "#A6AEDC", "#E2E5EE"
EARIN = "#90A8D2"
NAVY, INK = "#1A2E66", "#0C1224"

BODY_C, BODY_R = np.array([0.0, 0.0, 0.26]), np.array([0.158, 0.143, 0.165])
HEAD_C, HEAD_R = np.array([0.0, -0.045, 0.45]), np.array([0.124, 0.112, 0.116])


def on_head(d):
    """The point of the head's ellipsoid along d from its centre."""
    d = unit(*d)
    return HEAD_C + d / np.sqrt(((d / HEAD_R) ** 2).sum())


def build(src, out):
    k = Kit(src, coat="shell")
    k.add("body", ell(BODY_C, BODY_R), ICE, "body")
    k.add("chest", ell((0, -0.04, 0.33), (0.13, 0.11, 0.1)), ICE, "body")
    k.add("head", ell(HEAD_C, HEAD_R), ICE, "head")
    # the hood swells a little at the sides and back of the head, round the face
    k.add("hood", ell(HEAD_C + (0, 0.025, 0.012), (0.128, 0.105, 0.11)), ICE, "head")
    # the snout: pointed, down and forward from under the eyes
    k.add("snout", ell((0, -0.14, 0.395), (0.05, 0.066, 0.042), rot=(38, 0, 0), deform=sh.pointed), ICE, "head")
    # small hood points for ears, leaning out and back
    k.add("ear", sh.ear((0.075, 0.0, 0.53), (0.112, 0.03, 0.6), 0.042), ICE, "ear{s}", mirror=True)

    # arms: short and thick, held in front, three claws on each hand
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"arm{side}", tube(np.array([(0.12, -0.04, 0.335), (0.15, -0.1, 0.3), (0.135, -0.155, 0.265)]) * m,
                                 [0.052, 0.049, 0.044]), ICE, f"arm{side}")
        h = np.array([0.13, -0.19, 0.255])
        for j, dx in enumerate((-0.022, 0.0, 0.022)):
            r = (h + (dx, 0.004 * abs(dx) / 0.022, 0.0)) * m
            k.add(f"hclaw{side}{j}", sh.claw(r, (dx * 1.5 * s, -1, -0.5), 0.036, 0.019), CLAW, f"arm{side}",
                  fuse=False, surface="shell")
    # legs: round thighs, lavender feet, two claws each
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"thigh{side}", ell(np.array((0.125, -0.01, 0.14)) * m, (0.088, 0.092, 0.092)), ICE, f"leg{side}")
        k.add(f"foot{side}", ell(np.array((0.13, -0.05, 0.03)) * m, (0.054, 0.078, 0.034)), FOOT, f"leg{side}")
        for j, dx in enumerate((-0.017, 0.017)):
            r = np.array((0.13 + dx, -0.12, 0.022)) * m
            k.add(f"fclaw{side}{j}", sh.claw(r, (dx * 1.2 * s, -1, -0.15), 0.034, 0.021, curl=0.25), CLAW,
                  f"leg{side}", fuse=False, surface="shell")
    # the tail: thick at its root, flat-ish and pointed, back, up and out to its right
    TAIL = np.array([(0, 0.11, 0.17), (0.05, 0.22, 0.15), (0.11, 0.31, 0.17), (0.17, 0.38, 0.22)])
    k.add("tail", tube(TAIL, [0.078, 0.062, 0.038, 0.01]), ICE, "tail")

    # --- the blocks' seams: rows round the back, the joints offset row to row as bricks ---------------------------
    def back_bricks(Q):
        x, y, z = Q[:, 0] - BODY_C[0], Q[:, 1] - BODY_C[1], Q[:, 2]
        phi = np.arctan2(x, y)
        rows = np.array([0.16, 0.27, 0.37])
        dz = np.abs(z[:, None] - rows[None]).min(1)
        line = inside(dz - 0.005, 0.002)
        row = np.searchsorted(rows, z)
        step = 0.9
        f = (phi / step + 0.5 * (row % 2)) % 1.0
        g = np.minimum(f, 1 - f) * step * np.hypot(x, y)
        join = inside(g - 0.005, 0.002) * (z > 0.1) * (z < 0.43)
        back = smoothstep(-0.02, 0.03, y + 0.6 * np.abs(x) - 0.05) * (np.abs(phi) < 2.2)
        return np.maximum(line, join) * back
    k.paint(back_bricks, SEAM, on=["body", "chest"])

    # the hood: its rim an arch over the eyes coming down beside the cheeks; a line across the crown and the nape
    def hood_lines(Q):
        x, y, z = Q[:, 0], Q[:, 1], Q[:, 2]
        rim = inside(np.abs(z - (0.56 - 8.0 * x * x)) - 0.0055, 0.002) * (y < -0.07) * (z > 0.37)
        crown = inside(np.abs(y + 0.005 + 0.2 * (z - 0.5)) - 0.005, 0.002) * (z > 0.5)
        nape = inside(np.abs(y - 0.06 + 0.1 * (z - 0.45)) - 0.005, 0.002) * (z > 0.36)
        return np.maximum(np.maximum(rim, crown), nape)
    k.paint(hood_lines, SEAM, on=["head", "hood"])
    # arms, thighs and tail: seams across them
    for s, side in ((1, "R"), (-1, "L")):
        k.paint(sh.rings(np.array((0.12 * s, -0.04, 0.335)), (0.03 * s, -0.06, -0.035), [0.055], half=0.005), SEAM,
                on=[f"arm{side}"])
        k.paint(lambda Q, s=s: inside(np.abs(Q[:, 2] - 0.15) - 0.005, 0.002) * (Q[:, 0] * s > 0.12)
                * (Q[:, 1] > -0.07), SEAM, on=[f"thigh{side}"])
        k.paint(lambda Q, s=s: inside(np.abs((Q[:, 0] * s - 0.15) + 0.4 * (Q[:, 1] - 0.0)) - 0.005, 0.002)
                * (Q[:, 2] > 0.15) * (Q[:, 1] > -0.07), SEAM, on=[f"thigh{side}"])
    tc = TAIL[0]; td = TAIL[-1] - TAIL[0]
    k.paint(sh.rings(tc, td, [0.08, 0.15, 0.21], half=0.005), SEAM, on=["tail"])
    k.paint(lambda Q: inside(np.abs(Q[:, 2] - (0.17 + 0.25 * (Q[:, 1] - 0.11))) - 0.005, 0.002) * (Q[:, 1] > 0.15),
            SEAM, on=["tail"])

    # --- the yellow: the belly from the chest down, the muzzle under the eyes ------------------------------------
    def belly(Q):
        x, y, z = Q[:, 0], Q[:, 1], Q[:, 2]
        e = (x / 0.115) ** 2 + ((z - 0.24) / 0.155) ** 2
        return inside((np.sqrt(e) - 1) * 0.1, 0.0025) * (y < -0.04)
    k.paint(belly, CREAM, on=["body", "chest"])
    def muzzle(Q):
        x, y, z = Q[:, 0], Q[:, 1], Q[:, 2]
        top = 0.405 + 0.45 * np.abs(x) + 0.25 * (y + 0.15)
        return inside(z - top, 0.0025) * (y < -0.1) * (z > 0.33) * (np.abs(x) < 0.075)
    k.paint(muzzle, CREAM, on=["head", "snout", "hood"])
    # darker blue inside the ears: their front faces
    for s, side in ((1, "R"), (-1, "L")):
        b, t = np.array((0.075 * s, 0.0, 0.53)), np.array((0.112 * s, 0.03, 0.6))
        ax = unit(*(t - b))
        def lining(Q, b=b, ax=ax):
            r = Q - b
            along = r @ ax
            across = np.linalg.norm(r - np.outer(along, ax), axis=1)
            w = 0.024 * (1 - along / 0.085)
            return inside(across - w, 0.002) * (along > 0.012) * (Q[:, 1] < b[1] + 0.002 + 0.25 * along)
        k.paint(lining, EARIN, on=[f"ear{side}"])

    # --- the eyes: large dark navy ovals, a white glint, just under the hood's rim --------------------------------
    de = unit(0.62, -0.75, 0.15)
    EC = on_head(de) + np.array([0, -0.004, 0])
    look = sh.oval_eye(NAVY, INK)
    lid = srgb(ICE)
    def eyes(state):
        k.decal(EC, de, (0.034, 0.045), eye_look(state, look, lid, srgb(INK), line=0.16), eps=0.004, rings=20,
                segs=56, mirror=True)

    k.build(voxel=0.006, smooth=4, band=0.006)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    build(*args())
