# ASCUA - Informe de analisis de eficiencia (Meta Quest)

Equipo: Agustin Falcon  |  Dispositivo de medicion: [PC / Unity Editor, o Meta Quest si hay lente: completar]  |  Fecha de las mediciones: [completar]
> Las cifras de este informe DEBEN salir de mediciones reales. Si no hay lente disponible, se miden en el PC (Unity Editor, ventana Game > **Stats**: Batches, SetPass, Tris, y el contador FPS del juego) y se indica en cada tabla "medido en PC". Los valores del Quest quedan pendientes y no se estiman.

## 1. Metodo de medicion
- Herramienta propia `PerfProbe` (corre sola en cada escena): FPS, tiempo de cuadro promedio/peor/percentil 99, draw calls, SetPass, triangulos, renderers y luces activas, memoria. Guarda `perf_log.csv` (1 fila por segundo) y `perf_resumen.txt` en la carpeta de datos de la app.
- Para leer draw calls y triangulos compilar con **Development Build** (en release valen -1).
- Panel dentro del lente: click de ambos joysticks. Optimizaciones ON/OFF: X + Y juntos 0,8 s (reinicia la muestra).
- Sacar los archivos: `adb pull /sdcard/Android/data/<package>/files/perf_resumen.txt` y `perf_log.csv`.
- Complemento (capturas para el informe): Unity Profiler conectado por adb (`Window > Analysis > Profiler`, Target = dispositivo) y/o Meta Quest Developer Hub > Performance Analyzer / OVR Metrics Tool.
- **Recorrido repetible** (el mismo para ANTES y DESPUES, 60 s cada punto): A) Hall quieto mirando el portal; B) Comarca frente a los vendedores; C) piso 1 en medio de una rafaga de fuego; D) cima con el jefe.

## 2. Resultados
| Punto | FPS prom | ms prom | ms P99 | Draw calls | SetPass | Triangulos | Luces | Memoria MB |
|---|---|---|---|---|---|---|---|---|
| A Hall | 72 | | | | | | | |
| B Comarca (aldea, Unity Editor en PC) | 72  | | | | | | | |
Hasta ahora: el contador FPS del juego marco 72 en la aldea (Editor en PC). Como el juego fija `targetFrameRate = 72`, esa cifra es el tope y no mide cuanto margen hay.

## 3. Cuellos de botella (completar con lo que muestren las mediciones)
Hipotesis a verificar (basadas en como esta construido el juego):
1. **Luces puntuales**: cada piso tiene antorchas, sellos y luces de casetas. Costo: pixeles iluminados x luces. Verificar con el numero de luces del panel.
2. **Draw calls**: el mundo se arma con muchas primitivas sueltas (columnas, runas, tejas). Verificar con SetPass/draw calls.
3. **Sombras en tiempo real** y **HDR / texturas de profundidad** del URP Asset.
4. **Resolucion de render por ojo** (Quest es raster-bound).
5. **Efectos**: particulas y destellos de las bolas de fuego, explosiones, luces parpadeantes.
6. **Scripts**: `FindObjectsByType` en `Update`/escaneos periodicos (ya acotados a 0,25-3 s), `IMGUI` de depuracion (vendedores/puertas) y creacion de objetos en tiempo de ejecucion (GC).
Resultado del analisis: [completar: cual fue el cuello real, con captura del Profiler].

## 4. Optimizaciones aplicadas (ya implementadas en el codigo)
| # | Optimizacion | Donde | Efecto esperado |
|---|---|---|---|
| 1 | Sin sombras en luces puntuales y alcance maximo 14 m | `QuestPerf.Lights` | menos pasadas de sombra / pixeles iluminados |
| 2 | Maximo 4 luces reales en el Hall (el resto es emision) | `Hall.BuildPlaza` | menos costo de iluminacion |
| 3 | Static batching de la plaza del Hall | `QuestPerf.Combine` | menos draw calls |
| 4 | `shadowCastingMode = Off` en todos los objetos decorativos | `Hall.Prim` | sin pasada de sombras |
| 5 | Escala de render por ojo 0,85 | `QuestPerf.Apply` (XRSettings.eyeTextureResolutionScale) | menos pixeles |
| 6 | Sin vSync de Unity, 72 FPS objetivo, anisotropico off, LOD bias 0,7, sin soft particles ni reflection probes en tiempo real | `QuestPerf.Apply` | menos ancho de banda |
| 7 | URP Asset: sin HDR, MSAA 4x, sombras a 25 m, sin textura de profundidad/opaca, maximo 2 luces extra por objeto, shadowmap 1024 | `Examen > 3 - Aplicar ajustes de rendimiento Quest` (deja registro antes->despues en `Informe/`) | menos memoria y pasadas |
| 8 | OpenXR Single Pass Instanced | `Examen > 2 - Configurar Meta Quest` | un draw por ambos ojos |
| 9 | Texturas de vendedores y kits a 1024 px | `FantasyKit`, `MedievalKit` | menos memoria de texturas |
| 10 | Texturas de plataformas y puertas **procedurales** (128 px, 1 material compartido por tipo) | `Surf` | cero archivos, pocos materiales |

## 7. Metodo alternativo sin lente (medicion en PC)
1. Abrir la escena, dar Play y abrir `Game > Stats` (Batches, SetPass calls, Tris, Shadow casters).
2. Hacer el recorrido A-D del punto 1 durante 60 s cada uno; anotar FPS (contador del juego), Batches, SetPass y Tris.
3. ANTES: tecla **O** para apagar las optimizaciones; DESPUES: **O** de nuevo. Repetir el mismo recorrido.
4. Guardar capturas de la ventana Stats de cada punto como evidencia.
Aviso: el PC es mucho mas potente que el Quest. Estos valores sirven para comparar antes/despues (cuantos draw calls y luces se ahorran), no para afirmar que el juego corre a 72 FPS en el lente.

## 8. Problemas reales encontrados durante el desarrollo
**Vendedores que dejaban de funcionar (costo varias iteraciones).**
- Sintoma: la tienda no abria al acercarse al vendedor, aunque el debug mostraba "Vendedores: 3".
- Diagnostico: el `Editor.log` mostraba `NearVendor: cerca de Herrero ... Modal.Open=True` mientras el juego seguia corriendo (habilidades y gancho respondian). `Modal.Begin()` pone `timeScale=0` y marca `Open=true`; otras partes (menu, Hall, cambio de escena) restauraban `timeScale=1` sin llamar a `Modal.End()`, y el flag quedaba pegado. Con `Modal.Open` en true, `Vendor.Update` y `NearVendor` nunca volvian a abrir la tienda.
- Solucion: `Modal.Open` ahora se autocorrige (si `timeScale>0` el flag se baja solo) y `Modal` se reinicia al cargar cada escena. Se agrego ademas una linea de depuracion (Modal, tiendaAbierta, timeScale, distancia) para ver el estado en pantalla.
- Leccion: un estado global (`static`) que depende de otro estado global (`timeScale`) debe validarse contra el, o reiniciarse al cambiar de escena.

**Errores de compilacion al migrar a Unity 6.** `TMP_Text.enableWordWrapping` esta obsoleto (CS0619); se reemplazo por `textWrappingMode`.

**Puertas de los pisos 3 y 4 sin abrir.** Se reconstruyen por codigo (`RebuildDoor` / `ClearPath`) y se limpian obstaculos en el pasillo.

**Pendiente de medir en el Quest:** [completar si se prueba en el lente].
