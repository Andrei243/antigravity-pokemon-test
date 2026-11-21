# Zorua-Hisui sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown
# from Zorua's script (zorua.py, imported: its frame, head, muzzle, cheek tufts, ears and their lining, body, legs, tail
# and its way of laying an eye on the head):
#   blender --background --factory-startup --python sculpts/zoruahisui.py -- <the export's Zorua-Hisui.glb> <out.glb>
# From the references: Zorua's build in a pale grey coat; a great white puff over the back of the crown, from which a
# thick wisp of fur rises, curls back and turns red at its end; ears lined dark violet; three round red marks in a row
# on the brow; sleepy amber eyes under a heavy lid, a blush of pink under them; a black nose; spiky white tufts out
# from the cheeks; a big white ruff round the neck, pink at its sides; short grey legs with dark violet tips; a bushy
# white tail behind, from which three red tendrils curl out to the back and the side.
#
# Written to be grown from (zoroarkhisui.py keeps its colours, its wisps and its way of the eyes).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, eye_look, polygon, lumpy, args, sibling

z_ = sibling("zorua")
H, HR = z_.H, z_.HR

# --- colours ----------------------------------------------------------------------------------------------------------
COAT = "#C9CED6"           # the coat, a pale cool grey
WHITE = "#F6F6F8"           # the puff, the ruff, the tail
WISP = "#E2485A"            # the red of the wisps' ends and the brow's marks
PINK = "#EFA2AC"
LINING = "#4C3C5E"          # the ears' lining: dark violet
PAW = "#4A3E52"
AMBER, PUPIL = "#F2A41C", "#3A1E08"
EYE_W, INK = "#FBF6EA", "#2A1E2A"


# --- a wisp: a tube that waves along a line, white at its root and red toward its end -------------------------------
def wavy(a, b, amp, waves, side, n=7):
    """Points from a to b with a sine of amp across side: a tendril's line."""
    a, b = np.array(a, float), np.array(b, float)
    s = np.array(side, float); s /= np.linalg.norm(s)
    return [a + (b - a) * t + s * amp * math.sin(math.pi * waves * t) * min(1.0, 3 * t) for t in np.linspace(0, 1, n)]


def param(P, pts, n=240):
    """For points P: the share of the way along a line (0..1) of the nearest point on it, and the distance to it."""
    pts = np.array(pts, float)
    seg = np.linalg.norm(np.diff(pts, axis=0), axis=1)
    cum = np.concatenate([[0], np.cumsum(seg)]); L = cum[-1]
    s = np.linspace(0, L, n)
    line = np.stack([np.interp(s, cum, pts[:, i]) for i in range(3)], axis=1)
    t, d = np.empty(len(P)), np.empty(len(P))
    for a in range(0, len(P), 4000):
        D = ((P[a:a + 4000, None, :] - line[None]) ** 2).sum(2)
        j = D.argmin(1)
        t[a:a + 4000] = s[j] / L
        d[a:a + 4000] = np.sqrt(D[np.arange(len(j)), j])
    return t, d


def wisp(k, name, pts, radii, bone, start=0.45, end=0.8, root=WHITE, red=WISP, reach=0.03):
    """A tendril fused into what it grows from, its colour going over from root to red between start and end."""
    k.add(name, tube(pts, radii), root, bone)
    def m(P):
        t, d = param(P, pts)
        return smoothstep(start, end, t) * inside(d - reach, 0.004)
    k.paint(m, red, on=[name])


# --- the puff over the crown and its great wisp, rising, curling back and red at its end ----------------------------
PUFF = H + np.array([0, 0.04, 0.072])
PUFF_R = np.array([0.096, 0.088, 0.06])
# laid back over the nape and curling, so it does not stand as a horn and make the body small in the game
CROWN_WISP = [PUFF + (0, 0.06, 0.02), PUFF + (0.0, 0.1, 0.06), PUFF + (0.01, 0.115, 0.1),
              PUFF + (0.03, 0.1, 0.125), PUFF + (0.048, 0.07, 0.122), PUFF + (0.055, 0.055, 0.104)]
CROWN_R = [0.034, 0.026, 0.019, 0.014, 0.011, 0.008]


def add_puff(k):
    k.add("puff", ell(PUFF, PUFF_R, rot=(-12, 0, 0), deform=lumpy(5, 2, 0.06)), WHITE, "head")
    k.add("puffback", ell(PUFF + (0, 0.045, 0.0), (0.06, 0.05, 0.042)), WHITE, "head")
    wisp(k, "crownwisp", CROWN_WISP, CROWN_R, "head", start=0.42, end=0.78)


# --- the ruff: a great white roll round the neck under the jaw, pink at its sides -----------------------------------
def add_ruff(k):
    k.add("ruff", ell((0, -0.05, 0.17), (0.07, 0.056, 0.042), deform=lumpy(6, 2, 0.08)), WHITE, "body")
    for s in (1, -1):
        k.add(f"ruffside{'R' if s > 0 else 'L'}", ell((0.06 * s, -0.035, 0.19), (0.032, 0.04, 0.036)), WHITE, "body")
    k.paint(lambda P: smoothstep(0.05, 0.08, np.abs(P[:, 0])) * inside(P[:, 2] - 0.205, 0.008)
            * inside(0.15 - P[:, 2], 0.008) * inside(P[:, 1] + 0.02, 0.008) * 0.7, PINK, on=["ruff"])


