# Goodra sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/goodra.py -- <the export's Goodra.glb> <out.glb>
#
# Goomy and Sliggoo grown (sibling("goomy"), sibling("sliggoo")): the colours, the green spots, the horns and feelers
# with their round knobs, Sliggoo's green eye.
# From the references (drawn from the front three-quarter): a tall plump dragon of lavender slime, a pear widest at
# the hips sitting low on short stubby legs with greyish feet, a long thick neck running up into a large head carried
# forward; a purple stripe down the front from under the chin to between the legs; a thick tail sweeping back and out,
# curling up at its tip, purple beneath with a row of green spots along each side; short arms held forward and down,
# flat mitts dripping green slime; green drips from the corners of the jaw; a short rounded snout with a small smile;
# green eyes with a dark pupil and a glint; a row of green dots under each eye; two short horns on the head's top front;
# two great feelers from the top of the head, thick at the root, rising, arching back and drooping down the back,
# each curling at its end. (The task's "antennae that curl": they curl only at their ends, after hanging down.)
# Skeleton: body (0, 0, 0.28), head (0, -0.06, 0.56), ears (+-0.03, -0.06, 0.65), arms (+-0.1, -0.04, 0.36),
# legs (+-0.07, 0, 0.15), tail (0, 0.08, 0.18). The export is 0.74 tall.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, outward, srgb, inside, unit, eye_look, args, sibling

g = sibling("goomy")
sl = sibling("sliggoo")
LAVENDER, PURPLE, LINE, INK = "#D8C6EC", g.PURPLE, g.LINE, g.INK
FOOT, SLIME = "#B2A2C8", "#A8D86C"
EYE, EYE_RIM = sl.EYE, sl.EYE_RIM

HEAD, HEADR = np.array([0.0, -0.09, 0.635]), np.array([0.095, 0.105, 0.088])
SNOUT, SNOUTR = HEAD + (0, -0.083, -0.03), np.array([0.07, 0.062, 0.054])
TAIL = [(0.0, 0.08, 0.21), (0.04, 0.21, 0.14), (0.12, 0.31, 0.115), (0.2, 0.37, 0.15), (0.235, 0.375, 0.23),
        (0.215, 0.345, 0.285), (0.18, 0.33, 0.27)]
TAILR = [0.125, 0.09, 0.066, 0.05, 0.038, 0.031, 0.027]


def drip(k, name, top, length, r, bone):
    """A drip of green slime: a short neck swelling to a round drop (loose)."""
    top = np.array(top, float)
    k.add(name, outward(tube([top, top + (0, -0.002, -length * 0.5), top + (0, -0.004, -length)],
                             [r * 0.85, r * 0.8, r * 1.2], seg=14)), SLIME, bone, fuse=False)


def under(path, radii, share=0.15, start=0):
    """The purple under a tube's line: points lower than its centre by more than share of its radius."""
    fine = []
    for a, b, ra, rb in zip(path[:-1], path[1:], radii[:-1], radii[1:]):
        for t in np.linspace(0, 1, 30, endpoint=False):
            fine.append((*(np.array(a) + (np.array(b) - np.array(a)) * t), ra + (rb - ra) * t))
    fine = np.array(fine)
    C, R = fine[:, :3], fine[:, 3]
    def mask(P):
        j = np.concatenate([((P[a:a + 3000, None, :] - C[None]) ** 2).sum(2).argmin(1) for a in range(0, len(P), 3000)])
        d = np.linalg.norm(P - C[j], axis=1)
        return inside(P[:, 2] - C[j, 2] + share * R[j], 0.003) * (j >= start) * (d < R[j] * 1.6)
    return mask


def belly(P):
    """The purple stripe down the front: narrow under the chin, broad over the belly, narrowing between the legs."""
    z = P[:, 2]
    w = np.interp(z, [0.05, 0.14, 0.27, 0.42, 0.52, 0.6], [0.0, 0.07, 0.105, 0.075, 0.05, 0.0])
    return inside(np.abs(P[:, 0]) - w, 0.0025) * (P[:, 1] < -0.03) * (z > 0.06) * (z < 0.6)


def eye_open(u, v):
    """Green with a dark rim, a black pupil and a white glint."""
    c = np.tile(srgb(EYE), (len(u), 1))
    c[u * u + v * v > 0.8 ** 2] = srgb(EYE_RIM)
    c[((u + 0.05) / 0.36) ** 2 + ((v + 0.02) / 0.46) ** 2 < 1] = srgb(INK)
    c[((u - 0.16) / 0.18) ** 2 + ((v - 0.28) / 0.18) ** 2 < 1] = np.array([1.0, 1.0, 1.0])
    return c


