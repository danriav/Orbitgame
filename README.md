# DarkOrbit 2.0 - Unity Client

Este proyecto es una reconstrucción moderna y nativa del cliente de DarkOrbit utilizando el motor Unity. El objetivo es transicionar del paradigma de contenedores web (Flash/Electron) hacia un ecosistema tridimensional nativo con alto rendimiento y arquitectura servidor-autoritativa.

## 🚀 Características Principales

- **Motor**: Unity 6 / Unity 2023.x+.
- **Arquitectura de Red**: Implementación con **FishNet** (Server-Authoritative) para una sincronización fluida y segura.
- **Visualización**: Juego en el plano XY con proyección ortográfica, combinando la jugabilidad clásica 2D con modelos 3D de alta fidelidad.
- **Pipeline de Assets**: Sistema automatizado para la integración de assets extraídos del cliente oficial.
- **Modularidad**: Uso de *Assembly Definitions* (.asmdef) para separar lógica de red, editor y runtime.

## 📁 Estructura del Proyecto

- `ClienteUnity/`: Proyecto principal de Unity.
- `Plan.txt`: Manifiesto técnico y arquitectónico detallado (Mayo 2026).
- `Guia_Assets_Naves.txt`: Guía de referencia para el mapeo de modelos y materiales.

## 🛠️ Requisitos e Instalación

1. **Unity Hub**: Asegúrate de tener instalada una versión compatible de Unity (recomendado Unity 6).
2. **FishNet**: El proyecto depende de FishNet para la capa de red. Asegúrate de importar el paquete si no está presente en `Packages`.
3. **Apertura**: Abre la carpeta `ClienteUnity` desde Unity Hub.

## ⚙️ Configuración de Escena

El proyecto incluye una herramienta de automatización para preparar la escena principal:
1. Ve al menú superior en Unity: `DO > Scene Setup > Create Main_Space Scene`.
2. Esto generará la escena `Main_Space` con el `NetworkManager`, la cámara ortográfica y el entorno espacial configurado.

---
*Este repositorio contiene únicamente el código fuente y la estructura del proyecto. Los assets pesados han sido excluidos según las políticas de Git.*
