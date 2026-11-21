# Mudsdale sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown from
# Mudbray's script (mudbray.py, imported as sibling("mudbray")): the same brown body, the same blunt locks of mane,
# the same painted splotches and the same way of making the eye, the legs grown from socks of mud into boots.
#   blender --background --factory-startup --python sculpts/mudsdale.py -- <the export's Mudsdale.glb> <out.glb>
#
# From the references: a great heavy draft horse, brown, its deep barrel on short legs; a thick neck arched high and a
# long head carried pointing down, an orange-red muzzle and a red-orange smear up its forehead; small pointed ears;
# heavy-lidded eyes, white with a dark pupil under a low brown lid, red beneath; a thick wavy mane of dark slate from
# the poll down the crest of the neck to the withers, braided dreadlocks hanging over the shoulders and one beside the
# face, each ending in a red-orange tip; each leg sunk to the belly in a great boot of red-orange mud in stacked rings,
# widest at the ground, a dark cleft at its front; a thick dark tail hanging down with red-orange ends.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, outward, srgb, inside, unit, polygon, eye_look, args, sibling

mb = sibling("mudbray")
bz = mb.bz
BROWN, MANE, MUD = "#8C5E48", "#2E3742", "#E0603E"
LEGDARK, CLEFT, NOSTRIL = "#6E3A2E", "#2A1E1C", "#1B1A1E"
WHITE, INK, GLINT = mb.WHITE, mb.INK, mb.GLINT
LID, UNDER = srgb(BROWN), srgb("#C8463A")


def boot(k, name, x, y, top, r0, r1, bone, rings=5):
    """A great boot of mud: rings stacked from top to the ground, each a squat ball, widening downward."""
    hz = top / rings * 0.62
    for i in range(rings):
        t = i / (rings - 1)
        z = top * (1 - t) + hz * t
        rr = r0 + (r1 - r0) * t
        k.add(f"{name}ring{i}", ell((x, y - 0.006 * t, z), (rr, rr * 1.04, hz)), MUD, bone, mirror=True)
    # the cleft at the front of the hoof: a dark wedge set into the boot's foot, standing just proud of it
    yf = y - 0.006 - r1 * 1.04
    k.add(name + "cleft", hull([(x - 0.02, yf + 0.004, 0.0), (x + 0.02, yf + 0.004, 0.0), (x, yf - 0.005, 0.0),
                                (x, yf - 0.002, 0.065), (x - 0.016, yf + 0.03, 0.0), (x + 0.016, yf + 0.03, 0.0),
                                (x, yf + 0.03, 0.065)]),
          CLEFT, bone, fuse=False, mirror=True, surface="shell")


def dread(k, name, pts, r, bone, tip_len, beads=True):
    """A braided dreadlock: a dark tube beaded along its length, ending in a red-orange tip."""
    pts = np.array(pts, float)
    n = 14
    s = np.linspace(0, 1, n)
    P = np.array([np.interp(s, np.linspace(0, 1, len(pts)), pts[:, j]) for j in range(3)]).T
    R = r * (1 - 0.25 * s) * (1 + (0.16 * np.cos(s * math.pi * 10) if beads else 0))
    k.add(name, outward(tube(P, R, seg=14, per=4)), MANE, bone, fuse=False, surface="hair")
    d = P[-1] - P[-2]; d /= np.linalg.norm(d)
    c = P[-1] + d * tip_len * 0.45
    k.add(name + "tip", outward(tube([P[-1] - d * 0.004, c, P[-1] + d * tip_len], [R[-1] * 1.15, R[-1] * 1.25,
                                                                                    R[-1] * 0.9], seg=14, per=4)),
          MUD, bone, fuse=False, surface="hair")


def heavy_eye(u, v):
    """The open eye: a white under a low brown lid, a dark pupil toward the front, red beneath (u outward, v up)."""
    r = np.sqrt(u * u + v * v)
    c = np.tile(UNDER, (len(u), 1))
    c[r < 0.8] = WHITE
    c[((u + 0.12) / 0.4) ** 2 + ((v + 0.05) / 0.55) ** 2 < 1] = INK
    c[np.hypot(u + 0.02, v - 0.0) < 0.12] = GLINT
    lid = 0.36 - 0.14 * u
    c[v > lid] = LID
    c[np.abs(v - lid) < 0.12] = INK
    c[(r > 0.8) & (v > lid)] = LID
    return c


