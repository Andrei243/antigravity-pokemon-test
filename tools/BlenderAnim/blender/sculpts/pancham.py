# Pancham sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/pancham.py -- <the export's Pancham.glb> <out.glb>
# Written to be grown from: pangoro.py imports its frame (sibling("pancham")): the scowling eye in its dark patch, the
# spiky tuft on the crown, the round dark ears, the pointed tufts of fur, the leaf.
#
# The design, from the references: a panda cub with a great cream head half its height and wider than its body by
# two fifths; three cream spikes of fur on the crown; big round dark grey ears on the crown's corners; dark grey
# patches round the eyes, their top edge sloping down to the nose in a scowl, the eyes grey with a black pupil and a
# white glint; a small dark nose, a smirk of a mouth; a green leaf held in the mouth's corner on its left (-X); a pale
# pointed tuft out on each cheek; a small dark grey body with a lighter jagged mark on the chest, short arms carried
# forward and down, a cream round tail low behind; short thick legs.
# Reference ratios (3D picture): head 0.5 of the height (0.55 with the tuft), head width / body width 1.4, ear width
# / head width 0.3.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, outward, srgb, inside, unit, tri, polygon, eye_look, args, sibling

t = sibling("teddiursa")
sk = sibling("skiddo")

CREAM, DARK, PATCH, MARK = "#F2EDCE", "#4C4E58", "#3E3F48", "#8E919E"
NOSE, LEAF, IRIS_G = "#2A2A30", "#3AA84E", "#8C8E98"
INK, GLINT = srgb("#141418"), np.ones(3)


def tuft(k, root, spikes, colour, bone="head", tag="tuft"):
    """Pointed spikes of fur from root: each (direction, length, width, thick)."""
    for i, (d, L, W, T) in enumerate(spikes):
        sk.spike(k, f"{tag}{i}", root, d, L, W, T, bone, colour, side=(1, 0, 0), mid=0.3, belly=0.8)


def cheek(k, root, d, L, W, T, colour, bone="head"):
    """A pointed tuft out on each cheek, mirrored by hand."""
    for s in (1, -1):
        m = np.array([s, 1, 1.0])
        sk.spike(k, f"cheek{'R' if s > 0 else 'L'}", np.array(root) * m, np.array(d) * m, L, W, T, bone, colour,
                 side=(0, 0, 1), mid=0.25, belly=0.8)


def leaf(k, root, d, L, W, T, colour, bone="head", name="leaf"):
    """A leaf held in the mouth: a flat-faced plate from root along d, broad in its middle."""
    sk.spike(k, name, root, d, L, W, T, bone, colour, side=(0, 0, 1), surface="leaf", mid=0.5, belly=0.7)


def panda_eye(cut, iris=IRIS_G, iris_r=0.62, pupil_r=0.36, glint=(0.25, 0.3, 0.22)):
    """A round eye cut along its top by the scowl: grey iris, black pupil, a white glint (u outward, v up)."""
    iris = srgb(iris) if isinstance(iris, str) else iris
    def look(u, v):
        reach = np.array([cut(a) for a in np.arctan2(v, u)])
        c = np.tile(INK, (len(u), 1))
        d = np.hypot(u + 0.05, v + 0.08)
        c[d < iris_r] = iris
        c[d < pupil_r] = INK
        c[np.hypot(u - glint[0], v - glint[1]) < glint[2]] = GLINT
        c[np.hypot(u, v) / reach > 0.92] = INK
        return c
    return look


def scowl_eyes(k, E, de, half, patch_half, patch_col, cut, look, lid, up=(0, 0, 1), line_col=None):
    """
    The dark patch round each eye (in every state) and the lay function of the eyes laid on it. cut is the eye's
    outline sloping down toward the nose (u outward); the left eye's is mirrored by hand.
    """
    m = np.array([-1, 1, 1.0])
    PATCH_OUT = polygon([(-1.0, 0.15), (-0.45, 0.62), (0.4, 0.95), (1.0, 0.65), (1.05, -0.2), (0.6, -0.85),
                         (-0.2, -0.95), (-0.85, -0.55)])
    k.decal(E, de, patch_half, patch_col, eps=0.003, rings=14, segs=56, outline=PATCH_OUT, up=up)
    k.decal(E * m, de * m, patch_half, patch_col, eps=0.003, rings=14, segs=56,
            outline=lambda th: PATCH_OUT(math.pi - th), up=up * m)

    def lay(state):
        lk = eye_look(state, look, lid, INK if line_col is None else line_col, line=0.18)
        k.decal(E, de, half, lk, eps=0.0055, rings=20, segs=56, outline=cut, up=up)
        k.decal(E * m, de * m, half, lambda u, v: lk(-u, v), eps=0.0055, rings=20, segs=56,
                outline=lambda th: cut(math.pi - th), up=up * m)
    return lay


def brow(BA, BB):
    """An eye's outline cut straight along v = BA + BB u (u outward: a slope up toward the outer side scowls)."""
    def cut(th):
        s = math.sin(th) - BB * math.cos(th)
        return min(1.0, BA / s) if s > 1e-6 else 1.0
    return cut


