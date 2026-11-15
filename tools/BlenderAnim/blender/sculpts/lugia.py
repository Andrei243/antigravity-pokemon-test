# Lugia sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/lugia.py -- <the export's Lugia.glb> <out.glb>
# From the references: a great white dragon-bird hovering upright, a heavy pear of a body with a big pale blue belly, a
# long thick neck carrying a smooth pointed head forward, the eyes in dark blue plates that sweep back past the head
# into points, the mouth open (red inside); great wings like arms raised in a V and swept back, each ending in a hand of
# five broad rounded finger feathers; a row of dark blue plates down the back, a long tapering tail with two dark blue
# fins at its end, and short heavy legs, thighs fused into the body, three blunt toes a foot.
# The export's joints: body (0, 0, 0.381), head (0, -0.04, 0.521), wings (+-0.09, 0, 0.481), tail (0, 0.08, 0.241),
# legs (+-0.06, -0.03, 0.261).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, slab, srgb, inside, unit, eye_look, sibling, args

a = sibling("articuno")          # the wing's axes and the round-ended feather outline
sp = a.sp                        # the solid kite of a spike

WHITE, SHADE, BELLY, NAVY = "#F2F3FA", "#DDE2F0", "#6A9CE0", "#24398E"
MOUTH, THROAT, EYEW, PUPIL = "#E46E72", "#9A2C3C", "#FBFBFF", "#121428"

H = np.array([0.0, -0.18, 0.79])           # the head's centre, carried forward and up on the long neck
HR = np.array([0.08, 0.102, 0.082])


def taper_back(V):
    """An ellipsoid drawn to a point toward +y (the back of the head)."""
    V = V.copy()
    t = np.clip(V[:, 1], 0, 1)
    V[:, 0] *= 1 - 0.75 * t ** 1.4
    V[:, 2] *= 1 - 0.7 * t ** 1.4
    return V


def snout_taper(V):
    """Narrowing toward -y (the snout's tip)."""
    V = V.copy()
    t = np.clip(-V[:, 1], 0, 1)
    V[:, 0] *= 1 - 0.45 * t
    V[:, 2] *= 1 - 0.3 * t
    return V


# --- the wings: a thick arm and a hand of five broad finger feathers -----------------------------------------------------
ELEV, SWEEP = 40, 26
ROOT = np.array([0.11, 0.02, 0.53])
ARM = 0.25
FINGERS = [(-6, 0.17, 0.064), (18, 0.185, 0.066), (42, 0.18, 0.064), (66, 0.165, 0.06), (90, 0.14, 0.056)]


def wings(k):
    ex, ez = a.wing_axes(ELEV, SWEEP, (0.25, 0.55, -0.8))
    n = np.cross(ex, ez)
    wrist = ROOT + ex * ARM
    # the arm: thick, fused into the shoulder
    k.add("arm", tube([ROOT - ex * 0.03, ROOT + ex * ARM * 0.5, wrist], [0.07, 0.058, 0.05]), WHITE, "wing{s}",
          mirror=True)
    k.add("shoulder", ell(ROOT + ex * 0.01, (0.07, 0.075, 0.075)), WHITE, "wing{s}", mirror=True)
    # the palm, a thick rounded plate about the wrist
    palm = [(0.11 * math.cos(t), 0.075 * math.sin(t) + 0.03) for t in np.linspace(0, 2 * math.pi, 18, endpoint=False)]
    k.add("palm", slab(palm, wrist - ex * 0.02, ex, ez, 0.042), WHITE, "wing{s}", fuse=False, mirror=True)
    # five broad round-ended fingers fanned from along the span (up and out) round to the trailing edge
    for j, (ang, L, w) in enumerate(FINGERS):
        off = wrist + n * (0.005 if j % 2 else -0.005)
        k.add(f"finger{j}", slab(a.feather_outline(0.02, 0.02, 90 - ang, L, w, seg=11), off, ex, ez, 0.034),
              WHITE if j % 2 else SHADE, "wing{s}", fuse=False, mirror=True)
    # a fold of skin along the arm's trailing edge, thick
    fold = [(-0.02, 0.0), (ARM * 0.55, 0.0), (ARM, 0.03), (ARM * 0.7, 0.09), (0.05, 0.1)]
    k.add("fold", slab(fold, ROOT, ex, ez, 0.034), WHITE, "wing{s}", fuse=False, mirror=True)


