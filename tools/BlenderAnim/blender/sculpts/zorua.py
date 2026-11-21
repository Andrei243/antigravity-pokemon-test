# Zorua sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/zorua.py -- <the export's Zorua.glb> <out.glb>
# A small dark grey fox, mostly head: a short pointed muzzle with a black nose and a sly smirk, big pointed ears lined
# dark navy, spiky tufts out from the cheeks, a red flame of a tuft swept up and back over the crown, red marks over the
# top of each big cyan eye and two red spots on the brow; a black ruff round the neck; short legs with red tips; a
# bushy tail up behind.
#
# Written to be grown from (zoroark.py keeps its colours and the way of its eyes, red marks, ears, nose and mouth).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, tri, eye_look, polygon, lumpy, args

# --- colours ----------------------------------------------------------------------------------------------------------
GREY = "#4E4C62"            # the coat, a cool dark grey
RUFF = "#22223A"            # the ruff and the ears' lining: near black, navy
RED = "#C42838"
CYAN, PUPIL = "#2EB8D2", "#0E2630"
EYE_W, INK = "#F4F6F8", "#141420"
NOSE = "#0E0E14"

# --- the frame (the skeleton's joints: body (0, 0.01, 0.13), head (0, -0.06, 0.2), ears (+-0.04, -0.07, 0.31),
#     forelegs (+-0.035, -0.05, 0.1), hind legs (+-0.035, 0.07, 0.1), tail (0, 0.09, 0.15)) ---------------------------
H = np.array([0, -0.086, 0.258])
HR = np.array([0.096, 0.083, 0.079])
LEG_X = 0.036
FORE_Y, HIND_Y = -0.052, 0.07


def on_head(x, z, lift=0.0, h=None, hr=None):
    h = H if h is None else h
    hr = HR if hr is None else hr
    zn = (z - h[2]) / hr[2]
    q = max(0.0, 1 - zn * zn - (x / hr[0]) ** 2)
    p = np.array([x, h[1] - hr[1] * math.sqrt(q), z])
    n = (p - h) / hr ** 2; n /= np.linalg.norm(n)
    return p + n * lift, n


def along(P, pts, n=200):
    pts = np.array(pts, float)
    seg = np.linalg.norm(np.diff(pts, axis=0), axis=1)
    cum = np.concatenate([[0], np.cumsum(seg)]); L = cum[-1]
    s = np.linspace(0, L, n)
    line = np.stack([np.interp(s, cum, pts[:, i]) for i in range(3)], axis=1)
    Q = P.copy(); Q[:, 0] = np.abs(Q[:, 0])
    j = np.concatenate([((Q[a:a + 4000, None, :] - line[None]) ** 2).sum(2).argmin(1) for a in range(0, len(Q), 4000)])
    return s[j] / L, Q - line[j]


# --- the head: round, widest at the cheeks, a short pointed muzzle -----------------------------------------------------
def cheeky(V):
    V = V.copy()
    low = np.clip(-V[:, 2], 0, 1)
    V[:, 0] *= 1 + 0.08 * (1 - V[:, 2] ** 2) - 0.1 * low ** 2
    return V


def taper(V):
    """A muzzle narrowing to its tip at -Y."""
    V = V.copy()
    t = np.clip(-V[:, 1], 0, 1)
    V[:, 0] *= 1 - 0.45 * t
    V[:, 2] *= 1 - 0.3 * t
    return V


def add_head(k):
    k.add("head", ell(H, HR, deform=cheeky), GREY, "head")
    k.add("muzzle", ell(H + (0, -0.072, -0.03), (0.036, 0.046, 0.03), deform=taper), GREY, "head")
    # Points of fur out from each cheek: three, the lowest down
    for i, (z, out, down, r) in enumerate(((0.05, 0.05, 0.008, 0.017), (-0.2, 0.054, -0.01, 0.017),
                                           (-0.45, 0.04, -0.028, 0.014))):
        b = H + np.array([HR[0] * 0.86, -HR[1] * 0.05, HR[2] * z])
        t = b + np.array([out, 0.014, down])
        k.add(f"tuft{i}", blade([b, b + (t - b) * 0.5, t], [r, r * 0.62, 0.002], (0, 1, 0.2), 0.5), GREY, "head",
              mirror=True)


# --- the ears: big and pointed, up and out from the top corners, lined dark --------------------------------------------
EAR = [(0.046, -0.084, 0.31), (0.064, -0.082, 0.344), (0.082, -0.076, 0.376), (0.098, -0.068, 0.405)]
EAR_R = [0.038, 0.033, 0.019, 0.003]


