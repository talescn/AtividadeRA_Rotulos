# Realidade Aumentada com rótulos de produtos

Aplicação de **Realidade Aumentada baseada em marcadores** feita com **Unity + Vuforia**. O rótulo de um produto de mercado funciona como marcador: ao apontar a câmera para a embalagem, a **tabela nutricional real** daquele produto aparece na frente dele, desenhada a partir dos dados do produto.

## Como funciona

1. O Vuforia inicia e o script [`GerenciadorRotulosRA`](Assets/Scripts/GerenciadorRotulosRA.cs) percorre a lista de produtos.
2. Para cada produto, a foto do rótulo vira um **Image Target criado em tempo de execução**, com a largura real da embalagem em metros.
3. Os dados do produto são buscados pelo **GTIN** (o número do código de barras), e a tabela nutricional é **desenhada com TextMeshPro**, deitada sobre o plano do rótulo. Sem dados, aparece a foto da tabela.
4. Quando o rótulo é reconhecido pela câmera, a tabela aparece. Quando o produto sai de vista, ela some.
5. A interface ([`InterfaceRotulosRA`](Assets/Scripts/InterfaceRotulosRA.cs)) deixa ampliar a tabela em RA e abrir o **modo leitura**, com a tabela em tela cheia.

Não há nenhum marcador fixo no projeto: trocar de produto é trocar a foto do rótulo e o JSON com os dados.

## Zoom e modo leitura

| Ação | No celular | No Editor (webcam) |
|---|---|---|
| Ampliar ou reduzir a tabela em RA (0,5x a 3x) | controle deslizante ou pinça | controle deslizante ou roda do mouse |
| Abrir o modo leitura | tocar na tabela ou no botão **Ler** | clicar na tabela ou no botão **Ler** |
| Zoom no modo leitura | pinça | roda do mouse |
| Mover a tabela no modo leitura | arrastar com um dedo | arrastar com o botão esquerdo |
| Voltar para a câmera | botão **Fechar** | botão **Fechar** |

O botão **Ler** só aparece quando há um produto na câmera e abre o último que apareceu. O modo leitura continua aberto mesmo que o produto saia da câmera.

## Produtos de teste

| Produto | GTIN | Marcador | Tabela |
|---|---|---|---|
| Sprite (garrafa 200 ml) | 78939745 | `Assets/Fotos/rotulo1.jpg` | dados de `Resources/Produtos/78939745.json` |
| Leite Ninho Integral Forti+ (1 L) | 7898215157403 | `Assets/Fotos/rotulo2.jpg` | dados de `Resources/Produtos/7898215157403.json` |
| Creme de Cebola Maggi (68 g) | a conferir na embalagem | `Assets/Fotos/rotulo3.jpg` | foto `Assets/Fotos/tabela3.jpg` (até o GTIN ser conferido) |

## Dados dos produtos

Cada produto tem um JSON em `Assets/Resources/Produtos/{gtin}.json`, transcrito da tabela do rótulo. Valor ausente ou ilegível fica `null`; nada é inventado.

| Campo | Conteúdo |
|---|---|
| `gtin`, `nome`, `marca` | identificação do produto |
| `conferidoEm`, `fonte` | data em que os valores foram conferidos (AAAA-MM-DD) e de onde vieram (`rotulo` ou `openfoodfacts`) |
| `porcao`, `porcoesPorEmbalagem` | medida caseira, quantidade e unidade (`g` ou `ml`) |
| `base100`, `observacaoBase100` | a que a coluna "por 100" se refere (no Maggi, ao alimento pronto) |
| `nutrientes` | `chave`, `nome`, `unidade`, valor por 100, valor por porção, `%VD` e nível de recuo |
| `ingredientes`, `alergenos` | lista de ingredientes e a frase "ALÉRGICOS: ..." (nulos por enquanto: as fotos não mostram) |

O [`RepositorioProdutos`](Assets/Scripts/RepositorioProdutos.cs) busca os dados nesta ordem:

