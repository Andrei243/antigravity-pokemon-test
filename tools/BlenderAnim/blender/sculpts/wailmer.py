# Wailmer sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/wailmer.py -- <the export's Wailmer.glb> <out.glb>
#
# From the references: a round ball of a whale, cream below with a few curved seams running down from the front, and
# over its top a blue cap that overhangs the front a little and slopes down toward the back. Under the cap's front edge
# a broad grin: a white band, straight along the cap and curved below like a smile, split by dark lines. Tiny dark eyes
# high on the cap's sides, two blowholes on top. Two broad blue flippers out of the sides, swept back and down, with
# grooves along them. No tail is seen (a low blue hump behind).
#
# Written to be grown from (Wailord: sibling("wailmer")): the grin, the seams, the eyes, the flipper and the cap's
# line are the functions and constants below.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, srgb, inside, unit, eye_look, args

BLUE, CREAM, SEAM, WHITE, LINEC, INK, GROOVE, LIGHT = ("#2E68B4", "#F2E2AE", "#C4A676", "#F6F6F2", "#3A4250",
                                                       "#16191E", "#1F4C88", "#9DB8E6")

C = np.array([0.0, 0.0, 0.236])
R = np.array([0.214, 0.2, 0.202])
BALL_C = np.array([0.0, 0.0, 0.228])
CAP_C, CAP_R = np.array([0.0, 0.03, 0.28]), np.array([0.178, 0.212, 0.14])


def cap_z(P, front=0.058, slope=0.24):
    """The cap's lower edge: level across the front, sloping down toward the back."""
    return C[2] + front - slope * np.clip(P[:, 1] + R[1], 0, None)


def frame(c, d, w, r):
    """An ellipsoid laid along d (radius r[0]), w across it (r[1]) and thin square to both (r[2])."""
    d = unit(*d); w = np.array(w, float); w = w - d * (w @ d); w /= np.linalg.norm(w)
    n = np.cross(d, w)
    V, F = ell((0, 0, 0), (1, 1, 1), sub=4)
    V = V * np.array(r)
    return V @ np.stack([d, w, n]) + np.array(c, float), F, (d, w, n)


def flipper(k, root, d, w, r, bone, name="flipper", grooves=3):
    """A broad fused flipper with dark grooves down its length (painted in its own frame)."""
    c = np.array(root, float) + unit(*d) * r[0] * 0.85
    V, F, (dd, ww, nn) = frame(c, d, w, r)
    k.add(name, (V, F), BLUE, bone)
    def groove(P):
        q = P - c
        a, b = q @ dd / r[0], q @ ww / r[1]
        m = np.zeros(len(P))
        for g in np.linspace(-0.45, 0.45, grooves):
            m = np.maximum(m, inside(np.abs(b - g * (0.6 + 0.4 * a)) * r[1] - 0.0035, 0.002) * (a > -0.05) * (a < 0.85))
        return m
    k.paint(groove, GROOVE, on=[name])


def body(k):
    k.add("ball", ell(BALL_C, R, sub=5), CREAM, "body")
    k.add("cap", ell(CAP_C, CAP_R, sub=5), BLUE, "body")
    k.add("hump", ell((0.0, 0.15, 0.262), (0.09, 0.07, 0.075)), BLUE, "tail")
    k.paint(lambda P: inside(cap_z(P) - P[:, 2], 0.0022), BLUE)
    k.paint(lambda P: inside(P[:, 2] - cap_z(P), 0.0022), CREAM, on=["ball", "cap"])


def seams(k, c=C, at=(-62, -30, 0, 30, 62), z_top=None, width=0.0034):
    """Curved seams down the cream front, each bending toward the middle as it goes down."""
    def m(P):
        a = np.degrees(np.arctan2(P[:, 0] - c[0], -(P[:, 1] - c[1])))
        rxy = np.hypot(P[:, 0] - c[0], P[:, 1] - c[1])
        t = np.clip((cap_z(P) - P[:, 2]) / 0.3, 0, 1)
        out = np.zeros(len(P))
        for a0 in at:
            a_eff = a0 * (1 - 0.35 * t) + 8 * np.sign(a0) * t * 0
            out = np.maximum(out, inside(np.abs(np.radians(a - a_eff)) * rxy - width, 0.002))
        return out * (P[:, 2] < cap_z(P) - 0.03) * (P[:, 1] < c[1] + 0.05)
    k.paint(m, SEAM)


CW, CK, CLN, CB, CL = srgb(WHITE), srgb(INK), srgb(LINEC), srgb(BLUE), srgb(LIGHT)


def grin(k, centre, normal, half, lines=7, bg=CREAM):
    """The white band under the cap: straight along its top, a smile below, split by dark lines."""
    cb = srgb(bg)
    def look(u, v):
        c = np.tile(cb, (len(u), 1))
        band = v > -0.95 + 1.75 * u * u
        c[band] = CW
        for g in np.linspace(-0.8, 0.8, lines):
            c[band & (np.abs(u - g) < 0.042) & (v < 0.86)] = CLN
        return c
    k.decal(centre, normal, half, look, eps=0.004, rings=18, segs=180)


def blowholes(k, spots):
    for p, n in spots:
        k.decal(p, n, (0.0075, 0.0075), INK, up=(0, -1, 0), eps=0.004, rings=6, segs=24)


def eye_open(u, v):
    c = np.tile(CK, (len(u), 1))
    c[((u + 0.25) / 0.32) ** 2 + ((v - 0.3) / 0.32) ** 2 < 1] = CW       # a glint high toward the front
    return c


def eyes(k, state, centre, normal, half):
    ink = CK if state in ("Open", "Fierce") else CL
    look = eye_look(state, eye_open, CB, ink, line=0.18)
    k.decal(centre, normal, half, look, eps=0.0045, rings=20, segs=56, mirror=True)


def on_cap(a_deg, z):
    """The cap's surface at an angle round the upright (nought straight ahead) and a height, and its normal."""
    a = math.radians(a_deg)
    s = (z - CAP_C[2]) / CAP_R[2]
    ca = math.sqrt(max(1 - s * s, 0))
    d = np.array([math.sin(a) * ca, -math.cos(a) * ca, s])
    n = d / CAP_R; n /= np.linalg.norm(n)
    return CAP_C + d * CAP_R, n


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="scales")
    body(k)
    for s, bone in ((1, "finR"), (-1, "finL")):
        flipper(k, (0.185 * s, -0.04, 0.176), (0.72 * s, 0.5, -0.3), (0.0, 0.5, 1.0) if s > 0 else (0.0, 0.5, 1.0),
                (0.13, 0.075, 0.02), bone, name="flipper" + ("R" if s > 0 else "L"))
    seams(k)
    grin(k, (0.0, -0.196, C[2] + 0.034), unit(0, -1, 0.14), (0.118, 0.03))
    blowholes(k, [on_cap(a, CAP_C[2] + 0.132) for a in (-14, 14)])
    k.build(voxel=0.005, smooth=3, band=0.004)
    p, n = on_cap(44, C[2] + 0.108)
    k.eye_states(lambda state: eyes(k, state, p, n, (0.016, 0.02)))
    k.export(out)
