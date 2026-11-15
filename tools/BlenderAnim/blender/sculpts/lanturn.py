# Lanturn sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown from
# Chinchou's script (its blue, its yellow, its glowing lights):
#   blender --background --factory-startup --python sculpts/lanturn.py -- <the export's Lanturn.glb> <out.glb>
#
# The design, from the references: a big plump blue fish, its head round and blunt, its body narrowing into a tail
# fin whose upper lobe is blue and whose lower lobe is yellow with a ragged edge. A yellow band runs down each side of
# the head from the crown over the eye to the top of the mouth, and across the front of the face above it; the eye
# sits in it, a pointed white eye with a large red iris and a black pupil. The mouth is open: dark red rim, pink
# inside. A long pale blue fin on each side swept back, a small pale fin under the front. From the top of the head a
# feeler rises and curves forward to a large glowing yellow orb; a short branch of it curls up behind to a small one.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, outward, srgb, inside, unit, eye_look, polygon, sibling, args

ch = sibling("chinchou")
BLUE, YELLOW, LIGHT, INK = ch.BLUE, ch.YELLOW, ch.LIGHT, ch.INK
FIN = "#B4CCEE"
RED, PUPIL, WHITE, MOUTH_RIM, MOUTH_IN, TONGUE = "#D8402E", "#1A1414", "#F8F8F4", "#7A2430", "#B84858", "#E8909A"

HEAD = np.array([0.0, -0.1, 0.21])
HEAD_R = (0.13, 0.125, 0.14)
# The yellow band: a stroke in (y, z) from the crown down and forward to the top of the mouth, wrapping the head
BAND_A, BAND_B, BAND_W = np.array([-0.06, 0.355]), np.array([-0.235, 0.195]), 0.042


def band(P):
    """How far a point lies outside the yellow band, measured in (y, z) from the stroke's line."""
    q = P[:, 1:3]
    ab = BAND_B - BAND_A
    t = np.clip(((q - BAND_A) @ ab) / (ab @ ab), 0, 1)
    d = np.linalg.norm(q - (BAND_A + t[:, None] * ab), axis=1)
    return d - BAND_W


def body(k):
    k.add("body", ell((0, 0.02, 0.2), (0.13, 0.19, 0.14)), BLUE, "body")
    k.add("head", ell(HEAD, HEAD_R), BLUE, "body")
    k.add("belly", ell((0, -0.03, 0.15), (0.115, 0.15, 0.1)), BLUE, "body")
    k.add("stalk", tube([(0, 0.12, 0.2), (0, 0.2, 0.208), (0, 0.255, 0.212)], [0.085, 0.05, 0.032]), BLUE, "tail")
    k.paint(lambda P: inside(band(P), 0.0025) * inside(P[:, 1] + 0.02, 0.003), YELLOW)

    # The long pale side fins, swept back and out, their faces to the front and side
    side = [(0.0, -0.018), (0.06, -0.026), (0.13, -0.014), (0.165, 0.004), (0.13, 0.024), (0.06, 0.028), (0.0, 0.016)]
    k.add("fin", slab(side, (0.11, -0.04, 0.2), unit(0.72, 0.66, 0.2), (0, 0, 1), 0.013), FIN, "fin{s}", fuse=False,
          mirror=True, surface="scales")
    # A small pale fin under the front, a pair, splayed
    low = [(0.0, -0.014), (0.04, -0.018), (0.075, -0.006), (0.07, 0.012), (0.035, 0.018), (0.0, 0.012)]
    k.add("pelvic", slab(low, (0.04, -0.09, 0.08), unit(0.35, 0.45, -0.82), unit(0.9, -0.3, 0.3), 0.011), FIN,
          "body", fuse=False, mirror=True, surface="scales")

    # The tail fin, run back and out to the +X side so its face shows from the front and from behind
    T0 = np.array([0.0, 0.245, 0.212])
    tilt = math.radians(40)
    EX, EZ = unit(0.6, 0.8, 0.0), (0.25 * math.sin(tilt), 0.0, math.cos(tilt))
    upper = [(0.0, 0.03), (0.05, 0.07), (0.095, 0.108), (0.122, 0.125), (0.122, 0.09), (0.102, 0.03),
             (0.088, -0.004), (0.05, -0.02), (0.0, -0.02)]
    lower = [(0.0, -0.008), (0.05, -0.014), (0.086, -0.004), (0.102, -0.03), (0.116, -0.074), (0.128, -0.118),
             (0.104, -0.1), (0.092, -0.126), (0.072, -0.1), (0.052, -0.106), (0.03, -0.06), (0.0, -0.03)]
    k.add("tailfin", slab(upper, T0, EX, EZ, 0.014), BLUE, "tail", fuse=False, surface="scales")
    k.add("tailfinlow", slab(lower, T0 + np.array([0, 0.002, 0]), EX, EZ, 0.016), YELLOW, "tail", fuse=False,
          surface="scales")

    # The feeler and its two lights: a long stalk forward to the great orb, a short curl up behind to the small one
    stalk = [(0, -0.09, 0.32), (0, -0.085, 0.39), (0, -0.115, 0.44), (0, -0.18, 0.46), (0, -0.245, 0.44),
             (0, -0.275, 0.41)]
    ch.feeler(k, "feeler", stalk, 0.012, "lure", colour=FIN, tip=None)
    curl = [(0, -0.085, 0.395), (0, -0.065, 0.435), (0, -0.04, 0.46), (0, -0.025, 0.465)]
    ch.feeler(k, "curl", curl, 0.011, "lure", colour=FIN, tip=None)
    ch.orb(k, "orb", (0, -0.292, 0.385), 0.054, "lure")
    ch.orb(k, "orbsmall", (0, -0.008, 0.47), 0.03, "lure")

    # The open mouth: a patch across the front under the band
    def mouth(u, v):
        c = np.tile(srgb(MOUTH_IN), (len(u), 1))
        c[((u / 0.6) ** 2 + ((v + 0.55) / 0.42) ** 2) < 1] = srgb(TONGUE)
        c[OPEN(u, v) > 0.84] = srgb(MOUTH_RIM)
        return c
    k.decal((0, -0.2, 0.15), unit(0, -1, -0.45), (0.058, 0.04), mouth, eps=0.004, outline=polygon(MOUTH_SHAPE),
            rings=14, segs=72)


