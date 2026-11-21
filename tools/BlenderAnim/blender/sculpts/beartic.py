# Beartic sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown from
# Cubchoo's script (cubchoo.py, imported as sibling("cubchoo")): the same long snout with its big dark nose, the same
# round dark pads laid on paws and soles, the same pale blue for what is ice (the cub's drip, the bear's beard).
#   blender --background --factory-startup --python sculpts/beartic.py -- <the export's Beartic.glb> <out.glb>
#
# The design, from the references (they overrule the task's "blue face": the face is white, the beard alone is blue):
# a great white polar bear standing upright, a heavy pear of a body, broad hips; a small head with a long snout and a
# dark nose, small round ears, a small fierce dark eye under a scowling grey brow; the jaws open, pink inside, two
# white fangs; a beard of blue icicles hanging from the lower jaw onto the chest, its middle point the longest, white
# at the tips; thick arms (here forward and a little down) with big paws, a dark pad on each palm and four dark claws;
# a white star of shaggy fur on the belly; the lower body and the short thick legs pale blue-grey, a jagged hem of fur
# between, jagged fur at the ankles, broad feet.
# Reference ratios (3D picture): head without beard 0.25 of the height, head width / hip width 0.35, hips 0.52 of the
# height, legs showing 0.23 of the height, beard 0.17 of the height below the chin.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, polygon, srgb, inside, unit, boxy, eye_look, tri, args, sibling

c = sibling("cubchoo")
t = c.t
WHITE, LOWER, BROW, NOSE = "#E6ECF4", "#A9B9DB", "#7E8492", c.NOSE
ICE, ICETIP, MOUTH, TONGUE, FANG = "#7DBCEC", "#E8F5FD", "#B8485A", "#E88A98", "#FAFAF6"
PAD, CLAW, INK, FLUFF = c.PAD, "#383A44", "#20222A", "#F7F9FC"


def spike(k, name, root, d, length, width, thick, bone, color, side=None, surface="shell"):
    """A solid kite-sectioned spike from root along d: an icicle, a claw, a tuft of fur."""
    root, d = np.array(root, float), unit(*d)
    a = np.cross(d, (0, 0, 1.0)) if side is None else np.array(side, float)
    if np.linalg.norm(a) < 1e-3:
        a = np.array([1.0, 0, 0])
    a = a - d * (a @ d); a /= np.linalg.norm(a)
    b = np.cross(d, a)
    k.add(name, hull([root + a * width, root - a * width, root + b * thick, root - b * thick,
                      root + d * length * 0.4 + a * width * 0.75, root + d * length * 0.4 - a * width * 0.75,
                      root + d * length * 0.4 + b * thick * 0.7, root + d * length * 0.4 - b * thick * 0.7,
                      root + d * length]), color, bone, fuse=False, surface=surface)


