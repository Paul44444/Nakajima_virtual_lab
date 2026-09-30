# Reference results

Result tables written by the lab (`Assets/analysis_results/`) at the state of the manuscript revision. They allow the
figures to be re-plotted without repeating the Unity studies (see
[../docs/reproducing_the_paper.md](../docs/reproducing_the_paper.md)); column definitions are given in
[../docs/data_formats.md](../docs/data_formats.md#result-tables-assetsanalysis_resultstsv).

| File | Study |
|---|---|
| `lighting_sweep.tsv` | displacement and strain error vs. illumination factor |
| `noise_sweep.tsv` | displacement and strain error vs. photon shot noise |
| `speckle_sweep.tsv` | displacement and strain error vs. speckle size (materials and procedural patterns) |
| `depth_results.tsv` | stereo depth error for the illumination and speckle stages |
| `sceneflow_results.tsv` | error of the three-dimensional displacement reconstructed from both cameras |
| `param_study_latest.tsv` | sensitivity to the numerical solver parameters |
| `texture_comparison.tsv` | speckle statistics of the photographed and rendered textures |
| `dic_comparison_28_29_prelim.tsv` | comparison with DICe, pydic, µDIC at the large deformation-gradient region (TV with λ = 0.1) |
| `dic_comparison_28_29_lambda005.tsv` | the same with the reference value λ = 0.05 |

Each line records the render resolution, the regulariser, and the strain smoothing of the stage
(`render_res`, `regularization`, `strain_sigma`), so results obtained with different settings can be distinguished.
