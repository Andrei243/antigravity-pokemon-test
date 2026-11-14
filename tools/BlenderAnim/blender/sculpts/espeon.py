# Espeon sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/espeon.py -- <the export's Espeon.glb> <out.glb>
# Grown from eevee.py (its frame of legs, trunk, head, eyes, nose and mouth), refitted to Espeon's taller skeleton: a
# slender lilac cat on long slim legs, a smaller head with a fine muzzle, very large ears lined blue, a forked tuft
# of fur at each cheek hanging down and out, a red gem on the forehead, almond eyes (pale lavender with a purple iris
# under a heavy dark lid) and a long thin tail carried in a curve close behind, forked in a V at its tip.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, tri, unit, eye_look, polygon, args

src, out = args()
k = Kit(src, coat="fur")

# --- colours ----------------------------------------------------------------------------------------------------------
LILAC = "#E4B8DC"          # the coat: between the drawing's dusty lilac and the 3D model's pink
LINING = "#4C86BE"         # inside the ears: blue
GEM = "#D8243C"
EYE_W, IRIS, PUPIL, LID = "#EEE8F6", "#8E62B4", "#4A2C6E", "#43275E"
NOSE, INK = "#5C3858", "#5C3456"

# --- the frame (the skeleton's joints: body 0.26 up, head joint (0, -0.13, 0.41), ears (+-0.055, -0.13, 0.51),
#     forelegs (+-0.05, -0.08, 0.24), hind legs (+-0.05, 0.13, 0.24), tail (0, 0.16, 0.28)) ------------------------------
# Eevee's frame moved up 0.12 and forward 0.05 at the head, the legs longer, the trunk longer and a little narrower;
# slender, but not stick-legged: full chest and thighs over the tops of the legs, a short thick neck.
H = np.array([0, -0.15, 0.48])           # the head's centre, over and in front of its joint
HR = np.array([0.104, 0.096, 0.095])     # a little smaller than Eevee's (0.112, 0.099, 0.103)
LEG_X = 0.048
FORE_Y, HIND_Y = -0.082, 0.13
BODY_Z = 0.255


def head_shape(V):
    """Eevee's soft round head, a little narrower toward the chin so the face reads as a cat's."""
    V = V.copy()
    V[:, 0] *= 1 + 0.04 * np.clip(-V[:, 2], 0, 1) * (1 - np.clip(-V[:, 2], 0, 1)) - 0.08 * np.clip(-V[:, 2], 0, 1) ** 2
    return V


def add_head():
    k.add("head", ell(H, HR, deform=head_shape), LILAC, "head")
    # A fine muzzle standing a little forward under the eyes
    k.add("muzzle", ell(H + (0, -0.072, -0.036), (0.036, 0.032, 0.026)), LILAC, "head")
    # A short thick neck rising out of the chest into the head
    k.add("neck", tube([(0, -0.065, 0.27), (0, -0.092, 0.33), (0, -0.115, 0.38), (0, -0.135, 0.43)],
                       [0.076, 0.07, 0.064, 0.06]), LILAC, "head")


def add_body():
    # A slender but full trunk: a deep chest, a waist, a rounded rump
    k.add("chest", ell((0, -0.075, BODY_Z + 0.02), (0.074, 0.088, 0.1)), LILAC, "body")
    k.add("middle", ell((0, 0.03, BODY_Z), (0.066, 0.112, 0.082)), LILAC, "body")
    k.add("rump", ell((0, 0.115, BODY_Z - 0.004), (0.072, 0.076, 0.088)), LILAC, "body")


