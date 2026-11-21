# Mudbray sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/mudbray.py -- <the export's Mudbray.glb> <out.glb>
# From the references: a brown donkey foal with a big head; a big round pale pink muzzle with two black nostrils at its
# front; round white eyes with a black pupil, high on the sides of the head; a pale pink splotch on the forehead and
# one under each eye; long upright ears, brown with a pink lining; a dark grey mane of blunt locks, a fringe falling
# over the forehead between the ears and a crest of blocks down the back of the neck; pale pink splotches of mud on the
# chest and the belly; each leg brown above a lumpy pale pink sock of mud, a dark hoof peeping out under it; a dark
# grey bushy tail like a broom.
#
# Written to be grown from (mudsdale.py imports it with sibling("mudbray")): the line's brown and mane, the legs with
# their socks and hooves, the locks of the mane and tail, the painted splotches and the eye are the functions below;
# the calls that build Mudbray are under __main__.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, lathe, srgb, inside, unit, polygon, eye_look, args, sibling

bz = sibling("blitzle")               # on_ell, side_mark (a polygon painted through the body in (y, z))

# --- the line's colours -------------------------------------------------------------------------------------------
BROWN, MUD, MANE = "#9A6A52", "#EBC3B0", "#45464D"
HOOF, NOSTRIL, EAR_IN = "#2C2B30", "#1B1A1E", "#E79A8A"
WHITE, INK, GLINT = srgb("#F7F5F2"), srgb("#141317"), np.array([1.0, 1.0, 1.0])
RIM = srgb("#3A2A24")


def sock(k, name, x, y, z_top, r, bone, color=MUD, lumps=4):
    """A lumpy sock of mud round the lower leg: fused balls stacked from z_top to the ground, widest at the foot."""
    for i in range(lumps):
        t = i / (lumps - 1)
        z = z_top * (1 - t) + r * 0.65 * t
        rr = r * (0.8 + 0.25 * t)
        jx = 0.08 * r * (1 if i % 2 else -1)
        k.add(f"{name}sock{i}", ell((x + jx, y - 0.03 * r * i, z), (rr, rr * 1.05, r * 0.62)), color, bone,
              mirror=True)


def hoof(k, name, x, y, r, bone, color=HOOF):
    """A dark hoof under the sock: a low drum standing a little forward, its rim showing at the front."""
    k.add(name + "hoof", lathe((x, y - r * 0.12, 0.0), (0, 0, 1),
                               [(0, 0), (r, 0), (r * 1.02, r * 0.18), (r * 0.9, r * 0.42), (0, r * 0.42)]),
          color, bone, fuse=False, mirror=True, surface="shell")


def leg(k, name, x, y, top, radii, bone, sock_top, sock_r, color=BROWN, mud=MUD, hoof_color=HOOF, lumps=4):
    """A leg: a thick brown tube from the fused shoulder or thigh, a sock of mud over its lower part, a hoof."""
    rt, rk, rb = radii
    k.add(name, tube([(x, y, top), (x, y - 0.004, top * 0.55), (x, y - 0.004, sock_top)], [rt, rk, rb]), color, bone,
          mirror=True)
    sock(k, name, x, y - 0.004, sock_top, sock_r, bone, mud, lumps)
    hoof(k, name, x, y - 0.004, sock_r * 0.98, bone, hoof_color)


def lock(a, b, w, t, flare=1.0, up=(0, 0, 1)):
    """A blunt lock of mane: a flat-faced block from a to b, w wide and t thick at its root, flaring by flare."""
    a, b = np.array(a, float), np.array(b, float)
    d = b - a; d /= np.linalg.norm(d)
    e1 = np.cross(d, up)
    if np.linalg.norm(e1) < 1e-3:
        e1 = np.array([1.0, 0, 0])
    e1 /= np.linalg.norm(e1)
    e2 = np.cross(e1, d)
    P = []
    for p, s in ((a, 1.0), (b - d * w * 0.25, flare)):
        for sx in (-1, 1):
            for sy in (-1, 1):
                P.append(p + e1 * sx * w * s + e2 * sy * t * (0.8 if s != 1.0 else 1.0))
    P += [b + e1 * w * flare * 0.7, b - e1 * w * flare * 0.7]
    return hull(P)


