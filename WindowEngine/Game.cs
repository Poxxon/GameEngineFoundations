using System;
using System.IO;
using System.Collections.Generic;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Graphics.OpenGL4;

namespace WindowEngine
{
    // Minimal shader helper kept in this file
    internal sealed class Shader : IDisposable
    {
        public int Handle { get; private set; }

        public Shader(string vertexPath, string fragmentPath)
        {
            string vertexSrc = File.ReadAllText(vertexPath);
            string fragmentSrc = File.ReadAllText(fragmentPath);

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
                Console.WriteLine($"[Program Link Error] {GL.GetProgramInfoLog(Handle)}");

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

        public void SetVector3(string name, Vector3 v)
        {
            int loc = GL.GetUniformLocation(Handle, name);
            if (loc != -1) GL.Uniform3(loc, ref v);
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
                Console.WriteLine($"[{type} Shader Error] {GL.GetShaderInfoLog(shader)}");
        }
    }

    public class Game : GameWindow
    {
        // GPU resources
        private int _vao, _vbo, _ebo;
        private Shader _shader;
        private int _indexCount;

        // State
        private float _time = 0f;
        private bool _wireframe = false;

        // Interaction state
        private Vector2 _lastMouse;
        private bool _dragging = false;
        private float _modelYaw = 0f;
        private float _modelPitch = 0f;

        // Light state (keyboard-controlled)
        private Vector3 _lightPos = new Vector3(2f, 2f, 2f);
        private Vector3 _lightColor = new Vector3(1f, 1f, 1f);
        private Vector3 _objectColor = new Vector3(1.0f, 0.5f, 0.31f);

        private static float[] GenerateSphere(float radius, int sectors, int stacks)
        {
            List<float> data = new();
            for (int i = 0; i <= stacks; ++i)
            {
                float stackAngle = MathF.PI / 2 - i * MathF.PI / stacks;
                float xy = radius * MathF.Cos(stackAngle);
                float z = radius * MathF.Sin(stackAngle);

                for (int j = 0; j <= sectors; ++j)
                {
                    float sectorAngle = j * 2 * MathF.PI / sectors;
                    float x = xy * MathF.Cos(sectorAngle);
                    float y = xy * MathF.Sin(sectorAngle);

                    data.Add(x);
                    data.Add(y);
                    data.Add(z);

                    Vector3 n = new Vector3(x, y, z).Normalized();
                    data.Add(n.X);
                    data.Add(n.Y);
                    data.Add(n.Z);
                }
            }
            return data.ToArray();
        }

        private static uint[] GenerateSphereIndices(int sectors, int stacks)
        {
            List<uint> indices = new();
            for (int i = 0; i < stacks; ++i)
            {
                int k1 = i * (sectors + 1);
                int k2 = k1 + sectors + 1;

                for (int j = 0; j < sectors; ++j, ++k1, ++k2)
                {
                    if (i != 0)
                    {
                        indices.Add((uint)k1);
                        indices.Add((uint)k2);
                        indices.Add((uint)(k1 + 1));
                    }
                    if (i != (stacks - 1))
                    {
                        indices.Add((uint)(k1 + 1));
                        indices.Add((uint)k2);
                        indices.Add((uint)(k2 + 1));
                    }
                }
            }
            return indices.ToArray();
        }

        public Game()
            : base(GameWindowSettings.Default, new NativeWindowSettings
            {
                Size = new Vector2i(1280, 768),
                Title = "WindowEngine — A5 Phong Lighting (OpenTK)"
            })
        {
            CenterWindow(new Vector2i(1280, 768));
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            GL.Viewport(0, 0, e.Width, e.Height);
        }

        // Setup: buffers, vertex format, shaders, depth test
        protected override void OnLoad()
        {
            base.OnLoad();

            GL.ClearColor(0.15f, 0.18f, 0.22f, 1f);

            // Enable depth testing for correct 3D rendering
            GL.Enable(EnableCap.DepthTest);
            GL.DepthFunc(DepthFunction.Lequal);

            float[] sphereVertices = GenerateSphere(1.0f, 36, 18);
            uint[] sphereIndices = GenerateSphereIndices(36, 18);
            _indexCount = sphereIndices.Length;

            // VBO
            _vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, sphereVertices.Length * sizeof(float), sphereVertices, BufferUsageHint.StaticDraw);

            // EBO
            _ebo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, sphereIndices.Length * sizeof(uint), sphereIndices, BufferUsageHint.StaticDraw);

            // VAO
            _vao = GL.GenVertexArray();
            GL.BindVertexArray(_vao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);

            // layout(location=0) => vec3 position
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            // layout(location=1) => vec3 normal
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);

