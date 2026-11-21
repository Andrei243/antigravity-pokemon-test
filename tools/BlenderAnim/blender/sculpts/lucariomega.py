# Mega Lucario sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/lucariomega.py -- <the export's Lucario-Mega.glb> <out.glb>
# Lucario (lucario.py, which builds as it is read, so its way is copied here: the round blue head, the long black
# muzzle, the mask painted round the head's axis, the almond eyes with a red iris, the slim blue body).
# What the Mega changes, largest first, from its two pictures: four long black tendrils flowing back from the head,
# tipped in red, as long as a third of the body is tall; a great cream tail like a flame, broad and spiky; the cream
# chest fur grown into a spiky collar up to the cheeks and a jagged hem at the waist; black lightning stripes down
# the blue body and thighs; red forearms and paws and red feet, white spikes at the wrists and the ankles; black
# shoulder pads, one with a white spike out of it; taller ears; the eyes orange-red.
# lucariomegaz.py builds Mega Lucario Z with lucario_mega(src, out, z=True): a dark grey head and body with a pale
# grey mask over the eyes, blue eyes, teal limbs ringed in grey with grey discs at the knees and shins, a yellow
# collar of fur and a great yellow fall of it over the hips, two thin teal cords from the head, a long broad sash
# streaming from the right shoulder (grey, then teal with a zigzag), and a dark grey shark's tail.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, blade, polygon, srgb, inside, smoothstep, tri, unit, eye_look, args, hull,
                       slab, ring_band, lathe)


def cone(b, d, l, w, flat=0.6):
    """A hard spike: a hull from a diamond root to a point."""
    b = np.array(b, float); d = unit(*d)
    a1 = np.cross(d, (0, 0, 1.0))
    if np.linalg.norm(a1) < 1e-3:
        a1 = np.array([1.0, 0, 0])
    a1 /= np.linalg.norm(a1); a2 = np.cross(d, a1)
    return hull([b + a1 * w, b - a1 * w, b + a2 * w * flat, b - a2 * w * flat, b - d * w * 0.5, b + d * l])


