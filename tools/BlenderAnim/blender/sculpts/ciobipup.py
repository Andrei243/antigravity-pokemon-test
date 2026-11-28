# Ciobipup sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/ciobipup.py -- <the export's Ciobipup.glb> <out.glb>
# Carpathia No. 10021 (plan 46), the project's own design: docs/art/reference/gemini/carpathia/10021-Ciobipup.jpg and
# CP-dex-1-plateau.md, "21. Ciobipup: a shepherd's puppy". From the picture: a fluffy white puppy in a play bow, its
# forelegs stretched out low in front with big round cream paws, its rump and hind legs up; a big round head (some two
# fifths of its height) with a fluffy crown and cheeks, a short muzzle, a round black nose and an open smile with a pink
# tongue; dark grey patches round both eyes, the eyes large and dark with glints; floppy cream ears hanging beside the
# face; a twisted red and white woollen cord round its neck with a red tassel bound in white at the throat; a great
# fluffy tail curled up over its back like a snail's shell, as large as the head, a cream swirl at its middle.
# Written to be grown from: mioritar.py takes its cord, tassel, eyes, tufts and tail from here.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, lathe, srgb, inside, unit, eye_look, polygon, args, outward, sibling

WHITE, CREAM, SHADE = "#F4F0E6", "#E6D7B8", "#D9CDB6"
PATCH, NOSE, MOUTH, TONGUE = "#3C3C42", "#1C1A1E", "#7A2A30", "#E8868E"
IRIS, PUPIL, GLINT, INK = "#5A3A26", "#141012", "#FFFFFF", "#EDE6D6"
RED, CORDW = "#C8303A", "#F6F4F0"


# --- shared by the line -----------------------------------------------------------------------------------------------
def tuft(k, name, base, d, length, r, col, bone, flat=(0, 1, 0), f=0.5, fuse=True, tip=0.12):
    """A pointed tuft of fur: a blade from base along d, r thick at its root, flattened across `flat`."""
    base = np.array(base, float)
    d = unit(*d)
    fl = np.array(flat, float); fl = fl - d * (fl @ d)
    if np.linalg.norm(fl) < 1e-3:
        fl = np.cross(d, (1.0, 0, 0))
    k.add(name, blade([base, base + d * length * 0.5, base + d * length], [r, r * 0.62 + r * tip * 0.3, r * tip], fl, f), col,
          bone, fuse=fuse)


def ring_frame(axis):
    a = unit(*axis)
    e1 = np.array([1.0, 0, 0]) - a * a[0]; e1 /= np.linalg.norm(e1)
    e2 = np.cross(a, e1)
    return a, e1, e2


def cord(k, c, axis, R, r, n, bone, prefix="cord", cols=(RED, CORDW)):
    """A twisted woollen cord round a neck: n short beads round a ring, each turned across it, red and white in turn."""
    a, e1, e2 = ring_frame(axis)
    c = np.array(c, float)
    for i in range(n):
        th = 2 * math.pi * i / n
        p = c + R * (math.cos(th) * e1 + math.sin(th) * e2)
        tan = -math.sin(th) * e1 + math.cos(th) * e2
        d = unit(*(tan * 0.75 + a * 0.66))           # the twist
        side = np.cross(d, p - c); side /= np.linalg.norm(side)
        V, F = ell((0, 0, 0), (r, r, r * 2.1), sub=2)
        M = np.stack([side, np.cross(d, side), d], axis=1)
        k.add(f"{prefix}{i}", outward((V @ M.T + p, F)), cols[i % 2], bone, fuse=False, surface="cloth")
    return a, e1, e2


