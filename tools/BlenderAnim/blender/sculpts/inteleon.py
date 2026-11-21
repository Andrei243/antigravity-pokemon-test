# Inteleon sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/inteleon.py -- <the export's Inteleon.glb> <out.glb>
#
# Sobble and Drizzile grown (sobble.py: the line's blues, on_ell, its fin made of two plates, the curled tail). From the
# references: a tall lizard like a secret agent. A long lizard's head with a pointed light blue snout, a dark blue
# hood over the crown and the brow coming down to cut the eyes, heavy-lidded gold eyes; a great yellow crest rising
# back from the crown, notched into points on its +X side with a dark blue stripe up its other edge. A thin light blue
# neck; a slim trunk dressed as a suit: dark blue lapels down the front, a white shirt-front between them, the waist
# and legs black, a light blue stripe down the back of each leg; long yellow tails of a coat hanging from the shoulders
# behind, to the knees, turned out to the sides. Light blue upper arms, black forearms and long black fingers, carried
# forward and down. Black feet with three claws. A long light blue tail back and out to +X, down to the ground and up
# into a hook.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, hull, polygon, srgb, inside, unit, eye_look, args, sibling

sb = sibling("sobble")
LIGHT = "#5CC2EA"
NAVY = "#2656B4"
BLACK = "#2C3138"
SHIRT = "#EEF2F2"
YELLOW = "#EEE070"
GOLD = srgb("#F2C230")
INK, WHITE = sb.INK, sb.WHITE
VOXEL = 0.0075

H = np.array([0.0, -0.045, 0.84])              # the head: a lizard's, long and pointed
HR = np.array([0.096, 0.116, 0.098])
SNOUT_C, SNOUT_R = H + (0, -0.124, -0.034), (0.06, 0.098, 0.058)


def claw(base, d, length, width):
    d = unit(*d)
    a, b = np.cross(d, (0, 0, 1.0)), None
    if np.linalg.norm(a) < 1e-3:
        a = np.array([1.0, 0, 0])
    a = a / np.linalg.norm(a); b = np.cross(a, d)
    base = np.array(base, float)
    return hull([base + a * width / 2, base - a * width / 2, base + b * width / 2, base - b * width / 2,
                 base + d * length * 0.5 + b * width * 0.15, base + d * length - b * width * 0.2])


def crest(k, base, lean, scale):
    """The crest: a yellow plate rising back from the crown, notched into three points on its +X side, a dark blue
    stripe up its other edge; its face to the front, laid back by lean and turned a little to +X."""
    ex = unit(math.cos(0.3), math.sin(0.3), 0)
    ez = unit(0, math.sin(lean), math.cos(lean))
    # three deep rounded lobes on the +X side, each pointing out and up
    plate = [(-0.016, -0.03), (0.02, -0.03), (0.05, -0.01), (0.09, 0.0), (0.112, 0.012), (0.1, 0.026),
             (0.07, 0.036), (0.1, 0.052), (0.124, 0.07), (0.11, 0.084), (0.075, 0.092), (0.095, 0.112),
             (0.112, 0.134), (0.096, 0.148), (0.06, 0.15), (0.035, 0.168), (0.014, 0.162), (0.0, 0.13),
             (-0.01, 0.09), (-0.016, 0.05), (-0.018, 0.01)]
    plate = [(a * scale * (1.25 if a > 0 else 1.0), b * scale) for a, b in plate]
    k.add("crest", slab(plate, base, ex, ez, 0.03), YELLOW, "head", fuse=False)
    # the crest's lower lobe on the other side, behind the -X eye
    lobe = [(0.0, -0.02), (0.0, 0.03), (-0.03, 0.05), (-0.06, 0.06), (-0.048, 0.035), (-0.068, 0.02), (-0.03, -0.005)]
    lobe = [(a * scale, b * scale) for a, b in lobe]
    exl = unit(math.cos(0.5), -math.sin(0.5), 0)
    k.add("crestlobe", slab(lobe, np.array(base, float) + (-0.02, 0.0, -0.02), exl, ez, 0.03), YELLOW, "head",
          fuse=False)
    edge = [(-0.022, -0.03), (-0.006, -0.03), (-0.002, 0.02), (0.002, 0.07), (0.01, 0.12), (0.022, 0.158),
            (0.03, 0.171), (0.012, 0.166), (-0.002, 0.135), (-0.014, 0.095), (-0.021, 0.05), (-0.023, 0.01)]
    edge = [(a * scale, b * scale) for a, b in edge]
    k.add("creststripe", slab(edge, np.array(base, float) + ex * -0.001, ex, ez, 0.03), NAVY, "head", fuse=False)


