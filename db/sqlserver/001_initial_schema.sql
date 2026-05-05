SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.InvoiceLines', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.InvoiceLines
    (
        Id UNIQUEIDENTIFIER NOT NULL,
        InvoiceId UNIQUEIDENTIFIER NOT NULL,
        Description NVARCHAR(500) NOT NULL,
        Amount DECIMAL(18, 2) NOT NULL,
        Quantity DECIMAL(18, 4) NOT NULL,
        UnitPrice DECIMAL(18, 4) NOT NULL,

        CONSTRAINT PK_InvoiceLines PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_InvoiceLines_Amount_NonNegative CHECK (Amount >= 0),
        CONSTRAINT CK_InvoiceLines_Quantity_NonNegative CHECK (Quantity >= 0),
        CONSTRAINT CK_InvoiceLines_UnitPrice_NonNegative CHECK (UnitPrice >= 0)
    );
END;
GO

IF OBJECT_ID(N'dbo.PaymentAttempts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PaymentAttempts
    (
        Id UNIQUEIDENTIFIER NOT NULL,
        PaymentId UNIQUEIDENTIFIER NOT NULL,
        AttemptNumber INT NOT NULL,
        Status NVARCHAR(50) NOT NULL,
        FailureReason NVARCHAR(500) NULL,
        CreatedAt DATETIMEOFFSET(7) NOT NULL
            CONSTRAINT DF_PaymentAttempts_CreatedAt DEFAULT SYSDATETIMEOFFSET(),

        CONSTRAINT PK_PaymentAttempts PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_PaymentAttempts_AttemptNumber_Positive CHECK (AttemptNumber > 0),
        CONSTRAINT CK_PaymentAttempts_Status CHECK (Status IN (N'succeeded', N'failed'))
    );
END;
GO

IF OBJECT_ID(N'dbo.Payments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Payments
    (
        Id UNIQUEIDENTIFIER NOT NULL,
        InvoiceId UNIQUEIDENTIFIER NOT NULL,
        Amount DECIMAL(18, 2) NOT NULL,
        Status NVARCHAR(50) NOT NULL,
        Provider NVARCHAR(100) NOT NULL,
        IdempotencyKey NVARCHAR(200) NOT NULL,
        CreatedAt DATETIMEOFFSET(7) NOT NULL
            CONSTRAINT DF_Payments_CreatedAt DEFAULT SYSDATETIMEOFFSET(),

        CONSTRAINT PK_Payments PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_Payments_Amount_Positive CHECK (Amount > 0),
        CONSTRAINT CK_Payments_Status CHECK (Status IN (N'succeeded', N'failed'))
    );
END;
GO

IF OBJECT_ID(N'dbo.IdempotencyRecords', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.IdempotencyRecords
    (
        Id UNIQUEIDENTIFIER NOT NULL,
        [Key] NVARCHAR(200) NOT NULL,
        RequestHash NVARCHAR(128) NOT NULL,
        Status NVARCHAR(50) NOT NULL,
        StatusCode INT NULL,
        ResponsePaymentId UNIQUEIDENTIFIER NULL,
        ErrorCode NVARCHAR(100) NULL,
        ErrorMessage NVARCHAR(500) NULL,
        CreatedAt DATETIMEOFFSET(7) NOT NULL
            CONSTRAINT DF_IdempotencyRecords_CreatedAt DEFAULT SYSDATETIMEOFFSET(),
        UpdatedAt DATETIMEOFFSET(7) NOT NULL
            CONSTRAINT DF_IdempotencyRecords_UpdatedAt DEFAULT SYSDATETIMEOFFSET(),

        CONSTRAINT PK_IdempotencyRecords PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_IdempotencyRecords_Status CHECK (Status IN (N'in_progress', N'completed', N'failed'))
    );
END;
GO

IF OBJECT_ID(N'dbo.Invoices', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Invoices
    (
        Id UNIQUEIDENTIFIER NOT NULL,
        CustomerId UNIQUEIDENTIFIER NOT NULL,
        SubscriptionId UNIQUEIDENTIFIER NOT NULL,
        Status NVARCHAR(50) NOT NULL,
        AmountDue DECIMAL(18, 2) NOT NULL,
        AmountPaid DECIMAL(18, 2) NOT NULL,
        BillingPeriodStart DATE NOT NULL,
        BillingPeriodEnd DATE NOT NULL,
        DueDate DATE NOT NULL,
        CreatedAt DATETIMEOFFSET(7) NOT NULL
            CONSTRAINT DF_Invoices_CreatedAt DEFAULT SYSDATETIMEOFFSET(),

        CONSTRAINT PK_Invoices PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_Invoices_Status CHECK (Status IN (N'open', N'paid', N'void')),
        CONSTRAINT CK_Invoices_AmountDue_NonNegative CHECK (AmountDue >= 0),
        CONSTRAINT CK_Invoices_AmountPaid_NonNegative CHECK (AmountPaid >= 0),
        CONSTRAINT CK_Invoices_BillingPeriod CHECK (BillingPeriodEnd > BillingPeriodStart)
    );
END;
GO

IF OBJECT_ID(N'dbo.Subscriptions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Subscriptions
    (
        Id UNIQUEIDENTIFIER NOT NULL,
        CustomerId UNIQUEIDENTIFIER NOT NULL,
        PricePlanId UNIQUEIDENTIFIER NOT NULL,
        Status NVARCHAR(50) NOT NULL,
        StartDate DATE NOT NULL,
        CurrentPeriodStart DATE NOT NULL,
        CurrentPeriodEnd DATE NOT NULL,
        CancelAtPeriodEnd BIT NOT NULL
            CONSTRAINT DF_Subscriptions_CancelAtPeriodEnd DEFAULT 0,

        CONSTRAINT PK_Subscriptions PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_Subscriptions_Status CHECK (Status IN (N'active', N'past_due', N'canceled')),
        CONSTRAINT CK_Subscriptions_CurrentPeriod CHECK (CurrentPeriodEnd > CurrentPeriodStart)
    );
END;
GO

IF OBJECT_ID(N'dbo.PricePlans', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PricePlans
    (
        Id UNIQUEIDENTIFIER NOT NULL,
        ProductId UNIQUEIDENTIFIER NOT NULL,
        BillingType NVARCHAR(50) NOT NULL,
        Amount DECIMAL(18, 2) NOT NULL,
        Currency NVARCHAR(3) NOT NULL,
        BillingInterval NVARCHAR(20) NOT NULL,
        UsageUnit NVARCHAR(100) NULL,

        CONSTRAINT PK_PricePlans PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_PricePlans_BillingType CHECK (BillingType IN (N'recurring', N'usage')),
        CONSTRAINT CK_PricePlans_Amount_NonNegative CHECK (Amount >= 0),
        CONSTRAINT CK_PricePlans_BillingInterval CHECK (BillingInterval IN (N'month', N'year')),
        CONSTRAINT CK_PricePlans_Currency_Length CHECK (LEN(Currency) = 3)
    );
END;
GO

IF OBJECT_ID(N'dbo.Products', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Products
    (
        Id UNIQUEIDENTIFIER NOT NULL,
        Name NVARCHAR(200) NOT NULL,
        Description NVARCHAR(500) NOT NULL,
        Active BIT NOT NULL
            CONSTRAINT DF_Products_Active DEFAULT 1,

        CONSTRAINT PK_Products PRIMARY KEY CLUSTERED (Id)
    );
END;
GO

IF OBJECT_ID(N'dbo.Customers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Customers
    (
        Id UNIQUEIDENTIFIER NOT NULL,
        Email NVARCHAR(320) NOT NULL,
        CompanyName NVARCHAR(200) NOT NULL,
        BillingAddress NVARCHAR(500) NOT NULL,
        PaymentMethodId NVARCHAR(100) NOT NULL,
        Status NVARCHAR(50) NOT NULL,
        CreatedAt DATETIMEOFFSET(7) NOT NULL
            CONSTRAINT DF_Customers_CreatedAt DEFAULT SYSDATETIMEOFFSET(),

        CONSTRAINT PK_Customers PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_Customers_Status CHECK (Status IN (N'active', N'inactive'))
    );
END;
GO

IF OBJECT_ID(N'dbo.FK_PricePlans_Products_ProductId', N'F') IS NULL
BEGIN
    ALTER TABLE dbo.PricePlans
    ADD CONSTRAINT FK_PricePlans_Products_ProductId
        FOREIGN KEY (ProductId) REFERENCES dbo.Products (Id);
END;
GO

IF OBJECT_ID(N'dbo.FK_Subscriptions_Customers_CustomerId', N'F') IS NULL
BEGIN
    ALTER TABLE dbo.Subscriptions
    ADD CONSTRAINT FK_Subscriptions_Customers_CustomerId
        FOREIGN KEY (CustomerId) REFERENCES dbo.Customers (Id);
END;
GO

IF OBJECT_ID(N'dbo.FK_Subscriptions_PricePlans_PricePlanId', N'F') IS NULL
BEGIN
    ALTER TABLE dbo.Subscriptions
    ADD CONSTRAINT FK_Subscriptions_PricePlans_PricePlanId
        FOREIGN KEY (PricePlanId) REFERENCES dbo.PricePlans (Id);
END;
GO

IF OBJECT_ID(N'dbo.FK_Invoices_Customers_CustomerId', N'F') IS NULL
BEGIN
    ALTER TABLE dbo.Invoices
    ADD CONSTRAINT FK_Invoices_Customers_CustomerId
        FOREIGN KEY (CustomerId) REFERENCES dbo.Customers (Id);
END;
GO

IF OBJECT_ID(N'dbo.FK_Invoices_Subscriptions_SubscriptionId', N'F') IS NULL
BEGIN
    ALTER TABLE dbo.Invoices
    ADD CONSTRAINT FK_Invoices_Subscriptions_SubscriptionId
        FOREIGN KEY (SubscriptionId) REFERENCES dbo.Subscriptions (Id);
END;
GO

IF OBJECT_ID(N'dbo.FK_InvoiceLines_Invoices_InvoiceId', N'F') IS NULL
BEGIN
    ALTER TABLE dbo.InvoiceLines
    ADD CONSTRAINT FK_InvoiceLines_Invoices_InvoiceId
        FOREIGN KEY (InvoiceId) REFERENCES dbo.Invoices (Id);
END;
GO

IF OBJECT_ID(N'dbo.FK_Payments_Invoices_InvoiceId', N'F') IS NULL
BEGIN
    ALTER TABLE dbo.Payments
    ADD CONSTRAINT FK_Payments_Invoices_InvoiceId
        FOREIGN KEY (InvoiceId) REFERENCES dbo.Invoices (Id);
END;
GO

IF OBJECT_ID(N'dbo.FK_PaymentAttempts_Payments_PaymentId', N'F') IS NULL
BEGIN
    ALTER TABLE dbo.PaymentAttempts
    ADD CONSTRAINT FK_PaymentAttempts_Payments_PaymentId
        FOREIGN KEY (PaymentId) REFERENCES dbo.Payments (Id);
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Subscriptions_CustomerId'
        AND object_id = OBJECT_ID(N'dbo.Subscriptions')
)
BEGIN
    CREATE INDEX IX_Subscriptions_CustomerId
        ON dbo.Subscriptions (CustomerId);
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Payments_InvoiceId_Succeeded'
        AND object_id = OBJECT_ID(N'dbo.Payments')
)
BEGIN
    CREATE UNIQUE INDEX IX_Payments_InvoiceId_Succeeded
        ON dbo.Payments (InvoiceId)
        WHERE Status = N'succeeded';
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Payments_InvoiceId'
        AND object_id = OBJECT_ID(N'dbo.Payments')
)
BEGIN
    CREATE INDEX IX_Payments_InvoiceId
        ON dbo.Payments (InvoiceId);
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Payments_IdempotencyKey'
        AND object_id = OBJECT_ID(N'dbo.Payments')
)
BEGIN
    CREATE UNIQUE INDEX IX_Payments_IdempotencyKey
        ON dbo.Payments (IdempotencyKey);
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_PaymentAttempts_PaymentId_AttemptNumber'
        AND object_id = OBJECT_ID(N'dbo.PaymentAttempts')
)
BEGIN
    CREATE UNIQUE INDEX IX_PaymentAttempts_PaymentId_AttemptNumber
        ON dbo.PaymentAttempts (PaymentId, AttemptNumber);
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_IdempotencyRecords_Key'
        AND object_id = OBJECT_ID(N'dbo.IdempotencyRecords')
)
BEGIN
    CREATE UNIQUE INDEX IX_IdempotencyRecords_Key
        ON dbo.IdempotencyRecords ([Key]);
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Invoices_Status'
        AND object_id = OBJECT_ID(N'dbo.Invoices')
)
BEGIN
    CREATE INDEX IX_Invoices_Status
        ON dbo.Invoices (Status);
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Invoices_SubscriptionId_BillingPeriod'
        AND object_id = OBJECT_ID(N'dbo.Invoices')
)
BEGIN
    CREATE UNIQUE INDEX IX_Invoices_SubscriptionId_BillingPeriod
        ON dbo.Invoices (SubscriptionId, BillingPeriodStart, BillingPeriodEnd);
END;
GO
