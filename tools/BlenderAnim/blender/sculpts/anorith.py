# Anorith sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/anorith.py -- <the export's Anorith.glb> <out.glb>
# Written to be grown from: armaldo.py imports it (sibling("anorith")) and keeps its frame: the white feathers with their
# red tips as solid plates, the black face with its red marks, the grey prongs as hulls, the stalked white eyes.
#
# From the references (both drawn from above): a flat ancient shrimp. A grey-green body, broad at the front and
# tapering back, a dark grey shell over its back in five segments with a green rim round it; at the front a black face
# pointing forward with two red leaf-shaped marks. Two grey blades for a tail in a V behind. Down each side a fan of
# four white feathers with red pointed tips. At the sides of the head two short stalks with big round white eyes, a
# large black pupil looking forward. Two green arms of two segments curve forward and down from under the face, each
# ending in a hooked grey claw. Feathers raised in a V (level, they are edge-on from the game's cameras) and a little
# shorter than drawn, so that the model is not far wider than tall.
# Skeleton: body (0, 0, 0.14), head (0, -0.1, 0.14), arms (+-0.04, -0.1, 0.12), tail (0, 0.17, 0.15),
# legs (+-0.04, -0.04, 0.1), hind (+-0.04, 0.08, 0.1).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, eye_look, polygon, outward, hull, slab, args

GREEN, SHELLC, SEAM, FACE = "#9CBCA2", "#5E6468", "#3E4246", "#26282C"
RED, FEATHER, GREY, WHITE, INK = "#E05866", "#F2F4F6", "#585C62", "#F4F6F6", "#121416"

# --- the frame -------------------------------------------------------------------------------------------------------
BODY_C, BODY_R = np.array([0.0, 0.03, 0.108]), (0.094, 0.15, 0.07)
HEAD_C, HEAD_R = np.array([0.0, -0.13, 0.118]), (0.072, 0.07, 0.058)


def feather(k, name, root, d, length, width, bone="body", tip_share=0.36, white=FEATHER, red=RED, thick=0.011,
            roll=18.0, toward=1.0):
    """
    A white plate along d with a red pointed tip, its face turned up and out (the plane holds d and the level line
    square to it), then rolled about d by roll degrees so its face turns toward the back too (level to the back
    camera it is a line; toward=-1 turns it to the front instead). The red tip is a thicker plate of its own over the white's end.
    """
    d = unit(*d)
    w = np.cross((0, 0, 1.0), d); w /= np.linalg.norm(w)
    n = np.cross(d, w)
    best = None
    for r in (math.radians(roll), -math.radians(roll)):
        w2 = math.cos(r) * w + math.sin(r) * n
        ny = toward * np.cross(d, w2)[1] * np.sign(np.cross(d, w2)[2])
        if best is None or ny > best[0]:
            best = (ny, w2)
    w = best[1]
    L, W = length, width / 2
    body = [(0, -W * 0.7), (L * 0.15, -W), (L * 0.7, -W), (L * 0.72, W), (L * 0.15, W), (0, W * 0.7)]
    k.add(name, slab(body, root, d, w, thick), white, bone, fuse=False, surface="shell")
    t0 = L * (1 - tip_share)
    tip = [(t0, -W * 1.08), (L * 0.84, -W * 1.12), (L, 0.0), (L * 0.84, W * 1.12), (t0, W * 1.08)]
    k.add(name + "tip", slab(tip, np.asarray(root) - np.cross(d, w) * 0.0005, d, w, thick * 1.45), red, bone,
          fuse=False, surface="shell")


def prong(k, name, root, d, length, w0, t0, bone="tail", colour=GREY):
    """A tapering blade with a kite section, at least 0.03 at its root: a tail prong, a spike."""
    d = unit(*d)
    a = np.cross(d, (0, 0, 1.0)); a /= np.linalg.norm(a)
    b = np.cross(a, d)
    root = np.asarray(root, float)
    pts = []
    for s, f in ((0.0, 1.0), (0.45, 0.82), (0.8, 0.42)):
        c = root + d * length * s
        pts += [c + a * w0 * f, c - a * w0 * f, c + b * t0 * f, c - b * t0 * f]
    pts.append(root + d * length)
    k.add(name, hull(pts), colour, bone, fuse=False, surface="shell")


