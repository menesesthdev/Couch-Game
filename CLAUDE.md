# CLAUDE.md

Contexto e convenções deste projeto para sessões do Claude Code. Leia antes de gerar ou alterar código.

## Visão geral

**coachgame** — estimador de progressão de rank para Valorant. O jogador cola o link do perfil (tracker.gg ou `nome#tag` + região) e a aplicação estima quantos dias/partidas faltam para atingir um rank-alvo, com base em RR médio ganho/perdido, win rate e ritmo de partidas por dia. Sem login — os dados vêm de um perfil público, do mesmo jeito que qualquer tracker de Valorant funciona.

Por enquanto, **apenas Valorant**. Não generalizar para outros jogos ainda.

## Stack e arquitetura

- Backend: .NET 10, ASP.NET Core (controllers), C#
- Frontend: Angular 22 (nunca React) — standalone components, signals, zoneless
- Banco: PostgreSQL, migrations com EF Core (Npgsql, nomes em snake_case via EFCore.NamingConventions)
- Sem autenticação de usuário nesta fase — não implementar login, RSO ou OAuth

Backend em Clean Architecture / DDD, seguindo o mesmo padrão de camadas usado nos outros projetos do autor:

```
backend/
  CoachGame.slnx
  src/
    CoachGame.Domain/          # entidades, value objects, regras de negócio puras (sem dependências)
    CoachGame.Application/     # casos de uso + portas (interfaces) para fonte de dados e repositórios
    CoachGame.Infrastructure/  # cliente HTTP da HenrikDev API, EF Core/DbContext, migrations
    CoachGame.Api/             # controllers REST, DTOs de entrada/saída, tratamento de erros
  tests/
    CoachGame.Tests/           # xUnit
```

Dependências apontam para dentro: Api → Application/Infrastructure → Domain. O domínio não conhece EF, HTTP nem ASP.NET.

Exceções de domínio (`DomainException`) viram HTTP 400; `JogadorNaoEncontradoException` → 404; `FonteIndisponivelException` → 503 — tudo como ProblemDetails com `title` legível para a UI (ver `ErrosHandler`).

Nomeie entidades de domínio em português, como nos projetos anteriores (`Paciente`, `Consulta` etc.):

- `Jogador` — Riot ID (nome + tag + região), PUUID, rank/RR atuais
- `SnapshotRR` — registro histórico de rank/RR em um instante (é o que alimenta win rate e médias reais)
- `Meta` — rank-alvo + data-limite definidos pelo usuário
- `CenarioEstimativa` — resultado do cálculo para um win rate (`Real` = desempenho atual; `Simulacao` = "e se você vencer X%?")
- `AtoCompetitivo` — ato da temporada (nome, início, fim), usado como prazo padrão da meta
- Value objects: `RiotId` (parse de `nome#tag` e link do tracker.gg), `Rank` (tier + RR), enum `Tier` (IDs iguais aos da HenrikDev: 3 = Ferro 1 … 27 = Radiante)
- `CalculadoraEstimativa` — serviço de domínio com a regra central abaixo

## Regra de negócio central: o cálculo da estimativa

Sem formalismo matemático — é aritmética simples, não estatística pesada:

1. Calcular quanto RR falta entre o RR atual e a meta (considerando também a troca de rank/divisão, não só o número de RR).
2. Para cada cenário, calcular o saldo de RR por partida: `win rate × RR médio ganho − (1 − win rate) × RR médio perdido`. Cenários: o **real** (win rate do histórico) e **3 simulações** ("e se você vencer mais?") no mesmo ritmo de partidas/dia do jogador, começando no primeiro múltiplo de 5% acima tanto do win rate atual quanto do ponto de equilíbrio (`perda / (ganho + perda)`) — ou seja, **toda simulação sobe**. Não mostrar cenários em que o jogador nunca chega (o antigo 1V/2D foi removido por isso).
3. Dividir o RR que falta pelo RR médio ganho por partida → partidas necessárias.
4. Dividir partidas necessárias pelo ritmo de partidas por dia do jogador → dias estimados.
5. O "cenário real" usa as médias calculadas a partir do `SnapshotRR` mais recente (win rate real, RR médio ganho/perdido reais, partidas/dia reais).

