# -*- coding: utf-8 -*-
"""
traduzir-para-ingles.py -- troca os textos VISIVEIS do mod para ingles e gera o CSV de
traducao com o portugues.

POR QUE INGLES NO CODIGO E O PORTUGUES NO CSV, e nao o contrario
    E o mecanismo do proprio Eco, conferido no servidor em 15/09/2026:
      - LocDisplayName / LocDescription sao os atributos localizaveis (1839 e 1906 usos)
      - Mods/Translations/*.csv tem a coluna "English" como CHAVE, e as demais linguas
        como valor (visto em Gates.csv e MarketMod.csv, os dois instalados aqui)
    Ou seja: o texto que fica no codigo E a chave de traducao. Em ingles, o mod funciona
    para todo mundo e o CSV acrescenta o portugues. Em portugues, ele so funcionaria aqui.

O QUE NAO MUDA
    Nome de classe, de propriedade e de metodo continuam como estao. Eles nao aparecem
    para o jogador, e renomear quebraria o ForaDeEstoqueMesas.cs e o gerador sem ganho.

RESSALVA HONESTA
    Que o CSV traduza texto de ABA e de Description de componente NAO foi testado -- os
    dois CSVs que existem aqui traduzem LocDisplayName/LocDescription de ITEM e OBJETO.
    Se nao pegar, o mod fica em ingles, que e o padrao do mod.io de qualquer forma.
"""
import io, os, re, sys

AQUI = os.path.dirname(os.path.abspath(__file__))
ALVO = os.path.join(AQUI, "Mods", "UserCode", "BBCBrasil", "ForaDeEstoque", "ForaDeEstoque.cs")
CSV = os.path.join(AQUI, "Mods", "Translations", "ForaDeEstoque.csv")

# (texto no codigo hoje)  ->  (ingles, portugues-BR)
TROCAS = [
    ('CreateComponentTabLoc("Fora de Estoque", true)',
     'CreateComponentTabLoc("Out of Stock", true)', "Out of Stock", "Fora de Estoque"),

    ('public string Title => "Fora de Estoque";',
     'public string Title => "Out of Stock";', None, None),   # mesma string, ja coberta

    ('"Quais lojas aparecem para escolher. Minhas: as suas. Da cidade: todas as do assentamento desta mesa."',
     '"Which stores show up in the list to choose from. Mine: yours. In town: every store in this table\'s settlement."',
     "Which stores show up in the list to choose from. Mine: yours. In town: every store in this table's settlement.",
     "Quais lojas aparecem para escolher. Minhas: as suas. Da cidade: todas as do assentamento desta mesa."),

    ('"A loja que esta sendo monitorada."',
     '"The store being watched."',
     "The store being watched.", "A loja que esta sendo monitorada."),

    ('"Passa para a proxima loja da lista."',
     '"Move to the next store in the list."',
     "Move to the next store in the list.", "Passa para a proxima loja da lista."),

    ('"Refaz a lista agora."',
     '"Rebuild the list now."',
     "Rebuild the list now.", "Refaz a lista agora."),

    ('"O que esta mesa fabrica, esta zerado na loja, e voce ainda nao mandou fabricar."',
     '"What this table crafts, is sold out at the store, and you have not queued yet."',
     "What this table crafts, is sold out at the store, and you have not queued yet.",
     "O que esta mesa fabrica, esta zerado na loja, e voce ainda nao mandou fabricar."),

    ('"(aperte Atualizar)"', '"(press Refresh)"', "(press Refresh)", "(aperte Atualizar)"),
    ('"nenhuma loja sua"', '"no store of yours"', "no store of yours", "nenhuma loja sua"),
    ('"nenhuma loja neste assentamento"', '"no store in this settlement"',
     "no store in this settlement", "nenhuma loja neste assentamento"),
    ('"Nada faltando aqui."', '"Nothing missing here."',
     "Nothing missing here.", "Nada faltando aqui."),
    ('"erro ao montar a lista (ver log)"', '"could not build the list (see log)"',
     "could not build the list (see log)", "erro ao montar a lista (ver log)"),
]

