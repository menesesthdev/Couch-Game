# CLAUDE.md

Contexto e convenções deste projeto para sessões do Claude Code. Leia antes de gerar ou alterar código.

## Visão geral

**Valorant Coach** — estimador de progressão de rank para Valorant. O jogador cola o link do perfil (tracker.gg ou `nome#tag` + região) e a aplicação estima quantos dias/partidas faltam para atingir um rank-alvo, com base em RR médio ganho/perdido, win rate e ritmo de partidas por dia. Sem login — os dados vêm de um perfil público, do mesmo jeito que qualquer tracker de Valorant funciona.

Por enquanto, **apenas Valorant**. Não generalizar para outros jogos ainda.

## Stack e arquitetura

- Backend: .NET 10, ASP.NET Core (controllers), C#
- Frontend: Angular 22 (nunca React) — standalone components, signals, zoneless
- Banco: PostgreSQL, migrations com EF Core (Npgsql, nomes em snake_case via EFCore.NamingConventions)
- Duas áreas distintas: **progressão** (anônima, qualquer jogador, é o coração do projeto) e **loja** (só a conta conectada). Nenhuma tela anônima pode mudar de comportamento por existir sessão

O projeto se chamou **coachgame** até 20/09/2026. O código usa `ValorantCoach.*`, mas **a infraestrutura ainda usa `coachgame` de propósito**: nomes de container, volume e o banco/usuário/senha do Postgres. Renomear obrigaria a recriar o volume e perder os dados locais, sem ganho nenhum. A chave da connection string, essa sim, é `ValorantCoach` (em `appsettings.json`, no `DependencyInjection` e no `docker-compose.yml`). A chave `coachgame.buscas-recentes` no localStorage também fica, para não apagar as buscas recentes de quem já usou.

Backend em Clean Architecture / DDD, seguindo o mesmo padrão de camadas usado nos outros projetos do autor:

```
backend/
  ValorantCoach.slnx
  src/
    ValorantCoach.Domain/          # entidades, value objects, regras de negócio puras (sem dependências)
    ValorantCoach.Application/     # casos de uso + portas (interfaces) para fonte de dados e repositórios
    ValorantCoach.Infrastructure/  # cliente HTTP da HenrikDev API, EF Core/DbContext, migrations
    ValorantCoach.Api/             # controllers REST, DTOs de entrada/saída, tratamento de erros
  tests/
    ValorantCoach.Tests/           # xUnit
```

Dependências apontam para dentro: Api → Application/Infrastructure → Domain. O domínio não conhece EF, HTTP nem ASP.NET.

Exceções de domínio (`DomainException`) viram HTTP 400; `JogadorNaoEncontradoException` → 404; `FonteIndisponivelException` → 503 — tudo como ProblemDetails com `title` legível para a UI (ver `ErrosHandler`).

Nomeie entidades de domínio em português, como nos projetos anteriores (`Paciente`, `Consulta` etc.):

