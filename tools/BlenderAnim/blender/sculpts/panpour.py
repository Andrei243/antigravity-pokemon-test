# Panpour sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/panpour.py -- <the export's Panpour.glb> <out.glb>
#
# Written to be grown from: simipour.py imports its frame (sibling("panpour")): the colours, the white eyes with their
# lid crease, the cream face painted round the head's axis, the nose and the cat's smile, the arms and big flat hands,
# the legs and feet, the tail and its tuft of balls.
#
# The design, from the references: a small light-blue monkey with a big round head; a cream face over the lower half
# of the head that rises round each eye, the blue dipping to a point between them; white eyes outlined in black with a
# curved lid line across their tops (no pupil shows), a small dark nose, a wide cat's smile; big round ears out at the
# sides, blue with a pale two-lobed lining; on the crown a tall puffy tuft like a spout of water, a stalk rising into a
# cloud of round lumps that leans back; a slim cream body with a blue collar whose jagged edge points down the chest;
# long cream arms with big flat hands (held out in the art, carried forward and down here); short bent cream legs and
# small feet; a long thin cream tail out of its back to the +X side, blue near its end and tipped by three blue balls.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, boxy, polygon, tri, eye_look, args

BLUE, PALE, CREAM = "#4CB8DC", "#B8EAF4", "#F2D8A0"
INK, NOSE, WHITE, LIDTINT = "#1E2026", "#3A2A26", np.array([1.0, 1.0, 1.0]), "#D6E4EC"

# --- the frame (the skeleton's joints: body (0, 0, 0.2), head (0, -0.01, 0.24), ears (+-0.065, -0.02, 0.32), arms
#     (+-0.05, -0.01, 0.21), legs (+-0.04, 0, 0.1), tail (0, 0.04, 0.1)) -------------------------------------------------
PANPOUR = dict(
    H=(0.0, -0.01, 0.335), HR=(0.104, 0.094, 0.098),          # the head's middle and radii
    jowl=((0.0, -0.045, 0.3), (0.08, 0.055, 0.052)),           # the lower face standing forward
    ear=((0.158, 0.005, 0.355), (0.057, 0.019, 0.073), 22),      # centre, radii, turned toward the front (deg)
    lining=0.72,                                                # the lining's size against the ear's
    eye_x=0.048, eye_z=0.03, eye_half=(0.03, 0.036), eye_tilt=0.12,
    face_top=([0, 0.1, 0.28, 0.55, 0.8, 1.05, 1.35, 1.65], [0.15, 0.35, 0.56, 0.6, 0.48, 0.1, -0.35, -0.6]),
    nose_z=-0.17, mouth_z=-0.42, mouth_half=(0.03, 0.017),
    torso=((0.0, 0.0, 0.175), (0.052, 0.046, 0.07)), hips=((0.0, 0.006, 0.118), (0.068, 0.052, 0.05)),
    collar=0.205, collar_teeth=5, collar_drop=0.022,
    shoulder=(0.045, -0.01, 0.215), elbow=(0.075, -0.052, 0.176), wrist=(0.084, -0.1, 0.138),
    arm_r=(0.028, 0.024, 0.022), hand_r=(0.036, 0.042, 0.017),
    hip=(0.04, 0.0, 0.112), knee=(0.064, -0.03, 0.068), ankle=(0.058, -0.012, 0.03),
    leg_r=(0.036, 0.025, 0.018), foot_r=(0.026, 0.038, 0.016),
    tail=[(0.0, 0.035, 0.105), (0.05, 0.1, 0.075), (0.12, 0.135, 0.07), (0.19, 0.125, 0.095), (0.225, 0.095, 0.14)],
    tail_r=0.0155, tail_blue=0.72, tail_balls=3, ball_r=0.024,
)


def head_coords(P, Pr):
    """Round the head's upright axis: the angle from the front (radians) and the height as a share of the radius."""
    H, R = np.array(Pr["H"]), np.array(Pr["HR"])
    a = np.arctan2(P[:, 0] - H[0], -(P[:, 1] - H[1]))
    return a, (P[:, 2] - H[2]) / R[2]


