# The chibi people's files (style guide, "Characters from files"): one build's body, face, hair cuts, hats, garments
# and things held, each a piece of zones on the engine's skeleton for the build.
#   blender --background --factory-startup --python sculpts/people_chibi.py -- <Kid|Adult|Little|Elder> <out.glb> [only,pieces]
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from peoplekit import People, E, Box, T, lathe, torus, cone, V, P, A, W, K, STRIPE, SHADE, THIRD, inside, smoothstep, unit

argv = sys.argv[sys.argv.index("--") + 1:]
build, out = argv[0], argv[1]
only = set(argv[2].split(",")) if len(argv) > 2 else None

pp = People(build)
f = pp.f
H = V(f["HeadC"]); R = f["HeadR"]
HIP, WAIST, CHEST, NECK = f["HipY"], f["WaistY"], f["ChestY"], f["NeckY"]
SH, EL, WR = V(f["Shoulder"]), V(f["Elbow"]), V(f["Wrist"])
LT, KN, AN = V(f["LegTop"]), V(f["Knee"]), V(f["Ankle"])
TW, TD, AR, LR = f["TorsoW"], f["TorsoD"], f["ArmR"], f["LegR"]
EYE_Y = H[1] - f["EyeDrop"]; EYE_X = f["EyeHalf"]
ADULT = build in ("Adult", "Elder")
PALM = WR + (WR - EL) / np.linalg.norm(WR - EL) * 0.044
MIR = np.array([-1.0, 1, 1])


def want(name):
    return only is None or any(name == o or name.startswith(o + ".") or o.startswith(name + ".") for o in only)


def lerp(a, b, t):
    return a + (b - a) * t


def head_dir(az, el):
    """A direction from the head's middle: az round from the front toward the character's left, el up, in radians."""
    return np.array([math.sin(az) * math.cos(el), math.sin(el), math.cos(az) * math.cos(el)])


def on_head(az, el, r=1.0):
    return H + head_dir(az, el) * R * r


def piece(name, tint, hides="", **kw):
    pp.piece(name, tint, hides, **kw)


# ---------------------------------------------------------------------------------------------------------------- body
def chin(Vb):
    # The unit sphere in Blender's axes (z up, -y front): narrower below, the jaw a little forward
    Vb = Vb.copy()
    low = np.clip(-Vb[:, 2], 0, 1)
    Vb[:, 0] *= 1 - 0.07 * low ** 2
    Vb[:, 1] -= 0.03 * low
    return Vb


if want("body"):
    pp.add("skull", E(H, (R, R * 0.95, R * 0.95), deform=chin, sub=4), P, "head")
    pp.add("cheeks", E(H + (0, -R * 0.24, R * 0.18), (R * 0.83, R * 0.62, R * 0.76)), P, "head")
    pp.add("nose", E((0, EYE_Y - 0.055, H[2] + R * 0.93), (0.017, 0.014, 0.014)), P, "head")
    pp.add("ear", E(H + (R * 0.97, -R * 0.1, -0.005), (0.03, 0.048, 0.034)), P, "head", mirror=True)
    pp.add("neck", T([(0, NECK - 0.07, 0), (0, NECK + 0.05, 0.004)], [0.05, 0.048]), P, "head")
    # The ears' hollows, a shade of the skin
    pp.paint(lambda Q: inside(np.hypot((np.abs(Q[:, 0]) - R * 1.0) / 0.02, (Q[:, 1] - (H[1] - R * 0.1)) / 0.03) - 1, 0.3)
             * (np.abs(Q[:, 0]) > R * 0.96), SHADE, on=["ear"])
    piece("body.head", "skin", voxel=0.011)

    pp.add("chest", E((0, lerp(WAIST, NECK, 0.52), 0), (TW * 0.95, (NECK - WAIST) * 0.66, TD * 0.95)), P, "spine")
    pp.add("belly", E((0, WAIST - 0.01, 0.004), (TW * 0.9, (WAIST - HIP) * 0.9, TD * 0.95)), P, "spine")
    pp.add("hips", E((0, HIP + 0.035, 0), (TW * 0.92, 0.075, TD * 0.95)), W, "hips")
    piece("body.torso", "skin")

    pp.add("upper", T([SH, EL], [AR * 0.92, AR * 0.86]), P, "upperArm.{s}", mirror=True)
    pp.add("fore", T([EL, WR], [AR * 0.86, AR * 0.8]), P, "foreArm.{s}", mirror=True)
    piece("body.arms", "skin")
    pp.add("hand", E(PALM, (0.04, 0.046, 0.04)), P, "hand.{s}", mirror=True)
    pp.add("thumb", E(PALM + (-0.012, -0.004, 0.034), (0.016, 0.022, 0.016)), P, "hand.{s}", mirror=True)
    piece("body.hands", "skin")

    pp.add("thigh", T([LT, KN], [LR * 0.9, LR * 0.82]), P, "thigh.{s}", mirror=True)
    pp.add("shin", T([KN, AN + (0, 0.01, 0)], [LR * 0.82, LR * 0.74]), P, "shin.{s}", mirror=True)
    piece("body.legs", "skin")
    pp.add("foot", E((AN[0], 0.034, 0.03), (0.046, 0.034, 0.08)), P, "foot.{s}", mirror=True)
    pp.add("ankle", T([AN + (0, 0.02, 0), (AN[0], 0.04, 0.0)], [LR * 0.72, 0.04]), P, "foot.{s}", mirror=True)
    piece("body.feet", "skin")

# ---------------------------------------------------------------------------------------------------------------- face
# Everything of the face is a patch on the head's surface, cast from the front
EW, EH = (0.038, 0.055) if ADULT else (0.046, 0.066)
EYE_Y -= 0.012


def face_n(x, y):
    """The way a patch at (x, y) of the face looks out: mostly forward, a little with the head's round."""
    return unit(x * 0.55, (y - H[1]) * 0.3, R)


def face_c(x, y):
    return (x, y, H[2] + R * 0.9)


def patch(x, y, half, zone, turn=0.0, both=True, **kw):
    """A patch on the face at (x, y), turned by an angle (positive lifts its outer end), on both sides."""
    up = (-math.sin(turn), math.cos(turn), 0)
    pp.decal(face_c(x, y), face_n(x, y), half, zone, up=up, mirror=both and x != 0, **kw)


