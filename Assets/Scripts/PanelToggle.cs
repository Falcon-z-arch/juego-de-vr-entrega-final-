using UnityEngine;

// Muestra u oculta un panel de la UI (ej. el panel "Elegir personaje").
public class PanelToggle : MonoBehaviour
{
    public GameObject panel;

    public void Toggle() { if (panel != null) panel.SetActive(!panel.activeSelf); }
    public void Show() { if (panel != null) panel.SetActive(true); }
    public void Hide() { if (panel != null) panel.SetActive(false); }
}
