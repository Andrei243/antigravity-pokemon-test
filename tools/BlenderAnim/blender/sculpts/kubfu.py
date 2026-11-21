# Kubfu sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/kubfu.py -- <the export's Kubfu.glb> <out.glb>
# Written to be grown from: urshifu.py imports its frame (sibling("kubfu")): the black eye with its glint cut along
# its top by a heavy black brow, the yellow muzzle with its black nose and frown, the pointed tufts of fur, the white
# fur swept up off the brow and trailing behind, the black claw-points of the feet.
#
# The design, from the references: a small warm-grey bear cub of a fighter; a great round head some two fifths of the
# height (over half with its tufts), ringed with pointed grey tufts (a tall one on the crown, several out at each side
# and behind), round grey ears on the crown's corners; a white fringe of fur across the brow, swept up to the right
# (+X) and trailing back behind the head on that side; heavy black brows slanting down to the nose over black eyes
# with a white glint; a pale yellow muzzle with a black nose and a frown; a white point of fur at the throat; a small
# narrow body, short arms with black fists carried forward and down; long legs in a wide stance, black feet with
# three claw-points. No tail is seen.
# Reference ratios (3D picture): head (chin to ear top) 0.4 of the height, 0.55 with the crown's tuft; head width /
# body width 1.9 (2.2 with the side tufts); muzzle width / head width 0.3.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, polygon, eye_look, args, sibling

t = sibling("teddiursa")
sk = sibling("skiddo")
hit = t.hit

GREY, WHITE, BLACK, MUZZLE = "#B4ABA2", "#F6F5F2", "#25242A", "#ECE5A2"
NOSE, MOUTH = "#1C1B20", "#3A3530"
INK, GLINT = srgb("#141418"), np.ones(3)


def spikes(k, root, list_, colour, bone="head", tag="spk", mirror=False, surface="fur"):
    """Pointed solid tufts of fur from root + offset: each (offset, direction, length, half-width, half-thickness).
    mirror: also at -X, the offset and direction mirrored by hand."""
    root = np.array(root, float)
    for i, (o, d, L, W, T) in enumerate(list_):
        sides = ((1, "R"), (-1, "L")) if mirror else ((1, ""),)
        for s, nm in sides:
            m = np.array([s, 1, 1.0])
            sk.spike(k, f"{tag}{i}{nm}", root + np.array(o) * m, np.array(d) * m, L, W, T, bone, colour,
                     mid=0.35, belly=0.75, surface=surface)


def bear_eye(at=(0.32, 0.25), size=0.3, low=None):
    """A solid black eye with a white glint high on its outer side (u outward, v up); low: a second small glint."""
    def look(u, v):
        c = np.tile(INK, (len(u), 1))
        c[((u - at[0]) / size) ** 2 + ((v - at[1]) / size) ** 2 < 1] = GLINT
        if low is not None:
            c[((u - low[0]) / low[2]) ** 2 + ((v - low[1]) / low[2]) ** 2 < 1] = GLINT
        return c
    return look


def brow_line(BA, BB):
    """The eye's top cut straight along v = BA + BB u (u outward): sloping up to the outer side, a scowl."""
    def cut(th):
        s = math.sin(th) - BB * math.cos(th)
        return min(1.0, BA / s) if s > 1e-6 else 1.0
    return cut


def brow_eyes(k, surface, E, de, half, BA, BB, lid, brow_col, brow_t, brow_span=(-0.9, 1.25), look=None,
              line_col=None):
    """
    Eyes cut along their top by a heavy brow, and the brow itself (a slanting bar on the fused surface, laid before
    build, so in every state). surface: the part (V, F) the brow's centre is found on. half: the eye's half sizes.
    Returns the lay function for eye_states.
    """
    m = np.array([-1, 1, 1.0])
    cut = brow_line(BA, BB)
    look = look or bear_eye()
    up = np.array([0, 0, 1.0])
    # The brow: its lower edge the eye's cut line, brow_t thick, from u = brow_span[0] to brow_span[1] (eye units)
    a, b = half
    def low(x):                    # the cut line's height in metres at x metres outward of the eye's middle
        return b * (BA + BB * x / a) - 0.004
    mid_x = a * (brow_span[0] + brow_span[1]) / 2
    dz = low(mid_x) + brow_t / 2
    right = np.cross(up, de); right /= np.linalg.norm(right)
    BC = hit(surface, E + right * mid_x + up * dz + de * 0.3, -de)
    hb = (a * (brow_span[1] - brow_span[0]) / 2 + 0.002, brow_t * 1.2)
    pts = []
    for x in np.linspace(a * brow_span[0], a * brow_span[1], 6):
        pts.append(((x - mid_x) / hb[0], (low(x) - dz) / hb[1]))
    for x in np.linspace(a * brow_span[1], a * brow_span[0], 6):
        pts.append(((x - mid_x) / hb[0], (low(x) + brow_t - dz) / hb[1]))
    BOUT = polygon(pts)
    k.decal(BC, de, hb, brow_col, eps=0.0025, rings=10, segs=64, outline=BOUT, up=up)
    k.decal(BC * m, de * m, hb, brow_col, eps=0.0025, rings=10, segs=64, outline=lambda th: BOUT(math.pi - th),
            up=up * m)

    def lay(state):
        lk = eye_look(state, look, lid, INK if line_col is None else line_col, line=0.2)
        k.decal(E, de, half, lk, eps=0.0035, rings=20, segs=56, outline=cut, up=up)
        k.decal(E * m, de * m, half, lambda u, v: lk(-u, v), eps=0.0035, rings=20, segs=56,
                outline=lambda th: cut(math.pi - th), up=up * m)
    return lay


