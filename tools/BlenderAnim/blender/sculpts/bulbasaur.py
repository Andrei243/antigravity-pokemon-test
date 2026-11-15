# Bulbasaur sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/bulbasaur.py -- <the export's Bulbasaur.glb> <out.glb>
#
# The design, from the references: a squat teal toad on four short legs, three white claws on each foot, dark teal
# patches with corners on its brow, back and legs; a broad flat head wider than the body, small pointed ears at its top
# corners, a wide open grin (pink inside, two small fangs under the upper lip), red eyes slanting up to their outer
# corners under a heavy dark top line, the iris toward the nose, a white catch-light; on its back a green bulb of
# cloves, as wide as the head, rising to a point leaning back with a tuft of small leaves.
#
# Written to be grown from (ivysaur.py, venusaur.py import it with sibling("bulbasaur")): every part is a function
# taking the kit and its sizes, and the build is under __main__.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, hull, srgb, inside, smoothstep, unit, boxy, eye_look, polygon, args, \
    outward, leaf

# --- the line's colours (Ivysaur and Venusaur pass their own) --------------------------------------------------------
TEAL, SPOT, INK, CLAW = "#7CCFB4", "#2E7764", "#1E2A28", "#F2F0EA"
PINK, MOUTH = "#EC8A92", "#A8344A"
RED, PUPIL, WHITE = "#D42A3C", "#6E0E1C", "#FFFFFF"
BULB, BULB2, LEAFC = "#5CB14C", "#4CA040", "#4A9E3E"
EDGE = 0.0035                                               # a crisp paint's edge: under a voxel


# --- small helpers ------------------------------------------------------------------------------------------------------
def on_ell(c, r, d):
    """The point of an ellipsoid (centre c, radii r) in direction d from its centre."""
    d = np.array(d, float)
    q = d / np.array(r, float)
    return np.array(c, float) + d / np.linalg.norm(q)


def frame(n):
    """A frame on a surface seen along -n: right (u), up (v), out (n)."""
    n = unit(*n)
    up = np.array([0, 0, 1.0]) - n * n[2]
    if np.linalg.norm(up) < 1e-3:
        up = np.array([0, -1.0, 0]) - n * n[1]
    up /= np.linalg.norm(up)
    return np.cross(up, n), up, n


def poly_radius(pts, th):
    """For angles th, the distance from the middle to a star-shaped polygon's edge (pts in u, v)."""
    pts = np.array(pts, float)
    d = np.stack([np.cos(th), np.sin(th)], axis=-1)
    best = np.full(len(th), 9.0)
    for a, b in zip(pts, np.roll(pts, -1, axis=0)):
        e = b - a
        den = d[:, 0] * e[1] - d[:, 1] * e[0]
        ok = np.abs(den) > 1e-9
        den = np.where(ok, den, 1)
        t = (a[0] * e[1] - a[1] * e[0]) / den
        s = (a[0] * d[:, 1] - a[1] * d[:, 0]) / den
        hit = ok & (t > 0) & (s >= -1e-6) & (s <= 1 + 1e-6)
        best = np.where(hit, np.minimum(best, t), best)
    return best


def blob(c, n, half, shape, depth=0.04, edge=EDGE, flip=False):
    """A paint mask: a polygon (u, v in -1..1, times half) laid flat on the surface at c, seen along -n."""
    c = np.array(c, float)
    er, eu, en = frame(n)
    hu, hv = half
    sgn = -1.0 if flip else 1.0
    def mask(P):
        q = P - c
        u = sgn * (q @ er) / hu
        v = (q @ eu) / hv
        w = q @ en
        rho = np.hypot(u, v)
        R = poly_radius(shape, np.arctan2(v, u))
        return inside((rho - R) * min(hu, hv), edge) * inside(np.abs(w) - depth, 0.004)
    return mask


