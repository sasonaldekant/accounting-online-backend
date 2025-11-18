namespace ERPAccounting.Common.Constants;

public static class ErrorMessages
{
    public const string NotFoundTitle = "Resource not found";
    public const string ConflictTitle = "Conflict detected";
    public const string ValidationFailedTitle = "Validation failed";
    public const string BadRequestTitle = "Invalid request";
    public const string LookupErrorTitle = "Lookup error";
    public const string UnhandledExceptionTitle = "Unexpected error";
    public const string UnhandledExceptionDetail = "Dogodila se neočekivana greška. Molimo pokušajte ponovo.";

    public const string DocumentNotFound = "Dokument nije pronađen.";
    public const string DocumentLineItemNotFound = "Stavka dokumenta nije pronađena.";
    public const string DocumentCostNotFound = "Trošak dokumenta nije pronađen.";
    public const string DocumentCostItemNotFound = "Stavka zavisnog troška nije pronađena.";
    public const string ConcurrencyConflict = "Stavka je promenjena od strane drugog korisnika.";
    public const string MissingIfMatchHeader = "Zahtev mora da sadrži If-Match header sa ETag vrednošću.";
    public const string InvalidIfMatchHeader = "If-Match header mora sadržati validan Base64 ETag.";
    public const string ValidationFailed = "Prosleđeni podaci nisu prošli validaciju.";
    public const string LookupLoadFailed = "Greška pri učitavanju {0}.";
    public const string StoredProcedureFailed = "Greška pri izvršavanju procedure {0}.";
    public const string CostManualDistributionRequired = "Za ručnu raspodelu morate navesti iznose po stavkama.";
    public const string InvalidCostDistributionMethod = "Nepodržan način raspodele troškova.";
}
