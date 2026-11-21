# Simisear sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/simisear.py -- <the export's Simisear.glb> <out.glb>
#
# Simisage's tall monkey (sibling("simisage")) with its own colours and mane, as Pansear is Pansage's. From the
# references: red where Simisage is green; a mane of flame on the head (a curled mound over the brow with a swirl on
# its front, a tall tongue of flame rising from it, two more curling out to the sides and two small ones over the ears);
# very wide ears, red with an orange lining cut like a flame; cream over the face, round eyes with dark pupils, a
# small dark nose and a wide smile; no brows; two puffs of white fur on the shoulders; red arms and cream hands with
# long fingers hanging (the hands open, palms up in the art); a cream chest over round red hips whose edge rises into
# the cream in big points; red legs and cream feet; a great bushy tail of flame puffs behind, out to the +X side.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
import sculptkit
from sculptkit import ell, tube, srgb, unit, tri, lumpy, args

s = sculptkit.sibling("simisage")
p = s.p
RED, TAIL = "#E55A5E", "#CC4650"


def mane(k, P):
    """The mane of flame: a mound over the brow with a swirl, a tall tongue from it and curls out to the sides."""
    main = P["main"]
    D, DR = s.H + np.array([0, -0.016, 0.052]), np.array([0.1, 0.09, 0.064])
    k.add("mound", ell(D, DR), main, "head")
    T = s.H + np.array([0, 0.006, 0.1])
    k.add("tongue", tube([T, T + (-0.006, 0.006, 0.07), T + (0.018, 0.008, 0.125), T + (0.052, 0.004, 0.14),
                          T + (0.066, 0.0, 0.124)], [0.046, 0.032, 0.02, 0.011, 0.005]), main, "head")
    k.add("curl", tube([s.H + (0.05, 0.02, 0.07), s.H + (0.1, 0.022, 0.105), s.H + (0.14, 0.018, 0.13),
                        s.H + (0.152, 0.012, 0.155)], [0.034, 0.025, 0.014, 0.005]), main, "head", mirror=True)
    k.add("sidecurl", tube([s.H + (0.085, 0.03, 0.04), s.H + (0.125, 0.034, 0.06), s.H + (0.15, 0.03, 0.085)],
                           [0.026, 0.016, 0.005]), main, "head", mirror=True)
    # The swirl: a ridge winding in to a point on the mound's front
    S = p.on_ell(D, DR, (0.0, -1, 0.3))
    n = unit(*((S - D) / DR ** 2))
    ex = np.cross((0, 0, 1.0), n); ex /= np.linalg.norm(ex)
    ez = np.cross(n, ex)
    pts = []
    for i in range(26):
        t = 2.5 * math.pi * i / 25
        r = 0.048 * (1 - t / (2.9 * math.pi))
        q = S + r * (math.cos(t + 2.2) * ex + math.sin(t + 2.2) * ez)
        pts.append(p.on_ell(D, DR, q - D, lift=0.004))
    k.add("swirl", tube(pts, np.linspace(0.016, 0.012, len(pts))), main, "head")


def puffs(k, P):
    """Two puffs of white fur on the shoulders."""
    k.add("puff", ell((0.074, -0.03, 0.322), (0.06, 0.052, 0.046), deform=lumpy(5, 3, 0.12), sub=3), s.FUR, "body",
          fuse=False, mirror=True, surface="fur")


def open_hand(k, P, W, fw):
    """A cream palm, four long fingers hanging forward and down, the thumb inward."""
    C = W + fw * 0.028
    k.add("hand", ell(C, (0.038, 0.034, 0.022)), p.CREAM, "arm{s}", mirror=True)
    for j, dx in enumerate((-0.022, -0.006, 0.01, 0.026)):
        a = C + np.array([dx, -0.02, -0.004])
        k.add(f"finger{j}", tube([a, a + (dx * 0.25, -0.03, -0.012), a + (dx * 0.35, -0.042, -0.04)],
                                 [0.0155, 0.015, 0.0145]), p.CREAM, "arm{s}", mirror=True)
    k.add("thumb", tube([C + (-0.03, -0.004, 0.004), C + (-0.05, -0.022, -0.004)], [0.016, 0.015]), p.CREAM,
          "arm{s}", mirror=True)


def flame_tail(k, P):
    """A great bushy tail: a stalk out of the back and three puffs climbing to a point, out to the +X side."""
    k.add("tail", tube([(0, 0.06, 0.17), (0.05, 0.13, 0.14), (0.1, 0.18, 0.14)], [0.03, 0.034, 0.04]), TAIL, "tail")
    for i, (c, r) in enumerate((((0.13, 0.2, 0.155), (0.086, 0.076, 0.078)),
                                 ((0.19, 0.212, 0.258), (0.07, 0.064, 0.066)),
                                 ((0.218, 0.2, 0.345), (0.052, 0.05, 0.05)))):
        k.add(f"tailpuff{i}", ell(c, r), TAIL, "tail")
    k.add("tailtip", tube([(0.22, 0.198, 0.37), (0.214, 0.194, 0.43), (0.19, 0.19, 0.47), (0.172, 0.19, 0.468)],
                          [0.04, 0.024, 0.011, 0.005]), TAIL, "tail")


def zig_waist(th):
    """Round red hips whose edge rises into the cream in big points across the front."""
    front = np.clip(np.cos(th), 0, 1)
    return 0.198 + 0.042 * (tri(th / 1.1 + 0.5) + 1) / 2 * front ** 1.2


def sear_top(ax):
    return 0.046 - 1.0 * ax ** 2


def round_eye(u, v):
    """A white ringed dark, a round dark pupil in its middle."""
    c = np.tile(srgb(p.INK), (len(u), 1))
    c[u * u + v * v < 0.775 ** 2] = p.WHITE
    c[((u + 0.04) / 0.38) ** 2 + ((v + 0.04) / 0.44) ** 2 < 1] = srgb(p.INK)
    return c


def smile():
    CC, CI = srgb(p.CREAM), srgb(p.INK)
    def look(u, v):
        c = np.tile(CC, (len(u), 1))
        line = -0.2 + 0.55 * u * u
        c[(np.abs(v - line) < 0.24) & (np.abs(u) < 0.92)] = CI
        return c
    return look, None, (0.042, 0.017), (0, -1, -0.5)


FLAME0 = [(-0.8, -0.2), (-0.5, -0.62), (0.3, -0.66), (0.82, -0.25), (0.9, 0.18), (0.6, 0.04), (0.74, 0.46),
         (0.3, 0.24), (0.02, 0.56), (-0.45, 0.42)]

FLAME = [(0.84 * a, 0.86 * b) for a, b in FLAME0]

SIMISEAR = dict(
    s.SIMISAGE, main=RED, lining="#F5A02E",
    ear_c=(0.2, 0.026, 0.44), ear_r=(0.1, 0.024, 0.072), ear_rot=(0, -14, 20), ear_outline=FLAME,
    crest=mane, brows=None, collar=puffs, cuffs=False, legs_cuffs=False, hand=open_hand, tail=flame_tail,
    top=sear_top, waist=zig_waist, eye=round_eye, eye_outline=None, eye_half=(0.028, 0.034),
    eye_dir=(0.4, -0.9, 0.04), lid=p.CREAM, mouth=smile, hips=(0.104, 0.088, 0.064),
    shoulder=(0.058, -0.008, 0.3), elbow=(0.112, -0.064, 0.244), wrist=(0.132, -0.112, 0.19),
    arm_r=[0.039, 0.034, 0.031], arm_paint=True,
)

if __name__ == "__main__":
    src, out = args()
    s.tall(src, out, SIMISEAR)
