# Clawitzer sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/clawitzer.py -- <the export's Clawitzer.glb> <out.glb>
#
# Clauncher grown (clauncher.py's frame, by sibling: the round head, the segments with their dark grey bands painted
# between them, the fringe of blue points under the face, the short legs, the eyes laid about the head's own middle).
# From the references: the one claw has become a great dark blue cannon on its right side (+X), twice as long as the
# shrimp behind it and the largest mass by far: tall and rounded at the back, three dark grey bands over its top, a
# round lighter knob on its side from which a long yellow whip runs back over it, a short yellow whip curling up from
# its back; at the front a dark blue upper jaw hooked down to a point over a yellow lower jaw set with teeth, the mouth
# dark between them. The shrimp is light blue, its head with a dark grey band across the brow over two narrow mean
# yellow eyes, a blue point of a crest on top, a small claw of its own held before it (yellow at the tip, on the body
# bone: there is no left arm), its body curled under, and a fan of long dark grey spikes standing out behind it.
# Bones: body (0, 0, 0.135), head (-0.054, -0.067, 0.175), armR (0.047, -0.054, 0.175), tail (0, 0.076, 0.135),
# leg{s} (+-0.054, -0.041, 0.094), hind{s} (+-0.054, 0.054, 0.094).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, args, outward, hull, sibling

c0 = sibling("clauncher")
PALE, CANNON, KNOB, BAND, YELLOW = "#74C4EC", "#2E62C4", "#4A86DC", "#46484F", "#F2C232"
MOUTH = "#141A30"
CANNON_LIFT = 0.04

HEAD_C, HEAD_R = np.array([-0.075, -0.125, 0.128]), np.array([0.058, 0.056, 0.056])
SEGS = [((-0.055, -0.055, 0.11), (0.064, 0.056, 0.064)), ((-0.045, 0.008, 0.094), (0.058, 0.05, 0.056)),
        ((-0.04, 0.06, 0.08), (0.046, 0.04, 0.045)), ((-0.04, 0.098, 0.072), (0.034, 0.032, 0.034))]
BANDS = (-0.08, -0.022, 0.036, 0.08)

# ---- the cannon: loose pieces (it stands beside the face, and a fused one would catch the eyes' patches) --------------
CAN_C, CAN_R = np.array([0.072, -0.075, 0.205]), np.array([0.066, 0.215, 0.088])


def taper(V):
    """Full and tall at the back, narrowing toward the jaws at the front (-y)."""
    V = V.copy()
    t = (V[:, 1] + 1) / 2                     # 0 at the front, 1 at the back
    s = 0.62 + 0.38 * np.clip(t * 1.4, 0, 1) ** 0.7
    V[:, 0] *= s
    V[:, 2] = V[:, 2] * s + 0.12 * (1 - t)    # the front's line rises a little to the jaw
    return V


def can_section(y):
    """The cannon's half width and half height at y, and the middle's height there."""
    t = (y - CAN_C[1]) / CAN_R[1]
    q = math.sqrt(max(1 - t * t, 0.0))
    tt = (t + 1) / 2
    s = 0.62 + 0.38 * min(max(tt * 1.4, 0), 1) ** 0.7
    return CAN_R[0] * q * s, CAN_R[2] * q * s, CAN_C[2] + CAN_R[2] * 0.12 * (1 - tt)


def can_band(y, w=0.022, lift=1.05):
    """A dark grey band over the cannon's top and down its sides: a hull of its arc at y and y + w."""
    pts = []
    for yy in (y, y + w):
        a, b, zc = can_section(yy)
        for th in np.linspace(math.radians(-25), math.radians(205), 16):
            pts.append((CAN_C[0] + a * lift * math.cos(th), yy, zc + b * lift * math.sin(th)))
    return hull(pts)


def cannon(k):
    k.add("cannonarm", outward(tube([(0.0, -0.05, 0.16), (0.03, -0.06, 0.18), (0.055, -0.07, 0.19)],
                                    [0.03, 0.034, 0.036], seg=14, per=4)), CANNON, "armR", fuse=False,
          surface="shell")
    k.add("cannon", outward(ell(CAN_C, CAN_R, deform=taper)), CANNON, "armR", fuse=False, surface="shell")
    for i, y in enumerate((-0.03, 0.025, 0.075)):
        k.add(f"canband{i}", can_band(y), BAND, "armR", fuse=False, surface="shell")
    # the upper jaw hooked down at the front
    k.add("upperjaw", outward(tube([(0.072, -0.245, 0.22), (0.072, -0.31, 0.206), (0.072, -0.35, 0.17),
                                    (0.072, -0.36, 0.13)], [0.046, 0.034, 0.018, 0.006], seg=16, per=6)),
          CANNON, "armR", fuse=False, surface="shell")
    # the dark mouth, and the yellow lower jaw under it with its teeth
    k.add("mouth", outward(ell((0.072, -0.25, 0.155), (0.038, 0.075, 0.03))), MOUTH, "armR", fuse=False,
          surface="shell")
    k.add("lowerjaw", outward(ell((0.072, -0.235, 0.118), (0.046, 0.105, 0.024), rot=(-8, 0, 0))), YELLOW,
          "armR", fuse=False, surface="shell")
    for i, y in enumerate((-0.31, -0.27, -0.23, -0.19)):
        z = 0.128 + (y + 0.31) * -0.1
        for sx in (1, -1):
            x = 0.072 + sx * 0.026
            k.add(f"tooth{i}{'R' if sx > 0 else 'L'}",
                  hull([(x - 0.01, y - 0.01, z), (x + 0.01, y - 0.01, z), (x - 0.01, y + 0.012, z),
                        (x + 0.01, y + 0.012, z), (x, y, z + 0.03)]), YELLOW, "armR", fuse=False, surface="shell")
    # the knob on its outer side and the whips
    knob = np.array([0.132, -0.1, 0.232])
    k.add("knob", outward(ell(knob, (0.024, 0.03, 0.026))), KNOB, "armR", fuse=False, surface="shell")
    k.add("whip", outward(tube([knob + (0, 0.01, 0.012), (0.12, -0.02, 0.29), (0.095, 0.09, 0.31),
                                (0.075, 0.19, 0.3), (0.06, 0.25, 0.27)], [0.009, 0.0085, 0.008, 0.007, 0.006],
                               seg=10, per=6)), YELLOW, "armR", fuse=False, surface="shell")
    k.add("whip2", outward(tube([(0.07, 0.1, 0.27), (0.072, 0.14, 0.31), (0.07, 0.18, 0.34), (0.06, 0.2, 0.37)],
                                [0.009, 0.008, 0.007, 0.006], seg=10, per=6)), YELLOW, "armR", fuse=False,
          surface="shell")


