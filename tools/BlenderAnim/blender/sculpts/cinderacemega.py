# Mega Cinderace (a Mega of our own, which no game has) sculpted from nothing on the skeleton and clips of the game's
# own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/cinderacemega.py -- <the export's Cinderace-Mega.glb> <out.glb>
#
# No pictures: the design is the game's own sculptor (PokemonModels.Ours.Megas.cs, CinderaceMega) and the look of its
# export. Cinderace as cinderace.py builds it (copied here, since that script builds in one function; its scorbunny.py
# helpers are called as it calls them), with what the Mega changes:
# - its red deepened to crimson (the shorts, the feet);
# - its long ears swept back and turned to flame toward their tips, each tip streaming a tongue of fire back, and the
#   crest of flame over the brow laid back and lengthened into a crest streaming behind the head;
# - its white ruff become a scarf: a white roll round the neck over the navy collar, two tails trailing behind it and
#   out to +X;
# - a captain's band of gold round its right arm;
# - a ball of fire held under its right foot, ready to kick, tongues of flame on it.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, lathe, ring_band, outward, polygon, srgb, inside, unit, boxy, lumpy, args, \
    sibling

sc = sibling("scorbunny")
cd = sibling("cinderace")
WHITE, YELLOW, GOLD, NAVY, PINK, INK = cd.WHITE, cd.YELLOW, cd.GOLD, cd.NAVY, cd.PINK, cd.INK
IRIS, PUPIL, SCL = cd.IRIS, cd.PUPIL, cd.SCL
CRIMSON, RIM, HEART, BAND, FIRE = "#B0283A", "#F05A28", "#FFDC6E", "#ECBE40", "#F47828"

H, HR, DE, EYE_HALF, EYE_SHAPE = cd.H, cd.HR, cd.DE, cd.EYE_HALF, cd.EYE_SHAPE
EAR_LEN, EAR_W, EAR_T = cd.EAR_LEN, cd.EAR_W, cd.EAR_T
EAR_BASE = H + (0.052, 0.012, 0.074)
EAR_ROT = (-28, 24, 8)                       # swept back (Cinderace's: -6, 26, 8)


def join(shapes):
    V, F, off = [], [], 0
    for v, f in shapes:
        V.append(np.asarray(v)); F.extend([[i + off for i in q] for q in f]); off += len(v)
    return np.vstack(V), F


def tongue(base, tip, r):
    """A tongue of flame from base to tip: swelling, then drawn to a point."""
    d = np.array(tip, float) - np.array(base, float)
    L = np.linalg.norm(d)
    return lathe(base, d / L, [(0, 0), (r * 0.7, 0.08 * L), (r, 0.3 * L), (r * 0.75, 0.6 * L), (r * 0.3, 0.88 * L),
                               (0, L)], seg=20)


