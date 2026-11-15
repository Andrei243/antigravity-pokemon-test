# Sandslash sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/sandslash.py -- <the export's Sandslash.glb> <out.glb>
# Sandshrew grown (sandshrew.py: the same colours, claws, ears and way of making the eyes). From the references: hunched
# forward, its back from the crown to the tail bristling with big brown pointed spines laid back like quills, two
# shades of brown; a yellow head with a long pointed snout, a yellow spike rising from the forehead, long pointed ears,
# black almond eyes with a white glint; a cream belly; arms held forward with two long curved white claws each; strong
# round thighs on short legs, two white claws a foot; a yellow tail pointed back and low, turned to its right.
# The plates' seams of Sandshrew are gone (the pictures show none).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, eye_look, polygon, args, hull, sibling

sh = sibling("sandshrew")
SKIN, CREAM, CLAW, INK = sh.SKIN, sh.CREAM, sh.CLAW, sh.INK
QUILL, QUILL_DARK = "#A86C3A", "#7C4A24"

BODY_C, BODY_R, BODY_ROT = np.array([0.0, 0.02, 0.37]), np.array([0.15, 0.16, 0.17]), (18, 0, 0)
HEAD_C, HEAD_R = np.array([0.0, -0.135, 0.56]), np.array([0.09, 0.088, 0.082])


def spine(root, d, length, width, thick, up=(0, 0, 1.0)):
    """A quill: a solid leaf of a kite section, widest a third of the way out, from root along d."""
    d = unit(*d)
    side = np.cross(d, up); side /= np.linalg.norm(side)
    n = np.cross(side, d)
    root = np.array(root, float)
    mid = root + d * length * 0.32
    tip = root + d * length
    pts = [root + side * width * 0.32, root - side * width * 0.32, root + n * thick * 0.5, root - n * thick * 0.5,
           root - d * width * 0.25,
           mid + side * width * 0.5, mid - side * width * 0.5, mid + n * thick * 0.5, mid - n * thick * 0.5,
           root + d * length * 0.7 + side * width * 0.2, root + d * length * 0.7 - side * width * 0.2,
           tip]
    return hull(pts)


