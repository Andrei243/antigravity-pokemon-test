# Eternatus sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/eternatus.py -- <the export's Eternatus.glb> <out.glb>
#
# From the references: a dragon like a skeleton floating in the air, dark navy bone edged in red, read as head, core
# and tail in a line. The head is the first mass: a long wedge of a skull thrust forward and a little down, its face
# red, its jaws broad and jagged, pale eyes, a crest of dark and red spikes swept back from it. Behind the head a spine
# runs back, a few thick ribs and spines on it, banded red; under it hangs the core, a hexagonal case with a red rim
# and a glowing pink heart. From behind the head two short arms lie back and out along the body, banded red and
# hooked. Under the head and behind the core hang its claws, long straight talons in rakes of three. Behind, the tail
# trails up and away in great chunky segments, swung out to +X, hooked at its end.
# (The task's text spoke of tattered wings: the pictures show no membrane; the "wings" are the two arms, a mirror
# pair laid out in a V, and the rakes of talons.)
# Bones: body (0, 0, 0.398); head (0, -0.13, 0.498); arms (+-0.06, -0.1, 0.478); legs (+-0.05, 0.06, 0.358);
# tail (0, 0.12, 0.358).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, lathe, outward, ring_band, srgb, inside, unit, axes, eye_look, args

NAVY = "#1E2266"
DARK = "#151848"
RED = "#E6324C"
RIM = "#D42C4A"
PINK = "#F26FAE"
HOT = "#FFD2EA"
EYE_W, EYE_RIM = "#FFF2F6", "#F06090"

SPINE = [(0, -0.2, 0.5), (0, -0.07, 0.47), (0, 0.07, 0.45), (0, 0.2, 0.43)]
TAIL = [(0.0, 0.2, 0.43), (0.04, 0.32, 0.45), (0.1, 0.42, 0.51), (0.16, 0.5, 0.59), (0.2, 0.55, 0.66),
        (0.2, 0.52, 0.71)]
H = np.array([0.0, -0.44, 0.48])           # the skull's middle: forward and a little low, twice the first round's size
HR = np.array([0.13, 0.24, 0.12])
CORE = np.array([0.0, 0.03, 0.27])


def spike(root, tip, w, t=None, side=None):
    """A tapering solid point from a root of half width w (and half thickness t) to its tip: a flat-faced hull."""
    root, tip = np.array(root, float), np.array(tip, float)
    d = tip - root
    a, b = axes(d)
    if side is not None:
        a = np.array(side, float); a = a - d * (a @ d) / (d @ d); a /= np.linalg.norm(a)
        b = np.cross(d / np.linalg.norm(d), a)
    t = w * 0.6 if t is None else t
    mid = root + d * 0.45
    return hull([root + a * w, root - a * w, root + b * t, root - b * t, mid + a * w * 0.55, mid - a * w * 0.55,
                 mid + b * t * 0.55, mid - b * t * 0.55, tip])


def chunk(p0, p1, r0, r1, n=7, seed=0):
    """A chunky jagged segment between two points: the hull of two rough rings."""
    p0, p1 = np.array(p0, float), np.array(p1, float)
    a, b = axes(p1 - p0)
    rs = np.random.RandomState(seed)
    pts = []
    for p, r in ((p0, r0), (p1, r1)):
        for i in range(n):
            th = 2 * math.pi * (i + rs.rand() * 0.4) / n
            k = r * (0.8 + 0.35 * rs.rand())
            pts.append(p + k * (math.cos(th) * a * 0.75 + math.sin(th) * b))
    return hull(pts)


def wedge(V):
    """The skull narrowing and flattening toward its front (-Y): a wedge, not an egg."""
    V = V.copy()
    t = (V[:, 1] + 1) / 2
    V[:, 0] *= 0.45 + 0.55 * t
    V[:, 2] *= 0.6 + 0.4 * t
    return V


def add_spine(k):
    """The spine fused with the neck and the skull (the patches for the eyes need a fused surface)."""
    k.add("spine", tube(SPINE, [0.05, 0.048, 0.046, 0.044]), NAVY, "body")
    k.add("neck", tube([SPINE[0], H + (0, 0.17, 0.01)], [0.05, 0.06]), NAVY, "head")
    k.paint(lambda P: inside(np.abs(np.mod(P[:, 1] + 0.1, 0.075) - 0.037) - 0.009, 0.002) * (P[:, 1] > -0.15)
            * (P[:, 1] < 0.22), RED, on=["spine"])
    # A few thick spines above and short thick ribs out to the sides, clear of the core below
    for i, y in enumerate((-0.1, -0.01, 0.08, 0.17)):
        z = np.interp(y, [p[1] for p in SPINE], [p[2] for p in SPINE])
        k.add(f"dorsal{i}", spike((0, y, z + 0.035), (0, y + 0.07, z + 0.11 + 0.015 * (i % 2)), 0.034, 0.022,
                                  side=(1, 0, 0)), RED if i % 2 else DARK, "body", fuse=False, surface="shell")
        for s, nm in ((1, "R"), (-1, "L")):
            k.add(f"rib{i}{nm}", spike((s * 0.035, y, z - 0.01), (s * 0.13, y + 0.04, z - 0.055), 0.03, 0.022,
                                       side=(0, 1, 0)), NAVY, "body", fuse=False, surface="shell")


