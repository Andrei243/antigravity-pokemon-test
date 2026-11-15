# Groudon sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/groudon.py -- <the export's Groudon.glb> <out.glb>
#
# From the references: a massive red armoured dinosaur leaning forward over two huge rounded thighs fused into its
# body, short thick shins and broad feet with three white claws; a warm grey underside from the throat to the crotch;
# a high shoulder hump with big white spikes over each shoulder and a row of them down the back; thick arms hanging
# forward and down, each with a round shoulder, a white spike at the elbow and four great white claws; the head
# carried low and forward, red plates swept back off it in a crest of ridges, a red plate hanging down each cheek
# beside the grey jaw, the mouth open a little with white teeth, small yellow eyes under a black brow; black line
# markings over the armour (circles on the thighs and shoulders, bands round the limbs, lines across the back); a
# thick tail sweeping back with a fan of red plates on top and white spikes along its sides.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, ring_band, srgb, inside, smoothstep, unit, eye_look, polygon, args, sibling

lv = sibling("larvitar")

RED, GREY, INKC, WHITE = "#D8302E", "#9E9690", "#1A1216", "#F2F0EC"
MOUTHC, YELLOW = "#4A3A3A", "#F4D218"

H, HR = np.array([0.0, -0.33, 0.6]), np.array([0.105, 0.135, 0.095])       # the skull, as laid out
J, HS, SHIFT = np.array([0.0, -0.27, 0.58]), 1.3, np.array([0.0, -0.035, 0.015])   # the head grown about its joint
HG, HRG = J + (H - J) * HS + SHIFT, HR * HS


def on_ell(c, r, d):
    d = unit(*d)
    t = 1.0 / math.sqrt(((d / np.array(r)) ** 2).sum())
    return np.array(c, float) + d * t


def spike(k, name, base, d, length, width, bone, mirror=False, color=WHITE, sink=0.03):
    d = unit(*d)
    up = np.cross(d, (0, 0, 1.0)) if abs(d[2]) < 0.9 else np.array([1.0, 0, 0])
    lv.spike(k, name, base, d, length, width, width * 0.85, up, color, bone, sink=sink, mirror=mirror)


def body(k):
    # The trunk: one leaning mass, the shoulders a high hump, the hips low over the thighs
    k.add("torso", ell((0, 0.02, 0.45), (0.2, 0.26, 0.2), rot=(-14, 0, 0)), RED, "body")
    k.add("hump", ell((0, -0.08, 0.57), (0.215, 0.19, 0.155), rot=(-20, 0, 0)), RED, "body")
    k.add("chest", ell((0, -0.17, 0.46), (0.165, 0.13, 0.16)), RED, "body")
    k.add("neck", tube([(0, -0.13, 0.57), (0, -0.22, 0.6), (0, -0.28, 0.6)], [0.12, 0.105, 0.095]), RED, "head")

    # The grey underside, from the throat down the chest and belly to the crotch
    def belly(P):
        q = (P - np.array([0, -0.2, 0.36])) / np.array([0.15, 0.17, 0.27])
        return inside(np.linalg.norm(q, axis=1) - 1, 0.004) * (P[:, 1] < 0.12)
    k.paint(belly, GREY, on=["torso", "chest", "neck", "hump"])

    # Black lines across the back between the plates, and round the hump
    def seams(P):
        m = np.zeros(len(P))
        top = P[:, 2] > 0.42
        for y0 in (-0.02, 0.11, 0.22):
            m = np.maximum(m, inside(np.abs(P[:, 1] - y0 + 0.25 * (P[:, 2] - 0.6)) - 0.006, 0.002) * top)
        m = np.maximum(m, inside(np.abs(P[:, 0]) - 0.005, 0.002) * (P[:, 2] > 0.66) * (P[:, 1] > -0.05))
        return m * (1 - belly(P))
    k.paint(seams, INKC, on=["torso", "hump"])


def legs(k):
    for s, sd in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        # The thigh: a great ball fused into the hip, the shin short, the foot broad
        k.add(f"thigh{sd}", ell(np.array((0.19, 0.04, 0.27)) * m, (0.125, 0.17, 0.165)), RED, f"leg{sd}")
        k.add(f"shin{sd}", tube(np.array([(0.2, -0.0, 0.17), (0.205, -0.03, 0.07)]) * m, [0.09, 0.085]), RED,
              f"leg{sd}")
        k.add(f"foot{sd}", ell(np.array((0.205, -0.07, 0.045)) * m, (0.095, 0.13, 0.05)), RED, f"leg{sd}")
        for j, dx in enumerate((-0.05, 0.0, 0.05)):
            lv.claw(k, f"toe{sd}{j}", np.array((0.205 + dx, -0.185, 0.04)) * m, np.array((dx * 0.6, -1, -0.25)) * m,
                    0.06, 0.045, f"leg{sd}")
        # The white spike out of the knee's side
        spike(k, f"knee{sd}", np.array((0.3, -0.02, 0.26)) * m, np.array((1, -0.35, 0.2)) * m, 0.09, 0.055, f"leg{sd}")
        # A black ring round the shin
        k.add(f"shinband{sd}", ring_band(np.array((0.2, -0.015, 0.13)) * m, (0, -0.2, 1), 0.091, 0.014, thick=0.006),
              INKC, f"leg{sd}", fuse=False)

    # The circle on each thigh's side, with a line across it
    def ring(u, v):
        r = np.sqrt(u * u + v * v)
        c = np.tile(srgb(RED), (len(u), 1))
        c[(r > 0.8) | (np.abs(u) < 0.07) & (r < 0.8) & (v < 0)] = srgb(INKC)
        return c
    k.decal((0.312, 0.05, 0.27), (1, 0, 0), (0.09, 0.09), ring, eps=0.004, rings=24, segs=64, mirror=True)


