# Rayquaza's Mega Evolution sculpted on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/rayquazamega.py -- <the export's Rayquaza-Mega.glb> <out.glb>
# From the two pictures: Rayquaza's coil, darker and armoured. Largest first: two long gold streamers out of the back
# of the head, flowing up and back over the coil, flat orange vanes along them and a red-orange bead at each end (kept
# within the coil's span here: as long as the picture's, they would make the body a speck in the game's cell); the
# head grown large and armoured, a long green blade a side running forward and down past the jaw like a mandible, a
# great crest a side swept up and back off the crown, gold lines on them, the mouth open (red within), a gold V on the
# brow, red eyes; the body's underside black with a row of orange spots, the green above crossed by dark lines; the
# fins down its sides longer, swept back, green with gold rims; orange flame wisps behind the jaw; small dark arms
# with white claws.
# Grown from rayquaza.py (sibling): its coil, its spline and frame, its arms, its eyes cut by a brow.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, slab, ring_band, outward, srgb, inside, unit, eye_look, args, sibling

ray = sibling("rayquaza")

GREEN, DARKG, BELLY = "#239A5E", "#17683E", "#1C1F22"
GOLD, ORANGE, BEAD = "#F0C23C", "#F59A3C", "#E8603A"
RED_MAW, WHITE, INKC = "#B8323E", ray.WHITE, ray.INKC
EYE_RED = srgb("#E23C4A")

HJ = ray.HJ
HS = 1.4                                     # the head grown about its joint (larger than Rayquaza's)


def body(k):
    for i, bone in enumerate(ray.BONES):
        sel = (ray.SEG >= i - 0.15) & (ray.SEG <= i + 1.15)
        k.add(f"seg{i:02d}", tube(ray.CRV[sel][::2], ray.RAD[sel][::2], seg=24, per=5), GREEN, bone)
    # dark lines round the body between its segments
    for j, s in enumerate((2.5, 4.0, 5.5, 7.0, 8.5, 10.0)):
        c, t, side, up, r = ray.at(s)
        k.add(f"band{j}", ring_band(c, t, r - 0.001, 0.009, thick=0.006), INKC, ray.BONES[int(s)], fuse=False)
    FC, FS, UPV, R, P = ray.FC, ray.FS, ray.UPV, ray.R, ray.P
    # the underside black: below the centre line by its own up
    def under(Q):
        j = np.concatenate([((Q[a:a + 3000, None, :] - FC[None]) ** 2).sum(2).argmin(1) for a in range(0, len(Q), 3000)])
        rad = Q - FC[j]
        rr = np.interp(FS[j], np.arange(len(P)), R)
        return inside((rad * UPV[j]).sum(1) + 0.12 * rr, 0.003) * (FS[j] > 0.3)
    k.paint(under, BELLY, on=["seg"])
    # a row of orange spots down the black
    spots = []
    for s in np.arange(0.9, 10.6, 0.55):
        c, t, side, up, r = ray.at(s)
        spots.append((c - up * r * 0.98, 0.022 * r / 0.065 + 0.006))
    def dots(Q):
        m = np.zeros(len(Q))
        for c, rd in spots:
            m = np.maximum(m, inside(np.linalg.norm(Q - c, axis=1) - rd, 0.003))
        return m
    k.paint(dots, ORANGE, on=["seg"])


FIN = [(0.0, -0.05), (0.07, -0.08), (0.15, -0.13), (0.13, -0.04), (0.11, 0.035), (0.05, 0.055), (0.0, 0.05)]


def fins(k):
    # Fins down the sides, longer and swept back toward the tail, gold-rimmed, banked so that their faces show
    for j, (s, size) in enumerate(((3.0, 0.95), (6.0, 1.1), (9.0, 0.9))):
        c, t, side, up, r = ray.at(s)
        for sd in (1, -1):
            out_ = side * sd
            ex = unit(*(out_ * 0.9 + up * 0.35))
            o = c + out_ * r * 0.6
            nm = f"{j}{'R' if sd > 0 else 'L'}"
            k.add(f"fin{nm}", slab(np.array(FIN) * size, o, ex, t, 0.024), GOLD, ray.BONES[int(s)], fuse=False,
                  surface="scales")
            inner = np.array(FIN) * size * np.array([0.84, 0.74]) + np.array([0.004, 0.0])
            k.add(f"finin{nm}", slab(inner, o, ex, t, 0.032), GREEN, ray.BONES[int(s)], fuse=False, surface="scales")
    # the tail's fin of three points, gold-rimmed
    c, t, side, up, r = ray.at(0.05)
    d = -t
    TF = [(0.0, -0.03), (0.07, -0.1), (0.05, -0.03), (0.14, 0.0), (0.05, 0.03), (0.07, 0.1), (0.0, 0.03)]
    ez = unit(*(side * 0.8 + up * 0.6))
    k.add("tailfin", slab(TF, ray.TIP - d * 0.02, d, ez, 0.024), GOLD, "tail", fuse=False, surface="scales")
    inner = [(0.012, -0.018), (0.055, -0.066), (0.045, -0.02), (0.11, 0.0), (0.045, 0.02), (0.055, 0.066),
             (0.012, 0.018)]
    k.add("tailfinin", slab(inner, ray.TIP - d * 0.02, d, ez, 0.032), GREEN, "tail", fuse=False, surface="scales")


