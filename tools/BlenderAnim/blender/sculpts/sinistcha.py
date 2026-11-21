# Sinistcha sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/sinistcha.py -- <the export's Sinistcha.glb> <out.glb>
#
# Grown from Poltchageist's script (sibling("poltchageist")): the same cream, black glaze, matcha, bamboo, painted
# loops and seal. From the references: a deep tea bowl glazed dark brown-black, a thick brown rim and a small foot; a
# cream panel on its front, to +X, painted with two big loops and a wavy line under them, the seal at its +X edge.
# The ghost of matcha fills the bowl and rises out of it leaning to -X: a gourd-shaped green head with yellow curls
# for eyes and a small smile, its two arms draped over the front rim on the -X side with little fingers hanging, and
# two round green feet under the bowl. On its head it wears a bamboo tea whisk: a pale tube of a handle, a black cord
# tied under it and the thin tines flaring down round the back and sides of the head. (The task's text spoke of a
# mound of green ghost: the pictures show it a gourd-shaped head and arms over the rim, the face findable.)
# The skeleton: body (0, 0, 0.176), head (0, 0, 0.216), armL (-0.06, -0.02, 0.216), armR (0.06, -0.02, 0.216).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, lathe, outward, args, sibling

po = sibling("poltchageist")
si = po.si

CREAM, INK, MATCHA, MATCHA_DARK = po.CREAM, po.INK, po.MATCHA, po.MATCHA_DARK
BAMBOO, BAMBOO_DARK = po.BAMBOO, po.BAMBOO_DARK
BOWL, RIM, FOOT = "#2E2523", "#7A4A2A", "#4A3428"
CURL_YELLOW, CURL_INK = "#F2DC52", "#3C6E32"

# --- the frame ----------------------------------------------------------------------------------------------------
BOWL_P = [(0.045, 0.046), (0.08, 0.056), (0.11, 0.08), (0.125, 0.11), (0.13, 0.145), (0.13, 0.172), (0.127, 0.19)]
BOWL_TOP = 0.19
HEAD_C, HEAD_R = np.array([-0.034, -0.004, 0.262]), np.array([0.058, 0.054, 0.064])
EYE_DX, EYE_DZ, EYE_HALF = 0.42, 0.06, (0.0172, 0.0172)
LEAN = unit(-0.22, 0.06, 1.0)
VOXEL = 0.0032


def on_bowl(a, z, inset=0.002):
    return po.on_jar(a, z, BOWL_P, inset)


def on_ell(c, r, d):
    d = unit(*d)
    c, r = np.array(c, float), np.array(r, float)
    p = c + d / math.sqrt(((d / r) ** 2).sum())
    n = (p - c) / r ** 2
    return p, n / np.linalg.norm(n)


def wave_look(u, v):
    c = np.tile(srgb(CREAM), (len(u), 1))
    c[(np.abs(v - 0.38 * np.sin(u * 8.5)) < 0.3) & (np.abs(u) < 0.95)] = srgb(INK)
    return c


# --- the parts ----------------------------------------------------------------------------------------------------
def bowl(k):
    k.add("bowl", si.cup_solid(BOWL_P, BOWL_TOP), BOWL, "body")
    az = lambda P: np.arctan2(P[:, 0], -P[:, 1])
    rad = lambda P: np.hypot(P[:, 0], P[:, 1])
    # the brown band under the rim
    k.paint(lambda P: inside(0.17 - P[:, 2], 0.002) * (rad(P) > 0.118), RIM)
    # the cream panel on the front, to +X
    k.paint(lambda P: inside(np.abs(az(P) - 0.6) - 0.74, 0.0016) * inside(P[:, 2] - 0.167, 0.0016)
            * inside(0.058 - P[:, 2], 0.0016) * (rad(P) > 0.1), CREAM)
    # matcha filling it to the brim
    k.paint(lambda P: inside(BOWL_TOP - 0.006 - P[:, 2], 0.0015) * inside(rad(P) - 0.118, 0.0015), MATCHA)
    k.add("rim", si.gold_ring((0, 0, BOWL_TOP), 0.125, 0.0095), RIM, "body", fuse=False)
    k.add("foot", lathe((0, 0, 0), (0, 0, 1), [(0.0, 0.028), (0.05, 0.028), (0.054, 0.034), (0.05, 0.05),
                                                (0.0, 0.05)], seg=56), FOOT, "body", fuse=False)
    # the panel's loops, the wavy line under them and the seal at its +X edge
    for a in (0.3, 0.92):
        p, n = on_bowl(a, 0.128)
        k.decal(p, n, (0.03, 0.036), po.loop_eye, eps=0.0026, rings=20, segs=56)
    p, n = on_bowl(0.6, 0.078)
    k.decal(p, n, (0.06, 0.01), wave_look, eps=0.0026, rings=10, segs=72)
    p, n = on_bowl(1.36, 0.14)
    k.decal(p, n, (0.016, 0.019), po.seal_look, eps=0.0028, outline=po.HEXAGON, rings=14, segs=60)


