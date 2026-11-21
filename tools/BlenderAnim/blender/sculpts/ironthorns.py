# Iron Thorns sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/ironthorns.py -- <the export's Iron Thorns.glb> <out.glb>
#
# A machine Tyranitar, grown from tyranitar.py's frame (larvitar.py's spikes, claws and eyes under a brow through
# sibling("larvitar")). From the references: lime green armour, the head and trunk one great cone, narrow at the top
# where a small blocky head sits forward with a crest swept up and back off the crown and a point back off each cheek,
# its jaws a dark open maw, small cyan eyes under a scowling brow; the cone swelling to the hips, set with glowing
# mint-green triangular windows (one high on each side of the chest, one lower on each flank, a large one pointing
# down over the belly); the lower belly and crotch dark grey; great round thighs bulging out of the hips with a window
# each, short thick shins, dark grey feet with three dark claws; short arms (the pictures hold them out to the sides:
# here forward and down) with a dark grey elbow, a blocky hand of dark claws and two black spikes on the forearm; a row
# of glowing translucent green plates down the back (here two rows splayed apart, as the game's own sculpt has them, so
# both cameras see their faces); a thick tail sweeping back and out to +X, banded by glowing green rings and ending in
# a star of green points round a dark one.
# Bones: body (0, 0, 0.48), arm{s} (0.16, -0.02, 0.62), tail (0, 0.12, 0.34), head (0, -0.04, 0.68),
# jaw (0, -0.07, 0.73), leg{s} (0.11, 0, 0.32).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, ring_band, outward, srgb, inside, unit, polygon, eye_look, args, sibling

lv = sibling("larvitar")

GREEN, DARK, CLAW, MAW = "#9CCB3A", "#3C3A40", "#38363C", "#2A262C"
WINDOW, SPECK = "#5CE6A0", "#E6FFF0"
GLOW = "#62F0A4"
EYE_CYAN = srgb("#3CD6E8")

H = np.array([0.0, -0.085, 0.79])            # the head's middle
HR = np.array([0.082, 0.088, 0.07])


def cone(V):
    """The trunk: an egg swelling to the hips and drawn in toward its top, where the head sits."""
    V = V.copy()
    z = V[:, 2]
    s = 1 + 0.16 * np.clip(-z, 0, 1) - 0.55 * np.clip(z, 0, 1) ** 1.3
    V[:, 0] *= s
    V[:, 1] *= s
    V[:, 1] -= 0.18 * np.clip(z, 0, 1) ** 2         # the top carried forward
    return V


def trunk(k):
    k.add("torso", ell((0, 0.0, 0.47), (0.215, 0.19, 0.33), deform=cone, sub=5), GREEN, "body")
    k.add("hips", ell((0, 0.01, 0.3), (0.2, 0.18, 0.13)), GREEN, "body")
    # the dark lower belly and crotch, between the thighs and under the belly's window
    def briefs(P):
        x, y, z = P[:, 0], P[:, 1], P[:, 2]
        return inside(z - (0.33 - 0.25 * np.abs(x)), 0.004) * inside(y + 0.02, 0.01) * inside(np.abs(x) - 0.17, 0.004)
    k.paint(briefs, DARK, on=["torso", "hips"])


def head(k):
    k.add("head", ell(H, HR, deform=lambda V: V / (np.abs(V) ** 2.6).sum(1, keepdims=True) ** (1 / 2.6)), GREEN,
          "head")
    k.add("snout", ell(H + (0, -0.075, 0.0), (0.06, 0.055, 0.038), rot=(-8, 0, 0)), GREEN, "head")
    k.add("brow", ell(H + (0.04, -0.06, 0.03), (0.04, 0.05, 0.02), rot=(-12, 0, -12)), GREEN, "head", mirror=True)
    # the open maw under the snout: a dark mass set back between the snout and the cone's lip
    k.add("maw", ell(H + (0, -0.085, -0.052), (0.055, 0.05, 0.034), rot=(10, 0, 0)), MAW, "jaw")
    k.add("chin", ell(H + (0, -0.07, -0.095), (0.07, 0.05, 0.025)), GREEN, "jaw")
    for s in (1, -1):
        lv.spike(k, f"fang{'R' if s > 0 else 'L'}", H + (0.03 * s, -0.135, -0.035), (0, -0.1, -1), 0.03, 0.022,
                 0.016, (0, 1, 0), "#E8EEE0", "head", sink=0.01, surface="metal")
    # the crest up and back off the crown, a point back off each cheek
    lv.spike(k, "crest", H + (0, -0.01, 0.05), (0, 0.55, 0.84), 0.17, 0.075, 0.05, (1, 0, 0), GREEN, "head",
             sink=0.04, surface="metal")
    lv.spike(k, "cheek", H + (0.065, 0.0, 0.01), (0.55, 0.75, 0.38), 0.11, 0.055, 0.04, (0, 0, 1), GREEN, "head",
             sink=0.03, mirror=True, surface="metal")