def muzzle_face(k, MZ, MR, nose_c, nose_half, mouth_c, mouth_half, bone="head"):
    """The yellow muzzle (a fused oval standing forward), a black rounded-triangle nose on its top and a frown with a
    short line up to the nose. Returns the muzzle's shape."""
    SH = ell(MZ, MR)
    k.add("muzzle", SH, MUZZLE, bone)
    NC = hit(SH, np.array(nose_c) + (0, -0.5, 0), (0, 1, 0))
    k.decal(NC, unit(0, -1, 0.25), nose_half, NOSE, eps=0.003, rings=10, segs=48,
            outline=polygon([(-1, 0.55), (-0.6, 0.95), (0.6, 0.95), (1, 0.55), (0.25, -0.9), (-0.25, -0.9)]))
    CM, CY = srgb(MOUTH), srgb(MUZZLE)
    def frown(u, v):
        c = np.tile(CY, (len(u), 1))
        arch = -0.55 + 0.75 * (1 - u * u)
        c[(np.abs(v - arch) < 0.2) & (np.abs(u) < 0.85)] = CM
        c[(np.abs(u) < 0.09) & (v > arch) & (v < 0.95)] = CM
        return c
    MC = hit(SH, np.array(mouth_c) + (0, -0.5, 0), (0, 1, 0))
    k.decal(MC, unit(0, -1, -0.1), mouth_half, frown, eps=0.003, rings=10, segs=56)
    return SH


def clawed_foot(k, bone, F, FR, s, colour, claw_col, L, W, T, tag="toe"):
    """A foot of three toes fused over its front, each tapering to a point (L long, W its root's radius)."""
    k.add("foot" + bone[-1], ell(F, FR), colour, bone)
    for i, dx in enumerate((-0.7, 0.0, 0.7)):
        root = F + np.array([dx * FR[0] * s, -FR[1] * 0.55, -FR[2] * 0.15])
        d = unit(0.3 * dx * s, -1, -0.12)
        k.add(f"{tag}{bone[-1]}{i}", tube([root, root + d * L * 0.6, root + d * L], [W, W * 0.75, W * 0.35]),
              claw_col, bone)


H = np.array([0.0, -0.012, 0.318])            # the head's middle (its bone is at 0, 0, 0.3)
HR = np.array([0.108, 0.097, 0.099])


