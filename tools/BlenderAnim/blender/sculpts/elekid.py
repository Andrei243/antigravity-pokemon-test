# Elekid sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/elekid.py -- <the export's Elekid.glb> <out.glb>
#
# The design, from the references: one yellow egg that is head and body together; two flat prongs on top like a
# plug's pins, each with a hole near its top; scowling eyes (white, a dark pupil at the inner corner, a heavy dark
# line along the top edge sloping to the nose); a small dark mouth with two white fangs; a dark band round the
# lower body with a dark bolt hanging from it over the belly; thin arms ending in great round forearms with three
# dark bands and a yellow fist with three white claws; short stubby feet. No tail.
#
# Written to be grown from (Electabuzz, Electivire): the line's colours, the band (a loose ring standing just off
# the body, so its edges stay crisp), the bolt (convex patches on the fused front), the eye and the mouth are named
# functions below; the calls are at the end.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, sheet, srgb, inside, smoothstep, unit, boxy, eye_look, polygon, args

# --- the line's colours ---------------------------------------------------------------------------------------------
YELLOW, INK, CLAW = "#F6D232", "#33363C", "#F2F2F0"
WHITE, PUPIL, MOUTH = "#FAFAF6", "#1E1E24", "#3A2228"
HOLE = "#B8921C"                         # the lining of a prong's hole
CY, CI, CW, CP, CM = srgb(YELLOW), srgb(INK), srgb(WHITE), srgb(PUPIL), srgb(MOUTH)


def ring_band(c, axis, radius, length, thick=0.006, nu=73):
    """
    A loose ring round a limb or a body (a stripe that keeps its edges): its middle at c, along axis, radius(t) for
    t in 0..1 along its length (the surface's own radius plus a little), its seam behind.
    """
    a = unit(*axis)
    ref = np.array([0, 0, 1.0]) if abs(a[2]) < 0.9 else np.array([0, 1.0, 0])
    e1 = np.cross(a, ref); e1 /= np.linalg.norm(e1)
    e2 = np.cross(a, e1)
    # the seam where a ray from the axis points back (+Y)
    back = np.array([0, 1.0, 0]) - a * a[1]
    phase = math.atan2(np.dot(back, e2), np.dot(back, e1)) if np.linalg.norm(back) > 1e-6 else 0.0
    def at(u, v):
        th = phase + 2 * math.pi * u
        r = radius(v)
        off = (v - 0.5) * length
        return (np.asarray(c)[None, None, :] + a[None, None, :] * off[..., None]
                + r[..., None] * (np.cos(th)[..., None] * e1[None, None, :] + np.sin(th)[..., None] * e2[None, None, :]))
    return sheet(at, nu, 3, thick)


def body_ring(z0, width, rx, ry, cy=0.0, lift=0.004, thick=0.006):
    """A loose band round a body whose cross-section at height z is an ellipse rx(z), ry(z) about (0, cy)."""
    def at(u, v):
        th = 2 * math.pi * u + math.pi / 2          # the seam behind (+Y)
        z = z0 + (v - 0.5) * width
        return np.stack([np.cos(th) * (rx(z) + lift), cy + np.sin(th) * (ry(z) + lift), z], -1)
    return sheet(at, 97, 3, thick)


def ell_radius(c, r):
    """The cross-section radii of an ellipsoid at a height, for a band round it."""
    def rx(z):
        k = np.sqrt(np.clip(1 - ((z - c[2]) / r[2]) ** 2, 0.02, 1))
        return r[0] * k
    def ry(z):
        k = np.sqrt(np.clip(1 - ((z - c[2]) / r[2]) ** 2, 0.02, 1))
        return r[1] * k
    return rx, ry


def convex_patch(k, pts, centre, normal, color, up=(0, 0, 1), eps=0.004, segs=96, rings=12):
    """A crisp mark of corners (a bolt's piece) laid on the fused surface: pts in metres in the patch's own plane."""
    pts = np.array(pts, float)
    mid = pts.mean(axis=0)
    rel = pts - mid
    hx, hy = np.abs(rel[:, 0]).max(), np.abs(rel[:, 1]).max()
    n = unit(*normal)
    right = np.cross(np.array(up, float), n); right /= np.linalg.norm(right)
    upv = np.cross(n, right)
    c = np.array(centre, float) + right * mid[0] + upv * mid[1]
    k.decal(c, n, (hx, hy), color, up=up, eps=eps, outline=polygon(rel / np.array([hx, hy])), rings=rings, segs=segs)


