# Volcanion sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/volcanion.py -- <the export's Volcanion.glb> <out.glb>
#
# From the references: a squat red beast set low on four great boots, its back rising in a pointed hump. Out of each
# side of its back rises a great red arm of a tube; the two arch up and meet over it in a ring that faces the front
# (the task's text called them blue: the pictures draw them red), a gold cap where they join at the top, cyan ports
# down the face of each arm, two cyan bands across each and knobs standing out from the ring's outer edge. The head is
# small and carried low and forward between the forelegs: red, a tan beak of a jaw, a small blue eye under an angry brow,
# a great pointed red crest swept up and out from each side of the head. A dark teal neck and teal at the knees; a gold
# diamond on the chest; each boot a broad red cylinder flaring at the foot, with yellow claws; teal spikes off the hips
# and a short pointed tail.
# Bones: body (0, 0.02, 0.3); head (0, -0.15, 0.36); legR/L (+-0.11, -0.12, 0.24); hindR/L (+-0.11, 0.14, 0.24);
# tail (0, 0.2, 0.32).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, boxy, tube, hull, lathe, ring_band, outward, srgb, inside, unit, eye_look, polygon, args

RED, DARKRED, TEAL, TAN, GOLD, CYAN = "#C8303C", "#9A2430", "#24505A", "#E8B862", "#F2C230", "#3CCCEC"
CLAW, WHITE, BLUE, INKV = "#F2CA48", "#F4F2EE", "#3C9EE8", "#1C1418"

RC, RR, RT = np.array([0.0, 0.07, 0.55]), 0.19, 0.044          # the ring: centre, radius, the tube's radius


def ring_at(a):
    """A point on the ring's middle line at angle a (degrees, 0 out to +X, 90 at the top), and the way along it."""
    r = math.radians(a)
    return RC + RR * np.array([math.cos(r), 0, math.sin(r)]), np.array([-math.sin(r), 0, math.cos(r)])


def join(shapes):
    V, F, off = [], [], 0
    for v, f in shapes:
        V.append(v); F.extend([[i + off for i in q] for q in f]); off += len(v)
    return np.vstack(V), F


def body(k):
    k.add("trunk", ell((0.0, 0.02, 0.255), (0.175, 0.2, 0.15)), RED, "body")

    def peak(V):
        V = V.copy()
        t = np.clip((V[:, 2] + 1) / 2, 0, 1)
        w = 1 - 0.75 * t ** 1.5
        V[:, 0] *= w; V[:, 1] *= w
        return V
    k.add("hump", ell((0.0, 0.05, 0.42), (0.11, 0.12, 0.18), rot=(-8, 0, 0), deform=peak), RED, "body")
    k.add("tail", tube([(0.0, 0.2, 0.29), (0.0, 0.28, 0.31), (0.0, 0.34, 0.34)], [0.05, 0.03, 0.008]), RED, "tail")
    # the gold diamond on the chest
    D = polygon([(0, 1), (1, 0), (0, -1), (-1, 0)])
    k.decal((0.0, -0.174, 0.215), unit(0, -1, -0.15), (0.03, 0.045), GOLD, outline=D, eps=0.003, rings=10, segs=64)
    # teal spikes off the hips
    for s, sd in ((1, "R"), (-1, "L")):
        b = np.array((0.15 * s, 0.14, 0.33)); t = np.array((0.27 * s, 0.25, 0.39))
        d = unit(*(t - b)); a = np.cross(d, (0, 0, 1.0)); a /= np.linalg.norm(a); n = np.cross(a, d)
        k.add(f"hipspike{sd}", hull([b + a * 0.045, b - a * 0.045, b + n * 0.03, b - n * 0.03,
                                     b + (t - b) * 0.45 + a * 0.02, t]), TEAL, "body", fuse=False, surface="shell")


def arms(k):
    """The two arms: out of the back's sides and up round the ring to meet at the top."""
    for s, sd in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        pts = [np.array((0.12, 0.08, 0.33)) * m, np.array((0.17, 0.075, 0.4)) * m]
        for a in (-12, 12, 36, 60, 80, 92):
            p, _ = ring_at(a)
            pts.append(p * m)
        k.add(f"tube{sd}", tube(pts, [0.05, 0.047] + [RT] * 6, seg=24, per=6), RED, "body")
        # two cyan bands across each arm
        for a in (14, 64):
            p, t = ring_at(a)
            k.add(f"band{sd}{a}", ring_band(p * m, t * m, RT - 0.002, 0.012, thick=0.006), CYAN, "body",
                  fuse=False, surface="shell")
        # knobs standing out from the outer edge
        for a in (2, 38, 72):
            p, t = ring_at(a)
            out_ = unit(*(p - RC))
            o = (p + out_ * (RT - 0.012)) * m
            k.add(f"knob{sd}{a}", lathe(o, out_ * m, [(0.0, 0.0), (0.019, 0.0), (0.019, 0.03), (0.0, 0.03)], seg=24),
                  DARKRED, "body", fuse=False, surface="shell")
    # the cyan ports down the face of each arm, front and back: one glowing piece
    ports = []
    for s in (1, -1):
        m = np.array([s, 1, 1.0])
        for a in (-4, 28, 48, 76):
            p, _ = ring_at(a)
            for fy in (-1, 1):
                o = p * m + np.array([0, fy * (RT - 0.006), 0])
                ports.append(lathe(o, (0, fy, 0), [(0.0, 0.0), (0.019, 0.0), (0.019, 0.01), (0.0, 0.01)], seg=24))
    k.add("ports", join(ports), CYAN, "body", fuse=False, glow=CYAN, surface="shell")
    # the gold cap where the arms meet, front and back
    for fy in (-1, 1):
        top, _ = ring_at(90)
        k.add(f"cap{fy}", outward(ell(top + np.array([0, fy * (RT - 0.004), 0]), (0.034, 0.01, 0.02))), GOLD, "body",
              fuse=False, surface="shell")


