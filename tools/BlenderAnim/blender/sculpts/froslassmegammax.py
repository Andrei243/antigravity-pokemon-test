# Mega Dynamax Froslass (Froslass-Mega-Mmax), a form of this project's own, sculpted as Froslass-Mega's script with
# the Mega Dynamax form's additions laid on its kit just before it is meshed (sculptkit.py):
#   blender --background --factory-startup --python sculpts/froslassmegammax.py -- <the export's Froslass-Mega-Mmax.glb> <out.glb>
# The Mega whole and unchanged (froslassmega.py builds as it is read: its build runs with this script's arguments), and
# over it, from the picture (docs/art/reference/gemini/megadynamax/Mega-Dynamax-Froslass.jpg), largest first: a broad
# crimson ribbon running out of the sash in a great loop to the right and winding down round the gown to the clouds
# (the sash itself turned the ribbon's crimson); a crown of glowing ice crystals about the horns; clouds of Max energy
# about the hem; the Key Stone's emblem, a pink-red disc in a snowflake of ice, at the throat above the sash.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
import sculptkit
from sculptkit import Kit, ell, tube, lathe, ring_band, srgb, unit, sibling, outward

CRIMSON, DEEP, VIOLET, DVIOLET, CYAN = "#AC223E", "#701630", "#6C3EA0", "#42246E", "#4EE2EC"
RIBBON, ICE, ICEHI = "#96203A", "#86E4F0", "#C8F6FA"
STONE, STONERING = "#FF8A6A", "#F2508A"

# From froslassmega.py (read, not imported: it builds as it is read): the head's centre HC (0, -0.01, 1.03) at F 0.8,
# radii (0.108, 0.094, 0.1); the horns' bases at (+-0.04, 0.005, 1.1); the chest (0, 0, 0.84) r (0.055, 0.05, 0.065);
# the sash (0, -0.005, 0.79) r (0.095, 0.082, 0.05) in #D8503A; the gown's radii 0.066 at z 0.62, 0.076 at 0.45,
# 0.094 at 0.28, 0.125 at 0.2, 0.15 at 0.16; its snow cloud of puffs about (0, -0.05) out to 0.25; the voxel 0.0055.
SASH = "#D8503A"


def ribbon(pts, width, thick, per=6, seg=18):
    """A flat band along a smooth curve through pts, its broad face turned away from the body's upright axis."""
    pts = np.array(pts, float)
    ext = np.vstack([2 * pts[0] - pts[1], pts, 2 * pts[-1] - pts[-2]])
    P = []
    for i in range(len(pts) - 1):
        p0, p1, p2, p3 = ext[i], ext[i + 1], ext[i + 2], ext[i + 3]
        for j in range(per):
            t = j / per
            P.append(0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t
                            + (-p0 + 3 * p1 - 3 * p2 + p3) * t ** 3))
    P.append(pts[-1])
    P = np.array(P)
    T = np.gradient(P, axis=0)
    T /= np.linalg.norm(T, axis=1, keepdims=True)
    ang = np.linspace(0, 2 * math.pi, seg, endpoint=False)
    cs, sn = np.cos(ang), np.sin(ang)
    sx = np.sign(cs) * np.abs(cs) ** 0.35
    sy = np.sign(sn) * np.abs(sn) ** 0.35
    V, rows = [], []
    for i, p in enumerate(P):
        n = np.array([p[0], p[1] + 0.03, 0.0])
        n = n - T[i] * (n @ T[i]); n /= np.linalg.norm(n)
        b = np.cross(T[i], n)
        w = width * (0.75 + 0.25 * min(1, (len(P) - 1 - i) / 4))
        rows.append(len(V))
        V.extend(p + (w / 2) * sx[j] * b + (thick / 2) * sy[j] * n for j in range(seg))
    F = []
    for r0, r1 in zip(rows, rows[1:]):
        for j in range(seg):
            F.append([r0 + j, r0 + (j + 1) % seg, r1 + (j + 1) % seg, r1 + j])
    s, e = len(V), len(V) + 1
    V.append(P[0] - T[0] * thick * 0.3); V.append(P[-1] + T[-1] * thick * 0.3)
    for j in range(seg):
        F.append([s, rows[0] + (j + 1) % seg, rows[0] + j])
        F.append([e, rows[-1] + j, rows[-1] + (j + 1) % seg])
    return outward((np.array(V), F))


def crystal(base, d, length, r, seg=6):
    """A six-sided crystal: a prism that narrows a little and ends in a point."""
    return lathe(base, d, [(0, -r), (r, 0), (r * 0.88, length * 0.72), (0, length)], seg=seg)


