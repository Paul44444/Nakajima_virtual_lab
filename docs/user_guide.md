# User guide

The lab runs inside the Unity Editor in Play mode (scene `Assets/Scenes/SampleScene.unity`). The screen has three
groups of controls:

1. **Start panel** (top right): resolution, `with_exp`, `with_tv`, **Start** — runs the standard analysis.
2. **Control panel** (right edge, below the gallery button): tabs *Analyse*, *Sweeps*, *Stereo*, *Realbild* — all
   evaluations and studies.
3. **Scene panels** (left and centre, original interface): data paths, image series, display switches, experiment
   design.

Results are shown in the **gallery** (button *Bilder (N)*), a window with the rendered images and maps, zoom
(mouse wheel), pan (drag), reset (right click or double click), rotation in 90° steps, a camera filter, and a text
report with error statistics.

Every button and input field of the control panel shows an explanation when the mouse rests on it for about 0.4 s.

---

## 1. Start panel (standard analysis)

| Control | Function |
|---|---|
| resolution toggles / slider | render resolution of the square camera images (the slider in the *Analyse* tab allows 64–2048 px) |
| `with_exp` | render the camera images of the selected frames |
| `with_tv` | compute the optical flow between the frames |
| **Start** | runs the analysis; results go to the experiment folder `Assetsexp_normal` |

The frames of the analysis (time steps of the forming simulation) are set in the *Analyse* tab (field *Frames*,
default `1, 27`).

## 2. Control panel

Hovering over a tab opens it as a **preview**; it stays open while the mouse is inside the panel and closes when the
mouse leaves. **Clicking** a tab pins it open; clicking the pinned tab again collapses the panel. The *TV-Parameter*
panel behaves the same way (hover = preview, click = pin).

### Tab *Analyse* (single analysis)

| Control | Meaning |
|---|---|
| **Genauigkeit** (accuracy) | compares the last computed flow with the mesh reference: maps of u, v, absolute and relative error, strain maps, statistics; writes raw maps to `Assetsexp_normal/time_flow_v/nice_pics/` |
| **Bilder: Laden** | loads saved analysis images into the gallery |
| **Frames** + OK | time steps of the sample, e.g. `1, 27` or `28, 29` (ranges such as `27-29` are allowed); used by Start, accuracy, depth, and 3D displacement |
| **Analyse-k** + OK | exposure factor k of the normal analysis: images are scaled by k (8-bit clipping) before the flow computation; **1 = unmodified**. Ignored in sweeps |
| resolution slider | render resolution (64–2048 px), applied when the slider is released |
| **Speckle: …** | speckle texture of the Nakajima look (measured / random / random high-contrast) |
| **Look AKTIV: …** | render look: *klassisch* (classic) or *Nakajima* (cameras, lamps, and sample as in the real-image comparison) |
| **TV-Parameter** | opens the solver parameters (see below) |
| **Parameter: Neu / Laden** | runs / shows the numerical parameter study (λ, θ, pyramid levels, warps, stopping tolerance, regulariser) |

**TV-Parameter panel** (empty field = automatic default)

| Field | Meaning |
|---|---|
| lambda | weight of the data term; larger = less smoothing (reference 0.05) |
| theta | coupling between flow and auxiliary variable in the primal–dual scheme (reference 0.3) |
| nscales | pyramid levels; limited internally so that the coarsest level stays at least ~16 px |
| nwarps | warps per pyramid level (more = more accurate for large displacements, slower) |
| Iterationen | maximum iterations per warp |
| epsilon | stopping tolerance (reference 5·10⁻⁷) |
| Strain-Glättung sigma | Gaussian smoothing of the displacement before differentiation, px (empty = 12, 0 = off) |
| TGV alpha0/alpha1 | ratio of the TGV weights; only used when the regulariser is TGV |
| Übernehmen / Auto | apply the values / reset all fields to automatic |
| Rechenweg | GPU (compute shaders) or CPU (reference) |
| Regularisierung | TV (first order) or TGV (second order, GPU only) |

