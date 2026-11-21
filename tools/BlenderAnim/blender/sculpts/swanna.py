# Swanna sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/swanna.py -- <the export's Swanna.glb> <out.glb>
# Ducklett grown (its frame helpers and its way of making the eyes, rim and webbed feet, imported from ducklett.py):
# from the references, a white swan: a great round body whose folded wings bulge up over the back in two round masses
# and wrap forward round the chest; under them on the front and the flanks pale blue feathers, the white wing feathers
# coming down over them in points; a thick white neck in a gentle S to a small head with a round puff behind like a
# bun and three white points on the crown; a long thin yellow beak with a black band at its root running back to a
# half-shut dark eye; long fingered wing tips and a fan of white tail feathers trailing back low; short dark grey legs
# and broad webbed dark grey feet.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, srgb, inside, tri, unit, eye_look, polygon, args, sibling

d_ = sibling("ducklett")
p_ = sibling("pidgey")
spike = p_.spike

WHITE, BLUE, BEAK, BAND, LEG, EYE_DARK, EYE_LID = "#F3F5F9", "#93C1EE", "#F3C92F", "#26272D", "#585B63", "#34343C", "#A7A9B4"
H = np.array([0, -0.118, 0.612])            # the head's centre, over the head's joint (0, -0.1, 0.57)
HR = np.array([0.05, 0.063, 0.049])
VOXEL = 0.0062


def body(k):
    k.add("body", ell((0, 0.05, 0.205), (0.13, 0.17, 0.13), rot=(-6, 0, 0)), WHITE, "body")
    k.add("breast", ell((0, -0.07, 0.235), (0.1, 0.09, 0.1)), WHITE, "body")
    # the folded wings: two round masses over the back, wrapping forward round the chest
    for s in (1, -1):
        k.add("wingmass", ell((0.088 * s, 0.075, 0.262), (0.095, 0.165, 0.11), rot=(-8, 0, 7 * s)), WHITE,
              "wingR" if s > 0 else "wingL")
        k.add("wingfront", ell((0.085 * s, -0.04, 0.24), (0.06, 0.07, 0.085), rot=(0, 0, -20 * s)), WHITE,
              "wingR" if s > 0 else "wingL")
    # the thick neck in a gentle S, broad where it leaves the chest
    k.add("neckbase", ell((0, -0.09, 0.3), (0.07, 0.065, 0.075)), WHITE, "body")
    k.add("neck", tube([(0, -0.095, 0.3), (0, -0.125, 0.38), (0, -0.112, 0.46), (0, -0.098, 0.53), (0, -0.108, 0.6)],
                       [0.054, 0.043, 0.04, 0.039, 0.039]), WHITE, "head")
    k.add("head", ell(H, HR, rot=(-8, 0, 0)), WHITE, "head")
    # the round puff behind the head, a bun
    k.add("bun", ell((0, -0.07, 0.635), (0.046, 0.05, 0.05)), WHITE, "head")


def chest(k):
    """Pale blue under the wings on the front and the flanks, the white coming down over it in points."""
    def m(P):
        top = 0.255 - 0.035 * (tri(P[:, 0] / 0.07 + 0.5) + 1) / 2 - 0.25 * np.clip(P[:, 1] + 0.02, 0, 1)
        return inside(P[:, 2] - top, 0.003) * inside(P[:, 1] - 0.09, 0.02) * (P[:, 2] > 0.06)
    k.paint(m, BLUE, on=["body", "breast", "neckbase"])


def crest(k):
    top = H + np.array([0, 0.012, HR[2] * 0.85])
    k.add("crest", spike(top + (0, -0.02, -0.01), top + (0, 0.02, 0.075), 0.034, 0.024, side=(1, 0, 0)), WHITE,
          "head", fuse=False)
    for s in (1, -1):
        k.add("crest", spike(top + (0.018 * s, -0.012, -0.012), top + (0.045 * s, 0.03, 0.045), 0.028, 0.022,
                             side=(0, 1, 0)), WHITE, "head", fuse=False)


def beak(k):
    root = H + np.array([0, -0.052, -0.012])
    tip = root + unit(0, -1, -0.32) * 0.14
    k.add("beak", tube([root, (root * 0.55 + tip * 0.45), tip - (tip - root) * 0.03],
                       [0.017, 0.0115, 0.004]), BEAK, "head", fuse=False, surface="shell")
    k.add("band", ell(root + np.array([0, 0.004, 0]), (0.0235, 0.013, 0.021)), BAND, "head", fuse=False,
          surface="shell")


