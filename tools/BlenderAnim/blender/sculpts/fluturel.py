# Fluturel (Carpathia No. 10074, the caterpillar) sculpted from nothing on the skeleton and clips of the game's own
# export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/fluturel.py -- <the export's Fluturel.glb> <out.glb>
#
# From its picture (docs/art/reference/gemini/carpathia/10074-Fluturel.jpg) and its brief (CP-dex-2-mountains.md, 74):
# a plump velvety black caterpillar curled asleep on a little rosette of plump green sedum leaves. A big round black
# head in front, its eyes always shut in sleepy arcs, dark grey cheeks, a small nose and a little smile, two short
# feelers with round tips. Behind it the body curls round in a C of fat round segments with grooves between, each
# segment with orange spots in rows down the back and the sides, and soft white tufts at the segments' sides; little
# black stubs of feet under the front. The line's frame for Fluturapol (fluturapol.py): the black, the orange, the
# white of the tufts and the way the face is made (on_ell, a light line on black).
# Bones (a chain from the tail to the head): body (-0.12, 0.12, 0.072), seg1 (-0.02, 0.18, 0.076),
# seg2 (0.09, 0.14, 0.08), seg3 (0.15, 0.04, 0.085), seg4 (0.13, -0.06, 0.09), seg5 (0.06, -0.12, 0.095),
# head (0.06, -0.15, 0.115).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, outward, srgb, inside, smoothstep, unit, eye_look, args

BLACK, CHEEK, LINE, ORANGE, TUFT = "#2C2A32", "#55525E", "#9C9AA8", "#F2802A", "#E6E2DA"
LEAF, LEAFDARK = "#6EA068", "#4E7E4E"

CHAIN = [("body", (-0.12, 0.12, 0.072)), ("seg1", (-0.02, 0.18, 0.076)), ("seg2", (0.09, 0.14, 0.08)),
         ("seg3", (0.15, 0.04, 0.085)), ("seg4", (0.13, -0.06, 0.09)), ("seg5", (0.06, -0.12, 0.095))]
TAILTIP = np.array([-0.165, 0.065, 0.062])
HC, HR = np.array([0.045, -0.19, 0.122]), np.array([0.098, 0.088, 0.092])


def on_ell(c, r, az, el, lift=0.0):
    """The point of an ellipsoid at an azimuth from the front (+ toward +X) and an elevation, in degrees, and its
    outward normal there."""
    a, e = math.radians(az), math.radians(el)
    d = np.array([math.sin(a) * math.cos(e), -math.cos(a) * math.cos(e), math.sin(e)])
    p = np.array(c) + np.array(r) * d
    n = d / np.array(r); n /= np.linalg.norm(n)
    return p + n * lift, n


def segments():
    """The body's segments from the tail to the neck: (bone, centre, radius, the way along the body)."""
    pts = [TAILTIP] + [np.array(p) for _, p in CHAIN]
    bones = ["body"] + [b for b, _ in CHAIN]
    out_ = []
    for i in range(len(pts) - 1):
        a, b = pts[i], pts[i + 1]
        d = (b - a) / np.linalg.norm(b - a)
        for f in ((0.5, 1.0) if i else (0.35, 1.0)):
            t = i + f
            r = 0.05 + 0.026 * smoothstep(0.0, 3.0, t)
            out_.append((bones[i + 1] if f == 1.0 else bones[i], a + (b - a) * f, r, d))
    return out_


