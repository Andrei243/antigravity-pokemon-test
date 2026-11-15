# Articuno sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/articuno.py -- <the export's Articuno.glb> <out.glb>
# A slender light blue bird hovering upright: a pale breast down the whole front, a small head with a crest of three
# darker blue diamond plumes swept back off the forehead, red-brown eyes under a slanting brow, a short grey beak, great
# rounded wings raised in a V and swept back, each a fan of long rounded feathers, a long deep blue ribbon of a tail that
# swings out to the right and round, and short grey legs with small feet. Its wing, ribbon, eye and leg makers are
# shared with Zapdos and Moltres (zapdos.py and moltres.py take them through sibling("articuno")).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, sheet, srgb, inside, smoothstep, unit, outward, sibling, args

sp = sibling("spearow")          # the feather spike, the foot and the eye cut along a brow

BLUE, WING, WING2, PALE = "#78BDEB", "#8ACAF2", "#73B4E4", "#CDE7F7"
CREST, TAIL, STRIPE = "#2C6EBE", "#3A8CD6", "#2765AC"
BEAK, LEG, CLAW = "#A0929C", "#ABA5AA", "#5E585C"
WHITE, IRIS, PUPIL, INK = "#FBFBF8", "#B2362E", "#1A1012", "#1E3E68"

H = np.array([0, -0.07, 0.852])          # the head's centre, over the head's joint (0, -0.04, 0.798)
HR = np.array([0.074, 0.08, 0.076])


# --- shared makers ------------------------------------------------------------------------------------------------------
def wing_axes(elev, sweep, chord):
    """The +X wing's span (raised elev degrees, swept sweep back) and its chord, square to it, toward `chord`."""
    e, s = math.radians(elev), math.radians(sweep)
    d = np.array([math.cos(e) * math.cos(s), math.cos(e) * math.sin(s), math.sin(e)])
    c = np.array(chord, float)
    c = c - d * (d @ c)
    return d, c / np.linalg.norm(c)


def feather_outline(a0, b0, ang, length, width, seg=9):
    """A long feather in the wing's plane from (a0, b0), turned ang degrees from the chord toward the span, its end round."""
    t = math.radians(ang)
    d = np.array([math.sin(t), math.cos(t)])
    s = np.array([d[1], -d[0]])
    root = np.array([a0, b0])
    end = root + d * (length - width * 0.5)
    pts = [root + s * width * 0.42]
    for th in np.linspace(0, math.pi, seg):
        pts.append(end + s * width * 0.5 * math.cos(th) + d * width * 0.55 * math.sin(th))
    pts.append(root - s * width * 0.42)
    return [tuple(p) for p in pts]


def fan_wing(k, root, axes, span, feathers, arm, colours, thick=0.024, bone="wing{s}", shoulder=0.05):
    """
    A wing of loose plates: an arm plate along the span, and long feathers fanned from it (a0 along the span, the
    angle off the chord, length, width each), alternately a hair off the plane so neighbours share no face.
    """
    ex, ez = axes
    n = np.cross(ex, ez)
    k.add("wingarm", slab(arm, root, ex, ez, thick + 0.006), colours[0], bone, fuse=False, mirror=True)
    for j, (a0, ang, L, w) in enumerate(feathers):
        off = root + n * (0.004 if j % 2 else -0.004)
        k.add(f"feather{j}", slab(feather_outline(a0, 0.02, ang, L, w), off, ex, ez, thick),
              colours[j % len(colours)], bone, fuse=False, mirror=True)
    k.add("shoulder", ell(root + ex * 0.025, (shoulder, shoulder * 1.15, shoulder)), colours[0], bone, mirror=True)


def spline(pts, n):
    """n points along a Catmull-Rom curve through pts."""
    p = np.array(pts, float)
    e = np.vstack([2 * p[0] - p[1], p, 2 * p[-1] - p[-2]])
    out_ = []
    for t in np.linspace(0, len(p) - 1, n):
        i = min(int(t), len(p) - 2); f = t - i
        p0, p1, p2, p3 = e[i], e[i + 1], e[i + 2], e[i + 3]
        out_.append(0.5 * ((2 * p1) + (-p0 + p2) * f + (2 * p0 - 5 * p1 + 4 * p2 - p3) * f * f
                           + (-p0 + 3 * p1 - 3 * p2 + p3) * f ** 3))
    return np.array(out_)


def ribbon(path, width, thick, up=(0, 0, 1), twist=None, nu=80, nv=5, u0=0.0, u1=1.0):
    """A thick ribbon along a curve: width(u) across it, laid toward `up` (turned twist(u) radians about the curve)."""
    C = spline(path, 400)
    T = np.gradient(C, axis=0); T /= np.linalg.norm(T, axis=1, keepdims=True)
    U = np.array(up, float)
    W = U[None] - T * (T @ U)[:, None]; W /= np.linalg.norm(W, axis=1, keepdims=True)
    S = np.cross(T, W)
    grid = np.linspace(0, 1, 400)
    def at(u, v):
        uu = u0 + (u1 - u0) * (0.015 + 0.97 * u)
        idx = np.clip((uu * 399).astype(int), 0, 399)
        c, w, s = C[idx], W[idx], S[idx]
        if twist is not None:
            a = twist(uu)[..., None]
            w = w * np.cos(a) + s * np.sin(a)
        return c + w * ((v - 0.5) * width(uu))[..., None]
    return outward(sheet(at, nu, nv, thick))


def eye_at(head, radii, de):
    """The point on the head's ellipsoid along de."""
    de = np.array(de, float)
    return head + de / np.linalg.norm(de / radii)


