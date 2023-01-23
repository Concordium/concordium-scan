namespace Application.Api.GraphQL.Bakers;

public class BakerFilter
{
    public BakerPoolOpenStatus? OpenStatusFilter { get; set; }
    public bool? IncludeRemoved { get; set; }
}