# Gothitelle sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/gothitelle.py -- <the export's Gothitelle.glb> <out.glb>
# Grown from gothita.py and gothorita.py: the same lilac, black and white, the same bows and red pout, Gothorita's
# heavy-lidded blue eyes and the fringe coming down to a point between them.
#
# The design, from the references: a tall lady. A black head of hair drawn up into a point at its top, leaning a
# little to one side, a white bow behind the point; the lilac face at its front under the pointed fringe, the eyes
# half shut, a small red mouth. A crown of long flat black strands of hair fanning out from behind the head, four to a
# side, rounded at their ends. A long black gown in four tiers, each flaring and stepping out over the next, a solid
# bell to the ground (no legs show), white bows down its front: at the collar and on each lower tier. Thin black arms
# held forward and down, a white cuff at each wrist and a pointed lilac hand curling up out of it.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, lathe, srgb, inside, smoothstep, unit, eye_look, args, sibling

g = sibling("gothita")
gr = sibling("gothorita")
LILAC, BLACK, WHITE = g.LILAC, g.BLACK, g.WHITE

# --- the frame (the skeleton's joints: body (0, 0, 0.3), head (0, -0.01, 0.5), arms (+-0.05, 0, 0.44)) ------------
HC = np.array([0.0, -0.006, 0.586])      # the head's middle
HR = np.array([0.093, 0.086, 0.102])
# the gown's tiers: top, bottom, radius at the top, radius at the hem
TIERS = [(0.478, 0.352, 0.036, 0.074), (0.368, 0.268, 0.058, 0.094), (0.284, 0.138, 0.074, 0.124),
         (0.154, 0.0, 0.098, 0.152)]


def gown_radius(z):
    """The outer radius of the gown at height z (the tier whose skirt is outermost there)."""
    best = 0.0
    for top, bot, rt, rb in TIERS:
        if bot <= z <= top:
            best = max(best, rt + (rb - rt) * (top - z) / (top - bot))
    return best


