# Tornadus (Incarnate Forme) sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/tornadus.py -- <the export's Tornadus.glb> <out.glb>
#
# The three forces of nature share one body, made by genie() here; Thundurus and Landorus import it (sibling) and give
# their own colours, head pieces and tail. From the references: a muscular genie's torso, arms crossed high over the
# chest, rising out of a lumpy cloud that trails a twisting wisp below; a broad squarish head sunk between the
# shoulders; small fierce yellow-orange eyes under a scowling brow; a great white moustache like a star across the
# lower face, its points out past the cheeks, up beside the eyes and down at the chin, an open dark mouth with fangs in
# it; round spots on the shoulders, arms and body.
# Tornadus: green, purple spots, a purple horn each side of the head, a cloud of white hair swirling up behind it; its
# long thick purple tail loops round the body (up behind the head, out wide on its -X side, forward along the cloud's
# front) and ends in a curl, three yellow-orange swirls on its outer side.
# The skeleton: root, body (0, 0, 0.477), armR/L (+-0.11, 0, 0.537), tail (0, 0.05, 0.337), head (0, -0.02, 0.577).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, hull, axes, srgb, inside, smoothstep, unit, boxy, eye_look, polygon, args)

# --- the frame every force shares -----------------------------------------------------------------------------------
HEAD = np.array([0, -0.035, 0.685])          # the head's centre
HEAD_R = (0.092, 0.082, 0.084)
CLOUD = np.array([0, 0.0, 0.265])            # the cloud's middle
EYE = np.array([0.042, -0.108, 0.698])       # the right eye's centre, on the face
EYE_N = unit(0.38, -0.92, 0.08)
VOXEL = 0.0068
CLOUD_GROW = 1.14                             # the cloud a little wider than the body, as the pictures have it

CLOUD_WHITE, CLOUD_SHADE = "#F2F4F8", "#BEC3D2"
WHITE = "#F6F6F4"
MOUTH, FANG = "#3A1C2A", "#FAFAF6"
INK, PUPIL = "#1E1418", "#141012"


def spike(base, tip, r, n=6, flat=1.0, turn=0.0):
    """A solid point: a ring of n corners round base (squashed across by flat) and the tip; a flat-faced hull."""
    base, tip = np.array(base, float), np.array(tip, float)
    a, b = axes(tip - base)
    pts = [base + r * (math.cos(t + turn) * a + flat * math.sin(t + turn) * b)
           for t in np.linspace(0, 2 * math.pi, n, endpoint=False)]
    return hull(pts + [tip])


def blob_mask(points):
    """Round spots: (centre, radius) each, painted as discs where the surface passes through the ball."""
    def m(P):
        out = np.zeros(len(P))
        for c, r in points:
            out = np.maximum(out, inside(np.linalg.norm(P - np.array(c), axis=1) - r, 0.0035))
        return out
    return m


# --- the cloud ------------------------------------------------------------------------------------------------------
PUFFS = [(0, 0, 0.01, 0.105), (-0.15, 0.0, 0.0, 0.085), (0.15, 0.01, 0.0, 0.085), (0, -0.09, -0.005, 0.09),
         (0, 0.09, 0.0, 0.085), (-0.09, -0.075, 0.045, 0.078), (0.09, -0.07, 0.045, 0.078),
         (-0.085, 0.075, 0.05, 0.075), (0.09, 0.07, 0.045, 0.075), (0, 0.0, 0.075, 0.095),
         (-0.08, -0.05, -0.06, 0.07), (0.075, -0.04, -0.06, 0.072), (0.0, 0.06, -0.06, 0.07),
         (-0.205, -0.03, 0.03, 0.058), (0.2, 0.025, 0.035, 0.06), (0.05, -0.13, -0.03, 0.058),
         (-0.06, 0.125, 0.005, 0.058), (-0.15, 0.08, 0.06, 0.055), (0.15, -0.08, 0.06, 0.055)]