def legs(k):
    for bone, y, by in (("leg{s}", -0.12, -0.165), ("hind{s}", 0.14, 0.175)):
        k.add("haunch" + bone[0], ell((0.16, y, 0.23), (0.095, 0.1, 0.1)), RED, bone, mirror=True)
        k.add("knee" + bone[0], ell((0.185, (y + by) / 2, 0.175), (0.06, 0.06, 0.045)), TEAL, bone, mirror=True)
        k.add("boot" + bone[0], tube([(0.2, by, 0.19), (0.205, by, 0.14), (0.21, by - 0.005, 0.1)], [0.07, 0.078, 0.086],
                                     seg=24, per=5), RED, bone, mirror=True)
        k.add("sole" + bone[0], ell((0.21, by - 0.008, 0.052), (0.095, 0.1, 0.052)), RED, bone, mirror=True)
        # yellow claws round the boot's front
        for j, (dx, dy) in enumerate(((-0.05, -0.045), (0.0, -0.075), (0.05, -0.045))):
            for s, sd in ((1, "R"), (-1, "L")):
                b = np.array(((0.21 + dx) * s, by - 0.008 + dy, 0.03))
                d = unit(dx * 6 * s, -1, -0.12)
                a = np.cross(d, (0, 0, 1.0)); a /= np.linalg.norm(a)
                k.add(f"claw{bone[0]}{sd}{j}", hull([b + a * 0.017, b - a * 0.017, b + (0, 0, 0.018), b - (0, 0, 0.016),
                                                      b + d * 0.04]), CLAW, bone.format(s=sd), fuse=False,
                      surface="shell")


H, HR = np.array([0.0, -0.225, 0.33]), np.array([0.078, 0.085, 0.07])


def on_head(d):
    d = unit(*d)
    t = 1.0 / math.sqrt(((d / HR) ** 2).sum())
    return H + d * t


def head(k):
    k.add("neck", ell((0.0, -0.15, 0.3), (0.085, 0.075, 0.075)), TEAL, "head")
    k.add("head", ell(H, HR), RED, "head")
    k.add("beak", ell((0.0, -0.28, 0.29), (0.05, 0.05, 0.03), rot=(-14, 0, 0), deform=boxy(3)), TAN, "head")
    k.add("brow", ell((0.035, -0.265, 0.358), (0.04, 0.035, 0.02), rot=(0, -14, -20)), RED, "head", mirror=True)
    # the crests: great pointed red blades swept up and out from the head's sides
    for s, sd in ((1, "R"), (-1, "L")):
        b = np.array((0.06 * s, -0.2, 0.37)); t = np.array((0.19 * s, -0.12, 0.49))
        d = unit(*(t - b)); a = np.array((0, 1.0, -0.4)); a = a - d * (a @ d); a /= np.linalg.norm(a)
        n = np.cross(d, a)
        k.add(f"crest{sd}", hull([b + a * 0.045, b - a * 0.05, b + n * 0.024, b - n * 0.024,
                                  b + (t - b) * 0.5 + a * 0.03 + n * 0.012, b + (t - b) * 0.5 - a * 0.02, t]),
              RED, "head", fuse=False, surface="shell")


EYE_N = unit(0.6, -0.74, 0.18)
EA, EB = 0.32, 0.5


def cut(th):
    s = math.sin(th) - EB * math.cos(th)
    return min(1.0, EA / s) if s > 1e-6 else 1.0


CW, CB, CK, CR = srgb(WHITE), srgb(BLUE), srgb(INKV), srgb(RED)


def eye_open(u, v):
    c = np.tile(CW, (len(u), 1))
    c[((u + 0.15) / 0.55) ** 2 + ((v + 0.05) / 0.62) ** 2 < 1] = CB
    c[((u + 0.15) / 0.25) ** 2 + ((v + 0.05) / 0.3) ** 2 < 1] = CK
    c[v > EA + EB * u - 0.22] = CK
    return c


def eyes(k, state):
    e = on_head(EYE_N)
    look = eye_look(state, eye_open, CR, CK, line=0.18)
    k.decal(e, EYE_N, (0.023, 0.02), look, eps=0.004, rings=20, segs=56, outline=cut, mirror=True)


def volcanion(src, dst):
    k = Kit(src, coat="shell")
    body(k)
    arms(k)
    legs(k)
    head(k)
    k.build(voxel=0.0072, smooth=4, band=0.004, wband=0.05)
    k.eye_states(lambda state: eyes(k, state))
    k.export(dst)


if __name__ == "__main__":
    volcanion(*args())
