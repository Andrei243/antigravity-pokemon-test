# Maschiff sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/maschiff.py -- <the export's Maschiff.glb> <out.glb>
#
# From the references: a low, front-heavy mastiff puppy, salmon red all over (the brief's "orange-brown": the pictures
# show a salmon red coat). A great head, as wide as the body and two thirds of the height, carried low and forward;
# a broad dark plum muzzle that is the lower half of the face, bulging into jowls at the sides, an orange nose at its
# front, a clenched grin of white teeth on each side of it; small fierce eyes under a heavy brow sloping to the nose;
# floppy ears folded down at the top corners of the head. A barrel body on short thick legs with round toed paws, a
# dark plum underside between the legs. Yellow hair: a swept lock on the back of the head, a streak along the back,
# a plume of a tail up and back.
#
# Written to be grown from (mabosstiff.py keeps this frame: the legs and paws, the floppy ear, the scowling eye, the
# locks of hair, the nose).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, hull, srgb, inside, tri, unit, eye_look, polygon, boxy, args

SALMON, PLUM, YELLOW = "#D4675F", "#4C3B4C", "#F2C232"
NOSE, WHITE, INK, PUPIL = "#EC6A2A", "#FAF8F2", "#241A22", "#1A1418"

# --- the frame (joints: body (0, 0.04, 0.15), head (0, -0.07, 0.2), ears (+-0.07, -0.1, 0.26),
#     legs (+-0.07, -0.06, 0.1), hinds (+-0.07, 0.13, 0.1), tail (0, 0.17, 0.19)) -----------------------------------------
HC = np.array([0.0, -0.15, 0.218])        # the skull
HR = np.array([0.104, 0.088, 0.086])
MC = np.array([0.0, -0.21, 0.142])       # the dark muzzle, the lower half of the face
MR = np.array([0.088, 0.066, 0.062])
LX, FY, HY = 0.074, -0.072, 0.132
BROW = (0.22, 0.55)                       # the eye's top under the brow: v = BROW[0] + BROW[1] u (u outward)


def on_ell(c, r, x, z, front=True):
    """The point of an ellipsoid's front at a given x and z."""
    c, r = np.array(c, float), np.array(r, float)
    s = 1 - ((x - c[0]) / r[0]) ** 2 - ((z - c[2]) / r[2]) ** 2
    return np.array((x, c[1] + (-1 if front else 1) * r[1] * math.sqrt(max(s, 0.0)), z))


def normal_at(c, r, p):
    n = (np.array(p) - c) / np.array(r) ** 2
    return n / np.linalg.norm(n)


def tuft(k, name, base, d, length, r, col, bone, flat=(0, 0, 1), f=0.45, fuse=True):
    """A pointed lock of hair: a blade from base along d, r thick at its root, flattened across `flat`."""
    base = np.array(base, float)
    d = unit(*d)
    fl = np.array(flat, float); fl = fl - d * (fl @ d)
    if np.linalg.norm(fl) < 1e-3:
        fl = np.cross(d, (1.0, 0, 0))
    k.add(name, blade([base, base + d * length * 0.5, base + d * length], [r, r * 0.66, r * 0.16], fl, f), col, bone,
          fuse=fuse)


def leg(k, x, y, top, r, paw, col, bone, name, haunch, toes=True):
    """A short thick leg: a haunch fused into the flank over its top, a stub of shin, a round paw with three toes."""
    k.add(name + "top", ell((x - 0.004, y, top + haunch[2] * 0.25), haunch), col, bone, mirror=True)
    pr = np.array(paw, float)
    k.add(name, tube([(x, y, top), (x + 0.002, y - 0.006, (top + pr[2]) / 2), (x + 0.004, y - 0.012, max(pr[2] + 0.008, r * 0.96 + 0.001))],
                     [r * 1.12, r * 1.02, r * 0.96]), col, bone, mirror=True)
    pc = np.array((x + 0.004, y - 0.022, pr[2]))
    k.add(name + "paw", ell(pc, pr), col, bone, mirror=True)
    if toes:
        for j, dx in enumerate((-0.6, 0.0, 0.6)):
            k.add(f"{name}toe{j}", ell(pc + (dx * pr[0] * 0.78, -pr[1] * 0.7, -pr[2] * 0.12),
                                       (pr[0] * 0.4, pr[1] * 0.4, pr[2] * 0.85)), col, bone, mirror=True)
    return pc


def toe_lines(k, paws, pr, col):
    """Short dark grooves between the toes on the front of each paw (paws: centres on +X)."""
    def mask(P):
        m = np.zeros(len(P))
        q = P.copy(); q[:, 0] = np.abs(q[:, 0])
        for pc in paws:
            for dx in (-0.3, 0.3):
                gx = pc[0] + dx * pr[0] * 0.78
                m = np.maximum(m, inside(np.abs(q[:, 0] - gx) - 0.0024, 0.0015) * (q[:, 1] < pc[1] - pr[1] * 0.5)
                               * (q[:, 2] > 0.004) * (q[:, 2] < pc[2] + pr[2] * 0.55)
                               * (np.abs(q[:, 1] - pc[1]) < pr[1] * 1.3))
        return m
    k.paint(mask, col)


