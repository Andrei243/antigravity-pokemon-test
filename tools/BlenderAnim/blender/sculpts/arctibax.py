# Arctibax sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/arctibax.py -- <the export's Arctibax.glb> <out.glb>
#
# Frigibax grown (frigibax.py: the same yellow eye cut by a brow, the same pale claws and ice, the same dark fin), from
# the references: a slate-blue dragon crouched forward on two legs, its trunk leaning, a thick neck curving up to a
# head held forward. A mask of ice over the head: a pale visor across the brow cutting the top of each yellow eye,
# and the snout and jaw cased in ice ending in a hook at the chin. A great dark rounded fin on the back with tall
# jagged spikes of ice standing up in front of it. Long arms hanging forward to near the ground, three pale claws to
# each hand and an orange V hanging from the back of each wrist. Pale grey patches on the chest and the lower belly,
# heavy thighs, short shins, feet of three pale claws, a long tail out behind.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, slab, srgb, inside, unit, args, sibling

fb = sibling("frigibax")
bg = fb.bg

SLATE = "#6E88A5"
PALE = "#A9B7C6"
ORANGE = "#F39B28"
SPIKE = "#BEDDEE"
ICE, FIN, CLAW = fb.ICE, fb.FIN, fb.CLAW

HC, HR = np.array([0.0, -0.215, 0.47]), np.array([0.082, 0.09, 0.074])     # the skull


def prong(a, b, w0, w1, t):
    """A flat-faced bar from a to b, w0 wide at a and w1 at b, t thick: a hull (one leg of the orange V)."""
    a, b = np.array(a, float), np.array(b, float)
    d = unit(*(b - a))
    side = np.cross(d, (0, 1.0, 0))
    if np.linalg.norm(side) < 1e-3:
        side = np.array([1.0, 0, 0])
    side /= np.linalg.norm(side)
    up = np.cross(side, d)
    P = []
    for c, w in ((a, w0), (b, w1)):
        for s in (1, -1):
            for q in (1, -1):
                P.append(c + up * s * w / 2 + side * q * t / 2)
    return hull(P)


