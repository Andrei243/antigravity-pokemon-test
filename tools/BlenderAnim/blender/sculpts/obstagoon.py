# Obstagoon sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/obstagoon.py -- <the export's Obstagoon.glb> <out.glb>
#
# The design, from the references: a tall black badger standing upright in a wide stance, its arms crossed in an X
# before its chest. A long tapering white badger snout carried forward, a black nose at its tip, a black mask through
# the eyes running back to small black ears, angry magenta-red eyes; the mouth open at the front and a long flat pink
# tongue hanging out over the crossed arms. A great black mane, the largest mass after the trunk: from the brow up over
# the head and down the back to the shoulder blades, spiky along its edge, with a white band along its lower side
# (black diamonds on it, black teeth along its top edge) from behind the ear round the back of the neck. Big grey fur
# puffs on the shoulders and grey fur hanging from the backs of the arms; black upper arms, a white four-point star on
# the right one (+X, the viewer's right in the art's three-quarter); the forearms great grey shaggy sleeves, crossed,
# dark claws at their ends. A black trunk and hips; black legs set wide with a white stripe down their inner fronts,
# grey shaggy fur round the ankles, dark feet with pale claws.
# Reference ratios (3D picture): head (cranium and snout) 0.175 of the height (made 0.19), snout 0.3 of the height,
# shoulder span 0.55 of the height, the feet 0.42 apart.
# The export's joints: body (0, 0, 0.48), arms (+-0.13, -0.02, 0.55), head (0, -0.03, 0.64), legs (+-0.08, 0, 0.32).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, blade, hull, srgb, inside, unit, eye_look, args, outward, tri, polygon, axes,
                       lumpy)

BLACK, WHITE, GREY, DGREY = "#2E2C33", "#EEECEA", "#A2A4AC", "#4C4C54"
CLAW, TONGUE, MOUTH = "#C8C8CE", "#EC8AA6", "#4A1C2C"
IRIS, PUPIL = "#E2306C", "#1A0E14"

HC = np.array([0, -0.05, 0.77])              # the cranium's middle
GROW = 1.15                                   # the head laid out at its first size and grown about its middle


def g(p):
    """A place laid out on the first head, on the grown one."""
    return HC + GROW * (np.array(p, float) - HC)


HCR = np.array([0.11, 0.105, 0.098]) * GROW
SNOUT = [g(p) for p in ((0, -0.11, 0.765), (0, -0.195, 0.742), (0, -0.27, 0.722))]
SNOUT_R = [r * GROW for r in (0.078, 0.056, 0.032)]
EYE = g((0.05, -0.142, 0.8))
# the mane's middle line, from the brow over the head and down the back
ARC = np.array([(0, -0.07, 0.84), (0, 0.03, 0.89), (0, 0.14, 0.865), (0, 0.23, 0.765), (0, 0.27, 0.635),
                (0, 0.25, 0.52)])
ARC_R = [0.055, 0.085, 0.1, 0.1, 0.09, 0.06]
MANE_X = 1.3                                  # the mane is wider than it is deep: a hood


def along(c, d, half_len, half_w, deform=None, flat=1.0):
    """An ellipsoid laid along a direction from its middle c: half_len along d, half_w across (times flat one way)."""
    a, b = axes(d)
    V, F = ell((0, 0, 0), (half_w, half_w * flat, half_len), deform=deform)
    return V @ np.stack([a, b, unit(*d)]) + np.array(c, float), F


def arc_frame(P):
    """For points P: the nearest point of the mane's line (densely sampled), its arc length and the side toward the
    head in the y-z plane (positive inward)."""
    line =np.array([ARC[0]] + [ARC[i] + (ARC[i + 1] - ARC[i]) * t for i in range(len(ARC) - 1)
                                for t in np.linspace(0, 1, 30)[1:]])
    seg = np.diff(line, axis=0)
    s_at = np.concatenate([[0], np.cumsum(np.linalg.norm(seg, axis=1))])
    tang = np.gradient(line, axis=0); tang /= np.linalg.norm(tang, axis=1, keepdims=True)
    yz = P[:, 1:3]
    d2 = ((yz[:, None, :] - line[None, :, 1:3]) ** 2).sum(2)
    i = d2.argmin(1)
    t = tang[i]
    inward = np.stack([t[:, 2], -t[:, 1]], 1)          # the tangent turned toward the head (front and down)
    off = ((yz - line[i, 1:3]) * inward).sum(1)
    return s_at[i], off


