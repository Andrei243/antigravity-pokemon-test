# Linoone-Galar sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/linoonegalar.py -- <the export's Linoone-Galar.glb> <out.glb>
# Linoone's frame (linoone.py: the long tube of a body, the legs, the head, the tail and its frame) in Galar's colours,
# with Zigzagoon-Galar's face (zigzagoongalar.py: the star round the eye, the pink half-lidded eye, the hanging tongue).
# From the references: a long, low black weasel; a broad white band along its whole top from the crown to the tail's
# end, a black streak down the middle of it; on each flank behind the shoulder a white burst; a long head, white over
# the top with the black streak running down the muzzle to the black nose, black under the jaw; a magenta eye in a
# black star; a long pink tongue hanging from the open mouth; white spikes on the crown and the cheeks; black legs,
# grey feet with grey claws; the tail black below and white above, with white spikes at its end.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, unit, eye_look, polygon, outward, args, sibling

z_ = sibling("zigzagoon")
l_ = sibling("linoone")
zg = sibling("zigzagoongalar")

WHITE, BLACK, FOOT, CLAW = zg.WHITE, "#2C2A30", "#8C8C94", "#5C5C64"
NOSE, MOUTH, TONGUE, MASK = zg.NOSE, zg.MOUTH, zg.TONGUE, zg.MASK
IRIS, PUPIL, INK = zg.IRIS, zg.PUPIL, zg.INK

HC, HR = l_.HC, l_.HR
LX, FY, HY = l_.LX, l_.FY, l_.HY
BODY, BRAD = l_.BODY, l_.BRAD
TB, TL = l_.TB, l_.TL
TD = unit(0.42, 0.72, 0.55)          # raised more than Linoone's, so the long body fills its cell


def body_mid(y):
    """The body tube's centre height at y (its points' z, interpolated)."""
    ys = [p[1] for p in BODY]; zs = [p[2] for p in BODY]
    return np.interp(y, ys, zs)


