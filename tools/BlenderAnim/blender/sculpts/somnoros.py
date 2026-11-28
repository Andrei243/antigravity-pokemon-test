# Somnoros sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown from
# somnel.py's frame (its flat head, its wide mouth, its sleepy eyes on bulges, its whiskers, its round tail):
#   blender --background --factory-startup --python sculpts/somnoros.py -- <the export's Somnoros.glb> <out.glb>
#
# From its picture (No. 10118, the river giant catfish): Somnel grown huge and long, mottled: the back mostly black
# with olive patches, the flanks olive blotched with black, the belly a dull tan spotted dark; the same vast mouth across
# its front, the same heavy-lidded eyes on bulges, now with a glint in them; whiskers like ropes, two from the mouth's
# corners trailing on the ground and out, two short under the chin; a nightcap of river weed, a mat on its crown with
# thick strands hanging down over its brow and the sides of its head, one clump down a cheek; a long body whose tail
# curls up behind and hooks forward, a ragged dark fin along the curl's outer edge and a ribbon of fin along the back.
# Joints: body (0, 0.04, 0.321), head (0, -0.2, 0.321), tail (0, 0.3, 0.321), finR/L (+-0.18, -0.08, 0.201).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, tube, ell, slab, sheet, srgb, inside, unit, lumpy, outward, args, sibling

sn = sibling("somnel")

OLIVE, BLACK, BELLY, WHISKER, WEED, WEEDL, FIN = ("#635C3C", "#22201A", "#958A60", "#5A553A", "#223A1A", "#2E4A22",
                                                  "#2E2C20")
sn.OLIVE = OLIVE

HC, HR = np.array([0.0, -0.25, 0.32]), np.array([0.285, 0.22, 0.19])
ZM = 0.295
BULGE = np.array([0.19, -0.315, 0.45])
EYE_N = unit(0.5, -0.72, 0.45)
EYE_HALF = (0.046, 0.043)

# The tail's curl, drawn in a plane that runs back and some 35 degrees out to +X: (back, up) and a radius each
CURL_B, CURL_U = unit(0.57, 0.82, 0), np.array([0, 0, 1.0])
CURL = [(-0.06, 0.0, 0.16), (0.18, 0.01, 0.13), (0.36, 0.04, 0.095), (0.46, 0.1, 0.072), (0.5, 0.175, 0.058),
        (0.46, 0.23, 0.045), (0.39, 0.245, 0.036)]
CURL_O = np.array([0.0, 0.26, 0.32])


def curl_point(s, h):
    return CURL_O + np.multiply.outer(s, CURL_B) + np.multiply.outer(h, CURL_U)


def body(k):
    k.add("head", ell(HC, HR, deform=sn.flat_head(0.25)), OLIVE, "head")
    k.add("bulge", ell(BULGE, (0.058, 0.055, 0.05)), OLIVE, "head", mirror=True)
    # behind the wide head the body runs on and tapers into the tail's curl as one fish
    k.add("trunk", tube([(0, -0.15, 0.31), (0, 0.0, 0.31), (0, 0.14, 0.315), (0, 0.24, 0.32)],
                        [0.2, 0.2, 0.175, 0.15], seg=28), OLIVE, "body")
    pts = [curl_point(s, h) for s, h, _ in CURL]
    k.add("tailcurl", tube(pts, [r for _, _, r in CURL], seg=24, per=10), OLIVE, "tail")
    # paddles at the sides and small fins behind, as Somnel's
    # the pectorals on the flanks behind the head, lying back along the body; small pelvics far back underneath
    k.add("pec", sn.pectoral((0.2, -0.06, 0.29), 0.2, 0.075, 0.02), FIN, "fin{s}", fuse=False, mirror=True,
          surface="skin")
    k.add("pel", ell((0.1, 0.18, 0.15), (0.016, 0.05, 0.038), rot=(15, 0, -45)), FIN, "body", mirror=True)


