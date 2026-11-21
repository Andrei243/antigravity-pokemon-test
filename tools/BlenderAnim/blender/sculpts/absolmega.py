# Mega Absol sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/absolmega.py -- <the export's Absol-Mega.glb> <out.glb>
# Grown from absol.py (which builds as it is read, so its frame is copied here: the same body, legs, ruff, hood, face
# paint and eyes); absolmegaz.py builds Absol-Mega-Z by absol_mega(z=True). From the pictures, the masses largest first:
#   1. a pair of great white feathered wings out of the back, rising up and back in a V, five thick feathers each over
#      a fused root of fur, lavender in their middle;
#   2. the body and legs of Absol, with large spiky cuffs of fur at the forearms and the hind shins;
#   3. the hood, its fur now falling as one broad lock over the face's +X side down to the chest, hiding that eye: one
#      red eye shows, on -X;
#   4. a short thick crescent horn on the hood's -X side, curving up and back, and a small spike on the +X side;
#   5. a broad dark oval on the forehead, a lighter lens in it;
#   6. a dark tail like a bat's wing, held back level, its trailing edge notched into points.
# Mega Absol Z, from its own pictures: the same frame in navy, a lighter slate face with an open mouth and a fang,
# four great crimson plumes each side rising like flame from navy roots, an angular bent horn, a lock with a red tip,
# a spiny pale-blue tail like a branch, thin blue spurs on the legs, the legs spread wide in a crouch.
# The export's joints: body (0, 0.04, 0.4), head (0, -0.2, 0.55), tail (0, 0.26, 0.44), legs (+-0.08, -0.12, 0.32),
# hinds (+-0.08, 0.18, 0.32), wings (+-0.06 or +-0.05, 0, 0.5).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, sheet, srgb, inside, unit, eye_look, polygon, lumpy, hull, args, tri

k = None
Z = False
C = {}

# --- the frame (absol.py's) --------------------------------------------------------------------------------------
H = np.array([0, -0.25, 0.64])
HR = np.array([0.12, 0.126, 0.138])
LEG_X = 0.086
FORE_Y, HIND_Y = -0.12, 0.18
BODY_Z = 0.405


def palette(z):
    if not z:
        return dict(FUR="#ECEFF6", SHADE="#C9CEEA", SLATE="#3A548C", FACE="#6079B0", MARK="#34508A", LENS="#5A73AE",
                    CLAW="#3A548C", HORN="#3A548C", TAIL="#3A548C", RED="#E2283A", PUPIL="#1A0A10", SCLERA="#F4F2EE",
                    INK="#141A2C", LINE="#1E2A48")
    return dict(FUR="#1B2A47", SHADE="#26385C", SLATE="#22355E", FACE="#4D6CA8", MARK="#5F84C4", LENS="#5F84C4",
                CLAW="#6A8CC8", HORN="#4A73B8", TAIL="#4A73B8", RED="#E2283A", PUPIL="#1A0A10", SCLERA="#F4F2EE",
                INK="#0C1222", LINE="#C8D4EE", PLUME="#E8245C", PLUMETIP="#F25A82", MOUTH="#7A1C2E", FANG="#F2F2F0")


# --- body, legs, ruff, head (absol.py's, the legs spread in Z's crouch) ---------------------------------------------
def add_body():
    W = C["FUR"]
    k.add("chest", ell((0, -0.1, BODY_Z - 0.01), (0.108, 0.112, 0.15)), W, "body")
    k.add("middle", ell((0, 0.04, BODY_Z - 0.008), (0.104, 0.155, 0.138)), W, "body")
    k.add("rump", ell((0, 0.172, BODY_Z - 0.008), (0.118, 0.112, 0.15)), W, "body")
    k.add("neck", tube([(0, -0.12, 0.47), (0, -0.18, 0.54), (0, -0.23, 0.6)], [0.08, 0.076, 0.072]), W, "head")


