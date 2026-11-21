# Cosmog sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/cosmog.py -- <the export's Cosmog.glb> <out.glb>
#
# From the references: a little cloud of nebula gas, a round lumpy ball dark navy on top going to violet and then pink
# underneath. Its face is a black blob with rounded lobes on the front: a round cyan eye on each side lobe, two small
# yellow dots above between them, a pink mouth below. Two stalks of navy gas rise from the top out to the sides and end
# in puffy clouds of bright sky blue that glitter with white four-pointed sparkles; a little blue droplet of gas floats
# beside each. A thin gold crescent stands round the ball behind it, its points showing over the top and under the
# bottom.
# The skeleton: body (0, 0, 0.15), arms (+-0.06, 0, 0.21). The export is 0.42 tall.
# The line's frame: Cosmoem, Solgaleo and Lunala take this script's colours and its sparkle (sibling("cosmog")).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, slab, outward, srgb, inside, smoothstep, unit, eye_look, polygon, args

# --- the line's colours -----------------------------------------------------------------------------------------------
NAVY, VIOLET, PINK = "#2A2C78", "#6A3AA8", "#E0609E"
BLUE, BLUELIGHT = "#3E9CEC", "#6EC2F6"
BLACK = "#16141E"
CYAN, DOT, MOUTH, MOUTHDARK = "#52C8F4", "#F2E070", "#F08AA8", "#B03060"
GOLD, GOLDDARK = "#E6D47A", "#B8A048"
STAR = "#FFFFFF"


# --- the line's makes -------------------------------------------------------------------------------------------------
def on_ell(C, R, d):
    """The point of an ellipsoid along d from its centre, and the ellipsoid's normal there."""
    d = unit(*d); C = np.array(C, float); R = np.array(R, float)
    p = C + d / np.linalg.norm(d / R)
    n = (p - C) / R ** 2
    return p, n / np.linalg.norm(n)


