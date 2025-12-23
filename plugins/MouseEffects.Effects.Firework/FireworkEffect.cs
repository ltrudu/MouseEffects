using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using MouseEffects.Core.Effects;
using MouseEffects.Core.Input;
using MouseEffects.Core.Rendering;
using MouseEffects.Core.Time;
using MouseEffects.Effects.Firework.Core;
using MouseEffects.Effects.Firework.Styles;
using MouseEffects.Text;
using MouseEffects.Text.Style;

using MouseButtons = MouseEffects.Core.Input.MouseButtons;

namespace MouseEffects.Effects.Firework;

public sealed class FireworkEffect : EffectBase
{
    private struct FireworkRocket
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public Vector4 Color;
        public float Size;
        public float Age;
        public float TargetY;
        public bool IsActive;
    }

    [StructLayout(LayoutKind.Sequential, Size = 48)]
    private struct FrameData
    {
        public Vector2 ViewportSize;
        public float Time;
        public float GlowIntensity;
        public float EnableTrails;
        public float TrailLength;
        public float HdrMultiplier;
        public float Padding1;
        public float Padding2;
        public float Padding3;
        public float Padding4;
        public float Padding5;
    }

    private const int MaxParticlesLimit = 30000;
    private const int MaxRockets = 50;

    private static readonly EffectMetadata _metadata = new()
    {
        Id = "firework",
        Name = "Firework",
        Description = "Creates stunning firework explosions with colorful particles and trails",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Interactive
    };

    private IBuffer? _particleBuffer;
    private IBuffer? _frameDataBuffer;
    private IShader? _vertexShader;
    private IShader? _pixelShader;

    // Text overlay for particle count display
    private TextOverlay? _textOverlay;
    private bool _displayParticleCount;
    private bool _displayStyle;

    private readonly ParticleGPU[] _gpuParticles = new ParticleGPU[MaxParticlesLimit];
    private int _activeParticleCount;

    private readonly FireworkRocket[] _rockets = new FireworkRocket[MaxRockets];
    private Vector2 _lastMousePos;
    private float _lastSpawnDistance;
    private bool _wasLeftPressed;
    private bool _wasRightPressed;
    private float _rainbowHue;
    private float _rocketRainbowHue;
    private float _viewportHeight = 1080f;

    // Style system
    private IFireworkStyle _currentStyle = new ClassicBurstStyle();
    private FireworkContext? _context;
    private ParticlePool? _particlePool;
    private string _fireworkStyleName = "Classic Burst";

    // Style-specific parameters (loaded from config)
    private float _spinSpeed = 8f;
    private float _spinRadius = 30f;
    private bool _enableSparkTrails = true;
    private float _droopIntensity = 2f;
    private float _branchDensity = 2f;
    private float _flashRate = 20f;
    private float _popIntensity = 0.5f;
    private float _particleMultiplier = 5f;
    private int _sparkDensity = 15;
    private float _trailPersistence = 1f;
    private int _maxSparksPerParticle = 8;

    // Random Wave mode
    private bool _randomWaveMode;
    private float _waveDuration = 5f;
    private bool _randomWaveDuration;
    private float _waveDurationMin = 3f;
    private float _waveDurationMax = 10f;
    private float _currentWaveTime;
    private float _currentWaveDuration;
    private int _waveStyleIndex;
    private readonly List<int> _waveStyleSequence = new();
    // Wave transition - wait for particles to die before switching
    private bool _waveTransitioning;
    private int _pendingWaveStyleIndex;
    private static readonly string[] StyleNames = {
        "Classic Burst", "Spinner", "Willow", "Crackling", "Chrysanthemum",
        "Brocade", "Comet", "Crossette", "Palm", "Peony", "Pearls", "Fish",
        "Green Bees", "Pistil", "Stars", "Tail", "Strobe", "Glitter"
    };

    // Configuration values
    private int _maxParticles = 5000;
    private int _maxFireworks = 50;
    private float _particleLifespan = 2.5f;
    private bool _spawnOnLeftClick = true;
    private bool _spawnOnRightClick;
    private int _minParticlesPerFirework = 50;
    private int _maxParticlesPerFirework = 150;
    private float _clickExplosionForce = 300f;
    private bool _enableRandomExplosionSize;
    private float _minExplosionSize = 0.5f;
    private float _maxExplosionSize = 1.5f;
    private bool _spawnOnMove;
    private float _moveSpawnDistance = 100f;
    private float _moveExplosionForce = 150f;
    private float _minParticleSize = 3f;
    private float _maxParticleSize = 8f;
    private float _glowIntensity = 0.8f;
    private bool _enableTrails = true;
    private float _trailLength = 0.3f;
    private float _gravity = 150f;
    private float _drag = 0.98f;
    private float _spreadAngle = 360f;
    private bool _rainbowMode = true;
    private float _rainbowSpeed = 0.5f;
    private Vector4 _primaryColor = new(1f, 0.3f, 0.1f, 1f);
    private Vector4 _secondaryColor = new(1f, 0.8f, 0.2f, 1f);
    private bool _useRandomColors = true;
    private bool _enableSecondaryExplosion = true;
    private float _secondaryExplosionDelay = 0.8f;
    private int _secondaryParticleCount = 20;
    private float _secondaryExplosionForce = 100f;

    // Rocket settings
    private bool _enableRocketMode;
    private float _rocketSpeed = 500f;
    private float _rocketMinAltitude = 0.1f;
    private float _rocketMaxAltitude = 0.3f;
    private float _rocketMaxFuseTime = 3.0f;
    private float _rocketSize = 12f;
    private bool _rocketRainbowMode = true;
    private float _rocketRainbowSpeed = 0.5f;
    private Vector4 _rocketPrimaryColor = new(1f, 0.8f, 0.2f, 1f);
    private Vector4 _rocketSecondaryColor = new(1f, 0.4f, 0.1f, 1f);
    private bool _rocketUseRandomColors = true;

    // Automatic mode settings
    private bool _automaticMode;
    private int _numberOfLaunchpads = 5;
    private bool _randomLaunchAngle;
    private float _minLaunchAngle = -45f;  // degrees from vertical (-90 = left, 0 = up, 90 = right)
    private float _maxLaunchAngle = 45f;
    private string _launchStyle = "All Together";  // "All Together", "Left to Right", "Right to Left", "Random Sequence"
    private float _autoSpawnRate = 2f;  // fireworks per second
    private float _autoSpawnDelay = 0.5f;  // seconds between shots
    private float _autoSpawnTimer;
    private int _currentLaunchpadIndex;
    private float _viewportWidth = 1920f;
    private List<int> _launchpadSequence = new();
    private List<int> _launchStyleSequence = new();
    private int _currentLaunchStyleIndex;

    public override EffectMetadata Metadata => _metadata;

    protected override void OnInitialize(IRenderContext context)
    {
        var particleDesc = new BufferDescription
        {
            Size = MaxParticlesLimit * Marshal.SizeOf<ParticleGPU>(),
            Type = BufferType.Structured,
            Dynamic = true,
            StructureStride = Marshal.SizeOf<ParticleGPU>()
        };
        _particleBuffer = context.CreateBuffer(particleDesc, default);

        var frameDesc = new BufferDescription
        {
            Size = Marshal.SizeOf<FrameData>(),
            Type = BufferType.Constant,
            Dynamic = true
        };
        _frameDataBuffer = context.CreateBuffer(frameDesc, default);

        string shaderSource = LoadEmbeddedShader("FireworkShader.hlsl");
        _vertexShader = context.CompileShader(shaderSource, "VSMain", ShaderStage.Vertex);
        _pixelShader = context.CompileShader(shaderSource, "PSMain", ShaderStage.Pixel);

        for (int j = 0; j < MaxRockets; j++)
            _rockets[j] = new FireworkRocket { IsActive = false };

        // Initialize particle pool and context
        _particlePool = new ParticlePool(_maxParticles);

        _context = new FireworkContext
        {
            Pool = _particlePool,
            CurrentStyle = _currentStyle,
            GetRainbowColor = () => HueToRgb(_rainbowHue + Random.Shared.NextSingle() * 0.1f),
            GetRandomColor = () => HueToRgb(Random.Shared.NextSingle()),
            GetPrimaryColor = () => _primaryColor,
            GetSecondaryColor = () => _secondaryColor,
            GetRandomInt = (min, max) => Random.Shared.Next(min, max),
            GetRandomFloat = () => Random.Shared.NextSingle()
        };

        // Initialize text overlay for particle count display
        _textOverlay = new TextOverlay();
        _textOverlay.Initialize(context);
    }

    protected override void OnConfigurationChanged()
    {
        // Display particle count
        if (Configuration.TryGet("displayParticleCount", out bool displayPart))
            _displayParticleCount = displayPart;
        if (Configuration.TryGet("displayStyle", out bool displayStyle))
            _displayStyle = displayStyle;

        // General settings
        if (Configuration.TryGet("maxParticles", out int maxPart))
        {
            _maxParticles = Math.Clamp(maxPart, 1000, MaxParticlesLimit);
            _particlePool?.Resize(_maxParticles);
        }

        if (Configuration.TryGet("maxFireworks", out int maxFw))
            _maxFireworks = Math.Clamp(maxFw, 1, MaxRockets);

        if (Configuration.TryGet("particleLifespan", out float lifespan))
            _particleLifespan = lifespan;

        if (Configuration.TryGet("spawnOnLeftClick", out bool leftClick))
            _spawnOnLeftClick = leftClick;

        if (Configuration.TryGet("spawnOnRightClick", out bool rightClick))
            _spawnOnRightClick = rightClick;

        if (Configuration.TryGet("minParticlesPerFirework", out int minPart))
            _minParticlesPerFirework = minPart;

        if (Configuration.TryGet("maxParticlesPerFirework", out int maxPartFw))
            _maxParticlesPerFirework = maxPartFw;

        if (Configuration.TryGet("clickExplosionForce", out float clickForce))
            _clickExplosionForce = clickForce;

        if (Configuration.TryGet("enableRandomExplosionSize", out bool randomExpSize))
            _enableRandomExplosionSize = randomExpSize;
        if (Configuration.TryGet("minExplosionSize", out float minExpSize))
            _minExplosionSize = minExpSize;
        if (Configuration.TryGet("maxExplosionSize", out float maxExpSize))
            _maxExplosionSize = maxExpSize;

        if (Configuration.TryGet("spawnOnMove", out bool spawnMove))
            _spawnOnMove = spawnMove;

        if (Configuration.TryGet("moveSpawnDistance", out float moveDist))
            _moveSpawnDistance = moveDist;

        if (Configuration.TryGet("moveExplosionForce", out float moveForce))
            _moveExplosionForce = moveForce;

        if (Configuration.TryGet("minParticleSize", out float minSize))
            _minParticleSize = minSize;

        if (Configuration.TryGet("maxParticleSize", out float maxSize))
            _maxParticleSize = maxSize;

        if (Configuration.TryGet("glowIntensity", out float glow))
            _glowIntensity = glow;

        if (Configuration.TryGet("enableTrails", out bool trails))
            _enableTrails = trails;

        if (Configuration.TryGet("trailLength", out float trailLen))
            _trailLength = trailLen;

        if (Configuration.TryGet("gravity", out float gravity))
            _gravity = gravity;

        if (Configuration.TryGet("drag", out float drag))
            _drag = drag;

        if (Configuration.TryGet("spreadAngle", out float spread))
            _spreadAngle = spread;

        // Firework color settings
        if (Configuration.TryGet("rainbowMode", out bool rainbow))
            _rainbowMode = rainbow;

        if (Configuration.TryGet("rainbowSpeed", out float rainbowSpd))
            _rainbowSpeed = rainbowSpd;

        if (Configuration.TryGet("primaryColor", out Vector4 primary))
            _primaryColor = primary;

        if (Configuration.TryGet("secondaryColor", out Vector4 secondary))
            _secondaryColor = secondary;

        if (Configuration.TryGet("useRandomColors", out bool randomColors))
            _useRandomColors = randomColors;

        // Secondary explosion
        if (Configuration.TryGet("enableSecondaryExplosion", out bool secondaryExp))
            _enableSecondaryExplosion = secondaryExp;

        if (Configuration.TryGet("secondaryExplosionDelay", out float secondaryDelay))
            _secondaryExplosionDelay = secondaryDelay;

        if (Configuration.TryGet("secondaryParticleCount", out int secondaryCount))
            _secondaryParticleCount = secondaryCount;

        if (Configuration.TryGet("secondaryExplosionForce", out float secondaryForce))
            _secondaryExplosionForce = secondaryForce;

        // Rocket settings
        if (Configuration.TryGet("enableRocketMode", out bool rocketMode))
            _enableRocketMode = rocketMode;

        if (Configuration.TryGet("rocketSpeed", out float rocketSpd))
            _rocketSpeed = rocketSpd;

        if (Configuration.TryGet("rocketMinAltitude", out float minAlt))
            _rocketMinAltitude = minAlt;

        if (Configuration.TryGet("rocketMaxAltitude", out float maxAlt))
            _rocketMaxAltitude = maxAlt;

        if (Configuration.TryGet("rocketMaxFuseTime", out float maxFuse))
            _rocketMaxFuseTime = maxFuse;

        if (Configuration.TryGet("rocketSize", out float rocketSize))
            _rocketSize = rocketSize;

        if (Configuration.TryGet("rocketRainbowMode", out bool rocketRainbow))
            _rocketRainbowMode = rocketRainbow;

        if (Configuration.TryGet("rocketRainbowSpeed", out float rocketRainbowSpd))
            _rocketRainbowSpeed = rocketRainbowSpd;

        if (Configuration.TryGet("rocketPrimaryColor", out Vector4 rocketPrimary))
            _rocketPrimaryColor = rocketPrimary;

        if (Configuration.TryGet("rocketSecondaryColor", out Vector4 rocketSecondary))
            _rocketSecondaryColor = rocketSecondary;

        if (Configuration.TryGet("rocketUseRandomColors", out bool rocketRandomColors))
            _rocketUseRandomColors = rocketRandomColors;

        // Automatic mode settings
        if (Configuration.TryGet("automaticMode", out bool autoMode))
            _automaticMode = autoMode;
        if (Configuration.TryGet("numberOfLaunchpads", out int numPads))
            _numberOfLaunchpads = Math.Clamp(numPads, 1, 20);
        if (Configuration.TryGet("randomLaunchAngle", out bool randAngle))
            _randomLaunchAngle = randAngle;
        if (Configuration.TryGet("minLaunchAngle", out float minAngle))
            _minLaunchAngle = minAngle;
        if (Configuration.TryGet("maxLaunchAngle", out float maxAngle))
            _maxLaunchAngle = maxAngle;
        if (Configuration.TryGet("launchStyle", out string launchStyle))
        {
            bool styleChanged = _launchStyle != launchStyle;
            _launchStyle = launchStyle;
            if (styleChanged)
            {
                _launchStyleSequence.Clear(); // Reset sequence when style changes
                _currentLaunchStyleIndex = 0;
            }
        }
        if (Configuration.TryGet("autoSpawnRate", out float spawnRate))
            _autoSpawnRate = spawnRate;
        if (Configuration.TryGet("autoSpawnDelay", out float spawnDelay))
            _autoSpawnDelay = spawnDelay;

        // Firework Style
        if (Configuration.TryGet("fireworkStyle", out string styleName))
        {
            if (_fireworkStyleName != styleName)
            {
                _fireworkStyleName = styleName;
                _currentStyle = FireworkStyleFactory.Create(styleName);
                if (_context != null)
                    _context.CurrentStyle = _currentStyle;

                // Apply style defaults to give each style its characteristic feel
                ApplyStyleDefaults();
            }
        }

        // Style-specific parameters
        if (Configuration.TryGet("spinSpeed", out float spinSpd))
        {
            _spinSpeed = spinSpd;
            _currentStyle.SetParameter("spinSpeed", spinSpd);
        }
        if (Configuration.TryGet("spinRadius", out float spinRad))
        {
            _spinRadius = spinRad;
            _currentStyle.SetParameter("spinRadius", spinRad);
        }
        if (Configuration.TryGet("enableSparkTrails", out bool sparkTrails))
        {
            _enableSparkTrails = sparkTrails;
            _currentStyle.SetParameter("enableSparkTrails", sparkTrails);
        }
        if (Configuration.TryGet("droopIntensity", out float droop))
        {
            _droopIntensity = droop;
            _currentStyle.SetParameter("droopIntensity", droop);
        }
        if (Configuration.TryGet("branchDensity", out float branch))
        {
            _branchDensity = branch;
            _currentStyle.SetParameter("branchDensity", branch);
        }
        if (Configuration.TryGet("flashRate", out float flash))
        {
            _flashRate = flash;
            _currentStyle.SetParameter("flashRate", flash);
        }
        if (Configuration.TryGet("popIntensity", out float pop))
        {
            _popIntensity = pop;
            _currentStyle.SetParameter("popIntensity", pop);
        }
        if (Configuration.TryGet("particleMultiplier", out float partMult))
        {
            _particleMultiplier = partMult;
            _currentStyle.SetParameter("particleMultiplier", partMult);
        }
        if (Configuration.TryGet("sparkDensity", out int sparkDens))
        {
            _sparkDensity = sparkDens;
            _currentStyle.SetParameter("sparkDensity", sparkDens);
        }
        if (Configuration.TryGet("trailPersistence", out float trailPers))
        {
            _trailPersistence = trailPers;
            _currentStyle.SetParameter("trailPersistence", trailPers);
        }
        if (Configuration.TryGet("maxSparksPerParticle", out int maxSparks))
        {
            _maxSparksPerParticle = maxSparks;
            _currentStyle.SetParameter("maxSparksPerParticle", maxSparks);
        }

        // Random Wave mode settings
        if (Configuration.TryGet("randomWaveMode", out bool waveMode))
            _randomWaveMode = waveMode;
        if (Configuration.TryGet("waveDuration", out float waveDur))
            _waveDuration = waveDur;
        if (Configuration.TryGet("randomWaveDuration", out bool randWaveDur))
            _randomWaveDuration = randWaveDur;
        if (Configuration.TryGet("waveDurationMin", out float waveDurMin))
            _waveDurationMin = waveDurMin;
        if (Configuration.TryGet("waveDurationMax", out float waveDurMax))
            _waveDurationMax = Math.Max(waveDurMax, _waveDurationMin + 0.1f);

        // Initialize wave if mode just enabled and sequence empty
        if (_randomWaveMode && _waveStyleSequence.Count == 0)
            InitializeWaveSequence();
    }

    protected override void OnUpdate(GameTime gameTime, MouseState mouseState)
    {
        float dt = (float)gameTime.DeltaTime.TotalSeconds;
        float totalTime = (float)gameTime.TotalTime.TotalSeconds;

        if (_rainbowMode)
        {
            _rainbowHue += _rainbowSpeed * dt;
            if (_rainbowHue > 1f) _rainbowHue -= 1f;
        }

        if (_rocketRainbowMode)
        {
            _rocketRainbowHue += _rocketRainbowSpeed * dt;
            if (_rocketRainbowHue > 1f) _rocketRainbowHue -= 1f;
        }

        // Random Wave mode update
        if (_randomWaveMode)
            UpdateWave(dt);

        // Automatic mode update
        if (_automaticMode)
            UpdateAutomaticMode(dt, totalTime);

        UpdateRockets(dt, totalTime);
        UpdateParticles(dt, totalTime);

        bool leftPressed = mouseState.IsButtonPressed(MouseButtons.Left);
        bool rightPressed = mouseState.IsButtonPressed(MouseButtons.Right);

        if (_spawnOnLeftClick && leftPressed && !_wasLeftPressed)
        {
            int particleCount = Random.Shared.Next(_minParticlesPerFirework, _maxParticlesPerFirework + 1);
            SpawnFirework(mouseState.Position, particleCount, _clickExplosionForce, totalTime);
        }

        if (_spawnOnRightClick && rightPressed && !_wasRightPressed)
        {
            int particleCount = Random.Shared.Next(_minParticlesPerFirework, _maxParticlesPerFirework + 1);
            SpawnFirework(mouseState.Position, particleCount, _clickExplosionForce, totalTime);
        }

        _wasLeftPressed = leftPressed;
        _wasRightPressed = rightPressed;

        if (_spawnOnMove)
        {
            float distanceFromLast = Vector2.Distance(mouseState.Position, _lastMousePos);
            _lastSpawnDistance += distanceFromLast;
            if (_lastSpawnDistance >= _moveSpawnDistance)
            {
                int moveParticleCount = Random.Shared.Next(_minParticlesPerFirework / 3, _maxParticlesPerFirework / 3 + 1);
                SpawnFirework(mouseState.Position, Math.Max(5, moveParticleCount), _moveExplosionForce, totalTime);
                _lastSpawnDistance = 0f;
            }
        }

        _lastMousePos = mouseState.Position;
    }

    private void UpdateRockets(float dt, float totalTime)
    {
        for (int i = 0; i < MaxRockets; i++)
        {
            ref FireworkRocket rocket = ref _rockets[i];
            if (!rocket.IsActive) continue;

            rocket.Age += dt;
            rocket.Position += rocket.Velocity * dt;
            rocket.Velocity.Y += _gravity * 0.3f * dt;

            if (_enableTrails)
                SpawnRocketTrail(rocket.Position, rocket.Color);

            // Check explosion conditions:
            // 1. Reached target altitude (Y position)
            // 2. OR exceeded max fuse time (safety fallback)
            bool reachedTarget = rocket.Position.Y <= rocket.TargetY;
            bool timedOut = rocket.Age >= _rocketMaxFuseTime;

            if (reachedTarget || timedOut)
            {
                // During wave transition, just deactivate rocket without explosion
                if (!_waveTransitioning)
                {
                    int particleCount = Random.Shared.Next(_minParticlesPerFirework, _maxParticlesPerFirework + 1);
                    float explosionForce = _clickExplosionForce;
                    if (_enableRandomExplosionSize)
                    {
                        float multiplier = _minExplosionSize + Random.Shared.NextSingle() * (_maxExplosionSize - _minExplosionSize);
                        explosionForce *= multiplier;
                    }
                    SpawnExplosion(rocket.Position, particleCount, explosionForce, rocket.Color, totalTime, isSecondary: false);
                }
                rocket.IsActive = false;
            }
        }
    }

    private void UpdateParticles(float dt, float totalTime)
    {
        if (_context == null || _particlePool == null) return;

        _context.Time = totalTime;
        _context.DeltaTime = dt;
        _context.UseRandomColors = _useRandomColors;
        _context.RainbowMode = _rainbowMode;
        _context.MinParticleSize = _minParticleSize;
        _context.MaxParticleSize = _maxParticleSize;
        _context.ParticleLifespan = _particleLifespan;
        _context.SpreadAngle = _spreadAngle;
        _context.EnableSecondaryExplosion = _enableSecondaryExplosion;
        _context.SecondaryExplosionDelay = _secondaryExplosionDelay;
        _context.SecondaryParticleCount = _secondaryParticleCount;
        _context.SecondaryExplosionForce = _secondaryExplosionForce;

        _activeParticleCount = 0;

        // Update particles through pool
        int poolCapacity = _particlePool.Capacity;
        for (int i = 0; i < poolCapacity; i++)
        {
            ref var p = ref _particlePool.GetParticle(i);
            if (p.Life <= 0f) continue;

            p.Life -= dt;
            if (p.Life <= 0f) continue;

            // Common physics
            p.Position += p.Velocity * dt;
            p.Velocity.Y += _gravity * dt;
            p.Velocity *= _drag;

            // Style-specific update
            _currentStyle.UpdateParticle(ref p, dt, totalTime);

            // Secondary explosion
            if (_enableSecondaryExplosion && !p.HasExploded && p.CanExplode &&
                p.Life / p.MaxLife < 1f - _secondaryExplosionDelay / p.MaxLife)
            {
                p.HasExploded = true;
                // No secondary explosions during wave transition
                if (!_waveTransitioning)
                {
                    Vector4 color = p.Color;
                    _currentStyle.SpawnExplosion(_context, p.Position, _secondaryExplosionForce,
                        color, _secondaryParticleCount, isSecondary: true);
                }
            }

            // No new particles during wave transition
            if (!_waveTransitioning)
            {
                // Trail particle spawning - unified interface for all styles with trails
                if (_currentStyle.HasTrailParticles && _currentStyle.ShouldSpawnTrail(ref p, dt))
                {
                    var trailParticle = _currentStyle.CreateTrailParticle(in p, _context);
                    _particlePool.Spawn(trailParticle);
                }

                // Crossette splitting - stars split into cross pattern
                if (_currentStyle is CrossetteStyle crossStyle)
                {
                    if (crossStyle.ShouldSplit(ref p))
                    {
                        int splitCount = 4;
                        for (int s = 0; s < splitCount; s++)
                        {
                            var splitStar = crossStyle.CreateSplitStar(in p, _context, s);
                            _particlePool.Spawn(splitStar);
                        }
                    }
                }
            }

            _activeParticleCount++;
        }
    }

    private void SpawnFirework(Vector2 position, int particleCount, float force, float totalTime)
    {
        // Block new fireworks during wave transition
        if (_waveTransitioning) return;

        if (_enableRocketMode)
        {
            SpawnRocket(position, totalTime);
            return;
        }

        // Apply random explosion size multiplier
        float effectiveForce = force;
        if (_enableRandomExplosionSize)
        {
            float multiplier = _minExplosionSize + Random.Shared.NextSingle() * (_maxExplosionSize - _minExplosionSize);
            effectiveForce = force * multiplier;
        }

        Vector4 color = GetFireworkColor();
        SpawnExplosion(position, particleCount, effectiveForce, color, totalTime, isSecondary: false);
    }

    private void SpawnRocket(Vector2 position, float totalTime)
    {
        for (int i = 0; i < _maxFireworks; i++)
        {
            if (!_rockets[i].IsActive)
            {
                ref FireworkRocket rocket = ref _rockets[i];
                rocket.Position = position;
                rocket.Velocity = new Vector2((Random.Shared.NextSingle() - 0.5f) * 100f, -_rocketSpeed);
                rocket.Color = GetRocketColor();
                rocket.Size = _rocketSize;
                rocket.Age = 0f;

                // Calculate target Y position based on altitude settings (% from top)
                // Random value between minAltitude and maxAltitude
                float altitudeRange = _rocketMaxAltitude - _rocketMinAltitude;
                float randomAltitude = _rocketMinAltitude + Random.Shared.NextSingle() * altitudeRange;
                rocket.TargetY = _viewportHeight * randomAltitude;

                // If rocket is launched above the explosion zone, set minimal target
                // so it explodes almost immediately
                if (position.Y <= rocket.TargetY)
                {
                    rocket.TargetY = position.Y - 10f; // Explode after minimal travel
                }

                rocket.IsActive = true;
                break;
            }
        }
    }

    private void SpawnRocketTrail(Vector2 position, Vector4 color)
    {
        if (_particlePool == null) return;
        if (_waveTransitioning) return; // No new trails during transition

        for (int i = 0; i < 2; i++)
        {
            var particle = new FireworkParticle
            {
                Position = position,
                Velocity = new Vector2((Random.Shared.NextSingle() - 0.5f) * 20f, Random.Shared.NextSingle() * 30f + 10f),
                Color = color * 0.5f,
                Life = 0.2f,
                MaxLife = 0.2f,
                Size = _minParticleSize * 0.4f,
                CanExplode = false,
                HasExploded = false
            };
            _particlePool.Spawn(particle);
        }
    }

    private void SpawnExplosion(Vector2 position, int count, float force, Vector4 baseColor, float totalTime, bool isSecondary)
    {
        if (_context == null) return;

        _context.Time = totalTime;
        _currentStyle.SpawnExplosion(_context, position, force, baseColor, count, isSecondary);
    }


    private Vector4 GetFireworkColor()
    {
        if (_rainbowMode)
            return HueToRgb(_rainbowHue + Random.Shared.NextSingle() * 0.1f);

        if (_useRandomColors)
            return HueToRgb(Random.Shared.NextSingle());

        return _primaryColor;
    }

    private Vector4 GetRocketColor()
    {
        if (_rocketRainbowMode)
            return HueToRgb(_rocketRainbowHue + Random.Shared.NextSingle() * 0.1f);

        if (_rocketUseRandomColors)
            return HueToRgb(Random.Shared.NextSingle());

        return _rocketPrimaryColor;
    }

    protected override void OnRender(IRenderContext context)
    {
        if (_vertexShader == null || _pixelShader == null)
            return;

        // Store viewport dimensions for rocket altitude and launchpad calculations
        _viewportHeight = context.ViewportSize.Y;
        _viewportWidth = context.ViewportSize.X;

        // Count active rockets
        int activeRocketCount = 0;
        for (int i = 0; i < MaxRockets; i++)
        {
            if (_rockets[i].IsActive)
                activeRocketCount++;
        }

        // Skip particle rendering if nothing to draw, but still render text overlays
        bool hasParticles = _activeParticleCount > 0 || activeRocketCount > 0;
        bool hasTextOverlay = (_displayParticleCount || _displayStyle) && _textOverlay != null;
        if (!hasParticles && !hasTextOverlay)
            return;

        float totalTime = (float)DateTime.Now.TimeOfDay.TotalSeconds;

        var frameData = new FrameData
        {
            ViewportSize = context.ViewportSize,
            Time = totalTime,
            GlowIntensity = _glowIntensity,
            EnableTrails = _enableTrails ? 1f : 0f,
            TrailLength = _trailLength,
            HdrMultiplier = context.HdrPeakBrightness
        };
        context.UpdateBuffer(_frameDataBuffer!, frameData);

        int activeIndex = 0;

        // Copy particles from pool to GPU buffer
        if (_particlePool != null)
        {
            activeIndex = _particlePool.CopyToGpu(_gpuParticles, _currentStyle, _maxParticles);
        }

        // Add rockets to GPU buffer
        for (int i = 0; i < MaxRockets && activeIndex < _maxParticles; i++)
        {
            ref FireworkRocket rocket = ref _rockets[i];
            if (!rocket.IsActive) continue;

            // Calculate life based on distance to target (for visual fade effect)
            float distanceToTarget = rocket.Position.Y - rocket.TargetY;
            float totalDistance = _viewportHeight * (_rocketMaxAltitude - _rocketMinAltitude);
            float rocketLife = Math.Max(0.1f, distanceToTarget / Math.Max(1f, totalDistance));

            _gpuParticles[activeIndex] = new ParticleGPU
            {
                Position = rocket.Position,
                Velocity = rocket.Velocity,
                Color = rocket.Color,
                Size = rocket.Size,
                Life = rocketLife,
                MaxLife = 1f
            };
            activeIndex++;
        }

        // Only upload and draw active particles (major performance optimization)
        int particlesToDraw = activeIndex;
        if (particlesToDraw > 0)
        {
            // Only upload the active portion of the buffer
            context.UpdateBuffer(_particleBuffer!, (ReadOnlySpan<ParticleGPU>)_gpuParticles.AsSpan(0, particlesToDraw));
            context.SetVertexShader(_vertexShader);
            context.SetPixelShader(_pixelShader);
            context.SetConstantBuffer(ShaderStage.Vertex, 0, _frameDataBuffer!);
            context.SetConstantBuffer(ShaderStage.Pixel, 0, _frameDataBuffer!);
            context.SetShaderResource(ShaderStage.Vertex, 0, _particleBuffer!);
            context.SetBlendState(BlendMode.Additive);
            context.SetPrimitiveTopology(PrimitiveTopology.TriangleList);
            // Only draw active instances instead of all _maxParticles
            context.DrawInstanced(6, particlesToDraw, 0, 0);
            context.SetBlendState(BlendMode.Alpha);
        }

        // Render text overlays if enabled (always render even with no particles)
        if ((_displayParticleCount || _displayStyle) && _textOverlay != null)
        {
            _textOverlay.BeginFrame();
            _textOverlay.Time = totalTime;

            // Display particle count at top-left
            if (_displayParticleCount)
            {
                var textStyle = new TextStyle
                {
                    Size = 24f,
                    Color = new Vector4(1f, 1f, 1f, 0.9f),
                    GlowIntensity = 0.5f
                };

                string particleText = $"PARTICLES: {particlesToDraw:D5}";
                Vector2 particlePos = new(20f, 20f);

                Vector2 bgCenter = new(248.5f, 33.5f);
                Vector2 bgSize = new(515f, 58f);
                _textOverlay.AddBackground(bgCenter, bgSize, new Vector4(0f, 0f, 0f, 0.75f), 0.1f);
                _textOverlay.AddText(particleText, particlePos, textStyle);
            }

            // Display style name at top center with wave animation and rainbow color
            if (_displayStyle)
            {
                float screenCenterX = context.ViewportSize.X / 2f;
                string styleName = _fireworkStyleName.ToUpper();

                // Rainbow color cycling
                float hue = (totalTime * 0.5f) % 1f;
                Vector4 rainbowColor = HueToRgb(hue);
                rainbowColor.W = 1f;

                // Background for style name (dynamic width based on text length)
                float styleTextWidth = styleName.Length * 50f + 40f;
                Vector2 styleBgCenter = new(screenCenterX, 60f);
                Vector2 styleBgSize = new(styleTextWidth, 100f);
                _textOverlay.AddBackground(styleBgCenter, styleBgSize, new Vector4(0f, 0f, 0f, 0.75f), 0.1f);

                var styleTextStyle = new TextStyle
                {
                    Size = 36f,
                    Color = rainbowColor,
                    GlowIntensity = 2.0f,
                    Animation = TextAnimation.Wave(2f, 8f, 0.3f)
                };

                _textOverlay.AddTextCentered(styleName, new Vector2(screenCenterX, 60f), styleTextStyle);
            }

            _textOverlay.EndFrame();
            _textOverlay.Render(context);
        }
    }

    protected override void OnDispose()
    {
        _particleBuffer?.Dispose();
        _frameDataBuffer?.Dispose();
        _vertexShader?.Dispose();
        _pixelShader?.Dispose();
        _textOverlay?.Dispose();
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

    #region Random Wave Mode

    private void InitializeWaveSequence()
    {
        _waveStyleSequence.Clear();
        for (int i = 0; i < StyleNames.Length; i++)
            _waveStyleSequence.Add(i);
        ShuffleSequence();
        _waveStyleIndex = 0;
        _currentWaveTime = 0f;
        _currentWaveDuration = GetNextWaveDuration();
        ApplyWaveStyle();
    }

    private void ShuffleSequence()
    {
        // Fisher-Yates shuffle
        for (int i = _waveStyleSequence.Count - 1; i > 0; i--)
        {
            int j = Random.Shared.Next(i + 1);
            (_waveStyleSequence[i], _waveStyleSequence[j]) = (_waveStyleSequence[j], _waveStyleSequence[i]);
        }
    }

    private float GetNextWaveDuration()
    {
        if (_randomWaveDuration)
            return _waveDurationMin + Random.Shared.NextSingle() * (_waveDurationMax - _waveDurationMin);
        return _waveDuration;
    }

    private void UpdateWave(float dt)
    {
        if (_waveStyleSequence.Count == 0)
            InitializeWaveSequence();

        // If transitioning, wait for all particles AND rockets to finish before switching
        if (_waveTransitioning)
        {
            int aliveParticles = _particlePool?.CountAlive() ?? 0;
            int activeRockets = CountActiveRockets();

            if (aliveParticles == 0 && activeRockets == 0)
            {
                // All particles and rockets finished, now switch to new style
                _waveStyleIndex = _pendingWaveStyleIndex;
                ApplyWaveStyle();
                _waveTransitioning = false;
                _currentWaveTime = 0f;
                _currentWaveDuration = GetNextWaveDuration();
            }
            return; // Don't update wave timer during transition
        }

        _currentWaveTime += dt;

        if (_currentWaveTime >= _currentWaveDuration)
        {
            // Time to switch - calculate next style index
            int nextIndex = _waveStyleIndex + 1;

            // If we've gone through all styles, reshuffle
            if (nextIndex >= _waveStyleSequence.Count)
            {
                ShuffleSequence();
                nextIndex = 0;
            }

            // Enter transition mode - wait for particles to die
            _pendingWaveStyleIndex = nextIndex;
            _waveTransitioning = true;
        }
    }

    private int CountActiveRockets()
    {
        if (_rockets == null) return 0;
        int count = 0;
        for (int i = 0; i < _rockets.Length; i++)
            if (_rockets[i].IsActive)
                count++;
        return count;
    }

    private void ApplyWaveStyle()
    {
        if (_waveStyleIndex < 0 || _waveStyleIndex >= _waveStyleSequence.Count)
            return;

        int styleIndex = _waveStyleSequence[_waveStyleIndex];
        if (styleIndex >= 0 && styleIndex < StyleNames.Length)
        {
            string styleName = StyleNames[styleIndex];
            _fireworkStyleName = styleName;
            _currentStyle = FireworkStyleFactory.Create(styleName);
            if (_context != null)
                _context.CurrentStyle = _currentStyle;

            // Apply style defaults for distinctive behavior
            ApplyStyleDefaults();
        }
    }

    #endregion

    #region Automatic Mode

    private void UpdateAutomaticMode(float dt, float totalTime)
    {
        if (_waveTransitioning) return;  // Don't spawn during wave transitions

        _autoSpawnTimer += dt;

        // Check if it's time to spawn
        if (_autoSpawnTimer >= _autoSpawnDelay)
        {
            _autoSpawnTimer = 0f;
            SpawnAutomaticFireworks(totalTime);
        }
    }

    private static readonly string[] LaunchStyles = { "All Together", "Left to Right", "Right to Left", "Double Left/Right", "Random Sequence" };

    private void SpawnAutomaticFireworks(float totalTime)
    {
        // Calculate launchpad positions (evenly distributed along bottom of screen)
        float margin = _viewportWidth * 0.1f;  // 10% margin on each side
        float usableWidth = _viewportWidth - 2 * margin;
        float spacing = _numberOfLaunchpads > 1 ? usableWidth / (_numberOfLaunchpads - 1) : 0;
        float launchY = _viewportHeight - 50f;  // 50px from bottom

        // Get effective launch style (resolve "Random" to actual style using sequence)
        string effectiveStyle = _launchStyle;
        if (string.Equals(_launchStyle, "Random", StringComparison.OrdinalIgnoreCase))
        {
            if (_launchStyleSequence.Count == 0 || _launchStyleSequence.Count != LaunchStyles.Length)
                InitializeLaunchStyleSequence();

            int styleIndex = _launchStyleSequence[_currentLaunchStyleIndex];
            effectiveStyle = LaunchStyles[styleIndex];

            _currentLaunchStyleIndex++;
            if (_currentLaunchStyleIndex >= _launchStyleSequence.Count)
            {
                _currentLaunchStyleIndex = 0;
                ShuffleLaunchStyleSequence();
            }
        }

        switch (effectiveStyle)
        {
            case "All Together":
                // Launch from all launchpads at once
                for (int i = 0; i < _numberOfLaunchpads; i++)
                {
                    float x = margin + i * spacing;
                    if (_numberOfLaunchpads == 1) x = _viewportWidth / 2f;
                    SpawnFromLaunchpad(new Vector2(x, launchY), totalTime);
                }
                break;

            case "Left to Right":
                // Launch from one launchpad at a time, left to right
                {
                    float x = margin + _currentLaunchpadIndex * spacing;
                    if (_numberOfLaunchpads == 1) x = _viewportWidth / 2f;
                    SpawnFromLaunchpad(new Vector2(x, launchY), totalTime);
                    _currentLaunchpadIndex = (_currentLaunchpadIndex + 1) % _numberOfLaunchpads;
                }
                break;

            case "Right to Left":
                // Launch from one launchpad at a time, right to left
                {
                    int reverseIndex = _numberOfLaunchpads - 1 - _currentLaunchpadIndex;
                    float x = margin + reverseIndex * spacing;
                    if (_numberOfLaunchpads == 1) x = _viewportWidth / 2f;
                    SpawnFromLaunchpad(new Vector2(x, launchY), totalTime);
                    _currentLaunchpadIndex = (_currentLaunchpadIndex + 1) % _numberOfLaunchpads;
                }
                break;

            case "Double Left/Right":
                // Launch two rockets simultaneously - one from left going right, one from right going left
                {
                    // Left side launchpad (going through left to right)
                    float leftX = margin + _currentLaunchpadIndex * spacing;
                    if (_numberOfLaunchpads == 1) leftX = _viewportWidth / 2f;
                    SpawnFromLaunchpad(new Vector2(leftX, launchY), totalTime);

                    // Right side launchpad (going through right to left)
                    int rightIndex = _numberOfLaunchpads - 1 - _currentLaunchpadIndex;
                    float rightX = margin + rightIndex * spacing;
                    if (_numberOfLaunchpads == 1) rightX = _viewportWidth / 2f;
                    if (rightIndex != _currentLaunchpadIndex) // Avoid double spawn from same pad
                        SpawnFromLaunchpad(new Vector2(rightX, launchY), totalTime);

                    _currentLaunchpadIndex = (_currentLaunchpadIndex + 1) % _numberOfLaunchpads;
                }
                break;

            case "Random Sequence":
                // Launch from random launchpads
                {
                    if (_launchpadSequence.Count == 0)
                        InitializeLaunchpadSequence();

                    int padIndex = _launchpadSequence[_currentLaunchpadIndex];
                    float x = margin + padIndex * spacing;
                    if (_numberOfLaunchpads == 1) x = _viewportWidth / 2f;
                    SpawnFromLaunchpad(new Vector2(x, launchY), totalTime);

                    _currentLaunchpadIndex++;
                    if (_currentLaunchpadIndex >= _launchpadSequence.Count)
                    {
                        _currentLaunchpadIndex = 0;
                        ShuffleLaunchpadSequence();
                    }
                }
                break;
        }
    }

    private void SpawnFromLaunchpad(Vector2 position, float totalTime)
    {
        // Determine launch angle
        float angleRad;
        if (_randomLaunchAngle)
        {
            float angleDeg = _minLaunchAngle + Random.Shared.NextSingle() * (_maxLaunchAngle - _minLaunchAngle);
            angleRad = angleDeg * MathF.PI / 180f;
        }
        else
        {
            angleRad = 0f;  // Straight up
        }

        // Calculate velocity from angle (0 = up, negative = left, positive = right)
        float vx = MathF.Sin(angleRad) * _rocketSpeed;
        float vy = -MathF.Cos(angleRad) * _rocketSpeed;  // Negative because Y is down

        if (_enableRocketMode)
        {
            // Spawn a rocket with the calculated angle
            SpawnRocketWithVelocity(position, new Vector2(vx, vy), totalTime);
        }
        else
        {
            // Direct explosion at launchpad position (for non-rocket mode)
            int particleCount = Random.Shared.Next(_minParticlesPerFirework, _maxParticlesPerFirework + 1);
            float effectiveForce = _clickExplosionForce;
            if (_enableRandomExplosionSize)
            {
                float multiplier = _minExplosionSize + Random.Shared.NextSingle() * (_maxExplosionSize - _minExplosionSize);
                effectiveForce *= multiplier;
            }
            Vector4 color = GetFireworkColor();
            SpawnExplosion(position, particleCount, effectiveForce, color, totalTime, isSecondary: false);
        }
    }

    private void SpawnRocketWithVelocity(Vector2 position, Vector2 velocity, float totalTime)
    {
        for (int i = 0; i < _maxFireworks; i++)
        {
            if (!_rockets[i].IsActive)
            {
                ref FireworkRocket rocket = ref _rockets[i];
                rocket.Position = position;
                rocket.Velocity = velocity;
                rocket.Color = GetRocketColor();
                rocket.Size = _rocketSize;
                rocket.Age = 0f;

                // Calculate target Y based on altitude settings
                float altitudeRange = _rocketMaxAltitude - _rocketMinAltitude;
                float randomAltitude = _rocketMinAltitude + Random.Shared.NextSingle() * altitudeRange;
                rocket.TargetY = _viewportHeight * randomAltitude;

                if (position.Y <= rocket.TargetY)
                {
                    rocket.TargetY = position.Y - 10f;
                }

                rocket.IsActive = true;
                break;
            }
        }
    }

    private void InitializeLaunchpadSequence()
    {
        _launchpadSequence.Clear();
        for (int i = 0; i < _numberOfLaunchpads; i++)
            _launchpadSequence.Add(i);
        ShuffleLaunchpadSequence();
    }

    private void ShuffleLaunchpadSequence()
    {
        for (int i = _launchpadSequence.Count - 1; i > 0; i--)
        {
            int j = Random.Shared.Next(i + 1);
            (_launchpadSequence[i], _launchpadSequence[j]) = (_launchpadSequence[j], _launchpadSequence[i]);
        }
    }

    private void InitializeLaunchStyleSequence()
    {
        _launchStyleSequence.Clear();
        _currentLaunchStyleIndex = 0;
        for (int i = 0; i < LaunchStyles.Length; i++)
            _launchStyleSequence.Add(i);
        ShuffleLaunchStyleSequence();
    }

    private void ShuffleLaunchStyleSequence()
    {
        for (int i = _launchStyleSequence.Count - 1; i > 0; i--)
        {
            int j = Random.Shared.Next(i + 1);
            (_launchStyleSequence[i], _launchStyleSequence[j]) = (_launchStyleSequence[j], _launchStyleSequence[i]);
        }
    }

    #endregion

    /// <summary>
    /// Applies the current style's default parameters for distinctive visual behavior.
    /// This gives each style its characteristic feel (gravity, particle count, etc.)
    /// </summary>
    private void ApplyStyleDefaults()
    {
        var defaults = _currentStyle.GetDefaults();

        // Apply physics defaults - these make each style feel different
        _gravity = defaults.Gravity;
        _drag = defaults.Drag;
        _particleLifespan = defaults.ParticleLifespan;
        _minParticlesPerFirework = defaults.MinParticlesPerFirework;
        _maxParticlesPerFirework = defaults.MaxParticlesPerFirework;
        _clickExplosionForce = defaults.ExplosionForce;
        _minParticleSize = defaults.MinParticleSize;
        _maxParticleSize = defaults.MaxParticleSize;
        _spreadAngle = defaults.SpreadAngle;
        _enableSecondaryExplosion = defaults.EnableSecondaryExplosion;

        // Apply style-specific parameters from defaults
        if (defaults.StyleSpecific != null)
        {
            foreach (var kvp in defaults.StyleSpecific)
            {
                _currentStyle.SetParameter(kvp.Key, kvp.Value);
            }
        }
    }

    private static string LoadEmbeddedShader(string name)
    {
        var assembly = typeof(FireworkEffect).Assembly;
        string resourceName = $"MouseEffects.Effects.Firework.Shaders.{name}";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
