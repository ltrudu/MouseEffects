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

namespace MouseEffects.Effects.Invaders;

public sealed class InvadersEffect : EffectBase, IHotkeyProvider
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

    // Invader types matching classic Retro Invaders
    private enum InvaderType
    {
        Small = 0,   // Squid - top row, 200 points
        Medium = 1,  // Crab - middle rows, 100 points
        Big = 2      // Octopus - bottom row, 50 points
    }

    private struct Invader
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public InvaderType Type;
        public float Size;
        public float AnimPhase;
        public float Health;
        public bool IsActive;
    }

    private struct Rocket
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public Vector4 Color;
        public float Size;
        public float Age;
        public bool IsActive;
    }

    private struct ExplosionParticle
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public Vector4 Color;
        public float Size;
        public float Life;
        public float MaxLife;
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
        public float EntityType; // 0=particle, 1=rocket, 2=invader small, 3=invader medium, 4=invader big
    }

    [StructLayout(LayoutKind.Sequential, Size = 64)]
    private struct FrameData
    {
        public Vector2 ViewportSize;
        public float Time;
        public float RenderStyle;      // 0=Modern, 1=Retro
        public float GlowIntensity;
        public float EnableTrails;
        public float TrailLength;
        public float NeonIntensity;
        public float AnimSpeed;
        public float HdrMultiplier;
        public float Padding2;
        public float Padding3;
        public float Padding4;
        public float Padding5;
        public float Padding6;
        public float Padding7;
    }

    private const int MaxInvaders = 100;
    private const int MaxRockets = 50;
    private const int MaxExplosionParticles = 2000;

    private static readonly EffectMetadata _metadata = new()
    {
        Id = "invaders",
        Name = "Retro Invaders",
        Description = "Defend against waves of neon retro invaders with rockets from your cursor",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Interactive
    };

    private IBuffer? _entityBuffer;
    private IBuffer? _frameDataBuffer;
    private IShader? _vertexShader;
    private IShader? _pixelShader;

    private readonly Invader[] _invaders = new Invader[MaxInvaders];
    private readonly Rocket[] _rockets = new Rocket[MaxRockets];
    private readonly ExplosionParticle[] _explosions = new ExplosionParticle[MaxExplosionParticles];
    private readonly EntityGPU[] _gpuEntities = new EntityGPU[MaxInvaders + MaxRockets + MaxExplosionParticles];

    // Text overlay for score, timer, game over, and high scores
    private TextOverlay? _textOverlay;

    private Vector2 _lastMousePos;
    private float _lastSpawnDistance;
    private bool _wasLeftPressed;
    private bool _wasRightPressed;
    private float _invaderSpawnTimer;
    private int _score;
    private float _totalTime;
    private float _viewportWidth = 1920f;
    private float _viewportHeight = 1080f;

    // Rocket configuration (similar to Firework)
    private bool _spawnOnLeftClick = true;
    private bool _spawnOnRightClick;
    private bool _spawnOnMove;
    private float _moveSpawnDistance = 80f;
    private float _rocketSpeed = 600f;
    private float _rocketSize = 8f;
    private bool _rocketRainbowMode = true;
    private float _rocketRainbowSpeed = 0.5f;
    private Vector4 _rocketColor = new(0f, 1f, 0.5f, 1f); // Neon green like classic

    // Invader configuration
    private float _invaderSpawnRate = 1.5f; // seconds between spawns
    private float _invaderMinSpeed = 50f;
    private float _invaderMaxSpeed = 150f;
    private float _invaderBigSize = 48f;
    private float _invaderMediumSizePercent = 0.5f; // 50% of big
    private float _invaderSmallSizePercent = 0.25f; // 25% of big (changed from 15% for visibility)
    private int _maxActiveInvaders = 20;
    private float _invaderDescentSpeed = 30f;
    private Vector4 _invaderSmallColor = new(1f, 0.2f, 0.8f, 1f);  // Magenta
    private Vector4 _invaderMediumColor = new(0.2f, 0.8f, 1f, 1f); // Cyan
    private Vector4 _invaderBigColor = new(0.2f, 1f, 0.4f, 1f);    // Green

    // Explosion configuration
    private int _explosionParticleCount = 30;
    private float _explosionForce = 200f;
    private float _explosionLifespan = 1.0f;
    private float _explosionParticleSize = 6f;
    private float _explosionGlowIntensity = 1.5f;

    // Visual configuration
    private int _renderStyle = 0; // 0=Modern, 1=Retro
    private float _glowIntensity = 1.2f;
    private float _neonIntensity = 1.0f;
    private bool _enableTrails = true;
    private float _trailLength = 0.4f;
    private float _animSpeed = 2.0f;

    // Scoring
    private int _scoreSmall = 200;
    private int _scoreMedium = 100;
    private int _scoreBig = 50;

    // Score overlay
    private bool _showScoreOverlay = true;
    private float _scoreOverlaySize = 32f;
    private float _scoreOverlaySpacing = 1.5f; // Spacing multiplier between digits
    private float _scoreOverlayMargin = 20f; // Margin between labels and values
    private float _scoreOverlayBgOpacity = 0.7f; // Background opacity
    private Vector4 _scoreOverlayColor = new(0f, 1f, 0f, 1f); // Green
    private float _scoreOverlayX = 70f;
    private float _scoreOverlayY = 50f;

    // Timer and game state
    private float _timerDuration = 90f; // Default 1.5 minute
    private float _elapsedTime;
    private bool _isGameActive;
    private bool _isGameEnded;
    private bool _waitingForFirstHit = true; // Timer starts on first kill
    private bool _isGameOver; // True if ended due to collision (not timer)
    private string _gameOverReason = "";
    private bool _showWelcomeScreen = true;

    // Clickable text constants
    private const string ClickToStartText = "CLICK HERE TO START";
    private const string ClickToRestartText = "CLICK HERE TO RESTART";

    // Reset hotkey
    private bool _enableResetHotkey;

    // High scores
    private List<HighScoreEntry> _highScores = new();
    private int _newHighScoreIndex = -1; // Index of just-added score, -1 if none
    private bool _highScoresSaved;

    private float _rainbowHue;

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

    /// <summary>
    /// Event raised when high scores are updated and should be saved.
    /// </summary>
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
        _isGameActive = true;
        _isGameEnded = false;
        _isGameOver = false;
        _gameOverReason = "";
        _waitingForFirstHit = true; // Timer starts on first kill
        _newHighScoreIndex = -1;
        _highScoresSaved = false;

        // Clear all invaders
        for (int i = 0; i < MaxInvaders; i++)
            _invaders[i].IsActive = false;

        // Clear all rockets
        for (int i = 0; i < MaxRockets; i++)
            _rockets[i].IsActive = false;

        // Clear all explosions
        for (int i = 0; i < MaxExplosionParticles; i++)
            _explosions[i].Life = 0f;

        _invaderSpawnTimer = 0f;
    }

    protected override void OnInitialize(IRenderContext context)
    {
        int totalEntities = MaxInvaders + MaxRockets + MaxExplosionParticles;
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

        string shaderSource = LoadEmbeddedShader("InvadersShader.hlsl");
        _vertexShader = context.CompileShader(shaderSource, "VSMain", ShaderStage.Vertex);
        _pixelShader = context.CompileShader(shaderSource, "PSMain", ShaderStage.Pixel);

        // Initialize text overlay
        _textOverlay = new TextOverlay();
        _textOverlay.Initialize(context);

        // Initialize arrays
        for (int i = 0; i < MaxInvaders; i++)
            _invaders[i] = new Invader { IsActive = false };

        for (int i = 0; i < MaxRockets; i++)
            _rockets[i] = new Rocket { IsActive = false };

        for (int i = 0; i < MaxExplosionParticles; i++)
            _explosions[i] = new ExplosionParticle { Life = 0f };
    }

    protected override void OnConfigurationChanged()
    {
        // Rocket settings
        if (Configuration.TryGet("spawnOnLeftClick", out bool leftClick))
            _spawnOnLeftClick = leftClick;
        if (Configuration.TryGet("spawnOnRightClick", out bool rightClick))
            _spawnOnRightClick = rightClick;
        if (Configuration.TryGet("spawnOnMove", out bool spawnMove))
            _spawnOnMove = spawnMove;
        if (Configuration.TryGet("moveSpawnDistance", out float moveDist))
            _moveSpawnDistance = moveDist;
        if (Configuration.TryGet("rocketSpeed", out float rocketSpd))
            _rocketSpeed = rocketSpd;
        if (Configuration.TryGet("rocketSize", out float rocketSz))
            _rocketSize = rocketSz;
        if (Configuration.TryGet("rocketRainbowMode", out bool rocketRainbow))
            _rocketRainbowMode = rocketRainbow;
        if (Configuration.TryGet("rocketRainbowSpeed", out float rocketRainbowSpd))
            _rocketRainbowSpeed = rocketRainbowSpd;
        if (Configuration.TryGet("rocketColor", out Vector4 rocketCol))
            _rocketColor = rocketCol;

        // Invader settings
        if (Configuration.TryGet("invaderSpawnRate", out float spawnRate))
            _invaderSpawnRate = spawnRate;
        if (Configuration.TryGet("invaderMinSpeed", out float minSpd))
            _invaderMinSpeed = minSpd;
        if (Configuration.TryGet("invaderMaxSpeed", out float maxSpd))
            _invaderMaxSpeed = maxSpd;
        if (Configuration.TryGet("invaderBigSize", out float bigSize))
            _invaderBigSize = bigSize;
        if (Configuration.TryGet("invaderMediumSizePercent", out float medPct))
            _invaderMediumSizePercent = medPct;
        if (Configuration.TryGet("invaderSmallSizePercent", out float smallPct))
            _invaderSmallSizePercent = smallPct;
        if (Configuration.TryGet("maxActiveInvaders", out int maxInv))
            _maxActiveInvaders = Math.Clamp(maxInv, 1, MaxInvaders);
        if (Configuration.TryGet("invaderDescentSpeed", out float descent))
            _invaderDescentSpeed = descent;
        if (Configuration.TryGet("invaderSmallColor", out Vector4 smallCol))
            _invaderSmallColor = smallCol;
        if (Configuration.TryGet("invaderMediumColor", out Vector4 medCol))
            _invaderMediumColor = medCol;
        if (Configuration.TryGet("invaderBigColor", out Vector4 bigCol))
            _invaderBigColor = bigCol;

        // Explosion settings
        if (Configuration.TryGet("explosionParticleCount", out int expCount))
            _explosionParticleCount = expCount;
        if (Configuration.TryGet("explosionForce", out float expForce))
            _explosionForce = expForce;
        if (Configuration.TryGet("explosionLifespan", out float expLife))
            _explosionLifespan = expLife;
        if (Configuration.TryGet("explosionParticleSize", out float expSize))
            _explosionParticleSize = expSize;
        if (Configuration.TryGet("explosionGlowIntensity", out float expGlow))
            _explosionGlowIntensity = expGlow;

        // Visual settings
        if (Configuration.TryGet("renderStyle", out int style))
            _renderStyle = style;
        if (Configuration.TryGet("glowIntensity", out float glow))
            _glowIntensity = glow;
        if (Configuration.TryGet("neonIntensity", out float neon))
            _neonIntensity = neon;
        if (Configuration.TryGet("enableTrails", out bool trails))
            _enableTrails = trails;
        if (Configuration.TryGet("trailLength", out float trailLen))
            _trailLength = trailLen;
        if (Configuration.TryGet("animSpeed", out float anim))
            _animSpeed = anim;

        // Scoring
        if (Configuration.TryGet("scoreSmall", out int scoreS))
            _scoreSmall = scoreS;
        if (Configuration.TryGet("scoreMedium", out int scoreM))
            _scoreMedium = scoreM;
        if (Configuration.TryGet("scoreBig", out int scoreB))
            _scoreBig = scoreB;

        // Score overlay
        if (Configuration.TryGet("showScoreOverlay", out bool showOverlay))
            _showScoreOverlay = showOverlay;
        if (Configuration.TryGet("scoreOverlaySize", out float overlaySize))
            _scoreOverlaySize = overlaySize;
        if (Configuration.TryGet("scoreOverlaySpacing", out float overlaySpacing))
            _scoreOverlaySpacing = overlaySpacing;
        if (Configuration.TryGet("scoreOverlayMargin", out float overlayMargin))
            _scoreOverlayMargin = overlayMargin;
        if (Configuration.TryGet("scoreOverlayBgOpacity", out float bgOpacity))
            _scoreOverlayBgOpacity = bgOpacity;
        if (Configuration.TryGet("scoreOverlayColor", out Vector4 overlayColor))
            _scoreOverlayColor = overlayColor;
        if (Configuration.TryGet("scoreOverlayX", out float overlayX))
            _scoreOverlayX = overlayX;
        if (Configuration.TryGet("scoreOverlayY", out float overlayY))
            _scoreOverlayY = overlayY;

        // Timer
        if (Configuration.TryGet("timerDuration", out float timerDur))
            _timerDuration = timerDur;

        // Reset hotkey
        if (Configuration.TryGet("enableResetHotkey", out bool resetHotkey))
            _enableResetHotkey = resetHotkey;

        // High scores
        if (Configuration.TryGet("highScoresJson", out string? highScoresJson) && !string.IsNullOrEmpty(highScoresJson))
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

        // Initialize with defaults if empty
        if (_highScores.Count == 0)
        {
            _highScores = new List<HighScoreEntry>
            {
                new(2000, "04/12/2025"),
                new(1500, "04/12/2025"),
                new(1000, "04/12/2025"),
                new(500, "04/12/2025"),
                new(200, "04/12/2025")
            };
        }
    }

    private void TriggerGameOver(string reason)
    {
        _isGameOver = true;
        _isGameEnded = true;
        _isGameActive = false;
        _gameOverReason = reason;

        // Clear all rockets but keep invaders visible for the "death" moment
        for (int i = 0; i < MaxRockets; i++)
            _rockets[i].IsActive = false;
    }

    private void CheckAndUpdateHighScores()
    {
        if (_highScoresSaved) return;
        _highScoresSaved = true;

        int currentPpm = (int)PointsPerMinute;
        string today = DateTime.Now.ToString("dd/MM/yyyy");

        // Find insertion position (list is sorted descending)
        int insertIndex = -1;
        for (int i = 0; i < _highScores.Count; i++)
        {
            if (currentPpm > _highScores[i].PointsPerMinute)
            {
                insertIndex = i;
                break;
            }
        }

        // If better than at least one score, or list has less than 5 entries
        if (insertIndex >= 0)
        {
            _highScores.Insert(insertIndex, new HighScoreEntry(currentPpm, today));
            // Keep only top 5
            while (_highScores.Count > 5)
                _highScores.RemoveAt(_highScores.Count - 1);
            _newHighScoreIndex = insertIndex;

            // Serialize and notify for save
            string json = JsonSerializer.Serialize(_highScores);
            HighScoresChanged?.Invoke(json);
        }
        else if (_highScores.Count < 5)
        {
            // List not full, add at the end
            _highScores.Add(new HighScoreEntry(currentPpm, today));
            _newHighScoreIndex = _highScores.Count - 1;

            // Serialize and notify for save
            string json = JsonSerializer.Serialize(_highScores);
            HighScoresChanged?.Invoke(json);
        }
        else
        {
            _newHighScoreIndex = -1;
        }
    }

    /// <summary>
    /// Gets the current high scores as JSON for saving.
    /// </summary>
    public string GetHighScoresJson()
    {
        return JsonSerializer.Serialize(_highScores);
    }

    protected override void OnUpdate(GameTime gameTime, MouseState mouseState)
    {
        float dt = (float)gameTime.DeltaTime.TotalSeconds;
        _totalTime = (float)gameTime.TotalTime.TotalSeconds;

        if (_rocketRainbowMode)
        {
            _rainbowHue += _rocketRainbowSpeed * dt;
            if (_rainbowHue > 1f) _rainbowHue -= 1f;
        }

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
                    _isGameActive = true; // Start the game when welcome screen is dismissed
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
                    // High scores screen - text below score entries (like Retropede)
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

            // Check if timer reached duration
            if (_elapsedTime >= _timerDuration)
            {
                _isGameEnded = true;
                _isGameActive = false;

                // Clear all invaders and rockets
                for (int i = 0; i < MaxInvaders; i++)
                    _invaders[i].IsActive = false;
                for (int i = 0; i < MaxRockets; i++)
                    _rockets[i].IsActive = false;

                // Check and update high scores (only on win, not game over)
                CheckAndUpdateHighScores();
            }
        }

        // Only update explosions (for fade out) when game ended
        UpdateExplosions(dt);

        // Skip game logic if game ended
        if (_isGameEnded)
            return;

        // Update game entities
        UpdateInvaders(dt);
        UpdateRockets(dt);
        CheckCollisions();
        CheckMouseCollision(mouseState.Position);

        // Handle input for rocket spawning (only when game is active)
        if (_isGameActive)
        {
            bool leftPressed = mouseState.IsButtonPressed(MouseButtons.Left);
            bool rightPressed = mouseState.IsButtonPressed(MouseButtons.Right);

            if (_spawnOnLeftClick && leftPressed && !_wasLeftPressed)
            {
                SpawnRocket(mouseState.Position);
            }

            if (_spawnOnRightClick && rightPressed && !_wasRightPressed)
            {
                SpawnRocket(mouseState.Position);
            }

            _wasLeftPressed = leftPressed;
            _wasRightPressed = rightPressed;

            if (_spawnOnMove)
            {
                float distanceFromLast = Vector2.Distance(mouseState.Position, _lastMousePos);
                _lastSpawnDistance += distanceFromLast;
                if (_lastSpawnDistance >= _moveSpawnDistance)
                {
                    SpawnRocket(mouseState.Position);
                    _lastSpawnDistance = 0f;
                }
            }

            _lastMousePos = mouseState.Position;

            // Spawn invaders periodically
            _invaderSpawnTimer += dt;
            if (_invaderSpawnTimer >= _invaderSpawnRate)
            {
                SpawnInvader();
                _invaderSpawnTimer = 0f;
            }
        }
    }

    private void UpdateInvaders(float dt)
    {
        for (int i = 0; i < MaxInvaders; i++)
        {
            ref Invader inv = ref _invaders[i];
            if (!inv.IsActive) continue;

            // Update animation phase
            inv.AnimPhase += _animSpeed * dt;
            if (inv.AnimPhase > MathF.PI * 2f)
                inv.AnimPhase -= MathF.PI * 2f;

            // Move horizontally and slowly descend
            inv.Position += inv.Velocity * dt;
            inv.Position.Y += _invaderDescentSpeed * dt;

            // Bounce off screen edges
            float halfSize = inv.Size / 2f;
            if (inv.Position.X <= halfSize || inv.Position.X >= _viewportWidth - halfSize)
            {
                inv.Velocity.X = -inv.Velocity.X;
                inv.Position.X = Math.Clamp(inv.Position.X, halfSize, _viewportWidth - halfSize);
            }

            // Game over if invader reaches bottom of screen
            if (inv.Position.Y > _viewportHeight - inv.Size / 2f)
            {
                TriggerGameOver("INVADED");
                return;
            }
        }
    }

    private void CheckMouseCollision(Vector2 mousePos)
    {
        if (!_isGameActive || _isGameEnded) return;

        for (int i = 0; i < MaxInvaders; i++)
        {
            ref Invader inv = ref _invaders[i];
            if (!inv.IsActive) continue;

            // Simple circle collision between mouse and invader
            float collisionDist = inv.Size / 2f;
            float actualDist = Vector2.Distance(mousePos, inv.Position);

            if (actualDist < collisionDist)
            {
                // Create explosion at mouse position
                SpawnExplosion(mousePos, new Vector4(1f, 0.2f, 0.2f, 1f), inv.Size);
                TriggerGameOver("TOUCHED");
                return;
            }
        }
    }

    private void UpdateRockets(float dt)
    {
        for (int i = 0; i < MaxRockets; i++)
        {
            ref Rocket rocket = ref _rockets[i];
            if (!rocket.IsActive) continue;

            rocket.Age += dt;
            rocket.Position += rocket.Velocity * dt;

            // Spawn trail particles
            if (_enableTrails && Random.Shared.NextSingle() > 0.5f)
            {
                SpawnTrailParticle(rocket.Position, rocket.Color);
            }

            // Deactivate if off top of screen
            if (rocket.Position.Y < -rocket.Size)
            {
                rocket.IsActive = false;
            }
        }
    }

    private void UpdateExplosions(float dt)
    {
        for (int i = 0; i < MaxExplosionParticles; i++)
        {
            ref ExplosionParticle p = ref _explosions[i];
            if (p.Life <= 0f) continue;

            p.Life -= dt;
            if (p.Life <= 0f) continue;

            p.Position += p.Velocity * dt;
            p.Velocity *= 0.96f; // Drag
            p.Velocity.Y += 100f * dt; // Gravity
        }
    }

    private void CheckCollisions()
    {
        for (int r = 0; r < MaxRockets; r++)
        {
            ref Rocket rocket = ref _rockets[r];
            if (!rocket.IsActive) continue;

            for (int i = 0; i < MaxInvaders; i++)
            {
                ref Invader inv = ref _invaders[i];
                if (!inv.IsActive) continue;

                // Simple circle collision
                float collisionDist = (rocket.Size + inv.Size) / 2f;
                float actualDist = Vector2.Distance(rocket.Position, inv.Position);

                if (actualDist < collisionDist)
                {
                    // First kill starts the timer
                    if (_waitingForFirstHit)
                    {
                        _waitingForFirstHit = false;
                    }

                    // Hit! Create explosion
                    Vector4 explosionColor = GetInvaderColor(inv.Type);
                    SpawnExplosion(inv.Position, explosionColor, inv.Size);

                    // Add score based on invader type
                    _score += inv.Type switch
                    {
                        InvaderType.Small => _scoreSmall,
                        InvaderType.Medium => _scoreMedium,
                        InvaderType.Big => _scoreBig,
                        _ => 50
                    };

                    // Deactivate both
                    rocket.IsActive = false;
                    inv.IsActive = false;
                    break;
                }
            }
        }
    }

    private void SpawnRocket(Vector2 position)
    {
        for (int i = 0; i < MaxRockets; i++)
        {
            if (!_rockets[i].IsActive)
            {
                ref Rocket rocket = ref _rockets[i];
                rocket.Position = position;
                rocket.Velocity = new Vector2(0, -_rocketSpeed);
                rocket.Color = GetRocketColor();
                rocket.Size = _rocketSize;
                rocket.Age = 0f;
                rocket.IsActive = true;
                break;
            }
        }
    }

    private void SpawnInvader()
    {
        // Count active invaders
        int activeCount = 0;
        for (int i = 0; i < MaxInvaders; i++)
        {
            if (_invaders[i].IsActive) activeCount++;
        }

        if (activeCount >= _maxActiveInvaders) return;

        for (int i = 0; i < MaxInvaders; i++)
        {
            if (!_invaders[i].IsActive)
            {
                ref Invader inv = ref _invaders[i];

                // Random type with weighted distribution
                float typeRoll = Random.Shared.NextSingle();
                if (typeRoll < 0.2f)
                    inv.Type = InvaderType.Small;
                else if (typeRoll < 0.6f)
                    inv.Type = InvaderType.Medium;
                else
                    inv.Type = InvaderType.Big;

                // Size based on type
                inv.Size = inv.Type switch
                {
                    InvaderType.Small => _invaderBigSize * _invaderSmallSizePercent,
                    InvaderType.Medium => _invaderBigSize * _invaderMediumSizePercent,
                    InvaderType.Big => _invaderBigSize,
                    _ => _invaderBigSize
                };

                // Spawn from left or right edge at top
                bool fromLeft = Random.Shared.NextSingle() > 0.5f;
                inv.Position = new Vector2(
                    fromLeft ? -inv.Size : _viewportWidth + inv.Size,
                    Random.Shared.NextSingle() * 100f + 50f // Top area with some variance
                );

                // Move towards center
                float speed = _invaderMinSpeed + Random.Shared.NextSingle() * (_invaderMaxSpeed - _invaderMinSpeed);
                inv.Velocity = new Vector2(fromLeft ? speed : -speed, 0);

                inv.AnimPhase = Random.Shared.NextSingle() * MathF.PI * 2f;
                inv.Health = 1f;
                inv.IsActive = true;
                break;
            }
        }
    }

    private void SpawnExplosion(Vector2 position, Vector4 baseColor, float size)
    {
        int particleCount = (int)(_explosionParticleCount * (size / _invaderBigSize));
        particleCount = Math.Clamp(particleCount, 10, _explosionParticleCount * 2);

        for (int i = 0; i < particleCount; i++)
        {
            SpawnExplosionParticle(position, baseColor);
        }
    }

    private void SpawnExplosionParticle(Vector2 position, Vector4 baseColor)
    {
        for (int i = 0; i < MaxExplosionParticles; i++)
        {
            ref ExplosionParticle p = ref _explosions[i];
            if (p.Life <= 0f)
            {
                float angle = Random.Shared.NextSingle() * MathF.PI * 2f;
                float force = _explosionForce * (0.3f + Random.Shared.NextSingle() * 0.7f);

                p.Position = position;
                p.Velocity = new Vector2(MathF.Cos(angle) * force, MathF.Sin(angle) * force);

                // Vary color slightly
                p.Color = baseColor;
                p.Color.X = Math.Clamp(p.Color.X + (Random.Shared.NextSingle() - 0.5f) * 0.3f, 0f, 1f);
                p.Color.Y = Math.Clamp(p.Color.Y + (Random.Shared.NextSingle() - 0.5f) * 0.3f, 0f, 1f);
                p.Color.Z = Math.Clamp(p.Color.Z + (Random.Shared.NextSingle() - 0.5f) * 0.3f, 0f, 1f);

                // Add some white/yellow for fire effect
                float whiteness = Random.Shared.NextSingle() * 0.5f;
                p.Color.X = Math.Min(1f, p.Color.X + whiteness);
                p.Color.Y = Math.Min(1f, p.Color.Y + whiteness * 0.8f);

                p.Size = _explosionParticleSize * (0.5f + Random.Shared.NextSingle() * 0.5f);
                p.Life = _explosionLifespan * (0.5f + Random.Shared.NextSingle() * 0.5f);
                p.MaxLife = p.Life;
                break;
            }
        }
    }

    private void SpawnTrailParticle(Vector2 position, Vector4 color)
    {
        for (int i = 0; i < MaxExplosionParticles; i++)
        {
            ref ExplosionParticle p = ref _explosions[i];
            if (p.Life <= 0f)
            {
                p.Position = position + new Vector2(
                    (Random.Shared.NextSingle() - 0.5f) * 4f,
                    Random.Shared.NextSingle() * 8f
                );
                p.Velocity = new Vector2(
                    (Random.Shared.NextSingle() - 0.5f) * 20f,
                    Random.Shared.NextSingle() * 40f + 20f
                );
                p.Color = color * 0.6f;
                p.Color.W = 1f;
                p.Size = 3f;
                p.Life = 0.15f;
                p.MaxLife = 0.15f;
                break;
            }
        }
    }

    private Vector4 GetRocketColor()
    {
        if (_rocketRainbowMode)
            return HueToRgb(_rainbowHue + Random.Shared.NextSingle() * 0.1f);
        return _rocketColor;
    }

    private Vector4 GetInvaderColor(InvaderType type)
    {
        return type switch
        {
            InvaderType.Small => _invaderSmallColor,
            InvaderType.Medium => _invaderMediumColor,
            InvaderType.Big => _invaderBigColor,
            _ => _invaderMediumColor
        };
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
            EnableTrails = _enableTrails ? 1f : 0f,
            TrailLength = _trailLength,
            NeonIntensity = _neonIntensity,
            AnimSpeed = _animSpeed,
            HdrMultiplier = context.HdrPeakBrightness
        };
        context.UpdateBuffer(_frameDataBuffer!, frameData);

        int entityIndex = 0;
        int totalEntities = MaxInvaders + MaxRockets + MaxExplosionParticles;

        // Add invaders to GPU buffer
        for (int i = 0; i < MaxInvaders && entityIndex < totalEntities; i++)
        {
            ref Invader inv = ref _invaders[i];
            if (!inv.IsActive) continue;

            float entityType = inv.Type switch
            {
                InvaderType.Small => 2f,
                InvaderType.Medium => 3f,
                InvaderType.Big => 4f,
                _ => 3f
            };

            _gpuEntities[entityIndex] = new EntityGPU
            {
                Position = inv.Position,
                Velocity = new Vector2(inv.AnimPhase, 0), // Pass animation phase in velocity.x
                Color = GetInvaderColor(inv.Type),
                Size = inv.Size,
                Life = 1f,
                MaxLife = 1f,
                EntityType = entityType
            };
            entityIndex++;
        }

        // Add rockets to GPU buffer
        for (int i = 0; i < MaxRockets && entityIndex < totalEntities; i++)
        {
            ref Rocket rocket = ref _rockets[i];
            if (!rocket.IsActive) continue;

            _gpuEntities[entityIndex] = new EntityGPU
            {
                Position = rocket.Position,
                Velocity = rocket.Velocity,
                Color = rocket.Color,
                Size = rocket.Size,
                Life = 1f,
                MaxLife = 1f,
                EntityType = 1f // Rocket
            };
            entityIndex++;
        }

        // Add explosion particles to GPU buffer
        for (int i = 0; i < MaxExplosionParticles && entityIndex < totalEntities; i++)
        {
            ref ExplosionParticle p = ref _explosions[i];
            if (p.Life <= 0f) continue;

            _gpuEntities[entityIndex] = new EntityGPU
            {
                Position = p.Position,
                Velocity = p.Velocity,
                Color = p.Color,
                Size = p.Size,
                Life = p.Life,
                MaxLife = p.MaxLife,
                EntityType = 0f // Particle
            };
            entityIndex++;
        }

        // Render game entities (invaders, rockets, explosions)
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

        _textOverlay.AddTextCentered("INVADERS", new Vector2(centerX, centerY - _scoreOverlaySize * 2f), titleStyle);
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
            // Define styles matching the original neon look - bright and readable
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

            var ppmLabelStyle = new TextStyle
            {
                Color = new Vector4(1f, 1f, 0.2f, 1f),
                Size = _scoreOverlaySize * 0.6f * 0.8f,
                Spacing = _scoreOverlaySpacing,
                GlowIntensity = 1.5f
            };

            var ppmValueStyle = new TextStyle
            {
                Color = new Vector4(1f, 1f, 0.3f, 1f),
                Size = _scoreOverlaySize * 0.8f,
                Spacing = _scoreOverlaySpacing,
                GlowIntensity = 1.8f
            };

            var timerLabelStyle = new TextStyle
            {
                Color = new Vector4(0.2f, 0.9f, 1f, 1f),
                Size = _scoreOverlaySize * 0.6f * 0.8f,
                Spacing = _scoreOverlaySpacing,
                GlowIntensity = 1.5f
            };

            var timerValueStyle = new TextStyle
            {
                Color = new Vector4(0.3f, 1f, 1f, 1f),
                Size = _scoreOverlaySize * 0.7f,
                Spacing = _scoreOverlaySpacing,
                GlowIntensity = 1.8f
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
            RenderHighScores(totalTime);
        }

        _textOverlay.EndFrame();
        _textOverlay.Render(context);
    }

    private void RenderHighScores(float totalTime)
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

        // Add restart prompt just below the last entry (like Retropede)
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

    private static Vector4 HueToRgb(float hue)
    {
        hue -= MathF.Floor(hue);
        float h = hue * 6f;
        float x = 1f - MathF.Abs(h % 2f - 1f);

        Vector3 rgb = (int)h switch
        {
            0 => new Vector3(1f, x, 0f),
            1 => new Vector3(x, 1f, 0f),
            2 => new Vector3(0f, 1f, x),
            3 => new Vector3(0f, x, 1f),
            4 => new Vector3(x, 0f, 1f),
            _ => new Vector3(1f, 0f, x),
        };

        return new Vector4(rgb.X, rgb.Y, rgb.Z, 1f);
    }

    private static string LoadEmbeddedShader(string name)
    {
        var assembly = typeof(InvadersEffect).Assembly;
        string resourceName = $"MouseEffects.Effects.Invaders.Shaders.{name}";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
