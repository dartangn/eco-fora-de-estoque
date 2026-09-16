# -*- coding: ascii -*-
"""
empacotar.py -- monta o .zip do mod no formato que o mod.io espera.

POR QUE NAO Compress-Archive DO POWERSHELL
    Ele grava os caminhos com BARRA INVERTIDA ("Mods\\UserCode\\..."). O padrao ZIP manda
    barra normal, e o unzip do Linux -- onde roda a maioria dos servidores de Eco -- nao
    trata "\\" como separador: em vez de criar as pastas, ele cria UM arquivo com barras
    invertidas no nome. O mod "instala" e nao carrega, sem erro nenhum.
    Conferido no zip gerado em 15/09/2026: "Mods\\UserCode\\BBCBrasil\\...".

    Este script usa zipfile do Python, que grava "/" -- e no fim REABRE o arquivo e confere.

O zip funde com a RAIZ do servidor: a primeira pasta dentro dele e "Mods".
"""
import os, sys, zipfile

AQUI = os.path.dirname(os.path.abspath(__file__))
RAIZ = os.path.join(AQUI, "pacote")

# NUMERO DA VERSAO -- mude AQUI, e use o MESMO no campo "Version" do mod.io.
#
# Decisao do Raul em 16/09/2026: depois da 2.0.0 vem a **2.0.1**, nao a 3. O primeiro numero
# so sobe quando houver mudanca que quebre servidor de quem ja usa (por exemplo, trocar o
# caminho das pastas, ou tirar algo que o jogador ja configurou). Correcao vai no ultimo
# numero; funcionalidade nova, no do meio.
#
# Ja publicadas: 1.0.0 (15/09) e 2.0.0 (16/09).
VERSAO = "2.0.0"
SAIDA = os.path.join(AQUI, "ForaDeEstoque-v%s-Eco14.1.zip" % VERSAO)

# so a arvore Mods/ entra no zip -- README e gerador ficam no GitHub, nao no pacote
DENTRO = os.path.join(RAIZ, "Mods")
if not os.path.isdir(DENTRO):
    sys.exit("[XX] nao achei %s" % DENTRO)

arquivos = []
for pasta, _, arqs in os.walk(DENTRO):
    for a in sorted(arqs):
        completo = os.path.join(pasta, a)
        # caminho DENTRO do zip, sempre com barra normal
        rel = os.path.relpath(completo, RAIZ).replace(os.sep, "/")
        arquivos.append((completo, rel))

if not arquivos:
    sys.exit("[XX] nada para empacotar")

with zipfile.ZipFile(SAIDA, "w", zipfile.ZIP_DEFLATED) as z:
    for completo, rel in arquivos:
        z.write(completo, rel)

# ---- conferir RELENDO o arquivo gerado
print("=== %s ===" % os.path.basename(SAIDA))
with zipfile.ZipFile(SAIDA) as z:
    nomes = z.namelist()
    for n in nomes:
        print("   %8d  %s" % (z.getinfo(n).file_size, n))
    ruins = [n for n in nomes if "\\" in n]
    if ruins:
        sys.exit("[XX] %d caminho(s) com barra invertida: %s" % (len(ruins), ruins[:2]))
    if not all(n.startswith("Mods/") for n in nomes):
        sys.exit("[XX] ha entrada fora de Mods/ -- o zip tem de fundir com a raiz do servidor")
    print("   todos com barra normal e sob Mods/  ok")
print("   %d bytes" % os.path.getsize(SAIDA))