def inteleon(src, out):
    k = Kit(src, coat="scales")

    # --- the trunk: slim, a suit; the hips and legs black ------------------------------------------------------------
    k.add("hips", ell((0, 0.008, 0.5), (0.074, 0.062, 0.07)), BLACK, "body")
    k.add("torso", ell((0, -0.005, 0.615), (0.068, 0.056, 0.11)), LIGHT, "body")
    k.add("chest", ell((0, -0.012, 0.66), (0.075, 0.058, 0.055)), LIGHT, "body")
    k.add("neck", tube([(0, -0.005, 0.68), (0, -0.015, 0.73), (0, -0.03, 0.775)], [0.036, 0.032, 0.032]), LIGHT, "head")
    # the dark blue lapels down the front, carried on as a stripe down the middle of the black
    k.paint(lambda P: inside(np.abs(P[:, 0]) - 0.052, 0.002) * inside(P[:, 1] + 0.02, 0.003)
            * inside(P[:, 2] - 0.71, 0.003) * inside(0.545 - P[:, 2], 0.003), NAVY, on=["torso", "chest"])
    k.paint(lambda P: inside(0.555 - P[:, 2], 0.003) * inside(P[:, 2] - 0.62, 0.003), BLACK, on=["torso"])
    k.paint(lambda P: inside(np.abs(P[:, 0]) - 0.02, 0.002) * inside(P[:, 1] + 0.03, 0.003)
            * inside(P[:, 2] - 0.6, 0.003) * inside(0.475 - P[:, 2], 0.003), NAVY, on=["hips", "torso"])
    # the white shirt-front between the lapels, a point down
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
    # the hood: dark blue over the crown and the back of the head, down to just over the eyes
    k.paint(lambda P: inside(H[2] + 0.018 - 0.25 * np.clip(P[:, 1] - H[1], 0, 1) - P[:, 2], 0.002)
            * inside(H[1] - 0.06 - P[:, 1], 0.004)
            * inside(np.linalg.norm((P - H) / (HR + 0.04), axis=1) - 1, 0.004), NAVY,
            on=["head", "brow", "snout", "neck"])
    crest(k, H + (-0.006, 0.03, HR[2] - 0.02), math.radians(52), 1.3)

    # --- the coat's tails: long yellow plates from the shoulders behind, to the knees, turned out to the sides -------
    for s in (1, -1):
        ex = unit(0.8 * s, 0.6, 0)            # across the plate: out and back
        ez = unit(0.12 * s, 0.25, -1.0)       # down it
        tail_out = [(-0.035, 0.0), (0.04, 0.0), (0.055, 0.12), (0.045, 0.25), (0.02, 0.36), (-0.01, 0.4),
                    (-0.02, 0.3), (-0.03, 0.15)]
        k.add(f"coat{'R' if s > 0 else 'L'}", slab(tail_out, (0.055 * s, 0.045, 0.69), ex, ez, 0.03), YELLOW, "body",
              fuse=False)

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
        k.add(f"claw{j}", claw((0.07, -0.09, 0.016), d, 0.04, 0.02), BLACK, "leg{s}", fuse=False, mirror=True)
    k.add("heel", claw((0.07, 0.01, 0.016), (0.1, 1, -0.2), 0.03, 0.018), BLACK, "leg{s}", fuse=False, mirror=True)
    # the light blue stripe down the back of each leg, a dark blue edge before it
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

    k.build(voxel=VOXEL, smooth=4, band=0.006, wband=0.05)

    # --- the eyes: heavy-lidded, gold, the dark blue hood cutting them ------------------------------------------------
    navy = srgb(NAVY)

    def look(u, v, lid_v=0.18, slope=-0.25):
        c = np.tile(GOLD, (len(u), 1))
        c[np.hypot(u + 0.05, v + 0.1) < 0.3] = INK
        edge = lid_v + slope * u
        c[v > edge] = navy
        c[np.abs(v - edge) < 0.1] = INK
        return c

    de = unit(0.78, -0.55, 0.3)
    E = sb.on_ell(H, HR, de)
    En = sb.ell_normal(H, HR, E)

    def lay(state):
        if state == "Fierce":
            fn = lambda u, v: look(u, v, lid_v=0.02, slope=0.35)
        else:
            fn = eye_look(state, look, navy, INK, line=0.15)
        k.decal(E, En, (0.032, 0.026), fn, eps=0.002, rings=20, segs=56, mirror=True)

    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    inteleon(*args())
