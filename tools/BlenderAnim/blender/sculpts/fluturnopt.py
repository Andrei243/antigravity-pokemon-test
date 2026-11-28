# Fluturnopt sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/fluturnopt.py -- <the export's Fluturnopt.glb> <out.glb>
# Grown from noptila.py (sibling("noptila")): the same grey fur, the same dark rounded snout, the caterpillar's pale
# watching eye-spots grown into the owl-eyes of its wings.
# From its picture (docs/art/reference/gemini/carpathia/10050-Fluturnopt.jpg) and its brief (CP-dex-1b, No. 50): a
# plush grey night moth hovering. A big round fluffy head with a pale ruff under the chin, big round eyes (white, a
# dark brown iris, a black pupil, a glint), a small dark snout like a beak; two feathery antennae, pale gold with grey,
# rising from the crown and curling out; a fuzzy thorax and a long banded abdomen hanging down and back; little grey
# arms with dark claws held forward, the near one holding a small brass lantern that glows; two great dusk-purple
# wings, the largest mass: a forewing rising high with a big gold-rimmed owl-eye and a pale band, a rounder hindwing
# under it with an eye of cream, black and gold rings; every wing edged in a darker purple. The wings are a mirror
# pair raised in a V and swept back so both of the game's cameras see their faces.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, slab, lathe, srgb, inside, unit, eye_look, lumpy, args, sibling

nt = sibling("noptila")

GREY, LIGHT, DARK, CLAW = nt.GREY, "#B3ABA4", nt.DARK, nt.CLAW
PURPLE, RIM, GOLD, CREAM, PUPIL = "#857095", "#4E3F5E", "#D9BE6A", nt.SPOT, "#2E2A30"
HEART, BRASS, GLASS = "#F1E8D2", "#6A5638", "#FFE09A"
ANT_GOLD, ANT_GREY = "#DCCB8E", "#7E7874"

HC = np.array([0.0, -0.072, 0.418])
HR = np.array([0.108, 0.1, 0.1])
SNOUT = (np.array([0, -0.168, 0.398]), np.array([0.032, 0.026, 0.026]))
VOXEL = 0.0058


def body(k):
    k.add("head", ell(HC, HR, deform=lumpy(7, 5, 0.035)), GREY, "head")
    k.add("ruff", ell((0, -0.118, 0.34), (0.085, 0.055, 0.05), deform=lumpy(9, 3, 0.06)), LIGHT, "head")
    k.add("snout", ell(*SNOUT), DARK, "head")
    k.add("thorax", ell((0, -0.005, 0.305), (0.102, 0.098, 0.098), deform=lumpy(6, 4, 0.04)), GREY, "body")
    # the abdomen hanging down and back in three plump rolls
    for i, (y, z, r) in enumerate(((0.03, 0.235, 0.088), (0.062, 0.17, 0.08), (0.088, 0.11, 0.062))):
        k.add(f"abdomen{i}", ell((0, y, z), (r, r * 1.02, r * 0.78), rot=(-28, 0, 0)), GREY, "body")
    # darker creases between the rolls and a paler front of the thorax
    k.paint(lambda P: inside(np.abs((P[:, 2] - 0.2) + 0.5 * (P[:, 1] - 0.045)) - 0.007, 0.004)
            * inside(P[:, 2] - 0.26, 0.01), "#6E6864")
    k.paint(lambda P: inside(np.abs((P[:, 2] - 0.138) + 0.5 * (P[:, 1] - 0.077)) - 0.006, 0.004)
            * inside(P[:, 2] - 0.2, 0.01), "#6E6864")
    k.paint(lambda P: inside(P[:, 1] + 0.07, 0.02) * inside(P[:, 2] - 0.33, 0.02) * inside(0.25 - P[:, 2], 0.02),
            LIGHT)


def limbs(k):
    """Short fuzzy arms held forward and down, little legs tucked under the thorax; dark claws."""
    for name, bone, a, b in (("arm", "arm{s}", (0.07, -0.07, 0.3), (0.098, -0.142, 0.252)),
                             ("leg", "leg{s}", (0.055, -0.03, 0.21), (0.078, -0.098, 0.16))):
        a, b = np.array(a), np.array(b)
        k.add(name, tube([a, (a + b) / 2 + np.array([0.006, 0, 0.004]), b], [0.026, 0.023, 0.02]), GREY, bone,
              mirror=True)
        for j, off in enumerate((-0.012, 0.012)):
            c = b + np.array([off, -0.012, -0.012])
            k.add(f"{name}claw{j}", hull([c + np.array([0.006, 0, 0.004]), c + np.array([-0.006, 0, 0.004]),
                                          c + np.array([0, 0.007, 0.004]), c + np.array([0, -0.004, 0.006]),
                                          c + np.array([0, -0.01, -0.018])]), CLAW, bone, fuse=False, mirror=True,
                  surface="shell")


