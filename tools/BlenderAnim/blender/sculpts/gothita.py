# Gothita sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/gothita.py -- <the export's Gothita.glb> <out.glb>
# The base of its line: gothorita.py and gothitelle.py import its frame (the bows, the eyes, the lips, the colours).
#
# The design, from the references: a small doll. A big wide lilac head, wider than tall, an onion's shape; a black
# cap over its top coming down at the front in jagged points; a black teardrop topknot standing up out of the crown;
# two white bows on the head's upper sides like a pair of ears; large oval eyes, a light blue iris ringed in deeper
# blue, a dark pupil and a glint, a few short lashes at the outer corner; small pouting red lips. Under the head a big
# white bow at the throat, wider than the body; a small black body, its lower part a white band whose top edge is a
# zigzag of black teeth; two little pointed black arms out and down; short black legs on small feet.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, tri, eye_look, outward, args

LILAC, BLACK, WHITE, KNOT = "#B596D6", "#2E2A36", "#F6F6FA", "#C4C2D2"
LIPS, LIPDARK = "#E8505E", "#A82638"
EYEWHITE, IRIS, IRISDEEP, PUPIL, INKH = "#FFFFFF", "#62C6F2", "#2C7ACC", "#16285C", "#1A1622"

# --- the frame (the skeleton's joints: body (0, 0, 0.11), head (0, -0.01, 0.18), arms (+-0.045, 0, 0.15),
#     legs (+-0.025, 0, 0.06)) ---------------------------------------------------------------------------------------
HC = np.array([0.0, -0.004, 0.258])      # the head's middle
HR = np.array([0.113, 0.093, 0.09])


def on_ell(c, r, d):
    """The point of an ellipsoid (centre c, radii r) along direction d from its centre, and its normal there."""
    d = np.array(d, float); c = np.array(c, float); r = np.array(r, float)
    p = c + d / math.sqrt(((d / r) ** 2).sum())
    n = (p - c) / r ** 2
    return p, n / np.linalg.norm(n)


def bow(k, name, c, ax, an, length, width, thick, bone, mirror=False, colour=WHITE, knot=KNOT):
    """
    A bow: two white lobes from a knot at c, along +ax and -ax, each narrow at the knot and broad and rounded at its
    end, their faces toward an (loose pieces, crisp); a small grey knot between them.
    """
    ax = unit(*ax); an = np.array(an, float); an = an - ax * (an @ ax); an /= np.linalg.norm(an)
    aw = np.cross(ax, an)                                  # (ax, an, aw) right-handed
    c = np.array(c, float)
    M = np.stack([ax, an, aw], axis=1)
    def lobe(V):
        V = V.copy()
        t = (V[:, 0] + 1) / 2                              # 0 at the knot, 1 at the end
        w = 0.42 + 0.68 * np.sqrt(np.clip(t, 0, 1)) - 0.12 * t ** 4
        V[:, 2] *= w; V[:, 1] *= 0.55 + 0.45 * w
        return V
    for s, tag in ((1, "a"), (-1, "b")):
        V, F = ell((0, 0, 0), (length / 2, thick, width), deform=lobe, sub=3)
        V = V.copy(); V[:, 0] += length / 2 - 0.12 * length       # its narrow end in the knot
        V[:, 0] *= s
        if s < 0:
            F = [f[::-1] for f in F]
        V = V @ M.T + c
        k.add(f"{name}{tag}", outward((V, F)), colour, bone, fuse=False, mirror=mirror)
    V, F = ell((0, 0, 0), (length * 0.24, thick * 1.15, width * 0.42), sub=3)
    k.add(f"{name}knot", outward((V @ M.T + c, F)), knot, bone, fuse=False, mirror=mirror)


def lips(k, c, n, half, up=(0, 0, 1)):
    """Small pouting red lips: a red bump on the face and a patch of an upper and a lower lip with a dark line."""
    CL, CD = srgb(LIPS), srgb(LIPDARK)
    def look(u, v):
        c_ = np.tile(CL, (len(u), 1))
        c_[(np.abs(v - 0.02 * (1 - u * u)) < 0.13) & (np.abs(u) < 0.62)] = CD
        return c_
    k.decal(c, n, half, look, up=up, eps=0.003, rings=12, segs=48)


def round_eye(lashes=True):
    """The line's open eye: a white oval in a dark outline, a large light blue iris ringed in deeper blue, a dark
    pupil and a glint, short lashes at the outer corner; u outward, v up."""
    W, I, D, P, K = srgb(EYEWHITE), srgb(IRIS), srgb(IRISDEEP), srgb(PUPIL), srgb(INKH)
    def eye(u, v, lid):
        c = np.tile(lid, (len(u), 1))
        o = ((u + 0.08) / 0.8) ** 2 + (v / 0.95) ** 2
        c[o < 1] = K
        c[o < 0.72] = W
        ir = ((u + 0.1) / 0.56) ** 2 + ((v + 0.04) / 0.74) ** 2
        c[(o < 0.72) & (ir < 1)] = D
        c[(o < 0.72) & (ir < 0.66)] = I
        c[((u + 0.1) / 0.26) ** 2 + ((v + 0.04) / 0.36) ** 2 < 1] = P
        c[((u - 0.06) / 0.15) ** 2 + ((v - 0.3) / 0.17) ** 2 < 1] = W
        if lashes:
            # two short thick lashes off the outer corner, up and out
            for a, b in ((0.25, 0.58), (-0.15, 0.85)):
                d = np.array([math.cos(a), math.sin(a)])
                q0 = np.array([0.62, b * 0.55])
                t = (u - q0[0]) * d[0] + (v - q0[1]) * d[1]
                e = np.abs(-(u - q0[0]) * d[1] + (v - q0[1]) * d[0])
                c[(t > -0.05) & (t < 0.3) & (e < 0.075)] = K
        return c
    return eye


