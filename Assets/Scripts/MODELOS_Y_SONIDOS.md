# Modelos 3D, musica y sonidos

El juego funciona sin ningun archivo externo: las formas son simples y la musica se genera por codigo.
Cuando tengas tus archivos, solo los pones en las carpetas de abajo con el nombre exacto. No hay que tocar codigo ni volver a construir escenas.

## Modelos 3D (paso a paso)
1. En Unity crea la carpeta `Assets/Resources/Modelos/` (con ese nombre exacto).
2. Importa tu modelo: arrastra el `.fbx` (o `.obj`) a `Assets/`. Unity NO lee `.glb` solo: conviertelo a `.fbx` en Blender (File > Export > FBX) o instala el paquete **glTFast** (Package Manager > Add package by name: `com.unity.cloud.gltfast`).
3. Convierte el modelo en prefab: arrastralo desde Project a la escena (Hierarchy) y luego desde la Hierarchy a `Assets/Resources/Modelos/`. Borralo de la escena.
4. Ponle EXACTAMENTE uno de estos nombres al prefab:

| Nombre del prefab | Reemplaza a |
|---|---|
| `Cubo` | Cubo de puzzle |
| `BolaFuego` | Bolas de fuego que caen |
| `Artefacto` | El Ascua (mano, menu, galeria) |
| `Moneda` | Monedas |
| `Cofre` | Cofre de monedas |
| `Altar` | Altar de reliquias |
| `Cronarca` | El jefe final |
| `Vendedor_Herrero` | Vendedor Herrero |
| `Vendedor_Alquimista` | Vendedor Alquimista |
| `Vendedor_Mercader` | Vendedor Mercader |

5. Dale Play. No hay que reconstruir escenas ni tocar codigo: el juego busca el prefab por nombre y lo pone en lugar de la forma simple, ajustado al tamano de esa forma. Las colisiones no cambian.

Problemas tipicos:
- **Se ve rosado:** `Edit > Rendering > Materials > Convert Selected Built-in Materials to URP` (seleccionando los materiales del modelo).
- **Sale girado o de lado:** arregla el giro dentro del prefab (crea un objeto vacio, mete tu modelo adentro girado, y guarda ESE vacio como prefab con el nombre de la tabla). Para objetos que ya estan en la escena (vendedores) tambien sirve `Model Slot > Rotation Offset`.
- **Sale chico o grande:** `Model Slot > Scale Multiplier` (en objetos de la escena). Para los que se crean al jugar (moneda, cofre, bola de fuego) ajusta el prefab o el valor en `ModelSlot.cs`.
- **El modelo es muy alto y delgado (una persona):** la forma simple es una capsula; el modelo se escala a su tamano mayor, asi que si queda chico prueba `Scale Multiplier` 1.4.

Los demas objetos (placa, palanca, cristal, puertas, torre, personajes) siguen con formas simples. Si quieres mas ranuras, dime cuales.
Para tu Certamen: estos modelos pueden ser los del Certamen 1 (los 3 de Blender y los de Copilot 3D). Exportalos como .fbx.

## Musica
Crea `Assets/Resources/Audio/` y pon archivos (.mp3 .wav .ogg) con estos nombres:

| Nombre | Donde suena |
|---|---|
| `Musica_menu` | Menu principal y galeria |
| `Musica_intro` | Cinematica de lore |
| `Musica_volcan` | Modo historia |
| `Musica_pruebas` | Probar controles |
| `Musica_comarca` | La Comarca (aldea) |
| `Musica_boss` | Pelea contra el Cronarca |
| `Musica_final` | Cinematica final |

Si falta alguno, suena la pista generada por codigo.

## Efectos de sonido
Los efectos (saltar, disparo, golpe, etc.) son tonos generados en `Sfx.cs`. Si quieres sonidos reales avisame y los cambio por ranuras igual que la musica.

## Donde conseguirlos (gratis)
Revisa siempre la licencia y pon los creditos si la licencia lo pide.
- Modelos: Kenney (kenney.nl, CC0), Quaternius (quaternius.com, CC0), Poly Pizza (poly.pizza), Sketchfab (filtro "Downloadable" y licencia CC).
- Musica y sonidos: Kenney Audio (CC0), OpenGameArt.org, Freesound.org, Pixabay Music, Incompetech.
- Los modelos que hiciste en el Certamen 1 tambien sirven: exportalos como .fbx o .glb y arrastralos al proyecto.

Importante: yo no puedo descargar archivos de internet desde mi entorno, por eso deje todo listo para que solo los sueltes en las carpetas.