def add_core(k):
    """The core: a hexagonal case with a red rim, its pink heart glowing from both sides (a volume, not a plate)."""
    c = CORE
    k.add("corerim", lathe(c, (1, 0, 0), [(0, -0.05), (0.105, -0.05), (0.118, -0.035), (0.118, 0.035), (0.105, 0.05),
                                          (0, 0.05)], seg=6), RIM, "body", fuse=False, surface="shell")
    k.add("coreheart", lathe(c, (1, 0, 0), [(0, -0.057), (0.078, -0.057), (0.078, 0.057), (0, 0.057)], seg=6), PINK,
          "body", fuse=False, surface="shell", glow=PINK)
    k.add("corehot", lathe(c, (1, 0, 0), [(0, -0.063), (0.036, -0.063), (0.036, 0.063), (0, 0.063)], seg=6), HOT,
          "body", fuse=False, surface="shell", glow=HOT)
    # It hangs from the spine on two struts
    for s, nm in ((1, "R"), (-1, "L")):
        k.add(f"strut{nm}", spike((s * 0.025, 0.0, 0.46), (s * 0.03, 0.02, 0.37), 0.02, 0.016), NAVY, "body",
              fuse=False, surface="shell")


def add_head(k):
    """The long wedge of a skull, red-faced, with broad jagged jaws and a crest swept back from it."""
    k.add("skull", ell(H, HR, rot=(8, 0, 0), deform=wedge), NAVY, "head")
    # The red face: all of the front, under the dark ridge
    k.paint(lambda P: inside(P[:, 1] - (H[1] + 0.06), 0.004) * inside(P[:, 2] - (H[2] + 0.055 - 0.12 * (P[:, 1] - H[1])),
                                                                          0.003), RED, on=["skull"])
    # A dark ridge of bone down the skull's top
    k.add("helm", hull([H + p for p in ((0.06, -0.1, 0.05), (-0.06, -0.1, 0.05), (0.075, 0.17, 0.05),
                                         (-0.075, 0.17, 0.05), (0, -0.27, 0.025), (0, -0.08, 0.125), (0, 0.17, 0.15),
                                         (0, 0.26, 0.06))]), DARK, "head", fuse=False, surface="shell")
    # Broad jagged jaws, fangs down and red points forward over the snout
    for s, nm in ((1, "R"), (-1, "L")):
        k.add(f"jaw{nm}", spike(H + (s * 0.085, 0.1, -0.06), H + (s * 0.045, -0.29, -0.11), 0.045, 0.03), DARK,
              "head", fuse=False, surface="shell")
        for j, y in enumerate((-0.16, -0.07, 0.02)):
            k.add(f"fang{j}{nm}", spike(H + (s * (0.045 + 0.012 * j), y, -0.07), H + (s * (0.05 + 0.012 * j), y - 0.02, -0.15),
                                        0.018, 0.014), RED, "head", fuse=False, surface="shell")
        k.add(f"snout{nm}", spike(H + (s * 0.04, -0.14, 0.03), H + (s * 0.05, -0.33, 0.0), 0.03, 0.02), RED, "head",
              fuse=False, surface="shell")
        k.add(f"cheek{nm}", spike(H + (s * 0.1, 0.06, -0.01), H + (s * 0.2, 0.2, -0.04), 0.035, 0.022), RED, "head",
              fuse=False, surface="shell")
    # The crest: dark and red spikes swept back from the skull, no taller than the skull is long
    crest = (((0.0, 0.05, 0.12), (0.0, 0.9, 0.45), 0.34, DARK), ((0.035, 0.03, 0.1), (0.3, 0.9, 0.4), 0.32, RED),
             ((-0.035, 0.03, 0.1), (-0.3, 0.9, 0.4), 0.32, RED), ((0.06, 0.08, 0.07), (0.55, 0.85, 0.2), 0.28, DARK),
             ((-0.06, 0.08, 0.07), (-0.55, 0.85, 0.2), 0.28, DARK), ((0.0, -0.05, 0.11), (0.0, 0.75, 0.66), 0.24, RED))
    for i, (o, d, ln, col) in enumerate(crest):
        root = H + np.array(o)
        k.add(f"crest{i}", spike(root, root + unit(*d) * ln, 0.04, 0.024), col, "head", fuse=False, surface="shell")


