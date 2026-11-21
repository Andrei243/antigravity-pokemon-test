# Meowscarada sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/meowscarada.py -- <the export's Meowscarada.glb> <out.glb>
# Grown from sprigatito.py and floragato.py (the colours' family, the head coordinates, the sly eye cut by its lid,
# the mask painted round the head's axis, the spikes of fur).
# From the references: a tall magician of a cat; a hood of very dark green drawn up over the head into a tall point
# leaning to its right (the viewer's left) and a shorter one to its left; the dark of the hood comes down over the eyes
# as a mask, with a bright green diamond over each eye; sly half-shut pink eyes; a pale muzzle; pale locks of hair
# hanging at both sides of the face; a ruff of pink petals at the throat; a pale body with a dark green V down the
# chest and a skirt of pale fur with a jagged hem; great puffy pale sleeves, dark green hands with dark claws; a cloak
# of leaves hanging from the shoulders and arms, mid green outside and brighter inside, flaring to a jagged hem at the
# knees, the largest mass; long dark green legs; a short pale tail. (The flower bomb it juggles is left out: it floats
# apart from the body.)
# (The pictures overrule the task's text: the mask is the hood's own very dark green, not a mask of its own; the
# flower at the chest is a ruff of pink petals round the throat; the cloak hangs from the arms as much as the shoulders.)
# Reference ratios (3D picture): hood and head / height 0.26 (here 0.3, the head a third larger), cloak's span / height
# 0.9 drawn spread (here 0.52, carried down and back), leg showing under the skirt / height 0.4 (here 0.34: the cloak and
# skirt hang low and the shins are thick).
# The skeleton's joints: body (0, 0, 0.55), arms (+-0.06, 0, 0.63), head (0, -0.01, 0.68), legs (+-0.045, 0, 0.43).
# About 0.95 tall.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, hull, sheet, outward, srgb, inside, smoothstep, unit, polygon, boxy, tri,
                       args, sibling)

sp = sibling("sprigatito")
WHITE = "#EDF4E2"         # the body: a pale, nearly white green
HOOD = "#22432A"          # the hood, the mask, the hands and the legs
CLOAK, CLOAKIN = "#3A9A42", "#5CBF52"
BRIGHT = sp.BRIGHT
MARK = "#3E8A3A"           # the diamonds over the eyes: a mid green, marks and not eyes
PETAL, PETALDARK = "#EC6F92", "#C84470"
PINK, PINKDARK, INK = "#E8608A", "#B8335E", sp.INK
CLAW = "#141E16"

H = np.array([0.0, -0.028, 0.8])
HR = np.array([0.112, 0.104, 0.112])
EYE_X, EYE_Z = 0.05, 0.0
EYE_HALF = (0.046, 0.03)
# a sly half-shut eye: the lid a straight line sloping down toward the nose, the bottom round (u outward)
EYE_SHAPE = [(-1.0, 0.18), (0.0, 0.34), (1.0, 0.5), (1.0, -0.2), (0.55, -0.75), (-0.2, -0.85), (-0.8, -0.55)]
# the pale lower face, below the hood's mask (arc out from the middle, height from the head's middle)
MUZZLE = [(0.0, -0.02), (0.036, -0.034), (0.079, -0.036), (0.097, -0.07), (0.07, -0.117), (0.027, -0.134),
          (0.0, -0.137)]


def add_head(k):
    k.add("head", ell(H, HR, deform=boxy(2.2)), HOOD, "head")
    k.add("muzzle", ell(H + np.array([0, -0.072, -0.056]), (0.059, 0.045, 0.042)), WHITE, "head")
    k.add("hoodback", ell(H + np.array([0, 0.042, 0.025]), (0.118, 0.098, 0.112)), HOOD, "head")
    k.add("neck", tube([(0, 0.0, 0.62), (0, -0.01, 0.72)], [0.038, 0.036]), WHITE, "head")
    # the face: dark everywhere the hood is, the muzzle pale below the mask
    k.paint(lambda P: inside(P[:, 2] - 0.705, 0.002) * (P[:, 2] > 0.66), WHITE, on=["neck", "head", "hoodback"])
    sp.mask_paint(k, sp.mirrored(MUZZLE), WHITE, h=H, hr=HR, on=("head", "muzzle"), front=0.05, edge=0.0016)


def add_hood(k):
    """The hood's two points: a tall one leaning to the model's right (-X), a shorter one out to its left (+X)."""
    base = H + np.array([0, 0.016, 0.0])
    tall = [base + np.array(p) * 1.4 for p in ((-0.064, -0.02, 0.05), (0.03, -0.022, 0.07), (-0.06, 0.05, 0.04),
                                         (0.03, 0.05, 0.06), (-0.02, 0.016, 0.1))]
    tall.append(np.array([-0.112, 0.06, 1.03]))
    k.add("hoodpoint", hull(tall), HOOD, "head", fuse=False, surface="cloth")
    short = [base + np.array(p) * 1.4 for p in ((0.02, -0.02, 0.064), (0.075, -0.016, 0.03), (0.02, 0.048, 0.06),
                                          (0.075, 0.044, 0.026), (0.05, 0.016, 0.06))]
    short.append(np.array([0.16, 0.05, 0.955]))
    k.add("hoodear", hull(short), HOOD, "head", fuse=False, surface="cloth")


