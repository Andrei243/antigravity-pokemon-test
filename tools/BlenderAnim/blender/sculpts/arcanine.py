# Arcanine sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/arcanine.py -- <the export's Arcanine.glb> <out.glb>
# Grown from growlithe.py (its frame of trunk, legs, head, its loose nose, its mouth, its brow-cut eyes, its chevrons,
# stripes, claws and fur), refitted to Arcanine's tall skeleton: a great orange dog with bold black stripes on long
# legs, broad shoulders and thighs and a deep trunk; the whole head cream under a huge shaggy mane that runs down over
# the chest, an orange band round each eye running back into the orange ears (cream inside), a black nose, a smile,
# fierce dark eyes; cream tufts behind the legs; a huge billowing cream tail rising and arching back.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, blade, sheet, hull, outward, srgb, inside, smoothstep, tri, unit, eye_look,
                       polygon, lumpy, args)

src, out = args()
k = Kit(src, coat="fur")

# --- colours (Growlithe's, the iris a dark brown) ---------------------------------------------------------------------
ORANGE = "#EE8E3E"
CREAM = "#F2E2BC"
STRIPE = "#22262C"
CLAW = "#F4F2EC"
NOSE, INK = "#18181C", "#2A1A14"
EYE, IRIS, PUPIL = "#F6F4EE", "#3A3024", "#141210"

# --- the frame (the skeleton's joints: body (0, 0.02, 0.47), head (0, -0.21, 0.64), ears (+-0.075, -0.23, 0.8),
#     forelegs (+-0.085, -0.18, 0.41), hind legs (+-0.085, 0.22, 0.41), tail (0, 0.28, 0.53)) ---------------------------
# Growlithe's frame on legs twice as long: broad shoulder and thigh masses over the top half of each leg, the trunk as
# deep as the leg showing under it, the head lifted on a short thick neck hidden in the mane.
H = np.array([0, -0.29, 0.7])
HR = np.array([0.118, 0.108, 0.108])
LEG_X = 0.082
FORE_Y, HIND_Y = -0.18, 0.21
BODY_Z = 0.445
LEG_TOP = 0.44
LEG_R = (0.064, 0.056, 0.052)


def head_shape(V):
    V = V.copy()
    V[:, 0] *= 1 + 0.1 * np.clip(-V[:, 2], 0, 1) * (1 - np.clip(-V[:, 2], 0, 1))
    return V


def add_head(color=CREAM, muzzle=CREAM, neck=ORANGE):
    k.add("head", ell(H, HR, deform=head_shape), color, "head")
    k.add("muzzle", ell(H + (0, -0.09, -0.045), (0.066, 0.06, 0.046)), muzzle, "head")
    k.add("chin", ell(H + (0, -0.07, -0.08), (0.05, 0.045, 0.03)), muzzle, "head")
    k.add("neck", tube([(0, -0.15, 0.5), (0, -0.21, 0.58), (0, -0.25, 0.65)], [0.1, 0.09, 0.085]), neck, "head")


def round_head(P):
    return np.arctan2(P[:, 0], -(P[:, 1] - H[1]))


def in_poly(x, y, pts):
    pts = np.array(pts, float)
    res = np.zeros(len(x), bool)
    for (x1, y1), (x2, y2) in zip(pts, np.roll(pts, -1, axis=0)):
        res ^= ((y1 > y) != (y2 > y)) & (x < (x2 - x1) * (y - y1) / (y2 - y1 + 1e-12) + x1)
    return res


def poly_mask(x, y, pts, edge=0.0035):
    """A polygon painted with a soft edge (luxio.py's): the signed distance to its outline, eased."""
    pts = np.array(pts, float)
    d = np.full(len(x), 9.0)
    for (x1, y1), (x2, y2) in zip(pts, np.roll(pts, -1, axis=0)):
        ex, ey = x2 - x1, y2 - y1
        t = np.clip(((x - x1) * ex + (y - y1) * ey) / (ex * ex + ey * ey + 1e-12), 0, 1)
        d = np.minimum(d, np.hypot(x - x1 - t * ex, y - y1 - t * ey))
    return inside(np.where(in_poly(x, y, pts), -d, d), edge)


