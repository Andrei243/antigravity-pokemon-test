# Vespiquen sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/vespiquen.py -- <the export's Vespiquen.glb> <out.glb>
#
# Grown from Combee's script: the same honey colours, the same hard-edged honeycomb cells made as loose prisms with a
# lighter face standing proud of each, the same pale wings as flat plates. From the references: a queen wasp floating
# over a great bell of a gown. A round golden head, orange round its sides and back, with a rounded black lobe standing
# out and up at each upper side and a tall red hexagonal gem down the forehead; a dark brown-orange mask over the face,
# cut straight over small slanting red eyes, two yellow mandible lines curving down from under the eyes to meet at the
# chin, a thin yellow ridge for a nose. Two pairs of pale wings out of the back behind the lobes, the upper rising.
# A short black waist. Thin black arms out to the sides, each a thin upper arm and a thick black forearm ending in an
# orange claw. The gown: a faceted dome as wide as the whole is tall, yellow at the waist, a black band, a yellow
# band, a second black band (shown at the sides and back), then the honeycomb: orange hexagonal cells round the lower
# front and all over the underside, a yellow cell at the bottom's middle.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, eye_look, polygon, args

src, out = args()
k = Kit(src, coat="shell")

GOLD, ORANGE, MASK, MAND, BLACK = "#F6B62C", "#E2802A", "#9A4A28", "#FAD04A", "#38383C"
CELLRIM, CELLFACE, CELLMID, GEM, GEMEDGE = "#C9681C", "#F0932A", "#F4C83C", "#E2423A", "#A8262A"
WING, WINGEDGE, CLAW = "#E4EAF2", "#B6C2D2", "#F08A2C"

H = np.array([0.0, -0.01, 0.80])           # the head's centre
HR = np.array([0.164, 0.152, 0.162])
E_ = 0.004                                  # a crisp paint's edge


def flatfaces(quads, tris=(), away=None):
    """Faces that share no points, so each shades flat (a cut stone, a facet); turned to face away from a point."""
    V, F = [], []
    for q in list(quads) + list(tris):
        q = [np.array(p, float) for p in q]
        if away is not None:
            nrm = np.cross(q[1] - q[0], q[2] - q[0])
            if np.dot(nrm, np.mean(q, axis=0) - np.array(away, float)) < 0:
                q = q[::-1]
        o = len(V)
        V.extend(q)
        F.append(list(range(o, o + len(q))))
    return np.array(V, float), F


# --- the gown: a faceted bell, its bands loose single-coloured rings of flat panels ---------------------------------
ZC, REQ, DEPTH, CUP, CLOW = 0.175, 0.385, 0.92, 0.445, 0.165    # equator height, radius, front-back share, semi-axes
NSIDE = 16


def gown_r(z):
    """The bell's radius at a height: an upper ellipse to the waist and a rounder lower one under the equator."""
    t = np.where(z >= ZC, (z - ZC) / CUP, (ZC - z) / CLOW)
    return REQ * np.sqrt(np.clip(1 - t * t, 0, 1))


def band(z0, z1, steps):
    """A ring of the bell between two heights (z0 above z1), its panels flat."""
    zs = np.linspace(z0, z1, steps + 1)
    a = np.radians(360 / NSIDE * np.arange(NSIDE + 1) + 360 / NSIDE / 2)
    quads, tris = [], []
    for i in range(steps):
        ra, rb = float(gown_r(zs[i])), float(gown_r(zs[i + 1]))
        for j in range(NSIDE):
            p = lambda r, z, an: (r * math.sin(an), -r * DEPTH * math.cos(an), z)
            q = [p(ra, zs[i], a[j]), p(ra, zs[i], a[j + 1]), p(rb, zs[i + 1], a[j + 1]), p(rb, zs[i + 1], a[j])]
            if ra < 1e-4:
                tris.append([q[0], q[2], q[3]])
            elif rb < 1e-4:
                tris.append([q[0], q[1], q[2]])
            else:
                quads.append(q)
    return flatfaces(quads, tris, away=(0, 0, ZC))


