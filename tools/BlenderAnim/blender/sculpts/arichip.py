# Arichip sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/arichip.py -- <the export's Arichip.glb> <out.glb>
# Carpathia No. 10019 (plan 46), the project's own design: docs/art/reference/gemini/carpathia/10019-Arichip.jpg and
# CP-dex-1-plateau.md, "19. Arichip: a hedgehog". From the picture: a small round brown hedgehog low on four stubby
# legs with round toes; its back and the top of its head a great mound of dry autumn leaves (oak-lobed, yellow,
# golden, orange, rust and brown, each its own colour, overlapping like shingles and sweeping back, a few standing up
# over the brow); one red apple with a brown stem stuck in the leaves on the upper back; a brown head out in front of
# the mound, a pointed snout with a round black nose, small round dark eyes with a glint, a pale blush under each eye,
# a small smile; small round ears with a darker lining at the mound's front edge; the brown flanks and belly showing
# under the leaves. The picture overrules the brief only in size: the "tiny" eyes are built bold for the game.
# Written to be grown from: aricorn.py calls hedgehog() with its own parameters (the skeleton is this one's x 1.6).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, hull, srgb, inside, unit, eye_look, args, outward

BROWN, FACE, LEGC, LINING = "#B07C55", "#BC8960", "#9C6A46", "#7E5236"
NOSE, PUPIL, WHITE, INK, BLUSH, SMILE = "#1E1A1C", "#1C1414", "#FFFFFF", "#3A2418", "#E0A27E", "#5A3420"
APPLE, STEM, APPLELEAF = "#C8352C", "#6A4424", "#5A9A3A"
AUTUMN = ["#E9BE3A", "#DA842E", "#B45A26", "#F2D25C", "#C8682A", "#9C5A2A"]
MOUND = "#B8692E"

ARICHIP = dict(
    name="Arichip", K=1.0, leaves=AUTUMN, plain_share=0.0, mound=MOUND, count=1.0, size=1.0,
    fruit=[("apple", (0.5, 0.15, 0.85), APPLE)], muzzle=None, brows=False, claws=False, seed=1019,
)


# --- shared helpers -----------------------------------------------------------------------------------------------
def board(ob):
    """The game's board check: the four corners' sum against its square (at most 0.95 of it)."""
    V = np.array([v.co[:] for v in ob.data.vertices])
    h = V[:, 2].max(); w = V[:, 0].max() - V[:, 0].min(); l = V[:, 1].max() - V[:, 1].min()
    sq = 1.3 * max(h, 0.85 * max(w, l))
    s = 0.853 * w + 0.522 * l
    print(f"KIT: board height {h:.3f} width {w:.3f} length {l:.3f}: corners {s:.3f} of square {sq:.3f}"
          f" = {s / sq:.2f}")


def oak(L, w, lobes=3):
    """An oak leaf's outline along +a: lobed sides, a pointed tip, a narrow stalk."""
    t = np.linspace(0.0, 1.0, 13)[1:-1]
    half = w * np.sin(math.pi * t) ** 0.7 * (0.72 + 0.28 * np.abs(np.cos(lobes * math.pi * t)))
    right = [(L * a, -b) for a, b in zip(t, half)]
    left = [(L * a, b) for a, b in zip(t[::-1], half[::-1])]
    return [(0.0, -0.18 * w)] + right + [(L, 0.0)] + left + [(0.0, 0.18 * w)]


def plain(L, w):
    """A plain pointed leaf's outline along +a (a fruit tree's)."""
    t = np.linspace(0.0, 1.0, 11)[1:-1]
    half = w * np.sin(math.pi * t ** 0.85) ** 0.8
    return [(0.0, -0.15 * w)] + [(L * a, -b) for a, b in zip(t, half)] + [(L, 0.0)] + \
        [(L * a, b) for a, b in zip(t[::-1], half[::-1])] + [(0.0, 0.15 * w)]


def leaf_plate(k, name, base, d, o, L, w, col, thick, shape, bone="body"):
    """A leaf as a thick hard-edged plate from base along d, its face turned out along o."""
    d = unit(*d)
    across = np.cross(o, d)
    if np.linalg.norm(across) < 1e-3:
        across = np.cross((0, 0, 1.0), d)
    across = across / np.linalg.norm(across)
    outline = oak(L, w) if shape == "oak" else plain(L, w)
    k.add(name, slab(outline, base, d, across, thick), col, bone, fuse=False, surface="leaf")


