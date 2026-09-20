// Servidor BBC Brasil -- mod "Fora de Estoque", v2
//
// O QUE MUDOU NA v2 (16/09/2026), a pedido do Raul
//     1. A LOJA FICA PRESA A MESA. Antes a escolha era uma posicao na lista (`escolhida`),
//        e posicao muda sozinha quando uma loja nasce ou some -- a mesa passava a olhar
//        outra loja sem ninguem ter mexido nela. Agora guarda o NOME, que sobrevive a isso
//        e ao reinicio do servidor.
//     2. A LISTA JA ESTA PRONTA QUANDO A ABA ABRE. O pedido foi "atualizar sozinho ao
//        abrir a aba". NAO EXISTE esse gancho no Eco -- procurado no codigo do jogo
//        instalado (nenhum override On* de UI em componente) e na API oficial do ModKit;
//        o unico parente e ITickOnDemand, que e temporizador agendado por quem chama.
//        Entao o caminho adequado e o inverso: manter o estado sempre certo, para que
//        abrir a aba nao precise de nada. E o que a loja fixada viabiliza.
//     3. DE QUEBRA, UM DEFEITO DE DESEMPENHO NOSSO. Na v1 cada mesa do mundo recalculava a
//        cada mudanca de oferta, e cada recalculo varria TODOS os objetos do mundo atras de
//        lojas: uma venda custava (mesas) x (objetos do mundo). Agora mesa sem loja fixada
//        sai na primeira linha do evento, e a que tem faz uma conta pequena. Isso importa
//        neste servidor mais que em outro: o gargalo medido do Eco e UMA thread, que fica
//        em ~98% de um nucleo mesmo com o mundo vazio (secao 61 do CLAUDE.md).
//
// O QUE FAZ
//     Toda mesa de fabricacao ganha uma aba que mostra o que AQUELA mesa fabrica e esta
//     zerado na loja escolhida -- e some quando voce manda fabricar.
//
//         FALTA = (o que esta mesa fabrica)
//               INTERSECAO (ofertas de VENDA da loja escolhida com estoque 0)
//               MENOS (o que ja esta na fila desta mesa)
//
// POR QUE ESTADO E NAO SO EVENTO
//     O pedido foi "entra quando chega o alerta do sino". O instante do sino tem evento
//     publico -- TradeOffer.ItemUpdatedEvent -- e ele esta usado aqui, como GATILHO.
//     Mas a LISTA sai do ESTADO (oferta de venda com Stack.Quantity == 0), nao do evento.
//     Motivo: evento e um instante e se perde. Se o servidor reiniciar, se o item zerar
//     enquanto ninguem esta olhando, uma lista feita so de eventos nasce vazia e mente.
//     O estado esta sempre legivel e diz a mesma coisa que o sino disse.
//
// COMO ISTO SE ENCAIXA NA ORDEM DE DECISAO OFICIAL (Mods/UserCode/README.md)
//     classe nova : WorldObjectComponent      -> "for new objects"
//     [RequireComponent] em partial class     -> "existing classes customization"
//     ZERO .override.cs, ZERO Harmony, nenhum arquivo do __core__ copiado.
//     Override e copia congelada: a atualizacao do Eco deixa a copia velha SEM AVISAR --
//     foi assim que o Big fast shovel piorou a pa deste servidor.
//
// DE ONDE VEIO CADA API (nada aqui foi deduzido)
//     docs.play.eco, lida em 15/09/2026 -- ver API-ECO-MODKIT.md:
//         IHasTradeOffers.AllOffers        IEnumerable<TradeOffer>
//         IHasTradeOffers.SourceName       LocString
//         IHasTradeOffers.Owners           IAlias
//         IHasTradeOffers.Parent           WorldObject
//         TradeOffer.Buying / .IsTagOffer / .Stack (ItemStack)
//         TradeOffer.ItemUpdatedEvent      static ThreadSafeAction<TradeOffer>
//         CraftingComponent.Recipes        IEnumerable<RecipeFamily>
//         CraftingComponent.WorkOrders     ControllerList<WorkOrder>
//     ATENCAO ao namespace: TradeOffer esta em Eco.Gameplay.Components, e NAO em
//     Store.Internal como o StoreItemData. Errar isso e CS0246 e o servidor nao sobe.
//
//     codigo que JA COMPILA neste servidor:
//         a forma da aba                   CavRnMods/EcoGnome/StoreObject.cs
//         Owners?.UserSet.Contains(user)   __core__
//         Evento.Add(handler)              __core__
//         Recipes[0].Products              nosso CouroDobrado.cs
//         .Stack.Quantity                  __core__
//         WorldObjectManager.ForEach       __core__
//
// V1: UMA MESA SO (Workbench). Provar a aba antes de gerar as 69 -- e o passo 2 do plano
// do MOD-FORA-DE-ESTOQUE.md. Mesa de MOD fica de fora: referenciar tipo de mod nao
// instalado nao compila e derruba o servidor (a armadilha do PergaminhosDosMods.cs).

