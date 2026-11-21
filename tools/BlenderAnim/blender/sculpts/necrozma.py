# Necrozma sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/necrozma.py -- <the export's Necrozma.glb> <out.glb>
#
# From the references: a being of black crystal, all flat facets and points. Its largest masses are its two hands:
# great hexagonal prisms, each nearly as long as the trunk, hung low and wide at the ends of thin angular arms that
# rise from the shoulders to raised elbows; each pointed at the wrist like a pencil, its lower end and a broad panel of
# its front white, three long hooked claws from its lower end. A crown of a head bristling with spikes swept up and back
# (a crest in the middle, others fanning out at the sides and behind, two blades down the cheeks), a pointed jaw below;
# in the face between them ONE small eye, a downward triangle of rainbow prism (the C# sculpt has two cyan eyes: the
# pictures decide). A broad angular chest with a spike rising from each shoulder and one behind; short prism legs, knees
# bent and feet apart, a pointed knee and clawed feet, so the trunk hangs low between the hands.
# This design is the exception to arms inside the body's width: the prisms held out wide are its silhouette (the
# game's shot of the first round, the hands at the hips, read as a thin man). A mirror pair: the art's two hands differ
# only by the three-quarter view.
# Masses ranked from the pictures: the two hand-prisms, the crown, the chest, the legs. Ratios (3D picture): a
# hand-prism's length / height 0.4 (built 0.4), across / height 0.19 (built 0.19); width / height 1.3 in the art's pose
# (built about 0.85: the prisms hang at x +-0.3); crown and jaw / height 0.42 with the spikes (built about 0.38).
# The export stands 0.82 tall: voxel 0.0065, the thickness floor 0.025.
# Bones: body (0, 0, 0.5); head (0, -0.02, 0.61); arms (+-0.06, 0, 0.58); legs (+-0.05, 0, 0.42).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, unit, eye_look, polygon, args

BLACK, FACET, WHITE = "#26242E", "#34323E", "#D8DAE6"
BLUE, RED, YELLOW, GREEN, VIOLET = "#3E7CF0", "#F0485A", "#F6D23A", "#4CD88A", "#B068E8"
LINE = "#A6A8B8"

DZ = -0.09                   # the trunk, head and arms hang this much lower than the skeleton's joints: short legs
HC0 = np.array([0.0, -0.022, 0.66])                   # where the head's points were first measured about
HC, HR = HC0 + (0, 0, DZ), np.array([0.056, 0.07, 0.06])    # the head's fused core
G = 1.52                     # the crown, jaw and spikes drawn about the core this much larger than first measured
CHEST = 1.4                  # the chest this much broader than first measured
SHOULDER = np.array([0.07, 0.0, 0.6 + DZ])
ELBOW = np.array([0.2, -0.05, 0.62])                  # raised over the shoulder
WRIST = np.array([0.29, -0.1, 0.55])                  # the prism's pointed top
TOP_C, LOW_C = (0.29, -0.1), (0.315, -0.118)         # the prism leans out to its foot
PRISM_TOP, PRISM_LOW, PRISM_MID = 0.5, 0.17, 0.27     # its hexagon's top, its slanted foot, where the white begins
PRISM_R = 0.08                                        # across, 0.16; front to back 0.13
HIP = np.array([0.06, 0.0, 0.43 + DZ])
KNEE = np.array([0.115, -0.06, 0.19])
ANKLE = np.array([0.105, 0.0, 0.036])


def Q(*p):
    """A point of the head (in the first measure), drawn about the head's core G times larger."""
    return tuple(HC + G * (np.array(p, float) - HC0))


def box_along(a, b, w0, t0, w1, t1, up=(0, 0, 1)):
    """A flat-faced limb from a to b: a rectangle w0 by t0 at a growing to w1 by t1 at b (a hull)."""
    a, b = np.array(a, float), np.array(b, float)
    d = b - a; d /= np.linalg.norm(d)
    s = np.cross(d, up)
    if np.linalg.norm(s) < 1e-3:
        s = np.cross(d, (0, 1.0, 0))
    s /= np.linalg.norm(s)
    f = np.cross(s, d)
    pts = []
    for c, w, t in ((a, w0, t0), (b, w1, t1)):
        for sx in (1, -1):
            for sy in (1, -1):
                pts.append(c + s * sx * w / 2 + f * sy * t / 2)
    return hull(pts)


def point_along(a, d, length, w, t, up=(0, 0, 1)):
    """A spike or claw: a kite-section root at a narrowing to a point length along d (a hull)."""
    a = np.array(a, float); d = unit(*d)
    s = np.cross(d, up)
    if np.linalg.norm(s) < 1e-3:
        s = np.cross(d, (1.0, 0, 0))
    s /= np.linalg.norm(s)
    f = np.cross(s, d)
    return hull([a + s * w / 2, a - s * w / 2, a + f * t / 2, a - f * t / 2, a - d * 0.006, a + d * length])


