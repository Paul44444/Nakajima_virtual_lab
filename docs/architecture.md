# Code architecture

The lab is a single Unity scene (`Assets/Scenes/SampleScene.unity`) driven by one main component, `vis_3D`, attached to
the scene object `sphere`. User-interface scripts find this component (`GameObject.Find("sphere")`) and call its
public methods. Code comments are mostly German and carry a date prefix (`//DDMMYYYY`) that documents when and why a
change was made.

## Main components

| File | Role |
|---|---|
| `vis_3D.cs` | main controller (see below) |
| `TvL1Gpu.cs`, `TgvL1Gpu.cs` | GPU solvers for one pyramid level (upload, `RunWarp` per warp, download); used by `vis_3D.find_displ` |
| `Resources/TVL1.compute`, `Resources/TGVL1.compute` | compute kernels (gradients, warping, primal–dual updates) |
| `ExperimentImageGallery.cs` | control panel with tabs, tooltips, TV-parameter panel, image gallery, result reports; loads previous analyses |
| `HoverTip.cs` | reports pointer enter/exit for tooltips and hover previews |
| `ResolutionSlider.cs` | render-resolution slider (synchronised with the legacy resolution toggles) |
| `NakajimaRealImageComparison.cs` | renders the Nakajima look and registers it to a real photograph |
| `Cam_manager.cs` | interactive camera (zoom/rotation), ignores the mouse wheel over the UI panels |
| small scripts (`*_but.cs`, `*_panel.cs`, `Switch_*.cs`, `U_V.cs`, `Strain_D.cs`, ...) | handlers of the original scene UI (paths, series, display switches, experiment design) |
| `CamLightDiagnostics.cs` | read-only diagnostics for degenerate camera/light transforms |

## `vis_3D.cs` by topic

| Topic | Key methods |
|---|---|
| set-up, paths, cameras, lights | `Start`, `resolve_path_base`, `set_up_cam`, `set_up_exp_setup_lights`, `set_ambient_factor` |
| sample meshes | `blade_path_for_idx`, `load_blade_from_verts`, `load_mesh_from_verts`, `tris_from_coords` |
| speckle textures | `apply_speckles`, `load_procedural_speckle`, `describe_speckle_texture` |
| image acquisition | rendering coroutines of the standard analysis, `manage_read_im`, exposure and shot noise |
| optical flow | `compute_ims` → `find_displ` (pyramid, warping; GPU or CPU per level), `set_up_pars` (parameters, overrides from the TV panel) |
| ground truth (2D) | pixel-to-triangle mask with barycentric coordinates (`load_tris`), `load_distortion_ground_truth`, `find_truth_flow` |
| accuracy | `save_accuracy_analysis` (maps, strain, statistics, raw maps) |
| sweeps | `start_lighting_sweep`, `start_noise_sweep`, `start_speckle_sweep`, `compute_study_metrics`, `merge_sweep_table`, `sweep_setup_key`, `set_overall_progress` |
| exposure study | `run_exposure_study`, `show_exposure_study` |
| parameter study | `run_param_study`, `show_param_study` |
| stereo depth | `run_stereo`, `collect_stereo_jobs`, `stereo_depth_step`, `stereo_ground_truth` |
| 3D displacement | `run_scene_flow`, `scene_flow_step`, `scene_flow_ground_truth`, `tv_flow_arrays`, `flow_to_screen`, `score_mappings` |
| geometry helpers | `cam_matrix` (P·V in double), `invert4`, `pixel_ray`, `project_vp`, `triangulate_dlt` |
| I/O | `write_raw_map` (`.f32`), `write_to_txt`, `run_python` (calls the scripts in `scripts/`) |

The legacy stereo formula (`stereo_depth_step_legacy`, `mat_raw2dists`) and `vis_3D_old.cs` are kept for traceability
and are not used by the current workflows.

## Asynchronous execution

Long analyses are `async` methods that await background tasks (flow computation) and yield to the Unity main loop.
GPU work must run on the main thread; `vis_3D` dispatches it accordingly. Loops check whether the cameras still exist
and stop when Play mode ends.

## Python scripts (`scripts/`)

| Script | Purpose |
|---|---|
| `plot_sweeps.py` | curves of the sweeps (called automatically after each sweep) |
| `plot_depth_maps.py`, `plot_scene_flow.py` | map panels of depth and 3D displacement (called automatically) |
| `plot_param_study.py` | parameter-study plots (`--paper` for the manuscript version) |
| `plot_exposure_study.py`, `plot_maps.py`, `figure_accuracy.py`, `tv_error.py`, `strain_sweep.py` | further plots and checks |
| `make_paper_*.py`, `compose_heights_3d.py`, `plot_depth_3d.py` | manuscript figures |
| `texture_comparison.py`, `speckle_size.py` | speckle statistics |
| `make_procedural_speckles.py`, `make_speckle_textures.py` | texture generation |
| `stl2verts.py` | conversion of STL exports of a finite-element solver into the mesh format |
| `dic_comparison.py`, `plot_dic_comparison.py` | comparison with DICe, pydic, µDIC |

## Extending the lab

- **New study**: follow the pattern of `start_speckle_sweep` — build a list of `Params`, run the stages through the
  standard pipeline, evaluate with `compute_study_metrics`, and merge the result line with `merge_sweep_table`.
- **New flow estimator**: implement a method with the signature of `compute_ims` (two images → two displacement
  arrays) or evaluate external estimators offline as in `scripts/dic_comparison.py`, which reuses the raw reference maps.
- **Different forming simulation**: export the sample surface of every output step as STL (constant vertex order),
  convert it with `scripts/stl2verts.py`, and place the files in `Assets/verts/`.
