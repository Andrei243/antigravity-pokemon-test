# Shiinotic sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/shiinotic.py -- <the export's Shiinotic.glb> <out.glb>
# Grown from morelull.py (sibling("morelull")): its black oval eyes and green cheek dots laid the same way, its wavy
# band painted in the cap's own frame, its colours' way.
#
# From the references: a tall mushroom person under a great cap wider than it is tall: a purple dome with round lime
# spots, a pink ruffle round its lower edge rising into the purple in waves, lime gills under it. Under the cap a white
# spoon of a head on a short stalk with a blank face: two black upright ovals, a small black smile, a lime dot on each
# cheek; a white neck down to a round pink bulb of a body, frilled at its top, on two little pink feet; thin white arms
# from the neck down beside the bulb, each ending in three lime finger tendrils.
# (The task's "wide spotted cap, a blank smiling face and long arms with finger tendrils" hold; the cap's spots are
# lime, its frill pink; the face is set lower and the cap's underside rises outward so the face shows from the game's
# front camera.)
# Skeleton: body (0, 0, 0.16), arms (+-0.015, 0, 0.18), head (0, 0, 0.3), legR (0, 0, 0.05): the bulb on legR, the
# neck and face on body, the cap on head.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, args, sibling

m = sibling("morelull")

WHITE_ = "#F6F4EC"
PURPLE, PINK, LIME, SPOT = "#B47FC6", "#F4C2D4", "#D6EAA2", "#C9E58C"
BULB = "#F2B8CE"
FINGER = "#CDE89A"
INK = m.INK

# --- Shiinotic's frame (about 0.48 tall) ----------------------------------------------------------------------------
CAP = np.array([0.0, 0.006, 0.33])        # the cap's middle, at its rim
CAP_R = np.array([0.215, 0.205, 0.155])   # its radii, the dome's height above the rim
CAP_UNDER = 0.48                          # the gills' depth as a share of the dome's
HC = np.array([0.0, -0.004, 0.21])        # the face's middle
HR = np.array([0.043, 0.038, 0.047])
EYE_X, EYE_Z, EYE_HALF = 0.0165, 0.221, (0.0085, 0.0135)
VOXEL = 0.0045


def cap_shape(V):
    """The unit sphere made a cap: the dome above, the gills a shallow bowl under it."""
    V = V.copy()
    V[:, 2] = np.where(V[:, 2] < 0, V[:, 2] * CAP_UNDER, V[:, 2])
    return V


def shiinotic(src, out_path):
    k = Kit(src, coat="leaf")

    # ---- the cap: a purple dome, lime gills beneath, a pink ruffle round its edge rising into the purple in waves
    k.add("cap", ell(CAP, CAP_R, deform=cap_shape), PURPLE, "head")
    def gills(P):
        q = P - CAP
        return inside(q[:, 2] + 0.004, 0.003) * inside(np.linalg.norm(q / CAP_R, axis=1) - 1.2, 0.01)
    def ruffle_band(P):
        q = P - CAP
        th = np.arctan2(q[:, 1], q[:, 0])
        return inside(q[:, 2] - (0.032 + 0.012 * np.sin(9 * th)), 0.003) * inside(-q[:, 2] - 0.004, 0.003)
    k.paint(ruffle_band, PINK, on=["cap"])
    k.paint(gills, LIME, on=["cap"])
    for i in range(30):
        a = 2 * math.pi * i / 30
        w = 0.008 * math.sin(7 * a)
        k.add(f"frill{i}", ell((0.214 * math.cos(a), 0.204 * math.sin(a) + 0.006, 0.322 + w),
                               (0.04, 0.04, 0.03), sub=3), PINK, "head")
    k.add("stalk", tube([(0, 0.0, 0.232), (0, 0.003, 0.29), (0, 0.006, 0.32)], [0.021, 0.022, 0.03]), WHITE_, "head")

    # ---- the face: a white spoon of a head on a short neck, a smile and lime cheeks
    k.add("face", ell(HC, HR), WHITE_, "body")
    k.add("neck", tube([(0, 0.0, 0.12), (0, -0.002, 0.16), (0, -0.004, 0.19)], [0.02, 0.019, 0.024]), WHITE_, "body")
    m.cheeks(k, HC, HR, 0.031, 0.21, (0.0068, 0.0058))
    CW, CK = srgb(WHITE_), INK
    def smile(u, v):
        c = np.tile(CW, (len(u), 1))
        c[(np.abs(v - (0.55 * u * u - 0.25)) < 0.3) & (np.abs(u) < 0.92)] = CK
        return c
    sp, sn = m.on_ell(HC, HR, (0.0, -1.0, -0.28))
    k.decal(sp, sn, (0.013, 0.0075), smile, eps=0.0015, rings=12, segs=56)

    # ---- the arms: thin white arms from the neck down beside the bulb, three lime fingers each
    for s in (1, -1):
        side = "R" if s > 0 else "L"
        hand = np.array([0.066 * s, -0.036, 0.088])
        k.add("arm" + side, tube([(0.016 * s, -0.004, 0.168), (0.048 * s, -0.022, 0.138), hand],
                                 [0.0085, 0.008, 0.0078]), WHITE_, "arm" + side)
        for j, (dx, dy) in enumerate(((0.012, -0.004), (0.0, -0.012), (-0.01, -0.004))):
            k.add(f"finger{j}{side}", tube([hand, hand + np.array([dx * s * 0.6, dy * 0.6, -0.022]),
                                            hand + np.array([dx * s, dy, -0.046])], [0.0076, 0.0074, 0.007]),
                  FINGER, "arm" + side)

    # ---- the bulb: round and pink, frilled at its top, on two little feet
    k.add("bulb", ell((0, 0.0, 0.074), (0.062, 0.056, 0.056)), BULB, "legR")
    for i in range(8):
        a = 2 * math.pi * (i + 0.5) / 8
        k.add(f"bulbfrill{i}", ell((0.046 * math.sin(a), -0.042 * math.cos(a), 0.118), (0.022, 0.022, 0.018), sub=3),
              BULB, "legR")
    for s in (1, -1):
        k.add("foot" + ("R" if s > 0 else "L"), ell((0.026 * s, -0.018, 0.013), (0.022, 0.026, 0.013), sub=3), BULB,
              "legR")


    # ---- the spots: round lime patches on the dome, cast from above
    def spots():
        for (x, y) in ((0.0, 0.0), (0.1, -0.06), (-0.1, -0.06), (0.0, -0.13), (0.13, 0.05), (-0.13, 0.05),
                       (0.0, 0.11)):
            p = CAP + np.array([x, y, 0])
            q = np.array([x / CAP_R[0], y / CAP_R[1]])
            z = math.sqrt(max(1 - q @ q, 0.0)) * CAP_R[2]
            p = p + np.array([0, 0, z])
            n = np.array([x / CAP_R[0] ** 2, y / CAP_R[1] ** 2, z / CAP_R[2] ** 2]); n /= np.linalg.norm(n)
            k.decal(p, n, (0.026, 0.02) if (x, y) != (0.0, 0.0) else (0.018, 0.018), SPOT, up=(0, 1, 0), eps=0.0012,
                    rings=8, segs=32)
    spots()
    k.build(voxel=VOXEL, smooth=5, band=0.004, wband=0.03)

    def eyes(state):
        m.lay_eyes(k, HC, HR, EYE_X, EYE_Z, EYE_HALF, WHITE_, state)
    k.eye_states(eyes)
    k.export(out_path)


if __name__ == "__main__":
    shiinotic(*args())
