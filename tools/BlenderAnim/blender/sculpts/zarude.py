# Zarude sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/zarude.py -- <the export's Zarude.glb> <out.glb>
#
# The design, from the references: a black ape of the jungle crouched to spring, the trunk leaning forward; a grey
# chest and belly with a black mark like a spine and ribs down its middle; a black head with great swept-up pointed
# tufts for ears, spiky fur on the crown and jagged fur at the cheeks; red eyes with black pupils in white angular
# rims, a pale grey muzzle standing forward with a dark nose, an open snarl with two white fangs; a shaggy black ruff
# round the neck; very long arms (one raised in the pictures, carried forward and down here, the claws at knee height)
# each wound at the forearm with thick lime vines in four coils; huge dark hands with long curved claws and pale
# palms; a green thorn at each shoulder's back and at each heel; crouched legs, thick thighs level forward over short
# sloped shins, long dark feet with long toes; a long thin tail curling up behind, run out to +X, a tuft at its tip.
# Reference ratios (3D picture, the raised arm left out): head with its tufts 0.3 of the height, head width / chest
# width 0.95, vine coils' width / head width 0.8, hand length / height 0.25.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, lathe, outward, srgb, inside, unit, polygon, eye_look, args, sibling

kb = sibling("kubfu")
hit, sk = kb.hit, kb.sk

BLACK, GREY, MUZZLE, PALM = "#2A2B33", "#6B6D76", "#B4B6BE", "#C9CDD3"
VINE, VINE_D, THORN = "#7CC43A", "#55A02C", "#6DBB34"
HANDS, CLAW = "#232838", "#3A3E4E"
WHITE, RED, MOUTH, FANG = srgb("#F2F2EE"), srgb("#E2381E"), "#3A1E22", "#F4F2EA"
INK = srgb("#121216")

H = np.array([0.0, -0.13, 0.665])            # the head's middle (its bone is at 0, -0.06, 0.56)
HR = np.array([0.098, 0.092, 0.098])


def torus(c, d, R, r, seg=36, rseg=14):
    """A ring (a coil of vine) round axis d: a lathe of a circle of radius r at R from the axis."""
    prof = [(R + r * math.cos(a), r * math.sin(a)) for a in np.linspace(0, 2 * math.pi, rseg + 1)]
    return lathe(c, d, prof, seg=seg)


