# Marshadow sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/marshadow.py -- <the export's Marshadow.glb> <out.glb>
#
# From the references: a small grey shadow, most of it a great rounded hood of a head. Its upper front is in deep
# shadow, a black band over the face from the brow down across the eyes; in it two tall eyes, orange round red with a
# yellow core and a small yellow flame-drop over each. On the hood's top two curled lobes like snail shells, a spiral
# on their fronts and a small point rising from each; the hood's lower sides puffed out beside the face; on the top a
# round topknot on a short stalk with a hooked wisp curling to its right (+X). A small frowning mouth under the shadow.
# A ruffled scarf of pale grey round the neck, one end trailing out to the right; a short body dark at the chest and
# fading to grey, short stubby legs, small round fists hanging forward at its sides, a thin wisp of a tail trailing
# behind on the ground to the right.
# Ratios (3D picture, topknot included): head (hood, chin to lobes' tops) / height 0.47, head width / body width 1.3,
# eye height / head height 0.3. The export stands 0.41 tall: voxel 0.004, the thickness floor 0.012.
# Bones: body (0, 0, 0.13); head (0, 0, 0.2); arms (+-0.04, 0, 0.17); legs (+-0.03, 0, 0.08); tail (0, 0.04, 0.08).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, lumpy, args

GREY, GREY_DK, GREY_LT, SHADOW = "#8A8D93", "#6C6F76", "#A9ACB1", "#25262B"
ORANGE, RED, YELLOW, LINE = "#F7681C", "#D9301C", "#FBE04A", "#7E8288"

H = np.array([0.0, -0.004, 0.25])         # the head's (hood's) centre
HR = np.array([0.104, 0.092, 0.098])


def on_ell(c, r, d):
    d = unit(*d)
    s = 1 / math.sqrt(((d / np.array(r)) ** 2).sum())
    p = np.array(c) + d * s
    n = (p - c) / np.array(r) ** 2
    return p, n / np.linalg.norm(n)


def hood(V):
    """The hood: a ball a little flattened at the top, widest low down where the cheeks puff out."""
    V = V.copy()
    V[:, 0] *= 1 + 0.08 * np.clip(-V[:, 2], 0, 1) * (1 - np.clip(-V[:, 2], 0, 1))
    V[:, 2] = np.where(V[:, 2] > 0, V[:, 2] * 0.94, V[:, 2])
    return V


def add_head(k):
    k.add("head", ell(H, HR, deform=hood, sub=5), GREY, "head")
    # the puffed lower sides of the hood beside the face, curling out a little
    k.add("cheek", ell(H + (0.078, 0.008, -0.05), (0.04, 0.05, 0.042)), GREY, "head", mirror=True)
    k.add("cheektip", tube([(0.1, 0.0, 0.205), (0.122, -0.005, 0.2), (0.128, -0.01, 0.214)], [0.02, 0.013, 0.008]),
          GREY, "head", mirror=True)
    # the two curled lobes on the hood's top, each with a small point rising out of it
    k.add("lobe", ell((0.07, 0.006, 0.325), (0.045, 0.04, 0.04)), GREY, "head", mirror=True)
    k.add("lobetip", tube([(0.09, 0.006, 0.348), (0.104, 0.004, 0.37), (0.1, 0.0, 0.384)], [0.016, 0.01, 0.006]),
          GREY, "head", mirror=True)
    # each lobe's spiral, a darker raised curl on its front (loose, so it keeps its line)
    pts = []
    for t in np.linspace(0, 1.75, 22):
        a = 2 * math.pi * t
        r = 0.026 * (1 - t / 2.1)
        pts.append((0.072 + r * math.cos(a + 0.6), -0.034 + 0.01 * (t / 1.75), 0.325 + r * math.sin(a + 0.6)))
    k.add("curl", tube(pts, [0.0062] * len(pts), seg=10, per=3), GREY_DK, "head", fuse=False, mirror=True)
    # the topknot: a ball on a short stalk, a hooked wisp curling out to +X
    k.add("stalk", tube([(0, 0.006, 0.33), (0, 0.006, 0.352)], [0.017, 0.014]), GREY, "head")
    k.add("knot", ell((0, 0.006, 0.372), (0.028, 0.026, 0.026)), GREY, "head")
    k.add("wisp", tube([(0.02, 0.006, 0.378), (0.046, 0.006, 0.392), (0.066, 0.006, 0.39), (0.072, 0.006, 0.375),
                        (0.064, 0.006, 0.366)], [0.012, 0.01, 0.008, 0.0065, 0.006]), GREY, "head")

    # the shadow over the upper face: from the brow down across the eyes, a soft lower edge
    def shade(P):
        q = P - H
        a = np.arctan2(q[:, 0], -q[:, 1])
        # a rounded band: highest in the middle, its ends curving down beside the eyes, the lower edge across the
        # eyes' lower halves (they hang below it into the grey, as in the pictures)
        s = np.clip(np.abs(a) / 1.2, 0, 1)
        top = H[2] + 0.058 - 0.03 * s ** 2.2
        bot = H[2] - 0.012 + 0.012 * s ** 2 - 0.008 * np.exp(-(a / 0.25) ** 2)
        front = inside(np.abs(a) - 1.2, 0.08) * (q[:, 1] < 0.02)
        near = np.linalg.norm(q, axis=1) < 0.13
        return inside(P[:, 2] - top, 0.004) * inside(bot - P[:, 2], 0.009) * front * near
    k.paint(shade, SHADOW, on=["head", "cheek"])
    # the small frowning mouth under the shadow
    p, n = on_ell(H, HR, (0, -0.9, -0.62))
    def mouth(u, v):
        return np.tile(srgb(SHADOW), (len(u), 1))
    k.decal(p, n, (0.011, 0.0048), mouth, eps=0.002, rings=6, segs=36,
            outline=lambda th: 1.0 - 0.25 * max(0.0, math.sin(th)))


