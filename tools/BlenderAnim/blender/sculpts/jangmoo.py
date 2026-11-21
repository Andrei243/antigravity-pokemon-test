# Jangmo-o sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/jangmoo.py -- <the export's Jangmo-o.glb> <out.glb>
# From the references: a small grey dragon on all fours, its head nearly half its height. A great yellow scale stands on
# its crown like a heart lying on its side, two round lobes at its top and its point over the brow, overhanging the
# face. A big eye on each side of the round grey head: white, a red iris toward the snout with a black slit, cut flat
# along its top by a heavy black brow line that turns down at the back. A small grey fin of three points behind each
# eye, a short muzzle with a mouth line and a small fang at its corner. A white tuft on the chest. The back and flanks
# carry grey scales edged yellow along their lower ends; the belly and the four short thick legs are charcoal, three
# yellow claws a foot. A grey tail curving up behind to a round yellow drop.
#
# Written to be grown from (hakamoo.py and kommoo.py import it with sibling("jangmoo")): the line's colours, its eye
# under a black brow, its claws and its yellow-edged scales are the functions below; the calls are under __main__.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, inside, smoothstep, unit, polygon, eye_look, axes, args

# --- the line's colours ---------------------------------------------------------------------------------------------
GREY, DARK = "#B9BABF", "#4A4850"
YELLOW, PALEYELLOW = "#F2CF3A", "#F8E58E"
RED = "#D8483C"
WHITE_FUR = "#F4F4F2"
INK, WHITE = srgb("#141418"), np.array([1.0, 1.0, 1.0])
RED_EYE = srgb("#D42A2A")


def on_ell(c, r, d):
    """The point of an ellipsoid (centre c, radii r, unturned) along the direction d from its centre."""
    d = unit(*d)
    t = 1.0 / math.sqrt(((d / np.array(r)) ** 2).sum())
    return np.array(c, float) + d * t


def claw(base, d, length, width, up=(0, 0, 1.0), flat=0.5):
    """A claw or a spike: a flat-faced point from base along d, a kite `width` wide across `up`."""
    base, d = np.array(base, float), unit(*d)
    up = np.array(up, float); up = up - d * (up @ d); up /= np.linalg.norm(up)
    side = np.cross(d, up)
    b = base - d * width * 0.3
    return hull([b + side * width / 2, b - side * width / 2, b + up * width * flat, b - up * width * flat,
                 base + d * length * 0.5 + side * width * 0.3, base + d * length * 0.5 - side * width * 0.3,
                 base + d * length * 0.5 + up * width * flat * 0.5, base + d * length * 0.5 - up * width * flat * 0.5,
                 base + d * length])


def oriented(c, n, d, r, sub=3):
    """An ellipsoid with radii r = (across, along d, along n): a scale lying on a surface whose normal is n, its
    length running along d."""
    n = unit(*n)
    d = np.array(d, float); d = d - n * (d @ n); d /= np.linalg.norm(d)
    a = np.cross(d, n)
    V, F = ell((0, 0, 0), r, sub=sub)
    return V @ np.stack([a, d, n]) + np.array(c, float), F


def scale(k, name, c, n, d, size, bone, inner=GREY, rim=YELLOW, rim_share=0.3, thick=0.28, inner_share=0.86):
    """A scale of the line: a yellow plate under a smaller plate of the inner colour, set toward the root (-d), so the
    yellow shows as an edge along the scale's free end (+d). Both are loose pieces lying on the normal n."""
    c, n = np.array(c, float), unit(*n)
    dd = np.array(d, float); dd = dd - n * (dd @ n); dd /= np.linalg.norm(dd)
    k.add(name + "rim", oriented(c, n, dd, (size, size * 1.15, size * thick)), rim, bone, fuse=False, surface="shell")
    k.add(name + "in", oriented(c - dd * size * rim_share + n * size * 0.06, n, dd,
                                (size * inner_share, size * 1.15 * (1 - rim_share * 0.8) * inner_share / 0.86, size * thick * 1.02)),
          inner, bone, fuse=False, surface="shell")


def brow_cut(BA, BB):
    """An eye's outline cut straight along the brow v = BA + BB u (u toward the back): its reach at each angle."""
    def cut(th):
        s = math.sin(th) - BB * math.cos(th)
        return min(1.0, BA / s) if s > 1e-6 else 1.0
    return cut


