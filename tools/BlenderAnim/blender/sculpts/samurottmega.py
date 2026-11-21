# Mega Samurott (a Mega of our own, plan 06 · R20b) sculpted on the skeleton and clips of the game's own export of the
# form (sculptkit.py), grown from samurott.py (its body, head, helmet, horn, legs, tail and face copied from samurott()
# and changed here; its plate and claw helpers and oshawott.py's eyes and nose used as they are):
#   blender --background --factory-startup --python sculpts/samurottmega.py -- <the export's Samurott-Mega.glb> <out.glb>
# No pictures: the game's own sculptor (PokemonModels.Ours.Megas.cs, SamurottMega) is the design. The sea lion clad in
# steel: its blue darkened to navy, its cream shells turned to steel; its helmet grown into a kabuto with a neck guard
# and a tall crescent crest of gold rising from a gold disc on the brow (the largest change); its moustache longer;
# plates of steel armour edged cream laid over its shoulders and flanks; and both seamitars half drawn from sheaths on
# the forelegs, longer than the leg, rising past the shoulders.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, hull, slab, srgb, inside, smoothstep, unit, args, sibling

sm = sibling("samurott")
o = sm.o
NAVY, PALE, STEEL, RIDGE, WHITE, NOSE = "#40547A", "#96ACC8", "#96A6BE", "#6A7892", "#F2EEDC", sm.NOSE
GOLD, CREAM, EDGE, SHEATH = "#E8BE46", "#ECE2BE", "#D4DCE8", "#28344F"
CLAW, MOUTH, RED, INK = sm.CLAW, sm.MOUTH, sm.RED, sm.INK


def eye(u, v):
    """Samurott's small red eye, cut by the kabuto's navy rim."""
    c = np.tile(srgb(RED), (len(u), 1))
    c[((u + 0.12) / 0.42) ** 2 + ((v + 0.02) / 0.55) ** 2 < 1] = srgb(INK)
    c[((u - 0.05) / 0.2) ** 2 + ((v - 0.35) / 0.2) ** 2 < 1] = (1.0, 1.0, 1.0)
    c[u * u + v * v > 0.8] = srgb(INK)
    c[v > 0.45 + 0.4 * u] = srgb(NAVY)
    return c


def box(c, half, ex, ey):
    """A flat-faced box: centre, half sizes along ex, ey and their cross."""
    ex = unit(*ex); ey = np.array(ey, float); ey = ey - ex * (ey @ ex); ey /= np.linalg.norm(ey)
    ez = np.cross(ex, ey)
    return hull([np.array(c) + ex * a * half[0] + ey * b * half[1] + ez * d * half[2]
                 for a in (-1, 1) for b in (-1, 1) for d in (-1, 1)])


def armour(k):
    """Three plates of steel each side over the shoulder and flank, sloping out and down, each edged cream below."""
    for sx in (1, -1):
        side = "RL"[sx < 0]
        for i in range(3):
            c = np.array([0.118 * sx, -0.11 + 0.085 * i, 0.39 - 0.035 * i])
            t = math.radians(30)
            out = np.array([math.cos(t) * sx, 0, -math.sin(t)])        # the plate's slope, out and down
            up = np.array([math.sin(t) * sx, 0, math.cos(t)])          # its face
            k.add(f"pauldron{side}{i}", box(c, (0.065, 0.048, 0.016), out, (0, 1, 0)), STEEL, "body", fuse=False,
                  surface="metal")
            lip = c + out * 0.066 - up * 0.002
            k.add(f"pauldronlip{side}{i}", box(lip, (0.009, 0.05, 0.019), out, (0, 1, 0)), CREAM, "body", fuse=False,
                  surface="metal")


