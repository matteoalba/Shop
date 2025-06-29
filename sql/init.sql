-- OrderDb
USE master;
GO

IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'OrderDb')
BEGIN
    CREATE DATABASE OrderDb;
END
GO

USE OrderDb;
GO

-- Tabella Orders
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Orders' AND xtype='U')
BEGIN
    CREATE TABLE Orders (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CustomerId UNIQUEIDENTIFIER NOT NULL,
        TotalAmount DECIMAL(18, 2) NOT NULL CHECK (TotalAmount >= 0),
        Status NVARCHAR(50) NOT NULL DEFAULT 'Pending',
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );
END
GO

-- Tabella OrderItems
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='OrderItems' AND xtype='U')
BEGIN
    CREATE TABLE OrderItems (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        OrderId INT NOT NULL,
        ProductId UNIQUEIDENTIFIER NOT NULL,
        Quantity INT NOT NULL CHECK (Quantity > 0),
        UnitPrice DECIMAL(18, 2) NOT NULL CHECK (UnitPrice >= 0),
        CONSTRAINT FK_OrderItems_Orders FOREIGN KEY (OrderId) REFERENCES Orders(Id) ON DELETE CASCADE
    );
END
GO

-- PaymentDb
USE master;
GO

IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'PaymentDb')
BEGIN
    CREATE DATABASE PaymentDb;
END
GO

USE PaymentDb;
GO

-- Payments table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Payments' AND xtype='U')
BEGIN
    CREATE TABLE Payments (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        OrderId INT NOT NULL,
        Amount DECIMAL(18, 2) NOT NULL CHECK (Amount <> 0),
        Status NVARCHAR(50) NOT NULL DEFAULT 'Pending',
        PaymentMethod NVARCHAR(50) NOT NULL,
        TransactionId NVARCHAR(100),
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );
END
GO

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='PaymentRefund' AND xtype='U')
BEGIN
    CREATE TABLE PaymentRefund (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        PaymentId INT NOT NULL,
        Amount DECIMAL(18, 2) NOT NULL CHECK (Amount > 0),
        Reason NVARCHAR(255) NOT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_PaymentRefund_Payments FOREIGN KEY (PaymentId) REFERENCES Payments(Id) ON DELETE CASCADE
    );
END
GO

-- StockDb
USE master;
GO

IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'StockDb')
BEGIN
    CREATE DATABASE StockDb;
END
GO

USE StockDb;
GO

-- Tabella prodotti
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Products' AND xtype='U')
BEGIN
    CREATE TABLE Products (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        Name NVARCHAR(200) NOT NULL,
        Description NVARCHAR(MAX),
        Price DECIMAL(18, 2) NOT NULL CHECK (Price >= 0),
        QuantityInStock INT NOT NULL DEFAULT 0 CHECK (QuantityInStock >= 0),
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );
END
GO

-- Tabella stock reservations per gestire le prenotazioni di stock durante le SAGA
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='StockReservations' AND xtype='U')
BEGIN
    CREATE TABLE StockReservations (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        OrderId INT NOT NULL,
        ProductId UNIQUEIDENTIFIER NOT NULL,
        Quantity INT NOT NULL CHECK (Quantity > 0),
        Status NVARCHAR(50) NOT NULL DEFAULT 'Reserved',
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_StockReservations_Products FOREIGN KEY (ProductId) REFERENCES Products(Id) ON DELETE CASCADE
    );
END
GO
-- Popolamento iniziale casuale
IF NOT EXISTS (SELECT TOP 1 1 FROM Products)
BEGIN
    INSERT INTO Products (Id, Name, Description, Price, QuantityInStock, CreatedAt, UpdatedAt)
    VALUES
        (NEWID(), 'Laptop', 'High performance laptop', 1299.99, 50, GETDATE(), GETDATE()),
        (NEWID(), 'Smartphone', 'Latest model smartphone', 799.99, 100, GETDATE(), GETDATE()),
        (NEWID(), 'Tablet', '10-inch tablet', 499.99, 75, GETDATE(), GETDATE()),
        (NEWID(), 'Headphones', 'Noise-cancelling headphones', 199.99, 150, GETDATE(), GETDATE()),
        (NEWID(), 'Smart Watch', 'Fitness tracking watch', 249.99, 80, GETDATE(), GETDATE());
END
GO

-- Indici
CREATE NONCLUSTERED INDEX IX_Orders_CustomerId ON Orders(CustomerId);
CREATE NONCLUSTERED INDEX IX_Orders_Status ON Orders(Status);
CREATE NONCLUSTERED INDEX IX_Orders_CreatedAt ON Orders(CreatedAt);

CREATE NONCLUSTERED INDEX IX_OrderItems_OrderId ON OrderItems(OrderId);
CREATE NONCLUSTERED INDEX IX_OrderItems_ProductId ON OrderItems(ProductId);

CREATE NONCLUSTERED INDEX IX_Payments_OrderId ON Payments(OrderId);
CREATE NONCLUSTERED INDEX IX_Payments_Status ON Payments(Status);
CREATE NONCLUSTERED INDEX IX_Payments_TransactionId ON Payments(TransactionId);
CREATE NONCLUSTERED INDEX IX_Payments_CreatedAt ON Payments(CreatedAt);

CREATE NONCLUSTERED INDEX IX_PaymentRefund_PaymentId ON PaymentRefund(PaymentId);

CREATE NONCLUSTERED INDEX IX_Products_Name ON Products(Name);
CREATE NONCLUSTERED INDEX IX_Products_Price ON Products(Price);

CREATE NONCLUSTERED INDEX IX_StockReservations_OrderId ON StockReservations(OrderId);
CREATE NONCLUSTERED INDEX IX_StockReservations_ProductId ON StockReservations(ProductId);
CREATE NONCLUSTERED INDEX IX_StockReservations_Status ON StockReservations(Status);
GO
