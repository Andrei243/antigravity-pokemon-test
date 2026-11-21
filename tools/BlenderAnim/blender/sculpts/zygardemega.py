# Mega Zygarde sculpted on the skeleton and clips of the game's own export of the form (sculptkit.py):
#   blender --background --factory-startup --python sculpts/zygardemega.py -- <the export's Zygarde-Mega.glb> <out.glb>
#
# A species of its own on the export's skeleton: the pictures show it grown from the Complete Forme (an upright green
# figure), which has no Blender sculpt, so nothing is grown from zygarde.py (the 50% Forme's serpent) but its colours.
# From the references, the masses by size: (1) a vast black cannon of a cylinder held over the figure's head, 0.4 of
# the height across and some 2.3 times as long, its rounded nose forward bearing a lavender four-armed star on its end
# with a blue crescent at one edge, two red zigzag bands round its middle, a salmon hexagon on its top near the back,
# five black claws splayed out of its back end; (2) the figure hanging under its rear third, 0.57 of the height: a small
# black head with a green crest, cyan eyes; a ruff of black spikes round the shoulders with a red hexagon in the middle
# of the chest and a blue and a green gem beside it; a green body glowing pale at the chest, long green arms and legs
# marked with black and white hexagons, a white blade on its right forearm, a black tail sweeping down behind its right
# leg; (3) four black legs like a spider's from under the cannon's back down to the figure's shoulders. The cannon is
# held along the model's length (nose to the front, a few degrees up) as the game's own sculpt holds it, which the game's
# front three-quarter camera sees as the art draws it. The tail is on the body bone (the skeleton has no tail bone).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, sheet, lathe, slab, hull, outward, srgb, inside, tri, unit, eye_look, polygon,
                       axes, args)

BLACK, BLACK2, GREEN, GREEN2, WHITE = "#3A322E", "#2A2422", "#8CD82C", "#A8EE3A", "#F2F6EE"
CANNON, RED, LAVENDER, BLUE, SALMON = "#3A3134", "#E8243C", "#C7A0EE", "#3E6EE0", "#F07060"
GLOWC, CYAN = "#E6FBC8", "#5AF0E8"

# --- the cannon: a lathe along AX from its back end C0 ---------------------------------------------------------------
C0 = np.array([0.0, 0.25, 0.81])
AX = unit(0, -1, 0.12)
LEN = 0.74
PROFILE = [(0, 0.0), (0.105, 0.0), (0.138, 0.03), (0.152, 0.09), (0.156, 0.3), (0.154, 0.46), (0.146, 0.55),
           (0.126, 0.62), (0.095, 0.675), (0.055, 0.715), (0, LEN)]
EA, EB = axes(AX)                                   # EA across (x), EB up


def radius(s):
    rs = np.array([p[0] for p in PROFILE]); ss = np.array([p[1] for p in PROFILE])
    return np.interp(s, ss, rs)


def on_cannon(s, phi, lift):
    """A point on the cannon's surface at s along it and phi round it (0 toward -x, 90 down: axes() of the axis), lift off it."""
    s = np.asarray(s, float); phi = np.asarray(phi, float)
    r = radius(s) + lift
    return (C0 + AX * s[..., None] + r[..., None] * (np.cos(phi)[..., None] * EA + np.sin(phi)[..., None] * EB))