def add_legs():
    W = C["FUR"]
    spread = 0.075 if Z else 0.0           # Z stands with its paws set wide
    reach = 0.05 if Z else 0.0              # and its fore paws forward, its hind paws back
    for nm, bone, y0, dy in (("f", "leg{s}", FORE_Y, -reach), ("h", "hind{s}", HIND_Y, reach)):
        top = np.array([LEG_X, y0, 0.33])
        knee = np.array([LEG_X + 0.002 + spread * 0.5, y0 + dy * 0.5 + (0.03 if nm == "h" else -0.002), 0.18])
        foot = np.array([LEG_X + 0.004 + spread, y0 + dy - (0.008 if nm == "f" else -0.01), 0.06])
        if nm == "f":
            k.add("shoulder", ell((LEG_X, FORE_Y + 0.004, 0.33), (0.062, 0.076, 0.135)), W, bone, mirror=True)
        else:
            k.add("thigh", ell((LEG_X + 0.004, HIND_Y + 0.008, 0.33), (0.07, 0.102, 0.15)), W, bone, mirror=True)
        k.add(f"leg{nm}", tube([top, knee, foot], [0.062, 0.054, 0.052]), W, bone, mirror=True)
        paw = foot + (0.001, -0.026 if nm == "f" else -0.026, -0.024)
        k.add(f"paw{nm}", ell(paw, (0.064, 0.08, 0.04)), W, bone, mirror=True)
        # three dark claws, curving forward and down to the ground
        for j, dx in enumerate((-0.036, 0.0, 0.036)):
            a = paw + (dx, -0.056, 0.006)
            ln = 1.25 if Z else 1.0
            k.add(f"claw{nm}{j}", blade([a, a + np.array([dx * 0.3, -0.03, 0.0]) * ln, a + np.array([dx * 0.45, -0.055, -0.03]) * ln],
                                        [0.022, 0.016, 0.003], (1, 0, 0), 0.75), C["CLAW"], bone, fuse=False,
                  mirror=True, surface="shell")
        # the cuff of spiky fur round the lower leg (Mega) or a thin blue spur behind it (Z)
        mid = knee + (foot - knee) * 0.35
        if not Z:
            for j, (ang, up) in enumerate(((-70, 0.25), (-25, 0.35), (20, 0.3), (65, 0.15), (110, 0.05))):
                r_ = math.radians(ang)
                d = unit(math.sin(r_) * 0.9, math.cos(r_) * 0.9 + 0.35, up)        # out and back round the leg
                base = mid + np.array([math.sin(r_), math.cos(r_), 0]) * 0.045
                ln = 0.12 if j in (1, 2) else 0.1
                k.add(f"cuff{nm}{j}", blade([base, base + d * ln * 0.5, base + d * ln], [0.045, 0.03, 0.004],
                                            np.cross(d, (0, 0, 1)) + 1e-6, 0.5),
                      C["SHADE"] if j % 2 else W, bone, mirror=True)
        else:
            a = mid + (0, 0.04, 0.02)
            k.add(f"spur{nm}", blade([a, a + (0.01, 0.03, 0.04), a + (0.02, 0.035, 0.1)], [0.02, 0.015, 0.004],
                                     (1, 0, 0), 0.7), C["HORN"], bone, fuse=False, mirror=True, surface="shell")


def add_ruff():
    W = C["FUR"]
    k.add("ruff", ell((0, -0.205, 0.445), (0.118, 0.088, 0.13), deform=lumpy(6, 3, 0.05)), W, "body")
    k.add("ruffcollar", ell((0, -0.15, 0.53), (0.104, 0.085, 0.08), deform=lumpy(5, 2, 0.05)), W, "body")
    for i, a in enumerate(np.linspace(-1.1, 1.1, 5)):
        base = np.array((0.085 * math.sin(a), -0.205 - 0.07 * math.cos(a), 0.36))
        d = unit(0.4 * math.sin(a), -0.25 * math.cos(a), -1.0)
        ln = 0.1 - 0.02 * abs(a)
        k.add(f"rufftuft{i}", blade([base, base + d * ln * 0.55, base + d * ln], [0.036, 0.022, 0.003],
                                    (math.sin(a), -math.cos(a), 0.0), 0.5), W, "body")
    # Mega's collar flares wider: four points out at each side of the neck and shoulder
    for j, (z, y, d) in enumerate(((0.54, -0.19, unit(1, -0.1, 0.4)), (0.49, -0.21, unit(1, -0.2, 0.05)),
                                   (0.43, -0.2, unit(1, -0.2, -0.35)), (0.37, -0.17, unit(1, -0.15, -0.7)))):
        base = np.array((0.1, y, z))
        k.add(f"ruffside{j}", blade([base, base + d * 0.04, base + d * 0.075], [0.034, 0.02, 0.003], (0, 1, 0), 0.5),
              W, "body", mirror=True)


