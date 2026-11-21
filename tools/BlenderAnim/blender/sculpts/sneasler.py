# Sneasler sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py). Sneasel's and
# Weavile's scripts build as they are read and cannot be imported, so their ways are taken over here: the almond eye
# with a red iris under a dark lid (Weavile's eye), white hull claws on the feet, a gem on the brow.
#   blender --background --factory-startup --python sculpts/sneasler.py -- <the export's Sneasler.glb> <out.glb>
#
# The design, from the references: a tall lean weasel standing on two legs, pale grey-blue; a big round cat's head,
# a pointed ear on its -X side and on its +X side a long crest (the "feather" of the task's text) that rises over the
# head and sweeps back and down behind it, dark purple turning magenta toward its end; a violet mask over the lower
# face with a jagged edge up round the eyes; sly red eyes under heavy dark lids; a small grin with one fang; an orange
# gem on the brow and another on the chest; a spiky pale ruff round the neck; long arms, a violet mark on each
# forearm, dark purple hands with three great curved claws striped magenta, held down in front of the thighs; long
# bent legs, violet ragged fur at the ankles and feet, three white claws on each foot.
# The joints: body (0, 0, 0.52), arms (+-0.08, 0, 0.63), head (0, -0.02, 0.67), legs (+-0.06, 0.01, 0.4).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, hull, axes, srgb, inside, unit, tri, eye_look, polygon, args, sibling

t = sibling("teddiursa")
GREY, PALE, VIOLET, DARK, MAGENTA = "#B6C3E4", "#E8EDF6", "#6A5BB2", "#3B2452", "#E2306E"
GOLD, GOLDRIM, WHITE, INK, RED, MOUTH = "#F4B030", "#C8701E", "#F6F7FA", "#16141E", "#C8283E", "#2A1428"

H = np.array([0.0, -0.035, 0.815])          # the head's middle
S = 1.18                                   # the head grown about its middle, a third over the measured ratio
HR = np.array([0.108, 0.098, 0.1]) * S


def spike(base, tip, r, n=5, flat=0.7):
    base, tip = np.array(base, float), np.array(tip, float)
    a, b = axes(tip - base)
    pts = [base + r * (math.cos(q) * a + flat * math.sin(q) * b) for q in np.linspace(0, 2 * math.pi, n, endpoint=False)]
    return hull(pts + [tip])


def body(k):
    k.add("chest", ell((0, 0.0, 0.6), (0.078, 0.062, 0.09)), GREY, "body")
    k.add("waist", ell((0, 0.005, 0.5), (0.06, 0.054, 0.075)), GREY, "body")
    k.add("hips", ell((0, 0.01, 0.42), (0.08, 0.066, 0.062)), GREY, "body")
    k.add("neck", tube([(0, 0.0, 0.64), (0, -0.015, 0.72)], [0.045, 0.042]), GREY, "head")
    # the spiky pale ruff round the neck, open over the throat a little
    for i in range(11):
        a = math.radians(-150 + 300 * i / 10)
        o = np.array([math.sin(a), -math.cos(a), 0.0])
        b = np.array([0, -0.005, 0.675]) + o * np.array([0.05, 0.045, 0]) + (0, 0, 0.006 * (i % 2))
        k.add(f"ruff{i}", spike(b, b + o * 0.05 + (0, 0, -0.02 + 0.012 * (i % 2)), 0.017), PALE, "body", fuse=False,
              surface="fur")
    # the gem on the chest: an orange diamond standing proud
    c = np.array([0, -0.064, 0.61])
    k.add("chestgem", hull([c + (0, 0, 0.026), c + (0, 0, -0.026), c + (0.016, 0, 0), c + (-0.016, 0, 0),
                            c + (0, -0.01, 0), c + (0, 0.008, 0)]), GOLD, "body", fuse=False, surface="shell")


def legs(k):
    for s, bone in ((1, "legR"), (-1, "legL")):
        m = np.array([s, 1, 1.0])
        k.add("thigh" + bone[-1], ell(np.array((0.072, -0.005, 0.355)) * m, (0.052, 0.058, 0.085)), GREY, bone)
        k.add("leg" + bone[-1], tube([np.array(p) * m for p in [(0.075, -0.01, 0.33), (0.095, -0.055, 0.22),
                                                                (0.1, 0.0, 0.11), (0.1, -0.01, 0.05)]],
                                     [0.046, 0.034, 0.03, 0.034]), GREY, bone)
        k.add("ankle" + bone[-1], ell(np.array((0.1, -0.005, 0.07)) * m, (0.042, 0.042, 0.045)), GREY, bone)
        k.add("foot" + bone[-1], ell(np.array((0.1, -0.035, 0.03)) * m, (0.04, 0.06, 0.03)), VIOLET, bone)
        for j, dx in enumerate((-0.02, 0.0, 0.02)):
            a = np.array([0.1 + dx, -0.085, 0.024]) * m
            k.add(f"toe{bone[-1]}{j}", spike(a, a + (dx * 0.3 * s, -0.04, -0.016), 0.012, n=5, flat=0.8), WHITE, bone,
                  fuse=False, surface="shell")
    # violet ragged fur over the ankles and feet
    def cuff(P):
        edge = 0.13 + 0.018 * tri(np.arctan2(P[:, 1], np.abs(P[:, 0]) - 0.1) * 7 / (2 * math.pi))
        return inside(P[:, 2] - edge, 0.003)
    k.paint(cuff, VIOLET, on=["leg", "ankle", "foot"])


