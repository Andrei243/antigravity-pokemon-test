# Miltank sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py). It borrows
# teddiursa.py's small helpers (the ray onto a part, the paint round an upright axis, the hull claw for its hooves).
#   blender --background --factory-startup --python sculpts/miltank.py -- <the export's Miltank.glb> <out.glb>
#
# The design, from the references: a pink upright cow, round and heavy, the belly hanging low; a great cream-yellow
# udder over the lower front with four pink teats; a big head with a pink face and a black hood over its crown and
# back, coming down in a point between the eyes and round the neck as a black collar; two small cream horns; black ears
# out to the sides, cream inside; big blue eyes under the hood's edge; a round pink snout with a black nose and a wide
# open smiling mouth, dark red with a pink tongue; short arms and legs ending in black cloven hooves; black patches on
# its back and hip; a long thin cream tail ending in a black ball.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, polygon, srgb, inside, unit, boxy, eye_look, args, sibling

t = sibling("teddiursa")
PINK, BLACK, UDDER, TEAT, HORN = "#F2A4B8", "#2E2C3A", "#F4E6A6", "#EE8EA6", "#EFE6C8"
LINING, TAIL = "#EADB98", "#EAD994"
WHITE, BLUE, PUPIL, MOUTH, TONGUE = "#FFFFFF", "#3A70C8", "#141420", "#8A2236", "#EC7C8E"


def hoof(k, name, c, d, size, bone):
    """A black cloven hoof: two flat-faced toes side by side along d, a notch between them."""
    c, d = np.array(c, float), unit(*d)
    a = np.cross(d, (0, 0, 1.0))
    if np.linalg.norm(a) < 1e-3:
        a = np.array([1.0, 0, 0])
    a /= np.linalg.norm(a)
    b = np.cross(a, d)
    for i, s in enumerate((-1, 1)):
        o = c + a * s * size * 0.55
        P = [o + a * w * size * 0.5 + b * h * size * 0.75 + d * z * size
             for w in (-1, 1) for h in (-1, 1) for z in (-0.5, 0.35)]
        P.append(o + d * size * 1.0 + a * s * size * 0.1)
        k.add(f"{name}{i}", hull(P), BLACK, bone, fuse=False, surface="shell")


