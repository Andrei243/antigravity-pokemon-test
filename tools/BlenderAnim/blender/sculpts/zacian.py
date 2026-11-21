# Zacian (Hero of Many Battles) sculpted from nothing on the skeleton and clips of the game's own export of it
# (sculptkit.py):
#   blender --background --factory-startup --python sculpts/zacian.py -- <the export's Zacian.glb> <out.glb>
#
# From the references: a tall blue wolf of the old tales with no sword. A coat of blue fur over the whole trunk, ragged
# in uneven points along the back and hanging in points down the flanks and belly; a long blue mane falling from the
# back of the head down over the throat and the front of the chest; cream legs under the coat, slim but no sticks,
# black hooves. A long narrow head: blue above with tall pointed ears, cream from the eyes down over the long muzzle,
# a pink nose; a black band through and behind the yellow eye; two red streaks over the brow and a red curl at the back
# of the jaw. A red flap curling up from each shoulder, two pink-red braids looping along each flank from the shoulder
# to the hip; a black arc over the shoulder; white scars, a cross on the hip and slashes on the shoulder. A great rust
# brush of a tail hanging behind. (The task's text said braided cyan fur: the pictures' braids are pink-red.)
#
# Written to be grown from: Zamazenta is built by these functions (sibling("zacian")), each taking the kit, a frame
# of numbers and its colours.
# Bones: body (0, 0.02, 0.47); head (0, -0.16, 0.55); legs (+-0.07, -0.19, 0.42); hind legs (+-0.07, 0.2, 0.42);
# tail (0, 0.25, 0.51).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, blade, hull, outward, srgb, inside, unit, polygon, lumpy, args, sibling,
                       eye_look)

R = sibling("raikou")
RR = sibling("rockruff")

# --- Zacian's colours -------------------------------------------------------------------------------------------------
C = dict(
    coat="#3A88DE", deep="#2A62BC", leg="#EEE6D4", hoof="#1C1A20", face="#F4EEE4", nose="#E2949A",
    mask="#1A1A22", mark="#E0645C", braid="#EC8774", tail="#C8695C", tail2="#B05A50",
    eye="#F6CC3A", iris="#F2B22A", pupil="#16121A", line="#F4EEE4",
)

# --- the frame ------------------------------------------------------------------------------------------------------
F = dict(
    x=0.075, fy=-0.19, hy=0.2,                  # the legs' joints
    bz=0.40, bw=0.112, deep=0.165,               # the trunk: its middle's height, half width, half depth
    front=-0.16, back=0.19,                      # the chest's and the rump's middles
    H=np.array([0.0, -0.36, 0.745]),             # the head's middle: high, forward of the mane
    HR=np.array([0.08, 0.095, 0.084]),            # a third larger than the reference's narrow head
    eye_dir=unit(0.6, -0.74, 0.24), eye_r=0.025,
    neck=[(0, -0.19, 0.5), (0, -0.27, 0.6), (0, -0.33, 0.69)],
)


def tuft(base, d, length, w, t):
    """A solid tapering point of fur (a flat-faced hull, thick at the root)."""
    return RR.spike(base, d, length, w, t)


def jit(i, a=0.25):
    """A fixed unevenness for the i-th tuft: no two of a row alike."""
    return 1 + a * math.sin(i * 2.399 + 0.7)


# --- the trunk and the legs --------------------------------------------------------------------------------------------
def add_trunk(k, F, coat):
    """A deep trunk hanging low between the legs: the chest the deepest, the rump a little higher."""
    z, w, d = F["bz"], F["bw"], F["deep"]
    k.add("chest", ell((0, F["front"], z), (w, 0.14, d)), coat, "body")
    k.add("middle", ell((0, (F["front"] + F["back"]) / 2, z - 0.005), (w + 0.004, 0.2, d * 0.96)), coat, "body")
    k.add("rump", ell((0, F["back"], z + 0.02), (w, 0.12, d * 0.92)), coat, "body")
    # One long flank over all three, so the sides run smooth between the shoulder and the thigh
    k.add("flank", ell((0, (F["front"] + F["back"]) / 2, z - 0.01), (w + 0.022, 0.33, d * 0.86)), coat, "body")


