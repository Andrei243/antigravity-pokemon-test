# Tritanor sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/tritanor.py -- <the export's Tritanor.glb> <out.glb>
# Carpathia No. 10069, the crested newt (CP-dex-2-mountains.md, 69), grown from Tritonel (tritonel.py: its eye, its
# spots, its crest and fin sheets, its ellipsoid helpers). From its picture: a heavy newt standing on its bent hind
# legs, dark olive and warty, the trunk a pear leaning a little forward; a broad flat toad's head set low on the
# shoulders, golden eyes with a black slit on bulges at its top, a wide mouth over an orange jaw and throat; the orange
# belly spotted black from the throat to the groin; short arms bent forward with three pale claws, great haunches and
# short thick shins, broad feet with three pale claws; a long tail lying on the ground and curling round to the front,
# orange underneath. A tall jagged crest from the back of the head along the spine and the tail, like a banner: orange
# webbing on dark rays, each point tipped with a violet gland.
# The export's joints: body (0, 0, 0.52), arms (+-0.2, -0.06, 0.7), legs (+-0.14, 0, 0.38), tail (0, 0.16, 0.4),
# head (0, -0.06, 0.84).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, inside, unit, polygon, args, sibling

t = sibling("tritonel")

DARK = "#4C4E2C"           # the warty coat, Tritonel's olive grown dark
WART = "#3A3C22"           # the warts
BELLY = t.BELLY            # the line's orange
SPOT = "#261E14"           # the belly's spots, darker than Tritonel's
WEB = "#D2803A"            # the crest's webbing
RAY = "#5E5630"            # its rays
VIOLET = "#A254D4"         # the glands
CLAW = "#DED2B6"
GOLD = srgb("#F2C230")

BODY_C, BODY_R = np.array([0.0, -0.02, 0.54]), np.array([0.245, 0.225, 0.3])
HEAD_C, HEAD_R = np.array([0.0, -0.09, 0.885]), np.array([0.205, 0.185, 0.095])
SNOUT_C, SNOUT_R = np.array([0.0, -0.205, 0.87]), np.array([0.17, 0.085, 0.078])
JAW_C, JAW_R = np.array([0.0, -0.13, 0.8]), np.array([0.19, 0.15, 0.08])
TAIL = [(0.0, 0.17, 0.4), (0.04, 0.37, 0.22), (0.15, 0.55, 0.11), (0.33, 0.63, 0.072), (0.47, 0.55, 0.06),
        (0.52, 0.42, 0.05)]
TAIL_R = [0.13, 0.095, 0.07, 0.05, 0.034, 0.016]
# the crest's foot: from the back of the head down the spine and along the tail's top, a little under the skin
CREST = [(0.0, -0.06, 0.95), (0.0, 0.05, 0.9), (0.0, 0.13, 0.8), (0.0, 0.18, 0.66), (0.0, 0.2, 0.52),
         (0.0, 0.2, 0.4), (0.03, 0.34, 0.27), (0.13, 0.52, 0.15), (0.31, 0.62, 0.1), (0.46, 0.55, 0.085)]
POINTS = 13


def envelope(u):
    """How tall the crest stands along its foot: highest over the shoulders, low at the head and down the tail."""
    return np.interp(u, [0.0, 0.08, 0.22, 0.38, 0.55, 0.75, 1.0], [0.05, 0.16, 0.3, 0.26, 0.16, 0.1, 0.04])


def jag(u):
    """Points: each a sharp peak, the webbing dipping to half between."""
    f = (u * POINTS) % 1.0
    return 0.52 + 0.48 * (1 - np.abs(2 * f - 1)) ** 1.6