def seamitar(k, sx):
    """A sheath on the foreleg's outer side and the seamitar half drawn from it, rising past the shoulder."""
    side = "RL"[sx < 0]
    root = np.array([0.205 * sx, -0.235, 0.13])
    k.add(f"sheath{side}", tube([root + (0, 0.004, -0.045), root + (0.004 * sx, 0.012, 0.035)], [0.026, 0.022]), SHEATH,
          f"leg{side}", fuse=False, surface="shell")
    k.add(f"guard{side}", box(root + (0, 0.014, 0.05), (0.03, 0.026, 0.011), (1, 0, 0), (0, 1, 0)), GOLD, f"leg{side}",
          fuse=False, surface="metal")
    tip = np.array([0.255 * sx, -0.02, 0.56])
    d = tip - root - (0, 0, 0.05)
    L = float(np.linalg.norm(d))
    face = unit(sx, -0.55, 0.0)                                   # its flat turned out and to the front
    ez = np.cross(face, d / L)
    curve = [(0.0, -0.022), (0.3 * L, -0.026), (0.65 * L, -0.026), (0.9 * L, -0.018), (L, 0.012), (0.85 * L, 0.012),
             (0.5 * L, 0.004), (0.0, 0.006)]
    k.add(f"seamitar{side}", slab(curve, root + (0, 0, 0.05), d, ez, 0.018), EDGE, f"leg{side}", fuse=False,
          surface="metal")


def crest(k, g, H):
    """The kabuto's crescent of gold: a disc on the brow, two horns rising out and up, a little back."""
    root = np.array([0.0, -0.29, 0.575])
    k.add("crestdisc", ell(g(root), (0.03, 0.016, 0.03)), GOLD, "head", fuse=False, surface="metal")
    for sx in (1, -1):
        pts = [root + (0.018 * sx, 0.0, 0.0), root + (0.09 * sx, -0.005, 0.03), root + (0.15 * sx, 0.01, 0.1),
               root + (0.18 * sx, 0.04, 0.19), root + (0.17 * sx, 0.07, 0.26)]
        k.add(f"crescent{'RL'[sx < 0]}", blade([g(q) for q in pts], [0.027, 0.03, 0.028, 0.022, 0.014],
                                              (0, 1, 0.2), 0.42), GOLD, "head", fuse=False, surface="metal")


