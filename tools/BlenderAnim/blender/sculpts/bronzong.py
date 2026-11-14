# Bronzong sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/bronzong.py -- <the export's Bronzong.glb> <out.glb>
#
# Grown from Bronzor's script: the same bronze teals and metal coat, the same way of painting rings and lines in
# coordinates round the body's middle and of making an eye a patch with its rim and a slit of light.
# From the references: a great bronze bell (a dotaku), a straight-sided cone wider at its mouth than its top and open
# underneath, a pale thick ring round its top. On the top a flat crest: a loop with a round hole in its middle and a
# plate either side of it set with small round knobs. From the bell's shoulders two long flat bars of arms reach out
# sideways, sagging a little and rising at their ends, pale and banded with darker stripes, their ends dark. Its upper
# part pale with two slots like brows and a stripe down the middle; low on its front a pale panel holding two round red
# eyes (a dark rim, a dark ring inside, a dark pupil) and between them a row of three white teeth; its lower part a
# darker teal, its lip darker still. (The pictures put the eyes in the bell's lower half, not in the dome's upper part,
# and show no row of bosses on the bell itself: the round knobs are the crest's.)
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, eye_look, args

src, out = args()
k = Kit(src, coat="metal")

UPPER, BODY, LOWER, DARK, LINE = "#8CCFCB", "#56A3A6", "#45898E", "#2E6A70", "#23545A"
PANEL, SLOT, HOLLOW, TOOTH = "#A6D6D3", "#78BDBA", "#1C2A2C", "#F2F6F2"
ARM, BAND = "#93D2CF", "#4C979C"
E_ = 0.003                     # a crisp paint's edge, under half a voxel

Z0, Z1 = 0.16, 0.784           # where the straight side starts and ends
R0, R1 = 0.30, 0.22            # its radius at the mouth and at the top


def rb(z):
    """The bell's radius at a height (straight between the mouth and the top)."""
    return R0 + (R1 - R0) * np.clip((z - Z0) / (Z1 - Z0), 0, 1)


def lathe(profile, n=72):
    """A solid of revolution about the upright axis from (radius, height) pairs; a radius of 0 is a single point."""
    V, rings = [], []
    for r, z in profile:
        if r <= 0:
            rings.append([len(V)]); V.append((0.0, 0.0, z))
        else:
            ring = []
            for i in range(n):
                a = 2 * math.pi * i / n
                ring.append(len(V)); V.append((r * math.cos(a), r * math.sin(a), z))
            rings.append(ring)
    F = []
    for A, B in zip(rings, rings[1:]):
        if len(A) == 1:
            F += [[A[0], B[(i + 1) % n], B[i]] for i in range(n)]
        elif len(B) == 1:
            F += [[A[i], A[(i + 1) % n], B[0]] for i in range(n)]
        else:
            F += [[A[i], A[(i + 1) % n], B[(i + 1) % n], B[i]] for i in range(n)]
    return np.array(V, float), F


def torus(c, R, r, axis="z", n=64, m=16):
    a = np.linspace(0, 2 * math.pi, n, endpoint=False)
    b = np.linspace(0, 2 * math.pi, m, endpoint=False)
    A, Bb = np.meshgrid(a, b, indexing="ij")
    X, Y, Z = (R + r * np.cos(Bb)) * np.cos(A), (R + r * np.cos(Bb)) * np.sin(A), r * np.sin(Bb)
    V = np.stack([X, Y, Z] if axis == "z" else [X, Z, Y], -1).reshape(-1, 3) + np.array(c)
    F = []
    for i in range(n):
        for j in range(m):
            F.append([i * m + j, ((i + 1) % n) * m + j, ((i + 1) % n) * m + (j + 1) % m, i * m + (j + 1) % m])
    if axis != "z":
        F = [f[::-1] for f in F]
    return V, F


