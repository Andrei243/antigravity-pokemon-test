# Palkia sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/palkia.py -- <the export's Palkia.glb> <out.glb>
#
# From the references: a tall pale biped, white with a lilac tint, leaning a little forward on two heavy legs, a long
# thick tail lying behind and curling up at its end. A long neck thick at its root carries a small flat head forward:
# a broad crest over the brow sweeping far back over the neck, red eyes in dark almond surrounds under it, the jaws
# open on a pink mouth with two small white fangs. On each shoulder a great white ball of armour, a pink pearl set in
# its side in a purple ring, purple ribs running out from the ring. The chest and the arms are grey; each forearm
# carries a white ribbed fan sweeping back from a purple cuff, the hand's white claws pointing forward. Long pale fin
# wings in three blades rise back from the shoulders. Purple lines run down the neck's sides, the thighs' fronts and
# along the tail; a purple band at each ankle over a white foot with three big claws.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, sheet, srgb, inside, smoothstep, unit, polygon, eye_look, args

src, out = args()
k = Kit(src, coat="scales")

WHITE, SHADE, GREY, PURPLE = "#ECE6F2", "#D9D1E3", "#8D8999", "#9C4A8E"
PEARL, PEARLRIM, CLAW, MOUTH = "#F2988C", "#7E3A78", "#E2E8F2", "#D8707E"
WING, WING2, EYERED, EYEDARK = "#E6E0EE", "#D6CEE2", "#D8303A", "#3A2840"
VOXEL = 0.0089


# --- helpers -----------------------------------------------------------------------------------------------------------
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
    s = np.linspace(0, 1, n)
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
    return sheet(at, 30, 3, thick)


def ring_band(c, axis, radius, length, thick=0.006, nu=73):
    """electabuzz.py's loose ring round a limb, its seam behind."""
    a = unit(*axis)
    ref = np.array([0, 0, 1.0]) if abs(a[2]) < 0.9 else np.array([0, 1.0, 0])
    e1 = np.cross(a, ref); e1 /= np.linalg.norm(e1)
    e2 = np.cross(a, e1)
    back = np.array([0, 1.0, 0]) - a * a[1]
    phase = math.atan2(np.dot(back, e2), np.dot(back, e1)) if np.linalg.norm(back) > 1e-6 else 0.0
    def at(u, v):
        th = phase + 2 * math.pi * u
        off = (v - 0.5) * length
        return (np.asarray(c)[None, None, :] + a[None, None, :] * off[..., None]
                + radius * (np.cos(th)[..., None] * e1[None, None, :] + np.sin(th)[..., None] * e2[None, None, :]))
    return sheet(at, nu, 3, thick)


def area(p):
    return 0.5 * np.sum(p[:, 0] * np.roll(p[:, 1], -1) - np.roll(p[:, 0], -1) * p[:, 1])


def earclip(p):
    idx = list(range(len(p)))
    tris = []
    def cross(o, a, b):
        return (a[0] - o[0]) * (b[1] - o[1]) - (a[1] - o[1]) * (b[0] - o[0])
    guard = 0
    while len(idx) > 3 and guard < 100000:
        guard += 1
        m = len(idx)
        cut = False
        for i in range(m):
            a, b, c = idx[i - 1], idx[i], idx[(i + 1) % m]
            if cross(p[a], p[b], p[c]) <= 1e-12:
                continue
            if any(cross(p[a], p[b], p[j]) >= 0 and cross(p[b], p[c], p[j]) >= 0 and cross(p[c], p[a], p[j]) >= 0
                   for j in idx if j not in (a, b, c)):
                continue
            tris.append((a, b, c)); idx.pop(i); cut = True
            break
        if not cut:
            tris.append((idx[-1], idx[0], idx[1])); idx.pop(0)
    tris.append(tuple(idx))
    return tris


def flat(outline, origin, ex, ez, thick):
    """yanmega.py's flat plate: a polygon in the plane of ex, ez about origin, its frame right-handed by making."""
    ex, ez = unit(*ex), unit(*ez)
    nrm = np.cross(ex, ez); nrm /= np.linalg.norm(nrm)
    p = np.array(outline, float)
    if area(p) < 0:
        p = p[::-1]
    W = np.array(origin, float) + p[:, :1] * ex + p[:, 1:] * ez
    m = len(p)
    front, back = W + nrm * thick / 2, W - nrm * thick / 2
    F = []
    for a, b, c in earclip(p):
        F.append([a, b, c]); F.append([m + c, m + b, m + a])
    rim = []
    for i in range(m):
        j = (i + 1) % m
        rim.extend([front[i], back[i], back[j], front[j]])
        F.append([2 * m + 4 * i + q for q in range(4)])
    return np.vstack([front, back, np.array(rim)]), F