def cloud(k):
    """A lumpy mass of fused puffs, grey underneath, a twisting wisp trailing down to the ground."""
    for i, (dx, dy, dz, r) in enumerate(PUFFS):
        g = CLOUD_GROW
        k.add(f"cloud{i}_", ell(CLOUD + np.array([dx * g, dy * 1.05, dz]), (r * g, r * 0.95, r * 0.85), sub=4),
              CLOUD_WHITE, "root")
    wisp = [CLOUD + [0.02, 0.01, -0.07], CLOUD + [0.055, -0.005, -0.14], CLOUD + [0.0, -0.02, -0.19],
            CLOUD + [0.045, 0.0, -0.225], (0.085, 0.01, 0.03)]
    k.add("cloudwisp", tube(wisp, [0.055, 0.038, 0.024, 0.017, 0.014]), CLOUD_WHITE, "root")
    k.paint(lambda P: smoothstep(CLOUD[2] + 0.01, CLOUD[2] - 0.08, P[:, 2]) * 0.85, CLOUD_SHADE, on=["cloud"])


# --- the torso and the crossed arms -----------------------------------------------------------------------------------
def torso(k, F):
    skin = F["skin"]
    k.add("abdomen", ell((0, 0.01, 0.415), (0.105, 0.082, 0.11)), skin, "body")
    k.add("chest", ell((0, 0.0, 0.53), (0.152, 0.105, 0.1)), skin, "body")
    k.add("pec", ell((0.066, -0.062, 0.555), (0.08, 0.058, 0.058)), skin, "body", mirror=True)
    k.add("lat", ell((0.12, 0.02, 0.49), (0.062, 0.08, 0.085)), skin, "body", mirror=True)
    k.add("trap", ell((0, 0.03, 0.6), (0.135, 0.08, 0.06)), skin, "body")
    k.add("delt", ell((0.155, 0.0, 0.59), (0.078, 0.075, 0.072)), skin, "arm{s}", mirror=True)
    # Each forearm rises across the chest to the other side, the right one in front: an X of thick arms just under
    # the chin, the fists tucked against the other arm's biceps
    for s, side, front, z0, z1 in ((1, "R", -0.172, 0.468, 0.552), (-1, "L", -0.146, 0.486, 0.536)):
        S = np.array([0.165 * s, -0.01, 0.575])
        E = np.array([0.18 * s, -0.11, 0.47 if s > 0 else 0.488])
        H = np.array([-0.1 * s, front + 0.02, z1])
        M = np.array([0.03 * s, front, (z0 + z1) / 2])
        k.add(f"upper{side}", tube([S, (S + E) / 2 + [0.012 * s, 0, 0], E], [0.058, 0.06, 0.05]), skin, f"arm{side}")
        k.add(f"biceps{side}", ell(S + (E - S) * 0.5 + [0.008 * s, -0.012, 0], (0.05, 0.052, 0.058)), skin,
              f"arm{side}")
        k.add(f"fore{side}", tube([E, M, H], [0.05, 0.052, 0.045]), skin, f"arm{side}")
        k.add(f"fist{side}", ell(H + [-0.012 * s, 0.006, 0.0], (0.046, 0.043, 0.047), deform=boxy(2.4)), skin,
              f"arm{side}")
    k.paint(blob_mask(F["spots"]), F["spot"], on=["abdomen", "chest", "pec", "lat", "trap", "delt", "upper", "biceps",
                                                    "fore", "fist"])