def mudsdale(src, out):
    k = Kit(src, coat="fur")

    # --- the trunk: a deep heavy barrel, the chest and rump great round masses, the belly at a quarter of the height
    k.add("barrel", ell((0, 0.02, 0.33), (0.112, 0.21, 0.135)), BROWN, "body")
    k.add("chest", ell((0, -0.15, 0.35), (0.11, 0.105, 0.13)), BROWN, "body")
    k.add("rump", ell((0, 0.17, 0.35), (0.112, 0.11, 0.122)), BROWN, "body")

    # --- legs: shoulders and thighs fused low over them, a short dark shin, then the boot of mud ----------------
    k.add("shoulder", ell((0.074, -0.13, 0.27), (0.058, 0.07, 0.09)), BROWN, "leg{s}", mirror=True)
    k.add("thigh", ell((0.076, 0.165, 0.28), (0.062, 0.084, 0.1)), BROWN, "hind{s}", mirror=True)
    for name, y, bone in (("fore", -0.132, "leg{s}"), ("hind", 0.165, "hind{s}")):
        k.add(name, tube([(0.08, y, 0.25), (0.08, y - 0.004, 0.2)], [0.05, 0.046]), LEGDARK, bone, mirror=True)
        boot(k, name, 0.08, y, 0.19, 0.05, 0.066, bone)

    # --- a thick neck arched high; a long head pointing down and forward, the orange-red muzzle ----------------
    k.add("neck", tube([(0, -0.16, 0.38), (0, -0.2, 0.5), (0, -0.225, 0.6)], [0.1, 0.085, 0.07]), BROWN, "head")
    C, CR = np.array([0, -0.245, 0.64]), np.array([0.066, 0.075, 0.066])
    k.add("cranium", ell(C, CR), BROWN, "head")
    FC = np.array([0, -0.305, 0.575])
    k.add("face", ell(FC, (0.054, 0.056, 0.1), rot=(-36, 0, 0)), BROWN, "head")
    MZ, MR = np.array([0, -0.352, 0.505]), np.array([0.05, 0.05, 0.045])
    k.add("muzzle", ell(MZ, MR, rot=(-36, 0, 0)), MUD, "head", surface="skin")
    for s in (1, -1):
        dn = unit(0.35 * s, -0.9, -0.35)
        k.decal(MZ + np.array([0.022 * s, -0.04, -0.012]), dn, (0.01, 0.013), NOSTRIL, up=unit(-0.3 * s, 0.3, 1),
                eps=0.003, rings=8, segs=40)
    # the red-orange smear up the forehead
    fn = unit(0, -0.82, 0.57)
    k.decal(FC + fn * 0.054 + np.array([0, 0.006, 0.03]), fn, (0.024, 0.05), MUD, up=unit(0, 0.57, 0.82), eps=0.003,
            outline=polygon([(-1, 1), (1, 1), (0.6, -0.3), (0.25, -1), (-0.25, -1), (-0.6, -0.3)]), rings=10,
            segs=44)

    # --- small pointed ears, up and a little back ----------------------------------------------------------------
    k.add("ear", tube([(0.036, -0.235, 0.69), (0.046, -0.228, 0.73), (0.055, -0.222, 0.77)], [0.024, 0.017, 0.004]),
          BROWN, "ear{s}", mirror=True)

    # --- the mane: a thick wavy ridge of dark slate from the poll down the crest to the withers ------------------
    for i, t in enumerate(np.linspace(0, 1, 9)):
        p = np.array((0, -0.235, 0.71)) * (1 - t) + np.array((0, -0.06, 0.46)) * t
        p = p + np.array((0, 0.028 + 0.012 * math.sin(t * math.pi), 0.012 * math.sin(t * math.pi)))
        r = 0.05 + 0.012 * math.sin(t * math.pi)
        k.add(f"mane{i}", ell(p + (0.008 * (1 if i % 2 else -1), 0, 0), (r * 1.05, r * 0.9, r * 0.8),
                              rot=(-30 + 25 * t, 0, 0)), MANE, "head" if t < 0.55 else "body", surface="hair")
    # the mane's front over the poll, between the ears
    k.add("poll", ell((0, -0.25, 0.71), (0.034, 0.04, 0.03)), MANE, "head", surface="hair")
    # braided dreadlocks hanging over the shoulders, one beside the face, each with a red-orange tip
    for s in (1, -1):
        m = np.array([s, 1, 1.0])
        dread(k, "dreadA" + "RL"[s < 0], [np.array(p) * m for p in [(0.04, -0.16, 0.6), (0.085, -0.16, 0.5),
                                                                     (0.1, -0.17, 0.4), (0.112, -0.175, 0.33)]],
              0.026, "head", 0.05)
        dread(k, "dreadB" + "RL"[s < 0], [np.array(p) * m for p in [(0.04, -0.09, 0.52), (0.09, -0.07, 0.45),
                                                                     (0.118, -0.06, 0.37), (0.124, -0.05, 0.31)]],
              0.026, "body", 0.05)
    dread(k, "dreadF", [(0.05, -0.215, 0.6), (0.078, -0.27, 0.52), (0.074, -0.3, 0.45), (0.07, -0.31, 0.41)],
          0.022, "head", 0.045)

    # --- the tail: thick dark locks hanging back and down, splayed, red-orange at their ends ------------------------
    TB = np.array((0, 0.265, 0.43))
    k.add("tailroot", ell(TB, (0.04, 0.04, 0.04)), MANE, "tail", surface="hair")
    for i, (dx, dz, L) in enumerate([(0.0, -0.85, 0.2), (0.45, -0.7, 0.18), (-0.45, -0.7, 0.18), (0.22, -0.4, 0.16),
                                     (-0.22, -0.4, 0.16)]):
        d = unit(dx, 0.5, dz)
        bend = np.array((0, 0.02, 0.0))
        dread(k, f"tail{i}", [TB, TB + d * L * 0.5 + bend, TB + d * L + bend * 0.5], 0.034, "tail", 0.05,
              beads=False)

    # --- the eyes: heavy-lidded, on the sides of the head turned a little forward ----------------------------------
    de = unit(0.74, -0.6, 0.15)
    E = bz.on_ell(C + (0, -0.012, -0.012), CR, de)
    def eyes(state):
        k.decal(E, de, (0.031, 0.026), eye_look(state, heavy_eye, LID, INK, line=0.16), up=unit(0, 0.2, 1),
                eps=0.005, rings=20, segs=56, mirror=True)

    k.build(voxel=0.0065, smooth=4, band=0.006)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    mudsdale(*args())