def floppy_ear(k, root, drop, size, col, bone="ear{s}", out=0.35):
    """A floppy ear folded down from the top corner of the head (mirrored): a thick rounded flap hanging out and down,
    its fold a roll along the top."""
    root = np.array(root, float)
    d = unit(out, -0.2, -1)
    c = root + d * drop * 0.55
    def pear(V):
        V = V.copy()
        V[:, :2] *= (1 + 0.25 * np.clip(-V[:, 2], 0, 1) - 0.25 * np.clip(V[:, 2], 0, 1))[:, None]
        return V
    k.add("ear", ell(c, (size[0], size[1], drop * 0.55), rot=(-10, -math.degrees(math.atan2(out, 1)), 0),
                     deform=pear), col, bone, mirror=True, fuse=False)
    rf = size[0] * 1.1
    k.add("earfold", tube([root + (-0.01, 0.022, -0.006), root + (0.0, 0.0, 0.0), root + (0.006, -0.026, -0.008)],
                          [rf * 0.9, rf, rf * 0.9]), col, bone, mirror=True, fuse=False)


def clipped(brow=BROW, n=96):
    """The unit circle cut off above v = brow[0] + brow[1] u: an eye's outline under a brow."""
    pts = []
    for th in np.linspace(0, 2 * math.pi, n, endpoint=False):
        du, dv = math.cos(th), math.sin(th)
        t = 1.0
        if dv > brow[0] + brow[1] * du:
            lo, hi = 0.0, 1.0
            for _ in range(40):
                t = (lo + hi) / 2
                if t * dv > brow[0] + brow[1] * t * du:
                    hi = t
                else:
                    lo = t
            t = lo
        pts.append((t * math.cos(th), t * math.sin(th)))
    return pts


def scowl(white=WHITE, pupil=PUPIL, ink=INK, brow=BROW, band=0.3, p=(-0.12, -0.1, 0.5), iris=None):
    """The open eye: a white, a big dark pupil toward the nose (with an iris round it if given), a dark band along
    the cut top."""
    CW, CP, CI = srgb(white), srgb(pupil), srgb(ink)
    CR = srgb(iris) if iris else None
    def eye(u, v):
        c = np.tile(CW, (len(u), 1))
        r = np.hypot(u - p[0], (v - p[1]) / 1.1)
        if CR is not None:
            c[r < p[2]] = CR
            c[(np.abs(u - p[0]) < p[2] * 0.22) & (r < p[2] * 0.92)] = CP      # a slit pupil
        else:
            c[r < p[2]] = CP
        c[v > brow[0] + brow[1] * u - band] = CI
        return c
    return eye


def lay_eyes(k, state, E, n, half, look, lid, ink=INK, brow=BROW, line=0.16):
    """Both eyes laid by hand, a side each, u outward on both, cut by the brow in the open states."""
    f = eye_look(state, look, srgb(lid), srgb(ink), line=line)
    out_ = clipped(brow)
    for s in (1, -1):
        nn = np.array(n) * np.array([s, 1, 1.0])
        outline = polygon([(s * a, b) for a, b in out_]) if state in ("Open", "Fierce") else None
        col = f if s == 1 else (lambda a, b, g=f: g(-a, b))
        k.decal(np.array(E) * np.array([s, 1, 1.0]), nn, half, col, eps=0.0025, outline=outline, rings=20, segs=56)


def lens(n=64, top=0.5, bottom=0.9):
    """A lens's outline, its top line flatter than its bottom: a grin's opening."""
    pts = []
    for th in np.linspace(0, 2 * math.pi, n, endpoint=False):
        u = math.cos(th)
        v = math.sin(th) * (top if math.sin(th) > 0 else bottom)
        pts.append((u, v * (1 - 0.35 * u * u)))
    return pts


def grin(k, at, n, half, teeth=WHITE, ink=INK, zig=3.5, up=(0, 0, 1)):
    """A clenched grin on each side of the muzzle: a lens of white teeth, a bold zigzag where they meet."""
    CW, CI = srgb(teeth), srgb(ink)
    def look(u, v):
        c = np.tile(CW, (len(u), 1))
        line = 0.1 * tri(u * zig)
        c[np.abs(v - line) < 0.2] = CI
        return c
    k.decal(at, n, half, look, eps=0.0025, outline=polygon(lens()), rings=16, segs=64, mirror=True, up=up)


