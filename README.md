# Space Invaders: YoRHa Combat Simulation

Space Invaders reimagined as a NieR: Automata-inspired prequel: the final combat certification of Unit 11S, a newly activated android, aboard the Bunker before deployment to Earth.

The game started as my CSCI 381 Space Invaders assignment, built on my professor's in-class project, and was expanded afterwards with a story, three levels, a support Pod and new visuals. Every model in the game (ship, enemies, shots, Pod, laser) is original and built in code from primitives and procedural meshes, so the project needs no external art assets. This README is self-contained.

## Contents

1. [How to Run](#how-to-run)
2. [Story and Characters](#story-and-characters)
3. [Gameplay](#gameplay)
4. [Visual Design](#visual-design)
5. [Assignment Requirements](#assignment-requirements)
   - [Alien Speed-Up](#alien-speed-up)
   - [Lose Detection (and Win Condition)](#lose-detection-and-win-condition)
   - [Ship Screen Clamp](#ship-screen-clamp)
   - [Code Issues](#code-issues)
   - [Balancing](#balancing)
   - [Optional Features](#optional-features)
6. [Script Overview](#script-overview)
7. [Technical Notes](#technical-notes)
8. [Development Notes](#development-notes)

---

## How to Run

- **Unity version:** 6000.6.7f1 (Universal Render Pipeline)
- **Scene:** `Assets/Scenes/SampleScene.unity`. Open it and press **Play**.

| Key | Action |
|---|---|
| Left / Right Arrows | Move the ship |
| Space | Fire a dart |
| E or Left Shift | Fire Pod Program: Laser (when charged) |
| Space or Enter | Start; continue through briefings, debriefs and dialogue pages |
| Any key | Finish the dialogue currently typing |
| R or Enter | Retry after a failure |

---

## Story and Characters

**Premise.** Unit 11S is a YoRHa Type S (Scanner) android, newly activated aboard the Bunker, YoRHa's orbital base. Before deployment to Earth, 11S has to pass three combat simulations against simulated machine lifeforms. The Commander reads the logs but is never named or seen. No canon characters appear.

**Characters.**
- **Operator 17O**, the O-type support unit monitoring the certification. Professional on the surface, with small human slips: she says more than she should, then tries to take it back.
- **Pod 217**, 11S's support Pod, which floats beside the ship. It speaks only in flat, literal, tagged lines ("Report:", "Analysis:", "Proposal:", "Query:", "Alert:", "Correction:", "Negative.", "Affirmative."). It never shows emotion; its humour is deadpan, usually by being far too literal.

**The three simulations.**
- **SIM 01: Basic Combat.** An introduction to the targets, the Pod and its laser.
- **SIM 02: Advanced Combat.** Faster and more aggressive, built from combat data recorded on the surface before 11S was activated. The Pod asks a question the Operator would rather not answer.
- **SIM 03: Anomaly.** A simulation that wasn't on the schedule, with enemy data that doesn't match any training record. Something about it is wrong, and the enemies look like it.

**Tone.** The story hints rather than explains. Questions go unanswered, records are restricted, and "within parameters" comes up more than once. Before and after each simulation there's a short exchange between the Operator and the Pod. The game ends with the certification, the Operator's closing words and a quiet epilogue.

---

## Gameplay

### Flow

```
Title -> Briefing -> Simulation -> Debrief -> next Briefing -> ... -> SIM 03 -> Certification -> Epilogue -> Title
                         |
                      failure -> Retry (straight back into the same simulation)
```

1. **Title.** The game behind it is frozen until Space or Enter.
2. **Briefing.** A NieR-style panel headed with the simulation's name (e.g. "SIM 01: BASIC COMBAT"). It shows a formal system line, then the Operator–Pod conversation typing out line by line.
3. **Simulation.** Clear every target to pass. The Pod comments during play (see [Pod commentary](#pod-commentary)).
4. **Debrief.** "SIMULATION COMPLETE", targets destroyed, lives remaining and "UNIT REPAIRED.", then a short conversation. Continue to the next briefing.
5. **Failure.** "SIMULATION FAILED" / "Unit will be recalibrated." with `[ RETRY ]`. SIM 03 has its own failure screen: "SIMULATION INTEGRITY COMPROMISED", with a glitch effect and `[ REINITIALIZE ]`. Retrying restarts the same simulation with full lives.
6. **Certification.** After SIM 03: "CERTIFICATION COMPLETE", the run's totals and a rank, then a conversation that depends on the rank.
7. **Epilogue.** A black screen with a few quiet lines, then any key returns to the title.

### Lives and run stats

- **Lives:** every simulation starts with **3 lives**, because the unit is repaired between simulations. A hit costs one life, and the ship then blinks and can't be hit for 1.5 s. Losing the last life fails the simulation.
- **Instant failure:** the aliens reaching the ship's line fails the simulation however many lives are left.
- **Run stats:** the run tracks **lives lost**, **retries** and **targets destroyed** across all three simulations, including attempts that were later retried. They reset when a new run starts from the title.
- **Rank:** the run gets the first rank whose limits it's within:

| Rank | Max lives lost | Max retries |
|---|---|---|
| S | 1 | 0 |
| A | 3 | 0 |
| B | 8 | 2 |
| C | any | any |

The thresholds and each rank's dialogue are Inspector fields on GameManager.

### Pod Program: Laser

- **Charging:** every target destroyed charges the Pod. After **8 kills** the meter is full and pulses orange, and the Pod announces it.
- **Firing:** E or Left Shift fires a 0.6 s beam straight up from the Pod. It's bright, with an orange core, and slightly shakes the screen. It destroys every alien in its path, each with a hit effect.
- **Charge rules:** kills made by the laser don't recharge it, and the charge resets each simulation.

### Alien shots

| Shot | Look | Behaviour |
|---|---|---|
| **Orange** | Spinning orange diamond with a light centre | Destructible: a dart that hits it destroys both |
| **Dark** | Dark diamond with a thin orange outline | Indestructible: it destroys the dart and keeps going, so it must be dodged |

Each level sets the share of dark shots. Alien shots pass through each other.

### The anomaly (SIM 03)

- **Corrupted enemies:** SIM 03's enemies are corrupted versions of the normal shapes, each with a piece knocked out of place and often one missing. Every few seconds one briefly jitters or flickers.
- **Commentary:** the Pod makes occasional uneasy remarks about the simulation.
- **Failure screen:** it has its own title, glitch effect and dialogue.

### Levels

| | SIM 01: Basic Combat | SIM 02: Advanced Combat | SIM 03: Anomaly |
|---|---|---|---|
| Grid | 10 × 3 (30 targets) | 10 × 4 (40) | 10 × 4 (40) |
| Base speed (units/sec) | 5 | 6 | 7 |
| Max speed multiplier (last alien's speed) | 5× (25) | 4.5× (27) | 4.5× (31.5) |
| Speed-up exponent | 1.5 | 1.5 | 1.4 |
| Fire rate, full fleet → last alien (shots/sec) | 1.5 → 3.5 | 1.9 → 3.9 | 2.1 → 4.0 |
| Aimed shots | 35% | 45% | 50% |
| Alien bullet speed (units/sec) | 9 | 10 | 10.5 |
| Dark (indestructible) shots | 0% | 20% | 35% |
| Step down per bounce | 0.5 | 0.6 | 0.7 |
| Anomaly | No | No | Yes |

### Pod commentary

During play, a small box labelled **POD 217** appears above the Pod meter. A line types out quickly and hides after about 3.5 s. The Pod remarks on:

- the simulation starting
- the laser being ready
- laser shots that destroy 3 or more targets
- taking a hit, and reaching the last life
- the fleet passing 2.5× and 4× speed
- three targets left, and the final target
- the first dark shot in a level
- occasionally in SIM 03, the anomaly

---

## Visual Design

The look is based on NieR: Automata's hacking minigame and menus.

- **Palette:**
  - parchment beige `#DAD4BB` / `#CDC8B0`
  - dark brown-gray ink `#4E4B42` / `#3A3830`
  - a light hull colour `#F0EDE0`
  - orange-red `#E06A3A`, used only for enemy shots, hits, warnings and the Pod's labels
- **Background:** a flat beige backdrop with a faint grid generated in code, replacing the skybox.
- **UI:** letter-spaced uppercase text and thin lines with small square end-caps. Panels have dark header bars, and menu items are highlighted in brackets (`[ START ]`, `[ RETRY ]`) on a dark bar with a blinking square marker.
- **Unlit materials:** every model uses flat, unlit materials, so shapes read as clean silhouettes.

**Code-built models** (all original designs):

| Model | Design |
|---|---|
| Ship | A sharp light arrowhead with a dark outline, a dark spine and wing accents, and a small orange core with a pulsing halo |
| Alien, rows 1 and 4 | A dark cube inside a slowly turning ring of segments |
| Alien, row 2 | A dark octahedron spinning on its vertical axis, so its silhouette narrows and widens |
| Alien, row 3 | A spiked octagonal shell rotating slowly |
| Anomaly aliens (SIM 03) | The same shapes, corrupted: a piece displaced, often one missing, with short jitter and flicker glitches |
| Alien shots | Orange diamonds (destructible), or dark diamonds with an orange outline (indestructible) |
| Darts | Thin light projectiles with a dark edge and a short trail that tapers and blends into the background |
| Pod 217 | A small dark diamond drone with an orange eye, two side fins and a thruster, floating beside the ship |
| Laser | A bright beam with an orange core |

All aliens have a small orange eye and bob gently. Hits produce bursts of small square particles in the palette colours plus a brief flash.

---

## Assignment Requirements

This section covers the original CSCI 381 assignment: three core mechanics, four code issues, balancing and optional features. Where the later expansion changed something, a *Later* note says so. Tuning values are `[SerializeField]` fields, with Inspector names in **bold**.

### Alien Speed-Up

The fleet's sideways speed increases as aliens are destroyed, as in the arcade original.

- **Where it's triggered:** `AlienFleet.RemoveAlien()` runs whenever an alien is destroyed and calls `UpdateDifficulty()`. That recalculates the speed, and the fire rate, from the number of aliens left.
- **Formula:**

  ```
  t          = 1 - (aliensLeft - 1) / (startingAliens - 1)   // 0 with the full fleet, 1 with one alien left
  multiplier = lerp(1, maxSpeedMultiplier, t ^ speedUpExponent)
  speed      = baseSpeed * multiplier
  ```

- **Why the exponent:** an exponent above 1 saves more of the speed-up for the last few aliens, so the final alien is noticeably fast.
- **Fixed step-down distance:** the fleet steps down at **Base Speed**, not the current speed, so each drop is the same distance however fast the fleet is moving. Otherwise a fast fleet would drop several rows per bounce.

With SIM 01's values (Base Speed 5, Max Speed Multiplier 5, Speed Up Exponent 1.5):

| Aliens left | 30 | 15 | 5 | 1 |
|---|---|---|---|---|
| Multiplier | 1.00× | 2.49× | 4.20× | 5.00× |
| Sideways speed (units/sec) | 5.0 | 12.4 | 21.0 | 25.0 |

*Later:* these values now come from each level's LevelData (see [Levels](#levels)). AlienFleet's own Inspector values are used only if GameManager has no levels.

### Lose Detection (and Win Condition)

There are two ways to lose and one way to win.

**1. Aliens reach the ship (instant failure, whatever the lives).**
- **The check:** every physics step, `AlienFleet` finds the bottom of the lowest alien (`Alien.BottomY`) and compares it with a lose line just above the ship.
- **The line:** it's set **once when the level starts**, at the ship's top (`Spaceship.TopY`) plus **Lose Line Offset** (0.5 units). Both values are measured from the actual meshes, so nothing depends on hard-coded heights.
- **When it's crossed:** `GameManager.GameOver()` is called. At that point the player can no longer win, and the offset stops the aliens from actually touching the ship.
- *Later:* the line was originally calculated from the ship's live position every step. When a physics bug let bullet hits push the ship down off-screen (see [The Rigidbody fix](#the-rigidbody-fix)), the line sank with it, and the aliens descended forever without triggering a loss. The line is now a fixed Y value.

**2. Bullet hits with lives.**
1. An alien bullet hitting the ship calls `Spaceship.Hit()`. It ignores the hit if the ship is still invulnerable.
2. Otherwise it plays the hit effect and calls `GameManager.PlayerHit()`, which removes a life and raises `LivesChanged`.
3. At 0 lives, `GameOver()` is called. Otherwise the ship blinks and is invulnerable for **Invulnerable Duration** (1.5 s).
4. The bullet is destroyed; the ship never is.

**Win condition.** When `AlienFleet.RemoveAlien()` removes the last alien, it calls `GameManager.LevelCleared()`. That shows the debrief, or the certification after the final level. (In the assignment version this was `Win()`, which ended the game.)

**End of play.** Ending a simulation freezes the game (`Time.timeScale = 0`) and changes GameManager's state, which shows the matching screen. Repeat calls are ignored, for example when the last alien dies on the same step the fleet lands.

### Ship Screen Clamp

The ship is kept fully on screen using the camera, not hard-coded numbers.

- **Finding the edges:** every frame, `Spaceship.ScreenEdgeX()` casts a ray from the camera through the left and right screen edges (at the ship's screen height) and intersects it with the plane the ship moves in. This gives the world x of each edge, works with orthographic and perspective cameras, and adapts to window resizing.
- **The ship's width:** `MeasureExtents()` combines the bounds of the ship's renderers once (in `Awake`) and stores separate left, right and top distances from the pivot.
  - In the assignment version, this mattered because the ship was three capsules extending about 1.65 units left of the pivot but only 1.0 right.
  - *Later:* the code-built arrowhead is centred on the pivot (about 1.44 units each side), and the extents are now measured from that model.
- **Clamping:** the ship's x is clamped between `leftEdge + leftExtent` and `rightEdge - rightExtent`, every frame (not just while moving), so resizing the Game view can't leave it off-screen.

### Code Issues

A review of the original gameplay scripts. The four fixed issues below were selected from a longer review of nine issues. They were fixed in C# only; no scenes, prefabs or `.meta` files were changed, and gameplay behaved the same as before. **Line numbers refer to the original scripts.**

#### Fixed: 1. UI text rebuilt every frame

**Files:** `AlienFleet.cs:39-42`, `AlienCounter.cs:17-20`

**Problem:** `AlienFleet.Update()` copied `aliens.Count` into `AlienCounter.score` every frame. `AlienCounter.Update()` then built a new string (`"Remaining Aliens: " + score.ToString(...)`) and assigned it to the TextMeshPro text, also every frame.

**Why it was bad:** the count only changes when an alien dies, yet the code allocated a new string every frame (garbage the GC has to collect) and made TextMeshPro regenerate its mesh 60+ times a second for no reason.

**Change:**
- `AlienCounter` now has a `SetRemaining(int remaining)` method and no `Update()`. `AlienFleet` calls it once after spawning, then only when an alien is actually removed. `AlienFleet.Update()` was deleted.
- `AlienCounter` gets its `TextMeshProUGUI` in `Awake()` instead of `Start()`, so it's ready before `AlienFleet.Start()` makes the first call.
- `SetRemaining` repeats the lookup if it's still `null`, so it doesn't depend on `Awake` having run.

*Later:* the text comes from an Inspector **Format** field, now `"Targets Remaining: {0}"`.

#### Fixed: 2. Movement measured per physics step, not per second

**Files:** `Dart.cs:23`, `AlienBullet.cs:23`, `AlienFleet.cs:52,56`, `Alien.cs:30`

**Problem:** speeds were fixed distances added every `FixedUpdate` (`.3`, `.1`, `.1`). The alien's fire chance (`.002`) was also rolled once per physics step.

**Why it was bad:** game speed depended on the physics timestep. Changing **Project Settings > Time > Fixed Timestep** would have changed how fast everything moved and how often aliens fired. Unity's convention is to use units per second, scaled by the step length.

**Change:** values are per second and multiplied by `Time.fixedDeltaTime`. The timestep is 0.02 s (50 steps/sec), so each old value was multiplied by 50 to keep the same feel:

| Value | Old (per step) | New (per second) |
|---|---|---|
| Dart speed | .3 | 15 |
| AlienBullet speed | .1 | 5 |
| AlienFleet speed | .1 | 5 |
| Alien fire chance | .002 | .1 |

`Rigidbody.MovePosition` was considered and rejected; see [Technical Notes](#why-rigidbodymoveposition-isnt-used).

*Later:* balancing raised the alien bullet speed to 9, and each level now sets its own. The per-alien fire chance was replaced by a fleet-wide fire rate (see [Balancing](#balancing)), also per second.

#### Fixed: 3. `static` tuning values hidden from the Inspector

**Files:** `Alien.cs:10-12`, `AlienBullet.cs:5-7`, `Dart.cs:5-6`, `Spaceship.cs:9-10`

**Problem:** tuning values (`speed`, `shotCooldown`, `bulletSpawnRate`) were `static`, and several sat under `[Header("Inscribed")]`. Meanwhile `lastShot`, which is internal runtime state, was `public`.

**Why it was bad:** Unity doesn't serialize static fields, so these values never appeared in the Inspector and the header labelled nothing. A `public` `lastShot` showed up in the Inspector and could be changed by any script.

**Change:** the tuning values are `[SerializeField] private` instance fields, adjustable per prefab in the Inspector, and `lastShot` is `private`. Every tuning value added since follows the same pattern.

#### Fixed: 4. Redundant `GetComponent` and reaching into the fleet's list

**Files:** `Alien.cs:39`, `AlienFleet.cs:13`

**Problem:** `fleet.GetComponent<AlienFleet>().aliens.Remove(gameObject)`. `fleet` was already an `AlienFleet`, and the alien was editing the fleet's public list directly.

**Why it was bad:** the `GetComponent` call did a lookup that returned the same object. Letting another class edit the list meant the fleet couldn't react to removals, such as updating the counter.

**Change:**
- Added `public void RemoveAlien(Alien alien)` to `AlienFleet`, which removes the alien and updates the counter. `Alien` calls `fleet.RemoveAlien(this)`.
- The list is now `private List<Alien>` instead of `public List<GameObject>`.
- If two darts hit the same alien in one frame, `Remove` returns `false` the second time, so the counter isn't updated twice.

*Later:* `RemoveAlien` became the single place where speed-up, fire rate, kill counts and the level-cleared check happen. Every kill, whether by dart or laser, goes through `Alien.Die()`, which calls it.

#### Extra cleanup

- **Empty methods:** removed the empty `Start()` / `Update()` methods (`Alien.cs:15-24`, `AlienBullet.cs:9-18`, `Dart.cs:9-18`, `Spaceship.cs:14-17`). Unity calls `Update()` every frame on every script that defines it, even empty ones.
- **Unused `using` lines:** removed `using UnityEngine.InputSystem;` (`AlienFleet.cs:3`) and `using UnityEngine.UI;` (`AlienCounter.cs:2`).
- **Dead code:** removed the unused `lowerBound` field (`AlienFleet.cs:18`) and its commented-out block (`AlienFleet.cs:57-61`).
- **Wasted read:** `Alien.FixedUpdate` read `transform.position` every step even when not firing (`Alien.cs:28`). *Later:* aliens no longer have a `FixedUpdate`; the fleet decides when they fire.

#### Found but not fixed

- **No object pooling** (`Alien.cs:31`, `Spaceship.cs:37`, `Dart.cs:27`, `AlienBullet.cs:27`):
  - *Issue:* bullets are created and destroyed constantly; `UnityEngine.Pool.ObjectPool<T>` could reuse them.
  - *Not fixed:* at a few bullets per second the cost is negligible, and pooling adds a lot of code.
- **Two passes over the fleet every step** (`AlienFleet.cs:47-79`):
  - *Issue:* moving the aliens and finding the fleet's edges could share one loop. `left`/`right` start at `0` rather than real min/max values.
  - *Not fixed:* the fleet is at most 40 aliens, and the `0` start values happen to give correct results.
- **String-based `Invoke`** (`AlienFleet.cs:84`):
  - *Issue:* `Invoke("startSidewaysMotion", ...)` breaks silently if the method is renamed, and the name isn't PascalCase.
  - *Not fixed:* low risk; left unchanged.
- **Magic numbers:**
  - *Issue:* the grid spacing and start position in `AlienFleet.Start`, and the ±10 off-screen limits in `Dart` and `AlienBullet`.
  - *Partly fixed:* the column and row counts now come from each level.
- **Spaceship input** (`Spaceship.cs:22-33`):
  - *Issue:* the left and right code is duplicated, and the unused `InputSystem_Actions` asset is ignored.
  - *Fixed since:* the 0.05 s cooldown that did almost nothing was replaced by a dart limit and a 0.25 s cooldown. The ship also stays on screen and copes with a missing keyboard now.
- **Collision filtering in code** (`Alien.cs:41`, `AlienBullet.cs:34`):
  - *Issue:* both handlers destroyed whatever they touched.
  - *Fixed since:* aliens only react to darts, alien bullets report hits to the ship instead of destroying it, and bullets ignore each other.

### Balancing

**The problem:** the original game was too easy, for four reasons.

1. **Slow bullets.** Alien bullets moved at 5 units/sec, taking about 2.4 s to fall from the bottom row to the ship.
2. **Unlimited shooting.** The 0.05 s cooldown did almost nothing, so spraying darts beat aiming.
3. **Easier as you go.** Each alien fired on its own (0.1 shots/sec each), so **the fleet's total fire rate *fell* as aliens died**: from 3 shots/sec with 30 aliens to 0.1 shots/sec with one. The end game, which should be the hardest part, was the safest.
4. **Shots from anywhere.** Back-row aliens could fire too, with bullets passing through the aliens in front. The shots were random and didn't force the player to react to the front line.

These values were chosen by working through the numbers, not by extensive playtesting.

#### Before / after (assignment version)

| Setting (script → Inspector field) | Original | New | Why |
|---|---|---|---|
| Which aliens can fire (`AlienFleet`) | Any alien, each rolling its own chance | Only the lowest alien in each column | As in the arcade original. The front line becomes the threat, and clearing a column's bottom alien moves its fire up a row. The fleet decides when and which alien fires; `Alien.Fire()` just spawns the bullet. |
| Fleet fire rate (**Base Fire Rate** / **Max Fire Rate**) | 0.1/sec per alien: 3/sec with 30 aliens, **falling** to 0.1/sec with 1 | 1.5/sec with the full fleet, **rising** to 3.5/sec with 1 alien left | Fixes the backwards curve, so pressure builds as the player nears victory. The starting rate is lower, but shots are faster and some are aimed. About 0.65 bullets/sec now land in the ship's path, up from about 0.4, and arrive twice as fast. |
| Aimed shots (**Aimed Shot Chance**) | None | 35% of shots come from the column nearest the player | Stops the player parking in one spot, without making every shot a threat. |
| Alien bullet speed (`AlienBullet` → **Speed**) | 5 units/sec | 9 units/sec | Cuts the fall time from about 2.4 s to about 1.3 s. That's still dodgeable, since the ship moves 10 units/sec. |
| Darts on screen (`Spaceship` → **Max Darts On Screen**) | Unlimited | 2 | Makes accuracy matter: a miss ties up a slot for about 1.1 s. Two (not the arcade's one) allows a follow-up shot. |
| Shot cooldown (`Spaceship` → **Shot Cooldown**) | 0.05 s | 0.25 s | Stops both darts leaving in one burst. |
| Speed-up curve (**Max Speed Multiplier** / **Speed Up Exponent**) | 6× at the last alien, exponent 2 | 5×, exponent 1.5 | The old curve stayed slow, then spiked; with limited darts, hitting a 30 units/sec last alien was mostly luck. |
| Lives (`GameManager` → **Starting Lives**) | None (one hit was game over) | 3 lives, plus 1.5 s blinking invulnerability after each hit | Offsets the harder firing. Invulnerability stops one burst taking several lives. The lose line is still an instant loss. |

| Fleet speed (units/sec) by aliens left | 30 | 20 | 10 | 5 | 1 |
|---|---|---|---|---|---|
| Original (6×, exponent 2) | 5.0 | 8.0 | 16.9 | 23.6 | 30.0 |
| New (5×, exponent 1.5) | 5.0 | 9.1 | 16.5 | 21.0 | 25.0 |

| Fleet shots/sec by aliens left | 30 | 20 | 10 | 5 | 1 |
|---|---|---|---|---|---|
| Original (0.1 per alien) | 3.0 | 2.0 | 1.0 | 0.5 | 0.1 |
| New (1.5 → 3.5) | 1.5 | 2.2 | 2.9 | 3.2 | 3.5 |

#### Later changes to the balance

- **Values per level:** the fleet, firing and bullet values moved into each level's LevelData. SIM 01 keeps the assignment values above; SIM 02 and SIM 03 raise them (see [Levels](#levels)).
- **Firing logic:** the fleet still fires at a fleet-wide rate from the lowest alien in each column, with some shots aimed. Each shot is also either orange (destructible) or dark (indestructible), with the share of dark shots set per level.
- **Lives:** now reset to 3 at the start of every simulation. In the assignment version there was one level; in an earlier expansion step, lives carried over between levels.
- **The laser:** the Pod Program gives the player a periodic way to clear a column.
- **SIM 03 eased:** after the laser was added, SIM 03 dropped from 11 to 10 columns, with a slightly lower fire rate and bullet speed.

### Optional Features

The assignment's four optional features, plus a fifth added in the expansion.

#### Lives

- **What the player sees:** "LIVES: 3" in the top-right corner. On a hit there's an orange burst, an orange screen flash and a blinking, invulnerable ship.
- **How it works:**
  - `GameManager` stores `Lives` and raises `LivesChanged` in `PlayerHit()`.
  - `LivesDisplay` formats the text from its **Format** field.
  - `Spaceship` runs the invulnerability coroutine, which toggles the model's renderers.
- *Later:* lives reset to full at the start of every simulation and on every retry.

#### Start / end screens

- **What the player sees:**
  - **Start:** the title screen, with a blinking `[ START ]` bar and the controls.
  - **End:** in the assignment version, a result panel with `[ RETRY ]`.
  - *Later:* the end screen became the debrief, failure and certification panels, plus the briefing before each level and the epilogue.
- **How it works:** `NierScreens` builds every screen in code with `NierStyle` helpers. It shows them when GameManager changes state, and handles their key presses.

#### Background

- **What the player sees:** a flat beige backdrop with a faint grid instead of the skybox.
- **How it works:**
  - `NierBackground` sets the camera to clear to solid beige.
  - It generates a 160 × 160 px grid tile in code and puts it on a large quad behind the playfield.
  - It turns off the camera's post-processing, whose tonemapping and vignette shifted the palette.
  - `NierTextStyle` restyles the HUD text.

#### Collision animations

- **What the player sees:**
  - **Alien destroyed:** a pale diamond flash plus 12 dark and 4 orange spinning squares.
  - **Ship hit:** a larger orange flash, 18 orange and 8 dark squares, and an orange full-screen flash.
- **How it works:**
  - `HitEffects` builds both particle systems in code.
  - It is triggered by `Alien.Die()` and `Spaceship.Hit()`.
  - It runs on unscaled time, so the final hit still animates on the frozen end screen.

#### Additional levels (expansion)

- **What the player sees:** three simulations with rising difficulty, each with its own briefing, debrief and failure conversations, plus a final certification and rank.
- **How it works:** each level is a **LevelData** ScriptableObject in `Assets/Levels`, holding its fleet and firing settings, its anomaly flag and its dialogue. GameManager plays an ordered list of them.
- **Default levels:** the editor menu **Tools → YoRHa → Reset Default Levels (Overwrite)** rewrites the three assets with the default values and script.

---

## Script Overview

| Script | Description |
|---|---|
| **Game flow** | |
| `GameManager.cs` | Singleton state machine (Title, Briefing, Playing, Debrief, Lost, Won, Epilogue). Holds the level list, lives, per-level and run-wide stats, and certification ranks and dialogue. Raises `StateChanged`, `LivesChanged` and `TargetDestroyed`, and reloads the scene between levels. |
| `LevelData.cs` | ScriptableObject for one simulation: fleet and firing settings, dark-shot share, anomaly flag, optional enemy prefab override, and briefing, debrief and failure dialogue. |
| `Editor/LevelDataDefaults.cs` | Editor menus that create or reset the three default level assets with their values and dialogue. |
| **Gameplay** | |
| `AlienFleet.cs` | Spawns the grid from the current level and moves it. Applies the speed-up and fire-rate curves, picks which alien fires (orange or dark), and checks the fixed lose line and level clear. Raises events for the Pod's commentary. |
| `Alien.cs` | One alien. Builds its model by row, fires when told to, reports its bottom edge, and dies through `Die()` (darts and laser). |
| `AlienBullet.cs` | An alien shot, orange or dark. Hits the ship, destroys darts, and is destroyed by darts only if orange. |
| `Spaceship.cs` | Player movement, screen clamp, firing (dart limit and cooldown), and hits with invulnerability. Builds its model and arrowhead collider, and locks its Rigidbodies. |
| `Dart.cs` | The player's projectile, with its model and trail. |
| `Pod.cs` | The support Pod: follows the ship, charges on kills, and fires the laser through `Alien.Die()`. Holds the Pod's **Designation** ("POD 217"). |
| **UI** | |
| `NierScreens.cs` | Builds and runs every screen (title, briefing, debrief, failure, certification, epilogue), the HUD level label, the hit flash and the anomaly title glitch. |
| `DialogueLine.cs` | One line of dialogue: a speaker (Operator or Pod) and text. |
| `DialogueView.cs` | Shows a conversation with speaker labels, sequential typing and pagination. |
| `TypewriterText.cs` | Reveals TextMeshPro text a few characters at a time, using unscaled time. |
| `PodChatter.cs` | The Pod's in-play commentary box, with priorities, a cooldown and variant picking. |
| `PodMeter.cs` | The bottom-left Pod Program charge meter. |
| `AlienCounter.cs` | HUD "Targets Remaining" text. |
| `LivesDisplay.cs` | HUD "LIVES: n" text. |
| `NierStyle.cs` | Static palette and helpers for building UI in code. |
| `NierTextStyle.cs` | Restyles an existing HUD text to the NieR look. |
| **Visuals** | |
| `NierArt.cs` | Singleton providing one unlit material per colour, plus cached meshes, for the models. |
| `NierShapes.cs` | Procedural meshes: extruded polygons (arrowhead, octagon, spikes) and an octahedron. |
| `NierModels.cs` | The model designs: ship, three alien shapes and their anomaly corruption, both bullet types, darts. |
| `AlienVisual.cs` | Alien idle bob, and the anomaly glitch (jitter and flicker), applied to the model only. |
| `Spin.cs`, `Pulse.cs` | Small animation helpers: continuous rotation and a pulsing scale. |
| `NierBackground.cs` | Solid beige background, generated grid, post-processing off. |
| `HitEffects.cs` | Singleton that builds and plays the square-particle bursts and flashes. |
| `TutorialInfo/Readme.cs`, `TutorialInfo/Editor/ReadmeEditor.cs` | Unity project-template scripts; not part of the game. |

---

## Technical Notes

### GameManager as the central state machine

- **States:** GameManager owns the flow through `GameState`: **Title**, **Briefing**, **Playing**, **Debrief**, **Lost**, **Won** (certification) and **Epilogue**.
- **Frozen time:** every state except Playing runs with `Time.timeScale = 0`, so screens animate on unscaled time.
- **Reporting, not deciding:** gameplay scripts only report what happened:
  - `AlienFleet` reports kills, a cleared level and the aliens landing.
  - `Spaceship` reports hits.
- **GameManager decides:** it handles lives, failure, level progression, ranking, and freezing and unfreezing time. UI scripts listen to its events and call its methods for menu choices (`StartFromTitle`, `BeginLevel`, `ContinueToNextLevel`, `Retry`, `ShowEpilogue`, `ReturnToTitle`).
- **Input timing:** all menu input is read in `NierScreens.LateUpdate`, after the ship's `Update`, so the key that starts a level doesn't also fire a dart. A short input delay stops a held fire key from skipping a screen.

| Event | Source | Listeners |
|---|---|---|
| `StateChanged` | GameManager | NierScreens, Spaceship (stop blinking), Pod (stop laser), PodChatter |
| `LivesChanged` | GameManager | LivesDisplay, NierScreens (hit flash), PodChatter |
| `TargetDestroyed` | GameManager | Pod (charge) |
| `ChargeChanged`, `Ready`, `LaserFinished` | Pod | PodMeter, PodChatter |
| `AliensRemainingChanged`, `BulletFired` | AlienFleet | PodChatter |

Listeners subscribe in `Start` and unsubscribe in `OnDestroy`. Each keeps its own reference to the object it subscribed to.

### Scene-reload level flow and domain reload safety

- **One fresh scene per level:** every level runs in a freshly reloaded scene, which gives clean state with no leftover bullets, aliens or timers.
- **Carrying state across reloads:** static fields survive `SceneManager.LoadScene`:
  - **Pending start:** `hasPendingStart`, `pendingState` and `pendingLevelIndex` say how the next load should begin. For example, "Briefing of level 2" when continuing, or "Playing level 1" when retrying.
  - **Run stats:** `runLivesLost`, `runRetries` and `runTargetsDestroyed` accumulate across the run.
  - **New run:** when there's no pending start (entering Play mode, or returning to the title), `Awake` treats the load as a new run and resets the run stats.
- **Domain reload is disabled:** the project has Enter Play Mode Options turned on with domain reload off, so statics would otherwise keep their values between Play sessions.
  - **Resetting statics:** every mutable static is reset in a `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]` method: `GameManager` (Instance, pending fields, run stats), `HitEffects.Instance` and `NierArt.Instance`.
  - **Statics that don't need it:** the palette colours, model outlines and similar values are `static readonly` and never change.
- **Each load resets the rest:** `Awake` resets lives, state and `Time.timeScale`. `OnDestroy` restores `Time.timeScale` to 1, so stopping Play mode on a frozen screen doesn't leave the Editor frozen.
- **Singletons during a reload:** the old scene's copy may still exist briefly, so each singleton only treats an existing `Instance` as a duplicate if it's in the same scene.
- **Read-only level data:** LevelData fields are private, with read-only properties, because changes made to a ScriptableObject in Play mode are saved into the asset.

### Dialogue and pagination

- **Storage:** conversations are lists of `DialogueLine` (speaker plus text).
  - LevelData stores the briefing, debrief and failure conversations.
  - GameManager assembles the certification conversation from the rank's Operator message, its Pod line and a shared closing exchange.
  - GameManager's component menu has **Reset Certification Dialogue**, because the scene keeps its own saved copy.
- **Display:** `DialogueView` gives each line a speaker label (the Operator's in ink, the Pod's in orange). Lines type one after another with `TypewriterText`.
- **Pagination:** before showing a conversation, it measures each line's wrapped height with TextMeshPro's `GetPreferredValues` and splits the lines into pages that fit its area.
- **Controls:** any key completes the current page's typing; Space/Enter then turns the page, or continues on the last one. The hint below the panel says which applies.

### Pod commentary: priorities and cooldown

- **Cooldown:** after each line, a global cooldown (6 s) starts. During it, a new line is shown only if its priority is higher than the line currently up.
- **Priorities:**
  - **3:** last life
  - **2:** first dark shot, final target
  - **1:** level start, laser ready, multi-kill, hit, fleet acceleration, three targets left
  - **0:** anomaly remarks, every 12–20 s in SIM 03
- **Variants:** each event picks a random variant, never the same one twice in a row.
- **Visibility:** lines hide after 3.5 s and are cleared when play stops.
- **Editing:** every line, priority and timing is an Inspector field on `PodChatter`.

### The Rigidbody fix

After the ship stopped being destroyed on hit, alien bullets started knocking it over and pushing it off-screen.

- **The cause:** the scene's Spaceship had a **second, non-kinematic Rigidbody** added on top of the prefab's kinematic one; it came from the original project. Gravity was off, but each bullet impact pushed and spun the ship, and nothing slowed it.
- **The fix, all in code:**
  - **Ship:** `Spaceship.LockRigidbodies()` makes every Rigidbody on the ship kinematic, with gravity off and all movement frozen. Kinematic bodies still receive `OnCollisionEnter` from the non-kinematic bullets.
  - **Darts, bullets and aliens:** they get gravity off and `FreezeAll` constraints in `Awake`. They stay non-kinematic (see below), but collisions can no longer move them.
  - **Lose line:** fixed at level start, as described [above](#lose-detection-and-win-condition).
- **Collider:** the ship's original capsule colliders are disabled at runtime. A convex collider in the shape of the arrowhead model replaces them, so the hitbox matches what the player sees.

### Why `Rigidbody.MovePosition` isn't used

- **How objects move:** everything is moved by setting `transform.position`.
- **Why not MovePosition:** it is meant for kinematic Rigidbodies, and with the project's default contact-pairs setting, two kinematic bodies never send `OnCollisionEnter`. Making darts, bullets and aliens kinematic would stop hits from registering.
- **Tunnelling isn't a risk:** at 0.02 s per step, the fastest object (the dart at 15 units/sec) moves 0.3 units per step, which is less than the colliders' size.

### Collision layers

| Layer | Used by | Doesn't collide with |
|---|---|---|
| Spaceship | Ship | Darts |
| Alien | Aliens | Alien bullets |
| Bullet | Darts | Ship |
| Alien_Bullet | Alien bullets | Aliens |

On top of the layers, the scripts filter collisions:
- aliens react only to darts
- alien bullets call `Hit()` on the ship, destroy darts (surviving if dark), and ignore other alien bullets
- the Pod and all model parts have no colliders

---

## Development Notes

This project was developed with Claude Code (AI assistance) through my own prompts. I directed the design, story and requirements, did all of the Unity Editor setup, and playtested and verified each change.
