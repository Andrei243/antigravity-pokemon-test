# Chikorita sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/chikorita.py -- <the export's Chikorita.glb> <out.glb>
#
# The design, from the references: a small pale green quadruped, its head great and tall (a rounded bullet as wide as
# the body, the face flat and the muzzle full below), big red eyes with a dark rim, a white glint and a dark red middle,
# a small smile; on its crown a short stalk and one great pointed leaf, longer than the head is tall, rising and
# sweeping back; a necklace of round dark green buds under its chin and pointed buds at the shoulders; four short
# stubby legs on wide feet, two small white claws on each.
#
# Written to be grown from (bayleef.py, meganium.py import it with sibling("chikorita")): every part is a function
# taking the kit and its sizes, and the build is under __main__.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, sheet, srgb, inside, smoothstep, unit, eye_look, args, outward

# --- the line's colours (Bayleef and Meganium pass their own) --------------------------------------------------------
GREEN = "#BEE38E"          # the coat: the drawing's pale green
LEAF, RIB = "#68BC4E", "#4F9A3C"
BUD = "#5DA84C"
CLAW = "#F2F0EA"
RED, PUPIL, WHITE, INK = "#D42A3A", "#6E0E1C", "#FFFFFF", "#2A1A1C"
SMILE = "#3E6A34"


# --- small helpers ----------------------------------------------------------------------------------------------------
def hit(ells, o, d):
    """Where a ray from o along d leaves the union of ellipsoids ((centre, radii), ...): the far side of the furthest."""
    o, d = np.array(o, float), unit(*d)
    best = 0.0
    for c, r in ells:
        c, r = np.array(c, float), np.array(r, float)
        q, e = (o - c) / r, d / r
        a, b, cc = e @ e, 2 * (q @ e), q @ q - 1
        disc = b * b - 4 * a * cc
        if disc > 0:
            best = max(best, (-b + math.sqrt(disc)) / (2 * a))
    return o + d * best


# --- the body ---------------------------------------------------------------------------------------------------------
def trunk(k, parts, col):
    """The trunk: a few fused ellipsoids on the body bone (chest, middle, rump, belly), each (centre, radii)."""
    for i, (c, r) in enumerate(parts):
        k.add(f"trunk{i}", ell(c, r), col, "body")


def claws(k, foot, front, n, spread, size, col, bone):
    """n small solid white points on a foot's front edge."""
    xs = [0.0] if n == 1 else list(np.linspace(-spread, spread, n))
    for i, dx in enumerate(xs):
        a = np.array([foot[0] + dx, front + 0.01, 0.0])
        s = size
        pts = [a + (-s * 0.6, 0.004, 0.003), a + (s * 0.6, 0.004, 0.003), a + (0, 0.006, s * 1.0),
               a + (0, s * 0.5, 0.003), a + (dx * 0.2, -s * 0.8, s * 0.2)]
        k.add(f"claw{bone[:4]}{i}", hull(pts), col, bone, fuse=False, mirror=True, surface="shell")


def legs(k, spec, col, claw_col):
    """
    Four stubby legs (spec: see the calls): a fat haunch fused into the flank over each joint, a short thick shin,
    a wide round foot, claws on its front.
    """
    x, top = spec["x"], spec["top"]
    for name, y, bone, haunch in (("leg", spec["front"], "leg{s}", spec["shoulder"]),
                                  ("hind", spec["hind"], "hind{s}", spec["thigh"])):
        hx, hy, hz = spec.get("haunch_at", (0.012, 0.0, 0.0))
        k.add(name + "top", ell((x + hx, y + hy, top + hz), haunch), col, bone, mirror=True)
        k.add(name, tube([(x, y, top), (x + 0.004, y - 0.004, (top + spec["ankle"]) / 2),
                          (x + 0.006, y - 0.006, spec["ankle"])], [spec["r"] * 1.1, spec["r"], spec["r"] * 0.96]),
              col, bone, mirror=True)
        fr = spec["foot"]
        foot = (x + 0.006, y - spec["toe"], fr[2])
        k.add(name + "foot", ell(foot, fr), col, bone, mirror=True)
        claws(k, foot, foot[1] - fr[1], spec["claws"], spec["spread"], spec["claw"], claw_col, bone)


