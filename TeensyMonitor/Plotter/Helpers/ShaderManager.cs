using OpenTK.Graphics.OpenGL4;

namespace TeensyMonitor.Plotter.Helpers
{
    internal static class ShaderManager
    {
        internal struct ShaderProgram
        {
            public string Name;
            public int    ProgramId;

        }


        private static ThreadLocal<Dictionary<string, ShaderProgram>?> _allShaderPrograms = new(() => []);

        public static int Get(string name)
        {
            var _shaderPrograms = _allShaderPrograms.Value!;
            if (_shaderPrograms.Count == 0) Init();

            if (_shaderPrograms.TryGetValue(name, out var program))
                return program.ProgramId;

            throw new KeyNotFoundException($"Shader program '{name}' not found.");
        }

        private static void Init()
        {
            var files = Directory.GetFiles(@"Resources\Shaders", "*.*", SearchOption.AllDirectories);
            Array.Sort(files, StringComparer.OrdinalIgnoreCase);

            var _shaderPrograms = _allShaderPrograms.Value!;
            for (int i = 0; i < files.Length; i+=2)  // process in pairs, .frag then .vert
            {   if (i+1 >= files.Length) break;

                ref var fragFile = ref files[i    ];
                ref var vertFile = ref files[i + 1];

                if (!fragFile.EndsWith(".frag", StringComparison.OrdinalIgnoreCase)) continue;
                if (!vertFile.EndsWith(".vert", StringComparison.OrdinalIgnoreCase)) continue;

                var vertexSource = File.ReadAllText(vertFile);
                var fragmentSource = File.ReadAllText(fragFile);

                var program = new ShaderProgram
                {
                    Name = Path.GetFileNameWithoutExtension(fragFile),
                    ProgramId = CompileShaders(vertexSource, fragmentSource)
                };

                _shaderPrograms!.Add(program.Name, program);
            }
        }

        public static void Clear()
        {
            var _shaderPrograms = _allShaderPrograms.Value!;
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