def add_head():
    W = C["FUR"]
    k.add("head", ell(H, HR), W, "head")
    k.add("face", ell(H + (0, -0.045, -0.035), (0.09, 0.094, 0.09)), W, "head")
    k.add("muzzle", ell(H + (0, -0.12, -0.078), (0.04, 0.038, 0.034)), W, "head")
    for j, (z, d) in enumerate(((0.63, unit(-1, -0.15, 0.2)), (0.585, unit(-1, -0.2, -0.25)), (0.54, unit(-0.8, -0.2, -0.7)))):
        base = np.array((-0.095, -0.29, z))
        k.add(f"cheektuft{j}", blade([base, base + d * 0.025, base + d * 0.048], [0.026, 0.016, 0.003], (0, 1, 0), 0.5),
              W, "head")


def add_face_paint():
    def face(P):
        Q = P - H
        r = np.hypot(Q[:, 0], Q[:, 1])
        a = np.abs(np.arctan2(Q[:, 0], -Q[:, 1]))
        ztop = 0.03 + 0.04 * np.minimum(a, 0.5)
        return (inside((a - 0.92) * r, 0.0025) * inside(Q[:, 2] - ztop, 0.0025) * inside(Q[:, 1] + 0.035, 0.0025)
                * inside(-0.13 - Q[:, 2], 0.003) * (np.linalg.norm(Q, axis=1) < 0.21))
    k.paint(face, C["FACE"])
    n = unit(0, -1, 0.3)
    k.decal(H + (0, -0.13, 0.04), n, (0.104, 0.04), C["FUR"], eps=0.004, rings=12, segs=72,
            outline=polygon([(-1, 1), (1, 1), (1, -0.25), (0.55, -0.45), (0, -0.7), (-0.55, -0.45), (-1, -0.25)]))


# --- the lock: the hood's fur falling broad over the face's +X side to the chest, a loose piece ----------------------
def add_lock():
    pts = [(0.035, -0.3, 0.77), (0.062, -0.365, 0.72), (0.072, -0.393, 0.63), (0.076, -0.385, 0.53),
           (0.07, -0.36, 0.43), (0.056, -0.32, 0.35)]
    radii = [0.05, 0.066, 0.07, 0.064, 0.05, 0.012]
    k.add("lock", blade(pts, radii, unit(0.35, -1, 0), 0.38), C["FUR"], "head", fuse=False)
    # a second, narrower strand behind it at the face's outer edge, so the side reads as fur
    pts2 = [(0.08, -0.3, 0.75), (0.13, -0.33, 0.66), (0.14, -0.33, 0.55), (0.125, -0.31, 0.46)]
    k.add("lock2", blade(pts2, [0.04, 0.05, 0.042, 0.006], unit(1, -0.4, 0), 0.5), C["FUR"], "head", fuse=False)
    if Z:
        # the lock's red tip
        k.add("locktip", blade([(0.064, -0.345, 0.385), (0.06, -0.33, 0.35), (0.052, -0.31, 0.32)], [0.04, 0.03, 0.006],
                               unit(0.35, -1, 0), 0.5), C["PLUME"], "head", fuse=False)


