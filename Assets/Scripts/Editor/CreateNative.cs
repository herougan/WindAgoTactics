using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class CreateNative : MonoBehaviour
{
    public static GameObject wallQuad; // prefabs

    // [MenuItem("GameObject/Create Material")]
    // static void CreateMaterial()
    // {
    //     // Create a simple material asset

    //     Material material = new Material(Shader.Find("Specular"));
    //     AssetDatabase.CreateAsset(material, "Assets/MyMaterial.mat");

    //     // Print the path of the created asset
    //     Debug.Log(AssetDatabase.GetAssetPath(material));
    // }

    [MenuItem("GameObject/3D Object/Triangle")]
    static void CreateTriangle() {
        // Create base wall
        wallQuad = Resources.Load("prefabs/wall") as GameObject;
        GameObject triangle = Instantiate(wallQuad, Vector3.zero, wallQuad.transform.rotation);
        //
        MeshFilter meshFilter = triangle.GetComponent<MeshFilter>();
        Mesh mesh = new Mesh();
        
        /* Mesh Vertices (Height) */
        Vector3[] vertices = new Vector3[3] {
            new Vector3(0, 0, 0),
            new Vector3(1, 0, 0),
            new Vector3(0, 1, 0),
        };

        /* Mesh Triangulation */
        int[] tris = new int[3] {
            // Tri
            0, 2, 1
        };

        /* Mesh Normals */
        // Vector3[] normals = new Vector[4] {
        //     -Vector3.forward;
        //     -Vector3.forward;
        //     -Vector3.forward;
        //     -Vector3.forward;
        // }
        // mesh.normals = normals;

        /* Mesh Calculations */
        mesh.vertices = vertices;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        meshFilter.mesh = mesh;

        triangle.name = "Triangle";

        // AssetDatabase.CreateAsset(mesh, "Assets/Mesh/Triangle.asset");
        // AssetDatabase.SaveAssets();
        // PrefabUtility.SaveAsPrefabAsset(triangle, "Assets/Prefabs/Blocks/Triangle.prefab");
    }

}
