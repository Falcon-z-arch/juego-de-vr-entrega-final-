# ASCUA - El peso de Karnak

Roguelike de puzzles en equipo para **Meta Quest** (OpenXR) y celular Android. Unity 6 (URP, Input System).
Proyecto del curso de Realidad Virtual (UDD): Certamen 2 (APK Android) y Examen (Meta Quest).

> Equipo: [completar nombres]  |  Curso/seccion: [completar]  |  Profesor: [completar]

## Historia
El Cronarca detuvo el tiempo en la Comarca. El Ascua, un reloj de arena, corrompe a quien lo carga. Un hobbit, un mago, un ingeniero y un peleador suben la montana de Karnak (4 pisos de puzzles y fuego) para vencer al Cronarca y devolver el Ascua a la lava.

## Como jugar en Meta Quest
1. Instala el APK (`adb install -r ASCUA_Quest.apk`) o usa SideQuest.
2. Empiezas en el **Hall**: lee los paneles (historia, controles, objetivo) y cruza el **portal violeta** (o apuntalo con el control derecho y aprieta el gatillo).
3. En la Comarca habla con los 3 vendedores, abre la Puerta de Karnak y sube.

| Accion | Control Quest |
|---|---|
| Moverse / girar | Joystick izquierdo / derecho (giro por pasos) |
| Saltar / cambiar personaje / agacharse | A / X / Y |
| Interactuar apuntando | B (mano derecha) |
| Agarrar cubo o agarre de escalada | Grip |
| Ingeniero: gancho | Gatillo apuntando |
| Mago: arco a dos manos | Ambos gatillos; separar las manos carga el disparo |
| Panel de rendimiento | Click de ambos joysticks |
| Optimizaciones ON/OFF (para medir) | X + Y juntos (0,8 s) |

## Compilar desde el codigo fuente
1. Unity **6000.6.6f1**, plantilla Universal 3D. Paquetes: Input System, TextMeshPro (importar TMP Essentials), XR Plug-in Management, **OpenXR** y **Unity OpenXR: Meta**.
2. Copia `Scripts/` a `Assets/Scripts/` y `Editor/` a `Assets/Editor/` (ya estan en este repositorio).
3. Menu **Certamen2 > 0 - Construir TODO**.
4. Menu **Examen > 1 - Crear escena Hall**.
5. Menu **Examen > 2 - Configurar Meta Quest** y **Examen > 3 - Aplicar ajustes de rendimiento Quest**.
6. `File > Build Profiles > Android > Build` con el Quest en modo desarrollador.

## Estructura
- `Assets/Scripts`: logica del juego (VRRig = manos y escalada; CoopPuzzle = puzzles en equipo; Hall = hall de acceso; PerfProbe = medicion).
- `Assets/Editor`: generadores de escenas por codigo (Comarca, Menu, Galeria) y `ExamenTools` (Hall, Quest, rendimiento).
- `Informe/`: informe de eficiencia, funcionalidades, secuencia integrada y evidencia de IA.

## Enlaces
- Itch.io: [completar]
- Teaser: [completar]