TOP = ZC + CUP                            # the waist's point
BANDS = [(TOP, 0.55, 3, GOLD), (0.55, 0.47, 1, BLACK), (0.47, 0.34, 2, GOLD), (0.34, 0.27, 2, BLACK),
         (0.27, ZC - CLOW, 8, GOLD)]
for i, (z0, z1, st, col) in enumerate(BANDS):
    k.add(f"gown{i}", band(z0, z1, st), col, "body", fuse=False, surface="shell")


# --- the honeycomb: hexagonal cells round the lower front and over the underside, as Combee's cells are made --------
def bell_point(theta, psi):
    """A point of the bell by its angle from the bottom pole (theta) and round the upright axis (psi, 0 at the front),
    and the bell's outward normal there."""
    cz = CLOW if theta <= math.pi / 2 else CUP
    a, b = REQ, REQ * DEPTH
    p = np.array([a * math.sin(theta) * math.sin(psi), -b * math.sin(theta) * math.cos(psi), ZC - cz * math.cos(theta)])
    n = np.array([p[0] / a ** 2, p[1] / b ** 2, (p[2] - ZC) / cz ** 2])
    return p, n / np.linalg.norm(n)


def hexcell(c, n, up, r, d_in, d_out):
    """A flat-topped hexagonal prism about c along its normal n, its flat top toward up: sharp-edged (no shared points)."""
    n = unit(*n)
    right = np.cross(up, n); right /= np.linalg.norm(right)
    upv = np.cross(n, right)
    ang = np.radians(60 * np.arange(6))
    ring = [right * math.cos(t) + upv * math.sin(t) for t in ang]
    f = [c + n * d_out + r * q for q in ring]
    b = [c - n * d_in + r * q for q in ring]
    quads = [[f[i], b[i], b[(i + 1) % 6], f[(i + 1) % 6]] for i in range(6)]
    tris = [[c + n * d_out, f[i], f[(i + 1) % 6]] for i in range(6)] + [[c - n * d_in, b[(i + 1) % 6], b[i]] for i in range(6)]
    # away from a point inside the prism: with d_in nought c lies in the back cap's own plane, and its faces were
    # then turned by rounding, some one way and some the other
    return flatfaces(quads, tris, away=c + n * (d_out - d_in) / 2)


RC =0.084                                  # a cell's corner radius
# the meridian's arc length down from the equator's top layer to the pole, to set the layers half a cell apart
TH = np.linspace(0, math.radians(103), 400)
PTS = np.array([bell_point(t, 0)[0] for t in TH])
ARC = np.concatenate([[0], np.cumsum(np.linalg.norm(np.diff(PTS, axis=0), axis=1))])
TOPARC = ARC[-1]
cell = 0


def put_cell(theta, psi, face=CELLFACE):
    global cell
    c, nrm = bell_point(theta, psi)
    # up along the meridian, away from the pole
    c2, _ = bell_point(theta + 0.05, psi)
    up = c2 - c; up -= nrm * np.dot(up, nrm); up /= np.linalg.norm(up)
    k.add(f"cell{cell}", hexcell(c, nrm, up, RC * 0.97, 0.03, 0.018), CELLRIM, "body", fuse=False, surface="shell")
    k.add(f"cface{cell}", hexcell(c, nrm, up, RC * 0.78, 0.0, 0.024), face, "body", fuse=False, surface="shell")
    cell += 1


# three staggered rings of the hem, all the way round so it is the same from every side
s = TOPARC
for layer in range(3):
    theta = float(np.interp(s, ARC, TH))
    p0, _ = bell_point(theta, 0)
    rho = math.hypot(p0[0], p0[1]) / 0.96
    nring = max(3, int(round(2 * math.pi * rho / (3 * RC))))   # a column's cells three corner radii apart
    step = 2 * math.pi / nring
    off = (layer % 2) * step / 2
    for j in range(nring):
        psi = off + j * step
        # the lowest ring's front cell is the yellow one the references show at the bottom's middle
        put_cell(theta, psi, CELLMID if (layer == 2 and j == 0) else CELLFACE)
    s -= math.sqrt(3) / 2 * RC
# the nearly flat underside: a flat honeycomb, a cell at the pole and six round it
put_cell(1e-3, 0.0, CELLMID)
thp = float(np.interp(math.sqrt(3) * RC * 0.98, ARC, TH))
for j in range(6):
    put_cell(thp, math.radians(60 * j))

