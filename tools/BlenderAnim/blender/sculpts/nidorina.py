# Nidorina sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/nidorina.py -- <the export's Nidorina.glb> <out.glb>
# Grown from nidoranf.py (its spines, claws, plated ears with their inset lining, glint and red eye). From the
# references: a heavier pale blue quadruped, the chest high and the head carried up on it, a cream belly and chest;
# a bigger head with a short broad snout, the mouth a closed line with a small white fang standing up at each corner;
# red eyes under a scowling lid; great ears standing up and out, a mid-blue lining with a dark navy crescent on the
# outer side and points along their rims; big spines down the back, the first and greatest over the shoulders behind
# the head; dark blue angular spots on the haunches and the flanks; thick legs, three white claws a foot.
# The pictures overrule what Nidoran had: no forehead horn, no whiskers, no buck teeth.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, inside, unit, polygon, eye_look, sibling, args

nf = sibling("nidoranf")
on_ell, spine, claw, glint = nf.on_ell, nf.spine, nf.claw, nf.glint
pointed_ear, bold_spine = nf.pointed_ear, nf.bold_spine

SKIN, BELLY, SPOT, SPINE_TIP = "#A6D2EC", "#E6EEF2", "#2C6A9E", "#82B2D4"
LINING, RIM = "#3F84C8", "#1C4F90"
CLAW, FANG = nf.CLAW, nf.TOOTH
MOUTHLINE = srgb("#2A3550")
RED, PUPIL, INK, WHITE = nf.RED, nf.PUPIL, nf.INK, nf.WHITE

TORSO_C, TORSO_R = np.array([0.0, 0.045, 0.215]), np.array([0.155, 0.175, 0.152])
CHEST_C, CHEST_R = np.array([0.0, -0.085, 0.275]), np.array([0.132, 0.112, 0.135])
HEAD_C, HEAD_R = np.array([0.0, -0.155, 0.41]), np.array([0.142, 0.128, 0.122])
SNOUT_C, SNOUT_R, SNOUT_T = np.array([0.0, -0.255, 0.372]), np.array([0.09, 0.085, 0.068]), (6, 0, 0)


def eye_scowl(rim=0.84, lid_edge=(0.22, 0.42)):
    """Nidoran's red eye under a lid sloping down toward the nose (-u), its edge a dark line: a scowl."""
    base = nf.eye_open(rim=rim)
    lid = srgb(SKIN)
    def eye(u, v):
        c = base(u, v)
        edge = lid_edge[0] + lid_edge[1] * u
        c[v > edge + 0.14] = lid
        c[np.abs(v - edge) <= 0.14] = INK
        return c
    return eye


