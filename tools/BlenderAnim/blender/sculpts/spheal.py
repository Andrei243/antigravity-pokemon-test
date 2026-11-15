# Spheal sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/spheal.py -- <the export's Spheal.glb> <out.glb>
#
# Written to be grown from (Sealeo, Walrein: sibling("spheal")). From the references: one round ball of a body, blue
# over the back and cream over the whole lower front, the line between them running from under the muzzle down the
# sides to the ground behind; the face on the ball itself: a broad blue muzzle standing forward from the upper front,
# two white fangs pointing down from its underside; small round black eyes with a white glint above the muzzle's sides;
# two small round ears on the crown, a little apart; white oval spots on the back and the upper sides; two small cream
# flippers flat on the ground at the front, pointing out; a blue fish's tail behind, two lobes rising off the ground.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, lathe, srgb, inside, smoothstep, unit, eye_look, outward, args

BLUE, CREAM, WHITE, INK, EARIN = "#5FA6E2", "#F4ECBE", "#FAFBFC", "#15161E", "#4A8FD0"


def at_surface(C, R, d):
    """The point of an ellipsoid (centre C, radii R) in a direction d from its centre."""
    d = np.array(d, float)
    return np.array(C, float) + d / np.linalg.norm(d / np.array(R, float))


def eye(u, v):
    """A round black eye with a white glint up and out (u outward, v up)."""
    c = np.tile(srgb(INK), (len(u), 1))
    c[((u - 0.18) / 0.34) ** 2 + ((v - 0.34) / 0.32) ** 2 < 1] = (1.0, 1.0, 1.0)
    return c


def eyes(k, spots, half, state, lid, open_look=None):
    """The two eyes as patches, the left one's colour function mirrored so u is outward on both."""
    look = eye_look(state, open_look or eye, srgb(lid), srgb(INK), line=0.17)
    for at, d, sx in spots:
        col = look if sx > 0 else (lambda u, v, look=look: look(-u, v))
        k.decal(at, d, half, col, eps=0.003, rings=20, segs=56)


def fang(k, name, root, d, length, r, bone, color=None):
    """A solid pointed fang or tusk from root along d (mirrored)."""
    d = unit(*d)
    k.add(name, lathe(np.array(root, float) - d * r * 0.4, d,
                      [(0, 0), (r, 0), (r * 0.9, length * 0.35), (r * 0.55, length * 0.75), (0, length)], seg=24),
          color or WHITE, bone, fuse=False, mirror=True, surface="shell")


def flipper(c, half, rot, toes=3, amp=0.1):
    """A flipper: a thick flat oval whose far end (local +x) is lobed into round toes."""
    def f(V):
        V = V.copy()
        th = np.arctan2(V[:, 1], V[:, 0])
        far = np.clip(V[:, 0], 0, 1) ** 2
        V[:, :2] *= (1 + amp * far * np.abs(np.cos(toes * th)))[:, None]
        return V
    return ell(c, half, rot=rot, deform=f)


def fluke(k, root, back, spread, lift, size, color, bone):
    """A fish's tail of two lobes from root, each going back and out to its side and rising (faces up and back)."""
    for sx in (1, -1):
        d = unit(spread * sx, back, lift)
        c = np.array(root, float) + d * size[0] * 0.85
        # the lobe's long axis (local x) along d, its width across, its thin side the plate's normal
        s = np.cross(d, (0, 0, 1.0)); s /= np.linalg.norm(s)
        n = np.cross(d, s)
        V, F = flipper((0, 0, 0), size, (0, 0, 0), toes=2, amp=0.12)
        V = V @ np.stack([d, s, n])
        k.add(f"fluke{'RL'[sx < 0]}", (V + c, F), color, bone)


def spots(k, C, R, dirs, color=WHITE):
    """Oval spots on the ball, mirrored: (direction, half sizes) each."""
    for d, half in dirs:
        d = unit(*d)
        up = (0, 0, 1)
        k.decal(at_surface(C, R, d), d, half, color, up=up, eps=0.002, rings=12, segs=44, mirror=True)


