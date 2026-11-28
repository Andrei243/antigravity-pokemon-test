# Meduzaur sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/meduzaur.py -- <the export's Meduzaur.glb> <out.glb>
#
# Carpathia's crown jellyfish (No. 10159, Water/Poison), Meduzel grown (its bell, inner dome, rings, ribbons and eyes
# imported from meduzel.py), from its picture (docs/art/reference/gemini/carpathia/10159-Meduzaur.jpg) and its brief
# (CP-dex-3b-river-and-caves-more.md, 159): a big heavy domed bell, glassy blue-violet, its top raised in a crown of
# rounded points and ridged down its sides from them; between the ridges its rings grown into sun-marks, a ring with
# short rays; the same soft inner dome in front with the same small dark eyes; under the bell a deep ruff of frilly
# purple, purple ruffled mouth arms hanging from it among eight long pale tentacles that curl at their ends. It hovers.
# The faint blue-violet glow the brief gives it at night is left out: a fused part can't glow apart from the rest.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, sheet, outward, srgb, inside, smoothstep, unit, args, sibling

m_ = sibling("meduzel")
around, on_ell, board = m_.around, m_.on_ell, m_.board


class Big(m_.Jelly):
    BELL, SHADE, BLUE, INNER, RING, FRINGE, UNDER, RIBBON, RIBBONEDGE = (
        "#C6D0F2", "#A79CE0", "#A6CAEE", "#C0C6F0", "#9A88CE", "#A890DC", "#A898DA", "#C4CAEE", "#A99CDC")
    RUFF, RUFFDK, TENTACLE, TIP = "#9C68CA", "#7C4CAC", "#C2CAEE", "#A494DA"
    C = np.array([0.0, 0.0, 0.672])
    R = np.array([0.258, 0.248, 0.238])
    FLAT = -0.62
    LOBES = 12
    IC = np.array([0.0, -0.202, 0.6])
    IR = np.array([0.112, 0.08, 0.098])
    RINGS = [(a, 40) for a in (22.5, -22.5, 67.5, -67.5, 112.5, -112.5, 157.5, -157.5)]
    RING_HALF, RING_IN, RING_OUT = 0.046, 0.5, 0.98
    EYE_X, EYE_Z, EYE = 0.04, 0.6, (0.026, 0.028)
    VOXEL = 0.0068
    LOW = 0.115


def crown(k, J):
    """Eight rounded points round the bell's top and a ridge running down the bell from each."""
    for j in range(8):
        az = 45 * j
        ridge = []
        for el in (74, 64, 52, 40, 28, 16):
            e = math.radians(el)
            d = around(az) * math.cos(e) + np.array([0, 0, math.sin(e)])
            ridge.append(on_ell(J.C, J.R, d) - J.C * 0 + unit(*((on_ell(J.C, J.R, d) - J.C) / J.R ** 2)) * 0.002)
        if az == 0:                         # the front's ridge stops above the inner dome
            ridge = ridge[:4]
        k.add(f"ridge{j}", tube(ridge, np.linspace(0.016, 0.011, len(ridge))), J.BELL, "body", surface="shell")
        e = math.radians(74)
        d = around(az) * math.cos(e) + np.array([0, 0, math.sin(e)])
        p = on_ell(J.C, J.R, d)
        up = unit(*(around(az, 0.45) + np.array([0, 0, 1.0])))
        k.add(f"point{j}", tube([p - up * 0.01, p + up * 0.03, p + up * 0.045], [0.026, 0.02, 0.012]), J.BELL, "body",
              surface="shell")