SH = np.array([0.082, 0.0, 0.635])
EL = np.array([0.13, -0.02, 0.52])
WR = np.array([0.115, -0.085, 0.42])


def claw(k, name, root, mid, tip, r, color, bone):
    """A great curved claw in two solid pieces: thick at its root, bending down to a point."""
    root, mid, tip = np.array(root, float), np.array(mid, float), np.array(tip, float)
    k.add(name + "a", spike(root, mid + (mid - root) * 0.08, r, n=6, flat=0.65), color, bone, fuse=False,
          surface="shell")
    k.add(name + "b", spike(mid - (mid - root) * 0.15, tip, r * 0.62, n=6, flat=0.65), color, bone, fuse=False,
          surface="shell")


def arms(k):
    for s, bone in ((1, "armR"), (-1, "armL")):
        m = np.array([s, 1, 1.0])
        k.add("upper" + bone[-1], tube([SH * m, ((SH + EL) / 2 + (0.008, 0, 0)) * m, EL * m], [0.036, 0.036, 0.032]),
              GREY, bone)
        k.add("fore" + bone[-1], tube([EL * m, WR * m], [0.034, 0.037]), GREY, bone)
        k.add("hand" + bone[-1], ell((WR + (0.0, -0.012, -0.02)) * m, (0.04, 0.042, 0.045)), DARK, bone)
        # the three great claws, down and a little forward, their tips at the knee
        for j, dx in enumerate((-0.022, 0.0, 0.022)):
            root = WR + (dx, -0.03, -0.04)
            mid = WR + (dx * 1.2, -0.07, -0.11)
            tip = WR + (dx * 1.1 - 0.005, -0.065, -0.2)
            claw(k, f"claw{bone[-1]}{j}", root * m, mid * m, tip * m, 0.019, MAGENTA if j == 1 else DARK, bone)
        # a short spike of fur at the elbow's back
        k.add("elbow" + bone[-1], spike(EL * m + (0.01 * s, 0.015, 0.0), (EL + (0.035, 0.05, 0.03)) * m, 0.014),
              GREY, bone, fuse=False, surface="fur")
    # the dark purple of the hands reaching up the wrist in points, a violet mark on the forearm
    AD = unit(*(WR - EL))
    def gauntlet(P):
        Q = P.copy(); Q[:, 0] = np.abs(Q[:, 0])
        tt = (Q - EL) @ AD
        across = Q - EL - np.outer(tt, AD)
        ang = np.arctan2(across[:, 2], across[:, 0])
        L = np.linalg.norm(WR - EL)
        return inside((L * 0.72 + 0.012 * tri(ang * 4 / (2 * math.pi))) - tt, 0.003)
    k.paint(gauntlet, DARK, on=["fore", "hand"])
    def mark(P):
        Q = P.copy(); Q[:, 0] = np.abs(Q[:, 0])
        c = EL + (WR - EL) * 0.42 + (0.03, 0.0, 0.0)
        return inside(np.linalg.norm(Q - c, axis=1) - 0.02, 0.003)
    k.paint(mark, VIOLET, on=["fore"])


