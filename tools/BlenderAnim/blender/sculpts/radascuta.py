# Radascuta (Carpathia No. 10072, the stag beetle grub) sculpted from nothing on the skeleton and clips of the game's
# own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/radascuta.py -- <the export's Radascuta.glb> <out.glb>
#
# From its picture (docs/art/reference/gemini/carpathia/10072-Radascuta.jpg) and its brief (CP-dex-2-mountains.md,
# 72): a chubby young stag beetle on its six short legs. A glossy chestnut dome of wing cases parted down the middle
# covers its back and sides to half its height; a chestnut hood with a hard rim covers the top and sides of a round
# cream head, its edge dipping to a point over the brow and running down beside the face to the cheeks. Under the
# shell a fat cream belly bulges out, folded in bands. The face: big round fierce eyes under a heavy cream brow that
# slopes to the nose, dark brown irises with a glint, a small rounded snout over a short mouth, and at the corners of
# the mouth two short dark mandibles curling down and in like little pincers. Two short segmented antennae rise from
# the hood. Two small arms forward under the chin (chestnut, dark clawed hands), four short dark legs under the belly.
# Written to be grown from (Radaskon, radaskon.py): the palette, the eye, the hood, the mandibles and the board check
# are module-level functions.
# Bones: body (0, 0.03, 0.17), arm{s} (0.09, -0.09, 0.12), head (0, -0.12, 0.16), leg{s} (0.1, -0.06, 0.1),
# hind{s} (0.1, 0.13, 0.1).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, sheet, hull, outward, srgb, inside, smoothstep, unit, eye_look, args, polygon)

CHESTNUT, DEEP, SEAM, GLOSS = "#93452A", "#6E321E", "#3E1E14", "#B9643E"
CREAM, FOLD, BROW = "#F2E2B8", "#D8C394", "#F6EACA"
LEG, CLAW, IRIS, INK, MOUTH = "#4C3024", "#3A2219", "#5A341E", "#2A1810", "#4A2A1C"
WHITE = "#FFFFFF"


def on_ell(c, r, az, el, lift=0.0):
    """The point of an ellipsoid at an azimuth from the front (+ toward +X) and an elevation, in degrees, and its
    outward normal there."""
    a, e = math.radians(az), math.radians(el)
    d = np.array([math.sin(a) * math.cos(e), -math.cos(a) * math.cos(e), math.sin(e)])
    p = np.array(c) + np.array(r) * d
    n = d / np.array(r); n /= np.linalg.norm(n)
    return p + n * lift, n


# --- shells: the wing cases over the back and the hood over the head --------------------------------------------
def elytron(c, r, p0, p1, t0, t1, thick, nu=24, nv=36):
    """The right wing case: a sheet on the ellipsoid (c, r) from the midline (t0, degrees from the top) round to the
    side (t1), from p0 to p1 along its length (-1 the front, 1 the back)."""
    c, r = np.array(c, float), np.array(r, float)
    def at(u, v):
        t = np.radians(t0 + (t1 - t0) * u)
        a = p0(t) if callable(p0) else p0
        p = a + (p1 - a) * v
        q = np.sqrt(np.clip(1 - p * p, 1e-4, 1))
        return np.stack([c[0] + r[0] * q * np.sin(t), c[1] + r[1] * p, c[2] + r[2] * q * np.cos(t)], axis=-1)
    return outward(sheet(at, nu, nv, thick))


def hood(c, r, edge, side_deg, back_deg, thick, nu=44, nv=20):
    """A hard hood over a head, open toward the front: about the head's forward axis, phi round from the top (+ to
    +X) out to side_deg, theta from the front at edge(phi) (both in degrees) to back_deg."""
    c, r = np.array(c, float), np.array(r, float)
    def at(u, v):
        phi = np.radians(-side_deg + 2 * side_deg * u)
        th0 = np.radians(edge(np.degrees(phi)))
        th = th0 + (np.radians(back_deg) - th0) * v
        d = np.stack([np.sin(th) * np.sin(phi), -np.cos(th), np.sin(th) * np.cos(phi)], axis=-1)
        return c + d * r
    return outward(sheet(at, nu, nv, thick))


# --- the face ------------------------------------------------------------------------------------------------------
def brow_cut(slope, top, s):
    """An eye's outline: its oval cut by the brow's straight line, high at the outer corner and low at the inner (u
    toward +X: s is the eye's side)."""
    line = polygon([(-1.6, -1.6), (1.6, -1.6), (1.6, top + slope * 1.6 * s), (-1.6, top - slope * 1.6 * s)])
    return lambda th: min(1.0, line(th))


