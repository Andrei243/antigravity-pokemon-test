# Aurorus sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/aurorus.py -- <the export's Aurorus.glb> <out.glb>
#
# Grown from amaura.py's frame (sibling("amaura")): its crystals, nails, eyes and the frill's paint. From the
# references: a tall blue sauropod. A deep long body on four thick legs with two white nails on each foot; a long neck
# rising almost upright from the chest to a small head; the front of the neck, the chest and the belly pale, their
# edge with the blue jagged; a long tail rising behind and coiling at its end into a spiral. A white mask over the
# upper face with the stern almond eyes in it (a blue iris under a heavy dark lid), a diamond of ice on the forehead.
# The design's largest mass is the sail: a great fan from the back of the head down the back of the neck to the
# shoulders, yellow at its root, lilac toward its wavy edge, white at the very edge, with a point rising over the
# head; here a mirror pair, each fanned back and some 35 degrees out so its face shows to both of the game's cameras.
# Diamonds of ice down the neck's sides, along the flanks and on the tail.
# Reference ratios (3D picture): head 0.12 of the height (built a third larger, 0.15), the body 0.28 deep and 0.54
# long, the legs 0.17 showing and 0.1 across, the sail some 0.55 high and 0.5 long, the tail's coil 0.17 across.
# The export's joints: body (0, 0.04, 0.32), tail (0, 0.21, 0.34), head (0, -0.2, 0.88), sail (0, -0.23, 0.92),
# legs (+-0.08, -0.1, 0.28), hinds (+-0.08, 0.16, 0.28).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, sheet, srgb, inside, smoothstep, tri, unit, eye_look, polygon, args, sibling

a = sibling("amaura")

BLUE, PALE = "#6F98DC", "#C6E2F0"
SAIL_Y, SAIL_L, SAIL_W = "#F6EC9C", "#CFCBEE", "#F4F2FC"
MASK, GEM = "#F6F8FC", "#58D2F0"
ICE, ICE_TOP = "#5CCCEE", "#E4F8FF"
INK = "#141C34"

H = np.array([0.0, -0.25, 0.885])
HR = np.array([0.074, 0.098, 0.074])
BC, BR = np.array([0.0, 0.04, 0.31]), np.array([0.142, 0.27, 0.142])
NECK = np.array([(0, -0.13, 0.36), (0, -0.17, 0.5), (0, -0.19, 0.62), (0, -0.2, 0.74), (0, -0.215, 0.84)])
NRAD = [0.125, 0.088, 0.066, 0.058, 0.056]


def on_body(y, z, s, lift=0.0):
    """The body's surface on the side s at (y, z)."""
    q = 1 - ((y - BC[1]) / BR[1]) ** 2 - ((z - BC[2]) / BR[2]) ** 2
    x = BR[0] * math.sqrt(max(q, 0.02))
    p = np.array([x * s, y, z])
    n = (p - BC) / BR ** 2
    return p + unit(*n) * lift, unit(*n)


def sail(k, s):
    """One half of the sail: a thick fan from the back of the neck, fanned up, back and out on the side s."""
    back = unit(0.6 * s, 0.8, 0)
    up = np.array([0, 0, 1.0])
    A, B = np.array([0, -0.205, 0.93]), np.array([0, -0.08, 0.46])
    US = np.array([0.0, 0.2, 0.45, 0.7, 0.88, 1.0])
    PHI = np.radians([-12, 18, 52, 86, 112, 135])
    RS = np.array([0.11, 0.155, 0.27, 0.33, 0.25, 0.07])

    def at(u, v):
        uu = 0.012 + 0.976 * u
        phi = np.interp(uu, US, PHI)
        R = np.interp(uu, US, RS) * (1 + 0.075 * np.sin(2 * math.pi * 5.5 * uu))     # a wavy, lobed edge
        root = A[None, None] + (B - A)[None, None] * uu[..., None]
        d = np.cos(phi)[..., None] * up + np.sin(phi)[..., None] * back
        vv = 0.04 + 0.96 * v
        return root + d * (R * vv)[..., None]
    k.add("sail" + ("R" if s > 0 else "L"), sheet(at, 70, 18, 0.032), SAIL_Y, "sail", surface="cloth")

    def share(P):
        """How far out a point is, from the root (0) to the edge (1)."""
        t = np.clip(((P - A) @ (B - A)) / ((B - A) @ (B - A)), 0, 1)
        root = A[None] + (B - A)[None] * t[:, None]
        R = np.interp(t, US, RS) * (1 + 0.075 * np.sin(2 * math.pi * 5.5 * t))
        return np.linalg.norm(P - root, axis=1) / R
    side = (lambda P: P[:, 0] > 0) if s > 0 else (lambda P: P[:, 0] < 0)
    k.paint(lambda P: smoothstep(0.5, 0.78, share(P)) * side(P), SAIL_L, on=["sail"])
    k.paint(lambda P: smoothstep(0.86, 0.95, share(P)) * side(P), SAIL_W, on=["sail"])


