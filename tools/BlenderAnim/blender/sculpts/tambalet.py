# Tambalet (No. 10085, Carpathia, Sound), sculpted from nothing on the skeleton and clips of the game's own export:
#   blender --background --factory-startup --python sculpts/tambalet.py -- <the export's Tambalet.glb> <out.glb>
# From the picture (docs/art/reference/gemini/carpathia/10085-Tambalet.jpg) and the brief (CP-dex-2-mountains.md, 85):
# a small tortoise whose shell is the top of a cimbalom: a flat trapezoid box of polished red-brown wood, wider at the
# back, its deck lighter, a row of silver strings stretched across it from side to side over two gold bridges, a
# silver tuning peg at each string's end along both side rails; the tortoise's orange-tan body under it, a pale
# plastron showing below the box; a neck stretched out from under the box's front, a round head with big friendly
# brown eyes, a little snout and a smile, brown swirls on the brow and the cheeks, a teal wave down the neck; four thick
# splayed tortoise legs with three cream toes each, a yellow zigzag round the near (+X) foreleg, a teal one round the
# far, a gold swirl on each thigh; a little pointed tail; two long feelers from the crown, up in loops, each ending in a
# soft pale felt hammer with a brown collar: one rests on the strings (+X, behind), one hangs beside the face (-X).
#
# Written to be grown from (tambalon.py imports it with sibling("tambalet")): the whole frame scales by S["k"]; the
# palette, the leg, the swirl, the eye and the feeler are the functions below; `cimbalom(src, out, P, extra)` builds
# the line, `extra(k, P)` adding what the grown stage alone has.
#
# Reference ratios (picture): the box 0.47 of the height deep (incl. legs) and as wide as long; the head 0.27 of the
# box's width across (built 0.30: a tenth more); the feelers' loops 0.30 of the whole height over the box (built 0.33);
# the legs showing under the box 0.35 of the body's height.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, hull, lathe, slab, outward, srgb, inside, unit, polygon, eye_look, tri, args)

# --- the line's palette ---------------------------------------------------------------------------------------------
TAMBALET = dict(
    k=1.0, skin="#D69A5C", skin2="#C0824A", belly="#EBC77E", toe="#F0DEB4",
    side="#963C26", deck="#B5563A", frame="#C66A44", string="#C9CCD6", bridge="#D2A55A", peg="#C8CAD2",
    hammer="#DDD2C2", collar="#7A4A30", swirl="#A8622E", teal="#3E9E94", yellow="#E3C23C", gold="#CF9E3C",
    ink="#2A1A12", iris="#5A3A24", white="#FBF6EC", mouth="#7A3E26",
    strings=8, eyes="open", voxel=0.0055,
)


def on_ell(c, r, d):
    d = unit(*d)
    t = 1.0 / math.sqrt(((d / np.array(r)) ** 2).sum())
    return np.array(c, float) + d * t


def spiral_look(col, ground, turns=1.6):
    """A swirl: a line winding out from the middle (u, v in -1..1), the rest the ground's colour."""
    C, G = srgb(col), srgb(ground)
    def look(u, v):
        r = np.hypot(u, v); th = np.arctan2(v, u)
        ph = (r / 0.9 * turns - th / (2 * math.pi)) % 1.0
        c = np.tile(G, (len(u), 1))
        c[(np.abs(ph - 0.5) < 0.2) & (r < 0.95)] = C
        c[r < 0.16] = C
        return c
    return look


def wave_look(col, ground, waves=1.5, amp=0.42, half=0.26):
    """A wave across the patch (a teal ripple down a neck)."""
    C, G = srgb(col), srgb(ground)
    def look(u, v):
        c = np.tile(G, (len(u), 1))
        c[(np.abs(v - amp * np.sin(u * math.pi * waves)) < half) & (np.abs(u) < 0.92)] = C
        return c
    return look


