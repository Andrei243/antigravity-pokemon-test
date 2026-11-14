# Nosepass sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/nosepass.py -- <the export's Nosepass.glb> <out.glb>
# The first of its line (probopass.py is grown from this file: the same stone, nose red, mask and way of making eyes).
#
# The design, from the references: a statue's head of blue stone, cut in flat facets, that is all of the body: tall,
# its front flat, its top chamfered down toward the face and higher behind. Across the face a black band, a mask
# wrapping round to the face's side edges, and in it the eyes, always shut: a pale blue line curving down. Under the
# mask's middle the great red nose, the species' signature: a thick chevron seen from the side, its upper arm running
# straight forward to a blunt point, its lower arm coming back down to the face, a darker notch between the two. Under
# the nose a broad block of stone standing forward like a chin. Two stubby arms of stone out of the sides at the middle,
# each a block with a black hole in its end; two short blocks for feet, each with a black hole in its front.
# All the stone is hard loose pieces (convex hulls drawn flat-faced, as rhyhorn.py's plates are); the mask's middle
# is a thin fused plate standing just inside the mask's front, which the eyes' patches lie on (combee.py's way).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import bmesh
import numpy as np
from sculptkit import Kit, ell, srgb, unit, boxy, eye_look, args

# --- the line's colours -------------------------------------------------------------------------------------------
STONE, RED, REDDARK, MASK, SLIT, HOLE = "#82A4DA", "#E2505A", "#A22E38", "#1C1E26", "#A8CCF2", "#16181E"


def shade(h, f):
    """A colour a little lighter or darker (f above or below 1), still written as on screen."""
    c = np.clip(np.array([int(h[i:i + 2], 16) for i in (1, 3, 5)]) * f, 0, 255).astype(int)
    return "#%02X%02X%02X" % tuple(c)


def hull(V):
    """Points made into a solid: their convex hull, its coplanar faces merged, every face wound outward."""
    bm = bmesh.new()
    for v in np.asarray(V).tolist():
        bm.verts.new(v)
    h = bmesh.ops.convex_hull(bm, input=bm.verts[:])
    bmesh.ops.delete(bm, geom=list({g for g in h["geom_interior"] + h["geom_unused"] if isinstance(g, bmesh.types.BMVert)}), context="VERTS")
    bmesh.ops.dissolve_limit(bm, angle_limit=math.radians(3), verts=bm.verts[:], edges=bm.edges[:])
    bm.verts.index_update()
    P = np.array([v.co[:] for v in bm.verts])
    F = [[v.index for v in f.verts] for f in bm.faces]
    bm.free()
    mid = P.mean(axis=0)
    out = []
    for f in F:
        q = P[f]
        n = np.zeros(3)
        for i in range(len(q)):                      # Newell's normal
            a, b = q[i], q[(i + 1) % len(q)]
            n += np.cross(a, b)
        out.append(f if np.dot(n, q.mean(axis=0) - mid) > 0 else f[::-1])
    return P, out


def frame(axis):
    """Two directions square to an axis, right-handed: e1 x e2 = axis (e1 level where it can be)."""
    a = np.array(axis, float); a /= np.linalg.norm(a)
    ref = np.array([0, 0, 1.0]) if abs(a[2]) < 0.9 else np.array([1.0, 0, 0])
    e1 = np.cross(ref, a); e1 /= np.linalg.norm(e1)
    e2 = np.cross(a, e1)
    return a, e1, e2


def octagon(c, e1, e2, a, b, ch):
    """A rectangle of half sizes a (along e1) and b (along e2) about c with its corners cut by ch of the smaller."""
    cut = ch * min(a, b)
    return [c + e1 * x + e2 * y for x, y in ((a - cut, b), (a, b - cut), (a, -b + cut), (a - cut, -b), (-a + cut, -b),
                                              (-a, -b + cut), (-a, b - cut), (-a + cut, b))]


ROCKS = []              # the loose pieces drawn flat-faced


def k_add_rock(k, name, shape, color, bone):
    k.add(name, shape, color, bone, fuse=False, surface="shell")
    ROCKS.append(name)


def block(k, name, p0, p1, half0, half1, bone, color, ch=0.3):
    """A block of stone from p0 to p1, its section a chamfered rectangle: half0 at p0, half1 at p1 (along e1, e2 of
    the axis's frame). Hard-edged: a convex hull drawn flat."""
    p0, p1 = np.array(p0, float), np.array(p1, float)
    a, e1, e2 = frame(p1 - p0)
    pts = octagon(p0, e1, e2, *half0, ch) + octagon(p1, e1, e2, *half1, ch)
    k_add_rock(k, name, hull(np.array(pts)), color, bone)


