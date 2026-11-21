# Bouffalant sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/bouffalant.py -- <the export's Bouffalant.glb> <out.glb>
#
# The design, from the references: a brown buffalo carrying its head low under a great round afro of dark grey-brown
# hair that covers its head, neck and shoulders, its surface a cauliflower of puffs; the face peeks out under the afro's
# front: a brown brow, yellow eyes with a black slit pupil half hidden by the hair, a pale grey-blue muzzle with two
# dark nostrils; two cream horns come out of the afro's sides, run out level and curve up at the tips, each with two
# gold rings near its middle; a brown body with ragged shaggy fur hanging under the belly and over the tops of the
# legs; slim grey-brown lower legs (here thicker, no stilts) on pale grey hooves; a short tail raised in a tuft.
# Reference ratios (3D picture): afro 0.76 of the height across, length nose to rump 1.0 of the height, legs showing
# under the fringe 0.23 of the height, horn span about the length.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, ring_band, srgb, inside, unit, eye_look, outward, args

HIDE, AFRO, AFRO2, FACE, MUZZLE = "#A86A34", "#584C45", "#655850", "#7E5530", "#AEB0BE"
NOSTRIL, LEG, HOOF, HORN, GOLD = "#3A3436", "#857670", "#CFCFD4", "#E4D8AA", "#ECB93A"
EYE, INK = "#F2C230", "#1A1614"


def tuft(k, name, root, d, length, width, thick, bone, color=HIDE, side=None):
    """A ragged tuft of fur: a solid kite-sectioned spike hanging from root along d."""
    root, d = np.array(root, float), unit(*d)
    a = np.cross(d, (0, 0, 1.0)) if side is None else np.array(side, float)
    if np.linalg.norm(a) < 1e-3:
        a = np.array([1.0, 0, 0])
    a = a - d * (a @ d); a /= np.linalg.norm(a)
    b = np.cross(d, a)
    k.add(name, hull([root + a * width, root - a * width, root + b * thick, root - b * thick,
                      root + d * length * 0.5 + a * width * 0.6, root + d * length * 0.5 - a * width * 0.6,
                      root + d * length]), color, bone, fuse=False, surface="fur")


