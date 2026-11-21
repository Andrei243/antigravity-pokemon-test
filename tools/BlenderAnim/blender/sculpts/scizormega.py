# Mega Scizor sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/scizormega.py -- <the export's Scizor-Mega.glb> <out.glb>
#
# Grown from scizor.py (which builds as it is read, so what is needed is copied): its red metal and dark joints, the
# head ball with its almond eyes laid by eye_frame, the dark V of the face, the wings' axes. Its private plates are the
# kit's slab now, its blades and teeth the kit's hull.
#
# What Mega Scizor changes, from the pictures, largest mass first:
#   1. the PINCERS, each nearly half the model's height long and a quarter deep: an upper jaw of red curving down to a
#      hooked point, a sawtooth of dark teeth under it; a lower jaw of white with dark teeth along its top; a dark
#      round hinge with a teal eye on its outer side. Held forward and out (45 degrees), a little down: the pictures
#      fling them sideways, which is the art's pose, not the rest pose;
#   2. long legs of red armour, each one tapering blade from the hip to a white claw, splayed;
#   3. a sharper helmet of a head: a tall crest swept back and two pairs of side points, a dark face, teal eyes;
#   4. a ring of dark plates round the waist over a red abdomen coming to a point, a red stripe down its front;
#   5. dark round shoulders and hips;
#   6. one pair of pale veined wings, longer than the base's.
# Skeleton (the Mega's own, above the base's): body (0, 0, 0.46), head (0, -0.04, 0.66), arms (+-0.09, -0.03, 0.62),
# wings (+-0.04, 0.05, 0.6), legs (+-0.07, 0.02, 0.34).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, slab, lathe, outward, srgb, inside, unit, eye_look, polygon, args

src, dst = args()
k = Kit(src, coat="metal")

RED, DARK, CLAW, PALE, TEAL = "#D9323C", "#3C3C46", "#F4F4F0", "#E8E6E2", "#3CC8CC"
WING, WINGEDGE, INK = "#DCE2EA", "#A4ACBA", "#15171C"

TH_C, TH_R = np.array([0.0, -0.025, 0.575]), (0.066, 0.056, 0.07)
AB_C, AB_R = np.array([0.0, 0.01, 0.43]), (0.07, 0.066, 0.08)
H, HR = np.array([0.0, -0.07, 0.745]), np.array([0.058, 0.064, 0.06])
SHOULDER = np.array([0.1, -0.03, 0.62])
HINGE = np.array([0.2, -0.1, 0.6])
HIP = np.array([0.08, 0.01, 0.335])


def frame_at(origin, ex, ey, ez):
    o, ex, ey, ez = (np.array(a, float) for a in (origin, ex, ey, ez))
    def place(P):
        P = np.atleast_2d(np.asarray(P, float))
        return o + P[:, :1] * ex + P[:, 1:2] * ey + P[:, 2:3] * ez
    return place


# --- the body: thorax, dark plates round the waist, a red abdomen to a point --------------------------------------------
k.add("thorax", ell(TH_C, TH_R), RED, "body")
k.add("neck", tube([(0, -0.03, 0.62), (0, -0.05, 0.7)], [0.03, 0.028]), DARK, "head")
def point_down(V):
    V = V.copy()
    s = 1 - 0.55 * np.clip(-V[:, 2], 0, 1) ** 1.5
    V[:, 0] *= s; V[:, 1] *= s
    return V
k.add("abdomen", ell(AB_C, AB_R, rot=(12, 0, 0), deform=point_down), RED, "body")
k.add("waist", ell((0, 0.0, 0.49), (0.06, 0.055, 0.04)), DARK, "body")
# The dark V from the neck down the thorax's front
k.paint(lambda P: inside(np.abs(P[:, 0]) - 0.045 * np.clip((P[:, 2] - 0.55) / 0.08, 0, 1), 0.003)
        * (P[:, 1] < TH_C[1] - 0.02) * inside(P[:, 2] - 0.66, 0.004) * inside(0.54 - P[:, 2], 0.004), DARK,
        on=["thorax", "neck"])

