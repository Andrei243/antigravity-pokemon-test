# Ceruledge sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/ceruledge.py -- <the export's Ceruledge.glb> <out.glb>
# Grown from charcadet.py (sibling): its flames (glowing tongues gathered a bone and a colour), its black thighs, its
# way of laying an eye on the head's ellipsoid; Armarouge's helm drawn to a chin, in dark violet.
#
# The design, from the references: a swordsman in dark blue-violet armour. Its largest masses are two long blades of
# ghostly fire running down and out from its forearms, blue at the hilt going to lilac and pink at the point, a guard
# of blue flame licking up round each wrist; and two great angular greaves from the knee to the ground, pointed at
# the toe, each with a guard rising over the knee. A narrow violet helm drawn to a chin, a dark visor with two pink
# eyes, the one on +X the larger; a pale spike out of each side of the helm, the +X one long and level, the -X one short
# and rising; a plume of blue fire rising from the back of the helm and curling to +X. An angular violet chest with a
# blue line of fire down it, pointed shoulder plates, a dark waist, plates over the hips; short black thighs.
# Skeleton: body (0, 0, 0.68), head (0, -0.01, 0.83), flame (0, 0, 0.96), arms (+-0.07, 0, 0.77), legs (+-0.05, 0,
# 0.55).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, boxy, srgb, inside, smoothstep, unit, eye_look, polygon, tri, args, sibling

cd = sibling("charcadet")
COAL, INK = cd.COAL, cd.INK
NAVY, VIOLET, DARK, VISOR = "#2E2A6A", "#4A3E98", "#1C1A40", "#151330"
GREAVE, FOOT, LINE, SPIKE = "#3A347E", "#28245C", "#6A9CF8", "#C8C8F2"
BLADE, LILAC, PINK, FIRE = "#6E9EF8", "#A88CF2", "#E472DC", "#5878F0"
EYE, PUPIL = "#F276D6", "#3A1440"

HC, HR = np.array([0.0, -0.018, 0.918]), np.array([0.102, 0.106, 0.13])
SHO, ELB, HAND = np.array([0.072, 0.0, 0.775]), np.array([0.12, -0.022, 0.69]), np.array([0.128, -0.085, 0.605])
TIP = np.array([0.37, -0.19, 0.2])


def blade_frame(s):
    """The blade's line from the hand to its point, its face turned to the front and out, its width across."""
    h, t = HAND * np.array([s, 1, 1.0]), TIP * np.array([s, 1, 1.0])
    d = t - h; L = np.linalg.norm(d); d /= L
    fn = unit(0.5 * s, -0.86, 0.0); fn = fn - d * (fn @ d); fn /= np.linalg.norm(fn)
    w = np.cross(d, fn)
    return h, d, L, fn, w


