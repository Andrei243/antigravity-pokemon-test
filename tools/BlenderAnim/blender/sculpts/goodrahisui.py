# Goodra-Hisui sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/goodrahisui.py -- <the export's Goodra-Hisui.glb> <out.glb>
#
# Goodra and Sliggoo-Hisui grown (sibling("goomy"), sibling("goodra"), sibling("sliggoohisui")): Goodra's lilac body,
# stubby legs, forward arms with their drips, its feelers and short horns; Sliggoo-Hisui's pale steel shell with its
# drawn spiral and its sleepy lidded eyes.
# From the references (drawn from the front three-quarter): a great ball of pale steel on its back, the largest mass by
# far (some 0.65 of the height across), resting on the tail, which runs back under it and curls up its back, lilac on
# top and violet beneath with two dark slate ovals on each side; a spiral drawn on each side of the ball; the body
# leans forward from it on short thick legs, violet below; a long thick neck carries a long head low and forward, a
# rounded snout with a smug little smile; heavy-lidded grey-blue eyes with dark pupils; two dark slate ovals on the
# side of the head behind each eye; two short horns on the crown; two thick feelers rising from the crown, arching back
# over the top of the ball and curling at their ends against it; short arms forward and down, icy steel-blue drips
# hanging from the hands and the corners of the jaw.
# Where the pictures overrule the task's text: the spots are dark slate ovals, not green dots; the drips are icy
# steel-blue, not green; the feelers lie back over the shell and curl there (the pictures draw the two unlike, from
# the three-quarter: they are built as a mirror pair).
# Skeleton: body (0, 0, 0.28), head (0, -0.06, 0.56), ears (+-0.03, -0.06, 0.65), arms (+-0.1, -0.04, 0.36),
# legs (+-0.07, 0, 0.15), tail (0, 0.08, 0.18), as Goodra's.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, outward, srgb, inside, unit, eye_look, args, sibling

g = sibling("goomy")
gd = sibling("goodra")
LAVENDER, PURPLE, LINE, INK = "#DCC8E8", "#9C84D2", g.LINE, g.INK
FOOT, DRIP = "#A894C6", "#A9BDD4"
SHELL_C, SPIRAL, DIMPLE = "#CED2DB", "#7C8293", "#A3A8B4"
SPOT, SPOT_RIM = "#56637C", "#2C3446"
EYE, EYE_RIM, PUPIL, EYE_LIGHT = "#8EA2BC", "#1E2230", "#2A3040", "#F2F6FA"

HEAD, HEADR = np.array([0.0, -0.255, 0.54]), np.array([0.1, 0.11, 0.094])
SNOUT, SNOUTR = HEAD + (0, -0.1, -0.03), np.array([0.078, 0.082, 0.065])
SHELL, SHELLR = np.array([0.0, 0.17, 0.49]), np.array([0.235, 0.255, 0.255])
TAIL = [(0.0, 0.06, 0.17), (0.0, 0.2, 0.125), (0.0, 0.33, 0.15), (0.0, 0.42, 0.25), (0.0, 0.43, 0.35)]
TAILR = [0.125, 0.115, 0.1, 0.08, 0.062]


def drip(k, name, top, length, r, bone):
    """An icy drip: a short neck swelling to a drop (loose)."""
    top = np.array(top, float)
    k.add(name, outward(tube([top, top + (0, -0.002, -length * 0.5), top + (0, -0.004, -length)],
                             [r * 0.9, r * 0.8, r * 1.2], seg=14)), DRIP, bone, fuse=False, surface="metal")


def spiral_look(turns=1.6, width=0.075):
    P, S, D = srgb(SHELL_C), srgb(SPIRAL), srgb(DIMPLE)
    def look(u, v):
        c = np.tile(P, (len(u), 1))
        rr = np.sqrt(u * u + v * v)
        th = (np.arctan2(v, u) / (2 * math.pi)) % 1
        f = (rr * turns - th) % 1
        d = np.minimum(f, 1 - f) / turns
        c[(d < width) & (rr < 0.9) & (rr > 0.14)] = S
        c[rr < 0.14] = D
        return c
    return look


def eye_open(u, v):
    """A heavy-lidded grey-blue eye: a dark pupil and a glint, its top cut by the lid sloping down outward."""
    c = np.tile(srgb(EYE), (len(u), 1))
    c[((u + 0.08) / 0.38) ** 2 + ((v + 0.15) / 0.55) ** 2 < 1] = srgb(PUPIL)
    c[((u - 0.2) / 0.2) ** 2 + ((v - 0.02) / 0.18) ** 2 < 1] = srgb(EYE_LIGHT)
    c[u * u + v * v > 0.8 ** 2] = srgb(EYE_RIM)
    lid = 0.32 - 0.18 * u
    c[v > lid] = srgb(LAVENDER)
    c[np.abs(v - lid) < 0.15] = srgb(EYE_RIM)
    return c


def smirk(u, v):
    c = np.tile(srgb(LAVENDER), (len(u), 1))
    c[(np.abs(v - (0.55 * u * u - 0.22 + 0.12 * u)) < 0.2) & (np.abs(u) < 0.95)] = srgb(LINE)
    return c


