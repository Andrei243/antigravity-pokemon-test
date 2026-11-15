# Ho-Oh sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/hooh.py -- <the export's Ho-Oh.glb> <out.glb>
# From the references: a great red phoenix, white from the waist down, a long red neck with a green ring round it, a
# small head under a crest of rounded gold lobes curling back, a long yellow beak held open (red inside), a red eye in a
# black ring; great red wings raised in a V and swept back, a fringe of long feathers along each, every feather red at
# the root, then a white band, then a green tip; a fan of long gold tail feathers spread down behind; short dark
# blue-grey legs tucked under the white belly, the toes curled.
# The export's joints: body (0, 0, 0.469), head (0, -0.04, 0.619), wings (+-0.08, 0.02, 0.549), tail (0, 0.1, 0.369),
# legs (+-0.045, 0, 0.359).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, slab, ring_band, outward, srgb, inside, unit, eye_look, sibling, args

a = sibling("articuno")          # the wing's axes and the round-ended feather outline

RED, RED2, WHITE, GREEN = "#E8452E", "#D63A28", "#F7F4EC", "#8CCB2E"
GOLD, GOLD2, PALEGOLD = "#F7C22E", "#EDA62A", "#FBE58A"
BEAK, MOUTH, LEG, CLAW = "#F6CE34", "#D8344A", "#5E7898", "#3A465A"
RIM, IRIS, PUPIL, GLINT = "#1C1418", "#D8283A", "#3A0A12", "#FFFFFF"

H = np.array([0.0, -0.13, 0.83])           # the head's centre, high on the long neck
HR = np.array([0.064, 0.078, 0.068])


def feather(k, name, ex, ez, n, root, a0, ang, L, w, thick, bone, colours, bands=(0.56, 0.8), lift=0.0):
    """
    A long feather of the wing, root to tip in three crisp bands (each band a plate a little thicker and wider than the
    one under it, so it shows on both faces): colours = (root, middle, tip).
    """
    t = math.radians(ang)
    d = np.array([math.sin(t), math.cos(t)])
    s = np.array([d[1], -d[0]])
    org = root + n * lift
    k.add(name, slab(a.feather_outline(a0, 0.02, ang, L, w), org, ex, ez, thick), colours[0], bone, fuse=False,
          mirror=True)
    p0 = np.array([a0, 0.02]) + d * L * bands[0]
    p1 = np.array([a0, 0.02]) + d * L * bands[1]
    ww = w * 0.53
    mid = [tuple(p0 + s * ww), tuple(p1 + s * ww), tuple(p1 - s * ww), tuple(p0 - s * ww)]
    k.add(name + "m", slab(mid, org, ex, ez, thick + 0.008), colours[1], bone, fuse=False, mirror=True)
    k.add(name + "t", slab(a.feather_outline(p1[0], p1[1], ang, L * (1 - bands[1]), w * 1.06), org, ex, ez,
                           thick + 0.008), colours[2], bone, fuse=False, mirror=True)


# --- the wings ------------------------------------------------------------------------------------------------------------
ELEV, SWEEP = 44, 28
WROOT = np.array([0.085, 0.04, 0.6])
SPAN = 0.3
ARM = [(-0.02, -0.03), (SPAN * 0.5, -0.04), (SPAN, -0.03), (SPAN + 0.03, 0.0), (SPAN * 0.8, 0.07), (0.12, 0.1),
       (-0.02, 0.06)]
# along the span (a0), off the chord toward the span (deg), length, width: the long primaries at the tip pointing out
FEATHERS = [(0.03, 8, 0.23, 0.078), (0.08, 17, 0.25, 0.078), (0.13, 27, 0.27, 0.078), (0.18, 39, 0.28, 0.078),
            (0.22, 52, 0.29, 0.076), (0.26, 66, 0.28, 0.072), (0.29, 80, 0.26, 0.068), (0.31, 94, 0.22, 0.062)]


