# Growlithe sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/growlithe.py -- <the export's Growlithe.glb> <out.glb>
# An orange puppy with bold black stripes: a big round head with a broad cream muzzle and chin, a large black nose and
# a smiling mouth, dark eyes cut flat by a confident brow, small round ears with cream insides, a spiky cream tuft on
# top; a great shaggy cream ruff over the chest and round the neck; a stout trunk on thick legs, a black chevron round
# each leg and stripes down the flanks and haunches; small white claws; a big bushy cream tail.
#
# Written to be grown from (Arcanine keeps this frame: the trunk, the legs, the head, the way the eyes, nose, mouth,
# stripes, fur and claws are made). Each part is a constant or a small function below, the calls at the end.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, blade, sheet, hull, outward, srgb, inside, smoothstep, tri, unit, eye_look,
                       polygon, lumpy, args)

src, out = args()
k = Kit(src, coat="fur")

# --- colours ----------------------------------------------------------------------------------------------------------
ORANGE = "#EE8E3E"         # the coat: between the drawing's soft orange and the 3D model's bright one
CREAM = "#F2E2BC"          # the muzzle, the tuft, the ruff, the tail, inside the ears
STRIPE = "#22262C"         # the stripes: near black with a cold tinge
CLAW = "#F4F2EC"
NOSE, INK = "#18181C", "#2A1A14"
EYE, IRIS, PUPIL = "#F6F4EE", "#3A4254", "#141820"

# --- the frame (the skeleton's joints: body (0, 0.02, 0.24), head (0, -0.12, 0.32), ears (+-0.07, -0.13, 0.46),
#     forelegs (+-0.065, -0.1, 0.2), hind legs (+-0.065, 0.14, 0.2), tail (0, 0.17, 0.29)) ------------------------------
H = np.array([0, -0.17, 0.405])          # the head's centre, over and in front of its joint
HR = np.array([0.112, 0.102, 0.1])       # its radii: a big round puppy's head
LEG_X = 0.064
FORE_Y, HIND_Y = -0.1, 0.14
BODY_Z = 0.25
LEG_TOP = 0.23
LEG_R = (0.046, 0.041, 0.039)            # a leg's radius at its top, middle and foot


def head_shape(V):
    """Round, fuller at the cheeks below the middle."""
    V = V.copy()
    V[:, 0] *= 1 + 0.1 * np.clip(-V[:, 2], 0, 1) * (1 - np.clip(-V[:, 2], 0, 1))
    return V


def add_head(color=ORANGE, muzzle=CREAM):
    k.add("head", ell(H, HR, deform=head_shape), color, "head")
    # A broad short muzzle standing forward low on the face, and a chin under it
    k.add("muzzle", ell(H + (0, -0.086, -0.042), (0.066, 0.05, 0.046)), muzzle, "head")
    k.add("chin", ell(H + (0, -0.065, -0.078), (0.05, 0.04, 0.028)), muzzle, "head")
    k.add("neck", tube([(0, -0.07, 0.28), (0, -0.11, 0.33), (0, -0.14, 0.37)], [0.07, 0.068, 0.066]), color, "head")


def round_head(P):
    """The angle round the head's upright axis from straight ahead (radians) of points."""
    return np.arctan2(P[:, 0], -(P[:, 1] - H[1]))


def add_face_cream(color=CREAM, coat=ORANGE):
    """The cream of the lower face: the muzzle, the chin and the cheeks under the eyes, under a crisp line."""
    def line(P):
        return H[2] - 0.026 - 0.35 * np.clip(np.abs(P[:, 0]) - 0.045, 0, None)
    front = lambda P: (np.abs(round_head(P)) < 1.45) * (P[:, 1] < H[1] + 0.02)
    k.paint(lambda P: inside(P[:, 2] - line(P), 0.0025) * front(P) * (P[:, 2] > H[2] - 0.13), color,
            on=["head", "muzzle", "chin"])
    k.paint(lambda P: inside(line(P) - P[:, 2], 0.0025) * front(P) * (P[:, 2] < H[2] + 0.02), coat,
            on=["head", "muzzle", "chin"])


def add_body(color=ORANGE):
    # A stout trunk: chest, middle, rump, its belly over the tops of the legs
    k.add("chest", ell((0, -0.07, BODY_Z), (0.088, 0.088, 0.094)), color, "body")
    k.add("middle", ell((0, 0.03, BODY_Z - 0.004), (0.082, 0.1, 0.088)), color, "body")
    k.add("rump", ell((0, 0.13, BODY_Z), (0.088, 0.078, 0.094)), color, "body")


def leg_r(z, top=LEG_TOP):
    """A leg's radius at a height."""
    return np.interp(z, [0.03, top * 0.5, top], [LEG_R[2], LEG_R[1], LEG_R[0]])


