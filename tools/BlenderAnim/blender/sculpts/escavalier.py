# Escavalier sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/escavalier.py -- <the export's Escavalier.glb> <out.glb>
#
# From the references: Karrablast grown into a knight in Shelmet's shell. A silver helmet whose visor comes down to a
# point in the middle, with two dark slanted slits; under it Karrablast's dark face, its yellow eyes peeping out under
# the visor's edge, a dark mouth with one white tooth. On the helmet's top a silver fan of a crest, ribbed, and over it
# a great red plume that sweeps from a hook over the brow to a curl down the back. Between the arms the torso shows
# Karrablast's yellow plates, blue at its sides. Each arm is a great silver snail shell, its spiral standing out on its
# outer face, and out of each shell a lance, striped silver and red, pointing forward and down, with a short silver
# point behind. Red pointed flaps hang below the torso. It floats: no legs.
# The export's joints: body (0, 0, 0.296), head (0, -0.02, 0.416), arms (+-0.09, -0.04, 0.296). It hovers, about 0.6
# tall from 0.07 up.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, lathe, outward, srgb, inside, unit, axes, sibling, args

kb = sibling("karrablast")

SILVER, STEEL, GROOVE = "#D9D6CC", "#BDB9AF", "#9C988E"
RED, DARK, MOUTH, SLIT = "#D42A42", kb.DARK, "#1E1A20", "#25262C"

HC = np.array([0, -0.012, 0.458])         # the helmet's middle
HR = np.array([0.078, 0.08, 0.084])
FACE_C, FACE_R = np.array([0, -0.036, 0.39]), np.array([0.058, 0.05, 0.046])
TC, TR = np.array([0, -0.004, 0.29]), np.array([0.074, 0.068, 0.088])     # the torso


def on_ell(c, r, d, lift=0.0):
    d = unit(*d)
    return c + d * (1 / math.sqrt(((d / r) ** 2).sum()) + lift)


def framed(V, c, n):
    """Points of a local frame (x, y in a plane, z along n) put at c."""
    a, b = axes(n)
    return V @ np.stack([a, b, unit(*n)]) + np.array(c, float)