def tritanor(src, out):
    k = Kit(src, coat="scales")

    # The trunk: a pear, the belly low and full, leaning a little forward
    k.add("body", ell(BODY_C, BODY_R, rot=(-6, 0, 0)), DARK, "body")
    k.add("chest", ell((0, -0.06, 0.72), (0.21, 0.18, 0.15)), DARK, "body")
    k.add("rump", ell((0, 0.1, 0.42), (0.2, 0.15, 0.16)), DARK, "body")

    # The head: broad and flat, a wide rounded snout, a full orange jaw under it, eye bulges on its top
    k.add("head", ell(HEAD_C, HEAD_R), DARK, "head")
    k.add("snout", ell(SNOUT_C, SNOUT_R), DARK, "head")
    k.add("jaw", ell(JAW_C, JAW_R), DARK, "head")
    EB = []
    for s, side in ((1, "R"), (-1, "L")):
        c = np.array((0.125 * s, -0.135, 0.955))
        EB.append(c)
        k.add(f"brow{side}", ell(c, (0.062, 0.06, 0.05)), DARK, "head")

    # The arms: short and thick, bent forward, the hands before the belly with three claws
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"shoulder{side}", ell(np.array((0.2, -0.06, 0.7)) * m, (0.075, 0.075, 0.08)), DARK, f"arm{side}")
        k.add(f"arm{side}", tube(np.array([(0.2, -0.06, 0.7), (0.27, -0.12, 0.6), (0.235, -0.25, 0.52)]) * m,
                                 [0.064, 0.056, 0.048]), DARK, f"arm{side}")
        k.add(f"hand{side}", ell(np.array((0.23, -0.29, 0.5)) * m, (0.05, 0.045, 0.04)), DARK, f"arm{side}")
        for j, d in enumerate(((0.55, -0.6, -0.6), (0.0, -0.75, -0.65), (-0.5, -0.6, -0.65))):
            b = np.array((0.23, -0.3, 0.49)) + np.array(d) * 0.035
            dd = unit(*d)
            k.add(f"finger{side}{j}", tube(np.array([b, b + dd * 0.03]) * m, [0.022, 0.019]), DARK, f"arm{side}")
            tip = b + dd * 0.03
            a1 = np.cross(dd, (0, 0, 1.0)); a1 /= np.linalg.norm(a1)
            a2 = np.cross(a1, dd)
            claw = [tip + a1 * 0.018, tip - a1 * 0.018, tip + a2 * 0.017, tip - a2 * 0.017, tip + dd * 0.05]
            k.add(f"claw{side}{j}", hull(np.array(claw) * m), CLAW, f"arm{side}", fuse=False, surface="shell")

        # The legs: great haunches fused into the hips, the knees bent forward, short thick shins, broad feet
        k.add(f"thigh{side}", ell(np.array((0.17, -0.01, 0.33)) * m, (0.13, 0.15, 0.15), rot=(20, 0, 0)), DARK,
              f"leg{side}")
        k.add(f"shin{side}", tube(np.array([(0.2, -0.07, 0.22), (0.215, -0.05, 0.12), (0.215, -0.04, 0.085)]) * m,
                                  [0.095, 0.085, 0.075]), DARK, f"leg{side}")
        k.add(f"foot{side}", ell(np.array((0.22, -0.08, 0.035)) * m, (0.1, 0.13, 0.038)), DARK, f"leg{side}")
        for j, d in enumerate(((0.55, -0.85, -0.1), (0.0, -1.0, -0.1), (-0.55, -0.85, -0.1))):
            b = np.array((0.22, -0.16, 0.035)) + np.array((d[0] * 0.06, d[1] * 0.04, 0))
            dd = unit(*d)
            a1 = np.cross(dd, (0, 0, 1.0)); a1 /= np.linalg.norm(a1)
            claw = [b + a1 * 0.022, b - a1 * 0.022, b + (0, 0, 0.026), b + (0, 0, -0.012), b + dd * 0.07]
            k.add(f"toe{side}{j}", hull(np.array(claw) * m), CLAW, f"leg{side}", fuse=False, surface="shell")

    # The tail: a heavy tube lying back on the ground and curling round to the front at +X
    k.add("tail", tube(TAIL, TAIL_R), DARK, "tail")
    k.add("tailfin", t.fin_sheet(TAIL[1:-1], lambda u: 0.006 + 0.0 * u, lambda u: 0.036 * (1 - u) + 0.008, 9, 0.08, 0.03),
          BELLY, "tail", fuse=False)

    # Warts over the back, the shoulders, the haunches and the head's top
    rs = np.random.RandomState(10069)
    for i in range(46):
        d = rs.normal(size=3); d[1] = abs(d[1]) * 1.2 + 0.1; d /= np.linalg.norm(d)
        if d[2] < -0.5:
            continue
        p = t.on_ell(BODY_C, BODY_R, d)
        k.add(f"wart{i}", ell(p, (0.024, 0.024, 0.02)), WART, "body")
    for i, (x, y, z) in enumerate(((0.06, -0.02, 0.97), (-0.07, 0.0, 0.965), (0.17, 0.0, 0.93), (-0.17, 0.02, 0.92),
                                   (0.0, 0.03, 0.97))):
        k.add(f"hwart{i}", ell((x, y, z), (0.022, 0.022, 0.016)), WART, "head")
    for s, side in ((1, "R"), (-1, "L")):
        for i, (x, y, z) in enumerate(((0.26, 0.03, 0.38), (0.24, 0.1, 0.28), (0.29, -0.02, 0.27), (0.31, -0.1, 0.6),
                                       (0.24, 0.0, 0.76))):
            k.add(f"lwart{side}{i}", ell((x * s, y, z), (0.022, 0.022, 0.02)), WART, "body")

    # The crest: orange webbing along the spine and tail, standing out of the back square to it and leaned toward +X
    # (so that the front camera sees its face), dark rays in it, a small violet gland on each point
    way = lambda C: t.stand_out(C, up=0.55, tilt=24.0)
    k.add("web", t.frill(CREST, lambda u: envelope(u) * jag(u), 0, 0.0, 0.036, n=260, nv=8, way=way), WEB, "body",
          fuse=False)
    C = t.curve(CREST)
    D = way(C)
    T = np.gradient(C, axis=0); T /= np.linalg.norm(T, axis=1, keepdims=True)
    for i in range(POINTS):
        u = (i + 0.5) / POINTS
        j = int(round(u * (len(C) - 1)))
        base, d, tg = C[j], D[j], T[j]
        h = float(envelope(u))
        tip = base + d * h
        side = np.cross(tg, d); side /= np.linalg.norm(side)
        bone = "head" if u < 0.06 else ("body" if u < 0.45 else "tail")
        w = 0.03 + 0.03 * h / 0.3
        mid = base + d * h * 0.55
        ray = [base - tg * w, base + tg * w, base + side * 0.026, base - side * 0.026,
               mid + side * 0.015, mid - side * 0.015, mid + tg * 0.012, tip]
        k.add(f"ray{i}", hull(ray), RAY, bone, fuse=False)
        g = 0.021 + 0.016 * h / 0.3
        gl = tip + d * g * 0.6
        k.add(f"gland{i}", tube([gl - d * g * 0.5, gl + d * g * 0.5], [g, g * 0.8], seg=14, per=4), VIOLET, bone,
              fuse=False)

    # The orange: the jaw and throat under the mouth, and the belly down to the groin
    def under(P):
        q = (P - BODY_C) / BODY_R
        # an oval on the front, inside the forearms
        oval = (P[:, 0] / 0.17) ** 2 + ((P[:, 2] - 0.54) / 0.33) ** 2 - 1
        belly = inside(q[:, 1] + 0.45, 0.025) * inside(oval, 0.05) * inside(P[:, 2] - 0.84, 0.01)
        jaw = inside(P[:, 2] - (0.84 - 0.3 * np.clip(P[:, 1] + 0.27, 0, 1) ** 1.0), 0.004) * (P[:, 2] > 0.7) \
            * inside(P[:, 1] + 0.0, 0.01) * inside(np.abs(P[:, 0]) - 0.185, 0.006)
        return np.maximum(belly, jaw)
    k.paint(under, BELLY)
    # the mouth: a wide dark line across the snout's front where the orange meets the dark
    wide = [(u, 0.1 + 0.3 * u * u) for u in np.linspace(1, -1, 15)] + \
           [(u, -0.14 + 0.3 * u * u) for u in np.linspace(-1, 1, 15)]
    k.decal((0, -0.288, 0.842), unit(0, -1, -0.1), (0.16, 0.04), "#1E1A10", eps=0.004, outline=polygon(wide),
            rings=8, segs=80)

    # Spots on the orange
    places = []
    for d, h in (((0.0, -1, 0.35), 0.026), ((0.3, -0.9, 0.2), 0.024), ((-0.3, -0.9, 0.3), 0.024),
                 ((0.15, -0.95, 0.05), 0.026), ((-0.25, -0.95, -0.05), 0.026), ((0.34, -0.85, -0.15), 0.022),
                 ((-0.34, -0.85, -0.2), 0.022), ((0.1, -0.95, -0.3), 0.025), ((-0.12, -0.9, -0.45), 0.024),
                 ((0.35, -0.85, -0.45), 0.022), ((-0.3, -0.85, 0.05), 0.02), ((0.3, -0.9, 0.45), 0.02),
                 ((0.0, -0.85, -0.6), 0.022)):
        places.append((BODY_C, BODY_R, d, h))
    for d, h in (((0.3, -0.8, -0.5), 0.018), ((-0.35, -0.8, -0.45), 0.018), ((0.0, -0.75, -0.65), 0.018),
                 ((0.6, -0.6, -0.5), 0.016), ((-0.6, -0.6, -0.5), 0.016)):
        places.append((JAW_C, JAW_R, d, h))
    # cast nearly straight from the front, so that no ray lands on the forearms before the belly
    for c, r, d, h in places:
        n = t.normal_on(c, r, d)
        k.decal(t.on_ell(c, r, d), unit(n[0] * 0.35, n[1], n[2] * 0.6), (h, h), SPOT, eps=0.004, rings=6, segs=28)
    # Nostrils
    for s in (1, -1):
        d = (0.22 * s, -0.8, 0.55)
        k.decal(t.on_ell(SNOUT_C, SNOUT_R, d), t.normal_on(SNOUT_C, SNOUT_R, d), (0.014, 0.011), "#1E1A10",
                eps=0.004, rings=4, segs=20)

    # The eyes: golden with a black slit, on the bulges, looking forward and out
    de = unit(0.5, -0.72, 0.48)
    E = t.on_ell(EB[0], (0.062, 0.06, 0.05), de)
    look = t.round_eye(GOLD, pupil=srgb("#120E08"), rim=srgb("#1A140A"), slit=0.22)
    lay = t.lay_eyes(k, E, t.normal_on(EB[0], (0.062, 0.06, 0.05), de), (0.046, 0.046), look, DARK, srgb("#1A140A"))

    k.build(voxel=0.0115, smooth=4, band=0.012, wband=0.08)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    tritanor(*args())
