# Raboot sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/raboot.py -- <the export's Raboot.glb> <out.glb>
# Grown from scorbunny.py (its ears, tufts, feet, eyes and tail, built by its functions).
#
# From the references (they overrule the brief, which spoke of a grey-black-and-white rabbit with its hands tucked in
# its fur and a red fringe): a pale grey head with a shock of swept-back pointed tufts on its crown and two long grey
# ears that stream back and down past the shoulders like hair; a yellow band across the brow; half-lidded eyes, white
# with a red-orange iris and a black pupil under a straight dark lid; a small yellow nose; a red-orange scarf wrapped
# round the neck and over the mouth, its ends hanging at the front and sides. Red-orange arms like sleeves ending in
# white fists. A charcoal body with a woolly tuft on the chest, charcoal legs, big red-orange feet with a yellow pad
# under each; a little white tail.
# The export's joints: body (0, 0, 0.3), head (0, 0, 0.39), arms (+-0.06, 0, 0.37), legs (+-0.045, 0, 0.24); no ear or
# tail bone. About 0.65 tall.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, polygon, srgb, inside, unit, boxy, lumpy, eye_look, args, sibling

sc = sibling("scorbunny")
GREY, EARGREY, DARK, WOOL = "#D9D9DC", "#CFCFD3", "#46474F", "#55565F"
SCARF, YELLOW, WHITE, PAD = "#E2502A", "#F6CC4E", "#F4F3EF", "#E8C24A"
INK, IRIS, PUPIL = sc.INK3, srgb("#E8501E"), srgb("#1A1214")

H = np.array([0.0, -0.018, 0.498])           # the head's centre, a fifth over the measure
HR = np.array([0.108, 0.097, 0.103])
DE = unit(0.5, -0.85, 0.06)
EYE_HALF = (0.032, 0.0225)
# a half-lidded eye: the top a straight lid sloping a little toward the nose, the bottom round (u outward)
EYE_SHAPE = ([(u, 0.42 + 0.22 * u) for u in np.linspace(1.0, -1.0, 9)]
             + [(u, -0.85 * math.sqrt(max(0.0, 1 - u * u)) + 0.05 * u) for u in np.linspace(-0.95, 0.95, 13)])


