# Magearna sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/magearna.py -- <the export's Magearna.glb> <out.glb>
#
# From the references: a clockwork girl, pale pinkish steel. Most of her is a great ball of a gown shaped like a Poke
# Ball: gold crescents (smiles) round its upper half, a black band round its middle with a gold ring of a buckle in
# front (a white rounded square inside it), its lower half paler and parting at the bottom into two lobes over two
# thin pointed legs. On it a small glass ball of a chest, a Poke Ball's halves (red to the viewer's left, blue to the
# right) inside; round shoulders, thin arms with pointed claw-like hands (carried forward and down here; the art holds
# them out). A round white head framed behind by a cog of a halo, its teeth on the right (+X) side only; big pink eyes
# in gold frames with a gold line rising over each, a small blue nose, a tiny mouth. Two great fans of ears, crescent
# plates rimmed in gold, rising up and out from the head's top.
# Ratios (3D picture): gown width / height 0.5, gown height / height 0.42, head width / gown width 0.45, an ear's
# span / head width 1.15. The export stands 0.68 tall: voxel 0.006, the thickness floor 0.02.
# Bones: body (0, 0, 0.3); head (0, 0, 0.47); ears (+-0.03, 0, 0.58); arms (+-0.045, 0, 0.45); legs (+-0.05, 0, 0.14).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, hull, slab, lathe, ring_band, srgb, inside, unit, eye_look, args, outward)

STEEL, WHITE, GOLD, BLACK = "#DCC6CF", "#F2EDF0", "#E4C44A", "#2A2A30"
GLASS, RED, BLUE = "#D6ECF0", "#E2405A", "#3F86D6"
PINK, PINK_DK, PINK_LT = "#F49AAC", "#D04A6E", "#FBD0D8"
INK = "#3A2630"

GC, GR = np.array([0.0, 0.0, 0.2]), np.array([0.165, 0.165, 0.142])      # the gown
SC = np.array([0.0, -0.006, 0.372])                                         # the chest's glass ball
H, HR = np.array([0.0, -0.006, 0.522]), np.array([0.084, 0.072, 0.078])   # the head


def on_ell(c, r, d):
    d = unit(*d)
    s = 1 / math.sqrt(((d / np.array(r)) ** 2).sum())
    p = np.array(c) + d * s
    n = (p - c) / np.array(r) ** 2
    return p, n / np.linalg.norm(n)


def add_gown(k):
    k.add("gown", ell(GC, GR, sub=5), STEEL, "body")
    # the lower half parting into two lobes over the legs
    for s in (1, -1):
        k.add(f"lobe{'R' if s > 0 else 'L'}", ell((0.066 * s, 0.004, 0.105), (0.08, 0.1, 0.06)), WHITE,
              "body")
    k.paint(lambda P: inside(P[:, 2] - GC[2], 0.003), WHITE, on=["gown", "lobe"])
    # the black band round the middle, the gold buckle in front with its white square
    k.add("band", ring_band((0, 0, GC[2]), (0, 0, 1), GR[0] - 0.001, 0.014, thick=0.007), BLACK, "body", fuse=False,
          surface="metal")
    k.add("buckle", lathe((0, -0.157, GC[2]), (0, -1, 0), [(0.024, 0.0), (0.04, 0.0), (0.04, 0.014), (0.024, 0.014),
                                                           (0.024, 0.0)], seg=48), GOLD, "body", fuse=False,
          surface="metal")
    k.add("buckleplate", hull([(x, y, z) for x in (-0.018, 0.018) for y in (-0.158, -0.168) for z in (0.18, 0.22)]),
          WHITE, "body", fuse=False, surface="metal")
    # gold smiles round the upper half
    for i in range(8):
        az = math.radians(22.5 + 45 * i)
        el = math.radians(30)
        out_ = np.array([math.sin(az) * math.cos(el), -math.cos(az) * math.cos(el), math.sin(el)])
        t = np.array([math.cos(az), math.sin(az), 0.0])
        up = np.array([-math.sin(az) * math.sin(el), math.cos(az) * math.sin(el), math.cos(el)])
        pts = []
        for s in np.linspace(-1, 1, 7):
            d = out_ + t * s * 0.3 - up * 0.12 * (1 - s * s)
            p, n = on_ell(GC, GR, d)
            pts.append(p + n * 0.004)
        k.add(f"smile{i}", tube(pts, [0.004, 0.008, 0.011, 0.012, 0.011, 0.008, 0.004], seg=12, per=4), GOLD, "body",
              fuse=False, surface="metal")
    # two thin pointed legs under the lobes
    k.add("leg", tube([(0.04, -0.01, 0.07), (0.04, -0.012, 0.035), (0.04, -0.015, 0.01)], [0.016, 0.013, 0.01]),
          WHITE, "leg{s}", mirror=True)


