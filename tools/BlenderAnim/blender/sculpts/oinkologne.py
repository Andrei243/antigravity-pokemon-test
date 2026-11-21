# Oinkologne (the male) sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/oinkologne.py -- <the export's Oinkologne.glb> <out.glb>
# Grown from lechonk.py (sibling("lechonk")): the same snout and nostrils, the half-lidded eye with its yellow mark, the
# hooves, the curled tail and the flopping ears, on a hog's frame.
#
# From the references: a big grey-violet hog, its barrel of a body the largest mass, on four legs that show under it a
# little (thick, straight, magenta hooves); the head, the neck and the chest one darker violet mass in front, the head
# carried high with its snout lifted; a flat magenta snout disc with two dark nostrils; half-lidded pale grey eyes with
# dark pupils, a heavy lid over them, a sharp yellow mark under each; two great floppy ears falling forward from the
# crown over the brow like a fringe, the same dark violet; a thin grey tail rising from the rump in a loop and ending in
# a big magenta tuft like an egg, hanging behind.
# The pictures overrule the task's text: the snout is a flat disc, not a "great rounded snout"; the colour is a
# grey-violet with a darker violet front, not grey-brown; the tail ends in a magenta tuft.
# Reference ratios (3D picture, hooves to the ear's top): head and neck 0.38 of the height; body 0.58 of the height deep,
# 1.4 times as long as the height; legs showing under the belly 0.2; the snout disc 0.12 of the height across; the tail's
# tuft 0.2 of the height long.
# The export's joints: body (0, 0.02, 0.3), head (0, -0.16, 0.35), ears (+-0.07, -0.19, 0.44), legs (+-0.09, -0.12,
# 0.2), hind legs (+-0.09, 0.14, 0.2), tail (0, 0.23, 0.36).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, args, sibling

L = sibling("lechonk")

BODY = "#6A5E72"
FRONT = "#4A3A58"
SNOUT = "#E8489A"
NOSTRIL = "#36182A"
HOOF = "#E8509E"
YELLOW = "#EEDA80"
WHITE = "#E6E2EA"
PUPIL = "#2C2632"
INK = "#241C2A"
TUFT = "#E2449A"

HC = np.array([0.0, -0.205, 0.45])          # the head's middle
HR = np.array([0.145, 0.125, 0.13])
VOXEL = 0.0062


def oinkologne(src, out):
    k = Kit(src, coat="fur")
    # the barrel, the neck and chest, the head
    k.add("body", ell((0, 0.06, 0.296), (0.168, 0.24, 0.19)), BODY, "body")
    k.add("bodyrump", ell((0, 0.2, 0.3), (0.15, 0.13, 0.17)), BODY, "body")
    k.add("chest", ell((0, -0.11, 0.33), (0.152, 0.14, 0.17)), BODY, "body")
    k.add("neck", ell((0, -0.15, 0.41), (0.13, 0.12, 0.13)), BODY, "head")
    k.add("head", ell(HC, HR), BODY, "head")
    k.add("jowl", ell(HC + (0, -0.02, -0.06), (0.1, 0.09, 0.07)), BODY, "head")
    # legs: shoulders and haunches fused in, thick shins, magenta hooves
    for nm, y, top, bone in (("leg", -0.125, (0.068, 0.075, 0.085), "leg{s}"), ("hind", 0.16, (0.078, 0.09, 0.1), "hind{s}")):
        k.add(nm + "haunch", ell((0.095, y + (0.01 if nm == "hind" else 0), 0.215), top), BODY, bone, mirror=True)
        k.add(nm, tube([(0.096, y, 0.17), (0.097, y - 0.005, 0.1), (0.098, y - 0.008, 0.05)], [0.056, 0.052, 0.05]),
              BODY, bone, mirror=True)
        L.hoof(k, (0.098, y - 0.012, 0.022), (0.046, 0.052, 0.022), bone, HOOF, name=nm + "hoof")
    k.paint(lambda P: inside(P[:, 2] - 0.04, 0.003) * (np.abs(P[:, 0]) > 0.04), HOOF, on=["leg", "hind"])
    # the brow over each eye
    az, ez = 0.72, 0.47
    E, en = L.on_ball(HC, HR, az, ez)
    bp, bn = L.on_ball(HC, HR, az - 0.05, ez + 0.042)
    k.add("brow", ell(bp - bn * 0.014, (0.05, 0.034, 0.02), rot=(0, -14, -math.degrees(az))), FRONT, "head",
          mirror=True)
    # the darker violet of the head, the neck and the chest
    def front(P):
        return inside(P[:, 1] - (-0.02 + 0.35 * (P[:, 2] - 0.35)), 0.004) * inside(0.175 - P[:, 2], 0.004)
    k.paint(front, FRONT, on=["body", "chest", "neck", "head", "jowl", "brow", "leg"])
    # the snout, lifted a little
    L.snout(k, HC + (0, -0.11, 0.016), (0.058, 0.042, 0.05), "head", SNOUT, NOSTRIL, tilt=20, nostril_size=(0.2, 0.42))
    # the ears falling forward over the brow
    L.flop_ear(k, (0.07, -0.15, 0.568), (0.14, -0.232, 0.545), (0.188, -0.29, 0.472), (0.05, 0.058, 0.014),
               FRONT, FRONT, (0.6, 0.1, 1.0), "ear{s}", drop=0.008)
    # the yellow mark under each eye
    en2 = unit(en[0] * 1.1, en[1], en[2] * 0.6)
    half = (0.034, 0.035)
    L.under_mark(k, E + np.array([0.002, 0.004, 0.0]), en2, half, YELLOW, 0.026, (0.028, 0.016, -0.02))
    # the tail: a loop up from the rump, ending in a big magenta tuft hanging behind
    end = L.curl_tail(k, (0.0, 0.285, 0.44), 0.04, (0.42, 0.9, 0.0), 0.0115, BODY, turns=0.85)
    d = unit(0.25, 0.3, -0.92)
    k.add("tuft", ell(end + d * 0.055, (0.05, 0.052, 0.074), rot=(-10, 0, 8)), TUFT, "tail", fuse=False,
          surface="fur")
    k.build(voxel=VOXEL, smooth=4, band=0.006, wband=0.05)
    look = L.lidded_eye(WHITE, PUPIL, INK)
    k.eye_states(lambda s: L.lay_eyes(k, s, E, en2, half, look, FRONT, INK, cut=0.3))
    k.export(out)


if __name__ == "__main__":
    oinkologne(*args())