def raboot(src, path):
    k = Kit(src, coat="fur")

    # --- the head: round, pale grey ----------------------------------------------------------------------------------
    k.add("head", ell(H, HR, deform=boxy(2.2)), GREY, "head")
    k.add("snout", ell(H + (0, -0.073, -0.022), (0.046, 0.033, 0.033)), GREY, "head")
    k.add("cheek", ell(H + (0.057, -0.04, -0.04), (0.046, 0.044, 0.042)), GREY, "head", mirror=True)

    # --- the yellow band across the brow, round the head -------------------------------------------------------------
    def band(P):
        d = (P - H) / (HR * 1.25)
        near = (d ** 2).sum(1) < 1
        z = P[:, 2] - H[2]
        lo = 0.033 + 0.006 * np.clip(P[:, 1] - H[1], 0, 1) * 10
        return near * inside(lo - z, 0.002) * inside(z - (lo + 0.031), 0.002)
    k.paint(band, YELLOW, on=["head", "snout", "cheek"])

    # --- the shock of tufts on the crown, swept back ------------------------------------------------------------------
    sc.tuft(k, "hair", H + (0.034, 0.012, 0.088), [(0.3, 0.85, 0.45), (0.7, 0.62, 0.25), (0.15, 0.5, 0.85)], [0.098, 0.082, 0.07], 0.038, GREY,
            "head", thick=0.02)
    k.add("hairmid", hull([H + (0.034, -0.012, 0.094), H + (-0.034, -0.012, 0.094), H + (0, -0.03, 0.08),
                           H + (0, 0.012, 0.086), H + (0.018, 0.062, 0.13), H + (-0.018, 0.062, 0.13),
                           H + (0, 0.125, 0.15)]), GREY, "head", fuse=False)

    # --- the ears: broad soft blades from the back of the crown, sweeping back and down behind the shoulders to the
    # middle of the back like long hair, splayed only a little (tips at x +-0.09); their broad faces to the back
    root = H + np.array([0.034, 0.062, 0.062])
    tip = np.array([0.092, 0.17, 0.33])
    m = sc.frame(tip - root, (-0.3, -0.92, 0.25))
    sc.ear(k, root, float(np.linalg.norm(tip - root)), 0.044, 0.015, None, "head", EARGREY, widest=0.5, root=0.6,
           tipr=0.3, m=m)

    # --- the scarf: round the neck and over the mouth, its ends hanging -----------------------------------------------
    k.add("scarf", ell((0, -0.018, 0.41), (0.09, 0.088, 0.042), deform=boxy(2.3)), SCARF, "head")
    k.add("muffle", ell(H + (0, -0.068, -0.07), (0.07, 0.038, 0.034)), SCARF, "head")
    for i, (root, d, L, w) in enumerate([((0.04, -0.07, 0.4), (0.2, -0.3, -0.93), 0.085, 0.026),
                                          ((0.078, -0.03, 0.4), (0.5, -0.1, -0.86), 0.075, 0.024),
                                          ((0.07, 0.04, 0.4), (0.4, 0.45, -0.8), 0.07, 0.022)]):
        root = np.array(root, float); d = unit(*d)
        a = np.cross(d, (0, 0, 1.0)); a /= np.linalg.norm(a)
        n = np.cross(a, d)
        pts = [root + a * w, root - a * w, root + n * 0.012, root - n * 0.012,
               root + d * L * 0.5 + a * w * 0.9, root + d * L * 0.5 - a * w * 0.9, root + d * L + a * w * 0.3,
               root + d * L * 0.9 - a * w * 0.6]
        k.add(f"scarfend{i}", hull(pts), SCARF, "body", fuse=False, mirror=True)

    # --- a charcoal body, a woolly tuft on the chest -------------------------------------------------------------------
    def pear(V):
        V = V.copy(); s = 1 + 0.1 * np.clip(-V[:, 2], 0, 1)
        V[:, 0] *= s; V[:, 1] *= s
        return V
    k.add("torso", ell((0, 0.006, 0.31), (0.062, 0.052, 0.078), deform=pear), DARK, "body")
    k.add("wool", ell((0, -0.04, 0.33), (0.044, 0.026, 0.044), deform=lumpy(4, 3, 0.18)), WOOL, "body")
    k.add("hips", ell((0, 0.008, 0.245), (0.064, 0.052, 0.042)), DARK, "body")
    sc.tail(k, (0, 0.058, 0.25), (0.024, 0.022, 0.022), WHITE, "body")

    # --- red-orange sleeves bent forward, white fists before the chest ------------------------------------------------
    sh, el, wr = np.array([0.062, -0.004, 0.372]), np.array([0.092, -0.016, 0.318]), np.array([0.064, -0.07, 0.296])
    k.add("arm", tube([sh, el, wr], [0.028, 0.025, 0.023]), SCARF, "arm{s}", mirror=True)
    k.add("fist", ell(wr + (0.0, -0.02, -0.002), (0.026, 0.026, 0.026)), WHITE, "arm{s}", mirror=True)
    k.paint(lambda P: inside(P[:, 1] - (wr[1] - 0.004), 0.002) * (np.abs(np.abs(P[:, 0]) - wr[0]) < 0.04)
            * (np.abs(P[:, 2] - wr[2]) < 0.035), WHITE, on=["fist", "arm"])

    # --- charcoal legs, thighs fused into the hips, big red-orange feet --------------------------------------------------
    k.add("thigh", ell((0.046, 0.004, 0.22), (0.048, 0.05, 0.062)), DARK, "leg{s}", mirror=True)
    k.add("leg", tube([(0.048, 0.002, 0.2), (0.052, -0.004, 0.12), (0.054, -0.008, 0.05)], [0.035, 0.03, 0.027]),
          DARK, "leg{s}", mirror=True)
    sc.foot(k, (0.054, -0.008, 0.04), (0.036, 0.066, 0.03), SCARF, SCARF, pad=PAD)
    k.paint(lambda P: inside(P[:, 2] - 0.058, 0.002) * inside(0.04 - P[:, 2], 0.002) * (np.abs(P[:, 0]) > 0.02)
            * (P[:, 1] > -0.03), SCARF, on=["leg", "foot"])

    # --- a small yellow nose over the scarf ----------------------------------------------------------------------------
    NOSE = polygon([(-1, 0.8), (1, 0.8), (0, -1)])
    k.decal(sc.on_ell(H + (0, -0.073, -0.022), (0.046, 0.033, 0.033), (0, -1, 0.15)), unit(0, -1, 0.1),
            (0.008, 0.007), YELLOW, eps=0.002, outline=NOSE, rings=6, segs=24)

    k.build(voxel=0.0055, band=0.005, wband=0.035)

    E = sc.on_ell(H, HR, DE)
    lid = srgb(GREY)

    def look(u, v):
        c = np.tile(srgb("#FAF8F4"), (len(u), 1))
        c[(u + 0.12) ** 2 + (v + 0.05) ** 2 < 0.6 ** 2] = IRIS
        c[((u + 0.12) / 0.24) ** 2 + ((v + 0.05) / 0.34) ** 2 < 1] = PUPIL
        c[(u + 0.3) ** 2 + (v - 0.15) ** 2 < 0.13 ** 2] = sc.GLINT
        top = 0.42 + 0.22 * u
        c[v > top - 0.2] = INK                                   # the dark lid along the top
        return c

    def fierce(u, v):
        c = look(u, v)
        edge = 0.05 + 0.4 * u
        c[v > edge] = lid
        c[np.abs(v - edge) < 0.16] = INK
        return c
    k.eye_states(lambda state: sc.lay_eyes(k, state, E, DE, EYE_HALF, look, lid, INK, outline=EYE_SHAPE,
                                           fierce=fierce))
    k.export(path)


if __name__ == "__main__":
    raboot(*args())
