# Snivy sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/snivy.py -- <the export's Snivy.glb> <out.glb>
# From the references: a small green grass snake standing upright on two short cream legs. A round green cranium set
# a little behind the neck, a long pointed snout running forward and up from it, green on top and cream below, a small
# smile on its side; a big eye on each side under a heavy lid (white, a large red-brown iris toward the front, a dark
# pupil), a yellow crescent framing the eye from above and behind. A pear of a body, green behind, the cream running
# from the chin down the throat and the belly to the legs; small green arms hanging forward and down, three stubby
# fingers. A yellow collar round the back of the neck whose two ends hang back over the shoulders and curl up like the
# tails of a coat. A green tail from the low back, yellow beneath, sweeping back and out to the +X side and up into a
# great flat three-pointed leaf standing behind.
#
# Written to be grown from (servine.py imports it with sibling("snivy")): the line's colours, its head, eye, face and
# front paints, its arms, legs and leaves are functions taking a frame of named numbers (F); the build is under
# __main__.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from types import SimpleNamespace
import numpy as np
from sculptkit import Kit, ell, tube, blade, slab, srgb, inside, smoothstep, unit, eye_look, args, outward, sibling

T = sibling("treecko")          # on_ell, ell_frame, brow_cut

# --- the line's colours ---------------------------------------------------------------------------------------------
GREEN = "#3FA64C"
LEAF = "#2C8E52"
CREAM = "#F3EDB6"
YELLOW = "#F2C634"
IRIS = "#A8402E"
INK, WHITE = srgb("#1A1418"), srgb("#FFFFFF")
MOUTH = "#5A4A2E"
VOXEL = 0.0042


def frame_axes(d):
    """The snout's frame: across (x), along d, and up square to both (all in the y-z plane but x)."""
    d = unit(*d)
    up = np.array([0, 0, 1.0]) - d * d[2]; up /= np.linalg.norm(up)
    return np.array([1.0, 0, 0]), d, up


# --- the head ----------------------------------------------------------------------------------------------------------
def head(k, F):
    """A round cranium, a long tapering snout forward and up from its front, a full chin under it."""
    ex, d, up = frame_axes(F.snout_dir)
    k.add("cranium", ell(F.H, F.HR), GREEN, "head")
    taper = F.snout_taper

    def point(V):
        V = V.copy()
        t = (V[:, 1] + 1) / 2                      # 0 at the root, 1 at the tip
        w = 1 - taper * np.clip(t, 0, 1) ** 1.2
        V[:, 0] *= w; V[:, 2] *= w
        V[:, 2] += 0.12 * (1 - V[:, 1] ** 2) * (V[:, 2] > 0) * F.snout_flat   # a flatter top, like the crown's line
        V[:, 2] += getattr(F, "snout_curl", 0.0) * np.clip(t, 0, 1) ** 2      # the tip swept up, like a leaf's
        return V
    sc = F.H + d * F.snout_mid
    k.add("snout", T.ell_frame(sc, F.snout_r, ex, d, up, deform=point), GREEN, "head")
    k.add("chin", ell(F.H + F.chin_at, F.chin_r), GREEN, "head")


def snout_point(F, s, side, lift=0.0):
    """A point on the snout's side s along it from the cranium's middle, lift up the side (a share of its height)."""
    ex, d, up = frame_axes(F.snout_dir)
    t = (s - F.snout_mid) / F.snout_r[1]
    sph = math.sqrt(max(1 - t * t, 0.0))
    w = (1 - F.snout_taper * ((t + 1) / 2) ** 1.2) * sph
    rx, rz = F.snout_r[0] * w, F.snout_r[2] * w
    a = math.asin(max(-0.95, min(0.95, lift)))
    return F.H + d * s + ex * side * rx * math.cos(a) + up * rz * math.sin(a), unit(*(ex * side * math.cos(a) + up * math.sin(a)))


def face_cream(F):
    """The cream of the face: under a line running from the snout's tip along its middle back to under the eye."""
    ex, d, up = frame_axes(F.snout_dir)
    s0, s1, low = F.cream_line

    def mask(P):
        rel = P - F.H
        s = rel @ d
        h = rel @ up
        line = low + (0.0 - low) * smoothstep(s0, s1, s)
        near = inside(np.linalg.norm(rel - d[None] * np.clip(s, 0, F.snout_mid + F.snout_r[1])[:, None], axis=1) - F.face_reach, 0.004)
        return inside(h - line, 0.0035) * near * (P[:, 2] > F.H[2] - F.HR[2] - 0.03)
    return mask


