# Drednaw sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/drednaw.py -- <the export's Drednaw.glb> <out.glb>
#
# Chewtle grown (its frame through sibling("chewtle"): the eye cut flat by its brow, the claws, the spikes, a point on
# an ellipsoid). From the references: a great snapping turtle low on four thick teal legs, longer than it is tall. The
# largest mass is the shell: a rust-brown dome of rounded plates, skirted all round by a broad ochre-yellow rim that
# sweeps up at the front into a hood over the neck. The head is the second mass, big and blocky on a thick neck: teal,
# under a yellow helmet that juts forward over the snout into a point with a nostril and rises to a peak over the crown;
# a huge cream lower jaw like a scoop, its top edge a zigzag of teeth under a red gum line, a fang at its front; small
# fierce white eyes under a heavy black brow; a yellow knob at each jaw hinge. Yellow spikes on the elbows, white claws,
# a cream plastron under the body, a thick tail with yellow spikes along it, two more at the shell's back.
# Reference ratios (3D picture): height / length 0.6, the shell 0.62 of the length and the head with its jaw 0.3 of it
# and 0.55 of the height, the belly 0.18 of the height off the ground.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, sheet, outward, srgb, inside, unit, boxy, tri, sibling, args

ch = sibling("chewtle")
spike, claws, on_ell, normal_at, lay_eyes, eye_open = ch.spike, ch.claws, ch.on_ell, ch.normal_at, ch.lay_eyes, ch.eye_open

TEAL, YELLOW, BROWN, SEAM, CREAM = "#47C2BE", "#F0B428", "#B8481C", "#7E2C12", "#F4D68C"
GUM, INK, CLAW, KNOB = "#D24A40", "#1A1E22", "#F6F4EE", "#F2BC2E"
DARKTEAL = "#2E8C8A"

SC = np.array([0.0, 0.07, 0.33])          # the shell's dome
SR = np.array([0.255, 0.34, 0.2])
HC = np.array([0.0, -0.38, 0.305])        # the head's block
HR = np.array([0.128, 0.14, 0.118])
VOXEL = 0.0075


def dome_point(th, e, scale=1.0):
    """A point of the shell's dome: th round from the front (+ toward +X), e its elevation (radians)."""
    th, e = np.asarray(th, float), np.asarray(e, float)
    return np.stack([SC[0] + SR[0] * scale * np.cos(e) * np.sin(th),
                     SC[1] - SR[1] * scale * np.cos(e) * np.cos(th),
                     SC[2] + SR[2] * scale * np.sin(e)], axis=-1)


def rim():
    """The ochre rim round the dome, sweeping up at the front into a hood that stands off the shell."""
    def at(u, v):
        th = (u - 0.5) * 2 * math.pi + math.pi            # the seam behind
        th = np.where(th > math.pi, th - 2 * math.pi, th)
        front = np.exp(-(th / 0.75) ** 2)
        e_top = 0.2 + 0.95 * front
        e = -0.2 + (e_top + 0.2) * v
        scale = 1.045 + 0.06 * (1 - v) + 0.1 * v * v * front
        return dome_point(th, e, scale)
    return outward(sheet(at, 97, 9, 0.024))


