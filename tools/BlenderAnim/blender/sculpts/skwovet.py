# Skwovet sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/skwovet.py -- <the export's Skwovet.glb> <out.glb>
# A small grey-brown squirrel crouched forward: a big round head whose lower half is two great pale cheek pouches
# round a pale muzzle, an orange nose, a grin with one tooth, round dark eyes; dark two-pronged ear tufts with orange
# at their roots; short forelegs down to the ground before the chest, short haunched hind legs; a pale-brown tail rising
# from the rump up the front of a great dark round tail mass, which ends in a hooked curl on top.
#
# Written to be grown from (greedent.py keeps its colours' way, its eyes, nose, ear tufts, tail ball and curls).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, eye_look, polygon, args

# --- colours ----------------------------------------------------------------------------------------------------------
COAT = "#968A82"            # the grey-brown coat
DARK = "#554D48"            # the tail ball and the ear tufts
CREAM = "#ECE6DE"           # the cheek pouches and the muzzle
ORANGE = "#F08A3A"          # the nose and the ears' roots
EYE_W, INK = "#FAFAFA", "#18161A"
MOUTH, TOOTH = "#5A3A34", "#FFFFFF"

# --- the frame (skeleton: body (0, 0, 0.12), head (0, -0.01, 0.18), arms (+-0.05, -0.02, 0.15),
#     legs (+-0.04, 0, 0.07), tail (0, 0.05, 0.1)) ---------------------------------------------------------------------
H = np.array([0, -0.05, 0.222])
HR = np.array([0.079, 0.068, 0.07])


def on_head(x, z, h=None, hr=None, lift=0.0):
    h = H if h is None else h
    hr = HR if hr is None else hr
    zn = (z - h[2]) / hr[2]
    q = max(0.0, 1 - zn * zn - (x / hr[0]) ** 2)
    p = np.array([x, h[1] - hr[1] * math.sqrt(q), z])
    n = (p - h) / hr ** 2; n /= np.linalg.norm(n)
    return p + n * lift, n


def add_head(k):
    k.add("head", ell(H, HR), COAT, "head")
    # the cheek pouches: two great pale balls low on each side, and the muzzle between them
    k.add("cheek", ell(H + (0.05, -0.044, -0.042), (0.05, 0.044, 0.042)), CREAM, "head", mirror=True)
    k.add("muzzle", ell(H + (0, -0.066, -0.03), (0.04, 0.03, 0.034)), CREAM, "head")
    k.add("chin", ell(H + (0, -0.05, -0.062), (0.044, 0.034, 0.024)), CREAM, "head")


def ear_tuft(k, base, s, h, colour=DARK, name="ear"):
    """A dark tuft of two prongs, the main one up and a little out, the second shorter one out to the side."""
    b = np.array(base, float)
    main = [b, b + np.array([0.008, 0.002, 0.04]) * s, b + np.array([0.02, 0.006, 0.08]) * s]
    k.add(name + "a", blade(main, [0.026 * s, 0.018 * s, 0.006 * s], (0, 1, 0), 0.55), colour, "head", mirror=True)
    side = b + np.array([0.006, 0.0, 0.026]) * s
    sp = [side, side + np.array([0.022, 0.004, 0.018]) * s, side + np.array([0.042, 0.006, 0.026]) * s]
    k.add(name + "b", blade(sp, [0.016 * s, 0.012 * s, 0.005 * s], (0, 1, 0), 0.55), colour, "head", mirror=True)
    # orange at the tuft's root, in front: a small loose wedge
    k.add(name + "root", ell(b + np.array([0.004, -0.01, 0.006]) * s, np.array([0.012, 0.006, 0.009]) * s,
                             rot=(20, 0, 0)), ORANGE, "head", fuse=False, mirror=True)


