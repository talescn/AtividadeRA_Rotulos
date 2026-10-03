# Realidade Aumentada com rótulos de produtos

Aplicação de **Realidade Aumentada baseada em marcadores** feita com **Unity + Vuforia**. O rótulo de um produto de mercado funciona como marcador: ao apontar a câmera para a embalagem, a **tabela nutricional real** daquele produto aparece na frente dele.

> Atividade 01 da disciplina **Mundos Virtuais e Realidade Aumentada** (Grupo Anchieta), proposta pelo professor Clayton Valdo.

## Como funciona

1. O Vuforia inicia e o script [`GerenciadorRotulosRA`](Assets/Scripts/GerenciadorRotulosRA.cs) percorre a lista de produtos.
2. Para cada produto, a foto do rótulo vira um **Image Target criado em tempo de execução**, com a largura real da embalagem em metros.
3. Cada target recebe um quad com a foto da tabela nutricional do próprio produto, deitado sobre o plano do rótulo.
4. Quando o rótulo é reconhecido pela câmera, a tabela aparece. Quando o produto sai de vista, ela some.

Não há nenhum marcador fixo no projeto: trocar de produto é trocar duas fotos.

## Produtos de teste

| Produto | Marcador | Tabela nutricional |
|---|---|---|
| Sprite (garrafa 200 ml) | `Assets/Fotos/rotulo1.jpg` | `Assets/Fotos/tabela1.jpg` |
| Leite Ninho Integral (1 L) | `Assets/Fotos/rotulo2.jpg` | `Assets/Fotos/tabela2.jpg` |
| Creme de Cebola Maggi (68 g) | `Assets/Fotos/rotulo3.jpg` | `Assets/Fotos/tabela3.jpg` |

## Requisitos

- **Unity** 6000.6.0f1 (template Universal 3D, URP)
- **Vuforia Engine** 11.4.4 (`com.ptc.vuforia.engine`)
- Uma **license key** do Vuforia (a gratuita de desenvolvimento serve)
- Uma **webcam** para testar no Play Mode do Editor

## Como rodar

1. **Clone o repositório**
   ```bash
   git clone https://github.com/talescn/AtividadeRA_Rotulos.git
   ```
2. **Baixe o Vuforia Engine 11.4.4** (arquivo `.tgz`) no [Vuforia Developer Portal](https://developer.vuforia.com/). O arquivo tem cerca de 138 MB, acima do limite do GitHub, por isso não está no repositório.
3. **Ajuste o caminho do pacote** em `Packages/manifest.json`, apontando a dependência `com.ptc.vuforia.engine` para onde você salvou o `.tgz`.
4. **Abra o projeto** no Unity Hub.
5. **Cole sua license key** em *Window > Vuforia Configuration*. A configuração com a chave fica fora do Git (veja o `.gitignore`).
6. **Abra a cena** `Assets/Scenes/AtividadeRA_Rotulos.unity`, ligue a webcam e aperte **Play**.
7. Mostre uma embalagem (ou a foto do rótulo na tela) para a câmera.

## Dicas para um bom reconhecimento

- **Mantenha o rótulo de frente** para a câmera e bem iluminado, sem reflexo.
- **Distância**: para o Sprite, de 15 a 20 cm funciona melhor. Ninho e Maggi são reconhecidos mais longe.
- **Embalagens com bastante detalhe e contraste** são reconhecidas muito mais rápido. Foto do marcador de boa qualidade importa mais do que qualquer ajuste de código.
- **Garrafas curvas e transparentes** (como a do Sprite) são mais difíceis por causa do reflexo e da curvatura.

## Estrutura do projeto

```
Assets/
├── Fotos/                       fotos recortadas dos rótulos e das tabelas
├── Scenes/
│   └── AtividadeRA_Rotulos.unity   cena principal
├── Scripts/
│   └── GerenciadorRotulosRA.cs  cria os targets e exibe as tabelas
└── Editor/
    └── MontarCenaAtividade.cs   menu que monta a cena automaticamente
```

### Configuração por produto

No componente `GerenciadorRotulosRA`, cada item da lista `produtos` tem:

| Campo | Para que serve |
|---|---|
| `nome` | identifica o produto nos objetos criados e nos logs |
| `rotulo` | foto do rótulo usada como marcador (precisa de *Read/Write* habilitado) |
| `tabelaNutricional` | foto da tabela exibida sobre o produto |
| `larguraRotuloMetros` | largura **real** do rótulo, em metros (ex.: `0.10` = 10 cm) |
| `larguraTabelaMetros` | largura da tabela exibida, em metros |

O campo `distanciaFrente` define o quanto a tabela fica à frente da superfície do rótulo.

## Como trocar ou adicionar produtos

1. Coloque as novas fotos em `Assets/Fotos` (`rotuloN.jpg` e `tabelaN.jpg`).
2. Ajuste nomes e larguras em `Assets/Editor/MontarCenaAtividade.cs`.
3. Apague a cena atual e use o menu **Atividade RA > Montar cena dos rotulos**. Ele configura a importação das fotos, cria a cena com o `GerenciadorRotulosRA` preenchido e a adiciona ao Build Settings.

## Decisões técnicas

- **Image Targets em tempo de execução** (`ObserverFactory.CreateImageTarget`), sem depender de banco de dados criado no portal do Vuforia.
- **`maxSimultaneousImageTargets: 3`**. Com apenas 1 e *extended tracking*, um produto "prendia" o slot e impedia o reconhecimento do próximo.
- **Filtro `Tracked`** no `DefaultObserverEventHandler`: a tabela só aparece enquanto o rótulo está realmente visível, evitando tabela "fantasma" na tela depois que o produto sai do campo de visão.

## Limitações conhecidas

- As tabelas são **imagens fixas** ligadas a cada rótulo. Se o fabricante alterar o rótulo ou os valores, é preciso trocar as fotos.
- Testado apenas no **Play Mode do Editor com webcam**, não em dispositivo móvel.
- Reconhecimento menos estável em superfícies curvas e com reflexo.

## Ideias de evolução

- Ler o texto do rótulo com **OCR** e extrair a tabela nutricional e os ingredientes automaticamente.
- Consultar uma base aberta de alimentos (por exemplo, o [Open Food Facts](https://world.openfoodfacts.org/)) por código de barras, em vez de usar imagens fixas.
- Destacar **alérgenos e substâncias** que cada pessoa quer evitar.
- Escalar para um **catálogo maior** de produtos, por exemplo com o Cloud Recognition do Vuforia.
- Gerar um build para **Android/iOS**.

## Créditos

Atividade proposta pelo professor **Clayton Valdo**. Desenvolvido por **Tales Noronha** (Grupo Anchieta).