def smile(u, v):
    c = np.tile(srgb(LAVENDER), (len(u), 1))
    c[(np.abs(v - (0.6 * u * u - 0.25)) < 0.22) & (np.abs(u) < 0.95)] = srgb(LINE)
    return c


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="scales")

    # The pear: broad hips low to the ground, a narrower chest, a thick neck
    k.add("torso", ell((0, 0.012, 0.245), (0.152, 0.142, 0.205)), LAVENDER, "body")
    k.add("chest", ell((0, -0.02, 0.415), (0.108, 0.102, 0.11)), LAVENDER, "body")
    k.add("neck", tube([(0, -0.025, 0.45), (0, -0.045, 0.53), (0, -0.07, 0.6)], [0.085, 0.07, 0.064]), LAVENDER,
          "head")

    # The legs: thighs fused into the hips, a stub and a greyish foot
    k.add("hip", ell((0.072, -0.005, 0.13), (0.088, 0.1, 0.11)), LAVENDER, "leg{s}", mirror=True)
    k.add("foot", ell((0.085, -0.04, 0.03), (0.062, 0.08, 0.032)), FOOT, "leg{s}", mirror=True)

    # The head: large, carried forward, a short rounded snout, two short horns
    k.add("head", ell(HEAD, HEADR), LAVENDER, "head")
    k.add("snout", ell(SNOUT, SNOUTR), LAVENDER, "head")
    for s in (1, -1):
        m = np.array([s, 1, 1.0]); nm = "R" if s > 0 else "L"
        k.add(f"horn{nm}", g.feeler(np.array([(0.04, -0.13, 0.7), (0.048, -0.136, 0.73), (0.054, -0.138, 0.752)])
                                    * m, [0.018, 0.013, 0.0085], bulb=1.0), LAVENDER, "head", fuse=False)
        # The feelers: thick at the root, up, back and down the back, swept out, curling at the end
        k.add(f"feeler{nm}", g.feeler(np.array([(0.035, -0.07, 0.7), (0.05, -0.035, 0.75), (0.08, 0.03, 0.765),
                                                (0.12, 0.1, 0.72), (0.15, 0.16, 0.63), (0.165, 0.2, 0.54),
                                                (0.175, 0.22, 0.47), (0.195, 0.2, 0.425), (0.21, 0.165, 0.44)]) * m,
                                      [0.036, 0.034, 0.031, 0.027, 0.023, 0.02, 0.018, 0.017, 0.016], bulb=1.15),
              LAVENDER, f"ear{nm}", fuse=False)

    # The arms: short, forward and down, elbows bent, flat mitts dripping green
    k.add("arm", tube([(0.095, -0.03, 0.385), (0.122, -0.09, 0.375), (0.115, -0.16, 0.35)], [0.044, 0.037, 0.031]),
          LAVENDER, "arm{s}", mirror=True)
    k.add("mitt", ell((0.11, -0.195, 0.335), (0.042, 0.04, 0.022), rot=(-22, 0, 8)), LAVENDER, "arm{s}", mirror=True)
    for s in (1, -1):
        nm = "R" if s > 0 else "L"
        for j, (x, y, ln) in enumerate(((0.095, -0.205, 0.05), (0.13, -0.19, 0.038))):
            drip(k, f"handdrip{nm}{j}", (x * s, y, 0.33), ln, 0.012, f"arm{nm}")
        drip(k, f"chindrip{nm}", (0.06 * s, -0.15, 0.585), 0.05, 0.012, "head")

    # The tail: thick, back and out to +X, curling up at its tip
    k.add("tail", tube(TAIL, TAILR), LAVENDER, "tail")

    # The purple: the stripe down the front, the tail beneath, the feet's line kept crisp
    k.paint(belly, PURPLE, on=["torso", "chest", "neck", "hip"])
    k.paint(under(TAIL, TAILR, share=0.1, start=12), PURPLE, on=["tail"])

    # Green spots along each side of the tail, over the purple's edge
    for s in (1, -1):
        for t in (0.45, 0.95, 1.45, 1.9):
            i = int(t); f = t - i
            c = np.array(TAIL[i]) + (np.array(TAIL[i + 1]) - np.array(TAIL[i])) * f
            r = TAILR[i] + (TAILR[i + 1] - TAILR[i]) * f
            d = np.array(TAIL[i + 1]) - np.array(TAIL[i]); d[2] = 0; d /= np.linalg.norm(d)
            side = np.cross(d, (0, 0, 1.0)) * s
            n = unit(*(side + (0, 0, -0.35)))
            k.decal(c + n * r * 0.95, n, (0.018, 0.022), g.spot_look(), eps=0.002, rings=12, segs=48,
                    up=tuple(d))

    # A row of green dots under each eye, the smile on the snout
    for s in (1, -1):
        for d, h in (((0.62, -0.55, -0.3), 0.011), ((0.75, -0.38, -0.38), 0.011), ((0.84, -0.18, -0.4), 0.01),
                     ((0.88, 0.02, -0.35), 0.009)):
            p, n = g.on(HEAD, HEADR, (d[0] * s, d[1], d[2]))
            g.spot(k, p, n, (h, h * 1.15), eps=0.0018)
    p, n = g.on(SNOUT, SNOUTR, (0, -0.85, -0.45))
    k.decal(p, unit(0, -0.9, -0.4), (0.04, 0.017), smile, eps=0.002, rings=10, segs=48)

    k.build(voxel=0.0065, smooth=4, band=0.009, wband=0.05)

    CL = srgb(LAVENDER)

    def eyes(state):
        for s in (1, -1):
            p, n = g.on(HEAD, HEADR, (0.55 * s, -0.76, 0.22))
            k.decal(p, n, (0.022, 0.026), g.mirrored(eye_look(state, eye_open, CL, srgb(INK), line=0.17), s),
                    eps=0.003, rings=20, segs=56)

    k.eye_states(eyes)
    k.export(out)
