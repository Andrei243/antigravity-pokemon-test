# Pansear sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/pansear.py -- <the export's Pansear.glb> <out.glb>
#
# Pansage's small monkey (sibling("pansage")) with its own colours and tuft. From the references: red where Pansage is
# green, the ears' linings orange-yellow; on the head a great red mound like a curl of soft ice, bulging over the brow
# with a swirl on its front and a point rising out of its top and curling over to the +X side; the cream of the face
# lower, from the eyes' lower edge down, straight across; the eyes tall whites cut flat across the top by a dark lid,
# large dark pupils to the inner side (a sly look); a small closed cat's mouth (a "w"); red shorts whose edge rises
# into the cream in points at the front; the tail's tip a red flame.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import sibling, ell, tube, srgb, unit, polygon, tri, args

p = sibling("pansage")


def swirl(k, P, top):
    """The soft-ice curl: a mound over the crown bulging forward, a swirl ridge on its front and a point on top."""
    main = P["main"]
    D, DR = p.H + np.array([0, -0.018, 0.056]), np.array([0.108, 0.094, 0.066])
    k.add("mound", ell(D, DR), main, "head")
    T = p.H + np.array([0, 0.0, 0.112])
    k.add("crown", ell(T, (0.074, 0.068, 0.046)), main, "head")
    k.add("point", tube([T + (0, 0.004, 0.02), T + (0.012, 0.006, 0.075), T + (0.045, 0.004, 0.115),
                         T + (0.078, -0.002, 0.118), T + (0.088, -0.006, 0.098)], [0.044, 0.032, 0.02, 0.011, 0.005]),
          main, "head")
    # The swirl: a ridge winding in to a point on the mound's front, a little to the +X side
    S = p.on_ell(D, DR, (0.25, -1, 0.35))
    n = p.unit(*((S - D) / DR ** 2))
    ex = np.cross((0, 0, 1.0), n); ex /= np.linalg.norm(ex)
    ez = np.cross(n, ex)
    pts = []
    for i in range(26):
        t = 2.5 * math.pi * i / 25
        r = 0.052 * (1 - t / (2.9 * math.pi))
        q = S + r * (math.cos(t + 2.2) * ex + math.sin(t + 2.2) * ez)
        pts.append(p.on_ell(D, DR, q - D, lift=0.004))
    k.add("swirl", tube(pts, np.linspace(0.016, 0.012, len(pts))), main, "head")


def flame_tip(k, P, end, plane):
    """The tail's tip: a red flame, round below and drawn to a point curling to the side."""
    E = np.array(end)
    def flame(V):
        V = V.copy()
        t = np.clip(V[:, 2], 0, 1)
        V[:, 0] *= 1 - 0.85 * t ** 1.4; V[:, 1] *= 1 - 0.85 * t ** 1.4
        V[:, 0] -= 0.35 * t ** 2
        return V
    k.add("flametip", ell(E + (0, 0, 0.05), (0.034, 0.024, 0.062), deform=flame), P["main"], "tail")


def sear_top(ax):
    """Pansear: the cream begins at the eyes' lower edge, straight across."""
    return -0.036 + 1.0 * ax ** 2


def zig_waist(th):
    """Red shorts whose edge rises into the cream in points across the front."""
    front = np.clip(np.cos(th), 0, 1)
    return 0.12 + 0.036 * (tri(th / 1.4 + 0.5) + 1) / 2 * front ** 1.5


LID = [(math.cos(t), min(math.sin(t), 0.6)) for t in np.linspace(0, 2 * math.pi, 48, endpoint=False)]


def lidded_eye(u, v):
    """A tall white cut flat across the top by a dark lid, a large dark pupil to the inner side."""
    c = np.tile(srgb(p.INK), (len(u), 1))
    white = (u * u + v * v < 0.775 ** 2) & (v < 0.38)
    c[white] = p.WHITE
    c[white & (((u + 0.2) / 0.5) ** 2 + ((v + 0.12) / 0.62) ** 2 < 1)] = srgb(p.INK)
    return c


def cat_mouth():
    CC, CI = srgb(p.CREAM), srgb(p.INK)
    def look(u, v):
        c = np.tile(CC, (len(u), 1))
        w = -0.25 + 1.2 * np.abs(np.abs(u) - 0.5)
        c[(np.abs(v - w) < 0.26) & (np.abs(u) < 0.92)] = CI
        return c
    return look, None, (0.03, 0.018), (0, -1, -0.38)


PANSEAR = dict(
    p.PANSAGE, main="#E55A5E", lining="#F5B03A",
    ear_c=(0.16, 0.024, 0.35), ear_r=(0.068, 0.022, 0.1), ear_rot=(0, -8, 24),
    tuft=swirl, tip=flame_tip, top=sear_top, waist=zig_waist,
    eye=lidded_eye, eye_half=(0.031, 0.04), eye_dir=(0.42, -0.88, 0.0), lid="#E55A5E",
    eye_outline=polygon(LID), mouth=cat_mouth,
)

if __name__ == "__main__":
    src, out = args()
    p.small(src, out, PANSEAR)