def kubfu(src, out_path):
    k = Kit(src, coat="fur")

    # --- a small narrow body, a pear; a white point of fur at the throat ----------------------------------------
    def pear(V):
        V = V.copy()
        s = 1 + 0.1 * np.clip(-V[:, 2], 0, 1) - 0.08 * np.clip(V[:, 2], 0, 1)
        V[:, 0] *= s; V[:, 1] *= s
        return V
    k.add("body", ell((0, 0.002, 0.185), (0.064, 0.054, 0.07), deform=pear), GREY, "body")
    k.add("hips", ell((0, 0.006, 0.145), (0.066, 0.054, 0.04)), GREY, "body")

    # --- long legs in a wide stance: thighs fused into the hips, the shins tapering, black clawed feet ------------
    for s, bone in ((1, "legR"), (-1, "legL")):
        m = np.array([s, 1, 1.0])
        k.add("thigh" + bone[-1], ell(np.array((0.05, 0.004, 0.13)) * m, (0.044, 0.046, 0.05)), GREY, bone)
        k.add("leg" + bone[-1], tube([np.array(p) * m for p in [(0.052, 0.0, 0.12), (0.072, -0.008, 0.075),
                                                                 (0.088, -0.002, 0.03)]], [0.037, 0.031, 0.027]),
              GREY, bone)
        F = np.array((0.092, -0.016, 0.018)) * m
        clawed_foot(k, bone, F, (0.034, 0.042, 0.02), s, BLACK, BLACK, 0.034, 0.014, 0)
        # the black sock's top in points over the ankle
        def sock(P, F=F, s=s):
            d = np.hypot(P[:, 0] - F[0], P[:, 1] - F[1])
            a = np.arctan2(P[:, 1] - F[1], P[:, 0] - F[0])
            top = 0.036 + 0.01 * (1 - np.abs(np.sin(a * 2.5)))
            return inside(P[:, 2] - top, 0.0018) * inside(d - 0.05, 0.003)
        k.paint(sock, BLACK, on=["leg" + bone[-1], "foot" + bone[-1]])

    # --- short arms carried forward and down, black fists before the belly ---------------------------------------
    k.add("arm", tube([(0.064, -0.006, 0.245), (0.088, -0.036, 0.21), (0.076, -0.078, 0.19)], [0.027, 0.024, 0.022]),
          GREY, "arm{s}", mirror=True)
    k.add("fist", ell((0.07, -0.096, 0.186), (0.03, 0.029, 0.03)), BLACK, "arm{s}", mirror=True)

    # --- the great round head, the round ears, the yellow muzzle -----------------------------------------------
    HEAD = ell(H, HR, deform=lambda V: V * (1 + 0.05 * np.clip(-V[:, 2], 0, 1))[:, None])
    k.add("head", HEAD, GREY, "head")
    k.add("ear", ell((0.074, 0.012, 0.398), (0.036, 0.022, 0.034), rot=(0, -28, 0)), GREY, "head", mirror=True)
    muzzle_face(k, H + (0, -0.083, -0.052), (0.034, 0.024, 0.03), H + (0, -0.09, -0.032), (0.012, 0.008),
                H + (0, -0.09, -0.06), (0.018, 0.011))

    # the white point of fur at the throat, under the chin
    k.decal(hit(ell((0, 0.002, 0.185), (0.064, 0.054, 0.07), deform=pear), (0, -0.5, 0.205), (0, 1, 0)),
            unit(0, -1, 0.1), (0.02, 0.018), WHITE, eps=0.003, rings=10, segs=48,
            outline=polygon([(-1, 0.85), (-0.35, 0.95), (0, 0.6), (0.35, 0.95), (1, 0.85), (0.15, -0.95), (-0.15, -0.95)]))

    # --- the grey tufts round the head: a tall one on the crown, out at the sides, back behind ----------------------
    spikes(k, H, [((0.0, 0.02, 0.08), (0.18, 0.25, 1), 0.1, 0.036, 0.015)], GREY, tag="crown")
    spikes(k, H, [((-0.035, 0.03, 0.072), (-0.4, 0.4, 1), 0.065, 0.03, 0.014)], GREY, tag="crownb")
    spikes(k, H, [((0.095, 0.01, 0.05), (1, 0.25, 0.65), 0.055, 0.030, 0.014),
                  ((0.104, 0.0, 0.005), (1, 0.15, 0.1), 0.06, 0.031, 0.014),
                  ((0.098, -0.005, -0.04), (1, 0.15, -0.4), 0.055, 0.030, 0.014),
                  ((0.072, -0.03, -0.075), (0.75, -0.1, -0.75), 0.042, 0.024, 0.012),
                  ((0.06, 0.07, 0.02), (0.45, 1, 0.1), 0.05, 0.030, 0.014),
                  ((0.06, 0.07, -0.04), (0.45, 1, -0.4), 0.048, 0.027, 0.013)], GREY, tag="side", mirror=True)

    # --- the white fringe across the brow, swept up to the right (+X), and its plume trailing behind ---------------
    k.add("fringe", ell(H + (0.008, -0.062, 0.07), (0.088, 0.045, 0.03), rot=(-28, 0, -6)), WHITE, "head")
    spikes(k, H, [((0.04, -0.06, 0.075), (0.9, 0.15, 0.45), 0.085, 0.034, 0.015),
                  ((-0.02, -0.06, 0.085), (0.25, 0.3, 1), 0.06, 0.028, 0.013),
                  ((-0.06, -0.06, 0.06), (-0.9, 0.1, 0.35), 0.048, 0.025, 0.012),
                  ((0.07, 0.04, 0.0), (0.62, 0.78, -0.05), 0.12, 0.04, 0.015),
                  ((0.068, 0.055, -0.03), (0.5, 0.82, -0.3), 0.1, 0.036, 0.015)], WHITE, tag="fringe")

    # --- the eyes: black with a glint, cut along the top by heavy black brows sloping down to the nose ------------
    de = unit(0.32, -0.94, 0.04)
    E = hit(HEAD, H + (0.042, -0.5, -0.004), (0, 1, 0))
    lay = brow_eyes(k, HEAD, E, de, (0.02, 0.023), 0.42, 0.42, srgb(GREY), BLACK, 0.012, look=bear_eye(),
                    line_col=INK)

    k.build(voxel=0.0045, smooth=4, band=0.003, wband=0.035)
    k.eye_states(lay)
    k.export(out_path)


if __name__ == "__main__":
    kubfu(*args())
