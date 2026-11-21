# Samurott-Hisui sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/samurotthisui.py -- <the export's Samurott-Hisui.glb> <out.glb>
#
# Grown from samurott.py (copied here: its build function takes no arguments that reach what changes) and through it
# from oshawott.py (sibling("oshawott")): the same sea lion on four splayed legs, the same eyes as patches under the
# helmet's rim, the same nose standing proud and the same three-banded helmet over the nape. From the references: the
# coat a deeper blue; the helmet navy, its bands curled and edged in red, a row of small red spikes over the nape; the
# horn a navy blade rising from the brow straight UP in a zigzag like a bolt of lightning, with short prongs at its
# kinks (no forward horn); small red eyes; a red nose; a great white beard flowing down the chest and swept out to both
# sides in points, and long white whiskers from the snout sweeping out and down; navy armour plates, each with a red
# blade along its middle, stacked down the outside of all four legs; red pointed claws; the pale grey-blue patch down
# the chest and belly; the tail raised behind, its fin of two lobes, a navy zigzag spike on its back.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, hull, srgb, inside, smoothstep, unit, eye_look, args, sibling

o = sibling("oshawott")
BLUE, PALE, NAVY, RED, WHITE, NOSE = "#2F5FAE", "#A7B6CE", "#272B55", "#B42A40", "#F1F3F7", "#6E1C2E"
CLAW, MOUTH, EYERED, INK = "#A8243A", "#7A1E2E", "#E0402E", o.INK


def eye(u, v):
    """A small red eye with a round black pupil toward the front and a white glint (u outward, v up)."""
    c = np.tile(srgb(EYERED), (len(u), 1))
    c[((u + 0.12) / 0.42) ** 2 + ((v + 0.02) / 0.55) ** 2 < 1] = srgb(INK)
    c[((u - 0.05) / 0.2) ** 2 + ((v - 0.35) / 0.2) ** 2 < 1] = (1.0, 1.0, 1.0)
    c[u * u + v * v > 0.8] = srgb(INK)
    edge = 0.45 + 0.4 * u                 # the helmet's rim cuts it, lower toward the snout
    c[v > edge] = srgb(BLUE)
    return c


def plate(k, name, c, d, out_dir, r, length, spike, bone, span=70, lift=0.014, color=NAVY):
    """A pointed armour plate wrapped round the outer side of a limb (axis d through c, radius r): a flat-faced hull."""
    d = unit(*d); w = np.array(out_dir, float); w = w - d * (w @ d); w /= np.linalg.norm(w)
    s = np.cross(d, w)
    pts = []
    for a in np.linspace(-span, span, 5):
        t = math.radians(a)
        rad = w * math.cos(t) + s * math.sin(t)
        for along in (-length / 2, length / 2):
            for rr in (r * 0.8, r + lift):
                pts.append(np.array(c) + d * along + rad * rr)
    pts.append(np.array(c) + w * (r + spike) + d * (length * 0.5 + spike * 0.6))
    k.add(name, hull(pts), color, bone, fuse=False, surface="shell")


def armour(k, name, c, d, out_dir, r, length, spike, bone):
    """A navy plate with a red blade standing along its middle, as each plate of the references has."""
    plate(k, name, c, d, out_dir, r, length, spike, bone)
    plate(k, name + "red", c, d, out_dir, r + 0.006, length * 0.85, spike * 0.9, bone, span=22, lift=0.016, color=RED)


def claws(k, foot, fwd, bone, n=3):
    fwd = unit(*fwd); side = unit(*np.cross(fwd, (0, 0, 1)))
    for i in range(n):
        off = (i - (n - 1) / 2) * 0.026
        root = np.array(foot) + fwd * 0.028 + side * off
        tip = root + fwd * 0.04 + np.array([0, 0, -0.012])
        pts = [root + side * 0.012 + (0, 0, 0.016), root - side * 0.012 + (0, 0, 0.016), root + side * 0.012 - (0, 0, 0.008),
               root - side * 0.012 - (0, 0, 0.008), tip + (0, 0, -0.006)]
        k.add(f"claw{bone}{i}", hull(pts), CLAW, bone, fuse=False, surface="shell")


