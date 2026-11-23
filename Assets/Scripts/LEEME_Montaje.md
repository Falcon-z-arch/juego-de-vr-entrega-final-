# ASCUA - roguelike en primera persona (celular y VR)

## Pasos
1. Proyecto Unity 6 con plantilla **Universal 3D** (URP). Paquetes: **Input System** y **TextMeshPro** (importa TMP Essentials).
2. `Edit > Project Settings > Player > Other Settings > Active Input Handling = Input System Package (New)`.
3. Copia los `.cs` a `Assets/Scripts/` y la carpeta **Editor** a `Assets/Editor/`. Espera a que compile.
4. Menu **Certamen2 > 1 - Configurar Android**.
5. Menu **Certamen2 > 0 - Construir TODO**. Crea Menu, Comarca (aldea + montana), ZonaPruebas y Galeria y los deja en Build Profiles.
6. Con la escena **Menu** abierta, dale **Play**.

## Menu principal
- **Modo Historia:** cinematica de lore y la Comarca. El boton **Menu** (arriba a la derecha) vuelve al menu.
- **Probar controles:** zona de pruebas con las interacciones sueltas.
- **Galeria de objetos:** una vitrina con cartel por cada objeto del juego (cubo, placa, baliza, respiradero, cofre, altar y vendedor funcionan).

## El juego (roguelike, una sola montana)
Todo ocurre en la escena **Comarca**: la aldea esta al pie de la montana y de ahi se sube en linea recta hacia el este.
1. **La Comarca (pie de la montana):** 3 vendedores (toca el puesto): Herrero, Alquimista y Mercader. Venden mejoras PERMANENTES con las monedas del banco. Aqui NO cae nada.
2. **Puerta de Karnak:** toca la puerta para abrirla. Al abrirse **empieza el bombardeo** de bolas de fuego (solo cae pasada la puerta).
3. **4 pisos en la montana**, unidos por rampas: Bosque Susurrante, Laberinto de Raices, Paso Helado y Ruinas Antiguas. Cada piso es una terraza con un **puzzle en equipo** al azar: siempre hacen falta el **Hobbit** (se agacha y entra por una gatera baja a tirar una palanca o pisar una placa) y el **Mago o el Ingeniero**, a eleccion (foso de lava: el mago dispara al cristal y sube un puente, el ingeniero usa el gancho al pilar; o torre alta: el mago dispara al cristal de arriba, el ingeniero sube con el gancho y tira la palanca). Se pueden hacer en cualquier orden y el avance no se pierde al cambiar de personaje. Al resolverlos **baja la puerta roja del fondo** y la rampa lleva al piso siguiente.
   - Las bolas de fuego caen por oleadas (calma, aviso, rafaga). Una sombra roja marca donde caen; explotan en area chica y quitan 1 vida con empuje suave. La magia las destruye (a veces sueltan moneda). Hay refugios en cada terraza.
   - Cada piso tiene monedas, y un **cofre** (pisos 1 y 3) o un **altar de reliquias** (pisos 2 y 4): eliges 1 de 3 mejoras que duran la partida.
4. **Cima:** al llegar arriba despierta el **Cronarca** (guardian del tiempo). Manecillas giratorias a ras de suelo (salta), bombas de tiempo y rebobinar (te devuelve 4 s atras). Se hiere con magia o lanzandole los cubos (4 de dano).
   Al vencerlo, toca la lava llevando el Ascua: cinematica final y botin (+50 monedas).
5. **Si caes** (0 vidas): el tiempo retrocede, vuelves a la aldea y conservas las monedas ganadas. Si te caes de una rampa, vuelves al ultimo checkpoint (cada puerta abierta guarda uno).

## Personajes y artefacto
- Solo existe el personaje elegido; al cambiar, el nuevo aparece donde estaba el anterior.
- **Carga del artefacto:** quien lo lleva se corrompe (barras arriba a la izquierda). Hobbit 1.2%/s, mago e ingeniero 2.2%/s. Con 70% vas lento; a 100% te vence y hay relevo forzado. Un agotado no se elige hasta bajar de 50%. Cada relevo da 4 s de impulso.
- **Hobbit:** agacharse. **Mago:** magia hacia la mira (rompe fuego, activa cristales, hiere al jefe). **Ingeniero:** gancho libre a cualquier superficie (28 m) que funciona como dash; encadenalo para trepar.

