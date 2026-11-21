# Oricorio (the Baile Style) sculpted from nothing on the skeleton and clips of the game's own export of it
# (sculptkit.py):
#   blender --background --factory-startup --python sculpts/oricorio.py -- <the export's Oricorio.glb> <out.glb>
# From the references: a little red dancer bird on long thin pink legs; a small red egg of a body on a long curved
# neck; a round red head with a white curl on each side behind the eye; a half-lidded blue-violet eye under a heavy
# red lid; a small hooked pale pink bill. Its wings are the design's largest mass: long sleeves of tiered ruffles, each
# tier red with a black scalloped hem, ending in a fan of red feathers tipped black; the tail is a shorter sleeve of
# the same ruffles out behind. The pictures show no crest on the head (the tall plume in them is a raised wing), so
# none is built. Written to be grown from: the other styles can import it (sibling("oricorio")) with other colours.
import os, sys, math
from types import SimpleNamespace
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, sheet, outward, axes, srgb, inside, unit, eye_look, args, sibling

g = sibling("pidgey")           # its helpers: spike, hooked, legs

RED, TIPS, CURL, BILL, LEGC, CLAW = "#DC3238", "#2A2630", "#F6F2F2", "#F2C6CC", "#F2A6B0", "#C47A86"


def frame():
    F = SimpleNamespace()
    F.H = np.array([0, -0.02, 0.246])               # the head's centre, over its joint (0, -0.01, 0.21)
    F.HR = np.array([0.05, 0.05, 0.049])
    F.BODY = ((0, 0.012, 0.136), (0.043, 0.051, 0.048), (12, 0, 0))
    F.NECK = ([(0, 0.004, 0.16), (0, -0.012, 0.184), (0, -0.016, 0.204)], [0.034, 0.028, 0.028])
    F.LEGX, F.LEGTOP, F.ANKLE, F.LEGY = 0.02, 0.105, 0.012, 0.002
    F.LEGR = (0.009, 0.0082)
    F.TOE, F.HEEL, F.TOER = 0.03, 0.017, 0.0052
    F.BILL = ((0, -0.07, 0.24), (0.0145, 0.022, 0.013), 8)
    F.EYEDIR = unit(0.6, -0.74, 0.14)
    F.EYE = (0.017, 0.017)
    F.CURLDIR = unit(0.9, 0.12, 0.3)
    F.CURL = dict(r0=0.0035, step=0.012, thick=0.004, lift=-0.0012)
    F.VOXEL = 0.0028
    F.SCALLOPS, F.HEMFROM = 5, 0.7
    # a sleeve: root, direction, tiers as (start, length, radius at start, flare), its fan at the end. The wings are
    # held forward and down before the body like skirts held out: the fans' tips at x 0.07, well before the belly
    F.WING = dict(root=np.array([0.034, -0.008, 0.168]), d=unit(0.25, -0.79, -0.59), core=0.017, flat=0.6,
                  tiers=[(0.0, 0.046, 0.02, 0.024), (0.031, 0.046, 0.029, 0.024), (0.062, 0.044, 0.037, 0.021)],
                  fan=dict(at=0.104, n=5, spread=70, length=0.046, width=0.032, roll=0))
    F.TAIL = dict(root=np.array([0.0, 0.04, 0.142]), d=unit(0.4, 0.72, 0.56), core=0.012, flat=0.6,
                  tiers=[(0.0, 0.045, 0.016, 0.016), (0.03, 0.045, 0.022, 0.016), (0.06, 0.044, 0.026, 0.014)],
                  fan=dict(at=0.11, n=5, spread=80, length=0.052, width=0.024, roll=0))
    return F


def on_head(F, d):
    return F.H + d / np.linalg.norm(d / F.HR)


# --- the parts ----------------------------------------------------------------------------------------------------
def body(k, F, colour=RED):
    c, r, rot = F.BODY
    k.add("body", ell(c, r, rot=rot), colour, "body")
    pts, radii = F.NECK
    k.add("neck", tube(pts, radii), colour, "head")
    k.add("head", ell(F.H, F.HR), colour, "head")


def bill(k, F, colour=BILL):
    c, r, tilt = F.BILL
    k.add("bill", ell(c, r, rot=(tilt, 0, 0), deform=g.hooked), colour, "head", fuse=False, surface="shell")


def curls(k, F, colour=CURL, turns=1.3):
    """A white curl on the side of the head behind the eye: a tube along a spiral laid on the head (mirror)."""
    n = F.CURLDIR
    r0, step, thick, lift = F.CURL["r0"], F.CURL["step"], F.CURL["thick"], F.CURL["lift"]
    c = on_head(F, n)
    a, b = axes(n)
    th = np.linspace(0, 2 * math.pi * turns, 90)
    r = r0 + step * th / (2 * math.pi)
    pts = []
    for t, rr in zip(th, r):
        q = c + rr * (math.cos(t) * a + math.sin(t) * b)
        d = q - F.H
        pts.append(F.H + d / np.linalg.norm(d / F.HR) + n * lift)
    k.add("curl", tube(pts[::3], [thick] * len(pts[::3]), seg=12, per=4), colour, "head", fuse=False, mirror=True)


def scallop(u, n=7):
    """A hem of n round scallops round the tier: 0 between them, 1 at their middles."""
    return np.abs(np.sin(math.pi * n * u))


