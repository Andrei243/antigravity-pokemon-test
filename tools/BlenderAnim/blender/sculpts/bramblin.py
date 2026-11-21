# Bramblin sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/bramblin.py -- <the export's Bramblin.glb> <out.glb>
#
# From the references: a round cage of thick dry branches growing up from a dark knot at its foot, each branch
# curving up the ball and ending in a free point near its top (the points of the two halves part at the top); the
# upper half pale tan, the lower half orange, the knot dark brown with a fringe of little dark twigs; a thorn here
# and there out of the sides and the top. Inside the cage, seen between the branches, float two dark diamond eyes
# with a red slit each, their tops leaning in. The cage is open: the background shows through.
#
# Written to be grown from (brambleghast.py imports it): the colours, a branch, a thorn, the knot's twigs and the
# diamond eye with its slit are functions here; the calls that build Bramblin are under __main__.
# Skeleton: body (0, 0, 0.322), head (0, 0, 0.222): the cage on body, the eyes on head.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, lathe, srgb, inside, smoothstep, unit, eye_look, polygon, args

# --- the line's colours ----------------------------------------------------------------------------------------------
TAN, ORANGE, KNOT = "#EDC57A", "#E3884C", "#6E4430"
EYE, SLIT, LINE = "#4A2A22", "#E8583A", "#C2704A"

# --- Bramblin's frame --------------------------------------------------------------------------------------------------
C = np.array([0.0, 0.0, 0.3])            # the cage's middle
R = np.array([0.25, 0.235, 0.255])       # its radii
BR = 0.021                               # a branch's radius at its root
VOXEL = 0.005


def sph(az, el, f=1.0, c=None, r=None):
    """A point of the cage at az degrees round from the front (+ toward +X) and el degrees up, f of the radius out."""
    c = C if c is None else c
    r = R if r is None else r
    a, e = math.radians(az), math.radians(el)
    return c + np.array([math.sin(a) * math.cos(e), -math.cos(a) * math.cos(e), math.sin(e)]) * r * f


def mix(a, b, t):
    return srgb(a) * (1 - t) + srgb(b) * t


def colour_at(z, c=None, r=None, low=None):
    """The cage's colour at a height: tan over the middle, orange under it (for loose pieces)."""
    c = C if c is None else c
    low = ORANGE if low is None else low
    return srgb(TAN) if z > c[2] + 0.01 else srgb(low)


def spike(k, name, root, d, length, radius, colour, bone="body", seg=10):
    """A solid thorn: a cone from a base sunk behind root out along d."""
    d = unit(*d)
    k.add(name, lathe(np.array(root, float), d, [(0, -radius * 0.9), (radius, -radius * 0.3),
                                                  (radius * 0.6, length * 0.45), (0, length)], seg=seg),
          colour, bone, fuse=False, surface="shell")


def branch(k, name, pts, r0, r1, bone="body", colour=TAN, tip=0.06, tip_colour=None):
    """One branch: a fused tube through pts tapering r0 to r1, and a loose point carrying on from its end."""
    pts = np.array(pts, float)
    radii = np.linspace(r0, r1, len(pts))
    k.add(name, tube(pts, radii, seg=14, per=6), colour, bone)
    d = pts[-1] - pts[-2]
    spike(k, name + "tip", pts[-1] - unit(*d) * r1 * 1.2, d, tip + r1 * 0.8, r1 * 0.92,
          srgb(TAN) if tip_colour is None else tip_colour, bone)


def eye_part(k, name, c, half, lean, bone="head", colour=EYE):
    """A dark diamond floating in the cage: an ellipsoid flattened front to back and drawn to points top and bottom."""
    def diamond(V):
        V = V.copy()
        V[:, 0] *= 1 - 0.72 * np.abs(V[:, 2]) ** 1.1
        return V
    k.add(name, ell(c, (half[0], half[0] * 0.55, half[1]), rot=(0, lean, 0), deform=diamond, sub=4), colour, bone)


def slit_open(u, v):
    """The open eye: the dark diamond with a red lens of a slit up its middle."""
    c = np.tile(srgb(EYE), (len(u), 1))
    c[(u / 0.36) ** 2 + (v / 0.78) ** 2 < 1] = srgb(SLIT)
    return c


DIAMOND = [(0.0, 1.0), (0.58, 0.05), (0.42, -0.5), (0.0, -1.0), (-0.42, -0.5), (-0.58, 0.05)]


def lay_eye(k, state, c, n, half, lean_deg, side, eps=0.0022):
    """One eye's patch on the front of its diamond, its top leaning lean_deg toward +X (u outward on both sides)."""
    a = math.radians(lean_deg)
    up = (math.sin(a), 0, math.cos(a))
    look = eye_look(state, slit_open, srgb(EYE), srgb(LINE), line=0.16)
    col = look if side > 0 else (lambda u, v, f=look: f(-u, v))
    k.decal(c, n, half, col, up=up, eps=eps, outline=polygon(DIAMOND), rings=20, segs=56)


