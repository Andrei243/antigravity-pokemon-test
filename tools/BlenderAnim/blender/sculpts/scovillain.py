# Scovillain sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/scovillain.py -- <the export's Scovillain.glb> <out.glb>
#
# Grown from Capsakid (sibling("capsakid")): its greens, its sly half-moon eyes and its solid leaf points. From the
# references: two lumpy pepper heads on two thin green stalks, the red one on the -X side (headL) and the green one on
# the +X side (headR), facing apart; each gapes with a wide dark mouth over a dropped jaw, an orange tongue inside and
# white fangs at the corners, and glares with half-moon eyes under lumpy brows. The stalks rise from a star of light
# green leaves on a light green chest; arms akimbo, hands on the hips (the skeleton has no arm bones: they go on the
# body); a zigzag line where the chest's light green meets the dark green of a great bulb of a lower body; thick dark
# legs in a wide stance with white toe caps; a small dark spike of a tail behind.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, outward, tri, args, sibling

cap = sibling("capsakid")

RED, HGREEN = "#E04A2B", "#4DB25A"
LIGHT, DARKB, STALK, COLLAR = "#3F9B5B", "#2B7044", "#3C9A57", "#5FB86E"
MOUTH, TONGUE, WHITE, INK = "#4A1E16", "#E8923A", cap.WHITE, cap.INK
VOXEL = 0.0058


def frame(look, up=(0, 0, 1.0)):
    """Columns side, back (-look), up: local -Y is where the head looks (doduo.py's)."""
    L = unit(*look)
    side = np.cross(-L, up); side /= np.linalg.norm(side)
    return np.stack([side, -L, np.cross(side, -L)], axis=1)


def placed(shape, c, M):
    V, F = shape
    return V @ M.T + np.array(c, float), F


class PepperHead:
    """A lumpy pepper of a head at centre, looking along look, gaping over a dropped jaw."""

    UC, UR = np.array([0, 0, 0.04]), np.array([0.112, 0.1, 0.078])      # the upper head, local (times K)
    K = 1.28                                                           # the heads are a third of the height

    def __init__(self, k, tag, bone, centre, look, colour, neck_from):
        self.k, self.tag, self.bone, self.c, self.colour = k, tag, bone, np.array(centre, float), colour
        self.M = M = frame(look)
        c, K = self.c, self.K
        def put(name, shape, col, fuse=True, surface=None):
            V, F = shape
            sh = placed((V * K, F), c, M)
            k.add(f"{name}{tag}", sh if fuse else outward(sh), col, bone, fuse=fuse, surface=surface)
        def at(p):
            return c + M @ (np.array(p, float) * K)
        put("upper", ell(self.UC, self.UR), colour)
        for i, (o, r) in enumerate([((-0.05, 0.015, 0.094), (0.05, 0.05, 0.04)), ((0.05, 0.015, 0.094), (0.05, 0.05, 0.04)),
                                    ((0, 0.045, 0.09), (0.055, 0.045, 0.04)), ((-0.088, 0.0, 0.025), (0.035, 0.07, 0.06)),
                                    ((0.088, 0.0, 0.025), (0.035, 0.07, 0.06))]):
            put(f"lump{i}", ell(o, r), colour)
        for s in (1, -1):        # the brows, lumps over the eyes
            put(f"brow{'R' if s > 0 else 'L'}", ell((0.046 * s, -0.06, 0.086), (0.042, 0.035, 0.024)), colour)
        # a wide gaping mouth: the jaw dropped well below the head, the dark maw set back between
        put("jaw", ell((0, 0.006, -0.104), (0.094, 0.088, 0.036)), colour)
        put("nape", ell((0, 0.04, -0.05), (0.092, 0.07, 0.065)), colour)
        put("maw", ell((0, -0.016, -0.052), (0.08, 0.052, 0.05)), MOUTH)
        for s in (1, -1):        # the cheeks close the mouth's sides between the head and the jaw
            put(f"cheek{'R' if s > 0 else 'L'}", ell((0.082 * s, 0.02, -0.05), (0.04, 0.066, 0.058)), colour)
        put("tongue", ell((0, -0.04, -0.08), (0.04, 0.042, 0.012), rot=(-14, 0, 0)), TONGUE, fuse=False,
            surface="skin")
        # white fangs at the corners, the upper pair down, the lower pair up
        fw = M @ np.array([0, -1.0, 0])
        for s in (1, -1):
            sd = "R" if s > 0 else "L"
            cap.point(k, f"fangU{sd}{tag}", at((0.058 * s, -0.07, -0.012)), M @ unit(0, -0.25, -1),
                      0.012, 0.032, 0.011, WHITE, bone, up=fw, surface="shell")
            cap.point(k, f"fangD{sd}{tag}", at((0.064 * s, -0.07, -0.088)), M @ unit(0, -0.25, 1),
                      0.011, 0.027, 0.011, WHITE, bone, up=fw, surface="shell")
        # the stalk, from the collar up into the head's underside
        b = np.array(neck_from, float)
        top = at((0, 0.03, -0.09))
        mid = (b + top) / 2 + np.array([0, 0, -0.02]) + (top - b) * np.array([0.25, 0, 0])
        k.add(f"stalk{tag}", tube([b, mid, top], [0.019, 0.016, 0.02]), STALK, bone)
        self.eyes_at = []
        for s in (1, -1):
            d = unit(0.42 * s, -0.86, 0.16)
            sc = 1 / math.sqrt(((d / self.UR) ** 2).sum())
            self.eyes_at.append((at(self.UC + d * sc), M @ d, M[:, 0] * s))

    def eyes(self, state):
        lk = eye_look(state, cap.angry_eye("#3A2A22"), srgb(self.colour), srgb(INK), line=0.16)
        r = cap.cut_outline(0.22, 0.45)
        up = self.M[:, 2]
        for p, n, o in self.eyes_at:
            right = np.cross(up, n)
            sg = 1.0 if right @ o > 0 else -1.0
            self.k.decal(p, n, (0.03 * self.K, 0.027 * self.K), (lambda u, v, sg=sg: lk(sg * u, v)), up=up,
                         eps=0.0022, outline=(r if sg > 0 else (lambda th: r(math.pi - th))), rings=20, segs=56)


