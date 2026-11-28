# Pidgeot-Mega-Mmax: Pidgeot-Mega's own script, whole and unchanged, with the Mega Dynamax form's additions laid on
# its kit just before it is meshed (sculptkit.py):
#   blender --background --factory-startup --python sculpts/pidgeotmegammax.py -- <the export's Pidgeot-Mega-Mmax.glb> <out.glb>
#
# From the design (docs/art/reference/gemini/megadynamax/Mega-Dynamax-Pidgeot.jpg), what it adds over the Mega,
# largest first: the tail fanned into a great spray of long six-sided crystal spears, pink, orange and cyan, sweeping
# back and down; the crest's streamers turned to crystal and two broad ribbons of pink and orange crystal looping up
# from the crown and trailing back to its right; the Key Stone's emblem glowing on its cream breast; the clouds of Max
# energy rolling about its feet and crimson puffs floating about its head.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
import sculptkit
from sculptkit import Kit, ell, lathe, ring_band, slab, sheet, srgb, unit, sibling, args, axes, outward

CRIMSON, DEEP, VIOLET, DEEPV, CYAN = "#AC223E", "#701630", "#6C3EA0", "#42246E", "#4EE2EC"
CPINK, CORANGE, CCYAN = "#F27CB8", "#FF9C58", "#72D6F0"
DISC, RING, STAR = "#FFE6C8", "#F49AC8", "#F2803C"


def emblem(k, c, n, r, bone):
    """The Key Stone's emblem: a glowing disc in a ring of another colour, a four-pointed star on it."""
    n = unit(*n)
    k.add("emblem", lathe(c, n, [(0, 0), (r, 0), (r, 0.008), (0, 0.008)], seg=32), DISC, bone, fuse=False, glow=DISC)
    k.add("emblemring", ring_band(c + n * 0.004, n, r * 1.22, 0.012, thick=0.012), RING, bone, fuse=False, glow=RING)
    a, b = axes(n)
    pts = []
    for i in range(8):
        rr = r * (0.72 if i % 2 == 0 else 0.24)
        pts.append((rr * math.cos(math.pi * i / 4), rr * math.sin(math.pi * i / 4)))
    k.add("emblemstar", slab(pts, c + n * 0.011, a, b, 0.004), STAR, bone, fuse=False, glow=STAR)


def clump(k, name, at, size, col, other, bone, tangent=(1, 0, 0), rng=None, z0=None):
    """A cloud: four or five round puffs of mixed sizes, nearly as high as wide, one small one riding on top."""
    t = np.array(tangent, float); t /= np.linalg.norm(t)
    at = np.array(at, float)
    spec = [(0.0, 1.0, 0.0), (0.72, 0.8, -0.1), (-0.7, 0.72, -0.12), (1.3, 0.52, -0.25), (0.22, 0.55, 0.72)]
    for j, (o, s, h) in enumerate(spec):
        r = size * s * (1 + 0.12 * rng.uniform(-1, 1))
        p = at + t * o * size + np.array([0, 0, (r * 0.78 if z0 is None else 0) + h * size])
        if z0 is None:
            p[2] = max(p[2], r * 0.88)          # no puff under the ground
        else:
            p[2] = at[2] + h * size
        k.add(f"{name}_{j}", ell(p, (r, r, r * 0.88)), other if j == 2 else col, bone)


def ground_clouds(k, cx, cy, rx, ry, n, size, seed=7):
    rng = np.random.default_rng(seed)
    cols = (CRIMSON, VIOLET, DEEP, DEEPV)
    for i in range(n):
        a = 2 * math.pi * (i + 0.25) / n
        p = np.array([cx + rx * math.cos(a), cy + ry * math.sin(a), 0.0])
        tang = (-rx * math.sin(a), ry * math.cos(a), 0)
        clump(k, f"cloud{i}", p, size * (1 + 0.15 * math.sin(3 * a)), cols[i % 4], cols[(i + 1) % 4], "root", tang, rng)
        if i % 3 == 1:
            q = p + np.array([0.0, 0.0, size * 1.75])
            k.add(f"curl{i}", ell(q, (size * 0.32, size * 0.32, size * 0.28), sub=2), CYAN, "root", fuse=False, glow=CYAN)


def spline(pts, n):
    """Points along a Catmull-Rom curve through pts."""
    pts = np.array(pts, float)
    ext = np.vstack([2 * pts[0] - pts[1], pts, 2 * pts[-1] - pts[-2]])
    out_pts = []
    segs = len(pts) - 1
    for t in np.linspace(0, segs, n):
        i = min(int(t), segs - 1); u = t - i
        p0, p1, p2, p3 = ext[i], ext[i + 1], ext[i + 2], ext[i + 3]
        out_pts.append(0.5 * (2 * p1 + (-p0 + p2) * u + (2 * p0 - 5 * p1 + 4 * p2 - p3) * u * u
                              + (-p0 + 3 * p1 - 3 * p2 + p3) * u ** 3))
    return np.array(out_pts)


