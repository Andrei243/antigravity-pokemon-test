# Ninetales sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/ninetales.py -- <the export's Ninetales.glb> <out.glb>
# Grown from vulpix.py (its frame of trunk, legs, head, eyes, nose and mouth, and its curling lobes for the tails),
# refitted to Ninetales' tall skeleton: a slender fox in a pale golden coat, standing on long legs with broad shoulders
# and thighs; a long narrow head with pointed upright ears, narrow red almond eyes slanting to the nose, a black nose;
# a long crest of pointed locks swept up and back from the head; a fluffy paler ruff on the chest; nine long tails
# rising behind in a fan and sweeping back, each tapering to an orange tip.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, tri, unit, eye_look, polygon, lumpy, args

src, out = args()
k = Kit(src, coat="fur")

# --- colours ----------------------------------------------------------------------------------------------------------
COAT = "#F0DF94"           # pale gold: between the drawing's cream-yellow and the 3D model's yellow
RUFF = "#F6EAB4"           # the chest's ruff, paler
TIP = "#EE7428"            # the tails' tips
EYE, PUPIL = "#D42638", "#3A0A10"   # red eyes with a dark middle
NOSE, INK = "#2A1A14", "#3A2A18"
LINE = "#8A7442"           # the closed eyes' line, a darker gold

# --- the frame (the skeleton's joints: body (0, 0.04, 0.34), head (0, -0.16, 0.56), ears (+-0.045, -0.14, 0.67),
#     crest (0, -0.15, 0.67), forelegs (+-0.08, -0.12, 0.28), hind legs (+-0.08, 0.2, 0.28), tail (0, 0.22, 0.4)) -------
# Vulpix's frame on legs nearly twice as long: a broad shoulder and thigh fused over the top half of each leg, the
# trunk as deep as the leg showing under it, a short thick neck, the head longer and narrower.
H = np.array([0, -0.215, 0.625])         # the head's centre, over and in front of its joint
HR = np.array([0.114, 0.119, 0.106])      # its radii: a tenth over the first round's, as the game's shot asked
LEG_X = 0.074
FORE_Y, HIND_Y = -0.12, 0.2
BODY_Z = 0.325                            # stood a little lower than the joints: the legs show shorter
LEG_TOP = 0.32


def head_shape(V):
    V = V.copy()
    V[:, 0] *= 1 + 0.08 * np.clip(-V[:, 2], 0, 1) * (1 - np.clip(-V[:, 2], 0, 1))
    return V


def add_head(color=COAT):
    k.add("head", ell(H, HR, deform=head_shape), color, "head")
    # A longer, narrower fox's muzzle than Vulpix's, standing forward and a little down
    k.add("muzzle", ell(H + (0, -0.125, -0.04), (0.054, 0.092, 0.044), rot=(-6, 0, 0)), color, "head")
    # A short thick neck from the chest up into the head
    k.add("neck", tube([(0, -0.11, 0.4), (0, -0.155, 0.49), (0, -0.19, 0.57)], [0.095, 0.085, 0.078]), color, "head")


def add_body(color=COAT):
    # A deep trunk, its belly low enough that the legs show no longer than it is deep
    k.add("chest", ell((0, -0.1, BODY_Z + 0.01), (0.112, 0.115, 0.15)), color, "body")
    k.add("middle", ell((0, 0.04, BODY_Z), (0.102, 0.135, 0.132)), color, "body")
    k.add("rump", ell((0, 0.19, BODY_Z), (0.108, 0.1, 0.14)), color, "body")


