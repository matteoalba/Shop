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

-- Orders table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Orders' AND xtype='U')
BEGIN
    CREATE TABLE Orders (
        Id UNIQUEIDENTIFIER PRIMARY KEY,
        CustomerId UNIQUEIDENTIFIER NOT NULL,
        TotalAmount DECIMAL(18, 2) NOT NULL,
        Status NVARCHAR(50) NOT NULL,
        CreatedAt DATETIME2 NOT NULL,
        UpdatedAt DATETIME2 NOT NULL
    );
END
GO

-- OrderItems table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='OrderItems' AND xtype='U')
BEGIN
    CREATE TABLE OrderItems (
        Id UNIQUEIDENTIFIER PRIMARY KEY,
        OrderId UNIQUEIDENTIFIER NOT NULL,
        ProductId UNIQUEIDENTIFIER NOT NULL,
        Quantity INT NOT NULL,
        UnitPrice DECIMAL(18, 2) NOT NULL,
        CONSTRAINT FK_OrderItems_Orders FOREIGN KEY (OrderId) REFERENCES Orders(Id) ON DELETE CASCADE
    );
END
GO

-- SagaState table for tracking the SAGA state
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='SagaStates' AND xtype='U')
BEGIN
    CREATE TABLE SagaStates (
        Id UNIQUEIDENTIFIER PRIMARY KEY,
        OrderId UNIQUEIDENTIFIER NOT NULL,
        Status NVARCHAR(50) NOT NULL,
        PaymentStatus NVARCHAR(50),
        StockStatus NVARCHAR(50),
        CreatedAt DATETIME2 NOT NULL,
        UpdatedAt DATETIME2 NOT NULL
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
        Id UNIQUEIDENTIFIER PRIMARY KEY,
        OrderId UNIQUEIDENTIFIER NOT NULL,
        Amount DECIMAL(18, 2) NOT NULL,
        Status NVARCHAR(50) NOT NULL,
        PaymentMethod NVARCHAR(50) NOT NULL,
        TransactionId NVARCHAR(100),
        CreatedAt DATETIME2 NOT NULL,
        UpdatedAt DATETIME2 NOT NULL
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

-- Products table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Products' AND xtype='U')
BEGIN
    CREATE TABLE Products (
        Id UNIQUEIDENTIFIER PRIMARY KEY,
        Name NVARCHAR(200) NOT NULL,
        Description NVARCHAR(MAX),
        Price DECIMAL(18, 2) NOT NULL,
        QuantityInStock INT NOT NULL,
        CreatedAt DATETIME2 NOT NULL,
        UpdatedAt DATETIME2 NOT NULL
    );
END
GO

-- StockReservations table for tracking stock reservations during SAGA
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='StockReservations' AND xtype='U')
BEGIN
    CREATE TABLE StockReservations (
        Id UNIQUEIDENTIFIER PRIMARY KEY,
        OrderId UNIQUEIDENTIFIER NOT NULL,
        ProductId UNIQUEIDENTIFIER NOT NULL,
        Quantity INT NOT NULL,
        Status NVARCHAR(50) NOT NULL,
        CreatedAt DATETIME2 NOT NULL,
        UpdatedAt DATETIME2 NOT NULL
    );
END
GO

-- Insert sample product data
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
