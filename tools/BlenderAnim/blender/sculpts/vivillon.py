# Vivillon (its Meadow pattern, the species' own) sculpted from nothing on the skeleton and clips of the game's own
# export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/vivillon.py -- <the export's Vivillon.glb> <out.glb>
#
# Grown from scatterbug.py (sibling("scatterbug")): the line's dark grey and tan, and the way a face is laid on a round
# head (on_head). Written so that the other patterns can be grown from it: the wings' colours are one dictionary
# (MEADOW) handed to wings().
#
# From the references: a small body, a white thorax with two black beads on its front, a dark grey abdomen below it
# with two little black legs tipped tan hanging before it; a round head, dark grey above and pale silver below, a black
# band across it holding two eyes that are white and black checks, an open smiling mouth with a pink tongue; two black
# feelers with cream clubs. Four broad wings round the body, the upper pair large and reaching up and out to a tip
# that is cut in square steps, the lower pair smaller and rounded: pink, a crimson band toward the edge with pale
# blocks in it, a dark rim, and round the root a ring of cyan and white blocks with crimson blocks beyond it. The
# pattern is stacked slabs, each colour thicker than the one under it, so it shows on both faces; the wings are a
# mirror pair in the plane of the body swept a little back, their faces to both of the game's cameras.
# Bones: root, body (0, 0, 0.222), wing{s} (0.02, 0.02, 0.232), head (0, -0.01, 0.292).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, outward, srgb, inside, smoothstep, unit, eye_look, args, polygon, sibling

sb = sibling("scatterbug")

GREY, SILVER, BAND, THORAX, ABDOMEN, BEAD = "#4A4A52", "#D6D6DC", "#1E1E24", "#EEEEF0", "#36363C", "#1C1C20"
TAN, MOUTH, TONGUE, WHITE = sb.TAN, "#5E1A2A", "#F07E98", "#FFFFFF"

MEADOW = dict(rim="#3E3438", band="#C8355C", ground="#F28CB4", block="#FBDDE8", inner="#40C4D2", pale="#F8F0F4",
              ring="#C8355C")

HC = np.array([0.0, -0.018, 0.3])            # the head's middle
HR = np.array([0.045, 0.041, 0.042])
T = np.array([0.0, -0.004, 0.236])           # the thorax's middle
TR = np.array([0.03, 0.028, 0.03])
EYE_Z = HC[2] + 0.004                        # the eye band's middle


def body(k):
    k.add("thorax", ell(T, TR), THORAX, "body")
    k.add("abdomen", ell((0, 0.006, 0.192), (0.021, 0.021, 0.034), rot=(-10, 0, 0)), ABDOMEN, "body")
    k.add("tail", ell((0, 0.014, 0.158), (0.013, 0.013, 0.02), rot=(-15, 0, 0)), ABDOMEN, "body")
    for s in (1, -1):
        k.add(f"bead{'R' if s > 0 else 'L'}", ell((0.017 * s, -0.026, 0.236), (0.0075, 0.0075, 0.0075), sub=3),
              BEAD, "body", fuse=False, surface="shell")
    # two little black legs hanging before the abdomen, tipped tan
    k.add("leg", tube([(0.016, -0.012, 0.212), (0.024, -0.028, 0.196), (0.026, -0.036, 0.178)],
                      [0.0085, 0.0078, 0.0072]), ABDOMEN, "body", mirror=True)
    k.add("foot", outward(ell((0.027, -0.038, 0.17), (0.0085, 0.0085, 0.011), sub=3)), TAN, "body", fuse=False,
          mirror=True, surface="shell")


def head(k):
    k.add("skull", ell(HC, HR, sub=5), GREY, "head")
    front = lambda P: smoothstep(-0.005, -0.02, P[:, 1] - HC[1])
    k.paint(lambda P: inside(P[:, 2] - (EYE_Z - 0.014), 0.0015) * front(P), SILVER, on=["skull"])
    k.paint(lambda P: inside(np.abs(P[:, 2] - EYE_Z + 0.001) - 0.0135, 0.0015) * front(P)
            * inside(np.abs(P[:, 0]) - 0.04, 0.003), BAND, on=["skull"])
    # the open smiling mouth: flat along its top, round below, a pink tongue
    p, n = sb.on_head((0, -0.92, -0.42), HC, HR)
    def mouth(u, v, m=srgb(MOUTH), t=srgb(TONGUE)):
        c = np.tile(m, (len(u), 1))
        c[(v < -0.15) & (u * u + (v + 0.75) ** 2 < 0.5)] = t
        return c
    smile = polygon([(math.cos(a), min(0.3, math.sin(a))) for a in np.linspace(0, 2 * math.pi, 40, endpoint=False)])
    k.decal(p, n, (0.013, 0.011), mouth, eps=0.002, outline=smile, rings=14, segs=48)


def feelers(k):
    pts = [(0.01, -0.02, 0.335), (0.018, -0.022, 0.36), (0.03, -0.024, 0.382), (0.044, -0.024, 0.398)]
    k.add("feeler", outward(tube(pts, [0.0068, 0.0064, 0.006, 0.0058])), BAND, "head", fuse=False, mirror=True)
    k.add("club", outward(ell((0.049, -0.024, 0.41), (0.01, 0.01, 0.016), rot=(0, -28, 0), sub=3)), TAN, "head",
          fuse=False, mirror=True, surface="shell")