def cannon(k):
    k.add("cannon", lathe(C0, AX, PROFILE, seg=56), CANNON, "body", fuse=False, surface="shell")
    # two red zigzag bands round its middle
    for s0, w, amp, n in ((0.42, 0.04, 0.022, 6), (0.2, 0.026, 0.014, 5)):
        def band(u, v, s0=s0, w=w, amp=amp, n=n):
            phi = 2 * math.pi * u - math.pi / 2
            s = s0 + w * (v - 0.5) + amp * tri(u * n)
            return on_cannon(s, phi, 0.006)
        k.add(f"redband{s0}", outward(sheet(band, 121, 4, 0.006)), RED, "body", fuse=False, surface="shell")
    # the lavender star on the nose's end: four arms from the tip out over the dome
    for j in range(4):
        ph0 = math.radians(45 + 90 * j)
        def arm(u, v, ph0=ph0):
            s = LEN - 0.006 - (LEN - 0.53) * u
            w = 0.04 * (1 - u) ** 0.8 + 0.008
            r = radius(s) + 0.004
            phi = ph0 + w * (v - 0.5) / np.maximum(r, 0.03)
            return on_cannon(s, phi, 0.005 + 0.002 * j)
        k.add(f"star{j}", outward(sheet(arm, 24, 4, 0.006)), LAVENDER, "body", fuse=False, surface="shell")
    # a blue crescent at the nose's edge, on the model's left
    def crescent(u, v):
        phi = math.radians(-40) + math.radians(80) * u
        s = 0.62 + 0.022 * (v - 0.5) * (0.2 + np.sin((0.03 + 0.94 * u) * math.pi)) * 2
        return on_cannon(s, phi, 0.005)
    k.add("bluecrescent", outward(sheet(crescent, 24, 4, 0.006)), BLUE, "body", fuse=False, surface="shell")
    # the salmon hexagon on its top near the back (EB, the frame's second axis, points down: phi -90 is the top)
    o = on_cannon(0.11, -math.pi / 2, 0.002)
    hexo = [(0.03 * math.cos(math.radians(60 * i)), 0.026 * math.sin(math.radians(60 * i))) for i in range(6)]
    k.add("hexeye", slab(hexo, o, AX, EA, 0.012), SALMON, "body", fuse=False, surface="shell")
    # five claws splayed out of its back end
    for j, ph in enumerate((90, 30, 150, -20, 200)):
        p = math.radians(ph)
        root = C0 + 0.07 * (math.cos(p) * EA + math.sin(p) * EB) + AX * 0.01
        out_ = math.cos(p) * EA + math.sin(p) * EB
        d = unit(*(-AX + out_ * 0.28))
        ln = 0.19 - 0.025 * (j > 2)
        a, b = axes(d)
        tip = root + d * ln
        bend = root + d * ln * 0.55 - out_ * 0.01
        k.add(f"claw{j}", hull([root + a * 0.03, root - a * 0.03, root + b * 0.022, root - b * 0.022,
                                bend + a * 0.017, bend - a * 0.017, bend + b * 0.013, bend - b * 0.013, tip]),
              BLACK2, "body", fuse=False, surface="shell")


def struts(k):
    """Four black legs like a spider's from under the cannon's back down to the figure's shoulders."""
    for s in (1, -1):
        for j, (top, knee, foot) in enumerate((((0.07, 0.1, 0.67), (0.19, 0.03, 0.63), (0.07, -0.05, 0.565)),
                                               ((0.06, 0.2, 0.675), (0.22, 0.15, 0.6), (0.05, -0.03, 0.52)))):
            m = np.array([s, 1, 1.0])
            P = [np.array(p) * m for p in (top, knee, foot)]
            k.add(f"strut{'R' if s > 0 else 'L'}{j}", outward(tube(P, [0.017, 0.016, 0.014], seg=12, per=6)),
                  BLACK, "body", fuse=False, surface="shell")
            k.add(f"strutknee{'R' if s > 0 else 'L'}{j}", outward(ell(P[1], (0.021, 0.021, 0.021), sub=2)),
                  BLACK, "body", fuse=False, surface="shell")


# --- the figure ------------------------------------------------------------------------------------------------------
HEAD_C, HEAD_R = np.array([0.0, -0.09, 0.628]), np.array([0.05, 0.052, 0.048])


