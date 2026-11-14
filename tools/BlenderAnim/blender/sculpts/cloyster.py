# Cloyster sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/cloyster.py -- <the export's Cloyster.glb> <out.glb>
# Grown from shellder.py: the same shell made of loose sheets with a darker lining, the same stout rounded cone spikes,
# the same way of laying big simple eyes on a dark round body.
#
# The design, from the references: a round shell of grey violet in two halves, left and right, meeting over the top
# and under the bottom and opening at the front in a tall oval; stout cone spikes standing out all round it, each on a
# rounded knob of a base, the two over the top long, and low round knobs between them. The opening's edge is a pale,
# wavy, ridged rim, with a second pale wavy rim set back inside it and the dark grey lining between. In it a dark
# violet pearl of a head: narrow white eyes slanting down to the middle with black pupils toward it, a wide white grin
# with a row of flat teeth, and a pale grey horn rising from its top and leaning out to its right (the viewer's left).
# The skeleton is one bone, the body, at z 0.592: everything is on it.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, polygon, eye_look, args

src, out = args()
k = Kit(src, coat="shell")

SHELL, LINING, RIM, PEARL, HORN = "#9A92C8", "#4E4A62", "#CDD0EE", "#3D3772", "#B4B6CC"
WHITE, INK = "#F6F6F4", "#121218"

C = np.array([0.0, 0.0, 0.592])           # the shell's middle, on the body bone
R = np.array([0.40, 0.38, 0.42])          # the shell's radii
OX, OZ = 0.245, 0.335                     # the opening's half width and half height


def wave(a):
    """The opening's edge: wavy all round, as the rim's ridges."""
    return 1 + 0.045 * np.sin(9 * a)


def front_t(a, ox=OX, oz=OZ, scale=1.0):
    """How far forward (a share of R's y, -1 the front) the shell reaches at angle a round the front-back axis."""
    rx, rz = ox * wave(a), oz * wave(a)
    rho = 1 / np.sqrt((np.sin(a) / rx) ** 2 + (np.cos(a) / rz) ** 2)          # the opening's edge, off the axis
    # as a share of the shell's own radius at that angle
    rs = 1 / np.sqrt((np.sin(a) / (R[0] * scale)) ** 2 + (np.cos(a) / (R[2] * scale)) ** 2)
    return -np.sqrt(np.clip(1 - (rho / rs) ** 2, 0, 1))


def bumps(a, t):
    # a gently knobbly outside
    return 0.025 * np.sin(5 * a + 1.3) * np.sin(7 * t + 0.4)


def half(side, scale=1.0, lift=0.0, back=0.995):
    """One half of the shell: u from the top (a 0) round its side to the bottom (a pi), v from the opening to the
    hinge at the back."""
    def at(u, v):
        a = -0.04 + u * (math.pi + 0.08)
        tf = front_t(a, scale=scale) + lift
        t = tf + v * (back - tf)
        w = np.sqrt(np.clip(1 - t * t, 0, 1))
        s = scale * (1 + bumps(a, t) * (scale > 0.99))
        x = side * R[0] * np.sin(a) * w * s
        z = R[2] * np.cos(a) * w * s
        y = R[1] * t * scale
        return np.stack([x + C[0], y + C[1], z + C[2]], axis=-1)
    return at


from sculptkit import sheet
for side, nm in ((1, "R"), (-1, "L")):
    k.add("shell" + nm, sheet(half(side, 1.0 + 0.004 * (side < 0)), 90, 46, 0.018), SHELL, "body", fuse=False,
          surface="shell")
    # the lining, darker, under it and reaching a little less far forward
    k.add("lining" + nm, sheet(half(side, 0.9, lift=0.03), 70, 36, 0.01), LINING, "body", fuse=False, surface="shell")
k.add("hinge", ell(C + (0, R[1] * 0.985, 0), (0.06, 0.03, 0.06), sub=3), SHELL, "body", fuse=False, surface="shell")

# The pale rim round the opening's edge, ridged with the edge's wave, and a second one set back inside it
def ring_pts(ox, oz, depth, n=180):
    a = np.linspace(0, 2 * math.pi, n, endpoint=False)
    a = np.concatenate([a, a[:4]])
    rx, rz = ox * wave(a), oz * wave(a)
    rho = 1 / np.sqrt((np.sin(a) / rx) ** 2 + (np.cos(a) / rz) ** 2)
    x, z = rho * np.sin(a), rho * np.cos(a)
    y = -R[1] * np.sqrt(np.clip(1 - (x / R[0]) ** 2 - (z / R[2]) ** 2, 0, 1)) + depth
    return np.stack([x, y, z], axis=1) + C


k.add("rim", tube(ring_pts(OX * 0.99, OZ * 0.99, 0.006), [0.026] * 184, seg=14, per=2), RIM, "body", fuse=False,
      surface="shell")
k.add("rimin", tube(ring_pts(OX * 0.8, OZ * 0.84, 0.075), [0.019] * 184, seg=12, per=2), RIM, "body", fuse=False,
      surface="shell")


