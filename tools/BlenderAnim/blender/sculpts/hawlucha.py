# Hawlucha sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/hawlucha.py -- <the export's Hawlucha.glb> <out.glb>
# From the references: a small hawk of the wrestling ring in a luchador's mask. The head wears a green mask over the
# crown, the back and round the eyes, the lower face and chin white; a red hooked beak at the front; fierce yellow eyes
# with a black pupil, ringed in orange and black, cut straight by the mask's brow sloping down to the beak; an orange
# knot at the back of the crown with two short ribbon ends. Red shoulders, a red back and red arms held forward and a
# little down, each hand red with three fingers tipped yellow; the chest and belly white, the red coming down over the
# chest in points; a wing of long green feathers hangs from under each arm like a cape, a mirror pair splayed out
# toward the front. White fluffy thighs ending in points of down, short yellow shins and yellow feet of three toes.
# The pictures overrule the task's text: the underside is white, not green; the green is the mask and the wings.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, outward, srgb, inside, tri, unit, polygon, eye_look, args, sibling

T = sibling("treecko")
P = sibling("helioptile")
GREEN, RED, WHITE_, ORANGE = "#2FAE88", "#E0303E", "#F4F2EE", "#F58A2A"
YELLOW, BEAK = "#F4CE3A", "#E42A38"
MOUTH = "#B4505E"
INK, WHITE = srgb("#15161C"), np.array([1.0, 1.0, 1.0])
GOLD, RING = srgb("#F8C820"), srgb("#F07A22")
VOXEL = 0.005

H = np.array([0.0, -0.032, 0.416])             # the head
HR = np.array([0.074, 0.078, 0.087])
EYE_DIR = unit(0.6, -0.74, 0.24)
EYE_HALF = (0.029, 0.025)


def mask(Pn):
    """The green mask: over the crown and the back, down past the eyes to the beak at the front; white below."""
    q = (Pn - H) / HR
    a = np.arctan2(np.abs(q[:, 0]), -q[:, 1])
    edge = np.interp(a, [0, 0.45, 0.9, 1.5, 2.2, 3.15], [-0.12, -0.18, -0.3, -0.42, -0.62, -0.7])
    near = inside((q ** 2).sum(1) - 1.25, 0.02) * (Pn[:, 2] > 0.3)
    return inside(edge - q[:, 2], 0.012) * near


def chest_edge(Pn):
    """The line between the red over the shoulders and back and the white of the chest: in points, the red dipping
    lowest at the middle of the front, high at the sides, low over the back."""
    ph = np.arctan2(Pn[:, 0], -Pn[:, 1])
    base = np.interp(np.abs(ph), [0, 0.5, 1.2, 1.9, 3.15], [0.268, 0.29, 0.31, 0.27, 0.2])
    return base + 0.012 * tri(ph * 9 / (2 * math.pi)) - 0.03 * np.exp(-(ph / 0.22) ** 2)