def arctibax(src, out):
    k = Kit(src, coat="scales")

    # --- the trunk leaning forward, the chest up toward a thick neck -------------------------------------------------
    k.add("trunk", ell((0, 0.03, 0.27), (0.11, 0.15, 0.12), rot=(-22, 0, 0)), SLATE, "body")
    k.add("chest", ell((0, -0.09, 0.33), (0.092, 0.085, 0.1)), SLATE, "body")
    k.add("neck", tube([(0, -0.09, 0.34), (0, -0.15, 0.41), (0, -0.19, 0.45)], [0.075, 0.066, 0.06]), SLATE, "head")

    # --- the head held forward, its snout cased in ice ---------------------------------------------------------------
    k.add("skull", ell(HC, HR), SLATE, "head")
    k.add("snout", ell((0, -0.3, 0.446), (0.06, 0.07, 0.052), rot=(12, 0, 0)), ICE, "head", surface="shell")
    k.add("jaw", ell((0, -0.28, 0.405), (0.058, 0.07, 0.035), rot=(18, 0, 0)), ICE, "head", surface="shell")
    # the hook of ice at the chin
    k.add("hook", bg.fang((0, -0.335, 0.41), (0, 0.35, -1), 0.065, 0.06, up=(0, 1, 0)), ICE, "head", fuse=False,
          surface="shell")
    # the visor across the brow: a hard plate of ice, its point forward over the snout, its wings over the eyes
    k.add("visor", hull([(0, -0.375, 0.492), (0, -0.35, 0.465), (0.088, -0.27, 0.522), (-0.088, -0.27, 0.522),
                         (0.09, -0.285, 0.495), (-0.09, -0.285, 0.495), (0.075, -0.15, 0.53), (-0.075, -0.15, 0.53),
                         (0, -0.2, 0.552), (0, -0.27, 0.548), (0.03, -0.22, 0.5), (-0.03, -0.22, 0.5)]),
          ICE, "head", fuse=False, surface="shell")

    # --- the fin: a great dark rounded fan on the back, banked a little to +X, spikes of ice in front of it ----------
    ez = unit(math.sin(math.radians(14)), 0, math.cos(math.radians(14)))
    FN = [(-0.05, 0.33), (-0.04, 0.43), (-0.01, 0.51), (0.04, 0.565), (0.1, 0.585), (0.15, 0.56), (0.185, 0.5),
          (0.195, 0.42), (0.18, 0.33), (0.09, 0.28)]
    k.add("fin", slab(FN, (0, 0, 0), (0, 1, 0), ez, 0.026), FIN, "body", fuse=False, surface="shell")
    k.add("finroot", tube([(0, -0.04, 0.33), (0, 0.06, 0.34), (0, 0.16, 0.32)], [0.03, 0.034, 0.026]), SLATE, "body")
    spikes = [((0, -0.05, 0.38), (0.06, 0.12, 1), 0.25, 0.1), ((0.0, 0.03, 0.42), (0.1, 0.55, 1), 0.17, 0.08),
              ((0.02, -0.07, 0.39), (0.6, -0.35, 0.75), 0.1, 0.06), ((-0.02, -0.07, 0.39), (-0.6, -0.35, 0.75), 0.1, 0.06),
              ((0.0, 0.1, 0.42), (0.25, 0.75, 0.9), 0.11, 0.065)]
    for i, (b, d, ln, w) in enumerate(spikes):
        k.add(f"icespike{i}", fb.shard(b, d, ln, w, w * 0.4), SPIKE, "body", fuse=False, surface="shell")

    # --- the tail: long and level, out behind, its end a little out to +X --------------------------------------------
    k.add("tail", tube([(0, 0.12, 0.26), (0, 0.24, 0.25), (0.03, 0.34, 0.24), (0.06, 0.42, 0.235)],
                       [0.075, 0.052, 0.032, 0.015]), SLATE, "tail")

    # --- the legs: heavy thighs fused into the hips, short shins, feet of three pale claws ---------------------------
    k.add("thigh", ell((0.09, 0.05, 0.2), (0.076, 0.095, 0.1), rot=(-15, 0, 0)), SLATE, "leg{s}", mirror=True)
    k.add("shin", tube([(0.097, 0.085, 0.13), (0.097, 0.075, 0.045)], [0.045, 0.036]), SLATE, "leg{s}", mirror=True)
    k.add("foot", ell((0.097, 0.02, 0.023), (0.044, 0.066, 0.025)), SLATE, "leg{s}", mirror=True)
    for s in (1, -1):
        sd = "R" if s > 0 else "L"
        for j, dx in enumerate((-0.026, 0.0, 0.026)):
            k.add(f"toe{sd}{j}", fb.claw((s * (0.097 + dx), -0.035, 0.016), (s * dx * 4, -1, -0.25), 0.04, 0.026),
                  CLAW, f"leg{sd}", fuse=False, surface="shell")

    # --- the arms: hanging forward and down to near the ground, three pale claws, an orange V at the wrist -----------
    k.add("arm", tube([(0.07, -0.09, 0.355), (0.097, -0.15, 0.26), (0.09, -0.2, 0.145)], [0.034, 0.028, 0.024]),
          SLATE, "arm{s}", mirror=True)
    k.add("hand", ell((0.088, -0.214, 0.118), (0.027, 0.029, 0.028)), SLATE, "arm{s}", mirror=True)
    for s in (1, -1):
        sd = "R" if s > 0 else "L"
        for j, dx in enumerate((-0.016, 0.0, 0.016)):
            k.add(f"hclaw{sd}{j}", fb.claw((s * (0.088 + dx), -0.232, 0.1), (s * dx * 3, -0.55, -0.85), 0.04, 0.02,
                                           up=(0, 1, 0)), CLAW, f"arm{sd}", fuse=False, surface="shell")
        # the orange V: down from the back of the wrist to a point, and up and back again
        W = np.array((s * 0.104, -0.178, 0.15))
        V = np.array((s * 0.118, -0.15, 0.035))
        T = np.array((s * 0.12, -0.075, 0.115))
        k.add(f"vA{sd}", prong(W, V, 0.024, 0.016, 0.019), ORANGE, f"arm{sd}", fuse=False, surface="shell")
        k.add(f"vB{sd}", prong(V + (0, 0.004, 0), T, 0.018, 0.006, 0.019), ORANGE, f"arm{sd}", fuse=False,
              surface="shell")

    # --- pale grey patches on the chest and the lower belly ----------------------------------------------------------
    def patch(c, r):
        return lambda P: inside(np.sqrt((((P - np.array(c)) / np.array(r)) ** 2).sum(1)) - 1, 0.05)
    k.paint(patch((0, -0.15, 0.3), (0.075, 0.05, 0.075)), PALE, on=["chest", "trunk"])
    k.paint(patch((0, -0.04, 0.17), (0.075, 0.1, 0.04)), PALE, on=["trunk"])

    k.build(voxel=0.0055, smooth=3, band=0.005, wband=0.04)

    # --- the eyes: yellow, a black pupil toward the snout, the top cut by the visor ----------------------------------
    cut, look = fb.yellow_eye(BA=0.42, BB=0.42, pupil_c=(-0.22, -0.08), pupil_r=0.5)
    de = unit(0.8, -0.5, 0.2)
    E = bg.on_ell(HC, HR, de)
    eyes = fb.eyes_under_brow(k, E, de, (0.034, 0.032), look, cut, srgb(SLATE), eps=0.003)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    arctibax(*args())
