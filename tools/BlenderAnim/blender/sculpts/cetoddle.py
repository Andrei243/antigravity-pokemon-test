# Cetoddle sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/cetoddle.py -- <the export's Cetoddle.glb> <out.glb>
#
# From the references: a little white whale calf standing on two short legs. One round ball of a body that is its head;
# its front a huge open mouth, dark inside, a pink tongue and a white lower lip rising in the middle; one small horn on
# the crown toward the front; black oval eyes with a white glint at the mouth's upper corners, each under a pink shadow
# that sits over its upper lid. Two long flipper arms, white, the tips pink pads; a short tail rising behind with a fluke
# of two lobes, each with a pink spot; a pink spot on the side. Short stubby legs, white-grey. (The art flings the arms
# wide and up: here they are carried forward and down. The pink soles are hidden when it stands and are left out.)
#
# Written to be grown from (Cetitan: sibling("cetoddle")): the eye, the flipper with its pink pad, the fluke, the horn
# and the leg are functions that take their bone.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, lathe, srgb, inside, unit, eye_look, polygon, args

WHITE, GREY, PINK, INK = "#F5F3F6", "#D2CCD4", "#EE9CC6", "#1A1820"
MOUTH, TONGUE, HORN = "#5A4652", "#E89AA2", "#E4E6EE"


def at_surface(C, R, d):
    """The point of an ellipsoid (centre C, radii R) in a direction d from its centre."""
    d = np.array(d, float)
    return np.array(C, float) + d / np.linalg.norm(d / np.array(R, float))


def flat(c, d, half, up=(0, 0, 1.0), roll=0.0, deform=None):
    """A flattened ellipsoid lying along d: half sizes (along, across, thick), its face toward up, rolled about d."""
    d = unit(*d)
    s = np.cross(d, up); s /= np.linalg.norm(s)
    n = np.cross(s, d)
    r = math.radians(roll)
    s, n = s * math.cos(r) + n * math.sin(r), n * math.cos(r) - s * math.sin(r)
    V, F = ell((0, 0, 0), (half[1], half[0], half[2]), deform=deform)
    return V @ np.stack([s, d, n]) + np.array(c, float), F


def eye_open(u, v):
    """A black oval eye with a white glint up and out (u outward, v up)."""
    c = np.tile(srgb(INK), (len(u), 1))
    c[((u - 0.12) / 0.34) ** 2 + ((v - 0.36) / 0.3) ** 2 < 1] = (1.0, 1.0, 1.0)
    return c


def eyes(k, spots, half, state, lid=WHITE, open_look=None, eps=0.0018):
    """The two eyes as patches, the left one's colour function mirrored so u is outward on both."""
    look = eye_look(state, open_look or eye_open, srgb(lid), srgb(INK), line=0.17)
    for at, d, sx in spots:
        col = look if sx > 0 else (lambda u, v, look=look: look(-u, v))
        k.decal(at, d, half, col, eps=eps, rings=20, segs=56)


def flipper(k, name, root, d, half, bone, up=(0, 0, 1.0), roll=0.0, pad=0.32, color=WHITE, mirror=True):
    """A long flipper arm fused at root along d, rounded at its end, the end a pink pad on both faces."""
    def shape(V):
        V = V.copy(); V[:, 0] *= 0.82 + 0.25 * np.clip(V[:, 1] + 0.2, 0, 1); return V
    dd = unit(*d)
    c = np.array(root, float) + dd * half[0] * 0.92
    k.add(name, flat(c, d, half, up, roll, deform=shape), color, bone, mirror=mirror)
    tip = c + dd * half[0] * 0.78
    for sx in ((1, -1) if mirror else (1,)):
        t = tip * np.array([sx, 1, 1])
        k.paint(lambda P, t=t: inside(np.linalg.norm(P - t, axis=1) - half[0] * pad, 0.003), PINK, on=[name])
    return tip


def fluke(k, root, d_lobe, half, bone, spot=True, color=WHITE, name="fluke", up=(0, -0.6, 1.0)):
    """A tail fluke of two lobes from root, each along d_lobe (given for +X), with a pink spot on each."""
    for sx in (1, -1):
        d = unit(d_lobe[0] * sx, d_lobe[1], d_lobe[2])
        def lobe(V):
            V = V.copy(); V[:, 0] *= 1.0 - 0.45 * np.clip(V[:, 1], 0, 1) ** 1.5; return V
        c = np.array(root, float) + d * half[0] * 0.9
        nm = f"{name}{'RL'[sx < 0]}"
        k.add(nm, flat(c, d, half, up=up, deform=lobe), color, bone)
        if spot:
            sc = c + d * half[0] * 0.25
            k.paint(lambda P, sc=sc: inside(np.linalg.norm((P - sc) / (half[0] * 0.5, half[0] * 0.5, half[0] * 0.5),
                                                          axis=1) - 1, 0.06), PINK, on=[nm])


