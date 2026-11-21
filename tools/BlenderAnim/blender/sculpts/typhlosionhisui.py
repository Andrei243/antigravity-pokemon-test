# Typhlosion-Hisui sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/typhlosionhisui.py -- <the export's Typhlosion-Hisui.glb> <out.glb>
# Typhlosion's Hisuian form, from the pictures: the same great cream pear of a body on short legs, a little slimmer
# at the waist, the arms short and forward; but the dark is a deep indigo, and it reaches further: a hood over the
# crown and nape that wraps round the neck as a collar, a stripe down the spine to a long ragged tail trailing low
# behind. The snout is longer and narrower, the mouth a closed sly line, the eyes half shut under a heavy brow, red.
# The fire is a ghost's: a great plume of violet tongues with a red heart streaming up and back from the nape (the
# design's second mass, as tall as the head and body above the shoulders), and on the collar a string of glowing
# beads, red-pink with a violet swirl. Built on typhlosion.py's frame (copied: its function takes no arguments) and
# the line's own helpers (cyndaquil's hood and claws, quilava's eyes under a brow).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, hull, srgb, inside, smoothstep, unit, polygon, outward, args, sibling

cq = sibling("cyndaquil")
ql = sibling("quilava")
INDIGO, CREAM = "#352E62", "#F4E6A4"
VIOLET, VCORE = "#B65AD8", "#E8344E"
GLOW_VIOLET, GLOW_VCORE = "#C060E8", "#FF3C58"
BEAD, SWIRL = "#E43A62", "#8A3AC8"
MOUTH = srgb("#2A1E3A")

H, HR = np.array([0.0, -0.085, 0.755]), np.array([0.112, 0.116, 0.1])
SNOUT = [(0, -0.13, 0.735), (0, -0.21, 0.716), (0, -0.275, 0.7)]


class GhostFlames(cq.Flames):
    """The line's tongues of fire in violet with a red heart."""

    def flame(self, base, d, length, width, bone, side, flick=0.3, heart=0.6):
        b = np.array(base, float)
        d = unit(*d)
        self.groups.setdefault((bone, VIOLET), []).append(self.tongue(b, d, length, width, side, flick))
        ny = cq.frame(d, side)[:, 1]
        for sg in (1, -1):
            self.groups.setdefault((bone, VCORE), []).append(
                self.tongue(b + ny * sg * width * 0.27, d, length * heart, width * 0.6, side, flick * 0.5, thick=0.3,
                            rag=0.15, root=0.3))

    def add(self, k):
        for i, ((bone, colour), shapes) in enumerate(self.groups.items()):
            V, F, off = [], [], 0
            for sv, sf in shapes:
                V.append(sv); F.extend([[j + off for j in f] for f in sf]); off += len(sv)
            k.add(f"ghost{i}", (np.vstack(V), F), colour, bone, fuse=False,
                  glow=GLOW_VIOLET if colour == VIOLET else GLOW_VCORE)
        self.groups = {}