def add_trunk(k):
    k.add("trunk", ell((0, 0.02, 0.46), (0.16, 0.13, 0.17)), BLACK, "body")
    k.add("chest", ell((0, -0.01, 0.6), (0.19, 0.14, 0.1)), BLACK, "body")
    k.add("hips", ell((0, 0.02, 0.32), (0.15, 0.12, 0.095)), BLACK, "body")
    k.add("neck", tube([(0, 0.0, 0.6), (0, -0.04, 0.72)], [0.1, 0.085]), BLACK, "head")


def add_head(k):
    def jowl(V):
        V = V.copy()
        V[:, 0] *= 1 + 0.08 * np.clip(-V[:, 2], 0, 1)
        return V
    k.add("head", ell(HC, HCR, deform=jowl), WHITE, "head")
    k.add("snout", tube(SNOUT, SNOUT_R), WHITE, "head")
    k.add("ear", blade([g((0.075, 0.0, 0.83)), g((0.095, 0.03, 0.88)), g((0.105, 0.05, 0.91))],
                       [0.032, 0.024, 0.008], (1, 0, 0), 0.55), BLACK, "head", mirror=True)
    k.add("nose", outward(ell(g((0, -0.297, 0.73)), np.array((0.03, 0.022, 0.023)) * GROW)), BLACK, "head",
          fuse=False, surface="skin")

    # the black mask through the eye, rising back to the ear; the crown black under the mane
    def mask(P):
        x = np.abs(P[:, 0])
        line = EYE[2] - 0.004 + 0.3 * (P[:, 1] - EYE[1])
        band = inside(np.abs(P[:, 2] - line) - 0.03, 0.0025) * inside(0.03 - x, 0.003) * inside(EYE[1] - 0.075 - P[:, 1], 0.003)
        crown = inside(0.845 - 0.3 * (P[:, 1] + 0.05) - P[:, 2], 0.003) * (P[:, 1] > -0.16)
        return np.maximum(band, crown)
    k.paint(mask, BLACK, on=["head", "snout", "ear"])

    # the mouth: a dark wedge on each side of the snout opening to the front, the tongue's pink in it
    CM, CT = srgb(MOUTH), srgb(TONGUE)
    def mouth(u, v):
        c = np.tile(CM, (len(u), 1))
        c[(u < -0.55) & (np.abs(v) < 0.45)] = CT
        return c
    k.decal(g((0.045, -0.215, 0.712)), unit(1, -0.15, -0.45), (0.072, 0.017), mouth, eps=0.003, rings=10, segs=48,
            outline=polygon([(-1, -0.95), (-1, 0.95), (1, 0.3), (1, -0.3)]), mirror=True)
    # the long flat tongue out of the mouth's front, hanging over the crossed arms
    shift = g((0, -0.235, 0.71)) - np.array((0, -0.235, 0.71))
    pts = [np.array(p) + shift for p in ((0, -0.235, 0.71), (0.006, -0.29, 0.69), (0.016, -0.315, 0.63),
                                          (0.026, -0.305, 0.57), (0.034, -0.29, 0.535))]
    k.add("tongue", outward(blade(pts, [0.024, 0.027, 0.028, 0.026, 0.018], (0.15, 1, 0.2), 0.42)), TONGUE, "head",
          fuse=False, surface="skin")


def add_mane(k):
    V, F = tube(ARC, ARC_R, seg=24, per=10)
    V[:, 0] *= MANE_X
    k.add("mane", (V, F), BLACK, "head")
    # spikes along its back edge, swept back
    for i, (t, L, r) in enumerate(((0.12, 0.08, 0.035), (0.3, 0.11, 0.042), (0.47, 0.12, 0.045), (0.63, 0.11, 0.042),
                                   (0.78, 0.1, 0.038), (0.92, 0.08, 0.032))):
        j = t * (len(ARC) - 1)
        a = int(min(j, len(ARC) - 2)); f = j - a
        p = ARC[a] + (ARC[a + 1] - ARC[a]) * f
        tg = unit(*(ARC[a + 1] - ARC[a]))
        out_ = unit(0, -tg[2], tg[1])                  # away from the head
        rr = ARC_R[a] + (ARC_R[a + 1] - ARC_R[a]) * f
        root = p + out_ * rr * 0.7
        d = unit(*(out_ * 0.55 + tg * 0.75))
        k.add(f"manespike{i}", blade([root, root + d * L * 0.5, root + d * L], [r, r * 0.6, 0.008], (1, 0, 0), 0.6),
              BLACK, "head")
        # and a lower pair off the sides
        if 0.2 < t < 0.85:
            for s in (1, -1):
                sroot = p + np.array([s * rr * MANE_X * 0.75, 0, 0]) + out_ * rr * 0.2
                sd = unit(*(np.array([s * 0.7, 0, 0]) + tg * 0.8 + out_ * 0.2))
                k.add(f"maneside{i}{'R' if s > 0 else 'L'}",
                      blade([sroot, sroot + sd * L * 0.4, sroot + sd * L * 0.75], [r * 0.85, r * 0.5, 0.007],
                            (0, 0, 1), 0.6), BLACK, "head")

    # the white band along the mane's lower side, black teeth along its top edge
    def band(P):
        s, off = arc_frame(P)
        x = np.abs(P[:, 0])
        top = 0.006 + 0.02 * (1 - np.abs(tri(s / 0.075)))
        return (inside(top - off, 0.0025) * inside(off - 0.075, 0.003) * inside(0.045 - x, 0.004)
                * inside(0.22 - s, 0.004) * (P[:, 2] > 0.5))
    k.paint(band, WHITE, on=["mane", "maneside"])


