# Appletun sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/appletun.py -- <the export's Appletun.glb> <out.glb>
#
# Grown from Applin's script (sibling("applin")): the line's red and greens, its on_ell, the scalloped two-tone skin
# (scallop) for the cap's pink front, its lobe shape for the leaves. From the references: a great round body like an
# apple pie, its top half pale golden crust under a lattice of thick orange strips, its lower half green, on four
# short fat legs with round toes; out of the top a little stalk and two leaves in a V, one crimson and one green; a
# short pointed tail lying on the ground behind; in front a long low neck, cream underneath, lying along the ground to
# a broad flat head whose chin rests near the ground; on the head a red cap of apple skin, its front pink below a
# scalloped edge; eyes shut in sleepy arcs just under the cap's rim, a small smile.
# (The task gave it "leaf-like ears": the pictures have none; the leaves stand on the body's top, and the head wears a
# red cap. The eyes are shut in both pictures, so the Open state is the sleepy arc.)
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, sheet, srgb, inside, unit, eye_look, args, outward, sibling

a = sibling("applin")
GREEN, GREEN_DARK = "#4FA05A", "#3C8048"
CRUST, LATTICE = "#F4D07E", "#EC9C44"
RED, PINK = "#B42240", "#E9667E"
LEAF_RED, LEAF_GREEN, STALK = "#B41E3C", "#9CCB78", "#7A5432"
CREAM = "#EEDDA0"
INK, MOUTH = "#2A1A14", "#5A2028"

# --- Appletun's frame (joints: body (0, 0, 0.24), head (0, -0.18, 0.2), tail (0, 0.25, 0.2), legs (+-0.13,
#     -0.14, 0.14), hinds (+-0.13, 0.15, 0.14); about 0.55 tall) ------------------------------------------------------
BODY_C = np.array([0.0, 0.045, 0.25])
BODY_R = np.array([0.222, 0.232, 0.235])
CRUST_Z = 0.245                            # where the crust gives way to the green
HEAD_C = np.array([0.0, -0.29, 0.108])
HEAD_R = np.array([0.102, 0.096, 0.076])
CAP_C = np.array([0.0, -0.29, 0.134])
CAP_R, CAP_H = 0.112, 0.07
LX = 0.135


def bottom(V):
    V = V.copy()
    V[:, 2] = np.where(V[:, 2] < 0, V[:, 2] * 0.9, V[:, 2])
    return V


def body_point(kx, t):
    """A point on the body's dome along a strip: kx across (-1..1), t the angle from the front over the top."""
    w = math.sqrt(max(1 - kx * kx, 0))
    return BODY_C + BODY_R * np.array([kx, -math.cos(t) * w, math.sin(t) * w])


def strip(pts, lift, width=0.066, thick=0.017):
    """A flat strip of crust laid on the dome along pts, its face to the dome's normal."""
    P = np.array(pts, float)
    N = (P - BODY_C) / BODY_R ** 2
    N /= np.linalg.norm(N, axis=1, keepdims=True)
    T = np.gradient(P, axis=0); T /= np.linalg.norm(T, axis=1, keepdims=True)
    S = np.cross(N, T)
    M = P + N * lift
    def at(u, v):
        i = np.clip(np.round(u * (len(P) - 1)).astype(int), 0, len(P) - 1)
        return M[i] + S[i] * ((v - 0.5) * width)[..., None]
    return outward(sheet(at, len(P), 5, thick))


def lattice(k):
    for j, kx in enumerate((-0.52, 0.0, 0.52)):
        w = math.sqrt(1 - kx * kx)
        t0 = math.asin(min(1, (CRUST_Z + 0.03 - BODY_C[2]) / (BODY_R[2] * w)))
        ts = np.linspace(t0, math.pi - t0, 40)
        k.add(f"latticea{j}", strip([body_point(kx, t) for t in ts], 0.002), LATTICE, "body", fuse=False,
              surface="shell")
    for j, ky in enumerate((-0.45, 0.05, 0.55)):
        w = math.sqrt(1 - ky * ky)
        t0 = math.asin(min(1, (CRUST_Z + 0.03 - BODY_C[2]) / (BODY_R[2] * w)))
        ts = np.linspace(t0, math.pi - t0, 40)
        pts = [BODY_C + BODY_R * np.array([-math.cos(t) * w, ky, math.sin(t) * w]) for t in ts]
        k.add(f"latticeb{j}", strip(pts, 0.0065), LATTICE, "body", fuse=False, surface="shell")


def leaves(k):
    top = BODY_C + np.array([0, 0, BODY_R[2]])
    k.add("stalk", tube([top - (0, 0, 0.02), top + (0, 0, 0.03)], [0.013, 0.011]), STALK, "body", fuse=False)
    base = top + np.array([0, 0, 0.026])
    for name, colour, deg in (("leafred", LEAF_RED, (38, 0, -40)), ("leafgreen", LEAF_GREEN, (-38, 0, -40))):
        m = a.rot(deg)
        c = base + m @ np.array([0, 0, 0.05])
        k.add(name, outward(ell(c, (0.046, 0.0125, 0.052), rot=deg, deform=a.lobe_shape)), colour, "body",
              fuse=False, surface="leaf")


