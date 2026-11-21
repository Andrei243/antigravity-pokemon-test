# Flittle sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/flittle.py -- <the export's Flittle.glb> <out.glb>
#
# The design, from the references: a pale yellow egg of a body that is all head, a wavy two-layered skirt round its
# middle (a yellow frill over a pink one, the widest part of it), an orange underside below the skirt and two tiny
# lilac and white stub feet. From the top of its sides rise two broad white tufts like handkerchiefs, each out of a
# little crown of yellow spikes. Its face: two tall aqua ovals of eyes with a thick black rim and three lashes on top,
# a lilac triangle on the brow pointing down and a tiny round lilac beak. No legs to speak of and no frilled legs.
#
# Written to be grown from (espathra.py imports it with sibling): the skirt (frill), the tuft (tuft) and the eye's look
# (aqua_eye) are functions; the build is under the main guard.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, sheet, slab, hull, outward, srgb, inside, unit, eye_look, polygon, args)

# --- the line's colours ---------------------------------------------------------------------------------------------
YELLOW, YELLOW_DK = "#F6E77A", "#E8D25E"
PINK = "#EE9CC8"
ORANGE = "#F29A48"
WHITE = "#F6F6F4"
LILAC, LILAC_DK = "#B6A0E2", "#9682C8"
AQUA, AQUA_LT = "#8ED8D6", "#C4F0EC"
INK = srgb("#17161C")

# --- the frame ------------------------------------------------------------------------------------------------------
EGG_C = np.array([0.0, 0.0, 0.143])
EGG_R = np.array([0.09, 0.084, 0.094])
EYE_X, EYE_Z, EYE_HALF = 0.034, 0.158, (0.0155, 0.031)
VOXEL = 0.0028


def on_ell(c, r, x, z, front=True):
    """The point of an ellipsoid's front (or back) with that x and z, and its outward normal there."""
    c, r = np.array(c, float), np.array(r, float)
    a = (x - c[0]) / r[0]; b = (z - c[2]) / r[2]
    dy = r[1] * math.sqrt(max(1 - a * a - b * b, 0.0))
    p = np.array([x, c[1] - dy if front else c[1] + dy, z])
    n = (p - c) / r ** 2
    return p, n / np.linalg.norm(n)


def frill(c, root, edge, top, drop, lobes=6, wave=0.012, phase=0.0, thick=0.006, arc=(0.0, 1.0), nu=121, nv=7,
          rise=0.0):
    """
    A wavy skirt round an upright axis through c: from radius root at height top out to radius edge, falling by drop,
    its rim in lobes waves that rise and fall by wave (out and down). arc: the share of the round it covers, from
    the back (0 the back, 0.5 the front); a whole round has its seam behind. rise lifts the root toward the sides.
    A loose piece.
    """
    c = np.array(c, float)
    a0, a1 = arc

    def at(u, v):
        t = a0 + (a1 - a0) * u
        th = 2 * math.pi * t + math.pi / 2                  # t 0 the back (+Y), 0.5 the front (-Y)
        w = np.sin(lobes * 2 * math.pi * t + phase)
        side = np.abs(np.cos(th))                           # 1 at the sides
        r = root + (edge - root) * v + wave * 0.6 * w * v ** 1.5
        z = top + rise * side - drop * v ** 1.4 + wave * w * v ** 1.5
        return np.stack([c[0] + r * np.cos(th), c[1] + r * np.sin(th), c[2] + z + 0 * u], axis=-1)
    return outward(sheet(at, nu, nv, thick))


def tuft(root, d, w, length, width, thick):
    """A broad tuft like a handkerchief: narrow at its root, wide and gently waved across its top, in the plane of d
    (along it) and w (across it). A loose piece."""
    L, W = length, width
    outline = [(0.0, -0.18 * W), (0.35 * L, -0.42 * W), (0.72 * L, -0.56 * W), (0.93 * L, -0.5 * W), (1.0 * L, -0.28 * W),
               (0.99 * L, -0.04 * W), (1.0 * L, 0.16 * W), (0.95 * L, 0.42 * W), (0.75 * L, 0.52 * W),
               (0.38 * L, 0.36 * W), (0.0, 0.16 * W)]
    return slab(outline, root, d, w, thick)


def spike(base, d, length, r):
    """A small solid spike from base along d: a hull of a ring and a point."""
    d = unit(*d)
    a = np.cross(d, (0, 0, 1.0))
    if np.linalg.norm(a) < 1e-3:
        a = np.array([1.0, 0, 0])
    a /= np.linalg.norm(a); b = np.cross(d, a)
    base = np.array(base, float)
    pts = [base + r * (math.cos(t) * a + math.sin(t) * b) for t in np.linspace(0, 2 * math.pi, 7)[:-1]]
    pts.append(base + d * length)
    pts.append(base - d * r * 0.8)
    return hull(np.array(pts))


def aqua_eye(fill=AQUA, light=AQUA_LT, rim=0.22, lashes=3, oval=0.74):
    """A tall oval eye of one colour in a thick black rim, lighter below, and lashes over its top (u outward, v up):
    the oval fills the lower part of the patch, the lashes the band above it."""
    cf, cl = srgb(fill), srgb(light)

    def look(u, v):
        c = np.tile(cf, (len(u), 1))
        # the oval's own frame: it fills |v| < oval of the patch, centred a little low
        ov = (v + (1 - oval) * 0.5) / oval
        ou = u / 0.94
        d = np.hypot(ou, ov)
        lower = np.clip((-ov - 0.1) / 0.8, 0, 1)[:, None]
        c = c * (1 - lower) + cl * lower
        c[d > 1 - rim] = INK
        outside = d > 1.0
        c[outside] = np.nan
        # the lashes: short strokes over the top, the outer one longest
        for i in range(lashes):
            x = -0.45 + 0.45 * i
            ang = math.radians(-20 + 22 * i)
            dx, dy = math.sin(ang), math.cos(ang)
            s = (u - x) * dx + (v - 0.56) * dy                 # along the lash
            n = (u - x) * dy - (v - 0.56) * dx                 # across it
            c[(np.abs(n) < 0.13) & (s > 0) & (s < 0.4 + 0.04 * i)] = INK
        return c
    return look


