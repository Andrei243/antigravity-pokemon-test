# Naclstack sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown from
# nacli.py (its colours, salt blocks, banded rock, sparkle and framed eyes):
#   blender --background --factory-startup --python sculpts/naclstack.py -- <the export's Naclstack.glb> <out.glb>
#
# The design, from the references: a low beast of salt blocks. Its head is a white cube at the front with a crack down
# its top, and on its face two eyes like upturned L's in orange-yellow with dark frames, each with its upright stroke on
# the inner side and its bar along the top running outward. Its lower half sits in a brown box that runs back as the
# body; on the body's back half lies a big dark brown rock, lumpy, greyer on top, with a white diamond on its side and a
# dark seam round its rear. Under the body a white-grey block, and four great cube feet, pale pink above and white
# below with a wavy line between, each with a small brown stone like a claw at its outer corner.
# Where the pictures overruled the brief: the four legs ARE the cubes (no legs under them), the brown is a box round the
# head and body rather than plates, and the eyes are L-shaped bars, not Nacli's straight ones.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, srgb, unit, eye_look, args, hull, polygon, inside, boxy, sibling

n = sibling("nacli")

BROWN_BOX, BROWN_TOP = "#B07A60", "#C59074"
ROCK, ROCK_TOP = "#6E5244", "#8F7466"
FOOT_PINK, FOOT_WHITE = "#EBD5CA", "#F7F5F2"
CLAW = "#B27556"

FACE_C, FACE_R = (0.0, -0.095, 0.254), (0.09, 0.098, 0.08)
FACE_FRONT = FACE_C[1] - FACE_R[1]
LEG = [(0.094, -0.115, "leg"), (0.094, 0.158, "hind")]
LEG_HALF = (0.06, 0.058, 0.063)


def legs(k):
    for x, y, b in LEG:
        k.add(b + "_", ell((x, y, LEG_HALF[2]), LEG_HALF, rot=(0, 0, 3 if b == "leg" else -3), deform=boxy(9), sub=5),
              FOOT_PINK, b + "{s}", mirror=True)
        # the claw: a small brown stone at the outer corner, front for the forelegs and back for the hind
        cy = y - LEG_HALF[1] if b == "leg" else y + LEG_HALF[1]
        sy = -1 if b == "leg" else 1
        pts = [(x + 0.045, cy + sy * 0.012, 0.0), (x + 0.068, cy - sy * 0.01, 0.0), (x + 0.03, cy - sy * 0.02, 0.0),
               (x + 0.052, cy + sy * 0.004, 0.045), (x + 0.06, cy - sy * 0.012, 0.012), (x + 0.036, cy + sy * 0.0, 0.01)]
        k.add(b + "claw", hull(pts), CLAW, b + "{s}", fuse=False, mirror=True, surface="shell")
    # the white below a wavy line on each foot
    def low(P):
        th = np.arctan2(P[:, 1], P[:, 0]) * 3 + P[:, 0] * 60 + P[:, 1] * 40
        edge = 0.048 + 0.008 * np.sin(4 * th) + 0.004 * np.sin(9 * th + 1)
        return inside(P[:, 2] - edge, 0.003) * inside(P[:, 2] - 0.14, 0.003)
    k.paint(low, FOOT_WHITE, on=["leg_", "hind_"])


