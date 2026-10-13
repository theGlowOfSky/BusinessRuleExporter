namespace BusinessRuleExporter
{
    public sealed class BusinessRuleRow
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public string Table { get; set; }
        public string State { get; set; }
        public string StatusCode { get; set; }
        public string Scope { get; set; }
        public string ReferencedFields { get; set; }
        public string VariableMapping { get; set; }
        public string Logic { get; set; }
    }
}
