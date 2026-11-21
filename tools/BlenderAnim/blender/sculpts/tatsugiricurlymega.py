# Tatsugiri-Curly-Mega sculpted on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/tatsugiricurlymega.py -- <the export's .glb> <out.glb>
#
# From the references (they overrule the game's own sculptor, whose Mega is one fish with a band of seaweed): every
# Mega Tatsugiri is a platter of all three looks served together, each lying on its own cushion of rice on a cloth that
# runs from magenta through violet to sky blue, with blades of green baran standing between them. The look the Mega is
# named for lies in the middle and wears a heap of red-orange roe; the other two lie to either side.
#   - Curly (orange): Tatsugiri's own sculpt (tatsugiri.py), drawn through a frame that places, turns and sizes it.
#   - Droopy (hot pink): a long body lying low whose rounded head droops forward to the cloth, half-shut sad eyes low
#     on the front of it, a crumpled white frill on its back and a two-lobed tail fin trailing behind.
#   - Stretchy (yellow): a long straight body with a blunt rounded snout, narrowed scowling eyes at its front, a row of
#     small spikes laid back along its back and a whale's fluke at its tail.
# Largest masses, in the pictures and here: the three fish (each about as long as the base, the middle one a fifth
# larger), the cloth under all of them, the grass, the roe. The design is wider than tall (see the report).
#
# The look in the middle takes the skeleton (body, head, tail); everything else is on the root, so the platter stays
# still under it. platter(src, out, middle) builds any of the three; the Droopy and Stretchy Megas call it.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, slab, hull, srgb, inside, unit, eye_look, outward, lumpy, args, sibling)

base = sibling("tatsugiri")

CURLY = base.CURLY
DROOPY = dict(body="#EE4F80", fin="#D93468", frill="#F6F0EE")
STRETCHY = dict(body="#F8D21E", spike="#EDB712", belly="#FBE27A")
INK, WHITE = base.INK, base.WHITE
RICE = base.RICE
ROE = "#F0461A"
GRASS = ("#2DB457", "#6FD45A")
CLOTH = ("#E046C8", "#9A62EC", "#4FB4F2")
CW, CK = srgb(WHITE), srgb(INK)


# --- a frame that places a fish: local points (the base's own frame) are sized, turned about Z and moved ------------
class Placed:
    def __init__(self, k, prefix, at, yaw, size, bones):
        self.k, self.prefix, self.o, self.s, self.bones = k, prefix, np.array(at, float), size, bones
        a = math.radians(yaw)
        self.R = np.array([[math.cos(a), -math.sin(a), 0], [math.sin(a), math.cos(a), 0], [0, 0, 1.0]])

    def to(self, V):
        return (np.asarray(V, float) * self.s) @ self.R.T + self.o

    def back(self, P):
        return ((P - self.o) @ self.R) / self.s

    def bone(self, b):
        return self.bones(b)

    def add(self, name, shape, color, bone, fuse=True, mirror=False, glow=None, surface=None, carve=False):
        V, F = shape
        if mirror:
            for side, nm in ((0, "R"), (1, "L")):
                VV = V if side == 0 else V * np.array([-1, 1, 1.0])
                FF = F if side == 0 else [f[::-1] for f in F]
                self.k.add(self.prefix + name + nm, (self.to(VV), FF), color, self.bone(bone.format(s="RL"[side])),
                           fuse=fuse, glow=glow, surface=surface, carve=carve)
            return
        self.k.add(self.prefix + name, (self.to(V), F), color, self.bone(bone), fuse=fuse, glow=glow, surface=surface,
                   carve=carve)

    def paint(self, mask, color, on=None):
        assert on, "a fish's paint names its parts"
        self.k.paint(lambda P: mask(self.back(P)), color, on=[self.prefix + o for o in on])

    def decal(self, center, normal, half, color, up=(0, 0, 1), eps=0.004, outline=None, rings=14, segs=44,
              surface=None, mirror=False):
        def one(c, n, u, col):
            self.k.decal(self.to(c), np.array(n, float) @ self.R.T, (half[0] * self.s, half[1] * self.s), col,
                         up=np.array(u, float) @ self.R.T, eps=eps * self.s, outline=outline, rings=rings, segs=segs,
                         surface=surface)
        one(center, normal, up, color)
        if mirror:
            m = np.array([-1, 1, 1.0])
            col = (lambda u, v: color(-u, v)) if callable(color) else color
            one(np.array(center, float) * m, np.array(normal, float) * m, np.array(up, float) * m, col)


