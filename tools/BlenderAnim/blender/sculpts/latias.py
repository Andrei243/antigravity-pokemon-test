# Latias sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/latias.py -- <the export's Latias.glb> <out.glb>
#
# From the references: a jet of a dragon floating level. A plump teardrop of a body, red below and behind, drawn out
# into a short tail ending in a flat fin of three points; a long thick neck rising forward from the chest to a smooth
# head shaped like a jet's nose, white, with a red band crossing over the top of the head through both eyes (the eye
# sits in the band: yellow, a black pupil, a glint); two white fins sweeping back and up from the back of the skull;
# the neck and the chest white down to the arms; a blue triangle, hollow, on the red lower belly; two short red arms
# folded forward with three white claws each; long swept-back jet wings, raised in a V, serrated near their tips.
# Latios (latios.py) is built by lati() with its own colours and measures.
# Bones: root 0; body (0, 0, 0.186); head (0, -0.101, 0.186); armR (0.06, -0.1, 0.146); wingR (0.06, 0, 0.246);
# tail (0, 0.18, 0.126).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, polygon, args, outward, hull, slab

LATIAS = dict(
    MAIN="#DD4553", WHITE="#F1F2F8", MARK="#3F95DA", IRIS="#F4C21E", IRIS_DEEP="#C78A10",
    GROW=1.0,
    # the white: above a level just over the arms and forward of a line rising back up the neck (body measure)
    WHITE_Z=0.0, WHITE_Y0=-0.075, WHITE_SLOPE=0.5,
    LATIOS_FACE=False,
    WING=[(0, -0.08), (0.14, 0.02), (0.24, 0.13), (0.285, 0.205), (0.248, 0.2), (0.252, 0.228), (0.205, 0.215),
          (0.15, 0.225), (0.0, 0.175)],
    WING_TURN_R=(-12, 0.05), WING_TURN_L=(-12, 0.05),      # (dihedral in degrees, the chord's rise behind)
    FIN_LEN=1.0, TAIL_FIN=[(0, -0.03), (0.055, 0.03), (0.075, 0.1), (0.032, 0.06), (0, 0.11), (-0.032, 0.06),
                           (-0.075, 0.1), (-0.055, 0.03)],
    FLANK_SPIKES=False, LIFT=0.0,
)

INKH = "#16121A"


def on_ell(c, r, d):
    d = unit(*d)
    s = 1 / math.sqrt(((d / np.array(r)) ** 2).sum())
    p = np.array(c) + d * s
    n = (p - c) / np.array(r) ** 2
    return p, n / np.linalg.norm(n)


def kite(root, tip, chord, normal, width, thick, tip_thick=0.4):
    """A solid fin: a flat-faced hull from a root edge (width along chord) to a tip, thick across normal."""
    root, tip = np.array(root, float), np.array(tip, float)
    c = unit(*chord); n = unit(*normal)
    pts = []
    for a in (-0.5, 0.5):
        for b in (-0.5, 0.5):
            pts.append(root + c * a * width + n * b * thick)
    mid = root + (tip - root) * 0.45
    for b in (-0.5, 0.5):
        pts.append(mid + c * 0.15 * width + n * b * thick * 0.8)
        pts.append(tip + n * b * thick * tip_thick)
    return hull(pts)


