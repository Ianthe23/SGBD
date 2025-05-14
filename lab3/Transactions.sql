USE MagazinMercerie;
GO

--1. Tabel de logare
CREATE TABLE LoggingTable
(
	LoggingID INT IDENTITY(1,1) PRIMARY KEY,
	Operation VARCHAR(50),
	TableName VARCHAR(50),
	TimeStamp DATETIME DEFAULT GETDATE()
);
GO

--2. Functii de baza
CREATE FUNCTION dbo.IsNotNullOrEmpty(@v NVARCHAR(MAX)) RETURNS BIT AS
BEGIN
	--Removes leading and trailing spaces and checks if the result is not an empty string.
	RETURN CASE WHEN @v IS NOT NULL AND LTRIM(RTRIM(@v)) <> '' THEN 1 ELSE 0 END; 
END;
GO

--3. Functii de validare parametrilor 
CREATE OR ALTER FUNCTION dbo.ValidateAc
(
	@Marime VARCHAR(30),
	@Material VARCHAR(40),
	@MarimeGaura INT,
	@AngajatNume VARCHAR(100)
) RETURNS VARCHAR(200)
AS
BEGIN
	DECLARE @error VARCHAR(200) = '';

	IF dbo.IsNotNullOrEmpty(@Marime) = 0
		SET @error += 'Marime Ac invalida!';
	IF dbo.IsNotNullOrEmpty(@Material)  = 0
		SET @error += 'Material Ac invalid!';
	IF @MarimeGaura IS NULL OR @MarimeGaura <= 0
		SET @error += 'MarimeGaura Ac invalid!';
	IF NOT EXISTS (SELECT 1 FROM Angajat WHERE Nume = @AngajatNume)
		SET @error += 'Angajatul "' + ISNULL(@AngajatNume,'') + '"nu exista!';

	RETURN @error;
END;
GO

CREATE OR ALTER FUNCTION dbo.ValidateGhemBumbac
(
	@NrFire INT,
	@Culoare VARCHAR(30),
	@Producator VARCHAR(50),
	@Lungime INT,
	@Grosime INT,
	@CuloareRaft VARCHAR(30),
	@AngajatNume VARCHAR(100)
)
	RETURNS VARCHAR(200)
AS
BEGIN
	DECLARE @error VARCHAR(200) = '';

	IF @NrFire IS NULL OR @NrFire <= 0 OR @NrFire >= 11
		SET @error += 'NrFire GhemBumbac invalid!';
	IF dbo.IsNotNullOrEmpty(@Culoare) = 0
		SET @error += 'Culoare GhemBumbac invalida!';
	IF dbo.IsNotNullOrEmpty(@Producator) = 0
		SET @error += 'Producator GhemBumbac invalida!';
	IF @Lungime IS NULL OR @Lungime <= 0
		SET @error += 'Lungime GhemBumbac invalid!';
	IF @Grosime IS NULL OR @Grosime <= 0
		SET @error += 'Grosime GhemBumbac invalid!';
	IF dbo.IsNotNullOrEmpty(@CuloareRaft) = 0
		SET @error += 'CuloareRaft invalid!';
	IF dbo.IsNotNullOrEmpty(@AngajatNume) = 0
		SET @error += 'AngajatNume GhemBumbac invalid!';

	RETURN @error;
END;
GOa

CREATE FUNCTION dbo.ValidateUtilizareAc
(
    @Aid INT,
    @Gid INT
) RETURNS VARCHAR(200)
AS
BEGIN
    DECLARE @error VARCHAR(200) = '';
    IF NOT EXISTS(SELECT 1 FROM Ac    WHERE Aid = @Aid) SET @error += 'Ac inexistent. ';
    IF NOT EXISTS(SELECT 1 FROM GhemBumbac      WHERE Gid = @Gid) SET @error += 'GhemBumbac inexistent. ';
    IF EXISTS   (SELECT 1 FROM UtilizareAc WHERE Aid = @Aid AND Gid = @Gid)
        SET @error += 'Asocierea deja exista. ';
    RETURN @error;
