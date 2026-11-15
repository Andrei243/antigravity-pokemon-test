# Slowking sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown from
# Slowpoke's script (slowpoke.py) and Slowbro's (slowbro.py): the line's pink, its head with the cream bill, its round
# eyes with their tiny pupils, its claws.
#   blender --background --factory-startup --python sculpts/slowking.py -- <the export's Slowking.glb> <out.glb>
#
# The design, from the references: Slowpoke stood upright on short thick legs, a heavy pear of a body, a little cream
# low on the belly; its mouth shut under the cream bill, a small fang at its corners; no ears to be seen; a broad ruff
# round its neck of red and white wedges (radial stripes, scalloped at the edge); short arms folded forward over the
# belly; a thick tail out of the back, sweeping out to the +X side and lifting at its tip. On its head a pale grey
# Shellder worn like a turban: a fat rolled brim over the eyes, two big horns curving up out of its sides, a low spire
# rising up the back with a few solid spikes, and a red gem in a white socket on the front.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, sheet, lathe, srgb, inside, unit, outward, args, sibling

sp = sibling("slowpoke")
PINK, CREAM = sp.PINK, "#EEE0B2"
SHELL, SOCKET, GEM = "#CDCAC4", "#F8F7F3", "#D8262E"
RUFF_RED, RUFF_WHITE = "#E0453A", "#F6F2EE"

# --- the frame ------------------------------------------------------------------------------------------------------
HC = np.array([0.0, -0.045, 0.635])       # the head's middle (the head bone is at 0, -0.02, 0.54)
HS = 1.3                                  # the head's scale (Slowpoke's own is 1)
EYE_DIR = unit(0.46, -0.86, 0.3)          # lower than Slowpoke's: the brim comes down to the eyes' tops
CROWN_BASE = HC + np.array([0, 0.01, 0.08])
CS = 0.84                                 # the crown's scale against its numbers below
CROWN_TILT = math.radians(-6)            # the brim lower at the front
RUFF_C = np.array([0.0, -0.02, 0.5])
RUFF_IN, RUFF_OUT, RUFF_DROP, RUFF_N = 0.12, 0.25, 0.05, 14


def crown_frame(p):
    """Points in the crown's own frame (z up its axis) to the world: tilted forward about x, set on the head."""
    c, s = math.cos(CROWN_TILT), math.sin(CROWN_TILT)
    p = np.asarray(p, float) * CS
    y = p[..., 1] * c + p[..., 2] * s
    z = -p[..., 1] * s + p[..., 2] * c
    return np.stack([p[..., 0], y, z], axis=-1) + CROWN_BASE


def crown(k):
    """The Shellder worn as a turban: loose pieces, so their edges keep and the brim cuts the eyes' tops."""
    # the rolled brim and the low dome over it, one solid of turning
    prof = [(0.0, -0.02), (0.13, -0.02), (0.17, -0.012), (0.205, 0.008), (0.218, 0.04), (0.212, 0.072),
            (0.19, 0.095), (0.15, 0.108), (0.13, 0.13), (0.11, 0.16), (0.07, 0.185), (0.0, 0.195)]
    V, F = lathe((0, 0, 0), (0, 0, 1), prof, seg=56)
    V = V * np.array([1.0, 0.93, 1.0])
    k.add("crown", outward((crown_frame(V), F)), SHELL, "head", fuse=False, surface="shell")
    # the spire up the back: whorls narrowing to a point
    spire = [(0.0, 0.17, 0.125), (0.03, 0.22, 0.095), (0.055, 0.265, 0.068), (0.075, 0.3, 0.045),
             (0.088, 0.325, 0.026)]
    whorls = []
    for i, (y, z, r) in enumerate(spire):
        c = crown_frame((0.0, y, z))
        k.add(f"whorl{i}", outward(ell(c, np.array((r, r * 0.95, r * 0.72)) * CS)), SHELL, "head", fuse=False, surface="shell")
        whorls.append((c, r * CS))
    # solid spikes out of the whorls, leaning up the spire and out to the sides
    for i, (wi, d) in enumerate([(0, unit(0.85, 0.2, 0.55)), (0, unit(-0.85, 0.2, 0.55)), (1, unit(0.6, 0.45, 0.75)),
                                 (1, unit(-0.6, 0.45, 0.75)), (2, unit(0.0, 0.7, 0.75)), (3, unit(0, 0.2, 1))]):
        c, r = whorls[wi]
        out = np.array(d)
        side = np.cross(out, (0, 0, 1.0))
        if np.linalg.norm(side) < 1e-3:
            side = np.array([1.0, 0, 0])
        side /= np.linalg.norm(side)
        up = np.cross(side, out)
        root = c + out * r * 0.55
        w = (0.034 if wi < 2 else 0.026) * CS
        pts = [root + w * (math.cos(q) * side + math.sin(q) * up) for q in np.linspace(0, 2 * math.pi, 7)[:-1]]
        pts += [root - out * w, c + out * (r + (0.07 - 0.008 * wi) * CS)]
        k.add(f"spike{i}", hull(pts), SHELL, "head", fuse=False, surface="shell")
    # two big horns out of the brim's sides, curving up and a little in
    for sx in (1, -1):
        P = [crown_frame((0.17 * sx, 0.0, 0.04)), crown_frame((0.24 * sx, 0.0, 0.07)),
             crown_frame((0.265 * sx, -0.01, 0.13)), crown_frame((0.255 * sx, -0.015, 0.18)),
             crown_frame((0.232 * sx, -0.015, 0.215))]
        k.add(f"horn{'RL'[sx < 0]}", outward(tube(P, list(np.array([0.048, 0.043, 0.032, 0.021, 0.012]) * CS), seg=18, per=6)), SHELL,
              "head", fuse=False, surface="shell")
    # the red gem in its white socket on the front of the dome
    gc = crown_frame((0.0, -0.135, 0.125))
    n = unit(0, -1, 0.25)
    k.add("socket", outward(ell(gc - n * 0.004, (0.04, 0.02, 0.034), rot=(-14, 0, 0))), SOCKET, "head", fuse=False,
          surface="shell")
    k.add("gem", outward(ell(gc + n * 0.009, (0.026, 0.018, 0.023), rot=(-14, 0, 0))), GEM, "head", fuse=False,
          surface="shell", glow=GEM)


