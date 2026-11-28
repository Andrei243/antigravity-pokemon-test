# Valvoara (No. 10136, Carpathia) sculpted from nothing on the skeleton and clips of the game's own export of it:
#   blender --background --factory-startup --python sculpts/valvoara.py -- <the export's Valvoara.glb> <out.glb>
#
# Grown from valvisor.py: its pale ghost's colour, its flame drawn up from the head (now her hair), its rusty lamp
# (now with an orange flame's glow and hung on a chain).
# From its picture (docs/art/reference/gemini/carpathia/10136-Valvoara.jpg) and its brief (CP-dex-3-river-and-caves.md,
# 136), the vâlva băilor. Largest first: a long veil of hammered gold leaf falling from her crown over her back and
# sides, darker bronze inside, its hem billowing out behind; a slim pale body whose lower half streams down into
# long wisps that trail back and end in ore dust; a pale flame of hair rising over a gold crown with a peak on the
# brow; a miner's lamp hung on a chain from one hand, the chain running on to the other hand at her waist; veins of
# gold over her body; gold almond eyes, a small smile.
# The export has no sculptor (a small biped by rule): body 0.379, arms (+-0.13, 0, 0.466), head 0.517, legs 0.27.
# The head goes on head, everything else on body: her shoulders stand at 0.6, so the arms' joints would swing them
# from their middles, and her lower body is a stream, not legs (legL and legR carry nothing).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, blade, sheet, hull, outward, srgb, inside, smoothstep, unit, polygon,
                       eye_look, sibling, args)

vv = sibling("valvisor")

GHOST, GHOSTB, HAIR = vv.GHOST, vv.GHOSTB, vv.FLAMEB
VEIL, VEILIN, CROWN, VEIN = "#D9A848", "#8E5E30", "#E8B84A", "#D8A23A"
EYEGOLD, EYEDEEP, EYERIM, GLINT = "#F8CE44", "#E08820", "#5C3A1C", "#FFFFFF"
CHEEK, MOUTH = "#E4B88A", "#7A5A60"
FLAMEGLASS = "#FF9A3C"

HC = np.array([0.0, -0.008, 0.735])
HR = np.array([0.074, 0.068, 0.084])
VOXEL = 0.0055


def on_head(a, z):
    return vv.on_ell(a, z, HC, HR)


def chin(V):
    """Narrows the lower face toward a small pointed chin."""
    V = V.copy()
    t = np.clip(-V[:, 2], 0, 1)
    V[:, 0] *= 1 - 0.32 * t ** 1.5
    V[:, 1] *= 1 - 0.12 * t ** 1.5
    return V


def veil_at(r_in=0.0):
    """The veil's middle surface: u round from her +X front edge over the back to the -X front edge, v down."""
    def at(u, v):
        edge = math.radians(58) + math.radians(40) * v                # the front edges open lower down
        phi = edge + (2 * math.pi - 2 * edge) * u                       # angle from the front toward +X
        z = 0.805 - (0.44 + 0.05 * np.sin(3 * phi + 0.6) + 0.06 * np.clip(np.sin(phi), 0, 1)) * v
        r = 0.068 + 0.125 * v ** 0.9 - r_in
        r = r * (1 + 0.35 * v ** 2 * np.clip(np.sin(phi), 0, 1) * np.clip(np.cos(phi - math.pi / 2), 0, 1))
        cy = 0.014 + 0.07 * v
        cx = 0.03 * v ** 2
        return np.stack([cx + r * np.sin(phi), cy - r * np.cos(phi), z], axis=-1)
    return at


