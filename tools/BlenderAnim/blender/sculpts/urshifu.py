# Urshifu (Single Strike Style) sculpted from nothing on the skeleton and clips of the game's own export of it
# (sculptkit.py), grown from Kubfu's script (kubfu.py, imported as sibling("kubfu")): the same black eye with its
# glint under a heavy black brow, the same yellow muzzle with its black nose and frown, the same pointed tufts.
#   blender --background --factory-startup --python sculpts/urshifu.py -- <the export's Urshifu.glb> <out.glb>
#
# The design, from the references: a great grey bear of a fighter in a wide crouching stance; a head carried forward,
# grey in the face with a hood of black spiky fur over its crown and back, heavy black brows over small fierce eyes,
# a yellow muzzle standing forward; a long ribbon of white fur streaming up and back off the crown (the art's
# headband of fur), branching at its end; huge grey shoulders and arms (raised to the face in the art, carried
# forward and down here) with yellow fists; a white chest split by a black zigzag down its middle, framed in black fur
# that runs from the hood down the front into a long black apron of fur hanging between the legs to near the ground,
# its hem in points; black jagged fur flaring off the hips; huge round grey thighs, dark shins, a white band over each
# dark foot, three yellow claws.
# Reference ratios (3D picture, the streamer left out): head 0.25 of the height (made a fifth larger: 0.3), shoulder
# width / height 0.7, thigh width / height 0.27, fist / head width 0.55.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, sheet, slab, ring_band, outward, srgb, inside, unit, tri, args, sibling

kb = sibling("kubfu")
hit, sk = kb.hit, kb.sk

GREY, BLACK, WHITE, YELLOW = "#6E6F7A", "#262B2A", "#F2F2EF", "#EBD33A"
SHIN, STREAM_ROOT = "#30353A", "#B8BAC0"


