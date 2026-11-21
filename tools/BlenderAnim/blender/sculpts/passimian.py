# Passimian sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/passimian.py -- <the export's Passimian.glb> <out.glb>
#
# The design, from the references (the pictures overrule the task's text where they differ): a black-and-white lemur
# ape. The skeleton is a biped's, so the all-fours pose of the pictures is carried as a crouch leaning forward. The
# back, the shoulders, the arms and the big thighs are white shaggy fur; the chest, the belly, the shins and the feet,
# the hands and the face are charcoal; three green leaf marks on the chest. On the head a pale lime fruit shell worn as a
# helmet, coming down to the brow, an orange forked mark on its front and two green leaves standing out of its top;
# white spiky fur round the sides of the head under the shell's rim; stern yellow eyes; a dark lemur's muzzle with a
# black nose. A long thick charcoal tail curling up behind, a fluffy white band near its dark tip. It holds a pale lime
# berry like a ball in its +X hand, in front of its belly. Height 0.71 as the export; a single-stage line.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, lathe, leaf, outward, srgb, inside, unit, boxy, tri, eye_look, polygon, \
    args, sibling

ted = sibling("teddiursa")
hit = ted.hit

WHITE, DARK, BLACK = "#EEEEEA", "#43454C", "#1E1F24"
LIME, LIME_D, ORANGE, GREEN = "#D6E794", "#B9D06A", "#F07A2A", "#2E9E5A"
BALL, BALL_D, IRIS, INK = "#C9DC7A", "#A9C25A", "#F4C21E", "#141418"

HC = np.array([0.0, -0.15, 0.575])           # the skull's middle (the head's bone is at 0, -0.06, 0.49)
HR = np.array([0.09, 0.085, 0.085])
CHEST = np.array([0.0, -0.065, 0.42])
BELLY = np.array([0.0, 0.015, 0.32])


def body(k):
    """A torso leaning forward, the chest out over the belly, its front charcoal and its back white."""
    k.add("chest", ell(CHEST, (0.108, 0.098, 0.105)), WHITE, "body")
    k.add("belly", ell(BELLY, (0.1, 0.1, 0.1)), WHITE, "body")
    k.add("neck", ell((0, -0.105, 0.49), (0.065, 0.06, 0.06)), WHITE, "body")
    # The charcoal front: everything forward of a slanting line, its edge jagged like the fur
    def front(P):
        cut = -0.01 + 0.25 * (P[:, 2] - 0.4) - 0.6 * np.abs(P[:, 0]) ** 2 + 0.012 * tri(P[:, 0] * 18 + P[:, 2] * 6)
        return inside(P[:, 1] - cut, 0.003)
    k.paint(front, DARK, on=["chest", "belly", "neck"])


def legs(k):
    """Crouched legs: big white haunches fused into the hips reaching nearly to the ankle, short thick charcoal shins,
    long charcoal feet with toes."""
    for s, bone in ((1, "legR"), (-1, "legL")):
        m = np.array([s, 1, 1.0])
        k.add("haunch" + bone[-1], ell(np.array((0.075, 0.01, 0.22)) * m, (0.085, 0.1, 0.1)), WHITE, bone)
        k.add("thigh" + bone[-1], tube([np.array(p) * m for p in [(0.06, 0.01, 0.27), (0.09, -0.04, 0.18),
                                                                   (0.1, -0.08, 0.12)]], [0.075, 0.07, 0.05]),
              WHITE, bone)
        k.add("shin" + bone[-1], tube([np.array(p) * m for p in [(0.1, -0.075, 0.11), (0.095, 0.0, 0.05)]],
                                      [0.045, 0.038]), DARK, bone)
        F = np.array((0.098, -0.035, 0.024)) * m
        k.add("foot" + bone[-1], ell(F, (0.042, 0.07, 0.024)), DARK, bone)
        for i, dx in enumerate((-0.022, 0.0, 0.022)):
            p0 = F + np.array([dx * s, -0.05, 0.0])
            k.add(f"toe{bone[-1]}{i}", tube([p0, p0 + np.array([dx * s * 0.4, -0.035, -0.008])], [0.014, 0.012]),
                  DARK, bone)
        # Crisp edge where the white haunch meets the charcoal shin
        K = np.array((0.1, -0.07, 0.1)) * m
        def edge(P, K=K):
            return inside(P[:, 2] - K[2], 0.003) * inside(np.linalg.norm(P[:, :2] - K[:2], axis=1) - 0.08, 0.003)
        k.paint(edge, DARK, on=["thigh" + bone[-1], "shin" + bone[-1], "haunch" + bone[-1]])