END;
GO

CREATE OR ALTER PROCEDURE dbo.InsertAcAndGhemBumbac_FullRollback
(
	@Marime VARCHAR(30),
	@Material VARCHAR(40),
	@MarimeGaura INT,
	@AngajatAcNume VARCHAR(100),
	@NrFire INT,
	@Culoare VARCHAR(30),
	@Producator VARCHAR(50),
	@Lungime INT,
	@Grosime INT,
	@CuloareRaft VARCHAR(30),
	@AngajatGhemBumbacNume VARCHAR(100)
)
AS
BEGIN
	BEGIN TRAN;
	BEGIN TRY
		DECLARE
			@msg VARCHAR(200),
			@fullMsg VARCHAR(300),
			@Rid INT,
			@AngajatAcId INT,
			@AngajatGhemBumbacId INT,
			@maxIdAc INT,
			@maxIdGhemBumbac INT;

		--1. validare Ac
		SET @msg = dbo.ValidateAc(@Marime, @Material, @MarimeGaura, @AngajatAcNume);
		IF @msg <> ''
		BEGIN
			SET @fullMsg = 'Ac: ' + @msg;
			THROW 51000, @fullMsg, 1;
		END

		--2. inserare Ac
		SELECT @AngajatAcId = AngajatId FROM Angajat WHERE Nume = @AngajatAcNume;
		SELECT @maxIdAc = MAX(Aid) + 1 FROM Ac;
		PRINT @maxIdAc;
		INSERT INTO Ac(Aid, Marime, Material, MarimeGaura, AngajatId)
		VALUES (@maxIdAc, @Marime, @Material, @MarimeGaura, @AngajatAcId);
		INSERT INTO LoggingTable(Operation, TableName) VALUES('INSERT', 'Ac');

		
		--3. validare GhemBumbac
		SET @msg = dbo.ValidateGhemBumbac(@NrFire, @Culoare, @Producator, @Lungime, @Grosime, @CuloareRaft, @AngajatGhemBumbacNume);
		IF @msg <> ''
        BEGIN
            SET @fullMsg = 'GhemBumbac: ' + @msg;
            THROW 51001, @fullMsg, 1;
        END

		--4. inserare GhemBumbac
		SELECT @AngajatGhemBumbacId = AngajatId FROM Angajat WHERE Nume = @AngajatGhemBumbacNume;
		SELECT @Rid = Rid FROM Raft WHERE Culoare = @CuloareRaft
		SELECT @maxIdGhemBumbac = MAX(Gid) + 1 FROM GhemBumbac;
		PRINT @maxIdGhemBumbac;
		INSERT INTO GhemBumbac(Gid, NrFire, Culoare, Producator, Lungime, Grosime, Rid, AngajatId)
		VALUES (@maxIdGhemBumbac, @NrFire, @Culoare, @Producator, @Lungime, @Grosime, @Rid, @AngajatGhemBumbacId);
		INSERT INTO LoggingTable(Operation,TableName) VALUES('INSERT','GhemBumbac');

		--5. validare asociere M-N
		SET @msg = dbo.ValidateUtilizareAc(@maxIdAc, @maxIdGhemBumbac);
        IF @msg <> ''
        BEGIN
            SET @fullMsg = 'UtilizareAc: ' + @msg;
            THROW 51002, @fullMsg, 1;
        END

		-- 6. inserare asociere
        INSERT INTO UtilizareAc(Aid,Gid)
             VALUES(@maxIdAc, @maxIdGhemBumbac);
        INSERT INTO LoggingTable(Operation,TableName) VALUES('INSERT','UtilizareAc');

        -- 7. commit
        COMMIT TRAN;
        INSERT INTO LoggingTable(Operation,TableName) VALUES('COMMIT','Transaction');
    END TRY
    BEGIN CATCH
        ROLLBACK TRAN;
        INSERT INTO LoggingTable(Operation,TableName) VALUES('ROLLBACK','Transaction');
        THROW;  
    END CATCH
