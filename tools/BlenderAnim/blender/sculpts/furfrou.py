# Furfrou (its natural coat) sculpted from nothing on the skeleton and clips of the game's own export of it
# (sculptkit.py):
#   blender --background --factory-startup --python sculpts/furfrou.py -- <the export's Furfrou.glb> <out.glb>
# A white poodle in a great shaggy coat on long legs: a mop of spiky white hair over the head with a fringe falling over
# the brow, two long white locks hanging from it down in front of the shoulders, a long black snout with a blue nose
# like a bean at its tip and a red eye under the fringe with a blue lid; a deep white trunk, spiky tufts of fur at the
# chest, the elbows and round each ankle, the legs shaded grey below, dark grey paws with black claws; a big bushy
# pointed plume of a tail rising behind.
#
# The pictures overrule the brief here: the face is dark only on the snout and round the eye, the legs only shaded
# grey with dark paws, and the tail's tip white like the rest of the plume.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, blade, hull, outward, srgb, inside, smoothstep, unit, eye_look, polygon, lumpy, args)

# --- colours ------------------------------------------------------------------------------------------------------------
WHITE = "#E0DED8"
SHADE = "#CBC8CE"          # the grey of the lower legs
FACE = "#322C30"           # the snout and the skin round the eye
PAW, CLAW = "#4E484E", "#1C1A1E"
NOSE = "#6C9CCC"
RED, PUPIL, LID = "#D42A3A", "#1A1014", "#4A7EC4"
INK = "#14100F"

# --- the frame (the skeleton's joints: body (0, 0, 0.3), head (0, -0.14, 0.38), ears (+-0.04, -0.18, 0.51), legs
#     (+-0.06, -0.13, 0.26), hind legs (+-0.06, 0.15, 0.26), tail (0, 0.127, 0.34)) ------------------------------------------
BODY_Z = 0.315
LEG_X = 0.06
FORE_Y, HIND_Y = -0.13, 0.15
LEG_TOP = 0.27
H = np.array([0, -0.19, 0.535])          # the skull under the mop
M = np.array([0, -0.165, 0.615])         # the mop's centre


def spike(base, d, length, width, thick=None):
    """A pointed tuft of fur: a flat-faced kite from a base buried in the coat to its tip."""
    width, length = width * 1.45, length * 0.85
    d = unit(*d); base = np.array(base, float)
    a, b = np.cross(d, (0, 0, 1.0)), None
    if np.linalg.norm(a) < 1e-3:
        a = np.array([1.0, 0, 0])
    a /= np.linalg.norm(a); b = np.cross(d, a)
    t = thick if thick is not None else width * 0.55
    return hull([base + a * width, base - a * width, base + b * t, base - b * t, base + d * length * 0.4 + a * width * 0.6,
                 base + d * length * 0.4 - a * width * 0.6, base + d * length])


def add_body(color=WHITE):
    k.add("chest", ell((0, -0.085, BODY_Z), (0.086, 0.09, 0.094)), color, "body")
    k.add("middle", ell((0, 0.02, BODY_Z - 0.005), (0.08, 0.12, 0.085)), color, "body")
    k.add("rump", ell((0, 0.13, BODY_Z), (0.084, 0.078, 0.09)), color, "body")
    k.add("neck", tube([(0, -0.1, 0.36), (0, -0.15, 0.44), (0, -0.175, 0.51)], [0.07, 0.062, 0.058]), color, "head")


