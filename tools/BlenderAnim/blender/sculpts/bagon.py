# Bagon sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/bagon.py -- <the export's Bagon.glb> <out.glb>
# From the references: a small blue dragon child standing upright, its head half its height. The head IS a great grey
# dome of rock: three rounded lobes running from over the brow back over the crown and down the nape in a tongue to
# the shoulders, the blue face running up into it under its front edge. A round blue snout forward, the lower jaw a
# cream band under it, a white fang standing up from each corner of the jaw; angry eyes (white, a black pupil toward
# the snout) cut straight by the dome's edge sloping down to the snout; a yellow round ear with a darker middle behind
# each eye. A pear of a body, a cream wedge on the lower belly, stubby arms forward and down, thick thighs fused into
# the hips, short feet with three round toes, a short tail down behind.
#
# Written to be grown from (shelgon.py and salamence.py import it with sibling("bagon")): the line's colours, its
# angry eye cut by a brow, its claws and fangs and its grooved grey rock are the functions below; the calls that build
# Bagon are under __main__.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, inside, smoothstep, unit, polygon, eye_look, args

# --- the line's colours ---------------------------------------------------------------------------------------------
BLUE = "#5FB2DC"
ROCK, GROOVE = "#C9CACE", "#8E9096"
CREAM = "#F2D77E"
EAR, EAR_MID = "#F2CC4A", "#B88E1E"
FANG = "#F6F6F2"
INK, WHITE = srgb("#15161C"), np.array([1.0, 1.0, 1.0])


def on_ell(c, r, d):
    """The point of an ellipsoid (centre c, radii r, unturned) along the direction d from its centre."""
    d = unit(*d)
    t = 1.0 / math.sqrt(((d / np.array(r)) ** 2).sum())
    return np.array(c, float) + d * t


def fang(base, d, length, width, up=(0, 1.0, 0)):
    """A fang or claw: a flat-faced point from base along d, its section a kite wide across `up`."""
    base, d = np.array(base, float), unit(*d)
    up = np.array(up, float); up = up - d * (up @ d); up /= np.linalg.norm(up)
    side = np.cross(d, up)
    b = base - d * 0.01
    return hull([b + side * width / 2, b - side * width / 2, b + up * width * 0.4, b - up * width * 0.4,
                 base + d * length * 0.5 + side * width * 0.28, base + d * length * 0.5 - side * width * 0.28,
                 base + d * length])


def brow_cut(BA, BB):
    """An eye's outline cut straight along the brow v = BA + BB u (u toward the back): its reach at each angle."""
    def cut(th):
        s = math.sin(th) - BB * math.cos(th)
        return min(1.0, BA / s) if s > 1e-6 else 1.0
    return cut


def angry_eye(cut, iris=None, rim=0.8, pupil_c=(-0.2, -0.1), pupil_r=0.46, iris_r=0.62, white=WHITE):
    """An eye under a brow: a black rim along its outline, its white, a black pupil toward the snout (an iris round
    the pupil when one is given)."""
    def eye(u, v):
        reach = np.array([cut(t) for t in np.arctan2(v, u)])
        rho = np.sqrt(u * u + v * v) / reach
        c = np.tile(INK, (len(u), 1))
        c[rho < rim] = white
        d = np.sqrt((u - pupil_c[0]) ** 2 + (v - pupil_c[1]) ** 2)
        if iris is not None:
            c[(d < iris_r) & (rho < rim)] = iris
        c[(d < pupil_r) & (rho < rim)] = INK
        return c
    return eye


def eyes_under_brow(k, E, de, half, look_open, cut, lid, line=0.16):
    """Lays the eyes for a state: the right as it is, the left with its picture and outline turned over."""
    m = np.array([-1, 1, 1.0])
    def eyes(state):
        look = eye_look(state, look_open, lid, INK, line=line)
        k.decal(E, de, half, look, eps=0.005, rings=20, segs=56, outline=cut)
        k.decal(E * m, de * m, half, lambda u, v: look(-u, v), eps=0.005, rings=20, segs=56,
                outline=lambda th: cut(math.pi - th))
    return eyes


def groove(P, c, a, w, depth_dir=None):
    """A painted groove: points within w of the plane through c square to a (a line round a dome between lobes)."""
    return inside(np.abs((P - np.array(c)) @ unit(*a)) - w, 0.003)


