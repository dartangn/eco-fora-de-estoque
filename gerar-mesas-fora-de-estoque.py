#!/usr/bin/env python3
# -*- coding: ascii -*-
"""
gerar-mesas-fora-de-estoque.py -- gera o arquivo que pendura a aba "Fora de Estoque" em
TODAS as mesas de fabricacao do jogo base.

    python3 gerar-mesas-fora-de-estoque.py --seco     # mostra o que faria, nao grava
    python3 gerar-mesas-fora-de-estoque.py            # grava

    --raiz <caminho>      a raiz da instalacao do Eco (a pasta que contem Mods/)
    --destino <arquivo>   onde gravar o .cs gerado

Sem --raiz, ele PROCURA a instalacao: sobe a partir desta pasta e do diretorio atual ate
achar um "Mods/__core__". Sem --destino, grava ao lado dos outros arquivos do mod, em
Mods/UserCode/BBCBrasil/ForaDeEstoque/ForaDeEstoqueMesas.cs.

RODA NO SERVIDOR, e le a lista do proprio servidor -- nunca de uma lista escrita a mao.
Assim uma atualizacao do Eco que acrescente ou tire mesa aparece na contagem, em vez de
passar despercebida.

POR QUE UM ARQUIVO SEPARADO DO COMPONENTE
    O componente (ForaDeEstoque.cs) e escrito a mao e quase nunca muda.
    A lista de mesas e GERADA e muda a cada versao do Eco. Separados, regerar a lista nao
    arrisca o codigo, e o diff de uma atualizacao mostra exatamente o que o jogo mudou.

MESA DE MOD FICA DE FORA, de proposito
    Referenciar tipo de mod nao instalado nao compila e DERRUBA o servidor. Mod publico tem
    de cobrir so o jogo base; mesa de mod vira arquivo extra opcional -- ver MESAS-DE-MOD.txt.
"""
import os, re, sys

ARGS = sys.argv[1:]
SECO = "--seco" in ARGS
ESPERADO = 69          # medido em 15/09/2026 na 0.14.1.1

# caminho relativo, a partir da raiz do servidor, de tudo que este script toca
REL_CORE = os.path.join("Mods", "__core__")
REL_USERCODE = os.path.join("Mods", "UserCode")
REL_DESTINO = os.path.join(REL_USERCODE, "BBCBrasil", "ForaDeEstoque", "ForaDeEstoqueMesas.cs")


def opcao(nome):
    """--nome VALOR ; devolve None se nao foi passada."""
    if nome in ARGS:
        i = ARGS.index(nome)
        if i + 1 < len(ARGS):
            return ARGS[i + 1]
        sys.exit("[XX] %s exige um valor" % nome)
    return None


def achar_raiz():
    """Acha a raiz do servidor. Mesma ordem do instalar.py, para nao divergirem:
    --raiz -> variavel ECO_SERVER -> subindo a partir daqui e do diretorio atual.

    A marca e Mods/__core__: o codigo do JOGO, que so o proprio Eco cria. Procurar por
    "EcoServer" ou por "Mods" pegaria pasta de download ou de backup.
    """
    env = os.environ.get("ECO_SERVER")
    if env and os.path.isdir(os.path.join(env, REL_CORE)):
        return os.path.abspath(env)
    candidatos = []
    for base in (os.path.dirname(os.path.abspath(__file__)), os.path.abspath(os.getcwd())):
        atual = base
        while True:
            candidatos.append(atual)
            pai = os.path.dirname(atual)
            if pai == atual:
                break
            atual = pai
    for c in candidatos:
        if os.path.isdir(os.path.join(c, REL_CORE)):
            return c
    sys.exit("[XX] nao achei a instalacao do Eco (nenhum %s acima daqui).\n"
             "     Passe --raiz <caminho da pasta que contem Mods/>" % REL_CORE)


RAIZ = opcao("--raiz") or achar_raiz()
CORE = os.path.join(RAIZ, REL_CORE)
USERCODE = os.path.join(RAIZ, REL_USERCODE)
DESTINO = opcao("--destino") or os.path.join(RAIZ, REL_DESTINO)

if not os.path.isdir(CORE):
    sys.exit("[XX] %s nao existe -- --raiz aponta para fora da instalacao" % CORE)

print("=== instalacao ===")
print("   raiz    : %s" % RAIZ)
print("   destino : %s" % DESTINO)
print()

ATRIBUTO = re.compile(r"RequireComponent\(typeof\(CraftingComponent\)\)")
CLASSE = re.compile(r"public\s+partial\s+class\s+([A-Za-z0-9_]+Object)\b")


def ler(caminho):
    try:
        with open(caminho, encoding="utf-8-sig", errors="replace") as fh:
            return fh.read()
    except Exception:
        return ""


