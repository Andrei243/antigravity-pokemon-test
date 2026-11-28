# Pelireed sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/pelireed.py -- <the export's Pelireed.glb> <out.glb>
#
# The Carpathian Water starter's second stage (No. 10008), Pelicub grown (its feet, eyes, beak, palette and down
# imported from pelicub.py), from its picture (docs/art/reference/gemini/carpathia/10008-Pelireed.jpg) and its brief
# (CP-dex-1-plateau.md, 8): a young white pelican of the delta with a faint green-grey cast and a pale blue-grey belly;
# an oval body low on short legs and broad tan webbed feet; a thick white neck in an S to a head carried forward, a
# shock of reed blades standing up and back from its crown and hanging down its nape; a long pale yellow beak and
# under it a pouch striped in blue and cream like a little drum's skin; round brown eyes under a cocky half lid; one
# wing (the right, +X) raised beside the head, its long flight feathers reed green, the other folded along the
# flank with its green primaries running back over a fan of white tail feathers.
# Written to be grown from: buciumbill.py imports it (sibling("pelireed")) for the grown body, neck, wings, tail,
# pouch and eyes, with a configuration of its own.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, srgb, inside, smoothstep, unit, args, sibling

c_ = sibling("pelicub")
spike = c_.spike
quill = c_.p_.quill
frame_ell = c_.frame_ell
on_ell = c_.on_ell


class Cfg:
    """What a grown pelican of the line is: Pelireed's numbers; buciumbill.py copies and changes them."""
    g = 1.0                                     # the skeleton's scale against Pelireed's
    WHITE, SHADE, BEAK, POUCH, STRIPE, FOOT = "#F0F5F0", "#C6D4DC", "#ECD496", "#F3E3B8", "#5E8CC4", "#E9B868"
    REED, REED2 = "#76A766", "#A6C98C"           # the crest's two greens
    WINGTIP, MANTLE = "#6E9E62", None            # the flight feathers; a mantle over the back and wings (none)
    IRIS, PUPIL = "#6A3E2A", "#1E1410"
    TAIL, TAIL2 = WHITE, SHADE                   # the tail fan's two colours
    SLANT = 0.9                                 # the pouch's stripes: bands slanting down toward the tip
    VOXEL = 0.0068
    BC, BR = (0.0, 0.04, 0.21), (0.178, 0.22, 0.158)
    HC, HR = (0.0, -0.06, 0.6), (0.12, 0.126, 0.11)
    NECK = [(0, -0.075, 0.31), (0, -0.105, 0.41), (0, -0.075, 0.5), (0, -0.06, 0.56)]
    NECKR = [0.088, 0.072, 0.066, 0.07]
    POUCH_DEPTH, POUCH_W = 0.22, 0.072
    raised = True                               # the right wing raised beside the head (the picture's pose)
    EYEDIR = unit(0.6, -0.72, 0.3)
    EYE = (0.025, 0.025)
    LID = (0.42, -0.12)                         # the half lid's line: v = a + b u


# --- the grown body ------------------------------------------------------------------------------------------------
def body(k, C):
    g = C.g
    k.add("body", ell(np.array(C.BC) * g, np.array(C.BR) * g, rot=(-8, 0, 0)), C.WHITE, "body")
    k.add("chest", ell(np.array((0, -0.075, 0.26)) * g, np.array((0.13, 0.11, 0.12)) * g), C.WHITE, "body")
    k.add("neck", tube(C.NECK, C.NECKR), C.WHITE, "head")
    k.add("head", ell(C.HC, C.HR, rot=(-6, 0, 0)), C.WHITE, "head")


def shade(k, C):
    """The pale blue-grey of the belly, and (Buciumbill) the mantle over the back."""
    g = C.g
    bz = C.BC[2] * g
    k.paint(lambda P: inside(P[:, 2] - (bz - 0.05 * g), 0.03 * g), C.SHADE, on=["body", "chest"])


def legs(k, C):
    g = C.g
    c_.feet(k, 0.08 * g, (0.0, 0.022 * g), (0.012 * g, 0.105 * g), 3.3 * g, 0.024 * g, 0.024 * g, C.FOOT)