def maschiff(src, out):
    k = Kit(src, coat="fur")

    # --- the trunk: a barrel, low, the haunches fused into it ---------------------------------------------------------
    k.add("chest", ell((0, -0.045, 0.152), (0.094, 0.086, 0.088)), SALMON, "body")
    k.add("middle", ell((0, 0.045, 0.15), (0.084, 0.1, 0.078)), SALMON, "body")
    k.add("rump", ell((0, 0.122, 0.164), (0.088, 0.072, 0.082)), SALMON, "body")

    # --- short thick legs, round toed paws ------------------------------------------------------------------------
    PR = (0.04, 0.046, 0.024)
    pf = leg(k, LX, FY, 0.11, 0.036, PR, SALMON, "leg{s}", "leg", (0.05, 0.058, 0.07))
    ph = leg(k, LX, HY, 0.11, 0.036, PR, SALMON, "hind{s}", "hind", (0.054, 0.066, 0.075))
    toe_lines(k, [pf, ph], PR, "#8A3C38")
    # the dark plum underside between the legs
    k.paint(lambda P: inside(P[:, 2] - (0.098 + 0.008 * tri(P[:, 1] * 18)), 0.003) * (P[:, 1] > -0.07)
            * (P[:, 1] < 0.12) * inside(np.abs(P[:, 0]) - 0.07, 0.004) * (P[:, 2] > 0.045), PLUM,
            on=["chest", "middle", "rump"])

    # --- the head: a great skull carried low and forward, the dark muzzle and jowls under it ----------------------
    k.add("neck", ell((0, -0.095, 0.185), (0.082, 0.07, 0.075)), SALMON, "head")
    k.add("head", ell(HC, HR, deform=boxy(2.4)), SALMON, "head")
    for s in (1, -1):        # the heavy brow over each eye, sloping down toward the nose
        k.add(f"brow{'R' if s > 0 else 'L'}", ell(HC + (s * 0.046, -0.07, 0.03), (0.042, 0.022, 0.02),
                                                  rot=(0, s * 18, 0)), SALMON, "head")
    k.add("muzzle", ell(MC, MR, deform=boxy(2.5)), PLUM, "head")
    k.add("jowl", ell(MC + (0.06, 0.02, -0.022), (0.042, 0.05, 0.048)), PLUM, "head", mirror=True)
    k.add("chin", ell(MC + (0, -0.012, -0.044), (0.06, 0.05, 0.03)), PLUM, "head")

    # floppy ears folded down at the top corners of the head
    floppy_ear(k, (0.094, -0.16, 0.272), 0.1, (0.02, 0.05), SALMON)

    # --- yellow hair: a lock swept back off the back of the head, a streak along the back, the tail's plume --------
    for i, (x, y, z, d, ln) in enumerate(((0.0, -0.16, 0.296, (0.0, 1, 0.22), 0.12), (0.03, -0.155, 0.292, (0.3, 1, 0.14), 0.1),
                                          (-0.03, -0.155, 0.292, (-0.3, 1, 0.14), 0.1),
                                          (0.052, -0.14, 0.28, (0.5, 1, 0.0), 0.08),
                                          (-0.052, -0.14, 0.28, (-0.5, 1, 0.0), 0.08))):
        tuft(k, f"lock{i}", (x, y, z), d, ln, 0.03, YELLOW, "head")
    for i, (x, y, z, ln) in enumerate(((0.012, -0.02, 0.236, 0.1), (-0.014, 0.05, 0.228, 0.1),
                                       (0.01, 0.12, 0.236, 0.08))):
        tuft(k, f"streak{i}", (x, y, z), (x * 4, 1, 0.12), ln, 0.026, YELLOW, "body")
    # the tail: a plume back, up and out to +X, a fan of thick locks whose faces the cameras see
    T0 = np.array((0.0, 0.18, 0.2))
    for i, (d, ln, r) in enumerate((((0.35, 1, 0.75), 0.16, 0.03), ((0.55, 1, 0.45), 0.13, 0.026),
                                    ((0.15, 1, 1.0), 0.12, 0.024))):
        tuft(k, f"tail{i}", T0 + (0.004 * i, 0.0, 0.004 * i), d, ln, r, YELLOW, "tail", flat=(-0.6, 0.3, 0.8), f=0.5)

    # --- the face: the orange nose on the muzzle's front, the clenched grins on its sides -------------------------
    k.add("nose", ell(MC + (0, -MR[1] - 0.004, 0.034), (0.022, 0.016, 0.017), rot=(14, 0, 0)), NOSE, "head",
          fuse=False, surface="skin")
    gp = on_ell(MC, MR, 0.052, MC[2] - 0.006)
    grin(k, gp, unit(0.62, -0.78, 0.0), (0.042, 0.026))

    k.build(voxel=0.0042, smooth=3, band=0.005, wband=0.04)

    # --- small fierce eyes under the brow ----------------------------------------------------------------------------
    E = on_ell(HC, HR, 0.044, HC[2] + 0.006)
    n = unit(0.3, -1, 0.08)
    look = scowl()
    k.eye_states(lambda s: lay_eyes(k, s, E, n, (0.024, 0.02), look, SALMON))
    k.export(out)


if __name__ == "__main__":
    maschiff(*args())
