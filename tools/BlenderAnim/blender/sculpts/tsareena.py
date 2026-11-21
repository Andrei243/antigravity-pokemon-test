# Tsareena sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/tsareena.py -- <the export's Tsareena.glb> <out.glb>
#
# Grown from Bounsweet's and Steenee's scripts (sibling): the same magenta, white and lime green, the cap and the
# round-ended leaf (now the brim's swoosh), Steenee's four-lobed bulb and its magenta top with points on the white.
# From the references: a fruit queen. A white egg of a head under a lime cap whose brim sweeps out in a broad flat
# swoosh to its left, curling down at its end; a small magenta bud of a crown on the cap's right with a yellow band;
# long green hair falling from the cap round the sides and the back like an open cape to near the ground, flaring out
# at its foot, rows of yellow ovals on it; haughty half-lidded magenta eyes under long magenta lashes swept up and
# out; a red calyx over the mouth, its two sides curling up; a slim coral chest, thin magenta arms folded before it,
# the hands clasped under the chin; a big white bulb of lobes for a skirt; two long magenta legs, full at the thigh,
# each in a boot whose top is a cup of points, tapering to a point at the ground.
# (The task called the legs purple: the pictures have the line's magenta, and a coral-red chest and a red calyx.)
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, sheet, lathe, hull, ring_band, srgb, inside, unit, tri, eye_look, args, \
    outward, sibling, polygon

b = sibling("bounsweet")
st = sibling("steenee")
MAGENTA, MAGENTA_DARK, WHITE, GREEN = b.MAGENTA, b.MAGENTA_DARK, b.WHITE, b.GREEN
HAIR = "#A2D466"
CORAL, CALYX, CALYX_DARK = "#EC5A70", "#DC3A64", "#A41C44"
YELLOW = "#F0EA3A"
HAIR_IN = "#6EA244"
LASH_HEX = "#C01C66"
LASH = srgb(LASH_HEX)
LID_LINE, IRIS, PUPIL = srgb("#6A0C34"), srgb("#B81E6C"), srgb("#4A0828")
WHITE_ = np.array([1.0, 1.0, 1.0])

# --- Tsareena's frame (joints: body (0, 0, 0.42), arms (+-0.025, 0, 0.54), head (0, 0, 0.57), legs (+-0.035, 0,
#     0.36); 0.85 tall) --------------------------------------------------------------------------------------------
BC = np.array([0.0, 0.0, 0.442])          # the bulb's middle
HC = np.array([0.0, -0.006, 0.682])       # the head's middle
HR = np.array([0.098, 0.09, 0.104])
CAP = np.array([0.0, 0.006, 0.738])
EYE_D = (0.44, -0.89, 0.02)
EYE_HALF = (0.032, 0.024)
# the hair: radius and depth (a share of the radius) down its length, the front edge's angle from the front
HAIR_TOP, HAIR_FOOT = 0.762, 0.06
HAIR_V = [0.0, 0.15, 0.35, 0.5, 0.7, 0.88, 1.0]
HAIR_R = [0.098, 0.124, 0.156, 0.18, 0.204, 0.236, 0.27]


def hair_at(sx=1, th_edge_top=62.0, th_edge_foot=36.0, inset=0.0):
    """
    One curtain of hair as a sheet, on the sx side: u from its front edge round to the middle of the back (where it
    meets the other), v down. Each curtain is a broad lobe of its own: fuller across its middle, its foot rounded
    (lowest at its middle) and flared out. inset draws it in (the darker lining inside it).
    """
    def at(u, v):
        r = np.interp(v, HAIR_V, HAIR_R) + 0.04 * np.sin(math.pi * u) * np.sqrt(v) - inset
        depth = np.interp(v, [0, 0.4, 1], [0.98, 0.86, 0.72])
        edge = math.radians(th_edge_top) + (math.radians(th_edge_foot) - math.radians(th_edge_top)) * v ** 1.5
        th = edge + (math.radians(182) - edge) * u                # angle from the front, toward +X
        foot = HAIR_FOOT + 0.09 * (2 * u - 1) ** 2
        z = HAIR_TOP - (HAIR_TOP - foot) * v + inset * 0.6 * v
        return np.stack([sx * r * np.sin(th), -r * depth * np.cos(th) + 0.012, z], axis=-1)
    return at


def hair_point(at, u, v):
    """A point of the hair's middle surface and its outward way there."""
    P = at(np.array([[u]]), np.array([[v]]))[0, 0]
    du = at(np.array([[u + 1e-3]]), np.array([[v]]))[0, 0] - P
    dv = at(np.array([[u]]), np.array([[v + 1e-3]]))[0, 0] - P
    n = np.cross(du, dv); n /= np.linalg.norm(n)
    if np.dot(n, P - np.array([0, 0, P[2]])) < 0:
        n = -n
    return P, n