def eye_patch(look, lid):
    """The open look with whatever lies outside the oval and the lashes painted the lid's colour."""
    def f(u, v):
        c = look(u, v)
        bad = np.isnan(c[:, 0])
        c[bad] = lid
        return c
    return f


def flittle(src, out):
    k = Kit(src, coat="fur")

    # --- the egg: body and head in one ------------------------------------------------------------------------------
    k.add("egg", ell(EGG_C, EGG_R), YELLOW, "body")
    k.add("belly", ell((0, 0, 0.074), (0.072, 0.068, 0.05)), YELLOW, "body")
    k.add("crown", ell(EGG_C + np.array([0, 0.002, 0.03]), EGG_R * np.array([0.92, 0.9, 0.78])), YELLOW, "head")
    # the orange underside, below the skirt
    k.paint(lambda P: inside(P[:, 2] - 0.092, 0.003), ORANGE)

    # --- the skirt: a pink frill under a yellow one, the widest part of it --------------------------------------------
    k.add("skirtpink", frill(EGG_C + np.array([0, 0, 0]), 0.084, 0.136, -0.043, 0.03, lobes=6, wave=0.012,
                             phase=1.6, thick=0.009, rise=0.008), PINK, "body", fuse=False, surface="cloth")
    k.add("skirt", frill(EGG_C + np.array([0, 0, 0]), 0.086, 0.13, -0.035, 0.026, lobes=6, wave=0.013,
                         phase=0.0, thick=0.0095, rise=0.012), YELLOW, "body", fuse=False, surface="cloth")

    # --- the feet: lilac stubs with white toes -----------------------------------------------------------------------
    for s in (1, -1):
        k.add(f"foot{s}", outward(ell((0.032 * s, -0.012, 0.022), (0.016, 0.019, 0.02))), LILAC, f"leg{'R' if s > 0 else 'L'}",
              fuse=False, surface="skin")
        k.add(f"toe{s}", outward(ell((0.032 * s, -0.02, 0.008), (0.013, 0.014, 0.009))), WHITE,
              f"leg{'R' if s > 0 else 'L'}", fuse=False, surface="skin")

    # --- the tufts: broad white handkerchiefs out of yellow spiky crowns ---------------------------------------------
    for s in (1, -1):
        side = "R" if s > 0 else "L"
        root = np.array([0.046 * s, 0.006, 0.212])
        d = unit(0.55 * s, 0.12, 1.0)
        w = np.cross(d, unit(0, -1, 0.0)) * s                       # across the tuft, its face to the front
        w = w / np.linalg.norm(w)
        k.add(f"tuft{side}", tuft(root, d, w, 0.082, 0.078, 0.011), WHITE, f"ear{side}", fuse=False, surface="cloth")
        # a lilac shade at its root, showing on both faces
        k.add(f"tuftroot{side}", tuft(root - d * 0.002, d, w, 0.026, 0.07, 0.0135), LILAC, f"ear{side}", fuse=False,
              surface="cloth")
        # the yellow crown of spikes round its root
        for j, (dx, dy, ln) in enumerate(((0.022, -0.016, 0.026), (-0.012, -0.018, 0.022), (0.026, 0.012, 0.024),
                                          (-0.014, 0.016, 0.02))):
            q = root + np.array([dx * s, dy, -0.006]) - EGG_C
            base = EGG_C + q / np.linalg.norm(q / EGG_R)                     # on the egg's surface
            nrm = (base - EGG_C) / EGG_R ** 2; nrm /= np.linalg.norm(nrm)
            dd = unit(*(nrm + np.array([0.25 * s, 0, 0.5])))
            k.add(f"crown{side}{j}", spike(base - dd * 0.006, dd, ln + 0.006, 0.011), YELLOW, "head", fuse=False,
                  surface="fur")

    # --- the face: a lilac triangle on the brow and a tiny round beak, before the build ------------------------------
    tp, tn = on_ell(EGG_C, EGG_R, 0.0, 0.196)
    k.decal(tp, tn, (0.016, 0.014), LILAC, outline=polygon([(-1.0, 0.62), (1.0, 0.62), (0.0, -0.95)]), eps=0.0018,
            rings=12, segs=60)
    bp, bn = on_ell(EGG_C, EGG_R, 0.0, 0.126)
    k.add("beak", outward(ell(bp + bn * 0.003, (0.0115, 0.009, 0.0085))), LILAC, "head", fuse=False, surface="shell")
    k.add("beakshade", outward(ell(bp + bn * 0.004 + np.array([0, 0, -0.003]), (0.0085, 0.0075, 0.0045))), LILAC_DK,
          "head", fuse=False, surface="shell")

    k.build(voxel=VOXEL, smooth=4, band=0.004, wband=0.02)

    # --- the eyes: tall aqua ovals in thick black rims, three lashes over each -----------------------------------------
    lid = srgb(YELLOW)
    look = eye_patch(aqua_eye(), lid)

    def eyes(state):
        lk = eye_look(state, look, lid, INK, line=0.12)
        for s in (1, -1):
            p, n = on_ell(EGG_C, EGG_R, EYE_X * s, EYE_Z)
            k.decal(p, n, EYE_HALF, (lambda u, v, s=s: lk(s * u, v)), eps=0.0018, rings=20, segs=56)

    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    flittle(*args())
