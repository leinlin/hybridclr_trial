using System;
using System.Collections.Generic;
using System.IO;
using HybridCLR.Editor.Commands;
using HybridCLR.Editor.Installer;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Il2Cpp;
using UnityEngine;

namespace HybridCLR.Editor.BuildProcessors
{
    public class BuildIL2CPPProcess : IIl2CppProcessor
    {
        public int callbackOrder => 0;
        
        public static void CopyStripDlls(string srcStripDllPath, BuildTarget target)
        {
            if (!SettingsUtil.Enable)
            {
                Debug.Log($"[CopyStrippedAOTAssemblies] disabled");
                return;
            }

            var dstPath = SettingsUtil.GetAssembliesPostIl2CppStripDir(target);
            BashUtil.RecreateDir(dstPath);

            foreach (var fileFullPath in Directory.GetFiles(srcStripDllPath, "*.dll"))
            {
                var file = Path.GetFileName(fileFullPath);
                File.Copy($"{fileFullPath}", $"{dstPath}/{file}", true);
            }
        }
        
        public void OnBeforeConvertRun(BuildReport report, Il2CppBuildPipelineData data)
        {
            BuildTarget target = data.target;
            string aotDllPath = data.inputDirectory;
            Il2CppDefGeneratorCommand.GenerateIl2CppDef();
        
            // 桥接函数生成依赖于AOT dll，必须保证已经build过，生成AOT dll
            MethodBridgeGeneratorCommand.GenerateMethodBridgeAndReversePInvokeWrapper(target, aotDllPath);
            //AOTReferenceGeneratorCommand.GenerateAOTGenericReference(target);

            // copy 保留 strip之后的AOT DLL
            CopyStripDlls(aotDllPath, target);
            
            if (PrebuildCommand.IsGenAll)
            {
                PrebuildCommand.IsGenAll = false;
                throw new StopIL2CPPStepException("This is gen all build, we just need dll,so stop il2cpp convert and compile step");
            }
        }

        public void OnAfterConvertRun(BuildReport report, Il2CppBuildPipelineData data, string outputDirectory)
        {
            //return;
        }
    }
}