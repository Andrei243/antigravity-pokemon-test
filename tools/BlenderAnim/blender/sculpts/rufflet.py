# Rufflet sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/rufflet.py -- <the export's Rufflet.glb> <out.glb>
# From the references: a small dark blue-violet eaglet that is mostly head: a great shaggy ball of cream down, its
# tufts pointing out all round and hanging in points over the shoulders; on its front a dark blue-violet mask with a
# row of points along its top and a point out at each cheek; big round white eyes with a thick black rim and a black
# pupil; a short hooked yellow beak under them; a long white feather standing from the forehead and curving back to its
# right, red at its root; a small round dark body with the wings folded tight round it; a little grey tail feather
# behind; short yellow legs with three toes forward, one back and big black talons.
# Written to be grown from: braviary.py imports it (sibling("rufflet")) for its colours, beak, tufts and feet.
import os, sys, math
from types import SimpleNamespace
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, args, sibling

p = sibling("pidgey")
d_ = sibling("ducklett")

NAVY, CREAM, RED, WHITE, YELLOW, BLACK, GREY = ("#62629C", "#F2E8BE", "#E2392E", "#F6F6F8", "#F1CB5E", "#1C1A20",
                                                "#9EA1B4")
VOXEL = 0.0042
HC = np.array([0, -0.005, 0.258])           # the ball of down's centre, over the head's joint (0, -0.01, 0.18)
HR = np.array([0.122, 0.108, 0.118])
FC = np.array([0, -0.092, 0.258])           # the mask's ellipsoid on the front of the ball
FR = np.array([0.074, 0.036, 0.06])


def fib(n, seed=3):
    """n directions spread evenly over a sphere, a little jittered."""
    rs = np.random.RandomState(seed)
    i = np.arange(n) + 0.5
    phi = np.arccos(1 - 2 * i / n)
    th = math.pi * (1 + 5 ** 0.5) * i + rs.uniform(-0.25, 0.25, n)
    D = np.stack([np.cos(th) * np.sin(phi), np.sin(th) * np.sin(phi), np.cos(phi)], 1)
    return D / np.linalg.norm(D, axis=1, keepdims=True)


def tufts(n, size, amount, skip=None, seed=3):
    """A deform that raises n pointed tufts on a sphere: a shaggy ball of down."""
    D = fib(n, seed)
    if skip is not None:
        D = D[~skip(D)]
    c0 = math.cos(size)
    def f(V):
        m = (V @ D.T).max(1)
        b = np.clip(1 - np.arccos(np.clip(m, -1, 1)) / size, 0, 1) ** 1.15
        return V * (1 + amount * b)[:, None]
    return f


def mask_poly():
    half = [(0.0, 0.074), (0.011, 0.046), (0.025, 0.066), (0.036, 0.04), (0.054, 0.052), (0.058, 0.024),
            (0.084, 0.01), (0.06, -0.012), (0.046, -0.032), (0.022, -0.04), (0.0, -0.04)]
    return p.mirrored(half)


# --- the parts ----------------------------------------------------------------------------------------------------
def body(k):
    k.add("body", ell((0, 0.004, 0.108), (0.07, 0.066, 0.062)), NAVY, "body")
    # the wings folded round the body, their fronts meeting low on the chest
    for s, b in ((1, "wingR"), (-1, "wingL")):
        k.add("wing", ell((0.042 * s, -0.012, 0.102), (0.032, 0.056, 0.052), rot=(12, 0, 22 * s)), NAVY, b)


def head(k):
    front = lambda D: (D[:, 1] < -0.62) & (np.abs(D[:, 2]) < 0.75)
    k.add("ruff", ell(HC, HR, deform=tufts(96, 0.3, 0.26, skip=front), sub=5), CREAM, "head")
    k.add("face", ell(FC, FR), NAVY, "head")
    # the mask's outline: cream painted back outside it
    poly = mask_poly()
    k.paint(lambda P: (1 - p.poly_mask(P[:, 0], P[:, 2] - FC[2], poly, edge=0.0022)) * (P[:, 1] < -0.04), CREAM,
            on=["face"])
    k.paint(lambda P: p.poly_mask(P[:, 0], P[:, 2] - FC[2], poly, edge=0.0022) * (P[:, 1] < -0.06), NAVY)