# --- the head -------------------------------------------------------------------------------------------------------
def head(k, F):
    skin = F["skin"]
    k.add("neck", ell((0, 0.0, 0.625), (0.075, 0.065, 0.05)), skin, "head")
    k.add("head", ell(HEAD, HEAD_R, deform=boxy(2.3)), skin, "head")
    k.add("cheek", ell((0.055, -0.07, 0.66), (0.048, 0.045, 0.042)), skin, "head", mirror=True)
    # the scowling brow: a ridge from low at the nose to high at the temple
    k.add("brow", tube([(0.008, -0.112, 0.716), (0.04, -0.106, 0.73), (0.078, -0.088, 0.744)], [0.013, 0.016, 0.014]),
          skin, "head", mirror=True)
    # The moustache: a white mass across the lower face, a point rising between the eyes, and solid points out past
    # the cheeks, up beside the eyes and down under the chin
    W = F.get("white", WHITE)
    k.add("tache", ell((0, -0.098, 0.652), (0.078, 0.042, 0.036)), W, "head")
    k.add("tachebridge", ell((0, -0.118, 0.69), (0.016, 0.012, 0.03)), W, "head")
    for s, side in ((1, "R"), (-1, "L")):
        for j, (b, t, r) in enumerate([((0.06, -0.1, 0.656), (0.155, -0.075, 0.666), 0.022),
                                      ((0.07, -0.09, 0.672), (0.122, -0.07, 0.738), 0.018),
                                      ((0.035, -0.115, 0.632), (0.068, -0.105, 0.585), 0.018)]):
            k.add(f"tachept{side}{j}", spike(np.array(b) * [s, 1, 1], np.array(t) * [s, 1, 1], r, n=5, flat=0.75),
                  W, "head", fuse=False, surface="fur")
    k.add("tachechin", spike((0, -0.118, 0.628), (0, -0.112, 0.592), 0.02, n=5, flat=0.75), W, "head", fuse=False,
          surface="fur")
    # The open mouth on the moustache's front: dark, two fangs down from its top and two up from its bottom
    CM, CF = srgb(MOUTH), srgb(FANG)
    def mouth(u, v):
        c = np.tile(CM, (len(u), 1))
        for x in (-0.45, 0.45):
            c[(v > 0.05) & (np.abs(u - x) < 0.24 * (v - 0.05) / 0.95 + 0.02)] = CF
            c[(v < -0.45) & (np.abs(u - x * 0.8) < 0.16 * (-0.45 - v) / 0.55 + 0.01)] = CF
        return c
    k.decal((0, -0.138, 0.648), unit(0, -1, -0.12), (0.038, 0.024), mouth, eps=0.004, rings=16, segs=48,
            outline=polygon([(-1, 1), (1, 1), (0.78, -0.6), (0.3, -1), (-0.3, -1), (-0.78, -0.6)]))


def eyes_for(k, F):
    """Small fierce eyes under the brow: yellow, a round black pupil, a dark rim; the brow's dark cut over them."""
    LID, BROW = srgb(F["skin"]), srgb(F["brow"])
    IRIS, CINK, CP = srgb(F["eye"]), srgb(INK), srgb(PUPIL)
    def top(u):
        return 0.12 + 0.55 * u                      # low at the nose, high at the temple
    def open_eye(u, v):
        r = (u / 0.98) ** 2 + (v / 0.9) ** 2
        c = np.tile(LID, (len(u), 1))
        c[r < 1] = CINK
        c[r < 0.62] = IRIS
        c[((u + 0.06) / 0.34) ** 2 + ((v + 0.06) / 0.42) ** 2 < 1] = CP
        c[((u + 0.2) / 0.12) ** 2 + ((v - 0.16) / 0.12) ** 2 < 1] = (1, 1, 1)
        return c
    def lay(state):
        base = eye_look(state, open_eye, LID, CINK, line=0.16)
        def look(u, v):
            c = base(u, v)
            t = top(u)
            c[v > t] = BROW
            c[np.abs(v - t) < 0.12] = CINK
            return c
        k.decal(EYE, EYE_N, (0.03, 0.024), look, eps=0.005, rings=20, segs=56, mirror=True)
    return lay


def genie(src, out, F, extras):
    """One of the forces: F its colours and spots, extras(k, F) its horns, hair and tail."""
    k = Kit(src, coat="fur")
    cloud(k)
    torso(k, F)
    head(k, F)
    extras(k, F)
    k.build(voxel=VOXEL, band=0.006)
    k.eye_states(eyes_for(k, F))
    k.export(out)


# --- Tornadus's own ---------------------------------------------------------------------------------------------------
PURPLE, SWIRL, SWIRLLINE = "#8C66B8", "#F4B24A", "#C77A22"

