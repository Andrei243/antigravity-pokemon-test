# Buzzwole sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/buzzwole.py -- <the export's Buzzwole.glb> <out.glb>
#
# From the references: a mosquito swollen with muscle. A small black head sunk between the shoulders, ringed by a few
# black spikes, two antennae in a V (black, banded red), a long silver proboscis with a black stripe slanting down and
# forward to the model's left; two great orange pecs shining yellow on a black frame, red bulges of muscle beside the
# head, smaller red ones under the pecs; a narrow banded waist; enormous arms, the largest masses: red shoulders, dark
# red upper arms with red biceps, red forearms, huge black fists, black spikes at shoulder, elbow and wrist (carried
# forward and down here, the fists before the belly); small pale orange wings behind; four segmented red legs with
# black knees ending in black spikes, two each side, one forward and one back.
# The skeleton: body (0, 0, 0.46), armL/R (-+0.12, 0, 0.56), head (0, -0.03, 0.6), legL/R (-+0.06, 0, 0.32).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, hull, slab, ring_band, outward, srgb, inside, smoothstep, unit, boxy,
                       eye_look, args)

RED, ORANGE, YELLOW, DARK = "#E43A2C", "#F7962F", "#FAC64C", "#8A2523"
BLACK, FIST, SILVER, WING, IRIS = "#2B2426", "#433739", "#C6C9D1", "#F6B46E", "#F7A63A"

HC, HR = np.array([0.0, -0.02, 0.638]), np.array([0.066, 0.06, 0.06])
VOXEL = 0.0072


def on_ell(c, r, d):
    d = unit(*d)
    return np.array(c, float) + d / math.sqrt(((d / np.array(r)) ** 2).sum())


def spike(base, d, r, length, n=6):
    """A black cone: a ring of n points round base, square to d, and a tip along d."""
    a, b = np.cross(d, (0, 0, 1.0)), None
    if np.linalg.norm(a) < 1e-3:
        a = np.cross(d, (1.0, 0, 0))
    a = a / np.linalg.norm(a); b = np.cross(d, a)
    pts = [base + r * (math.cos(t) * a + math.sin(t) * b) for t in np.linspace(0, 2 * math.pi, n, endpoint=False)]
    return hull(pts + [base + np.array(d) * length])


