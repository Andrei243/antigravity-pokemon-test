# Stantler sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/stantler.py -- <the export's Stantler.glb> <out.glb>
# From the references: a tan deer with a deep chest and a long neck, a cream patch down the front of the chest and a
# cream line under the belly, two cream spots high on each flank, and behind a great round rump of a tail; slender
# legs on broad shoulders and thighs, dark purple hooves; a head with a big round brown nose, ears lying out to the
# sides, grumpy eyes under a brow sloping down to the nose; and two yellow antlers branching up and out from the crown,
# each curling back in over a dark purple orb that sits on its inner side like an eye.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, outward, srgb, inside, smoothstep, unit, polygon, eye_look, args, sibling

lv = sibling("larvitar")              # the eye cut by a brow

TAN, CREAM, NOSE, HOOF = "#D4A87C", "#F4E4C0", "#8A4C36", "#4E2E5E"
ANTLER, ORB = "#F2CF4A", "#3C2A52"
INK = lv.INK
IRIS = srgb("#3A2A22")

H, HR = np.array([0.0, -0.262, 0.765]), np.array([0.095, 0.108, 0.086])
LEG_X = 0.078
FORE_Y, HIND_Y = -0.16, 0.2


def join(*shapes):
    V, F, off = [], [], 0
    for sv, sf in shapes:
        V.append(sv); F.extend([[i + off for i in f] for f in sf]); off += len(sv)
    return np.vstack(V), F