def blade(b, d, ln, w, th, up=(0, 0, 1.0)):
    """A flat-faced blade from b along d, wide across up, th thick."""
    b, d = np.array(b, float), unit(*d)
    u = np.array(up, float); u = u - d * (u @ d); u /= np.linalg.norm(u)
    side = np.cross(d, u)
    return hull([b - d * 0.03 + u * w / 2, b - d * 0.03 - u * w / 2, b + side * th / 2, b - side * th / 2,
                 b + d * ln * 0.5 + u * w * 0.4, b + d * ln * 0.5 - u * w * 0.3,
                 b + d * ln * 0.5 + side * th * 0.35, b + d * ln * 0.5 - side * th * 0.35, b + d * ln])


def head(k):
    """The head as laid out at Rayquaza's size; the caller grows it about its joint."""
    k.add("skull", ell(ray.H, ray.HR * np.array([1.05, 1.0, 1.08])), GREEN, "head")
    k.add("snout", ell((0, -0.345, 0.6), (0.046, 0.1, 0.034), rot=(-4, 0, 0)), GREEN, "head")
    k.add("brow", ell((0.034, -0.3, 0.63), (0.028, 0.05, 0.017), rot=(-12, 0, -15)), GREEN, "head", mirror=True)
    # the jaws open, red within
    k.add("jaw", ell((0, -0.32, 0.535), (0.042, 0.09, 0.02), rot=(24, 0, 0)), GREEN, "head")
    k.add("maw", ell((0, -0.31, 0.56), (0.037, 0.075, 0.026), rot=(12, 0, 0)), RED_MAW, "head")
    # gold lines along the snout's sides and a gold V on the brow
    def lines(Q):
        Q = HJ + (Q - HJ) / HS
        y, z, x = Q[:, 1], Q[:, 2], np.abs(Q[:, 0])
        side_ = inside(np.abs(z - (0.603 + 0.12 * (y + 0.3))) - 0.0045, 0.0015) * (y < -0.27) * (y > -0.42) * (x > 0.02)
        vee = inside(np.abs(x - 0.6 * (y + 0.345)) - 0.0055, 0.0015) * (y > -0.345) * (y < -0.27) * (z > 0.628)
        return np.maximum(side_ * (z > 0.575), vee)
    k.paint(lines, GOLD, on=["snout", "skull", "brow"])
    for s in (1, -1):
        a = np.array((0.028 * s, -0.4, 0.58))
        k.add(f"fang{s}", hull([a + (0.008, 0, 0), a - (0.008, 0, 0), a + (0, 0.01, 0), a - (0, 0.006, 0),
                                a + (0.002 * s, -0.004, -0.03)]), WHITE, "head", fuse=False, surface="shell")
    for s, sd in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        # the mandible: a long blade off the cheek running forward and down past the jaw, a gold edge under it
        k.add(f"mandible{sd}", blade(np.array((0.05, -0.23, 0.57)) * m, np.array((0.18, -0.72, -0.5)) * m, 0.26, 0.06,
                                     0.026, up=(0, 0.5, 1)), GREEN, "head", fuse=False, surface="scales")
        k.add(f"mandedge{sd}", blade(np.array((0.052, -0.24, 0.548)) * m, np.array((0.18, -0.72, -0.5)) * m, 0.245,
                                     0.022, 0.03, up=(0, 0.5, 1)), GOLD, "head", fuse=False, surface="scales")
        # the great crest swept up and back off the crown, a smaller one behind it, both gold-edged beneath
        k.add(f"crest{sd}", blade(np.array((0.03, -0.25, 0.645)) * m, np.array((0.3, 0.85, 0.42)) * m, 0.27, 0.06,
                                  0.03, up=(0, -0.6, 0.8)), GREEN, "head", fuse=False, surface="scales")
        k.add(f"crestedge{sd}", blade(np.array((0.032, -0.25, 0.632)) * m, np.array((0.3, 0.85, 0.42)) * m, 0.25,
                                      0.022, 0.034, up=(0, -0.6, 0.8)), GOLD, "head", fuse=False, surface="scales")
        k.add(f"cheek{sd}", blade(np.array((0.05, -0.22, 0.59)) * m, np.array((0.75, 0.62, 0.1)) * m, 0.14, 0.04,
                                  0.026), GREEN, "head", fuse=False, surface="scales")
        # orange flame wisps behind the jaw
        for j, (dz, dl) in enumerate(((0.02, 0.1), (-0.015, 0.12), (-0.05, 0.09))):
            k.add(f"wisp{sd}{j}", blade(np.array((0.045, -0.24, 0.55 + dz)) * m, np.array((0.4, 0.85, 0.15 - dz * 4)) * m,
                                        dl, 0.035, 0.022), ORANGE, "head", fuse=False, surface="scales")


