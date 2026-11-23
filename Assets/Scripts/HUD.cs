using System.Collections;
using TMPro;
using UnityEngine;

// Retroalimentacion visual: mensajes en pantalla y temporizador del artefacto.
public class HUD : MonoBehaviour
{
    public static HUD I;

    public TMP_Text messageText;
    public TMP_Text artifactText;
    public TMP_Text lifeText;

    Coroutine hideRoutine;

    public static void SetLife(string text)
    {
        if (I != null && I.lifeText != null) I.lifeText.text = text;
    }

    void Awake()
    {
        I = this;
        if (messageText != null) messageText.text = "";
        if (artifactText != null) artifactText.text = "";
    }

    public static void Say(string msg)
    {
        Debug.Log(msg);
        if (I != null) I.Show(msg);
    }

    public static void SetArtifactText(string text)
    {
        if (I != null && I.artifactText != null) I.artifactText.text = text;
    }

    void Show(string msg)
    {
        if (messageText == null) return;
        messageText.text = msg;
        if (hideRoutine != null) StopCoroutine(hideRoutine);
        hideRoutine = StartCoroutine(HideLater());
    }

    IEnumerator HideLater()
    {
        yield return new WaitForSeconds(2.8f);
        messageText.text = "";
    }
}