# Shapes of the dark patches: irregular, with corners, as the drawings have them
PATCH = [(-1, 0.1), (-0.6, 0.85), (0.2, 1), (0.9, 0.55), (1, -0.25), (0.5, -0.9), (-0.3, -1), (-0.85, -0.6)]
PATCH2 = [(-1, -0.3), (-0.4, 0.9), (0.6, 1), (1, 0.2), (0.7, -0.8), (-0.2, -1)]
WIDE = [(-1, 0.05), (-0.75, 0.75), (0.1, 1), (0.85, 0.7), (1, -0.1), (0.6, -0.8), (-0.2, -0.95), (-0.8, -0.55)]
TRI = [(-1, 0.85), (1, 0.85), (0.1, -1)]


def patch(k, c, n, half, col, shape=PATCH, mirror=False, depth=0.04):
    k.paint(blob(c, n, half, shape, depth), col)
    if mirror:
        m = np.array([-1, 1, 1.0])
        k.paint(blob(np.array(c) * m, np.array(n) * m, half, shape, depth, flip=True), col)


# --- the body ----------------------------------------------------------------------------------------------------------
def body(k, c, r, col, chest=None):
    """A squat body, and a chest that carries the head."""
    k.add("body", ell(c, r), col, "body")
    if chest:
        k.add("chest", ell(*chest), col, "body")


def claws(k, foot, front, spread, size, col, bone):
    """Three solid white points on a foot's front edge (foot: its middle at the ground; front: its toe's y)."""
    for i, dx in enumerate((-spread, 0.0, spread)):
        x = foot[0] + dx * 1.0
        a = np.array([x, front + 0.012, 0.0])
        s = size
        pts = [a + (-s * 0.55, 0, 0.004), a + (s * 0.55, 0, 0.004), a + (0, 0.004, s * 0.95),
               a + (0, s * 0.3, 0.004), a + (dx * 0.25, -s * 0.85, s * 0.18)]
        k.add(f"claw{bone[:4]}{i}", hull(pts), col, bone, fuse=False, mirror=True, surface="shell")


def legs(k, spec, col, claw_col):
    """
    Four squat legs (spec: dict, see the calls): a fat haunch fused into the body's flank over each joint, tapering
    to a short thick shin that splays outward (by spec["splay"]) to a wide foot, claws on its front.
    """
    x, top, r = spec["x"], spec["top"], spec["r"]
    sp = spec.get("splay", 0.0)
    for name, y, bone in (("leg", spec["front"], "leg{s}"), ("hind", spec["hind"], "hind{s}")):
        k.add(name + "top", ell((x - 0.01, y + 0.01, top), spec["thigh"]), col, bone, mirror=True)
        k.add(name, tube([(x, y, top), (x + sp * 0.6, y - 0.005, (top + spec["ankle"]) / 2),
                          (x + sp, y - 0.005, spec["ankle"])], [r * 1.15, r, r * 0.95]), col, bone, mirror=True)
        fr = spec["foot"]
        foot = (x + sp + 0.005, y - spec["toe"], fr[2])
        k.add(name + "foot", ell(foot, fr, rot=(0, 0, -12)), col, bone, mirror=True)
        claws(k, foot, foot[1] - fr[1], spec["spread"], spec["claw"], claw_col, bone)


def underside(k, belly, rump, col):
    """A round belly hanging low between the legs and a rump the hind haunches are part of."""
    k.add("belly", ell(*belly), col, "body")
    k.add("rump", ell(*rump), col, "body")


def head(k, H, HR, jaw, col):
    """A broad flat head, a jaw under its front."""
    k.add("head", ell(H, HR, deform=boxy(2.4)), col, "head")
    k.add("jaw", ell(*jaw, deform=boxy(2.2)), col, "head")


def ears(k, base, tip, r, col):
    """Small pointed ears at the head's top corners, flat front to back."""
    k.add("ear", blade([base, (np.array(base) + np.array(tip)) / 2, tip], [r, r * 0.6, 0.008], (0, 1, 0), 0.5), col,
          "ear{s}", mirror=True)


