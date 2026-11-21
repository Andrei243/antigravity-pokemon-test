# Cosmoem sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/cosmoem.py -- <the export's Cosmoem.glb> <out.glb>
#
# Grown from Cosmog's script (sibling("cosmog")): its colours, its black face and the cyan and pink of its face.
# From the references: a still cocoon. A ball of the night sky, deep blue going violet and pink low on one side, white
# sparkles in it, sits in a hard pale-gold shell: a broad oval plate behind it, wider than tall, and eight pointed
# petals of the same gold laid round the ball's rim like a half-open shutter, turned a little like a pinwheel, the
# longer ones reaching past the oval. On the front a black face with its eyes shut in two U-shaped lines, a short cyan
# stroke and a pink stroke left of Cosmog's marks.
# The skeleton: one body bone at (0, 0, 0.182). The export is 0.33 tall and 0.44 wide.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, hull, outward, srgb, inside, smoothstep, unit, eye_look, polygon, args, sibling

cg = sibling("cosmog")

SHELL, SHELLDARK = "#EEDC8C", "#D2BC66"
NAVY, VIOLET, PINK = "#22287A", "#5A3CB0", "#C870C8"
LINE = "#EEE6B8"

C = np.array([0.0, 0.0, 0.182])
R = 0.1
FACE_D = unit(0.12, -1, 0.06)
VOXEL = 0.0036


def shell(k):
    """The oval plate behind the ball, and the eight petals round its rim."""
    k.add("back", outward(ell(C + (0, 0.045, 0), (0.205, 0.07, 0.135), sub=5)), SHELL, "body", fuse=False,
          surface="shell")
    # a darker rim of the oval's front face, where the plate turns, so the oval reads as a dish
    k.add("backrim", outward(ell(C + (0, 0.06, 0), (0.212, 0.055, 0.142), sub=5)), SHELLDARK, "body", fuse=False,
          surface="shell")
    for i in range(8):
        a = math.radians(i * 45 + 8)
        L = 0.2 if i % 2 == 0 else 0.155
        if math.sin(a) < -0.6:
            L = min(L, 0.15)
        d = np.array([math.cos(a), 0, math.sin(a)])
        t = np.array([math.cos(a + 0.12), 0, math.sin(a + 0.12)])          # the tip turned: a pinwheel
        side = np.array([-math.sin(a), 0, math.cos(a)])
        r0 = 0.072
        root = C + d * r0 + (0, -math.sqrt(R * R - r0 * r0) - 0.004, 0)
        mid = C + (d * 0.55 + t * 0.45) * 0.115 + (0, -0.052, 0)
        tip = C + t * L + (0, -0.012, 0)
        far = mid + (tip - mid) * 0.6 + (0, -0.004, 0)
        n = np.array([0, -1.0, 0])
        pts = [root + side * 0.011 + n * 0.006, root - side * 0.011 + n * 0.006, root + side * 0.011 - n * 0.008,
               root - side * 0.011 - n * 0.008,
               mid + side * 0.024 + n * 0.008, mid - side * 0.024 + n * 0.008, mid + side * 0.024 - n * 0.009,
               mid - side * 0.024 - n * 0.009, far + side * 0.016, far - side * 0.016, tip]
        k.add(f"petal{i}", hull(pts), SHELL, "body", fuse=False, surface="shell")


def ball(k):
    k.add("ball", ell(C, (R, R, R), sub=5), NAVY, "body", surface="scales")
    # the nebula glowing violet and pink low on the +X side
    k.paint(lambda P: smoothstep(0.09, 0.02, np.linalg.norm(P - (C + (0.04, -0.05, -0.045)), axis=1)), VIOLET)
    k.paint(lambda P: smoothstep(0.06, 0.0, np.linalg.norm(P - (C + (0.05, -0.04, -0.06)), axis=1)) * 0.8, PINK)
    # sparkles in the night sky, one bright glow up on the -X side
    star = cg.star_outline()
    for d, s in (((-0.55, -0.7, 0.45), 0.011), ((0.62, -0.6, 0.3), 0.009), ((0.35, -0.75, -0.55), 0.007),
                 ((-0.5, -0.65, -0.5), 0.008), ((0.15, -0.6, 0.75), 0.007)):
        p, n = cg.on_ell(C, (R, R, R), d)
        k.decal(p, n, (s, s), cg.STAR, eps=0.0022, outline=star, rings=6, segs=72)
    p, n = cg.on_ell(C, (R, R, R), (-0.55, -0.62, 0.55))
    k.decal(p, n, (0.012, 0.012), "#F4F4FF", eps=0.0024, rings=6, segs=36)


def face(k):
    """The black face and Cosmog's cyan and pink, now short strokes: in every state of the eyes."""
    p, n = cg.on_ell(C, (R, R, R), FACE_D)
    k.decal(p, n, (0.056, 0.053), cg.BLACK, eps=0.0016, rings=10, segs=64)
    for (dx, dz), col, half in (((-0.012, 0.016), cg.CYAN, (0.0035, 0.009)), ((-0.009, -0.016), cg.MOUTH, (0.0035, 0.008))):
        q, m = cg.on_ell(C, (R, R, R), FACE_D + np.array([dx, 0, dz]) / R)
        k.decal(q, m, half, col, eps=0.003, up=(0.15, 0, 1), rings=6, segs=28)


CB, CL = srgb(cg.BLACK), srgb(LINE)


def u_eye(tilt=0.0):
    """Shut for good: a U-shaped line, which is the open look."""
    def look(u, v):
        c = np.tile(CB, (len(u), 1))
        c[(np.abs(u) < 0.82) & (np.abs(v - (-0.05 - 0.4 * (1 - u * u)) - tilt * u) < 0.19)] = CL
        return c
    return look


def eyes(k, state):
    p, n = cg.on_ell(C, (R, R, R), FACE_D)
    for s in (1, -1):
        q, m = cg.on_ell(C, (R, R, R), FACE_D + np.array([0.024 * s + 0.006, 0, 0.004]) / R)
        if state in ("Open", "Shut"):
            look = u_eye()
        elif state == "Fierce":
            look = u_eye(0.35)
        else:
            look = eye_look(state, u_eye(), CB, CL, line=0.19)
        k.decal(q, m, (0.014, 0.013), look if s > 0 else (lambda a, b, f=look: f(-a, b)), eps=0.003, rings=20,
                segs=56)


def cosmoem(src, out):
    k = Kit(src, coat="shell")
    ball(k)
    face(k)
    shell(k)
    k.build(voxel=VOXEL, smooth=4, band=0.005, wband=0.03)
    k.eye_states(lambda st: eyes(k, st))
    k.export(out)


if __name__ == "__main__":
    cosmoem(*args())
