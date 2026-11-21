# Whirlipede sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/whirlipede.py -- <the export's Whirlipede.glb> <out.glb>
#
# Venipede curled into its shell (venipede.py's cones with dark bands, its half-moon eye): from the references, a
# grey-lilac wheel rolling forward, its axle across (X). Each side a flat hub: a grey disc in six sectors round a black
# hexagonal hole in which a yellow-orange eye peers out under a black lid. The tread is cut into segments by dark lines
# running from hub to hub; red rings with a lilac middle and short triangular lilac spikes, hooked back, are scattered
# over it. Two long antennae out of the upper front, lilac with dark grey stripes, reaching forward and up; one short
# striped spike out of the back, pointing back and a little down.
# Skeleton: body (0, 0, 0.29), head (0, -0.1, 0.29), tail (0, 0.18, 0.39).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, hull, srgb, inside, unit, eye_look, polygon, args, sibling

vp = sibling("venipede")
TREAD, SEG, RING, HUB, HUBDARK = "#B2A4C4", "#6E6280", "#D2445E", "#85828E", "#5A5764",
BLACK, YELLOW, STRIPE = "#1A181E", vp.YELLOW, "#5E5A66"

C = np.array([0.0, 0.0, 0.29])
R = np.array([0.235, 0.29, 0.29])
FLAT = 0.8                        # the hubs' flats, at this share of the half width
HUB_R = 0.17                      # the hub's radius on the flat


def wheel(V):
    V = V.copy()
    V[:, 0] = np.clip(V[:, 0], -FLAT, FLAT)
    return V


def on_tread(phi, a):
    """A point of the tread and its normal: phi round the axle from the front (0) over the top (90), a across."""
    p = math.radians(phi)
    return vp.on_ell(C, R * np.array([1.0, 1, 1]), (a, -math.cos(p), math.sin(p)))


def angle_round(P):
    return np.degrees(np.arctan2(P[:, 2] - C[2], -(P[:, 1] - C[1])))


SPIKES = [(18, 0.32), (62, -0.3), (104, 0.3), (146, -0.32), (196, 0.3), (214, -0.3),(318, 0.34), (340, -0.3),
          (84, -0.62), (170, 0.62), (222, -0.6), (6, 0.62)]
RINGS = [(40, -0.42), (125, 0.4), (215, -0.45), (300, 0.42), (5, -0.5), (170, -0.1), (90, 0.62)]


def body(k):
    k.add("wheel", ell(C, R, deform=wheel, sub=5), TREAD, "body")
    # the segment lines, hub to hub
    for a0 in range(0, 360, 36):
        def m(P, a0=a0):
            d = (angle_round(P) - a0 + 180) % 360 - 180
            r = np.hypot(P[:, 1] - C[1], P[:, 2] - C[2])
            return inside(np.abs(np.radians(d)) * r - 0.0045, 0.002) * (np.abs(P[:, 0]) < R[0] * FLAT - 0.006)
        k.paint(m, SEG)


def marks(k):
    CT, CR = srgb(TREAD), srgb(RING)
    def ring(u, v):
        r = np.sqrt(u * u + v * v)
        c = np.tile(CT, (len(u), 1))
        c[(r > 0.42) & (r < 0.92)] = CR
        return c
    for phi, a in RINGS:
        p, n = on_tread(phi, a)
        k.decal(p, n, (0.056, 0.056), ring, up=(1, 0, 0) if abs(n[2]) > 0.8 else (0, 0, 1), eps=0.004, rings=16, segs=48)
    # the hubs: six grey sectors round a black hexagon
    CH, CD, CB = srgb(HUB), srgb(HUBDARK), srgb(BLACK)
    def hub(u, v):
        r = np.sqrt(u * u + v * v)
        th = np.arctan2(v, u)
        hexr = r * np.cos(((th + math.pi / 6) % (math.pi / 3)) - math.pi / 6)
        c = np.tile(CH, (len(u), 1))
        sector = (np.floor((th + math.pi) / (math.pi / 3)) % 2) == 0
        c[sector] = CD * 0.35 + CH * 0.65
        line = np.abs(np.sin(3 * th)) * r < 0.035
        c[line & (r > 0.3)] = CD
        c[r > 0.9] = CD
        c[hexr < 0.3] = CB
        return c
    for s in (1, -1):
        k.decal(C + np.array((s * R[0] * FLAT, 0, 0)), (s, 0, 0), (HUB_R, HUB_R), hub, eps=0.004, rings=28, segs=84)
        # the hexagonal hole, a patch of its own so its edges are straight
        hexa = polygon([(math.cos(math.radians(30 + 60 * i)), math.sin(math.radians(30 + 60 * i))) for i in range(6)])
        k.decal(C + np.array((s * R[0] * FLAT, 0, 0)), (s, 0, 0), (HUB_R * 0.42, HUB_R * 0.42), BLACK, eps=0.0055,
                outline=hexa, rings=8, segs=60)


def pieces(k):
    for i, (phi, a) in enumerate(SPIKES):
        p, n = on_tread(phi, a)
        p = p - n * 0.01
        back = np.array((0, math.sin(math.radians(phi)), math.cos(math.radians(phi))))   # along the roll, backward
        side = np.cross(n, back); side /= np.linalg.norm(side)
        tip = p + n * 0.095 + back * 0.04
        pts = [p + back * 0.042, p - back * 0.038, p + side * 0.022, p - side * 0.022, tip]
        k.add(f"spike{i}", hull(pts), TREAD, "body", fuse=False, surface="shell")
    bands = ((0.16, 0.26), (0.38, 0.48), (0.6, 0.7), (0.8, 0.88))
    for s in (1, -1):
        vp.antenna(k, f"ant{'R' if s > 0 else 'L'}", (0.075 * s, -0.215, 0.43), unit(0.22 * s, -0.78, 0.58), 0.3,
                   0.03, "head", color=TREAD, band=STRIPE, bands=bands)
    vp.antenna(k, "tailspike", (0.0, 0.25, 0.2), unit(0.0, 0.88, -0.3), 0.19, 0.03, "tail", color=TREAD, band=STRIPE,
               bands=((0.3, 0.42), (0.56, 0.68)))


# ---- the eye: yellow-orange under a black lid, low and forward in the hexagon (u runs to the back on both sides)
CY, CB = srgb(YELLOW), srgb(BLACK)
def eye_open(u, v):
    c = np.tile(CY, (len(u), 1))
    c[v > 0.32 - 0.25 * u] = CB                                      # the lid, lower toward the front
    c[((u + 0.25) / 0.2) ** 2 + ((v + 0.05) / 0.5) ** 2 < 1] = CB      # the pupil, toward the front
    c[(u / 1.0) ** 2 + (v / 1.0) ** 2 > 0.8] = CB
    return c


def eyes(k, state):
    look = eye_look(state, eye_open, CB, CY, line=0.17)
    k.decal(C + np.array((R[0] * FLAT, -0.004, -0.008)), (1, 0, 0), (0.042, 0.036), look, eps=0.007, rings=20,
            segs=56, mirror=True)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="shell")
    body(k)
    marks(k)
    pieces(k)
    k.build(voxel=0.006, smooth=3, band=0.004)
    k.eye_states(lambda state: eyes(k, state))
    k.export(out)
