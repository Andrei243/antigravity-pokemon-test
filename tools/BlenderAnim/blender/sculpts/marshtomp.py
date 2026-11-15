# Marshtomp sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown from
# mudkip.py (its blue, its orange, its thick fins and hard-rimmed gills):
#   blender --background --factory-startup --python sculpts/marshtomp.py -- <the export's Marshtomp.glb> <out.glb>
#
# The design, from the references: an upright light-teal mudfish, a broad head that is one mass with a stout body;
# one dark slate fin on the top of the head, swept back like a sickle; round orange cheeks, each with a long orange
# spike pointing out sideways; a very wide open mouth, pink inside, under small orange eyes with black pupils set high
# and close; a big orange oval on the belly; thick arms ending in broad flat hands of three thick fingers (raised in
# the pictures, held forward and down here); short thick legs on round feet; a broad dark slate tail fin behind,
# wider than the body, its two lower lobes showing past the legs from the front. (The brief expected two head fins
# and a cream belly: the pictures show one head fin and an orange belly.)
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, slab, srgb, inside, unit, eye_look, polygon, sibling, args

mk = sibling("mudkip")
TEAL, ORANGE, SLATE, RAY = "#6ACCD8", mk.ORANGE, "#4C6886", "#6A88A6"
MOUTH, LIP, INK, IRIS = "#E9848E", "#8A2C3C", mk.INK, "#F08A24"
EDGE = mk.EDGE

# --- the frame ---------------------------------------------------------------------------------------------------------
H = np.array([0, -0.035, 0.43])          # the head's middle
HR = np.array([0.158, 0.13, 0.12])
BC = np.array([0, 0.0, 0.215])            # the body's middle
BR = np.array([0.118, 0.105, 0.15])
EYE_D = unit(0.36, -0.76, 0.54)
EYE_HALF = (0.021, 0.024)
VOXEL = 0.006


def pear(V):
    V = V.copy()
    s = 1 + 0.12 * np.clip(-V[:, 2], 0, 1) ** 0.8 - 0.08 * np.clip(V[:, 2], 0, 1)
    V[:, 0] *= s; V[:, 1] *= s
    return V


def head_body(k, h=H, hr=HR, bc=BC, br=BR, col=TEAL):
    """A broad head, a thick neck into a stout body widest low down: one mass."""
    def flat(V):
        V = V.copy()
        V[:, 0] *= 1 + 0.06 * np.clip(-V[:, 2], 0, 1)             # full at the cheeks
        V[:, 2] = np.where(V[:, 2] > 0, V[:, 2] * 0.9, V[:, 2])     # a flatter top
        return V
    k.add("head", ell(h, hr, deform=flat), col, "head")
    k.add("muzzle", ell((0, h[1] - hr[1] * 0.45, h[2] - hr[2] * 0.3), (hr[0] * 0.82, hr[1] * 0.62, hr[2] * 0.62)), col, "head")
    k.add("neck", ell((0, -0.01, (h[2] + bc[2]) / 2 + 0.03), (hr[0] * 0.8, hr[1] * 0.85, 0.09)), col, "body")
    k.add("body", ell(bc, br, deform=pear), col, "body")


def eye_open(u, v):
    """An orange eye with a black upright pupil, a dark rim and a white glint."""
    c = np.tile(srgb(IRIS), (len(u), 1))
    c[(u / 0.34) ** 2 + (v / 0.62) ** 2 < 1] = srgb(INK)
    c[((u - 0.2) / 0.2) ** 2 + ((v - 0.32) / 0.2) ** 2 < 1] = (1, 1, 1)
    c[u * u + v * v > 0.8] = srgb(INK)
    return c


def mouth(k, h=H, z0=None, aw=1.15, depth=0.105, lip=0.0):
    """A very wide open mouth painted round the head's upright axis: its upper edge nearly level, turned up a little at
    the corners, its lower edge a deep curve; dark red under the lip, pink below."""
    z0 = h[2] - 0.006 if z0 is None else z0
    def shape(P):
        q = P - h
        a = np.arctan2(q[:, 0], -q[:, 1])
        top = z0 + 0.012 * (a / aw) ** 2
        w = np.clip(1 - (a / aw) ** 2, 0, 1)
        bot = top - depth * w ** 0.55
        m = inside(P[:, 2] - top, 0.003) * inside(bot - P[:, 2], 0.003) * inside(np.abs(a) - aw, 0.02)
        return m * (P[:, 1] < h[1] - 0.02), top
    k.paint(lambda P: shape(P)[0], MOUTH)
    if lip:
        k.paint(lambda P: shape(P)[0] * inside(shape(P)[1] - lip - P[:, 2], 0.003), LIP)