SCOWL = [(-1.0, 0.02), (-0.4, 0.42), (0.3, 0.76), (1.0, 0.98), (0.98, 0.3), (0.72, -0.4), (0.22, -0.72),
         (-0.38, -0.68), (-0.82, -0.36)]


def scowl_outline(flip=False):
    """The eye's almond: its top edge sloping down to the nose (u < 0 inward), the outer corner high. flip: for the
    eye on -X, whose patch's u runs inward (a mirrored patch's outline is not mirrored, so each eye is laid alone)."""
    return polygon([(-u, v) for u, v in SCOWL] if flip else SCOWL)


def pair_of_eyes(k, E, de, half, look, eps=0.005):
    """Two eyes laid one at a time, the -X one with its outline and colour turned so that +u is outward on both."""
    k.decal(E, de, half, look, eps=eps, outline=scowl_outline(), rings=20, segs=56)
    m = np.array([-1, 1, 1.0])
    k.decal(np.array(E) * m, np.array(de) * m, half, lambda u, v: look(-u, v), eps=eps,
            outline=scowl_outline(flip=True), rings=20, segs=56)


def scowl_top(u):
    """The top edge's height along u (the polygon's own line), for the dark brow line drawn under it."""
    return np.interp(u, [-1.0, -0.4, 0.3, 1.0], [0.02, 0.42, 0.76, 0.98])


def scowl_eye(pupil_at=(-0.26, -0.1), pupil_r=(0.4, 0.48), brow=0.2, iris=None, iris_r=None):
    """An angry eye: white, a solid pupil toward the inner corner, a heavy dark line along the top edge."""
    def eye(u, v):
        c = np.tile(CW, (len(u), 1))
        if iris is not None:
            c[((u - pupil_at[0]) / iris_r[0]) ** 2 + ((v - pupil_at[1]) / iris_r[1]) ** 2 < 1] = iris
        c[((u - pupil_at[0]) / pupil_r[0]) ** 2 + ((v - pupil_at[1]) / pupil_r[1]) ** 2 < 1] = CP
        c[v > scowl_top(u) - brow] = CI
        return c
    return eye


def fanged_mouth(fangs=(-0.42, 0.42), fang_w=0.24, fang_down=-0.35):
    """A small dark mouth with two white fangs hanging from its top edge."""
    def mouth(u, v):
        c = np.tile(CM, (len(u), 1))
        for f in fangs:
            t = (v - fang_down) / (1.0 - fang_down)
            c[(t > 0) & (np.abs(u - f) < fang_w * t)] = CW
        return c
    return mouth


def lens(w=0.62):
    """A wide shallow mouth's outline, its top edge waved a little."""
    return polygon([(-1, 0.1), (-0.6, 0.55), (-0.2, 0.4), (0.2, 0.4), (0.6, 0.55), (1, 0.1), (0.7, -0.5), (0, -0.75),
                    (-0.7, -0.5)])


def striped_forearm(k, E, D, length, radii, bands, bone, fist_r, claw_len=0.04):
    """A great round forearm from E along D: yellow, dark loose bands at shares of its length, a fist with claws."""
    D = unit(*D)
    pts = [E + D * length * t for t in (0, 0.5, 1)]
    k.add("fore", tube(pts, radii), YELLOW, bone, mirror=True)
    for i, t in enumerate(bands):
        r = radii[0] + (radii[-1] - radii[0]) * t
        c = E + D * length * t
        k.add(f"band{i}", ring_band(c, D, lambda v, r=r: np.full_like(v, r + 0.004), 0.034), INK, bone, fuse=False,
              mirror=True)
    F = E + D * (length + fist_r * 0.35)
    k.add("fist", ell(F, (fist_r, fist_r, fist_r)), YELLOW, bone, mirror=True)
    # three white claws on the fist's end, the middle one forward
    side = np.cross(D, (0, 0, 1.0)); side /= np.linalg.norm(side)
    upv = np.cross(side, D)
    for i, (a, b) in enumerate([(0.0, 0.55), (0.55, -0.35), (-0.55, -0.35)]):
        o = (side * a + upv * b) * fist_r * 0.62
        base = F + o + D * fist_r * 0.55
        k.add(f"claw{i}", tube([base, base + D * claw_len * 0.6 + o * 0.15, base + D * claw_len], [0.013, 0.009, 0.002]),
              CLAW, bone, fuse=False, mirror=True, surface="shell")
    return F


