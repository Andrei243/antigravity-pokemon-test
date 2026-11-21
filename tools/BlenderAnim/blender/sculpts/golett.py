# Golett sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/golett.py -- <the export's Golett.glb> <out.glb>
#
# The design, from the references: a small golem of clay. Its body is one great round barrel of deep blue clay with
# paler teal patches; across its front two brown straps cross in an X and hold a pale square seal turned on its corner,
# a yellow square spiral on it. A small faceted head of pale teal clay sits on top, its point to the back; across its
# front a band of deep blue on which the eyes glow, two yellow upright blocks, each cut by a dark slot from the top.
# Short deep-blue upper arms to huge faceted fists of pale teal rock, carried forward and down here (the art raises
# one); short stub legs to big faceted rock feet.
# The task's text spoke of a blue seal with swirls on the chest: the pictures show a pale seal with one yellow square
# spiral, held by two crossed brown straps.
# The skeleton: body (0, 0, 0.24), arms (+-0.13, 0, 0.3), head (0, -0.01, 0.37), legs (+-0.07, 0, 0.12).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, unit, smoothstep, inside, eye_look, args, boxy, hull, polygon, sheet, \
    outward, sibling

stone = sibling("regirock").stone          # a stone with flat faces: a box with its edges cut back by chance

# --- colours (Golurk grows from these) --------------------------------------------------------------------------------
DEEP, PATCH, ROCK, ROCKSHADE = "#2A7FAE", "#6FB9C6", "#86C6CC", "#6CAFB8"
STRAP, SEAL, SPIRAL = "#946640", "#A9DAD8", "#F2DA78"
GLOW, SLOT = "#F6E28C", "#1C4E70"

# --- the frame --------------------------------------------------------------------------------------------------------
BODY_C, BODY_R = np.array([0.0, 0.0, 0.245]), np.array([0.162, 0.142, 0.158])
HEAD_C = np.array([0.0, -0.01, 0.43])
FACE_Y = -0.099                       # the eye band's front
EYE_X, EYE_Z, EYE_HALF = 0.031, 0.428, (0.017, 0.024)
FIST = np.array([0.225, -0.065, 0.2])
FOOT = np.array([0.085, -0.012, 0.042])
VOXEL = 0.0045


def ell_point(c, r, w):
    """Where a ray from an ellipsoid's middle along w leaves it."""
    w = np.asarray(w, float)
    t = 1 / np.sqrt(((w / r) ** 2).sum(-1))
    return c + w * t[..., None]


def strap(k, name, c, r, normal, width, bone, lift=0.0055, thick=0.009):
    """A brown strap round an ellipsoid in the plane square to normal, a little off its surface (a loose band)."""
    n = unit(*normal)
    e1 = np.cross(n, (0, 1.0, 0)) if abs(n[1]) < 0.9 else np.cross(n, (1.0, 0, 0))
    e1 /= np.linalg.norm(e1); e2 = np.cross(n, e1)
    def at(u, v):
        th = 2 * math.pi * (u + 0.25)            # its seam at the back
        w = np.cos(th)[..., None] * e1 + np.sin(th)[..., None] * e2
        p = ell_point(c, r, w)
        return p + w * lift + n * ((v - 0.5) * width)[..., None]
    k.add(name, outward(sheet(at, 97, 3, thick)), STRAP, bone, fuse=False, surface="leather")


def bar(p, q, w, ex, ey, nz, t0, t1):
    """A flat bar from p to q (in a plate's a, b), w wide, standing from t0 to t1 off the plate."""
    p, q = np.array(p, float), np.array(q, float)
    d = q - p
    d = d / max(np.linalg.norm(d), 1e-9)
    s = np.array([-d[1], d[0]])
    pts = []
    for a in (p - d * w, q + d * w):
        for b in (-w, w):
            for t in (t0, t1):
                uv = a + s * b
                pts.append(ex * uv[0] + ey * uv[1] + nz * t)
    return np.array(pts)


