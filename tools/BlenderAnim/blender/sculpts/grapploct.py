# Grapploct sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/grapploct.py -- <the export's Grapploct.glb> <out.glb>
#
# Clobbopus grown (clobbopus.py's mask painted round the head's axis, its eyes with a pupil of their own, its ring of
# an ear, its loose suckers): from the references, an octopus wrestler standing. A great blue mantle, the largest mass,
# an egg leaning back over the head; on the face a yellow mask like a star of eight points round the eyes, a paler rim
# round it; light blue eyes with a dark rim, their tops cut by an angry brow sloping to the middle, a dark pupil low
# toward the nose; a yellow ring with a dark middle on each side. Under the head a broad dark purple chest (blue behind),
# two thick purple arms held forward and down with curled fists, yellow suckers under them; four thick purple
# tentacles for legs splaying out and curling up at their ends, blue beneath, yellow suckers on them.
# (The art holds one arm high across the body; in the rest pose both are forward at the chest. No head bone: the head
# is the body.)
# Skeleton: body (0, 0, 0.42); arms (+-0.12, -0.03, 0.44); legs (+-0.08, -0.05, 0.24), hinds (+-0.08, 0.05, 0.24).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, args, sibling

cb = sibling("clobbopus")
sz = sibling("sizzlipede")
BLUE, PURPLE, YELLOW, PALE = "#2C4CC4", "#433A6C", "#F4E234", "#E2F08C"
EYEB, RIM, PUPIL, INK = "#AEDCF2", "#1C2450", "#141A3A", "#141A30"

HC, HR = np.array([0.0, -0.035, 0.655]), np.array([0.18, 0.155, 0.14])
MANTLE_C, MANTLE_R = np.array([0.0, 0.07, 0.83]), np.array([0.185, 0.18, 0.23])

# The star of a mask as (arc, height) about the eyes' middle: points up, out above the eyes, out level, out below
# and down to the nose
MZ = 0.66
STAR = [(round(1.4 * r * math.cos(math.radians(a)), 4), round(1.3 * r * math.sin(math.radians(a)), 4)) for a, r in
        ((90, 0.145), (67, 0.066), (44, 0.135), (22, 0.064), (0, 0.165), (-24, 0.066), (-50, 0.125), (-71, 0.064),
         (-90, 0.12))]
EYE_S, EYE_Z, EYE_HALF = 0.066, 0.656, (0.041, 0.034)
EYER, EYEL = sz.cut_eye(low=0.06, slope=0.4)


def body(k):
    k.add("head", ell(HC, HR), BLUE, "body")
    k.add("mantle", ell(MANTLE_C, MANTLE_R, rot=(-24, 0, 0)), BLUE, "body")
    k.add("chest", ell((0, 0.0, 0.42), (0.175, 0.14, 0.13)), PURPLE, "body")
    k.add("hips", ell((0, 0.01, 0.27), (0.165, 0.14, 0.1)), PURPLE, "body")
    # blue behind the chest
    k.paint(lambda P: inside(0.04 - P[:, 1], 0.004) * inside(P[:, 2] - 0.56, 0.004), BLUE, on=["chest", "hips"])


ARM = [(0.14, -0.03, 0.455), (0.245, -0.08, 0.455), (0.24, -0.19, 0.465), (0.175, -0.26, 0.455)]
FIST, FIST_R = np.array([0.15, -0.29, 0.445]), np.array([0.078, 0.074, 0.074])


def arms(k):
    k.add("arm", tube(ARM, [0.078, 0.072, 0.066, 0.062], seg=24, per=6), PURPLE, "arm{s}", mirror=True)
    k.add("fist", ell(FIST, FIST_R), PURPLE, "arm{s}", mirror=True)
    k.add("curl", ell(FIST + np.array((-0.01, -0.04, -0.045)), (0.056, 0.046, 0.044)), PURPLE, "arm{s}", mirror=True)
    # the shoulder's back blue
    k.paint(lambda P: inside(-0.01 - P[:, 1], 0.005) * inside(0.4 - P[:, 2], 0.005), BLUE, on=["arm"])
    # bold yellow suckers under the fist and along the forearm's underside
    for d, r in (((0.25, -0.55, -0.8), 0.03), ((-0.45, -0.3, -0.85), 0.027), ((0.75, 0.1, -0.65), 0.026)):
        p, n = cb.on_ell(FIST, FIST_R, d)
        k.add("sucker", cb.oriented(p + n * 0.003, (r, r, r * 0.4), n), YELLOW, "arm{s}", fuse=False, mirror=True)
    for c, d, r in (((0.245, -0.16, 0.41), (0.5, -0.2, -0.85), 0.024),):
        n = unit(*d)
        k.add("sucker", cb.oriented(np.array(c) + n * 0.003, (r, r, r * 0.4), n), YELLOW, "arm{s}", fuse=False,
              mirror=True)


