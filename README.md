# MagniSnap

## Project Description
MagniSnap is a C# Windows Forms application designed as a startup project for implementing image processing algorithms, specifically focusing on the **Intelligent Scissors** (also known as Livewire) technique for image segmentation.

This project provides a basic framework for loading, displaying, and processing images, along with utility functions for pixel manipulation and energy calculation.

## Features
- **Image Loading & Display**: Open images from the file system and display them in the application window.
- **Image Information**: View image width, height, and current mouse coordinates.
- **Image Processing Toolkit**:
  - **Gaussian Smoothing**: Apply Gaussian filters to smooth images and reduce noise.
  - **Edge Energy Calculation**: Compute gradient magnitudes and edge energies, essential for the Intelligent Scissors algorithm.
- **Interactive Tools**:
  - **Livewire Tool**: A placeholder for the Intelligent Scissors tool, allowing for interactive segmentation (to be implemented).

## Project Structure
- **MagniSnap/**: Contains the source code for the application.
  - `mainForm.cs`: The main entry point for the UI, handling user interactions and events.
  - `ImageToolkit.cs`: A library of static functions for image processing, including:
    - `OpenImage`: Loads an image into a 2D RGB pixel array.
    - `ViewImage`: Renders the pixel array to a PictureBox.
    - `GaussianFilter1D`: Implements Gaussian smoothing.
    - `CalculatePixelEnergies`: Computes edge energy for graph-based segmentation.

## Getting Started
### Prerequisites
- Visual Studio (2019 or later recommended)
- .NET Framework

### Installation & Usage
1. Clone the repository or download the source code.
2. Open the solution file `MagniSnap.sln` located in `Project Description & Template/MagniSnap Startup Code/[TEMPLATE] MagniSnap/`.
3. Build the solution to restore dependencies.
4. Run the application.
5. Use the **File > Open** menu to load an image.
6. Select the **Livewire** tool to test the segmentation features (once implemented).

## Notes
This codebase contains template regions marked with `#region Do Change Remove Template Code`. These are intended to be modified or replaced as part of the implementation of the full Intelligent Scissors algorithm.
