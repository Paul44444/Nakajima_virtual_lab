# Methods

This page summarises what the lab computes and how. Equations and parameter values follow the implementation in
`Assets/vis_3D.cs`, `Assets/TvL1Gpu.cs`, `Assets/TgvL1Gpu.cs`, and the compute shaders in `Assets/Resources/`.

## 1. Virtual experiment

**Sample geometry.** The deformed sheet surface of each output step of the forming simulation is loaded from
`Assets/verts/verts_<n>.txt` (triangle list, identical vertex order in every step; see
[data_formats.md](data_formats.md)). The same vertex order in all steps defines the material-point correspondence
used for the ground truth.

**Cameras.** Two virtual cameras form a stereo rig: both look at the sample centre from about 875 mm, rotated by
±10° about the global x axis (stereo baseline 304 mm), vertical field of view 6°, square images of adjustable
resolution. The scene is scaled with 3 scene units per millimetre.

**Illumination.** The two laboratory lamps of the setup are scaled by a dimensionless factor I (reference I = 1);
all other lights are disabled. For I < 1 the indirect ambient light is attenuated by the same factor. The camera
exposure is fixed (no automatic exposure), and the images pass through the fixed tone mapping of HDRP before 8-bit
quantisation.

**Speckle patterns.** Three kinds of textures are used: the photographed pattern, an experiment-derived texture, and
procedural patterns (`scripts/make_procedural_speckles.py`: circular black speckles of diameter s on a jittered grid
with spacing Δ = s/0.7, seamless 4096² textures with anti-aliased edges). The speckle size in image pixels is
measured as the full width at half maximum of the image autocorrelation (`scripts/speckle_size.py`).

**Image degradations** (applied to the rendered 8-bit images, gray value g ∈ [0, 1]):
- exposure factor k: g' = min(1, round(255·k·g)/255);
- photon shot noise with full-scale capacity N_max electrons: g' = Poisson(N_max·g)/N_max, clipped to [0, 1]
  (one reproducible realisation per stage).

## 2. Optical flow

The displacement u = (u₁, u₂) between two images I₀, I₁ minimises the TV-L1 energy

E(u) = ∫ |∇u₁| + |∇u₂| + λ |ρ(u)| dx,  ρ(u) = I₁(x + u₀) + ∇I₁(x + u₀)·(u − u₀) − I₀(x),

linearised about the current warp u₀. It is solved with the duality-based scheme of Zach, Pock and Bischof (2007) in
the formulation of Sánchez Pérez, Meinhardt-Llopis and Facciolo (2013): an auxiliary field v coupled to u by
(1/2θ)|u − v|², alternating a pointwise thresholding step for v and a dual projection step (time step τ = 0.25)
for u, inside a coarse-to-fine pyramid (zoom factor 0.5) with several warps per level.

**TGV variant.** Optionally the first-order TV term is replaced by second-order total generalized variation
(Bredies, Kunisch and Pock, 2010) with weights α₁ (first order) and α₀ = r·α₁ (second order); r = α₀/α₁ is set in
the *TV-Parameter* panel. TGV favours piecewise-affine instead of piecewise-constant fields and is implemented on the
GPU only.

**Parameters.** Reference values: λ = 0.05, θ = 0.3, 8 pyramid levels requested (limited internally so that the
coarsest level stays at least about 16 px, i.e. 7 levels at 1024 px), 8 warps per level, at most 500 iterations,
stopping tolerance 5·10⁻⁷. Empty fields in the *TV-Parameter* panel restore the automatic defaults. The sensitivity to
these parameters is studied by *Parameter: Neu* (see the appendix of the manuscript).

**Implementations.** `TvL1Gpu` / `TgvL1Gpu` solve one pyramid level on the GPU (`TVL1.compute`, `TGVL1.compute`);
the pyramid, warping, and I/O are handled in `vis_3D.find_displ`. A CPU implementation of TV-L1 serves as reference.
A guard against vanishing image gradients is applied in every warp (previously only in the first warp, which caused
NaN values in textureless regions).

## 3. Strain

The in-plane normal strains are the derivatives of the Gaussian-smoothed displacement (standard deviation σ in
pixels, default 12, set in the *TV-Parameter* panel), ε_xx = ∂u/∂x and ε_yy = ∂v/∂y in image coordinates. The same
operator is applied to the reference displacement.

## 4. Ground truth