MOUTH_SHAPE = [(-1.0, 0.75), (-0.5, 0.95), (0.0, 1.0), (0.5, 0.95), (1.0, 0.75), (0.85, 0.05), (0.55, -0.6),
               (0.0, -0.9), (-0.55, -0.6), (-0.85, 0.05)]


def radial(pts):
    poly = polygon(pts)
    def frac(u, v):
        th = np.arctan2(v, u)
        R = np.array([poly(t) for t in th])
        return np.sqrt(u * u + v * v) / np.maximum(R, 1e-6)
    return frac


OPEN = radial(MOUTH_SHAPE)

# --- the eyes: pointed white eyes in the band, a large red iris toward the front, a black pupil ---------------------
EYE = [(math.cos(t) * (1 - 0.12 * abs(math.sin(t))), 0.88 * math.sin(t)) for t in np.linspace(0, 2 * math.pi, 28, endpoint=False)]
EYE_FRAC = radial(EYE)
EYE_C = np.array([0.1, -0.135, 0.284])
EYE_N = unit(0.76, -0.64, 0.12)
EYE_HALF = (0.044, 0.04)
CW, CR, CK, CY = srgb(WHITE), srgb(RED), srgb(INK), srgb(YELLOW)


def eye_open(u, v):
    """+u runs toward the back on a side's patch: the iris stands toward the front (-u)."""
    c = np.tile(CW, (len(u), 1))
    c[((u + 0.12) / 0.62) ** 2 + (v / 0.72) ** 2 < 1] = CR
    c[EYE_FRAC(u, v) > 0.84] = CK
    return c


def eyes(k, state):
    k.decal(EYE_C, EYE_N, EYE_HALF, eye_look(state, eye_open, CY, CK, line=0.15), eps=0.004, outline=polygon(EYE),
            rings=20, segs=56, mirror=True)
    if state in ("Open", "Fierce"):
        pupil = EYE_C + np.array([0.0, -0.006, -0.004])
        k.decal(pupil, EYE_N, (0.011, 0.017), srgb(PUPIL), eps=0.0055, rings=8, segs=32, mirror=True)
        glint = EYE_C + np.array([0.0, -0.012, 0.01])
        k.decal(glint, EYE_N, (0.005, 0.005), CW, eps=0.007, rings=4, segs=20, mirror=True)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="scales")
    body(k)
    k.build(voxel=0.006, smooth=4, band=0.006)
    k.eye_states(lambda state: eyes(k, state))
    k.export(out)
