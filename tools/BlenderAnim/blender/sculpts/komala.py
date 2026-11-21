# Komala sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/komala.py -- <the export's Komala.glb> <out.glb>
#
# The design, from the references (the pictures overrule the task's text where they differ): a little grey-blue koala
# fast asleep sitting up, hugging a log. A big round head, half its height, with great round ears standing out at the
# crown's corners, peach inside low toward the head; a large dark oval nose standing out of the middle of the face; the
# eyes always shut, curved lines either side of the nose, a white tuft over each; white puffs on the cheeks and a white
# fluffy bib under the chin; a small open pink mouth under the nose. A round small body, a pale belly low in front;
# short thick arms round the log, dark claws gripping its front; short legs, the +X foot showing its sole with dark pads.
# The log stands on the -X side on a slant, brown bark, a pale cut end with a ring at its top, dark knots on its front.
# Height 0.40 as the export; a single-stage line.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, lathe, outward, srgb, inside, unit, boxy, eye_look, polygon, args, sibling

ted = sibling("teddiursa")
hit = ted.hit

BLUE, PEACH, WHITE, BELLY = "#9DBEDF", "#F4C59A", "#F4F6F8", "#E2ECF4"
NOSE, INK, MOUTH, LIP = "#45495A", "#2A2C36", "#E8939A", "#5A3A44"
BARK, BARK_D, CUT, RING, KNOT, CLAW = "#8A5E3E", "#6E4A30", "#E2CFA4", "#B99A6E", "#4A3424", "#3C3E48"

HC = np.array([0.0, -0.02, 0.283])            # the head's middle (its bone is at 0, -0.01, 0.23)
HR = np.array([0.116, 0.099, 0.107])
LOG_C = np.array([-0.1, -0.14, 0.145])        # the log's middle, its axis and half length, its radius
LOG_D = unit(-0.55, -0.12, 0.82)
LOG_H, LOG_R = 0.12, 0.054


def log(k):
    """A loose log on a slant: bark, a pale cut end with a darker ring at the top, a pale bottom, dark knots."""
    L, r = LOG_H, LOG_R
    k.add("log", lathe(LOG_C, LOG_D, [(0, -L), (r * 0.9, -L), (r, -L + 0.006), (r, L - 0.006), (r * 0.92, L),
                                      (0, L)], seg=40), BARK, "armL", fuse=False, surface="leather")
    k.add("logend", lathe(LOG_C, LOG_D, [(0, L - 0.003), (r * 0.86, L - 0.003), (r * 0.86, L + 0.003),
                                         (0, L + 0.003)], seg=40), CUT, "armL", fuse=False, surface="leather")
    k.add("logring", lathe(LOG_C, LOG_D, [(0, L), (0.03, L), (0.03, L + 0.0048), (0, L + 0.0048)], seg=40), RING,
          "armL", fuse=False, surface="leather")
    k.add("logcore", lathe(LOG_C, LOG_D, [(0, L), (0.022, L), (0.022, L + 0.0062), (0, L + 0.0062)], seg=40), CUT,
          "armL", fuse=False, surface="leather")
    k.add("logfoot", lathe(LOG_C, LOG_D, [(0, -L - 0.003), (r * 0.86, -L - 0.003), (r * 0.86, -L + 0.003),
                                          (0, -L + 0.003)], seg=40), CUT, "armL", fuse=False, surface="leather")
    # Dark knots on the bark's front
    front = np.array([0, -1.0, 0]) - LOG_D * LOG_D[1] * -1
    front = unit(*(np.array([0, -1.0, 0.0]) - LOG_D * np.dot(LOG_D, (0, -1.0, 0))))
    for i, (t, turn) in enumerate(((-0.45, 0.25), (0.15, -0.35))):
        side = unit(*np.cross(LOG_D, front))
        o = unit(*(front + side * turn))
        p = LOG_C + LOG_D * (t * L) + o * (r - 0.004)
        k.add(f"knot{i}", outward(ell(p, (0.013, 0.013, 0.013), sub=3)), KNOT, "armL", fuse=False,
              surface="leather")


