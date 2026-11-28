# Cerbanor (No. 10060, Carpathia, Grass/Psychic), sculpted from nothing on the skeleton and clips of the game's export:
#   blender --background --factory-startup --python sculpts/cerbanor.py -- <the export's Cerbanor.glb> <out.glb>
# Grown from cerbel.py (sibling("cerbel")): the same russet coat with cream under it and white spots over the back,
# the same leg with its hock and dark hoof, the same ear with its lining, the same dark eye in a pale patch, the
# same small leaves. From the picture (10060-Cerbanor.jpg) and the brief (CP-dex-2-mountains.md, 60): a tall red stag
# with a deep body; a great ruff of moss and ivy round the neck and over the chest, ivy trailing from it; the throat
# cream above the ruff; a long head, cream round the nose and under the jaw, a dark nose; calm half-shut eyes; ears
# out to the sides; huge branching antlers like boughs, dark brown, moss over their beams, small white flowers in
# the moss and ivy hanging from them; a faint ring of light between their tines; the legs mossy green from the knee
# down, dark hooves; a short tail, cream beneath.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, blade, lumpy, outward, srgb, inside, unit, eye_look, args, sibling)

cb = sibling("cerbel")
dl = sibling("deerling")              # the five-petalled flower
lv = sibling("larvitar")              # an eye's outline cut along a lid

RED, CREAM, LINING, PATCH = "#B4582E", "#F0DDBA", "#EFC8B4", "#E8CBA8"
HOOF, MOSSLEG, NOSE = "#3E302A", "#6E7A3E", "#2E2420"
ANTLER, MOSS, MOSS2, IVY = "#6A4E36", "#4F8C3A", "#73A84E", "#3C7334"
FLOWER, MIDDLE, HALO = "#FAFAF2", "#F0D25A", "#EEF4C8"


def join(*shapes):
    V, F, off = [], [], 0
    for sv, sf in shapes:
        V.append(sv); F.extend([[i + off for i in f] for f in sf]); off += len(sv)
    return np.vstack(V), F


def ring(c, radius, rad, n=48):
    """A closed ring (a halo) lying level about c: a tube round a circle, its ends overlapping behind."""
    c = np.array(c, float)
    ts = np.linspace(0.5 * math.pi - 0.2, 2.5 * math.pi + 0.2, n)
    pts = [c + radius * np.array([math.cos(t), math.sin(t), 0]) for t in ts]
    return outward(tube(pts, [rad] * n, seg=12, per=3))


def ivy_chain(k, name, top, drop, side, bone, n, color=IVY):
    """A strand of ivy hanging from top: a short dark stem and leaves one under another, turned out by turns."""
    top = np.array(top, float)
    pts = [top + np.array([0.008 * side * math.sin(2.4 * t), -0.01 * t, -drop * t]) for t in np.linspace(0, 1, 4)]
    k.add(name, outward(tube(pts, [0.014, 0.013, 0.012, 0.011], seg=10, per=4)), color, bone, fuse=False,
          surface="leaf")
    for i in range(n):
        t = (i + 0.6) / n
        q = top + np.array([0.008 * side * math.sin(2.4 * t), -0.01 * t, -drop * t])
        turn = 1 if i % 2 else -0.4
        cb.a_leaf(k, f"{name}leaf{i}", q, (0.75 * side * turn, -0.6, -0.55), 0.052, 0.025, bone,
                  color if i % 2 else MOSS2, thick=0.018)