def round_eye(P):
    """The open eye: a white with a dark rim, a big brown iris toward the front, a black pupil, two glints."""
    W_, I_, K_, R_ = srgb(P["white"]), srgb(P["iris"]), srgb(P["ink"]), srgb(P["ink"])
    G = np.array([1.0, 1.0, 1.0])
    def look(u, v):
        c = np.tile(W_, (len(u), 1))
        c[np.hypot((u + 0.12) / 0.66, (v + 0.02) / 0.74) < 1] = I_
        c[np.hypot((u + 0.14) / 0.38, (v + 0.02) / 0.44) < 1] = K_
        c[np.hypot(u, v) > 0.86] = R_
        c[np.hypot((u + 0.0) / 0.2, (v - 0.32) / 0.2) < 1] = G
        c[np.hypot(u + 0.36, v + 0.3) < 0.09] = G
        return c
    return look


def lidded_eye(P):
    """The grown stage's open eye, half shut as if lost in music: the lid over the top, a dark slit of eye below."""
    L_, I_, K_ = srgb(P["skin"]), srgb(P["iris"]), srgb(P["ink"])
    def look(u, v):
        lid = 0.05 - 0.38 * (1 - u * u)              # the lid's edge, an arc curving down like a calm closed eye
        c = np.tile(L_, (len(u), 1))
        sliver = (v < lid) & (v > lid - 0.42 * (1 - u * u) - 0.02) & (np.abs(u) < 0.86)
        c[sliver] = I_
        c[sliver & (np.hypot(u + 0.12, v - lid + 0.16) < 0.2)] = K_
        c[(np.abs(v - lid) < 0.15) & (np.abs(u) < 0.95)] = K_
        return c
    return look


def eyes_pair(k, E, de, half, look_open, lid, ink, line=0.15):
    """Both eyes, the left's colour function mirrored so that u runs outward on both."""
    m = np.array([-1, 1, 1.0])
    def lay(state):
        lk = eye_look(state, look_open, lid, ink, line=line)
        k.decal(E, de, half, lk, eps=0.0028 * 1, rings=20, segs=56)
        k.decal(E * m, de * m, half, (lambda u, v, g=lk: g(-u, v)), eps=0.0028, rings=20, segs=56)
    return lay


def leg(k, P, name, top, foot, radii, bone, mark=None):
    """A thick splayed tortoise leg: a shoulder fused into the body, the leg out and down to a round foot, three toes."""
    K = P["k"]
    top, foot = np.array(top) * K, np.array(foot) * K
    k.add(name + "mass", ell(top, np.array([0.068, 0.072, 0.062]) * K), P["skin"], bone, mirror=True)
    mid = (top + foot) / 2 + np.array([0.012, 0, 0.005]) * K
    k.add(name, tube([top, mid, foot + np.array([0, 0, 0.02]) * K], np.array(radii) * K), P["skin"], bone, mirror=True)
    fc = foot + np.array([0.004, -0.008, 0.0]) * K
    k.add(name + "foot", ell(fc, np.array([0.062, 0.066, 0.032]) * K), P["skin"], bone, mirror=True)
    for j, dx in enumerate((-0.03, 0.0, 0.03)):
        tc = fc + np.array([dx, -0.052, -0.01]) * K
        k.add(f"{name}toe{j}", ell(tc, np.array([0.013, 0.016, 0.012]) * K * P.get("toe_size", 1.0), sub=3),
              P["toe"], bone, fuse=False, mirror=True, surface="shell")


def band_paint(k, P, xc, yc, zc, colour, side, waves=3, amp=0.012, half=0.006, reach=0.075):
    """A zigzag band painted round a leg at height zc (one side's leg alone: side +1 or -1)."""
    K = P["k"]
    xc, yc, zc, amp, half, reach = xc * K, yc * K, zc * K, amp * K, half * K, reach * K
    def m(Q):
        x = Q[:, 0] * side
        a = np.arctan2(Q[:, 1] - yc, x - xc)
        z = zc + amp * tri(waves * a / (2 * math.pi))
        return (inside(np.abs(Q[:, 2] - z) - half, 0.002 * K) * inside(np.hypot(x - xc, Q[:, 1] - yc) - reach, 0.004)
                * (x > 0.06 * K))
    k.paint(m, colour)


