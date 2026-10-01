# Realidade Aumentada com rótulos de produtos (Unity + Vuforia)

Aponte a câmera para o rótulo de um produto e a **tabela nutricional** dele aparece na frente da embalagem.

Atividade 01 da disciplina **Mundos Virtuais e Realidade Aumentada**, Grupo Anchieta.

- **Unity** 6000.6.0f1 (URP)
- **Vuforia Engine** 11.4.4
- Produtos: Sprite, Leite Ninho Integral e Creme de Cebola Maggi

## Como rodar
1. Baixe o Vuforia Engine 11.4.4 (`.tgz`) no [Vuforia Developer Portal](https://developer.vuforia.com/) e ajuste o caminho em `Packages/manifest.json`.
2. Abra o projeto no Unity Hub e cole sua license key em **Window > Vuforia Configuration**.
3. Abra `Assets/Scenes/AtividadeRA_Rotulos.unity` e aperte Play com a webcam ligada.

Para trocar os produtos, substitua as imagens em `Assets/Fotos` e use o menu **Atividade RA > Montar cena dos rotulos**.
