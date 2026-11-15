# Rotom-Mow sculpted from nothing on the skeleton and clips of the game's own export of the form (sculptkit.py):
#   blender --background --factory-startup --python sculpts/rotommow.py -- <the export's Rotom-Mow.glb> <out.glb>
#
# The design, from the references: Rotom in a lawn mower. A low orange deck, open and black on top, with two big dark
# wheels with orange hubs at its front corners; at its front in the middle Rotom's own head, a round orange mound
# whose top runs up into a cone and on into a tall thin handle that ends in a flat oval top; on the head two large
# eyes (a white ring round a green iris) and a wide grin of white zigzag teeth. A green aura rims everything, and a
# jagged green bolt hangs under the head. Grown from rotom.py: the aura is opaque green set a little behind the
# orange (a sleeve round the deck's back end, a tube behind the handle with the handle again behind it, a disc
# under the top), and the eyes are the same patch.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, unit, eye_look, args, slab, lathe, outward, boxy, tri

src, out = args()
k = Kit(src, coat="shell")

ORANGE, AURA, DARK, TYRE = "#F27028", "#86C43E", "#2A2A2E", "#26262A"
WHITE, IRIS, PUPIL, INK, TEETH, GUM = "#FFFFFF", "#8CD43C", "#2F6E12", "#24301A", "#F4F4F0", "#4A4A50"

# --- the deck: a low rounded box, its open top black (fused, with the head) -----------------------------------
DC, DR = np.array([0.0, 0.035, 0.3]), np.array([0.17, 0.125, 0.088])
k.add("deck", ell(DC, DR, deform=boxy(5), sub=5), ORANGE, "body")
TOP = DC[2] + DR[2]
# the open top: a hollow carved into the deck behind the head, lined dark
k.add("deckopen", ell((0, 0.04, TOP + 0.004), (0.132, 0.062, 0.034), deform=boxy(4)), DARK, "body", carve=True)

# --- the head: a round mound at the deck's front, its top running up into the handle ---------------------------
HC, HR = np.array([0.0, -0.1, 0.29]), np.array([0.128, 0.092, 0.102])
k.add("head", ell(HC, HR, sub=5), ORANGE, "body")
STEM = [HC + np.array([0, 0.005, 0.07]), HC + np.array([0, 0.03, 0.15]), np.array([0, -0.045, 0.5]),
        np.array([0, -0.04, 0.6])]
k.add("cone", tube(STEM, [0.07, 0.026, 0.016, 0.016]), ORANGE, "body")
TC = np.array([0.0, -0.04, 0.612])


def oval(V, F, c, sy):
    V = V.copy(); V[:, 1] = c[1] + (V[:, 1] - c[1]) * sy
    return V, F


def disc(c, r, thick, d=(0, 0, 1), seg=56, edge=0.0):
    """A disc on the axis d, its rim rounded by edge."""
    if edge:
        prof = [(0, -thick / 2), (r - edge, -thick / 2), (r, 0), (r - edge, thick / 2), (0, thick / 2)]
    else:
        prof = [(0, -thick / 2), (r, -thick / 2), (r, thick / 2), (0, thick / 2)]
    return lathe(c, d, prof, seg=seg)


k.add("top", oval(*disc(TC, 0.1, 0.026, edge=0.01), TC, 0.62), ORANGE, "body", fuse=False)

# --- the wheels: dark drums with an orange hub on the outside --------------------------------------------------
WC, WR, WW = np.array([0.183, -0.055, 0.215]), 0.088, 0.062
k.add("wheel", lathe(WC, (1, 0, 0), [(0, -WW / 2), (WR - 0.012, -WW / 2), (WR, -WW / 2 + 0.012),
                                     (WR, WW / 2 - 0.012), (WR - 0.012, WW / 2), (0, WW / 2)]),
      TYRE, "body", fuse=False, mirror=True)
k.add("hub", lathe(WC + np.array([WW / 2 - 0.004, 0, 0]), (1, 0, 0),
                   [(0, 0), (0.044, 0), (0.044, 0.006), (0.036, 0.012), (0, 0.012)]),
      ORANGE, "body", fuse=False, mirror=True)

# --- the aura: as rotom.py's, opaque green set a little BEHIND the orange, so it rims it from the front ---------
# The handle's a thicker tube set back, with the handle again behind it (rotom.py's way of
# showing the core through from behind); the top's a disc a little larger and thinner. (A disc behind each wheel
# was a green bar down its side from the front, edge-on: the wheels have none.)
BACK = 0.022


