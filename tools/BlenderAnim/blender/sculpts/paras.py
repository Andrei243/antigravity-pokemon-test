# Paras sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/paras.py -- <the export's Paras.glb> <out.glb>
#
# From the references: a small orange crab-like bug, a round domed shell that is all head and body at once. Two big
# round eyes on its front, white with a large black pupil, a blue-grey rim and a glint; a small round snout under them
# with a white tooth hanging at each side of a dark mouth; a few small darker dots across the brow. Two big orange
# pincers carried forward at its sides, each a broad crescent with a thumb, their points to the ground. Four thin
# brownish legs at the back. Two red mushrooms with large yellow spots grow out of its back, tilted apart.
#
# Written to be grown from (parasect.py imports it): the colours, the eye, the claw and the leg are functions here.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, unit, eye_look, args

ORANGE, SHADE, LEG = "#F0874A", "#D46A34", "#C25E30"
CAP, SPOT, STALK = "#D83A68", "#F6C84A", "#F2D6B4"
WHITE, RIM, PUPIL, MOUTH = "#F6F6F8", "#93A3BA", "#141418", "#2A1612"


def claw(k, s, shoulder, elbow, palm, size, color, bone):
    """A pincer: a short arm from the shoulder, a broad crescent from the palm curving down and in to a point at the
    ground, a smaller thumb inside it. s is the side (+1 on +X); points are written for +X and mirrored by s."""
    m = np.array([s, 1, 1.0])
    side = "R" if s > 0 else "L"
    k.add(f"arm{side}", tube(np.array([shoulder, elbow, palm]) * m, [size * 0.55, size * 0.6, size * 0.75]), color,
          bone, fuse=False)
    P = np.array(palm)
    main = np.array([P + (0, -0.005, 0.01), P + (0.012 * size / 0.05, -0.03, -0.045 * size / 0.05),
                     P + (0.008 * size / 0.05, -0.04, -0.1 * size / 0.05), P + (-0.022 * size / 0.05, -0.03, -0.145 * size / 0.05)])
    k.add(f"pincer{side}", blade(main * m, [size * 0.95, size, size * 0.62, size * 0.1], (0, 1, 0.2), 0.62), color,
          bone, fuse=False)
    thumb = np.array([P + (-0.01, -0.012, -0.02), P + (-0.03 * size / 0.05, -0.03, -0.055 * size / 0.05),
                      P + (-0.04 * size / 0.05, -0.034, -0.085 * size / 0.05)])
    k.add(f"thumb{side}", blade(thumb * m, [size * 0.45, size * 0.38, size * 0.08], (0, 1, 0.2), 0.62), color,
          bone, fuse=False)


def leg(k, name, s, hip, knee, tip, r, color, bone):
    m = np.array([s, 1, 1.0])
    mid = (np.array(knee) + np.array(tip)) / 2 + np.array((0.008, 0, 0.0))
    k.add(name, tube(np.array([hip, knee, mid, tip]) * m, [r, r * 0.85, r * 0.65, r * 0.3]), color, bone, fuse=False)


def round_eye(white=WHITE, rim=RIM, pupil=PUPIL, pupil_r=0.56, rim_w=0.18):
    """The line's eye: a white disc, a blue-grey rim, a large black pupil a little inward, a glint up and outward."""
    CW, CR, CP = srgb(white), srgb(rim), srgb(pupil)
    def eye(u, v):
        r = np.sqrt(u * u + v * v)
        c = np.tile(CW, (len(u), 1))
        c[r > 1 - rim_w] = CR
        c[(u + 0.08) ** 2 + (v + 0.02) ** 2 < pupil_r ** 2] = CP
        c[(u - 0.16) ** 2 + (v - 0.22) ** 2 < 0.17 ** 2] = CW
        return c
    return eye


