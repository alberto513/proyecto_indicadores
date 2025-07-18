extrair_ninja_docs.py
importar sistema operacional
importar docx
importar json
de coleções importar defaultdict

# Rota onde se encontram os documentos
docs_path = r"C:\Users\PC\Documents\DOCUMENTOS MAS ANTIGUOS\PDF CONVERSACION CLAUDE\DOCUMENTACION NINJA"
# A rota onde você guardará as informações extraídas
output_path = r"C:\Users\PC\Documents\DOCUMENTOS MAS ANTIGUOS\PDF CONVERSACION CLAUDE\DOCUMENTACION NINJA\Extracted"

# Crie uma pasta de saída se não existir
se não os.path.exists(output_path):
    os.makedirs(caminho_de_saída)

# Categorias para organizar os documentos
categorias = {
    "Indicadores": ["Indicadores", "Indicadores personalizados", "Modelo", "Classe base"],
    "Desenho": ["Desenhar", "Desenho", "Linha", "Gráficos"],
    "Série": ["Série", "ISérie", "Série de preço", "Série de valor"],
    "Desempenho": ["Desempenho", "Otimização", "Métricas"],
    "Métodos": ["Métodos", "Propriedades"],
    "Configuração": ["Parâmetro", "Configuração"],
    "Ciclo de vida": ["Ciclo de vida", "Navegação"],
    "Armazenamento": ["Armazenamento", "Valores históricos", "MaximumBarsLookBack"]
}

# Dicionário para armazenar o conteúdo por categoria
content_by_category = defaultdict(lista)
metadados = {}

# Função para determinar a categoria de um documento
def get_category(nome do arquivo):
    para categoria, palavras-chave em categories.items():
        para palavra-chave em palavras-chave:
            se palavra-chave.lower() em nomedoarquivo.lower():
                retornar categoria
    retornar "Geral"

# Processar cada documento
contagem_de_documentos = 0
print("Iniciando processamento de documentos...")

para nome de arquivo em os.listdir(docs_path):
    se nomedoarquivo.endswith(".docx") ou nomedoarquivo.endswith(".doc"):
        tentar:
            file_path = os.path.join(caminho_docs, nome_do_arquivo)
            print(f"Processando: {nomedoarquivo}")

            # Extrai o conteúdo do documento
            doc = docx.Document(caminho_do_arquivo)
            conteúdo_texto = []

            # Extrair texto de párrafos
            para parágrafos em doc.parágrafos:
                se para.text.strip():
                    text_content.append(para.texto)

            # Extrair texto de tabelas
            para tabela em doc.tables:
                para linha em table.rows:
                    texto_linha = []
                    para célula em row.cells:
                        se célula.texto.strip():
                            row_text.append(célula.texto.tira())
                    se texto_linha:
                        text_content.append(" | ".join(texto_da_linha))

            # Determinar a categoria
            categoria = get_category(nome do arquivo)

            # Salvar o conteúdo
            doc_info = {
                "nome do arquivo": nome do arquivo,
                "conteúdo": conteúdo_texto,
                "tamanho": os.path.getsize(caminho_do_arquivo)
            }

            content_by_category[categoria].append(doc_info)

            # Guardar o conteúdo em um arquivo de texto individual
            txt_filename = os.path.splitext(filename)[0] + ".txt"
            com open(os.path.join(output_path, txt_filename), "w", encoding="utf-8") como f:
                f.write(f"Documento: {nome do arquivo}\n")
                f.write(f"Categoria: {category}\n")
                f.write(f"Tamanho: {os.path.getsize(file_path)} bytes\n")
                f.escreva("-" * 80 + "\n\n")
                f.write("\n\n".join(text_content))

            contagem_de_documentos += 1

        exceto Exceção como e:
            print(f"Erro ao processar {filename}: {str(e)}")

# Guardar o índice de categorias
com open(os.path.join(output_path, "index.json"), "w", encoding="utf-8") como f:
    json.dump(conteúdo_por_categoria, f, recuo=2)

# Crie um arquivo de currículo
com open(os.path.join(output_path, "resumen.txt"), "w", encoding="utf-8") como f:
    f.write("RESUMEN DE DOCUMENTAÇÃO NINJATRADER\n")
    f.escrever("=" * 80 + "\n\n")

    para categoria, documentos em content_by_category.items():
        f.write(f"\n\nCATEGORIA: {category.upper()}\n")
        f.escrever("-" * 80 + "\n")

        para doc em docs:
            f.write(f"• {doc['nome do arquivo']} ({doc['tamanho']} bytes)\n")
            se doc['content'] e len(doc['content']) > 0:
                primeira_linha = doc['conteúdo'][0]
                pré-visualização = primeira_linha[:100] + "..." se len(primeira_linha) > 100 senão primeira_linha
                f.write(f" Início: {preview}\n\n")

print(f"Processamento concluído. {doc_count} documentos são processados.")
print(f"Os resultados serão salvos em: {output_path}")
    