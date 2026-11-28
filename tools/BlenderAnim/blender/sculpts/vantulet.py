# Vantulet (No. 10113, Carpathia; Vântuleț) sculpted from nothing on the skeleton and clips of the game's own export of
# it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/vantulet.py -- <the export's Vantulet.glb> <out.glb>
#
# The design, from its picture (10113-Vantulet.jpg) and its brief (CP-dex-2b, "113. Vantulet: a little whirl of
# wind"): a little whirl of pale air. Its head is a big round puff, pale blue-white, its top wound into a spiral like
# a rolled cream (the spiral's eye high on the front, so the game's cameras see it), a soft blue line in its turns.
# A round face low on the front: two small dark eyes with a white glint and a little round whistling mouth. Two swirls
# for arms curl out of its sides under the chin. Below the head the air winds down in a broad ribbon, a loop and a
# half round an open middle, narrowing to a pointed tail; a single green leaf with a darker midrib is caught in the
# ribbon on its right (+X). The picture's loose wisps and motion lines are left out.
#
# The skeleton: body (0, 0, 0.43), tail (0, -0.02, 0.35), arms (+-0.1, -0.06, 0.39).
#
# Written to be grown from (vantoroi.py imports it with sibling("vantulet")): the spiral, the swirl curl, the ribbon,
# the leaf and the eyes are functions taking what changes; Vantulet's calls are under __main__.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, sheet, srgb, inside, unit, eye_look, args, slab, axes

# --- the line's colours ---------------------------------------------------------------------------------------------
AIR, AIR_LOW, LINE = "#E4EEF7", "#C9DCEE", "#93B6D8"
LEAF, RIB = "#6EAA4C", "#477E34"
EYE_DARK = srgb("#33404F")
INK = srgb("#33404F")
WHITE = np.array([1.0, 1.0, 1.0])

# --- Vantulet's frame -----------------------------------------------------------------------------------------------
HC = np.array([0.0, -0.01, 0.425])
HR = np.array([0.135, 0.12, 0.118])
SPIRAL_AXIS = (0.15, -0.45, 0.88)        # the spiral's eye high on the front, toward the cameras
EYE_D = (0.37, -0.92, -0.08)
EYE_HALF = (0.02, 0.026)
MOUTH_D = (0.0, -1.0, -0.43)
VOXEL = 0.0042


def on_ell(c, r, d):
    d = unit(*d)
    c, r = np.array(c, float), np.array(r, float)
    p = c + d / math.sqrt(((d / r) ** 2).sum())
    n = (p - c) / r ** 2
    return p, n / np.linalg.norm(n)


def spiral_mask(c, axis, pitch, width, reach, cap, sign=1.0):
    """A paint mask: an Archimedean spiral's line round an axis through c, on the cap where (P - c) . axis > cap."""
    a = unit(*axis)
    e1, e2 = axes(a)
    c = np.array(c, float)
    def mask(P):
        q = P - c
        h = q @ a
        x, y = q @ e1, q @ e2
        r = np.sqrt(x * x + y * y)
        th = np.arctan2(y, x) * sign
        f = (r / pitch - th / (2 * math.pi)) % 1.0
        line = inside(np.abs(f - 0.5) - width, 0.06)
        return line * inside(r - reach, 0.004) * inside(cap - h, 0.004) * (r > 0.006)
    return mask


def spiral_ridge(c, r, axis, pitch, turns, lift, radius, sign=1.0, start=0.012):
    """A raised turn of air along the spiral on the ellipsoid's cap: a tube following the surface."""
    a = unit(*axis)
    e1, e2 = axes(a)
    pts = []
    for t in np.linspace(0, turns, int(turns * 18) + 2):
        rr = start + pitch * t
        th = 2 * math.pi * t * sign
        d = a * 1.0 + (e1 * math.cos(th) + e2 * math.sin(th)) * (rr / max(r) * 1.25)
        p, nrm = on_ell(c, r, d)
        pts.append(p + nrm * lift)
    pts = np.array(pts)
    return tube(pts, np.linspace(radius * 0.6, radius, len(pts)), seg=12, per=3)


def curl(root, out_dir, face, radius, turns, thick, lead=0.05, sign=1.0, n=26):
    """A swirl: a short lead out from the root, then a spiral of shrinking radius in the plane facing `face`."""
    root = np.array(root, float)
    o = unit(*out_dir)
    f = unit(*face)
    up = np.cross(f, o); up /= np.linalg.norm(up)
    start = root + o * lead
    centre = start + up * radius * sign
    pts = [root, root + o * lead * 0.5]
    for t in np.linspace(0, turns, n):
        rr = radius * (1 - 0.72 * t / turns)
        th = -math.pi / 2 * sign + 2 * math.pi * t * sign
        pts.append(centre + (o * math.cos(th) + up * math.sin(th) * sign) * rr)
    pts = np.array(pts)
    rad = np.concatenate([[thick * 1.15, thick * 1.1], np.linspace(thick, thick * 0.62, n)])
    return tube(pts, rad, seg=14, per=3)