# --- the horns ---------------------------------------------------------------------------------------------------
def add_horn():
    if not Z:
        # a short thick crescent on -X, rising up and curving back over the hood
        pts = [(-0.075, -0.27, 0.72), (-0.11, -0.27, 0.8), (-0.125, -0.23, 0.875), (-0.105, -0.16, 0.92),
               (-0.07, -0.09, 0.915)]
        k.add("horn", blade(pts, [0.036, 0.044, 0.04, 0.028, 0.006], unit(1, 0.25, 0), 0.5), C["HORN"], "head",
              fuse=False, surface="shell")
    else:
        # angular and bent: up and out, a corner, then up and forward to a point; flat-faced hulls
        R, K1, T = np.array([-0.075, -0.25, 0.72]), np.array([-0.17, -0.2, 0.85]), np.array([-0.12, -0.28, 1.0])
        for nm, a, b, ra, rb in (("horn", R, K1, 0.05, 0.046), ("horntip", K1, T, 0.048, 0.0)):
            d = b - a; d /= np.linalg.norm(d)
            s1 = np.cross(d, (0, 1.0, 0)); s1 /= np.linalg.norm(s1)
            s2 = np.cross(d, s1)
            pts = []
            for p, r in ((a, ra), (b, rb)):
                for t in range(4):
                    ang = math.pi / 2 * t
                    pts.append(p + r * (math.cos(ang) * s1 + 0.45 * math.sin(ang) * s2) if r > 0 else p)
            k.add(nm, hull(np.array(pts)), C["HORN"], "head", fuse=False, surface="shell")
    # the small spike on the hood's +X side, pointing up and back
    k.add("spike", blade([(0.075, -0.22, 0.75), (0.105, -0.2, 0.8), (0.125, -0.17, 0.85)], [0.026, 0.02, 0.004],
                         unit(1, 0.3, 0), 0.6), C["HORN"], "head", fuse=False, surface="shell")


# --- the wings (Mega) or the plumes (Z) ----------------------------------------------------------------------------
def add_wings():
    if not Z:
        # one broad fused fan a side: six wide feathers overlapping from a root on the back, their tips apart,
        # the fan's face turned out and back so both cameras see it; lavender across its middle
        root = np.array([0.065, 0.03, 0.53])
        tips = [(0.17, -0.06, 0.93), (0.25, 0.02, 0.9), (0.31, 0.1, 0.83), (0.35, 0.18, 0.73), (0.36, 0.25, 0.62),
                (0.33, 0.3, 0.52)]
        for i, t in enumerate(tips):
            t = np.array(t)
            a = root + (0.005, 0.02 * i, 0.02 - 0.008 * i)
            mid = a + (t - a) * 0.5 + np.array([0.035, -0.025, 0.03])
            n = np.cross(t - a, unit(-0.3, 1, 0.15))
            k.add(f"feather{i}", blade([a, mid, a + (t - a) * 0.82 + np.array([0.01, -0.01, 0.01]), t],
                                       [0.05, 0.068, 0.05, 0.006], n, 0.4), C["FUR"], "wing{s}", mirror=True)

        def shade(P):
            Q = P.copy(); Q[:, 0] = np.abs(Q[:, 0])
            d = np.linalg.norm(Q - root, axis=1)
            return inside(d - 0.27, 0.012) * inside(0.13 - d, 0.012) * (Q[:, 0] > 0.11)
        k.paint(shade, C["SHADE"], on=["feather"])
    else:
        # four crimson plumes rising like flame from navy roots, fused so the root is one mass; the red painted
        root = np.array([0.055, 0.05, 0.5])
        tips = [(0.2, 0.0, 0.98), (0.31, 0.12, 0.92), (0.38, 0.25, 0.8), (0.4, 0.36, 0.64)]
        for i, t in enumerate(tips):
            t = np.array(t)
            a = root + (0.0, 0.035 * i, 0.02 - 0.01 * i)
            mid = a + (t - a) * 0.55 + np.array([0.035, -0.03, 0.04])
            tip2 = t + np.array([-0.02, -0.04, -0.02])           # the tip curls forward and down
            n = np.cross(t - a, unit(-0.25, 1, 0.1))
            k.add(f"plume{i}", blade([a, mid, t, tip2], [0.05, 0.072, 0.045, 0.006], n, 0.4), C["FUR"], "wing{s}",
                  mirror=True)
        def reach(P):
            Q = P.copy(); Q[:, 0] = np.abs(Q[:, 0])
            return Q, np.linalg.norm(Q - root, axis=1)

        def red(P):
            Q, d = reach(P)
            ang = np.arctan2(Q[:, 2] - root[2], Q[:, 1] - root[1])
            return inside(0.2 + 0.03 * tri(ang * 3) - d, 0.004) * (Q[:, 0] > 0.11)

        def tip(P):
            Q, d = reach(P)
            return inside(0.36 - d, 0.01) * (Q[:, 0] > 0.18)
        k.paint(red, C["PLUME"], on=["plume"])
        k.paint(tip, C["PLUMETIP"], on=["plume"])