def skeleton(b):
    return b


def still(b):
    return "root"


# --- Curly: the base's own sculpt -------------------------------------------------------------------------------
def curly(p):
    base.body(p, CURLY)
    base.markings(p, CURLY)
    base.fins(p, CURLY)
    base.rice(p)


def curly_eyes(p, state):
    base.eyes(p, state, CURLY)


CURLY_ROE = np.array([0.0, -0.13, 0.19])


# --- Droopy ------------------------------------------------------------------------------------------------------
D_LINE = [(-0.16, 0.088, 0.05), (-0.1, 0.112, 0.053), (-0.03, 0.124, 0.053), (0.04, 0.124, 0.05),
          (0.1, 0.116, 0.044), (0.155, 0.1, 0.034), (0.195, 0.084, 0.024)]
D_HC, D_HR = np.array([0.0, -0.185, 0.072]), np.array([0.053, 0.062, 0.05])
D_EYE_N = unit(0.6, -0.72, -0.2)
D_EYE_AT = D_HC + D_EYE_N * D_HR * 0.97
D_EYE_HALF = (0.022, 0.024)


def droopy(p):
    C = DROOPY
    pts = [(0, y, z) for y, z, _ in D_LINE]
    rad = [r for _, _, r in D_LINE]
    p.add("neck", tube(pts[:3], rad[:3], seg=28), C["body"], "head")
    p.add("trunk", tube(pts[2:5], rad[2:5], seg=28), C["body"], "body")
    p.add("tail", tube(pts[4:], rad[4:], seg=28), C["body"], "tail")
    p.add("head", ell(D_HC, D_HR, sub=5), C["body"], "head")
    # the crumpled white frill on its back, a little pink at its root
    p.add("frill", outward(ell((0, 0.04, 0.172), (0.022, 0.06, 0.026), rot=(-12, 0, 0), deform=lumpy(6, 3, 0.18),
                               sub=4)), C["frill"], "body", fuse=False)
    # the two-lobed tail fin trailing low behind, turned out to +X
    ex = unit(math.sin(math.radians(40)), math.cos(math.radians(40)), 0)
    fin = [(-0.004, -0.012), (0.03, -0.03), (0.062, -0.034), (0.048, -0.012), (0.04, 0.0), (0.056, 0.018),
           (0.066, 0.04), (0.03, 0.026), (-0.004, 0.012)]
    p.add("tailfin", slab(fin, (0, 0.19, 0.085), ex, (0, 0, 1), 0.013), C["fin"], "tail", fuse=False)
    p.add("rice", outward(ell((0, 0.03, 0.036), (0.085, 0.12, 0.036), deform=lumpy(7, 3, 0.08), sub=4)), RICE,
          "root", fuse=False, surface="cloth")

    def ground(P):
        m = np.zeros(len(P))
        for s in (1, -1):
            c = D_EYE_AT * np.array([s, 1, 1])
            m = np.maximum(m, inside(np.linalg.norm(P - c, axis=1) - 0.03, 0.003))
        return m
    p.paint(ground, C["body"], on=["head"])


