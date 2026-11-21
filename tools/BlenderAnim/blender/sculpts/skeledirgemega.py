# Mega Skeledirge (a Mega of our own, plan 06 · R20b) sculpted on the skeleton and clips of the game's own export of
# the form (sculptkit.py):
#   blender --background --factory-startup --python sculpts/skeledirgemega.py -- <the export's Skeledirge-Mega.glb> <out.glb>
# Grown from skeledirge.py through sibling: the base's own function builds the whole crocodile (its skull, jaws, teeth,
# marks, legs, belly, eyes and the bird on its snout), and a Kit of this script's own changes the parts and paints it
# was given just before the body is fused. What PokemonModels.Ours.Megas.cs's SkeledirgeMega says the Mega changes,
# largest mass first:
#   1. the bird of flame on its head grown into a great crown of fire: a wide orange brim on the skull's top, a yellow
#      heart, ten tongues of flame round the brim leaning out, purple from the waist up and orange at the foot, three
#      tall orange tongues in the middle (the middle one the tallest), and the bird's face at the crown's front (the
#      base's two plumes off the sides of the head go, as the sculptor takes the old flames off the crown);
#   2. its red darkened to maroon;
#   3. the painted skull spread down its back as five ribs and a spine in bone white (the base's grey lattice and its
#      black flank bands go, which the ribs take the place of);
#   4. the wisp at its tail become a flame shaped like the head of a guitar: a lavender paddle with three white pegs on
#      each edge, rising back and out from a white neck at the tail's end (the base's curled white tip goes).
# No pictures of a Mega of our own exist: the export's own sheet is the design.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
import sculptkit
from sculptkit import ell, tube, outward, srgb, inside, unit, args, sibling

SK = sibling("skeledirge")
F = SK.F
MAROON = "#96282C"
BONE = SK.BONE
ORANGE, GLOW_ORANGE = SK.ORANGE, SK.GLOW_ORANGE
GOLD, GLOW_GOLD = SK.GOLD, SK.GLOW_GOLD
PURPLE, GLOW_PURPLE = "#A060E2", "#A060E2"
LAVENDER, GLOW_LAVENDER = "#CEBAFA", "#CEBAFA"
PEG = "#FFFFFF"

BRIM = np.array([0.0, -0.235, 0.43])         # the crown's brim, on the skull's flat top
TAIL_END = np.array([0.17, 0.53, 0.24])      # where the base's tail ends and its curled tip began
TAIL_UP = sculptkit.unit(0.45, 0.55, 0.7)    # the guitar's neck: back, out to +X and up


def along(c, d, across, r, deform=None, sub=3):
    """An ellipsoid laid along d: r is (half width across, half thickness, half length along d)."""
    d = unit(*d)
    a = np.array(across, float); a = a - d * (a @ d); a /= np.linalg.norm(a)
    n = np.cross(d, a)
    V, F_ = ell((0, 0, 0), r, deform=deform, sub=sub)
    return outward((V @ np.stack([a, n, d]) + np.array(c, float), F_))


def tongue(root, tip, w, sub=3):
    """A tongue of flame from its round foot at root to its point at tip."""
    root, tip = np.array(root, float), np.array(tip, float)
    d = tip - root
    ln = np.linalg.norm(d)
    return along((root + tip) / 2, d, np.cross(d, (0, -1.0, 0.0)) + np.array([1e-3, 0, 0]),
                 (w, w * 0.8, ln / 2), deform=SK.teardrop, sub=sub)