# --- the bell: a straight-sided cone, its top domed a little, a lip round its mouth; hollow underneath ----------
k.add("bell", lathe([(0, 0.122), (0.29, 0.122), (0.304, 0.135), (R0, Z0), (R1, Z1), (0.205, 0.805), (0.14, 0.838),
                     (0, 0.848)]), BODY, "body")
k.add("lip", torus((0, 0, 0.138), 0.293, 0.021), LOWER, "body")
k.add("hollow", lathe([(0, -0.05), (0.262, -0.05), (0.262, 0.15), (R0 + (R1 - R0) * (0.5 - Z0) / (Z1 - Z0) - 0.036, 0.5),
                       (0.16, 0.6), (0, 0.62)]), HOLLOW, "body", carve=True)
# the pale ring round its top
k.add("collar", torus((0, 0, 0.776), 0.218, 0.027), UPPER, "body")

# --- the crest on top: a loop with a round hole, a plate either side set with small round knobs ------------------
k.add("crestbase", ell((0, 0, 0.845), (0.19, 0.036, 0.045)), UPPER, "body")
k.add("loop", torus((0, 0, 0.9), 0.082, 0.034, axis="y"), UPPER, "body")
k.add("loopneck", ell((0, 0, 0.84), (0.07, 0.036, 0.045)), UPPER, "body")
for s in (1, -1):
    k.add(f"lobe{'R' if s > 0 else 'L'}", ell((0.15 * s, 0, 0.87), (0.085, 0.032, 0.062), rot=(0, -15 * s, 0)),
          UPPER, "body")
    for i, (x, z, r) in enumerate([(0.222, 0.88, 0.028), (0.18, 0.935, 0.026), (0.1, 0.985, 0.024)]):
        k.add(f"knob{'R' if s > 0 else 'L'}{i}", ell((x * s, 0, z), (r, 0.028, r)), UPPER, "body")

# --- the arms: long flat bars from the shoulders, sagging a little and rising at their ends -------------------
ARMPATH = [(0.2, 0.0, 0.6), (0.31, 0.0, 0.615), (0.44, 0.0, 0.612), (0.57, 0.0, 0.63), (0.69, 0.0, 0.665)]
k.add("arm", blade(ARMPATH, [0.058, 0.06, 0.06, 0.062, 0.066], (0, 1, 0), 0.42), ARM, "arm{s}", mirror=True)

# the arms' bands and dark ends, by the distance out from the middle
ARMBANDS = [(0.3, 0.33), (0.348, 0.366), (0.515, 0.55), (0.568, 0.586)]
def bands(P):
    ax = np.abs(P[:, 0])
    m = np.zeros(len(P))
    for a, b in ARMBANDS:
        m = np.maximum(m, inside(a - ax, E_) * inside(ax - b, E_))
    return m * (P[:, 2] > 0.5) * (ax > 0.27)
k.paint(bands, BAND)
k.paint(lambda P: inside(0.665 - np.abs(P[:, 0]), E_) * (P[:, 2] > 0.5), DARK)


# --- the bell's colours and its face, in coordinates round its axis (arc length round the side, height) ----------
def uz(P):
    ang = np.arctan2(P[:, 0], -P[:, 1])
    return ang * rb(P[:, 2]), P[:, 2]

def onbell(P):
    return (np.hypot(P[:, 0], P[:, 1]) > rb(P[:, 2]) - 0.012) & (P[:, 2] > 0.125) & (P[:, 2] < 0.8)

def rect(u0, u1, z0, z1, e=E_):
    def m(P):
        u, z = uz(P)
        return inside(u0 - u, e) * inside(u - u1, e) * inside(z0 - z, e) * inside(z - z1, e) * onbell(P)
    return m

def front(P):
    return P[:, 1] < 0

# lower part darker, the upper part pale all round, the lip darker still
k.paint(lambda P: inside(P[:, 2] - 0.3, E_) * onbell(P), LOWER)
k.paint(lambda P: inside(0.69 - P[:, 2], E_) * onbell(P), UPPER)
k.paint(lambda P: inside(np.abs(P[:, 2] - 0.69) - 0.005, E_) * onbell(P), LINE)
k.paint(lambda P: inside(P[:, 2] - 0.165, E_) * (P[:, 2] > 0.1) * (np.hypot(P[:, 0], P[:, 1]) > 0.27), DARK)

