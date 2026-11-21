# Mega Ampharos sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), its
# frame copied from ampharos.py (whose function takes no arguments) and its fleece made by mareep.py's own way
# (sibling("mareep"): the fused cloud of puffs, the banded horn ears, the dark round eyes with a white glint).
#   blender --background --factory-startup --python sculpts/ampharosmega.py -- <the export's Ampharos-Mega.glb> <out.glb>
#
# The design, from the references: Ampharos whose fleece has come back as long white hair: the largest mass is the
# tail, grown into a great white cloud of fleece curling out behind the legs and to one side, ending in a wisp that
# rises, five glowing red beads set in it; then a mane of long thick white locks flowing back and down from the back
# of the head behind the neck to the middle of the back; a curl of white fleece on the crown between the ears, the
# red jewel before it; the white belly grown from the neck's foot down between the legs; dark points on the feet.
# The rest is Ampharos's: the yellow pear of a body, the flipper arms forward and down, the neck ringed black three
# times, the long muzzle, the small dark eyes, the horn ears banded black.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, hull, ring_band, srgb, inside, unit, args, outward, sibling

m = sibling("mareep")
YELLOW, BELLY, BLACK = "#F6CC2E", "#F4F2EA", m.BLACK
FLEECE = "#F5F4F2"
JEWEL = "#E8263A"

# The tail's cloud: lobes of fleece along a curve back and out to +X, each (centre, radii)
CLOUD = [((0.03, 0.22, 0.2), (0.1, 0.095, 0.1)),
         ((0.1, 0.35, 0.21), (0.115, 0.11, 0.115)),
         ((0.17, 0.47, 0.27), (0.1, 0.095, 0.105))]
WISP = [(0.18, 0.5, 0.33), (0.17, 0.53, 0.44), (0.19, 0.5, 0.52), (0.22, 0.54, 0.6)]
BEADS = [(0.11, 0.24, 0.29), (0.2, 0.32, 0.14), (0.14, 0.45, 0.35), (0.26, 0.46, 0.2), (0.21, 0.37, 0.31)]


def pear(V):
    V = V.copy()
    s = 1 + 0.1 * np.clip(-V[:, 2], 0, 1) - 0.22 * np.clip(V[:, 2], 0, 1) ** 1.4
    V[:, 0] *= s; V[:, 1] *= s
    return V


def mane(k, H):
    """Long thick white locks from the back of the head, flowing back and down behind the neck, fanned side to side
    and curling up a little at their ends."""
    # (x, height of its root, length, how far it falls): a few thick locks of uneven length, one over another
    locks = [(0.0, 0.05, 1.0, 0.1), (-0.035, 0.01, 0.9, 0.2), (0.035, 0.0, 1.05, 0.25), (-0.02, -0.05, 0.8, 0.35),
             (0.025, -0.08, 0.7, 0.45)]
    for i, (x, lift, ln, fall) in enumerate(locks):
        s0 = H + np.array([x, 0.035, lift])
        pts = [s0,
               s0 + np.array([x * 0.4, 0.11 * ln, 0.02 - 0.15 * fall]),
               s0 + np.array([x * 0.8, 0.22 * ln, -0.02 - 0.35 * fall]),
               s0 + np.array([x * 1.2, 0.32 * ln, -0.08 - 0.5 * fall]),
               s0 + np.array([x * 1.5, 0.4 * ln, -0.13 - 0.58 * fall])]
        k.add(f"lock{i}", outward(tube(pts, [0.045, 0.062, 0.054, 0.036, 0.013], seg=18, per=6)), FLEECE, "head",
              fuse=False, surface="hair")


