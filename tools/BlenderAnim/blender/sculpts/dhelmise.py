# Dhelmise sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/dhelmise.py -- <the export's Dhelmise.glb> <out.glb>
#
# From the references: a hovering anchor taken over by seaweed. The anchor is a broad rust-brown crescent with dark
# spots, its two arms rising to arrowhead flukes at the top sides. Teal seaweed covers its shank from the crown up to
# a fist-like knob at the top (a little cone standing forward from it), from which three fronds rise like horns (left,
# middle, and a broad leaf curving up to the right) and two small leaves hang; a band of it wraps the anchor's crown,
# the rust showing through in triangles, and hangs below in seven wavy streamers. Behind the knob stands a ship's
# wheel: a mauve wooden rim, a yellow ring inside it, yellow spokes ending in yellow handles past the rim. Its eye is a
# porthole on the wheel's rim at its right (+X): a yellow ring round a pale cream face with a red-orange slit pupil.
# Hard-edged pieces (the anchor, the flukes, the wheel) are loose; the seaweed and the eye's face are fused.
# The skeleton: root, body (0, 0, 0.454) alone. Hovering, 0.1 to 0.89.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, slab, hull, lathe, ring_band, srgb, inside, unit, eye_look, polygon, \
    boxy, args

TEAL, RUST, SPOT = "#2C929E", "#B06E38", "#4A3830"
MAUVE, YELLOW = "#9A7088", "#F0CE34"
CREAM, PUPIL, PUPILRIM, INK = "#F4EEC4", "#E8542A", "#7A1C14", "#3A2418"

OC = np.array([0.0, 0.0, 0.505])            # the anchor crescent's circle: its centre and outer radius
OR = 0.24
ARM = 0.205                                 # the arms' middle line
WC = np.array([0.025, 0.055, 0.6])          # the wheel's centre
EYE_A = 20.0                                # the eye's angle on the rim (degrees from +X, up)
VOXEL = 0.0062


def xz(a_deg, r, y=0.0, c=OC):
    a = math.radians(a_deg)
    return np.array([c[0] + r * math.cos(a), c[1] + y, c[2] + r * math.sin(a)])


def anchor(k):
    """The crescent (an outline extruded), arrowhead flukes at its ends, dark spots on both faces."""
    A = np.linspace(170, 370, 41)
    width = lambda a: 0.068 + 0.03 * max(0.0, -math.sin(math.radians(a)))
    outer = [(OR * math.cos(math.radians(a)), OC[2] + OR * math.sin(math.radians(a))) for a in A]
    inner = [((OR - width(a)) * math.cos(math.radians(a)), OC[2] + (OR - width(a)) * math.sin(math.radians(a)))
             for a in A[::-1]]
    k.add("anchor", slab(outer + inner, (0, 0, 0), (1, 0, 0), (0, 0, 1), 0.046), RUST, "body", fuse=False,
          surface="metal")
    for s, a in ((1, 10.0), (-1, 170.0)):
        end = xz(a, OR - width(a) / 2)
        t = np.array([0.26 * s, 0, 0.966])             # pointing up and a little out
        n = np.array([0.966, 0, -0.26 * s])
        apex, base = end + t * 0.082, end - t * 0.012
        pts = []
        for y in (-0.024, 0.024):
            for p in (apex, base + n * 0.062, base - n * 0.062, end + t * 0.012):
                pts.append(p + (0, y, 0))
        k.add(f"fluke{'R' if s > 0 else 'L'}", hull(pts), RUST, "body", fuse=False, surface="metal")
    for a, r in ((-14, 0.017), (2, 0.014), (-2, 0.011), (194, 0.016), (178, 0.013), (186, 0.011)):
        rr = ARM + (0.012 if a in (-2, 186) else -0.004)
        p = xz(a, rr)
        for y in (-1, 1):
            k.add(f"spot{a}{y}", ell(p + (0, y * 0.0228, 0), (r, 0.0045, r * 1.25), sub=3), SPOT, "body",
                  fuse=False, surface="metal")


def wheel(k):
    """The wheel behind: a wooden rim, a yellow ring, eight yellow spokes and handles past the rim."""
    Y = np.array([0, 1.0, 0])
    k.add("rim", ring_band(WC, Y, 0.19, 0.038, thick=0.046), MAUVE, "body", fuse=False, surface="shell")
    k.add("ring", ring_band(WC + (0, -0.004, 0), Y, 0.128, 0.03, thick=0.02), YELLOW, "body", fuse=False,
          surface="metal")
    k.add("hub", ell(WC, (0.035, 0.025, 0.035), sub=3), YELLOW, "body", fuse=False, surface="metal")
    for i in range(8):
        a = EYE_A + 45 * i
        d = np.array([math.cos(math.radians(a)), 0, math.sin(math.radians(a))])
        n = np.array([-d[2], 0, d[0]])
        a0, a1 = WC + d * 0.02, WC + d * 0.205
        pts = [p + n * sx * 0.012 + Y * sy * 0.011 for p in (a0, a1) for sx in (-1, 1) for sy in (-1, 1)]
        k.add(f"spoke{i}", hull(pts), YELLOW, "body", fuse=False, surface="metal")
        if i == 0:
            start, prof = 0.245, [(0, 0), (0.014, 0), (0.017, 0.012), (0.012, 0.026), (0.019, 0.05),
                                  (0.016, 0.064), (0, 0.07)]
        else:
            start, prof = 0.2, [(0, 0), (0.014, 0), (0.016, 0.02), (0.012, 0.035), (0.019, 0.06), (0.016, 0.078),
                                (0, 0.084)]
        k.add(f"handle{i}", lathe(WC + d * start, d, prof, seg=20), YELLOW, "body", fuse=False, surface="metal")


