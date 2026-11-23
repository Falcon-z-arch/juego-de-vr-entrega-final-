# Respaldo en GitHub y publicacion en itch.io

## GitHub (codigo fuente + historial del equipo)
1. Copiar `.gitignore` y `README.md` (de esta entrega) a la raiz del proyecto Unity (`My project (1)`).
2. Subir SOLO: `Assets/`, `Packages/`, `ProjectSettings/`, `README.md`, `.gitignore`, `Informe/`. (No subir `Library/`, `Temp/`, `Logs/`, `UserSettings/`, ni el `.apk`.)
3. Commits de cada integrante con su propio usuario (el historial del equipo es parte de la nota):
```
cd "C:\Users\nitsu\My project (1)"
git init
git add .
git commit -m "ASCUA: proyecto Unity del Certamen 2"
git branch -M main
git remote add origin https://github.com/<usuario>/<repo>.git
git push -u origin main
```
4. Si hay archivos > 100 MB (modelos/audio) usar Git LFS: `git lfs install` y `git lfs track "*.fbx" "*.mp3" "*.wav" "*.png"` antes del `git add`.
5. Para el equipo: `Settings > Collaborators` del repositorio; cada uno trabaja en su rama y abre Pull Request.

## itch.io
1. Crear cuenta -> `Dashboard > Create new project`.
2. Kind of project: **Downloadable**. Subir el `.apk` (marcar "Android"). itch.io no tiene categoria Quest: indicarlo en la descripcion y en las tags (`virtual-reality`, `meta-quest`, `unity`, `roguelike`).
3. Descripcion: pegar historia, controles (tabla del README), requisitos (Meta Quest 2/3, modo desarrollador o SideQuest para instalar) e instrucciones: `adb install -r ASCUA_Quest.apk`.
4. Capturas: minimo 3 (Hall, puzzle, jefe) + el **teaser** (video de YouTube o archivo) en la seccion "Trailer / Screenshots".
5. Visibilidad: Public (o Restricted + contrasena si el profesor lo prefiere). Guardar y copiar el enlace a `Informe/Entrega_Examen.md`.

## Compilar el APK para Quest
1. `Examen > 2 - Configurar Meta Quest`, `Examen > 3 - Aplicar ajustes...`, `Examen > 1 - Crear escena Hall`.
2. Quest: modo desarrollador (app Meta Horizon en el celular), USB, aceptar "Permitir depuracion USB" dentro del lente. `adb devices` debe listarlo.
3. `File > Build Profiles > Android`: marcar la escena Hall primero, y para medir rendimiento marcar `Development Build`; para la entrega final, desmarcarlo. `Build And Run`.
