# Vanillite sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/vanillite.py -- <the export's Vanillite.glb> <out.glb>
#
# The design, from the references: a scoop of soft white snow, a round ball wider than it is tall, its face on its
# front; on it a puffed tier of snow like a cushion, and on that a pointed tip curling over to the right (+X). Big
# upright oval eyes, pale blue with a large dark blue pupil and a dark rim; a dark blue mouth, flat along the top and
# round below; a small blue diamond of ice on each cheek outside the eyes. Under the ball a frill of snow over a short
# faceted cone of blue ice, with a small pale wing of ice to each side of the cone's top. (The art's puffs of cold air
# float apart from it and are left out, as the 3D model leaves them out.)
# The skeleton: body (0, 0, 0.206), head (0, 0, 0.266), tail (0, 0, 0.146). The ball is the body's, the tier and the tip
# the head's, the cone and its wings the tail's.
# Vanillish's and Vanilluxe's scripts are grown from this one: they import its colours, its eye, its mouth, its
# diamonds, its swirl and its cone through sibling("vanillite").
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, eye_look, polygon, hull, slab, axes, args

# --- the line's colours -------------------------------------------------------------------------------------------------
SNOW, SHADE = "#F3F5FA", "#D2DEEE"          # the snow, and its bluish shade underneath
ICE, ICEDEEP, ICEPALE = "#8ACBF0", "#62B0E4", "#C2E6F8"
NAVY, EYEFILL = "#2C5598", "#A6D0F0"         # the eyes' rim, pupil and mouth; the eyes' pale blue
GEM_LIGHT, GEM_DARK = "#C8ECFB", "#5FB8EC"   # the diamonds' two halves

# --- the frame ----------------------------------------------------------------------------------------------------------
C = np.array([0.0, 0.0, 0.2])                 # the ball
R = np.array([0.117, 0.108, 0.103])
TIER_C, TIER_R = np.array([0.0, 0.004, 0.318]), np.array([0.084, 0.08, 0.046])
EYE_X, EYE_Z, EYE_HALF = 0.047, 0.205, (0.025, 0.037)
MOUTH_Z, MOUTH_HALF = 0.152, (0.035, 0.017)
GEM_X, GEM_Z, GEM_HALF = 0.1, 0.208, (0.016, 0.022)
VOXEL = 0.0042


# --- shared shapes ------------------------------------------------------------------------------------------------------
def on_ell(c, r, x=None, z=None, d=None):
    """A point on an upright ellipsoid's front at (x, z) (or along a direction d from its middle) and its normal."""
    c, r = np.array(c, float), np.array(r, float)
    if d is None:
        t = 1 - ((x - c[0]) / r[0]) ** 2 - ((z - c[2]) / r[2]) ** 2
        p = np.array([x, c[1] - r[1] * math.sqrt(max(t, 0.02)), z])
    else:
        d = unit(*d)
        p = c + d / math.sqrt(((d / r) ** 2).sum())
    n = (p - c) / r ** 2
    return p, n / np.linalg.norm(n)


def swirl(k, base, scale, bone, name="swirl", lean=1.0):
    """The soft-serve tip: thick where it leaves the snow, rising and curling over toward +X to a point."""
    b = np.array(base, float)
    s = scale
    pts = [b, b + (0.0, 0.0, 0.024 * s), b + (0.009 * s * lean, 0.0, 0.046 * s), b + (0.028 * s * lean, 0.0, 0.062 * s),
           b + (0.047 * s * lean, 0.0, 0.067 * s)]
    k.add(name, tube(pts, [0.043 * s, 0.035 * s, 0.025 * s, 0.015 * s, 0.009 * s]), SNOW, bone)


def crystal(p0, p1, prof, n=6, turn=0.0, jitter=0.0, seed=0):
    """
    A crystal along p0 -> p1: rings of n sides at shares t of the way, (t, radius) each (a radius of nought a point),
    made a hull with flat faces. jitter moves each corner in or out a little, so the faces catch the light unevenly.
    """
    rs = np.random.RandomState(seed)
    p0, p1 = np.array(p0, float), np.array(p1, float)
    d = p1 - p0
    a, b = axes(d)
    pts = []
    for i, (t, r) in enumerate(prof):
        c = p0 + d * t
        if r <= 0:
            pts.append(c)
            continue
        tw = math.radians(turn) + (i % 2) * math.pi / n * 0.6
        for j in range(n):
            th = 2 * math.pi * j / n + tw
            rr = r * (1 + jitter * (rs.rand() - 0.5))
            pts.append(c + rr * (math.cos(th) * a + math.sin(th) * b))
    return hull(np.array(pts))


def gem(p0, p1, r, n=4, turn=45.0):
    """A small diamond of ice: two pyramids base to base along p0 -> p1."""
    return crystal(p0, p1, [(0, 0), (0.5, r), (1, 0)], n=n, turn=turn)


def eye_open(fill, pupil, rim, pupil_half=(0.44, 0.56), pupil_c=(0.0, -0.06), rim_at=0.81):
    """The open eye: a pale fill, a large dark upright pupil low in it, a rim a fifth of the eye's half size."""
    f, p, m = srgb(fill), srgb(pupil), srgb(rim)
    def look(u, v):
        c = np.tile(f, (len(u), 1))
        c[((u - pupil_c[0]) / pupil_half[0]) ** 2 + ((v - pupil_c[1]) / pupil_half[1]) ** 2 < 1] = p
        c[u * u + v * v > rim_at ** 2] = m
        return c
    return look


