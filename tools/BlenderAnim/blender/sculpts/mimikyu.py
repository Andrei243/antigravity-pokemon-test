# Mimikyu sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/mimikyu.py -- <the export's Mimikyu.glb> <out.glb>
#
# From the references (the 3D picture's pale yellow cloth): a small ghost under a rag made up to look like Pikachu.
# The rag is one thick mass: a bell of cloth to the ground, its hem in ragged points splayed out, dark claws peeking
# from under it in front; two small dark upright dashes on its front (its own eyes, which the game's five eye states
# are laid on); and on top the disguise's head, round, lolling some 22 degrees toward its right (+X lower). One ear
# stands up from the head's left (-X) and leans in; the other droops out to the right and hangs its tip; both end in
# black. The face is drawn on: two black scribbled ring eyes, a black zigzag mouth, orange scribbled cheeks. A wooden
# stick of a tail zigzags up from behind it on its right, its plane facing front and back.
# The skeleton: body (0, 0, 0.12), head (0, 0, 0.22), tail (0, 0.06, 0.08). About 0.45 tall.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
import mathutils
from sculptkit import Kit, ell, tube, blade, hull, slab, srgb, inside, unit, eye_look, polygon, args

CLOTH, BLACK, CHEEK, STICK = "#E4D9A2", "#2A262A", "#E2873A", "#7A6250"
INK, CLAW = "#1E1A1E", "#2E2A30"

H = np.array([0.006, -0.004, 0.236])        # the disguise's head
HR = np.array([0.09, 0.08, 0.075])
ROLL = 22.0                                 # degrees the head lolls, its +X side down
M = np.array(mathutils.Euler([0, math.radians(ROLL), 0]).to_matrix())
VOXEL = 0.0042


def hp(p):
    """A point of the head's own frame (before its roll) in the model's."""
    return H + M @ np.array(p, float)


def hd(d):
    return M @ np.array(d, float)


def on_head(d):
    d = unit(*d)
    return d / math.sqrt(((d / HR) ** 2).sum())


def body(k):
    def bell(V):
        V = V.copy()
        s = 0.36 + 0.64 * ((1 - np.clip(V[:, 2], -1, 1)) / 2) ** 0.9
        V[:, 0] *= s; V[:, 1] *= s
        V[:, 2] = np.where(V[:, 2] < -0.82, -0.82 + (V[:, 2] + 0.82) * 0.4, V[:, 2])
        return V
    k.add("body", ell((0, 0.008, 0.098), (0.102, 0.096, 0.104), deform=bell), CLOTH, "body")
    k.add("neck", ell((0.003, 0.004, 0.178), (0.05, 0.047, 0.04)), CLOTH, "body")
    # the hem: ragged points splayed out on the ground, longer and shorter by turns
    for i in range(9):
        a = math.radians(i * 40 + 20)
        d = np.array([math.sin(a), -math.cos(a), 0])
        L = 0.122 if i % 2 == 0 else 0.11
        k.add(f"hem{i}", blade([d * 0.07 + (0, 0.008, 0.032), d * 0.098 + (0, 0.008, 0.013), d * L + (0, 0.008, 0.006)],
                               [0.034, 0.022, 0.008], (0, 0, 1), 0.45), CLOTH, "body")
    # dark claws peeking out under the hem in front
    for i, ang in enumerate((32, -38, 8)):
        d = np.array([math.sin(math.radians(ang)), -math.cos(math.radians(ang)), 0])
        side = np.array([-d[1], d[0], 0])
        base = d * 0.085 + (0, 0.008, 0.006)
        k.add(f"claw{i}", hull([base - side * 0.009, base + side * 0.009, base + (0, 0, 0.012),
                                base + d * 0.035 + (0, 0, 0.002), base - d * 0.01]), CLAW, "body", fuse=False,
              surface="shell")


def head(k):
    k.add("head", ell(H, HR, rot=(0, ROLL, 0)), CLOTH, "head")
    # the upright ear (-X): from the head's top up and a little in, its last part black (a loose sleeve)
    up = [(-0.04, 0.006, 0.05), (-0.04, 0.008, 0.1), (-0.032, 0.01, 0.15), (-0.018, 0.012, 0.185),
          (-0.004, 0.014, 0.205)]
    ur = [0.028, 0.024, 0.018, 0.012, 0.007]
    k.add("earup", blade([hp(p) for p in up], ur, hd((0, 1, 0)), 0.55), CLOTH, "head")
    k.add("earuptip", blade([hp(p) for p in up[2:]], [r + 0.003 for r in ur[2:]], hd((0, 1, 0)), 0.6), BLACK,
          "head", fuse=False)
    # the drooping ear (+X): out to the right, its tip hanging
    dr = [(0.045, 0.006, 0.05), (0.09, 0.008, 0.08), (0.135, 0.01, 0.088), (0.178, 0.012, 0.074),
          (0.208, 0.014, 0.045)]
    rr = [0.027, 0.023, 0.019, 0.014, 0.008]
    k.add("eardown", blade([hp(p) for p in dr], rr, hd((0, 1, 0)), 0.55), CLOTH, "head")
    k.add("eardowntip", blade([hp(p) for p in dr[2:]], [rr[2] + 0.003, rr[3] + 0.003, rr[4] + 0.003],
                              hd((0, 1, 0)), 0.6), BLACK, "head", fuse=False)


