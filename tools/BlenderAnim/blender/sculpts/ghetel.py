# Ghetel (No. 10137, Carpathia) sculpted from nothing on the skeleton and clips of the game's own export of it:
#   blender --background --factory-startup --python sculpts/ghetel.py -- <the export's Ghetel.glb> <out.glb>
#
# From its picture (docs/art/reference/gemini/carpathia/10137-Ghetel.jpg) and its brief (CP-dex-3-river-and-caves.md,
# 137): a small creature of clear blue ice shaped like an icicle turned point down. Largest first: a faceted icicle
# of a body tapering to a point it stands on, a crystal's rim where it meets the head, a shard standing off its lower
# flank; a round smooth dome of a head with the face on it; two small ice nubs for arms at its sides. Big slate-blue
# eyes under frosty white tufts of brow that slope down to the nose (a glower), a small open mouth, glossy glints on
# the dome, bubbles of trapped air in the ice.
# The export has no sculptor (a spiky ball by rule): root, and body (0.333); everything is on body. Its longest side
# (0.95, the ball with its spikes) is the picture's longest, the height, so Ghetel stands 0.9 tall.
#
# Written to be grown from (stalaghet.py imports it with sibling("ghetel")): the palette, on_ell, spike, the eyes, the
# brows and the mouth are functions here; the calls under __main__.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, outward, srgb, inside, smoothstep, unit, polygon, eye_look, args

# --- the line's colours --------------------------------------------------------------------------------------------
ICE, ICEDOME, ICEDEEP, ICEPALE, FROST = "#A9D3EE", "#C3E3F5", "#7DB3DE", "#DDF0FA", "#F6FBFE"
EYEWHITE, IRIS, PUPIL, EYERIM, GLINT = "#F4FAFE", "#3E5670", "#1C2838", "#24344A", "#FFFFFF"
MOUTH, TONGUE, CHEEK = "#2A3C54", "#7FA6C8", "#9FCCEA"

HC = np.array([0.0, 0.0, 0.63])
HR = np.array([0.218, 0.192, 0.27])
VOXEL = 0.0065


def on_ell(a, z, c=HC, r=HR):
    """The point of an ellipsoid at azimuth a (radians from the front, + toward +X) and height z, and its normal."""
    e = math.asin(float(np.clip((z - c[2]) / r[2], -1, 1)))
    q = np.array([math.sin(a) * math.cos(e), -math.cos(a) * math.cos(e), math.sin(e)])
    p = np.array(c, float) + q * np.array(r, float)
    n = q / np.array(r, float)
    return p, n / np.linalg.norm(n)


def spike(base, d, length, width, sides=6, twist=0.3, mid=0.55, bend=None, depth=0.8):
    """An ice spike or stalagmite: a hexagonal prism from base along d, narrowing to a mid ring and a point. A hull."""
    d = unit(*d)
    a = np.cross(d, (0, 0, 1.0)) if abs(d[2]) < 0.95 else np.cross(d, (1.0, 0, 0))
    a /= np.linalg.norm(a); b = np.cross(d, a)
    base = np.array(base, float)
    pts = []
    for h, w in ((0.0, width), (mid * length, width * 0.62)):
        for j in range(sides):
            t = twist + 2 * math.pi * j / sides
            pts.append(base + d * h + w * (math.cos(t) * a + depth * math.sin(t) * b))
    tip = base + d * length
    if bend is not None:
        tip = tip + np.array(bend, float)
    pts.append(tip)
    return hull(pts)


def eye_open(u, v):
    """A big round eye: a white with a large slate iris toward the nose, a solid pupil, two glints, a dark rim."""
    c = np.tile(srgb(EYEWHITE), (len(u), 1))
    c[((u + 0.08) / 0.72) ** 2 + ((v + 0.06) / 0.78) ** 2 < 1] = srgb(IRIS)
    c[((u + 0.08) / 0.38) ** 2 + ((v + 0.06) / 0.44) ** 2 < 1] = srgb(PUPIL)
    c[((u - 0.22) / 0.2) ** 2 + ((v - 0.3) / 0.2) ** 2 < 1] = srgb(GLINT)
    c[((u + 0.34) / 0.1) ** 2 + ((v + 0.36) / 0.1) ** 2 < 1] = srgb(GLINT)
    c[u * u + v * v > 0.72] = srgb(EYERIM)
    return c


