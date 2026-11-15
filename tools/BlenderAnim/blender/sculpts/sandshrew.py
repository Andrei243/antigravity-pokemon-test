# Sandshrew sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/sandshrew.py -- <the export's Sandshrew.glb> <out.glb>
# From the references: a round little yellow armadillo standing upright, its head grown into its body with no neck,
# its back, head, arms, thighs and tail armoured in plates drawn by a few broad darker seams; a big cream belly from the
# chin to the legs and a cream muzzle under a pointed snout, the forehead's plate coming down to a point over it; large
# dark blue oval eyes with a white glint; small pointed ears, pink inside; short arms held in front with three white
# claws, round thighs on small feet with two white claws, and a thick pointed tail behind, turned to its right.
# Written to be grown from (sandslash.py): the colours, the claw, the eye, the ear and the seams are functions.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, eye_look, args, hull, outward

SKIN, SEAM, CREAM, PINK, CLAW = "#E8C870", "#A8823C", "#F4E6BE", "#E8A0A4", "#F6F6F8"
NAVY, INK = "#1E3A6E", "#0E1424"


# --- pieces of the line --------------------------------------------------------------------------------------------
def claw(root, d, length, width, up=(0, 0, 1.0), curl=0.35):
    """A claw: a solid with a kite section, its root width wide and 0.6 of that thick, curving down along d."""
    d = unit(*d)
    side = np.cross(d, up); side /= np.linalg.norm(side)
    down = np.cross(side, d)
    root = np.array(root, float)
    tip = root + d * length - down * length * curl
    mid = root + d * length * 0.5 - down * length * curl * 0.3
    t = width * 0.6
    pts = [root + side * width / 2, root - side * width / 2, root + down * t / 2, root - down * t / 2,
           root - d * width * 0.3,
           mid + side * width * 0.3, mid - side * width * 0.3, mid + down * t * 0.3, mid - down * t * 0.3,
           tip]
    return hull(pts)


def ear(base, tip, width, depth=0.034):
    """An ear: a flat pointed cone from base to tip, its face toward the front."""
    base, tip = np.array(base, float), np.array(tip, float)
    V, F = blade([base, base + (tip - base) * 0.5, tip], [width, width * 0.6, 0.004], (0, 1, 0),
                 depth / (2 * width))
    return V, F


def pointed(V):
    """A snout narrowing to its front (-Y), flatter there."""
    t = np.clip(-V[:, 1], 0, 1)
    return np.stack([V[:, 0] * (1 - 0.45 * t), V[:, 1], V[:, 2] * (1 - 0.3 * t)], 1)


def oval_eye(iris, ink, rim=0.84):
    I, K, W = srgb(iris), srgb(ink), srgb("#FFFFFF")
    def look(u, v):
        c = np.tile(I, (len(u), 1))
        c[v < -0.25] = I * 0.8 + srgb("#3E6CB0") * 0.2                 # a lighter blue low in the eye
        c[u * u + v * v > rim * rim] = K
        c[((u + 0.12) / 0.3) ** 2 + ((v - 0.34) / 0.3) ** 2 < 1] = W
        return c
    return look


def rings(c, axis, at, half=0.0045):
    """Seams round a part: planes square to axis through c at the distances given."""
    a = unit(*axis)
    def mask(Q):
        s = (Q - np.array(c, float)) @ a
        return np.max([inside(np.abs(s - t) - half, 0.002) for t in at], axis=0)
    return mask


# --- Sandshrew -----------------------------------------------------------------------------------------------------
BODY_C, BODY_R = np.array([0.0, 0.0, 0.26]), np.array([0.155, 0.14, 0.165])
HEAD_C, HEAD_R = np.array([0.0, -0.05, 0.445]), np.array([0.112, 0.1, 0.105])


