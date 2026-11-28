# Radaskon (Carpathia No. 10073, the stag beetle) sculpted from nothing on the skeleton and clips of the game's own
# export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/radaskon.py -- <the export's Radaskon.glb> <out.glb>
#
# Grown from radascuta.py (sibling("radascuta")): the line's cream, chestnut and dark brown, its hood over the head
# (now an iron helm), its fierce eyes under a brow cut sloping to the nose, its snout and the short mandibles curling
# at the mouth.
# From its picture (docs/art/reference/gemini/carpathia/10073-Radaskon.jpg) and its brief (CP-dex-2-mountains.md, 73):
# an armoured beetle knight standing on its hind legs, knees bent in a wide stance. Its largest masses: the polished
# chestnut wing cases on its back, a great oval shield from above the shoulders to behind the knees, parted down the
# middle and rimmed in bronze with iron rivets; and the antlers, two iron beams rising from the helm in a V, each with
# three tines, every tip bronze. The front is a big round cream plastron in plates. The head in an iron helm with a
# bronze rim round the face; iron pauldrons, iron vambraces with a bronze band and a spike, dark clawed hands held
# forward; dark chestnut thighs fused into the hips, iron knee caps, short armoured shins with a spur, clawed feet.
# Bones: body (0, 0.02, 0.6), arm{s} (0.27, -0.04, 0.84), head (0, -0.08, 0.92), leg{s} (0.17, 0.02, 0.42).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, sheet, hull, lathe, ring_band, outward, srgb, inside, smoothstep, unit,
                       args, sibling)

rc = sibling("radascuta")

CHESTNUT, DEEP, SEAM = "#7E3A24", "#5E2A1A", "#341810"
CREAM, FOLD, BROW, LEG = rc.CREAM, rc.FOLD, rc.BROW, rc.LEG
IRON, IRONLIGHT, BRONZE, RIVET = "#4C4A52", "#66646E", "#C2803E", "#9A98A2"

HC, HR = np.array([0.0, -0.135, 0.985]), np.array([0.167, 0.145, 0.156])    # the head
BC, BR = np.array([0.0, -0.05, 0.56]), np.array([0.25, 0.2, 0.31])         # the plastron
TC, TR = np.array([0.0, 0.05, 0.7]), np.array([0.24, 0.17, 0.32])          # the trunk under the wing cases
EC, ER = np.array([0.0, 0.07, 0.71]), np.array([0.375, 0.26, 0.53])         # the wing cases' ellipsoid


def case(c, r, p0, p1, t0, t1, thick, nu=26, nv=44):
    """The right wing case of an upright beetle: a sheet on the ellipsoid (c, r) round the back from the midline (t0,
    degrees from straight behind) toward the side (t1), from p0 to p1 up its height (-1 the foot, 1 the top)."""
    c, r = np.array(c, float), np.array(r, float)
    def at(u, v):
        t = np.radians(t0 + (t1 - t0) * u)
        p = p0 + (p1 - p0) * v
        q = np.sqrt(np.clip(1 - p * p, 1e-4, 1))
        return np.stack([c[0] + r[0] * q * np.sin(t), c[1] + r[1] * q * np.cos(t), c[2] + r[2] * p], axis=-1)
    return outward(sheet(at, nu, nv, thick))


def helm_edge(phi):
    a = np.abs(phi)
    return 48 + 30 * (a / 114) ** 1.6 - 8 * np.exp(-(phi / 16) ** 2)


def rim_band(c, r, edge, side_deg, width, thick, nu=80):
    """The bronze rim round the helm's face: a strip just over the helm from its edge back by width degrees."""
    c, r = np.array(c, float), np.array(r, float)
    def at(u, v):
        phi = np.radians(-side_deg + 2 * side_deg * u)
        th = np.radians(edge(np.degrees(phi)) - 1.5 + width * v)
        d = np.stack([np.sin(th) * np.sin(phi), -np.cos(th), np.sin(th) * np.cos(phi)], axis=-1)
        return c + d * r
    return outward(sheet(at, nu, 6, thick))