def face_mask(Pr):
    """The cream face: under a top line that rises round each eye and dips to a point between them."""
    H, R = np.array(Pr["H"]), np.array(Pr["HR"])
    xs, ys = Pr["face_top"]
    def m(P):
        a, zr = head_coords(P, Pr)
        top = np.interp(np.abs(a), xs, ys)
        near = np.linalg.norm((P - H) / (R * 1.25), axis=1) < 1
        return (inside((zr - top) * R[2], 0.0028) * (np.abs(a) < 1.7) * near * (P[:, 2] > H[2] - R[2] * 1.3)
                * (np.abs(P[:, 0] - H[0]) < R[0] * Pr.get("face_w", 1.08)))
    return m


def front_point(Pr, x, zr, lift=0.0):
    """The head's front surface at x and a height zr (share of the radius), as on its ellipsoid."""
    H, R = np.array(Pr["H"]), np.array(Pr["HR"])
    q = max(0.0, 1 - zr * zr - (x / R[0]) ** 2)
    return np.array([H[0] + x, H[1] - R[1] * math.sqrt(q) - lift, H[2] + zr * R[2]])


def normal_at(Pr, p):
    H, R = np.array(Pr["H"]), np.array(Pr["HR"])
    n = (p - H) / R ** 2
    return n / np.linalg.norm(n)


def ears(k, Pr, color=BLUE, lining=PALE):
    """Big round ears out at the sides, turned toward the front, a pale two-lobed lining on their fronts."""
    c, r, turn = Pr["ear"]
    c = np.array(c)
    k.add("ear", ell(c, r, rot=(0, 0, turn)), color, "ear{s}", mirror=True)
    # The lining: a heart on its side, its two lobes toward the ear's rim
    n = unit(math.sin(math.radians(turn)) * 0.9, -1, 0.05)
    front = c + np.array([0, -r[1] * 0.98, 0])
    s = Pr["lining"]
    k.decal(front, n, (r[0] * s, r[2] * s), lining, outline=lobes([(0.18, 0.4, 0.62), (0.18, -0.4, 0.62)]),
            eps=0.0035, rings=12, segs=56, mirror=True)


def lobes(discs):
    """A decal's outline that is the union of discs (u, v, radius), each holding the middle: two lobes, a clover."""
    def r(th):
        d = np.array([math.cos(th), math.sin(th)])
        best = 0.0
        for u, v, rad in discs:
            c = np.array([u, v])
            b = d @ c
            q = b * b - (c @ c - rad * rad)
            if q >= 0:
                best = max(best, b + math.sqrt(q))
        return best
    return r


def collar(k, Pr, color=BLUE):
    """The blue fur round the neck down to a jagged edge pointing down the chest, the cream belly under it."""
    top, teeth, drop = Pr["collar"], Pr["collar_teeth"], Pr["collar_drop"]
    T = np.array(Pr["torso"][0])
    def m(P):
        a = np.arctan2(P[:, 0], -(P[:, 1] - T[1]))
        front = np.clip(1 - np.abs(a) / 1.6, 0, 1)
        edge = top - drop * front * (0.5 + 0.5 * tri(a * teeth / (2 * math.pi) + 0.5))
        return inside(edge - P[:, 2], 0.0025) * inside(P[:, 2] - (Pr["H"][2] - Pr["HR"][2] * 0.55), 0.003) \
            * (np.hypot(P[:, 0], P[:, 1] - T[1]) < 0.075)
    k.paint(m, color)


def arms(k, Pr, color=CREAM, fingers=3, open_hand=False):
    """Long arms carried forward and a little down, ending in big flat hands with short fingers and a thumb."""
    S, El, W = (np.array(Pr[x]) for x in ("shoulder", "elbow", "wrist"))
    a0, a1, a2 = Pr["arm_r"]
    k.add("arm", tube([S, El, W], [a0, a1, a2]), color, "arm{s}", mirror=True)
    d = unit(*(W - El))
    side = unit(d[1], -d[0], 0) if d[0] > 0 else unit(-d[1], d[0], 0)
    side = side if side[0] >= 0 else -side
    hr = np.array(Pr["hand_r"])
    Hc = W + d * hr[1] * 0.7
    k.add("hand", ell(Hc, hr, rot=(0, 0, math.degrees(math.atan2(d[0], -d[1])))), color, "arm{s}", mirror=True)
    for i in range(fingers):
        o = (i - (fingers - 1) / 2) * hr[0] * 0.62
        a = Hc + d * hr[1] * 0.55 + side * o
        b = Hc + d * hr[1] * (1.25 if open_hand else 1.05) + side * o * 1.3 + np.array([0, 0, -0.004])
        k.add(f"finger{i}", tube([a, b], [hr[2] * 0.95, hr[2] * 0.85]), color, "arm{s}", mirror=True)
    t0 = Hc - side * hr[0] * 0.55
    k.add("thumb", tube([t0, t0 - side * hr[0] * 0.45 + d * hr[1] * 0.35], [hr[2] * 0.95, hr[2] * 0.85]), color,
          "arm{s}", mirror=True)


