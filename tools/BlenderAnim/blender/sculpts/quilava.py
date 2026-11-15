# Quilava sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/quilava.py -- <the export's Quilava.glb> <out.glb>
# Cyndaquil grown tall and slim, standing upright (the pictures stand it on two legs): a long body like a bottle, wide
# and heavy at the foot where the haunches are fused in, narrowing to a short thick neck; dark blue-green over the
# crown, the snout's top and all down the back, cream in front along one crisp line. The head carries a pointed snout
# with a small mouth under its tip, open red eyes cut along the top by the teal brow sloping down to the snout, and two
# small pointed ears red within. Thin arms hang in front of the belly; short broad feet. Fire bursts from the crown, a
# crest of thick tongues swept up and back, and from the rump, a fan streaming back and out. Built by Cyndaquil's own
# functions (its hood, flames and claws); its eyes are the line's open eye, which Typhlosion takes from here.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, hull, srgb, inside, smoothstep, unit, polygon, eye_look, args, sibling

cq = sibling("cyndaquil")
TEAL, CREAM = cq.TEAL, cq.CREAM
INK, WHITE = cq.INK, np.array([1.0, 1.0, 1.0])
RED = srgb("#C42A2E")
EAR_RED = "#D8424A"
MOUTH = "#8E2A30"


def back_mask(zs, ys, top, edge=0.0035):
    """Teal behind a line drawn up the back in the side view (y at each height z), up to the height top."""
    zs, ys = np.array(zs, float), np.array(ys, float)
    def mask(P):
        return inside(np.interp(P[:, 2], zs, ys) - P[:, 1], edge) * inside(P[:, 2] - top, edge)
    return mask


# --- the line's open eye: white, a red iris, a black pupil, its top cut by the brow -------------------------------
def brow_cut(BA, BB):
    """An eye's outline cut straight along the brow v = BA + BB u (u outward): its reach at each angle."""
    def cut(th):
        s = math.sin(th) - BB * math.cos(th)
        return min(1.0, BA / s) if s > 1e-6 else 1.0
    return cut


def fierce_eye(iris, cut, rim=0.84, iris_c=(-0.12, -0.05), iris_r=0.58, pupil_r=0.27):
    """An eye under a brow: a dark rim along the cut outline, white, a red iris and a pupil toward the nose."""
    def eye(u, v):
        reach = np.array([cut(t) for t in np.arctan2(v, u)])
        rho = np.sqrt(u * u + v * v) / reach
        c = np.tile(INK, (len(u), 1))
        c[rho < rim] = WHITE
        d = np.sqrt((u - iris_c[0]) ** 2 + (v - iris_c[1]) ** 2)
        c[(d < iris_r) & (rho < rim)] = iris
        c[(d < pupil_r) & (rho < rim)] = INK
        c[(np.sqrt((u - iris_c[0] + 0.18) ** 2 + (v - iris_c[1] - 0.22) ** 2) < 0.15) & (rho < rim)] = WHITE
        return c
    return eye


def eyes_under_brow(k, E, de, half, look_open, cut, lid, line=0.18):
    """Lays the two eyes for a state: the right as it is, the left with its picture and outline turned over."""
    m = np.array([-1, 1, 1.0])
    def eyes(state):
        look = eye_look(state, look_open, lid, INK, line=line)
        k.decal(E, de, half, look, eps=0.006, rings=20, segs=56, outline=cut)
        k.decal(E * m, de * m, half, lambda u, v: look(-u, v), eps=0.006, rings=20, segs=56,
                outline=lambda th: cut(math.pi - th))
    return eyes


def ear(k, base, d, length, width, bone="head"):
    """A small pointed ear: a teal solid, its red lining a thinner solid set into its front face."""
    d = unit(*d)
    base = np.array(base, float)
    fwd = np.array([0, -1.0, 0]); fwd -= d * (fwd @ d); fwd /= np.linalg.norm(fwd)
    a = np.cross(d, fwd)
    tip = base + d * length
    k.add("ear", hull([base + a * width, base - a * width, base + fwd * width * 0.9, base - fwd * width * 0.9,
                       tip]), TEAL, bone, fuse=False, mirror=True, surface="fur")
    lb = base + fwd * width * 0.45 + d * length * 0.12
    k.add("earin", hull([lb + a * width * 0.6, lb - a * width * 0.6, lb + fwd * width * 0.22,
                         lb + d * length * 0.68 + fwd * width * 0.06, lb - fwd * width * 0.05]), EAR_RED, bone,
          fuse=False, mirror=True)


# --- Quilava ----------------------------------------------------------------------------------------------------------
H, HR = np.array([0.0, 0.005, 0.535]), np.array([0.088, 0.092, 0.079])
SNOUT = [(0, -0.055, 0.525), (0, -0.12, 0.51), (0, -0.165, 0.495)]


