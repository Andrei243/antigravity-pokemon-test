# Valvisor (No. 10135, Carpathia) sculpted from nothing on the skeleton and clips of the game's own export of it:
#   blender --background --factory-startup --python sculpts/valvisor.py -- <the export's Valvisor.glb> <out.glb>
#
# From its picture (docs/art/reference/gemini/carpathia/10135-Valvisor.jpg) and its brief (CP-dex-3-river-and-caves.md,
# 135): a small pale mine spirit. Largest first: a big round head drawn up into a candle's flame, with licks of flame
# peeling off its sides and a curling tip; an old rusty miner's oil lamp held out on one side by its bail, its glass
# glowing gold behind two crossed guard bars; a little body under the head that thins into a wispy tail trailing back
# and down; one arm out to the lamp, the other curled on the belly. Big dark eyes with a pale glow in each, grey
# cheeks, a small round open mouth. A few grains of gold dust at the tail's end (painted, the falling grains dropped).
# The game's export has no sculptor (a candle by rule): its bones are root, body (0.188) and flame (0.461); the head
# goes on flame, the body, the arms, the tail and the lamp on body (there are no arm bones).
#
# Written to be grown from (valvoara.py imports it with sibling("valvisor")): the palette, on_ell, lick, lamp, link
# and the eyes' looks are functions here; the calls under __main__.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, lathe, hull, outward, srgb, inside, smoothstep, unit, eye_look, args

# --- the line's colours --------------------------------------------------------------------------------------------
GHOST, GHOSTB, FLAMEB = "#E6EDF4", "#C7D4E2", "#D2E1EE"
EYE, EYEGLOW, GLINT = "#1D2336", "#D8EAAE", "#FFFFFF"
CHEEK, MOUTH = "#B9C6D6", "#33303F"
RUST, RUSTD, RUSTL = "#5C4A3C", "#3D3129", "#7E5E42"
GLASS = "#FFC85A"
GOLD = "#E2B040"

# --- Valvisor's frame (the skeleton: body 0.188, flame 0.461; the export stands 0.776 tall) -------------------------
HC = np.array([0.0, -0.005, 0.43])
HR = np.array([0.148, 0.13, 0.135])
VOXEL = 0.0055


def on_ell(a, z, c=HC, r=HR):
    """The point of an ellipsoid at azimuth a (radians from the front, + toward +X) and height z, and its normal."""
    e = math.asin(np.clip((z - c[2]) / r[2], -1, 1))
    q = np.array([math.sin(a) * math.cos(e), -math.cos(a) * math.cos(e), math.sin(e)])
    p = np.array(c, float) + q * np.array(r, float)
    n = q / np.array(r, float)
    return p, n / np.linalg.norm(n)


def lick(k, name, pts, radii, colour, bone, squash=(0, 1, 0), f=0.6):
    """A lick of flame peeling off a body: a tapering blade, fused."""
    k.add(name, blade(pts, radii, squash, f), colour, bone)


def link(c, d, R, r, seg=20, nr=10):
    """One ring of a chain about centre c, its axis d: a torus as a lathe of a closed circle."""
    prof = [(R + r * math.cos(t), r * math.sin(t)) for t in np.linspace(0, 2 * math.pi, nr + 1)]
    return outward(lathe(c, d, prof, seg=seg))


