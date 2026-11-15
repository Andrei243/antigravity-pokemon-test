# Entei sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/entei.py -- <the export's Entei.glb> <out.glb>
#
# Built on raikou.py's frame (its trunk, legs, hugging plates and eyes, through sibling("raikou")). From the references:
# a massive brown lion standing tall, the trunk and chest heavy, the legs thick. A shaggy brown mane hangs from the
# neck over the shoulders like a cape, its hem ragged; a cream bib runs down the chest to a grey-white ragged fringe
# between the forelegs. The head is brown with a red plate over the face, a V pointing down to the nose with its upper
# corners swept out over the brows; a yellow flame of a crest is laid back over the crown; a grey moustache hooks down
# each side of the cream muzzle. The eyes are red under the plate's edge. Grey blades fan up and back from the back
# out of a grey-green cloud of smoke that trails behind as the tail. Black shackles round each leg above the paws, and
# a brown fringe of fur over each pale blue-grey paw; the hind legs dark brown below the knee.
# Bones: body (0, 0.02, 0.47); head (0, -0.26, 0.6); legs (+-0.11, -0.2, 0.42); hind legs (+-0.11, 0.22, 0.42);
# tail (0, 0.29, 0.5).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, blade, hull, lathe, outward, srgb, inside, tri, unit, lumpy, args, sibling)

r = sibling("raikou")

BROWN = "#A0602E"
DARK = "#5A3A2E"
CREAM = "#F2E2A4"
FRINGE = "#DCE2E8"
RED = "#D8283A"
GOLD = "#F4CC3C"
GREY = "#B4BCC8"
SMOKE = "#B8C4B0"
SHACKLE = "#2C2E34"
PAW = "#BCCADC"
EYE, IRIS, PUPIL = "#F6F4EE", "#D23A26", "#1A1012"

LEG_X = 0.11
FORE_Y, HIND_Y = -0.2, 0.22
LEG_TOP = 0.42
LEG_R = (0.08, 0.068, 0.062)
PAW_R = (0.078, 0.088, 0.04)
BODY_Z = 0.45
# The head is long, the muzzle within it, so the face plate hugs it from brow to nose
H = np.array([0.0, -0.31, 0.7])
HR = np.array([0.12, 0.165, 0.118])
HC = H + (0, -0.03, 0.0)
EYE_DIR = unit(0.62, -0.68, 0.32)
EYE_R = 0.026
E = HC + np.array([0.0, 0.0, 0.0])


def on_head(d):
    p, n = r.on_ell(HC, HR, d)
    return p, n


def add_head(k):
    k.add("head", ell(HC, HR), BROWN, "head")
    k.add("jaw", ell(H + (0, -0.08, -0.08), (0.075, 0.08, 0.05)), CREAM, "head")
    k.add("neck", tube([(0, -0.16, 0.5), (0, -0.23, 0.6), (0, -0.27, 0.66)], [0.13, 0.12, 0.11]), BROWN, "head")
    # The muzzle's cream under the plate, front and lower half of the long head
    def muzzle(P):
        q = (P - HC) / HR
        return inside(q[:, 2] + 0.25 - 0.5 * np.clip(-q[:, 1] - 0.4, 0, 1), 0.02) * (q[:, 1] < -0.2) \
            * (np.linalg.norm(q, axis=1) < 1.25)
    k.paint(muzzle, CREAM, on=["head", "jaw"])


def add_plate(k):
    """The red face plate: a V hugging the head from the nose's top up over the brows, its corners swept out."""
    def keep(D):
        return (D[:, 1] < -0.15) & (D[:, 2] > -0.25) & (D[:, 2] < 0.66) & (np.abs(D[:, 0]) < (D[:, 2] + 0.22) * 0.88)
    k.add("plate", r.plate(HC, HR, keep, lift=0.02, under=0.008), RED, "head", fuse=False, surface="shell")