def add_diamonds(k):
    """Black diamonds on the white band, cast from the side."""
    for i, (y, z) in enumerate(((0.15, 0.79), (0.205, 0.7), (0.225, 0.6))):
        c = np.array([0.11, y, z])
        k.decal(c, unit(1, 0.12, 0.0), (0.024, 0.026), BLACK, eps=0.0035, rings=8, segs=32,
                outline=polygon([(1, 0), (0, 1), (-1, 0), (0, -1)]), mirror=True)


def add_arms(k):
    for s, bone in ((1, "armR"), (-1, "armL")):
        m = np.array([s, 1, 1.0])
        S = np.array((0.16, -0.02, 0.6)) * m
        E = np.array((0.21, -0.11, 0.46)) * m
        k.add("arm" + bone[-1], tube([S - (0.04 * s, 0, 0), S, E], [0.075, 0.072, 0.064]), BLACK, bone)
        k.add("puff" + bone[-1], ell(np.array((0.205, 0.02, 0.665)) * m, (0.09, 0.09, 0.08), deform=lumpy(5, 3, 0.08)),
              GREY, bone)
        # crossed: the right forearm in front, rising to the left shoulder; the left behind it, rising to the right
        W = np.array((-0.1, -0.255, 0.565)) if s > 0 else np.array((0.105, -0.165, 0.585))
        Ein = E + unit(*(W - E)) * 0.02
        L = np.linalg.norm(W - Ein)
        lump = lumpy(6, 4, 0.1) if s > 0 else lumpy(5, 3, 0.04)      # the one behind smoother, pulled back
        k.add("sleeve" + bone[-1], outward(along((Ein + W) / 2, W - Ein, L / 2 + 0.02, 0.078 if s > 0 else 0.07,
                                                 deform=lump)), GREY, bone, fuse=False, surface="fur")
        d = unit(*(W - E))
        k.add("fist" + bone[-1], outward(ell(W + d * 0.012, (0.038, 0.038, 0.038))), DGREY, bone, fuse=False,
              surface="skin")
        a, b = axes(d)
        for j, o in enumerate((-0.018, 0.018)):
            base = W + d * 0.035 + b * o
            tip = base + d * 0.022 - a * 0.016 * (1 if s > 0 else -1)
            k.add(f"claw{bone[-1]}{j}", hull([base + a * 0.012, base - a * 0.012, base + b * 0.011, base - b * 0.011,
                                              tip]), "#6E6E78", bone, fuse=False, surface="shell")
        # grey fur falling from the shoulder puff down the back of the arm to the hip: one shaggy mass, a few
        # short uneven points at its foot
        k.add("hang" + bone[-1], ell(np.array((0.235, 0.025, 0.47)) * m, (0.055, 0.07, 0.14), deform=lumpy(4, 3, 0.08)),
              GREY, bone)
        for j, (dy, L2, r) in enumerate(((-0.03, 0.05, 0.03), (0.02, 0.075, 0.034), (0.065, 0.045, 0.028))):
            root = np.array((0.235 * s, dy, 0.36))
            k.add(f"hangpt{bone[-1]}{j}", blade([root, root + (0.01 * s, 0.01, -L2 * 0.5), root + (0.015 * s, 0.015, -L2)],
                                                [r, r * 0.65, 0.007], (1, 0, 0), 0.6), GREY, bone)


def add_star(k):
    """The white four-point star on the right upper arm."""
    c = np.array((0.185, -0.065, 0.53)) + unit(1, -0.35, 0.15) * 0.066
    k.decal(c, unit(1, -0.35, 0.15), (0.034, 0.046), WHITE, eps=0.003, rings=10, segs=64,
            outline=polygon([(0, 1), (0.2, 0.2), (1, 0), (0.2, -0.2), (0, -1), (-0.2, -0.2), (-1, 0), (-0.2, 0.2)]))


