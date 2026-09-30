# Data formats

## Input: deformed sheet surface (`Assets/verts/verts_<n>.txt`)

One file per output step `n` of the forming simulation. Plain text, one vertex per line, coordinates in millimetres:

```
0.00000 -4.00000 3.97500
0.00000 -3.90000 3.97500
-0.10000 -3.90000 3.97500
...
```

- Every three consecutive lines form one triangle (triangle list, no index buffer); the files of the data set have
  498,600 lines (166,200 triangles).
- **The vertex order must be identical in all steps.** The lab uses this order to follow material points from one
  frame to the next (ground truth of displacement and 3D displacement).
- Only the sample surface is included (tools such as punch, die, and blank holder are removed).

**Other finite-element solvers.** `scripts/stl2verts.py` converts ASCII STL exports (one file per output step, e.g.
`t_<n>_stl.stl`) into this format; the part names of the sample are configurable in the script. Any solver that can
export the deformed sample surface as STL, or as a triangle list with a constant vertex order, can therefore be
used. The finite-element solver is not required to run the lab.

## Rendered images and flow maps (experiment folders)

Each analysis writes to a folder next to `Assets/` whose name is `Assets` followed by the experiment label:

| Folder | Created by |
|---|---|
| `Assetsexp_normal/` | *Start* (standard analysis) |
| `Assetslighting_<I>/` | illumination sweep (`0p1` = 0.1) |
| `Assetsshot_e<N>/`, `Assetsshot_clean/` | shot-noise sweep |
| `Assetsspeckle_<size>/`, `Assetsspeckle_p<s>/` | speckle sweep (material size / procedural diameter) |

Contents (examples for resolution 512):

```
cam_0/uv/im_<frame>_r512.png           rendered image of camera 0 (8-bit gray)
cam_1/uv/im_<frame>_r512.png           rendered image of camera 1
time_flow_u/, time_flow_v/             flow maps (PNG + .f32) of the temporal analysis
accuracy_raw_value[_ref]_[u|v]_r512.f32  flow and reference of the stage (sweeps)
depth_tv_r512.f32, depth_ref_r512.f32  stereo depth and reference [mm]
disparity_epe_r512.f32                 end-point error of the stereo disparity [px]
stereo_gt_dx_screen_r512.f32, ..._dy_  exact disparity in screen coordinates [px]
sceneflow_{tv,tv_b,ref}_{x,y,z}_r512.f32  3D displacement (TV, variant B, reference) [mm]
sceneflow_pos_ref_{x,y,z}_r512.f32     reference position in the first frame, rig frame [mm]
sceneflow_epe_r512.f32                 3D end-point error [mm]
depth_maps_r512.png, sceneflow_maps_r512.png   overview panels shown in the gallery
```

The raw maps of the accuracy analysis of the standard experiment are written to
`Assetsexp_normal/time_flow_v/nice_pics/accuracy_raw_value[_ref]_[u|v]_r<res>.f32` (overwritten by each run).

## Raw float maps (`*.f32`)

Little-endian binary:

| Offset | Type | Content |
|---|---|---|
| 0 | int32 | magic number |
| 4 | int32 | n (number of columns, image x) |
| 8 | int32 | m (number of rows) |
| 12 | float32[n·m] | values, index `i·m + j`, i = column x, j = row counted from the top |

Invalid pixels (outside the sample) are `NaN` or, in the accuracy maps, a large negative sentinel (about minus the
resolution); treat values below −100 as invalid. Reading in Python:

```python
import struct, numpy as np
raw = open(path, "rb").read()
_, n, m = struct.unpack("<iii", raw[:12])
a = np.frombuffer(raw, dtype="<f4", offset=12).reshape(n, m).T   # image[row][column]
a[a < -100] = np.nan
img = np.rot90(a)                                                # orientation of the manuscript figures
```

Component naming: in the raw maps, **u is the displacement along the image column direction (x)** and **v along the
row direction**; the rotation by 90° in the manuscript makes u the vertical component.

## Result tables (`Assets/analysis_results/*.tsv`)

Tab-separated, one line per stage; the header is the first line. When a sweep is repeated, stages with the same key
(label, resolution, regulariser, strain σ) are replaced and the previous table is kept as a backup
(`*_before_<timestamp>.tsv`).

**Sweeps** (`lighting_sweep.tsv`, `noise_sweep.tsv`, `speckle_sweep.tsv`)

| Column | Meaning |
|---|---|
| `experiment` | stage label (folder `Assets<label>`) |
| `lighting_intensity` / `peak_electrons`, `full_scale_relative_sigma` / `speckle_size` | parameter of the stage (procedural speckles are stored as negative sizes, `-s`) |
| `mean_v_error`, `std_v_error`, `min_v_error`, `max_v_error` | statistics of the pixel-wise relative error of flow component v over the evaluated sample region (1 = 100 %) |
| `u_mae`, `v_mae` | mean absolute error of the components [px] |
| `exx_rel_mae`, `eyy_rel_mae` | mean absolute strain error divided by the mean absolute reference strain |
| `render_res`, `regularization`, `strain_sigma` | settings of the stage |

**Depth** (`depth_results.tsv`): `depth_mae_mm`, `depth_bias_mm`, `depth_rel_mae` (relative to the distance),
`depth_rel_relief` (MAE divided by the mean absolute deviation of the reference depth from its mean),
`disparity_epe_px`, `selftest_mae_mm`, `n_px`, `ref_mean_mm`, `mapping` (array-to-screen mapping, `*` = locked).

**3D displacement** (`sceneflow_results.tsv`): `epe3d_mm` and `rel_epe3d` (mean 3D end-point error, absolute and
relative to the mean |D|), `mae_{x,y,z}_mm` and `rel_mae_{x,y,z}` (rig components), `mean_abs_ref_mm`,
`epe3d_b_mm` (variant B), `pos_mae_mm` (position error in frame A), `flow0_epe_px`, `flow1_epe_px`, `disp_epe_px`
(2D errors of the three flows), `selftest_mm`, `n_px`, `mapping`.

**Parameter study** (`param_study_latest.tsv`): one line per run with the parameters (`lambda`, `theta`, `nscales`,
`nwarps`, `iterations`, `epsilon`, `regularization`, `tgv_ratio`), `time_s`, and error statistics of u, v, ε_xx,
ε_yy (`*_bias`, `*_rmse`, `*_mae`, `*_corr`, `*_rel_mae`).

**DIC comparison** (`dic_comparison.tsv`, written by `scripts/dic_comparison.py`): `method`, `epe_px` (evaluation
region), `epe_edge_px` (edge band), `mae_u_px`, `mae_v_px`, `runtime_s`, `n_points`.

**Texture comparison** (`texture_comparison.tsv`): speckle statistics of the photographed and rendered textures —
`fwhm` (autocorrelation width, px), `aniso`, `mig` (mean intensity gradient), `contrast`, `dark` (dark fraction),
`chord_dark`, `chord_bright`, `skew`, `fmean`, `ks` (Kolmogorov–Smirnov distance of the gray-value distributions),
`dpsd` (distance of the radially averaged power spectra).

The tables used in the manuscript are included in the repository folder `results/`.
