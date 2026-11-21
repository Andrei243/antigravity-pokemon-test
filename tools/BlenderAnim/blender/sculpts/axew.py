# Axew sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/axew.py -- <the export's Axew.glb> <out.glb>
# From the references: a small grey-green dragon child, its head a third of its height. A dark green cap over the
# top and back of the head rises into a tall pointed crest leaning back; the cap's dark runs down round each big eye
# (white, a red iris, a dark pupil, a white glint). A round paler snout with two nostrils, a flat pale tusk out of
# each corner of the mouth, sideways. A scalloped lime collar round the neck. A pear of a body with a paler belly,
# stubby arms forward and down with three little fingers, thick thighs fused into the hips, round feet, a short tail
# with two small spikes on its top.
#
# Written to be grown from (fraxure.py and haxorus.py import it with sibling("axew")): the line's red eye in its
# dark mask, its claws and tusks as kite-section hulls and its haunches are the functions below; the calls that
# build Axew are under __main__.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, inside, smoothstep, unit, polygon, eye_look, tri, args

# --- the line's colours ---------------------------------------------------------------------------------------------
BODY, PALE = "#93A57E", "#B8C59C"
CAP, MASK = "#4F6039", "#3A4729"
TUSK = "#D6DFBA"
LIME = "#93C43C"
RED_EYE = srgb("#D23A33")
INK, WHITE = srgb("#17191A"), np.array([1.0, 1.0, 1.0])


def on_ell(c, r, d):
    """The point of an ellipsoid (centre c, radii r, unturned) along the direction d from its centre."""
    d = unit(*d)
    t = 1.0 / math.sqrt(((d / np.array(r)) ** 2).sum())
    return np.array(c, float) + d * t


def spike(base, d, length, width, up=(0, 1.0, 0), flat=0.4, blunt=0.0):
    """A claw, a tusk or a spike: a flat-faced point from base along d, its section a kite `width` wide across `up`
    and flat * width thick along it; blunt > 0 ends it in an edge that share of the width wide, not a point."""
    base, d = np.array(base, float), unit(*d)
    up = np.array(up, float); up = up - d * (up @ d); up /= np.linalg.norm(up)
    side = np.cross(d, up)
    b = base - d * width * 0.3
    return hull([b + side * width / 2, b - side * width / 2, b + up * width * flat, b - up * width * flat,
                 base + d * length * 0.5 + side * width * 0.3, base + d * length * 0.5 - side * width * 0.3,
                 base + d * length * 0.5 + up * width * flat * 0.55, base + d * length * 0.5 - up * width * flat * 0.55,
                 base + d * length + side * width * blunt / 2, base + d * length - side * width * blunt / 2])


def red_eye(iris=RED_EYE, iris_r=0.72, pupil_r=0.37, pupil_c=(0.0, 0.0), outline=None):
    """The line's eye: a white, a big red iris, a dark pupil a little toward the snout, a white glint up and back
    (+u runs toward the back on a sideways patch). outline: a cut of the eye's edge (a brow), as a patch's."""
    def eye(u, v):
        reach = np.array([outline(t) for t in np.arctan2(v, u)]) if outline else 1.0
        c = np.tile(WHITE, (len(u), 1))
        d = np.sqrt((u - pupil_c[0]) ** 2 + (v - pupil_c[1]) ** 2)
        c[d < iris_r] = iris
        c[d < pupil_r] = INK
        g = np.sqrt((u - 0.2) ** 2 + (v - 0.3) ** 2)
        c[g < 0.17] = WHITE
        return c
    return eye


def eye_pair(k, E, de, half, look_open, lid, outline=None, line=0.16, eps=0.005, ink=None):
    """Lays the eyes for a state: the right as it is, the left with its picture (and outline) turned over."""
    m = np.array([-1, 1, 1.0])
    def eyes(state):
        look = eye_look(state, look_open, lid, INK if ink is None else ink, line=line)
        k.decal(E, de, half, look, eps=eps, rings=20, segs=56, outline=outline)
        k.decal(E * m, de * m, half, lambda u, v: look(-u, v), eps=eps, rings=20, segs=56,
                outline=(lambda th: outline(math.pi - th)) if outline else None)
    return eyes


