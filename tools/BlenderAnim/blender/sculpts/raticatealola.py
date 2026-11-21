# Raticate-Alola sculpted from nothing on the skeleton and clips of the game's own export of the form (sculptkit.py):
#   blender --background --factory-startup --python sculpts/raticatealola.py -- <the export's Raticate-Alola.glb> <out.glb>
# Raticate's Alolan form, grown from raticate.py and rattata.py (their teeth, ear linings, cut red eyes, ringed tail and
# big flat feet): a very fat charcoal rat sitting up, one round mass with the head run into it, its fur ragged in
# spikes over the crown, the back and the flanks; two great puffed tan cheeks either side of the muzzle, the largest
# masses of the head, with four big teeth showing between them and two thick whisker spikes out of each; a tan beard
# of spikes under the chin; red eyes under a scowling brow; pointed ears lined brown; a brown belly with a ragged top
# edge; small tan clawed hands held at the chest; tan feet; a long tan tail with darker rings.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, inside, unit, tri, ring_band, args, sibling

r = sibling("rattata")
rc = sibling("raticate")
ra = sibling("rattataalola")

FUR, CREAM, BELLY, LINING, NOSE = "#4A4C48", "#E6DABA", "#6E5040", "#7A5848", "#2C2C2A"
TAIL, RING, CLAW = "#E2D6B6", "#B8A888", "#F2EAD8"
IRIS = "#D83228"
H = np.array([0, -0.08, 0.515])           # the head's centre
HR = np.array([0.16, 0.15, 0.135])
EC = np.array([0.15, -0.02, 0.665])       # the ear's centre (+X)
ER = np.array([0.074, 0.024, 0.082])
EA = (0, 30, 22)
spike = ra.spike