def droopy_open(u, v):
    r = np.sqrt(u * u + v * v)
    c = np.tile(CW, (len(u), 1))
    c[r > 0.86] = CK
    c[(u + 0.05) ** 2 + (v + 0.3) ** 2 < 0.5 ** 2] = CK
    edge = 0.2 - 0.35 * u                   # the heavy lid, lower at the outer corner: a sad look
    c[v > edge] = srgb(DROOPY["body"])
    c[np.abs(v - edge) < 0.15] = CK
    c[r > 0.97] = srgb(DROOPY["body"])
    return c


def droopy_eyes(p, state):
    look = eye_look(state, droopy_open, srgb(DROOPY["body"]), CK, line=0.16)
    p.decal(D_EYE_AT, D_EYE_N, D_EYE_HALF, look, eps=0.0018, rings=20, segs=56, mirror=True)


DROOPY_ROE = np.array([0.02, -0.07, 0.19])


# --- Stretchy ----------------------------------------------------------------------------------------------------
S_LINE = [(-0.13, 0.112, 0.047), (-0.07, 0.117, 0.051), (0.0, 0.121, 0.052), (0.07, 0.125, 0.049),
          (0.13, 0.131, 0.042), (0.18, 0.141, 0.031), (0.215, 0.15, 0.022)]
S_HC, S_HR = np.array([0.0, -0.145, 0.108]), np.array([0.051, 0.062, 0.048])
S_EYE_N = unit(0.58, -0.78, 0.12)
S_EYE_AT = S_HC + S_EYE_N * S_HR * 0.97
S_EYE_HALF = (0.019, 0.019)


def stretchy(p):
    C = STRETCHY
    pts = [(0, y, z) for y, z, _ in S_LINE]
    rad = [r for _, _, r in S_LINE]
    p.add("neck", tube(pts[:3], rad[:3], seg=28), C["body"], "head")
    p.add("trunk", tube(pts[2:5], rad[2:5], seg=28), C["body"], "body")
    p.add("tail", tube(pts[4:], rad[4:], seg=28), C["body"], "tail")
    p.add("head", ell(S_HC, S_HR, sub=5), C["body"], "head")

    def belly(P):
        return inside(P[:, 2] - 0.095, 0.01) * inside(P[:, 1] - 0.16, 0.01)
    p.paint(belly, C["belly"], on=["neck", "trunk", "tail", "head"])

    def ground(P):
        m = np.zeros(len(P))
        for s in (1, -1):
            c = S_EYE_AT * np.array([s, 1, 1])
            m = np.maximum(m, inside(np.linalg.norm(P - c, axis=1) - 0.03, 0.003))
        return m
    p.paint(ground, C["body"], on=["head"])
    # the row of small spikes laid back along its back
    for i, y in enumerate((-0.06, -0.015, 0.03, 0.075)):
        z = 0.12 + 0.05 - 0.004
        h = 0.03 - 0.003 * i
        p.add(f"spike{i}", hull([(-0.008, y - 0.014, z - 0.006), (0.008, y - 0.014, z - 0.006),
                                 (-0.008, y + 0.016, z - 0.006), (0.008, y + 0.016, z - 0.006),
                                 (0.0, y + 0.03, z + h)]), C["spike"], "body", fuse=False)
    # the whale's fluke at the tail, turned out to +X
    ex = unit(math.sin(math.radians(40)), math.cos(math.radians(40)), 0)
    fluke = [(-0.004, -0.012), (0.032, -0.03), (0.066, -0.036), (0.05, -0.012), (0.042, 0.0), (0.058, 0.02),
             (0.07, 0.044), (0.032, 0.028), (-0.004, 0.012)]
    p.add("fluke", slab(fluke, (0, 0.21, 0.152), ex, (0, 0, 1), 0.013), C["body"], "tail", fuse=False)
    p.add("rice", outward(ell((0, 0.03, 0.038), (0.085, 0.125, 0.038), deform=lumpy(7, 3, 0.08), sub=4)), RICE,
          "root", fuse=False, surface="cloth")


