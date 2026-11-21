# Scolipede's Mega Evolution sculpted from nothing on the skeleton and clips of the game's own export of it
# (sculptkit.py):
#   blender --background --factory-startup --python sculpts/scolipedemega.py -- <the export's Scolipede-Mega.glb> <out.glb>
# Scolipede grown (scolipede.py through sibling: its segments, its head, its eye and teardrops; venipede.py's spikes
# and cones). From the references: the reared front half armoured in lilac-grey, every segment a heavy plate with a
# teardrop of magenta round a dark heart on its side and seams of mint-green light between them; a great lilac shield
# hanging from the throat over the dark belly to near the ground, notched at its foot, a short spike down its middle;
# the head a lilac helmet with a beak, the eye yellow-green under a heavy lid; the horns grown long, dark banded in
# magenta and ending in blades of mint-green light; the rear half crimson with the same teardrops, its two prongs
# dark with magenta bands; crimson legs with dark bands and points.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, ring_band, outward, srgb, inside, unit, polygon, args, sibling, blade

sc = sibling("scolipede")
vp = sc.vp
ARMOUR, CRIMSON, BELLY, MINT = "#B9AEC8", "#B42A4E", "#34363E", "#52E2B6"
MAGENTA, HEART, DARK, EYE = "#C24AA8", "#3A2A44", "#2C2A34", "#D2DC4A"
REARED, REAR, HEAD_C, HEAD_R = sc.REARED, sc.REAR, sc.HEAD_C, sc.HEAD_R
SCALE = np.array((1.24, 1.16, 1.08))       # the armoured segments, fuller than the base's


def body(k):
    for i, (c, r, b) in enumerate(REARED):
        # the armour a little fuller than the base's segments
        k.add(f"back{i}", ell(c, np.array(r) * SCALE), ARMOUR, b)
    for i, (c, r, b) in enumerate(REAR):
        k.add(f"rear{i}", ell(c, r, deform=vp.flat_bottom(-0.85)), CRIMSON, b)
    k.add("belly", ell((0, -0.04, 0.255), (0.165, 0.175, 0.215)), BELLY, "body")
    k.add("throat", tube([(0, -0.07, 0.36), (0, -0.11, 0.49), (0, -0.135, 0.61), (0, -0.145, 0.72)],
                         [0.13, 0.11, 0.09, 0.07], seg=24), ARMOUR, "front5")
    k.add("head", ell(HEAD_C, np.array(HEAD_R) * 1.14), ARMOUR, "head")
    def hook(V):
        V = V.copy()
        V[:, 2] -= 0.55 * np.clip(-V[:, 1], 0, 1) ** 2
        V[:, 0] *= 0.6 + 0.4 * np.clip(1 + V[:, 1], 0, 1)
        return V
    k.add("beak", ell((0, -0.22, 0.79), (0.06, 0.082, 0.044), deform=hook), ARMOUR, "head")
    k.add("jaw", ell((0, -0.19, 0.735), (0.055, 0.065, 0.03), rot=(12, 0, 0)), ARMOUR, "head")

    # the seams of light between the armoured segments, and two across the helmet
    chain = [np.array(c, float) for c, r, b in REARED]
    for a, b in zip(chain, chain[1:]):
        mid, n = (a + b) / 2, (b - a) / np.linalg.norm(b - a)
        def seam(Q, mid=mid, n=n):
            return inside(np.abs((Q - mid) @ n) - 0.008, 0.003) * (Q[:, 1] > mid[1] - 0.06)
        k.paint(seam, MINT, on=["back"])
    for dz in (0.035, 0.0):
        k.paint(lambda Q, dz=dz: inside(np.abs(Q[:, 1] - (-0.1 + 0.6 * (Q[:, 2] - 0.8 - dz))) - 0.007, 0.003)
                * (Q[:, 2] > 0.8) * inside(np.abs(Q[:, 0]) - 0.09, 0.004), MINT, on=["head"])
    # mint along the belly's foot, under the shield
    k.paint(lambda Q: inside(np.abs(Q[:, 2] - 0.12) - 0.012, 0.004) * (Q[:, 1] < 0.0), MINT, on=["belly"])


def marks(k):
    drop = []
    for th in np.linspace(0, 2 * math.pi, 64, endpoint=False):
        u, v = math.cos(th), math.sin(th)
        if v < 0:
            u *= 1 + v * 0.9
        drop.append((u * 0.8, v * 1.0 + 0.1 * (v > 0)))
    DROP = polygon(drop)
    CM, CH = srgb(MAGENTA), srgb(HEART)
    def tear(u, v):
        reach = np.array([DROP(t) for t in np.arctan2(v, u)])
        rho = np.sqrt(u * u + v * v) / reach
        c = np.tile(CM, (len(u), 1))
        c[rho < 0.55] = CH
        return c
    for (c, r, b), scale in [(x, SCALE) for x in REARED[1:]] + [(x, 1.0) for x in REAR[:4]]:
        for s in (1, -1):
            d = (0.9 * s, 0.42, 0.05) if c[1] > 0.1 else (0.86 * s, 0.5, 0.08)
            p, n = vp.on_ell(np.array(c), np.array(r) * scale, d)
            k.decal(p, n, (0.04, 0.052), tear, eps=0.004, rings=16, segs=56, outline=DROP)
    for s in (1, -1):
        c, r, b = REARED[0]
        p, n = vp.on_ell(c, np.array(r) * SCALE, (0.92 * s, 0.38, 0.0))
        k.decal(p, n, (0.045, 0.06), tear, eps=0.004, rings=16, segs=56, outline=DROP)


