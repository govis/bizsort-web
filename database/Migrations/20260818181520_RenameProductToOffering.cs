using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BizSrt.Data.Migrations
{
    public partial class RenameProductToOffering : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable("Products", newName: "Offerings");
            migrationBuilder.RenameTable("CompanyProducts", newName: "CompanyOfferings");
            migrationBuilder.RenameTable("ProductMedia", newName: "OfferingMedia");
            migrationBuilder.RenameTable("CategoryProductAttributes", newName: "CategoryOfferingAttributes");
            migrationBuilder.RenameTable("ProductTypes", newName: "OfferingTypes");
            migrationBuilder.RenameTable("ProductAttributeTypes", newName: "OfferingAttributeTypes");
            migrationBuilder.RenameTable("CompanyProductFacets", newName: "CompanyOfferingFacets");
            migrationBuilder.RenameTable("CompanyProductFacetValues", newName: "CompanyOfferingFacetValues");
            migrationBuilder.RenameTable("CompanyProductFacetNames", newName: "CompanyOfferingFacetNames");
            migrationBuilder.RenameTable("CompanyProductFacetSets", newName: "CompanyOfferingFacetSets");
            migrationBuilder.RenameTable("CompanyProductFacetSetDetails", newName: "CompanyOfferingFacetSetDetails");
            migrationBuilder.RenameTable("FacetSetCompanyProducts", newName: "FacetSetCompanyOfferings");
            
            migrationBuilder.RenameColumn("Product", "CompanyOfferings", "Offering");
            migrationBuilder.RenameColumn("Product", "OfferingMedia", "Offering");
            migrationBuilder.RenameColumn("Product", "CompanyOfferingFacets", "Offering");
            migrationBuilder.RenameColumn("Product", "FacetSetCompanyOfferings", "Offering");
            
            // Renaming PKs
            migrationBuilder.Sql("BEGIN TRY EXEC sp_rename 'PK_Products', 'PK_Offerings' END TRY BEGIN CATCH END CATCH");
            migrationBuilder.Sql("BEGIN TRY EXEC sp_rename 'PK_CompanyProducts', 'PK_CompanyOfferings' END TRY BEGIN CATCH END CATCH");
            migrationBuilder.Sql("BEGIN TRY EXEC sp_rename 'PK_ProductMedia', 'PK_OfferingMedia' END TRY BEGIN CATCH END CATCH");
            migrationBuilder.Sql("BEGIN TRY EXEC sp_rename 'PK_CategoryProductAttributes', 'PK_CategoryOfferingAttributes' END TRY BEGIN CATCH END CATCH");
            migrationBuilder.Sql("BEGIN TRY EXEC sp_rename 'PK_ProductTypes', 'PK_OfferingTypes' END TRY BEGIN CATCH END CATCH");
            migrationBuilder.Sql("BEGIN TRY EXEC sp_rename 'PK_ProductAttributeTypes', 'PK_OfferingAttributeTypes' END TRY BEGIN CATCH END CATCH");
            migrationBuilder.Sql("BEGIN TRY EXEC sp_rename 'PK_CompanyProductFacets', 'PK_CompanyOfferingFacets' END TRY BEGIN CATCH END CATCH");
            migrationBuilder.Sql("BEGIN TRY EXEC sp_rename 'PK_CompanyProductFacetValues', 'PK_CompanyOfferingFacetValues' END TRY BEGIN CATCH END CATCH");
            migrationBuilder.Sql("BEGIN TRY EXEC sp_rename 'PK_CompanyProductFacetNames', 'PK_CompanyOfferingFacetNames' END TRY BEGIN CATCH END CATCH");
            migrationBuilder.Sql("BEGIN TRY EXEC sp_rename 'PK_CompanyProductFacetSets', 'PK_CompanyOfferingFacetSets' END TRY BEGIN CATCH END CATCH");
            migrationBuilder.Sql("BEGIN TRY EXEC sp_rename 'PK_CompanyProductFacetSetDetails', 'PK_CompanyOfferingFacetSetDetails' END TRY BEGIN CATCH END CATCH");
            migrationBuilder.Sql("BEGIN TRY EXEC sp_rename 'PK_FacetSetCompanyProducts', 'PK_FacetSetCompanyOfferings' END TRY BEGIN CATCH END CATCH");
            migrationBuilder.Sql("EXEC sp_rename 'ProductTextSearch', 'OfferingTextSearch'");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse of Up
            migrationBuilder.Sql("EXEC sp_rename 'ProductTextSearch', 'OfferingTextSearch'");
        }
    }
}


