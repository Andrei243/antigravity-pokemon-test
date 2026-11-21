# Sandy Shocks sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/sandyshocks.py -- <the export's Sandy Shocks.glb> <out.glb>
# A paradox of Magneton's line: magnemite.py's cross-headed screws are borrowed (sibling("magnemite")); the rest is
# built anew, since its magnets are round bars and legs here.
#
# From the references: three pale blue-grey balls, each with one big yellow eye ringed black and a small black pupil:
# one on top, two below it side by side. The top one has a violet mark like a brow over its eye and a row of dark
# dots under it, a horseshoe magnet out to its -X side and another rising up and back on its +X side. Each lower ball
# stands on a long horseshoe magnet as a leg, bent over at the top under the ball: its inner end goes down to the
# ground, its outer end splays out to the side. The magnets are round grey bars banded red or blue near each end, and
# every end bursts into a clump of black iron filings. Cross-headed screws stand on the balls' sides. (The task's text
# expected sand-grey bodies with brows and a spiked mane: the pictures show pale blue-grey balls, a violet mark and
# filings at the magnets' ends only, and are followed.)
# Bones: body (0, 0, 0.4), head (0, 0, 0.48), leg{s} (0.11, 0, 0.3), tail (0, 0.03, 0.37).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
import sculptkit
from sculptkit import Kit, ell, tube, hull, ring_band, srgb, unit, eye_look, args, sibling, polygon

mg = sibling("magnemite")

BALL, BAR, RED, BLUE, FILINGS = "#AFC6D2", "#A6AAB1", "#D8424A", "#3F8FD8", "#29282D"
YELLOW, INK, PUPIL, VIOLET, DOT = "#F3DC3A", "#1E2026", "#121316", "#8B5FB8", "#2C3038"

TOP = np.array([0.0, 0.012, 0.585])
LOW = np.array([0.116, -0.004, 0.41])
R = 0.104
BAR_R = 0.023
VOXEL = 0.0048


def ball(k, name, bone, c, r, look):
    c = np.array(c, float)
    k.add(name, ell(c, (r, r, r)), BALL, bone)
    k.add(name + "face", ell(c + np.array(look) * 0.5 * r, (0.68 * r, 0.68 * r, 0.68 * r)), BALL, bone)


def filings(k, name, bone, c, d, n=11, L=0.1, seed=0, flat=False):
    """A clump of black iron filings: thick pointed spikes bursting from c about d."""
    rs = np.random.RandomState(seed)
    d = unit(*d)
    a, b = sculptkit.axes(d)
    c = np.array(c, float)
    for i in range(n):
        ang = 2 * math.pi * i / n + rs.rand() * 0.5
        tilt = 0.55 + 0.5 * rs.rand() if not flat else 1.15 + 0.25 * rs.rand()
        dirn = d * math.cos(tilt) + (a * math.cos(ang) + b * math.sin(ang)) * math.sin(tilt)
        if flat:
            dirn[2] = max(dirn[2], -0.15)
            dirn /= np.linalg.norm(dirn)
        e1, e2 = sculptkit.axes(dirn)
        w = 0.022
        l = L * (0.75 + 0.5 * rs.rand())
        pts = [c + e1 * w, c - e1 * w * 0.5 + e2 * w * 0.87, c - e1 * w * 0.5 - e2 * w * 0.87, c - dirn * 0.01,
               c + dirn * l]
        k.add(f"{name}{i}", hull(pts), FILINGS, bone, fuse=False, surface="metal")


def band(k, name, bone, at, axis, col):
    k.add(name, ring_band(at, axis, BAR_R + 0.004, 0.034, thick=0.008), col, bone, fuse=False, surface="metal")


def u_magnet(k, name, bone, bend, u, x, rad, prong, cols, seed, L=0.1):
    """A horseshoe magnet of round bar: its curve's middle at bend, its two prongs running along u, x across."""
    u = unit(*u)
    x = np.array(x, float); x = x - u * (x @ u); x /= np.linalg.norm(x)
    O = np.array(bend, float) + u * rad
    arc = [O + rad * (math.cos(t) * x - math.sin(t) * u) for t in np.linspace(0, math.pi, 9)]
    a_end, z_end = O + x * rad + u * prong, O - x * rad + u * prong
    pts = [a_end, O + x * rad + u * prong * 0.5] + arc + [O - x * rad + u * prong * 0.5, z_end]
    k.add(name, sculptkit.outward(tube(pts, [BAR_R] * len(pts))), BAR, bone, fuse=False, surface="metal")
    for j, (end, col) in enumerate(((a_end, cols[0]), (z_end, cols[1]))):
        band(k, f"{name}band{j}", bone, end - u * 0.03, u, col)
        filings(k, f"{name}fil{j}", bone, end + u * 0.01, u, L=L, seed=seed + j)