def arc(x, y, w, h, thick, zone, steps=5, both=True, eps=0.005):
    """A curved line on the face: w across, h how far its middle stands above its ends (below, if negative)."""
    for i in range(steps):
        t0, t1 = -1 + 2 * i / steps, -1 + 2 * (i + 1) / steps
        tm = (t0 + t1) / 2
        dy0, dy1 = h * (1 - t0 * t0), h * (1 - t1 * t1)
        turn = math.atan2(dy1 - dy0, (t1 - t0) * w)
        seg = math.hypot(dy1 - dy0, (t1 - t0) * w) / 2 + thick * 0.6
        patch(x + tm * w, y + h * (1 - tm * tm), (seg, thick), zone, turn=turn, both=both, rings=2, segs=10, eps=eps)


def eye_open(wide=False):
    def colour(u, v):
        c = np.tile(K, (len(u), 1))                                               # the lid's dark rim, heavier above
        c[(u / 0.9) ** 2 + ((v + 0.12) / 0.8) ** 2 < 1] = W                       # the white
        c[(u / 0.8) ** 2 + ((v + 0.1) / 0.8) ** 2 < 1] = P                        # the iris
        c[((u / 0.8) ** 2 + ((v + 0.1) / 0.8) ** 2 < 1) & (v < -0.56)] = A        # lighter below
        c[(u / 0.36) ** 2 + ((v - 0.02) / 0.44) ** 2 < 1] = THIRD                 # the pupil
        c[((u / 0.8) ** 2 + ((v + 0.1) / 0.8) ** 2 < 1) & (v > 0.46)] = THIRD     # the lid's shade on the iris
        c[((u + 0.34) / 0.28) ** 2 + ((v - 0.36) / 0.26) ** 2 < 1] = W            # two highlights
        c[((u - 0.34) / 0.14) ** 2 + ((v + 0.44) / 0.13) ** 2 < 1] = W
        return c
    k = 1.12 if wide else 1.0
    patch(EYE_X, EYE_Y, (EW * k, EH * k), colour, rings=10, segs=28)


if want("face"):
    S = "body.head"
    eye_open(); piece("face.eyes.open", "eyes", surface=S)
    eye_open(True); piece("face.eyes.wide", "eyes", surface=S)
    arc(EYE_X, EYE_Y - EH * 0.35, EW * 1.05, EH * 0.42, 0.0055, K, steps=5)       # shut: a lid curving down
    piece("face.eyes.shut", "eyes", surface=S)

    # Lashes, for the looks that have them: two ticks at each eye's outer corner
    for nm, k, dy in (("open", 1.0, 0.0), ("wide", 1.12, 0.0), ("shut", 1.0, -EH * 0.5)):
        patch(EYE_X + EW * k * 0.95, EYE_Y + EH * k * 0.5 + dy, (0.014, 0.005), K, turn=0.6, rings=2, segs=10, eps=0.006)
        patch(EYE_X + EW * k * 1.05, EYE_Y + EH * k * 0.2 + dy, (0.012, 0.0045), K, turn=0.3, rings=2, segs=10, eps=0.006)
        piece("face.lashes." + nm, "eyes", surface=S)

    BY = EYE_Y + EH + 0.022
    brows = {"Neutral": (0.0, 0.0), "Happy": (0.006, 0.05), "Surprised": (0.016, 0.0), "Sad": (0.0, 0.32), "Angry": (-0.006, -0.38)}
    for nm, (lift, turn) in brows.items():
        patch(EYE_X, BY + lift, (EW * 0.95, 0.0065), P, turn=turn, rings=2, segs=12, eps=0.005)
        piece("face.brows." + nm, "brows", surface=S)

    MY = EYE_Y - EH - 0.036
    LIP, DARK_MOUTH, TONGUE = "#96404A", "#5A2630", "#E88A8E"
    arc(0, MY - 0.004, 0.022, -0.006, 0.0042, LIP, steps=3, both=False)
    piece("face.mouth.Neutral", "", colours="true", surface=S)
    arc(0, MY + 0.004, 0.034, -0.016, 0.0048, LIP, steps=5, both=False)
    piece("face.mouth.Happy", "", colours="true", surface=S)
    patch(0, MY - 0.004, (0.014, 0.017), DARK_MOUTH, both=False, rings=4, segs=16)
    piece("face.mouth.Surprised", "", colours="true", surface=S)
    arc(0, MY - 0.012, 0.024, 0.01, 0.0042, LIP, steps=4, both=False)
    piece("face.mouth.Sad", "", colours="true", surface=S)
    arc(0, MY - 0.01, 0.02, 0.005, 0.0048, LIP, steps=3, both=False)
    piece("face.mouth.Angry", "", colours="true", surface=S)
    patch(0, MY - 0.004, (0.024, 0.02), DARK_MOUTH, both=False, rings=5, segs=18)
    patch(0, MY - 0.014, (0.014, 0.008), TONGUE, both=False, rings=3, segs=12, eps=0.007)
    piece("face.mouth.open", "", colours="true", surface=S)

    patch(EYE_X + 0.05, EYE_Y - EH * 0.95, (0.038, 0.021), P, rings=4, segs=18, eps=0.003)
    piece("face.Blush", "blush", surface=S)

    # What grows on the face, in the hair's colour
    pp.add("stache", E((0.045, EYE_Y - 0.088, H[2] + R * 0.9), (0.05, 0.022, 0.026), roll=-0.28), P, "head", fuse=False, mirror=True)
    piece("face.Mustache", "hair")
    pp.add("beard", E(H + (0, -R * 0.84, R * 0.34), (R * 0.72, R * 0.3, R * 0.58)), P, "head")
    pp.add("burn", T([H + (R * 0.9, -R * 0.08, R * 0.08), H + (R * 0.64, -R * 0.78, R * 0.4)], [0.05, 0.06]), P, "head", mirror=True)
    piece("face.Beard", "hair")
    pp.add("brow", E((EYE_X, EYE_Y + 0.07, H[2] + R * 0.86), (0.05, 0.017, 0.024), roll=0.12), P, "head", fuse=False, mirror=True)
    piece("face.BushyBrows", "hair")
    # Glasses: a rim round each eye, a bridge and the arms back to the ears
    GZ = H[2] + R * 0.97
    pp.add("rim", torus((EYE_X, EYE_Y, GZ), 0.046, 0.006, pitch=math.pi / 2, seg=18, ring=6), P, "head", fuse=False, mirror=True)
    pp.add("bridge", T([(-EYE_X + 0.046, EYE_Y, GZ), (EYE_X - 0.046, EYE_Y, GZ)], [0.005, 0.005], seg=6, per=1), P, "head", fuse=False)
    pp.add("arm", T([(EYE_X + 0.046, EYE_Y, GZ), (R * 0.98, EYE_Y + 0.01, H[2] + 0.02)], [0.005, 0.005], seg=6, per=2), P, "head", fuse=False, mirror=True)
    piece("face.Glasses", "glasses")

