# Exeggutor-Alola sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/exeggutoralola.py -- <the export's Exeggutor-Alola.glb> <out.glb>
#
# The design, from the references: Exeggutor's pear-shaped ringed trunk on its two short legs with cream soles and
# white claws, grown into a palm: out of the trunk's top a long ringed brown neck rises to three pale yellow heads side
# by side under a crown of long green leaves: on the left one laughing with its eyes squeezed shut, in the middle one
# with round open eyes and a wide open mouth, on the right one smirking under half-shut lids. A long tail curls out
# behind to the +X side and up, ending in a fourth head, pale green, with two small leaves on top.
# The neck is shortened (the brief's own ask): in the pictures it is half the height, which would leave the heads specks
# in the game's cell. The heads, the crown and the leaves ride the neck's bone (its joint at the neck's root), since
# the export's head and leaf joints stand where the long neck put them, far above the shortened top.
# Built as exeggutor.py is (it builds as it is read, so its frame is copied here and changed): its trunk, grooves,
# legs, soles, claws, leaf plates, sleepy eye, laugh and smile.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, sheet, srgb, inside, unit, eye_look, polygon, hull, ring_band, outward, args

BARK, GROOVE, CREAM, HEADH = "#B98A5E", "#7A5538", "#F1E6B4", "#F1E5A2"
LEAF1, LEAF2, CROWN, CLAW = "#4CA23A", "#3B8F31", "#3D7F30", "#F4F2EC"
TAILH = "#C4DE9A"
INKH, MOUTHH, TONGUEH = "#2A1E24", "#8C2A36", "#EE8C92"
INK, HEAD_, MOUTH, TONGUE = srgb(INKH), srgb(HEADH), srgb(MOUTHH), srgb(TONGUEH)
TAIL_ = srgb(TAILH)
WHITE = np.array([1.0, 1.0, 1.0])

TRUNK_C, TRUNK_R = np.array([0.0, 0.01, 0.255]), np.array([0.168, 0.158, 0.215])
GROOVES = [0.2, 0.27, 0.34, 0.405]
NECK = [(0.0, 0.0, 0.4), (0.0, 0.0, 0.55), (0.0, -0.005, 0.7), (0.0, -0.01, 0.84)]
NECK_R = [0.088, 0.066, 0.06, 0.06]
NECK_RINGS = [0.52, 0.585, 0.65, 0.715, 0.78]
HEADS = [  # centre, radius, the way its face looks (degrees round from the front), its mood
    ((-0.128, -0.03, 0.905), 0.1, -32, "laugh"),
    ((0.0, -0.07, 0.915), 0.106, 0, "shout"),
    ((0.128, -0.03, 0.905), 0.1, 32, "smirk"),
]
CROWN_C = np.array([0.0, 0.035, 0.975])
HIP, FOOT_C, FOOT_R = np.array([0.11, -0.03, 0.15]), np.array([0.175, -0.14, 0.062]), np.array([0.074, 0.064, 0.064])
TAIL = [(0.0, 0.12, 0.2), (0.04, 0.25, 0.12), (0.14, 0.36, 0.12), (0.22, 0.43, 0.2)]
TAIL_R = [0.06, 0.045, 0.037, 0.032]
TAILHEAD_C, TAILHEAD_R = np.array([0.24, 0.445, 0.27]), 0.068
TOP = "neck"


def pear(V):
    V = V.copy()
    s = 1 + 0.08 * np.clip(-V[:, 2], 0, 1) - 0.12 * np.clip(V[:, 2], 0, 1) ** 1.5
    V[:, 0] *= s; V[:, 1] *= s
    V[:, 2] = np.maximum(V[:, 2], -0.92)
    return V


def on_egg(c, r, az, el, lift=0.0):
    a, e = math.radians(az), math.radians(el)
    d = np.array([math.sin(a) * math.cos(e), -math.cos(a) * math.cos(e), math.sin(e)])
    r = np.array(r, float) * np.ones(3)
    s = 1 / math.sqrt(((d / r) ** 2).sum())
    p = np.array(c) + d * s
    n = (p - c) / r ** 2
    n /= np.linalg.norm(n)
    return p + n * lift, n


