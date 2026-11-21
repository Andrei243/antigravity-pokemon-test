# Hakamo-o sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/hakamoo.py -- <the export's Hakamo-o.glb> <out.glb>
# Jangmo-o grown up (built on jangmoo.py's frame through sibling): from the references a grey dragon risen on two legs
# in a fighter's crouch. On its crown the great yellow scale now stands upright as a heart facing forward, its two
# lobes at the top, a paler heart in its middle. A smaller grey head with a short muzzle, the line's red eye under a
# heavy black brow, small grey fins of points at the back of the head. Three yellow rings stacked round the neck. A grey
# chest of muscle, yellow-edged grey scales on the shoulders. The upper arms grey, the forearms charcoal with a fringe
# of yellow scales and yellow claws, and on each forearm's outer side a great oval shield of yellow scale with a red
# middle. Great grey haunches over short charcoal shins, charcoal feet with yellow claws. A thick grey tail curving up
# high behind, ending in a round yellow scale.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, lathe, srgb, inside, smoothstep, unit, polygon, eye_look, sibling, args

jm = sibling("jangmoo")
GREY, DARK, YELLOW, PALEYELLOW, RED = jm.GREY, jm.DARK, jm.YELLOW, jm.PALEYELLOW, jm.RED
WHITE_FUR, INK = jm.WHITE_FUR, jm.INK
claw, oriented, scale, on_ell = jm.claw, jm.oriented, jm.scale, jm.on_ell


def torus(c, d, R, r, seg=40, rseg=12):
    prof = [(R + r * math.cos(a), r * math.sin(a)) for a in np.linspace(0, 2 * math.pi, rseg + 1)]
    return lathe(c, d, prof, seg=seg)


def shield(k, name, c, n, d, size, bone, rim=YELLOW, middle=RED):
    """A great oval scale on a forearm: yellow, a red middle lifted over it (loose pieces)."""
    n = unit(*n)
    k.add(name, oriented(c, n, d, (size * 0.62, size, size * 0.2)), rim, bone, fuse=False, surface="shell")
    k.add(name + "mid", oriented(np.array(c) + n * size * 0.08, n, d, (size * 0.34, size * 0.58, size * 0.16)), middle,
          bone, fuse=False, surface="shell")