def ruff(k, J):
    """A deep ruff of frilly purple under the bell's margin: a sheet round it, waved up and down along its edge."""
    rz = m_.rim_z(J)
    def at(u, v, inner=False):
        th = 2 * math.pi * u                   # the seam at the back
        v = 0.02 + 0.96 * v
        r = 0.17 + 0.115 * v - (0.004 if inner else 0)
        z = rz + 0.012 - 0.07 * v - 0.034 * np.sin(14 * th) * v ** 1.3
        return np.stack([r * np.sin(th), r * np.cos(th), z], -1)
    k.add("ruff", outward(sheet(at, 168, 9, 0.018)), J.RUFF, "body", fuse=False, surface="skin")
    k.add("ruffin", outward(sheet(lambda u, v: at(u, v, True), 168, 9, 0.01)), J.RUFFDK, "body", fuse=False,
          surface="skin")


def arm(k, J, name, bone, root, az, length):
    """A frilly purple mouth arm hanging from under the bell, its broad face to the front, its edges ruffled."""
    rh = around(az)
    across = np.array([rh[1], -rh[0], 0.0]) * -1          # across the arm, round the bell
    def at(u, v):
        t = 0.015 + 0.97 * u
        c = (root[None, None, :] + np.array([0, 0, -length])[None, None, :] * t[..., None]
             + rh[None, None, :] * (0.035 * t + 0.012 * np.sin(t * 7))[..., None])
        w = 0.042 * (1 - 0.55 * t) * (2 * v - 1)
        ruffle = 0.016 * np.sin(t * 30 + v * 3) * (2 * v - 1) ** 2
        return c + across[None, None, :] * w[..., None] + rh[None, None, :] * ruffle[..., None]
    k.add(name, outward(sheet(at, 40, 9, 0.014)), J.RUFF, bone, fuse=False, surface="skin")


def tentacles(k, J):
    """Eight long pale tentacles from the joints inside the bell, drifting, each curling at its end; four mouth arms."""
    bones = k.arm.data.bones
    rz = m_.rim_z(J)
    for i in range(8):
        b = f"tentacle{i}"
        h = np.array(bones[b].head_local)
        out = unit(h[0], h[1], 0)
        az = math.degrees(math.atan2(out[0], -out[1]))
        tang = np.array([out[1], -out[0], 0.0]) * (1 if i % 2 else -1)
        low = J.LOW + 0.03 + 0.03 * (i % 3)
        R0 = 0.11 + 0.02 * (i % 2)
        pts = [h, h * np.array([1.2, 1.2, 1]) + np.array([0, 0, -(h[2] - rz) - 0.03])]
        for t in (0.25, 0.5, 0.75):
            z = rz - (rz - low) * t
            pts.append(out * (R0 + 0.04 * t) + tang * 0.025 * math.sin(t * 7 + i) + np.array([0, 0, z]))
        # the curl: a small loop at the end, in the plane across the tentacle
        end = pts[-1] + np.array([0, 0, -(pts[-1][2] - low)])
        cc = end + tang * 0.026
        for a in (0.0, 0.9, 1.8, 2.7, 3.6, 4.4):
            pts.append(cc - tang * 0.026 * math.cos(a) * (1 - a / 12) + np.array([0, 0, 0.026 * math.sin(a) * (1 - a / 12)]))
        radii = np.concatenate([np.linspace(0.016, 0.013, 5), np.linspace(0.0125, 0.01, len(pts) - 5)])
        k.add(f"tentacle{i}", outward(tube(pts, radii)), J.TENTACLE if i % 2 else J.TIP, b, fuse=False,
              surface="shell")
        if i % 2 == 0:
            root = out * 0.14 + np.array([0, 0, rz - 0.01])
            arm(k, J, f"moutharm{i}", b, root, az + 22, 0.25 + 0.03 * (i % 4 == 0))


def meduzaur(src, out_path, J=Big):
    k = Kit(src, coat="shell")
    m_.bell(k, J)
    crown(k, J)
    m_.tints(k, J)
    m_.rings(k, J, ticks=10)
    ruff(k, J)
    tentacles(k, J)
    k.build(voxel=J.VOXEL, band=0.006, wband=0.05)
    board(k)
    k.eye_states(m_.eyes(k, J))
    k.export(out_path)


if __name__ == "__main__":
    meduzaur(*args())
