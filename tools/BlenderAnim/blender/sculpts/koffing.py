# Koffing sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/koffing.py -- <the export's Koffing.glb> <out.glb>
#
# From the references: a blue-purple floating ball studded with small low rounded bumps all over but the face; wide
# white eyes, their tops a flat heavy lid line, the small black pupils up against it, rolled up; a broad open grin, a
# flat top and a round bottom, pink within, a small white fang hanging at each corner; a cream skull-and-crossbones on
# the lower front (the skull a ring with a jaw and a notch between two teeth, the bones an X with knobbed ends); puffs
# of olive-yellow gas leaking from little vents round its rim.
# The skeleton: one body bone at (0, 0, 0.25).
# Weezing is grown from this script (sibling("koffing")): the ball, its bumps and vents, the eye, the grin, the mark.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, lumpy, eye_look, polygon, args

PURPLE, MARK, PINK, FANG = "#8A80BE", "#F2EEDC", "#E98A96", "#F6F4F2"
HOLE = "#4E4580"                   # the skull's hole and the notch between its teeth
WHITE, PUPIL, INK = "#F8F7F4", "#16121A", "#241C30"
GAS, GASLIGHT, GASDARK = "#BDB176", "#CDC38E", "#A99E64"


# --- the line's makes ---------------------------------------------------------------------------------------------
def ball(k, name, bone, C, R, colour=None, face=(0, -1, 0), clear=0.5, n=18, bump=0.2, seed=7):
    """The ball and its low round bumps spread evenly over it, none where the face looks out (within clear of face)."""
    colour = colour or PURPLE
    C = np.array(C, float)
    k.add(name, ell(C, (R, R, R), sub=5), colour, bone)
    f = unit(*face)
    rs = np.random.RandomState(seed)
    golden = math.pi * (3 - math.sqrt(5))
    m = 0
    for i in range(n * 3):
        z = 1 - 2 * (i + 0.5) / (n * 3)
        r = math.sqrt(1 - z * z)
        d = np.array([math.cos(golden * i) * r, math.sin(golden * i) * r, z])
        if d @ f > clear or (m % 2 and rs.rand() < 0.5):
            continue
        m += 1
        if m > n:
            break
        s = R * bump * (0.85 + 0.3 * rs.rand())
        c = C + d * R * 0.98
        # a low dome: flattened along its own direction
        a = math.degrees(math.atan2(d[1], d[0])); b = math.degrees(math.acos(np.clip(d[2], -1, 1)))
        k.add(f"{name}bump{m}", ell(c, (s, s, s * 0.75), rot=(0, b, a)), colour, bone)


def vent_puff(k, name, bone, C, R, d, size, colour=None, gap=0.006, seed=0):
    """A little vent standing out of the ball along d, and a puff of gas loose at its mouth."""
    d = unit(*d); C = np.array(C, float)
    base = C + d * R * 0.92
    top = C + d * (R + 0.024)
    k.add(f"{name}vent", tube([base, top], [0.026, 0.014], seg=14), PURPLE, bone)
    pc = C + d * (R + 0.024 + gap + size * 0.75)
    # a cloud of three soft balls: the puff and two smaller ones tucked into its sides, lighter on top
    side = np.cross(d, (0, 1.0, 0)); side = side / (np.linalg.norm(side) + 1e-9)
    rs = np.random.RandomState(seed)
    k.add(f"{name}puff", ell(pc, (size, size * 0.88, size * 0.92), sub=4), colour or GAS, bone, fuse=False)
    k.add(f"{name}puffa", ell(pc + side * size * 0.72 + d * size * 0.2, (size * 0.6,) * 3, sub=3), GASLIGHT, bone,
          fuse=False)
    k.add(f"{name}puffb", ell(pc - side * size * (0.6 + 0.2 * rs.rand()) - d * size * 0.05 + (0, 0.01, 0),
                              (size * 0.52,) * 3, sub=3), GASDARK, bone, fuse=False)


def frame(C, R, d, up_hint=(0, 0, 1.0)):
    """The point on the ball along d, with right and up in its tangent plane (right x up = outward)."""
    n = unit(*d)
    right = np.cross(up_hint, n); right /= np.linalg.norm(right)
    up = np.cross(n, right)
    return np.array(C, float) + n * R, n, right, up


def onto(C, R, p):
    """A point of a tangent plane brought onto the ball, and the ball's normal there."""
    q = np.array(p, float) - np.array(C, float)
    n = q / np.linalg.norm(q)
    return np.array(C, float) + n * R, n


def eye_open_lidded(top=0.45, pupil=(0.0, 0.0), pr=(0.22, 0.34), droop=0.12, band=0.16):
    """A wide white eye under a flat lid line (following lid_outline's), its pupil rolled up under the lid."""
    def look(u, v):
        c = np.tile(srgb(WHITE), (len(u), 1))
        lidline = top - droop * np.clip(u, 0, 1)
        rr = u * u + (np.minimum(v, 0) / 1.0) ** 2
        c[((u - pupil[0]) / pr[0]) ** 2 + ((v - pupil[1]) / pr[1]) ** 2 < 1] = srgb(PUPIL)
        c[rr > 0.84 ** 2] = srgb(INK)
        c[v > lidline - band] = srgb(INK)
        return c
    return look


def lid_outline(top=0.45, droop=0.12):
    """A flat-topped eye: the lid's line across the top (the outer corner a little lower), a round bottom."""
    pts = [(1.0, top - droop), (0.5, top - droop * 0.5), (0.0, top), (-0.5, top), (-1.0, top - 0.04)]
    for a in np.linspace(math.pi, 2 * math.pi, 13)[1:-1]:
        pts.append((math.cos(a), math.sin(a)))
    return pts


