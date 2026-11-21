# Flutter Mane sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/fluttermane.py -- <the export's Flutter Mane.glb> <out.glb>
# A paradox of Misdreavus's line, built anew (misdreavus.py builds as it is read and can't be imported; its pearls,
# blades and half-lidded eye are the way followed here).
#
# From the references: a big dark teal head, round and a little wider than tall, the top and back of it lighter
# sea-green in scalloped puffs; three red horns rising from the crown and laid back, the middle one longest. Low on
# the face two half-lidded eyes: a red almond, a round yellow iris toward the nose with a black upright slit, a flat
# teal lid over the top sloping down to the nose. Under the chin a cluster of round red beads, under them a short
# skirt of pointed teal locks flaring down, red showing between them. From each temple a thick lock of hair sweeps up
# and back like an arm, ending in a teal puff fanned with long pink-violet feathers (the largest masses of the design,
# kept a mirror pair raised in a V and swept back so the model stays about as wide as it is tall). The art draws one
# lock up and one out: the 3D model shows they are a pair.
# Bones: body (0, 0, 0.307), wing{s} (0.09, 0.04, 0.397).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, blade, slab, lathe, outward, srgb, inside, smoothstep, unit, eye_look, args,
                       polygon)

TEAL, SEA, RED, BEAD, SHINE = "#2C5A61", "#4E9383", "#D8283C", "#D42A3A", "#FFB4B8"
FEATHER, YELLOW, INK = "#C98BD6", "#F4D23A", "#160E12"

H = np.array([0.0, 0.0, 0.36])
HR = np.array([0.152, 0.135, 0.128])
VOXEL = 0.0052


def head(k):
    k.add("head", ell(H, HR), TEAL, "body")
    # the face flattened a little where the eyes lie, a brow notch between them
    k.add("face", ell(H + (0, -0.05, -0.025), (0.125, 0.087, 0.092)), TEAL, "body")
    # the sea-green puffs over the top and back
    for i, (c, r) in enumerate([((0.0, 0.045, 0.465), (0.11, 0.092, 0.055)),
                                ((0.08, 0.06, 0.45), (0.068, 0.075, 0.052)),
                                ((-0.08, 0.06, 0.45), (0.068, 0.075, 0.052)),
                                ((0.0, 0.1, 0.4), (0.105, 0.062, 0.075))]):
        k.add(f"puff{i}", ell(c, r), SEA, "body")


def horns(k):
    for i, (x, l, lean) in enumerate(((0.0, 0.12, 0.0), (0.045, 0.085, 0.45), (-0.045, 0.085, -0.45))):
        base = np.array([x * 1.2, 0.015, 0.478])
        d = unit(lean, 0.55, 1.0)
        k.add(f"horn{i}", lathe(base, d, [(0, -0.01), (0.026, -0.01), (0.022, l * 0.4), (0.011, l * 0.8), (0, l)]),
              RED, "body", fuse=False, surface="shell")


def skirt(k):
    """Pointed teal locks flaring down from under the chin, red showing between them."""
    k.add("neck", ell((0, 0.01, 0.245), (0.09, 0.085, 0.05)), TEAL, "body")
    for i in range(11):
        a = 2 * math.pi * (i + 0.5) / 11
        o = np.array([math.sin(a), -math.cos(a), 0.0])
        L = 1.0 + 0.12 * (i % 2)
        root = np.array([0, 0.01, 0]) + o * 0.055 + (0, 0, 0.24)
        mid = np.array([0, 0.01, 0]) + o * 0.1 + (0, 0, 0.18)
        tip = np.array([0, 0.01, 0]) + o * (0.13 * L) + (0, 0, 0.235 - 0.12 * L)
        k.add(f"lock{i}", blade([root, mid, tip], [0.062, 0.046, 0.01], o, 0.45), TEAL, "body")
    k.add("lining", lathe((0, 0.01, 0), (0, 0, 1), [(0, 0.235), (0.06, 0.235), (0.1, 0.17), (0.105, 0.14),
                                                   (0, 0.145)]), RED, "body", fuse=False, surface="cloth")


def beads(k):
    for j, (a, z, r) in enumerate([(-0.75, 0.252, 0.022), (-0.38, 0.242, 0.026), (0.0, 0.238, 0.029),
                                   (0.38, 0.242, 0.026), (0.75, 0.252, 0.022), (-0.2, 0.208, 0.023),
                                   (0.2, 0.208, 0.023)]):
        rad = 0.088 + r * 0.4
        c = np.array([math.sin(a) * rad, 0.005 - math.cos(a) * rad, z])
        k.add(f"bead{j}", ell(c, (r, r, r), sub=3), BEAD, "body", fuse=False, surface="shell")
        hl = c + unit(-0.45, -0.75, 0.55) * r * 0.82
        k.add(f"shine{j}", ell(hl, (r * 0.28, r * 0.18, r * 0.22), sub=2), SHINE, "body", fuse=False, surface="shell")


