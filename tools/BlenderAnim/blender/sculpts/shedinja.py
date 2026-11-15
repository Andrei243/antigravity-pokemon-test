# Shedinja sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/shedinja.py -- <the export's Shedinja.glb> <out.glb>
#
# The design, from the references: a hollow shed shell, hovering. A great tan helmet of a head with two small points
# on top and a seam down its middle, its front running down to a rounded point over the abdomen; a round pale tan eye
# bulging from each side, blank but for a black line across it and a black dot at its inner end; a grey-beige
# segmented abdomen hanging under it with two knobs at its foot; two tan oval claws hanging at the front; three stiff
# tan wing rays a side, broken off at their tips; a dark hole in its back; and a white halo floating above it, apart.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, hull, lathe, srgb, inside, unit, eye_look, polygon, args

TAN, PALE, SEAM, GREY, LINE = "#D6B676", "#E6CE92", "#9C7C4A", "#BCB2A4", "#8C8274"
HOLE, INK, HALO = "#33261A", "#121010", "#F4F6FC"

H = np.array([0.0, -0.025, 0.285])          # the head's centre
HR = np.array([0.092, 0.088, 0.074])


def ray(k, name, bone, root, d, L, w, branch):
    """A stiff wing ray: a tapering solid, flat across its face (faces front and back), broken at its tip, with a
    short stub off its trailing side."""
    d = np.array(d, float); d /= np.linalg.norm(d)
    p = np.cross((0, 1.0, 0), d); p /= np.linalg.norm(p)         # across the ray, in its plane
    t = np.array([0, 1.0, 0])                                      # its thickness
    root = np.array(root, float)
    tip = root + d * L
    pts = [root + p * w + t * 0.008, root + p * w - t * 0.008, root - p * w + t * 0.008, root - p * w - t * 0.008,
           tip + p * w * 0.55 + t * 0.005, tip + p * w * 0.55 - t * 0.005,
           tip - d * 0.022 - p * w * 0.5 + t * 0.005, tip - d * 0.022 - p * w * 0.5 - t * 0.005]
    k.add(name, hull(pts), TAN, bone, fuse=False)
    if branch:
        b0 = root + d * L * 0.55 + p * w * 0.3
        bd = unit(*(d * 0.7 + p * 0.7))
        bt = b0 + bd * L * 0.2
        pts = [b0 + d * 0.012 + t * 0.006, b0 + d * 0.012 - t * 0.006, b0 - d * 0.012 + t * 0.006,
               b0 - d * 0.012 - t * 0.006, bt + t * 0.004, bt - t * 0.004, bt - d * 0.012 + t * 0.004,
               bt - d * 0.012 - t * 0.004]
        k.add(name + "b", hull(pts), TAN, bone, fuse=False)