# --- Bramblin ---------------------------------------------------------------------------------------------------------
# Each branch: (azimuth at its foot, at its top, elevation it ends at, a wobble's phase)
BRANCHES = [(-6, 8, 52, 0.0), (6, -7, 48, 1.3),
            (48, 36, 72, 2.1), (-48, -36, 70, 0.7),
            (92, 80, 66, 1.7), (-92, -78, 70, 2.6),
            (138, 124, 74, 0.4), (-138, -122, 72, 1.1),
            (180, 168, 66, 2.2)]
EYE_AT = np.array([0.1, -0.115, 0.325])
EYE_HALF = (0.04, 0.068)
EYE_LEAN = 18.0                     # degrees, the tops leaning toward the middle
# Thorns on the branches: (branch, share of the way up, a tilt up, length)
THORNS = [(4, 0.62, 0.15, 0.06), (5, 0.66, -0.05, 0.055), (2, 0.85, 0.5, 0.05), (3, 0.86, 0.6, 0.05),
          (6, 0.75, 0.3, 0.045), (7, 0.45, -0.2, 0.045), (8, 0.7, 0.2, 0.045)]


def cage(k, c, r, branches, br, scale=1.0, bone="body"):
    """The branches round a ball from a knot at its foot; gives back each branch's points."""
    paths = []
    for i, (a0, a1, e1, ph) in enumerate(branches):
        els = np.linspace(-62, e1, 8)
        pts = []
        for j, e in enumerate(els):
            t = j / (len(els) - 1)
            az = a0 + (a1 - a0) * t + (2 if abs(a0) < 20 else 5) * math.sin(3.1 * t + ph)
            f = 1.0 + 0.035 * math.sin(4.3 * t + 2 * ph) - 0.06 * t ** 3
            pts.append(sph(az, e, f, c, r))
        # from the knot under the cage up into the first point
        foot = c + np.array([0, 0, -r[2] * 0.88]) + (pts[0] - c) * np.array([0.35, 0.35, 0])
        pts = [foot] + pts
        branch(k, f"br{i}", pts, br, br * 0.78, bone, tip=0.07 * scale,
               tip_colour=colour_at(pts[-1][2], c))
        paths.append(np.array(pts))
    return paths


def thorns(k, paths, c, specs, radius, bone="body", colour=None):
    """Thorns standing out of branches, from the branch's own middle line outward from the ball's middle."""
    for i, (b, t, dz, L) in enumerate(specs):
        P = paths[b]
        x = t * (len(P) - 1)
        j = min(int(x), len(P) - 2)
        p = P[j] + (P[j + 1] - P[j]) * (x - j)
        n = unit(*(p - c)) + np.array([0, 0, dz])
        spike(k, f"thorn{i}", p, n, L + radius, radius,
              colour if colour is not None else colour_at(p[2], c), bone)


def bramblin(src, out):
    k = Kit(src, coat="shell")
    paths = cage(k, C, R, BRANCHES, BR)
    # the knot at the foot
    knot = C + np.array([0, 0, -R[2] * 0.9])
    k.add("knot", ell(knot, (0.075, 0.07, 0.045)), KNOT, "body")
    for i in range(11):
        a = i * 360 / 11 + 13
        d = np.array([math.sin(math.radians(a)), -math.cos(math.radians(a)), -0.55 - 0.25 * (i % 2)])
        p = knot + np.array([d[0], d[1], 0]) * 0.055 + np.array([0, 0, -0.01])
        spike(k, f"twig{i}", p, d, 0.045 + 0.012 * (i % 3), 0.013, srgb(KNOT))
    # thorns off the sides and the top
    thorns(k, paths, C, THORNS, 0.015)
    # the colours: tan above, orange below, the knot dark
    k.paint(lambda P: inside(P[:, 2] - (C[2] + 0.0), 0.03), ORANGE, on=["br"])
    k.paint(lambda P: inside(P[:, 2] - (C[2] - R[2] * 0.72), 0.02), KNOT, on=["br", "knot"])
    # the eyes, floating in the cage
    for s in (1, -1):
        eye_part(k, "eye" + ("R" if s > 0 else "L"), EYE_AT * np.array([s, 1, 1]), EYE_HALF, -EYE_LEAN * s)
    k.build(voxel=VOXEL, smooth=3, band=0.006)

    def eyes(state):
        for s in (1, -1):
            c = EYE_AT * np.array([s, 1, 1]) + np.array([0, -EYE_HALF[0] * 0.55, 0])
            lay_eye(k, state, c, (0, -1, 0), (EYE_HALF[0] * 0.86, EYE_HALF[1] * 0.9), -EYE_LEAN * s, s)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    bramblin(*args())