def body(k):
    k.add("chest", ell((0, 0.0, 0.37), (0.076, 0.066, 0.076)), LIGHT, "body")
    k.add("bulb", ell((0, 0.008, 0.25), (0.155, 0.122, 0.1)), DARKB, "body")
    # the zigzag where the chest's light green meets the dark
    def zig(P):
        th = np.arctan2(P[:, 0], -P[:, 1])
        z = 0.302 + 0.022 * tri(th * 6 / (2 * math.pi))
        return inside(z - P[:, 2], 0.0025) * inside(np.hypot(P[:, 0], P[:, 1]) - 0.17, 0.004)
    k.paint(zig, LIGHT, on=["chest", "bulb"])
    # arms akimbo, hands on the hips
    k.add("arm", tube([(0.06, 0.0, 0.405), (0.118, 0.012, 0.352), (0.112, 0.008, 0.3)], [0.026, 0.022, 0.022]), LIGHT,
          "body", mirror=True)
    k.add("hand", ell((0.104, 0.0, 0.29), (0.03, 0.026, 0.028)), LIGHT, "body", mirror=True)
    # thick dark legs in a wide stance, white toe caps
    k.add("leg", tube([(0.085, 0.005, 0.24), (0.145, -0.005, 0.14), (0.172, -0.012, 0.07)], [0.084, 0.072, 0.058]),
          DARKB, "leg{s}", mirror=True)
    k.add("foot", ell((0.178, -0.04, 0.032), (0.064, 0.082, 0.034)), DARKB, "leg{s}", mirror=True)
    k.paint(lambda P: inside(P[:, 1] + 0.078, 0.002) * inside(P[:, 2] - 0.075, 0.004)
            * inside(np.abs(P[:, 0]) - 0.26, 0.004) * inside(0.1 - np.abs(P[:, 0]), 0.004), WHITE, on=["foot"])
    # a small spike of a tail
    cap.point(k, "tail", (0, 0.11, 0.255), unit(0, 0.8, 0.6), 0.024, 0.06, 0.016, DARKB, "body")
    # the star of leaves the stalks rise from
    k.add("collarmound", ell((0, 0.0, 0.43), (0.05, 0.046, 0.026)), COLLAR, "body")
    for i in range(7):
        t = math.radians(i * 360 / 7 + 26)
        d = np.array([math.sin(t), -math.cos(t), 0.0])
        cap.point(k, f"collar{i}", np.array([0, 0, 0.432]) + d * 0.034, unit(*(d + np.array([0, 0, -0.32]))),
                  0.03, 0.066, 0.014, COLLAR, "body")


def scovillain(src, out):
    k = Kit(src, coat="leaf")
    body(k)
    heads = [PepperHead(k, "L", "headL", (-0.195, -0.01, 0.615), (-0.42, -0.9, 0.06), RED, (-0.016, 0.0, 0.43)),
             PepperHead(k, "R", "headR", (0.195, 0.0, 0.59), (0.42, -0.9, 0.1), HGREEN, (0.016, 0.0, 0.43))]
    k.build(voxel=VOXEL, smooth=3, band=0.007, wband=0.045)
    k.eye_states(lambda state: [h.eyes(state) for h in heads])
    k.export(out)


if __name__ == "__main__":
    scovillain(*args())
