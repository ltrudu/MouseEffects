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

namespace MouseEffects.Effects.Retropede;

public sealed class RetropedeEffect : EffectBase, IHotkeyProvider
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

    private struct RetropedeSegment
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float Size;
        public bool IsHead;
        public int ChainId;
        public int SegmentIndex;
        public float AnimPhase;
        public bool IsActive;
        public bool MovingRight;
        public int RowIndex; // Current row for descent logic
    }

    private struct Mushroom
    {
        public Vector2 Position;
        public int Health; // 1-4
        public float Size;
        public bool IsActive;
        public bool IsPoisoned;
    }

    private struct Spider
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float Size;
        public float AnimPhase;
        public bool IsActive;
        public float TimeAlive;
    }

    private struct DDTBomb
    {
        public Vector2 Position;
        public float Size;
        public bool IsActive;
        public bool IsExploding;
        public float ExplosionTimer;
        public float GasRadius;
    }

    private struct Laser
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float Size;
        public bool IsActive;
    }

    private struct Cannon
    {
        public Vector2 Position;
        public float Size;
    }

    private struct Particle
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public Vector4 Color;
        public float Size;
        public float Life;
        public float MaxLife;
        public int ParticleType; // 0=explosion, 1=DDT gas, 2=trail
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
        public float EntityType; // 0=particle, 1=laser, 2=cannon, 3=head, 4=body, 5-8=mushroom, 9=spider, 10=DDT, 11=gas, 12-21=digits, 22=colon, 23-48=letters, 49=space, 50=bg rect
        public float RenderStyle; // Visual variation for entity
        public float AnimPhase;
        public float Health; // For mushrooms
        public float Padding;
    }

    [StructLayout(LayoutKind.Sequential, Size = 80)]
    private struct FrameData
    {
        public Vector2 ViewportSize;
        public float Time;
        public float RenderStyle;
        public float GlowIntensity;
        public float NeonIntensity;
        public float AnimSpeed;
        public float HdrMultiplier;
        public float PlayerZoneY;
        public float RetroScanlines;
        public float RetroPixelScale;
        public float Padding1;
        public float Padding2;
        public float Padding3;
        public float Padding4;
        public float Padding5;
        public float Padding6;
        public float Padding7;
        public float Padding8;
        public float Padding9;
    }

    private const int MaxRetropedeChains = 20;
    private const int MaxSegmentsPerChain = 50;
    private const int MaxMushrooms = 200;
    private const int MaxLasers = 10;
    private const int MaxParticles = 2000;

    private static readonly EffectMetadata _metadata = new()
    {
        Id = "retropede",
        Name = "Retropede",
        Description = "Classic arcade Retropede game with mushrooms, spiders, and DDT bombs",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Interactive
    };

    private IBuffer? _entityBuffer;
    private IBuffer? _frameDataBuffer;
    private IShader? _vertexShader;
    private IShader? _pixelShader;

    private readonly RetropedeSegment[] _retropedeSegments = new RetropedeSegment[MaxRetropedeChains * MaxSegmentsPerChain];
    private readonly Mushroom[] _mushrooms = new Mushroom[MaxMushrooms];
    private readonly Laser[] _lasers = new Laser[MaxLasers];
    private readonly Particle[] _particles = new Particle[MaxParticles];
    private readonly EntityGPU[] _gpuEntities = new EntityGPU[MaxRetropedeChains * MaxSegmentsPerChain + MaxMushrooms + MaxLasers + MaxParticles + 10]; // +10 for spider, DDT bombs, cannon

    // Text overlay for score, timer, game over, and high scores
    private TextOverlay? _textOverlay;

    private Spider _spider;
    private readonly DDTBomb[] _ddtBombs = new DDTBomb[4];
    private Cannon _cannon;

    private int _nextChainId;
    private float _spiderSpawnTimer;
    private float _ddtSpawnTimer;
    private Vector2 _lastMousePos;
    private bool _wasLeftPressed;
    private bool _wasRightPressed;
    private float _lastSpawnDistance;

    private int _score;
    private float _elapsedTime;
    private float _viewportWidth = 1920f;
    private float _viewportHeight = 1080f;

    // Game configuration
    private int _currentWave = 1;
    private bool _isGameActive = true;
    private bool _isGameEnded;
    private bool _isGameOver;
    private bool _waitingForFirstHit = true;
    private bool _showWelcomeScreen = true;
    private string _gameOverReason = "";
    private float _playerZoneHeight = 200f; // Bottom area where cannon can move

    // Clickable text constants
    private const string ClickToStartText = "CLICK HERE TO START";
    private const string ClickToRestartText = "CLICK HERE TO RESTART";

    // Laser configuration
    private bool _fireOnLeftClick = true;
    private bool _fireOnRightClick;
    private bool _fireOnMove;
    private float _moveSpawnDistance = 50f;
    private float _laserSpeed = 800f;
    private float _laserSize = 6f;
    private Vector4 _laserColor = new(0f, 1f, 0.5f, 1f);

    // Retropede configuration
    private float _retropedeSegmentSize = 20f;
    private float _retropedeSpeed = 80f;
    private float _retropedeDropDistance = 30f;
    private int _initialSegments = 12; // Configurable from 5 to 50
    private Vector4 _retropedeHeadColor = new(1f, 0.2f, 0.2f, 1f); // Red
    private Vector4 _retropedeBodyColor = new(0.8f, 0.2f, 1f, 1f); // Purple

    // Mushroom configuration
    private float _mushroomSize = 20f;
    private int _initialMushroomCount = 50;
    private float _mushroomFreeZoneHeight = 80f; // Top zone where initial mushrooms won't spawn
    private Vector4 _mushroomColor = new(0f, 1f, 0.5f, 1f);

    // Spider configuration
    private float _spiderSize = 32f;
    private float _spiderSpeed = 120f;
    private float _spiderSpawnRate = 5f;
    private Vector4 _spiderColor = new(1f, 1f, 0f, 1f); // Yellow

    // DDT configuration
    private float _ddtSize = 24f;
    private float _ddtSpawnRate = 10f;
    private float _ddtExplosionDuration = 1f;
    private float _ddtMaxGasRadius = 200f;
    private Vector4 _ddtColor = new(0f, 1f, 1f, 1f); // Cyan

    // Cannon configuration
    private float _cannonSize = 32f;
    private Vector4 _cannonColor = new(0f, 0.8f, 1f, 1f);

    // Visual configuration
    private float _glowIntensity = 1.2f;
    private float _neonIntensity = 1.0f;
    private float _animSpeed = 2.0f;
    private float _renderStyle = 0f; // 0=neon, 1=retro
    private float _retroScanlines = 1f;
    private float _retroPixelScale = 2f;

    // Scoring
    private int _scoreRetropedeHead = 100;
    private int _scoreRetropedeBody = 10;
    private int _scoreMushroom = 1;
    private int _scoreSpiderClose = 900;
    private int _scoreSpiderMedium = 600;
    private int _scoreSpiderFar = 300;

    // Score overlay
    private bool _showScoreOverlay = true;
    private float _scoreOverlaySize = 32f;
    private float _scoreOverlaySpacing = 1.5f;
    private float _scoreOverlayMargin = 20f;
    private float _scoreOverlayBgOpacity = 0.7f;
    private Vector4 _scoreOverlayColor = new(0f, 1f, 0f, 1f);
    private float _scoreOverlayX = 70f;
    private float _scoreOverlayY = 50f;

    // Timer
    private float _timerDuration = 90f;


    // High scores
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
    public bool EnableResetHotkey => true;
    public IReadOnlyList<HighScoreEntry> HighScores => _highScores;
    public int NewHighScoreIndex => _newHighScoreIndex;
    public int CurrentWave => _currentWave;

    public event Action<string>? HighScoresChanged;

    public IEnumerable<HotkeyDefinition> GetHotkeys()
    {
        yield return new HotkeyDefinition
        {
            Id = "reset",
            DisplayName = "Reset Game",
            Modifiers = HotkeyModifiers.Alt | HotkeyModifiers.Shift,
            Key = HotkeyKey.R,
            IsEnabled = true, // Always enabled
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
        _nextChainId = 0;

        // Clear all entities
        for (int i = 0; i < MaxRetropedeChains * MaxSegmentsPerChain; i++)
            _retropedeSegments[i].IsActive = false;
        for (int i = 0; i < MaxMushrooms; i++)
            _mushrooms[i].IsActive = false;
        for (int i = 0; i < MaxLasers; i++)
            _lasers[i].IsActive = false;
        for (int i = 0; i < MaxParticles; i++)
            _particles[i].Life = 0f;
        for (int i = 0; i < 4; i++)
            _ddtBombs[i].IsActive = false;

        _spider.IsActive = false;
        _spiderSpawnTimer = 0f;
        _ddtSpawnTimer = 0f;

        // Initialize cannon at center bottom
        _cannon.Position = new Vector2(_viewportWidth / 2f, _viewportHeight - 50f);
        _cannon.Size = _cannonSize;

        // Spawn initial wave
        SpawnRetropedeWave();
        SpawnInitialMushrooms();
    }

    protected override void OnInitialize(IRenderContext context)
    {
        int totalEntities = MaxRetropedeChains * MaxSegmentsPerChain + MaxMushrooms + MaxLasers + MaxParticles + 10;
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

        string shaderSource = LoadEmbeddedShader("RetropedeShader.hlsl");
        _vertexShader = context.CompileShader(shaderSource, "VSMain", ShaderStage.Vertex);
        _pixelShader = context.CompileShader(shaderSource, "PSMain", ShaderStage.Pixel);

        // Initialize text overlay
        _textOverlay = new TextOverlay();
        _textOverlay.Initialize(context);

        // Initialize arrays
        for (int i = 0; i < MaxRetropedeChains * MaxSegmentsPerChain; i++)
            _retropedeSegments[i] = new RetropedeSegment { IsActive = false };
        for (int i = 0; i < MaxMushrooms; i++)
            _mushrooms[i] = new Mushroom { IsActive = false };
        for (int i = 0; i < MaxLasers; i++)
            _lasers[i] = new Laser { IsActive = false };
        for (int i = 0; i < MaxParticles; i++)
            _particles[i] = new Particle { Life = 0f };
        for (int i = 0; i < 4; i++)
            _ddtBombs[i] = new DDTBomb { IsActive = false };

        _spider = new Spider { IsActive = false };
        _cannon.Position = new Vector2(960f, 1030f);
        _cannon.Size = _cannonSize;

        // Initialize game
        ResetGame();
    }

    protected override void OnConfigurationChanged()
    {
        // Laser settings
        if (Configuration.TryGet("mp_spawnOnLeftClick", out bool leftClick))
            _fireOnLeftClick = leftClick;
        if (Configuration.TryGet("mp_spawnOnRightClick", out bool rightClick))
            _fireOnRightClick = rightClick;
        if (Configuration.TryGet("mp_spawnOnMove", out bool fireMove))
            _fireOnMove = fireMove;
        if (Configuration.TryGet("mp_moveFireThreshold", out float moveDist))
            _moveSpawnDistance = moveDist;
        if (Configuration.TryGet("mp_laserSpeed", out float laserSpd))
            _laserSpeed = laserSpd;
        if (Configuration.TryGet("mp_laserSize", out float laserSz))
            _laserSize = laserSz;
        if (Configuration.TryGet("mp_laserColor", out Vector4 laserCol))
            _laserColor = laserCol;

        // Retropede settings
        if (Configuration.TryGet("mp_segmentSize", out float segSize))
            _retropedeSegmentSize = segSize;
        if (Configuration.TryGet("mp_baseSpeed", out float millSpeed))
            _retropedeSpeed = millSpeed;
        if (Configuration.TryGet("mp_startingSegments", out int initSegs))
            _initialSegments = Math.Clamp(initSegs, 1, MaxSegmentsPerChain);
        if (Configuration.TryGet("mp_headColor", out Vector4 headCol))
            _retropedeHeadColor = headCol;
        if (Configuration.TryGet("mp_bodyColor", out Vector4 bodyCol))
            _retropedeBodyColor = bodyCol;

        // Mushroom settings
        if (Configuration.TryGet("mp_mushroomSize", out float mushSize))
            _mushroomSize = mushSize;
        if (Configuration.TryGet("mp_initialMushroomCount", out int mushCount))
            _initialMushroomCount = Math.Clamp(mushCount, 0, MaxMushrooms);
        if (Configuration.TryGet("mp_mushroomFreeZoneHeight", out float freeZone))
            _mushroomFreeZoneHeight = freeZone;
        if (Configuration.TryGet("mp_mushroomColor", out Vector4 mushCol))
            _mushroomColor = mushCol;

        // Spider settings
        if (Configuration.TryGet("mp_spiderSize", out float spiderSz))
            _spiderSize = spiderSz;
        if (Configuration.TryGet("mp_spiderSpeed", out float spiderSpd))
            _spiderSpeed = spiderSpd;
        if (Configuration.TryGet("mp_spiderSpawnRate", out float spiderRate))
            _spiderSpawnRate = spiderRate;
        if (Configuration.TryGet("mp_spiderColor", out Vector4 spiderCol))
            _spiderColor = spiderCol;

        // DDT settings
        if (Configuration.TryGet("mp_ddtExplosionDuration", out float ddtDur))
            _ddtExplosionDuration = ddtDur;
        if (Configuration.TryGet("mp_ddtExplosionRadius", out float ddtRadius))
            _ddtMaxGasRadius = ddtRadius;
        if (Configuration.TryGet("mp_ddtColor", out Vector4 ddtCol))
            _ddtColor = ddtCol;

        // Cannon settings
        if (Configuration.TryGet("mp_cannonSize", out float cannonSz))
            _cannonSize = cannonSz;
        if (Configuration.TryGet("mp_playerZoneHeight", out float zoneHeight))
            _playerZoneHeight = zoneHeight;

        // Visual settings
        if (Configuration.TryGet("mp_glowIntensity", out float glow))
            _glowIntensity = glow;
        if (Configuration.TryGet("mp_neonIntensity", out float neon))
            _neonIntensity = neon;
        if (Configuration.TryGet("mp_animSpeed", out float anim))
            _animSpeed = anim;
        if (Configuration.TryGet("mp_renderStyle", out float style))
            _renderStyle = style;
        if (Configuration.TryGet("mp_retroScanlines", out float scanlines))
            _retroScanlines = scanlines;
        if (Configuration.TryGet("mp_retroPixelScale", out float pixelScale))
            _retroPixelScale = pixelScale;

        // Scoring
        if (Configuration.TryGet("mp_scoreHead", out int scoreHead))
            _scoreRetropedeHead = scoreHead;
        if (Configuration.TryGet("mp_scoreBody", out int scoreBody))
            _scoreRetropedeBody = scoreBody;
        if (Configuration.TryGet("mp_scoreMushroom", out int scoreMush))
            _scoreMushroom = scoreMush;
        if (Configuration.TryGet("mp_scoreSpiderClose", out int scoreSpiderC))
            _scoreSpiderClose = scoreSpiderC;
        if (Configuration.TryGet("mp_scoreSpiderMedium", out int scoreSpiderM))
            _scoreSpiderMedium = scoreSpiderM;
        if (Configuration.TryGet("mp_scoreSpiderFar", out int scoreSpiderF))
            _scoreSpiderFar = scoreSpiderF;

        // Score overlay
        if (Configuration.TryGet("mp_showScoreOverlay", out bool showOverlay))
            _showScoreOverlay = showOverlay;
        if (Configuration.TryGet("mp_scoreOverlaySize", out float overlaySize))
            _scoreOverlaySize = overlaySize;
        if (Configuration.TryGet("mp_scoreOverlayX", out float overlayX))
            _scoreOverlayX = overlayX;
        if (Configuration.TryGet("mp_scoreOverlayY", out float overlayY))
            _scoreOverlayY = overlayY;

        // Timer
        if (Configuration.TryGet("mp_timerDuration", out float timerDur))
            _timerDuration = timerDur;

        // High scores
        if (Configuration.TryGet("mp_highScoresJson", out string? highScoresJson) && !string.IsNullOrEmpty(highScoresJson))
        {
            try
            {
                var loaded = JsonSerializer.Deserialize<List<HighScoreEntry>>(highScoresJson);
                if (loaded != null && loaded.Count > 0)
                {
                    _highScores = loaded;
                }
            }
            catch
            {
                // Invalid JSON, use defaults
            }
        }

        if (_highScores.Count == 0)
        {
            _highScores = new List<HighScoreEntry>
            {
                new(2000, "20/12/2025"),
                new(1500, "20/12/2025"),
                new(1000, "20/12/2025"),
                new(500, "20/12/2025"),
                new(200, "20/12/2025")
            };
        }

        // Apply speed changes to active segments in real-time
        ApplySpeedToActiveSegments();
    }

    private void ApplySpeedToActiveSegments()
    {
        for (int i = 0; i < MaxRetropedeChains * MaxSegmentsPerChain; i++)
        {
            ref RetropedeSegment seg = ref _retropedeSegments[i];
            if (!seg.IsActive) continue;

            // Preserve direction, update speed magnitude
            float direction = seg.MovingRight ? 1f : -1f;
            seg.Velocity.X = direction * _retropedeSpeed;
        }
    }

    private void TriggerGameOver(string reason)
    {
        _isGameOver = true;
        _isGameEnded = true;
        _isGameActive = false;
        _gameOverReason = reason;

        // Clear all lasers
        for (int i = 0; i < MaxLasers; i++)
            _lasers[i].IsActive = false;
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

    public string GetHighScoresJson()
    {
        return JsonSerializer.Serialize(_highScores);
    }

    protected override void OnUpdate(GameTime gameTime, MouseState mouseState)
    {
        float dt = (float)gameTime.DeltaTime.TotalSeconds;

        // Handle welcome screen - click on text to start
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
                }
            }
            _wasLeftPressed = leftPressed;
            return; // Skip all game logic while showing welcome screen
        }

        // Handle game over/success - click on text to restart (go back to welcome screen)
        if (_isGameEnded)
        {
            bool leftPressed = mouseState.IsButtonPressed(MouseButtons.Left);
            if (leftPressed && !_wasLeftPressed)
            {
                float textSize = _scoreOverlaySize * 0.8f;
                Vector2 textPos;

                if (_isGameOver)
                {
                    // Game over screen - text below game over message
                    var center = new Vector2(_viewportWidth / 2f, _viewportHeight / 2f);
                    textPos = center + new Vector2(0, _scoreOverlaySize * 7f);
                }
                else
                {
                    // High scores screen - text below score entries
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
                    ResetGame(); // This sets _showWelcomeScreen = true
                }
            }
            _wasLeftPressed = leftPressed;
            return; // Skip game logic when ended
        }

        // Update timer if game is active and not waiting for first hit
        if (_isGameActive && !_isGameEnded && !_waitingForFirstHit)
        {
            _elapsedTime += dt;

            if (_elapsedTime >= _timerDuration)
            {
                _isGameEnded = true;
                _isGameActive = false;

                // Clear all entities
                for (int i = 0; i < MaxRetropedeChains * MaxSegmentsPerChain; i++)
                    _retropedeSegments[i].IsActive = false;
                for (int i = 0; i < MaxLasers; i++)
                    _lasers[i].IsActive = false;

                CheckAndUpdateHighScores();
            }
        }

        // Always update particles for fade out
        UpdateParticles(dt);

        // Skip game logic if game ended
        if (_isGameEnded)
            return;

        UpdateRetropedes(dt);
        UpdateMushrooms(dt);
        UpdateSpider(dt);
        UpdateDDT(dt);
        UpdateLasers(dt);
        CheckCollisions();
        CheckCannonCollision();

        // Handle cannon control and laser firing
        if (_isGameActive)
        {
            // Update cannon position to follow mouse
            _cannon.Position.X = mouseState.Position.X;
            _cannon.Position.Y = Math.Clamp(mouseState.Position.Y, _viewportHeight - _playerZoneHeight, _viewportHeight - 30f);

            bool leftPressed = mouseState.IsButtonPressed(MouseButtons.Left);
            bool rightPressed = mouseState.IsButtonPressed(MouseButtons.Right);

            if (_fireOnLeftClick && leftPressed && !_wasLeftPressed)
            {
                FireLaser();
            }

            if (_fireOnRightClick && rightPressed && !_wasRightPressed)
            {
                FireLaser();
            }

            _wasLeftPressed = leftPressed;
            _wasRightPressed = rightPressed;

            if (_fireOnMove)
            {
                float distanceFromLast = Vector2.Distance(mouseState.Position, _lastMousePos);
                _lastSpawnDistance += distanceFromLast;
                if (_lastSpawnDistance >= _moveSpawnDistance)
                {
                    FireLaser();
                    _lastSpawnDistance = 0f;
                }
            }

            _lastMousePos = mouseState.Position;

            // Spawn spider periodically
            _spiderSpawnTimer += dt;
            if (_spiderSpawnTimer >= _spiderSpawnRate && !_spider.IsActive)
            {
                SpawnSpider();
                _spiderSpawnTimer = 0f;
            }

            // Spawn DDT bombs periodically
            _ddtSpawnTimer += dt;
            if (_ddtSpawnTimer >= _ddtSpawnRate)
            {
                SpawnDDTBomb();
                _ddtSpawnTimer = 0f;
            }
        }
    }

    private void UpdateRetropedes(float dt)
    {
        for (int i = 0; i < MaxRetropedeChains * MaxSegmentsPerChain; i++)
        {
            ref RetropedeSegment seg = ref _retropedeSegments[i];
            if (!seg.IsActive) continue;

            seg.AnimPhase += _animSpeed * dt;
            if (seg.AnimPhase > MathF.PI * 2f)
                seg.AnimPhase -= MathF.PI * 2f;

            // Movement logic
            Vector2 nextPos = seg.Position + seg.Velocity * dt;
            bool shouldDrop = false;

            // Check edge collision
            float halfSize = seg.Size / 2f;
            if (nextPos.X <= halfSize || nextPos.X >= _viewportWidth - halfSize)
            {
                shouldDrop = true;
            }

            // Check mushroom collision (for all segments)
            for (int m = 0; m < MaxMushrooms; m++)
            {
                ref Mushroom mush = ref _mushrooms[m];
                if (!mush.IsActive) continue;

                float dist = Vector2.Distance(nextPos, mush.Position);
                if (dist < (seg.Size + mush.Size) / 2f)
                {
                    shouldDrop = true;
                    break;
                }
            }

            if (shouldDrop)
            {
                // Drop one row
                seg.Position.Y += _retropedeDropDistance;
                seg.RowIndex++;
                seg.MovingRight = !seg.MovingRight;
                seg.Velocity.X = -seg.Velocity.X;
            }
            else
            {
                seg.Position = nextPos;
            }

            // Check if retropede reached bottom (game over)
            if (seg.Position.Y > _viewportHeight - _playerZoneHeight)
            {
                TriggerGameOver("RETROPEDE REACHED PLAYER ZONE");
                return;
            }
        }

        // Check if all retropedes dead, spawn next wave
        bool anyActive = false;
        for (int i = 0; i < MaxRetropedeChains * MaxSegmentsPerChain; i++)
        {
            if (_retropedeSegments[i].IsActive)
            {
                anyActive = true;
                break;
            }
        }

        if (!anyActive && _isGameActive)
        {
            _currentWave++;
            SpawnRetropedeWave();
        }
    }

    private void UpdateMushrooms(float dt)
    {
        // Mushrooms are static, just maintain active state
    }

    private void UpdateSpider(float dt)
    {
        if (!_spider.IsActive) return;

        _spider.AnimPhase += _animSpeed * dt;
        _spider.TimeAlive += dt;

        // Zig-zag movement
        float zigZagFrequency = 2f;
        float zigZagAmplitude = 100f;
        _spider.Velocity.Y = MathF.Sin(_spider.TimeAlive * zigZagFrequency) * zigZagAmplitude;

        _spider.Position += _spider.Velocity * dt;

        // Check mushroom eating
        for (int i = 0; i < MaxMushrooms; i++)
        {
            ref Mushroom mush = ref _mushrooms[i];
            if (!mush.IsActive) continue;

            float dist = Vector2.Distance(_spider.Position, mush.Position);
            if (dist < (_spider.Size + mush.Size) / 2f)
            {
                mush.IsActive = false;
                SpawnExplosionParticles(mush.Position, _mushroomColor, 5);
            }
        }

        // Deactivate if off screen
        if (_spider.Position.X < -_spider.Size || _spider.Position.X > _viewportWidth + _spider.Size)
        {
            _spider.IsActive = false;
        }
    }

    private void UpdateDDT(float dt)
    {
        for (int i = 0; i < 4; i++)
        {
            ref DDTBomb ddt = ref _ddtBombs[i];
            if (!ddt.IsActive) continue;

            if (ddt.IsExploding)
            {
                ddt.ExplosionTimer += dt;
                ddt.GasRadius = (ddt.ExplosionTimer / _ddtExplosionDuration) * _ddtMaxGasRadius;

                // Spawn gas particles
                if (Random.Shared.NextSingle() > 0.7f)
                {
                    SpawnDDTGasParticle(ddt.Position, ddt.GasRadius);
                }

                // Destroy entities in gas radius
                DestroyInGasRadius(ddt.Position, ddt.GasRadius);

                if (ddt.ExplosionTimer >= _ddtExplosionDuration)
                {
                    ddt.IsActive = false;
                }
            }
        }
    }

    private void UpdateLasers(float dt)
    {
        for (int i = 0; i < MaxLasers; i++)
        {
            ref Laser laser = ref _lasers[i];
            if (!laser.IsActive) continue;

            laser.Position += laser.Velocity * dt;

            // Deactivate if off screen
            if (laser.Position.Y < -laser.Size)
            {
                laser.IsActive = false;
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

            // Apply drag and gravity based on particle type
            if (p.ParticleType == 0) // Explosion
            {
                p.Velocity *= 0.96f;
                p.Velocity.Y += 100f * dt; // Gravity
            }
            else if (p.ParticleType == 1) // DDT gas
            {
                p.Velocity *= 0.98f; // Slow drift
            }
            else if (p.ParticleType == 2) // Trail
            {
                p.Velocity *= 0.90f;
            }
        }
    }

    private void CheckCollisions()
    {
        for (int l = 0; l < MaxLasers; l++)
        {
            ref Laser laser = ref _lasers[l];
            if (!laser.IsActive) continue;

            // Check laser vs retropede
            for (int i = 0; i < MaxRetropedeChains * MaxSegmentsPerChain; i++)
            {
                ref RetropedeSegment seg = ref _retropedeSegments[i];
                if (!seg.IsActive) continue;

                float dist = Vector2.Distance(laser.Position, seg.Position);
                if (dist < (laser.Size + seg.Size) / 2f)
                {
                    // First kill starts timer
                    if (_waitingForFirstHit)
                    {
                        _waitingForFirstHit = false;
                    }

                    // Hit!
                    SpawnExplosionParticles(seg.Position, seg.IsHead ? _retropedeHeadColor : _retropedeBodyColor, 10);

                    int hitChainId = seg.ChainId;

                    if (seg.IsHead)
                    {
                        _score += _scoreRetropedeHead;
                        // Create mushroom at head position
                        CreateMushroom(seg.Position);
                        // Promote next segment to head if exists
                        PromoteNextSegmentToHead(seg.ChainId, seg.SegmentIndex);
                        seg.IsActive = false;
                        // Drop and reverse the remaining chain
                        DropAndReverseChain(hitChainId);
                    }
                    else
                    {
                        _score += _scoreRetropedeBody;
                        // Split retropede at this point (creates new chain from rear segments)
                        int newChainId = _nextChainId; // Store new chain ID before split
                        SplitRetropede(seg.ChainId, seg.SegmentIndex);
                        seg.IsActive = false;
                        // Drop and reverse both the front chain and the new rear chain
                        DropAndReverseChain(hitChainId);
                        if (_nextChainId > newChainId) // New chain was created
                            DropAndReverseChain(newChainId);
                    }
                    laser.IsActive = false;
                    break;
                }
            }

            if (!laser.IsActive) continue;

            // Check laser vs mushroom
            for (int m = 0; m < MaxMushrooms; m++)
            {
                ref Mushroom mush = ref _mushrooms[m];
                if (!mush.IsActive) continue;

                float dist = Vector2.Distance(laser.Position, mush.Position);
                if (dist < (laser.Size + mush.Size) / 2f)
                {
                    if (_waitingForFirstHit)
                    {
                        _waitingForFirstHit = false;
                    }

                    mush.Health--;
                    _score += _scoreMushroom;

                    if (mush.Health <= 0)
                    {
                        mush.IsActive = false;
                        SpawnExplosionParticles(mush.Position, _mushroomColor, 8);
                    }

                    laser.IsActive = false;
                    break;
                }
            }

            if (!laser.IsActive) continue;

            // Check laser vs spider
            if (_spider.IsActive)
            {
                float dist = Vector2.Distance(laser.Position, _spider.Position);
                if (dist < (laser.Size + _spider.Size) / 2f)
                {
                    if (_waitingForFirstHit)
                    {
                        _waitingForFirstHit = false;
                    }

                    // Score based on distance from cannon
                    float distFromCannon = Vector2.Distance(_spider.Position, _cannon.Position);
                    float screenDist = _viewportHeight;
                    if (distFromCannon < screenDist * 0.3f)
                        _score += _scoreSpiderClose;
                    else if (distFromCannon < screenDist * 0.6f)
                        _score += _scoreSpiderMedium;
                    else
                        _score += _scoreSpiderFar;

                    SpawnExplosionParticles(_spider.Position, _spiderColor, 15);
                    _spider.IsActive = false;
                    laser.IsActive = false;
                }
            }

            if (!laser.IsActive) continue;

            // Check laser vs DDT bomb
            for (int d = 0; d < 4; d++)
            {
                ref DDTBomb ddt = ref _ddtBombs[d];
                if (!ddt.IsActive || ddt.IsExploding) continue;

                float dist = Vector2.Distance(laser.Position, ddt.Position);
                if (dist < (laser.Size + ddt.Size) / 2f)
                {
                    // Trigger explosion
                    ddt.IsExploding = true;
                    ddt.ExplosionTimer = 0f;
                    ddt.GasRadius = 0f;
                    laser.IsActive = false;
                    break;
                }
            }
        }
    }

    private void CheckCannonCollision()
    {
        if (!_isGameActive || _isGameEnded) return;

        // Check retropede vs cannon
        for (int i = 0; i < MaxRetropedeChains * MaxSegmentsPerChain; i++)
        {
            ref RetropedeSegment seg = ref _retropedeSegments[i];
            if (!seg.IsActive) continue;

            float dist = Vector2.Distance(seg.Position, _cannon.Position);
            if (dist < (seg.Size + _cannon.Size) / 2f)
            {
                SpawnExplosionParticles(_cannon.Position, _cannonColor, 20);
                TriggerGameOver("CANNON DESTROYED");
                return;
            }
        }

        // Check spider vs cannon
        if (_spider.IsActive)
        {
            float dist = Vector2.Distance(_spider.Position, _cannon.Position);
            if (dist < (_spider.Size + _cannon.Size) / 2f)
            {
                SpawnExplosionParticles(_cannon.Position, _cannonColor, 20);
                TriggerGameOver("CANNON DESTROYED");
            }
        }
    }

    private void SpawnRetropedeWave()
    {
        int segmentCount = Math.Min(_initialSegments + (_currentWave - 1) * 2, MaxSegmentsPerChain);
        int chainId = _nextChainId++;

        float startX = _viewportWidth / 2f;
        float startY = 50f;
        float segmentSpacing = _retropedeSegmentSize;

        for (int i = 0; i < segmentCount; i++)
        {
            int idx = chainId * MaxSegmentsPerChain + i;
            if (idx >= MaxRetropedeChains * MaxSegmentsPerChain) break;

            ref RetropedeSegment seg = ref _retropedeSegments[idx];
            seg.Position = new Vector2(startX - i * segmentSpacing, startY);
            seg.Velocity = new Vector2(_retropedeSpeed, 0f);
            seg.Size = _retropedeSegmentSize;
            seg.IsHead = (i == 0);
            seg.ChainId = chainId;
            seg.SegmentIndex = i;
            seg.AnimPhase = Random.Shared.NextSingle() * MathF.PI * 2f;
            seg.IsActive = true;
            seg.MovingRight = true;
            seg.RowIndex = 0;
        }
    }

    private void SplitRetropede(int chainId, int hitSegmentIndex)
    {
        // Create mushroom at hit position
        Vector2 hitPos = _retropedeSegments[chainId * MaxSegmentsPerChain + hitSegmentIndex].Position;
        CreateMushroom(hitPos);

        // Check if there are segments after the hit segment
        int firstSegmentAfter = hitSegmentIndex + 1;
        bool hasSegmentsAfter = false;

        for (int i = firstSegmentAfter; i < MaxSegmentsPerChain; i++)
        {
            int idx = chainId * MaxSegmentsPerChain + i;
            if (idx >= MaxRetropedeChains * MaxSegmentsPerChain) break;
            if (_retropedeSegments[idx].IsActive && _retropedeSegments[idx].ChainId == chainId)
            {
                hasSegmentsAfter = true;
                break;
            }
        }

        if (!hasSegmentsAfter) return;

        // Create new chain from segments after hit
        int newChainId = _nextChainId++;
        if (newChainId >= MaxRetropedeChains) return;

        int newSegmentIdx = 0;
        for (int i = firstSegmentAfter; i < MaxSegmentsPerChain; i++)
        {
            int oldIdx = chainId * MaxSegmentsPerChain + i;
            if (oldIdx >= MaxRetropedeChains * MaxSegmentsPerChain) break;
            if (!_retropedeSegments[oldIdx].IsActive || _retropedeSegments[oldIdx].ChainId != chainId) continue;

            int newIdx = newChainId * MaxSegmentsPerChain + newSegmentIdx;
            if (newIdx >= MaxRetropedeChains * MaxSegmentsPerChain) break;

            ref RetropedeSegment oldSeg = ref _retropedeSegments[oldIdx];
            ref RetropedeSegment newSeg = ref _retropedeSegments[newIdx];

            newSeg = oldSeg;
            newSeg.ChainId = newChainId;
            newSeg.SegmentIndex = newSegmentIdx;
            newSeg.IsHead = (newSegmentIdx == 0);

            oldSeg.IsActive = false;
            newSegmentIdx++;
        }
    }

    private void PromoteNextSegmentToHead(int chainId, int headSegmentIndex)
    {
        // Find next segment in chain and make it head
        for (int i = headSegmentIndex + 1; i < MaxSegmentsPerChain; i++)
        {
            int idx = chainId * MaxSegmentsPerChain + i;
            if (idx >= MaxRetropedeChains * MaxSegmentsPerChain) break;

            ref RetropedeSegment seg = ref _retropedeSegments[idx];
            if (seg.IsActive && seg.ChainId == chainId)
            {
                seg.IsHead = true;
                return;
            }
        }
    }

    private void DropAndReverseChain(int chainId)
    {
        // Make all segments in a chain drop one row and reverse direction
        for (int i = 0; i < MaxSegmentsPerChain; i++)
        {
            int idx = chainId * MaxSegmentsPerChain + i;
            if (idx >= MaxRetropedeChains * MaxSegmentsPerChain) break;

            ref RetropedeSegment seg = ref _retropedeSegments[idx];
            if (seg.IsActive && seg.ChainId == chainId)
            {
                seg.Position.Y += _retropedeDropDistance;
                seg.RowIndex++;
                seg.MovingRight = !seg.MovingRight;
                seg.Velocity.X = -seg.Velocity.X;
            }
        }
    }

    private void CreateMushroom(Vector2 position)
    {
        for (int i = 0; i < MaxMushrooms; i++)
        {
            ref Mushroom mush = ref _mushrooms[i];
            if (!mush.IsActive)
            {
                mush.Position = position;
                mush.Health = 4;
                mush.Size = _mushroomSize;
                mush.IsActive = true;
                mush.IsPoisoned = false;
                return;
            }
        }
    }

    private void SpawnInitialMushrooms()
    {
        // Calculate spawn area: from mushroom free zone to player zone
        float minY = _mushroomFreeZoneHeight;
        float maxY = _viewportHeight - _playerZoneHeight;
        float spawnHeight = maxY - minY;

        for (int i = 0; i < _initialMushroomCount; i++)
        {
            Vector2 pos = new Vector2(
                Random.Shared.NextSingle() * _viewportWidth,
                Random.Shared.NextSingle() * spawnHeight + minY
            );

            CreateMushroom(pos);
        }
    }

    private void SpawnSpider()
    {
        bool fromLeft = Random.Shared.NextSingle() > 0.5f;
        float playAreaBottom = _viewportHeight - _playerZoneHeight;

        _spider.Position = new Vector2(
            fromLeft ? -_spiderSize : _viewportWidth + _spiderSize,
            Random.Shared.NextSingle() * playAreaBottom * 0.5f + playAreaBottom * 0.5f
        );
        _spider.Velocity = new Vector2(fromLeft ? _spiderSpeed : -_spiderSpeed, 0f);
        _spider.Size = _spiderSize;
        _spider.AnimPhase = 0f;
        _spider.IsActive = true;
        _spider.TimeAlive = 0f;
    }

    private void SpawnDDTBomb()
    {
        // Find inactive DDT slot
        for (int i = 0; i < 4; i++)
        {
            ref DDTBomb ddt = ref _ddtBombs[i];
            if (!ddt.IsActive)
            {
                float playAreaHeight = _viewportHeight - _playerZoneHeight;
                ddt.Position = new Vector2(
                    Random.Shared.NextSingle() * _viewportWidth,
                    Random.Shared.NextSingle() * playAreaHeight * 0.6f + 50f
                );
                ddt.Size = _ddtSize;
                ddt.IsActive = true;
                ddt.IsExploding = false;
                ddt.ExplosionTimer = 0f;
                ddt.GasRadius = 0f;
                return;
            }
        }
    }

    private void FireLaser()
    {
        for (int i = 0; i < MaxLasers; i++)
        {
            if (!_lasers[i].IsActive)
            {
                ref Laser laser = ref _lasers[i];
                laser.Position = _cannon.Position;
                laser.Velocity = new Vector2(0, -_laserSpeed);
                laser.Size = _laserSize;
                laser.IsActive = true;
                return;
            }
        }
    }

    private void DestroyInGasRadius(Vector2 center, float radius)
    {
        // Destroy retropede segments
        for (int i = 0; i < MaxRetropedeChains * MaxSegmentsPerChain; i++)
        {
            ref RetropedeSegment seg = ref _retropedeSegments[i];
            if (!seg.IsActive) continue;

            float dist = Vector2.Distance(seg.Position, center);
            if (dist < radius)
            {
                SpawnExplosionParticles(seg.Position, seg.IsHead ? _retropedeHeadColor : _retropedeBodyColor, 5);
                seg.IsActive = false;
            }
        }

        // Destroy mushrooms
        for (int i = 0; i < MaxMushrooms; i++)
        {
            ref Mushroom mush = ref _mushrooms[i];
            if (!mush.IsActive) continue;

            float dist = Vector2.Distance(mush.Position, center);
            if (dist < radius)
            {
                SpawnExplosionParticles(mush.Position, _mushroomColor, 3);
                mush.IsActive = false;
            }
        }

        // Destroy spider
        if (_spider.IsActive)
        {
            float dist = Vector2.Distance(_spider.Position, center);
            if (dist < radius)
            {
                SpawnExplosionParticles(_spider.Position, _spiderColor, 8);
                _spider.IsActive = false;
            }
        }
    }

    private void SpawnExplosionParticles(Vector2 position, Vector4 baseColor, int count)
    {
        for (int i = 0; i < count; i++)
        {
            for (int p = 0; p < MaxParticles; p++)
            {
                ref Particle particle = ref _particles[p];
                if (particle.Life <= 0f)
                {
                    float angle = Random.Shared.NextSingle() * MathF.PI * 2f;
                    float force = 150f + Random.Shared.NextSingle() * 100f;

                    particle.Position = position;
                    particle.Velocity = new Vector2(MathF.Cos(angle) * force, MathF.Sin(angle) * force);
                    particle.Color = baseColor;
                    particle.Size = 4f + Random.Shared.NextSingle() * 4f;
                    particle.Life = 0.8f + Random.Shared.NextSingle() * 0.4f;
                    particle.MaxLife = particle.Life;
                    particle.ParticleType = 0; // Explosion
                    break;
                }
            }
        }
    }

    private void SpawnDDTGasParticle(Vector2 center, float maxRadius)
    {
        for (int i = 0; i < MaxParticles; i++)
        {
            ref Particle p = ref _particles[i];
            if (p.Life <= 0f)
            {
                float angle = Random.Shared.NextSingle() * MathF.PI * 2f;
                float distance = Random.Shared.NextSingle() * maxRadius;

                p.Position = center + new Vector2(MathF.Cos(angle) * distance, MathF.Sin(angle) * distance);
                p.Velocity = new Vector2(
                    (Random.Shared.NextSingle() - 0.5f) * 20f,
                    (Random.Shared.NextSingle() - 0.5f) * 20f
                );
                p.Color = _ddtColor * 0.6f;
                p.Color.W = 0.4f;
                p.Size = 8f + Random.Shared.NextSingle() * 8f;
                p.Life = 0.5f;
                p.MaxLife = 0.5f;
                p.ParticleType = 1; // DDT gas
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
        float playerZoneY = _viewportHeight - _playerZoneHeight;

        var frameData = new FrameData
        {
            ViewportSize = context.ViewportSize,
            Time = totalTime,
            RenderStyle = _renderStyle,
            GlowIntensity = _glowIntensity,
            NeonIntensity = _neonIntensity,
            AnimSpeed = _animSpeed,
            HdrMultiplier = context.HdrPeakBrightness,
            PlayerZoneY = playerZoneY,
            RetroScanlines = _retroScanlines,
            RetroPixelScale = _retroPixelScale
        };
        context.UpdateBuffer(_frameDataBuffer!, frameData);

        int entityIndex = 0;
        int totalEntities = MaxRetropedeChains * MaxSegmentsPerChain + MaxMushrooms + MaxLasers + MaxParticles + 10;

        // Add retropede segments
        for (int i = 0; i < MaxRetropedeChains * MaxSegmentsPerChain && entityIndex < totalEntities; i++)
        {
            ref RetropedeSegment seg = ref _retropedeSegments[i];
            if (!seg.IsActive) continue;

            _gpuEntities[entityIndex] = new EntityGPU
            {
                Position = seg.Position,
                Velocity = seg.Velocity,
                Color = seg.IsHead ? _retropedeHeadColor : _retropedeBodyColor,
                Size = seg.Size,
                Life = 1f,
                MaxLife = 1f,
                EntityType = seg.IsHead ? 3f : 4f,
                RenderStyle = _renderStyle,
                AnimPhase = seg.AnimPhase,
                Health = 1f
            };
            entityIndex++;
        }

        // Add mushrooms
        for (int i = 0; i < MaxMushrooms && entityIndex < totalEntities; i++)
        {
            ref Mushroom mush = ref _mushrooms[i];
            if (!mush.IsActive) continue;

            float entityType = 5f + (4 - mush.Health); // 5-8 for health 4-1
            _gpuEntities[entityIndex] = new EntityGPU
            {
                Position = mush.Position,
                Velocity = Vector2.Zero,
                Color = _mushroomColor,
                Size = mush.Size,
                Life = 1f,
                MaxLife = 1f,
                EntityType = entityType,
                RenderStyle = _renderStyle,
                AnimPhase = 0f,
                Health = mush.Health
            };
            entityIndex++;
        }

        // Add spider
        if (_spider.IsActive && entityIndex < totalEntities)
        {
            _gpuEntities[entityIndex] = new EntityGPU
            {
                Position = _spider.Position,
                Velocity = _spider.Velocity,
                Color = _spiderColor,
                Size = _spider.Size,
                Life = 1f,
                MaxLife = 1f,
                EntityType = 9f,
                RenderStyle = _renderStyle,
                AnimPhase = _spider.AnimPhase,
                Health = 1f
            };
            entityIndex++;
        }

        // Add DDT bombs
        for (int i = 0; i < 4 && entityIndex < totalEntities; i++)
        {
            ref DDTBomb ddt = ref _ddtBombs[i];
            if (!ddt.IsActive) continue;

            if (ddt.IsExploding)
            {
                // Render as gas cloud (entity type 11)
                _gpuEntities[entityIndex] = new EntityGPU
                {
                    Position = ddt.Position,
                    Velocity = new Vector2(ddt.GasRadius, 0f),
                    Color = _ddtColor * 0.5f,
                    Size = ddt.GasRadius * 2f,
                    Life = ddt.ExplosionTimer / _ddtExplosionDuration,
                    MaxLife = 1f,
                    EntityType = 11f,
                    RenderStyle = _renderStyle,
                    AnimPhase = 0f,
                    Health = 1f
                };
            }
            else
            {
                // Render as DDT bomb (entity type 10)
                _gpuEntities[entityIndex] = new EntityGPU
                {
                    Position = ddt.Position,
                    Velocity = Vector2.Zero,
                    Color = _ddtColor,
                    Size = ddt.Size,
                    Life = 1f,
                    MaxLife = 1f,
                    EntityType = 10f,
                    RenderStyle = _renderStyle,
                    AnimPhase = 0f,
                    Health = 1f
                };
            }
            entityIndex++;
        }

        // Add lasers
        for (int i = 0; i < MaxLasers && entityIndex < totalEntities; i++)
        {
            ref Laser laser = ref _lasers[i];
            if (!laser.IsActive) continue;

            _gpuEntities[entityIndex] = new EntityGPU
            {
                Position = laser.Position,
                Velocity = laser.Velocity,
                Color = _laserColor,
                Size = laser.Size,
                Life = 1f,
                MaxLife = 1f,
                EntityType = 1f,
                RenderStyle = _renderStyle,
                AnimPhase = 0f,
                Health = 1f
            };
            entityIndex++;
        }

        // Add cannon
        if (entityIndex < totalEntities)
        {
            _gpuEntities[entityIndex] = new EntityGPU
            {
                Position = _cannon.Position,
                Velocity = Vector2.Zero,
                Color = _cannonColor,
                Size = _cannon.Size,
                Life = 1f,
                MaxLife = 1f,
                EntityType = 2f,
                RenderStyle = _renderStyle,
                AnimPhase = 0f,
                Health = 1f
            };
            entityIndex++;
        }

        // Add particles
        for (int i = 0; i < MaxParticles && entityIndex < totalEntities; i++)
        {
            ref Particle p = ref _particles[i];
            if (p.Life <= 0f) continue;

            _gpuEntities[entityIndex] = new EntityGPU
            {
                Position = p.Position,
                Velocity = p.Velocity,
                Color = p.Color,
                Size = p.Size,
                Life = p.Life,
                MaxLife = p.MaxLife,
                EntityType = 0f,
                RenderStyle = p.ParticleType,
                AnimPhase = 0f,
                Health = 1f
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

        // Render text overlay using centralized TextOverlay system
        RenderTextOverlay(context, totalTime);
    }

    private void RenderWelcomeScreen(float totalTime)
    {
        if (_textOverlay == null) return;

        float centerX = _viewportWidth / 2f;
        float centerY = _viewportHeight / 2f;

        // Compute rainbow color from time (HSV to RGB)
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

        // Title: Large wave animated with rainbow color
        var titleStyle = new TextStyle
        {
            Color = new Vector4(r, g, b, 1f),
            Size = _scoreOverlaySize * 4f,  // 4x larger than normal
            Spacing = _scoreOverlaySpacing,
            GlowIntensity = 2.5f,
            Animation = TextAnimation.Wave(2f, 8f, 0.3f)
        };

        // Prompt: Pulsing white
        var promptStyle = new TextStyle
        {
            Color = new Vector4(1f, 1f, 1f, 0.9f),
            Size = _scoreOverlaySize * 0.8f,
            Spacing = _scoreOverlaySpacing,
            GlowIntensity = 1.5f,
            Animation = TextAnimation.Pulse(2f, 0.3f)
        };

        _textOverlay.AddTextCentered("RETROPEDE", new Vector2(centerX, centerY - _scoreOverlaySize * 2f), titleStyle);
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
            // Define styles matching the original neon look
            var labelStyle = new TextStyle
            {
                Color = new Vector4(_scoreOverlayColor.X * 0.7f, _scoreOverlayColor.Y * 0.7f, _scoreOverlayColor.Z * 0.7f, 1f),
                Size = _scoreOverlaySize * 0.6f,
                Spacing = _scoreOverlaySpacing,
                GlowIntensity = 1.0f
            };

            var scoreStyle = new TextStyle
            {
                Color = _scoreOverlayColor,
                Size = _scoreOverlaySize,
                Spacing = _scoreOverlaySpacing,
                GlowIntensity = 1.2f
            };

            var ppmLabelStyle = new TextStyle
            {
                Color = new Vector4(1f, 1f, 0f, 0.7f),
                Size = _scoreOverlaySize * 0.6f * 0.8f,
                Spacing = _scoreOverlaySpacing,
                GlowIntensity = 1.0f
            };

            var ppmValueStyle = new TextStyle
            {
                Color = new Vector4(1f, 1f, 0f, 1f),
                Size = _scoreOverlaySize * 0.8f,
                Spacing = _scoreOverlaySpacing,
                GlowIntensity = 1.2f
            };

            var timerLabelStyle = new TextStyle
            {
                Color = new Vector4(0f, 0.8f, 1f, 0.7f),
                Size = _scoreOverlaySize * 0.6f * 0.8f,
                Spacing = _scoreOverlaySpacing,
                GlowIntensity = 1.0f
            };

            var timerValueStyle = new TextStyle
            {
                Color = new Vector4(0f, 0.8f, 1f, 1f),
                Size = _scoreOverlaySize * 0.7f,
                Spacing = _scoreOverlaySpacing,
                GlowIntensity = 1.2f
            };

            // Build the score panel
            _textOverlay.CreateBuilder()
                .Panel(new Vector2(_scoreOverlayX, _scoreOverlayY))
                .WithBackground(new Vector4(0.05f, 0.05f, 0.1f, 1f), _scoreOverlayBgOpacity, _scoreOverlaySize * 0.5f)
                .Line("SCORE", _score.ToString(), labelStyle, scoreStyle, 300f)
                .Line("POINTS/MIN", ((int)PointsPerMinute).ToString(), ppmLabelStyle, ppmValueStyle, 300f)
                .Line("COUNTDOWN", _waitingForFirstHit && _isGameActive ? "READY" : FormatTimer(RemainingTime), timerLabelStyle, timerValueStyle, 300f)
                .Build();
        }

        // Game Over text
        if (_isGameOver)
        {
            float pulseTime = _elapsedTime * 3f;
            float glowPulse = 0.6f + 0.4f * MathF.Sin(pulseTime);
            float colorShift = MathF.Sin(pulseTime * 0.7f) * 0.15f;

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
                Color = new Vector4(1f, 0.5f, 0.3f, glowPulse * 0.8f),
                Size = _scoreOverlaySize * 1.2f,
                Spacing = _scoreOverlaySpacing,
                GlowIntensity = 1.5f
            };

            // Restart text with wave animation and rainbow colors (shader handles rainbow)
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

        // High scores display (when game ends with win)
        if (_isGameEnded && !_isGameOver && _highScores.Count > 0)
        {
            RenderHighScoresText(totalTime);
        }

        _textOverlay.EndFrame();
        _textOverlay.Render(context);
    }

    private void RenderHighScoresText(float totalTime)
    {
        if (_textOverlay == null) return;

        float hsTime = totalTime * 2f;
        float titlePulse = 0.7f + 0.3f * MathF.Sin(hsTime);

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

        // Title
        _textOverlay.AddTextCentered("HIGH SCORES", new Vector2(hsCenterX, hsCenterY - hsEntrySize * 5f), titleStyle);

        // Entries
        float entryY = hsCenterY - hsEntrySize * 1.5f;
        float entryLineHeight = hsEntrySize * 2.5f;

        for (int scoreIdx = 0; scoreIdx < _highScores.Count && scoreIdx < 5; scoreIdx++)
        {
            var entry = _highScores[scoreIdx];
            bool isNewScore = scoreIdx == _newHighScoreIndex;

            TextStyle entryStyle;
            if (isNewScore)
            {
                // Rainbow cycling for new high score
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

        // Restart instruction with wave animation and rainbow colors
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
        // Estimate text dimensions based on character count and font size
        // Each character is roughly 0.7 * fontSize wide with spacing
        float charWidth = fontSize * 0.7f * _scoreOverlaySpacing;
        float textWidth = text.Length * charWidth;
        float textHeight = fontSize * 1.5f; // Add some vertical padding

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
        var assembly = typeof(RetropedeEffect).Assembly;
        string resourceName = $"MouseEffects.Effects.Retropede.Shaders.{name}";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
