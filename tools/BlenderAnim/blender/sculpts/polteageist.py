# Polteageist sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/polteageist.py -- <the export's Polteageist.glb> <out.glb>
#
# Grown from Sinistea's script (sibling("sinistea")): the same porcelain colours, gold rings, teal curls and purple
# tea. From the references: a round teapot, its upper part pale aqua with teal curls over it and a teal line with
# little waves at its widest, its lower part white; a gold ring round its foot and two purple legs of tea stepping out
# under it; a white spout on its -X side (the viewer's left) and a gold handle like a question mark on +X; a crack in
# its +X front through which the tea shows, purple above and peach below. Out of its open top rises the tea itself,
# a purple ghost with orange curls for eyes and a small smile, its little fingers gripping the pot's front rim and an
# arm reaching over the +X side, the white lid with its aqua band, gold rim and gold knob lifted on its head and tilted
# back so the face shows under it. (The task's text spoke of the ghost flowing from the spout and lid: the pictures
# show it rising from the pot's top under the lid; the spout is plain.)
# The skeleton: body (0, 0, 0.17) alone; everything is the body's.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, lathe, outward, polygon, args, sibling

si = sibling("sinistea")

AQUA, TEAL, TEAL_DARK, GOLD = si.AQUA, si.TEAL, si.TEAL_DARK, si.GOLD
TEA, TEA_DEEP = si.TEA, si.TEA_DEEP
PORCELAIN, LAVENDER = "#F4F5F8", "#DCDAEC"
PEACH, SWIRL_ORANGE, SWIRL_INK = "#F2B488", "#F6A43A", "#B9561C"
GHOST_INK = "#4A1F66"

# --- the frame ----------------------------------------------------------------------------------------------------
POT = [(0.07, 0.066), (0.104, 0.082), (0.13, 0.112), (0.141, 0.15), (0.138, 0.185), (0.124, 0.222), (0.102, 0.25),
       (0.084, 0.27)]
TOP = 0.272
WAIST = 0.146                                       # the line between the aqua above and the white below
GHOST_C, GHOST_R = np.array([0.0, -0.006, 0.316]), np.array([0.064, 0.056, 0.054])
LID_C, LID_D = np.array([0.0, 0.004, 0.346]), (0.0, 0.16, 1.0)
EYE_X, EYE_Z, EYE_HALF = 0.026, 0.322, (0.018, 0.02)
VOXEL = 0.0032


def on_pot(a, z, inset=0.002):
    """A point on the pot's side at azimuth a (from the front, +X positive) and height z, and the normal there."""
    r = si.radius_at(z, POT)
    dz = 0.004
    slope = (si.radius_at(z + dz, POT) - si.radius_at(z - dz, POT)) / (2 * dz)
    n = unit(math.sin(a), -math.cos(a), -slope)
    return np.array([r * math.sin(a), -r * math.cos(a), z]) - n * inset, n


def on_ell(c, r, d):
    d = unit(*d)
    c, r = np.array(c, float), np.array(r, float)
    p = c + d / math.sqrt(((d / r) ** 2).sum())
    n = (p - c) / r ** 2
    return p, n / np.linalg.norm(n)


def waist(az):
    return WAIST + 0.006 * np.cos(8 * az)


def crack_look(u, v):
    """The tea seen through the crack: purple above, peach below, blended across its middle."""
    t = np.clip((0.35 - v) / 0.9, 0, 1)[:, None]
    return srgb(TEA) * (1 - t) + srgb(PEACH) * t


CRACK = polygon([(0.0, 1.0), (0.55, 0.78), (0.95, 0.25), (0.82, -0.45), (0.35, -1.0), (-0.45, -0.88), (-0.95, -0.15),
                 (-0.72, 0.6)])


def swirl_eye(u, v):
    """The ghost's eye: an orange disc with a darker curl in it and a dark purple rim."""
    c = si.curl_look(SWIRL_ORANGE, SWIRL_INK, width=0.12)(u, v)
    c[np.hypot(u, v) > 0.84] = srgb(GHOST_INK)
    return c