def fins(k):
    m = dict(fuse=False, surface="skin")
    # the ragged fin along the curl's outer edge, standing out from the tube in the curl's own plane
    P = np.array([curl_point(s, h) for s, h, _ in CURL])
    R = np.array([r for _, _, r in CURL])
    # a finer line along the curl from its rise to its end
    t = np.linspace(1.6, len(CURL) - 1.05, 40)
    idx = np.arange(len(CURL))
    S = np.interp(t, idx, [c[0] for c in CURL]); H = np.interp(t, idx, [c[1] for c in CURL])
    Rt = np.interp(t, idx, R)
    ds, dh = np.gradient(S), np.gradient(H)
    nl = np.hypot(ds, dh)
    os_, oh = dh / nl, -ds / nl                       # the outer side of a curl that turns up and forward
    def at(u, v):
        i = u * (len(t) - 1)
        s = np.interp(i, np.arange(len(t)), S); h = np.interp(i, np.arange(len(t)), H)
        r = np.interp(i, np.arange(len(t)), Rt)
        a, b = np.interp(i, np.arange(len(t)), os_), np.interp(i, np.arange(len(t)), oh)
        w = (0.6 * r + (0.05 + 0.012 * np.sin(u * 40)) * v) * np.sin(np.pi * np.clip(u * 0.9 + 0.08, 0, 1)) ** 0.3
        return curl_point(s + a * w, h + b * w)
    k.add("curlfin", outward(sheet(at, 40, 4, 0.02)), FIN, "tail", **m)
    # the ribbon along the back, low, leaning to +X so that the front camera sees its face
    def ribbon(u, v):
        y = -0.02 + 0.3 * u
        top = 0.31 + 0.2 * np.sqrt(np.clip(1 - ((y - 0.04) / 0.3) ** 2, 0, 1)) * (1 - 0.15 * np.clip(y, 0, 1))
        hgt = (0.012 + (0.04 + 0.008 * np.sin(u * 30)) * v) * np.sin(np.pi * (0.03 + 0.94 * u)) ** 0.5
        return np.stack([0.35 * hgt, y, top - 0.012 + hgt], axis=-1)
    k.add("dorsal", outward(sheet(ribbon, 30, 3, 0.02)), FIN, "body", **m)
    # the tail's own round fan at the curl's end, pointing forward and up
    s, h, _ = CURL[-1]
    end = curl_point(s, h)
    a = math.radians(35)
    back = np.array([math.sin(a), math.cos(a), 0])
    out = []
    for th in np.linspace(-math.pi / 2, math.pi / 2, 21):
        out.append((0.065 * math.cos(th) * (1 - 0.12 * math.cos(3 * th) ** 2), 0.06 * math.sin(th)))
    out += [(-0.01, 0.025), (-0.01, -0.025)]
    k.add("caudal", slab(out, end, unit(-0.4 * back[0], -0.4 * back[1], 0.9), back, 0.02), FIN, "tail", **m)


def markings(k):
    rs = np.random.RandomState(10118)
    # blotches over the body and the tail, kept off the eyes' bulges and the mouth
    C, Rr = [], []
    for i in range(70):
        y = rs.uniform(-0.35, 0.42)
        ang = rs.uniform(-0.3, math.pi + 0.3)
        side = 1 if rs.rand() < 0.5 else -1
        C.append((side * 0.26 * math.cos(ang), y, 0.32 + 0.22 * math.sin(ang))); Rr.append(rs.uniform(0.03, 0.06))
    for i in range(30):
        s = rs.uniform(0.05, 1.0)
        tt = s * (len(CURL) - 1)
        idx = np.arange(len(CURL))
        p = curl_point(np.interp(tt, idx, [c[0] for c in CURL]), np.interp(tt, idx, [c[1] for c in CURL]))
        C.append(tuple(p + rs.normal(size=3) * 0.06)); Rr.append(rs.uniform(0.025, 0.045))
    for p, r in (((0.1, -0.2, 0.5), 0.05), ((-0.14, -0.17, 0.48), 0.045), ((0.22, -0.12, 0.42), 0.05),
                 ((-0.24, -0.2, 0.38), 0.045), ((0.0, -0.08, 0.52), 0.06), ((0.27, -0.25, 0.3), 0.035)):
        C.append(p); Rr.append(r)
    C, Rr = np.array(C), np.array(Rr)
    def blotch(P):
        d = np.linalg.norm(P[:, None, :] - C[None], axis=2) / Rr[None] - 1
        m = inside(d.min(1), 0.15)
        for s in (1, -1):
            m *= 1 - inside(np.linalg.norm(P - BULGE * np.array([s, 1, 1]), axis=1) - 0.085, 0.01)
        m *= 1 - inside(P[:, 1] + 0.4, 0.01)          # nothing on the mouth's front
        return m
    def back(P):
        zc = 0.32 + 0.04 * np.clip(P[:, 1], 0, 0.3)
        m = inside(zc + 0.1 + 0.025 * np.sin(P[:, 1] * 22) - P[:, 2], 0.01) * inside(-0.2 - P[:, 1], 0.02)
        return m
    def belly(P):
        zb = 0.235 - 0.03 * np.clip((P[:, 1] + 0.05) / 0.35, 0, 1)
        return inside(P[:, 2] - zb, 0.005) * inside(P[:, 1] - 0.45, 0.01)
    def chin(P):
        return inside(P[:, 2] - (ZM - 0.03), 0.005) * inside(P[:, 1] - (HC[1] + 0.08), 0.01)
    k.paint(back, BLACK, on=["trunk", "tailcurl"])
    k.paint(belly, BELLY, on=["trunk", "head"])
    k.paint(chin, BELLY, on=["head"])
    k.paint(blotch, BLACK)
    k.paint(lambda P: blotch(P) * inside(P[:, 2] - 0.2, 0.01), "#4E4430")      # on the belly the spots are brown
    sn.mouth(k, HC, ZM, 64.0, 0.046, 0.024, -0.03)