Sempre apresentar o resultado como estimativa, nunca como previsão exata — deixar isso explícito na resposta da API e na UI.

Até Ascendente 3 cada divisão vale 100 RR. Do Imortal 1 em diante a HenrikDev devolve RR **acumulado** (ex.: Imortal 2 com 156 RR) e os cortes reais dependem do leaderboard; simplificação atual: Imortal 2, Imortal 3 e Radiante começam 100, 200 e 300 RR acima do Imortal 1 (ver `Rank`).

Além dos cenários, a API devolve os **requisitos do prazo** (`RequisitosPrazo`): partidas/dia necessárias mantendo o win rate real e win rate necessário mantendo o ritmo real, e os **degraus** (RR que falta em cada divisão até a meta). Sem histórico recente, as simulações usam médias padrão (+20 / −18, 3 partidas/dia) e o cenário real é omitido.

**Prazo padrão = final do ato atual.** O calendário vem de `valorant-api.com/v1/seasons` (sem chave, cache de 6 h; `ValorantApiCalendario`), exposto em `GET /api/calendario/ato-atual`. O fim do ato é em UTC (ex.: 14/10 00:00Z); a UI usa o último dia local antes disso (13/10 no Brasil). O usuário pode trocar para "Outra data".

Refinamentos como RR médio caindo conforme o jogador sobe de elo são melhoria futura, não bloqueio para o MVP.

## Fonte de dados externa: HenrikDev API

Documentação: https://docs.henrikdev.xyz — API comunitária não oficial, não afiliada à Riot Games.

- Autenticação: chave de API simples no header `Authorization`, sem OAuth. Começar com chave **Basic** (30 req/min). Configurar via `dotnet user-secrets` ou variável `HenrikDev__ApiKey` — nunca commitar.
- Endpoint em uso: `GET /valorant/v2/mmr-history/{region}/{platform}/{name}/{tag}` — uma chamada traz PUUID, rank atual (entrada mais recente) e, por partida, mapa, `last_change`, tier e RR após a partida. Formato conferido com dados reais.
- Endpoints da nossa API: `GET /api/jogadores/perfil?perfil=&regiao=`, `GET /api/jogadores/busca?q=&regiao=`, `POST /api/estimativas`, `GET /api/calendario/ato-atual`, `GET /api/ranks`.
- Endpoints relevantes (conferir a doc antes de implementar, os paths podem mudar entre versões):
  - MMR atual por `nome#tag` + região/plataforma
  - Histórico de MMR (`mmr-history`, e a variante `stored-mmr-history` que já vem persistida pelo lado deles — avaliar se reduz a necessidade de polling próprio)
  - Histórico de partidas (`matchlist`/`match`) para calcular win rate e RR médio ganho/perdido reais
- Resolver o Riot ID a partir do link colado: URLs do tracker.gg seguem o padrão `tracker.gg/valorant/profile/riot/nome%23tag/overview` — extrair `nome` e `tag` (decodificar `%23` como `#`), e também aceitar `nome#tag` digitado direto.
- Tratar rate limit (HTTP 429) com backoff e cache — não fazer polling agressivo por jogador. Hoje: `AddStandardResilienceHandler` (retry exponencial respeitando `Retry-After`) + `IMemoryCache` de 10 min por perfil.
- **Não é uma fundação com garantia de estabilidade.** Se o projeto crescer além de escala de portfólio, o caminho correto é migrar para a API oficial da Riot com RSO — não tentar contornar limites da HenrikDev nem fazer scraping próprio do cliente do jogo.

## Frontend — diretrizes de design