def frame(ex, ez):
    """An orthonormal pair: ex, and ez made square to it."""
    ex = unit(*ex); ez = np.array(ez, float)
    ez = ez - ex * np.dot(ez, ex)
    return ex, ez / np.linalg.norm(ez)


def claw(base, d, length, r):
    d = unit(*d); base = np.array(base, float)
    return tube([base, base + d * length * 0.55 + (0, 0, -0.004), base + d * length], [r, r * 0.65, 0.002])


# --- the trunk: a deep chest leaning a little forward over broad hips --------------------------------------------------
k.add("torso", ell((0, -0.015, 0.57), (0.142, 0.13, 0.165), rot=(18, 0, 0)), WHITE, "body")
k.add("hips", ell((0, 0.035, 0.42), (0.165, 0.15, 0.125)), WHITE, "body")
# The grey chest: a shield down the front from under the neck to the waist
def chest(P):
    x, y, z = P[:, 0], P[:, 1], P[:, 2]
    half = np.interp(z, [0.44, 0.5, 0.62, 0.7], [0.0, 0.07, 0.1, 0.075])
    return inside(np.abs(x) - half, 0.006) * inside(y + 0.06, 0.006) * inside(z - 0.71, 0.006) * inside(0.45 - z, 0.006)
k.paint(chest, GREY)

# --- the neck: thick at its root, carrying the head up and forward ------------------------------------------------------
NECK = [(0, -0.01, 0.63), (0, -0.06, 0.74), (0, -0.118, 0.83), (0, -0.17, 0.89)]
NECK_R = [0.1, 0.08, 0.066, 0.058]
k.add("neck", tube(NECK[:3], NECK_R[:3]), WHITE, "body")
k.add("throat", tube(NECK[1:], NECK_R[1:]), WHITE, "head")
# a bold purple line down each side of the neck to the chest (lower half on the body, upper on the head)
for i, (t0, t1, bone) in enumerate(((0.0, 0.55, "body"), (0.5, 0.92, "head"))):
    k.add(f"neckline{i}", strip(NECK, NECK_R, (0, -1, 0), math.radians(72), 0.05, t0, t1, lift=0.003 + 0.0006 * i),
          PURPLE, bone, fuse=False, mirror=True)

# --- the head: a clear mass carried forward on the neck; a broad crest sweeping back over it ----------------------------
# drawn at its first size about the old neck's top, then grown by HS about the new neck's top
HJ0, HJ, HS = np.array([0, -0.15, 0.858]), np.array(NECK[-1]), 1.25
def hp(p):
    return HJ + (np.array(p, float) - HJ0) * HS
def hr(r):
    return np.array(r, float) * HS
H = hp((0, -0.17, 0.885))
HR = hr((0.063, 0.08, 0.056))
k.add("skull", ell(H, HR), WHITE, "head")
k.add("snout", ell(hp((0, -0.245, 0.873)), hr((0.045, 0.067, 0.033))), WHITE, "head")
k.add("jaw", ell(hp((0, -0.226, 0.826)), hr((0.037, 0.064, 0.02)), rot=(22, 0, 0)), WHITE, "head")
# the open mouth: one crisp pink wedge along each side of the jaws, widest at the front, and a lens across the front
MOUTH_POLY = polygon([(-1.0, -0.85), (-0.4, -0.6), (0.4, -0.3), (1.0, -0.06), (1.0, 0.06), (0.4, 0.22),
                      (-0.4, 0.5), (-1.0, 0.85)])
MN = unit(1, -0.28, -0.05)
MC = hp((0.036, -0.248, 0.848))
k.decal(MC, MN, hr((0.044, 0.016)), MOUTH, eps=0.004, outline=MOUTH_POLY, rings=12, segs=64)
k.decal(MC * (-1, 1, 1), MN * (-1, 1, 1), hr((0.044, 0.016)), MOUTH, eps=0.004,
        outline=lambda th: MOUTH_POLY(math.pi - th), rings=12, segs=64)
