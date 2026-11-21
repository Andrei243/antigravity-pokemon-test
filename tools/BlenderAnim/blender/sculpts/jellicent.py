# Jellicent (the male) sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/jellicent.py -- <the export's Jellicent.glb> <out.glb>
#
# Frillish grown (its crown, its round eyes and its ribbon tentacles, from frillish.py). From the references: a great
# blue dome of a head under a small pale crown; round it at its middle a huge white puff like a moustache, two great
# lobes meeting at a cleft in front, greyer underneath; small round eyes close together on the dome above it (a navy
# rim, a white ring, a red middle); the blue of the head rounding out under the puff. Below, two broad tentacles held
# out to the sides ending in cloud-like paddles with pale rims, and three hanging down, curling, with white spots.
# The skeleton: body (0, 0, 0.529), head (0, 0, 0.669), tentacle0..4 round (0, 0, 0.399).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, srgb, inside, smoothstep, unit, eye_look, sibling, args

f = sibling("frillish")

BLUE, PUFF, SHADE, RIB, EDGE, SPOT = "#4AAEEA", "#EEF4FA", "#C8DAEC", "#3FA6E6", "#D2EBF8", "#EEF6FB"
CROWN, NAVY, RED, WHITE = "#DCEFF9", "#25337E", "#E5385A", "#FFFFFF"

D, DR = np.array([0.0, 0.01, 0.56]), np.array([0.19, 0.175, 0.19])
VOXEL = 0.0062
EYE_ARC, EYE_Z, EYE_HALF = 0.062, 0.672, (0.023, 0.028)
RING_Z = 0.5


def puff(k):
    """The moustache: a ring of fused balls round the head, two great lobes meeting at a cleft in front."""
    n = 28
    for i in range(n):
        th = 2 * math.pi * (i + 0.5) / n                       # 0 at the front
        front = math.cos(th)
        cleft = math.exp(-(th if th < math.pi else 2 * math.pi - th) ** 2 / 0.08)
        side = abs(math.sin(th))
        R = 0.19 + 0.03 * side - 0.035 * cleft
        r = 0.085 + 0.02 * side + 0.01 * max(front, 0) - 0.03 * cleft - 0.01 * max(-front, 0)
        c = np.array([R * math.sin(th), D[1] - R * math.cos(th), RING_Z + 0.008 * side])
        k.add(f"puff{i}_", ell(c, (r * 1.05, r * 1.05, r * 0.88)), PUFF, "body")


def jellicent(src, out):
    k = Kit(src, coat="fur")
    k.add("head", ell(D, DR), BLUE, "body")
    k.add("under", ell((0, 0.0, 0.415), (0.135, 0.125, 0.075)), BLUE, "body")
    puff(k)
    # greyer under the puff
    k.paint(lambda P: smoothstep(RING_Z + 0.01, RING_Z - 0.07, P[:, 2]) * 0.85, SHADE, on=["puff"])
    f.crown(k, np.array([0, 0.01, D[2] + DR[2] + 0.004]), 0.03, CROWN)

    # --- the tentacles -----------------------------------------------------------------------------------------------
    def paddle_w(t):
        w = 0.018 + 0.06 * smoothstep(0.1, 0.62, t)
        w *= 1 + 0.16 * math.sin(t * 6 * math.pi) * smoothstep(0.35, 0.7, t)
        return w * (math.sqrt(max(1 - ((t - 0.72) / 0.285) ** 2, 0)) if t > 0.72 else 1)

    def hang_w(t):
        return (0.022 + 0.034 * math.sin(math.pi * min(t / 0.8, 1) ** 0.75)) * (1 - smoothstep(0.62, 1.0, t) * 0.92)

    spots = []
    for s, bout, bfront in ((1, "tentacle3", "tentacle4"), (-1, "tentacle1", "tentacle0")):
        m = np.array([s, 1, 1.0])
        arm = [(0.04, 0.0, 0.39), (0.11, -0.03, 0.33), (0.18, -0.07, 0.29), (0.235, -0.1, 0.275), (0.265, -0.11, 0.28)]
        f.tentacle(k, f"arm{s}", bout, [np.array(p) * m for p in arm], paddle_w, unit(-0.25 * s, -0.5, 0.83), RIB, EDGE,
                   thick=0.024)
        hang = [(0.035, -0.035, 0.38), (0.07, -0.07, 0.28), (0.1, -0.08, 0.18), (0.1, -0.07, 0.09), (0.07, -0.055, 0.04)]
        spots.append((bfront, f.tentacle(k, f"hang{s}", bfront, [np.array(p) * m for p in hang], hang_w,
                                         unit(0.4 * s, -0.92, 0.0), RIB, EDGE, thick=0.024)))
    mid = [(0.0, 0.0, 0.38), (0.0, -0.02, 0.28), (0.01, -0.025, 0.17), (0.0, -0.015, 0.08), (-0.02, -0.01, 0.035)]
    spots.append(("tentacle2", f.tentacle(k, "mid", "tentacle2", mid, hang_w, unit(0, -1, 0), RIB, EDGE, thick=0.024)))
    # white spots through the hanging tentacles, showing on both faces
    for bone, (P, side, face, W, t) in spots:
        for tt, off in ((0.3, 0.0), (0.58, 0.0)):
            i = int(np.argmin(np.abs(t - tt)))
            T = P[min(i + 1, len(P) - 1)] - P[max(i - 1, 0)]; T /= np.linalg.norm(T)
            a = 0.42 * W[i] * 0.72
            V, F = ell((0, 0, 0), (a, a * 1.5, 0.017), sub=3)
            V = V @ np.stack([side[i], T, face[i]], axis=0) + P[i] + side[i] * off
            k.add("spot", (V, F), SPOT, bone, fuse=False, surface="skin")

    k.build(voxel=VOXEL, smooth=6, band=0.004)

    # --- the eyes: small and round, close together on the dome over the puff ------------------------------------------
    look_open = f.round_eye(NAVY, RED, RED, WHITE, ring=WHITE)

    def eyes(state):
        for side in (1, -1):
            d = f.head_dir(EYE_ARC * side, EYE_Z, D, DR)
            look = eye_look(state, look_open, srgb(BLUE), srgb(NAVY), line=0.17)
            col = look if side == 1 else (lambda u, v, g=look: g(-u, v))
            k.decal(f.on_ell(D, DR, d), d, EYE_HALF, col, eps=0.0035, rings=20, segs=56)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    jellicent(*args())