def drednaw(src, out):
    k = Kit(src, coat="shell")

    # --- the body under the shell, cream beneath -----------------------------------------------------------
    k.add("body", ell((0, 0.03, 0.248), (0.215, 0.33, 0.145)), TEAL, "body", surface="skin")
    k.paint(lambda P: inside(P[:, 2] - 0.2, 0.004) * (np.abs(P[:, 0]) < 0.2) * (np.abs(P[:, 1] - 0.03) < 0.3),
            CREAM, on=["body"])

    # --- the shell: a rust dome of rounded plates and its ochre rim ------------------------------------------
    k.add("shell", ell(SC, SR), BROWN, "body")
    def seams(P):
        p = (P - SC) / SR
        e = np.arcsin(np.clip(p[:, 2], -1, 1))
        th = np.arctan2(p[:, 0], -p[:, 1])
        ring = inside(np.abs(e - 0.78) - 0.025, 0.006)
        merid = inside(np.abs(tri(th / 1.15 + 0.5)) - 0.0 - 1 + 0.045, 0.01) * (e > 0.12) * (e < 0.78)
        top = inside(np.abs(p[:, 0] / 1.0) - 0.035, 0.008) * (e > 0.78) * (np.abs(p[:, 1]) < 0.45)
        return np.clip(ring + merid + top, 0, 1) * (np.linalg.norm(p, axis=1) > 0.97)
    k.paint(seams, SEAM, on=["shell"])
    # under the rim the dome is the body's: teal, so no brown shows between the rim and the legs
    k.paint(lambda P: inside(P[:, 2] - (SC[2] - 0.045), 0.004), TEAL, on=["shell"])
    k.add("rim", rim(), YELLOW, "body", fuse=False)
    for i, (y, z) in enumerate(((0.37, 0.26), (0.41, 0.3))):
        k.add(f"backspike{i}", spike((0.06 + 0.07 * i, y, z), (0.35, 0.8, 0.45), 0.075, 0.03, 0.022), YELLOW, "body",
              fuse=False, mirror=True)

    # --- thick legs: shoulders fused into the flanks, short shins, wide feet with white claws, spiked elbows ---
    FOOT = (0.072, 0.082, 0.04)
    for nm, bone, y in (("leg", "leg{s}", -0.17), ("hind", "hind{s}", 0.17)):
        dy = -0.02 if nm == "leg" else 0.02
        k.add(nm + "top", ell((0.15, y, 0.22), (0.09, 0.1, 0.1)), TEAL, bone, mirror=True, surface="skin")
        k.add(nm, tube([(0.16, y, 0.2), (0.185, y + dy, 0.12), (0.195, y + dy * 1.5, 0.06)], [0.078, 0.07, 0.066]),
              TEAL, bone, mirror=True, surface="skin")
        foot = (0.2, y + dy * 2 - 0.02, 0.038)
        k.add(nm + "foot", ell(foot, FOOT), TEAL, bone, mirror=True, surface="skin")
        claws(k, foot, FOOT, 3, CLAW, bone, size=1.0, spread=0.6)
        # a dark segment line round the leg
        k.paint(lambda P, y=y: inside(np.abs(P[:, 2] - 0.135) - 0.004, 0.002) * (np.abs(np.abs(P[:, 0]) - 0.18) < 0.09)
                * (np.abs(P[:, 1] - y) < 0.09), DARKTEAL, on=[nm])
    k.add("elbow", spike((0.24, -0.14, 0.19), (0.7, 0.55, 0.35), 0.07, 0.03, 0.02), YELLOW, "leg{s}", fuse=False,
          mirror=True)

    # --- the tail: thick, back and a little out to +X, yellow spikes along its top --------------------------
    T = [(0, 0.3, 0.25), (0.03, 0.42, 0.21), (0.08, 0.53, 0.17)]
    k.add("tail", tube(T, [0.075, 0.05, 0.022]), TEAL, "tail", surface="skin")
    for i, t in enumerate((0.25, 0.55, 0.82)):
        p = np.array(T[0]) + (np.array(T[2]) - np.array(T[0])) * t
        r = 0.075 + (0.022 - 0.075) * t
        k.add(f"tailspike{i}", spike(p + (0, 0, r * 0.7), (0.3, 0.6, 1), 0.07 - 0.02 * t, 0.026 - 0.008 * t, 0.018),
              YELLOW, "tail", fuse=False)

    # --- the head: a blocky teal head on a thick neck, a huge cream jaw ---------------------------------------
    k.add("neck", tube([(0, -0.15, 0.3), (0, -0.25, 0.31), (0, -0.31, 0.31)], [0.105, 0.1, 0.1]), TEAL, "head",
          surface="skin")
    k.add("head", ell(HC, HR, deform=boxy(2.6)), TEAL, "head", surface="skin")
    k.add("snout", ell((0, -0.47, 0.31), (0.1, 0.075, 0.075), deform=boxy(2.4)), TEAL, "head", surface="skin")
    k.add("jaw", ell((0, -0.43, 0.2), (0.128, 0.13, 0.072), deform=boxy(2.4)), CREAM, "head", surface="skin")
    # The mouth: the jaw's top a zigzag of teeth, a red gum line over it, a dark lip line over that
    def zig(P):
        p = P - HC
        th = np.arctan2(p[:, 0], -p[:, 1])
        return 0.238 + 0.026 * tri(th * 2.2 + 0.5)
    front = lambda P: (P[:, 1] < -0.3) * (P[:, 2] > 0.12) * (P[:, 2] < 0.34)
    k.paint(lambda P: inside(P[:, 2] - zig(P), 0.0025) * front(P), CREAM, on=["jaw", "head", "snout"])
    k.paint(lambda P: inside(np.abs(P[:, 2] - zig(P) - 0.014) - 0.012, 0.0025) * front(P), GUM)
    k.paint(lambda P: inside(np.abs(P[:, 2] - zig(P) - 0.03) - 0.0045, 0.002) * front(P), INK)
    k.add("fang", spike((0.045, -0.545, 0.235), (0.15, -0.3, 1), 0.06, 0.018, 0.014), CREAM, "head", fuse=False,
          mirror=True)
    k.add("knob", ell((0.128, -0.33, 0.265), (0.035, 0.042, 0.048)), KNOB, "head", fuse=False, mirror=True)

    # The yellow helmet: over the crown, jutting forward past the snout to a point, a peak over the crown
    helm = [(0, -0.62, 0.37), (0.05, -0.56, 0.34), (-0.05, -0.56, 0.34), (0.1, -0.46, 0.37), (-0.1, -0.46, 0.37),
            (0.115, -0.32, 0.4), (-0.115, -0.32, 0.4), (0.07, -0.25, 0.43), (-0.07, -0.25, 0.43),
            (0, -0.31, 0.56), (0, -0.5, 0.445), (0.04, -0.42, 0.47), (-0.04, -0.42, 0.47)]
    k.add("helmet", hull(helm), YELLOW, "head", fuse=False)
    k.add("nostril", ell((0, -0.598, 0.36), (0.012, 0.01, 0.012)), INK, "head", fuse=False)

    k.build(voxel=VOXEL, smooth=4, band=0.006)
    V = np.array([v.co[:] for v in k.ob.data.vertices])
    a = math.radians(31.5)
    proj = V[:, 0] * math.cos(a) + V[:, 1] * math.sin(a)
    h, w, l = V[:, 2].max(), np.ptp(V[:, 0]), np.ptp(V[:, 1])
    print("KIT: board: shows %.3f in a square of %.3f" % (np.ptp(proj), 1.3 * max(h, 0.85 * max(w, l))))

    # Small fierce eyes on the head's sides under the helmet, a heavy brow sloping down to the snout
    E = on_ell(HC, HR * 1.04, 0.098, 0.345)
    de = unit(*(normal_at(HC, HR, E) + np.array((0, -0.55, 0.05))))
    cut = lambda u: 0.2 + 0.35 * u
    k.eye_states(lambda s: lay_eyes(k, s, E, de, (0.03, 0.026), eye_open(0.15), TEAL, cut, line=0.22, eps=0.003))
    k.export(out)


if __name__ == "__main__":
    drednaw(*args())