def tassel(k, top, size, bone, prefix="tassel"):
    """A red tassel bound in white hanging from a white knot."""
    top = np.array(top, float)
    k.add(prefix + "knot", ell(top, (size * 0.34, size * 0.3, size * 0.32), sub=3), CORDW, bone, fuse=False,
          surface="cloth")
    k.add(prefix, lathe(top - (0, 0, size * 0.18), (0, 0, -1),
                        [(0, 0), (size * 0.22, 0.0), (size * 0.3, size * 0.25), (size * 0.42, size * 1.05),
                         (size * 0.3, size * 1.12), (0, size * 1.12)], seg=24), RED, bone, fuse=False,
          surface="cloth")
    k.add(prefix + "band", lathe(top - (0, 0, size * 0.18), (0, 0, -1),
                                 [(0, size * 0.12), (size * 0.29, size * 0.12), (size * 0.31, size * 0.3),
                                  (0, size * 0.3)], seg=24), CORDW, bone, fuse=False, surface="cloth")


def dog_eye(iris=IRIS, pupil=PUPIL, glint=GLINT):
    """A big dark eye: a brown iris round a black pupil, a big glint up and out and a small one low."""
    I, P, G = srgb(iris), srgb(pupil), srgb(glint)
    def look(u, v):
        c = np.tile(I, (len(u), 1))
        r = np.sqrt(u * u + (v + 0.04) ** 2)
        c[r < 0.62] = P
        c[((u - 0.26) / 0.28) ** 2 + ((v - 0.32) / 0.28) ** 2 < 1] = G
        c[((u + 0.3) / 0.13) ** 2 + ((v + 0.34) / 0.13) ** 2 < 1] = G
        return c
    return look


def plume(k, base, h, pts, radii, col, bone, swirl, centre, n_tufts=8, prefix="tail", lift=0.0):
    """A great curled tail in the plane of h (back and out) and up: pts are (along h, up) from base; its fluff in
    tufts round its outer edge; a cream swirl painted round the curl's middle on both faces."""
    base = np.array(base, float); h = unit(*h)
    P3 = [base + h * s + np.array((0, 0, z)) for s, z in pts]
    k.add(prefix, tube(P3, radii), col, bone)
    # Fluff round the outer edge of the curl
    cen = base + h * centre[0] + np.array((0, 0, centre[1]))
    for i in range(n_tufts):
        t = (i + 0.5) / n_tufts * (len(P3) - 1.5)
        a = int(t); f = t - a
        p = P3[a] * (1 - f) + P3[a + 1] * f
        rr = radii[a] * (1 - f) + radii[a + 1] * f
        out = p - cen; out = out - np.cross(h, (0, 0, 1)) * 0; out /= np.linalg.norm(out)
        k.add(f"{prefix}puff{i}", ell(p + out * rr * 0.62 + np.array((0, 0, lift * rr)), (rr * 0.58,) * 3, sub=3),
              col, bone)
    nrm = np.cross(h, (0, 0, 1.0)); nrm /= np.linalg.norm(nrm)
    def mask(Q):
        q = Q - cen
        across = q @ nrm
        inplane = q - np.outer(across, nrm)
        x, z = inplane @ h, inplane[:, 2]
        r = np.sqrt(x * x + z * z)
        th = (np.arctan2(z, -x) + math.pi) / (2 * math.pi)
        target = swirl[0] + swirl[1] * th
        return inside(np.abs(r - target) - swirl[2], 0.003) * (np.abs(across) < 0.2) * (r < swirl[0] + swirl[1])
    k.paint(mask, swirl[3], on=[prefix])


def on_ell(c, r, x, z, front=True):
    c, r = np.array(c, float), np.array(r, float)
    s = 1 - ((x - c[0]) / r[0]) ** 2 - ((z - c[2]) / r[2]) ** 2
    return np.array((x, c[1] + (-1 if front else 1) * r[1] * math.sqrt(max(s, 0.0)), z))


# --- Ciobipup -----------------------------------------------------------------------------------------------------
# The frame (joints: body (0, 0.03, 0.25), head (0, -0.14, 0.24), ears (+-0.09, -0.18, 0.36), legs (+-0.07, -0.1,
# 0.17), hinds (+-0.08, 0.14, 0.26), tail (0, 0.19, 0.33)): the play bow, the chest low and the rump up.
HC = np.array([0.0, -0.205, 0.292])
HR = np.array([0.13, 0.118, 0.118])
MC = np.array([0.0, -0.305, 0.255])
MR = np.array([0.062, 0.05, 0.046])


