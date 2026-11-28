# Coltarina (No. 10079, Carpathia) sculpted from nothing on the skeleton and clips of the game's own export of it:
#   blender --background --factory-startup --python sculpts/coltarina.py -- <the export's Coltarina.glb> <out.glb>
#
# Floricolt grown (sibling("floricolt")): the same felted star of white petals round a woolly yellow disc, the same
# palette, the same dark eye with its glints. From its picture (docs/art/reference/gemini/carpathia/10079-Coltarina.jpg):
# a graceful edelweiss spirit, its head a great star of seven petals with seven smaller between; calm almond eyes under
# a dark upper lid and a small smile; behind and over the head a halo of a thin sage stalk set with a dozen little
# edelweiss stars; a slender woolly sage torso with white tufts of wool round its shoulders, its gown a skirt of long
# feathery sage fronds falling over a crag of pale limestone that is its lower body; two sleeves of hanging fronds for
# arms, carried forward and down; two stones at the crag's foot. Its back: the fronds, the crag and the halo all round.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, inside, unit, eye_look, polygon, outward, sibling, args

fl = sibling("floricolt")

ROCK, ROCK_SHADE, ROCK_DARK = "#CBCAC3", "#AEADA6", "#96958E"
GOWN, GOWN_DARK, GOWN_LIGHT, WOOL = "#8FA594", "#7A9180", "#A9BCAC", "#F2F2EC"
LID = "#3A2A22"


def rock(k, name, c, size, seed, bone, color=ROCK):
    """A block of limestone: a flat-faced hull of jittered corners round c."""
    rs = np.random.RandomState(seed)
    pts = []
    for i in range(14):
        d = rs.normal(size=3); d /= np.linalg.norm(d)
        pts.append(np.array(c) + d * np.array(size) * (0.85 + 0.3 * rs.rand()))
    k.add(name, hull(pts), color, bone, fuse=False, surface="shell")


def almond_eye(u, v):
    """Floricolt's dark eye drawn long: an almond under a heavy dark upper lid, two glints."""
    c = np.tile(srgb(fl.INK), (len(u), 1))
    c[((u + 0.2) / 0.32) ** 2 + ((v - 0.08) / 0.34) ** 2 < 1] = srgb(fl.GLINT)
    c[((u - 0.3) / 0.13) ** 2 + ((v + 0.38) / 0.13) ** 2 < 1] = srgb(fl.GLINT)
    c[v > 0.55 - 0.15 * u * u] = srgb(LID)
    return c


ALMOND = polygon([(-1.0, 0.1), (-0.7, -0.6), (0.0, -0.85), (0.7, -0.55), (1.0, 0.25), (0.6, 0.8), (-0.2, 0.9),
                  (-0.75, 0.6)])


