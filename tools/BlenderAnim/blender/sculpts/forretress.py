# Forretress sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/forretress.py -- <the export's Forretress.glb> <out.glb>
# Grown from pineco.py's way of making things: hard pieces loose, the eyes patches on the fused body under the shell.
#
# The design, from the references: a ball of pale pinkish lilac shell, lumpy all over and pocked with craters, in two
# halves that part round its middle. Their edges are cut into flat-ended teeth, so the gap between them is wide round
# each eye and narrow between: in it the red body shows, and across the red a black band joins two round eyes (a
# black ring, white, a black pupil). Four red cannons come out of the gap, two straight out at the sides and two lower
# toward the back, each a tapering barrel with a bore at its end. The shell's edges show a greyer wall.
# The skeleton is one bone, the body, at z 0.25: everything is on it.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, srgb, inside, smoothstep, unit, eye_look, lathe, args

SHELL, WALL, PIT, RED, BORE = "#DDCBDA", "#A493A6", "#C4B0C2", "#D02C38", "#3A1418"
BLACK, WHITE = "#151417", "#F4F2EE"

C = np.array([0.0, 0.0, 0.25])          # the ball's middle, on the body bone
R = 0.24                                # the shell's radius
RIN = 0.198                             # the red body's, inside the gap
G_NOTCH, G_NARROW = 0.33, 0.135          # the gap's half height (radians of latitude) at a notch and between
# the notches round the middle: (azimuth from the front toward +X in degrees, half width of its flat)
NOTCHES = [(22, 13), (-22, 13), (60, 11), (-60, 11), (97, 12), (-97, 12), (136, 12), (-136, 12), (180, 12)]
RAMP = 6.0
EYE_AZ, EYE_R = 22.0, 0.046
CRATERS = [(40, 55), (-35, 48), (105, 42), (-115, 40), (165, 52), (0, 76), (68, -50), (-60, -46),
           (130, -42), (-150, -55), (-12, -38)]
rs = np.random.RandomState(205)
LUMPS = rs.normal(size=(46, 3)); LUMPS /= np.linalg.norm(LUMPS, axis=1, keepdims=True)
LUMP_AMP = 0.06 + 0.035 * rs.rand(len(LUMPS))


def dirs(points):
    return np.array([[math.cos(math.radians(la)) * math.sin(math.radians(az)),
                      -math.cos(math.radians(la)) * math.cos(math.radians(az)),
                      math.sin(math.radians(la))] for az, la in points])


CRATER_D = dirs(CRATERS)


def gap(az):
    """The gap's half height at azimuth az (degrees, an array)."""
    t = np.zeros_like(az)
    for c, h in NOTCHES:
        d = np.abs((az - c + 180) % 360 - 180)
        t = np.maximum(t, np.clip((h + RAMP - d) / RAMP, 0, 1))
    return G_NARROW + (G_NOTCH - G_NARROW) * t


def angles(U):
    az = np.degrees(np.arctan2(U[:, 0], -U[:, 1]))
    lat = np.arcsin(np.clip(U[:, 2], -1, 1))
    return az, lat


def shell_shape(V):
    """The unit sphere made the shell: lumps and craters on its halves, sunk to the red body in the gap."""
    U = V / np.linalg.norm(V, axis=1, keepdims=True)
    az, lat = angles(U)
    s = smoothstep(gap(az), gap(az) + 0.02, np.abs(lat))
    r = RIN / R + (1 - RIN / R) * s
    bump = np.zeros(len(U))
    for d, a in zip(LUMPS, LUMP_AMP):
        ang = np.arccos(np.clip(U @ d, -1, 1))
        bump = np.maximum(bump, a * np.exp(-(ang / 0.3) ** 2))
    for d in CRATER_D:
        ang = np.arccos(np.clip(U @ d, -1, 1))
        bump += -0.06 * np.exp(-(ang / 0.1) ** 2) + 0.022 * np.exp(-((ang - 0.14) / 0.04) ** 2)
    # the lumps fade toward the gap, so its walls stay clean
    edge = smoothstep(gap(az) + 0.02, gap(az) + 0.12, np.abs(lat))
    return U * (r + bump * edge)[:, None]


def rel(P):
    D = P - C
    rad = np.linalg.norm(D, axis=1)
    az, lat = angles(D / rad[:, None])
    return rad, az, lat


def cannon(k, name, az, tilt, reach):
    a, t = math.radians(az), math.radians(tilt)
    d = np.array([math.sin(a) * math.cos(t), -math.cos(a) * math.cos(t), math.sin(t)])
    prof = [(0, 0.15), (0.052, 0.15), (0.052, 0.205), (0.031, reach), (0.017, reach), (0.017, reach - 0.03),
            (0, reach - 0.03)]
    k.add(name, lathe(C, d, prof, seg=36), RED, "body", fuse=False, surface="shell")
    # the bore's dark floor
    k.add(name + "bore", lathe(C, d, [(0, reach - 0.031), (0.018, reach - 0.031), (0.018, reach - 0.012),
                                      (0, reach - 0.012)], seg=24), BORE, "body", fuse=False, surface="shell")


def eye_open(u, v):
    r = np.sqrt(u * u + v * v)
    c = np.tile(srgb(BLACK), (len(u), 1))
    c[(r > 0.42) & (r < 0.775)] = srgb(WHITE)
    return c


def eyes(k, state):
    for s in (1, -1):
        a = math.radians(s * EYE_AZ)
        n = np.array([math.sin(a), -math.cos(a), 0.0])
        look = eye_look(state, eye_open, srgb(RED), srgb(BLACK), line=0.17)
        lk = look if s > 0 else (lambda f: (lambda u, v: f(-u, v)))(look)
        k.decal(C + n * RIN, n, (EYE_R, EYE_R), lk, eps=0.003, rings=20, segs=56)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="shell")
    k.add("shell", ell(C, (R, R, R), deform=shell_shape, sub=6), SHELL, "body")
    # the craters' floors a shade darker
    for d in CRATER_D:
        k.paint(lambda P, d=d: inside(np.arccos(np.clip(((P - C) / np.linalg.norm(P - C, axis=1)[:, None]) @ d,
                                                        -1, 1)) - 0.085, 0.012), PIT)
    # the halves' edges, a greyer wall down to the red
    k.paint(lambda P: inside(np.abs(rel(P)[2]) - gap(rel(P)[1]) - 0.035, 0.004), WALL)
    k.paint(lambda P: inside(rel(P)[0] - (RIN + 0.009), 0.0025), RED)
    # the black band across the front that joins the eyes
    k.paint(lambda P: inside(np.abs(P[:, 2] - C[2]) - 0.0105, 0.002) * inside(rel(P)[0] - (RIN + 0.009), 0.0025)
            * inside(np.abs(rel(P)[1]) - 34, 0.5), BLACK)
    cannon(k, "cannonR", 97, 0, 0.335)
    cannon(k, "cannonL", -97, 0, 0.335)
    cannon(k, "cannonBR", 136, -18, 0.3)
    cannon(k, "cannonBL", -136, -18, 0.3)
    k.build(voxel=0.005, smooth=3)
    k.eye_states(lambda state: eyes(k, state))
    k.export(out)
