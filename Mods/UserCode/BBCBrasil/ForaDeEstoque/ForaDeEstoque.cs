// Servidor BBC Brasil -- mod "Fora de Estoque", v1
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
        Minhas,
        DaCidade,
    }

    [Serialized, CreateComponentTabLoc("Fora de Estoque", true), HasIcon("StoreComponent"), Priority(900)]
    public class ForaDeEstoqueComponent : WorldObjectComponent
    {
        public override WorldObjectComponentClientAvailability Availability
            => WorldObjectComponentClientAvailability.Always;

        [SyncToView] public override string IconName => "StoreComponent";

        [SyncToView, Autogen, Sort(0), UITypeName("GeneralHeader")]
        public string Title => "Fora de Estoque";

        [Eco(AccessType.FullAccess), Sort(1), Description(
            "Quais lojas aparecem para escolher. Minhas: as suas. Da cidade: todas as do assentamento desta mesa.")]
        public MostrarLojas Mostrar { get; set; } = MostrarLojas.Minhas;

        // PropReadOnly NAO E OPCIONAL AQUI.
        // Sem ele, [Autogen] faz o CLIENTE tratar a propriedade como editavel e chamar um
        // SetLoja/SetFalta que nao existe -> "Missing RPC call SetFalta" e o jogador cai
        // (medido em campo 15/09, com a aba ja funcionando). O servidor sobrevive; o cliente
        // nao. Molde: HotWheels/Components/ChargerComponent.cs, instalado aqui.
        [SyncToView, Autogen, PropReadOnly, UITypeName("StringDisplay"), Sort(2),
         Description("A loja que esta sendo monitorada.")]
        public string Loja => this.loja;

        [Autogen, RPC, Sort(3), UITypeName("BigButton"), Description(
            "Passa para a proxima loja da lista.")]
        public void ProximaLoja(Player player)
        {
            this.escolhida++;
            this.Recalcular(player);
        }

        [Autogen, RPC, Sort(4), UITypeName("BigButton"), Description(
            "Refaz a lista agora.")]
        public void Atualizar(Player player) => this.Recalcular(player);

        [SyncToView, Autogen, PropReadOnly, UITypeName("StringDisplay"), Sort(5),
         Description("O que esta mesa fabrica, esta zerado na loja, e voce ainda nao mandou fabricar.")]
        public string Falta => this.falta;

        // ---------------------------------------------------------------- estado interno
        [Serialized] int escolhida;          // indice da loja na lista filtrada
        [Serialized] string loja = "(aperte Atualizar)";
        [Serialized] string falta = "";
        User ultimoUsuario;                  // para o evento saber por quem recalcular

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
            TradeOffer.ItemUpdatedEvent.Add(_ => this.Recalcular(null));
        }

        // ---------------------------------------------------------------- o miolo
        void Recalcular(Player player)
        {
            try
            {
                var user = player?.User ?? this.ultimoUsuario;
                if (player != null) this.ultimoUsuario = player.User;

                var lojas = this.AcharLojas(user);
                if (lojas.Count == 0)
                {
                    this.Mostra(this.Mostrar == MostrarLojas.Minhas
                        ? "nenhuma loja sua"
                        : "nenhuma loja neste assentamento", "");
                    return;
                }

                // o indice roda em circulo, entao o botao nunca sai da lista
                if (this.escolhida < 0 || this.escolhida >= lojas.Count) this.escolhida = 0;
                var loja = lojas[this.escolhida];
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
                    Enfeita(string.Format("{0}   ({1} / {2})",
                        loja.SourceName, this.escolhida + 1, lojas.Count)),
                    faltando.Count == 0
                        ? Enfeita("Nada faltando aqui.")
                        : Enfeita(string.Join("\n", faltando)));
            }
            catch (Exception e)
            {
                // Diagnostico nao pode derrubar jogabilidade -- mesma regra do KabongLog.
                this.Mostra(this.loja, "erro ao montar a lista (ver log)");
                Log.WriteLine(Localizer.DoStr("[ForaDeEstoque] " + e.Message));
            }
        }

        /// <summary>As lojas que entram na lista de escolha, segundo o filtro.</summary>
        List<IHasTradeOffers> AcharLojas(User user)
        {
            var achadas = new List<IHasTradeOffers>();
            var assentamentoDaMesa = this.Parent?.CachedSettlementAtPos;

            WorldObjectManager.ForEach(wo =>
            {
                var loja = wo.GetComponent<StoreComponent>();
                if (loja == null) return;

                if (this.Mostrar == MostrarLojas.Minhas)
                {
                    // dono: a forma que o __core__ usa
                    if (user == null) return;
                    if (!(wo.Owners?.UserSet.Contains(user) ?? false)) return;
                }
                else
                {
                    // "da cidade" usa o assentamento DA MESA, nao o do jogador: e a mesa que
                    // vai fabricar, entao a aba responde igual para quem quer que a abra.
                    if (assentamentoDaMesa == null) return;
                    if (wo.CachedSettlementAtPos != assentamentoDaMesa) return;
                }
                achadas.Add(loja);
            });

            return achadas;
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

            // 3. as ofertas de VENDA zeradas
            var falta = new List<string>();
            foreach (var oferta in loja.AllOffers)
            {
                if (oferta == null) continue;
                if (oferta.Buying) continue;         // so venda: na mesa se fabrica, nao se compra
                if (oferta.IsTagOffer) continue;     // decisao do Raul: so item especifico
                var pilha = oferta.Stack;
                if (pilha == null || pilha.Quantity > 0) continue;   // ainda tem estoque
                var item = pilha.Item;
                if (item == null) continue;
                if (!fabrica.Contains(item.Type)) continue;          // esta mesa nao faz
                if (naFila.Contains(item.Type)) continue;            // ja mandou fabricar
                // UILink() e o que o jogo usa para escrever item no texto COM ICONE e
                // clicavel -- 553 usos no __core__. So o DisplayName sai como texto puro,
                // que foi o que o Raul viu em campo em 15/09.
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