def beartic(src, out):
    k = Kit(src, coat="fur")
    H = np.array([0, -0.06, 0.632])            # the head's middle (its bone is at 0, -0.03, 0.54)
    HR = np.array([0.08, 0.086, 0.074])
    B = np.array([0, 0.01, 0.39])              # the trunk's middle

    # The trunk: a heavy pear, broad hips, shoulders under a thick short neck
    def pear(V):
        V = V.copy()
        s = 1 + 0.12 * np.clip(-V[:, 2], 0, 1) - 0.18 * np.clip(V[:, 2], 0, 1)
        V[:, 0] *= s; V[:, 1] *= s
        return V
    k.add("trunk", ell(B, (0.2, 0.16, 0.235), deform=pear), WHITE, "body")
    k.add("chest", ell((0, -0.02, 0.525), (0.15, 0.13, 0.1)), WHITE, "body")
    k.add("neck", tube([(0, -0.01, 0.53), (0, -0.04, 0.6)], [0.11, 0.085]), WHITE, "head")
    k.add("tail", ell((0, 0.16, 0.25), (0.035, 0.03, 0.032)), WHITE, "body")

    # The head: small, a long snout forward, the jaws open
    HEAD = ell(H, HR, deform=boxy(2.3))
    k.add("head", HEAD, WHITE, "head")
    SNOUT = ell(H + (0, -0.105, -0.012), (0.046, 0.082, 0.04), rot=(-6, 0, 0))
    k.add("snout", SNOUT, WHITE, "head")
    NC = t.hit(SNOUT, H + (0, -1, -0.002), (0, 1, 0))
    k.add("nose", c.outward(ell(NC + (0, 0.006, 0.004), (0.022, 0.014, 0.015), rot=(-10, 0, 0))), NOSE, "head",
          fuse=False, surface="shell")
    k.add("jaw", ell(H + (0, -0.085, -0.074), (0.04, 0.066, 0.022), rot=(-14, 0, 0)), WHITE, "head")
    k.add("maw", ell(H + (0, -0.09, -0.048), (0.036, 0.062, 0.026), rot=(-8, 0, 0)), MOUTH, "head")
    k.add("tongue", ell(H + (0, -0.11, -0.058), (0.026, 0.035, 0.012), rot=(-12, 0, 0)), TONGUE, "head")
    k.paint(lambda P: inside(np.hypot(P[:, 0] / 0.035, (P[:, 2] - (H[2] - 0.05)) / 0.02) - 1, 0.002)
            * (P[:, 1] < H[1] - 0.04), MOUTH, on=["head", "snout", "jaw"])
    for s in (1, -1):
        spike(k, f"fang{'R' if s > 0 else 'L'}", H + (0.025 * s, -0.155, -0.03), (0, -0.2, -1), 0.026, 0.008, 0.007,
              "head", FANG)

    # Small round ears on the crown
    k.add("ear", ell((0.055, -0.03, 0.695), (0.026, 0.016, 0.024)), WHITE, "ear{s}", mirror=True)

    # The beard of icicles, from under the jaw onto the chest, its middle the longest
    root_y, root_z = H[1] - 0.04, H[2] - 0.09
    BEARD = [(0.0, 0.23, 0.032), (0.026, 0.18, 0.028), (-0.026, 0.185, 0.028), (0.048, 0.13, 0.026),
             (-0.048, 0.135, 0.026), (0.064, 0.09, 0.022), (-0.064, 0.092, 0.022), (0.012, 0.15, 0.024),
             (-0.014, 0.155, 0.024)]
    for i, (x, L, w) in enumerate(BEARD):
        y = root_y - 0.04 * math.cos(x / 0.08) + 0.012 * (i > 6)
        root = np.array([x, y, root_z + 0.02])
        d = (x * 1.2, -0.1, -1)
        spike(k, f"ice{i}", root, d, L, w * 1.45, w * 0.75, "head", ICE, side=(1, 0, 0))
        if L > 0.1:
            tip = root + unit(*d) * L * 0.62
            spike(k, f"icetip{i}", tip, d, L * 0.38 + 0.004, w * 0.8, w * 0.5, "head", ICETIP, side=(1, 0, 0))
    for s in (1, -1):          # up the cheeks to the corners of the jaws
        for j, (dz, L) in enumerate(((0.03, 0.07), (0.0, 0.1))):
            root = H + (0.064 * s, -0.09 + 0.02 * j, -0.06 + dz)
            spike(k, f"cheek{s}{j}", root, (0.35 * s, -0.25, -1), L, 0.03, 0.02, "head", ICE, side=(1, 0, 0))

    # Thick arms forward and a little down, big paws, a dark pad on each palm and four dark claws
    for s, bone in ((1, "armR"), (-1, "armL")):
        m = np.array([s, 1, 1.0])
        k.add("arm" + bone[-1], tube([np.array(p) * m for p in [(0.13, 0.0, 0.52), (0.22, -0.05, 0.47),
                                                                 (0.27, -0.14, 0.44)]], [0.068, 0.058, 0.05]),
              WHITE, bone)
        PAW = ell(np.array((0.245, -0.17, 0.44)) * m, (0.058, 0.045, 0.062))
        k.add("paw" + bone[-1], PAW, WHITE, bone)
        pc = t.hit(PAW, np.array((0.245, -1, 0.432)) * m, (0, 1, 0))
        k.decal(pc, unit(0.1 * s, -1, 0), (0.034, 0.03), PAD, eps=0.004, rings=10, segs=44)
        for i, dx in enumerate((-0.033, -0.011, 0.011, 0.033)):
            base = np.array((0.245 + dx, -0.188, 0.488 - 0.012 * abs(dx) / 0.033)) * m
            spike(k, f"claw{bone[-1]}{i}", base, (0.25 * s * dx / 0.033, -0.75, 0.6), 0.05, 0.012, 0.009, bone, CLAW)

    # The belly's star of shaggy fur
    BC = np.array([0, -0.168, 0.33])
    k.add("fluff", ell(BC + (0, 0.02, 0), (0.06, 0.03, 0.06)), FLUFF, "body", fuse=False, surface="fur")
    for i, a in enumerate(np.radians(np.arange(0, 360, 36) + 8)):
        o = np.array([math.cos(a), -0.12, math.sin(a)])
        spike(k, f"tuft{i}", BC + (0, 0.012, 0) + o * np.array([0.035, 0.06, 0.035]), o, 0.048 + 0.012 * (i % 2),
              0.026, 0.011, "body", FLUFF, side=np.cross(o, (0, 1.0, 0)), surface="fur")

    # Short thick legs, the thighs fused into the hips; broad feet with three small claws
    for s, bone in ((1, "legR"), (-1, "legL")):
        m = np.array([s, 1, 1.0])
        k.add("thigh" + bone[-1], ell(np.array((0.115, 0.0, 0.2)) * m, (0.1, 0.11, 0.12)), WHITE, bone)
        k.add("shin" + bone[-1], tube([np.array(p) * m for p in [(0.12, -0.005, 0.15), (0.125, -0.015, 0.06)]],
                                      [0.075, 0.07]), LOWER, bone)
        k.add("foot" + bone[-1], ell(np.array((0.127, -0.05, 0.028)) * m, (0.074, 0.098, 0.03)), LOWER, bone)
        for i, dx in enumerate((-0.03, 0.0, 0.03)):
            spike(k, f"toe{bone[-1]}{i}", np.array((0.127 + dx, -0.135, 0.028)) * m, (0.2 * s * np.sign(dx), -1, 0),
                  0.03, 0.01, 0.008, bone, CLAW)
        # jagged fur at the ankle
        for i, a in enumerate(np.radians(np.arange(-150, 160, 40))):
            o = np.array([math.sin(a), -math.cos(a), 0])
            spike(k, f"ankle{bone[-1]}{i}", np.array((0.125, -0.01, 0.085)) * m + o * 0.06, o + (0, 0, -0.9),
                  0.04, 0.016, 0.009, bone, LOWER, surface="fur")

    # The lower body pale blue-grey under a jagged hem of fur
    def lower(P):
        ang = np.arctan2(P[:, 0], -(P[:, 1] - B[1]))
        return inside(P[:, 2] - (0.255 + 0.024 * tri(ang * 3.2)), 0.003)
    k.paint(lower, LOWER)

    # The scowling grey brow over each eye, sloping down to the snout
    de = unit(0.6, -0.78, 0.15)
    E = t.hit(HEAD, (0.042, -1, H[2] + 0.018), (0, 1, 0))
    for s in (1, -1):
        mm = np.array([s, 1, 1.0])
        k.decal(E * mm + (0, 0, 0.016), de * mm, (0.024, 0.006), BROW, eps=0.0035, rings=6, segs=40,
                outline=polygon([(s * u, v) for u, v in [(-1, -1.4), (1, 1.2), (0.9, 1.9), (-1, 0.0)]]))

    # Small fierce dark eyes: the top line slopes down to the nose
    CW, CK = srgb(WHITE), srgb(INK)
    OUT = [(-1.0, -0.3), (-0.6, 0.1), (0.3, 0.55), (1.0, 0.5), (0.9, -0.35), (0.1, -0.7), (-0.6, -0.6)]
    open_eye = t.bear_eye(CK, at=(0.25, 0.15), size=0.27)
    def fierce(u, v):
        col = open_eye(u, v)
        col[v > 0.15 + 0.45 * u] = CW
        return col
    def lay(state):
        look = fierce if state == "Fierce" else eye_look(state, open_eye, CW, CK, line=0.22)
        for s in (1, -1):
            shape = polygon([(u * s, v) for u, v in OUT])
            col = look if s > 0 else (lambda u, v, f=look: f(-u, v))
            k.decal(E * np.array([s, 1, 1]), de * np.array([s, 1, 1]), (0.019, 0.016), col, eps=0.004,
                    outline=shape, rings=20, segs=56)

    k.build(voxel=0.0068, band=0.005, wband=0.05)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    beartic(*args())
