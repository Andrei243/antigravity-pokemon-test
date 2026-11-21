# Mega Skarmory sculpted from nothing on the skeleton and clips of the game's own export of the form (sculptkit.py):
#   blender --background --factory-startup --python sculpts/skarmorymega.py -- <the export's Skarmory-Mega.glb> <out.glb>
#
# Grown from skarmory.py (its egg of a body, its segmented neck, its helmet of a head and the wing's fan of slab blades,
# whose frame and outline are borrowed through sibling). From the references: the steel turned gold and the blades
# navy. The wings are the great change: long spears fanned up and back, navy and gold in turn, far longer than the
# base's red blades. The beak is closed and longer, a navy crest swept back off the crown beside a gold one, the eye red
# in a navy mask. The legs run back from under the body to a navy joint, and from each a sheaf of long blades hangs down
# and back; a navy crescent rises over the rump where the base had its hooked tail.
# The skeleton (the base's): body (0, 0, 0.414), wingR/L (+-0.06, -0.02, 0.484), tail (0, 0.08, 0.324),
# head (0, -0.07, 0.524), legR/L (+-0.04, 0.02, 0.324).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, hull, ring_band, srgb, inside, unit, sibling, args

sp = sibling("spearow")
sk = sibling("skarmory")

GOLD, GOLD2, NAVY, NAVY2 = "#E2B232", "#C8992A", "#2C3768", "#212A56"
MOUTH, RED, PUPIL, INK = "#B8323A", "#D8283A", "#141018", "#1C2244"

H = sk.H                                    # the head's centre, as the base's
HR = sk.HR
ELEV, SWEEP = 46, 58
ROOT = np.array([0.07, 0.01, 0.5])


def wing_axes():
    e, s = math.radians(ELEV), math.radians(SWEEP)
    ex = np.array([math.cos(e) * math.cos(s), math.cos(e) * math.sin(s), math.sin(e)])
    ez = np.array([0.15, 0.6, -0.8])
    ez = ez - ex * (ez @ ex)
    return ex, ez / np.linalg.norm(ez)


def wing(k):
    """The wing's spears: a fan from along the span back toward the chord, navy and gold in turn, the longest first."""
    ex, ez = wing_axes()
    n = np.cross(ex, ez)
    fan = ((0.04, 0.01, 2, 0.6, 0.07, NAVY), (0.07, 0.04, 13, 0.55, 0.068, GOLD), (0.09, 0.07, 24, 0.5, 0.066, NAVY2),
           (0.1, 0.1, 36, 0.44, 0.064, GOLD2), (0.1, 0.13, 50, 0.37, 0.06, NAVY), (0.09, 0.15, 66, 0.3, 0.056, GOLD))
    for j, (a0, b0, ang, L, w, col) in enumerate(fan):
        off = ROOT + n * (0.009 if j % 2 else -0.005)
        k.add(f"blade{j}", slab(sk.blade_outline(a0, b0, ang, L, w), off, ex, ez, 0.028), col, "wing{s}", fuse=False,
              mirror=True, surface="shell")
    # the gold plate along the leading edge over the spears' roots
    plate = [(0.0, -0.04), (0.14, -0.05), (0.26, -0.02), (0.18, 0.05), (0.05, 0.07), (0.0, 0.05)]
    k.add("plate", slab(plate, ROOT - n * 0.016, ex, ez, 0.034), GOLD, "wing{s}", fuse=False, mirror=True)
    k.add("shoulder", ell(ROOT + ex * 0.03, (0.05, 0.06, 0.05)), GOLD, "wing{s}", mirror=True)


def leg_sheaf(k, s, side):
    """A gold leg running back and down from the thigh to a navy joint, a sheaf of long blades hanging from it."""
    m = np.array([s, 1, 1.0])
    knee = np.array([0.065, 0.06, 0.27])
    ankle = np.array([0.08, 0.17, 0.225])
    k.add(f"shin{side}", tube([knee * m, ((knee + ankle) / 2 + (0, 0, -0.01)) * m, ankle * m], [0.024, 0.021, 0.022]),
          GOLD, f"leg{side}", fuse=False, surface="metal")
    k.add(f"joint{side}", ell(ankle * m, (0.03, 0.034, 0.03), sub=3), NAVY, f"leg{side}", fuse=False)
    for j, (dy, dz, L, col) in enumerate(((0.6, -1.0, 0.24, GOLD), (0.9, -0.9, 0.3, NAVY), (1.0, -0.6, 0.33, GOLD2),
                                         (1.0, -0.3, 0.3, NAVY2))):
        d = unit(0.12 * s + 0.04 * s * j, dy, dz)
        base = ankle * m + np.array([0.004 * s * j, 0.0, 0.0])
        k.add(f"sheaf{j}{side}", sp.spike(base, base + d * L, 0.05, 0.016, up=(0, 0, 1)), col, f"leg{side}",
              fuse=False, surface="shell")


