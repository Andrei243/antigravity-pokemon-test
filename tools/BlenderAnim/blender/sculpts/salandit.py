# Salandit sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/salandit.py -- <the export's Salandit.glb> <out.glb>
# From the references: a small dark grey salamander on four sprawling legs, its head black and as long as its trunk,
# carried low: a cranium tapering into a long snout that ends in a round bulb, a row of jagged points hanging along
# each side of the lower jaw, a large almond eye of lavender whose top is cut by a heavy black lid (a sleepy look), a
# pair of broad flaps standing up and out at the back of the head. Thin limbs bent out to the sides, five long thin
# fingers splayed on each foot; a long tail rising from the hips and curling over at its tip. An orange-red stripe runs
# up the top of the tail from its tip to the hips, where it opens into four fingers with a round dot at each end.
#
# Written to be grown from (Salazzle imports it with sibling("salandit")): the line's colours, its almond eye cut by
# the lid, its slit, its long fingers, its limbs and its stripe along a tube are the functions below; the calls that
# build Salandit are under __main__.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, slab, srgb, inside, smoothstep, unit, eye_look, args

# --- the line's colours ---------------------------------------------------------------------------------------------
BODY = "#4C4C54"            # the coat
HEAD = "#2A2A30"            # Salandit's black head
STRIPE = "#EE4A1C"          # the orange-red stripe
LAV, VIOLET = srgb("#C8B8F6"), srgb("#8C78D0")      # the eye and its darker edge
SLIT = srgb("#4A3A86")
INK, WHITE = srgb("#121216"), np.array([1.0, 1.0, 1.0])
VOXEL = 0.0019


def on_ell(c, r, d):
    """The point of an ellipsoid (centre c, radii r, unturned) along the direction d from its centre."""
    d = unit(*d)
    return np.array(c, float) + d / math.sqrt(((d / np.array(r, float)) ** 2).sum())


def normal_on(c, r, d):
    p = on_ell(c, r, d) - np.array(c, float)
    n = p / np.array(r, float) ** 2
    return n / np.linalg.norm(n)


# --- the eye ----------------------------------------------------------------------------------------------------------
def lid_cut(top, slope, sag=0.0):
    """An almond's outline cut along the lid: v = top + slope u - sag u^2 (u toward the back of the head, v up)."""
    def cut(th):
        c, s = math.cos(th), math.sin(th)
        # the almond: pointed at both ends
        base = 1.0 / math.sqrt(c * c + (s / (0.62 + 0.38 * abs(c))) ** 2) if abs(s) > 1e-9 else 1.0
        base = min(1.0, base)
        if s <= 1e-6:
            return base
        # where the ray meets the lid line (solve r s = top + slope r c - sag r^2 c^2 by steps)
        r = base
        for _ in range(30):
            if r * s > top + slope * r * c - sag * (r * c) ** 2:
                r *= 0.97
            else:
                break
        return r
    return cut


def eye_colour(cut, rim=0.86, ink=INK):
    """The open eye: lavender, darker violet toward its rim, a thin black rim (u toward the back, v up)."""
    def eye(u, v):
        reach = np.array([max(cut(t), 1e-3) for t in np.arctan2(v, u)])
        rho = np.sqrt(u * u + v * v) / reach
        w = smoothstep(0.45, 0.82, rho)[:, None]
        c = LAV[None] * (1 - w) + VIOLET[None] * w
        c[rho >= rim] = ink
        return c
    return eye


