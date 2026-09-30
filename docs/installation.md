# Installation

## 1. Unity

1. Install **Unity Hub** and, through it, the Unity Editor **6000.6.2f1** (the exact version is recorded in
   `ProjectSettings/ProjectVersion.txt`; newer 6000.x versions will usually upgrade the project automatically).
   The Windows build support module is sufficient.
2. Clone the repository:
   ```
   git clone https://github.com/Paul44444/Nakajima_virtual_lab.git
   ```
3. Download the data archive (see [README](../README.md#data)) and extract it **into the project folder**, so that
   the following folders exist:
   ```
   Assets/verts/verts_1.txt ... verts_<n>.txt
   Assets/Resources/Targets/
   Assets/Resources/blender_data/
   Assets/cam00/
   Assets/nakajima_full_angle/
   ```
4. In Unity Hub choose *Add project from disk* and select the cloned folder. The first import compiles the scripts
   and imports the HDRP package (several minutes).
5. Open `Assets/Scenes/SampleScene.unity`.
6. If Unity asks to import **TMP Essentials**, confirm (the essential TextMesh Pro resources are included, but Unity
   may still offer the import after an upgrade).

### Recommended editor setting

The long-running analyses are asynchronous. With *Edit > Project Settings > Editor > Enter Play Mode Settings*
set to *Reload Domain disabled*, a running analysis survives the end of Play mode; the analyses stop cleanly when the
cameras of the scene are destroyed. Scripts are recompiled when Play mode is left.

## 2. Paths

All paths are derived from the project folder at run time:

| Purpose | Location |
|---|---|
| deformed meshes | `Assets/verts/verts_<n>.txt` (fallback if the legacy folder `<base>/play_blender_pycahrm/write_mesh/` does not exist) |
| experiment folders (rendered images, flow maps) | next to `Assets/`, named `Assets<label>` (e.g. `Assetsexp_normal`, `Assetslighting_1`) |
| result tables, plots, raw maps of the studies | `Assets/analysis_results/` |
| optional base path | first line of `<project>/path_base.txt` (only needed for the legacy mesh folder) |

The prefix `Assets` in the experiment folder names is historical: the label of an experiment is appended to the path
of the `Assets` folder. The folders are created automatically.

## 3. Python (figures and analyses)

The scripts in `scripts/` require Python 3.11 or newer:

```
python -m pip install numpy matplotlib pillow
```

Run them from the project folder, e.g. `python scripts/plot_sweeps.py lighting Assets/analysis_results/lighting_sweep.tsv Assets/analysis_results`.
Several buttons in the lab call these scripts automatically after a study (e.g. the sweep plots); for this,
`python` must be on the `PATH`.

### Optional: DIC comparison environment

The comparison with DICe, pydic, and µDIC uses a separate environment because µDIC (2021) requires an older Python:

```
py -3.11 -m venv tools/dic_venv
tools\dic_venv\Scripts\python -m pip install muDIC opencv-python
```

pydic is a single file (`pydic.py`, GPL-3.0) that is not redistributed here; download it from
<https://gitlab.com/damien.andre/pydic> into `tools/downloads/`. DICe is installed with its Windows installer from
<https://github.com/dicengine/dice/releases>. Details: [dic_comparison.md](dic_comparison.md).

## 4. Troubleshooting

| Symptom | Remedy |
|---|---|
| Pink materials, missing laboratory models | the data archive is missing or extracted to the wrong folder |
| "Keine TV-Ergebnisse" when pressing *Genauigkeit* | run *Start* with `with_tv` first (or load a previous analysis) |
| Very slow flow computation | check in *TV-Parameter* that the path is *GPU*; the CPU path is a reference implementation |
| Sweep plots do not appear | Python is not on the `PATH`, or `numpy`/`matplotlib` are missing; the tables are still written |
| Black images in an analysis | check *Analyse-k* (exposure factor of the normal analysis) — it must be 1 for unmodified images |
| HDRP warnings about the render-pipeline asset | open *Project Settings > Graphics* and assign `Assets/Resources/paul_HDRP_asset.asset` |
