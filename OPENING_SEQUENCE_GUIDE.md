# Opening Sequence Setup Guide

## What It Does

The `OpeningSequence` script creates a cinematic intro for your game:
1. **Black screen start** - Using the same transition system as scene changes
2. **Smooth camera descent** - Only during cinematic (smooth movement)
3. **Instant camera follow** - After cinematic ends (no smoothing during gameplay)
4. **Player control** - Disabled during sequence, restored after
5. **Tutorial cards** - Show/hide when player enters/exits trigger zones

## Key Changes

### Camera Behavior
- **During Cinematic**: Smooth, curved camera movement with fade
- **During Gameplay**: **Instant snap** to player position (no smoothing)
- Uses `cinematicMode` flag on `PlayerCamera` to switch between modes

### Tutorial Card Handling
- **Waits for IntroCards** to be loaded by EssentialObjectsLoader
- **Shows Walk card immediately** if player spawns inside Trigger1
- Proper trigger detection using `IsTouching()` method

## Setup Instructions

### 1. Hierarchy Setup

Create this structure in your starting scene:

```
Scene Root
??? OpeningSequenceManager (Empty GameObject)
?   ??? OpeningSequence.cs (attached here)
?   ??? Trigger1 (Empty GameObject)
?   ?   ??? BoxCollider2D (Is Trigger: ?)
?   ??? Trigger2 (Empty GameObject)
?   ?   ??? BoxCollider2D (Is Trigger: ?)
?   ??? Trigger3 (Empty GameObject)
?       ??? BoxCollider2D (Is Trigger: ?)
```

### 2. Tutorial Cards Setup (EssentialObjects)

The IntroCards should be in your **EssentialObjects prefab**:

```
EssentialObjects
??? UI
    ??? IntroCards (Tag: "IntroCards")
        ??? Walk (Image component, Color: white with alpha 0)
        ??? Jump (Image component, Color: white with alpha 0)
        ??? Attack (Image component, Color: white with alpha 0)
```

**Important:**
- IntroCards are loaded via `EssentialObjectsLoader`
- `OpeningSequence` **waits up to 2 seconds** for them to load
- Cards must be children of GameObject tagged with `IntroCards`

### 3. Tag Setup

Make sure these tags exist in your project:
- `IntroCards` - For the tutorial cards parent
- `TransitionEffect` - Should already exist for SceneController
- `Player` - For trigger detection

### 4. Trigger Placement

Position the triggers in your level where you want tutorial cards to appear:
- **Trigger1** ? Shows "Walk" card (player likely spawns here)
- **Trigger2** ? Shows "Jump" card
- **Trigger3** ? Shows "Attack" card

**Trigger Setup:**
1. Each trigger needs a `BoxCollider2D` component
2. Set **Is Trigger** to ? (checked)
3. Size the collider to cover the area where the card should be visible
4. **Player can spawn inside Trigger1** - Walk card will show immediately

### 5. Inspector Settings

On the `OpeningSequence` component:

#### Camera Movement
- **Start Camera Position**: `(0.25, 20.5, -10)` (high up)
- **End Camera Position**: `(0.25, 0, -10)` (player level)
- **Camera Move Time**: `5` seconds (duration of descent)
- **Camera Move Curve**: EaseInOut curve for smooth motion

#### Fade Settings
- **Initial Black Duration**: `1` second (hold black before fade)
- **Fade In Duration**: `2` seconds (fade in during descent)

#### Tutorial Cards
- **Card Fade Duration**: `0.5` seconds (smooth card transitions)
- **Intro Cards Tag**: `IntroCards`
- **Show Walk Card On Start**: ? (checked) - Shows Walk card if player spawns in Trigger1

#### References
- **Transition Effect Tag**: `TransitionEffect`

## How It Works

### Opening Sequence Timeline

```
T+0s:  Black screen, camera at (0.25, 20.5, -10)
       Player controls OFF, cinematicMode ON
       
T+1s:  Start fading in + SMOOTH camera descent begins
       
T+3s:  Fade complete (still descending smoothly)
       
T+6s:  Camera at (0.25, 0, -10)
       Player controls ON, cinematicMode OFF
       Camera now follows player INSTANTLY (no smoothing)
       
       If player spawned in Trigger1: Walk card appears
```

### Camera Modes

**Cinematic Mode (cinematicMode = true)**
- Camera movement is smooth and curved
- Used during opening sequence
- PlayerCamera's `LateUpdate()` is **skipped**