def add_legs(color=COAT):
    # Broad shoulder and thigh masses fused over the upper half of each leg, the lower leg tapering to a clear paw
    k.add("shoulder", ell((LEG_X - 0.002, FORE_Y + 0.006, 0.24), (0.07, 0.08, 0.13)), color, "leg{s}", mirror=True)
    k.add("leg", tube([(LEG_X - 0.003, FORE_Y + 0.006, LEG_TOP), (LEG_X, FORE_Y, 0.16), (LEG_X + 0.002, FORE_Y - 0.004, 0.045)],
                      [0.06, 0.052, 0.048]), color, "leg{s}", mirror=True)
    k.add("paw", ell((LEG_X + 0.002, FORE_Y - 0.02, 0.03), (0.052, 0.064, 0.03)), color, "leg{s}", mirror=True)
    k.add("thigh", ell((LEG_X - 0.002, HIND_Y + 0.006, 0.25), (0.078, 0.098, 0.14)), color, "hind{s}", mirror=True)
    k.add("hind", tube([(LEG_X, HIND_Y + 0.015, LEG_TOP - 0.02), (LEG_X + 0.002, HIND_Y + 0.035, 0.16),
                        (LEG_X + 0.002, HIND_Y + 0.012, 0.085), (LEG_X + 0.002, HIND_Y - 0.002, 0.045)],
                       [0.062, 0.053, 0.049, 0.048]), color, "hind{s}", mirror=True)
    k.add("hpaw", ell((LEG_X + 0.002, HIND_Y - 0.018, 0.03), (0.052, 0.064, 0.03)), color, "hind{s}", mirror=True)


def add_ruff(color=RUFF):
    """A full fused mane round the neck and over the chest, paler than the coat, in pointed tufts along its edge."""
    c = np.array([0, -0.2, 0.46])
    k.add("ruff", ell(c, (0.112, 0.085, 0.13), deform=lumpy(5, 3, 0.06)), color, "body")
    k.add("ruffcollar", ell((0, -0.14, 0.53), (0.105, 0.085, 0.085), deform=lumpy(4, 2, 0.06)), color, "body")
    for i, a in enumerate(np.linspace(-1.2, 1.2, 6)):
        base = np.array((0.09 * math.sin(a), c[1] + 0.01 - 0.06 * math.cos(a), 0.37))
        d = unit(0.4 * math.sin(a), -0.35 * math.cos(a), -1.0)
        ln = 0.085 - 0.015 * abs(a)
        k.add(f"rufftuft{i}", blade([base, base + d * ln * 0.55, base + d * ln], [0.04, 0.024, 0.003],
                                    (math.sin(a), -math.cos(a), 0.0), 0.5), color, "body")
    for j, (z, d) in enumerate(((0.08, unit(1, -0.05, 0.25)), (0.0, unit(1, -0.1, -0.25)), (-0.08, unit(1, -0.1, -0.7)))):
        base = c + (0.1, 0.02, z)
        k.add(f"ruffside{j}", blade([base, base + d * 0.035, base + d * 0.065], [0.04, 0.025, 0.003], (0, 1, 0), 0.5),
              color, "body", mirror=True)


def along(P, pts, n=200, mirror=True):
    """Nearest place on a polyline (|x| taken for a mirrored part's both sides): its share of the length and the offset."""
    pts = np.array(pts, float)
    seg = np.linalg.norm(np.diff(pts, axis=0), axis=1)
    cum = np.concatenate([[0], np.cumsum(seg)]); L = cum[-1]
    s = np.linspace(0, L, n)
    line = np.stack([np.interp(s, cum, pts[:, i]) for i in range(3)], axis=1)
    Q = P.copy()
    if mirror:
        Q[:, 0] = np.abs(Q[:, 0])
    j = np.concatenate([((Q[a:a + 4000, None, :] - line[None]) ** 2).sum(2).argmin(1) for a in range(0, len(Q), 4000)])
    return s[j] / L, Q - line[j]


# --- the ears: tall, pointed and upright, leaning a little out and back; no dark lining (the pictures show a faint
#     shade only) -------------------------------------------------------------------------------------------------------
EAR = [(0.055, -0.205, 0.695), (0.07, -0.197, 0.755), (0.084, -0.187, 0.81), (0.094, -0.18, 0.85)]
EAR_R = [0.05, 0.043, 0.024, 0.003]


def add_ears(color=COAT):
    k.add("ear", blade(EAR, EAR_R, (0, 1, 0), 0.34), color, "ear{s}", mirror=True)