def ciobipup(src, out):
    k = Kit(src, coat="fur")

    # The trunk in the bow: the chest low, the rump high, the haunches fused into it
    k.add("chest", ell((0, -0.085, 0.175), (0.088, 0.09, 0.08)), WHITE, "body")
    k.add("middle", ell((0, 0.03, 0.225), (0.086, 0.115, 0.085), rot=(16, 0, 0)), WHITE, "body")
    k.add("rump", ell((0, 0.13, 0.275), (0.1, 0.08, 0.09)), WHITE, "body")
    k.add("neck", tube([(0, -0.08, 0.2), (0, -0.15, 0.255)], [0.075, 0.072]), WHITE, "head")

    # The forelegs stretched out low in front, big round cream paws
    for s, nm in ((1, "R"), (-1, "L")):
        k.add(f"leg{nm}", tube([(0.072 * s, -0.09, 0.15), (0.078 * s, -0.15, 0.07), (0.08 * s, -0.212, 0.04)],
                                [0.058, 0.052, 0.046]), WHITE, f"leg{nm}")
        k.add(f"legpaw{nm}", ell((0.082 * s, -0.236, 0.03), (0.053, 0.063, 0.032)), CREAM, f"leg{nm}")
        for j, dx in enumerate((-0.026, 0.0, 0.026)):
            k.add(f"legtoe{nm}{j}", ell((0.082 * s + dx * 1.15, -0.292, 0.024), (0.019, 0.018, 0.022)), CREAM, f"leg{nm}")
        # The hind legs standing, thick fluffy thighs fused into the rump
        k.add(f"hindtop{nm}", ell((0.08 * s, 0.15, 0.22), (0.062, 0.072, 0.085)), WHITE, f"hind{nm}")
        k.add(f"hind{nm}", tube([(0.083 * s, 0.155, 0.17), (0.087 * s, 0.145, 0.09), (0.088 * s, 0.135, 0.04)],
                                 [0.062, 0.055, 0.048]), WHITE, f"hind{nm}")
        k.add(f"hindpaw{nm}", ell((0.088 * s, 0.12, 0.028), (0.05, 0.058, 0.03)), CREAM, f"hind{nm}")
    # The legs' cream below the fur, crisp
    k.paint(lambda Q: inside(Q[:, 2] - 0.045, 0.004) * (np.abs(Q[:, 0]) > 0.03), CREAM, on=["leg", "hind"])

    # Fluffy tufts: the chest's ruff, the back, the thighs' feathering
    for i, x in enumerate((-0.05, -0.018, 0.018, 0.05)):
        tuft(k, f"ruff{i}", (x, -0.14, 0.17), (x * 2.5, -0.5, -1), 0.05, 0.032, WHITE, "body", flat=(0, 1, 0), f=0.7, tip=0.45)
    for i, (y, z) in enumerate(((-0.04, 0.27), (0.04, 0.3), (0.11, 0.34))):
        k.add(f"back{i}", ell((0, y, z), (0.06, 0.045, 0.03)), WHITE, "body")
    for s, nm in ((1, "R"), (-1, "L")):
        tuft(k, f"thightuft{nm}", (0.1 * s, 0.19, 0.2), (s * 0.4, 0.8, -0.6), 0.055, 0.03, WHITE, f"hind{nm}",
             flat=(1, 0, 0))
        tuft(k, f"elbowtuft{nm}", (0.098 * s, -0.12, 0.1), (s * 0.5, 0.6, -0.5), 0.045, 0.026, WHITE, f"leg{nm}",
             flat=(1, 0, 0))

    # The big round head, a fluffy crown and cheeks, a short muzzle
    k.add("head", ell(HC, HR), WHITE, "head")
    k.add("muzzle", ell(MC, MR), WHITE, "head")
    k.add("chin", ell(MC + (0, 0.01, -0.035), (0.045, 0.035, 0.028)), WHITE, "head")
    for i, (x, d) in enumerate(((-0.06, (-0.5, 0.2, 1)), (-0.02, (-0.1, -0.2, 1)), (0.025, (0.15, 0.25, 1)),
                                (0.065, (0.5, 0.0, 1)), (0.0, (0.0, 0.8, 0.8)))):
        tuft(k, f"crown{i}", HC + (x, -0.02 + 0.03 * (i == 4), 0.095), d, 0.034, 0.034, WHITE, "head",
             flat=(0, 1, 0), f=0.85, tip=0.65)
    for s, nm in ((1, "R"), (-1, "L")):
        for j, (dz, ln) in enumerate(((-0.02, 0.05), (-0.065, 0.045))):
            tuft(k, f"cheek{nm}{j}", HC + (s * 0.1, -0.03, dz), (s, -0.1, -0.45), ln * 0.8, 0.03, WHITE, "head",
                 flat=(0, 0, 1), f=0.85, tip=0.55)
        # Floppy cream ears hanging beside the face
        k.add(f"ear{nm}", blade([(0.1 * s, -0.19, 0.375), (0.142 * s, -0.2, 0.31), (0.15 * s, -0.205, 0.225)],
                                 [0.042, 0.048, 0.03], (1, 0, 0.2 * s), 0.42), CREAM, f"ear{nm}")
    k.add("nose", ell(MC + (0, -0.045, 0.014), (0.025, 0.017, 0.018), rot=(10, 0, 0)), NOSE, "head", fuse=False,
          surface="shell")

    # The great curled tail over the back, its plane turned 35 degrees out to +X, a cream swirl
    plume(k, (0, 0.2, 0.0), (0.57, 0.82, 0), [(0.0, 0.33), (0.04, 0.42), (0.025, 0.5), (-0.045, 0.545),
                                              (-0.115, 0.515), (-0.135, 0.445), (-0.095, 0.405), (-0.06, 0.44)],
          [0.058, 0.08, 0.09, 0.09, 0.082, 0.07, 0.055, 0.042], WHITE, "tail",
          (0.014, 0.05, 0.008, SHADE), (-0.055, 0.46), n_tufts=8, lift=0.1)

    # The twisted cord and its tassel at the throat
    a, e1, e2 = cord(k, (0, -0.135, 0.22), (0, -0.62, 0.78), 0.079, 0.0105, 26, "head")
    front = np.array((0, -0.135, 0.22)) - e2 * 0.082
    tassel(k, front + (0, -0.006, -0.004), 0.034, "head")

    # The dark patches round the eyes, and the open smile with its tongue
    E = on_ell(HC, HR, 0.058, 0.318) + (0, -0.001, 0)
    EN = unit(0.5, -1, 0.12)
    def blob(th):
        return 1.0 + 0.07 * math.cos(2 * th - 0.5) + 0.06 * math.sin(th) - 0.04 * math.cos(3 * th)
    k.decal(E + (0.004, 0, 0.002), EN, (0.043, 0.04), PATCH, eps=0.003, outline=blob, rings=12, segs=72,
            mirror=True)
    CM, CT = srgb(MOUTH), srgb(TONGUE)
    def mouth(u, v):
        c = np.tile(CM, (len(u), 1))
        c[(u / 0.62) ** 2 + ((v + 0.55) / 0.55) ** 2 < 1] = CT
        return c
    def d_shape(th):
        s = math.sin(th)
        return min(1.0, 0.38 / s) if s > 0 else 1.0
    k.decal(MC + (0, -0.03, -0.03), unit(0, -1, -0.3), (0.034, 0.026), mouth, eps=0.003, outline=d_shape,
            rings=12, segs=48)

    k.build(voxel=0.0055, smooth=3, band=0.005)

    look = dog_eye()
    lid, ink = srgb(PATCH), srgb(INK)
    def lay(state):
        k.decal(E, EN, (0.025, 0.027), eye_look(state, look, lid, ink), eps=0.005, rings=20, segs=56, mirror=True)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    ciobipup(*args())
