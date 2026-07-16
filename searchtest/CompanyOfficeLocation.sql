Text                                                                                                                                                                                                                                                           
---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

                                                                                                                                                                                                                                                             

                                                                                                                                                                                                                                                             
CREATE FUNCTION [dbo].[CompanyOfficeLocation]
                                                                                                                                                                                                                
  (@Location INT)
                                                                                                                                                                                                                                            
RETURNS TABLE
                                                                                                                                                                                                                                                
AS
                                                                                                                                                                                                                                                           
RETURN
                                                                                                                                                                                                                                                       
(
                                                                                                                                                                                                                                                            
	SELECT CP.Id, CO.Id AS Office FROM CompanyProfiles AS CP
                                                                                                                                                                                                    
	INNER JOIN (SELECT Company, CASE WHEN [Order] = 0 THEN 0 ELSE Id END AS Id,
                                                                                                                                                                                 
	ROW_NUMBER() OVER(PARTITION BY Company ORDER BY Company, [Order]) AS RowNum
                                                                                                                                                                                 
    FROM CompanyOffices
                                                                                                                                                                                                                                      
	WHERE [Location] = @Location OR 
                                                                                                                                                                                                                            
    EXISTS (SELECT NULL FROM Locations_Unwound L WHERE L.Parent = @Location AND Location = L.Child)) AS CO ON CP.Id = CO.Company AND CO.RowNum = 1
                                                                                                           
)
                                                                                                                                                                                                                                                            

                                                                                                                                                                                                                                                             