def lantern(k):
    """The small brass lantern hanging from the +X hand (the side the game's front camera sees)."""
    h = np.array([0.098, -0.16, 0.238])
    down = (0, 0, -1)
    ring = [h + np.array([0, 0.016 * math.sin(t), 0.016 * math.cos(t) - 0.002]) for t in np.linspace(0.4, 2 * math.pi - 0.4, 9)]
    k.add("handle", tube(ring, [0.0055] * 9), BRASS, "armR", fuse=False, surface="metal")
    top = h + np.array([0, 0, -0.02])
    k.add("cap", lathe(top, down, [(0, 0), (0.008, 0.002), (0.024, 0.022), (0.03, 0.03), (0.03, 0.036), (0, 0.036)]),
          BRASS, "armR", fuse=False, surface="metal")
    k.add("glass", lathe(top + np.array([0, 0, -0.034]), down, [(0, 0), (0.022, 0.0), (0.03, 0.03), (0.026, 0.068),
                                                                 (0, 0.068)]),
          GLASS, "armR", fuse=False, glow=GLASS)
    for s in (1, -1):
        for t in (0.0, 1.0):
            p = top + np.array([0.031 * s * (1 - t), 0.031 * s * t, -0.034])
            k.add(f"bar{s}{int(t)}", tube([p, p + np.array([0.003 * np.sign(p[0] - top[0]) if t == 0 else 0,
                                                             0.003 * s if t else 0, -0.034]), p + np.array([0, 0, -0.068])],
                                          [0.0045] * 3), BRASS, "armR", fuse=False, surface="metal")
    k.add("base", lathe(top + np.array([0, 0, -0.1]), down, [(0, 0), (0.032, 0.0), (0.034, 0.012), (0.02, 0.018),
                                                              (0, 0.018)]), BRASS, "armR", fuse=False, surface="metal")


def antennae(k):
    """Feathery antennae: a pale gold comb along a grey shaft, rising from the crown and curling outward."""
    for sx, side in ((1, "R"), (-1, "L")):
        root = np.array([0.03 * sx, -0.095, 0.488])
        d = unit(0.42 * sx, -0.18, 1.0)
        across = unit(1.0 * sx, 0.0, -0.42)
        L, W = 0.165, 0.05
        t = np.linspace(0.0, 1.0, 23)
        curl = 0.07 * t ** 2.0
        w = W * np.sin(np.pi * np.clip(t * 0.92 + 0.04, 0, 1)) ** 0.7 + 0.004
        teeth = 1 + 0.28 * np.where(np.arange(len(t)) % 2 == 0, 1.0, -1.0)
        teeth[[0, -1]] = 1
        upper = np.stack([t * L, curl + w * teeth], axis=1)
        lower = np.stack([t * L, curl - w * teeth * 0.85], axis=1)[::-1]
        outline = np.vstack([upper, lower])
        shaft = np.vstack([np.stack([t[:-1] * L, curl[:-1] + 0.007], axis=1),
                           np.stack([t[:-1] * L, curl[:-1] - 0.007], axis=1)[::-1]])
        if sx > 0:
            ex, ez = d, across
        else:
            ex, ez = d, -across                     # keeps the frame right-handed on the mirrored side
            outline = outline * np.array([1, -1]); shaft = shaft * np.array([1, -1])
        k.add("antenna" + side, slab(outline, root, ex, ez, 0.011), ANT_GOLD, "ear" + side, fuse=False)
        k.add("shaft" + side, slab(shaft, root, ex, ez, 0.017), ANT_GREY, "ear" + side, fuse=False)


# --- the wings ----------------------------------------------------------------------------------------------------
FORE = [(0.0, 0.03), (0.08, 0.075), (0.18, 0.118), (0.28, 0.148), (0.36, 0.158), (0.425, 0.132), (0.452, 0.07),
        (0.44, -0.005), (0.4, -0.07), (0.32, -0.102), (0.21, -0.094), (0.11, -0.066), (0.04, -0.044), (0.0, -0.026)]
