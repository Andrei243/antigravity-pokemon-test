# Tyrunt sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/tyrunt.py -- <the export's Tyrunt.glb> <out.glb>
#
# From the references: a small grey-brown tyrannosaur leaning low and forward, its head enormous (as long as the body
# and as tall as the whole figure with its jaws open, wider than the body), carried in front and low, the lower jaw
# almost on the ground. The jaws gape on a pink mouth, two white fangs down from the front of the upper jaw and two up
# from the lower. A pale grey-white strap runs from the top of the head behind the eye down the back of the cheek and
# round the whole lower jaw as its rim. Two thick horns on the crown, brown at the root and orange at the tip, and such
# a spike on the tip of the short tail, which stands up behind the rump. A round white eye with a black rim and pupil,
# cut across its top by a brow that slopes to the snout; a nostril on the snout's round tip. A ruff of big white
# spikes round the back of the neck, pointing back. Tiny dark purple-grey arms tucked under the neck with two white
# claws; thick brown thighs fused into the hips, short shins, broad feet of three white claws and a spur behind.
# A few paler brown patches over the coat.
# Written to be grown from (tyrantrum.py): the head's frame is `jaws`, the spikes `spike` and `horn`.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from mathutils import Euler
from sculptkit import Kit, ell, tube, boxy, srgb, inside, unit, args, sibling

bg = sibling("bagon")           # its fang (a flat-faced point), its eye under a brow

BROWN, PATCH = "#7C6355", "#8E7564"
STRAP, RUFF = "#D9D7DD", "#F3F2F5"
ORANGE = "#F2881C"
ARM = "#5C4A60"
MOUTH, THROAT = "#E28C96", "#B2404E"
TOOTH = "#F7F5F1"
CLAW = "#F4F3F0"


def spike(k, name, base, d, length, width, color, bone, up=(0, 0, 1)):
    """One flat-faced point: a kite at its root, wide across `up` x d."""
    k.add(name, bg.fang(np.array(base, float), d, length, width, up=up), color, bone, fuse=False, surface="shell")


def horn(k, name, base, d, length, width, bone, root=BROWN, tip=ORANGE, up=(0, 1, 0)):
    """A two-coloured horn: its root of the body's colour, its tip of another, each a solid of its own."""
    base, d = np.array(base, float), unit(*d)
    spike(k, name + "root", base, d, length * 0.6, width, root, bone, up=up)
    spike(k, name + "tip", base + d * length * 0.3, d, length * 0.7, width * 0.66, tip, bone, up=up)


def frame(rot):
    return np.array(Euler([math.radians(a) for a in rot]).to_matrix())


def jaws(k, J):
    """
    The head: a skull, the upper jaw forward, the lower jaw dropped open (its upper face the mouth's colour, so only a
    rim of its own colour shows), a mouth set back between them, the throat deep red. J holds the frame's numbers and
    colours (body, its surface, the jaw's).
    """
    body, surf = J["body"], J["surface"]
    k.add("skull", ell(J["S"], J["SR"], deform=boxy(2.2)), body, "head", surface=surf)
    k.add("snout", ell(J["snout"], J["snoutR"], rot=(J["snoutRot"], 0, 0)), body, "head", surface=surf)
    k.add("cheek", ell(J["cheek"], J["cheekR"]), body, "head", mirror=True, surface=surf)
    k.add("jawroot", ell(J["root"], J["rootR"]), body, "head", surface=surf)
    k.add("jaw", ell(J["jaw"], J["jawR"], rot=(J["jawRot"], 0, 0)), J["jawColor"], "head", surface=surf)
    k.add("mouth", ell(J["mouth"], J["mouthR"], rot=(J["mouthRot"], 0, 0)), MOUTH, "head", surface="skin")
    jc, jr, M = np.array(J["jaw"]), np.array(J["jawR"]), frame((J["jawRot"], 0, 0))

    def jaw_top(P):
        q = (P - jc) @ M                         # the jaw's own frame
        rho = np.hypot(q[:, 0] / jr[0], q[:, 1] / jr[1])
        return inside(rho - J["rim"], 0.02) * (1 - inside(0.15 - q[:, 2] / jr[2], 0.05))

    k.paint(jaw_top, MOUTH, on=["jaw"])
    mc = np.array(J["mouth"])
    k.paint(lambda P: inside(mc[1] + J["mouthR"][1] * J.get("throat", 0.0) - P[:, 1], 0.004) *
            inside(np.abs(P[:, 0]) - J["mouthR"][0] * 0.75, 0.004), THROAT, on=["mouth"])


