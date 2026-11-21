# Chesnaught sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown from
# Chespin's and Quilladin's scripts (chespin.py, quilladin.py, imported: the line's colours, the quill and the way a
# face is painted into the green):
#   blender --background --factory-startup --python sculpts/chesnaught.py -- <the export's Chesnaught.glb> <out.glb>
# From the references: a great armoured knight. Its largest mass is the shell: a dome over its back and shoulders, cream
# on top with two great cream horns up and out from its top, green below and round the front, where a green band
# framed by a red line runs round the face; the shell's lining dark brown, seen under the shoulders. A white face sunk
# in the shell's front, ringed by a jagged white mane; small black fierce eyes; a red triangle mark with a pale middle
# over them; a small frowning mouth. A green V of the shell's
# collar over the chest and a jagged white tuft of fur hanging on it. A cream body with brown segment lines; thick cream
# legs set apart, two brown lines round each, three brown claws on each foot. Long heavy arms carried forward and down:
# white below, a green sleeve along their top and outer side with small green spikes on its edge, three great brown
# claws at each hand. A thin green tail behind.
# (The pictures overrule the task's text: there is no "white beard of fur" over the chin alone: the white is a mane
# round the whole face and a tuft on the chest; the arms are white with a green sleeve, not plated.)
# Reference ratios (3D picture, feet to the horns' tips): shell width / height 0.61; horns above the shell's top /
# height 0.15; mane width / height 0.24; arm thickness / height 0.15; legs showing / height 0.27.
# The export's joints: body (0, 0, 0.36), arms (+-0.15, -0.02, 0.46), head (0, -0.06, 0.5), legs (+-0.08, 0, 0.2).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, args, outward, axes, polygon, hull, sibling

cp = sibling("chespin")
INK, WHITE = cp.INK, cp.WHITE
GREEN = "#86C24C"
CREAM = "#E8DDB4"
RED = "#D23A3A"
LINING = "#5C3A26"
CLAW = "#7A4A2C"
LINE = "#9C7A52"
FUR = "#F4F2EC"
MOUTH = "#5A3A34"

# --- the frame ----------------------------------------------------------------------------------------------------------
SHELL = np.array([0.0, 0.055, 0.5])
SHELLR = np.array([0.24, 0.18, 0.205])
HEAD = np.array([0.0, -0.105, 0.552])
HEADR = np.array([0.08, 0.066, 0.075])
FACE_C = np.array([0.0, 0.552])              # the face's middle seen from the front (x, z)
EYE_X, EYE_Z, EYE_HALF = 0.03, 0.566, (0.0155, 0.0125)
SH = np.array([0.152, -0.03, 0.462])
EL = np.array([0.215, -0.075, 0.385])
WR = np.array([0.235, -0.13, 0.295])
HAND = np.array([0.24, -0.15, 0.258])
OUTER = unit(0.78, 0.1, 0.62)                # the arm's sleeve side


def cone(base, d, length, r0, r1=0.006):
    d = unit(*d); base = np.array(base, float)
    return tube([base - d * r0 * 0.5, base + d * length * 0.5, base + d * length], [r0, (r0 + r1) * 0.5, r1], seg=24, per=6)


def spike(base, d, length, w):
    d = unit(*d); a, b = axes(d); base = np.array(base, float)
    return hull([base + a * w, base - a * w, base + b * w, base - b * w, base - d * w * 0.8, base + d * length])


def add_shell(k):
    k.add("shell", ell(SHELL, SHELLR), CREAM, "body", surface="shell")
    for side, s in (("R", 1), ("L", -1)):
        k.add("horn" + side, cone((0.12 * s, 0.07, 0.62), (0.52 * s, 0.12, 0.85), 0.17, 0.052, 0.007), CREAM, "body",
              surface="shell")
        k.add("knob" + side, cone((0.175 * s, -0.07, 0.47), (0.35 * s, -0.25, 1), 0.06, 0.026, 0.006), CREAM, "body",
              surface="shell")
    def ring(P):
        return np.hypot(P[:, 0] / 0.105, (P[:, 2] - FACE_C[1]) / 0.098)
    front = lambda P: inside(P[:, 1] + 0.025, 0.003)
    on = ["shell"]
    # green: the lower rim all round and a band round the face in front
    k.paint(lambda P: np.maximum(inside(P[:, 2] - 0.405, 0.002),
                                 front(P) * inside(ring(P) - 1.85, 0.02) * inside(P[:, 2] - 0.645, 0.002)), GREEN, on=on)
    # the red line inside the band, round the face's top and sides
    k.paint(lambda P: front(P) * inside(np.abs(ring(P) - 1.32) - 0.075, 0.02) * (P[:, 2] > 0.47) * inside(P[:, 2] - 0.625, 0.002), RED, on=on)
    k.paint(lambda P: front(P) * inside(ring(P) - 1.14, 0.02) * (P[:, 2] > 0.47), CREAM, on=on)
    # the dark lining seen under the shoulders
    k.paint(lambda P: inside(P[:, 2] - 0.335, 0.002) * (np.abs(P[:, 0]) > 0.12), LINING, on=on)
    # the knobs' red rim
    k.paint(lambda P: inside(np.abs(P[:, 2] - 0.478) - 0.006, 0.002) * (np.abs(P[:, 0]) > 0.15) * (P[:, 1] < -0.02),
            RED, on=["knob"])


