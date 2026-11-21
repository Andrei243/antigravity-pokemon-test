# Galarian Zapdos sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/zapdosgalar.py -- <the export's Zapdos-Galar.glb> <out.glb>
# Zapdos's kin on zapdos.py's frame (its starburst wing over a black one behind, spikes for feathers, the black patch
# round a fierce eye): a great orange running bird leaning forward, its body bristling with jagged feathers that hang
# over black haunches, a long curving neck to a spiky head with a crest swept back, a long thin black beak held level,
# yellow eyes in a black mark slanting toward the beak, jagged orange wings half raised with a black lightning bolt on
# each, black spikes behind them and behind the tail's orange ones, and long thick black legs bent at the hock on great
# black feet whose talons are yellow over a red band.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, lathe, srgb, inside, tri, unit, sibling, args

z = sibling("zapdos")
a = z.a
sp = a.sp

ORANGE, ORANGE2, BLACK, GREY = "#EE6A34", "#E05A2A", "#2A2428", "#5E5460"
BEAK, TALON, BAND = "#1E1A20", "#F6CC22", "#E24A26"
YELLOW, PUPIL, WHITE = "#F8D828", "#16120E", "#FBFBF8"

H = np.array([0, -0.255, 0.955])          # the head's centre, high and forward on its neck (the joint (0, -0.1, 0.8))
HR = np.array([0.086, 0.094, 0.08])     # half as large again as the old sculpt's: a small head on a neck is lost


def spike(k, name, base, tip, w, t, colour, bone):
    k.add(name, sp.spike(base, tip, w, t, up=(0, -1, 0) if abs(unit(*(np.array(tip) - base))[1]) < 0.9 else (0, 0, 1)),
          colour, bone, fuse=False)


ELEV, SWEEP = 26, 34
WANG = [-35, -8, 20, 48, 76]
WLEN = [0.36, 0.5, 0.47, 0.38, 0.27]
WWID = [14, 15, 14, 13, 12]
# a black lightning bolt across the wing's face, in (along the span, along the chord)
BOLT = [(0.07, 0.035), (0.14, -0.005), (0.16, 0.045), (0.24, 0.0), (0.245, 0.045), (0.165, 0.095), (0.145, 0.045),
        (0.075, 0.085)]


def legs(k):
    """Great black haunches fused into the hips, a thick shin down and back to the hock, the foot's bone forward and
    down to great black toes, each with a yellow talon over a red band."""
    for s, n in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        bone = f"leg{n}"
        hip = np.array([0.095, 0.0, 0.55]) * m
        hock = np.array([0.115, 0.12, 0.26]) * m
        foot = np.array([0.11, -0.03, 0.055]) * m
        k.add(f"haunch{n}", ell(hip + np.array([0.0, 0.02, -0.075]), (0.098, 0.12, 0.15), rot=(18, 0, 0)), BLACK, bone)
        k.add(f"shin{n}", tube([hip + (0, 0.01, -0.08), (hip + hock) / 2 + (0.005 * s, 0.02, 0), hock],
                               [0.078, 0.06, 0.046]), BLACK, bone)
        k.add(f"tarsus{n}", tube([hock, (hock + foot) / 2, foot], [0.046, 0.04, 0.038]), BLACK, bone)
        k.add(f"ball{n}", ell(foot + (0, 0, 0.0), (0.045, 0.05, 0.035)), BLACK, bone)
        for j, (ang, L) in enumerate(((-34, 0.1), (0, 0.12), (34, 0.1), (180, 0.07))):
            r = math.radians(ang)
            d = np.array([math.sin(r) * s, -math.cos(r), 0])
            tip = foot + d * L + (0, 0, -0.025)
            k.add(f"toe{n}{j}", tube([foot, foot + d * L * 0.55 + (0, 0, 0.006), tip], [0.026, 0.024, 0.021]), BLACK,
                  bone)
            cd = unit(*(d + (0, 0, -0.7)))
            k.add(f"band{n}{j}", lathe(tip - cd * 0.006, cd, [(0, 0), (0.022, 0), (0.022, 0.014), (0, 0.014)], seg=20),
                  BAND, bone, fuse=False, surface="shell")
            k.add(f"talon{n}{j}", lathe(tip + cd * 0.008, cd, [(0, 0), (0.02, 0), (0.014, 0.022), (0.006, 0.04),
                                                                (0, 0.05)], seg=20), TALON, bone, fuse=False,
                  surface="shell")
        # a light zigzag down the haunch's outer side
        c = hip + np.array([0.0, 0.0, -0.03])
        k.paint((lambda c, s: lambda P: inside(np.abs((P[:, 1] - c[1]) - 0.02 * tri((P[:, 2] - c[2]) / 0.05)) - 0.006,
                                                  0.0025)
                 * inside(np.abs(P[:, 2] - c[2]) - 0.075, 0.004) * (s * P[:, 0] > abs(c[0]) + 0.04))(c, s), GREY,
                on=[f"haunch{n}"])