def spiral_panel(k, name, centre, normal, up, half, bone, plate=SEAL, ink=SPIRAL, turn=30, step=None, w=None):
    """A pale square plate turned by turn degrees on its face, a raised yellow square spiral on it."""
    nz = unit(*normal)
    ex = np.cross(np.array(up, float), nz); ex /= np.linalg.norm(ex)
    ey = np.cross(nz, ex)
    a = math.radians(turn)
    ex, ey = ex * math.cos(a) + ey * math.sin(a), -ex * math.sin(a) + ey * math.cos(a)
    centre = np.array(centre, float)
    pts = []
    for sa in (-1, 1):
        for sb in (-1, 1):
            for t, h in ((-0.012, half * 0.9), (0.0, half), (0.009, half * 0.92)):
                pts.append(centre + ex * sa * h + ey * sb * h + nz * t)
    k.add(name, hull(pts), plate, bone, fuse=False, surface="shell")
    step = step or half * 0.27
    w = w or half * 0.11
    path = [(0, 0), (1, 0), (1, 1), (-1, 1), (-1, -1), (2, -1), (2, 2), (-2, 2), (-2, -2), (1, -2)]
    for j, (p, q) in enumerate(zip(path, path[1:])):
        P = bar(np.array(p) * step, np.array(q) * step, w, ex, ey, nz, 0.006, 0.0125)
        k.add(f"{name}spiral{j}", hull(P + centre), ink, bone, fuse=False, surface="shell")


def body(k):
    k.add("body", ell(BODY_C, BODY_R), DEEP, "body")
    # paler teal patches over the clay, as the art's mottling
    blots = [((0.13, -0.05, 0.13), (0.06, 0.06, 0.05)), ((-0.12, -0.07, 0.11), (0.05, 0.05, 0.045)),
             ((0.1, 0.09, 0.32), (0.06, 0.05, 0.05)), ((-0.14, 0.04, 0.3), (0.045, 0.05, 0.05)),
             ((0.0, 0.1, 0.12), (0.07, 0.05, 0.05)), ((-0.06, -0.1, 0.36), (0.04, 0.04, 0.03))]
    def patches(P):
        m = np.zeros(len(P))
        for c, r in blots:
            q = (P - np.array(c)) / np.array(r)
            m = np.maximum(m, inside(np.sqrt((q ** 2).sum(1)) - 1, 0.08))
        return m
    k.paint(patches, PATCH, on=["body"])
    for s in (1, -1):
        strap(k, f"strap{'R' if s > 0 else 'L'}", BODY_C, BODY_R, (math.cos(math.radians(38)), 0, s * math.sin(math.radians(38))),
              0.034, "body")
    p = ell_point(BODY_C, BODY_R, unit(0, -1, 0.06))
    spiral_panel(k, "seal", p + np.array([0, -0.012, 0]), (0, -1, 0.06), (0, 0, 1), 0.058, "body", turn=24)


def head(k):
    """A small faceted head of pale clay, its point to the back; a deep blue band across its front for the eyes."""
    c = HEAD_C
    pts = []
    for z, hx, hyf, hyb in ((0.375, 0.07, 0.065, 0.06), (0.405, 0.088, 0.078, 0.07), (0.455, 0.086, 0.076, 0.074),
                            (0.488, 0.068, 0.05, 0.066)):
        for sx in (-1, 1):
            pts += [(sx * hx, c[1] - hyf, z), (sx * hx * 1.08, c[1], z), (sx * hx, c[1] + hyb, z),
                    (sx * hx * 0.55, c[1] - hyf * 1.03, z), (sx * hx * 0.55, c[1] + hyb * 1.03, z)]
    pts.append((-0.015, c[1] + 0.035, 0.512))         # the point, leaning back
    k.add("head", hull(pts), ROCK, "head", fuse=False, surface="shell")
    k.add("band", ell((0, FACE_Y + 0.0075, EYE_Z), (0.072, 0.0075, 0.034), deform=boxy(5)), DEEP, "head")
    k.add("neck", ell((0, 0.0, 0.37), (0.06, 0.055, 0.03)), DEEP, "body")