# --- Elekid's own frame ---------------------------------------------------------------------------------------------
BODY_C, BODY_R = np.array([0, 0.0, 0.2]), np.array([0.154, 0.136, 0.16])
H, HR = np.array([0, -0.004, 0.262]), np.array([0.146, 0.13, 0.14])     # the upper egg: the head bone's
SHOULDER = np.array([0.1, -0.02, 0.225])
# The forearms hang forward and down beside the egg, the fists before its lower half: held out to the sides they
# made the model half as wide again as tall, and the game drew it small to fit them
ELBOW = np.array([0.175, -0.06, 0.21])
ARM_D = unit(-0.08, -0.75, -0.65)
FORE_LEN, FORE_R = 0.18, [0.04, 0.047, 0.053]
BAND_Z, BAND_W = 0.136, 0.028


def on_head(d, lift=0.0):
    d = unit(*d)
    s = 1 / math.sqrt(((d / HR) ** 2).sum())
    return H + d * (s + lift)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="fur")

    # The egg: a lower body and an upper head as one smooth mass
    k.add("body", ell(BODY_C, BODY_R), YELLOW, "body")
    k.add("head", ell(H, HR), YELLOW, "head")

    # The prongs: flat plates facing front, rising a little outward, each with a hole near its top
    k.add("prong", ell((0.062, 0.012, 0.44), (0.031, 0.015, 0.1), rot=(0, 6, 0), deform=boxy(3.5)), YELLOW, "head",
          mirror=True)
    k.add("hole", ell((0.067, 0.012, 0.495), (0.015, 0.05, 0.024), rot=(0, 6, 0)), HOLE, "head", carve=True,
          mirror=True)

    # Short stubby legs and round feet, a little forward
    k.add("leg", tube([(0.055, 0.0, 0.09), (0.06, -0.01, 0.05)], [0.04, 0.038]), YELLOW, "leg{s}", mirror=True)
    k.add("foot", ell((0.062, -0.03, 0.028), (0.042, 0.056, 0.03)), YELLOW, "leg{s}", mirror=True)

    # Thin arms and great banded forearms
    k.add("arm", tube([SHOULDER, ELBOW], [0.024, 0.022]), YELLOW, "arm{s}", mirror=True)
    striped_forearm(k, ELBOW, ARM_D, FORE_LEN, FORE_R, (0.32, 0.6, 0.86), "arm{s}", 0.054)

    # The band round the lower body
    rx, ry = ell_radius(BODY_C, BODY_R)
    k.add("belt", body_ring(BAND_Z, BAND_W, rx, ry, lift=0.002), INK, "body", fuse=False)

    # The bolt hanging from the band over the belly: two slanted pieces with a step between
    nb = unit(0, -1, -0.45)
    cb = np.array([0, -0.12, 0.098])
    convex_patch(k, [(-0.05, 0.036), (0.024, 0.036), (0.004, -0.006), (-0.064, -0.006)], cb, nb, INK)
    convex_patch(k, [(-0.02, 0.006), (0.056, 0.006), (0.01, -0.058)], cb, nb, INK)

    # The mouth: small and dark with two white fangs
    dm = unit(0, -1, -0.5)
    k.decal(on_head(dm), dm, (0.026, 0.012), fanged_mouth(), eps=0.004, outline=lens(), rings=16, segs=72)

    # The eyes: angry almonds, the top edge sloping to the nose
    de = unit(0.4, -0.9, -0.05)
    E = on_head(de)
    def eyes(state):
        pair_of_eyes(k, E, de, (0.037, 0.03), eye_look(state, scowl_eye(), CY, CI))

    k.build(voxel=0.005)
    k.eye_states(eyes)
    k.export(out)