def stretchy_open(u, v):
    r = np.sqrt(u * u + v * v)
    c = np.tile(CW, (len(u), 1))
    c[r > 0.86] = CK
    c[(u + 0.1) ** 2 + (v + 0.25) ** 2 < 0.5 ** 2] = CK
    edge = 0.3 + 0.38 * u                   # the lid lower at the inner corner: a scowl
    c[v > edge] = srgb(STRETCHY["body"])
    c[np.abs(v - edge) < 0.15] = CK
    c[(r > 0.97) & (v > edge)] = srgb(STRETCHY["body"])
    return c


def stretchy_eyes(p, state):
    look = eye_look(state, stretchy_open, srgb(STRETCHY["body"]), CK, line=0.16)
    p.decal(S_EYE_AT, S_EYE_N, S_EYE_HALF, look, eps=0.0018, rings=20, segs=56, mirror=True)


STRETCHY_ROE = np.array([0.0, -0.125, 0.162])

LOOKS = dict(curly=(curly, curly_eyes, CURLY_ROE), droopy=(droopy, droopy_eyes, DROOPY_ROE),
             stretchy=(stretchy, stretchy_eyes, STRETCHY_ROE))

# Where each look lies on the platter (at, yaw, size) for each Mega, from its pictures: the middle one a fifth larger
# and raised on a deeper heap of rice, back from the other two so that its face shows between them
RAISED = 0.03
LAYOUT = {
    "curly": dict(curly=((0.0, 0.06, RAISED), 0, 1.2), stretchy=((-0.165, -0.06, 0.012), 6, 1.0),
                  droopy=((0.165, -0.04, 0.012), -6, 1.0)),
    "droopy": dict(droopy=((0.0, 0.0, RAISED), 0, 1.2), curly=((-0.165, 0.03, 0.012), 6, 1.0),
                   stretchy=((0.16, 0.08, 0.012), -6, 1.0)),
    "stretchy": dict(stretchy=((0.0, 0.02, RAISED), 0, 1.2), droopy=((-0.165, -0.07, 0.012), 6, 1.0),
                     curly=((0.16, 0.08, 0.012), -6, 1.0)),
}


def roe(p, at, bone):
    """A heap of roe: a ring of five, two over it and one on top, each a glossy ball."""
    ring = [(0.017 * math.cos(a), 0.017 * math.sin(a), 0) for a in np.radians(np.arange(5) * 72 + 18)]
    top = [(0.009, 0.0, 0.017), (-0.009, 0.002, 0.017), (0.0, -0.001, 0.032)]
    for i, d in enumerate(ring + top):
        p.add(f"roe{i}", outward(ell(np.array(at) + np.array(d), (0.012, 0.012, 0.012), sub=3)), ROE, bone,
              fuse=False, surface="plastic")


def cloth(k):
    th = np.linspace(0, 2 * math.pi, 72, endpoint=False)
    r = 1 + 0.05 * np.sin(7 * th) + 0.035 * np.sin(13 * th + 1.0) + 0.02 * np.sin(23 * th)
    outline = np.stack([0.3 * r * np.cos(th), 0.25 * r * np.sin(th)], 1)
    k.add("cloth", slab(outline, (0, 0.02, 0.006), (1, 0, 0), (0, 1, 0), 0.012), CLOTH[0], "root", surface="cloth")
    # magenta at the left, violet in the middle, sky blue at the right, run into one another
    k.paint(lambda P: inside(-0.1 - P[:, 0], 0.07), CLOTH[1], on=["cloth"])
    k.paint(lambda P: inside(0.11 - P[:, 0], 0.07), CLOTH[2], on=["cloth"])