def aurorus(src, out):
    k = Kit(src, coat="scales")

    # --- the body: deep and long ----------------------------------------------------------------------------------------
    k.add("body", ell(BC, BR), BLUE, "body")
    k.add("bodychest", ell((0, -0.13, 0.33), (0.13, 0.13, 0.13)), BLUE, "body")
    k.add("bodyrump", ell((0, 0.2, 0.32), (0.13, 0.13, 0.13)), BLUE, "body")

    # --- the legs: thick, the shoulders and thighs fused into the flanks, two white nails a foot --------------------------
    for name, y, bone, top in (("fore", -0.11, "leg{s}", (0.08, 0.1, 0.12)),
                               ("hind", 0.17, "hind{s}", (0.088, 0.11, 0.13))):
        x = 0.088
        k.add(name + "top", ell((x - 0.008, y, 0.3), top), BLUE, bone, mirror=True)
        k.add(name, tube([(x, y, 0.28), (x + 0.003, y - 0.004, 0.14), (x + 0.004, y - 0.008, 0.06)],
                         [0.066, 0.056, 0.055]), BLUE, bone, mirror=True)
        k.add(name + "foot", ell((x + 0.004, y - 0.014, 0.03), (0.06, 0.07, 0.03)), BLUE, bone, mirror=True)
        for j, dx in enumerate((-0.025, 0.025)):
            a.nail(k, f"{name}nail{j}", (x + 0.004 + dx, y - 0.075, 0.016), (0, -1, -0.35), 0.024, bone)

    # --- the tail: rising behind and coiling at its end; the coil's plane turned 30 degrees to +X -------------------------
    yv = unit(0.5, 0.866, 0)
    C = np.array([0.0, 0.5, 0.53]) + np.array([0.5, 0.0, 0]) * 0.0
    pts, rad = [(0, 0.27, 0.36), (0, 0.38, 0.39), (0.012, 0.47, 0.42)], [0.09, 0.065, 0.05]
    cy = np.array([0.0, 0.0, 0.0])
    for i, al in enumerate(np.radians(np.linspace(-55, 470, 16))):
        r = 0.088 * (1 - 0.68 * i / 15)
        p = np.array([0, C[1], C[2]]) + r * (math.cos(al) * yv + math.sin(al) * np.array([0, 0, 1.0]))
        p[0] += 0.0
        pts.append(tuple(p)); rad.append(0.043 - 0.025 * i / 15)
    k.add("tail", tube(pts, rad, seg=16, per=6), BLUE, "tail")

    # --- the neck: rising nearly upright from the chest -------------------------------------------------------------------
    k.add("necklow", tube(NECK[:3], NRAD[:3]), BLUE, "body")
    k.add("neck", tube(NECK[1:], NRAD[1:]), BLUE, "head")

    # --- the head: small, a tapering snout, a white mask over the upper face -----------------------------------------------
    k.add("head", ell(H, HR), BLUE, "head")
    k.add("snout", ell(H + (0, -0.08, -0.024), (0.05, 0.068, 0.042), rot=(-6, 0, 0)), BLUE, "head")
    k.add("brow", ell(H + (0, -0.05, 0.022), (0.066, 0.05, 0.045)), BLUE, "head")

    # The mask is patches (paint over the brow's crease hatched): one round each eye, one over the forehead joining them
    dm = unit(0.64, -0.72, 0.16)
    k.decal(H + (0, -0.05, 0.016) + dm * 0.06, dm, (0.05, 0.04), MASK, eps=0.003, rings=16, segs=56, mirror=True)
    k.decal(H + (0, -0.07, 0.058), unit(0, -0.55, 0.84), (0.048, 0.042), MASK, eps=0.0032, rings=16, segs=56)

    # --- the sail: the largest mass ---------------------------------------------------------------------------------------
    sail(k, 1)
    sail(k, -1)

    # --- the pale front: the neck's front, the chest, the belly; a jagged edge --------------------------------------------
    NC = []
    for i in range(len(NECK) - 1):
        for t in np.linspace(0, 1, 20, endpoint=False):
            NC.append(NECK[i] * (1 - t) + NECK[i + 1] * t)
    NC.append(NECK[-1]); NC = np.array(NC)
    NT = np.gradient(NC, axis=0); NT /= np.linalg.norm(NT, axis=1, keepdims=True)
    SL = np.concatenate([[0], np.cumsum(np.linalg.norm(np.diff(NC, axis=0), axis=1))])

    def throat(P):
        j = ((P[:, None, :] - NC[None]) ** 2).sum(2).argmin(1)
        rad_ = P - NC[j]
        rad_ -= NT[j] * (rad_ * NT[j]).sum(1, keepdims=True)
        rad_ /= np.linalg.norm(rad_, axis=1, keepdims=True) + 1e-9
        front = np.array([0, -1.0, 0])[None] - NT[j] * (NT[j] @ np.array([0, -1.0, 0]))[:, None]
        front /= np.linalg.norm(front, axis=1, keepdims=True)
        ang = np.arccos(np.clip((rad_ * front).sum(1), -1, 1))
        edge = 1.25 + 0.28 * tri(SL[j] / 0.13)
        return inside(ang - edge, 0.03) * (P[:, 2] > 0.3) * (P[:, 2] < 0.81) * (P[:, 1] < -0.06)
    k.paint(throat, PALE)

    def belly(P):
        edge = 0.27 + 0.025 * tri(P[:, 1] / 0.12) - 0.08 * smoothstep(0.1, 0.3, P[:, 1])
        return inside(P[:, 2] - edge, 0.004) * (P[:, 1] < 0.3)
    k.paint(belly, PALE, on=["body"])
    k.paint(lambda P: inside(P[:, 2] - (0.36 - 0.25 * (P[:, 1] + 0.12)), 0.004) * (P[:, 1] < -0.04)
            * (P[:, 2] > 0.2), PALE, on=["body"])

    # --- diamonds of ice: the forehead, down the neck's sides, along the flanks and on the tail ----------------------------
    a.crystal(k, "gem", H + (0, -0.055, 0.067), (0, -0.55, 0.83), 0.026, "head", low=GEM, top=ICE_TOP, sink=0.3, rise=0.3)
    for s, side in ((1, "R"), (-1, "L")):
        for i, (pi, sz) in enumerate(((32, 0.034), (58, 0.03))):
            p, t = NC[pi], NT[pi]
            sideways = np.array([s, 0, 0.0]) - t * t[0] * s
            r = np.interp(pi, [0, 20, 40, 60, 80], NRAD)
            d = unit(s, -0.55, 0.1)
            a.crystal(k, f"neckice{i}{side}", p + d * r * 0.97, d, sz, "body" if pi < 40 else "head",
                      low=ICE, top=ICE_TOP, sink=0.3, rise=0.35)
        for i, (y, z, sz) in enumerate(((-0.06, 0.39, 0.036), (0.06, 0.41, 0.038), (0.18, 0.4, 0.034))):
            p, n = on_body(y, z, s)
            a.crystal(k, f"flankice{i}{side}", p, n + np.array([0, 0, 0.3]), sz, "body", low=ICE, top=ICE_TOP,
                      sink=0.3, rise=0.35)
        for i, (j, sz) in enumerate(((1, 0.03), (2, 0.026))):
            p = np.array(pts[j]); r = rad[j]
            d = unit(s, 0.15, 0.35)
            a.crystal(k, f"tailice{i}{side}", p + d * r * 0.95, d, sz, "tail", low=ICE, top=ICE_TOP, sink=0.3,
                      rise=0.35)

    # --- the mouth: a short stern line on the snout ------------------------------------------------------------------------
    cb, ck = srgb(BLUE), srgb(INK)

    def mouth(u, v):
        c = np.tile(cb, (len(u), 1))
        c[(np.abs(v - (-0.35 * u * u + 0.05)) < 0.22) & (np.abs(u) < 0.8)] = ck
        return c
    k.decal(H + (0, -0.135, -0.045), unit(0, -0.75, -0.65), (0.018, 0.01), mouth, eps=0.003, rings=12, segs=48)

    k.build(voxel=0.0075, smooth=5, band=0.008)

    # --- the eyes: stern almonds in the white mask, a blue iris under a heavy dark lid ------------------------------------
    de = unit(0.64, -0.72, 0.16)
    E = H + (0, -0.05, 0.012) + de * 0.062
    base = a.eye_open(MASK, ink=INK, iris_top="#1A58B4", iris_low="#3CAAF0", pupil="#0A1430", iris_w=0.5,
                      iris_h=0.9, iris_u=-0.18, rim=0.86)
    cm, ck = srgb(MASK), srgb(INK)

    def open_eye(u, v):
        c = base(u, v)
        lid = 0.52 - 0.32 * u                       # the heavy lid slants down toward the nose: stern
        c[v > lid] = cm
        c[np.abs(v - lid) < 0.16] = ck
        return c
    ALMOND = polygon([(-1, 0.05), (-0.8, -0.55), (-0.3, -0.85), (0.35, -0.82), (0.85, -0.45), (1, 0.15),
                      (0.7, 0.7), (0.1, 0.95), (-0.6, 0.75)])

    def eyes(state):
        k.decal(E, de, (0.03, 0.026), eye_look(state, open_eye, cm, ck, line=0.16), eps=0.0055, rings=20, segs=56,
                outline=ALMOND, mirror=True)

    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    aurorus(*args())