def hexring(c, rx, ry, z):
    return [(c[0] + rx * math.cos(math.radians(60 * i)), c[1] + ry * math.sin(math.radians(60 * i)), z)
            for i in range(6)]


def add_core(k):
    """What is fused: the chest's and the head's cores and the face between crown and jaw, which carries the eye."""
    k.add("torso", ell((0, 0.0, 0.52 + DZ), (0.06, 0.04, 0.075)), BLACK, "body")
    k.add("pelvis", ell((0, 0.0, 0.445 + DZ), (0.03, 0.026, 0.03)), BLACK, "body")
    k.add("neck", tube([(0, -0.005, 0.59 + DZ), (0, -0.015, 0.63 + DZ)], [0.024, 0.024]), BLACK, "head")
    k.add("head", ell(HC, HR), BLACK, "head")


def add_chest(k):
    k.add("chest", hull([(x * s * CHEST, y, z + DZ) for s in (1, -1) for x, y, z in
                         ((0.072, -0.034, 0.602), (0.062, 0.036, 0.6), (0.046, -0.046, 0.49), (0.042, 0.032, 0.48),
                          (0.022, -0.024, 0.43), (0.02, 0.02, 0.43))]
                        + [(0, -0.066, 0.55 + DZ), (0, 0.05, 0.555 + DZ)]), BLACK, "body", fuse=False)
    k.add("hips", hull([(x * s, y, z + DZ) for s in (1, -1) for x, y, z in
                        ((0.07, -0.04, 0.47), (0.07, 0.036, 0.47), (0.04, -0.028, 0.39), (0.04, 0.028, 0.39))]),
          FACET, "body", fuse=False)
    k.add("shoulderspike", point_along(SHOULDER + (0.025, 0.01, 0.0), (0.35, 0.45, 0.82), 0.13, 0.048, 0.034), BLACK,
          "arm{s}", fuse=False, mirror=True)
    k.add("backspike", point_along((0, 0.045, 0.57 + DZ), (0, 0.85, 0.5), 0.11, 0.044, 0.032), FACET, "body",
          fuse=False)


def add_head(k):
    # the crown above the eye, the pointed jaw below it; the fused face between them
    k.add("crown", hull([Q(x * s, y, z) for s in (1, -1) for x, y, z in
                         ((0.05, -0.02, 0.682), (0.034, -0.062, 0.686), (0.046, 0.04, 0.672), (0.03, -0.034, 0.742))]
                        + [Q(0, -0.072, 0.694), Q(0, 0.052, 0.684), Q(0, 0.0, 0.772)]), BLACK, "head", fuse=False)
    k.add("jaw", hull([Q(x * s, y, z) for s in (1, -1) for x, y, z in ((0.04, -0.046, 0.636), (0.04, 0.01, 0.632))]
                      + [Q(0, -0.066, 0.636), Q(0, -0.046, 0.594), Q(0, 0.0, 0.604)]), BLACK, "head", fuse=False)
    # the spikes swept up and back (laid back, so the body stays large in the game)
    k.add("crest", point_along(Q(0, 0.0, 0.752), (0, 0.66, 0.75), 0.11 * G, 0.04 * G, 0.03 * G), FACET, "head",
          fuse=False)
    k.add("crownspike", point_along(Q(0.032, 0.0, 0.722), (0.55, 0.45, 0.7), 0.09 * G, 0.036 * G, 0.028 * G), BLACK,
          "head", fuse=False, mirror=True)
    k.add("sidespike", point_along(Q(0.05, 0.005, 0.69), (0.82, 0.35, 0.45), 0.075 * G, 0.032 * G, 0.026 * G),
          FACET, "head", fuse=False, mirror=True)
    k.add("rearspike", point_along(Q(0.0, 0.045, 0.7), (0, 0.9, 0.3), 0.08 * G, 0.034 * G, 0.027 * G), BLACK, "head",
          fuse=False)
    # two blades down the cheeks
    k.add("cheek", point_along(Q(0.044, -0.036, 0.664), (0.35, -0.38, -0.86), 0.07 * G, 0.03 * G, 0.025 * G), FACET,
          "head", fuse=False, mirror=True)


