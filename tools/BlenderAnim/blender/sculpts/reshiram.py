# Reshiram sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/reshiram.py -- <the export's Reshiram.glb> <out.glb>
#
# From the references: a tall white feathered dragon standing upright on two legs; a long neck ringed by two pale
# blue-grey bands, carrying a long head forward, the snout tapering to a point, blue eyes lined in dark grey; a mane of
# white fur from the back of the head swept back down the neck and trailing behind as a long wavy streamer; two short
# points up off the back of the head; tufts at the chin and the cheeks; great white feathered wings raised up and back,
# their long primaries pointing out and back, two small grey-blue claws at each wing's wrist; thin arms held forward with
# three grey-blue claws; feather spikes at the chest's sides and up off the hips; huge round thighs, shins short and
# lean with a feather spur behind, long feet with three grey-blue claws; and the turbine tail: a white drum low behind
# the hips, ringed by two blue-grey bands, its back open, long white feathers radiating up and back from it.
# The export's joints: body (0, 0, 0.42), head (0, -0.04, 0.62), arms (+-0.07, -0.03, 0.55), wings (+-0.05, 0.02,
# 0.56), tail (0, 0.06, 0.34), legs (+-0.06, 0.02, 0.3).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, hull, slab, ring_band, lathe, srgb, inside, unit, eye_look, polygon, args

WHITE, SHADE, BAND, CLAW = "#F4F5FA", "#D9DEEE", "#AEB9DC", "#9EA9D2"
CORE, EYEB, INKC, LINER = "#7E8CC0", "#33B2EA", "#141A2C", "#4A5064"

H = np.array([0.0, -0.17, 0.84])           # the skull's middle, as laid out
HR = np.array([0.064, 0.074, 0.064])
J, HS = np.array([0.0, -0.14, 0.78]), 1.33  # the head grown about its root, a third larger
HG, HRG = J + (H - J) * HS, HR * HS
NECK = np.array([(0, -0.05, 0.55), (0, -0.09, 0.645), (0, -0.125, 0.73), (0, -0.15, 0.8)])
NECK_R = [0.1, 0.07, 0.06, 0.056]


def on_ell(c, r, d):
    d = unit(*d)
    t = 1.0 / math.sqrt(((d / np.array(r)) ** 2).sum())
    return np.array(c, float) + d * t


def spike(k, name, base, d, length, width, thick, bone, color=WHITE, up=None, sink=0.02, mirror=False, surface="fur"):
    """A solid pointed piece (a feather spike, a claw): a kite in section, wide across up and thick along it."""
    base, d = np.array(base, float), unit(*d)
    if up is None:
        up = np.cross(d, (0, 0, 1.0)) if abs(d[2]) < 0.9 else np.array([1.0, 0, 0])
    up = np.array(up, float); up = up - d * (up @ d); up /= np.linalg.norm(up)
    side = np.cross(d, up)
    b = base - d * sink
    pts = [b + up * width / 2, b - up * width / 2, b + side * thick / 2, b - side * thick / 2,
           base + d * length * 0.45 + up * width * 0.32, base + d * length * 0.45 - up * width * 0.32,
           base + d * length * 0.45 + side * thick * 0.32, base + d * length * 0.45 - side * thick * 0.32,
           base + d * length]
    k.add(name, hull(pts), color, bone, fuse=False, mirror=mirror, surface=surface)


def snout_taper(V):
    V = V.copy()
    t = np.clip(-V[:, 1], 0, 1)
    V[:, 0] *= 1 - 0.5 * t
    V[:, 2] *= 1 - 0.35 * t
    return V