def lay_eyes(k, E, de, half, cut, lid, shut_ink, slit, slit_at, glint=True, rim_ink=INK):
    """The eyes' layer for a state: an almond each side cut by the lid, the slit and the glint patches of their own.
    The left eye is laid by hand (its outline is not symmetric). rim_ink: the eye's thin rim (the lid's colour on a
    black head, where an ink rim would read as goggles)."""
    look = eye_colour(cut, ink=srgb(rim_ink) if isinstance(rim_ink, str) else rim_ink)
    lid = srgb(lid)

    def fierce(u, v):
        c = look(u, v)
        edge = 0.05 + 0.4 * u                     # the lid slanting down toward the nose (u toward the back)
        c[v > edge] = lid
        return c

    def lay(state):
        if state == "Fierce":
            fn = fierce
        elif state == "Open":
            fn = look
        else:
            fn = eye_look(state, look, lid, shut_ink, line=0.17)
        m = np.array([-1, 1, 1.0])
        k.decal(E, de, half, fn, eps=0.0012, rings=20, segs=56, outline=cut)
        k.decal(E * m, de * m, half, lambda u, v: fn(-u, v), eps=0.0012, rings=20, segs=56,
                outline=lambda th: cut(math.pi - th))
        if state in ("Open", "Fierce"):
            right = np.cross((0, 0, 1.0), de); right /= np.linalg.norm(right)
            upv = np.cross(de, right)
            (sw, sh), (sx, sy) = slit, slit_at
            if state == "Fierce":
                sh, sy = sh * 0.6, sy - sh * 0.35
            c = E + right * sx * half[0] + upv * sy * half[1]
            k.decal(c, de, (sw * half[0], sh * half[1]), SLIT, eps=0.0019, rings=6, segs=32, mirror=True)
            if glint and state == "Open":
                g = E + right * (sx - 0.32) * half[0] + upv * (sy + 0.05) * half[1]
                k.decal(g, de, (half[0] * 0.13, half[0] * 0.13), "#FFFFFF", eps=0.0024, rings=4, segs=20,
                        mirror=True)
    return lay


# --- limbs and fingers ----------------------------------------------------------------------------------------------
def fingers(k, name, base, dirs, length, r, bone, color, s):
    """Long thin fingers splayed from base, each a tapering tube along its direction, curling a little down."""
    m = np.array([s, 1, 1.0])
    base = np.array(base, float)
    for j, d in enumerate(dirs):
        d = unit(*d)
        mid = base + d * length * 0.55 + np.array([0, 0, length * 0.05])
        tip = base + d * length + np.array([0, 0, -length * 0.12])
        k.add(f"{name}{j}", tube(np.array([base, mid, tip]) * m, [r * 1.15, r, r * 0.85]), color, bone)


def limb(k, name, pts, radii, bone, color, s):
    m = np.array([s, 1, 1.0])
    k.add(name, tube(np.array(pts) * m, radii), color, bone)


# --- a stripe along the top of a tube -------------------------------------------------------------------------------
def along(pts, n=400):
    """A tube's centre line (the kit's own Catmull-Rom) sampled finely: points and their tangents."""
    pts = np.array(pts, float)
    ext = np.vstack([2 * pts[0] - pts[1], pts, 2 * pts[-1] - pts[-2]])
    P = []
    for i in range(len(pts) - 1):
        p0, p1, p2, p3 = ext[i], ext[i + 1], ext[i + 2], ext[i + 3]
        for t in np.linspace(0, 1, n // (len(pts) - 1), endpoint=False):
            P.append(0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t
                            + (-p0 + 3 * p1 - 3 * p2 + p3) * t ** 3))
    P.append(pts[-1])
    P = np.array(P)
    T = np.gradient(P, axis=0); T /= np.linalg.norm(T, axis=1, keepdims=True)
    return P, T


def stripe_mask(pts, radii, width, t0=0.0, t1=1.0, reach=1.6, all_round=False):
    """Paint along a tube's top (its back, which turns to face the front as the tube rises): within `width` (a share
    of the tube's radius, across) of the line on its surface that faces cross(x, tangent), between shares t0 and t1 of
    its length."""
    P, T = along(pts)
    side = np.array([1.0, 0, 0]) - T * T[:, :1]
    side /= np.linalg.norm(side, axis=1, keepdims=True)
    D = np.cross(side, T)                       # the tube's top
    R = np.interp(np.linspace(0, 1, len(P)), np.linspace(0, 1, len(radii)), radii)
    sh = np.linspace(0, 1, len(P))

    def mask(Q):
        out_ = np.zeros(len(Q))
        for i0 in range(0, len(Q), 20000):
            q = Q[i0:i0 + 20000]
            d2 = ((q[:, None, :] - P[None, ::2, :]) ** 2).sum(2)
            j = np.argmin(d2, axis=1) * 2
            off = q - P[j]
            r = np.linalg.norm(off, axis=1)
            near = r < R[j] * reach
            across = np.abs((off * side[j]).sum(1)) / np.maximum(R[j], 1e-6)
            top = ((off * D[j]).sum(1) > 0) | all_round
            within = (sh[j] >= t0) & (sh[j] <= t1)
            out_[i0:i0 + 20000] = inside(across - width, 0.08) * near * top * within
        return out_
    return mask


