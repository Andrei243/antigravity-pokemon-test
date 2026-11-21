# Iron Valiant sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/ironvaliant.py -- <the export's Iron Valiant.glb> <out.glb>
#
# A machine knight between Gardevoir and Gallade (their idioms, written anew: gardevoir.py and gallade.py build as
# they are read and can't be imported). From the references: a rounded green helmet with a tall teal fin rising off its
# brow and leaning back; green locks hanging at its sides, pink at their lower ends; a dark face with angular pink eyes
# and a short white point out of each side of it; a white chest with a pink oval gem; a slim dark waist; round white
# shoulders; white arms with dark elbows, each forearm carrying a pink blade that runs back past the elbow and out past
# the hand (the art holds one up and one out as a long lance: here both forward and down, shorter, a mirror pair); a
# white ring at the hips and a gown of long pointed panels hanging nearly to the ground, white in front with pink tips
# and green at the sides and back, open at the front on two white legs with dark knees and pink stripes at the feet.
# The head is made a third larger than measured and the limbs thicker than drawn, so the figure reads at the game's size.
# Bones: body (0, 0, 0.6), leg{s} (0.035, 0, 0.5), arm{s} (0.06, 0, 0.75), head (0, 0, 0.78).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, sheet, outward, srgb, inside, unit, polygon, eye_look, args

WHITE, GREEN, DARK, PINK, TEAL = "#F2F3F7", "#4FB255", "#33343C", "#EC3C8A", "#7ED8CC"
GREEN_IN = "#3C9444"
HC = np.array([0.0, 0.012, 0.87])            # the helmet's middle
HCR = np.array([0.104, 0.102, 0.096])
FC = np.array([0.0, -0.05, 0.853])          # the dark face's middle
FR = np.array([0.068, 0.054, 0.064])


def spike(k, name, base, d, length, width, thick, up, color, bone, sink=0.01, mirror=False, surface="metal"):
    """A flat-faced point from base along d, its kite section wide across `up`."""
    base, d = np.array(base, float), unit(*d)
    up = np.array(up, float); up = up - d * (up @ d); up /= np.linalg.norm(up)
    side = np.cross(d, up)
    b = base - d * sink
    pts = [b + side * width / 2, b - side * width / 2, b + up * thick / 2, b - up * thick / 2,
           base + d * length * 0.45 + side * width * 0.3, base + d * length * 0.45 - side * width * 0.3,
           base + d * length * 0.45 + up * thick * 0.3, base + d * length * 0.45 - up * thick * 0.3,
           base + d * length]
    k.add(name, hull(np.array(pts)), color, bone, fuse=False, mirror=mirror, surface=surface)


def legs(k):
    KN = (0.055, -0.006, 0.29)
    k.add("thigh", tube([(0.045, 0.0, 0.54), KN], [0.036, 0.032]), WHITE, "leg{s}", mirror=True)
    k.add("knee", ell(KN, (0.036, 0.034, 0.034)), DARK, "leg{s}", mirror=True)
    k.add("shin", tube([KN, (0.06, -0.002, 0.08)], [0.031, 0.033]), WHITE, "leg{s}", mirror=True)
    k.add("foot", ell((0.06, -0.02, 0.032), (0.036, 0.055, 0.032)), WHITE, "leg{s}", mirror=True)
    # two pink stripes down the front of each foot
    for dx in (-0.011, 0.011):
        x0, y0 = 0.06 + dx, -0.03
        box = [(x0 + a, y0 + b, z) for a in (-0.005, 0.005) for b in (-0.012, 0.008) for z in (0.03, 0.11)]
        k.add("stripe" + ("a" if dx < 0 else "b"), hull(np.array(box)), PINK, "leg{s}", fuse=False, mirror=True,
              surface="metal")