k.decal(hp((0, -0.29, 0.848)), unit(0, -1, -0.08), hr((0.03, 0.013)), MOUTH, eps=0.004, rings=10, segs=48)
k.add("crest", blade([hp(p) for p in [(0, -0.258, 0.902), (0, -0.17, 0.935), (0, -0.07, 0.955), (0, 0.04, 0.95),
                                      (0, 0.11, 0.935)]],
                     hr([0.045, 0.06, 0.055, 0.04, 0.026]), (0, 0, 1), 0.48), WHITE, "head")
# two small white fangs down from the upper jaw's front
for s in (1, -1):
    b = hp((0.022 * s, -0.295, 0.862))
    k.add(f"fang{'R' if s > 0 else 'L'}", tube([b, b + hr((0, -0.003, -0.012)), b + hr((0, -0.004, -0.022))],
                                              hr([0.007, 0.005, 0.0012])), CLAW, "head", fuse=False, surface="shell")
# a short fin back from each jaw's corner
k.add("cheekfin", blade([hp(p) for p in [(0.035, -0.17, 0.842), (0.05, -0.135, 0.832), (0.06, -0.105, 0.818)]],
                        hr([0.014, 0.01, 0.002]), (1, 0, 0.2), 0.5), WHITE, "head", fuse=False, mirror=True)

# --- the shoulders: great balls of armour on the body, a pink pearl in each side --------------------------------------
B = np.array([0.235, -0.005, 0.645])
RB = 0.118
A = unit(1.0, -0.84, 0.32)              # the pearl's axis: out, turned some 40 degrees to the front, a little up
k.add("pauldron", ell(B, (RB, RB, RB)), WHITE, "body", mirror=True)
k.add("pearl", ell(B + A * (RB - 0.014), (0.05, 0.05, 0.05), sub=4), PEARL, "body", fuse=False, mirror=True,
      glow=PEARL, surface="shell")
gl = unit(*(A + np.array([0, -0.35, 0.75])))
k.add("glint", ell(B + A * (RB - 0.014) + gl * 0.043, (0.012, 0.012, 0.012), sub=2), "#FFFFFF", "body", fuse=False,
      mirror=True, surface="shell")
SPOKES = np.radians([30, 90, 150, 210, 270, 330])
def pauldron_look(u, v):
    rho = np.sqrt(u * u + v * v)
    phi = np.arctan2(v, u)
    c = np.tile(srgb(WHITE), (len(u), 1))
    c[(rho > 0.48) & (rho < 0.67)] = srgb(PEARLRIM)
    d = np.min(np.abs(((phi[:, None] - SPOKES[None, :]) + math.pi) % (2 * math.pi) - math.pi), axis=1)
    arc = d * rho * 0.84 * RB
    c[(rho >= 0.65) & (arc < 0.012 + 0.006 * (rho - 0.65))] = srgb(PURPLE)
    return c
k.decal(B + A * RB, A, (0.84 * RB, 0.84 * RB), pauldron_look, eps=0.004, rings=24, segs=72, mirror=True)
# the ribs go on round the ball's back: spokes meeting at its rear-outer pole, as on a ball of segments
AR = unit(0.55, 0.8, 0.25)
def pauldron_back(u, v):
    rho = np.sqrt(u * u + v * v)
    phi = np.arctan2(v, u)
    c = np.tile(srgb(WHITE), (len(u), 1))
    d = np.min(np.abs(((phi[:, None] - SPOKES[None, :]) + math.pi) % (2 * math.pi) - math.pi), axis=1)
    c[(rho > 0.12) & (d * rho * 0.66 * RB < 0.013)] = srgb(PURPLE)
    c[rho < 0.2] = srgb(PURPLE)
    return c
k.decal(B + AR * RB, AR, (0.66 * RB, 0.66 * RB), pauldron_back, eps=0.0035, rings=20, segs=72, mirror=True)

# --- the arms: grey, short, down and forward; a white ribbed fan back from each wrist's purple cuff --------------------
SH = np.array([0.17, -0.04, 0.585])
EL = np.array([0.235, -0.07, 0.475])
WR = np.array([0.225, -0.2, 0.44])
k.add("arm", tube([SH, (SH + EL) / 2, EL, (EL + WR) / 2, WR], [0.058, 0.055, 0.05, 0.05, 0.052]), GREY, "arm{s}",
      mirror=True)