# The plates: three a side round the waist's top, leaving the red front between them
for s in (1, -1):
    for i, a in enumerate((38, 82, 126)):
        t = math.radians(a)
        r = np.array([s * math.sin(t), -math.cos(t), 0.0])          # outward from the waist's axis
        tg = np.array([s * math.cos(t), math.sin(t), 0.0])          # along the ring
        c = np.array([0.0, 0.0, 0.475]) + r * np.array([0.074, 0.068, 0])
        pts = []
        for dz, out, w in ((0.035, 0.0, 0.024), (0.0, 0.016, 0.03), (-0.04, 0.008, 0.026)):
            for ws in (-1, 1):
                for th in (-0.012, 0.016):
                    pts.append(c + tg * ws * w + r * (out + th) + np.array([0, 0, dz]))
        k.add(f"plate{'R' if s > 0 else 'L'}{i}", hull(pts), DARK, "body", fuse=False)

# --- the shoulders and arms: dark balls, a short red arm out to the pincer's hinge -------------------------------------
k.add("shoulder", ell(SHOULDER, (0.044, 0.044, 0.044)), DARK, "arm{s}", mirror=True)
k.add("arm", tube([SHOULDER + (0.03, -0.01, 0), (SHOULDER + HINGE) / 2, HINGE], [0.028, 0.026, 0.03]), RED, "arm{s}",
      mirror=True)

# --- the pincers ---------------------------------------------------------------------------------------------------------
PIN_L = 0.4                      # the upper jaw's length, nearly half the model's height


def pincer(s):
    side = "R" if s > 0 else "L"
    bone = f"arm{side}"
    hg = HINGE * np.array([s, 1, 1.0])
    d = unit(s * 0.62, -0.77, -0.14)                          # forward and out, a little down
    up = np.array([0, 0, 1.0]) - d * d[2]; up /= np.linalg.norm(up)
    sd = np.cross(up, d)                                       # (d, sd, up) right-handed as ex, ey, ez below
    pl = frame_at(hg, d, sd, up)

    # the hinge: a dark drum across the jaws' root, a teal eye on each of its faces. Swept forward, the pincer's inner
    # face is the one the front camera sees (in the pictures' T it is the front face) and its outer face the back's
    n = sd + up * 0.15; n /= np.linalg.norm(n)
    hc = hg + d * 0.03
    k.add(f"hinge{side}", lathe(hc, n, [(0, -0.06), (0.05, -0.06), (0.058, -0.045), (0.058, 0.045), (0.05, 0.06),
                                        (0, 0.06)]), DARK, bone, fuse=False)
    for e, f in (("a", 1), ("b", -1)):
        k.add(f"eyering{e}{side}", lathe(hc + n * f * 0.058, n * f, [(0, -0.004), (0.034, -0.004), (0.034, 0.004),
                                                                       (0, 0.006)]), TEAL, bone, fuse=False)
        k.add(f"eyemid{e}{side}", lathe(hc + n * f * 0.064, n * f, [(0, -0.003), (0.013, -0.003), (0.013, 0.003),
                                                                      (0, 0.004)]), DARK, bone, fuse=False)

    # the upper jaw: a long red dome from the hinge, its underside flat, the far end hooking down
    def dome(V):
        V = V.copy()
        V[:, 2] = np.where(V[:, 2] < 0, V[:, 2] * 0.3, V[:, 2])
        a = (V[:, 0] + 1) / 2                                  # 0 at the hinge, 1 at the far end
        V[:, 2] -= 0.55 * np.clip(a - 0.6, 0, 1) ** 2 / 0.16   # the end bends down
        return V
    V, F = ell((0, 0, 0), (PIN_L / 2, 0.048, 0.07), deform=dome)
    V = V + np.array([PIN_L / 2 + 0.01, 0, 0.022])
    k.add(f"jawU{side}", outward((pl(V), F)), RED, bone, fuse=False)
    # its hooked point
    k.add(f"hook{side}", hull([pl(p)[0] for p in [(PIN_L - 0.04, -0.03, 0.0), (PIN_L - 0.04, 0.03, 0.0),
                                                     (PIN_L - 0.07, -0.02, -0.03), (PIN_L - 0.07, 0.02, -0.03),
                                                     (PIN_L - 0.02, 0, -0.075), (PIN_L - 0.1, 0, 0.01)]]),
          RED, bone, fuse=False)
    # the dark teeth under it, a sawtooth pointing down
    for i, a in enumerate(np.linspace(0.1, 0.28, 5)):
        z0 = 0.018 - 0.004 * i
        k.add(f"toothU{i}{side}", hull([pl(p)[0] for p in [(a - 0.03, -0.02, z0), (a + 0.012, -0.02, z0),
                                                           (a - 0.03, 0.02, z0), (a + 0.012, 0.02, z0),
                                                           (a + 0.006, -0.012, z0 - 0.05),
                                                           (a + 0.006, 0.012, z0 - 0.05)]]), DARK, bone, fuse=False)

    # the lower jaw: a white blade under it, flat on top, with dark teeth along its top pointing up
    def bowl(V):
        V = V.copy()
        V[:, 2] = np.where(V[:, 2] > 0, V[:, 2] * 0.25, V[:, 2])
        return V
    V, F = ell((0, 0, 0), (PIN_L * 0.4, 0.042, 0.052), deform=bowl)
    V = V + np.array([PIN_L * 0.44, 0, -0.07])
    k.add(f"jawL{side}", outward((pl(V), F)), PALE, bone, fuse=False)
    for i, a in enumerate(np.linspace(0.1, 0.27, 4)):
        z0 = -0.066
        k.add(f"toothL{i}{side}", hull([pl(p)[0] for p in [(a - 0.026, -0.018, z0), (a + 0.016, -0.018, z0),
                                                           (a - 0.026, 0.018, z0), (a + 0.016, 0.018, z0),
                                                           (a - 0.012, -0.01, z0 + 0.044),
                                                           (a - 0.012, 0.01, z0 + 0.044)]]), DARK, bone, fuse=False)
    # dark between the jaws at the hinge, so the mouth opens toward the far end
    V, F = ell((0, 0, 0), (0.07, 0.036, 0.05))
    k.add(f"maw{side}", outward((pl(V + np.array([0.06, 0, -0.025])), F)), DARK, bone, fuse=False)