def naclstack(src, dest):
    k = Kit(src, coat="shell")
    legs(k)
    # the head: a white cube (fused, for its eyes and crack)
    k.add("face", ell(FACE_C, FACE_R, deform=boxy(9), sub=5), n.SALT, "head")
    # the brown box round the head's lower half, running back as the body
    k.add("box", n.box((0, 0.0, 0.17), (0.102, 0.205, 0.05), cham=0.08), BROWN_BOX, "body", fuse=False, surface="shell")
    k.add("boxtop", n.box((0, -0.115, 0.214), (0.1, 0.1, 0.012), cham=0.25), BROWN_TOP, "body", fuse=False, surface="shell")
    # the white-grey block under the body
    k.add("belly", n.box((0, 0.02, 0.1), (0.07, 0.16, 0.04)), n.SALT_GREY, "body", fuse=False, surface="shell")
    # the rock on the back, greyer on top, a dark seam round its rear and a white diamond on its side
    k.add("rock", ell((0, 0.105, 0.236), (0.104, 0.124, 0.086), rot=(-8, 0, 6),
                      deform=n.rock_shape(11, amount=0.6, flat=-0.7)), ROCK, "body")
    on_rock = lambda P: inside(-P[:, 1] - 0.02, 0.003)
    k.paint(lambda P: inside(0.272 - P[:, 2], 0.004) * on_rock(P), ROCK_TOP, on=["rock"])
    k.paint(lambda P: inside(np.abs(P[:, 2] - 0.2 - 0.008 * np.sin(P[:, 0] * 90)) - 0.009, 0.003)
            * inside(0.16 - P[:, 1], 0.004), n.DEEP, on=["rock"])
    for s in (1, -1):
        n.sparkle(k, (s * 0.09, 0.08, 0.235), (s, 0, 0.2), (0.012, 0.018))
    # the crack down the head's top and front
    crack = polygon([(0.0, 1.0), (0.5, 0.3), (0.12, -0.2), (0.6, -1.0), (-0.3, -0.25), (0.0, 0.2), (-0.5, 0.9)])
    k.decal((0.004, FACE_FRONT, 0.312), (0, -1, 0), (0.008, 0.017), n.FRAME, eps=0.0015, outline=crack, rings=8, segs=48)
    k.decal((0.004, -0.13, FACE_C[2] + FACE_R[2]), (0, 0, 1), (0.007, 0.028), n.FRAME, up=(0, 1, 0), eps=0.0015,
            outline=crack, rings=8, segs=48)
    k.build(voxel=0.0036, smooth=3, band=0.004, wband=0.03)

    # the eyes: an upturned L each, its upright on the inner side, its bar running outward along the top
    W, H = 0.0118, 0.0118              # the patch's units: the stroke is 2 units wide
    CY = 0.282                          # the patch's middle: the corner square, top inner
    CX = 0.024
    def ell_outline(sign):
        pts = [(-1, 1), (3.2, 1), (3.2, -1), (1, -1), (1, -4.4), (-1, -4.4)]
        if sign < 0:
            pts = [(-a, b) for a, b in pts][::-1]
        return polygon(pts)

    def ell_open(sign):
        def look(u, v):
            u = u * sign
            t = np.clip((v + 4.4) / 5.4, 0, 1)[:, None]
            c = n.CO[None] * (1 - t) + n.CL[None] * t
            inner = ((u > -0.55) & (u < 0.55) & (v > -3.95) & (v < 0.55)) | \
                    ((u > -0.55) & (u < 2.75) & (v > -0.55) & (v < 0.55))
            c[~inner] = n.CF
            return c
        return look

    def ell_state(state, sign):
        o = ell_open(sign)
        if state == "Open":
            return o
        lid, ink = srgb(n.SALT), n.CF
        def look(u, v):
            uu = u * sign
            # the eye's own frame, then a lid line through the bar (u mapped to -1..1 along the bar)
            t = (uu - 1.1) / 2.1
            c = np.tile(lid, (len(u), 1))
            if state == "Fierce":
                c = o(u, v)
                edge = 0.15 - 0.5 * t
                c[(v > edge)] = lid
                c[np.abs(v - edge) < 0.42] = ink
                return c
            vv = v / 1.0
            if state == "Shut":
                c[np.abs(vv - (-0.1 - 0.5 * (1 - t * t))) < 0.45] = ink
            elif state == "Smile":
                c[np.abs(vv - (-0.6 + 0.8 * (1 - t * t))) < 0.45] = ink
            else:
                c[np.abs(np.abs(vv) - 0.7 * (1 - t) / 2) < 0.4] = ink
            c[(np.abs(t) > 1.1)] = lid
            return c
        return look

    def lay(state):
        for sign in (1, -1):
            k.decal((sign * CX, FACE_FRONT, CY), (0, -1, 0), (W, H), ell_state(state, sign), eps=0.0018,
                    outline=ell_outline(sign), rings=24, segs=72)
    k.eye_states(lay)
    k.export(dest)


if __name__ == "__main__":
    naclstack(*args())