def horn(k, name, root, d, length, r, bone, color=HORN, mirror=False):
    """A solid pointed horn of ice from root along d."""
    d = unit(*d)
    k.add(name, lathe(np.array(root, float) - d * r * 0.5, d,
                      [(0, 0), (r, 0), (r * 0.86, length * 0.35), (r * 0.5, length * 0.72), (0, length)], seg=28),
          color, bone, fuse=False, mirror=mirror, surface="shell")


def leg(k, hip, foot, r_top, r_foot, bone, color=WHITE):
    """A short thick leg from the hip to a round foot flat on the ground."""
    f = np.array(foot, float)
    k.add("leg", tube([hip, (f[0], f[1], r_foot * 1.0)], [r_top, r_foot * 0.9], seg=24), color, bone, mirror=True)
    k.add("foot", ell((f[0], f[1] - r_foot * 0.25, r_foot * 0.75), (r_foot * 1.05, r_foot * 1.25, r_foot * 0.75)), color,
          bone, mirror=True)


B, BR = np.array([0.0, 0.0, 0.232]), np.array([0.156, 0.15, 0.15])


def cetoddle(src, out):
    k = Kit(src, coat="fur")
    k.add("body", ell(B, BR, sub=5), WHITE, "body")
    # The legs: short, thick, a little grey, the belly close above them
    leg(k, (0.072, -0.005, 0.12), (0.078, -0.02, 0.0), 0.058, 0.05, "leg{s}")
    k.paint(lambda P: inside(P[:, 2] - 0.07, 0.012), GREY, on=["leg", "foot"])
    # The flipper arms: long, from the upper sides, carried forward and down, the hands in front of the belly
    flipper(k, "arm", (0.11, -0.05, 0.26), (0.14, -0.84, -0.52), (0.1, 0.042, 0.017), "arm{s}", up=(0, -0.3, 1.0),
            roll=40)
    # The tail: a short stem rising behind and a fluke of two lobes, each with a pink spot
    k.add("tailstem", tube([(0, 0.11, 0.2), (0, 0.18, 0.21), (0, 0.22, 0.24)], [0.07, 0.042, 0.03], seg=24), WHITE,
          "tail")
    fluke(k, (0, 0.22, 0.245), (0.6, 0.45, 0.66), (0.075, 0.04, 0.013), "tail")
    # A pink spot high on each side and the pink shadow over each eye (in every state)
    for sx in (1, -1):
        d = unit(0.92 * sx, 0.2, 0.15)
        k.decal(at_surface(B, BR, d), d, (0.024, 0.024), PINK, eps=0.002, rings=12, segs=44)
    SHADOW = [(-1, -0.5), (-0.6, 0.5), (0.2, 1), (1, 0.6), (1, -0.4), (0.3, -0.9), (-0.5, -1)]   # u outward on +X: high at the outer side
    ED = [unit(0.5 * sx, -0.74, 0.44) for sx in (1, -1)]
    for d, sx in zip(ED, (1, -1)):
        sd = unit(0.5 * sx, -0.62, 0.61)
        k.decal(at_surface(B, BR, sd), sd, (0.036, 0.019), PINK, up=(0, 0, 1), eps=0.0012, rings=14, segs=48,
                outline=polygon([(sx * a, b) for a, b in SHADOW]))
    # The great open mouth over the lower front: dark, a pink tongue, the white lower lip rising in the middle
    def mouth(u, v):
        c = np.tile(srgb(MOUTH), (len(u), 1))
        c[(u / 0.78) ** 2 + ((v + 0.62) / 0.6) ** 2 < 1] = srgb(TONGUE)
        c[(u / 0.3) ** 2 + ((v + 1.0) / 0.42) ** 2 < 1] = srgb(WHITE)
        return c
    md = unit(0, -0.96, -0.28)
    k.decal(at_surface(B, BR, md), md, (0.092, 0.08), mouth, eps=0.0018, rings=32, segs=96,
            outline=lambda th: 1.0 if math.sin(th) < 0 else 1.0 - 0.18 * math.sin(th) ** 2)
    # The one horn on the crown toward the front, pointing up and a little forward
    hd = unit(0, -0.4, 0.92)
    horn(k, "horn", at_surface(B, BR, hd), unit(0, -0.3, 1.0), 0.066, 0.024, "body")
    k.build(voxel=0.0042, smooth=4, band=0.005, wband=0.03)
    E = [(at_surface(B, BR, d), d, sx) for d, sx in zip(ED, (1, -1))]
    k.eye_states(lambda st: eyes(k, E, (0.023, 0.031), st, eps=0.0026))
    k.export(out)


if __name__ == "__main__":
    cetoddle(*args())