def lati(src, out, P):
    k = Kit(src, coat="fur")
    G = P["GROW"]
    B = np.array(k.arm.data.bones["body"].head_local) + np.array([0, 0, P["LIFT"]])
    MAIN, WHITE, MARK = P["MAIN"], P["WHITE"], P["MARK"]

    def at(*o):
        return B + G * np.array(o, float)

    # --- the body: a plump teardrop narrowing behind ----------------------------------------------------------------
    BODY_C, BODY_R = at(0, 0.03, -0.01), G * np.array([0.09, 0.15, 0.096])

    def teardrop(V):
        V = V.copy()
        t = np.clip(V[:, 1], 0, 1)
        s = 1 - 0.38 * t ** 1.3
        V[:, 0] *= s; V[:, 2] *= s
        V[:, 2] -= 0.1 * (1 - V[:, 1] ** 2) * (V[:, 2] < 0)        # a fuller belly
        return V

    k.add("body", ell(BODY_C, BODY_R, deform=teardrop, sub=5), MAIN, "body")
    k.add("chest", ell(at(0, -0.08, 0.015), G * np.array([0.075, 0.075, 0.08])), MAIN, "body")
    TAIL = [at(0, 0.11, -0.01), at(0, 0.2, -0.045), at(0, 0.29, -0.08)]
    k.add("tail", tube(TAIL, [G * 0.07, G * 0.042, G * 0.022]), MAIN, "tail")

    # --- the neck and the head --------------------------------------------------------------------------------------
    H, HR = at(0, -0.25, 0.168), G * np.array([0.074, 0.11, 0.074])

    def nose(V):
        V = V.copy()
        s = 1 - 0.28 * np.clip(-V[:, 1], 0, 1) ** 1.6
        V[:, 0] *= s; V[:, 2] *= s
        V[:, 2] += 0.12 * np.clip(-V[:, 1], 0, 1) ** 2 * 0      # level snout
        return V

    k.add("neck", tube([at(0, -0.09, 0.02), at(0, -0.16, 0.095), H + G * np.array([0, 0.055, -0.02])],
                       [G * 0.072, G * 0.057, G * 0.052]), WHITE, "head")
    k.add("head", ell(H, HR, deform=nose, sub=5), WHITE, "head")

    # the head's fins: two solid white fins sweeping back and up from the back of the skull
    fl = P["FIN_LEN"]
    k.add("earfin", kite(H + G * np.array([0.032, 0.045, 0.03]), H + G * np.array([0.06, 0.06 + 0.12 * fl, 0.06 * fl]),
                         (0, 1, -0.3), (1, 0, -0.3), G * 0.08, G * 0.032), WHITE, "head", fuse=False, mirror=True)

    # --- the arms: short, folded forward, three white claws ----------------------------------------------------------
    S, M, HD = at(0.064, -0.11, -0.02), at(0.078, -0.155, -0.03), at(0.072, -0.195, -0.036)
    k.add("arm", tube([S, M, HD], [G * 0.034, G * 0.03, G * 0.029]), MAIN, "arm{s}", mirror=True)
    k.add("hand", ell(HD + G * np.array([0, -0.008, -0.004]), G * np.array([0.032, 0.03, 0.026])), MAIN, "arm{s}",
          mirror=True)
    for i, dx in enumerate((-0.018, 0.0, 0.018)):
        base = HD + G * np.array([dx, -0.03, -0.012])
        tip = HD + G * np.array([dx * 1.2, -0.058, -0.026])
        k.add(f"claw{i}", kite(base, tip, (1, 0, 0), (0, 0.3, 1), G * 0.016, G * 0.012), WHITE, "arm{s}", fuse=False,
              mirror=True)

    # Latios's sharper flanks: two jagged fins low on each side of the hind body
    if P["FLANK_SPIKES"]:
        for i, (y, z, ln) in enumerate(((0.06, -0.06, 0.07), (0.12, -0.05, 0.06))):
            k.add(f"flank{i}", kite(at(0.07, y, z), at(0.115, y + ln * 0.8, z - 0.035),
                                    (0, 1, 0), (1, 0, 0.6), G * 0.05, G * 0.02), MAIN, "body", fuse=False, mirror=True)

    # --- the wings: broad swept-back jet blades, nearly level, the tips a little down ---------------------------------
    # A mirror pair (the field and the battle see the model from every side): tips a little down, the trailing edge
    # raised a little so the upper faces tilt toward the front
    W = [(a * G, b * G) for a, b in P["WING"]]
    for side, (deg, back) in ((1, P["WING_TURN_R"]), (-1, P["WING_TURN_L"])):
        dih = math.radians(deg)
        ex = np.array([side * math.cos(dih), 0.0, math.sin(dih)])
        k.add(f"wing{'R' if side > 0 else 'L'}", slab(W, at(0.05 * side, 0.0, 0.045), ex, unit(0, 1, back), G * 0.032),
              MAIN, f"wing{'R' if side > 0 else 'L'}", fuse=False)

    # --- the tail's fin: three points, banked 35 degrees so its face shows from behind --------------------------------
    td = unit(*(TAIL[-1] - TAIL[-2]))
    x0 = np.array([1.0, 0, 0]); x0 = x0 - td * (x0 @ td); x0 /= np.linalg.norm(x0)
    bank = math.radians(35)
    fx = math.cos(bank) * x0 + math.sin(bank) * np.cross(td, x0)
    TF = [(a * G, b * G) for a, b in P["TAIL_FIN"]]
    k.add("tailfin", slab(TF, TAIL[-1] - td * G * 0.01, fx, td, G * 0.022), MAIN, "tail", fuse=False)

    # --- colour: the white neck and chest, the arms red again ---------------------------------------------------------
    WZ, WY, WS = B[2] + G * P["WHITE_Z"], B[1] + G * P["WHITE_Y0"], P["WHITE_SLOPE"]
    k.paint(lambda Q: inside(WZ - Q[:, 2], 0.004) * inside(Q[:, 1] - (WY + WS * (Q[:, 2] - WZ)), 0.004), WHITE)
    k.paint(lambda Q: np.ones(len(Q)), MAIN, on=["arm", "hand"])
    k.paint(lambda Q: inside(np.linalg.norm((Q - H) / HR, axis=1) - 1.05, 0.004), WHITE)   # the head whole again

    # the eye's place
    E, EN = on_ell(H, HR, (0.78, -0.5, 0.2))
    E = E - EN * 0.004
    if not P["LATIOS_FACE"]:
        # Latias: a red band over the top of the head through both eyes
        d = unit(0, 0.3, 0.95); n = unit(0, -0.95, 0.3)
        Ec = E + n * G * 0.006
        k.paint(lambda Q: inside(np.abs((Q - Ec) @ n) - G * 0.0275, 0.002) * inside(-((Q - E) @ d) - G * 0.032, 0.0015)
                * inside(np.linalg.norm(Q - H, axis=1) - G * 0.13, 0.003), MAIN)
        lid = MAIN
    else:
        # Latios: a blue cap and nose, a white oval round each eye
        def cap(Q):
            # the front of the head blue as far back as just behind the eye, its edge sloping back up over the top
            front = inside(Q[:, 1] - (E[1] + G * 0.014 + 0.35 * (Q[:, 2] - E[2])), 0.002)
            oval = inside(((Q[:, 1] - E[1] + G * 0.006) / (G * 0.04)) ** 2 + ((Q[:, 2] - E[2]) / (G * 0.029)) ** 2 - 1,
                          0.06) * inside(G * 0.03 - np.abs(Q[:, 0]), 0.003)
            near = inside(np.linalg.norm(Q - H, axis=1) - G * 0.14, 0.003)
            return np.clip(front - oval, 0, 1) * near
        k.paint(cap, MAIN)
        lid = WHITE

    # --- the triangle on the lower belly: hollow, the mark's colour round the ground's ------------------------------
    TP, TN = on_ell(at(0, -0.08, 0.015), G * np.array([0.075, 0.075, 0.08]), (0, -0.9, -0.42))
    TP = TP + np.array([0, 0, 0.0])
    TRI = polygon([(-1, 0.75), (1, 0.75), (0, -1)])
    k.decal(TP, TN, (G * 0.036, G * 0.037), MARK, eps=0.004, outline=TRI, rings=10, segs=60, up=(0, 0, 1))
    inner = MAIN
    k.decal(TP + TN * 0.0, TN, (G * 0.018, G * 0.019), inner, eps=0.0065, outline=TRI, rings=8, segs=60, up=(0, 0, 1))

    # --- the mouth: a small open mouth under the nose's tip ----------------------------------------------------------
    MP, MN = on_ell(H, HR, (0, -0.86, -0.5))
    k.decal(MP, MN, (G * 0.018, G * 0.008), "#B8505E", eps=0.003, rings=10, segs=48,
            outline=polygon([(-1, 0.6), (1, 0.6), (0.45, -0.7), (-0.45, -0.7)]))

    k.build(voxel=0.0055, smooth=4, band=0.005)

    # --- the eyes: round, a bold iris, a black pupil, a glint ------------------------------------------------------
    LID, INK = srgb(lid), srgb(INKH)
    IRIS, DEEP, GLINT = srgb(P["IRIS"]), srgb(P["IRIS_DEEP"]), srgb("#FFFFFF")

    def eye(u, v):
        r = np.sqrt(u * u + v * v)
        c = np.tile(IRIS, (len(u), 1))
        c[v < -0.25 - 0.1 * u] = DEEP
        c[(u / 0.42) ** 2 + ((v + 0.02) / 0.55) ** 2 < 1] = INK
        c[((u - 0.2) / 0.2) ** 2 + ((v - 0.3) / 0.2) ** 2 < 1] = GLINT
        c[r > 0.83] = INK
        return c

    def eyes(state):
        k.decal(E, EN, (G * 0.0275, G * 0.0255), eye_look(state, eye, LID, INK, line=0.18), eps=0.005, up=(0, 0, 1),
                rings=20, segs=56, mirror=True)

    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    src, out = args()
    lati(src, out, LATIAS)
