using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace BancoRapido.Editor
{
    public static class BancoInstalador
    {
        [MenuItem("Banco VR/Crear simulacion")]
        public static void Crear()
        {
            if(EditorApplication.isPlaying){EditorUtility.DisplayDialog("Banco VR","Deten Play antes de crear la escena.","Aceptar");return;}
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            string ruta=EditorUtility.SaveFilePanelInProject("Guardar simulacion","Intermediario_VR","unity","Elige donde guardar la nueva escena.");
            if(string.IsNullOrEmpty(ruta))return;
            var escena=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var root=new GameObject("BANCO - pulsa Play para generar la oficina");
            root.AddComponent<BancoSimulacion>();
            EditorSceneManager.SaveScene(escena,ruta);
            Selection.activeGameObject=root;
            EditorUtility.DisplayDialog("Simulacion preparada","Pulsa Play y abre la pestana Game. La oficina se construye al iniciar.\n\nClic: botones\nBoton derecho + raton: mirar\nG: seleccion por mirada de 2 segundos","Listo");
        }
    }
}