def add_crest(k):
    """The yellow flame laid back over the crown: three flat-faced tongues."""
    top, _ = on_head((0, -0.5, 0.85))
    for i, (dx, tip) in enumerate(((0.0, (0.0, 0.25, 0.085)), (0.045, (0.075, 0.2, 0.05)), (-0.045, (-0.075, 0.2, 0.05)))):
        b = top + (dx, 0.0, -0.012)
        t = top + np.array(tip)
        pts = [b + (0.024, -0.03, 0), b + (-0.024, -0.03, 0), b + (0.022, 0.03, -0.008), b + (-0.022, 0.03, -0.008),
               b + (0, -0.01, 0.03), (b + t) / 2 + (0, -0.01, 0.03), t]
        k.add(f"crest{i}", hull(pts), GOLD, "head", fuse=False, surface="shell")


def add_moustache(k):
    pts = [H + (0.035, -0.19, -0.005), H + (0.075, -0.18, -0.045), H + (0.098, -0.15, -0.11), H + (0.09, -0.12, -0.17)]
    k.add("moustache", outward(blade(pts, [0.022, 0.028, 0.022, 0.007], (0, 1, 0.3), 0.55)), GREY, "head",
          fuse=False, mirror=True, surface="shell")


def add_mane(k):
    """The brown cape over the shoulders and the cream bib down the chest, each ragged at its hem."""
    k.add("manenape", ell((0, -0.18, 0.62), (0.16, 0.12, 0.12), deform=lumpy(5, 3, 0.06)), BROWN, "head")
    k.add("cape", ell((0.12, -0.2, 0.48), (0.085, 0.13, 0.17), deform=lumpy(4, 3, 0.06)), BROWN, "body", mirror=True)
    for s in (1, -1):
        sd = "R" if s > 0 else "L"
        for j, y in enumerate((-0.29, -0.22, -0.15, -0.08)):
            at = np.array((0.16 * s, y, 0.34))
            d = unit(0.25 * s, 0.1, -1)
            k.add(f"capetuft{sd}{j}", blade([at, at + d * 0.045, at + d * 0.085], [0.04, 0.026, 0.004], (1, 0, 0), 0.5),
                  BROWN, "body")
    k.add("bib", ell((0, -0.3, 0.5), (0.105, 0.07, 0.17), deform=lumpy(4, 3, 0.04)), CREAM, "body")
    for j, x in enumerate((-0.07, -0.035, 0.0, 0.035, 0.07)):
        at = np.array((x, -0.32 + abs(x) * 0.3, 0.37 - abs(x) * 0.3))
        d = unit(x * 2, -0.25, -1)
        k.add(f"fringe{j}", blade([at, at + d * 0.045, at + d * 0.08], [0.032, 0.02, 0.004], (0, 1, 0), 0.5), FRINGE,
              "body")
    # The fringe's white rises a little into the bib's hem, ragged
    def hem(P):
        return inside(P[:, 2] - (0.375 + 0.012 * tri(P[:, 0] / 0.035)), 0.003) * (P[:, 1] < -0.24)
    k.paint(hem, FRINGE, on=["bib", "fringe"])


def add_paws(k):
    """A brown fringe over each pale paw, three toes in front, and a black shackle round the leg above."""
    for bone, y, lr in (("leg{s}", FORE_Y - 0.008, LEG_R), ("hind{s}", HIND_Y + 0.004, LEG_R)):
        for j, dx in enumerate((-0.03, 0.0, 0.03)):
            k.add(f"toe{bone[0]}{j}", ell((LEG_X + dx, y - 0.1, 0.03), (0.022, 0.026, 0.028)), PAW, bone, mirror=True)
        tuft = BROWN if bone.startswith("leg") else DARK
        k.add(f"pawfur{bone[0]}", ell((LEG_X, y - 0.01, 0.085), (0.078, 0.08, 0.035), deform=lumpy(6, 2, 0.12)),
              tuft, bone, mirror=True)
        z = 0.14
        rr = r.leg_radius(z, LEG_TOP, lr)
        ri, ro, h = rr - 0.006, rr + 0.02, 0.024
        k.add(f"shackle{bone[0]}", lathe((LEG_X, y, z), (0, 0, 1), [(ri, -h), (ro, -h), (ro, h), (ri, h), (ri, -h)],
                                         seg=48), SHACKLE, bone, fuse=False, mirror=True, surface="metal")