def eyes_on(k, state, C, R, d, half, look, top=0.45, droop=0.12, lid=None):
    """A pair of eyes on a ball along d (the +X one given), each u outward."""
    for s in (1, -1):
        at, n, right, up = frame(C, R, np.array(d) * (s, 1, 1))
        lk = eye_look(state, look, srgb(lid or PURPLE), srgb(INK), line=0.16)
        lk = lk if s > 0 else (lambda f: (lambda u, v: f(-u, v)))(lk)
        out = polygon([(s * u, v) for u, v in lid_outline(top, droop)])
        k.decal(at, n, half, lk, eps=0.004, outline=out, rings=20, segs=56)


def grin(k, C, R, d, half, fangs=0.74, colour=PINK):
    """A broad open grin: a flat top and a round bottom, a dark rim, a white fang hanging at each corner."""
    at, n, right, up = frame(C, R, d)
    dark = srgb("#7A2A3A"); pink = srgb(colour)
    def look(u, v):
        c = np.tile(pink, (len(u), 1))
        c[(u * u + np.minimum(v, 0) ** 2 > 0.86 ** 2) | (v > 0.42)] = dark
        w = smoothstep(-0.1, -0.7, v)[:, None] * 0.0
        return c * (1 - w)
    out = [(1.0, 0.55), (0.0, 0.58), (-1.0, 0.55)] + [(math.cos(a), math.sin(a)) for a in np.linspace(math.pi, 2 * math.pi, 15)[1:-1]]
    k.decal(at, n, half, look, eps=0.004, outline=polygon(out), rings=16, segs=60)
    for s in (1, -1):
        p, nn = onto(C, R, at + right * s * half[0] * fangs + up * half[1] * 0.3)
        k.decal(p, nn, (half[0] * 0.17, half[1] * 0.42), FANG, up=up, eps=0.0065,
                outline=polygon([(-1, 1), (1, 1), (0, -1)]), rings=6, segs=36)


def skull_mark(k, C, R, d, size, colour=MARK, hole=None):
    """The skull and crossbones on a ball along d, size about its width: the skull a bold ring with a jaw and a
    notched pair of teeth above a flat X of bones with knobbed ends, each shape a patch of its own; the bones some
    0.03 wide at a size of 0.155, the skull's hole and notch a dark purple."""
    hole = hole or HOLE
    at, n, right, up = frame(C, R, d)
    def patch(x, y, half, col, eps, outline=None, ang=0.0, segs=48, rings=8):
        p, nn = onto(C, R, at + right * x * size + up * y * size)
        u2 = math.cos(ang) * up - math.sin(ang) * right
        k.decal(p, nn, (half[0] * size, half[1] * size), col, up=u2, eps=eps, outline=outline, rings=rings, segs=segs)
    box = polygon([(1, 1), (-1, 1), (-1, -1), (1, -1)])
    BW, BL, BY, KN = 0.1, 0.42, -0.17, 0.1          # a bone's half width and half length, the X's middle, a knob
    # the bones: two bars crossing flat, and two knobs at each end
    for sgn in (1, -1):
        ang = math.radians(60 * sgn)
        patch(0, BY, (BW, BL), colour, 0.004, box, ang)
        for end in (1, -1):
            e = np.array([math.sin(-ang), math.cos(ang)]) * BL * end
            q = np.array([math.cos(ang), math.sin(ang)])
            for kn in (1, -1):
                c = e + q * KN * 0.95 * kn
                patch(c[0], BY + c[1], (KN, KN), colour, 0.004)
    # the skull: a disc with a jaw, its hole and the notch between its teeth dark
    patch(0, 0.28, (0.24, 0.24), colour, 0.0045)
    patch(0, 0.1, (0.15, 0.1), colour, 0.0045, box)
    patch(0, 0.31, (0.11, 0.11), hole, 0.0075)
    patch(0, 0.07, (0.028, 0.06), hole, 0.0075, box)


# --- Koffing ------------------------------------------------------------------------------------------------------
C = np.array([0.0, 0.0, 0.285])
R = 0.19
EYE_D, EYE_HALF = (0.36, -0.71, 0.6), (0.054, 0.038)
GRIN_D, GRIN_HALF = (0.0, -0.966, 0.26), (0.072, 0.044)
MARK_D, MARK_SIZE = (0.0, -0.933, -0.36), 0.155
PUFFS = [((0.78, 0.25, 0.58), 0.05), ((-0.8, 0.25, 0.55), 0.047), ((0.82, 0.3, -0.48), 0.045),
         ((-0.78, 0.3, -0.52), 0.048), ((0.25, 0.35, -0.9), 0.036), ((-0.15, 0.55, 0.82), 0.034)]

if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="shell")
    ball(k, "ball", "body", C, R, face=(0, -1, -0.2), clear=0.45)
    for i, (d, s) in enumerate(PUFFS):
        vent_puff(k, f"gas{i}", "body", C, R, d, s, seed=i)
    grin(k, C, R, GRIN_D, GRIN_HALF)
    skull_mark(k, C, R, MARK_D, MARK_SIZE)
    k.build(voxel=0.005, smooth=3)

    def eyes(state):
        eyes_on(k, state, C, R, EYE_D, EYE_HALF, eye_open_lidded(pupil=(0.0, 0.1)))

    k.eye_states(eyes)
    k.export(out)
