# Liliacel (Carpathia No. 10139) sculpted from nothing on the skeleton and clips of the game's own export of it:
#   blender --background --factory-startup --python sculpts/liliacel.py -- <the export's Liliacel.glb> <out.glb>
# From its picture: a small brown bat HANGING HEAD DOWN, which the brief makes its identity: two small feet at the top
# gripping with hooked claws, a little furry rump between them, then its dark brown wings wrapped round it as a
# cocoon narrow at the top and broad below, finger ribs running across the folds, the hem hanging over the top of its
# head with a pointed flap on one side; below, its round brown head upside down: a pug nose with two nostrils and a
# small mouth above it (nearer the cocoon), tiny black eyes with a glint; two huge round ears hanging down and out,
# brown rims, their insides a soft pale-violet glow.
# The skeleton is a bird's standing upright (head joint above the body): kept on body throughout, since the head and
# ears hang BELOW the body here and the head, ear and leg joints would swing them from the wrong end.
# The export hovers (lowest 0.107, top 0.975): the hanging bat spans the same.
# Written to be grown from: Liliacon imports its palette and helpers (sibling("liliacel")).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, slab, outward, srgb, inside, smoothstep, unit, eye_look, polygon, args

# --- the line's colours -------------------------------------------------------------------------------------------
FUR, FURLIGHT, FURDARK = "#5C473D", "#735A4D", "#43342E"
WING, RIB = "#4A3A33", "#64503F"
NOSE, NOSTRIL = "#6E564B", "#271B18"
EARGLOW, EARCORE = "#9563E6", "#B98CFF"
CLAW = "#2E2420"
EYEINK, GLINT = "#120D0F", "#FFFFFF"


# --- helpers the line shares ----------------------------------------------------------------------------------------
def plate(pts, thick, hint=None):
    """A flat plate with a hard rim through points (roughly in one plane), its face toward hint. Returns (shape, n)."""
    P = np.array(pts, float)
    o = P.mean(axis=0)
    ex = P[1] - P[0]; ex /= np.linalg.norm(ex)
    n = np.cross(P[1] - P[0], P[2] - P[0]); n /= np.linalg.norm(n)
    if hint is not None and n @ np.array(hint, float) < 0:
        n = -n
    ez = np.cross(n, ex)
    outline = [((p - o) @ ex, (p - o) @ ez) for p in P]
    return slab(outline, o, ex, ez, thick), n


def claw(base, d, length, r, up=(0, 1, 0)):
    """A solid claw: a diamond of points round base and a tip along d."""
    d = unit(*d); up = np.array(up, float)
    a = np.cross(d, up)
    if np.linalg.norm(a) < 1e-3:
        a = np.cross(d, (1, 0, 0))
    a /= np.linalg.norm(a); b = np.cross(a, d)
    base = np.array(base, float)
    return hull([base + a * r, base - a * r, base + b * r, base - b * r, base + d * length])


def frame_ell(c, r, d, face, deform=None, sub=4):
    """An ellipsoid laid along d (its first radius) with its third radius along face, wound outward."""
    d = np.array(d, float); d /= np.linalg.norm(d)
    n = np.array(face, float); n = n - d * (n @ d); n /= np.linalg.norm(n)
    w = np.cross(n, d)
    V, F = ell((0, 0, 0), r, deform=deform, sub=sub)
    return outward((V @ np.stack([d, w, n], axis=1).T + np.array(c, float), F))


def dished(front=0.4):
    def f(V):
        V = V.copy()
        V[:, 2] = np.where(V[:, 2] > 0, V[:, 2] * front, V[:, 2])
        return V
    return f


def bat_eye(ink=EYEINK, glint=GLINT):
    """The line's eye: solid black with one bold glint and a small second one."""
    K, G = srgb(ink), srgb(glint)
    def look(u, v):
        c = np.tile(K, (len(u), 1))
        c[((u - 0.28) / 0.32) ** 2 + ((v - 0.3) / 0.32) ** 2 < 1] = G
        c[((u + 0.35) / 0.14) ** 2 + ((v + 0.35) / 0.14) ** 2 < 1] = G
        return c
    return look