def oval(c, n, a, bb, thick, along):
    """A flat oval spot lying on a surface (its long way along `along`): a loose flattened ball along n."""
    n = unit(*n)
    x = np.array(along, float); x = x - n * (x @ n); x /= np.linalg.norm(x)
    y = np.cross(n, x)
    V, F = ell((0, 0, 0), (bb, a, thick), sub=3)
    return outward((V @ np.stack([x, y, n], axis=1).T + np.array(c, float), F))




def haughty_eye(u, v):
    """Half shut under a heavy lid whose edge rises outward: a white, a large magenta iris, a dark pupil, a glint."""
    c = np.tile(srgb(WHITE), (len(u), 1))
    top = 0.12 + 0.22 * u
    inside_ = (u / 0.95) ** 2 + (v / 0.95) ** 2 < 1
    ir = ((u + 0.02) / 0.6) ** 2 + ((v + 0.12) / 0.7) ** 2 < 1
    c[inside_ & ir] = IRIS
    c[inside_ & (((u + 0.02) / 0.28) ** 2 + ((v + 0.12) / 0.36) ** 2 < 1)] = PUPIL
    c[inside_ & (((u - 0.16) / 0.15) ** 2 + ((v + 0.0) / 0.15) ** 2 < 1)] = WHITE_
    c[(u / 0.95) ** 2 + (v / 0.95) ** 2 > 0.82 ** 2] = LID_LINE
    c[~((u / 0.95) ** 2 + (v / 0.95) ** 2 < 1)] = srgb(WHITE)
    c[v > top] = srgb(WHITE)
    c[(np.abs(v - top) < 0.15) & (np.abs(u) < 0.95)] = LID_LINE
    return c


