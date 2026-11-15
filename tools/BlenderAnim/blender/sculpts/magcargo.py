# Magcargo sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/magcargo.py -- <the export's Magcargo.glb> <out.glb>
# Grown from slugma.py (sibling("slugma")): its puddle, craters, drips, flames and way of making eyes kept.
#
# From the references: a bigger slug of lava, redder than Slugma, its body a thick column out of a lumpy cratered
# puddle; a frog-like head: two yellow eyes with black pupils on round bumps on top, a great round snout in front, a
# wide smiling mouth under it; thick drips of lava hang from both sides of the head down the column. On its back a
# large round shell of grey-blue rock in lumpy plates with glowing orange lava in the cracks between them, and a flame
# rising from the shell's top, red outside and yellow at its heart, leaning back and to one side.
# The skeleton: body (0, 0, 0.1), head (0, -0.03, 0.24), shell (0, 0.12, 0.24).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, inside, smoothstep, unit, eye_look, sibling, args

sl = sibling("slugma")

MAG, DEEP, LIGHT = "#EC4A3C", "#B8302A", "#FF7A60"
ROCK, ROCK2 = "#7E8CA6", "#6A7792"
SEAM, SEAM_GLOW = "#FF8A2A", "#FF8A2A"
FIRE, FIRE_GLOW, HEART, HEART_GLOW = "#F2442A", "#FF4A28", "#FFD040", "#FFD860"
MOUTHC = "#5A140E"

H = np.array([0.0, -0.06, 0.45])
HR = np.array([0.125, 0.11, 0.09])
SNOUT = np.array([0.0, -0.155, 0.43])
SNOUTR = (0.075, 0.065, 0.062)
BUMP = np.array([0.068, -0.1, 0.53])
BUMPR = 0.056
EN = unit(0.32, -0.88, 0.35)
EHALF = (0.048, 0.046)
SC = np.array([0.0, 0.165, 0.35])          # the shell's middle
SR = np.array([0.17, 0.18, 0.18])


def eye_open(u, v):
    c = np.tile(srgb(sl.YELLOW), (len(u), 1))
    c[u * u + v * v > 0.8 ** 2] = srgb(sl.RIM)
    c[(u + 0.04) ** 2 + (v + 0.0) ** 2 < 0.3 ** 2] = srgb(sl.PUPIL)
    return c


def rock_shell(k, c, r, n=24, seed=5, gap=0.1, depth=0.8, bump=0.08, bone="shell"):
    """A round shell of rock plates (flat-faced hulls) over a glowing core that shows in the cracks between them."""
    rs = np.random.RandomState(seed)
    i = np.arange(n) + 0.5
    phi = np.arccos(1 - 2 * i / n); th = math.pi * (1 + 5 ** 0.5) * i
    S = np.stack([np.cos(th) * np.sin(phi), np.sin(th) * np.sin(phi), np.cos(phi)], 1) + rs.normal(0, 0.12, (n, 3))
    S /= np.linalg.norm(S, axis=1, keepdims=True)
    m = 2400
    j = np.arange(m) + 0.5
    ph = np.arccos(1 - 2 * j / m); tt = math.pi * (1 + 5 ** 0.5) * j
    D = np.stack([np.cos(tt) * np.sin(ph), np.sin(tt) * np.sin(ph), np.cos(ph)], 1)
    owner = np.argmax(D @ S.T, axis=1)
    for q in range(n):
        if S[q, 1] < -0.55:            # facing into the body: never seen
            continue
        pts = D[owner == q]
        if len(pts) < 4:
            continue
        pts = pts * (1 - gap) + S[q] * gap
        pts /= np.linalg.norm(pts, axis=1, keepdims=True)
        h = 1 + bump * rs.rand()
        # a little dome to each plate: the middle stands proud of the rim
        lift = 1 + 0.05 * (pts @ S[q] - (pts @ S[q]).min()) / max(1e-6, np.ptp(pts @ S[q]))
        outer = c + pts * r * (h * lift)[:, None]
        inner = c + pts * r * depth
        k.add(f"plate{q}", hull(np.vstack([outer[::2], inner[::3]])), ROCK if q % 3 else ROCK2, bone, fuse=False,
              surface="shell")
    k.add("core", ell(c, r * 0.93, sub=4), SEAM, bone, fuse=False, glow=SEAM_GLOW)


