# Zeraora sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/zeraora.py -- <the export's Zeraora.glb> <out.glb>
#
# From the references: a lean yellow-and-black cat standing upright, all lightning. A yellow-furred head, spiky, its
# face dark grey to a short muzzle, the mouth open (dark red, an orange tongue), fierce blue eyes under a slanting brow,
# spiky yellow locks hanging over the brow; black pointed ears with orange insides on top; a great blue crescent rising
# beside each side of the head and a small blue bolt of a whisker out from each cheek (the art, turned three-quarter,
# shows one crescent; counted as a pair). A spiky yellow ruff round the neck and shoulders, a black chest with a blue
# chevron of lightning at the collar, a pinched black waist. Thin black upper arms and great yellow gauntlet forearms
# and fists, blue pads on the fists, white claws. Great yellow haunches with black lightning bolts, fur spiking at the
# knees, black shins and feet, white claws. A long yellow plume of a tail, broad, golden toward its jagged tip.
# The art's lunge is not the rest pose: here it stands, chest out, arms forward and down.
# Ratios (3D picture): head (chin to ears' tips) / height 0.22 (built 0.27, a fifth larger); forearm width / head width
# 0.55; tail longer than the body. The export stands 0.70 tall: voxel 0.0065, the thickness floor 0.021.
# Bones: body (0, 0, 0.4); head (0, -0.03, 0.52); arms (+-0.07, 0, 0.48); legs (+-0.05, 0.01, 0.3); tail (0, 0.05, 0.32).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, blade, hull, slab, srgb, inside, tri, unit, eye_look, polygon, lumpy, args,
                       outward)

YELLOW, GOLD, BLACK, FACE = "#F4D42A", "#C2901E", "#33333B", "#45454E"
BLUE, ORANGE, WHITE = "#3CB4F0", "#E58A44", "#F2F2F4"
MOUTH, TONGUE, NOSE = "#9E2420", "#F0743E", "#15151A"
EYE_W, EYE_BLUE, EYE_INK = "#F6F8FA", "#2F93DE", "#101418"

H = np.array([0.0, -0.035, 0.606])          # the head's centre
HR = np.array([0.09, 0.083, 0.081])
MC = np.array([0.0, -0.108, 0.578])       # the muzzle's
MR = np.array([0.048, 0.042, 0.036])
SHOULDER = np.array([0.088, 0.0, 0.492])
ELBOW = np.array([0.128, -0.03, 0.43])
WRIST = np.array([0.108, -0.105, 0.39])
FIST = np.array([0.098, -0.13, 0.372])


def on_ell(c, r, d):
    d = unit(*d)
    s = 1 / math.sqrt(((d / np.array(r)) ** 2).sum())
    p = np.array(c) + d * s
    n = (p - c) / np.array(r) ** 2
    return p, n / np.linalg.norm(n)


def spike(base, d, length, w, t, up=(0, 0, 1)):
    """A pointed tuft or claw: a kite-section root at base narrowing to a point length along d (a hull)."""
    base = np.array(base, float); d = unit(*d)
    s = np.cross(d, up)
    if np.linalg.norm(s) < 1e-3:
        s = np.cross(d, (1, 0, 0))
    s /= np.linalg.norm(s)
    f = np.cross(s, d)
    return hull([base + s * w / 2, base - s * w / 2, base + f * t / 2, base - f * t / 2, base - d * 0.006,
                 base + d * length])


