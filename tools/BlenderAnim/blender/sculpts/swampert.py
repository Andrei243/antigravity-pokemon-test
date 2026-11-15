# Swampert sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown from
# mudkip.py and marshtomp.py (the line's blue, orange, slate fins, hard-rimmed gills, orange eyes and open mouth):
#   blender --background --factory-startup --python sculpts/swampert.py -- <the export's Swampert.glb> <out.glb>
#
# The design, from the references: a massive blue brute crouched on all fours (the skeleton's quadruped), its head
# carried low and forward on a short thick neck; two big rounded dark slate fins standing up from the top of the head
# in a V; orange gills behind the mouth's corners, each with three points swept back; a wide open mouth, pink inside;
# small orange eyes high on the face; a pale grey-white throat, chest and belly; huge arms splayed forward and out to
# broad flat hands of three fingers, an orange pad of two plates on each forearm; heavy haunches on short legs and
# broad feet, an orange pad of two plates on each thigh; a broad dark slate fan of a tail fin rising behind.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, slab, srgb, inside, unit, eye_look, sibling, outward, args

mk = sibling("mudkip")
mt = sibling("marshtomp")
BLUE, ORANGE, SLATE, PALE, INK = "#3E9EE0", mk.ORANGE, mt.SLATE, "#D9E2EC", mk.INK
EDGE = mk.EDGE

# --- the frame ---------------------------------------------------------------------------------------------------------
H = np.array([0, -0.315, 0.405])          # the head's middle
HR = np.array([0.162, 0.138, 0.11])
EYE_D = unit(0.32, -0.74, 0.6)
EYE_HALF = (0.018, 0.02)
VOXEL = 0.0068


def trunk(k):
    k.add("trunk", ell((0, 0.05, 0.26), (0.165, 0.25, 0.14)), BLUE, "body")
    k.add("belly", ell((0, 0.0, 0.165), (0.145, 0.21, 0.095)), BLUE, "body")
    k.add("chest", ell((0, -0.16, 0.31), (0.16, 0.14, 0.145)), BLUE, "body")
    k.add("fill", ell((0, -0.08, 0.22), (0.14, 0.16, 0.11)), BLUE, "body")
    k.add("neck", ell((0, -0.25, 0.36), (0.125, 0.11, 0.1)), BLUE, "head")
    def flat(V):
        V = V.copy()
        V[:, 0] *= 1 + 0.08 * np.clip(-V[:, 2], 0, 1)
        V[:, 2] = np.where(V[:, 2] > 0, V[:, 2] * 0.85, V[:, 2])
        return V
    k.add("head", ell(H, HR, deform=flat), BLUE, "head")
    k.add("muzzle", ell((0, H[1] - 0.055, H[2] - 0.03), (0.115, 0.085, 0.07)), BLUE, "head")


def pad(k, name, c, ldir, out, l, w, bone):
    """An orange pad of two plates on a limb's outer side: loose flattened balls laid along the limb."""
    ldir = unit(*ldir)
    out = np.array(out, float) - ldir * np.dot(out, ldir); out /= np.linalg.norm(out)
    across = np.cross(ldir, out)
    for i, t in enumerate((-0.53, 0.53)):
        cc = np.array(c, float) + ldir * l * t
        k.add(f"{name}{i}", outward(mk.plate(cc, across, out, ldir, w, 0.017, l * 0.5)), ORANGE, bone, fuse=False,
              mirror=True, surface="shell")


def arms(k):
    sh, el, wr = np.array([0.14, -0.16, 0.31]), np.array([0.245, -0.24, 0.18]), np.array([0.25, -0.31, 0.055])
    k.add("shoulder", ell((0.14, -0.15, 0.29), (0.105, 0.11, 0.11)), BLUE, "leg{s}", mirror=True)
    k.add("arm", tube([sh, el, wr], [0.074, 0.064, 0.054]), BLUE, "leg{s}", mirror=True)
    k.add("hand", ell((0.255, -0.345, 0.026), (0.075, 0.08, 0.027), rot=(0, 0, -20)), BLUE, "leg{s}", mirror=True)
    for i, a in enumerate((-0.55, 0.0, 0.55)):
        d = np.array([math.sin(a - 0.35), -math.cos(a - 0.35), 0])
        k.add(f"finger{i}", tube([(0.255, -0.345, 0.024) + d * 0.04, (0.255, -0.345, 0.022) + d * 0.1], [0.025, 0.022]),
              BLUE, "leg{s}", mirror=True)
    mid = (el + wr) / 2
    pad(k, "armpad", mid + np.array([0.05, 0.0, 0.0]), wr - el, (1, 0.2, 0.2), 0.064, 0.04, "leg{s}")