namespace Eco.Mods.TechTree
{
    using Eco.Core.Controller;
    using Eco.Core.Utils;
    using Eco.Gameplay.Components;
    using Eco.Gameplay.Components.Store;
    using Eco.Gameplay.Items;
    using Eco.Gameplay.Objects;
    using Eco.Gameplay.Players;
    using Eco.Gameplay.Systems.TextLinks;   // UILink() -- o link de item COM icone
    using Eco.Shared.Items;          // AccessType  -- SEM ISTO: CS0103 em AccessType
    using Eco.Shared.Localization;
    using Eco.Shared.Logging;
    using Eco.Shared.Networking;
    using Eco.Shared.Serialization;
    using Eco.Shared.SharedTypes;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;     // [Description] -- SEM ISTO: CS0246 em DescriptionAttribute
    using System.Linq;

    /// <summary>Quais lojas entram na lista de ESCOLHA. Monitorada e sempre UMA.</summary>
    [Serialized, Eco, Localized]
    public enum MostrarLojas
    {
        Mine,
        InTown,
    }

    [Serialized, CreateComponentTabLoc("Out of Stock", true), HasIcon("StoreComponent"), Priority(900)]
    public class ForaDeEstoqueComponent : WorldObjectComponent
    {
        public override WorldObjectComponentClientAvailability Availability
            => WorldObjectComponentClientAvailability.Always;

        [SyncToView] public override string IconName => "StoreComponent";

        [SyncToView, Autogen, Sort(0), UITypeName("GeneralHeader")]
        public string Title => "Out of Stock";

        [Eco(AccessType.FullAccess), Sort(1), Description(
            "Which stores show up in the list to choose from. Mine: yours. In town: every store in this table's settlement.")]
        public MostrarLojas Mostrar { get; set; } = MostrarLojas.Mine;

        // PropReadOnly NAO E OPCIONAL AQUI.
        // Sem ele, [Autogen] faz o CLIENTE tratar a propriedade como editavel e chamar um
        // SetLoja/SetFalta que nao existe -> "Missing RPC call SetFalta" e o jogador cai
        // (medido em campo 15/09, com a aba ja funcionando). O servidor sobrevive; o cliente
        // nao. Molde: HotWheels/Components/ChargerComponent.cs, instalado aqui.
        [SyncToView, Autogen, PropReadOnly, UITypeName("StringDisplay"), Sort(2),
         Description("The store being watched.")]
        public string Loja => this.loja;

        [Autogen, RPC, Sort(3), UITypeName("BigButton"), Description(
            "Move to the next store in the list. The one you stop on stays PINNED to this table.")]
        public void ProximaLoja(Player player)
        {
            // Exatamente como na v1: anda um e deixa o Recalcular mostrar o resultado --
            // inclusive o aviso quando a lista vem vazia. Foi mexer nisto que fez o botao
            // parecer quebrado em campo (16/09): eu pus a construcao da lista aqui, e
            // quando ela vinha vazia a tela nao mudava.
            //
            // Soltar a fixada e de proposito: sem isso o Recalcular realinharia pelo nome
            // e desfaria o pulo que o jogador acabou de pedir.
            this.escolhida++;
            this.lojaFixada = "";
            this.lojaRef = null;
            this.Recalcular(player);
        }

