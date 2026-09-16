# Changelog

All notable changes to **Out of Stock** are listed here.

Format: [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) ·
Versioning: [Semantic Versioning](https://semver.org/) — which is also the `x.y.z` shape the
mod.io version field expects.

> Strange Loop Games does not publish a changelog convention for mods (neither `EcoModKit` nor
> `EcoIssues` carries one), so this follows the general standard. What their platform *does*
> define is the per-file **Changelog** box on mod.io — one change per line — and each entry
> below is written so it can be pasted straight into it.

---

## [2.0.1] — 2026-09-16

Documentation only. **No code changed** — `ForaDeEstoque.cs` and `ForaDeEstoqueMesas.cs` are
byte for byte the 2.0.0 files. If you already run 2.0.0 and have no tables from other mods,
there is nothing here for you.

### Fixed
- **The install instructions were wrong about install order.** 2.0.0 said to install this mod
  *last*, after every mod that adds a crafting table. That is not how it works: the table list
  ships **ready-made** for the 69 base-game tables and nothing is scanned at install time, and
  Eco compiles the whole of `Mods/UserCode` in one pass at every startup — **so the order does
  not matter**. What actually needs to come after your table mods is *running `instalar.py`*,
  which is a separate, optional step and is **not in the zip**. The old wording sent people to
  do something with no effect while leaving out the step that has one.
- **`MESAS-DE-MOD.txt`** (the only instructions that travel inside the package) now leads with
  the one-command route, gives a `findstr` line for **Windows** servers next to the `grep` one
  (there is no `grep` on Windows), and says plainly that those commands list *files* — the
  class name has to be read out of them.
- **Removing a table mod now carries the warning it always needed:** the generated line still
  names a type that is gone, which is `CS0246`, and **the server will not start**.

---

## [2.0.0] — 2026-09-16

Approved in the field on a test server running Eco 0.14.1.1 with 29 other mods.

### Added
- **The store is pinned to the table.** Press *Next store* until you land on the shop you want;
  that shop stays on that table. It is saved **by name**, so it survives a new shop appearing,
  an old one disappearing, and a server restart. There is no extra button — the last shop you
  leave selected is the one that is kept.
- **`MESAS-DE-MOD.txt`** ships inside the mod folder: how to add crafting tables from other
  mods, the three requirements, and why those lines are not shipped ready-made.
- *In town* now lists **your own shops first**.

### Changed
- **The tab is already up to date when you open it.** The list is rebuilt when an offer in the
  pinned shop changes — the same instant the sold-out bell rings — instead of only when you
  press *Refresh*. Eco has no "tab was opened" hook (checked in the installed game code and in
  the ModKit API), so the design keeps the state correct rather than rebuilding on open.
- The **Mine** filter now accepts both ownership checks the game itself uses:
  `WorldObject.Owners` and `WorldObjectManager.GetOwnedBy`.
- *Refresh* also re-finds the pinned shop, in case it was renamed.

### Fixed
- **Performance.** In 1.0.0 every crafting table in the world rebuilt its list on every trade
  offer change, and every rebuild scanned **every object in the world** looking for shops — so
  a single sale cost *(tables) × (world objects)*. A table with no shop pinned now does no work
  at all, and a table with one does a small, local calculation.

### Notes
- ~~Install this mod last, after every mod that adds a crafting table.~~ **This was wrong, and
  is corrected in 2.0.1:** install order does not matter. What must come after your table mods
  is *running `instalar.py`*, and only if you want modded tables covered.
- Unchanged: still server-side only, no `.override.cs`, no Harmony, nothing written to the
  world. Uninstalling is deleting the folder.

---

## [1.0.0] — 2026-09-15

First public release.

### Added
- A new **Out of Stock** tab on the 69 crafting tables of the base game, listing
  `(what THIS table crafts) ∩ (sell offers with 0 stock in the chosen shop) − (what is already
  queued on THIS table)`.
- Choice of which shops to pick from: **Mine** or **In town** (the settlement of the *table*,
  not of the player — the table is what will craft, so the tab answers the same for anyone who
  opens it).
- Items are shown with the game's own icon and link (`UILink()`), not as plain text.
- Nothing to tick off by hand: queueing the item on the table is what removes it from the list,
  because the mod reads `CraftingComponent.WorkOrders`. Cancel the order and it comes back.

---

## Em português

**2.0.1 — 16/09/2026.** Só documentação; **o código não mudou** (os dois `.cs` são byte a byte
os da 2.0.0). A 2.0.0 mandava instalar este mod **por último**, e isso está errado: a lista das
69 mesas do jogo base vem **pronta** no pacote, nada é varrido na instalação, e o Eco compila
todo o `Mods/UserCode` numa única passada a cada arranque — **a ordem não importa**. O que
precisa vir depois dos seus mods de mesa é *rodar o `instalar.py`*, que é passo separado,
opcional, e **não está no zip**. O `MESAS-DE-MOD.txt` passou a começar pelo caminho de um
comando, ganhou a linha equivalente com `findstr` para servidor **Windows** (que não tem
`grep`), e o aviso que faltava: **ao remover um mod de mesa**, a linha gerada continua citando
um tipo que não existe mais — `CS0246`, e o servidor não sobe.

**2.0.0 — 16/09/2026.** A loja fica **presa à mesa**, gravada pelo nome, e sobrevive ao
reinício; a fixação é implícita — a última que você deixar selecionada é a que fica. A lista já
está pronta quando a aba abre, porque ela é refeita quando uma oferta da loja fixada muda, e não
só quando se aperta *Atualizar*. O filtro *Minhas* passou a aceitar as duas formas de dono que o
próprio jogo usa, e *Da cidade* traz as suas lojas primeiro. Corrigido um defeito de desempenho
da 1.0.0: cada mesa do mundo varria **todos os objetos do mundo** a cada mudança de oferta.
Entrou também o `MESAS-DE-MOD.txt`, explicando como acrescentar mesa de outro mod.
*(A frase "instale por último" que saiu aqui na 2.0.0 estava errada — ver 2.0.1.)*

**1.0.0 — 15/09/2026.** Primeira versão pública: a aba nas 69 mesas do jogo base, escolha entre
*Minhas* e *Da cidade*, itens com ícone e link do próprio jogo, e nada para marcar à mão — quem
marca é a fila da própria mesa.

[2.0.1]: https://mod.io/g/eco/m/out-of-stock-141
[2.0.0]: https://mod.io/g/eco/m/out-of-stock-141
[1.0.0]: https://mod.io/g/eco/m/out-of-stock-141
