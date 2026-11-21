# Shroodle sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/shroodle.py -- <the export's Shroodle.glb> <out.glb>
#
# From the references (they overrule the brief's "grey shrew with a pale face and coloured paws"): a low creature
# whose largest mass is a great dark brown head; on each side of it a huge glassy mint-green eye with a small dark pupil
# low and forward; from the lower front a thick drooping snout ending in a bulb, a white tongue tipped purple hanging
# from it; small round dark ears on the head's back sides; a cream tuft on the crown curling back; behind the head a
# blue-grey dome of a back, pale lilac underneath, on four small white feet, the hind ones with a blue-grey pad.
#
# Written to be grown from: Grafaiai keeps its colours and its glassy mint eye (lens_eye, MINT, LENS_LIGHT, PUPIL).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, unit, eye_look, outward, args

BROWN, BLUE, BELLY, FOOT = "#3A2B27", "#5F7D99", "#D7D2E8", "#EEEDF4"
CREAM, TONGUE, TIP = "#E9E6C4", "#F2F0F4", "#8E6BD6"
MINT, LENS_LIGHT, PUPIL, SHUTLINE = "#74D9AF", "#B4F0D4", "#173329", "#B9AE9E"

H = np.array([0.0, -0.03, 0.105])           # the head's centre
HR = np.array([0.08, 0.085, 0.087])          # its half sizes
VOXEL = 0.0026
EYE_DIR = (0.72, -0.6, 0.1)                  # the eyes look out to the sides and some 50 degrees forward
EYE_HALF = (0.047, 0.053)


def on_ell(C, R, d, lift=0.0):
    """The point of an ellipsoid's surface in a direction from its centre."""
    d = unit(*d)
    s = 1 / math.sqrt(((d / np.array(R)) ** 2).sum())
    return np.array(C) + d * (s + lift)


def lens_eye(mint=MINT, light=LENS_LIGHT):
    """A glassy lens with no white: the colour lighter toward its top and a white glint up and back."""
    cm, cl, cw = srgb(mint), srgb(light), np.array([1.0, 1.0, 1.0])
    def look(u, v):
        t = np.clip((v + 0.2) / 1.0, 0, 1)[:, None]
        c = cm[None] * (1 - t * 0.55) + cl[None] * t * 0.55
        c[((u - 0.3) / 0.24) ** 2 + ((v - 0.5) / 0.2) ** 2 < 1] = cw
        return c
    return look


def head(k):
    k.add("head", ell(H, HR), BROWN, "head")
    # flattened cheeks so each great eye lies on a near-flat face
    de = unit(*EYE_DIR)
    a = math.degrees(math.atan2(de[1], de[0]))
    E = on_ell(H, HR, de)
    k.add("cheek", ell(E - de * 0.016, (0.02, 0.058, 0.062), rot=(0, 0, a)), BROWN, "head", mirror=True)
    # the snout: thick, out of the lower front, drooping to a bulb
    k.add("snout", tube([(0, -0.07, 0.085), (0, -0.118, 0.07), (0, -0.148, 0.052)], [0.034, 0.026, 0.025]), BROWN,
          "head")
    k.add("bulb", ell((0, -0.16, 0.045), (0.034, 0.033, 0.029)), BROWN, "head")
    # small round ears on the back of the head's sides, facing out and back
    k.add("ear", ell((0.072, 0.03, 0.135), (0.011, 0.025, 0.025), rot=(0, 0, 35)), BROWN, "ear{s}", mirror=True)


def tongue(k):
    """A flat white tongue hanging from under the bulb, its tip purple."""
    ax = unit(0, 0.85, 0.45)
    k.add("tongue", outward(blade([(0, -0.18, 0.024), (0, -0.188, 0.014)], [0.0105, 0.0105], ax, 0.42)), TONGUE,
          "head", fuse=False, surface="skin")
    k.add("tonguetip", outward(blade([(0, -0.189, 0.0125), (0, -0.195, 0.006)], [0.0103, 0.008], ax, 0.42)), TIP,
          "head", fuse=False, surface="skin")


def tuft(k):
    """The cream tuft on its crown, swelling at its root and curling back to a hooked point."""
    k.add("tuft", outward(tube([(0, -0.03, 0.17), (0, -0.033, 0.2), (0, -0.024, 0.226), (0, -0.004, 0.238),
                                (0, 0.014, 0.232)], [0.027, 0.024, 0.015, 0.007, 0.0025])), CREAM, "head",
          fuse=False, surface="fur")


def body(k):
    k.add("back", ell((0, 0.052, 0.072), (0.07, 0.088, 0.07)), BLUE, "body")
    k.add("hips", ell((0, 0.09, 0.045), (0.062, 0.05, 0.045)), BLUE, "body")
    for nm, y, bone in (("foot", -0.045, "leg{s}"), ("hind", 0.088, "hind{s}")):
        k.add(nm, ell((0.047, y, 0.013), (0.022, 0.03, 0.0135)), FOOT, bone, mirror=True)
    # the pale underside along the dome's foot
    k.paint(lambda P: inside(P[:, 2] - 0.026, 0.002) * inside(-P[:, 1] - 0.02, 0.003), BELLY, on=["back", "hips"])
    # blue-grey pads on the hind feet's backs
    k.decal((0.047, 0.117, 0.013), unit(0.15, 1, 0.05), (0.011, 0.008), BLUE, eps=0.0012, rings=10, segs=36,
            mirror=True)


def eyes(k, state):
    de = unit(*EYE_DIR)
    E = on_ell(H, HR, de) - de * 0.005
    look = eye_look(state, lens_eye(), srgb(BROWN), srgb(SHUTLINE), line=0.13)
    k.decal(E, de, EYE_HALF, look, eps=0.0016, rings=20, segs=56, mirror=True)
    if state in ("Open", "Fierce"):
        # the pupil: low on the eye and toward the snout, larger than drawn so it reads small
        dp = unit(0.6, -0.76, -0.04)
        k.decal(on_ell(H, HR, dp) - dp * 0.012, de, (0.0085, 0.0085), PUPIL, eps=0.0024, rings=10, segs=36,
                mirror=True)


def shroodle(src, out):
    k = Kit(src, coat="fur")
    head(k)
    body(k)
    tongue(k)
    tuft(k)
    k.build(voxel=VOXEL, smooth=3, band=0.003, wband=0.02)
    k.eye_states(lambda state: eyes(k, state))
    k.export(out)


if __name__ == "__main__":
    shroodle(*args())
