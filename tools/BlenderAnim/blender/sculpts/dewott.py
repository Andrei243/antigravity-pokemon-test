# Dewott sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/dewott.py -- <the export's Dewott.glb> <out.glb>
#
# Grown from oshawott.py (sibling("oshawott")): its eye, nose, foot, paddle tail, ear and ribbed scalchop. From the
# references (they overrule the task's "black skirt": the skirt is navy blue; the ears, hands, feet and tail are the
# dark grey): a taller otter standing with its feet apart; a round light blue head with a pointed tuft rising from its
# crown and leaning back; dark grey rounded ears on its upper corners; black eyes cut at the top by a brow sloping down
# to the nose; a small pink-red nose and a frown; two white whiskers out of each cheek, spreading like a V; a slim light
# blue body; arms forward with dark grey three-fingered hands; a navy skirt of fur from the waist to the knees, its hem
# jagged into points; a scalchop on each thigh over the skirt; short blue shins, dark grey feet with toes; a dark grey
# paddle tail behind and out to one side.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, hull, sheet, srgb, inside, smoothstep, unit, tri, args, sibling

o = sibling("oshawott")
BLUE, SKIRT, GREY, NOSE, WHITE, INK = "#5EC4DF", "#2F5AA6", "#40424C", "#E26A78", "#F6F7F9", o.INK


def eye(u, v):
    """Oshawott's black eye with its glint, cut at the top by the brow's line sloping down to the nose."""
    c = o.eye(u, v * 1.1 + 0.08)
    edge = 0.3 + 0.42 * u
    c[v > edge] = srgb(BLUE)
    return c


