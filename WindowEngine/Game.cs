using System;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Graphics.OpenGL4;

namespace WindowEngine
{
    // Minimal shader helper kept in this file (no extra files needed)
    internal sealed class Shader : IDisposable
    {
        public int Handle { get; private set; }

        public Shader(string vertexSrc, string fragmentSrc)
        {
            int vs = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vs, vertexSrc);
            GL.CompileShader(vs);
            CheckCompile(vs, "Vertex");

            int fs = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fs, fragmentSrc);
            GL.CompileShader(fs);
            CheckCompile(fs, "Fragment");

            Handle = GL.CreateProgram();
            GL.AttachShader(Handle, vs);
            GL.AttachShader(Handle, fs);
            GL.LinkProgram(Handle);

            GL.GetProgram(Handle, GetProgramParameterName.LinkStatus, out int ok);
            if (ok == 0)
            {
                Console.WriteLine($"[Program Link Error] {GL.GetProgramInfoLog(Handle)}");
            }

            // The shaders are linked into the program; delete shader objects
            GL.DetachShader(Handle, vs);
            GL.DetachShader(Handle, fs);
            GL.DeleteShader(vs);
            GL.DeleteShader(fs);
        }

        public void Use() => GL.UseProgram(Handle);

        public void SetMatrix4(string name, Matrix4 m)
        {
            int loc = GL.GetUniformLocation(Handle, name);
            if (loc != -1) GL.UniformMatrix4(loc, false, ref m);
        }

        public void Dispose()
        {
            if (Handle != 0)
            {
                GL.DeleteProgram(Handle);
                Handle = 0;
            }
        }

        private static void CheckCompile(int shader, string type)
        {
            GL.GetShader(shader, ShaderParameter.CompileStatus, out int ok);
            if (ok == 0)
            {
                Console.WriteLine($"[{type} Shader Error] {GL.GetShaderInfoLog(shader)}");
            }
        }
    }

    public class Game : GameWindow
    {
        // GPU resources
        private int _vao, _vbo, _ebo;
        private Shader _shader;

        // State
        private float _time = 0f;
        private bool _wireframe = false;

        // Quad data: 4 vertices, 6 indices (two triangles)
        // Positions only (x, y, z) to keep it simple
        private readonly float[] _vertices =
        {
            -0.5f, -0.5f, 0.0f, // 0 bottom-left
             0.5f, -0.5f, 0.0f, // 1 bottom-right
             0.5f,  0.5f, 0.0f, // 2 top-right
            -0.5f,  0.5f, 0.0f  // 3 top-left
        };

        private readonly uint[] _indices =
        {
            0, 1, 2,
            2, 3, 0
        };

        public Game()
            : base(GameWindowSettings.Default, new NativeWindowSettings
            {
                Size = new Vector2i(1280, 768),
                Title = "WindowEngine — A2 Quad (OpenTK)"
            })
        {
            CenterWindow(new Vector2i(1280, 768));
        }

        // Resize: keep viewport in sync
        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            GL.Viewport(0, 0, e.Width, e.Height);
        }

        // Load once: buffers, vertex format, shaders
        protected override void OnLoad()
        {
            base.OnLoad();

            GL.ClearColor(0.2f, 0.3f, 0.35f, 1f);

            // Generate buffers
            _vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, _vertices.Length * sizeof(float), _vertices, BufferUsageHint.StaticDraw);

            _ebo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, _indices.Length * sizeof(uint), _indices, BufferUsageHint.StaticDraw);

            // Describe vertex format via VAO
            _vao = GL.GenVertexArray();
            GL.BindVertexArray(_vao);

            // Bind buffers while configuring VAO
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);

            // layout(location=0) => vec3 position
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            // Unbind VAO; state is recorded in VAO
            GL.BindVertexArray(0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            // Do NOT unbind EBO here if you keep VAO unbound, it's okay because EBO binding is stored in VAO when VAO was bound.
            // (We already bound EBO while VAO was bound.)

            // Minimal shaders with uMVP uniform and solid color
            const string vs = @"#version 330 core
                layout(location=0) in vec3 aPos;
                uniform mat4 uMVP;
                void main() {
                    gl_Position = uMVP * vec4(aPos, 1.0);
                }";

            const string fs = @"#version 330 core
                out vec4 FragColor;
                void main() {
                    FragColor = vec4(0.85, 0.35, 0.6, 1.0);
                }";

            _shader = new Shader(vs, fs);

            // Start filled
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
        }

        // Update: input & time
        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);

            _time += (float)args.Time;

            // ESC to quit
            if (KeyboardState.IsKeyDown(Keys.Escape))
                Close();

            // F1 toggles wireframe (on key press, not hold)
            if (KeyboardState.IsKeyPressed(Keys.F1))
            {
                _wireframe = !_wireframe;
                GL.PolygonMode(MaterialFace.FrontAndBack, _wireframe ? PolygonMode.Line : PolygonMode.Fill);
            }
        }

        // Render
        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);

            GL.Clear(ClearBufferMask.ColorBufferBit);

            _shader.Use();

            // Build MVP (simple rotation around Y)
            var model = Matrix4.CreateRotationY(_time);
            var view = Matrix4.LookAt(new Vector3(0, 0, 2), Vector3.Zero, Vector3.UnitY);
            var proj = Matrix4.CreatePerspectiveFieldOfView(
                MathHelper.DegreesToRadians(45f),
                Size.X / (float)Size.Y,
                0.1f, 100f);

            var mvp = model * view * proj;
            _shader.SetMatrix4("uMVP", mvp);

            GL.BindVertexArray(_vao);
            GL.DrawElements(PrimitiveType.Triangles, _indices.Length, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);

            SwapBuffers();
        }

        // Cleanup
        protected override void OnUnload()
        {
            base.OnUnload();

            GL.BindVertexArray(0);
            GL.DeleteVertexArray(_vao);

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.DeleteBuffer(_vbo);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
            GL.DeleteBuffer(_ebo);

            _shader?.Dispose();
            GL.UseProgram(0);
        }
    }
}