def front_cream(F):
    """The cream down the front: the throat and the belly, within an angle of the front, down to the legs."""
    zs = [p[0] for p in F.front_axis]; ys = [p[1] for p in F.front_axis]
    z0, z1, ang = F.front_span

    def mask(P):
        x, y, z = P[:, 0], P[:, 1], P[:, 2]
        yc = np.interp(z, zs, ys)
        a = np.arctan2(np.abs(x), -(y - yc))
        return inside(a - ang, 0.03) * inside(z - z1, 0.004) * inside(z0 - z, 0.004)
    return mask


def mouth(k, F):
    """A small smile on the snout's side: a dark line curving up toward its back end."""
    c, n = snout_point(F, F.mouth_at[0], 1, F.mouth_at[1])
    ink, cream = srgb(MOUTH), srgb(CREAM)

    def look(u, v):
        col = np.tile(cream, (len(u), 1))
        line = -0.45 + 1.0 * ((u + 1) / 2) ** 2            # u toward the back: a smile rising at the back end
        col[(np.abs(v - line) < 0.24) & (np.abs(u) < 0.9)] = ink
        return col
    ex, d, up = frame_axes(F.snout_dir)
    k.decal(c, n, F.mouth_h, look, eps=0.0035, rings=8, segs=40, mirror=True, up=up)


# --- the eyes -----------------------------------------------------------------------------------------------------------
def lidded_eye(F):
    """The open eye (u outward = back, v up): white, a large red-brown iris toward the front, a dark pupil, a glint;
    a heavy dark line along the lid's cut and a thin dark rim below."""
    top, slope = F.lid
    iu, iv, ir = F.iris
    I, K = srgb(IRIS), INK

    def eye(u, v):
        c = np.tile(WHITE, (len(u), 1))
        di = np.hypot(u - iu, v - iv)
        c[di < ir] = I
        c[((u - iu) / (ir * 0.42)) ** 2 + ((v - iv) / (ir * 0.62)) ** 2 < 1] = K
        c[((u - iu + ir * 0.4) / (ir * 0.2)) ** 2 + ((v - iv - ir * 0.35) / (ir * 0.22)) ** 2 < 1] = WHITE
        c[np.hypot(u, v) > 0.86] = K
        c[v > top + slope * u - 0.16] = K
        return c
    return eye


def eyes(k, F):
    """The eyes' layer for a state: on the cranium's sides, turned forward, the top cut by the lid; laid by hand so that
    +u runs back on both."""
    de = unit(*F.eye_dir)
    E = T.on_ell(F.H, F.HR, de) + np.array(F.eye_shift, float)
    cut = T.brow_cut(*F.lid)
    look = lidded_eye(F)
    lid = srgb(F.lid_col)

    def fierce(u, v):
        c = look(u, v)
        edge = F.lid[0] - 0.25 - 0.45 * u           # the lid slanting down toward the snout
        c[v > edge] = lid
        c[np.abs(v - edge) < 0.15] = INK
        return c

    def lay(state):
        fn = fierce if state == "Fierce" else eye_look(state, look, lid, INK, line=0.16)
        m = np.array([-1, 1, 1.0])
        k.decal(E, de, F.eye_h, fn, eps=0.0045, rings=20, segs=56, outline=cut)
        k.decal(E * m, de * m, F.eye_h, lambda u, v: fn(-u, v), eps=0.0045, rings=20, segs=56,
                outline=lambda th: cut(math.pi - th))
    return lay, E, de


def eye_frame(k, F, E, de):
    """The yellow crescent behind and over the eye: an oval patch under it, set back and up."""
    c = E + np.array(F.frame_shift, float)
    k.decal(c, de, F.frame_h, YELLOW, eps=0.0028, rings=16, segs=56, mirror=True)


# --- limbs ----------------------------------------------------------------------------------------------------------------
def leg(k, s, side, F, color=CREAM):
    """A short thick leg: a thigh fused into the low belly, a short shin, a pointed foot."""
    m = np.array([s, 1, 1.0])
    bone = f"leg{side}"
    k.add(f"thigh{side}", ell(np.array(F.thigh_c) * m, F.thigh_r), color, bone)
    k.add(f"shin{side}", tube(np.array(F.shin) * m, F.shin_r), color, bone)

    def point(V):
        V = V.copy()
        t = np.clip(-V[:, 1], 0, 1)                    # toward the toe
        V[:, 0] *= 1 - 0.55 * t; V[:, 2] *= 1 - 0.4 * t
        return V
    k.add(f"foot{side}", ell(np.array(F.foot_c) * m, F.foot_r, deform=point), color, bone)