# --- the tail: Zorua's, white, and three red tendrils curling out of its end -----------------------------------------
TENDRILS = [((0.02, 0.17, 0.27), (0.13, 0.24, 0.28), (0, 0, 1.0)),
            ((0.03, 0.17, 0.24), (0.12, 0.25, 0.19), (0, 0, 1.0)),
            ((0.0, 0.17, 0.26), (0.03, 0.28, 0.31), (1.0, 0, 0))]


def add_tail(k):
    z_.add_tail(k)                     # in the coat's colour set below
    # bushier than Zorua's: a mass of white fur over its upper half
    k.add("tailfluff", ell((0.008, 0.15, 0.225), (0.038, 0.042, 0.05), rot=(-30, 0, 0), deform=lumpy(5, 2, 0.08)),
          WHITE, "tail")
    for i, (a, b, side) in enumerate(TENDRILS):
        pts = wavy(a, b, 0.016, 2.0, side)
        wisp(k, f"tendril{i}", pts, [0.016, 0.014, 0.012, 0.011, 0.01, 0.009, 0.007], "tail", start=0.12, end=0.42)


# --- the face: three round red marks on the brow, pink under the eyes, the nose and the smile ----------------------
def add_marks(k):
    for x, z, r in ((0.0, 0.054, 0.0125), (0.043, 0.036, 0.0105), (-0.043, 0.036, 0.0105)):
        p, n = z_.on_head(x, H[2] + z)
        k.decal(p, n, (r, r), WISP, eps=0.0026, rings=8, segs=36)
    for s in (1, -1):
        p, n = z_.on_head(0.062 * s, H[2] - 0.03)
        k.decal(p, n, (0.016, 0.009), PINK, eps=0.0022, rings=8, segs=36)


def add_nose_mouth(k):
    tip = H + np.array([0, -0.122, -0.022])
    k.decal(tip, unit(0, -1, 0.35), (0.011, 0.0085), "#16121A", eps=0.003,
            outline=polygon([(-1, 0.6), (1, 0.6), (0.3, -0.9), (-0.3, -0.9)]), rings=6, segs=28)
    CG, CI = srgb(COAT), srgb(INK)
    def smile(u, v):
        c = np.tile(CG, (len(u), 1))
        c[(np.abs(v - (-0.1 + 0.45 * u * u)) < 0.2) & (np.abs(u) < 0.85)] = CI
        return c
    k.decal(H + np.array([0.0, -0.108, -0.05]), unit(0, -1, -0.3), (0.016, 0.009), smile, eps=0.003, rings=10, segs=40)


# --- the eyes: round, amber, half shut under a heavy lid of the coat's colour --------------------------------------
OUTLINE = [(-1, -0.05), (-0.65, 0.45), (0.0, 0.68), (0.75, 0.6), (1, 0.05), (0.62, -0.6), (-0.05, -0.78), (-0.72, -0.52)]


def eye_colour(iris=AMBER, lid=COAT, lidline=0.18):
    """An amber eye whose upper part is under the lid: the lid's edge a dark line sloping a little to the outer side."""
    CW, CA, CP, CI, CL = srgb(EYE_W), srgb(iris), srgb(PUPIL), srgb(INK), srgb(lid)
    CO = srgb("#C8680E")
    rim = polygon(OUTLINE)
    def eye(u, v):
        c = np.tile(CW, (len(u), 1))
        ir = np.hypot((u + 0.02) / 0.6, (v + 0.12) / 0.7)
        c[ir < 1] = CA
        c[(ir < 1) & (v < -0.45)] = CO
        c[np.hypot((u + 0.02) / 0.26, (v + 0.12) / 0.4) < 1] = CP
        c[np.hypot((u - 0.18) / 0.12, (v + 0.02) / 0.12) < 1] = (1, 1, 1)
        rr = np.hypot(u, v) / np.array([rim(t) for t in np.arctan2(v, u)])
        edge = lidline - 0.08 * u
        c[v > edge + 0.16] = CL
        c[np.abs(v - edge - 0.08) < 0.1] = CI
        c[rr > 0.86] = CI
        return c
    return eye


def lay_eyes(k, state, h=None, hr=None, x=0.045, dz=-0.008, half=(0.029, 0.024), lid=COAT, iris=AMBER, turn=1.2):
    h = H if h is None else h
    hr = HR if hr is None else hr
    p, n = z_.on_head(x, h[2] + dz, 0.0, h, hr)
    n = unit(n[0] * turn, n[1], n[2])
    look = eye_look(state, eye_colour(iris, lid), srgb(lid), srgb(INK), line=0.16)
    q = np.array([-1, 1, 1.0])
    for s in (1, -1):
        m = q if s < 0 else np.ones(3)
        k.decal(p * m, n * m, half, look if s > 0 else (lambda u, v: look(-u, v)), eps=0.0042,
                outline=polygon([(a * s, b) for a, b in OUTLINE]), rings=20, segs=56)


def zorua_hisui(src, out):
    k = Kit(src, coat="fur")
    z_.GREY, z_.RED = COAT, PAW          # Zorua's body, legs, head and tail in this form's coat; its paws violet
    z_.add_body(k)
    z_.add_legs(k)
    z_.add_head(k)
    z_.add_ears(k, color=COAT, lining=LINING)
    add_puff(k)
    add_ruff(k)
    z_.GREY = WHITE
    add_tail(k)
    z_.GREY = COAT
    add_marks(k)
    add_nose_mouth(k)
    k.build(voxel=0.0036, band=0.004)
    k.eye_states(lambda s: lay_eyes(k, s))
    k.export(out)


if __name__ == "__main__":
    zorua_hisui(*args())