# The orange band round each eye, running back and up into the ear, in (the arc round the head's upright axis in
# metres, height above the head's centre)
R = 0.12
BAND = [(0.034, 0.0), (0.04, 0.05), (0.075, 0.075), (0.12, 0.098), (0.17, 0.115), (0.205, 0.095), (0.195, 0.045),
        (0.12, 0.012), (0.085, -0.03), (0.05, -0.03)]


def add_band(color=ORANGE):
    def band(P):
        s = np.abs(round_head(P)) * R
        return poly_mask(s, P[:, 2] - H[2], BAND, edge=0.003) * (P[:, 1] < H[1] + 0.06)
    k.paint(band, color, on=["head"])


def add_body(color=ORANGE):
    # A deep trunk: chest, middle and rump
    k.add("chest", ell((0, -0.13, BODY_Z), (0.125, 0.125, 0.155)), color, "body")
    k.add("middle", ell((0, 0.02, BODY_Z - 0.01), (0.118, 0.15, 0.145)), color, "body")
    k.add("rump", ell((0, 0.19, BODY_Z), (0.122, 0.11, 0.15)), color, "body")


def leg_r(z, top=LEG_TOP):
    return np.interp(z, [0.03, top * 0.5, top], [LEG_R[2], LEG_R[1], LEG_R[0]])


def add_legs(color=ORANGE):
    # Broad shoulder and thigh masses over the upper half of each leg, the lower leg tapering, broad paws
    k.add("shoulder", ell((LEG_X - 0.004, FORE_Y + 0.006, LEG_TOP - 0.05), (0.066, 0.075, 0.12)), color, "leg{s}", mirror=True)
    k.add("leg", tube([(LEG_X, FORE_Y, LEG_TOP), (LEG_X, FORE_Y, LEG_TOP * 0.5), (LEG_X, FORE_Y, 0.05)],
                      list(LEG_R)), color, "leg{s}", mirror=True)
    k.add("paw", ell((LEG_X, FORE_Y - 0.016, 0.033), (0.064, 0.072, 0.033)), color, "leg{s}", mirror=True)
    k.add("thigh", ell((LEG_X - 0.004, HIND_Y + 0.01, LEG_TOP - 0.04), (0.074, 0.09, 0.13)), color, "hind{s}", mirror=True)
    k.add("hind", tube([(LEG_X, HIND_Y, LEG_TOP), (LEG_X, HIND_Y, LEG_TOP * 0.5), (LEG_X, HIND_Y, 0.05)],
                       list(LEG_R)), color, "hind{s}", mirror=True)
    k.add("hpaw", ell((LEG_X, HIND_Y - 0.016, 0.033), (0.064, 0.072, 0.033)), color, "hind{s}", mirror=True)


def add_claws(color=CLAW):
    for bone, y in (("leg{s}", FORE_Y), ("hind{s}", HIND_Y)):
        for j, dx in enumerate((-0.028, 0.0, 0.028)):
            a = np.array([LEG_X + dx, y - 0.084, 0.022])
            pts = [a + (-0.009, 0.015, -0.008), a + (0.009, 0.015, -0.008), a + (-0.008, 0.015, 0.01),
                   a + (0.008, 0.015, 0.01), a + (0, -0.008, 0.0), a + (0, 0.0, -0.015)]
            k.add(f"claw{bone[0]}{j}", hull(pts), color, bone, fuse=False, mirror=True, surface="shell")


def chevron(c, z, r, width=0.03, dip=0.055, thick=0.007):
    """A band round an upright leg at height z: its middle line dips by `dip` toward the front in a V."""
    c = np.array(c, float)

    def at(u, v):
        th = 2 * math.pi * u
        front = 1 - np.abs((th - math.pi) / math.pi)
        zz = z + 0.4 * dip - dip * front + (v - 0.5) * width
        rr = r(zz) if callable(r) else np.full_like(zz, r)
        return np.stack([c[0] + rr * np.sin(th), c[1] + rr * np.cos(th), zz], axis=-1)
    return outward(sheet(at, 73, 3, thick))