HIND = [(0.0, 0.03), (0.07, 0.072), (0.15, 0.09), (0.22, 0.078), (0.268, 0.028), (0.262, -0.046), (0.214, -0.094),
        (0.13, -0.098), (0.055, -0.066), (0.0, -0.032)]


def shrink(pts, k_):
    p = np.array(pts, float); c = p.mean(axis=0)
    return c + (p - c) * k_


def oval(c, a, b, n=28, point=0.0):
    """An almond or an oval about c in the wing's own (a, b): point sharpens its two ends."""
    th = np.linspace(0, 2 * math.pi, n, endpoint=False)
    r = 1 - point * (1 - np.abs(np.sin(th)))
    return np.stack([c[0] + a * np.cos(th), c[1] + b * np.sin(th) * r], axis=1)


def wing(k, name, outline, root, ex, ez, marks):
    """A plate edged in the dark rim, the purple inset over it, and each mark a thicker plate stacked on that."""
    k.add(name, slab(outline, root, ex, ez, 0.009), RIM, "wing{s}", fuse=False, mirror=True, surface="leaf")
    k.add(name + "face", slab(shrink(outline, 0.93), root, ex, ez, 0.012), PURPLE, "wing{s}", fuse=False,
          mirror=True, surface="leaf")
    for j, (pts, col) in enumerate(marks):
        k.add(f"{name}mark{j}", slab(pts, root, ex, ez, 0.015 + 0.003 * j), col, "wing{s}", fuse=False, mirror=True,
              surface="leaf")


def wings(k):
    root = np.array([0.045, 0.05, 0.338])
    ex, ez = unit(1.0, 0.42, 0.95), unit(-0.3, 0.3, 1.0)
    e = np.array([0.3, 0.02])
    band = [(0.12, -0.03), (0.2, -0.06), (0.3, -0.07), (0.38, -0.055), (0.41, -0.03), (0.38, -0.045), (0.3, -0.052),
            (0.2, -0.044), (0.13, -0.015)]
    wing(k, "fore", FORE, root, ex, ez,
         [(band, CREAM), (oval(e, 0.078, 0.052, point=0.55), GOLD), (oval(e, 0.058, 0.036, point=0.5), CREAM),
          (oval(e + (0.004, 0), 0.034, 0.028), PUPIL), (oval(e + (-0.01, 0.006), 0.012, 0.01), HEART)])
    hroot = root + np.array([-0.005, 0.015, -0.03])
    hx, hz = unit(1.0, 0.5, -0.62), unit(0.0, 0.3, 1.0)
    h = np.array([0.16, -0.008])
    wing(k, "hind", HIND, hroot, hx, hz,
         [(oval(h, 0.075, 0.06), CREAM), (oval(h, 0.058, 0.046), PUPIL), (oval(h, 0.044, 0.034, point=0.4), GOLD),
          (oval(h, 0.026, 0.02), PUPIL), (oval(h + (-0.008, 0.005), 0.009, 0.008), HEART)])


# --- the face -----------------------------------------------------------------------------------------------------
WHITE, IRIS, INK, GLINT = srgb("#F2EEE4"), srgb("#4A3426"), srgb("#141012"), np.array([1.0, 1.0, 1.0])
EYEDIR = unit(0.44, -0.84, 0.26)
EYE = (0.037, 0.037)


def eye_open(u, v):
    r = np.hypot(u, v)
    c = np.tile(WHITE, (len(u), 1))
    c[np.hypot(u + 0.12, v + 0.02) < 0.62] = IRIS
    c[np.hypot(u + 0.12, v + 0.02) < 0.38] = INK
    c[np.hypot(u + 0.34, v - 0.3) < 0.2] = GLINT
    c[r > 0.86] = INK
    return c


def eyes(k):
    E = HC + EYEDIR / np.linalg.norm(EYEDIR / HR)
    m = np.array([-1, 1, 1.0])

    def lay(state):
        look = eye_look(state, eye_open, srgb(GREY), INK, line=0.17)
        k.decal(E, EYEDIR, EYE, look, eps=0.006, rings=20, segs=56)
        k.decal(E * m, EYEDIR * m, EYE, lambda u, v: look(-u, v), eps=0.006, rings=20, segs=56)
    return lay


def fluturnopt(src, out):
    k = Kit(src, coat="fur")
    body(k)
    limbs(k)
    lantern(k)
    antennae(k)
    wings(k)
    k.build(voxel=VOXEL, band=0.008, wband=0.05)
    k.eye_states(eyes(k))
    k.export(out)


if __name__ == "__main__":
    fluturnopt(*args())
