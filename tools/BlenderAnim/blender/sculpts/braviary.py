# Braviary sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/braviary.py -- <the export's Braviary.glb> <out.glb>
# Rufflet grown (its colours, hooked beak, feathers and taloned feet, imported from rufflet.py): from the references,
# a great eagle in the air, its body dark blue-violet leaning forward, red over the shoulders and the back; broad wings
# raised in a V and swept back, red above, dark blue-violet below with a red leading edge, their tips fingered; a red
# head with a blue mark on the brow over a big hooked yellow beak, open on a pink mouth; a small fierce eye under a red
# brow; a crown of white plumes swept back over the head with cream ones hanging over the nape and white tufts at the
# cheeks; a fan of tail feathers, red, then a yellow band, then blue tips; shaggy dark thighs and yellow scaled feet
# hanging forward, gripping, with dark talons.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, srgb, inside, unit, eye_look, polygon, args, sibling

r_ = sibling("rufflet")
p = r_.p
NAVY, CREAM, RED, WHITE, YELLOW, BLACK = r_.NAVY, r_.CREAM, r_.RED, r_.WHITE, r_.YELLOW, r_.BLACK
NAVY = "#585A98"
BAND, BLUE, PINK, IRIS = "#F2C83C", "#2F72C8", "#E88C9C", "#7C8FBF"
VOXEL = 0.0062
H = np.array([0, -0.168, 0.445])             # the head's centre, forward of the head's joint (0, -0.12, 0.421)
HR = np.array([0.066, 0.075, 0.064])
SH = np.array([0.075, -0.01, 0.395])         # the wing's joint (0.07, -0.02, 0.381)


# --- the body ---------------------------------------------------------------------------------------------------
def body(k):
    k.add("body", ell((0, 0.01, 0.318), (0.105, 0.165, 0.112), rot=(-18, 0, 0)), NAVY, "body")
    k.add("chest", ell((0, -0.1, 0.35), (0.09, 0.085, 0.095)), NAVY, "body")
    k.add("neck", ell((0, -0.135, 0.41), (0.065, 0.07, 0.07), rot=(-25, 0, 0)), NAVY, "head")
    k.add("head", ell(H, HR), RED, "head")
    for s, b in ((1, "wingR"), (-1, "wingL")):
        k.add("shoulder", ell(SH * np.array([s, 1, 1]) + (-0.012 * s, 0.045, -0.015), (0.048, 0.065, 0.045)), RED, b)
    # red over the back between the wings, navy below
    k.paint(lambda P: inside((0.35 - 0.12 * (P[:, 1] - 0.0)) - P[:, 2], 0.004) * inside(-0.02 - P[:, 1], 0.006), RED,
            on=["body", "chest"])
    # the head red above, the throat navy under the beak's line
    k.paint(lambda P: inside(P[:, 2] - (H[2] - 0.035 + 0.25 * (P[:, 1] - H[1])), 0.003), NAVY, on=["head"])


def thighs(k):
    for s, b in ((1, "legR"), (-1, "legL")):
        k.add("thigh", ell((0.058 * s, -0.075, 0.25), (0.05, 0.06, 0.06), rot=(-30, 0, 0)), NAVY, b)


# --- the head ---------------------------------------------------------------------------------------------------
def crown(k):
    """White plumes swept back from the brow over the crown, cream ones hanging over the nape, white tufts at the cheeks."""
    top = H + np.array([0, 0.0, HR[2] * 0.8])
    for x, rise, ln in ((0.0, 0.11, 0.2), (0.03, 0.09, 0.18), (-0.03, 0.09, 0.18), (0.055, 0.05, 0.16),
                        (-0.055, 0.05, 0.16)):
        base = top + np.array([x * 0.6, -0.05, -0.01])
        tip = base + unit(x * 3.2, 1.0, rise * 4.5) * ln
        k.add("plume", p.spike(base, tip, 0.06, 0.024, side=(1, 0, -x * 4), at=0.35), WHITE, "head", fuse=False)
    for x in (0.045, 0.0, -0.045):
        base = H + np.array([x, 0.04, 0.03])
        tip = base + unit(x * 3, 1.0, -0.35) * 0.13
        k.add("mane", p.spike(base, tip, 0.065, 0.026, side=(1, 0, 0), at=0.35), CREAM, "head", fuse=False)
    for s in (1, -1):
        for j, (dz, ln) in enumerate(((0.0, 0.085), (-0.03, 0.07))):
            base = H + np.array([0.045 * s, -0.035, -0.025 + dz])
            tip = base + unit(0.7 * s, 1.0, -0.2) * ln
            k.add("cheek", p.spike(base, tip, 0.042, 0.022, side=(0, 0, 1), at=0.35), WHITE, "head", fuse=False)