def add_legs(color=WHITE, shade=SHADE, paw=PAW):
    for bone, y, nm, top in (("leg{s}", FORE_Y, "fore", (0.05, 0.058, 0.095)), ("hind{s}", HIND_Y, "hind", (0.058, 0.07, 0.1))):
        k.add(f"{nm}top", ell((LEG_X - 0.004, y + 0.004, LEG_TOP), top), color, bone, mirror=True)
        k.add(f"{nm}leg", tube([(LEG_X, y, LEG_TOP), (LEG_X, y - 0.003, 0.15), (LEG_X, y - 0.006, 0.045)],
                               [0.042, 0.034, 0.03]), color, bone, mirror=True)
        k.add(f"{nm}cuff", ell((LEG_X, y - 0.006, 0.07), (0.042, 0.045, 0.034), deform=lumpy(5, 2, 0.12)), color, bone,
              mirror=True)
        k.add(f"{nm}paw", ell((LEG_X, y - 0.018, 0.02), (0.034, 0.044, 0.022)), paw, bone, mirror=True)
        # the cuff's spikes flaring out and down round the ankle
        for j, a in enumerate(np.linspace(-1.9, 1.9, 5)):
            d = unit(math.sin(a), -math.cos(a), -0.55)
            base = np.array((LEG_X, y - 0.006, 0.07)) + np.array((math.sin(a), -math.cos(a), 0)) * 0.03
            k.add(f"{nm}cuffspike{j}", spike(base, d, 0.05, 0.017), color, bone, fuse=False, mirror=True, surface="fur")
        # three black claws at the front of the paw
        for j, dx in enumerate((-0.016, 0.0, 0.016)):
            c = np.array((LEG_X + dx, y - 0.058, 0.012))
            k.add(f"{nm}claw{j}", hull([c + (-0.007, 0.012, -0.008), c + (0.007, 0.012, -0.008), c + (-0.006, 0.012, 0.007),
                                        c + (0.006, 0.012, 0.007), c + (0, -0.008, -0.004)]), CLAW, bone, fuse=False,
                  mirror=True, surface="shell")
    # the lower legs shaded grey down to the cuff
    k.paint(lambda P: inside(P[:, 2] - 0.19, 0.012) * (P[:, 2] > 0.035), shade, on=["foreleg", "hindleg", "forecuff",
                                                                                    "hindcuff"])


def add_fluff(color=WHITE):
    # tufts at the chest, pointing forward and down
    for j, (x, z, d) in enumerate(((0.0, 0.33, (0, -1, -0.5)), (0.04, 0.3, (0.4, -1, -0.6)), (-0.04, 0.3, (-0.4, -1, -0.6)),
                                   (0.02, 0.37, (0.2, -1, -0.2)), (-0.025, 0.36, (-0.25, -1, -0.25)))):
        base = (x, -0.16, z)
        k.add(f"chesttuft{j}", spike(base, d, 0.06, 0.022), color, "body", fuse=False, surface="fur")
    # elbows and the backs of the thighs, pointing back
    for j, (base, d, ln) in enumerate((((0.085, -0.11, 0.25), (0.6, 0.5, -0.6), 0.055),
                                       ((0.09, 0.19, 0.25), (0.5, 0.8, -0.4), 0.055),
                                       ((0.08, 0.04, 0.24), (0.5, 0.2, -0.9), 0.045))):
        k.add(f"bodytuft{j}", spike(base, d, ln, 0.02), color, "body", fuse=False, mirror=True, surface="fur")


# --- the head: a mop of spiky hair, a fringe over the brow, a long dark snout ---------------------------------------
def add_head(color=WHITE, face=FACE):
    k.add("skull", ell(H, (0.066, 0.07, 0.066)), color, "head")
    k.add("mop", ell(M, (0.112, 0.108, 0.088), deform=lumpy(5, 3, 0.07)), color, "head")
    k.add("face", ell(H + (0, -0.03, -0.004), (0.05, 0.048, 0.046)), face, "head")
    k.add("snout", tube([(0, -0.23, 0.53), (0, -0.28, 0.512), (0, -0.315, 0.497)], [0.04, 0.032, 0.026]), face, "head")
    k.add("jaw", ell((0, -0.26, 0.49), (0.03, 0.04, 0.02)), face, "head")


MOP = [  # (direction from the mop's centre, length, width)
    ((0, -0.2, 1), 0.1, 0.034), ((0.5, -0.1, 0.9), 0.095, 0.032), ((-0.5, -0.1, 0.9), 0.09, 0.032),
    ((0.25, 0.5, 0.85), 0.1, 0.034), ((-0.25, 0.5, 0.85), 0.1, 0.034), ((0.85, 0.2, 0.5), 0.085, 0.03),
    ((-0.85, 0.2, 0.5), 0.085, 0.03), ((0, 0.9, 0.45), 0.09, 0.032), ((0.6, 0.65, 0.3), 0.075, 0.028),
    ((-0.6, 0.65, 0.3), 0.075, 0.028), ((0.95, -0.25, 0.15), 0.07, 0.028), ((-0.95, -0.25, 0.15), 0.07, 0.028),
    ((0.3, -0.6, 0.75), 0.08, 0.03), ((-0.3, -0.6, 0.75), 0.08, 0.03),
]


