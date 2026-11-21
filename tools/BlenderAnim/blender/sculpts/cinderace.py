# Cinderace sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/cinderace.py -- <the export's Cinderace.glb> <out.glb>
# Grown from scorbunny.py (its ears, stripes, tufts, feet, eyes and tail, built by its functions).
#
# From the references (they overrule the brief, which spoke of red-tipped ears and a black-and-red top): a tall white
# rabbit, its head white with a tall red flame of a crest rising from the brow between two long white ears that curve
# up and out, each with a yellow stripe up its front; a gold band across the brow coming to a point between the eyes;
# great white tufts flaring from the cheeks; sharp red eyes ringed dark; a small open mouth. A white chest under a navy
# collar with pointed flaps; long white arms with white hands; red-orange shorts on full thighs, a yellow mark like a
# spark on each, yellow tufts of fur at the knees; long navy shins; red-orange feet with yellow pads under them; a white
# tuft of fur at the hips behind and a small white tail.
# The export's joints: body (0, 0, 0.5), head (0, 0, 0.66), arms (+-0.06, 0, 0.62), legs (+-0.055, 0, 0.42), tail
# (0, 0.05, 0.44). About 0.95 tall.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, outward, polygon, srgb, inside, unit, boxy, lumpy, args, sibling

sc = sibling("scorbunny")
WHITE, RED, YELLOW, GOLD = sc.WHITE, "#E0452A", "#F6C93A", "#F2B92E"
NAVY, PINK, INK = "#1E2A52", "#E8828E", sc.INK3
IRIS, PUPIL, SCL = srgb("#E8341E"), srgb("#1A1014"), srgb("#F8F4EE")

H = np.array([0.0, -0.014, 0.745])           # the head's centre, a third over the measure
HR = np.array([0.1, 0.093, 0.097])
DE = unit(0.5, -0.85, 0.08)
EYE_HALF = (0.031, 0.021)
# a sharp eye: the top a line sloping down toward the nose, the bottom round (u outward)
EYE_SHAPE = ([(u, 0.55 + 0.35 * u) for u in np.linspace(1.0, -1.0, 9)]
             + [(u, -0.8 * math.sqrt(max(0.0, 1 - u * u)) + 0.1 * u) for u in np.linspace(-0.95, 0.95, 13)])
EAR_LEN, EAR_W, EAR_T = 0.24, 0.042, 0.015