**Gameplay Mode (cinematicMode = false)**
- Camera **instantly snaps** to follow player
- No smoothing, no lag
- Dead zone system with instant response

### Tutorial Card Behavior

**If player spawns in Trigger1:**
1. Sequence completes
2. `playerInTrigger1` flag is true
3. Walk card immediately fades in

**Normal trigger behavior:**
- Enter trigger ? Card fades in
- Exit trigger ? Card fades out
- Rapid entry/exit ? Smooth transitions from current alpha

## PlayerCamera Changes

### Removed Features
- ? Vertical positioning offset (0.33 rule)
- ? Smooth camera follow during gameplay
- ? Vertical smooth speed

### Added Features
- ? `cinematicMode` flag
- ? Instant camera snap to player position
- ? Dead zone with instant response (no lerp/smoothing)

### Camera Behavior Now

```csharp
// During gameplay (cinematicMode = false)
if (playerPos.x < minX)
    baseCameraPos.x = playerPos.x + boundX; // INSTANT
else if (playerPos.x > maxX)
    baseCameraPos.x = playerPos.x - boundX; // INSTANT

// During cinematic (cinematicMode = true)
LateUpdate() is skipped entirely
OpeningSequence manually controls camera position
```

## Debugging

### Console Messages

```
"OpeningSequence: Found IntroCards parent and tutorial cards"
  ? IntroCards loaded successfully

"Opening sequence complete - player controls restored"
  ? Sequence finished, player can move
```

### Common Issues

**1. "IntroCards parent not found after waiting"**
- Check IntroCards GameObject has tag `IntroCards`
- Verify IntroCards is in EssentialObjects prefab
- Check EssentialObjectsLoader is in scene and has prefab reference
- **Wait longer**: Increase `maxWaitTime` to 5f in code

**2. Walk card doesn't show when spawning in Trigger1**
- Verify `Show Walk Card On Start` is checked
- Check player has "Player" tag
- Ensure Trigger1 BoxCollider2D is marked as trigger
- Player's collider must overlap Trigger1's collider

**3. Camera isn't smooth during cinematic**
- Check `cameraMoveCurve` is set to EaseInOut
- Verify `cameraMoveTime > 0`
- Make sure sequence is actually running (check console logs)

**4. Camera is still smooth during gameplay**
- Check `cinematicMode` is false after sequence
- Verify PlayerCamera.LateUpdate() isn't being overridden
- Look for other scripts modifying camera position

**5. Triggers not working**
- Player must have a Collider2D component
- Trigger must have BoxCollider2D with "Is Trigger" checked
- Both must be on layers that interact in Physics2D settings
- Use `IsTouching()` method (not transform comparison)

## Advanced Customization

### Skip Intro in Editor

```csharp
void Start()
{
    #if UNITY_EDITOR
    // FOR TESTING: Skip intro
    if (Input.GetKey(KeyCode.LeftControl))
    {
        SkipIntro();
        return;
    }
    #endif
    
    StartCoroutine(InitializeSequence());
}

private void SkipIntro()
{
    sequenceComplete = true;
    if (fadeImage != null) SetImageAlpha(fadeImage, 0f);
    if (playerMovement != null) playerMovement.enabled = true;
    if (playerCamera != null) playerCamera.cinematicMode = false;
    mainCamera.transform.position = endCameraPosition;
}
```

Hold Left Control when scene starts to skip intro.

### Adjust Camera Dead Zone

In PlayerCamera component:
- **boundX**: Horizontal dead zone size (2.0 = player can move 2 units left/right)
- **boundY**: Vertical dead zone size (1.5 = player can move 1.5 units up/down)

Larger values = more player movement before camera follows.

### Different Trigger Actions

Want different behavior per trigger? Modify `OnTriggerEnter2D`:

```csharp
if (trigger1 != null && otherCollider.IsTouching(trigger1))
{
    playerInTrigger1 = true;
    if (sequenceComplete)
    {
        ShowCard(ref walkCardCoroutine, walkCard, true);
        
        // Add custom behavior
        audioManager.PlaySFX(audioManager.tutorialSound);
        StartCoroutine(FlashArrow());
    }
}
```

## Performance Notes

- **IntroCards wait loop**: Max 2 seconds (0.1s intervals)
- **Cinematic mode**: Skips entire `LateUpdate()` for performance
- **Trigger detection**: Uses `IsTouching()` (faster than bounds checks)
- **Card fades**: Individual coroutines prevent overlapping animations