# --- curls (Vulpix's): a lobe running out along d and turning toward e; here with a long gentle arc, tapering to a
#     point, for the tails and the crest's locks -----------------------------------------------------------------------
def curl(base, d, e, straight, R, r_root, r_mid, turns=1.15, n=26, taper=0.82, shrink=0.62, at=0.35, hold=1.1):
    """Points and radii of a curling lobe: straight along d, then a shrinking spiral (or arc) turning toward e."""
    d = unit(*d); e = np.array(e, float); e = e - d * (e @ d); e /= np.linalg.norm(e)
    pts, rad = [], []
    for i in range(n):
        t = i / (n - 1)
        if t < at:
            s = t / at
            a, b = straight * s, 0.0
            r = r_root + (r_mid - r_root) * smoothstep(0, 1, s)
        else:
            s = (t - at) / (1 - at)
            phi = s * turns * 2 * math.pi
            rr = R * (1 - shrink * s)
            a, b = straight + rr * math.sin(phi), -R + rr * math.cos(phi)
            r = r_mid * (1 - taper * s ** hold)
        pts.append(np.array(base, float) + a * d - b * e)
        rad.append(r)
    return pts, rad


# --- the tails: nine fat plumes from the rump, fanned up and back behind the hindquarters like a peacock's train,
#     standing larger than the body from the front three-quarter; each fat in its middle third and tapering to an
#     orange tip that curls up and a little forward ------------------------------------------------------------------
TAIL_ROOT = np.array([0, 0.25, 0.4])      # at the rump, just behind the tail bone's joint (0, 0.22, 0.4)
# Laid by hand: nine roots at the rump, fanned like a peacock's train behind and above the hindquarters. Seen from
# behind each leaves the rump at its angle from straight up (through 140 degrees); every one leans back by BACK, so the
# middle one rises some 58 degrees above the level and the outer ones some 18. (angle, length before the curl, the
# curl's radius, the thickness in the middle third)
TAIL_FAN = [(-70, 0.3, 0.09, 0.072), (-52, 0.33, 0.1, 0.075), (-35, 0.36, 0.1, 0.077), (-17, 0.38, 0.1, 0.078),
            (0, 0.39, 0.1, 0.08), (17, 0.38, 0.1, 0.078), (35, 0.36, 0.1, 0.077), (52, 0.33, 0.1, 0.075),
            (70, 0.3, 0.09, 0.072)]
BACK = 0.62
TAIL_LINES = []


def add_tails(color=COAT, tip=TIP):
    back, up, side = np.array([0, 1.0, 0]), np.array([0, 0, 1.0]), np.array([1.0, 0, 0])
    for i, (ang, ln, R, th) in enumerate(TAIL_FAN):
        a = math.radians(ang)
        d = unit(*(back * BACK + up * math.cos(a) + side * math.sin(a)))
        # The tip curls up and a little forward, over the tail's own length
        e = up * 1.0 - back * 0.15 + side * math.sin(a) * 0.6
        base = TAIL_ROOT + d * 0.02
        # Fat in the middle third, tapering to the tip
        pts, rad = curl(base, d, e, ln, R, 0.035, th, turns=0.26, taper=0.92, shrink=0.3, at=0.55, hold=1.6)
        k.add(f"tail{i}", tube(pts, rad), color, "tail")
        TAIL_LINES.append(pts)
    k.add("tailroot", ell(TAIL_ROOT + (0, 0.005, 0.005), (0.055, 0.05, 0.05)), color, "tail")
    for i, pts in enumerate(TAIL_LINES):
        def tipmask(P, pts=pts):
            t, off = along(P, pts, mirror=False)
            return inside(0.8 - t, 0.004) * (np.linalg.norm(off, axis=1) < 0.1)
        k.paint(tipmask, tip, on=[f"tail{i}"])