# The deck's aura is a solid plate of the deck's own rounded section, a little larger, round its back half: the
# deck's back end stands out of it behind, its front half before it (a ring with air between showed as a strap)
k.add("auradeck", outward(ell(DC + np.array([0, 0.094, 0]), (DR[0] * 0.9 + 0.016, 0.016, DR[2] * 0.9 + 0.016),
                              deform=boxy(5), sub=5)), AURA, "body", fuse=False, surface="glow")
AB = np.array([0, BACK, 0])
SA = [p + AB for p in STEM[1:]]
k.add("aurastem", outward(tube(SA, [0.036, 0.027, 0.027])), AURA, "body", fuse=False, surface="glow")
V, F = tube(STEM[1:], [0.026, 0.016, 0.016])
V = V.copy(); V[:, 1] += 2 * BACK          # the handle again, further back, showing through from behind
k.add("backstem", outward((V, F)), ORANGE, "body", fuse=False)
k.add("auratop", outward(oval(*disc(TC, 0.116, 0.012), TC, 0.66)), AURA, "body", fuse=False, surface="glow")


# --- the bolt under the head: a jagged flat plate, its face to the front ---------------------------------------
def offset_outline(line, widths, longest=3.0):
    """A closed outline round a polyline, each point its half-width, its corners mitred to points (rotom.py's)."""
    line = np.array(line, float)
    n = len(line)
    seg = [(line[i + 1] - line[i]) / np.linalg.norm(line[i + 1] - line[i]) for i in range(n - 1)]
    left = lambda t: np.array([-t[1], t[0]])
    L, Rr = [], []
    for i in range(n):
        w = widths[i]
        if i == 0 or i == n - 1:
            t = seg[0] if i == 0 else seg[-1]
            m = left(t); kk = 1.0
            p = line[i] + (-t if i == 0 else t) * w * 0.3
        else:
            m = left(seg[i - 1]) + left(seg[i]); m /= np.linalg.norm(m)
            kk = min(1 / max(np.dot(m, left(seg[i])), 1e-3), longest)
            p = line[i]
        L.append(p + m * w * kk); Rr.append(p - m * w * kk)
    return [tuple(p) for p in L + Rr[::-1]]


BOLT = [(0.0, 0.0), (0.035, -0.05), (-0.008, -0.062), (0.06, -0.118), (0.026, -0.124), (0.1, -0.175)]
bolt = offset_outline([(x * 0.75, z * 0.75) for x, z in BOLT], [0.022, 0.018, 0.016, 0.015, 0.012, 0.003], longest=2.6)
k.add("bolt", slab(bolt, np.array([0.0, -0.04, 0.215]), (1, 0, 0), (0, 0, 1), 0.02), AURA, "tail", fuse=False,
      surface="glow")

# --- the grin: a wide crescent of white teeth with a zigzag between them, edged dark -------------------------
CO, CT, CG, CK = srgb(ORANGE), srgb(TEETH), srgb(GUM), srgb(INK)
def grin(u, v):
    c = np.tile(CO, (len(u), 1))
    vt = 0.2 + 0.62 * u * u
    vb = -0.86 + 1.68 * u * u
    inner = (v < vt) & (v > vb)
    edge = (v < vt + 0.11) & (v > vb - 0.11)
    c[edge] = CK
    c[inner] = CT
    vm = (vt + vb) / 2
    zig = vm + (vt - vb) * 0.3 * tri(u * 3.0 + 0.5)
    c[inner & (np.abs(v - zig) < 0.075)] = CG
    return c
dg = unit(0, -1, -0.32)
GP = HC + np.array([0, -HR[1] * 0.9, -0.05])
k.decal(GP + dg * 0.02, dg, (0.084, 0.036), grin, eps=0.004, outline=lambda th: 1.0, rings=44, segs=160)

k.build(voxel=0.005)

# Large eyes: a white ring round a green iris with a solid dark pupil and a glint, a dark edge
CW, CI, CP = srgb(WHITE), srgb(IRIS), srgb(PUPIL)
def eye(u, v):
    c = np.tile(CW, (len(u), 1))
    r = np.sqrt(u * u + v * v)
    c[r < 0.6] = CI
    c[((u - 0.04) / 0.2) ** 2 + ((v + 0.02) / 0.34) ** 2 < 1] = CP
    c[((u + 0.14) / 0.14) ** 2 + ((v - 0.24) / 0.14) ** 2 < 1] = CW
    c[r > 0.86] = CK
    return c
de = unit(0.4, -0.86, 0.3)
E = HC + de * 1 / math.sqrt(((de / HR) ** 2).sum())
def eyes(state):
    k.decal(E, de, (0.034, 0.037), eye_look(state, eye, CO, CK, line=0.16), eps=0.004, rings=20, segs=56,
            up=(0, 0, 1), mirror=True)

k.eye_states(eyes)
k.export(out)
