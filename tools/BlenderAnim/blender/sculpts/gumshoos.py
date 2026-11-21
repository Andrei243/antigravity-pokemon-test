# Gumshoos sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/gumshoos.py -- <the export's Gumshoos.glb> <out.glb>
# Yungoos grown (yungoos.py, whose grin, claws, spikes, tuft and ears it borrows). From the references: a stout brown
# mongoose standing up, a pear of a body wide at the hips leaning its head forward, head and neck one mass; a great
# yellow helmet of hair over the top and back of the head swept forward over the brow like a bowl; heavy-lidded,
# half-shut eyes under its rim; a big round red nose; a grin of white teeth and a spiky yellow beard; small round
# ears; a wide yellow front from the chin to the legs; short arms with dark claws, a tuft of fur at each elbow;
# pointed tufts of fur along the back; short thick legs on wide feet with black claws; a long tail along the ground,
# yellow underneath, ending in yellow points.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, polygon, args, sibling

y_ = sibling("yungoos")

BROWN, YELLOW, NOSE = "#9C7458", "#EAD064", "#E85A64"
LID, BLACK, WHITE, CLAW = "#5C4A44", y_.BLACK, y_.WHITE, y_.CLAW
INK, EARIN = "#2E2220", "#3A2A26"

# The frame (joints: body (0, 0, 0.24), arms (+-0.06, -0.03, 0.32), legs (+-0.05, 0, 0.12), tail (0, 0.06, 0.15),
# head (0, -0.02, 0.37))
HC = np.array([0.0, -0.08, 0.44])       # the head's middle, forward of the neck: a third larger than measured
HR = np.array([0.1, 0.094, 0.084])
MC = HC + np.array([0, -0.06, -0.066])  # the muzzle under the hair's rim
MR = np.array([0.064, 0.042, 0.036])
SPINE = [(0.17, 0.012), (0.28, -0.008), (0.36, -0.04), (0.43, -0.075)]   # (height, the body's middle y)
TAIL = [(0, 0.06, 0.15), (0.005, 0.15, 0.075), (0.03, 0.26, 0.038), (0.07, 0.36, 0.03), (0.1, 0.42, 0.034)]
TRAD = [0.042, 0.033, 0.026, 0.022, 0.02]


def heavy_eye(lid_v=0.12):
    """A half-shut eye: a heavy dark lid over its top, a black line at the lid's edge, the pupil low in the white."""
    L, K, W, P = srgb(LID), srgb(INK), srgb(WHITE), srgb(BLACK)
    def look(u, v):
        c = np.tile(W, (len(u), 1))
        c[((u + 0.05) / 0.42) ** 2 + ((v + 0.25) / 0.5) ** 2 < 1] = P
        c[np.hypot(u - 0.12, v + 0.05) < 0.13] = W
        edge = lid_v - 0.12 * u
        c[v > edge] = L
        c[np.abs(v - edge) < 0.14] = K
        return c
    return look


