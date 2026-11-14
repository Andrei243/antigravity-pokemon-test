# Sudowoodo sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/sudowoodo.py -- <the export's Sudowoodo.glb> <out.glb>
# Bonsly grown (bonsly.py), in its brown, green and yellow: the pot gone, the trunk a tall log that is head and body in
# one, a forked twig on its top; thin branch arms held up and out, each forking into three twigs that end in round
# green balls; yellow oval spots down the front; short legs set wide on long flat feet turned out; small dark eyes and
# a wide pink grin.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, args

src, out = args()
k = Kit(src, coat="leaf")

BROWN, DARK, GREEN, YELLOW, INK, LIP = "#C08756", "#9E6A42", "#46AE5E", "#F2D14C", "#24180E", "#D88A86"

# The trunk: a tall log, a little wider at the hips, round-topped. Its lower part is the body's, its upper the head's
# (the face lies on the head's part, so the eyes follow the head)
k.add("trunk", tube([(0, 0.0, 0.24), (0.004, 0.0, 0.4), (0.0, 0.0, 0.55)], [0.118, 0.106, 0.1]), BROWN, "body")
k.add("top", tube([(0.0, 0.0, 0.55), (-0.004, 0.0, 0.64), (-0.006, 0.0, 0.71)], [0.1, 0.098, 0.094]), BROWN, "head")
k.add("hips", ell((0, 0.0, 0.2), (0.13, 0.105, 0.085)), BROWN, "body")

# The forked twig on top: a short stalk, a knot, two blunt branches up and out, the left lower and the right longer
k.add("stalk", tube([(-0.006, 0.0, 0.77), (-0.004, 0.0, 0.84), (0.0, 0.0, 0.87)], [0.026, 0.022, 0.021]), BROWN, "head")
k.add("knot", ell((0.0, 0.0, 0.872), (0.03, 0.022, 0.02)), BROWN, "head")
k.add("twigL", tube([(0.0, 0.0, 0.875), (-0.05, 0.0, 0.9), (-0.095, 0.0, 0.915)], [0.02, 0.019, 0.019]), BROWN, "head")
k.add("twigR", tube([(0.0, 0.0, 0.875), (0.035, 0.0, 0.915), (0.07, 0.0, 0.955)], [0.02, 0.019, 0.019]), BROWN, "head")

# Branch arms held up and out, each ending in a fork of three short twigs and three green balls
FORK = np.array([0.32, -0.005, 0.69])
BALLS = [np.array(b) for b in ((0.39, -0.01, 0.81), (0.25, -0.02, 0.775), (0.405, -0.01, 0.65))]
k.add("arm", tube([(0.07, 0.0, 0.56), (0.17, -0.004, 0.6), (0.26, -0.005, 0.645), FORK], [0.03, 0.024, 0.02, 0.018]),
      BROWN, "arm{s}", mirror=True)
for i, b in enumerate(BALLS):
    d = b - FORK; d /= np.linalg.norm(d)
    k.add(f"sprig{i}", tube([FORK, FORK + d * 0.05], [0.015, 0.013]), BROWN, "arm{s}", mirror=True)
    k.add(f"ball{i}", ell(b, (0.068, 0.066, 0.066)), GREEN, "arm{s}", fuse=False, mirror=True, surface="leaf")

# Short legs set wide, on long flat feet turned out
k.add("leg", tube([(0.06, 0.0, 0.2), (0.125, -0.015, 0.12), (0.15, -0.025, 0.06)], [0.06, 0.05, 0.042]), BROWN,
      "leg{s}", mirror=True)
k.add("foot", ell((0.185, -0.055, 0.028), (0.056, 0.095, 0.028), rot=(0, 0, 35)), BROWN, "leg{s}", mirror=True)

# The darker bark round the hips, as the drawing shades it
k.paint(lambda P: inside(P[:, 2] - (0.2 + 0.03 * np.cos(P[:, 0] * 20)), 0.02) * (P[:, 2] > 0.1) * (np.abs(P[:, 0]) < 0.14),
        DARK, on=["trunk", "hips"])

# Yellow oval spots down the front, slanting a little
SPOTS = [((-0.03, 0.45), 0.012, 0.03, 20), ((0.045, 0.4), 0.011, 0.026, 15), ((-0.06, 0.36), 0.011, 0.027, 25),
         ((0.03, 0.31), 0.012, 0.028, 10), ((-0.045, 0.24), 0.012, 0.03, 25), ((0.07, 0.25), 0.01, 0.022, 0)]
for (x, z), a, b, tilt in SPOTS:
    up = (math.sin(math.radians(tilt)), 0, math.cos(math.radians(tilt)))
    k.decal((x, -0.1, z), unit(x * 3, -1, 0), (a, b), YELLOW, up=up, eps=0.003, rings=8, segs=36)

# The wide grin: a pink mouth curving up, its dark line along the middle
CB, CI, CL = srgb(BROWN), srgb(INK), srgb(LIP)
def mouth(u, v):
    c = np.tile(CB, (len(u), 1))
    # an open grin: a gently curved top, a round bottom, pink inside a dark line
    top = 0.5 - 0.25 * (1 - u * u)
    bot = top - 1.15 * np.clip(1 - u * u, 0, 1) ** 0.6
    au = np.abs(u)
    fill = (v < top) & (v > bot) & (au < 0.97)
    c[fill] = CI
    inner = (v < top - 0.2) & (v > bot + 0.2) & (au < 0.9)
    c[inner] = CL
    return c
k.decal((0, -0.1, 0.668), (0, -1, 0), (0.062, 0.021), mouth, eps=0.003, rings=28, segs=120)

# Small dark eyes, tall ovals with a white glint
def eye(u, v):
    c = np.tile(CB, (len(u), 1))
    c[(u / 0.95) ** 2 + (v / 0.97) ** 2 < 1] = CI
    c[((u - 0.15) / 0.3) ** 2 + ((v - 0.4) / 0.25) ** 2 < 1] = (1, 1, 1)
    return c
de = unit(0.3, -0.95, 0.05)
E = np.array([0.04, -0.097, 0.715])
def eyes(state):
    k.decal(E, de, (0.012, 0.019), eye_look(state, eye, CB, CI, line=0.2), eps=0.005, rings=20, segs=56, mirror=True)

k.build(voxel=0.007)
k.eye_states(eyes)
k.export(out)