def stantler(src, out):
    k = Kit(src, coat="fur")

    # The trunk: a deep chest, the middle, and behind it the great round rump of the tail
    k.add("chest", ell((0, -0.15, 0.45), (0.105, 0.11, 0.145)), TAN, "body")
    k.add("middle", ell((0, 0.02, 0.45), (0.1, 0.17, 0.125)), TAN, "body")
    k.add("rump", ell((0, 0.27, 0.49), (0.148, 0.17, 0.17)), TAN, "tail")
    # The neck: long and thick, up and forward to the head
    k.add("neck", tube([(0, -0.17, 0.5), (0, -0.21, 0.6), (0, -0.235, 0.69), (0, -0.245, 0.74)],
                       [0.085, 0.068, 0.06, 0.058]), TAN, "head")
    # The head, the muzzle forward and down, the big round nose loose at its end
    k.add("head", ell(H, HR), TAN, "head")
    k.add("muzzle", ell((0, -0.352, 0.73), (0.064, 0.08, 0.058), rot=(12, 0, 0)), TAN, "head")
    k.add("nose", ell((0, -0.418, 0.722), (0.057, 0.047, 0.05), sub=3), NOSE, "head", fuse=False, surface="skin")
    # The ears: lying out to the sides, flat to the front
    k.add("ear", blade([(0.075, -0.24, 0.815), (0.135, -0.235, 0.82), (0.195, -0.23, 0.812)], [0.036, 0.032, 0.007],
                       (0, 1, 0), 0.38), TAN, "ear{s}", mirror=True)

    # The legs: broad shoulders and thighs over the upper half of each leg, the lower leg tapering, dark hooves
    k.add("shoulder", ell((LEG_X - 0.005, FORE_Y, 0.39), (0.062, 0.075, 0.12)), TAN, "leg{s}", mirror=True)
    k.add("leg", tube([(LEG_X, FORE_Y, 0.36), (LEG_X + 0.002, FORE_Y + 0.005, 0.2), (LEG_X + 0.002, FORE_Y, 0.05)],
                      [0.042, 0.03, 0.025]), TAN, "leg{s}", mirror=True)
    k.add("thigh", ell((LEG_X, HIND_Y, 0.39), (0.075, 0.1, 0.14)), TAN, "hind{s}", mirror=True)
    k.add("hind", tube([(LEG_X, HIND_Y, 0.34), (LEG_X + 0.004, HIND_Y + 0.04, 0.2), (LEG_X + 0.002, HIND_Y, 0.05)],
                       [0.048, 0.03, 0.025]), TAN, "hind{s}", mirror=True)
    for bone, y in (("leg{s}", FORE_Y), ("hind{s}", HIND_Y)):
        k.add(f"hoof{bone[0]}", ell((LEG_X + 0.002, y - 0.01, 0.026), (0.031, 0.038, 0.028),
                                    deform=lambda V: V * (1 + 0.15 * np.clip(-V[:, 2:3], 0, 1))), HOOF, bone,
              mirror=True, surface="shell")

    # The cream down the front of the chest and under the belly, and the two spots high on each flank
    k.decal((0, -0.24, 0.43), unit(0, -1, 0.15), (0.065, 0.1), CREAM, eps=0.003, rings=14, segs=56)
    k.paint(lambda P: inside(P[:, 2] - 0.345, 0.004) * inside(np.abs(P[:, 1] + 0.02) - 0.16, 0.01)
            * inside(np.abs(P[:, 0]) - 0.07, 0.01), CREAM, on=["chest", "middle"])
    k.decal((0.1, -0.08, 0.52), unit(1, -0.1, 0.55), (0.02, 0.032), CREAM, up=unit(0, -0.4, 1), eps=0.003, rings=8,
            segs=40, mirror=True)
    k.decal((0.1, -0.035, 0.49), unit(1, 0, 0.45), (0.011, 0.011), CREAM, eps=0.003, rings=6, segs=32, mirror=True)

    # The antlers: a yellow beam up and out from the crown curling back in at the top, a tine up from its inner
    # side, one out from its outer, one at the top; a dark orb on the inner side of the lower beam. In the plane of
    # the face, tilted a little back, so that the game's front and back cameras both see them whole.
    BEAM = [(0.035, -0.235, 0.83), (0.085, -0.24, 0.885), (0.16, -0.25, 0.92), (0.215, -0.255, 0.99),
            (0.205, -0.25, 1.06), (0.145, -0.24, 1.095)]
    beam = outward(tube(BEAM, [0.024, 0.021, 0.02, 0.018, 0.016, 0.011], seg=14, per=6))
    inner = outward(tube([(0.085, -0.24, 0.885), (0.078, -0.24, 0.95), (0.098, -0.235, 1.01)], [0.018, 0.015, 0.01],
                         seg=12, per=6))
    outer = outward(tube([(0.16, -0.25, 0.92), (0.225, -0.255, 0.9), (0.27, -0.255, 0.935)], [0.018, 0.015, 0.01],
                         seg=12, per=6))
    top = outward(tube([(0.205, -0.25, 1.06), (0.235, -0.245, 1.1), (0.25, -0.24, 1.13)], [0.016, 0.013, 0.009],
                       seg=12, per=6))
    k.add("antler", join(beam, inner, outer, top), ANTLER, "head", fuse=False, mirror=True, surface="shell")
    k.add("orb", ell((0.12, -0.24, 0.935), (0.033, 0.033, 0.033), sub=3), ORB, "head", fuse=False, mirror=True,
          surface="shell")

    # The eyes: grumpy, white with a small dark iris toward the nose, cut by the brow sloping down to it
    de = unit(0.66, -0.7, 0.2)
    E = lv.on_ell(H, HR, de)
    cut = lv.brow_cut(0.22, 0.4)
    look = lv.angry_eye(cut, iris=IRIS, rim=0.8, iris_c=(-0.2, -0.12), iris_r=0.42, pupil_r=0.2)
    m = np.array([-1, 1, 1.0])
    LID = srgb(TAN)

    def eyes(state):
        lk = eye_look(state, look, LID, INK, line=0.16)
        k.decal(E, de, (0.036, 0.031), lk, eps=0.006, rings=20, segs=56, outline=cut)
        k.decal(E * m, de * m, (0.036, 0.031), lambda u, v: lk(-u, v), eps=0.006, rings=20, segs=56,
                outline=lambda th: cut(math.pi - th))

    k.build(voxel=0.0075, smooth=4)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    stantler(*args())