# --- the waist ---------------------------------------------------------------------------------------------------
k.add("waist", tube([(0, -0.004, TOP - 0.05), (0, -0.007, 0.64), (0, -0.01, 0.69)], [0.045, 0.036, 0.05]), BLACK,
      "body", fuse=False)

# --- the arms: a thin black upper arm, a thick black forearm, an orange claw ---------------------------------------
for s_, side in ((1, "R"), (-1, "L")):
    m = np.array([s_, 1, 1.0])
    sh, el, wr = np.array([0.07, -0.02, 0.69]), np.array([0.205, -0.03, 0.655]), np.array([0.335, -0.045, 0.675])
    k.add(f"uparm{side}", tube([sh * m, ((sh + el) / 2) * m, el * m], [0.02, 0.016, 0.018]), BLACK, f"arm{side}",
          fuse=False)
    k.add(f"forearm{side}", tube([el * m, (el + (wr - el) * 0.4) * m, wr * m], [0.026, 0.04, 0.03]), BLACK,
          f"arm{side}", fuse=False)
    k.add(f"cuff{side}", ell(wr * m + np.array([s_ * 0.012, -0.002, -0.004]), (0.026, 0.024, 0.024)), CLAW,
          f"arm{side}", fuse=False)
    for i, (dz, dy) in enumerate(((0.012, -0.01), (-0.02, -0.004), (0.0, 0.016))):
        base = wr * m + np.array([s_ * 0.025, dy, dz])
        tip = base + np.array([s_ * 0.045, -0.015, dz * 0.6 - 0.02])
        mid = base * 0.5 + tip * 0.5 + np.array([s_ * 0.004, 0, 0.008])
        k.add(f"claw{side}{i}", blade([base, mid, tip], [0.014, 0.01, 0.002], (0, 1, 0), 0.6), CLAW, f"arm{side}",
              fuse=False)


# --- the wings: two pairs of pale flat plates, as Combee's ------------------------------------------------------------
def flat(outline, origin, ex, ez, thick):
    """A flat plate of a convex-ish outline (a, b along ex, ez about origin), fanned from its mean, sharp-edged."""
    ex, ez = unit(*ex), unit(*ez)
    nrm = np.cross(ex, ez); nrm /= np.linalg.norm(nrm)
    p = np.array(outline, float)
    W = np.array(origin, float) + p[:, :1] * ex + p[:, 1:] * ez
    mm = len(p)
    mid = W.mean(axis=0)
    front, back = W + nrm * thick / 2, W - nrm * thick / 2
    V = list(front) + list(back) + [mid + nrm * thick / 2, mid - nrm * thick / 2]
    F = []
    for i in range(mm):
        j = (i + 1) % mm
        F.append([2 * mm, i, j]); F.append([2 * mm + 1, mm + j, mm + i])
        F.append([i, mm + i, mm + j, j])
    return np.array(V), F


th = np.linspace(0, 2 * math.pi, 48, endpoint=False)
def wing_outline(L, Wd, inset=1.0):
    t = (np.cos(th) + 1) / 2
    w = np.sin(th) * Wd * (0.3 + 0.7 * np.sqrt(np.clip(t, 0, 1))) * 1.1
    return np.stack([0.02 + (t * L - 0.02) * inset + (1 - inset) * L * 0.08, w * inset], axis=1)

for s_, side in ((1, "R"), (-1, "L")):
    for name, root, ex, ez, L, Wd in (
            ("wingU", (0.105, 0.075, 0.885), (1.0, 0.45, 0.38), (-0.3, 0.2, 1.0), 0.27, 0.07),
            ("wingD", (0.115, 0.08, 0.845), (1.0, 0.42, -0.12), (0.0, 0.3, 1.0), 0.21, 0.052)):
        r = np.array(root) * (s_, 1, 1)
        exs, ezs = (ex[0] * s_, ex[1], ex[2]), (ez[0] * s_, ez[1], ez[2])
        k.add(f"{name}{side}", flat(wing_outline(L, Wd), r, exs, ezs, 0.005), WINGEDGE, f"wing{side}", fuse=False)
        k.add(f"{name}in{side}", flat(wing_outline(L, Wd, 0.86), r, exs, ezs, 0.008), WING, f"wing{side}",
              fuse=False)