        [Autogen, RPC, Sort(4), UITypeName("BigButton"), Description(
            "Rebuild the list now. Also re-finds the pinned store if it was moved or renamed.")]
        public void Atualizar(Player player)
        {
            this.lojaRef = null;             // solta o cache e acha a loja fixada de novo
            this.Recalcular(player);
        }

        [SyncToView, Autogen, PropReadOnly, UITypeName("StringDisplay"), Sort(5),
         Description("What this table crafts, is sold out at the store, and you have not queued yet.")]
        public string Falta => this.falta;

        // ---------------------------------------------------------------- estado interno
        [Serialized] int escolhida;          // posicao na lista, so enquanto se escolhe
        [Serialized] int total;              // tamanho da lista da ultima escolha, so para exibir
        [Serialized] string lojaFixada = ""; // v2: a loja PRESA a esta mesa, guardada pelo NOME
        [Serialized] string loja = "(press Next store)";
        [Serialized] string falta = "";
        IHasTradeOffers lojaRef;             // cache da loja fixada. NAO serializado de proposito:
                                             // referencia de objeto do mundo nao sobrevive a
                                             // reinicio, e o nome sobrevive.
        User ultimoUsuario;                  // para o evento saber por quem recalcular

        /// <summary>Prende a loja a esta mesa. Guarda o NOME, nao a posicao na lista.
        ///
        /// A v1 guardava so o indice `escolhida`. Indice e fragil: basta uma loja nova
        /// nascer, ou uma sumir, para a mesa passar a olhar outra loja sem ninguem mexer
        /// nela. Nome sobrevive a isso e sobrevive ao reinicio do servidor.</summary>
        void Fixar(IHasTradeOffers escolha)
        {
            this.lojaFixada = escolha.SourceName.ToString();
            this.lojaRef = escolha;
        }

        /// <summary>Alinha a esquerda e aumenta letra e icone.
        /// Num lugar so, para trocar o tamanho sem cacar pelo arquivo.
        /// 130% foi o primeiro teste em campo (15/09) -- ficou legivel mas pequeno;
        /// 165% foi o pedido seguinte do Raul.</summary>
        const string TAMANHO = "165%";

        static string Enfeita(string texto)
            => "<align=\"left\"><size=" + TAMANHO + ">" + texto + "</size></align>";

        /// <summary>Troca o texto e AVISA o cliente. Sem o Changed, o valor muda no servidor
        /// e a aba continua mostrando o anterior -- padrao do __core__ (this.Changed(nameof(..))).</summary>
        void Mostra(string novaLoja, string novoFalta)
        {
            this.loja = novaLoja;
            this.falta = novoFalta;
            this.Changed(nameof(this.Loja));
            this.Changed(nameof(this.Falta));
        }

        public override void Initialize()
        {
            base.Initialize();
            // GATILHO: o mesmo instante do sino de "sem estoque".
            // Nao roda nada em Tick() -- so quando uma oferta muda de fato.
            //
            // v2 -- O GUARDA DA PRIMEIRA LINHA NAO E ENFEITE.
            // Na v1 este evento chamava Recalcular em TODA mesa do mundo, e cada
            // Recalcular varria TODOS os objetos do mundo atras de lojas
            // (WorldObjectManager.ForEach dentro de AcharLojas). Ou seja: uma unica venda
            // custava (mesas no mundo) x (objetos no mundo). Com a loja fixada, mesa que
            // ninguem configurou sai na primeira linha, e a que foi configurada faz uma
            // conta pequena: receitas desta mesa x ofertas de UMA loja x fila desta mesa.
            // Isto importa aqui mais que em outro servidor: o gargalo medido do Eco e uma
            // thread so, que ja fica em ~98% de um nucleo (secao 61 do CLAUDE.md).
            TradeOffer.ItemUpdatedEvent.Add(_ =>
            {
                if (string.IsNullOrEmpty(this.lojaFixada)) return;
                this.Recalcular(null);
            });
        }

