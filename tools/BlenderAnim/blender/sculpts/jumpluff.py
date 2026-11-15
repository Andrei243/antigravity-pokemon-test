# Jumpluff sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/jumpluff.py -- <the export's Jumpluff.glb> <out.glb>
#
# Grown from Hoppip's script (sibling("hoppip")): the same round body, round eyes with their rim and glint, open mouth,
# stubs and legs. From the references: a round blue body; small red eyes with a white glint set high and apart; a
# small open mouth between them, a pink tongue in it; small oval paddles of arms on the front of its sides, hanging;
# two short blue feet under it; three cream balls of cotton, lighter on top and a shade darker below: a big one held
# over its head on a short green stalk with three small leaves at the stalk's foot, and one at each lower side on its
# arms, close against the body.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, leaf, srgb, inside, unit, lumpy, args, outward, sibling

hp = sibling("hoppip")

BLUE = "#5E84DA"
COTTON, COTTON_LOW = "#F4E6B0", "#E0CC90"
GREEN, STALK = "#4EAA3C", "#5EA83C"
RED, RED_DARK, RIM = srgb("#E8405A"), srgb("#B42640"), srgb("#28224A")
INK = srgb("#222850")

# --- Jumpluff's frame (joints: body (0, 0, 0.194), arms (+-0.09, -0.02, 0.184), leaf (0, 0, 0.294),
#     legs (+-0.05, -0.02, 0.104)) ------------------------------------------------------------------------------------
C = np.array([0.0, 0.0, 0.196])
R = np.array([0.11, 0.102, 0.104])
CROWN = np.array([0.0, 0.004, 0.292])
TOP = np.array([0.0, 0.008, 0.412])     # the big ball's middle
TOP_R = 0.088
LOW = np.array([0.112, 0.022, 0.104])   # a lower ball's middle (+X side)
LOW_R = 0.066
EYE_D = (0.42, -0.86, 0.3)
EYE_HALF = (0.019, 0.021)
MOUTH_D = (0.0, -1.0, 0.02)
MOUTH_HALF = (0.021, 0.024)


def cotton(k, name, c, r, bone, seed, lumps=7):
    """A ball of cotton: a lumpy ball with smaller balls fused round it, fuller at its sides."""
    c = np.array(c, float)
    k.add(name, ell(c, (r, r, r * 0.97), deform=lumpy(5, 4, 0.05)), COTTON, bone)
    rs = np.random.RandomState(seed)
    for i in range(lumps):
        a = 2 * math.pi * (i + rs.rand() * 0.5) / lumps
        z = rs.uniform(-0.5, 0.6)
        d = np.array([math.cos(a) * math.sqrt(1 - z * z), math.sin(a) * math.sqrt(1 - z * z), z])
        k.add(f"{name}{i}", ell(c + d * r * 0.62, (r * 0.44,) * 3, sub=3), COTTON, bone)


def cotton_shade(k, c, r):
    """The lower third of a ball a shade darker, its edge soft."""
    c = np.array(c, float)
    def mask(P):
        Q = P.copy(); Q[:, 0] = np.where(c[0] != 0, np.abs(Q[:, 0]), Q[:, 0])
        near = inside(np.linalg.norm(Q - c, axis=1) - r * 1.4, 0.004)
        return near * inside((Q[:, 2] - c[2]) + r * 0.32, 0.012)
    k.paint(mask, COTTON_LOW, on=["top", "low"])


def jumpluff(src, out):
    k = Kit(src, coat="fur")
    k.add("body", ell(C, R, deform=hp.round_body(square=2.1, full=0.03)), BLUE, "body")
    # small oval paddles of arms on the front of the sides, hanging
    hp.stub(k, "arm", (0.1, -0.056, 0.166), (0.026, 0.024, 0.042), BLUE, "arm{s}", rot=(-10, 18, 0))
    hp.legs(k, BLUE, top=(0.05, -0.02, 0.11), bottom=(0.052, -0.026, 0.086), radius=0.03,
            foot=(0.052, -0.036, 0.078), foot_r=(0.033, 0.039, 0.025))

    # the stalk and the big ball over the head
    k.add("stalk", tube([CROWN, CROWN + (0, 0.002, 0.04), TOP - (0, 0, TOP_R * 0.8)], [0.017, 0.016, 0.017]),
          STALK, "leaf")
    cotton(k, "top", TOP, TOP_R, "leaf", 3, lumps=8)
    # the lower balls, close against the body's lower sides, on the arms
    for sx, sd in ((1, 5), (-1, 9)):
        cotton(k, "low" + ("R" if sx > 0 else "L"), LOW * (sx, 1, 1), LOW_R, "arm" + ("R" if sx > 0 else "L"), sd,
               lumps=6)
    cotton_shade(k, TOP, TOP_R)
    cotton_shade(k, LOW, LOW_R)

    # three small leaves at the stalk's foot
    for i, (dx, dy, dz) in enumerate(((1, 0.1, 0.42), (-1, 0.1, 0.42), (0.15, 1, 0.55))):
        d = unit(dx, dy, dz)
        k.add(f"sprout{i}", outward(leaf(CROWN + (0, 0, 0.018) + d * 0.004, d, 0.052, 0.02, thick=0.011)), GREEN,
              "leaf", fuse=False, surface="leaf")

    hp.open_mouth(k, C, R, MOUTH_D, MOUTH_HALF, inside_col="#9A2238", tongue="#F08A9E")
    k.build(voxel=0.0055, smooth=4, band=0.006)

    look = hp.round_eye(fill=RED, rim=RIM, dark=RED_DARK)
    def eyes(state):
        hp.lay_eyes(k, C, R, EYE_D, EYE_HALF, look, BLUE, INK, state)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    jumpluff(*args())