def feeler(k, P, pts, bone, r):
    """A long feeler from the crown through pts, ending in a soft felt hammer with a brown collar."""
    K = P["k"]
    pts = np.array(pts) * K
    k.add("feeler" + bone, outward(tube(pts, [r * K] * len(pts), seg=14, per=8)), P["skin"], bone, fuse=False,
          surface="skin")
    d = unit(*(pts[-1] - pts[-2]))
    end = pts[-1] - d * 0.004 * K
    k.add("collar" + bone, lathe(end, d, [(0, -0.004 * K), (0.016 * K, -0.004 * K), (0.018 * K, 0.004 * K),
                                          (0.018 * K, 0.012 * K), (0, 0.012 * K)], seg=24),
          P["collar"], bone, fuse=False, surface="leather")
    prof = [(0, 0.01), (0.02, 0.012), (0.027, 0.022), (0.03, 0.036), (0.028, 0.05), (0.02, 0.058), (0, 0.06)]
    k.add("hammer" + bone, lathe(end, d, [(a * K, b * K) for a, b in prof], seg=28), P["hammer"], bone, fuse=False,
          surface="cloth")


def halfwidth(P, y):
    """The box's half width at y (it widens toward the back)."""
    K = P["k"]
    t = np.clip((y / K + 0.19) / 0.44, 0, 1)
    return (0.188 + (0.232 - 0.188) * t) * K


def box(k, P, z0=0.172, z1=0.272):
    """The cimbalom's box: a trapezoid of red-brown wood, its deck a lighter inset panel, bridges, strings, pegs."""
    K = P["k"]
    y0, y1 = -0.19 * K, 0.25 * K
    z0, z1 = z0 * K, z1 * K
    pts = []
    for y in (y0, y1):
        w = halfwidth(P, y)
        for s in (1, -1):
            for dz, ins in ((0, 0.014), (0.016, 0), (z1 - z0 - 0.018 * K, 0), (z1 - z0, 0.012)):
                pts.append((s * (w - ins * K), y + (ins * K if y < 0 else -ins * K), z0 + dz))
    k.add("box", hull(pts), P["side"], "body", fuse=False, surface="shell")
    # a moulding of the rim's colour round the box's sides, so the back and the far side are not plain
    band = []
    for y in (y0, y1):
        w = halfwidth(P, y) + 0.002 * K
        for s in (1, -1):
            for dz in (0.03 * K, 0.044 * K):
                band.append((s * w, y + (-0.002 * K if y < 0 else 0.002 * K), z0 + dz))
    k.add("moulding", hull(band), P.get("band", P["frame"]), "body", fuse=False, surface="shell")
    # the deck: a lighter panel inset in a frame of the rim's colour
    dk = []
    for y, inset in ((y0, 0.028), (y1, 0.028)):
        w = halfwidth(P, y)
        for s in (1, -1):
            for z, ins in ((z1 - 0.01 * K, 0), (z1 + 0.004 * K, 0.004)):
                dk.append((s * (w - (0.026 + ins) * K), y + (inset + ins) * K * (1 if y < 0 else -1), z))
    k.add("frame", hull([(x * 1.0, y, z) for x, y, z in
                         [(s * (halfwidth(P, y) - 0.008 * K), y + (0.006 * K if y < 0 else -0.006 * K), z1 + dz)
                          for y in (y0, y1) for s in (1, -1) for dz in (-0.004 * K, 0.002 * K)]]),
          P["frame"], "body", fuse=False, surface="shell")
    k.add("deck", hull(dk), P["deck"], "body", fuse=False, surface="shell")
    top = z1 + 0.004 * K
    # two gold bridges running front to back, which the strings rest on
    for j, x in enumerate((-0.07, 0.09)):
        bx = x * K
        k.add(f"bridge{j}", hull([(bx + sx * 0.008 * K, y, top + dz) for sx in (1, -1) for y in (y0 + 0.05 * K, y1 - 0.05 * K)
                                  for dz in (0, 0.014 * K)]), P["bridge"], "body", fuse=False, surface="shell")
    sz = top + 0.014 * K + 0.004 * K
    n = P["strings"]
    gap = 0.32 * K / (n - 1)
    half = min(0.004 * K, gap * 0.24 / 2)            # a string no wider than a quarter of the gap between two
    for i in range(n):
        y = (-0.125 + 0.32 * i / (n - 1)) * K
        w = halfwidth(P, y) - 0.03 * K
        # a thin flat strip lying across the bridges' tops: a line on the wood from both cameras, not a bar
        k.add(f"string{i}", slab([(-w, -half), (w, -half), (w, half), (-w, half)], (0, y, sz - 0.002 * K),
                                 (1, 0, 0), (0, 1, 0), 0.0035 * K), P["string"], "body", fuse=False, surface="metal")
        for s in (1, -1):
            # a short silver stud on the rail, no taller than the rim is thick
            px = s * (halfwidth(P, y) - 0.016 * K)
            k.add(f"peg{i}{'R' if s > 0 else 'L'}",
                  lathe((px, y, top - 0.003 * K), (0, 0, 1),
                        [(0, 0), (0.008 * K, 0), (0.008 * K, 0.005 * K), (0.005 * K, 0.008 * K), (0, 0.009 * K)],
                        seg=16),
                  P["peg"], "body", fuse=False, surface="metal")