# ---------------------------------------------------------------------------------------------------------------- hair
HC = H + (0, 0.02, -0.02)        # the hair's middle, a little up and back of the head's


def hairline(low=0.5, wide=1.25, jaw=-0.42):
    """A window in the hair's shell for the face: up to `low` of the radius above the middle, `wide` radians to each side."""
    def cut(Q):
        d = Q - H
        az = np.abs(np.arctan2(d[:, 0], d[:, 2]))
        el = d[:, 1] / R
        face = (az < wide) & (el < low - 0.12 * (az / wide) ** 2)
        under = (az < 2.2) & (el < jaw - 0.25 * np.clip((az - 1.2) / 1.0, 0, 1))
        nape = el < -0.78
        return (face | under | nape).astype(float)
    return cut


def cap(k=1.05, low=0.5, wide=1.25, ear=-0.12, nape=-0.62, top=None, c=None, r=None, name="shell"):
    """
    The hair's cap over the skull: a ball whose points over the face (up to `low` of the radius above the middle and
    `wide` radians to each side), under the line from the ears (`ear`) down to the nape, and above `top` when given,
    are drawn in under the skin, so the hairline is the cap's own smooth edge.
    """
    def deform(Vb):
        Vb = Vb.copy()
        az = np.abs(np.arctan2(Vb[:, 0], -Vb[:, 1]))
        el = Vb[:, 2]
        face = (az < wide) & (el < low - 0.14 * (az / wide) ** 2)
        under = el < ear + (nape - ear) * np.clip((az - wide) / (math.pi - wide), 0, 1)
        gone = face | under
        if top is not None:
            gone |= el > top
        Vb[gone] *= 0.72
        return Vb
    rr = r if r is not None else (R * k, R * k * 0.97, R * k)
    pp.add(name, E(HC if c is None else c, rr, deform=deform, sub=5), P, "head")


def shell(k=1.05, **kw):
    cap(k, **kw)


def lock(a, b, ra, rb, bone="head", name="lock", mid=None, mirror=False):
    """A lock of hair from a to b, bowed out through mid when given."""
    pts = [a, b] if mid is None else [a, mid, b]
    rs = [ra, rb] if mid is None else [ra, (ra + rb) * 0.55, rb]
    pp.add(name, T(pts, rs, seg=10, per=3), P, bone, mirror=mirror)


def fringe(spec, start=0.62):
    """Locks over the brow: (az, how low the tip comes as a share of the radius above the middle, lean) each."""
    for i, (az, low, lean) in enumerate(spec):
        lock(on_head(az, math.asin(start), 0.96), on_head(az + lean, math.asin(low), 1.06), 0.085, 0.02,
             name=f"fringe{i}", mid=on_head(az + lean * 0.5, math.asin((start + low) / 2 + 0.06), 1.09))


def sides_and_back(hatted):
    """Locks down the sides and the back, to just under the ears."""
    for i, (az, el0, el1, r) in enumerate([(1.45, 0.25, -0.38, 0.07), (2.0, 0.2, -0.5, 0.075), (2.6, 0.15, -0.6, 0.08)]):
        lock(on_head(az, el0, 1.0), on_head(az, el1, 1.1), r, 0.03, name=f"side{i}", mirror=True)
    lock(on_head(math.pi, 0.1, 1.0), on_head(math.pi, -0.66, 1.1), 0.085, 0.04, name="back")
    if not hatted:
        for i, az in enumerate((0.0, 0.9, -0.9, 2.2, -2.2)):
            lock(HC + (0, R * 1.0, -R * 0.1), on_head(az, 0.75, 1.06), 0.07, 0.045, name=f"top{i}")


SHORT_FRINGE = [(-0.95, 0.02, -0.12), (-0.62, 0.2, -0.1), (-0.28, 0.3, -0.06), (0.06, 0.2, 0.05), (0.4, 0.3, 0.06), (0.7, 0.18, 0.1), (0.98, 0.02, 0.12)]


