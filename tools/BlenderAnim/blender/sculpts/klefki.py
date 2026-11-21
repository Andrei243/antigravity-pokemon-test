# Klefki sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/klefki.py -- <the export's Klefki.glb> <out.glb>
#
# The design, from the references: a silver key ring standing upright, facing front, with a small silver head hanging
# inside it from the top: a bulb wide below and narrowing above into a stalk that runs up through the ring and ends
# above it in a key's notched head with a round knob. On the bulb's face two round black eyes, a black keyhole between
# and below them for a mouth, a pink drop mark on the brow, and a pink drop hanging under the chin like a tongue. Four
# keys hang on the ring: a gold key on its own small gold loop at the left pointing out and down, a black-bowed key
# with a grey blade at the top right pointing up and out, a black key with a round bow at the bottom left, and an
# ornate copper key hanging at the bottom right. The ring and the keys are thick loose pieces (lathes and slabs),
# never thinner than the floor; the head is fused for its face's patches.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, unit, eye_look, args, hull, slab, lathe, polygon, smoothstep

SILVER, SILVER_DK = "#DADDE8", "#A9AEBE"
PINK, PINK_LT = "#EE8A9E", "#F8D2DA"
INK = "#1E1D24"
GOLD, BLACK, STEEL, COPPER = "#D8B256", "#2E2C33", "#B4B8C2", "#B8674A"

RING_C, RING_R, WIRE = np.array([0.0, 0.0, 0.2]), 0.105, 0.0085
HEAD_C, HEAD_R = np.array([0.0, -0.004, 0.24]), np.array([0.052, 0.044, 0.056])
FRONT = (1, 0, 0), (0, 0, 1)              # the plane the ring and the keys lie in: x across, z up


def torus(c, axis, R, r, seg=48, rs=16):
    """A ring of round section: R from the axis to the wire's middle, r the wire's radius."""
    prof = [(R + r * math.cos(t), r * math.sin(t)) for t in np.linspace(0, 2 * math.pi, rs + 1)]
    return lathe(c, axis, prof, seg=seg)


def on_ring(deg):
    """The ring's wire at an angle (0 to the right, 90 at the top), and the outward direction there."""
    a = math.radians(deg)
    d = np.array([math.cos(a), 0.0, math.sin(a)])
    return RING_C + d * RING_R, d


def key(k, name, root, d, outline, colour, thick=0.013):
    """A key's flat silhouette in the ring's plane, from root along d (in the plane): outline in (along, across)."""
    d = unit(*d)
    across = np.cross((0, -1.0, 0), d)               # in the plane, square to d
    k.add(name, slab(outline, root, d, across, thick), colour, "body", fuse=False, surface="metal")


def bitted(L0, L1, w, teeth):
    """A shaft from L0 to L1 of half width w, with teeth hanging on its -across side: (start, end, depth) each."""
    pts = [(L0, w)]
    pts.append((L1, w))
    pts.append((L1, -w))
    for a, b, dep in reversed(teeth):
        pts += [(b, -w), (b, -w - dep), (a, -w - dep), (a, -w)]
    pts.append((L0, -w))
    return pts


def head_shape(V):
    """The bulb: wide below, narrowing above toward the stalk."""
    V = V.copy()
    t = np.clip((V[:, 2] + 1) / 2, 0, 1)
    w = 1 - 0.5 * smoothstep(0.35, 1.0, t)
    V[:, 0] *= w; V[:, 1] *= w
    return V