def add_head(k):
    k.add("head", ell(HEAD, HEADR), FUR, "head")
    # the jagged white mane round the face
    for i in range(18):
        a = 2 * math.pi * i / 18
        rx, rz = math.sin(a), math.cos(a)
        base = np.array([0.074 * rx, -0.128, FACE_C[1] + 0.07 * rz])
        ln = 0.048 if rz > -0.3 else 0.06
        k.add(f"mane{i}", spike(base, (rx, -0.55, rz), ln, 0.02), FUR, "head", fuse=False, surface="fur")


def add_face(k):
    # the red triangle with its pale middle over the eyes
    p, n = cp.on_ell(0.0, 0.597, HEAD, HEADR)
    k.decal(p, unit(0, n[1], n[2] * 0.6), (0.024, 0.02), RED, eps=0.003, rings=10, segs=48,
            outline=polygon([(-1, 0.9), (1, 0.9), (0, -1)]))
    k.decal(p + n * 0.001 + np.array([0, 0, 0.004]), unit(0, n[1], n[2] * 0.6), (0.01, 0.009), CREAM, eps=0.0042,
            rings=6, segs=36, outline=polygon([(-1, 0.9), (1, 0.9), (0, -1)]))
    # a small frown
    p, n = cp.on_ell(0.0, 0.52, HEAD, HEADR)
    M_, F_ = srgb(MOUTH), srgb(FUR)
    def frown(u, v):
        c = np.tile(F_, (len(u), 1))
        c[np.abs(v - (0.5 - 0.9 * u * u)) < 0.45] = M_
        return c
    k.decal(p, unit(0, n[1], n[2] * 0.4), (0.022, 0.008), frown, eps=0.003, rings=8, segs=48)


def add_body(k):
    k.add("torso", ell((0, 0.0, 0.345), (0.13, 0.11, 0.15)), CREAM, "body")
    k.add("hips", ell((0, 0.01, 0.235), (0.135, 0.1, 0.075)), CREAM, "body")
    # the collar's green V over the chest
    k.paint(lambda P: inside(0.43 - 0.08 * np.clip(1 - np.abs(P[:, 0]) / 0.13, 0, 1) - P[:, 2], 0.002)
            * inside(P[:, 1] + 0.02, 0.003), GREEN, on=["torso"])
    # brown segment lines round the belly
    for z in (0.215, 0.265):
        k.paint(lambda P, z=z: inside(np.abs(P[:, 2] - z) - 0.0035, 0.0015) * (np.abs(P[:, 0]) < 0.12), LINE,
                on=["torso", "hips"])
    # the white tuft hanging on the chest
    rs = np.random.RandomState(3)
    for i in range(9):
        x = (i - 4) * 0.014
        base = np.array([x, -0.115 + abs(x) * 0.25, 0.44 - abs(x) * 0.3])
        d = (x * 3 + rs.uniform(-0.2, 0.2), -0.35, -1.0)
        k.add(f"chestfur{i}", spike(base, d, 0.07 - abs(x) * 0.4, 0.02), FUR, "body", fuse=False, surface="fur")


def add_legs(k):
    k.add("haunch", ell((0.085, 0.0, 0.2), (0.075, 0.075, 0.07)), CREAM, "leg{s}", mirror=True)
    k.add("leg", tube([(0.095, -0.005, 0.18), (0.125, -0.015, 0.1), (0.135, -0.02, 0.045)], [0.062, 0.057, 0.055]),
          CREAM, "leg{s}", mirror=True)
    k.add("foot", ell((0.138, -0.045, 0.028), (0.065, 0.08, 0.03)), CREAM, "leg{s}", mirror=True)
    for z in (0.085, 0.135):
        k.paint(lambda P, z=z: inside(np.abs(P[:, 2] - z) - 0.0035, 0.0015) * (np.abs(P[:, 0]) > 0.06), LINE,
                on=["leg", "haunch"])
    for i, dx in enumerate((-0.035, 0.0, 0.035)):
        b = np.array((0.138 + dx, -0.115, 0.022))
        k.add(f"toe{i}", hull([b + (0.015, 0.03, -0.018), b + (-0.015, 0.03, -0.018), b + (0.013, 0.03, 0.022),
                               b + (-0.013, 0.03, 0.022), b + (0, -0.03, -0.014)]), CLAW, "leg{s}", fuse=False,
              mirror=True, surface="shell")


