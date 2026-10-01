# AtividadeRA_Rotulos — Realidade Aumentada Marker Based

Projeto da **Atividade 01 de Mundos Virtuais e Realidade Aumentada (MVRA)**, Grupo Anchieta, Prof. Clayton Valdo (entrega 30/09/2026, MP4, por grupo).

## Objetivo
App em **Unity + Vuforia** em que o **rótulo** de 3 produtos de mercado é o marcador (Image Target).
Ao filmar o produto, a **tabela nutricional real daquele produto** aparece na frente dele.

Produtos usados:
| # | Produto | Marcador | Tabela |
|---|---------|----------|--------|
| 1 | Sprite (garrafa 200 ml) | `Assets/Fotos/rotulo1.jpg` | `Assets/Fotos/tabela1.jpg` |
| 2 | Leite Ninho Integral 1 L | `Assets/Fotos/rotulo2.jpg` | `Assets/Fotos/tabela2.jpg` |
| 3 | Creme de Cebola Maggi 68 g | `Assets/Fotos/rotulo3.jpg` | `Assets/Fotos/tabela3.jpg` |

## Stack
- Unity **6000.6.0f1** (Unity 6.6), template Universal 3D (URP)
- Vuforia Engine **11.4.4** (`com.ptc.vuforia.engine`)
- Teste feito no Play Mode do Editor com webcam

## Estrutura
- `Assets/Scripts/GerenciadorRotulosRA.cs` — em runtime, quando o Vuforia inicia, cria um Image Target para cada rótulo
  (`VuforiaBehaviour.Instance.ObserverFactory.CreateImageTarget(texture, larguraMetros, nome)`), adiciona
  `DefaultObserverEventHandler` (filtro `Tracked`, para não deixar tabela "fantasma" no extended tracking) e cria um Quad
  com material Unlit mostrando a tabela, deitado sobre o plano do target (rotação X=90°, Y = `distanciaFrente`).
- `Assets/Editor/MontarCenaAtividade.cs` — menu **Atividade RA > Montar cena dos rotulos**: ajusta a importação das fotos
  (Read/Write ON, sem compressão, sem NPOT scale), cria `Assets/Scenes/AtividadeRA_Rotulos.unity` com ARCamera +
  `GerenciadorRotulosRA` configurado e coloca a cena no Build Settings. Roda sozinho se a cena ainda não existir.
- `Assets/Resources/VuforiaConfiguration.asset` — config do Vuforia (`maxSimultaneousImageTargets: 3`).
  **Contém a license key — não versionar** (está no `.gitignore`).
- `Assets/Fotos/` — fotos recortadas dos rótulos e das tabelas.

## Parâmetros por produto (no componente GerenciadorRotulosRA)
| Produto | Largura rótulo (m) | Largura tabela (m) |
|---------|-------------------|--------------------|
| Sprite | 0.06 | 0.16 |
| LeiteNinho | 0.095 | 0.09 |
| CremeCebolaMaggi | 0.115 | 0.11 |

## Como rodar
1. Ter o pacote `com.ptc.vuforia.engine-11.4.4.tgz` (baixar no Vuforia Developer Portal) e ajustar o caminho em
   `Packages/manifest.json` (hoje aponta para `file:C:/Users/Tales/My project/Packages/...`). O .tgz tem ~138 MB,
   acima do limite do GitHub, por isso não fica no repositório.
2. Abrir o projeto no Unity Hub, colar a license key em *Window > Vuforia Configuration*.
3. Abrir a cena `AtividadeRA_Rotulos` e dar Play; mostrar os produtos para a webcam.

## Lições / problemas conhecidos
- Sprite: garrafa curva e transparente → rastreamento mais instável. Mostrar de perto (15–20 cm), logo de frente, sem reflexo.
- Ninho e Maggi rastreiam rápido (muito detalhe e contraste).
- Com `maxSimultaneousImageTargets: 1` e extended tracking, um produto "prendia" o slot e impedia reconhecer o próximo — resolvido com 3 targets simultâneos + filtro `Tracked`.

## Convenções
- Código e comentários em português, sem acentos nos identificadores.
- Para trocar um produto: substituir `rotuloN.jpg`/`tabelaN.jpg` em `Assets/Fotos`, ajustar nomes/larguras em
  `MontarCenaAtividade.cs`, apagar a cena e rodar o menu *Atividade RA > Montar cena dos rotulos*.