def bouffalant(src, out):
    k = Kit(src, coat="fur")

    # The body: a barrel, a hump of shoulders under the afro, a round rump
    k.add("barrel", ell((0, 0.06, 0.295), (0.145, 0.24, 0.15)), HIDE, "body")
    k.add("shoulders", ell((0, -0.1, 0.35), (0.15, 0.13, 0.16)), HIDE, "body")
    k.add("rump", ell((0, 0.2, 0.33), (0.13, 0.11, 0.13)), HIDE, "body")

    # The face under the afro's front, carried low
    HC = np.array([0, -0.3, 0.27])
    HEAD = ell(HC, (0.068, 0.075, 0.07))
    k.add("head", HEAD, FACE, "head")
    MZ = ell(HC + (0, -0.06, -0.06), (0.058, 0.055, 0.05), rot=(-20, 0, 0))
    k.add("muzzle", MZ, MUZZLE, "head")
    k.add("neck", tube([(0, -0.14, 0.38), (0, -0.24, 0.32)], [0.1, 0.08]), FACE, "head")

    # The afro: a great ball of puffs over head, neck and shoulders
    AC, AR = np.array([0, -0.15, 0.44]), np.array([0.19, 0.18, 0.18])
    k.add("afro", ell(AC, AR * 0.9), AFRO, "head")
    n = 70
    for i in range(n):
        z = 1 - 2 * (i + 0.5) / n
        a = i * math.pi * (3 - math.sqrt(5))
        r = math.sqrt(1 - z * z)
        p = np.array([r * math.cos(a), r * math.sin(a), z])
        if p[2] < -0.75:
            continue
        c = AC + p * AR * 0.9
        k.add(f"afro{i}", ell(c, (0.055, 0.055, 0.052)), AFRO if i % 3 else AFRO2, "head")

    # The horns: out of the afro's sides, level, curving up at the tips; two gold rings each
    HP = [(0.12, -0.18, 0.42), (0.22, -0.18, 0.41), (0.3, -0.17, 0.415), (0.355, -0.155, 0.45), (0.38, -0.14, 0.51)]
    HRR = [0.046, 0.044, 0.038, 0.027, 0.011]
    k.add("horn", outward(tube(HP, HRR, seg=24)), HORN, "head", fuse=False, mirror=True, surface="shell")
    for x, r in ((0.232, 0.0435), (0.268, 0.0415)):
        k.add(f"ring{x}", ring_band((x, -0.178, 0.41), (1, 0.02, -0.03), r + 0.005, 0.02, thick=0.009), GOLD, "head",
              fuse=False, mirror=True, surface="metal")

    # The legs: shoulder and thigh masses in the body, the lower legs grey-brown, pale hooves
    LEGS = (("fore", "leg{s}", [(0.09, -0.17, 0.3), (0.092, -0.18, 0.18), (0.094, -0.19, 0.09), (0.095, -0.2, 0.05)]),
            ("hind", "hind{s}", [(0.09, 0.2, 0.3), (0.092, 0.22, 0.18), (0.094, 0.23, 0.09), (0.095, 0.22, 0.05)]))
    for name, bone, pts in LEGS:
        k.add(name, tube(pts, [0.072, 0.054, 0.046, 0.042]), LEG, bone, mirror=True)
        x, y = pts[-1][0], pts[-1][1]
        k.add(name + "hoof", ell((x, y - 0.008, 0.026), (0.044, 0.05, 0.028),
                                deform=lambda V: V * (1 + 0.12 * np.clip(-V[:, 2:3], 0, 1))), HOOF, bone,
              mirror=True, surface="shell")
    k.add("shoulder", ell((0.095, -0.17, 0.29), (0.07, 0.09, 0.11)), HIDE, "leg{s}", mirror=True)
    k.add("thigh", ell((0.092, 0.2, 0.29), (0.072, 0.1, 0.12)), HIDE, "hind{s}", mirror=True)
    k.paint(lambda P: inside(P[:, 2] - 0.15, 0.004) * (np.abs(P[:, 0]) > 0.03), LEG,
            on=["fore", "hind", "shoulder", "thigh"])
    k.paint(lambda P: inside(P[:, 2] - 0.046, 0.003) * (np.abs(P[:, 0]) > 0.03), HOOF, on=["fore", "hind"])

    # The shaggy fringe: ragged tufts under the belly and over the tops of the lower legs
    for i, y in enumerate(np.linspace(-0.12, 0.18, 7)):
        for s in (1, -1):
            x = s * (0.1 + 0.012 * (i % 2))
            tuft(k, f"belly{i}{s}", (x + 0.02 * s, y, 0.185), (0.3 * s, 0.05, -1), 0.07 + 0.015 * (i % 2), 0.026, 0.012, "body",
                 side=(0, 1, 0))
    for i, y in enumerate((-0.06, 0.04, 0.12)):
        tuft(k, f"under{i}", (0, y, 0.17), (0, 0.1, -1), 0.06, 0.03, 0.012, "body", side=(1, 0, 0))
    for name, bone, (lx, ly) in (("fore", "leg{s}", (0.093, -0.18)), ("hind", "hind{s}", (0.093, 0.22))):
        for s in (1, -1):
            for j, a in enumerate(np.radians((-60, 0, 60, 180))):
                o = np.array([math.sin(a) * s, -math.cos(a), 0])
                tuft(k, f"knee{name}{s}{j}", np.array((lx * s, ly, 0.16)) + o * 0.05, o * 0.4 + (0, 0, -1), 0.06,
                     0.02, 0.01, bone.format(s="R" if s > 0 else "L"))

    # The tail: short, raised, a dark tuft at its end
    k.add("tail", outward(tube([(0, 0.28, 0.4), (0, 0.32, 0.43), (0, 0.34, 0.465)], [0.018, 0.015, 0.013], seg=12)),
          HIDE, "tail", fuse=False, surface="fur")
    for j, sx in enumerate((-0.4, 0.0, 0.4)):
        tuft(k, f"tailtuft{j}", (0, 0.34, 0.455), (sx, 0.25, 1), 0.06, 0.022, 0.012, "tail", AFRO, side=(1, 0, 0))

    # Two dark nostrils on the muzzle's front
    NOSE = np.array([0, -1, -0.35]); NOSE /= np.linalg.norm(NOSE)
    for s in (1, -1):
        k.decal(np.array((0.024 * s, -0.405, 0.2)), unit(0.2 * s, -1, -0.3), (0.011, 0.009), NOSTRIL, eps=0.003,
                rings=6, segs=28)

    # Yellow eyes with a black slit, under the afro's edge
    CF, CK, CY = srgb(FACE), srgb(INK), srgb(EYE)
    def open_eye(u, v):
        col = np.tile(CY, (len(u), 1))
        col[(u / 0.28) ** 2 + (v / 0.85) ** 2 < 1] = CK
        col[u * u + v * v > 0.78] = CK
        return col
    de = unit(0.55, -0.8, 0.2)
    E = HC + (0.045, -0.055, 0.02)
    def lay(state):
        k.decal(E, de, (0.02, 0.016), eye_look(state, open_eye, CF, CK, line=0.2), eps=0.003, rings=20, segs=56,
                mirror=True)

    k.build(voxel=0.0062, band=0.005, wband=0.05)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    bouffalant(*args())
