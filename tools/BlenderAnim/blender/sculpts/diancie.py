# Diancie sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/diancie.py -- <the export's Diancie.glb> <out.glb>
# Borrows carbink.py's rock, ears and on_ell (sibling), as the design grows out of Carbink's, and roggenrola.py's
# faceted crystals.
#
# The design, from the references: a small lady rising out of a lump of grey rock. A round grey head with two small
# pointed grey ears out at its sides; on its brow a great round pink diamond in a brilliant's cut, and behind the head a
# crown of pink crystals fanned out: two rising up and out, two smaller further out, and two long ones reaching down and
# out like wings. Large red eyes with dark pupils, a small open mouth; a cream crescent of a collar under the chin, a
# small pink diamond on the chest pointing down. A white body flaring into a short frilled skirt, its arms broad white
# sleeves held out and down with small grey hands. Under the skirt the rock, tapering to a point and leaning, a pink
# crystal set in its side.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, unit, eye_look, args, hull, sibling

C = sibling("carbink")
R = C.R                                   # roggenrola.py: crystal, Glints

GREY, GREY_DK = "#8E8BA8", "#6E6B88"      # the head, ears and hands; the rock
ROCK = "#7E7B94"
WHITE, CREAM = "#F6F5FA", "#EFD98E"
PINK, PINK_DK, PINK_LT = "#F4A9CB", "#E57BAA", "#FAD2E4"
RED, RED_DK, EYE_INK = "#E0303C", "#8C1422", "#2A1420"

HEAD_C = np.array([0.0, -0.012, 0.35])
HEAD_R = np.array([0.068, 0.061, 0.063])


def brilliant(c, n, r, depth, up=(0, 0, 1.0)):
    """A round cut diamond facing along n: a pavilion's point behind, a girdle of sixteen, a crown of eight and an
    octagonal table."""
    n = unit(*n)
    u = np.array(up, float); u = u - n * (u @ n); u /= np.linalg.norm(u)
    e = np.cross(u, n)
    c = np.array(c, float)
    pts = [c - n * depth]
    for i in range(16):
        a = 2 * math.pi * i / 16
        pts.append(c + r * (math.cos(a) * e + math.sin(a) * u))
    for i in range(8):
        a = 2 * math.pi * (i + 0.5) / 8
        pts.append(c + n * depth * 0.32 + r * 0.78 * (math.cos(a) * e + math.sin(a) * u))
        pts.append(c + n * depth * 0.5 + r * 0.5 * (math.cos(a + math.pi / 8) * e + math.sin(a + math.pi / 8) * u))
    return hull(np.array(pts))