def klefki(src, out):
    k = Kit(src, coat="metal")

    # --- the ring, upright and facing front ---------------------------------------------------------------------
    k.add("ring", torus(RING_C, (0, 1, 0), RING_R, WIRE, seg=72), SILVER, "body", fuse=False, surface="metal")

    # --- the head: a bulb hanging in the ring, its stalk up through the ring to a key's head above --------------
    k.add("head", ell(HEAD_C, HEAD_R, deform=head_shape, sub=5), SILVER, "head")
    k.add("stalk", tube([(0, -0.002, 0.28), (0, 0.0, 0.31), (0.002, 0.0, 0.345)], [0.012, 0.0105, 0.0105]), SILVER,
          "head")
    for z, w in ((0.322, 0.016), (0.338, 0.019)):
        k.add(f"notch{z}", ell((0.011, 0.0, z), (w * 0.75, 0.009, 0.0065)), SILVER, "head")
    k.add("knob", ell((0.0, 0.0, 0.358), (0.016, 0.011, 0.014)), SILVER, "head")
    # the pink drop hanging under the chin
    k.add("drop", ell((0.0, -0.016, 0.177), (0.016, 0.012, 0.022),
                      deform=lambda V: V * np.array([1, 1, 1])[None] * (1 - 0.45 * np.clip(V[:, 2:3], 0, 1))),
          PINK, "head", fuse=False, surface="skin")

    # --- the keys ------------------------------------------------------------------------------------------------
    # gold, at the left on a small gold loop, pointing out and down
    p, o = on_ring(188)
    k.add("loopgold", torus(p + o * 0.012, (0, 0, 1), 0.018, 0.0055), GOLD, "body", fuse=False, surface="metal")
    r0 = p + o * 0.026 + np.array([0, -0.004, 0])
    gold = [(0, 0.018), (0.008, 0.026), (0.05, 0.026), (0.056, 0.012)] + \
           [(x, y) for x, y in bitted(0.056, 0.135, 0.008, [(0.095, 0.105, 0.01), (0.115, 0.128, 0.008)])[1:-1]] + \
           [(0.056, -0.012), (0.05, -0.026), (0.008, -0.026), (0, -0.018)]
    key(k, "keygold", r0, (-1, 0, -0.28), gold, GOLD)

    # black-bowed, grey-bladed, at the top right, pointing up and out
    p, o = on_ring(32)
    k.add("bowblack", torus(p, unit(-o[2], 0, o[0]) * 1.0, 0.017, 0.009), BLACK, "body", fuse=False, surface="metal")
    k.add("bowblock", hull([p + o * 0.012 + np.array([sx * 0.02, sy * 0.011, sz * 0.016])
                            for sx in (1, -1) for sy in (1, -1) for sz in (1, -1)]), BLACK, "body", fuse=False,
          surface="metal")
    blade = bitted(0.0, 0.1, 0.0085, [(0.045, 0.055, 0.008), (0.065, 0.075, 0.01), (0.085, 0.095, 0.007)])
    blade = [(x, -y) for x, y in blade]              # the teeth on the upper side
    key(k, "keysteel", p + o * 0.028, (0.85, 0, 0.55), blade, STEEL)

    # black with a round bow, at the bottom left, pointing out and down
    p, o = on_ring(228)
    k.add("bowround", torus(p + o * 0.016, unit(0.5 * o[2], 0.85, -0.5 * o[0]), 0.02, 0.0075), BLACK, "body", fuse=False,
          surface="metal")
    key(k, "keyblack", p + o * 0.034, (-0.85, 0, -0.5),
        bitted(0.0, 0.085, 0.0085, [(0.055, 0.067, 0.012), (0.072, 0.085, 0.009)]), BLACK)

    # copper and ornate, at the bottom right, hanging down
    p, o = on_ring(298)
    k.add("bowcopper", torus(p + o * 0.012, unit(0.5 * o[2], 0.85, -0.5 * o[0]), 0.017, 0.0065), COPPER, "body", fuse=False,
          surface="metal")
    orn = [(0, 0.016), (0.012, 0.024), (0.02, 0.014), (0.028, 0.022), (0.036, 0.009)] + \
          bitted(0.036, 0.085, 0.0085, [(0.055, 0.064, 0.01), (0.07, 0.08, 0.012)])[1:-1] + \
          [(0.036, -0.009), (0.028, -0.022), (0.02, -0.014), (0.012, -0.024), (0, -0.016)]
    key(k, "keycopper", p + o * 0.024, (0.25, 0, -1), orn, COPPER)

    # --- the face's marks, in every state: the keyhole of a mouth and the pink drop on the brow ------------------
    def front(x, z):
        """The bulb's front at x, z, and its normal (the bulb taken as its ellipsoid, the deform's narrowing kept)."""
        t = np.clip((z - HEAD_C[2]) / HEAD_R[2], -1, 1)
        w = 1 - 0.5 * smoothstep(0.35, 1.0, (t + 1) / 2)
        a = x / (HEAD_R[0] * w)
        dy = HEAD_R[1] * w * math.sqrt(max(1 - a * a - t * t, 0.0))
        p = np.array([x, HEAD_C[1] - dy, z])
        n = unit(x / (HEAD_R[0] * w) ** 2, -dy / (HEAD_R[1] * w) ** 2, (z - HEAD_C[2]) / HEAD_R[2] ** 2)
        return p, n
    ink, pk, pl = srgb(INK), srgb(PINK), srgb(PINK_LT)
    def keyhole(u, v):
        c = np.tile(ink, (len(u), 1))
        return c
    p, n = front(0.0, 0.21)
    k.decal(p, n, (0.012, 0.017), keyhole, eps=0.0015, rings=12, segs=48,
            outline=polygon([(-0.62, 0.62), (0.0, 0.96), (0.62, 0.62), (0.72, 0.18), (0.32, -0.12), (0.52, -1.0),
                             (-0.52, -1.0), (-0.32, -0.12), (-0.72, 0.18)]))
    def brow(u, v):
        c = np.tile(pk, (len(u), 1))
        c[(u / 0.3) ** 2 + ((v + 0.25) / 0.32) ** 2 < 1] = pl
        return c
    p, n = front(0.0, 0.27)
    k.decal(p, n, (0.009, 0.013), brow, eps=0.0015, rings=12, segs=48,
            outline=lambda th: 1.0 if math.sin(th) < 0.3 else 1.0 - 0.45 * (math.sin(th) - 0.3) / 0.7)

    k.build(voxel=0.003, smooth=5, band=0.004, wband=0.03)

    # --- the eyes: round and black, a small highlight ------------------------------------------------------------
    lid = srgb(SILVER)
    def open_eye(u, v):
        c = np.tile(ink, (len(u), 1))
        c[((u + 0.28) / 0.22) ** 2 + ((v - 0.32) / 0.22) ** 2 < 1] = srgb("#6E6E7A")
        return c
    pe, ne = front(0.023, 0.236)

    def eyes(state):
        k.decal(pe, ne, (0.0125, 0.0135), eye_look(state, open_eye, lid, ink, line=0.17), eps=0.0015, rings=20,
                segs=56, mirror=True)

    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    klefki(*args())