def ampharos_mega(src, out):
    k = Kit(src, coat="scales")
    B = np.array([0, 0.0, 0.33])

    # The body: a pear, fullest low down, shoulders narrowing into the neck
    k.add("body", ell(B, (0.135, 0.118, 0.175), deform=pear), YELLOW, "body")
    k.add("shoulders", ell((0, -0.005, 0.462), (0.098, 0.082, 0.07)), YELLOW, "body")

    # Thick thighs fused into the hips, short shins, broad feet with a dark point at the front
    for s, bone in ((1, "legR"), (-1, "legL")):
        x = np.array([s, 1, 1.0])
        k.add("thigh" + bone[-1], ell(np.array((0.088, -0.01, 0.205)) * x, (0.078, 0.088, 0.095)), YELLOW, bone)
        k.add("shin" + bone[-1], tube([np.array(p) * x for p in [(0.094, -0.018, 0.16), (0.098, -0.03, 0.05)]],
                                      [0.05, 0.043]), YELLOW, bone)
        k.add("foot" + bone[-1], ell(np.array((0.1, -0.05, 0.028)) * x, (0.05, 0.072, 0.03)), YELLOW, bone)
        t = np.array((0.1 * s, -0.116, 0.018))
        k.add("toe" + bone[-1], hull([t + (0, -0.012, -0.004), t + (0.02, 0.012, -0.014), t + (-0.02, 0.012, -0.014),
                                      t + (0.018, 0.012, 0.012), t + (-0.018, 0.012, 0.012), t + (0, 0.0, 0.014),
                                      t + (0.014, 0.03, 0.0), t + (-0.014, 0.03, 0.0)]), BLACK, bone, fuse=False,
              surface="shell")

    # The flipper arms: broad at the shoulder, tapering to a point, carried forward and down, faces to the front
    for s, bone in ((1, "armR"), (-1, "armL")):
        x = np.array([s, 1, 1.0])
        k.add("arm" + bone[-1], blade([np.array(p) * x for p in [(0.095, -0.03, 0.46), (0.15, -0.085, 0.42),
                                                                   (0.195, -0.135, 0.38), (0.22, -0.162, 0.355)]],
                                      [0.05, 0.044, 0.03, 0.012], np.array((0.45, -0.55, 0.7)) * x, 0.48),
              YELLOW, bone)

    # The white belly from the neck's foot down between the legs, rising to a point at the top
    def belly(P):
        e = ((P[:, 0] / 0.108) ** 2 + ((P[:, 2] - 0.34) / 0.18) ** 2)
        point = inside(np.abs(P[:, 0]) * 2.2 + P[:, 2] - 0.62, 0.006) * inside(P[:, 2] - 0.62)
        # the thighs keep their yellow: nothing within the thighs' own ellipsoids, a little grown
        tx = np.abs(P[:, 0]) - 0.088
        thigh = (tx / 0.086) ** 2 + ((P[:, 1] + 0.01) / 0.096) ** 2 + ((P[:, 2] - 0.205) / 0.104) ** 2
        return np.maximum(inside(np.sqrt(e) - 1, 0.012) * (P[:, 2] < 0.47), point) * (P[:, 1] < -0.02) \
            * (P[:, 2] > 0.15) * (1 - inside(thigh - 1, 0.03))
    k.paint(belly, BELLY)

    # The long thick neck, three black rings near its top
    k.add("necklow", tube([(0, 0.0, 0.45), (0, -0.008, 0.56)], [0.064, 0.054]), YELLOW, "body")
    NECK = [(0, -0.008, 0.54), (0, -0.016, 0.63), (0, -0.026, 0.72)]
    k.add("neckup", tube(NECK, [0.054, 0.05, 0.05]), YELLOW, "head")
    for z in (0.585, 0.625, 0.665):
        yy = -0.008 - 0.018 * (z - 0.54) / 0.18
        k.add(f"ring{int(z * 1000)}", ring_band((0, yy, z), (0, -0.1, 1), 0.0505, 0.022, thick=0.007), BLACK, "head",
              fuse=False, surface="scales")

    # The head: a round skull, a long rounded muzzle standing forward and a little down
    H, HR = np.array([0, -0.032, 0.765]), np.array([0.084, 0.087, 0.082])
    HEAD = ell(H, HR)
    k.add("head", HEAD, YELLOW, "head")
    k.add("muzzle", tube([(0, -0.07, 0.758), (0, -0.14, 0.738), (0, -0.182, 0.72)], [0.056, 0.043, 0.034]), YELLOW,
          "head")
    k.add("jowl", ell((0, -0.06, 0.725), (0.062, 0.06, 0.045)), YELLOW, "head")

    # The curl of fleece on the crown, between and behind the ears
    m.wool(k, "crown", (0, -0.01, 0.832), (0.066, 0.064, 0.042), "head", col=FLEECE, n=18, puff=0.034,
           sink=0.012, skip=lambda d: d[2] < -0.45, seed=3)

    # The jewel on the front of the fleece over the brow, glowing
    gd = unit(0, -0.83, 0.55)                       # set in the front of the crown's fleece
    gp = np.array([0, -0.01, 0.832]) + gd * 0.082
    k.add("jewel", ell(gp + gd * 0.012, (0.03, 0.03, 0.03), sub=4), JEWEL, "head", fuse=False, glow=JEWEL,
          surface="shell")

    # The ears: horns rising up and out, banded black in the middle and at the point
    m.ear_cone(k, "ear", np.array([0.042, -0.03, 0.805]), unit(0.42, 0.14, 0.9), 0.122, 0.035, YELLOW,
               [(0.36, 0.53, BLACK), (0.72, 1.0, BLACK)], "ear{s}")

    mane(k, H)

    # The tail: a great cloud of fleece curling back and out to +X, a wisp rising from its end, red beads in it
    k.add("tailroot", tube([(0, 0.08, 0.25), (0.01, 0.16, 0.21), (0.03, 0.22, 0.2)], [0.06, 0.07, 0.08]), FLEECE, "tail")
    for i, (cc, rr) in enumerate(CLOUD):
        m.wool(k, f"cloud{i}", cc, rr, "tail", col=FLEECE, n=30, puff=0.05, sink=0.02, seed=7 + i)
    k.add("wisp", outward(tube(WISP, [0.055, 0.038, 0.026, 0.017], seg=16, per=6)), FLEECE, "tail", fuse=False,
          surface="hair")
    for i, b in enumerate(BEADS):
        cc, rr = min(CLOUD, key=lambda e: np.linalg.norm(np.array(e[0]) - b))
        cc, rr = np.array(cc), np.array(rr)
        d = np.array(b, float) - cc
        p = cc + d / math.sqrt(((d / rr) ** 2).sum()) + unit(*(d / rr ** 2)) * 0.045   # on the fleece's puffs
        k.add(f"bead{i}", ell(p, (0.03, 0.03, 0.03), sub=3), JEWEL, "tail", fuse=False, glow=JEWEL, surface="shell")

    k.build(voxel=0.0075, smooth=4, band=0.005)

    de = unit(0.55, -0.76, 0.3)
    E = m.hit(HEAD, H + np.array([0, -0.01, 0.008]) + de * 0.4, -de)
    k.eye_states(m.lay_dark_eyes(k, E, de, (0.021, 0.026), YELLOW))
    k.export(out)


if __name__ == "__main__":
    ampharos_mega(*args())