FD = unit(*(WR - EL))
k.add("hand", ell(WR + FD * 0.035, (0.042, 0.046, 0.036)), GREY, "arm{s}", mirror=True)
k.add("cuff", ring_band(WR - FD * 0.018, FD, 0.056, 0.045), PURPLE, "arm{s}", fuse=False, mirror=True)
for j, (dx, dz) in enumerate(((-0.022, 0.012), (0.0, 0.02), (0.022, 0.012))):
    b = WR + FD * 0.07 + np.array([dx, 0, dz - 0.012])
    k.add(f"claw{j}", claw(b, (dx * 2, -1, -0.35), 0.05, 0.011), CLAW, "arm{s}", fuse=False, mirror=True,
          surface="shell")
# the fan: wedges radiating back from the outer side of the wrist, white and a shade, each scalloped
FO = WR + np.array([0.045, 0.01, 0.012])
fx, fz = frame((0.42, 1, 0.05), (0, 0, 1))
ANG = np.radians([-46, -26, -8, 10, 28, 44])
for j in range(len(ANG) - 1):
    a0, a1 = ANG[j], ANG[j + 1]; am = (a0 + a1) / 2
    r = 0.215 - 0.035 * abs(am) / 0.8
    outline = [(0.0, 0.0), (r * math.cos(a0), r * math.sin(a0)), (1.08 * r * math.cos(am), 1.08 * r * math.sin(am)),
               (r * math.cos(a1), r * math.sin(a1))]
    k.add(f"fan{j}", flat(outline, FO, fx, fz, 0.014 if j % 2 else 0.02), WHITE if j % 2 == 0 else SHADE, "arm{s}",
          fuse=False, mirror=True)

# --- the wings: long pale fins of three blades rising back and out from the shoulders ----------------------------------
WROOT = np.array([0.11, 0.075, 0.69])
k.add("wingroot", ell(WROOT, (0.045, 0.05, 0.045)), WHITE, "wing{s}", mirror=True)
wx, wz = frame((0.27, 0.92, 0.28), (0.6, 0, -0.8))
WL = 0.67
for j in range(3):
    a = math.radians(10 * j)
    ex = math.cos(a) * wx + math.sin(a) * wz
    ez = -math.sin(a) * wx + math.cos(a) * wz
    L = WL * (1 - 0.13 * j)
    outline = [(0.0, -0.035), (L * 0.55, -0.03), (L, 0.0), (L * 0.92, 0.075), (L * 0.5, 0.1), (0.0, 0.07)]
    nrm = np.cross(ex, ez)
    k.add(f"wing{j}", flat(outline, WROOT + nrm * (0.014 * j), ex, ez, 0.024), WING if j % 2 == 0 else WING2,
          "wing{s}", fuse=False, mirror=True)

# --- the legs: heavy thighs fused into the hips, the knee forward, a white foot with three big claws -------------------
k.add("thigh", ell((0.14, -0.045, 0.3), (0.108, 0.135, 0.14), rot=(-22, 0, 0)), WHITE, "leg{s}", mirror=True)
SHIN = [(0.155, -0.105, 0.2), (0.16, -0.03, 0.125), (0.16, 0.012, 0.07)]
k.add("shin", tube(SHIN, [0.07, 0.058, 0.052]), WHITE, "leg{s}", mirror=True)
k.add("foot", ell((0.16, -0.035, 0.036), (0.066, 0.1, 0.038)), WHITE, "leg{s}", mirror=True)
k.add("ankle", ring_band((0.16, 0.006, 0.098), (0, 0.3, 1), 0.06, 0.04), PURPLE, "leg{s}", fuse=False, mirror=True)
for j, dx in enumerate((-0.035, 0.0, 0.035)):
    b = np.array([0.16 + dx, -0.115, 0.034])
    k.add(f"toe{j}", claw(b, (dx * 1.5, -1, -0.25), 0.05, 0.018), CLAW, "leg{s}", fuse=False, mirror=True,
          surface="shell")
