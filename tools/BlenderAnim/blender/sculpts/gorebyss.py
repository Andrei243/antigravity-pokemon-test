# Gorebyss sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/gorebyss.py -- <the export's Gorebyss.glb> <out.glb>
#
# From the references: a long pink fish whose round head is the thickest of it, drawn out in front into a long thin
# pointed snout; a big oval eye on each side, white ringed black with a tall black pupil and three lashes; a long
# streamer from the top of the head sweeping up and back, its end lavender; a thin fin hanging back from under the
# chin; a pair of lavender fins like little clam shells on the chest; the body tapering back and down, then the tail
# bending up again, two white bands round it near the bend, and a round lavender disc standing at its end.
# The body is laid as dratini.py's (its Line, by sibling). The skeleton: body (0.06, 0.32, 0.16) with the tail on the
# same joint, seg1 (0.04, 0.22, 0.26) with finL and finR on it, seg2 (0, 0.08, 0.34), head (0, -0.04, 0.36).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, ring_band, srgb, unit, eye_look, args, outward, sibling

PINK, LAV, LAVDARK, WHITE, INK = "#F4A2CC", "#B794DC", "#9C78C8", "#F8F6F6", "#141418"

dr = sibling("dratini")

# The line, tail's end first: up from the bend under the disc, down and back up through the joints to the head
LINE = [(0.07, 0.56, 0.31), (0.072, 0.535, 0.2), (0.07, 0.47, 0.115), (0.066, 0.39, 0.11), (0.06, 0.32, 0.16),
        (0.05, 0.27, 0.215), (0.04, 0.22, 0.26), (0.02, 0.15, 0.31), (0.0, 0.08, 0.34), (0.0, -0.03, 0.365)]
RADS = [0.018, 0.021, 0.025, 0.03, 0.035, 0.042, 0.05, 0.06, 0.07, 0.08]
BONES = ["tail", "tail", "tail", "tail", "body", "body", "seg1", "seg1", "seg2"]

HC = np.array([0.0, -0.072, 0.378])          # the head
HR = np.array([0.08, 0.108, 0.094])


def on(c, r, d):
    d = unit(*d)
    return c + d * r, d


def gorebyss(src, out):
    k = Kit(src, coat="scales")
    line = dr.Line(LINE, RADS)
    line.tubes(k, BONES, PINK)

    # --- the head and its snout --------------------------------------------------------------------------------------
    k.add("skull", ell(HC, HR, rot=(6, 0, 0)), PINK, "head")
    k.add("snout", tube([(0, -0.135, 0.37), (0, -0.21, 0.343), (0, -0.27, 0.318), (0, -0.33, 0.302)],
                        [0.054, 0.03, 0.015, 0.008]), PINK, "head")
    # the streamer from the top of the head, up and back, lavender at its end
    pts = [(0.0, -0.06, 0.45), (0.01, -0.02, 0.495), (0.035, 0.05, 0.515), (0.06, 0.13, 0.53), (0.075, 0.2, 0.535)]
    k.add("streamer", outward(tube(pts[:4], [0.019, 0.018, 0.017, 0.016])), PINK, "head", fuse=False,
          surface="scales")
    k.add("streamertip", outward(tube([pts[2], pts[3], pts[4]], [0.0175, 0.0175, 0.012])), LAV, "head", fuse=False,
          surface="scales")
    # the thin fin under the chin, hanging back
    k.add("chin", outward(tube([(0, -0.09, 0.31), (0, -0.075, 0.27), (0.004, -0.045, 0.235), (0.01, -0.01, 0.215)],
                               [0.016, 0.015, 0.013, 0.01])), PINK, "head", fuse=False, surface="scales")

    # --- the chest fins: lavender, scalloped like little clam shells, faces to the front and back -------------------
    for s, side in ((1, "R"), (-1, "L")):
        root = np.array([0.04 + s * 0.035, 0.215, 0.245])
        ex = unit(s * 1.0, 0.35, -0.25)
        ez = np.array([0, 0, 1.0])
        R = 0.056
        pts = [(0.0, 0.0)]
        for a in np.linspace(-0.47 * math.pi, 0.47 * math.pi, 31):
            rr = R * (1 - 0.16 * abs(math.sin(1.5 * (a + 0.47 * math.pi) * 2 / 0.94)))
            pts.append((rr * math.cos(a), rr * math.sin(a)))
        k.add("fin" + side, slab([(x, y) for x, y in pts], root, ex, ez, 0.018), LAV, "fin" + side, fuse=False,
              surface="scales")
        k.add("finknob" + side, outward(ell(root + ex * 0.012, (0.016, 0.016, 0.02))), LAVDARK, "fin" + side,
              fuse=False, surface="scales")

    # --- the tail: two white bands near the bend, a round disc standing at its end -----------------------------------
    for sv, ln in ((2.3, 0.022), (2.72, 0.016)):
        p, t, rad = line.at(sv)
        k.add(f"band{sv}", ring_band(p, t, rad - 0.003, ln), WHITE, "tail", fuse=False, surface="scales")
    p, t, rad = line.at(0.0)
    ex = unit(0.57, 0.82, 0)
    disc = [(0.05 * math.cos(a), 0.062 * math.sin(a)) for a in np.linspace(0, 2 * math.pi, 40, endpoint=False)]
    k.add("disc", slab(disc, p + np.array([0, 0.008, 0.05]), ex, (0, 0, 1.0), 0.02), LAV, "tail", fuse=False,
          surface="scales")

    k.build(voxel=0.0042, smooth=4, band=0.006)

    # --- the eyes: big ovals, white ringed black, a tall black pupil, three lashes over the top --------------------
    W, K, L = srgb(WHITE), srgb(INK), srgb(PINK)

    def eye(u, v):
        c = np.tile(L, (len(u), 1))
        r = np.sqrt(u * u + v * v)
        c[r < 0.74] = K
        c[r < 0.62] = W
        c[(u / 0.28) ** 2 + ((v + 0.02) / 0.43) ** 2 < 1] = K
        ang = np.arctan2(v, u)
        for a in (math.radians(55), math.radians(80), math.radians(105)):
            ca, sa = math.cos(a), math.sin(a)
            along = u * ca + v * sa
            across = -u * sa + v * ca
            c[(along > 0.68) & (along < 0.98) & (np.abs(across) < 0.07)] = K
        return c

    def eyes(state):
        look = eye_look(state, eye, L, K, line=0.12)
        for s in (1, -1):
            lk = look if s > 0 else (lambda f: (lambda u, v: f(-u, v)))(look)
            c, n = on(HC, HR, (s * 0.66, -0.7, 0.2))
            k.decal(c, n, (0.042, 0.049), lk, eps=0.003, rings=32, segs=80)

    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    gorebyss(*args())
