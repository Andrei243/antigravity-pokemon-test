# Arctozolt sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/arctozolt.py -- <the export's Arctozolt.glb> <out.glb>
#
# From the references: Dracozolt's yellow bird (dracozolt.py's `bird_head`, `bird_arm`, `zolt_eyes`) stuck in the top of
# a great frozen sea beast, the lower half by far the largest mass. The beast: a round upright egg of a body, as wide
# as two thirds of the whole height, under a thick cap of snow whose lower edge bulges in round lobes all round; under
# the cap a pale aqua belly in front and blue sides and back, darker low behind. Two blue flippers spread forward and
# out from its foot along the ground, their tips white with blue spots; a blue tail behind with a dark blue fin like a
# shark's rising back. The bird: only its thin yellow neck rises from the top of the snow, curving forward into the
# same crested head as Dracozolt's, its beak bent down; its eyes squeezed shut in tears, a blue tear on the cheek, an
# icicle hanging from the beak and frosted blue drool. Its two yellow arms reach out from under the snow at the front
# and hang their claws over the cap's edge. No wings show.
# Written to be grown from: arctovish.py takes the frozen body (`ice_body`, `flipper`, `tail_fin`).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, slab, srgb, inside, unit, args, sibling

dz = sibling("dracozolt")

SNOW = "#EEF4FA"
AQUA = "#B6E2E8"
BLUE = "#4A9CD8"
DEEP = "#2C76BE"
FIN = "#2A6CB0"
TEAR = "#6CB6EE"
ICE = "#E6F2FB"
DROOL = "#9CD2F2"

BODY_C, BODY_R = np.array([0.0, 0.03, 0.29]), np.array([0.215, 0.2, 0.25])
CAP_Z = 0.31                    # the snow cap's lower edge, at its middle
HEAD_C = np.array([0.0, -0.075, 0.675])
HEAD_G = 1.12


def flipper(k, root, tip, width, bone, color=BLUE, spots=True):
    """A flat flipper from root to tip (mirrored), its last third white with two blue spots."""
    root, tip = np.array(root, float), np.array(tip, float)
    d = tip - root
    L = np.linalg.norm(d); d /= L
    side = np.cross(d, (0, 0, 1.0)); side /= np.linalg.norm(side)
    pts = [root + d * L * t for t in np.linspace(0, 1, 6)]
    rad = [width * w for w in (0.8, 1.0, 1.0, 0.95, 0.8, 0.45)]
    k.add("flipper", tube(pts, rad), color, bone, mirror=True)
    # flattened: a second, flat ellipsoid gives it its broad face
    c = root + d * L * 0.55
    yaw = math.degrees(math.atan2(d[0], -d[1]))
    pitch = math.degrees(math.asin(np.clip(d[2], -1, 1)))
    k.add("fpaddle", ell(c, (width * 1.5, L * 0.5, width * 0.55), rot=(-pitch, 0, -yaw)), color, bone, mirror=True)
    tipc = root + d * L * 0.82

    def white(P, tipc=tipc, d=d):
        q = np.column_stack([np.abs(P[:, 0]), P[:, 1], P[:, 2]])
        return inside(((tipc - d * L * 0.12) - q) @ d, 0.004)
    k.paint(white, SNOW, on=["flipper", "fpaddle"])
    if spots:
        for j, off in enumerate((-0.35, 0.35)):
            at = root + d * L * 0.72 + side * width * off
            at[2] += width * 0.45
            k.decal(at, (0, 0, 1), (width * 0.28, width * 0.28), color, up=tuple(d), eps=0.003, rings=8, segs=32,
                    mirror=True)


def tail_fin(k, root, out_deg=50, h=0.17, color=FIN):
    """A shark's fin rising back from root, its plane turned out_deg toward +X so the game's cameras see its face."""
    t = math.radians(out_deg)
    ex = (math.sin(t), math.cos(t), 0.0)
    FO = [(0.0, -0.035), (0.08, -0.02), (0.15, 0.04), (0.2, h), (0.13, 0.07), (0.07, 0.035), (0.0, 0.035)]
    k.add("tailfin", slab(FO, root, ex, (0, 0, 1), 0.03), color, "tail", fuse=False)


def ice_body(k, C, R, cap_z, lobes=10, cap=True, lobe=0.075):
    """The frozen beast's egg of a body: snow over its top in round lobes, a pale aqua belly, blue sides and back."""
    C, R = np.array(C, float), np.array(R, float)
    k.add("ice", ell(C, R), BLUE, "body")
    if cap:
        k.add("snow", ell(C + np.array([0, 0, 0.08]), R * np.array([1.05, 1.05, 0.8])), SNOW, "body")
        for i in range(lobes):
            a = 2 * math.pi * (i + 0.5) / lobes
            rr = 0.94
            p = C + np.array([math.sin(a) * R[0] * rr, -math.cos(a) * R[1] * rr, cap_z - C[2] + 0.02 * math.cos(3 * a)])
            k.add(f"lobe{i}", ell(p, (lobe, lobe, lobe * 0.8)), SNOW, "body")

    def belly(P):
        q = (P - C) / R
        return inside(q[:, 1] + 0.35, 0.05) * inside(np.hypot(q[:, 0] / 0.75, (q[:, 2] + 0.25) / 0.85) - 1, 0.04)

    def low(P):
        q = (P - C) / R
        return inside(q[:, 2] + 0.35 - 0.5 * q[:, 1], 0.08)
    k.paint(low, DEEP, on=["ice"])
    k.paint(belly, AQUA, on=["ice"])


def arctozolt(src, out):
    k = Kit(src, coat="scales")

    ice_body(k, BODY_C, BODY_R, CAP_Z)
    # the flippers on the leg joints, spread forward and out along the ground
    flipper(k, (0.12, -0.06, 0.12), (0.25, -0.345, 0.03), 0.04, "leg{s}")
    k.add("tail", tube([(0, 0.18, 0.15), (0, 0.29, 0.11), (0, 0.37, 0.09)], [0.075, 0.05, 0.03]), BLUE, "tail")
    tail_fin(k, (0.0, 0.33, 0.1))

    # --- the bird: its neck from the top of the snow, its arms out from under the cap --------------------------------
    k.add("neck", tube([(0, 0.0, 0.5), (0, 0.008, 0.575), (0, -0.02, 0.635), (0, -0.06, 0.665)],
                       [0.04, 0.034, 0.032, 0.034]), dz.YELLOW, "head")
    dz.bird_arm(k, (0.12, -0.16, 0.33), (0.15, -0.225, 0.3), (0.12, -0.255, 0.255), G=1.1)
    h, SK, R = dz.bird_head(k, HEAD_C, G=HEAD_G, cheek=TEAR, tilt=16)
    # the icicle hanging from the beak, the frosted drool from the corner of the mouth
    k.add("icicle", hull([h((0.012, -0.11, -0.005)), h((-0.012, -0.11, -0.005)), h((0, -0.095, -0.004)),
                          h((0, -0.125, -0.006)), h((0.002, -0.112, -0.1))]), ICE, "head", fuse=False, surface="shell")
    k.add("drool", tube([h((0.03, -0.06, -0.03)), h((0.04, -0.07, -0.07)), h((0.036, -0.075, -0.1))],
                        [0.014, 0.016, 0.013]), DROOL, "head", fuse=False, surface="skin")
    lay = dz.zolt_eyes(k, h, SK, R, G=HEAD_G, kind="squeeze")

    k.build(voxel=0.007, smooth=4, band=0.006, wband=0.06)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    arctozolt(*args())