def hakamoo(src, out):
    k = Kit(src, coat="scales")

    # --- the trunk: a deep chest over a narrower waist, broad hips -------------------------------------------------
    k.add("chest", ell((0, -0.015, 0.42), (0.088, 0.072, 0.078), rot=(-8, 0, 0)), GREY, "body")
    k.add("waist", ell((0, 0.0, 0.33), (0.066, 0.06, 0.07)), GREY, "body")
    k.add("hips", ell((0, 0.01, 0.26), (0.085, 0.075, 0.06)), GREY, "body")
    # the muscles of the belly: two columns of soft bumps
    for j, z in enumerate((0.39, 0.345, 0.3)):
        k.add(f"ab{j}", ell((0.024, -0.062 + 0.004 * j, z), (0.022, 0.012, 0.018)), GREY, "body", mirror=True)
    k.add("neck", tube([(0, -0.02, 0.45), (0, -0.04, 0.49), (0, -0.05, 0.52)], [0.045, 0.042, 0.042]), GREY, "head")

    # the rings round the neck
    for j, (y, z, R) in enumerate(((-0.03, 0.47, 0.05), (-0.04, 0.445, 0.054), (-0.045, 0.418, 0.058))):
        k.add(f"ring{j}", torus((0, y, z), unit(0, -0.35, 1), R, 0.012), YELLOW, "body" if j else "head", fuse=False,
              surface="shell")

    # --- the legs: great grey haunches fused into the hips, short charcoal shins, charcoal feet, yellow claws -------
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"thigh{side}", ell(np.array((0.095, -0.02, 0.2)) * m, (0.07, 0.085, 0.09), rot=(-15, 0, 0)), GREY,
              f"leg{side}")
        k.add(f"shin{side}", tube(np.array([(0.11, 0.02, 0.13), (0.118, 0.03, 0.07), (0.12, 0.015, 0.03)]) * m,
                                  [0.04, 0.034, 0.032]), DARK, f"leg{side}")
        k.add(f"foot{side}", ell(np.array((0.12, -0.025, 0.022)) * m, (0.04, 0.065, 0.022)), DARK, f"leg{side}")
        for j, dx in enumerate((-0.022, 0.0, 0.022)):
            k.add(f"toe{side}{j}", claw(np.array((0.12 + dx, -0.085, 0.018)) * m, unit(dx * 6 * s, -1, -0.2), 0.036,
                                        0.022), YELLOW, f"leg{side}", fuse=False, surface="shell")
        k.add(f"heel{side}", claw(np.array((0.12, 0.04, 0.02)) * m, unit(0, 1, -0.3), 0.028, 0.02), YELLOW,
              f"leg{side}", fuse=False, surface="shell")
    k.paint(lambda P: inside(P[:, 2] - 0.135, 0.004), DARK, on=["thigh"])

    # --- the arms: grey upper arms, charcoal forearms carried forward and down, a shield on each ------------------
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        sh, el, ha = np.array((0.09, -0.005, 0.44)), np.array((0.125, -0.07, 0.37)), np.array((0.095, -0.18, 0.34))
        k.add(f"shoulder{side}", ell(sh * m, (0.045, 0.045, 0.045)), GREY, f"arm{side}")
        k.add(f"upper{side}", tube([sh * m, el * m], [0.036, 0.034]), GREY, f"arm{side}")
        k.add(f"fore{side}", tube([el * m, ((el + ha) / 2) * m, ha * m], [0.038, 0.04, 0.036]), DARK, f"arm{side}")
        k.add(f"hand{side}", ell((ha + (0, -0.012, -0.004)) * m, (0.036, 0.034, 0.032)), DARK, f"arm{side}")
        for j, (dx, dz) in enumerate(((-0.018, 0.006), (0.0, -0.006), (0.018, 0.004))):
            b = ha + np.array((dx, -0.035, dz))
            k.add(f"claw{side}{j}", claw(b * m, unit(dx * 6 * s, -0.8, -0.6), 0.036, 0.02, up=(0, 0.6, 1)), YELLOW,
                  f"arm{side}", fuse=False, surface="shell")
        # the fringe of yellow scales along the forearm's top
        for j, t in enumerate((0.2, 0.5, 0.8)):
            p = el + (ha - el) * t + np.array((0.0, 0.0, 0.034))
            k.add(f"fringe{side}{j}", claw(p * m, unit(0.3 * s, 0.4, 1), 0.03, 0.03, up=(0, 1, 0), flat=0.4), YELLOW,
                  f"arm{side}", fuse=False, surface="shell")
        # the shield: a great oval on the forearm's outer side, turned forward so the front sees it
        c = (el + ha) / 2 + np.array((0.045, -0.005, -0.005))
        shield(k, f"shield{side}", c * m, unit(0.8 * s, -0.6, 0.0), (0, -0.25, -1), 0.085, f"arm{side}")
        # the shoulder's scale
        scale(k, f"pauldron{side}", (sh + (0.03, 0.0, 0.03)) * m, unit(0.7 * s, -0.1, 0.7), (s * 0.6, 0, -0.8), 0.04,
              f"arm{side}")

    # --- the tail: thick, curving up high behind to a round yellow scale -------------------------------------------
    tp = [(0, 0.06, 0.25), (0, 0.15, 0.22), (0, 0.22, 0.27), (0, 0.25, 0.37), (0, 0.24, 0.46), (0, 0.215, 0.535)]
    k.add("tail", tube(tp, [0.055, 0.045, 0.037, 0.03, 0.022, 0.016]), GREY, "tail")
    k.add("tailtip", oriented((0, 0.205, 0.55), unit(0, -0.55, 0.55), (0, 0, 1), (0.04, 0.05, 0.014)), YELLOW, "tail",
          fuse=False, surface="shell")
    k.add("tailtipin", oriented((0, 0.2, 0.556), unit(0, -0.55, 0.55), (0, 0, 1), (0.024, 0.032, 0.012)), PALEYELLOW,
          "tail", fuse=False, surface="shell")

    # --- the head: smaller than Jangmo-o's against the body, a short muzzle forward --------------------------------
    J, G = np.array([0.0, -0.04, 0.47]), 1.2
    first, firstd = len(k.parts), len(k.decals)
    H, HR = np.array([0.0, -0.07, 0.535]), np.array([0.066, 0.074, 0.066])
    k.add("head", ell(H, HR), GREY, "head")
    k.add("muzzle", ell((0, -0.125, 0.515), (0.045, 0.05, 0.04), rot=(-8, 0, 0)), GREY, "head")
    # the mouth: a dark line round the muzzle's front, a small fang at each corner
    k.decal((0, -0.172, 0.502), unit(0, -1, -0.15), (0.026, 0.0038), INK, eps=0.003, rings=4, segs=40)
    for s in (1, -1):
        k.decal(np.array((0.035 * s, -0.152, 0.497)), unit(0.75 * s, -0.65, -0.05), (0.014, 0.0035), INK, eps=0.003,
                rings=4, segs=32)
        k.add("fang" + ("R" if s > 0 else "L"), claw((0.03 * s, -0.158, 0.5), unit(0, -0.2, -1), 0.011, 0.011,
                                                      up=(0, -1, 0)), "#F6F6F2", "head", fuse=False, surface="shell")
    # the fins at the back of the head: grey points fanned back
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        root = np.array((0.056, -0.03, 0.565)) * m
        for j, (dy, dz, ln) in enumerate(((0.9, 0.5, 0.04), (0.8, 0.9, 0.034), (0.9, 0.0, 0.032))):
            k.add(f"fin{side}{j}", claw(root, unit(0.45 * s, dy, dz), ln, 0.026, up=(s, 0, 0.2), flat=0.4), GREY,
                  "head", fuse=False)

    # the crest: the great yellow heart standing up on the crown, facing forward, a paler heart in its middle
    for s, side in ((1, "R"), (-1, "L")):
        k.add(f"crestlobe{side}", oriented((0.036 * s, -0.045, 0.66), unit(0, -1, 0.25), (s * 0.4, 0, 1),
                                           (0.044, 0.058, 0.02)), YELLOW, "head", fuse=False, surface="shell")
    k.add("crestpoint", hull([(0.0, -0.06, 0.58), (0.05, -0.04, 0.635), (-0.05, -0.04, 0.635), (0.0, -0.025, 0.6),
                              (0.03, -0.035, 0.67), (-0.03, -0.035, 0.67), (0, -0.055, 0.66), (0, -0.02, 0.65)]),
          YELLOW, "head", fuse=False, surface="shell")
    for s, side in ((1, "R"), (-1, "L")):
        k.add(f"crestheart{side}", oriented((0.018 * s, -0.061, 0.655), unit(0, -1, 0.25), (s * 0.5, 0, 1),
                                            (0.018, 0.024, 0.01)), PALEYELLOW, "head", fuse=False, surface="shell")
    k.add("crestheartpt", oriented((0, -0.063, 0.633), unit(0, -1, 0.25), (0, 0, 1), (0.012, 0.022, 0.009)),
          PALEYELLOW, "head", fuse=False, surface="shell")

    # the whole head grown about the neck's top, so the face reads at the game's size
    for p in k.parts[first:]:
        p["V"] = J + (p["V"] - J) * G
    for dd in k.decals[firstd:]:
        dd["c"] = J + (dd["c"] - J) * G
        dd["half"] = tuple(h * G for h in dd["half"])
    # --- the eyes: the line's red eye under the black brow, on the head's sides ----------------------------------
    de = unit(0.62, -0.72, 0.12)
    E = J + (on_ell(H, HR, de) - J) * G
    cut = jm.brow_cut(0.5, -0.15)
    eyes = jm.eye_pair(k, E, de, (0.036, 0.031), jm.line_eye(0.5, -0.15), srgb(GREY), outline=cut, line=0.16,
                       eps=0.005)

    k.build(voxel=0.006, smooth=4, band=0.004)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    hakamoo(*args())
