using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using DarkOrbit.Networking;   // NetworkHealth, NetworkShipController
using DarkOrbit.Combat;       // WeaponLoadout, ParticleAttribute


namespace DarkOrbit.UI
{
    /// <summary>
    /// HUD de combate del juego. Se crea proceduralmente en runtime.
    ///
    /// Layout:
    ///   Abajo-Izquierda : Barras HP (verde) / Shield (azul) / NanoHull (amarillo)
    ///   Abajo-Centro    : Selector de ammo láser + rocket + cantidad
    ///   Arriba-Derecha  : Panel de objetivo (nombre + HP bar + distancia)
    ///   Arriba-Izquierda: Recursos (Uridium, Créditos)
    ///   Derecha-Centro  : Cooldowns de Particle Cannon (Helios/Nidhogg/Indra)
    ///
    /// Teclas:
    ///   X = ciclar ammo láser
    ///   Z = ciclar rocket
    ///   F1/F2/F3 = particle cannons
    /// </summary>
    public class GameHUD : MonoBehaviour
    {
        // ── Singletons / Referencias ───────────────────────────────────────────
        public static GameHUD Instance { get; private set; }

        private NetworkHealth   _localHealth;
        private WeaponLoadout   _localLoadout;
        private NetworkShipController _localController;

        // ── Colores del HUD ────────────────────────────────────────────────────
        private static readonly Color BgColor          = new Color(0f, 0f, 0f, 0.72f);
        private static readonly Color AccentColor      = new Color(0f, 0.81f, 1f, 1f);   // #00CFFF
        private static readonly Color HpColor          = new Color(0.18f, 0.9f, 0.35f);   // verde
        private static readonly Color ShieldColor      = new Color(0.2f, 0.55f, 1f, 1f);  // azul
        private static readonly Color NanoColor        = new Color(1f, 0.85f, 0.1f, 1f);  // amarillo
        private static readonly Color TextColor        = new Color(0.87f, 0.95f, 1f, 1f); // blanco hielo
        private static readonly Color DimTextColor     = new Color(0.55f, 0.65f, 0.75f, 1f);
        private static readonly Color HeliosColor      = new Color(1f, 0.5f, 0.1f, 1f);
        private static readonly Color NidhoggColor     = new Color(0.3f, 1f, 0.2f, 1f);
        private static readonly Color IndraColor       = new Color(0.2f, 0.8f, 1f, 1f);

        // ── Referencias a barras ───────────────────────────────────────────────
        private Image  _hpFill, _shieldFill, _nanoFill;
        private Text   _hpText, _shieldText, _nanoText;

        // Ammo
        private Text   _laserAmmoName, _laserAmmoCount;
        private Text   _rocketName,    _rocketCount;

        // Target
        private GameObject _targetPanel;
        private Text       _targetName, _targetDistance;
        private Image      _targetHpFill;

        // Recursos
        private Text _uridiumText, _creditsText;

        // Particle Cooldowns
        private Image _heliosCd, _nidhoggCd, _indraCd;
        private Text  _heliosPct, _nidhoggPct, _indraPct;

        // Simulated resources (TODO: conectar con sistema de economía)
        private int _uridium = 15000;
        private int _credits = 2500000;

        // ══════════════════════════════════════════════════════════════════════
        //  INICIALIZACIÓN
        // ══════════════════════════════════════════════════════════════════════

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            BuildCanvas();
            StartCoroutine(FindLocalPlayer());
        }