def add_legs(color=ORANGE):
    # Thick legs, a shoulder and a thigh over the top of each, broad paws
    k.add("shoulder", ell((LEG_X - 0.004, FORE_Y + 0.004, LEG_TOP - 0.02), (0.05, 0.056, 0.07)), color, "leg{s}", mirror=True)
    k.add("leg", tube([(LEG_X, FORE_Y, LEG_TOP), (LEG_X, FORE_Y, LEG_TOP * 0.5), (LEG_X, FORE_Y, 0.04)],
                      list(LEG_R)), color, "leg{s}", mirror=True)
    k.add("paw", ell((LEG_X, FORE_Y - 0.014, 0.026), (0.042, 0.05, 0.025)), color, "leg{s}", mirror=True)
    k.add("thigh", ell((LEG_X - 0.004, HIND_Y + 0.006, LEG_TOP - 0.015), (0.056, 0.068, 0.08)), color, "hind{s}", mirror=True)
    k.add("hind", tube([(LEG_X, HIND_Y, LEG_TOP), (LEG_X, HIND_Y, LEG_TOP * 0.5), (LEG_X, HIND_Y, 0.04)],
                       list(LEG_R)), color, "hind{s}", mirror=True)
    k.add("hpaw", ell((LEG_X, HIND_Y - 0.014, 0.026), (0.042, 0.05, 0.025)), color, "hind{s}", mirror=True)


def add_claws(color=CLAW):
    """Three small white claws at the front of each paw: little flat-faced points."""
    for bone, y in (("leg{s}", FORE_Y), ("hind{s}", HIND_Y)):
        for j, dx in enumerate((-0.022, 0.0, 0.022)):
            a = np.array([LEG_X + dx, y - 0.055, 0.016])
            pts = [a + (-0.007, 0.012, -0.006), a + (0.007, 0.012, -0.006), a + (-0.006, 0.012, 0.008),
                   a + (0.006, 0.012, 0.008), a + (0, -0.006, 0.0), a + (0, 0.0, -0.012)]
            k.add(f"claw{bone[0]}{j}", hull(pts), color, bone, fuse=False, mirror=True, surface="shell")


# --- stripes: a chevron round a leg (a loose band whose front dips to a point, its seam behind), and lens-shaped
#     stripes laid on the flanks from the side ---------------------------------------------------------------------
def chevron(c, z, r, width=0.022, dip=0.04, thick=0.006):
    """A band round an upright leg at height z: its middle line dips by `dip` toward the front in a V."""
    c = np.array(c, float)

    def at(u, v):
        th = 2 * math.pi * u                     # nought straight behind, half a turn in front
        front = 1 - np.abs((th - math.pi) / math.pi)        # 1 in front, 0 behind
        zz = z + 0.4 * dip - dip * front + (v - 0.5) * width
        rr = r(zz) if callable(r) else np.full_like(zz, r)
        return np.stack([c[0] + rr * np.sin(th), c[1] + rr * np.cos(th), zz], axis=-1)
    return outward(sheet(at, 73, 3, thick))


def add_leg_stripes(zs=(0.085, 0.165), color=STRIPE):
    for bone, y, name in (("leg{s}", FORE_Y, "fore"), ("hind{s}", HIND_Y, "back")):
        for j, z in enumerate(zs):
            k.add(f"stripe{name}{j}", chevron((LEG_X, y, 0), z, lambda zz: leg_r(zz) - 0.003), color, bone,
                  fuse=False, mirror=True, surface="fur")


STRIPE_OUT = polygon([(-0.6, -1.0), (0.6, -1.0), (1.0, 0.0), (0.7, 0.85), (0.0, 1.0), (-0.7, 0.85), (-1.0, 0.0)])
# Flank stripes (centre, half width, half length, lean of the stripe's length from upright toward the back)
FLANK = [((0.0, -0.02, 0.28), 0.014, 0.05, -0.25), ((0.0, 0.045, 0.285), 0.014, 0.06, -0.3),
         ((0.0, 0.11, 0.285), 0.014, 0.06, -0.35), ((0.0, 0.165, 0.21), 0.012, 0.04, 0.6)]


def add_flank_stripes(stripes=FLANK, color=STRIPE):
    for c, w, l, lean in stripes:
        up = unit(0, -lean, 1)
        k.decal(np.array(c) + (0.08, 0, 0.015), (1, 0, 0.5), (w, l), color, up=up, eps=0.003, outline=STRIPE_OUT, rings=10,
                segs=40, mirror=True)


