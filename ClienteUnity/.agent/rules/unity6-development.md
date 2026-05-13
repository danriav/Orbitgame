# Reglas de Desarrollo — Unity 6 (6000.x)
> Versión: 1.0 | Motor: Unity 6000.4.6f1 | Lenguaje: C# (.NET 9)

---

## 1. Arquitectura y Estructura del Proyecto

### 1.1 Organización de Carpetas
```
Assets/
├── _Project/               # Todo el código y assets del proyecto
│   ├── Scripts/
│   │   ├── Core/           # Sistemas base (GameManager, EventBus, etc.)
│   │   ├── Entities/       # Entidades del juego (Player, Enemy, NPC)
│   │   ├── Controllers/    # Controladores de input, cámara, UI
│   │   ├── Services/       # Servicios singleton (AudioService, SaveService)
│   │   ├── Data/           # ScriptableObjects y estructuras de datos
│   │   ├── UI/             # Componentes de interfaz
│   │   └── Utils/          # Utilidades y extensiones
│   ├── Prefabs/
│   ├── ScriptableObjects/
│   ├── Scenes/
│   └── Art/
└── Plugins/                # SDKs y librerías externas
```

### 1.2 Namespaces
- Todo el código del proyecto debe estar bajo el namespace `DO` o subespacios: `DO.Core`, `DO.Entities`, `DO.UI`, etc.
- Los plugins externos **no** se modifican; se encapsulan mediante adaptadores.

---

## 2. Principios de Programación Orientada a Objetos

### 2.1 Principios SOLID (obligatorios)
- **S** — Single Responsibility: cada clase tiene una única razón para cambiar.
- **O** — Open/Closed: las clases deben ser extensibles sin modificar su código fuente.
- **L** — Liskov Substitution: las subclases deben poder sustituir a sus clases base.
- **I** — Interface Segregation: preferir interfaces pequeñas y específicas.
- **D** — Dependency Inversion: depender de abstracciones, no de implementaciones concretas.

### 2.2 Herencia vs Composición
- **Preferir composición** sobre herencia profunda.
- La herencia se limita a **máximo 2 niveles** de profundidad.
- Usar interfaces (`IInteractable`, `IDamageable`, `IInitializable`) para contratos de comportamiento.

### 2.3 Ejemplo de estructura correcta
```csharp
// ✅ CORRECTO — Composición + Interfaces
public interface IDamageable
{
    void TakeDamage(float amount);
}

public class HealthComponent : MonoBehaviour, IDamageable
{
    [SerializeField] private float _maxHealth = 100f;
    private float _currentHealth;

    public event Action<float> OnHealthChanged;
    public event Action OnDeath;

    private void Awake() => _currentHealth = _maxHealth;

    public void TakeDamage(float amount)
    {
        _currentHealth = Mathf.Max(0, _currentHealth - amount);
        OnHealthChanged?.Invoke(_currentHealth / _maxHealth);
        if (_currentHealth <= 0) OnDeath?.Invoke();
    }
}

// ❌ INCORRECTO — Herencia excesiva
public class FlyingEnemyBossWithShield : EnemyBoss { ... }
```

---

## 3. Patrones de Diseño en Unity

### 3.1 Patrones permitidos y recomendados

