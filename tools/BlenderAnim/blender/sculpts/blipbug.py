# Blipbug sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/blipbug.py -- <the export's Blipbug.glb> <out.glb>
# Written to be grown from (dottler.py and orbeetle.py take its colours, its spike and its way of making eyes).
#
# The design, from the references: a great egg of a head, the largest mass (half the height), navy over its upper two
# thirds and pale yellow round its lower third; two huge tall oval eyes side by side on its front like the lenses of a
# pair of glasses, across the navy and yellow line, pale grey with a darker grey lower half, a thin navy rim and no
# pupil; a tuft of two long navy spikes splayed in a V on the crown, a little back; a small V of two navy prongs on each
# side of the head. Under the chin a navy collar and two small yellow-orange mandibles like a bow tie, two small cream
# hands under them; a short body of two segments trailing back low, the front one yellow-orange with two cream feet,
# the back one navy with a small V of a tail rising from its end. The art tilts the head and curls the body; here the
# skeleton stands it upright.
# Skeleton: body (0, 0, 0.1), head (0, 0, 0.15), arms (+-0.04, -0.03, 0.11), legs (+-0.03, -0.01, 0.04),
# tail (0, 0.04, 0.06). The export stands 0.34 tall.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, hull, srgb, inside, smoothstep, unit, axes, eye_look, args

NAVY, YELLOW, ORANGE, CREAM = "#3E4596", "#F3E6A8", "#F2A631", "#F1EAD0"
MANDIBLE = "#F4B92E"
LENS, LENS_LOW, RIM, GLINT = "#EEECF0", "#A9A0AC", "#2A2D6A", "#FFFFFF"
E = 0.0025                                      # a crisp paint's edge at this size

HC = np.array([0.0, -0.006, 0.232])           # the head's middle
HR = np.array([0.086, 0.079, 0.098])          # its radii (a tenth over the reference's)
LINE = 0.197                                  # the navy above, the pale yellow below


def on_ell(d, c, r):
    """A point of an ellipsoid in a direction, and the outward normal there."""
    d = np.array(d, float); d /= np.linalg.norm(d)
    p = c + d / math.sqrt(((d / r) ** 2).sum())
    n = (p - c) / r ** 2
    return p, n / np.linalg.norm(n)


def spike(k, name, root, d, length, w, bone, color=NAVY, mirror=False, t=None):
    """A solid pointed spike: a kite-sectioned hull from a root w across to a point, along d."""
    root = np.array(root, float); d = unit(*d)
    a, b = axes(d)
    t = w * 0.7 if t is None else t
    pts = [root - d * w * 0.4 + a * w / 2, root - d * w * 0.4 - a * w / 2, root - d * w * 0.4 + b * t / 2,
           root - d * w * 0.4 - b * t / 2, root + d * length * 0.35 + a * w * 0.38, root + d * length * 0.35 - a * w * 0.38,
           root + d * length]
    k.add(name, hull(pts), color, bone, fuse=False, mirror=mirror)


def prongs(k, name, root, d1, d2, length, w, bone, color=NAVY, mirror=False):
    """A V of two spikes from one root: the line's tuft, its side prongs and its tail."""
    spike(k, name + "a", root, d1, length, w, bone, color, mirror)
    spike(k, name + "b", root, d2, length * 0.82, w * 0.9, bone, color, mirror)


def lens(u, v, upper=srgb(LENS), lower=srgb(LENS_LOW), rim=srgb(RIM), glint=srgb(GLINT), rim_w=0.13):
    """The line's eye as Blipbug has it: a lens, pale above and grey below a curved line, a thin rim, a glint."""
    r = np.sqrt(u * u + v * v)
    c = np.tile(upper, (len(u), 1))
    c[v < -0.12 + 0.18 * u * u] = lower
    c[(u - 0.42) ** 2 / 0.16 ** 2 + (v - 0.42) ** 2 / 0.24 ** 2 < 1] = glint
    c[r > 1 - rim_w] = rim
    return c


def rimmed(look, rim=srgb(RIM), rim_w=0.13):
    """Whatever the eye does, the lens keeps its rim: shut, it is a grey lens with a dark line across it."""
    def f(u, v):
        c = look(u, v)
        c[np.sqrt(u * u + v * v) > 1 - rim_w] = rim
        return c
    return f


