# Cubone sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/cubone.py -- <the export's Cubone.glb> <out.glb>
#
# The design, from the references: a small light-brown biped, round in the belly, with a big cream oval down its front;
# short thick legs on flat feet with two white claws each; a short tail with a little spike on it; short arms held
# down. Over its head it wears a skull, pale grey, as big as its whole body: rounded at the back of the crown with two
# short blunt horns standing up and out from its top, a long snout reaching forward and down with two dark nostril
# holes, small teeth along its lower edge; under the snout its own brown chin. The eyes look out of the skull's big
# holes: the brown of its face in each hole, a dark eye with a white glint, its top lid stern. It holds a bone, a thick
# white shaft with two knobs at each end, in its hand on -X (the viewer's left, as the art has it), carried forward.
# Marowak is built by this script's functions (sibling("cubone")): its colours, limbs, club, sockets and eyes.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, polygon, hull, outward, tri, args

# --- the line's colours -------------------------------------------------------------------------------------------------
SKIN, BELLY, SKULL, SOCKET_RIM = "#D6A262", "#F2E2B4", "#D3D4DA", "#86838C"
SOCKET, CLAW, NOSTRIL = "#C08A4E", "#F6F4F0", "#3A3036"
INKH = "#22191A"
INK, WHITE = srgb(INKH), np.array([1.0, 1.0, 1.0])


def on_ell(c, r, az, el, lift=0.0):
    """A point of an ellipsoid at azimuth az (0 the front, -Y; + toward +X) and elevation el, and its normal."""
    a, e = math.radians(az), math.radians(el)
    d = np.array([math.sin(a) * math.cos(e), -math.cos(a) * math.cos(e), math.sin(e)])
    r = np.array(r, float) * np.ones(3)
    s = 1 / math.sqrt(((d / r) ** 2).sum())
    p = np.array(c) + d * s
    n = (p - c) / r ** 2
    n /= np.linalg.norm(n)
    return p + n * lift, n


# --- the body's parts, shared with Marowak ------------------------------------------------------------------------------
def body(k, F):
    """The torso, the belly's cream oval, the tail with its spike."""
    k.add("torso", ell(F["torso_c"], F["torso_r"], rot=(F.get("lean", 0), 0, 0)), F["skin"], "body")
    bc, br = np.array(F["belly_c"]), np.array(F["belly_r"])
    k.paint(lambda P: inside(np.sqrt((((P - bc) / br)[:, [0, 2]] ** 2).sum(1)) - 1, 0.003) * (P[:, 1] < bc[1] + 0.04),
            F["belly"], on=["torso"])
    t = [np.array(p) for p in F["tail"]]
    k.add("tail", tube(t, F["tail_r"]), F["skin"], "tail")
    s0, s1 = np.array(F["spike"][0]), np.array(F["spike"][1])
    k.add("spike", tube([s0, (s0 + s1) / 2, s1], [0.022, 0.014, 0.004]), F["skin"], "tail")


def legs(k, F):
    """Thick thighs fused into the body, short shins, flat feet, two white claws each pointing forward."""
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"thigh{side}", ell(np.array(F["thigh_c"]) * m, F["thigh_r"]), F["skin"], f"leg{side}")
        k.add(f"shin{side}", tube([np.array(p) * m for p in F["shin"]], F["shin_r"]), F["skin"], f"leg{side}")
        fc = np.array(F["foot_c"]) * m
        k.add(f"foot{side}", ell(fc, F["foot_r"]), F["skin"], f"leg{side}")
        for dx in (-0.022, 0.022):
            b = fc + np.array([dx, -F["foot_r"][1] * 0.78, -0.004])
            w, h = 0.013, 0.011
            pts = [b + (-w, 0.01, -h), b + (w, 0.01, -h), b + (-w, 0.01, h), b + (w, 0.01, h),
                   b + (0, -0.03, -h * 0.6)]
            k.add(f"claw{side}{dx > 0}", hull(pts), CLAW, f"leg{side}", fuse=False, surface="shell")


def arm(k, F, side, pts, radii, hand_c, hand_r):
    """An arm from the shoulder down and forward to a round hand."""
    k.add(f"arm{side}", tube(pts, radii), F["skin"], f"arm{side}")
    k.add(f"hand{side}", ell(hand_c, hand_r), F["skin"], f"arm{side}")


