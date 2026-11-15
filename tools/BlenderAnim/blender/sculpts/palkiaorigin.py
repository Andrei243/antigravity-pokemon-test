# Palkia's Origin Forme sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/palkiaorigin.py -- <the export's Palkia-Origin.glb> <out.glb>
#
# Grown from palkia.py (its colours, its almond red eyes, its pearls and purple lines), on the form's own skeleton:
# a quadruped (body (0, 0.04, 0.54), head (0, -0.32, 0.88), tail (0, 0.3, 0.52), wings on the back at z 0.68, four
# legs from x 0.1, z 0.46). From the references: Palkia remade as a pale lilac horse of centaur's build: a barrel on
# long legs ending in pale pointed hooves, a chest rising upright in front into a thick neck, a small head with a
# pointed crest swept up and back, red eyes in dark almond surrounds. On each shoulder by the neck's root a great
# pink-orange pearl caged by purple ribs. A broad purple hoop curls on each flank, a purple band round the barrel and
# purple lines down the neck. White ribbed plates hang down the front of each foreleg from the shoulder over a purple
# band at the knee; purple bands at the hocks. Two great pale feather blades rise from the back in a V, swept back,
# and the tail is a plume of long pale tines streaming back and curling up at their ends.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, blade, sheet, srgb, inside, unit, polygon, eye_look, args, hull, slab,
                       ring_band, outward)

src, out = args()
k = Kit(src, coat="scales")

# palkia.py's family of colours; the Origin Forme's coat is a pale pink-lavender, deeper under the belly, its wings,
# tail and plates a paler, cooler lilac-white that stands apart from it
WHITE, SHADE, GREY, PURPLE = "#E6D2E4", "#D9BEDC", "#8D8999", "#9C4A8E"
PEARL, PEARLRIM, CLAW, HOOF = "#F2988C", "#7E3A78", "#E2E8F2", "#CCDAEE"
WING, WING2, EYERED, EYEDARK = "#EEEAF6", "#DCD6EC", "#D8303A", "#3A2840"
PALE, PALE2 = "#F0ECF6", "#E0DAEE"
VOXEL = 0.0078


# --- helpers (palkia.py's spline and strip; arceus.py's kite) ------------------------------------------------------
def spline(pts, radii, per=8):
    """The centre line the kit's tube runs through (its own Catmull-Rom), with tangents and radii."""
    pts = np.array(pts, float); radii = np.array(radii, float)
    ext = np.vstack([2 * pts[0] - pts[1], pts, 2 * pts[-1] - pts[-2]])
    P, R = [], []
    for i in range(len(pts) - 1):
        p0, p1, p2, p3 = ext[i], ext[i + 1], ext[i + 2], ext[i + 3]
        for j in range(per):
            t = j / per
            P.append(0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t
                            + (-p0 + 3 * p1 - 3 * p2 + p3) * t ** 3))
            R.append(radii[i] + (radii[i + 1] - radii[i]) * t)
    P.append(pts[-1]); R.append(radii[-1])
    P, R = np.array(P), np.array(R)
    T = np.gradient(P, axis=0); T /= np.linalg.norm(T, axis=1, keepdims=True)
    return P, T, R


def strip(pts, radii, ref, phi, width, t0=0.0, t1=1.0, lift=0.004, thick=0.005):
    """A crisp loose stripe lying along a tube at an angle phi from ref (turned toward T x ref), lift off its surface."""
    P, T, R = spline(pts, radii)
    n = len(P)
    ref = np.array(ref, float)
    def at(u, v):
        uu = t0 + (t1 - t0) * u
        idx = uu * (n - 1)
        i0 = np.clip(np.floor(idx).astype(int), 0, n - 2); f = (idx - i0)[..., None]
        c = P[i0] * (1 - f) + P[i0 + 1] * f
        t = T[i0] * (1 - f) + T[i0 + 1] * f; t /= np.linalg.norm(t, axis=-1, keepdims=True)
        r = (R[i0] * (1 - f[..., 0]) + R[i0 + 1] * f[..., 0])
        r1 = ref - t * (t @ ref)[..., None]; r1 /= np.linalg.norm(r1, axis=-1, keepdims=True)
        r2 = np.cross(t, r1)
        a = phi + (v - 0.5) * width / np.maximum(r, 0.02)
        d = np.cos(a)[..., None] * r1 + np.sin(a)[..., None] * r2
        return c + d * (r + lift)[..., None]
    return outward(sheet(at, 30, 3, thick))