TORNADUS = dict(
    skin="#5E9A40", brow="#3E6E2A", eye="#F4A43A", spot="#8C66B8",
    spots=[((0.17, -0.02, 0.655), 0.04), ((-0.17, -0.02, 0.655), 0.036), ((0.235, -0.07, 0.53), 0.03),
           ((-0.235, -0.06, 0.54), 0.028), ((0.06, -0.22, 0.51), 0.026), ((-0.06, -0.198, 0.545), 0.022),
           ((0.07, -0.08, 0.385), 0.028), ((-0.05, -0.075, 0.36), 0.022), ((0.1, 0.09, 0.53), 0.035),
           ((-0.1, 0.09, 0.5), 0.03), ((0.0, 0.1, 0.42), 0.03)])

# The tail's line: up out of the cloud behind the +X side, over behind the head, out wide on the -X side, down and
# forward along the cloud's front, ending in a curl at its front
TAIL = [(0.17, 0.08, 0.36), (0.2, 0.11, 0.5), (0.15, 0.15, 0.62), (0.03, 0.17, 0.67), (-0.1, 0.15, 0.665),
        (-0.22, 0.09, 0.62), (-0.3, 0.02, 0.5), (-0.29, -0.04, 0.37), (-0.2, -0.13, 0.27), (-0.06, -0.19, 0.225),
        (0.06, -0.2, 0.215), (0.13, -0.205, 0.245), (0.135, -0.21, 0.295), (0.09, -0.212, 0.31),
        (0.06, -0.21, 0.28)]
TAIL_R = [0.032, 0.036, 0.038, 0.038, 0.038, 0.038, 0.037, 0.035, 0.033, 0.031, 0.029, 0.026, 0.023, 0.02, 0.016]


def swirl(u, v):
    """A yellow-orange scroll: a disc with a spiral line of a turn and a half wound into it."""
    rr = np.sqrt(u * u + v * v); th = np.mod(np.arctan2(v, u), 2 * math.pi)
    a = 0.82 / (3 * math.pi)
    c = np.tile(srgb(SWIRL), (len(u), 1))
    best = np.full(len(u), 9.0)
    for kk in range(2):
        t = th + 2 * math.pi * kk
        best = np.minimum(best, np.where(t <= 3 * math.pi, np.abs(rr - (0.12 + a * t)), 9.0))
    c[best < 0.075] = srgb(SWIRLLINE)
    c[rr > 0.9] = srgb(SWIRLLINE)
    return c


def tornadus_extras(k, F):
    # The horns: a solid purple point each side of the crown, up and out
    for s, side in ((1, "R"), (-1, "L")):
        k.add(f"horn{side}", spike((0.06 * s, -0.035, 0.745), (0.12 * s, -0.03, 0.83), 0.024, n=6), PURPLE, "head",
              fuse=False, surface="shell")
    # The hair: a cloud of white puffs swirling up behind the head, fuller on the +X side
    for i, (x, y, z, r) in enumerate([(0.0, 0.04, 0.755, 0.06), (0.06, 0.05, 0.75, 0.055), (-0.055, 0.05, 0.745, 0.05),
                                      (0.1, 0.06, 0.7, 0.048), (-0.09, 0.06, 0.7, 0.044), (0.0, 0.08, 0.705, 0.06),
                                      (0.04, 0.03, 0.79, 0.04), (0.09, 0.03, 0.775, 0.036)]):
        k.add(f"hair{i}_", ell((x, y, z), (r, r * 0.9, r)), WHITE, "head")
    k.add("tail", tube(TAIL, TAIL_R, per=10), PURPLE, "tail")
    # three swirls on the outer side of the wide arc
    P = np.array(TAIL); mid = np.array([-0.08, 0.0, 0.45])
    for i, p, h in ((5, P[5], 0.031), (6, P[6], 0.031), (7, (P[6] + P[7]) / 2, 0.024)):
        o = p - mid; o[1] = 0; o /= np.linalg.norm(o)
        o = unit(*(o + np.array([0, -0.5, 0])))           # turned a little to the front, where both cameras see it
        k.decal(p + o * TAIL_R[i] * 0.97, o, (h, h), swirl, eps=0.004, rings=16, segs=48)


if __name__ == "__main__":
    src, out = args()
    genie(src, out, TORNADUS, tornadus_extras)