def ear_lining(k, base, tip, r, col, n=(0.3, -1, 0.1)):
    """The inside of an ear: a narrower point painted on its front face."""
    base, tip = np.array(base, float), np.array(tip, float)
    d = tip - base
    L = np.linalg.norm(d); d /= L
    def mask(P, sx):
        q = P - base * np.array([sx, 1, 1])
        dd = d * np.array([sx, 1, 1])
        t = q @ dd
        across = q - np.outer(t, dd)
        rad = np.hypot(across[:, 0], across[:, 2])               # across the ear's face, not through it
        w = r * 0.62 * (1 - np.clip(t / (L * 0.92), 0, 1))
        front = P[:, 1] < base[1] - 0.004
        on_ear = np.abs(across[:, 1]) < r * 0.6                   # the ear's own face, not the head round it
        return inside(rad - w, EDGE) * (t > L * 0.4) * front * on_ear * (np.sign(P[:, 0]) == sx)
    k.paint(lambda P: np.clip(mask(P, 1) + mask(P, -1), 0, 1), col)


def mouth(k, H, R, z, W, D, cols, fangs=None, front=0.25, opened=True, rise=0.018, line_w=0.005):
    """
    A grin painted round the head's upright axis (s: angle x R, z): the upper lip a line rising a little to its
    corners (by rise), the lower edge a deep curve; pink inside, darker under the lip, a dark line along the lip; fangs
    (each (s, width, length)) white points hanging from the lip. A shut mouth (opened=False) is the line alone, its
    fangs hanging over the lower lip.
    """
    pink, dark, ink, white = (srgb(c) for c in cols)
    def coords(P):
        q = P - np.array(H)
        ang = np.arctan2(q[:, 0], -q[:, 1])
        return ang * R, np.cos(ang)
    def upper(s):
        return z + rise * (s / W) ** 2
    def lower(s):
        return z - D * np.clip(1 - (s / W) ** 2, 0, 1) ** 0.6
    def region(P):
        s, c = coords(P)
        m = inside(np.abs(s) - W, EDGE) * inside(P[:, 2] - upper(s), EDGE) * inside(lower(s) - P[:, 2], EDGE)
        return m * (c > front)
    def shade(P):
        s, c = coords(P)
        return region(P) * inside(P[:, 2] - (upper(s) - D * 0.22), EDGE)
    def line(P):
        s, c = coords(P)
        return inside(np.abs(P[:, 2] - upper(s)) - line_w, EDGE) * inside(np.abs(s) - W * 1.04, EDGE) * (c > front)
    if opened:
        k.paint(region, pink)
        k.paint(shade, dark)
    if fangs:
        def teeth(P):
            s, c = coords(P)
            m = np.zeros(len(P))
            for s0, w, h in fangs:
                for sg in (1, -1):
                    t = (upper(s) - P[:, 2]) / h                    # 0 at the lip, 1 at the point
                    m = np.maximum(m, inside(np.abs(s - sg * s0) - w * (1 - t), EDGE) * (t > -0.2) * (t < 1))
            return m * (region(P) if opened else (c > front))
        k.paint(teeth, white)
    k.paint(line, ink)


def eye_colours(white, ink, shape, top=-0.15):
    """The eye's ground (u outward, v up): white, a heavy dark line along its top, a thin rim all round. The iris, the
    pupil and the catch-light are patches of their own over it (IRIS), so their edges are round, not stepped."""
    W, K = srgb(white), srgb(ink)
    def look(u, v):
        c = np.tile(W, (len(u), 1))
        k_ = np.hypot(u, v) / poly_radius(shape, np.arctan2(v, u))
        c[(k_ > 0.74) & (v > top)] = K
        c[(k_ > 0.9)] = K
        return c
    return look


# The iris's pieces in the eye's u, v: (centre, radii, colour key), each a round patch clipped to the eye's inside
IRIS = [((-0.16, -0.08), (0.8, 0.92), "iris"), ((-0.12, -0.06), (0.32, 0.5), "pupil"),
        ((0.02, 0.24), (0.15, 0.22), "light")]