# --- the spikes: stout rounded cones on knobs, standing out all round -----------------------------------------------
def on_shell(d):
    d = unit(*d)
    s = 1 / np.sqrt((d[0] / R[0]) ** 2 + (d[1] / R[1]) ** 2 + (d[2] / R[2]) ** 2)
    return C + d * s, d


def spike(name, d, length, r0):
    p, dd = on_shell(d)
    root = p + dd * 0.035
    tip = p + dd * length
    pts = [root + (tip - root) * t for t in np.linspace(0, 1, 7)]
    k.add(name, tube(pts, [r0 * (1 - t) + 0.01 * t for t in np.linspace(0, 1, 7)], seg=20, per=4), SHELL, "body",
          fuse=False, surface="shell")
    k.add(name + "knob", ell(p + dd * 0.05, (r0 * 1.12, r0 * 1.12, r0 * 1.12), sub=3), SHELL, "body", fuse=False,
          surface="shell")


SPIKES = [((0.72, 0.2, 0.75), 0.34, 0.07), ((-0.72, 0.2, 0.75), 0.30, 0.07),       # the two long ones over the top
          ((1, 0.15, 0.0), 0.2, 0.06), ((-1, 0.15, -0.05), 0.17, 0.058),
          ((0.68, 0.1, -0.78), 0.17, 0.058), ((-0.66, 0.15, -0.8), 0.15, 0.055),
          ((0.05, 0.45, 1), 0.12, 0.05), ((0.05, 0.4, -1), 0.12, 0.05),
          ((0.55, 1, 0.35), 0.14, 0.055), ((-0.55, 1, -0.3), 0.14, 0.055), ((0.0, 1, -0.6), 0.1, 0.05)]
for i, (d, ln, r0) in enumerate(SPIKES):
    spike(f"spike{i}", d, ln, r0)

# low round knobs between them
for i, d in enumerate([(0.9, 0.0, 0.42), (-0.9, 0.05, 0.4), (0.9, 0.05, -0.4), (-0.9, 0.0, -0.45), (0.45, 0.6, 0.8),
                       (-0.45, 0.6, 0.85), (0.35, 0.6, -0.9), (-0.4, 0.55, -0.85), (0.95, 0.55, 0.1),
                       (-0.95, 0.6, 0.0), (0.0, 1.0, 0.2)]):
    p, dd = on_shell(d)
    k.add(f"knob{i}", ell(p + dd * 0.014, (0.044, 0.044, 0.044), sub=3), SHELL, "body", fuse=False,
          surface="shell")

# --- the pearl of a head inside, its front flattened for the face ---------------------------------------------------
PC = C + np.array([0, -0.06, -0.01])
k.add("pearl", ell(PC, (0.19, 0.17, 0.25)), PEARL, "body")
k.add("face", ell(PC + (0, -0.095, 0.0), (0.16, 0.08, 0.2)), PEARL, "body")

# the horn: a pale grey cone from the head's top, leaning out to the model's left (-X: the viewer's left)
HP = [PC + (0.0, -0.07, 0.18), PC + (-0.06, -0.17, 0.27), PC + (-0.15, -0.26, 0.34), PC + (-0.26, -0.32, 0.4)]
k.add("horn", tube(HP, [0.065, 0.05, 0.03, 0.008], seg=18, per=6), HORN, "body", fuse=False, surface="metal")

# --- the grin: a wide white crescent, its top curving up at the corners, flat teeth with dark lines between ---------
CW, CI, CP = srgb(WHITE), srgb(INK), srgb(PEARL)


def grin(u, v):
    c = np.tile(CP, (len(u), 1))
    top = 0.25 + 0.55 * u * u
    bot = -0.8 + 1.0 * u * u
    w = (v < top) & (v > bot) & (np.abs(u) < 0.96)
    c[w] = CW
    for x in ():
        c[w & (np.abs(u - x) < 0.06) & (v > top - 0.55)] = srgb("#8A86A8")
    return c


k.decal(PC + (0, -0.17, -0.09), unit(0, -1, -0.15), (0.12, 0.07), grin, eps=0.004, rings=18, segs=64)

k.build(voxel=0.006, smooth=4, band=0.006)


# --- the eyes: narrow white almonds slanting down to the middle, a black pupil toward it ------------------------------
def eye(u, v):
    c = np.tile(CP, (len(u), 1))
    top = 0.15 + 0.6 * u                       # high at the outer corner, low at the inner: a sly, mean look
    white = ((u / 0.95) ** 2 + (v / 0.85) ** 2 < 1) & (v < top)
    c[white] = CW
    c[white & (((u + 0.28) / 0.3) ** 2 + ((v + 0.18) / 0.42) ** 2 < 1)] = CI
    return c


PALE = srgb("#C9C4F0")


def eyes(state):
    for s in (1, -1):
        look = eye_look(state, eye, CP, PALE, line=0.17)
        lk = look if s > 0 else (lambda f: (lambda u, v: f(-u, v)))(look)
        k.decal(PC + (s * 0.075, -0.17, 0.05), unit(s * 0.4, -1, 0.05), (0.06, 0.05), lk, eps=0.003, rings=20, segs=56)


k.eye_states(eyes)
k.export(out)
