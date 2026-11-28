# Harciogre (Carpathia No. 10014) sculpted from nothing on the skeleton and clips of the game's own export (sculptkit.py):
#   blender --background --factory-startup --python sculpts/harciogre.py -- <the export's Harciogre.glb> <out.glb>
# From its picture (docs/art/reference/gemini/carpathia/10014-Harciogre.jpg): Harciog grown big and round and grumpy,
# a brighter orange over the same pure black belly, its white cheeks bulging with hoarded seed; small eyes under a
# scowl, a frown under the pink nose; its forearms grown heavy and hanging at its sides, caked in earth to the elbow,
# each paw a spade of four long grey claws; on its back a woven sack of grain tied at the neck and open at the top,
# held by a strap of roots crossing over its chest. The sack is carried on its right (+X) shoulder, the side the
# game's front camera sees.
#
# Grown from harciog.py (sibling("harciog")): its frame times 1.5, cheeks, muzzle, nose, ears, eyes and belly.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, ring_band, srgb, inside, unit, args, sibling

h = sibling("harciog")
g = sibling("pidgey")            # spike

K = 1.5
ORANGE, BROW, MUD, CLAW = "#DD7233", "#A9522A", "#7A6650", "#8E8A84"
BURLAP, ROPE, GRAIN, ROOT = "#B49866", "#6E4E30", "#D9BE7C", "#6B4A2E"
WHITE = "#EFEFF1"


def arms(k):
    """Heavy forearms hanging at the sides, caked in earth to the elbow, each paw a spade of four long claws."""
    k.add("arm", tube([(0.205, -0.06, 0.475), (0.262, -0.1, 0.36), (0.268, -0.16, 0.25)], [0.068, 0.062, 0.066]),
          ORANGE, "arm{s}", mirror=True)
    k.add("hand", ell((0.268, -0.19, 0.2), (0.072, 0.066, 0.074)), ORANGE, "arm{s}", mirror=True)
    k.paint(lambda P: inside(P[:, 2] - (0.33 + 0.028 * np.sin(P[:, 0] * 61) * np.sin(P[:, 1] * 47 + 1)
                                       + 0.018 * np.sin(P[:, 2] * 90)), 0.006), MUD, on=["arm", "hand"])
    for j, dx in enumerate((-0.045, -0.015, 0.015, 0.045)):
        base = np.array([0.268 + dx, -0.235, 0.17 - abs(dx) * 0.4])
        tip = base + unit(dx * 1.4, -0.75, -1.0) * (0.11 - abs(dx) * 0.4)
        k.add(f"claw{j}", g.spike(base, tip, 0.032, 0.028, side=(1, 0, 0)), CLAW, "arm{s}", fuse=False,
              mirror=True, surface="shell")


def sack(k):
    """The woven sack of grain on its back, over the right shoulder, tied at the neck and open at the top."""
    k.add("sack", ell((0.1, 0.2, 0.56), (0.15, 0.125, 0.2), rot=(0, -16, 0)), BURLAP, "body", surface="cloth")
    k.add("sackneck", ell((0.148, 0.2, 0.77), (0.078, 0.072, 0.065), rot=(0, -16, 0)), BURLAP, "body",
          surface="cloth")
    # darker bands of the weave, broad enough to read
    k.paint(lambda P: inside(np.abs(np.sin((P[:, 2] - 0.02 * P[:, 0]) * 38)) - 0.22, 0.05)
            * inside(P[:, 2] - 0.72, 0.01), "#9C8156", on=["sack"])
    axis = unit(-math.sin(math.radians(16)), 0, math.cos(math.radians(16)))
    k.add("tie", ring_band((0.138, 0.2, 0.73), axis, 0.068, 0.03, thick=0.02), ROPE, "body", fuse=False)
    k.add("grain", ell((0.16, 0.2, 0.818), (0.06, 0.055, 0.022), rot=(0, -16, 0)), GRAIN, "body", fuse=False)


def straps(k, F):
    """The strap of roots: two ropes crossing over the chest under the cheeks, each going up over a shoulder."""
    (bc, br) = F["BODY"]
    bc, br = np.array(bc), np.array(br)
    for s in (1, -1):
        pts = []
        for phi, z in ((-118, 0.6), (-80, 0.53), (-45, 0.45), (-15, 0.39), (15, 0.33), (45, 0.28), (66, 0.26)):
            a = math.radians(phi) * s
            q = math.sqrt(max(0.05, 1 - ((z - bc[2]) / br[2]) ** 2))
            r = np.array([br[0] * q * math.sin(a), -br[1] * q * math.cos(a)])
            n = r / np.linalg.norm(r)
            pts.append((r[0] + n[0] * 0.016, r[1] + n[1] * 0.016 + bc[1], z))
        k.add(f"strap{'R' if s > 0 else 'L'}", tube(pts, [0.015] * len(pts)), ROOT, "body", fuse=False)


def harciogre(src, out_path):
    F = h.frame(K)
    k = Kit(src, coat="fur")
    h.body(k, F, K, orange=ORANGE, white=WHITE)
    arms(k)
    sack(k)
    h.belly(k, K)
    h.ear_linings(k, F)
    h.nose(k, F, K)
    h.mouth(k, F, K, frown=True, ground=WHITE)
    straps(k, F)
    # the scowl: a heavy orange brow over each eye, sloping down to the nose
    c, r = F["HEAD"]
    D = unit(0.38, -0.78, 0.34)
    E = np.array(c) + D / np.linalg.norm(D / np.array(r))
    for s in (1, -1):
        e = E * np.array([s, 1, 1])
        k.add(f"brow{'R' if s > 0 else 'L'}", ell(e + np.array([-0.012 * s, -0.008, 0.056]), (0.036, 0.019, 0.014),
                                                   rot=(10, 0, 22 * -s)),
              BROW, "head")
    k.build(voxel=0.0085, band=0.006, wband=0.07)
    # the eyes, their top edge sloping down toward the nose under the scowl (u toward the back)
    angry = [(1.0, 0.75), (-1.0, -0.05), (-0.95, -0.45), (-0.6, -0.85), (0.0, -1.0), (0.6, -0.85), (0.95, -0.4)]
    k.eye_states(h.eyes(k, F, K, lid=ORANGE, half=(0.023, 0.026), outline=angry))
    k.export(out_path)


if __name__ == "__main__":
    harciogre(*args())