def magcargo(src, out):
    k = Kit(src, coat="scales")

    # The puddle of lava, a mound, lumps round its rim
    sl.puddle(k, "puddle", (0, 0.04, 0.03), 0.265, 0.25, 0.045, "body", colour=MAG)
    k.add("mound", ell((0, 0.04, 0.09), (0.19, 0.18, 0.08)), MAG, "body")
    for i, (x, y, z, r) in enumerate([(0.17, -0.08, 0.06, 0.055), (-0.18, -0.05, 0.06, 0.055), (0.15, 0.16, 0.07, 0.055),
                                      (-0.14, 0.17, 0.07, 0.055), (0.02, 0.22, 0.06, 0.055), (0.05, -0.17, 0.055, 0.05)]):
        k.add(f"lump{i}", ell((x, y, z), (r, r, r * 0.8)), MAG, "body")

    # The thick column, leaning forward to the head
    k.add("neck", tube([(0, 0.02, 0.16), (0, -0.02, 0.25), (0, -0.045, 0.36), (0, -0.055, 0.42)],
                       [0.14, 0.115, 0.108, 0.11]), MAG, "head")
    k.add("head", ell(H, HR), MAG, "head")
    k.add("snout", ell(SNOUT, SNOUTR), MAG, "head")
    k.add("bump", ell(BUMP, (BUMPR, BUMPR * 0.95, BUMPR)), MAG, "head", mirror=True)

    # Thick drips hanging from both sides of the head down the column
    for s in (1, -1):
        sl.drip(k, f"dripA{s}", (0.115 * s, -0.11, 0.42), 0.12, 0.022, 0.034, "head", colour=MAG)
        sl.drip(k, f"dripB{s}", (0.135 * s, -0.04, 0.43), 0.085, 0.02, 0.03, "head", colour=MAG)

    # Craters in the puddle and the mound
    for (x, y, z), n, r in [((0.14, -0.14, 0.07), unit(0.45, -0.7, 0.55), 0.034),
                            ((-0.1, -0.17, 0.06), unit(-0.3, -0.75, 0.6), 0.028),
                            ((0.1, 0.2, 0.08), unit(0.4, 0.7, 0.6), 0.03)]:
        sl.crater(k, (x, y, z), n, r, deep=DEEP, light=LIGHT, base=MAG)

    # The mouth: a wide smile under the snout
    D = srgb(MOUTHC); B = srgb(MAG)
    def smile(u, v):
        c = np.tile(B, (len(u), 1))
        c[(np.abs(v - (0.35 - 0.75 * (1 - u * u))) < 0.3) & (np.abs(u) < 0.9)] = D
        return c
    k.decal((0, -0.14, 0.355), unit(0, -1, -0.25), (0.075, 0.03), smile, eps=0.004, rings=10, segs=48)

    # The shell of rock on the back, and its flame
    rock_shell(k, SC, SR)
    fl = sl.Flames()
    base = SC + np.array([0.02, 0.03, 0.15])
    fl.flame(base, (0.3, 0.45, 1), 0.2, 0.05, "shell", side=(1, 0, 0), flick=0.35, colour=FIRE, glow=FIRE_GLOW,
             heart=HEART, heart_glow=HEART_GLOW)
    fl.flame(base + (0.0, 0.02, -0.01), (0.1, 0.9, 0.8), 0.11, 0.035, "shell", side=(0, 0, 1), flick=0.3, colour=FIRE,
             glow=FIRE_GLOW, heart=None)
    fl.add(k)

    k.build(voxel=0.0065, smooth=3)

    E = BUMP + EN * BUMPR * 0.9
    k.eye_states(lambda st: sl.lay_eyes(k, st, E, EN, EHALF, lid=MAG, look_open=eye_open))
    k.export(out)


if __name__ == "__main__":
    magcargo(*args())