# --- the locks of hair like arms, swept up and back, a feathered puff at the end
LOCK = np.array([(0.1, 0.03, 0.42), (0.16, 0.08, 0.49), (0.2, 0.15, 0.53), (0.21, 0.23, 0.54), (0.205, 0.3, 0.52)])
FAN_A, FAN_B = unit(0.1, 0.3, 0.95), unit(0.62, 0.35, 0.7)


def feather(k, name, root, d, L, W):
    n0 = np.cross(FAN_A, FAN_B); n0 /= np.linalg.norm(n0)
    n = n0 - d * (d @ n0); n /= np.linalg.norm(n)
    ez = np.cross(n, d)
    t = np.linspace(0, 1, 20)
    w = W * np.sin(np.pi * np.clip(t, 0.02, 0.98)) ** 0.8 * (1 - 0.35 * t)
    outline = np.vstack([np.stack([t * L, w], 1), np.stack([t * L, -w], 1)[::-1]])
    k.add(name, slab(outline, root, d, ez, 0.013), FEATHER, "wing{s}", fuse=False, mirror=True, surface="fur")
    t2 = np.linspace(0, 0.35, 10)
    w2 = W * np.sin(np.pi * np.clip(t2, 0.02, 0.98)) ** 0.8 * (1 - 0.35 * t2) + 0.002
    root2 = np.vstack([np.stack([t2 * L, w2], 1), np.stack([t2 * L, -w2], 1)[::-1]])
    k.add(name + "root", slab(root2, root, d, ez, 0.019), TEAL, "wing{s}", fuse=False, mirror=True, surface="fur")


def hair(k):
    k.add("lock", tube(LOCK, [0.07, 0.064, 0.057, 0.05, 0.045]), TEAL, "wing{s}", mirror=True)
    end = LOCK[-1]
    k.add("lockpuff", ell(end + (0.005, 0.015, 0.005), (0.062, 0.062, 0.055)), TEAL, "wing{s}", mirror=True)
    for i, t in enumerate(np.linspace(0, 1, 5)):
        d = FAN_A * (1 - t) + FAN_B * t; d /= np.linalg.norm(d)
        feather(k, f"feather{i}", end + d * 0.03, d, 0.17 + 0.04 * math.sin(t * math.pi), 0.038)
    # two shorter feathers along the lock's back
    for i, j in enumerate((1, 2, 3)):
        d = unit(0.3, 0.38, 0.87)
        feather(k, f"featherside{i}", LOCK[j] + (0.01, 0.03, 0.02), d, 0.13, 0.03)


# --- the eyes: a red almond, a yellow iris toward the nose with a black slit, a flat teal lid sloping to the nose
CR, CY, CK, CT = srgb(RED), srgb(YELLOW), srgb(INK), srgb(TEAL)


def lid(u):
    return 0.32 + 0.16 * u


def eye_open(u, v):
    c = np.tile(CR, (len(u), 1))
    c[((u + 0.18) / 0.52) ** 2 + ((v + 0.05) / 0.56) ** 2 < 1] = CY
    c[((u + 0.18) / 0.13) ** 2 + ((v + 0.05) / 0.48) ** 2 < 1] = CK
    c[v > lid(u) + 0.1] = CT
    c[np.abs(v - lid(u)) <= 0.1] = CK
    return c


EYE_CUT = polygon([(-0.95, 0.05), (-0.65, -0.55), (0.0, -0.78), (0.65, -0.55), (1.0, 0.25), (0.6, 0.72),
                   (-0.35, 0.68)])


def fluttermane(src, out):
    k = Kit(src, coat="fur")
    head(k)
    horns(k)
    skirt(k)
    beads(k)
    hair(k)
    k.build(voxel=VOXEL, smooth=4, band=0.004, wband=0.05)

    de = unit(0.44, -0.88, -0.12)
    ep = H + de / math.sqrt(((de / HR) ** 2).sum())
    en = (ep - H) / HR ** 2; en /= np.linalg.norm(en)

    def eyes(state):
        if state == "Fierce":
            def look(u, v):
                c = eye_open(u, v)
                e = 0.05 + 0.4 * u
                c[v > e + 0.1] = CT
                c[np.abs(v - e) <= 0.11] = CK
                return c
        else:
            look = eye_look(state, eye_open, CT, srgb("#9CC2BE") if state != "Open" else CK, line=0.16)
        for side in (1, -1):
            p = ep * np.array([side, 1, 1]); n = en * np.array([side, 1, 1])
            col = look if side == 1 else (lambda a, b, f=look: f(-a, b))
            cut = (lambda th: min(1.0, EYE_CUT(th))) if side == 1 else (lambda th: min(1.0, EYE_CUT(math.pi - th)))
            k.decal(p, n, (0.056, 0.046), col, eps=0.002, rings=20, segs=56, outline=cut)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    fluttermane(*args())