def gothita(src, out):
    k = Kit(src, coat="fur")

    # --- the body: a small black pear, its lower part a white band under a zigzag of black teeth
    k.add("torso", ell((0, 0.002, 0.096), (0.046, 0.042, 0.06)), BLACK, "body")
    def band(P):
        th = np.arctan2(P[:, 0], -P[:, 1])
        top = 0.088 - 0.015 * (0.5 + 0.5 * tri(th * 5 / (2 * math.pi)))          # black teeth pointing down
        return inside(P[:, 2] - top, 0.002) * inside(0.05 - P[:, 2], 0.002) * inside(np.hypot(P[:, 0], P[:, 1]) - 0.06)
    k.paint(band, WHITE)
    k.add("neck", ell((0, -0.004, 0.158), (0.03, 0.028, 0.026)), BLACK, "body")

    # the arms: little pointed stubs out to the sides and down, a little forward
    k.add("arm", tube([(0.03, 0.0, 0.135), (0.058, -0.008, 0.12), (0.084, -0.016, 0.098)], [0.016, 0.011, 0.006]),
          BLACK, "arm{s}", mirror=True)
    # short legs on small feet
    k.add("leg", tube([(0.024, 0.0, 0.06), (0.026, -0.004, 0.026)], [0.02, 0.018]), BLACK, "leg{s}", mirror=True)
    k.add("foot", ell((0.027, -0.012, 0.014), (0.021, 0.026, 0.014)), BLACK, "leg{s}", mirror=True)

    # --- the head: a wide lilac onion, fuller at the eyes, its bottom a little flatter
    def onion(V):
        V = V.copy()
        V[:, 2] = np.where(V[:, 2] < 0, V[:, 2] * 0.88, V[:, 2])
        w = 1 + 0.06 * (1 - V[:, 2] ** 2) - 0.08 * np.clip(V[:, 2], 0, 1) ** 2
        V[:, 0] *= w; V[:, 1] *= w
        return V
    k.add("head", ell(HC, HR, deform=onion, sub=5), LILAC, "head")
    # the black cap: over the crown, down to a jagged line at the front, lower at the back
    def cap(P):
        Q = P - HC
        th = np.arctan2(Q[:, 0], -Q[:, 1])                 # 0 at the front
        front = np.cos(th) > 0
        teeth = 0.5 + 0.5 * tri(th * 7 / (2 * math.pi) + 0.5)                 # a tooth down at the middle
        line = HR[2] * (0.5 - 0.2 * teeth * np.clip(np.cos(th), 0, 1) - 0.18 * np.clip(-np.cos(th), 0, 1))
        line = line - HR[2] * 0.12 * np.exp(-(th / 0.16) ** 2)          # the middle point a little deeper
        return inside(line - Q[:, 2], 0.002) * inside(np.linalg.norm(Q, axis=1) - 0.16)
    k.paint(cap, BLACK)
    # the topknot: a black teardrop standing out of the crown on a short stalk
    k.add("stalk", tube([(0, 0.004, HC[2] + HR[2] - 0.02), (0, 0.008, HC[2] + HR[2] + 0.012)], [0.015, 0.014]),
          BLACK, "head")
    def drop(V):
        V = V.copy()
        t = (V[:, 2] + 1) / 2
        V[:, 0] *= 0.55 + 0.45 * np.sqrt(t); V[:, 1] *= 0.55 + 0.45 * np.sqrt(t)
        return V
    k.add("knot", ell((0, 0.008, HC[2] + HR[2] + 0.042), (0.03, 0.027, 0.038), rot=(-6, 0, 0), deform=drop), BLACK,
          "head")

    # the lips: a small red pout low on the face
    lp, ln = on_ell(HC, HR, (0, -1, -0.5))
    k.add("lipbump", ell(lp + ln * -0.004, (0.016, 0.01, 0.012)), LIPS, "head")
    lips(k, lp + ln * 0.004, ln, (0.016, 0.013))

    # --- the bows: one at the throat, wider than the body; one on each of the head's upper sides
    bow(k, "neckbow", (0, -0.04, 0.163), (1, 0, 0.06), (0, -1, 0.25), 0.058, 0.03, 0.017, "body")
    bp, bn = on_ell(HC, HR, (0.66, 0.05, 0.8))
    bow(k, "headbow", bp + bn * 0.016, (1, 0.05, -0.12), (0.25, -1, 0.15), 0.068, 0.042, 0.018, "head", mirror=True)

    k.build(voxel=0.0042, smooth=4, band=0.006)

    # --- the eyes: large ovals a little apart, looking straight out
    LID, INK = srgb(LILAC), srgb(INKH)
    eye = round_eye()
    open_look = lambda u, v: eye(u, v, LID)
    ep, en = on_ell(HC, HR, (0.42, -0.9, -0.02))
    def lay(state):
        k.decal(ep, en, (0.031, 0.036), eye_look(state, open_look, LID, INK, line=0.15), eps=0.003, rings=20,
                segs=56, mirror=True)

    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    gothita(*args())
