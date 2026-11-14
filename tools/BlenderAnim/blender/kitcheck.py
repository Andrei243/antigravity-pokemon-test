# The kit's own check (plan 48, the kit session): every shape of sculptkit.py that is a closed piece is wound outward
# under any frame, and the winding check finds what is wrong with a piece that is not. No model is built. Run by
# Blender itself; it ends with 1 on a finding:
#   blender --background --factory-startup --python-exit-code 1 --python tools/BlenderAnim/blender/kitcheck.py
import math, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import numpy as np
from sculptkit import ell, tube, sheet, leaf, hull, slab, ring_band, lathe, volume, winding, outward, axes

wrong = []


def tell(name, shape, want=None):
    V, F = shape
    found = winding(V, F)
    ok = found == want and (want is not None or volume(V, F) >= 0)
    print(f"KITCHECK: {'ok' if ok else '!!'} {name}: {len(V)} points, {len(F)} faces, volume {volume(V, F):+.6f}, winding {found}")
    if not ok:
        wrong.append(name)
    return shape


def flip(shape):
    return shape[0], [f[::-1] for f in shape[1]]


tell("ell", ell((0, 0, 0), (0.1, 0.2, 0.3)))
tell("tube", tube([(0, 0, 0), (0, 0, 0.2), (0.1, 0, 0.3)], [0.03, 0.03, 0.02]))
tell("sheet", sheet(lambda u, v: np.stack([u * 0.2, v * 0.0, v * 0.2], axis=-1), 9, 9, 0.01))
# The kit's leaf is wound inside out and kept so (its docstring says why): a loose one is outward(leaf(...))
tell("leaf as it is", leaf((0, 0, 0), (0, 0, 1), 0.2, 0.08), "inside out")
for d in ((0, 0, 1), (1, 0, 0), (0, -1, 0.3), (-0.4, 0.2, -0.7)):
    tell(f"outward(leaf) along {d}", outward(leaf((0, 0, 0), d, 0.2, 0.08)))

box = [(x, y, z) for x in (-0.05, 0.05) for y in (-0.03, 0.03) for z in (0, 0.08)]
gem = [(0, 0, 0.1), (0, 0, -0.1)] + [(0.05 * math.cos(t), 0.05 * math.sin(t), 0) for t in np.linspace(0, 2 * math.pi, 7)[:-1]]
tell("hull of a box", hull(box))
tell("hull of a gem", hull(gem))
tell("hull of a box, turned over", flip(hull(box)), "inside out")
V, F = hull(box)
if len(F) != 6 or len(V) != 24:
    print("KITCHECK: !! a box's hull is", len(F), "faces of", len(V), "points, not 6 of 24 (each face its own points)")
    wrong.append("hull's faces")

star = [(0.1 * (1 if i % 2 == 0 else 0.45) * math.cos(i * math.pi / 5 + math.pi / 2),
         0.1 * (1 if i % 2 == 0 else 0.45) * math.sin(i * math.pi / 5 + math.pi / 2)) for i in range(10)]
for frame, ex, ez in (("a right-handed frame", (1, 0, 0), (0, 0, 1)), ("a left-handed frame", (-1, 0, 0), (0, 0, 1)),
                      ("a slanted frame", (1, 1, 0), (0, 0.3, 1))):
    a = tell(f"slab of a star in {frame}", slab(star, (0, 0, 0.3), ex, ez, 0.012))
    b = tell(f"slab of a star walked the other way in {frame}", slab(star[::-1], (0, 0, 0.3), ex, ez, 0.012))
    if abs(volume(*a) - volume(*b)) > 1e-9:
        print("KITCHECK: !! a slab's volume hangs on the way its outline is walked")
        wrong.append("slab's volume")
tell("slab, turned over", flip(slab(star, (0, 0, 0.3), (1, 0, 0), (0, 0, 1), 0.012)), "inside out")

for axis in ((0, 0, 1), (1, 0, 0), (0, 1, 0), (0, -1, 0), (0.3, -0.4, 0.8)):
    tell(f"ring_band round {axis}", ring_band((0, 0, 0.2), axis, 0.05, 0.03))
tell("ring_band that tapers", ring_band((0, 0, 0.2), (0, 0, 1), lambda v: 0.05 - 0.02 * v, 0.03))

barrel = [(0, 0), (0.05, 0), (0.05, 0.12), (0.035, 0.12), (0.035, 0.04), (0, 0.04)]
for d in ((0, 0, 1), (0, -1, 0), (1, 0, 0), (-0.5, -0.5, 0.2)):
    tell(f"lathe along {d}", lathe((0, 0, 0), d, barrel))
    tell(f"lathe along {d}, its profile the other way", lathe((0, 0, 0), d, barrel[::-1]))

A, B = hull(box), flip(hull([(x + 0.3, y, z) for x, y, z in box]))
tell("two shells, one turned over", (np.vstack([A[0], B[0]]), A[1] + [[i + len(A[0]) for i in f] for f in B[1]]), "a part inside out")
C = hull(box)
tell("a box with one face turned over", (C[0], [C[1][0][::-1]] + C[1][1:]), "faces against each other")
tell("one open face (can't be told)", (np.array([(0, 0, 0), (1, 0, 0), (1, 0, 1), (0, 0, 1.0)]), [[0, 1, 2, 3]]))

for d in ((0, 0, 1), (0.2, 0.5, 0.7), (-1, 0, 0), (0, -1, 0)):
    a, b = axes(d)
    if np.dot(np.cross(a, b), np.array(d, float)) <= 0 or abs(np.dot(a, b)) > 1e-9:
        print("KITCHECK: !! axes", d, "is no right-handed frame")
        wrong.append(f"axes {d}")

print("KITCHECK:", "all as it should be" if not wrong else f"{len(wrong)} wrong: {', '.join(wrong)}")
if wrong:
    sys.exit(1)
