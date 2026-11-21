# Spectrier sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), on
# Glastrier's frame (glastrier.py, imported as sibling("glastrier")): the same barrel, legs and way of making them,
# slimmer, as the two are a pair.
#   blender --background --factory-startup --python sculpts/spectrier.py -- <the export's Spectrier.glb> <out.glb>
#
# The design, from the references: a slender black horse of the dead, its head carried low and forward. Its mane is
# long wavy locks of deep purple hanging from the head and the crest of the neck down before the chest, paling to
# lilac at their ends; a pale lilac forelock falls over the forehead to one cheek. Small pointed black ears; narrow
# glowing white eyes under heavy black lids. Its lower legs are deep purple socks, and under each a flat purple hoof
# that floats a little apart from the leg. A long wavy lilac tail hangs down behind. (The brief's white feathering at
# the hooves is not in the pictures: the socks and the floating hooves are purple.)
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, polygon, eye_look, args, sibling, blade

gl = sibling("glastrier")
BLACK, PURPLE, LILAC, TIP = "#2C2E38", "#5C3FB8", "#CBB8EA", "#9C88DC"
WHITE, PUPIL, INK = srgb("#F4F0FF"), srgb("#8A6EE0"), srgb("#14141C")
SLIM = dict(z=0.458, half_w=0.112, half_d=0.152, half_l=0.235, y=0.0)    # the belly at about 0.32
LEG_X = 0.07
H = np.array([0.0, -0.37, 0.74])
HR = np.array([0.062, 0.085, 0.068])


def wavy(top, bottom, amp, waves, phase, n=16):
    """A lock's path from top down to bottom, swinging side to side and back and forth."""
    top, bottom = np.array(top, float), np.array(bottom, float)
    P = []
    for t in np.linspace(0, 1, n):
        p = top * (1 - t) + bottom * t
        w = amp * math.sin(2 * math.pi * waves * t + phase) * min(1, t * 3)
        P.append(p + np.array([w, 0.5 * w, 0]))
    return P


def spectrier(src, out):
    k = Kit(src, coat="fur")
    gl.trunk(k, BLACK, SLIM)
    gl.legs(k, BLACK, x=LEG_X, r=0.057)
    # the purple socks, and the flat hooves floating a little under them
    k.paint(lambda P: inside(P[:, 2] - 0.16, 0.006), PURPLE, on=["fore", "hind"])
    for s, side in ((1, "R"), (-1, "L")):
        for y, bone in ((gl.FORE_Y - 0.005, f"leg{side}"), (gl.HIND_Y + 0.005, f"hind{side}")):
            k.add(f"hoof{bone}", ell(((LEG_X + 0.004) * s, y - 0.008, 0.011), (0.05, 0.058, 0.012)), PURPLE, bone,
                  fuse=False, surface="hair")

    # --- a neck forward and up to a head carried low ------------------------------------------------------------
    k.add("neck", tube([(0, -0.17, 0.55), (0, -0.25, 0.63), (0, -0.32, 0.71)], [0.095, 0.078, 0.065]), BLACK,
          "head")
    k.add("cranium", ell(H, HR), BLACK, "head")
    FC = np.array([0.0, -0.44, 0.665])
    k.add("face", ell(FC, (0.05, 0.056, 0.098), rot=(-44, 0, 0)), BLACK, "head")
    k.add("muzzle", ell((0, -0.5, 0.6), (0.043, 0.047, 0.042), rot=(-44, 0, 0)), BLACK, "head")
    k.add("ear", blade([(0.034, -0.34, 0.79), (0.044, -0.335, 0.84), (0.05, -0.33, 0.885)], [0.024, 0.017, 0.005],
                       (0, 1, 0), 0.55), BLACK, "head", mirror=True)

    # --- the mane: a crest of purple along the neck, long wavy locks hanging before the chest ----------------
    for i, t in enumerate(np.linspace(0, 1, 11)):
        p = np.array((0, -0.33, 0.79)) * (1 - t) + np.array((0, -0.14, 0.64)) * t
        k.add(f"crest{i}", ell(p, (0.058, 0.05, 0.042)), PURPLE, "head" if t < 0.6 else "body", surface="hair")
    locks = [((0.045, -0.34, 0.74), (0.06, -0.4, 0.3), 0.022, 2.2, 0.0),
             ((0.08, -0.3, 0.72), (0.12, -0.35, 0.25), 0.026, 2.0, 1.4),
             ((0.11, -0.24, 0.68), (0.165, -0.3, 0.3), 0.024, 1.8, 2.6),
             ((0.12, -0.17, 0.64), (0.17, -0.22, 0.38), 0.02, 1.6, 0.7)]
    for s, side in ((1, "R"), (-1, "L")):
        for j, (top, bot, amp, waves, ph) in enumerate(locks):
            m = np.array([s, 1, 1.0])
            P = [p * m for p in wavy(top, bot, amp, waves, ph + (0.9 if s < 0 else 0))]
            R = list(np.linspace(0.03, 0.017, len(P)))
            k.add(f"lock{j}{side}", tube(P, R, seg=12, per=4), PURPLE, "head", surface="hair")
    k.paint(lambda P: inside(P[:, 2] - 0.43, 0.06), TIP, on=["lock"])
    # the pale forelock over the forehead, falling to the model's left cheek
    k.add("forelock", tube([(0.0, -0.38, 0.815), (-0.008, -0.45, 0.785), (-0.025, -0.49, 0.725), (-0.04, -0.505, 0.665),
                            (-0.042, -0.525, 0.615)], [0.026, 0.026, 0.022, 0.019, 0.016], seg=12, per=4), LILAC, "head",
          surface="hair")

    # --- the tail: long, wavy, lilac, hanging down behind --------------------------------------------------------
    TP = [(0, 0.28, 0.6), (0, 0.36, 0.58)] + wavy((0, 0.4, 0.53), (0.0, 0.44, 0.14), 0.04, 1.6, 0.4, n=8)[1:]
    k.add("tail", tube(TP, list(np.linspace(0.04, 0.022, len(TP))), seg=14, per=4), LILAC, "tail", surface="hair")

    # --- the eyes: narrow, glowing white under heavy black lids ----------------------------------------------
    de = unit(0.74, -0.6, 0.2)
    E = gl.on_ell(H + np.array([0, -0.03, -0.01]), HR, de)
    B = srgb(BLACK)
    def narrow(u, v):
        c = np.tile(B, (len(u), 1))
        top, bot = 0.3 - 0.25 * u * u + 0.08 * u, -0.45 + 0.35 * u * u
        inn = (np.abs(u) < 0.92) & (v < top) & (v > bot)
        c[inn] = WHITE
        c[inn & (((u + 0.1) / 0.22) ** 2 + ((v + 0.1) / 0.32) ** 2 < 1)] = PUPIL
        c[(np.abs(v - top) < 0.12) & (np.abs(u) < 0.98)] = INK
        return c
    def eyes(state):
        k.decal(E, de, (0.03, 0.022), eye_look(state, narrow, B, srgb(LILAC), line=0.16), up=unit(0, 0.25, 1),
                eps=0.005, rings=20, segs=56, mirror=True)

    k.build(voxel=0.0068, smooth=4, band=0.006)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    spectrier(*args())
