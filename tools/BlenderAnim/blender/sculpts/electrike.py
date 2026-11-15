# Electrike sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/electrike.py -- <the export's Electrike.glb> <out.glb>
#
# The design, from the references: a low green dog, long in the body, with a big head carried forward; two broad
# lobes of fur swept straight back from the brow on each side of the crown, like ears laid flat; a yellow lightning
# mark from the snout's tip back over the eye to under the lobes; small fierce eyes, white with a black pupil and a
# heavy black upper line; the jaw hanging open on a pink mouth with two white fangs; a tuft of fur on the chest and
# a spike on the back; short thick legs with broad paws; a tail like a bolt rising up and back, green below and yellow
# above with a zag, and a lower green spike behind it.
# Manectric is grown from this script's frame (sibling("electrike")): the eye, the spikes and the tail's bolt.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, blade, hull, slab, srgb, inside, smoothstep, unit, eye_look, polygon, boxy, args)

GREEN, YELLOW = "#7FCB7A", "#F4D45A"
MOUTH, THROAT, FANG = "#E27E8C", "#A8404E", "#F4F2EA"
EYEWHITE, PUPIL, INK = "#F6F6F0", "#16161C", "#1E2220"


def in_poly(x, y, pts):
    pts = np.array(pts, float)
    res = np.zeros(len(x), bool)
    for (x1, y1), (x2, y2) in zip(pts, np.roll(pts, -1, axis=0)):
        res ^= ((y1 > y) != (y2 > y)) & (x < (x2 - x1) * (y - y1) / (y2 - y1 + 1e-12) + x1)
    return res


def poly_mask(x, y, pts, edge=0.003):
    """A polygon painted with a soft edge: the signed distance to its outline (negative inside), eased."""
    pts = np.array(pts, float)
    d = np.full(len(x), 9.0)
    for (x1, y1), (x2, y2) in zip(pts, np.roll(pts, -1, axis=0)):
        ex, ey = x2 - x1, y2 - y1
        t = np.clip(((x - x1) * ex + (y - y1) * ey) / (ex * ex + ey * ey + 1e-12), 0, 1)
        d = np.minimum(d, np.hypot(x - x1 - t * ex, y - y1 - t * ey))
    return inside(np.where(in_poly(x, y, pts), -d, d), edge)


def spike(a, b, wx, wy, up=(0, 0, 1)):
    """A solid spike from a (its foot, a kite wx across x-ish and wy across the other way) to the point b: a hull."""
    a, b = np.array(a, float), np.array(b, float)
    d = b - a; d /= np.linalg.norm(d)
    e1 = np.cross(d, up)
    if np.linalg.norm(e1) < 1e-3:
        e1 = np.array([1.0, 0, 0])
    e1 /= np.linalg.norm(e1)
    e2 = np.cross(e1, d)
    m = a + (b - a) * 0.35
    return hull([a + e1 * wx, a - e1 * wx, a + e2 * wy, a - e2 * wy,
                 m + e1 * wx * 0.7, m - e1 * wx * 0.7, m + e2 * wy * 0.7, m - e2 * wy * 0.7, b])


def almond(th):
    """An almond: a top arc flatter than the bottom, pointed at both corners."""
    c, s = math.cos(th), math.sin(th)
    return 1.0 / max((abs(c) ** 1.7 + abs(s) ** 1.7) ** (1 / 1.7), 1e-6) * (0.9 if s > 0 else 1.0)


def make_eye(white, pupil, ink, pu=-0.18, pr=(0.46, 0.62), top=lambda u: 0.42 + 0.3 * u, glint=True):
    """The open eye's colour function (u outward, v up): a white almond, a round pupil toward the nose, a heavy dark
    line along the top that slants down toward the nose (the scowl), a small glint in the pupil."""
    W, P, I = srgb(white), srgb(pupil), srgb(ink)
    def eye(u, v):
        c = np.tile(W, (len(u), 1))
        c[((u - pu) / pr[0]) ** 2 + ((v + 0.02) / pr[1]) ** 2 < 1] = P
        if glint:
            c[((u - pu - 0.14) / 0.14) ** 2 + ((v - 0.22) / 0.16) ** 2 < 1] = (1, 1, 1)
        c[v > top(u)] = I
        return c
    return eye


def fierce(open_look, lid, ink, top=lambda u: 0.42 + 0.3 * u):
    """Fierce on an eye that already scowls: the dark top line brought lower toward the nose."""
    def look(u, v):
        c = open_look(u, v)
        edge = np.minimum(top(u), 0.1 + 0.55 * u)
        c[v > edge] = ink
        return c
    return look