def bagon(src, out):
    k = Kit(src, coat="scales")

    # The body: a pear, broad at the hips, the belly low
    k.add("body", ell((0, 0.0, 0.17), (0.115, 0.1, 0.12)), BLUE, "body")
    k.add("chest", ell((0, -0.02, 0.25), (0.085, 0.08, 0.06)), BLUE, "body")
    # The cream wedge on the lower belly, pointing up
    k.decal((0, -0.105, 0.13), unit(0, -1, 0.05), (0.055, 0.075), CREAM, eps=0.004,
            outline=polygon([(-1, -0.85), (1, -0.85), (0.08, 1), (-0.08, 1)]), rings=12, segs=60)

    # The head: a blue core, the round snout forward, the cream lower jaw under it
    H, HR = np.array([0.0, -0.035, 0.355]), np.array([0.1, 0.1, 0.09])
    k.add("head", ell(H, HR), BLUE, "head")
    k.add("snout", ell((0, -0.13, 0.33), (0.078, 0.085, 0.062), rot=(-6, 0, 0)), BLUE, "head")
    k.add("jaw", ell((0, -0.11, 0.282), (0.07, 0.085, 0.03), rot=(4, 0, 0)), CREAM, "head")
    k.add("neck", ell((0, -0.01, 0.285), (0.085, 0.08, 0.06)), BLUE, "head")

    # The dome of rock: three lobes from over the brow back over the crown, the outer two turned out, and a tongue down
    # the nape. The face runs up into it from the front; from behind it is all rock.
    k.add("dome", ell((0, -0.02, 0.4), (0.118, 0.14, 0.088), rot=(-6, 0, 0)), ROCK, "head", surface="shell")
    k.add("lobe0", ell((0, -0.01, 0.445), (0.05, 0.14, 0.06), rot=(-14, 0, 0)), ROCK, "head", surface="shell")
    k.add("lobe1", ell((0.072, 0.0, 0.43), (0.05, 0.135, 0.058), rot=(-14, -22, 0)), ROCK, "head", surface="shell",
          mirror=True)
    k.add("nape", ell((0, 0.095, 0.3), (0.078, 0.05, 0.1), rot=(10, 0, 0)), ROCK, "head", surface="shell")
    k.add("napetop", ell((0, 0.075, 0.37), (0.1, 0.07, 0.07)), ROCK, "head", surface="shell")
    # The grooves between the lobes
    k.paint(lambda P: (groove(P, (0.038, 0, 0), (1, 0, -0.25), 0.004) + groove(P, (-0.038, 0, 0), (1, 0, 0.25),
                                                                                 0.004)) *
            (P[:, 2] > 0.4) * (P[:, 1] < 0.09), GROOVE, on=["dome", "lobe"])

    # The fangs: white points standing up from the jaw's corners, outside the upper lip
    for s, side in ((1, "R"), (-1, "L")):
        k.add(f"fang{side}", fang((0.055 * s, -0.175, 0.285), unit(0.12 * s, -0.25, 1), 0.05, 0.026), FANG, "head",
              fuse=False, surface="shell")

    # The arms: stubby, forward and down
    k.add("arm", tube([(0.075, -0.03, 0.235), (0.11, -0.07, 0.21), (0.125, -0.1, 0.19)], [0.034, 0.03, 0.028]), BLUE,
          "arm{s}", mirror=True)
    k.add("hand", ell((0.128, -0.11, 0.185), (0.03, 0.03, 0.026)), BLUE, "arm{s}", mirror=True)

    # The legs: thick thighs fused into the hips, short shins, broad feet with three round toes
    k.add("thigh", ell((0.085, -0.01, 0.115), (0.07, 0.08, 0.08)), BLUE, "leg{s}", mirror=True)
    k.add("shin", tube([(0.09, -0.01, 0.08), (0.095, -0.015, 0.04)], [0.05, 0.046]), BLUE, "leg{s}", mirror=True)
    k.add("foot", ell((0.095, -0.045, 0.025), (0.05, 0.07, 0.026)), BLUE, "leg{s}", mirror=True)
    for j, dx in enumerate((-0.03, 0.0, 0.03)):
        k.add(f"toe{j}", ell((0.095 + dx, -0.108, 0.02), (0.017, 0.02, 0.016)), BLUE, "leg{s}", mirror=True)

    # The tail: short, down and back
    k.add("tail", tube([(0, 0.07, 0.14), (0, 0.13, 0.09), (0, 0.17, 0.05)], [0.05, 0.035, 0.018]), BLUE, "tail")

    # The ears: a yellow disc behind each eye, its middle darker
    ED = unit(1, 0.05, 0.05)
    EP = on_ell(H, HR, ED)
    k.decal(EP, ED, (0.026, 0.028), EAR, eps=0.004, rings=8, segs=40, mirror=True)
    k.decal(EP, ED, (0.011, 0.012), EAR_MID, eps=0.0065, rings=5, segs=24, mirror=True)

    # The mouth: a dark line where the snout meets the cream jaw, along the front
    k.decal((0, -0.205, 0.305), unit(0, -1, -0.2), (0.05, 0.006), "#2A3A4A", eps=0.004, rings=4, segs=40)

    # The eyes: on the face's sides under the dome's front edge, which slopes down toward the snout
    de = unit(0.62, -0.74, 0.2)
    E = on_ell(H, HR, de) + np.array([0, -0.004, -0.008])
    cut = brow_cut(0.3, 0.45)
    eyes = eyes_under_brow(k, E, de, (0.034, 0.036), angry_eye(cut, pupil_c=(-0.22, -0.2), pupil_r=0.4), cut, srgb(BLUE))

    k.build(voxel=0.0055, smooth=4, band=0.004)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    bagon(*args())