def cinderace(src, path):
    k = Kit(src, coat="fur")

    # --- the head: white, a muzzle a little forward ---------------------------------------------------------------------
    k.add("head", ell(H, HR, deform=boxy(2.2)), WHITE, "head")
    k.add("snout", ell(H + (0, -0.07, -0.032), (0.048, 0.039, 0.039)), WHITE, "head")
    k.add("cheek", ell(H + (0.053, -0.039, -0.039), (0.046, 0.044, 0.041)), WHITE, "head", mirror=True)
    k.add("neck", tube([(0, 0.0, 0.62), (0, -0.006, 0.69)], [0.042, 0.04]), WHITE, "head")

    # --- the gold band across the brow, coming to a point between the eyes ------------------------------------------
    def tiara(P):
        z = P[:, 2] - H[2]
        x = np.abs(P[:, 0])
        front = inside(P[:, 1] - (H[1] - 0.035), 0.002) * inside(x - 0.086, 0.002)
        bandlo = 0.036 + 0.12 * x
        point = 0.042 - 0.05 * np.clip(1 - x / 0.02, 0, 1)
        lo = np.minimum(bandlo, point)
        return front * inside(lo - z, 0.0018) * inside(z - (0.06 + 0.12 * x), 0.0018)
    k.paint(tiara, GOLD, on=["head", "snout"])

    # --- the crest: a tall red flame rising from the brow, a yellow streak in it ------------------------------------------
    def flame(V):
        V = V.copy()
        t = (V[:, 2] + 1) / 2
        w = np.where(t < 0.35, 0.45 + 0.55 * np.sin(0.5 * math.pi * t / 0.35),
                     np.cos(0.5 * math.pi * (t - 0.35) / 0.65) ** 0.9)
        V[:, 0] *= w; V[:, 1] *= 0.5 + 0.5 * w
        return V
    base = H + np.array([0, -0.07, 0.05])
    d = unit(0, 0.2, 1)
    k.add("crest", outward(ell(base + d * 0.11, (0.042, 0.02, 0.11), rot=(-11.3, 0, 0), deform=flame)), RED,
          "head", fuse=False)
    k.add("crestlight", outward(ell(base + d * 0.1 + (-0.016, -0.0, 0.0), (0.009, 0.022, 0.06),
                                             rot=(-11.3, -12, 0), deform=flame)), YELLOW, "head", fuse=False)

    # --- the ears: long, white, curving up and out, a yellow stripe up each front -------------------------------------
    eb, em = sc.ear(k, H + (0.052, 0.012, 0.074), EAR_LEN, EAR_W, EAR_T, (-6, 26, 8), "head", WHITE, widest=0.45,
                    root=0.55, tipr=0.15)
    sc.ear_stripe(k, eb, em, EAR_LEN, EAR_W, EAR_T, 0.12, 0.62, 0.32, YELLOW)

    # --- great white tufts flaring from the cheeks --------------------------------------------------------------------
    sc.tuft(k, "cheektuft", H + (0.09, -0.01, -0.034), [(1, 0.15, 0.35), (1, 0.2, -0.05), (1, 0.1, -0.45)],
            [0.07, 0.06, 0.045], 0.022, WHITE, "head", thick=0.016)

    # --- a white chest, a narrow waist, a navy collar with pointed flaps --------------------------------------------------
    k.add("chest", ell((0, -0.004, 0.565), (0.07, 0.058, 0.075)), WHITE, "body")
    k.add("waist", ell((0, 0.004, 0.5), (0.056, 0.05, 0.05)), WHITE, "body")
    k.add("collar", ell((0, 0.0, 0.628), (0.076, 0.066, 0.026)), NAVY, "body")
    for i, (root, dd, L, w) in enumerate([((0.026, -0.058, 0.628), (0.35, -0.3, -0.88), 0.06, 0.022),
                                           ((0.066, -0.03, 0.632), (0.9, -0.15, -0.35), 0.05, 0.02),
                                           ((0.05, 0.045, 0.632), (0.5, 0.6, -0.4), 0.045, 0.02)]):
        root = np.array(root, float); dd = unit(*dd)
        a = np.cross(dd, (0, 0, 1.0)); a /= np.linalg.norm(a)
        n = np.cross(a, dd)
        k.add(f"flap{i}", hull([root + a * w, root - a * w, root + n * 0.011, root - n * 0.011,
                                root + dd * L * 0.4 + a * w * 0.8, root + dd * L * 0.4 - a * w * 0.8, root + dd * L]),
              NAVY, "body", fuse=False, mirror=True)

    # --- long white arms bent forward, white hands before the belly ----------------------------------------------------
    sh, el, wr = np.array([0.066, -0.002, 0.618]), np.array([0.104, -0.012, 0.532]), np.array([0.062, -0.078, 0.5])
    k.add("arm", tube([sh, el, wr], [0.03, 0.026, 0.024]), WHITE, "arm{s}", mirror=True)
    k.add("hand", ell(wr + (0.0, -0.022, -0.004), (0.028, 0.028, 0.03)), WHITE, "arm{s}", mirror=True)

    # --- red-orange shorts on full thighs, a yellow spark on each; yellow tufts at the knees ---------------------------
    k.add("hips", ell((0, 0.008, 0.45), (0.088, 0.07, 0.058)), RED, "body")
    k.add("thigh", ell((0.062, -0.004, 0.37), (0.062, 0.066, 0.088)), RED, "leg{s}", mirror=True)
    k.add("knee", ell((0.066, -0.01, 0.282), (0.05, 0.052, 0.03), deform=lumpy(7, 2, 0.22)), YELLOW, "leg{s}",
          mirror=True)
    # navy shins, long, a little thick; red-orange feet with yellow pads under them
    k.add("shin", tube([(0.066, -0.008, 0.27), (0.068, 0.0, 0.16), (0.068, 0.004, 0.06)], [0.039, 0.034, 0.031]),
          NAVY, "leg{s}", mirror=True)
    sc.foot(k, (0.068, 0.002, 0.04), (0.042, 0.078, 0.036), RED, RED, pad=YELLOW)
    k.paint(lambda P: inside(P[:, 2] - 0.065, 0.002) * inside(0.045 - P[:, 2], 0.002) * (np.abs(P[:, 0]) > 0.025)
            * (P[:, 1] > -0.045), RED, on=["shin", "foot"])

    # --- white fur at the hips behind, a small white tail ----------------------------------------------------------------
    for i, (root, dd, L) in enumerate([((0.06, 0.045, 0.46), (0.75, 0.6, -0.3), 0.07),
                                        ((0.03, 0.06, 0.45), (0.3, 0.9, -0.35), 0.06)]):
        sc.tuft(k, f"hipfur{i}", root, [dd], [L], 0.024, WHITE, "body", thick=0.018)
    sc.tail(k, (0, 0.074, 0.46), (0.03, 0.028, 0.03), WHITE, "tail")

    # --- the face: a small open mouth, the sparks on the thighs --------------------------------------------------------
    MO = polygon([(-1, 0.6), (1, 0.6), (0.7, -0.4), (0, -1), (-0.7, -0.4)])
    CM, CT = srgb("#5A2238"), srgb(PINK)

    def mouth(u, v):
        c = np.tile(CM, (len(u), 1))
        c[(u / 0.7) ** 2 + ((v + 0.8) / 0.6) ** 2 < 1] = CT
        return c
    k.decal(H + (0, -0.104, -0.058), unit(0, -1, -0.3), (0.016, 0.012), mouth, eps=0.002, outline=MO, rings=10, segs=48)
    k.decal(sc.on_ell(H + (0, -0.07, -0.032), (0.048, 0.039, 0.039), (0, -1, 0.1)), unit(0, -1, 0.1),
            (0.006, 0.0045), "#D06A78", eps=0.002, rings=6, segs=24)
    SPARK = [(-1, -0.1), (-0.25, 0.5), (0.15, 0.12), (1, 0.45), (0.3, -0.5), (-0.1, -0.12)]
    for side in (1, -1):
        k.decal((0.074 * side, -0.066, 0.39), unit(0.35 * side, -1, 0.05), (0.034, 0.02), YELLOW, eps=0.002,
                outline=polygon([(u * side, v) for u, v in SPARK]), rings=10, segs=48)

    k.build(voxel=0.0075, band=0.006, wband=0.045)

    E = sc.on_ell(H, HR, DE)
    lid = srgb(WHITE)
    NV = srgb(NAVY)

    def look(u, v):
        c = np.tile(SCL, (len(u), 1))
        c[(u + 0.1) ** 2 + (v + 0.1) ** 2 < 0.62 ** 2] = IRIS
        c[((u + 0.1) / 0.24) ** 2 + ((v + 0.1) / 0.36) ** 2 < 1] = PUPIL
        c[(u + 0.3) ** 2 + (v - 0.12) ** 2 < 0.13 ** 2] = sc.GLINT
        top = 0.55 + 0.35 * u
        c[v > top - 0.22] = NV                                   # the dark lid along the top
        c[(u > 0.55) & (v > -0.2)] = NV                          # the dark ring at the outer corner
        return c
    k.eye_states(lambda state: sc.lay_eyes(k, state, E, DE, EYE_HALF, look, lid, INK, outline=EYE_SHAPE))
    k.export(path)


if __name__ == "__main__":
    cinderace(*args())
