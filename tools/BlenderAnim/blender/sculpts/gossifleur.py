# Gossifleur sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/gossifleur.py -- <the export's Gossifleur.glb> <out.glb>
#
# The design, from the references (they overrule the brief, which expected a green bud with leaf feet): a little
# flower with no legs at all. A great yellow bloom of five puffy pointed petals sits over its head like a bonnet,
# tipped toward the front so its top shows, a small cream flower of five petals at its middle; the two front petals
# leave a gap where the face peeps out under the brim. The face is small and cream, a mint-green point coming down
# from the crown between the eyes; tall dark brown eyes with a small yellow glint low down; a small smile. Under the
# face a mint-green leaf is its body: two round lobes over the chin with a notch between, two little points out at
# its sides like arms, tapering to a tip that curls down and to one side. Round the head behind and at the sides a
# ruff of long red petals curls out and down, their tips turning up like flames.
# Reference ratios (3D picture): bloom 0.85 of the height wide and 0.6 of it tall; the ruff 1.07 of the height wide;
# the face 0.19 of the height wide; the leaf body 0.43 of the height from its top to its tip.
#
# Written to be grown from (eldegoss.py imports it with sibling("gossifleur")): the face, the forehead's point, the
# eyes, the smile and the strap-like petals are functions here; the build is under __main__.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, unit, eye_look, args, outward, sibling, polygon

ct = sibling("cottonee")
on_ell = ct.on_ell

# --- the line's colours --------------------------------------------------------------------------------------------
YELLOW, YELLOW_DEEP = "#F7D21E", "#E6B414"
CENTRE_PETAL, CENTRE = "#F6EFC6", "#F2CF3C"
RED = "#E64A38"
MINT, MINT_DARK = "#5ECC9E", "#46B286"
FACE = "#F2EBB6"
EYE, GLINT = srgb("#3A2A22"), srgb("#F2C42A")
INK = srgb("#3A2A22")
SMILE = "#4A3A2A"

# --- Gossifleur's frame (joints: body (0, 0, 0.1), head (0, -0.01, 0.13)) ------------------------------------------
HC = np.array([0.0, -0.01, 0.112])       # the face's middle
HR = np.array([0.036, 0.032, 0.04])
BLOOM = np.array([0.0, 0.01, 0.178])     # the bloom's middle
TILT = unit(0, -0.5, 0.866)              # the bloom's face, tipped toward the front
EYE_D = (0.5, -0.86, 0.0)
EYE_HALF = (0.0098, 0.0136)


def frame_of(d, n):
    """A right-handed frame (across, n, d) for a part laid along d with its face to n."""
    d = unit(*d)
    n = np.array(n, float); n = n - d * (n @ d); n /= np.linalg.norm(n)
    a = np.cross(n, d)
    return np.stack([a, n, d], axis=1)


def puff_petal(c, d, n, length, width, thick, point=0.6):
    """A puffy pointed petal from its middle c along d (its tip at c + d * length), its face to n: an ellipsoid
    tapered to a point at the tip and a little to the root."""
    def shape(V):
        V = V.copy()
        t = V[:, 2]
        s = (1 - point * np.clip(t, 0, 1) ** 1.3) * (1 - 0.25 * np.clip(-t, 0, 1) ** 2)
        V[:, 0] *= s; V[:, 1] *= s
        return V
    V, F = ell((0, 0, 0), (width, thick, length), deform=shape)
    return V @ frame_of(d, n).T + np.array(c, float), F


def strap(root, d, length, drop, width, curl=0.2, sag=1.0):
    """A long strap-like petal from root out along the level direction d, falling by drop, its tip turned up by
    curl: the points and radii of its middle line, flattened across later."""
    d = np.array(d, float); d /= np.linalg.norm(d)
    root = np.array(root, float)
    pts, rad = [], []
    for t in np.linspace(0, 1, 6):
        z = -drop * (math.sin(t * math.pi * 0.62) ** sag) + curl * drop * max(t - 0.7, 0) ** 2 / 0.09
        pts.append(root + d * length * t + np.array([0, 0, z]))
        rad.append(width * (0.6 + 0.55 * math.sin(math.pi * min(t * 1.15, 1.0))) if t < 1 else width * 0.18)
    return pts, rad


