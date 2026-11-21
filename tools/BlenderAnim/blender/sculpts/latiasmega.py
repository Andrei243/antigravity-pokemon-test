# Latias's Mega Evolution sculpted on the skeleton and clips of the game's own export of the form (sculptkit.py):
#   blender --background --factory-startup --python sculpts/latiasmega.py -- <the export's Latias-Mega.glb> <out.glb>
#
# Grown from latias.py (its helpers through sibling; its frame of a jet flying level, its head, neck, arms and eyes
# copied here, since lati() takes no argument that reaches the Mega's wings). From the references: lavender where
# Latias is red, white neck and chest running down to a point between two purple hips; the head Latios's way, a
# lavender cap and nose with the eye at its lower edge; two white crests standing up in a V from the back of the
# skull. The Mega's one great change, its largest mass: each wing is a huge rounded pod on the shoulder, longer than
# the neck and head, with a hollow pale triangle on its outer face near its front, three white claws at its rear tip
# and a long flat blade sweeping out and back from its top, serrated at its end; the arms are short pale stubs that
# run into the pods. The lower body is fuller, with two long fins hanging back from the hips and the tail's fin.
# Latios-Mega (latiosmega.py) is built by mega() with its own eyes and measures.
# Bones: root 0; body (0, 0, 0.189); head (0, -0.101, 0.189); armR (0.06, -0.1, 0.149); wingR (0.06, 0, 0.249);
# tail (0, 0.18, 0.129).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, polygon, args, slab, sibling

lat = sibling("latias")
on_ell, kite = lat.on_ell, lat.kite

LATIAS_MEGA = dict(
    MAIN="#9C86DE", WHITE="#F0F0F7", GREY="#CFCBE0", MARK="#E2DEF0", IRIS="#F4C21E", IRIS_DEEP="#C78A10",
    GROW=1.0, LIFT=0.0,
)

INKH = "#16121A"