# --- the head: round and golden, a black lobe out at each upper side -------------------------------------------------
k.add("head", ell(H, HR), GOLD, "head")
k.add("lobe", ell(H + np.array([0.126, 0.022, 0.114]), (0.05, 0.052, 0.074), rot=(0, 42, 0)), BLACK, "head",
      fuse=False, mirror=True)

# the red gem down the forehead: a tall hexagon lying on the head, its edges bevelled up to a flat top
def on_head(s, t, lift):
    a, b = s / HR[0], t / HR[2]
    d = np.array([math.sin(a) * math.cos(b), -math.cos(a) * math.cos(b), math.sin(b)])
    return H + d * HR * 1.0 + d * lift
GEMO = [(0.0, 0.222), (0.027, 0.196), (0.027, 0.128), (0.0, 0.1), (-0.027, 0.128), (-0.027, 0.196)]
def gem():
    mid_s, mid_t = 0.0, np.mean([p[1] for p in GEMO])
    outer = [on_head(s, t, -0.004) for s, t in GEMO]
    lip = [on_head(s, t, 0.006) for s, t in GEMO]
    inner = [on_head(mid_s + (s - mid_s) * 0.5, mid_t + (t - mid_t) * 0.74, 0.024) for s, t in GEMO]
    centre = on_head(mid_s, mid_t, 0.026)
    quads = [[lip[i], lip[(i + 1) % 6], inner[(i + 1) % 6], inner[i]] for i in range(6)]
    sides = [[outer[i], outer[(i + 1) % 6], lip[(i + 1) % 6], lip[i]] for i in range(6)]
    tris = [[centre, inner[i], inner[(i + 1) % 6]] for i in range(6)]
    below = on_head(mid_s, mid_t, -0.06)
    return flatfaces(quads, tris, away=below), flatfaces(sides, away=below)
gtop, gside = gem()
k.add("gem", gtop, GEM, "head", fuse=False, surface="shell")
k.add("gemside", gside, GEMEDGE, "head", fuse=False, surface="shell")


# --- paint: orange round the sides and back, then the face's mask, the mandibles and the nose ----------------------
def head_uv(P):
    """Coordinates round the head's upright axis: (angle from the front times the radius, height over the centre)."""
    q = P - H
    return np.arctan2(q[:, 0], -q[:, 1]) * HR[0], q[:, 2], q


def rim(P):
    q = (P - H) / HR
    nq = np.linalg.norm(q, axis=1) + 1e-9
    front = -q[:, 1] / nq
    return smoothstep(0.45, 0.1, front + 0.35 * np.clip(q[:, 2] / nq, 0, 1)) * smoothstep(0.25, -0.15, q[:, 2] / nq) * (nq < 1.25)
k.paint(rim, ORANGE)


def poly_sd(pts, X, Y):
    """Signed distance to a polygon (negative inside)."""
    pts = np.array(pts, float)
    d = np.full(len(X), 9.0)
    sign = np.ones(len(X))
    for a, b in zip(pts, np.roll(pts, -1, axis=0)):
        e = b - a
        w = np.stack([X - a[0], Y - a[1]], 1)
        t = np.clip((w @ e) / (e @ e), 0, 1)
        d = np.minimum(d, np.linalg.norm(w - t[:, None] * e, axis=1))
        c1 = (Y >= a[1]) & (Y < b[1]) & (e[0] * (Y - a[1]) - e[1] * (X - a[0]) > 0)
        c2 = (Y < a[1]) & (Y >= b[1]) & (e[0] * (Y - a[1]) - e[1] * (X - a[0]) < 0)
        sign[c1 | c2] *= -1
    return d * sign


MASKPOLY = [(0.0, 0.088), (0.03, 0.086), (0.075, 0.094), (0.11, 0.088), (0.126, 0.03), (0.114, -0.04),
            (0.07, -0.09), (0.0, -0.118), (-0.07, -0.09), (-0.114, -0.04), (-0.126, 0.03), (-0.11, 0.088),
            (-0.075, 0.094), (-0.03, 0.086)]