def tsareena(src, out):
    k = Kit(src, coat="scales")

    # --- the bulb: Steenee's (st.bulb), broader, with a lobe at the front; its magenta top under a coral chest
    st.bulb(k, BC, sx=1.45, sz=1.08, front=True, upto=0.52, on=["chest"])
    k.add("chest", tube([(0, 0.0, 0.49), (0, -0.004, 0.55), (0, -0.004, 0.6)], [0.044, 0.036, 0.026]), CORAL, "body")
    k.paint(lambda P: inside(P[:, 2] - 0.39, 0.003) * inside(np.hypot(P[:, 0], P[:, 1]) - 0.07), MAGENTA,
            on=["bulb", "lobe", "bottom"])

    # --- the legs: full at the thigh, tapering to a point; a boot's cup of points round each thigh
    for sx in (1, -1):
        side = "R" if sx > 0 else "L"
        pts = [(0.05 * sx, 0.002, 0.4), (0.048 * sx, -0.006, 0.27), (0.034 * sx, -0.012, 0.12),
               (0.024 * sx, -0.012, 0.03), (0.021 * sx, -0.01, 0.006)]
        k.add("leg" + side, tube(pts, [0.044, 0.034, 0.02, 0.009, 0.004]), MAGENTA, "leg" + side)
        for j in range(5):
            a = 2 * math.pi * (j + 0.25) / 5
            w = np.array([math.sin(a), -math.cos(a), 0])
            root = np.array([0.049 * sx, -0.004, 0.25]) + w * 0.03
            tip = root + w * 0.03 + np.array([0, 0, 0.1 + 0.025 * (j % 2)])
            e = np.cross(w, (0, 0, 1.0))
            k.add(f"cuff{side}{j}", hull([root + e * 0.02, root - e * 0.02, root - w * 0.014 + e * 0.012,
                                          root - w * 0.014 - e * 0.012, tip]), MAGENTA_DARK, "leg" + side,
                  fuse=False)

    # --- the arms: thin magenta, folded before the chest, the hands clasped under the chin
    for sx in (1, -1):
        side = "R" if sx > 0 else "L"
        sh = np.array([0.03 * sx, -0.004, 0.565])
        el = np.array([0.064 * sx, -0.04, 0.52])
        wr = np.array([0.02 * sx, -0.07, 0.548])
        k.add("arm" + side, tube([sh, el, wr], [0.015, 0.014, 0.0135]), MAGENTA, "arm" + side)
        k.add("hand" + side, ell(wr + np.array([-0.006 * sx, -0.004, 0.012]), (0.016, 0.014, 0.019)), MAGENTA,
              "arm" + side)

    # --- the head: a white egg, the red calyx over its mouth and the lashes over its eyes (in every state)
    def egg(V):
        V = V.copy()
        s = 1 + 0.05 * np.clip(-V[:, 2], 0, 1) - 0.08 * np.clip(V[:, 2], 0, 1) ** 2
        V[:, 0] *= s; V[:, 1] *= s
        return V
    k.add("head", ell(HC, HR, deform=egg, sub=5), WHITE, "head")
    k.add("neck", tube([(0, -0.002, 0.58), (0, -0.004, 0.63)], [0.03, 0.03]), WHITE, "head")
    # the red calyx over the mouth: one patch, its two sides curling up past the cheeks, a point down at its middle.
    # Cast level from the front, over the clasped hands; the outline's corners go round its middle in order
    cp, cn = b.on_ell(HC, HR, (0, -1, -0.5))
    CC, CD = srgb(CALYX), srgb(CALYX_DARK)
    def calyx(u, v):
        c = np.tile(CC, (len(u), 1))
        return c
    k.decal(cp, (0, -1, -0.12), (0.066, 0.044), calyx, eps=0.003, rings=20, segs=80,
            outline=polygon([(0.95, 0.55), (0.4, 0.36), (0.0, 0.42), (-0.4, 0.36), (-0.95, 0.55), (-0.8, 0.0),
                             (-0.35, -0.55), (0.0, -1.0), (0.35, -0.55), (0.8, 0.0)]))
    # the lashes: two tapered magenta strokes off each eye's outer top, swept up and out just past the head's outline
    for sx in (1, -1):
        p, n = b.on_ell(HC, HR, (0.66 * sx, -0.7, 0.3))
        for j, (ln_, rise) in enumerate(((0.06, 0.058), (0.046, 0.028))):
            pts = [p - n * 0.004, p + np.array([0.026 * sx, 0.004, rise * 0.45]) + n * 0.004,
                   p + np.array([ln_ * sx, 0.012, rise]) + n * 0.004]
            k.add(f"lash{'R' if sx > 0 else 'L'}{j}", outward(tube(pts, [0.0135, 0.009, 0.004])), LASH_HEX, "head",
                  fuse=False)

    # --- the cap, its brim's swoosh to the left (-X) curling down at its end, the crown's bud on its right
    b.cap(k, CAP, 0.106, 0.074, "head")
    k.add("swoosh", b.round_leaf((-0.03, 0.0, 0.79), (-0.25, -0.012, 0.72), 0.072, 0.02, (-0.1, -0.2, 1.0),
                                 cup=0.45, thick=0.034, end=0.05, full=0.3), GREEN, "head", fuse=False,
          surface="leaf")
    bud = np.array([0.04, 0.006, 0.826])
    k.add("bud", outward(ell(bud, (0.03, 0.028, 0.03), sub=3)), MAGENTA, "head", fuse=False)
    k.add("budband", ring_band(bud, (0.9, -0.2, 0.4), 0.031, 0.012, thick=0.008), YELLOW, "head", fuse=False)
    for j in range(4):
        a = 2 * math.pi * j / 4 + 0.4
        w = np.array([math.cos(a), math.sin(a), 0])
        tip = bud + np.array([0, 0, 0.046]) + w * 0.012
        k.add(f"budpt{j}", hull([bud + w * 0.016 + (0, 0, 0.012), bud + w * 0.004 + (0, 0, 0.016) + np.cross(w, (0, 0, 1)) * 0.01,
                                 bud + w * 0.004 + (0, 0, 0.016) - np.cross(w, (0, 0, 1)) * 0.01, tip]),
              MAGENTA_DARK, "head", fuse=False)

    # --- the hair: one thick cape round the sides and back, flaring at its foot, with rows of yellow ovals
    #     (on the head, as the game's own sculpt hangs it, so it stays under the cap in the clips)
    spots = [(0.06, 0.22, 0.016, 0.01), (0.1, 0.3, 0.018, 0.011), (0.07, 0.38, 0.015, 0.009),
             (0.1, 0.8, 0.03, 0.013), (0.2, 0.86, 0.032, 0.014), (0.3, 0.88, 0.03, 0.013), (0.4, 0.86, 0.026, 0.012),
             (0.55, 0.84, 0.03, 0.013), (0.68, 0.88, 0.028, 0.012), (0.5, 0.6, 0.024, 0.011)]
    for sx, side in ((1, "R"), (-1, "L")):
        at = hair_at(sx)
        k.add("hair" + side, outward(sheet(at, 40, 36, 0.034)), HAIR, "head", fuse=False, surface="leaf")
        k.add("lining" + side, outward(sheet(hair_at(sx, inset=0.026), 40, 36, 0.008)), HAIR_IN, "head",
              fuse=False, surface="leaf")
        for j, (u, v, a, bb) in enumerate(spots):
            P, n = hair_point(at, u, v)
            dv = at(np.array([[u]]), np.array([[v + 1e-3]]))[0, 0] - P
            k.add(f"spot{side}{j}", oval(P + n * 0.018, n, a, bb, 0.006, dv), YELLOW, "head", fuse=False,
                  surface="leaf")

    k.build(voxel=0.006, smooth=4, band=0.006)

    # --- the haughty eyes
    LID, INK = srgb(WHITE), LID_LINE
    ep, en = b.on_ell(HC, HR, EYE_D)
    def eyes(state):
        k.decal(ep, en, EYE_HALF, eye_look(state, haughty_eye, LID, INK, line=0.16), eps=0.003, rings=20, segs=56,
                mirror=True)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    tsareena(*args())