def body(k):
    # the trunk leaning forward some 20 degrees, the chest out, the waist slim over the thighs
    k.add("torso", ell((0, -0.03, 0.44), (0.088, 0.095, 0.15), rot=(20, 0, 0)), WHITE, "body")
    k.add("chest", ell((0, -0.085, 0.53), (0.108, 0.1, 0.098), rot=(20, 0, 0)), WHITE, "body")
    k.add("hips", ell((0, 0.05, 0.33), (0.1, 0.1, 0.085)), WHITE, "body")
    k.add("neck", tube(NECK, NECK_R), WHITE, "head")
    # two pale blue-grey bands round the neck
    for j, z in enumerate((0.66, 0.725)):
        cy = np.interp(z, NECK[:, 2], NECK[:, 1]); r = np.interp(z, NECK[:, 2], NECK_R)
        k.add(f"band{j}", ring_band((0, cy, z), (0, -0.25, 1), r + 0.004, 0.03, thick=0.014), BAND, "head",
              fuse=False, surface="shell")
    # feather spikes at the chest's sides, pointing down and out
    for j, (b, d, L) in enumerate((((0.09, -0.11, 0.52), (0.7, -0.2, -0.7), 0.08),
                                   ((0.085, -0.06, 0.44), (0.75, 0.1, -0.65), 0.075))):
        spike(k, f"chesttuft{j}", b, d, L, 0.05, 0.03, "body", mirror=True)
    # feather spikes up and back off the hips, over the turbine
    for j, (b, d, L) in enumerate((((0.06, 0.1, 0.42), (0.35, 0.7, 0.62), 0.17),
                                   ((0.09, 0.08, 0.36), (0.75, 0.6, 0.3), 0.14))):
        spike(k, f"hipfeather{j}", b, d, L, 0.06, 0.032, "body", mirror=True)


def legs(k):
    for s, sd in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        bone = f"leg{sd}"
        # a huge round thigh fused into the hips, reaching low
        k.add(f"thigh{sd}", ell(np.array((0.1, 0.045, 0.27)) * m, (0.078, 0.115, 0.125)), WHITE, bone)
        k.add(f"shin{sd}", tube(np.array([(0.12, 0.08, 0.19), (0.128, 0.07, 0.11), (0.13, 0.05, 0.05)]) * m,
                                [0.058, 0.044, 0.04]), WHITE, bone)
        k.add(f"foot{sd}", ell(np.array((0.13, -0.005, 0.026)) * m, (0.048, 0.088, 0.027)), WHITE, bone)
        for j, dx in enumerate((-0.026, 0.0, 0.026)):
            spike(k, f"toe{sd}{j}", np.array((0.13 + dx, -0.08, 0.025)) * m, np.array((dx * 1.2, -1, -0.35)) * m,
                  0.05, 0.034, 0.026, bone, color=CLAW, up=(0, 0, 1), sink=0.015, surface="shell")
        spike(k, f"spur{sd}", np.array((0.13, 0.085, 0.035)) * m, (0, 1, -0.2), 0.045, 0.03, 0.024, bone, color=CLAW,
              up=(0, 0, 1), sink=0.012, surface="shell")
        # the feather spur behind the shin
        spike(k, f"shinfeather{sd}", np.array((0.125, 0.1, 0.13)) * m, (0.25 * s, 1, -0.35), 0.1, 0.05, 0.028, bone,
              up=(0, 0, 1))


def arms(k):
    for s, sd in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        bone = f"arm{sd}"
        k.add(f"arm{sd}", tube(np.array([(0.075, -0.06, 0.55), (0.125, -0.13, 0.5), (0.13, -0.24, 0.47)]) * m,
                               [0.044, 0.036, 0.032]), WHITE, bone)
        # the hand open forward: a flat palm, three claws spread
        k.add(f"hand{sd}", ell(np.array((0.13, -0.265, 0.468)) * m, (0.04, 0.03, 0.03)), WHITE, bone)
        for j, dx in enumerate((-0.028, 0.0, 0.028)):
            spike(k, f"claw{sd}{j}", np.array((0.13 + dx, -0.288, 0.468)) * m, np.array((dx * 2.5, -1, -0.15)) * m,
                  0.05, 0.028, 0.022, bone, color=CLAW, up=(0, 0, 1), sink=0.012, surface="shell")


# --- the wings: an arm raised up, out and back, feathers fanned from it down and back -------------------------------
ROOT = np.array([0.06, 0.03, 0.58])
SPAN = unit(0.38, 0.68, 0.62)            # out, back and up: a V
TRAIL = unit(0.12, 0.62, -0.78)          # the feathers' way, down and back
ARM = 0.42
FEATHERS = [(0.06, 6, 0.28, 0.15), (0.2, 14, 0.33, 0.16), (0.34, 22, 0.38, 0.16), (0.48, 31, 0.42, 0.16),
            (0.62, 41, 0.45, 0.155), (0.75, 52, 0.47, 0.15), (0.87, 63, 0.46, 0.14), (0.97, 75, 0.42, 0.13)]