def add_ears(k, ear=EAR, radii=EAR_R, color=GREY, lining=RUFF, width=0.024, flat=0.32):
    k.add("ear", blade(ear, radii, (0, 1, 0), flat), color, "ear{s}", mirror=True)
    d = unit(*(np.array(ear[-1]) - np.array(ear[0])))
    across = np.cross(d, (0, 1.0, 0)); across /= np.linalg.norm(across)

    def lin(P):
        t, off = along(P, ear)
        half = width * np.sin(np.pi * np.clip((t + 0.12) / 1.0, 0, 1)) ** 0.7
        return (inside(np.abs(off @ across) - half, 0.002) * inside(off[:, 1] + 0.001, 0.0015)
                * inside(t - 0.84, 0.01) * (t > 0.06))
    k.paint(lin, lining, on=["ear"])


# --- the red flame on the crown: three tongues swept up and back, the middle one tallest --------------------------------
def add_crest(k):
    top = H + np.array([0, 0.008, HR[2] * 0.86])
    for i, (x, pts, r) in enumerate((
            (0.0, [(0, -0.03, -0.01), (0, -0.012, 0.035), (0, 0.025, 0.072), (0, 0.075, 0.092)], [0.042, 0.034, 0.02, 0.003]),
            (0.016, [(0, -0.02, -0.012), (0.008, 0.0, 0.026), (0.016, 0.035, 0.05), (0.022, 0.07, 0.056)],
             [0.028, 0.023, 0.013, 0.002]),
            (-0.016, [(0, -0.01, -0.012), (-0.006, 0.02, 0.022), (-0.012, 0.055, 0.04), (-0.018, 0.085, 0.042)],
             [0.026, 0.021, 0.012, 0.002]))):
        P = [top + np.array([x, 0, 0]) + np.array(q) for q in pts]
        k.add(f"crest{i}", blade(P, r, (1, 0, 0), 0.72), RED, "head")


# --- the ruff: a black collar round the neck and over the chest, its lower edge in points -------------------------------
def add_ruff(k):
    k.add("ruff", ell((0, -0.045, 0.165), (0.054, 0.048, 0.04), deform=lumpy(6, 2, 0.12)), RUFF, "body")
    for i, a in enumerate((-60, -30, 0, 30, 60)):
        t = math.radians(a)
        b = np.array([0.04 * math.sin(t), -0.045 - 0.04 * math.cos(t), 0.15])
        tip = b + np.array([0.016 * math.sin(t), -0.014 * math.cos(t), -0.035])
        k.add(f"ruffpt{i}", blade([b, (b + tip) / 2, tip], [0.018, 0.012, 0.002], (math.cos(t), -math.sin(t) * 0, 0), 0.5),
              RUFF, "body")


def add_body(k):
    k.add("chest", ell((0, -0.03, 0.13), (0.044, 0.05, 0.05)), GREY, "body")
    k.add("rump", ell((0, 0.055, 0.125), (0.046, 0.055, 0.048)), GREY, "body")


def add_legs(k):
    # Short legs, fused shoulders and thighs; red tips
    k.add("shoulder", ell((LEG_X - 0.004, FORE_Y, 0.1), (0.026, 0.03, 0.04)), GREY, "leg{s}", mirror=True)
    k.add("leg", tube([(LEG_X, FORE_Y, 0.095), (LEG_X + 0.002, FORE_Y - 0.004, 0.05), (LEG_X + 0.003, FORE_Y - 0.008, 0.016)],
                      [0.019, 0.017, 0.016]), GREY, "leg{s}", mirror=True)
    k.add("paw", ell((LEG_X + 0.003, FORE_Y - 0.016, 0.012), (0.018, 0.022, 0.012)), RED, "leg{s}", mirror=True)
    k.add("thigh", ell((LEG_X, HIND_Y + 0.004, 0.095), (0.03, 0.04, 0.045)), GREY, "hind{s}", mirror=True)
    k.add("hind", tube([(LEG_X + 0.002, HIND_Y + 0.01, 0.08), (LEG_X + 0.003, HIND_Y + 0.014, 0.045),
                        (LEG_X + 0.003, HIND_Y + 0.004, 0.016)], [0.019, 0.017, 0.016]), GREY, "hind{s}", mirror=True)
    k.add("hpaw", ell((LEG_X + 0.003, HIND_Y - 0.006, 0.012), (0.018, 0.022, 0.012)), RED, "hind{s}", mirror=True)
    k.paint(lambda P: inside(P[:, 2] - 0.028, 0.0018), RED, on=["leg", "hind", "paw", "hpaw"])
    k.paint(lambda P: inside(0.028 - P[:, 2], 0.0018), GREY, on=["leg", "hind"])


def add_tail(k):
    # A bushy tail up and back from the rump, its end in points
    b = np.array([0, 0.09, 0.14])
    pts = [b, b + (0, 0.04, 0.03), b + (0.012, 0.07, 0.08), b + (0.02, 0.08, 0.13)]
    k.add("tail", tube(pts, [0.024, 0.034, 0.034, 0.022]), GREY, "tail")
    for i, (d, r) in enumerate((((0.0, 0.03, 0.06), 0.02), ((0.025, 0.03, 0.045), 0.017), ((-0.022, 0.035, 0.04), 0.016))):
        s = pts[-1]
        k.add(f"tailpt{i}", blade([s, s + np.array(d) * 0.5, s + np.array(d)], [r, r * 0.6, 0.002], (1, 0, 0), 0.6),
              GREY, "tail")


