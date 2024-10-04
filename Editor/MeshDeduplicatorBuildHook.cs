#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEditor;
using VRC.SDKBase.Editor.BuildPipeline;

[InitializeOnLoad]
public class MeshDeduplicatorBuildHook : IVRCSDKPreprocessAvatarCallback{
	#if MODULAR_AVATAR_EXISTS
	public int callbackOrder => -15;
	#else
	public int callbackOrder => -1025;
	#endif
	
	public bool OnPreprocessAvatar(GameObject avatarGameObject)
	{
		// d4rkpl4y3r.AvatarOptimizer.AvatarBuildHook instance = Microsoft.Practices.ServiceLocator.Current.GetInstance<d4rkpl4y3r.AvatarOptimizer.AvatarBuildHook>();
		d4rkpl4y3r.AvatarOptimizer.AvatarBuildHook instance =  new d4rkpl4y3r.AvatarOptimizer.AvatarBuildHook();
		if (instance != null)
		{
			Debug.Log("d4rkpl4y3r.AvatarOptimizer.AvatarBuildHook found with order " + instance.callbackOrder);
			if(instance.callbackOrder >= callbackOrder){
				EditorUtility.DisplayDialog("Can not reliably deduplicate meshes", "You must manually decrease d4rkpl4y3r.AvatarOptimizer.AvatarBuildHook callbackOrder by one in the source.", "OK");
			}
		}else{
			Debug.Log("d4rkpl4y3r.AvatarOptimizer.AvatarBuildHook not found");
		}
		
		Debug.Log("MeshDeduplicatorBuildHook: OnPreprocessAvatar with order " + callbackOrder);
		// GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
		// sphere.transform.parent = avatarGameObject.transform;
		
		MeshDeduplicator deduplicator = avatarGameObject.GetComponent<MeshDeduplicator>();
		if(deduplicator != null && deduplicator.enabled && deduplicator.deduplicateOnUpload){
			deduplicator.DeduplicateMeshes();
		}
		
		return true;
	}
	
	public MeshDeduplicatorBuildHook(){
		Debug.Log("MeshDeduplicatorBuildHook: constructor with order " + callbackOrder);
		// // StartCoroutine(WaitAfterInitialization);
		// EditorApplication.delayCall += () => {
		// 	WaitAfterInitialization();
		// };
	}
	
	void WaitAfterInitialization()
	{
		
		
	}
}

#endif