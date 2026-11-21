# Perrserker sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), its coin
# made by Meowth's own function (meowth.py, imported as sibling("meowth")):
#   blender --background --factory-startup --python sculpts/perrserker.py -- <the export's Perrserker.glb> <out.glb>
#
# The design, from the references: a stocky tan cat on two short bent legs, almost all head and beard. The head is a
# dark slate helmet of hardened hair coming down to the mouth, two dark curved horns out of its sides rising out and up
# with pale tips, small dark spikes round its sides, a pale grey coin with dark ridges on its brow a little to the
# right (+X); half-lidded orange eyes with dark slit pupils set in the helmet, their tops cut flat by it. A huge open
# mouth, dark red inside with a pink tongue, a row of white triangle teeth above and a few below, framed by a great
# shaggy pale grey beard from the cheeks down over the chest to the belly, jagged all round: the largest mass. Tan arms
# with dark shaggy cuffs at the wrists and tan paws, carried forward and a little down. The tail comes out from behind
# the hips on the left (-X, the viewer's left in the art), shaggy at its root, round the side and up beside the beard:
# a dark shaggy ring, a tan knob with a dark spot and a long dark blade for its tip (the pictures show four dark
# cuffs, two at the ankles and one on the right wrist; the fourth is this ring, as the game's own sculptor has the
# blade on the tail). Tan shaggy fur at the hips behind, dark shaggy cuffs round the ankles, tan feet.
# Reference ratios (3D picture): head with helmet and beard 0.62 of the height, beard 0.6 of the height across, horn
# tips 0.66 of the height apart, legs 0.23 of the height, blade 0.3 of the height.
# The export's joints: body (0, 0, 0.3), arms (+-0.13, 0, 0.35), head (0, -0.04, 0.44), legs (+-0.07, 0, 0.18),
# tail (0, 0.1, 0.25).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, blade, hull, srgb, inside, unit, eye_look, args, outward, polygon, lumpy,
                       sibling, axes)

mw = sibling("meowth")

TAN, DARK, BEARD = "#BCA88E", "#3A4543", "#CACDD2"
HORNTIP, COIN, RIDGE = "#CDD0D2", "#E6E7EA", "#767A80"
EYE, PUPIL, MOUTH, TONGUE, TOOTH = "#F29A1E", "#1E1A18", "#8E2234", "#E27A8C", "#FFFFFF"
PAD = "#2A2A2E"

HC = np.array([0, -0.04, 0.56])               # the helmeted head's middle
HR = np.array([0.178, 0.138, 0.128])
BC = np.array([0, -0.035, 0.38])              # the beard's middle
BR = np.array([0.215, 0.155, 0.16])


def on_ell(c, r, x, z, lift=0.0, front=True):
    """The front point of an ellipsoid (centre c, radii r) at (x, z), lifted along its normal, and the normal."""
    q = max(0.0, 1 - ((x - c[0]) / r[0]) ** 2 - ((z - c[2]) / r[2]) ** 2)
    p = np.array([x, c[1] - r[1] * math.sqrt(q), z])
    n = (p - c) / np.array(r) ** 2; n /= np.linalg.norm(n)
    return p + n * lift, n


