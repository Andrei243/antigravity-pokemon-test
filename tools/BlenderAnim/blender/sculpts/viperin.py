# Viperin sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/viperin.py -- <the export's Viperin.glb> <out.glb>
# Carpathia No. 10070, the viper (CP-dex-2-mountains.md, 70). From its picture: a short, thick, dark grey viper coiled
# on the ground in two heavy loops, the inner one lying on the outer; its neck rises out of the coil in an S, showing a
# pale belly of broad banded scales, to a broad arrow-shaped head carried level and forward. A bold zigzag down the
# whole back, a black jagged line round a darker grey band; a dark stripe from each eye back along the side of the
# head; grey eyes with a black slit under a heavy brow; a small soft brownish bump on the tip of the snout; a pale lip
# under the long mouth line; the thin tail rising behind at the right.
#
# The body is laid as ekans.py's (its Line, belly and seams, imported): one smooth line through the chain of joints, a
# tube a segment on its own bone, the paints by arc length and the angle round the line. Written to be grown from
# (cornviper.py imports it with sibling("viperin")): the zigzag, the head and the eyes are functions taking their sizes.
# The export's chain: body (0.14, 0.3, 0.13) where the tail ends, seg1 (0.135, -0.147, 0.065) round the outer coil to
# seg6, seg7 to seg10 the inner coil, seg11 (0, -0.08, 0.3) up the neck, head (0, -0.1, 0.37).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, eye_look, tri, args, sibling

ek = sibling("ekans")

# --- the line's palette ---------------------------------------------------------------------------------------------
GREY = "#58565A"           # the coat
ZIG = "#3A383E"            # the zigzag's band
BLACK = "#16141A"          # its jagged edge, the eye stripe
BELLY = "#B8B4B2"          # the pale belly and lip
SCUTE = "#7C7880"          # the seams between the belly's scales
NOSE = "#7E6662"           # the soft bump on the snout
EYE = srgb("#E8E6D6")
INK = srgb("#121014")


# --- the zigzag down the back ---------------------------------------------------------------------------------------
def zigzag(line, s0, s1, every, width, amp, edge, below=None):
    """Two masks along the line between its parameters s0 and s1: the band down the back (within an angle of the top
    that zigzags with arc length, a point every `every`) and its black jagged rim, `edge` radians wide."""
    def angle(Q):
        j, s, d = line.place(Q)
        a = np.arccos(np.clip(-d, -1, 1))                  # 0 on the top of the body, pi on the belly
        w = width + amp * tri(s / every)
        ok = (line.S[j] > s0) * (line.S[j] < s1)
        if below is not None:
            ok = ok * (Q[:, 2] < below)
        return a, w, ok
    def band(Q):
        a, w, ok = angle(Q)
        return inside(a - w, 0.008) * ok
    def rim(Q):
        a, w, ok = angle(Q)
        return inside(np.abs(a - w) - edge, 0.008) * ok
    return band, rim


def front_only(line, mask, s0, s1):
    """A mask kept off the back of the body between the line's parameters s0 and s1 (where the neck leaves the coil
    the belly's way turns from down to forward, and a pale band showed behind)."""
    def m(Q):
        j = line.near(Q)
        behind = (Q[:, 1] > line.C[j, 1]) & (line.S[j] > s0) & (line.S[j] < s1)
        return mask(Q) * ~behind
    return m


def scutes(line, s0, s1, every, width=0.5):
    """The belly's broad scales: seams across the pale belly only."""
    b = ek.belly(line, s0, s1, width=width)
    sm = ek.seams(line, s0, s1, every, half=0.0042)
    return lambda Q: b(Q) * sm(Q)