def add_body(k):
    k.add("body", ell((0, 0.014, 0.102), (0.068, 0.076, 0.076), rot=(-18, 0, 0)), COAT, "body")
    k.add("chest", ell((0, -0.03, 0.112), (0.056, 0.05, 0.062)), COAT, "body")
    # forelegs down to the ground before the chest, a little bent
    k.add("arm", tube([(0.046, -0.03, 0.15), (0.054, -0.066, 0.09), (0.05, -0.082, 0.03)], [0.022, 0.019, 0.017]),
          COAT, "arm{s}", mirror=True)
    k.add("paw", ell((0.05, -0.09, 0.013), (0.02, 0.026, 0.013)), COAT, "arm{s}", mirror=True)
    # hind legs: haunches fused into the flanks, little feet forward
    k.add("haunch", ell((0.048, 0.03, 0.062), (0.04, 0.05, 0.056)), COAT, "leg{s}", mirror=True)
    k.add("foot", ell((0.05, -0.006, 0.013), (0.022, 0.036, 0.013)), COAT, "leg{s}", mirror=True)


def add_tail(k):
    # the great dark ball, up behind the body and over it
    k.add("tailball", ell((0, 0.16, 0.27), (0.086, 0.08, 0.1)), DARK, "tail")
    # the hooked curl on its top, leaning forward
    k.add("tailcurl", tube([(0, 0.16, 0.34), (0, 0.15, 0.38), (0, 0.12, 0.4), (0, 0.1, 0.39)], [0.03, 0.022, 0.016, 0.012]),
          DARK, "tail")
    # a trailing dark lock out of its lower back
    k.add("tailwisp", blade([(0.0, 0.2, 0.22), (0.0, 0.24, 0.18), (0.004, 0.25, 0.13)], [0.03, 0.02, 0.006],
                            (1, 0, 0), 0.5), DARK, "tail")
    # the pale tail itself, out of the rump and up the ball's front, ending in a tuft at the head's height
    k.add("tailbase", tube([(0, 0.05, 0.09), (0, 0.1, 0.1), (0, 0.13, 0.15), (0, 0.11, 0.22), (0, 0.085, 0.27)],
                           [0.034, 0.04, 0.042, 0.036, 0.026]), COAT, "tail")
    for i, (d, r) in enumerate((((0.0, -0.02, 0.04), 0.022), ((0.026, -0.022, 0.024), 0.018),
                                ((-0.024, -0.02, 0.026), 0.018))):
        s = np.array((0, 0.085, 0.27))
        k.add(f"tailtuft{i}", blade([s, s + np.array(d) * 0.5, s + np.array(d)], [r, r * 0.6, 0.006], (1, 0, 0), 0.6),
              COAT, "tail")


def add_face(k):
    # the orange nose, loose, on the muzzle's top
    k.add("nose", ell(H + (0, -0.094, -0.012), (0.016, 0.01, 0.01)), ORANGE, "head", fuse=False, surface="skin")
    CC, CM, CT = srgb(CREAM), srgb(MOUTH), srgb(TOOTH)
    def grin(u, v):
        c = np.tile(CC, (len(u), 1))
        c[(np.abs(v - (-0.2 + 0.7 * u * u)) < 0.2) & (np.abs(u) < 0.9)] = CM
        c[(np.abs(u) < 0.22) & (v < -0.05) & (v > -0.75)] = CT
        return c
    k.decal(H + (0, -0.094, -0.036), unit(0, -1, -0.2), (0.02, 0.014), grin, eps=0.0025, rings=12, segs=48)


def eye_colour():
    CW, CI = srgb(EYE_W), srgb(INK)
    def eye(u, v):
        c = np.tile(CW, (len(u), 1))
        c[np.hypot(u / 0.72, (v + 0.04) / 0.76) < 1] = CI
        c[np.hypot((u - 0.22) / 0.24, (v - 0.26) / 0.24) < 1] = (1, 1, 1)
        return c
    return eye


def lay_eyes(k, state, x=0.036, dz=0.014, half=(0.019, 0.021), lid=COAT, h=None, hr=None, look=None):
    p, n = on_head(x, (H if h is None else h)[2] + dz, h, hr)
    n = unit(n[0] * 1.15, n[1], n[2])
    f = eye_look(state, look or eye_colour(), srgb(lid), srgb(INK), line=0.16)
    k.decal(p, n, half, f, eps=0.004, rings=20, segs=56, mirror=True)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="fur")
    add_body(k)
    add_head(k)
    ear_tuft(k, H + (0.044, 0.004, 0.05), 1.0, H)
    add_tail(k)
    add_face(k)
    k.build(voxel=0.004, band=0.004)
    k.eye_states(lambda s: lay_eyes(k, s))
    k.export(out)
