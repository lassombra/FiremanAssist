# FiremanAssist
Fireman Assist Mod for Derail Valley

Right now this mod only protects against water issues and does so in a very naive way.  Any loco you enter will automatically get the monitor attached if it is a supported loco.

#### Current Features
* Water management in 4 levels, selectable through UMM settings window:
 - None - turns off, useful if you want to disable the management temporarily without disabling the mod, say for a realistic save
 - No Explosions - Ensures you never let the water get too low.  Can still overfill and damage your cylinders, but will never underfill so long as you have water available to add.
 - Over/Underfill protection - Ensures you never let water get too low or too high.  _Might_ allow overfill if going downhill for an extended period of time, as it uses the same information the user has (the water glass)
 - Full management - Does its best to provide the optimum injector setting for the current situation.  Will aim to inject more water if the pressure is too high, and less water if the pressure is too low, to assist in firing in the sweet spot around 13 bar.  See notes about injector curves later.  If you manually adjust the injector more than about 5 percentage points, it'll disengage, dropping to over/under protection until one of those cases is hit.

#### Injector Override
The injector can be overriden by the user.  The manager has a few options for how it reacts:
- None - Override will be overwritten each tick - expect weird flickering if you try to mess with it
- Temporary - Override will stick until the manager decides it wants a new value.  Note that it is possible to overfill with override, but underfill is prevented
- Complete - Override will turn off the smart management until a safety threshold is crossed, at which point it comes back.  This can be useful if you only want to override to for example run water down or overfill, but also lets you practice water management while the mod protects you from accidental over/under, and you can give it back to the mod when needed.

#### Planned Features
* Adding coal to keep fire going
* Adding coal to heat fire if pressure drops
* Advanced fireman behavior

#### Injector curves
Currently there are 3 curves used.  When pressure is dropping below 13 bar, or is below 12 bar, the minimum fill curve is used to make it easier to get pressure back up.  If pressure is over 14 bar, the maximum fill curve is used to help prevent the safety popping (and thus wasting water) without having to aggressively close the damper (thus wasting coal)

The exact curves are:
- Minimum fill - 75% - 80% fill normalized, quad root curve.
- Maximum fill - 80% - 85% fill normalized, cube curve (not cube root, taken to the power of 3)
- Nominal fill - 75% - 81.66% (2/3 of the glass) fill normalized, square root curve.

The results of the curves are rounded to 10% and provide guidance - at  or below the "bottom" of each curve, the injector will be at 100%.  At or above the top, it'll be at 0%.  It then follows either a root or exponential curve between those points.