def buzzwole(src, out):
    k = Kit(src, coat="shell")

    # --- the trunk: a black frame under two great orange pecs ---------------------------------------------------------
    k.add("core", ell((0, 0.02, 0.5), (0.15, 0.1, 0.14)), BLACK, "body")
    k.add("pec", ell((0.098, -0.035, 0.5), (0.094, 0.072, 0.088), rot=(0, -8, 0)), ORANGE, "body", mirror=True)
    k.add("trap", ell((0.125, 0.0, 0.62), (0.07, 0.065, 0.06)), RED, "body", mirror=True)
    k.add("back", ell((0, 0.075, 0.52), (0.12, 0.065, 0.12)), DARK, "body")
    k.add("abs", ell((0.045, -0.03, 0.39), (0.045, 0.045, 0.04)), RED, "body", mirror=True)
    k.add("waist", ell((0, 0.0, 0.35), (0.07, 0.062, 0.05)), RED, "body")
    k.add("belly", ell((0, 0.01, 0.31), (0.075, 0.07, 0.04)), DARK, "body")
    # the pecs shine yellow at their upper middles
    for s in (1, -1):
        c = np.array([0.09 * s, -0.09, 0.53])
        k.paint(lambda P, c=c: inside(np.linalg.norm((P - c) / np.array([0.06, 0.08, 0.05]), axis=1) - 1, 0.25),
                YELLOW, on=["pec"])
    # a black seam between the pecs, down the middle
    k.paint(lambda P: inside(np.abs(P[:, 0]) - 0.012, 0.004) * smoothstep(0.0, -0.04, P[:, 1])
            * inside(np.abs(P[:, 2] - 0.5) - 0.09, 0.004), BLACK, on=["pec", "core"])

    # --- the head, sunk between the shoulders --------------------------------------------------------------------------
    k.add("head", ell(HC, HR), BLACK, "head")
    k.add("collar", ell((0, 0.0, 0.6), (0.075, 0.07, 0.04)), BLACK, "head")
    for i, (x, z, lean) in enumerate([(0.0, 0.7, 0.0), (0.045, 0.684, 0.7), (-0.045, 0.684, -0.7),
                                      (0.068, 0.655, 1.3), (-0.068, 0.655, -1.3)]):
        d = unit(lean * 0.6, 0.35, 1.0)
        k.add(f"hspike{i}", spike(np.array([x, 0.0, z - 0.012]), d, 0.014, 0.045), BLACK, "head", fuse=False)
    # the antennae, a V, banded red
    for s, side in ((1, "R"), (-1, "L")):
        A = np.array([(0.02 * s, 0.01, 0.68), (0.06 * s, 0.03, 0.75), (0.115 * s, 0.05, 0.83)])
        k.add(f"antenna{side}", outward(tube(A, [0.013, 0.012, 0.011])), BLACK, "head", fuse=False)
        for t in (0.42, 0.75):
            p = A[0] + (A[2] - A[0]) * t
            k.add(f"aband{side}{t}", ring_band(p, A[2] - A[0], 0.0145, 0.022, thick=0.006), RED, "head", fuse=False)
    # the proboscis: silver with a black stripe under it, slanting down and forward to the model's left (-X)
    root, tip = np.array([-0.012, -0.075, 0.625]), np.array([-0.165, -0.215, 0.46])
    d = unit(*(tip - root))
    under = unit(*np.cross(d, np.cross((0, 0, 1.0), d)))         # the way down from its line
    k.add("proboscis", outward(tube([root, (root + tip) / 2, tip], [0.017, 0.012, 0.004])), SILVER, "head",
          fuse=False, surface="metal")
    k.add("stripe", outward(tube([root - under * 0.006, (root + tip) / 2 - under * 0.0065, tip - under * 0.002],
                                 [0.013, 0.0095, 0.003])), BLACK, "head", fuse=False)

    # --- the arms: the largest masses, carried forward and down, black fists before the belly ------------------------
    SH, EL, WR = np.array([0.2, 0.0, 0.6]), np.array([0.272, -0.05, 0.45]), np.array([0.2, -0.2, 0.405])
    FC = np.array([0.168, -0.255, 0.4])
    k.add("shoulder", ell(SH + (0, 0, 0.01), (0.092, 0.088, 0.082)), RED, "arm{s}", mirror=True)
    k.add("upper", tube([SH, (SH + EL) / 2 + (0.012, 0, 0), EL], [0.075, 0.08, 0.07]), DARK, "arm{s}", mirror=True)
    k.add("biceps", ell((0.24, -0.06, 0.52), (0.06, 0.05, 0.065), rot=(0, 15, 0)), RED, "arm{s}", mirror=True)
    k.add("fore", tube([EL, (EL + WR) / 2 + (0.01, 0, 0.01), WR], [0.07, 0.074, 0.064]), RED, "arm{s}", mirror=True)
    k.add("foremuscle", ell((0.255, -0.13, 0.44), (0.055, 0.065, 0.055), rot=(0, 0, 30)), RED, "arm{s}", mirror=True)
    k.add("fist", ell(FC, (0.07, 0.068, 0.068), deform=boxy(2.6)), FIST, "arm{s}", mirror=True)
    for i, z in enumerate((0.43, 0.405, 0.38)):
        k.add(f"knuckle{i}", ell((0.17, -0.318, z), (0.045, 0.02, 0.014)), FIST, "arm{s}", mirror=True)
    # the shoulders and forearms shine orange on top
    k.paint(lambda P: smoothstep(0.62, 0.68, P[:, 2]) * smoothstep(0.1, 0.16, np.abs(P[:, 0])), ORANGE,
            on=["shoulder", "trap"])
    # black spikes: shoulder up and back, elbow out and back, wrist out
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        for j, (b, d, r, L) in enumerate([((0.24, 0.04, 0.67), (0.6, 0.4, 0.8), 0.024, 0.08),
                                          ((0.33, 0.0, 0.45), (1.0, 0.35, -0.2), 0.022, 0.07),
                                          ((0.25, -0.17, 0.43), (1.0, 0.0, 0.25), 0.018, 0.055)]):
            k.add(f"aspike{side}{j}", spike(np.array(b) * m, unit(*(np.array(d) * m)), r, L), BLACK, f"arm{side}",
                  fuse=False)

    # --- the wings: small, pale orange, behind the chest in a V ------------------------------------------------------
    for s, side in ((1, "R"), (-1, "L")):
        o = np.array([0.07 * s, 0.1, 0.52])
        ex = unit(0.55 * s, 0.55, -0.6)
        ez = unit(0.35 * s, -0.25, 0.9)
        outline = [(0.0, -0.015), (0.06, 0.03), (0.15, 0.035), (0.21, 0.0), (0.15, -0.025), (0.06, -0.03)]
        k.add(f"wing{side}", slab(outline, o, ex, ez, 0.01), WING, "body", fuse=False)

    # --- the legs: four, segmented, each a red thigh, a black knee and a tapering shin to a black spike --------------
    for s, side in ((1, "R"), (-1, "L")):
        for j, (hip, knee, foot) in enumerate([((0.05, -0.035, 0.33), (0.1, -0.085, 0.2), (0.125, -0.12, 0.065)),
                                               ((0.05, 0.045, 0.33), (0.09, 0.095, 0.2), (0.105, 0.14, 0.065))]):
            m = np.array([s, 1, 1.0])
            H, K, F = np.array(hip) * m, np.array(knee) * m, np.array(foot) * m
            k.add(f"thigh{side}{j}", tube([H, (H + K) / 2, K], [0.034, 0.042, 0.03]), RED, f"leg{side}")
            k.add(f"knee{side}{j}", ell(K, (0.03, 0.03, 0.03)), BLACK, f"leg{side}")
            k.add(f"shin{side}{j}", tube([K, (K + F) / 2, F], [0.029, 0.03, 0.022]), DARK, f"leg{side}")
            k.add(f"calf{side}{j}", ell((K + F) / 2 + np.array([0.004 * s, 0, 0.01]), (0.032, 0.032, 0.045)), RED,
                  f"leg{side}")
            k.add(f"toe{side}{j}", spike(F + np.array([0, 0, 0.012]), np.array([0, 0, -1.0]), 0.022, 0.08),
                  BLACK, f"leg{side}", fuse=False)
            mid = (K + F) / 2
            k.add(f"lspike{side}{j}", spike(mid + np.array([0.025 * s, 0, 0]), unit(1.0 * s, 0, -0.3), 0.012, 0.04),
                  BLACK, f"leg{side}", fuse=False)
    # a black band round the waist and one under it
    k.add("band1", ring_band((0, 0.0, 0.365), (0, 0, 1), 0.068, 0.014, thick=0.008), BLACK, "body", fuse=False)
    k.add("band2", ring_band((0, 0.008, 0.33), (0, 0, 1), 0.074, 0.012, thick=0.008), BLACK, "body", fuse=False)

    k.build(voxel=VOXEL, smooth=4, band=0.007)

    # --- the eyes: small and orange on the black head, either side of the proboscis --------------------------------
    CB, CI, CW, CK = srgb(BLACK), srgb(IRIS), srgb("#FFE6B0"), srgb("#140E10")
    def open_eye(u, v):
        r = (u / 0.95) ** 2 + (v / 0.9) ** 2
        c = np.tile(CB, (len(u), 1))
        c[r < 1] = CI
        c[((u + 0.25) / 0.28) ** 2 + ((v - 0.3) / 0.28) ** 2 < 1] = CW
        return c
    def lay(state):
        look = eye_look(state, open_eye, CB, CI, line=0.18)
        for s in (1, -1):
            d = unit(0.55 * s, -0.8, 0.2)
            col = look if s > 0 else (lambda u, v, f=look: f(-u, v))
            k.decal(on_ell(HC, HR, d), d, (0.016, 0.014), col, eps=0.003, rings=20, segs=56)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    buzzwole(*args())