# --- the tail ------------------------------------------------------------------------------------------------------
def plate(origin, e1, e2, curve, wf, wb, thick, nu=40, nv=6):
    e1, e2 = np.array(e1, float), np.array(e2, float)
    n = np.cross(e1, e2); n /= np.linalg.norm(n)
    origin = np.array(origin, float)

    def at(u, v):
        u = 0.015 + 0.97 * u
        ab = curve(u)
        c = origin + ab[0][..., None] * e1 + ab[1][..., None] * e2
        eps = 1e-3
        ab2 = curve(u + eps)
        t = (ab2[0] - ab[0])[..., None] * e1 + (ab2[1] - ab[1])[..., None] * e2
        t /= np.linalg.norm(t, axis=-1, keepdims=True)
        p = np.cross(n, t)
        return c + p * (-wb(u) + v * (wf(u) + wb(u)))[..., None]
    return sheet(at, nu, nv, thick)


TAIL_ROOT = np.array([0.0, 0.245, 0.45])


def add_tail():
    if not Z:
        # a bat's wing held back nearly level, turned 35 degrees out to +X and tilted so both cameras see its face:
        # its upper edge smooth, its lower edge notched into three points
        e1 = unit(0.5, 0.86, 0.05)
        e2 = unit(-0.3, 0.2, 1.0)
        curve = lambda u: np.array([0.33 * u, 0.06 * u + 0.05 * u ** 2])
        wf = lambda u: np.interp(u, [0, 0.4, 0.8, 1], [0.014, 0.024, 0.03, 0.002])
        notch = lambda u: 0.035 + 0.03 * np.maximum(0, 1 - np.abs((u * 3.2) % 1 - 0.85) * 4)
        wb = lambda u: np.interp(u, [0, 0.1, 1], [0.014, 1, 1]) * notch(u) * np.interp(u, [0, 0.9, 1], [1, 1, 0.1])
        from sculptkit import outward
        k.add("tail", outward(plate(TAIL_ROOT, e1, e2, curve, wf, wb, 0.024)), C["TAIL"], "tail", fuse=False,
              surface="shell")
    else:
        # a spiny branch, back, up and out to +X, its spines forward and down
        pts = np.array([(0.0, 0.25, 0.45), (0.07, 0.36, 0.5), (0.16, 0.45, 0.55), (0.26, 0.5, 0.6)])
        k.add("tail", tube(pts, [0.03, 0.025, 0.02, 0.008]), C["TAIL"], "tail", fuse=False, surface="shell")
        for i, t in enumerate((0.25, 0.45, 0.65, 0.82)):
            j = int(t * 3); f = t * 3 - j
            p = pts[j] + (pts[j + 1] - pts[j]) * f
            for sgn in (1, -1):
                d = unit(0.25 * sgn, -0.3, -0.9 if sgn > 0 else 0.6)
                ln = 0.06 * (1.1 - t * 0.5)
                k.add(f"tailspine{i}{'a' if sgn > 0 else 'b'}", blade([p, p + d * ln * 0.5, p + d * ln],
                      [0.017, 0.012, 0.003], (1, 0, 0.2), 0.7), C["TAIL"], "tail", fuse=False, surface="shell")
    k.add("tailroot", ell(TAIL_ROOT + (0, -0.01, 0.0), (0.03, 0.03, 0.03)), C["FUR"], "tail")


# --- the forehead's mark: a broad dark oval with a lighter lens -----------------------------------------------------
def add_mark():
    n = unit(-0.25, -0.72, 0.65)          # off to the -X side, clear of the lock
    CM, CL = srgb(C["MARK"]), srgb(C["LENS"])
    def mark(u, v):
        c = np.tile(CM, (len(u), 1))
        if not Z:
            c[((u - 0.1) / 0.45) ** 2 + ((v + 0.05) / 0.45) ** 2 < 1] = CL
        return c
    k.decal(H + n * 0.13, n, (0.036, 0.026), mark, eps=0.0065, rings=14, segs=56)