def hind(k):
    k.add("thigh", ell((0.15, 0.13, 0.2), (0.12, 0.15, 0.13)), BLUE, "hind{s}", mirror=True)
    k.add("shin", tube([(0.19, 0.17, 0.14), (0.21, 0.18, 0.05)], [0.072, 0.06]), BLUE, "hind{s}", mirror=True)
    k.add("foot", ell((0.22, 0.14, 0.025), (0.07, 0.1, 0.027), rot=(0, 0, -15)), BLUE, "hind{s}", mirror=True)
    for i, a in enumerate((-0.5, 0.0, 0.5)):
        d = np.array([math.sin(a - 0.25), -math.cos(a - 0.25), 0])
        k.add(f"toe{i}", tube([(0.22, 0.12, 0.024) + d * 0.04, (0.22, 0.12, 0.022) + d * 0.095], [0.024, 0.021]),
              BLUE, "hind{s}", mirror=True)
    pad(k, "legpad", (0.262, 0.14, 0.2), (0, 1, -0.15), (1, 0, 0.1), 0.07, 0.05, "hind{s}")


def _paddle():
    pts = [(-0.028, 0.0), (0.028, 0.0)]
    for t in np.linspace(-1.0, math.pi + 1.0, 22):
        pts.append((0.078 * math.cos(t), 0.145 + 0.108 * math.sin(t)))
    return pts


HEADFIN = _paddle()
TAILFAN = [(-0.06, 0.0)] + [(0.12 + 0.19 * math.cos(t), 0.1 + 0.185 * math.sin(t))
                          for t in np.linspace(math.radians(165), math.radians(-105), 26)] + [(0.06, -0.05)]
GILL = [(-0.025, 0.03), (0.03, 0.042), (0.11, 0.066), (0.065, 0.012), (0.13, -0.008), (0.065, -0.026),
        (0.1, -0.066), (0.025, -0.044), (-0.025, -0.032)]


def rays(k, origin, ex, thick, col=mt.RAY):
    """Lighter rays fanning out over both faces of the tail fin, from its root to near its rim."""
    ex = unit(*ex); n = np.cross(ex, (0, 0, 1.0))
    p0 = np.array([0.02, 0.0])
    for i, t in enumerate(np.radians([150, 108, 66, 24, -18])):
        p1 = np.array([0.12, 0.1]) + 0.165 * np.array([math.cos(t), math.sin(t)])
        d = p1 - p0; d /= np.linalg.norm(d); q = np.array([-d[1], d[0]])
        wedge = [p0 + q * 0.005, p1 + q * 0.011, p1 - q * 0.011, p0 - q * 0.005]
        for s in (1, -1):
            o = np.array(origin, float) + n * s * (thick / 2 + 0.0025)
            k.add(f"ray{i}{'a' if s > 0 else 'b'}", slab(wedge, o, ex, (0, 0, 1.0), 0.006), col, "tail", fuse=False,
                  surface="skin")


def swampert(src, out):
    k = Kit(src, coat="scales")
    trunk(k)
    arms(k)
    hind(k)
    # the two head fins: rounded paddles up and out of the head's top in a V, faces to the front, leaning back
    k.add("headfin", slab(HEADFIN, (0.07, H[1] + 0.03, H[2] + HR[2] * 0.68), (0.85, 0, -0.55), (0.55, 0.35, 0.76),
                          0.042), SLATE, "head", fuse=False, mirror=True, surface="skin")
    # the tail fin: a broad fan rising behind, run back and out to +X so its face shows to both cameras
    out_ = unit(0.57, 0.82, 0)
    k.add("tailroot", tube([(0, 0.24, 0.3), (0, 0.24, 0.3) + out_ * 0.1], [0.07, 0.045]), BLUE, "tail")
    k.add("tailfin", slab(TAILFAN, (0.02, 0.24, 0.32), out_, (0, 0, 1.0), 0.04), SLATE, "tail", fuse=False,
          surface="skin")
    rays(k, (0.02, 0.24, 0.32), out_, 0.04)
    mk.gill(k, (0.14, H[1] + 0.03, H[2] - 0.035), (1, 0.4, 0), GILL, 0.036, ORANGE)
    # the pale throat, chest and belly
    def pale(P):
        zline = np.minimum(0.27 - 0.6 * (P[:, 1] + 0.05), H[2] - 0.075)
        return inside(P[:, 2] - zline, 0.004) * inside(np.abs(P[:, 0]) - 0.115, 0.01) * inside(P[:, 1] - 0.2, 0.01)
    k.paint(pale, PALE, on=["trunk", "belly", "chest", "fill", "neck", "head", "muzzle"])
    mt.mouth(k, h=H, z0=H[2] - 0.012, aw=1.2, depth=0.075)
    k.build(voxel=VOXEL, band=0.006)
    lid = srgb(BLUE)
    k.eye_states(lambda state: k.decal(mk.on_ell(H, HR, EYE_D), EYE_D, EYE_HALF,
                                       eye_look(state, mt.eye_open, lid, srgb(INK), line=0.2), eps=0.004, rings=20,
                                       segs=56, mirror=True))
    k.export(out)


if __name__ == "__main__":
    swampert(*args())