def nidorina(src, out):
    k = Kit(src, coat="fur")

    # The body: a deep pear down to within a few hundredths of the ground, the chest high under the head, a cream belly
    k.add("torso", ell(TORSO_C, TORSO_R, deform=nf.pear), SKIN, "body")
    k.add("belly", ell((0, 0.03, 0.13), (0.145, 0.17, 0.105)), SKIN, "body")
    k.add("chest", ell(CHEST_C, CHEST_R), SKIN, "body")
    k.add("tailnub", ell((0, 0.235, 0.21), (0.045, 0.04, 0.04)), SKIN, "tail")
    def belly(P):
        e = np.sqrt((P[:, 0] / 0.1) ** 2 + ((P[:, 2] - 0.2) / 0.14) ** 2) - 1
        front = inside(e, 0.004) * inside(P[:, 1] + 0.05, 0.004)
        under = inside(P[:, 2] - 0.1, 0.004) * inside(np.abs(P[:, 0]) - 0.085, 0.004) * inside(P[:, 1] - 0.14, 0.004)
        return np.maximum(front, under) * inside(P[:, 2] - 0.33, 0.004)
    k.paint(belly, BELLY, on=["torso", "chest", "belly"])

    # The head: a broad crown, a short broad snout, full cheeks
    k.add("head", ell(HEAD_C, HEAD_R), SKIN, "head")
    k.add("snout", ell(SNOUT_C, SNOUT_R, rot=SNOUT_T), SKIN, "head")
    k.add("cheek", ell((0, -0.17, 0.355), (0.112, 0.08, 0.072)), SKIN, "head")
    k.add("neck", ell((0, -0.11, 0.33), (0.105, 0.09, 0.09)), SKIN, "head")

    # The legs: squat, fat shoulders and haunches fused into the flanks, a short stub of shin, a wide foot, three
    # white claws a foot
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"shoulder{side}", ell(np.array((0.1, -0.105, 0.15)) * m, (0.078, 0.088, 0.11)), SKIN, f"leg{side}")
        k.add(f"leg{side}", tube(np.array([(0.105, -0.125, 0.085), (0.107, -0.13, 0.04)]) * m, [0.062, 0.058]), SKIN,
              f"leg{side}")
        k.add(f"paw{side}", ell(np.array((0.107, -0.15, 0.026)) * m, (0.066, 0.075, 0.028)), SKIN, f"leg{side}")
        k.add(f"haunch{side}", ell(np.array((0.105, 0.15, 0.14)) * m, (0.1, 0.12, 0.125)), SKIN, f"hind{side}")
        k.add(f"hind{side}", tube(np.array([(0.112, 0.16, 0.08), (0.113, 0.155, 0.04)]) * m, [0.062, 0.058]), SKIN,
              f"hind{side}")
        k.add(f"hpaw{side}", ell(np.array((0.113, 0.135, 0.026)) * m, (0.066, 0.075, 0.028)), SKIN, f"hind{side}")
        for foot, y, bone in (("f", -0.218, f"leg{side}"), ("h", 0.063, f"hind{side}")):
            for j, dx in enumerate((-0.026, 0.0, 0.026)):
                b = np.array((0.109 + dx, y, 0.018))
                k.add(f"claw{foot}{side}{j}", claw(b * m, np.array((dx * 0.8, -1, -0.5)) * m, 0.027, 0.018), CLAW,
                      bone, fuse=False, surface="shell")

    # The ears: Nidoran's, larger, a mid-blue lining inside a dark navy rim, the body's colour round both
    EB = on_ell(HEAD_C, HEAD_R, (0.78, 0.15, 0.62))
    d = unit(0.6, 0.5, 0.8)
    pointed_ear(k, EB - d * 0.035, d, (0.55, -1.0, 0.25), 0.28, 0.23, 0.026, SKIN, LINING, RIM,
                root=(EB - d * 0.025, 0.038))

    # A few bold spines, the greatest behind the head, laid back, a shade darker at the tip
    for j, (dd, lean, length, width) in enumerate([((0, -0.45, 1), (0, 0.6, 1), 0.15, 0.08),
                                                   ((0, 0.25, 1), (0, 0.8, 1), 0.1, 0.06),
                                                   ((0, 0.85, 0.6), (0, 1, 0.6), 0.075, 0.05)]):
        bold_spine(k, f"spine{j}", on_ell(TORSO_C, TORSO_R, dd), lean, length, width, SKIN, SPINE_TIP, "body",
                   sink=0.02)
    bold_spine(k, "flankspine", on_ell(TORSO_C + (0, 0.02, 0.0), TORSO_R * (1.08, 1.0, 1.04), (0.72, 0.5, 0.48)),
               (0.8, 0.6, 0.45), 0.07, 0.048, SKIN, SPINE_TIP, "body", mirror=True, sink=0.018)

    # Dark blue angular spots on the haunches and the flanks
    ANGLED = [polygon([(-1, 0.3), (-0.2, 1), (0.9, 0.5), (0.6, -0.8), (-0.5, -1)]),
              polygon([(-0.9, -0.2), (0, 1), (1, 0.4), (0.5, -1)]),
              polygon([(-1, 0.6), (0.7, 1), (1, -0.4), (-0.3, -1)])]
    for j, (d, half) in enumerate([((0.85, 0.45, 0.2), (0.04, 0.03)), ((0.75, 0.75, -0.1), (0.026, 0.022)),
                                   ((0.6, 0.35, 0.75), (0.028, 0.022)), ((0.9, -0.05, -0.15), (0.022, 0.018))]):
        n = unit(*d)
        k.decal(on_ell(TORSO_C, TORSO_R, n), n, half, SPOT, eps=0.003, outline=ANGLED[j % 3], rings=8, segs=40,
                mirror=True)

    # The mouth: a closed line across the snout's foot, its ends turned down; a white fang up at each corner
    dm = unit(0, -0.8, -0.6)
    MC = on_ell(SNOUT_C, SNOUT_R, dm, SNOUT_T)
    skin = srgb(SKIN)
    def mouth(u, v):
        c = np.tile(skin, (len(u), 1))
        c[(np.abs(v - (0.2 - 0.55 * u * u)) < 0.22) & (np.abs(u) < 0.95)] = MOUTHLINE
        return c
    k.decal(MC, dm, (0.068, 0.03), mouth, eps=0.003, rings=10, segs=48)
    for s in (-1, 1):
        c = on_ell(SNOUT_C, SNOUT_R, unit(s * 0.62, -0.7, -0.5), SNOUT_T) + (0, -0.002, -0.004)
        k.add(f"fang{'R' if s > 0 else 'L'}", spine(c, (s * 0.1, -0.4, 1), 0.036, 0.02, sink=0.008), FANG,
              "head", fuse=False, surface="shell")

    # The eyes: Nidoran's red ones, larger, under a scowling lid
    de = unit(0.58, -0.75, 0.24)
    E = on_ell(HEAD_C, HEAD_R, de)
    half = (0.042, 0.034)
    eye = eye_scowl()
    LID = srgb(SKIN)
    def eyes(state):
        k.decal(E, de, half, eye_look(state, eye, LID, INK, line=0.15), up=(0, 0.25, 1), eps=0.005, rings=20,
                segs=56, mirror=True)
        if state == "Open":
            glint(k, E, de, half, at=(-0.3, 0.05), size=0.24, eps=0.007)

    k.build(voxel=0.0058, smooth=4)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    nidorina(*args())
