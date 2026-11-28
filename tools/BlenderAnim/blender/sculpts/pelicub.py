# Pelicub sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/pelicub.py -- <the export's Pelicub.glb> <out.glb>
#
# The Carpathian Water starter (No. 10007), from its picture (docs/art/reference/gemini/carpathia/10007-Pelicub.jpg)
# and its brief (CP-dex-1-plateau.md, 7): a round, fluffy pelican chick in white down with a pale blue sheen. A big
# round head sitting straight on a round body with no neck showing, little tufts of down on its crown and at the
# lower edge of the body; an oversized pale yellow beak, its upper half flat with a rounded tip turned down a little,
# and under it a wobbly pouch of paler yellow sagging lowest toward the middle; round black eyes high on the head
# either side of the beak's root, a white glint each; short stubby folded wings, tipped in a few soft points; a small
# fluffy tail turned up behind; big orange webbed feet on hardly any legs, the belly sitting almost on them.
# Written to be grown from: pelireed.py imports it (sibling("pelicub")) for the family's feet, eyes, pouch and
# palette, and buciumbill.py through pelireed.py.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, slab, srgb, inside, smoothstep, lumpy, unit, eye_look, args, sibling

d_ = sibling("ducklett")
p_ = sibling("pidgey")
spike = p_.spike
frame_ell = d_.frame_ell

# The family's palette
WHITE, SHEEN, BEAK, POUCH, FOOT, INK = "#EEF6FA", "#CDE2EF", "#EBD290", "#F5E3A8", "#F29A4C", "#16181E"
INKC, WHITEC = srgb(INK), np.array([1.0, 1.0, 1.0])

HEIGHT = 0.56                               # the export's
VOXEL = 0.0052
HC = np.array([0.0, -0.02, 0.43])           # the head's centre, over the head's joint (0, -0.02, 0.34)
HR = np.array([0.137, 0.128, 0.13])
BC = np.array([0.0, 0.03, 0.195])           # the body's centre, over the body's joint (0, 0.02, 0.22)
BR = np.array([0.19, 0.18, 0.162])


def on_ell(c, r, d):
    d = np.array(d, float)
    return np.array(c, float) + d / np.linalg.norm(d / np.array(r, float))


def board(k):
    """The game's board check on the built body: the span it shows at the board's turn against its square."""
    V = np.array([v.co[:] for v in k.ob.data.vertices])
    a = math.radians(31.5)
    proj = V[:, 0] * math.cos(a) + V[:, 1] * math.sin(a)
    h, w, l = V[:, 2].max(), np.ptp(V[:, 0]), np.ptp(V[:, 1])
    sq = 1.3 * max(h, 0.85 * max(w, l))
    print("KIT: board: shows %.3f in a square of %.3f (%.2f); four corners %.3f" % (np.ptp(proj), sq, np.ptp(proj) / sq,
                                                                                  0.853 * w + 0.522 * l))


# --- the family's parts ---------------------------------------------------------------------------------------------
def feet(k, x, ankle, top, scale, thick, legr, colour=FOOT, turn=14, toes_y=-0.012):
    """Short legs from top down to the ankle, and a broad webbed foot of three toes (ducklett's outline) at scale."""
    foot = [(a * scale, b * scale) for a, b in d_.FOOT_OUTLINE]
    for s, side in ((1, "R"), (-1, "L")):
        xs = x * s
        k.add(f"leg{side}", tube([(xs, top[0], top[1]), (xs * 1.04, ankle[0], ankle[1] + legr * 0.8)], [legr, legr * 0.9]), colour,
              f"leg{side}", fuse=False, surface="scales")
        t = math.radians(turn * s)
        ex = np.array([math.cos(t), math.sin(t), 0])
        ez = np.array([math.sin(t), -math.cos(t), 0])
        k.add(f"foot{side}", slab(foot, (xs * 1.08, ankle[0] + toes_y, thick / 2), ex, ez, thick), colour,
              f"leg{side}", fuse=False, surface="scales")