def bolt(k, name, pts, widths, bone, color, across, prongs=()):
    """A zigzag blade along points (a hull a stretch, a kite in section, its flat face across `across`)."""
    pts = [np.array(p, float) for p in pts]
    for i in range(len(pts) - 1):
        a, b = pts[i], pts[i + 1]
        d = unit(*(b - a))
        w = np.array(across, float); w = w - d * (w @ d); w /= np.linalg.norm(w)
        t = np.cross(d, w)
        wa, wb = widths[i], widths[i + 1]
        P = [a + w * wa, a - w * wa, a + t * wa * 0.55, a - t * wa * 0.55]
        P += [b + w * wb, b - w * wb, b + t * wb * 0.55, b - t * wb * 0.55] if wb > 0.002 else [b]
        P += [a - d * wa * 0.5]                 # each stretch reaches back into the last, so the joints are closed
        k.add(f"{name}{i}", hull(P), color, bone, fuse=False, surface="shell")
    for j, (at, to, wr) in enumerate(prongs):
        at, to = np.array(at, float), np.array(to, float)
        d = unit(*(to - at))
        w = np.array(across, float); w = w - d * (w @ d); w /= np.linalg.norm(w)
        t = np.cross(d, w)
        k.add(f"{name}p{j}", hull([at + w * wr, at - w * wr, at + t * wr * 0.6, at - t * wr * 0.6, to]), color, bone,
              fuse=False, surface="shell")