def claw(k, name, base, d, bone):
    """A small dark claw: a flat-faced wedge from base along d."""
    base, d = np.array(base, float), unit(*d)
    a = unit(*np.cross(d, (0, 0, 1.0)))
    b = np.cross(a, d)
    w = 0.006
    k.add(name, hull([base + a * w + b * w * 0.8, base - a * w + b * w * 0.8, base + a * w - b * w * 0.7,
                      base - a * w - b * w * 0.7, base - d * w, base + d * 0.022 - b * w * 0.3]), CLAW, bone,
          fuse=False, surface="shell")


def komala(src, out):
    k = Kit(src, coat="fur")

    # The body: a small round pear sitting, its pale belly low in front
    def pear(V):
        V = V.copy()
        s = 1 + 0.08 * np.clip(-V[:, 2], 0, 1) - 0.1 * np.clip(V[:, 2], 0, 1)
        V[:, 0] *= s; V[:, 1] *= s
        return V
    B = np.array((0.0, 0.01, 0.12))
    k.add("body", ell(B, (0.1, 0.095, 0.112), deform=pear), BLUE, "body")
    k.paint(lambda P: inside(np.hypot(P[:, 0] / 0.06, (P[:, 2] - 0.09) / 0.07) - 1, 0.003) * (P[:, 1] < B[1] - 0.04),
            BELLY, on=["body"])

    # Short legs: thighs fused into the bottom, feet forward; the +X sole shows its pads
    for s, bone in ((1, "legR"), (-1, "legL")):
        m = np.array([s, 1, 1.0])
        k.add("thigh" + bone[-1], ell(np.array((0.068, -0.04, 0.065)) * m, (0.055, 0.065, 0.052)), BLUE, bone)
        k.add("foot" + bone[-1], ell(np.array((0.082, -0.112, 0.042)) * m, (0.036, 0.028, 0.04)), BLUE, bone)
    FT = hit(ell((0.082, -0.112, 0.042), (0.036, 0.028, 0.04)), (0.082, -1, 0.042), (0, 1, 0))
    def pads(u, v):
        c = np.tile(srgb(BLUE), (len(u), 1))
        c[(u / 0.55) ** 2 + ((v + 0.3) / 0.45) ** 2 < 1] = srgb(INK)
        for x, y in ((-0.55, 0.55), (0.0, 0.7), (0.55, 0.55)):
            c[((u - x) / 0.22) ** 2 + ((v - y) / 0.24) ** 2 < 1] = srgb(INK)
        return c
    k.decal(FT, (0, -1, 0), (0.028, 0.034), pads, eps=0.0025, rings=16, segs=48)

    # The head, big and round; the dark nose stands out of its middle
    HEAD = ell(HC, HR, deform=boxy(2.15))
    k.add("head", HEAD, BLUE, "head")
    k.add("cheek", ell(HC + np.array([0, -0.035, -0.045]), (0.085, 0.06, 0.05)), BLUE, "head")
    NC = HC + np.array([0.0, -0.088, -0.008])
    k.add("nose", ell(NC, (0.034, 0.026, 0.046), rot=(0, -10, 0)), NOSE, "head")
    # White puffs on the cheeks and a white bib under the chin
    for s in (1, -1):
        for j, (dx, dy, dz, r) in enumerate([(0.0, 0.0, 0.0, 0.017), (0.012, 0.01, 0.014, 0.014),
                                            (0.016, 0.006, -0.012, 0.014)]):
            c = HC + np.array([(0.083 + dx) * s, -0.06 + dy, -0.045 + dz])
            k.add(f"puff{'R' if s > 0 else 'L'}{j}", ell(c, (r, r, r)), WHITE, "head")
    for j, x in enumerate((-0.036, -0.018, 0.0, 0.018, 0.036)):
        k.add(f"bib{j}", ell((x, -0.094 + 0.012 * abs(x) / 0.036, 0.19 - 0.006 * (j % 2)), (0.017, 0.014, 0.015)),
              WHITE, "body")

    # Great round ears at the crown's corners, turned 30 degrees out so their faces show; peach low and inward
    EC = np.array((0.142, 0.0, 0.355))
    k.add("ear", ell(EC, (0.074, 0.026, 0.066), rot=(0, 0, 30)), BLUE, "ear{s}", mirror=True)
    en = unit(math.sin(math.radians(30)), -math.cos(math.radians(30)), 0)
    lin = hit(ell(EC, (0.074, 0.026, 0.066), rot=(0, 0, 30)), EC + en * 0.2 + np.array([-0.012, 0, -0.016]), -en)
    k.decal(lin, en, (0.048, 0.042), srgb(PEACH), eps=0.003, rings=12, segs=48, mirror=True)

    # Short thick arms round the log: the -X arm round its front, the +X arm across the chest onto its top side
    k.add("armL", tube([(-0.072, -0.03, 0.2), (-0.045, -0.12, 0.18), (-0.075, -0.19, 0.162)], [0.032, 0.03, 0.028]),
          BLUE, "armL")
    k.add("handL", ell((-0.092, -0.2, 0.152), (0.026, 0.02, 0.026)), BLUE, "armL")
    k.add("armR", tube([(0.072, -0.03, 0.19), (0.03, -0.1, 0.168), (-0.035, -0.13, 0.175)], [0.032, 0.03, 0.028]),
          BLUE, "armR")
    k.add("handR", ell((-0.052, -0.14, 0.178), (0.026, 0.024, 0.024)), BLUE, "armR")
    for i, dz in enumerate((0.014, 0.0, -0.014)):
        claw(k, f"clawL{i}", (-0.1 + dz * 0.6, -0.212, 0.14 + dz * 0.4), (-0.2, -0.15, -1), "armL")
    log(k)

    # A small open pink mouth under the nose
    MC = hit(HEAD, (0.0, -1, HC[2] - 0.065), (0, 1, 0))
    def mouth(u, v):
        c = np.tile(srgb(BLUE), (len(u), 1))
        r = (u / 1.0) ** 2 + ((v + 0.1) / 0.8) ** 2
        c[r < 1] = srgb(LIP)
        c[(r < 0.6) & (v < 0.25)] = srgb(MOUTH)
        return c
    k.decal(MC, unit(0, -1, -0.15), (0.018, 0.012), mouth, eps=0.003, rings=12, segs=48)

    # The white tuft over each eye, sloping out
    for s in (1, -1):
        tc = hit(HEAD, (0.068 * s, -1, HC[2] + 0.04), (0, 1, 0))
        k.decal(tc, unit(0.55 * s, -0.83, 0.1), (0.011, 0.024), srgb(WHITE), up=(-0.35 * s, 0, 1), eps=0.003,
                rings=10, segs=40)

    # Eyes always shut: the open state is the line itself, curving down; Fierce the same line tilted toward the nose
    CB, CI = srgb(BLUE), srgb(INK)
    def slit(u, v):
        c = np.tile(CB, (len(u), 1))
        c[(np.abs(u) < 0.85) & (np.abs(v - (0.15 - 0.45 * (1 - u * u))) < 0.3)] = CI
        return c
    def fierce(u, v):
        c = np.tile(CB, (len(u), 1))
        c[(np.abs(u) < 0.85) & (np.abs(v - (0.05 + 0.35 * u)) < 0.3)] = CI
        return c
    de = unit(0.5, -0.86, 0.05)
    E = hit(HEAD, (0.055, -1, HC[2] + 0.006), (0, 1, 0))
    def lay(state):
        look = fierce if state == "Fierce" else (slit if state == "Open" else eye_look(state, slit, CB, CI, line=0.28))
        k.decal(E, de, (0.028, 0.02), look, eps=0.0025, rings=20, segs=56, mirror=True)

    k.build(voxel=0.004, smooth=3, band=0.004, wband=0.03)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    komala(*args())
