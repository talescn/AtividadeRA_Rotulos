# Realidade Aumentada com rótulos de produtos

Aplicação de **Realidade Aumentada baseada em marcadores** feita com **Unity + Vuforia**. O rótulo de um produto de mercado funciona como marcador: ao apontar a câmera para a embalagem, a **tabela nutricional real** daquele produto aparece na frente dele.

## Como funciona

1. O Vuforia inicia e o script [`GerenciadorRotulosRA`](Assets/Scripts/GerenciadorRotulosRA.cs) percorre a lista de produtos.
2. Para cada produto, a foto do rótulo vira um **Image Target criado em tempo de execução**, com a largura real da embalagem em metros.
3. Cada target recebe um quad com a foto da tabela nutricional do próprio produto, deitado sobre o plano do rótulo.
4. Quando o rótulo é reconhecido pela câmera, a tabela aparece. Quando o produto sai de vista, ela some.
5. A interface ([`InterfaceRotulosRA`](Assets/Scripts/InterfaceRotulosRA.cs)) deixa ampliar a tabela em RA e abrir o **modo leitura**, com a tabela em tela cheia.

Não há nenhum marcador fixo no projeto: trocar de produto é trocar duas fotos.

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
├── Scenes/
│   └── AtividadeRA_Rotulos.unity   cena principal
├── Scripts/
│   ├── GerenciadorRotulosRA.cs  cria os targets, exibe as tabelas e aplica o zoom
│   ├── InterfaceRotulosRA.cs    zoom, pinça, botão Ler e modo leitura
│   ├── CalculosGestos.cs        contas do zoom, da pinça e do arrasto
│   └── AjustarAreaSegura.cs     mantém os controles fora do entalhe e das barras do sistema
└── Editor/
    ├── MontarCenaAtividade.cs   menu que monta a cena automaticamente
    ├── MontarInterfaceRA.cs     cria a interface (barra de zoom, botão Ler, modo leitura)
    ├── GerarBuildAndroid.cs     menu que configura o Android e gera o APK
    └── Testes/                  testes de EditMode
```

## Testes

Os testes de EditMode ficam em `Assets/Editor/Testes`: contas do zoom e da pinça, a estrutura da cena montada pelo menu e as configurações de Android. Rode por *Window > General > Test Runner > EditMode* ou pela linha de comando, com o Unity fechado:

```bash
Unity.exe -batchmode -projectPath . -runTests -testPlatform EditMode -testResults resultados.xml
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
3. Apague a cena atual e use o menu **Atividade RA > Montar cena dos rotulos**. Ele configura a importação das fotos, cria a cena com o `GerenciadorRotulosRA` preenchido e a interface de zoom e leitura, e a adiciona ao Build Settings.

## Decisões técnicas

- **Image Targets em tempo de execução** (`ObserverFactory.CreateImageTarget`), sem depender de banco de dados criado no portal do Vuforia.
- **`maxSimultaneousImageTargets: 3`**. Com apenas 1 e *extended tracking*, um produto "prendia" o slot e impedia o reconhecimento do próximo.
- **Filtro `Tracked`** no `DefaultObserverEventHandler`: a tabela só aparece enquanto o rótulo está realmente visível, evitando tabela "fantasma" na tela depois que o produto sai do campo de visão.
- **Só o Input System novo**: a pinça usa o EnhancedTouch, o Editor usa o mouse, e o `EventSystem` usa o `InputSystemUIInputModule`.
- **Toque na tabela por raycast**: o quad da tabela mantém o collider, que o `DefaultObserverEventHandler` liga e desliga junto com a tabela.

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