# --- the crest: long pointed locks swept up and back from the top of the head, fanning a little apart -------------------
def add_crest(color=COAT):
    top = H + (0, 0.02, HR[2] * 0.7)
    # Four thick locks rising from the crown and sweeping back, the middle ones longest and highest
    for i, (x, d, ln, r) in enumerate(((0.022, (0.12, 0.5, 0.86), 0.26, 0.062), (-0.022, (-0.12, 0.5, 0.86), 0.26, 0.062),
                                       (0.035, (0.4, 0.75, 0.52), 0.21, 0.054), (-0.035, (-0.4, 0.75, 0.52), 0.21, 0.054))):
        d = unit(*d)
        base = top + (x, 0, 0)
        mid = base + d * ln * 0.5
        end = base + d * ln + np.array([0, 0.07, -0.04])          # the end swept back and drooping
        k.add(f"crest{i}", blade([base, mid, mid + (end - mid) * 0.6, end], [r, r * 0.8, r * 0.45, 0.003],
                                 np.cross(d, (1.0, 0, 0)) if abs(d[0]) < 0.9 else (0, 0, 1), 0.68), color, "crest")


# --- the face: patches on the fused head, laid before the body's build ---------------------------------------------------
CB, CI = srgb(COAT), srgb(INK)


def add_nose(at=H + (0, -0.215, -0.028), half=(0.017, 0.012), color=NOSE):
    k.decal(at, unit(0, -1, 0.25), half, color, eps=0.003, outline=polygon([(-1, 0.6), (1, 0.6), (0.3, -0.9), (-0.3, -0.9)]),
            rings=6, segs=28)


def mouth(u, v):
    """A closed mouth: one clean line along the muzzle's side, turning up at the corners (a calm fox's smile)."""
    c = np.tile(CB, (len(u), 1))
    c[(np.abs(v - (-0.1 + 0.9 * (np.abs(u) - 0.3) ** 2)) < 0.22) & (np.abs(u) < 0.9)] = CI
    return c


def add_mouth(at=H + (0, -0.196, -0.064), half=(0.032, 0.014), look=mouth):
    k.decal(at, unit(0, -1, -0.3), half, look, eps=0.003, rings=14, segs=64)


# The eyes: Vulpix's way, narrowed into red almonds slanting down to the nose with a dark slit of a pupil and a small
# white glint; no rim. Laid by hand, a side each (u toward +X on both).
EYE_DIR = unit(0.55, -0.82, 0.12)
EYE_HALF = (0.044, 0.027)
EYE_OUTLINE = [(-1.0, -0.35), (-0.55, 0.2), (0.1, 0.62), (0.7, 0.95), (1.0, 0.75), (0.85, 0.05), (0.4, -0.55),
               (-0.2, -0.75), (-0.7, -0.62)]
CE, CP = srgb(EYE), srgb(PUPIL)


def eye(u, v):
    c = np.tile(CE, (len(u), 1))
    c[((u - 0.05) / 0.3) ** 2 + ((v + 0.0) / 0.82) ** 2 < 1] = CP
    c[((u - 0.4) / 0.2) ** 2 + ((v - 0.4) / 0.3) ** 2 < 1] = (1, 1, 1)
    return c


E = H + EYE_DIR * HR[0] * 1.0 + np.array([0, 0, 0.004])


def eyes(state):
    look = eye_look(state, eye, CB, srgb(LINE))
    k.decal(E, EYE_DIR, EYE_HALF, look, eps=0.004, rings=20, segs=56, outline=polygon(EYE_OUTLINE))
    m = np.array([-1, 1, 1.0])
    k.decal(E * m, EYE_DIR * m, EYE_HALF, lambda u, v: look(-u, v), eps=0.004, rings=20, segs=56,
            outline=polygon([(-x, y) for x, y in EYE_OUTLINE]))


if __name__ == "__main__":
    add_body()
    add_legs()
    add_head()
    add_ears()
    add_ruff()
    add_crest()
    add_tails()
    add_nose()
    add_mouth()

    k.build(voxel=0.0078, band=0.004)
    k.eye_states(eyes)
    k.export(out)