def splotch(c, r, wob=0.22, n=5, seed=0.0):
    """A splotch of mud painted on the body: a ball whose edge wobbles (n lobes), at centre c with radii r."""
    c, r = np.array(c, float), np.array(r, float)
    def mask(P):
        Q = (P - c) / r
        th = np.arctan2(Q[:, 2], Q[:, 1])
        d = np.linalg.norm(Q, axis=1) - (1 + wob * np.sin(n * th + seed))
        return inside(d * min(r), 0.002)
    return mask


def round_eye(u, v):
    """The open eye: a dark rim, a white, a black pupil toward the front and a glint (u outward, v up)."""
    r = np.sqrt(u * u + v * v)
    c = np.tile(RIM, (len(u), 1))
    c[r < 0.8] = WHITE
    c[((u + 0.18) / 0.44) ** 2 + ((v + 0.04) / 0.56) ** 2 < 1] = INK
    c[np.hypot(u + 0.06, v - 0.22) < 0.15] = GLINT
    return c


def mudbray(src, out):
    k = Kit(src, coat="fur")

    # --- the trunk: a barrel, a round chest and rump, the belly at a quarter of the height -------------------------
    k.add("barrel", ell((0, 0.01, 0.198), (0.064, 0.12, 0.066)), BROWN, "body")
    k.add("chest", ell((0, -0.078, 0.205), (0.062, 0.064, 0.07)), BROWN, "body")
    k.add("rump", ell((0, 0.098, 0.208), (0.064, 0.068, 0.066)), BROWN, "body")

    # --- legs: shoulders and thighs fused over the upper half, thick shins, socks of mud, dark hooves ------------
    k.add("shoulder", ell((0.042, -0.08, 0.165), (0.036, 0.046, 0.066)), BROWN, "leg{s}", mirror=True)
    k.add("thigh", ell((0.044, 0.1, 0.17), (0.04, 0.058, 0.072)), BROWN, "hind{s}", mirror=True)
    leg(k, "fore", 0.045, -0.082, 0.15, (0.028, 0.022, 0.021), "leg{s}", 0.078, 0.031)
    leg(k, "hind", 0.046, 0.1, 0.15, (0.03, 0.022, 0.021), "hind{s}", 0.078, 0.031)

    # --- a short thick neck up to a big head; a big round pale muzzle --------------------------------------------
    k.add("neck", tube([(0, -0.08, 0.215), (0, -0.112, 0.27), (0, -0.14, 0.32)], [0.054, 0.048, 0.046]), BROWN, "head")
    H, HR = np.array([0, -0.154, 0.352]), np.array([0.068, 0.07, 0.064])
    k.add("head", ell(H, HR), BROWN, "head")
    k.add("cheek", ell((0, -0.196, 0.318), (0.058, 0.054, 0.052)), BROWN, "head")
    MZ, MR = np.array([0, -0.242, 0.302]), np.array([0.055, 0.052, 0.048])
    k.add("muzzle", ell(MZ, MR), MUD, "head", surface="skin")
    # two black nostrils at the muzzle's front, turned a little out
    for s in (1, -1):
        dn = unit(0.32 * s, -1, 0.12)
        k.decal(bz.on_ell(MZ, MR, unit(0.36 * s, -1, 0.1)), dn, (0.011, 0.014), NOSTRIL, up=unit(-0.3 * s, 0, 1),
                eps=0.003, rings=8, segs=40)

    # --- long ears standing up and a little out, brown with a pink lining ----------------------------------------
    EB, ET = np.array([0.026, -0.15, 0.392]), np.array([0.062, -0.14, 0.488])
    k.add("ear", tube([EB, (EB + ET) / 2 + (0.004, -0.004, 0), ET], [0.02, 0.025, 0.007]), BROWN, "ear{s}",
          mirror=True)
    en = unit(0.2, -1, 0.05)
    lining = polygon([(0, 1), (0.6, 0.3), (0.55, -0.6), (0, -1), (-0.55, -0.6), (-0.6, 0.3)])
    k.decal((EB + ET) / 2 + (0.002, -0.02, 0.004), en, (0.012, 0.034), EAR_IN, up=unit(0.36, 0, 1), eps=0.003,
            outline=lining, rings=10, segs=44, mirror=True)

    # --- the mane: a fringe of blunt locks over the forehead, a crest of blocks down the back of the neck --------
    # the fringe's roots on the crown between the ears, each lock lying down over the forehead's top
    for i, (x, L) in enumerate([(0.0, 0.056), (0.022, 0.052), (-0.022, 0.052), (0.042, 0.04), (-0.042, 0.04)]):
        n = unit(x / HR[0] * 0.9, -0.5, 1)
        a = bz.on_ell(H, HR, n) + n * 0.004
        d = unit(x * 5, -0.55, -0.83)
        k.add(f"fringe{i}", lock(a + n * 0.004, a + d * L + n * 0.008, 0.014, 0.011, 1.12, up=n), MANE, "head",
              fuse=False, surface="hair")
    # the crest: blunt blocks down the back of the neck, broad, leaning a little to each side in turn
    for i, t in enumerate(np.linspace(0, 1, 6)):
        p = np.array((0, -0.14, 0.41)) * (1 - t) + np.array((0, -0.07, 0.268)) * t
        L = 0.042 - 0.012 * t
        d = unit(0.18 * (1 if i % 2 else -1), 0.85, 0.6 - 0.3 * t)
        k.add(f"crest{i}", lock(p - d * 0.01, p + d * L, 0.015, 0.012, 1.15, up=(1, 0, 0)), MANE,
              "head" if t < 0.6 else "body", fuse=False, surface="hair")

    # --- the tail: a dark broom of a few thick locks hanging back and down ---------------------------------------
    TB = np.array((0, 0.158, 0.232))
    k.add("tailroot", ell(TB, (0.02, 0.02, 0.02)), BROWN, "tail")
    for i, (dx, dz, L) in enumerate([(0.0, -0.55, 0.11), (0.45, -0.5, 0.1), (-0.45, -0.5, 0.1), (0.2, -0.2, 0.09),
                                     (-0.2, -0.2, 0.09)]):
        d = unit(dx, 0.75, dz)
        k.add(f"tail{i}", lock(TB, TB + d * L, 0.014, 0.012, 1.5, up=(0, 0, 1)), MANE, "tail", fuse=False,
              surface="hair")

    # --- splotches of mud: the chest, the belly's sides, the throat, the forehead and under each eye ------------
    k.paint(splotch((0, -0.13, 0.165), (0.045, 0.03, 0.03), 0.25, 4), MUD)
    for s in (1, -1):
        k.paint(splotch((0.05 * s, 0.03, 0.15), (0.04, 0.045, 0.024), 0.3, 5, 1.0), MUD)
        k.paint(splotch((0.052 * s, -0.04, 0.24), (0.03, 0.028, 0.02), 0.28, 4, 2.0), MUD)
    k.paint(lambda P: splotch((0, -0.2, 0.39), (0.018, 0.03, 0.022), 0.2, 4)(P) * (P[:, 1] < -0.17), MUD)

    # --- the eyes: round and white, high on the sides of the head, a pink splotch under each ---------------------
    de = unit(0.72, -0.62, 0.22)
    E = bz.on_ell(H, HR, de)
    dc = unit(0.75, -0.6, -0.25)
    k.decal(bz.on_ell(H + (0, -0.01, -0.012), HR, dc), dc, (0.016, 0.012), MUD, up=unit(0, 0.3, 1), eps=0.003,
            rings=8, segs=40, mirror=True)
    B = srgb(BROWN)
    def eyes(state):
        k.decal(E, de, (0.021, 0.024), eye_look(state, round_eye, B, INK, line=0.16), up=unit(0, 0.15, 1),
                eps=0.005, rings=20, segs=56, mirror=True)

    k.build(voxel=0.0045, smooth=4, band=0.006)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    mudbray(*args())
