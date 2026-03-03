# Overview

This repository contains the code for building a virtual lab to simulate imaging in the Nakajima and MUC-Test. 
The framework is written in C# for the Unity Engine. To use it, download your scripts into the UnityEngine under 
Assets. 

![Screenshot](screenshot.CW6Imm)

There are following elements to set up the lab: 
- Importing Meshes of metal samples
- Applying speckle patterns
- set lighting positions, intensities
- set camera positions, rotations, field of view

Following functionalities are provided:
- Compute optical flow with TV regularization
- visualize rendered images, computed optical flow, strains
- construct groundd truth to compare with flow and strainss
- compute and visualize error maps