def add_chest(k):
    k.add("neck", tube([(0, -0.004, 0.4), (0, -0.006, 0.45)], [0.02, 0.02]), STEEL, "head")
    k.add("chest", ell(SC, (0.044, 0.042, 0.044)), GLASS, "body")
    k.add("chestseat", ell((0, 0, 0.336), (0.05, 0.048, 0.016)), STEEL, "body")
    # the Poke Ball inside: red to the viewer's left (-X), blue to the right, a black line and a white button
    def ball(u, v):
        c = np.tile(srgb(RED), (len(u), 1))
        c[u > 0] = srgb(BLUE)
        c[np.abs(u) < 0.12] = srgb(BLACK)
        r = u * u + v * v
        c[r < 0.33 ** 2] = srgb(BLACK)
        c[r < 0.22 ** 2] = srgb(WHITE)
        c[r > 0.82 ** 2] = srgb(GLASS)
        return c
    k.decal(SC + np.array([0, -0.044, 0.0]), (0, -1, 0), (0.033, 0.033), ball, eps=0.0025, rings=16, segs=56)
    # round shoulders, thin arms, pointed claw-like hands carried forward and down
    k.add("shoulder", ell((0.064, -0.004, 0.404), (0.032, 0.032, 0.032)), STEEL, "arm{s}", mirror=True)
    k.add("arm", tube([(0.08, -0.012, 0.398), (0.094, -0.04, 0.38), (0.102, -0.062, 0.366)], [0.013, 0.012, 0.013]),
          STEEL, "arm{s}", mirror=True)
    k.add("hand", tube([(0.1, -0.058, 0.368), (0.11, -0.084, 0.352), (0.118, -0.112, 0.336), (0.124, -0.138, 0.322)],
                       [0.018, 0.024, 0.017, 0.006]), STEEL, "arm{s}", mirror=True)


def add_head(k):
    k.add("head", ell(H, HR), WHITE, "head")
    # the cog behind the head, its teeth on the right (+X) side
    k.add("cog", lathe(H + np.array([0, 0.03, 0]), (0, 1, 0), [(0.074, -0.013), (0.104, -0.013), (0.104, 0.013),
                                                                 (0.074, 0.013), (0.074, -0.013)], seg=64),
          WHITE, "head", fuse=False, surface="metal")
    for i, a in enumerate((-50, -12, 26, 64)):
        th = math.radians(a)
        r1, r2 = 0.1, 0.124
        e = np.array([math.cos(th), 0, math.sin(th)])
        t = np.array([-math.sin(th), 0, math.cos(th)])
        c = H + np.array([0, 0.03, 0])
        pts = [c + e * r + t * w + np.array([0, y, 0]) for r, ww in ((r1, 0.02), (r2, 0.015)) for w in (ww, -ww)
               for y in (-0.012, 0.012)]
        k.add(f"tooth{i}", hull(pts), WHITE, "head", fuse=False, surface="metal")
    # the ears: great crescent fans rising up and out, rimmed in gold (the rim thicker, so it shows on both faces)
    outer = [(0.0, 0.0), (0.02, 0.06), (0.06, 0.1), (0.11, 0.115), (0.16, 0.095), (0.19, 0.05), (0.2, 0.012)]
    inner = [(0.15, 0.036), (0.1, 0.036), (0.05, 0.022), (0.014, -0.006)]
    ex, ez, o = unit(0.95, 0.3, 0), unit(0, 0.25, 1), (0.026, 0.012, 0.582)
    k.add("ear", slab(outer + inner, o, ex, ez, 0.02), STEEL, "ear{s}", fuse=False, mirror=True, surface="metal")
    mid = np.array([0.1, 0.045])
    rim_out = [np.array(p) for p in outer[1:]]
    rim_in = [p + 0.016 * (mid - p) / np.linalg.norm(mid - p) for p in rim_out]
    rim = [tuple(p) for p in rim_out] + [tuple(p) for p in rim_in[::-1]]
    k.add("earrim", slab(rim, o, ex, ez, 0.028), GOLD, "ear{s}", fuse=False, mirror=True, surface="metal")
    # the gold frame round each eye with a line rising over it (in every state, under the eye's own patch)
    for side in (1, -1):
        p, n = on_ell(H, HR, (0.5 * side, -0.84, 0.04))
        k.decal(p, n, (0.0275, 0.0335), GOLD, eps=0.002, rings=16, segs=56,
                outline=lambda th: 1.0 + 0.5 * max(0.0, math.sin(th)) ** 24)
    # the small blue nose and the tiny mouth
    p, n = on_ell(H, HR, (0, -0.97, -0.2))
    k.decal(p, n, (0.0075, 0.0075), BLUE, eps=0.002, rings=6, segs=32)
    p, n = on_ell(H, HR, (0, -0.9, -0.44))
    k.decal(p, n, (0.0065, 0.0058), INK, eps=0.002, rings=6, segs=32)


def open_eye(u, v):
    r = u * u + v * v
    c = np.tile(srgb(PINK), (len(u), 1))
    c[r > 0.8 ** 2] = srgb(PINK_DK)
    c[(r > 0.36 ** 2) & (r < 0.5 ** 2)] = srgb(PINK_DK)
    c[r < 0.2 ** 2] = srgb(PINK_LT)
    c[((u + 0.3) / 0.16) ** 2 + ((v - 0.34) / 0.16) ** 2 < 1] = (1, 1, 1)
    return c


def eyes(k, state):
    look = eye_look(state, open_eye, srgb(WHITE), srgb(INK), line=0.16)
    for side in (1, -1):
        p, n = on_ell(H, HR, (0.5 * side, -0.84, 0.04))
        col = look if side == 1 else (lambda a, b, g=look: g(-a, b))
        k.decal(p, n, (0.023, 0.029), col, eps=0.0032, rings=20, segs=56)


def magearna(src, out):
    k = Kit(src, coat="metal")
    add_gown(k)
    add_chest(k)
    add_head(k)
    k.build(voxel=0.006, smooth=4, band=0.005, wband=0.04)
    k.eye_states(lambda s: eyes(k, s))
    k.export(out)


if __name__ == "__main__":
    magearna(*args())
