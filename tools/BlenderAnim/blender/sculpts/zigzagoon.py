# Zigzagoon sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/zigzagoon.py -- <the export's Zigzagoon.glb> <out.glb>
# From the references: a low, long, bristly raccoon close to the ground on short legs; a round brown head with a
# black mask across the eyes, round brown eyes with a black pupil and a white glint in a white rim, a short muzzle with
# a black nose and a zigzag mouth, small spiky ears; a spiky coat in zigzag bands of brown and cream round the body,
# its spikes laid back; cream feet with small white claws; a big bushy banded tail held up behind.
# Written to be grown from (linoone.py keeps this frame: the eye, the nose, the spikes, the feet and claws).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, inside, tri, unit, eye_look, args

BROWN, CREAM, DARKB, MASK = "#8E735E", "#EEE8DC", "#6A5446", "#26201E"
FOOT, CLAW, NOSE = "#E8E2D6", "#F6F4F0", "#1E1A1A"
IRIS, PUPIL, WHITE, INK = "#8A5A32", "#160E0A", "#F6F2EA", "#1E1612"

# The frame (joints: body (0, 0, 0.16), head (0, -0.13, 0.18), ears (+-0.04, -0.15, 0.24), legs (+-0.06, -0.09, 0.09),
# hinds (+-0.06, 0.09, 0.09), tail (0, 0.17, 0.17))
HC = np.array([0.0, -0.16, 0.175])
HR = np.array([0.094, 0.088, 0.088])
BC = np.array([0.0, 0.03, 0.14])
BR = np.array([0.105, 0.17, 0.095])
LX, FY, HY = 0.066, -0.085, 0.1


# --- shared by the line ----------------------------------------------------------------------------------------------
def spike(base, d, length, w, t, side=None):
    """A solid tapering tuft of fur (or a claw): a flat-faced hull, w wide and t thick at its root."""
    base = np.array(base, float)
    d = unit(*d)
    if side is None:
        side = np.cross(d, (0, 0, 1.0))
        if np.linalg.norm(side) < 1e-3:
            side = np.array([1.0, 0, 0])
    side = np.array(side, float); side = side - d * (side @ d); side /= np.linalg.norm(side)
    face = np.cross(d, side)
    pts = [base + side * w, base - side * w, base + face * t, base - face * t,
           base + d * length * 0.45 + side * w * 0.5, base + d * length * 0.45 - side * w * 0.5, base + d * length]
    return hull(pts)


def feet(k, legs, r, col, claw, bone_of):
    """Short legs ending in wide cream feet, three small white claws on each."""
    for name, (x, y, z), bone in legs:
        k.add(name + "foot", ell((x, y - 0.01, 0.02), r), col, bone, mirror=True)
        for i, dx in enumerate((-0.016, 0.0, 0.016)):
            a = np.array([x + dx, y - 0.01 - r[1] + 0.006, 0.012])
            k.add(f"claw{name}{i}", spike(a, (dx * 2, -1, -0.4), 0.02, 0.008, 0.006), claw, bone, fuse=False,
                  mirror=True, surface="shell")


def round_eye(iris, pupil, white, ink, rim=0.8, pupil_r=0.5):
    """A round eye: a white rim, a coloured iris, a black pupil, a white glint up and out."""
    W, I, P = srgb(white), srgb(iris), srgb(pupil)
    def look(u, v):
        c = np.tile(W, (len(u), 1))
        r = np.hypot(u, v)
        c[r < rim] = I
        c[np.hypot(u + 0.05, v + 0.05) < pupil_r * rim] = P
        c[np.hypot(u - 0.22, v - 0.25) < 0.2] = (1, 1, 1)
        return c
    return look


def lay_eyes(k, state, E, n, half, look, lid, ink):
    k.decal(E, n, half, eye_look(state, look, srgb(lid), srgb(ink), line=0.16), eps=0.005, rings=20, segs=56,
            mirror=True)


def zigzag(axis, origin, period, amp, wave, share=0.42, edge=0.004):
    """Crisp zigzag bands across a part: along `axis` from `origin`, each `period` long, the edges zigzagging by amp
    with the angle round the axis (wave teeth a turn). 1 on the cream bands."""
    axis = unit(*axis)
    a, b = np.cross(axis, (0, 0, 1.0)), None
    if np.linalg.norm(a) < 1e-3:
        a = np.array([1.0, 0, 0])
    a /= np.linalg.norm(a); b = np.cross(axis, a)
    origin = np.array(origin, float)
    def mask(P):
        q = P - origin
        s = q @ axis
        ang = np.arctan2(q @ a, q @ b) / (2 * math.pi)
        f = (s + amp * tri(ang * wave)) / period
        fr = f - np.floor(f)
        return inside((np.abs(fr - 0.5) - share / 2) * period, edge)
    return mask