def line_eye(ba, bb, iris_c=(-0.22, -0.05), iris_r=0.42, slit=(0.11, 0.42), brow=0.3, rim=0.86):
    """The line's eye: white, a red iris toward the snout (-u) with a black upright slit, cut along its top by the
    brow's heavy black line, which also runs down the back edge."""
    def eye(u, v):
        cut = brow_cut(ba, bb)
        reach = np.array([cut(t) for t in np.arctan2(v, u)])
        rho = np.sqrt(u * u + v * v) / reach
        c = np.tile(WHITE, (len(u), 1))
        d = np.sqrt((u - iris_c[0]) ** 2 + (v - iris_c[1]) ** 2)
        c[d < iris_r] = RED_EYE
        c[(np.abs(u - iris_c[0]) / slit[0]) ** 2 + ((v - iris_c[1]) / slit[1]) ** 2 < 1] = INK
        g = np.sqrt((u - iris_c[0] - 0.15) ** 2 + (v - iris_c[1] - 0.2) ** 2)
        c[g < 0.1] = WHITE
        # the black brow along the top edge, and down the back edge
        top = ba + bb * u
        c[v > top - brow] = INK
        c[(rho > rim) & (u > 0.2)] = INK
        c[rho > 0.95] = INK
        return c
    return eye


def BA_line(u, cut):
    """The cut's top line's height over u (found from the cut at a few angles): where the brow lies."""
    th = np.arctan2(1.0, u)                      # a point straight up from u, roughly
    return np.array([cut(t) * math.sin(t) for t in th])


def eye_pair(k, E, de, half, look_open, lid, outline=None, line=0.16, eps=0.005, up=(0, 0, 1)):
    """Lays the eyes for a state: the right as it is, the left with its picture (and outline) turned over."""
    m = np.array([-1, 1, 1.0])
    def eyes(state):
        look = eye_look(state, look_open, lid, INK, line=line)
        k.decal(E, de, half, look, eps=eps, rings=20, segs=56, outline=outline, up=up)
        k.decal(E * m, de * m, half, lambda u, v: look(-u, v), eps=eps, rings=20, segs=56,
                outline=(lambda th: outline(math.pi - th)) if outline else None, up=np.array(up) * m)
    return eyes


def heart_crest(k, base, lobes, bone):
    """The great yellow scale on the crown: a base along the brow and two round lobes at its top, fused yellow."""
    k.add("crestbase", ell(base[0], base[1], rot=base[2]), YELLOW, bone, surface="shell")
    for j, (c, r, rot) in enumerate(lobes):
        k.add(f"crestlobe{j}", ell(c, r, rot=rot), YELLOW, bone, surface="shell")