def add_legs(k, F, coat, low, hoof, scale=1.0):
    """
    No stilts: a broad shoulder and thigh mass fused over the upper half of each leg, down to z 0.2; under it a short
    lower leg bent back at the elbow and the hock, tapering but never a stick, to a paw with a black hoof in front.
    """
    x, s = F["x"], scale
    for nm, y, bone, front in (("leg", F["fy"], "leg{s}", True), ("hind", F["hy"], "hind{s}", False)):
        if front:
            k.add("shoulder", ell((x, y + 0.005, 0.335), (0.062 * s, 0.082 * s, 0.13)), coat, bone, mirror=True)
            pts = [(x, y, 0.3), (x, y + 0.028, 0.18), (x, y - 0.002, 0.1), (x, y - 0.012, 0.05)]
        else:
            k.add("thigh", ell((x, y + 0.02, 0.34), (0.068 * s, 0.098 * s, 0.135)), coat, bone, mirror=True)
            pts = [(x, y + 0.01, 0.3), (x, y + 0.04, 0.17), (x, y + 0.014, 0.1), (x, y - 0.004, 0.05)]
        k.add("u" + nm, tube(pts, [0.046 * s, 0.035 * s, 0.028 * s, 0.029 * s]), low, bone, mirror=True)
        py = pts[-1][1] - 0.022
        k.add("p" + nm, ell((x, py, 0.026), (0.034 * s, 0.046 * s, 0.026)), low, bone, mirror=True)
        a = np.array([x, py - 0.03 * s, 0.016])
        k.add("hoof" + nm, hull([a + (-0.03 * s, 0.03, -0.014), a + (0.03 * s, 0.03, -0.014), a + (-0.026 * s, 0.03, 0.014),
                                 a + (0.026 * s, 0.03, 0.014), a + (-0.02 * s, -0.012, -0.016), a + (0.02 * s, -0.012, -0.016),
                                 a + (0, -0.016, 0.0)]), hoof, bone, fuse=False, mirror=True, surface="shell")


def add_neck(k, F, coat):
    k.add("neck", tube(F["neck"], [0.08, 0.072, 0.066]), coat, "head")


# --- the head ---------------------------------------------------------------------------------------------------------
def add_head(k, F, top, face, nose, ears=True, ear_color=None):
    """A long narrow wolf's head: the skull, a long muzzle and jaw, the face's colour from the eyes down."""
    H, HR = F["H"], F["HR"]
    k.add("head", ell(H, HR), top, "head")
    k.add("snout", ell(H + (0, -0.098, -0.03), (0.042, 0.08, 0.04), rot=(-8, 0, 0)), face, "head")
    k.add("jaw", ell(H + (0, -0.075, -0.062), (0.036, 0.062, 0.022), rot=(-10, 0, 0)), face, "head")
    E = R.on_ell(H, HR, F["eye_dir"])[0]

    def lower(P):
        near = (np.linalg.norm((P - H) / (0.15, 0.22, 0.15), axis=1) < 1) & (P[:, 1] < H[1] + 0.05)
        return inside(P[:, 2] - (E[2] - 0.012 + 0.22 * (P[:, 1] - E[1])), 0.0025) * near
    k.paint(lower, face, on=["head", "snout", "jaw"])
    k.add("nose", ell(H + (0, -0.172, -0.012), (0.017, 0.014, 0.014)), nose, "head", fuse=False, surface="skin")
    if ears:
        for s, nm in ((1, "R"), (-1, "L")):
            b0 = H + np.array([0.042 * s, 0.03, 0.055])
            tip = b0 + np.array([0.02 * s, 0.035, 0.1])
            k.add("ear" + nm, blade([b0, (b0 + tip) / 2, tip], [0.034, 0.024, 0.006], unit(0.2 * s, 1, -0.4), 0.5),
                  ear_color or top, "head")


def eye_point(F):
    return R.on_ell(F["H"], F["HR"], F["eye_dir"])