def hand(k, bone, W, d, s, ball=None):
    """A charcoal hand with long fingers; over a ball, the fingers curl down round it."""
    d = unit(*d)
    P = W + d * 0.03
    k.add("palm" + bone[-1], ell(P, (0.034, 0.03, 0.026)), DARK, bone)
    for i, ang in enumerate((-35, -12, 12, 35)):
        side = np.array([math.sin(math.radians(ang)) * s, -math.cos(math.radians(ang)) * 0.4, 0])
        if ball is not None:
            C, r = ball
            o = unit(*(P + side * 0.06 - C))
            p1 = C + unit(*(o + np.array([0, 0, -0.3]))) * (r + 0.012)
            p2 = C + unit(*(o + np.array([0, 0, -1.0]))) * (r + 0.01)
            pts = [P + side * 0.025, p1, p2]
        else:
            pts = [P + side * 0.025, P + side * 0.04 + d * 0.035, P + side * 0.045 + d * 0.06]
        k.add(f"finger{bone[-1]}{i}", tube(pts, [0.013, 0.012, 0.011]), DARK, bone)


def arms(k):
    """White shaggy arms carried forward and down, a jagged white cuff over each charcoal hand."""
    BALL_C, BALL_R = np.array((0.075, -0.25, 0.215)), 0.058
    for s, bone in ((1, "armR"), (-1, "armL")):
        m = np.array([s, 1, 1.0])
        S = np.array((0.095, -0.05, 0.46)) * m
        E = np.array((0.135, -0.13, 0.36)) * m
        W = (np.array((0.085, -0.225, 0.3)) if s > 0 else np.array((0.105, -0.185, 0.255))) * m
        k.add("shoulder" + bone[-1], ell(S + np.array([0.01 * s, 0.01, 0]), (0.06, 0.065, 0.06)), WHITE, bone)
        k.add("upper" + bone[-1], tube([S, (S + E) / 2 + np.array([0.012 * s, 0, 0]), E], [0.052, 0.048, 0.044]),
              WHITE, bone)
        k.add("fore" + bone[-1], tube([E, W], [0.044, 0.036]), WHITE, bone)
        # A jagged cuff of white fur over the wrist
        d = unit(*(W - E))
        a = unit(*np.cross(d, (0, 0, 1.0)))
        b = np.cross(d, a)
        for i in range(7):
            th = 2 * math.pi * (i + 0.5) / 7
            o = math.cos(th) * a + math.sin(th) * b
            base = W - d * 0.03 + o * 0.03
            k.add(f"cuff{bone[-1]}{i}", tube([base, base + d * 0.035 + o * 0.016], [0.018, 0.006]), WHITE, bone)
        if s > 0:
            hand(k, bone, W, (0, -0.4, -1), s, ball=(BALL_C, BALL_R))
            k.add("ball", outward(ell(BALL_C, (BALL_R,) * 3, sub=4)), BALL, bone, fuse=False, surface="shell")
            for j, rot in enumerate((0, 60, 120)):
                # A berry's shallow seams: thin darker bands round the ball
                k.add(f"seam{j}", outward(ell(BALL_C, (BALL_R * 1.012, BALL_R * 1.012, 0.004), rot=(rot, 30, 0),
                                              sub=3)), BALL_D, bone, fuse=False, surface="shell")
        else:
            hand(k, bone, W, (0.2, -0.5, -1), s)