# --- the face ----------------------------------------------------------------------------------------------------------
def eye_colours(white, iris, pupil, ink, rim=0.84, at=(-0.12, -0.06), size=(0.76, 0.86), mid=(0.42, 0.52),
                light=((0.14, 0.36), (0.26, 0.24)), slit=False):
    """The open eye (u outward, v up): white ground, a big iris toward the nose, a dark middle, a white glint up and
    out, a dark rim a sixth of its half-size."""
    W, I, P, K = srgb(white), srgb(iris), srgb(pupil), srgb(ink)
    def look(u, v):
        c = np.tile(W, (len(u), 1))
        c[((u - at[0]) / size[0]) ** 2 + ((v - at[1]) / size[1]) ** 2 < 1] = I
        c[((u - at[0]) / mid[0]) ** 2 + ((v - at[1]) / mid[1]) ** 2 < 1] = P
        (lu, lv), (ru, rv) = light
        c[((u - lu) / ru) ** 2 + ((v - lv) / rv) ** 2 < 1] = W
        c[np.hypot(u, v) > rim] = K
        return c
    return look


def lay_eyes(k, state, centre, d, half, look, lid, ink):
    k.decal(centre, d, half, eye_look(state, look, srgb(lid), srgb(ink), line=0.15), eps=0.005, rings=20, segs=56,
            mirror=True)


def smile(lip, ink, depth=0.32, width=0.3):
    """A small smile: a line curving up at its corners with a little dip in its middle (u, v in -1..1)."""
    L, K = srgb(lip), srgb(ink)
    def look(u, v):
        c = np.tile(L, (len(u), 1))
        curve = -0.25 + 0.75 * (np.abs(u) - 0.3) ** 2 - 0.12 * np.exp(-(u / 0.18) ** 2)
        c[(np.abs(v - curve) < width) & (np.abs(u) < 0.92)] = K
        return c
    return look


# --- the leaf ---------------------------------------------------------------------------------------------------------
def big_leaf(k, name, base, back, length, th0, th1, width, thick, col, rib, bone, tip_pow=1.0):
    """
    A great pointed leaf from base, rising at th0 degrees and sweeping out toward `back` (a level direction) until it
    runs at th1, its face turned across the plane of up and back (so it shows from the side the plane faces); widest
    a little before its middle, pointed at its tip; a darker midrib on both faces.
    """
    base = np.array(base, float)
    h = np.array(back, float); h[2] = 0; h /= np.linalg.norm(h)
    up = np.array([0, 0, 1.0])
    n = np.cross(h, up)
    U = np.linspace(0, 1, 200)
    th = np.radians(th0 + (th1 - th0) * U)
    tang = np.outer(np.cos(th), h) + np.outer(np.sin(th), up)
    mids = base + np.vstack([[0, 0, 0], np.cumsum((tang[1:] + tang[:-1]) / 2 * length / 199, axis=0)])

    def mid(uu):
        return np.stack([np.interp(uu, U, mids[:, i]) for i in range(3)], axis=-1)

    def across(uu):
        t = np.radians(th0 + (th1 - th0) * uu)
        return -np.sin(t)[..., None] * h + np.cos(t)[..., None] * up

    def at(u, v):
        uu = 0.015 + 0.97 * u
        w = width * np.sin(np.pi * uu ** 0.78) ** 0.62 * (1 - 0.25 * uu ** tip_pow) + 0.003
        s = (v - 0.5) * 2 * w
        cup = 0.018 * (1 - (2 * v - 1) ** 2) * np.sin(np.pi * uu)
        return mid(uu) + s[..., None] * across(uu) + cup[..., None] * n
    k.add(name, outward(sheet(at, 90, 11, thick)), col, bone, fuse=False, surface="leaf")
    if rib:
        for sg in (1, -1):
            pts = [mid(t) + n * sg * (thick / 2 + 0.001) for t in np.linspace(0.04, 0.86, 9)]
            k.add(f"{name}rib{'a' if sg > 0 else 'b'}", outward(tube(pts, [0.0075] * 6 + [0.0065, 0.005, 0.004], seg=10,
                                                                      per=4)), rib, bone, fuse=False, surface="leaf")
    return mids


# --- the necklace -------------------------------------------------------------------------------------------------------
def bud(k, name, at, out, r, col, bone):
    """A round bud standing proud of the surface at `at`, along `out`."""
    at, out = np.array(at, float), unit(*out)
    k.add(name, ell(at + out * r * 0.35, (r, r, r), sub=3), col, bone, fuse=False, surface="leaf")


def spike_bud(k, name, at, out, r, length, col, bone):
    """A pointed bud: a cone of a few flat faces from a round base on the surface, its point along `out`."""
    at, out = np.array(at, float), unit(*out)
    a = np.cross(out, (0, 0, 1.0))
    if np.linalg.norm(a) < 1e-3:
        a = np.array([1.0, 0, 0])
    a /= np.linalg.norm(a); b = np.cross(out, a)
    pts = [at - out * r * 0.4 + r * (math.cos(t) * a + math.sin(t) * b) for t in np.linspace(0, 2 * math.pi, 7)[:-1]]
    pts.append(at + out * length)
    k.add(name, hull(pts, merge=0), col, bone, fuse=False, surface="leaf")