def figure(k):
    # head and crest
    k.add("head", ell(HEAD_C, HEAD_R), BLACK, "head")
    k.add("jaw", ell(HEAD_C + (0, -0.012, -0.018), (0.028, 0.03, 0.02)), BLACK, "head")
    k.add("crest", hull([HEAD_C + p for p in ((0.016, -0.03, 0.02), (-0.016, -0.03, 0.02), (0.0, -0.045, 0.012),
                                              (0.0, 0.0, 0.035), (0.0, -0.05, 0.066), (0.011, -0.04, 0.045),
                                              (-0.011, -0.04, 0.045))]), GREEN, "head", fuse=False, surface="scales")
    for s in (1, -1):
        k.add("horn" + ("R" if s > 0 else "L"), hull([HEAD_C + p for p in
                                                       ((0.02 * s, -0.01, 0.025), (0.03 * s, 0.0, 0.015),
                                                        (0.025 * s, 0.01, 0.02), (0.065 * s, 0.005, 0.05))]),
              BLACK2, "head", fuse=False, surface="scales")
    # neck and body
    k.add("neck", ell((0, -0.08, 0.585), (0.024, 0.024, 0.03)), BLACK, "body")
    k.add("chest", ell((0, -0.076, 0.53), (0.068, 0.048, 0.056)), GREEN, "body")
    k.add("belly", ell((0, -0.072, 0.455), (0.05, 0.04, 0.062)), GREEN, "body")
    k.add("hips", ell((0, -0.07, 0.37), (0.06, 0.044, 0.045)), GREEN, "body")
    # the chest's pale glow
    k.paint(lambda P: inside(np.linalg.norm((P - (0, -0.115, 0.505)) / (0.03, 0.04, 0.045), axis=1) - 1, 0.01)
            * (P[:, 1] < -0.09), GLOWC)
    # arms: long green, down and a little forward, black at the elbow
    for s, sn in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        P = [np.array(p) * m for p in ((0.05, -0.078, 0.54), (0.085, -0.095, 0.47), (0.095, -0.115, 0.4))]
        k.add("arm" + sn, tube(P, [0.027, 0.024, 0.021], seg=16, per=6), GREEN, "arm" + sn)
        k.paint(lambda Q, e=P[1]: inside(np.linalg.norm(Q - e, axis=1) - 0.02, 0.002), BLACK, on=["arm" + sn])
        hand = P[2] + np.array((0, -0.006, -0.012))
        for j, dx in enumerate((-0.01, 0.0, 0.01)):
            tip = hand + np.array((dx * s, -0.012, -0.04 + 0.006 * abs(j - 1)))
            k.add(f"finger{sn}{j}", hull([hand + (0.008, 0, 0.0), hand - (0.008, 0, 0), hand + (0, 0.008, 0),
                                          hand - (0, 0.008, 0), tip]), BLACK, "arm" + sn, fuse=False,
                  surface="scales")
    # a white blade along the right forearm (the model's right, +X... the art's viewer-left arm is the figure's right)
    k.add("blade", hull([(0.087, -0.11, 0.47), (0.095, -0.1, 0.47), (0.1, -0.13, 0.36), (0.093, -0.14, 0.34),
                         (0.081, -0.125, 0.42)]), WHITE, "armR", fuse=False, surface="shell")
    # legs: long green, black at the knees, slim feet
    for s, sn in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        P = [np.array(p) * m for p in ((0.045, -0.07, 0.35), (0.062, -0.085, 0.24), (0.056, -0.07, 0.125))]
        k.add("leg" + sn, tube(P, [0.037, 0.031, 0.024], seg=16, per=6), GREEN, "leg" + sn)
        k.add("foot" + sn, ell(P[2] + np.array((0, -0.012, -0.008)), (0.019, 0.03, 0.016)), BLACK, "leg" + sn)
        k.paint(lambda Q, e=P[1]: inside(np.linalg.norm(Q - e, axis=1) - 0.024, 0.002), BLACK, on=["leg" + sn])
    # the black tail sweeping down behind the right leg, a green stripe along it
    tail = [(0.03, -0.04, 0.4), (0.07, -0.02, 0.33), (0.1, -0.01, 0.24), (0.12, -0.03, 0.16), (0.1, -0.06, 0.11),
            (0.075, -0.08, 0.115)]
    k.add("tail", tube(tail, [0.028, 0.028, 0.025, 0.021, 0.016, 0.012], seg=16, per=6), BLACK, "body")
    k.paint(lambda P: inside(np.abs(P[:, 0] - 0.115 + 0.4 * (P[:, 2] - 0.25)) - 0.006, 0.002)
            * (P[:, 0] > 0.05) * (P[:, 2] < 0.34) * (P[:, 2] > 0.15) * (P[:, 1] < -0.015), GREEN, on=["tail"])


