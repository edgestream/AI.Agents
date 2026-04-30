internal sealed record FakeNewsStory(
    string Headline,
    string Summary,
    string Source,
    string Url,
    IReadOnlyList<string> Tags);
