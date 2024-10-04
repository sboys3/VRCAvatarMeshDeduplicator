using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;

public class MeshDeduplicator : MonoBehaviour, VRC.SDKBase.IEditorOnly
{
	public bool deduplicateOnUpload = true;
	// Start is called before the first frame update
	void Start(){
	}

	// Update is called once per frame
	void Update(){
		
	}
	
	void OnEnable(){
		Debug.Log("Truncate Test: 1.23456789 -> " + TruncateFloat(1.23456789f));
		Debug.Log("Truncate Test: 1345 -> " + TruncateFloat(1345f));
		Debug.Log("Truncate Test: 0.00000001 -> " + TruncateFloat(0.00000001f));
		Debug.Log("Truncate Test: 1234567890 -> " + TruncateFloat(1234567890f));
		if(!deduplicateOnUpload){
			DeduplicateMeshes();
		}
	}
	
	[StructLayout(LayoutKind.Explicit)]
	struct FloatToInt 
	{
		[FieldOffset(0)]private float f;
		[FieldOffset(0)]private uint i;
		public static uint Convert(float value){
			return new FloatToInt { f = value }.i;
		}
		public static float Convert(uint value){
			return new FloatToInt { i = value }.f;
		}
	}
	
	// vertex values seem to be slightly varied after optimization, so we need to truncate them to a low precision before hashing. This cuts off all but 5 bits of the mantissa.
	public float TruncateFloat(float value){
		return Math.Abs(value) < 0.0000001 ? 0 : FloatToInt.Convert(FloatToInt.Convert(value) & 0xFFFC0000);
	}
	
	// generate a unique hash for a mesh based on its data
	public string HashMesh(Mesh mesh){
		// choose up to 200 vertices to hash
		// this is not perfect and may not distinguish very similar meshes
		// optimization can make the vertex values slightly different, so even with truncation this can still sometimes result in different hashes
		string vertexString = "";
		Vector3[] vertices = mesh.vertices;
		int interval = Math.Max(vertices.Length / 200, 1);
		for(int i = 0; i < vertices.Length; i += interval){
			float x = TruncateFloat(vertices[i].x);
			float y = TruncateFloat(vertices[i].y);
			float z = TruncateFloat(vertices[i].z);
			vertexString += "<" + x + "," + y + "," + z + ">";
		}
		//submesh counts
		for(int i = 0; i < mesh.subMeshCount; i++){
			vertexString += "<sub" + i + "," + mesh.GetSubMesh(i).vertexCount + ">";
		}
		Hash128 vertexHash = new Hash128();
		vertexHash.Append(vertexString);
		vertexString = "VERT" + vertexHash.ToString();
		
		
		// sample 47 uv values if available
		string uvString = "NOUV";
		if(mesh.uv.Length > 0){
			Vector2[] uv = mesh.uv;
			int intervalUV = Math.Max(uv.Length / 47, 1);
			for(int i = 0; i < uv.Length; i += intervalUV){
				float u = uv[i].x;
				float v = uv[i].y;
				uvString += "<" + u + "," + v + ">";
			}
			Hash128 uvHash = new Hash128();
			uvHash.Append(uvString);
			uvString = "UV" + uvHash.ToString();
		}
		
		
		// vertex attribute names
		string attributesString = "";
		UnityEngine.Rendering.VertexAttributeDescriptor[] attributes =  mesh.GetVertexAttributes();
		for(int i = 0; i < attributes.Length; i++){
			UnityEngine.Rendering.VertexAttributeDescriptor attribute = attributes[i];
			attributesString += "<" + attribute.attribute + "," + attribute + ">";
		}
		Hash128 attributesHash = new Hash128();
		attributesHash.Append(attributesString);
		attributesString = "ATTR" + attributesHash.ToString();
		
		
		// blend shapes names
		string blendShapesString = "NOBLEND";
		if(mesh.blendShapeCount > 0){
			for(int i = 0; i < mesh.blendShapeCount; i++){
				string blendShapeName = mesh.GetBlendShapeName(i);
				blendShapesString += i + ":" + blendShapeName + "|";
			}
			Hash128 blendShapesHash = new Hash128();
			blendShapesHash.Append(blendShapesString);
			blendShapesString = "BLEND" + blendShapesHash.ToString();
		}
		
		// bone weights
		string boneWeightsString = "NOBONE";
		if(mesh.boneWeights.Length > 0){
			boneWeightsString = "";
			int intervalBoneWeights = Math.Max(mesh.boneWeights.Length / 23, 1);
			for(int i = 0; i < mesh.boneWeights.Length; i += intervalBoneWeights){
				BoneWeight boneWeight = mesh.boneWeights[i];
				boneWeightsString += "<" + boneWeight.weight0 + "," + boneWeight.boneIndex0 + "," + boneWeight.weight1 + "," + boneWeight.boneIndex1 + "," + boneWeight.weight2 + "," + boneWeight.boneIndex2 + "," + boneWeight.weight3 + "," + boneWeight.boneIndex3 + ">";
			}
			Hash128 boneWeightsHash = new Hash128();
			boneWeightsHash.Append(boneWeightsString);
			boneWeightsString = "BONE" + boneWeightsHash.ToString();
		}
			
		
		// include some common things like name, vertex count and triangle count in addition to the hashs
		string meshHash = mesh.name + "|" + mesh.vertices.Length + "|" + mesh.triangles.Length + "|" + vertexString + "|" + uvString + "|" + attributesString + "|" + blendShapesString + "|" + boneWeightsString;
		// Debug.Log(meshHash);
		return meshHash;
	}
	