def add_spikes(k):
    """Grey blades fanning up, back and out of the smoke on the back: solid kites, laid back."""
    for s in (1, -1):
        for j, (base, tip) in enumerate((((0.06, -0.08, 0.6), (0.22, 0.06, 0.8)), ((0.08, 0.02, 0.6), (0.27, 0.16, 0.73)),
                                         ((0.08, 0.11, 0.58), (0.25, 0.28, 0.64)))):
            b = np.array((base[0] * s, base[1], base[2])); t = np.array((tip[0] * s, tip[1], tip[2]))
            d = unit(*(t - b)); a = np.cross(d, (s * 1.0, 0, 0)); a /= np.linalg.norm(a)
            n = np.cross(a, d)
            pts = [b + a * 0.08, b - a * 0.08, b + n * 0.026, b - n * 0.026, b + (t - b) * 0.4 + a * 0.065 + n * 0.018,
                   b + (t - b) * 0.4 - a * 0.05 - n * 0.012, t]
            k.add(f"spike{'R' if s > 0 else 'L'}{j}", hull(pts), GREY, "body", fuse=False, surface="shell")


def add_smoke(k):
    """The grey-green smoke: billows along the back behind the blades, trailing off behind as the tail."""
    for i, (c, rr, b) in enumerate((((0, -0.05, 0.6), (0.13, 0.1, 0.09), "body"), ((0, 0.08, 0.62), (0.15, 0.11, 0.1), "body"),
                                    ((0, 0.2, 0.6), (0.13, 0.1, 0.1), "body"), ((0.03, 0.3, 0.55), (0.09, 0.08, 0.08), "tail"),
                                    ((0.07, 0.38, 0.5), (0.065, 0.06, 0.06), "tail"), ((0.1, 0.43, 0.46), (0.045, 0.045, 0.045), "tail"))):
        k.add(f"smoke{i}", ell(c, rr, deform=lumpy(5, 3, 0.08)), SMOKE, b)


SHUT_LINE = "#2A1A14"


def eye_top(u):
    return 0.22 + 0.5 * u


def eyes(k, state):
    p, n = on_head(EYE_DIR)
    r.lay_eyes(k, state, p, n, EYE_R, r.eye_open(EYE, IRIS, PUPIL, iris_r=(0.66, 0.82)), BROWN, SHUT_LINE, eye_top)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="fur")
    r.add_trunk(k, BROWN, z=BODY_Z, w=0.15, deep=0.16, front=-0.14, back=0.19)
    r.add_legs(k, BROWN, LEG_X, FORE_Y, HIND_Y, LEG_TOP, r=LEG_R, shoulder=(0.085, 0.1, 0.15),
               thigh=(0.09, 0.11, 0.16), paw=PAW_R, paw_color=PAW)
    # The hind legs dark brown below the knee (crisp, kept to the hind legs by place)
    k.paint(lambda P: inside(P[:, 2] - 0.25, 0.003) * (np.abs(P[:, 1] - HIND_Y) < 0.11) * (np.abs(P[:, 0]) > 0.03)
            * (P[:, 2] > 0.1), DARK)
    # The paws' soles pale all round, where the leg's rounded end shows under the fringe
    k.paint(lambda P: inside(P[:, 2] - 0.05, 0.003), PAW)
    add_head(k)
    add_mane(k)
    add_paws(k)
    add_smoke(k)
    add_plate(k)
    add_crest(k)
    add_moustache(k)
    add_spikes(k)
    k.build(voxel=0.008, band=0.004)
    k.eye_states(lambda s: eyes(k, s))
    k.export(out)