def raticate_alola(src, out):
    k = Kit(src, coat="fur")

    # One great round mass sitting up, the head run into it with no neck
    def pear(V):
        V = V.copy(); s = 1 - 0.08 * V[:, 2]
        V[:, 0] *= s; V[:, 1] *= s
        return V
    k.add("body", ell((0, 0.02, 0.285), (0.255, 0.235, 0.262), deform=pear), FUR, "body")
    k.add("head", ell(H, HR), FUR, "head")
    k.add("brow", ell((0, -0.13, 0.565), (0.12, 0.08, 0.06)), FUR, "head")

    # The ragged fur: spikes laid back over the crown and down the back, and out at the flanks
    i = 0
    for az, el, ln in ((0, 70, 0.07), (25, 62, 0.06), (-25, 62, 0.06), (50, 48, 0.055), (-50, 48, 0.055),
                       (0, 50, 0.06), (75, 35, 0.05), (-75, 35, 0.05)):
        a, e = math.radians(az), math.radians(el)
        d = unit(math.sin(a) * math.cos(e), -math.cos(a) * math.cos(e) * 0.2 + 0.6, math.sin(e))
        p = H + np.array([math.sin(a) * math.cos(e) * HR[0], 0.03, math.sin(e) * HR[2]]) * 0.95
        k.add(f"crown{i}", spike(p, d, ln, 0.05, 0.03), FUR, "head", fuse=False); i += 1
    B = np.array([0, 0.02, 0.285]); BR = np.array([0.255, 0.235, 0.262])
    for az, el, ln in ((150, 30, 0.06), (180, 40, 0.065), (210, 30, 0.06), (120, 15, 0.055), (240, 15, 0.055),
                       (95, 0, 0.05), (265, 0, 0.05), (165, 5, 0.05), (195, 5, 0.05), (110, -20, 0.045),
                       (250, -20, 0.045), (180, 65, 0.055)):
        a, e = math.radians(az), math.radians(el)
        n = np.array([math.sin(a) * math.cos(e), -math.cos(a) * math.cos(e), math.sin(e)])
        p = B + n * BR * 0.96
        d = unit(*(n + np.array([0, 0.2, -0.2])))
        k.add(f"back{i}", spike(p, d, ln * 1.45, 0.06, 0.036), FUR, "body", fuse=False); i += 1

    # Pointed ears tilted out, lined brown
    def point(V):
        V = V.copy(); t = np.clip(V[:, 2], 0, 1)
        V[:, 0] *= 1 - 0.55 * t ** 2
        V[:, 2] *= 1 + 0.25 * t
        return V
    k.add("ear", ell(EC, ER, rot=EA, deform=point), FUR, "ear{s}", mirror=True)
    k.add("earroot", ell((0.11, -0.03, 0.61), (0.045, 0.03, 0.05)), FUR, "ear{s}", mirror=True)
    r.ear_lining(k, EC, EA, (0.048, 0.052), -0.012, LINING)

    # The great puffed cheeks, the largest masses of the head, and the muzzle's bridge between them
    k.add("cheek", ell((0.118, -0.2, 0.43), (0.12, 0.105, 0.108)), CREAM, "head", mirror=True)
    k.add("bridge", ell((0, -0.24, 0.5), (0.05, 0.07, 0.05), rot=(-10, 0, 0)), FUR, "head")
    k.add("nose", ell((0, -0.305, 0.508), (0.026, 0.016, 0.019)), NOSE, "head", fuse=False, surface="skin")
    # four big teeth between the cheeks, two over two
    r.teeth(k, (0, -0.3, 0.478), 0.034, 0.048)
    for s, n in ((1, "R"), (-1, "L")):
        k.add(f"lowtooth{n}", ell((s * 0.034 * 0.55, -0.296, 0.405), (0.017, 0.011, 0.023), rot=(6, 0, 0),
                                   deform=r.boxy(5) if hasattr(r, "boxy") else None), r.WHITE, "head", fuse=False,
              surface="shell")
    k.add("gums", ell((0, -0.27, 0.43), (0.04, 0.03, 0.05)), "#3A2228", "head")
    # two thick whisker spikes out of each cheek, out and up
    k.add("whiskA", spike((0.2, -0.24, 0.46), (1, -0.3, 0.55), 0.13, 0.046, 0.034), CREAM, "head", fuse=False,
          mirror=True)
    k.add("whiskB", spike((0.21, -0.22, 0.41), (1, -0.15, 0.05), 0.09, 0.04, 0.03), CREAM, "head", fuse=False,
          mirror=True)
    # a beard of tan spikes under the chin
    for j, (x, ln) in enumerate(((0, 0.075), (0.035, 0.065), (-0.035, 0.065), (0.065, 0.05), (-0.065, 0.05))):
        k.add(f"beard{j}", spike((x, -0.25, 0.37), (x * 2.5, -0.35, -1), ln, 0.034, 0.024), CREAM, "head", fuse=False)

    # Small arms with tan hands held at the chest, three claws each
    k.add("arm", tube([(0.13, -0.12, 0.36), (0.13, -0.2, 0.335), (0.12, -0.24, 0.32)], [0.045, 0.04, 0.034]), FUR,
          "arm{s}", mirror=True)
    k.add("hand", ell((0.118, -0.26, 0.315), (0.036, 0.03, 0.03)), CREAM, "arm{s}", mirror=True)
    for j, dx in enumerate((-0.02, 0.0, 0.02)):
        k.add(f"claw{j}", spike((0.118 + dx, -0.282, 0.31), (dx * 4, -0.5, -1), 0.03, 0.014, 0.01), CLAW, "arm{s}",
              fuse=False, mirror=True, surface="shell")
    k.paint(lambda P: inside(-0.24 - P[:, 1], 0.003) * (P[:, 2] > 0.26) * (P[:, 2] < 0.37) * (np.abs(P[:, 0]) > 0.06),
            CREAM, on=["arm", "hand"])

    # Big flat tan feet, three toes forward with claws
    k.add("thigh", ell((0.14, -0.03, 0.13), (0.1, 0.11, 0.1)), FUR, "leg{s}", mirror=True)
    k.add("foot", ell((0.15, -0.11, 0.028), (0.062, 0.1, 0.028)), CREAM, "leg{s}", mirror=True)
    for j, ang in enumerate((-22, 0, 22)):
        d = np.array([math.sin(math.radians(ang)), -math.cos(math.radians(ang)), 0])
        b0 = np.array([0.15, -0.15, 0.024])
        k.add(f"toe{j}", tube([b0, b0 + d * 0.06 + (0, 0, 0.004), b0 + d * 0.095], [0.019, 0.017, 0.013]), CREAM,
              "leg{s}", mirror=True)
        tip = b0 + d * 0.1
        k.add(f"toeclaw{j}", spike(tip, d + (0, 0, -0.5), 0.028, 0.014, 0.01), CLAW, "leg{s}", fuse=False,
              mirror=True, surface="shell")
    k.paint(lambda P: inside(P[:, 2] - 0.052, 0.003) * (P[:, 2] < 0.1), CREAM, on=["foot", "toe", "thigh"])

    # The brown belly: the lower front, its top edge ragged
    def belly(P):
        a = np.arctan2(P[:, 0], -(P[:, 1] - 0.02))
        edge = 0.235 + 0.035 * tri(a / 0.32) - 0.03 * np.abs(a)
        return (inside(P[:, 2] - edge, 0.003) * inside(np.abs(a) - 1.35, 0.01) * (P[:, 2] > 0.04))
    k.paint(belly, BELLY, on=["body"])

    # The long tail lying out behind and curving up to the right, tan with darker rings
    TP = [(0, 0.2, 0.12), (0.03, 0.33, 0.06), (0.12, 0.47, 0.05), (0.24, 0.55, 0.1), (0.32, 0.57, 0.22),
          (0.35, 0.55, 0.36)]
    TR = [0.038, 0.032, 0.027, 0.022, 0.018, 0.014]
    k.add("tail", tube(TP, TR, seg=16), TAIL, "tail")
    L = rc.spline(TP)
    R = np.interp(np.linspace(0, 1, len(L)), np.linspace(0, 1, len(TR)), TR)
    for j in range(10, len(L) - 4, 5):
        d = L[j + 1] - L[j - 1]
        k.add(f"ring{j}", ring_band(L[j], d, R[j] + 0.0015, 0.008, thick=0.004, nu=41), RING, "tail", fuse=False)

    # Red eyes above the cheeks, cut along the top by the scowling brow
    CW, CF = srgb(r.WHITE), srgb(FUR)
    look = r.red_eye(CW, srgb(IRIS), srgb(r.PUPIL))
    de = unit(0.5, -0.8, 0.32)
    E = H + de / np.linalg.norm(de / HR)
    top = lambda u: 0.2 + 0.4 * u
    ftop = lambda u: -0.02 + 0.5 * u
    def eyes(state):
        r.lay_eyes(k, state, E, de, (0.036, 0.034), look, top, ftop, CF, srgb("#E8E0CC"))

    k.build(voxel=0.0075, band=0.006)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    raticate_alola(*args())