def paras(src, out):
    k = Kit(src, coat="shell")

    # ---- the shell: a round dome that is head and body at once, its front a little flattened where the face is
    B = np.array([0.0, 0.005, 0.165])
    BR = np.array([0.132, 0.122, 0.102])
    k.add("body", ell(B, BR), ORANGE, "body")
    k.add("face", ell((0, -0.06, 0.162), (0.112, 0.075, 0.092)), ORANGE, "head")
    k.add("belly", ell((0, 0.0, 0.1), (0.1, 0.1, 0.05)), ORANGE, "body")
    # the snout under the eyes
    k.add("snout", ell((0, -0.138, 0.138), (0.032, 0.026, 0.024)), ORANGE, "head")
    # the back a shade darker toward the tail
    k.paint(lambda P: inside(0.085 - P[:, 1], 0.03), SHADE, on=["body", "belly"])

    # ---- two mushrooms on the back, tilted apart
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"stalk{side}", tube(np.array([(0.06, 0.06, 0.24), (0.078, 0.066, 0.272)]) * m, [0.026, 0.022]),
              STALK, f"shroom{side}")
        C = np.array((0.088, 0.07, 0.29)) * m
        dome = lambda V: np.where(V[:, 2:3] < 0, V * np.array([1, 1, 0.45]), V)
        k.add(f"cap{side}", ell(C, (0.07, 0.064, 0.052), rot=(-8, 24 * s, 0), deform=dome), CAP, f"shroom{side}")
        # the spots: big yellow patches over the cap, cast from its middle, most of them on the faces the game sees
        up = np.array(Euler_up(24 * s))
        for (a, b, h) in ((0.1, -0.15, 0.02), (0.75, -0.5, 0.019), (-0.55, -0.75, 0.018), (0.85, 0.4, 0.014),
                          (-0.5, 0.7, 0.017), (0.15, 1.1, 0.016)):
            d = unit(*(up * 0.8 + np.array((a * s, b, 0.0)) * 0.75))
            k.decal(C + d * 0.055, d, (h, h * 0.85), SPOT, up=(0, 1, 0) if abs(d[1]) < 0.7 else (1, 0, 0),
                    eps=0.003, rings=8, segs=28)

    # ---- the pincers, carried forward at the sides, points to the ground
    for s in (1, -1):
        claw(k, s, (0.095, -0.06, 0.15), (0.11, -0.1, 0.15), (0.112, -0.135, 0.155), 0.047, ORANGE,
             "armR" if s > 0 else "armL")

    # ---- four thin legs at the back
    for s, side in ((1, "R"), (-1, "L")):
        leg(k, f"leg{side}", s, (0.1, 0.0, 0.12), (0.142, 0.0, 0.145), (0.148, 0.015, 0.004), 0.016, LEG, f"leg{side}")
        leg(k, f"hind{side}", s, (0.092, 0.085, 0.12), (0.135, 0.1, 0.14), (0.14, 0.12, 0.004), 0.015, LEG,
            f"hind{side}")

    # ---- the teeth: a white tooth at each side of the mouth under the snout
    for s, side in ((1, "R"), (-1, "L")):
        k.add(f"tooth{side}", ell((0.018 * s, -0.148, 0.112), (0.011, 0.009, 0.016), rot=(10, 0, -12 * s)), WHITE,
              "head", fuse=False)
    # the mouth: a dark patch under the snout, between the teeth
    k.decal((0, -0.15, 0.116), unit(0, -1, -0.35), (0.014, 0.01), MOUTH, eps=0.003, rings=8, segs=32)
    # the dots on the brow
    for (x, z) in ((0.0, 0.24), (0.03, 0.236), (-0.03, 0.236), (0.055, 0.225), (-0.055, 0.225)):
        d = unit(x * 4, -0.7, (z - 0.165) * 6)
        k.decal((x, -0.1, z), d, (0.0085, 0.0085), SHADE, eps=0.003, rings=6, segs=24)

    k.build(voxel=0.005, smooth=3)

    # ---- the eyes: big round, white with a large black pupil
    E = np.array([0.064, -0.128, 0.198])
    EN = unit(0.42, -0.88, 0.2)
    eye = round_eye()
    def eyes(state):
        k.decal(E, EN, (0.036, 0.036), eye_look(state, eye, srgb(ORANGE), srgb(PUPIL), line=0.16), eps=0.004,
                rings=20, segs=56, mirror=True)
    k.eye_states(eyes)
    k.export(out)


def Euler_up(tilt):
    """The way up out of a cap tilted outward by tilt degrees about the forward axis (a little back as well)."""
    t = math.radians(tilt)
    return (math.sin(t), 0.14, math.cos(t))


if __name__ == "__main__":
    paras(*args())