### Tab *Sweeps* (parameter studies)

Each study has a list field, **Neu** (run) and **Laden** (show the last result). Already computed stages are reused;
new results are merged into the table.

| Study | List entries | Output |
|---|---|---|
| **Licht** (illumination) | factors I of the two laboratory lamps, e.g. `0.01, 0.1, 1, 10` (1 = reference) | `analysis_results/lighting_sweep.tsv`, folders `Assetslighting_<I>` |
| **Rauschen** (shot noise) | full-scale electron capacity N_max, `∞` = noise-free, smaller = more noise | `noise_sweep.tsv`, folders `Assetsshot_e<N>` |
| **Speckle** | material sizes (e.g. `0.700`) or procedural patterns `p<s>` (speckle diameter s in texture pixels), separated by `;` | `speckle_sweep.tsv`, folders `Assetsspeckle_<label>` |
| **Belichtung** (exposure) | exposure factors k applied to the existing images (optionally `N=…` for noise) | `exposure_study_*.tsv` |

After each sweep the lab calls `scripts/plot_sweeps.py` and shows the curves in the gallery.

### Tab *Stereo*

| Control | Function |
|---|---|
| **Höhe: Neu / Licht / Speckle** | stereo depth (cam_0/cam_1) for the last normal analysis, or for all stages of the illumination / speckle sweep; `analysis_results/depth_results.tsv`, raw maps `depth_tv/depth_ref/...` in each experiment folder, map panel in the gallery |
| **3D-Fluss: Neu / Licht / Speckle** | three-dimensional displacement from both cameras for the same sets of stages; `analysis_results/sceneflow_results.tsv`, raw maps `sceneflow_*` |

### Tab *Realbild* (real image)

| Control | Function |
|---|---|
| file name | photograph of the real Nakajima test (in `Assets/cam00/`) |
| Kamera-FOV | field of view of the comparison camera in degrees (default 6) |
| **Neu / Laden** | renders the scene in the Nakajima look with cam_0 and registers it to the photograph; shows the last comparison |
| **Neu (andere Kamera)** | the same with cam_1 |

## 3. Scene panels (original interface)

These panels come from the first version of the lab and are still functional:

| Panel | Purpose |
|---|---|
| buttons at the top left | open the analysis panel, the display switches, and the experiment design |
| *save / analyze / load* | save the current configuration, analyse, load a saved configuration |
| display switches (bottom left) | component (u/v), strain mode and derivative direction, time step, experiment, matching steps, flat display |
| *Paths* | two image paths for analysing a user-supplied image pair, with a preview (previous/next) |
| image-series panel | input and output folders for evaluating a user-supplied image series |
| experiment design | camera, light, and sample configuration of the virtual experiment |

At start-up the lab moves these panels to the left if they would overlap the control panel.

## 4. Typical workflows

**Accuracy of a single image pair**
1. *Analyse*: set *Frames* (e.g. `1, 27`), keep *Analyse-k* = 1, choose the resolution.
2. Start panel: `with_exp` + `with_tv`, **Start**.
3. *Analyse*: **Genauigkeit** → gallery with maps and report.

**Illumination / noise / speckle study**
1. *Sweeps*: edit the list, press **Neu**. Each stage is rendered and analysed; the overall progress is shown in the
   progress bar.
2. **Laden** shows the table and the plot of the last run at any time.
3. For depth and 3D displacement of the same stages: *Stereo* → **Höhe: Licht** / **3D-Fluss: Licht** (or *Speckle*).

**Numerical parameter study**: *Analyse* → **Parameter: Neu** (a second click stops after the current run);
**Parameter: Laden** shows the plots.

**Comparison with DIC software**: see [dic_comparison.md](dic_comparison.md).

## 5. Log and progress

Detailed messages (parameters, self-tests, mapping of array to screen axes, runtimes) are written to the Unity console
and to `Logs/Editor.log`. Long runs can be stopped by leaving Play mode; results of completed stages are kept.