def add_head(k):
    k.add("head", ell(HC, HR, deform=lambda V: V * (1 + 0.05 * np.clip(-V[:, 2], 0, 1))[:, None]), DARK, "head")
    # the horns out of the helmet's sides, rising out and up, pale at the tips
    pts = [(0.13, 0.0, 0.6), (0.195, 0.012, 0.655), (0.228, 0.02, 0.715), (0.226, 0.022, 0.77)]
    k.add("horn", blade(pts, [0.05, 0.038, 0.022, 0.008], (0, 1, 0), 0.65), DARK, "head", mirror=True)
    k.paint(lambda P: inside(0.712 - P[:, 2], 0.0025) * (np.abs(P[:, 0]) > 0.18), HORNTIP, on=["horn"])
    # small dark spikes round the helmet's sides
    for i, (az, z, L) in enumerate(((52, 0.625, 0.04), (80, 0.56, 0.035))):
        a = math.radians(az)
        d = unit(math.sin(a), -math.cos(a) * 0.6, 0.35)
        root = HC + np.array([HR[0] * math.sin(a) * 0.92, -HR[1] * math.cos(a) * 0.92, z - HC[2]])
        k.add(f"spike{i}", hull([root + (0.016, 0, 0), root - (0.016, 0, 0), root + (0, 0.016, 0), root - (0, 0.016, 0),
                                 root + (0, 0, 0.016), root + d * L]), DARK, "head", fuse=False, mirror=True,
              surface="hair")
    k.add("crest", hull([HC + (-0.016, 0.02, 0.105), HC + (0.016, 0.02, 0.105), HC + (0, 0.045, 0.1),
                         HC + (0, -0.0, 0.1), HC + (0, 0.04, 0.16)]), DARK, "head", fuse=False, surface="hair")
    # the coin on the brow, a little to the right
    c, n = on_ell(HC, HR, 0.045, 0.64, 0.012)
    up = np.array([0, 0, 1.0]) - n * n[2]; up /= np.linalg.norm(up)
    side = np.cross(up, n)
    half, thick = (0.036, 0.05), 0.022
    k.add("coin", mw.coin(c, n, up, half=half, thick=thick), COIN, "head", fuse=False, surface="metal")
    for i, s in enumerate((0.45, 0.0, -0.45)):
        w = half[0] * 0.86 * math.sqrt(1 - s * s) * 0.85
        p = c + n * (thick * 0.5 + 0.003) + up * s * half[1]
        k.add(f"ridge{i}", hull([p + side * sx * w + up * sz * 0.0075 + n * sn * 0.004
                                 for sx in (-1, 1) for sz in (-1, 1) for sn in (-1, 1)]), RIDGE, "head", fuse=False,
              surface="metal")


def add_beard(k):
    k.add("beard", ell(BC, BR, deform=lumpy(7, 4, 0.05)), BEARD, "head")
    k.add("cheek", ell((0.165, -0.07, 0.465), (0.085, 0.08, 0.07), deform=lumpy(5, 3, 0.06)), BEARD, "head",
          mirror=True)
    k.add("mouthplate", ell((0, -0.12, 0.445), (0.12, 0.07, 0.075)), BEARD, "head")
    # jagged tufts round the beard's edge, out and down: few, broad at the root, of uneven length
    for i, (az, L, r) in enumerate(((-105, 0.07, 0.05), (-68, 0.1, 0.056), (-30, 0.06, 0.05), (4, 0.095, 0.058),
                                    (36, 0.065, 0.05), (72, 0.1, 0.055), (108, 0.06, 0.048))):
        a = math.radians(az)
        d = unit(math.sin(a), -math.cos(a) * 0.5, -0.55 - 0.35 * math.cos(a))
        root = BC + np.array([BR[0] * 0.8 * math.sin(a), -BR[1] * 0.65 * math.cos(a), -0.06 - 0.06 * math.cos(a)])
        k.add(f"tuft{i}", blade([root, root + d * L * 0.45, root + d * L], [r, r * 0.62, 0.008], (0, 1, 0), 0.55),
              BEARD, "head")
    for s in (1, -1):
        for j, z in enumerate((0.5, 0.43)):
            root = np.array([0.2 * s, -0.04, z])
            d = unit(s, 0.1, -0.35 - 0.3 * j)
            k.add(f"sidetuft{j}{'R' if s > 0 else 'L'}",
                  blade([root, root + d * 0.045, root + d * 0.085], [0.038, 0.026, 0.007], (0, 1, 0), 0.6), BEARD,
                  "head")