def trunk_radius(z):
    t = (z - TRUNK_C[2]) / TRUNK_R[2]
    s = 1 + 0.08 * max(-t, 0) - 0.12 * max(t, 0) ** 1.5
    return TRUNK_R[0] * s * math.sqrt(max(0.0, 1 - t * t))


def neck_radius(z):
    zs = [p[2] for p in NECK]
    return float(np.interp(z, zs, NECK_R))


SOLE_N = unit(0.2, -1, 0.35)


def sole(P):
    m = np.zeros(len(P))
    for s in (1, -1):
        c = FOOT_C * np.array([s, 1, 1])
        n = SOLE_N * np.array([s, 1, 1])
        d = P - c
        m = np.maximum(m, inside(0.042 - d @ n, 0.002) * inside(np.linalg.norm(d, axis=1) - 0.1, 0.002))
    return m


def leaf_plate(k, name, root, a, reach, rise, width, color, bone, thick=0.017, n=40):
    """Exeggutor's leaf: its midline arching up and out from root toward azimuth a, its tip falling."""
    ar = math.radians(a)
    d = np.array([math.sin(ar), -math.cos(ar), 0.0])
    tilt = math.radians(18 + 42 * abs(math.sin(ar)))
    w0 = np.cross((0, 0, 1.0), d)
    w = w0 * math.cos(tilt) + np.array([0, 0, 1.0]) * math.sin(tilt)
    root = np.array(root, float) + d * 0.03 + np.array([0, 0, 0.02])

    def at(u, v):
        uu = 0.015 + 0.97 * u
        h = rise * (2.4 * uu - 2.1 * uu * uu)
        mid = root[None, None] + d[None, None] * (reach * uu)[..., None] + np.array([0, 0, 1.0])[None, None] * h[..., None]
        wide = width * (np.clip(uu / 0.18, 0, 1) ** 0.6) * (1 - uu) ** 0.85 + 0.003
        s = (v - 0.5) * 2 * wide
        return mid + s[..., None] * w[None, None]

    k.add(name, outward(sheet(at, n, 7, thick)), color, bone, fuse=False, surface="leaf")


# --- the faces --------------------------------------------------------------------------------------------------------
SLEEPY = [(-1.0, 0.05), (-0.45, 0.48), (0.45, 0.5), (1.0, 0.12), (0.5, -0.55), (-0.5, -0.6)]


def outline_of(pts):
    f = polygon(pts)
    cache = {}
    def r(th):
        key = round(th, 6)
        if key not in cache:
            cache[key] = f(th)
        return cache[key]
    return r


SLEEPY_O = outline_of(SLEEPY)


def rim_share(o, u, v):
    th = np.arctan2(v, u)
    return np.hypot(u, v) / np.array([o(t) for t in th])


def sleepy(u, v):
    """Half-shut: the lid drooping over the top half, a heavy lid line, the pupil low (the base's)."""
    c = np.tile(WHITE, (len(u), 1))
    c[rim_share(SLEEPY_O, u, v) > 0.8] = INK
    c[v > 0.06 - 0.12 * u] = HEAD_
    c[np.abs(v - (0.06 - 0.12 * u)) < 0.17] = INK
    c[(((u + 0.05) / 0.36) ** 2 + ((v + 0.2) / 0.34) ** 2 < 1) & (v < 0.06 - 0.12 * u)] = INK
    return c


def squeezed(u, v):
    """Shut tight with laughing: a thick dark arch on the yellow."""
    c = np.tile(HEAD_, (len(u), 1))
    c[(np.abs(u) < 0.85) & (np.abs(v - (-0.35 + 0.6 * (1 - u * u))) < 0.22)] = INK
    return c


def round_eye(u, v):
    """Wide open: a white disc ringed in ink, a dark pupil in the middle."""
    c = np.tile(WHITE, (len(u), 1))
    rr = np.hypot(u, v)
    c[rr > 0.8] = INK
    c[((u + 0.02) / 0.34) ** 2 + ((v + 0.04) / 0.4) ** 2 < 1] = INK
    return c