def hair(cut, hatted):
    # Under a hat the crown is the hat's: no locks over the top, and the fringe starts lower
    start = 0.5 if hatted else 0.64
    if cut == "Bald":
        # A band of short hair round the back and the sides, the crown bare
        cap(1.03, low=1.2, wide=1.5, ear=-0.2, nape=-0.6, top=0.38)
        return None, None
    if cut == "Bowl":
        # One round mass cut straight across the brow, sloping down past the ears to the nape
        cap(1.0, low=0.22, wide=1.5, ear=0.0, nape=-0.55, c=H + (0, 0.03, -0.015), r=(R * 1.13, R * 1.06, R * 1.12))
        return None, None
    if cut == "Afro":
        c = H + (0, R * 0.4, -R * 0.2)
        rad = np.array([R * 1.3, R * 0.95, R * 1.15])
        pp.add("mass", E(c, rad, sub=4), P, "head")
        dirs = [(0, 1, 0), (0, 0.75, 0.65), (0.5, 0.7, 0.5), (-0.5, 0.7, 0.5)]
        for y in (0.6, 0.1):
            for deg in (70, 115, 160, 200, 245, 290):
                a = math.radians(deg)
                dirs.append((math.sin(a), y, math.cos(a)))
        for i, d in enumerate(dirs):
            pp.add(f"curl{i}", E(c + unit(*d) * rad * 0.9, (R * 0.28,) * 3, sub=2), P, "head")
        shell(1.03, low=0.42, wide=1.2)
        return None, None
    if cut == "Curls":
        shell(1.02, low=0.4, wide=1.15)
        dirs = [] if hatted else [(0, 1, 0)]
        for y, count in ((0.75, 6), (0.4, 9), (0.02, 10), (-0.36, 8)):
            if hatted and y > 0.45:
                continue
            for i in range(count):
                a = (i + (0.5 if count % 2 == 0 else 0)) / count * 2 * math.pi
                d = (math.sin(a), y, math.cos(a))
                if d[2] > 0.3 and y < 0.38:
                    continue
                dirs.append(d)
        for x in (-0.55, -0.18, 0.18, 0.55):
            dirs.append((x, 0.5 + abs(x) * 0.15, 0.82))
        for i, d in enumerate(dirs):
            pp.add(f"curl{i}", E(HC + unit(*d) * R, (R * 0.21,) * 3, sub=2), P, "head")
        return None, None
    if cut == "Spiky":
        shell(1.04)
        spikes = [(0.0, 1.2, 0.0), (0.9, 0.95, 0.2), (-0.9, 0.95, 0.2), (2.0, 0.9, 0.1), (-2.0, 0.9, 0.1), (3.14, 0.8, 0.0),
                  (1.5, 0.35, 0.0), (-1.5, 0.35, 0.0), (2.5, 0.2, 0.0), (-2.5, 0.2, 0.0), (3.14, 0.1, 0.0)]
        for i, (az, el, _) in enumerate(spikes):
            if hatted and el > 0.5:
                continue
            d = head_dir(az, el)
            pp.add(f"spike{i}", cone(HC + d * R * 0.8, HC + d * R * 1.55 + (0, R * 0.08, 0), R * 0.3, 0.012), P, "head")
        fringe([(-0.75, 0.26, -0.22), (-0.3, 0.16, -0.12), (0.2, 0.28, 0.14), (0.7, 0.2, 0.2)], start)
        sides_and_back(True)
        return None, None
    if cut in ("Swept", "Bun"):
        # Combed back off a high brow
        shell(1.05, low=0.66, wide=1.1)
        if not hatted:
            # One full mass over the top, higher at the brow, with a few broad locks lying back over it
            pp.add("pomp", E(H + (0, R * 0.7, R * 0.08), (R * 0.8, R * 0.4, R * 0.78), pitch=-0.22, sub=4), P, "head")
        lock(H + (R * 0.8, R * 0.42, R * 0.45), H + (R * 0.62, -R * 0.25, -R * 0.75), 0.065, 0.045, name="sideA", mirror=True)
        lock(H + (R * 0.92, R * 0.05, R * 0.35), H + (R * 0.7, -R * 0.45, -R * 0.55), 0.055, 0.04, name="sideB", mirror=True)
        if cut == "Bun":
            back = unit(0, 0.56, -0.83)
            pp.add("knot", E(H + back * R * 1.2, (R * 0.38,) * 3), P, "head")
            pp.add("twist", torus(H + back * R * 1.0, R * 0.3, 0.022, pitch=math.atan2(-0.83, 0.56) + 0.0, seg=16, ring=6), SHADE, "head")
        return None, None
    # Short, Long, Braids, Bunches, Ponytail: a fringe, locks down the sides and the back
    shell(1.05)
    fringe(SHORT_FRINGE, start)
    sides_and_back(hatted)
    if cut == "Long":
        pp.add("fall", E(H + (0, -R * 0.75, -R * 0.5), (R * 0.98, R * 1.0, R * 0.5), sub=4), P, "hair")
        for i, x in enumerate((-0.55, -0.18, 0.18, 0.55)):
            lock(H + (x * R, -R * 0.2, -R * 0.82 + abs(x) * R * 0.25), H + (x * R * 1.12, -R * 1.65, -R * 0.6 + abs(x) * R * 0.2),
                 0.1, 0.05, bone="hair", name=f"fall{i}")
        lock(H + (R * 0.9, -R * 0.05, R * 0.18), H + (R * 0.92, -R * 1.05, R * 0.12), 0.065, 0.035, name="cheek", mirror=True)
    if cut == "Braids":
        root = H + (R * 0.88, -R * 0.42, -R * 0.02)
        sh = V((0.175, NECK + 0.025, 0.11)); end = V((0.165, CHEST - 0.045, TD + 0.045))
        lock(root, sh, 0.05, 0.042, name="plait", mirror=True)
        for i in range(4):
            at = lerp(sh, end, (i + 0.5) / 4)
            pp.add(f"bead{i}", E(at, (0.04, np.linalg.norm(end - sh) / 4 * 0.62, 0.034)), P, "head", fuse=False, mirror=True)
        pp.add("tie", E(end + (0, -0.012, 0), (0.024,) * 3, sub=2), W, "head", fuse=False, mirror=True)
        pp.add("tuft", cone(end + (0, -0.03, 0), end + (0, -0.07, 0), 0.026, 0.008), P, "head", fuse=False, mirror=True)
    if cut == "Bunches":
        tie = H + (R * 0.92, R * 0.3, -R * 0.15)
        pp.add("bunch", T([tie, H + (R * 1.32, R * 0.42, -R * 0.22), H + (R * 1.4, -R * 0.3, -R * 0.16)], [R * 0.2, R * 0.18, R * 0.06]),
               P, "hair", mirror=True)
        pp.add("tie", E(tie + (R * 0.08, 0, 0), (R * 0.12,) * 3, sub=2), W, "hair", fuse=False, mirror=True)
    if cut == "Ponytail":
        back = unit(0, 0.05, -1) if hatted else unit(0, 0.45, -0.9)
        tie = H + back * R * 1.1
        pp.add("tail", T([tie, tie + (0, -R * 0.25, -R * 0.38), H + (0, -R * 1.45, -R * 1.2)], [R * 0.2, R * 0.19, R * 0.05]), P, "hair")
        pp.add("tie", torus(tie + back * R * 0.06, R * 0.15, 0.02, pitch=math.atan2(back[2], back[1]), seg=14, ring=6), W, "hair", fuse=False)
    return None, None


CUTS = ["Short", "Spiky", "Long", "Swept", "Afro", "Bun", "Braids", "Bunches", "Bowl", "Ponytail", "Curls", "Bald"]
for cut in CUTS:
    for hatted in (False, True):
        if hatted and cut in ("Afro", "Bowl", "Bald"):
            continue                 # the same under a hat: the reader takes the plain piece
        name = f"hair.{cut}" + (".hat" if hatted else "")
        if not want(name):
            continue
        window, extra = hair(cut, hatted)

        def both(Q, window=window, extra=extra):
            m = np.zeros(len(Q))
            if window is not None:
                m = np.maximum(m, window(Q))
            if extra is not None:
                m = np.maximum(m, extra(Q))
            return m
        piece(name, "hair", voxel=0.012, smooth=7)

