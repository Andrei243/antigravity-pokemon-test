# Sandslash-Alola sculpted on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/sandslashalola.py -- <the export's Sandslash-Alola.glb> <out.glb>
# Grown from sandslash.py (its hunched trunk, head, eyes and the quill's shape) in sandshrewalola.py's ice colours.
# From the references: hunched forward, pale icy blue with a white belly; its back from the crown to the tail a crown
# of great pale cyan icicles, the largest mass of the design, rising up and back and spreading to the sides; a pale
# blue head with a long pointed snout, two long pointed ears and three small icicles behind them; black almond eyes
# with a white glint; long lavender-blue forearms held forward and down, each ending in two long pale ice claws at
# knee height; strong round thighs on short legs, two pale claws a foot; a short tail with small icicles on it.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, args, sibling

sh = sibling("sandshrew")
ss = sibling("sandslash")
sa = sibling("sandshrewalola")

ICE, CLAW, INK = "#C8E2F6", "#DDE2F4", sa.INK
BELLY = "#F6F6F4"
ARM = "#A6B2DE"                       # the forearms, a lavender blue (the drawing's)
QUILL, QUILL_HI = "#8CD0F2", "#B4E4F8"

BODY_C, BODY_R, BODY_ROT = ss.BODY_C, ss.BODY_R, ss.BODY_ROT
HEAD_C, HEAD_R = np.array([0.0, -0.14, 0.565]), np.array([0.096, 0.094, 0.088])


