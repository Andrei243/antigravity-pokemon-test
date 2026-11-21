# Thwackey sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/thwackey.py -- <the export's Thwackey.glb> <out.glb>
#
# Grown from grookey.py (sibling): its ear with a lining, its cut stick, its leaf, its muzzle and nostrils.
# From the references (they overrule the task's text: no leafy hood; the green on its chest is a dark teal tuft of
# fur, not a ball): a chartreuse monkey whose head and body are one tall bean with no neck, a little peak on the
# crown; two brown sticks crossed in an X behind the top of the head; a pale cream band round the eyes with a leaf
# mark rising from it on the forehead; stern amber eyes, the top lid flat; a big orange muzzle drooping like a beak,
# two nostrils; round dark-brown ears at the sides; a small dark-green tuft behind the +X ear. Thick dark-brown arms
# folded forward before the chest (crossed in the art; here each hand comes in before the chest, inside the body's
# width), orange hands with fingers; a dark teal tuft of fur on the chest; short bowed chartreuse legs in a wide
# stance, broad feet with three toes; a short shaggy brown tail hanging behind. Height about 0.56.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, lumpy, outward, srgb, inside, unit, eye_look, polygon, args, sibling

g = sibling("grookey")
hit, round_about = g.hit, g.round_about

LIME, MASK, TEAL = "#CCDA4C", "#F1EDBA", "#1F7A5E"
ORANGE, ORANGE_D, BROWN, BROWN_D = "#F06A2A", "#B8461C", "#5E3B2C", "#3C2418"
WOOD, WOOD_END, TUFT = "#8E5C34", "#D2A672", "#2E8A55"
IRIS, INK = "#F2C232", "#17151A"

H = np.array([0.0, -0.006, 0.405])          # the head's middle (its bone is at 0, 0, 0.32)
HR = np.array([0.095, 0.086, 0.104])


def body(k):
    """Head and body one tall bean: the torso, a fill where a neck would be, the head and a peak on the crown."""
    def bean(V):
        V = V.copy(); s = 1 + 0.08 * np.clip(-V[:, 2], 0, 1)
        V[:, 0] *= s; V[:, 1] *= s
        return V
    k.add("torso", ell((0, 0.006, 0.215), (0.083, 0.073, 0.125), deform=bean), LIME, "body")
    k.add("neck", ell((0, 0.0, 0.32), (0.084, 0.074, 0.07)), LIME, "body")
    HEAD = ell(H, HR)
    k.add("head", HEAD, LIME, "head")
    k.add("peak", tube([H + (0, 0.004, HR[2] - 0.035), H + (0, 0.006, HR[2] + 0.01)], [0.04, 0.022]), LIME,
          "head")
    k.add("cheeks", ell(H + (0, -0.03, -0.035), (0.082, 0.06, 0.06)), LIME, "head")
    # The dark teal tuft of fur on the chest, before the folded arms
    k.add("chest", ell((0, -0.062, 0.272), (0.058, 0.03, 0.045), deform=lumpy(5, 3, 0.12)), TEAL, "body")
    return HEAD


def legs(k):
    """Short bowed legs in a wide stance, haunches fused into the hips, broad feet with three toes."""
    k.add("haunch", ell((0.062, 0.0, 0.13), (0.056, 0.058, 0.062)), LIME, "leg{s}", mirror=True)
    k.add("shin", tube([(0.084, -0.006, 0.11), (0.098, -0.012, 0.06), (0.1, -0.016, 0.03)], [0.036, 0.033, 0.03]),
          LIME, "leg{s}", mirror=True)
    F = np.array((0.104, -0.036, 0.017))
    k.add("foot", ell(F, (0.042, 0.056, 0.018)), LIME, "leg{s}", mirror=True)
    for i, dx in enumerate((-0.022, 0.0, 0.022)):
        k.add(f"toe{i}", ell(F + np.array([dx, -0.052, -0.003]), (0.013, 0.017, 0.012)), LIME, "leg{s}",
              mirror=True)


def arms(k):
    """Thick dark-brown arms folded forward: down to the elbow at the side, the forearm in and up before the chest,
    an orange hand with its fingers curled at the chest's tuft."""
    S, E, W = np.array((0.068, -0.004, 0.302)), np.array((0.112, -0.05, 0.236)), np.array((0.05, -0.108, 0.262))
    k.add("upper", tube([S, (S + E) / 2 + np.array([0.01, 0, 0]), E], [0.036, 0.034, 0.032]), BROWN, "arm{s}",
          mirror=True)
    k.add("fore", tube([E, W], [0.032, 0.028]), BROWN, "arm{s}", mirror=True)
    d = unit(*(W - E))
    P = W + d * 0.022
    k.add("hand", ell(P, (0.026, 0.028, 0.026)), ORANGE, "arm{s}", mirror=True)
    for i, dz in enumerate((0.018, 0.004, -0.01)):
        p0 = P + d * 0.016 + np.array([0, -0.004, dz])
        k.add(f"finger{i}", tube([p0, p0 + np.array([-0.012, 0.01, -0.004])], [0.0105, 0.009]), ORANGE, "arm{s}",
              mirror=True)
    k.add("thumb", tube([P + np.array([0.0, -0.02, 0.016]), P + np.array([-0.014, -0.028, 0.026])],
                        [0.0105, 0.009]), ORANGE, "arm{s}", mirror=True)