def trunk(k):
    k.add("hip", ell((0, 0.0, 0.565), (0.095, 0.085, 0.042)), WHITE, "body")
    k.add("waist", ell((0, 0.004, 0.635), (0.034, 0.03, 0.06)), DARK, "body")
    k.add("chest", ell((0, 0.0, 0.725), (0.075, 0.055, 0.065)), WHITE, "body")
    k.add("shoulder", ell((0.085, 0.0, 0.755), (0.042, 0.04, 0.04)), WHITE, "arm{s}", mirror=True)
    k.add("neck", tube([(0, 0.0, 0.77), (0, -0.01, 0.84)], [0.02, 0.018]), DARK, "head")
    # the gem: a pink oval with a darker heart and a glint
    gem_dark, gem, glint = srgb("#B0205E"), srgb(PINK), srgb("#FFE4F0")
    def look(u, v):
        c = np.tile(gem, (len(u), 1))
        c[(u * u + v * v) > 0.72] = gem_dark
        c[((u + 0.25) / 0.25) ** 2 + ((v - 0.35) / 0.3) ** 2 < 1] = glint
        return c
    k.decal((0, -0.055, 0.735), unit(0, -1, 0.05), (0.022, 0.03), look, eps=0.003, rings=12, segs=48)


def arms(k):
    EL, HA = np.array((0.14, -0.05, 0.665)), np.array((0.13, -0.15, 0.6))
    k.add("upper", tube([(0.09, 0.0, 0.75), (0.12, -0.02, 0.71), EL], [0.03, 0.028, 0.026]), WHITE, "arm{s}",
          mirror=True)
    k.add("elbow", ell(EL, (0.03, 0.03, 0.03)), DARK, "arm{s}", mirror=True)
    k.add("fore", tube([EL, HA], [0.027, 0.025]), WHITE, "arm{s}", mirror=True)
    k.add("hand", ell(HA + (0, -0.02, -0.01), (0.024, 0.026, 0.026)), DARK, "arm{s}", mirror=True)
    # the pink blade along the forearm's outer side: from behind the elbow out past the hand, its face outward
    d = unit(*(HA - EL))
    side = unit(1, 0, 0.05)
    up = np.cross(d, side); up /= np.linalg.norm(up)
    pts = []
    for t, w in ((-0.35, 0.004), (0.0, 0.03), (0.5, 0.036), (1.0, 0.032), (1.6, 0.012), (2.0, 0.0)):
        c = EL + (HA - EL) * t + side * 0.03
        for s in (1, -1):
            pts.append(c + up * w * s + side * 0.008)
            pts.append(c + up * w * s * 0.8 - side * 0.008)
    k.add("blade", hull(np.array(pts)), PINK, "arm{s}", fuse=False, mirror=True, surface="metal")


def gown(k):
    """Long pointed panels from the hip ring to near the ground: white at the front with pink tips, green round the
    sides and back, the front left open on the legs."""
    def panel(phi, w, inset, v0=0.0, v1=1.0):
        def at(u, v):
            v = v0 + (v1 - v0) * v
            th = phi + (u - 0.5) * w * np.sin(np.pi * (0.22 + 0.78 * v)) ** 0.55
            r = 0.082 + 0.18 * v ** 1.05 - inset
            z = 0.57 - 0.5 * v + 0.03 * v ** 3
            return np.stack([r * np.sin(th), -r * np.cos(th), z], axis=-1)
        return at
    for name, a, w, inset, col in (("panelback", 180, 1.5, 0.012, GREEN), ("panelside", 108, 1.3, 0.006, GREEN),
                                   ("panelrear", 148, 1.1, 0.0, WHITE)):
        k.add(name, outward(sheet(panel(math.radians(a), w, inset), 14, 26, 0.013)), col, "body", fuse=False,
              mirror=name != "panelback", surface="metal")
    k.add("panelfront", outward(sheet(panel(math.radians(60), 1.15, -0.004, 0.0, 0.8), 14, 22, 0.014)), WHITE, "body",
          fuse=False, mirror=True, surface="metal")
    k.add("paneltip", outward(sheet(panel(math.radians(60), 1.15, -0.004, 0.8, 1.0), 10, 8, 0.014)), PINK, "body",
          fuse=False, mirror=True, surface="metal")


