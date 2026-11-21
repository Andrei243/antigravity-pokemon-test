# Galarian Moltres sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/moltresgalar.py -- <the export's Moltres-Galar.glb> <out.glb>
# Moltres's kin on moltres.py's frame (its trunk and long neck, its ragged tongues of fire, articuno.py's wing axes and
# eyes cut along a brow): a long slender bird of charcoal black, a black curl of a crest hooked up and over off the
# crown, pale blue eyes in a crimson rim, a long open crimson beak, a crimson band down each side of the neck to the
# breast, great wings raised high and swept back whose leading edges are black flame and whose bodies are crimson and
# pink fire, a tail of black and pink flame streaming back, and crimson legs with dark talons hanging under.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, srgb, inside, unit, sibling, args

mo = sibling("moltres")
a = mo.a
sp = a.sp

BODY, BODY2, SMOKE = "#2E2A33", "#38333E", "#26222A"
CRIMSON, PINK, ROSE = "#D41C58", "#F04A8C", "#FA8CB8"
BEAK, MOUTH, LEG, CLAW = "#D8245C", "#F4A2AE", "#D42A5E", "#3A2830"
WHITE, IRIS, PUPIL, RIM = "#F4FBFE", "#9CD8EE", "#1A1418", "#D41C58"

H = np.array([0, -0.205, 0.545])        # the head's centre, carried forward of the head's joint on a long neck
HR = np.array([0.07, 0.08, 0.068])      # a tenth over Moltres's: a small head on a long neck is lost

ELEV, SWEEP = 52, 32
SPAN = 0.3
ARM = [(0.0, -0.025), (SPAN * 0.5, -0.03), (SPAN, -0.012), (SPAN + 0.03, 0.02), (SPAN * 0.7, 0.06), (0.12, 0.08),
       (0.0, 0.06)]

groups = {}


def flame(base, d, length, width, bone, side, colour, flick=0.3, core=None):
    """moltres.py's tongue, in one of the form's colours; a core of another colour on both its faces if asked."""
    b = np.array(base, float)
    groups.setdefault((bone, colour), []).append(mo.tongue(b, d, length, width, side, flick))
    if core:
        ny = mo.frame(d, side)[:, 1]
        for sg in (1, -1):
            groups.setdefault((bone, core), []).append(
                mo.tongue(b + ny * sg * width * 0.25, d, length * 0.55, width * 0.55, side, flick * 0.5, thick=0.3,
                          rag=0.15, root=0.25))


GLOWS = {PINK: "#FF4C90", ROSE: "#FF8CBC"}


def add_flames(k):
    for i, ((bone, colour), shapes) in enumerate(groups.items()):
        V, F, off = [], [], 0
        for sv, sf in shapes:
            V.append(sv); F.extend([[j + off for j in f] for f in sf]); off += len(sv)
        k.add(f"flame{i}", (np.vstack(V), F), colour, bone, fuse=False, mirror="{s}" in bone, glow=GLOWS.get(colour))


