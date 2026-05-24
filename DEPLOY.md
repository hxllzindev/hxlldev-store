# 📝 Instruções para publicar no GitHub

## Passo 1: Criar repositório no GitHub
1. Acesse https://github.com/new
2. Nome: `hxlldev-store` (ou outro que preferir)
3. Descrição: `Microserviços com Saga Pattern, Event Sourcing e Kafka`
4. Selecione **Public** (para portfólio)
5. **NÃO** inicialize com README (você já tem um)
6. Clique em **Create repository**

## Passo 2: Conectar e fazer push (no terminal)

Copie e execute os comandos abaixo (substitua `<seu-usuario>` pelo seu usuário do GitHub):

```bash
cd /Users/yuki/Desktop/hxlldev-store

git config user.name "Seu Nome"
git config user.email "seu-email@example.com"

git branch -M main
git remote add origin https://github.com/<seu-usuario>/hxlldev-store.git
git push -u origin main
```

## Passo 3: Verificar no GitHub

1. Acesse https://github.com/<seu-usuario>/hxlldev-store
2. Você verá:
   - Todos os arquivos do projeto
   - README.md com instruções
   - docker-compose.yml pronto para usar
   - 1 commit com histórico completo

## Bônus: Adicionar ao seu portfólio

No seu LinkedIn/CV/portfólio, inclua:

**HXLLDEV Store** — Microserviços com Saga Pattern
- Arquitetura distribuída com 3 serviços polyglot (C#, Java, Python)
- Event Sourcing + Kafka + PostgreSQL
- Orquestração de transações com padrão Saga
- Pronto para rodar com `docker-compose up`
- https://github.com/<seu-usuario>/hxlldev-store

---

**Dúvidas ou quer mais ajustes?** É só avisar! 🚀