def add_leg_stripes(zs=(0.12, 0.22), color=STRIPE):
    for bone, y, name in (("leg{s}", FORE_Y, "fore"), ("hind{s}", HIND_Y, "back")):
        for j, z in enumerate(zs):
            k.add(f"stripe{name}{j}", chevron((LEG_X, y, 0), z, lambda zz: leg_r(zz) - 0.004), color, bone,
                  fuse=False, mirror=True, surface="fur")


STRIPE_OUT = polygon([(-0.6, -1.0), (0.6, -1.0), (1.0, 0.0), (0.7, 0.85), (0.0, 1.0), (-0.7, 0.85), (-1.0, 0.0)])
FLANK = [((0.0, -0.05, 0.52), 0.018, 0.075, -0.25), ((0.0, 0.03, 0.525), 0.018, 0.085, -0.3),
         ((0.0, 0.11, 0.52), 0.018, 0.08, -0.35), ((0.0, 0.19, 0.5), 0.017, 0.07, -0.4),
         ((0.0, 0.25, 0.4), 0.015, 0.05, 0.6)]


def add_flank_stripes(stripes=FLANK, color=STRIPE):
    for c, w, l, lean in stripes:
        up = unit(0, -lean, 1)
        k.decal(np.array(c) + (0.11, 0, 0.02), (1, 0, 0.5), (w, l), color, up=up, eps=0.003, outline=STRIPE_OUT,
                rings=10, segs=40, mirror=True)


def along(P, pts, n=200):
    pts = np.array(pts, float)
    seg = np.linalg.norm(np.diff(pts, axis=0), axis=1)
    cum = np.concatenate([[0], np.cumsum(seg)]); L = cum[-1]
    s = np.linspace(0, L, n)
    line = np.stack([np.interp(s, cum, pts[:, i]) for i in range(3)], axis=1)
    Q = P.copy(); Q[:, 0] = np.abs(Q[:, 0])
    j = np.concatenate([((Q[a:a + 4000, None, :] - line[None]) ** 2).sum(2).argmin(1) for a in range(0, len(Q), 4000)])
    return s[j] / L, Q - line[j]


# --- the ears: Growlithe's round ears grown into broad rounded points standing out at the head's upper corners beside
#     the mane, orange with cream inside ------------------------------------------------------------------------------
EAR = [(0.095, -0.285, 0.775), (0.135, -0.29, 0.805), (0.175, -0.295, 0.835), (0.205, -0.298, 0.86)]
EAR_R = [0.055, 0.05, 0.03, 0.006]


def add_ears(color=ORANGE, inner=CREAM):
    """Broad pointed ears standing out from the mane up and to the side, orange with a cream lining on their fronts."""
    k.add("ear", blade(EAR, EAR_R, (0, 1, 0), 0.34), color, "ear{s}", mirror=True)
    d = unit(*(np.array(EAR[-1]) - np.array(EAR[0])))
    across = np.cross(d, (0, 1.0, 0)); across /= np.linalg.norm(across)

    def lining(P):
        t, off = along(P, EAR)
        half = 0.025 * np.sin(np.pi * np.clip((t + 0.1) / 1.0, 0, 1)) ** 0.7
        w = off @ across
        front = -off[:, 1]
        return (inside(np.abs(w) - half, 0.0025) * inside(-front + 0.002, 0.002) * inside(t - 0.82, 0.01)
                * (np.abs(P[:, 0]) > 0.1))
    k.paint(lining, inner, on=["ear"])


