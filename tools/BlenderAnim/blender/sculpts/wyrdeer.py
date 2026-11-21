# Wyrdeer sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/wyrdeer.py -- <the export's Wyrdeer.glb> <out.glb>
# Grown from stantler.py (sibling("stantler")): the same frame (its leg places, the head ball with a muzzle and a big
# loose round nose, ears lying out to the sides, grumpy eyes cut by a lid, antlers of loose tubes with a dark orb on the
# inner side of each); what the pictures change.
#
# From the references: an old pale grey stag. A great white beard from the cheeks under the eyes down over the whole
# chest to below the shoulders, ending in a curl: the largest mass from the front. A pale grey face above it with a big
# maroon nose sitting on the beard, heavy-lidded sleepy eyes, grey ears out to the sides, white within. Pale gold
# antlers, square-cornered: a short beam out from the brow to a corner, up from it, and there forking out and up,
# each fork kinked with a stubby tine; a dark purple orb sits in each lower corner. A deep grey trunk, two pale marks
# high on each flank, a big white puff of a tail. Thick legs: grey shoulders and thighs fused into the flanks, a big
# round knee on each foreleg, white shins and socks with a tuft of white fur behind each fetlock, dark grey hooves.
# The task's text expected a grey-brown stag with black antlers: the pictures show pale grey and gold antlers.
# Masses ranked from the pictures: the beard, the trunk, the antlers, the head, the tail.
# Ratios (3D picture): head width / height 0.19 (built 0.2), beard width / height 0.24 (0.23), trunk depth / leg
# showing 0.64 (0.68), antler span / height 0.5 (0.52), antler height / height 0.31 (0.3).
# Bones: body (0, 0.02, 0.46); head (0, -0.17, 0.58); ears (+-0.05, -0.2, 0.8); legs (+-0.075, -0.16, 0.4);
# hinds (+-0.075, 0.2, 0.4); tail (0, 0.26, 0.51).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, outward, lumpy, srgb, inside, unit, polygon, eye_look, args, sibling

st = sibling("stantler")
lv = st.lv

GREY, LIGHT, WHITE, NOSE, HOOF = "#BEC1C9", "#DADCE2", "#F6F6F8", "#8E3E3C", "#4A4A54"
ANTLER, ORB = "#F2D27E", "#3A2C40"
INK = lv.INK
IRIS = srgb("#2E2A30")

H, HR = np.array([0.0, -0.27, 0.745]), np.array([0.104, 0.112, 0.09])
LEG_X, FORE_Y, HIND_Y = st.LEG_X, st.FORE_Y, st.HIND_Y
join = st.join