def build(src, out):
    k = Kit(src, coat="shell")
    k.add("body", ell(BODY_C, BODY_R, rot=BODY_ROT), ICE, "body")
    k.add("chest", ell((0, -0.07, 0.43), (0.125, 0.105, 0.105)), ICE, "body")
    k.add("hips", ell((0, 0.06, 0.25), (0.135, 0.13, 0.1)), ICE, "body")
    k.add("neck", ell((0, -0.1, 0.5), (0.075, 0.07, 0.07)), ICE, "head")
    k.add("head", ell(HEAD_C, HEAD_R), ICE, "head")
    k.add("snout", ell((0, -0.225, 0.53), (0.046, 0.074, 0.039), rot=(22, 0, 0), deform=sh.pointed), ICE, "head")
    # two long pointed ears, up and a little out
    k.add("ear", sh.ear((0.05, -0.12, 0.63), (0.085, -0.1, 0.75), 0.04, depth=0.036), ICE, "ear{s}", mirror=True)
    # three small icicles behind the ears, laid back
    for j, (x, length) in enumerate(((-0.04, 0.1), (0.0, 0.12), (0.04, 0.1))):
        root = HEAD_C + np.array([x, 0.035, 0.06])
        k.add(f"crown{j}", ss.spine(root, (x * 3, 0.7, 0.75), length, 0.055, 0.034), QUILL if j == 1 else QUILL_HI,
              "head", fuse=False, surface="shell")

    # long forearms forward and down, two long ice claws each at knee height
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"arm{side}", tube(np.array([(0.12, -0.06, 0.44), (0.165, -0.14, 0.38), (0.17, -0.22, 0.28),
                                           (0.165, -0.25, 0.2)]) * m, [0.054, 0.05, 0.047, 0.045]), ICE, f"arm{side}")
        h = np.array([0.165, -0.265, 0.18])
        for j, dx in enumerate((-0.02, 0.02)):
            r = (h + (dx, 0.0, 0.0)) * m
            k.add(f"hclaw{side}{j}", sh.claw(r, (dx * s * 1.5, -0.55, -1), 0.13, 0.036, up=(0, -1, 0.3), curl=0.3),
                  CLAW, f"arm{side}", fuse=False, surface="shell")
    # legs: strong round thighs, short shins, two claws a foot
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"thigh{side}", ell(np.array((0.13, 0.02, 0.19)) * m, (0.095, 0.11, 0.105)), ICE, f"leg{side}")
        k.add(f"shin{side}", tube(np.array([(0.135, 0.0, 0.13), (0.135, -0.02, 0.05)]) * m, [0.054, 0.046]), ICE,
              f"leg{side}")
        k.add(f"foot{side}", ell(np.array((0.135, -0.05, 0.03)) * m, (0.054, 0.078, 0.032)), ICE, f"leg{side}")
        for j, dx in enumerate((-0.018, 0.018)):
            r = np.array((0.135 + dx, -0.12, 0.022)) * m
            k.add(f"fclaw{side}{j}", sh.claw(r, (dx * 1.2 * s, -1, -0.15), 0.048, 0.025, curl=0.25), CLAW,
                  f"leg{side}", fuse=False, surface="shell")
    # the tail: pale blue, pointed, back and low, out to its right, two small icicles on it
    TAIL = [(0, 0.13, 0.23), (0.05, 0.25, 0.19), (0.1, 0.35, 0.15), (0.135, 0.42, 0.13)]
    k.add("tail", tube(TAIL, [0.07, 0.05, 0.03, 0.008]), ICE, "tail")
    for j, (p, length) in enumerate((((0.045, 0.24, 0.235), 0.09), ((0.09, 0.33, 0.18), 0.07))):
        k.add(f"tspike{j}", ss.spine(p, (0.2, 0.6, 0.8), length, 0.05, 0.03), QUILL_HI, "tail", fuse=False,
              surface="shell")

    # --- the icicles: few and great, rising up and back from the crown of the back and spreading to the sides ----
    # (elevation round the body from its middle, azimuths from straight back toward +X, length, colour)
    rows = [(74, [-18, 18], 0.36, QUILL), (56, [-62, 0, 62], 0.34, QUILL_HI),
            (36, [-88, -36, 36, 88], 0.3, QUILL), (16, [-66, 0, 66], 0.26, QUILL_HI),
            (-4, [-86, -34, 34, 86], 0.2, QUILL), (-24, [-30, 30], 0.15, QUILL_HI)]
    c18, s18 = math.cos(math.radians(18)), math.sin(math.radians(18))
    rm = np.array([[1, 0, 0], [0, c18, -s18], [0, s18, c18]])
    n = 0
    for elev, phis, length, colour in rows:
        e = math.radians(elev)
        for phi in phis:
            p = math.radians(phi)
            dirn = rm @ np.array([math.sin(p) * math.cos(e), math.cos(p) * math.cos(e), math.sin(e)])
            root = BODY_C + dirn * BODY_R * 0.72
            lay = unit(*(dirn * np.array([0.75, 0.35, 0.3]) + np.array([0, 0.42, 0.95])))
            k.add(f"quill{n}", ss.spine(root, lay, length, 0.16, 0.07), colour, "body", fuse=False, surface="shell")
            n += 1

    # --- the white belly, and under the snout ------------------------------------------------------------------
    def belly(Q):
        x, y, z = Q[:, 0], Q[:, 1], Q[:, 2]
        e = (x / 0.105) ** 2 + ((z - 0.33) / 0.16) ** 2
        return inside((np.sqrt(e) - 1) * 0.1, 0.0025) * (y < -0.06)
    k.paint(belly, BELLY, on=["body", "chest", "hips"])
    # the forearms lavender from the elbow down
    for s, side in ((1, "R"), (-1, "L")):
        k.paint(lambda Q: inside(Q[:, 2] - 0.36, 0.003), ARM, on=[f"arm{side}"])

    # --- the eyes: black almonds slanting up toward the back, a white glint (sandslash.py's) ---------------------
    EC = np.array([0.054, -0.208, 0.585])
    de = unit(0.7, -0.62, 0.22)
    BLACK, WHITE = srgb("#121216"), srgb("#FFFFFF")
    def look(u, v):
        c = np.tile(BLACK, (len(u), 1))
        c[((u + 0.15) / 0.3) ** 2 + ((v - 0.3) / 0.36) ** 2 < 1] = WHITE
        return c
    lid = srgb(ICE)
    def eyes(state):
        k.decal(EC, de, (0.036, 0.026), eye_look(state, look, lid, srgb(INK), line=0.18), eps=0.004, rings=20,
                segs=56, up=(0, -0.45, 1), mirror=True)

    k.build(voxel=0.0065, smooth=4, band=0.006)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    build(*args())
