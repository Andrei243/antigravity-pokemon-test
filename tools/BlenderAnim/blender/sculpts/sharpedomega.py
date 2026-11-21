# Sharpedo-Mega sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/sharpedomega.py -- <the export's Sharpedo-Mega.glb> <out.glb>
#
# Grown from sharpedo.py (sibling("sharpedo"): its body, maw, teeth and fins). From the references: the head drawn out
# into a long pointed snout running forward and up past the open maw, the design's largest change: blue over its top,
# white under it, a yellow zigzag along its sides where the two meet, three white spikes in a row on its ridge; the lower
# jaw tipped in yellow; the eye red in a white ring and a black rim; four black gill bars with yellow between; yellow
# bars on the dorsal fin, a yellow streak along each pectoral fin, yellow bars on the white fin under the rear.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, hull, slab, srgb, inside, unit, eye_look, tri, sibling, args

sp = sibling("sharpedo")
cv = sp.cv
BLUE, WHITE, YELLOW, INK = sp.BLUE, sp.WHITE, sp.YELLOW, sp.INK
HC, HR = sp.HC, sp.HR

# The snout: an ellipsoid over the maw, tilted up toward its point and tapering to it
SC, SR, TILT = np.array([0.0, -0.32, 0.345]), np.array([0.09, 0.2, 0.066]), -22.0
T = math.tan(math.radians(-TILT))


def snout_mid(y):
    """The height of the snout's middle plane at y (its axis rises toward the front)."""
    return SC[2] - (y - SC[1]) * T


def snout(V):
    V = V.copy()
    t = np.clip(-V[:, 1], 0, 1)
    V[:, 0] *= 1 - 0.62 * t ** 1.3
    V[:, 2] *= 1 - 0.55 * t ** 1.3
    return V


def point_spike(k, name, base, d, length, width, color, bone):
    base, d = np.array(base, float), unit(*d)
    a = np.array([1.0, 0, 0]); a = a - d * (a @ d); a /= np.linalg.norm(a)
    b = np.cross(d, a)
    bb = base - d * 0.02
    pts = [bb + a * width / 2, bb - a * width / 2, bb + b * width * 0.4, bb - b * width * 0.4, base + d * length]
    k.add(name, hull(pts), color, bone, fuse=False, surface="shell")


def sharpedomega(src, out):
    k = Kit(src, coat="scales")
    sp.body(k)
    k.add("snout", ell(SC, SR, rot=(TILT, 0, 0), deform=snout), BLUE, "body")
    # white under the snout's middle plane, a yellow zigzag above it on its sides
    def under(P):
        return (inside(P[:, 2] - (snout_mid(P[:, 1]) + 0.016), 0.0025) * (P[:, 1] < -0.2)
                * (P[:, 2] > 0.25))
    k.paint(under, WHITE, on=["snout"])
    def zig(P):
        m = snout_mid(P[:, 1])
        h = 0.012 + 0.026 * (0.5 + 0.5 * tri((P[:, 1] + 0.2) / 0.07))
        return (inside(m + 0.016 - P[:, 2], 0.0025) * inside(P[:, 2] - (m + 0.02 + h), 0.0025) * (P[:, 1] < -0.14)
                * (P[:, 1] > -0.47) * (np.abs(P[:, 0]) > 0.025))
    k.paint(zig, YELLOW, on=["snout", "head"])
    # the lower jaw's tip in yellow
    k.paint(lambda P: inside(np.linalg.norm((P - np.array([0, -0.29, 0.135])) / np.array([0.08, 0.06, 0.05]), axis=1)
                             - 1, 0.003) * (P[:, 2] < 0.18), YELLOW)
    # three white spikes in a row on the snout's ridge, pointing forward and up
    for j, (y, ln) in enumerate(((-0.19, 0.1), (-0.3, 0.095), (-0.41, 0.08))):
        t = np.clip(-(y - SC[1]) / SR[1], 0, 1)
        top = snout_mid(y) + SR[2] * (1 - 0.55 * t ** 1.3) * 0.95
        point_spike(k, f"ridge{j}", (0, y, top), (0, -0.75, 0.66), ln, 0.066, WHITE, "body")

    sp.teeth(k)
    sp.fins(k)
    # yellow bars on the dorsal fin, a streak along each pectoral, bars on the white fin under the rear: plates thicker
    # than the fins, in each fin's own frame, so they show through both faces
    for j, bar in enumerate(([(-0.012, 0.03), (0.075, 0.03), (0.082, 0.05), (-0.003, 0.05)],
                             [(0.022, 0.075), (0.066, 0.075), (0.072, 0.095), (0.032, 0.095)])):
        k.add(f"dorsalbar{j}", slab(bar, np.array([0.0, 0.0, 0.352]), unit(0.6, 0.8, 0.12), (0.0, 0.0, 1.0), 0.034),
              YELLOW, "body", fuse=False, surface="scales")
    for s in (1, -1):
        side = 'R' if s > 0 else 'L'
        streak = [(0.03, 0.006), (0.16, -0.012), (0.17, -0.02), (0.03, -0.012)]
        k.add(f"pecbar{side}", slab(streak, np.array([0.092 * s, -0.06, 0.212]), unit(0.72 * s, 0.6, -0.34),
                                    (0.0, -0.15, 1.0), 0.034), YELLOW, f"fin{side}", fuse=False, surface="scales")
    for j, bar in enumerate(([(-0.02, -0.03), (0.04, -0.03), (0.042, -0.045), (-0.015, -0.045)],
                             [(0.0, -0.07), (0.035, -0.07), (0.03, -0.085), (0.008, -0.085)])):
        k.add(f"ventralbar{j}", slab(bar, np.array([0.0, 0.11, 0.2]), unit(0.6, 0.8, 0.0), (0.0, 0.0, 1.0), 0.034),
              YELLOW, "tail", fuse=False, surface="scales")

    # four black gill bars with yellow between, behind the eye
    CK, CB, CY = srgb(INK), srgb(BLUE), srgb(YELLOW)
    def gill(u, v):
        c = np.tile(CB, (len(u), 1))
        x = u + 0.4 * v * v
        c[(np.abs(v) < 0.8) & (x > -0.82) & (x < 0.82)] = CY
        for x0 in (-0.62, -0.2, 0.22, 0.64):
            c[(np.abs(x - x0) < 0.12) & (np.abs(v) < 0.86)] = CK
        return c
    for s in (1, -1):
        cv.side_patch(k, math.radians(70), HC[2] + 0.045, (0.05, 0.036), gill, side=s, c=HC, r=HR, rings=24,
                      segs=120)

    k.build(voxel=0.0045, smooth=3, band=0.004)

    # the eye: red in a white ring and a black rim
    CI, CW, CL = srgb(sp.IRIS), srgb(WHITE), srgb(sp.LIGHT)
    def eye_open(u, v):
        q = u * u + v * v
        c = np.tile(CI, (len(u), 1))
        c[q > 0.5 ** 2] = CW
        c[q > 0.76 ** 2] = CK
        return c
    def eyes(state):
        ink = CK if state in ("Open", "Fierce") else CL
        for s in (1, -1):
            cv.side_patch(k, sp.EYE_A, sp.EYE_Z, (0.03, 0.03), eye_look(state, eye_open, CB, ink, line=0.16), side=s,
                          c=HC, r=HR)
            if state in ("Open", "Fierce"):
                cv.side_patch(k, sp.EYE_A, sp.EYE_Z, (0.008, 0.008), INK, side=s, eps=0.0055, rings=8, segs=32, c=HC,
                              r=HR)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    sharpedomega(*args())