def add_arms(k):
    """The two arms lying back and out along the body from behind the head, banded red, hooked at their ends: a
    mirror pair in a V, ending well below the crest's top."""
    path = np.array([(0.06, -0.1, 0.48), (0.12, -0.04, 0.55), (0.18, 0.05, 0.59), (0.22, 0.14, 0.57),
                     (0.23, 0.19, 0.52)])
    k.add("arm", outward(tube(path, [0.034, 0.03, 0.026, 0.021, 0.016])), NAVY, "arm{s}", fuse=False, mirror=True,
          surface="shell")
    for j, (a, b) in enumerate(((1, 2), (2, 3))):
        c = (path[a] + path[b]) / 2
        k.add(f"armband{j}", ring_band(c, path[b] - path[a], 0.031 - 0.004 * j, 0.022, thick=0.007), RED, "arm{s}",
              fuse=False, mirror=True, surface="shell")
    k.add("armspike", spike(path[2], path[2] + unit(0.5, 0.3, 0.8) * 0.07, 0.022, 0.016), RED, "arm{s}", fuse=False,
          mirror=True, surface="shell")
    k.add("armhook", spike(path[-1], path[-1] + (0.0, -0.04, -0.08), 0.02, 0.016), RED, "arm{s}", fuse=False,
          mirror=True, surface="shell")


def rake(k, name, bone, palm, tips, w=0.032):
    """A rake of three long straight talons from one palm, red at their points."""
    palm = np.array(palm, float)
    k.add(name + "palm", hull([palm + (dx, dy, dz) for dx in (-0.022, 0.022) for dy in (-0.024, 0.024)
                               for dz in (-0.02, 0.02)]), DARK, bone, fuse=False, mirror=True, surface="shell")
    for j, t in enumerate(tips):
        k.add(f"{name}{j}", spike(palm, t, w, w * 0.7), NAVY if j % 2 == 0 else DARK, bone, fuse=False, mirror=True,
              surface="shell")
        k.add(f"{name}tip{j}", spike(np.array(t) - (np.array(t) - palm) * 0.2, t, w * 0.5, w * 0.38), RED, bone,
              fuse=False, mirror=True, surface="shell")


def add_rakes(k):
    # Under the head, reaching forward and down, on a bone from the spine
    k.add("forelimb", spike((0.035, -0.12, 0.46), (0.1, -0.26, 0.36), 0.026, 0.02), NAVY, "arm{s}", fuse=False,
          mirror=True, surface="shell")
    rake(k, "fore", "arm{s}", (0.1, -0.26, 0.36), [(0.12, -0.56, 0.27), (0.17, -0.5, 0.17), (0.14, -0.4, 0.1)])
    # Behind the core, reaching down, on a bone from the spine
    k.add("hindlimb", spike((0.03, 0.2, 0.42), (0.08, 0.22, 0.24), 0.026, 0.02), NAVY, "leg{s}", fuse=False,
          mirror=True, surface="shell")
    rake(k, "hind", "leg{s}", (0.08, 0.22, 0.24), [(0.1, 0.08, 0.04), (0.16, 0.22, 0.02), (0.12, 0.36, 0.05)])


def add_tail(k):
    """The tail trailing up and away behind in great chunky segments, swung out to +X, hooked at its end."""
    radii = [0.044, 0.052, 0.058, 0.052, 0.04, 0.026]
    for i in range(len(TAIL) - 1):
        k.add(f"tail{i}", chunk(TAIL[i], TAIL[i + 1], radii[i], radii[i + 1], seed=i), NAVY if i % 2 else DARK, "tail",
              fuse=False, surface="shell")
        if 0 < i < len(TAIL) - 2:
            p = np.array(TAIL[i + 1])
            k.add(f"tailspike{i}", spike(p, p + unit(0.6, 0.9, 0.1) * 0.07, 0.022, 0.016), RED, "tail", fuse=False,
                  surface="shell")
    tip = np.array(TAIL[-1])
    for j, d in enumerate(((0.0, -0.4, -1.0), (0.4, -0.2, -1.0), (-0.3, -0.6, -0.8))):
        k.add(f"tailhook{j}", spike(tip, tip + unit(*d) * 0.07, 0.018, 0.014), RED if j else DARK, "tail", fuse=False,
              surface="shell")


def eye_open(u, v):
    c = np.tile(srgb(EYE_RIM), (len(u), 1))
    c[(u / 0.78) ** 2 + (v / 0.62) ** 2 < 1] = srgb(EYE_W)
    return c


def eyes(k, state):
    p = H + np.array((0.075, -0.1, 0.025))         # on the red face's side, under the ridge; the ray finds the surface
    n = unit(0.85, -0.45, 0.25)
    look = eye_look(state, eye_open, srgb(RED), srgb(DARK), line=0.18)
    for side in (1, -1):
        col = look if side == 1 else (lambda a, b, f=look: f(-a, b))
        k.decal(p * (side, 1, 1), n * (side, 1, 1), (0.045, 0.026), col, up=unit(0, 0.35, 1), eps=0.002, rings=20,
                segs=56)


def eternatus(src, out):
    k = Kit(src, coat="shell")
    add_spine(k)
    add_head(k)
    add_core(k)
    add_arms(k)
    add_rakes(k)
    add_tail(k)
    k.build(voxel=0.006, band=0.004)
    k.eye_states(lambda s: eyes(k, s))
    k.export(out)


if __name__ == "__main__":
    eternatus(*args())