def dewott(src, out):
    k = Kit(src, coat="fur")
    H = np.array([0, -0.012, 0.425])          # the head's centre
    HR = np.array([0.1, 0.092, 0.094])
    k.add("head", ell(H, HR), BLUE, "head")
    k.add("cheeks", ell(H + (0, -0.012, -0.03), (0.096, 0.078, 0.06)), BLUE, "head")
    # the tuft: a flame of fur from the crown, leaning back, with a small second point at its front
    k.add("tuft", blade([(0, 0.0, 0.5), (0.004, 0.03, 0.545), (0.008, 0.065, 0.575), (0.012, 0.095, 0.595)],
                        [0.045, 0.033, 0.018, 0.004], (1, 0, 0), 0.5), BLUE, "head")
    k.add("tuft2", blade([(0.0, -0.01, 0.505), (0.022, 0.0, 0.54), (0.038, 0.015, 0.555)], [0.022, 0.013, 0.003],
                         (0.3, 1, 0), 0.5), BLUE, "head")

    # A slim body widening a little to the hips under the skirt
    k.add("body", ell((0, 0.0, 0.31), (0.066, 0.056, 0.095)), BLUE, "body")
    k.add("hips", ell((0, 0.004, 0.22), (0.075, 0.062, 0.06)), BLUE, "body")
    # legs: thighs under the skirt, short thick shins showing below it, dark grey feet set wide
    k.add("thigh", ell((0.062, 0.0, 0.16), (0.055, 0.056, 0.075)), BLUE, "leg{s}", mirror=True)
    k.add("shin", tube([(0.068, 0.0, 0.13), (0.078, -0.02, 0.08), (0.082, -0.01, 0.04)], [0.05, 0.047, 0.04]), BLUE, "leg{s}", mirror=True)
    o.foot(k, (0.086, -0.032, 0.017), (0.034, 0.052, 0.018), GREY, "leg{s}", yaw=-14)
    k.paint(lambda P: inside(P[:, 2] - 0.038, 0.003) * (np.abs(P[:, 0]) > 0.04), GREY, on=["shin", "foot"])

    # Arms forward and down, the hands before the hips a little out from the shells, dark grey hands of three fingers
    k.add("arm", tube([(0.06, -0.01, 0.375), (0.085, -0.06, 0.32), (0.105, -0.1, 0.275)], [0.03, 0.026, 0.023]), BLUE,
          "arm{s}", mirror=True)
    HC = np.array([0.11, -0.115, 0.262])
    k.add("hand", ell(HC, (0.026, 0.025, 0.024)), GREY, "arm{s}", mirror=True)
    for i, (dx, dz) in enumerate([(-0.012, 0.012), (0.002, -0.004), (0.016, -0.014)]):
        b0 = HC + (dx, -0.012, dz)
        k.add(f"finger{i}", tube([b0, b0 + (0.004, -0.022, -0.004)], [0.011, 0.009]), GREY, "arm{s}", mirror=True)
    def gloves(P):
        q = np.stack([np.abs(P[:, 0]) - HC[0], P[:, 1] - HC[1] + 0.01, P[:, 2] - HC[2]], 1)
        return inside(np.linalg.norm(q, axis=1) - 0.034, 0.003)
    k.paint(gloves, GREY, on=["arm", "hand", "finger"])

    # Dark grey ears on the head's upper corners
    for sx in (1, -1):
        o.ear(k, H, HR, unit(0.7 * sx, 0.08, 0.68), (0.027, 0.018, 0.028), GREY, f"ear{'RL'[sx < 0]}", lean=38, sx=sx)

    # The dark grey paddle tail behind, out to +X, low
    k.add("tailroot", tube([(0, 0.05, 0.19), (0.035, 0.1, 0.14)], [0.034, 0.028]), GREY, "tail")
    o.paddle(k, "tail", (0.035, 0.1, 0.13), (0.57, 0.8, -0.05), (0.042, 0.07, 0.016), GREY, "tail", roll=25)

    # The skirt: a cone of navy fur from the waist to the knees, its hem jagged into points, its seam behind
    def skirt(u, v):
        th = -math.pi / 2 + 2 * math.pi * (-0.012 + 1.024 * u)  # start behind (+Y), its ends overlapping
        jag = tri(u * 9 + 0.5)
        z = 0.285 - v * (0.15 + 0.022 * jag * smoothstep(0.55, 1.0, v))
        r = (0.068 + v * 0.06 + 0.012 * v * v) * (1 + 0.035 * smoothstep(0.93, 1.0, u))   # the end laps over the start
        return np.stack([r * np.cos(th) * 1.05, -r * np.sin(th) * 0.95, z], -1)
    V, F = sheet(skirt, 145, 18, 0.008)
    k.add("skirt", (V, F), SKIRT, "body", fuse=False, surface="fur")
    k.add("waist", ell((0, 0.002, 0.27), (0.077, 0.066, 0.03)), SKIRT, "body")

    # A scalchop on each thigh, over the skirt, hinge down
    for sx in (1, -1):
        n = unit(0.9 * sx, -0.42, 0.2)
        o.scalchop(k, f"shell{'RL'[sx < 0]}", (0.112 * sx, -0.048, 0.2), n, (0, 0, 1), 0.06, "body", bend=0.1,
                   thick=0.012)

    # Two white whiskers out of each cheek, spreading in a V, solid tapering blades
    for sx in (1, -1):
        root = np.array([0.078 * sx, -0.062, 0.405])
        for dz, lift in ((0.01, 0.045), (-0.008, -0.035)):
            tip = root + (0.09 * sx, 0.01, lift)
            w = 0.011
            pts = [root + (0, -0.008, dz + w), root + (0, -0.008, dz - w), root + (0, 0.008, dz + w),
                   root + (0, 0.008, dz - w), root + (-0.02 * sx, 0, dz), tip]
            k.add(f"whisker{'RL'[sx < 0]}{int(dz > 0)}", hull(pts), WHITE, "head", fuse=False, surface="fur")

    # The nose, the frown
    nd = unit(0, -1, -0.18)
    o.nose(k, o.at_surface(H, HR, nd), nd, (0.016, 0.01, 0.012), NOSE, "head")
    INKC, BL = srgb(INK), srgb(BLUE)
    def frown(u, v):
        c = np.tile(BL, (len(u), 1))
        arc = -0.55 + 0.95 * (1 - u * u)
        c[(np.abs(v - arc) < 0.36) & (np.abs(u) < 0.9)] = INKC
        return c
    md = unit(0, -0.9, -0.42)
    k.decal(o.at_surface(H, HR, md) + (0, 0, 0), md, (0.016, 0.009), frown, eps=0.003, rings=16, segs=56)

    k.build(voxel=0.0045, smooth=4, band=0.004)
    E = [(o.at_surface(H, HR, d), d, sx) for sx in (1, -1) for d in [unit(0.44 * sx, -0.88, 0.05)]]
    k.eye_states(lambda st: o.eyes(k, E, (0.018, 0.024), st, BLUE, open_look=eye))
    k.export(out)


if __name__ == "__main__":
    dewott(*args())