def zarude(src, out_path):
    k = Kit(src, coat="fur")

    # --- the trunk leaning forward: the chest out over the belly; grey in front with the black spine and ribs ----
    k.add("chest", ell((0, -0.06, 0.53), (0.12, 0.105, 0.115), rot=(-15, 0, 0)), BLACK, "body")
    k.add("belly", ell((0, 0.0, 0.405), (0.1, 0.095, 0.105)), BLACK, "body")
    k.add("hips", ell((0, 0.035, 0.34), (0.105, 0.09, 0.07)), BLACK, "body")
    def front(P):
        x, z = np.abs(P[:, 0]), P[:, 2]
        cut = -0.035 - 0.25 * (z - 0.45)          # the grey front, forward of a line slanting with the lean
        return (inside(P[:, 1] - cut, 0.003) * inside(x - (0.085 - 0.15 * np.abs(z - 0.5)), 0.003)
                * inside(z - 0.62, 0.003) * inside(0.33 - z, 0.003))
    k.paint(front, GREY, on=["chest", "belly"])
    def spine(P):
        x, z = np.abs(P[:, 0]), P[:, 2]
        m = inside(x - 0.009, 0.002) * inside(z - 0.6, 0.003) * inside(0.34 - z, 0.003)
        for zr in (0.58, 0.53, 0.48, 0.43):
            m = np.maximum(m, inside(np.abs(z - (zr - 0.45 * x)) - 0.009, 0.002) * inside(x - 0.07, 0.003))
        return m * (P[:, 1] < -0.02)
    k.paint(spine, BLACK, on=["chest", "belly"])

    # --- the head: black; a pale grey muzzle standing forward, its nose; the snarl and two fangs ----------------
    HEAD = ell(H, HR)
    k.add("head", HEAD, BLACK, "head")
    k.add("neckfill", ell((0, -0.09, 0.6), (0.085, 0.08, 0.07)), BLACK, "head")
    MZ = H + (0, -0.085, -0.045)
    MZS = ell(MZ, (0.05, 0.045, 0.042))
    k.add("muzzle", MZS, MUZZLE, "head")
    NC = hit(MZS, MZ + (0, -0.5, 0.022), (0, 1, 0))
    k.decal(NC, unit(0, -1, 0.3), (0.022, 0.01), "#1A1A20", eps=0.003, rings=8, segs=44,
            outline=polygon([(-1, 0.3), (-0.6, 0.9), (0.6, 0.9), (1, 0.3), (0.3, -0.9), (-0.3, -0.9)]))
    MC = hit(MZS, MZ + (0, -0.5, -0.016), (0, 1, 0))
    k.decal(MC, unit(0, -1, -0.15), (0.034, 0.016), MOUTH, eps=0.003, rings=10, segs=56,
            outline=polygon([(-1, 0.5), (0, 0.85), (1, 0.5), (0.7, -0.6), (0, -0.9), (-0.7, -0.6)]))
    for s in (1, -1):
        p = MC + np.array([0.018 * s, -0.002, 0.012])
        k.add("fang" + ("R" if s > 0 else "L"), hull([p + (-0.006, 0, 0), p + (0.006, 0, 0), p + (0, 0.008, 0),
                                                      p + (0.002 * s, -0.004, -0.02)]), FANG, "head", fuse=False)

    # the great pointed tufts swept up and out for ears, the spiky crown, jagged fur at the cheeks
    kb.spikes(k, H, [((0.075, 0.01, 0.055), (1, 0.15, 0.62), 0.165, 0.062, 0.022),
                     ((0.088, 0.02, 0.02), (1, 0.3, 0.2), 0.11, 0.045, 0.019),
                     ((0.065, 0.045, 0.065), (0.75, 0.6, 0.6), 0.1, 0.04, 0.018),
                     ((0.085, -0.02, -0.04), (0.9, 0.0, -0.4), 0.07, 0.03, 0.015),
                     ((0.07, -0.05, -0.07), (0.6, -0.2, -0.8), 0.06, 0.026, 0.014)], BLACK, tag="tuft", mirror=True)
    kb.spikes(k, H, [((0.0, 0.03, 0.093), (0.05, 0.75, 1), 0.065, 0.034, 0.016),
                     ((0.0, 0.08, 0.06), (0.0, 1, 0.35), 0.065, 0.034, 0.016)], BLACK, tag="crown")

    # --- a shaggy black ruff round the neck and over the shoulders, pointing down and out -----------------------
    for i, a in enumerate(np.radians(np.linspace(-150, 150, 11))):
        o = np.array([math.sin(a), math.cos(a), 0.0])
        root = np.array([0, -0.04, 0.6]) + o * np.array([0.1, 0.085, 0])
        sk.spike(k, f"ruff{i}", root, o + (0, 0, -0.9 - 0.3 * (i % 2)), 0.07 + 0.02 * (i % 2), 0.032, 0.015, "body",
                 BLACK, mid=0.3)

    # --- very long arms carried forward and down, vines wound round each forearm, huge clawed hands -------------
    for s, bone in ((1, "armR"), (-1, "armL")):
        m = np.array([s, 1, 1.0])
        S = np.array((0.125, -0.04, 0.545)) * m
        E = np.array((0.175, -0.11, 0.42)) * m
        W = np.array((0.15, -0.255, 0.31)) * m
        k.add("shoulder" + bone[-1], ell(S + np.array([0.01 * s, 0.01, 0]), (0.065, 0.065, 0.065)), BLACK, bone)
        k.add("upper" + bone[-1], tube([S, (S + E) / 2 + np.array([0.01 * s, 0, 0]), E], [0.05, 0.047, 0.044]),
              BLACK, bone)
        k.add("fore" + bone[-1], tube([E, W], [0.044, 0.04]), BLACK, bone)
        d = unit(*(W - E))
        for j, t in enumerate(np.linspace(0.3, 0.92, 4)):
            c = E + (W - E) * t
            R = 0.05 + 0.006 * math.sin(math.pi * (t - 0.2) / 0.8)
            k.add(f"vine{bone[-1]}{j}", torus(c, d, R, 0.024), VINE if j % 2 == 0 else VINE_D, bone, fuse=False,
                  surface="leaf")
        # a green thorn at the shoulder's back
        sk.spike(k, f"thorn{bone[-1]}", S + np.array([0.03 * s, 0.05, 0.03]), (0.3 * s, 0.6, 0.75), 0.075, 0.022,
                 0.013, bone, THORN, surface="leaf")
        # the huge hand: a dark palm, its pale inside, long fingers curling down, long curved claws
        hd = unit(0.05 * s, -0.75, -0.65)                 # the hand's way: forward and down
        P = W + hd * 0.05
        PS = ell(P, (0.066, 0.056, 0.05))
        k.add("palm" + bone[-1], PS, HANDS, bone)
        pn = unit(-0.2 * s, 0.55, -0.8)
        # its pale inside: paint on the palm alone, on the side turned down and back (the hand hangs forward)
        k.paint(lambda Q, P=P, pn=pn: inside(np.linalg.norm(Q - P, axis=1) - 0.075, 0.003)
                * inside(0.025 - (Q - P) @ pn, 0.003), PALM, on=["palm" + bone[-1]])
        for i, ang in enumerate((-32, -11, 11, 32)):
            a = math.radians(ang)
            side = np.array([math.sin(a) * s, 0, 0]) - hd * (1 - math.cos(a)) * 0.6
            base = P + side * 0.06 + hd * 0.035
            mid = base + hd * 0.055 + side * 0.02
            tip = mid + np.array([0, -0.012, -0.05])
            k.add(f"finger{bone[-1]}{i}", tube([base, mid, tip], [0.021, 0.018, 0.015]), HANDS, bone)
            cd = unit(*(tip - mid))
            k.add(f"claw{bone[-1]}{i}", hull([tip + (0.009, 0, 0), tip - (0.009, 0, 0), tip + (0, -0.01, 0.003),
                                               tip + (0, 0.009, 0.0), tip + cd * 0.05 + (0, 0.018, 0)]),
                  CLAW, bone, fuse=False, surface="shell")

    # --- crouched legs: thick thighs level forward over short sloped shins, long feet, a thorn at the heel --------
    for s, bone in ((1, "legR"), (-1, "legL")):
        m = np.array([s, 1, 1.0])
        k.add("haunch" + bone[-1], ell(np.array((0.1, 0.04, 0.32)) * m, (0.075, 0.09, 0.085)), BLACK, bone)
        K = np.array((0.155, -0.08, 0.26)) * m
        A = np.array((0.15, 0.01, 0.07)) * m
        k.add("thigh" + bone[-1], tube([np.array((0.1, 0.04, 0.33)) * m, K], [0.068, 0.055]), BLACK, bone)
        k.add("shin" + bone[-1], tube([K, A], [0.05, 0.04]), BLACK, bone)
        F = np.array((0.15, -0.045, 0.024)) * m
        k.add("foot" + bone[-1], ell(F, (0.045, 0.085, 0.026)), HANDS, bone)
        for i, dx in enumerate((-0.028, -0.009, 0.009, 0.028)):
            b0 = F + np.array([dx * s, -0.065, 0.0])
            tip = b0 + np.array([dx * s * 0.4, -0.04, -0.012])
            k.add(f"toe{bone[-1]}{i}", tube([b0, tip], [0.013, 0.011]), HANDS, bone)
            k.add(f"toeclaw{bone[-1]}{i}", hull([tip + (0.006, 0, 0.003), tip - (0.006, 0, -0.003),
                                                  tip + (0, 0, 0.01), tip + (0, -0.03, -0.01)]),
                  CLAW, bone, fuse=False, surface="shell")
        sk.spike(k, f"heel{bone[-1]}", A + np.array([0, 0.03, -0.01]), (0.2 * s, 1, 0.5), 0.07, 0.02, 0.012, bone,
                 THORN, surface="leaf")

    # --- the long thin tail curling up behind, run out to +X, a tuft at its tip ---------------------------------
    T = [np.array(p) for p in [(0.0, 0.1, 0.33), (0.04, 0.22, 0.22), (0.11, 0.32, 0.15), (0.19, 0.38, 0.18),
                               (0.22, 0.37, 0.26), (0.19, 0.33, 0.3)]]
    k.add("tail", tube(T, [0.024, 0.02, 0.018, 0.017, 0.017, 0.016]), BLACK, "tail")
    for i, d in enumerate([(-0.4, -0.5, 0.5), (0.3, -0.3, 0.8), (-0.6, 0.0, -0.2)]):
        sk.spike(k, f"tailtuft{i}", T[-1], d, 0.05, 0.022, 0.012, "tail", BLACK, mid=0.3)

    # --- the eyes: red with a black pupil, in white angular rims slanting up to the outer corners --------------
    OUT = [(-1.0, -0.35), (-0.45, -0.85), (0.5, -0.75), (1.0, 0.05), (0.85, 0.8), (0.1, 0.45), (-0.75, 0.15)]
    def open_look(u, v):
        c = np.tile(WHITE, (len(u), 1))
        c[(u / 0.62) ** 2 + ((v + 0.08) / 0.48) ** 2 < 1] = RED
        c[((u + 0.05) / 0.2) ** 2 + ((v + 0.08) / 0.32) ** 2 < 1] = INK
        return c
    de = unit(0.4, -0.9, 0.08)
    E = hit(HEAD, H + (0.043, -0.5, 0.018), (0, 1, 0))
    def lay(state):
        look = eye_look(state, open_look, WHITE, INK, line=0.2)
        for s in (1, -1):
            o = polygon(OUT if s > 0 else [(-a, b) for a, b in OUT])
            col = look if s > 0 else (lambda u, v, look=look: look(-u, v))
            k.decal(E * np.array([s, 1, 1]), de * np.array([s, 1, 1]), (0.03, 0.024), col, outline=o,
                    eps=0.0025, rings=20, segs=56)

    k.build(voxel=0.0068, smooth=4, band=0.004, wband=0.05)
    k.eye_states(lay)
    k.export(out_path)


if __name__ == "__main__":
    zarude(*args())