# The eye's outline: round, its top cut by a line sloping down toward the nose (the brow sits on that line)
def eye_outline(drop=0.42, slope=0.38):
    pts = []
    for t in np.linspace(math.pi, 2 * math.pi, 14):           # the lower half round
        pts.append((math.cos(t), math.sin(t)))
    for t in np.linspace(0, math.pi, 15):                       # the upper half, cut by the slope
        x, y = math.cos(t), math.sin(t)
        pts.append((x, min(y, 1 - drop + slope * x)))
    return polygon(pts)


BROW = [(-1.0, -0.45), (-0.4, -0.75), (0.4, -0.55), (1.0, -0.2), (1.0, 0.25), (0.78, 0.7), (0.55, 0.3),
        (0.32, 0.75), (0.08, 0.32), (-0.2, 0.62), (-0.42, 0.18), (-0.72, 0.38), (-0.85, 0.0)]


def lay_face(k, c, r, a, z, half, lid, tilt=20.0, brow_half=None, brow_lift=0.62, brow_colour=FROST):
    """
    The eyes and their brows: returns lay(state), which lays both eyes for a state (the brows in every state). Each
    eye's top is cut by the brow's sloping line; the brow is a frosty white tuft over it, its inner end lower.
    """
    bh = brow_half or (half[0] * 1.35, half[1] * 0.55)
    ang = math.radians(tilt)

    def frame(s):
        p, n = on_ell(a * s, z, c, r)
        right = np.cross((0, 0, 1.0), n); right /= np.linalg.norm(right); vu = np.cross(n, right)
        return p, n, right, vu

    def brow(s):
        p, n, right, vu = frame(s)
        up = vu * math.cos(ang) - s * right * math.sin(ang)
        bc = p + vu * half[1] * brow_lift + right * s * 0.1 * half[0]
        def col(u, v):
            cc = np.tile(srgb(brow_colour), (len(u), 1))
            cc[v < -0.35] = srgb(ICEPALE)
            return cc
        o = BROW if s > 0 else [(-x, y) for x, y in BROW]
        k.decal(bc, n, bh, col, up=up, eps=0.0042, rings=10, segs=60, outline=polygon(o))

    def lay(state):
        for s in (1, -1):
            p, n, right, vu = frame(s)
            look = eye_look(state, eye_open, srgb(lid), srgb(EYERIM), line=0.15)
            lk = look if s > 0 else (lambda u, v, f=look: f(-u, v))
            # the slope goes down toward the nose: +u is outward on +X, so the left eye's outline is turned round
            ol = eye_outline() if s > 0 else (lambda th, f=eye_outline(): f(math.pi - th))
            k.decal(p + n * 0.0004 * s, n, half, lk, eps=0.0025, rings=20, segs=56, outline=ol)
            brow(s)
    return lay


def mouth(k, p, n, half):
    """A small open mouth, dark with a pale tongue low in it."""
    def col(u, v):
        cc = np.tile(srgb(MOUTH), (len(u), 1))
        cc[((u - 0.05) / 0.55) ** 2 + ((v + 0.62) / 0.4) ** 2 < 1] = srgb(TONGUE)
        return cc
    k.decal(p, n, half, col, eps=0.0025, rings=10, segs=48,
            outline=polygon([(-1.0, 0.3), (-0.6, 0.75), (0.2, 0.9), (0.85, 0.6), (1.0, 0.0), (0.6, -0.75),
                             (0.0, -0.95), (-0.65, -0.7)]))


def board(ob):
    me = ob.data
    V = np.empty(len(me.vertices) * 3); me.vertices.foreach_get("co", V); V = V.reshape(-1, 3)
    a = math.radians(31.5)
    proj = V[:, 0] * math.cos(a) + V[:, 1] * math.sin(a)
    h, w, l = V[:, 2].max(), np.ptp(V[:, 0]), np.ptp(V[:, 1])
    print("KIT: board: shows %.3f in a square of %.3f (four-corner sum %.3f)"
          % (np.ptp(proj), 1.3 * max(h, 0.85 * max(w, l)), 0.853 * w + 0.522 * l))


EYE_A, EYE_Z, EYE_HALF = 0.37, 0.685, (0.052, 0.05)