def disc(k, name, c, n, a, b, color, bone, up=(0, 0, 1), t=0.003):
    """A flat loose oval lying on a face (a hole, a mark): a squashed ball in a right-handed frame (e1 x e2 = n)."""
    n = unit(*n)
    e2 = np.array(up, float) - n * np.dot(up, n); e2 /= np.linalg.norm(e2)
    e1 = np.cross(e2, n)
    V, F = ell((0, 0, 0), (a, b, t), sub=3)
    V = V @ np.stack([e1, e2, n]) + np.array(c, float) + n * t * 0.4
    k.add(name, (V, F), color, bone, fuse=False, surface="shell")


# --- the frame: the statue's head is the body --------------------------------------------------------------------
# (z, half width, front, back) up the head: wider at the foot, the front flat, the top chamfered toward the face
PROFILE = [(0.09, 0.14, -0.115, 0.125), (0.26, 0.166, -0.137, 0.142), (0.50, 0.16, -0.132, 0.137),
           (0.575, 0.136, -0.093, 0.125), (0.625, 0.093, -0.022, 0.082)]
CHAMFER = 0.27
MASK_Z = (0.366, 0.476)          # the black band across the eyes
EYE_Z, EYE_X, EYE_H = 0.436, 0.08, (0.042, 0.03)
LINE = 0.28                      # the shut eye's line, a share of the patch's half height
NOSE_ROOT, NOSE_TIP, NOSE_LOW = (0, -0.11, 0.405), (0, -0.315, 0.455), (0, -0.11, 0.335)
NOSE_W = (0.058, 0.042)          # the nose's half width at its root and at its point
NOSE_H = (0.038, 0.034)          # the upper arm's half height at its root and its point (the lower arm's 0.85 of it)
ARM0, ARM1 = (0.1, 0.01, 0.285), (0.232, -0.01, 0.255)       # short, a little down: the hole on the broad end
ARM_HALF = ((0.045, 0.042), (0.066, 0.056))
FOOT_X, FOOT_Y, FOOT_TOP = 0.085, 0.0, 0.12


def section(z, profile=PROFILE):
    """The head's (half width, front, back) at a height, between the profile's rows."""
    P = np.array(profile)
    return [float(np.interp(z, P[:, 0], P[:, i])) for i in (1, 2, 3)]


def ring(z, profile=PROFILE, ch=CHAMFER, grow=0.0):
    """The head's section at a height as an octagon (its front y negative), pushed out by grow."""
    hx, yf, yb = section(z, profile)
    cy = (yf + yb) / 2
    pts = octagon(np.array([0, cy, z]), np.array([1.0, 0, 0]), np.array([0, 1.0, 0]), hx, (yb - yf) / 2, ch)
    if grow:
        pts = [p + np.array([*(unit(p[0], p[1] - cy, 0)[:2] * grow), 0]) for p in pts]
    return pts


def head(k, name="head", profile=PROFILE, ch=CHAMFER, color=STONE):
    """The body: a hull of the profile's octagons."""
    pts = [p for z, *_ in profile for p in ring(z, profile, ch)]
    k_add_rock(k, name, hull(np.array(pts)), color, "body")


def mask(k, z0, z1, profile=PROFILE, ch=CHAMFER, plate_half=0.124):
    """The black band over the face's front and its front corners, standing 0.005 proud; and inside its front a thin
    fused plate for the eyes' patches (its front 0.001 behind the band's, so the patches stand 0.005 before it)."""
    pts = []
    for z in (z0, z1):
        oct_out = ring(z, profile, ch, grow=0.005)
        oct_side = ring(z, profile, ch, grow=-0.002)   # the band's ends kept inside the sides, unseen from behind
        oct_in = ring(z, profile, ch, grow=-0.03)
        for i in (2, 3, 4, 5):                       # the front edge and the two front corners' faces (front is -y)
            pts += [(oct_side if i in (2, 5) else oct_out)[i], oct_in[i]]
    k_add_rock(k, "mask", hull(np.array(pts)), MASK, "body")
    zm = (z0 + z1) / 2
    hx, yf, yb = section(zm, profile)
    front = yf - 0.004
    k.add("plate", ell((0, front + 0.006, zm), (plate_half, 0.006, (z1 - z0) / 2 * 0.8), deform=boxy(6)), MASK, "body")
    return front


def nose(k, root=NOSE_ROOT, tip=NOSE_TIP, low=NOSE_LOW, w=NOSE_W, h=NOSE_H, bone="body"):
    """The red nose: a chevron seen from the side, two thick arms meeting at a blunt point, a dark notch between."""
    root, tip, low = (np.array(p, float) for p in (root, tip, low))
    X = np.array([1.0, 0, 0])
    def arm(p0, p1, h0, h1, name):
        d = unit(*(p1 - p0))
        n = np.cross(X, d)                             # up, square to the arm in its own plane
        if n[2] < 0:
            n = -n
        pts = []
        for p, ww, hh in ((p0 - d * 0.02, w[0], h0), (p1, w[1], h1)):
            pts += octagon(p, X, n, ww, hh, 0.22)
        k_add_rock(k, name, hull(np.array(pts)), RED, bone)
    arm(root, tip, h[0], h[1], "nose")
    arm(low, tip, h[0] * 0.85, h[1] * 0.95, "noselow")
    # the notch's back: a darker wedge deep between the arms, narrower than they are
    mid = (root + low) / 2
    pts = octagon(mid + (0, -0.02, 0), X, np.array([0, 0, 1.0]), w[0] * 0.8, (root[2] - low[2]) / 2 * 0.9, 0.3)
    pts += [root + (tip - root) * 0.5 + (s * w[0] * 0.6, 0.0, -h[0] * 0.5) for s in (1, -1)]
    pts += [low + (tip - low) * 0.5 + (s * w[0] * 0.6, 0.0, h[0] * 0.4) for s in (1, -1)]
    k_add_rock(k, "notch", hull(np.array(pts)), REDDARK, bone)