def arm(k, s, side, F, color=GREEN):
    """A small arm forward and down along the body's side, a round hand, three stubby fingers."""
    m = np.array([s, 1, 1.0])
    bone = f"arm{side}"
    k.add(f"arm{side}", tube(np.array(F.arm) * m, F.arm_r), color, bone)
    hc = np.array(F.hand_c, float)
    k.add(f"hand{side}", ell(hc * m, F.hand_r), color, bone)
    for j, dd in enumerate(F.fingers):
        dd = unit(*dd)
        tip = hc + dd * F.finger_len
        k.add(f"finger{side}{j}", tube(np.array([hc + dd * 0.004, tip]) * m, [F.finger_r, F.finger_r * 0.8]),
              color, bone)


# --- the collar and the tail ------------------------------------------------------------------------------------------------
def collar(k, F):
    """A yellow collar: a band round the back of the neck, its two ends hanging back over the shoulders and curling up."""
    k.add("collarback", outward(tube(F.collar_back, F.collar_back_r)), YELLOW, "body", fuse=False, surface="leaf")
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        pts = np.array(F.collar) * m
        k.add(f"collar{side}", outward(blade(pts, F.collar_r, (s * F.collar_flat[0], F.collar_flat[1], F.collar_flat[2]),
                                             0.4)), YELLOW, "body", fuse=False, surface="leaf")


def tail(k, F):
    """The tail: a green tube from the low back, back and out to +X and up to the leaf's foot, yellow beneath."""
    pts = np.array(F.tail, float)
    k.add("tail", tube(pts, F.tail_r), GREEN, "tail")
    # the yellow underside: points of the tail facing down from its line
    dense = []
    for a, b in zip(pts, pts[1:]):
        for t in np.linspace(0, 1, 24, endpoint=False):
            dense.append(a + (b - a) * t)
    dense = np.array(dense + [pts[-1]])

    def under(P):
        j = ((P[:, None, :] - dense[None]) ** 2).sum(2).argmin(1)
        rel = P - dense[j]
        r = np.linalg.norm(rel, axis=1) + 1e-9
        return inside(rel[:, 2] / r + F.tail_under, 0.08)
    k.paint(under, YELLOW, on=["tail"])


def tail_leaf(k, F):
    """The great flat leaf at the tail's end: one solid plate, its plane back and out to +X, standing up."""
    ex = unit(*F.leaf_ex)
    k.add("leaf", slab(F.leaf_outline, F.leaf_at, ex, (0, 0, 1.0), F.leaf_thick), LEAF, "tail", fuse=False,
          surface="leaf")


