# Mega Inteleon (a Mega of our own, which no game has) sculpted from nothing on the skeleton and clips of the game's
# own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/inteleonmega.py -- <the export's Inteleon-Mega.glb> <out.glb>
#
# No pictures: the design is the game's own sculptor (PokemonModels.Ours.Megas.cs, InteleonMega) and the look of its
# export. Inteleon as inteleon.py builds it (copied here, since that script builds in one function; its crest and claw
# are called from it), with what the Mega changes, largest first:
# - its yellow crest swept back longer and broader into a hood, flaring at its sides in two paler lobes;
# - the tails of a coat, dark blue, falling from its waist to its knees and turned out to the sides (in place of the
#   yellow tails from the shoulders), a teal band round the waist where they hang from;
# - a scope over its right eye: the eye goes, a dark ring of metal and a pale lens lit from within take its place.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, hull, lathe, polygon, srgb, inside, unit, eye_look, args, sibling

sb = sibling("sobble")
it = sibling("inteleon")
LIGHT, NAVY, BLACK, SHIRT, YELLOW, GOLD = it.LIGHT, it.NAVY, it.BLACK, it.SHIRT, it.YELLOW, it.GOLD
INK, WHITE, VOXEL = it.INK, it.WHITE, it.VOXEL
COAT, TEAL, FLARE, SCOPE, LENS = "#243C78", "#28A0A0", "#E8E678", "#282C40", "#C8F6FA"
H, HR, SNOUT_C, SNOUT_R = it.H, it.HR, it.SNOUT_C, it.SNOUT_R


def flare(k, s):
    """A paler lobe of the hood flaring back and out from the side of the crown, its face to the front and back."""
    out = [(-0.024, 0.0), (0.024, 0.0), (0.04, 0.06), (0.038, 0.12), (0.02, 0.17), (0.0, 0.19), (-0.018, 0.13),
           (-0.026, 0.06)]
    origin = H + np.array([0.072 * s, 0.035, 0.045])
    ez = unit(0.85 * s, 0.4, 0.42)                  # along it: out, back and up
    ex = unit(0.45 * s, -0.85, 0.1)                 # across it
    if s < 0:
        out = [(-a, b) for a, b in out]
    k.add("flare" + ("R" if s > 0 else "L"), slab(out, origin, ex, ez, 0.03), FLARE, "head", fuse=False)