def add_legs(color=LILAC):
    # Eevee's legs, longer: a broad shoulder and a full thigh over the top half, the lower leg tapering to a clear paw
    k.add("shoulder", ell((LEG_X - 0.004, FORE_Y + 0.004, 0.2), (0.04, 0.05, 0.075)), color, "leg{s}", mirror=True)
    k.add("leg", tube([(LEG_X - 0.003, FORE_Y + 0.006, 0.22), (LEG_X, FORE_Y, 0.12), (LEG_X + 0.002, FORE_Y - 0.004, 0.03)],
                      [0.038, 0.031, 0.028]), color, "leg{s}", mirror=True)
    k.add("paw", ell((LEG_X + 0.002, FORE_Y - 0.017, 0.019), (0.03, 0.038, 0.021)), color, "leg{s}", mirror=True)
    k.add("thigh", ell((LEG_X - 0.003, HIND_Y + 0.006, 0.21), (0.046, 0.062, 0.085)), color, "hind{s}", mirror=True)
    k.add("hind", tube([(LEG_X, HIND_Y + 0.012, 0.18), (LEG_X + 0.002, HIND_Y + 0.01, 0.1),
                        (LEG_X + 0.002, HIND_Y - 0.002, 0.03)], [0.036, 0.03, 0.028]), color, "hind{s}", mirror=True)
    k.add("hpaw", ell((LEG_X + 0.002, HIND_Y - 0.015, 0.019), (0.03, 0.038, 0.021)), color, "hind{s}", mirror=True)


def along(P, pts, n=200):
    """Nearest place on a polyline (|x| taken, so a mirrored part's both sides): its share of the length and the offset."""
    pts = np.array(pts, float)
    seg = np.linalg.norm(np.diff(pts, axis=0), axis=1)
    cum = np.concatenate([[0], np.cumsum(seg)]); L = cum[-1]
    s = np.linspace(0, L, n)
    line = np.stack([np.interp(s, cum, pts[:, i]) for i in range(3)], axis=1)
    Q = P.copy(); Q[:, 0] = np.abs(Q[:, 0])
    j = np.concatenate([((Q[a:a + 4000, None, :] - line[None]) ** 2).sum(2).argmin(1) for a in range(0, len(Q), 4000)])
    return s[j] / L, Q - line[j]


# --- the ears: very large, wide at the root and pointed, leaning well out from the top corners of the head, lined
#     blue on their front faces with a lilac rim -------------------------------------------------------------------
EAR = [(0.048, -0.132, 0.525), (0.1, -0.127, 0.585), (0.155, -0.121, 0.645), (0.208, -0.115, 0.7)]
EAR_R = [0.054, 0.054, 0.036, 0.004]


def add_ears():
    k.add("ear", blade(EAR, EAR_R, (0, 1, 0), 0.3), LILAC, "ear{s}", mirror=True)
    d = unit(*(np.array(EAR[-1]) - np.array(EAR[0])))
    across = np.cross(d, (0, 1.0, 0)); across /= np.linalg.norm(across)

    def lining(P):
        t, off = along(P, EAR)
        half = 0.041 * np.sin(np.pi * np.clip((t + 0.12) / 1.0, 0, 1)) ** 0.7
        w = off @ across
        front = -off[:, 1]
        return (inside(np.abs(w) - half, 0.0025) * inside(-front + 0.002, 0.002) * inside(t - 0.9, 0.01)
                * (P[:, 2] > 0.52))
    k.paint(lining, LINING, on=["ear"])


# --- the cheek tufts: a flat lock of fur at each side of the face below the ears, hanging down and out, forked at
#     its end into two points (in place of Eevee's ruff) -------------------------------------------------------------
def add_cheeks():
    # About half a head long, clearly forked: the long point down and out, a shorter one out to the side
    base = H + (0.075, -0.035, -0.035)
    d1 = unit(0.75, -0.1, -1.0)
    mid = base + d1 * 0.045
    k.add("cheek", blade([base, mid, mid + d1 * 0.045], [0.03, 0.022, 0.004], (0.15, 1, 0), 0.4),
          LILAC, "head", mirror=True)
    d2 = unit(1.0, -0.1, -0.3)
    k.add("cheekfork", blade([mid - d1 * 0.006, mid + d2 * 0.04], [0.018, 0.004], (0.15, 1, 0), 0.45),
          LILAC, "head", mirror=True)


# --- the gem: a small red ball set in the middle of the forehead, a loose piece so its edge stays crisp ------------
def add_gem():
    d = unit(0, -0.76, 0.65)
    k.add("gem", ell(H + d * (HR[1] * 0.93), (0.015, 0.015, 0.015), sub=3), GEM, "head", fuse=False, surface="shell")


# --- the tail: long and thin, carried in an S close behind the body, swung out to the model's +X side, forked at its
#     tip in a clear V, the two prongs spreading across the swing so the V shows from the front three-quarter and from
#     behind -----------------------------------------------------------------------------------------------------------
TAIL = [(0.0, 0.16, 0.275), (0.035, 0.23, 0.29), (0.08, 0.285, 0.33), (0.11, 0.305, 0.39), (0.115, 0.295, 0.44),
        (0.11, 0.275, 0.465)]