def add_arms(k):
    k.add("upper", tube([SH, EL], [0.054, 0.056]), WHITE, "arm{s}", mirror=True)
    k.add("fore", tube([EL, WR], [0.058, 0.058]), WHITE, "arm{s}", mirror=True)
    k.add("hand", ell(HAND, (0.058, 0.056, 0.052)), WHITE, "arm{s}", mirror=True)
    # the green sleeve along the arm's top and outer side
    def sleeve(P):
        Q = P.copy(); Q[:, 0] = np.abs(Q[:, 0])
        best = np.full(len(Q), 9.0); side = np.zeros(len(Q))
        for a, b in ((SH, EL), (EL, WR)):
            ab = b - a
            t = np.clip(((Q - a) @ ab) / (ab @ ab), 0, 1)
            w = Q - (a + t[:, None] * ab)
            dd = np.linalg.norm(w, axis=1)
            pick = dd < best
            best[pick] = dd[pick]; side[pick] = (w @ OUTER)[pick]
        return inside(-side + 0.012, 0.002) * (Q[:, 2] > 0.28)
    k.paint(sleeve, GREEN, on=["upper", "fore"])
    # a line across the forearm's white
    du = unit(*(WR - EL))
    k.paint(lambda P: inside(np.abs((np.c_[np.abs(P[:, 0]), P[:, 1:]] - (EL + (WR - EL) * 0.45)) @ du) - 0.003, 0.0015)
            * (np.abs(P[:, 0]) > 0.18), LINE, on=["fore"])
    # small green spikes on the sleeve's edge
    for i, t in enumerate((0.2, 0.5, 0.8)):
        p = EL + (WR - EL) * t + OUTER * 0.05 if i else SH + (EL - SH) * 0.6 + OUTER * 0.048
        k.add(f"sleevespike{i}", spike(p, OUTER + np.array([0, 0, 0.3]), 0.032, 0.014), GREEN, "arm{s}", fuse=False,
              mirror=True, surface="shell")
    # three great brown claws at the hand, curving down and forward
    a, b = axes(unit(0, -1, -0.6))
    for i, dx in enumerate((-0.032, 0.0, 0.032)):
        q = HAND + np.array((dx, -0.035, -0.035))
        k.add(f"claw{i}", hull([q + (0.016, 0.012, 0.016), q + (-0.016, 0.012, 0.016), q + (0.016, -0.014, 0.012),
                                q + (-0.016, -0.014, 0.012), q + (0, 0.01, -0.01), q + (0.0, -0.045, -0.035),
                                q + (0.0, -0.02, -0.075)]), CLAW, "arm{s}", fuse=False, mirror=True, surface="shell")


def add_tail(k):
    pts = [(0, 0.14, 0.24), (0.04, 0.2, 0.16), (0.09, 0.24, 0.1), (0.13, 0.27, 0.07)]
    k.add("tail", tube(pts, [0.024, 0.02, 0.014, 0.006]), GREEN, "body")


def fierce_eye():
    """A small black eye with a glint, its top cut by the white brow, lower toward the nose."""
    I_, W_ = srgb(INK), srgb(WHITE)
    def look(u, v):
        c = np.tile(I_, (len(u), 1))
        c[((u - 0.15) / 0.24) ** 2 + ((v - 0.05) / 0.3) ** 2 < 1] = W_
        return c
    return look


def lay_eyes(k, state):
    F_, I_ = srgb(FUR), srgb(INK)
    def cut(look):
        def f(u, v):
            c = look(u, v)
            c[v > 0.5 + 0.45 * u] = F_                    # the brow, lower toward the nose
            return c
        return f
    if state == "Fierce":
        def look(u, v):
            c = fierce_eye()(u, v)
            c[v > 0.1 + 0.8 * u] = F_
            return c
    else:
        look = cut(eye_look(state, fierce_eye(), F_, I_, line=0.2))
    p, n = cp.on_ell(EYE_X, EYE_Z, HEAD, HEADR)
    k.decal(p, unit(n[0] * 0.8, n[1], n[2] * 0.6), EYE_HALF, look, eps=0.003, rings=20, segs=56, mirror=True)


def chesnaught(src, out):
    k = Kit(src, coat="shell")
    add_shell(k)
    add_head(k)
    add_face(k)
    add_body(k)
    add_legs(k)
    add_arms(k)
    add_tail(k)
    k.build(voxel=0.0065, smooth=4, band=0.006, wband=0.05)
    k.eye_states(lambda s: lay_eyes(k, s))
    k.export(out)


if __name__ == "__main__":
    chesnaught(*args())