**Image-plane displacement.** For every pixel of camera 0 in the reference frame, a ray through the pixel centre is
intersected with the mesh of the reference frame (Unity physics ray cast on a mesh collider). The hit triangle and its
barycentric coordinates identify a material point; the same barycentric combination on the mesh of the deformed frame
gives its new position, whose projection into the camera yields the reference displacement.

**Rays in double precision.** Pixel rays are built from two points of the same normalised device coordinate,
unprojected with the double-precision inverse of the camera matrix M = P·V (`projectionMatrix *
worldToCameraMatrix`). Earlier versions built the direction from a float near-plane point relative to the camera
centre; the rounding rotated the rays by about 10⁻³ rad (3 px). The log reports the round trip "pixel centre ↔
projection of the hit" (now about 4·10⁻⁴ px).

## 5. Stereo depth

The TV flow from camera 0 to camera 1 in the same frame provides the stereo correspondence p₁ = p₀ + s(p₀). Each pair
is triangulated linearly (DLT, least squares) from the two camera matrices M₀, M₁ in double precision; the depth is the
distance of the triangulated point from the centre of camera 0. The reference depth is the distance of the ray-cast
mesh point.

The flow solver works on arrays whose orientation relative to the screen depends on how images are read. The mapping
between array and screen axes (transpose, flips, component order, signs: 64 variants) is determined once against the
exact disparity with an offset-invariant median criterion and then kept fixed for all stages of a study (logged as
`o<k>m<l>`).

Self-tests: triangulating the exact projections of the reference points reproduces them (error 0 mm).

## 6. Three-dimensional displacement

For every pixel p of camera 0 in frame A:

1. stereo flow in frame A: p₁ = p + s_A(p) → P_A = DLT(p, p₁);
2. temporal flow of camera 0: p' = p + f₀(p);
3. temporal flow of camera 1, evaluated at p₁: p₁' = p₁ + f₁(p₁) → P_B = DLT(p', p₁');
4. displacement D = P_B − P_A.

Variant B (control) triangulates the deformed point from a second stereo flow in frame B instead of the temporal flow
of camera 1. The reference follows the material point by barycentric coordinates (section 4). Components are expressed
in the **stereo-rig frame**: x along the baseline (camera 0 → camera 1), z along the bisector of the two viewing
directions towards the cameras (approximately the sample normal), y = z × x.

## 7. Error measures

- **Accuracy maps** (*Genauigkeit*): value, reference, absolute error |‖u‖ − ‖u_ref‖| and relative error
  (absolute error / |u_ref|) per component; relative values are masked where the reference is close to zero (the
  report states the number and share of evaluated pixels; for strains the threshold is 10 % of the maximum absolute
  reference strain). The report also gives bias, RMSE, MAE (of the signed difference u − u_ref), and correlation.
  The manuscript figures exclude pixels with |u_ref| < 0.1 px from the relative error.
- **Sweeps**: mean relative displacement error over the evaluated sample region (displacement), mean absolute strain
  error normalised by the mean absolute reference strain (strain), mean absolute depth error normalised by the mean
  absolute deviation of the reference depth from its mean (depth), and the mean 3D end-point error (3D displacement).
- A 6 px border of the images (20 px for depth and 3D displacement) is excluded.

## 8. Conventions

- **Raw maps** are stored as `m[i][j]` with i = image column (x) and j = image row counted from the top.
- **Figures** of the manuscript show the images rotated by 90° (`numpy.rot90`) so that the dies appear left and right;
  in this orientation the vertical direction corresponds to the component u.
- **Units**: pixels for image-plane quantities; millimetres for depth and 3D displacement.

## Limitations

- The rendering is a controllable graphics pipeline, not a validated optical simulation: lens point-spread function,
  aperture-dependent blur, spectral response, and laboratory occluders are not calibrated. The images were rendered
  with the rasterised HDRP path; hardware ray tracing is available in HDRP but disabled in this project.
- One noise realisation is evaluated per stage; error bars describe spatial variation, not repeated acquisitions.
- The large deformation-gradient region of the meshes has no verified fracture surface; it is used as a stress test
  for image correspondence, not as a crack-detection benchmark.

## References

- C. Zach, T. Pock, H. Bischof: A duality based approach for realtime TV-L1 optical flow. DAGM 2007.
- J. Sánchez Pérez, E. Meinhardt-Llopis, G. Facciolo: TV-L1 optical flow estimation. Image Processing On Line 3 (2013).
- K. Bredies, K. Kunisch, T. Pock: Total generalized variation. SIAM J. Imaging Sciences 3 (2010).
- R. Hartley, A. Zisserman: Multiple View Geometry in Computer Vision. Cambridge University Press (2004).