**Layout de tracker** (busca → página de perfil com estatísticas), **visual inspirado no ChatGPT** apenas nos componentes: formato de botões, campos em pílula, fontes, cantos arredondados e sombras suaves. Não é uma interface de chat — não usar bolhas de mensagem nem layout conversacional.

- Páginas: home (título + busca grande centralizada) e perfil `/perfil/:regiao/:nome%23tag` (mesmo formato de URL do tracker.gg), com busca compacta na barra do topo
- Home: título, busca, buscas recentes clicáveis e "como funciona" em 3 passos
- Perfil: cabeçalho (ícone do rank, Riot ID, chips) → painel da meta em largura total (resposta, caminho de divisões, o que precisa para o prazo, simulações) → "Seu desempenho recente" (rank atual, estatísticas, partidas recentes com "mostrar todas")
- Busca com autocomplete: sugere jogadores já consultados (`GET /api/jogadores/busca`) + buscas recentes do navegador (localStorage). Não existe busca global de contas na Riot/HenrikDev — igual ao tracker.gg, só dá para sugerir quem já passou pela aplicação
- **Tema escuro único** (sem tema claro): fundo grafite, cards levemente mais claros, botão primário claro sobre escuro; verde/vermelho dessaturados só para ganho/perda de RR e status
- Cards com contorno sutil em vez de bordas duras; tipografia como principal hierarquia (tamanho/peso)
- **Intuitivo primeiro**: a resposta vem numa frase em linguagem direta ("No seu ritmo atual, você chega em X em cerca de N dias"), os detalhes depois. Evitar jargão na UI (win rate como "Vencendo 75%" / "8 de cada 10 partidas"; ritmo < 1/dia → "1 partida a cada N dias")
- Meta é uma frase editável ("Quero chegar em [rank] até [Final do ato | Outra data]") que recalcula sozinha ao mudar — sem botão "Calcular"; "Final do ato" vem pré-selecionado; selects em pílula sempre com seta (`.com-seta`)
- A resposta responde **quando**: data estimada de chegada na frase, selo "N dias antes/depois do fim do ato" e uma **linha do tempo** (hoje → prazo, com o ícone do rank-alvo na data estimada; vermelho se passar do prazo)
- Logo do projeto no canto superior esquerdo (`public/logo.png`, recortada de `assets/images/`), no lugar do nome
- "Como calculamos?" em `<details>` explica o método com os números do próprio jogador
- Ícones de rank vêm de `media.valorant-api.com` (mesma numeração de tier da HenrikDev)
- Nada de gráficos de BI; barras finas de progresso, a linha do tempo da meta e pontos de forma (V/D) são o limite
- Angular standalone components, signals, `rxResource`; sem estado global — estado local nos componentes
- Cores só por variáveis CSS em `src/styles.scss`; blocos compartilhados (`.card`, `.chip`, `.botao-*`, `.com-seta`) também lá

## Comandos

```bash
# tudo no Docker: web http://localhost:4200, API http://localhost:5080, Postgres :5432
# (chave da HenrikDev em .env → HENRIKDEV_API_KEY; ver .env.example)
docker compose up -d --build

# só o banco, para rodar backend/frontend localmente
docker compose up -d postgres

# backend (API em http://localhost:5080; aplica migrations na subida em Development ou com Banco__MigrarAoIniciar=true)
cd backend && dotnet run --project src/CoachGame.Api
cd backend && dotnet test

# nova migration
cd backend && dotnet tool restore
dotnet ef migrations add <Nome> --project src/CoachGame.Infrastructure --startup-project src/CoachGame.Api --output-dir Persistencia/Migrations

# frontend (http://localhost:4200, proxy de /api para o backend)
cd frontend && npm start
cd frontend && npm test
```

## Fora de escopo por enquanto

- Outros jogos além de Valorant
- Login / RSO / OAuth
- Notificações (e-mail, push, Discord)
- Multi-idioma