def miltank(src, out):
    k = Kit(src, coat="fur")
    H = np.array([0, -0.06, 0.61])              # the head's middle (its bone is at 0, -0.02, 0.5)
    HR = np.array([0.152, 0.135, 0.138])
    B = np.array([0, 0.005, 0.315])             # the body's middle

    # The round heavy body, the belly hanging low
    def pear(V):
        V = V.copy()
        s = 1 + 0.08 * np.clip(-V[:, 2], 0, 1) - 0.1 * np.clip(V[:, 2], 0, 1)
        V[:, 0] *= s; V[:, 1] *= s
        return V
    BODY = ell(B, (0.215, 0.195, 0.27), deform=pear)
    k.add("body", BODY, PINK, "body")

    # The head: big and round, the snout standing forward
    HEAD = ell(H, HR, deform=boxy(2.2))
    k.add("head", HEAD, PINK, "head")
    SNOUT = ell(H + (0, -0.11, -0.05), (0.095, 0.07, 0.07))
    k.add("snout", SNOUT, PINK, "head")

    # Black ears out to the sides, a little up and back, cream inside
    k.add("ear", ell((0.205, 0.005, 0.69), (0.088, 0.026, 0.044), rot=(0, -18, -12)), BLACK, "ear{s}", mirror=True)
    k.decal((0.225, -0.025, 0.698), unit(0.1, -1, 0.15), (0.05, 0.023), LINING, eps=0.003, rings=10, segs=48,
            mirror=True, up=(0.3, 0, 1))

    # Two small cream horns over the crown
    for s in (1, -1):
        base = np.array([0.055 * s, -0.02, 0.73])
        d = unit(0.35 * s, 0.05, 1)
        a = np.array([1.0, 0, 0]); b = np.array([0, 1.0, 0])
        P = [base + a * x * 0.022 + b * y * 0.02 for x in (-1, 1) for y in (-1, 1)]
        P += [base - d * 0.02 + a * x * 0.02 + b * y * 0.018 for x in (-1, 1) for y in (-1, 1)]
        P.append(base + d * 0.07)
        k.add(f"horn{'R' if s > 0 else 'L'}", hull(P), HORN, "head", fuse=False, surface="shell")

    # Short arms carried forward and a little down, black cloven hooves
    k.add("arm", tube([(0.15, -0.0, 0.46), (0.215, -0.06, 0.41), (0.245, -0.11, 0.37)], [0.068, 0.06, 0.054]),
          PINK, "arm{s}", mirror=True)
    for s, bone in ((1, "armR"), (-1, "armL")):
        hoof(k, "handhoof" + bone[-1], (0.255 * s, -0.15, 0.355), (0.15 * s, -0.8, -0.5), 0.037, bone)

    # Short legs, the thighs fused into the body, black cloven hooves turned forward
    for s, bone in ((1, "legR"), (-1, "legL")):
        m = np.array([s, 1, 1.0])
        k.add("thigh" + bone[-1], ell(np.array((0.12, -0.02, 0.13)) * m, (0.095, 0.1, 0.1)), PINK, bone)
        k.add("shin" + bone[-1], tube([np.array(p) * m for p in [(0.13, -0.04, 0.09), (0.135, -0.07, 0.045)]],
                                      [0.066, 0.058]), PINK, bone)
        hoof(k, "foothoof" + bone[-1], np.array((0.135, -0.11, 0.03)) * m, (0.1 * s, -1, -0.15), 0.048, bone)

    # The long thin tail, out behind to +X and down, ending in a black ball
    TP = [(0, 0.17, 0.2), (0.05, 0.27, 0.12), (0.14, 0.34, 0.075), (0.23, 0.35, 0.085)]
    k.add("tail", tube(TP, [0.018, 0.016, 0.015, 0.014]), TAIL, "tail", fuse=False)
    k.add("tailball", ell((0.255, 0.355, 0.095), (0.045, 0.045, 0.045), sub=3), BLACK, "tail", fuse=False)

    # The hood: black over the crown and the back of the head, down in a point between the eyes; the face pink
    FRONT = lambda P: P[:, 1] < H[1] + 0.02
    z0 = H[2]
    k.paint(lambda P: inside(z0 - 0.17 - P[:, 2], 0.003) * 1.0, BLACK, on=["head"])
    FACE = t.mirrored([(0.0, z0 + 0.03), (0.03, z0 + 0.075), (0.075, z0 + 0.09), (0.12, z0 + 0.072), (0.155, z0 + 0.02),
                       (0.172, z0 - 0.06), (0.15, z0 - 0.15), (0.0, z0 - 0.2)])
    k.paint(lambda P: t.poly_mask(*t.round_about(P, H, HR[0]), FACE, edge=0.0025) * FRONT(P), PINK,
            on=["head", "snout"])
    k.paint(lambda P: 1.0 + 0 * P[:, 0], PINK, on=["snout"])
    # The collar: black round the top of the body under the head
    k.paint(lambda P: inside(0.5 - P[:, 2], 0.003), BLACK, on=["body"])

    # The great udder over the lower front, its four pink teats
    def udder(P):
        s, z = t.round_about(P, B, 0.2)
        return inside(np.hypot(s / 0.165, (z - 0.25) / 0.19) - 1, 0.0025) * (P[:, 1] < B[1] - 0.04)
    k.paint(udder, UDDER)
    for i, (x, z) in enumerate(((0.06, 0.31), (-0.06, 0.31), (0.045, 0.2), (-0.045, 0.2))):
        p = t.hit(BODY, (x, -1, z), (0, 1, 0))
        n = unit(x * 2.5, -1, (z - 0.25) * 1.5)
        k.add(f"teat{i}", ell(p + n * 0.012, (0.016, 0.016, 0.026), rot=(-70, 0, 0), sub=3), TEAT, "body",
              fuse=False)

    # Black patches on the back and the hip
    for c, r in (((0.08, 0.2, 0.45), (0.1, 0.06, 0.075)), ((-0.14, 0.14, 0.2), (0.07, 0.08, 0.08))):
        c, r = np.array(c), np.array(r)
        k.paint(lambda P, c=c, r=r: inside(np.linalg.norm((P - c) / r, axis=1) - 1, 0.03) * (P[:, 2] < 0.5),
                BLACK, on=["body"])

    # The black nose on the snout's front; the wide open smiling mouth under it, dark red with a pink tongue
    NC = t.hit(SNOUT, (0, -1, z0 - 0.03), (0, 1, 0))
    k.decal(NC, unit(0, -1, 0.25), (0.034, 0.02), srgb(BLACK), eps=0.004, rings=10, segs=48)
    CM, CT = srgb(MOUTH), srgb(TONGUE)
    def mouth(u, v):
        c = np.tile(CM, (len(u), 1))
        c[(u / 0.6) ** 2 + ((v + 0.62) / 0.5) ** 2 < 1] = CT
        return c
    lips = polygon([(-1.0, 0.55), (1.0, 0.55), (0.92, 0.0), (0.66, -0.6), (0.0, -0.95), (-0.66, -0.6), (-0.92, 0.0)])   # a smile: flat on top, round below
    MC = t.hit(SNOUT, (0, -1, z0 - 0.08), (0, 1, 0))
    k.decal(MC, unit(0, -1, -0.25), (0.066, 0.045), mouth, eps=0.004, rings=12, segs=56, outline=lips,
            up=(0, -0.25, 1))

    # Big blue eyes under the hood's edge: white, a large blue iris, a black pupil, a white glint
    CW, CB, CP, CPK = srgb(WHITE), srgb(BLUE), srgb(PUPIL), srgb(PINK)
    def open_eye(u, v):
        c = np.tile(CW, (len(u), 1))
        c[((u + 0.12) / 0.66) ** 2 + ((v - 0.0) / 0.8) ** 2 < 1] = CB
        c[((u + 0.18) / 0.32) ** 2 + ((v + 0.05) / 0.42) ** 2 < 1] = CP
        c[((u - 0.1) / 0.2) ** 2 + ((v - 0.35) / 0.2) ** 2 < 1] = CW
        c[u * u + v * v > 0.86] = CP
        return c
    de = unit(0.42, -0.88, 0.12)
    E = t.hit(HEAD, (0.07, -1, z0 + 0.025), (0, 1, 0))
    def lay(state):
        k.decal(E, de, (0.031, 0.039), eye_look(state, open_eye, CPK, CP, line=0.18), eps=0.004, rings=20, segs=56,
                mirror=True)

    k.build(voxel=0.007, band=0.005)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    miltank(*args())
