# Mega Dynamax Delphox (a form of our own): Mega Delphox's script (delphoxmega.py) built whole and unchanged, with the
# form's additions laid on its kit just before it is meshed (sculptkit.py):
#   blender --background --factory-startup --python sculpts/delphoxmegammax.py -- <the export's Delphox-Mega-Mmax.glb> <out.glb>
# delphoxmega.py builds inline under its `if __name__ == "__main__":` with no function to call, so it is run here as
# __main__ (runpy) with this script's own arguments; the wrapped build lays the additions in. For the dependency scan this form names its Mega as sibling("delphoxmega").
# From the design's picture (docs/art/reference/gemini/megadynamax/Mega-Dynamax-Delphox.jpg), what it adds, by size:
#   1. a staff of pale crystal held up in the hand on the picture's left (the model's -X), longer than the head and
#      body together, pointed at its foot, three crystal prongs at its head clasping a swirling ball of fire ringed
#      in violet flame, a dark grip where the hand holds it;
#   2. the Max clouds: a ring of crimson and violet mounds about the robe's hem, cyan curls on them, and a wisp
#      curling off each side of the robe;
#   3. the Key Stone's emblem at the collar, a glowing disc with a four-pointed star in a violet ring.
# (The picture's body is drawn freely: Delphox's own long sleeves, robe and fox tail, no broomstick and no floating
# sticks. The Mega's sculpt keeps its own: the branch, the burning sticks, the ragged skirt.)
import os, sys, math, runpy
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, lathe, ring_band, unit, args

CRIMSON, DEEP, VIOLET, DEEPV, CYAN = "#AC223E", "#701630", "#6C3EA0", "#42246E", "#4EE2EC"
CRYSTAL = "#D8C0F2"
GRIP = "#4A3A40"
FIRE, YOLK, AURA = "#F2702A", "#FFD25A", "#A05AE6"
STONE, RINGC = "#FFE8D8", "#BE96FF"

# From delphoxmega.py: the hover LIFT 0.076; the left sleeve's hand near (-0.105, -0.14, 0.39); the chest
# (0, -0.012, 0.486) with the white ruff down its front from (0, -0.054, 0.511); the robe's hem at z 0.115, its
# tatters reaching some 0.23 from the middle; the branch's ball of fire behind at about (0.145, 0.30, 0.12).
HAND_L = np.array([-0.105, -0.15, 0.39])


def join(shapes):
    V, F, off = [], [], 0
    for v, f in shapes:
        V.append(np.asarray(v)); F.extend([[i + off for i in q] for q in f]); off += len(v)
    return np.vstack(V), F


def hexrod(p0, p1, r0, r1):
    d = unit(*(np.array(p1) - np.array(p0)))
    a = np.cross(d, (0, 1.0, 0)); a /= np.linalg.norm(a)
    b = np.cross(d, a)
    ring = lambda p, r: [np.array(p) + r * (math.cos(t) * a + math.sin(t) * b) for t in (math.pi * i / 3 for i in range(6))]
    return hull(ring(p0, r0) + ring(p1, r1))


def staff(k):
    # leaned out to -X and forward from the hand (the picture raises the arm; the model keeps its rest pose), on
    # the inner side of the floating stick's flame by the hand, so that the orb tops out not far over the ears
    lean = unit(-0.42, -0.3, 0.86)
    grip = HAND_L + np.array([0.03, -0.05, 0.0])
    foot = grip - lean * 0.12
    top = grip + lean * 0.33
    d = unit(*(top - foot))
    pieces = [hexrod(foot + d * 0.05, top, 0.016, 0.017),
              hull([foot, foot + d * 0.05 + np.array([0.016, 0, 0]), foot + d * 0.05 + np.array([-0.016, 0, 0]),
                    foot + d * 0.05 + np.array([0, 0.016, 0]), foot + d * 0.05 + np.array([0, -0.016, 0])])]
    # three prongs at its head, curling up round the ball of fire
    orb = top + d * 0.075
    for j in range(3):
        t = 2 * math.pi * j / 3 + 0.4
        out = unit(math.cos(t), math.sin(t), 0)
        p0, p1 = top, top + out * 0.045 + d * 0.035
        p2 = orb + out * 0.035 + d * 0.035
        pieces.append(hexrod(p0, p1, 0.013, 0.011))
        pieces.append(hull([p1 + out * 0.01, p1 - out * 0.01, p1 + np.cross(out, d) * 0.01,
                            p1 - np.cross(out, d) * 0.01, p2]))
    k.add("staff", join(pieces), CRYSTAL, "armL", fuse=False, glow=CRYSTAL)
    # the grip where the hand holds it
    g = grip
    k.add("grip", lathe(g, d, [(0, -0.022), (0.021, -0.022), (0.021, 0.022), (0, 0.022)], seg=16), GRIP, "armL",
          fuse=False, surface="leather")
    # the ball of fire, its bright heart toward the front, and the violet flame round it
    k.add("orb", ell(orb, (0.058, 0.058, 0.058), sub=3), FIRE, "armL", fuse=False, glow=FIRE)
    k.add("orbheart", ell(orb + np.array([0.012, -0.03, 0.008]), (0.034, 0.03, 0.034), sub=3), YOLK, "armL",
          fuse=False, glow=YOLK)
    rings = []
    # one ring of violet flame framing the ball, turned to face the front camera, flames licking out of it
    ax = unit(0.45, -0.89, 0.12)
    e1 = np.cross(ax, (0, 0, 1.0)); e1 /= np.linalg.norm(e1); e2 = np.cross(ax, e1)
    loop = [orb + 0.078 * (math.cos(t) * e1 + math.sin(t) * e2) for t in np.linspace(0, 2 * math.pi, 29)]
    loop.append(loop[1])
    rings.append(tube(loop, [0.015] * len(loop), seg=10))
    for j in range(7):
        t = 2 * math.pi * j / 7 + 0.2
        o = math.cos(t) * e1 + math.sin(t) * e2
        sw = math.cos(t + 1.3) * e1 + math.sin(t + 1.3) * e2
        b = orb + o * 0.078
        rings.append(hull([b + sw * 0.018, b - sw * 0.018, b + ax * 0.014, b - ax * 0.014,
                           b + o * 0.045 + sw * 0.02]))
    k.add("aura", join(rings), AURA, "armL", fuse=False, glow=AURA)


