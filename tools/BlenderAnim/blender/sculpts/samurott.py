# Samurott sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/samurott.py -- <the export's Samurott.glb> <out.glb>
#
# Grown from oshawott.py (sibling("oshawott")): its eyes as patches, its nose standing proud, its shell creams for the
# helmet and the armour. From the references: a great blue sea lion on four splayed legs, its chest lifted and its
# belly low; a pale blue-grey patch down the chest and belly, its top a V; a thick neck carrying the head forward; the
# head under a cream shell helmet ridged into three bands back over the nape, two spikes standing on its top and a long
# horn thrusting forward and up from the brow with a small prong at its root; small red eyes with a black pupil under the
# helmet's rim; an orange-red nose at the snout's tip; the mouth open, red inside; a long white moustache sweeping back
# and out from the snout's sides, and a white beard under the chin; cream armour of pointed plates stacked down the
# outside of each foreleg; black claws; the tail raised behind, ending in a fin of two lobes, pale inside, a small cream
# spike on its back.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, hull, srgb, inside, smoothstep, unit, eye_look, args, sibling

o = sibling("oshawott")
BLUE, PALE, CREAM, RIDGE, WHITE, NOSE = "#3D68B4", "#A9BBD6", "#E8D69A", "#C2A964", "#F4F6FA", "#E2603A"
CLAW, MOUTH, RED, INK = "#26252C", "#B8303C", "#E0462C", o.INK


def eye(u, v):
    """A small red eye with a round black pupil toward the front and a white glint (u outward, v up)."""
    c = np.tile(srgb(RED), (len(u), 1))
    c[((u + 0.12) / 0.42) ** 2 + ((v + 0.02) / 0.55) ** 2 < 1] = srgb(INK)
    c[((u - 0.05) / 0.2) ** 2 + ((v - 0.35) / 0.2) ** 2 < 1] = (1.0, 1.0, 1.0)
    c[u * u + v * v > 0.8] = srgb(INK)
    edge = 0.45 + 0.4 * u                 # the helmet's rim cuts it, lower toward the snout
    c[v > edge] = srgb(BLUE)
    return c


def plate(k, name, c, d, out_dir, r, length, spike, bone, color=CREAM):
    """A pointed armour plate wrapped round the outer side of a limb (axis d through c, radius r): a flat-faced hull."""
    d = unit(*d); w = np.array(out_dir, float); w = w - d * (w @ d); w /= np.linalg.norm(w)
    s = np.cross(d, w)
    pts = []
    for a in np.linspace(-70, 70, 5):
        t = math.radians(a)
        rad = w * math.cos(t) + s * math.sin(t)
        for along in (-length / 2, length / 2):
            for rr in (r * 0.8, r + 0.014):
                pts.append(np.array(c) + d * along + rad * rr)
    # the point: out and up the limb
    pts.append(np.array(c) + w * (r + spike) + d * (length * 0.5 + spike * 0.6))
    k.add(name, hull(pts), color, bone, fuse=False, surface="shell")


def claws(k, foot, fwd, bone, n=3):
    fwd = unit(*fwd); side = unit(*np.cross(fwd, (0, 0, 1)))
    for i in range(n):
        off = (i - (n - 1) / 2) * 0.026
        root = np.array(foot) + fwd * 0.03 + side * off
        tip = root + fwd * 0.03 + np.array([0, 0, -0.012])
        pts = [root + side * 0.009 + (0, 0, 0.012), root - side * 0.009 + (0, 0, 0.012), root + side * 0.009 - (0, 0, 0.006),
               root - side * 0.009 - (0, 0, 0.006), tip + (0, 0, -0.008)]
        k.add(f"claw{bone}{i}", hull(pts), CLAW, bone, fuse=False, surface="shell")