for s in (1, -1):
    pincer(s)

# --- the legs: a dark hip ball, then one red blade tapering to a white claw, splayed ------------------------------------
k.add("hip", ell(HIP, (0.04, 0.04, 0.04)), DARK, "leg{s}", mirror=True)
k.add("leg", tube([HIP + (0.01, 0, -0.01), (0.115, 0.0, 0.25), (0.145, -0.005, 0.13), (0.168, -0.01, 0.045)],
                  [0.04, 0.056, 0.04, 0.02]), RED, "leg{s}", mirror=True)
k.add("claw", hull([(0.152, -0.025, 0.06), (0.184, -0.025, 0.06), (0.152, 0.008, 0.06), (0.184, 0.008, 0.06),
                    (0.174, -0.016, 0.0)]), CLAW, "leg{s}", fuse=False, mirror=True)

# --- the wings: one pair, pale and veined, longer than the base's, raised in a V and swept back ------------------------
def wing_axes(s, sweep, lift, twist):
    sw, li = math.radians(sweep), math.radians(lift)
    ex = np.array([s * math.cos(li) * math.cos(sw), -math.cos(li) * math.sin(sw), math.sin(li)])
    ez = np.array([0.0, 1.0, math.tan(math.radians(twist))])
    ez = ez - ex * np.dot(ez, ex)
    return ex, ez / np.linalg.norm(ez)


def wing_outline(L, W, t0=0.0, t1=1.0, inset=1.0, n=26):
    t = np.linspace(t0, t1, n)
    a = 0.004 + t * L
    w = np.maximum(W * np.sqrt(np.clip(1 - ((t - 0.45) / 0.55) ** 2, 0, 1)) * (1.0 - 0.35 * t) * inset, 0.001)
    return np.vstack([np.stack([a, w], 1), np.stack([a, -w], 1)[::-1]])