def fluturel(src, out):
    k = Kit(src, coat="fur")
    segs = segments()
    for i, (bone, c, r, d) in enumerate(segs):
        k.add(f"seg{i:02d}", ell(c + (0, 0, 0.004), (r, r, r * 0.96)), BLACK, bone)
    k.add("neck", ell((0.06, -0.135, 0.1), (0.07, 0.06, 0.068)), BLACK, "seg5")
    k.add("skull", ell(HC, HR, sub=5), BLACK, "head")
    k.add("nose", ell(on_ell(HC, HR, 0, -10, -0.006)[0], (0.017, 0.012, 0.012)), BLACK, "head")
    # little black stubs of feet under the front segments
    for j, (bone, c, r, d) in enumerate(segs[-5:-1]):
        side = np.cross(d, (0, 0, 1.0)); side /= np.linalg.norm(side)
        for s in (1, -1):
            k.add(f"foot{j}{'R' if s > 0 else 'L'}", ell(c + side * s * r * 0.55 + (0, 0, -r * 0.8),
                                                        (0.015, 0.015, 0.016)), BLACK, bone)
    # feelers: short, up and a little out, round tips
    for s in (1, -1):
        b0 = HC + np.array([0.04 * s, -0.01, 0.07])
        pts = [b0, b0 + (0.016 * s, -0.004, 0.035), b0 + (0.034 * s, 0.0, 0.058)]
        k.add(f"feeler{'R' if s > 0 else 'L'}", outward(tube(pts, [0.0105, 0.0098, 0.009])), BLACK, "head",
              fuse=False)
        k.add(f"knob{'R' if s > 0 else 'L'}", outward(ell(pts[-1] + (0.002 * s, 0, 0.004), (0.0135, 0.0135, 0.0135),
                                                          sub=3)), BLACK, "head", fuse=False)
    # soft white tufts at the sides of every other segment, and up the back
    for i, (bone, c, r, d) in enumerate(segs[1:-1]):
        if i % 2:
            continue
        side = np.cross(d, (0, 0, 1.0)); side /= np.linalg.norm(side)
        for s in (1, -1):
            root = c + side * s * r * 0.85 + (0, 0, r * 0.15)
            out_d = side * s * 0.8 + np.array([0, 0, 0.6])
            for j, tw in enumerate((-0.6, -0.2, 0.2, 0.6)):
                dd = out_d + d * tw * 0.4 + np.array([0, 0, 0.1 * j])
                dd /= np.linalg.norm(dd)
                k.add(f"tuft{i}{j}{'R' if s > 0 else 'L'}",
                      outward(tube([root - dd * 0.008, root + dd * 0.013, root + dd * 0.024],
                                   [0.011, 0.009, 0.0045])), TUFT, bone, fuse=False)
    # the sedum rosette under it: plump leaves round a middle
    for j, (x, y, a, L, W) in enumerate(((0.02, 0.03, 0, 0.11, 0.075), (0.16, -0.12, 40, 0.085, 0.06),
                                         (-0.1, -0.1, -35, 0.09, 0.065), (0.2, 0.06, 75, 0.09, 0.06),
                                         (0.08, 0.2, 15, 0.085, 0.06), (-0.16, 0.06, -70, 0.085, 0.06),
                                         (-0.04, 0.2, -20, 0.08, 0.055), (0.05, -0.2, 5, 0.08, 0.055))):
        k.add(f"leaf{j}", outward(ell((x, y, 0.019), (W * 1.15, L * 1.15, 0.018), rot=(0, 0, a), sub=3)),
              LEAF if j % 2 == 0 else LEAFDARK, "root", fuse=False, surface="leaf")
    # cheeks and the smile, in every eye state
    for s in (1, -1):
        p, n = on_ell(HC, HR, 46 * s, -10)
        k.decal(p, n, (0.02, 0.014), CHEEK, eps=0.0016, rings=10, segs=36)
    Bk, Ln = srgb(BLACK), srgb(LINE)
    def smile(u, v):
        c = np.tile(Bk, (len(u), 1))
        c[(np.abs(v - (-0.1 + 0.55 * (u * u - 0.5))) < 0.22) & (np.abs(u) < 0.85)] = Ln
        return c
    p, n = on_ell(HC, HR, 0, -27)
    k.decal(p, n, (0.024, 0.012), smile, eps=0.0016, rings=10, segs=48)
    # orange spots in rows down the back and the sides
    for i, (bone, c, r, d) in enumerate(segs[1:-2]):
        side = np.cross(d, (0, 0, 1.0)); side /= np.linalg.norm(side)
        for s in (1, -1):
            for f, up, sz in ((0.42, 0.85, 0.012), (0.88, 0.32, 0.01)):
                if i % 2 and f > 0.5:
                    continue
                if f > 0.5 and np.dot(side * s, np.array([0.05, 0.03, 0]) - c) > 0:
                    continue
                nn = side * s * f + np.array([0, 0, up]); nn /= np.linalg.norm(nn)
                k.decal(c + (0, 0, 0.004) + nn * r * 0.97, nn, (sz, sz), ORANGE, eps=0.0016, rings=6, segs=24)
    k.build(voxel=0.0035, smooth=3, band=0.004, wband=0.03)
    V = np.array([v.co[:] for v in k.ob.data.vertices])
    a = math.radians(31.5)
    proj = V[:, 0] * math.cos(a) + V[:, 1] * math.sin(a)
    h, w, l = V[:, 2].max(), np.ptp(V[:, 0]), np.ptp(V[:, 1])
    print("KIT: board: shows %.3f in a square of %.3f" % (np.ptp(proj), 1.3 * max(h, 0.85 * max(w, l))))
    # the eyes, always shut: the sleepy arc is the open state, light on the black head
    def lay(state):
        look_as = {"Open": "Shut", "Shut": "Shut", "Squeeze": "Squeeze", "Smile": "Smile", "Fierce": None}[state]
        for s in (1, -1):
            p, n = on_ell(HC, HR, 31 * s, 8)
            if look_as:
                base = eye_look(look_as, lambda u, v: np.tile(Bk, (len(u), 1)), Bk, Ln, line=0.17)
            else:
                def base(u, v):
                    c = np.tile(Bk, (len(u), 1))
                    c[(np.abs(u) < 0.82) & (np.abs(v - (-0.2 + 0.42 * u)) < 0.17)] = Ln
                    return c
            k.decal(p, n, (0.032, 0.028), (lambda u, v, b=base, s=s: b(u * s, v)), eps=0.0016, rings=20, segs=56)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    fluturel(*args())