| Patrón | Uso recomendado |
|---|---|
| **ScriptableObject** | Datos de configuración, eventos, canales de comunicación |
| **Event Bus / SO Events** | Comunicación desacoplada entre sistemas |
| **Object Pool** | Proyectiles, partículas, enemigos repetitivos |
| **State Machine** | IA de enemigos, estados del jugador, flujo de UI |
| **Command** | Sistema de input, acciones reversibles (undo/redo) |
| **Observer** (via C# events) | Reacciones a cambios de estado |
| **Service Locator** | Acceso a servicios globales (preferir Injection) |

### 3.2 ScriptableObject como Canal de Eventos
```csharp
// Assets/Scripts/Data/Events/GameEventSO.cs
[CreateAssetMenu(menuName = "DO/Events/Game Event")]
public class GameEventSO : ScriptableObject
{
    private readonly List<IGameEventListener> _listeners = new();

    public void Raise() => _listeners.ForEach(l => l.OnEventRaised());
    public void Register(IGameEventListener listener) => _listeners.Add(listener);
    public void Unregister(IGameEventListener listener) => _listeners.Remove(listener);
}
```

### 3.3 Singleton de Servicios (con restricción)
- Los singletons se usan **únicamente** para servicios globales de vida larga.
- Implementar siempre con `DontDestroyOnLoad`.
- Limitar a: `GameManager`, `AudioService`, `SceneLoader`, `SaveSystem`.

```csharp
public abstract class ServiceBase<T> : MonoBehaviour where T : ServiceBase<T>
{
    public static T Instance { get; private set; }

    protected virtual void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = (T)this;
        DontDestroyOnLoad(gameObject);
    }
}
```

---

## 4. Estándares de Código C#

### 4.1 Convenciones de Nomenclatura

| Elemento | Convención | Ejemplo |
|---|---|---|
| Clases / Interfaces | PascalCase | `PlayerController`, `IDamageable` |
| Métodos públicos | PascalCase | `TakeDamage()`, `Initialize()` |
| Métodos privados | PascalCase | `HandleInput()` |
| Campos privados | _camelCase | `_currentHealth` |
| Propiedades públicas | PascalCase | `MaxHealth` |
| Constantes | UPPER_SNAKE | `MAX_ENEMIES` |
| Eventos | PascalCase con `On` | `OnPlayerDied` |
| Parámetros | camelCase | `float damageAmount` |
| Variables locales | camelCase | `float normalizedHealth` |

### 4.2 Reglas de MonoBehaviour
```csharp
public class PlayerController : MonoBehaviour
{
    // 1. Primero: SerializeFields
    [Header("Movement Settings")]
    [SerializeField] private float _speed = 5f;

    // 2. Propiedades públicas
    public float Speed => _speed;

    // 3. Referencias privadas (no serializadas)
    private Rigidbody _rb;
    private InputSystem_Actions _inputActions;

    // 4. Orden de métodos Unity: Awake → OnEnable → Start → Update → OnDisable → OnDestroy
    private void Awake() => CacheReferences();
    private void OnEnable() => SubscribeEvents();
    private void Start() => Initialize();
    private void Update() => HandleInput();
    private void OnDisable() => UnsubscribeEvents();

    // 5. Métodos privados al final
    private void CacheReferences() => _rb = GetComponent<Rigidbody>();
    private void SubscribeEvents() { }
    private void UnsubscribeEvents() { }
    private void Initialize() { }
    private void HandleInput() { }
}
```

### 4.3 Prohibiciones
- ❌ No usar `FindObjectOfType<>` en `Update()` ni en métodos frecuentes.
- ❌ No usar `GameObject.Find()` nunca.
- ❌ No exponer campos públicos directamente; usar `[SerializeField]` + propiedad.
- ❌ No usar `string` literals para tags/layers; usar constantes o enums.
- ❌ No hacer lógica de negocio dentro de corutinas sin encapsular.

---

## 5. Rendimiento y Optimización

### 5.1 Reglas de Update
- Nada costoso en `Update()`. Usar eventos y callbacks siempre que sea posible.
- Preferir `FixedUpdate()` para física y `LateUpdate()` para seguimiento de cámara.
- Usar el nuevo **Input System** de Unity en lugar del legacy `Input`.

### 5.2 Memoria y GC
- Cachear referencias en `Awake()` o `Start()`, nunca en `Update()`.
- Usar `StringBuilder` para concatenaciones de string en runtime.
- Inicializar listas y diccionarios con capacidad estimada: `new List<T>(capacity)`.
- Usar `ObjectPool<T>` (Unity 2021+) para objetos que se instancian frecuentemente.

### 5.3 Async en Unity 6
- Usar **Awaitable** (Unity 6 nativo) en lugar de corutinas cuando sea posible.
- Usar `UniTask` (si incluido) para async de alto rendimiento.
```csharp
// ✅ Unity 6 — Awaitable
private async Awaitable LoadSceneAsync(string sceneName)
{
    await SceneManager.LoadSceneAsync(sceneName);
}
```

---

## 6. Modularidad y Desacoplamiento

### 6.1 Regla de Dependencias
- Los sistemas no deben referenciarse directamente entre sí.
- La comunicación entre módulos se hace vía **eventos** (`Action`, `UnityEvent`, o `GameEventSO`).
- Cada sistema debe poder activarse/desactivarse sin romper otros.

### 6.2 Interfaces de Módulo
Cada módulo principal debe implementar:
```csharp
public interface IGameModule
{
    void Initialize();
    void Dispose();
}
```

### 6.3 Inyección de Dependencias
- Inyectar dependencias por constructor (clases puras) o por método `Initialize(dep)` (MonoBehaviours).
- Nunca usar `GetComponent<>` para acceder a sistemas externos; recibir la referencia por inyección.

---

## 7. Control de Versiones (Git)

### 7.1 .gitignore
Incluir siempre:
```
Library/
Temp/
Obj/
Build/
Builds/
Logs/
UserSettings/
*.DS_Store
```

### 7.2 Commits
- Formato: `[Área] Descripción breve en imperativo`
- Ejemplos:
  - `[Player] Añadir sistema de dash con cooldown`
  - `[UI] Corregir alineación del HUD en resoluciones 21:9`
  - `[Audio] Integrar AudioService con pool de sonidos`

### 7.3 Ramas
- `main` — producción estable
- `develop` — integración de features
- `feature/nombre-feature` — desarrollo individual
- `fix/nombre-bug` — correcciones

---

## 8. Testing

### 8.1 Edit Mode Tests (Unity Test Runner)
- Todo sistema de lógica pura (sin MonoBehaviour) debe tener tests unitarios.
- Ubicar en `Assets/_Project/Tests/EditMode/`.

### 8.2 Play Mode Tests
- Flujos críticos (inicialización, guardado, carga de escena) deben tener tests de integración.
- Ubicar en `Assets/_Project/Tests/PlayMode/`.

---

## 9. Documentación

- Todo `public` y `protected` debe tener comentario XML summary:
```csharp
/// <summary>
/// Aplica daño al componente de salud y dispara eventos correspondientes.
/// </summary>
/// <param name="amount">Cantidad de daño a aplicar (valor positivo).</param>
public void TakeDamage(float amount) { ... }
```
- Archivos complejos deben incluir un bloque de descripción al inicio.
- Los `ScriptableObject` deben documentar sus campos con `[Tooltip]`.