def red_eye(white, iris, pupil):
    """A white eye, a large red-brown iris toward the beak, a black pupil and a glint."""
    def look(u, v):
        c = np.tile(white, (len(u), 1))
        c[((u + 0.12) / 0.62) ** 2 + ((v + 0.08) / 0.7) ** 2 < 1] = iris
        c[((u + 0.18) / 0.3) ** 2 + ((v + 0.1) / 0.36) ** 2 < 1] = pupil
        c[((u + 0.0) / 0.13) ** 2 + ((v - 0.12) / 0.13) ** 2 < 1] = white
        return c
    return look


def legs(k, x, top, base, length, r, leg=LEG, claw=CLAW, thigh=None, thigh_r=0.045, colour=BLUE):
    """Short legs under fluffy thighs (fused), small feet with dark claws."""
    if thigh is not None:
        k.add("thigh", ell(thigh, (thigh_r, thigh_r * 1.1, thigh_r * 1.1)), colour, "leg{s}", mirror=True)
    for side in ((1, "R"), (-1, "L")):
        sp.foot(k, side, top, base, length, leg, claw, r=r)


# --- Articuno ---------------------------------------------------------------------------------------------------------
ELEV, SWEEP = 62, 26
SPAN = 0.33
ARM = [(0.0, -0.03), (SPAN * 0.5, -0.036), (SPAN - 0.01, -0.026), (SPAN + 0.02, 0.01), (SPAN * 0.75, 0.07),
       (0.12, 0.085), (0.0, 0.06)]
FEATHERS = [(a, ang, L, 0.06) for a, ang, L in zip(np.linspace(0.04, SPAN, 9), np.linspace(4, 62, 9),
                                                   np.linspace(0.15, 0.25, 9))]


def articuno(src, out):
    k = Kit(src, coat="fur")

    # A slender upright trunk, the breast full in front, and a short thick neck to the small head
    k.add("body", ell((0, 0.0, 0.63), (0.112, 0.115, 0.155), rot=(-6, 0, 0)), BLUE, "body")
    k.add("breast", ell((0, -0.04, 0.62), (0.098, 0.085, 0.135)), BLUE, "body")
    k.add("neck", tube([(0, -0.02, 0.72), (0, -0.045, 0.79), H + (0, 0.01, -0.03)], [0.07, 0.06, 0.058]), BLUE, "head")
    k.add("head", ell(H, HR), BLUE, "head")
    # the pale breast down the whole front, from under the beak to the belly
    k.paint(lambda P: inside((P[:, 0] / 0.082) ** 2 + ((P[:, 2] - 0.63) / 0.18) ** 2 - 1, 0.04)
            * inside(P[:, 1] + 0.01, 0.03) * (P[:, 2] < 0.81), PALE, on=["body", "breast", "neck"])

    # The crest: three darker blue diamond plumes swept back off the forehead, the middle one longest
    base = H + (0, -0.035, 0.055)
    for name, d, L, w in (("crestM", unit(0, 0.62, 0.78), 0.13, 0.05), ("crestR", unit(0.8, 0.45, 0.45), 0.1, 0.042),
                          ("crestL", unit(-0.8, 0.45, 0.45), 0.1, 0.042)):
        k.add(name, sp.spike(base + d * 0.005, base + d * L, w, 0.016, up=(0, -1, 0)), CREST, "head", fuse=False)

    # A short grey beak, hooked a little down
    B = H + (0, -0.06, -0.012)
    k.add("beak", tube([B, B + (0, -0.04, -0.008), B + (0, -0.066, -0.026)], [0.022, 0.013, 0.003], seg=20), BEAK,
          "head", fuse=False, surface="shell")

    # The great rounded wings, raised in a V and swept back: an arm and a fan of long round-ended feathers
    fan_wing(k, np.array([0.08, 0.03, 0.74]), wing_axes(ELEV, SWEEP, (0.45, 0.5, -0.75)), SPAN, FEATHERS, ARM,
             (WING, WING2), shoulder=0.034)

    # The long ribbon of a tail: back from the rump, swinging out to the right (+X) and round, its face to the front
    # three-quarter; a deeper stripe down its middle, thicker so it shows on both faces
    path = [(0, 0.06, 0.56), (0.05, 0.17, 0.52), (0.16, 0.26, 0.46), (0.28, 0.27, 0.39), (0.35, 0.18, 0.34),
            (0.36, 0.05, 0.31)]
    width = lambda u: (0.12 - 0.04 * u) * (1 - 0.6 * smoothstep(0.85, 1.0, u))
    k.add("tail", ribbon(path, width, 0.02, twist=lambda u: 0.5 * u), TAIL, "tail", fuse=False)
    k.add("tailstripe", ribbon(path, lambda u: 0.032 - 0.01 * u, 0.028, twist=lambda u: 0.5 * u, u0=0.06), STRIPE,
          "tail", fuse=False)
    k.add("rump", ell((0, 0.07, 0.56), (0.06, 0.05, 0.05)), BLUE, "tail")

    # Short grey legs under fluffy blue thighs
    legs(k, 0.05, (0.05, -0.005, 0.5), (0.055, -0.02, 0.4), 0.045, 0.012, thigh=(0.052, -0.01, 0.51))

    # Eyes: red-brown under a brow slanting down toward the beak
    de = unit(0.55, -0.8, 0.16)
    E = eye_at(H, HR, de)
    look = red_eye(srgb(WHITE), srgb(IRIS), srgb(PUPIL))
    def eyes(state):
        sp.lay_eyes(k, state, E, de, (0.03, 0.028), look, lambda u: 0.55 + 0.2 * u, lambda u: 0.1 + 0.5 * u,
                    srgb(BLUE), srgb(INK))

    k.build(voxel=0.0068)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    articuno(*args())
