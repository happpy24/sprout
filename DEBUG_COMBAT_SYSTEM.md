# Combat System Debug Guide

## Issues Fixed

### 1. Wall Knockback Not Working
**Problem**: Player wasn't being knocked back when attacking walls
**Solution**: 
- Changed wall detection to use LayerMask bitwise operation instead of layer name
- Added debug logging to verify wall collisions are detected
- Ensured `ApplyExternalForce` is properly called

**How to verify it's working**:
1. Look in Console for: `"Wall hit detected! Layer: [LayerName]"`
2. Look for: `"Applying wall knockback: [Vector2]"`
3. If you see these logs but still no knockback, check:
   - MeleeWeapon GameObject has Rigidbody2D (Kinematic)
   - MeleeWeapon has a Trigger Collider2D
   - Your tilemap layer is included in `tilemapLayer` LayerMask on PlayerMovement

### 2. VineCrawler Knockback Inconsistent
**Problem**: Movement velocity was overriding knockback in Update()
**Solution**: 
- Moved ALL velocity control to `FixedUpdate()` 
- Only logic/state checks remain in `Update()`
- This ensures knockback applied by Rigidbody2D isn't immediately overridden

**How to verify it's working**:
1. Look in Console for: `"[EnemyName] knockback direction: [Vector2], force: [float]"`
2. Enemy should slide backward when hit
3. If still not working, check:
   - VineCrawler has Rigidbody2D (Dynamic, not Kinematic)
   - EnemyBase `knockbackForce` value (try increasing to 10+ for testing)

### 3. Enemy Flash Effect Not Showing (URP Shader Fix)
**Problem**: URP Sprite-Lit-Default shader doesn't respond to standard `SpriteRenderer.color`
**Solution**: 
- Added MaterialPropertyBlock support for URP shaders
- Uses both `SpriteRenderer.color` AND `MaterialPropertyBlock.SetColor()` for maximum compatibility
- Stores original colors at Start() for accurate restoration

**How to verify it's working**:
1. Look in Console for: `"[EnemyName]: Starting flash effect with [N] sprite renderers"`
2. Enemy sprite should flash white 3 times
3. Then: `"[EnemyName]: Flash effect complete"`
4. **URP Note**: If using URP/2D/Sprite-Lit-Default, the flash now works via Material Property Blocks

### 4. Enemy Movement After Death
**Problem**: VineCrawler continued moving after hp reached 0
**Solution**: 
- Added `isDead` public bool to EnemyBase
- VineCrawler checks `isDead` in Update() and FixedUpdate()
- Rigidbody2D set to Kinematic on death to prevent further physics

**How to verify it's working**:
1. Kill an enemy and verify it stops moving immediately
2. Velocity should be set to zero
3. No further physics interactions should occur

## Additional Troubleshooting

### MeleeWeapon Setup Checklist
- [ ] MeleeWeapon GameObject (child of Player) exists
- [ ] Has Rigidbody2D component (Body Type: Kinematic)
- [ ] Has Collider2D component (Is Trigger: checked)
- [ ] Trigger collider is big enough to hit walls
- [ ] MeleeWeapon script is attached

### VineCrawler Setup Checklist
- [ ] Has Rigidbody2D (Body Type: Dynamic)
- [ ] Has Collider2D (not trigger)
- [ ] Has EnemyBase script
- [ ] Has VineCrawler script
- [ ] `knockbackForce` on EnemyBase is > 0

### Enemy Flash Setup Checklist (URP Compatible)
- [ ] Enemy has SpriteRenderer (or child has SpriteRenderer)
- [ ] ? Works with URP/2D/Sprite-Lit-Default shader
- [ ] ? Works with Sprites/Default shader
- [ ] No Animator overriding color constantly
- [ ] EnemyBase script is attached

## URP Shader Information

**Important**: The URP (Universal Render Pipeline) `Sprite-Lit-Default` shader requires a different approach than standard Unity shaders.

**What was changed:**
- Flash effect now uses `MaterialPropertyBlock` to set color
- This works by setting the `_Color` shader property directly
- Compatible with both URP and standard Unity shaders

**If flash still doesn't work with URP:**
1. Check if your material uses a custom shader variant
2. Try setting the material's Blend Mode to see if it affects color
3. Verify in the Inspector that the sprite's color changes during flash (it should)

## Testing Commands

Add this to a test script to verify systems manually:

```csharp
// Test wall knockback
playerMovement.ApplyExternalForce(Vector2.right * 5f, 0.2f);

// Test enemy damage
enemyBase.Damage(10, player.transform.position);

// Test if enemy is dead
Debug.Log($"Enemy dead: {enemyBase.isDead}");
```

## Common Issues

### "Wall hit detected" appears but no knockback
- Check `PlayerMovement.ApplyExternalForce()` is working
- Try increasing `wallKnockbackForce` to 10+
- Verify player isn't in knockback state from enemy damage

### Enemy slides but doesn't flash (URP)
- ? **FIXED**: Now uses MaterialPropertyBlock for URP shaders
- If still not working, check Console for "Starting flash effect" message
- Verify SpriteRenderer exists (add Debug.Log in Start)

### Flash happens but movement still overrides knockback
- ? **FIXED**: All movement now in FixedUpdate
- Ensure `isBeingKnockedBack` check is in FixedUpdate
- Verify `OnKnockbackStart()` is being called (add Debug.Log)

### Enemy keeps moving after death
- ? **FIXED**: Added isDead check in Update/FixedUpdate
- VineCrawler now stops all movement when hp <= 0
- Rigidbody2D becomes Kinematic on death
