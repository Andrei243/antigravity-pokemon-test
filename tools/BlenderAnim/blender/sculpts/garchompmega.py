# Mega Garchomp sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/garchompmega.py -- <the export's Garchomp-Mega.glb> <out.glb>
# Garchomp (garchomp.py, which builds as it is read, so its way is copied here: the hammerhead with its drooping
# horns, the open jaws and white teeth, the yellow star, the slit eyes cut by the brow, the red throat and chest).
# What the Mega changes, largest first, from its two pictures: its arms have become two great scythes (a crescent
# from the wrist up past the shoulder and hooking out and down, as long as half the body is tall, red with a navy
# back along the forearm's side); white teeth of spikes along the sides of the red chest; a yellow kite on the belly
# in place of the band; a red spike out of each knee and a red ring on each thigh; a broader tail fin.
# garchompmegaz.py builds Mega Garchomp Z with garchomp_mega(src, out, z=True): darker navy, two yellow chevrons
# on an orange chest, a red lance out of each horn's red end, the arms raised under great orange wing-blades edged
# with white spikes, navy fins at the shoulders, gold bands on the shins and the tail.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from mathutils import Euler
from sculptkit import (Kit, ell, tube, blade, srgb, inside, smoothstep, unit, polygon, eye_look, args, slab, hull)


def spike(b, d, l, w):
    b = np.array(b, float); d = unit(*d)
    return blade(np.array([b - d * 0.02, b + d * l * 0.5, b + d * l]), [w, w * 0.62, 0.002], (0, 1, 0), 0.5)


def cone(b, d, l, w, flat=0.6):
    """A hard spike: a hull from a diamond root to a point (a claw, a lance, a tooth of a saw)."""
    b = np.array(b, float); d = unit(*d)
    a1, a2 = np.cross(d, (0, 0, 1.0)), None
    if np.linalg.norm(a1) < 1e-3:
        a1 = np.array([1.0, 0, 0])
    a1 /= np.linalg.norm(a1); a2 = np.cross(d, a1)
    pts = [b + a1 * w, b - a1 * w, b + a2 * w * flat, b - a2 * w * flat, b - d * w * 0.5, b + d * l]
    return hull(pts)


def crescent(centre, widths, side):
    """A crescent's outline (a, b) from its centre line and widths; side 1 is the convex (left of travel) side."""
    C = np.array(centre, float); W = np.array(widths, float)
    T = np.gradient(C, axis=0); T /= np.linalg.norm(T, axis=1, keepdims=True)
    N = np.c_[-T[:, 1], T[:, 0]]
    left = C + N * (W * side[0])[:, None]
    right = C - N * (W * side[1])[:, None]
    return [tuple(p) for p in left] + [tuple(p) for p in right[::-1][1:-1]], C, N


