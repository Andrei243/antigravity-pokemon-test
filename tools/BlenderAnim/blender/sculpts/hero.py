# The boy hero as a wardrobe of pieces: a body in parts, a hair cut, and garments by slot, each a mesh object of its
# own on one skeleton (the chibi kit's own joints, CharacterModels' Kid frame), so an outfit is a choice of pieces.
# A draft to look at: the game reads no character from a file yet, and this one has a skeleton and no clips.
#   blender --background --factory-startup --python sculpts/hero.py -- - <out.glb>
#   blender --background --factory-startup --python sculpts/hero_outfits.py -- <out.glb> <board.png>
#
# The file's format (what an engine would read):
#   - a node a piece, named <slot>.<Shape> after the game's own words: body.*, face.*, hair.<HairCut>,
#     hat.<Headwear>, top.<shape>, bottoms.<shape>, shoes.<shape>, bag.<shape>, extra.<what>
#   - every piece skinned to the one armature
#   - extras on each node: "colours" is "zones" or "true"; "tint" names whose colours fill the zones (skin, hair,
#     eyes, or the garment's own); "hides" lists the body's parts the piece covers, which are then not drawn
#   - a piece of zones carries no colour of its own: its vertex colour is how much of each zone a point is, red the
#     primary colour, green the accent, blue the light (white) and none of them the dark (ink), so one mesh serves
#     every colourway and every dye. A piece of true colours is drawn as it is.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, tri, unit, boxy, args

_, out = args()

PRIMARY, ACCENT, LIGHT, DARK = np.array([1.0, 0, 0]), np.array([0, 1.0, 0]), np.array([0, 0, 1.0]), np.zeros(3)

# The Kid frame's joints. The game's (x, y up, z front) is Blender's (x, -z, y)
HIP, WAIST, CHEST, NECK = 0.40, 0.50, 0.585, 0.655
H = np.array([0, -0.012, 0.915])
HR = 0.25
SHOULDER, ELBOW, WRIST = (0.128, 0, 0.625), (0.19, 0.006, 0.505), (0.23, -0.008, 0.395)
LEGTOP, KNEE, ANKLE = (0.068, 0, 0.395), (0.071, -0.008, 0.222), (0.072, 0, 0.07)

def side(p, s):
    return (p[0] * s, p[1], p[2])
def between(a, b, t):
    return tuple(a[i] + (b[i] - a[i]) * t for i in range(3))
def on_head(az, el, r=HR):
    """A point about the head: az round it from the front (toward +X), el its height as a share of the radius."""
    e = math.asin(max(-1, min(1, el)))
    return H + r * np.array([math.sin(az) * math.cos(e), -math.cos(az) * math.cos(e), math.sin(e)])

bones = [("root", None, (0, 0, 0)), ("hips", "root", (0, 0, HIP)), ("spine", "hips", (0, 0, WAIST)),
         ("head", "spine", (0, 0, NECK))]
for s, n in ((1, "L"), (-1, "R")):
    bones += [(f"upperArm.{n}", "spine", side(SHOULDER, s)), (f"foreArm.{n}", f"upperArm.{n}", side(ELBOW, s)),
              (f"hand.{n}", f"foreArm.{n}", side(WRIST, s))]
for s, n in ((1, "L"), (-1, "R")):
    bones += [(f"thigh.{n}", "hips", side(LEGTOP, s)), (f"shin.{n}", f"thigh.{n}", side(KNEE, s)),
              (f"foot.{n}", f"shin.{n}", side(ANKLE, s))]
bones += [("hair", "head", (0, 0.108, 0.935)), ("bag", "spine", (0, 0.1, 0.635)), ("skirt", "hips", (0, 0, 0.44))]

k = Kit(name="Hero", skeleton=bones, sides=("L", "R"))

def zones(tint, hides=""):
    return {"colours": "zones", "tint": tint, "hides": hides}

# --- the body, in the parts garments cover -----------------------------------------------------------------------
def chin(V):
    V = V.copy()
    V[:, 0] *= 1 - 0.14 * np.clip(-V[:, 2], 0, 1) ** 2
    return V
k.add("head", ell(H, (0.255, 0.25, 0.245), deform=chin), PRIMARY, "head")
k.add("neck", tube([(0, 0, 0.6), (0, 0, 0.7)], [0.05, 0.05]), PRIMARY, "head")
k.add("ear", ell((0.25, 0.0, 0.89), (0.03, 0.035, 0.045)), PRIMARY, "head", mirror=True)
k.build(voxel=0.007, name="body.head", props=zones("skin"))

k.add("chest", ell((0, 0, 0.555), (0.116, 0.086, 0.106)), PRIMARY, "spine")
k.add("hips", ell((0, 0, 0.44), (0.11, 0.084, 0.07)), LIGHT, "hips")
k.build(voxel=0.007, name="body.torso", props=zones("skin"))