def tail_eye(u, v):
    c = np.tile(TAIL_, (len(u), 1))
    c[(np.abs(u) < 0.85) & (np.abs(v - (-0.1 - 0.35 * (1 - u * u))) < 0.24)] = INK
    return c


EYES = {"laugh": (squeezed, None), "shout": (round_eye, None), "smirk": (sleepy, SLEEPY_O)}


def d_shape(th):
    s = math.sin(th)
    return min(1.0, 0.25 / s) if s > 0 else 1.0


def laugh(u, v):
    c = np.tile(MOUTH, (len(u), 1))
    c[(u / 0.62) ** 2 + ((v + 0.72) / 0.5) ** 2 < 1] = TONGUE
    c[np.hypot(u, v) > 0.88] = INK
    return c


def smirk(u, v):
    """A sideways smile: a dark curve rising to the outer side, a little pink in its middle."""
    c = np.tile(HEAD_, (len(u), 1))
    curve = 0.1 * u - 0.35 * (1 - u * u) + 0.25
    c[(np.abs(v - curve) < 0.26) & (u > -0.85) & (u < 0.9)] = INK
    return c


def flip(f):
    return lambda u, v: f(-u, v)


def flip_outline(o):
    return lambda th: o(math.pi - th)


def exeggutor_alola(src, out):
    k = Kit(src, coat="leaf")

    # The trunk, ringed, as Exeggutor's
    k.add("trunk", ell(TRUNK_C, TRUNK_R, deform=pear), BARK, "body", surface="skin")
    for i, z in enumerate(GROOVES):
        k.add(f"groove{i}", ring_band((0, TRUNK_C[1], z), (0, 0, 1), trunk_radius(z) * 0.965 + 0.002, 0.011, thick=0.007),
              GROOVE, "body", fuse=False, surface="skin")
    # The neck: thick, ringed, rising from the trunk's top to the heads
    k.add("neck", tube(NECK, NECK_R), BARK, "neck", surface="skin")
    for i, z in enumerate(NECK_RINGS):
        k.add(f"neckring{i}", ring_band((0, np.interp(z, [p[2] for p in NECK], [p[1] for p in NECK]), z), (0, 0, 1),
                                        neck_radius(z) - 0.002, 0.009, thick=0.006),
              GROOVE, "neck", fuse=False, surface="skin")

    # The legs, the feet with their cream soles and three white claws, as Exeggutor's
    k.add("thigh", tube([HIP, (0.15, -0.09, 0.1), FOOT_C + (0, 0.03, 0.01)], [0.075, 0.068, 0.06]), BARK, "leg{s}",
          mirror=True, surface="skin")
    k.add("foot", ell(FOOT_C, FOOT_R, rot=(-20, 0, 12)), BARK, "leg{s}", mirror=True, surface="skin")
    k.paint(sole, CREAM)
    for j, dx in enumerate((-0.036, 0.0, 0.036)):
        b = FOOT_C + np.array([dx, -0.012, 0.05])
        d = unit(dx * 5, -0.6, 1)
        side = np.array([1.0, 0, 0]) * 0.015
        fwd = unit(0, -1, 0.1) * 0.015
        pts = [b - side, b + side, b - fwd * 0.6, b + fwd, b + d * 0.055]
        k.add(f"claw{j}", hull(pts), CLAW, "leg{s}", fuse=False, mirror=True, surface="shell")

    # The tail, curling back and out to +X and up, and its pale green head with two small leaves
    k.add("tail", tube(TAIL, TAIL_R), BARK, "tail", surface="skin")
    k.add("tailhead", ell(TAILHEAD_C, (TAILHEAD_R, TAILHEAD_R * 0.95, TAILHEAD_R * 0.92)), TAILH, "tail", surface="shell")
    tip = TAILHEAD_C + np.array([0, 0, TAILHEAD_R * 0.85])
    k.add("tailstem", tube([tip - (0, 0, 0.02), tip + (0, 0, 0.012)], [0.014, 0.012], seg=12), CROWN, "tail")
    for i, a in enumerate((60, 200)):
        leaf_plate(k, f"tailleaf{i}", tip - np.array([0, 0, 0.03]), a, 0.11, 0.045, 0.028, LEAF1, "tail",
                   thick=0.012, n=20)
    k.paint(lambda P: inside(np.linalg.norm(P - TAILHEAD_C, axis=1) - TAILHEAD_R * 1.02, 0.003), TAILH, on=["tailhead"])

    # The heads, side by side on the neck's top, a crown behind them
    for i, (c, r, look, mood) in enumerate(HEADS):
        k.add(f"head{i}", ell(c, (r, r * 0.96, r * 0.97)), HEADH, TOP, surface="shell")
    k.add("collar", ell((0.0, -0.005, 0.85), (0.09, 0.08, 0.05)), BARK, TOP, surface="skin")
    k.add("crown", ell(CROWN_C, (0.075, 0.07, 0.055)), CROWN, TOP)
    k.paint(lambda P: np.ones(len(P)), HEADH, on=["head"])

    LEAVES = [  # azimuth, reach, rise, width
        (-24, 0.24, 0.3, 0.054), (24, 0.24, 0.31, 0.054), (-60, 0.34, 0.25, 0.06), (60, 0.34, 0.26, 0.06),
        (-98, 0.38, 0.2, 0.06), (98, 0.38, 0.2, 0.06), (-138, 0.34, 0.23, 0.058), (138, 0.34, 0.24, 0.058),
        (180, 0.3, 0.27, 0.056), (-160, 0.28, 0.29, 0.054), (160, 0.28, 0.28, 0.054), (0, 0.14, 0.33, 0.05),
    ]
    for i, (a, reach, rise, width) in enumerate(LEAVES):
        leaf_plate(k, f"leaf{i}", CROWN_C, a, reach, rise, width, LEAF1 if i % 2 else LEAF2, TOP)

    # The mouths, before the body's build so they are in every eye state
    for c, r, look, mood in HEADS:
        if mood == "laugh":
            p, n = on_egg(c, r, look, -28)
            k.decal(p, n, (0.042, 0.036), laugh, eps=0.0034, outline=d_shape, rings=24, segs=64)
        elif mood == "shout":
            p, n = on_egg(c, r, look, -30)
            k.decal(p, n, (0.05, 0.044), laugh, eps=0.0034, outline=d_shape, rings=24, segs=64)
        else:
            p, n = on_egg(c, r, look, -26)
            k.decal(p, n, (0.04, 0.022), smirk, eps=0.0034, rings=24, segs=64)
    p, n = on_egg(TAILHEAD_C, TAILHEAD_R, 105, -22)
    k.decal(p, n, (0.016, 0.01), lambda u, v: np.tile(INK, (len(u), 1)), eps=0.003, rings=10, segs=40)

    k.build(voxel=0.0065, smooth=4, band=0.006)

    def eyes(state):
        for c, r, look, mood in HEADS:
            open_look, o = EYES[mood]
            f = open_look if (state == "Fierce" and mood == "laugh") else eye_look(state, open_look, HEAD_, INK, line=0.2)
            half = (r * 0.3, r * 0.27) if mood == "shout" else (r * 0.3, r * 0.22)
            for s in (1, -1):
                p, n = on_egg(c, r, look + s * 25, 14)
                kw = dict(outline=(o if s > 0 else flip_outline(o))) if o is not None else {}
                k.decal(p, n, half, f if s > 0 else flip(f), eps=0.004, rings=20, segs=56, **kw)
        f = tail_eye if state == "Fierce" else eye_look(state, tail_eye, TAIL_, INK, line=0.2)
        for s in (1, -1):
            p, n = on_egg(TAILHEAD_C, TAILHEAD_R, 105 + s * 26, 12)
            k.decal(p, n, (0.016, 0.012), f if s > 0 else flip(f), eps=0.003, rings=20, segs=56)

    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    exeggutor_alola(*args())