def gumshoos(src, out):
    k = Kit(src, coat="fur")

    # The pear of a body leaning forward, the neck and head one mass with it
    k.add("hips", ell((0, 0.012, 0.17), (0.1, 0.095, 0.115)), BROWN, "body")
    k.add("middle", ell((0, -0.008, 0.28), (0.08, 0.08, 0.09)), BROWN, "body")
    k.add("neck", ell((0, -0.045, 0.36), (0.078, 0.078, 0.078)), BROWN, "head")
    k.add("head", ell(HC, HR), BROWN, "head")
    k.add("muzzle", ell(MC, MR), BROWN, "head")

    # The helmet of hair, the head's largest mass: tall and rounded over the top, swept back and hanging down behind
    # to the shoulders, its front rim forward over the brow just above the eyes
    k.add("hair", ell(HC + (0, 0.004, 0.08), (0.112, 0.122, 0.082), rot=(-10, 0, 0)), YELLOW, "head")
    k.add("hairbrim", ell(HC + (0, -0.062, 0.072), (0.104, 0.074, 0.052), rot=(-12, 0, 0)), YELLOW, "head")
    k.add("hairback", ell(HC + (0, 0.07, -0.03), (0.1, 0.07, 0.1), rot=(20, 0, 0)), YELLOW, "head")
    k.add("hairnape", ell(HC + (0, 0.09, -0.1), (0.08, 0.05, 0.06)), YELLOW, "head")
    k.add("hairside", ell(HC + (0.07, 0.02, 0.04), (0.044, 0.08, 0.05)), YELLOW, "head", mirror=True)
    y_.ear(k, HC + (0.099, 0.012, -0.018), (0.021, 0.01, 0.023), (0, 0, -15), BROWN, EARIN)

    # Short thick legs: haunches fused into the hips, wide feet, black claws
    k.add("thigh", ell((0.056, 0.008, 0.105), (0.058, 0.062, 0.072)), BROWN, "leg{s}", mirror=True)
    k.add("shin", tube([(0.06, -0.002, 0.07), (0.062, -0.014, 0.024)], [0.034, 0.03]), BROWN, "leg{s}", mirror=True)
    k.add("foot", ell((0.064, -0.034, 0.013), (0.034, 0.05, 0.016)), BROWN, "leg{s}", mirror=True)
    y_.claws(k, "toe", (0.064, -0.082, 0.012), 0.016, 0.026, CLAW, "leg{s}")

    # Short arms down and forward, the hands before the belly; a tuft of fur back from each elbow, dark claws
    k.add("arm", tube([(0.062, -0.03, 0.322), (0.086, -0.028, 0.27), (0.074, -0.08, 0.238)], [0.029, 0.026, 0.024]),
          BROWN, "arm{s}", mirror=True)
    k.add("hand", ell((0.072, -0.092, 0.234), (0.025, 0.026, 0.022)), BROWN, "arm{s}", mirror=True)
    y_.claws(k, "finger", (0.072, -0.114, 0.23), 0.01, 0.018, CLAW, "arm{s}")
    k.add("elbow", y_.spike((0.088, -0.018, 0.27), (0.35, 1, -0.7), 0.05, 0.016, 0.01), BROWN, "arm{s}", fuse=False,
          mirror=True, surface="fur")

    # Pointed tufts of fur down the back and the sides of the hips
    for i, (z, ang, ln) in enumerate(((0.34, 0.5, 0.05), (0.27, 0.75, 0.06), (0.2, 0.95, 0.065), (0.13, 1.15, 0.06))):
        for s in (1, -1):
            yc = np.interp(z, [p[0] for p in SPINE], [p[1] for p in SPINE])
            rad = 0.07 if z > 0.25 else 0.092
            base = np.array([s * rad * math.sin(ang), yc + rad * math.cos(ang) * 0.9, z])
            k.add(f"backfur{i}{'R' if s > 0 else 'L'}", y_.spike(base, (s * 0.5, 0.75, -0.55), ln, 0.018, 0.011),
                  BROWN, "body", fuse=False, surface="fur")

    # The long tail along the ground, swung out to +X, yellow underneath and at its end
    k.add("tail", tube(TAIL, TRAD, seg=24), BROWN, "tail")
    def tail_under(P):
        zc, xc = y_.along_y(P, TAIL)
        return np.maximum(inside(P[:, 2] - (zc - 0.002), 0.002) * (P[:, 1] > 0.12), inside(0.39 - P[:, 1], 0.003))
    k.paint(tail_under, YELLOW, on=["tail"])
    y_.tuft(k, "tailtip", TAIL[-1], (0, 0, 1), (0.45, 0.89, 0.05), 0.08, 0.02, YELLOW, "tail", count=5)

    # The yellow front from the chin to the legs
    def front(P):
        z = P[:, 2]
        yc = np.interp(z, [p[0] for p in SPINE], [p[1] for p in SPINE])
        ang = np.abs(np.arctan2(P[:, 0], -(P[:, 1] - yc)))
        w = np.interp(z, [0.08, 0.15, 0.3, 0.4], [0.5, 0.95, 0.85, 0.7])
        return inside(ang - w, 0.04) * (z > 0.075) * (z < MC[2] - 0.01)
    k.paint(front, YELLOW, on=["hips", "middle", "neck", "head", "muzzle"])

    # The big red nose, the grin, the spiky yellow beard under it
    k.add("nose", ell(MC + (0, -MR[1] * 0.95, MR[2] * 0.5), (0.026, 0.021, 0.022)), NOSE, "head", fuse=False,
          surface="skin")
    y_.lay_grin(k, MC + (0, -MR[1] * 0.96, -0.014), unit(0, -1, -0.15), (0.048, 0.018), y_.grin_look())
    for i, (dx, ln) in enumerate(((0.0, 0.04), (0.02, 0.032), (-0.02, 0.032), (0.036, 0.024), (-0.036, 0.024))):
        base = MC + np.array([dx, -MR[1] * 0.55, -MR[2] * 0.9])
        k.add(f"beard{i}", y_.spike(base, (dx * 6, -0.4, -1), ln, 0.012, 0.008), YELLOW, "head", fuse=False,
              surface="fur")

    k.build(voxel=0.0055, smooth=4, band=0.004, wband=0.04)

    # The heavy-lidded eyes just under the hair's rim
    de = unit(0.42, -0.88, -0.1)
    E = HC + de / np.linalg.norm(de / HR) + np.array([0, -0.004, 0.0])
    look = heavy_eye()
    def eyes(state):
        f = eye_look(state, look, srgb(LID), srgb(INK), line=0.17)
        for side in (1, -1):
            col = f if side == 1 else (lambda a, b, g=f: g(-a, b))
            k.decal(E * (side, 1, 1), de * (side, 1, 1), (0.022, 0.018), col, eps=0.002, rings=20, segs=56)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    gumshoos(*args())