            GL.BindVertexArray(0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

            // Load shaders from files
            _shader = new Shader("phong.vert", "phong.frag");

            // Start filled
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);

            _time += (float)args.Time;

            if (KeyboardState.IsKeyDown(Keys.Escape))
                Close();

            if (KeyboardState.IsKeyPressed(Keys.F1))
            {
                _wireframe = !_wireframe;
                GL.PolygonMode(MaterialFace.FrontAndBack, _wireframe ? PolygonMode.Line : PolygonMode.Fill);
            }

            // --- Mouse drag to rotate the object ---
            var mouse = MouseState;
            if (mouse.IsButtonDown(MouseButton.Left))
            {
                if (!_dragging)
                {
                    _dragging = true;
                    _lastMouse = new Vector2(mouse.X, mouse.Y);
                }
                else
                {
                    var curr = new Vector2(mouse.X, mouse.Y);
                    var delta = curr - _lastMouse;
                    _lastMouse = curr;

                    float sens = 0.3f; // rotation sensitivity
                    _modelYaw   += delta.X * sens * MathF.PI / 180f;
                    _modelPitch += -delta.Y * sens * MathF.PI / 180f;
                    _modelPitch = MathHelper.Clamp(_modelPitch, -MathF.PI / 2 + 0.01f, MathF.PI / 2 - 0.01f);
                }
            }
            else
            {
                _dragging = false;
            }

            // --- Keyboard to move the point light ---
            float dt = (float)args.Time;
            float speed = 2.0f * (KeyboardState.IsKeyDown(Keys.LeftShift) ? 3.0f : 1.0f); // hold Shift to move faster

            if (KeyboardState.IsKeyDown(Keys.Left))
                _lightPos.X -= speed * dt;
            if (KeyboardState.IsKeyDown(Keys.Right))
                _lightPos.X += speed * dt;
            if (KeyboardState.IsKeyDown(Keys.Up))
                _lightPos.Z -= speed * dt; // move toward -Z
            if (KeyboardState.IsKeyDown(Keys.Down))
                _lightPos.Z += speed * dt; // move toward +Z

            if (KeyboardState.IsKeyDown(Keys.Q))
                _lightPos.Y -= speed * dt;
            if (KeyboardState.IsKeyDown(Keys.E))
                _lightPos.Y += speed * dt;

            // Optional light color quick toggles
            if (KeyboardState.IsKeyPressed(Keys.D1)) _lightColor = new Vector3(1f, 1f, 1f);       // white
            if (KeyboardState.IsKeyPressed(Keys.D2)) _lightColor = new Vector3(1.0f, 0.9f, 0.7f); // warm
            if (KeyboardState.IsKeyPressed(Keys.D3)) _lightColor = new Vector3(0.7f, 0.8f, 1.0f); // cool
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);

            // Clear both color and depth buffers
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            _shader.Use();

            // --- MVP setup ---
            var model = Matrix4.CreateRotationX(_modelPitch)
                    * Matrix4.CreateRotationY(_modelYaw)
                    * Matrix4.CreateScale(1.0f)
                    * Matrix4.CreateTranslation(0f, 0f, 0f);

            // Place camera directly in front of origin
            var cameraPos = new Vector3(0.0f, 0.0f, 5.0f);
            var view = Matrix4.LookAt(cameraPos, Vector3.Zero, Vector3.UnitY);

            // Adjust projection so vertical/horizontal alignment matches window
            float aspect = Size.X / (float)Size.Y;
            var proj = Matrix4.CreatePerspectiveFieldOfView(
                MathHelper.DegreesToRadians(45f),
                aspect,
                0.1f,
                100f
            );

            Vector3 lightPos = _lightPos;
            Vector3 lightColor = _lightColor;
            Vector3 objectColor = _objectColor;
            Vector3 viewPos = cameraPos;

            // Note: MVP order is projection * view * model
            var mvp = proj * view * model;
            _shader.SetMatrix4("uMVP", mvp);
            _shader.SetMatrix4("model", model);
            _shader.SetMatrix4("view", view);
            _shader.SetMatrix4("projection", proj);

            _shader.SetVector3("lightPos", lightPos);
            _shader.SetVector3("lightColor", lightColor);
            _shader.SetVector3("objectColor", objectColor);
            _shader.SetVector3("viewPos", viewPos);

            GL.BindVertexArray(_vao);
            GL.DrawElements(PrimitiveType.Triangles, _indexCount, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);

            SwapBuffers();
        }

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

            // Disable depth test (not strictly necessary on shutdown)
            GL.Disable(EnableCap.DepthTest);
        }
    }
}