        private IEnumerator FindLocalPlayer()
        {
            // Esperar hasta que FishNet spawne la nave local
            while (_localHealth == null)
            {
                var playerObj = GameObject.FindWithTag("Player");
                if (playerObj != null)
                {
                    _localHealth     = playerObj.GetComponent<NetworkHealth>();
                    _localLoadout    = playerObj.GetComponent<WeaponLoadout>();
                    _localController = playerObj.GetComponent<NetworkShipController>();

                    if (_localHealth != null)
                    {
                        _localHealth.OnStatsUpdate  += RefreshStatBars;
                        // Disparo inicial
                        _localHealth.FireInitialEvent();
                    }

                    if (_localLoadout != null)
                        _localLoadout.OnLoadoutChanged += RefreshAmmoPanel;
                }
                yield return new WaitForSeconds(0.5f);
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        //  UPDATE
        // ══════════════════════════════════════════════════════════════════════

        private void Update()
        {
            RefreshParticleCooldowns();
            RefreshTargetPanel();
        }

        // ══════════════════════════════════════════════════════════════════════
        //  CALLBACKS
        // ══════════════════════════════════════════════════════════════════════

        private void RefreshStatBars(float hp, float maxHp,
                                     float shield, float maxShield,
                                     float nano, float maxNano)
        {
            // HP
            SetBar(_hpFill,     _hpText,     hp,     maxHp,     "HP");
            // Shield
            SetBar(_shieldFill, _shieldText, shield, maxShield, "SHD");
            // NanoHull
            SetBar(_nanoFill,   _nanoText,   nano,   maxNano,   "NANO");
        }

        private void SetBar(Image fill, Text label, float current, float max, string prefix)
        {
            if (fill == null) return;
            float pct = max > 0 ? current / max : 0f;
            fill.fillAmount = pct;
            if (label != null)
                label.text = $"{prefix}  {FormatNumber(current)} / {FormatNumber(max)}";
        }

        private void RefreshAmmoPanel()
        {
            if (_localLoadout == null) return;

            var ammo = _localLoadout.GetActiveLaserAmmo();
            if (_laserAmmoName  != null) _laserAmmoName.text  = ammo?.displayName ?? "--";
            if (_laserAmmoCount != null) _laserAmmoCount.text = _localLoadout.GetActiveLaserAmmoCount().ToString("N0");
            if (ammo != null && _laserAmmoName != null) _laserAmmoName.color = ammo.projectileColor;

            var rocket = _localLoadout.GetActiveRocket();
            if (_rocketName  != null) _rocketName.text  = rocket?.displayName ?? "--";
            if (_rocketCount != null) _rocketCount.text = _localLoadout.GetActiveRocketCount().ToString("N0");
        }

        private void RefreshParticleCooldowns()
        {
            if (_localLoadout == null) return;

            UpdateCooldownUI(_heliosCd,  _heliosPct,  _localLoadout.GetCannonCooldownNormalized(ParticleAttribute.Helios));
            UpdateCooldownUI(_nidhoggCd, _nidhoggPct, _localLoadout.GetCannonCooldownNormalized(ParticleAttribute.Nidhogg));
            UpdateCooldownUI(_indraCd,   _indraPct,   _localLoadout.GetCannonCooldownNormalized(ParticleAttribute.Indra));
        }

        private void UpdateCooldownUI(Image fill, Text pctText, float normalizedCooldown)
        {
            if (fill == null) return;
            fill.fillAmount = normalizedCooldown;
            if (pctText != null)
                pctText.text = normalizedCooldown > 0.01f
                    ? $"{Mathf.CeilToInt(normalizedCooldown * 100f)}%"
                    : "LISTO";
        }

        private void RefreshTargetPanel()
        {
            if (_targetPanel == null || _localController == null) return;
            var target = _localController.GetCurrentTarget();

            if (target == null)
            {
                _targetPanel.SetActive(false);
                return;
            }

            _targetPanel.SetActive(true);
            if (_targetName != null)
                _targetName.text = target.entityName;

            if (_targetDistance != null)
            {
                float dist = Vector3.Distance(transform.position, target.transform.position);
                _targetDistance.text = $"{dist:F0} u";
            }

            var targetHealth = target.GetComponent<NetworkHealth>();
            if (targetHealth != null && _targetHpFill != null)
            {
                float pct = targetHealth.maxHealth.Value > 0
                    ? targetHealth.currentHealth.Value / targetHealth.maxHealth.Value : 0f;
                _targetHpFill.fillAmount = pct;
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        //  CONSTRUCCIÓN DEL CANVAS (procedural)
        // ══════════════════════════════════════════════════════════════════════

        private void BuildCanvas()
        {
            // Raíz del Canvas
            var canvasGO = new GameObject("[HUD] Canvas");
            canvasGO.transform.SetParent(transform);
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGO.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
            canvasGO.AddComponent<GraphicRaycaster>();

            var root = canvasGO.transform;

            BuildStatusPanel(root);
            BuildAmmoPanel(root);
            BuildTargetPanel(root);
            BuildResourcePanel(root);
            BuildParticleCannonPanel(root);
            BuildKeyGuide(root);
        }

        // ── Panel de Estadísticas (abajo izquierda) ───────────────────────────
        private void BuildStatusPanel(Transform root)
        {
            var panel = CreatePanel(root, "[HUD] Status",
                new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(16f, 16f), new Vector2(420f, 140f));

            // Título
            CreateLabel(panel, "SISTEMA DE COMBATE", 10f,
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(10f, -10f), new Vector2(420f, 22f), AccentColor, FontStyle.Bold);

            // Barras
            (_hpFill,     _hpText)     = CreateBar(panel, "HP",     HpColor,     -38f);
            (_shieldFill, _shieldText) = CreateBar(panel, "SHIELD", ShieldColor, -73f);
            (_nanoFill,   _nanoText)   = CreateBar(panel, "NANO",   NanoColor,   -108f);
        }

        private (Image fill, Text label) CreateBar(Transform parent, string prefix,
                                                    Color color, float yOffset)
        {
            float barW = 380f; float barH = 22f;

            // Label
            var lbl = CreateLabel(parent, $"{prefix}  ---", 9f,
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(10f, yOffset), new Vector2(barW, barH),
                DimTextColor, FontStyle.Normal);
            lbl.alignment = TextAnchor.MiddleLeft;

            // Background
            var bg = CreateImage(parent, "BarBG",
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(10f, yOffset), new Vector2(barW, barH),
                new Color(0.08f, 0.08f, 0.12f, 0.9f));

            // Fill
            var fillGO = new GameObject("Fill");
            fillGO.transform.SetParent(bg.transform, false);
            var fillImg = fillGO.AddComponent<Image>();
            fillImg.color    = color;
            fillImg.type     = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillAmount = 1f;
            var fillRect = fillImg.rectTransform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            return (fillImg, lbl);
        }

        // ── Panel de Ammo (abajo centro) ──────────────────────────────────────
        private void BuildAmmoPanel(Transform root)
        {
            var panel = CreatePanel(root, "[HUD] Ammo",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(-200f, 0f), new Vector2(400f, 80f)); // Fixed anchoredPos X to center it

            // Título
            CreateLabel(panel, "MUNICIÓN ACTIVA", 9f,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -8f), new Vector2(0f, 20f), AccentColor, FontStyle.Bold);

            // Laser ammo row
            CreateLabel(panel, "LÁSER:", 9f,
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(10f, -34f), new Vector2(60f, 20f), DimTextColor);
            _laserAmmoName = CreateLabel(panel, "UCB-100", 10f,
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(72f, -34f), new Vector2(130f, 20f), Color.white);
            _laserAmmoCount = CreateLabel(panel, "10,000", 10f,
                new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-10f, -34f), new Vector2(90f, 20f), TextColor);
            _laserAmmoCount.alignment = TextAnchor.MiddleRight;

            // Rocket row
            CreateLabel(panel, "ROCKET:", 9f,
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(10f, -60f), new Vector2(60f, 20f), DimTextColor);
            _rocketName = CreateLabel(panel, "PLT-3030", 10f,
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(72f, -60f), new Vector2(130f, 20f), Color.white);
            _rocketCount = CreateLabel(panel, "100", 10f,
                new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-10f, -60f), new Vector2(90f, 20f), TextColor);
            _rocketCount.alignment = TextAnchor.MiddleRight;
        }

        // ── Panel de Objetivo (arriba derecha) ────────────────────────────────
        private void BuildTargetPanel(Transform root)
        {
            _targetPanel = CreatePanel(root, "[HUD] Target",
                new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-16f, -16f), new Vector2(280f, 90f)).gameObject;
            _targetPanel.SetActive(false);
            var panelT = _targetPanel.transform;

            CreateLabel(panelT, "OBJETIVO", 9f,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -8f), new Vector2(0f, 18f), AccentColor, FontStyle.Bold);

