using System;
using Microsoft.Data.SqlClient;

class Program
{
    static void Main()
    {
        string connStr = "Server=.;Database=BizSort;Trusted_Connection=True;MultipleActiveResultSets=True;TrustServerCertificate=True";
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
                                 .Replace("CompanyProducts", "CompanyOfferings")
                                 .Replace("CompanyProductFacets", "CompanyOfferingFacets")
                                 .Replace("ProductTypes", "OfferingTypes")
                                 .Replace("Products", "Offerings")
                                 .Replace("CPt.Product", "CPt.Offering");

        using (var conn = new SqlConnection(connStr))
        {
            conn.Open();
            using (var cmd = new SqlCommand(newSpText, conn))
            {
                cmd.ExecuteNonQuery();
            }
        }
        Console.WriteLine("Stored Procedure updated successfully!");
    }
}