def add_mouth(k):
    M, n = on_ell((0, -0.12, 0.445), (0.12, 0.07, 0.075), 0.0, 0.448)
    CM, CT = srgb(MOUTH), srgb(TONGUE)
    def mouth(u, v):
        c = np.tile(CM, (len(u), 1))
        c[((u - 0.05) / 0.55) ** 2 + ((v + 0.8) / 0.45) ** 2 < 1] = CT
        return c
    shape = polygon([(-1, 0.75), (-0.5, 0.95), (0.5, 0.95), (1, 0.75), (0.8, -0.3), (0.35, -0.85), (-0.35, -0.85),
                     (-0.8, -0.3)])
    k.decal(M, n, (0.088, 0.058), mouth, eps=0.003, rings=14, segs=64, outline=shape)
    # the teeth, each a patch of its own: five above pointing down, three below pointing up
    tri_dn = polygon([(-1, 0.7), (1, 0.7), (0, -1)])
    tri_up = polygon([(-1, -0.7), (1, -0.7), (0, 1)])
    for x, w in ((-0.06, 0.015), (-0.03, 0.016), (0.0, 0.017), (0.03, 0.016), (0.06, 0.015)):
        p, nn = on_ell((0, -0.12, 0.445), (0.12, 0.07, 0.075), x, 0.474 - 0.15 * x * x)
        k.decal(p, nn, (w, 0.02), TOOTH, eps=0.0045, rings=6, segs=24, outline=tri_dn)
    for x in (-0.035, 0.0, 0.035):
        p, nn = on_ell((0, -0.12, 0.445), (0.12, 0.07, 0.075), x, 0.41)
        k.decal(p, nn, (0.012, 0.015), TOOTH, eps=0.0055, rings=6, segs=24, outline=tri_up)


def add_body(k):
    k.add("body", ell((0, 0.02, 0.255), (0.15, 0.13, 0.13)), TAN, "body")
    k.add("hips", ell((0, 0.03, 0.17), (0.13, 0.11, 0.08)), TAN, "body")
    # shaggy tan fur round the hips behind
    for i, a in enumerate(np.radians((-110, -70, 70, 110, 150, 210))):
        d = unit(math.sin(a), -math.cos(a), -0.25)
        root = np.array([0.12 * math.sin(a), 0.03 - 0.1 * math.cos(a), 0.22])
        k.add(f"shag{i}", blade([root, root + d * 0.04, root + d * 0.075], [0.035, 0.024, 0.006], (0, 0, 1), 0.6), TAN,
              "body")


def cuff(k, name, c, axis, r, bone):
    """A dark shaggy cuff round a wrist or an ankle."""
    a, b = axes(axis)
    V, F = ell((0, 0, 0), (r, r, r * 0.6), deform=lumpy(7, 2, 0.16))
    k.add(name, (V @ np.stack([a, b, unit(*axis)]) + np.array(c), F), DARK, bone)


def add_legs(k):
    for s, bone in ((1, "legR"), (-1, "legL")):
        m = np.array([s, 1, 1.0])
        k.add("thigh" + bone[-1], ell(np.array((0.085, 0.0, 0.15)) * m, (0.072, 0.078, 0.08)), TAN, bone)
        k.add("shin" + bone[-1], tube([np.array(p) * m for p in ((0.09, -0.005, 0.12), (0.1, -0.012, 0.055))],
                                      [0.052, 0.046]), TAN, bone)
        cuff(k, "anklecuff" + bone[-1], np.array((0.1, -0.012, 0.07)) * m, (0, 0, 1), 0.066, bone)
        k.add("foot" + bone[-1], ell(np.array((0.105, -0.05, 0.026)) * m, (0.055, 0.075, 0.028)), TAN, bone)


def add_arms(k):
    # both arms forward and a little down, the paws before the beard's sides
    k.add("arm", tube([(0.12, 0.0, 0.36), (0.19, -0.03, 0.3), (0.205, -0.11, 0.265)], [0.048, 0.043, 0.04]), TAN,
          "arm{s}", mirror=True)
    for s, bone in ((1, "armR"), (-1, "armL")):
        cuff(k, "wristcuff" + bone[-1], (0.205 * s, -0.115, 0.265), (0.1 * s, -1, -0.1), 0.058, bone)
    k.add("paw", ell((0.205, -0.165, 0.258), (0.045, 0.045, 0.042)), TAN, "arm{s}", mirror=True)