            _targetName = CreateLabel(panelT, "---", 11f,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -28f), new Vector2(0f, 22f), TextColor);

            // HP bar objetivo
            var bgT = CreateImage(panelT, "TargetHPBG",
                new Vector2(0f, 1f), new Vector2(0f, 1f), // Changed anchor to top-left
                new Vector2(10f, -58f), new Vector2(260f, 18f),
                new Color(0.08f, 0.08f, 0.12f, 0.9f));

            var fillGO = new GameObject("TargetHPFill");
            fillGO.transform.SetParent(bgT.transform, false);
            _targetHpFill = fillGO.AddComponent<Image>();
            _targetHpFill.color     = new Color(0.9f, 0.2f, 0.2f);
            _targetHpFill.type      = Image.Type.Filled;
            _targetHpFill.fillMethod = Image.FillMethod.Horizontal;
            _targetHpFill.fillAmount = 1f;
            var r = _targetHpFill.rectTransform;
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;

            _targetDistance = CreateLabel(panelT, "0 u", 9f,
                new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-10f, -58f), new Vector2(80f, 18f), DimTextColor);
            _targetDistance.alignment = TextAnchor.MiddleRight;
        }

        // ── Panel de Recursos (arriba izquierda) ──────────────────────────────
        private void BuildResourcePanel(Transform root)
        {
            var panel = CreatePanel(root, "[HUD] Resources",
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(16f, -16f), new Vector2(220f, 70f));

            CreateLabel(panel, "RECURSOS", 9f,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -8f), new Vector2(0f, 18f), AccentColor, FontStyle.Bold);

            CreateLabel(panel, "⬡ URIDIUM", 9f,
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(10f, -32f), new Vector2(100f, 18f), new Color(1f, 0.85f, 0.1f));
            _uridiumText = CreateLabel(panel, "15,000", 11f,
                new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-10f, -32f), new Vector2(100f, 18f), TextColor);
            _uridiumText.alignment = TextAnchor.MiddleRight;

            CreateLabel(panel, "$ CRÉDITOS", 9f,
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(10f, -55f), new Vector2(100f, 18f), HpColor);
            _creditsText = CreateLabel(panel, "2,500,000", 11f,
                new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-10f, -55f), new Vector2(100f, 18f), TextColor);
            _creditsText.alignment = TextAnchor.MiddleRight;

            RefreshResourceDisplay();
        }

        private void RefreshResourceDisplay()
        {
            if (_uridiumText != null) _uridiumText.text = _uridium.ToString("N0");
            if (_creditsText != null) _creditsText.text = _credits.ToString("N0");
        }

        // ── Panel de Particle Cannons (derecha) ───────────────────────────────
        private void BuildParticleCannonPanel(Transform root)
        {
            var panel = CreatePanel(root, "[HUD] ParticleCannons",
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-16f, -95f), new Vector2(130f, 190f));

            CreateLabel(panel, "P.CANNON", 9f,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -8f), new Vector2(0f, 18f), AccentColor, FontStyle.Bold);

            (_heliosCd,  _heliosPct)  = BuildCannonSlot(panel, "HELIOS",  "F1", HeliosColor,  -36f);
            (_nidhoggCd, _nidhoggPct) = BuildCannonSlot(panel, "NIDHOGG", "F2", NidhoggColor, -100f);
            (_indraCd,   _indraPct)   = BuildCannonSlot(panel, "INDRA",   "F3", IndraColor,   -164f);
        }

        private (Image fill, Text pct) BuildCannonSlot(
            Transform parent, string name, string key, Color color, float yOff)
        {
            // Label nombre
            CreateLabel(parent, $"[{key}] {name}", 9f,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, yOff), new Vector2(0f, 16f), color, FontStyle.Bold);

            // Barra de cooldown
            var bg = CreateImage(parent, $"{name}_CDBg",
                new Vector2(0f, 1f), new Vector2(0f, 1f), // Changed anchor to top-left
                new Vector2(10f, yOff - 18f), new Vector2(110f, 18f),
                new Color(0.08f, 0.08f, 0.12f, 0.9f));


            var fillGO = new GameObject($"{name}_CDFill");
            fillGO.transform.SetParent(bg.transform, false);
            var fill = fillGO.AddComponent<Image>();
            fill.color      = new Color(color.r, color.g, color.b, 0.7f);
            fill.type       = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillAmount = 0f;
            var r = fill.rectTransform;
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;

            // Texto "LISTO"
            var pctText = CreateLabel(parent, "LISTO", 8f,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, yOff - 18f), new Vector2(0f, 18f),
                TextColor);
            pctText.alignment = TextAnchor.MiddleCenter;

            return (fill, pctText);
        }

        // ── Guía de teclas (abajo derecha) ────────────────────────────────────
        private void BuildKeyGuide(Transform root)
        {
            var panel = CreatePanel(root, "[HUD] Keys",
                new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-16f, 16f), new Vector2(190f, 110f));

            string[] lines =
            {
                "[CTRL]  Auto-Laser",
                "[X]     Ciclar Ammo",
                "[Space] Rocket",
                "[Z]     Ciclar Rocket",
                "[F1/F2/F3] P.Cannon",
                "[1]     Prismatic Shield",
            };
            for (int i = 0; i < lines.Length; i++)
            {
                CreateLabel(panel, lines[i], 8f,
                    new Vector2(0f, 1f), new Vector2(1f, 1f),
                    new Vector2(10f, -12f - i * 16f), new Vector2(-20f, 16f),
                    DimTextColor);
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        //  HELPERS DE CONSTRUCCIÓN UI
        // ══════════════════════════════════════════════════════════════════════

        private RectTransform CreatePanel(Transform parent, string goName,
            Vector2 anchorMin, Vector2 anchorMax,
            Vector2 anchoredPos, Vector2 sizeDelta)
        {
            var go  = new GameObject(goName);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = BgColor;

            // Borde acento
            var border = new GameObject("Border");
            border.transform.SetParent(go.transform, false);
            var borderImg = border.AddComponent<Image>();
            borderImg.color = new Color(AccentColor.r, AccentColor.g, AccentColor.b, 0.5f);
            var borderRect = borderImg.rectTransform;
            borderRect.anchorMin = Vector2.zero; borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = new Vector2(-1f, -1f);
            borderRect.offsetMax = new Vector2(1f, 1f);
            border.transform.SetAsFirstSibling();

            var rect = img.rectTransform;
            rect.anchorMin     = anchorMin;
            rect.anchorMax     = anchorMax;
            rect.pivot         = anchorMin;
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta     = sizeDelta;
            return rect;
        }

        private Text CreateLabel(Transform parent, string text, float fontSize,
            Vector2 anchorMin, Vector2 anchorMax,
            Vector2 anchoredPos, Vector2 sizeDelta,
            Color color, FontStyle style = FontStyle.Normal)
        {
            var go = new GameObject("Label_" + text.Substring(0, Mathf.Min(text.Length, 10)));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.text      = text;
            t.fontSize  = Mathf.RoundToInt(fontSize);
            t.color     = color;
            t.fontStyle = style;
            t.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.alignment = TextAnchor.MiddleLeft;
            var rect = t.rectTransform;
            rect.anchorMin        = anchorMin;
            rect.anchorMax        = anchorMax;
            rect.pivot            = anchorMin; // Use anchorMin as pivot for predictable sizing
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta        = sizeDelta;
            return t;
        }

        private Image CreateImage(Transform parent, string goName,
            Vector2 anchorMin, Vector2 anchorMax,
            Vector2 anchoredPos, Vector2 sizeDelta, Color color)
        {
            var go = new GameObject(goName);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            var rect = img.rectTransform;
            rect.anchorMin        = anchorMin;
            rect.anchorMax        = anchorMax;
            rect.pivot            = anchorMin; // Use anchorMin as pivot
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta        = sizeDelta;
            return img;
        }




        private static string FormatNumber(float n)
        {
            if (n >= 1000000) return $"{n / 1000000f:F1}M";
            if (n >= 1000)    return $"{n / 1000f:F0}K";
            return $"{n:F0}";
        }

        private void OnDestroy()
        {
            if (_localHealth != null) _localHealth.OnStatsUpdate -= RefreshStatBars;
            if (_localLoadout != null) _localLoadout.OnLoadoutChanged -= RefreshAmmoPanel;
        }
    }
}
