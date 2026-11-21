# Froakie sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/froakie.py -- <the export's Froakie.glb> <out.glb>
#
# From the references: a small light-blue frog crouched on folded legs, its head far wider than its body, flat and
# wedge-like, with two great eyes bulging from its top: yellow in the blue bulge with a tall black pupil toward the
# middle. A dark blue patch over the top of the head between and behind the eyes. Two white bubbles on the snout and a
# small frowning mouth line. A collar of white foam bubbles (frubbles) round the neck, heaped over the shoulders and
# back and running down the chest in a short chain. Thin arms, white three-fingered hands resting on the ground in
# front; the thighs folded high beside the body, blue feet with three toes.
#
# Its evolutions grow from this frame: the colours, bubble(), bubble_ring(), digits() and the way the eyes are laid.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, args

BLUE, DARK, WHITE, GREY, YELLOW, INK = ("#6CC0F0", "#2F66BC", "#EEECE4", "#CDCAC2", "#F4E27A", "#1E2028")


def bubble(k, name, c, r, col, bone):
    """A loose foam bubble: a round ball that keeps its outline."""
    k.add(name, ell(c, (r, r, r * 0.94), sub=3), col, bone, fuse=False)


def bubble_ring(k, name, centre, radius, angles, sizes, bone, zs=None, cols=None):
    """Bubbles round an upright axis through centre, at angles (degrees, 0 = straight back, + toward +X)."""
    for i, (a, s) in enumerate(zip(angles, sizes)):
        t = math.radians(a)
        z = centre[2] + (zs[i] if zs is not None else 0.0)
        p = (centre[0] + radius * math.sin(t), centre[1] + radius * math.cos(t), z)
        col = cols[i] if cols is not None else (WHITE if i % 3 else GREY)
        bubble(k, f"{name}{i}", p, s, col, bone)


def digits(k, name, base, d, spread, length, r, col, bone, tip=None, mirror=True, n=3, tip_r=None):
    """Three fingers or toes from base along d, fanned across spread (a direction), with round tips."""
    d = np.array(d, float); d /= np.linalg.norm(d)
    sp = np.array(spread, float)
    for i, f in enumerate(np.linspace(-1, 1, n)):
        a = np.array(base, float) + sp * f * 0.5
        e = a + (d + sp * f * 0.6) * length
        k.add(f"{name}{i}", tube([a, (a + e) / 2, e], [r, r * 0.95, r * 0.9]), col, bone, mirror=mirror)
        if tip is not None:
            k.add(f"{name}tip{i}", ell(e + d * r * 0.2, (tip_r or r * 1.45,) * 3), tip, bone, mirror=mirror)