def feather(k, root=(0, -0.112, 0.305), mid=(0.024, -0.098, 0.385), tip=(0.095, -0.02, 0.44), w=0.052, t=0.014):
    """The long feather from the forehead, curving back to the right, red at its root."""
    root, mid, tip = (np.array(v, float) for v in (root, mid, tip))
    side = np.cross(unit(*(tip - root)), (0, -1.0, 0.25))
    k.add("feather", p.spike(root, root + (mid - root) * 0.62, w * 0.85, t * 1.3, side=side, at=0.55), RED, "head",
          fuse=False)
    k.add("feather", p.spike(root + (mid - root) * 0.3, tip, w * 1.2, t, side=side, at=0.3), WHITE, "head", fuse=False)


def beak(k):
    k.add("beak", ell((0, -0.142, 0.236), (0.025, 0.036, 0.023), rot=(12, 0, 0), deform=p.hooked), YELLOW, "head",
          fuse=False, surface="shell")
    k.add("beaklow", ell((0, -0.128, 0.218), (0.017, 0.022, 0.009), rot=(-6, 0, 0), deform=p.pointed), YELLOW, "head",
          fuse=False, surface="shell")


def tail(k):
    for s in (1, -1):
        base = np.array([0.012 * s, 0.055, 0.11])
        k.add("tail", p.spike(base, base + unit(0.35 * s, 1, 0.45) * 0.085, 0.04, 0.014, side=(1, 0, 0), at=0.5),
              GREY, "tail", fuse=False)


def leg_frame():
    F = SimpleNamespace()
    F.LEGX, F.LEGY, F.LEGTOP, F.ANKLE = 0.036, -0.012, 0.075, 0.019
    F.LEGR = (0.0135, 0.0125)
    F.TOE, F.HEEL, F.TOER = 0.04, 0.022, 0.0095
    return F


# --- the eyes -----------------------------------------------------------------------------------------------------
EYE = (0.025, 0.026)
EYEPOS = np.array([0.037, -0.121, 0.265])
EYEDIR = unit(0.32, -0.94, 0.05)
INKC, WHITEC = srgb(BLACK), np.array([1.0, 1.0, 1.0])


def eye_open(u, v):
    c = np.tile(WHITEC, (len(u), 1))
    c[u * u + v * v > 0.775 ** 2] = INKC
    return c


def eyes(k):
    right = np.cross((0, 0, 1.0), EYEDIR); right /= np.linalg.norm(right)
    up = np.cross(EYEDIR, right)
    LID = srgb(NAVY)
    def lay(state):
        k.decal(EYEPOS, EYEDIR, EYE, eye_look(state, eye_open, LID, INKC, line=0.18), eps=0.0035, rings=20, segs=56,
                mirror=True)
        if state in ("Open", "Fierce"):
            pu, pv, r = (-0.05, 0.0, 0.42) if state == "Open" else (0.0, -0.28, 0.36)
            k.decal(EYEPOS + right * pu * EYE[0] + up * pv * EYE[1], EYEDIR, (r * EYE[0], r * EYE[1]), INKC,
                    eps=0.0045, rings=8, segs=40, mirror=True)
    return lay


def rufflet(src, out):
    k = Kit(src, coat="fur")
    body(k)
    head(k)
    feather(k)
    beak(k)
    tail(k)
    p.legs(k, leg_frame(), foot=YELLOW, claw=BLACK)
    k.build(voxel=VOXEL, band=0.006)
    k.eye_states(eyes(k))
    k.export(out)


if __name__ == "__main__":
    rufflet(*args())
