# Out of Stock — a crafting-table tab that shows what your shop ran out of

**Eco 0.14.1** · server-side only · **no `.override.cs`, no Harmony**

Every crafting table gets a new tab listing **what that table can craft, is sold out in the
store you picked, and you haven't queued yet**.

```
OUT OF STOCK
Show stores:  [ Mine ▾ ]
              at 🏪 RM STORE
              [ Next store ]   [ Refresh ]     <- Next store picks the shop, Refresh builds the list

  Board
  Hardwood Board
  Large Standing Wood Sign
  Softwood Board
```

**Press "Next store"** until you land on the shop you want. That shop is then **pinned to that
table** — saved by name, so it survives a new shop appearing, an old one disappearing, and a
server restart, and you don't have to pick it again. **Press "Refresh" to build the list.**

> On a table you have not set up yet the tab opens empty — that is expected: it has no shop to
> look at until you press *Next store*. And **Eco has no "tab was opened" hook** (checked in the
> installed game code and in the ModKit API), so *Refresh* is what builds the list when you want
> to see it now.

> ⚠ **Tables from other mods need one extra step.** Out of the box the tab appears on the
> **69 crafting tables of the base game**. A table added by another mod — Mixology, IceCream,
> anything — only gets it after you run `instalar.py` on your own server. See *Install*.

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

**Nothing runs on a tick.** The list is built when you press *Next store* or *Refresh*, and
also when an offer in the pinned shop changes — the same instant the sold-out bell rings. A
table with no shop pinned does no work at all: it returns on the first line of the handler.

**What it does NOT do is build the list when you open the tab**, and that is not an oversight:
**Eco has no "tab was opened" hook** — checked in the installed game code and in the official
ModKit API, where the nearest thing is `ITickOnDemand` ("ticks only when explicitly requested").
So *Refresh* is the button that answers *show me now*.

**Reads state, not events.** The list comes from sell offers with `Stack.Quantity == 0`, not
from catching the sold-out moment. So it stays correct across server restarts, and after an
item sells out while you were offline.

---

## Install

Unzip into your server folder so that it merges with `Mods/` — these are the four files in
the package:

```
Mods/UserCode/BBCBrasil/ForaDeEstoque/ForaDeEstoque.cs
Mods/UserCode/BBCBrasil/ForaDeEstoque/ForaDeEstoqueMesas.cs     <- the 69 base-game tables
Mods/UserCode/BBCBrasil/ForaDeEstoque/MESAS-DE-MOD.txt          <- how to add modded tables
Mods/Translations/ForaDeEstoque.csv                             <- translation (optional)
```

Restart the server. That's it — no config file, no DLL, nothing to enable, no Python, and
**the order you installed your mods in does not matter**: Eco compiles the whole of
`Mods/UserCode` in a single pass at every startup.

**Uninstall:** delete the folder and restart. Nothing is written to the world.

### Tables from other mods — one extra step, on your machine

`ForaDeEstoqueMesas.cs` ships **ready-made** and covers the **69 crafting tables of the base
game** (Eco 0.14.1.1). A table added by another mod is *not* in it, and its line cannot be
shipped: a file naming a mod you don't have doesn't compile, and **a server that doesn't
compile doesn't start**. So that line is written on your own server, by a script that looks
at what you actually have.

The script is **not in the zip** — it lives in the repository:

```bash
# from https://github.com/dartangn/eco-fora-de-estoque
python3 instalar.py --seco    # says what it would do, writes nothing
python3 instalar.py           # writes ForaDeEstoqueMesasDeMod.cs next to the others
```

It finds the server on its own — `--raiz`, then the `ECO_SERVER` variable, then walking up
from where you ran it until it hits a `Mods/__core__`, then the running `EcoServer` process.
Linux and Windows, **Python 3 and nothing else**: no pip, no dependency.

**Run it again whenever your set of table mods changes:**

| when | what happens if you don't |
|---|---|
| you **add** a mod with a table | that table has no tab until the file is rebuilt |
| you **remove** a mod with a table | the line still names it → `CS0246`, and **the server will not start**. Delete that line, or the whole `ForaDeEstoqueMesasDeMod.cs` |
| **Eco updates** and changes the table list | run `gerar-mesas-fora-de-estoque.py` instead — that one rewrites the *base-game* list |