def beak(k):
    k.add("beak", ell((0, -0.248, 0.432), (0.034, 0.056, 0.034), rot=(14, 0, 0), deform=p.hooked), YELLOW, "head",
          fuse=False, surface="shell")
    k.add("beaklow", ell((0, -0.226, 0.395), (0.026, 0.04, 0.011), rot=(-12, 0, 0), deform=p.pointed), YELLOW, "head",
          fuse=False, surface="shell")
    k.add("mouth", ell((0, -0.218, 0.407), (0.024, 0.03, 0.014)), PINK, "head", fuse=False, surface="skin")


def brow_mark(k):
    """The small blue mark on the brow over the beak's root."""
    k.decal(H + np.array([0, -HR[1] * 0.82, HR[2] * 0.45]), unit(0, -0.75, 0.66), (0.022, 0.024), BLUE,
            outline=polygon([(0.0, -1.0), (1.0, 0.75), (-1.0, 0.75)]), eps=0.004, rings=10, segs=48)


# --- the wings ------------------------------------------------------------------------------------------------------
ELEV, SWEEP = 50, 22
SPAN = [(0.0, -0.015), (0.09, -0.028), (0.19, -0.024), (0.28, -0.008), (0.345, 0.025)]
TIPS = [(0.335, 0.085), (0.285, 0.135), (0.23, 0.165), (0.17, 0.175), (0.11, 0.17), (0.05, 0.15)]
LEAD = [(0.0, -0.02), (0.09, -0.033), (0.19, -0.029), (0.28, -0.013), (0.325, 0.01), (0.25, 0.03), (0.15, 0.05),
        (0.06, 0.07), (0.0, 0.08)]


def wing_axes():
    e, s = math.radians(ELEV), math.radians(SWEEP)
    d = np.array([math.cos(e) * math.cos(s), math.cos(e) * math.sin(s), math.sin(e)])
    c0 = np.array([0.35, 0.3, -0.9])
    c = c0 - d * (d @ c0); c /= np.linalg.norm(c)
    return d, c


def wing_outline():
    out_ = list(SPAN)
    for i, (a, b) in enumerate(TIPS):
        out_.append((a, b))
        nxt = TIPS[i + 1] if i + 1 < len(TIPS) else (0.0, 0.13)
        out_.append(((a + nxt[0]) / 2 + 0.008, (b + nxt[1]) / 2 - 0.035))
    out_.append((0.0, 0.13))
    return out_


def wings(k):
    """Two plates a wing, the red above (its face to the back and in) and the dark under it, and a red leading edge."""
    d, c = wing_axes()
    n = np.cross(d, c)
    root = SH - d * 0.03
    k.add("wingtop", slab(wing_outline(), root + n * 0.0065, d, c, 0.013), RED, "wing{s}", fuse=False, mirror=True)
    k.add("wingunder", slab(wing_outline(), root - n * 0.0065, d, c, 0.013), NAVY, "wing{s}", fuse=False, mirror=True)
    k.add("winglead", slab(LEAD, root, d, c, 0.034), RED, "wing{s}", fuse=False, mirror=True)


# --- the tail -------------------------------------------------------------------------------------------------------
def feather_part(ln, w, a0, a1):
    prof = np.array(p.FEATHER)
    half = [(a * ln, float(np.interp(a, prof[:, 0], prof[:, 1])) * w / 2) for a in np.linspace(a0, a1, 7)]
    return half + [(a, -h) for a, h in half[::-1]]


def tail(k):
    base0 = np.array([0, 0.125, 0.27])
    pitch = math.radians(-22)
    for i, yaw in enumerate((-36, -18, 0, 18, 36)):
        y = math.radians(yaw)
        d = np.array([math.sin(y) * math.cos(pitch), math.cos(y) * math.cos(pitch), math.sin(pitch)])
        side = np.array([math.cos(y), -math.sin(y), 0])
        r = math.radians(1.4 * yaw)
        side = side * math.cos(r) - np.array([0, 0, 1.0]) * math.sin(r)
        b = base0 + np.array([math.sin(y) * 0.02, 0, -0.008 * abs(yaw) / 18])
        ln, w = 0.24 - 0.02 * abs(yaw) / 18, 0.085
        k.add("tail", slab(feather_part(ln, w, 0.0, 1.0), b, d, side, 0.016), RED, "tail", fuse=False)
        k.add("tailband", slab(feather_part(ln, w * 1.03, 0.6, 0.78), b, d, side, 0.022), BAND, "tail", fuse=False)
        k.add("tailtip", slab(feather_part(ln, w * 1.03, 0.78, 0.995), b, d, side, 0.022), BLUE, "tail", fuse=False)