- `Jogador` — Riot ID (nome + tag + região), PUUID, rank/RR atuais
- `SnapshotRR` — registro histórico de rank/RR em um instante (é o que alimenta win rate e médias reais)
- `Meta` — rank-alvo + data-limite definidos pelo usuário
- `CenarioEstimativa` — resultado do cálculo para um win rate (`Real` = desempenho atual; `Simulacao` = "e se você vencer X%?")
- `AtoCompetitivo` — ato da temporada (nome, início, fim), usado como prazo padrão da meta
- `ResumoPartida` — uma partida vista só pelo lado do jogador (agente, mapa, K/D/A, tiros, dano, rounds) já com o RR casado; `EstatisticasRecentes` agrega o período em KD, headshot, dano/round, pontos/round, sequência atual e agentes mais jogados
- `PeriodoAnalise` — `Ultimas` (padrão, 20 partidas) ou `AtoAtual` (tudo dentro do ato em andamento)
- `DetalhePartida` / `JogadorPartida` — scoreboard completo dos dois times, usado na página de partida
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
- Endpoints em uso (formato conferido com dados reais):
  - `GET /valorant/v2/mmr-history/{region}/{platform}/{name}/{tag}` — PUUID, rank atual (entrada mais recente) e, por partida, `match_id`, mapa, `last_change`, tier e RR após a partida. O `match_id` é o que liga cada linha do histórico ao scoreboard.
  - `GET /valorant/v1/stored-matches/{region}/{name}/{tag}?mode=competitive&size=&page=` — agente, mapa, K/D/A, tiros (cabeça/corpo/perna), dano e placar de rounds. Aceita `size` até pelo menos 200 e pagina com `page`.
  - `GET /valorant/v1/stored-mmr-history/{region}/{name}/{tag}?size=&page=` — RR por partida, **com `match_id` e temporada**, paginado de verdade. Existe porque o `v2/mmr-history` **ignora `size`** e devolve só as últimas ~8 partidas; sem este endpoint não dá para listar 20 partidas com RR.
  - As duas fontes acima são casadas pelo `match_id` em `AnalisarDesempenho` — desempenho e RR vêm de endpoints diferentes. Partida sem entrada de RR (colocação) entra na lista mesmo assim, sem a variação.
  - `GET /valorant/v4/match/{region}/{matchid}` — scoreboard dos 10 jogadores, rounds e kills (~380 KB). Só é chamado quando alguém abre uma partida; partida encerrada não muda, então o cache é de 6 h (`CacheDetalhePartida`).
- **Arma só existe dentro do detalhe da partida**, ou seja, "top armas" custaria uma requisição por partida. Por isso o card lateral não tem armas — decisão consciente, não esquecimento.
- Endpoints da nossa API: `GET /api/jogadores/perfil?perfil=&regiao=`, `GET /api/jogadores/desempenho?perfil=&regiao=&periodo=`, `GET /api/jogadores/busca?q=&regiao=`, `GET /api/partidas/{regiao}/{matchId}`, `POST /api/estimativas`, `GET /api/calendario/ato-atual`, `GET /api/ranks`.
- Varrer o ato inteiro pagina de 100 em 100 e para na primeira partida anterior ao início do ato, com teto de 3 páginas — o suficiente para qualquer jogador humano sem estourar o rate limit.
- Antes de mexer em qualquer endpoint, conferir a doc: os paths mudam entre versões.
- Resolver o Riot ID a partir do link colado: URLs do tracker.gg seguem o padrão `tracker.gg/valorant/profile/riot/nome%23tag/overview` — extrair `nome` e `tag` (decodificar `%23` como `#`), e também aceitar `nome#tag` digitado direto.
- Tratar rate limit (HTTP 429) com backoff e cache — não fazer polling agressivo por jogador. Hoje: `AddStandardResilienceHandler` (retry exponencial respeitando `Retry-After`) + `IMemoryCache` de 10 min por perfil e por lista de partidas, 6 h por partida encerrada.
- **Não é uma fundação com garantia de estabilidade.** Se o projeto crescer além de escala de portfólio, o caminho correto é migrar para a API oficial da Riot com RSO — não tentar contornar limites da HenrikDev nem fazer scraping próprio do cliente do jogo.

## Loja pessoal (`/loja`)

Área separada do resto, e a única que depende de conta conectada. Espelha o que os *shop checkers* open-source fazem.