# --- the face: red marks, the nose, the smirk -------------------------------------------------------------------------
def add_marks(k):
    for side in (1, -1):
        # two red spots on the brow, above the inner end of each eye's mark
        p, n = on_head(0.022 * side, H[2] + 0.04)
        k.decal(p, n, (0.009, 0.009), RED, eps=0.003, rings=6, segs=28)


def add_nose_mouth(k):
    tip = H + np.array([0, -0.122, -0.022])
    k.decal(tip, unit(0, -1, 0.35), (0.012, 0.009), NOSE, eps=0.003,
            outline=polygon([(-1, 0.6), (1, 0.6), (0.3, -0.9), (-0.3, -0.9)]), rings=6, segs=28)
    CG, CI = srgb(GREY), srgb(INK)
    def smirk(u, v):
        c = np.tile(CG, (len(u), 1))
        c[(np.abs(v - (-0.1 + 0.55 * u + 0.3 * u * u)) < 0.18) & (u > -0.75) & (u < 0.9)] = CI
        return c
    k.decal(H + np.array([0.006, -0.108, -0.05]), unit(0.2, -1, -0.3), (0.018, 0.01), smirk, eps=0.003, rings=10, segs=40)


# --- the eyes: big and slanting up to the outer corner, a cyan iris with a dark pupil and a glint; a heavy dark lid
#     along the top under a red mark that sweeps up and out ----------------------------------------------------------
OUTLINE = [(-1, -0.1), (-0.55, 0.3), (0.2, 0.55), (1, 0.62), (0.95, 0.0), (0.5, -0.6), (-0.2, -0.75), (-0.8, -0.5)]
MARK = [(-1.0, -0.6), (-0.4, 0.2), (0.4, 0.6), (1.0, 0.95), (1.0, 0.15), (0.3, -0.35), (-0.5, -0.85)]


def eye_colour(iris=CYAN):
    CW, CC, CP, CI = srgb(EYE_W), srgb(iris), srgb(PUPIL), srgb(INK)
    rim = polygon(OUTLINE)
    def eye(u, v):
        c = np.tile(CW, (len(u), 1))
        c[np.hypot((u + 0.05) / 0.55, (v + 0.06) / 0.66) < 1] = CC
        c[np.hypot((u + 0.05) / 0.24, (v + 0.06) / 0.4) < 1] = CP
        c[np.hypot((u - 0.15) / 0.13, (v - 0.1) / 0.15) < 1] = (1, 1, 1)
        rr = np.hypot(u, v) / np.array([rim(t) for t in np.arctan2(v, u)])
        c[(rr > 0.7) & (v > 0.05 + 0.3 * u)] = CI
        c[rr > 0.86] = CI
        return c
    return eye


def lay_eyes(k, state, h=None, hr=None, x=0.04, dz=-0.006, half=(0.026, 0.021), lid=GREY, red=RED, iris=CYAN,
             turn=1.2):
    h = H if h is None else h
    hr = HR if hr is None else hr
    p, n = on_head(x, h[2] + dz, 0.0, h, hr)
    n = unit(n[0] * turn, n[1], n[2])
    look = eye_look(state, eye_colour(iris), srgb(lid), srgb(INK), line=0.16)
    q = np.array([-1, 1, 1.0])
    pm, nm = on_head(x + half[0] * 0.15, h[2] + dz + half[1] * 0.75, 0.0, h, hr)
    nm = unit(nm[0] * turn, nm[1], nm[2])
    for s in (1, -1):
        m = q if s < 0 else np.ones(3)
        # the red mark over the eye, sweeping up to the outer corner: in every state, laid under the eye's own patch
        k.decal(pm * m, nm * m, (half[0] * 1.15, half[1] * 0.95), red, eps=0.0025,
                outline=polygon([(a * s, b) for a, b in MARK]), rings=10, segs=48)
        k.decal(p * m, n * m, half, look if s > 0 else (lambda u, v: look(-u, v)), eps=0.0042,
                outline=polygon([(a * s, b) for a, b in OUTLINE]), rings=20, segs=56)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="fur")
    add_body(k)
    add_legs(k)
    add_head(k)
    add_ears(k)
    add_crest(k)
    add_ruff(k)
    add_tail(k)
    add_marks(k)
    add_nose_mouth(k)
    k.build(voxel=0.0036, band=0.004)
    k.eye_states(lambda s: lay_eyes(k, s, x=0.045, half=(0.029, 0.023)))
    k.export(out)