def add_locks(k):
    """The pale hair: a great fused mass each side of the face, from the hood's edge down to the shoulder, as large
    as the face, ending in a few solid points."""
    k.add("lock", ell((0.108, 0.004, 0.75), (0.052, 0.078, 0.098), rot=(0, -12, 0)), WHITE, "head", mirror=True)
    k.add("lockfoot", ell((0.116, 0.012, 0.672), (0.05, 0.066, 0.045)), WHITE, "head", mirror=True)
    for i, (off, dd, ln) in enumerate([((0.11, -0.03, 0.66), (0.25, -0.2, -1), 0.06),
                                        ((0.13, 0.02, 0.655), (0.45, 0.2, -1), 0.065),
                                        ((0.1, 0.05, 0.665), (0.1, 0.6, -1), 0.055)]):
        for s, side in ((1, "R"), (-1, "L")):
            root = np.array([s * off[0], off[1], off[2]])
            k.add(f"locktip{i}{side}", sp.spike(root, (s * dd[0], dd[1], dd[2]), ln, 0.03, 0.026), WHITE, "head",
                  fuse=False, surface="fur")


def add_marks(k):
    """A bright green diamond on the mask over each eye."""
    dia = polygon([(0, 1), (1, 0), (0, -1), (-1, 0)])
    for s in (1, -1):
        p, n = sp.on_head(s * 0.046, H[2] + 0.062, H, HR)
        k.decal(p, n, (0.007, 0.011), MARK, eps=0.0018, outline=dia, rings=6, segs=32)
    # a small dark nose and a little cat's smile on the pale muzzle
    p, n = sp.on_head(0.0, H[2] - 0.03, H, HR)
    CW, CI = srgb(WHITE), srgb(HOOD)

    def mouth(u, v):
        c = np.tile(CW, (len(u), 1))
        c[(np.abs(v - (-0.1 + 1.3 * (np.abs(u) - 0.42) ** 2)) < 0.2) & (np.abs(u) < 0.9) & (v < 0.3)] = CI
        c[(np.abs(u) < 0.24) & (v > 0.35)] = CI
        return c
    k.decal(H + np.array([0, -0.114, -0.072]), unit(0, -1, -0.1), (0.022, 0.015), mouth, eps=0.0018, rings=10,
            segs=48)


def add_collar(k):
    """A ruff of pink petals round the throat, the biggest at the sides like a bow."""
    c = np.array([0.0, -0.01, 0.685])
    for i, (ang, ln, w, dz) in enumerate([(25, 0.05, 0.03, 0.0), (65, 0.06, 0.034, 0.004), (105, 0.05, 0.03, 0.0),
                                          (140, 0.04, 0.026, 0.0)]):
        a = math.radians(ang)
        d = np.array([math.sin(a), -math.cos(a), -0.15])
        for s, side in ((1, "R"), (-1, "L")):
            m = np.array([s, 1, 1.0])
            dd = d * m
            cen = c + dd * (ln * 0.5 + 0.03) + np.array([0, 0, dz])
            rot = (0, 0, math.degrees(math.atan2(dd[0], -dd[1])) * -1)
            k.add(f"petal{i}{side}", outward(ell(cen, (w, ln * 0.55, 0.017), rot=rot, sub=3)), PETAL if i % 2 == 0 else PETALDARK,
                  "body", fuse=False, surface="leaf")


def add_body(k):
    k.add("chest", ell((0, 0.0, 0.6), (0.072, 0.056, 0.072)), WHITE, "body")
    k.add("waist", ell((0, 0.004, 0.52), (0.06, 0.05, 0.06)), WHITE, "body")
    k.add("hips", ell((0, 0.006, 0.455), (0.082, 0.066, 0.06)), WHITE, "body")
    # the dark V down the chest
    k.paint(lambda P: inside(np.abs(P[:, 0]) - 0.032 * np.clip((P[:, 2] - 0.54) / 0.13, 0, 1), 0.002)
            * (P[:, 2] > 0.54) * (P[:, 1] < -0.02), HOOD, on=["chest", "waist", "neck"])
    # the skirt of pale fur: points round the hips, hanging down to a jagged hem
    for i in range(14):
        a = 2 * math.pi * (i + 0.5) / 14
        r = np.array([math.sin(a) * 0.078, -math.cos(a) * 0.062, 0.44])
        d = unit(math.sin(a) * 0.35, -math.cos(a) * 0.3, -1)
        ln = 0.095 if i % 2 else 0.072
        k.add(f"skirt{i}", sp.spike(r, d, ln, 0.026, 0.012), WHITE, "body", fuse=False, surface="fur")
    # a short pale tail
    k.add("tail", sp.spike((0.0, 0.06, 0.45), (0.35, 0.8, -0.2), 0.08, 0.022, 0.03), WHITE, "body", fuse=False,
          surface="fur")


