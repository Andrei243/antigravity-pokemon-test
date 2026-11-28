# Muscarita (No. 10100, Carpathia) sculpted from nothing on the skeleton and clips of the game's own export of it:
#   blender --background --factory-startup --python sculpts/muscarita.py -- <the export's Muscarita.glb> <out.glb>
#
# Palariuta grown (palariuta.py, sibling("palariuta")): its scarlet cap with the curl, spots and gill lines, its moss,
# its amber eye on a dark shadow. From its picture (10100-Muscarita.jpg) and its brief (CP-dex-2b-mountains-more.md,
# 100), largest first: a great cap, some 0.62 of the height across, tilted like a dancer's hat (its high side on +X
# so the face shows to the game's front camera; the picture tilts it the other way), its broad underside of radial
# cream-pink gills showing; a slim white figure under it: a small white head with a dark mask band across two amber
# eyes and a sweet small smile; a frilled cream ring collar with moss on it; a slim torso and a flared pale pink frilled
# skirt; slim white legs in pointed moss boots; slim arms with moss at the wrists. The picture's dance (legs crossed,
# one arm raised) is built as the rest pose: legs apart and straight, both arms forward and a little down; the spores
# are dropped.
# The cap is a mesh of its own ("cap"), so the eyes' patches are cast on the body alone.
# Skeleton: body (0, 0, 0.501), head (0, 0, 0.661), arms (+-0.06, 0, 0.601), legs (+-0.04, 0, 0.361).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, sheet, outward, srgb, inside, smoothstep, unit, eye_look, args, sibling

P_ = sibling("palariuta")
P_.GILL, P_.GILL_D = "#F3E2D4", "#D9BBAE"         # the grown cap's gills, cream going pink

WHITE_, SKIRT, FRILL, MASK = "#F4F0E8", "#F2D6D4", "#E6DFAE", "#33202A"
SMILE = "#7A3A44"

CAPC = np.array([0.0, 0.03, 0.80])
CAPR = np.array([0.29, 0.27, 0.11])
CAPROT = (-17.0, -14.0, 0.0)              # the front up, +X up: the face and the gills to the front camera
HC = np.array([0.0, -0.004, 0.686])
HR = np.array([0.074, 0.068, 0.078])
EYE = np.array([0.032, -0.0605, 0.703])
EYE_HALF = (0.021, 0.0175)
VOXEL = 0.0056


def frill(c, r0, r1, drop, waves, amp, phase=0.0):
    """A ruffled ring of cloth (a collar, a skirt): from radius r0 at c's height out and down to r1, drop below it,
    its outer edge waving in height and out; its seam behind."""
    c = np.array(c, float)
    def at(u, v):
        th = math.pi / 2 + 2 * math.pi * u
        w = np.sin(waves * th + phase)
        r = r0 + (r1 - r0) * v + amp * 0.6 * v ** 2 * w
        z = c[2] - drop * v + amp * v ** 2 * w
        return np.stack([c[0] + r * np.cos(th), c[1] + r * np.sin(th), z], axis=-1)
    return outward(sheet(at, 97, 7, 0.008))