1. o **JSON local** (em `Resources`, para funcionar igual no Android);
2. o **cache** do aparelho, em `Application.persistentDataPath/Produtos`;
3. o **[Open Food Facts](https://world.openfoodfacts.org/)**: `GET /api/v2/product/{gtin}.json` só com os campos usados, User-Agent `AtividadeRA_Rotulos/versão (https://github.com/talescn/AtividadeRA_Rotulos)` e no máximo 15 consultas por minuto. O resultado vai para o cache.

Sem internet, ou se a base não tiver o produto, nada quebra: o app fica com o dado local ou com a foto da tabela.

## Requisitos

- **Unity** 6000.6.0f1 (template Universal 3D, URP)
- **Vuforia Engine** 11.4.4 (`com.ptc.vuforia.engine`)
- Uma **license key** do Vuforia (a gratuita de desenvolvimento serve)
- Uma **webcam** para testar no Play Mode do Editor
- Para o celular: o módulo **Android Build Support** do Unity e um aparelho com **Android 10** ou mais novo

## Como rodar

1. **Clone o repositório**
   ```bash
   git clone https://github.com/talescn/AtividadeRA_Rotulos.git
   ```
2. **Baixe o Vuforia Engine 11.4.4** (arquivo `com.ptc.vuforia.engine-11.4.4.tgz`) no [Vuforia Developer Portal](https://developer.vuforia.com/) e **coloque-o na pasta `Packages/`** do projeto. O `Packages/manifest.json` já aponta para ele com caminho relativo (`file:com.ptc.vuforia.engine-11.4.4.tgz`). O arquivo tem cerca de 138 MB, acima do limite do GitHub, por isso fica fora do repositório (o `.gitignore` ignora `Packages/*.tgz`).
3. **Abra o projeto** no Unity Hub.
4. **Cole sua license key** em *Window > Vuforia Configuration*. A configuração com a chave fica fora do Git (veja o `.gitignore`).
5. **Abra a cena** `Assets/Scenes/AtividadeRA_Rotulos.unity`, ligue a webcam e aperte **Play**.
6. Mostre uma embalagem (ou a foto do rótulo na tela) para a câmera.

## Gerar o APK (Android)

Requer o módulo **Android Build Support**, com **OpenJDK** e **Android SDK & NDK Tools**, instalado no Unity 6000.6.0f1 pelo Unity Hub.

1. Use o menu **Atividade RA > Gerar build Android**. Ele aplica as configurações abaixo, muda a plataforma para Android e gera `Builds/AtividadeRA_Rotulos.apk` (a pasta `Builds/` fica fora do Git).
2. Instale num celular com **Android 10 ou mais novo**. Com a depuração USB ligada:
   ```bash
   adb install -r Builds/AtividadeRA_Rotulos.apk
   ```
   Ou copie o APK para o celular e abra o arquivo (é preciso permitir a instalação de apps desconhecidos).
3. Na primeira abertura, permita o uso da câmera.

| Configuração | Valor | Motivo |
|---|---|---|
| Pacote | `com.talescn.rotulosra` | identificador do app (o template usava o da Unity) |
| Scripting backend e arquitetura | IL2CPP, ARM64 | o Vuforia não suporta mais armv7 desde a versão 11.2 |
| Orientação | retrato | |
| API mínima | 29 (Android 10) | mínimo da [lista de versões suportadas](https://developer.vuforia.com/library/vuforia-engine/platform-support/supported-versions/) do Vuforia |
| API gráfica | só OpenGL ES 3 | o Vulkan é experimental no Vuforia para Unity |
| Ponto de entrada | Activity | ponto de entrada clássico do Android |

Pela linha de comando, com o Unity fechado:

```bash
Unity.exe -batchmode -quit -projectPath . -buildTarget Android -executeMethod GerarBuildAndroid.GerarBuildBatch
```

## Dicas para um bom reconhecimento

- **Mantenha o rótulo de frente** para a câmera e bem iluminado, sem reflexo.
- **Distância**: para o Sprite, de 15 a 20 cm funciona melhor. Ninho e Maggi são reconhecidos mais longe.
- **Embalagens com bastante detalhe e contraste** são reconhecidas muito mais rápido. Foto do marcador de boa qualidade importa mais do que qualquer ajuste de código.
- **Garrafas curvas e transparentes** (como a do Sprite) são mais difíceis por causa do reflexo e da curvatura.

## Estrutura do projeto

```
Assets/
├── Fotos/                       fotos recortadas dos rótulos e das tabelas
├── Resources/Produtos/          um JSON por produto, com o GTIN no nome do arquivo
├── Scenes/
│   └── AtividadeRA_Rotulos.unity   cena principal
├── Scripts/
│   ├── GerenciadorRotulosRA.cs  cria os targets, exibe as tabelas e aplica o zoom
│   ├── InterfaceRotulosRA.cs    zoom, pinça, botão Ler e modo leitura
│   ├── TabelaNutricionalUI.cs   desenha a tabela a partir dos dados (TextMeshPro)
│   ├── DadosProduto.cs          modelo dos dados de um produto
│   ├── RepositorioProdutos.cs   busca os dados: JSON local, cache e Open Food Facts
│   ├── LimiteRequisicoes.cs     limite de consultas por minuto
│   ├── CalculosGestos.cs        contas do zoom, da pinça e do arrasto
│   └── AjustarAreaSegura.cs     mantém os controles fora do entalhe e das barras do sistema
├── Editor/
│   ├── MontarCenaAtividade.cs   menu que monta a cena automaticamente
│   ├── MontarInterfaceRA.cs     cria a interface (barra de zoom, botão Ler, modo leitura)
│   ├── GerarBuildAndroid.cs     menu que configura o Android e gera o APK
│   ├── GerarPreviaTabelas.cs    menu que salva um PNG de cada tabela em Builds/Previas
│   └── Testes/                  testes de EditMode
├── TextMesh Pro/                recursos essenciais do TextMeshPro (fonte e configurações)
└── link.xml                     evita que o IL2CPP remova as classes lidas do JSON
```

## Testes

Os testes de EditMode ficam em `Assets/Editor/Testes`: contas do zoom e da pinça, a estrutura da cena montada pelo menu, as configurações de Android, os JSONs dos produtos (campos, dígito do GTIN e valor energético contra 4 × carboidratos + 4 × proteínas + 9 × gorduras, com tolerância de 15%), o repositório e a tabela desenhada. Nenhum teste usa a rede. Rode por *Window > General > Test Runner > EditMode* ou pela linha de comando, com o Unity fechado:

```bash
Unity.exe -batchmode -projectPath . -runTests -testPlatform EditMode -testResults resultados.xml
```

### Configuração por produto

No componente `GerenciadorRotulosRA`, cada item da lista `produtos` tem:

| Campo | Para que serve |
|---|---|
| `nome` | identifica o produto nos objetos criados e nos logs |
| `gtin` | código de barras; com ele o app acha o JSON, o cache ou o produto no Open Food Facts |
| `rotulo` | foto do rótulo usada como marcador (precisa de *Read/Write* habilitado) |
| `tabelaNutricional` | foto da tabela, usada só quando o produto não tem dados |
| `larguraRotuloMetros` | largura **real** do rótulo, em metros (ex.: `0.10` = 10 cm) |
| `larguraTabelaMetros` | largura da tabela exibida, em metros |

O campo `distanciaFrente` define o quanto a tabela fica à frente da superfície do rótulo.

## Como trocar ou adicionar produtos

1. Coloque as novas fotos em `Assets/Fotos` (`rotuloN.jpg` e, como reserva, `tabelaN.jpg`).
2. Crie `Assets/Resources/Produtos/{gtin}.json` transcrevendo a tabela do rótulo (veja os dois JSONs existentes). Os testes conferem os campos e o valor energético.
3. Ajuste nomes, GTINs e larguras em `Assets/Editor/MontarCenaAtividade.cs`.
4. Apague a cena atual e use o menu **Atividade RA > Montar cena dos rotulos**. Ele configura a importação das fotos, cria a cena com o `GerenciadorRotulosRA` preenchido e a interface de zoom e leitura, e a adiciona ao Build Settings.
5. Para conferir o desenho da tabela sem câmera, use **Atividade RA > Gerar previa das tabelas**.

## Decisões técnicas

- **Image Targets em tempo de execução** (`ObserverFactory.CreateImageTarget`), sem depender de banco de dados criado no portal do Vuforia.
- **`maxSimultaneousImageTargets: 3`**. Com apenas 1 e *extended tracking*, um produto "prendia" o slot e impedia o reconhecimento do próximo.
- **Filtro `Tracked`** no `DefaultObserverEventHandler`: a tabela só aparece enquanto o rótulo está realmente visível, evitando tabela "fantasma" na tela depois que o produto sai do campo de visão.
- **Só o Input System novo**: a pinça usa o EnhancedTouch, o Editor usa o mouse, e o `EventSystem` usa o `InputSystemUIInputModule`.
- **Toque na tabela por raycast**: a tabela mantém um collider, que o `DefaultObserverEventHandler` liga e desliga junto com ela.
- **Tabela em um Canvas no espaço**, sobre o rótulo, com o mesmo desenho usado no modo leitura. O texto em TextMeshPro fica nítido em qualquer zoom.
- **JSON local antes do cache e do Open Food Facts**: a transcrição do rótulo é a referência; a base aberta só completa o que falta.
- **Newtonsoft Json** para ler campos nulos (o `JsonUtility` não aceita número nulo) e a resposta do Open Food Facts.

## Limitações conhecidas

- Os dados de cada produto são **transcritos à mão** do rótulo. Se o fabricante mudar a receita ou a tabela, é preciso atualizar o JSON.
- O app só **reconhece os produtos cadastrados** (a foto do rótulo vira o marcador). Produtos novos dependem de leitura de código de barras ou de reconhecimento em nuvem.
- A cobertura do Open Food Facts para produtos brasileiros é **desigual**: em outubro de 2026, o Ninho tinha a tabela igual à do rótulo, o Sprite 200 ml não tinha nenhum nutriente e o Maggi 68 g tinha valores inconsistentes.
- Reconhecimento menos estável em superfícies curvas e com reflexo.

## Ideias de evolução

- Ler o **código de barras** para identificar qualquer produto e buscar os dados pelo GTIN.
- Ler o texto do rótulo com **OCR** e extrair a tabela nutricional e os ingredientes automaticamente.
- Destacar **alérgenos e substâncias** que cada pessoa quer evitar, e ler a tabela em voz alta.
- Escalar para um **catálogo maior** de produtos, por exemplo com o Cloud Recognition do Vuforia.
- Gerar o build para **iOS**.