def ruff(k, root, petals, color=RED, bone="head", f=0.5):
    """The ruff of petals round the head: each (azimuth from the front in degrees, length, drop, width)."""
    for i, (az, L, drop, w) in enumerate(petals):
        a = math.radians(az)
        d = np.array([math.sin(a), -math.cos(a), 0.0])
        pts, rad = strap(np.array(root) + d * 0.012, d, L, drop, w)
        axis = np.cross(d, (0, 0, 1.0)) + np.array([0, 0, 0.3])     # broad face to the front and the sides
        k.add(f"ruff{i}", outward(blade(pts, rad, axis, f)), color, bone, fuse=False, surface="leaf")


def face(k, c, r, color=FACE, bone="head"):
    k.add("face", ell(c, r), color, bone)


def forehead_point(k, c, r, color, low, slope, on=("face",)):
    """The colour of the crown coming down onto the face to a point between the eyes, at low over the middle."""
    c = np.array(c, float)
    def mask(P):
        x = P[:, 0] - c[0]
        return inside(low + slope * np.abs(x) - (P[:, 2] - c[2]), 0.0012)
    k.paint(mask, color, on=list(on))


def point_patch(k, c, r, d, half, color, eps=0.0018):
    """The crown's point on the forehead as a crisp patch: a triangle pointing down between the eyes."""
    p, nrm = on_ell(c, r, d)
    k.decal(p, unit(0, -1, 0.08), half, color, eps=eps, outline=polygon([(-1.0, 0.9), (1.0, 0.9), (0.0, -1.0)]), rings=12,
            segs=60)


def dark_eye(fill=EYE, glint=GLINT):
    """The line's eye: tall and dark, a small glint low and a little in."""
    def look(u, v):
        col = np.tile(fill, (len(u), 1))
        col[((u + 0.05) / 0.3) ** 2 + ((v + 0.42) / 0.3) ** 2 < 1] = glint
        return col
    return look


def lay_eyes(k, c, r, d, half, look, lid, state, ink=INK, eps=0.0018):
    p, nrm = on_ell(c, r, d)
    k.decal(p, nrm, half, eye_look(state, look, srgb(lid) if isinstance(lid, str) else lid, ink, line=0.17),
            eps=eps, rings=20, segs=56, mirror=True)


def smile(k, c, r, d, half, ground, ink=SMILE, bend=0.6):
    """A small smile: a dark curve on the face's own colour."""
    p, nrm = on_ell(c, r, d)
    G, K = srgb(ground), srgb(ink)
    def look(u, v):
        col = np.tile(G, (len(u), 1))
        col[(np.abs(v - bend * (u * u) + 0.25) < 0.32) & (np.abs(u) < 0.86)] = K
        return col
    k.decal(p, nrm, half, look, eps=0.0018, rings=10, segs=48)


def bloom(k, c, n, petals=5, length=0.054, width=0.04, thick=0.026, spread=0.05, bone="head"):
    """The five puffy petals of the bloom round c in the plane square to n; the gap between the two front ones over
    the face. Returns the frame (front in the plane, across)."""
    n = unit(*n)
    fwd = np.array([0, -1.0, 0]); fwd = fwd - n * (fwd @ n); fwd /= np.linalg.norm(fwd)
    side = np.cross(fwd, n)
    for i in range(petals):
        a = math.radians(180 + 360 * i / petals)        # one straight back, the front pair at +-36 degrees
        d = math.cos(a) * fwd + math.sin(a) * side
        dd = d + n * 0.12                               # their tips lift a little
        k.add(f"bloom{i}", puff_petal(c + d * spread, dd, n, length, width, thick), YELLOW, bone, surface="leaf")
    return fwd, side