def cinderace_mega(src, path):
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

    # --- the crest of flame: laid back and longer, streaming behind the head, a yellow heart in it -------------------
    def flame(V):
        V = V.copy()
        t = (V[:, 2] + 1) / 2
        w = np.where(t < 0.35, 0.45 + 0.55 * np.sin(0.5 * math.pi * t / 0.35),
                     np.cos(0.5 * math.pi * (t - 0.35) / 0.65) ** 0.9)
        V[:, 0] *= w; V[:, 1] *= 0.5 + 0.5 * w
        return V
    base = H + np.array([0, -0.06, 0.06])
    dc = unit(0, 0.72, 0.7)
    tilt = -math.degrees(math.atan2(dc[1], dc[2]))
    k.add("crest", outward(ell(base + dc * 0.13, (0.046, 0.022, 0.135), rot=(tilt, 0, 0), deform=flame)), RIM,
          "head", fuse=False, glow=RIM)
    k.add("crestlight", outward(ell(base + dc * 0.115 + (0, 0, 0.012), (0.012, 0.024, 0.075), rot=(tilt, 0, 0),
                                    deform=flame)), HEART, "head", fuse=False, glow=HEART)

    # --- the ears: long and white, swept back, turned to flame toward their tips ---------------------------------------
    eb, em = sc.ear(k, EAR_BASE, EAR_LEN, EAR_W, EAR_T, EAR_ROT, "head", WHITE, widest=0.45, root=0.55, tipr=0.15)
    sc.ear_stripe(k, eb, em, EAR_LEN, EAR_W, EAR_T, 0.12, 0.5, 0.32, YELLOW)
    sc.ear_band(k, eb, em, EAR_LEN, EAR_W, EAR_T, 0.6, 1.1, RIM)
    sc.ear_band(k, eb, em, EAR_LEN, EAR_W, EAR_T, 0.5, 0.6, "#F7A24A")
    # a tongue of fire from each ear's tip, streaming back
    tongues = []
    for s in (1, -1):
        mm = np.array([s, 1, 1.0])
        tip = (eb + em @ np.array([0, 0, EAR_LEN * 0.9])) * mm
        tongues.append(tongue(tip, tip + np.array([0.035 * s, 0.17, 0.02]), 0.026))
    k.add("earflames", join(tongues), RIM, "head", fuse=False, glow=RIM)

    # --- great white tufts flaring from the cheeks --------------------------------------------------------------------
    sc.tuft(k, "cheektuft", H + (0.09, -0.01, -0.034), [(1, 0.15, 0.35), (1, 0.2, -0.05), (1, 0.1, -0.45)],
            [0.07, 0.06, 0.045], 0.022, WHITE, "head", thick=0.016)

    # --- a white chest, a narrow waist, a navy collar with pointed flaps --------------------------------------------------
    k.add("chest", ell((0, -0.004, 0.565), (0.07, 0.058, 0.075)), WHITE, "body")
    k.add("waist", ell((0, 0.004, 0.5), (0.056, 0.05, 0.05)), WHITE, "body")
    k.add("collar", ell((0, 0.0, 0.628), (0.076, 0.066, 0.026)), NAVY, "body")
    for i, (root, dd, L, w) in enumerate([((0.026, -0.058, 0.628), (0.35, -0.3, -0.88), 0.06, 0.022),
                                           ((0.066, -0.03, 0.632), (0.9, -0.15, -0.35), 0.05, 0.02)]):
        root = np.array(root, float); dd = unit(*dd)
        a = np.cross(dd, (0, 0, 1.0)); a /= np.linalg.norm(a)
        n = np.cross(a, dd)
        k.add(f"flap{i}", hull([root + a * w, root - a * w, root + n * 0.011, root - n * 0.011,
                                root + dd * L * 0.4 + a * w * 0.8, root + dd * L * 0.4 - a * w * 0.8, root + dd * L]),
              NAVY, "body", fuse=False, mirror=True)

    # --- the scarf: a white roll round the neck over the collar, two tails trailing back and out to +X ----------------
    th = np.linspace(0, 2 * math.pi, 37)[:-1] + math.pi / 2          # the roll's seam behind
    roll = [np.array([0.064 * math.cos(a), 0.058 * math.sin(a), 0.648 - 0.006 * math.sin(a)]) for a in th]
    scarf = [tube(roll + roll[:3], [0.02] * (len(roll) + 3), seg=14, per=3)]
    for pts, rad in (([(0.02, 0.05, 0.648), (0.06, 0.12, 0.628), (0.105, 0.19, 0.6), (0.145, 0.25, 0.56),
                       (0.17, 0.29, 0.515)], [0.03, 0.03, 0.029, 0.026, 0.022]),
                     ([(-0.015, 0.055, 0.64), (0.01, 0.12, 0.595), (0.045, 0.17, 0.55), (0.075, 0.21, 0.5),
                       (0.09, 0.235, 0.455)], [0.029, 0.029, 0.027, 0.024, 0.02])):
        scarf.append(tube(pts, rad, seg=14, per=6))
    k.add("scarf", outward(join(scarf)), WHITE, "body", fuse=False, surface="cloth")

    # --- long white arms bent forward, white hands before the belly ----------------------------------------------------
    sh, el, wr = np.array([0.066, -0.002, 0.618]), np.array([0.104, -0.012, 0.532]), np.array([0.062, -0.078, 0.5])
    k.add("arm", tube([sh, el, wr], [0.03, 0.026, 0.024]), WHITE, "arm{s}", mirror=True)
    k.add("hand", ell(wr + (0.0, -0.022, -0.004), (0.028, 0.028, 0.03)), WHITE, "arm{s}", mirror=True)
    # the captain's band of gold round the right arm (+X)
    k.add("armband", ring_band(sh * 0.45 + el * 0.55, el - sh, 0.0285, 0.026, thick=0.008), BAND, "armR", fuse=False,
          surface="metal")

    # --- crimson shorts on full thighs, a yellow spark on each; yellow tufts at the knees -------------------------------
    k.add("hips", ell((0, 0.008, 0.45), (0.088, 0.07, 0.058)), CRIMSON, "body")
    k.add("thigh", ell((0.062, -0.004, 0.37), (0.062, 0.066, 0.088)), CRIMSON, "leg{s}", mirror=True)
    k.add("knee", ell((0.066, -0.01, 0.282), (0.05, 0.052, 0.03), deform=lumpy(7, 2, 0.22)), YELLOW, "leg{s}",
          mirror=True)
    k.add("shin", tube([(0.066, -0.008, 0.27), (0.068, 0.0, 0.16), (0.068, 0.004, 0.06)], [0.039, 0.034, 0.031]),
          NAVY, "leg{s}", mirror=True)
    sc.foot(k, (0.068, 0.002, 0.04), (0.042, 0.078, 0.036), CRIMSON, CRIMSON, pad=YELLOW)
    k.paint(lambda P: inside(P[:, 2] - 0.065, 0.002) * inside(0.045 - P[:, 2], 0.002) * (np.abs(P[:, 0]) > 0.025)
            * (P[:, 1] > -0.045), CRIMSON, on=["shin", "foot"])

    # --- white fur at the hips behind, a small white tail ----------------------------------------------------------------
    for i, (root, dd, L) in enumerate([((0.06, 0.045, 0.46), (0.75, 0.6, -0.3), 0.07),
                                        ((0.03, 0.06, 0.45), (0.3, 0.9, -0.35), 0.06)]):
        sc.tuft(k, f"hipfur{i}", root, [dd], [L], 0.024, WHITE, "body", thick=0.018)
    sc.tail(k, (0, 0.074, 0.46), (0.03, 0.028, 0.03), WHITE, "tail")

    # --- the ball of fire under the right foot, ready to kick -----------------------------------------------------------
    B = np.array([0.074, -0.135, 0.05])
    k.add("fireball", outward(ell(B, (0.05, 0.05, 0.05), sub=4)), FIRE, "legR", fuse=False, glow=FIRE)
    k.add("fireheart", outward(ell(B + (0.008, -0.028, 0.012), (0.03, 0.026, 0.03))), HEART, "legR", fuse=False,
          glow=HEART)
    fl = [tongue(B + unit(*d) * 0.04, B + unit(*d) * 0.105, 0.017)
          for d in ((0.4, 0.1, 1), (-0.5, 0.2, 1), (0.0, -0.6, 1), (0.6, -0.3, 0.6))]
    k.add("ballflames", join(fl), RIM, "legR", fuse=False, glow=RIM)

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
        c[v > top - 0.22] = NV
        c[(u > 0.55) & (v > -0.2)] = NV
        return c
    k.eye_states(lambda state: sc.lay_eyes(k, state, E, DE, EYE_HALF, look, lid, INK, outline=EYE_SHAPE))
    k.export(path)


if __name__ == "__main__":
    cinderace_mega(*args())