def add_arms(k):
    # a thin angular arm rising out to a raised elbow and down to the prism's pointed top
    k.add("arm", box_along(SHOULDER + (0.02, 0, 0), ELBOW, 0.032, 0.03, 0.028, 0.028), BLACK, "arm{s}", fuse=False,
          mirror=True)
    k.add("fore", box_along(ELBOW, WRIST, 0.028, 0.028, 0.03, 0.03), BLACK, "arm{s}", fuse=False, mirror=True)
    # the hand: a great hexagonal prism leaning out to its foot, pointed at the top like a pencil, its lower end white
    def at(z):
        t = (PRISM_TOP - z) / (PRISM_TOP - PRISM_LOW)
        return (TOP_C[0] + (LOW_C[0] - TOP_C[0]) * t, TOP_C[1] + (LOW_C[1] - TOP_C[1]) * t)
    ry = PRISM_R * 0.8
    k.add("prism", hull(hexring(at(PRISM_TOP), PRISM_R, ry, PRISM_TOP) + hexring(at(PRISM_MID), PRISM_R, ry, PRISM_MID)
                        + [WRIST + (0, 0, 0.02)]), BLACK, "arm{s}", fuse=False, mirror=True)
    # its foot white, cut on a slant (lower at the front)
    foot = hexring(at(PRISM_MID + 0.003), PRISM_R * 1.03, ry * 1.03, PRISM_MID + 0.003)
    low = [(x, y, PRISM_LOW + 0.05 * (y - at(PRISM_LOW)[1]) / PRISM_R) for x, y, _ in
           hexring(at(PRISM_LOW), PRISM_R * 1.03, ry * 1.03, PRISM_LOW)]
    k.add("prismend", hull(foot + low), WHITE, "arm{s}", fuse=False, mirror=True)
    # a broad white panel on the prism's front face, rising from the white foot
    c = at(PRISM_MID + 0.08)
    fy = c[1] - ry * math.sin(math.radians(60)) - 0.003
    k.add("panel", hull([(c[0] + x, y, z) for x in (-0.034, 0.034) for y in (fy, fy - 0.007)
                         for z in (PRISM_MID, PRISM_MID + 0.15)]), WHITE, "arm{s}", fuse=False, mirror=True)
    # three long hooked claws from the lower end
    c = at(PRISM_LOW)
    for i, t in enumerate((-1, 0, 1)):
        base = (c[0] + 0.045 * t, c[1] - 0.02, PRISM_LOW + 0.012)
        k.add(f"claw{i}", point_along(base, (0.2 * t, -0.42, -0.9), 0.16, 0.042, 0.034), BLACK, "arm{s}",
              fuse=False, mirror=True)


def add_legs(k):
    # short thick prism legs, knees bent, feet apart
    k.add("thigh", box_along(HIP + (0.01, 0, 0.01), KNEE, 0.1, 0.09, 0.08, 0.075, up=(0, -1, 0.3)), BLACK,
          "leg{s}", fuse=False, mirror=True)
    k.add("knee", point_along(KNEE + (0, -0.02, 0.006), (0.15, -1, 0.5), 0.07, 0.06, 0.044), FACET, "leg{s}",
          fuse=False, mirror=True)
    k.add("shin", box_along(KNEE, ANKLE + (0, 0, 0.012), 0.08, 0.075, 0.06, 0.058, up=(0, -1, 0.1)), FACET, "leg{s}",
          fuse=False, mirror=True)
    for i, t in enumerate((-1, 0, 1)):
        k.add(f"toe{i}", point_along(ANKLE + (0.02 * t, -0.016, -0.02), (0.25 * t, -1, -0.25), 0.08, 0.034, 0.03),
              BLACK, "leg{s}", fuse=False, mirror=True)
    k.add("heel", point_along(ANKLE + (0, 0.02, -0.022), (0, 1, -0.2), 0.05, 0.032, 0.028), BLACK, "leg{s}",
          fuse=False, mirror=True)


# --- the eye: one downward triangle of rainbow prism in the middle of the face -----------------------------------------
TRI = [(-1.0, 0.75), (1.0, 0.75), (0.0, -1.0)]


def open_eye(u, v):
    a = np.degrees(np.arctan2(v, u))
    c = np.tile(srgb(YELLOW), (len(u), 1))
    c[(a > 90) & (a <= 200)] = srgb(BLUE)
    c[(a > -20) & (a <= 90)] = srgb(RED)
    c[(a > -90) & (a <= -20)] = srgb(VIOLET)
    c[(a > -160) & (a <= -90)] = srgb(GREEN)
    c[u * u + v * v < 0.2 ** 2] = (1, 1, 1)
    return c


def eyes(k, state):
    p = HC + np.array([0, -HR[1] * 0.98, -0.004])
    look = eye_look(state, open_eye, srgb(BLACK), srgb(LINE), line=0.18)
    k.decal(p, (0, -1, 0.05), (0.03, 0.026), look, outline=polygon(TRI), eps=0.002, rings=20, segs=56)


def necrozma(src, out):
    k = Kit(src, coat="shell")
    add_core(k)
    add_chest(k)
    add_head(k)
    add_arms(k)
    add_legs(k)
    k.build(voxel=0.0065, smooth=4, band=0.006, wband=0.05)
    k.eye_states(lambda s: eyes(k, s))
    k.export(out)


if __name__ == "__main__":
    necrozma(*args())