def on_ell(c, r, d):
    """The point of an ellipsoid (centre c, radii r) along direction d from its centre."""
    d = np.array(d, float); r = np.array(r, float)
    return np.array(c, float) + d / np.linalg.norm(d / r)


# --- the frame ------------------------------------------------------------------------------------------------------
COC_C, COC_R = np.array([0.0, 0.005, 0.645]), np.array([0.19, 0.138, 0.245])
HEAD_C, HEAD_R = np.array([0.0, -0.02, 0.355]), np.array([0.13, 0.115, 0.112])
VOXEL = 0.0072


def taper(nz):
    """The cocoon's width at a height (-1 bottom, 1 top): broadest a third of the way up, a shield over the head."""
    return (1 - 0.6 * np.clip((nz + 0.35) / 1.35, 0, 1) ** 1.15) * (1 + 0.3 * np.clip(-nz + 0.2, 0, 1))


def cocoon_taper(V):
    V = V.copy()
    k = taper(V[:, 2])
    V[:, 0] *= k; V[:, 1] *= k
    V[:, 2] = np.where(V[:, 2] < 0, V[:, 2] * 0.8, V[:, 2])
    return V


def on_cocoon(th, z, lift=0.004):
    """A point on the wrapped wings at an angle round from the front (toward +X) and a height."""
    nz = (z - COC_C[2]) / COC_R[2]
    if nz < 0:
        nz /= 0.8
    s = math.sqrt(max(0.0, 1 - nz * nz)) * float(taper(np.array(nz)))
    out = np.array([math.sin(th), -math.cos(th), 0.0])
    return COC_C + np.array([COC_R[0] * s * math.sin(th), -COC_R[1] * s * math.cos(th), 0.0]) + out * lift + \
        np.array([0, 0, z - COC_C[2]])


def rib(k, name, pts, r=0.012):
    P = [on_cocoon(th, z) for th, z in pts]
    k.add(name, outward(tube(P, [r] * len(P), seg=10, per=6)), RIB, "body", fuse=False, surface="leather")