def cimbalom(src, out, P=TAMBALET, extra=None):
    K = P["k"]
    k = Kit(src, coat="scales")

    # --- the soft body under the box, its pale plastron showing below the box -------------------------------------
    k.add("body", ell(np.array([0, 0.02, 0.158]) * K, np.array([0.178, 0.212, 0.08]) * K), P["skin"], "body")
    k.add("plastron", ell(np.array([0, 0.0, 0.122]) * K, np.array([0.15, 0.19, 0.05]) * K), P["belly"], "body")
    k.paint(lambda Q: inside(Q[:, 2] - 0.152 * K, 0.004 * K) * inside(np.abs(Q[:, 0]) - 0.17 * K, 0.006 * K)
            * inside(np.abs(Q[:, 1] - 0.01 * K) - 0.2 * K, 0.006 * K), P["belly"], on=["body", "plastron"])

    # --- four thick splayed legs ---------------------------------------------------------------------------------
    leg(k, P, "fore", (0.135, -0.125, 0.15), (0.17, -0.165, 0.03), (0.06, 0.058, 0.054), "leg{s}")
    leg(k, P, "hind", (0.135, 0.13, 0.15), (0.17, 0.155, 0.03), (0.062, 0.06, 0.056), "hind{s}")
    band_paint(k, P, 0.156, -0.146, 0.085, P["yellow"], 1, reach=0.085)
    band_paint(k, P, 0.156, -0.146, 0.085, P["teal"], -1, reach=0.085)

    # --- a little pointed tail -----------------------------------------------------------------------------------
    k.add("tail", tube(np.array([(0, 0.2, 0.17), (0.01, 0.26, 0.145), (0.022, 0.3, 0.12)]) * K,
                       np.array([0.034, 0.022, 0.009]) * K), P["skin"], "tail")

    # --- the neck out from under the box's front, the round head ------------------------------------------------
    k.add("neck", tube(np.array([(0, -0.13, 0.18), (0, -0.24, 0.23), (0, -0.32, 0.27)]) * K,
                       np.array([0.064, 0.058, 0.054]) * K), P["skin"], "head")
    H, HR = np.array([0, -0.37, 0.3]) * K, np.array([0.08, 0.078, 0.074]) * K
    k.add("head", ell(H, HR), P["skin"], "head")
    SN = np.array([0, -0.435, 0.278]) * K
    k.add("snout", ell(SN, np.array([0.052, 0.04, 0.042]) * K), P["skin"], "head")
    k.add("chin", ell(np.array([0, -0.4, 0.247]) * K, np.array([0.06, 0.05, 0.034]) * K), P["skin"], "head")

    # --- the face's marks (in every state) -----------------------------------------------------------------------
    for s in (1, -1):
        k.decal(SN + np.array([0.017 * s, -0.028, 0.02]) * K, unit(0.2 * s, -0.8, 0.55), np.array([0.0065, 0.0055]) * K,
                P["ink"], eps=0.0018 * K, rings=6, segs=24)
    sm = unit(0, -0.85, -0.5)
    k.decal(on_ell(SN, np.array([0.052, 0.04, 0.042]) * K, sm) + np.array([0, 0, 0.004]) * K, sm,
            np.array([0.04, 0.016]) * K,
            (lambda u, v, C=srgb(P["mouth"]), G=srgb(P["skin"]):
             np.where(((np.abs(v - (0.75 * u * u - 0.25)) < 0.24) & (np.abs(u) < 0.9))[:, None], C, G)),
            eps=0.002 * K, rings=8, segs=48)
    bd = unit(0, -0.6, 0.8)
    k.decal(on_ell(H, HR, bd), bd, np.array([0.026, 0.026]) * K, spiral_look(P["swirl"], P["skin"]),
            eps=0.002 * K, rings=12, segs=56)
    for s in (1, -1):
        cd = unit(0.95 * s, 0.05, -0.05)
        k.decal(on_ell(H, HR, cd), cd, np.array([0.019, 0.019]) * K,
                (lambda u, v, g=spiral_look(P["swirl"], P["skin"]), s=s: g(u * s, v)), eps=0.002 * K, rings=12, segs=56)
        nd = unit(0.95 * s, -0.3, 0.1)
        nc = np.array([0.05 * s, -0.27, 0.245]) * K
        k.decal(nc, nd, np.array([0.04, 0.02]) * K,
                (lambda u, v, g=wave_look(P["teal"], P["skin"]): g(u, v)), eps=0.002 * K, rings=10, segs=48,
                up=(0, 0.4, 1))
    for s in (1, -1):
        for j, (y, z) in enumerate(((0.13, 0.11),)):
            gd = unit(1 * s, 0.1, 0.1)
            k.decal(np.array([0.2 * s, y, z]) * K, gd, np.array([0.022, 0.022]) * K,
                    (lambda u, v, g=spiral_look(P["gold"], P["skin"]): g(u, v)), eps=0.002 * K, rings=12, segs=56)

    # --- the box, its deck, strings and pegs ------------------------------------------------------------------------
    box(k, P)

    # --- the two feelers: up from the crown in loops, one onto the strings (+X), one beside the face (-X) ----------
    feeler(k, P, [(0.03, -0.35, 0.36), (0.045, -0.33, 0.44), (0.07, -0.24, 0.51), (0.11, -0.1, 0.5),
                  (0.135, 0.0, 0.44), (0.14, 0.02, 0.385)], "earR", 0.0105)
    feeler(k, P, [(-0.03, -0.35, 0.36), (-0.05, -0.35, 0.45), (-0.1, -0.37, 0.5), (-0.16, -0.41, 0.47),
                  (-0.19, -0.43, 0.41), (-0.195, -0.435, 0.38)], "earL", 0.0105)

    if extra:
        extra(k, P, H, HR)

    de = unit(0.6, -0.75, 0.22)
    E = on_ell(H, HR, de)
    k.build(voxel=P["voxel"], smooth=4, band=0.006 * K, wband=0.035 * K)
    look = lidded_eye(P) if P["eyes"] == "lidded" else round_eye(P)
    half = np.array(P.get("eye_half", (0.026, 0.029))) * K
    k.eye_states(eyes_pair(k, E, de, half, look, srgb(P["skin"]), srgb(P["ink"]), line=0.16))
    k.export(out)


if __name__ == "__main__":
    cimbalom(*args())