def achar_mesas():
    """Toda classe *Object que declara RequireComponent(CraftingComponent).

    Cuidado que ja custou um ciclo: entre o atributo e a classe ha ATE 12 outros
    atributos (a CarpentryTable tem 10). Um `grep -A6` nao alcanca a classe e devolve
    4 mesas em vez de 69 -- parece resposta e e falso negativo. Por isso a busca vai do
    atributo ate a PRIMEIRA declaracao de classe depois dele, sem contar linhas.

    Em Python puro de proposito: assim funciona tambem em servidor Windows, que nao tem grep.
    """
    mesas = set()
    for pasta, _dirs, arqs in os.walk(CORE):
        for a in arqs:
            if not a.endswith(".cs"):
                continue
            texto = ler(os.path.join(pasta, a))
            if "CraftingComponent" not in texto:
                continue
            for m in ATRIBUTO.finditer(texto):
                c = CLASSE.search(texto[m.end(): m.end() + 2000])
                if c:
                    mesas.add(c.group(1))
    return sorted(mesas)


mesas = achar_mesas()
print("=== mesas de fabricacao do jogo base ===")
print("   achadas: %d   (esperado: %d)" % (len(mesas), ESPERADO))

if len(mesas) == 0:
    sys.exit("[XX] nenhuma mesa achada. O padrao mudou -- NAO vou gerar arquivo vazio.")
if len(mesas) != ESPERADO:
    print("   [!!] a contagem MUDOU. Se o Eco foi atualizado, isto e esperado --")
    print("        confira a lista abaixo e ajuste ESPERADO no topo deste arquivo.")

for i in range(0, len(mesas), 3):
    print("   " + "".join("%-34s" % x for x in mesas[i:i+3]))

# ---- TRAVA: nenhuma outra copia desta lista pode existir em UserCode
#
# Se uma versao anterior gravou em OUTRA pasta, as 69 classes nasceriam duas vezes e o
# servidor nao sobe (CS0101). Melhor abortar alto aqui do que descobrir no arranque.
alvo = os.path.abspath(DESTINO)
outras = []
if os.path.isdir(USERCODE):
    for pasta, _dirs, arqs in os.walk(USERCODE):
        for a in arqs:
            if not a.endswith(".cs"):
                continue
            p = os.path.abspath(os.path.join(pasta, a))
            if p == alvo:
                continue
            if "RequireComponent(typeof(ForaDeEstoqueComponent))" in ler(p):
                outras.append(p)
print()
if outras:
    print("=== [XX] a lista de mesas JA EXISTE em outro arquivo ===")
    for p in outras:
        print("   %s" % p)
    sys.exit("[XX] gravar tambem no destino criaria as classes duas vezes (CS0101).\n"
             "     Apague o antigo, ou aponte --destino para ele.")

linhas = [
    "// GERADO por gerar-mesas-fora-de-estoque.py -- NAO EDITAR A MAO.",
    "// Pendura a aba \"Fora de Estoque\" nas %d mesas de fabricacao do jogo base." % len(mesas),
    "//",
    "// A lista e lida do proprio servidor, nunca escrita a mao: assim uma atualizacao do Eco",
    "// que acrescente ou tire mesa aparece na contagem em vez de passar despercebida.",
    "//",
    "// Mesa de MOD fica de fora: referenciar tipo de mod nao instalado nao compila e derruba",
    "// o servidor. Como acrescentar as suas: MESAS-DE-MOD.txt, ao lado deste arquivo.",
    "//",
    "// Tecnica: [RequireComponent] em partial class -- o que o Mods/UserCode/README.md chama",
    "// de \"existing classes customization\". ZERO override, ZERO Harmony.",
    "",
    "namespace Eco.Mods.TechTree",
    "{",
    "    using Eco.Gameplay.Objects;",
    "",
]
for m in mesas:
    linhas.append("    [RequireComponent(typeof(ForaDeEstoqueComponent))] public partial class %s { }" % m)
linhas += ["}", ""]
texto = "\n".join(linhas)

if SECO:
    print("=== MODO SECO -- nada foi gravado ===")
    print("   geraria %d linhas em %s" % (len(linhas), DESTINO))
    print("   primeiras 3 mesas:")
    for m in mesas[:3]:
        print("      [RequireComponent(typeof(ForaDeEstoqueComponent))] public partial class %s { }" % m)
    sys.exit(0)

pasta_destino = os.path.dirname(DESTINO)
if pasta_destino and not os.path.isdir(pasta_destino):
    sys.exit("[XX] a pasta do destino nao existe: %s\n"
             "     Instale o mod primeiro (a pasta vem no zip)." % pasta_destino)

tmp = DESTINO + ".tmp"
with open(tmp, "w", encoding="ascii", newline="\n") as fh:
    fh.write(texto)
os.replace(tmp, DESTINO)

# conferir RELENDO do disco, nao pelo que o script acha que escreveu
with open(DESTINO, encoding="ascii") as fh:
    lido = fh.read()
n = lido.count("RequireComponent(typeof(ForaDeEstoqueComponent))")
print("=== CONFERINDO RELENDO DO DISCO ===")
print("   %s" % DESTINO)
print("   %d bytes   %d linhas de RequireComponent   (esperado %d)" % (len(lido), n, len(mesas)))
if n != len(mesas):
    sys.exit("[XX] contagem no disco nao bate. CONFERIR A MAO.")
if lido.count("{") != lido.count("}"):
    sys.exit("[XX] chaves desbalanceadas: %d / %d" % (lido.count("{"), lido.count("}")))
print("   chaves %d / %d   ok" % (lido.count("{"), lido.count("}")))
print()
print("[ok] gerado. Precisa de arranque VIGIADO -- e .cs novo.")
