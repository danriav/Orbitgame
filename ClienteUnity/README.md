# Unity Client - Technical Details

Este directorio contiene el proyecto de Unity para DarkOrbit 2.0.

## 📂 Organización de Scripts

Los scripts se encuentran bajo `Assets/_Project/Scripts/` y están organizados por dominios:

- **Networking**: Lógica de sincronización basada en FishNet.
  - `NetworkShipController.cs`: Control de movimiento autoritativo.
  - `NetworkHealth.cs`: Gestión de vida y daño sincronizado.
  - `AbilityPrismaticShield.cs`: Implementación de habilidades de red.
- **Controllers**: Lógica de control local (Cámara, Input).
- **Data**: Definiciones de datos y ScriptableObjects.
- **Editor**: Herramientas para el flujo de trabajo en el editor.
  - `MainSpaceSceneBuilder.cs`: Generador de escenas.

## 🔧 Workflow de Desarrollo

### Creación de Escenas
No modifiques la escena `Main_Space` directamente si planeas regenerarla. Usa el `MainSpaceSceneBuilder` para añadir nuevos elementos base al pipeline de construcción.

### Red (Networking)
El proyecto utiliza **FishNet**. Al crear nuevos comportamientos, hereda de `NetworkBehaviour` en lugar de `MonoBehaviour` y utiliza los atributos `[ServerRpc]` y `[ObserversRpc]` según sea necesario.

### Assets
Los modelos 3D y materiales deben colocarse en `Assets/Mesh/` y `Assets/Material/`. El sistema de mapeo busca nombres específicos documentados en la raíz del proyecto.

## 🏗️ Builds
Usa los scripts de build automatizados en el menú `DO > Build` (si están implementados en `BuildScripts.cs`) para generar ejecutables limpios.