def kite(name, base, d, wdir, length, width, thick, bone, color, mid=0.55, midw=0.8, surface="shell", tipw=0.0):
    """One solid blade: a kite in section (thin edges, a ridge down both faces), from base along d to a point."""
    base, d = np.array(base, float), unit(*d)
    w = np.array(wdir, float); w = w - d * (w @ d); w /= np.linalg.norm(w)
    n = np.cross(w, d)
    pts = []
    for t, ww in ((0.0, 1.0), (mid, midw)):
        c = base + d * length * t
        for a in (-1, 1):
            pts.append(c + w * a * width / 2 * ww)
            pts.append(c + n * a * thick / 2 * (1 - 0.35 * t))
    tip = base + d * length
    if tipw:
        pts += [tip + w * tipw, tip - w * tipw]
    else:
        pts.append(tip)
    k.add(name, hull(pts), color, bone, fuse=False, surface=surface)


# --- the trunk: a barrel on the legs, an upright chest in front, a rump behind --------------------------------------
k.add("barrel", ell((0, 0.03, 0.5), (0.108, 0.24, 0.115)), WHITE, "body")
k.add("chest", ell((0, -0.14, 0.57), (0.108, 0.115, 0.14), rot=(-18, 0, 0)), WHITE, "body")
k.add("rump", ell((0, 0.2, 0.51), (0.104, 0.115, 0.112)), WHITE, "body")
# the deeper shade under the belly and down the lower flanks
k.paint(lambda P: inside(P[:, 2] - 0.47, 0.012), SHADE, on=["barrel", "chest", "rump", "shoulder", "thigh"])

# --- the neck: thick at its root on the chest, carried up and a little forward --------------------------------------
NECK = [(0, -0.14, 0.6), (0, -0.19, 0.7), (0, -0.25, 0.79), (0, -0.3, 0.86)]
NECK_R = [0.112, 0.086, 0.064, 0.054]
k.add("neck", tube(NECK[:3], NECK_R[:3]), WHITE, "body")
k.add("throat", tube(NECK[1:], NECK_R[1:]), WHITE, "head")
for i, (t0, t1, bone) in enumerate(((0.0, 0.55, "body"), (0.5, 0.9, "head"))):
    k.add(f"neckline{i}", strip(NECK, NECK_R, (0, -1, 0), math.radians(72), 0.042, t0, t1, lift=0.003 + 0.0006 * i),
          PURPLE, bone, fuse=False, mirror=True)
# a broad purple stripe down the front of the throat, on down the chest between the pearls and along the underside
for i, (t0, t1, bone) in enumerate(((0.0, 0.55, "body"), (0.5, 0.86, "head"))):
    k.add(f"throatline{i}", strip(NECK, NECK_R, (0, -1, 0), 0.0, 0.036, t0, t1, lift=0.003 + 0.0006 * i),
          PURPLE, bone, fuse=False)
def chest_stripe(P):
    x, y, z = P[:, 0], P[:, 1], P[:, 2]
    down_front = inside(np.abs(x) - 0.02, 0.003) * inside(y + 0.17, 0.004) * inside(z - 0.62, 0.004) * inside(0.44 - z, 0.004)
    under = inside(np.abs(x) - 0.022, 0.003) * inside(z - 0.43, 0.004) * inside(y - 0.14, 0.004) * inside(-0.2 - y, 0.004)
    return np.maximum(down_front, under)
k.paint(chest_stripe, PURPLE, on=["barrel", "chest", "rump", "neck"])

# --- the head: small and narrow, the muzzle forward and a little down, a crest swept up and back --------------------
# drawn at its first size and then grown by HS about the head's joint (with its crest, fins, mouth and eyes)
J, HS = np.array([0, -0.32, 0.88]), 1.25
def g(p):
    return J + (np.asarray(p, float) - J) * HS