*Never copy `ForaDeEstoqueMesasDeMod.cs` from another server: it names that server's mods.*

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
- **Ship modded tables in the package** — the package covers the 69 base-game tables only.
  Modded tables are supported, but the line for them is **generated on your machine** by
  `instalar.py`, because referencing a type from a mod that isn't installed fails to compile
  and takes the whole server down — see *Install*.
- **Change anything.** It only reads. It places no orders and touches no offer.

---

## How the table list is built

Both lists are **generated**, never hand-written: the scripts read the server itself for every
object declaring `[RequireComponent(typeof(CraftingComponent))]`, so an Eco update that adds or
drops a table shows up in the count instead of going unnoticed.

| script | writes | run it when |
|---|---|---|
| `gerar-mesas-fora-de-estoque.py` | `ForaDeEstoqueMesas.cs` — the **base game** (ships ready-made, 69 on 0.14.1.1) | after an **Eco update**; it aborts rather than write an empty list |
| `instalar.py` | `ForaDeEstoqueMesasDeMod.cs` — **your mods**, and nobody else's | whenever you add or remove a mod with a table |

```bash
python3 gerar-mesas-fora-de-estoque.py --seco   # dry run first
python3 gerar-mesas-fora-de-estoque.py
```

Both find your server on their own, by walking up from where you run them until they hit a
`Mods/__core__` — the one folder only a real Eco server has. Run them from anywhere inside the
server tree, or pass `--raiz /path/to/server` if you keep the scripts somewhere else.

They also refuse to make a mess: `gerar-mesas-fora-de-estoque.py` aborts if it finds the same
table list already written somewhere else under `Mods/UserCode` — two copies would declare the
69 classes twice (`CS0101`) and the server would not start.

---

## Em português

Toda mesa de fabricação ganha uma aba que mostra **o que aquela mesa fabrica, está zerado na
loja que você escolheu, e você ainda não mandou fabricar**.

**Aperte "Próxima loja"** até chegar na loja certa: ela fica **presa àquela mesa**, gravada
pelo nome, e continua lá depois de reiniciar o servidor — você não precisa escolher de novo.
**Aperte "Atualizar" para montar a lista.** Depois de mandar fabricar, o item some. Ninguém
marca nada à mão: quem marca é a fila da própria mesa. Cancelou a ordem, ele volta.

> Na mesa que você ainda não configurou, a aba abre **vazia** — ela não tem loja para olhar
> até você apertar *Próxima loja*. E **o Eco não tem gancho de "abriu a aba"**, então quem
> monta a lista na hora em que você quer ver é o *Atualizar*.

Instalação: descompactar na pasta do servidor, de modo a fundir com `Mods/`, e reiniciar.
Para desinstalar, apagar a pasta. Nada é gravado no mundo. **Não precisa de Python, e a ordem
em que você instalou os seus mods não importa** — o Eco compila todo o `Mods/UserCode` numa
única passada a cada arranque.

> ### ⚠ Mesa de OUTRO MOD precisa de um passo a mais
>
> O pacote traz a lista pronta das **69 mesas do jogo base** (Eco 0.14.1.1). Mesa vinda de
> outro mod — Mixology, IceCream, qualquer um — **não entra por padrão**, e a linha dela não
> pode vir no pacote: arquivo que cita um mod que você não tem **não compila, e servidor que
> não compila não sobe**.
>
> Quem escreve essa linha é o `instalar.py`, **que não está no zip** — ele vem do repositório
> e olha o que você realmente tem instalado:
>
> ```bash
> python3 instalar.py --seco    # so diz o que faria
> python3 instalar.py           # grava ForaDeEstoqueMesasDeMod.cs
> ```
>
> **Rode de novo toda vez que mudar o conjunto de mods com mesa** — e principalmente ao
> **remover** um: a linha continua citando o mod que saiu, dá `CS0246` e **o servidor não
> sobe**. Apague a linha, ou o arquivo inteiro.

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