def urshifu(src, out_path):
    k = Kit(src, coat="fur")
    H = np.array([0.0, -0.085, 0.8])          # the head's middle (its bone is at 0, -0.03, 0.7)
    HR = np.array([0.122, 0.115, 0.122])

    # --- the trunk: a deep chest under huge shoulders, a heavy belly ------------------------------------------
    CHEST = ell((0, -0.015, 0.6), (0.2, 0.15, 0.13))
    k.add("chest", CHEST, GREY, "body")
    k.add("belly", ell((0, 0.0, 0.46), (0.17, 0.14, 0.14)), GREY, "body")
    k.add("neck", ell((0, -0.04, 0.69), (0.11, 0.1, 0.07)), GREY, "body")

    # the white chest, split by a black zigzag down its middle and framed in black fur
    def front(P):
        return P[:, 1] < -0.04
    def white(P):
        x, z = np.abs(P[:, 0]), P[:, 2]
        top = 0.68 - 0.25 * x
        bot = 0.47 + 0.6 * x
        return inside(z - top, 0.003) * inside(bot - z, 0.003) * inside(x - 0.15, 0.003) * front(P)
    k.paint(lambda P: inside(np.abs(P[:, 0]) - 0.185, 0.003) * inside(P[:, 2] - 0.73, 0.003)
            * inside(0.4 - P[:, 2], 0.003) * front(P), BLACK, on=["chest", "belly", "neck"])
    k.paint(white, WHITE, on=["chest", "belly"])
    def zig(P):
        x, z = np.abs(P[:, 0]), P[:, 2]
        w = 0.012 + 0.022 * (1 - np.abs(tri((z - 0.5) / 0.07)))
        return inside(x - w, 0.0025) * inside(z - 0.66, 0.003) * front(P)
    k.paint(zig, BLACK, on=["chest", "belly"])

    # --- huge shoulders and arms carried forward and down, elbows bent, yellow fists before the belly ------------
    for s, bone in ((1, "armR"), (-1, "armL")):
        m = np.array([s, 1, 1.0])
        k.add("shoulder" + bone[-1], ell(np.array((0.2, 0.0, 0.635)) * m, (0.11, 0.11, 0.1)), GREY, bone)
        k.add("arm" + bone[-1], tube([np.array(p) * m for p in [(0.2, 0.0, 0.62), (0.265, -0.07, 0.52),
                                                                 (0.2, -0.2, 0.465)]], [0.095, 0.08, 0.064]),
              GREY, bone)
        FC = np.array((0.165, -0.255, 0.45)) * m
        k.add("fist" + bone[-1], ell(FC, (0.064, 0.06, 0.062)), YELLOW, bone)
        for i, dz in enumerate((0.03, 0.0, -0.03)):
            k.add(f"knuckle{bone[-1]}{i}", ell(FC + np.array([0.012 * s, -0.052, dz]), (0.03, 0.02, 0.016)),
                  YELLOW, bone)

    # --- huge round thighs in a wide stance, dark shins, a white band over the dark feet, yellow claws ------------
    for s, bone in ((1, "legR"), (-1, "legL")):
        m = np.array([s, 1, 1.0])
        k.add("thigh" + bone[-1], ell(np.array((0.16, 0.0, 0.31)) * m, (0.14, 0.15, 0.155)), GREY, bone)
        k.add("shin" + bone[-1], tube([np.array(p) * m for p in [(0.21, -0.02, 0.2), (0.245, -0.02, 0.1),
                                                                  (0.255, -0.02, 0.06)]], [0.085, 0.075, 0.07]),
              SHIN, bone)
        F = np.array((0.26, -0.05, 0.036)) * m
        kb.clawed_foot(k, bone, F, (0.075, 0.095, 0.038), s, SHIN, YELLOW, 0.06, 0.024, 0, tag="claw")
        k.add("cuff" + bone[-1], ring_band(np.array((0.255, -0.025, 0.085)) * m, (0, 0, 1), 0.074, 0.032,
                                           thick=0.012), WHITE, bone, fuse=False)
        # the dark shin's top in points over the thigh
        K = np.array((0.215, -0.02, 0.2)) * m
        def knee(P, K=K):
            a = np.arctan2(P[:, 1] - K[1], P[:, 0] - K[0])
            top = 0.21 + 0.03 * (1 - np.abs(tri(a / 1.1)))
            return inside(P[:, 2] - top, 0.003) * inside(np.hypot(P[:, 0] - K[0], P[:, 1] - K[1]) - 0.11, 0.003)
        k.paint(knee, SHIN, on=["thigh" + bone[-1], "shin" + bone[-1]])
        # black jagged fur flaring off the hips, out and down
        for i, (o, d, L) in enumerate([((0.26, 0.02, 0.38), (1, 0.3, -0.25), 0.16),
                                      ((0.24, 0.09, 0.3), (0.8, 0.6, -0.55), 0.15),
                                      ((0.17, 0.12, 0.38), (0.4, 1, -0.2), 0.15),
                                      ((0.27, -0.03, 0.26), (0.9, -0.1, -0.7), 0.12)]):
            sk.spike(k, f"hipfur{bone[-1]}{i}", np.array(o) * m, np.array(d) * m, L, 0.055, 0.024, bone, BLACK,
                     mid=0.3, belly=0.8)

    # --- the long black apron of fur hanging between the legs, its hem in points ----------------------------------
    hem = []
    xs = np.linspace(0.11, -0.11, 9)
    for i, x in enumerate(xs):
        hem.append((x, 0.07 + (0.0 if i % 2 == 0 else 0.06)))
    jags = [(0.15, 0.39), (0.115, 0.34), (0.155, 0.27), (0.11, 0.21), (0.135, 0.14)]
    outline = ([(-0.13, 0.47), (0.0, 0.44), (0.13, 0.47)] + jags + hem
               + [(-x, z) for x, z in jags[::-1]])
    k.add("apron", slab(outline, (0, -0.175, 0.0), (1, 0, 0), unit(0, 0.07, 1), 0.026), BLACK, "body",
          fuse=False)
    k.add("aprontop", ell((0, -0.12, 0.43), (0.11, 0.05, 0.06)), BLACK, "body")

    # --- the head: grey in the face, a hood of black spiky fur over the crown and back ---------------------------
    HEAD = ell(H, HR)
    k.add("head", HEAD, GREY, "head")
    k.paint(lambda P: inside(-(P[:, 1] - (H[1] - 0.04)) - 1.1 * (P[:, 2] - (H[2] + 0.045)), 0.004)
            * inside(0.64 - P[:, 2], 0.004) * inside(np.abs(P[:, 0]) - 0.19, 0.004), BLACK, on=["head", "neck"])
    kb.spikes(k, H, [((0.0, 0.0, 0.115), (0.1, 0.35, 1), 0.11, 0.045, 0.02),
                     ((-0.05, 0.03, 0.1), (-0.45, 0.45, 1), 0.085, 0.04, 0.019)], BLACK, tag="crown")
    kb.spikes(k, H, [((0.09, 0.02, 0.075), (0.9, 0.35, 0.75), 0.08, 0.04, 0.018),
                     ((0.115, 0.03, 0.01), (1, 0.4, 0.15), 0.075, 0.038, 0.018),
                     ((0.07, 0.1, 0.03), (0.45, 1, 0.3), 0.08, 0.04, 0.018),
                     ((0.06, 0.11, -0.05), (0.4, 1, -0.3), 0.08, 0.04, 0.018),
                     ((0.11, 0.04, -0.06), (0.9, 0.4, -0.5), 0.07, 0.035, 0.017)], BLACK, tag="hood", mirror=True)

    # the yellow muzzle standing forward and a little down, its black nose and frown
    kb.MUZZLE = YELLOW
    MZ = H + (0, -0.112, -0.045)
    kb.muzzle_face(k, MZ, (0.058, 0.05, 0.048), MZ + (0, -0.03, 0.022), (0.02, 0.013), MZ + (0, -0.03, -0.024),
                   (0.03, 0.016))

    # --- the white ribbon of fur streaming up and back off the crown, run out to +X, branching at its end --------
    root = H + np.array([0.03, 0.07, 0.09])
    P1, P2, P3 = root + (0.05, 0.12, 0.065), root + (0.13, 0.25, 0.08), root + (0.22, 0.36, 0.05)
    path = [root, P1, P2, P3]
    def ribbon(u, v):
        tt = u * (len(path) - 1)
        i = np.clip(np.floor(tt).astype(int), 0, len(path) - 2)
        f = (tt - i)[..., None]
        A, B = np.array(path)[i], np.array(path)[i + 1]
        p = A + (B - A) * f
        w = 0.054 * (1 - 0.75 * u ** 1.5) + 0.004
        side = np.array(unit(0.25, -0.1, 1))
        return p + side[None, None, :] * ((v - 0.5) * 2 * w)[..., None]
    k.add("stream", outward(sheet(ribbon, 30, 6, 0.024)), WHITE, "head", fuse=False)
    k.add("streamroot", ell(root - (0, 0.01, 0.01), (0.028, 0.028, 0.024)), STREAM_ROOT, "head")
    sk.spike(k, "branch0", P2, (0.45, 0.6, 0.55), 0.09, 0.034, 0.016, "head", WHITE, mid=0.3)
    sk.spike(k, "branch1", P3 - (0.02, 0.03, 0.0), (0.85, 0.45, 0.2), 0.085, 0.032, 0.015, "head", WHITE, mid=0.3)

    # --- the eyes: small and fierce, black with a glint, under heavy black brows ---------------------------------
    de = unit(0.35, -0.93, 0.05)
    E = hit(HEAD, H + (0.05, -0.5, 0.012), (0, 1, 0))
    lay = kb.brow_eyes(k, HEAD, E, de, (0.025, 0.024), 0.3, 0.45, srgb(GREY), BLACK, 0.016,
                       look=kb.bear_eye(at=(0.3, 0.2), size=0.32), line_col=kb.INK)

    k.build(voxel=0.008, smooth=4, band=0.005, wband=0.06)
    k.eye_states(lay)
    k.export(out_path)


if __name__ == "__main__":
    urshifu(*args())
