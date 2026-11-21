# Salamence's Mega Evolution sculpted on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/salamencemega.py -- <the export's Salamence-Mega.glb> <out.glb>
# From the two pictures: Salamence streamlined for flight. Largest first: its two wings grown into ONE great red
# crescent over its back, its horns sweeping forward past the head, thick at the back (a quarter of its span), wider
# than the body is long, a darker rim along its outer edge (raised here in a V so that both of the game's cameras see
# its face); the body a blue torpedo with two grey armour bands (Salamence's chest plates grown into a shell) wrapped
# round it, the front one the larger, slanting back from the top; a long thin tail, red beneath, with a red fin at its
# end; short legs tucked under it, the forelegs forward, the hind legs back, white claws; the neck stretched forward
# to a long head held level, mouth shut, its lower half red, a tall forked crest a side swept up and back, a crest
# down off the jaw, small angry YELLOW eyes (the picture's; the base's are red).
# Grown from salamence.py (sibling): its blue, red and claws, its crests' blades, its eyes cut by a brow, its tail.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, hull, slab, sheet, outward, srgb, inside, unit, args, sibling

sal = sibling("salamence")
bg = sal.bg

BLUE, RED, DARKRED = sal.BLUE, sal.RED, sal.DARKRED
ARMOUR, ARMOUR_RIM = "#C8CCD6", "#9AA0AE"
FANG = sal.FANG
EYE_YELLOW = srgb("#F2C83A")

HC = np.array([0.0, -0.43, 0.445])          # the skull's middle, the neck stretched forward
HS = 1.18
HR = np.array([0.072, 0.09, 0.064]) * HS

# --- the crescent: one wing over the back, its horns forward, a bowl rising --------------------------------------------
CRES_Y = -0.18                              # the middle of the crescent's circle
CRES_R = (0.385, 0.40)                     # its centre line's radii (across, along)
CRES_W = 0.25                               # its width at the back
CRES_END = math.radians(152)                # how far round the horns reach
CRES_Z = 0.405                              # its height where it lies on the back
CRES_PITCH = -0.14                          # its forward half lifted


def cres_lift(X, Y):
    """How high the crescent stands over its point (X, Y): a smooth bowl rising to each side, the front lifted."""
    return CRES_Z + 0.45 * (np.sqrt(X * X + 0.012) - math.sqrt(0.012)) + 0.25 * X * X + CRES_PITCH * (Y - 0.2)


def cres_at(u, v):
    """The crescent's middle surface: u from one horn's tip round the back to the other's, v across its width."""
    f = np.abs(2 * u - 1)
    th = (2 * u - 1) * CRES_END
    w = CRES_W * (1 - f ** 1.7) ** 0.85 + 0.006
    cx, cy = CRES_R[0] * np.sin(th), CRES_Y + CRES_R[1] * np.cos(th)
    nx, ny = np.sin(th) * CRES_R[1], np.cos(th) * CRES_R[0]
    nl = np.sqrt(nx * nx + ny * ny)
    X = cx + nx / nl * w * (v - 0.5)
    Y = cy + ny / nl * w * (v - 0.5)
    return np.stack([X, Y, cres_lift(X, Y)], axis=-1)


def crescent(k):
    k.add("crescent", outward(sheet(cres_at, 141, 7, 0.028)), RED, "wingR", fuse=False)
    # the darker rounded rim along its outer edge, from horn to horn
    u = np.linspace(0.04, 0.96, 31)
    pts = cres_at(u[:, None], np.ones((len(u), 1)))[:, 0]
    rads = 0.009 + 0.012 * np.sin(np.pi * u) ** 0.6
    k.add("crescentrim", outward(tube(pts, rads)), DARKRED, "wingR", fuse=False)



def band(c_y, c_z, rx, rz, tilt, r):
    """A raised armour band round the body: a closed tube in a plane slanting back from the top."""
    pts = []
    for a in np.linspace(0, 2 * math.pi, 25):
        pts.append((rx * math.sin(a), c_y - tilt * math.cos(a), c_z + rz * math.cos(a)))
    pts.append(pts[1])
    return tube(pts, [r] * len(pts))