def gem_look(light=GEM_LIGHT, dark=GEM_DARK):
    """A diamond's two halves: the upper outer half pale, the lower inner half deep."""
    l_, d_ = srgb(light), srgb(dark)
    def look(u, v):
        c = np.tile(d_, (len(u), 1))
        c[v + 0.35 * u > 0.05] = l_
        return c
    return look


DIAMOND = polygon([(0, 1), (1, 0), (0, -1), (-1, 0)])


def mouth_outline(th):
    """A mouth flat along its top and round below."""
    s_ = math.sin(th)
    return min(1.0, 0.42 / s_) if s_ > 0.05 else 1.0


def lay_eyes(k, state, c, r, x, z, half, look_open, lid, ink, line=0.17, shut=False):
    """A pair of eyes at (+-x, z) on an upright ellipsoid. shut: the open eye is the closed one (a smile's arc)."""
    for side in (1, -1):
        p, n = on_ell(c, r, c[0] + x * side, z)
        if shut:
            st = {"Open": "Smile", "Fierce": "Shut"}.get(state, state)
        else:
            st = state
        look = eye_look(st, look_open, srgb(lid), srgb(ink), line=line)
        if shut and state == "Fierce":
            # the arc tilted down toward the middle, written by hand (eye_look's Fierce draws a lid over an open eye)
            li, ik = srgb(lid), srgb(ink)
            def look(u, v):
                cc = np.tile(li, (len(u), 1))
                cc[(np.abs(u) < 0.82) & (np.abs(v - (-0.05 + 0.38 * u)) < line)] = ik
                return cc
        col = look if side == 1 else (lambda u, v, f=look: f(-u, v))
        k.decal(p - n * 0.004, n, half, col, eps=0.0032, rings=20, segs=56)


def ice_cone(k, name, top, top_r, bottom, bottom_r, bone, n=8, seed=1, color=ICE):
    """A faceted cone of ice from a ring at top down to a ring (or a point) at bottom: a loose crystal."""
    rings = [(0.0, top_r), (0.3, top_r * 0.78 + bottom_r * 0.22), (0.65, top_r * 0.42 + bottom_r * 0.58),
             (1.0, bottom_r)]
    k.add(name, crystal(top, bottom, rings, n=n, jitter=0.16, seed=seed), color, bone, fuse=False, surface="shell")


# --- the parts ----------------------------------------------------------------------------------------------------------
def body(k):
    k.add("ball", ell(C, R, deform=lambda V: V * np.array([1, 1, 1]) * (1 + 0.04 * np.clip(-V[:, 2:3], 0, 1))), SNOW,
          "body")
    k.add("tier", ell(TIER_C, TIER_R), SNOW, "head")
    swirl(k, TIER_C + (0.0, 0.0, 0.032), 1.0, "head")
    # the frill of snow where the ball sits in its cone
    for i in range(10):
        a = 2 * math.pi * i / 10 + 0.3
        k.add(f"frill{i}", ell((0.048 * math.sin(a), -0.044 * math.cos(a), 0.105), (0.022, 0.022, 0.018)), SNOW, "body")
    # the bluish shade under the ball
    k.paint(lambda P: smoothstep(0.135, 0.085, P[:, 2]) * 0.75, SHADE)


def cone(k):
    ice_cone(k, "cone", (0, 0, 0.122), 0.046, (0, 0, 0.038), 0.032, "tail", n=7, seed=3)
    # the small pale wings of ice to each side of its top, turned a little toward the front
    for s in (1, -1):
        ex = unit(s, -0.42, -0.32)          # out, a little forward and down; its face to the front three-quarter
        ez = (0.0, 0.0, 1.0)
        outline = [(0.0, 0.0), (0.02, 0.016), (0.05, 0.02), (0.072, 0.008), (0.068, -0.008), (0.042, -0.016),
                   (0.012, -0.012)]
        k.add("wing" + ("R" if s > 0 else "L"), slab(outline, (0.03 * s, -0.006, 0.112), ex, ez, 0.012), ICEPALE,
              "tail", fuse=False, surface="shell")


def face(k):
    # the mouth, flat on top, round below
    p, n = on_ell(C, R, 0.0, MOUTH_Z)
    k.decal(p - n * 0.004, n, MOUTH_HALF, NAVY, eps=0.0032, outline=mouth_outline, rings=14, segs=72)
    # a diamond on each cheek
    for s in (1, -1):
        p, n = on_ell(C, R, GEM_X * s, GEM_Z)
        look = gem_look()
        k.decal(p - n * 0.004, n, GEM_HALF, look if s > 0 else (lambda u, v, f=look: f(-u, v)), eps=0.0032,
                outline=DIAMOND, rings=10, segs=64)


def vanillite(src, out):
    k = Kit(src, coat="fur")
    body(k)
    cone(k)
    face(k)
    k.build(voxel=VOXEL, smooth=3, band=0.006)
    look = eye_open(EYEFILL, NAVY, NAVY)
    k.eye_states(lambda st: lay_eyes(k, st, C, R, EYE_X, EYE_Z, EYE_HALF, look, SNOW, NAVY))
    k.export(out)


if __name__ == "__main__":
    vanillite(*args())