def lugia(src, out):
    k = Kit(src, coat="fur")

    # The body: a heavy pear, widest low, leaning back a little; the chest narrower where the neck rises
    k.add("body", ell((0, 0.02, 0.38), (0.155, 0.16, 0.19), rot=(-8, 0, 0)), WHITE, "body")
    k.add("chest", ell((0, -0.02, 0.52), (0.115, 0.11, 0.1)), WHITE, "body")
    # the long thick neck, out forward and up, under the head
    k.add("neck", tube([(0, -0.02, 0.55), (0, -0.06, 0.64), (0, -0.1, 0.72), H + (0, 0.04, -0.035)],
                       [0.088, 0.072, 0.064, 0.06]), WHITE, "head")

    # The head: smooth, a long snout drawn forward, the skull drawn back to a point
    k.add("skull", ell(H, HR), WHITE, "head")
    k.add("snout", ell(H + (0, -0.085, -0.018), (0.058, 0.09, 0.052), deform=snout_taper), WHITE, "head")
    k.add("occiput", ell(H + (0, 0.07, 0.018), (0.05, 0.1, 0.048), deform=taper_back), WHITE, "head")

    # The dark blue plates over the eyes: a mask painted round each eye, and a solid plate on it sweeping back and up
    # past the head into a point
    de = unit(0.62, -0.62, 0.32)
    E = H + de * HR * np.array([1.0, 1.0, 1.0]) + (0, -0.005, 0)
    def mask(P):
        q = P - H
        m = np.zeros(len(P))
        for s in (1, -1):
            d = ((q[:, 0] - s * 0.05) / 0.045) ** 2 + ((q[:, 1] + 0.045) / 0.075) ** 2 + ((q[:, 2] - 0.025) / 0.038) ** 2
            m = np.maximum(m, inside(d - 1, 0.05) * (s * q[:, 0] > 0.018))
        return m * (q[:, 2] > -0.01)
    k.paint(mask, NAVY)
    for s, nm in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        pts = [H + np.array(p) * m for p in ((0.055, -0.065, 0.045), (0.07, -0.02, 0.05), (0.045, -0.045, 0.072),
                                             (0.03, 0.0, 0.07), (0.05, 0.03, 0.055), (0.035, 0.175, 0.05),
                                             (0.06, 0.02, 0.035), (0.035, 0.03, 0.04))]
        k.add(f"plate{nm}", hull(pts), NAVY, "head", fuse=False, surface="shell")

    # The mouth, open: a red wedge from the snout's tip back along each side, a dark throat at its corner
    W, M, T = srgb(WHITE), srgb(MOUTH), srgb(THROAT)
    def mouth(u, v):
        c = np.tile(W, (len(u), 1))
        t = (u + 1) / 2                      # 0 at the tip (front), 1 at the corner
        half = 0.2 + 0.6 * t
        ok = (np.abs(v + 0.1) < half) & (u > -0.92) & (u < 0.85)
        c[ok] = M
        c[ok & (u > 0.35) & (np.abs(v + 0.1) < half * 0.55)] = T
        return c
    MC = H + (0.046, -0.105, -0.043)
    k.decal(MC, unit(1, -0.25, -0.35), (0.062, 0.032), mouth, up=unit(0, 0, 1), eps=0.004, rings=12, segs=56,
            mirror=True)
    k.decal(H + (0, -0.172, -0.045), unit(0, -1, -0.15), (0.03, 0.016), MOUTH, eps=0.004, rings=8, segs=40)

    # The belly: a great pale blue oval from the chest to between the legs
    k.decal((0, -0.13, 0.36), unit(0, -1, 0.05), (0.125, 0.165), BELLY, eps=0.003, rings=26, segs=80)

    # The plates down the back: dark blue, solid, laid back
    for j, (y, z, L, w) in enumerate([(0.09, 0.56, 0.1, 0.07), (0.13, 0.49, 0.12, 0.08), (0.155, 0.42, 0.12, 0.08),
                                      (0.165, 0.35, 0.11, 0.075), (0.155, 0.28, 0.1, 0.07)]):
        base = np.array([0, y, z])
        d = unit(0, 0.85, 0.55 - 0.1 * j)
        k.add(f"back{j}", sp.spike(base, base + d * L, w, 0.022, up=(1, 0, 0)), NAVY, "body" if j < 3 else "tail",
              fuse=False, surface="shell")

    wings(k)

    # The tail: long and tapering, out back from the rump and a little down, two dark fins at its end
    TAIL = [(0, 0.1, 0.27), (0, 0.26, 0.2), (0, 0.42, 0.17), (0, 0.56, 0.2)]
    k.add("tail", tube(TAIL, [0.1, 0.07, 0.045, 0.022]), WHITE, "tail")
    for j, (y, z, L, w) in enumerate([(0.36, 0.2, 0.065, 0.045), (0.45, 0.19, 0.06, 0.04)]):
        base = np.array([0, y, z])
        k.add(f"tailspine{j}", sp.spike(base, base + unit(0, 0.6, 0.8) * L, w, 0.02, up=(1, 0, 0)), NAVY, "tail",
              fuse=False, surface="shell")
    for s, nm in ((1, "R"), (-1, "L")):
        base = np.array([0.0, 0.53, 0.2])
        k.add(f"fin{nm}", sp.spike(base, base + unit(0.5 * s, 0.6, 0.62) * 0.14, 0.045, 0.02, up=(0, 1, 0)), NAVY,
              "tail", fuse=False, surface="shell")

    # The legs: heavy thighs fused into the lower body, short shins, broad feet with three blunt toes
    k.add("thigh", ell((0.09, -0.02, 0.22), (0.08, 0.09, 0.1), rot=(-12, 0, 0)), WHITE, "leg{s}", mirror=True)
    k.add("shin", tube([(0.1, -0.04, 0.15), (0.105, -0.05, 0.06)], [0.058, 0.048]), WHITE, "leg{s}", mirror=True)
    k.add("foot", ell((0.105, -0.08, 0.03), (0.055, 0.07, 0.032)), WHITE, "leg{s}", mirror=True)
    for j, dx in enumerate((-0.03, 0.0, 0.03)):
        k.add(f"toe{j}", ell((0.105 + dx * 1.1, -0.14, 0.024), (0.02, 0.032, 0.022)), SHADE if j == 1 else WHITE,
              "leg{s}", mirror=True)

    # The eyes: small and dark in the blue plates, a glint up and out
    EW, EP, NV = srgb(EYEW), srgb(PUPIL), srgb(NAVY)
    def eye(u, v):
        c = np.tile(EP, (len(u), 1))
        c[(u > 0.35) & (u * 0.6 + np.abs(v) < 0.85)] = EW
        c[((u - 0.05) / 0.22) ** 2 + ((v - 0.3) / 0.22) ** 2 < 1] = EW
        return c
    def eyes(state):
        k.decal(E, de, (0.025, 0.018), eye_look(state, eye, NV, srgb("#C8D4F2"), line=0.18), eps=0.005, rings=20,
                segs=56, mirror=True)

    k.build(voxel=0.0072, smooth=4)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    lugia(*args())