def diancie(src, out):
    k = Kit(src, coat="shell")
    glints = R.Glints()

    # --- the rock beneath: tapering to a point and leaning, a pink crystal in its side ------------------------------
    k.add("rock", C.rock((0.006, 0.006), [(0.17, 0.07, 0.064, 7), (0.13, 0.09, 0.078, 8), (0.07, 0.074, 0.066, 7),
                                          (0.025, 0.036, 0.034, 6)], 23, tip=(0.02, 0.01, 0.0), jitter=0.16),
          ROCK, "body", fuse=False, surface="shell")
    glints.put(R.crystal((0.07, -0.02, 0.075), (1, -0.35, -0.55), 0.06, 0.024, tip=0.35, turn=15), PINK, "body")
    glints.put(R.crystal((0.055, -0.05, 0.13), (0.7, -0.7, 0.1), 0.03, 0.015, tip=0.4), PINK_DK, "body")
    glints.put(R.crystal((-0.06, 0.03, 0.06), (-0.8, 0.4, -0.5), 0.034, 0.016, tip=0.4), PINK, "body")

    # --- the white body and its frilled skirt --------------------------------------------------------------------
    k.add("torso", ell((0, -0.004, 0.255), (0.038, 0.034, 0.05)), WHITE, "body")
    k.add("skirtcore", ell((0, 0.0, 0.192), (0.06, 0.054, 0.04)), WHITE, "body")
    for i in range(10):
        th = 2 * math.pi * i / 10
        k.add(f"frill{i}", ell((0.06 * math.sin(th), -0.055 * math.cos(th), 0.168 + 0.006 * (i % 2)),
                               (0.032, 0.03, 0.028)), WHITE, "body")

    # the sleeves: broad white cuffs held out, forward and down, small grey hands at their ends
    k.add("sleeve", tube([(0.03, -0.004, 0.275), (0.07, -0.025, 0.262), (0.098, -0.05, 0.245)], [0.022, 0.024, 0.028]),
          WHITE, "arm{s}", mirror=True)
    k.add("hand", ell((0.108, -0.06, 0.238), (0.016, 0.014, 0.015)), GREY, "arm{s}", mirror=True)

    # --- the head -------------------------------------------------------------------------------------------------
    k.add("neck", ell((0, -0.006, 0.296), (0.022, 0.02, 0.02)), GREY, "head")
    k.add("head", ell(HEAD_C, HEAD_R), GREY, "head")

    # the great diamond on the brow, facing forward and up
    gc = HEAD_C + np.array([0, -0.032, 0.05])
    k.add("diamond", brilliant(gc, (0, -0.75, 0.66), 0.045, 0.026), PINK, "head", fuse=False, surface="shell")
    # the crown of crystals fanned out behind the head
    back = HEAD_C + np.array([0, 0.02, 0.0])
    for x, z, d, ln, r, col in [(0.022, 0.04, (0.45, 0.2, 1), 0.105, 0.026, PINK),
                                (0.045, 0.025, (1, 0.25, 0.65), 0.085, 0.02, PINK_DK),
                                (0.04, -0.005, (1, 0.25, -0.62), 0.185, 0.03, PINK)]:
        for s in (1, -1):
            glints.put(R.crystal(back + np.array([s * x, 0, z]), (s * d[0], d[1], d[2]), ln, r, sides=6, tip=0.3,
                                 flat=0.6, turn=0, up=(0, 1, 0)), col, "head")
    glints.flush(k)

    # the small pointed ears out at the head's sides
    C.ear(k, "ear", (0.052, 0.0, 0.365), unit(1, 0.15, 0.25), (0, 0.35, 1), 0.05, 0.034, 0.012, GREY, None, 0, "head")

    # the cream collar: a crescent under the chin
    arc = [(0.04 * math.sin(t), -0.004 - 0.036 * math.cos(t), 0.284 + 0.012 * (1 - math.cos(t)))
           for t in np.linspace(-1.9, 1.9, 9)]
    k.add("collar", tube(arc, [0.01, 0.013, 0.015, 0.016, 0.016, 0.016, 0.015, 0.013, 0.01], seg=14), CREAM, "body",
          fuse=False, surface="shell")
    # the pink diamond on the chest, pointing down
    k.add("chestgem", hull([(0, -0.042, 0.268), (0.016, -0.04, 0.252), (-0.016, -0.04, 0.252), (0, -0.036, 0.215),
                            (0, -0.026, 0.25), (0, -0.052, 0.252)]), PINK_DK, "body", fuse=False, surface="shell",
          glow=PINK_DK)

    # the mouth: small and open
    mp, mn = C.on_ell(HEAD_C, HEAD_R, 0.0, 0.318)
    dark, tongue = srgb("#5A1A2C"), srgb("#E6728C")
    def mouth(u, v):
        c = np.tile(dark, (len(u), 1))
        c[(u / 0.6) ** 2 + ((v + 0.55) / 0.5) ** 2 < 1] = tongue
        return c
    k.decal(mp, mn, (0.0085, 0.0065), mouth, eps=0.002, rings=10, segs=40,
            outline=lambda th: 1.0 if math.sin(th) < 0 else 0.55)

    k.build(voxel=0.0035, smooth=5, band=0.004, wband=0.03)

    # --- the eyes: large and red, a dark pupil, a white highlight, slanted a little --------------------------------
    cr, cd, ck, lid = srgb(RED), srgb(RED_DK), srgb(EYE_INK), srgb(GREY)
    def open_eye(u, v):
        c = np.tile(srgb("#F8F2F4"), (len(u), 1))
        c[((u + 0.05) / 0.72) ** 2 + ((v + 0.02) / 0.8) ** 2 < 1] = cr
        c[((u + 0.08) / 0.34) ** 2 + ((v + 0.02) / 0.42) ** 2 < 1] = cd
        c[((u - 0.12) / 0.2) ** 2 + ((v - 0.34) / 0.2) ** 2 < 1] = (1, 1, 1)
        c[(u * u + v * v > 0.86 ** 2) | ((v > 0.55 - 0.25 * u) & (u * u + v * v > 0.7 ** 2))] = ck
        return c
    p, n = C.on_ell(HEAD_C, HEAD_R, 0.027, 0.338)

    def eyes(state):
        k.decal(p, n, (0.019, 0.021), eye_look(state, open_eye, lid, ck, line=0.15), eps=0.002, rings=20, segs=56,
                mirror=True)

    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    diancie(*args())
