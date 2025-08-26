Andrea.Squillante.basket-challenge

Arcade “Shooting Race” inspired by Basketball Stars™: score as many points as you can from a sequence of spots within a time limit. Built with Unity 2021.3.4f1, C#, and mobile-friendly swipe input.

Single player with optional AI opponent, juicy UI feedback (power bar with bands, swipe arrow, score popups), bonuses (Backboard & Fireball), camera framing, and a clean modular codebase using [SerializeField] over public.

🎮 Gameplay at a glance

Swipe-to-shoot (mouse on Desktop, touch on Mobile).

Arcade launcher: deterministic arc toward the hoop (distance-based launch angle) + yaw from swipe; optional snap-to-perfect.

Scoring:

3 pts = perfect (swish)

2 pts = make (rim/backboard)

Backboard Bonus: sometimes active; if you touch the board and score you get +4 / +6 / +8 by rarity.

Fireball Mode: fill the combo bar by scoring consecutively; while active, points are doubled (until a miss or timer expires).

AI Opponent (optional): configurable accuracy/timing; uses the same physics; does not collide with the player ball.

Timer: default 120s. Flow: Main Menu → Gameplay → Reward (final score).

✅ Feature checklist

Mandatory

 Single player (no animated character required)

 Camera movements (framing behind the shooter)

 Mouse input

 Mobile touch input (swipe)

 Basic score system (3 for perfect, 2 for others)

 Backboard bonus (blinking board; +4 / +6 / +8 if touched before scoring)

 Basic UI (start page, in-game HUD, reward page)

Optional

 AI competitor (customizable difficulty)

 Fireball (combo → double points for a limited time)

 Visual polish: swipe arrow, power bar with Perfect/Make/Backboard bands, score flyers

 Debug gizmos (power bands, helpers), editor-time camera framing


⚙️ Requirements

Unity 2021.3.4f1

Target: Desktop & Mobile (iOS/Android)

🚀 Getting started

Clone the repository and open with Unity 2021.3.4f1.

Open Scenes/Main.unity.

Press Play.

Desktop: click–drag (mouse) bottom→up to shoot.

Mobile: swipe touch the same way.

Quit button is available in the Main Menu (works in build; stops Play in Editor).

🧭 Controls & UI

Swipe Feedback: an arrow starts where you touch and points in swipe direction; label shows power %.

Power Bar Zones: three colored bands (Perfect/Make/Backboard) computed analytically for the current shot position.

Score Flyers: +points pop above the hoop, facing the camera.

Timer: 02:00 by default; configurable.

🧠 AI

AIShooterController picks an outcome (Perfect/Make/Backboard/Miss) using weights from AIDifficultyProfile.

It queries ShotPowerAdvisor and samples a power% inside the intended band, adds small jitter, and calls BallLauncher.LaunchAI().

Respects game flow (only acts in Gameplay).

Player and AI balls are on separate physics layers and do not collide with each other.

🔥 Fireball

Each consecutive make increases a combo bar.

When full, Fireball Mode starts: double points for a limited time (bar drains) or until a miss.

Visual feedback included.

🧱 Backboard Bonus

Randomly spawns with rarities: Common +4, Rare +6, Very Rare +8.

Visual marker/material color indicates rarity; claimed on backboard touch → score.

Resets/spawns per shot.

🎥 Camera

CameraFramer (ExecuteAlways) positions/aims the camera behind the shooter toward the hoop.


🎯 Physics & scale 

Arcade launcher: deterministic arc toward hoop with distance-based launch angle and fall multiplier for a snappier descent.

🛠 Configuration (ScriptableObjects)

ShotPhysicsProfile

Impulse mapping (impulsePerCm, impulsePerCmPerSec, maxImpulse)

Flight tuning (gravityMultiplier, airDragWhileFlying, sceneScale)

Spin (applySpin, backspinPerImpulse, sidespinPerImpulse)

Arcade arc (targetLaunchAngleDeg, distance auto-angle, yaw clamp)

AIDifficultyProfile

Shot interval range, initial delay

Weights: perfect/make/backboard/miss

Jitter: powerJitterPct, lateralNoiseDeg

adaptToBackboardBonus, backboardWeightBoost


🗂 Git workflow & tags

Each task in the challenge maps to a commit/tag (e.g., setup, input, detection, … fireball, ai).

📈 What I would add next

Object pooling for flyers/VFX; polished net animation & particle FX; richer camera transitions.

Music and sound effects.

Settings menu (sensitivity, left-handed mode), accessibility tweaks.


🙌 Final note

The project focuses on clean gameplay architecture, tune-ability, and feel. I’m happy to iterate based on your feedback—especially on polish priorities, balancing, and how you’d like to see the AI evolve.
