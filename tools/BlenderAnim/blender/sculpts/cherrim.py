# Cherrim (Overcast Form) sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/cherrim.py -- <the export's Cherrim.glb> <out.glb>
# Cherubi grown: the closed bud against the clouds. Two big dark purple petals folded down round a small pink body like a
# cloak, the outer one overlapping the inner along a seam that runs from under the sepals down and out to the hem; the
# hem flaring a little; a green five-pointed sepal cap on top, its front point hanging down the bud's front, the sides
# straight out; Cherubi's stem grown into a rose-pink comma curled over the top; the pink body peeking out under the
# hem with its eyes, on two tiny feet.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, sheet, srgb, unit, eye_look, args

src, out = args()
k = Kit(src, coat="leaf")

PURPLE, INNER, GREEN, PINK, FEET = "#7C5A98", "#694A86", "#3DA84A", "#DD6A82", "#CC5A72"
PLUM, CRIMSON = "#6E1636", "#B02A50"


def spline(pts, per=12):
    p = np.array(pts, float)
    e = np.vstack([2 * p[0] - p[1], p, 2 * p[-1] - p[-2]])
    o = []
    for i in range(len(p) - 1):
        p0, p1, p2, p3 = e[i], e[i + 1], e[i + 2], e[i + 3]
        for j in range(per):
            t = j / per
            o.append(0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t
                            + (-p0 + 3 * p1 - 3 * p2 + p3) * t ** 3))
    o.append(p[-1])
    return np.array(o)


# --- the bud's profile: (radius out from the axis, height), from the top under the sepals down to the flared hem ----
PROF = spline([(0.0, 0.53), (0.07, 0.524), (0.13, 0.505), (0.172, 0.476), (0.198, 0.432), (0.212, 0.372),
               (0.219, 0.3), (0.217, 0.232), (0.218, 0.172), (0.23, 0.122), (0.247, 0.086), (0.262, 0.064)], 16)
SEG = np.linalg.norm(np.diff(PROF, axis=0), axis=1)
ARC = np.concatenate([[0], np.cumsum(SEG)])
TAN = np.gradient(PROF, axis=0); TAN /= np.linalg.norm(TAN, axis=1, keepdims=True)
NRM = np.stack([-TAN[:, 1], TAN[:, 0]], axis=1)          # outward: away from the axis and up on the dome
DEPTH = 0.93                                              # the bud a little shallower front to back


def prof(s, off=0.0):
    """Radius and height at arc length s down the profile, pushed off along its outward normal."""
    h = np.interp(s, ARC, PROF[:, 0]); z = np.interp(s, ARC, PROF[:, 1])
    nh = np.interp(s, ARC, NRM[:, 0]); nz = np.interp(s, ARC, NRM[:, 1])
    return h + off * nh, z + off * nz


def s_at_height(z):
    return np.interp(-z, -PROF[:, 1], ARC)


S0 = float(np.interp(0.035, PROF[:, 0], ARC))             # where the petals start, hidden under the sepals


def hem(th):
    """The hem's height round the bud: higher at the front, so the face shows under it."""
    return 0.066 + 0.05 * np.clip(np.cos(th), 0, 1) ** 2


def around(th, h, z):
    return np.stack([np.sin(th) * h, -np.cos(th) * h * DEPTH, z], axis=-1)


def petal(lo, hi, off):
    """A petal: lo(f), hi(f) its angles at f (0 top, 1 hem) of its length; off how far out from the profile."""
    def at(u, v):
        f0 = v
        th0 = lo(f0) + (hi(f0) - lo(f0)) * u
        s1 = s_at_height(hem(th0))
        s = S0 + (s1 - S0) * v
        f = (s - S0) / (ARC[-1] - S0)
        th = lo(f) + (hi(f) - lo(f)) * u
        h, z = prof(s, off)
        return around(th, h, z)
    return sheet(at, 96, 40, 0.011)


D = math.radians
# The outer petal wraps from the back round the model's left and across the front; its edge runs from the top's front
# down and out to the hem on the right. The inner petal shows to the right of that edge and is overlapped at the back.
SEAM = lambda f: D(4) + D(52) * f ** 1.2
k.add("petalA", petal(lambda f: SEAM(f) - D(236), SEAM, 0.016), PURPLE, "body", fuse=False)
k.add("petalB", petal(lambda f: -D(30) + 0 * f, lambda f: D(205) + 0 * f, 0.0), INNER, "body", fuse=False)
k.add("cap", ell((0, 0, 0.522), (0.06, 0.056, 0.022)), PURPLE, "body", fuse=False)

