# Particle Flash Effect Guide

## What Changed

Replaced the URP shader color flash (which wasn't working) with a **particle system** that creates white flash bursts when enemies take damage.

## How It Works

1. **Automatic Setup**: When enemy spawns, `CreateDamageFlashParticles()` creates a child GameObject with a ParticleSystem
2. **Sprite Matching**: Particles use the same sprite as the enemy for perfect overlay
3. **Flash Bursts**: 3 quick bursts of white particles when damaged
4. **URP Compatible**: Uses `Sprite-Unlit-Default` shader which works perfectly with URP

## Visual Effect

- **Duration**: Each flash lasts 0.1 seconds
- **Count**: 3 flashes total (0.2s apart)
- **Color**: Pure white
- **Rendering**: Draws on top of enemy sprite

## Tweaking the Effect (In Code)

If you want to adjust the flash effect, here are the key parameters in `CreateDamageFlashParticles()`:

```csharp
// Flash duration
main.startLifetime = 0.1f; // How long each flash shows

// Flash size (adjust based on enemy size)
main.startSize = 2f; // Increase for bigger enemies

// Number of particle bursts per flash
emission.SetBursts(new ParticleSystem.Burst[] { 
    new ParticleSystem.Burst(0f, 3) // 3 particles per flash
});

// Flash timing (in FlashParticles coroutine)
yield return new WaitForSeconds(0.2f); // Time between flashes

// Number of flashes
for (int i = 0; i < 3; i++) // Change 3 to whatever you want
```

## Adjusting in Unity Inspector (After Play)

Once you play the game and an enemy spawns, you can:

1. **Find the particle system**: 
   - Select enemy in Hierarchy
   - Expand to see "DamageFlashParticles" child object
   
2. **View settings**: Click the ParticleSystem component

3. **Test different values** while game is running (changes won't save, just for testing):
   - **Start Lifetime**: How long flash shows
   - **Start Size**: How big the flash is
   - **Start Color**: Color of flash (try yellow, red, etc.)
   - **Emission > Bursts > Count**: How many particles per flash

4. **Copy values to code**: Once you find settings you like, copy them to the `CreateDamageFlashParticles()` method

## Different Flash Colors Per Enemy Type

Want different colored flashes? Add this to `EnemyBase`:

```csharp
[Header("Visual Effects")]
public Color flashColor = Color.white;

// Then in CreateDamageFlashParticles():
main.startColor = flashColor; // Instead of Color.white
```

Now you can set different flash colors in the Inspector for each enemy type!

## Troubleshooting

### Flash is too small
```csharp
main.startSize = 3f; // Increase this value
```

### Flash is too faint
```csharp
// Increase particle count
emission.SetBursts(new ParticleSystem.Burst[] { 
    new ParticleSystem.Burst(0f, 10) // More particles = brighter
});
```

### Flash happens too fast
```csharp
// In FlashParticles() coroutine
yield return new WaitForSeconds(0.3f); // Longer delay between flashes
```

### Want a different effect entirely
```csharp
// Try these alternatives in CreateDamageFlashParticles():

// Option 1: Expanding ring
shape.shapeType = ParticleSystemShapeType.Circle;
main.startSpeed = 5f; // Particles move outward

// Option 2: Random sparkles
shape.shapeType = ParticleSystemShapeType.Box;
main.startSpeed = 2f;
var velocityOverLifetime = damageFlashParticles.velocityOverLifetime;
velocityOverLifetime.enabled = true;

// Option 3: Fade out instead of instant
var colorOverLifetime = damageFlashParticles.colorOverLifetime;
colorOverLifetime.enabled = true;
Gradient grad = new Gradient();
grad.SetKeys(
    new GradientColorKey[] { 
        new GradientColorKey(Color.white, 0f), 
        new GradientColorKey(Color.white, 1f) 
    },
    new GradientAlphaKey[] { 
        new GradientAlphaKey(1f, 0f), 
        new GradientAlphaKey(0f, 1f) 
    }
);
colorOverLifetime.color = grad;
```

## Performance

This particle system is **very lightweight**:
- Only 3 particles per flash
- 3 flashes total = 9 particles max
- Particles destroyed after 0.1 seconds
- No continuous emission

Even with 50 enemies on screen taking damage simultaneously, this won't impact performance.
