# Dudunsparce (Two-Segment Form) sculpted from nothing on the skeleton and clips of the game's own export of it
# (sculptkit.py):
#   blender --background --factory-startup --python sculpts/dudunsparce.py -- <the export's Dudunsparce.glb> <out.glb>
# Grown from dunsparce.py (sibling): its yellow, blue, cream and white, and its eyes (big blue rings filled cream,
# always shut, dark lines across); the skeleton is another (a segment bone, the wings on the head), so the frame is new.
#
# From the references (they overrule the task's "small wings" only in number: two pairs): a long fat grub lying on
# the ground, its great round head the first mass, in front and higher than the rest. The head's lower front is one
# huge open mouth: dark red inside, a pink tongue filling its floor, a blue lip round its lower rim and five blue
# spikes hanging under the chin. Big oval eyes on the sides, cream filled, ringed thick blue, two dark lines across
# (shut). Two pairs of white blade wings: a short pair standing up from the back of the head, a longer pair swept back
# over the first segment. The underside blue all along; on each side of the first segment a bold dark navy spiral; a
# blue ring with a cream saddle at the waist; the second segment yellow, ending in a raised ridged stub with a blue
# collar at its end (no drill point: the 3D picture's stub is blunt).
# Ratios (the 3D picture): head height / total height 0.62, head width / body width 1.55, eye / head height 0.4,
# length / height 2.0.
# Skeleton: body (0, -0.04, 0.1), seg1 (0.025, 0.065, 0.1), tail (0.05, 0.27, 0.1), head (0, -0.14, 0.14),
# wings (+-0.07, -0.19, 0.29) on the head.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, args, hull, slab, lathe, ring_band, sibling

ds = sibling("dunsparce")
YELLOW, BLUE, PALE, WING, GROOVE, SLIT = ds.YELLOW, ds.BLUE, ds.PALE, ds.WING, ds.GROOVE, ds.SLIT
NAVY, MOUTH, TONGUE = "#2E3A92", "#6E1E26", "#E2767A"

HC = np.array([0.0, -0.2, 0.19])           # the head's middle
HR = np.array([0.135, 0.13, 0.128])
SEG0 = np.array([0.0, -0.01, 0.092])       # the first segment
SR0 = np.array([0.088, 0.105, 0.086])
SEG1 = np.array([0.035, 0.155, 0.09])      # the second
SR1 = np.array([0.08, 0.095, 0.082])
STUB = np.array([0.06, 0.225, 0.115])      # the stub's base
SD = unit(0.3, 0.6, 0.74)


def on_ell(c, r, d):
    """The point of an ellipsoid along a direction from its centre, and its outward normal there."""
    c, r = np.array(c, float), np.array(r, float)
    d = unit(*d)
    p = c + d / math.sqrt(((d / r) ** 2).sum())
    n = (p - c) / r ** 2
    return p, n / np.linalg.norm(n)


def wings(k):
    """Two pairs of white blade wings: a short one standing up from the head's back, a long one swept back."""
    for side, sname in ((1, "R"), (-1, "L")):
        for i, (root, d, L, w) in enumerate([
                (HC + (0.06 * side, 0.06, 0.09), unit(0.35 * side, 0.45, 0.82), 0.1, 0.036),
                (HC + (0.075 * side, 0.12, 0.05), unit(0.55 * side, 0.75, 0.38), 0.15, 0.04)]):
            n = unit(0.78 * side, -0.35, -0.2) if i == 0 else unit(0.6 * side, -0.25, -0.75)
            n = n - d * (n @ d); n /= np.linalg.norm(n)
            ex = np.cross(d, n)
            t = np.linspace(0, 1, 18)
            half = w * np.sqrt(t) * (1 - t) ** 0.55 * 1.55
            outline = [(half[j], t[j] * L) for j in range(len(t))] + [(-half[j], t[j] * L) for j in range(len(t) - 2, 0, -1)]
            k.add(f"wing{i}{sname}", slab(outline, root, ex, d, 0.02), WING, f"wing{sname}", fuse=False)


def stub(k):
    """The raised ridged stub ending the second segment, a blue collar at its end."""
    L, R0, R1, lip = 0.105, 0.064, 0.05, 0.006
    cuts = [0.0, 0.035, 0.07, L]
    prof = [(0.0, -0.01), (R0, -0.01)]
    r = lambda s: R0 + (R1 - R0) * s / L
    for a, b in zip(cuts, cuts[1:]):
        if a > 0:
            prof.append((r(a) + lip, a))
        prof.append((r(b), b))
    prof.append((0.0, L))
    k.add("stub", lathe(STUB, SD, prof, seg=44), YELLOW, "tail", fuse=False)
    for a in cuts[1:-1]:
        k.add(f"groove{int(a * 1000)}", ring_band(STUB + SD * (a - 0.004), SD, r(a) + 0.0015, 0.006, thick=0.004, nu=49),
              GROOVE, "tail", fuse=False)
    k.add("collar", lathe(STUB + SD * (L - 0.004), SD, [(0.0, 0.0), (0.058, 0.0), (0.06, 0.012), (0.056, 0.03),
                                                         (0.0, 0.03)], seg=44), BLUE, "tail", fuse=False)