def mask(k):
    """The black band at the beak's root runs back along the face to the eye."""
    E = eye_centre()
    def m(P):
        x = np.abs(P[:, 0])
        # a strip from the beak's root (x 0, y H-0.05) back to under the eye
        t = np.clip((P[:, 1] - (H[1] - 0.05)) / (E[1] - (H[1] - 0.05)), 0, 1)
        zc = (H[2] - 0.012) + t * (E[2] - 0.004 - (H[2] - 0.012))
        return inside(np.abs(P[:, 2] - zc) - (0.012 - 0.004 * t), 0.0025) * (P[:, 1] < E[1] + 0.004) \
            * (P[:, 2] > H[2] - 0.05) * (x < 0.06)
    k.paint(m, BAND, on=["head"])


def tail(k):
    """Long fingered wing tips and a fan of tail feathers trailing back low, their faces up to the camera behind."""
    for s in (1, -1):
        for j, (dx, dz, ln) in enumerate(((0.05, -0.01, 0.16), (0.0, 0.015, 0.17))):
            base = np.array([0.1 * s, 0.17, 0.22 + dz])
            d = unit(dx * s + 0.12 * s, 1.0, -0.12 + dz)
            k.add("wingtip", spike(base, base + d * ln, 0.085, 0.024, side=(0.3 * s, 0, 1), at=0.4), WHITE,
                  "wingR" if s > 0 else "wingL", fuse=False)
    for j, yaw in enumerate((-27, -9, 9, 27)):
        y = math.radians(yaw)
        base = np.array([math.sin(y) * 0.03, 0.18, 0.18])
        d = np.array([math.sin(y), math.cos(y), 0.16])
        ln = 0.24 - 0.025 * abs(yaw) / 15
        k.add("tail", spike(base, base + d * ln, 0.095, 0.024, side=(math.cos(y), -math.sin(y), 0.25 * yaw / 30),
                            at=0.45), WHITE, "body", fuse=False)


def legs(k):
    for s, side in ((1, "R"), (-1, "L")):
        x = 0.047 * s
        k.add(f"leg{side}", tube([(x, 0.03, 0.12), (x * 1.04, 0.015, 0.06), (x * 1.08, 0.0, 0.02)],
                                 [0.019, 0.017, 0.016]), LEG, f"leg{side}", fuse=False, surface="scales")
        turn = math.radians(14 * s)
        ex = np.array([math.cos(turn), math.sin(turn), 0])
        ez = np.array([math.sin(turn), -math.cos(turn), 0])
        foot = [(a * 1.55, b * 1.55) for a, b in d_.FOOT_OUTLINE]
        k.add(f"foot{side}", slab(foot, (x * 1.12, -0.008, 0.0105), ex, ez, 0.02), LEG, f"leg{side}", fuse=False,
              surface="scales")


# --- the eyes: a half-shut dark almond under a grey lid, cut along its top by the lid's black line ------------------
EYEDIR = unit(0.62, -0.7, 0.27)
EYE = (0.024, 0.0155)
ALMOND = [(-1.0, 0.05), (-0.6, 0.6), (0.0, 0.85), (0.6, 0.65), (1.0, 0.1), (0.6, -0.6), (0.0, -0.8), (-0.6, -0.55)]


def eye_centre():
    return H + EYEDIR / np.linalg.norm(EYEDIR / HR)


def eye_open(u, v):
    c = np.tile(srgb(EYE_DARK), (len(u), 1))
    lid = 0.12 + 0.18 * u                      # the lid's line, lower toward the beak
    c[v > lid] = srgb(EYE_LID)
    c[np.abs(v - lid) < 0.16] = d_.INKC
    return c


def eyes(k):
    E = eye_centre()
    m = np.array([-1, 1, 1.0])
    LID = srgb(WHITE)
    def lay(state):
        look = eye_look(state, eye_open, LID, d_.INKC, line=0.2)
        k.decal(E, EYEDIR, EYE, look, eps=0.004, outline=polygon(ALMOND), rings=20, segs=56)
        k.decal(E * m, EYEDIR * m, EYE, lambda u, v: look(-u, v), eps=0.004,
                outline=polygon([(-a, b) for a, b in ALMOND]), rings=20, segs=56)
    return lay


def swanna(src, out):
    k = Kit(src, coat="fur")
    body(k)
    chest(k)
    # the band at the beak root is the whole of the black: a painted strip back to the eye hatched at this size
    crest(k)
    beak(k)
    tail(k)
    legs(k)
    k.build(voxel=VOXEL, band=0.008)
    k.eye_states(eyes(k))
    k.export(out)


if __name__ == "__main__":
    swanna(*args())
