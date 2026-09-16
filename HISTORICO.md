# Fora de Estoque — histórico

## v2 — aprovada em campo em 16/09/2026

```
00891b430cf0ef6733a2e2cbffe4b3dc   ForaDeEstoque-v2.0-Eco14.1.zip   11.772 bytes
ccc379cdca30c6e26a9a93aae8cdfb06   ForaDeEstoque.cs                 22.795 bytes   escrito a mao
a38dd3e8219b28abd0138fc4fee77fa5   ForaDeEstoqueMesas.cs             7.533 bytes   GERADO
```

**O que mudou**

1. **A loja fica presa à mesa**, gravada pelo nome — sobrevive a loja nova nascendo, a loja
   sumindo e ao reinício do servidor. A v1 guardava a *posição* na lista, que muda sozinha.
   A fixação é **implícita**: a última que o jogador deixar selecionada é a que fica.
2. **A lista já está pronta quando a aba abre.** Não existe gancho de "abriu a aba" no Eco —
   procurado no código do jogo instalado e na API oficial do ModKit. Então o caminho é o
   inverso: manter o estado certo, para abrir não precisar de nada.
3. **Defeito de desempenho da v1, corrigido.** Cada mesa do mundo recalculava a cada mudança
   de oferta, e cada recálculo varria **todos os objetos do mundo** atrás de lojas. Agora mesa
   sem loja fixada sai na primeira linha do evento.
4. **Filtro "Minhas" com as duas formas de dono** — `Owners.UserSet` e
   `WorldObjectManager.GetOwnedBy`; e "Da cidade" traz as suas lojas primeiro.
5. **`MESAS-DE-MOD.txt` no pacote**, explicando como acrescentar mesa de mod e por que ela não
   vem pronta.

**O que o campo corrigiu, e vale registrar**

- Uma regressão minha: eu movi a construção da lista para dentro do botão, e com lista vazia
  ele saía sem mudar a tela — parecia que "só piscava". Voltou ao fluxo da v1.
- O `SourceName` é membro de `IHasTradeOffers`, **não** de `StoreComponent`. Ler direto da
  classe dá `CS1061` e derruba o arranque. Foi o arranque vigiado que pegou.
- O `"Minhas"` vazio do Raul **não era defeito do mod**: o terreno dele estava sem claim, e
  sem escritura não há dono. O filtro estava certo o tempo todo.

---

## v1 — congelada em 15/09/2026

**Esta versão foi provada em campo pelo Raul no servidor de teste.** Guardada antes de
qualquer experimento; se algo adiante quebrar, é para cá que se volta.

```
3afea5f117a4b35f6554c6e69f9519b0   ForaDeEstoque.cs          13.392 bytes   escrito a mao
a38dd3e8219b28abd0138fc4fee77fa5   ForaDeEstoqueMesas.cs      7.533 bytes   GERADO
e9e82cac4e66b293587e14ce42e7f81e   gerar-mesas-fora-de-estoque.py
```

---

## O que ela faz

Toda mesa de fabricação ganha uma aba **Fora de Estoque**:

```
FORA DE ESTOQUE
Mostrar:  [ Minhas ▾ ]
          at 🏪 RM STORE  (1 / 1)
          [ Próxima loja ]
          [ Atualizar ]

Board
Hardwood Board
Large Standing Wood Sign
Softwood Board
```

Uma linha resume o mod:

```
FALTA = (o que ESTA mesa fabrica)
      ∩ (ofertas de VENDA da loja escolhida com estoque 0)
      − (o que ja esta na fila desta mesa)
```

**O item some sozinho quando você manda fabricar** — ninguém marca nada à mão. Cancelou a
ordem, ele volta. Confirmado em campo: *"fabriquei, cliquei no atualizar e funcionou"*.

---

## Provado em campo

| | |
|---|---|
| a aba aparece nas **69 mesas** do jogo base | ✔ (testado na Carpentry Table) |
| acha as lojas e mostra `NOME (n / total)` | ✔ |
| lista só o que a mesa fabrica e está zerado | ✔ |
| **some ao mandar fabricar** | ✔ |
| item com ícone e clicável (`UILink()`) | ✔ |
| alinhado à esquerda, 165% | ✔ |
| não derruba o cliente | ✔ (`PropReadOnly`) |
| `0 error CS`, `0 Failed to start` | ✔ |

Com isso ficam provadas as quatro APIs que o pré-voo **não consegue** validar — no Linux o
Eco é um binário único, então elas não aparecem em `.cs` nenhum:

```
IHasTradeOffers.AllOffers      TradeOffer.Stack / .Buying / .IsTagOffer
IHasTradeOffers.SourceName     CraftingComponent.Recipes / .WorkOrders
```

---

## As quatro coisas que custaram um ciclo cada

Guardadas aqui porque, sem elas, quem retomar repete:

| Erro | Sintoma | Conserto |
|---|---|---|
| faltavam `using System.ComponentModel` e `Eco.Shared.Items` | 15 erros CS, teste derrubado | os dois estão no molde do Eco Gnome |
| `{ get; set; }` + `[Autogen]` em propriedade de exibição | **`Missing RPC call SetFalta`** — cliente desconectado | **`PropReadOnly`** + `=>` sobre campo privado |
| `item.DisplayName` | texto puro, sem ícone | **`item.UILink()`** (553 usos no `__core__`) |
| `grep -A6` para achar as mesas | devolveu **4** em vez de 69 | há até 12 atributos entre a âncora e a classe |

*O primeiro ganhou trava: o bloco 6 do `conferir-cs-novo.py`. Os outros não têm trava
possível — o que os evita é ler o molde inteiro antes de escrever.*

---

## Como reinstalar esta versão

Copie o `.cs` para a pasta do mod na sua instalação e regere a lista de mesas:

```bash
# <servidor> = a raiz da sua instalacao do Eco
cp ForaDeEstoque.cs <servidor>/Mods/UserCode/BBCBrasil/ForaDeEstoque/
python3 gerar-mesas-fora-de-estoque.py
```

Ou, mais simples, `python3 instalar.py` — ele acha a instalação sozinho e regera as duas
listas de mesas.

**Arranque vigiado, sempre** (Regra 18). E se o Eco for atualizado, rodar o gerador de novo:
ele lê a lista de mesas do próprio servidor e avisa se a contagem mudou.

---

## O que NÃO está aqui

- **README e traduções** para o mod.io
- o empacotamento `Mods/UserCode/<Autor>/<Mod>/` com título `Fora de Estoque [14.1]`
- as **3 mesas de mod** (ficam de fora de propósito: referenciar tipo de mod não instalado
  não compila e derruba o servidor)
- o seletor de **instância** de loja — a v1 usa o botão *Próxima loja*, que é vocabulário
  provado; um seletor de verdade continua sendo experimento