def grass(k, middle):
    # the baran: thick tapering blades in two clumps between the middle look and the others, faces to the front
    clumps = [((-0.09, -0.005), -1), ((0.095, 0.005), 1)]
    n = 0
    for (cx, cy), side in clumps:
        for i, (dx, dy, h, lean) in enumerate(((-0.018, 0.0, 0.22, -10), (0.0, 0.012, 0.27, 4), (0.02, -0.006, 0.19, 14),
                                               (0.008, 0.024, 0.24, -3))):
            lean = math.radians(lean * side + 6 * side)
            x, y = cx + dx * side, cy + dy
            tip = (x + h * math.sin(lean), y + 0.03, 0.012 + h * math.cos(lean))
            pts = [(x - 0.016, y, 0.012), (x + 0.016, y, 0.012), (x, y - 0.007, 0.012), (x, y + 0.007, 0.012),
                   (x - 0.008 + 0.35 * (tip[0] - x), y + 0.012, 0.012 + 0.35 * (tip[2] - 0.012)),
                   (x + 0.008 + 0.35 * (tip[0] - x), y + 0.012, 0.012 + 0.35 * (tip[2] - 0.012)), tip]
            k.add(f"grass{n}", hull(pts), GRASS[n % 2], "root", fuse=False, surface="leaf")
            n += 1


def platter(src, out, middle="curly"):
    k = Kit(src, coat="scales")
    placed = {}
    for look, (at, yaw, size) in LAYOUT[middle].items():
        p = Placed(k, look[:3] + "_", at, yaw, size, skeleton if look == middle else still)
        placed[look] = p
        LOOKS[look][0](p)
    mid = placed[middle]
    roe(mid, LOOKS[middle][2], "body" if middle == "droopy" else "head")
    # the deeper heap under the middle one, down to the cloth
    mid.add("heap", outward(ell((0, 0.035, 0.0), (0.095, 0.135, 0.026), deform=lumpy(5, 3, 0.07), sub=4)), RICE,
            "root", fuse=False, surface="cloth")
    cloth(k)
    grass(k, middle)
    # Each fused part's points by whose it is (a look's prefix, or the cloth), to split the body's surface after
    owner = [(p["name"][:4] if p["name"][:4] in ("cur_", "dro_", "str_") else "-", p["V"])
             for p in k.parts if p["fuse"] and not p["carve"]]
    k.build(voxel=0.0048, smooth=4, band=0.003, wband=0.02)
    split_surface(k, owner)

    # An eye patch is cast from 0.35 out along its normal, so an inner eye's rays met the neighbouring fish first:
    # each look's eyes are cast on that look's own share of the body and the three pieces made one for each state
    for state in ("Open", "Shut", "Squeeze", "Fierce", "Smile"):
        obs = []
        for look, p in placed.items():
            LOOKS[look][1](p, state)
            obs.append(k.build(name=f"eyes.{state}.{p.prefix}", surface=p.prefix))
        join(obs, f"eyes.{state}")
    k.export(out)


def split_surface(k, owner):
    """The body's fused surface cut into each look's own (by the nearest fused part's owner), as surfaces of the kit."""
    from mathutils.kdtree import KDTree
    P, F, W = k.surfaces[k.name]
    pts = np.vstack([V for _, V in owner])
    who = np.concatenate([[o] * len(V) for o, V in owner])
    kd = KDTree(len(pts))
    for i, v in enumerate(pts.tolist()):
        kd.insert(v, i)
    kd.balance()
    vo = np.array([who[kd.find(v)[1]] for v in P.tolist()])
    for prefix in ("cur_", "dro_", "str_"):
        keep = [f for f in F if all(vo[i] == prefix for i in f)]
        used = np.unique(np.concatenate([np.array(f) for f in keep]))
        remap = np.full(len(P), -1); remap[used] = np.arange(len(used))
        k.surfaces[prefix] = (P[used], [[int(remap[i]) for i in f] for f in keep], W[used])


def join(obs, name):
    import bpy
    for o in bpy.context.view_layer.objects:
        o.select_set(False)
    for o in obs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = obs[0]
    bpy.ops.object.join()
    obs[0].name = name
    obs[0].data.name = name


if __name__ == "__main__":
    platter(*args(), "curly")
