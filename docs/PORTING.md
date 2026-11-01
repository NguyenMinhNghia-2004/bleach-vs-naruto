# AS3 → C# porting rules (BVN engine → Unity 6)

Source: `D:\Na vs Ble\BleachVsNaruto` (branch develop, BVN 3.7).
Target: `D:\Na vs Ble\BVN-Unity\Assets\BVN\Game\` (assembly `BVN.Game`, references `BVN.Runtime`).

Several people port packages **in parallel without seeing each other's code**. These rules make independently
ported files agree on names and signatures. Follow them literally even where idiomatic C# would differ.

## 1. Files, namespaces, names
- One C# file per AS3 class, same class name, mirrored sub-folder: `CORE_KernelLogic/src/net/play5d/game/bvn/fighter/ctrler/FighterCtrler.as`
  → `Assets/BVN/Game/Kernel/fighter/ctrler/FighterCtrler.cs`. CORE_Shared → `Game/Shared/...`, CORE_Utils → `Game/Utils/...`,
  LIB_KyoLib → `Game/Kyo/...`, SHELL_Pc → `Game/Shell/...`.
- **Every** class lives in the single flat namespace `BVN.Game` (class names are unique across Kernel/Shared/Utils/KyoLib).
- File header (always exactly these usings; add others only if needed):
  ```csharp
  using System;
  using System.Collections.Generic;
  using BVN.Flash;
  using BVN.Script;
  using static BVN.Game.AS3;
  using static BVN.Game.Globals;
  ```
  **Never** `using UnityEngine;` in ported files (it makes `Event`, `Sprite`, `Random`, `Debug`… ambiguous). Write `UnityEngine.X` fully qualified if you truly need Unity.
- Keep **every member name exactly as in AS3** (lowerCamelCase methods/properties/fields, `UPPER_CASE` constants). Frame scripts in the game
  assets call engine members by these names through reflection (`$mc_ctrler.setAttack(...)`), so renaming breaks the game.
- C# keyword clashes: prefix `@` (`@object`, `@params`, `@base`, `@event`, `@lock`, `@checked`, `@fixed`, `@operator`, `@in`, `@out`, `@ref`, `@string`, `@default`, `@internal`, `@char`, `@decimal`, `@namespace`, `@this`...).
- A member with the same name as its class is illegal in C#: append `_` to the member (only case allowed to rename).

## 2. Type mapping (mandatory, no exceptions)
| AS3 | C# |
|---|---|
| `Number` | `double` |
| `int` / `uint` | `int` / `uint` |
| `Boolean` / `String` / `void` | `bool` / `string` / `void` |
| `*`, `Object` | `object` (use `ASObject` only when **creating** an object literal `{a:1}` → `new ASObject{["a"]=1}`) |
| `Array` | `List<object>` |
| `Vector.<T>` | `List<T>` (T mapped by this table) |
| `flash.utils.Dictionary` | `Dictionary<object, object>` |
| `Function` | `Delegate` (call with `AS3.Call(fn, args...)`; pass methods as `(Action)foo`, `(Action<object>)foo`, … ) |
| `Class` | `object` (instantiate with `AS3.New(cls)`; values are `System.Type` or `BVN.Script.LinkedClass`) |
| `XML`/`XMLList` | `object` (avoid; configs are JSON) |
| `ByteArray`, sockets, `flash.filesystem.*`, LAN | out of scope → see §6 |
| flash.display/events/geom/text/media/filters/ui classes | same names in `BVN.Flash` (`MovieClip`, `Sprite`, `Bitmap`, `BitmapData`, `Shape`, `TextField`, `TextFormat`, `Point`, `Rectangle`, `Matrix`, `ColorTransform`, `Event`, `KeyboardEvent`, `MouseEvent`, `EventDispatcher`, `Sound`, `SoundChannel`, `SoundTransform`, `GlowFilter`, `Keyboard`, `Stage`…) |

Member access on `object` values (AS3 dynamic access `o.power`, `o["x"]`): use `AS3.Get(o, "power")`, `AS3.Set(o, "power", v)`,
and convert with `AS3.Num(...)`, `AS3.Int(...)`, `AS3.Str(...)`, `AS3.Bool(...)`. `AS3.Has(o, "k")` for `o.k != undefined` / `"k" in o`.

## 3. Declarations
- AS3 `public`, `protected`, `internal` → C# `public`. AS3 `private` → `private`.
- **All non-private instance methods and properties are `virtual`**; AS3 `override` → C# `override` (keep the base signature).
  Classes are never `sealed`. Interfaces → C# interfaces, getters/setters in interfaces → C# properties.
- `var` field → C# field. `get x()/set x()` → C# property `x` with same name. `const` → `const` (or `static readonly` for non-constants).
- Static initialisers / static code → static constructor or field initialisers.
- Optional params keep defaults (`double x = double.NaN` is legal). `...rest` → `params object[] rest`.
- Constructors: same parameters. `super(...)` → `: base(...)`.
- AS3 `int` math: `n:Number = a / b` with int a, b → `(double)a / b`. `int(x)` → `AS3.Int(x)` or `(int)x` when x is double.
- `Math.random()` → `AS3.random()` (deterministic, seedable). `Math.max/min` with >2 args → `AS3.max/min(params)`. Other `Math.*` → `Math.*` (System).
- `trace(...)` → `trace(...)` (from `AS3`). `getTimer()` / `setTimeout` / `clearTimeout` / `setInterval` → same names on `AS3`.
- `for each (var x:T in list)` → `foreach (var o in list) { var x = (T)o; ... }`; `for (var k in obj)` → `foreach (var k in AS3.Keys(obj))`.
- `delete d[k]` → `d.Remove(k)`; `k in d` → `d.ContainsKey(k)` (or `AS3.Has`).
- Array helpers: `.length` → `.Count`, `push` → `Add`, `pop()` → `AS3.pop(list)`, `shift()` → `AS3.shift(list)`, `splice` → `AS3.splice(list, start, count, params items)`,
  `indexOf` → `IndexOf`, `concat` → `AS3.concat`, `slice` → `AS3.slice`, `join` → `AS3.join`, `sortOn` → `AS3.sortOn`.
  Setting `.length = 0` → `.Clear()`.
- String helpers: `substr/substring/slice/split/charAt/replace` → `AS3.substr(s,...)`, etc. (`split` returns `List<object>`). `toFixed` → `AS3.toFixed(n, d)`.
- `JSON.parse` → `AS3.JSONParse(s)` (returns `ASObject` / `List<object>` / `double` / `string` / `bool`). `JSON.stringify` → `AS3.JSONStringify`.
- `obj is T` / `obj as T` unchanged. Casts `T(x)` → `x as T` for reference types.
- Event listeners: handler `function onX(e:Event):void` → `void onX(Event e)`; register with `addEventListener(Event.ENTER_FRAME, onX)`.
  Subclass events need the generic form: `addEventListener<KeyboardEvent>(KeyboardEvent.KEY_DOWN, onKeyDown)` and the same for remove.
- `try/catch(e:Error)` → `catch (Exception e)`; `e.getStackTrace()` → `e.StackTrace`; `throw new Error(m)` → `throw new Exception(m)`.

## 4. Timeline / asset interop
- A converted Flash document is `BVN.Data.LibraryData` (`LibraryData.Load("fighter/naruto")`). Library symbol by linkage class:
  `MovieClip.CreateLinked(lib, "bsface1")`, document main timeline: `MovieClip.CreateRoot(lib)`.
- Calling a function defined in a timeline frame script (AS3 `mc.initFighter(obj)` on a MovieClip) → `Runtime.CallClipFunction(mc, "initFighter", obj)`.
  Reading/writing timeline variables (`mc.$mc_ctrler`) → `Runtime.GetClipVar(mc, "$mc_ctrler")` / `Runtime.SetClipVar(...)`.
  Checking a frame label exists: `mc.HasLabel(name)`.
- `getDefinitionByName` / `ApplicationDomain.getDefinition` / loaded SWF class lookup → `AssetBridge` (see `Game/Bridge/AssetBridge.cs`).
- Child access by instance name on timeline clips: `mc.getChildByName("bdmn")` (AS3 `mc.bdmn` → same call).

## 5. Determinism
Game logic runs on the Stage tick (`GameRender` ENTER_FRAME). Never read Unity time or `System.Random` in ported logic.

## 6. Out of scope (stub instead)
LAN / sockets / UDP (`ctrler/lan`, `CORE_Shared/.../data/lan`, shell sockets), AIR file system, Harman/mobile specifics, analytics/web.
When ported code references them, write a minimal stub class with the same name and members doing nothing (put stubs in
`Game/Stubs/<Name>.cs`; check the folder first — another porter may already have created it; if so extend, do not duplicate).

## 7. Done criteria for a porting task
- Every assigned `.as` has a `.cs` with all members ported (no "TODO later" bodies for logic). Comments may be translated briefly or kept.
- `bash "/d/Na vs Ble/tools/fastcompile.sh"` shows **no errors in your files** except `CS0246/CS0234/CS0117/CS1061` that
  refer to types or members from packages assigned to other porters (they will exist after integration).
