# Cyndaquil sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/cyndaquil.py -- <the export's Cyndaquil.glb> <out.glb>
# A small shrew standing hunched on two short legs, most of it a big round head: dark blue-green over the crown, the
# back of the head and down the back, cream below (the lower face, the throat, the belly, the legs) along one crisp
# line; a long tapering snout reaching forward, teal along its top to the tip and cream under it, a small dark nose;
# eyes that are always shut, bold dark arcs curving up; tiny arms held to the chest, short thick legs with two small
# grey claws a foot; and from the back a great burst of fire, thick tongues fanning up and back and out to the sides,
# orange-red with a yellow heart on both their faces.
#
# Written to be grown from (Quilava, Typhlosion import it with sibling("cyndaquil")): the line's colours, its hood of
# teal, its flames, eyes and claws are the functions below; the calls that build Cyndaquil are under __main__.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, inside, smoothstep, unit, outward, eye_look, args

# --- the line's colours -------------------------------------------------------------------------------------------
TEAL, CREAM = "#2B6676", "#F6E3A0"
CLAW = "#AEB6C8"
NOSE = "#4A2E2C"
FLAME, CORE = "#F4602A", "#FFD23A"
GLOW_FLAME, GLOW_CORE = "#FF6A2A", "#FFD860"
INK = srgb("#16181E")


def on_ell(c, r, d):
    """The point of an ellipsoid (centre c, radii r, unturned) along the direction d from its centre."""
    d = unit(*d)
    return np.array(c, float) + d / math.sqrt(((d / np.array(r, float)) ** 2).sum())


# --- the hood: teal above and behind a line drawn in the side view (y forward-back, z up) ---------------------------
def hood(ys, zs, edge=0.0035, below=None):
    """
    Teal where a point stands above the curve z(y) through (ys, zs) (ys increasing, front to back): a crisp line, the
    same across the model's width. below: (y, z) under which nothing is teal (the rump and legs stay cream).
    """
    ys, zs = np.array(ys, float), np.array(zs, float)
    def mask(P):
        m = inside(np.interp(P[:, 1], ys, zs) - P[:, 2], edge)
        if below is not None:
            m = m * smoothstep(below - 0.004, below + 0.004, P[:, 2])
        return m
    return mask


# --- the flames (moltres.py's thick ragged tongues, a bright heart on both faces) ----------------------------------
def frame(d, side):
    d = np.array(d, float); d /= np.linalg.norm(d)
    s = np.array(side, float); s -= d * np.dot(s, d); s /= np.linalg.norm(s)
    return np.stack([s, np.cross(d, s), d], axis=1)


class Flames:
    """Tongues of fire gathered by bone and colour, each group one glowing loose piece."""

    def __init__(self):
        self.groups, self.count = {}, 0

    def tongue(self, base, d, length, width, side, flick=0.3, thick=0.45, rag=0.26, waves=1.5, root=0.55):
        self.count += 1
        ph = 2.3 * self.count
        def shape(V):
            V = V.copy()
            t = np.clip((V[:, 2] + 1) / 2, 0, 1)
            w = (1 - t ** 1.1) ** 1.15 * (root + (1.55 - root) * np.sqrt(t))
            s = np.where(V[:, 0] >= 0, 1.0, -1.0)
            edge = 1 + rag * np.sin(2 * math.pi * waves * t + ph + s * 2.1) * smoothstep(0.1, 0.35, t)
            V[:, 0] *= w * edge; V[:, 1] *= w
            V[:, 0] += flick * (2.1 * t ** 2.4 - 0.55 * np.sin(math.pi * t))
            return V
        M = frame(d, side)
        V, F = ell((0, 0, 0), (width, width * thick, length / 2), deform=shape, sub=3)
        return outward((V @ M.T + np.array(base, float) + M[:, 2] * length / 2, F))

    def flame(self, base, d, length, width, bone, side, flick=0.3, heart=0.55):
        """A thick tongue of fire with its yellow heart laid on both faces."""
        b = np.array(base, float)
        d = unit(*d)
        self.groups.setdefault((bone, FLAME), []).append(self.tongue(b, d, length, width, side, flick))
        ny = frame(d, side)[:, 1]
        for sg in (1, -1):
            self.groups.setdefault((bone, CORE), []).append(
                self.tongue(b + ny * sg * width * 0.27, d, length * heart, width * 0.55, side, flick * 0.5, thick=0.3,
                            rag=0.15, root=0.3))

    def add(self, k):
        for i, ((bone, colour), shapes) in enumerate(self.groups.items()):
            V, F, off = [], [], 0
            for sv, sf in shapes:
                V.append(sv); F.extend([[j + off for j in f] for f in sf]); off += len(sv)
            k.add(f"flame{i}", (np.vstack(V), F), colour, bone, fuse=False,
                  glow=GLOW_FLAME if colour == FLAME else GLOW_CORE)
        self.groups = {}


# --- claws ----------------------------------------------------------------------------------------------------------
def claw(base, d, length, width, thick=None, up=(0, 0, 1)):
    """A small claw: a flat-faced solid from a kite at its root to its point along d."""
    d = unit(*d)
    a = np.cross(d, np.array(up, float)); a /= np.linalg.norm(a)
    b = np.cross(a, d)
    thick = thick if thick is not None else width * 0.8
    base = np.array(base, float)
    mid = base + d * length * 0.45
    return hull([base + a * width / 2, base - a * width / 2, base + b * thick / 2, base - b * thick / 2,
                 mid + a * width * 0.3, mid - a * width * 0.3, mid + b * thick * 0.3,
                 base + d * length - b * length * 0.2])


