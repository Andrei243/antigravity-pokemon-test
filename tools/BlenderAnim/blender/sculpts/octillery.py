# Octillery sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/octillery.py -- <the export's Octillery.glb> <out.glb>
#
# From the references: Remoraid grown into a red octopus, squat. A great round head, over half the height, resting on
# a skirt of eight thick tentacles; a tubular muzzle like a cannon's, red like the body with a dark bore, aimed up and forward
# from the front of the head; half-lidded eyes on the sides of the head (a heavy red lid over the top of a white eye,
# a dark pupil low in it, a dark rim; made as Remoraid's are); three yellow bumps on the top of the head; the
# tentacles curl up and in at their ends, large round yellow suckers on their outer faces. The tentacles are kept close
# round the body (the drawing spreads them wider), so the body is not drawn small in the game.
# The export's joints: body (0, 0, 0.34); tentacle0..7 in a ring of 0.08 at z 0.2 (0 front right, 7 front left).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, eye_look, args

src, out = args()
k = Kit(src, coat="scales")

RED, YELLOW, INK, BORE, WHITE = "#DE3A34", "#F6CC3C", "#1A1012", "#2A0E10", "#F8F4EA"


def oriented(c, r, n):
    """An ellipsoid of radii r whose local z is the direction n."""
    n = np.array(n, float); n /= np.linalg.norm(n)
    a = np.cross(n, (0, 0, 1.0)) if abs(n[2]) < 0.95 else np.cross(n, (1.0, 0, 0))
    a /= np.linalg.norm(a); b = np.cross(n, a)
    V, F = ell((0, 0, 0), r, sub=3)
    return V @ np.stack([a, b, n], axis=0) + np.array(c, float), F


# --- the head, and the mass under it that spreads into the tentacles ------------------------------------------------
H = np.array([0.0, 0.02, 0.34])
HR = np.array([0.185, 0.18, 0.17])
k.add("head", ell(H, HR), RED, "body")
k.add("skirt", ell((0, 0.01, 0.2), (0.15, 0.15, 0.07)), RED, "body")

# --- the muzzle: a red tube aimed up and forward, with a dark bore -------------------------------------------------
E = np.array([0.0, -0.255, 0.468])
AIM = unit(0, -0.5, 0.86)
k.add("muzzle", tube([(0, -0.12, 0.31), (0, -0.205, 0.385), E], [0.085, 0.06, 0.052]), RED, "body")
k.add("muzzlelip", oriented(E + AIM * 0.012, (0.058, 0.058, 0.03), AIM), RED, "body")
k.add("bore", tube([E - AIM * 0.03, E + AIM * 0.09], [0.028, 0.028], seg=24), BORE, "body", carve=True)

# --- the tentacles: out and down from the ring of joints, along the ground, then curling up and in ---------------
JOINTS = [(0.031, -0.054), (0.074, -0.011), (0.074, 0.051), (0.031, 0.094), (-0.031, 0.094), (-0.074, 0.051),
          (-0.074, -0.011), (-0.031, -0.054)]
for i, (jx, jy) in enumerate(JOINTS):
    o = np.array([jx, jy - 0.02, 0.0]); o /= np.linalg.norm(o)
    j = np.array([jx, jy, 0.0])
    z = np.array([0, 0, 1.0])
    back = jy > 0.06                          # the back ones curl a little tighter
    reach = 0.25 if back else 0.27
    p = [j * 0.6 + z * 0.22, j + o * 0.06 + z * 0.12, o * 0.17 + z * 0.052, o * (reach - 0.045) + z * 0.046,
         o * reach + z * 0.085, o * (reach + 0.018) + z * 0.128, o * (reach + 0.002) + z * 0.168]
    k.add(f"tentacle{i}", tube(p, [0.052, 0.05, 0.042, 0.035, 0.028, 0.021, 0.013]), RED, f"tentacle{i}")
    # Round yellow suckers on the curl's outer face, each about the tentacle's width
    cc = o * (reach - 0.04) + z * 0.118
    for q, r in ((p[3], 0.035), (p[4], 0.028), (p[5], 0.021)):
        nrm = q - cc; nrm /= np.linalg.norm(nrm)
        k.add(f"sucker{i}", oriented(q + nrm * r * 0.85, (r * 0.95, r * 0.95, r * 0.3), nrm), YELLOW,
              f"tentacle{i}", fuse=False)

# --- small yellow bumps on the top of the head ---------------------------------------------------------------------
for d in (unit(0.05, -0.4, 0.92), unit(0.6, 0.22, 0.77), unit(-0.6, 0.22, 0.77)):
    k.add("bump", oriented(H + d * HR * 0.98, (0.04, 0.04, 0.017), d), YELLOW, "body", fuse=False)

# --- the eyes: half-lidded, a heavy red lid over a white eye, a dark pupil low in it, a dark rim -------------------
CR, CK, CW = srgb(RED), srgb(INK), srgb(WHITE)
LID = 0.18                                        # the lid's lower edge, in v
def eye(u, v):
    r2 = u * u + v * v
    c = np.tile(CW, (len(u), 1))
    c[((u + 0.1) / 0.3) ** 2 + ((v + 0.3) / 0.34) ** 2 < 1] = CK
    c[r2 > 0.84 ** 2] = CK
    edge = LID - 0.3 * u                         # higher at the inner corner: bored, not cross
    c[v > edge + 0.15] = CR
    c[np.abs(v - edge - 0.07) < 0.09] = CK
    c[r2 > 1.0] = CR
    return c
ED = unit(0.72, -0.6, 0.3)
EC = H + ED * HR * 0.97
def eyes(state):
    k.decal(EC, ED, (0.056, 0.056), eye_look(state, eye, CR, CK, line=0.16), eps=0.005, rings=20, segs=56,
            mirror=True)

k.build(voxel=0.006, smooth=4)
k.eye_states(eyes)
k.export(out)