        // ---------------------------------------------------------------- o miolo
        void Recalcular(Player player)
        {
            try
            {
                var user = player?.User ?? this.ultimoUsuario;
                if (player != null) this.ultimoUsuario = player.User;

                // DOIS CAMINHOS, e a diferenca entre eles e o ponto da v2.
                //
                //   player != null -> o jogador apertou um botao. Monta a lista como a v1
                //                     fazia, com os mesmos avisos, e FIXA a que sobrar.
                //   player == null -> veio do evento de oferta, sem ninguem olhando. Usa so
                //                     a loja ja fixada e NAO varre o mundo. Mesa que ninguem
                //                     configurou nao custa nada.
                IHasTradeOffers loja;

                if (player == null)
                {
                    loja = this.ResolverLoja();
                    if (loja == null) return;      // nada fixado: nada a atualizar
                }
                else
                {
                    var lojas = this.AcharLojas(user);
                    if (lojas.Count == 0)
                    {
                        this.Mostra(this.Mostrar == MostrarLojas.Mine
                            ? "no store of yours"
                            : "no store in this settlement", "");
                        return;
                    }

                    // Se ja havia uma fixada e ela continua na lista, fica nela -- e isto que
                    // faz a escolha sobreviver a loja nova nascendo, a loja sumindo e ao
                    // reinicio. O ProximaLoja solta a fixada antes de chamar, entao o pulo
                    // do jogador nao e desfeito aqui.
                    if (!string.IsNullOrEmpty(this.lojaFixada))
                    {
                        var i = lojas.FindIndex(l => l.SourceName.ToString() == this.lojaFixada);
                        if (i >= 0) this.escolhida = i;
                    }
                    // o indice roda em circulo, entao o botao nunca sai da lista
                    if (this.escolhida < 0 || this.escolhida >= lojas.Count) this.escolhida = 0;

                    this.total = lojas.Count;
                    loja = lojas[this.escolhida];
                    this.Fixar(loja);              // A ULTIMA QUE O JOGADOR DEIXOU FICA FIXA.
                }

                var faltando = this.Faltando(loja);

                // ALINHAMENTO E TAMANHO
                // O StringDisplay centraliza e usa fonte pequena por padrao (visto em campo
                // 15/09). As tags sao as mesmas que o jogo usa em placa -- <align>, <size> --
                // lidas do binario e provadas la (secao 47.12 do CLAUDE.md).
                // O icone do UILink() e um sprite inline: em TextMeshPro ele escala junto com
                // a fonte, entao <size> aumenta os dois de uma vez.
                // o nome da loja tambem aumenta: em campo ele ficou menor que a lista,
                // e e o cabecalho -- o que menos deveria ser o menor
                this.Mostra(
                    Enfeita(this.total > 1
                        ? string.Format("{0}   ({1} / {2})",
                            loja.SourceName, this.escolhida + 1, this.total)
                        : loja.SourceName.ToString()),
                    faltando.Count == 0
                        ? Enfeita("Nothing missing here.")
                        : Enfeita(string.Join("\n", faltando)));
            }
            catch (Exception e)
            {
                // Diagnostico nao pode derrubar jogabilidade -- mesma regra do KabongLog.
                this.Mostra(this.loja, "could not build the list (see log)");
                Log.WriteLine(Localizer.DoStr("[ForaDeEstoque] " + e.Message));
            }
        }