k.add("upper", tube([SHOULDER, ELBOW], [0.042, 0.039]), PRIMARY, "upperArm.{s}", mirror=True)
k.add("fore", tube([ELBOW, WRIST], [0.039, 0.036]), PRIMARY, "foreArm.{s}", mirror=True)
k.build(voxel=0.007, name="body.arms", props=zones("skin"))
k.add("hand", ell((0.238, -0.01, 0.36), (0.04, 0.04, 0.045)), PRIMARY, "hand.{s}", mirror=True)
k.build(voxel=0.007, name="body.hands", props=zones("skin"))

k.add("thigh", tube([LEGTOP, KNEE], [0.052, 0.048]), PRIMARY, "thigh.{s}", mirror=True)
k.add("shin", tube([KNEE, (0.072, 0, 0.09)], [0.048, 0.044]), PRIMARY, "shin.{s}", mirror=True)
k.build(voxel=0.007, name="body.legs", props=zones("skin"))
k.add("foot", ell((0.072, -0.025, 0.036), (0.05, 0.085, 0.036)), PRIMARY, "foot.{s}", mirror=True)
k.build(voxel=0.007, name="body.feet", props=zones("skin"))

# --- the face, on the head's surface --------------------------------------------------------------------------------
def eye(u, v):
    c = np.tile(DARK, (len(u), 1))
    c[(u / 0.62) ** 2 + ((v + 0.25) / 0.6) ** 2 < 1] = PRIMARY
    c[(u / 0.34) ** 2 + ((v + 0.1) / 0.4) ** 2 < 1] = DARK
    c[((u + 0.25) / 0.3) ** 2 + ((v - 0.45) / 0.26) ** 2 < 1] = LIGHT
    return c
de = unit(0.37, -0.92, -0.17)
k.decal(H + de * 0.25, de, (0.038, 0.054), eye, eps=0.004, rings=10, segs=32, mirror=True)
k.build(name="face.eyes", props=zones("eyes"), surface="body.head")

db = unit(0.6, -0.72, -0.36)
k.decal(H + db * 0.25, db, (0.036, 0.02), "#F6ABA6", eps=0.003, rings=5, segs=24, mirror=True)
dm = unit(0, -0.88, -0.47)
k.decal(H + dm * 0.25, dm, (0.022, 0.006), "#B45A4C", eps=0.004, rings=3, segs=20)
k.build(name="face.marks", props={"colours": "true", "tint": "", "hides": ""}, surface="body.head")

# --- hair: a shell over the head with a window for the face, locks over the brow and tufts at the sides ----------
k.add("shell", ell(H + np.array([0, 0.012, 0.008]), (0.268, 0.262, 0.258)), PRIMARY, "hair")
k.add("back", ell((0, 0.05, 0.935), (0.268, 0.24, 0.245)), PRIMARY, "hair")
k.add("nape", ell((0, 0.17, 0.78), (0.17, 0.1, 0.1)), PRIMARY, "hair")
for i, (a, b) in enumerate([((0.21, 0.02, 0.9), (0.285, 0.03, 0.74)), ((0.19, 0.12, 0.86), (0.25, 0.17, 0.72)),
                            ((0.08, 0.2, 0.84), (0.1, 0.27, 0.71))]):
    k.add(f"tuft{i}", tube([a, b], [0.075, 0.012]), PRIMARY, "hair", mirror=True)
for i, (az, low, lean) in enumerate([(-0.78, 0.2, -0.1), (-0.4, 0.3, -0.06), (0.0, 0.22, 0.05), (0.4, 0.32, 0.06),
                                      (0.78, 0.2, 0.1)]):
    k.add(f"fringe{i}", tube([on_head(az, 0.64, 0.25), on_head(az + lean * 0.5, 0.5, 0.268),
                              on_head(az + lean, low, 0.268)], [0.028, 0.052, 0.01]), PRIMARY, "hair")
def window(P):
    d = P - H
    az = np.abs(np.arctan2(d[:, 0], -d[:, 1]))
    el = d[:, 2] / 0.25
    face = (az < 1.2) & (el < 0.58)
    jaw = (az < 2.1) & (el < -0.42)
    return (face | jaw).astype(float)
k.build(voxel=0.007, name="hair.Short", props=zones("hair"), cut=window)

# --- hats -------------------------------------------------------------------------------------------------------------
k.add("crown", ell((0, -0.005, 1.118), (0.275, 0.275, 0.085), rot=(8, 0, 0)), PRIMARY, "head", fuse=False)
k.add("band", ell((0, 0.0, 1.062), (0.243, 0.243, 0.032), rot=(8, 0, 0)), ACCENT, "head", fuse=False)
k.add("nub", ell((0, 0.01, 1.203), (0.022, 0.022, 0.02)), PRIMARY, "head", fuse=False)
k.build(name="hat.Beret", props=zones("garment"))

def dome(V):
    V = V.copy()
    V[:, 2] = np.maximum(V[:, 2], -0.12)
    return V
