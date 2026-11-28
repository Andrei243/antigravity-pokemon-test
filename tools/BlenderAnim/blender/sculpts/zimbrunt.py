# Zimbrunt (No. 10062, Carpathia, Ground/Normal), sculpted from nothing on the skeleton and clips of the game's export:
#   blender --background --factory-startup --python sculpts/zimbrunt.py -- <the export's Zimbrunt.glb> <out.glb>
# Grown from zimbrel.py (sibling("zimbrel")): the same wool puffs, the same leg with a fringe over its hoof, the same
# ear with its lining, the same round dark eye with a white glint (here under a heavy brow), the same pale star (here
# of six points), the same muzzle and nostrils. From the picture (10062-Zimbrunt.jpg) and the brief
# (CP-dex-2-mountains.md, 62): a giant European bison; a massive dark-brown hump and shaggy mane over the head, the
# neck, the shoulders and the forelegs, its largest mass; the hindquarters smooth, tawny orange with paler patches;
# the head carried low under the mane, brown with a lighter muzzle, stern eyes under a heavy brow; short black horns
# out and curving up; ears out to the sides under them; a pale six-pointed star between the horns; a heavy shaggy
# beard of tan and brown under the jaw, tan fur on the chest; thick dark legs, black hooves, clods of earth caked
# round each fetlock; a thin tail with a dark tuft.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, hull, lumpy, outward, srgb, inside, unit, polygon, args, sibling)

zz = sibling("zimbrel")
lv = sibling("larvitar")              # an eye's outline cut along a brow

DARK, DARK2, FACE, MUZ = "#5A3F2A", "#6E4E34", "#7C5A3C", "#A47E5E"
HIND, HINDLIGHT, TAN, HORN = "#C98A50", "#DEB07A", "#C9A274", "#262428"
HOOF, DIRT, LINING, STAR = "#2E2A2A", "#7E5C3C", "#C59A74", "#F2E8D2"
NOSTRIL = "#3A2620"

STAR6 = polygon([(math.cos(math.pi / 2 + i * math.pi / 6) * (1 if i % 2 == 0 else 0.56),
                  math.sin(math.pi / 2 + i * math.pi / 6) * (1 if i % 2 == 0 else 0.56)) for i in range(12)])


def tuft(k, name, root, d, length, width, thick, bone, color, side=None):
    """A ragged lock of shaggy fur: a solid kite-sectioned spike hanging from root along d."""
    root, d = np.array(root, float), unit(*d)
    a = np.cross(d, (0, 0, 1.0)) if side is None else np.array(side, float)
    if np.linalg.norm(a) < 1e-3:
        a = np.array([1.0, 0, 0])
    a = a - d * (a @ d); a /= np.linalg.norm(a)
    b = np.cross(d, a)
    k.add(name, hull([root + a * width, root - a * width, root + b * thick, root - b * thick,
                      root + d * length * 0.5 + a * width * 0.6, root + d * length * 0.5 - a * width * 0.6,
                      root + d * length]), color, bone, fuse=False, surface="fur")