def club(k, a, b, r, bone):
    """The bone: a thick white shaft from a to b, two round knobs across each end. Loose, so it keeps its edges."""
    a, b = np.array(a, float), np.array(b, float)
    d = unit(*(b - a))
    k.add("shaft", outward(tube([a, (a + b) / 2, b], [r, r * 0.92, r])), SKULL, bone, fuse=False, surface="shell")
    across = np.cross(d, unit(0.3, 0.2, 1.0)); across /= np.linalg.norm(across)
    for i, (end, sgn) in enumerate(((a, -1), (b, 1))):
        for j, s in enumerate((-1, 1)):
            k.add(f"knob{i}{j}", outward(ell(end + across * r * 0.95 * s + d * r * 0.4 * sgn, (r * 1.45,) * 3, sub=3)),
                  SKULL, bone, fuse=False, surface="shell")


def squircle(p=3.0):
    """A rounded box's outline: between an ellipse (2) and a square."""
    return lambda th: 1.0 / (abs(math.cos(th)) ** p + abs(math.sin(th)) ** p) ** (1 / p)


def tilted_up(n, a):
    """The patch's up turned about its normal by a degrees (its u axis rising by a)."""
    n = np.array(n, float)
    up0 = np.array([0, 0, 1.0])
    right0 = np.cross(up0, n); right0 /= np.linalg.norm(right0)
    up0 = np.cross(n, right0)
    a = math.radians(a)
    return up0 * math.cos(a) - right0 * math.sin(a)


def socket(k, c, n, half, s, tilt=16.0, rim=0.18, color=SOCKET, p=2.6):
    """An eye's hole in the skull: a dark grey rim and the brown of the face inside, two stacked rounded patches, the
    outer side a little higher (s the side, +1 on +X)."""
    up = tilted_up(n, tilt * s)
    o = squircle(p)
    k.decal(c, n, (half[0] * (1 + rim), half[1] * (1 + rim)), SOCKET_RIM, up=up, eps=0.003, outline=o, rings=12, segs=72)
    k.decal(c, n, half, color, up=up, eps=0.0045, outline=o, rings=12, segs=72)


def mirrored_decal(k, c, n, half, f, s, **kw):
    """A patch on the +X side as given, on -X flipped (u runs toward +X on both)."""
    if s > 0:
        k.decal(c, n, half, f, **kw)
    else:
        if "outline" in kw and kw["outline"] is not None:
            o = kw["outline"]
            kw["outline"] = lambda th: o(math.pi - th)
        k.decal(c, n, half, (lambda u, v: f(-u, v)) if callable(f) else f, **kw)


EYE_SHAPE = [(-1.0, -0.15), (-0.6, 0.5), (0.4, 0.82), (1.0, 0.35), (0.85, -0.45), (0.05, -0.85), (-0.7, -0.62)]


def outline_of(pts):
    f = polygon(pts)
    cache = {}
    def r(th):
        key = round(th, 6)
        if key not in cache:
            cache[key] = f(th)
        return cache[key]
    return r


EYE_O = outline_of(EYE_SHAPE)


def stern_eye(lid, top0=0.36, slope=0.3, iris=(-0.12, -0.08, 0.62, 0.74)):
    """A dark eye: a big black iris in a little white, a small white glint, the top edge a dark lid line sloping down
    toward the snout (u outward). Its outline is EYE_O."""
    def look(u, v):
        c = np.tile(WHITE, (len(u), 1))
        top = top0 + slope * u
        c[((u - iris[0]) / iris[2]) ** 2 + ((v - iris[1]) / iris[3]) ** 2 < 1] = INK
        c[((u - 0.12) / 0.15) ** 2 + ((v - 0.12) / 0.17) ** 2 < 1] = WHITE
        c[v > top] = lid
        c[np.abs(v - top) < 0.14] = INK
        share = np.hypot(u, v) / np.array([EYE_O(t) for t in np.arctan2(v, u)])
        c[share > 0.84] = INK
        return c
    return look