def lamp(k, base, H, bone, glass=GLASS, prefix="lamp", tilt=0.0, w=0.04):
    """
    An old miner's oil lamp standing on base (its foot's middle), H tall to the top of its bail: a wide foot and fount,
    a glowing glass globe with two guard wires a side and two crossed bars over its front and back, a cap with a
    chimney, the bail arcing over the top in the X-Z plane. Every piece loose. Returns the bail's top.
    """
    b = np.array(base, float)
    up = np.array([math.sin(tilt), 0, math.cos(tilt)])
    across = np.array([math.cos(tilt), 0, -math.sin(tilt)])
    fwd = np.array([0, -1.0, 0])

    def at(x, y, z):
        return b + across * x * H + fwd * (-y) * H + up * z * H

    def L(prof):
        return [(r_ * H, z * H) for r_, z in prof]
    k.add(prefix + "foot", lathe(b, up, L([(0, 0), (0.25, 0.0), (0.275, 0.025), (0.27, 0.06), (0.22, 0.085),
                                          (0.17, 0.11), (0.16, 0.135), (0, 0.135)]), seg=40),
          RUSTD, bone, fuse=False, surface="metal")
    k.add(prefix + "fount", lathe(b, up, L([(0, 0.06), (0.21, 0.07), (0.235, 0.095), (0.2, 0.125), (0, 0.13)]),
                                  seg=40), RUSTL, bone, fuse=False, surface="metal")
    k.add(prefix + "glass", lathe(b, up, L([(0, 0.12), (0.12, 0.125), (0.165, 0.18), (0.185, 0.27), (0.17, 0.37),
                                           (0.12, 0.45), (0, 0.46)]), seg=40),
          glass, bone, fuse=False, glow=glass, surface="plastic")
    k.add(prefix + "cap", lathe(b, up, L([(0, 0.44), (0.16, 0.44), (0.2, 0.47), (0.19, 0.5), (0.13, 0.55),
                                          (0.075, 0.57), (0.075, 0.64), (0.1, 0.655), (0.1, 0.7), (0.06, 0.72),
                                          (0, 0.72)]), seg=40),
          RUST, bone, fuse=False, surface="metal")
    # w: a wire's radius in the lamp's height (0.013 of a lamp 0.32 tall)
    for s, side in ((1, "R"), (-1, "L")):
        for dy, tag in ((-0.07, "f"), (0.07, "b")):
            g = [at(0.22 * s, dy, 0.085), at(0.27 * s, dy, 0.18), at(0.275 * s, dy, 0.3), at(0.22 * s, dy, 0.42),
                 at(0.17 * s, dy, 0.47)]
            k.add(prefix + "guard" + tag + side, tube(g, [w * H] * 5, seg=10, per=4), RUSTD, bone, fuse=False,
                  surface="metal")
    # the crossed bars over the glass, front and back: each runs over the globe's surface from a low corner to a
    # high one
    def globe_r(z):
        return float(np.interp(z, [0.12, 0.18, 0.27, 0.37, 0.45], [0.12, 0.165, 0.185, 0.17, 0.12]))
    for fb, tag in ((1, "f"), (-1, "b")):
        for s in (1, -1):
            pts = []
            for t in np.linspace(0, 1, 6):
                z = 0.17 + 0.24 * t
                ang = (t - 0.5) * 1.3 * s
                r_ = globe_r(z) + w * 0.6
                pts.append(at(r_ * math.sin(ang), fb * r_ * math.cos(ang), z))
            k.add(prefix + "bar" + tag + ("p" if s > 0 else "m"), tube(pts, [w * H * 0.85] * 6, seg=10, per=4),
                  RUSTD, bone, fuse=False, surface="metal")
    bail = [at(0.19, 0, 0.47), at(0.235, 0, 0.62), at(0.2, 0, 0.8), at(0.11, 0, 0.94), at(0.0, 0, 0.985),
            at(-0.11, 0, 0.94), at(-0.2, 0, 0.8), at(-0.235, 0, 0.62), at(-0.19, 0, 0.47)]
    k.add(prefix + "bail", tube(bail, [w * H] * 9, seg=10, per=5), RUSTD, bone, fuse=False, surface="metal")
    k.add(prefix + "knob", ell(at(0, 0, 0.72), (0.06 * H, 0.06 * H, 0.03 * H), sub=2), RUSTD, bone, fuse=False,
          surface="metal")
    return at(0, 0, 0.985)


def dark_eye(u, v):
    """Valvisor's eye: dark all over, a pale glow standing in it toward the nose, a white glint up and out."""
    c = np.tile(srgb(EYE), (len(u), 1))
    c[((u + 0.12) / 0.27) ** 2 + ((v + 0.08) / 0.38) ** 2 < 1] = srgb(EYEGLOW)
    c[((u - 0.38) / 0.17) ** 2 + ((v - 0.5) / 0.17) ** 2 < 1] = srgb(GLINT)
    return c


def board(ob):
    me = ob.data
    V = np.empty(len(me.vertices) * 3); me.vertices.foreach_get("co", V); V = V.reshape(-1, 3)
    a = math.radians(31.5)
    proj = V[:, 0] * math.cos(a) + V[:, 1] * math.sin(a)
    h, w, l = V[:, 2].max(), np.ptp(V[:, 0]), np.ptp(V[:, 1])
    print("KIT: board: shows %.3f in a square of %.3f (four-corner sum %.3f)"
          % (np.ptp(proj), 1.3 * max(h, 0.85 * max(w, l)), 0.853 * w + 0.522 * l))


EYE_A, EYE_Z, EYE_HALF = 0.45, 0.448, (0.039, 0.046)