def centre_flower(k, c, n, fwd, side, r=0.013, bone="head"):
    """The little cream flower at the bloom's middle: five round petals and a yellow heart, loose."""
    for i in range(5):
        a = math.radians(360 * i / 5)
        d = math.cos(a) * fwd + math.sin(a) * side
        V, F = ell((0, 0, 0), (r * 0.62, r * 0.32, r * 0.95), sub=3)
        V = V @ frame_of(d, n).T + c + d * r * 0.95
        k.add(f"cpetal{i}", outward((V, F)), CENTRE_PETAL, bone, fuse=False, surface="leaf")
    k.add("cheart", outward(ell(c + n * r * 0.2, (r * 0.48, r * 0.48, r * 0.4), sub=3)), CENTRE, bone, fuse=False,
          surface="leaf")


def gossifleur(src, out_path):
    k = Kit(src, coat="leaf")
    # --- the face, small and cream, under the bloom ---------------------------------------------------------------
    face(k, HC, HR)
    # the back of the head, hidden under the bloom and the ruff
    k.add("crown", ell(HC + (0, 0.014, 0.022), (0.034, 0.03, 0.03)), MINT, "head")
    forehead_point(k, HC, HR, MINT, 0.03, 1.0, on=("face", "crown"))
    point_patch(k, HC, HR, (0, -0.9, 0.42), (0.0105, 0.0105), MINT)

    # --- the leaf of the body: two lobes over the chin, little points at its sides, a tip curling down ---------
    k.add("bib", ell((0, -0.012, 0.07), (0.034, 0.028, 0.032)), MINT, "body")
    for s in (1, -1):
        k.add("lobe", ell((0.015 * s, -0.022, 0.084), (0.021, 0.014, 0.015)), MINT, "body")
    k.add("tip", tube([(0.0, -0.014, 0.05), (0.006, -0.02, 0.03), (0.016, -0.026, 0.014), (0.028, -0.03, 0.008)],
                      [0.024, 0.016, 0.0095, 0.0055]), MINT, "body")
    k.add("arm", outward(blade([(0.026, -0.016, 0.066), (0.044, -0.022, 0.056), (0.054, -0.026, 0.05)],
                               [0.0085, 0.006, 0.0035], (0, 1, 0), 0.6)), MINT, "body", mirror=True)
    # the lobes' top edge over the chin: two rounded humps meeting in a notch at the middle
    def chin(P):
        x = np.abs(P[:, 0])
        top = 0.087 + 0.008 * np.sqrt(np.clip(1 - ((x - 0.017) / 0.019) ** 2, 0, 1))
        return inside(P[:, 2] - top, 0.002) * (P[:, 1] < -0.012)
    k.paint(chin, MINT)
    # a shade deeper down the tip
    k.paint(lambda P: inside(P[:, 2] - 0.04, 0.01) * 0.45, MINT_DARK, on=["tip", "bib"])

    # --- the bloom over the head, tipped forward, and its little cream flower --------------------------------
    fwd, side = bloom(k, BLOOM, TILT)
    k.paint(lambda P: inside(np.linalg.norm(P - BLOOM, axis=1) - 0.03, 0.02) * 0.5, YELLOW_DEEP, on=["bloom"])
    centre_flower(k, BLOOM + TILT * 0.024, TILT, fwd, side)

    # --- the ruff of red petals behind and at the sides ------------------------------------------------------
    ruff(k, (0, 0.006, 0.122),
         [(62, 0.105, 0.05, 0.021), (-62, 0.105, 0.05, 0.021),
          (90, 0.118, 0.085, 0.023), (-90, 0.118, 0.085, 0.023),
          (118, 0.112, 0.11, 0.022), (-118, 0.112, 0.11, 0.022),
          (150, 0.1, 0.075, 0.021), (-150, 0.1, 0.075, 0.021)])

    # --- the small smile ---------------------------------------------------------------------------------------
    smile(k, HC, HR, (0, -1, -0.36), (0.0085, 0.0048), FACE)

    k.build(voxel=0.0024, smooth=4, band=0.0035, wband=0.03)

    def eyes(state):
        lay_eyes(k, HC, HR, EYE_D, EYE_HALF, dark_eye(), FACE, state)
    k.eye_states(eyes)
    k.export(out_path)


if __name__ == "__main__":
    gossifleur(*args())