def wings(k):
    ex = SPAN
    ez = TRAIL - ex * (TRAIL @ ex); ez /= np.linalg.norm(ez)
    n = np.cross(ex, ez)
    wrist = ROOT + ex * ARM
    k.add("wingarm", tube([ROOT - ex * 0.02, ROOT + ex * ARM * 0.5, wrist], [0.05, 0.042, 0.034]), WHITE, "wing{s}",
          mirror=True)
    # the covert plate along the arm, the feathers' root
    covert = [(-0.02, -0.015), (ARM + 0.02, -0.015), (ARM + 0.01, 0.09), (ARM * 0.6, 0.15), (ARM * 0.25, 0.16),
              (0.0, 0.13)]
    k.add("covert", slab([(a, b) for a, b in covert], ROOT + n * 0.012, ex, ez, 0.034), WHITE, "wing{s}", fuse=False, mirror=True)
    for j, (t, ang, L, w) in enumerate(FEATHERS):
        a = math.radians(ang)
        d = math.sin(a) * ex + math.cos(a) * ez         # from straight down the plane toward the span
        wd = math.cos(a) * ex - math.sin(a) * ez
        o = ROOT + ex * ARM * t + n * (0.005 if j % 2 else -0.005)
        out = [(0, -w * 0.5), (L * 0.35, -w * 0.55), (L * 0.75, -w * 0.38), (L, 0.0), (L * 0.75, w * 0.4),
               (L * 0.35, w * 0.55), (0, w * 0.5)]
        k.add(f"feather{j}", slab(out, o, d, wd, 0.032), WHITE if j % 2 else SHADE, "wing{s}", fuse=False,
              mirror=True)
    # two small claws at the wrist, pointing out along the span
    for j, off in enumerate((0.0, 0.03)):
        spike(k, f"wingclaw{j}", wrist + ez * off - ex * 0.01, ex + 0.3 * n - 0.4 * ez, 0.06, 0.03, 0.022,
              "wing{s}", color=CLAW, mirror=True, surface="shell")


def turbine(k):
    k.add("tailroot", tube([(0, 0.06, 0.37), (0, 0.15, 0.34), (0, 0.22, 0.33)], [0.08, 0.065, 0.058]), WHITE, "tail")
    c0, d = np.array([0, 0.2, 0.33]), unit(0, 1, -0.08)
    L, R = 0.23, 0.118
    k.add("drum", lathe(c0, d, [(0, 0.0), (0.085, 0.0), (R, 0.03), (R, L - 0.02), (R - 0.012, L), (0.074, L),
                               (0.062, L - 0.03), (0, L - 0.03)], seg=48), WHITE, "tail", fuse=False, surface="fur")
    k.add("core", lathe(c0, d, [(0, L - 0.04), (0.064, L - 0.04), (0.064, L - 0.028), (0, L - 0.028)], seg=40),
          CORE, "tail", fuse=False, surface="shell")
    for j, v in enumerate((0.07, 0.15)):
        k.add(f"tband{j}", ring_band(c0 + d * v, d, R + 0.005, 0.036, thick=0.012), BAND, "tail", fuse=False,
              surface="shell")
    # long feathers radiating from the drum, up and back, a few out the sides
    a_, b_ = np.cross(d, (1.0, 0, 0)), np.array([1.0, 0, 0])
    if a_[2] < 0:
        a_ = -a_
    for j, (deg, t, L2) in enumerate(((0, 0.55, 0.2), (40, 0.45, 0.18), (-40, 0.45, 0.18), (80, 0.6, 0.15),
                                      (-80, 0.6, 0.15), (120, 0.8, 0.12), (-120, 0.8, 0.12))):
        r = math.radians(deg)
        radial = math.cos(r) * a_ + math.sin(r) * b_
        base = c0 + d * (L * t) + radial * (R - 0.01)
        spike(k, f"turbinefeather{j}", base, radial * 0.8 + d * 0.75, L2, 0.055, 0.03, "tail", up=d, sink=0.02)