def chain(k, a, b, R, r, sag=0.0, prefix="chain", bone="body"):
    """A chain of links from a to b, sagging by sag in the middle, each link turned a quarter from the last."""
    a, b = np.array(a, float), np.array(b, float)
    n = max(2, int(round(np.linalg.norm(b - a) / (1.7 * R))))
    for i in range(n):
        t0, t1 = i / n, (i + 1) / n
        p0 = a + (b - a) * t0 - np.array([0, 0, sag * 4 * t0 * (1 - t0)])
        p1 = a + (b - a) * t1 - np.array([0, 0, sag * 4 * t1 * (1 - t1)])
        c, t = (p0 + p1) / 2, unit(*(p1 - p0))
        d1 = np.cross(t, (0, 0, 1.0)) if abs(t[2]) < 0.9 else np.cross(t, (1.0, 0, 0))
        d1 /= np.linalg.norm(d1)
        d = d1 if i % 2 == 0 else np.cross(t, d1)
        k.add(f"{prefix}{i}", vv.link(c, d, R, r), vv.RUSTD, bone, fuse=False, surface="metal")


def gold_eye(u, v):
    c = np.tile(srgb(EYEGOLD), (len(u), 1))
    c[((u + 0.05) / 0.34) ** 2 + ((v + 0.12) / 0.46) ** 2 < 1] = srgb(EYEDEEP)
    c[((u - 0.35) / 0.2) ** 2 + ((v - 0.38) / 0.24) ** 2 < 1] = srgb(GLINT)
    c[u * u + v * v > 0.8] = srgb(EYERIM)
    return c


ALMOND = [(-1.0, -0.1), (-0.65, -0.62), (0.0, -0.8), (0.6, -0.55), (1.0, 0.05), (0.8, 0.55), (0.25, 0.85),
          (-0.4, 0.7), (-0.85, 0.35)]
EYE_A, EYE_Z, EYE_HALF = 0.43, 0.742, (0.027, 0.02)

VEINS_FRONT = [[(0.0, 0.64), (0.016, 0.57), (-0.008, 0.5), (0.022, 0.42), (0.008, 0.33), (0.02, 0.24)],
               [(0.016, 0.57), (0.052, 0.53), (0.06, 0.46)],
               [(-0.04, 0.6), (-0.055, 0.52), (-0.03, 0.46)],
               [(-0.008, 0.5), (-0.045, 0.42), (-0.05, 0.32), (-0.03, 0.22)],
               [(0.022, 0.42), (0.06, 0.37), (0.05, 0.28)]]
VEINS_BACK = [[(0.0, 0.62), (0.02, 0.52), (-0.012, 0.42), (0.01, 0.3)], [(0.02, 0.52), (0.055, 0.45)],
              [(-0.012, 0.42), (-0.05, 0.34)]]


def veins(P, lines, front):
    d = np.full(len(P), 9.0)
    for line in lines:
        for (x0, z0), (x1, z1) in zip(line, line[1:]):
            a, b = np.array([x0, z0]), np.array([x1, z1])
            q = np.stack([P[:, 0], P[:, 2]], axis=1)
            t = np.clip(((q - a) @ (b - a)) / ((b - a) @ (b - a)), 0, 1)
            d = np.minimum(d, np.linalg.norm(q - (a + t[:, None] * (b - a)), axis=1))
    side = smoothstep(0.0, -0.02, P[:, 1]) if front else smoothstep(0.0, 0.02, P[:, 1])
    return inside(d - 0.0045, 0.002) * side * inside(P[:, 2] - 0.645) * inside(np.abs(P[:, 0]) - 0.1)


def recolour(ob, graded):
    """Loose pieces coloured point by point (as froslass.py): their points found in the built mesh by place."""
    from mathutils.kdtree import KDTree
    V = np.vstack([g[0] for g in graded]); C = np.vstack([g[1] for g in graded])
    kd = KDTree(len(V))
    for i, v in enumerate(V.tolist()):
        kd.insert(v, i)
    kd.balance()
    me = ob.data
    n = len(me.vertices)
    co = np.empty(n * 3, np.float32); me.vertices.foreach_get("co", co); co = co.reshape(n, 3)
    vc = {}
    for i, p in enumerate(co.tolist()):
        _, j, d = kd.find(p)
        if d < 1e-5:
            vc[i] = C[j]
    loops = np.empty(len(me.loops), np.int32); me.loops.foreach_get("vertex_index", loops)
    attr = me.color_attributes["Color"]
    rgba = np.empty(len(me.loops) * 4, np.float32); attr.data.foreach_get("color", rgba); rgba = rgba.reshape(-1, 4)
    for li, vi in enumerate(loops.tolist()):
        if vi in vc:
            rgba[li, :3] = vc[vi]
    attr.data.foreach_set("color", rgba.ravel())


