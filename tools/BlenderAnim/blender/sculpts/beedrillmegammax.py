# Beedrill-Mega-Mmax: Beedrill-Mega's own script, whole and unchanged, with the Mega Dynamax form's additions laid
# on its kit just before it is meshed (sculptkit.py):
#   blender --background --factory-startup --python sculpts/beedrillmegammax.py -- <the export's Beedrill-Mega-Mmax.glb> <out.glb>
#
# From the design (docs/art/reference/gemini/megadynamax/Mega-Dynamax-Beedrill.jpg), what it adds over the Mega,
# largest first: its four great stingers and the tail's sheathed in long six-sided spears of glowing violet crystal,
# each from the middle of the white stinger to well past its point; the four wings turned to stained glass, violet
# with cyan panels between dark leading; the Key Stone's emblem glowing on its front; the clouds of Max energy rolling
# on the ground under it with two cyan bolts of lightning in them, and crimson puffs about its middle.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
import sculptkit
from sculptkit import Kit, ell, tube, lathe, ring_band, slab, srgb, unit, sibling, args, axes

CRIMSON, DEEP, VIOLET, DEEPV, CYAN = "#AC223E", "#701630", "#6C3EA0", "#42246E", "#4EE2EC"
SPEAR, SPEAR2 = "#A060F0", "#C27CF6"
GLASS, PANEL, LEAD = "#9A48D8", "#58DCEE", "#2E1E48"
DISC, RING, STAR = "#E8D2FF", "#B47CFF", "#FFFFFF"


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
    k.add("emblemstar", slab(pts, c + n * 0.011, a, b, 0.004), "#C04CE0", bone, fuse=False, glow="#C04CE0")


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


def bolt(k, name, pts):
    """A jagged bolt of cyan lightning in the clouds, thick enough to stay whole."""
    k.add(name, tube(pts, [0.016] * len(pts), seg=8, per=2), CYAN, "root", fuse=False, glow=CYAN)


def maxed(k):
    """What the Mega Dynamax form lays over its Mega: called with the Mega's kit just before it is meshed."""
    # The forearms' lances sheathed in crystal from their middles to past their points (beedrillmega.py's lances:
    # the lathe from ELBOW - LANCE_D * 0.02 along LANCE_D, 0.07 at its widest, its point at 0.45)
    k.add("lancecrystal", lathe(m.ELBOW - m.LANCE_D * 0.02, m.LANCE_D,
                                [(0, 0.14), (0.076, 0.14), (0.059, 0.3), (0.029, 0.48), (0, 0.6)], seg=6),
          SPEAR, "arm{s}", fuse=False, mirror=True, glow=SPEAR)
    # The hind legs' stingers the same (from KNEE - LEG_D * 0.01 along LEG_D, its point at 0.3)
    k.add("legcrystal", lathe(m.KNEE - m.LEG_D * 0.01, m.LEG_D,
                              [(0, 0.06), (0.058, 0.06), (0.045, 0.16), (0.021, 0.32), (0, 0.4)], seg=6),
          SPEAR2, "hind{s}", fuse=False, mirror=True, glow=SPEAR2)
    # The tail's stinger (from AB_TOP + AB_AX * (AB_LEN - 0.07), its root radius r0)
    base = m.AB_TOP + m.AB_AX * (m.AB_LEN - 0.07)
    r0 = float(m.ab_radius(m.AB_LEN - 0.07))
    k.add("tailcrystal", lathe(base, m.AB_AX, [(0, 0.05), (r0 * 1.06 + 0.007, 0.05), (r0 * 0.53 + 0.005, 0.15),
                                              (0, 0.25)], seg=6), SPEAR, "body", fuse=False, glow=SPEAR)
    # The wings turned to stained glass: violet, the veins dark leading (shapes unchanged)
    for p in k.parts:
        if p["name"].startswith("wing"):
            if "vein" in p["name"]:
                p["color"] = srgb(LEAD)
            else:
                p["color"], p["glow"] = srgb(GLASS), GLASS
    # cyan panels on each wing, thicker than the wing so they show on both faces, thinner than the leading
    for name, root, L, W, sweep, lift, twist in (("wingU", (0.04, 0.05, 0.72), 0.34, 0.085, -14, 52, 45),
                                                 ("wingD", (0.04, 0.08, 0.69), 0.27, 0.07, -46, 34, -25)):
        ex, ez = m.wing_axes(sweep, lift, twist)
        o = np.array(root, float)
        for j, (t0, t1, sgn) in enumerate(((0.42, 0.6, 1), (0.66, 0.82, -1))):
            outline = [(t0 * L + 0.012, sgn * 0.01 * W), (t1 * L - 0.006, sgn * 0.01 * W),
                       (t1 * L - 0.006, sgn * 0.62 * W), (t0 * L + 0.012, sgn * 0.62 * W)]
            k.add(f"{name}panel{j}", slab(outline, o, ex, ez, 0.0145), PANEL, "wing{s}", fuse=False, mirror=True,
                  glow=PANEL)
    # The emblem on its front, on the abdomen's top between the thorax and the first band, facing the front camera
    n = unit(0.12, -1, 0.05)
    c = m.AB_TOP + m.AB_AX * 0.055
    r = float(m.ab_radius(0.055))
    c = c + n * (r + 0.016)
    emblem(k, c, n, 0.036, "body")
    # The clouds on the ground under it, within 1.2 times its own footprint
    ground_clouds(k, 0.0, 0.0, 0.36, 0.27, 11, 0.064)
    bolt(k, "boltR", [(0.3, -0.12, 0.05), (0.26, -0.15, 0.13), (0.3, -0.13, 0.2), (0.25, -0.17, 0.27)])
    bolt(k, "boltL", [(-0.3, 0.12, 0.05), (-0.27, 0.16, 0.13), (-0.31, 0.13, 0.21), (-0.26, 0.17, 0.28)])
    # Crimson puffs about its middle, behind the stingers and clear of the face
    rng = np.random.default_rng(11)
    for i, (deg, z) in enumerate(((110, 0.58), (200, 0.72), (250, 0.5))):
        a = math.radians(deg)
        p = np.array([0.3 * math.sin(a), 0.02 - 0.26 * math.cos(a), z])
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



m = sibling("beedrillmega")
m.beedrillmega(*args())