# --- Salandit -------------------------------------------------------------------------------------------------------
H = np.array([0.0, -0.12, 0.061])            # the cranium
HR = np.array([0.039, 0.05, 0.03])
TAIL = [(0, 0.075, 0.056), (0.004, 0.125, 0.068), (0.012, 0.16, 0.092), (0.02, 0.178, 0.122),
        (0.02, 0.172, 0.148), (0.012, 0.152, 0.162)]
TAIL_R = [0.024, 0.017, 0.013, 0.0105, 0.0085, 0.0062]


def jag(base, d, length, width, thick):
    """A small jagged point hanging from the jaw: a flat-faced kite at its root to a point along d."""
    d = unit(*d)
    a = np.array([0, 1.0, 0]) - d * d[1]; a /= np.linalg.norm(a)
    b = np.cross(d, a)
    base = np.array(base, float)
    return hull([base + a * width / 2, base - a * width / 2, base + b * thick / 2, base - b * thick / 2,
                 base + d * length])


def salandit(src, out):
    k = Kit(src, coat="scales")

    # The body: a long low trunk, the hips a little higher than the shoulders, the belly low between the legs
    k.add("chest", ell((0, -0.045, 0.05), (0.031, 0.045, 0.025), rot=(-6, 0, 0)), BODY, "body")
    k.add("trunk", ell((0, 0.0, 0.053), (0.032, 0.06, 0.026)), BODY, "body")
    k.add("hips", ell((0, 0.05, 0.057), (0.034, 0.04, 0.027)), BODY, "body")
    k.add("neck", ell((0, -0.085, 0.056), (0.028, 0.03, 0.024)), HEAD, "head")

    # The head: black, a cranium tapering into a long snout that ends in a round bulb, a full jaw under the eyes
    def taper(V):
        V = V.copy()
        t = np.clip(-V[:, 1], 0, 1)
        V[:, 0] *= 1 - 0.2 * t
        V[:, 2] *= 1 - 0.2 * t
        return V
    k.add("head", ell(H, HR), HEAD, "head")
    k.add("snout", ell((0, -0.172, 0.051), (0.025, 0.05, 0.019), deform=taper), HEAD, "head")
    k.add("bulb", ell((0, -0.208, 0.047), (0.022, 0.015, 0.016)), HEAD, "head")
    k.add("jaw", ell((0, -0.135, 0.041), (0.032, 0.055, 0.017)), HEAD, "head")
    # The jagged points hanging along the lower jaw, larger toward the back
    for j, (y, ln) in enumerate(((-0.098, 0.016), (-0.122, 0.014), (-0.146, 0.012), (-0.172, 0.009))):
        k.add(f"jag{j}", jag((0.026 - 0.004 * j, y, 0.034), (0.25, 0.15, -1), ln, 0.012, 0.0065), HEAD, "head",
              fuse=False, mirror=True)
    # The flaps at the back of the head, broad leaves standing up and out
    for s, side in ((1, "R"), (-1, "L")):
        d = unit(0.55 * s, 0.22, 0.8)
        ex = unit(1.0, -0.25 * s, 0.0)
        ex = ex - d * (ex @ d); ex /= np.linalg.norm(ex)
        flap = [(0.0, -0.004), (0.011, 0.006), (0.015, 0.02), (0.011, 0.034), (0.0, 0.044), (-0.011, 0.034),
                (-0.015, 0.02), (-0.011, 0.006)]
        k.add(f"flap{side}", slab(flap, (0.022 * s, -0.098, 0.078), ex, d, 0.0068), HEAD, "head", fuse=False)

    # The legs: thin, bent out to the sides, the feet flat with five long splayed fingers
    for s, side in ((1, "R"), (-1, "L")):
        limb(k, f"arm{side}", [(0.022, -0.06, 0.05), (0.058, -0.072, 0.044), (0.07, -0.088, 0.012)],
             [0.0125, 0.0095, 0.0078], f"leg{side}", BODY, s)
        k.add(f"shoulder{side}", ell(np.array((0.03, -0.058, 0.048)) * (s, 1, 1), (0.018, 0.02, 0.018)), BODY,
              f"leg{side}")
        k.add(f"palm{side}", ell(np.array((0.072, -0.094, 0.007)) * (s, 1, 1), (0.011, 0.011, 0.005)), BODY,
              f"leg{side}")
        fingers(k, f"finger{side}", (0.072, -0.096, 0.007), [(0.75, -0.6, 0), (0.4, -1, 0), (0.0, -1, 0),
                                                              (-0.35, -0.9, 0), (1, 0.1, 0)],
                0.026, 0.0034, f"leg{side}", BODY, s)
        limb(k, f"hind{side}", [(0.024, 0.058, 0.054), (0.06, 0.07, 0.048), (0.072, 0.066, 0.012)],
             [0.0135, 0.01, 0.008], f"hind{side}", BODY, s)
        k.add(f"haunch{side}", ell(np.array((0.032, 0.058, 0.05)) * (s, 1, 1), (0.016, 0.02, 0.017)), BODY,
              f"hind{side}")
        k.add(f"sole{side}", ell(np.array((0.074, 0.064, 0.007)) * (s, 1, 1), (0.011, 0.011, 0.005)), BODY,
              f"hind{side}")
        fingers(k, f"toe{side}", (0.075, 0.064, 0.007), [(1, -0.45, 0), (0.85, 0.15, 0), (0.55, 0.75, 0),
                                                         (0.15, 1, 0), (0.6, -1, 0)],
                0.024, 0.0034, f"hind{side}", BODY, s)

    # The tail: rising from the hips and curling over at its tip
    k.add("tail", tube(TAIL, TAIL_R), BODY, "tail")

    # The stripe: up the tail's top from its tip to the hips, where it opens into four fingers ending in dots
    k.paint(stripe_mask(TAIL, TAIL_R, 0.36, t0=0.04), STRIPE)
    top = 0.084
    root = np.array([0.0, 0.086])
    ends = [((0.01, 0.054), 0.0068), ((-0.01, 0.054), 0.0068), ((0.027, 0.066), 0.0064),
            ((-0.027, 0.066), 0.0064)]

    def hand(P):
        xy = P[:, :2]
        m = inside(np.linalg.norm(xy - root, axis=1) - 0.0085, 0.0015)
        for (ex, ey), rd in ends:
            e = np.array([ex, ey])
            ab = e - root
            t = np.clip(((xy - root) @ ab) / (ab @ ab), 0, 1)
            d = np.linalg.norm(xy - (root + t[:, None] * ab), axis=1)
            m = np.maximum(m, inside(d - 0.0032, 0.0012))
            m = np.maximum(m, inside(np.linalg.norm(xy - e, axis=1) - rd, 0.0012))
        return m * (P[:, 2] > 0.062) * (P[:, 1] > -0.01) * (P[:, 1] < 0.1)
    k.paint(hand, STRIPE)

    # The eyes: large almonds high on the sides of the cranium, their tops cut by the heavy lid
    de = unit(0.82, -0.38, 0.42)
    E = on_ell(H, HR, de) + np.array([0, -0.004, 0.0])
    cut = lid_cut(0.16, 0.14, 0.1)
    lay = lay_eyes(k, E, de, (0.03, 0.027), cut, HEAD, srgb("#9A8CC8"), slit=(0.13, 0.45), slit_at=(-0.34, -0.12),
                   rim_ink=HEAD)

    k.build(voxel=VOXEL, smooth=4, band=0.003, wband=0.018)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    salandit(*args())
