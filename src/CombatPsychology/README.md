# CombatPsychology

Psychological effects for mercenaries: combat stress, breakdowns, treatments and the
occasional battle high. Built on the game's own data-driven `StatusEffect` chassis, so
stress reads like any other condition — an effects-bar icon with a live percentage,
tooltips with exact numbers, item lines on anything that treats it.

## What it adds (v1)

### Stress (0–100, decays 1/turn when nothing bad happens)

| Stage | Range | Effects |
|---|---|---|
| Unease | 1–24 | none — a warning light |
| Anxiety | 25–49 | −10% accuracy, +15% pain received |
| Fear | 50–74 | −20% accuracy, −30° FOV, −5 dodge, stealth disabled |
| Terror | 75–100 | −30% accuracy, +30% pain received, hallucinations, breakdown rolls |

Gains (scaled by Fortitude and difficulty): getting hit +2, minor wound +4, wound +10,
amputation +25 (plus Shock), pain overflow +8/turn, qmorphosis stage-up +8, massive
single hits cause **Shock** (a stunned turn + spike).

**Breakdowns:** at Terror, each turn rolls 15–50% (rising with stress). A breakdown
freezes the merc for a turn and vents 15 stress — unless stress sits at 100, where the
roll can turn lethal (base 25%, reduced 4% per Fortitude point). A lethal breakdown is
an ordinary death: gear drop, cloning or permadeath exactly per difficulty settings.

### Fortitude

Every merc has base 3; perks carrying the `IFortitude` integer parameter add more (a
hook for future perk mods and our own v2). Each point above base cuts stress gain 7%
and the suicide roll 4%, and raises the Grim Determination chance 10%. Shown in the
stress tooltip.

### Positive psychology

| Effect | Trigger | What it does |
|---|---|---|
| Bloodlust | 3 kills within two turns | +15% melee accuracy, strong pain decay, 5 turns — then +15 stress comedown |
| Battle Focus | dealing damage without taking any | +4% ranged accuracy per stack (max 3); broken when hit |
| Adrenaline Rush | first drop below 30% HP each raid | +2 AP immediately, rapid pain decay 3 turns |
| Second Wind | first pain overflow each raid while stress < 50 | ignores the stun, halves pain |
| Grim Determination | entering Fear, chance = Fortitude × 10% | +20% accuracy while Fear/Terror lasts |

### Treatments

- **Tranq-Eze sedative** (new item, `item qm_psy_sedative` in the console): −35 stress,
  −10 pain, 12% sedative addiction risk per use.
- **Alcohol** items: −12 stress on use (existing addiction risk unchanged).
- **Anything with a nicotine addiction chance** (cigarettes etc.): −8 stress on use.
- **Sedative addiction** is a real addiction (id ends in `Addiction`), creeping upward
  once acquired, with pain/accuracy penalties at higher stages.

## Testing

Enable the in-game console mod/dev console, then:

```
statuseffect stress 80      // set stress level directly
item qm_psy_sedative        // spawn the sedative
morphine 50                 // vanilla command, for comparing addiction behavior
```

Watch `Player.log` for lines tagged `[CombatPsychology]`.

## Implementation notes

- Stress and the sedative addiction are `StatusEffectsRecord`s registered at
  `AfterConfigsLoaded`; their stage penalties are vanilla wound-effects, so icons,
  tooltips, save/load and the health screen all come from the base game.
- In-raid state only. `MercenarySystem.RestoreStateAfterMission` wipes the effects
  controller between raids, so everything (including addictions) resets on the ship.
  Persistent trauma/scars are the planned v2, using per-merc storage like `CurseData`.
- Localization is appended to the game's TSV via the `ResourcesLoad` hook; all 11
  language columns carry English for now.
- Icons are generated placeholders (`tools/make-icons.ps1`); drop replacement PNGs with
  the same names into `content/icons/`.