def pancham(src, out_path):
    k = Kit(src, coat="fur")
    H = np.array([0, -0.025, 0.31])             # the head's middle (its bone is at 0, -0.011, 0.269)
    HR = np.array([0.132, 0.11, 0.118])
    B = np.array([0, 0.0, 0.135])               # the body's middle

    # --- the body: small, a pear, dark grey; a cream round tail low behind ---------------------------------------
    def pear(V):
        V = V.copy()
        s = 1 + 0.12 * np.clip(-V[:, 2], 0, 1) - 0.1 * np.clip(V[:, 2], 0, 1)
        V[:, 0] *= s; V[:, 1] *= s
        return V
    k.add("body", ell(B, (0.086, 0.075, 0.09), deform=pear), DARK, "body")
    k.add("tail", ell((0, 0.068, 0.075), (0.05, 0.032, 0.042)), CREAM, "body")

    # the lighter jagged mark on the chest: points rising from a band across the belly
    def chest(P):
        x, z = P[:, 0], P[:, 2]
        top = 0.168 + 0.045 * (1 - np.abs(tri(x / 0.05)))
        return (inside(z - top, 0.002) * inside(0.115 - z, 0.002) * inside(np.abs(x) - 0.06, 0.002)
                * (P[:, 1] < B[1] - 0.04))
    k.paint(chest, MARK, on=["body"])

    # --- short thick legs, the thighs fused into the body; round feet ------------------------------------------
    for s, bone in ((1, "legR"), (-1, "legL")):
        m = np.array([s, 1, 1.0])
        k.add("thigh" + bone[-1], ell(np.array((0.05, 0.0, 0.085)) * m, (0.05, 0.055, 0.055)), DARK, bone)
        k.add("foot" + bone[-1], ell(np.array((0.055, -0.02, 0.026)) * m, (0.042, 0.055, 0.028)), DARK, bone)

    # --- short arms carried forward and down, round hands before the belly ------------------------------------
    k.add("arm", tube([(0.07, -0.01, 0.235), (0.088, -0.05, 0.195), (0.082, -0.088, 0.165)], [0.034, 0.031, 0.029]),
          DARK, "arm{s}", mirror=True)
    k.add("hand", ell((0.078, -0.098, 0.155), (0.034, 0.03, 0.032)), DARK, "arm{s}", mirror=True)

    # --- the great head; the dark ears; the spiky tuft; the cheek tufts ----------------------------------------
    HEAD = ell(H, HR, deform=lambda V: V * (1 + 0.04 * np.clip(-V[:, 2], 0, 1))[:, None])
    k.add("head", HEAD, CREAM, "head")
    k.add("snout", ell(H + (0, -0.085, -0.04), (0.06, 0.04, 0.04)), CREAM, "head")
    k.add("ear", ell((0.105, 0.0, 0.405), (0.05, 0.03, 0.048), rot=(0, -30, 0)), DARK, "ear{s}", mirror=True)
    tuft(k, H + (0, -0.01, HR[2] - 0.02), [((0, 0.15, 1), 0.065, 0.02, 0.012), ((0.45, 0.1, 0.9), 0.05, 0.017, 0.011),
                                         ((-0.45, 0.1, 0.9), 0.05, 0.017, 0.011)], CREAM)
    cheek(k, H + (0.118, -0.03, -0.055), (1, 0.1, -0.15), 0.05, 0.018, 0.011, CREAM)

    # --- the nose, the smirk, the leaf in the mouth's corner on its left ----------------------------------------
    NC = t.hit(HEAD, H + (0, -1, -0.03), (0, 1, 0))
    k.decal(NC, unit(0, -1, -0.1), (0.016, 0.012), NOSE, eps=0.003, rings=8, segs=44,
            outline=polygon([(-1, 0.6), (1, 0.6), (0.3, -0.9), (-0.3, -0.9)]))
    MC = t.hit(HEAD, H + (-0.006, -1, -0.07), (0, 1, 0))
    CK, CC = srgb(PATCH), srgb(CREAM)
    def smirk(u, v):
        c = np.tile(CC, (len(u), 1))
        c[np.abs(v - (-0.2 + 0.55 * ((u + 0.15) ** 2)) + 0.0 * u) < 0.28] = CK
        return c
    k.decal(MC, unit(-0.05, -1, -0.3), (0.04, 0.014), smirk, eps=0.003, rings=8, segs=56)
    leaf(k, H + (-0.045, -0.094, -0.07), (-1, -0.45, -0.3), 0.07, 0.025, 0.007, LEAF)

    # --- the eyes in their dark patches, scowling --------------------------------------------------------------
    de = unit(0.5, -0.85, 0.05)
    E = t.hit(HEAD, H + (0.053, -1, 0.0), (0, 1, 0))
    cut = brow(0.45, 0.42)
    lay = scowl_eyes(k, E, de, (0.028, 0.028), (0.053, 0.042), PATCH, cut, panda_eye(cut), srgb(PATCH),
                    line_col=srgb("#C4C6D0"))

    k.build(voxel=0.0045, band=0.003, wband=0.04)
    k.eye_states(lay)
    k.export(out_path)


if __name__ == "__main__":
    pancham(*args())