def typhlosion_hisui(src, out):
    k = Kit(src, coat="fur")

    # The body: Typhlosion's pear, a little slimmer at the waist, the hips low and wide over the thighs
    k.add("hips", ell((0, 0.02, 0.26), (0.2, 0.18, 0.19)), CREAM, "body")
    k.add("belly", ell((0, -0.01, 0.41), (0.172, 0.165, 0.22), rot=(-8, 0, 0)), CREAM, "body")
    k.add("chest", ell((0, -0.035, 0.54), (0.138, 0.125, 0.12), rot=(-10, 0, 0)), CREAM, "body")
    k.add("neck", tube([(0, -0.03, 0.56), (0, -0.05, 0.64), (0, -0.075, 0.7)], [0.108, 0.094, 0.084]), CREAM, "head")

    # The head carried forward: a crown, a longer, narrower snout than Typhlosion's
    k.add("head", ell(H, HR), CREAM, "head")
    k.add("snout", tube(SNOUT, [0.074, 0.056, 0.036]), CREAM, "head")
    k.add("jaw", ell((0, -0.14, 0.7), (0.07, 0.1, 0.046)), CREAM, "head")

    # The arms: short and thick, forward and a little down, three small claws a hand
    k.add("arm", tube([(0.125, -0.04, 0.53), (0.175, -0.1, 0.49), (0.18, -0.17, 0.455)], [0.054, 0.047, 0.042]),
          CREAM, "arm{s}", mirror=True)
    HC = np.array([0.18, -0.2, 0.445])
    k.add("hand", ell(HC, (0.042, 0.038, 0.038)), CREAM, "arm{s}", mirror=True)
    for j, (dx, dz) in enumerate([(0.02, 0.006), (0.0, -0.004), (-0.02, 0.006)]):
        b = HC + np.array((dx, -0.03, -0.006 + dz))
        k.add(f"claw{j}", cq.claw(b, (dx * 1.2, -1, -0.6), 0.03, 0.019, 0.015, up=(0, 1, 0)), cq.CLAW, "arm{s}",
              fuse=False, mirror=True, surface="shell")

    # The legs: thighs fused into the hips, a stub of shin, broad feet with three claws
    k.add("thigh", ell((0.125, -0.02, 0.17), (0.105, 0.125, 0.13)), CREAM, "leg{s}", mirror=True)
    k.add("shin", tube([(0.13, -0.04, 0.1), (0.135, -0.05, 0.068)], [0.068, 0.062]), CREAM, "leg{s}", mirror=True)
    k.add("foot", ell((0.137, -0.085, 0.028), (0.066, 0.1, 0.03)), CREAM, "leg{s}", mirror=True)
    for j, dx in enumerate((-0.028, 0.0, 0.028)):
        k.add(f"toe{j}", cq.claw((0.137 + dx, -0.175, 0.026), (dx * 0.6, -1, -0.15), 0.032, 0.023, 0.017), cq.CLAW,
              "leg{s}", fuse=False, mirror=True, surface="shell")

    # The tail: long, dark and ragged, trailing back and down to the ground and out to the +X side
    TP = [(0, 0.16, 0.26), (0.04, 0.27, 0.17), (0.1, 0.37, 0.09), (0.17, 0.46, 0.04)]
    k.add("tail", blade(TP, [0.065, 0.05, 0.034, 0.02], (0.6, 0, 0.8), 0.55), INDIGO, "tail")
    for j, (t, L, w) in enumerate([(0.3, 0.075, 0.034), (0.55, 0.085, 0.032), (0.8, 0.075, 0.028)]):
        P = np.array(TP)
        i = t * (len(P) - 1); a = int(i); f_ = i - a
        p = P[a] + (P[a + 1] - P[a]) * f_
        d = unit(0.35, 0.5, 0.9)
        side = unit(0.8, -0.6, 0)
        k.add(f"tailspike{j}", hull([p + side * w, p - side * w, p + d * 0.005 + unit(0, 0.6, -0.8) * w,
                                     p - unit(0, 0.6, -0.8) * w * 0.5, p + d * L]), INDIGO, "tail", fuse=False,
              surface="fur")

    # The dark: the spine's stripe, the hood from the nape over the crown to a point between the eyes, and a collar
    # round the neck under the beads
    stripe_ys = ([0.1, 0.2, 0.3, 0.4, 0.5, 0.58, 0.66, 0.76, 0.86], [0.14, 0.15, 0.155, 0.14, 0.1, 0.05, 0.01, -0.06, -0.09])
    back = ql.back_mask(*stripe_ys, 0.86)
    narrow = lambda P: inside(np.abs(P[:, 0]) - (0.08 + 0.04 * smoothstep(0.7, 0.8, P[:, 2])), 0.0035)
    crown = cq.hood([-0.4, -0.22, -0.19, -0.16, -0.09, 0.0, 0.05, 0.1, 0.4],
                    [0.765, 0.765, 0.772, 0.778, 0.795, 0.75, 0.69, 0.65, 0.65], below=0.64)
    point = lambda P: inside(np.abs(P[:, 0]) - np.interp(P[:, 1], [-0.255, -0.215, -0.18, -0.12, 0.2],
                                                        [0.0, 0.045, 0.12, 0.2, 0.3]), 0.0035)
    def collar(P):
        y, z = P[:, 1], P[:, 2]
        top = 0.665 + 0.06 * smoothstep(-0.11, 0.04, y)
        r = np.sqrt(P[:, 0] ** 2 + ((y + 0.05) / 1.05) ** 2)
        return inside(z - top, 0.003) * inside(0.585 - z, 0.003) * inside(r - 0.15, 0.003)
    dark = lambda P: np.maximum(np.maximum(back(P) * narrow(P), crown(P) * point(P)), collar(P))
    k.paint(dark, INDIGO)
    k.paint(lambda P: 1 - dark(P), CREAM, on=["hips", "belly", "chest", "neck", "head", "snout", "jaw", "arm",
                                              "hand", "thigh", "shin", "foot"])

    # Pointed dark ears laid back at the crown's sides
    for s in (1, -1):
        b = np.array([0.072 * s, -0.04, 0.83])
        tip = b + np.array([0.03 * s, 0.07, 0.065])
        k.add(f"ear{s}", hull([b + (0.025, 0, 0), b - (0.025, 0, 0), b + (0, 0.025, 0), b - (0, 0.025, 0),
                               b - (0, 0, 0.02), tip]), INDIGO, "head", fuse=False, surface="fur")

    # The ghost fire: a great plume from the nape and the back of the head, streaming up and back and out
    f = GhostFlames()
    N = np.array([0, 0.03, 0.72])
    for d, L, w, fl in [((0, 0.35, 1), 0.3, 0.09, 0.3),
                        ((0, 0.8, 0.9), 0.42, 0.1, 0.35),
                        ((0, 1, 0.45), 0.44, 0.1, 0.35),
                        ((0.5, 0.6, 0.85), 0.36, 0.09, 0.3), ((-0.5, 0.6, 0.85), 0.36, 0.09, 0.3),
                        ((0.7, 0.75, 0.35), 0.36, 0.085, 0.3), ((-0.7, 0.75, 0.35), 0.36, 0.085, 0.3),
                        ((0.35, 0.95, 0.1), 0.36, 0.08, 0.3), ((-0.35, 0.95, 0.1), 0.36, 0.08, 0.3)]:
        dd = unit(*d)
        side = np.cross(dd, (0, 0, 1.0))
        if np.linalg.norm(side) < 1e-3:
            side = np.array([1.0, 0, 0])
        f.flame(N + np.array([d[0] * 0.06, 0, 0]), dd, L, w, "flame", side, flick=fl)
    f.add(k)

    # The beads on the collar: glowing red-pink balls, a violet swirl on each, round the front of the neck
    beads = []
    swirls = []
    for ang, r in [(-48, 0.043), (8, 0.04), (55, 0.042)]:
        a = math.radians(ang)
        o = np.array([math.sin(a), -math.cos(a), 0.0])
        c = np.array([0, -0.05, 0.615]) + o * np.array([0.145, 0.14, 0])
        beads.append(ell(c, (r, r, r), sub=3))
        # the swirl: a violet cap set into the bead's lower outer side, only its face showing
        swirls.append(ell(c + o * r * 0.3 + (0, 0, -r * 0.3), (r * 0.78, r * 0.78, r * 0.6), sub=3))
    for name, group, col, glow in (("bead", beads, BEAD, BEAD), ("swirl", swirls, SWIRL, None)):
        V, F, off = [], [], 0
        for sv, sf in group:
            V.append(sv); F.extend([[j + off for j in f_] for f_ in sf]); off += len(sv)
        k.add(name, outward((np.vstack(V), F)), col, "head", fuse=False, glow=glow)

    # The mouth: a closed sly line along the jaw, rising a little at its back; the nose's dark tip
    def mouth(u, v):
        c = np.tile(srgb(CREAM), (len(u), 1))
        line = -0.1 + 0.35 * u * u
        c[(np.abs(v - line) < 0.3) & (np.abs(u) < 0.95)] = MOUTH
        return c
    k.decal((0, -0.25, 0.682), unit(0, -0.6, -0.8), (0.05, 0.016), mouth, eps=0.003, rings=10, segs=48)
    k.decal((0, -0.307, 0.705), (0, -1, 0.15), (0.012, 0.009), cq.NOSE, eps=0.003, rings=4, segs=20)

    # The eyes: half shut, red, under a heavy indigo brow sloping down to the snout
    # The outline is cut close over the middle (it must keep the patch's middle inside) and the patch is low, so the
    # eye is a narrow half-shut lens
    cut = ql.brow_cut(0.08, 0.3)
    de = unit(0.6, -0.74, 0.18)
    E = cq.on_ell(H + (0, -0.01, -0.006), HR, de)
    eyes = ql.eyes_under_brow(k, E, de, (0.05, 0.036), ql.fierce_eye(ql.RED, cut, iris_c=(-0.1, -0.3),
                                                                     iris_r=0.5, pupil_r=0.24, rim=0.74),
                              cut, srgb(CREAM), line=0.22)

    k.build(voxel=0.0075, smooth=4, band=0.006)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    typhlosion_hisui(*args())