def legs(k):
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"thigh{side}", ell(np.array((0.175, -0.005, 0.235)) * m, (0.13, 0.15, 0.15)), GREEN, f"leg{side}")
        k.add(f"shin{side}", tube(np.array([(0.19, -0.02, 0.15), (0.2, -0.035, 0.085)]) * m, [0.085, 0.075]), GREEN,
              f"leg{side}")
        k.add(f"foot{side}", ell(np.array((0.2, -0.07, 0.036)) * m, (0.095, 0.125, 0.04)), DARK, f"leg{side}")
        for j, dx in enumerate((-0.05, 0.0, 0.05)):
            lv.claw(k, f"toe{side}{j}", np.array((0.2 + dx, -0.175, 0.038)) * m, np.array((dx * 0.6, -1, -0.2)) * m,
                    0.05, 0.042, f"leg{side}", color=CLAW)
        lv.claw(k, f"heel{side}", np.array((0.2, 0.04, 0.038)) * m, np.array((0, 1, -0.2)), 0.04, 0.036,
                f"leg{side}", color=CLAW)
    # the shins' green meets the dark foot at a crisp line
    k.paint(lambda P: inside(P[:, 2] - 0.05, 0.003) * inside(np.abs(np.abs(P[:, 0]) - 0.2) - 0.1, 0.003), DARK,
            on=["shin", "foot"])


def arms(k):
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        EL, HA = np.array((0.235, -0.07, 0.565)), np.array((0.205, -0.2, 0.5))
        k.add(f"upper{side}", tube(np.array([(0.15, -0.02, 0.63), (0.205, -0.04, 0.6), EL]) * m, [0.075, 0.066, 0.058]),
              GREEN, f"arm{side}")
        k.add(f"elbow{side}", ell(EL * m, (0.055, 0.055, 0.055)), DARK, f"arm{side}")
        k.add(f"fore{side}", tube(np.array([EL + (0, -0.02, -0.005), HA]) * m, [0.062, 0.058]), GREEN, f"arm{side}")
        k.add(f"hand{side}", ell((HA + (0, -0.035, -0.01)) * m, (0.055, 0.05, 0.046), deform=lambda V:
                                  V / (np.abs(V) ** 3).sum(1, keepdims=True) ** (1 / 3)), DARK, f"arm{side}")
        for j, dx in enumerate((-0.026, 0.0, 0.026)):
            lv.claw(k, f"claw{side}{j}", (HA + (dx, -0.075, -0.02)) * m, np.array((dx, -1, -0.45)) * m, 0.045, 0.034,
                    f"arm{side}", color=CLAW)
        for j, (t, ln) in enumerate(((0.3, 0.06), (0.65, 0.05))):
            b = (EL + (HA - EL) * t + (0.04, 0.0, 0.035)) * m
            lv.spike(k, f"armspike{side}{j}", b, np.array((0.6, 0.15, 0.75)) * m, ln, 0.04, 0.03,
                     np.array((0, 1, 0)), "#25242A", f"arm{side}", sink=0.02, surface="metal")


TAIL = [(0, 0.15, 0.33), (0.04, 0.3, 0.22), (0.1, 0.41, 0.15), (0.17, 0.49, 0.12), (0.22, 0.53, 0.115)]
TR = [0.12, 0.09, 0.066, 0.048, 0.03]