def gothitelle(src, out):
    k = Kit(src, coat="fur")

    # --- the gown: four tiers, each a crisp bell of its own stepping out over the next, flat under its hem
    for i, (top, bot, rt, rb) in enumerate(TIERS):
        prof = [(0, top + 0.012), (rt * 0.7, top + 0.01), (rt, top), (rb, bot + 0.008), (rb - 0.003, bot),
                (0, bot)]
        k.add(f"tier{i}", lathe((0, 0.004, 0), (0, 0, 1), prof, seg=64), BLACK, "body", fuse=False)
    # a black core inside it for the body's weights, and the neck
    k.add("core", ell((0, 0.004, 0.4), (0.04, 0.036, 0.09)), BLACK, "body")
    k.add("neck", tube([(0, 0.0, 0.45), (0, -0.004, 0.52)], [0.024, 0.022]), BLACK, "head")

    # --- the arms: thin, held forward and down, a white cuff at the wrist, a pointed lilac hand curling up
    sh, el, wr = np.array([0.04, 0.0, 0.448]), np.array([0.1, -0.05, 0.41]), np.array([0.128, -0.118, 0.39])
    k.add("arm", tube([sh, el, wr], [0.014, 0.012, 0.011]), BLACK, "arm{s}", mirror=True)
    d = unit(*(wr - el))
    k.add("cuff", lathe(wr - d * 0.006, d, [(0, -0.007), (0.026, -0.007), (0.03, -0.002), (0.03, 0.003),
                                           (0.026, 0.007), (0, 0.007)], seg=40), WHITE, "arm{s}", fuse=False,
          mirror=True)
    hp = [wr, wr + d * 0.03 + np.array([0, 0, 0.004]), wr + d * 0.056 + np.array([0.004, 0, 0.018]),
          wr + d * 0.068 + np.array([0.006, 0, 0.034])]
    k.add("hand", tube(hp, [0.015, 0.015, 0.01, 0.004]), LILAC, "arm{s}", fuse=False, mirror=True)

    # --- the head: black hair drawn up into a point, the lilac face at its front under the pointed fringe
    k.add("head", ell(HC, HR, sub=5), BLACK, "head")
    k.add("face", ell(HC + np.array([0, -0.012, -0.014]), (0.082, 0.076, 0.088)), BLACK, "head")
    k.add("point", tube([HC + np.array([0, 0.006, 0.05]), HC + np.array([0.006, 0.012, 0.11]),
                         HC + np.array([0.022, 0.02, 0.16]), HC + np.array([0.04, 0.026, 0.184])],
                        [0.05, 0.03, 0.013, 0.004]), BLACK, "head")
    def face(P):
        Q = P - HC
        th = np.arctan2(Q[:, 0], -Q[:, 1])
        a = th * 0.09
        z = Q[:, 2]
        oval = (a / 0.068) ** 2 + ((z + 0.026) / 0.076) ** 2 - 1
        top = 0.0 + 0.75 * np.abs(a)
        top = np.minimum(top, 0.052 - 0.6 * np.clip(np.abs(a) - 0.05, 0, 1))
        return inside(oval * 0.06, 0.003) * inside(z - top, 0.003) * (Q[:, 1] < 0)
    k.paint(face, LILAC)

    # the mouth: a small red pout
    lp, ln = g.on_ell(HC, HR, (0, -1, -0.6))
    k.add("lipbump", ell(lp - ln * 0.003, (0.013, 0.009, 0.011)), g.LIPS, "head")
    g.lips(k, lp + ln * 0.005, ln, (0.013, 0.012))

    # the crown of strands: four a side fanning out from behind the head, swept a little back, rounded at the ends;
    # their flat faces to the front
    for s, side in ((1, "R"), (-1, "L")):
        for j, (ang, ln_) in enumerate(((42, 0.185), (17, 0.215), (-8, 0.215), (-33, 0.185))):
            a = math.radians(ang)
            dr = unit(s * math.cos(a), 0.22, math.sin(a))
            root = HC + np.array([s * 0.03, 0.03, 0.0]) + dr * 0.02
            bend = unit(s * math.cos(a), 0.3, math.sin(a) - 0.12)          # drooping a little toward the end
            pts = [root, root + dr * ln_ * 0.4, root + dr * ln_ * 0.75, root + (dr * 0.6 + bend * 0.4) * ln_]
            k.add(f"strand{side}{j}", blade(pts, [0.022, 0.023, 0.024, 0.022], (0, 1, 0), 0.45), BLACK, "head",
                  fuse=False)

    # --- the bows: one behind the point, one at the collar, one on each lower tier down the front
    g.bow(k, "headbow", HC + np.array([0, 0.034, 0.088]), (1, 0, 0.12), (0, -1, 0.55), 0.06, 0.036, 0.018, "head")
    g.bow(k, "collarbow", (0, -0.052, 0.456), (1, 0, 0.05), (0, -1, 0.3), 0.056, 0.032, 0.018, "body")
    for i, (z, size) in enumerate(((0.338, 1.0), (0.236, 1.08), (0.11, 1.18))):
        r = gown_radius(z)
        g.bow(k, f"gownbow{i}", (0, 0.004 - r - 0.012, z), (1, 0, 0.04), (0, -1, 0.35), 0.056 * size, 0.034 * size,
              0.02 * size, "body")

    k.build(voxel=0.005, smooth=4, band=0.006)

    # --- the eyes: Gothorita's, half shut under heavy lids
    LID, INK = srgb(LILAC), srgb(g.INKH)
    eye = gr.heavy_eye()
    open_look = lambda u, v: eye(u, v, LID)
    ep, en = g.on_ell(HC, HR, (0.4, -0.92, -0.06))
    def lay(state):
        k.decal(ep, en, (0.029, 0.03), eye_look(state, open_look, LID, INK, line=0.15), eps=0.003, rings=20,
                segs=56, mirror=True)

    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    gothitelle(*args())