        /// <summary>A loja fixada nesta mesa. Achada UMA vez por arranque e guardada.
        ///
        /// E este cache que torna o recalculo barato o bastante para a lista estar sempre
        /// pronta -- que e o que o jogador percebe como "abriu a aba e ja estava certo".
        /// Nao ha gancho de "abriu a aba" no Eco: procurado no codigo do jogo instalado
        /// (nenhum override On* de UI) e na API oficial (docs.play.eco). O unico parente e
        /// ITickOnDemand, que e temporizador agendado por quem chama, nao evento de
        /// abertura. Entao o caminho e manter o estado certo, nao recalcular ao abrir.</summary>
        IHasTradeOffers ResolverLoja()
        {
            if (this.lojaRef != null) return this.lojaRef;
            if (string.IsNullOrEmpty(this.lojaFixada)) return null;

            // Procura pelo NOME e IGNORA o filtro Mine/InTown de proposito: o filtro serve
            // para montar a lista de escolha; depois de escolhida, a loja e aquela. Sem isso,
            // uma loja fixada com "Mine" ficaria irresolvivel quando o evento chega sem
            // jogador nenhum na mesa -- que e justamente o caso do recalculo automatico.
            IHasTradeOffers achada = null;
            WorldObjectManager.ForEach(wo =>
            {
                if (achada != null) return;
                var componente = wo.GetComponent<StoreComponent>();
                if (componente == null) return;
                // SourceName e membro de IHasTradeOffers, NAO de StoreComponent -- ler direto
                // do componente da CS1061, e foi o que derrubou o arranque de 16/09 11:33.
                // No AcharLojas a conversao acontecia sozinha ao entrar na List<IHasTradeOffers>,
                // e por isso o erro nao existia la.
                IHasTradeOffers loja = componente;
                if (loja.SourceName.ToString() == this.lojaFixada) achada = loja;
            });
            this.lojaRef = achada;
            return achada;
        }

        /// <summary>As lojas que entram na lista de escolha, segundo o filtro.
        ///
        /// RELATO DE CAMPO 16/09/2026: com "Minhas" selecionado nao vinha loja NENHUMA, e o
        /// Raul teve de trocar para "Da cidade" e passar por todas ate achar a dele.
        ///
        /// A v1 testava so `wo.Owners?.UserSet.Contains(user)` -- forma que existe no
        /// __core__ (Benefits/LavishWorkspace.cs:36), mas que depende de o dono estar no
        /// UserSet daquele objeto. O jogo tem OUTRA forma, e e a que ele usa para enumerar
        /// o que e de um usuario: WorldObjectManager.GetOwnedBy(user), em
        /// Benefits/FrugalWorkspace.cs:24 e LavishWorkspace.cs:25. Assinatura conferida na
        /// API oficial: public static IEnumerable&lt;WorldObject&gt; GetOwnedBy(User user).
        ///
        /// Aqui as DUAS valem, em uniao. Nao sei qual das duas falhou no caso dele -- para
        /// saber com certeza eu teria de instrumentar e gastar um arranque -- e uniao so
        /// pode ACRESCENTAR loja, nunca tirar. E, alem disso, "Da cidade" passou a trazer as
        /// SUAS lojas primeiro: assim, mesmo que a deteccao de dono falhe por um motivo que
        /// eu nao previ, a loja dele fica no comeco em vez do fim da fila.</summary>
        List<IHasTradeOffers> AcharLojas(User user)
        {
            var achadas = new List<IHasTradeOffers>();
            var minhas = new List<IHasTradeOffers>();
            var assentamentoDaMesa = this.Parent?.CachedSettlementAtPos;

            // o conjunto do proprio jogo, montado uma vez
            var doJogador = new HashSet<WorldObject>();
            if (user != null)
                foreach (var wo in WorldObjectManager.GetOwnedBy(user))
                    doJogador.Add(wo);

            WorldObjectManager.ForEach(wo =>
            {
                var loja = wo.GetComponent<StoreComponent>();
                if (loja == null) return;

                var ehMinha = user != null
                    && (doJogador.Contains(wo) || (wo.Owners?.UserSet.Contains(user) ?? false));

                if (this.Mostrar == MostrarLojas.Mine)
                {
                    if (!ehMinha) return;
                }
                else
                {
                    // "da cidade" usa o assentamento DA MESA, nao o do jogador: e a mesa que
                    // vai fabricar, entao a aba responde igual para quem quer que a abra.
                    if (assentamentoDaMesa == null) return;
                    if (wo.CachedSettlementAtPos != assentamentoDaMesa) return;
                }
                if (ehMinha) minhas.Add(loja); else achadas.Add(loja);
            });

            minhas.AddRange(achadas);      // as suas primeiro, o resto depois
            return minhas;
        }

