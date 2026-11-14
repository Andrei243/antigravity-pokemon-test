# Shellder sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/shellder.py -- <the export's Shellder.glb> <out.glb>
#
# The design, from the references: a bivalve shell of blue-violet, half open at the front. The upper half is a domed
# cap of five rounded ribs running from the hinge at the back to a front lip that overhangs the face, its flanks
# coming down past the face on both sides; stout rounded cone spikes on it, one each side pointing out sideways and one
# each side rising up and back, and a low flap of a crest at the back of the top. The lower half is a dish with a wavy,
# frilled lip, darker inside. Between them a black round body: two big white eyes with small black pupils (a cheeky,
# wide stare), cut on top by the shell's lip, and a long pink tongue hanging out of the mouth below them, over the
# lower lip, a crease down its middle.
# The skeleton is one bone, the body, at z 0.239: everything is on it.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, sheet, srgb, inside, smoothstep, unit, eye_look, args

src, out = args()
k = Kit(src, coat="shell")

SHELL, INNER, PEARL, TONGUE, CREASE = "#8C84D8", "#544A92", "#1C1A24", "#EC8EA6", "#C8667E"
WHITE, INK = "#F6F6F4", "#101014"

C = np.array([0.0, 0.0, 0.24])          # the body's middle, on the body bone


def valve(c, R, a0, a1, y0, y1, up, rib, frill, r_scale=1.0, fwd=None):
    """
    One half of the shell as a sheet: u runs across the half round the front-back axis (angle a from the top, or the
    bottom for the lower half), v from the front lip (y0, a share of Ry) to the hinge (y1). rib(a, v) and frill(a, v)
    swell the radius; fwd(a) moves the front lip forward or back by the angle.
    """
    def at(u, v):
        a = a0 + u * (a1 - a0)
        yf = y0 + (fwd(a) if fwd is not None else 0)
        t = yf + v * (y1 - yf)                       # a share of Ry, -1 front .. 1 back
        w = np.sqrt(np.clip(1 - t * t, 0, 1))
        s = r_scale * (1 + rib(a, v) + frill(a, v))
        x = R[0] * np.sin(a) * w * s
        z = up * R[2] * np.cos(a) * w * s
        y = R[1] * t
        return np.stack([x + c[0], y + c[1], z + c[2]], axis=-1)
    return at


A_UP = math.radians(102)


def rib_up(a, v):
    # five rounded ribs across the top, creased between, fading out toward the hinge; the lip flares out as a brim
    return 0.045 * np.abs(np.cos(5 * a)) ** 0.6 * (1 - 0.6 * v) + 0.09 * (1 - v) ** 7


def none_(a, v):
    return 0 * a


def lip_up(a):
    # the lip runs nearly straight across over the eyes, the flanks falling back, so the face shows whole
    return 0.32 * (a / A_UP) ** 2


CU = C + np.array([0, 0.02, 0.02])
RU = (0.245, 0.215, 0.205)
k.add("shellup", sheet(valve(CU, RU, -A_UP, A_UP, -0.74, 0.999, 1, rib_up, none_, fwd=lip_up), 72, 48, 0.014),
      SHELL, "body", fuse=False, surface="shell")
# its inside darker, just under it and a little short of its edges
k.add("shellupin", sheet(valve(CU, RU, -A_UP * 0.985, A_UP * 0.985, -0.72, 0.995, 1, rib_up, none_, r_scale=0.955,
                               fwd=lip_up), 60, 40, 0.008),
      INNER, "body", fuse=False, surface="shell")

A_LO = math.radians(100)


def frill_lo(a, v):
    # the lower lip's ruffle: a wave round its edge, strongest at the front, flaring out a little
    near = 1 - smoothstep(0.0, 0.4, v)
    return (0.05 * np.sin(9 * a) + 0.06) * near


def lip_lo(a):
    return 0.04 * np.sin(9 * a) + 0.3 * (a / A_LO) ** 2


CL = C + np.array([0, 0.03, -0.005])
RL = (0.22, 0.2, 0.17)
k.add("shelllo", sheet(valve(CL, RL, -A_LO, A_LO, -0.82, 0.999, -1, none_, frill_lo, fwd=lip_lo), 84, 44, 0.014),
      SHELL, "body", fuse=False, surface="shell")
