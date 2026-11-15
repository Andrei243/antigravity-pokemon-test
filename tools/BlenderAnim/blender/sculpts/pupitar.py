# Pupitar sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/pupitar.py -- <the export's Pupitar.glb> <out.glb>
# From the references: a hard blue-grey cocoon standing on its point, a crown of big flat-faced spikes fanning out
# round its top and sides and down its back, the front left smooth as a face plate with a raised ridge down its middle
# and a V of a seam under it; through the plate two large black sockets slanting down to the middle, in each a white
# eye with a small red pupil, and over each a black slot of a vent; two black vents on each side; the lower half in
# stepped rings to the point, a short spike out at the front of the lowest ring.
# Larvitar's line (sibling("larvitar")): its spikes of flat-faced rock and its eye cut by a brow.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, ring_band, srgb, inside, smoothstep, unit, polygon, eye_look, args, sibling

lv = sibling("larvitar")

SHELL, SEAM = "#97A6D2", "#6A76A6"
INK, WHITE, PALE = lv.INK, lv.WHITE, srgb("#C8D2EE")
PUPIL = srgb("#D8304A")

# The shell: a turned body along z, its radius at each height
SZ = [0.02, 0.07, 0.14, 0.23, 0.34, 0.44, 0.515, 0.56]
SR = [0.012, 0.08, 0.125, 0.152, 0.16, 0.146, 0.11, 0.05]
SY = [0.01, 0.008, 0.004, 0.0, 0.0, 0.01, 0.02, 0.025]


def shell_r(z):
    return np.interp(z, SZ, SR)


def shell_y(z):
    return np.interp(z, SZ, SY)


# The face plate: a flattened swelling over the front of the upper shell
FC, FR = np.array([0, -0.045, 0.38]), np.array([0.128, 0.118, 0.14])


def front_y(z, x=0.0):
    """The front surface's y at height z (on the face plate where it stands proud, else on the shell)."""
    t = 1 - ((z - FC[2]) / FR[2]) ** 2 - (x / FR[0]) ** 2
    yf = FC[1] - FR[1] * math.sqrt(t) if t > 0 else 9.0
    r = shell_r(z)
    ys = shell_y(z) - math.sqrt(max(r * r - x * x, 0))
    return min(yf, ys)