def dudunsparce(src, out):
    k = Kit(src, coat="scales")

    # --- the head, the first and largest mass ---------------------------------------------------------------------
    k.add("head", ell(HC, HR), YELLOW, "head")
    k.add("neck", tube([HC + (0, 0.06, -0.06), SEG0 + (0, -0.02, 0.01)], [0.085, 0.085]), YELLOW, "head")
    # the huge open mouth on the lower front: a dark red hollow, a pink tongue on its floor
    MC = HC + np.array([0, -0.112, -0.045])
    k.add("mouth", ell(MC, (0.098, 0.07, 0.06), rot=(-12, 0, 0)), MOUTH, "head", carve=True)
    k.add("tongue", ell(MC + (0, 0.045, -0.03), (0.072, 0.06, 0.028), rot=(-10, 0, 0)), TONGUE, "head", fuse=False)

    # --- the body: two segments lying on the ground, the stub raised behind ----------------------------------------
    k.add("seg0", ell(SEG0, SR0), YELLOW, "body")
    k.add("waist", tube([SEG0 + (0, 0.07, 0), SEG1 + (0, -0.06, 0)], [0.07, 0.068]), YELLOW, "seg1")
    k.add("seg1", ell(SEG1, SR1), YELLOW, "seg1")
    k.add("tailroot", ell(STUB + (0, -0.01, -0.012), (0.066, 0.066, 0.06)), YELLOW, "tail")
    stub(k)

    # the blue underside all along, and the blue lip and chin under the mouth
    def under(P):
        y = P[:, 1]
        z0 = np.where(y < -0.1, 0.07 + 0.22 * np.clip(-(y + 0.1), 0, 0.22), 0.058)
        return inside(P[:, 2] - z0, 0.003)
    k.paint(under, BLUE, on=["head", "neck", "seg", "waist", "tailroot"])
    # the blue waist ring with its cream saddle
    k.paint(lambda P: inside(np.abs(P[:, 1] - 0.075 - 0.25 * P[:, 0]) - 0.022, 0.003) * (P[:, 2] < 0.2), BLUE,
            on=["waist", "seg"])
    k.paint(lambda P: inside(np.abs(P[:, 1] - 0.075 - 0.25 * P[:, 0]) - 0.022, 0.003) * inside(0.135 - P[:, 2], 0.003),
            PALE, on=["waist", "seg"])
    # a dark lining in the mouth's hollow
    k.paint(lambda P: inside(np.linalg.norm((P - MC) / np.array([0.1, 0.075, 0.065]), axis=1) - 1.08, 0.004), MOUTH,
            on=["head"])

    # five blue spikes under the chin, pointing down and forward
    for i, a in enumerate((-60, -30, 0, 30, 60)):
        th = math.radians(a)
        base = HC + np.array([0.085 * math.sin(th), -0.085 * math.cos(th) - 0.02, -0.118])
        d = unit(0.35 * math.sin(th), -0.55 * math.cos(th) - 0.1, -0.85)
        a1 = np.cross(d, (0, 0, 1.0)); a1 /= np.linalg.norm(a1)
        b1 = np.cross(d, a1)
        pts = [base - d * 0.015 + 0.019 * (math.cos(t) * a1 + math.sin(t) * b1) for t in np.linspace(0, 2 * math.pi, 7)[:-1]]
        pts.append(base + d * 0.04)
        k.add(f"spike{i}", hull(pts), BLUE, "head", fuse=False)

    wings(k)

    # a bold navy spiral on each side of the first segment, in every eye state
    CN, CB = srgb(NAVY), srgb(BLUE)
    def spiral(u, v):
        r = np.sqrt(u * u + v * v)
        c = np.tile(CB, (len(u), 1))
        c[(r > 0.55) & (r < 0.95)] = CN
        c[r < 0.3] = CN
        c[(np.abs(v + 0.05) < 0.18) & (u > 0.1) & (u < 0.75)] = CN
        return c
    SP, SN = on_ell(SEG0, SR0, (1.0, -0.15, -0.45))
    k.decal(SP, SN, (0.045, 0.045), spiral, up=(0, 0, 1), eps=0.003, rings=20, segs=56, mirror=True)

    k.build(voxel=0.0055, smooth=4, band=0.008)

    # The eyes: big ovals filled cream, ringed thick blue, two dark lines across: always shut
    CB, CP, CS = srgb(BLUE), srgb(PALE), srgb(SLIT)
    def lines(tilt):
        def look(u, v):
            c = np.tile(CP, (len(u), 1))
            for off in (0.2, -0.2):
                c[np.abs(v - off - tilt * u) < 0.13] = CS
            return c
        return look
    def ringed(f):
        def look(u, v):
            c = f(u, v)
            c[u * u + v * v > 0.7 ** 2] = CB
            return c
        return look
    EP, EN = on_ell(HC, HR, (0.72, -0.58, 0.36))
    def eyes(state):
        if state == "Open":
            f = lines(0.0)
        elif state == "Fierce":
            f = lines(0.4)
        else:
            f = eye_look(state, lines(0.0), CP, CS, line=0.15)
        k.decal(EP, EN, (0.055, 0.06), ringed(f), up=(0, 0, 1), eps=0.003, rings=20, segs=56, mirror=True)

    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    dudunsparce(*args())