def add_arms(k):
    sh, el, wr = np.array([0.07, 0.0, 0.628]), np.array([0.112, -0.03, 0.55]), np.array([0.068, -0.106, 0.5])
    k.add("upperarm", tube([sh, el], [0.04, 0.044]), WHITE, "arm{s}", mirror=True)
    k.add("sleeve", ell((0.116, -0.04, 0.55), (0.069, 0.072, 0.08), rot=(25, -15, 0)), WHITE, "arm{s}", mirror=True)
    k.add("forearm", tube([el + np.array([-0.006, -0.03, -0.01]), wr], [0.03, 0.026]), WHITE, "arm{s}", mirror=True)
    k.add("hand", ell(wr + np.array([0, -0.02, -0.006]), (0.03, 0.032, 0.03)), HOOD, "arm{s}", mirror=True)
    for j, dx in enumerate((-0.016, 0.0, 0.016)):
        base = wr + np.array([dx, -0.042, -0.008])
        for s, side in ((1, "R"), (-1, "L")):
            m = np.array([s, 1, 1.0])
            k.add(f"claw{j}{side}", sp.spike(base * m, unit(0, -1, -0.6), 0.02, 0.007, 0.007), CLAW, f"arm{side}",
                  fuse=False)
    # the pink pad on each palm
    for s, side in ((1, "R"), (-1, "L")):
        k.add(f"palm{side}", outward(ell(np.array([0.068 * s, -0.12, 0.478]), (0.016, 0.014, 0.008), sub=3)), PINK,
              f"arm{side}", fuse=False)


def add_legs(k):
    k.add("thigh", ell((0.056, 0.004, 0.37), (0.082, 0.08, 0.11)), HOOD, "leg{s}", mirror=True)
    k.add("shin", tube([(0.064, 0.0, 0.31), (0.07, -0.004, 0.18), (0.066, 0.006, 0.06)], [0.06, 0.048, 0.04]),
          HOOD, "leg{s}", mirror=True)
    k.add("foot", ell((0.066, -0.034, 0.034), (0.05, 0.08, 0.036)), HOOD, "leg{s}", mirror=True)
    k.paint(lambda P: inside(P[:, 2] - 0.43, 0.003), HOOD, on=["thigh", "hips"])


def cloak_surface(r_top, r_bot, z_top, z_bot, th_top, th_bot, jag):
    """The cloak as one surface round the body, open at the front: th from -th round the back (+Y) to +th."""
    def at(u, v):
        s = 2 * (0.008 + 0.984 * u) - 1
        a = np.abs(s)
        th = np.radians(s * (th_top + (th_bot - th_top) * v))
        bottom = z_bot + jag * (0.5 + 0.5 * tri(a * 3.5)) + 0.05 * a
        z = z_top + (bottom - z_top) * v
        r = r_top + (r_bot - r_top) * v ** 0.8 + 0.03 * a * v
        return np.stack([r * np.sin(th), r * np.cos(th), z], axis=-1)
    return at


def add_cloak(k):
    outer = cloak_surface(0.1, 0.28, 0.66, 0.27, 112, 128, 0.07)
    inner = cloak_surface(0.084, 0.262, 0.652, 0.285, 110, 126, 0.07)
    k.add("cloak", outward(sheet(outer, 80, 24, 0.022)), CLOAK, "body", fuse=False, surface="leaf")
    k.add("cloakin", outward(sheet(inner, 80, 24, 0.016)), CLOAKIN, "body", fuse=False, surface="leaf")


def meowscarada(src, out_path):
    k = Kit(src, coat="fur")
    add_body(k)
    add_arms(k)
    add_legs(k)
    add_head(k)
    add_hood(k)
    add_locks(k)
    add_collar(k)
    add_cloak(k)
    add_marks(k)
    # a pale rim round each eye, in every state, under the eye's own patch
    at, n = sp.on_head(EYE_X, H[2] + EYE_Z, H, HR)
    n = unit(n[0] * 1.1, n[1], n[2])
    rim = (EYE_HALF[0] * 1.07, EYE_HALF[1] * 1.14)
    k.decal(at, n, rim, WHITE, eps=0.0011, outline=polygon(EYE_SHAPE), rings=12, segs=56)
    q = np.array([-1, 1, 1.0])
    k.decal(at * q, n * q, rim, WHITE, eps=0.0011, outline=polygon([(-a, b) for a, b in EYE_SHAPE]), rings=12,
            segs=56)
    k.build(voxel=0.0075, smooth=4, band=0.006, wband=0.06)
    at, n = sp.on_head(EYE_X, H[2] + EYE_Z, H, HR)
    n = unit(n[0] * 1.1, n[1], n[2])
    k.eye_states(lambda s: sp.lay_eyes(k, s, at, n, EYE_HALF,
                                       sp.eye_colour(iris=PINK, iris_dark=PINKDARK, iris_r=0.8, pupil_w=0.22,
                                                     pupil_h=0.44),
                                       HOOD, "#B9E6A8", outline=EYE_SHAPE))
    k.export(out_path)


if __name__ == "__main__":
    meowscarada(*args())