def tail(k):
    """A long thick charcoal tail curling up behind, run back and some 35 degrees out to +X, a fluffy white band near
    its dark tip."""
    T = [np.array(p) for p in [(0.0, 0.06, 0.3), (0.05, 0.17, 0.27), (0.12, 0.29, 0.28), (0.17, 0.4, 0.34),
                               (0.19, 0.46, 0.42), (0.18, 0.44, 0.48), (0.15, 0.4, 0.5)]]
    k.add("tail", tube(T[:5], [0.04, 0.038, 0.036, 0.035, 0.035]), DARK, "tail")
    k.add("tailband", tube([T[4], T[5]], [0.046, 0.046]), WHITE, "tail")
    k.add("tailtip", tube([T[5], T[6]], [0.04, 0.03]), DARK, "tail")
    # The band's edges jagged like tufts
    for P0, P1, col in ((T[4], T[5], WHITE),):
        d = unit(*(P1 - P0))
        a = unit(*np.cross(d, (1.0, 0, 0)))
        b = np.cross(d, a)
        for i in range(8):
            th = 2 * math.pi * i / 8
            o = math.cos(th) * a + math.sin(th) * b
            base = P0 + o * 0.036
            k.add(f"tailtuft{i}", tube([base + d * 0.01, base - d * 0.03 + o * 0.006], [0.016, 0.006]), WHITE, "tail")


def head(k):
    HEAD = ell(HC, HR, deform=boxy(2.2))
    k.add("head", HEAD, DARK, "head")
    # The muzzle: a lemur's snout forward and a little down, its tip black
    MZ = np.array((0, -0.228, 0.545))
    k.add("muzzle", ell(MZ, (0.041, 0.056, 0.038), rot=(-12, 0, 0)), DARK, "head")
    k.add("nose", ell(MZ + np.array([0, -0.052, 0.007]), (0.025, 0.016, 0.018)), BLACK, "head")
    # White fur at the back and sides of the head, spiky round the cheeks under the shell's rim
    k.paint(lambda P: inside(-(P[:, 1] - (HC[1] + 0.01)), 0.003) * (P[:, 2] > HC[2] - 0.07), WHITE, on=["head"])
    for s in (1, -1):
        for j, (dz, dy, L) in enumerate([(0.02, 0.0, 0.055), (-0.01, -0.01, 0.06), (-0.04, 0.0, 0.05),
                                        (0.0, 0.03, 0.05)]):
            base = HC + np.array([0.07 * s, dy - 0.005, dz])
            d = unit(0.9 * s, 0.35, dz * 3)
            k.add(f"ruff{'R' if s > 0 else 'L'}{j}", tube([base, base + d * L * 0.55, base + d * L],
                                                           [0.024, 0.016, 0.005]), WHITE, "head")
    # The chest's green leaf marks
    def leafmark(c, n, half, up):
        k.decal(c, n, half, srgb(GREEN), up=up, eps=0.003, rings=10, segs=40,
                outline=polygon([(-1, 0), (-0.4, 0.5), (0.4, 0.45), (1, 0), (0.4, -0.45), (-0.4, -0.5)]))
    leafmark((0.055, -0.15, 0.44), unit(0.45, -0.85, 0.2), (0.032, 0.014), (0.3, 0, 1))
    leafmark((-0.06, -0.145, 0.43), unit(-0.45, -0.85, 0.2), (0.026, 0.012), (-0.5, 0, 1))
    leafmark((-0.05, -0.14, 0.38), unit(-0.4, -0.9, 0.0), (0.024, 0.011), (0.6, 0, 1))
    return HEAD


