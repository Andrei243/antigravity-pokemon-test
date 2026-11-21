# Sliggoo-Hisui sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/sliggoohisui.py -- <the export's Sliggoo-Hisui.glb> <out.glb>
#
# Sliggoo grown (sibling("goomy"), sibling("sliggoo")): the same pale lilac neck and head on a purple foot, the same
# soft horns and long feelers ending in round knobs, the same way of laying eyes, spots and the open smile.
# From the references (drawn from the front three-quarter): a great round puck of pale steel lying on the ground
# behind it, wider than the slug is tall, the new largest mass, with a spiral line drawn on its top round a dimple;
# the slug rises from the shell's front opening: a short purple foot with two little nubs pointing forward, a short
# pale neck, a teardrop head wider at the cheeks than at its crown; two short pointed horns on the crown, two long
# feelers from behind them rising up and out to each side, ending in round knobs; sleepy teal eyes, their tops cut
# flat by heavy lids; one large dark teal spot on each cheek, low and behind the eye; an open smile whose upper lip
# dips in the middle and a pink tongue hanging out.
# Where the pictures overrule the task's text: the shell is no hard-edged spiral piece but a smooth rounded puck with
# a spiral DRAWN on its top; the slug's colours are the base's lilac and violet, pinker, not a darker violet-grey;
# the cheek has one large dark spot, not the base's row of three green ones.
# Skeleton: body (0, 0, 0.12), head (0, -0.03, 0.24), ears (+-0.02, -0.04, 0.32), as Sliggoo's.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, args, boxy, sibling

g = sibling("goomy")
PALE, PURPLE, LINE, INK = "#E0CCEA", "#9C84D2", "#5E4A8C", g.INK
SHELL_C, SPIRAL, DIMPLE = "#CDD0D8", "#6A6F7E", "#9599A6"
SPOT, SPOT_RIM = "#4E7C70", "#27443C"
EYE, EYE_RIM, PUPIL, EYE_LIGHT = "#74C2AC", "#1E3833", "#2F6052", "#D6F2E8"
MOUTH, TONGUE = "#4A2440", "#F07A92"

HEAD, HEADR = np.array([0.0, -0.068, 0.266]), np.array([0.08, 0.072, 0.08])
JAW, JAWR = np.array([0.0, -0.077, 0.228]), np.array([0.09, 0.074, 0.056])
CROWN, CROWNR = np.array([0.0, -0.06, 0.312]), np.array([0.05, 0.05, 0.042])
SHELL, SHELLR = np.array([0.0, 0.1, 0.074]), np.array([0.152, 0.146, 0.074])


def spiral_look(turns=1.7, width=0.07):
    """A spiral line drawn out from a dimple in the middle of a round patch, on the shell's own colour."""
    P, S, D = srgb(SHELL_C), srgb(SPIRAL), srgb(DIMPLE)
    def look(u, v):
        c = np.tile(P, (len(u), 1))
        rr = np.sqrt(u * u + v * v)
        th = (np.arctan2(v, u) / (2 * math.pi)) % 1
        f = (rr * turns - th) % 1
        d = np.minimum(f, 1 - f) / turns
        c[(d < width) & (rr < 0.9) & (rr > 0.16)] = S
        c[rr < 0.16] = D
        return c
    return look


def mouth_look():
    """An open smile whose upper lip dips in the middle, a pink tongue hanging out below it."""
    P, M, T = srgb(PALE), srgb(MOUTH), srgb(TONGUE)
    def look(u, v):
        c = np.tile(P, (len(u), 1))
        hi = 0.3 + 0.45 * u * u - 0.22 * np.exp(-(u / 0.22) ** 2)
        lo = -0.3 + 0.55 * u * u
        m = (v < hi) & (v > lo) & (np.abs(u) < 0.94)
        c[m] = M
        t = (((u - 0.06) / 0.4) ** 2 + ((v + 0.42) / 0.5) ** 2) < 1
        c[t & (v < hi - 0.12)] = T
        return c
    return look