k.add("shelloin", sheet(valve(CL, RL, -A_LO * 0.985, A_LO * 0.985, -0.80, 0.995, -1, none_, frill_lo, r_scale=0.95,
                              fwd=lip_lo), 70, 36, 0.008),
      INNER, "body", fuse=False, surface="shell")


# The spikes: stout rounded cones, their roots sunk in the shell
def cone(root, tip, r0):
    root, tip = np.array(root, float), np.array(tip, float)
    pts = [root + (tip - root) * t for t in np.linspace(0, 1, 6)]
    return tube(pts, [r0 * (1 - t) + 0.006 * t for t in np.linspace(0, 1, 6)], seg=18, per=4)


for s in (1, -1):
    m = np.array([s, 1, 1])
    k.add("spikeside" + "RL"[s < 0], cone(C + np.array([0.19, 0.05, 0.05]) * m, C + np.array([0.29, 0.07, 0.1]) * m, 0.04),
          SHELL, "body", fuse=False, surface="shell")
    k.add("spikeup" + "RL"[s < 0], cone(C + np.array([0.13, 0.11, 0.14]) * m, C + np.array([0.21, 0.17, 0.27]) * m, 0.037),
          SHELL, "body", fuse=False, surface="shell")
# a low broad flap of a crest at the back of the top, turned to one side so it shows from the front three-quarter
k.add("crest", blade([C + (0.0, 0.12, 0.17), C + (0.025, 0.18, 0.215), C + (0.045, 0.22, 0.235)],
                     [0.06, 0.05, 0.03], unit(0.77, 0, -0.64), 0.35), SHELL, "body", fuse=False, surface="shell")

# --- the black body, its front a little flattened for the eyes ---------------------------------------------------------
k.add("pearl", ell(C, (0.175, 0.14, 0.15)), PEARL, "body")
k.add("face", ell(C + (0, -0.08, 0.015), (0.14, 0.065, 0.11)), PEARL, "body")
# the hinge at the back, where the two halves meet
k.add("hinge", ell(C + (0, 0.205, 0.012), (0.085, 0.03, 0.04)), SHELL, "body", fuse=False, surface="shell")

# The tongue: out of the mouth under the eyes, forward over the lower lip and hanging down, flat, its tip rounded
TP = [C + (0, -0.105, -0.05), C + (0, -0.18, -0.075), C + (0, -0.225, -0.125), C + (0, -0.232, -0.185),
      C + (0, -0.215, -0.22)]
k.add("tongue", blade(TP, [0.035, 0.05, 0.058, 0.06, 0.056], unit(0, 1, -0.15), 0.36), TONGUE, "body")
# its crease down the middle
k.paint(lambda P: inside(np.abs(P[:, 0]) - 0.003, 0.002) * (P[:, 2] < C[2] - 0.11) * (P[:, 2] > C[2] - 0.23)
        * (P[:, 1] < -0.21), CREASE, on=["tongue"])

k.build(voxel=0.006, smooth=4, band=0.006)

# --- the eyes: big white ovals straight on the black, a small round black pupil, a wide cheeky stare -------------------
CW, CI, PALE = srgb(WHITE), srgb(INK), srgb("#C9C4F0")


def eye(u, v):
    c = np.tile(srgb(PEARL), (len(u), 1))
    c[u * u + v * v < 0.9] = CW
    c[((u - 0.02) / 0.28) ** 2 + ((v - 0.0) / 0.28) ** 2 < 1] = CI
    return c


def eyes(state):
    for s in (1, -1):
        look = eye_look(state, eye, srgb(PEARL), PALE, line=0.17)
        lk = look if s > 0 else (lambda f: (lambda u, v: f(-u, v)))(look)
        k.decal(C + (s * 0.07, -0.13, 0.04), unit(s * 0.42, -1, 0.12), (0.052, 0.06), lk, eps=0.003, rings=20, segs=56)


k.eye_states(eyes)
k.export(out)