        /// <summary>A conta do mod, em uma linha.</summary>
        List<string> Faltando(IHasTradeOffers loja)
        {
            var mesa = this.Parent?.GetComponent<CraftingComponent>();
            if (mesa == null) return new List<string>();

            // 1. o que ESTA mesa fabrica
            var fabrica = new HashSet<Type>();
            foreach (var familia in mesa.Recipes)
                foreach (var receita in familia.Recipes)
                    foreach (var produto in receita.Products)
                        if (produto?.Item != null) fabrica.Add(produto.Item.Type);

            // 2. o que ja esta na fila desta mesa -- e o "ja mandei fabricar" que o jogo
            //    marca sozinho. Marcacao feita a mao e marcacao que o jogador esquece.
            var naFila = new HashSet<Type>();
            foreach (var ordem in mesa.WorkOrders)
                if (ordem?.Recipe != null)
                    foreach (var receita in ordem.Recipe.Recipes)
                        foreach (var produto in receita.Products)
                            if (produto?.Item != null) naFila.Add(produto.Item.Type);

            // 3. ESTOQUE POR ITEM, nao por oferta.
            //    Uma loja pode ter varias linhas de oferta do MESMO item. O caso comum e
            //    a loja de comida: o mesmo prato em duas ofertas, uma com o alimento
            //    fresco a preco cheio e outra com o que esta abaixo de 50% de frescor
            //    mais barato. Decidindo oferta por oferta, a linha vazia dizia "fora de
            //    estoque" com a outra cheia ao lado. Medido em 20/09/2026 com o mundo de
            //    producao: 89 de 453 analises (20%) tinham esse falso positivo.
            var estoquePorItem = new Dictionary<Type, int>();
            foreach (var o in loja.AllOffers)
            {
                if (o == null || o.Buying || o.IsTagOffer) continue;
                var p = o.Stack;
                if (p == null || p.Item == null) continue;
                var t = p.Item.Type;
                estoquePorItem[t] = (estoquePorItem.ContainsKey(t) ? estoquePorItem[t] : 0) + p.Quantity;
            }

            // 4. as ofertas de VENDA cujo item esta zerado na loja INTEIRA
            var falta = new List<string>();
            var jaListado = new HashSet<Type>();   // duas ofertas vazias do mesmo item listavam duas vezes
            foreach (var oferta in loja.AllOffers)
            {
                if (oferta == null) continue;
                if (oferta.Buying) continue;         // so venda: na mesa se fabrica, nao se compra
                if (oferta.IsTagOffer) continue;     // decisao do Raul: so item especifico
                var pilha = oferta.Stack;
                if (pilha == null) continue;
                var item = pilha.Item;
                if (item == null) continue;
                var estoqueTotal = estoquePorItem.ContainsKey(item.Type) ? estoquePorItem[item.Type] : 0;
                if (estoqueTotal > 0) continue;                      // a loja tem, somando TODAS as ofertas
                if (jaListado.Contains(item.Type)) continue;         // ja listado por outra oferta vazia
                if (!fabrica.Contains(item.Type)) continue;          // esta mesa nao faz
                if (naFila.Contains(item.Type)) continue;            // ja mandou fabricar
                // UILink() e o que o jogo usa para escrever item no texto COM ICONE e
                // clicavel -- 553 usos no __core__. So o DisplayName sai como texto puro,
                // que foi o que o Raul viu em campo em 15/09.
                jaListado.Add(item.Type);
                falta.Add("  " + item.UILink());
            }
            falta.Sort();
            return falta;
        }
    }

    // AS MESAS ficam em ForaDeEstoqueMesas.cs, GERADO por gerar-mesas-fora-de-estoque.py,
    // que le a lista do proprio servidor (69 na 0.14.1.1). Separado de proposito: este
    // arquivo e escrito a mao e quase nao muda; a lista muda a cada versao do Eco.
}
