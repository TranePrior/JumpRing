#if UNITY_EDITOR
using UnityEditor.iOS.Xcode;
using File = System.IO.File;
using UnityEditor;
using UnityEditor.Callbacks;


public class MakeEnriptionKeyFalsePostProccessIOS 
{
	[PostProcessBuild]
	public static void AddFalseEcnyptionKeyToInfoPlist(BuildTarget buildTarget, string pathToBuildProject)
	{
		if (buildTarget == BuildTarget.iOS)
		{
			string plistPath = $"{pathToBuildProject}/Info.plist";
			var document = new PlistDocument();
			document.ReadFromString(File.ReadAllText(plistPath));

			var rootDict = document.root;

			rootDict.SetBoolean("ITSAppUsesNonExemptEncryption",false);
            
			File.WriteAllText(plistPath,document.WriteToString());
		}
	}
}

#endif