def arms(k):
    for s, sd in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"shoulder{sd}", ell(np.array((0.2, -0.1, 0.55)) * m, (0.095, 0.095, 0.1)), RED, f"arm{sd}")
        k.add(f"arm{sd}", tube(np.array([(0.21, -0.11, 0.53), (0.25, -0.17, 0.44), (0.25, -0.26, 0.36)]) * m,
                               [0.078, 0.07, 0.066]), RED, f"arm{sd}")
        k.add(f"hand{sd}", ell(np.array((0.25, -0.285, 0.33)) * m, (0.07, 0.065, 0.06)), RED, f"arm{sd}")
        for j, (dx, dy) in enumerate(((-0.045, 0.0), (-0.015, -0.025), (0.017, -0.025), (0.047, 0.0))):
            lv.claw(k, f"claw{sd}{j}", np.array((0.25 + dx, -0.31 + dy, 0.29)) * m, np.array((dx * 0.7, -0.45, -1)) * m,
                    0.085, 0.04, f"arm{sd}")
        spike(k, f"elbow{sd}", np.array((0.3, -0.13, 0.45)) * m, np.array((0.75, 0.55, -0.1)) * m, 0.1, 0.055,
              f"arm{sd}")
        k.add(f"armband{sd}", ring_band(np.array((0.25, -0.22, 0.4)) * m, np.array((0, -0.75, -0.66)), 0.07, 0.014,
                                        thick=0.006), INKC, f"arm{sd}", fuse=False)
    # The big white spikes over the shoulders, and the row down the back
    for j, (b, d, ln, w) in enumerate((((0.2, -0.04, 0.68), (0.6, 0.25, 0.75), 0.15, 0.08),
                                       ((0.16, 0.06, 0.66), (0.55, 0.65, 0.5), 0.13, 0.07),
                                       ((0.17, 0.17, 0.58), (0.6, 0.75, 0.25), 0.12, 0.065),
                                       ((0.17, -0.18, 0.6), (0.85, -0.35, 0.35), 0.11, 0.06))):
        spike(k, f"back{j}", b, d, ln, w, "body", mirror=True)


def tail(k):
    T = [(0, 0.18, 0.4), (0.03, 0.36, 0.28), (0.08, 0.52, 0.17), (0.13, 0.64, 0.12), (0.17, 0.72, 0.1)]
    R = [0.16, 0.12, 0.085, 0.055, 0.025]
    k.add("tail", tube(T, R), RED, "tail")
    pts = np.array(T)

    def at(t):
        i = t * (len(pts) - 1); a = min(int(i), len(pts) - 2); f = i - a
        return pts[a] * (1 - f) + pts[a + 1] * f, np.interp(t, np.linspace(0, 1, len(R)), R), unit(*(pts[a + 1] - pts[a]))
    # The fan of red plates standing on the tail, leaning back and out to +X so their faces show
    for j, t in enumerate((0.3, 0.45, 0.6, 0.75)):
        c, r, d = at(t)
        ln = 0.2 - 0.03 * j
        up = unit(*(np.array([0.45, 0.0, 1.0]) + d * 0.35))
        base = c + np.array([0, 0, r * 0.75])
        tip = base + up * ln
        side = np.cross(d, up); side /= np.linalg.norm(side)
        w, th = 0.075, 0.035
        P = [base - d * w + side * th, base - d * w - side * th, base + d * w * 0.6 + side * th,
             base + d * w * 0.6 - side * th, tip + side * th * 0.4, tip - side * th * 0.4,
             tip - d * 0.05 + side * th * 0.5, tip - d * 0.05 - side * th * 0.5,
             base - up * 0.03]
        k.add(f"plate{j}", hull(P), RED, "tail", fuse=False, surface="shell")
    # White spikes out of its sides
    for j, t in enumerate((0.25, 0.5, 0.72)):
        c, r, d = at(t)
        for s, sd in ((1, "R"), (-1, "L")):
            out = np.cross((0, 0, 1.0), d) * -s
            out = unit(*(out * 1.0 + d * 0.35))
            spike(k, f"tspike{sd}{j}", c + out * r * 0.95, out, 0.19 - 0.03 * j, 0.065, "tail", sink=0.015)