def claw_row(k, name, at, d, bone, spread=0.026):
    for j, dx in enumerate((-spread, 0.0, spread)):
        b = np.array(at) + np.array((dx, 0, 0))
        k.add(f"{name}{j}", bg.fang(b, (dx * 1.2 + d[0], d[1], d[2]), 0.034, 0.022, up=(0, 0, 1)), FANG, bone,
              fuse=False, mirror=True, surface="shell")


def salamence_mega(src, out):
    k = Kit(src, coat="scales")

    # The body: a torpedo, the chest deep, tapering behind into the tail
    k.add("torso", ell((0, 0.0, 0.3), (0.12, 0.24, 0.105)), BLUE, "body")
    k.add("chest", ell((0, -0.12, 0.305), (0.115, 0.12, 0.112)), BLUE, "body")
    k.add("rump", ell((0, 0.16, 0.295), (0.095, 0.13, 0.088)), BLUE, "body")

    # The armour: two grey plates wrapped round it, slanting back from the top, the front one broad; each a painted
    # plate whose rear edge is a raised rim
    ARM = ((-0.02, 0.305, 0.125, 0.115, 0.075, 0.13), (0.135, 0.297, 0.1, 0.09, 0.05, 0.07))
    for i, (cy, cz, rx, rz, tilt, wid) in enumerate(ARM):
        k.add(f"armourrim{i}", band(cy, cz, rx - 0.004, rz - 0.004, tilt, 0.022 - 0.004 * i), ARMOUR_RIM, "body")
        def plate(P, cy=cy, cz=cz, rz=rz, tilt=tilt, wid=wid):
            y0 = cy - tilt * np.clip((P[:, 2] - cz) / rz, -1, 1)
            d = y0 - P[:, 1]
            return inside(-d, 0.003) * inside(d - wid, 0.003) * inside(np.abs(P[:, 0]) - 0.16, 0.003) * \
                inside(P[:, 2] - 0.43, 0.003) * inside(0.17 - P[:, 2], 0.003)
        k.paint(plate, ARMOUR, on=["torso", "chest", "rump"])

    # The neck: stretched forward to the head, red down its throat
    k.add("neck", tube([(0, -0.15, 0.33), (0, -0.25, 0.38), (0, -0.33, 0.42), (0, -0.38, 0.44)],
                       [0.085, 0.072, 0.064, 0.058]), BLUE, "head")
    def throat(P):
        t = np.clip((P[:, 1] + 0.15) / -0.23, 0, 1)
        cz = 0.33 + 0.11 * t
        return inside(P[:, 2] - cz + 0.035, 0.003) * inside(np.abs(P[:, 0]) - 0.045, 0.003) * (P[:, 1] < -0.18)
    k.paint(throat, RED, on=["neck"])

    # The head: long and level, the mouth shut, its lower half red
    k.add("skull", ell(HC, HR), BLUE, "head")
    k.add("snout", ell(HC + HS * np.array((0, -0.1, 0.002)), HS * np.array((0.052, 0.1, 0.036)), rot=(-4, 0, 0)),
          BLUE, "head")
    k.add("jaw", ell(HC + HS * np.array((0, -0.07, -0.04)), HS * np.array((0.05, 0.09, 0.026)), rot=(6, 0, 0)),
          RED, "head")
    def lower(P):
        q = (P - HC) / HS
        return inside(q[:, 2] + 0.012 - 0.12 * (q[:, 1] + 0.05), 0.003) * inside(np.linalg.norm(q, axis=1) - 0.2, 0.003)
    k.paint(lower, RED, on=["skull", "snout"])

    # The crests: a tall forked blade a side up and back, a short one out to the side, one down off the jaw
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        base = HC + HS * np.array([0.032, 0.01, 0.04]) * m
        k.add(f"crestup{side}", sal.spike(base, unit(0.28 * s, 0.55, 0.85), 0.25, 0.08, 0.034, (0, -0.8, 0.5)), BLUE,
              "head", fuse=False)
        k.add(f"crestfork{side}", sal.spike(base + unit(0.28 * s, 0.55, 0.85) * 0.08, unit(0.3 * s, 0.05, 1.0), 0.1,
                                            0.045, 0.028, (0, -1, 0)), BLUE, "head", fuse=False)
        k.add(f"crestside{side}", sal.spike(HC + HS * np.array([0.055, 0.02, -0.005]) * m, unit(0.7 * s, 0.7, 0.1),
                                            0.11, 0.05, 0.028, (0, 0, 1)), BLUE, "head", fuse=False)
        k.add(f"crestjaw{side}", sal.spike(HC + HS * np.array([0.035, 0.01, -0.05]) * m, unit(0.25 * s, 0.55, -0.8),
                                           0.13, 0.05, 0.028, (0, -1, 0)), BLUE, "head", fuse=False)

    # The legs: short, tucked under the body, the forelegs forward and the hind legs back
    k.add("shoulder", ell((0.085, -0.11, 0.255), (0.062, 0.065, 0.07)), BLUE, "leg{s}", mirror=True)
    k.add("foreleg", tube([(0.095, -0.12, 0.23), (0.11, -0.19, 0.16), (0.105, -0.25, 0.12)], [0.045, 0.039, 0.035]),
          BLUE, "leg{s}", mirror=True)
    k.add("forefoot", ell((0.105, -0.265, 0.11), (0.04, 0.045, 0.026)), BLUE, "leg{s}", mirror=True)
    claw_row(k, "foreclaw", (0.105, -0.3, 0.105), (0, -1, -0.5), "leg{s}", 0.02)
    k.add("haunch", ell((0.085, 0.13, 0.25), (0.068, 0.085, 0.072)), BLUE, "hind{s}", mirror=True)
    k.add("hindleg", tube([(0.095, 0.15, 0.22), (0.11, 0.23, 0.17), (0.105, 0.3, 0.145)], [0.046, 0.039, 0.035]),
          BLUE, "hind{s}", mirror=True)
    k.add("hindfoot", ell((0.105, 0.315, 0.14), (0.04, 0.045, 0.026)), BLUE, "hind{s}", mirror=True)
    claw_row(k, "hindclaw", (0.105, 0.35, 0.13), (0, 1, -0.4), "hind{s}", 0.02)

    # The tail: long and thin, out behind and round to the +X side, red beneath, a red fin at its end
    TAIL = [(0, 0.24, 0.29), (0.02, 0.36, 0.25), (0.07, 0.47, 0.205), (0.14, 0.56, 0.17), (0.22, 0.62, 0.15)]
    TR = [0.075, 0.058, 0.044, 0.032, 0.018]
    k.add("tail", blade(TAIL, TR, (0, 0, 1), 0.6), BLUE, "tail")
    k.paint(sal.tail_under(TAIL, TR, 0.15), RED, on=["tail"])
    tip, d = np.array(TAIL[-1]), unit(0.7, 0.7, -0.15)
    fz = unit(0.45, -0.45, 0.77)
    fin = [(-0.03, -0.03), (0.05, -0.045), (0.13, -0.02), (0.1, 0.0), (0.13, 0.03), (0.05, 0.04), (-0.03, 0.025)]
    k.add("tailfin", slab(fin, tip, d, fz, 0.018), RED, "tail", fuse=False)

    crescent(k)

    # The eyes: small and angry, yellow, cut by the brow
    de = unit(0.68, -0.64, 0.33)
    E = bg.on_ell(HC, HR, de)
    cut = bg.brow_cut(0.25, 0.5)
    eyes = bg.eyes_under_brow(k, E, de, (0.03, 0.026), bg.angry_eye(cut, iris=EYE_YELLOW, pupil_c=(-0.2, -0.15),
                                                                      pupil_r=0.24, iris_r=0.52), cut, srgb(BLUE))

    k.build(voxel=0.0065, smooth=3, band=0.004)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    salamence_mega(*args())
