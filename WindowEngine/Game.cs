using System;
using System.IO;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Graphics.OpenGL4;
using StbImageSharp;

namespace WindowEngine
{
    // Minimal shader helper kept in this file
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
        private int _texture;

        // State
        private float _time = 0f;
        private bool _wireframe = false;

        // -------- Cube geometry --------
        // Position (x,y,z) + TexCoord (u,v)
        private static readonly float[] s_CubeVertices =
        {
            // Front face (z = +0.5)
            -0.5f, -0.5f, 0.5f,  0f, 0f, // 0
             0.5f, -0.5f, 0.5f,  1f, 0f, // 1
             0.5f,  0.5f, 0.5f,  1f, 1f, // 2
            -0.5f,  0.5f, 0.5f,  0f, 1f, // 3

            // Back face (z = -0.5)
            -0.5f, -0.5f, -0.5f,  1f, 0f, // 4
             0.5f, -0.5f, -0.5f,  0f, 0f, // 5
             0.5f,  0.5f, -0.5f,  0f, 1f, // 6
            -0.5f,  0.5f, -0.5f,  1f, 1f  // 7
        };

        // 12 triangles (2 per face) -> 36 indices
        private static readonly uint[] s_CubeIndices =
        {
            // Front
            0, 1, 2,
            2, 3, 0,

            // Right
            1, 5, 6,
            6, 2, 1,

            // Back
            5, 4, 7,
            7, 6, 5,

            // Left
            4, 0, 3,
            3, 7, 4,

            // Top
            3, 2, 6,
            6, 7, 3,

            // Bottom
            4, 5, 1,
            1, 0, 4
        };

        public Game()
            : base(GameWindowSettings.Default, new NativeWindowSettings
            {
                Size = new Vector2i(1280, 768),
                Title = "WindowEngine — A4 Texture (OpenTK)"
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

            // VBO
            _vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, s_CubeVertices.Length * sizeof(float), s_CubeVertices, BufferUsageHint.StaticDraw);

            // EBO
            _ebo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, s_CubeIndices.Length * sizeof(uint), s_CubeIndices, BufferUsageHint.StaticDraw);

            // VAO
            _vao = GL.GenVertexArray();
            GL.BindVertexArray(_vao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);

            // layout(location=0) => vec3 position
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            // layout(location=1) => vec2 texcoord
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);

            GL.BindVertexArray(0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

            // Shaders
            const string vs = @"#version 330 core
                layout(location=0) in vec3 aPos;
                layout(location=1) in vec2 aTex;
                uniform mat4 uMVP;
                out vec2 TexCoord;
                void main() {
                    gl_Position = uMVP * vec4(aPos, 1.0);
                    TexCoord = aTex;
                }";

            const string fs = @"#version 330 core
                out vec4 FragColor;
                in vec2 TexCoord;
                uniform sampler2D uTexture;
                void main() {
                    FragColor = texture(uTexture, TexCoord);
                }";

            _shader = new Shader(vs, fs);

            _texture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, _texture);

            StbImage.stbi_set_flip_vertically_on_load(1);
            using (var stream = File.OpenRead("crate.png"))
            {
                var img = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
                GL.TexImage2D(TextureTarget.Texture2D,
                    0,
                    PixelInternalFormat.Rgba,
                    img.Width,
                    img.Height,
                    0,
                    PixelFormat.Rgba,
                    PixelType.UnsignedByte,
                    img.Data);
            }

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

            int texLoc = GL.GetUniformLocation(_shader.Handle, "uTexture");
            GL.Uniform1(texLoc, 0); // bind sampler to texture unit 0

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
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);

            // Clear both color and depth buffers
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            _shader.Use();

            // --- MVP setup ---
            // Rotate around both Y and X a bit so depth is obvious
            var model = Matrix4.CreateScale(1.9f) * Matrix4.CreateRotationY(_time * 0.9f) * Matrix4.CreateRotationX(_time * 0.6f);
            var view = Matrix4.LookAt(new Vector3(1.6f, 1.2f, 3.0f), Vector3.Zero, Vector3.UnitY);
            var proj = Matrix4.CreatePerspectiveFieldOfView(
                MathHelper.DegreesToRadians(60f),
                Size.X / (float)Size.Y,
                0.1f, 100f
            );

            var mvp = model * view * proj;
            _shader.SetMatrix4("uMVP", mvp);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, _texture);

            GL.BindVertexArray(_vao);
            GL.DrawElements(PrimitiveType.Triangles, s_CubeIndices.Length, DrawElementsType.UnsignedInt, 0);
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

            GL.DeleteTexture(_texture);

            _shader?.Dispose();
            GL.UseProgram(0);

            // Disable depth test (not strictly necessary on shutdown)
            GL.Disable(EnableCap.DepthTest);
        }
    }
}