def fruit(k, name, at, r, kind, col, up=(0, 0, 1)):
    """An apple, a plum or a pear stuck in the leaves, a brown stem and (for an apple) a small leaf at its top."""
    at = np.array(at, float)
    up = unit(*up)
    if kind == "pear":
        def pearish(V):
            V = V.copy()
            z = V[:, 2]
            f = 1 - 0.38 * np.clip(z, 0, 1)
            V[:, 0] *= f; V[:, 1] *= f
            return V
        V, F = ell((0, 0, 0), (r, r, r * 1.3), deform=pearish)
    elif kind == "plum":
        V, F = ell((0, 0, 0), (r * 0.92, r * 0.92, r))
    else:
        def dimple(V):
            V = V.copy()
            V[:, 2] -= 0.18 * np.exp(-(V[:, 0] ** 2 + V[:, 1] ** 2) / 0.08) * np.sign(V[:, 2])
            return V
        V, F = ell((0, 0, 0), (r, r, r * 0.92), deform=dimple)
    a = np.cross(up, (1.0, 0, 0)); a /= np.linalg.norm(a); b = np.cross(up, a)
    M = np.stack([b, a, up], axis=1)
    k.add(name, outward((V @ M.T + at, F)), col, "body", fuse=False, surface="skin")
    top = at + up * r * (1.25 if kind == "pear" else 0.85)
    k.add(name + "stem", tube([top - up * r * 0.1, top + up * r * 0.35 + b * r * 0.12], [r * 0.2, r * 0.17]),
          STEM, "body", fuse=False, surface="leaf")
    if kind == "apple":
        tip = top + up * r * 0.2
        lv = unit(*(b + up * 0.4))
        k.add(name + "leaf", slab(plain(r * 1.1, r * 0.32), tip, lv, a, r * 0.22), APPLELEAF, "body", fuse=False,
              surface="leaf")


def round_eye(white, pupil, glint=(0.3, 0.36, 0.3), glint2=(-0.28, -0.3, 0.14)):
    """A solid dark eye, a white glint up and out and a small one low and in."""
    W, P = srgb(white), srgb(pupil)
    def look(u, v):
        c = np.tile(P, (len(u), 1))
        c[((u - glint[0]) / glint[2]) ** 2 + ((v - glint[1]) / glint[2]) ** 2 < 1] = W
        c[((u - glint2[0]) / glint2[2]) ** 2 + ((v - glint2[1]) / glint2[2]) ** 2 < 1] = W
        return c
    return look


def on_ell(c, r, x, z, front=True):
    """The point of an ellipsoid's front at a given x and z."""
    c, r = np.array(c, float), np.array(r, float)
    s = 1 - ((x - c[0]) / r[0]) ** 2 - ((z - c[2]) / r[2]) ** 2
    return np.array((x, c[1] + (-1 if front else 1) * r[1] * math.sqrt(max(s, 0.0)), z))


def claw(base, d, length, w):
    base = np.array(base, float); d = unit(*d)
    side = np.cross(d, (0, 0, 1.0)); side /= np.linalg.norm(side)
    face = np.cross(d, side)
    return hull([base + side * w, base - side * w, base + face * w * 0.8, base - face * w * 0.8,
                 base + d * length])