def linoone_galar(src, out_path):
    k = Kit(src, coat="fur")

    # The long sleek body, low between the short legs (linoone.py's)
    k.add("body", tube(BODY, BRAD, seg=24), BLACK, "body")
    k.add("belly", ell((0, 0.03, 0.11), (0.07, 0.16, 0.05)), BLACK, "body")
    for name, (x, y), bone in (("leg", (LX, FY), "leg{s}"), ("hind", (LX, HY), "hind{s}")):
        k.add(name + "top", ell((x - 0.01, y + 0.01, 0.12), (0.044, 0.052, 0.052)), BLACK, bone, mirror=True)
        k.add(name, tube([(x - 0.004, y, 0.1), (x + 0.002, y - 0.006, 0.03)], [0.036, 0.032]), BLACK, bone,
              mirror=True)
        k.add(name + "foot", ell((x + 0.002, y - 0.016, 0.02), (0.034, 0.044, 0.021)), FOOT, bone, mirror=True)
        for i, dx in enumerate((-0.015, 0.0, 0.015)):
            a = np.array([x + 0.002 + dx, y - 0.05, 0.014])
            k.add(f"claw{name}{i}", z_.spike(a, (dx * 2, -1, -0.45), 0.038, 0.009, 0.007), CLAW, bone, fuse=False,
                  mirror=True, surface="shell")
    # the grey of the feet kept to the feet, the black of the leg down to the ankle
    k.paint(lambda P: inside(P[:, 2] - 0.032, 0.003), FOOT, on=["leg", "hind"])

    # The long narrow head, a pointed muzzle
    k.add("head", ell(HC, HR), WHITE, "head")
    k.add("neck", ell((0, -0.2, 0.152), (0.062, 0.06, 0.06)), WHITE, "head")
    k.add("snout", ell(HC + (0, -0.075, -0.012), (0.034, 0.065, 0.032), rot=(4, 0, 0)), WHITE, "head")

    # The flowing tail, back and a little up, swung out to the model's right (linoone.py's)
    a = np.cross(TD, (0, 0, 1.0)); a /= np.linalg.norm(a); b = np.cross(TD, a)
    pts = [TB + TD * t * TL + b * 0.04 * math.sin(t * 2.5) for t in (0, 0.3, 0.6, 0.85, 1.0)]
    k.add("tail", tube(pts, [0.046, 0.056, 0.05, 0.032, 0.01], seg=20), BLACK, "tail")
    tip = pts[-1]
    for i, (sa, sb) in enumerate(((0.0, 0.0), (0.5, 0.4), (-0.4, 0.5), (0.2, -0.5))):
        d = unit(*(TD + sa * a + sb * b))
        k.add(f"tailtip{i}", z_.spike(tip - TD * 0.04, d, 0.065, 0.022, 0.015), WHITE, "tail", fuse=False,
              surface="fur")

    # The white band over the top of the body, the neck and the tail; under the jaw and the throat black
    def top(P):
        y = P[:, 1]
        mid = body_mid(np.clip(y, -0.2, 0.24))
        on_body = inside(mid + 0.012 - 0.01 * np.cos(P[:, 0] * 40) - P[:, 2], 0.003) * (y > -0.2) * (y < 0.245)
        q = P - TB
        s = q @ TD
        tail = inside(-0.004 - q @ b, 0.003) * (s > -0.01)
        return np.maximum(on_body, tail)
    k.paint(top, WHITE, on=["body", "tail"])
    def throat(P):
        d = (P - HC) / HR
        jaw = inside(d[:, 2] + 0.15 + 0.12 * d[:, 1], 0.03)
        return jaw * (P[:, 1] < -0.15)
    k.paint(throat, BLACK, on=["head", "neck", "snout"])
    # the neck's sides black below the band, as the body's, so the white runs on from the body unbroken
    k.paint(lambda P: inside(P[:, 2] - 0.164 + 0.25 * np.clip(-0.21 - P[:, 1], 0, 1), 0.003) * (P[:, 1] > -0.27)
            * (P[:, 1] < -0.12), BLACK, on=["neck", "head"])

    # The black streak down the middle of the white: from the nose up the muzzle, over the crown and down the back
    def streak(P):
        y = P[:, 1]
        w = 0.012 + 0.018 * np.clip((y + 0.3) / 0.2, 0, 1) * np.clip((0.2 - y) / 0.2, 0, 1)
        return inside(np.abs(P[:, 0]) - w, 0.003) * (P[:, 2] > body_mid(np.clip(y, -0.2, 0.24)) + 0.03) * (y > -0.36) \
            * (y < 0.2)
    k.paint(streak, BLACK)

    # A white burst on each flank behind the shoulder (in every eye state, before build)
    for s in (1, -1):
        k.decal((0.08 * s, -0.06, 0.15), (s, -0.15, 0.1), (0.064, 0.05), WHITE, eps=0.004,
                outline=zg.star(n=6, inner=0.45, turn=10), rings=14, segs=72)

    # White spikes on the crown and the cheeks (linoone.py's tuft and cheeks)
    for s in (1, -1):
        for j, (dx, dy, ln) in enumerate(((0.0, 0.0, 0.04), (0.018, 0.012, 0.034))):
            base = np.array([(0.03 + dx) * s, -0.255 + dy, 0.198])
            k.add(f"tuft{'R' if s > 0 else 'L'}{j}", z_.spike(base, (0.4 * s, 0.35, 1), ln, 0.018, 0.013), WHITE,
                  "ear" + ("R" if s > 0 else "L"), fuse=False, surface="fur")
        for j, dz in enumerate((0.0, -0.025)):
            base = HC + np.array([0.056 * s, 0.01, -0.02 + dz])
            k.add(f"cheek{'R' if s > 0 else 'L'}{j}", z_.spike(base, (s * 0.8, 0.6, -0.2 - dz * 6), 0.045, 0.02,
                                                                0.014), WHITE, "head", fuse=False, surface="fur")

    # The black star round each eye, before build
    de = unit(0.62, -0.74, 0.2)
    E = HC + de / np.linalg.norm(de / HR) + (0, -0.012, 0.004)
    for s in (1, -1):
        k.decal(E * np.array([s, 1, 1.0]), de * np.array([s, 1, 1.0]), (0.04, 0.04), MASK, eps=0.004,
                outline=zg.star(turn=90 + 12 * s), rings=16, segs=60)

    # The black nose; the open mouth at the muzzle's tip; the long tongue hanging from it
    k.add("nose", ell(HC + (0, -0.138, -0.006), (0.018, 0.013, 0.013)), NOSE, "head", fuse=False, surface="skin")
    k.decal(HC + (0, -0.118, -0.036), unit(0, -1, -0.5), (0.03, 0.016), srgb(MOUTH), eps=0.003,
            outline=zg.mouth_outline(), rings=14, segs=80)
    tg = [HC + (0, -0.112, -0.04), HC + (0.004, -0.128, -0.062), HC + (0.008, -0.13, -0.09), HC + (0.01, -0.124, -0.108)]
    k.add("tongue", outward(blade(tg, [0.015, 0.019, 0.019, 0.015], (0, -1, 0.1), 0.42)), TONGUE, "head",
          fuse=False, surface="skin")

    k.build(voxel=0.0058, smooth=3, band=0.006)

    WH, IR, PU, KK, MK = srgb(WHITE), srgb(IRIS), srgb(PUPIL), srgb(INK), srgb(MASK)
    def look(u, v):
        c = np.tile(IR, (len(u), 1))
        c[np.hypot((u + 0.05) / 0.3, (v + 0.12) / 0.52) < 1] = PU
        c[np.hypot(u - 0.3, v - 0.05) < 0.17] = (1, 1, 1)
        c[np.hypot(u, v) > 0.88] = KK
        c[v > 0.3 - 0.2 * u] = MK
        c[np.abs(v - (0.3 - 0.2 * u)) < 0.11] = KK
        return c

    def eyes(state):
        f = eye_look(state, look, MK, srgb("#8A8A90"), line=0.16)
        k.decal(E, de, (0.023, 0.021), f, eps=0.006, rings=20, segs=56, mirror=True)
    k.eye_states(eyes)
    k.export(out_path)


if __name__ == "__main__":
    linoone_galar(*args())