def add_mask(k, F, color):
    """The black band through the eye and back along the cheek (a patch in every state, under the eye)."""
    p, n = eye_point(F)
    band = [(-1.0, -0.15), (-0.6, -0.75), (0.3, -0.9), (1.3, -0.45), (1.7, 0.05), (1.3, 0.35), (0.3, 0.55),
            (-0.6, 0.55)]
    for side in (1, -1):
        o = polygon([(side * a, b) for a, b in band])
        k.decal(p * (side, 1, 1), n * (side, 1, 1), (F["eye_r"] * 1.35, F["eye_r"] * 1.15), color, eps=0.0025,
                outline=o, rings=14, segs=72)


STROKE = R.STROKE


def stroke(k, c, n, length, width, color, up=(0, 0, 1), mirror=True, eps=0.0025):
    """One bold pointed stroke laid on the surface (a scar, a streak)."""
    k.decal(c, n, (width, length), color, up=up, eps=eps, outline=STROKE, rings=10, segs=40, mirror=mirror)


# --- fur --------------------------------------------------------------------------------------------------------------
def add_back_fur(k, F, colors, n=9, y0=-0.2, y1=0.26, z=0.55, bone="body", name="backfur"):
    """Uneven points of fur standing up and back along the back."""
    for i in range(n):
        y = y0 + (y1 - y0) * i / (n - 1)
        sx = 0.03 * (1 if i % 2 else -1)
        zz = z + 0.02 * math.cos((y - 0.02) / 0.3 * 1.4)
        k.add(f"{name}{i}", tuft((sx, y, zz), (sx * 2.2, 0.75, 0.55), 0.075 * jit(i, 0.35), 0.03, 0.016),
              colors[i % len(colors)], bone, fuse=False, surface="fur")


def add_skirt(k, F, colors, z=0.29, n=8, y0=-0.24, y1=0.25, length=0.1, name="skirt"):
    """Points of fur hanging down the lower flanks and the belly, uneven, to about z 0.18."""
    w = F["bw"]
    for i in range(n):
        y = y0 + (y1 - y0) * i / (n - 1)
        for s in (1, -1):
            base = (s * (w - 0.018), y, z + 0.012 * math.sin(i * 1.7))
            k.add(f"{name}{i}{'R' if s > 0 else 'L'}",
                  tuft(base, (s * 0.35, 0.18, -1), length * jit(i + (s < 0) * 3, 0.3), 0.032, 0.016),
                  colors[(i + (s < 0)) % len(colors)], "body", fuse=False, surface="fur")
    for i, y in enumerate(np.linspace(y0 + 0.06, y1 - 0.06, 4)):
        k.add(f"{name}b{i}", tuft((0.0, y, F["bz"] - F["deep"] + 0.03), (0, 0.15, -1), 0.06 * jit(i, 0.3), 0.034, 0.016),
              colors[i % len(colors)], "body", fuse=False, surface="fur")


# --- Zacian's own -----------------------------------------------------------------------------------------------------
def add_mane(k, F, coat, deep):
    """
    The long blue mane falling from behind the head over the throat and the front of the chest, the largest mass after
    the trunk: a fused fall of hair behind and under the head, ragged in uneven points at its foot and sides.
    """
    H = F["H"]
    k.add("manehood", ell(H + (0, 0.07, -0.02), (0.085, 0.075, 0.09), deform=lumpy(5, 3, 0.05)), coat, "head")
    k.add("manefall", ell((0, -0.29, 0.56), (0.108, 0.08, 0.17), deform=lumpy(6, 3, 0.06)), coat, "head")
    k.add("manebib", ell((0, -0.29, 0.43), (0.1, 0.075, 0.1), deform=lumpy(5, 2, 0.06)), coat, "body")
    n = 0
    for x, dz, ln in ((0.0, 0.0, 0.12), (0.045, 0.01, 0.1), (-0.045, 0.012, 0.11), (0.085, 0.05, 0.09),
                      (-0.085, 0.045, 0.085)):
        base = (x, -0.34, 0.37 + dz)
        k.add(f"manetip{n}", tuft(base, (x * 2, -0.25, -1), ln, 0.036, 0.018), deep if n % 2 else coat, "body",
              fuse=False, surface="fur"); n += 1
    # Locks hanging out of the mane's sides down the whole neck, uneven, so the neck is hair and no pillar
    for s in (1, -1):
        for j, (y, z, ln) in enumerate(((-0.33, 0.68, 0.08), (-0.3, 0.6, 0.1), (-0.28, 0.52, 0.11), (-0.29, 0.44, 0.09),
                                        (-0.25, 0.64, 0.075))):
            k.add(f"maneside{n}", tuft((s * 0.098, y, z), (s * 0.8, 0.35, -0.75), ln * jit(n), 0.034, 0.017),
                  coat if j % 2 else deep, "head" if z > 0.5 else "body", fuse=False, surface="fur"); n += 1
    # The nape: points back down the neck
    for j, (y, z) in enumerate(((-0.29, 0.75), (-0.24, 0.68), (-0.2, 0.6))):
        k.add(f"nape{j}", tuft((0.012 * (1 if j % 2 else -1), y, z), (0.2 * (1 if j % 2 else -1), 1, 0.15),
                               0.08 * jit(j), 0.032, 0.016), deep if j % 2 else coat, "head", fuse=False, surface="fur")