def add_nose():
    k.decal(H + (0, -0.15, -0.062), unit(0, -1, 0.15), (0.012, 0.008), C["INK"], eps=0.003,
            outline=polygon([(-1, 0.7), (1, 0.7), (0, -1)]), rings=6, segs=24)


def add_mouth():
    CS, CI = srgb(C["FACE"]), srgb(C["INK"])
    if not Z:
        def mouth(u, v):
            c = np.tile(CS, (len(u), 1))
            c[(np.abs(v - (-0.1 + 0.9 * (np.abs(u) - 0.4) ** 2)) < 0.3) & (np.abs(u) < 0.85)] = CI
            return c
        k.decal(H + (0, -0.145, -0.095), unit(0, -1, -0.3), (0.02, 0.011), mouth, eps=0.003, rings=12, segs=60)
        return
    # Z: open, a dark red mouth with a white fang hanging at its -X corner (the visible side)
    CM, CF = srgb(C["MOUTH"]), srgb(C["FANG"])
    def mouth(u, v):
        c = np.tile(CM, (len(u), 1))
        c[(u < -0.15) & (u > -0.55) & (v > 0.2 - (u + 0.35) ** 2 * 6)] = CF
        return c
    k.decal(H + (0, -0.142, -0.1), unit(0, -1, -0.35), (0.03, 0.02), mouth, eps=0.003, rings=14, segs=60,
            outline=polygon([(-1, 0.6), (1, 0.6), (0.6, -0.5), (0, -1), (-0.6, -0.5)]))


# --- the eye: only the -X one shows, the lock hiding the other -------------------------------------------------------
HE = 0.037
EYE_DIR = unit(-0.45, -0.88, 0.08)
E = np.array([-0.053, -0.37, 0.631])


def eye_top(u):
    return 0.22 + 0.42 * u


def clipped(n=96):
    pts = []
    for th in np.linspace(0, 2 * math.pi, n, endpoint=False):
        du, dv = math.cos(th), math.sin(th)
        t = 1.0
        if dv > eye_top(du):
            lo, hi = 0.0, 1.0
            for _ in range(40):
                t = (lo + hi) / 2
                if t * dv > eye_top(t * du):
                    hi = t
                else:
                    lo = t
            t = lo
        pts.append((t * math.cos(th), t * math.sin(th)))
    return pts


OUT = clipped()


def eyes(state):
    CR, CP, CW, CI = srgb(C["RED"]), srgb(C["PUPIL"]), srgb(C["SCLERA"]), srgb(C["INK"])

    def eye(u, v):
        c = np.tile(CW, (len(u), 1))
        r = np.sqrt(((u + 0.14) / 0.64) ** 2 + ((v + 0.06) / 0.76) ** 2)
        c[r < 1] = CR
        c[r < 0.42] = CP
        c[((u - 0.08) / 0.16) ** 2 + ((v - 0.12) / 0.14) ** 2 < 1] = (1, 1, 1)
        c[v > eye_top(u) - 0.18] = CI
        return c
    look = eye_look(state, eye, srgb(C["FACE"]), srgb(C["LINE"]), line=0.13)
    # the -X eye: u runs outward, toward -X, so the picture is flipped as absol.py flips its left eye
    outline = polygon([(-a, b) for a, b in OUT]) if state in ("Open", "Fierce") else None
    k.decal(E, EYE_DIR, (HE, HE * 0.85), lambda a, b, f=look: f(-a, b), eps=0.005, outline=outline, rings=20, segs=56)


def absol_mega(src, dst, z=False, voxel=0.008):
    global k, Z, C
    Z = z
    C = palette(z)
    k = Kit(src, coat="fur")
    add_body()
    add_legs()
    add_ruff()
    add_head()
    add_face_paint()
    add_lock()
    add_horn()
    add_wings()
    add_tail()
    add_mark()
    add_nose()
    add_mouth()
    k.build(voxel=voxel, smooth=3)
    k.eye_states(eyes)
    k.export(dst)


if __name__ == "__main__":
    s, d = args()
    absol_mega(s, d)