def lobe_frame(side, out_tilt):
    """The plane a lobe lies in: straight back and level along it, its face turned out (and a little up) to the side."""
    ex = unit(0, 1, 0.0)
    ez = unit(out_tilt * side, 0, 1)
    return ex, ez


def leaf_outline(start, length, half, rise, n=14):
    """A rounded leaf in a lobe's plane (a back along it, b up across it): from a (buried in the crown) to its point at
    start + length, its middle line rising by rise per unit back, broadest a third of the way along."""
    a = np.linspace(start, start + length, n)
    t = (a - start) / length
    w = half * np.sin(np.pi * np.clip(t, 0, 1) ** 0.75) ** 0.6
    mid = rise * a
    top = [(float(x), float(m + h)) for x, m, h in zip(a, mid, w)]
    bot = [(float(x), float(m - h)) for x, m, h in zip(a, mid, w)][::-1]
    return top + bot[1:-1]


# The two lobes of each side, pivoted at the crown and laid nearly flat: broad rounded leaves sweeping straight back
# over the neck and shoulders, their middle lines rising some 7 degrees, their points over the middle of the back; the
# lower one under and outside the upper, like a second layer
UPPER = leaf_outline(-0.07, 0.43, 0.055, 0.12)
LOWER = leaf_outline(-0.05, 0.36, 0.045, 0.1)
# The yellow bolt laid on the lower lobe's outer face, from behind the eye to the lobe's point: a wide zigzag band
# along the leaf's middle line
_ZB = [(0.0, 0.016), (0.095, 0.03), (0.08, 0.012), (0.195, 0.028), (0.18, 0.01), (0.305, 0.004),
       (0.16, -0.012), (0.175, 0.006), (0.06, -0.012), (0.075, 0.004), (0.0, -0.016)]
BOLT = [(a, b + 0.1 * a) for a, b in _ZB]
UPPER_ROOT = np.array((0.045, -0.24, 0.3))
LOWER_ROOT = np.array((0.075, -0.22, 0.255))