def folded_wing(k, C, s, side):
    """A wing folded along the flank: a broad mass, its long pointed flight feathers running back and up over the tail."""
    g = C.g
    sh = np.array([0.125 * s, -0.03, 0.33]) * g
    d = unit(0.1 * s, 1.0, -0.22)
    ch = unit(0, 0.25, 1.0)
    k.add(f"wing{side}", frame_ell(sh + d * 0.13 * g, (0.2 * g, 0.11 * g, 0.045 * g), d, ch), C.MANTLE or C.WHITE,
          f"wing{side}")
    base = sh + d * 0.16 * g
    for j, (dz, ln, w, up) in enumerate(((0.045, 0.27, 0.09, 0.3), (0.005, 0.29, 0.09, 0.18), (-0.035, 0.24, 0.08, 0.08))):
        b = base + np.array([0.03 * s, -0.01 * j, dz]) * g
        dd = unit(0.16 * s, 1.0, up)
        k.add(f"primary{side}{j}", spike(b, b + dd * ln * g, w * g, 0.034 * g, side=(0, -0.3, 1.0), at=0.35),
              C.WINGTIP, f"wing{side}", fuse=False, surface="fur")


def raised_wing(k, C):
    """
    The right wing raised beside the head, its face to the front: a white arm grown from the shoulder, the white
    coverts on it and five long pointed feathers fanned up and out, overlapping, reed green.
    """
    g = C.g
    sh = np.array([0.13, 0.02, 0.38]) * g
    ex = unit(0.25, 0.12, 0.96)                 # up the wing
    ez = unit(0.75, 0.6, -0.15)                 # across it, out and back toward its trailing edge
    elbow = sh + ex * 0.15 * g + ez * 0.02 * g
    k.add("wingarmR", tube([sh - ex * 0.03 * g, elbow], [0.065 * g, 0.045 * g]), C.WHITE, "wingR")
    n = np.cross(ex, ez)
    COV = [(-0.02, -0.035), (0.08, -0.05), (0.17, -0.03), (0.21, 0.03), (0.17, 0.11), (0.08, 0.12), (0.01, 0.06)]
    k.add("wingR", slab([(a * g, b * g) for a, b in COV], sh + n * 0.004 * g, ex, ez, 0.04 * g), C.WHITE, "wingR",
          fuse=False, surface="fur")
    for j, a in enumerate((-0.35, -0.05, 0.25, 0.6, 1.0)):
        base = sh + (ex * (0.15 - 0.012 * j) + ez * (0.0 + 0.03 * j)) * g
        dd = unit(*(ex + ez * a))
        ln = (0.2, 0.22, 0.21, 0.18, 0.15)[j]
        k.add(f"flightR{j}", spike(base, base + dd * ln * g, 0.08 * g, 0.03 * g, side=np.cross(n, dd), at=0.38),
              C.WINGTIP if j % 2 else C.REED, "wingR", fuse=False, surface="fur")


def tail(k, C):
    """A fan of white tail feathers turned up behind, under the wings' tips, their faces to the back camera."""
    g = C.g
    for j, yaw in enumerate((-28, -10, 10, 28)):
        y = math.radians(yaw)
        base = np.array([math.sin(y) * 0.04, 0.16, 0.25]) * g
        d = unit(math.sin(y) * 0.8, math.cos(y), 0.9)
        k.add(f"tail{j}", spike(base, base + d * (0.19 - 0.012 * abs(yaw) / 8) * g, 0.095 * g, 0.032 * g,
                                side=(math.cos(y), -math.sin(y), 0), at=0.45),
              C.TAIL if j in (1, 2) else C.TAIL2, "tail", fuse=False, surface="fur")


