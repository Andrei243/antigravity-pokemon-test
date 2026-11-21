# Darkrai-Mega sculpted on the skeleton and clips of the game's own export of the form (sculptkit.py), grown from
# darkrai.py (which builds as it is read, so what is needed is copied here: the line's black, the white of its hair,
# the almond eye with a pale rim, a dark pupil and a glint):
#   blender --background --factory-startup --python sculpts/darkraimega.py -- <the export's Darkrai-Mega.glb> <out.glb>
# From the references (they overrule the game's sculptor, which lays the body along the dark): Darkrai curled into a
# ball of night. Largest mass first: the black ball itself, crazed with pale cracks; long black tendrils of smoke
# streaming from it in waves, the longest out to each side, others up, back and down; a star of dark grey spikes
# spread over the ball's front round the face (the base's red collar gone grey and flat); a small black head in the
# star's middle with ONE magenta eye; a white plume standing up from the crown in a wavering flame, thin at its tip,
# and two white locks falling down beside the face.
# Reference ratios (3D picture): the plume above the ball / the ball's height 0.95; the width over the tendrils / the
# ball's width 3.4 (held to 2.8 here, so that the game's cell keeps the ball large); the star's span / the ball's
# width 0.75; the eye / the ball's width 0.09.
# The export's joints: body (0, 0, 0.14), head (0, -0.06, 0.21), trails (+-0.06, -0.03..0.03, 0.16..0.11).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, blade, hull, outward, srgb, unit, eye_look, polygon, inside, args)

src, out = args()
k = Kit(src, coat="fur")

BLACK, SMOKE, STAR, WHITE = "#2A2A32", "#26262E", "#7A7888", "#ECEDF1"
CRACK = "#D4BCD2"
PINK, PALE, PUPIL, GLINT = "#E23CC8", "#F6C2EE", "#2A0C2A", "#FFF2FC"

BC, BR = np.array([0.0, 0.015, 0.2]), np.array([0.148, 0.142, 0.142])


def on_ball(d):
    u = unit(*d)
    p = BC + BR * u
    n = u / BR; n /= np.linalg.norm(n)
    return p, n


# --- the ball, and the small head at its front --------------------------------------------------------------------
k.add("ball", ell(BC, BR), BLACK, "body")
HC = np.array([0.0, -0.11, 0.245])
k.add("head", ell(HC, (0.064, 0.05, 0.068)), BLACK, "head")


# The cracks: pale zigzag lines over the ball, each a short path on its surface
CRACKS = [[(0.3, -0.5, 0.8), (0.55, -0.2, 0.8), (0.7, 0.05, 0.7), (0.85, 0.2, 0.45)],
          [(-0.35, -0.45, 0.82), (-0.6, -0.25, 0.75), (-0.75, 0.0, 0.6)],
          [(0.2, 0.5, 0.85), (0.0, 0.7, 0.7), (0.15, 0.9, 0.4), (0.0, 1.0, 0.05)],
          [(0.95, -0.2, 0.0), (0.9, 0.2, -0.25), (0.75, 0.5, -0.35)],
          [(-0.95, -0.15, 0.1), (-0.85, 0.3, -0.2), (-0.6, 0.65, -0.35)],
          [(-0.3, 0.6, 0.75), (-0.5, 0.75, 0.4), (-0.4, 0.9, 0.1)],
          [(0.6, -0.75, -0.2), (0.75, -0.55, -0.45)],
          [(-0.55, -0.75, -0.25), (-0.72, -0.5, -0.45)]]
SEGS = []
for path in CRACKS:
    pts = [on_ball(d)[0] for d in path]
    SEGS += list(zip(pts[:-1], pts[1:]))


def cracks(P):
    best = np.full(len(P), 9.0)
    for a, b in SEGS:
        ab = b - a
        t = np.clip(((P - a) @ ab) / (ab @ ab), 0, 1)
        best = np.minimum(best, np.linalg.norm(P - (a + np.outer(t, ab)), axis=1))
    return inside(best - 0.0055, 0.002)


k.paint(cracks, CRACK, on=["ball"])

# --- the star: dark grey spikes spread over the ball's front round the face ----------------------------------------
STAR_POINTS = [(90, 0.13), (35, 0.11), (145, 0.11), (0, 0.14), (180, 0.14), (-30, 0.12), (-150, 0.12),
               (-65, 0.13), (-115, 0.13)]
FN = unit(0, -1, 0.32)                                      # the face's middle on the ball
FX, FZ = np.array([1.0, 0, 0]), unit(0, 0.32, 1)
for i, (ang, ln) in enumerate(STAR_POINTS):
    a = math.radians(ang)
    d = FX * math.cos(a) + FZ * math.sin(a)
    side = np.cross(FN, d)

    def onb(off):
        q = FN + off
        p, n = on_ball(q)
        return p, n
    r0, n0 = onb(d * 0.32)
    rl, nl = onb(d * 0.3 + side * 0.26)
    rr, nr = onb(d * 0.3 - side * 0.26)
    tp, nt = onb(d * (0.32 + (ln + 0.025) / 0.14))
    k.add(f"star{i}", hull([rl + nl * 0.018, rr + nr * 0.018, rl - nl * 0.02, rr - nr * 0.02,
                            r0 + n0 * 0.022, tp + nt * 0.006, tp - nt * 0.006]), STAR, "body", fuse=False,
          surface="fur")


