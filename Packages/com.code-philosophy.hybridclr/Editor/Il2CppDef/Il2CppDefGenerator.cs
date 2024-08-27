using HybridCLR.Editor.ABI;
using HybridCLR.Editor.Template;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace HybridCLR.Editor.Il2CppDef
{
    public class Il2CppDefGenerator
    {
        public class Options
        {
            public List<string> HotUpdateAssemblies { get; set; }

            public string OutputFile { get; set; }

            public string OutputFile2 { get; set; }

            public string UnityVersion { get; set; }
        }

        private readonly Options _options;
        public Il2CppDefGenerator(Options options)
        {
            _options = options;
        }


        private static readonly Regex s_unityVersionPat = new Regex(@"(\d+)\.(\d+)\.(\d+)");

        public void Generate()
        {
            GenerateIl2CppConfig();
            GeneratePlaceHolderAssemblies();
        }

        private void GenerateIl2CppConfig()
        {
            var frr = new FileRegionReplace(File.ReadAllText(_options.OutputFile));

            List<string> lines = new List<string>();

            lines.Add($"#define IL2CPP_ENABLE_PROFILER {(EditorUserBuildSettings.development ? 1 : 0)}");
            var match = s_unityVersionPat.Matches(_options.UnityVersion)[0];
            int majorVer = 2021;
            int minorVer1 = 4;
            int minorVer2 = 40;

            lines.Add($"#define HYBRIDCLR_UNITY_VERSION {majorVer}{minorVer1.ToString("D2")}{minorVer2.ToString("D2")}");
            lines.Add($"#define HYBRIDCLR_UNITY_{majorVer} 1");
            for (int ver = 2019; ver <= 2023; ver++)
            {
                if (majorVer >= ver)
                {
                    lines.Add($"#define HYBRIDCLR_UNITY_{ver}_OR_NEW 1");
                }
            }
            for (int ver = 6000; ver <= 6100; ver++)
            {
                if (majorVer >= ver)
                {
                    lines.Add($"#define HYBRIDCLR_UNITY_{ver}_OR_NEW 1");
                }
            }

            lines.Add($"#define UNITY_ENGINE_TUANJIE 1");

            frr.Replace("UNITY_VERSION", string.Join("\n", lines));

            frr.Commit(_options.OutputFile);
            //Debug.Log($"[HybridCLR.Editor.Il2CppDef.Generator] output:{_options.OutputFile}");
        }

        private void GeneratePlaceHolderAssemblies()
        {
            var frr = new FileRegionReplace(File.ReadAllText(_options.OutputFile2));

            List<string> lines = new List<string>();

            foreach (var ass in _options.HotUpdateAssemblies)
            {
                lines.Add($"\t\t\"{ass}\",");
            }

            frr.Replace("PLACE_HOLDER", string.Join("\n", lines));

            frr.Commit(_options.OutputFile2);
            //Debug.Log($"[HybridCLR.Editor.Il2CppDef.Generator] output:{_options.OutputFile2}");
        }
    }
}