def pupitar(src, out):
    k = Kit(src, coat="shell")
    k.add("shell", tube([(0, y, z) for y, z in zip(SY, SZ)], SR, seg=40, per=8), SHELL, "body")
    k.add("face", ell(FC, FR), SHELL, "body")

    # The stepped rings of the lower half: a lip of the shell's colour over a dark seam at each step
    for z in (0.14, 0.22):
        r = shell_r(z)
        k.add(f"lip{z}", ring_band((0, shell_y(z), z + 0.012), (0, 0, 1), r + 0.003, 0.018, thick=0.008), SHELL,
              "body", fuse=False, surface="shell")
        k.add(f"seam{z}", ring_band((0, shell_y(z), z - 0.004), (0, 0, 1), r - 0.001, 0.012, thick=0.006), SEAM,
              "body", fuse=False, surface="shell")

    # The short spike out at the front of the lowest ring, and the point under it
    lv.spike(k, "lowspike", (0, -0.1, 0.11), (0, -1, -0.35), 0.075, 0.05, 0.036, (1, 0, 0), SHELL, "body",
             sink=0.03)

    # The crown: big flat-faced spikes fanning out round the top, the sides and down the back, the face left clear.
    # Each is a kite whose broad face is in the plane of its own direction and the upright, thick across it.
    CROWN = [(58, 34, 0.16, 0.48), (102, 26, 0.16, 0.42), (145, 36, 0.15, 0.45), (180, 46, 0.15, 0.47),
             (25, 70, 0.14, 0.54), (125, 66, 0.14, 0.53), (0, 80, 0.13, 0.555)]
    for j, (phi, el, ln, z0) in enumerate(CROWN):
        for s in ((1, -1) if phi not in (0, 180) else (1,)):
            p, e = math.radians(phi) * s, math.radians(el)
            rad = np.array([math.sin(p), -math.cos(p), 0.0])
            d = math.cos(e) * rad + np.array([0, 0, math.sin(e)])
            base = np.array([0, shell_y(z0), z0]) + rad * shell_r(z0) * 0.8
            tang = np.array([math.cos(p), math.sin(p), 0.0])
            lv.spike(k, f"crown{j}{'R' if s > 0 else 'L'}", base, d, ln, 0.125, 0.05, tang, SHELL, "body",
                     sink=0.035)

    # The crown's middle: one spike up and back off the shell's top
    lv.spike(k, "crowntop", (0, 0.025, 0.55), (0, 0.45, 1), 0.15, 0.12, 0.05, (1, 0, 0), SHELL, "body", sink=0.05)

    # The ridge down the middle of the face plate: a low flat-faced keel from the crown down to between the eyes
    pts = []
    for z, w, lift in ((0.55, 0.012, 0.004), (0.47, 0.022, 0.014), (0.38, 0.024, 0.016), (0.305, 0.006, 0.006)):
        y = front_y(z)
        pts += [(0, y - lift, z), (w, y + 0.012, z), (-w, y + 0.012, z)]
    k.add("keel", hull(np.array(pts)), SHELL, "body", fuse=False, surface="shell")

    # The V of a seam under the face plate, down to a point under the keel
    def vee(P):
        return inside(np.abs(P[:, 2] - (0.29 + 0.55 * np.abs(P[:, 0]))) - 0.006, 0.0025) * (P[:, 1] < -0.04) * \
            inside(np.abs(P[:, 0]) - 0.135, 0.004)
    # (the V painted as a seam hatched at the voxel: left out)

    # The slots over the eyes and the vents on the sides: black, crisp patches
    SLOT = [(-1, -0.2), (-0.4, -0.9), (0.9, -0.8), (1, 0.3), (0.3, 0.9), (-0.8, 0.8)]
    for s in (1, -1):
        sp = [(a * s, b) for a, b in SLOT] if s > 0 else [(a * s, b) for a, b in SLOT][::-1]
        k.decal((0.062 * s, front_y(0.455, 0.062), 0.455), unit(0.45 * s, -0.88, 0.25), (0.032, 0.024), INK,
                eps=0.005, outline=polygon(sp), rings=8, segs=48)
        for z, w in ((0.39, 0.04), (0.33, 0.034)):
            k.decal((0.14 * s, 0.03, z), unit(s, 0.25, 0.05), (w, 0.011), INK, eps=0.004,
                    outline=polygon([(-1, -1), (1, -1), (1, 1), (-1, 1)]), rings=6, segs=40)

    # The eyes: a large black socket slanting down to the middle, a white eye in it, a small red pupil ringed black
    de = unit(0.5, -0.86, 0.04)
    E = np.array([0.068, front_y(0.37, 0.068), 0.37])
    cut = lv.brow_cut(0.42, 0.42)

    def eye(u, v):
        reach = np.array([cut(t) for t in np.arctan2(v, u)])
        rho = np.sqrt(u * u + v * v) / reach
        c = np.tile(INK, (len(u), 1))
        w = (((u + 0.08) / 0.62) ** 2 + ((v + 0.06) / 0.58) ** 2 < 1) & (rho < 0.8)
        c[w] = WHITE
        d = np.sqrt((u + 0.12) ** 2 + (v + 0.06) ** 2)
        c[w & (d < 0.27)] = INK
        c[w & (d < 0.19)] = PUPIL
        return c
    m = np.array([-1, 1, 1.0])

    def eyes(state):
        look = eye_look(state, eye, INK, PALE, line=0.12)
        k.decal(E, de, (0.056, 0.05), look, eps=0.006, rings=20, segs=56, outline=cut)
        k.decal(E * m, de * m, (0.056, 0.05), lambda u, v: look(-u, v), eps=0.006, rings=20, segs=56,
                outline=lambda th: cut(math.pi - th))

    k.build(voxel=0.0065, smooth=4)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    pupitar(*args())