def wings(k):
    ex, ez = a.wing_axes(ELEV, SWEEP, (0.25, 0.6, -0.75))
    n = np.cross(ex, ez)
    k.add("wingarm", slab(ARM, WROOT, ex, ez, 0.036), RED, "wing{s}", fuse=False, mirror=True)
    k.add("shoulder", ell(WROOT + ex * 0.03, (0.055, 0.065, 0.06)), RED, "wing{s}", mirror=True)
    for j, (a0, ang, L, w) in enumerate(FEATHERS):
        feather(k, f"feather{j}", ex, ez, n, WROOT, a0, ang, L, w, 0.024, "wing{s}",
                (RED if j % 2 else RED2, WHITE, GREEN), lift=0.005 if j % 2 else -0.005)


def hooh(src, out):
    k = Kit(src, coat="fur")

    # The body: a deep round trunk, red above and white from the waist down
    k.add("body", ell((0, 0.02, 0.46), (0.125, 0.14, 0.155), rot=(-10, 0, 0)), RED, "body")
    k.add("belly", ell((0, -0.03, 0.42), (0.11, 0.1, 0.12)), WHITE, "body")
    k.add("neck", tube([(0, -0.02, 0.57), (0, -0.06, 0.66), (0, -0.1, 0.75), H + (0, 0.03, -0.035)],
                       [0.078, 0.06, 0.052, 0.05]), RED, "head")
    k.add("head", ell(H, HR), RED, "head")
    # white from the waist down, front and sides, its edge crisp and curving up a little at the front
    def white(P):
        line = 0.5 - 0.035 * (P[:, 1] + 0.1) / 0.25
        return inside(P[:, 2] - line, 0.004) * (P[:, 2] > 0.2)
    k.paint(white, WHITE, on=["body", "belly", "thigh"])
    k.paint(lambda P: inside(line_up(P), 0.004), RED, on=["body", "belly"])

    # The green ring round the neck
    k.add("collar", ring_band((0, -0.065, 0.67), unit(0, -0.4, 1), lambda v: 0.062 - 0.006 * v, 0.024, thick=0.01),
          GREEN, "head", fuse=False)

    # The crest: rounded gold lobes off the forehead, curling up and back
    CB = H + (0, -0.02, 0.05)
    for j, (dx, d, L, r) in enumerate([(0.0, (0, 0.35, 1), 0.12, 0.024), (0.026, (0.35, 0.45, 0.9), 0.1, 0.022),
                                       (-0.026, (-0.35, 0.45, 0.9), 0.1, 0.022), (0.016, (0.2, 0.8, 0.6), 0.09, 0.021),
                                       (-0.016, (-0.2, 0.8, 0.6), 0.09, 0.021)]):
        d = unit(*d)
        b = CB + (dx, 0.0, 0.0)
        mid = b + d * L * 0.55 + (0, 0.01, 0.0)
        tip = b + d * L + (0, 0.03, -0.01)
        k.add(f"crest{j}", outward(tube([b, mid, tip], [r * 0.8, r, r * 1.15], seg=18)), GOLD if j < 3 else GOLD2,
              "head", fuse=False, surface="shell")
        k.add(f"knob{j}", ell(tip, (r * 1.3, r * 1.3, r * 1.3), sub=3), GOLD if j < 3 else GOLD2, "head", fuse=False,
              surface="shell")

    # The beak: long and yellow, held open, red inside
    B = H + (0, -0.06, -0.008)
    up = unit(0, -0.96, 0.1)
    k.add("beak", outward(tube([B, B + up * 0.075, B + up * 0.14 + (0, 0, -0.016)], [0.034, 0.022, 0.004], seg=20)),
          BEAK, "head", fuse=False, surface="shell")
    lo = unit(0, -0.9, -0.42)
    J = B + (0, 0.004, -0.024)
    k.add("jaw", outward(tube([J, J + lo * 0.06, J + lo * 0.105], [0.025, 0.016, 0.004], seg=20)), BEAK, "head",
          fuse=False, surface="shell")
    k.add("tongue", ell(B + (0, -0.04, -0.028), (0.02, 0.045, 0.012), rot=(-12, 0, 0)), MOUTH, "head", fuse=False)

    wings(k)

    # The tail: a fan of long gold feathers spread down and back behind, faces to the back
    TB = np.array([0, 0.13, 0.37])
    dn = unit(0, 0.58, -0.81)
    ax = np.array([1.0, 0, 0])
    for j, (ang, L, w) in enumerate([(-42, 0.27, 0.07), (-26, 0.31, 0.075), (-9, 0.34, 0.078), (9, 0.34, 0.078),
                                     (26, 0.31, 0.075), (42, 0.27, 0.07)]):
        t = math.radians(ang)
        d = dn * math.cos(t) + ax * math.sin(t)
        side = np.cross(d, np.cross(ax, dn))
        side = ax * math.cos(t) - dn * math.sin(t)
        base = TB + d * 0.02 + np.cross(ax, dn) * (0.004 if j % 2 else -0.004)
        out_ = [(0, -w * 0.3), (L * 0.6, -w * 0.5), (L, -w * 0.28), (L * 1.04, 0), (L, w * 0.28), (L * 0.6, w * 0.5),
                (0, w * 0.3)]
        k.add(f"tailfeather{j}", slab(out_, base, d, side, 0.03), GOLD if j % 2 else GOLD2, "tail", fuse=False)
        tipo = [(L * 0.7, -w * 0.47), (L, -w * 0.3), (L * 1.04, 0), (L, w * 0.3), (L * 0.7, w * 0.47)]
        k.add(f"tailtip{j}", slab(tipo, base, d, side, 0.038), PALEGOLD, "tail", fuse=False)
    k.add("rump", ell(TB + (0, -0.02, 0.01), (0.06, 0.06, 0.055)), WHITE, "tail")

    # The legs: white thighs in the belly, short dark shins tucked forward, the toes curled
    k.add("thigh", ell((0.055, -0.02, 0.35), (0.055, 0.06, 0.06)), WHITE, "leg{s}", mirror=True)
    for s, nm in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        top = np.array([0.058, -0.03, 0.32]) * m
        ank = np.array([0.062, -0.075, 0.26]) * m
        k.add(f"shin{nm}", outward(tube([top, ank], [0.026, 0.023])), LEG, f"leg{nm}", fuse=False, surface="scales")
        k.add(f"foot{nm}", ell(ank + (0, -0.012, -0.012), (0.032, 0.036, 0.028), sub=3), LEG, f"leg{nm}", fuse=False,
              surface="scales")
        for j, dx in enumerate((-0.018, 0.0, 0.018)):
            b0 = ank + (dx * s, -0.03, -0.012)
            k.add(f"toe{nm}{j}", outward(tube([b0, b0 + (0, -0.022, -0.012), b0 + (0, -0.012, -0.036)],
                                              [0.016, 0.014, 0.01])), LEG, f"leg{nm}", fuse=False, surface="scales")
            k.add(f"talon{nm}{j}", outward(tube([b0 + (0, -0.012, -0.036), b0 + (0, 0.008, -0.044)], [0.01, 0.003])),
                  CLAW, f"leg{nm}", fuse=False, surface="shell")

    # The eyes: a red iris in a heavy black ring, a dark pupil and a glint
    K, R, P, G, RD = srgb(RIM), srgb(IRIS), srgb(PUPIL), srgb(GLINT), srgb(RED)
    def eye(u, v):
        c = np.tile(K, (len(u), 1))
        c[(u ** 2 + v ** 2) < 0.62] = R
        c[((u + 0.1) / 0.32) ** 2 + (v / 0.4) ** 2 < 1] = P
        c[((u - 0.22) / 0.2) ** 2 + ((v - 0.3) / 0.2) ** 2 < 1] = G
        return c
    de = unit(0.66, -0.66, 0.36)
    E = H + de * HR
    def eyes(state):
        k.decal(E, de, (0.026, 0.024), eye_look(state, eye, RD, K, line=0.17), eps=0.005, rings=20, segs=56,
                mirror=True)

    k.build(voxel=0.007, smooth=4, band=0.008)
    k.eye_states(eyes)
    k.export(out)


def line_up(P):
    """Above the waist's line on the trunk (red there whatever the white belly part brought)."""
    return 0.5 - 0.035 * (P[:, 1] + 0.1) / 0.25 + 0.004 - P[:, 2]


if __name__ == "__main__":
    hooh(*args())