def antler(k, s):
    """An iron beam from the helm's crown, up and out, with three tines toward the middle and its tip, all bronze
    tipped."""
    m = np.array([s, 1, 1.0])
    beam = np.array([(0.05, -0.12, 1.07), (0.11, -0.14, 1.24), (0.19, -0.12, 1.42), (0.26, -0.08, 1.58),
                     (0.29, -0.05, 1.7)]) * m
    k.add(f"beam{'R' if s > 0 else 'L'}", outward(tube(beam, [0.06, 0.052, 0.046, 0.04, 0.034])), IRON, "head",
          fuse=False, surface="metal")
    tines = [(0.22, (0.36, -0.22, 1.38), 0.034), (0.5, (0.07, -0.21, 1.62), 0.032), (0.74, (0.43, -0.09, 1.64), 0.03)]
    tips = [((0.29, -0.05, 1.7), (0.3, -0.04, 1.76))]
    for j, (f, to, r0) in enumerate(tines):
        i = f * (len(beam) - 1)
        a = beam[int(i)] + (beam[min(int(i) + 1, len(beam) - 1)] - beam[int(i)]) * (i - int(i))
        b = np.array(to) * m
        mid = a + (b - a) * 0.55 + np.array([0, 0, 0.03])
        k.add(f"tine{j}{'R' if s > 0 else 'L'}", outward(tube([a, mid, b], [r0, r0 * 0.85, r0 * 0.7])), IRON, "head",
              fuse=False, surface="metal")
        tips.append((b, b + (b - mid) / np.linalg.norm(b - mid) * 0.07))
    for j, (a, b) in enumerate(tips):
        a, b = np.array(a) * (m if j == 0 else 1), np.array(b) * (m if j == 0 else 1)
        d = (b - a) / np.linalg.norm(b - a)
        k.add(f"tip{j}{'R' if s > 0 else 'L'}", outward(tube([a - d * 0.02, (a + b) / 2, b], [0.028, 0.02, 0.006])),
              BRONZE, "head", fuse=False, surface="metal")


