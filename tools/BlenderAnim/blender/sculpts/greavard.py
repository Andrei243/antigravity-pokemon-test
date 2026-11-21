# Greavard sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/greavard.py -- <the export's Greavard.glb> <out.glb>
#
# From the references: a little ghost dog of pale blue-grey shaggy fur that is nearly all head. A great round head with
# a short snout lifted a little and a black nose; a huge open mouth under it, dark red inside with a pink tongue, its
# lips ragged into points like teeth; no eyes to be seen, the fur of the brow hanging over them; long shaggy ears down
# the sides of the head. On the crown a white bone stuck upright like a candle, a black wick and a lavender flame with
# a yellow heart. A small body behind the head on stubby legs, long white hair hanging from the back and flanks; a
# tail curled up behind, white at its tip.
# The pictures overrule the brief: no candle stub but a bone used as one; the fur hangs as a white skirt down the
# flanks; the mouth is the face's largest part.
#
# Written to be grown from (houndstone.py keeps this frame: the ragged teeth, the hanging hair, the colours).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, hull, srgb, inside, unit, eye_look, args, sibling

M = sibling("maschiff")

FUR, SHADE, WHITE = "#A8B5CE", "#7F8BA6", "#EEF2F8"
MOUTH, TONGUE, NOSE = "#6E1C26", "#E07884", "#2A2A34"
BONE, FLAME, HEART, WICK = "#F2F1EC", "#B9A4DE", "#F6D88A", "#24222A"

# --- the frame (joints: body (0, 0.03, 0.14), head (0, -0.07, 0.2), ears (+-0.09, -0.08, 0.26),
#     flame (0, -0.07, 0.41), legs (+-0.07, -0.06, 0.08), hinds (+-0.07, 0.09, 0.08), tail (0, 0.15, 0.19)) ----------
HC = np.array([0.0, -0.075, 0.255])       # the cranium
HR = np.array([0.14, 0.128, 0.112])
UC = np.array([0.0, -0.19, 0.24])         # the upper snout and lip
UR = np.array([0.1, 0.08, 0.044])
JC = np.array([0.0, -0.16, 0.082])        # the dropped lower jaw
JR = np.array([0.118, 0.098, 0.056])


def fang(base, d, length, w, t):
    """A solid tapering point (a ragged tooth of the lip, a claw): a flat-faced hull."""
    base = np.array(base, float); d = unit(*d)
    side = np.cross(d, (0, 0, 1.0))
    if np.linalg.norm(side) < 1e-3:
        side = np.array([1.0, 0, 0])
    side /= np.linalg.norm(side)
    face = np.cross(d, side)
    return hull([base + side * w, base - side * w, base + face * t, base - face * t,
                 base + d * length * 0.5 + side * w * 0.4, base + d * length * 0.5 - side * w * 0.4, base + d * length])


def ragged_lip(k, name, c, r, z, down, n, length, w, col, bone, span=1.25, inset=0.008):
    """Ragged points along the front rim of a lip (an ellipse about c of radii r at height z), pointing down (upper
    lip) or up (lower lip): the teeth the fur makes."""
    for i in range(n):
        a = -span + 2 * span * i / (n - 1)          # 0 straight ahead
        x, y = r[0] * math.sin(a), -r[1] * math.cos(a)
        base = np.array((c[0] + x * (1 - inset / r[0]), c[1] + y * (1 - inset / r[1]), z))
        inward = -np.array((math.sin(a), -math.cos(a), 0)) * 0.25
        d = inward + np.array((0, 0, -1.0 if down else 1.0))
        ln = length * (1.0 if i % 2 == 0 else 0.7)
        k.add(f"{name}{i}", fang(base, d, ln, w, w * 0.55), col, bone, fuse=False)


def hanging_hair(k, name, c, r, z, n, length, col, bone, start=0.0, end=2 * math.pi, width=0.03, skip_front=0.0):
    """Long locks hanging down round an ellipse (centre c, radii r) at height z, a little outward."""
    for i in range(n):
        th = start + (end - start) * (i + 0.5) / n          # 0 straight ahead, round toward +X
        if abs(math.atan2(math.sin(th), math.cos(th))) < skip_front:
            continue
        sx, cy = math.sin(th), -math.cos(th)
        base = np.array((c[0] + r[0] * sx * 0.9, c[1] + r[1] * cy * 0.9, z))
        d = np.array((sx * 0.22, cy * 0.22, -1.0))
        ln = length * (1.0 if i % 2 else 0.82)
        M.tuft(k, f"{name}{i}", base, d, ln, width, col, bone, flat=(sx, cy, 0), f=0.42)


def candle(k, top, bone="flame"):
    """The bone stuck upright in the crown, its knobbed end up; a black wick; the lavender flame, its yellow heart
    thicker than it so that it shows on both faces."""
    top = np.array(top, float)
    k.add("boneshaft", tube([top + (0, 0, -0.07), top + (0, 0, -0.012)], [0.02, 0.018]), BONE, "head", fuse=False,
          surface="shell")
    for s in (1, -1):
        k.add(f"boneknob{'R' if s > 0 else 'L'}", ell(top + (s * 0.02, 0, 0.0), (0.024, 0.021, 0.02)), BONE, "head",
              fuse=False, surface="shell")
    k.add("wick", tube([top + (0, 0, 0.012), top + (0, 0, 0.03)], [0.004, 0.0035]), WICK, bone, fuse=False)
    def pointed(V):
        V = V.copy()
        t = np.clip(V[:, 2], 0, 1)
        V[:, :2] *= (1 - 0.85 * t ** 1.4)[:, None]
        return V
    k.add("flameouter", ell(top + (0, 0, 0.075), (0.034, 0.02, 0.058), deform=pointed), FLAME, bone, fuse=False)
    k.add("flameheart", ell(top + (0, 0, 0.062), (0.019, 0.024, 0.034), deform=pointed), HEART, bone, fuse=False,
          glow=HEART)