def ghost(k):
    k.add("pool", ell((0.0, 0.0, 0.19), (0.115, 0.115, 0.02)), MATCHA, "body", surface="skin")
    k.add("trunk", ell((-0.022, 0.0, 0.212), (0.07, 0.066, 0.04)), MATCHA, "head", surface="skin")
    k.add("ghosthead", ell(HEAD_C, HEAD_R), MATCHA, "head", surface="skin")
    def smile(u, v):
        c = np.tile(srgb(MATCHA), (len(u), 1))
        line = -0.25 + 0.6 * u * u
        c[(np.abs(v - line) < 0.28) & (np.abs(u) < 0.9)] = srgb(CURL_INK)
        return c
    p, n = on_ell(HEAD_C, HEAD_R, (0.0, -1.0, -0.42))
    k.decal(p, n, (0.012, 0.0062), smile, eps=0.0022, rings=10, segs=56)


def arms(k):
    """Both arms draped over the front rim on the -X side, little fingers hanging over the outside."""
    for bone, root, over, low in (("armL", (-0.062, -0.03, 0.218), (-0.1, -0.084, 0.206), (-0.106, -0.104, 0.172)),
                                  ("armR", (-0.004, -0.05, 0.212), (-0.034, -0.126, 0.205), (-0.04, -0.142, 0.17))):
        k.add("arm" + bone[-1], outward(tube([root, over, low], [0.0115, 0.0105, 0.0105])), MATCHA, bone,
              fuse=False, surface="skin")
        low = np.array(low)
        d = unit(*(low - np.array(over)))
        k.add("hand" + bone[-1], outward(ell(low, (0.014, 0.011, 0.011))), MATCHA, bone, fuse=False, surface="skin")
        for j, dx in enumerate((-0.009, 0.0, 0.009)):
            k.add(f"finger{bone[-1]}{j}", outward(tube([low + (dx, -0.002, -0.004), low + (dx * 1.3, -0.004, -0.02)],
                                                       [0.0058, 0.0052])), MATCHA, bone, fuse=False, surface="skin")
    for s in (1, -1):
        k.add("feet" + ("R" if s > 0 else "L"), outward(ell((0.042 * s, -0.012, 0.018), (0.022, 0.027, 0.018))),
              MATCHA, "body", fuse=False, surface="skin")


def whisk(k):
    """The whisk worn on the head: the handle up along the lean, a black cord, tines down round the back and sides."""
    top = HEAD_C + LEAN * (HEAD_R[2] - 0.004)
    k.add("handle", lathe(top, LEAN, [(0.0, -0.004), (0.022, -0.004), (0.023, 0.06), (0.019, 0.064),
                                      (0.012, 0.064), (0.012, 0.05), (0.0, 0.05)], seg=48), BAMBOO, "head",
          fuse=False)
    k.add("cord", si.gold_ring(top + LEAN * 0.006, 0.0235, 0.0045, axis=LEAN), INK, "head", fuse=False)
    for i in range(11):
        th = math.radians(60 + i * 24)                 # 60..300 degrees from the front: none over the face
        out = unit(math.sin(th), -math.cos(th), 0.0)          # azimuth th from the front (-Y), +X positive
        out = unit(*(out - LEAN * (out @ LEAN)))
        p0 = top + out * 0.016 + LEAN * 0.004
        p1 = HEAD_C + out * (HEAD_R[0] + 0.008) + LEAN * 0.035
        p2 = HEAD_C + out * (HEAD_R[0] + 0.011) - LEAN * 0.008
        k.add(f"tine{i}", outward(tube([p0, p1, p2], [0.0058, 0.0056, 0.0052])), BAMBOO, "head", fuse=False)


def lay_eyes(k, state):
    for s in (1, -1):
        p, n = on_ell(HEAD_C, HEAD_R, (EYE_DX * s, -1.0, EYE_DZ))
        def curl(u, v):
            return si.curl_look(MATCHA, CURL_YELLOW, width=0.16)(u, v)
        look = eye_look(state, curl, srgb(MATCHA), srgb(CURL_INK), line=0.17)
        k.decal(p, n, EYE_HALF, look if s > 0 else (lambda u, v, f=look: f(-u, v)), eps=0.0022, rings=20, segs=56)


def sinistcha(src, dst):
    k = Kit(src, coat="shell")
    bowl(k)
    ghost(k)
    arms(k)
    whisk(k)
    k.build(voxel=VOXEL, smooth=3, band=0.004, wband=0.03)
    k.eye_states(lambda st: lay_eyes(k, st))
    k.export(dst)


if __name__ == "__main__":
    sinistcha(*args())