def eye_open(slope, top, s, iris=IRIS):
    """The open eye: white, a big dark brown iris looking a little in, a black pupil, a glint, a dark rim and the
    brow's line along its cut top (u toward +X)."""
    W, I, K = srgb(WHITE), srgb(iris), srgb(INK)
    def look(u, v):
        uo = u * s                         # outward
        c = np.tile(W, (len(u), 1))
        c[((uo + 0.12) / 0.62) ** 2 + ((v + 0.04) / 0.68) ** 2 < 1] = I
        c[((uo + 0.14) / 0.34) ** 2 + ((v + 0.06) / 0.38) ** 2 < 1] = K
        c[((uo + 0.36) / 0.17) ** 2 + ((v - 0.2) / 0.17) ** 2 < 1] = W
        c[u * u + v * v > 0.84 ** 2] = K
        c[v > top + slope * uo - 0.2] = K
        return c
    return look


def lay_eyes(k, state, hc, hr, az, el, half, slope=0.42, top=0.42, lid=CREAM):
    """Both eyes, each laid by hand so that its brow cut slopes to the nose on both sides."""
    for s in (1, -1):
        p, n = on_ell(hc, hr, az * s, el)
        opened = eye_open(slope, top, s)
        if state == "Open":
            look, outline = opened, brow_cut(slope, top, s)
        else:
            base = eye_look(state, lambda u, v, o=opened, s=s: o(u * s, v), srgb(lid), srgb(INK), line=0.15)
            look, outline = (lambda u, v, b=base, s=s: b(u * s, v)), brow_cut(slope, top, s)
        k.decal(p, n, half, look, eps=0.0018, outline=outline, rings=20, segs=56)


def mandible(k, name, pts, radii, bone, colour=LEG):
    """A short dark pincer curling down and in: a loose tapering tube, mirrored."""
    k.add(name, outward(tube(pts, radii)), colour, bone, fuse=False, mirror=True, surface="shell")


def board(k):
    """The game's board check on the body just built: what it shows across at its angle against its square."""
    V = np.array([v.co[:] for v in k.ob.data.vertices])
    a = math.radians(31.5)
    proj = V[:, 0] * math.cos(a) + V[:, 1] * math.sin(a)
    h, w, l = V[:, 2].max(), np.ptp(V[:, 0]), np.ptp(V[:, 1])
    sq = 1.3 * max(h, 0.85 * max(w, l))
    print("KIT: board: shows %.3f in a square of %.3f (%.2f); corner sum %.3f" % (
        np.ptp(proj), sq, np.ptp(proj) / sq, 0.853 * w + 0.522 * l))


# --- Radascuta -----------------------------------------------------------------------------------------------------
BC, BR = np.array([0.0, 0.035, 0.122]), np.array([0.15, 0.188, 0.104])       # the cream belly
EC, ER = np.array([0.0, 0.06, 0.152]), np.array([0.158, 0.17, 0.124])        # the wing cases' ellipsoid
HC, HR = np.array([0.0, -0.152, 0.15]), np.array([0.116, 0.1, 0.104])      # the head


def hood_edge(phi):
    a = np.abs(phi)
    return 50 + 28 * (a / 112) ** 1.6 - 7 * np.exp(-(phi / 16) ** 2)


