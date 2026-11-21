# Toxel sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/toxel.py -- <the export's Toxel.glb> <out.glb>
#
# From the references (they overrule a "baby lizard sitting up with a horn-like tuft": Toxel crawls on all fours and
# its tuft is a white lightning blaze): a purple baby on all fours, the head more than half its height and the largest
# mass, thrust forward and low. Two pointed cat's ears up at the back corners of the head and a pointed flap standing
# out sideways from each side of the head below the eyes. A white blaze shaped like a flame or a bolt up the forehead
# from between the eyes to the crown, and a white spike of lightning rising off it. Sullen half-shut eyes set low and
# wide: a heavy purple lid over the top half with a dark line along it, a blue-violet iris with a dark pupil in the
# white below. A small mouth with a pale tongue hanging out, a pale spot at each corner. A short round body behind the
# head, pale lavender on its sides and under; four stubby legs with round feet and little toes; a long tail curving up
# behind, paler toward its rippled tip. The joints: body (0, 0, 0.12), head (0, -0.03, 0.18), armR/L (+-0.06, -0.03,
# 0.13), legR/L (+-0.05, -0.02, 0.06), tail (0, 0.08, 0.07).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, inside, unit, boxy, tri, eye_look, args

PURPLE, PALE, WHITE, IRIS, INK, LID_LINE, TONGUE = ("#A65BCF", "#E2C8F2", "#F4F0F6", "#5864B6", "#221A30",
                                                    "#4A2A62", "#E8A4D8")

# --- the eye: a heavy lid over the top half with a dark line along it, a blue-violet iris low in the white ----------
CW, CI, CK, CLL = srgb(WHITE), srgb(IRIS), srgb(INK), srgb(LID_LINE)


def sullen_eye(lid, lid_edge=0.12, iris=(0.06, -0.2), iris_r=0.56, pupil_r=0.24, iris_col=IRIS):
    """An eye's open look: the white, the iris and pupil low in it, the lid's colour over the top and a dark line
    along the lid's edge and round the white's lower rim."""
    CL, CIr = srgb(lid), srgb(iris_col)
    def look(u, v):
        c = np.tile(CW, (len(u), 1))
        r_i = np.sqrt((u - iris[0]) ** 2 + ((v - iris[1]) * 0.95) ** 2)
        c[r_i < iris_r] = CIr
        c[r_i < pupil_r] = CK
        c[u * u + v * v > 0.84 ** 2] = CLL
        e = lid_edge(u) if callable(lid_edge) else lid_edge          # a slanting lid for a scowl
        c[v > e + 0.14] = CL
        c[np.abs(v - e - 0.07) < 0.07] = CLL
        return c
    return look


def lay_eyes(k, state, at, n, half, lid, look):
    f = eye_look(state, look, srgb(lid), CLL, line=0.15)
    for s in (1, -1):
        k.decal(np.array(at) * (s, 1, 1), np.array(n) * (s, 1, 1), half,
                f if s > 0 else (lambda u, v, g=f: g(-u, v)), eps=0.0022, rings=20, segs=56)


HC, HR = np.array([0.0, -0.075, 0.19]), np.array([0.1, 0.085, 0.084])


def head(k):
    k.add("head", ell(HC, HR, deform=boxy(2.3)), PURPLE, "head")
    k.add("muzzle", ell((0.0, -0.13, 0.15), (0.07, 0.05, 0.045)), PURPLE, "head")
    # the ears: thick soft cones up at the back corners, leaning out
    k.add("ear", tube([(0.055, -0.055, 0.24), (0.072, -0.05, 0.285), (0.084, -0.045, 0.318)], [0.034, 0.02, 0.006]),
          PURPLE, "head", mirror=True)
    # the flaps standing out sideways below the eyes, swept a little back
    k.add("flap", tube([(0.085, -0.07, 0.16), (0.118, -0.062, 0.155), (0.145, -0.05, 0.152)], [0.032, 0.018, 0.006]),
          PURPLE, "head", mirror=True)


def blaze_mask(P):
    """The white blaze up the forehead: wide over the brow, narrowing upward with ragged, bolt-like edges."""
    x, y, z = P[:, 0], P[:, 1], P[:, 2]
    t = np.clip((z - 0.2) / 0.075, 0, 1)
    half = 0.034 * (1 - t) ** 0.8 + 0.005
    cx = 0.009 * tri((z - 0.2) / 0.05) + 0.004
    edge = half + 0.005 * tri((z - 0.2) / 0.016)
    return inside(np.abs(x - cx) - edge, 0.0016) * inside(0.198 - z, 0.002) * inside(z - 0.278, 0.002) \
        * inside(y + 0.06, 0.01)