def crown(k):
    """The crown of fire on the skull."""
    k.add("brim", outward(ell(BRIM, (0.215, 0.155, 0.026), sub=4)), ORANGE, "head", fuse=False, glow=GLOW_ORANGE)
    k.add("crownheart", outward(ell(BRIM + np.array([0, 0.0, 0.04]), (0.125, 0.092, 0.05), sub=3)), GOLD, "head",
          fuse=False, glow=GLOW_GOLD)
    purple, orange = [], []
    for i in range(10):
        a = i * 2 * math.pi / 10 + math.pi / 10
        out = np.array([math.sin(a), -math.cos(a), 0.0])
        root = BRIM + np.array([out[0] * 0.145, out[1] * 0.105, 0.012])
        tip = root + np.array([out[0] * 0.07, out[1] * 0.05, 0.17 + 0.05 * (i % 2)])
        purple.append(tongue(root + (tip - root) * 0.05, tip, 0.036))
        orange.append(tongue(root - np.array([0, 0, 0.01]), root + (tip - root) * 0.48, 0.04))
    k.add("crownflames", F.join(*purple), PURPLE, "head", fuse=False, glow=GLOW_PURPLE)
    k.add("crownfeet", F.join(*orange), ORANGE, "head", fuse=False, glow=GLOW_ORANGE)
    middle = []
    for x, h in ((-0.05, 0.2), (0.05, 0.2), (0.0, 0.27)):
        root = BRIM + np.array([x, 0.02, 0.04])
        middle.append(tongue(root, root + np.array([x * 0.5, 0.04, h]), 0.045))
    k.add("crownmiddle", F.join(*middle), ORANGE, "head", fuse=False, glow=GLOW_ORANGE)
    cores = []
    for x, h in ((-0.05, 0.12), (0.05, 0.12), (0.0, 0.17)):
        root = BRIM + np.array([x, 0.0, 0.05])
        cores.append(tongue(root + np.array([0, -0.018, 0]), root + np.array([x * 0.4, 0.01, h]), 0.026))
    k.add("crowncore", F.join(*cores), GOLD, "head", fuse=False, glow=GLOW_GOLD)
    # the bird's face at the crown's front: a ball with its dark ringed eye, a beak and two wings of flame
    at = BRIM + np.array([0.0, -0.12, 0.075])
    SK.bird(k, at, 1.45)
    k.add("crownbeak", tongue(at + np.array([0, -0.03, -0.015]), at + np.array([0, -0.075, -0.045]), 0.016), GOLD,
          "head", fuse=False, glow=GLOW_GOLD)
    for s in (1, -1):
        root = at + np.array([0.03 * s, 0.0, 0.01])
        k.add(f"crownwing{'R' if s > 0 else 'L'}", tongue(root, root + np.array([0.085 * s, 0.01, 0.05]), 0.022),
              ORANGE, "head", fuse=False, glow=GLOW_ORANGE)


def guitar(k):
    """The tail's flame shaped like a guitar's head: a white neck, a lavender paddle, three pegs on each edge."""
    d = TAIL_UP
    side = np.cross(d, (0, 0, 1.0)); side /= np.linalg.norm(side)
    neck_end = TAIL_END + d * 0.075
    k.add("tailneck", tube([TAIL_END, TAIL_END + d * 0.04, neck_end], [0.036, 0.03, 0.026]), BONE, "tail")
    mid = neck_end + d * 0.1
    k.add("paddle", along(mid, d, side, (0.046, 0.02, 0.115)), LAVENDER, "tail", fuse=False, glow=GLOW_LAVENDER)
    pegs = []
    for t in (0.05, 0.1, 0.15):
        for s in (1, -1):
            w = 0.046 * math.sqrt(max(0.0, 1 - ((t - 0.1) / 0.115) ** 2))
            pegs.append(outward(ell(neck_end + d * t + side * s * (w + 0.008), (0.014, 0.014, 0.014), sub=2)))
    k.add("pegs", F.join(*pegs), PEG, "tail", fuse=False, glow="#FFFFFF")


class MegaKit(sculptkit.Kit):
    """The base's Kit, changed into the Mega's just before the body is fused (the base's own build call)."""

    def build(self, *a, **kw):
        if not getattr(self, "_mega", False) and kw.get("name") is None:
            self._mega = True
            red, maroon = srgb(SK.RED), srgb(MAROON)
            for p in self.parts:
                if np.allclose(p["color"], red):
                    p["color"] = maroon
            self.parts = [p for p in self.parts if p["name"] != "tailtip"]
            self.paints = [(m, maroon if np.allclose(c, red) else c, on) for m, c, on in self.paints
                           if m.__name__ not in ("lattice", "lattice_lines", "flank")]
            body = ["torso", "chest", "rump", "neck"]

            def ribs(P):
                x, y, z = P[:, 0], P[:, 1], P[:, 2]
                near = np.min(np.abs(y[:, None] - (0.075 - 0.075 * np.arange(-2, 3))[None]), axis=1)
                return (inside(near - 0.017, 0.003) * inside(0.17 - z, 0.004) * inside(np.abs(x) - 0.2, 0.004))
            self.paint(ribs, BONE, on=body)

            def spine(P):
                x, y, z = P[:, 0], P[:, 1], P[:, 2]
                return inside(np.abs(x) - 0.024, 0.003) * inside(0.2 - z, 0.004) * inside(np.abs(y - 0.07) - 0.24, 0.004)
            self.paint(spine, BONE, on=body)
            crown(self)
            guitar(self)
        return super().build(*a, **kw)


SK.Kit = MegaKit
SK.plume = lambda *a, **kw: None             # the plumes off the head's sides go: the crown takes their fire


if __name__ == "__main__":
    SK.skeledirge(*args())