FORK = TAIL[-1]


def add_tail():
    # Thick enough to read at the game's size; it bends back over itself near the top, like the drawing's S
    k.add("tail", tube(TAIL, [0.03, 0.027, 0.025, 0.024, 0.023, 0.022]), LILAC, "tail")
    f = np.array(FORK)
    k.add("tailforkA", tube([f, f + (-0.028, -0.022, 0.035), f + (-0.055, -0.035, 0.06)], [0.021, 0.019, 0.017]),
          LILAC, "tail")
    k.add("tailforkB", tube([f, f + (0.03, 0.006, 0.04), f + (0.062, 0.01, 0.07)], [0.021, 0.019, 0.017]),
          LILAC, "tail")


# --- the face: patches on the fused head, laid before the body's build (Eevee's, smaller and finer) -----------------
CB, CI = srgb(LILAC), srgb(INK)


def add_nose(at=H + (0, -0.103, -0.03), half=(0.009, 0.0065), color=NOSE):
    k.decal(at, unit(0, -1, 0.1), half, color, eps=0.003, outline=polygon([(-1, 0.7), (1, 0.7), (0, -1)]),
            rings=6, segs=24)


def mouth(u, v):
    c = np.tile(CB, (len(u), 1))
    c[(np.abs(v - (-0.2 + 1.4 * (np.abs(u) - 0.45) ** 2)) < 0.28) & (np.abs(u) < 0.85)] = CI
    return c


def add_mouth(at=H + (0, -0.097, -0.054), half=(0.014, 0.008), look=mouth):
    k.decal(at, unit(0, -1, -0.2), half, look, eps=0.003, rings=16, segs=72)


# The eyes: almond shaped, their outer corners raised, pale lavender with a purple iris toward the nose and a darker
# middle (no glint: the white reads as one), under a heavy dark-purple upper lid. The outline is not symmetric, so each eye is a patch of
# its own with u outward (the left one's colour function and outline flipped).
EYE_DIR = unit(0.46, -0.88, 0.02)
EYE_HALF = (0.034, 0.026)
OUTLINE = [(-1, 0.1), (-0.6, 0.62), (0, 0.8), (0.6, 0.8), (1, 0.5), (0.8, -0.3), (0.2, -0.85), (-0.6, -0.62)]
CW, CIR, CPU, CL = srgb(EYE_W), srgb(IRIS), srgb(PUPIL), srgb(LID)
_RIM = polygon(OUTLINE)


def eye(u, v):
    # Three flat tones under the lid: the pale white, a purple iris toward the nose and its darker middle
    c = np.tile(CW, (len(u), 1))
    c[((u + 0.2) / 0.56) ** 2 + ((v + 0.08) / 0.72) ** 2 < 1] = CIR
    c[((u + 0.22) / 0.3) ** 2 + ((v + 0.08) / 0.44) ** 2 < 1] = CPU
    # The heavy upper lid: the outer rings of the patch along its top edge, so its line is the outline's own (a band
    # cut across the rings came out in steps); arched, from just over the inner corner to past the outer one
    rr = np.hypot(u, v) / np.array([_RIM(t) for t in np.arctan2(v, u)])
    c[(rr > 0.725) & (v > 0.12 + 0.2 * u)] = CL
    return c


E = H + EYE_DIR * HR[0] * 0.9 + np.array([0, 0, 0.004])


def eyes(state):
    look = eye_look(state, eye, CB, CL)
    k.decal(E, EYE_DIR, EYE_HALF, look, eps=0.004, outline=polygon(OUTLINE), rings=20, segs=56)
    m = np.array([-1, 1, 1.0])
    k.decal(E * m, EYE_DIR * m, EYE_HALF, lambda u, v: look(-u, v), eps=0.004,
            outline=polygon([(-x, y) for x, y in OUTLINE]), rings=20, segs=56)


add_body()
add_legs()
add_head()
add_ears()
add_cheeks()
add_gem()
add_tail()
add_nose()
add_mouth()

k.build(voxel=0.0055, band=0.006)
k.eye_states(eyes)
k.export(out)
