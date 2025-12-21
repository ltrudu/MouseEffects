using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text.Json;
using MouseEffects.Core.Effects;
using MouseEffects.Core.Input;
using MouseEffects.Core.Rendering;
using MouseEffects.Core.Time;
using MouseEffects.Text;
using MouseEffects.Text.Style;

using MouseButtons = MouseEffects.Core.Input.MouseButtons;

namespace MouseEffects.Effects.RetroCommand;

public sealed class RetroCommandEffect : EffectBase, IHotkeyProvider, IClickConsumer
{
    // High score entry for leaderboard
    public struct HighScoreEntry
    {
        public int PointsPerMinute { get; set; }
        public string Date { get; set; }

        public HighScoreEntry(int ppm, string date)
        {
            PointsPerMinute = ppm;
            Date = date;
        }
    }

    private struct City
    {
        public Vector2 Position;
        public float Size;
        public int Health;
        public bool IsActive;
        public float DestroyAnim;
    }

    private struct EnemyMissile
    {
        public Vector2 Position;
        public Vector2 StartPosition;
        public Vector2 TargetPosition;
        public float Speed;
        public float Size;
        public bool IsActive;
    }

    private struct CounterMissile
    {
        public Vector2 Position;
        public Vector2 StartPosition;
        public Vector2 TargetPosition;
        public float Speed;
        public float Size;
        public bool IsActive;
        public Vector4 Color;
    }

    private struct Explosion
    {
        public Vector2 Position;
        public float CurrentRadius;
        public float MaxRadius;
        public float ExpandSpeed;
        public float ShrinkSpeed;
        public float Life;
        public float MaxLife;
        public bool IsExpanding;
        public bool IsActive;
        public Vector4 Color;
    }