HEAD_FIRST = len(k.parts)
H = np.array([0, -0.335, 0.9])
HR = np.array([0.064, 0.08, 0.062])
k.add("skull", ell(H, HR), WHITE, "head")
k.add("snout", ell((0, -0.41, 0.884), (0.046, 0.068, 0.04)), WHITE, "head")
k.add("jaw", ell((0, -0.39, 0.846), (0.038, 0.066, 0.024), rot=(20, 0, 0)), WHITE, "head")
# the crest: a broad fin from the brow sweeping up and back to a point (laid back, so it does not stand high)
k.add("crest", blade([(0, -0.43, 0.925), (0, -0.36, 0.965), (0, -0.29, 0.99), (0, -0.23, 1.0), (0, -0.18, 0.998)],
                     [0.042, 0.054, 0.048, 0.034, 0.018], (0, 0, 1), 0.5), WHITE, "head")
# a short fin back from each jaw's corner, as palkia.py's
k.add("cheekfin", blade([(0.038, -0.345, 0.858), (0.052, -0.305, 0.85), (0.062, -0.27, 0.838)],
                        [0.016, 0.011, 0.002], (1, 0, 0.2), 0.5), WHITE, "head", fuse=False, mirror=True)
for p in k.parts[HEAD_FIRST:]:
    p["V"] = J + (p["V"] - J) * HS
H, HR = g(H), HR * HS
# a closed mouth: one short pink line along each side of the jaw
MOUTH_POLY = polygon([(-1.0, -0.5), (1.0, -0.25), (1.0, 0.25), (-1.0, 0.5)])
MN = unit(1, -0.3, -0.05)
k.decal(g((0.036, -0.43, 0.862)), MN, (0.04, 0.0075), "#C8607A", eps=0.004, outline=MOUTH_POLY, rings=8, segs=40)
k.decal(g((-0.036, -0.43, 0.862)), MN * (-1, 1, 1), (0.04, 0.0075), "#C8607A", eps=0.004,
        outline=lambda th: MOUTH_POLY(math.pi - th), rings=8, segs=40)
# a purple mark from under each eye back along the cheek toward the throat
CHEEK_POLY = polygon([(-1.0, -0.2), (-0.3, -0.7), (0.6, -0.6), (1.0, 0.0), (0.6, 0.55), (-0.3, 0.6)])
CN = unit(1, -0.05, -0.15)
k.decal(g((0.056, -0.315, 0.878)), CN, (0.046, 0.014), PURPLE, eps=0.004, outline=CHEEK_POLY, rings=8, segs=48,
        up=(0, 0.25, 1))
k.decal(g((-0.056, -0.315, 0.878)), CN * (-1, 1, 1), (0.046, 0.014), PURPLE, eps=0.004,
        outline=lambda th: CHEEK_POLY(math.pi - th), rings=8, segs=48, up=(0, 0.25, 1))

# --- the shoulders: a great pink-orange pearl beside the neck's root, caged by purple ribs ---------------------------
B = np.array([0.165, -0.15, 0.705])
RB = 0.088
k.add("mount", ell((0.11, -0.14, 0.655), (0.072, 0.075, 0.07)), WHITE, "body", mirror=True)
k.add("pearl", ell(B, (RB, RB, RB), sub=4), PEARL, "body", fuse=False, mirror=True, glow=PEARL, surface="shell")
gl = unit(0.45, -0.6, 0.65)
k.add("glint", ell(B + gl * (RB - 0.006), (0.016, 0.016, 0.012), sub=2), "#FFE8DE", "body", fuse=False, mirror=True,
      surface="shell")
# the cage: a band round the pearl's equator and two meeting over its outer face, a cross of panels seen from the front
for i, ax in enumerate(((0, 0, 1), (0.7, 0.7, 0.0), (0.7, -0.4, 0.6))):
    k.add(f"rib{i}", ring_band(B, ax, RB + 0.002, 0.028, thick=0.014), PURPLE, "body", fuse=False, mirror=True,
          surface="shell")

