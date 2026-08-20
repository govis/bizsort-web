-- Batch submitted through debugger: SQLQuery1.sql|7|0|C:\Documents and Settings\horyacv\Local Settings\Temp\~vs9.sql
                                                                                                                                        
-- Modified	Feb 16	by	V
                                                                                                                                                                                                                                      
CREATE OR ALTER PROCEDURE [dbo].[OfferingSearch] 
                                                                                                                                                                                                                      
	@OfferingType SMALLINT = 0,
                                                                                                                                                                                                                                  
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
                                                                                                                                                                                                                       

                                                                                                                                                                                                                                                             
	--SQL Server 2008 Proximity Search With The Geography Data Type
                                                                                                                                                                                             
	--http://blogs.lessthandot.com/index.php/DataMgmt/DataDesign/sql-server-2008-proximity-search-with-th
                                                                                                                                                       
	
                                                                                                                                                                                                                                                            
	DECLARE @GeoLocation GEOGRAPHY
                                                                                                                                                                                                                              
	SELECT @GeoLocation = CASE WHEN @Lattitude <> 0 AND @Longitude <> 0 THEN GEOGRAPHY::Point(@Lattitude, @Longitude, 4326) ELSE NULL END
                                                                                                                       

                                                                                                                                                                                                                                                             
	DECLARE @RecCount INT
                                                                                                                                                                                                                                       
	DECLARE @Offerings TABLE(Id BIGINT NOT NULL, Distance REAL, SortOrder FLOAT)
                                                                                                                                                                                 
	DECLARE @Facets TABLE([Name] SMALLINT NOT NULL, [Value] INT NOT NULL, [Count] INT NOT NULL)
                                                                                                                                                                 
	DECLARE @SortColumn INT
                                                                                                                                                                                                                                     
	SELECT @SortColumn = CASE 
                                                                                                                                                                                                                                  
		WHEN @TextQuery = 1 THEN 1/*[Rank]*/
                                                                                                                                                                                                                       
		WHEN @GeoLocation IS NULL THEN 2/*Created*/
                                                                                                                                                                                                                
		ELSE 3/*Distance*/ END;
                                                                                                                                                                                                                                    

                                                                                                                                                                                                                                                             
	DECLARE @FacetsTmp TABLE(Offering BIGINT, Excluded BIT NOT NULL, [Count] SMALLINT NOT NULL)
                                                                                                                                                                  
	IF @InclFacets > 0 OR @ExclFacets > 0
                                                                                                                                                                                                                       
	BEGIN
                                                                                                                                                                                                                                                       
		INSERT INTO @FacetsTmp
                                                                                                                                                                                                                                     
		SELECT Offering, 0 AS Excluded, COUNT(*) AS [Count] FROM CompanyOfferingFacets PFt WITH(NOLOCK)
                                                                                                                                                              
		INNER JOIN (SELECT [Value] = CONVERT(INT, SUBSTRING(@InclFacetValues, 4 * ([Util_Sequence].Number - 1) + 1, 4))
                                                                                                                                            
					FROM [Util_Sequence]
                                                                                                                                                                                                                                    
					WHERE [Util_Sequence].Number <= @InclFacets
                                                                                                                                                                                                             
					) AS Ft ON PFt.FacetValue=Ft.[Value]
                                                                                                                                                                                                                    
					GROUP BY PFt.Offering
                                                                                                                                                                                                                                    
		UNION SELECT Offering, 1 AS Excluded, COUNT(*) AS [Count] FROM CompanyOfferingFacets AS PFt WITH(NOLOCK)
                                                                                                                                                     
		INNER JOIN (SELECT [Value] = CONVERT(INT, SUBSTRING(@ExclFacetValues, 4 * ([Util_Sequence].Number - 1) + 1, 4))
                                                                                                                                            
					FROM [Util_Sequence]
                                                                                                                                                                                                                                    
					WHERE [Util_Sequence].Number <= @ExclFacets
                                                                                                                                                                                                             
					) AS Ft ON PFt.FacetValue=Ft.[Value]
                                                                                                                                                                                                                    
					GROUP BY PFt.Offering
                                                                                                                                                                                                                                    
	END
                                                                                                                                                                                                                                                         
	
                                                                                                                                                                                                                                                            
	----http://siderite.blogspot.com/2015/08/how-to-translate-t-sql-datetime2-to.html
                                                                                                                                                                           
	INSERT INTO @Offerings SELECT Id, Distance, CHOOSE(@SortColumn, [Rank], 25567+(DATEDIFF(SECOND,{d '1970-01-01'}, Created)+DATEPART(NANOSECOND,Created)/1.0E+9)/86400.0, 1 / CASE WHEN Distance = 0 THEN 0.0000000001 ELSE Distance END) AS SortOrder
         
	FROM (
                                                                                                                                                                                                                                                      
		SELECT P.Id, /*CASE WHEN P.GeoLocation IS NULL THEN*/ COALESCE(CO.Distance, 999999999) /*ELSE a1.Distance END*/ AS Distance, 
                                                                                                                              
		COALESCE(Pt.[Rank], 0) AS [Rank], P.Created
                                                                                                                                                                                                                
		FROM Offerings AS P WITH(NOLOCK)
                                                                                                                                                                                                                            
		/*CROSS APPLY
                                                                                                                                                                                                                                              
		(VALUES(CASE WHEN @GeoLocation IS NULL THEN 0 WHEN P.GeoLocation IS NULL THEN 999999999 ELSE ROUND(P.GeoLocation.STDistance(@GeoLocation) / 1000, 1) END)) AS a1(Distance)*/
                                                                               
		INNER JOIN Accounts AS A WITH(NOLOCK) ON P.CreatedBy = A.Id
                                                                                                                                                                                                
		INNER/*LEFT*/ JOIN CompanyOfferings AS CP ON /*P.[Location] IS NULL AND*/ P.Id = CP.Offering
                                                                                                                                                                 
		--https://www.youtube.com/watch?v=-m426WYclz8&feature=youtu.be
                                                                                                                                                                                             
		/*LEFT JOIN (SELECT Company, Id, Location, Distance,
                                                                                                                                                                                                       
			ROW_NUMBER() OVER(PARTITION BY Company ORDER BY Company, CASE WHEN @GeoLocation IS NULL THEN [Order]
                                                                                                                                                      
			ELSE Distance END) AS RowNum
                                                                                                                                                                                                                              
			FROM CompanyOffices WITH(NOLOCK)
                                                                                                                                                                                                                          
			CROSS APPLY
                                                                                                                                                                                                                                               
			(VALUES(CASE WHEN @GeoLocation IS NULL THEN 0 WHEN GeoLocation IS NULL THEN 999999999 ELSE ROUND(GeoLocation.STDistance(@GeoLocation) / 1000, 1) END)) AS a2(Distance)
                                                                                    
			WHERE @Location = 0 OR Location = @Location OR 
                                                                                                                                                                                                           
			EXISTS (SELECT NULL FROM Locations_Unwound L WHERE L.Parent = @Location AND Location = L.Child)) AS CO ON BP.Company = CO.Company AND CO.RowNum = 1*/
                                                                                                     
		--10% lesser query cost
                                                                                                                                                                                                                                    
		OUTER APPLY (SELECT TOP 1 CO.Id, CO.[Location], Distance
                                                                                                                                                                                                   
			FROM CompanyOffices AS CO WITH(NOLOCK)
                                                                                                                                                                                                                    
			CROSS APPLY
                                                                                                                                                                                                                                               
			(VALUES(CASE WHEN @GeoLocation IS NULL THEN 0 WHEN CO.GeoLocation IS NULL THEN 999999999 ELSE ROUND(CO.GeoLocation.STDistance(@GeoLocation) / 1000, 1) END)) AS a2(Distance)
                                                                              
			WHERE CO.Company = CP.Company AND (@Location = 0 OR CO.[Location] = @Location OR 
                                                                                                                                                                         
			EXISTS (SELECT NULL FROM Locations_Unwound L WHERE L.Parent = @Location AND L.Child = CO.[Location]))
                                                                                                                                                     
			--http://stackoverflow.com/questions/6001197/optimizing-sql-queries-by-removing-sort-operator-in-execution-plan
                                                                                                                                           
			ORDER BY CASE WHEN @GeoLocation IS NULL THEN CO.[Order]
                                                                                                                                                                                                   
			ELSE Distance/*1*/ END) AS CO
                                                                                                                                                                                                                             
		LEFT JOIN OfferingTextSearch2(@Query) AS Pt ON @TextQuery = 1 AND P.Id = Pt.[KEY]
                                                                                                                                                                           
		LEFT JOIN @FacetsTmp AS PFi ON P.Id = PFi.Offering AND PFi.Excluded = 0
                                                                                                                                                                                     
		LEFT JOIN @FacetsTmp AS PFe ON P.Id = PFe.Offering AND PFe.Excluded = 1
                                                                                                                                                                                     
		--Data.Entity.Offering.GetActive
                                                                                                                                                                                                                            
		WHERE P.[Type] != 0/*Multiproduct*/ AND CP.[UnlistedType] = 0/*Listed*/ AND P.[Status] = 1/*Active*/ AND
                                                                                                                                                   
		(@OfferingType = 0 OR (P.[Type] & @OfferingType) > 0) AND
                                                                                                                                                                                                    
		(@TextQuery = 0 OR Pt.[KEY] IS NOT NULL) AND
                                                                                                                                                                                                               
		(@Category = 0 OR CP.Category = @Category OR 
                                                                                                                                                                                                              
		EXISTS (SELECT NULL FROM Categories_Unwound C WHERE C.Parent = @Category AND CP.Category = C.Child)) AND
                                                                                                                                                   
		(@Location = 0 OR CO.[Location] IS NOT NULL /*OR
                                                                                                                                                                                                           
		EXISTS
                                                                                                                                                                                                                                                     
		(
                                                                                                                                                                                                                                                          
			SELECT NULL FROM OfferingLocations AS PL WITH(NOLOCK)
                                                                                                                                                                                                      
			WHERE PL.Offering = P.Id AND (PL.[Location] = 0 OR PL.[Location] = @Location OR 
                                                                                                                                                                           
			EXISTS (SELECT NULL FROM Locations_Unwound L WHERE L.Parent = PL.[Location] AND @Location = L.Child))
                                                                                                                                                     
			/*could be a bit faster then Locations_Unwound if we pass LocationPath as parameter to the SProc 
                                                                                                                                                         
			SELECT NULL FROM OfferingLocations AS PL WITH(NOLOCK)
                                                                                                                                                                                                      
			LEFT JOIN LocationPath(@Location) AS LP
                                                                                                                                                                                                                   
			ON PL.[Location] = LP.[Location]
                                                                                                                                                                                                                          
			WHERE PL.Offering = P.Id AND (PL.[Location] = 0 OR PL.[Location] = @Location OR LP.[Location] IS NOT NULL)*/
                                                                                                                                               
		)*/) AND
                                                                                                                                                                                                                                                   
		(@InclFacets = 0 OR COALESCE(PFi.[Count], 0) = @InclFacets) AND
                                                                                                                                                                                            
		(@ExclFacets = 0 OR COALESCE(PFe.[Count], 0) = 0)
                                                                                                                                                                                                          
	) AS Offerings
                                                                                                                                                                                                                                               
	WHERE @GeoLocation IS NULL OR Distance <= @Distance
                                                                                                                                                                                                         
	OPTION (RECOMPILE) -- ~40% query cost improvement
                                                                                                                                                                                                           
				
                                                                                                                                                                                                                                                         
	----http://stackoverflow.com/questions/15815624/sql-performance-using-row-number-and-dynamic-order-by
                                                                                                                                                       
	----http://www.4guysfromrolla.com/webtech/042606-1.shtml
                                                                                                                                                                                                    

                                                                                                                                                                                                                                                             
	IF @StartIndex = 0 AND @Length = 0
                                                                                                                                                                                                                          
		BEGIN
                                                                                                                                                                                                                                                      
			SELECT Id, Distance FROM @Offerings
                                                                                                                                                                                                                        
			ORDER BY SortOrder DESC
                                                                                                                                                                                                                                   
			SELECT @Length = @@ROWCOUNT
                                                                                                                                                                                                                               
		END
                                                                                                                                                                                                                                                        
	ELSE IF @StartIndex = 0 AND @Length > 0
                                                                                                                                                                                                                     
		BEGIN
                                                                                                                                                                                                                                                      
			/*SELECT Id, Distance FROM (SELECT ROW_NUMBER() OVER 
                                                                                                                                                                                                     
			(
                                                                                                                                                                                                                                                         
				ORDER BY SortOrder DESC
                                                                                                                                                                                                                                  
			) AS RowNumber, Id, Distance FROM @Offerings) AS P
                                                                                                                                                                                                         
			WHERE P.RowNumber <= @Length*/
                                                                                                                                                                                                                            
			SELECT TOP(@Length) Id, Distance FROM @Offerings
                                                                                                                                                                                                           
			ORDER BY SortOrder DESC
                                                                                                                                                                                                                                   
			
                                                                                                                                                                                                                                                          
			SELECT @RecCount = @@ROWCOUNT
                                                                                                                                                                                                                             
			IF (@RecCount < @Length)
                                                                                                                                                                                                                                  
				SELECT @Length = @RecCount
                                                                                                                                                                                                                               
			ELSE
                                                                                                                                                                                                                                                      
				SELECT @Length = COUNT(*) FROM @Offerings
                                                                                                                                                                                                                 
		END
                                                                                                                                                                                                                                                        
	ELSE IF @StartIndex > 0 AND @Length > 0
                                                                                                                                                                                                                     
		BEGIN
                                                                                                                                                                                                                                                      
			/*SELECT Id, Distance FROM (SELECT ROW_NUMBER() OVER 
                                                                                                                                                                                                     
			(
                                                                                                                                                                                                                                                         
				ORDER BY SortOrder DESC
                                                                                                                                                                                                                                  
			) AS RowNumber, Id, Distance FROM @Offerings) AS P
                                                                                                                                                                                                         
			WHERE P.RowNumber BETWEEN (@StartIndex + 1) AND (@StartIndex + @Length)*/
                                                                                                                                                                                 

                                                                                                                                                                                                                                                             
			SELECT Id, Distance FROM @Offerings
                                                                                                                                                                                                                        
			ORDER BY SortOrder DESC
                                                                                                                                                                                                                                   
			OFFSET @StartIndex ROWS FETCH NEXT @Length ROWS ONLY
                                                                                                                                                                                                      
		END
                                                                                                                                                                                                                                                        
	ELSE
                                                                                                                                                                                                                                                        
		RAISERROR('Unexpected StartIndex and/or Length', 16, 1);
                                                                                                                                                                                                   

                                                                                                                                                                                                                                                             
	IF @StartIndex = 0
                                                                                                                                                                                                                                          
		BEGIN
                                                                                                                                                                                                                                                      
			INSERT INTO @Facets
                                                                                                                                                                                                                                       
			SELECT PFV.[Name], PFV.Id AS [Value], COUNT(*) AS [Count] FROM @Offerings AS P
                                                                                                                                                                             
			INNER JOIN CompanyOfferingFacets AS PF ON P.Id = PF.Offering
                                                                                                                                                                                                
			INNER JOIN CompanyOfferingFacetValues AS PFV ON PF.FacetValue = PFV.Id
                                                                                                                                                                                     
			GROUP BY PFV.[Name], PFV.Id
                                                                                                                                                                                                                               
			HAVING COUNT(*) < @Length
                                                                                                                                                                                                                                 
			SELECT [Name], [Value], [Count] FROM @Facets	
                                                                                                                                                                                                             
		END
                                                                                                                                                                                                                                                        
END
                                                                                                                                                                                                                                                          