def cheek(k, at, r, spike_len, spike_r, spike_dir, col=ORANGE, bone="head", name="cheek"):
    """A round orange cheek pad (a loose flattened ball on the side of the head) and a long spike out of it."""
    at = np.array(at, float)
    k.add(name, ell(at, r, rot=(0, 0, -25)), col, bone, fuse=False, mirror=True, surface="skin")
    d = unit(*spike_dir)
    a = np.cross(d, (0, 0, 1.0)); a /= np.linalg.norm(a)
    b = np.cross(d, a)
    root = at + d * r[0] * 0.3
    pts = [root + spike_r * (math.cos(t) * a + math.sin(t) * b) for t in np.linspace(0, 2 * math.pi, 7)[:-1]]
    pts.append(root + d * spike_len)
    k.add(name + "spike", hull(pts), col, bone, fuse=False, mirror=True, surface="skin")


def arms(k, col=TEAL):
    """Thick arms held forward and down, broad flat hands of three thick fingers."""
    k.add("arm", tube([(0.105, 0.0, 0.37), (0.165, -0.035, 0.32), (0.195, -0.085, 0.27)], [0.05, 0.045, 0.042]),
          col, "arm{s}", mirror=True)
    k.add("hand", ell((0.205, -0.115, 0.25), (0.058, 0.036, 0.056), rot=(0, 0, 25)), col, "arm{s}", mirror=True)
    for i, (dx, dz) in enumerate(((-0.034, -0.04), (0.0, -0.052), (0.034, -0.04))):
        k.add(f"finger{i}", ell((0.205 + dx, -0.13 + dx * 0.4, 0.25 + dz), (0.022, 0.021, 0.03)), col, "arm{s}",
              mirror=True)


def legs(k, col=TEAL):
    k.add("thigh", ell((0.075, 0.0, 0.12), (0.075, 0.08, 0.08)), col, "leg{s}", mirror=True)
    k.add("leg", tube([(0.078, 0.0, 0.1), (0.084, -0.01, 0.04)], [0.058, 0.052]), col, "leg{s}", mirror=True)
    k.add("foot", ell((0.086, -0.03, 0.022), (0.058, 0.07, 0.024)), col, "leg{s}", mirror=True)


def head_fin(k, base, ex, outline, thick, col=SLATE):
    """The fin on the head: a hard-rimmed sickle swept back, turned a little to +X so its face shows from the front."""
    k.add("headfin", slab(outline, base, unit(*ex), (0, 0, 1.0), thick), col, "head", fuse=False, surface="skin")


def tail_fin(k, origin, ez, outline, thick, col=SLATE, rays=RAY, n_rays=0):
    """The broad tail fin behind, its face to the back (and the front past the legs)."""
    k.add("tailfin", slab(outline, origin, (1, 0, 0), unit(*ez), thick), col, "tail", fuse=False, surface="skin")


FIN = [(-0.075, -0.01), (-0.06, 0.05), (-0.02, 0.11), (0.05, 0.15), (0.15, 0.185), (0.105, 0.12), (0.085, 0.06),
       (0.07, -0.02)]
TAIL = [(0.0, 0.12), (0.09, 0.14), (0.16, 0.11), (0.205, 0.02), (0.2, -0.06), (0.17, -0.13), (0.11, -0.06),
        (0.05, -0.02), (0.0, -0.01), (-0.05, -0.02), (-0.11, -0.06), (-0.17, -0.13), (-0.2, -0.06), (-0.205, 0.02),
        (-0.16, 0.11), (-0.09, 0.14)]


def marshtomp(src, out):
    k = Kit(src, coat="scales")
    head_body(k)
    arms(k)
    legs(k)
    k.add("tailroot", tube([(0, 0.08, 0.2), (0, 0.13, 0.17)], [0.06, 0.045]), TEAL, "tail")
    head_fin(k, (0, -0.02, H[2] + HR[2] * 0.82), (0.32, 1, 0), FIN, 0.045)
    tail_fin(k, (0, 0.14, 0.16), (0, 0.45, 0.89), TAIL, 0.032)
    cheek(k, (0.145, H[1] - 0.02, H[2] - 0.025), (0.036, 0.05, 0.05), 0.11, 0.024, (1, -0.2, -0.12))
    mouth(k)
    # the orange oval on the belly
    k.decal((0, BC[1] - BR[1], BC[2] - 0.01), unit(0, -1, 0.08), (0.078, 0.1), ORANGE, eps=0.004, rings=20, segs=64)
    k.build(voxel=VOXEL, band=0.006)
    lid = srgb(TEAL)
    k.eye_states(lambda state: k.decal(mk.on_ell(H, HR, EYE_D), EYE_D, EYE_HALF,
                                       eye_look(state, eye_open, lid, srgb(INK), line=0.2), eps=0.004, rings=20,
                                       segs=56, mirror=True))
    k.export(out)


if __name__ == "__main__":
    marshtomp(*args())
