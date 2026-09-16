#!/usr/bin/env python3
# -*- coding: ascii -*-
"""
instalar.py -- confere a instalacao do Out of Stock e gera a lista de mesas de MOD.
Roda em Linux e em Windows, com Python 3 e nada mais (sem pip, sem dependencia).

    python3 instalar.py                  # acha a raiz sozinho, confere e gera
    python3 instalar.py --seco           # so diz o que faria, nao grava nada
    python3 instalar.py --raiz CAMINHO   # raiz explicita, quando a deteccao falha
    python3 instalar.py --so-mesas       # so refaz a lista de mesas de mod

POR QUE ELE DESCOBRE A RAIZ EM VEZ DE PERGUNTAR
    Caminho digitado a mao e onde o erro mora: "C:\\Eco" e "C:\\Eco\\Server" parecem a
    mesma coisa e so uma das duas funciona. O sinal que NAO mente e a pasta Mods/__core__,
    que so existe na raiz de um servidor Eco de verdade -- e o proprio jogo que a cria.
    Ordem de tentativa: --raiz -> variavel ECO_SERVER -> subindo a partir da pasta atual
    -> subindo a partir deste script -> o processo do EcoServer que estiver rodando.

O QUE ELE NAO FAZ, DE PROPOSITO
    Nao reinicia o servidor. Reiniciar derruba quem esta jogando, e essa decisao e de
    quem administra -- alem de que erro de compilacao aqui nao degrada nada: o servidor
    simplesmente NAO SOBE. Por isso ele termina dizendo o que vigiar no log.
"""
import os, re, subprocess, sys

SECO = "--seco" in sys.argv
SO_MESAS = "--so-mesas" in sys.argv

# a assinatura de uma raiz de servidor Eco: __core__ e o codigo do JOGO, so o jogo o cria
MARCA = os.path.join("Mods", "__core__")
ATRIBUTO = r"RequireComponent\(typeof\(CraftingComponent\)\)"

NOSSOS = [
    os.path.join("Mods", "UserCode", "BBCBrasil", "ForaDeEstoque", "ForaDeEstoque.cs"),
    os.path.join("Mods", "UserCode", "BBCBrasil", "ForaDeEstoque", "ForaDeEstoqueMesas.cs"),
]
OPCIONAL = os.path.join("Mods", "Translations", "ForaDeEstoque.csv")
DESTINO_MESAS = os.path.join("Mods", "UserCode", "BBCBrasil", "ForaDeEstoque",
                             "ForaDeEstoqueMesasDeMod.cs")


# ---------------------------------------------------------------- achar a raiz
def eh_raiz(caminho):
    """Raiz de verdade tem Mods/__core__ com codigo do jogo dentro.

    Conferir so se a PASTA existe nao basta: pasta vazia com o nome certo passaria, e o
    script diria 'instalado' sobre um lugar que nao serve. Entao conta arquivo .cs.
    """
    core = os.path.join(caminho, MARCA)
    if not os.path.isdir(core):
        return False
    for _raiz, _dirs, arqs in os.walk(core):
        for a in arqs:
            if a.endswith(".cs"):
                return True
    return False


def subindo(partida):
    """Sobe de pasta em pasta procurando a marca. Para na raiz do sistema de arquivos."""
    atual = os.path.abspath(partida)
    while True:
        if eh_raiz(atual):
            return atual
        pai = os.path.dirname(atual)
        if pai == atual:          # chegou em / ou em C:\ -- nao ha para onde subir
            return None
        atual = pai


def pelo_processo():
    """A raiz pela posicao do EcoServer que esta RODANDO. Ultimo recurso, e o mais certeiro
    quando existe: e literalmente o servidor que vai carregar o mod."""
    try:
        if sys.platform.startswith("win"):
            saida = subprocess.run(
                ["powershell", "-NoProfile", "-Command",
                 "Get-Process EcoServer -ErrorAction SilentlyContinue | "
                 "Select-Object -First 1 -ExpandProperty Path"],
                capture_output=True, text=True, timeout=25).stdout.strip()
            if saida:
                return subindo(os.path.dirname(saida))
        else:
            for pid in os.listdir("/proc"):
                if not pid.isdigit():
                    continue
                try:
                    exe = os.readlink("/proc/%s/exe" % pid)
                except OSError:
                    continue          # processo de outro usuario, ou ja morreu
                if os.path.basename(exe) == "EcoServer":
                    achado = subindo(os.path.dirname(exe))
                    if achado:
                        return achado
    except Exception:
        return None
    return None