    private struct Particle
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public Vector4 Color;
        public float Size;
        public float Life;
        public float MaxLife;
        public int ParticleType; // 0=debris, 1=trail
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct EntityGPU
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public Vector4 Color;
        public float Size;
        public float Life;
        public float MaxLife;
        public float EntityType;
    }

    [StructLayout(LayoutKind.Sequential, Size = 64)]
    private struct FrameData
    {
        public Vector2 ViewportSize;
        public float Time;
        public float RenderStyle;
        public float GlowIntensity;
        public float NeonIntensity;
        public float AnimSpeed;
        public float HdrMultiplier;
        public float ExplosionMaxRadius;
        public float CityZoneY;
        public float RetroScanlines;
        public float RetroPixelScale;
        public float Padding1;
        public float Padding2;
        public float Padding3;
        public float Padding4;
    }

    private const int MaxCities = 6;
    private const int MaxEnemyMissiles = 50;
    private const int MaxCounterMissiles = 20;
    private const int MaxExplosions = 30;
    private const int MaxParticles = 500;

    private static readonly EffectMetadata _metadata = new()
    {
        Id = "retrocommand",
        Name = "Retro Command",
        Description = "Defend cities from incoming missiles! Click to launch counter-missiles.",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Interactive
    };

    private IBuffer? _entityBuffer;
    private IBuffer? _frameDataBuffer;
    private IShader? _vertexShader;
    private IShader? _pixelShader;

    private readonly City[] _cities = new City[MaxCities];
    private readonly EnemyMissile[] _enemyMissiles = new EnemyMissile[MaxEnemyMissiles];
    private readonly CounterMissile[] _counterMissiles = new CounterMissile[MaxCounterMissiles];
    private readonly Explosion[] _explosions = new Explosion[MaxExplosions];
    private readonly Particle[] _particles = new Particle[MaxParticles];
    private readonly EntityGPU[] _gpuEntities = new EntityGPU[MaxCities + MaxEnemyMissiles + MaxCounterMissiles + MaxExplosions + MaxParticles + 10];

    private TextOverlay? _textOverlay;

    private Vector2 _lastMousePos;
    private bool _wasLeftPressed;
    private float _viewportWidth = 1920f;
    private float _viewportHeight = 1080f;

    // Render style
    private int _renderStyle = 0;

    // Explosion mode: 0=Instant at cursor, 1=Counter-missile
    private int _explosionMode = 1;
    private float _counterMissileSpeed = 400f;

    // Fire rate mode: 0=Unlimited, 1=MaxActive, 2=Cooldown
    private int _fireRateMode = 1;
    private int _maxActiveExplosions = 5;
    private float _fireCooldown = 0.3f;
    private float _cooldownTimer = 0f;

    // City settings
    private float _citySize = 40f;
    private Vector4 _cityColor = new(0f, 0.8f, 1f, 1f);

    // Base settings
    private float _baseSize = 50f;
    private Vector4 _baseColor = new(0f, 1f, 0.5f, 1f);

    // Enemy missile settings
    private float _enemyMissileSpeed = 100f;
    private float _enemyMissileSpeedIncrease = 15f;
    private int _enemyMissilesPerWave = 10;
    private float _enemyMissileSize = 8f;
    private Vector4 _enemyMissileColor = new(1f, 0.2f, 0.2f, 1f);

    // Explosion settings
    private float _explosionMaxRadius = 80f;
    private float _explosionExpandSpeed = 200f;
    private float _explosionShrinkSpeed = 150f;
    private float _explosionDuration = 1.5f;
    private Vector4 _explosionColor = new(1f, 0.8f, 0.2f, 1f);

    // Visual settings
    private float _glowIntensity = 1.2f;
    private float _neonIntensity = 1.0f;
    private bool _showTrails = true;

    // Wave system
    private float _wavePauseDuration = 2.0f;
    private int _scoreWaveBonus = 100;

    // Scoring
    private int _scoreMissile = 25;
    private int _scoreCityBonus = 500;

    // Score overlay
    private bool _showScoreOverlay = true;
    private float _scoreOverlaySize = 32f;
    private float _scoreOverlaySpacing = 1.3f;
    private float _scoreOverlayMargin = 20f;
    private float _scoreOverlayBgOpacity = 0.7f;
    private Vector4 _scoreOverlayColor = new(0f, 1f, 0f, 1f);
    private float _scoreOverlayX = 70f;
    private float _scoreOverlayY = 49f;

    // Timer and game state
    private float _timerDuration = 90f;
    private float _elapsedTime;
    private int _score;
    private int _currentWave = 1;
    private int _enemyMissilesRemaining;
    private float _waveSpawnTimer;
    private float _wavePauseTimer;
    private bool _isInWavePause;
    private bool _isGameActive;
    private bool _isGameEnded;
    private bool _waitingForFirstHit = true;
    private bool _isGameOver;
    private string _gameOverReason = "";
    private bool _showWelcomeScreen = true;
    private float _totalTime;

    private const string ClickToStartText = "CLICK HERE TO START";
    private const string ClickToRestartText = "CLICK HERE TO RESTART";

    private bool _enableResetHotkey;

    private List<HighScoreEntry> _highScores = new();
    private int _newHighScoreIndex = -1;
    private bool _highScoresSaved;

    public override EffectMetadata Metadata => _metadata;
    public int CurrentScore => _score;
    public float ElapsedTime => _elapsedTime;
    public float RemainingTime => Math.Max(0f, _timerDuration - _elapsedTime);
    public float TimerDuration => _timerDuration;
    public bool IsGameActive => _isGameActive;
    public bool IsGameEnded => _isGameEnded;
    public bool IsGameOver => _isGameOver;
    public string GameOverReason => _gameOverReason;
    public bool WaitingForFirstHit => _waitingForFirstHit;
    public float PointsPerMinute => _elapsedTime > 0 ? (_score / (_elapsedTime / 60f)) : 0f;
    public bool EnableResetHotkey => _enableResetHotkey;
    public IReadOnlyList<HighScoreEntry> HighScores => _highScores;
    public int NewHighScoreIndex => _newHighScoreIndex;
    public int CurrentWave => _currentWave;
    public int CitiesRemaining => CountActiveCities();

    /// <summary>
    /// Consumes left clicks during gameplay to prevent them from reaching the desktop.
    /// Clicks are only consumed when the game is active (playing), not on welcome/game over screens.
    /// </summary>
    public bool ShouldConsumeClicks => _isGameActive && !_isGameOver && !_isGameEnded;

    public event Action<string>? HighScoresChanged;

    public IEnumerable<HotkeyDefinition> GetHotkeys()
    {
        yield return new HotkeyDefinition
        {
            Id = "reset",
            DisplayName = "Reset Game",
            Modifiers = HotkeyModifiers.Alt | HotkeyModifiers.Shift,
            Key = HotkeyKey.R,
            IsEnabled = _enableResetHotkey,
            Callback = ResetGame
        };
    }

    public void ResetGame()
    {
        _showWelcomeScreen = true;
        _score = 0;
        _elapsedTime = 0f;
        _currentWave = 1;
        _isGameActive = true;
        _isGameEnded = false;
        _isGameOver = false;
        _gameOverReason = "";
        _waitingForFirstHit = true;
        _newHighScoreIndex = -1;
        _highScoresSaved = false;
        _isInWavePause = false;
        _wavePauseTimer = 0f;
        _cooldownTimer = 0f;

        InitializeCities();

        for (int i = 0; i < MaxEnemyMissiles; i++)
            _enemyMissiles[i].IsActive = false;

        for (int i = 0; i < MaxCounterMissiles; i++)
            _counterMissiles[i].IsActive = false;

        for (int i = 0; i < MaxExplosions; i++)
            _explosions[i].IsActive = false;

        for (int i = 0; i < MaxParticles; i++)
            _particles[i].Life = 0f;

        _waveSpawnTimer = 0f;
        _enemyMissilesRemaining = 0;
    }

    private void InitializeCities()
    {
        float cityY = _viewportHeight - _citySize;
        float baseX = _viewportWidth / 2f;
        float spacing = _citySize * 2.5f;

        // 3 cities on left
        for (int i = 0; i < 3; i++)
        {
            _cities[i] = new City
            {
                Position = new Vector2(baseX - spacing * (2 - i) - _baseSize, cityY),
                Size = _citySize,
                Health = 1,
                IsActive = true,
                DestroyAnim = 0f
            };
        }

        // 3 cities on right
        for (int i = 0; i < 3; i++)
        {
            _cities[3 + i] = new City
            {
                Position = new Vector2(baseX + spacing * (i + 1) + _baseSize, cityY),
                Size = _citySize,
                Health = 1,
                IsActive = true,
                DestroyAnim = 0f
            };
        }
    }

    private int CountActiveCities()
    {
        int count = 0;
        for (int i = 0; i < MaxCities; i++)
            if (_cities[i].IsActive) count++;
        return count;
    }

    protected override void OnInitialize(IRenderContext context)
    {
        int totalEntities = MaxCities + MaxEnemyMissiles + MaxCounterMissiles + MaxExplosions + MaxParticles + 10;
        var entityDesc = new BufferDescription
        {
            Size = totalEntities * Marshal.SizeOf<EntityGPU>(),
            Type = BufferType.Structured,
            Dynamic = true,
            StructureStride = Marshal.SizeOf<EntityGPU>()
        };
        _entityBuffer = context.CreateBuffer(entityDesc, default);

        var frameDesc = new BufferDescription
        {
            Size = Marshal.SizeOf<FrameData>(),
            Type = BufferType.Constant,
            Dynamic = true
        };
        _frameDataBuffer = context.CreateBuffer(frameDesc, default);

        string shaderSource = LoadEmbeddedShader("RetroCommandShader.hlsl");
        _vertexShader = context.CompileShader(shaderSource, "VSMain", ShaderStage.Vertex);
        _pixelShader = context.CompileShader(shaderSource, "PSMain", ShaderStage.Pixel);

        _textOverlay = new TextOverlay();
        _textOverlay.Initialize(context);

        InitializeCities();
    }

    protected override void OnConfigurationChanged()
    {
        if (Configuration.TryGet("rc_renderStyle", out int style))
            _renderStyle = style;
        if (Configuration.TryGet("rc_explosionMode", out int expMode))
            _explosionMode = expMode;
        if (Configuration.TryGet("rc_counterMissileSpeed", out float cmSpeed))
            _counterMissileSpeed = cmSpeed;
        if (Configuration.TryGet("rc_fireRateMode", out int frMode))
            _fireRateMode = frMode;
        if (Configuration.TryGet("rc_maxActiveExplosions", out int maxExp))
            _maxActiveExplosions = maxExp;
        if (Configuration.TryGet("rc_fireCooldown", out float cooldown))
            _fireCooldown = cooldown;
        if (Configuration.TryGet("rc_citySize", out float citySize))
            _citySize = citySize;
        if (Configuration.TryGet("rc_cityColor", out Vector4 cityCol))
            _cityColor = cityCol;
        if (Configuration.TryGet("rc_baseSize", out float baseSize))
            _baseSize = baseSize;
        if (Configuration.TryGet("rc_baseColor", out Vector4 baseCol))
            _baseColor = baseCol;
        if (Configuration.TryGet("rc_enemyMissileSpeed", out float emSpeed))
            _enemyMissileSpeed = emSpeed;
        if (Configuration.TryGet("rc_enemyMissileSpeedIncrease", out float emInc))
            _enemyMissileSpeedIncrease = emInc;
        if (Configuration.TryGet("rc_enemyMissilesPerWave", out int emWave))
            _enemyMissilesPerWave = emWave;
        if (Configuration.TryGet("rc_enemyMissileSize", out float emSize))
            _enemyMissileSize = emSize;
        if (Configuration.TryGet("rc_enemyMissileColor", out Vector4 emCol))
            _enemyMissileColor = emCol;
        if (Configuration.TryGet("rc_explosionMaxRadius", out float expRad))
            _explosionMaxRadius = expRad;
        if (Configuration.TryGet("rc_explosionExpandSpeed", out float expSpd))
            _explosionExpandSpeed = expSpd;
        if (Configuration.TryGet("rc_explosionShrinkSpeed", out float shrSpd))
            _explosionShrinkSpeed = shrSpd;
        if (Configuration.TryGet("rc_explosionDuration", out float expDur))
            _explosionDuration = expDur;
        if (Configuration.TryGet("rc_explosionColor", out Vector4 expCol))
            _explosionColor = expCol;
        if (Configuration.TryGet("rc_glowIntensity", out float glow))
            _glowIntensity = glow;
        if (Configuration.TryGet("rc_neonIntensity", out float neon))
            _neonIntensity = neon;
        if (Configuration.TryGet("rc_showTrails", out bool trails))
            _showTrails = trails;
        if (Configuration.TryGet("rc_wavePauseDuration", out float wavePause))
            _wavePauseDuration = wavePause;
        if (Configuration.TryGet("rc_scoreWaveBonus", out int waveBonus))
            _scoreWaveBonus = waveBonus;
        if (Configuration.TryGet("rc_scoreMissile", out int scoreMissile))
            _scoreMissile = scoreMissile;
        if (Configuration.TryGet("rc_scoreCityBonus", out int cityBonus))
            _scoreCityBonus = cityBonus;
        if (Configuration.TryGet("rc_showScoreOverlay", out bool showOverlay))
            _showScoreOverlay = showOverlay;
        if (Configuration.TryGet("rc_scoreOverlaySize", out float overlaySize))
            _scoreOverlaySize = overlaySize;
        if (Configuration.TryGet("rc_scoreOverlaySpacing", out float overlaySpacing))
            _scoreOverlaySpacing = overlaySpacing;
        if (Configuration.TryGet("rc_scoreOverlayMargin", out float overlayMargin))
            _scoreOverlayMargin = overlayMargin;
        if (Configuration.TryGet("rc_scoreOverlayBgOpacity", out float bgOpacity))
            _scoreOverlayBgOpacity = bgOpacity;
        if (Configuration.TryGet("rc_scoreOverlayColor", out Vector4 overlayColor))
            _scoreOverlayColor = overlayColor;
        if (Configuration.TryGet("rc_scoreOverlayX", out float overlayX))
            _scoreOverlayX = overlayX;
        if (Configuration.TryGet("rc_scoreOverlayY", out float overlayY))
            _scoreOverlayY = overlayY;
        if (Configuration.TryGet("rc_timerDuration", out float timerDur))
            _timerDuration = timerDur;
        if (Configuration.TryGet("rc_enableResetHotkey", out bool resetHotkey))
            _enableResetHotkey = resetHotkey;

        if (Configuration.TryGet("rc_highScoresJson", out string? highScoresJson) && !string.IsNullOrEmpty(highScoresJson))
        {
            try
            {
                var loaded = JsonSerializer.Deserialize<List<HighScoreEntry>>(highScoresJson);
                if (loaded != null && loaded.Count > 0)
                    _highScores = loaded;
            }
            catch { }
        }

        if (_highScores.Count == 0)
        {
            _highScores = new List<HighScoreEntry>
            {
                new(2000, "21/12/2025"),
                new(1500, "21/12/2025"),
                new(1000, "21/12/2025"),
                new(500, "21/12/2025"),
                new(200, "21/12/2025")
            };
        }
    }

    private void TriggerGameOver(string reason)
    {
        _isGameOver = true;
        _isGameEnded = true;
        _isGameActive = false;
        _gameOverReason = reason;
    }

    private void CheckAndUpdateHighScores()
    {
        if (_highScoresSaved) return;
        _highScoresSaved = true;

        int currentPpm = (int)PointsPerMinute;
        string today = DateTime.Now.ToString("dd/MM/yyyy");

        int insertIndex = -1;
        for (int i = 0; i < _highScores.Count; i++)
        {
            if (currentPpm > _highScores[i].PointsPerMinute)
            {
                insertIndex = i;
                break;
            }
        }

        if (insertIndex >= 0)
        {
            _highScores.Insert(insertIndex, new HighScoreEntry(currentPpm, today));
            while (_highScores.Count > 5)
                _highScores.RemoveAt(_highScores.Count - 1);
            _newHighScoreIndex = insertIndex;

            string json = JsonSerializer.Serialize(_highScores);
            HighScoresChanged?.Invoke(json);
        }
        else if (_highScores.Count < 5)
        {
            _highScores.Add(new HighScoreEntry(currentPpm, today));
            _newHighScoreIndex = _highScores.Count - 1;

            string json = JsonSerializer.Serialize(_highScores);
            HighScoresChanged?.Invoke(json);
        }
        else
        {
            _newHighScoreIndex = -1;
        }
    }

    public string GetHighScoresJson() => JsonSerializer.Serialize(_highScores);

    protected override void OnUpdate(GameTime gameTime, MouseState mouseState)
    {
        float dt = (float)gameTime.DeltaTime.TotalSeconds;
        _totalTime = (float)gameTime.TotalTime.TotalSeconds;

        // Update cooldown timer
        if (_cooldownTimer > 0)
            _cooldownTimer -= dt;

        // Handle welcome screen
        if (_showWelcomeScreen)
        {
            bool leftPressed = mouseState.IsButtonPressed(MouseButtons.Left);
            if (leftPressed && !_wasLeftPressed)
            {
                float centerX = _viewportWidth / 2f;
                float centerY = _viewportHeight / 2f;
                var textPos = new Vector2(centerX, centerY + _scoreOverlaySize * 3f);
                float textSize = _scoreOverlaySize * 0.8f;

                if (IsPointInCenteredText(mouseState.Position, ClickToStartText, textPos, textSize))
                {
                    _showWelcomeScreen = false;
                    _isGameActive = true;
                    InitializeCities();
                    StartNewWave();
                }
            }
            _wasLeftPressed = leftPressed;
            return;
        }

        // Handle game over/success - click to restart
        if (_isGameEnded)
        {
            bool leftPressed = mouseState.IsButtonPressed(MouseButtons.Left);
            if (leftPressed && !_wasLeftPressed)
            {
                float textSize = _scoreOverlaySize * 0.8f;
                Vector2 textPos;

                if (_isGameOver)
                {
                    var center = new Vector2(_viewportWidth / 2f, _viewportHeight / 2f);
                    textPos = center + new Vector2(0, _scoreOverlaySize * 7f);
                }
                else
                {
                    float hsCenterX = _viewportWidth / 2f;
                    float hsCenterY = _viewportHeight / 2f;
                    float hsEntrySize = _scoreOverlaySize * 1.0f;
                    float entryY = hsCenterY - hsEntrySize * 1.5f;
                    float entryLineHeight = hsEntrySize * 2.5f;
                    int scoreCount = Math.Min(_highScores.Count, 5);
                    entryY += scoreCount * entryLineHeight;
                    textPos = new Vector2(hsCenterX, entryY + hsEntrySize);
                }

                if (IsPointInCenteredText(mouseState.Position, ClickToRestartText, textPos, textSize))
                {
                    ResetGame();
                }
            }
            _wasLeftPressed = leftPressed;
            return;
        }

        // Update timer if game is active and not waiting for first hit
        if (_isGameActive && !_isGameEnded && !_waitingForFirstHit)
        {
            _elapsedTime += dt;

            if (_elapsedTime >= _timerDuration)
            {
                _isGameEnded = true;
                _isGameActive = false;

                // Add city bonus
                _score += CountActiveCities() * _scoreCityBonus;

                CheckAndUpdateHighScores();
            }
        }

        // Update all entities
        UpdateExplosions(dt);
        UpdateParticles(dt);

        if (_isGameEnded)
            return;

        // Handle wave pause
        if (_isInWavePause)
        {
            _wavePauseTimer -= dt;
            if (_wavePauseTimer <= 0)
            {
                _isInWavePause = false;
                _currentWave++;
                StartNewWave();
            }
            _wasLeftPressed = mouseState.IsButtonPressed(MouseButtons.Left);
            return;
        }

        UpdateEnemyMissiles(dt);
        UpdateCounterMissiles(dt);
        CheckCollisions();

        // Check if wave is complete
        if (_enemyMissilesRemaining <= 0 && CountActiveEnemyMissiles() == 0 && !_waitingForFirstHit)
        {
            // Award wave bonus
            _score += CountActiveCities() * _scoreWaveBonus;
            _isInWavePause = true;
            _wavePauseTimer = _wavePauseDuration;
        }

        // Spawn enemy missiles
        if (_enemyMissilesRemaining > 0 && !_isInWavePause)
        {
            float spawnInterval = 2.0f / (1f + _currentWave * 0.2f);
            _waveSpawnTimer += dt;
            if (_waveSpawnTimer >= spawnInterval)
            {
                SpawnEnemyMissile();
                _waveSpawnTimer = 0f;
            }
        }

        // Handle click input - ALL clicks captured during gameplay
        if (_isGameActive)
        {
            bool leftPressed = mouseState.IsButtonPressed(MouseButtons.Left);
            if (leftPressed && !_wasLeftPressed)
            {
                TryFire(mouseState.Position);
            }
            _wasLeftPressed = leftPressed;
        }

        _lastMousePos = mouseState.Position;
    }

    private void StartNewWave()
    {
        _enemyMissilesRemaining = _enemyMissilesPerWave + (_currentWave - 1) * 2;
        _waveSpawnTimer = 0f;
    }

    private int CountActiveEnemyMissiles()
    {
        int count = 0;
        for (int i = 0; i < MaxEnemyMissiles; i++)
            if (_enemyMissiles[i].IsActive) count++;
        return count;
    }

    private int CountActiveExplosions()
    {
        int count = 0;
        for (int i = 0; i < MaxExplosions; i++)
            if (_explosions[i].IsActive) count++;
        return count;
    }

    private bool CanFire()
    {
        switch (_fireRateMode)
        {
            case 0: // Unlimited
                return true;
            case 1: // Max active
                return CountActiveExplosions() < _maxActiveExplosions;
            case 2: // Cooldown
                return _cooldownTimer <= 0;
            default:
                return true;
        }
    }

    private void TryFire(Vector2 targetPosition)
    {
        if (!CanFire()) return;

        if (_fireRateMode == 2)
            _cooldownTimer = _fireCooldown;

        if (_explosionMode == 0)
        {
            // Instant explosion at cursor
            SpawnExplosion(targetPosition);
        }
        else
        {
            // Launch counter-missile
            SpawnCounterMissile(targetPosition);
        }
    }

    private void SpawnExplosion(Vector2 position)
    {
        for (int i = 0; i < MaxExplosions; i++)
        {
            if (!_explosions[i].IsActive)
            {
                _explosions[i] = new Explosion
                {
                    Position = position,
                    CurrentRadius = 0f,
                    MaxRadius = _explosionMaxRadius,
                    ExpandSpeed = _explosionExpandSpeed,
                    ShrinkSpeed = _explosionShrinkSpeed,
                    Life = _explosionDuration,
                    MaxLife = _explosionDuration,
                    IsExpanding = true,
                    IsActive = true,
                    Color = _explosionColor
                };
                break;
            }
        }
    }

    private void SpawnCounterMissile(Vector2 targetPosition)
    {
        for (int i = 0; i < MaxCounterMissiles; i++)
        {
            if (!_counterMissiles[i].IsActive)
            {
                Vector2 basePos = new(_viewportWidth / 2f, _viewportHeight - _baseSize);
                _counterMissiles[i] = new CounterMissile
                {
                    Position = basePos,
                    StartPosition = basePos,
                    TargetPosition = targetPosition,
                    Speed = _counterMissileSpeed,
                    Size = _enemyMissileSize * 0.8f,
                    IsActive = true,
                    Color = new Vector4(0.2f, 1f, 0.5f, 1f)
                };
                break;
            }
        }
    }

    private void SpawnEnemyMissile()
    {
        if (_enemyMissilesRemaining <= 0) return;

        // Find an active city to target
        var activeCities = new List<int>();
        for (int i = 0; i < MaxCities; i++)
            if (_cities[i].IsActive) activeCities.Add(i);

        if (activeCities.Count == 0) return;

        int targetCityIndex = activeCities[Random.Shared.Next(activeCities.Count)];
        Vector2 targetPos = _cities[targetCityIndex].Position;

        for (int i = 0; i < MaxEnemyMissiles; i++)
        {
            if (!_enemyMissiles[i].IsActive)
            {
                float startX = Random.Shared.NextSingle() * _viewportWidth;
                Vector2 startPos = new(startX, -_enemyMissileSize);

                float waveSpeedBonus = (_currentWave - 1) * _enemyMissileSpeedIncrease;
                float speed = _enemyMissileSpeed + waveSpeedBonus + Random.Shared.NextSingle() * 30f;

                _enemyMissiles[i] = new EnemyMissile
                {
                    Position = startPos,
                    StartPosition = startPos,
                    TargetPosition = targetPos,
                    Speed = speed,
                    Size = _enemyMissileSize,
                    IsActive = true
                };

                _enemyMissilesRemaining--;
                break;
            }
        }
    }

    private void UpdateEnemyMissiles(float dt)
    {
        for (int i = 0; i < MaxEnemyMissiles; i++)
        {
            ref EnemyMissile missile = ref _enemyMissiles[i];
            if (!missile.IsActive) continue;

            Vector2 dir = Vector2.Normalize(missile.TargetPosition - missile.Position);
            missile.Position += dir * missile.Speed * dt;

            // Spawn trail particle
            if (_showTrails && Random.Shared.NextSingle() > 0.7f)
            {
                SpawnTrailParticle(missile.Position, _enemyMissileColor * 0.5f);
            }

            // Check if reached target city
            float distToTarget = Vector2.Distance(missile.Position, missile.TargetPosition);
            if (distToTarget < _citySize / 2f)
            {
                // Hit city
                for (int c = 0; c < MaxCities; c++)
                {
                    if (_cities[c].IsActive && Vector2.Distance(missile.TargetPosition, _cities[c].Position) < _citySize)
                    {
                        _cities[c].IsActive = false;
                        _cities[c].DestroyAnim = 1f;
                        SpawnCityDebris(_cities[c].Position);
                        break;
                    }
                }

                missile.IsActive = false;

                // Check game over
                if (CountActiveCities() == 0)
                {
                    TriggerGameOver("ALL CITIES DESTROYED");
                }
            }
        }
    }

    private void UpdateCounterMissiles(float dt)
    {
        for (int i = 0; i < MaxCounterMissiles; i++)
        {
            ref CounterMissile missile = ref _counterMissiles[i];
            if (!missile.IsActive) continue;

            Vector2 dir = Vector2.Normalize(missile.TargetPosition - missile.Position);
            missile.Position += dir * missile.Speed * dt;

            // Spawn trail
            if (_showTrails && Random.Shared.NextSingle() > 0.6f)
            {
                SpawnTrailParticle(missile.Position, missile.Color * 0.5f);
            }

            // Check if reached target
            float distToTarget = Vector2.Distance(missile.Position, missile.TargetPosition);
            if (distToTarget < 10f)
            {
                // Explode at target
                SpawnExplosion(missile.TargetPosition);
                missile.IsActive = false;
            }
        }
    }

    private void UpdateExplosions(float dt)
    {
        for (int i = 0; i < MaxExplosions; i++)
        {
            ref Explosion exp = ref _explosions[i];
            if (!exp.IsActive) continue;

            exp.Life -= dt;
            if (exp.Life <= 0)
            {
                exp.IsActive = false;
                continue;
            }

            if (exp.IsExpanding)
            {
                exp.CurrentRadius += exp.ExpandSpeed * dt;
                if (exp.CurrentRadius >= exp.MaxRadius)
                {
                    exp.CurrentRadius = exp.MaxRadius;
                    exp.IsExpanding = false;
                }
            }
            else
            {
                exp.CurrentRadius -= exp.ShrinkSpeed * dt;
                if (exp.CurrentRadius <= 0)
                {
                    exp.IsActive = false;
                }
            }
        }
    }

    private void UpdateParticles(float dt)
    {
        for (int i = 0; i < MaxParticles; i++)
        {
            ref Particle p = ref _particles[i];
            if (p.Life <= 0f) continue;

            p.Life -= dt;
            if (p.Life <= 0f) continue;

            p.Position += p.Velocity * dt;
            p.Velocity *= 0.96f;
            p.Velocity.Y += 100f * dt;
        }
    }

    private void CheckCollisions()
    {
        // Check explosions vs enemy missiles
        for (int e = 0; e < MaxExplosions; e++)
        {
            ref Explosion exp = ref _explosions[e];
            if (!exp.IsActive || exp.CurrentRadius <= 0) continue;

            for (int m = 0; m < MaxEnemyMissiles; m++)
            {
                ref EnemyMissile missile = ref _enemyMissiles[m];
                if (!missile.IsActive) continue;

                float dist = Vector2.Distance(exp.Position, missile.Position);
                if (dist <= exp.CurrentRadius)
                {
                    // First hit starts timer
                    if (_waitingForFirstHit)
                    {
                        _waitingForFirstHit = false;
                    }

                    _score += _scoreMissile;
                    SpawnDebris(missile.Position, _enemyMissileColor);
                    missile.IsActive = false;
                }
            }
        }
    }

    private void SpawnDebris(Vector2 position, Vector4 color)
    {
        int count = 8;
        for (int i = 0; i < count; i++)
        {
            for (int p = 0; p < MaxParticles; p++)
            {
                if (_particles[p].Life <= 0)
                {
                    float angle = Random.Shared.NextSingle() * MathF.PI * 2f;
                    float force = 50f + Random.Shared.NextSingle() * 100f;

                    _particles[p] = new Particle
                    {
                        Position = position,
                        Velocity = new Vector2(MathF.Cos(angle) * force, MathF.Sin(angle) * force),
                        Color = color,
                        Size = 3f + Random.Shared.NextSingle() * 3f,
                        Life = 0.5f + Random.Shared.NextSingle() * 0.5f,
                        MaxLife = 1f,
                        ParticleType = 0
                    };
                    break;
                }
            }
        }
    }

    private void SpawnCityDebris(Vector2 position)
    {
        int count = 20;
        for (int i = 0; i < count; i++)
        {
            for (int p = 0; p < MaxParticles; p++)
            {
                if (_particles[p].Life <= 0)
                {
                    float angle = Random.Shared.NextSingle() * MathF.PI * 2f;
                    float force = 100f + Random.Shared.NextSingle() * 150f;

                    _particles[p] = new Particle
                    {
                        Position = position + new Vector2(Random.Shared.NextSingle() * 20f - 10f, 0),
                        Velocity = new Vector2(MathF.Cos(angle) * force, MathF.Sin(angle) * force - 50f),
                        Color = _cityColor,
                        Size = 4f + Random.Shared.NextSingle() * 4f,
                        Life = 1f + Random.Shared.NextSingle() * 0.5f,
                        MaxLife = 1.5f,
                        ParticleType = 0
                    };
                    break;
                }
            }
        }
    }

    private void SpawnTrailParticle(Vector2 position, Vector4 color)
    {
        for (int p = 0; p < MaxParticles; p++)
        {
            if (_particles[p].Life <= 0)
            {
                _particles[p] = new Particle
                {
                    Position = position + new Vector2(Random.Shared.NextSingle() * 4f - 2f, Random.Shared.NextSingle() * 4f - 2f),
                    Velocity = new Vector2(Random.Shared.NextSingle() * 10f - 5f, Random.Shared.NextSingle() * 20f),
                    Color = color,
                    Size = 2f,
                    Life = 0.15f,
                    MaxLife = 0.15f,
                    ParticleType = 1
                };
                break;
            }
        }
    }

    protected override void OnRender(IRenderContext context)
    {
        if (_vertexShader == null || _pixelShader == null)
            return;

        _viewportWidth = context.ViewportSize.X;
        _viewportHeight = context.ViewportSize.Y;

        float totalTime = (float)DateTime.Now.TimeOfDay.TotalSeconds;

        var frameData = new FrameData
        {
            ViewportSize = context.ViewportSize,
            Time = totalTime,
            RenderStyle = _renderStyle,
            GlowIntensity = _glowIntensity,
            NeonIntensity = _neonIntensity,
            AnimSpeed = 2.0f,
            HdrMultiplier = context.HdrPeakBrightness,
            ExplosionMaxRadius = _explosionMaxRadius,
            CityZoneY = _viewportHeight - _citySize,
            RetroScanlines = 1f,
            RetroPixelScale = 8f
        };
        context.UpdateBuffer(_frameDataBuffer!, frameData);

        int entityIndex = 0;
        int totalEntities = _gpuEntities.Length;

        // Add cities
        for (int i = 0; i < MaxCities && entityIndex < totalEntities; i++)
        {
            ref City city = ref _cities[i];
            float entityType = city.IsActive ? 4f : 5f; // 4=intact, 5=destroyed

            _gpuEntities[entityIndex] = new EntityGPU
            {
                Position = city.Position,
                Velocity = Vector2.Zero,
                Color = _cityColor,
                Size = city.Size,
                Life = 1f,
                MaxLife = 1f,
                EntityType = entityType
            };
            entityIndex++;
        }

        // Add missile base
        if (entityIndex < totalEntities)
        {
            _gpuEntities[entityIndex] = new EntityGPU
            {
                Position = new Vector2(_viewportWidth / 2f, _viewportHeight - _baseSize / 2f),
                Velocity = Vector2.Zero,
                Color = _baseColor,
                Size = _baseSize,
                Life = 1f,
                MaxLife = 1f,
                EntityType = 6f
            };
            entityIndex++;
        }

        // Add crosshair at mouse position (only during gameplay)
        if (_isGameActive && !_showWelcomeScreen && !_isGameEnded && entityIndex < totalEntities)
        {
            _gpuEntities[entityIndex] = new EntityGPU
            {
                Position = _lastMousePos,
                Velocity = Vector2.Zero,
                Color = new Vector4(1f, 1f, 1f, 0.8f),
                Size = 20f,
                Life = 1f,
                MaxLife = 1f,
                EntityType = 7f
            };
            entityIndex++;
        }

        // Add cooldown bar (if in cooldown mode and on cooldown)
        if (_fireRateMode == 2 && _cooldownTimer > 0 && _isGameActive && !_showWelcomeScreen && entityIndex < totalEntities)
        {
            float progress = 1f - (_cooldownTimer / _fireCooldown);
            _gpuEntities[entityIndex] = new EntityGPU
            {
                Position = _lastMousePos + new Vector2(0, 35f),
                Velocity = Vector2.Zero,
                Color = new Vector4(0f, 1f, 0.5f, 1f),
                Size = 25f,
                Life = progress,
                MaxLife = 1f,
                EntityType = 8f
            };
            entityIndex++;
        }

        // Add enemy missiles
        for (int i = 0; i < MaxEnemyMissiles && entityIndex < totalEntities; i++)
        {
            ref EnemyMissile missile = ref _enemyMissiles[i];
            if (!missile.IsActive) continue;

            Vector2 velocity = Vector2.Normalize(missile.TargetPosition - missile.Position);
            _gpuEntities[entityIndex] = new EntityGPU
            {
                Position = missile.Position,
                Velocity = velocity * 100f,
                Color = _enemyMissileColor,
                Size = missile.Size,
                Life = 1f,
                MaxLife = 1f,
                EntityType = 2f
            };
            entityIndex++;
        }

        // Add counter missiles
        for (int i = 0; i < MaxCounterMissiles && entityIndex < totalEntities; i++)
        {
            ref CounterMissile missile = ref _counterMissiles[i];
            if (!missile.IsActive) continue;

            Vector2 velocity = Vector2.Normalize(missile.TargetPosition - missile.Position);
            _gpuEntities[entityIndex] = new EntityGPU
            {
                Position = missile.Position,
                Velocity = velocity * 100f,
                Color = missile.Color,
                Size = missile.Size,
                Life = 1f,
                MaxLife = 1f,
                EntityType = 3f
            };
            entityIndex++;
        }

        // Add explosions
        for (int i = 0; i < MaxExplosions && entityIndex < totalEntities; i++)
        {
            ref Explosion exp = ref _explosions[i];
            if (!exp.IsActive) continue;

            _gpuEntities[entityIndex] = new EntityGPU
            {
                Position = exp.Position,
                Velocity = Vector2.Zero,
                Color = exp.Color,
                Size = exp.CurrentRadius,
                Life = exp.Life,
                MaxLife = exp.MaxLife,
                EntityType = 1f
            };
            entityIndex++;
        }

        // Add particles
        for (int i = 0; i < MaxParticles && entityIndex < totalEntities; i++)
        {
            ref Particle p = ref _particles[i];
            if (p.Life <= 0) continue;

            _gpuEntities[entityIndex] = new EntityGPU
            {
                Position = p.Position,
                Velocity = p.Velocity,
                Color = p.Color,
                Size = p.Size,
                Life = p.Life,
                MaxLife = p.MaxLife,
                EntityType = 0f
            };
            entityIndex++;
        }

        // Render game entities
        if (!_showWelcomeScreen && entityIndex > 0)
        {
            context.UpdateBuffer(_entityBuffer!, (ReadOnlySpan<EntityGPU>)_gpuEntities.AsSpan(0, entityIndex));
            context.SetVertexShader(_vertexShader);
            context.SetPixelShader(_pixelShader);
            context.SetConstantBuffer(ShaderStage.Vertex, 0, _frameDataBuffer!);
            context.SetConstantBuffer(ShaderStage.Pixel, 0, _frameDataBuffer!);
            context.SetShaderResource(ShaderStage.Vertex, 0, _entityBuffer!);
            context.SetBlendState(BlendMode.Additive);
            context.SetPrimitiveTopology(PrimitiveTopology.TriangleList);
            context.DrawInstanced(6, entityIndex, 0, 0);
        }

        RenderTextOverlay(context, totalTime);
    }

    private void RenderWelcomeScreen(float totalTime)
    {
        if (_textOverlay == null) return;

        float centerX = _viewportWidth / 2f;
        float centerY = _viewportHeight / 2f;

        float hue = (totalTime * 0.5f) % 1f;
        float r, g, b;
        int hi = (int)(hue * 6f) % 6;
        float f = hue * 6f - hi;
        switch (hi)
        {
            case 0: r = 1f; g = f; b = 0f; break;
            case 1: r = 1f - f; g = 1f; b = 0f; break;
            case 2: r = 0f; g = 1f; b = f; break;
            case 3: r = 0f; g = 1f - f; b = 1f; break;
            case 4: r = f; g = 0f; b = 1f; break;
            default: r = 1f; g = 0f; b = 1f - f; break;
        }

        var titleStyle = new TextStyle
        {
            Color = new Vector4(r, g, b, 1f),
            Size = _scoreOverlaySize * 2.5f,
            Spacing = _scoreOverlaySpacing,
            GlowIntensity = 2.5f,
            Animation = TextAnimation.Wave(2f, 8f, 0.3f)
        };

        var promptStyle = new TextStyle
        {
            Color = new Vector4(1f, 1f, 1f, 0.9f),
            Size = _scoreOverlaySize * 0.8f,
            Spacing = _scoreOverlaySpacing,
            GlowIntensity = 1.5f,
            Animation = TextAnimation.Pulse(2f, 0.3f)
        };

        _textOverlay.AddTextCentered("RETRO COMMAND", new Vector2(centerX, centerY - _scoreOverlaySize * 2f), titleStyle);
        _textOverlay.AddTextCentered(ClickToStartText, new Vector2(centerX, centerY + _scoreOverlaySize * 3f), promptStyle);
    }

    private void RenderTextOverlay(IRenderContext context, float totalTime)
    {
        if (_textOverlay == null) return;

        _textOverlay.BeginFrame();
        _textOverlay.Time = totalTime;

        if (_showWelcomeScreen)
        {
            RenderWelcomeScreen(totalTime);
            _textOverlay.EndFrame();
            _textOverlay.Render(context);
            return;
        }

        // Score overlay panel
        if (_showScoreOverlay && (_isGameActive || _isGameEnded))
        {
            var labelStyle = new TextStyle
            {
                Color = new Vector4(_scoreOverlayColor.X * 0.9f, _scoreOverlayColor.Y * 0.9f, _scoreOverlayColor.Z * 0.9f, 1f),
                Size = _scoreOverlaySize * 0.6f,
                Spacing = _scoreOverlaySpacing,
                GlowIntensity = 1.5f
            };

            var scoreStyle = new TextStyle
            {
                Color = _scoreOverlayColor,
                Size = _scoreOverlaySize,
                Spacing = _scoreOverlaySpacing,
                GlowIntensity = 1.8f
            };

            var ppmStyle = new TextStyle
            {
                Color = new Vector4(1f, 1f, 0.3f, 1f),
                Size = _scoreOverlaySize * 0.8f,
                Spacing = _scoreOverlaySpacing,
                GlowIntensity = 1.8f
            };

            var timerStyle = new TextStyle
            {
                Color = new Vector4(0.3f, 1f, 1f, 1f),
                Size = _scoreOverlaySize * 0.7f,
                Spacing = _scoreOverlaySpacing,
                GlowIntensity = 1.8f
            };

            var waveStyle = new TextStyle
            {
                Color = new Vector4(1f, 0.5f, 1f, 1f),
                Size = _scoreOverlaySize * 0.7f,
                Spacing = _scoreOverlaySpacing,
                GlowIntensity = 1.5f
            };

            var citiesStyle = new TextStyle
            {
                Color = CountActiveCities() <= 2 ? new Vector4(1f, 0.3f, 0.3f, 1f) : new Vector4(0.3f, 1f, 0.5f, 1f),
                Size = _scoreOverlaySize * 0.7f,
                Spacing = _scoreOverlaySpacing,
                GlowIntensity = 1.5f
            };

            _textOverlay.CreateBuilder()
                .Panel(new Vector2(_scoreOverlayX, _scoreOverlayY))
                .WithBackground(new Vector4(0.05f, 0.05f, 0.1f, 1f), _scoreOverlayBgOpacity, _scoreOverlaySize * 0.5f)
                .Line("SCORE", _score.ToString(), labelStyle, scoreStyle, 300f)
                .Line("PPM", ((int)PointsPerMinute).ToString(), labelStyle, ppmStyle, 300f)
                .Line("TIME", _waitingForFirstHit && _isGameActive ? "READY" : FormatTimer(RemainingTime), labelStyle, timerStyle, 300f)
                .Line("WAVE", _currentWave.ToString(), labelStyle, waveStyle, 300f)
                .Line("CITIES", CountActiveCities().ToString(), labelStyle, citiesStyle, 300f)
                .Build();
        }

        // Game Over text
        if (_isGameOver)
        {
            var gameOverStyle = new TextStyle
            {
                Color = new Vector4(1f, 1f, 1f, 1f),
                Size = _scoreOverlaySize * 2.5f,
                Spacing = _scoreOverlaySpacing,
                GlowIntensity = 2.0f,
                Animation = TextAnimation.Wave(2f, 8f, 0.15f)
            };

            var reasonStyle = new TextStyle
            {
                Color = new Vector4(1f, 0.3f, 0.3f, 1f),
                Size = _scoreOverlaySize * 1.2f,
                Spacing = _scoreOverlaySpacing,
                GlowIntensity = 1.5f,
                Animation = TextAnimation.Pulse(2f, 0.3f)
            };

            var restartStyle = new TextStyle
            {
                Color = new Vector4(1f, 1f, 1f, 1f),
                Size = _scoreOverlaySize * 0.8f,
                Spacing = _scoreOverlaySpacing,
                GlowIntensity = 1.5f,
                Animation = TextAnimation.Wave(2f, 5f, 0.15f)
            };

            var center = new Vector2(_viewportWidth / 2f, _viewportHeight / 2f);
            _textOverlay.AddTextCentered("GAME OVER", center, gameOverStyle);
            _textOverlay.AddTextCentered(_gameOverReason, center + new Vector2(0, _scoreOverlaySize * 4f), reasonStyle);
            _textOverlay.AddTextCentered(ClickToRestartText, center + new Vector2(0, _scoreOverlaySize * 7f), restartStyle);
        }

        // High scores (when game ends with win)
        if (_isGameEnded && !_isGameOver && _highScores.Count > 0)
        {
            RenderHighScores(totalTime);
        }

        _textOverlay.EndFrame();
        _textOverlay.Render(context);
    }

    private void RenderHighScores(float totalTime)
    {
        if (_textOverlay == null) return;

        var titleStyle = new TextStyle
        {
            Color = new Vector4(1f, 1f, 1f, 1f),
            Size = _scoreOverlaySize * 1.8f,
            Spacing = _scoreOverlaySpacing,
            GlowIntensity = 2.0f,
            Animation = TextAnimation.Wave(2f, 8f, 0.15f)
        };

        float hsCenterX = _viewportWidth / 2f;
        float hsCenterY = _viewportHeight / 2f;
        float hsEntrySize = _scoreOverlaySize * 1.0f;

        _textOverlay.AddTextCentered("HIGH SCORES", new Vector2(hsCenterX, hsCenterY - hsEntrySize * 5f), titleStyle);

        float entryY = hsCenterY - hsEntrySize * 1.5f;
        float entryLineHeight = hsEntrySize * 2.5f;

        for (int scoreIdx = 0; scoreIdx < _highScores.Count && scoreIdx < 5; scoreIdx++)
        {
            var entry = _highScores[scoreIdx];
            bool isNewScore = scoreIdx == _newHighScoreIndex;

            TextStyle entryStyle;
            if (isNewScore)
            {
                entryStyle = new TextStyle
                {
                    Color = new Vector4(1f, 1f, 1f, 1f),
                    Size = hsEntrySize,
                    Spacing = _scoreOverlaySpacing,
                    GlowIntensity = 1.5f,
                    Animation = TextAnimation.Rainbow(0.5f)
                };
            }
            else
            {
                float hsTime = totalTime * 2f;
                float bluePulse = 0.6f + 0.2f * MathF.Sin(hsTime + scoreIdx);
                entryStyle = new TextStyle
                {
                    Color = new Vector4(0.2f, 0.5f, 1f, bluePulse),
                    Size = hsEntrySize,
                    Spacing = _scoreOverlaySpacing,
                    GlowIntensity = 1.0f
                };
            }

            string entryText = $"{scoreIdx + 1}. {entry.PointsPerMinute}  {entry.Date}";
            _textOverlay.AddTextCentered(entryText, new Vector2(hsCenterX, entryY), entryStyle);

            entryY += entryLineHeight;
        }

        var restartStyle = new TextStyle
        {
            Color = new Vector4(1f, 1f, 1f, 1f),
            Size = _scoreOverlaySize * 0.8f,
            Spacing = _scoreOverlaySpacing,
            GlowIntensity = 1.5f,
            Animation = TextAnimation.Wave(2f, 5f, 0.15f)
        };
        _textOverlay.AddTextCentered(ClickToRestartText, new Vector2(hsCenterX, entryY + hsEntrySize), restartStyle);
    }

    private static string FormatTimer(float remainingSeconds)
    {
        remainingSeconds = Math.Max(0f, remainingSeconds);
        int totalSeconds = (int)remainingSeconds;
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        return $"{minutes:D2}:{seconds:D2}";
    }

    private bool IsPointInCenteredText(Vector2 point, string text, Vector2 textCenter, float fontSize)
    {
        float charWidth = fontSize * 0.7f * _scoreOverlaySpacing;
        float textWidth = text.Length * charWidth;
        float textHeight = fontSize * 1.5f;

        float left = textCenter.X - textWidth / 2f;
        float right = textCenter.X + textWidth / 2f;
        float top = textCenter.Y - textHeight / 2f;
        float bottom = textCenter.Y + textHeight / 2f;

        return point.X >= left && point.X <= right && point.Y >= top && point.Y <= bottom;
    }

    protected override void OnDispose()
    {
        _textOverlay?.Dispose();
        _entityBuffer?.Dispose();
        _frameDataBuffer?.Dispose();
        _vertexShader?.Dispose();
        _pixelShader?.Dispose();
    }

    private static string LoadEmbeddedShader(string name)
    {
        var assembly = typeof(RetroCommandEffect).Assembly;
        string resourceName = $"MouseEffects.Effects.RetroCommand.Shaders.{name}";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