def tail(k):
    """A short shaggy brown tail hanging behind."""
    k.add("tail", tube([(0, 0.06, 0.19), (0.008, 0.095, 0.17), (0.014, 0.108, 0.135)], [0.032, 0.034, 0.028]),
          BROWN, "tail")
    k.add("tailtuft", ell((0.016, 0.11, 0.125), (0.032, 0.028, 0.034), deform=lumpy(5, 3, 0.18)), BROWN, "tail")


def mask(k):
    """The pale cream band round the eyes, a leaf mark rising from it in the middle of the forehead."""
    EX, EZ = 0.042, H[2] - 0.004
    def m(P):
        x, z = round_about(P, H, HR[0])
        front = inside(P[:, 1] - (H[1] - 0.03), 0.003) * inside(np.sqrt((((P - H) / (HR * 1.35)) ** 2).sum(1)) - 1)
        lobe = np.hypot((np.abs(x) - EX) * 0.92, (z - EZ) * 1.15) - 0.044
        bridge = np.maximum(np.abs(x) - 0.06, np.abs(z - (EZ - 0.01)) - 0.026)
        mark = np.hypot(x / 0.022, (z - (EZ + 0.046)) / 0.034) - 1          # the leaf on the forehead
        d = np.minimum(np.minimum(lobe, bridge), mark * 0.02)
        return inside(d, 0.0025) * front
    k.paint(m, MASK)
    return EX, EZ


def thwackey(src, out):
    k = Kit(src, coat="fur")
    HEAD = body(k)
    legs(k)
    arms(k)
    tail(k)
    g.ear(k, (0.092, 0.012, 0.408), (0.042, 0.018, 0.042), (0, 0, 22), BROWN, BROWN_D)
    MC, MR = H + (0, -0.086, -0.046), (0.046, 0.044, 0.05)
    def beak(V):
        V = V.copy(); t = np.clip(-V[:, 2], 0, 1)          # narrower toward its drooping lower end
        V[:, 0] *= 1 - 0.35 * t
        return V
    S = ell(MC, MR, rot=(-28, 0, 0), deform=beak)
    k.add("muzzle", S, ORANGE, "head", surface="shell")
    EX, EZ = mask(k)

    # Two sticks crossed in an X behind the crown, the dark green tuft behind the +X ear
    g.stick(k, (-0.13, 0.05, 0.478), (0.125, 0.052, 0.522), 0.012, name="stickA", wood=WOOD, end=WOOD_END)
    g.stick(k, (-0.03, 0.058, 0.525), (0.15, 0.046, 0.47), 0.012, name="stickB", wood=WOOD, end=WOOD_END)
    for i, (d, L) in enumerate((((0.75, 0.55, 0.35), 0.05), ((0.6, 0.7, 0.05), 0.042))):
        g.leaf_piece(k, f"tuft{i}", H + (0.07, 0.04, 0.02), d, L, 0.016, 0.012, TUFT, "head", face=(0.6, -0.2, 0.75))

    # Nostrils on the muzzle's top front
    g.nostrils(k, S, MC, MR, 0.022, 0.014, (0.0058, 0.0048), srgb(ORANGE_D))

    # Stern amber eyes on the cream band: a black rim, the top line flat and sloping a little to the nose
    OUT = [(-1.0, 0.25), (-0.75, -0.6), (0.0, -1.0), (0.75, -0.6), (1.0, 0.0), (0.6, 0.45), (-0.6, 0.6)]
    def open_look(u, v):
        c = np.tile(srgb(INK), (len(u), 1))
        c[(u / 0.8) ** 2 + ((v + 0.08) / 0.72) ** 2 < 1] = srgb(IRIS)
        c[((u + 0.12) / 0.4) ** 2 + ((v + 0.06) / 0.46) ** 2 < 1] = srgb(INK)
        c[((u + 0.28) / 0.14) ** 2 + ((v - 0.12) / 0.14) ** 2 < 1] = np.array([1.0, 1.0, 1.0])
        return c
    de = unit(0.42, -0.9, 0.04)
    E = g.on_ell(H, HR, de)
    def lay(state):
        look = eye_look(state, open_look, srgb(MASK), srgb(INK), line=0.17)
        for s in (1, -1):
            out_ = polygon(OUT if s > 0 else [(-a, b) for a, b in OUT])
            col = look if s > 0 else (lambda u, v, look=look: look(-u, v))
            k.decal(E * np.array([s, 1, 1]), de * np.array([s, 1, 1]), (0.034, 0.031), col, outline=out_,
                    eps=0.002, rings=20, segs=56)

    k.build(voxel=0.0052, smooth=4, band=0.0035, wband=0.04)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    thwackey(*args())