LEGS = [("legR", 1, -1), ("hindR", 1, 1), ("legL", -1, -1), ("hindL", -1, 1)]
LEG = [(0.08, 0.05, 0.27), (0.15, 0.12, 0.15), (0.21, 0.17, 0.065), (0.28, 0.21, 0.045), (0.33, 0.205, 0.09)]
LEG_R = [0.09, 0.08, 0.062, 0.045, 0.03]


def legs(k):
    for bone, sx, sy in LEGS:
        m = np.array([sx, sy, 1.0])
        P = np.array(LEG) * m
        k.add(f"tent{bone}", tube(P, LEG_R, seg=24, per=6), PURPLE, bone)
        # suckers on the upper outer face near the curled end
        for t, r in ((1.5, 0.028), (2.4, 0.025), (3.25, 0.019)):
            i = int(t); f = t - i
            c = P[i] + (P[i + 1] - P[i]) * f
            rr = LEG_R[i] + (LEG_R[i + 1] - LEG_R[i]) * f
            d = P[i + 1] - P[i]; d /= np.linalg.norm(d)
            o = np.array((sx, sy, 0.0)) * 0.55 + np.array((0, 0, 1.0))
            n = o - d * np.dot(o, d); n /= np.linalg.norm(n)
            k.add(f"lsucker{bone}", cb.oriented(c + n * (rr * 0.93), (r, r, r * 0.4), n), YELLOW, bone,
                  fuse=False)
    k.paint(lambda P: inside(P[:, 2] - 0.05, 0.004) * (np.hypot(P[:, 0], P[:, 1]) > 0.18), BLUE, on=["tent"])


def eye_open(u, v):
    reach = np.array([EYER(t) for t in np.arctan2(v, u)])
    rho = np.sqrt(u * u + v * v) / reach
    c = np.tile(srgb(EYEB), (len(u), 1))
    c[rho > 0.78] = srgb(RIM)
    return c


def eyes(k):
    def lay(state):
        az = math.degrees(EYE_S / HR[0])
        el = math.degrees(math.asin((EYE_Z - HC[2]) / HR[2]))
        look = eye_look(state, eye_open, srgb(YELLOW), srgb(INK), line=0.17)
        for sg in (1, -1):
            p, n = cb.on_ell(HC, HR, cb.az_el(az * sg, el))
            col = look if sg > 0 else (lambda a, b, f=look: f(-a, b))
            k.decal(p, n, EYE_HALF, col, eps=0.003, rings=20, segs=56, outline=EYER if sg > 0 else EYEL)
            if state in ("Open", "Fierce"):
                r_ = np.cross((0, 0, 1.0), n); r_ /= np.linalg.norm(r_)
                pc = p - r_ * EYE_HALF[0] * 0.25 * sg - np.array((0, 0, EYE_HALF[1] * (0.25 if state == "Open" else 0.4)))
                h = (EYE_HALF[0] * 0.3, EYE_HALF[1] * (0.42 if state == "Open" else 0.3))
                k.decal(pc, n, h, PUPIL, eps=0.0055, rings=6, segs=32)
    return lay


def marks(k):
    star = [(a, MZ + z) for a, z in STAR]
    cb.mask_paint(k, HC, HR[0], star, PALE, edge=0.003, grow=0.0)
    inner = [(a * 0.88, MZ + z * 0.88) for a, z in STAR]
    cb.mask_paint(k, HC, HR[0], inner, YELLOW, edge=0.0025)
    p, n = cb.on_ell(HC, HR, cb.az_el(100, 6))
    k.decal(p, n, (0.03, 0.03), cb.ring_look(YELLOW, "#141414", BLUE, inner=0.45, outer=0.9), eps=0.003, rings=12,
            segs=44, mirror=True)


def grapploct(src, out_path):
    k = Kit(src, coat="fur")
    body(k)
    arms(k)
    legs(k)
    marks(k)
    k.build(voxel=0.0075, smooth=4, band=0.008, wband=0.07)
    k.eye_states(eyes(k))
    k.export(out_path)


if __name__ == "__main__":
    grapploct(*args())