def bead_path(pts, n=26):
    p = np.array(pts, float)
    seg = np.linalg.norm(np.diff(p, axis=0), axis=1)
    cum = np.concatenate([[0], np.cumsum(seg)]) / seg.sum()
    t = np.linspace(0, 1, n)
    return np.stack([np.interp(t, cum, p[:, i]) for i in range(3)], axis=1)


def braid(pts, r, n=26):
    """A braid: a thick round cord beaded along its length (alternate swellings), a loose piece."""
    P = bead_path(pts, n)
    radii = [r * (1.0 if i % 2 == 0 else 0.62) for i in range(n)]
    radii[0] *= 0.7; radii[-1] *= 0.6
    return outward(tube(P, radii, seg=12, per=3))


def add_braids(k, F, color):
    """Two braids a side looping along the flank from the shoulder to the hip, hugging the body."""
    k.add("braidA", braid([(0.128, -0.12, 0.52), (0.148, -0.05, 0.42), (0.148, 0.04, 0.34), (0.138, 0.13, 0.36),
                           (0.118, 0.2, 0.44)], 0.019), color, "body", fuse=False, mirror=True, surface="cloth")
    k.add("braidB", braid([(0.132, -0.15, 0.44), (0.146, -0.06, 0.33), (0.14, 0.04, 0.29), (0.134, 0.13, 0.31),
                           (0.12, 0.2, 0.36)], 0.017), color, "body", fuse=False, mirror=True, surface="cloth")
    # The red flap curling up and back from each shoulder, where the braids begin
    k.add("flap", outward(blade([(0.112, -0.13, 0.52), (0.135, -0.11, 0.6), (0.14, -0.05, 0.64), (0.13, 0.0, 0.6)],
                                [0.036, 0.044, 0.032, 0.012], (1, 0, 0), 0.35)), color, "body", fuse=False, mirror=True,
          surface="fur")


def add_shoulder_arc(k, color):
    """The black arc over each shoulder, behind the flap: paint in the side's own frame."""
    def arc(P):
        d = np.hypot(P[:, 1] - (-0.07), P[:, 2] - 0.46)
        return inside(np.abs(d - 0.1) - 0.012, 0.0025) * (P[:, 2] > 0.47) * (P[:, 1] > -0.15) * (np.abs(P[:, 0]) > 0.04)
    k.paint(arc, color, on=["chest", "middle", "flank"])


def add_scars(k, F, color):
    """White scars: a cross on each hip, slashes on each shoulder, one on the back."""
    p, n = R.on_ell((0, F["back"], F["bz"] + 0.02), (F["bw"], 0.12, F["deep"] * 0.92), (0.8, 0.25, 0.55))
    stroke(k, p, n, 0.04, 0.009, color, up=unit(0, -0.7, 0.7))
    stroke(k, p, n, 0.04, 0.009, color, up=unit(0, 0.7, 0.7))
    for j, (y, z, ln) in enumerate(((-0.1, 0.47, 0.035), (-0.06, 0.45, 0.032), (-0.02, 0.44, 0.026))):
        p, n = R.on_ell((0, F["front"], F["bz"]), (F["bw"], 0.14, F["deep"]), (0.85, y + 0.16, z - F["bz"] + 0.1))
        stroke(k, p, n, ln, 0.008, color, up=unit(0, -0.45, 1))
    p, n = R.on_ell((0, 0.0, F["bz"]), (F["bw"], 0.17, F["deep"]), (0.25, 0, 1))
    stroke(k, p, n, 0.035, 0.008, color, up=unit(0, 1, 0), mirror=False)