def arms(k):
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"arm{side}", tube([np.array([0.12, 0.0, 0.31]) * m, np.array([0.17, -0.03, 0.27]) * m,
                                  (FIST + (-0.03, 0.02, 0.04)) * m], [0.035, 0.032, 0.034]), DEEP, f"arm{side}")
        k.add(f"fist{side}", stone(FIST * m, (0.068, 0.07, 0.076), rot=(0, 0, -10 * s), seed=3 + (s > 0), cham=0.36),
              ROCK, f"arm{side}", fuse=False)
        # the knuckles: a second stone across the fist's front
        k.add(f"knuckle{side}", stone((FIST + (0.0, -0.045, -0.005)) * m, (0.06, 0.035, 0.05), seed=7 + (s > 0),
                                      cham=0.4), ROCKSHADE, f"arm{side}", fuse=False)


def legs(k):
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"leg{side}", tube([np.array([0.075, 0.0, 0.14]) * m, np.array([0.082, -0.005, 0.07]) * m],
                                 [0.042, 0.04]), DEEP, f"leg{side}")
        k.add(f"foot{side}", stone(FOOT * m, (0.058, 0.068, 0.042), rot=(0, 0, 6 * s), seed=11 + (s > 0), cham=0.45),
              ROCK, f"leg{side}", fuse=False)


RECT = polygon([(-1, -1), (1, -1), (1, 1), (-1, 1)])


def glyph_eyes(k, state, xs, z, half, face_y, slots, lid=DEEP, glow=GLOW, slot=SLOT, normal=(0, -1.0, 0)):
    """
    Eyes that are glyphs of light: a yellow upright block each, cut by a dark slot from its top (slots: each eye's
    slot as (u of its middle, its half width, how far down it reaches), in the block's own units). Shut, squeeze and
    smile are the band's colour with a yellow line; fierce lays a slanting lid of the band's colour over the top.
    """
    for x, sl in zip(xs, slots):
        c = (x, face_y, z)
        if state in ("Open", "Fierce"):
            k.decal(c, normal, half, glow, eps=0.0025, outline=RECT, rings=12, segs=96)
            u0, w, reach = sl
            # a patch's outline must hold its own middle: the slot is laid as a patch about the slot's middle
            sc = (x + u0 * half[0], face_y, z + (1 - reach) * half[1])
            k.decal(sc, normal, (w * half[0], reach * half[1]), slot, eps=0.0035, outline=RECT, rings=6, segs=48)
            if state == "Fierce":
                # a lid of the band's colour slanting down to the inner corner (u toward the middle is "i")
                i = -1 if x > 0 else 1
                lidline = polygon([(-i * 1.0, 1.0), (-i * 1.0, 0.1), (i * 1.0, -0.9), (i * 1.0, 1.0)])
                k.decal((x, face_y, z + half[1] * 0.55), normal, (half[0] * 1.2, half[1] * 0.5), lid, eps=0.0045,
                        outline=lidline, rings=10, segs=72)
        else:
            look = eye_look(state, lambda u, v: np.tile(srgb(glow), (len(u), 1)), srgb(lid), srgb(glow), line=0.22)
            k.decal(c, normal, (half[0] * 1.2, half[1]), look, eps=0.0025, outline=RECT, rings=20, segs=56)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="shell")
    body(k)
    head(k)
    arms(k)
    legs(k)
    k.build(voxel=VOXEL, smooth=4, band=0.006)
    # the left eye (the viewer's) cut down its middle, the right one nearer its inner side
    k.eye_states(lambda state: glyph_eyes(k, state, (-EYE_X, EYE_X), EYE_Z, EYE_HALF, FACE_Y,
                                          ((0.0, 0.3, 0.58), (-0.32, 0.3, 0.58))))
    k.export(out)