# --- the sepals: a green star of five points laid over the bud's top -------------------------------------------------
TIPS = [(0.0, 0.29, 0.0), (D(72), 0.235, D(10)), (-D(72), 0.235, D(10)), (D(144), 0.215, D(30)),
        (-D(144), 0.215, D(30))]          # (angle, length along the surface, how far it lifts off when it leaves it)


def sepal_at(u, v):
    th = (u - 0.5) * 2 * math.pi
    L = np.full_like(th, 0.11)
    lift_at = np.full_like(th, 0.5)
    alpha = np.zeros_like(th)
    wsum = np.zeros_like(th)
    for a, length, lift in TIPS:
        d = np.angle(np.exp(1j * (th - a)))
        w = np.clip(1 - np.abs(d) / D(36), 0, 1) ** 1.0
        L = np.maximum(L, 0.11 + (length - 0.11) * w)
        wf = np.clip(1 - np.abs(d) / D(36), 0, 1)
        alpha += wf * lift
        wsum += wf
    alpha /= np.maximum(wsum, 1e-6)
    # every point leaves the dome at its shoulder but the front one, which hangs down the bud's front
    front = np.clip(1 - np.abs(np.angle(np.exp(1j * th))) / D(40), 0, 1) ** 0.6
    lift_at = 0.15 + 0.4 * front
    sig = (0.02 + 0.98 * v) * L
    sl = np.minimum(sig, lift_at)
    h, z = prof(sl, 0.03)
    beyond = np.maximum(sig - lift_at, 0)
    h = h + beyond * np.cos(alpha)
    z = z + beyond * np.sin(alpha)
    return around(th, h, z)


k.add("sepals", sheet(sepal_at, 180, 26, 0.016), GREEN, "sepals", fuse=False)
k.add("sepalheart", ell((0, 0, 0.545), (0.05, 0.047, 0.02)), GREEN, "sepals", fuse=False)

# --- the curl: Cherubi's stem grown into a pink comma, its round head on the left, its tail hooked over to the right ---
BALL = np.array([-0.11, 0.0, 0.565])
def lift(pts):          # the curl drawn on the old, lower top: raised onto the bud's top and kept a little shorter
    return [(x, y, 0.53 + (z - 0.48) * 0.9) for x, y, z in pts]


BALL = np.array(lift([tuple(BALL)])[0])
k.add("curlball", ell(BALL, (0.07, 0.062, 0.066)), PINK, "sepals", fuse=False)
k.add("curl", tube(lift([(-0.11, 0.0, 0.57), (-0.085, 0.0, 0.615), (-0.04, 0.0, 0.638), (0.005, 0.0, 0.626),
                         (0.038, 0.0, 0.592), (0.046, 0.0, 0.553), (0.034, 0.0, 0.518), (0.016, 0.0, 0.497)]),
                   [0.062, 0.053, 0.044, 0.036, 0.029, 0.02, 0.012, 0.003], seg=24), PINK, "sepals", fuse=False)
k.add("stem", tube(lift([(-0.02, 0.0, 0.48), (-0.06, 0.0, 0.5), (-0.1, 0.0, 0.53)]), [0.026, 0.03, 0.04]), PINK,
      "sepals", fuse=False)

# --- the pink body under the cloak, and its feet ---------------------------------------------------------------------
FC = np.array([0, -0.055, 0.12])
FR = np.array([0.15, 0.13, 0.11])
k.add("face", ell(FC, FR), PINK, "body")
k.add("foot", ell((0.065, -0.07, 0.03), (0.045, 0.055, 0.032)), FEET, "leg{s}", mirror=True)

# Cherubi's eyes grown: dark plum with a crimson lower half and a glint, set apart, peeking out under the hem
CP, CPL, CCR = srgb(PINK), srgb(PLUM), srgb(CRIMSON)


def eye(u, v):
    c = np.tile(CPL, (len(u), 1))
    c[(u / 0.62) ** 2 + ((v + 0.25) / 0.55) ** 2 < 1] = CCR
    c[((u - 0.2) / 0.34) ** 2 + ((v - 0.4) / 0.3) ** 2 < 1] = (1, 1, 1)
    return c


de = unit(0.42, -0.9, -0.08)
E = FC + de / np.linalg.norm(de / FR)
E = E + np.array([0, 0, -0.035])


def eyes(state):
    k.decal(E, de, (0.02, 0.026), eye_look(state, eye, CP, CPL), eps=0.004, rings=20, segs=56, mirror=True)


k.build(voxel=0.006)
k.eye_states(eyes)
k.export(out)