# --- the tendrils: black ribbons of smoke waving out from the ball -------------------------------------------------
def tendril(name, root, d, length, wave, amp, r0, bone, n=9, phase=0.0, turn=0.0, barbs=()):
    """A flat ribbon from root along d, waving along `wave` (its broad face holds d and wave), tapering to a point."""
    d, wave = unit(*d), unit(*wave)
    wave = wave - d * (wave @ d); wave /= np.linalg.norm(wave)
    t = np.linspace(0, 1, n)
    pts = [np.array(root) + d * length * s + wave * amp * math.sin(2.6 * math.pi * s + phase) * min(1, s * 3)
           + np.array((0, 0, turn)) * s * s for s in t]
    radii = [r0 * (1 - 0.8 * s ** 1.2) for s in t]
    flat = np.cross(d, wave)
    k.add(name, outward(blade(pts, radii, flat, 0.26)), SMOKE, bone, fuse=False, surface="fur")
    # sharp barbs off the ribbon's edges, the smoke torn into points
    for j, sj in enumerate(barbs):
        q = int(sj * (n - 1))
        p = pts[q]
        e = wave * (1 if j % 2 == 0 else -1)
        w = radii[q]
        k.add(f"{name}barb{j}", hull([p + d * w * 0.8 + flat * 0.01, p - d * w * 0.8 + flat * 0.01,
                                      p + d * w * 0.8 - flat * 0.01, p - d * w * 0.8 - flat * 0.01,
                                      p + e * (w + 0.035) + d * 0.025]), SMOKE, bone, fuse=False, surface="fur")


for s, side in ((1, "R"), (-1, "L")):
    tendril(f"side{side}", (0.1 * s, 0.075, 0.2), (s, 0.12, 0.12), 0.3, (0, 0, 1), 0.03, 0.062, f"trail{side}1",
            barbs=(0.3, 0.5, 0.7))
    tendril(f"up{side}", (0.06 * s, 0.085, 0.28), (0.5 * s, 0.3, 0.85), 0.21, (s, 0, -0.5), 0.02, 0.048,
            f"trail{side}0", phase=1.0, barbs=(0.45,))
    tendril(f"low{side}", (0.09 * s, 0.09, 0.13), (0.75 * s, 0.4, -0.45), 0.2, (0, 0, 1), 0.022, 0.052,
            f"trail{side}2", phase=2.0, barbs=(0.4,))
    tendril(f"under{side}", (0.05 * s, 0.02, 0.08), (0.45 * s, -0.2, -0.55), 0.11, (s, 0, 0.4), 0.014, 0.04,
            f"trail{side}2", phase=0.5)
tendril("back", (0.0, 0.11, 0.22), (0.15, 1, 0.25), 0.22, (0, 0, 1), 0.025, 0.056, "body", phase=0.6, barbs=(0.4, 0.65))
tendril("crown", (-0.02, 0.06, 0.3), (-0.3, 0.55, 0.8), 0.18, (1, 0, 0), 0.02, 0.046, "body", phase=1.7)
# Broad roots: dark puffs where the side tendrils leave the ball
for s in (1, -1):
    k.add(f"root{'R' if s > 0 else 'L'}", ell((0.11 * s, 0.07, 0.19), (0.05, 0.06, 0.06)), BLACK,
          f"trail{'R' if s > 0 else 'L'}1")

# --- the plume: a white wavering flame up from the crown, and two locks falling beside the face --------------------
PLUME = [(0.0, -0.085, 0.3), (0.01, -0.085, 0.35), (-0.012, -0.08, 0.4), (0.012, -0.075, 0.45),
         (-0.006, -0.07, 0.5), (0.01, -0.065, 0.54)]
k.add("plume", blade(PLUME, [0.05, 0.044, 0.036, 0.028, 0.02, 0.013], (0, 1, 0), 0.62), WHITE, "head",
      surface="hair")
k.add("plumebase", ell((0.0, -0.1, 0.3), (0.06, 0.045, 0.032)), WHITE, "head", surface="hair")
for s, side in ((1, "R"), (-1, "L")):
    k.add(f"lock{side}", tube([(0.03 * s, -0.13, 0.305), (0.056 * s, -0.152, 0.265), (0.062 * s, -0.155, 0.215),
                               (0.054 * s, -0.146, 0.17)], [0.024, 0.022, 0.019, 0.014]), WHITE, "head",
          surface="hair")

k.build(voxel=0.0055, smooth=3, band=0.004)

# --- the eye: one magenta almond in the face's middle, a pale rim, a dark pupil and a glint ------------------------
EC = np.array([0.004, -0.16, 0.245])
EN = unit(0.04, -1, 0.05)
ALMOND = polygon([(1.0, 0.1), (0.62, 0.62), (0.05, 0.86), (-0.55, 0.66), (-1.0, 0.05), (-0.6, -0.58),
                  (0.0, -0.82), (0.6, -0.6)])
C_, P_, D_, G_ = srgb(PINK), srgb(PALE), srgb(PUPIL), srgb(GLINT)
LID, INK = srgb(BLACK), srgb("#C88AC0")


def open_eye(u, v):
    r = np.sqrt(u * u + v * v)
    c = np.tile(C_, (len(u), 1))
    c[r > 0.8] = P_
    c[(u * 1.6) ** 2 + (v * 1.1) ** 2 < 0.42 ** 2] = D_
    c[(u + 0.18) ** 2 + (v - 0.28) ** 2 < 0.16 ** 2] = G_
    return c


def eyes(state):
    k.decal(EC, EN, (0.03, 0.026), eye_look(state, open_eye, LID, INK, line=0.17), eps=0.004,
            outline=ALMOND, rings=20, segs=56)


k.eye_states(eyes)
k.export(out)