def radaskon(src, out):
    k = Kit(src, coat="shell")
    # --- the trunk, the plastron and the dark core under the wing cases ---------------------------------------
    k.add("plastron", ell(BC, BR, sub=5), CREAM, "body", surface="fur")
    k.add("trunk", ell(TC, TR, sub=5), CHESTNUT, "body")
    k.add("core", ell(EC + (0, -0.01, 0), ER - (0.025, 0.03, 0.03), sub=5), SEAM, "body")
    k.add("gorget", ell((0, -0.07, 0.84), (0.17, 0.14, 0.07)), IRON, "body", surface="metal")
    # --- the head in its helm ---------------------------------------------------------------------------------
    k.add("skull", ell(HC, HR, sub=5), CREAM, "head", surface="fur")
    for s, nm in ((1, "browR"), (-1, "browL")):
        p, n = rc.on_ell(HC, HR, 29 * s, 25, -0.009)
        k.add(nm, ell(p, (0.046, 0.017, 0.014), rot=(0, -16 * s, 0)), BROW, "head", surface="fur")
    k.add("snout", ell((0, -0.258, 0.935), (0.052, 0.032, 0.034)), BROW, "head", surface="fur")
    k.add("helm", rc.hood(HC, HR + 0.014, helm_edge, 114, 177, 0.02), IRON, "head", fuse=False, surface="metal")
    k.add("rim", rim_band(HC, HR + 0.026, helm_edge, 114, 9, 0.008), BRONZE, "head", fuse=False, surface="metal")
    # a bronze ridge over the crown from the brow back
    k.add("crest", outward(tube([(0, -0.26, 1.1), (0, -0.17, 1.165), (0, -0.06, 1.172), (0, 0.03, 1.12)],
                                [0.018, 0.02, 0.02, 0.016])), BRONZE, "head", fuse=False, surface="metal")
    rc.mandible(k, "mandible", [(0.062, -0.255, 0.905), (0.088, -0.278, 0.892), (0.088, -0.296, 0.866),
                                (0.064, -0.302, 0.856)], [0.024, 0.02, 0.014, 0.006], "head")
    for s in (1, -1):
        antler(k, s)
    # antennae: segmented, sweeping back from the helm's top
    base, tip = np.array([0.1, -0.06, 1.1]), np.array([0.27, 0.12, 1.26])
    k.add("antenna", outward(tube([base, (base + tip) / 2 + (0, 0, 0.04), tip], [0.015, 0.014, 0.013])), DEEP,
          "head", fuse=False, mirror=True)
    for i, t in enumerate((0.25, 0.45, 0.65, 0.85)):
        q = base + (tip - base) * t + np.array([0, 0, 0.04 * 4 * t * (1 - t)])
        k.add(f"aseg{i}", outward(ell(q, (0.021, 0.021, 0.021), sub=3)), CHESTNUT, "head", fuse=False, mirror=True)
    # --- the wing cases on its back: parted down the middle, a bronze rim, iron rivets ------------------------
    k.add("case", case(EC, ER, -0.88, 0.975, 3, 112, 0.024), CHESTNUT, "body", fuse=False, mirror=True)
    k.add("caserim", case(EC, ER + 0.006, -0.86, 0.955, 103, 112.5, 0.024, nu=6), BRONZE, "body", fuse=False,
          mirror=True, surface="metal")
    k.add("caseseam", case(EC, ER + 0.006, -0.88, 0.97, 3, 6.5, 0.022, nu=4), BRONZE, "body", fuse=False, mirror=True,
          surface="metal")
    for i, p in enumerate((-0.55, -0.2, 0.15, 0.5, 0.78)):
        t = math.radians(97)
        q = math.sqrt(1 - p * p)
        at = EC + ER * np.array([q * math.sin(t), q * math.cos(t), p]) + np.array([0.012 * math.sin(t), 0.012 * math.cos(t), 0])
        k.add(f"rivet{i}", outward(ell(at, (0.017, 0.017, 0.017), sub=2)), RIVET, "body", fuse=False, mirror=True,
              surface="metal")
    # --- arms: iron pauldron, chestnut upper arm, iron vambrace with a bronze band and spike, a clawed hand -----
    k.add("upper", tube([(0.25, -0.04, 0.84), (0.31, -0.08, 0.75), (0.34, -0.12, 0.66)], [0.075, 0.068, 0.064]),
          CHESTNUT, "arm{s}", mirror=True)
    k.add("pauldron", outward(ell((0.3, -0.04, 0.875), (0.12, 0.115, 0.09), rot=(0, 22, 0),
                                  deform=lambda V: np.where(V[:, 2:3] < -0.35, V * [1, 1, 0.5], V))), IRON,
          "arm{s}", fuse=False, mirror=True, surface="metal")
    k.add("forearm", tube([(0.34, -0.12, 0.66), (0.32, -0.2, 0.58), (0.29, -0.26, 0.52)], [0.06, 0.058, 0.056]),
          CHESTNUT, "arm{s}", mirror=True)
    vd = unit(-0.045, -0.125, -0.12)
    k.add("vambrace", lathe((0.336, -0.135, 0.645), vd, [(0.0, -0.005), (0.072, -0.004), (0.082, 0.03), (0.08, 0.1),
                                                         (0.07, 0.15), (0.0, 0.151)]), IRON, "arm{s}", fuse=False,
          mirror=True, surface="metal")
    k.add("vband", ring_band(np.array((0.336, -0.135, 0.645)) + vd * 0.03, vd, 0.086, 0.025, thick=0.01), BRONZE,
          "arm{s}", fuse=False, mirror=True, surface="metal")
    sp = np.array((0.385, -0.2, 0.6))
    k.add("vspike", hull([sp + (-0.01, -0.02, -0.02), sp + (-0.01, 0.02, -0.02), sp + (-0.01, 0.02, 0.02),
                          sp + (-0.01, -0.02, 0.02), sp + (0.06, 0.03, 0.04)]), IRONLIGHT, "arm{s}", fuse=False,
          mirror=True, surface="metal")
    HAND = np.array([0.285, -0.3, 0.48])
    k.add("hand", ell(HAND, (0.058, 0.06, 0.062)), LEG, "arm{s}", mirror=True)
    for j, dx in enumerate((-0.035, 0.0, 0.035)):
        b0 = HAND + np.array([dx, -0.03, -0.035])
        k.add(f"claw{j}", hull([b0 + (-0.014, -0.012, 0.012), b0 + (0.014, -0.012, 0.012), b0 + (0.014, 0.014, 0.012),
                                b0 + (-0.014, 0.014, 0.012), b0 + (dx * 0.2, -0.045, -0.055)]), LEG, "arm{s}",
              fuse=False, mirror=True)
    # --- legs: thick thighs fused into the hips, iron knee caps, short armoured shins, clawed feet ------------
    k.add("thigh", ell((0.2, -0.04, 0.35), (0.14, 0.14, 0.15), rot=(0, -24, 0)), LEG, "leg{s}", mirror=True)
    k.add("shin", tube([(0.27, -0.09, 0.26), (0.285, -0.06, 0.16), (0.29, -0.03, 0.08)], [0.085, 0.075, 0.068]),
          LEG, "leg{s}", mirror=True)
    k.add("foot", ell((0.295, -0.09, 0.045), (0.09, 0.125, 0.048)), LEG, "leg{s}", mirror=True)
    k.add("knee", outward(ell((0.28, -0.145, 0.265), (0.078, 0.05, 0.074))), IRON, "leg{s}", fuse=False, mirror=True,
          surface="metal")
    k.add("greave", hull([(0.24, -0.15, 0.2), (0.33, -0.15, 0.2), (0.34, -0.12, 0.075), (0.25, -0.12, 0.075),
                          (0.29, -0.185, 0.14), (0.28, -0.1, 0.12)]), IRONLIGHT, "leg{s}", fuse=False, mirror=True,
          surface="metal")
    sp = np.array((0.33, 0.03, 0.17))
    k.add("spur", hull([sp + (-0.02, -0.015, -0.02), sp + (-0.02, -0.015, 0.02), sp + (-0.02, 0.015, 0.02),
                        sp + (-0.02, 0.015, -0.02), sp + (0.03, 0.06, 0.03)]), IRON, "leg{s}", fuse=False, mirror=True,
          surface="metal")
    for j, dx in enumerate((-0.045, 0.0, 0.045)):
        b0 = np.array([0.295 + dx, -0.2, 0.035])
        k.add(f"toe{j}", hull([b0 + (-0.016, 0.02, 0.022), b0 + (0.016, 0.02, 0.022), b0 + (0.016, 0.02, -0.02),
                               b0 + (-0.016, 0.02, -0.02), b0 + (dx * 0.3, -0.05, -0.028)]), IRON, "leg{s}",
              fuse=False, mirror=True, surface="metal")
    # --- paint: the plastron's plates, chevrons down its front with a line down the middle --------------------
    front = lambda P: smoothstep(-0.12, -0.17, P[:, 1])
    for z in (0.36, 0.47, 0.58, 0.69):
        k.paint(lambda P, z=z: inside(np.abs(P[:, 2] - z - 0.18 * np.abs(P[:, 0])) - 0.005, 0.002) * front(P)
                * inside(np.abs(P[:, 0]) - 0.17, 0.01), FOLD, on=["plastron"])
    k.paint(lambda P: inside(np.abs(P[:, 0]) - 0.0045, 0.002) * front(P) * inside(P[:, 2] - 0.76, 0.01)
            * inside(0.3 - P[:, 2], 0.01), FOLD, on=["plastron"])
    # the core is dark only under the wing cases
    k.paint(lambda P: smoothstep(-0.02, 0.04, P[:, 1]) * inside(0.19 - P[:, 2], 0.01), SEAM, on=["core", "trunk"])
    # the mouth under the snout
    M, Cm = srgb(rc.MOUTH), srgb(CREAM)
    def mouth(u, v):
        c = np.tile(Cm, (len(u), 1))
        c[np.abs(v - (0.25 - 0.55 * u * u)) < 0.4] = M
        return c
    k.decal((0, -0.262, 0.897), unit(0, -1, -0.35), (0.036, 0.013), mouth, eps=0.0025, rings=10, segs=48)
    k.build(voxel=0.0092, smooth=4, band=0.008, wband=0.06)
    rc.board(k)
    k.eye_states(lambda state: rc.lay_eyes(k, state, HC, HR, 30, 5, (0.054, 0.051)))
    k.export(out)


if __name__ == "__main__":
    radaskon(*args())