def ribbon(top, bottom, turns, r0, w0, w1, drift=(0.03, 0.0), th0=0.0, flare=0.45, thick=0.02, nu=90):
    """The whirl winding down: a broad band round an open middle, its radius and width narrowing to a pointed tail."""
    def at(u, v):
        u = 0.012 + 0.976 * u
        th = th0 + 2 * math.pi * turns * u
        r = r0 * (1 - u) ** 0.85 + 0.008
        z = top - (top - bottom) * u ** 0.9
        w = w0 * (1 - u) + w1 * u
        cx, cy = drift[0] * u, drift[1] * u
        ca, sa = np.cos(th), np.sin(th)
        off = (v - 0.5) * w
        x = cx + (r + off * flare) * ca
        y = cy + (r + off * flare) * sa
        return np.stack([x, y, z + off], axis=-1)
    return sheet(at, nu, 5, thick)


def leaf(k, name, stem, tip, face, width, bone, colour=LEAF, rib=RIB, thick=0.016):
    """A leaf as a plate from its stem to its tip, its face turned to `face`; its midrib a narrower plate thicker than it,
    so the rib shows on both faces."""
    stem, tip = np.array(stem, float), np.array(tip, float)
    d = tip - stem; L = np.linalg.norm(d); d /= L
    f = unit(*face)
    across = np.cross(f, d); across /= np.linalg.norm(across)
    pts = [(t * L, width * math.sin(math.pi * min(1, t * 1.08)) ** 0.8 * (1 - 0.2 * t)) for t in np.linspace(0, 1, 12)]
    out = pts + [(a, -b) for a, b in pts[::-1][1:-1]]
    k.add(name, slab(out, stem, d, across, thick), colour, bone, fuse=False, surface="leaf")
    k.add(name + "rib", slab([(0.0, 0.004), (L * 0.85, 0.0025), (L * 0.85, -0.0025), (0.0, -0.004)], stem, d, across,
                             thick * 1.5), rib, bone, fuse=False, surface="leaf")


def small_eye(dark=EYE_DARK):
    """The line's small eye: dark, a white glint high toward the outside."""
    def look(u, v):
        c = np.tile(dark, (len(u), 1))
        c[((u - 0.2) / 0.34) ** 2 + ((v - 0.32) / 0.3) ** 2 < 1] = WHITE
        return c
    return look


def lay_eyes(k, c, r, d, half, look, lid, state, ink=INK, eps=0.003, outline=None):
    for side in (1, -1):
        p, nrm = on_ell(c, r, (d[0] * side, d[1], d[2]))
        f = eye_look(state, look, srgb(lid) if isinstance(lid, str) else lid, ink, line=0.16)
        col = f if side > 0 else (lambda u, v, f=f: f(-u, v))
        o = outline if outline is None or side > 0 else (lambda th, g=outline: g(math.pi - th))
        k.decal(p, nrm, half, col, eps=eps, rings=20, segs=56, outline=o)


def board(k):
    """The board check on the built body (PokemonStudio.Strip): the span at 31.5 degrees against its square."""
    V = np.array([v.co[:] for v in k.ob.data.vertices])
    a = math.radians(31.5)
    proj = V[:, 0] * math.cos(a) + V[:, 1] * math.sin(a)
    h, w, l = V[:, 2].max(), np.ptp(V[:, 0]), np.ptp(V[:, 1])
    print("KIT: board: shows %.3f in a square of %.3f" % (np.ptp(proj), 1.3 * max(h, 0.85 * max(w, l))))


def vantulet(src, out):
    k = Kit(src, coat="fur")
    k.add("head", ell(HC, HR), AIR, "body")
    k.add("ridge", spiral_ridge(HC, HR, SPIRAL_AXIS, 0.042, 2.1, 0.0, 0.013), AIR, "body")
    k.paint(spiral_mask(HC, SPIRAL_AXIS, 0.042, 0.13, 0.1, 0.03), LINE, on=["head", "ridge"])
    # a smaller swirl of the same line on the back of the head, for the back camera
    k.paint(spiral_mask(HC, (-0.25, 0.9, 0.35), 0.03, 0.14, 0.07, 0.06), LINE, on=["head"])
    # the whirl winding down into a tail
    k.add("whirl", ribbon(0.335, 0.075, 1.4, 0.1, 0.135, 0.026, drift=(0.03, 0.0), th0=-0.4, thick=0.024), AIR_LOW, "tail")
    k.add("neck", ell((0.0, 0.0, 0.325), (0.075, 0.07, 0.04), sub=3), AIR, "body")
    # two swirls for arms, under the chin, their faces toward the front
    k.add("arm", curl((0.095, -0.05, 0.355), (1.0, -0.35, -0.25), (0.35, -0.94, 0.0), 0.032, 1.25, 0.0125),
          AIR, "arm{s}", mirror=True)
    leaf(k, "leaf", (0.085, -0.075, 0.24), (0.175, -0.02, 0.395), (0.55, -0.83, 0.1), 0.032, "tail")
    # the round whistling mouth, in every state
    p, nrm = on_ell(HC, HR, MOUTH_D)
    k.decal(p, nrm, (0.0095, 0.012), INK, eps=0.003, rings=8, segs=36)
    k.build(voxel=VOXEL, smooth=4, band=0.006, wband=0.04)
    board(k)

    def lay(state):
        lay_eyes(k, HC, HR, EYE_D, EYE_HALF, small_eye(), AIR, state)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    vantulet(*args())
