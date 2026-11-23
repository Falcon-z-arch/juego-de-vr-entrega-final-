# Certamen 2 - Entrega (plantilla)

Proyecto: zona de pruebas del juego del artefacto (universo del Certamen 1).
Plataforma: Unity 6 (6000.6), APK Android, IL2CPP / ARM64.

## Lista numerada de funcionalidades y su logica

| # | Interaccion | Script | Logica |
|---|---|---|---|
| 1 | Movimiento con joystick tactil | PlayerMovement.cs | Lee la accion Move (On-Screen Stick), mueve con CharacterController relativo a la camara, aplica gravedad y rota hacia la direccion. |
| 2 | Pasar el artefacto / cambiar personaje | PartyManager.cs | Solo el personaje que lleva el artefacto es controlado; cambia el foco de la camara y emparenta el artefacto. |
| 3 | Agacharse (Hobbit) | CharacterRole.cs | El boton Habilidad alterna la escala vertical del personaje. |
| 4 | Disparo magico (Mago) | MagicProjectile.cs | Crea una esfera que avanza hacia adelante, con trigger; se destruye al chocar. |
| 5 | Cristal que reacciona | Crystal.cs | Al recibir un proyectil cambia color, emision y luz, suena y activa sus targets. |
| 6 | Gancho (Ingeniero) | GrapplePoint.cs, AbilityController.cs | Busca el punto de enganche mas cercano en rango y arrastra al personaje a "landing" con una cuerda visual. |
| 7 | Palanca | Lever.cs | Tocar gira el mango (Slerp) y activa o desactiva sus targets. |
| 8 | Puerta | Door.cs | Se desliza hacia arriba al activarse, con sonido y mensaje. |
| 9 | Interruptor de luz | LightSwitch.cs | Enciende o apaga una luz y el brillo emisivo del objeto. |
| 10 | Placa de presion | PressurePlate.cs | Trigger: cambia de color y activa targets mientras alguien la pisa. |
| 11 | Recoger herramienta | ToolPickup.cs, Inventory.cs | Guarda el objeto en el inventario (HUD) y lo quita de la escena. |
| 12 | Maquina | Machine.cs | Solo arranca si el inventario tiene la herramienta: engranaje girando, luz, humo, sonido y luego activa sus targets. |
| 13 | Checkpoint y caida | Checkpoint.cs, KillZone.cs | Guarda el punto de reaparicion; al caer se vuelve a el. |
| 14 | Enemigo perseguidor (opcional) | ChaserEnemy.cs | NavMeshAgent que persigue a quien lleva el artefacto y acelera con el tiempo. |
| 15 | Salto | PlayerMovement.cs | Boton o espacio: velocidad vertical = sqrt(altura * -2 * gravedad) si esta en el suelo. |
| 16 | Elegir personaje | PartyManager.cs, PanelToggle.cs | Panel con los 3 personajes; el elegido pasa a ser el controlado y la camara lo sigue. |
| 17 | Camara orbital | CameraFollow.cs | Gira con mouse o dedo, zoom y desplazamiento; un SphereCast evita que muros la tapen. |
| 18 | Inventario | Inventory.cs, InventoryUI.cs | Panel que se abre con boton o tecla I (no esta siempre visible). |
| 19 | Artefacto y perdida a los 5 min | ArtifactController.cs | Se lleva en un costado del personaje; si se suelta mas de 300 s se pierde y se reinicia la escena. |
| 20 | Cofre | Chest.cs | La tapa se anima al abrirse; el panel permite guardar y sacar objetos; se cierra al alejarse. |
| 21 | Rocas que caen | FallingRockSpawner.cs | Crea esferas con Rigidbody sobre el personaje; si golpean, vuelve al checkpoint. |
| 22 | Volcan y crater | LavaCrater.cs, ZonaPruebasBuilder.cs | Montana en terrazas; al tocar la lava con el artefacto se lanza al crater (objetivo del juego). |

Apoyo: TapInteractor.cs (toque + raycast + distancia), Interactable.cs / Activatable.cs (base de la cadena), HUD.cs y Sfx.cs (retroalimentacion visual y sonora).

## Secuencia integrada (cadena logica)

Cadena principal: **Tomar herramienta (11) -> Encender maquina (12) -> Puerta se abre (8)** con animacion del engranaje, luz, sonido y mensaje de confirmacion.
Si el jugador intenta encender la maquina sin la herramienta, recibe el mensaje "Necesitas: Llave inglesa" (condicion de la cadena).

Cadena secundaria: **Mago dispara (4) -> Cristal brilla (5) -> Segunda puerta se abre (8)**.

## Evidencia de Antigravity y OpenCode (completar por el equipo)
- Capturas de los prompts usados y de las respuestas.
- Problemas que se resolvieron (errores de compilacion, NavMesh, input tactil, build Android).
- Codigo revisado por el equipo: que se cambio o entendio de cada script.

## Demostracion
Video corto del celular mostrando las interacciones 1 a 13 y las dos cadenas.