def liliacel(src, out):
    k = Kit(src, coat="fur")

    # The rump between the feet, and the legs up to the feet that grip
    k.add("rump", ell((0, 0.0, 0.855), (0.055, 0.05, 0.05)), FUR, "body")
    k.add("leg", tube([(0.028, 0.0, 0.86), (0.04, 0.0, 0.905), (0.045, -0.004, 0.93)], [0.017, 0.015, 0.014]),
          FUR, "body", mirror=True)
    k.add("foot", ell((0.046, -0.006, 0.935), (0.024, 0.02, 0.014)), FURDARK, "body", mirror=True)
    for j, dx in enumerate((-0.016, 0.0, 0.016)):
        k.add(f"claw{j}", claw((0.046 + dx, -0.008, 0.94), (dx * 18, -0.35, 1), 0.036, 0.0115), CLAW, "body",
              fuse=False, mirror=True, surface="shell")

    # The wings wrapped round as a cocoon, narrow at the top, broad over the head
    k.add("cocoon", ell(COC_C, COC_R, deform=cocoon_taper), WING, "body", surface="leather")
    # The finger ribs across the folds, front and back, and the wings' meeting seam
    rib(k, "ribA", [(-0.7, 0.85), (-0.1, 0.72), (0.5, 0.58), (0.95, 0.49)])
    rib(k, "ribB", [(-0.15, 0.84), (0.45, 0.72), (1.0, 0.62), (1.45, 0.55)])
    rib(k, "seam", [(-1.0, 0.82), (-0.6, 0.66), (-0.3, 0.52), (-0.1, 0.45)], r=0.013)
    rib(k, "ribC", [(-1.6, 0.8), (-1.3, 0.62), (-1.15, 0.5)])
    rib(k, "backA", [(2.4, 0.84), (2.9, 0.7), (3.5, 0.56), (3.9, 0.5)])
    rib(k, "backB", [(3.0, 0.86), (3.5, 0.74), (4.1, 0.62), (4.5, 0.56)])
    rib(k, "backseam", [(math.pi + 0.15, 0.86), (math.pi + 0.05, 0.68), (math.pi - 0.05, 0.5)], r=0.013)
    # The flap of the wing's tip hanging past the head on the +X side, its point down
    flap, _ = plate([(0.07, -0.115, 0.5), (0.175, -0.075, 0.5), (0.245, -0.005, 0.37), (0.15, 0.02, 0.44)],
                    0.022, hint=(1, -0.6, 0))
    k.add("flap", flap, WING, "body", fuse=False, surface="leather")
    k.add("flaprib", outward(tube([(0.17, -0.08, 0.5), (0.24, -0.0, 0.375)], [0.012, 0.01], seg=10, per=4)), RIB,
          "body", fuse=False, surface="leather")

    # The head, upside down under the hem; the pug nose on its upper front (toward the cocoon)
    k.add("head", ell(HEAD_C, HEAD_R), FUR, "body")
    k.add("cheeks", ell((0, -0.045, 0.325), (0.115, 0.085, 0.07)), FUR, "body")
    k.add("muzzle", ell((0, -0.118, 0.405), (0.062, 0.042, 0.048)), NOSE, "body")
    # Tufts of fur round the crown, which hangs at the bottom here
    for i, a in enumerate((-150, -115, -65, -30, 30, 65, 115, 150)):
        t = math.radians(a)
        dx, dy = math.sin(t), -math.cos(t)
        b = (HEAD_C[0] + 0.1 * dx, HEAD_C[1] + 0.085 * dy, 0.29)
        k.add(f"tuft{i}", claw(b, (dx, dy, -0.9), 0.032, 0.015, up=(0, 0, 1)), FURLIGHT, "body", fuse=False)

    # The ears: great round dishes hanging down and out, a brown rim, a glowing pale-violet inside
    for s in (1, -1):
        root = np.array([0.085 * s, -0.01, 0.3])
        d = unit(0.55 * s, -0.05, -1.0)
        face = unit(0.38 * s, -1.0, -0.1)
        c = root + d * 0.12
        n = face - d * (face @ d); n /= np.linalg.norm(n)
        sn = "R" if s > 0 else "L"
        k.add(f"ear{sn}", frame_ell(c, (0.135, 0.09, 0.024), d, face, deform=dished(0.4)), FUR, "body",
              fuse=False)
        k.add(f"earglow{sn}", frame_ell(c + n * 0.006 + d * 0.006, (0.112, 0.072, 0.013), d, face, sub=3),
              EARGLOW, "body", fuse=False, glow=EARGLOW)
        k.add(f"earcore{sn}", frame_ell(c + n * 0.014 + d * 0.022, (0.06, 0.035, 0.009), d, face, sub=3),
              EARCORE, "body", fuse=False, glow=EARCORE)

    # The nostrils at the snout's lower front and the small mouth above them
    for s in (1, -1):
        k.decal((0.019 * s, -0.157, 0.398), unit(0.25 * s, -1, -0.15), (0.012, 0.009), NOSTRIL, eps=0.003,
                rings=10, segs=36)
    k.decal((0, -0.147, 0.434), unit(0, -1, 0.35), (0.024, 0.0065), NOSTRIL, up=(0, 0, -1), eps=0.003,
            rings=10, segs=48)

    k.build(voxel=VOXEL, smooth=4, band=0.006, wband=0.04)

    # The eyes: tiny in the art, bold here; black with a glint, on the face below the nose (the bat is upside down,
    # so the eyes' own up is the world's down)
    LID, INK = srgb(FUR), srgb(EYEINK)
    ED = unit(0.5, -0.84, -0.05)
    E = on_ell(HEAD_C, HEAD_R, ED)
    def eyes(state):
        k.decal(E, ED, (0.024, 0.024), eye_look(state, bat_eye(), LID, INK, line=0.17), up=(0, 0, -1),
                rings=20, segs=56, eps=0.004, mirror=True)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    liliacel(*args())