def build(src, out):
    k = Kit(src, coat="shell")
    k.add("body", ell(BODY_C, BODY_R, rot=BODY_ROT), SKIN, "body")
    k.add("chest", ell((0, -0.07, 0.43), (0.12, 0.1, 0.1)), SKIN, "body")
    k.add("hips", ell((0, 0.06, 0.25), (0.13, 0.13, 0.1)), SKIN, "body")
    k.add("neck", ell((0, -0.1, 0.5), (0.075, 0.07, 0.07)), SKIN, "head")
    k.add("head", ell(HEAD_C, HEAD_R), SKIN, "head")
    k.add("snout", ell((0, -0.215, 0.53), (0.045, 0.068, 0.038), rot=(20, 0, 0), deform=sh.pointed), SKIN, "head")
    k.add("ear", sh.ear((0.055, -0.1, 0.62), (0.105, -0.06, 0.72), 0.034), SKIN, "ear{s}", mirror=True)
    # the forehead's spike, rising and leaning back
    k.add("horn", spine((0, -0.17, 0.6), (0, 0.25, 1), 0.1, 0.05, 0.036), SKIN, "head", fuse=False, surface="shell")

    # arms held forward, two long curved claws on each hand
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"arm{side}", tube(np.array([(0.12, -0.06, 0.44), (0.16, -0.13, 0.4), (0.17, -0.2, 0.36)]) * m,
                                 [0.052, 0.046, 0.044]), SKIN, f"arm{side}")
        h = np.array([0.17, -0.235, 0.355])
        for j, dx in enumerate((-0.017, 0.017)):
            r = (h + (dx, 0.0, 0.0)) * m
            k.add(f"hclaw{side}{j}", sh.claw(r, (-0.12 * s + dx * s, -1, -0.25), 0.12, 0.032, curl=0.4), CLAW,
                  f"arm{side}", fuse=False, surface="shell")
    # legs: strong round thighs, short shins, two claws a foot
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"thigh{side}", ell(np.array((0.13, 0.02, 0.2)) * m, (0.09, 0.105, 0.1)), SKIN, f"leg{side}")
        k.add(f"shin{side}", tube(np.array([(0.135, 0.0, 0.14), (0.135, -0.02, 0.05)]) * m, [0.05, 0.042]), SKIN,
              f"leg{side}")
        k.add(f"foot{side}", ell(np.array((0.135, -0.05, 0.028)) * m, (0.05, 0.075, 0.03)), SKIN, f"leg{side}")
        for j, dx in enumerate((-0.017, 0.017)):
            r = np.array((0.135 + dx, -0.115, 0.022)) * m
            k.add(f"fclaw{side}{j}", sh.claw(r, (dx * 1.2 * s, -1, -0.15), 0.045, 0.024, curl=0.25), CLAW,
                  f"leg{side}", fuse=False, surface="shell")
    # the tail: yellow, pointed, back and low, out to its right
    k.add("tail", tube([(0, 0.13, 0.23), (0.05, 0.25, 0.19), (0.1, 0.35, 0.15), (0.135, 0.42, 0.13)],
                       [0.07, 0.05, 0.028, 0.006]), SKIN, "tail")

    # --- the quills: rows over the back from the crown down, laid back and a little up ----------------------------
    rows = [(68, [-30, 30], 0.18, QUILL), (52, [-66, -22, 22, 66], 0.21, QUILL_DARK),
            (34, [-84, -50, -17, 17, 50, 84], 0.22, QUILL), (16, [-70, -35, 0, 35, 70], 0.21, QUILL_DARK),
            (-2, [-84, -50, -17, 17, 50, 84], 0.19, QUILL), (-20, [-58, -20, 20, 58], 0.17, QUILL_DARK),
            (-38, [-32, 0, 32], 0.14, QUILL)]
    rm = np.array([[1, 0, 0], [0, math.cos(math.radians(18)), -math.sin(math.radians(18))],
                   [0, math.sin(math.radians(18)), math.cos(math.radians(18))]])
    n = 0
    for elev, phis, length, colour in rows:
        e = math.radians(elev)
        for phi in phis:
            p = math.radians(phi)
            dirn = np.array([math.sin(p) * math.cos(e), math.cos(p) * math.cos(e), math.sin(e)])
            dirn = rm @ dirn
            root = BODY_C + dirn * BODY_R * 0.78
            lay = unit(*(dirn * np.array([0.7, 0.45, 0.4]) + np.array([0, 0.55, 0.7])))
            k.add(f"quill{n}", spine(root, lay, length, 0.1, 0.04), colour, "body", fuse=False, surface="shell")
            n += 1
    # three over the crown, between the ears, laid back
    for j, (x, length) in enumerate(((-0.035, 0.12), (0.0, 0.14), (0.035, 0.12))):
        root = HEAD_C + np.array([x, 0.02, 0.055])
        k.add(f"crown{j}", spine(root, (x * 3, 0.85, 0.6), length, 0.06, 0.034), QUILL_DARK if j == 1 else QUILL,
              "head", fuse=False, surface="shell")

    # --- the cream: the belly, and under the snout ---------------------------------------------------------------
    def belly(Q):
        x, y, z = Q[:, 0], Q[:, 1], Q[:, 2]
        e = (x / 0.1) ** 2 + ((z - 0.34) / 0.15) ** 2
        return inside((np.sqrt(e) - 1) * 0.1, 0.0025) * (y < -0.06)
    k.paint(belly, CREAM, on=["body", "chest", "hips"])
    def chin(Q):
        x, y, z = Q[:, 0], Q[:, 1], Q[:, 2]
        top = 0.497 + 0.3 * np.abs(x) + 0.25 * (y + 0.2)
        return inside(z - top, 0.0025) * (y < -0.15)
    k.paint(chin, CREAM, on=["snout", "head"])

    # --- the eyes: black almonds, slanting up toward the back, a white glint ------------------------------------------
    # an oval whose up leans forward, so that its long axis rises toward the back
    EC = np.array([0.056, -0.2, 0.578])
    de = unit(0.7, -0.62, 0.22)
    half = (0.035, 0.025)
    BLACK, WHITE = srgb("#121216"), srgb("#FFFFFF")
    def look(u, v):
        c = np.tile(BLACK, (len(u), 1))
        c[((u + 0.15) / 0.3) ** 2 + ((v - 0.3) / 0.36) ** 2 < 1] = WHITE
        return c
    lid = srgb(SKIN)
    def eyes(state):
        k.decal(EC, de, half, eye_look(state, look, lid, srgb(INK), line=0.18), eps=0.005, rings=20, segs=56,
                up=(0, -0.45, 1), mirror=True)

    k.build(voxel=0.0065, smooth=4, band=0.006)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    build(*args())