def hawlucha(src, out):
    k = Kit(src, coat="fur")

    # The body: a white egg of a belly, red shoulders over it, a short thick neck into the head
    k.add("belly", ell((0, 0.01, 0.225), (0.084, 0.074, 0.1)), WHITE_, "body")
    k.add("shoulders", ell((0, 0.006, 0.305), (0.096, 0.07, 0.056)), RED, "body")
    k.add("neck", ell((0, -0.012, 0.345), (0.058, 0.056, 0.05)), WHITE_, "head")
    k.add("head", ell(H, HR), GREEN, "head")
    k.add("cheek", ell(H + (0, -0.03, -0.035), (0.058, 0.05, 0.045)), WHITE_, "head")
    k.paint(lambda Pn: inside(chest_edge(Pn) - Pn[:, 2], 0.0025) * inside(np.abs(Pn[:, 0]) - 0.11, 0.004)
            * inside(Pn[:, 2] - 0.345, 0.004) * inside(0.17 - Pn[:, 2], 0.004), RED)
    k.paint(lambda Pn: inside(Pn[:, 2] - chest_edge(Pn), 0.0025) * inside(np.abs(Pn[:, 0]) - 0.1, 0.004)
            * inside(0.2 - Pn[:, 2], 0.004) * inside(Pn[:, 1] + 0.0, 0.004), WHITE_, on=["belly", "shoulders", "neck"])
    k.paint(mask, GREEN)
    k.paint(lambda Pn: (1 - mask(Pn)) * inside(((Pn - H) / (HR * 1.15)) ** 2 @ np.ones(3) - 1, 0.02)
            * (Pn[:, 2] > 0.33), WHITE_)

    # The red hooked beak at the front of the mask
    b0 = H + np.array([0, -0.068, 0.012])
    k.add("beak", hull([b0 + (0.017, 0.0, 0.01), b0 + (-0.017, 0.0, 0.01), b0 + (0.013, 0.0, -0.022),
                        b0 + (-0.013, 0.0, -0.022), b0 + (0.009, -0.032, 0.0), b0 + (-0.009, -0.032, 0.0),
                        b0 + (0, -0.045, -0.026), b0 + (0, 0.012, 0.028)]), BEAK, "head", fuse=False, surface="shell")
    # The small open mouth under it, on the white
    k.decal(P.on_ell(H + (0, -0.03, -0.035), (0.058, 0.05, 0.045), (0, -0.85, -0.5)) + (0, 0, 0.004),
            unit(0, -0.85, -0.45), (0.014, 0.009), MOUTH, eps=0.003, rings=8, segs=32)

    # The orange knot at the back of the crown and its two ribbon ends, back and out to the +X side
    knot = H + np.array([0.01, 0.058, 0.05])
    k.add("knot", outward(ell(knot, (0.019, 0.016, 0.017))), ORANGE, "head", fuse=False, surface="cloth")
    for j, (d, ln) in enumerate([((0.5, 0.45, 0.75), 0.075), ((0.75, 0.55, 0.05), 0.065)]):
        k.add(f"ribbon{j}", T.leaf_blade(knot, d, (0.2, -0.9, 0.3), ln, 0.022, 0.016, widest=0.55, tip=0.012),
              ORANGE, "head", fuse=False, surface="cloth")

    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        # Legs: white fluffy thighs fused into the belly with points of down at their foot, yellow shins and feet
        k.add(f"thigh{side}", ell(np.array((0.054, 0.004, 0.13)) * m, (0.052, 0.058, 0.07)), WHITE_, f"leg{side}")
        for j in range(5):
            a = -0.9 + 1.8 * j / 4
            dd = np.array([math.sin(a) * 0.8 + 0.25, -math.cos(a) * 0.9, -1.0])
            base = np.array((0.056 + 0.04 * math.sin(a), 0.004 - 0.045 * math.cos(a), 0.082))
            tip = base + unit(*dd) * 0.03
            k.add(f"down{side}{j}", hull([(base + (0.012, 0, 0)) * m, (base - (0.012, 0, 0)) * m,
                                          (base + (0, 0.012, 0)) * m, (base - (0, 0.012, 0)) * m,
                                          (base + (0, 0, 0.012)) * m, tip * m]), WHITE_, f"leg{side}", fuse=False)
        k.add(f"shin{side}", tube(np.array([(0.058, -0.004, 0.085), (0.06, -0.014, 0.025)]) * m, [0.021, 0.019]),
              YELLOW, f"leg{side}")
        k.add(f"foot{side}", ell(np.array((0.061, -0.03, 0.012)) * m, (0.024, 0.036, 0.012)), YELLOW, f"leg{side}",
              fuse=False, surface="shell")
        P.toes(k, f"toe{side}", f"leg{side}", np.array((0.062, -0.06, 0.009)) * m, (0.2 * s, -1, 0), 0.03, 0.0085,
               colour=YELLOW)

        # Arms: red, forward and a little down, a red hand of three fingers tipped yellow
        sh, el, wr = np.array((0.06, 0.0, 0.306)), np.array((0.13, -0.05, 0.272)), np.array((0.094, -0.15, 0.236))
        k.add(f"pad{side}", ell(np.array((0.078, 0.0, 0.31)) * m, (0.046, 0.046, 0.04)), RED, f"wing{side}")
        k.add(f"arm{side}", tube(np.array([sh, el, wr]) * m, [0.03, 0.026, 0.023]), RED, f"wing{side}")
        hand = wr + np.array([0, -0.02, -0.004])
        k.add(f"hand{side}", ell(hand * m, (0.026, 0.024, 0.02)), RED, f"wing{side}")
        f = unit(*(wr - el))
        sd = unit(*np.cross(f, (0, 0, 1)))
        for j, o in enumerate((-1, 0, 1)):
            b = hand + f * 0.016 + sd * o * 0.014 + np.array([0, 0, -0.004])
            tip = b + unit(*(f + sd * o * 0.25 + np.array([0, 0, -0.25]))) * 0.022
            k.add(f"finger{side}{j}", outward(tube([b * m, tip * m], [0.0085, 0.0085])), RED, f"wing{side}",
                  fuse=False)
            k.add(f"nail{side}{j}", outward(ell((tip + unit(*(tip - b)) * 0.006) * m, (0.009, 0.009, 0.009), sub=2)),
                  YELLOW, f"wing{side}", fuse=False, surface="shell")

        # The wing: a fan of long green feathers hanging from under the arm like a cape, splayed out and turned
        # toward the front so its faces show to the game's cameras
        for j in range(6):
            t = j / 5
            root = sh + (el - sh) * min(t * 1.6, 1) + (wr - el) * max(t * 1.6 - 1, 0) + np.array([0.0, 0.01, -0.022])
            d = unit(0.34 + 0.26 * t, 0.32 - 0.2 * t, -1.0)
            ln = 0.15 + 0.06 * math.sin(math.pi * (0.25 + 0.6 * t))
            k.add(f"feather{side}{j}", T.leaf_blade(root * m, d * m, (0.65 * s, -0.75, 0.1), ln, 0.05, 0.02,
                                                    widest=0.4, tip=0.004), GREEN, f"wing{side}", fuse=False)
        # a web behind the upper half of the fan, so the far wing is no comb from behind
        web = []
        for j in range(6):
            t = j / 5
            root = sh + (el - sh) * min(t * 1.6, 1) + (wr - el) * max(t * 1.6 - 1, 0) + np.array([0.0, 0.016, -0.022])
            d = unit(0.34 + 0.26 * t, 0.32 - 0.2 * t, -1.0)
            ln = 0.15 + 0.06 * math.sin(math.pi * (0.25 + 0.6 * t))
            fc = unit(0.65, -0.75, 0.1)
            for p in (root, root + d * ln * 0.58):
                web += [(p + fc * 0.006) * m, (p - fc * 0.006) * m]
        k.add(f"web{side}", hull(web), GREEN, f"wing{side}", fuse=False)
        k.add(f"wingroot{side}", outward(T.ell_frame((sh + el) / 2 * m + np.array([0.004 * s, 0.006, -0.03]),
                                                     (0.012, 0.06, 0.03), (s, 0, 0), unit(*((el - sh) * m)),
                                                     (0, 0, 1))), GREEN, f"wing{side}", fuse=False)

    k.build(voxel=VOXEL, smooth=4, band=0.005, wband=0.04)

    # The eyes: yellow with a black pupil, ringed in orange and black, cut by the mask's brow sloping to the beak
    def top(u):
        return 0.35 + 0.35 * (u + 1) / 2                     # low by the beak (u -1), higher outside

    def open_eye(u, v):
        rho = np.sqrt(u * u + v * v)
        c = np.tile(GOLD, (len(u), 1))
        c[((u + 0.05) / 0.22) ** 2 + ((v + 0.05) / 0.42) ** 2 < 1] = INK
        c[np.hypot(u - 0.2, v - 0.2) < 0.13] = WHITE
        c[rho > 0.64] = RING
        c[rho > 0.86] = INK
        c[np.abs(v - top(u)) < 0.14] = INK
        return c
    outline = [(math.cos(a), min(math.sin(a), top(math.cos(a)) + 0.12)) for a in np.linspace(0, 2 * math.pi, 48,
                                                                                            endpoint=False)]
    E = P.on_ell(H, HR, EYE_DIR)

    def lay(state):
        look = eye_look(state, open_eye, srgb(GREEN), INK, line=0.16)
        for s in (1, -1):
            shape = polygon([(u * s, v) for u, v in outline]) if state in ("Open", "Fierce") else None
            col = look if s > 0 else (lambda u, v, f_=look: f_(-u, v))
            k.decal(E * np.array([s, 1, 1]), EYE_DIR * np.array([s, 1, 1]), EYE_HALF, col, up=(0, 0, 1), eps=0.003,
                    outline=shape, rings=20, segs=56)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    hawlucha(*args())
