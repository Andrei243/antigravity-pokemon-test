# Drampa sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/drampa.py -- <the export's Drampa.glb> <out.glb>
# From the references: a long teal dragon lying along the ground, an old grandfather of a dragon. Its head is held low
# at the front: a long teal head, the lower face and muzzle pale green with a curl of moustache at each corner of the
# mouth, a small round black eye high on the snout, a yellow ring like a curled brow over each cheek with a pink ball
# in it, a round white puff of beard under the chin, and long white locks of hair falling back and down from the crown
# over the neck. Behind the head a great white cloud of fur, lobed like heaped feathers and shaded grey-beige
# underneath, lies on the ground; out of it the body rises in a tall teal arch, its inner side pale green, and comes
# down behind into the teal hind body, which carries a wavy teal fin on its back and three pale spots on each side.
# The tail ends in a huge white plume of cloud rising behind, its top pointed forward, a curl hooked down at its back.
# No arms show in the pictures (the brief's small arms are hidden in the cloud).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, lathe, srgb, inside, smoothstep, unit, polygon, eye_look, axes, lumpy, args

TEAL, PALE = "#3E9C93", "#C3D9A6"
CLOUD, SHADE = "#F1EFE2", "#B9B19E"
YELLOW, PINK = "#F2D23A", "#E7A3C8"
INK, WHITE = srgb("#15161C"), np.array([1.0, 1.0, 1.0])


def on_ell(c, r, d):
    d = unit(*d)
    t = 1.0 / math.sqrt(((d / np.array(r)) ** 2).sum())
    return np.array(c, float) + d * t


def along(c, d, half_len, w, t, deform=None):
    """An ellipsoid laid along a direction: half_len along d, w across it (level), t across both."""
    a, b = axes(d)
    d = unit(*d)
    V, F = ell((0, 0, 0), (w, t, half_len), deform=deform, sub=3)
    return V @ np.stack([a, b, d]) + np.array(c, float), F


def torus(c, d, R, r, seg=36, rseg=14):
    """A ring of turning (a lathe of a circle)."""
    prof = [(R + r * math.cos(a), r * math.sin(a)) for a in np.linspace(0, 2 * math.pi, rseg + 1)]
    return lathe(c, d, prof, seg=seg)


