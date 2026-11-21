# Gourgeist (the average size) sculpted from nothing on the skeleton and clips of the game's own export of it
# (sculptkit.py):
#   blender --background --factory-startup --python sculpts/gourgeist.py -- <the export's Gourgeist.glb> <out.glb>
#
# Pumpkaboo grown (pumpkaboo.py: its lobed pumpkin, the curl of its stalk, its slit eyes): the pumpkin is now the
# body, dark brown, an egg narrowing upward into a long gold neck, with a glowing jack-o'-lantern face carved on it
# (two triangles and a wide crescent grin with one tooth). On the neck a small pale-gold face with one yellow slit
# eye showing and a closed smile; a mop of salmon hair over it, a lock falling across the face's other side and one
# down its right cheek, the dark stalk curling up out of the hair to the left. Two long flat locks of hair are its
# arms, each ending in a hand of three fingers: the art throws one up and out, here both hang forward and down before
# the pumpkin's shoulders.
# The skeleton: body (0, 0, 0.176), head (0, -0.02, 0.311), armR/armL at (+-0.04, -0.01, 0.361).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, polygon, srgb, inside, smoothstep, unit, eye_look, sibling, args

PB = sibling("pumpkaboo")
BODY, BODYRIB, STALK = "#5A4632", "#3E3024", "#5E4430"
NECKDARK, NECK, FACE = "#8C7234", "#C49A48", "#DDB868"
HAIR, HAIRDARK = "#EF9583", "#D9786A"
CARVE, CARVEDEEP = "#FFD84A", "#D48A1C"
EYEINK, SMILE = PB.EYEINK, "#6E4A22"

BODYC = np.array([0.0, 0.0, 0.12]); BODYR = np.array([0.11, 0.1, 0.095])
HEADC = np.array([0.0, -0.014, 0.352]); HEADR = np.array([0.052, 0.047, 0.056])


def egg(V):
    """Pumpkaboo's lobes on an egg: narrowing toward the top, where the neck rises."""
    V = PB.lobes(8, 0.045)(V)
    t = np.clip(V[:, 2], 0, 1)
    V[:, :2] *= (1 - 0.42 * t ** 1.6)[:, None]
    return V


def body(k):
    PB.pumpkin(k, BODYC, BODYR, BODY, BODYRIB, "body", extra=egg)
    k.add("neck", tube([(0, 0.0, 0.19), (0, -0.004, 0.235), (0, -0.008, 0.28), (0, -0.012, 0.32)],
                        [0.046, 0.033, 0.03, 0.038]), NECKDARK, "body")
    k.paint(lambda P: smoothstep(0.2, 0.31, P[:, 2]), NECK, on=["neck"])


def lantern(k):
    """The glowing jack-o'-lantern face: two triangles leaning in and a wide crescent grin with one tooth."""
    for side in (1, -1):
        tri_ = [(-0.9 * side, -0.75), (1.0 * side, -0.75), (-0.3 * side, 1.0)]
        def look(u, v):
            c = np.tile(srgb(CARVE), (len(u), 1))
            c[v > 0.3] = srgb(CARVEDEEP)
            return c
        k.decal((0.044 * side, -0.094, 0.158), unit(0.42 * side, -1, 0.18), (0.023, 0.023), look, eps=0.003,
                outline=polygon(tri_), rings=14, segs=72)
    body_c, carve, deep = srgb(BODY), srgb(CARVE), srgb(CARVEDEEP)
    def grin(u, v):
        e1 = u * u + ((v - 0.25) / 1.2) ** 2 < 1
        e2 = (u / 0.92) ** 2 + ((v - 0.92) / 1.0) ** 2 < 1
        band = (u / 0.98) ** 2 + ((v - 0.92) / 1.16) ** 2 < 1
        c = np.tile(body_c, (len(u), 1))
        m = e1 & ~e2
        c[m] = carve
        c[m & band] = deep
        tooth = (np.abs(u - 0.18) < 0.13) & (v > -0.32)
        c[tooth & m] = body_c
        return c
    k.decal((0, -0.106, 0.09), unit(0, -1, -0.12), (0.074, 0.04), grin, eps=0.003, rings=40, segs=120)