def cerbanor(src, out):
    k = Kit(src, coat="fur")

    # --- the trunk: deep, lowered so the leg showing is short ----------------------------------------------------
    BC, BR = np.array([0, 0.01, 0.585]), np.array([0.158, 0.3, 0.168])
    k.add("barrel", ell(BC, BR), RED, "body")
    k.add("chest", ell((0, -0.17, 0.61), (0.152, 0.14, 0.17)), RED, "body")
    k.add("rump", ell((0, 0.19, 0.615), (0.158, 0.14, 0.16)), RED, "body")

    # --- legs: broad shoulders and thighs, the hind leg with its hock, mossy green below the knee ------------------
    k.add("shoulder", ell((0.082, -0.165, 0.5), (0.062, 0.088, 0.14)), RED, "leg{s}", mirror=True)
    k.add("thigh", ell((0.086, 0.18, 0.515), (0.076, 0.118, 0.155)), RED, "hind{s}", mirror=True)
    cb.leg(k, "fore", [(0.078, -0.17, 0.44), (0.078, -0.173, 0.27), (0.078, -0.165, 0.13), (0.078, -0.17, 0.044)],
           [0.05, 0.038, 0.033, 0.031], "leg{s}", RED, hoof=HOOF, fetlock=MOSSLEG, band=(0.04, 0.3))
    cb.leg(k, "hind", [(0.08, 0.18, 0.44), (0.08, 0.16, 0.34), (0.08, 0.226, 0.23), (0.08, 0.205, 0.044)],
           [0.055, 0.045, 0.036, 0.031], "hind{s}", RED, hoof=HOOF, fetlock=MOSSLEG, band=(0.04, 0.27))

    # --- a thick neck forward to a long head; a dark nose ------------------------------------------------------
    k.add("neck", tube([(0, -0.15, 0.66), (0, -0.24, 0.8), (0, -0.282, 0.885)], [0.1, 0.08, 0.072]), RED, "head")
    H, HR = np.array([0, -0.31, 0.94]), np.array([0.08, 0.084, 0.082])
    k.add("head", ell(H, HR), RED, "head")
    MC = np.array([0, -0.408, 0.903])
    k.add("muzzle", ell(MC, (0.048, 0.078, 0.048), rot=(14, 0, 0)), RED, "head")
    k.add("nose", ell((0, -0.482, 0.909), (0.022, 0.016, 0.018)), NOSE, "head", fuse=False, surface="skin")
    k.paint(lambda P: inside(np.linalg.norm(P - MC, axis=1) - 0.11, 0.004) * inside(P[:, 2] - 0.902, 0.003)
            * inside(P[:, 1] + 0.345, 0.004), CREAM)
    k.paint(lambda P: inside(np.linalg.norm(P - (0, -0.47, 0.91), axis=1) - 0.032, 0.003), CREAM)
    def throat(P):
        yc = -0.15 - 0.585 * (P[:, 2] - 0.66)
        return (inside(P[:, 1] - (yc - 0.05), 0.005) * inside(np.abs(P[:, 0]) - 0.06, 0.008)
                * inside(P[:, 2] - 0.9, 0.004) * inside(0.62 - P[:, 2], 0.004))
    k.paint(throat, CREAM, on=["neck", "head", "muzzle"])
    k.paint(lambda P: inside(P[:, 2] - 0.45, 0.005) * inside(P[:, 1] - 0.22, 0.005) * inside(-0.2 - P[:, 1], 0.005)
            * inside(np.abs(P[:, 0]) - 0.07, 0.008), CREAM, on=["barrel", "chest", "rump"])

    # --- the ruff: a great mass of moss round the neck and over the chest, ivy leaves standing out of it --------
    RC, RR = np.array([0, -0.255, 0.735]), np.array([0.17, 0.135, 0.15])
    k.add("ruff", ell(RC, RR, rot=(-28, 0, 0), deform=lumpy(6, 4, 0.16)), MOSS, "body")
    k.add("ruffchest", ell((0, -0.27, 0.655), (0.15, 0.1, 0.11), deform=lumpy(5, 3, 0.18)), MOSS, "body")
    rs = np.random.RandomState(60)
    for i in range(22):
        th = -0.5 * math.pi + rs.uniform(-1.5, 1.5)
        ph = rs.uniform(-0.75, 0.5)
        d = np.array([math.cos(th) * math.cos(ph), math.sin(th) * math.cos(ph), math.sin(ph)])
        p = cb.on_ell(RC, RR * 1.05, d)
        cb.a_leaf(k, f"ivy{i}", p - d * 0.01, d + np.array([0, 0, -0.35]), 0.06 + 0.015 * (i % 3), 0.026, "body",
                  IVY if i % 2 else MOSS2, thick=0.02)
    # trailing ivy below the ruff, on the near and far side
    for j, (x, y) in enumerate(((0.13, -0.31), (-0.14, -0.3), (0.16, -0.21), (-0.16, -0.2))):
        ivy_chain(k, f"trail{j}", (x, y, 0.66), 0.15, np.sign(x), "body", 4)

    # --- the tail: short, cream beneath ------------------------------------------------------------------------
    k.add("tail", blade([(0, 0.27, 0.66), (0.008, 0.32, 0.685), (0.016, 0.352, 0.67)], [0.042, 0.038, 0.028],
                        (1, 0, 0), 0.72), RED, "tail")
    k.paint(lambda P: inside(0.004 - (P[:, 1] - (0.27 + 0.9 * (P[:, 2] - 0.66))), 0.003), CREAM, on=["tail"])

    # --- spots over the back, as the fawn's grown ---------------------------------------------------------------
    cb.spots(k, BC, BR, [((0.45, 0.25, 1), 0.017, 0.013), ((0.6, -0.1, 0.85), 0.014, 0.012),
                         ((0.7, 0.45, 0.7), 0.015, 0.012), ((0.25, 0.55, 1), 0.014, 0.012),
                         ((0.8, 0.15, 0.55), 0.013, 0.011), ((0.35, -0.2, 1), 0.012, 0.01),
                         ((0.55, 0.7, 0.75), 0.012, 0.01)])

    # --- the ears: out to the sides, a pale lining --------------------------------------------------------------
    cb.ear(k, (0.05, -0.305, 0.99), (1, 0.25, 0.5), unit(0.15, -0.9, 0.35), 0.125, 0.042, RED, LINING, 0.026)

    # --- the antlers: dark boughs up and out, tines forward and up, moss on the beams, flowers, ivy, a halo ------
    for s, nm in ((1, "R"), (-1, "L")):
        m = np.array((s, 1, 1.0))
        P = lambda *p: np.array(p) * m
        b = [P(0.035, -0.3, 1.0), P(0.1, -0.292, 1.07), P(0.19, -0.272, 1.14), P(0.27, -0.252, 1.22),
             P(0.315, -0.24, 1.3)]
        beam = tube(b, [0.032, 0.029, 0.026, 0.022, 0.017], seg=14, per=6)
        brow = tube([b[1], P(0.125, -0.35, 1.1), P(0.14, -0.385, 1.16)], [0.024, 0.02, 0.017], seg=12, per=6)
        mid = tube([b[2], P(0.185, -0.29, 1.22), P(0.2, -0.3, 1.29)], [0.024, 0.02, 0.017], seg=12, per=6)
        out_ = tube([b[3], P(0.34, -0.27, 1.26), P(0.385, -0.275, 1.31)], [0.022, 0.019, 0.016], seg=12, per=6)
        back = tube([P(0.23, -0.262, 1.18), P(0.245, -0.215, 1.25), P(0.25, -0.2, 1.31)], [0.021, 0.018, 0.016],
                    seg=12, per=6)
        k.add("antler" + nm, outward(join(beam, brow, mid, out_, back)), ANTLER, "head", fuse=False, surface="shell")
        # moss over the beam's middle, in a sleeve and lumps
        k.add("mosssleeve" + nm, outward(tube(b[1:4], [0.036, 0.034, 0.03], seg=14, per=6)), MOSS, "head",
              fuse=False, surface="leaf")
        for t, (p0, p1) in enumerate(((b[1], b[2]), (b[2], b[3]), (b[1], b[2]), (b[2], b[3]))):
            c = p0 + (p1 - p0) * (0.3 + 0.4 * (t % 2)) + np.array([0, -0.006 * (t // 2), 0.018])
            k.add(f"mosslump{t}" + nm, ell(c, (0.028, 0.026, 0.02), deform=lumpy(3, 2, 0.2), sub=3),
                  MOSS2 if t % 2 else MOSS, "head", fuse=False, surface="leaf")
        for c, n in ((P(0.15, -0.31, 1.13), (0.3 * s, -1, 0.5)), (P(0.24, -0.29, 1.21), (0.4 * s, -1, 0.6)),
                     (P(0.08, -0.31, 1.07), (0.2 * s, -1, 0.7))):
            dl.flower(k, c, n, 0.028, FLOWER, MIDDLE)
        # ivy hanging from the beams in chains of leaves
        ivy_chain(k, "ivyhang0" + nm, b[3] + P(0.012, 0, -0.03), 0.15, s, "head", 4)
        ivy_chain(k, "ivyhang1" + nm, b[2] + P(0.02, 0, -0.03), 0.1, s, "head", 3)
    k.add("halo", ring((0, -0.27, 1.205), 0.175, 0.017), HALO, "head", fuse=False, glow=HALO)

    # --- the face: a pale patch round each eye; calm eyes, half shut under a level lid ---------------------------
    de = unit(0.66, -0.7, 0.2)
    E = cb.on_ell(H, HR, de)
    k.decal(E + np.array([0.002, -0.002, -0.001]), de, (0.037, 0.034), PATCH, eps=0.003, rings=8, segs=40,
            mirror=True)
    k.decal((0.031, -0.442, 0.885), unit(0.5, -0.75, -0.3), (0.015, 0.004), cb.INK, up=unit(0, 0.35, 1), eps=0.003,
            rings=3, segs=24, mirror=True)

    k.build(voxel=0.0072, smooth=4, band=0.008)
    cut = lv.brow_cut(0.38, 0.12)
    k.eye_states(cb.eyes_pair(k, E, de, (0.029, 0.03), cb.dark_eye, srgb(PATCH), line=0.17, outline=cut))
    k.export(out)


if __name__ == "__main__":
    cerbanor(*args())