def mega(src, out, P):
    k = Kit(src, coat="fur")
    G = P["GROW"]
    B = np.array(k.arm.data.bones["body"].head_local) + np.array([0, 0, P["LIFT"]])
    MAIN, WHITE, GREY = P["MAIN"], P["WHITE"], P["GREY"]

    def at(*o):
        return B + G * np.array(o, float)

    # --- the body: a plump teardrop, fuller below than Latias's ----------------------------------------------------
    BODY_C, BODY_R = at(0, 0.03, -0.012), G * np.array([0.096, 0.155, 0.1])

    def teardrop(V):
        V = V.copy()
        t = np.clip(V[:, 1], 0, 1)
        s = 1 - 0.36 * t ** 1.3
        V[:, 0] *= s; V[:, 2] *= s
        V[:, 2] -= 0.12 * (1 - V[:, 1] ** 2) * (V[:, 2] < 0)
        return V

    k.add("body", ell(BODY_C, BODY_R, deform=teardrop, sub=5), MAIN, "body")
    k.add("chest", ell(at(0, -0.085, 0.012), G * np.array([0.08, 0.08, 0.084])), WHITE, "body")
    # the hips: two purple masses either side of the white's point
    k.add("hip", ell(at(0.052, 0.07, -0.06), G * np.array([0.062, 0.1, 0.07]), rot=(-12, 0, 0)), MAIN, "body",
          mirror=True)
    TAIL = [at(0, 0.11, -0.015), at(0, 0.2, -0.05), at(0, 0.29, -0.085)]
    k.add("tail", tube(TAIL, [G * 0.07, G * 0.044, G * 0.024]), MAIN, "tail")

    # the hind fins: two long solid fins hanging back from the hips
    k.add("hindfin", kite(at(0.055, 0.12, -0.1), at(0.085, 0.29, -0.19), (0, 1, 0.3), (1, 0, -0.35),
                          G * 0.08, G * 0.03, tip_thick=0.5), MAIN, "tail", fuse=False, mirror=True)

    # --- the neck and the head --------------------------------------------------------------------------------------
    H, HR = at(0, -0.27, 0.178), G * np.array([0.08, 0.115, 0.078])

    def nose(V):
        V = V.copy()
        s = 1 - 0.3 * np.clip(-V[:, 1], 0, 1) ** 1.6
        V[:, 0] *= s; V[:, 2] *= s
        return V

    k.add("neck", tube([at(0, -0.1, 0.02), at(0, -0.175, 0.1), H + G * np.array([0, 0.06, -0.022])],
                       [G * 0.07, G * 0.056, G * 0.052]), WHITE, "head")
    k.add("head", ell(H, HR, deform=nose, sub=5), WHITE, "head")

    # the head's crests: two white fins standing up in a V from the back of the skull, a second point under each
    k.add("crest", kite(H + G * np.array([0.03, 0.05, 0.035]), H + G * np.array([0.085, 0.115, 0.135]),
                        (0, 1, -0.4), (1, -0.2, -0.45), G * 0.075, G * 0.03), WHITE, "head", fuse=False, mirror=True)
    k.add("crest2", kite(H + G * np.array([0.04, 0.075, 0.01]), H + G * np.array([0.1, 0.16, 0.06]),
                         (0, 1, -0.3), (1, -0.2, -0.6), G * 0.05, G * 0.026), GREY, "head", fuse=False, mirror=True)

    # --- the arms: short pale stubs from the chest running up into the pods --------------------------------------
    S, M = at(0.07, -0.1, -0.018), at(0.1, -0.075, 0.03)
    k.add("arm", tube([S, M], [G * 0.032, G * 0.028]), GREY, "arm{s}", mirror=True)

    # --- the wings: great rounded pods on the shoulders ----------------------------------------------------------
    POD_C, POD_R = at(0.14, 0.02, 0.075), G * np.array([0.072, 0.21, 0.08])

    def pod(V):
        V = V.copy()
        t = np.clip(V[:, 1], 0, 1)                      # tapered behind to the claws' tip
        s = 1 - 0.55 * t ** 1.4
        V[:, 0] *= s; V[:, 2] *= s
        V[:, 2] += 0.25 * t ** 2                        # the rear tip lifting a little
        return V

    k.add("pod", ell(POD_C, POD_R, deform=pod, sub=5), MAIN, "wing{s}", mirror=True)

    # three white claws at the pod's rear tip, pointing back and down
    tipc = POD_C + np.array([0, POD_R[1] * 0.86, POD_R[2] * 0.12])
    for i, dz in enumerate((0.012, -0.004, -0.02)):
        base = tipc + G * np.array([0.004, -0.02, dz])
        tip = tipc + G * np.array([0.012, 0.035, dz * 1.6 - 0.02])
        k.add(f"claw{i}", kite(base, tip, (1, 0, 0), (0, -0.3, 1), G * 0.022, G * 0.016), WHITE, "wing{s}",
              fuse=False, mirror=True)

    # the blade: long and flat, out and back from the pod's top, raised in a V, serrated at its end
    BLADE = [(0, -0.1), (0.1, -0.01), (0.2, 0.1), (0.27, 0.19), (0.24, 0.185), (0.25, 0.215), (0.205, 0.2),
             (0.21, 0.228), (0.16, 0.215), (0.0, 0.17)]
    W = [(a * G, b * G) for a, b in BLADE]
    for side in (1, -1):
        dih = math.radians(50)
        ex = np.array([side * math.cos(dih), 0.0, math.sin(dih)])
        org = POD_C * np.array([side, 1, 1]) + G * np.array([side * 0.01, 0.0, 0.045])
        nm = "wing" + ("R" if side > 0 else "L")
        k.add("blade" + ("R" if side > 0 else "L"), slab(W, org, ex, unit(0, 1, 0.05), G * 0.03), MAIN, nm,
              fuse=False)

    # --- the tail's fin: three points, banked 35 degrees so its face shows from behind ---------------------------
    td = unit(*(TAIL[-1] - TAIL[-2]))
    x0 = np.array([1.0, 0, 0]); x0 = x0 - td * (x0 @ td); x0 /= np.linalg.norm(x0)
    bank = math.radians(35)
    fx = math.cos(bank) * x0 + math.sin(bank) * np.cross(td, x0)
    TF = [(0, -0.03), (0.05, 0.03), (0.07, 0.1), (0.03, 0.06), (0, 0.11), (-0.03, 0.06), (-0.07, 0.1), (-0.05, 0.03)]
    k.add("tailfin", slab([(a * G, b * G) for a, b in TF], TAIL[-1] - td * G * 0.01, fx, td, G * 0.024), MAIN,
          "tail", fuse=False)

    # --- colour: white neck and chest down to a point between the hips ------------------------------------------
    TOPZ, PT = B[2] + G * 0.012, B[1] + G * 0.05
    k.paint(lambda Q: inside(Q[:, 2] - TOPZ, 0.004)
            * inside(Q[:, 1] - (PT - 1.1 * np.abs(Q[:, 0] - B[0]) + 0.35 * (Q[:, 2] - TOPZ)), 0.004),
            WHITE, on=["body", "chest", "neck"])
    k.paint(lambda Q: inside(np.linalg.norm((Q - H) / HR, axis=1) - 1.05, 0.004), WHITE)   # the head whole again

    # the eye's place, and the lavender cap of the head's front with a white oval round the eye
    E, EN = on_ell(H, HR, (0.78, -0.5, 0.18))
    E = E - EN * 0.004

    def cap(Q):
        front = inside(Q[:, 1] - (E[1] + G * 0.016 + 0.35 * (Q[:, 2] - E[2])), 0.002)
        low = inside(Q[:, 2] - (E[2] - G * 0.012) + 0.3 * (Q[:, 1] - E[1]), 0.002)          # white under the nose
        oval = inside(((Q[:, 1] - E[1] + G * 0.004) / (G * 0.036)) ** 2 + ((Q[:, 2] - E[2] + G * 0.008) / (G * 0.026))
                      ** 2 - 1, 0.06) * inside(G * 0.03 - np.abs(Q[:, 0]), 0.003)
        near = inside(np.linalg.norm(Q - H, axis=1) - G * 0.145, 0.003)
        return np.clip(front - oval - low, 0, 1) * near

    k.paint(cap, MAIN)

    # --- the hollow triangle on each pod's outer face, near its front ---------------------------------------------
    TRI = polygon([(-1, 0.75), (1, 0.75), (0, -1)])
    TP, TN = on_ell(POD_C, POD_R, (1, -0.55, 0.35))
    for sx in (1, -1):
        m = np.array([sx, 1, 1.0])
        k.decal(TP * m, TN * m, (G * 0.034, G * 0.034), P["MARK"], eps=0.004, outline=TRI, rings=10, segs=60,
                up=(0, 0, 1))
        k.decal(TP * m, TN * m, (G * 0.019, G * 0.019), MAIN, eps=0.0065, outline=TRI, rings=8, segs=60, up=(0, 0, 1))

    # --- the mouth: a small open mouth under the nose's tip ----------------------------------------------------------
    MP, MN = on_ell(H, HR, (0, -0.86, -0.5))
    k.decal(MP, MN, (G * 0.018, G * 0.008), "#B8505E", eps=0.003, rings=10, segs=48,
            outline=polygon([(-1, 0.6), (1, 0.6), (0.45, -0.7), (-0.45, -0.7)]))

    k.build(voxel=0.0058, smooth=4, band=0.005)

    # --- the eyes: a bold iris, a black pupil, a glint, a fierce slant over the top --------------------------------
    LID, INK = srgb(WHITE), srgb(INKH)
    IRIS, DEEP, GLINT = srgb(P["IRIS"]), srgb(P["IRIS_DEEP"]), srgb("#FFFFFF")

    def eye(u, v):
        r = np.sqrt(u * u + v * v)
        c = np.tile(IRIS, (len(u), 1))
        c[v < -0.25 - 0.1 * u] = DEEP
        c[(u / 0.4) ** 2 + ((v + 0.02) / 0.55) ** 2 < 1] = INK
        c[((u - 0.2) / 0.2) ** 2 + ((v - 0.3) / 0.2) ** 2 < 1] = GLINT
        c[r > 0.83] = INK
        return c

    def eyes(state):
        k.decal(E, EN, (G * 0.028, G * 0.026), eye_look(state, eye, LID, INK, line=0.18), eps=0.005, up=(0, 0, 1),
                rings=20, segs=56, mirror=True)

    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    src, out = args()
    mega(src, out, LATIAS_MEGA)