def clipped(c, r, ok, n=48):
    """An ellipse (centre c, radii r) in the eye's u, v, pulled in to the region ok(u, v) along rays from its centre:
    the outline of a patch (in its own unit coordinates) laid at c with half sizes r."""
    pts = []
    for i in range(n):
        a = 2 * math.pi * i / n
        p = np.array([math.cos(a), math.sin(a)])
        lo, hi = 0.0, 1.0
        if not ok(c[0] + p[0] * r[0], c[1] + p[1] * r[1]):
            for _ in range(24):
                mid = (lo + hi) / 2
                if ok(c[0] + p[0] * r[0] * mid, c[1] + p[1] * r[1] * mid):
                    lo = mid
                else:
                    hi = mid
            hi = lo
        pts.append(p * max(hi, 0.02))
    return pts


def lay_eyes(k, state, centre, d, half, look, lid, ink, shape, cols, iris=IRIS, inner=0.74, fierce=(0.2, 0.5)):
    """
    Two eyes laid by hand: a mirrored patch keeps its outline unmirrored, and this outline is not symmetric. The +X
    eye's u runs outward as laid; the -X eye's runs inward, so its outline and colours are flipped. In the open and
    fierce states the iris's pieces go over the ground (cols: iris, pupil, light), clipped inside the rim (and under the
    fierce lid's line, v = a + b u).
    """
    f = eye_look(state, look, srgb(lid), srgb(ink), line=0.15)
    def ok(u, v):
        if math.hypot(u, v) > inner * poly_radius(shape, np.array([math.atan2(v, u)]))[0]:
            return False
        return state != "Fierce" or v < fierce[0] + fierce[1] * u - 0.16
    for s in (1, -1):
        m = np.array([s, 1, 1.0])
        n = unit(*(np.array(d) * m))
        c0 = np.array(centre) * m
        right = np.cross((0, 0, 1.0), n); right /= np.linalg.norm(right)
        up = np.cross(n, right)
        out_ = right * s                                      # outward on both sides
        outline = shape if s > 0 else [(-a, b) for a, b in shape]
        k.decal(c0, n, half, f if s > 0 else (lambda u, v: f(-u, v)), eps=0.005, rings=20, segs=56,
                outline=polygon(outline))
        if state not in ("Open", "Fierce"):
            continue
        for j, ((cu, cv), (ru, rv), key) in enumerate(iris):
            if state == "Fierce" and key == "light":
                continue                                       # under the lid it would be a speck
            pts = clipped((cu, cv), (ru, rv), ok)
            if s < 0:
                pts = [(-a, b) for a, b in pts]
            at = c0 + out_ * cu * half[0] + up * cv * half[1]
            k.decal(at, n, (ru * half[0], rv * half[1]), cols[key], eps=0.0058 + 0.0007 * j, rings=8, segs=48,
                    outline=polygon(pts))


# The eye's outline: the outer corner high, the top line falling to a pointed inner corner, round below (u outward)
EYE = [(-1, 0.02), (-0.6, 0.52), (0.2, 0.72), (0.88, 0.64), (1, 0.08), (0.8, -0.6), (0.1, -0.95),
       (-0.6, -0.76), (-0.95, -0.36)]


# --- the plant ---------------------------------------------------------------------------------------------------------
def bulb(k, base, height, radius, lean, cols, bone, n=6, tuft=True):
    """
    A bulb of n cloves: each a fat tube up a meridian from the base to the point, overlapping its neighbours so the
    seams between them are creases; two greens in turn, a core to fill the middle, a tuft of small leaves at the point.
    lean: how far back the point goes.
    """
    base = np.array(base, float)
    prof = [(0.55, 0.0), (0.95, 0.22), (1.0, 0.42), (0.78, 0.66), (0.38, 0.86), (0.05, 1.0)]
    rad = [0.5, 0.66, 0.66, 0.54, 0.32, 0.08]
    def at(rr, zz, a):
        lift = zz * height
        back = lean * (zz ** 1.6)
        return base + np.array([rr * radius * math.sin(a), -rr * radius * math.cos(a) + back, lift])
    for i in range(n):
        a = 2 * math.pi * (i + 0.5) / n
        pts = [at(rr, zz, a) for rr, zz in prof]
        k.add(f"clove{i}", tube(pts, [radius * q for q in rad], seg=20, per=6), cols[i % 2], bone, fuse=False,
              surface="leaf")
    k.add("bulbcore", ell(base + np.array([0, lean * 0.35, height * 0.42]), (radius * 0.86, radius * 0.86,
                                                                             height * 0.47)),
          cols[1], bone, fuse=False, surface="leaf")
    top = at(0.0, 1.0, 0)
    if tuft:
        for i, (dx, dy) in enumerate(((0.0, 0.5), (0.55, 0.2), (-0.55, 0.2))):
            d = unit(dx, dy, 1.0)
            k.add(f"tuft{i}", outward(leaf(top - d * 0.01, d, radius * 0.42, radius * 0.16, 0.012)), cols[0], bone,
                  fuse=False, surface="leaf")
    return top