# --- the parts ----------------------------------------------------------------------------------------------------
def pot(k):
    k.add("pot", si.cup_solid(POT, TOP), PORCELAIN, "body")
    az = lambda P: np.arctan2(P[:, 0], -P[:, 1])
    rad = lambda P: np.hypot(P[:, 0], P[:, 1])
    k.paint(lambda P: inside(P[:, 2] - 0.1, 0.02) * 0.7, LAVENDER, on=["pot"])
    k.paint(lambda P: inside(waist(az(P)) - P[:, 2], 0.0018), AQUA, on=["pot"])
    k.paint(lambda P: inside(np.abs(P[:, 2] - (waist(az(P)) + 0.002)) - 0.0026, 0.0012), TEAL, on=["pot"])
    k.paint(lambda P: inside(TOP - 0.005 - P[:, 2], 0.0015) * inside(rad(P) - 0.08, 0.0015), TEA, on=["pot"])
    # curls over the aqua, none where the crack is
    for a in (0.0, -1.05, -2.1, 2.25, math.pi):
        p, n = on_pot(a, 0.2)
        k.decal(p, n, (0.026, 0.026), si.curl_look(AQUA, TEAL, width=0.11), eps=0.0025, rings=18, segs=64)
    # the crack on the +X front
    p, n = on_pot(1.0, 0.162)
    k.decal(p, n, (0.046, 0.058), crack_look, eps=0.0028, outline=CRACK, rings=14, segs=72)
    # the white rim of the opening, the foot and its gold ring
    k.add("rim", si.gold_ring((0, 0, TOP - 0.002), 0.083, 0.0065), PORCELAIN, "body", fuse=False)
    k.add("foot", lathe((0, 0, 0), (0, 0, 1), [(0.0, 0.056), (0.072, 0.056), (0.075, 0.064), (0.07, 0.072),
                                                (0.0, 0.074)], seg=64), PORCELAIN, "body", fuse=False)
    k.add("footring", lathe((0, 0, 0), (0, 0, 1), [(0.0, 0.042), (0.082, 0.042), (0.088, 0.049), (0.085, 0.058),
                                                    (0.0, 0.058)], seg=64), GOLD, "body", fuse=False, surface="metal")


def spout(k):
    pts = [(-0.118, 0.0, 0.142), (-0.168, 0.0, 0.156), (-0.212, 0.0, 0.196), (-0.238, 0.0, 0.238),
           (-0.252, 0.0, 0.252)]
    k.add("spout", outward(tube(pts, [0.036, 0.026, 0.019, 0.016, 0.017])), PORCELAIN, "body", fuse=False)


def handle(k):
    pts = [(0.122, 0.0, 0.222), (0.172, 0.0, 0.246), (0.214, 0.0, 0.226), (0.222, 0.0, 0.18), (0.198, 0.0, 0.132),
           (0.15, 0.0, 0.106), (0.13, 0.0, 0.104)]
    k.add("handle", outward(tube(pts, [0.011, 0.0105, 0.0105, 0.0105, 0.0105, 0.011, 0.012])), GOLD, "body",
          fuse=False, surface="metal")


