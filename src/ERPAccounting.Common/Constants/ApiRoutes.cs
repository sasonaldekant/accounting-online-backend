namespace ERPAccounting.Common.Constants;

public static class ApiRoutes
{
    public static class DocumentLineItems
    {
        public const string Base = "api/v1/documents/{documentId:int}/items";
        public const string ItemById = "{itemId:int}";
    }

    public static class Lookups
    {
        public const string Base = "api/v1/lookups";
        public const string Partners = "partners";
        public const string OrganizationalUnits = "organizational-units";
        public const string TaxationMethods = "taxation-methods";
        public const string Referents = "referents";
        public const string DocumentsNd = "documents-nd";
        public const string TaxRates = "tax-rates";
        public const string Articles = "articles";
        public const string DocumentCosts = "document-costs/{documentId:int}";
        public const string CostTypes = "cost-types";
        public const string CostDistributionMethods = "cost-distribution-methods";
        public const string CostArticles = "cost-articles/{documentId:int}";
    }
}