def along(P, pts, n=200):
    """Nearest place on a polyline (|x| taken, so a mirrored part's both sides): its share of the length and the offset."""
    pts = np.array(pts, float)
    seg = np.linalg.norm(np.diff(pts, axis=0), axis=1)
    cum = np.concatenate([[0], np.cumsum(seg)]); L = cum[-1]
    s = np.linspace(0, L, n)
    line = np.stack([np.interp(s, cum, pts[:, i]) for i in range(3)], axis=1)
    Q = P.copy(); Q[:, 0] = np.abs(Q[:, 0])
    j = np.concatenate([((Q[a:a + 4000, None, :] - line[None]) ** 2).sum(2).argmin(1) for a in range(0, len(Q), 4000)])
    return s[j] / L, Q - line[j]


# --- the ears: small and round, at the top corners of the head, tipped out a little; cream inside -------------------
EC = np.array([0.088, -0.15, 0.488])
ER = (0.042, 0.02, 0.044)
EROT = (12, 0, -30)


def add_ears(color=ORANGE, inner=CREAM):
    k.add("ear", ell(EC, ER, rot=EROT), color, "ear{s}", mirror=True)
    k.paint(lambda P: inside(np.hypot(np.abs(P[:, 0]) - EC[0] + 0.004, P[:, 2] - EC[2] + 0.004) - 0.026, 0.0025)
            * (P[:, 1] < EC[1] - 0.006) * (P[:, 2] > H[2] + 0.04), inner, on=["ear"])


# --- the tuft: spiky cream fur standing up on top of the head, leaning back --------------------------------------------
TUFT = [((0.0, -0.02), (0, 0.35, 1), 0.085, 0.046), ((0.032, 0.0), (0.4, 0.55, 0.75), 0.07, 0.04),
        ((-0.032, 0.0), (-0.4, 0.55, 0.75), 0.07, 0.04), ((0.012, 0.03), (0.12, 0.85, 0.55), 0.08, 0.042),
        ((-0.012, 0.03), (-0.12, 0.85, 0.55), 0.08, 0.042), ((0.0, -0.045), (0, 0.05, 1), 0.06, 0.04)]


def add_tuft(color=CREAM, spikes=TUFT, base_r=(0.064, 0.07, 0.042)):
    top = H + (0, 0.0, HR[2] * 0.82)
    k.add("tuftbase", ell(top, base_r, deform=lumpy(4, 2, 0.08)), color, "head")
    for i, ((x, y), d, ln, r) in enumerate(spikes):
        d = unit(*d)
        base = top + (x, y, 0)
        k.add(f"tuft{i}", blade([base, base + d * ln * 0.7, base + d * ln * 1.3], [r, r * 0.6, 0.003],
                                np.cross(d, (1.0, 0, 0)) if abs(d[0]) < 0.9 else (0, 1, 0), 0.6), color, "head")


# --- the ruff: a great shaggy cream mass over the chest and round the neck, in pointed tufts along its edge -------------
RUFF = np.array([0, -0.155, 0.28])


def add_ruff(color=CREAM, c=RUFF, size=1.0):
    k.add("ruff", ell(c, np.array((0.11, 0.075, 0.1)) * size, deform=lumpy(5, 3, 0.06)), color, "body")
    k.add("ruffcollar", ell(c + (0, 0.07, 0.07 * size), np.array((0.11, 0.07, 0.065)) * size, deform=lumpy(4, 2, 0.06)),
          color, "body")
    for i, a in enumerate(np.linspace(-1.3, 1.3, 7)):
        base = c + np.array((0.095 * math.sin(a), 0.01 - 0.055 * math.cos(a), -0.06)) * size
        d = unit(0.45 * math.sin(a), -0.4 * math.cos(a), -1.0)
        ln = (0.075 - 0.015 * abs(a)) * size
        k.add(f"rufftuft{i}", blade([base, base + d * ln * 0.55, base + d * ln], np.array([0.036, 0.021, 0.003]) * size,
                                    (math.sin(a), -math.cos(a), 0.0), 0.5), color, "body")
    for j, (z, d) in enumerate(((0.07, unit(1, -0.1, 0.3)), (0.02, unit(1, -0.15, -0.2)), (-0.03, unit(1, -0.1, -0.6)))):
        base = c + np.array((0.1, 0.02, z)) * size
        k.add(f"ruffside{j}", blade([base, base + d * 0.03 * size, base + d * 0.05 * size],
                                    np.array([0.032, 0.02, 0.003]) * size, (0, 1, 0), 0.5), color, "body", mirror=True)