def tail(k):
    k.add("tail", tube(TAIL, TR), GREEN, "tail")
    pts = np.array(TAIL)
    for j, t in enumerate((0.4, 0.58, 0.74)):
        i = t * (len(pts) - 1); a = int(i); f = i - a
        c = pts[a] * (1 - f) + pts[a + 1] * f
        r = np.interp(t, np.linspace(0, 1, len(TR)), TR)
        k.add(f"tailring{j}", ring_band(c, pts[a + 1] - pts[a], r - 0.001, 0.02, thick=0.008), GLOW, "tail",
              fuse=False, glow=GLOW, surface="metal")
    # the star of green points round a dark one at its end
    end, d = pts[-1], unit(*(pts[-1] - pts[-2]))
    a1 = np.cross(d, (0, 0, 1.0)); a1 /= np.linalg.norm(a1)
    a2 = np.cross(d, a1)
    for j, ang in enumerate((90, 200, 340)):
        o = math.cos(math.radians(ang)) * a1 + math.sin(math.radians(ang)) * a2
        lv.spike(k, f"star{j}", end, unit(*(o * 1.0 + d * 0.5)), 0.1, 0.05, 0.034, d, GREEN, "tail", sink=0.02,
                 surface="metal")
    lv.spike(k, "startip", end, d, 0.1, 0.045, 0.034, unit(0, 0, 1), CLAW, "tail", sink=0.02, surface="metal")


def plates(k):
    """The glowing plates down the back, two rows splayed apart, each a thick kite-section hull."""
    for j, (z, y, ln, w) in enumerate(((0.75, 0.05, 0.19, 0.12), (0.64, 0.12, 0.23, 0.145), (0.52, 0.165, 0.22, 0.14),
                                        (0.4, 0.18, 0.16, 0.115))):
        b = np.array((0.04, y, z))
        d = unit(0.42, 0.82, 0.5 - 0.06 * j)
        lv.spike(k, f"plate{j}", b, d, ln, w, 0.04, np.cross(d, (1.0, 0, 0)) if j else (0, -0.5, 1), GLOW, "body",
                 sink=0.05, mirror=True, surface="metal")
    # glow: set on the plates added above
    for p in k.parts:
        if p["name"].startswith("plate"):
            p["glow"] = GLOW


def windows(k):
    """The glowing triangles let into the armour (decals: crisp, in every state), and a few white specks."""
    down = polygon([(-1, 1), (1, 1), (0, -1)])
    up_ = polygon([(-1, -1), (1, -1), (0, 1)])
    for s in (1, -1):
        m = np.array([s, 1, 1.0])
        k.decal(np.array((0.12, -0.165, 0.6)) * m, unit(0.45 * s, -1, 0.25), (0.045, 0.04), WINDOW, eps=0.0035,
                outline=down, rings=10, segs=48)
        k.decal(np.array((0.17, -0.12, 0.45)) * m, unit(0.75 * s, -0.75, 0.0), (0.05, 0.038), WINDOW, eps=0.0035,
                outline=up_, rings=10, segs=48)
        k.decal(np.array((0.3, -0.03, 0.24)) * m, unit(1 * s, -0.25, 0.0), (0.04, 0.05), WINDOW, eps=0.0035,
                outline=down, rings=10, segs=48)
    k.decal((0, -0.185, 0.39), unit(0, -1, 0.15), (0.075, 0.075), WINDOW, eps=0.0035, outline=down, rings=12,
            segs=56)
    for c, n in (((0.0, -0.19, 0.42), (0, -1, 0.15)), ((0.13, -0.17, 0.62), (0.45, -1, 0.25))):
        k.decal(np.array(c) + np.array(n) * 0.005, unit(*n), (0.006, 0.006), SPECK, eps=0.005, rings=4, segs=16)


def eyes(k):
    de = unit(0.6, -0.76, 0.2)
    E = lv.on_ell(H, HR, de) + np.array([0, -0.012, -0.004])
    cut = lv.brow_cut(0.24, 0.45)
    look_open = lv.angry_eye(cut, iris=EYE_CYAN, iris_r=0.62, pupil_r=0.24)
    m = np.array([-1, 1, 1.0])
    lid = srgb(GREEN)

    def lay(state):
        look = eye_look(state, look_open, lid, lv.INK, line=0.17)
        k.decal(E, de, (0.032, 0.029), look, eps=0.0035, rings=20, segs=56, outline=cut)
        k.decal(E * m, de * m, (0.032, 0.029), lambda u, v: look(-u, v), eps=0.0035, rings=20, segs=56,
                outline=lambda th: cut(math.pi - th))
    return lay


def ironthorns(src, out):
    k = Kit(src, coat="metal")
    trunk(k)
    head(k)
    legs(k)
    arms(k)
    tail(k)
    plates(k)
    windows(k)
    lay = eyes(k)
    k.build(voxel=0.0075, smooth=4, band=0.008)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    ironthorns(*args())
