
#if UNITY_EDITOR_OSX
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Callbacks;
using UnityEditor;
using System.IO;
using UnityEditor.iOS.Xcode;
using UnityEditor.iOS.Xcode.Extensions;

namespace Byn.Unity
{
	public static class IosPostBuild 
	{
		[PostProcessBuild]
		public static void OnPostprocessBuild (BuildTarget buildTarget, string path)
		{
			if (buildTarget == BuildTarget.iOS) {
				
				Debug.Log ("Running OnPostprocessBuild for WebRTC Network / Video Chat asset!");
				IosXcodeFix (path);
			}
		}
		public static void IosXcodeFix(string path)
		{
			PBXProject project = new PBXProject();
			string projPath = path + "/Unity-iPhone.xcodeproj/project.pbxproj";
			project.ReadFromString(File.ReadAllText(projPath));

			string target = project.TargetGuidByName("Unity-iPhone");
			Debug.Log ("Setting linker flag ENABLE_BITCODE to NO");
			project.SetBuildProperty (target, "ENABLE_BITCODE", "NO");

			//get the framework file id (check for possible different locations)
			string fileId = null;

//universal (new)
			if (fileId == null) {
				fileId = project.FindFileGuidByProjectPath ("Frameworks/WebRtcNetwork/Plugins/ios/universal/webrtccsharpwrap.framework");
			}
			if (fileId == null) {
				fileId = project.FindFileGuidByProjectPath ("Frameworks/WebRtcVideoChat/WebRtcNetwork/Plugins/ios/universal/webrtccsharpwrap.framework");
			}

//armv7 only
			if (fileId == null) {
				fileId = project.FindFileGuidByProjectPath ("Frameworks/WebRtcNetwork/Plugins/ios/armv7/webrtccsharpwrap.framework");
			}
			if (fileId == null) {
				fileId = project.FindFileGuidByProjectPath ("Frameworks/WebRtcVideoChat/WebRtcNetwork/Plugins/ios/armv7/webrtccsharpwrap.framework");
			}
//arm64 only
			if (fileId == null) {
				fileId = project.FindFileGuidByProjectPath ("Frameworks/WebRtcNetwork/Plugins/ios/arm64/webrtccsharpwrap.framework");
			}
			if (fileId == null) {
				fileId = project.FindFileGuidByProjectPath ("Frameworks/WebRtcVideoChat/WebRtcNetwork/Plugins/ios/arm64/webrtccsharpwrap.framework");
			}
//manual placement
			if (fileId == null) {
				fileId = project.FindFileGuidByProjectPath ("Frameworks/webrtccsharpwrap.framework");
			}

			Debug.Log ("Adding build phase CopyFrameworks to copy the framework to the app Frameworks directory");

#if UNITY_2017_2_OR_NEWER

			project.AddFileToEmbedFrameworks(target, fileId);
#else
			string copyFilePhase = project.AddCopyFilesBuildPhase(target,"CopyFrameworks", "", "10");
			project.AddFileToBuildSection (target, copyFilePhase, fileId);
			//Couldn't figure out how to set that flag yet.
			Debug.LogWarning("Code Sign On Copy flag must be set manually via Xcode for webrtccsharpwrap.framework:" +
			"Project settings -> Build phases -> Copy Frameworks -> set the flag Code Sign On Copy");
#endif
			

			//make sure the Framework is expected in the Frameworks path. Without that ios won't find the framework
			project.AddBuildProperty (target, "LD_RUNPATH_SEARCH_PATHS", "@executable_path/Frameworks");

			File.WriteAllText(projPath, project.WriteToString());
		}
	}
}

#endif