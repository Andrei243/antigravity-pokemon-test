# Glimmet sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/glimmet.py -- <the export's Glimmet.glb> <out.glb>
#
# The design, from the references: a hanging lilac bud of crystal, a teardrop wide below and pinched to a thin neck
# above, with a little point beneath and a seam down its side. On the neck a flower: a star of seven pointed petals of
# deep blue crystal, uneven in length, and in front of them a pale grey-blue cone of crystal pointing forward, with two
# plain yellow oval eyes side by side on its root. It hovers.
#
# Written to be grown from (glimmora.py imports it with sibling): the petal (petal), the face's cone (cone) and its
# eyes (cone_eyes) are functions; the build is under the main guard.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, outward, srgb, inside, unit, eye_look, args

# --- the line's colours -------------------------------------------------------------------------------------------
LILAC, LILAC_DK = "#A69AD8", "#8478BA"
BLUE, BLUE_LT, BLUE_DK = "#1E5CC8", "#2F7DE0", "#173E96"
CONE_PALE = "#D2E2F0"
YELLOW, YELLOW_DK = "#F4E43A", "#C4A818"
INK = srgb("#262A40")

# --- the frame ----------------------------------------------------------------------------------------------------
BULB_C, BULB_R = np.array([0.0, 0.0, 0.2]), np.array([0.082, 0.078, 0.13])
FLOWER_C = np.array([0.0, -0.005, 0.4])
VOXEL = 0.0042


def pear(V):
    """A teardrop: narrowing toward its top (z +1) into a neck, a little point below."""
    V = V.copy()
    t = np.clip(V[:, 2], 0, 1)
    k = 1 - 0.78 * t ** 1.6
    V[:, 0] *= k; V[:, 1] *= k
    b = np.clip(-V[:, 2], 0, 1)
    V[:, 0] *= 1 - 0.25 * b ** 3; V[:, 1] *= 1 - 0.25 * b ** 3
    return V


def petal(c, d, n, root, length, width, thick):
    """A pointed petal of crystal from c out along d, its face along n: a flat-faced kite, thick across n, a ridge
    down its middle (a hull of its outline lifted on both faces)."""
    d = unit(*d); n = np.array(n, float); n = n - d * (n @ d); n /= np.linalg.norm(n)
    w = np.cross(n, d)
    c = np.array(c, float)
    pts = []
    for s, h in ((0.0, 0.35), (0.3, 0.9), (0.55, 1.0), (0.8, 0.6), (1.0, 0.0)):
        for side in (1, -1):
            pts.append(c + d * (root + (length - root) * s) + w * side * width * h)
    for s in (0.15, 0.5, 0.82):
        for face in (1, -1):
            pts.append(c + d * (root + (length - root) * s) + n * face * thick * (1 - 0.6 * s))
    return hull(np.array(pts))


def taper(V):
    """A cone's deform of the unit sphere: full at its back (y +), narrowing to a point at its front (y -1)."""
    V = V.copy()
    t = np.clip((-V[:, 1] + 0.15) / 1.15, 0, 1)
    k = 1 - 0.97 * t ** 1.3
    V[:, 0] *= k; V[:, 2] *= k
    return V


def cone(k, c, r, length, colour, bone, name="cone"):
    """The face's cone of crystal: its root at c, round with radius r, pointing forward (-Y) length long. Fused, so
    the eyes lie on it."""
    k.add(name, ell(np.array(c, float) + np.array([0, -length * 0.42, 0]), (r, length * 0.62, r), deform=taper),
          colour, bone, surface="shell")


def cone_eyes(k, c, r, length, half, lid, up_angle=34, back=0.32):
    """Two plain yellow ovals side by side on the cone's root, up_angle degrees either side of its top. Returns
    lay(state)."""
    c = np.array(c, float)
    def look(u, v):
        rr = np.hypot(u, v)
        col = np.tile(srgb(YELLOW), (len(u), 1))
        col[rr > 0.8] = srgb(YELLOW_DK)
        return col
    L = srgb(lid)
    def lay(state):
        lk = eye_look(state, look, L, INK, line=0.16)
        for s in (1, -1):
            a = math.radians(up_angle) * s
            y = c[1] - length * back
            # the cone's radius there, as taper leaves it
            vy = -(y - (c[1] - length * 0.42)) / (length * 0.62)
            t = np.clip((vy + 0.15) / 1.15, 0, 1)
            rr = r * (1 - 0.97 * t ** 1.3) * math.sqrt(max(1 - vy * vy, 0))
            p = np.array([c[0] + rr * math.sin(a), y, c[2] + rr * math.cos(a)])
            nrm = unit(math.sin(a), -0.6, math.cos(a))
            k.decal(p, nrm, half, (lambda u, v, s=s: lk(s * u, v)), up=unit(0, -1, 0.2), eps=0.0018, rings=20,
                    segs=56)
    return lay


def glimmet(src, out):
    k = Kit(src, coat="shell")

    # --- the bud: a lilac teardrop pinched into a neck, a point beneath --------------------------------------------
    k.add("bulb", ell(BULB_C, BULB_R, deform=pear), LILAC, "body", surface="shell")
    k.add("point", tube([(0.0, 0.0, 0.1), (0.0, -0.005, 0.07), (0.006, -0.012, 0.056)], [0.024, 0.014, 0.008]),
          LILAC, "body", surface="shell")
    k.add("neck", tube([(0.0, 0.0, 0.3), (0.0, 0.004, 0.35), (0.0, 0.012, 0.39)], [0.026, 0.021, 0.024]), LILAC,
          "head", surface="shell")
    # the seam down its side: a darker line by its angle round the bud
    def seam(P):
        q = P - BULB_C
        th = np.arctan2(q[:, 0], -q[:, 1])
        return inside(np.abs(th - 0.55 - 0.25 * (q[:, 2] / 0.13)) - 0.03, 0.02) * (q[:, 2] > -0.12) * (q[:, 2] < 0.08)
    k.paint(seam, LILAC_DK, on=["bulb"])

    # --- the flower: a star of seven pointed blue petals, its face to the front --------------------------------------
    face = unit(0, -1, 0.22)
    for i, (ang, ln) in enumerate(((90, 0.132), (140, 0.12), (190, 0.135), (240, 0.12), (300, 0.11), (345, 0.13),
                                   (40, 0.12))):
        a = math.radians(ang)
        d = np.array([math.cos(a), 0.0, math.sin(a)])
        d = d - face * (d @ face)
        d = d + face * 0.12                           # a little cupped forward
        col = (BLUE, BLUE_LT)[i % 2]
        k.add(f"petal{i}", petal(FLOWER_C + np.array([0, 0.012, 0]), d, face, 0.02, ln, 0.034, 0.009), col, "head",
              fuse=False, surface="shell")
    k.add("heart", outward(ell(FLOWER_C + np.array([0, 0.012, 0]), (0.034, 0.016, 0.034))), BLUE_DK, "head",
          fuse=False, surface="shell")

    # --- the face: a pale cone pointing forward -----------------------------------------------------------------------
    CONE_C, CONE_R, CONE_L = FLOWER_C + np.array([0, -0.006, 0]), 0.046, 0.12
    cone(k, CONE_C, CONE_R, CONE_L, CONE_PALE, "head")

    k.build(voxel=VOXEL, smooth=4, band=0.005, wband=0.03)
    k.eye_states(cone_eyes(k, CONE_C, CONE_R, CONE_L, (0.018, 0.0135), CONE_PALE, up_angle=26, back=0.34))
    k.export(out)


if __name__ == "__main__":
    glimmet(*args())