def goodra_hisui(src, out):
    k = Kit(src, coat="scales")

    # The shell: a great ball of pale steel on the back
    k.add("shell", ell(SHELL, SHELLR), SHELL_C, "body", surface="metal")

    # The body leaning forward from the hips, a long thick neck, the head low and forward
    k.add("hips", ell((0, 0.02, 0.2), (0.135, 0.135, 0.135)), LAVENDER, "body")
    k.add("chest", ell((0, -0.085, 0.34), (0.105, 0.1, 0.11), rot=(-25, 0, 0)), LAVENDER, "body")
    k.add("neck", tube([(0, -0.11, 0.38), (0, -0.17, 0.45), (0, -0.215, 0.5)], [0.075, 0.062, 0.058]), LAVENDER,
          "head")
    k.add("head", ell(HEAD, HEADR), LAVENDER, "head")
    k.add("snout", ell(SNOUT, SNOUTR), LAVENDER, "head")

    # The tail under the shell, curling up its back
    k.add("tail", tube(TAIL, TAILR), LAVENDER, "tail")

    # Short thick legs: thighs fused into the hips, a stub and a foot
    k.add("thigh", ell((0.078, -0.015, 0.12), (0.08, 0.09, 0.105)), LAVENDER, "leg{s}", mirror=True)
    k.add("foot", ell((0.088, -0.06, 0.032), (0.058, 0.082, 0.034)), FOOT, "leg{s}", mirror=True)

    # The arms, forward and down, elbows bent, flat mitts
    k.add("arm", tube([(0.085, -0.1, 0.37), (0.118, -0.17, 0.35), (0.11, -0.235, 0.315)], [0.042, 0.035, 0.03]),
          LAVENDER, "arm{s}", mirror=True)
    k.add("mitt", ell((0.105, -0.265, 0.3), (0.042, 0.04, 0.022), rot=(-22, 0, 8)), LAVENDER, "arm{s}", mirror=True)
    for s in (1, -1):
        nm = "R" if s > 0 else "L"
        for j, (x, y, ln) in enumerate(((0.085, -0.275, 0.055), (0.118, -0.27, 0.045), (0.1, -0.24, 0.035))):
            drip(k, f"handdrip{nm}{j}", (x * s, y, 0.295), ln, 0.012, f"arm{nm}")
        drip(k, f"chindrip{nm}", (0.058 * s, -0.37, 0.465), 0.06, 0.013, "head")

    # Two short horns on the crown, two thick feelers back over the shell, curling against it
    for s in (1, -1):
        m = np.array([s, 1, 1.0]); nm = "R" if s > 0 else "L"
        k.add(f"horn{nm}", g.feeler(np.array([(0.045, -0.31, 0.61), (0.052, -0.318, 0.655), (0.056, -0.32, 0.68)])
                                    * m, [0.018, 0.013, 0.0085], bulb=1.0), LAVENDER, "head", fuse=False)
        k.add(f"feeler{nm}", g.feeler(np.array([(0.035, -0.245, 0.62), (0.05, -0.2, 0.685), (0.08, -0.11, 0.725),
                                                (0.12, 0.0, 0.75), (0.16, 0.1, 0.73), (0.2, 0.18, 0.675),
                                                (0.225, 0.21, 0.615), (0.215, 0.18, 0.575), (0.2, 0.16, 0.6)]) * m,
                                      [0.033, 0.031, 0.029, 0.026, 0.023, 0.02, 0.018, 0.016, 0.015], bulb=1.15),
              LAVENDER, f"ear{nm}", fuse=False)

    # The violet: the belly and thighs below, the tail beneath, the feet
    body = ["hips", "chest", "thigh", "tail"]
    k.paint(lambda P: inside(P[:, 2] - 0.1, 0.004) * (P[:, 1] < 0.12), PURPLE, on=body)
    k.paint(lambda P: inside(np.abs(P[:, 0]) - (0.075 - 0.12 * np.clip(P[:, 2] - 0.2, 0, 1)), 0.003)
            * (P[:, 1] < -0.04) * (P[:, 2] < 0.32), PURPLE, on=["hips", "chest"])
    k.paint(gd.under(TAIL, TAILR, share=0.3, start=8), PURPLE, on=["tail"])

    # Two dark slate ovals on each side of the tail, under the shell
    for s in (1, -1):
        for t in (1.25, 1.75):
            i = int(t); f = t - i
            c = np.array(TAIL[i]) + (np.array(TAIL[i + 1]) - np.array(TAIL[i])) * f
            r = TAILR[i] + (TAILR[i + 1] - TAILR[i]) * f
            n = unit(s, 0, -0.15)
            k.decal(c + n * r * 0.95, n, (0.024, 0.034), g.spot_look(SPOT, SPOT_RIM, 0.74), eps=0.002, rings=12,
                    segs=48)

    # The spiral drawn on each side of the shell
    for s in (1, -1):
        p, n = g.on(SHELL, SHELLR, (s, 0.05, 0.05))
        k.decal(p, n, (0.16, 0.16), spiral_look(), eps=0.002, rings=40, segs=160, up=(0, 0, 1))

    # Two dark ovals on the side of the head behind each eye, the smirk on the snout
    for s in (1, -1):
        for d, h in (((0.88, 0.3, 0.2), (0.016, 0.022)), ((0.86, 0.45, -0.22), (0.015, 0.021))):
            p, n = g.on(HEAD, HEADR, (d[0] * s, d[1], d[2]))
            k.decal(p, n, h, g.spot_look(SPOT, SPOT_RIM, 0.7), eps=0.0018, rings=12, segs=48)
    p, n = g.on(SNOUT, SNOUTR, (0, -0.85, -0.45))
    k.decal(p, unit(0, -0.9, -0.4), (0.042, 0.018), smirk, eps=0.002, rings=10, segs=48)

    k.build(voxel=0.0065, smooth=4, band=0.006, wband=0.05)

    CL = srgb(LAVENDER)

    def eyes(state):
        for s in (1, -1):
            p, n = g.on(HEAD, HEADR, (0.55 * s, -0.76, 0.2))
            k.decal(p, n, (0.025, 0.024), g.mirrored(eye_look(state, eye_open, CL, srgb(INK), line=0.17), s),
                    eps=0.003, rings=20, segs=56)

    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    goodra_hisui(*args())