def ruff(k):
    """The ruff of black spikes round the shoulders, the gems on the chest."""
    root = np.array([0, -0.072, 0.565])
    for i in range(12):
        t = math.radians(-100 + 200 * i / 11)
        d = np.array((math.sin(t), -0.55 * math.cos(t) - 0.15, 0.25 - 0.45 * (i % 2) - 0.2 * abs(math.sin(t))))
        d = unit(*d)
        r0 = root + np.array((0.04 * math.sin(t), -0.035 * math.cos(t), 0.0))
        a, b = axes(d)
        ln = 0.11 + 0.025 * (i % 3 == 0)
        k.add(f"ruff{i}", hull([r0 + a * 0.026, r0 - a * 0.026, r0 + b * 0.018, r0 - b * 0.018,
                                r0 + d * ln * 0.5 + a * 0.01, r0 + d * ln * 0.5 - a * 0.01, r0 + d * ln]),
              BLACK2, "body", fuse=False, surface="scales")
    k.add("ruffplate", ell((0, -0.085, 0.565), (0.06, 0.035, 0.022)), BLACK, "body")
    for x, col, rr in ((0.0, RED, 0.016), (0.034, GREEN2, 0.009), (-0.034, BLUE, 0.009)):
        hexo = [(rr * math.cos(math.radians(60 * i + 30)), rr * math.sin(math.radians(60 * i + 30))) for i in range(6)]
        o = np.array((x, -0.122 + 0.01 * abs(x) / 0.034, 0.567))
        k.add(f"gem{x}", slab(hexo, o, (1, 0, 0), (0, -0.25, 1), 0.012), col, "body", fuse=False, surface="shell")


def marks(k):
    """White and black hexagons on the legs and belly."""
    HEX = polygon([(math.cos(math.radians(60 * i)), math.sin(math.radians(60 * i))) for i in range(6)])
    for c, n, h, col in (((0.045, -0.11, 0.29), (0.3, -1, 0), 0.012, WHITE), ((-0.05, -0.11, 0.3), (-0.3, -1, 0), 0.012, WHITE),
                         ((0.048, -0.105, 0.18), (0.2, -1, 0), 0.011, BLACK), ((-0.047, -0.1, 0.18), (-0.2, -1, 0), 0.011, BLACK),
                         ((0.0, -0.105, 0.44), (0, -1, 0), 0.011, WHITE), ((0.0, -0.1, 0.405), (0, -1, 0), 0.011, WHITE)):
        k.decal(c, unit(*n), (h, h), col, outline=HEX, eps=0.002, rings=6, segs=48)


CW, CK, CC = srgb(WHITE), srgb("#141012"), srgb(CYAN)
EYE_N = unit(0.5, -1, 0.1)


def eye_at():
    d = unit(0.42, -0.9, 0.05)
    t = 1.0 / math.sqrt(((d / HEAD_R) ** 2).sum())
    return HEAD_C + d * t


def eye_open(u, v):
    c = np.tile(CC, (len(u), 1))
    c[((u + 0.05) / 0.4) ** 2 + (v / 0.45) ** 2 < 1] = CK
    c[((u - 0.2) / 0.22) ** 2 + ((v - 0.35) / 0.22) ** 2 < 1] = CW
    return c


def eyes(k, state):
    look = eye_look(state, eye_open, srgb(GREEN), CK, line=0.18)
    k.decal(eye_at(), EYE_N, (0.016, 0.014), look, eps=0.0018, rings=20, segs=56, mirror=True)


def face(k):
    # the green face mask round the eyes
    for sgn in (1, -1):
        m = np.array([sgn, 1, 1.0])
        k.decal(eye_at() * m, EYE_N * m, (0.026, 0.023), GREEN, eps=0.0012, rings=10, segs=48)


def zygardemega(src, dst):
    k = Kit(src, coat="scales")
    figure(k)
    ruff(k)
    marks(k)
    face(k)
    cannon(k)
    struts(k)
    k.build(voxel=0.0042, smooth=4, band=0.004, wband=0.04)
    k.eye_states(lambda state: eyes(k, state))
    k.export(dst)


if __name__ == "__main__":
    zygardemega(*args())
