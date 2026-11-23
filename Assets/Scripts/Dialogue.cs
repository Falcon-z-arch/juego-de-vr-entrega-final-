using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Dialogo con los vendedores: varias opciones de pregunta + abrir la tienda.
public static class Dialogue
{
    static GameObject window;
    static TMP_Text body;

    public class Npc
    {
        public string title, greet, who, mountain, rumor, tip;
    }

    public static Npc Get(string vendor)
    {
        switch (vendor)
        {
            case "Herrero":
                return new Npc
                {
                    title = "Barak, el Herrero",
                    greet = "Hmpf. Otro valiente que quiere subir. Si vas a morir, al menos hazlo bien armado.",
                    who = "Forje armas para la guardia de Valdenar antes de que el Cronarca detuviera el tiempo. Ahora solo me queda el yunque... y la terquedad.",
                    mountain = "Cuatro pisos y una cima. Cada puerta pide dos manos distintas: el hobbit por lo bajo, el mago o la ingeniera por lo alto. Nadie sube solo.",
                    rumor = "Dicen que las bolas de fuego caen alrededor de quien sube. No las esquives todas: busca los refugios de piedra.",
                    tip = "Consejo: la armadura te da una vida mas. Es lo primero que compraria yo."
                };
            case "Alquimista":
                return new Npc
                {
                    title = "Ssirra, la Alquimista",
                    greet = "Ssss... bienvenido, viajero. Mis pociones huelen raro, pero funcionan. Casi siempre.",
                    who = "Soy de las Marismas del Sur. Estudio como el Ascua corrompe a quien la lleva. Mi frasco anticorrupcion es fruto de mil errores.",
                    mountain = "El Ascua pesa en el alma. Cuanto mas la cargue un solo personaje, peor se pone. Reparte la carga entre los tres.",
                    rumor = "Un rumor: el Cronarca no es un monstruo, es un guardian que perdio la razon. Quiza el Ascua lo recuerde.",
                    tip = "Consejo: la infusion de descanso acelera a quienes no estas usando. Cambia de personaje a menudo."
                };
            default:
                return new Npc
                {
                    title = "Dorn, el Mercader",
                    greet = "¡Amigo! Botas, ganchos, cuerdas... todo lo que un escalador necesita. Y buen precio, claro.",
                    who = "Recorri todos los caminos de Valdenar. Ahora vendo desde la puerta de Karnak porque aqui pasa el que se atreve a subir.",
                    mountain = "Cada piso cambia: bosque, raices, hielo, ruinas. Y arriba, el jefe. Lleva monedas: lo que ganes subiendo lo guardas aunque caigas.",
                    rumor = "Dicen que los altares de reliquias solo se usan una vez por subida. Elige con cabeza.",
                    tip = "Consejo: las botas ligeras ayudan a salir de donde cae el fuego. Los ganchos rapidos, a subir."
                };
        }
    }

    public static void Open(string vendor, System.Action openShop)
    {
        Close();
        Modal.Begin();
        var npc = Get(vendor);
        var panel = UiKit.Panel(npc.title, new Vector2(1300, 1000), out var content);
        window = panel.gameObject;

        // texto del dialogo
        var tgo = new GameObject("Dialogo", typeof(RectTransform), typeof(LayoutElement));
        tgo.transform.SetParent(content, false);
        var le = tgo.GetComponent<LayoutElement>();
        le.preferredHeight = 230f; le.minHeight = 230f;
        body = tgo.AddComponent<TextMeshProUGUI>();
        body.text = npc.greet;
        body.fontSize = 34;
        body.alignment = TextAlignmentOptions.TopLeft;
        body.color = new Color(0.95f, 0.92f, 0.85f);
        body.textWrappingMode = TextWrappingModes.Normal;
        body.raycastTarget = false;

        UiKit.Btn(content, "Quiero ver tu tienda", 76f, () => { Close(); openShop?.Invoke(); }, 36);
        UiKit.Btn(content, "¿Quien eres?", 76f, () => Say(npc.who), 36);
        UiKit.Btn(content, "¿Que sabes de la montana?", 76f, () => Say(npc.mountain), 36);
        UiKit.Btn(content, "¿Algun rumor?", 76f, () => Say(npc.rumor), 36);
        UiKit.Btn(content, "Dame un consejo", 76f, () => Say(npc.tip), 36);
        UiKit.CloseButton(panel, Close);
    }

    static void Say(string text)
    {
        if (body != null) body.text = text;
        Sfx.Beep(600f, 0.05f);
    }

    public static void Close()
    {
        if (window != null) Object.Destroy(window);
        window = null;
        Modal.End();
    }
}