def radascuta(src, out):
    k = Kit(src, coat="shell")
    # belly and the dark core under the wing cases (seen through their parting)
    k.add("belly", ell(BC, BR, sub=5), CREAM, "body", surface="fur")
    k.add("core", ell(EC + (0, 0, -0.004), ER - 0.013, sub=5), SEAM, "body")
    # the head, the brows and the snout
    k.add("skull", ell(HC, HR, sub=5), CREAM, "head", surface="fur")
    for s, nm in ((1, "browR"), (-1, "browL")):
        p, n = on_ell(HC, HR, 29 * s, 27, -0.006)
        k.add(nm, ell(p, (0.032, 0.012, 0.01), rot=(0, -16 * s, 0)), BROW, "head", surface="fur")
    k.add("snout", ell((0, -0.25, 0.124), (0.038, 0.024, 0.025)), BROW, "head", surface="fur")
    # the wing cases, parted down the middle, and the hood
    k.add("elytron", elytron(EC, ER, lambda t: -0.8 + 0.3 * (t / np.radians(108)) ** 2, 0.994, 2.5, 108, 0.011), CHESTNUT, "body", fuse=False, mirror=True)
    k.add("hood", hood(HC, HR + 0.008, hood_edge, 112, 176, 0.011), CHESTNUT, "head", fuse=False)
    # antennae: short, segmented, up and out from the hood
    base, tip = np.array([0.04, -0.2, 0.25]), np.array([0.078, -0.215, 0.292])
    k.add("antenna", outward(tube([base, (base + tip) / 2, tip], [0.0085, 0.008, 0.0078])), DEEP, "head",
          fuse=False, mirror=True)
    for i, t in enumerate((0.3, 0.55, 0.8)):
        k.add(f"seg{i}", outward(ell(base + (tip - base) * t, (0.0105, 0.0105, 0.012), sub=3)), CHESTNUT, "head",
              fuse=False, mirror=True)
    k.add("club", outward(ell(tip + (0.002, 0, 0.004), (0.012, 0.012, 0.014), sub=3)), CHESTNUT, "head",
          fuse=False, mirror=True)
    # mandibles at the mouth's corners, curling down and in
    mandible(k, "mandible", [(0.046, -0.238, 0.1), (0.066, -0.256, 0.088), (0.064, -0.272, 0.07),
                             (0.044, -0.278, 0.062)], [0.017, 0.015, 0.011, 0.005], "head")
    # arms: forward under the chin, a chestnut upper arm and a dark clawed hand
    k.add("arm", tube([(0.095, -0.1, 0.125), (0.118, -0.138, 0.098), (0.128, -0.162, 0.076)],
                      [0.022, 0.02, 0.019]), CHESTNUT, "arm{s}", mirror=True)
    k.add("hand", ell((0.13, -0.172, 0.064), (0.021, 0.021, 0.024)), LEG, "arm{s}", mirror=True)
    for j, (dx, dy) in enumerate(((-0.012, -0.008), (0.012, -0.002))):
        b0 = np.array([0.13 + dx, -0.176 + dy, 0.048])
        k.add(f"claw{j}", hull([b0 + (-0.008, -0.008, 0.006), b0 + (0.008, -0.008, 0.006), b0 + (0.008, 0.008, 0.006),
                                b0 + (-0.008, 0.008, 0.006), b0 + (dx * 0.3, -0.012, -0.022)]), CLAW, "arm{s}",
              fuse=False, mirror=True, surface="shell")
    # six short thick legs tucked under the belly
    for bone, y0, dy in (("leg{s}", -0.07, -0.02), ("body", 0.035, 0.0), ("hind{s}", 0.13, 0.02)):
        k.add(f"thigh_{bone[:3]}", tube([(0.085, y0, 0.07), (0.112, y0 + dy * 0.4, 0.048), (0.12, y0 + dy, 0.024)],
                                       [0.031, 0.028, 0.025]), LEG, bone, mirror=True)
        k.add(f"foot_{bone[:3]}", ell((0.123, y0 + dy * 1.25, 0.012), (0.026, 0.03, 0.013)), LEG, bone, mirror=True)
    # the core is dark only under the wing cases: cream below their rim
    k.paint(lambda P: inside(P[:, 2] - 0.14, 0.003), CREAM, on=["core"])
    k.paint(lambda P: smoothstep(0.14, 0.18, P[:, 1]) * (1 - inside(P[:, 2] - 0.14, 0.003)), CHESTNUT, on=["core"])
    # the belly's folds: arcs round its length on the sides, rows under the chin
    for y in (-0.03, 0.03, 0.09, 0.15):
        k.paint(lambda P, y=y: inside(np.abs(P[:, 1] - y) - 0.0028, 0.0016) * inside(P[:, 2] - 0.132, 0.004)
                * smoothstep(0.03, 0.06, np.abs(P[:, 0])), FOLD, on=["belly"])
    for z in (0.052, 0.08):
        k.paint(lambda P, z=z: inside(np.abs(P[:, 2] - z) - 0.0028, 0.0016) * inside(P[:, 1] + 0.08, 0.004)
                * inside(np.abs(P[:, 0]) - 0.09, 0.006), FOLD, on=["belly"])
    # the mouth: a short dark curve under the snout
    M, Cm = srgb(MOUTH), srgb(CREAM)
    def mouth(u, v):
        c = np.tile(Cm, (len(u), 1))
        c[np.abs(v - (0.25 - 0.55 * u * u)) < 0.4] = M
        return c
    k.decal((0, -0.24, 0.094), unit(0, -1, -0.35), (0.026, 0.01), mouth, eps=0.0018, rings=10, segs=48)
    k.build(voxel=0.0039, smooth=3, band=0.004, wband=0.03)
    board(k)
    k.eye_states(lambda state: lay_eyes(k, state, HC, HR, 31, 9, (0.036, 0.038)))
    k.export(out)


if __name__ == "__main__":
    radascuta(*args())