def leg(k, s):
    """A lower ball's leg: a horseshoe bent over under the ball, its inner end to the ground, its outer end out."""
    side = "R" if s > 0 else "L"
    bone = f"leg{side}"
    h = unit(s, -0.42, 0)                       # across the arch, the outer end's way
    c = np.array([LOW[0], LOW[1], 0.255])
    rad = 0.072
    arc = [c + rad * (-math.cos(t) * h + math.sin(t) * np.array([0, 0, 1.0])) for t in np.linspace(0, math.pi, 9)]
    inner_foot = c - h * rad + (0, 0, -0.205)
    outer_end = c + h * (rad + 0.2) + (0, 0, -0.1)
    pts = [inner_foot, c - h * rad + (0, 0, -0.1)] + arc + [c + h * (rad + 0.055) + (0, 0, -0.045), outer_end]
    k.add(f"leg{side}", sculptkit.outward(tube(pts, [BAR_R] * len(pts))), BAR, bone, fuse=False, surface="metal")
    down = np.array([0, 0, -1.0])
    out_dir = outer_end - pts[-2]; out_dir /= np.linalg.norm(out_dir)
    band(k, f"legband{side}0", bone, inner_foot + (0, 0, 0.06), down, RED if s > 0 else BLUE)
    band(k, f"legband{side}1", bone, outer_end - out_dir * 0.035, out_dir, BLUE if s > 0 else RED)
    filings(k, f"foot{side}", bone, inner_foot + (0, 0, -0.008), (0, 0, 1), n=11, L=0.085, seed=7 + (s > 0), flat=True)
    filings(k, f"legfil{side}", bone, outer_end + out_dir * 0.01, out_dir, L=0.1, seed=11 + (s > 0))


# --- the eyes: yellow, ringed black, a small black pupil toward the middle
CY, CI, CP, CB = srgb(YELLOW), srgb(INK), srgb(PUPIL), srgb(BALL)


def eye_open(u, v):
    c = np.tile(CY, (len(u), 1))
    c[u * u + v * v > 0.86 ** 2] = CI
    c[(u + 0.12) ** 2 + (v - 0.02) ** 2 < 0.3 ** 2] = CP
    return c


def eye(k, state, centre, look, half, flip=False):
    n = unit(*look)
    at = np.array(centre, float) + n * R
    f = eye_look(state, eye_open, CB, CI, line=0.18)
    col = f if not flip else (lambda a, b: f(-a, b))
    k.decal(at, n, (half, half), col, eps=0.002, rings=20, segs=56)


TOP_LOOK = (0.12, -1, 0.06)
LOW_LOOK = {1: (0.42, -0.9, 0.04), -1: (-0.42, -0.9, 0.04)}


def sandyshocks(src, out):
    k = Kit(src, coat="metal")
    ball(k, "top", "head", TOP, R, unit(*TOP_LOOK))
    for s in (1, -1):
        ball(k, f"low{'R' if s > 0 else 'L'}", "body", LOW * (s, 1, 1), R, unit(*LOW_LOOK[s]))
        leg(k, s)
    # the top ball's magnets: one out to its -X side, one rising up and back on its +X side
    u_magnet(k, "magside", "head", TOP + (-0.095, 0.0, 0.0), (-1, 0.1, 0.12), (0, 0.25, 1), 0.042, 0.1,
             (RED, BLUE), 21)
    u_magnet(k, "magup", "head", TOP + (0.05, 0.035, 0.08), (0.5, 0.42, 0.76), (0.75, -0.45, -0.2), 0.042, 0.1,
             (BLUE, RED), 31)
    # screws on the balls' sides
    mg.screw(k, "screwtop", "head", TOP, R, (0.8, -0.35, -0.45))
    mg.screw(k, "screwL", "body", LOW * (-1, 1, 1), R, (-0.95, -0.25, 0.05))
    mg.screw(k, "screwR", "body", LOW, R, (0.95, -0.2, 0.15))
    # the top ball's violet brow mark and its row of dark dots
    tc = TOP + unit(-0.3, -0.72, 0.72) * R
    CV = srgb(VIOLET)
    def brow(u, v):
        return np.tile(CV, (len(u), 1))
    k.decal(tc, unit(-0.3, -0.72, 0.72), (0.046, 0.013), brow, up=unit(0.6, 0.3, 1), eps=0.002, rings=10, segs=40)
    CD = srgb(DOT)
    def dots(u, v):
        c = np.tile(CB, (len(u), 1))
        for x in (-0.72, -0.36, 0.0, 0.36, 0.72):
            y = -0.15 + 0.35 * x * x
            c[((u - x) / 0.17) ** 2 + ((v - y) / 0.38) ** 2 < 1] = CD
        return c
    dn = unit(0.08, -0.8, -0.6)
    k.decal(TOP + dn * R, dn, (0.054, 0.024), dots, eps=0.002, rings=12, segs=72)
    k.build(voxel=VOXEL, smooth=3, band=0.004, wband=0.05)

    def eyes(state):
        eye(k, state, TOP, TOP_LOOK, 0.056)
        for s in (1, -1):
            eye(k, state, LOW * (s, 1, 1), LOW_LOOK[s], 0.054, flip=s < 0)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    sandyshocks(*args())