# --- the mane: a huge shaggy cream mass round the head (behind it, over the crown and out at the cheeks, the face left
#     clear) and down over the chest to the forelegs, in pointed tufts all round its edge ------------------------------
def tufts(centre, n, radius, z0, z1, direction, length, r0, name, bone="body", spread=(-1.3, 1.3)):
    for i, a in enumerate(np.linspace(spread[0], spread[1], n)):
        base = np.array(centre) + np.array((radius * math.sin(a), -radius * math.cos(a) * 0.7, z0 + (z1 - z0) * abs(a) / 1.3))
        d = unit(*(np.array(direction(a))))
        ln = length * (1 - 0.15 * abs(a))
        k.add(f"{name}{i}", blade([base, base + d * ln * 0.55, base + d * ln], [r0, r0 * 0.6, 0.003],
                                  (math.sin(a), -math.cos(a), 0.0), 0.5), CREAM, bone)


def add_mane(color=CREAM):
    # Round the head: a lumpy mass behind and above, wider than the head
    k.add("manehead", ell(H + (0, 0.065, 0.03), (0.165, 0.12, 0.15), deform=lumpy(6, 3, 0.07)), color, "head")
    # Spiky locks over the crown, standing up and leaning back
    for i, (x, d, ln, r) in enumerate(((0.0, (0, 0.3, 1), 0.07, 0.05), (0.06, (0.4, 0.4, 1), 0.07, 0.045),
                                       (-0.06, (-0.4, 0.4, 1), 0.07, 0.045), (0.11, (0.8, 0.3, 0.6), 0.07, 0.042),
                                       (-0.11, (-0.8, 0.3, 0.6), 0.07, 0.042), (0.03, (0.15, -0.3, 1), 0.06, 0.042),
                                       (-0.03, (-0.15, -0.3, 1), 0.06, 0.042))):
        d = unit(*d)
        base = H + (x, 0.03, HR[2] * 0.55)
        k.add(f"manetop{i}", blade([base, base + d * ln * 0.55, base + d * ln], [r, r * 0.6, 0.003],
                                   np.cross(d, (1.0, 0, 0)) if abs(d[0]) < 0.9 else (0, 1, 0), 0.55), color, "head")
    # Out at the cheeks, below the ears, pointing out and down
    for j, (z, d) in enumerate(((0.0, unit(1, -0.1, 0.1)), (-0.05, unit(1, -0.15, -0.35)), (-0.1, unit(0.8, -0.2, -0.8)))):
        base = H + (0.15, 0.04, z)
        k.add(f"manecheek{j}", blade([base, base + d * 0.04, base + d * 0.075], [0.045, 0.028, 0.003], (0, 1, 0), 0.5),
              color, "head", mirror=True)
    # Over the chest, down to the tops of the forelegs, with pointed tufts along its lower edge
    c = np.array([0, -0.27, 0.5])
    k.add("manechest", ell(c, (0.15, 0.11, 0.17), deform=lumpy(5, 3, 0.06)), color, "body")
    k.add("manethroat", ell(c + (0, -0.01, 0.12), (0.12, 0.1, 0.1), deform=lumpy(4, 2, 0.06)), color, "body")
    tufts(c, 7, 0.12, 0.36, 0.42, lambda a: (0.45 * math.sin(a), -0.4 * math.cos(a), -1.0), 0.11, 0.045, "manetuft")
    for j, (z, d) in enumerate(((0.08, unit(1, -0.05, 0.2)), (0.0, unit(1, -0.1, -0.3)), (-0.08, unit(1, -0.1, -0.7)))):
        base = c + (0.13, 0.02, z)
        k.add(f"maneside{j}", blade([base, base + d * 0.045, base + d * 0.08], [0.045, 0.028, 0.003], (0, 1, 0), 0.5),
              color, "body", mirror=True)


def add_leg_tufts(color=CREAM):
    """Cream tufts on the backs of the forelegs at the elbow and on the hind legs behind the hock."""
    for bone, at, d in (("leg{s}", (LEG_X + 0.01, FORE_Y + 0.05, 0.3), unit(0.2, 1, -0.6)),
                        ("hind{s}", (LEG_X + 0.01, HIND_Y + 0.07, 0.3), unit(0.2, 1, -0.4))):
        at = np.array(at)
        k.add(f"legtuft{bone[0]}", blade([at, at + d * 0.04, at + d * 0.075], [0.04, 0.025, 0.003], (1, 0, 0), 0.55),
              color, bone, mirror=True)
        at2 = at + (0, -0.005, -0.045)
        k.add(f"legtuft2{bone[0]}", blade([at2, at2 + d * 0.03, at2 + d * 0.055], [0.032, 0.02, 0.003], (1, 0, 0), 0.55),
              color, bone, mirror=True)