def eye_round(iris, pupil=None, pupil_r=0.5, lid=None, lid_at=None, rim=0.86, glint=(-0.3, 0.36, 0.26)):
    """
    The family's eye (u outward, v up): a dark rim, an iris, a dark pupil, a white glint toward the inner top; lid_at
    (a, b) cuts its top along v = a + b u with the lid's colour over it and an ink line along the cut.
    """
    I = srgb(iris)
    P = srgb(pupil) if pupil else None
    L = srgb(lid) if lid else None
    def look(u, v):
        c = np.tile(I, (len(u), 1))
        c[u * u + v * v > rim ** 2] = INKC
        if P is not None:
            c[u * u + (v + 0.04) ** 2 < pupil_r ** 2] = P
        gu, gv, gr = glint
        c[(u - gu) ** 2 + (v - gv) ** 2 < gr ** 2] = WHITEC
        if lid_at is not None:
            edge = lid_at[0] + lid_at[1] * u
            c[v > edge] = L
            c[np.abs(v - edge) < 0.13] = INKC
        return c
    return look


def eye_layer(k, centre, direction, half, look, lid, line=0.16, eps=0.0035):
    """The eyes in a state: one patch a side cast down direction onto the head (mirrored, u outward)."""
    L = srgb(lid)
    def lay(state):
        k.decal(centre, direction, half, eye_look(state, look, L, INKC, line=line), eps=eps, rings=20, segs=56,
                mirror=True)
    return lay


def tufts(k, base, dirs, length, r, colour, bone, name="tuft"):
    """Little fused points of down: one from base[i] along dirs[i] each."""
    for i, (b, d) in enumerate(zip(base, dirs)):
        b = np.array(b, float); d = unit(*d)
        k.add(f"{name}{i}", tube([b, b + d * length * 0.6, b + d * length], [r, r * 0.85, max(r * 0.65, 0.009)]), colour, bone)


# --- Pelicub's own --------------------------------------------------------------------------------------------------
def body(k):
    k.add("body", ell(BC, BR, deform=lumpy(7, 4, 0.02)), WHITE, "body")
    # the head sits straight on the body, a broad joining mass and no neck
    k.add("join", ell((0.0, 0.01, 0.35), (0.135, 0.13, 0.13)), WHITE, "head")
    k.add("head", ell(HC, HR), WHITE, "head")
    # the tail: a small fluffy point turned up behind
    k.add("tail", blade([(0, 0.17, 0.22), (0, 0.235, 0.265), (0, 0.27, 0.305)], [0.05, 0.034, 0.012], (1, 0, 0), 0.6),
          WHITE, "tail")
    # tufts of down: on the crown, behind the head, and round the body's lower edge
    crown = HC + np.array([0, 0.01, HR[2] * 0.93])
    tufts(k, [crown + (0, -0.02, 0), crown + (0.035, 0.0, -0.005), crown + (-0.035, 0.0, -0.005), crown + (0, 0.04, -0.01)],
          [(0, -0.3, 1), (0.6, -0.1, 0.8), (-0.6, -0.1, 0.8), (0, 0.5, 0.8)], 0.036, 0.014, WHITE, "head")
    nape = [HC + np.array([0.09 * math.sin(a), 0.1 * math.cos(a) + 0.01, -0.07]) for a in (-0.9, 0.0, 0.9)]
    tufts(k, nape, [(math.sin(a) * 0.6, 0.9, -0.1) for a in (-0.9, 0.0, 0.9)], 0.03, 0.014, WHITE, "head", "napetuft")
    low = []
    dirs = []
    for j in range(9):
        a = 2 * math.pi * (j + 0.5) / 9
        if abs(math.sin(a)) < 0.35 and math.cos(a) < 0:      # leave the front under the pouch plain
            continue
        p = BC + np.array([BR[0] * 0.92 * math.sin(a), BR[1] * 0.92 * math.cos(a), -0.075])
        low.append(p); dirs.append((math.sin(a), math.cos(a), -0.55))
    tufts(k, low, dirs, 0.022, 0.016, WHITE, "body", "lowtuft")


