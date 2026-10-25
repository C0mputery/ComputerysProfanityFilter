using ComputerysProfanityFilter;

ProfanityList filter = new ProfanityList();
int expandedTerms = filter.GenerateEncodedVariations(DefaultProfanityList.Terms).Count;
int alwaysCensorTerms = DefaultProfanityList.AlwaysCensorTerms.Count;
int rawTerms = DefaultProfanityList.Terms.Count;

Console.WriteLine($"Raw terms: {rawTerms}");
Console.WriteLine($"Expanded terms: {expandedTerms}");
Console.WriteLine($"AlwaysCensorTerms: {alwaysCensorTerms}");