# --- the head: a broad arrow, wide at the jaws and narrowing to a blunt snout -----------------------------------------
def head(k, hc, hs, grey, belly, nose, bone="head"):
    """The skull, the jaws behind it, the brows over the eyes and the bump on the snout's tip; returns the head's
    frame (a function from a point in the head's own units to the model's)."""
    def hp(p):
        return hc + np.array(p, float) * hs
    def arrow(V):
        V = V.copy()
        t = np.clip(-V[:, 1], 0, 1)
        V[:, 0] *= 1 - 0.42 * t
        V[:, 2] *= 1 - 0.2 * t
        return V
    k.add("skull", ell(hp((0, -0.01, 0)), np.array((0.08, 0.105, 0.04)) * hs, deform=arrow), grey, bone)
    k.add("jaws", ell(hp((0, 0.045, -0.006)), np.array((0.088, 0.06, 0.044)) * hs), grey, bone)
    k.add("chin", ell(hp((0, -0.02, -0.03)), np.array((0.062, 0.085, 0.022)) * hs, deform=arrow), grey, bone)
    for s, side in ((1, "R"), (-1, "L")):
        k.add(f"brow{side}", ell(hp((0.038 * s, -0.035, 0.03)), np.array((0.022, 0.03, 0.008)) * hs, rot=(0, 0, -16 * s)),
              grey, bone)
    k.add("nose", ell(hp((0, -0.108, 0.006)), np.array((0.013, 0.011, 0.011)) * hs), nose, bone)
    # the pale lip under the mouth line, and the dark stripe from each eye back along the side of the head
    lip = hp((0, 0, -0.014))[2]
    def under(P):
        return inside(P[:, 2] - lip, 0.0015) * inside(np.abs(P[:, 1] - hp((0, 0.0, 0))[1]) - 0.12 * hs, 0.004) \
            * inside(np.linalg.norm(P - hc, axis=1) - 0.15 * hs, 0.004)
    k.paint(under, belly, on=["skull", "jaws", "chin", "nose"])
    def stripe(P):
        q = (P - hc) / hs
        zl = 0.004 - 0.12 * (q[:, 1] + 0.03)                       # sloping down toward the back
        return inside(np.abs(q[:, 2] - zl) - 0.0085, 0.0015) * (np.abs(q[:, 0]) > 0.045) * (q[:, 1] > -0.02) \
            * (q[:, 1] < 0.085)
    k.paint(stripe, BLACK, on=["skull", "jaws", "brow"])
    return hp


# --- the eyes: grey with a black slit, cut along the top by the brow ---------------------------------------------------
def brow_cut(top=0.5, slope=-0.2):
    """The eye's outline: a circle cut along v = top + slope u (u outward, toward the back)."""
    def cut(th):
        s = math.sin(th) - slope * math.cos(th)
        return min(1.0, top / s) if s > 1e-6 else 1.0
    return cut


def eye_colour(cut, iris=EYE, ink=INK, rim=0.84):
    def eye(u, v):
        reach = np.array([max(cut(t), 1e-3) for t in np.arctan2(v, u)])
        rho = np.sqrt(u * u + v * v) / reach
        c = np.tile(iris, (len(u), 1))
        c[rho > rim] = ink
        c[((u + 0.34) / 0.14) ** 2 + ((v - 0.06) / 0.16) ** 2 < 1] = np.array([1.0, 1.0, 1.0])
        return c
    return eye


def lay_eyes(k, E, de, half, lid, ink=INK, iris=EYE, slit=0.2, cut=None, fierce_slope=0.5):
    """lay(state) for eye_states: the eye each side cut by the brow, its slit a patch of its own in the open states."""
    cut = cut or brow_cut()
    look = eye_colour(cut, iris, ink)
    lid = srgb(lid) if isinstance(lid, str) else lid
    def fierce(u, v):
        c = look(u, v)
        edge = 0.05 - fierce_slope * u
        c[v > edge] = lid
        c[np.abs(v - edge) < 0.16] = ink
        return c
    def lay(state):
        fn = fierce if state == "Fierce" else (look if state == "Open" else eye_look(state, look, lid, ink, line=0.18))
        m = np.array([-1, 1, 1.0])
        k.decal(E, de, half, fn, eps=0.004 * half[0] / 0.03, rings=20, segs=56, outline=cut)
        k.decal(E * m, de * m, half, lambda u, v: fn(-u, v), eps=0.004 * half[0] / 0.03, rings=20, segs=56,
                outline=lambda th: cut(math.pi - th))
        if state in ("Open", "Fierce"):
            sh = 0.62 if state == "Open" else 0.38
            sy = -0.08 if state == "Open" else -0.3
            right = np.cross((0, 0, 1.0), de); right /= np.linalg.norm(right)
            upv = np.cross(de, right)
            c = E + upv * sy * half[1] - right * 0.04 * half[0]
            k.decal(c, de, (slit * half[0], sh * half[1]), ink, eps=0.007 * half[0] / 0.03, rings=6, segs=32,
                    mirror=True)
    return lay