# two bold purple lines down the front of each thigh from hip to knee: long bent bands, patches, crisp. In the
# patch's own u (across, 0.06 a unit) and v (down the thigh, 0.14 a unit) the band bows outward at its middle
def band_outline(width=0.38, bow=0.16):
    vs = np.linspace(-1, 1, 15)
    mid = bow * (1 - vs ** 2) - bow * 0.45
    half = width * np.sqrt(np.clip(1 - vs ** 8, 0.05, 1))
    pts = [(m + h, v) for m, h, v in zip(mid, half, vs)] + [(m - h, v) for m, h, v in zip(mid[::-1], half[::-1], vs[::-1])]
    return polygon(pts)
BAND = band_outline()
for c, n in (((0.11, -0.14, 0.32), (0.1, -1, 0)), ((0.19, -0.12, 0.31), (0.6, -1, 0))):
    k.decal(c, unit(*n), (0.06, 0.12), PURPLE, up=(0, 0, 1), eps=0.004, outline=BAND, rings=12, segs=96, mirror=True)
# and on down each shin's front
k.add("shinline", strip(SHIN, [0.07, 0.058, 0.052], (0, -1, 0), math.radians(40), 0.038, 0.05, 0.7, lift=0.003),
      PURPLE, "leg{s}", fuse=False, mirror=True)

# --- the tail: thick, lying back near the ground and curling up at its end, out to the +X side -------------------------
TAIL = [(0, 0.12, 0.38), (0.02, 0.3, 0.27), (0.07, 0.47, 0.19), (0.14, 0.6, 0.18), (0.2, 0.67, 0.26), (0.225, 0.69, 0.37)]
TAIL_R = [0.11, 0.088, 0.068, 0.05, 0.034, 0.016]
k.add("tail", tube(TAIL, TAIL_R), WHITE, "tail")
k.add("tailline", strip(TAIL, TAIL_R, (0, 0, 1), math.radians(-35), 0.048, 0.22, 0.92, lift=0.003), PURPLE, "tail",
      fuse=False)

# --- the eyes: red in a dark almond surround under the crest, slanting up toward the back ------------------------------
EYE_OUT = [(-1.0, -0.15), (-0.55, 0.3), (0.1, 0.62), (1.0, 0.95), (0.8, 0.25), (0.3, -0.45), (-0.4, -0.55)]
EYE_POLY = polygon(EYE_OUT)
def eye_outline(th):
    return EYE_POLY(th)
CR, CD, CK, CW, CL = srgb(EYERED), srgb(EYEDARK), srgb("#120A10"), srgb("#FFFFFF"), srgb(WHITE)
REACH = {}
def eye(u, v):
    th = np.arctan2(v, u)
    reach = np.array([eye_outline(t) for t in th])
    rho = np.sqrt(u * u + v * v) / reach
    c = np.tile(CD, (len(u), 1))
    c[rho < 0.68] = CR
    c[((u + 0.05) / 0.24) ** 2 + ((v - 0.02) / 0.34) ** 2 < 1] = CK
    c[((u + 0.18) / 0.1) ** 2 + ((v - 0.2) / 0.12) ** 2 < 1] = CW
    return c
de = unit(0.6, -0.74, 0.24)
EC = H + de / math.sqrt(((de / HR) ** 2).sum()) + np.array([0, -0.012, 0.0])
EH = (0.035 * HS, 0.024 * HS)
def eyes(state):
    look = eye_look(state, eye, CL, CD, line=0.16)
    k.decal(EC, de, EH, look, eps=0.005, rings=20, segs=56, outline=eye_outline)
    m = np.array([-1, 1, 1.0])
    k.decal(EC * m, de * m, EH, lambda u, v: look(-u, v), eps=0.005, rings=20, segs=56,
            outline=lambda th: eye_outline(math.pi - th))


# --- winding check: every loose piece's signed volume must be positive --------------------------------------------------
for p in k.parts:
    if p["fuse"] or p["carve"]:
        continue
    V = p["V"]; vol = 0.0
    for f in p["F"]:
        for i in range(1, len(f) - 1):
            vol += np.dot(V[f[0]], np.cross(V[f[i]], V[f[i + 1]]))
    if vol <= 0:
        print("KIT: !! inside out:", p["name"], round(vol / 6, 7))

k.build(voxel=VOXEL, smooth=4, band=0.004)
k.eye_states(eyes)
k.export(out)