def lucario_mega(src, out, z=False):
    k = Kit(src, coat="fur")
    if z:
        BLUE, BLACK, CREAM, LIMB, HAND, FOOT, MASK, IRIS = ("#454953", "#33363E", "#ECEA92", "#2894A0", "#2894A0",
                                                            "#2894A0", "#C2C9D2", "#2F8FE8")
    else:
        BLUE, BLACK, CREAM, LIMB, HAND, FOOT, MASK, IRIS = ("#3E9BE0", "#2A2D3A", "#EEDFA0", "#3E9BE0", "#D83A30",
                                                            "#D83A30", "#2A2D3A", "#EA5A28")
    WHITE, NOSE, GREY, REDTIP, TAN = "#F2F4F6", "#15161C", "#B9C1CB", "#D63A32", "#EFD98C"
    H = np.array([0.0, -0.005, 0.955])
    HR = np.array([0.106, 0.106, 0.104])        # a tenth larger than Lucario's: the face reads small

    # --- the head: round skull, a long muzzle, tall pointed ears --------------------------------------------------
    k.add("head", ell(H, HR), BLUE, "head")
    k.add("cheeks", ell((0, -0.035, 0.912), (0.09, 0.085, 0.072)), BLUE, "head")
    k.add("snout", ell((0, -0.135, 0.905), (0.04, 0.1, 0.04), rot=(-10, 0, 0)), BLACK, "head")
    k.add("jaw", ell((0, -0.1, 0.875), (0.042, 0.082, 0.029), rot=(-6, 0, 0)), BLACK, "head")
    k.add("ear", blade([(0.05, 0.012, 1.025), (0.078, 0.02, 1.1), (0.1, 0.03, 1.18), (0.115, 0.036, 1.275)],
                       [0.048, 0.04, 0.025, 0.004], (0, 1, 0), 0.45), BLACK if z else BLUE, "ear{s}", mirror=True)
    k.add("droot", tube([(0.05, 0.06, 0.96), (0.075, 0.09, 0.955)], [0.034, 0.03]), BLACK, "head", mirror=True)

    if z:
        # Two thin teal cords from the back of the head, up a little and then back, out and down to the hips
        cord = [(0.06, 0.08, 0.97), (0.1, 0.17, 0.99), (0.15, 0.27, 0.96), (0.19, 0.33, 0.9), (0.22, 0.36, 0.76),
                (0.27, 0.34, 0.66), (0.24, 0.31, 0.6)]
        k.add("cord", tube(cord, [0.022, 0.02, 0.019, 0.018, 0.018, 0.018, 0.017]), LIMB, "head", fuse=False,
              mirror=True)
    else:
        # Four long black tendrils flowing back and out from the head, wavy, their ends red
        for j, (dz, rch, r) in enumerate([(0.03, 0.0, 0.042), (-0.05, 0.03, 0.038)]):
            p = [(0.07, 0.07, 0.97 + dz), (0.11, 0.17, 0.97 + dz), (0.15, 0.26, 0.92 + dz),
                 (0.19 + rch, 0.35, 0.9 + dz), (0.23 + rch, 0.44, 0.84 + dz)]
            k.add(f"tendril{j}", tube(p, [r * 0.85, r, r * 0.95, r * 1.05, r * 1.1]), BLACK, "head", fuse=False,
                  mirror=True)
            q = np.array(p[-1]); d = unit(*(q - np.array(p[-2])))
            k.add(f"tendriltip{j}", ell(q + d * 0.025, (r * 1.25, r * 1.25, r * 1.25)), REDTIP, "head", fuse=False,
                  mirror=True)

    # --- the body --------------------------------------------------------------------------------------------
    k.add("neck", ell((0, 0.0, 0.81), (0.066, 0.066, 0.07)), CREAM, "body")
    k.add("torso", ell((0, 0.0, 0.67), (0.115, 0.09, 0.135)), CREAM, "body")
    k.add("waist", ell((0, 0.005, 0.54), (0.09, 0.078, 0.075)), BLUE, "body")
    k.add("hips", ell((0, 0.01, 0.46), (0.125, 0.1, 0.075)), BLUE, "body")
    CORE = lambda P: inside(np.hypot(P[:, 0] / 0.16, P[:, 1] / 0.14) - 1, 0.004)

    def front(P):
        return np.abs(np.arctan2(P[:, 0], -P[:, 1]))
    # the back blue; the cream at the front, its sides jagged
    def cream(P):
        return inside(front(P) - (1.35 + 0.15 * tri(P[:, 2] * 22)), 0.02)
    k.paint(lambda P: (1 - cream(P)) * inside(0.55 - P[:, 2], 0.003) * inside(P[:, 2] - 0.86, 0.003) * CORE(P), BLUE)
    # the hem of the chest fur, jagged points down over the blue belly
    def belly(P):
        a = front(P)
        bottom = 0.57 + 0.03 * tri(a * 2.2 + 0.5)
        return inside(P[:, 2] - bottom, 0.003) * inside(0.4 - P[:, 2], 0.003) * CORE(P)
    k.paint(belly, BLUE)
    if z:
        k.paint(lambda P: inside(P[:, 2] - 0.5, 0.003) * inside(0.4 - P[:, 2], 0.003) * inside(-0.02 - P[:, 1], 0.004) * CORE(P), BLACK)
    else:
        # black lightning stripes down the belly's sides
        def bolts(P):
            x = np.abs(P[:, 0]) - (0.075 + 0.016 * tri(P[:, 2] * 14))
            return inside(np.abs(x) - 0.011, 0.003) * inside(P[:, 2] - 0.6, 0.003) * inside(0.4 - P[:, 2], 0.003) \
                * inside(P[:, 1] + 0.0, 0.003) * CORE(P)
        k.paint(bolts, BLACK)
    # spiky fur: a collar up toward the cheeks and points down at the hem, loose so that they keep their points
    for j, (b, d, l) in enumerate([((0.07, -0.05, 0.83), (0.5, -0.4, 1), 0.07), ((0.1, -0.02, 0.8), (1, -0.2, 0.7), 0.07),
                                   ((0.12, 0.0, 0.76), (1, 0.0, 0.25), 0.06)]):
        k.add(f"collar{j}", cone(b, d, l, 0.028, 0.5), CREAM, "body", fuse=False, mirror=True, surface="fur")
    for j, x in enumerate((-0.07, -0.025, 0.025, 0.07)):
        k.add(f"hem{j}", cone((x, -0.075, 0.6), (x * 0.8, -0.35, -1), 0.06 if z else 0.045, 0.025, 0.45), CREAM,
              "body", fuse=False, surface="fur")
    if z:
        # a great fall of yellow fur over the hips, spiky at its foot
        k.add("fall", ell((0, -0.07, 0.47), (0.13, 0.065, 0.11), rot=(10, 0, 0)), CREAM, "body")
        k.paint(lambda P: inside(P[:, 1] + 0.03, 0.004) * inside(P[:, 2] - 0.6, 0.003), CREAM, on=["fall"])
        for j, x in enumerate((-0.09, -0.045, 0.0, 0.045, 0.09)):
            k.add(f"fallpt{j}", cone((x, -0.09, 0.42), (x * 1.4, -0.3, -1), 0.09 - abs(x) * 0.2, 0.03, 0.45), CREAM,
                  "body", fuse=False, surface="fur")
    else:
        k.add("spike", cone((0.0, -0.08, 0.72), (0, -1, 0.1), 0.07, 0.024), WHITE, "body", fuse=False, surface="shell")

    # arms: shoulder pads, the upper arm, the forearm and paw
    k.add("shoulder", ell((0.15, 0.0, 0.78), (0.058, 0.055, 0.056)), BLACK, "arm{s}", mirror=True)
    k.add("arm", tube([(0.15, 0.0, 0.77), (0.2, -0.02, 0.68), (0.23, -0.045, 0.62)], [0.042, 0.038, 0.036]), LIMB,
          "arm{s}", mirror=True)
    k.add("fore", tube([(0.23, -0.045, 0.625), (0.25, -0.075, 0.55), (0.256, -0.09, 0.5)], [0.038, 0.041, 0.044]),
          HAND, "arm{s}", mirror=True)
    k.add("paw", ell((0.258, -0.1, 0.465), (0.052 if z else 0.046, 0.052 if z else 0.047, 0.05)), HAND, "arm{s}",
          mirror=True)
    # a crisp line at the elbow between the blue upper arm and the red forearm
    k.paint(lambda P: inside(P[:, 2] - 0.615, 0.003) * inside(0.75 - P[:, 2], 0.003), HAND, on=["arm", "fore"])
    k.paint(lambda P: inside(0.615 - P[:, 2], 0.003), LIMB, on=["arm", "fore"])
    if z:
        k.add("wristring", ring_band((0.236, -0.058, 0.6), (0.02, -0.03, -0.075), 0.041, 0.025, 0.007), GREY,
              "arm{s}", fuse=False, mirror=True, surface="shell")
    else:
        k.add("shspike", cone((0.19, 0.0, 0.82), (1, -0.2, 0.6), 0.07, 0.024), WHITE, "arm{s}", fuse=False,
              mirror=True, surface="shell")
        k.add("pawspike", cone((0.29, -0.07, 0.53), (1, 0.3, 0.35), 0.075, 0.022), WHITE, "arm{s}", fuse=False,
              mirror=True, surface="shell")

    # legs: blue thighs fused into the hips, black (Z: teal) shins, red (Z: teal) feet
    k.add("thigh", ell((0.09, 0.0, 0.39), (0.082, 0.088, 0.11)), BLUE if not z else BLACK, "leg{s}", mirror=True)
    k.add("leg", tube([(0.095, 0.0, 0.35), (0.11, 0.005, 0.24), (0.115, 0.015, 0.13), (0.115, 0.01, 0.06)],
                      [0.062, 0.05, 0.043, 0.04]), BLACK if not z else LIMB, "leg{s}", mirror=True)
    k.add("foot", ell((0.117, -0.045, 0.032), (0.05, 0.09, 0.033)), FOOT, "leg{s}", mirror=True)
    for i, x in enumerate((-0.025, 0.0, 0.025)):
        k.add(f"toe{i}", ell((0.117 + x, -0.125, 0.024), (0.018, 0.022, 0.021)), FOOT, "leg{s}", mirror=True)
    if z:
        k.paint(lambda P: inside(0.3 - P[:, 2], 0.003) * inside(P[:, 2] - 0.45, 0.003), BLACK, on=["thigh", "leg"])
        k.add("kneedisc", lathe((0.13, -0.045, 0.27), unit(0.25, -1, 0.0),
                                [(0.0, 0.0), (0.03, 0.0), (0.032, 0.008), (0.0, 0.012)]), GREY, "leg{s}",
              fuse=False, mirror=True, surface="shell")
        k.add("shindisc", lathe((0.145, -0.02, 0.15), unit(0.7, -0.7, 0.0),
                                [(0.0, 0.0), (0.025, 0.0), (0.027, 0.007), (0.0, 0.01)]), GREY, "leg{s}",
              fuse=False, mirror=True, surface="shell")
    else:
        # blue down to the knee with a black bolt down the thigh's front; red up the ankle
        k.paint(lambda P: inside(P[:, 2] - 0.27, 0.003) * inside(0.45 - P[:, 2], 0.003), BLUE, on=["thigh", "leg"])
        def thighbolt(P):
            x = np.abs(P[:, 0]) - (0.1 + 0.014 * tri(P[:, 2] * 12))
            return inside(np.abs(x) - 0.01, 0.003) * inside(P[:, 1] + 0.03, 0.003) * inside(P[:, 2] - 0.44, 0.003) \
                * inside(0.27 - P[:, 2], 0.003)
        k.paint(thighbolt, BLACK, on=["thigh", "leg"])
        k.paint(lambda P: inside(P[:, 2] - (0.13 + 0.03 * tri(np.arctan2(P[:, 1], np.abs(P[:, 0]) - 0.115) * 0.5)), 0.003),
                FOOT, on=["leg"])
        for j, (b, d) in enumerate([((0.125, 0.04, 0.15), (0.3, 1, 0.25)), ((0.135, 0.035, 0.09), (0.45, 1, 0.1))]):
            k.add(f"ankle{j}", cone(b, d, 0.06, 0.02), WHITE, "leg{s}", fuse=False, mirror=True, surface="shell")

    # the tail
    if z:
        # a dark grey shark's tail, back, down and out to the +X side so that its face shows
        TL = [(0.0, 0.03), (0.12, 0.0), (0.3, -0.08), (0.42, -0.02), (0.34, -0.12), (0.4, -0.25), (0.2, -0.1),
              (0.0, -0.03)]
        k.add("tail", slab(TL, (0, 0.09, 0.47), unit(0.5, 0.85, 0), (0, 0, 1), 0.03), BLACK, "tail", fuse=False,
              surface="fur")
        k.add("tailroot", tube([(0, 0.06, 0.48), (0.03, 0.13, 0.46)], [0.035, 0.026]), BLACK, "tail")
    else:
        # a great cream tail like a flame: back and out to the +X side, broad, its spikes loose
        TD = unit(0.5, 0.85, -0.15)
        T0 = np.array([0.0, 0.08, 0.47])
        k.add("tail", ell(T0 + TD * 0.15, (0.1, 0.2, 0.11), rot=(8, 0, -math.degrees(math.atan2(TD[0], TD[1])))),
              TAN, "tail")
        k.add("tailroot", tube([(0, 0.06, 0.48), tuple(T0 + TD * 0.06)], [0.04, 0.045]), TAN, "tail")
        side = unit(TD[1], -TD[0], 0)
        for j, (t, sd, up, l) in enumerate([(0.36, 0.0, 0.0, 0.13), (0.3, 0.6, 0.3, 0.11), (0.3, -0.6, 0.2, 0.11),
                                            (0.2, 0.9, 0.6, 0.09), (0.2, -0.9, 0.5, 0.09), (0.26, 0.0, 0.9, 0.08)]):
            b = T0 + TD * t
            d = TD + side * sd + np.array([0, 0, up])
            k.add(f"tailspike{j}", cone(b, d, l * 1.35, 0.06, 1.0), TAN, "tail", fuse=False, surface="fur")

    if z:
        # the sash from the right shoulder: grey near it, then teal, streaming back, out and down
        EX, EZ = unit(0.55, 0.83, 0), unit(0, 0, 1)
        O = np.array([0.13, 0.05, 0.8])
        def band(t0, t1, n=9):
            t = np.linspace(t0, t1, n)
            a = 0.6 * t
            b = -0.32 * t + 0.03 * np.sin(math.pi * 2 * t)
            w = 0.05 + 0.025 * t
            up = [(a[i], b[i] + w[i]) for i in range(n)]
            dn = [(a[i], b[i] - w[i]) for i in range(n)][::-1]
            return up + dn
        k.add("sash", slab(band(0.0, 0.45), O, EX, EZ, 0.03), GREY, "scarf", fuse=False, surface="cloth")
        k.add("sashend", slab(band(0.4, 1.0) + [], O, EX, EZ, 0.036), LIMB, "scarf", fuse=False, surface="cloth")
        # a pale zigzag along the teal, thicker than it so that it shows on both faces
        for j in range(3):
            t = 0.52 + j * 0.15
            a, b = 0.6 * t, -0.32 * t + 0.03 * math.sin(math.pi * 2 * t)
            zz = [(a - 0.03, b + 0.03), (a, b - 0.03), (a + 0.03, b + 0.03), (a + 0.03, b + 0.045), (a, b - 0.012),
                  (a - 0.03, b + 0.045)]
            k.add(f"sashzig{j}", slab(zz, O, EX, EZ, 0.044), GREY, "scarf", fuse=False, surface="cloth")
        k.add("sashknot", ell((0.13, 0.03, 0.8), (0.045, 0.045, 0.045)), GREY, "scarf")

    # --- the mask: muzzle black, a band across the eyes back to the tendrils' roots -----------------------------
    def mask(P):
        q = P - H
        a = np.arctan2(q[:, 0], -q[:, 1])
        s = np.abs(a) * 0.1
        top = np.interp(s, [0.0, 0.012, 0.035, 0.08, 0.15, 0.3], [0.94, 0.95, 0.99, 0.995, 0.98, 0.965])
        bot = np.interp(s, [0.0, 0.05, 0.085, 0.13, 0.3], [0.85, 0.85, 0.862, 0.91, 0.91])
        if z:
            bot = np.maximum(bot, 0.905)
        near = (np.linalg.norm(q, axis=1) < 0.21) & (np.abs(a) < 1.85)
        return inside(P[:, 2] - top, 0.003) * inside(bot - P[:, 2], 0.003) * near
    k.paint(mask, MASK)
    if z:
        k.paint(lambda P: inside(0.9 - P[:, 2], 0.003) * inside(P[:, 1] + 0.11, 0.003), BLACK, on=["snout", "jaw"])
    k.paint(lambda P: inside(P[:, 2] - 0.86, 0.003) * inside(front(P) - 0.9, 0.01), CREAM, on=["cheeks", "neck"])
    k.paint(lambda P: inside(1.35 - front(P), 0.02) * inside(P[:, 2] - 0.905, 0.003) * inside(0.74 - P[:, 2], 0.003)
            * inside(np.abs(P[:, 0]) - 0.12, 0.003), BLUE)
    k.paint(lambda P: inside(np.linalg.norm(P - np.array([0, -0.235, 0.92]), axis=1) - 0.017, 0.003), NOSE,
            on=["snout"])

    # --- the eyes: Lucario's narrow slanted almonds, the iris the form's colour ---------------------------------
    CB, CW, CR, CD, CK = srgb(MASK), np.array([1.0, 1.0, 1.0]), srgb(IRIS), srgb(IRIS) * 0.45, srgb(NOSE)
    def eye(u, v):
        c = np.tile(CW, (len(u), 1))
        r = ((u - 0.02) / 0.52) ** 2 + ((v - 0.02) / 0.68) ** 2
        c[r < 1] = CD
        c[r < 0.68] = CR
        c[((u - 0.02) / 0.15) ** 2 + ((v - 0.02) / 0.5) ** 2 < 1] = CK
        c[((u + 0.14) / 0.11) ** 2 + ((v - 0.24) / 0.13) ** 2 < 1] = CW
        return c
    EYE = [(-1.0, -0.12), (-0.55, 0.16), (0.0, 0.42), (0.55, 0.62), (0.95, 0.58), (1.0, 0.4),
           (0.6, -0.28), (0.0, -0.52), (-0.55, -0.42)]
    LINE = srgb("#4A4E58") if z else srgb("#8A9AB0")
    def eyes(state):
        look = eye_look(state, eye, CB, LINE, line=0.16)
        for s in (1, -1):
            dn = unit(0.5 * s, -0.84, 0.18)
            e = H + np.array([0, 0, -0.005]) + dn * HR
            pts = [(u * s, v) for u, v in EYE]
            lk = look if s > 0 else (lambda f: (lambda u, v: f(-u, v)))(look)
            k.decal(e, dn, (0.038, 0.034), lk, outline=polygon(pts), eps=0.004, rings=20, segs=56)

    k.build(voxel=0.0075)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    src, dst = args()
    lucario_mega(src, dst)