# --- the head -----------------------------------------------------------------------------------------------------------
def add_head(k):
    k.add("head", ell(H, HR), YELLOW, "head")
    k.add("muzzle", ell(MC, MR, rot=(-8, 0, 0)), FACE, "head")
    k.add("jowl", ell((0, -0.068, 0.556), (0.062, 0.054, 0.036)), YELLOW, "head")
    # the face dark from the brow down to the muzzle, the yellow framing it at the sides and the top
    def face(P):
        q = P - H
        a = np.arctan2(q[:, 0], -q[:, 1])
        top = H[2] + 0.02 - 0.03 * (np.abs(a) / 0.9) ** 2
        return inside(P[:, 2] - top, 0.003) * inside(np.abs(a) - 0.9, 0.04) * (q[:, 1] < -0.02) \
            * inside(0.528 - P[:, 2], 0.004)
    k.paint(face, FACE, on=["head", "jowl", "muzzle"])
    # the spiky yellow locks hanging over the brow
    for x, d, ln in ((0.0, (0, -0.55, -0.83), 0.05), (0.03, (0.25, -0.6, -0.76), 0.042),
                     (-0.03, (-0.25, -0.6, -0.76), 0.042)):
        k.add(f"lock{x:+.2f}", spike((x, -0.084, 0.664), d, ln * 1.1, 0.032, 0.024), YELLOW, "head", fuse=False)
    # spiky mane behind and beside the head
    for x, y, z, d, ln in ((0.0, 0.035, 0.672, (0, 0.7, 0.5), 0.07), (0.055, 0.035, 0.652, (0.5, 0.7, 0.2), 0.065),
                           (-0.055, 0.035, 0.652, (-0.5, 0.7, 0.2), 0.065), (0.0, 0.048, 0.605, (0, 0.85, -0.3), 0.07),
                           (0.08, -0.01, 0.572, (0.85, 0.2, -0.5), 0.05), (-0.08, -0.01, 0.572, (-0.85, 0.2, -0.5), 0.05)):
        k.add(f"mane{x:+.2f}{z}", spike((x, y, z), d, ln, 0.04, 0.026), YELLOW, "head", fuse=False)
    # black pointed ears with orange insides
    k.add("ear", hull([(0.032, 0.006, 0.664), (0.072, 0.008, 0.654), (0.05, 0.026, 0.664), (0.052, -0.01, 0.664),
                       (0.08, 0.006, 0.742)]), BLACK, "head", fuse=False, mirror=True)
    k.add("earin", hull([(0.043, -0.011, 0.672), (0.065, -0.009, 0.668), (0.055, 0.0, 0.674), (0.074, -0.004, 0.722),
                         (0.054, -0.015, 0.676)]), ORANGE, "head", fuse=False, mirror=True)
    # a great blue crescent rising beside each side of the head, curving forward over the face's edge
    cres = [(0.0, 0.0), (0.035, 0.03), (0.055, 0.07), (0.055, 0.11), (0.04, 0.145), (0.02, 0.165), (0.022, 0.13),
            (0.028, 0.1), (0.022, 0.065), (0.005, 0.035), (-0.015, 0.015)]
    n = unit(0.85, -0.45, 0.0)
    ex = unit(-0.45, -0.85, 0.0)
    k.add("crescent", slab(cres, (0.08, -0.05, 0.548), ex, (0, 0, 1), 0.024), BLUE, "head", fuse=False,
          mirror=True, surface="shell")
    # a blue bolt of a whisker out from each cheek
    bolt = [(0.0, 0.008), (0.03, 0.013), (0.026, 0.003), (0.056, 0.006), (0.024, -0.012), (0.028, -0.002),
            (0.0, -0.008)]
    k.add("whisker", slab(bolt, (0.07, -0.08, 0.556), unit(1, 0.35, -0.15), (0, 0, 1), 0.021), BLUE, "head",
          fuse=False, mirror=True, surface="shell")
    # the nose, and the open mouth: one shape, dark red with an orange tongue
    k.paint(lambda P: inside(np.linalg.norm((P - (MC + (0, -0.036, 0.016))) / (1, 1.6, 1), axis=1) - 0.011, 0.002),
            NOSE, on=["muzzle"])
    p, nm = on_ell(MC, MR, (0, -0.72, -0.55))
    def mouth(u, v):
        c = np.tile(srgb(MOUTH), (len(u), 1))
        c[(u / 0.62) ** 2 + ((v + 0.45) / 0.5) ** 2 < 1] = srgb(TONGUE)
        return c
    k.decal(p, nm, (0.026, 0.016), mouth, eps=0.002, rings=10, segs=48,
            outline=lambda th: 1.0 if math.sin(th) < 0 else 0.62)