def shell(k):
    """The fruit shell worn as a helmet: a loose dome tilted back, its rim just over the brow in front and low
    behind, an orange fork on its front and two green leaves out of its top."""
    d = unit(0, 0.33, 1)
    C = HC + np.array([0, 0.0, -0.008])
    R, Hh = 0.107, 0.114
    prof = [(0, Hh)]
    for a in np.linspace(10, 90, 9):
        t = math.radians(a)
        prof.append((R * math.sin(t), Hh * math.cos(t)))
    prof += [(R * 0.92, -0.008), (0, -0.008)]
    k.add("shell", lathe(C, d, prof, seg=48), LIME, "head", fuse=False, surface="shell")
    # The orange fork: three short strokes on the shell's front
    a_, b_ = np.cross(d, (1.0, 0, 0)), None
    up = d
    fwd = unit(*np.cross((1.0, 0, 0), d))        # square to d, pointing to the front (-Y)
    def on_shell(ang_x, h, lift):
        t = math.acos(np.clip(h / Hh, -1, 1))
        r = R * math.sin(t)
        o = fwd * math.cos(ang_x) + np.array([1.0, 0, 0]) * math.sin(ang_x)
        return C + up * h + o * (r + lift)
    for name, x0, x1, h0, h1 in (("forkR", 0.0, 0.32, 0.012, 0.05), ("forkL", 0.0, -0.32, 0.012, 0.05),
                                  ("forkM", 0.0, 0.0, 0.012, 0.042)):
        pts = []
        for t in np.linspace(0, 1, 4):
            ax, h = x0 + (x1 - x0) * t, h0 + (h1 - h0) * t
            side = np.array([math.cos(ax), 0, -math.sin(ax) * 0]) * 0.0
            for lift in (-0.002, 0.004):
                for w in (-0.0065, 0.0065):
                    p = on_shell(ax + w / R, h, lift)
                    pts.append(p)
        k.add(name, hull(pts), ORANGE, "head", fuse=False, surface="shell")
    # Two green leaves standing out of its top, leaning back in a V
    top = C + up * Hh * 0.95
    for s in (1, -1):
        k.add("sprout" + ("R" if s > 0 else "L"), outward(leaf(top + np.array([0.004 * s, 0.006, -0.01]),
                                                               unit(0.32 * s, 0.35, 1), 0.1, 0.022, thick=0.02)),
              GREEN, "head", fuse=False, surface="leaf")


def passimian(src, out):
    k = Kit(src, coat="fur")
    body(k)
    legs(k)
    arms(k)
    tail(k)
    HEAD = head(k)
    shell(k)

    # Stern yellow eyes just under the shell's rim: a black pupil, the top line sloping down to the nose
    OUT = [(-1.0, -0.15), (-0.55, -0.8), (0.3, -0.92), (0.88, -0.45), (1.0, 0.25), (0.4, 0.55), (-0.5, 0.25)]
    def open_look(u, v):
        c = np.tile(srgb(INK), (len(u), 1))
        c[(u / 0.8) ** 2 + ((v + 0.12) / 0.62) ** 2 < 1] = srgb(IRIS)
        c[(u + 0.12) ** 2 + ((v + 0.15) / 1.2) ** 2 < 0.28 ** 2] = srgb(INK)
        return c
    de = unit(0.42, -0.9, 0.05)
    E = hit(HEAD, (0.037, -1, HC[2] - 0.002), (0, 1, 0))
    def lay(state):
        ink = srgb(INK) if state in ("Open", "Fierce") else srgb("#C8CAD0")
        look = eye_look(state, open_look, srgb(DARK), ink, line=0.2)
        for s in (1, -1):
            out_ = polygon(OUT if s > 0 else [(-a, b) for a, b in OUT])
            col = look if s > 0 else (lambda u, v, look=look: look(-u, v))
            k.decal(E * np.array([s, 1, 1]), de * np.array([s, 1, 1]), (0.025, 0.019), col, outline=out_,
                    eps=0.003, rings=20, segs=56)

    k.build(voxel=0.0062, smooth=3, band=0.004, wband=0.05)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    passimian(*args())
