# Opening Sequence Startup Bug Fixes

## Problems Fixed

### 1. Player Jumping on Startup ???
**Problem:** Player would jump immediately when the scene started, causing them to float in the air during the cinematic.

**Root Cause:** 
- PlayerMovement's `Update()` ran before OpeningSequence could disable it
- Jump input was being processed in the first frame
- `lastJumpPressedTime` could be set before controls were disabled

**Solution:**
```csharp
// In PlayerMovement.cs - Added initialization flag
private bool initialized = false;

void Start()
{
    initialized = true; // Mark as ready after first frame
}

void Update()
{
    // Only process input if initialized AND not dead
    if (!playerDead && initialized)
    {
        moveInput = Input.GetAxisRaw("Horizontal");
        jumpPressed = Input.GetKeyDown(KeyCode.Space);
        // ...
    }
    else
    {
        // Clear all input when disabled
        moveInput = 0f;
        jumpPressed = false;
        jumpHeld = false;
        dashPressed = false;
    }
}
```

### 2. Camera Snap Before Sequence ???
**Problem:** Camera would briefly snap to the player's position before the black screen appeared and the cinematic started.

**Root Cause:**
- `Start()` has a delay while waiting for components to load
- PlayerCamera's `LateUpdate()` ran before OpeningSequence disabled it
- Camera followed player for 1-2 frames before cinematic mode was enabled

**Solution:**
```csharp
// In OpeningSequence.cs - Moved setup to Awake()
void Awake()
{
    // Get references IMMEDIATELY
    mainCamera = Camera.main;
    playerCamera = mainCamera.GetComponent<PlayerCamera>();
    playerMovement = player.GetComponent<PlayerMovement>();
    
    // Disable EVERYTHING before first frame renders
    playerCamera.cinematicMode = true;
    playerMovement.playerDead = true;
    playerMovement.enabled = false;
    playerMovement.velocity = Vector2.zero;
    
    // Set positions IMMEDIATELY
    mainCamera.transform.position = startCameraPosition;
    SetImageAlpha(fadeImage, 1f); // Black screen
}
```

## Unity Execution Order

Understanding the fix requires knowing Unity's execution order:

```
Frame 1:
??? Awake()    ? Run on ALL objects first
??? Start()    ? Run on ALL objects second
??? Update()   ? Normal updates begin
??? LateUpdate() ? Camera updates

Frame 2 onwards:
??? Update()
??? LateUpdate()
```

### Before Fix

```
Frame 1:
??? PlayerMovement.Awake()
?   ??? Components initialized
??? OpeningSequence.Start()  ? TOO LATE!
?   ??? Tries to disable player (after Update already ran)
??? PlayerMovement.Update()
?   ??? Processes jump input ?
??? PlayerCamera.LateUpdate()
    ??? Follows player ?

Result: Player jumps, camera snaps
```

### After Fix

```
Frame 1:
??? PlayerMovement.Awake()
?   ??? Components initialized, input cleared
??? OpeningSequence.Awake()  ? EARLY ENOUGH!
?   ??? Disables PlayerMovement ?
?   ??? Enables cinematicMode ?
?   ??? Sets camera position ?
?   ??? Sets black screen ?
??? PlayerMovement.Update()
?   ??? Skipped (not initialized yet) ?
??? PlayerCamera.LateUpdate()
    ??? Skipped (cinematicMode = true) ?

Result: Perfect cinematic start!
```

## Key Changes Summary

### OpeningSequence.cs

**Moved to Awake():**
- ? Camera reference getting
- ? Player reference getting
- ? PlayerCamera.cinematicMode = true
- ? PlayerMovement disabling
- ? Camera position setting
- ? Black screen activation

**Kept in Start():**
- ? IntroCards loading (can wait)
- ? Trigger setup (not urgent)
- ? Sequence coroutine start

### PlayerMovement.cs

**Added:**
- ? `initialized` flag (prevents input before Start())
- ? Input clearing in Awake()
- ? Input validation in Update()

**Effect:**
- Input is NEVER processed before `Start()` completes
- All input is cleared when component is disabled

## Testing Checklist

? **Player doesn't jump on startup**
- Start scene
- Player should remain stationary during cinematic
- No upward velocity applied

? **Camera doesn't snap**
- Start scene
- Camera should be at startCameraPosition immediately
- Black screen should appear instantly
- No flash of player view

? **Sequence plays smoothly**
- Black screen holds for 1 second
- Camera descends smoothly
- Fade in occurs during descent
- Player controls restored at end

? **No initialization delay**
- Everything happens frame 1
- No visible setup time
- Seamless cinematic start

## Debug Verification

Add these temporary logs to verify timing:

```csharp
// In PlayerMovement.Awake()
Debug.Log($"PlayerMovement.Awake() - Frame: {Time.frameCount}");

// In OpeningSequence.Awake()
Debug.Log($"OpeningSequence.Awake() - Frame: {Time.frameCount}");

// In PlayerMovement.Update()
Debug.Log($"PlayerMovement.Update() - Frame: {Time.frameCount}, initialized: {initialized}");
```

Expected output:
```
PlayerMovement.Awake() - Frame: 1
OpeningSequence.Awake() - Frame: 1
PlayerMovement.Update() - Frame: 2, initialized: false
PlayerMovement.Update() - Frame: 3, initialized: true
```

## Common Issues

### "Player still jumps on startup"
- Check PlayerMovement has `initialized = false` by default
- Verify OpeningSequence.Awake() is running before Update()
- Make sure playerDead is being set to true in Awake()

### "Camera still snaps"
- Verify cinematicMode is being set in Awake()
- Check camera position is being set before first LateUpdate()
- Ensure PlayerCamera.LateUpdate() checks cinematicMode

### "Black screen doesn't appear"
- TransitionEffect must exist before scene starts
- fadeImage must be found in Awake()
- SetImageAlpha must be called before first frame renders

## Performance Notes

**No performance impact:**
- Awake() only runs once on scene load
- Same work is being done, just earlier
- Actually BETTER performance (no wasted Update() calls)

**Memory impact:**
- Zero - same objects, same references
- Just different execution timing

## Future Improvements

If you want to make the sequence even more robust:

```csharp
// In PlayerMovement.Awake()
void Awake()
{
    // ... existing code ...
    
    // Force physics to reset
    if (GetComponent<Rigidbody2D>())
    {
        GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
    }
}

// In OpeningSequence.Awake()
void Awake()
{
    // ... existing code ...
    
    // Force Unity to process changes immediately
    Physics2D.SyncTransforms();
}
```

But the current fix is sufficient for your use case!