# --- the tail: a huge billowing cream plume rising from the rump, arching back and falling to a spiky point, swung
#     some 40 degrees out to +X and flattened across its swing, with tufts along it ------------------------------------
TAIL = [(0.0, 0.29, 0.54), (0.04, 0.36, 0.61), (0.09, 0.43, 0.67), (0.14, 0.49, 0.68), (0.175, 0.54, 0.62),
        (0.19, 0.58, 0.55), (0.2, 0.6, 0.47)]
TAIL_R = [0.05, 0.1, 0.125, 0.12, 0.095, 0.06, 0.008]


def add_tail(color=CREAM, root=ORANGE, tail=TAIL, radii=TAIL_R):
    side = unit(1, -0.6, 0)
    k.add("tail", blade(tail, radii, side, 0.72), color, "tail")
    k.add("tailroot", ell(tail[0], (0.055, 0.05, 0.05)), root, "tail")
    for i, (t, ang, ln) in enumerate(((0.3, 1.5, 0.05), (0.45, 1.9, 0.06), (0.6, 2.2, 0.07), (0.75, 2.5, 0.065),
                                      (0.88, 2.9, 0.06), (0.5, -0.4, 0.05), (0.7, -0.2, 0.05))):
        n = len(tail) - 1
        f = t * n; a = int(f); s = f - a
        c = np.array(tail[a]) * (1 - s) + np.array(tail[a + 1]) * s
        r = radii[a] * (1 - s) + radii[a + 1] * s
        d = unit(*(math.cos(ang) * np.array((0, 0, 1.0)) + math.sin(ang) * unit(0.3, 1, 0)))
        base = c + d * r * 0.7
        k.add(f"tailtuft{i}", blade([base, base + d * ln * 0.55, base + d * ln], [0.04, 0.025, 0.003], side, 0.45),
              color, "tail")


# --- the face (Growlithe's) -------------------------------------------------------------------------------------------
def add_nose(at=H + (0, -0.145, -0.03), r=(0.028, 0.021, 0.02), color=NOSE):
    k.add("nose", ell(at, r, rot=(10, 0, 0)), color, "head", fuse=False, surface="shell")


CC, CI = srgb(CREAM), srgb(INK)


def mouth(u, v):
    c = np.tile(CC, (len(u), 1))
    c[(np.abs(v - (-0.25 + 0.75 * u * u)) < 0.16) & (np.abs(u) < 0.92)] = CI
    c[(np.abs(u) < 0.07) & (v > -0.2)] = CI
    return c


def add_mouth(at=H + (0, -0.145, -0.072), half=(0.036, 0.02), look=mouth):
    k.decal(at, unit(0, -1, -0.1), half, look, eps=0.003, rings=16, segs=72)


# The eyes: Growlithe's, the brow cutting them steeper toward the nose (fiercer), the iris a dark brown
EYE_DIR = unit(0.5, -0.84, 0.2)
EYE_R = 0.026
E = H + EYE_DIR * HR[0] * 0.92 + np.array([0, 0, 0.004])
BROW = (0.22, 0.5)


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
    r = np.sqrt(((u + 0.22) / 0.62) ** 2 + ((v + 0.08) / 0.78) ** 2)
    c[r < 1] = CIR
    c[r < 0.6] = CP
    c[((u + 0.02) / 0.2) ** 2 + ((v - 0.05) / 0.2) ** 2 < 1] = (1, 1, 1)
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
    add_band()
    add_ears()
    add_mane()
    add_leg_tufts()
    add_tail()
    add_claws()
    add_leg_stripes()
    add_flank_stripes()
    add_nose()
    add_mouth()

    k.build(voxel=0.008, band=0.004)
    k.eye_states(eyes)
    k.export(out)
