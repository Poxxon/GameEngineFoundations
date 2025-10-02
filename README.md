# WindowEngine — Assignment 4 (Textured Cube)

### Library Used
**OpenTK** (Open Toolkit) for windowing, OpenGL context, and input.

### Description
This assignment renders a textured cube using OpenGL and OpenTK. The cube is mapped with a crate texture to demonstrate texture mapping techniques in 3D graphics.

### Textures
A crate texture (`crate.png`) is loaded using StbImageSharp and applied to all faces of the cube, enhancing the visual realism.

### How I Rendered the Cube
- Defined **8 unique vertices** for a unit cube centered at the origin.
- Defined texture coordinates for each vertex.
- Used an **EBO** (index buffer) with **36 indices** (12 triangles) to draw the 6 faces.
- Enabled **depth testing** so hidden faces are not visible.
- Built **MVP** matrices:
  - `model`: rotates over time (Y and X) for a clear 3D effect
  - `view`: `LookAt` camera
  - `projection`: perspective with a 60° FOV
- Vertex shader multiplies `uMVP * vec4(aPos, 1.0)` and passes UV coordinates to the fragment shader.
- Fragment shader samples from the bound texture and outputs the textured color.

### How to Run
- Requires **.NET 8 SDK**
- From the project folder:
  ```bash
  dotnet run```