# Out of Stock — a crafting-table tab that shows what your shop ran out of

**Eco 0.14.1** · server-side only · **no `.override.cs`, no Harmony**

Every crafting table gets a new tab listing **what that table can craft, is sold out in the
store you picked, and you haven't queued yet**.

```
OUT OF STOCK
Show stores:  [ Mine ▾ ]
              at 🏪 RM STORE   (1 / 1)
              [ Next store ]   [ Refresh ]     <- press Refresh to build the list

  Board
  Hardwood Board
  Large Standing Wood Sign
  Softwood Board
```

**Press Refresh** to load your stores and to rebuild the list. It does not update by itself.

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

**Nothing to tick off.** Queue the item on the table, press **Refresh**, and it is gone from
the list — because the mod reads `CraftingComponent.WorkOrders`, the table's own queue.
Cancel the order and it comes back.

> A mark the player has to make by hand is a mark the player forgets to make.
> Here, the thing that marks it is the act of queuing, which they were going to do anyway.

**The list is built when you press Refresh**, not continuously. That is also how you pick up
a store the first time. Nothing runs on a tick — the mod costs nothing while you are not
looking at it.

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

**Aperte Atualizar** para a sua loja aparecer e para montar a lista — ela não se atualiza
sozinha. Depois de mandar fabricar, aperte Atualizar de novo e o item some. Ninguém marca
nada à mão: quem marca é a fila da própria mesa. Cancelou a ordem, ele volta.

Instalação: descompactar na pasta do servidor, de modo a fundir com `Mods/`, e reiniciar.
Para desinstalar, apagar a pasta. Nada é gravado no mundo.

Filtro *Minhas* mostra as suas lojas; *Da cidade* mostra as do assentamento **da mesa** — é a
mesa que vai fabricar, então a aba responde igual para quem quer que a abra.

---

## Credits — and how this mod was made

**The idea, the design, the requirements and all the testing are by
[dartangn](https://github.com/dartangn)**, who runs the BBC-Brasil Eco server and wanted an
answer to a real problem: something sells out in the shop, and you have to remember which
table makes it.

**The code was written by AI**, working from the official Strange Loop Games documentation —
the `Mods/UserCode/README.md` that ships with the server, and the ModKit API reference at
[docs.play.eco](https://docs.play.eco). Every API used here was read from that reference or
from code already running on the server; none of it was guessed.

That division matters, so it is stated plainly: a human decided **what** this should do and
proved that it does it; an AI wrote the C#.

**It was tested on a live server, not in theory.** A test server running Eco 0.14.1.1 with
29 other mods, on a copy of the production world. Field testing caught four things no amount
of code reading would have: a missing `PropReadOnly` that disconnected the client, item names
rendering without icons, the list being centered and too small, and — after the README was
already written — that the list does **not** refresh by itself.

The tab mechanism follows the pattern used by **Eco Gnome** and **GoodPrice**, whose source
made the approach clear.

### Em português

**A ideia, o desenho, a necessidade e todos os testes são do
[dartangn](https://github.com/dartangn)**, que administra o servidor Eco BBC-Brasil.

**O código foi escrito por IA**, com base na documentação oficial da Strange Loop Games — o
`Mods/UserCode/README.md` que vem com o servidor e a referência da API em
[docs.play.eco](https://docs.play.eco). Toda API usada aqui foi lida dessas fontes ou de
código que já roda no servidor; nada foi adivinhado.

Um humano decidiu **o que** o mod deve fazer e provou que ele faz; a IA escreveu o C#.