def wyrdeer(src, out):
    k = Kit(src, coat="fur")

    # --- the trunk: deep and grey, a white puff of a tail behind -------------------------------------------------
    k.add("barrel", ell((0, 0.04, 0.465), (0.122, 0.25, 0.125)), GREY, "body")
    k.add("chest", ell((0, -0.15, 0.47), (0.118, 0.12, 0.13)), GREY, "body")
    k.add("tail", ell((0, 0.31, 0.56), (0.088, 0.085, 0.085), deform=lumpy(4, 3, 0.08)), WHITE, "tail")
    k.add("tailtip", blade([(0, 0.34, 0.62), (0, 0.37, 0.66), (0, 0.355, 0.69)], [0.035, 0.025, 0.006], (1, 0, 0),
                           0.6), WHITE, "tail")
    for c, r, up in (((0.112, -0.02, 0.53), (0.012, 0.03), unit(0.2, -1, 0.4)), ((0.115, 0.05, 0.515), (0.009, 0.02),
                                                                                    unit(0.2, -1, 0.3))):
        k.decal(c, unit(1, 0, 0.35), r, LIGHT, up=up, eps=0.003, rings=8, segs=40, mirror=True)

    # --- the neck and head: stantler.py's head ball and muzzle, shorter, the big round nose loose ----------------
    k.add("neck", tube([(0, -0.13, 0.52), (0, -0.2, 0.61), (0, -0.24, 0.69)], [0.1, 0.095, 0.085]), GREY, "head")
    k.add("head", ell(H, HR), GREY, "head")
    k.add("muzzle", ell((0, -0.345, 0.705), (0.07, 0.075, 0.062), rot=(14, 0, 0)), GREY, "head")
    k.add("nose", ell((0, -0.408, 0.683), (0.046, 0.04, 0.043), sub=3), NOSE, "head", fuse=False, surface="skin")
    # the ears, out to the sides and drooping a little, white within
    k.add("ear", blade([(0.08, -0.25, 0.8), (0.15, -0.245, 0.795), (0.225, -0.235, 0.77)], [0.042, 0.038, 0.009],
                       (0, 1, 0), 0.4), GREY, "ear{s}", mirror=True)
    k.paint(lambda P: inside(P[:, 1] + 0.252, 0.002) * inside(np.abs(P[:, 2] - 0.795 + 0.2 * (np.abs(P[:, 0]) - 0.14))
                                                              - 0.017, 0.002) * inside(0.1 - np.abs(P[:, 0]), 0.003),
            WHITE, on=["ear"])

    # --- the beard: from the cheeks under the eyes down over the chest, ending in a curl --------------------------
    k.add("beardtop", ell((0, -0.29, 0.635), (0.13, 0.1, 0.09)), WHITE, "head")
    k.add("beard", ell((0, -0.27, 0.47), (0.14, 0.115, 0.2)), WHITE, "body")
    k.add("beardlow", ell((0, -0.265, 0.36), (0.1, 0.09, 0.09)), WHITE, "body")
    k.add("curl", tube([(0, -0.29, 0.3), (0.035, -0.33, 0.268), (0.07, -0.335, 0.285), (0.078, -0.32, 0.318)],
                       [0.04, 0.03, 0.022, 0.014]), WHITE, "body")

    # --- the legs: grey shoulders and thighs fused into the flanks, a round knee, white below, dark hooves --------
    k.add("shoulder", ell((LEG_X - 0.004, FORE_Y, 0.39), (0.068, 0.085, 0.13)), GREY, "leg{s}", mirror=True)
    k.add("leg", tube([(LEG_X, FORE_Y, 0.34), (LEG_X + 0.002, FORE_Y, 0.24), (LEG_X + 0.002, FORE_Y - 0.004, 0.06)],
                      [0.046, 0.04, 0.034]), WHITE, "leg{s}", mirror=True)
    k.add("knee", ell((LEG_X + 0.002, FORE_Y - 0.008, 0.25), (0.056, 0.058, 0.056)), LIGHT, "leg{s}", mirror=True)
    k.add("thigh", ell((LEG_X, HIND_Y, 0.4), (0.082, 0.11, 0.15)), GREY, "hind{s}", mirror=True)
    k.add("hind", tube([(LEG_X, HIND_Y + 0.01, 0.32), (LEG_X + 0.003, HIND_Y + 0.035, 0.2),
                        (LEG_X + 0.002, HIND_Y, 0.06)], [0.05, 0.04, 0.034]), GREY, "hind{s}", mirror=True)
    k.paint(lambda P: inside(P[:, 2] - 0.17, 0.003), WHITE, on=["hind"])
    k.paint(lambda P: inside(P[:, 2] - 0.205, 0.003) * inside(P[:, 1] + 0.05, 0.003), WHITE, on=["leg", "shoulder"])
    for bone, y in (("leg{s}", FORE_Y), ("hind{s}", HIND_Y)):
        k.add(f"hoof{bone[0]}", ell((LEG_X + 0.002, y - 0.012, 0.037), (0.042, 0.048, 0.032),
                                    deform=lambda V: V * (1 + 0.15 * np.clip(-V[:, 2:3], 0, 1))), HOOF, bone,
              mirror=True, surface="shell")
        # a tuft of white fur behind each fetlock: two thick points back and up
        for j, (dx, dz) in enumerate(((0.012, 0.0), (-0.012, 0.025))):
            lv.spike(k, f"tuft{bone[0]}{j}", (LEG_X + dx, y + 0.028, 0.095 + dz), (0.3, 1, 0.5), 0.075, 0.042, 0.032,
                     (0, 0, 1), WHITE, bone, sink=0.015, mirror=True, surface="fur")

    # --- the antlers: square-cornered gold beams, a dark orb in each lower corner ----------------------------------
    def beam(a, b, r0, r1):
        return outward(tube([a, (np.array(a) + np.array(b)) / 2, b], [r0, (r0 + r1) / 2, r1], seg=14, per=4))
    B0, E, F = (0.045, -0.25, 0.81), (0.165, -0.255, 0.85), (0.18, -0.25, 0.985)
    T1, M1 = (0.32, -0.235, 1.055), (0.245, -0.243, 1.02)
    G, T2 = (0.205, -0.25, 1.095), (0.165, -0.245, 1.17)
    k.add("antler", join(beam(B0, E, 0.027, 0.025), beam(E, F, 0.025, 0.024), beam(F, T1, 0.024, 0.021),
                         beam(M1, (0.255, -0.24, 1.09), 0.021, 0.019), beam(F, G, 0.023, 0.021),
                         beam(G, T2, 0.021, 0.019), beam(G, (0.26, -0.248, 1.13), 0.02, 0.018)),
          ANTLER, "head", fuse=False, mirror=True, surface="shell")
    k.add("orb", ell((0.105, -0.268, 0.895), (0.036, 0.036, 0.036), sub=3), ORB, "head", fuse=False, mirror=True,
          surface="shell")

    k.build(voxel=0.0075, smooth=4, band=0.005)

    # --- the eyes: sleepy, white with a dark iris, under a heavy grey lid ---------------------------------------
    de = unit(0.64, -0.72, 0.22)
    E_ = lv.on_ell(H, HR, de)
    cut = lv.brow_cut(0.12, 0.25)
    look = lv.angry_eye(cut, iris=IRIS, rim=0.8, iris_c=(-0.15, -0.2), iris_r=0.46, pupil_r=0.22)
    m = np.array([-1, 1, 1.0])
    LID = srgb(GREY)

    def eyes(state):
        lk = eye_look(state, look, LID, INK, line=0.16)
        k.decal(E_, de, (0.036, 0.03), lk, eps=0.004, rings=20, segs=56, outline=cut)
        k.decal(E_ * m, de * m, (0.036, 0.03), lambda u, v: lk(-u, v), eps=0.004, rings=20, segs=56,
                outline=lambda th: cut(math.pi - th))

    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    wyrdeer(*args())
