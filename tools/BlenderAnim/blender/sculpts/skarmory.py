# Skarmory sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/skarmory.py -- <the export's Skarmory.glb> <out.glb>
#
# From the references: a bird of silver-grey steel. An egg of a body, plated; a long neck of dark grey-blue segments
# rising forward to a large helmet of a head: a long pointed beak hanging open, red within, a crest swept back off the
# crown to a point and a second point behind; small yellow eyes with black pupils under a sharp brow. Wings raised and
# swept back: a long grey blade along the leading edge, and under it a fan of long red blade feathers sheathed in grey
# at their roots, their points trailing back. A dark grey tail curving down behind into a hook. Thin dark grey legs
# under steel thighs, three claws. (Wing, tail and neck from the pictures; spearow.py's spike, foot and eye cut.)
# The skeleton: body (0, 0, 0.414), wingR/L (+-0.06, -0.02, 0.484), tail (0, 0.08, 0.324), head (0, -0.07, 0.524),
# legR/L (+-0.04, 0.02, 0.324).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, hull, ring_band, srgb, inside, smoothstep, unit, sibling, args

sp = sibling("spearow")

STEEL, STEEL2, PALE = "#C4CCDD", "#AEB8CE", "#DCE2EE"
DARK, DARK2 = "#4E5674", "#3C4360"
RED, RED2 = "#C0343C", "#A82A34"
MOUTH, YELLOW, PUPIL, INK = "#E2666C", "#F2CC3A", "#141018", "#20243A"

H = np.array([0.0, -0.17, 0.74])          # the head's centre
HR = np.array([0.072, 0.097, 0.068])
ELEV, SWEEP = 46, 40
ROOT = np.array([0.075, 0.0, 0.5])


def wing_axes():
    e, s = math.radians(ELEV), math.radians(SWEEP)
    ex = np.array([math.cos(e) * math.cos(s), math.cos(e) * math.sin(s), math.sin(e)])
    ez = np.array([0.25, 0.55, -0.8])
    ez = ez - ex * (ez @ ex)
    return ex, ez / np.linalg.norm(ez)


def blade_outline(a0, b0, ang, length, width):
    """A long pointed blade in the wing's plane from (a0, b0), turned ang degrees from the span toward the chord."""
    t = math.radians(ang)
    d = np.array([math.cos(t), math.sin(t)]); s = np.array([-d[1], d[0]])
    r = np.array([a0, b0])
    pts = [r - s * width * 0.5, r + d * length * 0.5 - s * width * 0.55, r + d * length * 0.82 - s * width * 0.3,
           r + d * length, r + d * length * 0.8 + s * width * 0.32, r + d * length * 0.5 + s * width * 0.55,
           r + s * width * 0.5]
    return [tuple(p) for p in pts]


def wing(k):
    ex, ez = wing_axes()
    n = np.cross(ex, ez)
    # the red blades, fanned from the span toward the trailing edge, each a hair off its neighbour's plane
    for j in range(4):
        off = ROOT + n * (0.004 if j % 2 else -0.004) + n * 0.006
        k.add(f"blade{j}", slab(blade_outline(0.03 + 0.04 * j, 0.03 + 0.03 * j, 9 + 8 * j, 0.37 - 0.045 * j, 0.06),
                                off, ex, ez, 0.026), RED if j % 2 == 0 else RED2, "wing{s}", fuse=False, mirror=True,
              surface="shell")
        # its grey sheath over the root
        k.add(f"sheath{j}", slab(blade_outline(0.03 + 0.04 * j, 0.03 + 0.03 * j, 9 + 8 * j, 0.15, 0.068),
                                 off, ex, ez, 0.034), STEEL2, "wing{s}", fuse=False, mirror=True)
    # the long grey blade along the leading edge, over the red ones
    plate = [(0.0, -0.035), (0.2, -0.05), (0.44, -0.03), (0.32, 0.03), (0.1, 0.06), (0.0, 0.05)]
    k.add("plate", slab(plate, ROOT - n * 0.012, ex, ez, 0.032), STEEL, "wing{s}", fuse=False, mirror=True)
    k.add("shoulder", ell(ROOT + ex * 0.03, (0.05, 0.06, 0.05)), STEEL, "wing{s}", mirror=True)