def zimbrunt(src, out):
    k = Kit(src, coat="fur")

    # --- the hindquarters: smooth, tawny orange, paler patches over the flank ------------------------------------
    k.add("barrel", ell((0, 0.24, 0.64), (0.3, 0.42, 0.28)), HIND, "body")
    k.add("rump", ell((0, 0.48, 0.67), (0.285, 0.22, 0.27)), HIND, "body")
    k.paint(lambda P: inside(np.hypot((P[:, 1] - 0.3) / 0.17, (P[:, 2] - 0.74) / 0.11) - 1, 0.03)
            * inside(0.15 - np.abs(P[:, 0]), 0.02), HINDLIGHT, on=["barrel", "rump"])

    # --- the forequarters: the great dark hump and shaggy mane --------------------------------------------------
    FC, FR = np.array([0, -0.25, 0.7]), np.array([0.37, 0.3, 0.36])
    k.add("fore", ell(FC, FR, deform=lumpy(7, 5, 0.05)), DARK, "body")
    HC, HR_ = np.array([0, -0.16, 0.93]), np.array([0.34, 0.34, 0.3])
    k.add("hump", ell(HC, HR_, deform=lumpy(7, 5, 0.06)), DARK, "body")
    k.add("manefall", ell((0, -0.46, 0.73), (0.32, 0.2, 0.3), deform=lumpy(6, 4, 0.06)), DARK, "body")
    zz.puffs(k, "humppuff", HC, HR_, 36, 0.095, [DARK, DARK2, DARK], "body", below=0.0, front=0.85, seed=62,
             sink=0.88)
    zz.puffs(k, "forepuff", FC, FR, 30, 0.085, [DARK2, DARK, DARK], "body", below=-0.25, front=1.0, seed=63,
             sink=0.86)
    # tan shaggy fur on the chest, under the beard
    k.paint(lambda P: inside(P[:, 1] + 0.42, 0.02) * inside(P[:, 2] - 0.66, 0.02) * inside(np.abs(P[:, 0]) - 0.2, 0.03)
            * inside(0.3 - P[:, 2], 0.02), TAN, on=["fore", "manefall", "forepuff"])
    # the mane's ragged lower edge over the forelegs and the chest
    rs = np.random.RandomState(620)
    for i, th in enumerate(np.linspace(-0.5 * math.pi - 1.5, -0.5 * math.pi + 1.5, 11)):
        r = 0.32 + 0.02 * (i % 2)
        root = np.array([r * math.cos(th), -0.3 + 0.75 * r * math.sin(th) * 0.6, 0.42])
        o = np.array([math.cos(th), math.sin(th) * 0.6, 0])
        tuft(k, f"fringe{i}", root, o * 0.35 + (0, 0, -1), 0.12 + 0.03 * rs.rand(), 0.045, 0.02, "body",
             TAN if abs(th + 0.5 * math.pi) < 0.5 else DARK)

    # --- legs: thick and dark, fused shoulders and thighs, a shaggy fringe over black hooves, clods of earth -----
    k.add("shoulder", ell((0.24, -0.35, 0.5), (0.13, 0.15, 0.2)), DARK, "leg{s}", mirror=True)
    k.add("thigh", ell((0.22, 0.42, 0.52), (0.13, 0.17, 0.2)), HIND, "hind{s}", mirror=True)
    zz.leg(k, "foreleg", [(0.26, -0.36, 0.5), (0.265, -0.37, 0.28), (0.27, -0.37, 0.09)], [0.12, 0.1, 0.094],
           "leg{s}", DARK, hoof=HOOF, fringe=DARK2, fringe_r=1.15)
    zz.leg(k, "hindleg", [(0.25, 0.44, 0.48), (0.255, 0.42, 0.28), (0.26, 0.45, 0.09)], [0.12, 0.1, 0.092],
           "hind{s}", HIND, hoof=HOOF, fringe=DARK2, fringe_r=1.15)
    k.paint(lambda P: inside(P[:, 2] - 0.4, 0.02), DARK, on=["hindleg", "thigh"])
    for nm, bone, (x, y) in (("fore", "leg{s}", (0.27, -0.37)), ("hind", "hind{s}", (0.26, 0.45))):
        for j, (a, z, r) in enumerate(((-0.9, 0.2, 0.04), (0.6, 0.16, 0.034), (2.3, 0.22, 0.036), (-2.4, 0.15, 0.03))):
            c = (x + math.cos(a) * 0.1, y + math.sin(a) * 0.1, z)
            k.add(f"clod{nm}{j}", ell(c, (r, r, r * 0.8), deform=lumpy(3, 2, 0.25), sub=3), DIRT, bone, fuse=False,
                  mirror=True, surface="fur")

    # --- the tail: thin, swept a little toward +X, a dark tuft at its end ---------------------------------------
    k.add("tail", tube([(0, 0.7, 0.82), (0.05, 0.79, 0.68), (0.08, 0.82, 0.52)], [0.036, 0.032, 0.028]), HIND, "tail")
    for j, sx in enumerate((-0.5, 0.0, 0.5, 0.2)):
        tuft(k, f"tailtuft{j}", (0.08, 0.82, 0.55 - 0.02 * j), (sx, 0.2, -1), 0.2, 0.05, 0.024, "tail", DARK,
             side=(1, 0, 0))

    # --- the head: low under the mane, brown, a lighter muzzle; a dark shock over the crown ---------------------
    H, HR = np.array([0, -0.8, 0.62]), np.array([0.2, 0.19, 0.21])
    k.add("head", ell(H, HR), FACE, "head")
    k.add("muzzle", ell((0, -0.975, 0.5), (0.135, 0.1, 0.105)), MUZ, "head")
    k.add("brow", ell((0, -0.87, 0.72), (0.19, 0.09, 0.07)), FACE, "head")
    k.add("forelock", ell((0, -0.72, 0.82), (0.22, 0.17, 0.12), deform=lumpy(6, 3, 0.14)), DARK, "head")
    for i, (x, y, z) in enumerate(((0.0, -0.8, 0.86), (0.12, -0.78, 0.83), (-0.12, -0.78, 0.83), (0.06, -0.86, 0.82),
                                   (-0.06, -0.86, 0.82), (0.17, -0.7, 0.8), (-0.17, -0.7, 0.8))):
        k.add(f"lock{i}", ell((x, y, z), (0.065, 0.06, 0.055), deform=lumpy(4, 3, 0.12), sub=3), DARK2, "head")
    # the heavy beard under the jaw and throat, tan and brown, in ragged locks
    k.add("beard", ell((0, -0.8, 0.4), (0.16, 0.14, 0.15), deform=lumpy(6, 3, 0.12)), TAN, "head")
    for i, (x, y) in enumerate(((0, -0.86), (0.07, -0.85), (-0.07, -0.85), (0.12, -0.8), (-0.12, -0.8),
                                (0.04, -0.75), (-0.04, -0.75))):
        tuft(k, f"beardlock{i}", (x, y, 0.33), (x * 0.6, -0.15, -1), 0.15 + 0.02 * (i % 3), 0.045, 0.02, "head",
             TAN if i % 2 == 0 else DARK2)

    # --- short black horns out of the head's sides, curving up; ears out under them ----------------------------
    k.add("horn", outward(tube([(0.15, -0.73, 0.77), (0.26, -0.74, 0.8), (0.34, -0.76, 0.87), (0.355, -0.79, 0.97)],
                               [0.052, 0.044, 0.032, 0.014], seg=18, per=6)), HORN, "head", fuse=False, mirror=True,
          surface="shell")
    zz.ear(k, (0.18, -0.77, 0.64), (1, 0.15, -0.22), unit(0.1, -0.9, 0.35), 0.13, 0.055, FACE, LINING, 0.036)

    # --- the face: nostrils, a set mouth, the six-pointed star between the horns --------------------------------
    for s in (1, -1):
        k.decal((0.05 * s, -1.072, 0.52), unit(0.25 * s, -1, 0.1), (0.022, 0.016), NOSTRIL, eps=0.004, rings=6,
                segs=28)
    k.decal((0, -1.05, 0.425), unit(0, -0.8, -0.6), (0.06, 0.008), NOSTRIL, eps=0.004, rings=3, segs=28)
    sd = unit(0, -0.96, 0.28)
    k.decal(zz.on_ell(H, HR, sd) + np.array([0, -0.06, 0.06]), sd, (0.055, 0.055), STAR, eps=0.005, outline=STAR6,
            rings=10, segs=96)

    de = unit(0.62, -0.76, 0.12)
    E = zz.on_ell(H, HR, de)
    k.build(voxel=0.0125, smooth=4, band=0.014)
    cut = lv.brow_cut(0.4, 0.3)
    k.eye_states(zz.eyes_pair(k, E, de, (0.036, 0.03), zz.round_eye, srgb(FACE), line=0.17, outline=cut))
    k.export(out)


if __name__ == "__main__":
    zimbrunt(*args())