def muscarita(src, out_path):
    k = Kit(src, coat="leaf")

    # ---- the cap, a mesh of its own
    P_.toadstool(k, CAPC, CAPR, CAPROT, "head", 16, seed=8, under=0.45, curl=0.4, stem_r=0.035, lines=22,
                 spot_size=0.1)
    k.build(name="cap", voxel=VOXEL, smooth=4, band=0.005, wband=0.03)

    # ---- the head under it, the stalk's top going up into the cap
    k.add("head", ell(HC, HR), WHITE_, "head")
    k.add("capstalk", tube([HC + (0, 0.004, 0.04), HC + (0.004, 0.012, 0.1), HC + (0.008, 0.018, 0.13)],
                           [0.04, 0.034, 0.034]), WHITE_, "head")
    def mask(P):
        q = P - HC
        th = np.arctan2(q[:, 0], -q[:, 1])
        return inside(((q[:, 2] - 0.018) / 0.024) ** 2 + (th / 1.25) ** 4 - 1, 0.04) * (q[:, 1] < 0.02) \
            * inside(np.linalg.norm(q / HR, axis=1) - 1.3)
    k.paint(mask, MASK, on=["head"])
    CW, CS = srgb(WHITE_), srgb(SMILE)
    def smile(u, v):
        c = np.tile(CW, (len(u), 1))
        c[(np.abs(v - (0.7 * u * u - 0.3)) < 0.32) & (np.abs(u) < 0.9)] = CS
        return c
    k.decal(HC + (0, -0.0665, -0.022), unit(0, -1, -0.3), (0.017, 0.0085), smile, eps=0.0015, rings=10, segs=48)

    # ---- the neck, the collar of cream frill with moss on it, the torso
    k.add("neck", tube([(0, 0.0, 0.56), (0, -0.002, 0.62), HC + (0, 0, -0.04)], [0.03, 0.027, 0.03]), WHITE_, "body")
    k.add("torso", ell((0, 0.0, 0.5), (0.056, 0.048, 0.085)), WHITE_, "body")
    k.add("hips", ell((0, 0.0, 0.43), (0.06, 0.052, 0.05)), WHITE_, "body")
    k.add("collar", frill((0, 0.0, 0.624), 0.034, 0.112, 0.048, 8, 0.016), FRILL, "body", fuse=False,
          surface="cloth")
    P_.moss_ring(k, "collarmoss", (0, 0.0, 0.628), 0.047, 0.022, 14, "body", seed=3, z_wave=0.004)
    k.add("skirt", frill((0, 0.0, 0.468), 0.05, 0.16, 0.1, 9, 0.02, phase=0.4), SKIRT, "body", fuse=False,
          surface="cloth")

    # ---- slim legs apart, in pointed moss boots
    for s in (1, -1):
        side = "R" if s > 0 else "L"
        hip, knee, ankle = np.array([0.042 * s, 0.0, 0.4]), np.array([0.046 * s, -0.012, 0.22]), np.array(
            [0.046 * s, -0.004, 0.075])
        k.add("leg" + side, tube([hip, knee, ankle], [0.031, 0.025, 0.02]), WHITE_, "leg" + side)
        k.add("boot" + side, ell((0.046 * s, -0.022, 0.036), (0.03, 0.05, 0.036), rot=(-12, 0, 0)), P_.MOSS,
              "leg" + side)
        k.add("toe" + side, ell((0.046 * s, -0.066, 0.02), (0.018, 0.024, 0.018)), P_.MOSS_D, "leg" + side)
        P_.moss_ring(k, "cuff" + side, ankle + (0, 0, 0.0), 0.022, 0.013, 9, "leg" + side, seed=20 + s, z_wave=0.003)

    # ---- slim arms forward and a little down, moss at the wrists, small white hands
    for s in (1, -1):
        side = "R" if s > 0 else "L"
        sh, el, wr = np.array([0.05 * s, 0.0, 0.6]), np.array([0.118 * s, -0.03, 0.532]), np.array(
            [0.148 * s, -0.082, 0.468])
        k.add("arm" + side, tube([sh, el, wr], [0.019, 0.017, 0.016]), WHITE_, "arm" + side)
        k.add("hand" + side, ell(wr + (0.006 * s, -0.016, -0.02), (0.017, 0.013, 0.026), rot=(25, 0, 0)), WHITE_,
              "arm" + side)
        P_.moss_ring(k, "wrist" + side, wr + (0, 0.004, 0.006), 0.019, 0.011, 8, "arm" + side, seed=30 + s,
                     z_wave=0.002)

    body = k.build(voxel=VOXEL, smooth=4, band=0.005, wband=0.03)
    P_.board(body)

    n = unit(EYE[0] / HR[0] ** 2, EYE[1] / HR[1] ** 2, (EYE[2] - HC[2]) / HR[2] ** 2)
    look = P_.amber_eye(pupil=(0.3, 0.62))
    def lay(state):
        k.decal(EYE, n, EYE_HALF, eye_look(state, look, srgb(MASK), srgb(P_.AMBER), line=0.18), eps=0.0016,
                rings=20, segs=56, mirror=True)
    k.eye_states(lay)
    k.export(out_path)


if __name__ == "__main__":
    muscarita(*args())
