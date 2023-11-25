# FiremanAssist
Fireman Assist Mod for Derail Valley

Entering a supported loco will add a fireman to that loco.  You can also request a fireman with the comm radio, and finally, coupling a supported loco to a train that already has a fireman onboard will add a fireman to that loco (no limit).  Fireman state is not persisted across save games currently but can be reinitialized quickly after a load.

### Current Features
* Water Management from minimum protection to full management
* Slope-aware water management - understand the current water angle to interpret what it's seeing in the water glass.
* Fire Management from minimum protection to full management
* Throttle and Cutoff aware fire management - will allow the fire to cool if the engine is idling - keeping the pressure around 12-13 bar.  Ramps up coal usage as steam demand increases - but requires more than a momentary burst of usage to do so, so still good for shunting
* Comm Radio control to advise fireman to start or stop firing - Also shows current state, including out of water.
* Cylinder Cock control (useful when using MU mods to run multiple steam units)
* Each locomotive is managed independently
* Low Tender Water auto shutdown - prevents automatically adding coal when the tender is low on water - thus protecting against getting into an uncontrolled fire state with not enough water to add to the boiler.

#### Water Management Options
Water management in 4 levels, selectable through UMM settings window:
* None - turns off, useful if you want to disable the management temporarily without disabling the mod, say for a realistic save
* No Explosions - Ensures you never let the water get too low.  Can still overfill and damage your cylinders, but will never underfill so long as you have water available to add (and the injector can keep up, those grade transitions can get intense).
* Over/Underfill protection - Ensures you never let water get too low or too high.  _Might_ allow overfill if going downhill for an extended period of time, as it uses the same information the user has (the water glass)
* Full management - Does its best to provide the optimum injector setting for the current situation.  Will aim to inject more water if the pressure is too high, and less water if the pressure is too low, to assist in firing in the sweet spot around 13 bar.

#### Injector Override Options
The injector can be overriden by the user.  The manager has a few options for how it reacts:
* None - Override will be overwritten each tick - expect weird flickering if you try to mess with it - NOTE: While there is no fire, this is less tightly controlled, and if water is below the minimum, you can manually increase it.  The injector won't turn on automatically below that threshold to allow for towing engines, but if manually turned on, will turn itself off once minimum water to start firing is reached.  If firing is requested through the comm radio, injector will still work.
* Temporary - Override will stick until the manager decides it wants a new value.  Note that it is possible to overfill with override, but underfill is prevented
* Complete - Override will turn off the smart management until a safety threshold is crossed, at which point it comes back.  This can be useful if you only want to override to for example run water down or overfill, but also lets you practice water management while the mod protects you from accidental over/under, and you can give it back to the mod when needed.

#### Fire Management Options
The fire options only apply while the fire man is firing - when the fire is off and firing wasn't selected through the comm radio, the fireman will do nothing.
* None - Fireman will never add coal, it's entirely up to you to do so.
* Keep Burning - Fireman will not let the fire go out, and will start it, but it's up to you to add enough coal for your demand.
* Full - Fireman will do his best to ensure that the coal level is correct for the current pressure, pressure trend, and demand.

There are two other options though that control fireman behavior:
* Fireman manages blower and damper - if off, the fireman will leave these controls alone, allowing you to manually control them - a good first step towards full firing.
* Enable Auto Cylinder Cocks - **DEFAULT OFF** if on, then the fireman will open and close cylinder cocks automatically.  He will only open them if there is water in them and is _really efficient_ about closing them.  I'm hoping to make this a bit more realistic in the future.

### Planned Updates
* MU Mode - sets all engines except the one you were most recently in to "full management" with all features enabled, allowing you to have a fleet of locomotives behind you with working firemen while you can manage fire and water manually if you want in the front.  This will pair well with a future release of the no-cable MU mod which is adding support for steam engines.