def mask(P):
    u, v, q = head_uv(P)
    return inside(poly_sd(MASKPOLY, u, v), E_) * (q[:, 1] < 0.02) * (np.linalg.norm(q / HR, axis=1) < 1.2)
k.paint(mask, MASK)


def curve_d(pts, X, Y):
    pts = np.array(pts, float)
    d = np.full(len(X), 9.0)
    for a, b in zip(pts[:-1], pts[1:]):
        e = b - a
        w = np.stack([X - a[0], Y - a[1]], 1)
        t = np.clip((w @ e) / (e @ e), 0, 1)
        d = np.minimum(d, np.linalg.norm(w - t[:, None] * e, axis=1))
    return d
MANDS = []
for sd in (1, -1):
    tt = np.linspace(0, 1, 12)
    # from the outer cheek under the eye, down and in to meet at the chin
    MANDS.append(np.stack([sd * (0.1 * (1 - tt) ** 1.1), -0.034 - 0.084 * tt ** 0.9], 1))
def mandibles(P):
    u, v, q = head_uv(P)
    d = np.minimum(curve_d(MANDS[0], u, v), curve_d(MANDS[1], u, v))
    w = 0.0085 - 0.003 * np.clip(-v / 0.12, 0, 1)
    return inside(d - w, E_) * (q[:, 1] < 0.0) * (np.linalg.norm(q / HR, axis=1) < 1.2)
k.paint(mandibles, MAND)
def nose(P):
    u, v, q = head_uv(P)
    w = 0.0055 * np.clip(1 - (-0.012 - v) / 0.05, 0.3, 1)
    return inside(np.abs(u) - w, E_) * inside(v + 0.012, E_) * inside(-0.058 - v, E_) * (q[:, 1] < 0.0)
k.paint(nose, MAND)

# --- the eyes: small red almonds slanting down to the nose under the mask's straight edge ------------------------
RED, GLOW, DARK, WHITE = srgb("#EE3A34"), srgb("#FF8C78"), srgb("#3A120E"), np.array([1.0, 1.0, 1.0])
SURROUND = "#3E180E"                       # the dark round each eye, which a closed eye shows as its lid
LID, INKL = srgb(SURROUND), srgb(MAND)
def top(u):
    return 0.42 + 0.42 * u
def bottom(u):
    return top(u) - 1.15 * (1 - u * u) ** 0.7 - 0.12
ALMOND = [(u, top(u)) for u in np.linspace(1, -1, 9)] + [(u, bottom(u)) for u in np.linspace(-0.94, 0.94, 13)]
def eye_open(u, v):
    c = np.tile(RED, (len(u), 1))
    c[v > top(u) - 0.14] = DARK
    c[((u + 0.05) / 0.12) ** 2 + ((v + 0.1) / 0.4) ** 2 < 1] = DARK
    c[((u + 0.3) / 0.2) ** 2 + ((v - 0.12) / 0.2) ** 2 < 1] = WHITE
    return c
HE = 0.036
EU, EV = 0.062, 0.028
def eye_frame(side):
    a = side * EU / HR[0]
    d = np.array([math.sin(a), -math.cos(a), 0.0])
    return H + d * HR + np.array([0, 0, EV]), unit(math.sin(a) * 1.05, -math.cos(a), 0.12)
# a dark surround round each eye, part of the body so it shows in every state
for side in (1, -1):
    c, n = eye_frame(side)
    k.decal(c, n, (HE * 1.32, HE * 1.32), srgb(SURROUND), eps=0.0035, rings=12, segs=48,
            outline=polygon([(side * u, v) for u, v in (ALMOND if side == 1 else ALMOND[::-1])]))
def eyes(state):
    for side in (1, -1):
        c, n = eye_frame(side)
        flip = (lambda f: f) if side == 1 else (lambda f: (lambda u, v: f(-u, v)))
        outline = polygon([(side * u, v) for u, v in (ALMOND if side == 1 else ALMOND[::-1])])
        k.decal(c, n, (HE, HE), flip(eye_look(state, eye_open, LID, INKL, line=0.17)), eps=0.0055, outline=outline,
                rings=20, segs=56)

k.build(voxel=0.0045, smooth=4)
k.eye_states(eyes)
k.export(out)