END;
GO

-- 1.1 scenariu cu succes
EXEC dbo.InsertAcAndGhemBumbac_FullRollback
	@Marime = 'medium',
	@Material = 'plastic',
	@MarimeGaura = 4,
	@AngajatAcNume = 'Andrei',
	@NrFire = 7,
	@Culoare = 'Roz',
	@Producator = 'Irika',
	@Lungime = 3,
	@Grosime = 3,
	@CuloareRaft = 'Alb',
	@AngajatGhemBumbacNume = 'Alexia';

SELECT * FROM Ac;
SELECT * FROM GhemBumbac;
SELECT * FROM UtilizareAc;
SELECT * FROM Raft;
SELECT * FROM Angajat;
SELECT * FROM LoggingTable;

-- 1.2 scenariu cu eroare la inserarea utilizarii acului
EXEC dbo.InsertAcAndGhemBumbac_FullRollback
	@Marime = 'medium',
	@Material = '',
	@MarimeGaura = 4,
	@AngajatAcNume = 'Andrei',
	@NrFire = 15,
	@Culoare = 'Roz',
	@Producator = 'Irika',
	@Lungime = 3,
	@Grosime = 3,
	@CuloareRaft = 'Alb',
	@AngajatGhemBumbacNume = 'Alexia';
-- nu se insereaza nimic