def shedinja(src, out):
    k = Kit(src, coat="shell")

    # --- the head: a tan helmet, its front running down to a rounded point --------------------------------------
    k.add("head", ell(H, HR), TAN, "head")
    k.add("brow", ell((0, -0.07, 0.228), (0.07, 0.045, 0.072)), TAN, "head")
    # the eyes' bulbs, pale, bulging from the sides
    EB = np.array([0.083, -0.04, 0.27])
    k.add("bulb", ell(EB, (0.034, 0.034, 0.034)), PALE, "head", mirror=True)
    # the seam down the middle of the helmet's front and top
    def seam(P):
        q = P - H
        return inside(np.abs(q[:, 0]) - 0.0026, 0.0012) * (q[:, 2] > -0.12) * (q[:, 1] < 0.03) \
            * inside(np.linalg.norm(q / (HR + 0.03), axis=1) - 1.0, 0.01)
    k.paint(seam, SEAM, on=["head", "brow"])
    # two small points on top
    for s, side in ((1, "R"), (-1, "L")):
        base = H + np.array([0.045 * s, 0.0, 0.06])
        tip = base + np.array([0.01 * s, -0.004, 0.035])
        pts = [base + (0.018, 0, -0.004), base + (-0.018, 0, -0.004), base + (0, 0.018, -0.006),
               base + (0, -0.018, -0.002), tip]
        k.add(f"point{side}", hull(pts), TAN, "head", fuse=False)

    # --- the abdomen: grey-beige, segmented, two knobs at its foot ---------------------------------------------
    AB = np.array([0.0, 0.005, 0.158])
    k.add("abdomen", ell(AB, (0.064, 0.064, 0.09)), GREY, "body")
    k.add("knob", ell((0.024, -0.03, 0.078), (0.02, 0.02, 0.02)), GREY, "body", mirror=True)
    def segments(P):
        m = np.zeros(len(P))
        for z0 in (0.19, 0.16, 0.13, 0.1):
            m = np.maximum(m, inside(np.abs(P[:, 2] - z0 - 0.01 * (P[:, 1] / 0.064)) - 0.0026, 0.0012))
        return m * (P[:, 2] < 0.205)
    k.paint(segments, LINE, on=["abdomen"])

    # the hole in its back: a dark ragged patch on the helmet's back
    k.decal(H + np.array([0, 0.08, 0.0]), (0, 1, 0.15), (0.024, 0.034), HOLE, eps=0.003, rings=10, segs=48,
            outline=polygon([(0, 1), (0.5, 0.6), (0.35, 0.2), (0.8, -0.2), (0.4, -0.5), (0.2, -1), (-0.3, -0.7),
                             (-0.7, -0.4), (-0.4, 0.0), (-0.8, 0.4), (-0.4, 0.7)]))

    # --- the claws: tan ovals hanging at the front ---------------------------------------------------------------
    k.add("claw", ell((0.074, -0.085, 0.165), (0.021, 0.026, 0.05), rot=(-12, 0, -25)), TAN, "arm{s}", fuse=False,
          mirror=True)

    # --- the wings: three stiff rays a side, fanned out and swept back ---------------------------------------
    for s, side in ((1, "R"), (-1, "L")):
        root = np.array([0.06 * s, 0.035, 0.265])
        for i, (dz, L, br) in enumerate(((0.55, 0.15, True), (0.12, 0.165, False), (-0.35, 0.13, True))):
            ray(k, f"ray{i}{side}", f"wing{side}", root + (0, 0.004 * i, -0.012 * (i - 1)),
                (s * 1.0, 0.38, dz), L, 0.025, br)

    # --- the halo: a white ring floating above, apart --------------------------------------------------------
    R, a, b = 0.066, 0.017, 0.0085
    th = np.linspace(0, 2 * math.pi, 25)
    prof = [(R + a * math.cos(t), b * math.sin(t)) for t in th]
    k.add("halo", lathe((0, 0.0, 0.415), unit(0.12, -0.14, 1), prof, seg=56), HALO, "head", fuse=False, glow=HALO)

    k.build(voxel=0.0036, smooth=4, band=0.006)

    # --- the eyes: blank, a black line across, a dot at the inner end (the open state is that line) ----------------
    CP, CK = srgb(PALE), srgb(INK)
    def eye(u, v, slant=0.0):
        c = np.tile(CP, (len(u), 1))
        c[(np.abs(v - slant * u) < 0.16) & (np.abs(u) < 0.86)] = CK
        c[((u + 0.46) / 0.3) ** 2 + ((v - slant * -0.46) / 0.3) ** 2 < 1] = CK
        return c
    de = unit(0.88, -0.45, 0.05)
    p = EB + de * 0.034
    def eyes(state):
        if state not in ("Open", "Fierce"):
            look = eye_look(state, eye, CP, CK, line=0.14)
            k.decal(p, de, (0.029, 0.029), look, eps=0.003, rings=20, segs=56, mirror=True)
            return
        # the open line and its dot are patches of their own, so their edges are crisp; laid a side at a time, since
        # +u runs toward the back on the right and toward the front on the left
        slant = 0.35 if state == "Fierce" else 0.0
        for s in (1, -1):
            m = np.array([s, 1, 1.0])
            ps, ds = p * m, de * m
            fwd = np.array([0, -1.0, 0]) - ds * (ds @ np.array([0, -1.0, 0])); fwd /= np.linalg.norm(fwd)
            line = [(-1, -0.17 + slant * s * -1), (1, -0.17 + slant * s), (1, 0.17 + slant * s),
                    (-1, 0.17 + slant * s * -1)]
            k.decal(ps, ds, (0.023, 0.025), CK, eps=0.004, rings=8, segs=64, outline=polygon(line))
            k.decal(ps + fwd * 0.012 - np.array([0, 0, 0.025 * slant * 0.45]), ds, (0.0085, 0.0085), CK, eps=0.005,
                    rings=6, segs=40)

    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    shedinja(*args())