# --- Zigzagoon -------------------------------------------------------------------------------------------------------
def zigzagoon(src, out):
    k = Kit(src, coat="fur")

    # The long low body, its belly near the ground, the haunches fused in
    k.add("body", ell(BC, BR), BROWN, "body")
    k.add("chest", ell((0, -0.08, 0.14), (0.1, 0.08, 0.09)), BROWN, "body")
    k.add("rump", ell((0, 0.13, 0.145), (0.1, 0.08, 0.09)), BROWN, "body")
    k.add("belly", ell((0, 0.02, 0.09), (0.085, 0.15, 0.05)), FOOT, "body")
    for name, (x, y), bone in (("leg", (LX, FY), "leg{s}"), ("hind", (LX, HY), "hind{s}")):
        k.add(name, tube([(x - 0.008, y, 0.1), (x, y - 0.004, 0.035)], [0.04, 0.034]), BROWN, bone, mirror=True)
    feet(k, [("leg", (LX, FY, 0), "leg{s}"), ("hind", (LX, HY, 0), "hind{s}")], (0.034, 0.042, 0.022), FOOT, CLAW,
         None)

    # The round head and its short muzzle
    k.add("head", ell(HC, HR), BROWN, "head")
    k.add("snout", ell(HC + (0, -0.085, -0.03), (0.046, 0.04, 0.036)), BROWN, "head")

    # The coat's zigzag bands round the body, cream and brown
    bands = zigzag((0, 1, 0.25), (0, -0.07, 0.14), 0.085, 0.02, 7)
    k.paint(lambda P: bands(P) * (P[:, 1] > -0.09) * (P[:, 2] > 0.075), CREAM, on=["body", "chest", "rump"])

    # Spikes of fur laid back over the back and down the sides, each the colour of the band at its root
    def col_at(p):
        return CREAM if bands(np.array([p]))[0] > 0.5 else BROWN
    for yy in np.linspace(-0.06, 0.17, 6):
        for az in np.linspace(-1.3, 1.3, 7):
            x, z = BR[0] * 0.97 * math.sin(az), 0.14 + BR[2] * 0.97 * math.cos(az)
            yl = yy + 0.012 * (abs(az) > 0.6)
            base = np.array([x, yl, z])
            d = unit(0.55 * math.sin(az), 1.0, 0.35 * math.cos(az) - 0.1)
            k.add(f"fur{len(k.parts)}", spike(base - d * 0.01, d, 0.055, 0.022, 0.015), col_at(base), "body",
                  fuse=False, surface="fur")

    # Spiky tufts round the back of the head and small spiky ears
    for i, az in enumerate((-1.2, -0.6, 0.0, 0.6, 1.2)):
        base = HC + np.array([HR[0] * 0.9 * math.sin(az), 0.03, HR[2] * 0.9 * math.cos(az)])
        d = unit(0.6 * math.sin(az), 1.0, 0.6 * math.cos(az))
        k.add(f"nape{i}", spike(base - d * 0.01, d, 0.05, 0.022, 0.015), BROWN, "head", fuse=False, surface="fur")
    for s in (1, -1):
        for j, (dx, ln) in enumerate(((0.0, 0.05), (0.022, 0.04), (-0.018, 0.035))):
            base = np.array([(0.05 + dx) * s, -0.15, 0.235])
            k.add(f"ear{'R' if s > 0 else 'L'}{j}", spike(base, (s * (0.35 + dx * 6), 0.15, 1), ln, 0.02, 0.014),
                  BROWN, "ear" + ("R" if s > 0 else "L"), fuse=False, surface="fur")
    for s in (1, -1):
        base = HC + np.array([0.082 * s, -0.025, -0.03])
        k.add(f"cheek{'R' if s > 0 else 'L'}", spike(base, (s, 0.4, -0.25), 0.035, 0.02, 0.014), BROWN, "head",
              fuse=False, surface="fur")

    # The bushy banded tail, up and back, swung a little out to the model's right
    TB, TD = np.array([0.0, 0.17, 0.18]), unit(0.35, 0.75, 0.6)
    k.add("tail", ell(TB + TD * 0.1, (0.07, 0.065, 0.11), rot=(-40, 0, -25)), BROWN, "tail")
    tailbands = zigzag(TD, TB, 0.07, 0.015, 6)
    k.paint(lambda P: tailbands(P), CREAM, on=["tail"])
    a, b = np.cross(TD, (0, 0, 1.0)), None
    a /= np.linalg.norm(a); b = np.cross(TD, a)
    n = 0
    for t in (0.05, 0.1, 0.15):
        for th in np.linspace(0, 2 * math.pi, 7, endpoint=False):
            base = TB + TD * t + 0.058 * (math.cos(th) * a + math.sin(th) * b) * (1 - (t - 0.1) ** 2 * 30)
            d = unit(*(TD * 0.9 + 0.6 * (math.cos(th) * a + math.sin(th) * b)))
            col = CREAM if tailbands(np.array([base]))[0] > 0.5 else BROWN
            k.add(f"tailfur{n}", spike(base - d * 0.01, d, 0.05, 0.022, 0.015), col, "tail", fuse=False,
                  surface="fur"); n += 1
    k.add("tailtip", spike(TB + TD * 0.2, TD, 0.05, 0.03, 0.02), CREAM, "tail", fuse=False, surface="fur")

    # The black mask across the eyes, round the head
    def mask(P):
        d = (P - HC) / HR
        return inside(np.abs(d[:, 2] - 0.18 - 0.15 * np.abs(d[:, 0])) - 0.27, 0.04) * (d[:, 1] < 0.1)
    k.paint(mask, MASK, on=["head"])

    # The black nose; a zigzag mouth under it
    k.add("nose", ell(HC + (0, -0.124, -0.02), (0.02, 0.014, 0.015)), NOSE, "head", fuse=False, surface="skin")
    CB, CK = srgb(BROWN), srgb(INK)
    def mouth(u, v):
        c = np.tile(CB, (len(u), 1))
        c[(np.abs(v - 0.55 * tri(u * 1.0 + 0.5)) < 0.22) & (np.abs(u) < 0.95)] = CK
        return c
    k.decal(HC + (0, -0.115, -0.058), unit(0, -1, -0.3), (0.03, 0.013), mouth, eps=0.003, rings=12, segs=48)

    k.build(voxel=0.006, smooth=3, band=0.006)

    de = unit(0.55, -0.8, 0.15)
    E = HC + de / np.linalg.norm(de / HR) + (0, 0, 0.012)
    look = round_eye(IRIS, PUPIL, WHITE, INK)
    def eyes(state):
        lay_eyes(k, state, E, de, (0.026, 0.026), look, MASK, "#A89888")
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    zigzagoon(*args())
