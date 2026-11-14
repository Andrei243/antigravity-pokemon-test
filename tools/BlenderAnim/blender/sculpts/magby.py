# Magby sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/magby.py -- <the export's Magby.glb> <out.glb>
#
# The design, from the references: a small red biped whose cranium is a cloud of round puffs over a small face, the
# front puffs' lower edge cutting the tops of the eyes; tall white eyes with a large black pupil toward the nose; a
# yellow beak of a mouth pointing forward and down with a round opening at its tip; a black band round the neck; a
# yellow flame on the belly, wide below and rising to a point; short arms with white claws and a white spike on each
# wrist; short legs, three white claws a foot; a short tail with a ridge of points. Written to be grown from: Magmar
# and Magmortar keep its colours, its eye, its beak and its way of painting flame marks.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, eye_look, polygon, args

src, out = args()
k = Kit(src, coat="fur")

# The line's colours (Magmar and Magmortar copy these unchanged)
RED, YELLOW, BAND, CLAW, INK, MOUTH = "#EC4A44", "#F8D650", "#2B2528", "#F4F1E8", "#1C1416", "#A8384A"

B = np.array([0, 0.0, 0.175])            # the body's middle: a small pear, its round belly low over the thighs
BR = np.array([0.098, 0.092, 0.112])
H = np.array([0, -0.015, 0.372])         # the face's middle (the lower head under the puffs)
HR = np.array([0.08, 0.076, 0.074])


def volume(V, F):
    """Signed volume of a closed mesh: above nought when its faces are wound outward."""
    V = np.asarray(V, float)
    s = 0.0
    for f in F:
        for i in range(1, len(f) - 1):
            s += np.dot(V[f[0]], np.cross(V[f[i]], V[f[i + 1]]))
    return s / 6


def outward(name, shape):
    """A loose piece built by hand must be wound outward, or the game draws its inside."""
    if volume(*shape) <= 0:
        print("KIT: !! piece", name, "is wound inside out")
    return shape


def plate(c, n, r, t):
    """A flat round plate of radius r (or (a, b)) across, half thickness t along n, its middle at c."""
    a_, b_ = (r, r) if np.isscalar(r) else r
    V, F = ell((0, 0, 0), (a_, b_, t), sub=3)
    n = unit(*n)
    a = np.cross(n, (0, 0, 1.0))
    if np.linalg.norm(a) < 1e-3:
        a = np.array([1.0, 0, 0])
    a /= np.linalg.norm(a)
    b = np.cross(n, a)
    return V @ np.stack([a, b, n], axis=1).T + np.array(c, float), F


def ring(c, R, r, seg=40):
    """A band round an upright axis through c: a tube on a closed circle, its seam at the back."""
    a = np.linspace(0, 2 * math.pi, seg + 2) + math.pi / 2            # starting at the back (+Y)
    pts = [(c[0] + R * math.cos(t), c[1] + R * math.sin(t), c[2]) for t in a]
    return tube(pts, [r] * len(pts), seg=12, per=2)


def cone(base, d, length, r0, r1=0.0015):
    """A claw or a point: a short tapering tube from base along d."""
    d = unit(*d)
    b = np.array(base, float)
    return tube([b, b + d * length * 0.5, b + d * length], [r0, r0 * 0.6, r1], seg=12, per=4)


def in_poly(x, y, pts):
    pts = np.array(pts, float)
    res = np.zeros(len(x), bool)
    for (x1, y1), (x2, y2) in zip(pts, np.roll(pts, -1, axis=0)):
        res ^= ((y1 > y) != (y2 > y)) & (x < (x2 - x1) * (y - y1) / (y2 - y1 + 1e-12) + x1)
    return res


def poly_mask(x, y, pts, edge=0.0035):
    """A polygon painted with a crisp edge: the signed distance to its outline (negative inside), eased."""
    pts = np.array(pts, float)
    d = np.full(len(x), 9.0)
    for (x1, y1), (x2, y2) in zip(pts, np.roll(pts, -1, axis=0)):
        ex, ey = x2 - x1, y2 - y1
        t = np.clip(((x - x1) * ex + (y - y1) * ey) / (ex * ex + ey * ey + 1e-12), 0, 1)
        d = np.minimum(d, np.hypot(x - x1 - t * ex, y - y1 - t * ey))
    return inside(np.where(in_poly(x, y, pts), -d, d), edge)


