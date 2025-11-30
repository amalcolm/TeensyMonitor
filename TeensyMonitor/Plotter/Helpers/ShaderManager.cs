using OpenTK.Graphics.OpenGL4;

namespace TeensyMonitor.Plotter.Helpers
{
    internal static class ShaderManager
    {
        internal struct ShaderProgram
        {
            public string Name;

            public int ProgramId;

            public string FragmentShaderSource;
            public string VertexShaderSource;
        }


        [ThreadStatic]
        private static Dictionary<string, ShaderProgram>? _shaderPrograms;

        public static int Get(string name)
        {
            _shaderPrograms ??= [];
            if (_shaderPrograms.Count == 0) Init();

            if (_shaderPrograms.TryGetValue(name, out var program))
                return program.ProgramId;

            throw new KeyNotFoundException($"Shader program '{name}' not found.");
        }

        private static void Init()
        {
            var files = Directory.GetFiles(@"Resources\Shaders", "*.*", SearchOption.AllDirectories);
            Array.Sort(files, StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < files.Length; i+=2)  // process in pairs, .frag then .vert
            {   if (i+1 >= files.Length) break;

                ref var fragFile = ref files[i    ];
                ref var vertFile = ref files[i + 1];

                if (!fragFile.EndsWith(".frag", StringComparison.OrdinalIgnoreCase)) continue;
                if (!vertFile.EndsWith(".vert", StringComparison.OrdinalIgnoreCase)) continue;

                var program = new ShaderProgram
                {
                    Name = Path.GetFileNameWithoutExtension(fragFile),
                    FragmentShaderSource = File.ReadAllText(fragFile),
                    VertexShaderSource = File.ReadAllText(vertFile)
                };
                program.ProgramId = CompileShaders(program.VertexShaderSource, program.FragmentShaderSource);

                _shaderPrograms!.Add(program.Name, program);
            }
        }

        public static void Clear()
        {
            if (_shaderPrograms == null) return;

            foreach (var program in _shaderPrograms.Values)
                GL.DeleteProgram(program.ProgramId);

            _shaderPrograms.Clear();
        }


        private static int CompileShaders(string vertexSource, string fragmentSource)
        {
            int vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShader, vertexSource);
            GL.CompileShader(vertexShader);

            int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShader, fragmentSource);
            GL.CompileShader(fragmentShader);

            int program = GL.CreateProgram();
            GL.AttachShader(program, vertexShader);
            GL.AttachShader(program, fragmentShader);
            GL.LinkProgram(program);

            GL.DetachShader(program, vertexShader);
            GL.DetachShader(program, fragmentShader);
            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);

            return program;
        }

        public static void Dispose() => Clear();

    }
}