- **Login pela página oficial da Riot** (OAuth implicit, `client_id=riot-client`). O `response_type` **tem de ser `token id_token`**: com `scope=openid`, a Riot recusa `token` sozinho com *"The OpenID Connect response type cannot have token as the only value"*. Por isso o fragmento volta com os dois tokens, e o que interessa é o `access_token`. `ui_locales=pt-BR` deixa a tela de login em português. A senha **nunca** passa pela aplicação. Como esse client_id obriga `redirect_uri=http://localhost/redirect`, o navegador cai numa página de erro e o jogador copia a URL inteira: o token vem no fragmento (`#`), que o navegador não envia a servidor nenhum. É feio e não tem contorno — a UI explica os 3 passos em vez de esconder.
- **Nunca pedir usuário e senha**, em nenhuma hipótese. Isso é coleta de credencial, viola o ToS da Riot e é o que separa este desenho de um phishing.
- Sessão: `SessaoRiot` fica em `IMemoryCache` com id opaco em cookie HttpOnly (`SessaoLojaStore`). **Nada de token em banco nem em localStorage** — dura ~1h e some ao reiniciar a API, o que é aceitável para algo que expira sozinho. Os JWTs somados passam perto do limite de 4 KB por cookie, então o token não cabe no cookie de qualquer forma.
- Fonte: endpoints do **próprio cliente do jogo** em `pd.{shard}.a.pvp.net`, mais `entitlements.auth.riotgames.com` e `auth.riotgames.com/userinfo`. Shard: BR e LATAM caem em `na`.
  - Loja: **`POST /store/v3/storefront/{puuid}` com corpo `{}`**. A v2 (GET) foi removida pela Riot e hoje responde **404 em todos os shards** — e a documentação da comunidade (valapidocs, valorant-api-docs) **ainda descreve a v2**, então não confie nela sem testar. Conferido com conta real em 20/09/2026.
  - Carteira: `GET /store/v1/wallet/{puuid}` (essa continua v1).
  - Se o storefront der 404, teste `GET /account-xp/v1/players/{puuid}` no mesmo shard: se ele responder 200, a conta e o shard estão certos e o problema é o endpoint da loja.
  - Preço do bundle vem pronto em `TotalBaseCost`/`TotalDiscountedCost`; somar os itens dá outro número.
- Fica em `Infrastructure/Riot/`, **fora de `HenrikDev/`**, e atrás da porta `ILojaRiot` — nunca estender `IValorantDataProvider`, que é sobre desempenho do jogador. `Domain/Loja/` não conhece `Jogador` nem `CalculadoraEstimativa`.
- Ids viram nome e arte pelo `ICatalogoValorant` (valorant-api.com, cache de 12 h).
- **Não são endpoints documentados para terceiros**: podem quebrar sem aviso. Erro no login (mesmo 5xx, que a Riot devolve para token malformado) vira "refaça o login", não "tente de novo" — senão o jogador fica preso repetindo algo que nunca vai funcionar.

## Frontend — diretrizes de design

**Layout de tracker** (busca → página de perfil com estatísticas), **visual inspirado no ChatGPT** apenas nos componentes: formato de botões, campos em pílula, fontes, cantos arredondados e sombras suaves. Não é uma interface de chat — não usar bolhas de mensagem nem layout conversacional.