def inteleon_mega(src, out):
    k = Kit(src, coat="scales")

    # --- the trunk: slim, a suit; the hips and legs black ------------------------------------------------------------
    k.add("hips", ell((0, 0.008, 0.5), (0.074, 0.062, 0.07)), BLACK, "body")
    k.add("torso", ell((0, -0.005, 0.615), (0.068, 0.056, 0.11)), LIGHT, "body")
    k.add("chest", ell((0, -0.012, 0.66), (0.075, 0.058, 0.055)), LIGHT, "body")
    k.add("neck", tube([(0, -0.005, 0.68), (0, -0.015, 0.73), (0, -0.03, 0.775)], [0.036, 0.032, 0.032]), LIGHT, "head")
    k.paint(lambda P: inside(np.abs(P[:, 0]) - 0.052, 0.002) * inside(P[:, 1] + 0.02, 0.003)
            * inside(P[:, 2] - 0.71, 0.003) * inside(0.545 - P[:, 2], 0.003), NAVY, on=["torso", "chest"])
    k.paint(lambda P: inside(0.555 - P[:, 2], 0.003) * inside(P[:, 2] - 0.62, 0.003), BLACK, on=["torso"])
    k.paint(lambda P: inside(np.abs(P[:, 0]) - 0.02, 0.002) * inside(P[:, 1] + 0.03, 0.003)
            * inside(P[:, 2] - 0.6, 0.003) * inside(0.475 - P[:, 2], 0.003), NAVY, on=["hips", "torso"])
    # the teal band round the waist, where the coat's tails hang from
    k.paint(lambda P: inside(P[:, 2] - 0.566, 0.002) * inside(0.54 - P[:, 2], 0.002), TEAL, on=["hips", "torso"])
    k.decal((0, -0.068, 0.645), unit(0, -1, 0.15), (0.034, 0.05), SHIRT, eps=0.003, rings=24, segs=60,
            outline=polygon([(-1, 0.9), (1, 0.9), (0.0, -1.0)]))

    # --- the head: a lizard's, a pointed snout; a dark blue hood over the crown ---------------------------------------
    k.add("head", ell(H, HR), LIGHT, "head")
    def taper(V):
        V = V.copy()
        t = np.clip(-V[:, 1], 0, 1)
        V[:, 0] *= 1 - 0.45 * t ** 1.5
        V[:, 2] *= 1 - 0.35 * t ** 1.5
        return V
    k.add("snout", ell(SNOUT_C, SNOUT_R, deform=taper), LIGHT, "head")
    k.add("jaw", ell(H + (0, -0.045, -0.046), (0.068, 0.09, 0.046)), LIGHT, "head")
    k.add("brow", ell(H + (0.035, -0.035, 0.03), (0.03, 0.04, 0.02), rot=(0, 0, -15)), LIGHT, "head", mirror=True)
    k.paint(lambda P: inside(H[2] + 0.018 - 0.25 * np.clip(P[:, 1] - H[1], 0, 1) - P[:, 2], 0.002)
            * inside(H[1] - 0.06 - P[:, 1], 0.004)
            * inside(np.linalg.norm((P - H) / (HR + 0.04), axis=1) - 1, 0.004), NAVY,
            on=["head", "brow", "snout", "neck"])
    # The hood: Inteleon's crest, larger and laid further back, and a paler lobe flaring from each side
    it.crest(k, H + (-0.006, 0.035, HR[2] - 0.026), math.radians(55), 1.5)
    for s in (1, -1):
        flare(k, s)

    # --- the coat's tails: dark blue plates from the waist to the knees, turned out to the sides ----------------------
    for s in (1, -1):
        ex = unit(0.8 * s, 0.6, 0)            # across the plate: out and back
        ez = unit(0.16 * s, 0.3, -1.0)        # down it
        tail_out = [(-0.04, 0.0), (0.045, 0.0), (0.06, 0.09), (0.055, 0.19), (0.032, 0.27), (0.0, 0.3),
                    (-0.022, 0.22), (-0.034, 0.1)]
        k.add(f"coat{'R' if s > 0 else 'L'}", slab(tail_out, (0.052 * s, 0.052, 0.56), ex, ez, 0.03), COAT, "body",
              fuse=False, surface="cloth")

    # --- arms: light blue to the elbow, black forearms and long black fingers, forward and down ---------------------
    SH, EL, WR = np.array((0.058, -0.0, 0.69)), np.array((0.1, -0.035, 0.6)), np.array((0.07, -0.125, 0.56))
    k.add("upper", tube([SH, (SH + EL) / 2, EL], [0.026, 0.023, 0.022]), LIGHT, "arm{s}", mirror=True)
    k.add("fore", tube([EL, (EL + WR) / 2, WR], [0.022, 0.021, 0.019]), BLACK, "arm{s}", mirror=True)
    k.add("hand", ell(WR + (0, -0.02, -0.006), (0.024, 0.026, 0.016), rot=(20, 0, 0)), BLACK, "arm{s}", mirror=True)
    for j, (dx, ln) in enumerate([(0.016, 0.055), (0.0, 0.065), (-0.016, 0.055)]):
        b = WR + (dx, -0.036, -0.01)
        e = b + unit(dx * 4, -0.7, -0.75) * ln
        k.add(f"finger{j}", tube([b, (b + e) / 2 + (0, -0.006, 0), e], [0.015, 0.0145, 0.013]), BLACK, "arm{s}",
              mirror=True)
    k.add("thumb", tube([WR + (-0.016, -0.02, 0.0), WR + (-0.034, -0.045, -0.012)], [0.015, 0.0135]), BLACK, "arm{s}",
          mirror=True)

    # --- legs: haunches fused into the hips, long and tapering, black with a light blue stripe behind ---------------
    k.add("thigh", ell((0.058, -0.0, 0.405), (0.054, 0.058, 0.125)), BLACK, "leg{s}", mirror=True)
    k.add("shin", tube([(0.064, -0.012, 0.3), (0.068, -0.02, 0.2), (0.066, -0.005, 0.07), (0.066, 0.0, 0.035)],
                       [0.04, 0.034, 0.027, 0.025]), BLACK, "leg{s}", mirror=True)
    k.add("foot", ell((0.07, -0.042, 0.02), (0.034, 0.064, 0.02)), BLACK, "leg{s}", mirror=True)
    for j, d in enumerate([(0.4, -1, -0.15), (0.0, -1, -0.15), (-0.35, -1, -0.15)]):
        k.add(f"claw{j}", it.claw((0.07, -0.09, 0.016), d, 0.04, 0.02), BLACK, "leg{s}", fuse=False, mirror=True)
    k.add("heel", it.claw((0.07, 0.01, 0.016), (0.1, 1, -0.2), 0.03, 0.018), BLACK, "leg{s}", fuse=False, mirror=True)
    def back_of_leg(off):
        def m(P):
            yc = np.interp(P[:, 2], [0.03, 0.2, 0.3, 0.45], [0.0, -0.02, -0.012, 0.0])
            return inside(yc + off - P[:, 1], 0.002) * inside(np.abs(np.abs(P[:, 0]) - 0.066) - 0.03, 0.003) * inside(P[:, 2] - 0.47, 0.004)
        return m
    k.paint(back_of_leg(0.012), NAVY, on=["thigh", "shin", "hips"])
    k.paint(back_of_leg(0.022), LIGHT, on=["thigh", "shin", "hips"])

    # --- the tail: long and light blue, back and out to +X, down to the ground and up into a hook ------------------
    d = unit(0.5, 0.86, 0)
    root = np.array((0.0, 0.05, 0.46))
    path = [(0.0, 0.46), (0.07, 0.34), (0.15, 0.18), (0.23, 0.07), (0.32, 0.06), (0.39, 0.14), (0.42, 0.27),
            (0.41, 0.38), (0.37, 0.43), (0.33, 0.41), (0.34, 0.37)]
    pts = [root * np.array([1, 1, 0]) + d * b + np.array([0, 0, z]) for b, z in path]
    k.add("tail", tube(pts, list(np.linspace(0.034, 0.015, len(pts))), seg=18, per=6), LIGHT, "tail")

    # --- the scope over the right eye (+X): a dark ring of metal round a pale lens lit from within ------------------
    de = unit(0.78, -0.55, 0.3)
    E = sb.on_ell(H, HR, de)
    En = sb.ell_normal(H, HR, E)
    k.add("scopering", lathe(E - En * 0.004, En, [(0.022, 0.0), (0.035, 0.0), (0.037, 0.008), (0.035, 0.016),
                                                  (0.022, 0.016), (0.02, 0.008), (0.022, 0.0)], seg=40),
          SCOPE, "head", fuse=False, surface="metal")
    k.add("scopelens", lathe(E - En * 0.004, En, [(0, 0.0), (0.0225, 0.0), (0.0225, 0.008), (0, 0.012)], seg=40),
          LENS, "head", fuse=False, glow="#A8F0F8")
    # its arm back to the side of the head
    k.add("scopearm", tube([E + En * 0.004 + unit(0.2, 0.9, 0.1) * 0.03, H + np.array([0.09, 0.03, 0.015])],
                           [0.009, 0.008], seg=10, per=4), SCOPE, "head", fuse=False, surface="metal")

    k.build(voxel=VOXEL, smooth=4, band=0.006, wband=0.05)

    # --- the left eye: heavy-lidded, gold, the dark blue hood cutting it -----------------------------------------------
    navy = srgb(NAVY)

    def look(u, v, lid_v=0.18, slope=-0.25):
        c = np.tile(GOLD, (len(u), 1))
        c[np.hypot(u + 0.05, v + 0.1) < 0.3] = INK
        edge = lid_v + slope * u
        c[v > edge] = navy
        c[np.abs(v - edge) < 0.1] = INK
        return c

    m = np.array([-1, 1, 1.0])

    def lay(state):
        if state == "Fierce":
            fn = lambda u, v: look(u, v, lid_v=0.02, slope=0.35)
        else:
            fn = eye_look(state, look, navy, INK, line=0.15)
        k.decal(E * m, En * m, (0.032, 0.026), lambda u, v, fn=fn: fn(-u, v), eps=0.002, rings=20, segs=56)

    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    inteleon_mega(*args())
