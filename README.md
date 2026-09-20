# Valorant Coach

Estimador de progressão de rank para Valorant.

## Sobre o projeto

Ferramenta que estima quanto tempo (em dias e partidas) um jogador de Valorant levaria para atingir um rank-alvo, com base no desempenho atual: RR ganho/perdido por partida, win rate e ritmo de partidas por dia.

O jogador **não precisa fazer login**. Basta colar o link do perfil (tracker.gg ou Riot ID direto) e a aplicação busca os dados públicos automaticamente — assim como qualquer tracker de Valorant faz.

## Como funciona

1. Usuário cola o link do perfil (ex: `tracker.gg/valorant/profile/riot/nome%23tag/overview`) ou digita `nome#tag` + região diretamente.
2. O backend resolve o PUUID e busca o rank atual, RR e histórico recente de partidas.
3. O usuário define uma meta: rank-alvo + data-limite (ex.: *"Estou Platina 3 com 65 RR e quero chegar em Ascendente até 14/10"*).
4. O sistema calcula a estimativa em 4 cenários:
   - 3 vitórias / 0 derrotas por dia
   - 2 vitórias / 1 derrota por dia
   - 1 vitória / 2 derrotas por dia
   - Desempenho real do jogador, baseado no histórico
5. A estimativa é reavaliada conforme o jogador ganha ou perde RR ao longo do tempo. É sempre apresentada como **estimativa** — o resultado real varia partida a partida.

## Stack

- **Backend:** .NET 10 + ASP.NET Core (Clean Architecture / DDD)
- **Frontend:** Angular 22
- **Banco de dados:** PostgreSQL + EF Core migrations
- **Fonte de dados:** [HenrikDev Unofficial Valorant API](https://docs.henrikdev.xyz) — não é afiliada, endossada ou operada pela Riot Games

## Rodando localmente

### Tudo no Docker

```bash
cp .env.example .env         # coloque sua HENRIKDEV_API_KEY
docker compose up -d --build
```

- App: http://localhost:4200
- API: http://localhost:5080
- PostgreSQL: `localhost:5432` (usuário/senha/banco `coachgame`, nome antigo do projeto — mantido para não recriar o volume)

### Desenvolvimento (fora do Docker)

Pré-requisitos: .NET SDK 10, Node.js 22+, Docker (para o PostgreSQL) e uma chave da HenrikDev API ([obter aqui](https://discord.com/invite/X3GaVkX2YN)).

```bash
docker compose up -d postgres

# backend — http://localhost:5080
cd backend
dotnet user-secrets set "HenrikDev:ApiKey" "sua-chave" --project src/ValorantCoach.Api
dotnet run --project src/ValorantCoach.Api

# frontend — http://localhost:4200 (pare o container "web" antes, usa a mesma porta)
cd frontend && npm install && npm start
```

Testes: `cd backend && dotnet test` e `cd frontend && npm test`.

## Aviso importante

Este projeto consome uma **API não oficial** (HenrikDev) que reflete dados públicos de perfis do Valorant, sem exigir autenticação do jogador consultado. Não é afiliado, endossado ou patrocinado pela Riot Games, e não deve ser tratado como uma fundação com garantias de disponibilidade. Detalhes de uso, limites de requisição e o caminho para uma eventual migração para a API oficial (RSO) estão documentados no `CLAUDE.md`.

## Roadmap

- [x] Parsing de link e resolução de Riot ID
- [x] Integração com a HenrikDev API (MMR atual + histórico)
- [~] Persistência de snapshots de RR por jogador acompanhado (snapshot salvo a cada consulta; sem acompanhamento agendado ainda)
- [x] Motor de cálculo dos cenários de estimativa
- [ ] Definição de meta e acompanhamento diário
- [x] Frontend — layout minimalista

## Licença

A definir.