def body(k):
    k.add("trunk", ell((0.0, 0.05, 0.085), (0.075, 0.085, 0.07)), PURPLE, "body")
    k.add("chest", ell((0.0, -0.02, 0.1), (0.075, 0.06, 0.068)), PURPLE, "body")
    k.add("neck", ell((0.0, -0.06, 0.135), (0.07, 0.05, 0.05)), PURPLE, "head")
    # the forelegs: from the shoulders down to round feet under the front of the head
    for name, (top, foot), bone in (("fore", ((0.06, -0.03, 0.12), (0.068, -0.085, 0.022)), "arm{s}"),
                                     ("hind", ((0.055, 0.04, 0.08), (0.07, 0.085, 0.022)), "leg{s}")):
        mid = (np.array(top) + np.array(foot)) / 2 + np.array([0.008, 0.0, 0.0])
        k.add(name, tube([top, mid, foot], [0.034, 0.03, 0.026]), PURPLE, bone, mirror=True)
        f = np.array(foot) + np.array([0.0, -0.012, -0.006])
        k.add(name + "foot", ell(f, (0.033, 0.035, 0.018)), PURPLE, bone, mirror=True)
        for i, a in enumerate((-0.017, 0.0, 0.017)):
            k.add(f"{name}toe{i}", ell(f + np.array([a, -0.03, -0.004]), (0.011, 0.012, 0.01)), PURPLE, bone,
                  mirror=True)
    # the tail: from the rump curving up and back, its tip rippled
    V, F = tube([(0.0, 0.11, 0.08), (0.0, 0.17, 0.09), (0.015, 0.22, 0.12), (0.028, 0.255, 0.165), (0.03, 0.27, 0.2)],
                [0.05, 0.043, 0.035, 0.024, 0.009])
    # flattened from side to side toward the tip, its edge rippled there
    t = np.clip((V[:, 1] - 0.17) / 0.1, 0, 1)
    V[:, 0] = V[:, 0] * (1 - 0.5 * t) + 0.0025 * np.sin(V[:, 2] * 150) * t
    k.add("tail", (V, F), PALE, "tail")


def markings(k):
    k.paint(blaze_mask, WHITE, on=["head"])
    # the pale sides and underside of the body, and the tail's root fading to purple
    def sides(P):
        y, z = P[:, 1], P[:, 2]
        return inside(z - (0.085 + 0.25 * (y - 0.05)), 0.006) * inside(-0.03 - y, 0.006)
    k.paint(sides, PALE, on=["trunk", "chest"])
    k.paint(lambda P: inside(P[:, 1] - 0.13, 0.025), PURPLE, on=["tail"])


def mouth(k):
    # a small dark mouth line, the tongue hanging out of its middle, a pale spot at each corner
    def line(u, v):
        c = np.tile(srgb(PURPLE), (len(u), 1))
        c[np.abs(v - (0.25 * u * u - 0.1)) < 0.32] = srgb(LID_LINE)
        return c
    k.decal((0.0, -0.177, 0.14), unit(0, -1, -0.15), (0.028, 0.009), line, eps=0.0022, rings=10, segs=48)
    k.add("tongue", ell((0.0, -0.172, 0.123), (0.015, 0.012, 0.016), rot=(-25, 0, 0)), TONGUE, "head", fuse=False,
          surface="skin")
    k.decal((0.04, -0.162, 0.138), unit(0.5, -1, -0.1), (0.011, 0.01), PALE, eps=0.0022, rings=8, segs=32,
            mirror=True)


def spike(k):
    """The white spike of lightning rising off the blaze, a little to +X, laid back."""
    b = np.array([0.008, -0.105, 0.262])
    pts = [b + (-0.012, 0, 0), b + (0.012, 0.004, 0), b + (0, 0.012, 0.004), b + (0, -0.006, 0.002),
           b + (0.006, 0.008, 0.035), b + (-0.002, 0.004, 0.03), b + (0.012, 0.02, 0.07)]
    k.add("bolt", hull(pts), WHITE, "head", fuse=False, surface="shell")


EYE_AT = np.array([0.048, -0.153, 0.176])
EYE_N = unit(0.42, -1.0, 0.05)
EYE_HALF = (0.029, 0.021)


def toxel(src, out):
    k = Kit(src, coat="scales")
    head(k)
    body(k)
    markings(k)
    mouth(k)
    spike(k)
    k.build(voxel=0.0032, smooth=4, band=0.006, wband=0.035)
    look = sullen_eye(PURPLE)
    k.eye_states(lambda state: lay_eyes(k, state, EYE_AT, EYE_N, EYE_HALF, PURPLE, look))
    k.export(out)


if __name__ == "__main__":
    toxel(*args())
