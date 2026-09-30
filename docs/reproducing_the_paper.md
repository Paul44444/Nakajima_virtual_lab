# Reproducing the manuscript

All commands are run from the project folder. `<ms>` denotes a folder for the manuscript figures. Unless stated
otherwise, the studies use a render resolution of 512 px and frames 1 and 27. Before a final run, set the solver in
the *TV-Parameter* panel to the configuration stated in the manuscript and keep *Analyse-k* = 1.

The result tables of the manuscript are included in `results/`, so the plotting steps can be run without repeating
the (long) Unity studies; the raw maps needed for map figures are part of the experiment folders and are not stored in
git.

| Manuscript figure | Unity steps | Plot command |
|---|---|---|
| Speckle texture comparison | render the standard analysis and the procedural speckle stages | `python scripts/texture_comparison.py . <ms>/texture_comparison.png Assets/analysis_results/texture_comparison.tsv` |
| Image-plane displacement and error maps | *Start* + *Genauigkeit* (frames 1, 27) | `python scripts/make_paper_flow_figure.py Assetsexp_normal/time_flow_v/nice_pics 1024 <ms>/plot_exp_normal_v2.png` |
| Displacement error vs. illumination / speckle size | *Sweeps → Licht: Neu*, *Sweeps → Speckle: Neu* | `python scripts/make_paper_lighting_figures.py Assets/analysis_results/lighting_sweep.tsv <ms>`, then `python scripts/make_paper_speckle_figures.py Assets/analysis_results/speckle_sweep.tsv Assets/analysis_results/depth_results.tsv . <ms>` (always last) |
| Strain maps and strain error | as above; maps from *Genauigkeit* | included in `make_paper_lighting_figures.py` (set `STRAIN_RAW_DIR`, `STRAIN_RAW_RES` for the maps) |
| Photon shot noise | *Sweeps → Rauschen: Neu* | `python scripts/make_paper_noise_figure.py Assets/analysis_results/noise_sweep.tsv . <ms>` |
| Stereo depth and 3D displacement | *Stereo → Höhe: Licht*, *Höhe: Speckle*, *3D-Fluss: Neu* | `python scripts/make_paper_depth_figure.py Assets/analysis_results/depth_results.tsv Assetslighting_1 512 <ms>` → speckle panels (above) → `python scripts/plot_depth_3d.py --sceneflow Assetslighting_1 512 Assetslighting_1/cam_0/uv/im_1_r512.png <ms>/depth_3d_sceneflow.png` → `python scripts/compose_heights_3d.py <ms>/heights_v2.png <ms>/depth_3d_sceneflow.png <ms>/heights_v3.png` |
| Comparison with DIC software | frames `28, 29`, *Start* + *Genauigkeit* | see [dic_comparison.md](dic_comparison.md) |
| Parameter sensitivity (appendix) | *Analyse → Parameter: Neu* | `python scripts/plot_param_study.py --paper Assets/analysis_results/param_study_latest.tsv <ms>/param_study_overview_en.png` |

## Order of the runs

1. Standard analysis (*Start*, frames 1 and 27) and *Genauigkeit*.
2. Illumination, noise, and speckle sweeps (*Sweeps* tab). Each sweep renders every stage, computes the flow,
   evaluates it against the reference, and appends the stage to its table.
3. Depth and 3D displacement for the stages (*Stereo* tab).
4. Figures in the order of the table above; the speckle script replaces the speckle halves of the combined figures and
   must therefore run after the illumination and depth scripts.

## Numbers in the text

The values quoted in the manuscript text are taken directly from the tables:

| Quantity | Table and columns |
|---|---|
| displacement error vs. illumination / noise / speckle size | `lighting_sweep.tsv`, `noise_sweep.tsv`, `speckle_sweep.tsv`: `mean_v_error` (relative), `u_mae`, `v_mae` |
| strain error | same tables: `exx_rel_mae`, `eyy_rel_mae` |
| depth error | `depth_results.tsv`: `depth_mae_mm`, `depth_rel_relief`, `disparity_epe_px` |
| 3D displacement error | `sceneflow_results.tsv`: `epe3d_mm`, `rel_epe3d`, `mae_{x,y,z}_mm` |
| DIC comparison | `dic_comparison.tsv`: `epe_px`, `epe_edge_px`, `runtime_s` |
| parameter sensitivity | `param_study_latest.tsv` |

## Runtime

In the parameter study of the manuscript, one flow computation at 1024 × 1024 px with 7 pyramid levels, 8 warps,
and at most 500 iterations took about 28.6 s on the GPU (column `time_s` of `param_study_latest.tsv`). Sweeps
additionally render every stage; their duration scales with the number of stages and the resolution.