# --- the hedgehog ---------------------------------------------------------------------------------------------------
def hedgehog(src, out, P):
    K = P["K"]
    rs = np.random.RandomState(P["seed"])
    k = Kit(src, coat="fur")
    S = lambda *v: np.array(v, float) * K

    # The round brown body, its belly low; the leaves' mound over its back and the top of its head
    B, BR = S(0, 0.04, 0.142), S(0.13, 0.155, 0.096)
    k.add("body", ell(B, BR), BROWN, "body")
    M, MR = S(0, 0.045, 0.2), S(0.132, 0.16, 0.13)
    k.add("mound", ell(M, MR), P["mound"], "body", surface="leaf")
    # Brown wherever the leaves leave the flanks: under the mound's rim
    k.paint(lambda Q: inside(Q[:, 2] - (0.11 + 0.02 * np.cos(Q[:, 1] * 40 / K)) * K, 0.003 * K), BROWN, on=["mound"])

    # The head out in front of the mound, a pointed snout with a black nose
    H, HR = S(0, -0.125, 0.152), S(0.1, 0.09, 0.088)
    k.add("head", ell(H, HR), FACE, "head")
    def taper(V):
        V = V.copy()
        t = np.clip(-V[:, 1], 0, 1)
        V[:, 0] *= 1 - 0.5 * t; V[:, 2] *= 1 - 0.42 * t
        V[:, 2] -= 0.12 * t
        return V
    SC, SR = S(0, -0.2, 0.128), S(0.06, 0.075, 0.052)
    k.add("snout", ell(SC, SR, deform=taper), FACE, "head")
    nose_at = S(0, -0.272, 0.118)
    k.add("nose", ell(nose_at, S(0.018, 0.014, 0.015), rot=(15, 0, 0)), NOSE, "head", fuse=False, surface="shell")
    if P["muzzle"]:
        # The grey muzzle: the snout's front, its edge sloping from below the eye to the chin
        k.paint(lambda Q: inside(Q[:, 1] / K - (-0.215 + 0.6 * (Q[:, 2] / K - 0.13)), 0.0025)
                * (Q[:, 2] < 0.168 * K), P["muzzle"])

    # Small round ears at the mound's front edge, a darker lining
    for s, nm in ((1, "R"), (-1, "L")):
        ec = S(0.083 * s, -0.122, 0.218)
        k.add(f"ear{nm}", ell(ec, S(0.034, 0.016, 0.032), rot=(0, 0, -28 * s)), FACE, f"ear{nm}")
        k.decal(ec + S(0.006 * s, -0.016, 0.0), unit(0.45 * s, -1, 0.05), (0.02 * K, 0.019 * K), LINING,
                eps=0.003 * K, rings=8, segs=32)

    # Four stubby legs, haunches fused into the body, round paws with three toes
    PR = S(0.032, 0.036, 0.019)
    for nm, bone, y in (("leg", "leg{s}", -0.07), ("hind", "hind{s}", 0.1)):
        x = 0.075
        k.add(nm + "top", ell(S(x, y, 0.08), S(0.042, 0.046, 0.045)), BROWN, bone, mirror=True)
        k.add(nm, tube([S(x, y, 0.07), S(x + 0.002, y - 0.004, 0.025)], [0.03 * K, 0.028 * K]), LEGC, bone,
              mirror=True)
        pc = S(x + 0.002, y - 0.012, 0.019)
        k.add(nm + "paw", ell(pc, PR), LEGC, bone, mirror=True)
        for j, dx in enumerate((-0.55, 0.0, 0.55)):
            k.add(f"{nm}toe{j}", ell(pc + np.array((dx * PR[0] * 0.8, -PR[1] * 0.72, -PR[2] * 0.15)),
                                     (PR[0] * 0.38, PR[1] * 0.38, PR[2] * 0.82)), LEGC, bone, mirror=True)
            if P["claws"]:
                a = pc + np.array((dx * PR[0] * 0.8, -PR[1] * 1.05, -PR[2] * 0.45))
                k.add(f"{nm}claw{j}", claw(a, (dx * 0.3, -1, -0.7), 0.014 * K, 0.0062 * K), LINING, bone,
                      fuse=False, mirror=True, surface="shell")

    # The leaves: rows over the mound, each sweeping back and out, its face turned out; neighbours differ in colour
    cols = P["leaves"]
    thick = 0.011 * K
    n = 0
    last = -1
    CROWN = unit(0, -0.75, 0.66)
    rows = ((1.3, 3), (1.05, 7), (0.8, 9), (0.55, 11), (0.3, 12), (0.02, 13), (-0.28, 12))
    for row, (el, count) in enumerate(rows):
        count = int(round(count * P["count"]))
        for j in range(count):
            az = 2 * math.pi * (j + 0.5 * (row % 2)) / count + rs.uniform(-0.12, 0.12)
            o = np.array([math.cos(el) * math.sin(az), -math.cos(el) * math.cos(az), math.sin(el)])
            p = M + o * MR * 0.9
            if p[1] < -0.06 * K and p[2] < 0.215 * K:
                continue                              # the face stays clear of the mound's front rim
            if p[2] < (0.105 - 0.03 * max(o[1], 0)) * K:
                continue                              # the flanks below the rim stay brown
            # Lying on the mound like shingles, flowing from a crown over the brow back and down, tips lifted
            t = o - CROWN
            t = t - o * (t @ o)
            if np.linalg.norm(t) < 1e-3:
                t = np.array([0, 1.0, 0])
            d = unit(*t) + o * 0.24
            L = rs.uniform(0.092, 0.112) * K * P["size"] * (0.85 if el < 0 else 1.0)
            w = L * rs.uniform(0.32, 0.38)
            ci = rs.randint(len(cols))
            if ci == last:
                ci = (ci + 1) % len(cols)
            last = ci
            shape = "plain" if rs.rand() < P["plain_share"] else "oak"
            col = cols[ci]
            if shape == "oak" and P["plain_share"] > 0:
                col = AUTUMN[rs.randint(len(AUTUMN))]
            leaf_plate(k, f"leaf{n}", p - unit(*d) * L * 0.18, d, o, L, w, col, thick, shape)
            n += 1
    # Three leaves standing up over the brow, the middle one the largest
    for i, x in enumerate((-0.045, 0.0, 0.045)):
        o = unit(x * 6, -0.8, 0.8)
        L = (0.088 if x == 0 else 0.074) * K * P["size"]
        shape = "oak" if P["plain_share"] < 1 or i != 1 else "plain"
        col = (AUTUMN[3], AUTUMN[0], AUTUMN[1])[i] if P["plain_share"] == 0 else (cols[0], AUTUMN[1], cols[2])[i]
        leaf_plate(k, f"brow{i}", S(x, -0.095, 0.222), (x * 5, -0.1, 1.0), o, L, L * 0.36, col, thick,
                   "oak" if i != 2 or P["plain_share"] == 0 else "plain")

    # The fruit stuck in the leaves
    for i, (kind, dirn, col) in enumerate(P["fruit"]):
        o = unit(*dirn)
        r = 0.029 * K * (0.82 if kind == "plum" else 1.0) * P.get("fruit_size", 1.0)
        at = M + o * MR * 1.0 + o * r * 0.7
        fruit(k, f"fruit{i}", at, r, kind, col, up=o * 0.5 + np.array([0, 0, 1.0]))

    # The face: a pale blush under each eye, a small smile, kindly brows for the grown one
    E = on_ell(H, HR, 0.05 * K, 0.172 * K) + S(0, -0.001, 0)
    for s in (1, -1):
        bl = on_ell(H, HR, 0.072 * K * s, 0.142 * K)
        k.decal(bl, unit(0.6 * s, -1, -0.1), (0.017 * K, 0.009 * K), BLUSH, eps=0.003 * K, rings=8, segs=32)
        if P["brows"]:
            def brow(u, v):
                c = np.tile(srgb(FACE), (len(u), 1))
                c[np.abs(v - (0.2 - 0.5 * u * u)) < 0.42] = srgb(LINING)
                return c
            def brow_shape(th):
                return 1.0
            k.decal(E * np.array([s, 1, 1]) + S(0.004 * s, -0.004, 0.028), unit(0.42 * s, -1, 0.35),
                    (0.022 * K, 0.008 * K), srgb(LINING), eps=0.003 * K, rings=6, segs=32)
    SM, FC_ = srgb(SMILE), srgb(P["muzzle"] or FACE)
    def smile(u, v):
        c = np.tile(FC_, (len(u), 1))
        c[np.abs(v - (0.85 * u * u - 0.35)) < 0.26] = SM
        return c
    k.decal(S(0, -0.248, 0.093), unit(0, -1, -0.55), (0.028 * K, 0.014 * K), smile, eps=0.003 * K, rings=10,
            segs=40)

    ob = k.build(voxel=0.0042 * K, smooth=3, band=0.004 * K)
    board(ob)

    # Small, bold round dark eyes with a glint, set wide on the head
    look = round_eye(WHITE, PUPIL)
    lid, ink = srgb(FACE), srgb(INK)
    def lay(state):
        f = eye_look(state, look, lid, ink)
        k.decal(E, unit(0.45, -1, 0.08), (0.017 * K, 0.02 * K), f, eps=0.004 * K, rings=20, segs=56, mirror=True)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    hedgehog(*args(), ARICHIP)