def colours(k):
    """The pale blue sheen low on the body and in the wings' shade."""
    k.paint(lambda P: inside(P[:, 2] - (0.105 + 0.02 * np.sin(3 * np.arctan2(P[:, 0], P[:, 1]))), 0.02)
            * (P[:, 2] < 0.3), SHEEN, on=["body", "lowtuft"])
    k.paint(lambda P: inside(P[:, 2] - 0.21, 0.02) * (P[:, 1] > 0.03), SHEEN, on=["wing"])


def wings(k):
    """Short stubby wings folded at the sides, back and down, each ending in three soft points."""
    sh = np.array([0.168, 0.01, 0.265])
    d = unit(0.22, 0.72, -0.66)
    ch = unit(0.0, -0.6, -0.65)
    k.add("wing", frame_ell(sh + d * 0.06, (0.095, 0.062, 0.03), d, ch), WHITE, "wing{s}", mirror=True)
    for j, a in enumerate((-0.5, 0.0, 0.5)):
        root = sh + d * 0.11 + ch * a * 0.04
        fd = unit(*(d + ch * a * 0.5))
        k.add(f"wingpoint{j}", tube([root, root + fd * 0.04], [0.018, 0.011]), WHITE, "wing{s}", mirror=True)


def upper_beak(root, tip, width, thick, bulb=0.3, hook=0.6, narrow=0.78):
    """
    The family's upper bill, one smooth piece from root to tip: flat, drawn in a little at its root and broadening to
    a rounded tip (bulb), thicker there, the tip turned down by hook of its thickness.
    """
    root, tip = np.array(root, float), np.array(tip, float)
    L = np.linalg.norm(tip - root)
    V, F = ell((0, 0, 0), (width, L / 2, thick), sub=4)
    t = (V[:, 1] / (L / 2) + 1) / 2                     # 0 at the root, 1 at the tip
    V[:, 0] *= narrow + (1 - narrow) * smoothstep(0.0, 0.5, t) + bulb * smoothstep(0.55, 0.92, t) * (1 - t) * 4
    V[:, 2] *= 1 + bulb * 1.2 * smoothstep(0.6, 0.95, t)
    V[:, 2] -= hook * thick * smoothstep(0.7, 1.0, t)
    d = (tip - root) / L
    side = np.array([1.0, 0, 0])
    up = np.cross(side, d)          # right-handed: side, d, up
    M = np.stack([side, d, up], axis=1)
    return V @ M.T + (root + tip) / 2, F


def beak(k):
    """The upper half: broad and flat from the face, rounded at its tip, the tip turned down a little."""
    k.add("beak", upper_beak((0, -0.07, 0.4), (0, -0.46, 0.36), 0.074, 0.03), BEAK, "head", fuse=False,
          surface="shell")
    # the pouch: a sack of paler yellow sagging under it, lowest toward the middle, its top flat under the beak
    PC, PR = np.array([0.0, -0.255, 0.27]), np.array([0.092, 0.18, 0.13])
    def sack(V):
        V = V.copy()
        V[:, 2] = np.minimum(V[:, 2], 0.74)                      # its top flat under the beak
        V[:, 2] -= 0.18 * np.clip(-V[:, 2], 0, 1) * (1 - np.abs(V[:, 1] - 0.15))   # sagging lowest at the middle
        V[:, 0] *= 1 + 0.12 * np.clip(-V[:, 2], 0, 1)                             # wider where it sags
        return V
    k.add("pouch", ell(PC, PR, rot=(9, 0, 0), deform=sack), POUCH, "jaw", fuse=False, surface="shell")


EYEDIR = unit(0.5, -0.8, 0.2)
EYE = (0.028, 0.031)


def eyes(k):
    E = on_ell(HC, HR, EYEDIR)
    return eye_layer(k, E, EYEDIR, EYE, eye_round("#1C1E26"), WHITE)


def pelicub(src, out_path):
    k = Kit(src, coat="fur")
    body(k)
    wings(k)
    colours(k)
    beak(k)
    feet(k, 0.07, (0.0, 0.016), (0.012, 0.085), 2.6, 0.018, 0.02)
    k.build(voxel=VOXEL, band=0.006)
    board(k)
    k.eye_states(eyes(k))
    k.export(out_path)


if __name__ == "__main__":
    pelicub(*args())