	public void DisplayProgress(float amount){
		#if UNITY_EDITOR
		EditorUtility.DisplayProgressBar("Optimizing " + gameObject.name, "Deduplicating Meshes", amount);
		#endif
	}
	
	public void DeduplicateMeshes(){
		
		
		Dictionary<string, Mesh> meshDictionary = new Dictionary<string, Mesh>();
		SkinnedMeshRenderer[] skinnedMeshRenderers = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true);
		MeshFilter[] meshFilters = gameObject.GetComponentsInChildren<MeshFilter>(true);
		float totalCount = skinnedMeshRenderers.Length + meshFilters.Length;
		float doneCount = 0;
		DisplayProgress(0);
		foreach(SkinnedMeshRenderer skinnedMeshRenderer in skinnedMeshRenderers){
			Mesh mesh = skinnedMeshRenderer.sharedMesh;
			string hash = HashMesh(mesh);
			if(!meshDictionary.ContainsKey(hash)){
				meshDictionary.Add(hash, mesh);
				Debug.Log(hash);
				Debug.Log("Found " + GetScenePath(skinnedMeshRenderer.gameObject) /*+ " " + AssetDatabase.GetAssetPath(skinnedMeshRenderer.sharedMesh)*/);
				// Debug.Log(AssetDatabase.GetAssetPath(mesh));
			}else{
				// Debug.Log("Replacing mesh for " + skinnedMeshRenderer.name);
				Debug.Log("Replacing " + GetScenePath(skinnedMeshRenderer.gameObject));
				// Debug.Log(AssetDatabase.GetAssetPath(skinnedMeshRenderer.sharedMesh));
				skinnedMeshRenderer.sharedMesh = meshDictionary[hash];
				// Debug.Log(AssetDatabase.GetAssetPath(skinnedMeshRenderer.sharedMesh));
			}
			// skinnedMeshRenderer.sharedMesh = mesh;
			doneCount++;
			DisplayProgress(doneCount / totalCount);
		}
		foreach(MeshFilter meshFilter in meshFilters){
			Mesh mesh = meshFilter.sharedMesh;
			string hash = HashMesh(mesh);
			if(!meshDictionary.ContainsKey(hash)){
				meshDictionary.Add(hash, mesh);
				Debug.Log(hash);
				Debug.Log("Found " + GetScenePath(meshFilter.gameObject));
			}else{
				meshFilter.sharedMesh = meshDictionary[hash];
				Debug.Log("Replacing " + GetScenePath(meshFilter.gameObject));
			}
			// Debug.Log(mesh.name);
			doneCount++;
			DisplayProgress(doneCount / totalCount);
		}
		#if UNITY_EDITOR
		EditorUtility.DisplayProgressBar("Optimizing " + gameObject.name, "Deduplicating Meshes Done", 1.0f);
		EditorUtility.ClearProgressBar();
		EditorUtility.ClearProgressBar();
		#endif
	}
	
	public string GetScenePath(GameObject obj){
		string path = "";
		Transform currentTransform = obj.transform;
		while(currentTransform.parent != null){
			path = currentTransform.name + "/" + path;
			currentTransform = currentTransform.parent;
		}
		path = currentTransform.name + "/" + path;
		return path;
	}
}