def build(src, out):
    k = Kit(src, coat="shell")
    k.add("body", ell(BODY_C, BODY_R), SKIN, "body")
    k.add("chest", ell((0, -0.04, 0.33), (0.13, 0.11, 0.1)), SKIN, "body")
    k.add("head", ell(HEAD_C, HEAD_R), SKIN, "head")
    # the snout: pointed, forward and a little down
    k.add("snout", ell((0, -0.125, 0.405), (0.052, 0.066, 0.042), rot=(24, 0, 0), deform=pointed), SKIN, "head")
    k.add("ear", ear((0.068, -0.01, 0.525), (0.1, 0.015, 0.605), 0.04), SKIN, "ear{s}", mirror=True)

    # arms: short and thick, held in front, three claws on each hand
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"arm{side}", tube(np.array([(0.12, -0.04, 0.335), (0.15, -0.1, 0.3), (0.135, -0.155, 0.265)]) * m,
                                 [0.05, 0.047, 0.042]), SKIN, f"arm{side}")
        h = np.array([0.13, -0.19, 0.255])
        for j, dx in enumerate((-0.022, 0.0, 0.022)):
            r = (h + (dx, 0.004 * abs(dx) / 0.022, 0.0)) * m
            k.add(f"hclaw{side}{j}", claw(r, (dx * 1.5 * s, -1, -0.5), 0.034, 0.018), CLAW, f"arm{side}",
                  fuse=False, surface="shell")
    # legs: round thighs, small feet, two claws each
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"thigh{side}", ell(np.array((0.125, -0.01, 0.14)) * m, (0.085, 0.09, 0.09)), SKIN, f"leg{side}")
        k.add(f"foot{side}", ell(np.array((0.13, -0.05, 0.03)) * m, (0.05, 0.075, 0.032)), SKIN, f"leg{side}")
        for j, dx in enumerate((-0.016, 0.016)):
            r = np.array((0.13 + dx, -0.115, 0.022)) * m
            k.add(f"fclaw{side}{j}", claw(r, (dx * 1.2 * s, -1, -0.15), 0.032, 0.02, curl=0.25), CLAW, f"leg{side}",
                  fuse=False, surface="shell")
    # the tail: thick at its root, pointed, back, up and out to its right
    TAIL = np.array([(0, 0.11, 0.17), (0.05, 0.22, 0.15), (0.11, 0.31, 0.18), (0.16, 0.37, 0.235)])
    k.add("tail", tube(TAIL, [0.075, 0.058, 0.035, 0.008]), SKIN, "tail")

    # --- the plates' seams: rows round the back, the joints offset row to row as bricks ---------------------------
    def back_bricks(Q):
        x, y, z = Q[:, 0] - BODY_C[0], Q[:, 1] - BODY_C[1], Q[:, 2]
        phi = np.arctan2(x, y)                                   # nought at the back, +-pi in front
        rows = np.array([0.15, 0.245, 0.34])
        dz = np.abs(z[:, None] - rows[None]).min(1)
        line = inside(dz - 0.0045, 0.002)
        row = np.searchsorted(rows, z)
        step = 0.82
        f = (phi / step + 0.5 * (row % 2)) % 1.0
        g = np.minimum(f, 1 - f) * step * np.hypot(x, y)
        join = inside(g - 0.0045, 0.002) * (z > 0.1) * (z < 0.43)
        back = smoothstep(-0.02, 0.03, y + 0.6 * np.abs(x) - 0.05) * (np.abs(phi) < 2.2)
        return np.maximum(line, join) * back
    k.paint(back_bricks, SEAM, on=["body", "chest"])
    # the head's plate: a line across the crown, one over the nape, and the V of the forehead's point over the snout
    def head_plate(Q):
        x, y, z = Q[:, 0], Q[:, 1], Q[:, 2]
        crown = inside(np.abs(y - 0.0 + 0.25 * (z - 0.5)) - 0.0045, 0.002) * (z > 0.47)
        nape = inside(np.abs(y - 0.07 + 0.1 * (z - 0.45)) - 0.0045, 0.002) * (z > 0.38)
        vee = inside(np.abs(z - (0.405 + 1.0 * np.abs(x) + 0.35 * (y + 0.12))) - 0.0045, 0.002) * (y < -0.05) * \
            (np.abs(x) < 0.085) * (y > -0.2)
        return np.maximum(np.maximum(crown, nape), vee)
    k.paint(head_plate, SEAM, on=["head", "snout"])
    # arms, thighs and tail: seams across them
    for s, side in ((1, "R"), (-1, "L")):
        k.paint(rings(np.array((0.12 * s, -0.04, 0.335)), (0.03 * s, -0.06, -0.035), [0.055]), SEAM, on=[f"arm{side}"])
        k.paint(lambda Q, s=s: inside(np.abs(Q[:, 2] - 0.15) - 0.0045, 0.002) * (Q[:, 0] * s > 0.12)
                * (Q[:, 1] > -0.07), SEAM, on=[f"thigh{side}"])
        k.paint(lambda Q, s=s: inside(np.abs((Q[:, 0] * s - 0.15) + 0.4 * (Q[:, 1] - 0.0)) - 0.0045, 0.002)
                * (Q[:, 2] > 0.15) * (Q[:, 1] > -0.07), SEAM, on=[f"thigh{side}"])
    tc = TAIL[0]; td = TAIL[-1] - TAIL[0]
    k.paint(rings(tc, td, [0.075, 0.14, 0.2]), SEAM, on=["tail"])

    # --- the cream: the belly from the chin down, the muzzle under the snout's point --------------------------------
    def belly(Q):
        x, y, z = Q[:, 0], Q[:, 1], Q[:, 2]
        e = (x / 0.112) ** 2 + ((z - 0.245) / 0.15) ** 2
        return inside((np.sqrt(e) - 1) * 0.1, 0.0025) * (y < -0.04)
    k.paint(belly, CREAM, on=["body", "chest"])
    def muzzle(Q):
        x, y, z = Q[:, 0], Q[:, 1], Q[:, 2]
        top = 0.385 + 0.55 * np.abs(x) + 0.3 * (y + 0.12)
        return inside(z - top, 0.0025) * (y < -0.07) * (z > 0.3)
    k.paint(muzzle, CREAM, on=["head", "snout", "chest"])
    # pink inside the ears: their front faces, a narrower point within
    for s, side in ((1, "R"), (-1, "L")):
        b, t = np.array((0.068 * s, -0.01, 0.525)), np.array((0.1 * s, 0.015, 0.605))
        ax = unit(*(t - b))
        def lining(Q, b=b, ax=ax):
            r = Q - b
            along = r @ ax
            across = np.linalg.norm(r - np.outer(along, ax), axis=1)
            w = 0.025 * (1 - along / 0.088)
            return inside(across - w, 0.002) * (along > 0.012) * (Q[:, 1] < b[1] + 0.002 + 0.25 * (along))
        k.paint(lining, PINK, on=[f"ear{side}"])

    # --- the eyes: large dark blue ovals, a dark rim, a white glint ---------------------------------------------------
    EC = np.array([0.064, -0.122, 0.462])
    de = unit(0.68, -0.68, 0.18)
    half = (0.03, 0.04)
    look = oval_eye(NAVY, INK)
    lid = srgb(SKIN)
    def eyes(state):
        k.decal(EC, de, half, eye_look(state, look, lid, srgb(INK), line=0.16), eps=0.005, rings=20, segs=56,
                mirror=True)

    k.build(voxel=0.006, smooth=4, band=0.006)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    build(*args())