def reed_crest(k, C):
    """Reed blades standing up and back from the crown, a few hanging down the nape: thick flat hulls."""
    hc, hr = np.array(C.HC), np.array(C.HR)
    top = hc + np.array([0, 0.02, hr[2] * 0.75])
    blades = [(0.0, -0.25, 1.0, 0.15), (0.35, 0.05, 1.0, 0.14), (-0.35, 0.05, 1.0, 0.14), (0.15, 0.6, 0.85, 0.16),
              (-0.15, 0.6, 0.85, 0.16), (0.55, 0.55, 0.6, 0.12), (-0.55, 0.55, 0.6, 0.12), (0.0, 1.0, 0.5, 0.15)]
    for j, (dx, dy, dz, ln) in enumerate(blades):
        d = unit(dx, dy, dz)
        b = top + np.array([dx * 0.03, dy * 0.02, -0.005])
        k.add(f"reed{j}", spike(b, b + d * ln, 0.036, 0.026, side=np.cross(d, (0, 1.0, 0.2)), at=0.3),
              C.REED if j % 2 == 0 else C.REED2, "head", fuse=False, surface="leaf")
    for j, x in enumerate((-0.045, 0.0, 0.045)):
        b = hc + np.array([x, hr[1] * 0.75, hr[2] * 0.3])
        d = unit(x * 4, 0.75, -0.65)
        k.add(f"nape{j}", spike(b - d * 0.02, b + d * 0.13, 0.046, 0.028, side=np.cross(d, (0, 0, 1.0)), at=0.3),
              C.REED2 if j % 2 == 0 else C.REED, "head", fuse=False, surface="leaf")


def pouch(k, C, root, tip, depth, width, stripes=5, slant=0.9, name="pouch"):
    """
    The pouch under the beak, fused to the throat: a sack from root to tip hanging depth below the line, its top
    flat under the beak, striped across its length in two colours with a gentle wave.
    """
    root, tip = np.array(root, float), np.array(tip, float)
    L = np.linalg.norm(tip - root)
    d = (tip - root) / L
    c = (root + tip) / 2 - np.array([0, 0, depth * 0.55])
    def sack(V):
        V = V.copy()
        V[:, 2] = np.minimum(V[:, 2], 0.72)
        V[:, 2] -= 0.15 * np.clip(-V[:, 2], 0, 1) * (1 - np.abs(V[:, 1] + 0.1))
        f = smoothstep(0.0, 1.0, V[:, 1])            # narrowing and rising toward the tip, so its front is small
        V[:, 0] *= 1 - 0.45 * f
        V[:, 2] = np.where(V[:, 2] < 0, V[:, 2] * (1 - 0.55 * f), V[:, 2])
        return V
    up = np.array([0, 0, 1.0])
    ey = d; ex = np.cross(ey, up); ex /= np.linalg.norm(ex)
    ez = np.cross(ex, ey)
    V, F = ell((0, 0, 0), (width, L * 0.52, depth * 0.62), deform=sack)
    V = V @ np.stack([ex, ey, ez], axis=1).T + c
    k.add(name, (V, F), C.POUCH, "jaw", surface="shell")
    per = L / stripes
    def m(P):
        q = P - root
        along = q @ d + slant * (q @ ez) + 0.012 * np.sin(q[:, 0] * 60)
        return smoothstep(-0.18, 0.18, np.sin(2 * math.pi * along / per))
    k.paint(m, C.STRIPE, on=[name])


def eyes(k, C):
    E = on_ell(C.HC, C.HR, C.EYEDIR)
    look = c_.eye_round(C.IRIS, C.PUPIL, pupil_r=0.48, lid=C.WHITE, lid_at=C.LID, glint=(-0.3, 0.12, 0.22))
    return c_.eye_layer(k, E, C.EYEDIR, C.EYE, look, C.WHITE)


def pelireed(src, out_path, C=Cfg):
    k = Kit(src, coat="fur")
    body(k, C)
    shade(k, C)
    folded_wing(k, C, -1, "L")
    if C.raised:
        raised_wing(k, C)
    else:
        folded_wing(k, C, 1, "R")
    tail(k, C)
    reed_crest(k, C)
    hc, hr = np.array(C.HC), np.array(C.HR)
    root = hc + np.array([0, -hr[1] * 0.75, -0.02])
    tip = root + np.array([0, -0.34, -0.05])
    k.add("beak", c_.upper_beak(root, tip, 0.05, 0.022, bulb=0.22), C.BEAK, "head", fuse=False, surface="shell")
    pouch(k, C, root + np.array([0, 0.09, -0.06]), tip + np.array([0, 0.035, -0.012]), C.POUCH_DEPTH, C.POUCH_W, slant=C.SLANT)
    legs(k, C)
    k.build(voxel=C.VOXEL, band=0.006)
    c_.board(k)
    k.eye_states(eyes(k, C))
    k.export(out_path)


if __name__ == "__main__":
    pelireed(*args())