# the two slots like brows, dark-rimmed, and the stripe down the middle to the panel
for s in (1, -1):
    a, b = (0.04, 0.18) if s > 0 else (-0.18, -0.04)
    k.paint(lambda P, a=a, b=b: rect(a, b, 0.712, 0.762)(P) * front(P), LINE)
    k.paint(lambda P, a=a, b=b: rect(a + 0.008, b - 0.008, 0.72, 0.754)(P) * front(P), SLOT)
k.paint(lambda P: rect(-0.03, 0.03, 0.52, 0.79)(P) * front(P), LINE)
k.paint(lambda P: rect(-0.021, 0.021, 0.52, 0.79)(P) * front(P), UPPER)
# a little chevron where the stripe leaves the top
def chevron(P):
    u, z = uz(P)
    tip = 0.73 + 0.6 * np.abs(u)
    return inside(np.abs(z - tip) - 0.006, E_) * inside(np.abs(u) - 0.075, E_) * (z > 0.7) * onbell(P) * front(P)
# k.paint(chevron, LINE)

# the pale panel the eyes and the teeth sit in, dark-rimmed, its top stepped up in the middle
def panel(grow):
    def m(P):
        u, z = uz(P)
        top = 0.505 + 0.03 * inside(np.abs(u) - 0.07, 0.004)
        inn = inside(np.abs(u) - (0.205 + grow), E_) * inside(z - (top + grow), E_) * inside(0.335 - grow - z, E_)
        return inn * onbell(P) * front(P)
    return m
k.paint(panel(0.008), LINE)
k.paint(panel(0.0), PANEL)
k.paint(lambda P: rect(-0.021, 0.021, 0.52, 0.56)(P) * front(P), UPPER)

# the mouth: a dark slot with three white teeth
EZ = 0.42
k.paint(lambda P: rect(-0.062, 0.062, EZ - 0.022, EZ + 0.022)(P) * front(P), LINE)
for cu in (-0.037, 0.0, 0.037):
    k.paint(lambda P, cu=cu: rect(cu - 0.014, cu + 0.014, EZ - 0.014, EZ + 0.016, 0.002)(P) * front(P), TOOTH)

# --- the eyes: round, red in a dark rim, a darker ring inside, a dark pupil a little inward and low ---------------
RED, RIMC, RING, PUP, GLINT = srgb("#E2463E"), srgb("#6A2421"), srgb("#A22E2A"), srgb("#3A1A1A"), srgb("#FFD8D0")
def eye(u, v):
    r2 = u * u + v * v
    c = np.tile(RED, (len(u), 1))
    c[r2 > 0.78] = RIMC
    c[(r2 > 0.3) & (r2 < 0.42)] = RING
    c[(u + 0.06) ** 2 + (v + 0.05) ** 2 < 0.045] = PUP
    c[((u - 0.32) / 0.12) ** 2 + ((v - 0.38) / 0.1) ** 2 < 1] = GLINT
    return c
EU, ER = 0.118, 0.054
def eyes(state):
    for s in (1, -1):
        th = s * EU / rb(EZ)
        r = rb(EZ)
        e = np.array([r * math.sin(th), -r * math.cos(th), EZ])
        n = np.array([math.sin(th), -math.cos(th), (R0 - R1) / (Z1 - Z0)])
        look = eye_look(state, eye, srgb(PANEL), srgb(LINE), line=0.15)
        lk = look if s > 0 else (lambda f: (lambda u, v: f(-u, v)))(look)
        k.decal(e, n, (ER, ER), lk, eps=0.003, rings=20, segs=56)

k.build(voxel=0.0068, smooth=3)
k.eye_states(eyes)
k.export(out)