def head(k):
    def shape(V):
        V = V.copy()
        low = np.clip(-V[:, 2], 0, 1)
        V[:, 0] *= 1 - 0.18 * low ** 1.5
        return V
    HEAD = ell(H, HR, deform=shape)
    k.add("head", HEAD, GREY, "head")
    k.add("muzzle", ell(H + S * np.array((0, -0.075, -0.04)), np.array((0.052, 0.04, 0.036)) * S), GREY, "head")
    # the pointed ear on the -X side, up and out
    k.add("ear", blade([H + S * np.array((-0.06, 0.0, 0.065)), H + S * np.array((-0.085, 0.005, 0.11)), H + S * np.array((-0.105, 0.01, 0.15))],
                       [0.036, 0.022, 0.004], (0, 1, 0), 0.55), GREY, "head")
    # the violet mask over the lower face, its jagged edge up round the eyes and up the middle to the gem
    z0 = H[2]
    MASK = t.mirrored([(0.0, z0 + 0.03), (0.015, z0 + 0.0), (0.03, z0 - 0.012), (0.06, z0 - 0.025), (0.085, z0 - 0.03),
                       (0.1, z0 - 0.03), (0.09, z0 - 0.06), (0.105, z0 - 0.08), (0.07, z0 - 0.1), (0.04, z0 - 0.085),
                       (0.0, z0 - 0.1)])
    MASK = [(a * S, z0 + (z - z0) * S) for a, z in MASK]
    FRONT = lambda P: P[:, 1] < H[1] - 0.02
    k.paint(lambda P: t.poly_mask(*t.round_about(P, H, HR[0]), MASK, edge=0.0025) * FRONT(P), VIOLET,
            on=["head", "muzzle"])
    # the grin with its fang, low on the muzzle
    CM, CF, CV = srgb(MOUTH), srgb(WHITE), srgb(VIOLET)
    def grin(u, v):
        c = np.tile(CV, (len(u), 1))
        top = 0.25 + 0.2 * u
        low = top - 0.25 - 0.5 * np.clip(1 - ((u - 0.25) / 0.7) ** 2, 0, 1)
        dark = (v < top) & (v > low) & (np.abs(u) < 0.9)
        c[dark] = CM
        fang = dark & (v > top - 0.5) & (np.abs(u - 0.45) < 0.2 * (v - (top - 0.5)) / 0.5)
        c[fang] = CF
        return c
    MC = t.hit(ell(H + S * np.array((0, -0.075, -0.04)), np.array((0.052, 0.04, 0.036)) * S), (0.0, -1, z0 - 0.05 * S), (0, 1, 0))
    k.decal(MC, unit(0, -1, -0.1), (0.034 * S, 0.02 * S), grin, eps=0.004, rings=12, segs=48)
    # the gem on the brow
    G = t.hit(HEAD, (0, -1, z0 + 0.055 * S), (0, 1, 0))
    k.add("browgem", hull([G + (0, -0.004, 0.026), G + (0, -0.004, -0.022), G + (0.017, -0.003, 0.0),
                           G + (-0.017, -0.003, 0.0), G + (0, -0.014, 0.002), G + (0, 0.006, 0)]), GOLD, "head",
          fuse=False, surface="shell")
    return HEAD


def crest(k):
    """The long crest on the +X side: up over the head, then back and down behind, dark purple turning magenta."""
    P = [H + S * np.array((0.06, 0.01, 0.07)), H + S * np.array((0.1, 0.06, 0.13)), H + S * np.array((0.13, 0.15, 0.13)), H + S * np.array((0.15, 0.22, 0.06)),
         H + S * np.array((0.16, 0.25, -0.05))]
    k.add("crest", blade(P, [0.026, 0.03, 0.03, 0.028, 0.024], unit(0.8, 0, 0.2), 0.65), DARK, "head", fuse=False,
          surface="fur")
    Q = [H + S * np.array((0.158, 0.245, -0.03)), H + S * np.array((0.165, 0.26, -0.16)), H + S * np.array((0.15, 0.24, -0.29)), H + S * np.array((0.13, 0.21, -0.36))]
    k.add("crestend", blade(Q, [0.025, 0.026, 0.022, 0.006], unit(0.9, -0.2, 0), 0.65), MAGENTA, "head", fuse=False,
          surface="fur")


# --- the eyes: almond, slanting up to the outer corner, a heavy dark lid over a red iris -------------------------------
CG, CW, CR, CP, CI, CV = srgb(GREY), srgb(WHITE), srgb(RED), srgb("#1A0A10"), srgb(INK), srgb(VIOLET)


def top(u):
    return 0.12 + 0.3 * u


def bottom(u):
    return -0.62 + 0.22 * u + 0.25 * u * u


def eye(u, v):
    c = np.tile(CG, (len(u), 1))
    white = (v < top(u)) & (v > bottom(u)) & (np.abs(u) < 0.96)
    c[white] = CW
    iris = white & (((u - 0.05) / 0.42) ** 2 + ((v + 0.18) / 0.5) ** 2 < 1)
    c[iris] = CR
    c[iris & (((u - 0.05) / 0.15) ** 2 + ((v + 0.2) / 0.28) ** 2 < 1)] = CP
    c[(np.abs(v - top(u) - 0.04) < 0.15) & (np.abs(u) < 0.99)] = CI
    c[(u > 0.6) & (v > top(u)) & (v < top(u) + 0.4 * (u - 0.45)) & (u * u + v * v < 0.98)] = CI
    return c


def eyes(k, HEAD):
    de = unit(0.5, -0.86, 0.06)
    E = t.hit(HEAD, (0.048 * S, -1, H[2] + 0.004), (0, 1, 0))
    def lay(state):
        ink = CI if state in ("Open", "Fierce") else srgb("#4A4070")
        look = eye_look(state, eye, CG, ink, line=0.17)
        k.decal(E, de, (0.036 * S, 0.03 * S), look, eps=0.004, rings=20, segs=56, mirror=True)
    return lay


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="fur")
    body(k)
    legs(k)
    arms(k)
    HEAD = head(k)
    crest(k)
    k.build(voxel=0.0055, band=0.005)
    k.eye_states(eyes(k, HEAD))
    k.export(out)
