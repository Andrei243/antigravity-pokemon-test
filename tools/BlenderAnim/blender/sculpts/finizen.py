# Finizen sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/finizen.py -- <the export's Finizen.glb> <out.glb>
#
# From the references: a small plump dolphin, teal blue over the back and a pale grey-white from the lower jaw and the
# throat along the whole underside to the tail. A short round rostrum, the mouth open under it, pink inside. A big round
# eye on each side of the head in a white patch that runs down into the pale throat: a dark rim, the iris blue above and
# pink below, a dark pupil and a white glint. A white wavy line with small loops along each flank from behind the eye to
# the tail. A dorsal fin, teal with a white trailing tip; long teal pectoral fins swept back and down; a small pale fin
# under the belly; a light blue ring round the tail stock; a fluke of two lobes. The art's leap is not the rest pose: the
# body is held nearly level, the head a little down.
#
# Written to be grown from (Palafin's Zero Form: sibling("finizen").dolphin(src, out, 1.1, heart_mark=True)): every place
# is a constant of Finizen's frame times the scale.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, lathe, srgb, inside, unit, eye_look, polygon, args

TEAL, PALE, WHITE, RING, INK = "#3FB2D6", "#E2EAF0", "#F6F8FA", "#86CCEE", "#1C2230"
IRIS, IRIS_LOW, PUPIL, MOUTH, MOUTH_IN, HEART = "#3C78D8", "#F08A9C", "#3A1E28", "#E8889A", "#7C2E40", "#DC2C6C"

# Finizen's frame (scale 1): the head, the torso, the tail's line and the eye
HEAD_C, HEAD_R = np.array([0.0, -0.10, 0.15]), np.array([0.085, 0.092, 0.088])
TORSO_C, TORSO_R = np.array([0.0, -0.005, 0.158]), np.array([0.09, 0.12, 0.094])
TAIL_PTS = [(0.0, 0.06, 0.162), (0.0, 0.13, 0.164), (0.0, 0.205, 0.172)]
TAIL_R = [0.078, 0.046, 0.024]
EYE_D = unit(0.7, -0.62, 0.26)


def at_surface(C, R, d):
    """The point of an ellipsoid (centre C, radii R) in a direction d from its centre."""
    d = np.array(d, float)
    return np.array(C, float) + d / np.linalg.norm(d / np.array(R, float))


def flat(c, d, half, up=(0, 0, 1.0), roll=0.0, deform=None):
    """A flattened ellipsoid lying along d: half sizes (along, across, thick), its face toward up, rolled about d."""
    d = unit(*d)
    s = np.cross(d, up); s /= np.linalg.norm(s)
    n = np.cross(s, d)
    r = math.radians(roll)
    s, n = s * math.cos(r) + n * math.sin(r), n * math.cos(r) - s * math.sin(r)
    V, F = ell((0, 0, 0), (half[1], half[0], half[2]), deform=deform)
    return V @ np.stack([s, d, n]) + np.array(c, float), F


def torus(c, axis, R, r, seg=48, sides=16):
    """A ring: a lathe of a small circle at distance R from the axis, closed round on itself."""
    prof = [(R + r * math.cos(t), r * math.sin(t)) for t in np.linspace(0, 2 * math.pi, sides + 1)]
    return lathe(c, axis, prof, seg=seg)


def spine_z(y, k):
    """The body's middle height along its length (level)."""
    return np.full_like(np.asarray(y, float), 0.158 * k)


def spine_r(y, k):
    """The body's half height along its length."""
    return np.interp(y / k, [-0.2, -0.1, 0.0, 0.1, 0.2], [0.04, 0.083, 0.09, 0.062, 0.026]) * k


def eye_open(u, v):
    """A round eye: a dark rim, the iris blue above and pink below, a dark pupil, a white glint (u outward, v up)."""
    r = np.sqrt(u * u + v * v)
    c = np.tile(srgb(IRIS), (len(u), 1))
    c[v < -0.12] = srgb(IRIS_LOW)
    c[((u + 0.04) / 0.36) ** 2 + ((v + 0.02) / 0.4) ** 2 < 1] = srgb(PUPIL)
    c[((u + 0.22) / 0.2) ** 2 + ((v - 0.34) / 0.2) ** 2 < 1] = (1.0, 1.0, 1.0)
    c[r > 0.84] = srgb(INK)
    return c