def legs(k, Pr, color=CREAM, toes=3):
    """Short bent legs, thighs fused into the hips, small feet with toes at their fronts."""
    Hp, K, A = (np.array(Pr[x]) for x in ("hip", "knee", "ankle"))
    l0, l1, l2 = Pr["leg_r"]
    k.add("thigh", tube([Hp, K], [l0, l1]), color, "leg{s}", mirror=True)
    k.add("shin", tube([K, A + np.array([0, 0, 0.004])], [l1, l2]), color, "leg{s}", mirror=True)
    fr = np.array(Pr["foot_r"])
    Fo = A + np.array([0.004, -fr[1] * 0.45, -A[2] + fr[2]])
    k.add("foot", ell(Fo, fr), color, "leg{s}", mirror=True)
    for i in range(toes):
        o = (i - (toes - 1) / 2) * fr[0] * 0.6
        k.add(f"toe{i}", ell(Fo + np.array([o, -fr[1] * 0.78, -fr[2] * 0.15]), (fr[0] * 0.34, fr[0] * 0.4, fr[2] * 0.75)),
              color, "leg{s}", mirror=True)


def tail(k, Pr, base=CREAM, tip=BLUE):
    """The long thin tail out of the back and round to the +X side, blue toward its end, a tuft of balls at its tip."""
    pts = np.array(Pr["tail"], float)
    r = Pr["tail_r"]
    k.add("tail", tube(pts, [r * 1.15] + [r] * (len(pts) - 1)), base, "tail")
    # Blue from a share of the way along the third stretch from the end: a crisp line across the tail there
    a, b = pts[-2], pts[-1]
    c0 = pts[-3] + (pts[-2] - pts[-3]) * Pr["tail_blue"]
    dvec = unit(*(pts[-2] - pts[-3]))
    def seg_dist(P, p, q):
        t = np.clip(((P - p) @ (q - p)) / ((q - p) @ (q - p)), 0, 1)
        return np.linalg.norm(P - (p + t[:, None] * (q - p)), axis=1)
    def blue(P):
        d = np.minimum(np.minimum(seg_dist(P, pts[-3], pts[-2]), seg_dist(P, a, b)), np.linalg.norm(P - b, axis=1))
        return inside(-((P - c0) @ dvec), 0.0025) * (d < r * 2.2) * (np.linalg.norm(P - c0, axis=1) < 0.2)
    k.paint(blue, tip)
    # The tuft: balls round the tip, fanned in a plane the back camera sees
    d = unit(*(b - a))
    up = unit(0, 0, 1)
    side = np.cross(d, up); side /= np.linalg.norm(side)
    rb = Pr["ball_r"]
    n = Pr["tail_balls"]
    # A clover in a plane the front and the back cameras both see (across x and up), its middle past the tip
    across = unit(1, 0.25, 0)
    mid = b + d * rb * 1.1
    for i in range(n):
        ang = math.radians(Pr.get("ball_start", 30) + 360 * i / n)
        o = (math.cos(ang) * across + math.sin(ang) * up) * rb * 1.15
        k.add(f"tailball{i}", ell(mid + o, (rb, rb, rb)), tip, "tail")
    k.add("tailball_mid", ell(mid, (rb * 0.9,) * 3), tip, "tail")


