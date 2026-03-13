using Mockstar.ParserApi.Contracts;
using Mockstar.Scoring;

namespace Mockstar.Web.Persistence.Mapping;

public static class ContractMapper
{
    public static EventRecord ToDomain(ParserEventRecord contract)
    {
        var heats = contract.Heats.Select(ToDomainHeat).ToList();
        return new EventRecord(contract.Id, contract.Name, heats);
    }

    public static ParserEventRecord ToContract(EventRecord domain)
    {
        var heats = domain.Heats.Select(ToContractHeat).ToList();
        return new ParserEventRecord(domain.Id, domain.Name, heats);
    }

    private static Heat ToDomainHeat(ParserHeat contract)
    {
        var importSource = new ImportSource(
            Enum.Parse<ImportSourceKind>(contract.ImportSource.Kind, ignoreCase: true),
            DateTimeOffset.Parse(contract.ImportSource.Timestamp));

        var phase = Enum.Parse<RoundPhase>(contract.Phase, ignoreCase: true);

        var normalizedType = NormalizeHeatType(contract.Type);

        return normalizedType switch
        {
            nameof(JackAndJillPrelimHeat) => new JackAndJillPrelimHeat(
                contract.Id,
                contract.Name,
                phase,
                contract.LeaderEntries.Select(e => new BibEntry(e.Id, e.Bib)).ToList(),
                contract.FollowerEntries.Select(e => new BibEntry(e.Id, e.Bib)).ToList(),
                importSource),

            nameof(JackAndJillFinalHeat) => new JackAndJillFinalHeat(
                contract.Id,
                contract.Name,
                contract.LeaderEntries.Select(e => new BibEntry(e.Id, e.Bib)).ToList(),
                contract.FollowerEntries.Select(e => new BibEntry(e.Id, e.Bib)).ToList(),
                contract.Pairings.Select(p => new Pairing(p.LeaderBib, p.FollowerBib)).ToList(),
                importSource),

            nameof(StrictlyHeat) => new StrictlyHeat(
                contract.Id,
                contract.Name,
                phase,
                contract.CoupleEntries.Select(e => new CoupleEntry(e.Id, e.LeaderBib, e.FollowerBib)).ToList(),
                importSource),

            _ => throw new InvalidOperationException($"Unknown heat type: {contract.Type}")
        };
    }

    private static string NormalizeHeatType(string type) => type switch
    {
        "jack-and-jill-prelim" => nameof(JackAndJillPrelimHeat),
        "jack-and-jill-final" => nameof(JackAndJillFinalHeat),
        "strictly" => nameof(StrictlyHeat),
        _ => type
    };

    private static ParserHeat ToContractHeat(Heat domain)
    {
        var importSource = new ParserImportSource(
            domain.ImportSource.Kind.ToString(),
            domain.ImportSource.Timestamp.ToString("O"));

        return domain switch
        {
            JackAndJillPrelimHeat prelim => new ParserHeat(
                prelim.Id,
                prelim.Name,
                nameof(JackAndJillPrelimHeat),
                prelim.Phase.ToString(),
                prelim.LeaderEntries.Select(e => new ParserBibEntry(e.Id, e.Bib, e.Display)).ToList(),
                prelim.FollowerEntries.Select(e => new ParserBibEntry(e.Id, e.Bib, e.Display)).ToList(),
                [],
                [],
                [],
                importSource),

            JackAndJillFinalHeat final => new ParserHeat(
                final.Id,
                final.Name,
                nameof(JackAndJillFinalHeat),
                final.Phase.ToString(),
                final.LeaderEntries.Select(e => new ParserBibEntry(e.Id, e.Bib, e.Display)).ToList(),
                final.FollowerEntries.Select(e => new ParserBibEntry(e.Id, e.Bib, e.Display)).ToList(),
                [],
                final.Pairings.Select(p => new ParserPairing(p.LeaderBib, p.FollowerBib)).ToList(),
                [],
                importSource),

            StrictlyHeat strictly => new ParserHeat(
                strictly.Id,
                strictly.Name,
                nameof(StrictlyHeat),
                strictly.Phase.ToString(),
                [],
                [],
                strictly.CoupleEntries.Select(e => new ParserCoupleEntry(e.Id, e.LeaderBib, e.FollowerBib, e.Display)).ToList(),
                [],
                [],
                importSource),

            _ => throw new InvalidOperationException($"Unknown heat type: {domain.GetType().Name}")
        };
    }
}