def head(k):
    k.add("head", ell(HEADC, HEADR), FACE, "head")
    # the hair: a cap over the back and top of the head, its front edge well behind the face
    k.add("cap", ell((0.0, 0.02, 0.378), (0.06, 0.05, 0.048)), HAIR, "head", surface="hair")
    # one lock hanging over the face's -X side (as both pictures have it), and a short one by its +X cheek
    k.add("fringe", blade([(-0.016, -0.04, 0.405), (-0.034, -0.06, 0.372), (-0.046, -0.058, 0.338), (-0.052, -0.044, 0.305)],
                          [0.02, 0.02, 0.017, 0.012], (0, 1, 0), 0.6), HAIR, "head", surface="hair")
    k.add("cheek", blade([(0.046, -0.004, 0.375), (0.058, -0.016, 0.345), (0.062, -0.016, 0.318)],
                         [0.013, 0.012, 0.009], (0, 1, 0), 0.6), HAIR, "head", surface="hair")
    PB.curl(k, (0.0, 0.03, 0.416), 0.85, STALK, "head", turn=180)
    # the closed smile, under the eye
    k.decal((0.01, -0.061, 0.326), unit(0.2, -1, -0.1), (0.014, 0.0055), smile_line, eps=0.003, rings=10, segs=60)


def smile_line(u, v):
    c = np.tile(srgb(FACE), (len(u), 1))
    c[np.abs(v - (-0.35 + 0.8 * u * u)) < 0.32] = srgb(SMILE)
    return c


def arms(k):
    """Two flat locks from the back of the head's sides, hanging down and a little forward beside the neck and the
    pumpkin, each tapering to a hand of three fingers, clear of the lantern face."""
    for side in (1, -1):
        bone = "armR" if side == 1 else "armL"
        m = np.array([side, 1, 1.0])
        path = [np.array(p) * m for p in ((0.042, 0.022, 0.372), (0.074, 0.012, 0.34), (0.1, -0.008, 0.282),
                                          (0.118, -0.026, 0.222), (0.124, -0.04, 0.17))]
        k.add(f"lock{side}", blade(path, [0.024, 0.024, 0.021, 0.018, 0.015], (0, 1, 0), 0.6), HAIR, bone,
              surface="hair")
        hand = np.array([0.125, -0.044, 0.158]) * m
        k.add(f"palm{side}", ell(hand, (0.019, 0.016, 0.018)), HAIR, bone, surface="hair")
        for i, (dx, dy) in enumerate(((-0.012, -0.006), (0.0, -0.01), (0.012, -0.004))):
            f0 = hand + np.array([dx * side, dy, -0.01])
            k.add(f"finger{side}{i}", tube([f0, f0 + (dx * 0.5 * side, -0.008, -0.018), f0 + (dx * 0.8 * side, -0.004, -0.032)],
                                           [0.009, 0.008, 0.0066]), HAIR, bone, surface="hair")


def eyes(k, state):
    k.decal((0.019, -0.059, 0.352), unit(0.32, -1, 0.05), (0.0083, 0.018),
            eye_look(state, PB.slit, srgb(FACE), srgb(EYEINK), line=0.17), eps=0.0045, rings=20, segs=56)


def gourgeist(src, out_path):
    k = Kit(src, coat="leaf")
    body(k)
    lantern(k)
    head(k)
    arms(k)
    k.build(voxel=0.0042, smooth=3, band=0.005, wband=0.03)
    k.eye_states(lambda s: eyes(k, s))
    k.export(out_path)


if __name__ == "__main__":
    gourgeist(*args())