def ceruledge(src, out):
    k = Kit(src, coat="metal")
    flames = cd.Flames()

    # --- the helm: Armarouge's egg drawn down to a chin, dark violet with a dark visor ---------------------------
    def helm(V):
        V = V.copy()
        lo = np.clip(-V[:, 2], 0, 1)
        V[:, 0] *= 1 - 0.5 * lo ** 1.3
        V[:, 1] *= 1 - 0.35 * lo ** 1.3
        V[:, 1] -= 0.26 * lo ** 2
        V[:, 2] -= 0.12 * lo ** 2
        return V
    k.add("head", ell(HC, HR, deform=helm), NAVY, "head")
    k.add("neck", tube([(0, 0.0, 0.79), (0, -0.006, 0.85)], [0.036, 0.034]), DARK, "head")
    def visor(P):
        rel = P - HC
        a = cd.azimuth(P, HC)
        return (inside(np.abs(a) - 52, 2.0) * inside(rel[:, 2] - 0.03 + 0.02 * np.abs(a) / 52, 0.003)
                * inside(-0.085 - rel[:, 2], 0.003) * inside(rel[:, 1] + 0.01, 0.004))
    k.paint(visor, VISOR, on=["head"])
    # the brow's ridge down the middle of the helm, lighter violet
    k.paint(lambda P: inside(np.abs(P[:, 0]) - 0.012, 0.002) * inside(HC[2] + 0.03 - P[:, 2], 0.003)
            * inside(P[:, 1] - HC[1] + 0.02, 0.004), VIOLET, on=["head"])
    # a pale spike out of each side: the +X one long and level, the -X one short and rising
    for s, tip, rootz in ((1, (0.23, -0.03, 0.945), 0.9), (-1, (-0.14, -0.01, 1.06), 0.95)):
        b = np.array([0.082 * s, -0.012, rootz + 0.01])
        side = "R" if s > 0 else "L"
        k.add(f"spike{side}", hull([b + (0, 0.02, 0.018), b + (0, -0.02, 0.018), b + (0, 0.02, -0.018),
                                    b + (0, -0.02, -0.018), b + (-0.02 * s, 0, 0), tip]), SPIKE, "head", fuse=False)
    # the plume: blue fire rising from the back of the helm, curling to +X, a pink tip
    top = np.array([0.0, 0.04, 1.015])
    flames.add(cd.tongue(top, unit(0.3, 0.2, 1), 0.2, 0.058, side=(1, 0, 0), flick=0.4, thick=0.75, root=0.9,
                         sub=4), FIRE, "flame", FIRE)
    flames.add(cd.tongue(top + (0.03, 0.02, 0.05), unit(1, 0.2, 0.1), 0.11, 0.03, side=(0, 0, 1), flick=0.35,
                         thick=0.7), FIRE, "flame", FIRE)
    flames.add(cd.tongue(top + (0.035, 0.03, 0.13), unit(0.3, 0.2, 1), 0.07, 0.022, side=(1, 0, 0), flick=0.35,
                         thick=0.7), PINK, "flame", PINK)

    # --- the trunk: an angular violet chest with a line of blue fire, a dark waist, plates at the hips ----------------
    k.add("chest", ell((0, -0.004, 0.745), (0.078, 0.058, 0.085), deform=boxy(2.6)), VIOLET, "body")
    k.add("waist", ell((0, 0.0, 0.65), (0.05, 0.042, 0.05)), DARK, "body")
    k.add("pelvis", ell((0, 0.0, 0.585), (0.068, 0.052, 0.045)), NAVY, "body")
    k.paint(lambda P: inside(np.abs(P[:, 0] - 0.014 * tri((P[:, 2] - 0.62) / 0.07)) - 0.0065, 0.0015)
            * inside(P[:, 2] - 0.8, 0.002) * inside(0.62 - P[:, 2], 0.002) * inside(P[:, 1] + 0.02, 0.003), LINE,
            on=["chest", "waist"])
    for s in (1, -1):
        side = "R" if s > 0 else "L"
        m = np.array([s, 1, 1.0])
        # a pointed shoulder plate, up and out
        sp = [(0.05, 0.03, 0.8), (0.05, -0.04, 0.8), (0.09, 0.035, 0.76), (0.09, -0.045, 0.76), (0.065, 0.03, 0.83),
              (0.065, -0.04, 0.83), (0.15, -0.01, 0.86), (0.13, 0.0, 0.76)]
        k.add(f"shoulder{side}", hull([np.array(p) * m for p in sp]), VIOLET, f"arm{side}", fuse=False)
        # a plate over the hip
        hp = [(0.045, 0.04, 0.62), (0.045, -0.05, 0.62), (0.085, 0.04, 0.6), (0.085, -0.05, 0.6),
              (0.06, 0.03, 0.53), (0.06, -0.05, 0.53), (0.09, -0.01, 0.51)]
        k.add(f"hipplate{side}", hull([np.array(p) * m for p in hp]), NAVY, "body", fuse=False)

    # --- the arms: dark upper arms, armoured forearms held forward and down, the blades running on from the hands ------
    k.add("upperarm", tube([SHO, ELB], [0.028, 0.026]), NAVY, "arm{s}", mirror=True)
    k.add("forearm", tube([ELB, HAND], [0.034, 0.034]), VIOLET, "arm{s}", mirror=True)
    k.add("fist", ell(HAND + (0.0, -0.012, -0.012), (0.032, 0.034, 0.032)), NAVY, "arm{s}", mirror=True)
    for s in (1, -1):
        side = "R" if s > 0 else "L"
        h, d, L, fn, w = blade_frame(s)
        def section(t):
            half = np.interp(t, [0, 0.12, 0.4, 0.75, 1.0], [0.028, 0.04, 0.056, 0.04, 0.0])
            th = np.interp(t, [0, 0.5, 1.0], [0.018, 0.015, 0.004])
            c = h + d * L * t
            return [c + w * half, c - w * half, c + fn * th, c - fn * th]
        for name, (t0, t1), col in (("blade", (0.02, 0.5), BLADE), ("blademid", (0.5, 0.78), LILAC),
                                    ("bladetip", (0.78, 1.0), PINK)):
            ts = np.linspace(t0, t1, 4)
            k.add(f"{name}{side}", hull([p for t in ts for p in section(t)]), col, f"arm{side}", fuse=False, glow=col)
        # the guard: blue flame licking up and out round the wrist
        for dv, ln, wd in ((unit(0.45 * s, -0.2, 1), 0.12, 0.028), (unit(1 * s, -0.3, 0.4), 0.11, 0.026),
                           (unit(-0.4 * s, -0.5, 0.8), 0.08, 0.022)):
            flames.add(cd.tongue(h + d * 0.02, dv, ln, wd, side=(0, 1, 0), flick=0.3 * s, thick=0.7), FIRE, f"arm{side}",
                       FIRE)

    # --- the legs: short black thighs, great angular greaves to the ground -------------------------------------------
    k.add("thigh", tube([(0.05, 0.0, 0.57), (0.066, -0.008, 0.45)], [0.038, 0.036]), COAL, "leg{s}", mirror=True,
          surface="fur")
    for s in (1, -1):
        side = "R" if s > 0 else "L"
        m = np.array([s, 1, 1.0])
        x = 0.075
        # widest at the calf, drawn in to the knee and the ankle, a ridge down the front; a guard over the knee
        up = [(x - 0.034, 0.035, 0.5), (x + 0.034, 0.035, 0.5), (x - 0.034, -0.045, 0.5), (x + 0.034, -0.045, 0.5),
              (x + 0.05, -0.03, 0.57), (x + 0.03, -0.055, 0.53),
              (x - 0.062, 0.05, 0.33), (x + 0.064, 0.05, 0.33), (x - 0.06, -0.07, 0.33), (x + 0.062, -0.07, 0.33),
              (x, -0.09, 0.34),
              (x - 0.04, 0.035, 0.14), (x + 0.04, 0.035, 0.14), (x - 0.04, -0.045, 0.14), (x + 0.04, -0.045, 0.14)]
        k.add(f"greave{side}", hull([np.array(p) * m for p in up]), GREAVE, f"leg{side}", fuse=False)
        ft = [(x - 0.038, 0.03, 0.15), (x + 0.038, 0.03, 0.15), (x - 0.038, -0.04, 0.15), (x + 0.038, -0.04, 0.15),
              (x - 0.05, 0.055, 0.0), (x + 0.05, 0.055, 0.0), (x - 0.045, -0.07, 0.0), (x + 0.045, -0.07, 0.0),
              (x, -0.16, 0.0), (x, -0.13, 0.035)]
        k.add(f"foot{side}", hull([np.array(p) * m for p in ft]), FOOT, f"leg{side}", fuse=False)

    flames.put(k)
    k.build(voxel=0.0062, smooth=4, band=0.008, wband=0.06)

    # The eyes: pink on the dark visor, the +X one the larger; closed, a pink line
    def open_eye(u, v):
        c = np.tile(srgb(EYE), (len(u), 1))
        c[((u + 0.15) / 0.36) ** 2 + ((v + 0.12) / 0.48) ** 2 < 1] = srgb(PUPIL)
        c[((u - 0.25) / 0.2) ** 2 + ((v - 0.3) / 0.2) ** 2 < 1] = (0.97, 0.94, 0.97)
        return c
    shape = [(-1.0, -0.2), (-0.6, -0.8), (0.3, -0.9), (0.95, -0.4), (1.0, 0.5), (0.3, 0.55), (-0.6, 0.35)]
    def lay(state):
        ink = INK if state in ("Open", "Fierce") else EYE
        look = eye_look(state, open_eye, srgb(VISOR), srgb(ink), line=0.2)
        for s, half in ((1, (0.038, 0.027)), (-1, (0.03, 0.022))):
            p, n = cd.on_ell(HC, HR, cd.az_dir(27 * s, -0.12))
            col = look if s == 1 else (lambda a, b, f=look: f(-a, b))
            o = shape if s == 1 else [(-a, b) for a, b in shape]
            k.decal(p, n, half, col, eps=0.0018, rings=20, segs=56, outline=polygon(o))
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    ceruledge(*args())