- Páginas: home (título + busca grande centralizada), perfil `/perfil/:regiao/:nome%23tag` (mesmo formato de URL do tracker.gg), partida `/partida/:regiao/:matchId?jogador=nome%23tag` e loja `/loja`, com busca compacta na barra do topo
- Header tem a nav das duas áreas (Progressão · Loja) ao lado da logo
- Home: título, busca, buscas recentes clicáveis e "como funciona" em 3 passos
- Perfil: cabeçalho (ícone do rank, Riot ID, chips) → painel da meta em largura total (resposta, caminho de divisões, o que precisa para o prazo, simulações) → "Seu desempenho": lateral com rank atual, estatísticas de RR e o card "Seu jogo", e ao lado a lista de partidas com "mostrar todas"
- **Um seletor de período governa a seção inteira** (card e lista): "Últimas 20 partidas" (padrão) ou "Todo o ato atual". Ambos vêm do mesmo `rxResource`, que é separado do perfil — é a segunda chamada à fonte externa e o perfil abre mesmo se ela falhar
- Card "Seu jogo" **explica cada número em vez de só exibi-lo**: KD vira "Você troca em pé de igualdade", headshot vira "1 a cada 5 tiros que acertam", dano/round vira "cerca de 1,5 vida por round" (100 de dano = uma vida). Tem ainda a sequência atual ("3 vitórias seguidas") e os agentes mais jogados. **Sem top de mapas** — foi removido de propósito, para o card não virar uma parede de listas
- Nunca comparar o jogador com "a média" nesses textos: não temos esse dado e seria invenção
- Cada linha da lista traz retrato do agente, mapa, placar de rounds, K/D/A, headshot, rank e RR, e é um link para a página da partida — que mostra o placar, a faixa de rounds, o resumo do jogador e o scoreboard dos dois times
- Busca com autocomplete: sugere jogadores já consultados (`GET /api/jogadores/busca`) + buscas recentes do navegador (localStorage). Não existe busca global de contas na Riot/HenrikDev — igual ao tracker.gg, só dá para sugerir quem já passou pela aplicação
- **Tema escuro único** (sem tema claro): fundo grafite, cards levemente mais claros, botão primário claro sobre escuro; verde/vermelho dessaturados só para ganho/perda de RR e status
- Cards com contorno sutil em vez de bordas duras; tipografia como principal hierarquia (tamanho/peso)
- **Intuitivo primeiro**: a resposta vem numa frase em linguagem direta ("No seu ritmo atual, você chega em X em cerca de N dias"), os detalhes depois. Evitar jargão na UI (win rate como "Vencendo 75%" / "8 de cada 10 partidas"; ritmo < 1/dia → "1 partida a cada N dias")
- Meta é uma frase editável ("Quero chegar em [rank] até [Final do ato | Outra data]") que recalcula sozinha ao mudar — sem botão "Calcular"; "Final do ato" vem pré-selecionado; selects em pílula sempre com seta (`.com-seta`)
- A resposta responde **quando**: data estimada de chegada na frase, selo "N dias antes/depois do fim do ato" e uma **linha do tempo** (hoje → prazo, com o ícone do rank-alvo na data estimada; vermelho se passar do prazo)
- Logo do projeto no canto superior esquerdo, no lugar do nome: `public/logo.png` é o lockup "VALORANT COACH" recortado de `assets/images/`, exibido com altura fixa de 44px e largura automática (é deitado, ~1,43:1 — não force um quadrado). O `public/favicon.ico` sai **só da marca** (V + seta), porque o lockup inteiro vira um borrão a 16px; traz os tamanhos 48/32/16
- Ao regerar esses arquivos a partir do original: recorte pelo conteúdo visível (o PNG tem um halo preto de alfa baixo em volta, que some no grafite do tema), exporte a ~3x do tamanho exibido e **não quantize** — a paleta reduzida destrói o degradê do brilho
- "Como calculamos?" em `<details>` explica o método com os números do próprio jogador
- Imagens vêm de `media.valorant-api.com` (ver `core/midia.ts`): rank pela mesma numeração de tier da HenrikDev; agente pelo `displayicon.png` (retrato quadrado, como no tracker — solto sobre o card, aproveitando a transparência do PNG; nada de círculo ou caixa colorida atrás, que cortam o rosto); mapa pelo `stylizedbackgroundimage.png`, a arte que a Riot já entrega escurecida (361 KB contra 1,5 MB do `splash`)
- Cada partida do histórico e o cabeçalho da página de partida levam a arte do mapa ao fundo do bloco inteiro, em opacidade alta o bastante para reconhecer o mapa (0,85 na lista e 0,9 no cabeçalho — a arte já vem escura da Riot) e sumindo para a direita (`mask-image`), para os números continuarem legíveis — é o efeito do tracker. O fundo é um `<span class="fundo">` com `z-index: -1` dentro de um bloco com `isolation: isolate`, e não um `::before`, porque a URL é dinâmica
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
cd backend && dotnet run --project src/ValorantCoach.Api
cd backend && dotnet test

# nova migration
cd backend && dotnet tool restore
dotnet ef migrations add <Nome> --project src/ValorantCoach.Infrastructure --startup-project src/ValorantCoach.Api --output-dir Persistencia/Migrations

# frontend (http://localhost:4200, proxy de /api para o backend)
cd frontend && npm start
cd frontend && npm test
```

## Fora de escopo por enquanto

- Outros jogos além de Valorant
- Conta de usuário nossa (cadastro, senha, perfil salvo) — a única sessão que existe é a da Riot, na loja
- Notificações (e-mail, push, Discord)
- Multi-idioma
