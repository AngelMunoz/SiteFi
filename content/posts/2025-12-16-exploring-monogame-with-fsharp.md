---
title: Exploring MonoGame with F#
subtitle: The Evolution of Kipo
categories: fsharp, monogame, fsadvent, gamedev
abstract: Another December, another \#FsAdvent entry in my F# decade long journey! Thanks to Sergey Tihon for another consecutive year of organizing the very lovely F# Advent calendar...
date: 2025-12-16
language: en
---

Hey there!
It has been quite some time since the last blog entry.

Another December, another #FsAdvent entry in my F# decade long journey!
Thanks to [Sergey Tihon] for another consecutive year of organizing the very lovely [F# Advent calendar]

Rather than bore you with "How I burned out and recovered" stories, I thought it would be more interesting to share my recent journey into game development using F# and MonoGame.

Today I'll be talking about

## [Kipo]

Kipo is the second prototype I've build with [MonoGame] and although [Kps] shares similarities, Kipo took off from where I became blocked with Kps.

In the ideal world these are some of the overall goals I had for this project:

- F# : because I really love the language, and these days is hard for me to pick something else for my own projects.
- MonoGame: Because for some reason it is basically the only option that doesn't fight me tooth and nail to use F#. Neither Unity nor Godot have F# support at all. Sure I could jump through hoops to use F# in these engines but I find that the friction is not worth it at least for me.

- RPG in its core: If you ever played [Trickster Online] or even [Ragnarok Online], you'll know the feeling I'm looking for. I wanted to apply some of these mechanics in a single-player RPG format.
- Isometric View: It looks good, I didn't know what I was getting into.
- Tiled2D Maps: I don't really have experience with games, but every time I've given it a micro shot, tiled2d has been there and I started to get somewhat familiar with it.
- 3D entities: The scenario and world can be 2D as I think for that kind of work it is easier to have images/sprites, but when it comes to characters, entities, objects and so on, I think 3D is interesting to explore, particularly inspired by Super Mario World Games where the platform looks 2D but the characters are 3D.
- ECS-like architecture: Since I'm not an expert in game development, I wanted to explore some of the common patterns but after reading a little bit about ECS, I felt it brought a lot of things to the table that would make my life more complex than I needed it. Picking up some ideas from ECS and adapting core concepts from F# and adaptive data seemed like a good fit.
- [FSharp.Data.Adaptive]: Adaptive data is one of my favorite approaches to state changes in applications, so I wanted to explore if I could skeak it into a game architecture.

With that in mind, I started this project roughly two months ago (counting KPS as well) and I wanted to share some of the journey, phases, and learnings I had along the way.

> **_NOTE_**: I must say though that my previous experience writing video games has been close to zero, (a few demos of phaser.js, some monogame experiments no longer than 300 LOC, and at most one or two unity tutorials) so as I was implementing both Kipo and KPS I was learning quite a lot.

## Foundations & Architecture (Early November)

The project began not with graphics, but with a focus on data + types architecture.
What I've leaned over the years with F# is that a solid set of models and types can simplify life a long way.

### Re-architecture & Core Systems

While some of the architectural choices were born in Kps, some were entirely born in Kipo.
Most of it though, simply flowed from Kps to Kipo with the corresponding adjustments to the domain. This allowed me to not start strictly from zero and this time, I would focus a more on staying more true to Monogame where possible (GameComponent, DrawableGameComponent) than complete F# moules and functions.

The decision comes from being able to interop and leverage existing MonoGame Libraries in case I needed to, which ended up being useful when integrating Myra as a GUI library.

In any case back on the game itself, the main effort at the beginning was to port over the input, keybindings and action systems from KPS. These would allow me to start moving a player and invoking abilities for the early prototype phases.

Then simple motion in a top-down 2d world was more than enough to get started.

Kps had a monolithic `GameState` that managed every component of entities in a single record in a very monolithic way. In Kipo I decided to split the components into separate maps of components which allowed for more flexibility and granular updates although, that introduced a little bit of coginitive complexity as entity components are no longer grouped together.

```fsharp
// Kps code
type EntityComponents = {
  Identity: Classification.Profession
  BaseStats: Attributes.BaseAttributes
  Resources: Attributes.Resources
  Position: Position
  Movement: Movement
  AbilityCooldowns: HashMap<int<AbilityId>, TimeSpan>
  Effects: HashMap<int<EffectId>, ActiveEffect>
  Factions: Classification.Faction HashSet
  Abilities: int<AbilityId> HashSet
  PartyId: Guid<PartyId> voption
  Inventory: HashMap<Guid<InventoryItemInstanceId>, InventoryItem>
  EquippedItems: HashMap<Slot, Guid<InventoryItemInstanceId>>
}
```

> **_Note_**: [Kps Domain.fs] shows the full picture of the Kps domain types.

```fsharp
// Kipo code
type World =
    abstract Time: Time aval
    // Components as adaptive maps
    abstract Positions: amap<Guid<EntityId>, Vector2>
    abstract Velocities: amap<Guid<EntityId>, Vector2>
    abstract Resources: amap<Guid<EntityId>, Entity.Resource>
    abstract Factions: amap<Guid<EntityId>, Entity.Faction HashSet>
    // ...
    abstract AIControllers: amap<Guid<EntityId>, AI.AIController>
```

This in turn allowed the newer systems to operate on the components they care about and when updates were made, only the relevant state would update, not an entire entity.

### Reactive Data

One of the key technical choices was the integration of `FSharp.Data.Adaptive`.
This library allows for reactive programming patterns, where changes to data automatically propagate through the system. This also guaranteed a single source of truth along systems when these requested the most recent snapshot of the game state.

The built-in cache mechanisms also (hopefully as I didn't measure anything at this point though no performance issues have been noticed at all) helped to avoid unnecessary recomputations during the game loop.

For example:

```fsharp
type EffectProcessingSystem(game: Game, env: PomoEnvironment) =
  inherit GameSystem(game)

  let (Core core) = env.CoreServices

  // Adaptive computations were set in class private properties
  let timedEvents =
    core.World.ActiveEffects
    |> timedEffects
    |> calculateTimedEvents core.World

  let loopEvents =
    core.World.ActiveEffects
    |> loopEffects
    |> calculateLoopEvents core.World

  let permanentLoopEvents =
    core.World.ActiveEffects
    |> permanentLoopEffects
    |> calculatePermanentLoopEvents core.World

  let allEvents =
    let inline resolve _ a b = IndexList.append a b
    AMap.unionWith
      resolve
      timedEvents
      (AMap.unionWith resolve loopEvents permanentLoopEvents)
    // get only the values, as keys are not relevant here
    |> AMap.toASetValues

  let publishEvents(events: IndexList<LifecycleEvent> seq) =
    for evts in events do
      for evt in evts do
        match evt with
        | State stateEvent -> core.EventBus.Publish stateEvent
        | EffectTick(DamageIntent dmg) -> core.EventBus.Publish dmg
        | EffectTick(ResourceIntent res) -> core.EventBus.Publish res

  override _.Update _ =
    allEvents
    |> ASet.force // request the latest snapshot
    |> publishEvents
```

having the adaptive value "running" within the class and requesting the latest snapshot during the update loop allows the system to avoid dirty checking and flag-based management with ifs and other flow control mechanisms.

Most of these computations are pure functions that operate on the state and transform that data to produce something that can be used in this case by other systems via the event bus.

In this case, all of the effect-related events are calculated from the active effects map, which if no-one is applying or removing effects, this map remains unchanged and no recomputations are made even if we request the latest snapshot every frame.

Depending of what the systems do, some of them use adaptive values some of them just use pure functions that operate on the latest snapshot of the world and overall this is what most systems within the game look like.

## Gameplay Depth (Mid-November)

With these core systems in place, I was able to start iterating on basic gameplay features. Each time a new feature was added. It fit neatly into the established architecture, more domain types, a new system and the game loop just kept running smoothly.

Then the focus shifted to implementing deep RPG mechanics. F# algebraic data types make it easier to reason about the relation between different concepts and how those can be intertwined, for my case it boiled down to have simple and small types that represent the domain concepts and then build systems that operate on these types.

### Combat, Skills, and Inventory

Porting what I had already done in Kps, was a bit complex as some of the previous code assumed the existance of things that weren't there just yet however, the very core of combat and skills were ported with nearly no changes.

Non-Surprisingly F# is very good at math 😆, the complex part as usual was to decide how these calculations were going to be performed.

What are the attackers stats, what are the defender stats. How do defenses (and when) apply and so on.

Skills were a difficult piece in the sense that, there's a lot of moving parts and trying to be relatively generic while still being flexible enough to express different kinds of skills was a challenge in itself.

I've been playing Trickster Online at least a decade at this point (thanks private servers), and I never thought how complex skill definitions, effects placed from skills, targeting modes, nuances between single-hit, multi-hit, multi-target, directed-multi-target, area-of-effect, were... something I always took for granted as a player.

However, I think I was able to settle on a flexible enough skill definition format that allows me to express a wide variety of skills. For example, here's a fireball skill definition:

```jsonc
"2": {
    "Name": "Fireball",
    "Description": "Hurls a seeker fiery ball that explodes upon impact.",
    "Intent": "Offensive",
    "Cost": { "Type": "MP", "Amount": 20 },
    "Targeting": "TargetEntity",
    "Area": "Point",
    "Delivery": {
      "Type": "Projectile",
      "Speed": 100.0,
      "CollisionMode": "IgnoreTerrain"
    },
    "ElementFormula": {
      "Element": "Fire",
      "Formula": "(MA * 1.0) + (FireA * 1500)"
    }
}
```

> **_Note_**: For more definitions and the difference between them you can check [Skills.json]

Effects were complicated as there's stacking, permanency, effects that get stronger/weaker based on stats, and so on.

```fsharp
[<Struct>]
type Targeting =
    | Self
    | TargetEntity
    | TargetPosition
    | TargetDirection

[<Struct>]
type SkillArea =
    | Point
    | Circle of radius: float32 * maxCircleCircleTargets: int
    | Cone of angle: float32 * length: float32 * maxConeTargets: int
    | Line of width: float32 * length: float32 * maxLineTargets: int

[<Struct>]
type EffectModifier =
  | StaticMod of StatModifier
  // e.g. The more MA, the more HP heals/damages as the result of the effect
  | DynamicMod of expression: Formula.MathExpr * target: Stat
  // e.g. Hellfire residual burn damage based on ability power and element
  | AbilityDamageMod of
    abilityDamageValue: Formula.MathExpr *
    element: Element voption
  | ResourceChange of resource: ResourceType * amount: Formula.MathExpr

[<Struct>]
type Effect = {
    Name: string
    Kind: EffectKind
    Duration: Duration // e.g., Instant, Timed, Loop, Permanent
    Modifiers: EffectModifier[]
}
```

**Bonus: Flexible Formulas**

One thing that I feel like I should mention is how we handle skill damage sources. In a typical RPG, you have damage formulas, stat calculations, and elemental modifiers and a bunch of other things... it can get messy fast.

I didn't want to hardcode logic like `let damage = (attack * 2) - defense` directly into the codebase. I wanted the flexibility to tweak game balance because I feel mods could be a thing somewhere in the far future.

So, as usual with F#, I started with a type and wrote a small parser to convert the strings into a type-safe tree at runtime.

```fsharp
type MathExpr =
    | Const of float
    | Var of VarId  // e.g. "AttackPower", "FireRes"
    | Add of MathExpr * MathExpr
    | Mul of MathExpr * MathExpr
    | Pow of MathExpr * MathExpr
    // ... and so on
```

This allows me to write strings like `"(MA * 1.0) + (FireA * 1500)"` directly in the skill data. The game parses this at runtime and evaluates it when a fireball hits an enemy entity (or a friendly one if it relates to healing).

### AI Behaviors

One of the previous parts that I felt were almost good enough in Kps was the AI system.

Based on controllers that are attached to entities, the core of the AI controlled entities is based on perception, behavior and cues.

```fsharp
[<Struct>]
type CuePriority = {
  cueType: CueType
  minStrength: CueStrength
  priority: int
  response: ResponseType
}

type AIArchetype = {
  id: int<AiArchetypeId>
  name: string
  behaviorType: BehaviorType // e.g. Patrol, Aggressive
  perceptionConfig: PerceptionConfig
  cuePriorities: CuePriority[]
  decisionInterval: TimeSpan
}
```

> **_Note_**: For more details on how the AI system works, check [AI.fs]

I think there's still a lot of room for improvement here, but the basic systems are in place to allow entities to perceive their surroundings, react to stimuli (cues), and make decisions based on their defined behavior types.

### Event Bus

To decouple these growing systems, we refined the internal event system. This allowed gameplay events (like "UnitDamaged" or "SkillCast") to trigger independent reactions across the codebase without systems knowing about each other.

I have always been a big fan of event-driven code, firing an event and handling somewhere else I feel it is a good way to decouple systems even if that brings some complexity when trying to trace what is going on.

```fsharp
[<RequireQualifiedAccess>]
module SystemCommunications =
  [<Struct>]
  type EffectApplicationIntent = {
    SourceEntity: Guid<EntityId>
    TargetEntity: Guid<EntityId>
    Effect: Effect
  }

  [<Struct>]
  type ProjectileImpacted = {
    ProjectileId: Guid<EntityId>
    CasterId: Guid<EntityId>
    TargetId: Guid<EntityId>
    SkillId: int<SkillId>
  }

  // ... other strongly typed events
```

### Maps and Tiled2D

Tiled2D maps are basically just XML files that describe layers of tiles, objects. Some data can be attached to the map itself or to objects via custom properties.

And since there's already an XML parser built into .NET parsing these maps is very straightforward.

The [Map.fs] file defines the F# types that mirror the structure of the Tiled XML files, allowing the game engine to load and work with map data programmatically.

- **Basic Definitions**:

  - `Orientation`, `StaggerAxis`, `RenderOrder`: F# enums corresponding to Tiled's map settings.
  - `TileDefinition`, `MapTile`: Types to represent individual tiles and their instances on a map layer.

- **Map Components**:

  - `MapLayer`: An F# record representing a Tiled layer, containing its ID, name, dimensions, and a 2D array of optional `MapTile`s.
  - `MapObject`: Represents a single object from an object group, encapsulating its ID, name, type (`MapObjectType`), position (`X`, `Y`), size (`Width`, `Height`), rotation, custom properties, and geometric `Points` (for polygons).
  - `MapObjectType`: An F# discriminated union (`Wall`, `Zone`, `Spawn`, `Teleport`) to categorize map objects, facilitating type-safe pattern matching in game logic.
  - `PortalData`: Specifically for `Teleport` objects, defining the target map and spawn point.
  - `ObjectGroup`: A collection of `MapObject`s, matching the Tiled object groups.
  - `Tileset`: An F# record holding the metadata and `TileDefinition`s for a tileset.

- **`MapDefinition`**: This is the top-level type that holds all the parsed information from a `.xml` map file, including global properties, tilesets, layers, and object groups. This `MapDefinition` is then used by various game systems (rendering, physics, AI) to interact with the game world.

```fsharp
[<Struct>]
type MapObjectType =
    | Wall
    | Zone
    | Spawn
    | Teleport

[<Struct>]
type MapObject = {
    Id: int<ObjectId>
    Name: string
    Type: MapObjectType voption
    X: float32
    Y: float32
    // ...
    Points: IndexList<Vector2> voption // For polygons
}
```

You can find the full parsing code in [MapLoader.fs]

## Optimization & Refinement (Late November)

At some point when some core systems were in place. Adding more entities, traversing the map suddenly brought some random freezes, which I attributed to garbage collection. From what others have said in their F# experiences, excessive garbage collection can be a common issue due to the immutable nature of F#.

And while avoiding GC and trying to be performant from the start was also one of the goals, somehow I missed the mark.

So I revisited some of the core systems to realize that there were some bad assumptions of using adaptive data for things that change very often like positions, spatial grids which are updated every update cycle, so the adaptive graph was being invalidated every frame as the "root" data was changing constantly. That got me thinking "If we're going to invalidate the tree every frame, there's no point in using adaptive data for these parts". I opted for a mixed approach where adaptive data is used for more stable data while more dynamic data while still backed up by adaptive data, snapshots are calculated on demand rather than leveraging full adaptivity and calculations being made in adaptive computations.

```fsharp
[<Struct>]
type MovementSnapshot = {
    Positions: HashMap<Guid<EntityId>, Vector2>
    SpatialGrid: HashMap<GridCell, IndexList<Guid<EntityId>>>
    Rotations: HashMap<Guid<EntityId>, float32>
}

// In Projections.fs
member _.ComputeMovementSnapshot(scenarioId) =
    // Forces the adaptive values once per frame to build the snapshot
    let velocities = world.Velocities |> AMap.force
    let positions = world.Positions |> AMap.force
    // ... calculates physics logic ...
    calculateMovementSnapshot time velocities positions ...
```

This resulted in smoother performance and the freezes were gone.

> If you're using adaptive data for whatever reason, I would say that try to keep the adaptive roots/trunk as stable as possible when it comes to the frequency of input data and leave most of the high frequency changes to leaves in the adaptive tree.

### Spatial Optimization

A major refactor involved the spatial domain, implementing SAT for precise collision detection. The good news is that even if I'm not skilled in geometry this math is an already solved problem. So after a couple prompts to assistants I had a working implementation.

There are some edge cases where collision boudaries are not being calculated properly, specially when boundaries overlap, this is something I still need to investigate and fix. I guess AI is not replacing us soon after all 😆. It however, helped me to not divert too much time and effort to learn something that works and unblocks me from more interesting/important work untill I have a chance or motivation to fix this.

## The 3D Pivot (Early December - Present)

The most recent and significant shift has been the transition from 2D sprites to a full 3D visual style while keeping the same logic core.

I imported 3D models (using [KayKit assets]) and replaced the legacy 2D rendering logic. This also meant building a 3D animation system from scratch to handle model rigging, keyframes, and state-based animations like Idle, Walk, and Attack.

This is just another area where I have never worked before, but with a few prompts and trial and error I was able to get something basic working and it is sustainable enough to keep building upon!

I can see myself later on adding more complex animations like spell casting, taking damage, dying, and so on.

```fsharp
[<Struct>]
type RigNode = {
  ModelAsset: string
  Parent: string voption
  Offset: Vector3
  Pivot: Vector3 // Key for fixing rotation issues!
}

[<Struct>]
type Keyframe = {
  Time: TimeSpan
  Rotation: Quaternion
  Position: Vector3
}

[<Struct>]
type AnimationClip = {
  Name: string
  Duration: TimeSpan
  Tracks: Track[] // Array of keyframes per node
}
```

### Data Driven

One of the important aspects of this game is how some key systems are data-driven.

For example, AI Archetypes, Skills, Maps, and Items among others, are defined in JSON files which may be moved to a database later on.

This while allowing me to iterate fast on mechanics or design, also hints at a possible modding system in the future as well as oportunities for more F# tools to be created in order to facilitate content creation.

### Visual Polish: The Particle System (Mid-December)

With combat, skills and 3D rendering in place, one missing piece (among others) are particles! Things flying and arriving look good for a demo but they lack emotion, so the next sensible thing to add is particles which should give the visuals a little bit more of sense of being alive.

#### Modeling Emitter Behavior with DUs

The particle system needed to describe _where_ particles spawn (shapes) and _how_ they flow (emission modes). Rather than a flat configuration object with fields like `isOutward: bool` or `shapeType: string`, these became proper discriminated unions:

```fsharp
[<Struct>]
type EmitterShape =
    | Point
    | Sphere of radius: float32
    | Cone of angle: float32 * radius: float32
    | Line of width: float32 * length: float32

[<Struct>]
type EmissionMode =
    | Uniform   // Fill area evenly
    | Outward   // Flow away from origin
    | Inward    // Converge toward center
    | EdgeOnly  // Spawn at perimeter only
```

These two dimensions compose orthogonally: a `Sphere` + `Outward` creates an explosion, while `Sphere` + `Inward` creates an implosion. The pattern matching makes the logic clean:

```fsharp
let inline computeSpawnDistance (mode: EmissionMode) (length: float32) (rng: Random) =
    match mode with
    | Uniform  -> float32(Math.Sqrt(rng.NextDouble())) * length
    | Outward  -> float32(rng.NextDouble() * 0.85) * length
    | Inward   -> length * (0.85f + float32(rng.NextDouble() * 0.15))
    | EdgeOnly -> length * (0.95f + float32(rng.NextDouble() * 0.05))
```

Adding a new emission mode is straightforward: define the case, then the compiler points to every match expression that needs updating.

#### Cross-System Integration: Skills Shape Particles

One pattern that emerged was letting the combat system's `SkillArea` influence particle geometry. When a cone skill like Dragon's Breath fires, its particles should automatically fill that cone — not some hardcoded shape from the particle config.

The particle system checks for skill-driven overrides at spawn time:

```fsharp
| EmitterShape.Cone(configAngle, configRadius) ->
    let angle, length =
        match overrides.Area with
        | ValueSome(SkillArea.Cone(skillAngle, skillLength, _)) ->
            skillAngle, skillLength
        | ValueSome(SkillArea.AdaptiveCone(skillLength, _)) ->
            configAngle, skillLength
        | _ -> configAngle, configRadius * 10.0f
    spawnConeShape rng angle length effectiveMode
```

This keeps the systems decoupled — particles don't know about combat logic, they just receive typed data. Adding a new skill area type automatically prompts consideration of how particles should render it.

> **_Note_**: The full particle configuration reference can be found in [ParticleConfigReference.md]

### What's the actual F# role here?

F# is usually seen as a niche language, downplayed due it's functional nature and the fact that has to compete in a C# dominated space. Using it for Game development is even more niche, so why bother?

- Refactoring confidence. Moving from a single, monolithic GameState to separate component maps (and later switching parts of the renderer to 3D) felt safe because the compiler would catch a lot of the mechanical mistakes. That meant I could iterate fast: change a type, let the compiler point out what else needed fixing, and keep moving.

- Domain-first modeling. Instead of inventing a bunch of boilerplate classes, I encoded the game's rules and structures directly as types. Those types became a readable, enforceable design document — tooling (and the type system) did a lot of the heavy lifting.

- Things like units-of-measure/UMX for IDs and strongly typed events reduced accidental bugs (mixing up identifiers or event payloads). They're not flashy, but they save time when you refactor or add new systems.

All of that combined let me mix high-level functional ideas (the `MathExpr` tree for flexible formulas) with low-level, performance-conscious code (structs, spans, inline function control, among others) without feeling like I was fighting the language.

If you look at the commit history for Kipo you'll see a theme: quick, targeted rewrites and a lot of small, confident changes. I rebuilt the rendering pipeline to support skeletal 3D animation in a day, and when GC hiccups showed up I refactored movement to use frame snapshots.

That kind of speed comes from a practical place: F# encourages expressive domain modelling and gives you enough type-safety to catch many mistakes early. The payoff is especially noticeable while prototyping — you can experiment, refactor, and change direction without the usual fear of breaking everything.

As I usually say, F# is a high level rust with an even easier time to get going. You get performance, safety, and expressiveness all in one package.

### LLMs and what they've contributed.

If you've follow me before, you probably know that I've been using LLMs to help me with my Game Development Journey!

I know next to nothing from 3D graphics programming, Geometry, Animation, and many of the areas game development touches. so Things like Gemini, Qwen, Claude have been really useful to get me going where I wanted to go.

Whether you like AI or not, for me it is clear that these tools offer a value, I wouldn't let gemini or other assistants write the entire game for me that's for sure and I am very wary of code they write when I am familiar with the domain. I do inspect code in order to correct patterns that I don't like or that I think are not optimal. But when I am exploring new areas, these assistants help me get started, give me ideas, and unblock me when I get stuck.

LLMs produce slop? Yes. Humans produce slop? Also yes.

A challenge when working alone (especially when you don't know particular areas like geometry or 3D rendering) is detecting sloppy code. You can't really code review your own blind spots.

From my experience with this codebase, the AI-generated code wasn't so much wrong as it was redundant: repetitive patterns, duplicated logic, correct inputs and outputs but with bloated inner workings. Honestly, this reflects what us humans tend to do anyway.

There are still moments where I need to go back and do a full refactoring pass (specially before merging a PR) so I can ensure the code follows my general guidelines. Not super minuciously detailed, I'm human after all and my time is limited. It still requires deliberate effort to keep the codebase in shape and not let it become slop soup.

Combined with how expressive and robust F# is, moving fast and safe is possible even when exploring new areas, as long as the domain is well modelled F# code will flow naturally, things will break in expected ways and the compiler will help you get back on track. The most troublesome part is when there's subtle logical bugs or lack of domain knowledge, but that's where human intuition and experience come into play.
Even then AI assistants are good "rubber ducks" so rather than a "just code" tool, I also do a lot of back and forth to validate, refine and challenge my own understanding of the problem at hand.

## Conclusion

In just over a couple months, `Kipo` has evolved from a basic F# architectural prototype into a functional 3D RPG with complex systems for AI, combat, and inventory. This journey proves that F# is not just viable for game development but can offer a powerful, type-safe, and expressive way to build complex interactive systems.

If you are interested in game development with F# and MonoGame, I highly encourage you to give it a try. The ecosystem might be smaller than C#'s, but the power of the language makes up for it.

If you want me to share more of F# + AI personal experiences and personal approaches let me know in the comments or ping me on my socials!

[MonoGame]: https://monogame.net/
[Kps]: https://github.com/AngelMunoz/Kps
[Trickster Online]: https://en.wikipedia.org/wiki/Trickster_Online
[Ragnarok Online]: https://en.wikipedia.org/wiki/Ragnarok_Online
[FSharp.Data.Adaptive]: https://github.com/fsprojects/FSharp.Data.Adaptive
[Kps Domain.fs]: https://github.com/AngelMunoz/Kps/blob/main/Pomo.Lib/Domain.fs
[Skills.json]: https://github.com/AngelMunoz/Kipo/blob/main/Pomo.Core/Content/Skills.json
[AI.fs]: https://github.com/AngelMunoz/Kipo/blob/main/Pomo.Core/Systems/AISystem.fs
[Map.fs]: https://github.com/AngelMunoz/Kipo/blob/main/Pomo.Core/Domain/Map.fs
[MapLoader.fs]: https://github.com/AngelMunoz/Kipo/blob/main/Pomo.Core/Systems/MapLoader.fs
[KayKit assets]: https://kaylousberg.itch.io/prototype-bits
[ParticleConfigReference.md]: https://github.com/AngelMunoz/Kipo/blob/main/docs/ParticleConfigReference.md
[Sergey Tihon]: https://bsky.app/profile/sergeytihon.com
[F# Advent calendar]: https://sergeytihon.com/2025/11/03/f-advent-calendar-in-english-2025/
[Kipo]: https://github.com/AngelMunoz/Kipo
