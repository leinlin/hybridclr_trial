using System.Collections.Generic;
using System.IO;
using HybridCLR.Editor.Commands;
using HybridCLR.Editor.Installer;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.UnityLinker;
using UnityEngine;

namespace HybridCLR.Editor.BuildProcessors
{
    public class GenHybridCLRLinkProcess : IUnityLinkerProcessor
    {
        public int callbackOrder { get; }
        public string GenerateAdditionalLinkXmlFile(BuildReport report, UnityLinkerBuildPipelineData data)
        {
            return "";
        }

        private List<string> hotFixDLLFullPathList = new List<string>();
        
        private void CopyHotFixDlls(string srcStripDllPath, BuildTarget target)
        {
            List<string> allHotUpdateDllNames = SettingsUtil.HotUpdateAssemblyNamesExcludePreserved;
            // 检查是否重复填写
            var hotUpdateDllSet = new HashSet<string>();
            foreach(var hotUpdateDll in allHotUpdateDllNames)
            {
                if (string.IsNullOrWhiteSpace(hotUpdateDll))
                {
                    throw new BuildFailedException($"hot update assembly name cann't be empty");
                }
                if (!hotUpdateDllSet.Add(hotUpdateDll))
                {
                    throw new BuildFailedException($"hot update assembly:{hotUpdateDll} is duplicated");
                }
            }
            
            var hotfixDstPath = SettingsUtil.GetHotUpdateDllsOutputDirByTarget(target);
            BashUtil.RecreateDir(hotfixDstPath);

            foreach (var fileFullPath in Directory.GetFiles(srcStripDllPath, "*.dll"))
            {
                var file = Path.GetFileName(fileFullPath);
                if (hotUpdateDllSet.Contains(Path.GetFileNameWithoutExtension(file)))
                {
                    //Debug.Log($"[MoveStrippedAOTAssemblies] move hotfix strip dll {fileFullPath} ==> {hotfixDstPath}/{file}");
                    File.Copy($"{fileFullPath}", $"{hotfixDstPath}/{file}");
                    hotFixDLLFullPathList.Add(fileFullPath);
                }
            }
        }

        private void DeleteHotFixDLLS()
        {
            foreach (var fileFullPath in hotFixDLLFullPathList)
            {
                if(File.Exists(fileFullPath))
                    File.Delete(fileFullPath);
            }
        }

        public void OnBeforeRun(BuildReport report, UnityLinkerBuildPipelineData data)
        {
            BuildTarget target = data.target;
            string aotDllPath = data.inputDirectory;

            CopyHotFixDlls(aotDllPath, target);

            // 这几个生成依赖HotUpdateDlls
            LinkGeneratorCommand.GenerateLinkXml(data.target);
        }

        public void OnAfterRun(BuildReport report, UnityLinkerBuildPipelineData data)
        {
            DeleteHotFixDLLS();
        }
    }
}