# --- the purple loops: a band round the barrel and a broad hoop curling on each flank -------------------------------
k.add("girth", ring_band((0, -0.05, 0.525), (0, 1, 0), 0.112, 0.05, thick=0.01), PURPLE, "body", fuse=False)
HOOP = np.array([0.122, 0.07, 0.515])
HA = unit(1, -0.4, 0.05)
k.add("hoop", ring_band(HOOP, HA, 0.098, 0.032, thick=0.032), PURPLE, "body", fuse=False, mirror=True)
k.add("curl", ring_band(HOOP + HA * 0.006 + np.array([0, -0.03, 0.012]), HA, 0.045, 0.026, thick=0.022), PURPLE,
      "body", fuse=False, mirror=True)

# --- the legs: broad shoulders and thighs fused over long tapering legs, pale pointed hooves ------------------------
LX = 0.1
for tag, y, bone, hind in (("F", -0.16, "leg", 0), ("H", 0.22, "hind", 1)):
    if hind:
        # a broad thigh fused into the rump, the stifle forward, the hock bent back, a tapering cannon
        k.add("thigh", ell((LX - 0.006, y + 0.005, 0.41), (0.072, 0.112, 0.15)), WHITE, bone + "{s}", mirror=True)
        LEG = [(LX, y, 0.4), (LX, y - 0.02, 0.3), (LX, y + 0.065, 0.2), (LX, y + 0.042, 0.13), (LX, y + 0.02, 0.075)]
        LEG_R = [0.07, 0.054, 0.037, 0.029, 0.025]
    else:
        # a broad shoulder fused into the chest, the knee a little forward, a tapering cannon
        k.add("shoulder", ell((LX, y - 0.01, 0.41), (0.076, 0.095, 0.15)), WHITE, bone + "{s}", mirror=True)
        LEG = [(LX, y, 0.4), (LX, y - 0.005, 0.31), (LX, y - 0.035, 0.225), (LX, y - 0.015, 0.14), (LX, y + 0.008, 0.075)]
        LEG_R = [0.066, 0.05, 0.038, 0.029, 0.025]
    knee = LEG[2]
    k.add("limb" + tag, tube(LEG, LEG_R), WHITE, bone + "{s}", mirror=True)
    # a broad purple band at the knee or the hock
    kn = np.array(knee)
    k.add("joint" + tag, ring_band(kn + (0, 0, 0.006), (0, 0.15 * (1 if hind else -1), 1), 0.043, 0.042, thick=0.009),
          PURPLE, bone + "{s}", fuse=False, mirror=True)
    ank = LEG[-1]
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        a = np.array(ank) * m
        tip = np.array((s * LX, y - 0.05, 0.0))
        top = [a + (0.024 * math.cos(q), 0.024 * math.sin(q), 0.02) for q in np.linspace(0, 2 * math.pi, 7, endpoint=False)]
        midr = [a + (0.019 * math.cos(q), -0.012 + 0.019 * math.sin(q), -0.045) for q in np.linspace(0, 2 * math.pi, 6, endpoint=False)]
        k.add(f"hoof{tag}{side}", hull(top + midr + [tip]), HOOF, bone + side, fuse=False, surface="shell")
# the white ribbed plates hanging down the front of each foreleg from the shoulder toward the knee
for s, side in ((1, "R"), (-1, "L")):
    for j, dx in enumerate((-0.03, 0.0, 0.03)):
        kite(f"plate{j}{side}", (s * (LX + dx), -0.262 + 0.014 * abs(dx) / 0.03, 0.49), (s * dx * 0.6, -0.1, -1),
             (1, 0, 0), 0.25 - 0.035 * abs(dx) / 0.03, 0.036, 0.032, "leg" + side, PALE if j != 1 else PALE2,
             mid=0.5, midw=0.85)

