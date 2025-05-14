USE MagazinMercerie
GO

SELECT * FROM FirBumbac
SELECT * FROM Angajat
INSERT INTO FirBumbac(Fid, Culoare, Lungime, Grosime, Gid) VALUES
(4, 'Roz', 130, 5, 1)
INSERT INTO Angajat(Salariu, Nume) VALUES
(4000, 'Maria')

CREATE OR ALTER PROCEDURE deadlock_T1 AS
BEGIN
	BEGIN TRY
		BEGIN TRANSACTION
		UPDATE FirBumbac SET Culoare='Negru' WHERE Fid=4
		WAITFOR DELAY '00:00:08'
		UPDATE Angajat SET Nume='Mariuta' WHERE AngajatId=4
		COMMIT TRANSACTION
		PRINT 'Transaction T1 commit successful'
	END TRY
	BEGIN CATCH
		ROLLBACK TRANSACTION
		PRINT 'Transaction T1 failed'
	END CATCH
END

CREATE OR ALTER PROCEDURE deadlock_T2 AS
BEGIN
	SET DEADLOCK_PRIORITY HIGH
	BEGIN TRY
		BEGIN TRANSACTION
		UPDATE Angajat SET Nume='Anamaria' WHERE AngajatId=4
		WAITFOR DELAY '00:00:08'
		UPDATE FirBumbac SET Culoare='Alb' WHERE Fid=4
		COMMIT TRANSACTION
		PRINT 'Transaction T2 commit successful'
	END TRY
	BEGIN CATCH
		ROLLBACK TRANSACTION
		PRINT 'Transaction T2 failed'
	END CATCH
END

EXEC deadlock_T1
EXEC deadlock_T2

SELECT * FROM FirBumbac
SELECT * FROM Angajat
		