def axew(src, out):
    k = Kit(src, coat="scales")

    # The body: a pear, wide and low at the hips, a paler belly
    k.add("body", ell((0, 0.0, 0.095), (0.064, 0.057, 0.07)), BODY, "body")
    k.add("chest", ell((0, -0.008, 0.14), (0.052, 0.046, 0.04)), BODY, "body")
    k.decal((0, -0.056, 0.085), unit(0, -1, 0.05), (0.036, 0.042), PALE, eps=0.003, rings=10, segs=48)

    # The legs: thick thighs fused into the hips, a stub of shin, round feet with three toes
    k.add("thigh", ell((0.042, -0.006, 0.06), (0.037, 0.042, 0.042)), BODY, "leg{s}", mirror=True)
    k.add("shin", tube([(0.045, -0.01, 0.04), (0.047, -0.012, 0.022)], [0.027, 0.026]), BODY, "leg{s}", mirror=True)
    k.add("foot", ell((0.048, -0.02, 0.013), (0.03, 0.04, 0.014)), BODY, "leg{s}", mirror=True)
    for j, dx in enumerate((-0.016, 0.0, 0.016)):
        k.add(f"toe{j}", ell((0.048 + dx, -0.055, 0.011), (0.009, 0.01, 0.009)), BODY, "leg{s}", mirror=True)

    # The arms: stubby, forward and down, three little fingers
    k.add("arm", tube([(0.048, -0.018, 0.15), (0.068, -0.042, 0.13), (0.078, -0.062, 0.117)], [0.02, 0.018, 0.017]),
          BODY, "arm{s}", mirror=True)
    k.add("hand", ell((0.08, -0.068, 0.113), (0.018, 0.017, 0.016)), BODY, "arm{s}", mirror=True)
    for j, (dx, dz) in enumerate(((-0.008, -0.004), (0.004, -0.008), (0.014, -0.002))):
        p = np.array((0.08 + dx, -0.082, 0.11 + dz))
        k.add(f"finger{j}", tube([p, p + (0.0, -0.008, -0.004)], [0.0065, 0.006]), BODY, "arm{s}", mirror=True)

    # The tail: short, back and down, two small spikes on its top
    k.add("tail", tube([(0, 0.04, 0.075), (0, 0.09, 0.048), (0, 0.128, 0.03)], [0.034, 0.021, 0.008]), BODY, "tail")
    for j, (y, z) in enumerate(((0.078, 0.073), (0.106, 0.054))):
        k.add(f"tailspike{j}", spike((0, y, z), unit(0, 0.5, 1), 0.018 - 0.004 * j, 0.016, up=(1, 0, 0), flat=0.35),
              BODY, "tail", fuse=False)

    # The head: big and round, a paler round snout forward
    H, HR = np.array([0.0, -0.016, 0.215]), np.array([0.077, 0.072, 0.067])
    k.add("head", ell(H, HR), BODY, "head")
    k.add("snout", ell((0, -0.07, 0.192), (0.052, 0.046, 0.041)), PALE, "head")
    k.add("neck", ell((0, -0.006, 0.165), (0.05, 0.047, 0.03)), BODY, "head")

    # The crest: a tall point out of the crown, broad from front to back and flattened side to side, leaning back
    k.add("crest", tube([(0, 0.0, 0.25), (0, 0.03, 0.305), (0, 0.06, 0.35), (0, 0.083, 0.383)],
                        [0.062, 0.042, 0.022, 0.005]), CAP, "head")
    CV, CF = k.parts[-1]["V"], k.parts[-1]["F"]
    CV[:, 0] *= 0.58

    # The cap: dark over the top and back of the head, its front edge over the eyes, down the nape behind
    def cap(P):
        q = P - H
        edge = 0.012 - np.where(q[:, 1] > 0, 0.9, 0.3) * q[:, 1] - 0.15 * np.abs(q[:, 0]) * (q[:, 1] < 0)
        top = inside(edge - q[:, 2], 0.003)
        return top * inside(np.linalg.norm(q / (HR * 1.25), axis=1) - 1, 0.003)
    k.paint(cap, CAP, on=["head", "crest", "neck"])
    k.paint(lambda P: inside(P[:, 2] - 0.258, 0.003) * inside(np.abs(P[:, 0]) - 0.04, 0.003), CAP, on=["crest"])

    # The collar: a scalloped lime ruff round the neck, its lower edge in points
    k.add("collar", ell((0, -0.016, 0.16), (0.07, 0.066, 0.02)), LIME, "body")
    def ruff(P):
        a = np.arctan2(P[:, 0], -(P[:, 1] + 0.016))
        return inside(P[:, 2] - (0.149 + 0.007 * tri(a * 9 / (2 * math.pi))), 0.0025)
    k.paint(ruff, BODY, on=["collar"])

    # The tusks: flat pale blades out of the mouth's corners, sideways and a little forward
    for s, side in ((1, "R"), (-1, "L")):
        k.add(f"tusk{side}", spike((0.042 * s, -0.085, 0.183), unit(1.0 * s, -0.3, -0.05), 0.062, 0.036, up=(0, -0.6, 1),
                                   flat=0.3, blunt=0.55), TUSK, "head", fuse=False, surface="shell")

    # The nostrils and the mouth
    k.decal((0.012, -0.115, 0.2), unit(0.2, -1, 0.3), (0.0045, 0.006), MASK, eps=0.003, rings=4, segs=20, mirror=True)
    k.decal((0, -0.109, 0.174), unit(0, -1, -0.45), (0.022, 0.0035), MASK, eps=0.003, rings=4, segs=32)

    # The eyes: big, in the dark of the cap's sides
    de = unit(0.5, -0.84, 0.12)
    E = on_ell(H, HR, de)
    dm = unit(0.6, -0.73, 0.24)
    k.decal(on_ell(H, HR, dm), dm, (0.036, 0.036), MASK, eps=0.003, rings=12, segs=48, mirror=True)
    eyes = eye_pair(k, E, de, (0.024, 0.029), red_eye(), srgb(MASK), eps=0.0055)

    k.build(voxel=0.004, smooth=4, band=0.005)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    axew(*args())