def maxed(k):
    """What the Mega Dynamax form lays over its Mega: called with the Mega's kit just before it is meshed."""
    # The sash turned the ribbon's crimson (the part and its band of paint), its shape untouched
    old = srgb(SASH)
    for p in k.parts:
        if p["name"] == "sash":
            p["color"] = srgb(RIBBON)
    k.paints[:] = [(m, srgb(RIBBON) if np.allclose(c, old) else c, on) for m, c, on in k.paints]

    # ---- the ribbon: out of the sash on the right, a great loop out past the sleeve, then down round the gown ------
    # (in front of the right sleeve going out, behind it coming back, between the left sleeve and the gown, then
    # across the front and down on the right into the clouds)
    path = [(-0.075, -0.06, 0.79), (0.0, -0.098, 0.793), (0.1, -0.108, 0.795), (0.2, -0.125, 0.79), (0.37, -0.11, 0.74), (0.43, -0.03, 0.68),
            (0.39, 0.08, 0.625), (0.26, 0.15, 0.59), (0.09, 0.15, 0.56), (-0.06, 0.11, 0.53), (-0.12, 0.0, 0.5),
            (-0.11, -0.11, 0.46), (0.0, -0.15, 0.42), (0.15, -0.15, 0.37), (0.27, -0.11, 0.31), (0.32, -0.02, 0.24),
            (0.31, 0.07, 0.17)]
    k.add("ribbon", ribbon(path, 0.11, 0.038), RIBBON, "body", fuse=False, surface="cloth")

    # ---- the crown: glowing crystals of ice in a ring about the horns, the tallest at the back ------------------------
    rng = np.random.default_rng(5)
    for i in range(11):
        a = -math.pi * 0.85 + 1.7 * math.pi * i / 10            # round the back of the crown, the face left clear
        x, y = 0.07 * math.sin(a), 0.012 - 0.055 * math.cos(a)
        back = 0.5 - 0.5 * math.cos(a)                           # 0 at the front, 1 at the back
        base = np.array([x, y, 1.085 + 0.02 * back])
        d = unit(x * 4.0, y * 2.0 + 0.15, 1.0)
        L = 0.08 + 0.09 * back + 0.02 * rng.uniform(-1, 1)
        k.add(f"crown{i}", crystal(base, d, L, 0.019), ICE, "head", fuse=False, glow=ICE, surface="shell")
    k.add("crowntop", crystal(np.array([0, 0.035, 1.105]), unit(0, 0.12, 1), 0.2, 0.024), ICEHI, "head", fuse=False,
          glow=ICE, surface="shell")

    # ---- the emblem at the throat, over the sash: a pink-red disc in a ring, ice points round it ------------------------
    c = np.array([-0.012, -0.056, 0.858])
    d = unit(0, -1, 0.22)
    k.add("emblem", lathe(c, d, [(0, -0.002), (0.024, -0.002), (0.024, 0.008), (0, 0.011)]), STONE, "body",
          fuse=False, glow=STONE)
    k.add("emblemring", ring_band(c + d * 0.002, d, 0.031, 0.011, thick=0.009), STONERING, "body", fuse=False,
          glow=STONERING)
    a_, b_ = sculptkit.axes(d)
    for j in range(6):
        t = 2 * math.pi * j / 6 + math.pi / 2
        e = math.cos(t) * a_ + math.sin(t) * b_
        q = c + e * 0.03 - d * 0.004
        k.add(f"emblempt{j}", sculptkit.hull([q + b_ * 0.0 + np.cross(d, e) * 0.012, q - np.cross(d, e) * 0.012,
                                              q + e * 0.03, q + d * 0.012, q - d * 0.002]),
              ICE, "body", fuse=False, glow=ICE)

    # ---- the clouds: fused mounds in a ring about the hem's snow, crimson and violet by turns, clear of it ------------
    rnd = np.random.default_rng(11)
    n = 10
    for i in range(n):
        a = 2 * math.pi * (i + 0.3) / n
        cx, cy = 0.325 * math.cos(a), -0.05 + 0.28 * math.sin(a)
        col = (CRIMSON, VIOLET, DEEP, DVIOLET)[i % 4]
        other = (VIOLET, CRIMSON, DVIOLET, CRIMSON)[i % 4]
        for j in range(4):
            s = (0.07, 0.056, 0.048, 0.036)[j] * (1 + 0.12 * rnd.uniform(-1, 1))
            t = a + (0.0, 0.2, -0.2, 0.06)[j]
            rx, ry = 0.325 + (0.0, 0.02, 0.015, -0.02)[j], 0.28 + (0.0, 0.02, 0.015, -0.02)[j]
            p = (rx * math.cos(t), -0.05 + ry * math.sin(t), s * (0.85, 0.9, 0.85, 2.6)[j])
            k.add(f"mcloud{i}_{j}", ell(p, (s, s, s * 0.88)), other if j == 2 else col, "root")
        if i % 3 == 1:
            # a cyan curl riding on the mound: a short hooked arc, glowing
            q = np.array([cx, cy, 0.11])
            o = unit(math.cos(a), math.sin(a), 0)
            tg = np.cross((0, 0, 1.0), o)
            k.add(f"curl{i}", tube([q - tg * 0.05, q + o * 0.03 + (0, 0, 0.03), q + tg * 0.04 + (0, 0, 0.045),
                                    q + tg * 0.01 + o * 0.015 + (0, 0, 0.06)], [0.017, 0.019, 0.017, 0.015], seg=10),
                  CYAN, "root", fuse=False, glow=CYAN)


_build, _laid = Kit.build, []
def build(self, *a, **kw):
    if not _laid:               # the first build is the body's; the kit builds the eyes' five states after it
        _laid.append(True)
        maxed(self)
    return _build(self, *a, **kw)
Kit.build = build

sibling("froslassmega")        # builds as it is read, with this script's own arguments