def samurottmega(src, out):
    k = Kit(src, coat="fur")
    k.add("body", ell((0, 0.04, 0.235), (0.135, 0.2, 0.125)), NAVY, "body")
    k.add("chest", ell((0, -0.1, 0.31), (0.125, 0.12, 0.14)), NAVY, "body")
    k.add("neck", tube([(0, -0.12, 0.36), (0, -0.17, 0.42), (0, -0.2, 0.47)], [0.1, 0.085, 0.075]), NAVY, "head")
    J, G = np.array([0, -0.17, 0.4]), 1.15
    def g(p):
        return J + (np.asarray(p, float) - J) * G
    H = np.array([0, -0.215, 0.5])
    HR = np.array([0.078, 0.085, 0.07])
    k.add("head", ell(H, HR), NAVY, "head")
    k.add("snout", ell(H + (0, -0.075, -0.03), (0.05, 0.065, 0.045), rot=(12, 0, 0)), NAVY, "head")
    k.add("jaw", ell(H + (0, -0.06, -0.07), (0.045, 0.055, 0.03), rot=(-10, 0, 0)), NAVY, "head")

    # The kabuto: Samurott's helmet bands in steel, and a neck guard flaring over the nape
    segs = [((0, -0.235, 0.545), (0.085, 0.075, 0.055)), ((0, -0.17, 0.565), (0.092, 0.075, 0.065)),
            ((0, -0.1, 0.545), (0.088, 0.075, 0.065)), ((0, -0.04, 0.5), (0.078, 0.065, 0.06))]
    for i, (c, r) in enumerate(segs):
        k.add(f"helm{i}", ell(c, r, rot=(-10 * i, 0, 0)), STEEL, "head", surface="metal")
    k.add("helmguard", ell((0, -0.03, 0.47), (0.118, 0.07, 0.045), rot=(-25, 0, 0)), STEEL, "head", surface="metal")
    for y in (-0.205, -0.135, -0.07):
        k.paint(lambda P, y=y: (lambda Q: inside(np.abs(Q[:, 1] - y + 0.3 * (Q[:, 2] - 0.56)) - 0.005, 0.002) * (Q[:, 2] > 0.48))(J + (P - J) / G),
                RIDGE, on=["helm"])
    hr = np.array([0, -0.27, 0.56])
    tip = np.array([0, -0.5, 0.64])
    k.add("horn", hull([hr + (0.03, 0.02, -0.02), hr + (-0.03, 0.02, -0.02), hr + (0.025, 0.03, 0.04),
                        hr + (-0.025, 0.03, 0.04), hr + (0, -0.02, 0.03), tip]), STEEL, "head", fuse=False,
          surface="metal")
    for c, h in (((0, -0.15, 0.6), 0.08), ((0, -0.075, 0.585), 0.07)):
        c = np.array(c)
        k.add(f"spike{c[1]:.2f}", hull([c + (0.022, 0.02, -0.02), c + (-0.022, 0.02, -0.02), c + (0.018, -0.022, -0.02),
                                        c + (-0.018, -0.022, -0.02), c + (0, 0.035, h)]), STEEL, "head", fuse=False,
              surface="metal")
    # the crescent goes in after the head is grown (its points are given grown)
    # The moustache: longer than Samurott's, sweeping out and back and rising at its ends
    k.add("tache", blade([(0.035, -0.29, 0.475), (0.09, -0.25, 0.452), (0.16, -0.17, 0.445), (0.22, -0.08, 0.455),
                          (0.26, 0.0, 0.48), (0.28, 0.06, 0.52)], [0.026, 0.042, 0.042, 0.034, 0.022, 0.01],
                         (0, 0.25, 1), 0.45), WHITE, "head", fuse=False, mirror=True, surface="fur")
    k.add("beard", blade([(0, -0.25, 0.44), (0, -0.25, 0.4), (0, -0.235, 0.355)], [0.034, 0.028, 0.006], (0, 1, 0), 0.6),
          WHITE, "head", fuse=False, surface="fur")
    for sx in (1, -1):
        k.add(f"beard{'RL'[sx < 0]}", blade([(0.03 * sx, -0.24, 0.44), (0.045 * sx, -0.22, 0.39), (0.05 * sx, -0.2, 0.35)],
                                           [0.026, 0.02, 0.005], (1, 0.3, 0), 0.6), WHITE, "head", fuse=False,
              surface="fur")
    o.nose(k, H + (0, -0.135, -0.018), unit(0, -1, 0.15), (0.018, 0.012, 0.014), NOSE, "head")

    # The legs as Samurott's, the forelegs' plates in steel
    k.add("shoulder", ell((0.1, -0.13, 0.24), (0.085, 0.09, 0.11)), NAVY, "leg{s}", mirror=True)
    FL = [(0.11, -0.15, 0.22), (0.165, -0.235, 0.11), (0.195, -0.28, 0.035)]
    k.add("foreleg", tube(FL, [0.07, 0.054, 0.044]), NAVY, "leg{s}", mirror=True)
    k.add("forepaw", ell((0.2, -0.3, 0.025), (0.042, 0.05, 0.025), rot=(0, 0, -30)), NAVY, "leg{s}", mirror=True)
    k.add("haunch", ell((0.1, 0.14, 0.21), (0.09, 0.105, 0.11)), NAVY, "hind{s}", mirror=True)
    HL = [(0.12, 0.16, 0.18), (0.165, 0.235, 0.085), (0.185, 0.265, 0.03)]
    k.add("hindleg", tube(HL, [0.068, 0.053, 0.044]), NAVY, "hind{s}", mirror=True)
    k.add("hindpaw", ell((0.19, 0.25, 0.025), (0.042, 0.05, 0.025), rot=(0, 0, 30)), NAVY, "hind{s}", mirror=True)
    for sx in (1, -1):
        side = "RL"[sx < 0]
        m = np.array([sx, 1, 1])
        a, b = np.array(FL[0]) * m, np.array(FL[2]) * m
        d = unit(*(a - b))
        for i, t in enumerate((0.25, 0.48)):
            c = b + (a - b) * t
            sm.plate(k, f"armour{side}{i}", c, d, (sx, 0.15, 0.25), 0.05 + 0.03 * t, 0.06, 0.035, f"leg{side}",
                     color=STEEL)
        sm.claws(k, np.array((0.2, -0.3, 0.02)) * m, (0.4 * sx, -1, 0), f"leg{side}")
        sm.claws(k, np.array((0.19, 0.25, 0.02)) * m, (0.3 * sx, -1, 0), f"hind{side}")
        seamitar(k, sx)
    armour(k)

    # The tail as Samurott's
    TT = np.array([0.04, 0.4, 0.46])
    k.add("tail", tube([(0, 0.18, 0.3), (0, 0.3, 0.37), (0.02, 0.37, 0.43), TT], [0.075, 0.055, 0.042, 0.034]), NAVY,
          "tail")
    fd = unit(0.55, 0.8, 0.25)
    s, n, _ = o.frame(fd, roll=-30)
    for j, lobe in enumerate((unit(*(fd + n * 0.75)), unit(*(fd - n * 0.6)))):
        c = TT + lobe * 0.075
        V, F = ell((0, 0, 0), (0.016, 0.085, 0.04))
        ls = np.cross(lobe, s); ls /= np.linalg.norm(ls)
        V = V @ np.stack([s, lobe, ls]) + c
        k.add(f"fin{j}", (V, F), NAVY, "tail")
    k.paint(lambda P: inside(np.linalg.norm(P - (TT + fd * 0.1), axis=1) - 0.06, 0.004) * (
        ((P - TT) @ s) < 0.004), PALE, on=["fin"])
    sp = np.array([0.0, 0.31, 0.42])
    k.add("tailspike", hull([sp + (0.015, -0.02, -0.01), sp + (-0.015, -0.02, -0.01), sp + (0, 0.025, -0.01),
                             sp + (0, 0.02, 0.06)]), STEEL, "tail", fuse=False, surface="metal")

    def belly(P):
        top = 0.37 - 0.9 * np.abs(P[:, 0])
        front = smoothstep(-0.02, -0.08, P[:, 1] + 0.25 * (P[:, 2] - 0.2))
        return inside(P[:, 2] - top, 0.004) * inside(np.abs(P[:, 0]) - 0.085, 0.004) * front * (P[:, 2] > 0.12)
    k.paint(belly, PALE, on=["chest", "body"])
    INKC, BL = srgb(MOUTH), srgb(NAVY)
    def mouth(u, v):
        c = np.tile(BL, (len(u), 1))
        c[(v < 0.5 - 0.3 * u * u) & (u * u + v * v < 0.9)] = INKC
        return c
    k.decal(g(H + (0, -0.105, -0.06)), unit(0, -0.75, -0.65), (0.033, 0.024), mouth, eps=0.003, rings=16, segs=56)

    for p in k.parts:
        if p["bone"] == "head" and p["name"] != "neck":
            p["V"] = J + (np.asarray(p["V"]) - J) * G
    crest(k, g, H)
    k.build(voxel=0.006, smooth=4, band=0.004)
    E = [(g(o.at_surface(H, HR, d)), d, sx) for sx in (1, -1) for d in [unit(0.62 * sx, -0.76, 0.12)]]
    k.eye_states(lambda st: o.eyes(k, E, (0.02, 0.015), st, NAVY, open_look=eye))
    k.export(out)


if __name__ == "__main__":
    samurottmega(*args())