def head(k):
    k.add("helmet", ell(HC, HCR), GREEN, "head")
    k.add("face", ell(FC, FR), DARK, "head")
    # the helmet's edge over the face: dipping to a point between the eyes
    def visor(P):
        x, z = P[:, 0], P[:, 2]
        edge = FC[2] + 0.035 - 0.03 * np.clip(1 - np.abs(x) / 0.03, 0, 1)
        return inside(z - edge, 0.003) * inside(P[:, 1] - FC[1] - 0.005, 0.004) * inside(np.abs(x) - 0.058, 0.003) * inside(0.81 - z, 0.003)
    k.paint(visor, DARK)
    # the helmet's green back over the face's crown, where the dark face pushes up through it
    k.paint(lambda P: inside(FC[2] + 0.04 - P[:, 2], 0.003) * inside(np.linalg.norm(P - HC, axis=1) - 0.14, 0.003),
            GREEN, on=["face", "helmet"])
    # green locks hanging at the sides, pink at their lower ends
    k.add("lock", ell((0.108, -0.005, 0.81), (0.054, 0.062, 0.085), rot=(0, -12, 0)), GREEN, "head", mirror=True)
    k.paint(lambda P: inside(P[:, 2] - 0.775, 0.003) * inside(0.05 - np.abs(P[:, 0]), 0.003), PINK, on=["lock"])
    # the teal fin off the brow, leaning back
    spike(k, "crest", (0, -0.04, 0.93), (0, 0.32, 0.95), 0.18, 0.09, 0.034, (0, -1, 0.3), TEAL, "head", sink=0.04)
    # the white point out of each side of the face
    spike(k, "point", (0.058, -0.06, 0.875), (0.85, -0.05, 0.5), 0.075, 0.03, 0.02, (0, -1, 0), WHITE, "head",
          sink=0.015, mirror=True)


# --- the eyes: angular pink, a dark pupil, on the dark face --------------------------------------------------------
EYE_PINK, EYE_INK, EYE_GLINT = srgb("#F05AA0"), srgb("#1A1418"), srgb("#FFFFFF")
LID, LINE = srgb(DARK), srgb("#E07AAA")
EYE = polygon([(-0.95, -0.55), (0.1, -0.85), (0.95, -0.2), (0.85, 0.55), (-0.3, 0.75), (-0.95, 0.2)])


def eye_open(u, v):
    c = np.tile(EYE_PINK, (len(u), 1))
    c[((u + 0.1) / 0.32) ** 2 + ((v + 0.05) / 0.42) ** 2 < 1] = EYE_INK
    c[((u - 0.2) / 0.16) ** 2 + ((v - 0.28) / 0.18) ** 2 < 1] = EYE_GLINT
    return c


def lay(k):
    de = unit(0.45, -0.88, 0.08)
    E = FC + de / math.sqrt(((de / FR) ** 2).sum())
    m = np.array([-1, 1, 1.0])
    def eyes(state):
        look = eye_look(state, eye_open, LID, LINE, line=0.17)
        k.decal(E, de, (0.026, 0.024), look, eps=0.0025, rings=20, segs=56, outline=EYE)
        k.decal(E * m, de * m, (0.026, 0.024), lambda u, v: look(-u, v), eps=0.0025, rings=20, segs=56,
                outline=lambda th: EYE(math.pi - th))
    return eyes


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="metal")
    legs(k)
    trunk(k)
    arms(k)
    gown(k)
    head(k)
    eyes = lay(k)
    k.build(voxel=0.0055, smooth=4, band=0.006, wband=0.05)
    k.eye_states(eyes)
    k.export(out)