def add_legs(k):
    for s, bone in ((1, "legR"), (-1, "legL")):
        m = np.array([s, 1, 1.0])
        Hp, K, A = (np.array(p) * m for p in ((0.09, 0.01, 0.31), (0.15, -0.045, 0.18), (0.18, -0.02, 0.075)))
        k.add("thigh" + bone[-1], along((Hp + K) / 2 + (0, 0.0, 0.01), K - Hp, 0.1, 0.088), BLACK, bone)
        k.add("shin" + bone[-1], tube([K, A], [0.068, 0.058]), BLACK, bone)
        k.add("boot" + bone[-1], ell(np.array((0.185, -0.02, 0.085)) * m, (0.088, 0.088, 0.072),
                                     deform=lumpy(6, 3, 0.1)), GREY, bone)
        k.add("foot" + bone[-1], ell(np.array((0.19, -0.065, 0.03)) * m, (0.062, 0.085, 0.032)), DGREY, bone)
        for j, dx in enumerate((-0.03, 0.0, 0.03)):
            base = np.array((0.19 + dx, -0.135, 0.022)) * m
            tip = base + np.array((dx * 0.4 * s, -0.04, -0.014))
            k.add(f"toe{bone[-1]}{j}", hull([base + (0.012, 0, 0), base - (0.012, 0, 0), base + (0, 0.01, 0.012),
                                             base + (0, 0.01, -0.012), tip]), CLAW, bone, fuse=False, surface="shell")

        # the white stripe down the inner front of the leg
        def stripe(P, Hp=Hp, A=A, s=s):
            ax = unit(*(A - Hp))
            rel = P - Hp
            t = rel @ ax
            off = rel - np.outer(t, ax)
            f = unit(-s, -1.1, 0)
            r = np.linalg.norm(off, axis=1) + 1e-9
            return (inside(0.82 - (off @ f) / r, 0.05) * inside(-t, 0.01) * inside(t - np.linalg.norm(A - Hp) + 0.03, 0.005)
                    * (P[:, 0] * s > 0))
        k.paint(stripe, WHITE, on=["thigh", "shin"])
    # white at the crotch where the stripes meet
    k.paint(lambda P: inside(np.hypot(P[:, 0] / 0.05, (P[:, 2] - 0.245) / 0.06) - 1, 0.05) * (P[:, 1] < -0.02),
            WHITE, on=["hips", "thigh"])


def lay_eyes(k):
    CI, CP, CB, CW, CL = srgb(IRIS), srgb(PUPIL), srgb(BLACK), srgb("#FFFFFF"), srgb("#B8B8C2")
    shape = polygon([(-1, -0.15), (-0.45, 0.32), (0.4, 0.72), (1, 0.45), (0.85, -0.45), (0.05, -0.78), (-0.7, -0.55)])
    def eye(u, v):
        c = np.tile(CI, (len(u), 1))
        c[((u + 0.05) / 0.32) ** 2 + ((v + 0.02) / 0.62) ** 2 < 1] = CP
        c[((u - 0.32) / 0.16) ** 2 + ((v - 0.3) / 0.18) ** 2 < 1] = CW
        return c
    de = unit(0.78, -0.42, 0.32)
    half = (0.028 * GROW, 0.022 * GROW)
    m = np.array([-1, 1, 1.0])

    def lay(look, outline):
        """The almond is not symmetric, and a mirrored patch's outline is not mirrored: each eye laid by hand, the
        left one's outline and u turned round (its decal's u runs inward)."""
        k.decal(EYE, de, half, look, eps=0.0025, rings=20, segs=56, outline=outline)
        k.decal(EYE * m, de * m, half, lambda u, v: look(-u, v), eps=0.0025, rings=20, segs=56,
                outline=(lambda th: outline(math.pi - th)) if outline else None)

    def eyes(state):
        if state == "Fierce":
            lay(eye, lambda th: min(1.0, shape(th)) * (0.8 if math.sin(th) > 0 else 1.0))
        elif state == "Open":
            lay(eye, lambda th: min(1.0, shape(th)))
        else:
            lay(eye_look(state, eye, CB, CL, line=0.16), None)
    return eyes


def obstagoon(src, out_path):
    k = Kit(src, coat="fur")
    add_trunk(k)
    add_head(k)
    add_mane(k)
    add_arms(k)
    add_legs(k)
    add_star(k)
    add_diamonds(k)
    k.build(voxel=0.0072, band=0.004, wband=0.06)
    k.eye_states(lay_eyes(k))
    k.export(out_path)


if __name__ == "__main__":
    obstagoon(*args())