def achar_raiz():
    if "--raiz" in sys.argv:
        r = os.path.abspath(sys.argv[sys.argv.index("--raiz") + 1])
        if not eh_raiz(r):
            sys.exit("[XX] '%s' nao tem %s -- nao e raiz de servidor Eco.\n"
                     "     A raiz e a pasta que CONTEM Mods/, Configs/ e o EcoServer." % (r, MARCA))
        return r, "--raiz"
    env = os.environ.get("ECO_SERVER")
    if env and eh_raiz(env):
        return os.path.abspath(env), "variavel ECO_SERVER"
    r = subindo(os.getcwd())
    if r:
        return r, "subindo da pasta atual"
    r = subindo(os.path.dirname(os.path.abspath(__file__)))
    if r:
        return r, "subindo da pasta deste script"
    r = pelo_processo()
    if r:
        return r, "pelo processo do EcoServer em execucao"
    sys.exit("[XX] nao achei a raiz do servidor.\n"
             "     Rode de dentro da pasta do servidor, ou passe --raiz CAMINHO.\n"
             "     A raiz e a pasta que contem Mods%s__core__." % os.sep)


# ---------------------------------------------------- mesas de fabricacao de mod
def arquivos_com_atributo(raiz):
    """Todo .cs sob `raiz` que declara o atributo.

    Feito em Python puro, sem grep: grep nao existe no Windows. E os caminhos vem da
    propria caminhada, entao pasta com ESPACO no nome ("Mixology 14.0.3") nao quebra --
    foi exatamente esse defeito que, numa versao anterior deste codigo, achou 1 mesa em
    vez de 3 e parecia resposta legitima.
    """
    achados = []
    for pasta, _dirs, arqs in os.walk(raiz):
        for a in arqs:
            if not a.endswith(".cs"):
                continue
            caminho = os.path.join(pasta, a)
            try:
                texto = open(caminho, encoding="utf-8-sig", errors="replace").read()
            except Exception:
                continue
            if re.search(ATRIBUTO, texto):
                achados.append((caminho, texto))
    return achados


def mesas(raiz_busca, so_nomes=False):
    """(nome, namespace, partial?, arquivo) de cada mesa declarada sob raiz_busca.

    ARMADILHA QUE CUSTOU UM CICLO: entre o atributo e a classe cabem ate 12 outros
    atributos (a CarpentryTable tem 10). Procurar so 6 linhas adiante devolve 4 mesas em
    vez de 69 -- parece resposta e e falso negativo. Por isso a janela e larga.
    """
    saida = []
    for caminho, texto in arquivos_com_atributo(raiz_busca):
        for m in re.finditer(ATRIBUTO, texto):
            c = re.search(r"public\s+(partial\s+)?class\s+([A-Za-z0-9_]+Object)\b",
                          texto[m.end(): m.end() + 2000])
            if not c:
                continue
            if so_nomes:
                saida.append(c.group(2))
                continue
            # o namespace que vale e o ULTIMO declarado antes da classe, nao o primeiro
            # do arquivo: arquivo com dois namespaces existe, e pegar o primeiro poria a
            # classe no lugar errado -- CS0246 no arranque.
            antes = texto[: m.end() + c.end()]
            ns = re.findall(r"^\s*namespace\s+([A-Za-z0-9_.]+)", antes, re.M)
            saida.append((c.group(2), ns[-1] if ns else None, bool(c.group(1)),
                          os.path.relpath(caminho, raiz_busca)))
    return sorted(set(saida))


# ------------------------------------------------------------------------ corpo
raiz, como = achar_raiz()
print("=== raiz do servidor ===")
print("   %s" % raiz)
print("   achada: %s" % como)
exe = [n for n in ("EcoServer", "EcoServer.exe") if os.path.isfile(os.path.join(raiz, n))]
print("   executavel: %s" % (", ".join(exe) if exe else
      "nao vi EcoServer aqui (nao e impedimento, mas confira que e a raiz certa)"))
print("   sistema: %s" % ("Windows" if sys.platform.startswith("win") else sys.platform))

if not SO_MESAS:
    print()
    print("=== arquivos do mod ===")
    faltando = []
    for rel in NOSSOS:
        p = os.path.join(raiz, rel)
        if os.path.isfile(p):
            print("   [ok] %-70s %d bytes" % (rel, os.path.getsize(p)))
        else:
            print("   [XX] %-70s AUSENTE" % rel)
            faltando.append(rel)
    p = os.path.join(raiz, OPCIONAL)
    print("   [%s] %-70s %s" % ("ok" if os.path.isfile(p) else "--", OPCIONAL,
          ("%d bytes" % os.path.getsize(p)) if os.path.isfile(p) else "ausente (opcional: e a traducao)"))
    if faltando:
        print()
        print("   O zip nao foi extraido na raiz. Ele TEM de fundir com Mods/ --")
        print("   a primeira pasta dentro do zip se chama 'Mods' de proposito.")
        print("   Extraia em: %s" % raiz)
        sys.exit(1)