def skarmorymega(src, out):
    k = Kit(src, coat="metal")

    # The base's egg of a body, gold, its breast plated in navy seams
    k.add("body", ell((0, 0.03, 0.405), (0.115, 0.16, 0.14), rot=(-14, 0, 0)), GOLD, "body")
    k.add("breast", ell((0, -0.06, 0.45), (0.098, 0.09, 0.11)), GOLD, "body")
    for z0 in (0.35, 0.42):
        k.paint(lambda P, z0=z0: inside(np.abs(P[:, 2] - z0 - 0.25 * (P[:, 1] + 0.05)) - 0.004, 0.002)
                * (P[:, 1] < 0.0), GOLD2, on=["body", "breast"])

    # The neck: thick, navy, in segments, reaching forward to the head
    NECK = [(0, -0.07, 0.5), (0, -0.115, 0.58), (0, -0.145, 0.66), H + (0, 0.03, -0.035)]
    k.add("neck", tube(NECK, [0.064, 0.055, 0.05, 0.047]), NAVY, "head")
    for t, r in ((0.2, 0.063), (0.45, 0.058), (0.7, 0.053)):
        a = np.array(NECK[0]) * (1 - t) + np.array(NECK[3]) * t
        d = np.array(NECK[3]) - np.array(NECK[0])
        k.add(f"segment{t}", ring_band(a, d, r, 0.03, thick=0.008), NAVY2, "head", fuse=False)

    # The helmet of a head, a long closed beak with a red line between its halves, the crests swept back
    k.add("head", ell(H, HR), GOLD, "head")
    k.add("cheek", ell(H + (0, -0.04, -0.015), (0.055, 0.06, 0.045)), GOLD, "head")
    B = H + np.array([0, -0.07, 0.012])
    k.add("beak", hull([B + (0.036, 0.0, 0.02), B + (-0.036, 0.0, 0.02), B + (0.032, 0.01, -0.03), B + (-0.032, 0.01, -0.03),
                        B + (0.012, -0.14, -0.012), B + (-0.012, -0.14, -0.012), B + (0, -0.24, -0.05)]),
          GOLD, "head", fuse=False)
    k.add("jaw", hull([B + (0.03, 0.01, -0.034), B + (-0.03, 0.01, -0.034), B + (0.02, 0.0, -0.06), B + (-0.02, 0.0, -0.06),
                       B + (0, -0.17, -0.062), B + (0.006, -0.13, -0.045), B + (-0.006, -0.13, -0.045)]),
          GOLD2, "head", fuse=False)
    k.add("mouth", hull([B + (0.026, 0.0, -0.028), B + (-0.026, 0.0, -0.028), B + (0.024, 0.0, -0.04),
                         B + (-0.024, 0.0, -0.04), B + (0, -0.16, -0.05), B + (0.006, -0.12, -0.035),
                         B + (-0.006, -0.12, -0.035)]), MOUTH, "head", fuse=False)
    k.add("crest", sp.spike(H + (0, 0.0, 0.04), H + (0, 0.2, 0.15), 0.065, 0.022, up=(0, 0, 1)), NAVY, "head",
          fuse=False)
    k.add("crest2", sp.spike(H + (0, 0.03, 0.02), H + (0, 0.19, 0.04), 0.05, 0.02, up=(0, 0, 1)), GOLD, "head",
          fuse=False)
    k.add("ridge", sp.spike(H + (0, -0.09, 0.03), H + (0, 0.04, 0.07), 0.04, 0.016, up=(0, 0, 1)), GOLD, "head",
          fuse=False)

    wing(k)

    # The navy crescent over the rump, its points up, turned out to +X so both cameras see its face
    cres = []
    for t in np.linspace(0, 1, 9):
        a = math.radians(-20 + 220 * t)
        cres.append((0.085 * math.cos(a), 0.085 * math.sin(a)))
    for t in np.linspace(1, 0, 9):
        a = math.radians(-20 + 220 * t)
        cres.append((0.05 * math.cos(a) + 0.0, 0.05 * math.sin(a) + 0.03))
    k.add("crescent", slab(cres, (0.02, 0.17, 0.455), unit(0.55, 0.83, 0), (0, 0, 1), 0.03), NAVY, "tail", fuse=False,
          surface="shell")
    k.add("rump", ell((0, 0.14, 0.39), (0.05, 0.05, 0.045)), GOLD, "tail")

    # Thighs of gold, legs running back to the sheaves of blades
    k.add("thigh", ell((0.06, 0.03, 0.29), (0.05, 0.055, 0.05)), GOLD, "leg{s}", mirror=True)
    for s, side in ((1, "R"), (-1, "L")):
        leg_sheaf(k, s, side)

    # The navy mask round each eye, running back from it
    k.decal(H + np.array([0.058, -0.035, 0.012]), unit(0.8, -0.45, 0.15), (0.05, 0.03), NAVY, up=(0, 0, 1),
            eps=0.004, rings=12, segs=48, mirror=True)
    k.build(voxel=0.0065)

    look = sp.bird_eye(srgb(RED), srgb(PUPIL))
    de = unit(0.62, -0.72, 0.22)
    E = H + de / np.linalg.norm(de / HR)

    def eyes(state):
        sp.lay_eyes(k, state, E, de, (0.03, 0.026), look, lambda u: 0.3 + 0.35 * u, lambda u: -0.05 + 0.6 * u,
                    srgb(NAVY), srgb(GOLD))

    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    skarmorymega(*args())