def jangmoo(src, out):
    k = Kit(src, coat="scales")

    # --- the body: a low barrel, grey above and charcoal below ------------------------------------------------------
    BC, BR = np.array([0.0, 0.005, 0.132]), np.array([0.076, 0.098, 0.064])
    k.add("body", ell(BC, BR), GREY, "body")
    k.add("chest", ell((0, -0.06, 0.158), (0.066, 0.058, 0.062)), GREY, "body")
    k.add("neck", tube([(0, -0.07, 0.18), (0, -0.09, 0.22), (0, -0.095, 0.25)], [0.045, 0.042, 0.045]), GREY, "head")
    k.paint(lambda P: inside(P[:, 2] - (0.125 + 0.06 * np.clip(-P[:, 1] - 0.03, 0, 1)), 0.004), DARK,
            on=["body", "chest"])

    # the four short thick legs: haunches fused into the flanks, a short shin, a round foot with three yellow claws
    for (bone, y) in (("leg{s}", -0.065), ("hind{s}", 0.065)):
        k.add("thigh" + bone[:4], ell((0.054, y, 0.088), (0.044, 0.05, 0.05)), DARK, bone, mirror=True)
        k.add("shin" + bone[:4], tube([(0.058, y, 0.065), (0.06, y - 0.005, 0.03)], [0.032, 0.028]), DARK, bone,
              mirror=True)
        k.add("foot" + bone[:4], ell((0.058, y - 0.012, 0.017), (0.03, 0.038, 0.018)), DARK, bone, mirror=True)
        for j, dx in enumerate((-0.016, 0.0, 0.016)):
            k.add(f"claw{bone[:4]}{j}", claw((0.058 + dx, y - 0.04, 0.012), unit(dx * 6, -1, -0.15), 0.024, 0.015),
                  YELLOW, bone, fuse=False, mirror=True, surface="shell")

    # the scales over the back and flanks: grey, edged yellow along their lower ends
    for j, (y, ang) in enumerate([(-0.05, 50), (0.0, 52), (0.05, 55), (-0.025, 20), (0.03, 22)]):
        a = math.radians(ang)
        n = np.array([math.sin(a), 0.0, math.cos(a)])
        c = BC + np.array([BR[0] * math.sin(a) * 1.0, y, BR[2] * math.cos(a) * 1.0 + 0.0])
        c[2] += 0.004
        for s, side in ((1, "R"), (-1, "L")):
            m = np.array([s, 1, 1.0])
            scale(k, f"scale{j}{side}", c * m, n * m, (0.0, 0.25, -1.0), 0.026, "body")

    # the white tuft on the chest
    for j, (dx, dz, ln) in enumerate(((0.0, 0.0, 0.035), (0.018, 0.006, 0.028), (-0.018, 0.006, 0.028))):
        b = np.array((dx, -0.115, 0.185 + dz))
        k.add(f"tuft{j}", claw(b, unit(dx * 10, -0.7, -0.75), ln, 0.026, up=(0, -0.7, 0.7), flat=0.35), WHITE_FUR,
              "body", fuse=False, surface="fur")

    # the tail: up behind to a round yellow drop
    k.add("tail", tube([(0, 0.07, 0.15), (0, 0.13, 0.165), (0, 0.17, 0.185)], [0.032, 0.022, 0.014]), GREY, "tail")
    k.add("tailtip", ell((0, 0.2, 0.192), (0.022, 0.036, 0.028), rot=(-15, 0, 0)), YELLOW, "tail", surface="shell")

    # --- the head: round and big, a short muzzle forward ------------------------------------------------------------
    H, HR = np.array([0.0, -0.1, 0.27]), np.array([0.08, 0.088, 0.077])
    k.add("head", ell(H, HR), GREY, "head")
    k.add("muzzle", ell((0, -0.16, 0.25), (0.052, 0.05, 0.045)), GREY, "head")
    # the great yellow scale: its point along the brow, two lobes at its top
    heart_crest(k, ((0, -0.11, 0.335), (0.058, 0.09, 0.03), (-12, 0, 0)),
                [((0, -0.125, 0.362), (0.058, 0.066, 0.036), (-22, 0, 0)),
                 ((0, -0.05, 0.352), (0.054, 0.05, 0.03), (15, 0, 0))], "head")
    k.add("crestfront", ell((0, -0.172, 0.335), (0.046, 0.032, 0.022), rot=(-30, 0, 0)), YELLOW, "head",
          surface="shell")
    # the heart's pale middle on each face
    k.decal(on_ell((0, -0.1, 0.355), (0.058, 0.1, 0.05), (1, -0.05, 0.1)), unit(1, 0, 0.1), (0.016, 0.022),
            PALEYELLOW, eps=0.003, rings=8, segs=40, mirror=True)

    # the fins behind the eyes: three grey points fanned back and up
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        root = np.array((0.068, -0.04, 0.31)) * m
        for j, (dy, dz, ln) in enumerate(((0.9, 0.35, 0.05), (0.8, 0.75, 0.042), (0.85, -0.1, 0.04))):
            k.add(f"fin{side}{j}", claw(root, unit(0.45 * s, dy, dz), ln, 0.03, up=(s, 0, 0.2), flat=0.4), GREY, "head",
                  fuse=False)

    # the mouth: a dark line round the muzzle's front, a small white fang at each corner
    k.decal((0, -0.207, 0.24), unit(0, -1, -0.1), (0.03, 0.004), INK, eps=0.003, rings=4, segs=40)
    for s in (1, -1):
        k.decal(np.array((0.04 * s, -0.185, 0.232)), unit(0.75 * s, -0.65, -0.05), (0.016, 0.0035), INK, eps=0.003,
                rings=4, segs=32, up=(0, 0, 1))
        k.add("fang" + ("R" if s > 0 else "L"), claw((0.033 * s, -0.19, 0.236), unit(0, -0.2, -1), 0.012, 0.012,
                                                      up=(0, -1, 0)), "#F6F6F2", "head", fuse=False, surface="shell")

    # --- the eyes: big, on the head's sides, cut flat by the black brow -------------------------------------------
    de = unit(0.66, -0.66, 0.12)
    E = on_ell(H, HR, de)
    cut = brow_cut(0.55, -0.12)
    eyes = eye_pair(k, E, de, (0.04, 0.036), line_eye(0.55, -0.12), srgb(GREY), outline=cut, line=0.16, eps=0.005)

    k.build(voxel=0.0042, smooth=4, band=0.004)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    jangmoo(*args())