def skarmory(src, out):
    k = Kit(src, coat="metal")

    # An egg of a body, leaning into its flight, its breast full
    k.add("body", ell((0, 0.03, 0.405), (0.115, 0.16, 0.14), rot=(-14, 0, 0)), STEEL, "body")
    k.add("breast", ell((0, -0.06, 0.45), (0.098, 0.09, 0.11)), STEEL, "body")
    # plate seams across the breast and belly
    for z0 in (0.35, 0.44):
        k.paint(lambda P, z0=z0: inside(np.abs(P[:, 2] - z0 - 0.25 * (P[:, 1] + 0.05)) - 0.004, 0.002)
                * (P[:, 1] < 0.0), STEEL2, on=["body", "breast"])

    # The neck: thick, dark, in segments, rising forward to the head
    NECK = [(0, -0.07, 0.5), (0, -0.115, 0.58), (0, -0.145, 0.66), H + (0, 0.03, -0.035)]
    k.add("neck", tube(NECK, [0.062, 0.052, 0.047, 0.046]), DARK, "head")
    for t, r in ((0.2, 0.061), (0.45, 0.055), (0.7, 0.051)):
        a = np.array(NECK[0]) * (1 - t) + np.array(NECK[3]) * t
        d = np.array(NECK[3]) - np.array(NECK[0])
        k.add(f"segment{t}", ring_band(a, d, r, 0.03, thick=0.008), STEEL2, "head", fuse=False)

    # The helmet of a head, a long open beak, a crest swept back and a point behind
    k.add("head", ell(H, HR), STEEL, "head")
    k.add("cheek", ell(H + (0, -0.04, -0.015), (0.055, 0.06, 0.045)), STEEL, "head")
    B = H + np.array([0, -0.07, 0.012])
    k.add("beak", hull([B + (0.035, 0.0, 0.02), B + (-0.035, 0.0, 0.02), B + (0.03, 0.01, -0.025), B + (-0.03, 0.01, -0.025),
                        B + (0.012, -0.12, -0.01), B + (-0.012, -0.12, -0.01), B + (0, -0.2, -0.045)]),
          STEEL, "head", fuse=False)
    k.add("jaw", hull([B + (0.03, 0.01, -0.04), B + (-0.03, 0.01, -0.04), B + (0.02, 0.0, -0.065), B + (-0.02, 0.0, -0.065),
                       B + (0, -0.12, -0.075), B + (0.005, -0.1, -0.06), B + (-0.005, -0.1, -0.06)]),
          STEEL2, "head", fuse=False)
    k.add("mouth", hull([B + (0.022, 0.0, -0.025), B + (-0.022, 0.0, -0.025), B + (0.02, 0.0, -0.045),
                         B + (-0.02, 0.0, -0.045), B + (0, -0.13, -0.045), B + (0.008, -0.1, -0.03),
                         B + (-0.008, -0.1, -0.03)]), MOUTH, "head", fuse=False)
    k.add("crest", sp.spike(H + (0, 0.0, 0.04), H + (0, 0.17, 0.12), 0.06, 0.02, up=(0, 0, 1)), STEEL, "head",
          fuse=False)
    k.add("crest2", sp.spike(H + (0, 0.04, 0.0), H + (0, 0.15, -0.02), 0.04, 0.018, up=(0, 0, 1)), STEEL2, "head",
          fuse=False)
    k.add("ridge", sp.spike(H + (0, -0.09, 0.03), H + (0, 0.04, 0.07), 0.04, 0.016, up=(0, 0, 1)), STEEL, "head",
          fuse=False)

    wing(k)

    # The tail: dark, running back level and out to +X, a blade at its end
    k.add("tail", tube([(0, 0.13, 0.39), (0.04, 0.25, 0.385), (0.1, 0.33, 0.38), (0.16, 0.39, 0.38)],
                       [0.042, 0.034, 0.028, 0.022]), DARK, "tail", fuse=False)
    k.add("tailfin", sp.spike((0.14, 0.37, 0.38), (0.22, 0.46, 0.39), 0.06, 0.02, up=(0, 0, 1)), DARK, "tail",
          fuse=False)
    k.add("rump", ell((0, 0.14, 0.39), (0.05, 0.05, 0.045)), STEEL, "tail")

    # Thighs of steel, short thick dark legs hanging tucked, three claws and one behind
    k.add("thigh", ell((0.06, 0.025, 0.29), (0.05, 0.055, 0.05)), STEEL, "leg{s}", mirror=True)
    for side in ((1, "R"), (-1, "L")):
        sp.foot(k, side, (0.062, 0.02, 0.27), (0.066, 0.0, 0.215), 0.05, DARK, DARK2, r=0.022)

    look = sp.bird_eye(srgb(YELLOW), srgb(PUPIL))
    de = unit(0.62, -0.72, 0.22)
    E = H + de / np.linalg.norm(de / HR)

    def eyes(state):
        sp.lay_eyes(k, state, E, de, (0.03, 0.026), look, lambda u: 0.3 + 0.35 * u, lambda u: -0.05 + 0.6 * u,
                    srgb(STEEL), srgb(INK))

    k.build(voxel=0.0065)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    skarmory(*args())