def head(k):
    k.add("skull", ell(H, HR), WHITE, "head")
    k.add("snout", ell(H + (0, -0.085, -0.016), (0.046, 0.095, 0.04), deform=snout_taper), WHITE, "head")
    k.add("jaw", ell(H + (0, -0.06, -0.044), (0.038, 0.075, 0.024), rot=(8, 0, 0)), WHITE, "head")
    k.add("occiput", ell(H + (0, 0.05, 0.005), (0.058, 0.075, 0.058)), WHITE, "head")
    # the mane: locks swept back and down the neck, and the long wavy streamer trailing behind, out to +X
    # locks down the back of the neck, standing clear of it so they show from both cameras
    for j, (x, z, L) in enumerate(((0.0, 0.045, 0.2), (0.04, 0.02, 0.17), (-0.04, 0.02, 0.17))):
        b = H + (x, 0.05, z)
        k.add(f"lock{j}", blade([b, b + (x * 0.7, 0.1, -0.05), b + (x * 1.1, 0.15, -L)], [0.04, 0.034, 0.014],
                                (1, 0, 0), 0.6), WHITE, "head")
    # two short points up off the back of the head, tufts at the cheeks and the chin
    spike(k, "crown", H + (0.03, 0.03, 0.05), (0.3, 0.6, 0.75), 0.085, 0.045, 0.026, "head", mirror=True)
    spike(k, "cheektuft", H + (0.05, 0.0, -0.03), (0.6, 0.6, -0.55), 0.065, 0.04, 0.024, "head", mirror=True)
    spike(k, "chintuft", H + (0, -0.03, -0.06), (0, 0.4, -1), 0.06, 0.04, 0.026, "head")


# --- the eyes: blue, a dark pupil, lined in dark grey that runs back into a point -------------------------------------
OUT = [(-1, -0.15), (-0.55, -0.62), (0.2, -0.8), (0.8, -0.5), (1.15, 0.12), (0.7, 0.55), (0.0, 0.62), (-0.65, 0.4)]
EYE_OUT = polygon(OUT)
CB, CP, CK, CW, CL, LID = srgb(EYEB), srgb(INKC), srgb(INKC), srgb("#FFFFFF"), srgb(LINER), srgb(WHITE)


def eye_open(u, v):
    c = np.tile(CB, (len(u), 1))
    c[((u + 0.08) / 0.24) ** 2 + ((v + 0.02) / 0.42) ** 2 < 1] = CP
    c[((u + 0.3) / 0.14) ** 2 + ((v - 0.2) / 0.14) ** 2 < 1] = CW
    frac = np.array([math.hypot(a, b) / EYE_OUT(math.atan2(b, a)) for a, b in zip(u, v)])
    c[frac > 0.8] = CL
    return c


DE = unit(0.6, -0.68, 0.3)


def eyes(k, state):
    E = on_ell(HG, HRG, DE) + np.array([0, -0.008, -0.005])
    look = eye_look(state, eye_open, LID, CL, line=0.18)
    m = np.array([-1, 1, 1.0])
    k.decal(E, DE, (0.04, 0.029), look, eps=0.005, rings=20, segs=56, outline=EYE_OUT)
    k.decal(E * m, DE * m, (0.04, 0.029), lambda u, v: look(-u, v), eps=0.005, rings=20, segs=56,
            outline=lambda th: EYE_OUT(math.pi - th))


def streamer(k):
    """The long hair trailing back from the head, out to +X beside the wing."""
    S = [HG + (0, 0.08, -0.06), (0.05, 0.08, 0.74), (0.13, 0.16, 0.7), (0.21, 0.24, 0.66), (0.28, 0.31, 0.64),
         (0.33, 0.37, 0.66)]
    k.add("streamer", blade(S, [0.042, 0.04, 0.036, 0.032, 0.026, 0.014], (0, 0, 1), 0.55), WHITE, "head")


def reshiram(src, out):
    k = Kit(src, coat="fur")
    body(k)
    legs(k)
    arms(k)
    wings(k)
    turbine(k)
    n0 = len(k.parts)
    head(k)
    for p in k.parts[n0:]:
        p["V"] = J + (p["V"] - J) * HS
    streamer(k)
    # a thin grey mouth line along the snout's side
    k.decal(J + (H + (0.03, -0.115, -0.036) - J) * HS, unit(1, -0.3, -0.25), (0.045, 0.006), "#8A92AE", up=(0, 0, 1), eps=0.004,
            rings=4, segs=40, mirror=True)
    k.build(voxel=0.0075, smooth=4, band=0.006)
    k.eye_states(lambda state: eyes(k, state))
    k.export(out)


if __name__ == "__main__":
    reshiram(*args())