def add_head_marks(k, F, mark):
    """Two red streaks over each brow, sloping back; a red curl at the back of the jaw."""
    H, HR = F["H"], F["HR"]
    for j, (d, ln) in enumerate((((0.38, -0.6, 0.72), 0.03), ((0.55, -0.3, 0.75), 0.026))):
        p, n = R.on_ell(H, HR, d)
        stroke(k, p, n, ln, 0.0075, mark, up=unit(0, 1, 0.55))
    k.add("curl", outward(blade([H + (0.065, 0.03, 0.01), H + (0.088, 0.06, -0.01), H + (0.092, 0.09, 0.015),
                                 H + (0.08, 0.1, 0.035)], [0.017, 0.019, 0.014, 0.006], (1, 0, 0), 0.5)),
          mark, "head", fuse=False, mirror=True, surface="fur")


def add_tail(k, color, color2, base=(0.0, 0.265, 0.5), size=1.0):
    """The great brush of a tail hanging behind, swung a little out to +X, ragged points at its foot."""
    b = np.array(base, float)
    s = size
    k.add("tailroot", tube([b, b + np.array((0.012, 0.06, -0.03)) * s], [0.035 * s, 0.05 * s]), color, "tail")
    k.add("tail", ell(b + np.array((0.03, 0.1, -0.16)) * s, np.array((0.08, 0.082, 0.165)) * s, rot=(18, 0, -15),
                      deform=lumpy(5, 3, 0.04)), color, "tail")
    for i, (dx, dy, ln) in enumerate(((0.0, 0.0, 0.09), (0.04, 0.02, 0.075), (-0.035, -0.01, 0.07), (0.02, 0.04, 0.065),
                                      (0.06, -0.01, 0.06))):
        a = b + np.array((0.03 + dx, 0.16 + dy, -0.29)) * s
        k.add(f"tailtip{i}", tuft(a, (dx * 4, 0.3, -1), ln * s, 0.036 * s, 0.017), color2 if i % 2 else color, "tail",
              fuse=False, surface="fur")
    for i, (z, sd) in enumerate(((-0.1, 1), (-0.17, -1), (-0.23, 1))):
        a = b + np.array((0.03 + 0.055 * sd, 0.12 - 0.15 * z, z)) * s
        k.add(f"tailside{i}", tuft(a, (sd, 0.4, -0.7), 0.06 * s, 0.032, 0.016), color2, "tail", fuse=False,
              surface="fur")


# --- the eyes ----------------------------------------------------------------------------------------------------------
def eye_top(u):
    return 0.25 + 0.45 * u           # the brow sloping down to the nose: a fierce eye


def eyes(k, F, state, colors):
    p, n = eye_point(F)
    look = R.eye_open(colors["eye"], colors["iris"], colors["pupil"], iris_r=(0.62, 0.78), pupil_r=0.42,
                      at=(-0.15, -0.08))
    R.lay_eyes(k, state, p + n * 0.001, n, F["eye_r"], look, colors["mask"], colors["line"], eye_top)


def zacian(src, out):
    k = Kit(src, coat="fur")
    add_trunk(k, F, C["coat"])
    add_legs(k, F, C["coat"], C["leg"], C["hoof"])
    add_neck(k, F, C["coat"])
    add_head(k, F, C["coat"], C["face"], C["nose"])
    add_mane(k, F, C["coat"], C["deep"])
    add_back_fur(k, F, (C["coat"], C["deep"]))
    add_skirt(k, F, (C["deep"], C["coat"]))
    add_braids(k, F, C["braid"])
    add_shoulder_arc(k, C["mask"])
    add_scars(k, F, C["face"])
    add_head_marks(k, F, C["mark"])
    add_mask(k, F, C["mask"])
    add_tail(k, C["tail"], C["tail2"])
    k.build(voxel=0.0075, band=0.004)
    k.eye_states(lambda s: eyes(k, F, s, C))
    k.export(out)


if __name__ == "__main__":
    zacian(*args())