def ruff(k):
    """The ruff: wedges of red and white edge to edge, flaring out and down, each rounded at its outer edge. The red
    stand a little proud of the white, so the shared edges do not flicker."""
    step = 2 * math.pi / RUFF_N
    for i in range(RUFF_N):
        red = i % 2 == 0
        a0 = -math.pi / 2 + (i - 0.5) * step          # a red wedge straight in front
        grow = 1.03 if red else 1.0
        def at(u, v, a0=a0, grow=grow):
            uu = 0.015 + 0.97 * u
            th = a0 + step * uu
            rout = RUFF_OUT * grow * (0.9 + 0.1 * np.sin(math.pi * uu))
            r = RUFF_IN + (rout - RUFF_IN) * v
            x, y = r * np.cos(th), r * np.sin(th) * 0.92
            z = RUFF_C[2] - RUFF_DROP * v * (1 + 0.5 * (-np.sin(th)).clip(0, 1)) + 0.012 * v * v
            return np.stack([x + RUFF_C[0], y + RUFF_C[1], z], axis=-1)
        k.add(f"ruff{i}", outward(sheet(at, 9, 9, 0.024 if red else 0.02)), RUFF_RED if red else RUFF_WHITE, "body",
              fuse=False, surface="cloth")


def slowking(src, out):
    k = Kit(src, coat="scales")
    spec = sp.slow_head(k, HC, HS)
    # The mouth is shut under the bill and no ears show under the Shellder: take those parts back out
    k.parts = [p for p in k.parts if not (p["name"].startswith("ear") or (p["name"] == "mouth" and p["carve"]))]
    k.decals = k.decals[:-2]                          # the ears' curls
    spec["spots"] = [(sp.at_surface(HC, sp.HEAD_R * HS, EYE_DIR * np.array([sx, 1, 1])),
                      EYE_DIR * np.array([sx, 1, 1]), sx) for sx in (1, -1)]
    # The body: a heavy pear, the head set on it with no neck to speak of
    def pear(V):
        V = V.copy()
        s = 1 + 0.12 * np.clip(-V[:, 2], 0, 1) - 0.08 * np.clip(V[:, 2], 0, 1)
        V[:, 0] *= s; V[:, 1] *= s
        return V
    k.add("body", ell((0, 0.0, 0.29), (0.19, 0.165, 0.22), deform=pear), PINK, "body")
    k.add("shoulders", ell((0, -0.01, 0.46), (0.15, 0.13, 0.1)), PINK, "body")
    k.add("neck", ell((0, -0.03, 0.53), (0.12, 0.11, 0.08)), PINK, "body")
    # a little cream low on the belly
    k.paint(lambda P: inside((P[:, 0] / 0.12) ** 2 + ((P[:, 2] - 0.15) / 0.1) ** 2 - 1, 0.008) * (P[:, 1] < -0.05),
            CREAM, on=["body"])
    # Legs: heavy thighs fused into the body, short shins, broad feet with white claws
    k.add("thigh", ell((0.12, 0.0, 0.16), (0.1, 0.105, 0.11)), PINK, "leg{s}", mirror=True)
    k.add("leg", tube([(0.115, 0.0, 0.16), (0.13, -0.02, 0.06)], [0.082, 0.072]), PINK, "leg{s}", mirror=True)
    k.add("foot", ell((0.135, -0.05, 0.032), (0.074, 0.094, 0.036)), PINK, "leg{s}", mirror=True)
    sp.claws(k, "claw", (0.135, -0.138, 0.026), (0.15, -1, 0), 3, "leg{s}", size=1.4)
    # Arms: short, from the shoulders down and forward, folded in over the belly
    k.add("arm", tube([(0.14, -0.03, 0.44), (0.2, -0.1, 0.37), (0.13, -0.175, 0.3)], [0.056, 0.05, 0.045]),
          PINK, "arm{s}", mirror=True)
    k.add("hand", ell((0.11, -0.19, 0.29), (0.048, 0.044, 0.044)), PINK, "arm{s}", mirror=True)
    sp.claws(k, "finger", (0.09, -0.225, 0.285), (-0.6, -0.8, 0), 2, "arm{s}", size=1.4)
    # The tail: out of the back, along the ground out to the +X side, its tip lifting
    TAIL = [(0, 0.12, 0.22), (0.07, 0.24, 0.12), (0.19, 0.3, 0.08), (0.3, 0.29, 0.1), (0.36, 0.24, 0.17)]
    k.add("tail", tube(TAIL, [0.08, 0.072, 0.058, 0.042, 0.026]), PINK, "tail")
    ruff(k)
    crown(k)
    k.build(voxel=0.0068, smooth=3, band=0.006)
    k.eye_states(lambda st: sp.eyes(k, spec, st))
    k.export(out)


if __name__ == "__main__":
    slowking(*args())