# ---- the shrimp -----------------------------------------------------------------------------------------------------
def crest(k):
    t = HEAD_C + (0, 0.01, HEAD_R[2] * 0.8)
    k.add("crest", hull([t + (0.014, -0.012, 0), t + (-0.014, -0.012, 0), t + (0, 0.02, 0), t + (0, -0.01, -0.012),
                         t + (0.0, 0.03, 0.05)]), PALE, "head", fuse=False, surface="shell")


def brow(k, z0=0.141, w=0.007):
    """The dark grey band across the brow, over the eyes."""
    def m(P):
        d = (((P - HEAD_C) / (HEAD_R * 1.25)) ** 2).sum(1)
        return inside(d - 1, 0.01) * inside(np.abs(P[:, 2] - z0) - w, 0.002) * (P[:, 1] < HEAD_C[1] + 0.01)
    k.paint(m, BAND)


def small_claw(k):
    w = np.array([-0.098, -0.172, 0.074])
    k.add("smallarm", tube([HEAD_C + (-0.01, 0.01, -0.04), w], [0.016, 0.017], seg=12, per=4), PALE, "body")
    k.add("smallband", outward(ell(w + (0, -0.006, 0), (0.021, 0.012, 0.021))), BAND, "body", fuse=False,
          surface="shell")
    k.add("smallpalm", outward(ell(w + (0, -0.03, 0.004), (0.022, 0.03, 0.02))), PALE, "body", fuse=False,
          surface="shell")
    k.add("smalltip", hull([w + (0.012, -0.05, -0.006), w + (-0.012, -0.05, -0.006), w + (0, -0.05, 0.008),
                            w + (0, -0.075, -0.012)]), YELLOW, "body", fuse=False, surface="shell")


def spikes(k):
    root = np.array([-0.042, 0.045, 0.115])
    for i, (dy, dz, dx, L) in enumerate(((0.55, 0.85, -0.2, 0.11), (0.8, 0.55, -0.15, 0.13), (0.95, 0.2, -0.1, 0.14),
                                         (0.9, -0.15, -0.12, 0.12), (0.4, 0.75, 0.4, 0.1))):
        d = unit(dx, dy, dz)
        r = root + d * 0.03
        a = np.cross(d, (1.0, 0, 0)); a /= np.linalg.norm(a)
        b = np.cross(d, a)
        k.add(f"spike{i}", hull([r + a * 0.017, r - a * 0.017, r + b * 0.012, r - b * 0.012, r - d * 0.02,
                                 r + d * L]), BAND, "tail", fuse=False, surface="shell")


TOP = lambda u: 0.18 - 0.32 * u              # u toward the inner side: the brow slanting down toward the nose


def eye(u, v):
    return c0.clauncher_eye(u, v, top=TOP)


LEGS = [("legR", (-0.02, -0.04, 0.045), (0.0, -0.05, 0.004)), ("legL", (-0.09, -0.04, 0.045), (-0.11, -0.05, 0.004)),
        ("hindR", (-0.015, 0.03, 0.042), (0.004, 0.04, 0.004)), ("hindL", (-0.075, 0.03, 0.042), (-0.094, 0.04, 0.004))]


def clawitzer(src, out):
    k = Kit(src, coat="shell")
    c0.head(k, HEAD_C, HEAD_R, PALE)
    c0.segments(k, SEGS, PALE)
    c0.bands(k, BANDS, 0.007, BAND, xmax=0.2)
    brow(k)
    crest(k)
    small_claw(k)
    spikes(k)
    c0.bristles(k, HEAD_C, HEAD_R, n=5, length=0.028, w=0.011, color=PALE)
    c0.legs(k, LEGS, r=(0.016, 0.014, 0.011), color=PALE)
    cannon(k)
    for p in k.parts:              # the cannon carried over the shrimp, so its face shows under it
        if p["bone"] == "armR":
            p["V"] = p["V"] + np.array([0.0, 0.0, CANNON_LIFT])
    k.build(voxel=0.0032, smooth=3, band=0.004, wband=0.03)
    places = c0.eye_places(HEAD_C, HEAD_R, off=0.03, z=0.002)
    k.eye_states(c0.eyes(k, look=eye, places=places, half=(0.02, 0.02), top=TOP, lid=srgb(PALE)))
    k.export(out)


if __name__ == "__main__":
    clawitzer(*args())