# --- the eyes that are always shut ----------------------------------------------------------------------------------
def arc_look(lid, ink, bow=0.62, line=0.27, tilt=0.0):
    """A shut eye as Cyndaquil's: a bold dark arc curving up (u outward, v up), the lid round it. tilt turns the arc
    down toward the nose (Fierce)."""
    def look(u, v):
        uu = u * math.cos(tilt) + v * math.sin(tilt)
        vv = v * math.cos(tilt) - u * math.sin(tilt)
        c = np.tile(lid, (len(u), 1))
        curve = -0.3 + bow * (1 - uu * uu)
        on = (np.abs(vv - curve) < line) & (np.abs(uu) < 0.9)
        c[on] = ink
        return c
    return look


def shut_eyes(k, E, de, half, lid, line=0.27):
    """The five states of an eye that is always shut: open is the arc, fierce the arc tilted down to the nose."""
    open_look = arc_look(lid, INK, line=line)
    def eyes(state):
        if state == "Fierce":
            look = arc_look(lid, INK, bow=0.45, line=line, tilt=-0.45)
        else:
            look = eye_look(state, open_look, lid, INK, line=line * 0.8)
        k.decal(E, de, half, look, eps=0.006, rings=20, segs=56, mirror=True)
    return eyes


# --- Cyndaquil ------------------------------------------------------------------------------------------------------
H, HR = np.array([0.0, 0.0, 0.318]), np.array([0.108, 0.104, 0.098])   # the head's crown
SNOUT = [(0, -0.07, 0.3), (0, -0.14, 0.287), (0, -0.195, 0.277)]
HOOD = hood([-0.3, -0.2, -0.12, -0.07, -0.02, 0.03, 0.065, 0.1, 0.4],
            [0.283, 0.293, 0.325, 0.372, 0.385, 0.345, 0.235, 0.15, 0.15], below=0.135)


def cyndaquil(src, out):
    k = Kit(src, coat="fur")

    # The head: a round crown, the long snout tapering forward out of its foot
    k.add("head", ell(H, HR), TEAL, "head")
    k.add("cheek", ell((0, -0.035, 0.282), (0.085, 0.08, 0.062)), CREAM, "head")
    k.add("snout", tube(SNOUT, [0.06, 0.043, 0.03]), CREAM, "head")
    k.add("nose", ell((0, -0.222, 0.277), (0.02, 0.014, 0.016)), NOSE, "head", fuse=False, surface="shell")

    # The body: a short pear leaning forward under the head, round low hips, the belly near the ground
    k.add("torso", ell((0, 0.005, 0.18), (0.098, 0.1, 0.11), rot=(-12, 0, 0)), CREAM, "body")
    k.add("hips", ell((0, 0.015, 0.105), (0.108, 0.1, 0.08)), CREAM, "body")
    k.add("back", ell((0, 0.05, 0.22), (0.085, 0.07, 0.09)), TEAL, "body")

    # The arms: tiny, held to the chest
    k.add("arm", tube([(0.07, -0.05, 0.19), (0.07, -0.088, 0.168), (0.058, -0.108, 0.152)], [0.026, 0.022, 0.02]),
          CREAM, "arm{s}", mirror=True)

    # The legs: fat haunches fused into the hips, a stub of shin, broad feet with two grey claws
    k.add("haunch", ell((0.068, 0.0, 0.085), (0.06, 0.072, 0.07)), CREAM, "leg{s}", mirror=True)
    k.add("shin", tube([(0.065, -0.005, 0.07), (0.067, -0.012, 0.03)], [0.042, 0.038]), CREAM, "leg{s}", mirror=True)
    k.add("foot", ell((0.068, -0.035, 0.02), (0.044, 0.062, 0.022)), CREAM, "leg{s}", mirror=True)
    for dx in (-0.016, 0.016):
        k.add(f"claw{dx:+.2f}", claw((0.068 + dx, -0.092, 0.014), (dx * 0.8, -1, -0.2), 0.022, 0.018, 0.013), CLAW,
              "leg{s}", fuse=False, mirror=True, surface="shell")

    # The teal hood over the crown, the snout's top and down the back
    k.paint(HOOD, TEAL)
    k.paint(lambda P: 1 - HOOD(P), CREAM)

    # The fire bursting from the back: a fan of thick tongues up, back and out to the sides
    f = Flames()
    B = np.array([0, 0.075, 0.25])
    for d, L, w, side, fl in [((0, 0.4, 1), 0.2, 0.085, (1, 0, 0), 0.25),
                              ((0, 1, 0.7), 0.26, 0.09, (1, 0, 0), 0.3),
                              ((0, 1, 0.12), 0.21, 0.08, (1, 0, 0), 0.3),
                              ((0.7, 0.5, 0.8), 0.2, 0.075, (0, 1, 0.3), 0.25),
                              ((-0.7, 0.5, 0.8), 0.2, 0.075, (0, 1, 0.3), 0.25),
                              ((0.8, 0.9, 0.1), 0.18, 0.07, (0, 1, 0.6), 0.25),
                              ((-0.8, 0.9, 0.1), 0.18, 0.07, (0, 1, 0.6), 0.25)]:
        f.flame(B + np.array(d) * np.array([0.06, 0.0, 0.0]), d, L, w, "flame", side, flick=fl)
    f.add(k)

    # The nose's dark tip seen from the front
    k.decal((0, -0.226, 0.277), (0, -1, 0.1), (0.015, 0.012), NOSE, eps=0.003, rings=4, segs=20)

    # The eyes: bold dark arcs curving up, high on the sides of the face, on the cream just under the hood's line
    de = unit(0.62, -0.72, 0.12)
    E = on_ell(H + (0, -0.01, -0.012), HR, de)
    eyes = shut_eyes(k, E, de, (0.036, 0.028), srgb(CREAM), line=0.32)

    k.build(voxel=0.006, smooth=4, band=0.006)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    cyndaquil(*args())