def greavard(src, out):
    k = Kit(src, coat="fur")

    # --- a small body behind the head, stubby legs ---------------------------------------------------------------
    k.add("body", ell((0, 0.05, 0.13), (0.098, 0.105, 0.08)), FUR, "body")
    k.add("rump", ell((0, 0.11, 0.14), (0.088, 0.065, 0.075)), FUR, "body")
    PR = (0.036, 0.042, 0.022)
    pf = M.leg(k, 0.07, -0.05, 0.085, 0.032, PR, FUR, "leg{s}", "leg", (0.042, 0.048, 0.055))
    ph = M.leg(k, 0.07, 0.095, 0.085, 0.032, PR, FUR, "hind{s}", "hind", (0.046, 0.054, 0.06))
    M.toe_lines(k, [pf, ph], PR, SHADE)
    # long white hair from the back and flanks, hanging
    hanging_hair(k, "skirt", (0, 0.06, 0.0), (0.1, 0.11, 0), 0.175, 14, 0.1, WHITE, "body", start=0.55,
                 end=2 * math.pi - 0.55)
    k.paint(lambda P: inside(0.17 - P[:, 2], 0.004) * (P[:, 1] > 0.0) * (P[:, 2] > 0.06), WHITE, on=["body", "rump"])

    # --- the tail curled up behind, white at its tip ------------------------------------------------------------
    T = [(0.0, 0.16, 0.18), (0.01, 0.2, 0.24), (0.025, 0.205, 0.3), (0.04, 0.18, 0.335), (0.05, 0.155, 0.335)]
    k.add("tail", tube(T, [0.026, 0.024, 0.021, 0.017, 0.012]), FUR, "tail")
    k.paint(lambda P: inside(0.295 - P[:, 2], 0.004) * (P[:, 1] > 0.12), WHITE, on=["tail"])

    # --- the head: a great round cranium, the lifted snout, the dropped jaw, the mouth inside -----------------
    k.add("head", ell(HC, HR), FUR, "head")
    k.add("snout", ell(UC, UR), FUR, "head")
    k.add("jaw", ell(JC, JR), FUR, "head")
    k.add("cheek", ell((0.085, -0.13, 0.16), (0.055, 0.07, 0.085)), FUR, "head", mirror=True)
    k.add("mouth", ell((0, -0.15, 0.162), (0.096, 0.085, 0.06)), MOUTH, "head")
    k.add("tongue", ell((0, -0.18, 0.13), (0.07, 0.065, 0.022)), TONGUE, "head")
    # the ragged lips: points down from the upper lip, up from the lower
    ragged_lip(k, "uppertooth", UC, UR * (1, 1.02, 1), UC[2] - UR[2] * 0.55, True, 9, 0.032, 0.014, FUR, "head")
    ragged_lip(k, "lowertooth", JC, JR * (0.9, 0.94, 1), JC[2] + JR[2] * 0.62, False, 9, 0.034, 0.015, FUR, "head")
    # the black nose on the snout's tip, lifted
    k.add("nose", ell(UC + (0, -UR[1] + 0.004, 0.028), (0.03, 0.02, 0.02), rot=(-20, 0, 0)), NOSE, "head", fuse=False,
          surface="skin")
    # the fur of the brow hanging over the eyes, and shaggy fur under the chin
    for i, x in enumerate((-0.09, -0.045, 0.0, 0.045, 0.09)):
        M.tuft(k, f"fringe{i}", (x, -0.18 + abs(x) * 0.5, 0.33 - abs(x) * 0.3), (x * 0.8, -0.55, -1), 0.065, 0.03, FUR,
               "head", flat=(0, 1, 0.3), f=0.42)
    for i, x in enumerate((-0.07, -0.025, 0.025, 0.07)):
        M.tuft(k, f"chin{i}", (x, -0.19 + abs(x) * 0.4, 0.045), (x, -0.4, -1), 0.04, 0.024, FUR, "head",
               flat=(0, 1, 0), f=0.45)
    # long shaggy ears down the sides of the head
    M.floppy_ear(k, (0.118, -0.09, 0.3), 0.15, (0.026, 0.055), FUR, out=0.3)
    hanging_hair(k, "mane", (0, -0.03, 0.0), (0.13, 0.11, 0), 0.24, 8, 0.09, FUR, "head", start=2.0,
                 end=2 * math.pi - 2.0)

    candle(k, (0.0, -0.075, 0.405))

    k.build(voxel=0.0045, smooth=3, band=0.006, wband=0.04)

    # --- the eyes, hidden under the fringe: patches of the fur's own colour -----------------------------------
    E = M.on_ell(HC, HR, 0.06, 0.285)
    fur = srgb(FUR)
    def lay(state):
        for s in (1, -1):
            k.decal(E * np.array([s, 1, 1.0]), unit(0.35 * s, -1, 0.2), (0.025, 0.02), fur, eps=0.0025, rings=8,
                    segs=32)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    greavard(*args())
