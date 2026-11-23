using UnityEngine;
using UnityEngine.SceneManagement;

// Boton "Escenario": alterna entre el volcan (juego principal) y la zona de pruebas.
public class SceneSwitcher : MonoBehaviour
{
    public string mainScene = "Volcan";
    public string testScene = "ZonaPruebas";

    // Boton "Menu": vuelve al menu principal
    public void ToMenu()
    {
        Time.timeScale = 1f;
        if (Application.CanStreamedLevelBeLoaded("Menu")) SceneManager.LoadScene("Menu");
        else Toggle();
    }

    public void Toggle()
    {
        string now = SceneManager.GetActiveScene().name;
        string next = now == mainScene ? testScene : mainScene;
        if (Application.CanStreamedLevelBeLoaded(next)) SceneManager.LoadScene(next);
        else HUD.Say("Falta la escena " + next + " en Build Profiles");
    }
}