def add_tail(k):
    """The tail out from behind the hips on the left, shaggy at its root, round the side and up beside the beard: a
    dark shaggy ring, a tan knob with a dark spot, and its tip a long dark blade pointing up and out, its faces to the
    front."""
    pts = [(0, 0.1, 0.235), (-0.09, 0.17, 0.245), (-0.2, 0.16, 0.29), (-0.27, 0.08, 0.36), (-0.285, 0.02, 0.405)]
    k.add("tail", tube(pts, [0.042, 0.04, 0.037, 0.034, 0.032]), TAN, "tail")
    for i, (at, d) in enumerate((((-0.07, 0.17, 0.25), (-0.1, 0.6, -0.5)), ((-0.14, 0.18, 0.26), (0.1, 0.8, 0.2)),
                                 ((-0.21, 0.15, 0.3), (-0.6, 0.6, -0.3)), ((-0.12, 0.17, 0.27), (0.0, 0.3, 1)))):
        root, d = np.array(at), unit(*d)
        k.add(f"tailshag{i}", blade([root, root + d * 0.035, root + d * 0.065], [0.03, 0.02, 0.006], (0, 0, 1), 0.6),
              TAN, "tail")
    cuff(k, "tailcuff", (-0.285, 0.02, 0.41), (-0.2, -0.45, 1), 0.052, "tail")
    P = np.array((-0.295, 0.0, 0.455))
    k.add("tailknob", ell(P, (0.044, 0.042, 0.046)), TAN, "tail")
    k.add("tailspot", outward(ell(P + (0.0, -0.041, 0.004), (0.013, 0.008, 0.013))), PAD, "tail", fuse=False,
          surface="skin")
    d = unit(-0.4, -0.12, 1)
    side = unit(*np.cross(d, (0, 1.0, 0)))
    fwd = unit(*np.cross(side, d))
    base = P + d * 0.03
    L = 0.22
    pts = []
    for t, w in ((0.0, 0.022), (0.25, 0.036), (0.55, 0.038), (0.85, 0.022), (1.0, 0.0)):
        c = base + d * L * t
        th = 0.013 if w else 0.0
        pts += [c + side * w, c - side * w, c + fwd * th, c - fwd * th]
    k.add("blade", hull(pts), DARK, "tail", fuse=False, surface="metal")
    # one dark ridge down its front face
    a = base + d * L * 0.12 - fwd * 0.0125
    b = base + d * L * 0.82 - fwd * 0.011
    k.add("bladeline", hull([a + side * 0.011, a - side * 0.011, b + side * 0.009, b - side * 0.009,
                             a - fwd * 0.005, b - fwd * 0.005]), PAD, "tail", fuse=False, surface="metal")


def lay_eyes(k):
    CE, CP, CD, CL = srgb(EYE), srgb(PUPIL), srgb(DARK), srgb("#B8BCBE")
    def eye(u, v):
        c = np.tile(CE, (len(u), 1))
        c[np.abs(u + 0.05) < 0.16 * np.sqrt(np.clip(1 - ((v + 0.1) / 0.85) ** 2, 0, 1))] = CP
        c[np.hypot(u, v) > 0.88] = CP
        return c
    flat_top = lambda th: min(1.0, 0.42 / math.sin(th)) if math.sin(th) > 0.05 else 1.0
    p, n = on_ell(HC, HR, 0.078, 0.548)
    def eyes(state):
        if state == "Open":
            k.decal(p, n, (0.042, 0.034), eye, eps=0.003, rings=20, segs=56, mirror=True, outline=flat_top)
        elif state == "Fierce":
            # the lid slants down to the inner corner: an outline that is not symmetric, so each eye is laid by
            # hand (a mirrored patch's outline is not mirrored; the left decal's u runs inward)
            slant = lambda th: min(1.0, (0.15 + 0.3 * math.cos(th)) / math.sin(th)) if math.sin(th) > 0.05 else 1.0
            m = np.array([-1, 1, 1.0])
            k.decal(p, n, (0.042, 0.034), eye, eps=0.003, rings=20, segs=56, outline=lambda th: max(0.12, slant(th)))
            k.decal(p * m, n * m, (0.042, 0.034), lambda u, v: eye(-u, v), eps=0.003, rings=20, segs=56,
                    outline=lambda th: max(0.12, slant(math.pi - th)))
        else:
            k.decal(p, n, (0.042, 0.034), eye_look(state, eye, CD, CL, line=0.17), eps=0.003, rings=20, segs=56,
                    mirror=True)
    return eyes


def perrserker(src, out_path):
    k = Kit(src, coat="fur")
    add_head(k)
    add_beard(k)
    add_mouth(k)
    add_body(k)
    add_legs(k)
    add_arms(k)
    add_tail(k)
    k.build(voxel=0.006, band=0.004, wband=0.05)
    k.eye_states(lay_eyes(k))
    k.export(out_path)


if __name__ == "__main__":
    perrserker(*args())