# --- Viperin ----------------------------------------------------------------------------------------------------------
LINE = [(0.205, 0.37, 0.29), (0.2, 0.355, 0.215), (0.17, 0.32, 0.15), (0.16, 0.2, 0.085), (0.135, -0.147, 0.068),
        (-0.031, -0.19, 0.07), (-0.162, -0.088, 0.074), (-0.16, 0.07, 0.078), (-0.04, 0.162, 0.082),
        (0.095, 0.122, 0.09), (0.142, 0.0, 0.13), (0.081, -0.1, 0.158), (-0.028, -0.112, 0.176),
        (-0.096, -0.044, 0.19), (-0.08, -0.03, 0.265), (-0.02, -0.04, 0.34), (0.0, -0.08, 0.395),
        (0.0, -0.115, 0.418)]
RADS = [0.016, 0.026, 0.038, 0.058, 0.068, 0.07, 0.07, 0.07, 0.07, 0.069, 0.068, 0.067, 0.066, 0.064, 0.06,
        0.056, 0.052, 0.05]
BONES = ["body", "body", "body", "body", "seg1", "seg2", "seg3", "seg4", "seg5", "seg6", "seg7", "seg8", "seg9",
         "seg10", "seg10", "seg11", "seg11"]
HC = np.array([0.0, -0.19, 0.44])           # the head's middle
HS = 1.32


def viperin(src, out):
    k = Kit(src, coat="scales")
    line = ek.Line(LINE, RADS, rise=(0.17, 0.24))
    line.tubes(k, BONES, GREY)
    k.add("nape", tube([LINE[-2], LINE[-1], HC + (0, 0.05, -0.005)], [0.052, 0.05, 0.05]), GREY, "head")
    hp = head(k, HC, HS, GREY, BELLY, NOSE)

    # the pale belly, banded by broad scales, and the zigzag down the back
    k.paint(front_only(line, ek.belly(line, 3.0, 16.5, width=0.5, edge=0.004), 11.5, 15.0), BELLY, on=["seg"])
    k.paint(front_only(line, scutes(line, 3.0, 16.5, 0.036), 11.5, 15.0), SCUTE, on=["seg"])
    band, rim = zigzag(line, 0.15, 16.6, 0.085, 0.78, 0.36, 0.17, below=0.385)
    k.paint(band, ZIG, on=["seg", "nape"])
    k.paint(rim, BLACK, on=["seg", "nape"])
    # the zigzag's head: a dark wedge between the brows back over the crown
    def crown(P):
        q = (P - HC) / HS
        return inside(np.abs(q[:, 0]) - 0.012 - 0.22 * np.clip(q[:, 1] + 0.02, 0, 1), 0.0015) * (q[:, 1] > -0.02) \
            * (q[:, 2] > 0.02)
    k.paint(crown, BLACK, on=["skull", "jaws", "nape"])

    # the eyes, under the brows, looking out and forward
    E = hp((0.05, -0.04, 0.012))
    de = unit(0.78, -0.48, 0.38)
    lay = lay_eyes(k, E, de, (0.026 * HS, 0.026 * HS), GREY)

    k.build(voxel=0.0052, smooth=4, band=0.006, wband=0.05)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    viperin(*args())
