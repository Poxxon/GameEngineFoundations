# WindowEngine — Assignment 5 (Phong Lighting Report)

### Library Used
**OpenTK** (Open Toolkit) for windowing, OpenGL context, and input.

### Description
This project implements Phong lighting in OpenTK, replacing the previous textured cube. It demonstrates realistic lighting effects on 3D geometry using ambient, diffuse, and specular components.

### Overview
A 3D sphere is rendered with ambient, diffuse, and specular lighting, using vertex and fragment shaders written in GLSL. The lighting calculations provide a realistic shading effect that responds dynamically to light and camera positions.

### Controls
- Mouse rotation to rotate the sphere.
- Arrow keys and Q/E keys to move the light source.
- F1 key toggles wireframe mode.

### Report
The sphere is generated procedurally by computing vertex positions and normals, allowing for smooth shading across its surface. The vertex and fragment shaders implement the Phong lighting model, calculating ambient, diffuse, and specular components based on light and view directions. The camera is set up to view the sphere clearly, and the light source can be moved interactively by the user.

User interaction enables dynamic manipulation of the light position and viewing angle, providing immediate visual feedback on lighting changes. This project meets all the specified requirements and effectively demonstrates realistic lighting effects using OpenTK and GLSL shaders.

### How to Run
- Requires **.NET 8 SDK**
- From the project folder:
  ```bash
  dotnet run