def streamers(k):
    """The two gold streamers out of the back of the head, up and back over the coil, vanes along them, a bead at the
    end (laid out at full size: not grown with the head)."""
    for s, sd in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        pts = np.array([(0.05, -0.15, 0.66), (0.12, -0.08, 0.75), (0.21, 0.04, 0.78), (0.27, 0.18, 0.73),
                        (0.28, 0.32, 0.66), (0.23, 0.45, 0.64), (0.2, 0.55, 0.69)]) * m
        k.add(f"streamer{sd}", outward(tube(pts, [0.016, 0.016, 0.015, 0.015, 0.014, 0.014, 0.013])), GOLD, "head",
              fuse=False, surface="scales")
        for j, i in enumerate((2, 4)):
            c = pts[i]
            d = unit(*(pts[i + 1] - pts[i - 1]))
            up = np.array([0, 0, 1.0]); up = up - d * (up @ d); up /= np.linalg.norm(up)
            V = [(-0.06, 0.0), (-0.02, 0.035), (0.04, 0.02), (0.07, 0.0), (0.04, -0.02), (-0.02, -0.035)]
            k.add(f"vane{sd}{j}", slab(V, c, d, up, 0.02), ORANGE, "head", fuse=False, surface="scales")
        k.add(f"bead{sd}", outward(ell(pts[-1] + unit(*(pts[-1] - pts[-2])) * 0.015, (0.03, 0.03, 0.03), sub=3)), BEAD,
              "head", fuse=False, surface="scales")


def eye_open(u, v):
    c = np.tile(EYE_RED, (len(u), 1))
    c[((u + 0.05) / 0.3) ** 2 + (v / 0.42) ** 2 < 1] = ray.CK
    r = np.sqrt(u * u + v * v)
    c[r > 0.84] = ray.CK
    return c


def eyes(k, state):
    E = ray.on_ell(HJ + (ray.H - HJ) * HS, ray.HR * np.array([1.05, 1.0, 1.08]) * HS, ray.DE) + np.array([0, -0.006, 0])
    look = eye_look(state, eye_open, ray.CG, ray.CK, line=0.2)
    mm = np.array([-1, 1, 1.0])
    half = (0.024 * HS, 0.02 * HS)
    k.decal(E, ray.DE, half, look, eps=0.004, rings=20, segs=56, outline=ray.cut)
    k.decal(E * mm, ray.DE * mm, half, lambda u, v: look(-u, v), eps=0.004, rings=20, segs=56,
            outline=lambda th: ray.cut(math.pi - th))


def rayquaza_mega(src, out):
    k = Kit(src, coat="scales")
    body(k)
    fins(k)
    n0 = len(k.parts)
    ray.arms(k)
    for p in k.parts[n0:]:
        if p["name"].startswith(("arm", "hand")):
            p["color"] = srgb(BELLY)
    n0 = len(k.parts)
    head(k)
    for p in k.parts[n0:]:
        p["V"] = HJ + (p["V"] - HJ) * HS
    streamers(k)
    k.build(voxel=0.0055, smooth=4, band=0.004)
    k.eye_states(lambda state: eyes(k, state))
    k.export(out)


if __name__ == "__main__":
    rayquaza_mega(*args())