EYE_C = WC + 0.19 * np.array([math.cos(math.radians(EYE_A)), 0, math.sin(math.radians(EYE_A))]) + (0, -0.043, 0)


def porthole(k):
    """The eye's porthole: a fused cream face (for the eye's patches) in a loose yellow ring."""
    k.add("eyeface", ell(EYE_C, (0.05, 0.016, 0.05)), CREAM, "body", surface="shell")
    k.add("eyering", ring_band(EYE_C + (0, 0.004, 0), (0, 1, 0), 0.055, 0.03, thick=0.016), YELLOW, "body",
          fuse=False, surface="metal")


def weed(k):
    """The seaweed: the shank to a knob, the fronds, the band round the crown and its streamers below."""
    k.add("shank", tube([(0, 0.0, 0.3), (0, -0.005, 0.42), (0, -0.012, 0.55), (0, -0.02, 0.62)],
                        [0.072, 0.05, 0.045, 0.05]), TEAL, "body")
    k.add("knob", ell((0, -0.026, 0.655), (0.076, 0.062, 0.084), deform=boxy(2.6)), TEAL, "body")
    k.add("nub", tube([(-0.008, -0.07, 0.665), (-0.016, -0.108, 0.672)], [0.032, 0.008]), TEAL, "body")
    Y = (0, 1, 0)
    fronds = (
        ([(0.0, -0.03, 0.71), (0.012, -0.03, 0.78), (0.005, -0.025, 0.84), (-0.026, -0.02, 0.885)],
         [0.03, 0.023, 0.017, 0.012], 0.6),
        ([(-0.035, -0.028, 0.7), (-0.075, -0.026, 0.76), (-0.112, -0.022, 0.79), (-0.142, -0.018, 0.784)],
         [0.027, 0.022, 0.017, 0.012], 0.6),
        ([(0.035, -0.028, 0.7), (0.085, -0.026, 0.752), (0.128, -0.022, 0.8), (0.155, -0.018, 0.852),
          (0.162, -0.015, 0.884)], [0.026, 0.036, 0.03, 0.019, 0.012], 0.45),
        ([(-0.05, -0.04, 0.625), (-0.086, -0.046, 0.6), (-0.112, -0.046, 0.572)], [0.021, 0.018, 0.012], 0.5),
        ([(0.05, -0.04, 0.632), (0.076, -0.046, 0.606), (0.094, -0.046, 0.585)], [0.019, 0.016, 0.012], 0.5),
    )
    for i, (pts, rad, f) in enumerate(fronds):
        k.add(f"frond{i}", blade(pts, rad, Y, f), TEAL, "body")
    # the band round the crown, over both faces of the anchor
    angs = [204, 224, 247, 270, 293, 316, 336]
    rad = [0.042, 0.056, 0.064, 0.07, 0.064, 0.056, 0.042]
    k.add("band", blade([xz(a, ARM - 0.004) for a in angs], rad, Y, 0.62), TEAL, "body")
    # the streamers hanging below in waves, the outer ones curling out
    for i, x in enumerate((-0.165, -0.11, -0.055, 0.0, 0.055, 0.11, 0.165)):
        s = 1 if x >= 0 else -1
        zc = OC[2] - math.sqrt(ARM ** 2 - x ** 2) - 0.01
        end = 0.1 if i == 3 else (0.13 if i in (2, 4) else (0.15 if i in (1, 5) else 0.175))
        L = zc - end
        flare = 0.03 * abs(x) / 0.165
        w = 0.016 * (1 if i % 2 else -1)
        pts = [(x, 0.0, zc), (x + w, -0.004, zc - L * 0.3), (x - w + s * flare, 0.0, zc - L * 0.58),
               (x + w + s * flare * 2, 0.004, zc - L * 0.82), (x + s * flare * 2.6, 0.0, end)]
        k.add(f"streamer{i}", tube(pts, [0.026, 0.021, 0.018, 0.015, 0.0125]), TEAL, "body")


def notches(k):
    """The rust showing through the band in triangles, front and back (patches over the fused band)."""
    tri_ = polygon([(-1, -0.65), (1, -0.65), (0, 1)])
    for a in (224, 246, 294, 316):
        p = xz(a, ARM - 0.03)
        up = unit(-(p[0] - OC[0]), 0, -(p[2] - OC[2]))      # pointing up, toward the crescent's centre
        for n in ((0, -1, 0), (0, 1, 0)):
            k.decal(p, n, (0.019, 0.03), RUST, eps=0.003, outline=tri_, rings=8, segs=36, up=up)


def eyes(k):
    CC, CP, CR = srgb(CREAM), srgb(PUPIL), srgb(PUPILRIM)
    def eye(u, v):
        c = np.tile(CC, (len(u), 1))
        lens = np.abs(u) - 0.3 * (1 - (v / 0.86) ** 2)
        c[(lens < 0) & (np.abs(v) < 0.86)] = CR
        c[(lens < -0.08) & (np.abs(v) < 0.74)] = CP
        c[(np.abs(u) < 0.035) & (np.abs(v) < 0.55)] = CR
        return c
    n = unit(0.12, -1, 0)
    c = EYE_C + (0, -0.016, 0)
    def lay(state):
        k.decal(c, n, (0.044, 0.044), eye_look(state, eye, CC, srgb(INK)), eps=0.003, rings=20, segs=56)
    k.eye_states(lay)


def dhelmise(src, out):
    k = Kit(src, coat="leaf")
    anchor(k)
    wheel(k)
    porthole(k)
    weed(k)
    notches(k)
    k.build(voxel=VOXEL, band=0.006, wband=0.04)
    eyes(k)
    k.export(out)


if __name__ == "__main__":
    dhelmise(*args())