RED_MARK = [(-0.95, 0.55), (-0.5, 0.95), (0.25, 0.85), (0.8, 0.35), (0.95, -0.35), (0.6, -0.9), (0.0, -0.75),
            (-0.6, -0.15)]


def red_marks(k, c, n, half, colour=RED):
    """The two red leaf-shaped marks on the black face, laid as two patches (the outline is not symmetric)."""
    for s in (1, -1):
        shape = RED_MARK if s > 0 else [(-a, b) for a, b in RED_MARK]
        k.decal(np.array(c) * (s, 1, 1), unit(*(np.array(n) * (s, 1, 1))), half, colour, eps=0.003, rings=12, segs=48,
                outline=polygon(shape))


def eye_open(pr=0.6, px=0.04, pz=0.0):
    CW, CK = srgb(WHITE), srgb(INK)

    def eye(u, v):
        c = np.tile(CW, (len(u), 1))
        c[((u - px) ** 2 + (v - pz) ** 2) < pr * pr] = CK
        return c
    return eye


def ball_eyes(k, state, c, n, half, lid=WHITE, look=None):
    """The eyes on round balls: a big black pupil in white, the lid white."""
    lk = eye_look(state, look or eye_open(), srgb(lid), srgb(INK), line=0.18)
    for s in (1, -1):
        col = lk if s > 0 else (lambda f: (lambda u, v: f(-u, v)))(lk)
        k.decal(np.array(c) * (s, 1, 1), unit(*(np.array(n) * (s, 1, 1))), (half, half), col, eps=0.004, rings=20,
                segs=56)


# --- the parts -------------------------------------------------------------------------------------------------------
def body(k):
    def taper(V):
        V = V.copy()
        back = np.clip(V[:, 1], 0, 1)
        V[:, 0] *= 1 - 0.38 * back ** 1.3
        V[:, 2] *= 1 - 0.2 * back
        return V
    k.add("body", ell(BODY_C, BODY_R, deform=taper), GREEN, "body")
    k.add("belly", ell((0, 0.01, 0.062), (0.078, 0.13, 0.048)), GREEN, "body")
    k.add("head", ell(HEAD_C, HEAD_R), GREEN, "head")
    k.add("snout", ell((0, -0.172, 0.128), (0.05, 0.045, 0.042)), GREEN, "head")
    # the shell over the back, its rim left green, its seams dark
    def plate(P):
        x, y, z = P[:, 0], P[:, 1], P[:, 2]
        e = (x / (0.074 - 0.025 * np.clip((y - 0.03) / 0.13, 0, 1))) ** 2 + ((y - 0.025) / 0.128) ** 2
        return inside(e - 1, 0.04) * inside(0.13 - z, 0.004)
    k.paint(plate, SHELLC)
    k.paint(lambda P: plate(P) * sum(inside(np.abs(P[:, 1] - y) - 0.0032, 0.002)
                                     for y in (-0.055, -0.015, 0.025, 0.065, 0.1)), SEAM)
    # the black face, pointing forward
    def face(P):
        x, y, z = P[:, 0], P[:, 1], P[:, 2]
        return inside(y + 0.118 + 0.45 * np.abs(x), 0.003) * inside(0.1 - z, 0.004) * inside(np.abs(x) - 0.066, 0.003)
    k.paint(face, FACE)


def tail(k):
    k.add("tailstub", tube([(0, 0.13, 0.12), (0, 0.17, 0.13), (0, 0.195, 0.14)], [0.04, 0.03, 0.022]), GREEN, "tail")
    for s, side in ((1, "R"), (-1, "L")):
        prong(k, f"prong{side}", (0.008 * s, 0.185, 0.14), (0.36 * s, 0.82, 0.45), 0.17, 0.02, 0.016)