# ---------------------------------------------------------------------------------------------------------------- hats
def hat(kind):
    add = lambda name, shape, zone, bone="head": pp.add(name, shape, zone, bone, fuse=False)
    if kind == "Beret":
        at = H + (0, R * 0.84, -0.035)
        add("crown", lathe(at, [(R * 0.6, -R * 0.3), (R * 1.0, -R * 0.22), (R * 1.16, -R * 0.02), (R * 1.08, R * 0.2), (R * 0.7, R * 0.38), (R * 0.25, R * 0.45)],
                           pitch=-0.2, sz=0.95), P)
        add("band", torus(at + (0, -R * 0.26, 0.012), R * 0.98, 0.03, pitch=-0.2), A)
        add("stalk", cone(at + (0, R * 0.38, -0.012), at + (0, R * 0.56, -0.03), 0.022, 0.014), SHADE)
    elif kind == "Cap":
        at = H + (0, R * 0.36, -0.015)
        add("crown", lathe(at, [(R * 1.09, 0), (R * 1.1, R * 0.2), (R * 0.98, R * 0.52), (R * 0.62, R * 0.78), (R * 0.2, R * 0.86)], pitch=-0.12), P)
        add("peak", E(H + (0, R * 0.45, R * 1.1), (R * 0.78, 0.02, R * 0.6), pitch=0.2), A)
        add("button", E(at + (0, R * 0.87, -0.03), (0.024, 0.014, 0.024), sub=2), A)
    elif kind == "Fedora":
        at = H + (0, R * 0.3, -0.015)
        add("crown", lathe(at, [(R * 1.05, 0), (R * 1.03, R * 0.5), (R * 0.9, R * 0.86), (R * 0.5, R * 0.95), (R * 0.2, R * 0.9)], pitch=-0.1), P)
        add("brim", lathe(at + (0, -R * 0.06, 0), [(R * 0.9, -0.012), (R * 1.45, -0.014), (R * 1.5, 0.004), (R * 0.9, 0.014)], pitch=-0.1, sz=0.93), P)
        add("band", torus(at + (0, R * 0.12, 0), R * 1.05, 0.028, pitch=-0.1), A)
    elif kind == "Cowboy":
        at = H + (0, R * 0.38, -0.015)
        add("crown", lathe(at, [(R * 0.98, 0), (R * 0.95, R * 0.55), (R * 0.82, R * 0.92), (R * 0.4, R * 0.86), (R * 0.1, R * 0.74)], pitch=-0.08, sz=1.05), P)
        def curl(Vb):
            Vb = Vb.copy(); Vb[:, 2] += 0.9 * np.abs(Vb[:, 0]) ** 3; return Vb
        add("brim", E(at + (0, -R * 0.02, 0), (R * 1.62, R * 0.3, R * 1.38), deform=lambda Vb: curl(Vb * np.array([1, 1, 0.16])), sub=4), P)
        add("band", torus(at + (0, R * 0.1, 0), R * 1.0, 0.026, pitch=-0.08), A)
    elif kind == "WideHat":
        at = H + (0, R * 0.42, -0.02)
        add("crown", lathe(at, [(R * 1.05, 0), (R * 1.06, R * 0.3), (R * 0.9, R * 0.66), (R * 0.5, R * 0.88), (R * 0.15, R * 0.92)], pitch=-0.14), P)
        add("brim", lathe(at + (0, -R * 0.04, 0), [(R * 0.9, -0.01), (R * 1.6, -0.012), (R * 1.64, 0.004), (R * 0.9, 0.014)], pitch=-0.14, sz=0.96), P)
        add("band", torus(at + (0, R * 0.08, 0), R * 1.06, 0.028, pitch=-0.14), A)
    elif kind == "Helmet":
        at = H + (0, R * 0.5, -R * 0.06)
        def half(Vb):
            Vb = Vb.copy(); Vb[:, 2] = np.maximum(Vb[:, 2], -0.35); return Vb
        add("shell", E(at, (R * 1.2, R * 0.8, R * 1.28), pitch=-0.12, deform=half, sub=4), P)
        add("stripe", E(at + (0, R * 0.02, 0), (R * 0.17, R * 0.815, R * 1.3), pitch=-0.12, deform=half, sub=3), A)
        add("peak", E(H + (0, R * 0.36, R * 1.12), (R * 0.62, 0.02, R * 0.3), pitch=0.18), P)
    elif kind == "HardHat":
        at = H + (0, R * 0.55, -R * 0.02)
        def half(Vb):
            Vb = Vb.copy(); Vb[:, 2] = np.maximum(Vb[:, 2], -0.3); return Vb
        add("crown", E(at, (R * 1.14, R * 0.82, R * 1.18), deform=half, sub=4), P)
        add("brim", lathe(H + (0, R * 0.3, 0), [(R * 0.9, -0.012), (R * 1.42, -0.012), (R * 1.44, 0.006), (R * 0.9, 0.014)], sz=0.96), P)
        add("ridge", E(at + (0, R * 0.02, 0), (R * 0.12, R * 0.84, R * 1.16), deform=half, sub=3), A)
    elif kind == "Beanie":
        at = H + (0, R * 0.5, -R * 0.06)
        def half(Vb):
            Vb = Vb.copy(); Vb[:, 2] = np.maximum(Vb[:, 2], -0.3); return Vb
        add("crown", E(at, (R * 1.16, R * 0.86, R * 1.2), pitch=-0.15, deform=half, sub=4), P)
        add("cuff", torus(H + (0, R * 0.3, -R * 0.04), R * 1.1, 0.045, pitch=-0.15), A)
        add("pompom", E(at + (0, R * 0.9, -R * 0.14), (R * 0.22,) * 3), A)
    elif kind == "Bandana":
        at = H + (0, R * 0.42, -R * 0.08)
        def half(Vb):
            Vb = Vb.copy(); Vb[:, 2] = np.maximum(Vb[:, 2], -0.2); return Vb
        add("cloth", E(at, (R * 1.14, R * 0.86, R * 1.18), deform=half, sub=4), P)
        knot = H + (0, R * 0.2, -R * 1.2)
        add("knot", E(knot, (R * 0.17,) * 3, sub=2), A)
        for s in (1, -1):
            add(f"tail{s}", cone(knot, knot + (s * R * 0.28, -R * 0.55, -R * 0.25), 0.032, 0.02), A, "hair")
    elif kind == "Headband":
        add("band", torus(H + (0, R * 0.38, -R * 0.02), R * 1.1, 0.032, pitch=0.18), P)
        knot = H + (0, R * 0.62, -R * 1.14)
        add("knot", E(knot, (R * 0.13,) * 3, sub=2), A)
        for s in (1, -1):
            add(f"tail{s}", cone(knot, knot + (s * R * 0.22, -R * 0.7, -R * 0.3), 0.03, 0.018), A, "hair")
    elif kind == "Visor":
        add("band", torus(H + (0, R * 0.4, -R * 0.02), R * 1.1, 0.03, pitch=0.15), A)
        add("peak", E(H + (0, R * 0.36, R * 1.12), (R * 0.72, 0.02, R * 0.44), pitch=0.22), P)
    elif kind == "NurseCap":
        at = H + (0, R * 1.14, R * 0.42)
        add("cap", Box(at, (R * 0.52, 0.075, 0.04), pitch=-0.3), P)
        front = at + (0, 0.004, 0.036)
        add("crossV", Box(front, (0.014, 0.038, 0.012), pitch=-0.3, sub=2), A)
        add("crossH", Box(front, (0.038, 0.014, 0.012), pitch=-0.3, sub=2), A)
    elif kind == "Bow":
        normal = unit(0, 0.78, -0.62)
        knot = H + normal * R * 1.2
        for s in (1, -1):
            add(f"loop{s}", E(knot + (s * R * 0.44, R * 0.06, R * 0.08), (R * 0.42, R * 0.25, R * 0.12), pitch=0.9, roll=s * 0.35), P)
            add(f"tail{s}", cone(knot, knot + (s * R * 0.28, -R * 0.55, -R * 0.42), 0.034, 0.022), A)
        add("knot", E(knot + normal * R * 0.1, (R * 0.2, R * 0.2, R * 0.15), pitch=0.9, sub=2), A)
    elif kind == "Mask":
        c = H + (0, R * 0.12, -R * 0.12)
        cap(low=0.42, wide=1.05, ear=-0.75, nape=-0.9, c=c, r=(R * 1.12, R * 1.08, R * 1.06), name="hood")
        for i in range(7):
            a0, a1 = 0.62 - i * (2.6 / 7), 0.62 - (i + 1) * (2.6 / 7)
            p0 = c + (0, math.cos(a0) * R * 1.08, math.sin(a0) * R * 1.06)
            p1 = c + (0, math.cos(a1) * R * 1.08, math.sin(a1) * R * 1.06)
            pp.add(f"crest{i}", T([p0, p1], [0.04, 0.04], seg=8, per=1), A, "head", fuse=False)
    return None