def drampa(src, out):
    k = Kit(src, coat="fur")

    # --- the head: low at the front, a long teal head over a pale green muzzle -------------------------------------
    H, HR = np.array([0.0, -0.335, 0.285]), np.array([0.085, 0.1, 0.09])
    k.add("head", ell(H, HR), TEAL, "head", surface="scales")
    S, SR = np.array([0.0, -0.43, 0.262]), np.array([0.066, 0.085, 0.066])
    k.add("snout", ell(S, SR, rot=(-6, 0, 0)), TEAL, "head", surface="scales")
    k.add("jaw", ell((0, -0.4, 0.222), (0.07, 0.09, 0.045)), PALE, "head", surface="scales")
    k.add("neck", tube([(0, -0.29, 0.25), (0, -0.2, 0.22), (0, -0.11, 0.2)], [0.065, 0.06, 0.06]), TEAL, "head",
          surface="scales")
    # the pale lower face: under a line from the snout's tip back under the eye
    def lowface(P):
        q = P - S
        line = -0.006 + 0.12 * (q[:, 1] + 0.02)
        return inside(q[:, 2] - line, 0.003) * inside(P[:, 1] + 0.28, 0.004)
    k.paint(lowface, PALE, on=["head", "snout", "jaw"])
    # the moustache's curls at the mouth's corners, a nostril on the snout's tip, the mouth's line
    k.decal((0, -0.505, 0.247), unit(0, -1, -0.15), (0.032, 0.0045), "#3E5A44", eps=0.003, rings=4, segs=40)
    k.decal(on_ell(S, SR, (0.25, -1, 0.5)), unit(0.25, -1, 0.5), (0.0045, 0.0055), "#24524C", eps=0.003, rings=4, segs=20,
            mirror=True)
    for s in (1, -1):
        m = np.array([s, 1, 1.0])
        k.add("curl" + ("R" if s > 0 else "L"), tube(np.array([(0.04, -0.47, 0.232), (0.064, -0.45, 0.218),
                                                               (0.07, -0.425, 0.228), (0.06, -0.415, 0.243)]) * m,
                                                     [0.013, 0.014, 0.013, 0.011]), PALE, "head", surface="scales")

    # the beard: a round white puff hanging under the chin
    k.add("beard", ell((0, -0.405, 0.165), (0.04, 0.036, 0.036), deform=lumpy(3, 2, 0.08)), CLOUD, "head")
    k.add("beardroot", ell((0, -0.4, 0.195), (0.028, 0.03, 0.025)), CLOUD, "head")

    # the hair: long thick white locks falling back and down from the crown over the neck
    locks = [(0.03, 0.0, 0.0), (0.062, 0.01, -0.02), (0.088, 0.02, -0.035)]
    for s in (1, -1):
        m = np.array([s, 1, 1.0])
        for j, (x, dy, dz) in enumerate(locks):
            pts = np.array([(x * 0.7, -0.31, 0.37), (x + 0.01, -0.25 + dy, 0.36 + dz * 0.3),
                            (x + 0.03, -0.18 + dy, 0.29 + dz), (x + 0.035, -0.15 + dy, 0.2 + dz)]) * m
            k.add(f"lock{j}" + ("R" if s > 0 else "L"), tube(pts, [0.03, 0.033, 0.031, 0.026]), CLOUD, "head")
    k.add("lockmid", tube([(0, -0.3, 0.375), (0, -0.23, 0.37), (0, -0.16, 0.31)], [0.032, 0.034, 0.03]), CLOUD, "head")

    # the brows: a yellow ring on each side of the head with a pink ball in it
    for s in (1, -1):
        side = "R" if s > 0 else "L"
        rc = np.array([0.083 * s, -0.33, 0.33])
        k.add("ring" + side, torus(rc, unit(s, -0.35, 0.15), 0.028, 0.012), YELLOW, "head", fuse=False, surface="shell")
        k.add("pink" + side, ell(rc + np.array([-0.006 * s, 0.0, 0.0]), (0.019, 0.019, 0.019), sub=3), PINK, "head",
              fuse=False, surface="skin")

    # --- the cloud of fur it lies in: a core and heaped lobes radiating out and down ------------------------------
    C = np.array([0.0, 0.07, 0.105])
    k.add("cloudcore", ell(C, (0.16, 0.21, 0.13), deform=lambda V: np.c_[V[:, :2], np.maximum(V[:, 2], -0.75)]), CLOUD, "body")
    rs = np.random.RandomState(780)
    n = 0
    for el, count, reach in ((-5, 11, 1.0), (30, 8, 0.85)):
        for j in range(count):
            az = math.radians(360 * (j + 0.5 * (el > 0)) / count + rs.uniform(-8, 8))
            d = np.array([math.sin(az), -math.cos(az), math.sin(math.radians(el + rs.uniform(-6, 6)))])
            base = C + np.array([0.13 * math.sin(az), -0.17 * math.cos(az), 0.0]) * 0.75
            half = 0.085 * reach
            c = base + unit(*d) * half
            c[2] = max(c[2], 0.045)
            k.add(f"lobe{n}", along(c, d, half, 0.06, 0.045), CLOUD, "body")
            n += 1
    # puffs up the front where the neck goes in, so the head comes out of the cloud
    for j, (x, y, z, r) in enumerate([(0.07, -0.13, 0.17, 0.065), (-0.07, -0.13, 0.17, 0.065), (0.0, -0.1, 0.22, 0.06)]):
        k.add(f"lobefront{j}", ell((x, y, z), (r, r, r * 0.9), deform=lumpy(3, 2, 0.06)), CLOUD, "body")
    k.paint(lambda P: smoothstep(0.1, 0.02, P[:, 2]) * 0.9, SHADE, on=["cloud", "lobe"])

    # --- the body: up out of the cloud in a tall arch and down into the hind body ---------------------------------
    arch = np.array([(0, -0.08, 0.16), (0, -0.1, 0.3), (0, -0.09, 0.43), (0, -0.045, 0.535), (0, 0.02, 0.565),
                     (0, 0.07, 0.52), (0, 0.09, 0.42), (0, 0.105, 0.31), (0, 0.13, 0.24)])
    k.add("arch", tube(arch, [0.042, 0.042, 0.041, 0.04, 0.04, 0.04, 0.041, 0.044, 0.05], seg=20, per=8), TEAL, "body",
          surface="scales")
    # its inner side pale green: toward the middle of the arch
    O = np.array([0.0, 0.0, 0.36])
    line = tube(arch, [0.01] * len(arch), seg=4, per=30)[0]
    def inner(P):
        Q = line[np.concatenate([((P[a:a + 4000, None, :] - line[None]) ** 2).sum(2).argmin(1)
                                 for a in range(0, len(P), 4000)])]
        toO = O - Q; toO[:, 0] = 0
        toO /= np.linalg.norm(toO, axis=1, keepdims=True) + 1e-9
        off = P - Q
        return inside(0.012 - (off * toO).sum(1), 0.003) * inside(np.abs(P[:, 0]) - 0.034, 0.004) * (P[:, 2] > 0.23)
    k.paint(inner, PALE, on=["arch"])

    # the hind body: a teal mass on the cloud behind the arch
    R0 = np.array([0.0, 0.17, 0.25])
    k.add("hind", ell(R0, (0.08, 0.1, 0.075), rot=(-8, 0, 0)), TEAL, "tail", surface="scales")
    k.add("hindlow", ell((0, 0.19, 0.19), (0.09, 0.11, 0.06)), TEAL, "tail", surface="scales")
    # the three pale spots on each side
    for (dy, dz) in ((0.0, 0.0), (0.03, 0.006), (0.014, -0.025)):
        d = unit(1, 0.05, -0.1)
        p = on_ell(R0, (0.08, 0.1, 0.075), d) + np.array([0, 0.02 + dy, -0.01 + dz])
        k.decal(p, d, (0.0085, 0.011), PALE, eps=0.003, rings=5, segs=24, mirror=True)
    # the wavy fin on its back: three thick lobes rising and curling back, turned a little to one side
    for j, (y, z, h) in enumerate(((0.1, 0.3, 0.07), (0.15, 0.315, 0.065), (0.2, 0.31, 0.05))):
        b = np.array([0.0, y, z])
        tip = b + np.array([0.02, 0.03, h])
        k.add(f"fin{j}", hull([b + (0.0, -0.03, -0.01), b + (0.0, 0.03, -0.01), b + (0.016, 0.0, 0.0),
                               b + (-0.016, 0.0, 0.0), tip, tip + (0.0, -0.025, -0.02),
                               b + (0.006, -0.01, h * 0.6), b + (-0.006, -0.01, h * 0.6)]),
              TEAL, "tail", fuse=False, surface="scales")

    # --- the tail: a short teal root into a huge white plume of cloud rising behind -------------------------------
    k.add("tailroot", tube([(0, 0.22, 0.25), (0, 0.29, 0.29), (0, 0.33, 0.33)], [0.055, 0.05, 0.045]), TEAL, "tail",
          surface="scales")
    T = np.array([0.0, 0.36, 0.37])
    k.add("plume", ell(T, (0.1, 0.13, 0.15), rot=(20, 0, 0)), CLOUD, "tail")
    k.add("plumetop", tube([(0, 0.34, 0.45), (0, 0.29, 0.53), (0, 0.235, 0.55)], [0.085, 0.05, 0.018]), CLOUD, "tail")
    k.add("plumecurl", tube([(0, 0.44, 0.3), (0, 0.5, 0.22), (0, 0.51, 0.15), (0, 0.47, 0.11)],
                            [0.07, 0.05, 0.035, 0.024]), CLOUD, "tail")
    k.add("plumebulge", ell((0, 0.43, 0.34), (0.085, 0.09, 0.11)), CLOUD, "tail")

    # --- the eyes: small round black eyes high on the snout, a white glint ----------------------------------------
    def eye(u, v):
        c = np.tile(INK, (len(u), 1))
        c[(u + 0.25) ** 2 + (v - 0.3) ** 2 < 0.09] = WHITE
        return c
    de = unit(0.55, -0.62, 0.55)
    E = on_ell(S, SR, de)
    m = np.array([-1, 1, 1.0])
    def eyes(state):
        look = eye_look(state, eye, srgb(TEAL), INK, line=0.18)
        k.decal(E, de, (0.019, 0.022), look, eps=0.004, rings=20, segs=56)
        k.decal(E * m, de * m, (0.019, 0.022), lambda u, v: look(-u, v), eps=0.004, rings=20, segs=56)

    k.build(voxel=0.0058, smooth=4, band=0.004)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    drampa(*args())