def round_about(P, c, r):
    """(the arc round an upright axis through c, z): the coordinates a mark round a body is painted in."""
    return np.arctan2(P[:, 0] - c[0], -(P[:, 1] - c[1])) * r, P[:, 2]


def pear(V):
    V = V.copy()
    s = 1 + 0.14 * np.clip(-V[:, 2], 0, 1) - 0.2 * np.clip(V[:, 2], 0, 1) ** 1.5
    V[:, 0] *= s; V[:, 1] *= s
    return V


# --- the body ----------------------------------------------------------------------------------------------------
k.add("body", ell(B, BR, deform=pear), RED, "body")
k.add("neck", ell((0, -0.008, 0.285), (0.056, 0.052, 0.04)), RED, "head")
k.add("collar", outward("collar", ring((0, -0.006, 0.292), 0.05, 0.011)), BAND, "body", fuse=False,
      surface="leather")

# The yellow flame on the belly: wide below, its edge waving up to a point a little to one side
BELLY = [(-0.062, 0.085), (0.062, 0.085), (0.076, 0.13), (0.07, 0.18), (0.05, 0.215), (0.038, 0.19),
         (0.018, 0.232), (-0.008, 0.262), (-0.026, 0.228), (-0.05, 0.205), (-0.07, 0.158), (-0.074, 0.115)]
k.paint(lambda P: poly_mask(*round_about(P, B, BR[0]), BELLY) * (P[:, 1] < B[1] - 0.01)
        * (np.hypot(P[:, 0], P[:, 1] - B[1]) < 0.118), YELLOW)       # never on the hands held before it

# --- the head: a small face under a cloud of round puffs --------------------------------------------------------
k.add("face", ell(H, HR), RED, "head")
PUFFS = [((0.0, -0.025, 0.47), 0.064),           # the front middle, over the brow
         ((0.07, -0.036, 0.448), 0.056),         # the front sides, whose lower edge cuts the eyes
         ((0.1, 0.022, 0.455), 0.058),           # the back sides, wide
         ((0.0, 0.05, 0.495), 0.062),            # the back top
         ((0.0, 0.055, 0.425), 0.068),           # the back of the head
         ((0.045, 0.005, 0.508), 0.054)]         # the crown, a little off the middle
for i, (c, r) in enumerate(PUFFS):
    k.add(f"puff{i}", ell(c, (r, r, r)), RED, "head", mirror=c[0] != 0)
# a set-back core filling the creases between the puffs
k.add("core", ell((0, 0.01, 0.462), (0.088, 0.07, 0.06)), RED, "head")

# The beak: a yellow cone forward and down, a round dark opening at its tip
BEAK0 = np.array([0, -0.05, 0.345])
BEAKD = unit(0, -1, -0.32)
BEAKL = 0.088
k.add("beak", tube([BEAK0, BEAK0 + BEAKD * BEAKL * 0.55, BEAK0 + BEAKD * BEAKL], [0.054, 0.046, 0.034], seg=24),
      YELLOW, "head", fuse=False, surface="skin")
k.add("mouth", outward("mouth", plate(BEAK0 + BEAKD * (BEAKL + 0.031), BEAKD, 0.017, 0.004)), MOUTH, "head",
      fuse=False, surface="skin")

# --- arms, forward and a little down, each with two white claws and a spike on the wrist ---------------------------
ARM = [(0.075, -0.02, 0.24), (0.105, -0.06, 0.212), (0.122, -0.105, 0.182)]
k.add("arm", tube(ARM, [0.027, 0.025, 0.023]), RED, "arm{s}", mirror=True)
k.add("hand", ell(ARM[-1], (0.026, 0.026, 0.025)), RED, "arm{s}", mirror=True)
for i, d in enumerate([(0.35, -1, 0.15), (0.5, -1, -0.35)]):
    k.add(f"claw{i}", cone(np.array(ARM[-1]) + unit(*d) * 0.017, d, 0.03, 0.009), CLAW, "arm{s}", fuse=False,
          mirror=True, surface="shell")
