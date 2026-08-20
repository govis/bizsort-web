using System;
using Microsoft.Data.SqlClient;

class Program
{
    static void Main()
    {
        string connStr = "Server=.;Database=BizSort;Trusted_Connection=True;MultipleActiveResultSets=True;TrustServerCertificate=True";

        // 1. Fetch ProductTextSearch2
        string funcText = "";
        using (var conn = new SqlConnection(connStr))
        {
            conn.Open();
            using (var cmd = new SqlCommand("sp_helptext", conn))
            {
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@objname", "ProductTextSearch2");
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        funcText += reader.GetString(0);
                    }
                }
            }
        }
        
        // 2. Create OfferingTextSearch2
        string newFuncText = funcText.Replace("ALTER FUNCTION", "CREATE FUNCTION")
                                     .Replace("CREATE FUNCTION", "CREATE FUNCTION")
                                     .Replace("ProductTextSearch2", "OfferingTextSearch2")
                                     .Replace("Products", "Offerings");

        using (var conn = new SqlConnection(connStr))
        {
            conn.Open();
            using (var cmd = new SqlCommand(newFuncText, conn))
            {
                cmd.ExecuteNonQuery();
            }
            // Drop old function
            using (var cmd = new SqlCommand("DROP FUNCTION [dbo].[ProductTextSearch2]", conn))
            {
                cmd.ExecuteNonQuery();
            }
        }
        Console.WriteLine("Function OfferingTextSearch2 created and old dropped!");

        // 3. Update CompanySearch to use OfferingTextSearch2
        string spText = "";
        using (var conn = new SqlConnection(connStr))
        {
            conn.Open();
            using (var cmd = new SqlCommand("sp_helptext", conn))
            {
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@objname", "CompanySearch");
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        spText += reader.GetString(0);
                    }
                }
            }
        }
        
        string newSpText = spText.Replace("CREATE PROCEDURE", "ALTER PROCEDURE")
                                 .Replace("ProductTextSearch2", "OfferingTextSearch2");

        using (var conn = new SqlConnection(connStr))
        {
            conn.Open();
            using (var cmd = new SqlCommand(newSpText, conn))
            {
                cmd.ExecuteNonQuery();
            }
        }
        Console.WriteLine("Stored Procedure CompanySearch updated to use OfferingTextSearch2!");
    }
}