CREATE OR ALTER PROCEDURE dbo.InsertAcAndGhemBumbac_PartialRollback
(
	@Marime VARCHAR(30),
	@Material VARCHAR(40),
	@MarimeGaura INT,
	@AngajatAcNume VARCHAR(100),
	@NrFire INT,
	@Culoare VARCHAR(30),
	@Producator VARCHAR(50),
	@Lungime INT,
	@Grosime INT,
	@CuloareRaft VARCHAR(30),
	@AngajatGhemBumbacNUme VARCHAR(100)
)
AS
BEGIN
    DECLARE 
        @error       BIT          = 0,
        @msg       VARCHAR(200),
        @fullMsg   VARCHAR(300),
		@AngajatAcId INT,
		@AngajatGhemBumbacId INT,
		@Rid INT,
		@maxIdAc INT,
		@maxIdGhemBumbac INT;


    -- A. inserare Ac (tranzactie separata)
    BEGIN TRAN;
    BEGIN TRY
        SET @msg = dbo.ValidateAc(@Marime, @Material, @MarimeGaura, @AngajatAcNume);
        IF @msg <> ''
        BEGIN
            SET @fullMsg = 'Ac: ' + @msg;
            THROW 52000, @fullMsg, 1;
        END

		SELECT @AngajatAcId = AngajatId FROM Angajat WHERE Nume = @AngajatAcNume;
		SELECT @maxIdAc = MAX(Aid) + 1 FROM Ac;
        INSERT INTO Ac(Aid, Marime, Material, MarimeGaura, AngajatId)
             VALUES(@maxIdAc, @Marime, @Material, @MarimeGaura, @AngajatAcId);

        INSERT INTO LoggingTable(Operation,TableName) 
             VALUES('INSERT','Ac');

        COMMIT TRAN;
        INSERT INTO LoggingTable(Operation,TableName) 
             VALUES('COMMIT','Ac');
    END TRY
    BEGIN CATCH
        ROLLBACK TRAN;
        INSERT INTO LoggingTable(Operation,TableName) 
             VALUES('ROLLBACK','Ac');
        SET @error = 1;
		THROW;
    END CATCH; 

    -- B. inserare GhemBumbac (tranzactie separata)
    
    BEGIN TRAN;
    BEGIN TRY
        SET @msg = dbo.ValidateGhemBumbac(@NrFire, @Culoare, @Producator, @Lungime, @Grosime, @CuloareRaft, @AngajatGhemBumbacNume);
        IF @msg <> ''
        BEGIN
            SET @fullMsg = 'GhemBumbac: ' + @msg;
            THROW 52001, @fullMsg, 1;
        END

		SELECT @AngajatGhemBumbacId = AngajatId FROM Angajat WHERE Nume = @AngajatGhemBumbacNume;
		SELECT @Rid = Rid FROM Raft WHERE Culoare = @CuloareRaft
		SELECT @maxIdGhemBumbac = MAX(Gid) + 1 FROM GhemBumbac;
        INSERT INTO GhemBumbac(Gid, NrFire, Culoare, Producator, Lungime, Grosime, Rid, AngajatId)
             VALUES(@maxIdGhemBumbac, @NrFire, @Culoare, @Producator, @Lungime, @Grosime, @Rid, @AngajatGhemBumbacId);

        INSERT INTO LoggingTable(Operation,TableName) 
             VALUES('INSERT','GhemBumbac');

        COMMIT TRAN;
        INSERT INTO LoggingTable(Operation,TableName) 
             VALUES('COMMIT','GhemBumbac');
    END TRY
    BEGIN CATCH
        ROLLBACK TRAN;
        INSERT INTO LoggingTable(Operation,TableName) 
             VALUES('ROLLBACK', 'GhemBumbac');
        SET @error = 1;
		THROW;
    END CATCH;

    IF @error = 1 
        RETURN;  

    -- C. inserare asociere M–N (tranzactie separata)
    BEGIN TRAN;
    BEGIN TRY
        SET @msg = dbo.ValidateUtilizareAc(@maxIdAc, @maxIdGhemBumbac);
        IF @msg <> ''
        BEGIN
            SET @fullMsg = 'UtilizareAc ' + @msg;
            THROW 52002, @fullMsg, 1;
        END

        INSERT INTO UtilizareAc(Aid, Gid)
             VALUES(@maxIdAc, @maxIdGhemBumbac);

        INSERT INTO LoggingTable(Operation,TableName) 
             VALUES('INSERT','UtilizareAc');

        COMMIT TRAN;
        INSERT INTO LoggingTable(Operation,TableName) 
             VALUES('COMMIT','UtilizareAc');
    END TRY
    BEGIN CATCH
        ROLLBACK TRAN;
        INSERT INTO LoggingTable(Operation,TableName) 
             VALUES('ROLLBACK','UtilizareAc');
		THROW;
    END CATCH;
END;
GO

-- 2.1 succes complet
EXEC dbo.InsertAcAndGhemBumbac_PartialRollback
	@Marime = 'mare',
	@Material = 'metal',
	@MarimeGaura = 3,
	@AngajatAcNume = 'Alexia',
	@NrFire = 6,
	@Culoare = 'Galben',
	@Producator = 'Irika',
	@Lungime = 5,
	@Grosime = 3,
	@CuloareRaft = 'Alb',
	@AngajatGhemBumbacNume = 'Andrei'; 

-- 2.2 eroare la producatoru ghem bumbac (acul ramane in tabel)
EXEC dbo.InsertAcAndGhemBumbac_PartialRollback
	@Marime = 'mare',
	@Material = 'metal',
	@MarimeGaura = 3,
	@AngajatAcNume = 'Alexia',
	@NrFire = 6,
	@Culoare = 'Galben',
	@Producator = '',
	@Lungime = 5,
	@Grosime = 3,
	@CuloareRaft = 'Alb',
	@AngajatGhemBumbacNume = 'Andrei'; 

SELECT * FROM Ac;
SELECT * FROM GhemBumbac;
SELECT * FROM UtilizareAc;
SELECT * FROM Raft;
SELECT * FROM Angajat;
SELECT * FROM LoggingTable;