k.add("spike", cone((0.11, -0.085, 0.205), (0.3, -0.2, 1), 0.032, 0.01), CLAW, "arm{s}", fuse=False, mirror=True,
      surface="shell")

# --- legs, short, three white claws a foot -------------------------------------------------------------------------
# thighs that are part of the body, short bent shins, big clawed feet
k.add("thigh", ell((0.056, -0.012, 0.095), (0.05, 0.055, 0.045)), RED, "leg{s}", mirror=True)
k.add("leg", tube([(0.058, -0.012, 0.085), (0.062, -0.026, 0.055), (0.062, -0.018, 0.03)], [0.034, 0.03, 0.028]),
      RED, "leg{s}", mirror=True)
FOOT = np.array([0.064, -0.036, 0.022])
k.add("foot", ell(FOOT, (0.04, 0.058, 0.022)), RED, "leg{s}", mirror=True)
for i, a in enumerate((-30, 0, 30)):
    d = unit(math.sin(math.radians(a)), -math.cos(math.radians(a)), -0.1)
    k.add(f"toe{i}", cone(FOOT + d * 0.043 + (0, 0, -0.004), d, 0.028, 0.0095), CLAW, "leg{s}", fuse=False,
          mirror=True, surface="shell")

# --- a short tail back and a little out to +X, a ridge of points along it ------------------------------------------
TAIL = [(0.0, 0.06, 0.15), (0.02, 0.13, 0.13), (0.045, 0.19, 0.135), (0.065, 0.235, 0.15)]
k.add("tail", tube(TAIL, [0.036, 0.026, 0.016, 0.004]), RED, "tail")
for i, (c, r) in enumerate([((0.012, 0.1, 0.163), 0.013), ((0.035, 0.16, 0.152), 0.011)]):
    c = np.array(c)
    k.add(f"ridge{i}", blade([c, c + (0.005, 0.02, 0.022)], [r, 0.002], (1, 0, 0), 0.5), RED, "tail")

# --- the eyes: tall whites, their tops cut straight by the puffs' edge, a large black pupil toward the nose --------
# In each eye's own frame u runs outward and v up.
HE = (0.0175, 0.025)                      # an eye's half sizes
def eye_top(u):
    return 0.62 - 0.22 * u                # a little lower at the outer corner: Magby's worried look
EYE = [(u, eye_top(u)) for u in np.linspace(1, -1, 9)] + \
      [(0.98 * math.cos(t), 0.98 * math.sin(t)) for t in np.linspace(math.pi * 1.06, math.pi * 1.94, 15)]
de = unit(0.5, -0.85, 0.06)
E = np.array([0.044, -0.077, 0.383])
WHITE, CI, LID = np.array([1.0, 1.0, 1.0]), srgb(INK), srgb(RED)
PUPIL = (-0.28, -0.12, 0.52, 0.62)                   # its middle (u, v) and half sizes in the eye's units
def eye_open(u, v, pupil=PUPIL, top=eye_top):
    c = np.tile(WHITE, (len(u), 1))
    pu, pv, pa, pb = pupil
    c[((u - pu) / pa) ** 2 + ((v - pv) / pb) ** 2 < 1] = CI      # a solid pupil, half the eye
    c[v > top(u) - 0.2] = CI                         # the puff's dark edge over the eye
    c[np.hypot(u, v) > 0.84] = CI                    # a rim a sixth of the eye
    return c
def frame(n):
    n = np.array(n, float); n /= np.linalg.norm(n)
    right = np.cross((0, 0, 1.0), n); right /= np.linalg.norm(right)
    return right, np.cross(n, right)
def eyes(state, E=E, de=de, HE=HE, outline=EYE, look=eye_open, lid=LID):
    """Both eyes, laid by hand so that u runs outward on each (the outline is mirrored with it)."""
    for side in (1, -1):
        m = np.array([side, 1, 1.0])
        c, n = E * m, de * m
        flip = (lambda f: f) if side == 1 else (lambda f: (lambda u, v: f(-u, v)))
        out_ = polygon([(side * u, v) for u, v in (outline if side == 1 else outline[::-1])])
        k.decal(c, n, HE, flip(eye_look(state, look, lid, CI)), eps=0.006, outline=out_, rings=20, segs=56)

k.build(voxel=0.006, band=0.006)
k.eye_states(eyes)
k.export(out)