def legs(k):
    for name, y, bone in (("fore", -0.14, "leg{s}"), ("hind", 0.15, "hind{s}")):
        k.add(name + "top", ell((LX, y, 0.12), (0.078, 0.085, 0.095)), GREEN, bone, mirror=True)
        k.add(name + "foot", ell((LX + 0.006, y - 0.012, 0.034), (0.068, 0.078, 0.036)), GREEN, bone, mirror=True)
        for j, dx in enumerate((-0.036, 0.0, 0.036)):
            k.add(f"{name}toe{j}", ell((LX + 0.006 + dx, y - 0.08, 0.024), (0.02, 0.02, 0.02)), GREEN, bone,
                  mirror=True)


def cap_point(th, t, grow=0.0):
    """The cap's dome: th round its upright axis (0 ahead), t from the rim (0) to its top (1)."""
    rho = (CAP_R + grow) * np.cos(t * math.pi / 2) * (1 + 0.12 * np.clip(1 - t * 2.5, 0, 1))
    return np.stack([CAP_C[0] + rho * np.sin(th), CAP_C[1] - rho * np.cos(th) * 1.05,
                     CAP_C[2] + (CAP_H + grow) * np.sin(t * math.pi / 2) - 0.008 * (1 - t)], axis=-1)


def cap(k):
    def red(u, v):
        return cap_point(math.pi + 2 * math.pi * u, v * 0.97)
    k.add("cap", outward(sheet(red, 61, 14, 0.016)), RED, "head", fuse=False, surface="shell")
    k.add("captop", ell(CAP_C + (0, 0, CAP_H - 0.004), (0.012, 0.012, 0.008)), RED, "head", fuse=False,
          surface="shell")
    # The pink front: a solid of the cap's own thickness sunk into it, its face just proud, its top edge scalloped
    def pink(u, v):
        th = (u - 0.5) * math.radians(150)
        top = a.scallop(th, 9, 0.34, 0.22, 0.5) * (1 - 0.4 * np.abs(th) / math.radians(75))
        return cap_point(th, v * top, grow=0.004)
    k.add("cappink", outward(sheet(pink, 37, 8, 0.012)), PINK, "head", fuse=False, surface="shell")


EYE_D = (0.5, -0.86, -0.04)
EYE_HALF = (0.021, 0.015)


def sleepy(u, v):
    """The open eye: shut in a sleepy arc."""
    c = np.tile(srgb(GREEN), (len(u), 1))
    c[(np.abs(v - (0.05 - 0.45 * (1 - u * u))) < 0.22) & (np.abs(u) < 0.86)] = srgb(INK)
    return c


def fierce(u, v):
    c = np.tile(srgb(GREEN), (len(u), 1))
    c[(np.abs(v - (-0.15 + 0.4 * u)) < 0.22) & (np.abs(u) < 0.86)] = srgb(INK)
    return c


def lay_eyes(k, state):
    p, n = a.on_ell(HEAD_C, HEAD_R, EYE_D)
    look = fierce if state == "Fierce" else eye_look(state, sleepy, srgb(GREEN), srgb(INK), line=0.2)
    k.decal(p, n, EYE_HALF, look, up=(0, 0, 1), eps=0.002, rings=20, segs=56, mirror=True)


def appletun(src, out):
    k = Kit(src, coat="fur")
    k.add("body", ell(BODY_C, BODY_R, deform=bottom, sub=5), GREEN, "body")
    k.paint(lambda P: inside(CRUST_Z - P[:, 2], 0.02), CRUST, on=["body"])
    legs(k)
    k.add("tail", tube([(0, 0.24, 0.14), (0, 0.3, 0.08), (0, 0.345, 0.03)], [0.07, 0.042, 0.012]), GREEN, "tail")
    k.add("neck", tube([(0, -0.14, 0.17), (0, -0.2, 0.12), (0, -0.26, 0.105)], [0.078, 0.068, 0.064]), GREEN, "head")
    k.add("head", ell(HEAD_C, HEAD_R), GREEN, "head")
    k.paint(lambda P: inside(P[:, 2] - 0.068, 0.006) * (P[:, 1] < -0.15), CREAM, on=["neck", "head"])
    # the smile under the eyes
    p, n = a.on_ell(HEAD_C, HEAD_R, (0, -1, -0.32))
    def smile(u, v):
        c = np.tile(srgb(GREEN), (len(u), 1))
        c[(np.abs(v - (-0.35 + 0.7 * u * u)) < 0.26) & (np.abs(u) < 0.9)] = srgb(MOUTH)
        return c
    k.decal(p, n, (0.022, 0.01), smile, eps=0.002, rings=10, segs=40)
    lattice(k)
    leaves(k)
    cap(k)
    k.build(voxel=0.006, band=0.008, wband=0.05)
    k.eye_states(lambda state: lay_eyes(k, state))
    k.export(out)


if __name__ == "__main__":
    src, out_path = args()
    appletun(src, out_path)
