# Kabutops sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/kabutops.py -- <the export's Kabutops.glb> <out.glb>
# Grown from kabuto.py (sibling("kabuto")): its brown, its black underside, the shell's hard edges, now on an upright
# body.
#
# From the references: a tall lean brown biped leaning forward. The head is a broad flat hood of shell: a point rising
# up at its front, a longer one sweeping back, its sides coming down over the face like a shovel; under its brow two
# small fierce eyes, white with a black pupil, and a pointed snout. A pale grey-white chest plate in segments, black at
# the waist's sides and between the legs. Thin brown arms carried out forward, each ending at the wrist in a great
# pale curved scythe hanging down, its curve bowed out. A row of brown fins down its back, a thin pointed tail behind
# to the ground. Long digitigrade legs, thighs thick, shins tapering, feet of two white claws forward and one behind.
# Skeleton: body (0, 0, 0.5), arms (+-0.09, 0, 0.62), head (0, -0.02, 0.68), legs (+-0.07, 0.02, 0.4).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, eye_look, polygon, hull, sibling, args

kb = sibling("kabuto")
BROWN, UNDER = kb.BROWN, kb.UNDER
CHEST, LINE, BLADE, CLAW = "#E6E6E2", "#8C8C92", "#DADEE6", "#F2F2EE"
WHITE, INK = "#F8F8F6", "#141418"

# --- the frame ---------------------------------------------------------------------------------------------------------
TORSO_C, TORSO_R = np.array([0.0, -0.05, 0.565]), (0.1, 0.085, 0.12)
SKULL_C, SKULL_R = np.array([0.0, -0.09, 0.72]), (0.078, 0.092, 0.07)
SHOULDER, ELBOW, WRIST = np.array([0.095, -0.04, 0.625]), np.array([0.15, -0.08, 0.585]), np.array([0.17, -0.18, 0.59])
HIP, KNEE, ANKLE, FOOT = (np.array([0.078, 0.02, 0.43]), np.array([0.105, -0.075, 0.26]),
                          np.array([0.115, 0.03, 0.085]), np.array([0.115, -0.03, 0.03]))


def add_body():
    k.add("torso", ell(TORSO_C, TORSO_R, rot=(22, 0, 0)), BROWN, "body")
    k.add("hips", ell((0, 0.02, 0.44), (0.085, 0.07, 0.06)), BROWN, "body")
    k.add("neck", tube([(0, -0.06, 0.63), (0, -0.075, 0.72)], [0.058, 0.055]), BROWN, "head")
    # the chest plate, its segment lines, black at the waist's sides and between the legs
    def plate(P):
        x, y, z = P[:, 0], P[:, 1], P[:, 2]
        w = 0.078 - 0.4 * np.clip(0.53 - z, 0, 1)
        return inside(np.abs(x) - w, 0.003) * inside(y + 0.075, 0.004) * inside(z - 0.66, 0.004) * \
            inside(0.47 - z, 0.004)
    k.paint(plate, CHEST, on=["torso", "hips"])
    k.paint(lambda P: plate(P) * (inside(np.abs(P[:, 2] - 0.61) - 0.0035, 0.002)
                                  + inside(np.abs(P[:, 2] - 0.565) - 0.0035, 0.002)
                                  + inside(np.abs(P[:, 2] - 0.52) - 0.0035, 0.002)
                                  + inside(np.abs(P[:, 0]) - 0.0028, 0.002) * (P[:, 2] < 0.6)), LINE,
            on=["torso", "hips"])
    k.paint(lambda P: inside(0.47 - P[:, 2], 0.004) * inside(P[:, 2] - 0.52, 0.004) *
            inside(0.07 - np.abs(P[:, 0]), 0.004) * inside(P[:, 1] + 0.0, 0.006) * inside(-0.09 - P[:, 1], 0.006),
            UNDER, on=["torso", "hips"])


def add_head():
    k.add("skull", ell(SKULL_C, SKULL_R), BROWN, "head")
    k.add("snout", tube([(0, -0.14, 0.71), (0, -0.185, 0.68), (0, -0.2, 0.65)], [0.048, 0.03, 0.01]), BROWN,
          "head")
    # the hood: a wide flat crescent like a boomerang, its middle over the brow, its tips swept back and down
    k.add("hoodmid", hull([(0.06, -0.19, 0.735), (-0.06, -0.19, 0.735), (0.06, -0.19, 0.765), (-0.06, -0.19, 0.765),
                           (0.07, 0.06, 0.745), (-0.07, 0.06, 0.745), (0.07, 0.05, 0.795), (-0.07, 0.05, 0.795),
                           (0.0, -0.09, 0.815), (0.0, -0.21, 0.75)]),
          BROWN, "head", fuse=False, surface="shell")
    k.add("hoodwing", hull([(0.04, -0.19, 0.735), (0.04, -0.19, 0.768), (0.05, 0.03, 0.765), (0.05, 0.03, 0.802),
                            (0.17, -0.1, 0.735), (0.17, -0.09, 0.765), (0.19, 0.04, 0.72), (0.19, 0.05, 0.752),
                            (0.275, 0.13, 0.65), (0.27, 0.14, 0.675)]),
          BROWN, "head", fuse=False, mirror=True, surface="shell")


def add_fins():
    for j, z in enumerate((0.64, 0.58, 0.52, 0.46)):
        b = np.array([0.0, 0.05 + 0.012 * j, z])
        d = unit(0.25, 0.75, -0.62)
        L = 0.11 - 0.012 * j
        side = unit(-0.95, 0.31, 0)
        k.add(f"fin{j}", hull([b + side * 0.016, b - side * 0.016, b + (0, -0.03, 0.03), b + (0, 0.0, -0.035),
                               b + d * L]), BROWN, "body", fuse=False, surface="shell")
    k.add("tail", tube([(0, 0.04, 0.45), (0, 0.15, 0.32), (0, 0.24, 0.18), (0.02, 0.3, 0.06), (0.03, 0.32, 0.02)],
                       [0.05, 0.032, 0.02, 0.011, 0.004]), BROWN, "body")