## Mejoras de la Comarca (permanentes)
| Vendedor | Mejora | Niveles |
|---|---|---|
| Herrero | Armadura (+1 vida), Relevo ardiente (+2 s de impulso) | 3 |
| Alquimista | Frasco anticorrupcion (-15% carga), Infusion de descanso (+25% recuperacion) | 3 |
| Mercader | Botas ligeras (+8% velocidad), Gancho rapido (-20% espera) | 3 |

## Reliquias (solo durante la partida)
Corazon de brasa (+1 vida), Botas del viento (+15% vel), Manos de relojero (-30% espera gancho), Pulso arcano (+1 poder magia), Sello del Ascua (-25% carga), Resorte (+20% salto), Bolsa sin fondo (monedas +50%).

## Controles
| Accion | PC | Celular |
|---|---|---|
| Moverse | WASD | Joystick izquierdo |
| Mirar | Clic derecho + mouse | Arrastrar en la mitad derecha |
| Saltar | Espacio | Boton Saltar |
| Habilidad | Boton Habilidad | Boton Habilidad |
| Elegir personaje | Boton Personajes | Boton Personajes |
| Inventario | Tecla I | Boton Inventario |
| Interactuar | Clic en el objeto | Tocar el objeto |
| Cubo | Tocarlo = tomarlo; tocar de nuevo = lanzarlo | igual |

## Modo VR (Quest)
Sin casco el juego va en modo plano. Con casco, `VRRig` se activa solo.
1. `Package Manager`: instala **XR Plugin Management** y **OpenXR**.
2. `Project Settings > XR Plug-in Management > Android`: marca **OpenXR**; en `Enabled Interaction Profiles` agrega **Oculus Touch Controller Profile**; marca **Meta Quest Support**.
3. `Player`: Minimum API 29+, ARM64, IL2CPP. Build and Run con el Quest en modo desarrollador.

| Accion | Control |
|---|---|
| Moverse / girar | Joystick izquierdo / derecho (giro por pasos) |
| Saltar / cambiar personaje / agacharse | A / X / Y |
| Interactuar apuntando | B (mano derecha) |
| Agarrar cubo o agarre de escalada | Grip |
| Ingeniero: gancho | Gatillo apuntando |
| Mago: arco a dos manos | Ambos gatillos; separar las manos carga el disparo |
| Mago: disparo rapido | Toque corto de un gatillo |

Limites actuales en VR: las ventanas de tienda y de reliquias se manejan con toque en pantalla, asi que en VR todavia no se pueden usar; los agarres de escalada solo estan en la galeria.

## Modelos 3D y musica propios
Ver `MODELOS_Y_SONIDOS.md` (ranuras `Cubo`, `BolaFuego`, `Artefacto` y musicas por nombre).

## Tutoriales y carteles
- Los carteles de los vendedores solo aparecen al acercarte.
- La primera vez que usas cada vendedor, el altar, el cofre o llegas al piso 1, sale un mini tutorial. Se guardan; para verlos de nuevo: menu `Certamen2 > Reiniciar progreso y tutoriales` (tambien borra monedas y mejoras).

## Ajustes utiles
- `AvalancheDirector`: tiempos de calma, aviso y rafaga; intensidades.
- `FallingRockSpawner` / `FallingRock`: velocidad, tamano, radio de explosion (`Explosion Radius`), empuje (`Knockback`).
- `BossController`: `Hp Max` (24), velocidad de manecillas en `SetPhase`.
- `RunState.cs`: costos y efectos de mejoras y reliquias.
- `ArtifactBurden`, `AbilityController` (gancho), `PlayerHealth`.

## APK celular
`File > Build Profiles > Android > Build`. Si se ve rosado: `Edit > Rendering > Materials > Convert Selected Built-in Materials to URP`.

## Aviso
Nada de esto se pudo compilar ni probar en Unity desde aqui. Si Unity muestra un error rojo, copialo y se corrige.
