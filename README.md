# Realidade Aumentada com rótulos de produtos

Aplicação de **Realidade Aumentada baseada em marcadores** feita com **Unity + Vuforia**. O rótulo de um produto de mercado funciona como marcador: ao apontar a câmera para a embalagem, a **tabela nutricional real** daquele produto aparece na frente dele, desenhada a partir dos dados do produto.

## Como funciona

1. O Vuforia inicia e o script [`GerenciadorRotulosRA`](Assets/Scripts/GerenciadorRotulosRA.cs) percorre a lista de produtos.
2. Para cada produto, a foto do rótulo vira um **Image Target criado em tempo de execução**, com a largura real da embalagem em metros.
3. Os dados do produto são buscados pelo **GTIN** (o número do código de barras), e a tabela nutricional é **desenhada com TextMeshPro**, deitada sobre o plano do rótulo. Sem dados, aparece a foto da tabela.
4. Quando o rótulo é reconhecido pela câmera, a tabela aparece. Quando o produto sai de vista, ela some.
5. A interface ([`InterfaceRotulosRA`](Assets/Scripts/InterfaceRotulosRA.cs)) deixa ampliar a tabela em RA e abrir o **modo leitura**, com a tabela em tela cheia.
6. Sem nenhum rótulo na câmera, o app procura um **código de barras**. Com o GTIN lido, ele busca os dados e abre a tabela no modo leitura, inclusive de produtos que não têm rótulo cadastrado.
7. Com a tabela aberta, o botão **Ensinar rótulo** transforma o rótulo daquele produto num marcador novo, criado no próprio celular. Daí em diante a tabela aparece em RA sobre ele, como nos produtos da cena.

Não há nenhum marcador fixo no projeto: trocar de produto é trocar a foto do rótulo e o JSON com os dados, ou ensinar o rótulo no app.

## Zoom e modo leitura

| Ação | No celular | No Editor (webcam) |
|---|---|---|
| Ampliar ou reduzir a tabela em RA (0,5x a 3x) | controle deslizante ou pinça | controle deslizante ou roda do mouse |
| Abrir o modo leitura | tocar na tabela ou no botão **Ler** | clicar na tabela ou no botão **Ler** |
| Zoom no modo leitura | pinça | roda do mouse |
| Mover a tabela no modo leitura | arrastar com um dedo | arrastar com o botão esquerdo |
| Voltar para a câmera | botão **Fechar** | botão **Fechar** |

O botão **Ler** só aparece quando há um produto na câmera e abre o último que apareceu. O modo leitura continua aberto mesmo que o produto saia da câmera.

## Código de barras

Quando nenhum rótulo está na câmera, a faixa do topo mostra "Aponte para o rótulo ou para o código de barras" e o [`LeitorCodigoBarras`](Assets/Scripts/LeitorCodigoBarras.cs) procura códigos **EAN-13, EAN-8 e UPC-A** na imagem da câmera do Vuforia, várias vezes por segundo e fora da thread principal.