# os dois valores do enum viram ingles; o CSV devolve o portugues
ENUM = [("Minhas", "Mine", "Minhas"), ("DaCidade", "InTown", "Da cidade")]

texto = io.open(ALVO, encoding="utf-8").read()
original = texto
faltaram = []

for velho, novo, _en, _pt in TROCAS:
    if texto.count(velho) != 1:
        faltaram.append((velho[:60], texto.count(velho)))
        continue
    texto = texto.replace(velho, novo)

# enum: trocar so as declaracoes e os usos, nao palavras soltas
for velho, novo, _pt in ENUM:
    texto = re.sub(r"\bMostrarLojas\.%s\b" % velho, "MostrarLojas.%s" % novo, texto)
    texto = re.sub(r"^(\s*)%s,$" % velho, r"\1%s," % novo, texto, flags=re.M)

if faltaram:
    print("[XX] %d padrao(oes) nao casaram exatamente 1 vez -- NADA foi gravado:" % len(faltaram))
    for p, n in faltaram:
        print("   %dx  %s" % (n, p))
    sys.exit(1)

if "--seco" in sys.argv:
    print("=== MODO SECO ===")
    print("   %d trocas de texto + %d do enum" % (len(TROCAS), len(ENUM)))
    print("   o arquivo mudaria de %d para %d bytes" % (len(original), len(texto)))
    sys.exit(0)

tmp = ALVO + ".tmp"
io.open(tmp, "w", encoding="ascii", newline="\n").write(texto)
os.replace(tmp, ALVO)

# ---- o CSV, no mesmo formato dos que o servidor ja tem
COLUNAS = ["Context", "English", "Gibberish", "French", "Spanish", "German", "Korean",
           "BrazillianPortuguese", "SimplifedChinese", "Russian", "Italian", "Portuguese",
           "Hungarian", "Japanese", "Norwegian", "Polish", "Dutch", "Romanian", "Danish",
           "Czech", "Swedish", "Ukrainian", "Greek", "Arabic", "Turkish", "Finnish"]
I_EN, I_PT, I_PT2 = 1, 7, 11

linhas = ['Context,' + ','.join('"%s"' % c for c in COLUNAS[1:])]
pares = [(en, pt) for _v, _n, en, pt in TROCAS if en] + [(en, pt) for _v, en, pt in ENUM]
for en, pt in pares:
    cel = [""] * len(COLUNAS)
    cel[I_EN] = en
    cel[I_PT] = pt          # BrazillianPortuguese
    cel[I_PT2] = pt         # Portuguese
    linhas.append(",".join('"%s"' % c.replace('"', '""') for c in cel))

os.makedirs(os.path.dirname(CSV), exist_ok=True)
io.open(CSV, "w", encoding="utf-8-sig", newline="\r\n").write("\n".join(linhas) + "\n")

# ---- conferir RELENDO
lido = io.open(ALVO, encoding="ascii").read()
print("=== CONFERINDO RELENDO DO DISCO ===")
print("   %s  %d bytes" % (os.path.basename(ALVO), len(lido)))
sobrou = [en for _v, _n, en, pt in TROCAS if pt and pt in lido]
print("   textos em portugues que sobraram no codigo: %d" % len(sobrou))
for s in sobrou[:4]:
    print("      %s" % s[:70])
print("   chaves { } : %d / %d" % (lido.count("{"), lido.count("}")))
print("   %s  %d linhas" % (os.path.basename(CSV), len(linhas)))
if lido.count("{") != lido.count("}"):
    sys.exit("[XX] chaves desbalanceadas")
print("\n[ok] codigo em ingles, CSV com portugues. Precisa de arranque VIGIADO.")