def tail(k):
    """The wooden stick zigzagging up behind it on its right: one flat piece of the zigzag's outline (mitred
    corners, square-cut ends), its faces to the front and the back."""
    line = np.array([(0.03, 0.045), (0.088, 0.07), (0.122, 0.132), (0.094, 0.146), (0.16, 0.214)])
    w = [0.011, 0.013, 0.014, 0.015, 0.019]
    seg = [(line[i + 1] - line[i]) / np.linalg.norm(line[i + 1] - line[i]) for i in range(len(line) - 1)]
    left = lambda t: np.array([-t[1], t[0]])
    L, Rr = [], []
    for i, p in enumerate(line):
        if i == 0 or i == len(line) - 1:
            m, kk = left(seg[0] if i == 0 else seg[-1]), 1.0
        else:
            m = left(seg[i - 1]) + left(seg[i]); m /= np.linalg.norm(m)
            kk = min(1 / max(np.dot(m, left(seg[i])), 1e-3), 2.2)
        L.append(p + m * w[i] * kk); Rr.append(p - m * w[i] * kk)
    outline = [tuple(p) for p in L + Rr[::-1]]
    k.add("stick", slab(outline, (0, 0.1, 0), (1, 0, 0), (0, 0, 1), 0.026), STICK, "tail", fuse=False)


def face(k):
    """The face drawn on the head: a black zigzag mouth (paint in the head's frame), orange scribbled cheeks."""
    zig = np.array([(-0.05, -0.026), (-0.036, -0.03), (-0.024, -0.05), (-0.008, -0.032), (0.008, -0.052),
                    (0.024, -0.034), (0.038, -0.052)])
    def mouth(P):
        Q = (P - H) @ M                     # into the head's frame
        x, z = Q[:, 0], Q[:, 2]
        best = np.full(len(P), 9.0)
        for a, b in zip(zig, zig[1:]):
            ab = b - a
            t = np.clip(((x - a[0]) * ab[0] + (z - a[1]) * ab[1]) / (ab @ ab), 0, 1)
            best = np.minimum(best, np.hypot(x - a[0] - t * ab[0], z - a[1] - t * ab[1]))
        return inside(best - 0.0048, 0.0012) * inside(Q[:, 1] + 0.03, 0.002)
    k.paint(mouth, INK, on=["head"])
    rag = lambda th: 1 + 0.12 * math.sin(5 * th) + 0.06 * math.sin(9 * th + 1)
    for x, z in ((-0.068, -0.02), (0.06, -0.036)):
        d = hd(unit(x / HR[0], -0.8, z / HR[2]))
        c = H + d / math.sqrt(((M.T @ d / HR) ** 2).sum())
        k.decal(c, d, (0.019, 0.017), CHEEK, eps=0.002, outline=rag, rings=10, segs=48, up=hd((0, 0, 1)))
    # the ring eyes, scribbled: a black ring round a black middle, in every state (the disguise doesn't blink)
    CC, CI = srgb(CLOTH), srgb(INK)
    def ring(u, v):
        r = np.hypot(u, v)
        c = np.tile(CC, (len(u), 1))
        c[(r > 0.72) | (r < 0.38)] = CI
        c[(np.abs(np.arctan2(v, u) - 2.2) < 0.45) & (r < 0.72)] = CI          # the scribble's stroke in
        return c
    for x in (-0.03, 0.034):
        d = hd(unit(x / HR[0], -0.9, 0.16))
        c = H + d / math.sqrt(((M.T @ d / HR) ** 2).sum())
        k.decal(c, d, (0.025, 0.025), ring, eps=0.002, rings=16, segs=56, up=hd((0, 0, 1)))


def eyes(k):
    """Its own eyes: two small dark upright dashes on the cloth's front."""
    CC, CI = srgb(CLOTH), srgb(INK)
    def dash(u, v):
        return np.tile(CI, (len(u), 1))
    de = unit(0.12, -1, 0.1)
    def lay(state):
        k.decal((0.017, -0.06, 0.082), de, (0.0068, 0.014), eye_look(state, dash, CC, CI, line=0.2), eps=0.002,
                rings=20, segs=56, mirror=True)
    k.eye_states(lay)


def mimikyu(src, out):
    k = Kit(src, coat="fur")
    body(k)
    head(k)
    tail(k)
    face(k)
    k.build(voxel=VOXEL, band=0.005, wband=0.04)
    eyes(k)
    k.export(out)


if __name__ == "__main__":
    mimikyu(*args())
