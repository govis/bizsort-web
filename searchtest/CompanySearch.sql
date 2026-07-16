

-- Batch submitted through debugger: SQLQuery3.sql|7|0|C:\Users\horyacv\AppData\Local\Temp\~vsDA0E.sql

-- Modified	Feb 16	by	V

CREATE PROCEDURE [dbo].[CompanySearch] 

	@TransactionType SMALLINT = 0,

	@Category SMALLINT = 0,

	@Query NVARCHAR(4000) = NULL, --'test'

	@Location INT = 0,

	--@GeoLocation GEOGRAPHY = NULL, --GEOGRAPHY::STPointFromText('POINT(-79.41597 43.7802124)', 4326) 

	@Lattitude FLOAT = 0,

	@Longitude FLOAT = 0,

	@Distance REAL = 100,

	@InclFacets INT = 0,

	@InclFacetValues VARBINARY(40) = 0x00,

	@ExclFacets INT = 0,

	@ExclFacetValues VARBINARY(40) = 0x00,

	@StartIndex INT = 0,

	@Length INT = 0 OUTPUT	

AS

BEGIN



--Dynamic Search Conditions in T-SQL

--http://www.sommarskog.se/dyn-search.html



	SET NOCOUNT ON;



	DECLARE @TextQuery BIT = 1;



	--Workaround for Null or empty full-text predicate error

	--http://stackoverflow.com/questions/189765/7645-null-or-empty-full-text-predicate

	--http://social.msdn.microsoft.com/forums/en-US/sqldatabaseengine/thread/8008463e-d44f-4afd-9bec-706851cff5b3

	IF ISNULL(@Query,'') = ''

		SELECT @Query = '""', @TextQuery = 0



	DECLARE @GeoLocation GEOGRAPHY

	SELECT @GeoLocation = CASE WHEN @Lattitude <> 0 AND @Longitude <> 0 THEN GEOGRAPHY::Point(@Lattitude, @Longitude, 4326) ELSE NULL END



	DECLARE @RecCount INT

	DECLARE @Companies TABLE(Id INT NOT NULL, Office INT NOT NULL, Distance REAL, SortOrder FLOAT /*INDEX IX_SortOrder CLUSTERED*/)

	DECLARE @Facets TABLE([Name] SMALLINT NOT NULL, [Value] INT NOT NULL, [Count] INT NOT NULL)



	DECLARE @FacetsTmp TABLE(Company INT NOT NULL, Excluded BIT NOT NULL, [Count] SMALLINT NOT NULL)

	IF @InclFacets > 0 OR @ExclFacets > 0

	BEGIN

		INSERT INTO @FacetsTmp

		SELECT Company, 0 AS Excluded, COUNT(*) AS [Count] FROM CompanyFacets BFt WITH(NOLOCK)

		INNER JOIN (SELECT [Value] = CONVERT(INT, SUBSTRING(@InclFacetValues, 4 * ([Util_Sequence].Number - 1) + 1, 4))

					FROM [Util_Sequence]

					WHERE [Util_Sequence].Number <= @InclFacets

					) AS Ft ON BFt.FacetValue=Ft.[Value]

					GROUP BY BFt.Company

		UNION SELECT Company, 1 AS Excluded, COUNT(*) AS [Count] FROM CompanyFacets AS BFt WITH(NOLOCK)

		INNER JOIN (SELECT [Value] = CONVERT(INT, SUBSTRING(@ExclFacetValues, 4 * ([Util_Sequence].Number - 1) + 1, 4))

					FROM [Util_Sequence]

					WHERE [Util_Sequence].Number <= @ExclFacets

					) AS Ft ON BFt.FacetValue=Ft.[Value]

					GROUP BY BFt.Company

	END



	DECLARE @SortColumn INT

	SELECT @SortColumn = CASE 

		WHEN @TextQuery = 1 THEN 1/*[Rank]*/

		WHEN @GeoLocation IS NULL THEN 2/*Created*/

		ELSE 3/*Distance*/ END;



	--http://siderite.blogspot.com/2015/08/how-to-translate-t-sql-datetime2-to.html

	;WITH Company_CTE AS

	(	

		SELECT CP.Id, CO.Id AS Office, CO.Distance,

		COALESCE(CPt.[Rank], 0) * 3 + COALESCE(P.[ProductRank], 0) AS [Rank], CP.Created

		FROM CompanyProfiles AS CP WITH(NOLOCK)

		INNER JOIN Accounts AS A WITH(NOLOCK) ON CP.Id = A.Id

		--https://www.youtube.com/watch?v=-m426WYclz8&feature=youtu.be

		--15% lesser query cost

		INNER JOIN (SELECT Company, CASE WHEN [Order] = 0 THEN 0 ELSE Id END AS Id, Distance,

			ROW_NUMBER() OVER(PARTITION BY Company ORDER BY Company, CASE WHEN @GeoLocation IS NULL THEN [Order]

			ELSE Distance END) AS RowNum

			FROM CompanyOffices WITH(NOLOCK)

			CROSS APPLY

			(VALUES(CASE WHEN @GeoLocation IS NULL THEN 0 WHEN GeoLocation IS NULL THEN 999999999 ELSE ROUND(GeoLocation.STDistance(@GeoLocation) / 1000, 1) END)) AS a1(Distance)

			WHERE @Location = 0 OR [Location] = @Location OR 

			EXISTS (SELECT NULL FROM Locations_Unwound L WHERE L.Parent = @Location AND [Location] = L.Child)) AS CO ON CP.Id = CO.Company AND CO.RowNum = 1

		/*CROSS APPLY (SELECT TOP 1 CO.Id, Distance

			FROM CompanyOffices AS CO WITH(NOLOCK)

			CROSS APPLY

			(VALUES(CASE WHEN @GeoLocation IS NULL THEN 0 WHEN CO.GeoLocation IS NULL THEN 999999999 ELSE ROUND(CO.GeoLocation.STDistance(@GeoLocation) / 1000, 1) END)) AS a1(Distance)

			WHERE CO.Company = CP.Id AND (@Location = 0 OR CO.[Location] = @Location OR 

			EXISTS (SELECT NULL FROM Locations_Unwound L WHERE L.Parent = @Location AND CO.[Location] = L.Child))

			--http://stackoverflow.com/questions/6001197/optimizing-sql-queries-by-removing-sort-operator-in-execution-plan

			ORDER BY CASE WHEN @GeoLocation IS NULL THEN CO.[Order]

			ELSE Distance/*1*/ END) AS CO*/

		LEFT JOIN @FacetsTmp AS CFi ON CP.Id = CFi.Company AND CFi.Excluded = 0

		LEFT JOIN @FacetsTmp AS CFe ON CP.Id = CFe.Company AND CFe.Excluded = 1

		LEFT JOIN CompanyTextSearch2(@Query) AS CPt ON @TextQuery = 1 AND CP.Id = CPt.[KEY]

		LEFT JOIN (SELECT CPt.Company, MAX(COALESCE(Pt.[Rank], 0)) AS ProductRank 

			FROM CompanyProducts AS CPt WITH(NOLOCK)

			LEFT JOIN Products AS P WITH(NOLOCK) ON CPt.Product = P.Id

			LEFT JOIN ProductTextSearch2(@Query) AS Pt ON @TextQuery = 1 AND CPt.Product = Pt.[KEY]

			WHERE (P.[Type] = 0/*Multiproduct*/ OR (CPt.[UnlistedType] = 0/*Listed*/ AND P.[Status] = 1/*Active*/)) AND

			(@TextQuery = 0 OR Pt.[KEY] IS NOT NULL) AND

			(@Category = 0 OR P.[Type] = 0/*Multiproduct*/ OR CPt.Category = @Category OR

			EXISTS (SELECT NULL FROM Categories_Unwound C WHERE C.Parent = @Category AND CPt.Category = C.Child))

			GROUP BY CPt.Company

		) AS P ON CP.Id = P.Company

		--AdScrl.Data.CompanyProfile.GetActive

		WHERE A.[Status] = 2/*Active*/ AND

		(@TransactionType = 0 OR (CP.TransactionType & @TransactionType) > 0) AND

		(((@TextQuery = 0 OR CPt.[KEY] IS NOT NULL) AND

		(@Category = 0 OR CP.Category = @Category OR 

		EXISTS (SELECT NULL FROM Categories_Unwound C WHERE C.Parent = @Category AND CP.Category = C.Child))) OR 

		P.Company IS NOT NULL) AND

		(@InclFacets = 0 OR COALESCE(CFi.[Count], 0) = @InclFacets) AND

		(@ExclFacets = 0 OR COALESCE(CFe.[Count], 0) = 0)

	)

	INSERT INTO @Companies SELECT Id, Office, Distance, CHOOSE(@SortColumn, [Rank], 25567+(DATEDIFF(SECOND,{d '1970-01-01'}, Created)+DATEPART(NANOSECOND,Created)/1.0E+9)/86400.0, 1 / CASE WHEN Distance = 0 THEN 0.0000000001 ELSE Distance END) AS SortOrder

	FROM Company_CTE

	WHERE (@GeoLocation IS NULL OR Distance <= @Distance)

	OPTION (RECOMPILE)

	

	--https://www.microsoftpressstore.com/articles/article.aspx?p=2314819

	--http://dba.stackexchange.com/questions/90593/find-out-beforehand-how-many-records-a-query-has/90599#90599

	--http://stackoverflow.com/questions/27070104/tsql-is-there-a-way-to-limit-the-rows-returned-and-count-the-total-that-would-h/27074033#27074033

	IF @StartIndex = 0 AND @Length = 0

		BEGIN

			SELECT Id, Office, Distance FROM @Companies

			ORDER BY SortOrder DESC

			SELECT @Length = @@ROWCOUNT

		END

	ELSE IF @StartIndex = 0 AND @Length > 0

		BEGIN

			/*SELECT Id, Office, Distance FROM (SELECT ROW_NUMBER() OVER 

			(

				ORDER BY SortOrder DESC

			) AS RowNumber, Id, Office, Distance FROM @Companies) AS CP

			WHERE CP.RowNumber <= @Length*/

			SELECT TOP(@Length) Id, Office, Distance FROM @Companies

			ORDER BY SortOrder DESC



			SELECT @RecCount = @@ROWCOUNT

			IF (@RecCount < @Length)

				SELECT @Length = @RecCount

			ELSE

				SELECT @Length = COUNT(*) FROM @Companies

		END

	ELSE IF @StartIndex > 0 AND @Length > 0

		BEGIN

			/*SELECT Id, Office, Distance FROM (SELECT ROW_NUMBER() OVER 

			(

				ORDER BY SortOrder DESC

			) AS RowNumber, Id, Office, Distance FROM @Companies) AS CP

			WHERE CP.RowNumber BETWEEN (@StartIndex + 1) AND (@StartIndex + @Length)*/



			SELECT Id, Office, Distance FROM @Companies

			ORDER BY SortOrder DESC

			OFFSET @StartIndex ROWS FETCH NEXT @Length ROWS ONLY

		END

	ELSE

		RAISERROR('Unexpected StartIndex and/or Length', 16, 1);

		

	IF @StartIndex = 0

		BEGIN

			INSERT INTO @Facets

			SELECT CFV.[Name], CFV.Id AS [Value], COUNT(*) AS [Count] FROM @Companies AS C

			INNER JOIN CompanyFacets AS CF ON C.Id = CF.Company

			INNER JOIN CompanyFacetValues AS CFV ON CF.FacetValue = CFV.Id

			GROUP BY CFV.[Name], CFV.Id

			HAVING COUNT(*) < @Length

			SELECT [Name], [Value], [Count] FROM @Facets	

		END			

END