- A leitura usa o [ZXing.Net](https://github.com/micjahn/ZXing.Net/) 0.16.11 (Apache-2.0), em `Assets/Plugins/ZXing`. O leitor do próprio Vuforia exige o plano Premium para publicar.
- Com o celular em retrato, o código na horizontal da tela chega deitado na imagem da câmera: o decodificador tenta essa orientação primeiro e não perde tempo com as quatro rotações do `AutoRotate` (cerca de 2× mais rápido nos quadros sem código).
- Um código só vale depois de duas leituras iguais e com o dígito verificador correto. O mesmo código é ignorado por 8 s depois de aceito.
- O botão **Lanterna** (canto de cima) acende a luz do celular, e tocar na tela fora da tabela pede um foco naquele ponto. O [`ControleCamera`](Assets/Scripts/ControleCamera.cs) liga o foco contínuo sempre que o Vuforia começa ou volta de uma pausa.
- A cada 5 s o log mostra quantos quadros o leitor processou e quanto tempo levaram (`[RA] Leitor: ...`). Para guardar alguns quadros da câmera e examinar no computador, crie o arquivo `salvar_quadros` na pasta do app (`adb shell touch /sdcard/Android/data/com.talescn.rotulosra/files/salvar_quadros`): até 40 quadros vão para `Quadros/*.pgm`.
- Com o GTIN, os dados vêm do JSON local, do cache ou do Open Food Facts, nessa ordem. Se não houver tabela, a faixa diz o motivo: fora da base, na base sem tabela, sem conexão ou valores impossíveis.
- Tabelas fisicamente impossíveis vindas do Open Food Facts são recusadas: mais de 100 g de um nutriente em 100 g, macronutriente maior que a porção ou energia mais de 30% longe de 4 × C + 4 × P + 9 × G.

## Ensinar um rótulo

Para um produto lido pelo código de barras passar a ser reconhecido pela imagem, com a tabela em RA sobre o rótulo:

1. Com a tabela aberta no modo leitura, toque em **Ensinar rótulo**.
2. Aponte para o rótulo, de frente, bem perto e sem reflexo, e toque em **Capturar**. Por 0,8 s o [`CapturaRotulo`](Assets/Scripts/CapturaRotulo.cs) compara os quadros da câmera e fica com o mais nítido (variância do laplaciano), já girado para a orientação da tela.
3. A foto aparece congelada com um retângulo amarelo. Arraste os cantos (ou o meio) para deixar **só o rótulo** dentro dele: numa garrafa transparente, o que aparece através dela muda de lugar para lugar e estraga o marcador.
4. A nota do marcador, de 0 a 5, muda enquanto o retângulo é ajustado. Com 2 ou mais, **Criar marcador** cria o Image Target na hora, e a tabela já aparece sobre o rótulo.
5. Em garrafas, capture também os lados do rótulo: cada produto guarda até **4 vistas**, e a mais antiga sai quando passa disso. Várias vistas planas fazem o papel do Cylinder Target, que o Vuforia só cria pelo portal.

A nota ([`QualidadeMarcador`](Assets/Scripts/QualidadeMarcador.cs)) existe porque o Vuforia não dá a nota de estrelas do Target Manager em tempo de execução. Ela conta os cantos FAST-9 (o tipo de detalhe que o rastreamento procura) na imagem reduzida a 320 px e vê se eles cobrem uma grade de 6 × 6. Depois tira uma estrela por reflexo (mais de 3% de pixels estourados), por pouca luz e por recorte menor que 200 px, e mostra o motivo. Os limites foram calibrados com as fotos dos rótulos do projeto e com a faixa da Fanta vista pela câmera. A nota não sabe o que é transparente: recortar só o rótulo continua sendo trabalho de quem ensina.

Os marcadores ensinados ficam no aparelho ([`CatalogoMarcadores`](Assets/Scripts/CatalogoMarcadores.cs)): um PNG por vista e o índice `marcadores.json` em `persistentDataPath/Marcadores`, com `gtin`, `arquivo`, `larguraMetros`, `estrelas` e `criadoEm`. Quando o app abre, eles voltam junto com os produtos da cena. O tamanho real do rótulo não é medido: o marcador usa 8 cm e a tabela tem a mesma largura dele, então ela fica na proporção certa em qualquer rótulo (o zoom continua valendo).

## Diário de testes

No celular, o [`DiarioTestes`](Assets/Scripts/DiarioTestes.cs) guarda num arquivo as linhas `[RA]` do log e os erros, para medir um teste feito longe do computador, como num mercado, onde o celular fica sem cabo e o `logcat` se perde.

- Os arquivos ficam em `Android/data/com.talescn.rotulosra/files/Diario`, um por dia (`diario_AAAAMMDD.txt`), com hora, tipo e mensagem separados por tabulação. Cada arquivo para de crescer em 5 MB.
- Para copiar: `adb pull /sdcard/Android/data/com.talescn.rotulosra/files/Diario`.
- Entram o código lido, a origem dos dados (`no aparelho`, `Open Food Facts` ou `sem tabela` com o motivo), o fechamento de cada leitura, a nota de cada rótulo ensinado e as idas e voltas do app ao primeiro plano. Um `Diario iniciado` sem um `App fora de foco` antes indica que o app caiu.
- No Editor o diário fica desligado, porque o Console já mostra o log.
- O arquivo não tem dados pessoais: só GTINs, horários e o modelo do aparelho.

## Produtos de teste

| Produto | GTIN | Marcador | Tabela |
|---|---|---|---|
| Sprite (garrafa 200 ml) | 78939745 | `Assets/Fotos/rotulo1.jpg` | dados de `Resources/Produtos/78939745.json` |
| Leite Ninho Integral Forti+ (1 L) | 7898215157403 | `Assets/Fotos/rotulo2.jpg` | dados de `Resources/Produtos/7898215157403.json` |
| Creme de Cebola Maggi (68 g) | 7891000538500 | `Assets/Fotos/rotulo3.jpg` | dados de `Resources/Produtos/7891000538500.json` |
| Fanta Uva (garrafa 200 ml) | 78938571 | ensinado no app (**Ensinar rótulo**) | dados de `Resources/Produtos/78938571.json` |

A tabela da Fanta Uva foi transcrita da [foto do rótulo](https://world.openfoodfacts.org/product/78938571) enviada ao Open Food Facts, que tem o produto mas nenhum nutriente digitado. É o modelo antigo de rótulo (RDC 360), só com a porção de 200 ml: a coluna "100 ml" fica em branco, e "não contém quantidades significativas" de proteínas, gorduras e fibras virou 0.

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
│   ├── LeitorCodigoBarras.cs    lê o código de barras na imagem da câmera do Vuforia
│   ├── DecodificadorBarras.cs   decodifica EAN-13, EAN-8 e UPC-A com o ZXing
│   ├── LeituraEstavel.cs        confirma o código em duas leituras iguais
│   ├── ControleCamera.cs        foco contínuo, foco pelo toque e lanterna
│   ├── EnsinarRotuloUI.cs       fluxo do "Ensinar rótulo": captura, recorte, nota e criação do marcador
│   ├── CapturaRotulo.cs         escolhe o quadro mais nítido da câmera
│   ├── QualidadeMarcador.cs     nota de 0 a 5 de uma imagem candidata a marcador
│   ├── CatalogoMarcadores.cs    guarda os rótulos ensinados no aparelho
│   ├── ImagemCinza.cs           girar, recortar, reduzir e medir nitidez de imagens em cinza
│   ├── CalculosRecorte.cs       contas do retângulo de recorte
│   ├── AlcaRecorte.cs           alças que arrastam o retângulo
│   ├── ToqueParaFocar.cs        toque na tela de captura pede foco
│   ├── CalculosGestos.cs        contas do zoom, da pinça e do arrasto
│   ├── DiarioTestes.cs          guarda as linhas [RA] do log num arquivo no celular
│   └── AjustarAreaSegura.cs     mantém os controles fora do entalhe e das barras do sistema
├── Editor/
│   ├── MontarCenaAtividade.cs   menu que monta a cena automaticamente
│   ├── MontarInterfaceRA.cs     cria a interface (barra de zoom, botão Ler, lanterna, modo leitura)
│   ├── MontarEnsinoRA.cs        cria as telas de captura e de recorte do "Ensinar rótulo"
│   ├── GerarBuildAndroid.cs     menu que configura o Android e gera o APK
│   ├── GerarPreviaTabelas.cs    menu que salva um PNG de cada tabela em Builds/Previas
│   └── Testes/                  testes de EditMode
├── Plugins/ZXing/               ZXing.Net (DLL, licença e origem)
├── TextMesh Pro/                recursos essenciais do TextMeshPro (fonte e configurações)
└── link.xml                     evita que o IL2CPP remova as classes lidas do JSON
```

## Testes

Os testes de EditMode ficam em `Assets/Editor/Testes`: contas do zoom e da pinça, a estrutura da cena montada pelo menu, as configurações de Android, os JSONs dos produtos (campos, dígito do GTIN e valor energético contra 4 × carboidratos + 4 × proteínas + 9 × gorduras, com tolerância de 15%), o repositório, a tabela desenhada, o leitor de código de barras (com imagens geradas pelo próprio ZXing, inclusive giradas) e o "Ensinar rótulo": operações na imagem, a nota do marcador com a foto real do Maggi (inteira, borrada, com reflexo e pequena), o retângulo de recorte e o catálogo numa pasta temporária. Há também o diário de testes, gravado numa pasta temporária. Nenhum teste usa a rede nem a câmera. Rode por *Window > General > Test Runner > EditMode* ou pela linha de comando, com o Unity fechado:

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
2. Crie `Assets/Resources/Produtos/{gtin}.json` transcrevendo a tabela do rótulo (veja os JSONs existentes). Os testes conferem os campos e o valor energético.
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
- **Leitor de código de barras só depois de criados os marcadores**: o registro do formato da câmera espera o `GerenciadorRotulosRA` ficar pronto, e a imagem é pedida no `World.OnStateUpdated`, como na documentação do Vuforia. Pedir a imagem enquanto os marcadores eram criados derrubou o app uma vez no celular.
- **Câmera com o ARCore**: no realme 15T o Vuforia usa o ARCore 1.45 e entrega 1920 × 1080 a 30 fps. O foco contínuo é aceito, mas o foco numa região da imagem não existe com o ARCore (a documentação do Vuforia confirma). O toque para focar pede um foco único e volta ao contínuo depois de 2 s.
- **Marcador ensinado a partir da câmera**, e não de uma foto tirada fora: é a mesma câmera, a mesma luz e a mesma distância em que ele vai ser procurado. O quadro é girado 90° no sentido horário, a orientação do sensor no realme, para a tabela não aparecer tombada.

## Limitações conhecidas

- Os dados de cada produto são **transcritos à mão** do rótulo. Se o fabricante mudar a receita ou a tabela, é preciso atualizar o JSON.
- Pela imagem, o app reconhece os produtos da cena e os **rótulos ensinados no aparelho**. Os marcadores ensinados ficam só naquele celular.
- O giro do quadro ensinado supõe o sensor da câmera virado 90°, como no realme 15T. Num aparelho com o sensor virado 270°, a tabela apareceria de cabeça para baixo.
- A cobertura do Open Food Facts para produtos brasileiros é **desigual**: em outubro de 2026, o Ninho tinha a tabela igual à do rótulo, o Sprite 200 ml e a Fanta Uva 200 ml não tinham nenhum nutriente (a Fanta só a foto da tabela) e o Maggi 68 g tinha valores inconsistentes.
- Reconhecimento menos estável em superfícies curvas e com reflexo.

## Ideias de evolução

- Apagar os marcadores ensinados de um produto pelo próprio app.
- Ler o texto do rótulo com **OCR** e extrair a tabela nutricional e os ingredientes automaticamente.
- Destacar **alérgenos e substâncias** que cada pessoa quer evitar, e ler a tabela em voz alta.
- Escalar para um **catálogo maior** de produtos, por exemplo com o Cloud Recognition do Vuforia.
- Gerar o build para **iOS**.