print()
print("=== mesas de fabricacao vindas de MOD ===")
user = os.path.join(raiz, "Mods", "UserCode")
core_nomes = set(mesas(os.path.join(raiz, MARCA), so_nomes=True))
boas, recusadas = [], []
for nome, ns, eh_partial, arq in mesas(user):
    if nome in core_nomes:
        continue                       # ja coberta pelo arquivo do jogo base
    if not eh_partial:
        recusadas.append((nome, arq, "nao e `partial` -- declarar daria CS0260"))
    elif not ns:
        recusadas.append((nome, arq, "nao achei o namespace"))
    else:
        boas.append((nome, ns, arq))

print("   mesas do jogo base ja cobertas: %d" % len(core_nomes))
for nome, ns, arq in boas:
    print("   [ok] %-32s ns=%-22s %s" % (nome, ns, arq))
for nome, arq, motivo in recusadas:
    print("   [--] %-32s %s  (%s)" % (nome, arq, motivo))

alvo = os.path.join(raiz, DESTINO_MESAS)
if not boas:
    print("   nenhuma. Nada a gerar.")
    if os.path.isfile(alvo):
        print()
        print("   [!!] mas %s EXISTE." % DESTINO_MESAS)
        print("        Se voce removeu o mod daquelas mesas, APAGUE esse arquivo antes de")
        print("        reiniciar: tipo de mod ausente da CS0246 e o servidor nao sobe.")
    sys.exit(0)

por_ns = {}
for nome, ns, _a in boas:
    por_ns.setdefault(ns, []).append(nome)

linhas = [
    "// GERADO por instalar.py NESTE servidor -- NAO EDITAR A MAO,",
    "// e NAO COPIAR para outro servidor: a lista vale so para os mods instalados aqui.",
    "//",
    "// Pendura a aba \"Out of Stock\" em %d mesa(s) vinda(s) de mod." % len(boas),
    "//",
    "// SE VOCE REMOVER UM DESSES MODS, APAGUE A LINHA DELE ANTES DE REINICIAR.",
    "// Tipo de mod ausente da CS0246, e o servidor inteiro nao sobe.",
    "//",
    "// Ordem de instalacao nao importa: o Eco compila todo o Mods/UserCode numa unica",
    "// passada a cada arranque. O que importa e o mod da mesa estar PRESENTE no reinicio.",
    "",
]
for ns in sorted(por_ns):
    linhas += ["namespace %s" % ns, "{", "    using Eco.Gameplay.Objects;", ""]
    for nome in sorted(por_ns[ns]):
        linhas.append("    [RequireComponent(typeof(ForaDeEstoqueComponent))] "
                      "public partial class %s { }" % nome)
    linhas += ["}", ""]
texto = "\n".join(linhas)

if SECO:
    print()
    print("=== MODO SECO -- nada gravado. Sairia em %s:" % DESTINO_MESAS)
    print(texto)
    sys.exit(0)

os.makedirs(os.path.dirname(alvo), exist_ok=True)
tmp = alvo + ".tmp"
with open(tmp, "w", encoding="ascii", newline="\n") as fh:
    fh.write(texto)
os.replace(tmp, alvo)

# conferir RELENDO do disco, nunca a variavel que acabei de escrever
lido = open(alvo, encoding="ascii").read()
print()
print("=== conferindo relendo do disco ===")
print("   %s" % alvo)
print("   %d bytes, %d mesa(s), chaves %d/%d"
      % (len(lido), lido.count("RequireComponent"), lido.count("{"), lido.count("}")))
if lido.count("{") != lido.count("}") or lido.count("RequireComponent") != len(boas):
    sys.exit("[XX] arquivo saiu errado -- NAO reinicie com ele")

print()
print("[ok] pronto. Agora REINICIE o servidor e VIGIE o arranque:")
print("     o erro aqui nao degrada nada -- o servidor simplesmente NAO SOBE.")
print("     Procure no log, so do arranque novo:  'error CS'  e  'Failed to start'.")
print("     A compilacao termina em ~20s ('Loading mods ... Finished'); ja o")
print("     'Failed to start' pode aparecer so aos ~160s. Espere os dois.")