def chin(k, z0=0.12, z1=0.255, reach=0.075, profile=PROFILE, color=STONE):
    """The broad block of stone standing forward under the nose."""
    hx, yf, yb = section((z0 + z1) / 2, profile)
    back = [(sx * hx * 0.88, yf + 0.03, z) for sx in (1, -1) for z in (z0, z1)]
    front = [(sx * hx * 0.66, yf - reach, z) for sx in (1, -1) for z in (z0 + 0.025, z1 - 0.045)]
    mid = [(sx * hx * 0.84, yf - reach * 0.7, (z0 + z1) / 2 + 0.01) for sx in (1, -1)]
    top = [(sx * hx * 0.7, yf - reach * 0.55, z1 - 0.005) for sx in (1, -1)]
    k_add_rock(k, "chin", hull(np.array(back + front + mid + top)), shade(color, 0.97), "body")


def arms(k, a0=ARM0, a1=ARM1, half=ARM_HALF, color=STONE, holes=True):
    """The stubby arms: a block out of each side, a black hole in its end."""
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        p0, p1 = np.array(a0) * m, np.array(a1) * m
        block(k, "arm" + side, p0, p1, half[0], half[1], "arm" + side, shade(color, 1.02))
        if holes:
            d = unit(*(p1 - p0))
            disc(k, "armhole" + side, p1, d, 0.027, 0.031, HOLE, "arm" + side)


def feet(k, color=STONE):
    """Two short blocks under the head, a black hole in each one's front, turned a little out."""
    for s, side in ((1, "R"), (-1, "L")):
        c = np.array([s * FOOT_X, FOOT_Y, 0.0])
        block(k, "foot" + side, c + (0, 0, 0.0), c + (0, 0, FOOT_TOP), (0.07, 0.085), (0.065, 0.075), "leg" + side,
              shade(color, 0.95), ch=0.6)
        # on the broad outer front corner, turned out
        n = unit(s, -1, 0)
        at = c + np.array([s * 0.0635, -0.0485, 0.06])
        disc(k, "foothole" + side, at, n, 0.019, 0.024, HOLE, "leg" + side)


# --- the eyes: always shut, a pale line curving down on the black mask ------------------------------------------------
CM, CS = srgb(MASK), srgb(SLIT)


def slit(u, v):
    """The shut eye, which is its open state too (snorlax.py's rule): a pale line curving down."""
    c = np.tile(CM, (len(u), 1))
    c[(np.abs(u) < 0.82) & (np.abs(v - (-0.05 - 0.3 * (1 - u * u))) < LINE)] = CS
    return c


def fierce(u, v):
    """The same line tilted, low toward the nose (u outward is +)."""
    c = np.tile(CM, (len(u), 1))
    c[(np.abs(u) < 0.82) & (np.abs(v - (-0.12 + 0.38 * u)) < LINE)] = CS
    return c


def shut_eyes(k, state, front, z=EYE_Z, x=EYE_X, half=EYE_H):
    look = fierce if state == "Fierce" else (slit if state == "Open" else eye_look(state, slit, CM, CS, line=LINE))
    k.decal((x, front, z), (0, -1, 0), half, look, up=(0, 0, 1), eps=0.006, rings=20, segs=56, mirror=True)


def finish(k, voxel=0.005, smooth=2):
    """Builds the body with every stone piece drawn flat-faced: the mesh's faces are the fused surface's, then the
    loose pieces' in the order they were added, then the patches'."""
    loose = [(p["name"], len(p["F"])) for p in k.parts if not p["fuse"] and not p["carve"]]
    body = k.build(voxel=voxel, smooth=smooth)
    me = body.data
    flags = np.ones(len(me.polygons), bool)
    at = len(k.surfaces[k.name][1])
    for name, count in loose:
        if name in ROCKS:
            flags[at:at + count] = False
        at += count
    me.polygons.foreach_set("use_smooth", flags.tolist())
    return body


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="shell")
    head(k)
    front = mask(k, *MASK_Z)
    nose(k)
    chin(k)
    arms(k)
    feet(k)
    finish(k)
    k.eye_states(lambda state: shut_eyes(k, state, front))
    k.export(out)