# --- Snivy's frame -------------------------------------------------------------------------------------------------------
F = SimpleNamespace(
    H=np.array([0.0, 0.028, 0.335]), HR=np.array([0.064, 0.07, 0.07]),
    snout_dir=(0, -0.92, 0.39), snout_mid=0.095, snout_r=(0.047, 0.12, 0.042), snout_taper=0.62, snout_flat=0.6,
    chin_at=(0, -0.035, -0.035), chin_r=(0.052, 0.058, 0.042),
    cream_line=(0.02, 0.2, -0.03), face_reach=0.085,
    front_axis=[(0.0, -0.005), (0.15, -0.01), (0.25, 0.0), (0.3, 0.0)], front_span=(0.03, 0.3, 1.1),
    mouth_at=(0.115, -0.72), mouth_h=(0.017, 0.011),
    eye_dir=(0.8, -0.56, -0.05), eye_shift=(0, 0, 0), eye_h=(0.032, 0.035),
    lid=(0.24, 0.12), iris=(-0.2, -0.22, 0.56), lid_col=YELLOW,
    frame_shift=(0.0, 0.012, 0.008), frame_h=(0.047, 0.046),
    thigh_c=(0.036, 0.0, 0.07), thigh_r=(0.033, 0.035, 0.038),
    shin=[(0.039, -0.002, 0.055), (0.041, -0.006, 0.02)], shin_r=[0.022, 0.019],
    foot_c=(0.042, -0.02, 0.012), foot_r=(0.02, 0.036, 0.012),
    arm=[(0.05, -0.012, 0.205), (0.075, -0.035, 0.172), (0.082, -0.06, 0.145)], arm_r=[0.018, 0.016, 0.015],
    hand_c=(0.084, -0.07, 0.135), hand_r=(0.017, 0.017, 0.016),
    fingers=[(0.45, -0.45, -0.8), (0.0, -0.55, -0.85), (-0.45, -0.45, -0.8)], finger_len=0.022, finger_r=0.0095,
    collar_back=[(-0.045, 0.03, 0.242), (0.0, 0.048, 0.247), (0.045, 0.03, 0.242)], collar_back_r=[0.014, 0.015, 0.014],
    collar=[(0.026, -0.036, 0.236), (0.045, -0.008, 0.24), (0.047, 0.028, 0.236), (0.055, 0.062, 0.205),
            (0.064, 0.085, 0.168), (0.076, 0.103, 0.16), (0.085, 0.102, 0.18)],
    collar_r=[0.013, 0.02, 0.021, 0.02, 0.017, 0.013, 0.01], collar_flat=(1.0, 0.5, 0.1),
    tail=[(0.0, 0.03, 0.115), (0.022, 0.09, 0.075), (0.05, 0.14, 0.068), (0.078, 0.18, 0.095)],
    tail_r=[0.034, 0.028, 0.023, 0.019], tail_under=0.35,
    leaf_ex=(0.57, 0.82, 0.0), leaf_at=(0.078, 0.18, 0.09), leaf_thick=0.022,
    leaf_outline=[(-0.022, 0.0), (0.022, 0.0), (0.06, 0.035), (0.095, 0.085), (0.112, 0.15), (0.072, 0.118),
                  (0.042, 0.13), (0.026, 0.19), (0.004, 0.245), (-0.016, 0.195), (-0.03, 0.145), (-0.078, 0.13),
                  (-0.068, 0.088), (-0.044, 0.04)],
)


# Snivy's own frame over the line's (servine.py grows from F as it stands): a longer snout tapering to a point that
# sweeps up, bigger eyes, the legs stubs under a body that sits low.
SNIVY = SimpleNamespace(**vars(F))
SNIVY.__dict__.update(
    snout_dir=(0, -0.95, 0.3), snout_mid=0.112, snout_r=(0.046, 0.15, 0.042), snout_taper=0.78, snout_curl=0.35,
    cream_line=(0.02, 0.24, -0.03), face_reach=0.1, mouth_at=(0.13, -0.72), mouth_h=(0.02, 0.012),
    eye_h=(0.0384, 0.042), frame_h=(0.056, 0.055),
    thigh_c=(0.034, -0.004, 0.05), thigh_r=(0.028, 0.03, 0.026),
    shin=[(0.037, -0.006, 0.042), (0.04, -0.01, 0.018)], shin_r=[0.02, 0.018],
    foot_c=(0.041, -0.024, 0.011), foot_r=(0.019, 0.032, 0.011),
)


def snivy(src, out):
    F = SNIVY
    k = Kit(src, coat="scales")

    # The body: a pear, the belly low over the legs, a short thick neck into the head
    def pear(V):
        V = V.copy()
        low = np.clip(-V[:, 2], 0, 1)
        V[:, :2] *= (1 + 0.14 * low)[:, None]
        return V
    k.add("torso", ell((0, 0.0, 0.124), (0.067, 0.065, 0.114), deform=pear), GREEN, "body")
    k.add("chest", ell((0, -0.008, 0.215), (0.05, 0.047, 0.05)), GREEN, "body")
    k.add("neck", tube([(0, -0.002, 0.215), (0, 0.004, 0.26), (0, 0.012, 0.3)], [0.042, 0.036, 0.036]), GREEN, "head")
    head(k, F)

    for s, side in ((1, "R"), (-1, "L")):
        leg(k, s, side, F)
        arm(k, s, side, F)

    tail(k, F)
    tail_leaf(k, F)
    collar(k, F)

    # The cream: the face below the snout's middle line, the throat and the belly
    k.paint(face_cream(F), CREAM)
    k.paint(front_cream(F), CREAM, on=["torso", "chest", "neck", "chin", "thigh"])
    mouth(k, F)

    lay, E, de = eyes(k, F)
    eye_frame(k, F, E, de)
    k.build(voxel=VOXEL, smooth=4, band=0.005)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    snivy(*args())