def coltarina(src, out):
    k = Kit(src, coat="fur")
    FC = np.array([0.0, -0.03, 0.925])
    c, n, up, right = fl.star_flower(k, "", FC, (0, -1, 0.1), "head", big=7, L=0.205, W=0.058, T=0.017,
                                     small_L=0.155, small_W=0.044, disc=(0.105, 0.055), turn=90.0)
    # --- the halo of little stars behind and over the head ---
    RC, RR = np.array([0.0, 0.03, 0.985]), 0.235
    angs = np.radians(np.linspace(-28, 208, 40))
    arc = [RC + RR * np.array([math.cos(a), 0.0, math.sin(a)]) for a in angs]
    k.add("halo", tube(arc, [0.017] * len(arc), seg=12, per=4), fl.STEM, "head", surface="leaf")
    for s in (1, -1):        # the halo's two ends held in the back of the flower
        k.add(f"haloroot{'R' if s > 0 else 'L'}", tube([arc[0] * np.array([s, 1, 1]) if s > 0 else arc[-1],
                                                        (0.09 * s, 0.03, 0.89)], [0.017, 0.02]), fl.STEM, "head",
              surface="leaf")
    for j, a in enumerate(np.radians(np.linspace(-14, 194, 12))):
        p = RC + RR * np.array([math.cos(a), -0.0, math.sin(a)]) + (0, -0.018, 0)
        fl.star_flower(k, f"star{j}", p, (0, -1, 0.05), "head", big=3, L=0.05, W=0.021, T=0.009, small_L=0.046,
                       small_W=0.019, disc=(0.016, 0.012), turn=90 + 17 * j, fused_disc=False, back=False)
        # each little star is white from behind too: its back a woolly bud
        k.add(f"starback{j}", outward(ell(p + (0, 0.03, 0), (0.024, 0.014, 0.024), sub=3)), WOOL, "head", fuse=False,
              surface="cloth")
    # --- the torso, the wool at its shoulders, the neck ---
    k.add("neck", tube([(0, 0.0, 0.84), (0, 0.0, 0.76)], [0.04, 0.045]), GOWN, "head")
    k.add("torso", ell((0, 0.0, 0.66), (0.09, 0.075, 0.13)), GOWN, "body")
    k.add("skirt", ell((0, 0.01, 0.48), (0.17, 0.15, 0.18)), GOWN, "body")
    for j, a in enumerate(np.radians([-150, -120, -90, -60, -30, 0, 30, 60, 90, 120, 150, 180])):
        p = np.array([0.095 * math.sin(a), -0.085 * math.cos(a), 0.745 - 0.02 * abs(math.sin(a))])
        k.add(f"wool{j}", ell(p, (0.02, 0.02, 0.018), sub=3), WOOL, "body", surface="fur")
    k.paint(lambda P: inside(np.abs(P[:, 0]) - 0.022, 0.006) * (P[:, 1] < -0.05) * inside(P[:, 2] - 0.72, 0.01)
            * inside(0.55 - P[:, 2], 0.01), GOWN_LIGHT, on=["torso", "skirt"])
    # --- the gown: long fronds round the body falling over the crag ---
    for j in range(14):
        a = math.radians(-180 + 360 * j / 14 + 8)
        o = np.array([math.sin(a), -math.cos(a), 0.0])
        base = o * 0.07 + (0, 0, 0.66 - 0.02 * (j % 2))
        tip = o * (0.2 + 0.025 * (j % 3)) * np.array([1.0, 1.15, 1]) + (0, 0, 0.25 + 0.04 * ((j + 1) % 3))
        d = tip - base
        k.add(f"frond{j}", fl.petal(base, d, o + (0, 0, 0.3), np.linalg.norm(d), 0.058, 0.016, cup=-0.05),
              GOWN if j % 2 else GOWN_DARK, "body", fuse=False, surface="leaf")
    # the gown's back: fronds from the shoulders down over the back, so the back camera sees a gown, not a bare egg
    for j, x in enumerate((-0.07, -0.025, 0.025, 0.07)):
        base = np.array([x, 0.055, 0.75])
        tip = np.array([x * 2.2, 0.2, 0.42 - 0.03 * (j % 2)])
        k.add(f"backfrond{j}", fl.petal(base, tip - base, unit(x * 3, 1, 0.25), np.linalg.norm(tip - base), 0.05,
                                        0.016, cup=-0.04), GOWN_DARK if j % 2 else GOWN, "body", fuse=False,
              surface="leaf")
    # --- the sleeves: an arm under hanging fronds, forward and down ---
    for s, sd in ((1, "R"), (-1, "L")):
        arm = f"arm{sd}"
        k.add(f"armtube{sd}", tube([(0.07 * s, 0.0, 0.7), (0.15 * s, -0.04, 0.6), (0.2 * s, -0.09, 0.5)],
                                   [0.036, 0.032, 0.028]), GOWN, arm)
        for i, (dx, dy, dz, ln) in enumerate(((0.62, -0.35, -0.7, 0.34), (0.8, -0.15, -0.6, 0.31),
                                              (0.5, -0.5, -0.72, 0.28), (0.8, 0.12, -0.62, 0.26))):
            base = np.array([0.13 * s, -0.04 - 0.01 * i, 0.68 - 0.01 * i])
            d = unit(dx * s, dy, dz)
            k.add(f"sleeve{sd}{i}", fl.petal(base, d, unit(0.3 * s, -1, 0.2), ln, 0.062 - 0.005 * i, 0.017, cup=0.04),
                  GOWN if i % 2 == 0 else GOWN_DARK, arm, fuse=False, surface="leaf")
    # --- the crag of limestone ---
    rock(k, "crag", (0.0, 0.01, 0.2), (0.25, 0.19, 0.21), 3, "body")
    rock(k, "cragR", (0.15, -0.06, 0.13), (0.14, 0.12, 0.13), 7, "legR", ROCK_SHADE)
    rock(k, "cragL", (-0.15, -0.02, 0.12), (0.13, 0.13, 0.12), 11, "legL")
    rock(k, "cragtop", (-0.05, -0.08, 0.33), (0.11, 0.09, 0.1), 5, "body", ROCK_SHADE)
    rock(k, "pebbleL", (-0.03, -0.22, 0.03), (0.045, 0.04, 0.03), 13, "legL", ROCK_DARK)
    rock(k, "pebbleR", (0.24, -0.15, 0.04), (0.06, 0.05, 0.04), 17, "legR")

    # --- the face: a small smile on the disc ---
    front = c + n * 0.055 * 1.25
    def on_disc(x, z):
        return front + right * x + up * z - n * (0.055 * (x * x + z * z) / 0.105 ** 2) * 0.9
    def smile(u, v):
        col = np.tile(srgb(fl.DISC), (len(u), 1))
        col[np.abs(v - (0.5 - 0.9 * (1 - u * u))) < 0.3] = srgb(fl.MOUTH)
        return col
    k.decal(on_disc(0, -0.038), n, (0.02, 0.011), smile, eps=0.002, rings=10, segs=48)
    k.build(voxel=0.0085, smooth=4, band=0.006, wband=0.05)

    EYE_P, EYE_N = on_disc(0.046, 0.006), n + right * 0.35
    def eyes(state):
        look = almond_eye if state == "Open" else eye_look(state, almond_eye, srgb(fl.DISC), srgb(fl.INK), line=0.2)
        k.decal(EYE_P, EYE_N, (0.027, 0.022), look, eps=0.0025, outline=ALMOND if state in ("Open", "Fierce") else None,
                rings=20, segs=56, mirror=True)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    coltarina(*args())
