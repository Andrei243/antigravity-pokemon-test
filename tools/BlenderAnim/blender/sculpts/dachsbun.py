# Dachsbun sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/dachsbun.py -- <the export's Dachsbun.glb> <out.glb>
# Grown from fidough.py (sibling("fidough")): the same rolled ears, the ring round the neck (lumped into buns), the eye,
# the stubby legs and the way glaze is painted, baked brown.
#
# From the references: a long low dachshund of baked bread, orange-brown, its belly and lower face a pale golden tan;
# a round head carried high and looking up, a short tan muzzle with a small black nose; small dark eyes with a white
# glint; a dark crust over the crown edged in gold, a little bun on top; ears rolled up like buns, dark crust with
# orange turns; a great ring of dark crusty buns round the neck, the largest mass in front; a jabot of pale yellow
# pointed fluff under the chin over the ring; a dark ridge of crust along the back; short thick legs, small cream
# claws; a short dark stub of a tail rising behind, ending in a plume of golden pointed fluff.
# The pictures overrule the task's text: the ears are rolled buns (as Fidough's), not plaits, and there is a ring of
# buns round the neck with a fluff jabot; the tail ends in a golden plume.
# Reference ratios (3D picture, paws to the plume's top): the head 0.34 of the height; the ring of buns 1.4 times the
# head's width; the body 0.27 of the height deep and 1.3 times the height long; legs showing 0.28 (here 0.21, kept
# under a quarter); the plume 0.3 of the height tall.
# The export's joints: body (0, 0.05, 0.15), head (0, -0.08, 0.2), ears (+-0.06, -0.11, 0.28), legs (+-0.065, -0.06,
# 0.1), hind legs (+-0.065, 0.15, 0.1), tail (0, 0.2, 0.18).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, inside, unit, boxy, args, sibling

F = sibling("fidough")

ORANGE = "#D2622A"
CRUST = "#8C381C"
TAN = "#EEA24A"
GOLD = "#F2BE4A"
FLUFF = "#F6E096"
CLAW = "#F8E6B0"
NOSE = "#1C1414"
EYE = "#2C1A18"

HC = np.array([0.0, -0.135, 0.282])
HR = np.array([0.086, 0.08, 0.08])
VOXEL = 0.0042


def spike(base, d, length, w, t):
    """A solid tapering point of fluff: a flat-faced hull, wide across `side`, thin across its face."""
    base = np.array(base, float); d = unit(*d)
    side = np.cross(d, (0, 0, 1.0))
    if np.linalg.norm(side) < 1e-3:
        side = np.array([1.0, 0, 0])
    side /= np.linalg.norm(side)
    face = np.cross(d, side)
    pts = [base + side * w, base - side * w, base + face * t, base - face * t,
           base + d * length * 0.45 + side * w * 0.6 + face * t * 0.4, base + d * length * 0.45 - side * w * 0.6,
           base + d * length]
    return hull(pts)