def samurott(src, out):
    k = Kit(src, coat="fur")
    # The body: low behind, the chest lifted in front, a thick neck carrying the head forward
    k.add("body", ell((0, 0.04, 0.235), (0.135, 0.2, 0.125)), BLUE, "body")
    k.add("chest", ell((0, -0.1, 0.31), (0.125, 0.12, 0.14)), BLUE, "body")
    k.add("neck", tube([(0, -0.12, 0.36), (0, -0.17, 0.42), (0, -0.2, 0.47)], [0.1, 0.085, 0.075]), BLUE, "head")
    J, G = np.array([0, -0.17, 0.4]), 1.15
    def g(p):
        return J + (np.asarray(p, float) - J) * G
    H = np.array([0, -0.215, 0.5])
    HR = np.array([0.078, 0.085, 0.07])
    k.add("head", ell(H, HR), BLUE, "head")
    k.add("snout", ell(H + (0, -0.075, -0.03), (0.05, 0.065, 0.045), rot=(12, 0, 0)), BLUE, "head")
    k.add("jaw", ell(H + (0, -0.06, -0.07), (0.045, 0.055, 0.03), rot=(-10, 0, 0)), BLUE, "head")

    # The helmet: the skull's top and back, three cream bands sloping back over the nape
    segs = [((0, -0.235, 0.545), (0.085, 0.075, 0.055)), ((0, -0.17, 0.565), (0.092, 0.075, 0.065)),
            ((0, -0.1, 0.545), (0.088, 0.075, 0.065)), ((0, -0.04, 0.5), (0.078, 0.065, 0.06))]
    for i, (c, r) in enumerate(segs):
        k.add(f"helm{i}", ell(c, r, rot=(-10 * i, 0, 0)), CREAM, "head", surface="shell")
    # the ridges between the bands, darker
    for y in (-0.205, -0.135, -0.07):
        k.paint(lambda P, y=y: (lambda Q: inside(np.abs(Q[:, 1] - y + 0.3 * (Q[:, 2] - 0.56)) - 0.005, 0.002) * (Q[:, 2] > 0.48))(J + (P - J) / G),
                RIDGE, on=["helm"])
    # the horn: a long solid point from the brow forward and up, with a small prong at its root
    hr = np.array([0, -0.27, 0.56])
    tip = np.array([0, -0.52, 0.66])
    k.add("horn", hull([hr + (0.03, 0.02, -0.02), hr + (-0.03, 0.02, -0.02), hr + (0.025, 0.03, 0.04),
                        hr + (-0.025, 0.03, 0.04), hr + (0, -0.02, 0.03), tip]), CREAM, "head", fuse=False, surface="shell")
    pr = hr + (0, -0.06, 0.035)
    k.add("prong", hull([pr + (0.012, 0.015, -0.01), pr + (-0.012, 0.015, -0.01), pr + (0, -0.015, -0.01),
                         pr + (0, 0.02, 0.055)]), CREAM, "head", fuse=False, surface="shell")
    # two spikes on the top, leaning back
    for c, h in (((0, -0.15, 0.6), 0.09), ((0, -0.075, 0.585), 0.08)):
        c = np.array(c)
        k.add(f"spike{c[1]:.2f}", hull([c + (0.022, 0.02, -0.02), c + (-0.022, 0.02, -0.02), c + (0.018, -0.022, -0.02),
                                        c + (-0.018, -0.022, -0.02), c + (0, 0.035, h)]), CREAM, "head", fuse=False,
              surface="shell")

    # The moustache: long white blades from the snout's sides, sweeping back and out and rising at the ends
    k.add("tache", blade([(0.035, -0.29, 0.475), (0.085, -0.25, 0.455), (0.15, -0.17, 0.45), (0.2, -0.08, 0.46),
                          (0.23, -0.0, 0.48)], [0.026, 0.042, 0.042, 0.03, 0.008], (0, 0.25, 1), 0.45), WHITE, "head",
          fuse=False, mirror=True, surface="fur")
    # the beard under the chin
    k.add("beard", blade([(0, -0.25, 0.44), (0, -0.25, 0.4), (0, -0.235, 0.355)], [0.034, 0.028, 0.006], (0, 1, 0), 0.6),
          WHITE, "head", fuse=False, surface="fur")
    for sx in (1, -1):
        k.add(f"beard{'RL'[sx < 0]}", blade([(0.03 * sx, -0.24, 0.44), (0.045 * sx, -0.22, 0.39), (0.05 * sx, -0.2, 0.35)],
                                           [0.026, 0.02, 0.005], (1, 0.3, 0), 0.6), WHITE, "head", fuse=False,
              surface="fur")
    # the nose at the snout's tip
    nd = unit(0, -1, 0.15)
    o.nose(k, H + (0, -0.135, -0.018), nd, (0.018, 0.012, 0.014), NOSE, "head")

    # Forelegs splayed out and forward, thick at the shoulder, armour down their outer side
    k.add("shoulder", ell((0.1, -0.13, 0.24), (0.085, 0.09, 0.11)), BLUE, "leg{s}", mirror=True)
    FL = [(0.11, -0.15, 0.22), (0.165, -0.235, 0.11), (0.195, -0.28, 0.035)]
    k.add("foreleg", tube(FL, [0.07, 0.054, 0.044]), BLUE, "leg{s}", mirror=True)
    k.add("forepaw", ell((0.2, -0.3, 0.025), (0.042, 0.05, 0.025), rot=(0, 0, -30)), BLUE, "leg{s}", mirror=True)
    # Hind legs: haunches fused into the rump, shins out and back
    k.add("haunch", ell((0.1, 0.14, 0.21), (0.09, 0.105, 0.11)), BLUE, "hind{s}", mirror=True)
    HL = [(0.12, 0.16, 0.18), (0.165, 0.235, 0.085), (0.185, 0.265, 0.03)]
    k.add("hindleg", tube(HL, [0.068, 0.053, 0.044]), BLUE, "hind{s}", mirror=True)
    k.add("hindpaw", ell((0.19, 0.25, 0.025), (0.042, 0.05, 0.025), rot=(0, 0, 30)), BLUE, "hind{s}", mirror=True)

    for sx in (1, -1):
        side = "RL"[sx < 0]
        m = np.array([sx, 1, 1])
        a, b = np.array(FL[0]) * m, np.array(FL[2]) * m
        d = unit(*(a - b))                     # up the leg
        for i, t in enumerate((0.25, 0.48, 0.7)):
            c = b + (a - b) * t
            r = 0.05 + 0.03 * t
            plate(k, f"armour{side}{i}", c, d, (sx, 0.15, 0.25), r, 0.06, 0.04, f"leg{side}")
        claws(k, np.array((0.2, -0.3, 0.02)) * m, (0.4 * sx, -1, 0), f"leg{side}")
        claws(k, np.array((0.19, 0.25, 0.02)) * m, (0.3 * sx, -1, 0), f"hind{side}")

    # The tail raised behind, a fin of two lobes at its end, turned out to +X so both cameras see its faces
    TT = np.array([0.04, 0.4, 0.46])
    k.add("tail", tube([(0, 0.18, 0.3), (0, 0.3, 0.37), (0.02, 0.37, 0.43), TT], [0.075, 0.055, 0.042, 0.034]), BLUE,
          "tail")
    fd = unit(0.55, 0.8, 0.25)
    s, n, _ = o.frame(fd, roll=-30)
    for j, lobe in enumerate((unit(*(fd + n * 0.75)), unit(*(fd - n * 0.6)))):
        c = TT + lobe * 0.075
        V, F = ell((0, 0, 0), (0.016, 0.085, 0.04))
        ls = np.cross(lobe, s); ls /= np.linalg.norm(ls)
        V = V @ np.stack([s, lobe, ls]) + c
        k.add(f"fin{j}", (V, F), BLUE, "tail")
    k.paint(lambda P: inside(np.linalg.norm(P - (TT + fd * 0.1), axis=1) - 0.06, 0.004) * (
        ((P - TT) @ s) < 0.004), PALE, on=["fin"])
    sp = np.array([0.0, 0.31, 0.42])
    k.add("tailspike", hull([sp + (0.015, -0.02, -0.01), sp + (-0.015, -0.02, -0.01), sp + (0, 0.025, -0.01),
                             sp + (0, 0.02, 0.06)]), CREAM, "tail", fuse=False, surface="shell")

    # The pale patch down the chest and belly, its top a V
    def belly(P):
        top = 0.37 - 0.9 * np.abs(P[:, 0])
        front = smoothstep(-0.02, -0.08, P[:, 1] + 0.25 * (P[:, 2] - 0.2))
        return inside(P[:, 2] - top, 0.004) * inside(np.abs(P[:, 0]) - 0.085, 0.004) * front * (P[:, 2] > 0.12)
    k.paint(belly, PALE, on=["chest", "body"])
    # the mouth: open under the nose, red inside
    INKC, BL = srgb(MOUTH), srgb(BLUE)
    def mouth(u, v):
        c = np.tile(BL, (len(u), 1))
        c[(v < 0.5 - 0.3 * u * u) & (u * u + v * v < 0.9)] = INKC
        return c
    md = unit(0, -0.75, -0.65)
    k.decal(g(H + (0, -0.105, -0.06)), md, (0.033, 0.024), mouth, eps=0.003, rings=16, segs=56)

    # the head and all it wears grown a tenth and more about the neck, as the game wants a face to read
    for p in k.parts:
        if p["bone"] == "head" and p["name"] != "neck":
            p["V"] = J + (np.asarray(p["V"]) - J) * G
    k.build(voxel=0.006, smooth=4, band=0.004)
    E = [(g(o.at_surface(H, HR, d)), d, sx) for sx in (1, -1) for d in [unit(0.62 * sx, -0.76, 0.12)]]
    k.eye_states(lambda st: o.eyes(k, E, (0.02, 0.015), st, BLUE, open_look=eye))
    k.export(out)


if __name__ == "__main__":
    samurott(*args())