def quilava(src, out):
    k = Kit(src, coat="fur")

    # The body: a bottle, heavy at its foot, narrowing up to a short thick neck under the head
    k.add("base", ell((0, 0.01, 0.13), (0.125, 0.112, 0.125)), CREAM, "body")
    k.add("torso", ell((0, 0.0, 0.29), (0.098, 0.088, 0.16), rot=(-6, 0, 0)), CREAM, "body")
    k.add("neck", ell((0, -0.005, 0.43), (0.072, 0.072, 0.085)), CREAM, "head")

    # The head: a crown, a pointed snout reaching forward and a little down
    k.add("head", ell(H, HR), TEAL, "head")
    k.add("jaw", ell((0, -0.045, 0.502), (0.07, 0.078, 0.052)), CREAM, "head")
    k.add("snout", tube(SNOUT, [0.056, 0.04, 0.025]), CREAM, "head")

    # The ears: small, pointed, set high on the crown
    ear(k, (0.05, 0.025, 0.595), (0.4, 0.3, 1), 0.066, 0.032)

    # The arms: thin, hanging in front of the belly, small paws with three short fingers
    k.add("arm", tube([(0.072, -0.035, 0.37), (0.082, -0.07, 0.31), (0.07, -0.098, 0.26)], [0.033, 0.03, 0.027]),
          CREAM, "arm{s}", mirror=True)
    k.add("paw", ell((0.066, -0.104, 0.245), (0.03, 0.028, 0.03)), CREAM, "arm{s}", mirror=True)
    for j, dx in enumerate((-0.014, 0.0, 0.014)):
        k.add(f"finger{j}", tube([(0.066 + dx, -0.115, 0.232), (0.066 + dx * 1.2, -0.122, 0.214)], [0.011, 0.01]),
              CREAM, "arm{s}", mirror=True)

    # The legs: haunches fused into the body's foot, short broad feet with three small toes
    k.add("haunch", ell((0.075, 0.0, 0.1), (0.07, 0.085, 0.09)), CREAM, "leg{s}", mirror=True)
    k.add("foot", ell((0.078, -0.045, 0.022), (0.052, 0.072, 0.024)), CREAM, "leg{s}", mirror=True)
    for j, dx in enumerate((-0.022, 0.0, 0.022)):
        k.add(f"toe{j}", ell((0.078 + dx, -0.11, 0.018), (0.014, 0.016, 0.014)), CREAM, "leg{s}", mirror=True)

    # The teal over the crown and the snout's top, and all down the back
    head = cq.hood([-0.3, -0.16, -0.1, -0.065, -0.025, 0.03, 0.08, 0.4],
                   [0.51, 0.514, 0.533, 0.552, 0.57, 0.512, 0.42, 0.42], below=0.4)
    back = back_mask([0.05, 0.12, 0.25, 0.4, 0.47, 0.6], [0.11, 0.07, 0.045, 0.03, 0.035, 0.035], 0.47)
    teal = lambda P: np.maximum(head(P), back(P))
    k.paint(teal, TEAL)
    k.paint(lambda P: 1 - teal(P), CREAM)

    # The fire: a crest from the crown swept up and back, and a fan from the rump streaming back and out
    f = cq.Flames()
    C = np.array([0, -0.005, 0.595])
    for d, L, w, side, fl in [((0, -0.1, 1), 0.13, 0.06, (1, 0, 0), 0.2),
                              ((0, 0.75, 1), 0.19, 0.075, (1, 0, 0), 0.3),
                              ((0, 1, 0.45), 0.17, 0.065, (1, 0, 0), 0.3),
                              ((0.55, 0.5, 0.9), 0.14, 0.055, (0, 1, 0.3), 0.25),
                              ((-0.55, 0.5, 0.9), 0.14, 0.055, (0, 1, 0.3), 0.25)]:
        f.flame(C + np.array(d) * np.array([0.03, 0, 0]), d, L, w, "flame", side, flick=fl)
    R = np.array([0, 0.1, 0.22])
    for d, L, w, side, fl in [((0, 1, 0.75), 0.22, 0.08, (1, 0, 0), 0.3),
                              ((0, 1, 0.15), 0.21, 0.075, (1, 0, 0), 0.3),
                              ((0.65, 0.8, 0.55), 0.18, 0.065, (0, 1, 0.3), 0.25),
                              ((-0.65, 0.8, 0.55), 0.18, 0.065, (0, 1, 0.3), 0.25),
                              ((0.7, 0.9, -0.05), 0.15, 0.06, (0, 1, 0.5), 0.25),
                              ((-0.7, 0.9, -0.05), 0.15, 0.06, (0, 1, 0.5), 0.25)]:
        f.flame(R + np.array(d) * np.array([0.05, 0, 0]), d, L, w, "tail", side, flick=fl)
    f.add(k)

    # The mouth: a small open one under the snout's tip
    k.decal((0, -0.163, 0.477), unit(0, -0.75, -0.65), (0.024, 0.012), MOUTH, eps=0.003, rings=6, segs=32)
    # The nose's dark tip
    k.decal((0, -0.188, 0.496), (0, -1, 0.15), (0.01, 0.008), cq.NOSE, eps=0.003, rings=4, segs=20)

    # The eyes: open and red, high on the sides of the face, the teal brow cutting their tops down toward the snout
    cut = brow_cut(0.14, 0.45)
    de = unit(0.6, -0.74, 0.18)
    E = cq.on_ell(H + (0, -0.008, -0.004), HR, de)
    eyes = eyes_under_brow(k, E, de, (0.033, 0.033), fierce_eye(RED, cut), cut, srgb(CREAM))

    k.build(voxel=0.006, smooth=4, band=0.006)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    quilava(*args())