# --- the face -----------------------------------------------------------------------------------------------------
def eye_open(u, v):
    """A white eye in a black outline with a curved lid line across its top: content, no pupil showing."""
    c = np.tile(srgb(INK), (len(u), 1))
    inner = (u / 0.92) ** 2 + (v / 0.93) ** 2 < 1
    c[inner] = WHITE
    crease = 0.12 + 0.18 * (1 - u * u)
    c[inner & (v > crease)] = srgb(LIDTINT)
    c[inner & (np.abs(v - crease) < 0.11) & (np.abs(u) < 0.8)] = srgb(INK)
    return c


def lay_eyes(k, Pr, state, look=eye_open, lid=CREAM):
    p = front_point(Pr, Pr["eye_x"], Pr["eye_z"])
    n = normal_at(Pr, p)
    up = unit(-Pr["eye_tilt"], 0, 1)              # the tops lean a little out (u outward on +X)
    k.decal(p, n, Pr["eye_half"], eye_look(state, look, srgb(lid), srgb(INK), line=0.17), eps=0.004, rings=20,
            segs=56, mirror=True, up=up)


def nose_and_mouth(k, Pr, face=CREAM, ink=INK, nose=NOSE):
    p = front_point(Pr, 0.0, Pr["nose_z"])
    k.decal(p, normal_at(Pr, p), (0.0085, 0.0065), nose, eps=0.0045, rings=8, segs=36)
    def mouth(u, v):
        c = np.tile(srgb(face), (len(u), 1))
        line = 0.15 + 1.1 * (np.abs(u) - 0.5) ** 2 - 0.22
        c[(np.abs(v - line) < 0.2) & (np.abs(u) < 0.95)] = srgb(ink)
        return c
    m = front_point(Pr, 0.0, Pr["mouth_z"])
    k.decal(m, normal_at(Pr, m), Pr["mouth_half"], mouth, eps=0.004, rings=24, segs=80)


def head(k, Pr, color=BLUE):
    H, R = np.array(Pr["H"]), np.array(Pr["HR"])
    k.add("head", ell(H, R), color, "head")
    jc, jr = Pr["jowl"]
    k.add("jowl", ell(jc, jr), CREAM, "head")
    k.paint(face_mask(Pr), CREAM)


def spout(k, Pr, color=BLUE):
    """The tuft: a short stalk out of the crown rising into a cloud of round lumps that leans back."""
    H, R = np.array(Pr["H"]), np.array(Pr["HR"])
    top = H + np.array([0, 0.0, R[2] * 0.8])
    k.add("stalk", tube([top, top + np.array([0, 0.015, 0.035])], [0.045, 0.04]), color, "head")
    lumps = [((0.0, 0.02, 0.05), 0.045), ((-0.05, 0.03, 0.055), 0.038), ((0.052, 0.03, 0.055), 0.036),
             ((-0.025, 0.065, 0.075), 0.04), ((0.03, 0.068, 0.078), 0.039), ((-0.075, 0.07, 0.055), 0.03),
             ((0.08, 0.072, 0.058), 0.03), ((0.0, 0.1, 0.07), 0.036), ((-0.02, -0.012, 0.075), 0.03),
             ((0.025, -0.008, 0.08), 0.032), ((-0.045, 0.1, 0.045), 0.03), ((0.045, 0.105, 0.045), 0.03)]
    for i, (o, r) in enumerate(lumps):
        o = np.array(o) * (1.3, 1.0, 0.95)          # as wide as the head and more, leaning back, not raised
        k.add(f"puff{i}", ell(top + o, (r * 1.08, r * 1.0, r * 0.92)), color, "head")


def panpour(src, out, Pr=PANPOUR):
    k = Kit(src, coat="fur")
    head(k, Pr)
    spout(k, Pr)
    ears(k, Pr)
    T, TR = Pr["torso"]
    k.add("torso", ell(T, TR), CREAM, "body")
    hc, hr = Pr["hips"]
    k.add("hips", ell(hc, hr), CREAM, "body")
    k.add("neck", ell((0, -0.005, 0.228), (0.042, 0.04, 0.035)), BLUE, "body")
    collar(k, Pr)
    arms(k, Pr)
    legs(k, Pr)
    tail(k, Pr)
    nose_and_mouth(k, Pr)
    k.build(voxel=0.0045, smooth=3, band=0.006)
    k.eye_states(lambda s: lay_eyes(k, Pr, s))
    k.export(out)


if __name__ == "__main__":
    panpour(*args())