def whiskers(k):
    sn.whisker(k, "whisker", [(0.265, -0.37, 0.29), (0.31, -0.405, 0.245), (0.35, -0.42, 0.16), (0.4, -0.385, 0.095),
                              (0.465, -0.29, 0.082), (0.515, -0.14, 0.085), (0.535, 0.03, 0.095)], 0.015, 0.0105,
               colour=WHISKER)
    sn.whisker(k, "chin", [(0.085, -0.395, 0.165), (0.092, -0.412, 0.135), (0.1, -0.418, 0.112)], 0.011, 0.0095,
               colour=WHISKER)


def head_surface(th, z, push):
    k_ = math.sqrt(max(1 - ((z - HC[2]) / HR[2]) ** 2, 0.0))
    return np.array([(HR[0] + push) * math.sin(th) * k_, HC[1] - (HR[1] + push) * math.cos(th) * k_, z])


def weed(k):
    m = dict(fuse=False, surface="leaf")
    # the mat on the crown, lumpy
    k.add("weedmat", ell((0, -0.24, 0.47), (0.16, 0.13, 0.04), deform=lumpy(6, 3, 0.3), sub=3), WEED, "head", **m)
    # strands hanging from its edge: short over the brow between the eyes, long down the sides, none over the eyes
    locks = [(0, 0.44, 0.0), (12, 0.45, 0.0), (-12, 0.45, 0.0), (68, 0.33, 0.02), (-68, 0.33, 0.02), (84, 0.28, 0.0),
             (-84, 0.28, 0.0), (100, 0.27, 0.02), (-100, 0.27, 0.02), (118, 0.3, 0.0), (-118, 0.3, 0.0),
             (136, 0.34, 0.0), (-136, 0.34, 0.0), (150, 0.38, 0.0), (-150, 0.38, 0.0)]
    for i, (deg, zlow, wob) in enumerate(locks):
        th = math.radians(deg)
        zs = np.linspace(0.47, zlow, 6)
        pts = [head_surface(th + wob * j, z, 0.01 + 0.006 * j) for j, z in enumerate(zs)]
        pts[-1] = pts[-1] + np.array([math.sin(th), -math.cos(th), 0]) * 0.02          # the tip lifting clear
        col = WEED if i % 2 else WEEDL
        k.add(f"weed{i}", tube(pts, [0.017, 0.016, 0.015, 0.013, 0.011, 0.009], seg=10, per=4), col, "head", **m)
    # strands lying over the mat from the crown out to its edge, so that it reads as weed and not a plate
    for i in range(11):
        th = math.radians(-160 + 32 * i + 7 * math.sin(i * 2.3))
        d = np.array([math.sin(th), -math.cos(th), 0])
        c = np.array([0, -0.24, 0.0])
        pts = [c + d * 0.02 + (0, 0, 0.515), c + d * 0.08 + (0, 0, 0.51), c + d * 0.13 + (0, 0, 0.495),
               head_surface(th, 0.47, 0.018)]
        k.add(f"crownweed{i}", tube(pts, [0.016, 0.016, 0.015, 0.013], seg=10, per=4), WEEDL if i % 2 else WEED,
              "head", **m)
    # a clump down the +X cheek, behind the mouth's corner
    th = math.radians(74)
    pts = [head_surface(th, z, 0.012 + 0.004 * j) for j, z in enumerate(np.linspace(0.34, 0.21, 5))]
    for j, dx in enumerate((0.0, 0.02, -0.02)):
        q = [p + np.array([0, dx, 0]) for p in pts]
        k.add(f"cheekweed{j}", tube(q, [0.015, 0.014, 0.012, 0.011, 0.009], seg=10, per=4), WEEDL if j else WEED,
              "head", **m)


def nostrils(k):
    k.decal((0.07, -0.455, 0.385), unit(0.2, -0.8, 0.55), (0.013, 0.01), "#1C1E14", eps=0.0025, rings=6, segs=24,
            mirror=True)


def eyes(k, state):
    p = BULGE + EYE_N * 0.05
    sn.eye_pair(k, state, p, EYE_N, EYE_HALF, srgb(OLIVE), glint=True)


def somnoros(src, out):
    k = Kit(src, coat="scales")
    body(k)
    fins(k)
    markings(k)
    whiskers(k)
    weed(k)
    nostrils(k)
    k.build(voxel=0.0072, smooth=5, band=0.006, wband=0.06)
    sn.board(k)
    k.eye_states(lambda state: eyes(k, state))
    k.export(out)


if __name__ == "__main__":
    somnoros(*args())