def valvoara(src, out_path):
    k = Kit(src, coat="skin")

    # --- the head and her flame of hair --------------------------------------------------------------------------
    k.add("head", ell(HC, HR, deform=chin), GHOST, "head")
    # a flame of hair swept toward +X, its licks curling back up at their tips
    k.add("hair", tube([(0, 0.02, 0.775), (0.012, 0.032, 0.835), (0.012, 0.04, 0.88), (0.028, 0.05, 0.908),
                        (0.045, 0.056, 0.918)], [0.074, 0.056, 0.034, 0.018, 0.009], seg=24), HAIR, "head")
    vv.lick(k, "lickR", [(0.03, 0.02, 0.8), (0.068, 0.03, 0.835), (0.085, 0.038, 0.87), (0.08, 0.042, 0.895)],
            [0.046, 0.032, 0.018, 0.008], HAIR, "head")
    vv.lick(k, "lickL", [(-0.03, 0.022, 0.805), (-0.062, 0.032, 0.84), (-0.07, 0.038, 0.868), (-0.062, 0.042, 0.885)],
            [0.042, 0.028, 0.016, 0.008], HAIR, "head")
    for s, tag in ((1, "R"), (-1, "L")):
        vv.lick(k, "lickback" + tag, [(0.02 * s, 0.05, 0.785), (0.04 * s, 0.09, 0.815), (0.05 * s, 0.115, 0.845),
                                      (0.045 * s, 0.125, 0.865)], [0.04, 0.028, 0.015, 0.008], HAIR, "head",
                squash=(1, 0, 0))
    # the crown: a gold band over the brow, a peak in the middle and two small ones beside it
    band = []
    for t in np.linspace(-1.25, 1.25, 11):
        band.append((0.064 * math.sin(t), HC[1] - 0.062 * math.cos(t), 0.79 + 0.012 * t * t))
    k.add("crown", tube(band, [0.014] * len(band), seg=10, per=4), CROWN, "head", fuse=False, surface="metal")
    for x, h, w in ((0.0, 0.052, 0.022), (0.036, 0.03, 0.013), (-0.036, 0.03, 0.013)):
        yb = HC[1] - math.sqrt(max(0.062 ** 2 - x * x, 1e-6)) - 0.006
        pts = [(x - w, yb, 0.782), (x + w, yb, 0.782), (x - w, yb + 0.016, 0.782), (x + w, yb + 0.016, 0.782),
               (x, yb + 0.006, 0.782 + h)]
        k.add("peak" + str(int(x * 1000)), hull(pts), CROWN, "head", fuse=False, surface="metal")

    # --- the body ------------------------------------------------------------------------------------------------
    k.add("neck", tube([(0, 0.0, 0.58), (0, 0.004, 0.67)], [0.03, 0.027], seg=16), GHOST, "head")
    k.add("chest", ell((0, 0.0, 0.565), (0.074, 0.058, 0.075)), GHOST, "body")
    for s in (1, -1):
        k.add("shoulder" + ("R" if s > 0 else "L"), ell((0.066 * s, 0.0, 0.6), (0.032, 0.033, 0.03)), GHOST, "body")
    k.add("waist", ell((0, 0.006, 0.47), (0.064, 0.052, 0.075)), GHOST, "body")
    k.add("hips", ell((0, 0.012, 0.4), (0.082, 0.068, 0.065)), GHOST, "body")
    k.add("skirt", tube([(0, 0.014, 0.4), (0.005, 0.035, 0.34)], [0.08, 0.07], seg=24), GHOST, "body")
    # the stream: long wisps from the skirt's hem, trailing down and back toward +X, ending in ore dust
    tips = []
    for i, (ang, length, lift, r0) in enumerate(((-70, 1.0, 0.02, 0.042), (-25, 1.1, 0.0, 0.046), (20, 1.0, 0.03, 0.044),
                                                 (70, 1.15, 0.01, 0.042), (125, 1.05, 0.05, 0.04),
                                                 (180, 1.1, 0.04, 0.042), (235, 0.95, 0.06, 0.04))):
        a = math.radians(ang)
        s0 = np.array([0.04 * math.sin(a), 0.03 - 0.04 * math.cos(a), 0.37])
        out_ = np.array([math.sin(a), -math.cos(a), 0.0]) * 0.04
        drift = np.array([0.09, 0.2, 0.0]) * length
        pts = [s0, s0 + out_ * 0.6 + drift * 0.12 + [0, 0, -0.09], s0 + out_ + drift * 0.32 + [0, 0, -0.18],
               s0 + out_ * 1.1 + drift * 0.58 + [0, 0, -0.26 + lift * 0.3], s0 + out_ * 1.2 + drift * 0.85
               + [0, 0, -0.31 + lift * 0.6], s0 + out_ * 1.25 + drift * 1.08 + [0, 0, -0.335 + lift]]
        k.add(f"wisp{i}", tube(pts, [r0, r0 * 0.85, r0 * 0.68, r0 * 0.5, r0 * 0.36, 0.013], seg=16), GHOST, "body")
        tips.append(pts[-1])
    # --- the arms: +X out to the chain at her side, -X bent with its hand at her waist -----------------------------
    k.add("armR", tube([(0.085, -0.005, 0.6), (0.112, -0.045, 0.5), (0.168, -0.066, 0.52)], [0.028, 0.024, 0.021],
                       seg=16), GHOST, "body")
    k.add("handR", ell((0.178, -0.07, 0.522), (0.024, 0.02, 0.026)), GHOST, "body")
    k.add("armL", tube([(-0.085, -0.005, 0.6), (-0.118, -0.03, 0.5), (-0.062, -0.08, 0.46)], [0.028, 0.024, 0.021],
                       seg=16), GHOST, "body")
    k.add("handL", ell((-0.05, -0.088, 0.457), (0.023, 0.02, 0.024)), GHOST, "body")

    # --- the veil, gold outside and bronze inside ----------------------------------------------------------------
    # hammered leaf: each sheet's points coloured by a mottle of gold, pale gold and copper, set after the build
    graded = []
    uu, vv_ = np.meshgrid(np.linspace(0, 1, 72), np.linspace(0, 1, 30), indexing="ij")
    def mottle(base, light, dark):
        m = (np.sin(uu * 29.3 + vv_ * 7.1 + np.sin(vv_ * 13.7) * 2.1) * np.sin(vv_ * 19.9 - uu * 11.3 + np.sin(uu * 23.1))
             + 0.5 * np.sin(uu * 61.7 - vv_ * 37.3 + np.sin(uu * 41.9 + vv_ * 9.7) * 1.7)).ravel()
        c = np.tile(srgb(base), (len(m), 1))
        hi, lo = smoothstep(0.45, 1.0, m) * 0.7, smoothstep(-0.45, -1.0, m) * 0.6
        c = c * (1 - hi[:, None]) + srgb(light)[None] * hi[:, None]
        c = c * (1 - lo[:, None]) + srgb(dark)[None] * lo[:, None]
        return np.vstack([c, c])
    for name, r_in, thick, cols in (("veil", 0.0, 0.014, (VEIL, "#F0CE6E", "#B47436")),
                                     ("veilin", 0.017, 0.01, (VEILIN, "#B07A3C", "#6E4424"))):
        V, F = sheet(veil_at(r_in), 72, 30, thick)
        C = mottle(*cols)
        V2, F2 = outward((V, F))
        k.add(name, (V2, F2), cols[0], "body", fuse=False, surface="metal")
        graded.append((V2, C))

    # --- the lamp on its chain -----------------------------------------------------------------------------------
    top = vv.lamp(k, (0.21, -0.07, 0.165), 0.24, "body", glass=FLAMEGLASS, w=0.056)
    chain(k, (0.182, -0.072, 0.505), top + np.array([0, 0, 0.012]), 0.02, 0.0105, prefix="chainA")
    chain(k, (0.17, -0.078, 0.51), (-0.042, -0.1, 0.455), 0.02, 0.0105, sag=0.05, prefix="chainB")

    # --- ore dust at the wisps' ends: nuggets of gold and copper ----------------------------------------------------
    rs = np.random.RandomState(136)
    for i, t in enumerate(tips):
        for j in range(2 if i % 2 == 0 else 1):
            c = t + rs.uniform(-0.02, 0.02, 3) + np.array([0, 0.01, 0.012 * j])
            c[2] = max(c[2], 0.018)
            pts = c + rs.uniform(-1, 1, (7, 3)) * 0.015
            k.add(f"nugget{i}{j}", hull(pts), CROWN if (i + j) % 3 else VEILIN, "body", fuse=False, surface="metal")

    # --- colours on the fused body -------------------------------------------------------------------------------
    k.paint(lambda P: smoothstep(0.2, 0.05, P[:, 2]) * 0.7, GHOSTB)
    k.paint(lambda P: veins(P, VEINS_FRONT, True), VEIN)
    k.paint(lambda P: veins(P, VEINS_BACK, False), VEIN)
    dust = np.array([t + rs.uniform(-0.03, 0.03, 3) * [1, 1, 0.4] + [0, -0.02, 0.03] for t in tips for _ in range(3)])
    k.paint(lambda P: np.clip(sum(inside(np.linalg.norm(P - d, axis=1) - 0.009, 0.003) for d in dust), 0, 1)
            * inside(P[:, 2] - 0.16), VEIN)

    # cheeks and the small smile: every state
    for s in (1, -1):
        p, n = on_head(0.75 * s, 0.712)
        k.decal(p, n, (0.012, 0.008), CHEEK, eps=0.002, rings=8, segs=36)
    p, n = on_head(0.0, 0.682)
    def smile(u, v):
        c = np.tile(srgb(GHOST), (len(u), 1))
        c[(np.abs(v - (-0.25 + 0.55 * u * u)) < 0.3) & (np.abs(u) < 0.85)] = srgb(MOUTH)
        return c
    k.decal(p, n, (0.02, 0.012), smile, eps=0.002, rings=10, segs=44)

    body = k.build(voxel=VOXEL, smooth=4, band=0.006, wband=0.04)
    recolour(body, graded)
    vv.board(body)

    def lay(state):
        for s in (1, -1):
            p, n = on_head(EYE_A * s, EYE_Z)
            right = np.cross((0, 0, 1.0), n); right /= np.linalg.norm(right); vu = np.cross(n, right)
            a = math.radians(14)
            up = vu * math.cos(a) - s * right * math.sin(a)       # tilted up toward the outer corner
            look = eye_look(state, gold_eye, srgb(GHOST), srgb(EYERIM), line=0.16)
            if s > 0:
                k.decal(p, n, EYE_HALF, look, up=up, eps=0.0022, rings=20, segs=56, outline=polygon(ALMOND))
            else:
                k.decal(p, n, EYE_HALF, lambda u, v, f=look: f(-u, v), up=up, eps=0.0022, rings=20, segs=56,
                        outline=polygon([(-x, y) for x, y in ALMOND]))
    k.eye_states(lay)
    k.export(out_path)


if __name__ == "__main__":
    valvoara(*args())
