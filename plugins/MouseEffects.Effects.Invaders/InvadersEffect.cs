using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using MouseEffects.Core.Effects;
using MouseEffects.Core.Input;
using MouseEffects.Core.Rendering;
using MouseEffects.Core.Time;

using MouseButtons = MouseEffects.Core.Input.MouseButtons;

namespace MouseEffects.Effects.Invaders;

public sealed class InvadersEffect : EffectBase
{
    // Invader types matching classic Space Invaders
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
        public float GlowIntensity;
        public float EnableTrails;
        public float TrailLength;
        public float NeonIntensity;
        public float AnimSpeed;
        public float Padding1;
        public float Padding2;
        public float Padding3;
        public float Padding4;
        public float Padding5;
        public float Padding6;
        public float Padding7;
        public float Padding8;
    }

    private const int MaxInvaders = 100;
    private const int MaxRockets = 50;
    private const int MaxExplosionParticles = 2000;
    private const int MaxOverlayChars = 80; // Labels + Score + PPM + Timer display

    private static readonly EffectMetadata _metadata = new()
    {
        Id = "invaders",
        Name = "Space Invaders",
        Description = "Defend against waves of neon space invaders with rockets from your cursor",
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
    private readonly EntityGPU[] _gpuEntities = new EntityGPU[MaxInvaders + MaxRockets + MaxExplosionParticles + MaxOverlayChars];

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
    private float _timerDuration = 60f; // Default 1 minute
    private float _elapsedTime;
    private bool _isGameActive;
    private bool _isGameEnded;
    private bool _waitingForFirstHit = true; // Timer starts on first kill

    private float _rainbowHue;

    public override EffectMetadata Metadata => _metadata;
    public int CurrentScore => _score;
    public float ElapsedTime => _elapsedTime;
    public float RemainingTime => Math.Max(0f, _timerDuration - _elapsedTime);
    public float TimerDuration => _timerDuration;
    public bool IsGameActive => _isGameActive;
    public bool IsGameEnded => _isGameEnded;
    public bool WaitingForFirstHit => _waitingForFirstHit;
    public float PointsPerMinute => _elapsedTime > 0 ? (_score / (_elapsedTime / 60f)) : 0f;

    public void ResetGame()
    {
        _score = 0;
        _elapsedTime = 0f;
        _isGameActive = true;
        _isGameEnded = false;
        _waitingForFirstHit = true; // Timer starts on first kill

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
        int totalEntities = MaxInvaders + MaxRockets + MaxExplosionParticles + MaxOverlayChars;
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

            // Deactivate if off bottom of screen
            if (inv.Position.Y > _viewportHeight + inv.Size)
            {
                inv.IsActive = false;
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
            GlowIntensity = _glowIntensity,
            EnableTrails = _enableTrails ? 1f : 0f,
            TrailLength = _trailLength,
            NeonIntensity = _neonIntensity,
            AnimSpeed = _animSpeed
        };
        context.UpdateBuffer(_frameDataBuffer!, frameData);

        int entityIndex = 0;
        int totalEntities = MaxInvaders + MaxRockets + MaxExplosionParticles + MaxOverlayChars;

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

        // Add score overlay (score, PPM, timer) with labels
        if (_showScoreOverlay && _isGameActive || _isGameEnded)
        {
            float digitWidth = _scoreOverlaySize * _scoreOverlaySpacing;
            float lineHeight = _scoreOverlaySize * 1.6f;
            float startX = _scoreOverlayX;
            float currentY = _scoreOverlayY;
            float labelSize = _scoreOverlaySize * 0.6f;
            float labelWidth = labelSize * _scoreOverlaySpacing;

            // Helper to get entity type for a character
            static float CharToEntityType(char c)
            {
                if (c >= '0' && c <= '9') return 5f + (c - '0');
                if (c == ':') return 15f;
                if (c >= 'A' && c <= 'Z') return 16f + (c - 'A');
                if (c == ' ') return 42f;
                return 42f; // space for unknown
            }

            // Calculate background size based on content
            // "NB POINTS PER MIN" is longest label (17 chars including spaces, ~14 rendered)
            float bgPadding = _scoreOverlaySize * 0.5f;
            float bgWidth = (17 * labelWidth * 0.8f) + _scoreOverlayMargin + (10 * digitWidth) + bgPadding * 2;
            float bgHeight = lineHeight * 3 + bgPadding * 2;
            float bgCenterX = startX - bgPadding + bgWidth / 2;
            float bgCenterY = currentY - bgPadding + bgHeight / 2;

            // Render background first (entity type 50)
            if (_scoreOverlayBgOpacity > 0.01f && entityIndex < totalEntities)
            {
                _gpuEntities[entityIndex] = new EntityGPU
                {
                    Position = new Vector2(bgCenterX, bgCenterY),
                    Velocity = new Vector2(bgWidth / 2f, bgHeight / 2f), // width/height encoded in velocity
                    Color = new Vector4(0.05f, 0.05f, 0.1f, _scoreOverlayBgOpacity), // Dark blue-ish
                    Size = 1f,
                    Life = 1f,
                    MaxLife = 1f,
                    EntityType = 50f // Background
                };
                entityIndex++;
            }

            // Line 1: "SCORE" label + value
            string label1 = "SCORE";
            float labelX = startX;
            for (int i = 0; i < label1.Length && entityIndex < totalEntities; i++)
            {
                float entityType = CharToEntityType(label1[i]);
                if (entityType != 42f) // Skip spaces
                {
                    _gpuEntities[entityIndex] = new EntityGPU
                    {
                        Position = new Vector2(labelX, currentY),
                        Velocity = Vector2.Zero,
                        Color = _scoreOverlayColor * 0.7f,
                        Size = labelSize,
                        Life = 1f,
                        MaxLife = 1f,
                        EntityType = entityType
                    };
                    _gpuEntities[entityIndex].Color.W = 1f;
                    entityIndex++;
                }
                labelX += labelWidth;
            }

            // Score value after label + margin
            float valueX = labelX + _scoreOverlayMargin;
            string scoreStr = _score.ToString();
            for (int i = 0; i < scoreStr.Length && entityIndex < totalEntities; i++)
            {
                int digit = scoreStr[i] - '0';
                if (digit >= 0 && digit <= 9)
                {
                    _gpuEntities[entityIndex] = new EntityGPU
                    {
                        Position = new Vector2(valueX + i * digitWidth, currentY),
                        Velocity = Vector2.Zero,
                        Color = _scoreOverlayColor,
                        Size = _scoreOverlaySize,
                        Life = 1f,
                        MaxLife = 1f,
                        EntityType = 5f + digit
                    };
                    entityIndex++;
                }
            }

            // Line 2: "NB POINTS PER MIN" label + value
            currentY += lineHeight;
            string label2 = "NB POINTS PER MIN";
            labelX = startX;
            for (int i = 0; i < label2.Length && entityIndex < totalEntities; i++)
            {
                float entityType = CharToEntityType(label2[i]);
                if (entityType != 42f) // Skip spaces but advance position
                {
                    _gpuEntities[entityIndex] = new EntityGPU
                    {
                        Position = new Vector2(labelX, currentY),
                        Velocity = Vector2.Zero,
                        Color = new Vector4(1f, 1f, 0f, 0.7f), // Yellow dimmed
                        Size = labelSize * 0.8f,
                        Life = 1f,
                        MaxLife = 1f,
                        EntityType = entityType
                    };
                    entityIndex++;
                }
                labelX += labelWidth * 0.8f;
            }

            // PPM value after label + margin
            valueX = labelX + _scoreOverlayMargin;
            float ppm = _elapsedTime > 0 ? (_score / (_elapsedTime / 60f)) : 0f;
            string ppmStr = ((int)ppm).ToString();
            for (int i = 0; i < ppmStr.Length && entityIndex < totalEntities; i++)
            {
                int digit = ppmStr[i] - '0';
                if (digit >= 0 && digit <= 9)
                {
                    _gpuEntities[entityIndex] = new EntityGPU
                    {
                        Position = new Vector2(valueX + i * digitWidth * 0.8f, currentY),
                        Velocity = Vector2.Zero,
                        Color = new Vector4(1f, 1f, 0f, 1f), // Yellow for PPM
                        Size = _scoreOverlaySize * 0.8f,
                        Life = 1f,
                        MaxLife = 1f,
                        EntityType = 5f + digit
                    };
                    entityIndex++;
                }
            }

            // Line 3: "COUNTDOWN" label + timer value (MM:SS)
            currentY += lineHeight;
            string label3 = "COUNTDOWN";
            labelX = startX;
            for (int i = 0; i < label3.Length && entityIndex < totalEntities; i++)
            {
                float entityType = CharToEntityType(label3[i]);
                if (entityType != 42f)
                {
                    _gpuEntities[entityIndex] = new EntityGPU
                    {
                        Position = new Vector2(labelX, currentY),
                        Velocity = Vector2.Zero,
                        Color = new Vector4(0f, 0.8f, 1f, 0.7f), // Cyan dimmed
                        Size = labelSize * 0.8f,
                        Life = 1f,
                        MaxLife = 1f,
                        EntityType = entityType
                    };
                    entityIndex++;
                }
                labelX += labelWidth * 0.8f;
            }

            // Timer value after label + margin (show "READY" when waiting for first hit)
            valueX = labelX + _scoreOverlayMargin;
            string timerStr;
            if (_waitingForFirstHit && _isGameActive)
            {
                timerStr = "READY";
            }
            else
            {
                float remainingTime = Math.Max(0f, _timerDuration - _elapsedTime);
                int totalSeconds = (int)remainingTime;
                int minutes = totalSeconds / 60;
                int seconds = totalSeconds % 60;
                timerStr = $"{minutes:D2}:{seconds:D2}";
            }
            float timerX = valueX;
            for (int i = 0; i < timerStr.Length && entityIndex < totalEntities; i++)
            {
                float entityType = CharToEntityType(timerStr[i]);

                _gpuEntities[entityIndex] = new EntityGPU
                {
                    Position = new Vector2(timerX, currentY),
                    Velocity = Vector2.Zero,
                    Color = new Vector4(0f, 0.8f, 1f, 1f), // Cyan for timer
                    Size = _scoreOverlaySize * 0.7f,
                    Life = 1f,
                    MaxLife = 1f,
                    EntityType = entityType
                };
                entityIndex++;
                timerX += digitWidth * 0.7f;
            }
        }

        if (entityIndex == 0)
            return;

        // Clear remaining slots
        for (int j = entityIndex; j < totalEntities; j++)
        {
            _gpuEntities[j] = default;
        }

        context.UpdateBuffer(_entityBuffer!, (ReadOnlySpan<EntityGPU>)_gpuEntities.AsSpan(0, totalEntities));
        context.SetVertexShader(_vertexShader);
        context.SetPixelShader(_pixelShader);
        context.SetConstantBuffer(ShaderStage.Vertex, 0, _frameDataBuffer!);
        context.SetConstantBuffer(ShaderStage.Pixel, 0, _frameDataBuffer!);
        context.SetShaderResource(ShaderStage.Vertex, 0, _entityBuffer!);
        context.SetBlendState(BlendMode.Additive);
        context.SetPrimitiveTopology(PrimitiveTopology.TriangleList);
        context.DrawInstanced(6, totalEntities, 0, 0);
        context.SetBlendState(BlendMode.Alpha);
    }

    protected override void OnDispose()
    {
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
