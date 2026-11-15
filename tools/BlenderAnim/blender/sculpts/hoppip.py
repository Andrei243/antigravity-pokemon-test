# Hoppip sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/hoppip.py -- <the export's Hoppip.glb> <out.glb>
#
# From the references: a round pink body, a little squared and wider than tall, with two small pointed cat's ears at
# its top corners lined dark; small round yellow eyes with a dark rim, set apart; a big open mouth below them, flat on
# top and round below, dark red with a pink tongue; tiny round stubs of arms low on its sides; two short legs with
# round feet; a little stub of a tail behind; and out of its crown two long green leaves on short stalks, spread in a
# V, their edges stepped in a zigzag of four teeth to a point.
#
# Written to be grown from (skiploom.py and jumpluff.py import it with sibling("hoppip")): the line's eyes, its open
# mouth, its round body, its stubs and its feet are the functions below; the calls that build Hoppip are under
# __main__.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, hull, srgb, inside, unit, boxy, eye_look, args, outward

# --- the line's colours -------------------------------------------------------------------------------------------
PINK = "#F49AB8"
LINING = "#3A2634"                     # inside the ears
LEAF, STALK = "#74BE48", "#5EA83C"
YELLOW, RIM = srgb("#F6D63A"), srgb("#4A2632")
MOUTH, TONGUE = "#8E2236", "#F08A98"
INK = srgb("#5A2A3C")                   # a shut eye's line
WHITE = np.array([1.0, 1.0, 1.0])

# --- Hoppip's frame (joints: body (0, 0, 0.164), ears (+-0.07, 0, 0.234), leaf (0, 0, 0.274),
#     legs (+-0.05, -0.02, 0.074)) ------------------------------------------------------------------------------------
C = np.array([0.0, 0.0, 0.16])         # the body's middle
R = np.array([0.13, 0.114, 0.114])     # its radii
EAR_BASE = np.array([0.074, 0.004, 0.238])
EAR_TIP = np.array([0.128, 0.006, 0.318])
CROWN = np.array([0.0, 0.006, 0.262])   # where the stalks leave the head
EYE_D = (0.39, -0.9, 0.13)              # an eye's direction from the middle (+X side)
EYE_HALF = (0.022, 0.024)
MOUTH_D = (0.0, -1.0, -0.4)
MOUTH_HALF = (0.036, 0.033)


def on_ell(c, r, d):
    """The point of an ellipsoid (centre c, radii r, unturned) along the direction d from its centre, and its normal."""
    d = unit(*d)
    c, r = np.array(c, float), np.array(r, float)
    p = c + d / math.sqrt(((d / r) ** 2).sum())
    n = (p - c) / r ** 2
    return p, n / np.linalg.norm(n)


def round_body(square=2.35, full=0.04):
    """The unit sphere made the line's body: squared a little, a hair fuller below its middle."""
    def f(V):
        V = boxy(square)(V)
        s = 1 + full * np.clip(-V[:, 2], 0, 1)
        V[:, 0] *= s; V[:, 1] *= s
        return V
    return f


def stub(k, name, at, r, color, bone, rot=(0, 0, 0), mirror=True):
    """A small round stub (an arm, a foot), fused."""
    k.add(name, ell(at, r, rot=rot), color, bone, mirror=mirror)


def legs(k, color, top=(0.05, -0.02, 0.08), bottom=(0.052, -0.026, 0.04), radius=0.031,
         foot=(0.052, -0.038, 0.03), foot_r=(0.034, 0.04, 0.026)):
    """Two short legs under the body, each on a round foot."""
    k.add("leg", tube([top, bottom], [radius, radius]), color, "leg{s}", mirror=True)
    stub(k, "foot", foot, foot_r, color, "leg{s}")


def ears(k, color=PINK, lining=LINING, base=EAR_BASE, tip=EAR_TIP, radii=(0.046, 0.035, 0.017, 0.005), inset=0.56):
    """Small pointed cat's ears at the top corners, flattened front to back, their front faces lined dark."""
    pts = [base, base + (tip - base) * 0.4, base + (tip - base) * 0.8, tip]
    k.add("ear", blade(pts, list(radii), (0, 1, 0), 0.5), color, "ear{s}", mirror=True)
    d = unit(*(tip - base))
    L = np.linalg.norm(tip - base)
    across = np.cross(d, (0, 1.0, 0)); across /= np.linalg.norm(across)
    rr = np.array(radii, float)

    def mask(P):
        Q = P.copy(); Q[:, 0] = np.abs(Q[:, 0])
        rel = Q - base
        t = rel @ d / L
        w = rel @ across
        half = inset * np.interp(t, [0, 0.4, 0.8, 1.0], rr) - 0.003
        fr = rel[:, 1] - (tip[1] - base[1]) * t
        return (inside(np.abs(w) - half, 0.0025) * inside(fr + 0.005, 0.002) * inside(0.2 - t, 0.004)
                * inside(t - 0.88, 0.004) * (P[:, 2] > base[2] - 0.01))
    k.paint(mask, lining)


def round_eye(fill=YELLOW, rim=RIM, glint=True, dark=None):
    """The line's round eye: a solid colour, a darker lower part if asked, a dark rim and a white glint (u out, v up)."""
    def look(u, v):
        r = np.sqrt(u * u + v * v)
        c = np.tile(fill, (len(u), 1))
        if dark is not None:
            c[(v < -0.2) & (r < 0.78)] = dark
        if glint:
            c[((u - 0.22) / 0.26) ** 2 + ((v - 0.3) / 0.26) ** 2 < 1] = WHITE
        c[r > 0.76] = rim
        return c
    return look


