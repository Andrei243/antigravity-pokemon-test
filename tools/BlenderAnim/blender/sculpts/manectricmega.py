# Manectric-Mega sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/manectricmega.py -- <the export's Manectric-Mega.glb> <out.glb>
#
# Grown from manectric.py (copied: its function takes no arguments that reach what changes; its helpers, colours,
# eye and painted zag through sibling). From the references: the yellow of the head and mane grown into a fan of great
# flat lightning blades, the design's largest mass by far: on each side one sweeping forward along the head past the
# muzzle, two rising up and back high over the body and one lower behind them. The head is carried low and forward,
# yellow on top with a horn rising from the crown and a lower point behind on each side, the muzzle and jaw blue, the
# eyes red. The body blue, the haunches no longer yellow but bristling with blue points; a shaggy blue ruff under the
# throat; long legs, a yellow cuff on each foreleg with a yellow point running back from it; red claws.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, hull, srgb, inside, unit, eye_look, polygon, boxy, sibling, args

e = sibling("electrike")
mn = sibling("manectric")
spike, almond, poly_mask = e.spike, e.almond, e.poly_mask

BLUE, YELLOW, NAVY = mn.BLUE, mn.YELLOW, mn.NAVY
EYEWHITE, IRIS, PUPIL, INK, MARK = mn.EYEWHITE, mn.IRIS, mn.PUPIL, mn.INK, mn.MARK
CLAW = "#E4323C"

HC, HS = np.array((0, -0.19, 0.515)), 1.25      # manectric.py's head centre and scale
OFF = np.array((0.0, -0.05, -0.09))              # the head carried low and forward


def S(p):
    return HC + (np.array(p, float) - HC) * HS + OFF


def bolt_blade(base, d, length, width, thick, normal):
    """A flat lightning blade: broad a third of the way along, its leading edge bulging, pointed at the tip; its
    faces turned to `normal` (a solid hull, thick at the root)."""
    base, d = np.array(base, float), unit(*d)
    n = np.array(normal, float); n = n - d * (n @ d); n /= np.linalg.norm(n)
    a = np.cross(n, d)
    pts = [base + a * width * 0.32, base - a * width * 0.32, base + n * thick / 2, base - n * thick / 2]
    m = base + d * length * 0.33
    pts += [m + a * width * 0.55, m - a * width * 0.3, m + n * thick * 0.35, m - n * thick * 0.35]
    q = base + d * length * 0.66
    pts += [q + a * width * 0.22, q - a * width * 0.12, q + n * thick * 0.2, q - n * thick * 0.2]
    pts.append(base + d * length)
    return hull(pts)