def head(k):
    k.add("skull", ell(H, HR), RED, "head")
    k.add("snout", ell((0, -0.44, 0.59), (0.08, 0.09, 0.07), rot=(-8, 0, 0)), RED, "head")
    k.add("brow", ell((0.06, -0.39, 0.64), (0.045, 0.07, 0.03), rot=(-10, 0, -20)), RED, "head", mirror=True)
    # The grey lower jaw, hanging a little open
    k.add("jaw", ell((0, -0.41, 0.49), (0.075, 0.11, 0.05), rot=(18, 0, 0)), GREY, "head")
    k.add("mouth", ell((0, -0.5, 0.535), (0.06, 0.1, 0.028), rot=(8, 0, 0)), MOUTHC, "head", carve=True)
    # Teeth along the upper lip, few and bold
    for j, x in enumerate((0.04, 0.0, -0.04)):
        a = np.array((x, -0.505 + 0.012 * abs(x) / 0.04, 0.56))
        k.add(f"tooth{j}", hull([a + (0.013, 0, 0), a - (0.013, 0, 0), a + (0, 0.012, 0), a - (0, 0.008, 0),
                                 a + (0, -0.003, -0.032)]), WHITE, "head", fuse=False, surface="shell")
    # The crest: red ridges swept back off the crown, the middle one longest
    for j, (x, z, ln) in enumerate(((0.0, 0.69, 0.2), (0.055, 0.67, 0.17), (0.09, 0.63, 0.14))):
        for s in ((1,) if x == 0 else (1, -1)):
            b = np.array((x * s, -0.33, z))
            d = unit(0.2 * s * (x > 0), 0.85, 0.5)
            side = np.array([1.0, 0, 0]); w = 0.03
            upn = np.cross(side, d)
            P = [b + side * w, b - side * w, b - d * 0.07 + side * w * 0.6, b - d * 0.07 - side * w * 0.6,
                 b + d * ln, b + d * ln * 0.6 + upn * 0.03, b - upn * 0.04 + d * 0.05, b - d * 0.05 - upn * 0.03]
            k.add(f"crest{j}{s}", hull(P), RED, "head", fuse=False, surface="shell")
    # A red plate hanging down each cheek beside the jaw
    for s, sd in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        P = np.array([(0.085, -0.36, 0.58), (0.105, -0.3, 0.58), (0.095, -0.4, 0.52), (0.105, -0.34, 0.47),
                      (0.075, -0.4, 0.5), (0.065, -0.33, 0.48), (0.07, -0.3, 0.57)]) * m
        k.add(f"cheek{sd}", hull(P), RED, "head", fuse=False, surface="shell")


CR, CK, CY, CW = srgb(RED), srgb(INKC), srgb(YELLOW), np.array([1.0, 1.0, 1.0])


def eye_open(u, v):
    c = np.tile(CY, (len(u), 1))
    c[(u / 0.34) ** 2 + (v / 0.42) ** 2 < 1] = CK
    return c


DE = unit(0.62, -0.72, 0.25)
CUT = lv.brow_cut(0.32, 0.42)


def eyes(k, state):
    E = on_ell(HG, HRG, DE) + np.array([0, -0.008, 0])
    look = eye_look(state, eye_open, CR, CK, line=0.2)
    m = np.array([-1, 1, 1.0])
    # the black brow mark first, a slanted band over the eye
    for s in (1, -1):
        mm = np.array([s, 1, 1.0])
        k.decal(E * mm + np.array([0, -0.004, 0.018]), DE * mm, (0.042 * HS, 0.022 * HS), CK, eps=0.004, rings=8, segs=48,
                outline=polygon([(-1, -0.6), (1, 0.2), (1, 1), (-1, 0.2)] if s > 0 else
                                [(1, -0.6), (-1, 0.2), (-1, 1), (1, 0.2)]))
    k.decal(E, DE, (0.03 * HS, 0.027 * HS), look, eps=0.007, rings=20, segs=56, outline=CUT)
    k.decal(E * m, DE * m, (0.03 * HS, 0.027 * HS), lambda u, v: look(-u, v), eps=0.007, rings=20, segs=56,
            outline=lambda th: CUT(math.pi - th))


def groudon(src, out):
    k = Kit(src, coat="shell")
    body(k)
    legs(k)
    arms(k)
    tail(k)
    n0 = len(k.parts)
    head(k)
    for p in k.parts[n0:]:
        p["V"] = J + (p["V"] - J) * HS + SHIFT
    k.build(voxel=0.008, smooth=4, band=0.006)
    k.eye_states(lambda state: eyes(k, state))
    k.export(out)


if __name__ == "__main__":
    groudon(*args())