def lay_eyes(k, c, r, d, half, look, lid, ink, state, eps=0.005):
    """The pair of eyes at the direction d (+X side) from the ellipsoid (c, r), mirrored."""
    p, nrm = on_ell(c, r, d)
    k.decal(p, nrm, half, eye_look(state, look, srgb(lid) if isinstance(lid, str) else lid, ink, line=0.16),
            eps=eps, rings=20, segs=56, mirror=True)


def open_mouth(k, c, r, d, half, inside_col=MOUTH, tongue=TONGUE, top=0.36):
    """An open mouth on the fused face: flat along its top, round below, a tongue in its lower part."""
    p, nrm = on_ell(c, r, d)
    CM, CT = srgb(inside_col), srgb(tongue)
    def look(u, v):
        col = np.tile(CM, (len(u), 1))
        col[(u / 0.66) ** 2 + ((v + 0.66) / 0.52) ** 2 < 1] = CT
        return col
    def shape(th):
        s = math.sin(th)
        return min(1.0, top / s) if s > 0 else 1.0
    k.decal(p, nrm, half, look, eps=0.004, outline=shape, rings=12, segs=48)


def zigzag_leaf(k, name, root, d, n, length, width, color, bone, teeth=4, droop=0.12, thick=0.02, step=0.42):
    """
    A long leaf from root along d, its face toward n, its edges stepped in a zigzag: a chain of flat-faced trapezoids
    narrow at their foot and widening toward the tip, each stepping in again where the next begins, the last a point.
    The midline arches (its tip falls by droop of the length). Loose hulls, the leaf surface.
    """
    d = unit(*d)
    n = np.array(n, float); n = n - d * (n @ d); n /= np.linalg.norm(n)
    root = np.array(root, float)

    def frame(t):
        p = root + d * length * t + np.array([0, 0, -1.0]) * droop * length * t * t
        tan = d * length + np.array([0, 0, -1.0]) * 2 * droop * length * t
        tan /= np.linalg.norm(tan)
        nn = n - tan * (n @ tan); nn /= np.linalg.norm(nn)
        s = np.cross(nn, tan)
        return p, s, nn

    def W(t):                               # the leaf's half-width at the teeth's points
        return width * (0.55 + 0.45 * math.sin(math.pi * min(1.0, 0.25 + 0.8 * t)))

    edges = np.linspace(0.0, 1.0, teeth + 1)
    for i in range(teeth):
        t0, t1 = edges[i], edges[i + 1]
        last = i == teeth - 1
        a = max(0.0, t0 - 0.03)
        pa, sa, na = frame(a)
        pb, sb, nb = frame(t1)
        wa = W(t0) * step if i else width * 0.18
        pts = []
        for p, s, nn, w in ((pa, sa, na, wa), (pb, sb, nb, 0.0 if last else W(t1))):
            for sg in (-1, 1):
                for h in (-1, 1):
                    pts.append(p + s * sg * max(w, 0.002) + nn * h * thick / 2)
        if last:
            # the point: the far end closed to a thin edge, and the widest place a little before it
            pm, sm, nm = frame(t0 + (t1 - t0) * 0.45)
            for sg in (-1, 1):
                for h in (-1, 1):
                    pts.append(pm + sm * sg * W(t0 + 0.1) * 0.92 + nm * h * thick / 2)
        k.add(f"{name}{i}", hull(pts), color, bone, fuse=False, surface="leaf")


def hoppip(src, out):
    k = Kit(src, coat="fur")
    k.add("body", ell(C, R, deform=round_body()), PINK, "body")
    ears(k)
    # tiny round stubs of arms, low on the sides, carried a little forward and down
    stub(k, "arm", (0.12, -0.045, 0.118), (0.032, 0.03, 0.026), PINK, "body", rot=(0, 25, -20))
    legs(k, PINK)
    # a stub of a tail behind, out to +X and turned up at its end
    k.add("tail", tube([(0.04, 0.085, 0.105), (0.08, 0.13, 0.1), (0.108, 0.152, 0.122)], [0.022, 0.019, 0.018]),
          PINK, "body")

    # two stalks out of the crown and the leaves on them, in a V
    for sx in (1, -1):
        d = unit(sx * math.sin(math.radians(48)), 0.16, math.cos(math.radians(48)))
        start = CROWN + np.array([sx * 0.006, 0, -0.03])
        joint = CROWN + d * 0.06
        k.add(f"stalk{'R' if sx > 0 else 'L'}", outward(tube([start, CROWN + d * 0.02, joint], [0.016, 0.015, 0.015])),
              STALK, "leaf", fuse=False, surface="leaf")
        zigzag_leaf(k, f"leaf{'R' if sx > 0 else 'L'}", joint - d * 0.006, d, (0, -1.0, 0.4), 0.24, 0.036,
                    LEAF, "leaf")

    open_mouth(k, C, R, MOUTH_D, MOUTH_HALF)
    k.build(voxel=0.0052, smooth=4, band=0.006)

    def eyes(state):
        lay_eyes(k, C, R, EYE_D, EYE_HALF, round_eye(), PINK, INK, state)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    hoppip(*args())