# --- Bulbasaur ---------------------------------------------------------------------------------------------------------
H = np.array([0, -0.23, 0.37])                         # the head's middle
HR = np.array([0.25, 0.19, 0.165])
JAW = ((0, -0.275, 0.29), (0.215, 0.165, 0.1))
EYE_D = unit(0.6, -0.76, 0.24)
EYE_HALF = (0.06, 0.062)
LEGS = dict(x=0.165, front=-0.13, hind=0.21, top=0.19, ankle=0.05, r=0.085, thigh=(0.125, 0.14, 0.13), splay=0.045,
            foot=(0.095, 0.1, 0.042), toe=0.02, spread=0.035, claw=0.03)


def bulbasaur(src, out):
    k = Kit(src, coat="scales")
    body(k, (0, 0.05, 0.2), (0.21, 0.23, 0.15), TEAL, chest=((0, -0.09, 0.25), (0.175, 0.13, 0.13)))
    legs(k, LEGS, TEAL, CLAW)
    underside(k, ((0, 0.04, 0.16), (0.2, 0.26, 0.12)), ((0, 0.19, 0.17), (0.22, 0.16, 0.13)), TEAL)
    head(k, H, HR, JAW, TEAL)
    ears(k, (0.15, -0.215, 0.47), (0.2, -0.23, 0.585), 0.058, TEAL)
    bulb(k, (0, 0.08, 0.32), 0.31, 0.172, 0.05, (BULB, BULB2), "bulb", n=5)

    # The dark patches: one wide on the brow, a small triangle under it, small ones on the head's sides; on the back,
    # the flanks and the legs
    patch(k, on_ell(H, HR, (0, -0.42, 0.95)), (0, -0.42, 0.95), (0.085, 0.04), SPOT, WIDE)
    patch(k, on_ell(H, HR, (0, -0.85, 0.55)), (0, -0.85, 0.55), (0.024, 0.026), SPOT, TRI)
    patch(k, on_ell(H, HR, (0.62, -0.25, 0.74)), (0.62, -0.25, 0.74), (0.026, 0.022), SPOT, PATCH2, mirror=True)
    BC, BR = (0, 0.05, 0.2), (0.21, 0.23, 0.15)
    for d, h in (((0.85, 0.1, 0.4), (0.045, 0.036)), ((0.7, 0.62, 0.3), (0.034, 0.03)), ((0.75, -0.35, 0.45),
                                                                                          (0.03, 0.026))):
        patch(k, on_ell(BC, BR, d), d, h, SPOT, PATCH, mirror=True)
    patch(k, (0.285, -0.12, 0.17), (1, -0.2, 0.2), (0.028, 0.034), SPOT, PATCH2, mirror=True, depth=0.04)
    patch(k, (0.285, 0.22, 0.18), (1, 0.1, 0.2), (0.028, 0.032), SPOT, PATCH, mirror=True, depth=0.04)

    mouth(k, H, 0.21, 0.292, 0.185, 0.05, (PINK, MOUTH, INK, WHITE), fangs=[(0.085, 0.012, 0.022)])

    k.build(voxel=0.0068, band=0.008)
    centre = on_ell(H, HR, EYE_D)
    cols = dict(iris=RED, pupil=PUPIL, light=WHITE)
    k.eye_states(lambda state: lay_eyes(k, state, centre, EYE_D, EYE_HALF, eye_colours(WHITE, INK, EYE), TEAL, INK,
                                        EYE, cols))
    k.export(out)


if __name__ == "__main__":
    bulbasaur(*args())