def ghost(k):
    """The tea risen out of the pot: a rounded head with the face, fingers on the front rim, an arm over +X."""
    k.add("ghost", ell(GHOST_C, GHOST_R, deform=lambda V: V * (1 + 0.1 * np.clip(-V[:, 2:3], 0, 1))), TEA, "body",
          surface="skin")
    k.add("ghostneck", ell((0.0, 0.0, 0.272), (0.07, 0.066, 0.03)), TEA, "body", surface="skin")
    for s in (1, -1):
        nm = "R" if s > 0 else "L"
        k.add("palm" + nm, ell((0.036 * s, -0.084, 0.274), (0.022, 0.014, 0.012)), TEA, "body", surface="skin")
        for j, dx in enumerate((-0.013, 0.0, 0.013)):
            k.add(f"finger{nm}{j}", tube([(0.036 * s + dx, -0.09, 0.276), (0.036 * s + dx, -0.098, 0.266)],
                                         [0.0075, 0.0068]), TEA, "body", surface="skin")
    # the arm over the +X side, its hand on the rim there
    k.add("arm", tube([(0.05, 0.0, 0.31), (0.09, -0.006, 0.312), (0.11, -0.016, 0.29), (0.1, -0.03, 0.276)],
                      [0.016, 0.014, 0.013, 0.013]), TEA, "body", surface="skin")
    k.add("hand", ell((0.096, -0.036, 0.272), (0.018, 0.016, 0.011)), TEA, "body", surface="skin")
    # the legs stepping out under the foot
    for s in (1, -1):
        nm = "R" if s > 0 else "L"
        k.add("leg" + nm, tube([(0.03 * s, 0.0, 0.08), (0.038 * s, -0.006, 0.04), (0.044 * s, -0.012, 0.022)],
                               [0.017, 0.016, 0.015]), TEA, "body", surface="skin")
        k.add("foot" + nm, ell((0.046 * s, -0.022, 0.013), (0.021, 0.027, 0.013)), TEA, "body", surface="skin")
    # the smile
    def smile(u, v):
        c = np.tile(srgb(TEA), (len(u), 1))
        line = -0.2 + 0.55 * u * u
        c[(np.abs(v - line) < 0.27) & (np.abs(u) < 0.9)] = srgb(SWIRL_ORANGE)
        return c
    p, n = on_ell(GHOST_C, GHOST_R, (0.0, -1.0, -0.42))
    k.decal(p, n, (0.016, 0.008), smile, eps=0.0022, rings=10, segs=56)


def lid(k):
    """The lid lifted on the ghost's head and tilted back: white, an aqua band, a gold rim and knob."""
    d = LID_D
    k.add("lidlow", lathe(LID_C, d, [(0.0, 0.0), (0.094, 0.0), (0.0935, 0.012), (0.0895, 0.022), (0.0, 0.022)],
                          seg=64), PORCELAIN, "body", fuse=False)
    k.add("lidband", lathe(LID_C, d, [(0.0, 0.0215), (0.0905, 0.0215), (0.0795, 0.039), (0.0, 0.039)], seg=64),
          AQUA, "body", fuse=False)
    k.add("lidtop", lathe(LID_C, d, [(0.0, 0.0385), (0.0795, 0.0385), (0.062, 0.053), (0.036, 0.061),
                                     (0.0, 0.063)], seg=64), PORCELAIN, "body", fuse=False)
    k.add("lidrim", si.gold_ring(LID_C, 0.094, 0.0055, axis=d), GOLD, "body", fuse=False, surface="metal")
    k.add("knob", lathe(LID_C, d, [(0.0, 0.058), (0.012, 0.058), (0.01, 0.07), (0.024, 0.076), (0.025, 0.085),
                                   (0.014, 0.092), (0.0, 0.094)], seg=40), GOLD, "body", fuse=False, surface="metal")


def lay_eyes(k, state):
    for s in (1, -1):
        p, n = on_ell(GHOST_C, GHOST_R, (EYE_X * s / GHOST_R[0] * 1.2, -1.0, (EYE_Z - GHOST_C[2]) / GHOST_R[2]))
        look = eye_look(state, swirl_eye, srgb(TEA), srgb(GHOST_INK), line=0.16)
        k.decal(p, n, EYE_HALF, look if s > 0 else (lambda u, v, f=look: f(-u, v)), eps=0.0022, rings=20, segs=56)


def polteageist(src, dst):
    k = Kit(src, coat="shell")
    pot(k)
    spout(k)
    handle(k)
    ghost(k)
    lid(k)
    k.build(voxel=VOXEL, smooth=3, band=0.004, wband=0.03)
    k.eye_states(lambda st: lay_eyes(k, st))
    k.export(dst)


if __name__ == "__main__":
    polteageist(*args())