def add_body(k):
    # a short body, dark at the chest fading to grey, sitting low
    k.add("torso", ell((0, 0.004, 0.1), (0.07, 0.06, 0.066)), GREY, "body")
    k.paint(lambda P: smoothstep01((P[:, 2] - 0.085) / 0.05) * (np.abs(P[:, 0]) < 0.085), SHADOW, on=["torso"])
    # the ruffled scarf, pale, round the neck, one end trailing out to +X
    k.add("scarf", ell((0, -0.004, 0.155), (0.095, 0.086, 0.026), deform=lumpy(7, 1, 0.16)), GREY_LT, "body")
    k.add("scarfend", tube([(0.085, 0.0, 0.152), (0.112, 0.004, 0.148), (0.134, 0.0, 0.138), (0.142, -0.006, 0.126)],
                           [0.02, 0.016, 0.012, 0.007]), GREY_LT, "body")
    # short stubby legs and feet
    k.add("leg", tube([(0.032, 0.0, 0.07), (0.034, -0.002, 0.03), (0.034, -0.004, 0.016)], [0.03, 0.027, 0.026]),
          GREY, "leg{s}", mirror=True)
    k.add("foot", ell((0.035, -0.01, 0.014), (0.03, 0.034, 0.016)), GREY, "leg{s}", mirror=True)
    # arms: short, round fists hanging forward at the sides
    k.add("arm", tube([(0.05, 0.0, 0.135), (0.066, -0.016, 0.11), (0.072, -0.03, 0.088)], [0.022, 0.02, 0.019]),
          GREY, "arm{s}", mirror=True)
    k.add("fist", ell((0.074, -0.036, 0.078), (0.026, 0.026, 0.026)), GREY, "arm{s}", mirror=True)
    # the tail: a thin wisp trailing down behind onto the ground and out to +X
    k.add("tail", tube([(0, 0.05, 0.07), (0.012, 0.08, 0.035), (0.04, 0.1, 0.012), (0.08, 0.112, 0.008),
                        (0.11, 0.12, 0.012)], [0.012, 0.009, 0.0075, 0.0068, 0.006]), GREY, "tail")


def smoothstep01(t):
    t = np.clip(t, 0, 1)
    return t * t * (3 - 2 * t)


def open_eye(u, v):
    """The whole patch is the eye: an orange rim round red, a tall yellow core."""
    c = np.tile(srgb(ORANGE), (len(u), 1))
    c[u * u + v * v < 0.62] = srgb(RED)
    c[(u / 0.3) ** 2 + (v / 0.6) ** 2 < 1] = srgb(YELLOW)
    return c


def fierce_eye(u, v):
    c = open_eye(u, v)
    edge = 0.3 + 0.45 * u
    c[v > edge] = srgb(SHADOW)
    return c


def drop(u, v):
    return np.tile(srgb(YELLOW), (len(u), 1))


def eyes(k, state):
    lid, ink = srgb(SHADOW), srgb(LINE)
    for side in (1, -1):
        p, n = on_ell(H, HR, (0.44 * side, -0.9, -0.13))
        if state == "Fierce":
            f = fierce_eye
        else:
            f = eye_look(state, open_eye, lid, ink, line=0.16)
        col = f if side == 1 else (lambda a, b, g=f: g(-a, b))
        k.decal(p, n, (0.021, 0.035), col, eps=0.002, rings=20, segs=56)
        if state in ("Open", "Fierce"):
            # the flame-drop over the eye: a round foot, its point upward
            q, m = on_ell(H, HR, (0.42 * side, -0.88, 0.22))
            k.decal(q, m, (0.006, 0.011), drop, eps=0.0026, rings=8, segs=40,
                    outline=lambda th: 1.0 if math.sin(th) < 0 else 1.0 - 0.6 * math.sin(th) ** 3)


def marshadow(src, out):
    k = Kit(src, coat="fur")
    add_head(k)
    add_body(k)
    k.build(voxel=0.004, smooth=4, band=0.005, wband=0.03)
    k.eye_states(lambda s: eyes(k, s))
    k.export(out)


if __name__ == "__main__":
    marshadow(*args())