def eyes(k, spots, half, state):
    look = eye_look(state, eye_open, srgb(WHITE), srgb(INK), line=0.17)
    for at, d, sx in spots:
        col = look if sx > 0 else (lambda u, v, look=look: look(-u, v))
        k.decal(at, d, half, col, eps=0.0016, rings=20, segs=56)


def heart():
    """A heart's outline in u, v about its middle, for polygon()."""
    th = np.linspace(0, 2 * math.pi, 64, endpoint=False)
    x = 16 * np.sin(th) ** 3
    y = 13 * np.cos(th) - 5 * np.cos(2 * th) - 2 * np.cos(3 * th) - np.cos(4 * th)
    pts = np.stack([x / 17.0, (y + 2.0) / 17.0], axis=1)
    return pts


def dolphin(src, out, scale=1.0, heart_mark=False):
    k = scale
    K = Kit(src, coat="scales")
    S = lambda v: np.array(v, float) * k
    HC, HR, TC, TR = HEAD_C * k, HEAD_R * k, TORSO_C * k, TORSO_R * k
    # The body: head and torso fused into one plump spindle, the tail stock tapering behind
    K.add("head", ell(HC, HR, rot=(6, 0, 0)), TEAL, "body")
    K.add("torso", ell(TC, TR), TEAL, "body")
    K.add("torsofill", ell(S((0, -0.055, 0.155)), S((0.088, 0.075, 0.091))), TEAL, "body")   # fills the crease
    K.add("rear", tube([S(p) for p in TAIL_PTS[:2]], [r * k for r in TAIL_R[:2]], seg=32), TEAL, "body")
    K.add("stock", tube([S(p) for p in TAIL_PTS[1:]], [r * k for r in TAIL_R[1:]], seg=32), TEAL, "tail")
    # The rostrum, short and round, its front a little lower; the lower jaw pale under it, the mouth open between
    K.add("snout", ell(S((0, -0.188, 0.122)), S((0.036, 0.048, 0.028)), rot=(14, 0, 0)), TEAL, "body")
    K.add("jaw", ell(S((0, -0.168, 0.094)), S((0.032, 0.04, 0.017)), rot=(-12, 0, 0)), PALE, "body")
    # Pectoral fins: long, swept back, out and down, their faces turned toward the front and up
    for sx in (1, -1):
        root = S((0.06 * sx, -0.06, 0.098))
        d = unit(0.6 * sx, 0.72, -0.42)
        c = root + d * 0.058 * k
        def taper(V):
            V = V.copy(); V[:, 0] *= 1.0 - 0.45 * np.clip(V[:, 1], 0, 1); return V
        K.add(f"fin{'RL'[sx < 0]}", flat(c, d, (0.066 * k, 0.03 * k, 0.0095 * k), up=(0, -0.5, 1.0),
                                        roll=-25 * sx, deform=taper), TEAL, f"fin{'RL'[sx < 0]}")
    # A small pale fin under the belly, pointing down and back
    K.add("keel", flat(S((0, 0.04, 0.072)), unit(0, 0.55, -0.83), (0.026 * k, 0.016 * k, 0.008 * k), up=(1, 0, 0)),
          PALE, "body")
    # The dorsal fin, low and swept back, bent 40 degrees to the model's +X side so its face shows from behind
    lean = math.radians(40)
    base = S((0, -0.035, 0.235))
    fin = []
    for h, yy in ((0.0, 0.0), (0.035, 0.04), (0.058, 0.082), (0.064, 0.1)):
        fin.append(base + np.array([h * math.sin(lean), yy, h * math.cos(lean)]) * k)
    K.add("dorsal", blade(fin, [0.034 * k, 0.019 * k, 0.008 * k, 0.005 * k], (math.cos(lean), 0, -math.sin(lean)), 0.3),
          TEAL, "body")
    K.paint(lambda P: inside(base[1] + 0.06 * k - P[:, 1], 0.003), WHITE, on=["dorsal"])
    # The fluke: two lobes back, out and rising, so the back camera sees their faces
    tip = S(TAIL_PTS[-1])
    for sx in (1, -1):
        d = unit(0.7 * sx, 0.55, 0.62)
        def lobe(V):
            V = V.copy(); V[:, 0] *= 1.0 - 0.5 * np.clip(V[:, 1], 0, 1) ** 1.5; return V
        K.add(f"fluke{'RL'[sx < 0]}", flat(tip + d * 0.045 * k, d, (0.066 * k, 0.034 * k, 0.0105 * k), up=(0, -0.9, 0.5),
                                          deform=lobe), TEAL, "tail")
    # The pale underside: from the jaw and the throat back along the lower flanks to the tail
    def belly(P):
        y = P[:, 1]
        edge = spine_z(y, k) - spine_r(y, k) * (0.22 - 0.25 * np.clip((-y / k - 0.06) / 0.08, 0, 1))
        return inside(P[:, 2] - edge, 0.003 * k)
    K.paint(belly, PALE, on=["head", "torso", "rear", "stock", "jaw", "snout"])
    # The white patch round each eye, running down into the pale throat
    E = [(at_surface(HC, HR, EYE_D * (sx, 1, 1)), EYE_D * (sx, 1, 1), sx) for sx in (1, -1)]
    for at, d, sx in E:
        c = at - d * 0.006 * k + S((0, 0.004, -0.012))
        K.paint(lambda P, c=c: inside(np.linalg.norm((P - c) / (0.04 * k, 0.04 * k, 0.046 * k), axis=1) - 1, 0.05),
                WHITE, on=["head", "jaw"])
    # The wavy white line along each flank with small loops, painted in the body's own frame
    def zline(y):
        """The line's height (scale 1) along the flank: rising over the shoulder, waving and falling to the tail."""
        return (0.193 + 0.02 * np.sin(np.pi * np.clip((y + 0.05) / 0.2, 0, 1)) - 0.06 * np.clip((y - 0.08) / 0.12, 0, 1)
                + 0.008 * np.sin((y - 0.03) * 2 * np.pi / 0.05) * (y > 0.03))
    def wave(P):
        y = P[:, 1] / k
        z = P[:, 2] / k
        m = inside(np.abs(z - zline(y)) - 0.0042, 0.0018) * (y > -0.05) * (y < 0.18)
        for y0 in (0.035, 0.125):
            ring = np.abs(np.hypot(y - y0, z - (zline(np.array(y0)) + 0.011)) - 0.011) - 0.0038
            m = np.maximum(m, inside(ring, 0.0018))
        return m * (np.abs(P[:, 0]) > 0.03 * k)
    K.paint(wave, WHITE, on=["torso", "rear", "stock", "head"])
    # The ring round the tail stock
    rc = S((0, 0.178, 0.168))
    K.add("ring", torus(rc, unit(0, 1, 0.1), 0.037 * k, 0.0095 * k), RING, "tail", fuse=False, surface="shell")
    # The open mouth under the rostrum: one patch, dark rim, pink inside
    def mouth(u, v):
        c = np.tile(srgb(MOUTH), (len(u), 1))
        c[(u / 0.6) ** 2 + ((v - 0.25) / 0.5) ** 2 < 1] = srgb(MOUTH_IN)
        c[u * u + v * v > 0.8] = srgb("#4A2030")
        return c
    md = unit(0, -0.62, -0.78)
    K.decal(S((0, -0.19, 0.104)), md, (0.032 * k, 0.022 * k), mouth, eps=0.0012, rings=12, segs=48)
    if heart_mark:
        K.decal(S((0, -0.105, 0.07)), unit(0, -0.71, -0.71), (0.03 * k, 0.03 * k), HEART, up=(0, -1, 0.3),
                eps=0.0014, rings=12, segs=96, outline=polygon(heart()))
    K.build(voxel=0.003 * k, smooth=4, band=0.005 * k, wband=0.03 * k)
    K.eye_states(lambda st: eyes(K, E, (0.022 * k, 0.024 * k), st))
    K.export(out)


if __name__ == "__main__":
    dolphin(*args())
