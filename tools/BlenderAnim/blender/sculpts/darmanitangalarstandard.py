# Darmanitan-Galar-Standard sculpted from nothing on the skeleton and clips of the game's own export of it
# (sculptkit.py), grown from darmanitan.py (its ape's frame: long thick arms down to the ground, short hind legs, the
# zigzag cuff) and darumakagalar.py (its white, blues and ice, imported as sibling("darumakagalar")).
#   blender --background --factory-startup --python sculpts/darmanitangalarstandard.py -- <the export's .glb> <out.glb>
#
# The design, from the references (ranked by size): a great snowball on its head, the largest mass, as wide as its
# stance and half its height, three faceted ice diamonds across its front (the middle one the largest); a white ape's
# body under it, long thick white arms down to the ground ending in blue fists under a scalloped cuff of white fur;
# under the snowball's front edge a small face: a blue mask with two small round eyes (white, a dark-blue rim, a
# black pupil) and a small blue nose; below the face a great puffy white beard in lobes, hiding the mouth; two ice
# diamonds one over the other low on the belly; short hind legs on blue three-toed feet. No brows and no tail (the
# pictures show none).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, tri, eye_look, args, sibling

g = sibling("darumakagalar")
SNOW, SHADE, BLUE, RIM, ICE, INK = g.SNOW, g.SHADE, g.BLUE, g.RIM, g.ICE, g.INK

TORSO_C, TORSO_R = np.array([0.0, 0.0, 0.28]), np.array([0.19, 0.17, 0.205])
FACE_C, FACE_R = np.array([0.0, -0.13, 0.43]), np.array([0.1, 0.075, 0.075])
BALL_C, BALL_R = np.array([0.0, 0.0, 0.615]), np.array([0.235, 0.22, 0.22])


def arms(k):
    """Long thick white arms down to the ground on blue fists, a scalloped cuff of white fur over each."""
    SH, EL, WR = np.array([0.14, -0.05, 0.42]), np.array([0.225, -0.11, 0.26]), np.array([0.24, -0.15, 0.13])
    k.add("arm", tube([SH, EL, WR], [0.08, 0.075, 0.086]), SNOW, "leg{s}", mirror=True)
    FIST = np.array([0.245, -0.16, 0.05])
    k.add("fist", ell(FIST, (0.074, 0.08, 0.056)), BLUE, "leg{s}", mirror=True)
    # the cuff: a ring of fur lobes flaring over the top of the fist
    for i in range(9):
        a = 2 * math.pi * i / 9
        p = FIST + np.array([0.082 * math.cos(a), 0.088 * math.sin(a), 0.045 + 0.006 * (i % 2)])
        k.add(f"cuff{i}", ell(p, (0.03, 0.03, 0.03), sub=3), SNOW, "leg{s}", mirror=True)
    k.add("cuffcore", ell(FIST + (0, 0, 0.05), (0.08, 0.085, 0.038)), SNOW, "leg{s}", mirror=True)


def hind_legs(k):
    k.add("haunch", tube([(0.095, 0.07, 0.22), (0.11, 0.065, 0.13), (0.12, 0.05, 0.07)], [0.08, 0.07, 0.058]),
          SNOW, "hind{s}", mirror=True)
    FC = np.array([0.122, 0.04, 0.026])
    k.add("foot", ell(FC, (0.05, 0.06, 0.028)), BLUE, "hind{s}", mirror=True)
    for i, a in enumerate((-30, 0, 30)):
        dd = unit(math.sin(math.radians(a)), -math.cos(math.radians(a)), 0)
        k.add(f"toe{i}", tube([FC + dd * 0.04, FC + dd * 0.064], [0.019, 0.017]), BLUE, "hind{s}", mirror=True)


def beard(k):
    """The puffy white beard under the face: a broad roll with lobes along its lower edge."""
    k.add("beard", ell((0.0, -0.17, 0.36), (0.13, 0.07, 0.06)), SNOW, "head")
    for i, x in enumerate((-0.1, -0.05, 0.0, 0.05, 0.1)):
        k.add(f"lobe{i}", ell((x, -0.18 + 0.02 * abs(x) / 0.1, 0.33 - 0.012 * (i % 2)), (0.042, 0.045, 0.04), sub=3),
              SNOW, "head")
    k.add("cheekR", ell((0.1, -0.15, 0.405), (0.045, 0.05, 0.045)), SNOW, "head")
    k.add("cheekL", ell((-0.1, -0.15, 0.405), (0.045, 0.05, 0.045)), SNOW, "head")


def face(k):
    """The blue face under the snowball's front edge: a mask, a small nose."""
    k.add("face", ell(FACE_C, FACE_R), BLUE, "head")
    k.add("nose", ell(FACE_C + (0, -0.075, -0.006), (0.024, 0.018, 0.016)), BLUE, "head")


def ice(k):
    """Three diamonds across the snowball's front, the middle the largest; two low on the belly."""
    for i, (az, z, w, h) in enumerate(((-0.42, 0.652, 0.04, 0.037), (0.0, 0.668, 0.055, 0.05),
                                       (0.42, 0.652, 0.04, 0.037))):
        p, n = g.d.on_ball(BALL_C, BALL_R, az, z)
        g.ice_diamond(k, f"snowice{i}", p, n, w, h, 0.028, bone="head")
    for i, z in enumerate((0.19, 0.125)):
        p, n = g.d.on_ball(TORSO_C, TORSO_R, 0.0, z)
        g.ice_diamond(k, f"bellyice{i}", p, n, 0.042, 0.022, 0.018)


def eye_open(u, v):
    return g.eye_open(u, v, pupil=(-0.05, 0.0, 0.36), rim=0.72)


def eyes(k, state):
    for side in (1, -1):
        p, n = g.d.on_ball(FACE_C, FACE_R, side * 0.62, 0.447)
        look = eye_look(state, eye_open, srgb(BLUE), srgb(RIM), line=0.16)
        f = look if side > 0 else (lambda u, v, L=look: L(-u, v))
        k.decal(p, n, (0.03, 0.031), f, eps=0.0035, rings=20, segs=56)


def darmanitan_galar(src, out):
    k = Kit(src, coat="fur")
    k.add("torso", ell(TORSO_C, TORSO_R), SNOW, "body")
    k.add("chest", ell((0.0, -0.07, 0.36), (0.14, 0.12, 0.12)), SNOW, "body")
    k.add("snowball", ell(BALL_C, BALL_R), SNOW, "head")
    face(k)
    beard(k)
    arms(k)
    hind_legs(k)
    # the snowball's lower part a little shaded, as the pictures have it under its brim
    k.paint(lambda P: 0.6 * smoothstep(0.56, 0.47, P[:, 2]), SHADE, on=["snowball"])
    ice(k)
    k.build(voxel=0.0066, smooth=3, band=0.006)
    k.eye_states(lambda s: eyes(k, s))
    k.export(out)


if __name__ == "__main__":
    darmanitan_galar(*args())