HATS = ["Beret", "Cap", "NurseCap", "Bow", "Mask", "Fedora", "Helmet", "HardHat", "Beanie", "Bandana", "Headband", "Cowboy", "Visor", "WideHat"]
for kind in HATS:
    if want("hat." + kind):
        piece("hat." + kind, "hat", cut=hat(kind))

# ---------------------------------------------------------------------------------------------------------------- tops
OFF = 0.014                      # how far a garment stands off the body
CH_C = V((0, lerp(WAIST, NECK, 0.52), 0)); CH_R = V((TW * 0.95, (NECK - WAIST) * 0.66, TD * 0.95))


def torso_shell(hem_y, flare=0.0, name="body"):
    """A garment's body: over the chest and down to its hem (flared out by `flare` there)."""
    pp.add(name + "chest", E(CH_C, CH_R + OFF), P, "spine")
    pp.add(name + "waist", E((0, WAIST - 0.01, 0.004), (TW * 0.9 + OFF, (WAIST - HIP) * 0.9, TD * 0.95 + OFF)), P, "spine")
    low = min(hem_y, HIP + 0.02)
    pp.add(name + "hem", lathe((0, 0, 0), [(TW * 0.98 + OFF + flare, low), (TW * 0.96 + OFF + flare * 0.5, lerp(low, WAIST, 0.5)), (TW * 0.9 + OFF, WAIST)],
                               sz=(TD + OFF) / (TW + OFF), seg=20), P, "hips")


def sleeves(to=1.0, r=1.14, name="sleeve"):
    """Sleeves from the shoulder: to 1 at the wrist, shorter as a share of the upper arm when under 0.5."""
    if to <= 0.5:
        end = lerp(SH, EL, to * 2 * 0.62)
        pp.add(name, T([SH, end], [AR * r, AR * r * 1.02]), P, "upperArm.{s}", mirror=True)
        return
    pp.add(name + "U", T([SH, EL], [AR * r, AR * r * 0.95]), P, "upperArm.{s}", mirror=True)
    pp.add(name + "F", T([EL, lerp(EL, WR, 0.93)], [AR * r * 0.95, AR * r * 0.92]), P, "foreArm.{s}", mirror=True)


def stripes_on(parts, period=0.062, lo=HIP, hi=NECK):
    pp.paint(lambda Q: inside(np.sin((Q[:, 1] - lo) * 2 * math.pi / period) * 0.01 + 0.002, 0.003) * (Q[:, 1] > lo) * (Q[:, 1] < hi),
             STRIPE, on=parts)


if want("top.Shirt"):
    torso_shell(HIP + 0.03)
    sleeves(1.0, 1.1)
    stripes_on(["bodychest", "bodywaist", "bodyhem", "sleeve"])
    piece("top.Shirt", "top", "torso,arms")
if want("top.Tee"):
    torso_shell(HIP + 0.03)
    sleeves(0.5, 1.16)
    stripes_on(["bodychest", "bodywaist", "bodyhem", "sleeve"])
    pp.paint(lambda Q: inside(np.hypot(Q[:, 0], (Q[:, 1] - NECK) * 1.2) - 0.07, 0.003), A, on=["bodychest"])     # the neckband
    piece("top.Tee", "top", "torso")
if want("top.Jacket"):
    torso_shell(HIP + 0.015)
    sleeves(1.0, 1.2)
    # A ribbed band at the hem and the cuffs, a zip down the front and two pocket flaps
    pp.paint(lambda Q: inside(Q[:, 1] - (HIP + 0.045), 0.003), SHADE, on=["bodyhem", "bodywaist"])
    cuff = lerp(EL, WR, 0.74)
    pp.paint(lambda Q: inside(Q[:, 1] - cuff[1], 0.003) * (np.abs(Q[:, 0]) > TW + 0.02), SHADE, on=["sleeveF"])
    pp.paint(lambda Q: inside(np.abs(Q[:, 0]) - 0.007, 0.002) * (Q[:, 2] > 0) * (Q[:, 1] < NECK - 0.02), A, on=["body"])
    pp.paint(lambda Q: inside(np.maximum(np.abs(np.abs(Q[:, 0]) - TW * 0.55) - 0.03, np.abs(Q[:, 1] - (WAIST - 0.01)) - 0.006), 0.002) * (Q[:, 2] > 0),
             SHADE, on=["body"])
    piece("top.Jacket", "top", "torso,arms")
if want("top.Coat"):
    hem = lerp(KN[1], HIP, 0.45)
    torso_shell(HIP - 0.0, flare=0.012)
    pp.add("skirts", lathe((0, 0, 0), [(TW * 1.08 + OFF + 0.03, hem), (TW * 1.02 + OFF + 0.02, lerp(hem, HIP, 0.6)), (TW * 0.98 + OFF + 0.012, HIP + 0.03)],
                           sz=(TD + OFF + 0.012) / (TW + OFF), seg=20), P, "skirt")
    sleeves(1.0, 1.2)
    # The opening down the front, a dark line; the cuffs turned back
    pp.paint(lambda Q: inside(np.abs(Q[:, 0] - 0.004) - 0.004, 0.002) * (Q[:, 2] > 0) * (Q[:, 1] < NECK - 0.03), SHADE, on=["body", "skirts"])
    cuff = lerp(EL, WR, 0.78)
    pp.paint(lambda Q: inside(Q[:, 1] - cuff[1], 0.003) * (np.abs(Q[:, 0]) > TW + 0.02), SHADE, on=["sleeveF"])
    piece("top.Coat", "top", "torso,arms")