def sparkle(k, name, bone, at, n, size, color=STAR, thick=0.004, up=(0, 0, 1.0)):
    """A bold four-pointed sparkle as a loose plate standing just off a surface, facing along n."""
    n = unit(*n)
    ex = np.cross(up, n); ex /= np.linalg.norm(ex)
    ez = np.cross(n, ex)
    pts = []
    for i in range(8):
        a = math.pi / 2 * (i // 2) + (math.pi / 4 if i % 2 else 0)
        r = 1.0 if i % 2 == 0 else 0.3
        pts.append((r * math.cos(a) * size, r * math.sin(a) * size))
    k.add(name, slab(pts, np.array(at, float) + n * thick * 0.5, ex, ez, thick), color, bone, fuse=False)


def star_outline(sharp=0.32):
    """A four-pointed star as a decal's outline."""
    pts = []
    for i in range(8):
        a = math.pi / 2 * (i // 2) + (math.pi / 4 if i % 2 else 0)
        r = 1.0 if i % 2 == 0 else sharp
        pts.append((r * math.cos(a), r * math.sin(a)))
    return polygon(pts)


def nebula(k, C, R, top=NAVY, mid=VIOLET, low=PINK, on=None):
    """Navy over the top, violet below the middle, pink at the bottom, eased like gas."""
    z0, h = C[2], R[2]
    k.paint(lambda P: smoothstep(z0 + 0.1 * h, z0 - 0.45 * h, P[:, 2]), mid, on=on)
    k.paint(lambda P: smoothstep(z0 - 0.5 * h, z0 - 0.95 * h, P[:, 2]), low, on=on)


# --- Cosmog -----------------------------------------------------------------------------------------------------------
C = np.array([0.0, 0.0, 0.158])
R = np.array([0.112, 0.1, 0.106])
FACE_D = unit(0, -1, -0.05)
CLOUD = np.array([0.13, 0.0, 0.335])
CS = 1.2                               # the clouds' size
VOXEL = 0.004


def body(k):
    k.add("ball", ell(C, R, sub=5), NAVY, "body")
    # a flattened disc for the face, so its patches lie on smooth ground
    fp, fn = on_ell(C, R, FACE_D)
    k.add("facedisc", ell(fp + fn * -0.018, (0.07, 0.03, 0.066)), NAVY, "body")
    # lumps of gas round the sides, back and bottom, none on the face
    golden = math.pi * (3 - math.sqrt(5))
    rs = np.random.RandomState(5)
    m = 0
    for i in range(60):
        z = 1 - 2 * (i + 0.5) / 60
        rr = math.sqrt(1 - z * z)
        d = np.array([math.cos(golden * i) * rr, math.sin(golden * i) * rr, z])
        if d[1] < -0.45 or d[2] > 0.75:
            continue
        m += 1
        s = 0.034 * (0.85 + 0.35 * rs.rand())
        p, n = on_ell(C, R, d)
        k.add(f"lump{m}", ell(p - n * 0.01, (s, s, s * 0.85)), NAVY, "body")


def arms(k):
    """The stalks of gas rising out to the sides, and the clouds at their ends."""
    base = np.array([0.05, 0.0, 0.225])
    k.add("arm", tube([base, (0.075, 0.0, 0.265), (0.1, 0.0, 0.29), CLOUD + (-0.012, 0, -0.03)],
                      [0.04, 0.035, 0.032, 0.034]), NAVY, "arm{s}", mirror=True)
    cl = [((0, 0, 0), (0.05, 0.042, 0.044)), ((0.04, 0.004, -0.012), (0.034, 0.032, 0.032)),
          ((-0.04, 0.006, -0.008), (0.032, 0.03, 0.03)), ((0.018, 0.008, 0.036), (0.034, 0.03, 0.03)),
          ((-0.024, 0.01, 0.03), (0.03, 0.028, 0.028)), ((0.05, 0.012, 0.022), (0.024, 0.024, 0.024)),
          ((-0.05, 0.012, 0.026), (0.022, 0.022, 0.022)), ((0.0, 0.02, -0.036), (0.03, 0.026, 0.026))]
    for j, (o, r) in enumerate(cl):
        k.add(f"cloud{j}", ell(CLOUD + np.array(o) * CS, np.array(r) * CS), BLUE, "arm{s}", mirror=True)
    # a droplet of gas floating out beside each cloud
    tip = CLOUD + (0.083, -0.005, 0.05)
    k.add("drop", outward(tube([tip + (-0.008, 0, -0.01), tip + (0.003, 0, 0.003), tip + (0.016, 0, 0.012)],
                               [0.013, 0.009, 0.003])), BLUE, "arm{s}", fuse=False, mirror=True)


def crescent(k, gold=GOLD):
    """The gold crescent round the ball behind it: thickest behind, its points over the top and under the bottom."""
    turn = math.radians(55)
    ax_y = np.array([math.sin(turn), math.cos(turn), 0.0])          # the crescent's plane: this and up
    rr = 0.15
    pts, rad = [], []
    for t in np.linspace(-0.62, 0.62, 15):
        a = math.pi * t
        pts.append(C + rr * (math.sin(a) * np.array([0, 0, 1.0]) + math.cos(a) * ax_y) + (0, 0.004, 0))
        rad.append(0.003 + 0.017 * math.cos(math.pi * t / 1.24) ** 1.3)
    nrm = np.cross(ax_y, (0, 0, 1.0))
    k.add("crescent", outward(blade(pts, rad, nrm, 0.45)), gold, "body", fuse=False, surface="shell")


def face_marks(k):
    """The black face with its lobes, the yellow dots and the pink mouth (in every state of the eyes)."""
    fp, fn = on_ell(C, R, FACE_D)
    fp = fp + fn * 0.002
    lobes = lambda th: 1.0 + 0.09 * math.cos(5 * th + math.pi / 2)
    k.decal(fp, fn, (0.068, 0.064), BLACK, eps=0.0018, outline=lobes, rings=10, segs=80)
    for s in (1, -1):
        k.decal(fp + (0.017 * s, 0, 0.03), fn, (0.0085, 0.01), DOT, eps=0.0032, rings=6, segs=28)
    cm, cd = srgb(MOUTH), srgb(MOUTHDARK)
    def mouth(u, v):
        c = np.tile(cm, (len(u), 1))
        c[v > 0.25] = cd
        return c
    k.decal(fp + (0, 0, -0.026), fn, (0.0135, 0.0125), mouth, eps=0.0032, rings=8, segs=36)


CC, CW, CB = srgb(CYAN), srgb("#D8F6FF"), srgb(BLACK)


def eye_open(u, v):
    c = np.tile(CC, (len(u), 1))
    c[np.hypot(u + 0.3, v - 0.32) < 0.26] = CW
    return c


def eyes(k, state):
    fp, fn = on_ell(C, R, FACE_D)
    for s in (1, -1):
        p, n = on_ell(C, R, unit(0.46 * s, -1, -0.12))
        look = eye_look(state, eye_open, CB, CC if state != "Open" else CB, line=0.2)
        k.decal(p, n, (0.0165, 0.0175), look if s > 0 else (lambda a, b, f=look: f(-a, b)), eps=0.0034,
                rings=20, segs=56)


def cosmog(src, out):
    k = Kit(src, coat="fur")
    body(k)
    arms(k)
    nebula(k, C, R, on=["ball", "lump", "facedisc"])
    # the stalks go from navy to the clouds' blue, the clouds lighter on top
    k.paint(lambda P: smoothstep(0.27, 0.315, P[:, 2]), BLUE, on=["arm"])
    k.paint(lambda P: smoothstep(0.36, 0.4, P[:, 2]) * 0.6, BLUELIGHT, on=["cloud"])
    crescent(k)
    face_marks(k)
    # sparkles on the clouds' fronts
    for s in (1, -1):
        sd = "R" if s > 0 else "L"
        for j, (o, size) in enumerate((((0.0, 0.0, 0.006), 0.019), ((0.03 * s, 0.0, 0.03), 0.011),
                                       ((-0.032 * s, 0.0, -0.018), 0.009))):
            c = CLOUD * np.array([s, 1, 1]) + np.array(o) * CS
            p = c + np.array([0, -0.05, 0])
            # bring it onto the cloud's front
            p, n = on_ell(c, (0.06, 0.055, 0.055), (0.25 * s, -1, 0.15))
            sparkle(k, f"spark{sd}{j}", f"arm{sd}", p, n, size * CS)
    k.build(voxel=VOXEL, smooth=4, band=0.005, wband=0.025)
    k.eye_states(lambda st: eyes(k, st))
    k.export(out)


if __name__ == "__main__":
    cosmog(*args())
