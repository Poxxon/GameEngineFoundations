# WindowEngine — Assignment 3 (3D Cube)

### Library Used
**OpenTK** (Open Toolkit) for windowing, OpenGL context, and input.

### How I Rendered the Cube
- Defined **8 unique vertices** for a unit cube centered at the origin.
- Used an **EBO** (index buffer) with **36 indices** (12 triangles) to draw the 6 faces.
- Enabled **depth testing** so hidden faces are not visible.
- Built **MVP** matrices:
  - `model`: rotates over time (Y and X) for a clear 3D effect
  - `view`: `LookAt` camera
  - `projection`: perspective with a 60° FOV
- Vertex shader multiplies `uMVP * vec4(aPos, 1.0)`, fragment shader outputs a solid color.

### How to Run
- Requires **.NET 8 SDK**
- From the project folder:
  ```bash
  dotnet run