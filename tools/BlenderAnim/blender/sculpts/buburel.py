# Buburel sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/buburel.py -- <the export's Buburel.glb> <out.glb>
#
# The design, from the project's picture (10017-Buburel.jpg) and brief (CP-dex-1-plateau.md, 17): a round red
# ladybird, its shell a glossy dome, a fine black seam down its middle and seven round black spots (one on the seam at
# the front, three a side). Its first mass after the shell is a big round black head tucked under the shell's front
# rim: two white eyes with big black pupils and a glint, two round cream cheek dots lower and wider than the eyes, and
# two short black antennae bent forward from its crown. A dark brown-grey belly under the shell, six small black legs
# hanging, and under it all a wisp of pale yellow-green light that holds it up.
#
# Written to be grown from (buburoza.py imports it): the colours, on_ell, the eye, the cheek, the spot and the
# antenna; the build is under __main__. Skeleton: body (0, 0.02, 0.14), head (0, -0.07, 0.14). The game draws it
# hovering (the export stands 0.027 off the ground).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, polygon, args

RED, RED_D, BLACK, BELLY = "#D4503A", "#A93A2C", "#2C282A", "#4C403B"
WHITE, PUPIL, CHEEK, INK_LT = "#FAF8F2", "#121012", "#F0E6CE", "#C9C0B4"
WISP, WISP_GLOW = "#E6EDA8", "#E2EC9C"


def on_ell(c, r, d):
    """The point of an ellipsoid (centre c, radii r) in direction d from its centre, and its outward normal."""
    c, r, d = np.array(c, float), np.array(r, float), unit(*d)
    p = c + d / math.sqrt(((d / r) ** 2).sum())
    n = (p - c) / r ** 2
    return p, n / np.linalg.norm(n)


def round_eye(white=WHITE, pupil=PUPIL, iris=None, pupil_r=0.56, iris_r=0.8, look=(-0.05, -0.04), rim=None):
    """The line's eye: a white disc, a big round pupil looking a little in and down, a glint up and outward. With an
    iris (Buburoza) a brown ring round the pupil; with a rim a pale outer ring."""
    CW, CP = srgb(white), srgb(pupil)
    CI = srgb(iris) if iris else None
    CR = srgb(rim) if rim else None
    def eye(u, v):
        c = np.tile(CW, (len(u), 1))
        r2 = (u - look[0]) ** 2 + (v - look[1]) ** 2
        if CI is not None:
            c[np.sqrt(u * u + v * v) < 0.86] = CI
        c[r2 < pupil_r ** 2] = CP
        if CR is not None:
            c[np.sqrt(u * u + v * v) > 0.86] = CR
        c[(u - look[0] - 0.2) ** 2 + (v - look[1] - 0.24) ** 2 < 0.19 ** 2] = CW
        c[(u - look[0] + 0.2) ** 2 + (v - look[1] + 0.26) ** 2 < 0.08 ** 2] = CW
        return c
    return eye


def spot(k, centre, radii, d, half, color=BLACK, eps=0.0016):
    """A round spot on a dome (centre, radii) in direction d, cast along the surface's own normal."""
    p, n = on_ell(centre, radii, d)
    up = (0, 0, 1) if abs(n[2]) < 0.85 else (0, 1, 0)
    k.decal(p, n, (half, half), color, up=up, eps=eps, rings=10, segs=40)


def seam(k, centre, radii, a0, a1, w, color=BLACK, n=9, eps=0.0014):
    """The fine line where the shell parts, along the dome's top from angle a0 to a1 (degrees from straight ahead,
    over the top, toward the back): short square patches each cast along the surface's own normal."""
    centre, radii = np.array(centre, float), np.array(radii, float)
    angs = np.radians(np.linspace(a0, a1, n + 1))
    for a, b in zip(angs[:-1], angs[1:]):
        m = (a + b) / 2
        d = (0, -math.cos(m), math.sin(m))
        p, nn = on_ell(centre, radii, d)
        q0, _ = on_ell(centre, radii, (0, -math.cos(a), math.sin(a)))
        q1, _ = on_ell(centre, radii, (0, -math.cos(b), math.sin(b)))
        L = np.linalg.norm(q1 - q0) * 0.56
        along = unit(*(q1 - q0))
        k.decal(p, nn, (w, L), color, up=along, eps=eps, rings=3, segs=24,
                outline=polygon([(-1, 1), (1, 1), (1, -1), (-1, -1)]))