# --- the wings: two great pale feather blades a side, rising back from the shoulders in a V -------------------------
WROOT = np.array([0.08, 0.04, 0.68])
k.add("wingroot", ell(WROOT + (0, 0, 0.0), (0.045, 0.05, 0.045)), WHITE, "wing{s}", mirror=True)
for j, (ex, L, wdt, col) in enumerate((((0.36, 0.55, 0.75), 0.47, 0.1, WING), ((0.44, 0.72, 0.53), 0.39, 0.085, WING2))):
    ex = unit(*ex)
    ez = np.cross(ex, (0, 1.0, 0)); ez /= np.linalg.norm(ez)
    if ez[0] < 0:
        ez = -ez
    outline = [(0.0, -0.025), (L * 0.55, -0.03), (L, 0.004), (L * 0.82, 0.05), (L * 0.35, wdt), (0.0, 0.045)]
    k.add(f"feather{j}", slab(outline, WROOT + ex * 0.02 + np.array([0, 0.012 * j, 0]), ex, ez, 0.034), col,
          "wing{s}", fuse=False, mirror=True)

# --- the tail: a plume of long pale tines streaming back and a little out to +X, curling up at their ends ----------
k.add("tailroot", ell((0, 0.31, 0.545), (0.055, 0.06, 0.055)), WHITE, "tail")
TR = np.array([0, 0.33, 0.55])
for j, (elev, yaw, L) in enumerate(((-28, 26, 0.24), (-12, 32, 0.28), (4, 36, 0.3), (20, 30, 0.27), (36, 22, 0.22))):
    e, a = math.radians(elev), math.radians(yaw)
    d1 = np.array([math.sin(a) * math.cos(e), math.cos(a) * math.cos(e), math.sin(e)])
    e2 = e + math.radians(32)
    d2 = np.array([math.sin(a) * math.cos(e2), math.cos(a) * math.cos(e2), math.sin(e2)])
    b = TR + np.array([0, 0, 0.01 * (j - 2)])
    kite(f"tine{j}a", b, d1, (0, 0, 1) if abs(elev) < 60 else (1, 0, 0), L * 0.62, 0.05, 0.034, "tail",
         PALE if j % 2 == 0 else PALE2, mid=0.7, midw=0.85, surface="scales", tipw=0.016)
    kite(f"tine{j}b", b + d1 * L * 0.6, d2, (0, 0, 1), L * 0.5, 0.036, 0.026, "tail", PALE if j % 2 == 0 else PALE2,
         mid=0.45, midw=0.8, surface="scales")

# --- the eyes: palkia.py's red eye in a dark almond surround, slanting up toward the back ---------------------------
EYE_OUT = [(-1.0, -0.15), (-0.55, 0.3), (0.1, 0.62), (1.0, 0.95), (0.8, 0.25), (0.3, -0.45), (-0.4, -0.55)]
EYE_POLY = polygon(EYE_OUT)
CR, CD, CK, CW, CL = srgb(EYERED), srgb(EYEDARK), srgb("#120A10"), srgb("#FFFFFF"), srgb(WHITE)


def eye(u, v):
    th = np.arctan2(v, u)
    reach = np.array([EYE_POLY(t) for t in th])
    rho = np.sqrt(u * u + v * v) / reach
    c = np.tile(CD, (len(u), 1))
    c[rho < 0.56] = CR
    c[((u + 0.05) / 0.24) ** 2 + ((v - 0.02) / 0.34) ** 2 < 1] = CK
    c[((u + 0.18) / 0.1) ** 2 + ((v - 0.2) / 0.12) ** 2 < 1] = CW
    return c


de = unit(0.62, -0.72, 0.22)
EC = H + de / math.sqrt(((de / HR) ** 2).sum()) + np.array([0, -0.01, 0.0])
EH = (0.05, 0.036)


def eyes(state):
    look = eye_look(state, eye, CL, CD, line=0.16)
    k.decal(EC, de, EH, look, eps=0.005, rings=20, segs=56, outline=EYE_POLY)
    m = np.array([-1, 1, 1.0])
    k.decal(EC * m, de * m, EH, lambda u, v: look(-u, v), eps=0.005, rings=20, segs=56,
            outline=lambda th: EYE_POLY(math.pi - th))


k.build(voxel=VOXEL, smooth=4, band=0.004)
k.eye_states(eyes)
k.export(out)
