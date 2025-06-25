# ShopSaga - E-Commerce Microservices Application

Questo progetto è un'applicazione e-commerce basata su microservizi che implementa il pattern SAGA per transazioni distribuite, sviluppato per un progetto universitario.

## Architettura

L'applicazione è composta da tre microservizi principali:

1. **OrderService**: Gestione degli ordini e orchestrazione della SAGA
2. **PaymentService**: Gestione dei pagamenti
3. **StockService**: Gestione dell'inventario e delle scorte

Ciascun microservizio segue una struttura a 5 progetti:

- WebApi: API REST esposte dal servizio
- Business: Logica di business
- ClientHttp: Client HTTP per comunicazioni sincrone
- Repository: Accesso al database tramite Entity Framework Core
- Shared: DTO e modelli condivisi

## Pattern SAGA

Il progetto implementa il pattern SAGA per gestire transazioni distribuite tra i diversi microservizi:

- OrderService avvia la saga (transazione pivot)
- PaymentService gestisce la fase di pagamento
- StockService verifica e aggiorna le scorte

Sono implementate transazioni di compensazione in caso di fallimenti per garantire la consistenza dei dati.

## Tecnologie utilizzate

- ASP.NET Core 8.0
- Entity Framework Core
- Kafka per comunicazione asincrona
- HTTP Client per comunicazione sincrona
- Docker e Docker Compose
- SQL Server

## Struttura del progetto

```plaintext
ShopSaga/
├── src/
│   ├── OrderService/
│   │   ├── ShopSaga.OrderService.WebApi/
│   │   ├── ShopSaga.OrderService.Business/
│   │   ├── ShopSaga.OrderService.ClientHttp/
│   │   ├── ShopSaga.OrderService.Repository/
│   │   └── ShopSaga.OrderService.Shared/
│   ├── PaymentService/
│   │   ├── ShopSaga.PaymentService.WebApi/
│   │   ├── ShopSaga.PaymentService.Business/
│   │   ├── ShopSaga.PaymentService.ClientHttp/
│   │   ├── ShopSaga.PaymentService.Repository/
│   │   └── ShopSaga.PaymentService.Shared/
│   └── StockService/
│       ├── ShopSaga.StockService.WebApi/
│       ├── ShopSaga.StockService.Business/
│       ├── ShopSaga.StockService.ClientHttp/
│       ├── ShopSaga.StockService.Repository/
│       └── ShopSaga.StockService.Shared/
├── docker/
│   └── docker-compose.yml
└── sql/
    └── init.sql
```

## Come eseguire l'applicazione

1. Assicurati di avere Docker e Docker Compose installati
2. Esegui il file SQL per inizializzare i database:

   ```sql
   sqlcmd -S localhost -i sql/init.sql
   ```

3. Esegui l'applicazione con Docker Compose:

   ```bash
   cd docker
   docker-compose up -d
   ```

4. Accedi alle API Swagger dei servizi:
   - OrderService: [http://localhost:5001/swagger](http://localhost:5001/swagger)
   - PaymentService: [http://localhost:5002/swagger](http://localhost:5002/swagger)
   - StockService: [http://localhost:5003/swagger](http://localhost:5003/swagger)