# --- the body ----------------------------------------------------------------------------------------------------------
def add_body(k):
    k.add("neck", ell((0, -0.01, 0.53), (0.05, 0.045, 0.04)), BLACK, "body")
    k.add("chest", ell((0, -0.008, 0.47), (0.074, 0.058, 0.066)), BLACK, "body")
    k.add("waist", ell((0, 0.004, 0.395), (0.052, 0.046, 0.05)), BLACK, "body")
    k.add("hips", ell((0, 0.008, 0.33), (0.07, 0.058, 0.05)), BLACK, "body")
    # the spiky yellow ruff round the neck and over the shoulders
    k.add("ruff", ell((0, 0.004, 0.528), (0.08, 0.068, 0.03), deform=lumpy(6, 1, 0.18)), YELLOW, "body")
    k.add("tuft", ell(SHOULDER + (0.0, 0, 0.004), (0.04, 0.042, 0.04)), YELLOW, "arm{s}", mirror=True)
    for d, ln in (((0.5, 0.2, 0.84), 0.05), ((0.9, 0.1, -0.1), 0.045), ((0.3, 0.75, 0.3), 0.045)):
        k.add(f"tuftspike{d[2]}", spike(SHOULDER + np.array(d) * 0.03, d, ln, 0.034, 0.024), YELLOW, "arm{s}",
              fuse=False, mirror=True)
    # the blue chevron of lightning at the collar: two slanted arms meeting low in the middle
    for side in (1, -1):
        poly = [(side * u, v) for u, v in ((-1, -0.6), (-1, -0.1), (1, 0.65), (1, 0.15))]
        if side < 0:
            poly = poly[::-1]
        k.decal((0.026 * side, -0.062, 0.495), unit(0.3 * side, -1, 0.15), (0.03, 0.028), BLUE, eps=0.003,
                outline=polygon(poly), rings=8, segs=48)


def add_arms(k):
    k.add("upper", tube([SHOULDER, (SHOULDER + ELBOW) / 2, ELBOW], [0.025, 0.022, 0.024]), BLACK, "arm{s}",
          mirror=True)
    k.add("fore", tube([ELBOW, (ELBOW + WRIST) / 2, WRIST], [0.036, 0.042, 0.044]), YELLOW, "arm{s}", mirror=True)
    k.add("fist", ell(FIST, (0.044, 0.044, 0.044)), YELLOW, "arm{s}", mirror=True)
    # a black bolt along the forearm's outside
    def bolt(P):
        x = np.abs(P[:, 0])
        t = (P[:, 2] - WRIST[2]) / (ELBOW[2] - WRIST[2])
        line = 0.15 + 0.008 * tri(t * 2.2)
        return inside(np.abs(x - line) - 0.006, 0.002) * (t > 0.15) * (t < 0.85) * inside(P[:, 1] + 0.03, 0.01) \
            * inside(-0.11 - P[:, 1], 0.01) * (x > 0.13)
    pass
    # white claws curling down from the fist's front
    for i, t in enumerate((-1, 0, 1)):
        base = FIST + np.array([0.018 * t, -0.03, -0.024])
        k.add(f"claw{i}", spike(base, (0.1 * t, -0.45, -0.9), 0.03, 0.02, 0.022), WHITE, "arm{s}", fuse=False,
              mirror=True, surface="shell")
    # the blue pad on each fist's front, and two blue spots on the forearm's inside
    for side in (1, -1):
        m = np.array([side, 1, 1.0])
        k.decal(FIST * m + np.array([0, -0.043, 0.0]), (0, -1, 0), (0.024, 0.022), BLUE, eps=0.003, rings=10, segs=40)
        k.decal((0.082 * side, -0.07, 0.41), unit(-0.6 * side, -0.8, 0.1), (0.011, 0.011), BLUE, eps=0.003,
                rings=8, segs=36)


