# ASCUA - Informe de analisis de eficiencia (Meta Quest)

Equipo: [completar]  |  Dispositivo: Meta Quest [2 / 3 / Pro: completar]  |  Fecha de las mediciones: [completar]
> Las cifras de este informe DEBEN salir de mediciones reales hechas en el lente. Las celdas [completar] se llenan con `perf_resumen.txt` / `perf_log.csv` y con capturas.

## 1. Metodo de medicion
- Herramienta propia `PerfProbe` (corre sola en cada escena): FPS, tiempo de cuadro promedio/peor/percentil 99, draw calls, SetPass, triangulos, renderers y luces activas, memoria. Guarda `perf_log.csv` (1 fila por segundo) y `perf_resumen.txt` en la carpeta de datos de la app.
- Para leer draw calls y triangulos compilar con **Development Build** (en release valen -1).
- Panel dentro del lente: click de ambos joysticks. Optimizaciones ON/OFF: X + Y juntos 0,8 s (reinicia la muestra).
- Sacar los archivos: `adb pull /sdcard/Android/data/<package>/files/perf_resumen.txt` y `perf_log.csv`.
- Complemento (capturas para el informe): Unity Profiler conectado por adb (`Window > Analysis > Profiler`, Target = dispositivo) y/o Meta Quest Developer Hub > Performance Analyzer / OVR Metrics Tool.
- **Recorrido repetible** (el mismo para ANTES y DESPUES, 60 s cada punto): A) Hall quieto mirando el portal; B) Comarca frente a los vendedores; C) piso 1 en medio de una rafaga de fuego; D) cima con el jefe.

## 2. Resultados ANTES (optimizaciones OFF)
| Punto | FPS prom | ms prom | ms P99 | Draw calls | SetPass | Triangulos | Luces | Memoria MB |
|---|---|---|---|---|---|---|---|---|
| A Hall | [completar] | | | | | | | |
| B Comarca | [completar] | | | | | | | |
| C Piso 1 con fuego | [completar] | | | | | | | |
| D Jefe | [completar] | | | | | | | |
Meta de fluidez en Quest: 72 FPS estables (13,9 ms por cuadro).

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

## 5. Resultados DESPUES (optimizaciones ON)
| Punto | FPS prom | ms prom | ms P99 | Draw calls | SetPass | Triangulos | Luces | Memoria MB |
|---|---|---|---|---|---|---|---|---|
| A Hall | [completar] | | | | | | | |
| B Comarca | [completar] | | | | | | | |
| C Piso 1 con fuego | [completar] | | | | | | | |
| D Jefe | [completar] | | | | | | | |

## 6. Comparacion y conclusion
- Mejora de FPS promedio: [completar] %  |  Reduccion de draw calls: [completar] %  |  Mejora del P99: [completar] ms.
- Evidencia adjunta: [capturas del panel / Profiler / OVR Metrics antes y despues].
- Que quedo pendiente y por que: [completar].