def garchomp_mega(src, out, z=False):
    k = Kit(src, coat="scales")
    if z:
        NAVY, DARK, RED, YELLOW = "#3A3566", "#25213F", "#E25A30", "#F6C23A"
    else:
        NAVY, DARK, RED, YELLOW = "#4F5594", "#363B6E", "#D8483B", "#F5C22E"
    WHITE, MOUTH, BLADE = "#F4F5F7", "#8C2630", ("#E0552E" if z else "#D9452F")

    # The body: Garchomp's broad chest, slim waist and wide hips
    TV0, TF0 = tube([(0, 0.01, 0.36), (0, 0.01, 0.46), (0, 0.015, 0.58), (0, 0.02, 0.71), (0, 0.02, 0.8)],
                    [0.16, 0.15, 0.12, 0.145, 0.11])
    TV0[:, 1] = (TV0[:, 1] - 0.015) * 0.82 + 0.015
    k.add("torso", (TV0, TF0), NAVY, "body")
    k.add("hips", ell((0, 0.01, 0.4), (0.18, 0.14, 0.13)), NAVY, "body")
    k.add("neck", tube([(0, 0.015, 0.76), (0, 0.0, 0.86), (0, -0.035, 0.93)], [0.085, 0.066, 0.06]), NAVY, "head")

    # The head, as Garchomp's
    k.add("crown", ell((0, -0.02, 1.03), (0.1, 0.12, 0.075), rot=(-5, 0, 0)), NAVY, "head")
    k.add("snout", ell((0, -0.15, 1.005), (0.062, 0.13, 0.048), rot=(-4, 0, 0)), NAVY, "head")
    k.add("brow", ell((0.0, -0.1, 1.045), (0.08, 0.07, 0.028), rot=(8, 0, 0)), NAVY, "head")
    k.add("inside", ell((0, -0.15, 0.952), (0.044, 0.07, 0.03), rot=(14, 0, 0)), MOUTH, "jaw")
    JC, JR, JROT = np.array([0, -0.125, 0.9]), np.array([0.052, 0.11, 0.028]), (28, 0, 0)
    k.add("jaw", ell(JC, JR, rot=JROT), RED, "jaw")
    HA, HM, HB = np.array([0.04, 0.0, 1.05]), np.array([0.15, 0.03, 1.08]), np.array([0.235, 0.07, 1.045])
    k.add("horn", tube([HA, HM, HB], [0.055, 0.058, 0.052]), NAVY, "head", mirror=True)
    HD = unit(*(HB - HM))
    if z:
        # The horns' round ends red, and a red lance out of each, up and out
        k.paint(lambda P: inside(0.205 - np.abs(P[:, 0]), 0.004) * inside(P[:, 2] - 1.16, 0.004), RED, on=["horn"])
        k.add("lance", cone(HB + np.array([0.035, 0.0, 0.01]), (1, 0.15, 0.62), 0.27, 0.03, 0.8), RED, "head",
              fuse=False, mirror=True, surface="shell")
    else:
        k.paint(lambda P: inside(np.abs((np.c_[np.abs(P[:, 0]), P[:, 1:]] - HB) @ HD + 0.035) - 0.009, 0.003)
                * smoothstep(0.16, 0.18, np.abs(P[:, 0])), DARK, on=["horn"])

    # The front: red from the jaw down the throat over the chest to the waist
    def ycl(zz):
        return np.interp(zz, [0.4, 0.7, 0.85, 0.95], [0.0, 0.02, 0.0, -0.06])
    def front_angle(P):
        return np.abs(np.arctan2(P[:, 0], -(P[:, 1] - ycl(P[:, 2]))))
    def red_front(P):
        a = front_angle(P)
        reach = np.interp(P[:, 2], [0.42, 0.5, 0.6, 0.7, 0.76, 0.84, 0.9], [0.55, 0.7, 0.85, 1.2, 1.4, 0.85, 0.85])
        return inside(a - reach, 0.006) * inside(0.43 - P[:, 2], 0.003) * inside(P[:, 2] - 0.9, 0.004)
    k.paint(red_front, RED, on=["neck", "torso", "hips"])
    if z:
        # Two yellow chevrons on the belly, pointing down
        def chevrons(P):
            a = front_angle(P)
            m = 0
            for zc in (0.585, 0.505):
                m = m + inside(np.abs(P[:, 2] - (zc + 0.075 * np.minimum(a, 0.7))) - 0.017, 0.003)
            return np.clip(m, 0, 1) * inside(a - 0.68, 0.006) * inside(0.45 - P[:, 2], 0.003)
        k.paint(chevrons, YELLOW, on=["torso", "hips"])
    else:
        # A yellow kite on the belly, its top point up into the red, its lower one down onto the hips
        def kite(P):
            a = front_angle(P)
            top = 0.635 - 0.15 * np.clip(a / 0.62, 0, 1)
            bot = 0.44 + 0.05 * np.clip(a / 0.62, 0, 1)
            return inside(a - 0.62, 0.006) * inside(P[:, 2] - top, 0.003) * inside(bot - P[:, 2], 0.003)
        k.paint(kite, YELLOW, on=["torso", "hips"])
    def off_jaw(P):
        mrot = np.array(Euler([math.radians(a) for a in JROT]).to_matrix())
        q = (P - JC) @ mrot / JR
        e = np.sqrt((q ** 2).sum(1)) - 1
        above = np.maximum(inside(0.92 - P[:, 2], 0.004),
                           inside(0.87 - P[:, 2], 0.004) * inside(-0.06 - P[:, 1], 0.004))
        return inside(0.08 - e, 0.01) * above
    k.paint(off_jaw, NAVY, on=["crown", "snout", "brow", "neck"])

    # White teeth of a saw down each side of the red chest, pointing out and down
    for j, (zz, x, l) in enumerate([(0.74, 0.135, 0.06), (0.67, 0.13, 0.055), (0.6, 0.115, 0.05)]):
        k.add(f"chestspike{j}", cone((x, -0.085, zz), (1, -0.55, -0.35), l, 0.017), WHITE, "body", fuse=False,
              mirror=True, surface="shell")

    # The legs: heavy thighs fused into the hips, the knee forward, a short shin, a broad foot
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"thigh{side}", ell(np.array((0.155, -0.02, 0.32)) * m, (0.115, 0.135, 0.155), rot=(-15, 0, 0)), NAVY,
              f"leg{side}")
        k.add(f"shin{side}", tube(np.array([(0.165, -0.05, 0.21), (0.165, 0.02, 0.13), (0.165, 0.04, 0.07)]) * m,
                                  [0.072, 0.058, 0.05]), NAVY, f"leg{side}")
        k.add(f"foot{side}", ell(np.array((0.165, -0.045, 0.045)) * m, (0.078, 0.12, 0.046)), NAVY, f"leg{side}")
        for j, dx in enumerate((-0.04, 0.0, 0.04)):
            b = np.array((0.165 + dx, -0.14, 0.032))
            k.add(f"toe{side}{j}", blade(np.array([b, b + (dx * 0.3, -0.03, -0.006),
                                                   b + (dx * 0.45, -0.055, -0.016)]) * m,
                                         [0.019, 0.012, 0.003], (0, 0, 1), 0.55), WHITE, f"leg{side}", fuse=False,
                  surface="shell")
        if z:
            for j, (b, d, l) in enumerate([((0.255, -0.05, 0.38), (1, -0.35, -0.4), 0.08)]):
                k.add(f"spike{side}{j}", spike(np.array(b) * m, np.array(d) * m, l, 0.022), WHITE, f"leg{side}",
                      fuse=False, surface="shell")
        else:
            # A red spike out of the knee, forward and out
            k.add(f"knee{side}", cone(np.array((0.22, -0.13, 0.3)) * m, np.array((0.55, -1, -0.15)) * m, 0.11, 0.03),
                  RED, f"leg{side}", fuse=False, surface="shell")
            k.add(f"spike{side}0", spike(np.array((0.26, -0.03, 0.4)) * m, np.array((1, -0.3, -0.4)) * m, 0.07, 0.021),
                  WHITE, f"leg{side}", fuse=False, surface="shell")
    if z:
        # Gold bands round the shins, a chevron's point to the front
        k.paint(lambda P: inside(np.abs(P[:, 2] - (0.125 + 0.025 * np.clip(np.abs(P[:, 0] - np.sign(P[:, 0]) * 0.165)
                                                                              / 0.07, 0, 1))) - 0.012, 0.003)
                * inside(np.abs(np.abs(P[:, 0]) - 0.165) - 0.09, 0.004), YELLOW, on=["shin"])
    else:
        k.paint(lambda P: sum(inside(np.abs(P[:, 2] - zz) - 0.007, 0.003) for zz in (0.1, 0.135))
                * inside(np.abs(np.abs(P[:, 0]) - 0.165) - 0.09, 0.004), DARK, on=["shin"])
        # A red ring on the outside of each thigh, navy in its middle
        RC = np.array([0.255, -0.06, 0.29])
        def ring(P):
            d = np.linalg.norm(np.c_[np.abs(P[:, 0]), P[:, 1:]] - RC, axis=1)
            return inside(d - 0.048, 0.004) * inside(0.022 - d, 0.004)
        k.paint(ring, RED, on=["thigh"])

    # The arms: a short upper arm down to the elbow, the forearm up to the wrist, and the scythe on it
    if z:
        SH, EL, WR = (0.17, 0.0, 0.79), (0.3, -0.05, 0.77), (0.35, -0.09, 0.9)
        CL = 0.86 * np.array([(0.01, -0.17), (0.0, 0.0), (0.04, 0.13), (0.12, 0.2), (0.22, 0.18), (0.3, 0.07), (0.34, -0.06),
                       (0.35, -0.2)])
        WD = np.array([0.004, 0.12, 0.155, 0.155, 0.14, 0.11, 0.075, 0.004])
    else:
        SH, EL, WR = (0.17, 0.0, 0.79), (0.29, -0.05, 0.63), (0.33, -0.1, 0.72)
        CL = np.array([(-0.02, -0.16), (0.0, 0.0), (0.025, 0.14), (0.085, 0.24), (0.16, 0.255), (0.22, 0.18),
                       (0.25, 0.05), (0.245, -0.12)])
        WD = np.array([0.004, 0.11, 0.14, 0.14, 0.12, 0.095, 0.06, 0.004])
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"arm{side}", tube(np.array([SH, EL]) * m, [0.058, 0.048]), NAVY, f"arm{side}")
        k.add(f"fore{side}", tube(np.array([EL, WR]) * m, [0.048, 0.045]), NAVY, f"arm{side}")
    W = np.array(WR)
    EX, EZ = unit(1, 0.12, 0), unit(0, 0, 1)
    # the plane of the scythe turned a little toward the front cameras, faces forward and back
    OUT, C, N = crescent(CL, WD, (0.5, 0.5))
    k.add("scythe", slab(OUT, W + np.array([0.0, -0.005, 0.0]), EX, EZ, 0.03), BLADE, "arm{s}", fuse=False,
          mirror=True, surface="shell")
    # its navy back along the convex side, near the wrist, thicker so that it shows on both faces
    n = 5
    OUTB, _, _ = crescent(CL[:n], WD[:n] * np.array([1, 1, 1, 1, 1.0]), (0.52, -0.05))
    OUTB = [(a, b) for a, b in OUTB]
    k.add("scytheback", slab(OUTB, W + np.array([0.0, -0.005, 0.0]), EX, EZ, 0.042), NAVY, "arm{s}", fuse=False,
          mirror=True, surface="scales")
    if z:
        # White spikes along the wing's convex edge, pointing out from it
        for j, i in enumerate((2, 3, 4, 5, 6)):
            a, b = C[i] + N[i] * WD[i] * 0.45
            p = W + EX * a + EZ * b + np.array([0, -0.005, 0])
            d = EX * N[i][0] + EZ * N[i][1]
            k.add(f"wingspike{j}", cone(p, d, 0.095, 0.032), WHITE, "arm{s}", fuse=False, mirror=True,
                  surface="shell")
    else:
        k.add("armspike", spike((0.23, 0.0, 0.74), (0.6, 0.2, 0.8), 0.05, 0.02), WHITE, "arm{s}", fuse=False,
              mirror=True, surface="shell")

    # The tail: thick at the hips, long and low, a broad shark's fin of two lobes, the upper hooked
    k.add("tail", tube([(0, 0.1, 0.45), (0, 0.3, 0.33), (0, 0.52, 0.21), (0, 0.74, 0.16)],
                       [0.11, 0.085, 0.055, 0.028]), NAVY, "tail")
    CF = [(0.66, 0.16), (0.8, 0.24), (0.9, 0.36), (0.98, 0.45), (0.88, 0.2), (0.95, 0.04), (0.72, 0.12)]
    k.add("tailfin", slab(CF, (0, 0, 0), (0, 1, 0), (0, 0, 1), 0.032), NAVY, "tail", fuse=False, surface="scales")
    k.add("tailtop", slab([(0.36, 0.3), (0.5, 0.36), (0.46, 0.25)], (0, 0, 0), (0, 1, 0), (0, 0, 1), 0.03), NAVY,
          "tail", fuse=False, surface="scales")
    if z:
        k.paint(lambda P: inside(np.abs(P[:, 1] - 0.45) - 0.014, 0.003) * inside(np.abs(P[:, 0]) - 0.08, 0.004),
                YELLOW, on=["tail"])
    else:
        k.paint(lambda P: sum(inside(np.abs(P[:, 1] - y) - 0.008, 0.003) for y in (0.4, 0.47))
                * inside(np.abs(P[:, 0]) - 0.06, 0.004), DARK, on=["tail"])

    if z:
        # Navy fins off the shoulder blades, back and up and out
        k.add("shfin", slab([(0.0, 0.0), (0.2, 0.15), (0.12, 0.02), (0.1, -0.06)], (0.1, 0.06, 0.78),
                            unit(0.5, 1, 0), unit(0, 0.2, 1), 0.035), NAVY, "body", fuse=False, mirror=True,
              surface="scales")
    else:
        # The dorsal fin, smaller than Garchomp's
        DF = [(0.04, 0.86), (0.18, 0.95), (0.4, 1.0), (0.33, 0.9), (0.36, 0.86), (0.27, 0.74), (0.16, 0.62),
              (0.08, 0.68)]
        DV, DFF = slab(DF, (0, 0, 0), (0, 1, 0), (0, 0, 1), 0.045)
        DV = DV.copy(); DV[:, 0] *= np.clip(1 - 0.75 * (DV[:, 1] - 0.12) / 0.3, 0.3, 1)
        k.add("dorsal", (DV, DFF), NAVY, "body", fuse=False, surface="scales")
        k.add("dorsalroot", blade([(0, 0.09, 0.84), (0, 0.13, 0.73), (0, 0.15, 0.63)], [0.05, 0.06, 0.05], (1, 0, 0),
                                  0.6), NAVY, "body")

    # The teeth
    for j, (p, l) in enumerate([((0.035, -0.235, 0.975), 0.04), ((0.048, -0.18, 0.975), 0.032)]):
        p = np.array(p)
        k.add(f"tooth{j}", blade([p, p + (0, 0, -l * 0.5), p + (0, 0.002, -l)], [0.012, 0.008, 0.002], (0, 1, 0), 0.5),
              WHITE, "head", fuse=False, mirror=True, surface="shell")
    p = np.array((0.035, -0.2, 0.905))
    k.add("fang", blade([p, p + (0, 0, 0.016), p + (0, 0.002, 0.032)], [0.011, 0.007, 0.002], (0, 1, 0), 0.5),
          WHITE, "jaw", fuse=False, mirror=True, surface="shell")

    # The yellow star on the brow
    STAR = [(math.cos(math.pi / 2 + i * math.pi / 5) * (1 if i % 2 == 0 else 0.45),
             math.sin(math.pi / 2 + i * math.pi / 5) * (1 if i % 2 == 0 else 0.45)) for i in range(10)]
    k.decal((0, -0.12, 1.06), unit(0, -0.45, 1), (0.044, 0.044), YELLOW, up=(0, -1, 0), eps=0.005,
            outline=polygon(STAR), rings=10, segs=60)

    # The eyes, Garchomp's: a black rim, a yellow iris with a black slit, the top cut by the brow
    INK, EYE, LID = srgb("#141820"), srgb("#F2C21E"), srgb(NAVY)
    BA, BB = 0.25, 0.45
    def cut(th):
        s_ = math.sin(th) - BB * math.cos(th)
        return min(1.0, BA / s_) if s_ > 1e-6 else 1.0
    def eye(u, v):
        reach = np.array([cut(t) for t in np.arctan2(v, u)])
        rho = np.sqrt(u * u + v * v) / reach
        c = np.tile(INK, (len(u), 1))
        c[rho < 0.76] = EYE
        c[((u + 0.05) / 0.16) ** 2 + ((v + 0.12) / 0.6) ** 2 < 1] = INK
        return c
    de = unit(0.62, -0.74, 0.2)
    EC = np.array([0.058, -0.14, 1.025])
    def eyes(state):
        look = eye_look(state, eye, LID, INK, line=0.16)
        k.decal(EC, de, (0.032, 0.035), look, eps=0.006, rings=20, segs=56, outline=cut)
        mm = np.array([-1, 1, 1.0])
        k.decal(EC * mm, de * mm, (0.032, 0.035), lambda u, v: look(-u, v), eps=0.006, rings=20, segs=56,
                outline=lambda th: cut(math.pi - th))

    k.build(voxel=0.008, smooth=3)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    src, dst = args()
    garchomp_mega(src, dst)
