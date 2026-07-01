using IP;

namespace dotnet_eark_sip_examples;

/// <summary>
/// Parse an existing E-ARK SIP ZIP and print a summary of its contents. Mirrors the create-side examples but
/// exercises the read pipeline (<see cref="EARKSIP.Parse(string)"/>).
/// </summary>
/// <remarks>
/// Looks for a built SIP zip in the current directory (default name: <c>SIP_Example_1.zip</c>, produced by
/// <see cref="Example1"/>). Override by passing a path as the first CLI argument.
/// </remarks>
internal static class Example5
{
    public static void Run(string? sipPath = null)
    {
        string source = sipPath ?? Path.Combine(Directory.GetCurrentDirectory(), "SIP_Example_1.zip");
        if (!File.Exists(source))
        {
            Console.WriteLine("SIP not found at {0}. Run Example1 first, or pass a SIP path as an argument.", source);
            return;
        }

        Console.WriteLine("Parsing {0}", source);
        SIP sip = EARKSIP.Parse(source);

        Console.WriteLine();
        Console.WriteLine("---- Summary ----");
        Console.WriteLine("ID:                     {0}", sip.GetId());
        Console.WriteLine("Type:                   {0}", sip._GetType());
        Console.WriteLine("Profile:                {0}", sip.GetProfile());
        Console.WriteLine("Description:            {0}", sip.GetDescription());
        Console.WriteLine("Agents:                 {0}", sip.GetAgents().Count);
        foreach (IPAgent agent in sip.GetAgents())
        {
            Console.WriteLine("  - {0} (role={1}, type={2})", agent.GetName(), agent.GetRole(), agent._GetType());
        }
        Console.WriteLine("Ancestors:              {0}", string.Join(", ", sip.GetAncestors()));
        Console.WriteLine("Descriptive metadata:   {0}", sip.GetDescriptiveMetadata().Count);
        foreach (IPDescriptiveMetadata md in sip.GetDescriptiveMetadata())
        {
            Console.WriteLine("  - {0} (type={1}, version={2})", md.GetMetadata().GetFileName(), md.GetMetadataType()._GetType(), md.MetadataVersion ?? "—");
        }
        Console.WriteLine("Preservation metadata:  {0}", sip.GetPreservationMetadata().Count);
        Console.WriteLine("Technical metadata:     {0}", sip.GetTechnicalMetadata().Count);
        Console.WriteLine("Source metadata:        {0}", sip.GetSourceMetadata().Count);
        Console.WriteLine("Rights metadata:        {0}", sip.GetRightsMetadata().Count);
        Console.WriteLine("Other metadata:         {0}", sip.GetOtherMetadata().Count);
        Console.WriteLine("Schemas:                {0}", sip.GetSchemas().Count);
        Console.WriteLine("Documentation:          {0}", sip.GetDocumentation().Count);
        Console.WriteLine("Representations:        {0}", sip.GetRepresentations().Count);
        foreach (IPRepresentation rep in sip.GetRepresentations())
        {
            Console.WriteLine("  - {0} ({1} file(s), status={2})", rep.RepresentationID, rep.Data.Count, rep.GetStatus());
        }

        Console.WriteLine();
        Console.WriteLine("---- Validation report ----");
        Console.WriteLine("IsValid:                {0}", sip.IsValid());
        Console.WriteLine("Total entries:          {0}", sip.GetValidationReport().Entries.Count);
        foreach (ValidationEntry entry in sip.GetValidationReport().Entries)
        {
            Console.WriteLine("  {0}", entry);
        }
    }
}
