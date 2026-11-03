# Space Invaders: YoRHa Combat Simulation

CSCI 381 project. This README covers everything about the project in one place, so no other files are needed to follow it.

## Contents

1. [Overview](#overview)
2. [How to Run](#how-to-run)
3. [Gameplay Summary](#gameplay-summary)
4. [Required Features](#required-features)
   - [Alien Speed-Up](#alien-speed-up)
   - [Lose Detection (and Win Condition)](#lose-detection-and-win-condition)
   - [Ship Screen Clamp](#ship-screen-clamp)
   - [Code Issues](#code-issues)
   - [Balancing](#balancing)
5. [Optional Features](#optional-features)
6. [Script Overview](#script-overview)
7. [Technical Notes](#technical-notes)

---

## Overview

A Space Invaders game built on my professor's in-class Space Invaders project. The starting project had a moving alien fleet, a ship that fires darts, alien bullets, and an alien counter. I fixed code-quality issues in it, added the required mechanics (alien speed-up, win/lose detection, screen clamp), rebalanced the difficulty, and added lives, start/end screens, a new background and hit effects.

The visual theme is inspired by **NieR: Automata**, mainly its hacking minigame and menu UI:

- **Palette:** a warm beige/parchment background (`#DAD4BB`, `#CDC8B0`) with dark brown-gray text, lines and shapes (`#4E4B42`, `#3A3830`).
- **Orange accent:** orange-red (`#E06A3A`) is used only for danger: enemy bullets, hits and the "game over" marker.
- **Menus:** clean, letter-spaced uppercase text, thin lines with small square end-caps, and highlighted menu items in square brackets (`[ START ]`, `[ RETRY ]`) on a dark bar.
- **Hacking-minigame look:** a subtle grid backdrop and bursts of small square particles.

The ship, aliens, darts and bullets use flat, unlit materials in the palette colours, so they read as clean silhouettes against the beige grid.

## How to Run

- **Unity version:** 6000.6.7f1 (Universal Render Pipeline)
- **Scene:** `Assets/Scenes/SampleScene.unity`. Open it and press **Play**.

| Key | Action |
|---|---|
| Left / Right Arrows | Move the ship |
| Space | Fire a dart |
| Space or Enter | Start the game (start screen) |
| R or Enter | Retry (end screen) |

## Gameplay Summary

1. **Start screen.** The game opens on a NieR-style title screen showing the title, subtitle, a blinking `[ START ]` prompt and the controls. The game behind it is frozen.
2. **Play.**
   - **The fleet:** a 10 × 3 fleet moves side to side and steps down each time it reaches an edge. It speeds up and fires more often as aliens are destroyed. Only the lowest alien in each column fires, and some shots are aimed at the player's column.
   - **The player:** you have 3 lives and up to 2 darts on screen at once. When you're hit, the ship blinks and is invulnerable for 1.5 seconds.
3. **Win or lose.**
   - **Win:** destroy all 30 aliens.
   - **Lose:** run out of lives, or let the aliens descend to the ship's line.

   Either way the game freezes and an end-screen panel shows the result, aliens destroyed and lives remaining.
4. **Retry.** Press R or Enter to reload the scene and start a new game straight away (the start screen is skipped on retry).

---

## Required Features

All tuning values below are `[SerializeField]` fields that can be changed in the Inspector. Inspector field names are in **bold**.

### Alien Speed-Up

The fleet's sideways speed increases as aliens are destroyed, as in the arcade original.

- **Where it's triggered:** `AlienFleet.RemoveAlien()` runs whenever an alien is destroyed and calls `UpdateDifficulty()`, which recalculates the speed from the number of aliens left.
- **Formula:**

  ```
  t          = 1 − (aliensLeft − 1) / (startingAliens − 1)   // 0 with the full fleet, 1 with one alien left
  multiplier = lerp(1, maxSpeedMultiplier, t ^ speedUpExponent)
  speed      = baseSpeed × multiplier
  ```

- **Why the exponent:** an exponent above 1 saves more of the speed-up for the last few aliens, so the final alien is noticeably fast.
- **Fixed step-down distance:** the fleet steps down at **Base Speed**, not at the current speed, so each drop is the same distance (0.5 units) however fast the fleet is moving. Otherwise a fast fleet would drop several rows per bounce.

| Aliens left | 30 | 15 | 5 | 1 |
|---|---|---|---|---|
| Multiplier | 1.00× | 2.49× | 4.20× | 5.00× |
| Sideways speed (units/sec) | 5.0 | 12.4 | 21.0 | 25.0 |

| Inspector field (`AlienFleet`) | Value |
|---|---|
| **Base Speed** | 5 units/sec |
| **Max Speed Multiplier** | 5 |
| **Speed Up Exponent** | 1.5 |

### Lose Detection (and Win Condition)

There are two ways to lose and one way to win.

**1. Aliens reach the ship (instant game over, whatever the lives).**

- Every physics step, `AlienFleet` finds the bottom of the lowest alien (`Alien.BottomY`) and compares it with a line just above the top of the ship (`Spaceship.TopY` + **Lose Line Offset**).
- Both values are measured from the objects' actual meshes, so the line doesn't depend on hard-coded heights.
- If the alien reaches the line, `GameManager.GameOver()` is called. At that point the player can no longer win, and the offset stops the aliens from actually touching the ship.

**2. Bullet hits with lives.**

1. An alien bullet hitting the ship calls `Spaceship.Hit()`, which ignores the hit if the ship is still invulnerable from a previous one.
2. Otherwise it plays the hit effect and calls `GameManager.PlayerHit()`, which removes a life and raises the `LivesChanged` event so the display and screen flash update.
3. At 0 lives it calls `GameOver()`. If lives remain, the ship blinks and is invulnerable for **Invulnerable Duration**.
4. The bullet is always destroyed; the ship never is.

**Win condition.** When `AlienFleet.RemoveAlien()` removes the last alien, it calls `GameManager.Win()`.

**End of game.** `GameOver()` and `Win()` both freeze the game (`Time.timeScale = 0`) and switch GameManager to the Lost or Won state, which shows the end screen. Repeat calls are ignored, for example when the last alien dies on the same step the fleet lands.

| Inspector field | Value |
|---|---|
| `AlienFleet` → **Lose Line Offset** | 0.5 units above the top of the ship |
| `GameManager` → **Starting Lives** | 3 |
| `Spaceship` → **Invulnerable Duration** | 1.5 s |

### Ship Screen Clamp

The ship is kept fully on screen, using the camera rather than hard-coded numbers.

- **Finding the edges:** every frame, `Spaceship.ScreenEdgeX()` casts a ray from the camera through the left and right screen edges (at the ship's screen height) and intersects it with the plane the ship moves in. This gives the world x of each edge, works for orthographic and perspective cameras, and adapts if the window is resized.
- **The lopsided ship:** the ship's mesh isn't centred on its pivot. It is three capsules on child objects, extending about 1.65 units to the left of the pivot but only about 1.0 to the right.
- **Measuring it:** `MeasureExtents()` combines the bounds of all child renderers once (in `Awake`) and stores separate left, right and top distances.
- **Clamping:** the ship's x is clamped between `leftEdge + leftExtent` and `rightEdge − rightExtent`, so neither side can go off-screen.
- **Resizing:** the clamp runs every frame, not just while moving, so resizing the Game view can't leave the ship off-screen.

| Inspector field (`Spaceship`) | Value |
|---|---|
| **View Camera** | Empty, so it uses the Main Camera |
| **Speed** | 10 units/sec |

### Code Issues

A review of the original gameplay scripts in `Assets/`. These issues were fixed in C# only; no scenes, prefabs or `.meta` files were changed, and gameplay behaved the same as before. **Line numbers refer to the original scripts.** Where later work changed something again, a *Later* note says so.

#### Fixed: 1. UI text rebuilt every frame

**Files:** `AlienFleet.cs:39-42`, `AlienCounter.cs:17-20`

**Problem:** `AlienFleet.Update()` copied `aliens.Count` into `AlienCounter.score` every frame. `AlienCounter.Update()` then built a new string (`"Remaining Aliens: " + score.ToString(...)`) and assigned it to the TextMeshPro text, also every frame.

**Why it was bad:** The count only changes when an alien dies, yet the code allocated a new string every frame (garbage the GC has to collect) and made TextMeshPro regenerate its mesh 60+ times a second for no reason.

**Change:**
- `AlienCounter` now has a `SetRemaining(int remaining)` method and no `Update()`. `AlienFleet` calls it once after spawning, then only when an alien is actually removed. `AlienFleet.Update()` was deleted.
- `AlienCounter` now gets its `TextMeshProUGUI` in `Awake()` instead of `Start()`, so the text component is ready before `AlienFleet.Start()` makes the first call.
- `SetRemaining` also repeats the lookup if it's still `null`, so it doesn't depend on `Awake` having run (for example, if the counter's GameObject starts inactive).

#### Fixed: 5. Movement measured per physics step, not per second

**Files:** `Dart.cs:23`, `AlienBullet.cs:23`, `AlienFleet.cs:52,56`, `Alien.cs:30`

**Problem:** Speeds were fixed distances added every `FixedUpdate` (`.3`, `.1`, `.1`). The alien's fire chance (`.002`) was likewise rolled once per physics step.

**Why it was bad:** Game speed depended on the physics timestep. Changing **Project Settings > Time > Fixed Timestep** would have changed how fast everything moved and how often aliens fired. Unity's convention is to give values in units per second and scale them by the step length.

**Change:** Values are now per second and multiplied by `Time.fixedDeltaTime`. The project's timestep is 0.02 s (50 steps/sec), so each old value was multiplied by 50 to keep the same feel:

| Value | Old (per step) | New (per second) |
|---|---|---|
| Dart speed | .3 | 15 |
| AlienBullet speed | .1 | 5 |
| AlienFleet speed | .1 | 5 |
| Alien fire chance | .002 | .1 |

`Rigidbody.MovePosition` was considered and rejected; see [Technical Notes](#why-rigidbodymoveposition-wasnt-used).

*Later:* Balancing raised the alien bullet speed to 9. The per-alien fire chance was replaced by a fleet-wide fire rate (see [Balancing](#balancing)), which is also per second and scaled the same way.

#### Fixed: 6. `static` tuning values hidden from the Inspector

**Files:** `Alien.cs:10-12`, `AlienBullet.cs:5-7`, `Dart.cs:5-6`, `Spaceship.cs:9-10`

**Problem:** Tuning values (`speed`, `shotCooldown`, `bulletSpawnRate`) were `static`, and several sat under `[Header("Inscribed")]`. Meanwhile `lastShot`, which is internal runtime state, was `public`.

**Why it was bad:** Unity doesn't serialize static fields, so these values never appeared in the Inspector and the header labelled nothing. A `public` `lastShot` showed up in the Inspector and could be changed by any script, even though nothing outside the class should touch it.

**Change:** The tuning values are now `[SerializeField] private` instance fields, adjustable per prefab in the Inspector. `lastShot` is now `private`. Every tuning value added since follows the same pattern.

#### Fixed: 8. Redundant `GetComponent` and reaching into the fleet's list

**Files:** `Alien.cs:39`, `AlienFleet.cs:13`

**Problem:** `fleet.GetComponent<AlienFleet>().aliens.Remove(gameObject)`. `fleet` was already an `AlienFleet`, and the alien was editing the fleet's public list directly.

**Why it was bad:** The `GetComponent` call did a lookup that returned the same object. Letting another class edit the list directly meant the fleet couldn't react to removals, such as updating the counter.

**Change:**
- Added `public void RemoveAlien(Alien alien)` to `AlienFleet`, which removes the alien and updates the counter. `Alien` now calls `fleet.RemoveAlien(this)`.
- The list is now `private List<Alien>` instead of `public List<GameObject>`.
- If two darts hit the same alien in one frame, `Remove` returns `false` the second time and the counter isn't updated twice.

*Later:* `RemoveAlien` became the single place where speed-up, fire rate, the kill count and the win condition are updated.

#### Extra cleanup: empty `Start()` / `Update()` methods

**Files:** `Alien.cs:15-24`, `AlienBullet.cs:9-18`, `Dart.cs:9-18`, `Spaceship.cs:14-17`

**Problem / why:** Unity calls `Update()` every frame on every script that defines it, even when it's empty. That's wasted overhead for 30 aliens plus every live bullet.

**Change:** Removed the empty methods and their template comments.

#### Extra cleanup: unused code

- Removed unused `using UnityEngine.InputSystem;` (`AlienFleet.cs:3`) and `using UnityEngine.UI;` (`AlienCounter.cs:2`).
- Removed the unused `lowerBound` field (`AlienFleet.cs:18`) and the commented-out block that referenced it (`AlienFleet.cs:57-61`).
- `Alien.FixedUpdate` read `transform.position` into a local variable every step even when it didn't fire (`Alien.cs:28`). It now reads it only when spawning a bullet. *Later:* aliens no longer have a `FixedUpdate`, because the fleet decides when they fire.

#### Found but not fixed

These were left alone, either because they don't cause a visible problem at this game's scale or because fixing them would have meant restructuring more than the assignment called for.

- **No object pooling** (`Alien.cs:31`, `Spaceship.cs:37`, `Dart.cs:27`, `AlienBullet.cs:27`): bullets are created and destroyed constantly. `UnityEngine.Pool.ObjectPool<T>` could reuse them. Separately, `Instantiate` is followed by setting the position, where `Instantiate(prefab, position, rotation)` would do both at once. *Not fixed:* with at most a few bullets per second, the cost is negligible, and pooling adds a lot of code.
- **Two passes over the fleet every step** (`AlienFleet.cs:47-79`): moving the aliens and finding the left and right edges could share one loop. Alternatively, the aliens could be parented to the fleet so it only moves one transform. `left`/`right` start at `0` rather than real min/max values. *Not fixed:* 30 aliens is tiny. Parenting would change how the fleet moves, and the `0` start values happen to give correct results here.
- **String-based `Invoke`** (`AlienFleet.cs:84`): `Invoke("startSidewaysMotion", ...)` breaks silently if the method is renamed. Use `nameof` or a coroutine. The method name also isn't PascalCase. *Not fixed:* it works correctly, and it was left as the professor wrote it.
- **Magic numbers:** the grid spacing and start position in `AlienFleet.Start` (the number of columns and rows is now set in the Inspector), and the ±10 off-screen limits in `Dart` and `AlienBullet`.
- **Spaceship input** (`Spaceship.cs:22-33`): the left and right code is duplicated, and the unused `InputSystem_Actions` asset is ignored. The 0.05 s cooldown that did almost nothing was replaced by a dart limit and a 0.25 s cooldown (see [Balancing](#balancing)). Keeping the ship on screen and handling a missing keyboard were fixed later, along with the core mechanics.
- **Collision filtering in code** (`Alien.cs:41`, `AlienBullet.cs:34`): both handlers destroyed whatever they touched. This was fixed later, along with the core mechanics: aliens now react only to darts, and alien bullets report a hit to the ship instead of destroying it.

### Balancing

**The problem:** the original game was too easy, for four reasons.

1. **Slow bullets.** Alien bullets moved at 5 units/sec, taking about 2.4 s to fall from the bottom alien row to the ship, so the player had plenty of time to dodge.
2. **Unlimited shooting.** The player could fire as fast as they could press space (the 0.05 s cooldown did almost nothing), so spraying darts beat aiming.
3. **Easier as you go.** Each alien fired on its own (0.1 shots/sec each), so **the fleet's total fire rate *fell* as aliens died**: from 3 shots/sec with 30 aliens down to 0.1 shots/sec with one. The end game, which should be the hardest part, was the safest.
4. **Shots from anywhere.** Back-row aliens could fire too, with bullets passing through the aliens in front of them. The shots were random and didn't force the player to react to the front line.

These values were chosen by working through the numbers, not by extensive playtesting. They are a starting point to tune from in the Inspector.

#### Before / after

| Setting (script → Inspector field) | Original | New | Why |
|---|---|---|---|
| Which aliens can fire (`AlienFleet`) | Any alien, each rolling its own chance | Only the lowest alien in each column | As in the arcade original. The front line becomes the threat, and clearing a column's bottom alien moves its fire up a row, which the player can see and plan around. The fleet now controls firing, and `Alien.Fire()` just spawns the bullet. |
| Fleet fire rate (`AlienFleet` → **Base Fire Rate** / **Max Fire Rate**) | 0.1/sec per alien: 3/sec with 30 aliens, **falling** to 0.1/sec with 1 | 1.5/sec with the full fleet, **rising** to 3.5/sec with 1 alien left | Fixes the backwards curve, so pressure builds as the player gets closer to winning. The starting rate is lower than before, but bullets are faster and some are aimed (next two rows), so early shots are more threatening. With the old settings about 0.4 bullets/sec landed in the ship's path; now about 0.65/sec do, arriving twice as fast. |
| Aimed shots (`AlienFleet` → **Aimed Shot Chance**) | None (all random) | 35% of shots come from the column nearest the player | Random shots mostly land far from the ship. Aimed shots stop the player from parking in one spot and shooting, without making every shot a guaranteed threat. |
| Alien bullet speed (`AlienBullet` → **Speed**) | 5 units/sec | 9 units/sec | Cuts the time from bottom row to ship from about 2.4 s to about 1.3 s. That's still enough time to react, since the ship moves 10 units/sec and is about 2.7 units wide. |
| Darts on screen (`Spaceship` → **Max Darts On Screen**) | Unlimited | 2 | Makes accuracy matter. A miss ties up a slot for about 1.1 s while the dart flies off the top. Two (rather than the original arcade's one) allows a quick follow-up shot, keeping the game beatable. Set it to 1 for classic difficulty. |
| Shot cooldown (`Spaceship` → **Shot Cooldown**) | 0.05 s | 0.25 s | Stops both darts going out in one burst at the same spot. |
| Speed-up curve (`AlienFleet` → **Max Speed Multiplier** / **Speed Up Exponent**) | 6× at the last alien, exponent 2 | 5× at the last alien, exponent 1.5 | The old curve stayed slow for most of the game and then spiked. With limited darts, hitting a 30 units/sec last alien was mostly luck. The new curve speeds up earlier and peaks lower. |
| Lives (`GameManager` → **Starting Lives**) | None (one hit was a game over) | 3 lives, with 1.5 s of blinking invulnerability after each hit (`Spaceship` → **Invulnerable Duration**) | Offsets the harder firing, so a single unlucky bullet doesn't end a run. Invulnerability stops one burst from taking several lives at once. The aliens reaching the lose line is still an instant game over. |

#### Fleet sideways speed (units/sec) by aliens remaining

| Aliens left | 30 | 20 | 10 | 5 | 1 |
|---|---|---|---|---|---|
| Original (6×, exponent 2) | 5.0 | 8.0 | 16.9 | 23.6 | 30.0 |
| New (5×, exponent 1.5) | 5.0 | 9.1 | 16.5 | 21.0 | 25.0 |

#### Fleet shots per second by aliens remaining

| Aliens left | 30 | 20 | 10 | 5 | 1 |
|---|---|---|---|---|---|
| Original (0.1 per alien) | 3.0 | 2.0 | 1.0 | 0.5 | 0.1 |
| New (1.5 → 3.5) | 1.5 | 2.2 | 2.9 | 3.2 | 3.5 |

#### If it turns out too hard or too easy

- **Easier:** set **Max Darts On Screen** to 3, lower **Aimed Shot Chance**, or add a life.
- **Harder:** set **Max Darts On Screen** to 1, or raise the alien bullet **Speed** to about 11.
- The fleet's step down per bounce (0.5 units) and the lose line didn't change. Because the new curve makes the fleet faster in the mid-game, it bounces (and so steps down) a little more often around 20 aliens left, which adds some time pressure.

---

## Optional Features

### Lives

**What the player sees:**
- **"LIVES: 3"** in the top-right corner, in the NieR text style (dark, uppercase, letter-spaced, with a thin underline).
- **On a hit:** an orange burst at the ship and an orange screen flash. The counter drops by one, and the ship blinks and can't be hit for 1.5 seconds.
- **At 0 lives:** the end screen appears.

**How it works:**
- **GameManager:** `GameManager` stores `Lives` (set from **Starting Lives** in `Awake`) and raises a `LivesChanged` event in `PlayerHit()`.
- **Display:** `LivesDisplay` subscribes to `LivesChanged` and formats the text with its **Format** field (`"LIVES: {0}"`). It only sets a string, so it can be restyled freely or replaced by an icon display that listens to the same event.
- **Invulnerability:** `Spaceship` handles it with a coroutine that toggles the ship's child renderers every **Blink Interval** (0.1 s) for **Invulnerable Duration** (1.5 s). It also listens to `StateChanged`, so if the game ends mid-blink the ship is left visible.

### Start / End Screens

**What the player sees:**

| Screen | Contents |
|---|---|
| **Start** | <ul><li>A 90% beige overlay over the frozen game, inside a thin frame</li><li>A small "YORHA // COMBAT SIMULATION SYSTEM" header</li><li>Large letter-spaced "SPACE INVADERS" between two thin lines with square end-caps</li><li>The subtitle "YoRHa Unit: Combat Simulation"</li><li>A dark highlighted `[ START ]` bar with a blinking square marker</li><li>"PRESS SPACE OR ENTER", and the controls along the bottom</li></ul> |
| **End** | <ul><li>The game frozen behind a dimmed overlay</li><li>A centred panel with a dark "RESULT" header bar</li><li>**YOU WIN** or **GAME OVER**, underlined in dark ink for a win or orange for a loss</li><li>**Aliens Destroyed** (e.g. "12 / 30") and **Lives Remaining**</li><li>A highlighted `[ RETRY ]` bar, with "PRESS R OR ENTER"</li></ul> |

**How it works:**
- **Built in code:** `NierScreens` (on the Canvas) builds both screens in `Awake` using helpers from `NierStyle`, and shows or hides them when GameManager raises `StateChanged`.
- **Frozen game:** `GameManager` starts in a `Title` state with `Time.timeScale = 0`, so nothing moves until Space or Enter calls `StartGame()`.
- **Start key:** this input is read in `LateUpdate`, after the ship's `Update`, so the Space press that starts the game doesn't also fire a dart.
- **Retry:** on the end screen, R or Enter calls `Retry()`, which reloads the scene (see [Technical Notes](#domain-reload-handling)).
- **Blinking and flashes:** the blinking marker and screen flash use unscaled time, so they still animate while the game is frozen.
- **Text:** all screen text is in Inspector fields. The game uses TextMesh Pro's default font, and a different font can be assigned in **Font**.
- **Layout:** the Canvas uses **Scale With Screen Size** (1920 × 1080 reference), so the layout scales with the window.

### Background

**What the player sees:** a flat parchment-beige backdrop (`#DAD4BB`) with a faint grid: thin lines every world unit and a slightly darker line every 5 units, like the hacking minigame. This replaces the default skybox. The HUD texts are restyled to match: dark ink, uppercase, letter-spaced, with thin underlines.

**How it works:**
- **Camera:** `NierBackground` (on the Main Camera) sets the camera to clear to a solid beige.
- **Grid texture:** the script generates a 160 × 160 px tile in code (5 × 5 cells, with lines blended 8% / 20% toward the ink colour).
- **Quad:** the texture is applied to a large quad parented to the camera, 30 units in front of it and behind the playfield, sized to cover the view at any aspect ratio up to 4:1.
- **Material:** it uses the `Nier_Unlit` material (URP Particles/Unlit), so the beige isn't changed by lighting.
- **Post-processing:** the script turns off the camera's post-processing at runtime, because the scene's tonemapping, bloom and dark vignette visibly shifted the palette.
- **HUD:** `NierTextStyle` restyles the existing counter and lives texts.

| Inspector field (`NierBackground`) | Value |
|---|---|
| **Cell Size** | 1 unit |
| **Major Line Every** | 5 cells |
| **Minor / Major Line Strength** | 0.08 / 0.2 |
| **Disable Post Processing** | On |

### Collision Animations

**What the player sees:**

| Event | Effect |
|---|---|
| Alien destroyed | A pale diamond flash, plus a burst of 12 dark and 4 orange spinning squares that shrink away |
| Ship hit | A larger orange diamond flash, plus 18 orange and 8 dark squares, and a brief orange flash over the whole screen |

**How it works:**
- **Built in code:** `HitEffects` creates two particle systems entirely in code: one for the square debris and one for the flash. They use the `Nier_Unlit` material and don't emit on their own.
- **Triggering:** `Alien.OnCollisionEnter` and `Spaceship.Hit` call `PlayAlienDestroyed` / `PlayShipHit`, which move the system to the hit position and emit a set number of particles in each colour.
- **Debris:** debris particles live 0.3–0.6 s, fly out at 3–9 units/sec, spin, and shrink to nothing.
- **Flash:** the flash is a single large square turned 45° that shrinks away in 0.12 s.
- **Unscaled time:** the effects run on unscaled time, so the final hit still animates on the frozen end screen.
- **Screen flash:** the full-screen flash is drawn by `NierScreens` when lives change: orange at 35% opacity, fading over 0.25 s.

| Inspector field (`HitEffects`) | Value |
|---|---|
| **Alien Debris** / **Alien Accent Debris** | 12 / 4 |
| **Alien Flash Size** | 1.6 |
| **Ship Debris** / **Ship Dark Debris** | 18 / 8 |
| **Ship Flash Size** | 3 |

---

## Script Overview

All scripts are in `Assets/`.

| Script | Description |
|---|---|
| `GameManager.cs` | Singleton state machine (Title → Playing → Won/Lost). Holds lives and the kill count, raises `StateChanged` and `LivesChanged`, handles start and retry input, and reloads the scene on retry. |
| `AlienFleet.cs` | Spawns the alien grid and moves it side to side and down. Applies the speed-up and fire-rate curves, decides which alien fires, and checks the lose line and the win condition. |
| `Alien.cs` | A single alien. Fires a bullet when the fleet tells it to, reports its bottom edge for the lose line, and dies (with an effect) only when hit by a dart. |
| `AlienBullet.cs` | Moves down at **Speed**. On hitting the ship it calls `Spaceship.Hit()`; on hitting anything else (a dart) it destroys it. It always destroys itself. |
| `Spaceship.cs` | Player movement, screen clamp, firing (dart limit and cooldown), and taking hits with blinking invulnerability. |
| `Dart.cs` | The player's projectile. Moves up at **Speed** and is destroyed off-screen. |
| `AlienCounter.cs` | Shows "Remaining Aliens" on the HUD. Updated by the fleet only when the count changes. |
| `LivesDisplay.cs` | Shows "LIVES: n" on the HUD by listening to `GameManager.LivesChanged`. |
| `NierScreens.cs` | Builds the NieR-styled start screen, end screen and hit flash at runtime, and shows them based on game state. |
| `NierStyle.cs` | Static NieR palette, plus helpers for building UI in code (text, images, frames, highlighted menu items). |
| `NierTextStyle.cs` | Restyles an existing TextMeshPro text to the NieR look (colour, uppercase, letter spacing, underline). |
| `NierBackground.cs` | Replaces the skybox with a solid beige background and a generated grid, and turns off the camera's post-processing. |
| `HitEffects.cs` | Singleton that builds and plays the square-particle bursts and flashes for alien deaths and ship hits. |
| `TutorialInfo/Readme.cs`, `TutorialInfo/Editor/ReadmeEditor.cs` | Unity project-template scripts; not part of the game. |

---

## Technical Notes

### GameManager as the central state machine

`GameManager` owns the game's flow through a `GameState` enum: **Title**, **Playing**, **Won** and **Lost**.

- **Gameplay scripts report events:**
  - `AlienFleet` reports aliens destroyed, the fleet being cleared, and the aliens landing.
  - `Spaceship` reports being hit.
- **GameManager decides the outcome.** It handles losing a life, ending the game, freezing time, and starting or retrying.
- **Other scripts check the state.** For example, the ship ignores input unless `IsPlaying` is true.

This keeps the rules in one place, so adding levels, a pause state or more screens means changing GameManager and adding UI, not editing every gameplay script.

### Events

GameManager exposes two C# events. UI scripts subscribe in `Start` (after GameManager's `Awake` has set everything up) and unsubscribe in `OnDestroy`.

| Event | Raised when | Listeners |
|---|---|---|
| `StateChanged` | The game starts, is won or is lost | `NierScreens` (shows/hides screens), `Spaceship` (stops blinking at game end) |
| `LivesChanged` | The player loses a life | `LivesDisplay` (updates text), `NierScreens` (screen flash) |

Each listener keeps its own reference to the GameManager it subscribed to, so it can unsubscribe safely even after `GameManager.Instance` has been cleared.

### Domain reload handling

The project has **Enter Play Mode Options** turned on with **domain reload disabled**. This makes entering Play mode faster, but static fields keep their values from the previous Play session.

- **Resetting statics:** every mutable static field is reset in a `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]` method, which Unity runs at the start of every Play session:
  - `GameManager.Instance`
  - `GameManager.skipTitleOnLoad`
  - `HitEffects.Instance`
- **Palette colours:** the colours in `NierStyle` are `static readonly` and never change, so they don't need resetting.
- **Retry:**
  - `Retry()` reloads the scene, which recreates every object. It sets the static `skipTitleOnLoad` flag (so the new scene goes straight to Playing) and resets `Time.timeScale` to 1 before loading.
  - The new GameManager's `Awake` then resets `Lives`, `State`, the kill count and `Time.timeScale`.
  - `OnDestroy` also restores `Time.timeScale` to 1, so stopping Play mode on a frozen screen doesn't leave the Editor frozen.
- **Singletons during a reload:** the old scene's copy may still exist briefly. `GameManager` and `HitEffects` only treat an existing `Instance` as a duplicate if it's in the same scene; otherwise the new one takes over.

### Why `Rigidbody.MovePosition` wasn't used

The aliens, darts and bullets have Rigidbodies but are moved by setting `transform.position`.

- **What MovePosition needs:** it is meant for kinematic Rigidbodies.
- **Why that breaks collisions:** with the project's default physics contact-pairs setting, two kinematic bodies never send `OnCollisionEnter`. Making the darts, bullets and aliens kinematic would stop hits from registering. Fixing that would mean changing Project Settings.
- **Why tunnelling isn't a risk:** at 0.02 s per step, the fastest object (the dart at 15 units/sec) moves 0.3 units per step, which is smaller than the colliders. So objects can't pass through each other without a hit registering.

### Collision layers

Which objects can hit each other is set by physics layers in the layer collision matrix:

| Layer | Used by | Doesn't collide with |
|---|---|---|
| Spaceship | Ship | Darts |
| Alien | Aliens | Alien bullets |
| Bullet | Darts | Ship |
| Alien_Bullet | Alien bullets | Aliens |

The collision scripts also check what they hit: aliens only react to darts, and alien bullets call `Hit()` on the ship instead of destroying it.
