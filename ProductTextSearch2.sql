
                                                                                                                                                                                                                                                             
-- Created	Feb 09	by	V
                                                                                                                                                                                                                                       
-- Modified	Mar 12	by	V
                                                                                                                                                                                                                                      
CREATE FUNCTION [dbo].[ProductTextSearch2]
                                                                                                                                                                                                                   
	(@Query NVARCHAR(4000)) --http://stackoverflow.com/questions/3771302/the-full-text-query-parameter-for-fulltext-query-string-is-not-valid
                                                                                                                   

                                                                                                                                                                                                                                                             
RETURNS TABLE
                                                                                                                                                                                                                                                
AS
                                                                                                                                                                                                                                                           
RETURN
                                                                                                                                                                                                                                                       
(
                                                                                                                                                                                                                                                            
	SELECT [KEY], MAX(Rank) AS [Rank]
                                                                                                                                                                                                                           
	FROM
                                                                                                                                                                                                                                                        
    (
                                                                                                                                                                                                                                                        
		--SELECT [Rank] * 5 AS Rank, [KEY] FROM FREETEXTTABLE(Products,[Title],@Query)
                                                                                                                                                                             
		--UNION
                                                                                                                                                                                                                                                    
		SELECT [Rank] AS Rank, [KEY] FROM FREETEXTTABLE(Products,[Text],@Query)
                                                                                                                                                                                    
	) AS Pt
                                                                                                                                                                                                                                                     
	GROUP BY [KEY]
                                                                                                                                                                                                                                              
)
                                                                                                                                                                                                                                                            

                                                                                                                                                                                                                                                             