def manectricmega(src, out):
    k = Kit(src, coat="fur")

    # --- trunk ---------------------------------------------------------------------------------------------------
    k.add("chest", ell((0, -0.1, 0.32), (0.095, 0.115, 0.125)), BLUE, "body")
    k.add("middle", ell((0, 0.02, 0.32), (0.085, 0.12, 0.105)), BLUE, "body")
    k.add("rump", ell((0, 0.13, 0.33), (0.092, 0.09, 0.105)), BLUE, "body")

    # --- legs, as manectric.py's, the forepaws' claws red ---------------------------------------------------------
    k.add("shoulder", ell((0.068, -0.115, 0.255), (0.058, 0.07, 0.11)), BLUE, "leg{s}", mirror=True)
    k.add("leg", tube([(0.068, -0.12, 0.26), (0.072, -0.13, 0.15), (0.075, -0.14, 0.04)], [0.056, 0.043, 0.04]),
          BLUE, "leg{s}", mirror=True)
    k.add("paw", ell((0.076, -0.162, 0.023), (0.042, 0.054, 0.025)), BLUE, "leg{s}", mirror=True)
    k.add("cuff", ell((0.075, -0.142, 0.07), (0.054, 0.056, 0.036)), YELLOW, "leg{s}", mirror=True)
    # the yellow point running back from each cuff
    k.add("cuffbolt", spike((0.08, -0.11, 0.075), (0.1, 0.03, 0.05), 0.03, 0.022), YELLOW, "leg{s}", fuse=False,
          mirror=True)
    for i, dx in enumerate((-0.02, 0.0, 0.02)):
        base = np.array((0.076 + dx * 1.2, -0.208, 0.016))
        k.add(f"claw{i}", spike(base, base + np.array((0, -0.03, -0.01)), 0.01, 0.008), CLAW, "leg{s}",
              fuse=False, mirror=True, surface="shell")

    k.add("thigh", ell((0.07, 0.145, 0.27), (0.066, 0.088, 0.118)), BLUE, "hind{s}", mirror=True)
    k.add("hind", tube([(0.07, 0.15, 0.24), (0.072, 0.175, 0.12), (0.072, 0.155, 0.04)], [0.053, 0.04, 0.039]), BLUE,
          "hind{s}", mirror=True)
    k.add("hpaw", ell((0.073, 0.132, 0.023), (0.042, 0.054, 0.025)), BLUE, "hind{s}", mirror=True)
    k.add("hock", spike((0.072, 0.2, 0.12), (0.074, 0.25, 0.09), 0.02, 0.018), BLUE, "hind{s}", fuse=False,
          mirror=True)
    for i, dx in enumerate((-0.016, 0.016)):
        base = np.array((0.073 + dx * 1.2, 0.082, 0.015))
        k.add(f"hclaw{i}", spike(base, base + np.array((0, -0.026, -0.01)), 0.01, 0.008), CLAW, "hind{s}",
              fuse=False, mirror=True, surface="shell")
    # the haunches bristling with blue points, back and out and up
    for i, (a, b, w) in enumerate((((0.085, 0.16, 0.39), (0.12, 0.27, 0.5), 0.03),
                                   ((0.1, 0.19, 0.34), (0.15, 0.3, 0.36), 0.03),
                                   ((0.1, 0.19, 0.28), (0.15, 0.29, 0.24), 0.028),
                                   ((0.09, 0.18, 0.22), (0.12, 0.25, 0.14), 0.024),
                                   ((0.06, 0.12, 0.42), (0.09, 0.2, 0.53), 0.028))):
        k.add(f"bristle{i}", spike(a, b, w, w * 1.1), BLUE, "hind{s}", fuse=False, mirror=True)

    # --- neck and head: manectric.py's head carried lower and forward -----------------------------------------------
    k.add("neck", tube([(0, -0.1, 0.36), S((0, -0.155, 0.44)) * 0.5 + np.array((0, -0.13, 0.4)) * 0.5,
                        S((0, -0.18, 0.5))], [0.088, 0.085, 0.082]), YELLOW, "head")
    k.add("head", ell(S((0, -0.19, 0.515)), HS * np.array((0.088, 0.092, 0.087)), deform=boxy(2.3)), YELLOW, "head")
    def taper(V):
        V = V.copy(); t = np.clip((-V[:, 1] + 1) / 2, 0, 1)
        V[:, 0] *= 1 - 0.3 * t; V[:, 2] *= 1 - 0.2 * t
        return V
    k.add("muzzle", ell(S((0, -0.29, 0.468)), HS * np.array((0.047, 0.08, 0.042)), deform=taper), YELLOW, "head")
    k.add("jaw", ell(S((0, -0.26, 0.43)), HS * np.array((0.045, 0.07, 0.028))), BLUE, "head")
    k.add("nose", ell(S((0, -0.368, 0.474)), HS * np.array((0.017, 0.012, 0.013))), NAVY, "head", fuse=False,
          surface="skin")
    # the horn from the crown, up and a little back, and a lower point behind on each side
    k.add("crest", blade([S(p) for p in ((0, -0.185, 0.53), (0, -0.17, 0.6), (0, -0.145, 0.655), (0, -0.12, 0.7))],
                         [HS * r for r in (0.072, 0.05, 0.026, 0.004)], (1, 0, 0), 0.72), YELLOW, "head")
    k.add("horn", blade([S(p) for p in ((0.04, -0.13, 0.54), (0.06, -0.09, 0.59), (0.08, -0.05, 0.62))],
                        [HS * r for r in (0.04, 0.024, 0.004)], (1, 0, 0), 0.6), YELLOW, "head", mirror=True)

    # --- the ruff under the throat ------------------------------------------------------------------------------
    RUFF = (((0.0, -0.225, 0.33, (0, -0.45, -1), 0.1), (0.05, -0.215, 0.34, (0.5, -0.45, -1), 0.095),
             (0.085, -0.18, 0.36, (0.95, -0.25, -0.6), 0.085), (0.03, -0.235, 0.28, (0.3, -0.55, -1), 0.09),
             (0.075, -0.205, 0.29, (0.75, -0.4, -1), 0.08)))
    for i, (x, y, z, d, ln) in enumerate(RUFF):
        base = np.array((x, y, z))
        k.add(f"ruff{i}", blade([base, base + unit(*d) * ln * 0.5, base + unit(*d) * ln], [0.042, 0.028, 0.003],
                                (0, 1, 0) if x < 0.04 else (1, 0, 0), 0.5), BLUE, "body", mirror=x > 0)
    k.add("ruffmass", ell((0, -0.19, 0.35), (0.095, 0.07, 0.08)), BLUE, "body")

    # --- the tail: manectric.py's blue bolt, a little shorter ---------------------------------------------------
    k.add("tailroot", ell((0, 0.19, 0.35), (0.035, 0.04, 0.04)), BLUE, "tail")
    k.add("tail", spike((0, 0.19, 0.355), (0.01, 0.3, 0.52), 0.03, 0.036), BLUE, "tail", fuse=False)
    k.add("tailspur", spike((0.004, 0.24, 0.43), (0.01, 0.33, 0.4), 0.018, 0.022), BLUE, "tail", fuse=False)

    # --- THE BLADES: the yellow fan, each a mirror pair, faces out and up toward the cameras ----------------------
    BLADES = (
        # root, direction, length, width, thickness, bone
        ((0.09, -0.1, 0.5), (0.3, -0.92, -0.1), 0.4, 0.2, 0.05, "head"),    # forward along the head's side
        ((0.05, -0.08, 0.45), (0.18, 0.5, 0.85), 0.54, 0.26, 0.055, "body"),      # up and back, the highest
        ((0.07, -0.01, 0.44), (0.3, 0.78, 0.55), 0.56, 0.26, 0.055, "body"),      # back and up, the longest
        ((0.08, 0.07, 0.41), (0.42, 0.86, 0.22), 0.4, 0.2, 0.05, "body"),       # back and out, low
    )
    for i, (root, d, ln, w, th, bone) in enumerate(BLADES):
        k.add(f"blade{i}", bolt_blade(root, d, ln, w, th, (1, 0, 0.45)), YELLOW, bone, fuse=False, mirror=True,
              surface="fur")
    # one on the midline behind the crest, leaning back, its face turned 40 degrees to +X
    k.add("bladeM", bolt_blade((0, -0.05, 0.44), (0.05, 0.62, 0.78), 0.44, 0.22, 0.05, (0.77, 0, 0.64)), YELLOW,
          "body", fuse=False, surface="fur")

    # --- the blue under the muzzle with its zag, the black mark behind the eye -------------------------------------
    ZAG = [(-0.4, 0.4), (-0.39, 0.462), (-0.28, 0.462), (-0.236, 0.505), (-0.236, 0.47), (-0.175, 0.485),
           (-0.19, 0.43), (-0.21, 0.4)]
    def zag(P):
        Q = HC + (P - OFF - HC) / HS
        return poly_mask(Q[:, 1], Q[:, 2], ZAG, 0.0025 / HS) * (Q[:, 2] < 0.5) * (Q[:, 1] < -0.15)
    k.paint(zag, BLUE)
    # the mega's whole muzzle is blue up to a line under the eyes, rising toward the back
    def snout(P):
        Q = HC + (P - OFF - HC) / HS
        line = 0.488 + 0.15 * (Q[:, 1] + 0.3)
        return (inside(Q[:, 2] - line, 0.0025) * inside(Q[:, 1] + 0.21, 0.0025) * (np.abs(Q[:, 0]) < 0.13)
                * (Q[:, 2] > 0.37))
    k.paint(snout, BLUE)
    k.decal(S((0.085, -0.16, 0.52)), unit(1, -0.1, 0.05), (0.018, 0.03), MARK, up=unit(0, 0.35, 1), eps=0.003,
            outline=polygon([(0, 1), (0.5, 0.2), (0.85, -0.45), (0.5, -0.9), (0, -1), (-0.5, -0.9), (-0.85, -0.45),
                             (-0.5, 0.2)]), rings=12, segs=48, mirror=True)

    # --- fierce red eyes ------------------------------------------------------------------------------------------
    W, R, P_, I, Y = srgb(EYEWHITE), srgb(IRIS), srgb(PUPIL), srgb(INK), srgb(YELLOW)
    top = lambda u: 0.36 + 0.36 * u
    def look(u, v):
        c = np.tile(W, (len(u), 1))
        c[((u + 0.12) / 0.55) ** 2 + ((v + 0.05) / 0.7) ** 2 < 1] = R
        c[((u + 0.14) / 0.2) ** 2 + ((v + 0.05) / 0.36) ** 2 < 1] = P_
        c[((u + 0.02) / 0.12) ** 2 + ((v - 0.22) / 0.13) ** 2 < 1] = (1, 1, 1)
        c[v > top(u)] = I
        return c
    de = unit(0.64, -0.74, 0.12)
    E = S((0.062, -0.262, 0.528))
    def eyes(state):
        f = e.fierce(look, Y, I, top) if state == "Fierce" else eye_look(state, look, Y, I)
        k.decal(E, de, (0.032, 0.023), f, up=unit(0.3, 0, 1), eps=0.004, outline=almond, rings=20, segs=56,
                mirror=True)

    k.build(voxel=0.0058, band=0.005)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    manectricmega(*args())