def samurott_hisui(src, out):
    k = Kit(src, coat="fur")
    # The body: low behind, the chest lifted in front, a thick neck carrying the head forward and a little higher
    k.add("body", ell((0, 0.04, 0.24), (0.13, 0.2, 0.12)), BLUE, "body")
    k.add("chest", ell((0, -0.1, 0.315), (0.122, 0.12, 0.14)), BLUE, "body")
    k.add("neck", tube([(0, -0.12, 0.36), (0, -0.165, 0.43), (0, -0.19, 0.48)], [0.1, 0.085, 0.075]), BLUE, "head")
    J, G = np.array([0, -0.17, 0.4]), 1.18
    def g(p):
        return J + (np.asarray(p, float) - J) * G
    H = np.array([0, -0.21, 0.51])
    HR = np.array([0.078, 0.085, 0.07])
    k.add("head", ell(H, HR), BLUE, "head")
    # a long snout of the body's blue pushing forward and a little down from under the helmet
    k.add("snout", ell(H + (0, -0.1, -0.04), (0.054, 0.1, 0.046), rot=(15, 0, 0)), BLUE, "head")
    k.add("jaw", ell(H + (0, -0.085, -0.078), (0.046, 0.08, 0.03), rot=(-6, 0, 0)), BLUE, "head")

    # The helmet: navy, three bands sloping back over the nape, each edged in red where it curls; its front band
    # set back and up off the brow, so the face shows under it
    segs = [((0, -0.205, 0.568), (0.082, 0.07, 0.05)), ((0, -0.165, 0.575), (0.094, 0.075, 0.065)),
            ((0, -0.095, 0.555), (0.09, 0.075, 0.065)), ((0, -0.035, 0.51), (0.08, 0.065, 0.06))]
    for i, (c, r) in enumerate(segs):
        k.add(f"helm{i}", ell(c, r, rot=(-10 * i, 0, 0)), NAVY, "head", surface="shell")
    # the curls at the helmet's sides: a knob a side, each band's edge red
    for y, z in ((-0.15, 0.53), (-0.075, 0.51)):
        k.add(f"curl{y:.2f}", ell((0.085, y, z), (0.035, 0.04, 0.04)), NAVY, "head", mirror=True, surface="shell")
    for y in (-0.2, -0.13, -0.065):
        k.paint(lambda P, y=y: (lambda Q: inside(np.abs(Q[:, 1] - y + 0.3 * (Q[:, 2] - 0.56)) - 0.007, 0.002)
                                * (Q[:, 2] > 0.47))(J + (P - J) / G), RED, on=["helm", "curl"])
    # the horn: a navy bolt rising from the brow, its zigzag turned on the diagonal so both cameras see its kinks
    zx, zy = 0.7, 0.7
    def zz(base, a, z):
        return np.array(base, float) + np.array([zx * a, -zy * a * 0.6, z])
    hb = (0, -0.25, 0.57)
    hp = [zz(hb, 0, 0), zz(hb, 0.02, 0.07), zz(hb, -0.035, 0.12), zz(hb, 0.0, 0.2), zz(hb, -0.04, 0.25),
          zz(hb, -0.02, 0.32)]
    bolt(k, "horn", hp, [0.042, 0.038, 0.033, 0.027, 0.02, 0.0], "head", NAVY, (zx, -zy, 0),
         prongs=[(hp[2], hp[2] + np.array([-0.06, 0.03, 0.03]), 0.017), (hp[3], hp[3] + np.array([0.055, -0.02, 0.025]), 0.015)])
    # small red spikes in a row over the nape
    for c, h in (((0, -0.165, 0.625), 0.05), ((0, -0.095, 0.61), 0.055), ((0, -0.03, 0.565), 0.05)):
        c = np.array(c)
        k.add(f"spike{c[1]:.2f}", hull([c + (0.016, 0.016, -0.02), c + (-0.016, 0.016, -0.02), c + (0.014, -0.018, -0.02),
                                        c + (-0.014, -0.018, -0.02), c + (0, 0.035, h)]), RED, "head", fuse=False,
              surface="shell")
    for sx in (1, -1):
        c = np.array((0.105 * sx, -0.12, 0.555))
        k.add(f"sidespike{'RL'[sx < 0]}", hull([c + (0, 0.016, -0.016), c + (0, -0.016, -0.016), c + (0, 0.014, 0.016),
                                               c + (-0.012 * sx, 0, 0), c + (0.04 * sx, 0.025, 0.03)]), RED, "head",
              fuse=False, surface="shell")

    # The moustache: two long white locks from the snout's sides near its tip, sweeping back and down along the neck
    # to pointed ends that curl up, each a tapering flattened blade whose face looks out to its side
    for sx in (1, -1):
        sd = "RL"[sx < 0]
        m = np.array([sx, 1, 1])
        k.add(f"tache{sd}", blade([np.array(p) * m for p in ((0.035, -0.37, 0.475), (0.075, -0.345, 0.46),
                                                              (0.12, -0.29, 0.425), (0.15, -0.22, 0.375),
                                                              (0.16, -0.15, 0.335), (0.15, -0.1, 0.35),
                                                              (0.135, -0.085, 0.38))],
                                  [0.02, 0.032, 0.036, 0.033, 0.024, 0.013, 0.004], (sx * 0.85, -0.25, 0.45), 0.5),
              WHITE, "head", fuse=False, surface="fur")
    # the beard: a white tuft under the chin
    k.add("beard", blade([(0, -0.3, 0.44), (0, -0.295, 0.395), (0, -0.28, 0.345), (0, -0.265, 0.31)],
                         [0.036, 0.04, 0.026, 0.005], (0, 1, 0), 0.55), WHITE, "head", fuse=False, surface="fur")
    # the nose at the snout's tip, dark
    nd = unit(0, -1, 0.1)
    o.nose(k, H + (0, -0.19, -0.05), nd, (0.019, 0.013, 0.015), NOSE, "head")

    # Forelegs splayed out and forward, thick at the shoulder, armour down their outer side
    k.add("shoulder", ell((0.1, -0.13, 0.24), (0.085, 0.09, 0.11)), BLUE, "leg{s}", mirror=True)
    FL = [(0.11, -0.15, 0.22), (0.165, -0.235, 0.11), (0.195, -0.28, 0.035)]
    k.add("foreleg", tube(FL, [0.07, 0.054, 0.044]), BLUE, "leg{s}", mirror=True)
    k.add("forepaw", ell((0.2, -0.3, 0.025), (0.042, 0.05, 0.025), rot=(0, 0, -30)), BLUE, "leg{s}", mirror=True)
    # Hind legs: haunches fused into the rump, shins out and back
    k.add("haunch", ell((0.1, 0.14, 0.21), (0.09, 0.105, 0.11)), BLUE, "hind{s}", mirror=True)
    HL = [(0.12, 0.16, 0.18), (0.165, 0.235, 0.085), (0.185, 0.265, 0.03)]
    k.add("hindleg", tube(HL, [0.068, 0.053, 0.044]), BLUE, "hind{s}", mirror=True)
    k.add("hindpaw", ell((0.19, 0.25, 0.025), (0.042, 0.05, 0.025), rot=(0, 0, 30)), BLUE, "hind{s}", mirror=True)

    for sx in (1, -1):
        side = "RL"[sx < 0]
        m = np.array([sx, 1, 1])
        a, b = np.array(FL[0]) * m, np.array(FL[2]) * m
        d = unit(*(a - b))                     # up the leg
        for i, t in enumerate((0.25, 0.48, 0.7)):
            c = b + (a - b) * t
            armour(k, f"armour{side}{i}", c, d, (sx, 0.15, 0.25), 0.05 + 0.03 * t, 0.06, 0.065, f"leg{side}")
        a, b = np.array(HL[0]) * m, np.array(HL[2]) * m
        d = unit(*(a - b))
        for i, t in enumerate((0.35, 0.65)):
            c = b + (a - b) * t
            armour(k, f"harmour{side}{i}", c, d, (sx, 0.2, 0.3), 0.05 + 0.03 * t, 0.06, 0.045, f"hind{side}")
        claws(k, np.array((0.2, -0.3, 0.02)) * m, (0.4 * sx, -1, 0), f"leg{side}")
        claws(k, np.array((0.19, 0.25, 0.02)) * m, (0.3 * sx, -1, 0), f"hind{side}")

    # The tail raised behind, a fin of two lobes at its end, turned out to +X so both cameras see its faces
    TT = np.array([0.04, 0.4, 0.46])
    k.add("tail", tube([(0, 0.18, 0.3), (0, 0.3, 0.37), (0.02, 0.37, 0.43), TT], [0.075, 0.055, 0.042, 0.034]), BLUE,
          "tail")
    fd = unit(0.55, 0.8, 0.25)
    s, n, _ = o.frame(fd, roll=-30)
    for j, lobe in enumerate((unit(*(fd + n * 0.75)), unit(*(fd - n * 0.6)))):
        c = TT + lobe * 0.075
        V, F = ell((0, 0, 0), (0.016, 0.085, 0.04))
        ls = np.cross(lobe, s); ls /= np.linalg.norm(ls)
        V = V @ np.stack([s, lobe, ls]) + c
        k.add(f"fin{j}", (V, F), BLUE, "tail")
    k.paint(lambda P: inside(np.linalg.norm(P - (TT + fd * 0.1), axis=1) - 0.06, 0.004) * (
        ((P - TT) @ s) < 0.004), PALE, on=["fin"])
    # a navy zigzag spike on the tail's back
    tb = np.array([0.0, 0.3, 0.4])
    bolt(k, "tailspike", [tb, tb + (0.015, 0.03, 0.05), tb + (-0.01, 0.0, 0.09), tb + (0.02, 0.04, 0.14)],
         [0.022, 0.018, 0.013, 0.0], "tail", NAVY, (1, 0, 0))

    # The pale patch down the chest and belly, its top a V
    def belly(P):
        top = 0.37 - 0.9 * np.abs(P[:, 0])
        front = smoothstep(-0.02, -0.08, P[:, 1] + 0.25 * (P[:, 2] - 0.2))
        return inside(P[:, 2] - top, 0.004) * inside(np.abs(P[:, 0]) - 0.085, 0.004) * front * (P[:, 2] > 0.12)
    k.paint(belly, PALE, on=["chest", "body"])
    # the mouth: a small dark smile under the nose
    INKC, BL = srgb(MOUTH), srgb(BLUE)
    def mouth(u, v):
        c = np.tile(BL, (len(u), 1))
        c[(v < 0.35 - 0.3 * u * u) & (u * u + v * v < 0.85)] = INKC
        return c
    md = unit(0, -0.75, -0.65)
    k.decal(g(H + (0, -0.16, -0.085)), md, (0.03, 0.02), mouth, eps=0.003, rings=16, segs=56)

    # the head and all it wears grown about the neck, as the game wants a face to read
    for p in k.parts:
        if p["bone"] == "head" and p["name"] != "neck":
            p["V"] = J + (np.asarray(p["V"]) - J) * G
    k.build(voxel=0.006, smooth=4, band=0.004)
    E = [(g(o.at_surface(H, HR, d)), d, sx) for sx in (1, -1) for d in [unit(0.6 * sx, -0.79, 0.06)]]
    k.eye_states(lambda st: o.eyes(k, E, (0.024, 0.018), st, BLUE, open_look=eye))
    k.export(out)


if __name__ == "__main__":
    samurott_hisui(*args())