# Ghetel's own body (the correction round): one piece of ice. The dome is widest at the cheeks and runs straight down
# into a hull of eight vertical facets whose corners stand on the dome's widest ring, so there is no rim and no step;
# the hull stays nearly as wide for the upper third below the face, then narrows to the point, two corners chipped.
# These are used by ghetel() alone: the helpers above, which Stalaghet takes, are as they were.
DC = np.array([0.0, 0.0, 0.6])
DR = np.array([0.235, 0.205, 0.3])
WIDE = 1.04                                             # the corners stand this far out of the dome's widest ring
CRACK, CORE = "#6E9CC8", "#CCE8F7"
FACETS = 8
ROT = 0.2                                               # the corners' turn: corner j at ROT + j * 2 pi / FACETS


def ice_body(rs):
    """The hull's points: rings of eight corners (z, share of the dome's widest radii), two corners chipped."""
    rings = ((0.592, WIDE), (0.45, WIDE * 0.975), (0.3, 0.64), (0.15, 0.3))
    chipped = {2, 5}
    pts = []
    for ri, (z, f) in enumerate(rings):
        for j in range(FACETS):
            t = ROT + 2 * math.pi * j / FACETS
            if ri in (1, 2) and j in chipped:
                for dt in (-0.09, 0.09):                    # a chipped edge: the corner cut by a narrow face
                    pts.append((DR[0] * f * 0.95 * math.sin(t + dt), -DR[1] * f * 0.95 * math.cos(t + dt),
                                z + rs.uniform(-0.01, 0.01)))
                continue
            jz = 0.0 if ri == 0 else rs.uniform(-0.012, 0.012)
            jf = 1.0 if ri == 0 else 1 + rs.uniform(-0.03, 0.02)
            pts.append((DR[0] * f * jf * math.sin(t), -DR[1] * f * jf * math.cos(t), z + jz))
    pts.append((0.006, -0.008, 0.0))
    return pts, rings


def corner_line(rings, j):
    """A corner of the hull from its top ring down, as points (a facet's edge)."""
    t = ROT + 2 * math.pi * j / FACETS
    return [(DR[0] * f * math.sin(t), -DR[1] * f * math.cos(t), z) for z, f in rings]