# --- the wings: fans round the wing's root, each colour a slab thicker than the one under it ------------------------
ORIGIN = np.array([0.012, 0.026, 0.236])
EX = unit(1, 0.3, 0)                         # out, swept a little back
EZ = np.array([0, 0, 1.0])

# (angle in degrees, up positive; radius): the upper wing's outline, its tip cut in square steps on the inner side
UPPER = [(5, 0.168), (14, 0.212), (24, 0.248), (34, 0.278), (44, 0.302), (52, 0.318), (58, 0.326), (63.9, 0.326),
         (64, 0.296), (70.9, 0.296), (71, 0.262), (77.9, 0.262), (78, 0.2), (83, 0.2), (86, 0.12)]
LOWER = [(-5, 0.15), (-14, 0.18), (-24, 0.198), (-34, 0.206), (-44, 0.206), (-54, 0.198), (-63, 0.18),
         (-71, 0.15), (-77, 0.11)]


def radius(table, a):
    A = np.array([t[0] for t in table]); R = np.array([t[1] for t in table])
    if A[1] < A[0]:
        A, R = A[::-1], R[::-1]
    return float(np.interp(a, A, R))


def fan(table, inner, outer, a0, a1, n=40):
    """The outline of the part of a wing between two radii (each a share of the outline, or a length if absolute),
    from angle a0 to a1: a pie when inner is nought."""
    angs = np.linspace(a0, a1, n)
    def r(f, a):
        return f(a) if callable(f) else f * radius(table, a)
    out_ = [(r(outer, a) * math.cos(math.radians(a)), r(outer, a) * math.sin(math.radians(a))) for a in angs]
    if inner == 0:
        return np.array([(0.0, 0.0)] + out_)
    in_ = [(r(inner, a) * math.cos(math.radians(a)), r(inner, a) * math.sin(math.radians(a))) for a in angs[::-1]]
    return np.array(out_ + in_)


def plate(k, name, outline, colour, thick):
    k.add(name, slab(outline, ORIGIN, EX, EZ, thick), colour, "wing{s}", fuse=False, mirror=True, surface="leaf")


def wing(k, tag, table, a0, a1, colours, blocks, rings):
    lo, hi = min(a0, a1), max(a0, a1)
    # the steps of a tip are the outline's own: sample its angles finely so each step is square
    plate(k, f"{tag}rim", fan(table, 0, 1.0, lo, hi, 160), colours["rim"], 0.006)
    plate(k, f"{tag}band", fan(table, 0, 0.94, lo + 0.6, hi - 0.6, 160), colours["band"], 0.009)
    plate(k, f"{tag}ground", fan(table, 0, 0.76, lo + 1.2, hi - 1.2, 80), colours["ground"], 0.012)
    # pale blocks in the crimson band
    for i, (b0, b1) in enumerate(blocks):
        plate(k, f"{tag}block{i}", fan(table, 0.8, 0.9, b0, b1, 10), colours["block"], 0.0125)
    # the ring round the root: cyan and white blocks, crimson blocks beyond it
    for i, (r0, r1, b0, b1, c) in enumerate(rings):
        plate(k, f"{tag}ring{i}", fan(table, lambda a, r0=r0: r0, lambda a, r1=r1: r1, b0, b1, 8), colours[c], 0.016)


def wings(k, colours=MEADOW):
    upper_rings = [(0.05, 0.085, 10, 22, "inner"), (0.05, 0.085, 24, 36, "pale"), (0.05, 0.085, 38, 50, "inner"),
                   (0.05, 0.085, 52, 64, "pale"), (0.05, 0.085, 66, 78, "inner"),
                   (0.1, 0.13, 16, 28, "ring"), (0.1, 0.13, 42, 54, "pale"), (0.1, 0.13, 62, 74, "ring")]
    lower_rings = [(0.05, 0.08, -12, -24, "inner"), (0.05, 0.08, -26, -38, "pale"), (0.05, 0.08, -40, -52, "inner"),
                   (0.05, 0.08, -54, -66, "pale"), (0.095, 0.12, -20, -32, "ring"), (0.095, 0.12, -46, -58, "pale")]
    wing(k, "upper", UPPER, 5, 86, colours, [(18, 28), (40, 48)], upper_rings)
    wing(k, "lower", LOWER, -77, -5, colours, [(-22, -32), (-48, -58)],
         [(r0, r1, min(b0, b1), max(b0, b1), c) for r0, r1, b0, b1, c in lower_rings])


# --- the eyes: white and black checks on the black band ----------------------------------------------------------------
def eye_open(u, v, black=srgb(BAND), white=srgb(WHITE)):
    c = np.tile(black, (len(u), 1))
    ins = (np.abs(u) < 0.76) & (np.abs(v) < 0.7) & (np.abs(u) > 0.05) & (np.abs(v) > 0.05)
    c[ins & (u * v < 0)] = white
    return c


def eyes(k):
    p, n = sb.on_head((0.46, -0.86, 0.06), HC, HR)
    box = polygon([(-1, -0.85), (1, -0.85), (1, 0.85), (-1, 0.85)])
    def lay(state):
        k.decal(p, n, (0.0155, 0.0135), eye_look(state, eye_open, srgb(BAND), srgb(SILVER), line=0.16),
                eps=0.002, outline=box, rings=24, segs=64, mirror=True)
    return lay


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="fur")
    body(k)
    head(k)
    feelers(k)
    wings(k)
    k.build(voxel=0.0042, smooth=3, band=0.005, wband=0.03)
    k.eye_states(eyes(k))
    k.export(out)