def add_legs(k):
    # great yellow haunches fused into the hips, fur spiking at the knees; black shins and feet
    k.add("thigh", ell((0.064, -0.014, 0.258), (0.06, 0.068, 0.1)), YELLOW, "leg{s}", mirror=True)
    k.add("shin", tube([(0.07, -0.02, 0.2), (0.074, -0.004, 0.12), (0.072, 0.008, 0.055)], [0.04, 0.035, 0.031]),
          BLACK, "leg{s}", mirror=True)
    k.add("foot", ell((0.072, -0.03, 0.026), (0.036, 0.058, 0.026)), BLACK, "leg{s}", mirror=True)
    for i, (x, d) in enumerate(((0.06, (-0.3, -0.5, -0.8)), (0.09, (0.4, -0.5, -0.8)), (0.075, (0, -0.7, -0.7)))):
        k.add(f"knee{i}", spike((x, -0.066, 0.192), d, 0.045, 0.034, 0.024), YELLOW, "leg{s}", fuse=False, mirror=True)
    for i, t in enumerate((-1, 0, 1)):
        k.add(f"toe{i}", spike((0.072 + 0.016 * t, -0.08, 0.016), (0.1 * t, -1, -0.35), 0.026, 0.02, 0.021), WHITE,
              "leg{s}", fuse=False, mirror=True, surface="shell")
    # the black lightning bolt down each haunch's front and outside
    def bolt(P):
        x = np.abs(P[:, 0])
        q = np.stack([x - 0.066, P[:, 1] + 0.012], axis=1)
        a = np.arctan2(q[:, 0], -q[:, 1])
        z = P[:, 2]
        path = 0.75 + 0.32 * tri((z - 0.2) * 9)
        return inside(np.abs(a - path) * 0.066 - 0.008, 0.002) * (z > 0.2) * (z < 0.34)
    k.paint(bolt, BLACK, on=["thigh"])


def add_tail(k):
    # a long broad plume, back and out to +X, golden toward its jagged tip
    path = [(0.0, 0.05, 0.34), (0.03, 0.12, 0.36), (0.085, 0.2, 0.36), (0.14, 0.27, 0.335), (0.185, 0.33, 0.3),
            (0.22, 0.38, 0.255)]
    k.add("tail", blade(path, [0.028, 0.045, 0.056, 0.05, 0.034, 0.012], unit(0.82, -0.57, 0), 0.45), YELLOW,
          "tail")
    k.paint(lambda P: inside(0.27 - P[:, 1], 0.03) * (P[:, 0] > 0.08), GOLD, on=["tail"])
    for i, (b, d, ln) in enumerate((((0.16, 0.3, 0.3), (0.3, 0.6, -0.75), 0.06),
                                    ((0.2, 0.35, 0.27), (0.35, 0.75, -0.55), 0.065),
                                    ((0.21, 0.375, 0.26), (0.55, 0.8, -0.2), 0.05))):
        k.add(f"tailspike{i}", spike(b, d, ln, 0.04, 0.024), GOLD, "tail", fuse=False)


# --- the eyes: white, a big blue iris and a black pupil, the top cut by a brow sloping to the nose -----------------------
def top(u):
    return 0.15 + 0.5 * u


ALMOND = [(u, top(u)) for u in np.linspace(1, -1, 9)] + \
         [(u, -0.62 * (1 - u * u) ** 0.5 - 0.12) for u in np.linspace(-0.92, 0.92, 13)]


def open_eye(u, v):
    c = np.tile(srgb(EYE_W), (len(u), 1))
    c[((u + 0.05) / 0.62) ** 2 + ((v + 0.05) / 0.7) ** 2 < 1] = srgb(EYE_BLUE)
    c[((u + 0.08) / 0.26) ** 2 + ((v + 0.05) / 0.4) ** 2 < 1] = srgb(EYE_INK)
    c[((u - 0.16) / 0.14) ** 2 + ((v - 0.12) / 0.14) ** 2 < 1] = srgb(EYE_W)
    return c


def eyes(k, state):
    look = eye_look(state, open_eye, srgb(FACE), srgb("#9AA0AC"), line=0.17)
    for side in (1, -1):
        p, n = on_ell(H, HR, (0.42 * side, -0.88, 0.0))
        outline = polygon([(side * u, v) for u, v in (ALMOND if side == 1 else ALMOND[::-1])])
        col = look if side == 1 else (lambda a, b, g=look: g(-a, b))
        k.decal(p, n, (0.024, 0.02), col, outline=outline, eps=0.0025, rings=20, segs=56)


def zeraora(src, out):
    k = Kit(src, coat="fur")
    add_head(k)
    add_body(k)
    add_arms(k)
    add_legs(k)
    add_tail(k)
    k.build(voxel=0.0062, smooth=4, band=0.006, wband=0.05)
    k.eye_states(lambda s: eyes(k, s))
    k.export(out)


if __name__ == "__main__":
    zeraora(*args())