def froakie(src, out):
    k = Kit(src, coat="scales")
    V = 0.0028

    # --- the body: a small pear sat low between the folded legs ------------------------------------------------------
    k.add("body", ell((0, 0.006, 0.078), (0.046, 0.044, 0.05)), BLUE, "body")
    k.add("hips", ell((0, 0.014, 0.045), (0.044, 0.04, 0.032)), BLUE, "body")

    # --- the head: wide, flat and wedge-like, the snout low in front ---------------------------------------------
    k.add("head", ell((0, -0.012, 0.152), (0.091, 0.064, 0.041)), BLUE, "head")
    k.add("jaw", ell((0, -0.03, 0.138), (0.08, 0.047, 0.03)), BLUE, "head")
    k.add("crown", ell((0, 0.006, 0.172), (0.05, 0.045, 0.032)), BLUE, "head")
    # the great eyes bulging up from the head's top
    k.add("bulge", ell((0.043, -0.018, 0.2), (0.04, 0.036, 0.045)), BLUE, "head", mirror=True)
    # the dark patch over the top of the head between and behind the eyes
    k.paint(lambda P: inside(np.sqrt((P[:, 0] / 0.03) ** 2 + ((P[:, 1] - 0.012) / 0.05) ** 2
                                     + ((P[:, 2] - 0.19) / 0.04) ** 2) - 1, 0.003)
            * inside(np.abs(P[:, 0]) - 0.03, 0.002), DARK)

    # the two bubbles on the snout
    for s in (1, -1):
        bubble(k, f"nose{'R' if s > 0 else 'L'}", (0.012 * s, -0.07, 0.158), 0.0115, WHITE, "head")
    # a small frowning mouth on the front of the snout
    CBL, CI = srgb(BLUE), srgb(INK)
    def frown(u, v):
        c = np.tile(CBL, (len(u), 1))
        c[(np.abs(v - (0.25 - 0.75 * u * u)) < 0.32) & (np.abs(u) < 0.92)] = CI
        return c
    k.decal((0, -0.071, 0.131), unit(0, -1, -0.15), (0.018, 0.007), frown, eps=0.0012, rings=12, segs=60)

    # --- the frubbles: a collar of foam round the neck, heaped on the shoulders, a chain down the chest -------------
    bubble_ring(k, "collar", (0, 0.014, 0.116), 0.054, [-128, -100, -68, -38, -8, 22, 50, 78, 104, 130],
                [0.021, 0.03, 0.033, 0.029, 0.034, 0.03, 0.035, 0.031, 0.029, 0.021], "body",
                zs=[-0.006, 0.004, 0.0, 0.012, 0.004, 0.014, 0.002, 0.008, 0.002, -0.006],
                cols=[WHITE, WHITE, GREY, WHITE, WHITE, GREY, WHITE, WHITE, GREY, WHITE])
    # heaped higher over the shoulders and the upper back, irregular
    bubble_ring(k, "heap", (0, 0.024, 0.13), 0.058, [-92, -60, -18, 34, 70, 98], [0.026, 0.028, 0.024, 0.027,
                0.03, 0.025], "body", zs=[0.016, 0.026, 0.024, 0.026, 0.02, 0.014],
                cols=[WHITE, WHITE, GREY, WHITE, WHITE, WHITE])
    for i, (p, r) in enumerate([((0.0, -0.046, 0.112), 0.016), ((0.004, -0.05, 0.088), 0.012),
                                ((-0.004, -0.05, 0.068), 0.009)]):
        bubble(k, f"chest{i}", p, r, WHITE if i != 1 else GREY, "body")

    # --- arms: thin, carried forward and down, white three-fingered hands resting in front ------------------------
    k.add("arm", tube([(0.042, -0.02, 0.1), (0.052, -0.046, 0.08), (0.04, -0.066, 0.042)], [0.0105, 0.0095, 0.009]),
          BLUE, "arm{s}", mirror=True)
    k.add("hand", ell((0.038, -0.072, 0.028), (0.013, 0.012, 0.012)), WHITE, "arm{s}", mirror=True)
    digits(k, "finger", (0.038, -0.076, 0.016), (0.0, -0.5, -1.0), (0.018, 0, 0), 0.012, 0.005, WHITE, "arm{s}",
           tip=WHITE)

    # --- legs: thighs folded high beside the body, short shins, flat blue feet with three toes ---------------------
    k.add("thigh", tube([(0.03, 0.012, 0.05), (0.062, -0.01, 0.058), (0.078, -0.03, 0.052)], [0.026, 0.024, 0.019]),
          BLUE, "leg{s}", mirror=True)
    k.add("shin", tube([(0.078, -0.03, 0.052), (0.078, -0.01, 0.03), (0.07, 0.012, 0.016)], [0.018, 0.015, 0.013]),
          BLUE, "leg{s}", mirror=True)
    k.add("foot", ell((0.072, -0.016, 0.009), (0.02, 0.03, 0.009)), BLUE, "leg{s}", mirror=True)
    digits(k, "toe", (0.074, -0.038, 0.007), (0.25, -1.0, 0.0), (0.03, 0, 0), 0.016, 0.0055, BLUE, "leg{s}",
           tip=BLUE)

    k.build(voxel=V, smooth=4, band=0.004, wband=0.02)

    # --- the eyes: yellow in the blue bulge, a tall black pupil toward the middle ---------------------------------
    CY, CK = srgb(YELLOW), srgb(INK)
    def eye(u, v):
        return np.tile(CY, (len(u), 1))
    E = np.array([0.046, -0.052, 0.206])
    de = unit(0.3, -1, 0.12)
    def eyes(state):
        k.decal(E, de, (0.029, 0.036), eye_look(state, eye, CBL, CK, line=0.13), eps=0.0014, rings=20, segs=56,
                mirror=True)
        if state in ("Open", "Fierce"):
            k.decal(E + np.array([-0.008, 0, 0.002]), de, (0.0085, 0.016), CK, eps=0.0024, rings=8, segs=40,
                    mirror=True)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    froakie(*args())