H0 = np.array([0.0, -0.04, 0.2])       # the head's joint
G = 1.15                               # the head grown about its joint


def h(p):
    """A point of the head, grown about the head's joint."""
    return H0 + (np.array(p, float) - H0) * G


def r(v):
    return np.array(v, float) * G


def tyrunt(src, out):
    k = Kit(src, coat="scales")

    # --- the head: enormous, carried low in front, the jaws open ---------------------------------------------------
    J = dict(body=BROWN, surface="scales", jawColor=STRAP, rim=0.72,
             S=h((0, -0.13, 0.205)), SR=r((0.088, 0.095, 0.075)),
             snout=h((0, -0.22, 0.19)), snoutR=r((0.07, 0.085, 0.05)), snoutRot=-6,
             cheek=h((0.058, -0.13, 0.155)), cheekR=r((0.042, 0.065, 0.055)),
             root=h((0, -0.115, 0.12)), rootR=r((0.066, 0.06, 0.05)),
             jaw=h((0, -0.185, 0.092)), jawR=r((0.064, 0.088, 0.024)), jawRot=22,
             mouth=h((0, -0.19, 0.13)), mouthR=r((0.05, 0.085, 0.034)), mouthRot=16)
    jaws(k, J)
    S, SR = J["S"], J["SR"]
    k.add("nose", ell(h((0, -0.262, 0.198)), r((0.052, 0.04, 0.038))), BROWN, "head")
    k.add("brow", ell(h((0.058, -0.165, 0.248)), r((0.036, 0.05, 0.02)), rot=(-18, 0, -14)), BROWN, "head",
          mirror=True)

    # The pale strap: from the top of the head behind the eye down the back of the cheek to the jaw's hinge
    sc = h((0, -0.2, 0.14))

    def strap(P):
        y, z = (P[:, 1] - sc[1]) / G, (P[:, 2] - sc[2]) / G
        rr = np.hypot(y, z)
        th = np.arctan2(z, y)                       # 0 straight back, + up
        return inside(np.abs(rr - 0.1) - 0.02, 0.003) * inside(th - 1.4, 0.01) * inside(-1.1 - th, 0.01) * \
            inside(0.03 - np.abs(P[:, 0]), 0.004)

    # fangs: two down from the front of the upper jaw, two up from the lower
    for s in (1, -1):
        side = "R" if s > 0 else "L"
        spike(k, f"fang{side}", h((0.038 * s, -0.262, 0.152)), (0.12 * s, -0.15, -1), 0.04, 0.021, TOOTH, "head",
              up=(0, 1, 0))
        spike(k, f"tusk{side}", h((0.04 * s, -0.255, 0.098)), (0.1 * s, -0.25, 1), 0.03, 0.019, TOOTH, "head",
              up=(0, 1, 0))

    # two thick horns on the crown, brown with orange tips
    for s in (1, -1):
        horn(k, f"horn{'R' if s > 0 else 'L'}", (0.05 * s, -0.14, 0.282), (0.25 * s, 0.2, 1), 0.08, 0.05, "head")

    # the white ruff round the back of the neck: a few big spikes pointing back
    ruff = [((0.0, -0.06, 0.27), (0, 0.7, 0.7), 0.07), ((0.06, -0.065, 0.235), (0.6, 0.65, 0.45), 0.07),
            ((0.08, -0.07, 0.165), (0.85, 0.55, -0.05), 0.06), ((0.068, -0.08, 0.105), (0.6, 0.45, -0.65), 0.045)]
    for i, (p, d, ln) in enumerate(ruff):
        for s in ((1, -1) if p[0] else (1,)):
            spike(k, f"ruff{i}{'R' if s > 0 else 'L'}", (p[0] * s, p[1], p[2]), (d[0] * s, d[1], d[2]), ln, 0.05,
                  RUFF, "head", up=(0, 1, 0))

    # --- the body: a low egg leaning forward, smaller than the head; a short tail standing up --------------------
    k.add("neck", ell((0, -0.06, 0.165), (0.072, 0.07, 0.07)), BROWN, "body")
    k.add("torso", ell((0, 0.025, 0.17), (0.085, 0.095, 0.072), rot=(-8, 0, 0)), BROWN, "body")
    k.add("hips", ell((0, 0.075, 0.16), (0.08, 0.07, 0.07)), BROWN, "body")
    k.add("tail", tube([(0, 0.105, 0.175), (0, 0.155, 0.205), (0, 0.18, 0.245)], [0.048, 0.032, 0.02]), BROWN, "tail")
    horn(k, "tailspike", (0, 0.18, 0.245), (0, 0.4, 1), 0.065, 0.04, "tail", up=(1, 0, 0))

    # --- the arms: tiny, dark, tucked under the neck behind the jaw, two white claws ------------------------------
    k.add("arm", tube([(0.045, -0.04, 0.16), (0.068, -0.055, 0.13), (0.074, -0.068, 0.108)], [0.021, 0.018, 0.017]),
          ARM, "arm{s}", mirror=True)
    for s in (1, -1):
        side = "R" if s > 0 else "L"
        for j, dx in enumerate((-0.008, 0.008)):
            spike(k, f"hclaw{side}{j}", (s * (0.074 + dx), -0.078, 0.1), (s * dx * 6, -0.6, -0.8), 0.022, 0.013,
                  CLAW, f"arm{side}", up=(0, 0, 1))

    # --- the legs: thick thighs fused into the hips, short shins, broad feet ------------------------------------
    k.add("thigh", ell((0.062, 0.05, 0.13), (0.05, 0.072, 0.072)), BROWN, "leg{s}", mirror=True)
    k.add("shin", tube([(0.066, 0.055, 0.085), (0.068, 0.045, 0.036)], [0.033, 0.029]), BROWN, "leg{s}", mirror=True)
    k.add("foot", ell((0.068, 0.0, 0.018), (0.035, 0.062, 0.019)), BROWN, "leg{s}", mirror=True)
    for s in (1, -1):
        side = "R" if s > 0 else "L"
        for j, dx in enumerate((-0.022, 0.0, 0.022)):
            spike(k, f"toe{side}{j}", (s * (0.068 + dx), -0.052, 0.014), (s * dx * 8, -1, -0.2), 0.032, 0.017,
                  CLAW, f"leg{side}", up=(0, 0, 1))
        spike(k, f"spur{side}", (s * 0.068, 0.055, 0.016), (0, 1, -0.2), 0.026, 0.015, CLAW, f"leg{side}",
              up=(0, 0, 1))

    # --- a few paler patches over the coat ------------------------------------------------------------------------
    blots = [((0.07, 0.0, 0.2), (0.05, 0.045, 0.03)), ((0.085, 0.07, 0.14), (0.035, 0.04, 0.03)),
             (tuple(h((0.045, -0.24, 0.23))), (0.035, 0.03, 0.022))]

    def blotches(P):
        m = np.zeros(len(P))
        for c, rad in blots:
            for s in (1, -1):
                q = (P - np.array([c[0] * s, c[1], c[2]])) / np.array(rad)
                wob = 0.07 * np.sin(3 * np.arctan2(q[:, 2], q[:, 1]))
                m = np.maximum(m, inside(np.linalg.norm(q, axis=1) - 1 - wob, 0.05))
        return m

    k.paint(blotches, PATCH, on=["torso", "hips", "thigh", "snout", "nose"])
    k.paint(strap, STRAP, on=["skull", "cheek", "jawroot", "snout"])

    # --- a small nostril on each side of the snout's tip ------------------------------------------------------
    k.decal(h((0.028, -0.292, 0.208)), unit(0.35, -1, 0.15), (0.0065, 0.005), "#3A2A26", eps=0.003, rings=6, segs=24,
            mirror=True)

    # --- the eyes: round and white, a black rim and pupil, the brow cutting the top --------------------------
    de = unit(0.72, -0.6, 0.34)
    E = bg.on_ell(S, SR, de) + np.array([0, -0.004, -0.004])
    cut = bg.brow_cut(0.62, 0.45)
    look = bg.angry_eye(cut, rim=0.76, pupil_c=(-0.18, -0.06), pupil_r=0.4)
    eyes = bg.eyes_under_brow(k, E, de, (0.035, 0.037), look, cut, srgb(BROWN))

    k.build(voxel=0.0034, smooth=3, band=0.005, wband=0.03)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    tyrunt(*args())