# --- the feet, hanging forward and gripping ---------------------------------------------------------------------------
def feet(k):
    for s, side in ((1, "R"), (-1, "L")):
        x = 0.062 * s
        top = np.array([x, -0.1, 0.235])
        ank = np.array([x * 1.06, -0.15, 0.155])
        k.add(f"shin{side}", tube([top, (top + ank) / 2, ank], [0.024, 0.022, 0.02]), YELLOW, f"leg{side}",
              fuse=False, surface="scales")
        # shaggy dark feathers over the top of the shin
        for j, ang in enumerate((-70, -25, 20, 65, 110, 160, 210, 255)):
            rr = math.radians(ang)
            dd = np.array([math.sin(rr), -math.cos(rr), 0])
            b0 = top + dd * 0.026 + np.array([0, 0, 0.012])
            k.add(f"fluff{side}", p.spike(b0, b0 + dd * 0.028 + np.array([0, -0.012, -0.05 - 0.008 * (j % 2)]), 0.03,
                                          0.016, side=np.cross(dd, (0, 0, 1.0))), NAVY, f"leg{side}", fuse=False)
        for j, ang in enumerate((-30, 0, 30, 180)):
            rr = math.radians(ang)
            dd = np.array([math.sin(rr) * s, -math.cos(rr), 0])
            ln = 0.06 if ang != 180 else 0.035
            mid = ank + dd * ln * 0.55 + np.array([0, 0, 0.004])
            tip = ank + dd * ln + np.array([0, 0, -0.028])
            k.add(f"toe{side}", tube([ank, mid, tip], [0.015, 0.0135, 0.012]), YELLOW, f"leg{side}", fuse=False,
                  surface="scales")
            cd = unit(*(dd * 0.35 + np.array([0, 0, -1.0])))
            k.add(f"claw{side}", p.spike(tip, tip + cd * 0.03 - dd * 0.004, 0.019, 0.017), BLACK, f"leg{side}",
                  fuse=False, surface="shell")


# --- the eyes: small and fierce under the red brow ------------------------------------------------------------------
EYEDIR = unit(0.62, -0.74, 0.22)
EYE = (0.02, 0.016)
EYEOUT = [(-1.0, -0.15), (-0.55, 0.3), (0.0, 0.55), (0.55, 0.72), (1.0, 0.45), (0.85, -0.35), (0.3, -0.85),
          (-0.4, -0.8)]                       # u toward the back: the top slopes down to the beak under the brow
WHITEC, INKC = np.array([0.93, 0.94, 0.97]), srgb(BLACK)


def eye_open(u, v):
    c = np.tile(WHITEC, (len(u), 1))
    c[(u + 0.12) ** 2 + (v + 0.05) ** 2 < 0.6 ** 2] = srgb(IRIS)
    c[(u + 0.15) ** 2 + (v + 0.05) ** 2 < 0.3 ** 2] = INKC
    return c


def eyes(k):
    E = H + EYEDIR / np.linalg.norm(EYEDIR / HR)
    m = np.array([-1, 1, 1.0])
    LID = srgb(RED)
    def lay(state):
        look = eye_look(state, eye_open, LID, INKC, line=0.18)
        k.decal(E, EYEDIR, EYE, look, eps=0.004, outline=polygon(EYEOUT), rings=20, segs=56)
        k.decal(E * m, EYEDIR * m, EYE, lambda u, v: look(-u, v), eps=0.004,
                outline=polygon([(-a, b) for a, b in EYEOUT]), rings=20, segs=56)
    return lay


def braviary(src, out):
    k = Kit(src, coat="fur")
    body(k)
    thighs(k)
    brow_mark(k)
    crown(k)
    beak(k)
    wings(k)
    tail(k)
    feet(k)
    k.build(voxel=VOXEL, band=0.007)
    k.eye_states(eyes(k))
    k.export(out)


if __name__ == "__main__":
    braviary(*args())