# --- the tail: a big bushy cream plume back and up from the rump, swung some 40 degrees out to +X, flattened across
#     its swing, with spiky tufts along it ---------------------------------------------------------------------------
TAIL = [(0.0, 0.17, 0.29), (0.03, 0.215, 0.32), (0.075, 0.25, 0.37), (0.105, 0.26, 0.43), (0.12, 0.255, 0.48)]
TAIL_R = [0.035, 0.075, 0.088, 0.068, 0.008]


def add_tail(color=CREAM, root=ORANGE, tail=TAIL, radii=TAIL_R, tufts=1.0):
    side = unit(1, -0.6, 0)
    k.add("tail", blade(tail, radii, side, 0.72), color, "tail")
    k.add("tailroot", ell(tail[0], (0.04, 0.038, 0.038)), root, "tail")
    back = unit(0.6, 1, 0)
    for i, (t, ang, ln) in enumerate(((0.35, -0.4, 0.05), (0.5, 0.15, 0.06), (0.65, 0.65, 0.055), (0.8, 1.1, 0.05),
                                      (0.55, -1.0, 0.045))):
        n = len(tail) - 1
        f = t * n; a = int(f); s = f - a
        c = np.array(tail[a]) * (1 - s) + np.array(tail[a + 1]) * s
        r = radii[a] * (1 - s) + radii[a + 1] * s
        d = unit(*(math.cos(ang) * back + math.sin(ang) * np.array((0, 0, 1.0))))
        base = c + d * r * 0.7
        k.add(f"tailtuft{i}", blade([base, base + d * ln * tufts * 0.55, base + d * ln * tufts],
                                    np.array([0.032, 0.02, 0.003]) * tufts, side, 0.45), color, "tail")


# --- the face: a loose black nose, patches on the fused head laid before the body's build ----------------------------
def add_nose(at=H + (0, -0.135, -0.03), r=(0.026, 0.02, 0.019), color=NOSE):
    """A large round black nose at the tip of the muzzle, a loose piece so its edge stays crisp."""
    k.add("nose", ell(at, r, rot=(10, 0, 0)), color, "head", fuse=False, surface="shell")


CC, CI = srgb(CREAM), srgb(INK)


def mouth(u, v):
    """A smiling mouth: a line down from the nose, a wide curve turning up at the corners."""
    c = np.tile(CC, (len(u), 1))
    c[(np.abs(v - (-0.25 + 0.75 * u * u)) < 0.16) & (np.abs(u) < 0.92)] = CI
    c[(np.abs(u) < 0.07) & (v > -0.2)] = CI
    return c


def add_mouth(at=H + (0, -0.133, -0.068), half=(0.032, 0.018), look=mouth):
    k.decal(at, unit(0, -1, -0.1), half, look, eps=0.003, rings=16, segs=72)


# The eyes: round dark irises in a white, looking ahead, the top cut off flat by a brow sloping down toward the nose
# (a confident look); a white glint; no rim. In an eye's own units (u outward, v up). Laid by hand, a side each.
EYE_DIR = unit(0.52, -0.84, 0.15)
EYE_R = 0.026
E = H + EYE_DIR * HR[0] * 0.92 + np.array([0, 0, 0.006])
BROW = (0.28, 0.32)          # the cut: v = BROW[0] + BROW[1] * u


def eye_top(u):
    return BROW[0] + BROW[1] * u


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
CW, CIR, CP = srgb(EYE), srgb(IRIS), srgb(PUPIL)


def eye(u, v):
    c = np.tile(CW, (len(u), 1))
    r = np.sqrt(((u + 0.2) / 0.66) ** 2 + ((v + 0.05) / 0.8) ** 2)
    c[r < 1] = CIR
    c[r < 0.62] = CP
    c[((u - 0.0) / 0.22) ** 2 + ((v - 0.1) / 0.22) ** 2 < 1] = (1, 1, 1)
    return c


LID = ORANGE
SHUT_LINE = INK


def eyes(state):
    look = eye_look(state, eye, srgb(LID), srgb(SHUT_LINE), line=0.15)
    for side in (1, -1):
        n = EYE_DIR * np.array([side, 1, 1.0])
        outline = polygon([(side * a, b) for a, b in OUT]) if state in ("Open", "Fierce") else None
        col = look if side == 1 else (lambda a, b, f=look: f(-a, b))
        k.decal(E * np.array([side, 1, 1.0]), n, (EYE_R, EYE_R * 0.95), col, eps=0.004, outline=outline, rings=20,
                segs=56)


if __name__ == "__main__":
    add_body()
    add_legs()
    add_head()
    add_face_cream()
    add_ears()
    add_tuft()
    add_ruff()
    add_tail()
    add_claws()
    add_leg_stripes()
    add_flank_stripes()
    add_nose()
    add_mouth()

    k.build(voxel=0.006, band=0.004)
    k.eye_states(eyes)
    k.export(out)