if want("top.Robe"):
    hem = AN[1] + 0.02
    torso_shell(HIP, flare=0.014)
    pp.add("skirts", lathe((0, 0, 0), [(TW * 1.25 + 0.04, hem), (TW * 1.12 + 0.03, lerp(hem, HIP, 0.5)), (TW * 0.98 + OFF + 0.014, HIP + 0.03)],
                           sz=0.82, seg=20), P, "skirt")
    sleeves(1.0, 1.35)
    pp.paint(lambda Q: inside(np.abs(Q[:, 0] - 0.004 - (NECK - Q[:, 1]) * 0.0) - 0.006, 0.002) * (Q[:, 2] > 0) * (Q[:, 1] < NECK - 0.03), A, on=["body", "skirts"])
    pp.paint(lambda Q: inside(np.abs(Q[:, 1] - WAIST) - 0.014, 0.003), A, on=["body"])                             # a sash
    piece("top.Robe", "top", "torso,arms,legs")
if want("top.Swimsuit"):
    pp.add("suit", E(CH_C + (0, -0.02, 0), CH_R * np.array([1, 0.8, 1]) + 0.006), P, "spine")
    pp.add("suitw", E((0, WAIST - 0.01, 0.004), (TW * 0.9 + 0.006, (WAIST - HIP) * 0.9, TD * 0.95 + 0.006)), P, "spine")
    piece("top.Swimsuit", "top", "")

# ---------------------------------------------------------------------------------------------------------------- bottoms
def seat(r=1.0):
    pp.add("seat", E((0, HIP + 0.035, 0), (TW * 0.94 * r + 0.008, 0.08, TD * 0.97 * r + 0.008)), P, "hips")


if want("bottoms.Trousers"):
    seat()
    pp.add("thigh", T([LT, KN], [LR * 1.02, LR * 0.95]), P, "thigh.{s}", mirror=True)
    pp.add("shin", T([KN, AN + (0, 0.025, 0)], [LR * 0.95, LR * 0.9]), P, "shin.{s}", mirror=True)
    piece("bottoms.Trousers", "bottom", "legs")
if want("bottoms.Shorts"):
    seat()
    pp.add("leg", T([LT, lerp(LT, KN, 0.7)], [LR * 1.08, LR * 1.1]), P, "thigh.{s}", mirror=True)
    hem = lerp(LT, KN, 0.62)[1]
    pp.paint(lambda Q: inside(Q[:, 1] - hem, 0.003), SHADE, on=["leg"])
    piece("bottoms.Shorts", "bottom", "")
if want("bottoms.Skirt"):
    hem = lerp(KN[1], HIP, 0.3)
    pp.add("skirt", lathe((0, 0, 0), [(TW * 1.3 + 0.03, hem), (TW * 1.16 + 0.02, lerp(hem, HIP, 0.55)), (TW * 0.96 + 0.01, HIP + 0.05), (TW * 0.9, HIP + 0.085)],
                          sz=0.84, seg=24), P, "skirt")
    pp.paint(lambda Q: inside(hem + 0.022 - Q[:, 1], 0.003), SHADE, on=["skirt"])
    piece("bottoms.Skirt", "bottom", "")
if want("bottoms.Swim"):
    seat(1.0)
    pp.add("leg", T([LT, lerp(LT, KN, 0.3)], [LR * 1.03, LR * 1.03]), P, "thigh.{s}", mirror=True)
    piece("bottoms.Swim", "bottom", "")

# ---------------------------------------------------------------------------------------------------------------- shoes, bag, extras
if want("shoes"):
    def shoe(Vb):
        # Flat underneath
        Vb = Vb.copy(); Vb[:, 2] = np.maximum(Vb[:, 2], -0.75); return Vb
    pp.add("shoe", E((AN[0], 0.044, 0.032), (0.058, 0.046, 0.098), deform=shoe), P, "foot.{s}", fuse=False, mirror=True)
    pp.add("sole", E((AN[0], 0.014, 0.032), (0.061, 0.016, 0.101), deform=shoe), A, "foot.{s}", fuse=False, mirror=True)
    pp.add("cuff", T([AN + (0, 0.0, 0), AN + (0, 0.035, 0)], [LR * 0.86, LR * 0.84], seg=10, per=1), P, "foot.{s}", fuse=False, mirror=True)
    piece("shoes.Shoes", "shoes", "feet")
if want("bag"):
    by = lerp(WAIST, CHEST, 0.6)
    pp.add("bag", Box((0, by, -TD - 0.055), (0.085, 0.095, 0.05), n=4), P, "bag", fuse=False)
    pp.add("flap", Box((0, by + 0.04, -TD - 0.075), (0.078, 0.048, 0.036), n=4), A, "bag", fuse=False)
    pp.add("strap", T([(0.075, NECK - 0.02, -TD * 0.9), (0.08, NECK + 0.012, 0.02), (0.085, CHEST, TD + 0.016), (0.085, WAIST + 0.01, TD + 0.006)],
                      [0.013, 0.013, 0.013, 0.013], seg=8, per=3), P, "spine", fuse=False, mirror=True)
    piece("bag.Bag", "bag")