def electrike(src, out):
    k = Kit(src, coat="fur")

    # --- trunk: a few large smooth masses, long and low, deeper in front ----------------------------------------
    k.add("trunk", ell((0, 0.01, 0.152), (0.084, 0.165, 0.078)), GREEN, "body")
    k.add("chest", ell((0, -0.09, 0.158), (0.09, 0.09, 0.088)), GREEN, "body")

    # --- short thick legs: broad masses fused into the flanks, a stub of shin, wide paws -------------------------
    for name, y, bone in (("fore", -0.085, "leg{s}"), ("hind", 0.1, "hind{s}")):
        k.add(name + "top", ell((0.058, y, 0.12), (0.052, 0.066, 0.07)), GREEN, bone, mirror=True)
        k.add(name + "shin", tube([(0.064, y, 0.11), (0.068, y - 0.004, 0.035)], [0.044, 0.04]), GREEN, bone,
              mirror=True)
        k.add(name + "paw", ell((0.07, y - 0.022, 0.022), (0.043, 0.052, 0.024)), GREEN, bone, mirror=True)

    # --- the head: one long low wedge carried forward on a thick neck, tapering to the snout; the jaw open -------
    k.add("neck", ell((0, -0.14, 0.205), (0.078, 0.08, 0.075)), GREEN, "head")
    def wedge(V):
        V = V.copy(); t = np.clip((-V[:, 1] + 1) / 2, 0, 1)      # 0 at the back, 1 at the front
        V[:, 0] *= 1 - 0.35 * t; V[:, 2] *= 1 - 0.3 * t
        return V
    k.add("head", ell((0, -0.225, 0.252), (0.082, 0.125, 0.066), deform=wedge), GREEN, "head")
    k.add("snout", ell((0, -0.32, 0.24), (0.044, 0.07, 0.032), deform=wedge), GREEN, "head")
    k.add("jaw", ell((0, -0.29, 0.172), (0.042, 0.065, 0.022), rot=(-12, 0, 0)), GREEN, "head")
    k.add("jawroot", ell((0, -0.225, 0.19), (0.06, 0.055, 0.04)), GREEN, "head")
    # the open mouth: a pink lining set back between the jaws, darker at the throat
    k.add("mouth", ell((0, -0.285, 0.205), (0.038, 0.055, 0.028)), MOUTH, "head")
    k.add("throat", ell((0, -0.245, 0.208), (0.028, 0.02, 0.02)), THROAT, "head")
    for sx in (1, -1):
        k.add(f"fang{'R' if sx > 0 else 'L'}", spike((0.025 * sx, -0.348, 0.222), (0.027 * sx, -0.352, 0.19), 0.011,
                                                       0.009), FANG, "head", fuse=False, surface="shell")

    # --- the lobes: thick flat pointed plates swept straight back and a little up from the crown, the upper pair
    #     reaching past the shoulders, the lower pair laid over them outside; the yellow bolt on the lower ----------
    for side, nm in ((1, "R"), (-1, "L")):
        m = np.array((side, 1, 1.0))
        ex, ez = lobe_frame(side, 0.4)
        k.add("lobeup" + nm, slab(UPPER, UPPER_ROOT * m, ex, ez, 0.03), GREEN, "ear" + nm, fuse=False)
        ex, ez = lobe_frame(side, 0.4)
        n = np.cross(unit(*ex), ez - unit(*ex) * (ez @ unit(*ex))); n /= np.linalg.norm(n)
        if n[0] * side < 0:
            n = -n
        k.add("lobelow" + nm, slab(LOWER, LOWER_ROOT * m, ex, ez, 0.03), GREEN, "head", fuse=False)
        k.add("bolt" + nm, slab(BOLT, LOWER_ROOT * m + n * 0.017, ex, ez, 0.008), YELLOW, "head", fuse=False)
    # a fused fill under the lobes' roots, so the crown flows into them
    k.add("crown", ell((0, -0.19, 0.29), (0.065, 0.11, 0.035)), GREEN, "head")

    # --- tufts: a few short spikes of fur down the chest, one up the back ---------------------------------------
    for i, (x, y, z, d) in enumerate(((0.0, -0.21, 0.12, (0, -0.35, -1)), (0.038, -0.2, 0.125, (0.4, -0.3, -1)))):
        k.add(f"tuft{i}", blade([(x, y, z + 0.02), (x * 1.2, y - 0.01, z - 0.012), tuple(np.array((x, y, z)) + unit(*d) * 0.05)],
                                [0.026, 0.017, 0.003], (0, 1, 0), 0.5), GREEN, "body", mirror=i > 0)
    k.add("backspike", spike((0.0, 0.08, 0.21), (0.0, 0.15, 0.3), 0.022, 0.03), GREEN, "body", fuse=False)

    # --- the tail: a bolt rising up and back, green below, yellow above with a zag forward; a lower green spike ---
    k.add("tailroot", ell((0, 0.165, 0.2), (0.04, 0.045, 0.045)), GREEN, "tail")
    TA, TB = np.array((0, 0.17, 0.21)), np.array((0.006, 0.27, 0.39))
    k.add("tail", spike(TA, TB, 0.03, 0.036), GREEN, "tail", fuse=False)
    k.add("tailbolt", spike(TA + (TB - TA) * 0.5, TB + (TB - TA) * 0.03, 0.03 * 0.73, 0.036 * 0.73), YELLOW, "tail",
          fuse=False)
    k.add("tailzag", spike(TA + (TB - TA) * 0.55, TA + (TB - TA) * 0.55 + np.array((0, -0.05, 0.02)), 0.014, 0.018),
          YELLOW, "tail", fuse=False)
    k.add("tailspur", spike((0, 0.19, 0.19), (0.0, 0.31, 0.29), 0.026, 0.03), GREEN, "tail", fuse=False)

    # --- the yellow snout tip: the top of the snout's point -----------------------------------------------------
    k.paint(lambda P: inside(P[:, 1] + 0.335, 0.003) * inside(0.226 - P[:, 2], 0.003), YELLOW)

    # --- the eyes: under the lobes' front, white with a black pupil toward the nose and a heavy top line ---------
    de = unit(0.66, -0.7, 0.2)
    E = np.array([0.058, -0.285, 0.27])
    look = make_eye(EYEWHITE, PUPIL, INK)
    G, I = srgb(GREEN), srgb(INK)
    def eyes(state):
        f = fierce(look, G, I) if state == "Fierce" else eye_look(state, look, G, I)
        k.decal(E, de, (0.025, 0.02), f, up=unit(0.25, 0, 1), eps=0.004, outline=almond, rings=20, segs=56,
                mirror=True)

    k.build(voxel=0.005, smooth=8)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    electrike(*args())