def antenna(k, pts, r0, r1, knob, bone="head", color=BLACK):
    """A short black antenna through pts (written on +X), a round knob at its tip; loose, mirrored."""
    n = len(pts)
    k.add("antenna", tube(pts, list(np.linspace(r0, r1, n)), seg=12), color, bone, fuse=False, mirror=True)
    if knob:
        k.add("knob", ell(pts[-1], (knob, knob, knob)), color, bone, fuse=False, mirror=True)


# --- Buburel -------------------------------------------------------------------------------------------------------
SC, SR = np.array([0.0, 0.035, 0.162]), np.array([0.15, 0.155, 0.115])     # the shell's dome
HC, HR = np.array([0.0, -0.112, 0.148]), np.array([0.1, 0.082, 0.072])     # the head
E = 0.0018


def lay_eyes(k, state):
    look = eye_look(state, round_eye(), srgb(BLACK), srgb(INK_LT), line=0.17)
    for s in (1, -1):
        p, n = on_ell(HC, HR, (0.42 * s, -0.86, 0.2))
        col = look if s > 0 else (lambda u, v, f=look: f(-u, v))
        k.decal(p, n, (0.029, 0.033), col, up=(-0.1 * s, 0, 1), eps=0.0016, rings=20, segs=56)


def buburel(src, out):
    k = Kit(src, coat="shell")
    def dome(V):
        V = V.copy()
        V[:, 2] = np.where(V[:, 2] < 0, V[:, 2] * 0.55, V[:, 2])         # flatter underneath
        return V
    k.add("shell", ell(SC, SR, deform=dome, sub=5), RED, "body")
    k.add("belly", ell((0, 0.03, 0.105), (0.122, 0.125, 0.05)), BELLY, "body")
    k.add("head", ell(HC, HR, sub=5), BLACK, "head")
    # the seam and the seven spots
    seam(k, SC, SR * np.array([1, 1, 1.0 / 1.0]), 60, 150, 0.0028)
    spot(k, SC, SR, (0, -0.62, 0.78), 0.03)
    for s in (1, -1):
        spot(k, SC, SR, (0.52 * s, -0.28, 0.8), 0.031)
        spot(k, SC, SR, (0.88 * s, 0.12, 0.36), 0.035)
        spot(k, SC, SR, (0.45 * s, 0.62, 0.62), 0.031)
    # the cheek dots, lower and wider than the eyes and smaller
    for s in (1, -1):
        p, n = on_ell(HC, HR, (0.8 * s, -0.55, -0.26))
        k.decal(p, n, (0.021, 0.021), CHEEK, eps=0.0014, rings=10, segs=40)
    # the antennae: short, bent forward from the crown
    antenna(k, [(0.03, -0.118, 0.2), (0.036, -0.13, 0.238), (0.052, -0.158, 0.258), (0.068, -0.175, 0.256)],
            0.0085, 0.0078, 0.0105)
    # six small black legs hanging under the belly
    for i, y in enumerate((-0.06, 0.01, 0.075)):
        k.add(f"leg{i}", tube([(0.075, y, 0.085), (0.105, y - 0.005, 0.07), (0.112, y + 0.01, 0.042)],
                              [0.0085, 0.008, 0.0075], seg=12), BLACK, "body", fuse=False, mirror=True)
    # the wisp of light under it: one glowing swirl, on the root so that it stays as the body bobs
    t = np.linspace(0, 1, 30)
    ang = 2 * math.pi * (0.15 + 1.25 * t)
    rad = 0.085 * (1 - 0.55 * t)
    pts = np.stack([0.02 + rad * np.cos(ang), 0.03 + 0.9 * rad * np.sin(ang), 0.012 + 0.04 * t], 1)
    k.add("wisp", tube(pts, list(0.011 - 0.006 * t), seg=10), WISP, "root", fuse=False, glow=WISP_GLOW)
    k.build(voxel=0.0034, smooth=3, band=0.004, wband=0.02)
    k.eye_states(lambda state: lay_eyes(k, state))
    k.export(out)


if __name__ == "__main__":
    buburel(*args())
