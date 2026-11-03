# Code Issues

A review of the gameplay scripts in `Assets/`. The issues below are fixed in C# only; no scenes, prefabs, or `.meta` files were changed. Gameplay should behave the same as before. Line numbers refer to the original scripts.

## Fixed

### 1. UI text rebuilt every frame
**Files:** `AlienFleet.cs:39-42`, `AlienCounter.cs:17-20`

**Problem:** `AlienFleet.Update()` copied `aliens.Count` into `AlienCounter.score` every frame. `AlienCounter.Update()` then built a new string (`"Remaining Aliens: " + score.ToString(...)`) and assigned it to the TextMeshPro text, also every frame.

**Why it matters:** The count only changes when an alien dies, yet the code allocated a new string every frame (garbage the GC has to collect) and made TextMeshPro regenerate its mesh 60+ times a second for no reason.

**Change:** `AlienCounter` now has a `SetRemaining(int remaining)` method and no `Update()`. `AlienFleet` calls it once after spawning, then only when an alien is actually removed. `AlienFleet.Update()` was deleted. `AlienCounter` now gets its `TextMeshProUGUI` in `Awake()` instead of `Start()`, so the text component is ready before `AlienFleet.Start()` makes the first call. `SetRemaining` also repeats the lookup if it's still `null`, so it doesn't depend on `Awake` having run (for example, if the counter's GameObject starts inactive).

### 5. Movement measured per physics step, not per second
**Files:** `Dart.cs:23`, `AlienBullet.cs:23`, `AlienFleet.cs:52,56`, `Alien.cs:30`

**Problem:** Speeds were fixed distances added every `FixedUpdate` (`.3`, `.1`, `.1`). The alien's fire chance (`.002`) was likewise rolled once per physics step.

**Why it matters:** Game speed depended on the physics timestep. Changing **Project Settings > Time > Fixed Timestep** would have changed how fast everything moved and how often aliens fired. Unity's convention is to give values in units per second and scale them by the step length.

**Change:** Values are now per second and multiplied by `Time.fixedDeltaTime`. The project's timestep is 0.02 s (50 steps/sec), so each old value was multiplied by 50 to keep the same feel:

| Value | Old (per step) | New (per second) |
|---|---|---|
| Dart speed | .3 | 15 |
| AlienBullet speed | .1 | 5 |
| AlienFleet speed | .1 | 5 |
| Alien fire chance | .002 | .1 |

`Rigidbody.MovePosition` was considered and rejected. It is meant for kinematic Rigidbodies, and with the project's default contact-pairs setting, two kinematic bodies never send `OnCollisionEnter`, so making the darts, bullets, and aliens kinematic would stop hits from registering. The objects are still moved with `transform.position`, which is how they moved before.

### 6. `static` tuning values hidden from the Inspector
**Files:** `Alien.cs:10-12`, `AlienBullet.cs:5-7`, `Dart.cs:5-6`, `Spaceship.cs:9-10`

**Problem:** Tuning values (`speed`, `shotCooldown`, `bulletSpawnRate`) were `static`, and several sat under `[Header("Inscribed")]`. Meanwhile `lastShot`, which is internal runtime state, was `public`.

**Why it matters:** Unity doesn't serialize static fields, so these values never appeared in the Inspector and the header labelled nothing. A `public` `lastShot` showed up in the Inspector and could be changed by any script, even though nothing outside the class should touch it.

**Change:** The tuning values are now `[SerializeField] private` instance fields, adjustable per prefab in the Inspector. `lastShot` is now `private`.

### 8. Redundant `GetComponent` and reaching into the fleet's list
**Files:** `Alien.cs:39`, `AlienFleet.cs:13`

**Problem:** `fleet.GetComponent<AlienFleet>().aliens.Remove(gameObject)`. `fleet` was already an `AlienFleet`, and the alien was editing the fleet's public list directly.

**Why it matters:** The `GetComponent` call did a lookup that returned the same object. Letting another class edit the list directly meant the fleet couldn't react to removals, such as updating the counter.

**Change:** Added `public void RemoveAlien(Alien alien)` to `AlienFleet`, which removes the alien and updates the counter. `Alien` now calls `fleet.RemoveAlien(this)`. The list is now `private List<Alien>` instead of `public List<GameObject>`. If two darts hit the same alien in one frame, `Remove` returns `false` the second time and the counter isn't updated twice.

### 4. Empty `Start()` / `Update()` methods
**Files:** `Alien.cs:15-24`, `AlienBullet.cs:9-18`, `Dart.cs:9-18`, `Spaceship.cs:14-17`

**Problem / why:** Unity calls `Update()` every frame on every script that defines it, even when it's empty. That's wasted overhead for 30 aliens plus every live bullet.

**Change:** Removed the empty methods and their template comments.

### Unused code
- Removed unused `using UnityEngine.InputSystem;` (`AlienFleet.cs:3`) and `using UnityEngine.UI;` (`AlienCounter.cs:2`).
- Removed the unused `lowerBound` field (`AlienFleet.cs:18`) and the commented-out block that referenced it (`AlienFleet.cs:57-61`).
- `Alien.FixedUpdate` read `transform.position` into a local variable every step even when it didn't fire (`Alien.cs:28`). It now reads it only when spawning a bullet.

## Found but not fixed

- **No object pooling** (`Alien.cs:31`, `Spaceship.cs:37`, `Dart.cs:27`, `AlienBullet.cs:27`): bullets are created and destroyed constantly. `UnityEngine.Pool.ObjectPool<T>` could reuse them. Separately, `Instantiate` is followed by setting the position, where `Instantiate(prefab, position, rotation)` would do both at once.
- **Two passes over the fleet every step** (`AlienFleet.cs:47-79`): moving the aliens and finding the left and right edges could share one loop. Alternatively, the aliens could be parented to the fleet so it only moves one transform. `left`/`right` start at `0` rather than real min/max values.
- **String-based `Invoke`** (`AlienFleet.cs:84`): `Invoke("startSidewaysMotion", ...)` breaks silently if the method is renamed. Use `nameof` or a coroutine. The method name also isn't PascalCase.
- **Magic numbers:** the grid spacing and start position in `AlienFleet.Start` (the number of columns and rows is now set in the Inspector), and the ±10 off-screen limits in `Dart` and `AlienBullet`.
- **Spaceship input** (`Spaceship.cs:22-33`): the left and right code is duplicated, and the unused `InputSystem_Actions` asset is ignored. (The 0.05 s cooldown that did almost nothing was replaced by a dart limit and a 0.25 s cooldown; see Balance Changes. Keeping the ship on screen and handling a missing keyboard were fixed later, along with the core mechanics.)
- **Collision filtering in code** (`Alien.cs:41`, `AlienBullet.cs:34`): both handlers destroyed whatever they touched. This was fixed later, along with the core mechanics: aliens now react only to darts, and alien bullets report a hit to the ship instead of destroying it.

## Balance Changes

**The problem:** the game was too easy, for four reasons.

1. **Slow bullets.** Alien bullets moved at 5 units/sec, taking about 2.4 s to fall from the bottom alien row to the ship, so the player had plenty of time to dodge.
2. **Unlimited shooting.** The player could fire as fast as they could press space (the 0.05 s cooldown did almost nothing), so spraying darts beat aiming.
3. **Easier as you go.** Each alien fired on its own (0.1 shots/sec each), so the fleet's total fire rate *fell* as aliens died: from 3 shots/sec down to 0.1 shots/sec. The end game, which should be the hardest part, was the safest.
4. **Shots from anywhere.** Back-row aliens could fire too, with bullets passing through the aliens in front of them. The shots were random and didn't force the player to react to the front line.

Every value below is a `[SerializeField]` field and can be tuned in the Inspector. These values were chosen by working through the numbers, not by extensive playtesting. They are a starting point to tune from.

### Changes

| Setting (script → Inspector field) | Original | New | Why |
|---|---|---|---|
| Which aliens can fire (`AlienFleet`) | Any alien, each rolling its own chance | Only the lowest alien in each column | As in the arcade original. The front line becomes the threat, and clearing a column's bottom alien moves its fire up a row, which the player can see and plan around. The fleet now controls firing, and `Alien.Fire()` just spawns the bullet. |
| Fleet fire rate (`AlienFleet` → Base Fire Rate / Max Fire Rate) | 0.1/sec per alien: 3/sec with 30 aliens, falling to 0.1/sec with 1 | 1.5/sec with the full fleet, rising to 3.5/sec with 1 alien left | Fixes the backwards curve, so pressure builds as the player gets closer to winning. The starting rate is lower than before, but bullets are faster and some are aimed (next two rows), so early shots are more threatening. With the old settings about 0.4 bullets/sec landed in the ship's path; now about 0.65/sec do, arriving twice as fast. |
| Aimed shots (`AlienFleet` → Aimed Shot Chance) | None (all random) | 35% of shots come from the column nearest the player | Random shots mostly land far from the ship. Aimed shots stop the player from parking in one spot and shooting, without making every shot a guaranteed threat. |
| Alien bullet speed (`AlienBullet` → Speed) | 5 units/sec | 9 units/sec | Cuts the time from bottom row to ship from about 2.4 s to about 1.3 s. That's still enough time to react, since the ship moves 10 units/sec and is about 2.7 units wide. |
| Darts on screen (`Spaceship` → Max Darts On Screen) | Unlimited | 2 | Makes accuracy matter. A miss ties up a slot for about 1.1 s while the dart flies off the top. Two (rather than the original arcade's one) allows a quick follow-up shot, keeping the game beatable. Set it to 1 for classic difficulty. |
| Shot cooldown (`Spaceship` → Shot Cooldown) | 0.05 s | 0.25 s | Stops both darts going out in one burst at the same spot. |
| Speed-up curve (`AlienFleet` → Max Speed Multiplier / Speed Up Exponent) | 6× at the last alien, exponent 2 | 5× at the last alien, exponent 1.5 | The old curve stayed slow for most of the game and then spiked. With limited darts, hitting a 30 units/sec last alien was mostly luck. The new curve speeds up earlier and peaks lower. See the speed table below. |
| Lives (`GameManager` → Starting Lives) | None (one hit was a game over) | 3 lives, with 1.5 s of blinking invulnerability after each hit (`Spaceship` → Invulnerable Duration) | Offsets the harder firing, so a single unlucky bullet doesn't end a run. Invulnerability stops one burst from taking several lives at once. The aliens reaching the lose line is still an instant game over. |

### Fleet sideways speed (units/sec) by aliens remaining

| Aliens left | 30 | 20 | 10 | 5 | 1 |
|---|---|---|---|---|---|
| Original (6×, exponent 2) | 5.0 | 8.0 | 16.9 | 23.6 | 30.0 |
| New (5×, exponent 1.5) | 5.0 | 9.1 | 16.5 | 21.0 | 25.0 |

### Fleet shots per second by aliens remaining

| Aliens left | 30 | 20 | 10 | 5 | 1 |
|---|---|---|---|---|---|
| Original (0.1 per alien) | 3.0 | 2.0 | 1.0 | 0.5 | 0.1 |
| New (1.5 → 3.5) | 1.5 | 2.2 | 2.9 | 3.2 | 3.5 |

### If it turns out too hard or too easy

- **Easier:** set Max Darts On Screen to 3, lower Aimed Shot Chance, or add a life.
- **Harder:** set Max Darts On Screen to 1, or raise the alien bullet Speed to about 11.
- The fleet's step down per bounce (0.5 units) and the lose line didn't change. Because the new curve makes the fleet faster in the mid-game, it bounces (and so steps down) a little more often around 20 aliens left, which adds some time pressure.