def emblem(k):
    c = np.array([0.0, -0.088, 0.468])          # in front of the ruff's top, under the chin
    n = unit(0, -1, 0.25)
    k.add("emblem", lathe(c, n, [(0, -0.004), (0.022, -0.004), (0.022, 0.005), (0, 0.008)], seg=32), STONE, "body",
          fuse=False, glow=STONE)
    k.add("emblemring", ring_band(c + n * 0.002, n, 0.031, 0.01, thick=0.008), RINGC, "body", fuse=False, glow=RINGC)
    a = np.array([1.0, 0, 0]); b = np.cross(n, a)
    pts = []
    for j in range(4):
        e = a * math.cos(j * math.pi / 2 + math.pi / 4) + b * math.sin(j * math.pi / 2 + math.pi / 4)
        f = np.cross(n, e)
        base = c + n * 0.004
        pts.append(hull([base + e * 0.004 + f * 0.006, base + e * 0.004 - f * 0.006, base + e * 0.021 + n * 0.003,
                         base + e * 0.004 + f * 0.006 + n * 0.006, base + e * 0.004 - f * 0.006 + n * 0.006]))
    k.add("emblemstar", join(pts), "#FF6A4A", "body", fuse=False, glow="#FF6A4A")


def clump(k, name, c, s, col, alt, bone, rnd):
    """A cloud's mound: two round puffs of unlike sizes fused side by side, a smaller one of another colour riding on them."""
    c = np.array(c, float)
    out = unit(c[0], c[1], 0)
    side = np.array([-out[1], out[0], 0])
    for j, (ds, dt, dz, f) in enumerate(((0, 0, 0.78, 1.0), (0.02, 1.05, 0.72, 0.8), (0.0, 0.25, 1.7, 0.6))):
        r = s * f * (1 + 0.12 * rnd.uniform(-1, 1))
        p = c + out * ds + side * dt * s * 0.9
        p[2] = r * dz
        k.add(f"{name}_{j}", ell(p, (r, r, r * 0.92)), alt if j == 2 else col, bone)


def clouds(k, centre, rx, ry, n, s, skip, bone="root", seed=11):
    rnd = np.random.default_rng(seed)
    cols = (CRIMSON, VIOLET, DEEP, DEEPV)
    alts = (VIOLET, CRIMSON, DEEPV, DEEP)
    curls = []
    for i in range(n):
        a = 2 * math.pi * (i + 0.5) / n
        if skip(a):
            continue
        c = (centre[0] + rx * math.cos(a), centre[1] + ry * math.sin(a), 0.0)
        clump(k, f"cloud{i}", c, s, cols[i % 4], alts[i % 4], bone, rnd)
        if i % 3 == 0:
            q = np.array([c[0], c[1], s * 1.55]) + unit(math.cos(a), math.sin(a), 0) * s * 0.35
            curls.append(ell(q, (s * 0.32, s * 0.32, s * 0.28), sub=3))
    if curls:
        k.add("curls", join(curls), CYAN, bone, fuse=False, glow=CYAN)


def maxed(k):
    staff(k)
    emblem(k)
    # the ring about the hem, broken behind on +X where the branch's ball of fire hangs
    clouds(k, (0.0, 0.0), 0.31, 0.31, 11, 0.048, lambda a: math.radians(30) < a < math.radians(100))
    # a wisp curling off each side of the robe at the waist
    for s in (1, -1):
        for j, (p, r) in enumerate(zip([(0.24 * s, 0.06, 0.33), (0.268 * s, 0.095, 0.352), (0.285 * s, 0.135, 0.343),
                                        (0.29 * s, 0.17, 0.318), (0.255 * s, 0.085, 0.385)],
                                       [0.042, 0.038, 0.032, 0.026, 0.026])):
            k.add(f"wisp{'R' if s > 0 else 'L'}{j}", ell(p, (r, r, r * 0.85)), (CRIMSON, DEEP, CRIMSON, DEEP, VIOLET)[j], "body")


_build, _laid = Kit.build, []
def build(self, *a, **kw):
    if not _laid:               # the first build is the body's; the eyes' five states build after it
        _laid.append(True)
        maxed(self)
    return _build(self, *a, **kw)
Kit.build = build

runpy.run_path(os.path.join(os.path.dirname(os.path.abspath(__file__)), "delphoxmega.py"), run_name="__main__")