def spheal(src, out):
    k = Kit(src, coat="fur")
    B = np.array([0, 0.0, 0.148])
    BR = np.array([0.162, 0.152, 0.158])
    # The ball, a little squarer at its seat; the face's front on the head bone, flush with it
    def seat(V):
        V = V.copy()
        V[:, 2] = np.where(V[:, 2] < -0.7, -0.7 + (V[:, 2] + 0.7) * 0.55, V[:, 2])
        return V
    k.add("body", ell(B, BR, deform=seat), BLUE, "body")
    k.add("face", ell(B + (0, -0.04, 0.03), BR * (0.93, 0.93, 0.9)), BLUE, "head")
    # The muzzle: two broad lobes standing forward from the upper front, overhanging the cream
    M = B + np.array([0, -0.15, 0.04])
    for sx in (1, -1):
        k.add(f"muzzle{'RL'[sx < 0]}", ell(M + (0.03 * sx, -0.012, 0), (0.055, 0.05, 0.04), rot=(-8, 0, 0)), BLUE,
              "head")
    k.add("muzzlemid", ell(M + (0, 0.0, 0.003), (0.05, 0.05, 0.038)), BLUE, "head")
    fang(k, "fang", M + (0.042, -0.035, -0.03), (0.08, -0.25, -1), 0.034, 0.0135, "head")
    # Small round ears on the crown, a little apart and forward
    for sx in (1, -1):
        ec = at_surface(B, BR, unit(0.42 * sx, -0.12, 0.9)) + np.array([0.0, 0, -0.005])
        k.add(f"ear{'RL'[sx < 0]}", ell(ec, (0.034, 0.028, 0.03)), BLUE, "head")
    # Flippers flat on the ground at the front, pointing out and forward
    k.add("flipper", flipper((0.115, -0.125, 0.02), (0.05, 0.028, 0.015), (0, -8, -28), toes=2, amp=0.06), CREAM,
          "leg{s}", mirror=True)
    k.add("flipperroot", ell((0.08, -0.095, 0.03), (0.04, 0.03, 0.02), rot=(0, 0, -28)), CREAM, "leg{s}", mirror=True)
    # The tail: a short stem behind and two lobes rising off the ground
    k.add("tailstem", tube([(0, 0.11, 0.07), (0, 0.17, 0.05), (0, 0.205, 0.05)], [0.06, 0.042, 0.032]), BLUE, "tail")
    fluke(k, (0, 0.2, 0.05), 1.0, 0.55, 0.55, (0.058, 0.04, 0.017), BLUE, "tail")
    # The cream: the whole lower front, its line from under the muzzle down the sides to the ground behind
    def cream(P):
        n = (P - B) / BR
        n /= np.linalg.norm(n, axis=1, keepdims=True) + 1e-9
        return inside(0.43 * n[:, 1] + 0.9 * n[:, 2] + 0.16, 0.012)
    k.paint(cream, CREAM, on=["body", "face"])
    k.paint(lambda P: np.ones(len(P)), CREAM, on=["flipper"])
    # the inner bump of each ear, a little darker, facing forward and up
    for sx in (1, -1):
        ic = at_surface(B, BR, unit(0.42 * sx, -0.12, 0.9)) + np.array([0, -0.02, 0.005])
        k.paint(lambda P, ic=ic: inside(np.linalg.norm((P - ic) / (0.018, 0.012, 0.02), axis=1) - 1, 0.15), EARIN,
                on=["ear"])
    spots(k, B, BR, [((0.72, 0.45, 0.5), (0.034, 0.044)), ((0.9, 0.3, -0.02), (0.026, 0.032)),
                     ((0.6, 0.72, -0.12), (0.02, 0.026))])
    k.build(voxel=0.0045, smooth=4, band=0.006)
    E = [(at_surface(B, BR, d), d, sx) for sx in (1, -1) for d in [unit(0.4 * sx, -0.78, 0.48)]]
    k.eye_states(lambda st: eyes(k, E, (0.022, 0.025), st, BLUE))
    k.export(out)


if __name__ == "__main__":
    spheal(*args())