# --- Cubone -------------------------------------------------------------------------------------------------------------
CUBONE = dict(
    skin=SKIN, belly=BELLY,
    torso_c=(0.0, 0.0, 0.2), torso_r=(0.13, 0.115, 0.13), lean=0,
    belly_c=(0.0, -0.1, 0.19), belly_r=(0.095, 1, 0.105),
    tail=[(0.0, 0.08, 0.13), (0.0, 0.16, 0.08), (0.0, 0.22, 0.04)], tail_r=[0.05, 0.034, 0.016],
    spike=[(0.0, 0.15, 0.1), (0.0, 0.18, 0.15)],
    thigh_c=(0.075, -0.01, 0.11), thigh_r=(0.07, 0.075, 0.075),
    shin=[(0.08, -0.01, 0.07), (0.082, -0.015, 0.035)], shin_r=[0.05, 0.045],
    foot_c=(0.082, -0.035, 0.022), foot_r=(0.048, 0.07, 0.024),
)
SKULL_C, SKULL_R = np.array([0.0, -0.035, 0.44]), np.array([0.155, 0.145, 0.13])
SNOUT_C, SNOUT_R = np.array([0.0, -0.165, 0.39]), np.array([0.092, 0.105, 0.078])
EYE_AZ, EYE_EL = 40, 6


def cubone(src, out):
    k = Kit(src, coat="fur")
    F = CUBONE
    body(k, F)
    legs(k, F)

    # The head under the skull: the brown chin under the snout and the back of the neck
    k.add("chin", ell((0.0, -0.13, 0.325), (0.085, 0.08, 0.045)), SKIN, "head")
    k.add("neck", ell((0.0, 0.0, 0.325), (0.11, 0.1, 0.06)), SKIN, "head")
    # The skull: the crown, the long snout forward and down, a brow over the holes, two blunt horns
    k.add("skull", ell(SKULL_C, SKULL_R), SKULL, "head", surface="shell")
    k.add("snout", ell(SNOUT_C, SNOUT_R, rot=(14, 0, 0)), SKULL, "head", surface="shell")
    k.add("brow", ell((0.0, -0.11, 0.475), (0.12, 0.06, 0.045)), SKULL, "head", surface="shell")
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"horn{side}", tube([np.array(p) * m for p in ((0.07, 0.0, 0.53), (0.105, 0.02, 0.575), (0.14, 0.035, 0.607))],
                                  [0.046, 0.038, 0.031]), SKULL, "head", surface="shell")
    # small blunt teeth along the snout's lower edge, over the chin
    for j, x in enumerate((-0.045, -0.015, 0.015, 0.045)):
        k.add(f"tooth{j}", ell((x, -0.205 + 0.25 * x * x, 0.322), (0.013, 0.012, 0.016)), SKULL, "head",
              surface="shell")

    # Arms: the right (+X) down at its side, the left (-X) forward holding the bone
    arm(k, F, "R", [(0.105, -0.02, 0.27), (0.14, -0.05, 0.22), (0.155, -0.08, 0.175)], [0.036, 0.032, 0.03],
        (0.157, -0.09, 0.165), (0.03, 0.03, 0.03))
    arm(k, F, "L", [(-0.105, -0.02, 0.27), (-0.14, -0.07, 0.23), (-0.15, -0.12, 0.2)], [0.036, 0.032, 0.03],
        (-0.152, -0.13, 0.195), (0.032, 0.032, 0.032))
    club(k, (-0.152, -0.05, 0.08), (-0.152, -0.27, 0.27), 0.023, "armL")

    # The nostrils: two dark holes on the snout's front
    for s in (1, -1):
        p, n = on_ell(SNOUT_C, SNOUT_R, s * 22, 24)
        k.decal(p, n, (0.012, 0.016), NOSTRIL, up=tilted_up(n, -25 * s), eps=0.004, rings=8, segs=48)

    # The holes for the eyes
    holes = []
    for s in (1, -1):
        p, n = on_ell(SKULL_C, SKULL_R, s * EYE_AZ, EYE_EL)
        holes.append((p, n, s))
        socket(k, p, n, (0.046, 0.038), s)

    k.build(voxel=0.006, smooth=4, band=0.006)

    lid = srgb(SOCKET)
    look_open = stern_eye(lid, top0=0.32, slope=0.3)

    def eyes(state):
        f = eye_look(state, look_open, lid, INK, line=0.18)
        for p, n, s in holes:
            mirrored_decal(k, p, n, (0.031, 0.03), f, s, eps=0.0065, outline=EYE_O, rings=20, segs=56)

    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    cubone(*args())
