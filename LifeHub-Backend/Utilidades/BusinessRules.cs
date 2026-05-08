namespace LifeHub.Utilidades
{
    public class BusinessRules
    {
        public int MaxDocumentVersions { get; set; } = 30;
        public int MaxDocumentsPerUser { get; set; } = 20;
        public int MaxSpacesPerUser { get; set; } = 10;
        public int MaxPublishedDocumentsPerUser { get; set; } = 10;
        public int MaxProfileVisibleDocumentsPerUser { get; set; } = 3;
        public int MaxProfileVisibleSpacesPerUser { get; set; } = 3;
    }
}