k.add("dome", ell((0, 0.0, 1.03), (0.276, 0.272, 0.165), deform=dome), PRIMARY, "head", fuse=False)
k.add("brim", ell((0, -0.27, 1.008), (0.17, 0.15, 0.018), rot=(-10, 0, 0)), ACCENT, "head", fuse=False)
k.add("button", ell((0, 0.0, 1.195), (0.025, 0.025, 0.014)), ACCENT, "head", fuse=False)
k.build(name="hat.Cap", props=zones("garment"))

# --- tops -------------------------------------------------------------------------------------------------------------
k.add("chest", ell((0, 0, 0.555), (0.131, 0.101, 0.117)), PRIMARY, "spine")
k.add("hem", ell((0, 0, 0.445), (0.126, 0.098, 0.077)), PRIMARY, "hips")
k.add("upper", tube([SHOULDER, ELBOW], [0.052, 0.047]), PRIMARY, "upperArm.{s}", mirror=True)
k.add("fore", tube([ELBOW, between(ELBOW, WRIST, 0.9)], [0.047, 0.045]), PRIMARY, "foreArm.{s}", mirror=True)
k.add("collar", ell((0, 0.0, 0.648), (0.088, 0.082, 0.03)), ACCENT, "spine", fuse=False)
k.decal((0, -0.1, 0.52), (0, -1, 0), (0.007, 0.115), ACCENT, eps=0.003, rings=3, segs=16)
k.build(voxel=0.007, name="top.Jacket", props=zones("garment", "torso,arms"))

k.add("chest", ell((0, 0, 0.555), (0.127, 0.097, 0.114)), PRIMARY, "spine")
k.add("hem", ell((0, 0, 0.455), (0.121, 0.094, 0.068)), PRIMARY, "hips")
k.add("sleeve", tube([SHOULDER, between(SHOULDER, ELBOW, 0.55)], [0.054, 0.053]), PRIMARY, "upperArm.{s}", mirror=True)
# Stripes are the accent: a tee with none is one whose accent is its own colour
k.paint(lambda P: inside(np.sin(P[:, 2] * 2 * math.pi / 0.062) * 0.01, 0.003), ACCENT, on=["chest", "hem"])
k.build(voxel=0.007, name="top.Tee", props=zones("garment", "torso"))

# --- bottoms ---------------------------------------------------------------------------------------------------------
k.add("seat", ell((0, 0, 0.43), (0.118, 0.092, 0.07)), PRIMARY, "hips")
k.add("thigh", tube([LEGTOP, KNEE], [0.062, 0.057]), PRIMARY, "thigh.{s}", mirror=True)
k.add("shin", tube([KNEE, (0.072, 0, 0.1)], [0.057, 0.055]), PRIMARY, "shin.{s}", mirror=True)
k.build(voxel=0.007, name="bottoms.Trousers", props=zones("garment", "legs"))

k.add("seat", ell((0, 0, 0.43), (0.118, 0.092, 0.07)), PRIMARY, "hips")
k.add("leg", tube([LEGTOP, between(LEGTOP, KNEE, 0.72)], [0.066, 0.066]), PRIMARY, "thigh.{s}", mirror=True)
k.paint(lambda P: inside(P[:, 2] - 0.275, 0.003), ACCENT, on=["leg"])
k.build(voxel=0.007, name="bottoms.Shorts", props=zones("garment"))

# --- shoes, a bag, a scarf ---------------------------------------------------------------------------------------------
k.add("shoe", ell((0.072, -0.03, 0.045), (0.062, 0.098, 0.042)), PRIMARY, "foot.{s}", fuse=False, mirror=True)
k.add("sole", ell((0.072, -0.03, 0.016), (0.065, 0.101, 0.017)), ACCENT, "foot.{s}", fuse=False, mirror=True)
k.build(name="shoes.Sneakers", props=zones("garment", "feet"))

k.add("bag", ell((0, 0.135, 0.545), (0.082, 0.05, 0.09), deform=boxy(4)), PRIMARY, "bag", fuse=False)
k.add("flap", ell((0, 0.16, 0.585), (0.075, 0.035, 0.045), deform=boxy(4)), ACCENT, "bag", fuse=False)
k.add("strap", tube([(0.075, 0.09, 0.64), (0.08, -0.03, 0.668), (0.085, -0.094, 0.58), (0.085, -0.088, 0.47)],
                    [0.013, 0.013, 0.013, 0.013]), PRIMARY, "spine", fuse=False, mirror=True)
k.build(name="bag.Backpack", props=zones("garment"))

k.add("wrap", ell((0, -0.005, 0.662), (0.105, 0.098, 0.04)), PRIMARY, "spine", fuse=False)
k.add("tail", tube([(0.045, -0.085, 0.655), (0.05, -0.108, 0.58), (0.045, -0.104, 0.51)], [0.03, 0.028, 0.02]),
      PRIMARY, "spine", fuse=False)
k.build(name="extra.Scarf", props=zones("garment"))

k.export(out)