if want("extra"):
    # A scarf wound round the neck with an end hanging in front
    pp.add("wrap", torus((0, NECK + 0.012, 0.004), 0.07, 0.036, sx=1.1, sz=1.0, seg=20), P, "spine", fuse=False)
    pp.add("tail", T([(0.045, NECK + 0.0, TD * 0.9), (0.05, NECK - 0.075, TD + 0.03), (0.046, NECK - 0.15, TD + 0.03)], [0.03, 0.028, 0.02], seg=10, per=3),
           P, "spine", fuse=False)
    piece("extra.Scarf", "accent")
    pp.add("collar", torus((0, NECK - 0.012, 0.002), 0.062, 0.022, sx=1.15, seg=20, ring=6), P, "spine", fuse=False)
    piece("extra.Collar", "accent")
    hem = lerp(KN[1], HIP, 0.45)
    for i in range(3):
        y = lerp(CHEST + 0.02, HIP + 0.02, i / 2)
        zc = TD * 0.95 + OFF + 0.014
        pp.add(f"btn{i}", E((0.034, y, zc), (0.015, 0.015, 0.01), sub=2), P, "spine" if i < 2 else "hips", fuse=False, mirror=True)
    piece("extra.Buttons", "accent")
    # Overalls: a bib on the chest and two straps over the shoulders, in the bottom's colour
    zc = TD * 0.95 + OFF + 0.004
    pp.add("bib", Box((0, lerp(WAIST, CHEST, 0.45), zc), (TW * 0.6, (CHEST - WAIST) * 0.62, 0.012), n=5), P, "spine", fuse=False)
    pp.add("strap", T([(TW * 0.5, CHEST - 0.01, zc), (TW * 0.56, NECK - 0.012, 0.0), (TW * 0.45, CHEST - 0.02, -zc), (TW * 0.4, WAIST, -zc)],
                      [0.014] * 4, seg=8, per=3), P, "spine", fuse=False, mirror=True)
    pp.add("stud", E((TW * 0.44, CHEST - 0.03, zc + 0.012), (0.012, 0.012, 0.006), sub=2), A, "spine", fuse=False, mirror=True)
    piece("extra.Overalls", "bottom")
    pp.add("bib", Box((0, lerp(WAIST, CHEST, 0.3), zc), (TW * 0.66, (CHEST - WAIST) * 0.7, 0.01), n=5), P, "spine", fuse=False)
    pp.add("lap", Box((0, HIP - 0.01, zc + 0.012), (TW * 0.92, (WAIST - HIP) * 0.95, 0.012), n=5), P, "hips", fuse=False)
    pp.add("tie", torus((0, WAIST, 0.004), TW * 0.93 + OFF, 0.009, sz=(TD + OFF) / (TW + OFF), seg=20, ring=5), P, "spine", fuse=False)
    piece("extra.Apron", "apron")
    # A vest open down the front over the top
    pp.add("vest", E(CH_C + (0, -0.012, 0), CH_R * np.array([1, 0.92, 1]) + OFF + 0.008), P, "spine")
    pp.add("vestw", E((0, WAIST, 0.004), (TW * 0.9 + OFF + 0.008, (WAIST - HIP) * 0.7, TD * 0.95 + OFF + 0.008)), P, "spine")
    piece("extra.Vest", "vest", cut=lambda Q: ((np.abs(Q[:, 0]) < 0.022 + (Q[:, 1] - WAIST) * 0.28) & (Q[:, 2] > 0)).astype(float))
    # Team Galactic's mark on the chest: our own, a yellow field with the mark's facets in ink
    pp.add("field", E((0, CHEST - 0.005, zc + 0.002), (0.036, 0.036, 0.008), sub=3), "#F2D23C", "spine", fuse=False)
    pp.add("facetA", Box((0, CHEST - 0.005, zc + 0.008), (0.02, 0.006, 0.004), roll=0.6, sub=2), "#2A2630", "spine", fuse=False)
    pp.add("facetB", Box((0, CHEST - 0.005, zc + 0.008), (0.02, 0.006, 0.004), roll=-0.6, sub=2), "#2A2630", "spine", fuse=False)
    piece("extra.GalacticMark", "", colours="true")

# ---------------------------------------------------------------------------------------------------------------- held
PR = PALM * MIR                  # the right hand, on -X
if want("held"):
    add = lambda name, shape, zone, bone: pp.add(name, shape, zone, bone, fuse=False)
    add("rod", cone(PR, PR + (-0.04, 0.34, 0.56), 0.015, 0.007), "#54423A", "hand.R")
    add("reel", E(PR + (0, 0.05, 0.08), (0.024,) * 3, sub=2), "#B0B4BE", "hand.R")
    piece("held.Rod", "", colours="true")
    tip = PR + (-0.02, 0.4, 0.32); hoop = tip + (0, 0.05, 0.07)
    add("pole", cone(PR, tip, 0.014, 0.012), "#B08854", "hand.R")
    add("hoop", torus(hoop, 0.08, 0.011, pitch=0.5, seg=16, ring=6), "#B0B4BE", "hand.R")
    add("bag", E(hoop + (0, -0.07, 0.02), (0.066, 0.085, 0.066)), "#EEEEE4", "hand.R")
    piece("held.Net", "", colours="true")
    can = V((-0.13, H[1] + R * 1.25, -0.28))
    add("shaft", T([(-0.2, WAIST, 0.07), can], [0.016, 0.016], seg=8, per=1), K, "spine")
    add("canopy", lathe(can, [(0.33, -0.03), (0.3, 0.02), (0.18, 0.07), (0.03, 0.095)], pitch=-0.55, roll=0.35, seg=20), P, "spine")
    add("rim", torus(can + (0, -0.03, 0), 0.32, 0.02, pitch=-0.55, roll=0.35, seg=20, ring=6), A, "spine")
    piece("held.Parasol", "parasol")
    low = V((-0.22, HIP + 0.02, 0.11)); top = V((-0.25, CHEST + 0.3, -0.06))
    add("handle", T([low, top], [0.019, 0.018], seg=8, per=1), "#966C42", "spine")
    add("head", T([top + (0, -0.03, 0.17), top, top + (0, 0.03, -0.19)], [0.01, 0.028, 0.01], seg=8, per=3), "#969AA6", "spine")
    piece("held.Pickaxe", "", colours="true")
    along = unit(0.55, 0.84, 0); roll = -math.atan2(0.55, 0.84)
    gb = V((-0.03, HIP + 0.03, TD + 0.075))
    add("body", E(gb, (0.115, 0.1, 0.04), roll=roll), P, "spine")
    add("bout", E(gb + along * 0.13, (0.085, 0.07, 0.038), roll=roll), P, "spine")
    add("neck", T([gb + along * 0.18, gb + along * 0.42], [0.02, 0.018], seg=8, per=1), K, "spine")
    add("hole", E(gb + along * 0.06 + (0, 0, 0.034), (0.028, 0.028, 0.01), sub=2), K, "spine")
    piece("held.Guitar", "guitar")
    at = V((-0.27, CHEST + 0.09, 0.02))
    add("box", Box(at, (0.06, 0.065, 0.13)), P, "spine")
    add("grip", T([at + (0, 0.075, -0.05), at + (0, 0.075, 0.05)], [0.012, 0.012], seg=8, per=1), P, "spine")
    add("lens", lathe(at + (0, 0.005, 0.12), [(0.04, 0), (0.04, 0.04), (0.03, 0.045)], pitch=math.pi / 2, seg=14), K, "spine")
    piece("held.Camera", "camera")
    ring = TW * 1.15 + 0.04
    add("ring", torus((0, HIP + 0.04, 0.004), ring, 0.045, seg=32, ring=8), P, "hips")
    for i in range(4):
        a = (i + 0.5) * math.pi / 2
        add(f"stripe{i}", E((math.sin(a) * ring, HIP + 0.04, 0.004 + math.cos(a) * ring), (0.03, 0.048, 0.048), yaw=a, sub=2), W, "hips")
    piece("held.FloatRing", "float")

pp.export(out)