def eye_open(u, v):
    """A sleepy teal eye: a dark rim, a darker teal pupil, a glint; its top cut flat by the lid."""
    c = np.tile(srgb(EYE), (len(u), 1))
    c[((u + 0.04) / 0.36) ** 2 + ((v + 0.12) / 0.5) ** 2 < 1] = srgb(PUPIL)
    c[((u - 0.22) / 0.2) ** 2 + ((v - 0.05) / 0.18) ** 2 < 1] = srgb(EYE_LIGHT)
    c[u * u + v * v > 0.78 ** 2] = srgb(EYE_RIM)
    lid = 0.4 - 0.16 * u
    c[v > lid] = srgb(PALE)
    c[np.abs(v - lid) < 0.15] = srgb(EYE_RIM)
    return c


def sliggoo_hisui(src, out):
    k = Kit(src, coat="scales")

    # The shell: a great puck of pale steel behind, lying on the ground
    k.add("shell", ell(SHELL, SHELLR, deform=boxy(2.35)), SHELL_C, "body", surface="metal")

    # The foot rising out of the shell's front, two nubs forward, the neck and the teardrop head
    k.add("base", ell((0.0, -0.062, 0.05), (0.068, 0.058, 0.05)), PURPLE, "body")
    k.add("nub", tube([(0.032, -0.09, 0.034), (0.045, -0.125, 0.024), (0.05, -0.145, 0.018)],
                      [0.022, 0.018, 0.016]), PURPLE, "body", mirror=True)
    k.add("neck", tube([(0.0, -0.058, 0.06), (0.0, -0.062, 0.14), (0.0, -0.066, 0.2)], [0.052, 0.044, 0.05]),
          PALE, "head")
    k.add("head", ell(HEAD, HEADR), PALE, "head")
    k.add("jaw", ell(JAW, JAWR), PALE, "head")
    k.add("crown", ell(CROWN, CROWNR), PALE, "head")

    # The violet of the foot, a wavy dark line along its top
    foot = ["base", "nub", "neck"]
    k.paint(lambda P: g.wave(0.1, 0.012)(P) * (P[:, 1] < 0.0), PURPLE, on=foot)
    k.paint(lambda P: g.wave(0.1, 0.012, line=0.0022)(P) * (P[:, 2] > 0.06) * (P[:, 1] < 0.0), LINE, on=foot)

    # The spiral drawn on the shell's top round its dimple
    k.decal((0.0, 0.12, SHELL[2] + SHELLR[2]), (0, 0, 1), (0.1, 0.09), spiral_look(), eps=0.002, rings=40, segs=160,
            up=(0, 1, 0))

    # The two short horns on the crown, the two long feelers from behind them, up and out
    for s in (1, -1):
        m = np.array([s, 1, 1.0]); nm = "R" if s > 0 else "L"
        k.add(f"horn{nm}", g.feeler(np.array([(0.026, -0.08, 0.33), (0.033, -0.085, 0.365), (0.038, -0.087, 0.39)])
                                    * m, [0.014, 0.011, 0.0075], bulb=1.0), PALE, "head", fuse=False)
        k.add(f"feeler{nm}", g.feeler(np.array([(0.02, -0.045, 0.33), (0.05, -0.03, 0.385), (0.1, 0.0, 0.415),
                                                (0.15, 0.035, 0.405), (0.182, 0.06, 0.382)]) * m,
                                      [0.0155, 0.014, 0.0128, 0.0122, 0.0122], bulb=1.4), PALE, f"ear{nm}", fuse=False)

    # One large dark teal spot low on each cheek, behind the eye
    for s in (1, -1):
        p, n = g.on(JAW, JAWR, (0.82 * s, -0.42, 0.12))
        k.decal(p, n, (0.022, 0.026), g.spot_look(SPOT, SPOT_RIM, 0.74), eps=0.0018, rings=14, segs=48)

    # The open smile with its tongue out
    p, n = g.on(JAW, JAWR, (0, -0.9, -0.2))
    k.decal(p, unit(0, -1, -0.1), (0.044, 0.03), mouth_look(), eps=0.002, rings=24, segs=72)

    k.build(voxel=0.0036, smooth=4, band=0.004, wband=0.04)

    CP = srgb(PALE)

    def eyes(state):
        for s in (1, -1):
            p, n = g.on(HEAD, HEADR, (0.46 * s, -0.82, 0.24))
            k.decal(p, n, (0.021, 0.022), g.mirrored(eye_look(state, eye_open, CP, srgb(INK), line=0.18), s),
                    eps=0.002, rings=20, segs=56)

    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    sliggoo_hisui(*args())
