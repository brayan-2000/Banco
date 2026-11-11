using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BancoRapido.Editor
{
    public class BancoMaterialesAndroid : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        [MenuItem("Banco VR/Preparar materiales Android")]
        public static void Preparar()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder("Assets/Resources/BancoVR"))
                AssetDatabase.CreateFolder("Assets/Resources", "BancoVR");
            Crear("Lit", "Universal Render Pipeline/Lit");
            Crear("Unlit", "Universal Render Pipeline/Unlit");
            AssetDatabase.SaveAssets();
            Debug.Log("BancoVR: materiales preparados en Assets/Resources/BancoVR.");
        }

        static void Crear(string nombre, string shaderNombre)
        {
            Shader shader = Shader.Find(shaderNombre);
            if (shader == null)
                throw new BuildFailedException("No se encuentra " + shaderNombre + ". Revisa que URP esté instalado.");
            string ruta = "Assets/Resources/BancoVR/" + nombre + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, ruta);
            }
            material.shader = shader;
            material.SetColor("_BaseColor", Color.white);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0f);
            EditorUtility.SetDirty(material);
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            foreach (string nombre in new[] { "Lit", "Unlit" })
            {
                Material material = AssetDatabase.LoadAssetAtPath<Material>(
                    "Assets/Resources/BancoVR/" + nombre + ".mat");
                if (material == null || material.shader == null ||
                    material.shader.name != "Universal Render Pipeline/" + nombre)
                    throw new BuildFailedException(
                        "Ejecuta Banco VR > Preparar materiales Android y vuelve a compilar.");
            }
        }
    }
}
