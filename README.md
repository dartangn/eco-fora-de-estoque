# Out of Stock — a crafting-table tab that shows what your shop ran out of

**Eco 0.14.1** · server-side only · **no `.override.cs`, no Harmony**

Every crafting table gets a new tab listing **what that table can craft, is sold out in the
store you picked, and you haven't queued yet**.

```
FORA DE ESTOQUE  /  OUT OF STOCK
Show stores:  [ Mine ▾ ]
              at 🏪 RM STORE   (1 / 1)
              [ Next store ]   [ Refresh ]

  Board
  Hardwood Board
  Large Standing Wood Sign
  Softwood Board
```

The whole mod is one line:

```
MISSING = (what THIS table crafts)
        ∩ (SELL offers with 0 stock in the chosen store)
        − (what is already queued on THIS table)
```

---

## Why it exists

You keep a shop. Something sells out. The bell rings — and then you have to remember which
table makes it, walk there, and find it in a list of 200 recipes.

This tab answers that at the table itself: *what should I be making right now?*

## What makes it different from a to-do list

**Nothing to tick off.** The item disappears from the list the moment you queue it, because
the mod reads `CraftingComponent.WorkOrders` — the table's own queue. Cancel the order and
it comes back.

> A mark the player has to make by hand is a mark the player forgets to make.
> Here, the thing that marks it is the act of queuing, which they were going to do anyway.

**Reads state, not events.** The list comes from sell offers with `Stack.Quantity == 0`, not
from catching the sold-out moment. So it stays correct across server restarts, and after an
item sells out while you were offline.

---

## Install

Unzip into your server folder so that it merges with `Mods/`:

```
Mods/UserCode/BBCBrasil/ForaDeEstoque/ForaDeEstoque.cs
Mods/UserCode/BBCBrasil/ForaDeEstoque/ForaDeEstoqueMesas.cs
```

Restart the server. That's it — no config file, no DLL, nothing to enable.

**Uninstall:** delete the folder and restart. Nothing is written to the world.

---

## No override — and that is a feature, not purism

This mod uses only the two techniques the Strange Loop `Mods/UserCode/README.md` puts
**first**:

| What it does | What the official README calls it |
|---|---|
| a new `: WorldObjectComponent` class holding the tab | *"for new objects"* |
| `[RequireComponent(...)] public partial class XObject {}` | *"existing classes customization"* |

An `.override.cs` is a **frozen copy** of a game file: when Eco updates that file, your copy
silently goes stale. A mod built on `partial class` survives updates.

`Mods/UserCode/README.md` itself says *"**If you need to** completely override..."* — it is
the escape hatch, not the road.

---

## What it does NOT do

- **Buy offers** — a table crafts, it doesn't purchase. Sell offers only.
- **Tag offers** — specific items only.
- **Modded tables** — the 69 base-game tables only. Referencing a type from a mod that isn't
  installed fails to compile and takes the whole server down.
- **Change anything.** It only reads. It places no orders and touches no offer.

---

## How the table list is built

`ForaDeEstoqueMesas.cs` is **generated**, not hand-written. The generator reads the server
itself for every object declaring `[RequireComponent(typeof(CraftingComponent))]` — 69 on
0.14.1.1 — and aborts if the count changes without you noticing.

Re-run it after an Eco update:

```bash
python3 gerar-mesas-fora-de-estoque.py --seco   # dry run first
python3 gerar-mesas-fora-de-estoque.py
```

---

## Em português

Toda mesa de fabricação ganha uma aba que mostra **o que aquela mesa fabrica, está zerado na
loja que você escolheu, e você ainda não mandou fabricar**.

O item **some sozinho** quando você põe na fila — ninguém marca nada à mão. Cancelou a ordem,
ele volta.

Instalação: descompactar na pasta do servidor, de modo a fundir com `Mods/`, e reiniciar.
Para desinstalar, apagar a pasta. Nada é gravado no mundo.

Filtro *Minhas* mostra as suas lojas; *Da cidade* mostra as do assentamento **da mesa** — é a
mesa que vai fabricar, então a aba responde igual para quem quer que a abra.

---

## Credits

Built for the **BBC-Brasil** Eco server. Tested on Eco 0.14.1.1 with 29 other mods installed.

The tab mechanism follows the pattern used by **Eco Gnome** and **GoodPrice**, whose source
made the approach clear.