def dachsbun(src, out):
    k = Kit(src, coat="fur")
    # the long low body, the chest and rump
    k.add("body", ell((0, 0.06, 0.155), (0.076, 0.158, 0.074)), ORANGE, "body")
    k.add("bodyrump", ell((0, 0.165, 0.158), (0.072, 0.07, 0.07)), ORANGE, "body")
    k.add("bodychest", ell((0, -0.05, 0.165), (0.08, 0.08, 0.08)), ORANGE, "body")
    # a ridge of crust along the back
    for y in (-0.01, 0.07, 0.15):
        k.add("crust", ell((0, y, 0.222), (0.04, 0.042, 0.018)), CRUST, "body")
    # the pale belly
    k.paint(lambda P: inside(P[:, 2] - 0.108, 0.004) * inside(np.abs(P[:, 0]) - 0.05, 0.004), TAN, on=["body"])
    # legs: shoulders and haunches fused in, short thick shins, round paws with cream claws
    for nm, y, bone, top in (("leg", -0.06, "leg{s}", (0.042, 0.048, 0.05)), ("hind", 0.16, "hind{s}", (0.046, 0.058, 0.062))):
        k.add(nm + "haunch", ell((0.058, y, 0.125), top), ORANGE, bone, mirror=True)
        k.add(nm, tube([(0.06, y, 0.1), (0.061, y - 0.004, 0.034)], [0.034, 0.031]), ORANGE, bone, mirror=True)
        k.add(nm + "paw", ell((0.061, y - 0.012, 0.016), (0.032, 0.04, 0.016)), ORANGE, bone, mirror=True)
        for dx in (-0.014, 0.0, 0.014):
            k.add(nm + "claw", ell((0.061 + dx, y - 0.044, 0.011), (0.0055, 0.008, 0.006)), CLAW, bone, fuse=False,
                  mirror=True, surface="shell")
    k.paint(lambda P: inside(P[:, 2] - 0.006, 0.002) * (np.abs(P[:, 0]) > 0.03), CLAW, on=["legpaw", "hindpaw"])
    # the ring of buns round the neck, gold over its top edge
    RC, RR, RT = np.array([0.0, -0.08, 0.2]), 0.086, 28
    F.neck_ring(k, RC, RR, 0.042, RT, CRUST, "body", lumps=7, amp=0.22, n=70)
    F.ring_glaze(k, RC, RR, RT, GOLD, ["ring"], top=0.034, scallops=7, depth=0.006)
    # the head, carried high; the muzzle; the tan lower face
    k.add("head", ell(HC, HR, deform=boxy(2.3)), ORANGE, "head")
    MC = HC + np.array([0.0, -0.07, -0.032])
    k.add("muzzle", ell(MC, (0.05, 0.042, 0.036), rot=(-14, 0, 0)), TAN, "head")
    def lower(P):
        q = P - HC
        return inside(q[:, 2] + 0.012 - 0.25 * (-q[:, 1] - 0.03), 0.003) * (q[:, 1] < -0.02)
    k.paint(lower, TAN, on=["head", "muzzle"])
    k.add("nose", ell(MC + (0, -0.04, 0.018), (0.015, 0.012, 0.011), rot=(-14, 0, 0)), NOSE, "head", fuse=False,
          surface="skin")
    # the crust over the crown, edged gold, and its little bun
    k.add("bun", ell(HC + (0, -0.03, 0.074), (0.024, 0.021, 0.018)), CRUST, "head")
    def crown(edge):
        def m(P):
            q = P - HC
            a = np.arctan2(q[:, 0], -q[:, 1])
            return inside(edge - q[:, 2] + 0.004 * np.cos(3 * a), 0.003) * (np.abs(q[:, 0]) < 0.08)
        return m
    k.paint(crown(0.044), GOLD, on=["head", "bun"])
    k.paint(crown(0.055), CRUST, on=["head", "bun"])
    # the rolled ears: dark crust with orange turns
    F.roll_ear(k, (0.096, -0.1, 0.27), (0.042, 0.056, 0.062), (1, 0.15, 0), CRUST, "ear{s}")
    k.paint(lambda P: np.ones(len(P)), ORANGE, on=["earcoil"])
    # the jabot of pale fluff under the chin, hanging over the ring's front
    for x, l, ang in ((0.0, 0.07, 0.0), (0.04, 0.062, 0.55), (-0.04, 0.064, -0.55), (0.074, 0.05, 1.0),
                      (-0.074, 0.05, -1.0)):
        d = unit(math.sin(ang) * 0.9, -0.45, -1.0)
        k.add("jabot", spike((x, -0.192 + 0.012 * abs(ang), 0.198), d, l, 0.034, 0.014), FLUFF, "body", fuse=False,
              surface="fur")
    # the tail: a dark stub rising behind, a golden plume of points turned out to +X
    ex = unit(0.45, 0.7, 0.0)
    root, top = np.array([0.0, 0.205, 0.19]), np.array([0.02, 0.232, 0.255])
    k.add("tail", tube([root, (0.01, 0.222, 0.225), top], [0.028, 0.03, 0.026]), CRUST, "tail")
    for a, l in ((0.0, 0.1), (0.55, 0.085), (-0.55, 0.085), (1.05, 0.065), (-1.05, 0.065)):
        d = unit(*(np.array([0, 0.12, 1.0]) * math.cos(a) + ex * math.sin(a)))
        k.add("plume", spike(top + d * 0.008, d, l, 0.026, 0.012), GOLD, "tail", fuse=False, surface="fur")
    k.build(voxel=VOXEL, smooth=4, band=0.004, wband=0.035)
    E, en = F.on_front(HC, HR, 0.046, 0.296)
    en = unit(en[0] * 0.9, en[1], en[2] * 0.5)
    k.eye_states(lambda s: F.lay_eyes(k, s, E, en, (0.0155, 0.017), F.bun_eye(EYE, glint=(0.25, 0.3, 0.32),
                                                                              small=(-0.3, -0.35, 0.14)),
                                      ORANGE, EYE))
    k.export(out)


if __name__ == "__main__":
    dachsbun(*args())