def sleeve(k, F, S, bone, name, colour=RED, hem=TIPS, mirror=True):
    """
    A sleeve of ruffles: a fused core along d, then tiers, each a cone flaring toward the end, red, its hem black and
    scalloped (a second cone a hair over the first's last third); at the end a fan of red feathers tipped black.
    """
    root, d = S["root"], S["d"]
    a, b = axes(d)
    flat = S.get("flat", 1.0)
    k.add(f"{name}core", tube([root, root + d * 0.06, root + d * S["fan"]["at"]], [S["core"], S["core"] * 0.95,
                                                                                  S["core"] * 0.8]), colour, bone,
          mirror=mirror)
    behind = np.array([0, 1.0, 0])
    phase = math.atan2(behind @ b, behind @ a)
    for i, (s0, ln, r0, fl) in enumerate(S["tiers"]):
        def at(u, v, s0=s0, ln=ln, r0=r0, fl=fl, grow=1.0, v0=0.0):
            t = v0 + (1 - v0) * v
            reach = ln * (1 + 0.22 * scallop(u, F.SCALLOPS)) * t
            r = (r0 + fl * t * t) * grow
            ph = phase + 2 * math.pi * (0.005 + 0.99 * u)
            return (root[None, None] + d[None, None] * (s0 + reach)[..., None]
                    + r[..., None] * (np.cos(ph)[..., None] * a[None, None]
                                      + flat * np.sin(ph)[..., None] * b[None, None]))
        k.add(f"{name}tier{i}", outward(sheet(at, 72, 6, 0.004)), colour, bone, fuse=False, mirror=mirror)
        hemat = lambda u, v, at=at: at(u, v, grow=1.05, v0=F.HEMFROM)
        k.add(f"{name}hem{i}", outward(sheet(hemat, 72, 4, 0.0042)), hem, bone, fuse=False, mirror=mirror)
    fan = S["fan"]
    end = root + d * fan["at"]
    # the end closed by a flat red paddle, so no sleeve shows its hollow
    s0, ln, r0, fl = S["tiers"][-1]
    pr = (r0 + fl) * 0.95
    m = np.stack([a, b, d], axis=1)
    V, Fc = ell((0, 0, 0), (pr, pr * flat, ln * 0.55))
    k.add(f"{name}paddle", (V @ m.T + root + d * (s0 + ln * 0.9), Fc), colour, bone, fuse=False, mirror=mirror)
    roll = math.radians(fan["roll"])
    side0 = a * math.cos(roll) + b * math.sin(roll)
    for j in range(fan["n"]):
        ang = math.radians(-fan["spread"] / 2 + fan["spread"] * j / (fan["n"] - 1))
        fd = d * math.cos(ang) + side0 * math.sin(ang)
        ez = np.cross(np.cross(side0, d), fd)
        ez /= np.linalg.norm(ez)
        ln, w = fan["length"] * (1 - 0.12 * abs(ang) / math.radians(fan["spread"] / 2)), fan["width"]
        half = [(0.0, w * 0.3), (ln * 0.3, w * 0.48), (ln * 0.7, w * 0.5), (ln * 0.92, w * 0.36), (ln, 0.0)]
        out_ = half + [(x, -h) for x, h in half[::-1][1:-1]]
        k.add(f"{name}fan{j}", slab(out_, end, fd, ez, 0.006), hem, bone, fuse=False, mirror=mirror)
        cut = ln * 0.62
        inner = [(x, h * 0.97) for x, h in half if x < cut] + [(cut, w * 0.49)]
        inner = inner + [(x, -h) for x, h in inner[::-1]]
        k.add(f"{name}fanin{j}", slab(inner, end, fd, ez, 0.0105), colour, bone, fuse=False, mirror=mirror)


def legs(k, F, foot=LEGC, claw=CLAW):
    g.legs(k, F, foot=foot, claw=claw)


# --- the eyes -----------------------------------------------------------------------------------------------------
INK, IRIS, GLINT = srgb("#16141C"), srgb("#4A48B4"), np.array([1.0, 1.0, 1.0])


def eye_open(u, v, lid=RED):
    """Half lidded: a blue-violet iris round a dark pupil, the red lid down over its top half with a dark edge."""
    r = np.hypot(u, v)
    c = np.tile(IRIS, (len(u), 1))
    c[np.hypot(u + 0.05, v + 0.12) < 0.42] = INK
    c[np.hypot(u + 0.3, v + 0.36) < 0.13] = GLINT
    c[r > 0.82] = INK
    edge = 0.14 - 0.1 * u
    c[v > edge] = srgb(lid)
    c[np.abs(v - edge) < 0.16] = INK
    return c


def eyes(k, F, lid=RED):
    LID = srgb(lid)
    E = on_head(F, F.EYEDIR)
    m = np.array([-1, 1, 1.0])
    def lay(state):
        look = eye_look(state, eye_open, LID, INK, line=0.17)
        kw = dict(eps=0.003, rings=20, segs=56)
        k.decal(E, F.EYEDIR, F.EYE, look, **kw)
        k.decal(E * m, F.EYEDIR * m, F.EYE, lambda u, v: look(-u, v), **kw)
    return lay


def oricorio(src, out_path, colour=RED, hem=TIPS):
    F = frame()
    k = Kit(src, coat="fur")
    body(k, F, colour)
    bill(k, F)
    curls(k, F)
    sleeve(k, F, F.WING, "wing{s}", "wing", colour, hem)
    sleeve(k, F, F.TAIL, "body", "tail", colour, hem, mirror=False)
    legs(k, F)
    k.build(voxel=F.VOXEL, band=0.004, wband=0.03)
    k.eye_states(eyes(k, F, colour))
    k.export(out_path)


if __name__ == "__main__":
    oricorio(*args())
