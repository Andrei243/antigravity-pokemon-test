# Nincada sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/nincada.py -- <the export's Nincada.glb> <out.glb>
#
# The design, from the references: a low pale grey-white cicada nymph. A round white head in front with a grey zigzag
# line over its brow, a flat ringed eye on each side (black rim, a lime ring, a black pupil), two long white tusks
# hanging from under its face to the ground; a larger white abdomen behind, crossed by grey segment lines; a pair of
# small pale green leaf wings raised in a V on its back; two big grey-brown paddle claws on short white arms at the
# head's sides, standing on the ground; thin white legs, a short pair under the middle and a long pair running back.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, ring_band, srgb, inside, unit, tri, eye_look, args


WHITE, LINE, CLAW, WINGC, VEIN = "#ECEDE8", "#8E8E88", "#9A8E82", "#D6EAAE", "#9CC46A"
INK, LIME = "#141414", "#B8E03C"

H = np.array([0.0, -0.1, 0.118])            # the head's centre
HR = np.array([0.074, 0.07, 0.072])
AB = np.array([0.0, 0.04, 0.122])          # the abdomen's centre
ABR = np.array([0.095, 0.115, 0.095])


def abdomen_radius(y):
    """The abdomen's radius round its long axis at y (it is near enough round across)."""
    t = np.clip((y - AB[1]) / ABR[1], -1, 1)
    return 0.095 * np.sqrt(1 - t * t)


def wing(k, s):
    """A small pale green leaf on the abdomen's top, raised out and back in a V, its face up and out."""
    side = "R" if s > 0 else "L"
    root = np.array([0.036 * s, 0.025, 0.2])
    d = unit(0.5 * s, 0.8, 0.26)
    nd = unit(0.75 * s, 0.15, 0.65)
    ez = np.cross(nd, d); ez /= np.linalg.norm(ez)
    L, W = 0.088, 0.026
    t = np.linspace(0, 1, 14)
    a = t * L
    w = W * np.sin(np.pi * np.clip(t, 0, 1)) ** 0.8 * (0.6 + 0.4 * t)
    w = np.maximum(w, 0.0015)
    out = np.vstack([np.stack([a, w], 1), np.stack([a, -w], 1)[::-1]])
    k.add(f"wing{side}", slab(out, root, d, ez, 0.006), WINGC, "body", fuse=False, surface="leaf")
    # a vein down its middle, thicker so it shows on both faces
    vein = [(0.004, 0.0035), (0.072, 0.002), (0.072, -0.002), (0.004, -0.0035)]
    k.add(f"vein{side}", slab(vein, root, d, ez, 0.009), VEIN, "body", fuse=False, surface="leaf")


def nincada(src, out):
    k = Kit(src, coat="shell")

    # --- the body: the head and the abdomen, fused, low over the ground -------------------------------------------
    def taper(V):
        V = V.copy()
        s = 1 - 0.12 * np.clip(V[:, 1], 0, 1)            # narrower toward its tail
        V[:, 0] *= s; V[:, 2] *= s
        return V
    k.add("abdomen", ell(AB, ABR, deform=taper), WHITE, "body")
    k.add("neck", ell((0, -0.05, 0.115), (0.062, 0.04, 0.06)), WHITE, "body")
    k.add("head", ell(H, HR), WHITE, "head")
    # a little flattened face in front, so the eyes and the brow lie on a broad front
    k.add("face", ell((0, -0.142, 0.108), (0.06, 0.03, 0.055)), WHITE, "head")

    # the grey zigzag over the brow: a line round the head, a W in its middle
    def zigzag(P):
        q = P - H
        th = np.arctan2(q[:, 0], -q[:, 1])                 # 0 straight ahead
        base = 0.03
        z = base + np.where(np.abs(th) < 0.42, 0.016 * (tri(th / 0.42 + 0.5) - 1) * 0.5 + 0.008, 0.008)
        return inside(np.abs(q[:, 2] - z) - 0.0028, 0.0012) * inside(np.abs(th) - 1.05, 0.05) * (q[:, 1] < 0.01) \
            * inside(np.linalg.norm(q / (HR + 0.03), axis=1) - 1.0, 0.01)
    k.paint(zigzag, LINE, on=["head", "face"])

    # the abdomen's segment lines: painted rings across it, bowed a little back over the top (loose bands stood off
    # the remeshed body and made it read as a can from behind)
    def segments(P):
        q = P - AB
        y = q[:, 1] - 0.012 * np.clip(q[:, 2] / ABR[2], -1, 1)
        m = np.zeros(len(P))
        for y0 in (-0.05, -0.01, 0.03, 0.07):
            m = np.maximum(m, inside(np.abs(y - y0) - 0.0024, 0.0012))
        return m * (q[:, 1] > -0.09)
    k.paint(segments, LINE, on=["abdomen"])

    # --- the wings ------------------------------------------------------------------------------------------------
    for s in (1, -1):
        wing(k, s)

    # --- the tusks: two long white fangs hanging from under the face to the ground --------------------------------
    k.add("tusk", tube([(0.02, -0.15, 0.08), (0.022, -0.168, 0.05), (0.024, -0.176, 0.022), (0.025, -0.178, 0.006)],
                       [0.0085, 0.0078, 0.0068, 0.0055]), WHITE, "head", fuse=False, mirror=True)

    # --- the arms and their claws: big grey-brown paddles standing on the ground at the head's sides -------------
    k.add("arm", tube([(0.05, -0.12, 0.1), (0.085, -0.138, 0.098), (0.108, -0.15, 0.09)], [0.011, 0.01, 0.01]),
          WHITE, "arm{s}", fuse=False, mirror=True)
    def paddle(V):
        V = V.copy()
        s = 1 + 0.18 * np.clip(-V[:, 2], 0, 1)          # broader low down
        V[:, 1] *= s
        return V
    k.add("claw", ell((0.118, -0.156, 0.054), (0.017, 0.033, 0.054), rot=(0, -10, -38), deform=paddle), CLAW,
          "arm{s}", fuse=False, mirror=True)

    # --- the legs: a short pair under the middle, a long thin pair running back ----------------------------------
    k.add("leg", tube([(0.05, -0.03, 0.06), (0.1, -0.035, 0.062), (0.125, -0.03, 0.03), (0.132, -0.026, 0.004)],
                      [0.0085, 0.0078, 0.007, 0.006]), WHITE, "leg{s}", fuse=False, mirror=True)
    k.add("hind", tube([(0.055, 0.1, 0.075), (0.085, 0.16, 0.05), (0.105, 0.215, 0.022), (0.11, 0.24, 0.005)],
                       [0.0095, 0.0085, 0.0075, 0.0065]), WHITE, "hind{s}", fuse=False, mirror=True)

    k.build(voxel=0.0034, smooth=4, band=0.006)

    # --- the eyes: flat ringed discs on the head's sides, turned some 40 degrees off straight ahead ---------------
    CK, CL, CW = srgb(INK), srgb(LIME), srgb(WHITE)
    def eye(u, v):
        r = np.sqrt(u * u + v * v)
        c = np.tile(CK, (len(u), 1))
        ring = (r > 0.44) & (r < 0.74)
        c[ring] = CL
        return c
    de = unit(0.66, -0.74, 0.08)
    p = H + de / math.sqrt(((de / HR) ** 2).sum())
    n = (p - H) / HR ** 2; n /= np.linalg.norm(n)

    def eyes(state):
        k.decal(p, n, (0.023, 0.025), eye_look(state, eye, CW, CK, line=0.16), eps=0.003, rings=20, segs=56,
                mirror=True)

    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    nincada(*args())
