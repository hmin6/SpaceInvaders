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
- **Magic numbers:** the grid size and spacing in `AlienFleet.Start`, and the ±10 off-screen limits in `Dart` and `AlienBullet`.
- **Spaceship input** (`Spaceship.cs:22-33`): the left and right code is duplicated, the ship isn't kept on screen, it throws a null-reference error with no keyboard, and the unused `InputSystem_Actions` asset is ignored. The 0.05 s shot cooldown does almost nothing, because `wasPressedThisFrame` already allows only one shot per key press.
- **No collision filtering in code** (`Alien.cs:41`, `AlienBullet.cs:34`): both handlers destroy whatever they touch and rely entirely on the physics layer collision matrix.