def galar(src, out):
    k = Kit(src, coat="fur")

    # A long slender trunk leaning forward, a long neck carrying the head out and up
    k.add("body", ell((0, 0.03, 0.385), (0.088, 0.152, 0.092), rot=(-14, 0, 0)), BODY, "body")
    k.add("breast", ell((0, -0.07, 0.41), (0.078, 0.072, 0.078)), BODY, "body")
    k.add("neck", tube([(0, -0.08, 0.43), (0, -0.15, 0.49), H + (0, 0.025, -0.025)], [0.056, 0.048, 0.048]), BODY,
          "head")
    k.add("head", ell(H, HR), BODY, "head")
    # a crimson band down each side of the neck, from under the eye to the breast
    A_, B_ = np.array([H[1] + 0.005, H[2] - 0.03]), np.array([-0.06, 0.37])
    def band(P):
        q = P[:, 1:3] - A_
        e = B_ - A_
        t = np.clip(q @ e / (e @ e), 0, 1)
        d = np.linalg.norm(q - t[:, None] * e[None], axis=1)
        return inside(d - 0.016, 0.003) * (np.abs(P[:, 0]) > 0.03)
    k.paint(band, CRIMSON, on=["neck", "head", "breast", "body"])

    # The crest: a black curl hooked up off the crown and over toward the brow, a wisp of smoke from the nape
    curl = [H + (0, 0.03, 0.05), H + (0, 0.07, 0.12), H + (0, 0.04, 0.18), H + (0, -0.01, 0.175), H + (0, -0.015, 0.14)]
    k.add("curl", tube(curl, [0.03, 0.026, 0.021, 0.017, 0.014], seg=20), BODY2, "head", fuse=False)
    flame(H + (0, 0.05, 0.02), unit(0, 1, 0.35), 0.16, 0.04, "head", (1, 0, 0), SMOKE, flick=0.3)
    flame(H + (0.02, 0.05, -0.01), unit(0.3, 1, 0.1), 0.12, 0.032, "head", (0, 0, 1), SMOKE, flick=0.25)
    flame(H + (-0.02, 0.05, -0.01), unit(-0.3, 1, 0.1), 0.12, 0.032, "head", (0, 0, 1), SMOKE, flick=0.25)

    # The long crimson beak, open: the upper half long and hooked a little, the lower shorter and dropped, pink between
    B = H + (0, -0.064, -0.006)
    D = unit(0, -0.93, -0.36)
    k.add("beak", tube([B, B + D * 0.08, B + D * 0.16 + (0, 0, -0.008)], [0.024, 0.015, 0.003], seg=20), BEAK, "head",
          fuse=False, surface="shell")
    L = B + (0, 0.004, -0.018)
    DL = unit(0, -0.8, -0.6)
    k.add("jaw", tube([L, L + DL * 0.06, L + DL * 0.11], [0.018, 0.012, 0.003], seg=20), BEAK, "head", fuse=False,
          surface="shell")
    k.add("mouth", ell(B + D * 0.035 + (0, 0, -0.016), (0.014, 0.035, 0.012), rot=(-30, 0, 0)), MOUTH, "head",
          fuse=False)

    # The great wings raised high and swept back: black flame along the leading edge, crimson and pink fire behind
    ex, ez = a.wing_axes(ELEV, SWEEP, (0.25, 0.65, -0.7))
    root = np.array([0.07, 0.0, 0.455])
    k.add("wing", slab(ARM, root, ex, ez, 0.026), BODY, "wing{s}", fuse=False, mirror=True)
    k.add("shoulder", ell(root + ex * 0.02, (0.042, 0.052, 0.042)), BODY, "wing{s}", mirror=True)
    back = unit(*(0.55 * ez + np.array([0, 0.75, 0.15])))
    for j, (t, Lb, Lp) in enumerate(((0.03, 0.2, 0.26), (0.1, 0.24, 0.3), (0.17, 0.26, 0.31), (0.24, 0.24, 0.27),
                                     (0.31, 0.2, 0.22))):
        p = root + ex * t
        # the leading edge's black flame, rising out and up
        flame(p - ez * 0.01, unit(*(0.55 * ex + 0.5 * np.array([0, 0.2, 1]) - 0.2 * ez)), Lb, 0.05, "wing{s}", ez,
              SMOKE, flick=0.22)
        # the fire of the wing's body, streaming back from the arm
        flame(p + ez * 0.03, unit(*(back + 0.25 * ex)), Lp + 0.04, 0.09, "wing{s}", ex, CRIMSON, flick=0.25, core=PINK)
    flame(root + ex * (SPAN + 0.01), unit(*(ex + 0.2 * ez + np.array([0, 0, 0.3]))), 0.15, 0.045, "wing{s}", ez, SMOKE,
          flick=0.2)

    # The tail: flame streaming back, black above and crimson and pink below
    TB = np.array([0, 0.15, 0.35])
    for d, Ln, w, c in (((0, 1, 0.2), 0.3, 0.065, SMOKE), ((0.4, 1, 0.3), 0.24, 0.055, SMOKE),
                        ((-0.4, 1, 0.3), 0.24, 0.055, SMOKE), ((0.25, 1, -0.15), 0.28, 0.06, CRIMSON),
                        ((-0.25, 1, -0.15), 0.28, 0.06, CRIMSON)):
        flame(TB, unit(*d), Ln * 1.3, w * 1.65, "tail", (0, 0, 1) if abs(d[0]) < 0.1 else (1, 0, 0), c, flick=0.3,
              core=PINK if c == CRIMSON else None)
    k.add("rump", ell(TB + (0, -0.03, 0.0), (0.052, 0.062, 0.047)), BODY, "tail")
    add_flames(k)

    # Crimson legs hanging under dark thighs, dark talons
    k.add("thigh", ell((0.046, 0.01, 0.315), (0.038, 0.044, 0.044)), BODY, "leg{s}", mirror=True)
    for side in ((1, "R"), (-1, "L")):
        sp.foot(k, side, (0.048, 0.012, 0.3), (0.052, 0.03, 0.18), 0.045, LEG, CLAW, r=0.015)

    # The eyes: pale blue with a dark pupil in a crimson rim, under a brow slanting down toward the beak
    de = unit(0.62, -0.72, 0.16)
    E = a.eye_at(H, HR, de)
    def look(u, v):
        c = np.tile(srgb(RIM), (len(u), 1))
        c[(u / 0.8) ** 2 + (v / 0.8) ** 2 < 1] = srgb(IRIS)
        c[((u + 0.1) / 0.36) ** 2 + ((v + 0.05) / 0.48) ** 2 < 1] = srgb(PUPIL)
        c[((u - 0.12) / 0.14) ** 2 + ((v - 0.18) / 0.14) ** 2 < 1] = srgb(WHITE)
        return c
    def eyes(state):
        sp.lay_eyes(k, state, E, de, (0.03, 0.026), look, lambda u: 0.45 + 0.3 * u, lambda u: -0.05 + 0.6 * u,
                    srgb(BODY), srgb(CRIMSON))
    k.build(voxel=0.0062, band=0.006)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    galar(*args())