def valvisor(src, out_path):
    k = Kit(src, coat="skin")

    # --- the head, drawn up into a flame -------------------------------------------------------------------------
    k.add("head", ell(HC, HR), GHOST, "flame")
    k.add("flame", tube([(0, 0.0, 0.46), (0, 0.008, 0.55), (-0.008, 0.018, 0.63), (0.004, 0.028, 0.695),
                         (0.022, 0.036, 0.738), (0.03, 0.04, 0.752)],
                        [0.122, 0.1, 0.068, 0.04, 0.019, 0.011], seg=24), GHOST, "flame")
    # licks peeling off the sides (the one standing out like an ear on +X) and two behind for the back camera: few,
    # fat and curling back up at their tips
    lick(k, "lickear", [(0.095, 0.0, 0.475), (0.142, 0.008, 0.505), (0.172, 0.012, 0.54), (0.176, 0.014, 0.568)],
         [0.05, 0.032, 0.017, 0.008], GHOST, "flame")
    lick(k, "lickR", [(0.055, 0.02, 0.58), (0.093, 0.028, 0.625), (0.112, 0.032, 0.67), (0.104, 0.034, 0.705)],
         [0.048, 0.032, 0.017, 0.008], GHOST, "flame")
    lick(k, "lickL", [(-0.05, 0.02, 0.595), (-0.087, 0.03, 0.638), (-0.1, 0.034, 0.68), (-0.09, 0.036, 0.71)],
         [0.046, 0.03, 0.016, 0.008], GHOST, "flame")
    for s in (1, -1):
        lick(k, "lickback" + ("R" if s > 0 else "L"),
             [(0.045 * s, 0.07, 0.5), (0.078 * s, 0.115, 0.55), (0.092 * s, 0.14, 0.6), (0.085 * s, 0.15, 0.635)],
             [0.05, 0.032, 0.017, 0.008], GHOST, "flame", squash=(1, 0, 0))

    # --- the body, its arms and its wispy tail (trailing out to -X and back, clear of the lamp on +X) -------------
    k.add("torso", ell((0, 0.004, 0.29), (0.08, 0.07, 0.088)), GHOST, "body")
    k.add("tail", tube([(0, 0.012, 0.27), (-0.025, 0.04, 0.21), (-0.07, 0.07, 0.15), (-0.12, 0.1, 0.105),
                        (-0.165, 0.12, 0.075), (-0.195, 0.13, 0.06)], [0.072, 0.056, 0.041, 0.028, 0.017, 0.011],
                       seg=20), GHOST, "body")
    k.add("wispL", tube([(-0.05, 0.03, 0.29), (-0.1, 0.06, 0.27), (-0.15, 0.09, 0.24), (-0.18, 0.11, 0.22)],
                        [0.032, 0.024, 0.016, 0.011], seg=16), GHOST, "body")
    k.add("wispB", tube([(0.0, 0.05, 0.22), (-0.02, 0.1, 0.17), (-0.05, 0.15, 0.12), (-0.07, 0.18, 0.09)],
                        [0.034, 0.025, 0.016, 0.011], seg=16), GHOST, "body")
    # +X arm out to the lamp's bail, the -X one curled on the belly
    k.add("armR", tube([(0.055, -0.01, 0.31), (0.12, -0.035, 0.305), (0.192, -0.06, 0.318)], [0.032, 0.027, 0.025],
                       seg=16), GHOST, "body")
    k.add("handR", ell((0.203, -0.066, 0.32), (0.031, 0.028, 0.03)), GHOST, "body")
    k.add("armL", tube([(-0.055, -0.01, 0.31), (-0.085, -0.05, 0.27), (-0.045, -0.078, 0.25)], [0.03, 0.027, 0.025],
                       seg=16), GHOST, "body")
    k.add("handL", ell((-0.03, -0.083, 0.252), (0.028, 0.024, 0.026)), GHOST, "body")

    # --- the lamp, held by its bail on +X -------------------------------------------------------------------------
    lamp(k, (0.208, -0.07, 0.0), 0.322, "body")

    # --- colours on the fused body -------------------------------------------------------------------------------
    k.paint(lambda P: smoothstep(0.55, 0.7, P[:, 2]), FLAMEB)                   # the flame's top a little bluer
    k.paint(lambda P: smoothstep(0.22, 0.06, P[:, 2]) * 0.8, GHOSTB)             # the tail fading
    rs = np.random.RandomState(135)
    dust = []
    for t in np.linspace(0.55, 1.0, 9):
        q = np.array([-0.04, 0.05, 0.19]) * (1 - t) + np.array([-0.195, 0.13, 0.06]) * t
        dust.append(q + rs.uniform(-0.012, 0.012, 3))
    dust = np.array(dust)
    k.paint(lambda P: np.clip(sum(inside(np.linalg.norm(P - d, axis=1) - 0.011, 0.003) for d in dust), 0, 1)
            * inside(P[:, 2] - 0.2), GOLD)

    # cheeks and the little round open mouth: in every state
    for s in (1, -1):
        p, n = on_ell(0.78 * s, 0.39)
        k.decal(p, n, (0.022, 0.014), CHEEK, eps=0.0022, rings=8, segs=36)
    p, n = on_ell(0.0, 0.372)
    k.decal(p, n, (0.016, 0.012), MOUTH, eps=0.0022, rings=8, segs=36)

    body = k.build(voxel=VOXEL, smooth=4, band=0.006, wband=0.04)
    board(body)

    def lay(state):
        for s in (1, -1):
            p, n = on_ell(EYE_A * s, EYE_Z)
            look = eye_look(state, dark_eye, srgb(GHOST), srgb(EYE), line=0.16)
            lk = look if s > 0 else (lambda u, v, f=look: f(-u, v))
            k.decal(p, n, EYE_HALF, lk, eps=0.0025, rings=20, segs=56)
    k.eye_states(lay)
    k.export(out_path)


if __name__ == "__main__":
    valvisor(*args())