for s, side in ((1, "R"), (-1, "L")):
    o = np.array([0.03 * s, 0.06, 0.61])
    ex, ez = wing_axes(s, -40, 34, 55)
    k.add(f"wing{side}", slab(wing_outline(0.25, 0.058), o, ex, ez, 0.005), WINGEDGE, f"wing{side}", fuse=False)
    k.add(f"wingin{side}", slab(wing_outline(0.25, 0.058, 0.05, 0.96, 0.78), o, ex, ez, 0.008), WING, f"wing{side}",
          fuse=False)
    # a vein down the middle, darker
    k.add(f"vein{side}", slab([(0.02, 0.006), (0.22, 0.003), (0.22, -0.003), (0.02, -0.006)], o, ex, ez, 0.011),
          WINGEDGE, f"wing{side}", fuse=False)

# --- the head: a helmet of a ball, a tall crest swept back, two pairs of side points ------------------------------------
k.add("head", ell(H, HR), RED, "head")
k.add("chin", ell(H + (0, -0.045, -0.035), (0.036, 0.04, 0.034)), RED, "head")
# the crest: a broad fin swept up and back off the brow (the pictures' helmet point), not a needle
k.add("crest", hull([(0, -0.118, 0.775), (0.02, -0.09, 0.785), (-0.02, -0.09, 0.785), (0.02, -0.02, 0.79),
                     (-0.02, -0.02, 0.79), (0.012, -0.05, 0.85), (-0.012, -0.05, 0.85), (0, 0.02, 0.92)]),
      RED, "head", fuse=False)
for s, side in ((1, "R"), (-1, "L")):
    m = np.array([s, 1, 1.0])
    # a broad point each side of the crown, swept back and out
    k.add(f"point{side}", hull([np.array(p) * m for p in [(0.028, -0.095, 0.77), (0.052, -0.07, 0.785),
                                                          (0.04, -0.03, 0.79), (0.052, -0.06, 0.755),
                                                          (0.035, -0.05, 0.8), (0.1, 0.02, 0.85)]]), RED, "head",
          fuse=False)

# The dark face: a V from beside the eyes down the chin, the eyes on red above its arms
k.decal(H + (0, -0.075, -0.035), unit(0, -1, -0.15), (0.05, 0.06), DARK, eps=0.003, rings=16, segs=56,
        outline=polygon([(-1.0, 0.55), (-0.45, 0.35), (0.0, 0.1), (0.45, 0.35), (1.0, 0.55), (0.6, -0.4),
                         (0.0, -1.0), (-0.6, -0.4)]))

EYE_HALF = (0.03, 0.022)
EYE_SHAPE = [(-1.0, -0.15), (-0.55, 0.2), (0.0, 0.55), (0.55, 0.85), (1.0, 0.75), (0.95, 0.1), (0.55, -0.55),
             (0.0, -0.7), (-0.6, -0.5)]


def eye_frame(side, d=(0.6, -0.76, 0.3)):
    de = unit(side * d[0], d[1], d[2])
    p = H + de / math.sqrt(((de / HR) ** 2).sum())
    n = (p - H) / HR ** 2
    return p, n / np.linalg.norm(n)


def eye_open(u, v):
    c = np.tile(srgb(TEAL), (len(u), 1))
    c[((u + 0.12) / 0.4) ** 2 + ((v + 0.02) / 0.74) ** 2 < 1] = srgb(INK)
    c[(u / 0.15) ** 2 + ((v - 0.28) / 0.19) ** 2 < 1] = (1, 1, 1)
    return c


def add_eyes(state):
    look = eye_look(state, eye_open, srgb(RED), srgb(INK), line=0.17)
    for side in (1, -1):
        c, n = eye_frame(side)
        col = look if side == 1 else (lambda a, b, f=look: f(-a, b))
        shape = EYE_SHAPE if side == 1 else [(-a, b) for a, b in EYE_SHAPE]
        k.decal(c, n, EYE_HALF, col, eps=0.003, rings=20, segs=56, outline=polygon(shape))


k.build(voxel=0.0058, band=0.005)
k.eye_states(add_eyes)
k.export(dst)