def pieces(k):
    # the shield: two halves of a lilac plate hanging from the throat over the belly, notched at the foot
    for s, side in ((1, "R"), (-1, "L")):
        pts = []
        for x, z, y in ((0.0, 0.5, -0.245), (0.08, 0.49, -0.235), (0.14, 0.42, -0.21), (0.17, 0.3, -0.195),
                        (0.16, 0.18, -0.205), (0.09, 0.12, -0.25), (0.0, 0.19, -0.285), (0.0, 0.33, -0.285),
                        (0.07, 0.33, -0.27)):
            for t in (0.0, 0.035):
                pts.append((x * s, y + t, z))
        k.add(f"shield{side}", hull(pts), ARMOUR, "body", fuse=False, surface="shell")
    vp.spike(k, "shieldspike", (0, -0.28, 0.29), (0, -0.33, 0.22), 0.022, "body", color=ARMOUR, mirror=False,
             along=(1, 0, 0))
    # magenta mandibles under the beak, and a row of short magenta claws down each side of the throat's front
    for s, side in ((1, "R"), (-1, "L")):
        vp.spike(k, f"mandible{side}", (0.035 * s, -0.2, 0.73), (0.05 * s, -0.26, 0.69), 0.016, "head",
                 color=MAGENTA, mirror=False, along=(1, 0, 0))
        for j, (z, y, bone) in enumerate(((0.66, -0.2, "front6"), (0.57, -0.2, "front5"), (0.48, -0.2, "front4"))):
            base = np.array((0.06 * s, y + 0.02, z))
            vp.spike(k, f"tclaw{side}{j}", base, base + unit(0.5 * s, -0.8, -0.3) * 0.05, 0.016, bone,
                     color=MAGENTA, mirror=False, along=(0, 0, 1))
    # short magenta spikes forward out of the armoured segments' sides
    for i, (c, r, b) in enumerate(REARED):
        for s in (1, -1):
            p, n = vp.on_ell(c, np.array(r) * SCALE, (0.62 * s, -0.75, -0.05))
            base = p - n * 0.008
            vp.spike(k, f"fspike{i}{'R' if s > 0 else 'L'}", base, base + unit(0.45 * s, -0.85, -0.15) * 0.07, 0.022,
                     b, color=MAGENTA, mirror=False, along=(0, 0, 1))
    # horns: long, from the crown sweeping forward, up and out; dark, banded magenta, a mint blade at the end
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        P = np.array([(0.05, -0.11, 0.86), (0.075, -0.17, 0.93), (0.1, -0.23, 1.0), (0.125, -0.28, 1.06)]) * m
        R = [0.04, 0.036, 0.032, 0.028]
        flat = (s, 0.33, 0)
        k.add(f"horn{side}", outward(blade(P, R, flat, 0.5)), DARK, "head", fuse=False, surface="shell")
        for t in (0.3, 0.62):
            seg = t * (len(P) - 1); i = int(seg); f = seg - i
            c = P[i] + (P[i + 1] - P[i]) * f
            d = P[i + 1] - P[i]
            k.add(f"hband{side}{t}", outward(blade([c - d * 0.12, c + d * 0.12], [0.039, 0.036], flat, 0.56)),
                  MAGENTA, "head", fuse=False, surface="shell")
        T = np.array([(0.12, -0.27, 1.05), (0.145, -0.33, 1.12), (0.165, -0.385, 1.18), (0.178, -0.42, 1.215)]) * m
        k.add(f"blade{side}", outward(blade(T, [0.03, 0.028, 0.018, 0.004], flat, 0.45)), MINT, "head", fuse=False,
              surface="shell")
    # prongs: two up and back from the tail, dark with magenta bands
    for s in (1, -1):
        vp.antenna(k, f"prong{'R' if s > 0 else 'L'}", (0.04 * s, 0.55, 0.31), unit(0.3 * s, 0.62, 0.72), 0.23, 0.03,
                   "tail", color=DARK, band=MAGENTA, bands=((0.22, 0.34), (0.48, 0.6)))
    # legs: crimson, a dark band at the knee and a dark point for a foot
    for bone, y in (("leg{s}", -0.03), ("hind{s}", 0.23), ("hind{s}.001", 0.38)):
        for s, side in ((1, "R"), (-1, "L")):
            m = np.array([s, 1, 1.0])
            hip, knee, ankle = np.array((0.08, y, 0.24)), np.array((0.2, y - 0.01, 0.2)), np.array((0.215, y - 0.03, 0.055))
            if y < 0:
                hip, knee, ankle = (np.array((0.1, y, 0.16)), np.array((0.2, y - 0.02, 0.14)),
                                    np.array((0.215, y - 0.04, 0.05)))
            P = np.array([hip, knee, ankle]) * m
            bn = bone.format(s=side)
            k.add(f"leg{side}{y}", outward(tube(P, [0.044, 0.04, 0.032], seg=16)), CRIMSON, bn, fuse=False,
                  surface="shell")
            mid = P[1] + (P[2] - P[1]) * 0.55
            k.add(f"knee{side}{y}", ring_band(mid, (P[2] - P[1]), 0.04, 0.034, thick=0.007), DARK, bn,
                  fuse=False, surface="shell")
            vp.spike(k, f"foot{side}{y}", ankle * m + np.array((0, 0, 0.015)), np.array((0.225 * s, y - 0.07, 0.0)), 0.03,
                     bn, color=DARK, mirror=False, along=(1, 0, 0))


if __name__ == "__main__":
    src, path = args()
    # the base's eye, its lid the helmet's lilac and its iris yellow-green
    sc.CM, sc.CY = srgb(ARMOUR), srgb(EYE)
    k = Kit(src, coat="shell")
    body(k)
    marks(k)
    pieces(k)
    k.build(voxel=0.007, smooth=3, band=0.004)
    k.eye_states(lambda state: sc.eyes(k, state))
    k.export(path)