def add_arms():
    k.add("shoulder", ell(SHOULDER, (0.034, 0.034, 0.034)), BROWN, "arm{s}", mirror=True)
    k.add("arm", tube([SHOULDER, ELBOW, (ELBOW + WRIST) / 2, WRIST], [0.024, 0.021, 0.02, 0.022]), BROWN, "arm{s}",
          mirror=True)


# --- the scythes: a chain of flat-faced hulls along a curve bowed out, the spine outward, the edge in ------------------
def scythe(s):
    root = WRIST * (s, 1, 1)
    e1 = unit(s * 0.97, -0.25, 0.0)            # out (and a little forward) in the blade's plane
    e2 = np.array([0.0, 0.0, 1.0])
    N = np.cross(e1, e2); N /= np.linalg.norm(N)
    us = np.linspace(0, 1, 13)
    def at(u):
        a = 0.105 * math.sin(math.pi * min(u * 1.1, 1.0)) - 0.03 * u + 0.012
        b = 0.035 * (1 - (1 - u) ** 3) * 0 + 0.03 - 0.4 * u
        return root + a * e1 + b * e2
    pts = [at(u) for u in us]
    for i in range(len(us) - 1):
        ring = []
        for j in (i, i + 1):
            u = us[j]
            c = pts[j]
            t = unit(*(pts[min(j + 1, len(us) - 1)] - pts[max(j - 1, 0)]))
            n = np.cross(N, t); n /= np.linalg.norm(n)
            if np.dot(n, e1) < 0:
                n = -n
            taper = (1 - u) ** 0.7 if u > 0.15 else 1.0
            ws, we = 0.012 + 0.016 * taper, 0.004 + 0.05 * taper * math.sin(math.pi * min(u * 1.4 + 0.25, 1))
            th = 0.006 + 0.01 * taper
            ring += [c + n * ws, c - n * we, c + n * ws * 0.2 + N * th, c + n * ws * 0.2 - N * th]
        k.add(f"scythe{'RL'[s < 0]}{i}", hull(ring), BLADE, f"arm{'RL'[s < 0]}", fuse=False, surface="shell")


def add_legs():
    k.add("thigh", tube([HIP, (HIP + KNEE) / 2 + (0.008, 0, 0), KNEE], [0.08, 0.066, 0.044]), BROWN, "leg{s}",
          mirror=True)
    k.add("ham", ell((HIP + KNEE) / 2 + (0.0, 0.01, 0.02), (0.07, 0.062, 0.09), rot=(25, 0, 0)), BROWN, "leg{s}",
          mirror=True)
    k.add("shin", tube([KNEE, (KNEE + ANKLE) / 2, ANKLE], [0.044, 0.034, 0.026]), BROWN, "leg{s}", mirror=True)
    k.add("foot", tube([ANKLE, FOOT + (0, 0.01, 0.0), FOOT + (0, -0.04, -0.006)], [0.024, 0.022, 0.02]), BROWN,
          "leg{s}", mirror=True)
    for j, dx in enumerate((-0.022, 0.022)):
        b = FOOT + (dx, -0.055, -0.004)
        k.add(f"toe{j}", hull([b + (0.012, 0, 0.012), b + (-0.012, 0, 0.012), b + (0.012, 0, -0.012),
                               b + (-0.012, 0, -0.012), b + (dx * 0.3, -0.04, -0.02)]),
              CLAW, "leg{s}", fuse=False, mirror=True, surface="shell")
    b = ANKLE + (0, 0.02, -0.06)
    k.add("heel", hull([b + (0.01, 0, 0.012), b + (-0.01, 0, 0.012), b + (0.01, -0.01, -0.01),
                        b + (-0.01, -0.01, -0.01), b + (0, 0.035, -0.022)]),
          CLAW, "leg{s}", fuse=False, mirror=True, surface="shell")


# --- the eyes: small fierce almonds under the hood's brow ------------------------------------------------------------
EYE_C, EYE_HALF = np.array([0.046, -0.165, 0.712]), (0.04, 0.028)
EYE_SHAPE = [(-1.0, -0.2), (-0.5, 0.3), (0.2, 0.6), (0.8, 0.85), (1.0, 0.6), (0.85, -0.1), (0.4, -0.6),
             (-0.2, -0.75), (-0.7, -0.55)]


def eye_open(u, v):
    c = np.tile(srgb(WHITE), (len(u), 1))
    c[((u + 0.15) / 0.48) ** 2 + ((v + 0.0) / 0.75) ** 2 < 1] = srgb(INK)
    return c


def add_eyes(state):
    look = eye_look(state, eye_open, srgb(BROWN), srgb(INK), line=0.17)
    for s in (1, -1):
        col = look if s > 0 else (lambda a, b, f=look: f(-a, b))
        shape = EYE_SHAPE if s > 0 else [(-a, b) for a, b in EYE_SHAPE]
        k.decal(EYE_C * (s, 1, 1), unit(s * 0.5, -1, 0.05), EYE_HALF, col, eps=0.003, rings=20, segs=56,
                outline=polygon(shape))


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="shell")
    add_body()
    add_head()
    add_fins()
    add_arms()
    for s in (1, -1):
        scythe(s)
    add_legs()
    k.build(voxel=0.006, smooth=4, band=0.006)
    k.eye_states(add_eyes)
    k.export(out)