def add_mop(color=WHITE):
    R = np.array((0.112, 0.108, 0.088))
    for j, (d, ln, w) in enumerate(MOP):
        d = unit(*d)
        base = M + d * R * 0.8
        k.add(f"mopspike{j}", spike(base, d, ln * (1.0 if d[2] > 0.8 else 1.45), w * 1.15), color, "head", fuse=False, surface="hair")
    # the fringe: locks falling over the brow from the mop's front, stopping above the eyes
    for j, (x, ln) in enumerate(((0.0, 0.08), (0.045, 0.075), (-0.045, 0.075))):
        base = M + (x, -0.095, -0.025)
        k.add(f"fringe{j}", spike(base, (x * 6, -0.5, -0.9), ln, 0.028, 0.016), color, "head", fuse=False, surface="hair")


def add_locks(color=WHITE):
    """The long locks (its ears) hanging from the mop down in front of the shoulders, spiky at their ends."""
    pts = [(0.07, -0.19, 0.56), (0.098, -0.2, 0.47), (0.105, -0.2, 0.38), (0.1, -0.19, 0.3)]
    k.add("lock", blade(pts, [0.045, 0.05, 0.044, 0.012], unit(1, -0.5, 0), 0.5), color, "ear{s}", mirror=True)
    for j, (dx, d) in enumerate(((0.0, (0.1, -0.2, -1)), (0.02, (0.6, -0.2, -0.8)), (-0.02, (-0.3, -0.1, -0.9)))):
        k.add(f"lockspike{j}", spike((0.1 + dx, -0.19, 0.31), d, 0.06, 0.02), color, "ear{s}", fuse=False, mirror=True,
              surface="fur")


# --- the tail: a big bushy pointed plume rising behind, swung some 30 degrees out to +X ---------------------------------
TAIL = [(0.0, 0.14, 0.35), (0.015, 0.19, 0.42), (0.04, 0.22, 0.5), (0.065, 0.235, 0.6)]


def add_tail(color=WHITE):
    k.add("tail", blade(TAIL, [0.034, 0.064, 0.055, 0.006], unit(1, -0.6, 0), 0.7), color, "tail")
    for j, (t, side, ln) in enumerate(((0.35, 1, 0.05), (0.55, -1, 0.05), (0.7, 1, 0.045), (0.45, -1, 0.045))):
        n = len(TAIL) - 1; f = t * n; a = int(f); s = f - a
        c = np.array(TAIL[a]) * (1 - s) + np.array(TAIL[a + 1]) * s
        d = unit(0, 1, 0.3 * side) + unit(0.5, 0.3, 0) * 0.3
        k.add(f"tailspike{j}", spike(c + d * 0.04, d, ln, 0.02), color, "tail", fuse=False, surface="fur")


def add_nose():
    k.add("nose", ell((0, -0.338, 0.494), (0.022, 0.02, 0.013), rot=(-15, 0, 0)), NOSE, "head", fuse=False, surface="skin")


# --- the eye: under the fringe on the dark skin, a red iris and a black pupil, a blue lid along its top ---------------
EYE_DIR = unit(0.62, -0.72, 0.2)
EH = 0.019
E = H + (0, -0.03, -0.004) + EYE_DIR / np.linalg.norm(EYE_DIR / np.array((0.05, 0.048, 0.046)))
CR, CP, CL, CF = srgb(RED), srgb(PUPIL), srgb(LID), srgb(FACE)


def eye(u, v):
    c = np.tile(CR, (len(u), 1))
    c[np.hypot((u + 0.15) / 0.36, (v + 0.05) / 0.5) < 1] = CP
    c[np.hypot(u - 0.1, v - 0.15) < 0.15] = (1, 1, 1)
    c[v > 0.42 - 0.15 * u] = CL
    return c


ALMOND = polygon([(math.cos(t), 0.72 * math.sin(t)) for t in np.linspace(0, 2 * math.pi, 64,
                                                                                                    endpoint=False)])


def eyes(state):
    look = eye_look(state, eye, CL, srgb(INK), line=0.17)
    for side in (1, -1):
        col = look if side == 1 else (lambda a, b, f=look: f(-a, b))
        k.decal(E * np.array([side, 1, 1.0]), EYE_DIR * np.array([side, 1, 1.0]), (EH * 1.2, EH), col, eps=0.004,
                outline=ALMOND if state in ("Open", "Fierce") else None, rings=20, segs=56, mirror=False)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="fur")
    add_body()
    add_legs()
    add_fluff()
    add_head()
    add_mop()
    add_locks()
    add_tail()
    add_nose()
    k.build(voxel=0.007, smooth=4, band=0.005, wband=0.06)
    k.eye_states(eyes)
    k.export(out)
