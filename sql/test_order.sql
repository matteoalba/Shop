-- Insert a test order
INSERT INTO [Orders] (CustomerId, TotalAmount, Status, CreatedAt, UpdatedAt)
VALUES ('11111111-1111-1111-1111-111111111111', 125.00, 'Created', GETUTCDATE(), GETUTCDATE());

-- Seleziona l'ID dell'ultimo ordine inserito
SELECT TOP 1 Id FROM [Orders] ORDER BY Id DESC;

-- Questa query va eseguita dopo aver copiato il valore dell'ID dell'ordine dalla query precedente
-- Sostituire "1" con l'ID dell'ordine ottenuto
INSERT INTO [OrderItems] (OrderId, ProductId, Quantity, UnitPrice)
VALUES (1, '22222222-2222-2222-2222-222222222222', 2, 50.00);

-- Sostituire "1" con l'ID dell'ordine ottenuto
INSERT INTO [OrderItems] (OrderId, ProductId, Quantity, UnitPrice)
VALUES (1, '33333333-3333-3333-3333-333333333333', 1, 25.00);

-- Sostituire "1" con l'ID dell'ordine ottenuto
SELECT * FROM [Orders] WHERE Id = 1;

-- Sostituire "1" con l'ID dell'ordine ottenuto
SELECT * FROM [OrderItems] WHERE OrderId = 1;
