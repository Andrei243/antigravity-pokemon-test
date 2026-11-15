# Celebi sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/celebi.py -- <the export's Celebi.glb> <out.glb>
# From the references: a small pale green fairy that floats; a great head shaped like an onion, round below and drawn
# up to a point leaning back, two darker green ridges up its front tinged blue near the top; two long antennae from the
# top of the forehead arcing up and out, their ends blue; very large eyes low on the face, a heavy black rim round a
# blue iris with a white glint; a small smile; a tiny body under the head, thin arms held forward with three small
# fingers, short legs with deeper green feet hanging, and two pairs of small clear wings behind.
# The export's joints: body (0, 0, 0.237), head (0, -0.01, 0.297), arms (+-0.05, 0, 0.267), wings (+-0.03, 0.05,
# 0.277), legs (+-0.03, 0, 0.167).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, outward, srgb, inside, unit, eye_look, args

GREEN, DEEP, RIDGE, BLUE = "#D4ECA2", "#86BC58", "#94C466", "#3E98D8"
WING, WINGRIM, INK, IRIS, IRISDK, GLINT = "#E6F4F0", "#CFE6E2", "#18181E", "#3AA6E0", "#1E5C9E", "#FFFFFF"
MOUTH = "#3C4A2E"

H = np.array([0.0, 0.0, 0.405])            # the head's centre, over the head's joint (0, -0.01, 0.297)
HR = np.array([0.136, 0.126, 0.13])


def onion(V):
    """A ball drawn a little narrower toward its top, fuller in its lower half."""
    V = V.copy()
    z = V[:, 2]
    s = 1 + 0.05 * np.clip(-z, 0, 1) - 0.12 * np.clip(z, 0, 1) ** 1.5
    V[:, 0] *= s; V[:, 1] *= s
    return V