def feathers(k, lengths=(0.14, 0.14, 0.13, 0.12), lift=32):
    rise = math.radians(lift)
    for s, side in ((1, "R"), (-1, "L")):
        for i, (y, sweep, L) in enumerate(zip((-0.06, -0.02, 0.02, 0.06), (-6, 9, 24, 39), lengths)):
            ph = math.radians(sweep)
            d = (s * math.cos(rise) * math.cos(ph), math.cos(rise) * math.sin(ph), math.sin(rise))
            root = (s * (0.07 - 0.008 * i), y, 0.142 + 0.006 * i)
            feather(k, f"feather{side}{i}", root, d, L, 0.05)


def arms(k):
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1])
        sh, el, wr = np.array([0.05, -0.135, 0.1]), np.array([0.1, -0.205, 0.105]), np.array([0.098, -0.27, 0.05])
        k.add(f"upperarm{side}", tube([sh * m, ((sh + el) / 2 + (0.01, 0, 0.01)) * m, el * m], [0.024, 0.025, 0.026]),
              GREEN, f"arm{side}")
        k.add(f"elbow{side}", ell(el * m, (0.03, 0.03, 0.03)), GREEN, f"arm{side}")
        k.add(f"forearm{side}", tube([el * m, ((el + wr) / 2 + (0.006, -0.012, 0.0)) * m, wr * m],
                                     [0.027, 0.026, 0.022]), GREEN, f"arm{side}")
        # the hooked grey claw, down and in
        b = wr * m
        pts = [b + np.array([x, y, z]) * m for x, y, z in ((0.017, 0.0, 0.012), (-0.017, 0.0, 0.012),
                                                          (0.017, 0.014, -0.008), (-0.017, 0.014, -0.008),
                                                          (0.014, -0.016, -0.006), (-0.014, -0.016, -0.006),
                                                          (-0.004, -0.018, -0.032), (-0.026, -0.008, -0.05))]
        k.add(f"claw{side}", hull(pts), GREY, f"arm{side}", fuse=False, surface="shell")


def legs(k):
    for s, side in ((1, "R"), (-1, "L")):
        k.add(f"leg{side}", tube([(0.04 * s, -0.04, 0.05), (0.062 * s, -0.05, 0.025), (0.07 * s, -0.06, 0.014)],
                                 [0.021, 0.02, 0.02]), GREEN, f"leg{side}")
        k.add(f"hind{side}", tube([(0.04 * s, 0.08, 0.05), (0.06 * s, 0.09, 0.025), (0.066 * s, 0.1, 0.014)],
                                  [0.021, 0.02, 0.019]), GREEN, f"hind{side}")


STALK_ROOT, BALL_C, BALL_R = np.array([0.05, -0.15, 0.13]), np.array([0.142, -0.163, 0.138]), 0.031


def eyestalks(k):
    k.add("eyestalk", tube([STALK_ROOT, (STALK_ROOT + BALL_C) / 2, BALL_C], [0.021, 0.02, 0.02]), GREEN, "head",
          mirror=True)
    k.add("eyeball", ell(BALL_C, (BALL_R, BALL_R, BALL_R)), WHITE, "head", mirror=True)


EYE_N = (0.62, -1.0, 0.18)


def eye_at():
    n = unit(*EYE_N)
    return BALL_C + n * BALL_R


def anorith(src, out):
    k = Kit(src, coat="shell")
    body(k)
    tail(k)
    feathers(k)
    arms(k)
    legs(k)
    eyestalks(k)
    red_marks(k, (0.028, -0.17, 0.152), (0.25, -0.55, 0.8), (0.017, 0.023))
    k.build(voxel=0.005, smooth=4, band=0.005)
    k.eye_states(lambda st: ball_eyes(k, st, eye_at(), EYE_N, 0.025))
    k.export(out)


if __name__ == "__main__":
    anorith(*args())