def ribbon(k, name, pts, width, col, bone):
    """A broad ribbon of crystal along a curve, its face turned sideways so both of the game's cameras see it."""
    C = spline(pts, 60)
    T = np.gradient(C, axis=0); T /= np.linalg.norm(T, axis=1, keepdims=True)
    up = np.array([0, 0, 1.0])
    Wd = up[None] - T * (T @ up)[:, None]; Wd /= np.linalg.norm(Wd, axis=1, keepdims=True)
    def at(u, v):
        i = np.clip(np.round(u * (len(C) - 1)).astype(int), 0, len(C) - 1)
        w = width * (0.55 + 0.45 * np.sin(np.pi * np.clip(0.08 + 0.9 * u, 0, 1))) * (1 - 0.6 * u ** 3)
        return C[i] + Wd[i] * ((v - 0.5) * w)[..., None]
    k.add(name, outward(sheet(at, 60, 5, 0.018)), col, bone, fuse=False, glow=col)


def maxed(k):
    """What the Mega Dynamax form lays over its Mega: called with the Mega's kit just before it is meshed."""
    F = m.frame()
    H = F.H
    # The tail fanned into crystal spears from its base, back and down, a little toward +X for the front camera
    cols = (CCYAN, CPINK, CORANGE, CPINK)
    n = 11
    for i in range(n):
        yaw = math.radians(-36 + 90 * i / (n - 1) + 16)
        pitch = math.radians(-20 - 8 * abs(i - (n - 1) / 2) / ((n - 1) / 2))
        d = np.array([math.sin(yaw) * math.cos(pitch), math.cos(yaw) * math.cos(pitch), math.sin(pitch)])
        L = 0.64 - 0.14 * abs(i - (n - 1) / 2) / ((n - 1) / 2)
        r = 0.026
        root = F.TAILBASE + np.array([0.008 * (i - n // 2), -0.02, 0.0])
        k.add(f"spear{i}", lathe(root, d, [(0, 0), (r, 0), (r, 0.8 * L), (0, L)], seg=6), cols[i % 4], "tail",
              fuse=False, glow=cols[i % 4])
    # The crest's streamers turned to crystal (their shapes the Mega's)
    for p in k.parts:
        if p["name"] in ("streamer", "forelock"):
            p["color"], p["glow"] = srgb(CPINK), CPINK
    # Two broad crystal ribbons looping up from the crown and trailing back to its right (+X)
    ribbon(k, "ribbonA", [H + (0.0, 0.0, 0.11), H + (0.03, 0.12, 0.26), H + (0.14, 0.3, 0.3), H + (0.3, 0.44, 0.2),
                          H + (0.44, 0.52, 0.06)], 0.09, CPINK, "plume")
    ribbon(k, "ribbonB", [H + (-0.01, 0.02, 0.1), H + (0.04, 0.16, 0.2), H + (0.16, 0.3, 0.17), H + (0.3, 0.4, 0.04),
                          H + (0.42, 0.45, -0.1)], 0.08, CORANGE, "plume")
    # The emblem on the breast (pidgeot.py's frame: BREAST is the breast's ellipsoid)
    bc, br = np.array(F.BREAST[0]), np.array(F.BREAST[1])
    dn = unit(0.12, -1, 0.05)
    c = bc + dn / np.sqrt(((dn / br) ** 2).sum()) + dn * 0.008
    emblem(k, c, dn, 0.05, "body")
    # The clouds about the feet, within the wings' span
    ground_clouds(k, 0.0, 0.06, 0.38, 0.4, 12, 0.075)
    # Crimson puffs floating about the head
    rng = np.random.default_rng(11)
    for i, (deg, z) in enumerate(((70, 0.9), (150, 0.78), (215, 0.95), (285, 0.82))):
        a = math.radians(deg)
        p = np.array([0.36 * math.sin(a), -0.05 - 0.3 * math.cos(a), z])
        clump(k, f"float{i}", p, 0.045, (CRIMSON, DEEP)[i % 2], DEEP if i % 2 == 0 else CRIMSON, "body",
              (math.cos(a), math.sin(a), 0), rng, z0=True)


_build, _laid = Kit.build, []
def build(self, *a, **kw):
    if not _laid:               # the first build is the body's; the kit builds the eyes' five states after it
        _laid.append(True)
        before = {p["name"] for p in self.parts}
        maxed(self)
        clearance(self, before)
    return _build(self, *a, **kw)
Kit.build = build


def clearance(k, before):
    """Prints how near each fused addition (a cloud) comes to the Mega's own fused body, point to point."""
    from mathutils.kdtree import KDTree
    body = [p for p in k.parts if p["name"] in before and p["fuse"] and not p["carve"]]
    pts = np.vstack([p["V"] for p in body])
    owner = np.concatenate([[j] * len(p["V"]) for j, p in enumerate(body)])
    kd = KDTree(len(pts))
    for i, v in enumerate(pts.tolist()):
        kd.insert(v, i)
    kd.balance()
    worst = {}
    for p in k.parts:
        if p["name"] in before or not p["fuse"] or p["carve"]:
            continue
        d, near = min((kd.find(v)[2], kd.find(v)[1]) for v in p["V"].tolist())
        g = p["name"].split("_")[0]
        if g not in worst or d < worst[g][0]:
            worst[g] = (d, body[owner[near]]["name"])
    least = sorted(worst.items(), key=lambda e: e[1][0])[:4]
    print("KIT-x1: clearance of the clouds from the body:", ", ".join(f"{g} {d:.3f} from {n}" for g, (d, n) in least))



m = sibling("pidgeotmega")
m.pidgeotmega(*args())