def celebi(src, out):
    k = Kit(src, coat="fur")

    # The head: an onion, round below, drawn up to a point leaning back
    k.add("head", ell(H, HR, deform=onion), GREEN, "head")
    k.add("face", ell(H + (0, -0.045, -0.03), (0.118, 0.09, 0.09)), GREEN, "head")
    k.add("tip", tube([H + (0, 0.005, 0.07), H + (0, 0.02, 0.16), H + (0, 0.05, 0.22), H + (0, 0.085, 0.255)],
                      [0.088, 0.05, 0.024, 0.008]), GREEN, "head")

    # The two ridges up the front: darker green, from over each eye up toward the point, blue near their tops
    def ridge(colour_top):
        def mask(P):
            q = P - H
            phi = np.arctan2(q[:, 0], -q[:, 1])
            r = np.hypot(q[:, 0], q[:, 1])
            m = np.zeros(len(P))
            for s in (1, -1):
                t = np.clip((q[:, 2] - 0.06) / 0.14, 0, 1)
                aim = s * (0.78 - 0.32 * t)
                band = inside(np.abs(phi - aim) * np.maximum(r, 0.02) - 0.0065, 0.0025)
                span = inside(0.06 - q[:, 2], 0.004) * inside(q[:, 2] - 0.185, 0.004) * (q[:, 1] < 0.03)
                if colour_top:
                    span = span * inside(0.155 - q[:, 2], 0.004)
                m = np.maximum(m, band * span)
            return m
        return mask
    k.paint(ridge(False), RIDGE)
    k.paint(ridge(True), BLUE)

    # The antennae: from the top of the forehead, arcing up and out, their ends blue
    A = [(0.05, -0.07, 0.48), (0.08, -0.09, 0.545), (0.12, -0.08, 0.6), (0.165, -0.05, 0.625), (0.19, -0.02, 0.615)]
    k.add("antenna", outward(tube(A[:3], [0.012, 0.012, 0.0105])), GREEN, "head", fuse=False, mirror=True)
    k.add("antennatip", outward(tube(A[2:], [0.0122, 0.0122, 0.011])), BLUE, "head", fuse=False, mirror=True)

    # The tiny body under the head, the legs short and hanging, the feet deeper green
    k.add("body", ell((0, 0.005, 0.245), (0.05, 0.045, 0.055)), GREEN, "body")
    k.add("thigh", ell((0.03, 0.0, 0.185), (0.028, 0.03, 0.036)), GREEN, "leg{s}", mirror=True)
    k.add("leg", tube([(0.032, -0.008, 0.17), (0.036, -0.014, 0.14)], [0.022, 0.024]), DEEP, "leg{s}", mirror=True)
    k.add("foot", ell((0.037, -0.024, 0.125), (0.026, 0.033, 0.025)), DEEP, "leg{s}", mirror=True)
    k.paint(lambda P: inside(P[:, 2] - 0.168, 0.003) * (P[:, 2] > 0.08) * (np.abs(P[:, 0]) > 0.006), DEEP,
            on=["thigh", "leg", "foot", "body"])

    # The arms: thin, held forward and a little out, three small fingers
    k.add("arm", tube([(0.045, -0.01, 0.27), (0.075, -0.06, 0.25), (0.098, -0.11, 0.232)], [0.016, 0.015, 0.014]),
          GREEN, "arm{s}", mirror=True)
    HC = np.array([0.102, -0.122, 0.229])
    k.add("hand", ell(HC, (0.017, 0.016, 0.014)), GREEN, "arm{s}", mirror=True)
    for j, d in enumerate([(0.7, -0.6, 0.35), (0.2, -0.95, 0.1), (0.75, -0.4, -0.5)]):
        k.add(f"finger{j}", tube([HC, HC + unit(*d) * 0.028], [0.009, 0.0075]), GREEN, "arm{s}", mirror=True)

    # The wings: two small clear plates a side behind the shoulders, raised in a V, faces to front and back
    for nm, ex, L, w, root in (("wingup", unit(0.75, 0.4, 0.55), 0.15, 0.058, (0.03, 0.06, 0.29)),
                               ("winglo", unit(0.8, 0.45, -0.25), 0.11, 0.045, (0.03, 0.06, 0.265))):
        ez = np.array([0, 0.35, 1.0]) - ex * (np.array([0, 0.35, 1.0]) @ ex)
        outline = [(L * 0.5 * (1 - math.cos(t)), w * math.sin(t) * (0.8 + 0.2 * math.cos(t)))
                   for t in np.linspace(0, 2 * math.pi, 28, endpoint=False)]
        k.add(nm, slab(outline, root, ex, ez, 0.022), WING, "wing{s}", fuse=False, mirror=True)
        rim = [(L * 0.5 * (1 - 0.86 * math.cos(t)) + 0.004, w * 0.82 * math.sin(t)) for t in
               np.linspace(0, 2 * math.pi, 28, endpoint=False)]
        k.add(nm + "vein", slab([(L * 0.1, -0.005), (L * 0.85, -0.004), (L * 0.85, 0.004), (L * 0.1, 0.005)], root, ex,
                                ez, 0.026), WINGRIM, "wing{s}", fuse=False, mirror=True)

    # The mouth: a small smile low between the eyes
    G, M = srgb(GREEN), srgb(MOUTH)
    def smile(u, v):
        c = np.tile(G, (len(u), 1))
        c[(np.abs(v - (0.7 * u * u - 0.2)) < 0.24) & (np.abs(u) < 0.85)] = M
        return c
    k.decal(H + (0, -0.13, -0.085), unit(0, -1, -0.45), (0.022, 0.011), smile, eps=0.004, rings=10, segs=48)

    # The eyes: very large, low on the face; a heavy black rim, a blue iris darker at its top, a glint
    K, I, D, W = srgb(INK), srgb(IRIS), srgb(IRISDK), srgb(GLINT)
    def eye(u, v):
        c = np.tile(K, (len(u), 1))
        r = (u / 0.78) ** 2 + (v / 0.8) ** 2
        c[r < 1] = I
        c[(r < 1) & (v > 0.25)] = D
        c[((u + 0.08) / 0.24) ** 2 + ((v + 0.05) / 0.3) ** 2 < 1] = D
        c[((u + 0.25) / 0.2) ** 2 + ((v - 0.28) / 0.22) ** 2 < 1] = W
        return c
    de = unit(0.5, -0.82, -0.12)
    E = H + (0, 0, -0.035) + de * np.array([0.13, 0.125, 0.12])
    def eyes(state):
        k.decal(E, de, (0.048, 0.056), eye_look(state, eye, G, K, line=0.15), eps=0.005, rings=20, segs=56,
                mirror=True)

    k.build(voxel=0.0045, smooth=4, band=0.006)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    celebi(*args())
