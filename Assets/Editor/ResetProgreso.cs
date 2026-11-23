using UnityEditor;
using UnityEngine;

public static class ResetProgreso
{
    [MenuItem("Certamen2/Reiniciar progreso y tutoriales")]
    static void Reset()
    {
        if (!EditorUtility.DisplayDialog("Reiniciar", "Borra monedas, mejoras y tutoriales guardados. ¿Seguro?", "Si", "No")) return;
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
    }
}