def split_lid(look, v_line, above, below):
    """A shut eye across a colour line: the lid takes the colour on each side of it (no oval when it blinks)."""
    A = srgb(above)
    def f(u, v):
        c = look(u, v)
        lid = np.all(np.isclose(c, A[None]), axis=1) & (v < v_line)
        c[lid] = srgb(below)
        return c
    return f


def head(k):
    k.add("head", ell(HC, HR, sub=5), NAVY, "head")
    k.paint(lambda P: inside(P[:, 2] - LINE, 0.0015) * inside(np.linalg.norm((P - HC) / HR, axis=1) - 1.25, 0.004)
            * inside(0.15 - P[:, 2], 0.004), YELLOW)
    # the tuft: two long spikes splayed in a V from the crown, a little back
    top = HC + np.array([0, 0.012, HR[2] * 0.93])
    spike(k, "tuftR", top, (0.42, 0.25, 1), 0.078, 0.024, "head")
    spike(k, "tuftL", top + (0, 0.004, 0), (-0.5, 0.42, 1), 0.066, 0.022, "head")
    # a small V of prongs on each side of the head
    p, n = on_ell((1, 0.25, 0.32), HC, HR)
    prongs(k, "side", p - n * 0.008, (1, 0.35, 0.55), (1, 0.45, -0.05), 0.056, 0.021, "head", mirror=True)


def under(k):
    # the navy collar under the chin, two mandibles like a bow tie, two small cream hands
    k.add("collar", ell((0, -0.004, 0.137), (0.05, 0.046, 0.026)), NAVY, "head")
    for s in (1, -1):
        c = np.array([0.012 * s, -0.052, 0.142])
        tip = c + np.array([0.034 * s, -0.014, 0.0])
        k.add("mandR" if s > 0 else "mandL",
              hull([c + (0, 0.008, 0.012), c + (0, 0.008, -0.012), c + (0, -0.006, 0.0), tip + (0, 0, 0.012),
                    tip + (0, 0, -0.012), tip + (0.006 * s, 0.006, 0)]), MANDIBLE, "head", fuse=False)
    k.add("hand", ell((0.03, -0.044, 0.104), (0.019, 0.019, 0.019)), CREAM, "arm{s}", mirror=True)


def body(k):
    k.add("front", ell((0, 0.004, 0.058), (0.058, 0.054, 0.054)), ORANGE, "body")
    k.add("chest", ell((0, -0.02, 0.096), (0.036, 0.03, 0.03)), CREAM, "body")
    k.add("foot", ell((0.032, -0.036, 0.022), (0.02, 0.022, 0.021)), CREAM, "leg{s}", mirror=True)
    k.add("rear", ell((0, 0.066, 0.046), (0.046, 0.046, 0.044)), NAVY, "tail")
    k.add("tailend", ell((0, 0.1, 0.05), (0.03, 0.03, 0.03)), NAVY, "tail")
    prongs(k, "tail", (0, 0.11, 0.062), (0.35, 0.55, 1), (-0.3, 0.75, 0.75), 0.06, 0.018, "tail")
    # the back segment's navy meets the orange in a crisp line
    k.paint(lambda P: inside(P[:, 1] - 0.03, E) * inside(P[:, 2] - 0.11, E), ORANGE, on=["front", "rear"])


EYE_D = (0.41, -0.91, -0.18)
EYE_HALF = (0.034, 0.052)


def eyes(k, state, d=EYE_D, half=EYE_HALF, c=HC, r=HR):
    p, n = on_ell(d, c, r)
    v_line = (LINE - p[2]) / half[1]
    look = rimmed(eye_look(state, lens, srgb(LENS_LOW), srgb(RIM), line=0.17))
    k.decal(p, n, half, look, eps=0.002, rings=20, segs=56, mirror=True)


def blipbug(src, dst):
    k = Kit(src, coat="shell")
    head(k)
    under(k)
    body(k)
    k.build(voxel=0.004, smooth=4, band=0.006, wband=0.025)
    k.eye_states(lambda s: eyes(k, s))
    k.export(dst)


if __name__ == "__main__":
    blipbug(*args())