def ghetel(src, out_path):
    from mathutils import Vector
    from mathutils.bvhtree import BVHTree
    from mathutils.kdtree import KDTree
    k = Kit(src, coat="shell")
    rs = np.random.RandomState(137)

    # --- the dome of a head, widest at the cheeks -----------------------------------------------------------------
    def tuck(V):
        """Draws the dome's lower half in under the facets, so only the hull shows below the cheeks."""
        V = V.copy()
        V[:, :2] *= (1 - 0.1 * smoothstep(0.0, 0.15, -V[:, 2]))[:, None]
        return V
    k.add("head", ell(DC, DR, deform=tuck), ICEDOME, "body")

    # --- the icicle's facets ----------------------------------------------------------------------------------------
    pts, rings = ice_body(rs)
    HV, HF = hull(pts)
    k.add("icicle", (HV, HF), ICE, "body", fuse=False, surface="shell")
    tree = BVHTree.FromPolygons(HV.tolist(), HF)

    def on_ice(x, z, back=False):
        """The hull's surface seen from the front (or the back) at x, z: its point and outward normal."""
        o = Vector((x, 1.0 if back else -1.0, z))
        hit = tree.ray_cast(o, Vector((0, -1.0 if back else 1.0, 0)))
        n = np.array(hit[1])
        if (n[1] > 0) != back:
            n = -n
        return np.array(hit[0]), n

    # fine dark cracks over the front faces
    for ci, crack in enumerate((((0.075, 0.47), (0.058, 0.39), (0.064, 0.3)),
                                ((-0.085, 0.42), (-0.068, 0.35)),
                                ((-0.015, 0.24), (-0.004, 0.16)))):
        line = []
        for x, z in crack:
            q, n = on_ice(x, z)
            line.append(q + n * 0.002)
        k.add(f"crack{ci}", tube(line, [0.0046] * len(line), seg=8, per=4), CRACK, "body", fuse=False,
              surface="shell")
    # air bubbles inside it, showing as small caps on its faces, front and back
    for bi, (x, z, back) in enumerate(((0.1, 0.42, False), (-0.06, 0.48, False), (0.03, 0.33, False),
                                       (-0.04, 0.2, False), (0.12, 0.3, False), (0.05, 0.4, True),
                                       (-0.09, 0.3, True))):
        q, n = on_ice(x, z, back)
        k.add(f"bubble{bi}", outward(ell(q - n * 0.005, (0.014, 0.014, 0.014), sub=2)), ICEPALE, "body",
              fuse=False, surface="shell")

    # --- the nubs of arms at the chin's height, in the body's blue ---------------------------------------------
    for s, side in ((1, "R"), (-1, "L")):
        k.add("nub" + side, outward(ell((0.25 * s, -0.035, 0.535), (0.046, 0.04, 0.072), rot=(0, -32 * s, 0))),
              ICE, "body", fuse=False, surface="shell")

    # --- glints on the dome, two facet edges and a frost patch carried up its back, cheeks, the mouth ------------
    p, n = on_ell(-0.5, 0.83, DC, DR)
    k.decal(p, n, (0.05, 0.022), FROST, up=unit(0.5, 0, 1), eps=0.0025, rings=8, segs=40)
    p, n = on_ell(-0.75, 0.8, DC, DR)
    k.decal(p, n, (0.018, 0.014), FROST, eps=0.0025, rings=6, segs=32)
    p, n = on_ell(0.35, 0.86, DC, DR)
    k.decal(p, n, (0.03, 0.014), FROST, up=unit(-0.4, 0, 1), eps=0.0025, rings=6, segs=32)
    for j in (3, 4):
        t = ROT + 2 * math.pi * j / FACETS
        p, n = on_ell(t, 0.69, DC, DR)
        k.decal(p, n, (0.013, 0.085), FROST, eps=0.0025, rings=6, segs=28)
    p, n = on_ell(math.pi - 0.75, 0.8, DC, DR)
    k.decal(p, n, (0.055, 0.04), FROST, eps=0.0028, rings=8, segs=48,
            outline=polygon([(-1.0, 0.1), (-0.7, -0.55), (-0.2, -0.3), (0.2, -0.85), (0.6, -0.4), (1.0, -0.1),
                             (0.75, 0.5), (0.3, 0.3), (0.0, 0.85), (-0.45, 0.45)]))
    for s in (1, -1):
        p, n = on_ell(0.64 * s, 0.632, DC, DR)
        k.decal(p, n, (0.03, 0.017), CHEEK, eps=0.0022, rings=8, segs=36)
    p, n = on_ell(0.0, 0.618, DC, DR)
    mouth(k, p, n, (0.03, 0.022))

    body = k.build(voxel=VOXEL, smooth=4, band=0.008, wband=0.05)

    # The facets coloured face by face: a paler core down the front, the sides the ice's own, the back deeper
    me = body.data
    kd = KDTree(len(HV))
    for i, v in enumerate(HV.tolist()):
        kd.insert(v, i)
    kd.balance()
    nv = len(me.vertices)
    co = np.empty(nv * 3, np.float32); me.vertices.foreach_get("co", co); co = co.reshape(nv, 3)
    onhull = np.array([kd.find(q)[2] < 1e-5 for q in co.tolist()])
    attr = me.color_attributes["Color"]
    rgba = np.empty(len(me.loops) * 4, np.float32); attr.data.foreach_get("color", rgba); rgba = rgba.reshape(-1, 4)
    cI, cC, cD = srgb(ICE), srgb(CORE), srgb(ICEDEEP)
    for poly in me.polygons:
        if not all(onhull[v] for v in poly.vertices):
            continue
        nn = np.array(poly.normal)
        front = max(0.0, -nn[1])
        col = cI * (1 - smoothstep(0.7, 0.92, front)) + cC * smoothstep(0.7, 0.92, front)
        col = col * (1 - 0.45 * smoothstep(0.5, 0.9, nn[1])) + cD * 0.45 * smoothstep(0.5, 0.9, nn[1])
        col = col * (0.97 + 0.06 * ((poly.index * 7919) % 13) / 12)
        # a pale face beside each of two front corners and one at the back: the facets' edges catching the light
        m = np.array(poly.center)
        th = math.atan2(m[0], -m[1])
        for j in (0, 7, 4):
            t = ROT + 2 * math.pi * j / FACETS
            d = (th - t + math.pi) % (2 * math.pi) - math.pi
            if -0.5 < d < 0.0:
                col = col * 0.65 + srgb(FROST) * 0.35
        for li in poly.loop_indices:
            rgba[li, :3] = col
    attr.data.foreach_set("color", rgba.ravel())

    board(body)
    k.eye_states(lay_face(k, DC, DR, EYE_A, EYE_Z, EYE_HALF, ICEDOME))
    k.export(out_path)


if __name__ == "__main__":
    ghetel(*args())
