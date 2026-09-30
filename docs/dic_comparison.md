# Comparison with DIC software

`scripts/dic_comparison.py` runs three open-source DIC codes on one rendered image pair and evaluates them against the
same reference field as the TV estimator; `scripts/plot_dic_comparison.py` draws the comparison figure. The manuscript
uses the large deformation-gradient region between mesh frames 28 and 29.

| Code | Type | Source |
|---|---|---|
| DICe (Digital Image Correlation Engine, Sandia) | subset-based local DIC | <https://github.com/dicengine/dice> (Windows installer, v3.0-beta.8) |
| pydic (D. André) | local DIC (correlation windows tracked with OpenCV's pyramidal Lucas–Kanade) | <https://gitlab.com/damien.andre/pydic> (`pydic.py`, GPL-3.0, not redistributed here) |
| µDIC (Olufsen et al., SoftwareX 2020) | finite-element (global) DIC with Q4 elements | `pip install muDIC` (0.2.1) |

## Setup

```
py -3.11 -m venv tools/dic_venv
tools\dic_venv\Scripts\python -m pip install muDIC opencv-python
```

Download `pydic.py` into `tools/downloads/` (or set the environment variable `DIC_TOOLS` to a folder that contains
`downloads/pydic.py`). Install DICe with its installer; the script finds `dice.exe` in the default installation
folders (`C:\Program Files (x86)\Digital Image Correlation Engine\`).

## Reference data from the lab

1. *Analyse* tab: *Frames* = `28, 29`, *Analyse-k* = 1, resolution 512.
2. Start panel: `with_exp` + `with_tv`, **Start**; then **Genauigkeit**. This writes the TV result and the reference
   (`accuracy_raw_value[_ref]_[u|v]_r512.f32`) to `Assetsexp_normal/time_flow_v/nice_pics/` — note that this
   overwrites the maps of the previous accuracy run.

## Run

```
tools\dic_venv\Scripts\python scripts/dic_comparison.py Assetsexp_normal 512 28 29 Assetsexp_normal/time_flow_v/nice_pics <out>
tools\dic_venv\Scripts\python scripts/plot_dic_comparison.py <out> Assetsexp_normal/cam_0/uv/im_28_r512.png <out>/dic_comparison.png "TV (this work)"
```

Options: `--skip dice,mudic,pydic` omits codes; `--ncorr <u.csv> <v.csv>` adds externally computed NCorr fields
(pixel grid of the image, same orientation as the reference). The DICe initialisation can be changed with the
environment variable `DICE_INIT` (default `USE_FEATURE_MATCHING`).

## Settings (identical for all codes, no case-specific tuning)

- common evaluation region: largest axis-parallel rectangle inside the sample around the image centre;
- DICe and pydic: subset 31 px, step 4 px; µDIC: Q4 elements of 32 px (same nominal spatial resolution);
- DICe initialised by feature matching (with neighbour-based initialisation, wrong initial guesses propagated across
  the discontinuity and about 12 % of the subsets diverged);
- measured displacements are interpolated linearly onto the pixel grid; all errors are evaluated on the pixels for
  which every method provides a value;
- **edge band**: pixels within 5 px of the jump of the reference (|∇u_ref| > 1 px/px).

The axis convention of the codes (x = column to the right, y = row downwards) was verified with a known synthetic
shift; the mapping to the reference components is determined automatically and printed.

## Output

`<out>/dic_comparison.tsv` (errors in the evaluation region and in the edge band, runtime, number of measurement points)
and `<out>/dic_comparison_maps.npz` (all fields on the pixel grid) for the plot script.

A useful plausibility check is to warp the second image back with each displacement field and compare the gray-value
residual in the edge band; in the manuscript the reference field yields the smallest residual, which shows that the
edge offset common to all codes is an error of the estimators and not of the reference.