def galar(src, out):
    k = Kit(src, coat="fur")

    # The trunk leaning forward, its chest up into the neck; the long neck curving up to the head
    k.add("body", ell((0, 0.02, 0.645), (0.155, 0.185, 0.145), rot=(-10, 0, 0)), ORANGE, "body")
    k.add("chest", ell((0, -0.13, 0.72), (0.115, 0.1, 0.115)), ORANGE, "body")
    k.add("neck", tube([(0, -0.14, 0.78), (0, -0.2, 0.86), H + (0, 0.035, -0.04)], [0.07, 0.058, 0.056]), ORANGE,
          "head")
    k.add("head", ell(H, HR), ORANGE, "head")
    # the black under the trunk, between the haunches
    k.paint(lambda P: inside(P[:, 2] - 0.56, 0.006) * inside(np.abs(P[:, 0]) - 0.12, 0.01), BLACK, on=["body"])

    # The crest: jagged spikes swept back and up off the crown and down the nape, short ones under the jaw
    for i, (d, L, w) in enumerate([((0, 0.8, 0.7), 0.17, 0.06), ((0.45, 0.85, 0.5), 0.15, 0.055),
                                   ((-0.45, 0.85, 0.5), 0.15, 0.055), ((0.0, 0.5, 1.0), 0.13, 0.055),
                                   ((0.7, 0.7, 0.05), 0.12, 0.05), ((-0.7, 0.7, 0.05), 0.12, 0.05),
                                   ((0.5, 0.6, -0.6), 0.1, 0.045), ((-0.5, 0.6, -0.6), 0.1, 0.045),
                                   ((0.0, 0.2, -1.0), 0.09, 0.045)]):
        dd = unit(*d)
        base = H + dd * HR * 0.55 + (0, 0.02, 0)
        spike(k, f"crest{i}", base, base + dd * (L + 0.04), w, 0.02, ORANGE, "head")

    # The long thin black beak, held level and a little up
    B = H + (0, -0.085, -0.012)
    D = unit(0, -1, 0.12)
    k.add("beak", tube([B, B + D * 0.08, B + D * 0.17, B + D * 0.235], [0.024, 0.015, 0.008, 0.002], seg=20), BEAK,
          "head", fuse=False, surface="shell")

    # Feathers bristling over the trunk and hanging over the haunches, black ones behind between them
    for i, (d, L, w) in enumerate([((0.95, -0.2, -0.5), 0.16, 0.07), ((-0.95, -0.2, -0.5), 0.16, 0.07),
                                   ((0.8, 0.35, -0.65), 0.16, 0.07), ((-0.8, 0.35, -0.65), 0.16, 0.07),
                                   ((0.45, -0.8, -0.55), 0.13, 0.065), ((-0.45, -0.8, -0.55), 0.13, 0.065),
                                   ((0.0, -0.85, -0.6), 0.12, 0.06), ((0.9, 0.1, 0.25), 0.12, 0.06),
                                   ((-0.9, 0.1, 0.25), 0.12, 0.06), ((0.5, -0.75, 0.1), 0.11, 0.055),
                                   ((-0.5, -0.75, 0.1), 0.11, 0.055)]):
        dd = unit(*d)
        base = np.array([0, -0.03, 0.65]) + dd * np.array([0.13, 0.16, 0.12])
        spike(k, f"plume{i}", base, base + dd * L * 1.25 + (0, 0, -0.05), w, 0.022, ORANGE if i % 3 else ORANGE2, "body")
    for i, (d, L) in enumerate([((0.85, 0.1, -0.55), 0.15), ((-0.85, 0.1, -0.55), 0.15), ((0.6, -0.6, -0.55), 0.12),
                                ((-0.6, -0.6, -0.55), 0.12)]):
        dd = unit(*d)
        base = np.array([0, -0.03, 0.63]) + dd * np.array([0.11, 0.14, 0.1])
        spike(k, f"plumeblack{i}", base, base + dd * L + (0, 0, -0.02), 0.045, 0.018, BLACK, "body")

    # The jagged wings half raised and swept back: an orange starburst, a black one behind, a black bolt on the face
    ex, ez = a.wing_axes(ELEV, SWEEP, (0.3, 0.12, -1.0))
    n = np.cross(ex, ez)
    if n[1] < 0:
        n = -n
    root = np.array([0.12, -0.02, 0.75])
    k.add("wing", slab(z.star(WANG, WLEN, WWID, root=0.24, inner=0.12), root, ex, ez, 0.034), ORANGE, "wing{s}", fuse=False,
          mirror=True)
    k.add("wingblack", slab(z.star([w + 14 for w in WANG[:4]], [l * 0.82 for l in WLEN[:4]], [8] * 4, root=0.14,
                                   start=-25, end=90), root + n * 0.022, ex, ez, 0.02), BLACK, "wing{s}", fuse=False,
          mirror=True)
    k.add("bolt", slab([(p * 0.85 + 0.02, q * 0.85 + 0.005) for p, q in BOLT], root, ex, ez, 0.046), BLACK, "wing{s}", fuse=False, mirror=True)
    k.add("shoulder", ell(root + ex * 0.02, (0.055, 0.06, 0.055)), ORANGE, "wing{s}", mirror=True)

    # The tail: orange spikes back and down, black ones between
    TB = np.array([0, 0.17, 0.66])
    for i, (d, L) in enumerate([((0, 1, -0.25), 0.24), ((0.4, 1, -0.1), 0.2), ((-0.4, 1, -0.1), 0.2),
                                ((0.35, 0.9, -0.6), 0.17), ((-0.35, 0.9, -0.6), 0.17)]):
        dd = unit(*d)
        spike(k, f"tail{i}", TB, TB + dd * L, 0.07, 0.022, ORANGE, "tail")
    for i, (d, L) in enumerate([((0.2, 1, -0.45), 0.2), ((-0.2, 1, -0.45), 0.2)]):
        dd = unit(*d)
        spike(k, f"tailblack{i}", TB + (0, 0.02, 0), TB + dd * L, 0.045, 0.018, BLACK, "tail")
    k.add("rump", ell(TB + (0, -0.03, 0.0), (0.07, 0.06, 0.06)), ORANGE, "tail")

    legs(k)

    # The eyes: yellow under a fierce brow in a black mark that runs forward toward the beak
    de = unit(0.55, -0.8, 0.15)
    E = a.eye_at(H, HR, de)
    for s in (1, -1):
        m = np.array([s, 1, 1.0])
        k.decal(E * m + (0, -0.004, -0.002), de * m, (0.05, 0.03), BLACK, up=unit(-0.4 * s, 0, 1), eps=0.004, rings=8,
                segs=40)

    def look(u, v):
        c = np.tile(srgb(YELLOW), (len(u), 1))
        c[((u + 0.25) / 0.3) ** 2 + ((v + 0.1) / 0.45) ** 2 < 1] = srgb(PUPIL)
        return c
    def eyes(state):
        sp.lay_eyes(k, state, E, de, (0.032, 0.024), look, lambda u: 0.2 + 0.45 * u, lambda u: -0.1 + 0.6 * u,
                    srgb(BLACK), srgb(YELLOW))

    k.build(voxel=0.0072, band=0.006)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    galar(*args())