def escavalier(src, out):
    k = Kit(src, coat="metal")

    # --- Karrablast inside: the torso with its plates, the dark face -------------------------------------------------
    k.add("torso", ell(TC, TR), kb.BLUE, "body", surface="shell")
    kb.belly(k, TC, TR, top=0.335, bottom=0.215, width=0.95, lines=(0.31, 0.283, 0.256), on=["torso"])
    k.add("face", ell(FACE_C, FACE_R), DARK, "head", surface="shell")

    # --- the helmet: a silver dome, the visor coming down to a point over the face -----------------------------------
    k.add("helmet", ell(HC, HR), SILVER, "head")

    def vee(V):
        V = V.copy()
        low = np.clip(-V[:, 2], 0, 1)
        V[:, 0] *= 1 - 0.7 * low ** 1.3
        return V
    k.add("visor", ell((0, -0.064, 0.428), (0.074, 0.05, 0.076), deform=vee), SILVER, "head")
    # the face shows under the visor's sides: keep the dark where the visor does not reach
    k.paint(lambda P: inside(np.linalg.norm((P - FACE_C) / (FACE_R * 1.12), axis=1) - 1, 0.01)
            * inside(P[:, 2] - (0.392 + 0.25 * np.abs(P[:, 0])), 0.002) * (P[:, 1] < -0.02), DARK)

    # --- the crest: a silver fan of ribs along the helmet's top -------------------------------------------------------
    FC = np.array([0, 0.0, 0.47])
    for i, (a0, a1) in enumerate([(38, 56), (56, 74), (74, 92), (92, 110), (110, 128), (128, 146)]):
        pts = []
        for a in (a0, a1):
            for r, w in ((0.05, 0.028), (0.148, 0.015)):
                p = FC + r * np.array([0, -math.cos(math.radians(a)), math.sin(math.radians(a))])
                pts += [p + (w, 0, 0), p - (w, 0, 0)]
        k.add(f"crest{i}", hull(pts), SILVER if i % 2 == 0 else STEEL, "head", fuse=False)

    # --- the plume: a great red sweep from a hook over the brow to a curl down the back --------------------------------
    PL = [(26, 0.138), (10, 0.162), (28, 0.19), (58, 0.186), (90, 0.178), (120, 0.176), (150, 0.17), (171, 0.145),
          (165, 0.108)]
    pts = [FC + r * np.array([0, -math.cos(math.radians(a)), math.sin(math.radians(a))]) for a, r in PL]
    V, F = tube(pts, [0.011, 0.021, 0.029, 0.034, 0.036, 0.036, 0.031, 0.021, 0.011], seg=20, per=10)
    V[:, 0] *= 1.55                            # broad from the front, never a bar
    k.add("plume", outward((V, F)),
          RED, "head", fuse=False, surface="cloth")

    # --- the arms: a great snail shell each, its spiral standing out; a lance out of it --------------------------------
    SC = np.array([0.122, -0.062, 0.3])        # the right shell's middle
    SN = unit(0.6, -0.78, -0.14)               # the way its spiral faces
    SR = (0.096, 0.096, 0.062)
    V, F = ell((0, 0, 0), SR, sub=4)
    k.add("shell", (framed(V, SC, SN), F), SILVER, "arm{s}", fuse=False, mirror=True)
    th = np.linspace(0.6, 2.25 * 2 * math.pi, 60)
    rr = 0.013 + 0.076 * (th - th[0]) / (th[-1] - th[0])
    zz = SR[2] * np.sqrt(np.clip(1 - (rr / SR[0]) ** 2, 0, 1)) - 0.002
    spiral = np.stack([rr * np.cos(th), rr * np.sin(th), zz], axis=1)
    rad = 0.0085 + 0.003 * (th - th[0]) / (th[-1] - th[0])
    k.add("spiral", outward(tube(framed(spiral, SC, SN), rad, seg=12, per=3)), GROOVE, "arm{s}", fuse=False,
          mirror=True)

    LD = unit(0.16, -0.58, -0.8)              # the lance's way: forward and down
    LB = SC + np.array([-0.01, -0.045, -0.055])  # where it leaves the shell
    LL = 0.27
    k.add("guard", ell(LB + LD * 0.01, (0.038, 0.038, 0.038), sub=3), SILVER, "arm{s}", fuse=False, mirror=True)
    cuts = [0.0, 0.06, 0.1, 0.15, 0.19, 0.23, LL]
    for i in range(len(cuts) - 1):
        s0, s1 = cuts[i] + 0.02, cuts[i + 1] + 0.02
        r0, r1 = 0.038 * (1 - cuts[i] / LL) + 0.002, 0.038 * (1 - cuts[i + 1] / LL)
        prof = [(0.0, s0), (r0, s0), (max(r1, 0.0), s1)] + ([(0.0, s1)] if r1 > 1e-6 else [])
        k.add(f"lance{i}", lathe(LB, LD, prof, seg=28), STEEL if i % 2 == 0 else RED, "arm{s}", fuse=False,
              mirror=True)
    k.add("pommel", lathe(LB, -LD, [(0.0, 0.02), (0.02, 0.02), (0.02, 0.1), (0.0, 0.17)], seg=24), STEEL, "arm{s}", fuse=False,
          mirror=True)

    # --- red pointed flaps under the torso ------------------------------------------------------------------------------
    for i, az in enumerate((32, -32, 150, -150)):
        d = np.array([math.sin(math.radians(az)), -math.cos(math.radians(az)), 0])
        base = np.array([0, -0.004, 0.235]) + d * 0.045
        side = np.cross(d, (0, 0, 1))
        tip = base + d * 0.03 + np.array([0, 0, -0.11 if az == 0 else -0.085])
        k.add(f"flap{i}", hull([base + side * 0.03, base - side * 0.03, base - d * 0.02 + (0, 0, 0.02),
                                base + d * 0.012, tip]), RED, "body", fuse=False, surface="cloth")

    # --- the visor's two slits, the mouth and its tooth -----------------------------------------------------------------
    ds = unit(0.5, -0.84, 0.3)
    k.decal(on_ell(np.array([0, -0.064, 0.428]), np.array([0.074, 0.05, 0.076]), ds, 0.004), ds, (0.023, 0.0085),
            SLIT, up=(-0.42, 0, 1), eps=0.003, rings=12, segs=48, mirror=True)
    dm = unit(0.0, -0.8, -0.6)
    MC = on_ell(FACE_C, FACE_R, dm)
    CM, CD = srgb(MOUTH), srgb(DARK)

    def mouth(u, v):
        c = np.tile(CD, (len(u), 1))
        c[np.abs(v - (0.1 + 0.5 * u * u)) < 0.42] = CM
        return c
    k.decal(MC, dm, (0.032, 0.014), mouth, eps=0.003, rings=12, segs=56)
    t = MC + np.array([0.012, -0.004, 0.006])
    k.add("tooth", hull([t + (-0.006, 0, 0), t + (0.006, 0, 0), t + (0, 0.006, 0), t + (0, -0.002, -0.012)]),
          kb.FANG, "head", fuse=False)

    k.build(voxel=0.0055, band=0.006)

    # --- the yellow eyes, peeping out under the visor's edge ---------------------------------------------------------
    def eyes(state):
        for s in (1, -1):
            de = unit(0.42 * s, -0.84, -0.32)
            k.decal(on_ell(FACE_C, FACE_R, de), de, (0.021, 0.016),
                    kb.eye_state(state, s, lid=DARK), eps=0.0035, outline=kb.eye_outline(s), rings=27, segs=64)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    escavalier(*args())