# --- Chikorita ----------------------------------------------------------------------------------------------------------
H = np.array([0, -0.078, 0.27])                         # the head's middle
HR = np.array([0.112, 0.102, 0.128])
MUZZLE = (H + (0, -0.05, -0.065), (0.092, 0.07, 0.062))
TRUNK = [((0, -0.03, 0.135), (0.092, 0.085, 0.085)),       # chest, under the head
         ((0, 0.05, 0.13), (0.096, 0.1, 0.082)),           # middle
         ((0, 0.11, 0.125), (0.094, 0.07, 0.08)),          # rump
         ((0, 0.04, 0.095), (0.088, 0.12, 0.055))]         # belly, low
LEGS = dict(x=0.066, front=-0.05, hind=0.085, top=0.11, ankle=0.042, r=0.042, shoulder=(0.052, 0.058, 0.065),
            thigh=(0.056, 0.062, 0.068), foot=(0.044, 0.054, 0.026), toe=0.012, claws=2, spread=0.016, claw=0.016)
EYE_D = unit(0.47, -0.86, -0.12)
EYE_HALF = (0.034, 0.042)


def head_shape(V):
    """A bullet: a little narrower at the top, full at the jaw."""
    V = V.copy()
    V[:, 0] *= 1 - 0.1 * np.clip(V[:, 2], 0, 1)
    V[:, 1] *= 1 - 0.08 * np.clip(V[:, 2], 0, 1)
    return V


def necklace(k, ells, centre, rx, ry, tilt, z_low, r_bud, spikes, col, bone="body"):
    """Round buds round the front of the neck and pointed ones at the shoulders and behind, each set on the surface
    found along a ray out from the neck's middle. spikes: the angles (from the front) that take a pointed bud."""
    centre = np.array(centre, float)
    for i, a in enumerate(range(0, 360, 360 // 12)):
        ar = math.radians(a)
        d = np.array([math.sin(ar), -math.cos(ar), 0.0])
        z = centre[2] - tilt * math.cos(ar)
        o = np.array([centre[0], centre[1], z])
        p = hit(ells, o, d)
        out = unit(d[0], d[1], 0.25)
        if any(abs(((a + 180) % 360) - 180 - s) < 1 for s in spikes):
            spike_bud(k, f"spikebud{i}", p, out, r_bud * 1.05, r_bud * 2.3, col, bone)
        elif abs(((a + 180) % 360) - 180) <= 60:
            bud(k, f"bud{i}", p, out, r_bud, col, bone)


def chikorita(src, out):
    k = Kit(src, coat="fur")
    trunk(k, TRUNK, GREEN)
    legs(k, LEGS, GREEN, CLAW)
    k.add("head", ell(H, HR, deform=head_shape), GREEN, "head")
    k.add("muzzle", ell(*MUZZLE), GREEN, "head")
    k.add("neck", ell((0, -0.045, 0.19), (0.088, 0.075, 0.07)), GREEN, "head")

    # The stalk and the great leaf, rising and sweeping back and some 35 degrees out to +X
    top = hit([(H, HR)], H, (0, -0.02, 1)) - (0, 0, 0.012)
    k.add("stalk", outward(tube([top, top + (0.004, 0.01, 0.035), top + (0.01, 0.02, 0.06)], [0.016, 0.014, 0.013])),
          LEAF, "leaf", fuse=False, surface="leaf")
    big_leaf(k, "blade", top + (0.008, 0.016, 0.055), (0.57, 0.82, 0), 0.37, 42, 4, 0.068, 0.024, LEAF, RIB, "leaf")

    # The necklace: round buds in front, pointed ones at the shoulders and behind
    ells = TRUNK + [(H, HR), MUZZLE, ((0, -0.045, 0.19), (0.088, 0.075, 0.07))]
    necklace(k, ells, (0, 0.0, 0.17), 0.1, 0.1, 0.03, 0.14, 0.02, spikes=(90, -90, 150, -150), col=BUD)

    # The smile, low on the face between the eyes
    k.decal(hit([(H, HR), MUZZLE], H, (0, -1, -0.52)), unit(0, -1, -0.3), (0.027, 0.016), smile(GREEN, SMILE, width=0.32),
            eps=0.003, rings=16, segs=72)

    k.build(voxel=0.0058, band=0.008)
    centre = hit([(H, HR)], H, EYE_D)
    look = eye_colours(WHITE, RED, PUPIL, INK)
    k.eye_states(lambda state: lay_eyes(k, state, centre, EYE_D, EYE_HALF, look, GREEN, INK))
    k.export(out)


if __name__ == "__main__":
    chikorita(*args())
