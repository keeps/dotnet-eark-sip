# .NET E-ARK SIP

[![License](https://img.shields.io/badge/license-EUPL-blue.svg)](#license)

A .NET library and CLI tool for **creating and reading** E-ARK Submission Information Packages (SIPs). Supports E-ARK SIP formats 2.0.4, 2.1.0, and 2.2.0, making it easier to produce and consume valid SIPs for long-term digital preservation according to the E-ARK standards.

> **This repository is a fork of [igfej-justica-gov-pt/dotnet-eark-sip](https://github.com/igfej-justica-gov-pt/dotnet-eark-sip/).** The upstream library focuses on **creating** E-ARK SIPs. This fork extends it with **read support** — the ability to parse an existing E-ARK SIP ZIP back into the in-memory model, with a validation report that records every issue found along the way. All upstream create-side functionality is preserved unchanged.

## Table of Contents

- [Introduction](#introduction)
- [Why .NET E-ARK SIP?](#why-net-e-ark-sip)
- [Features](#features)
- [What this fork adds](#what-this-fork-adds)
- [Requirements](#requirements)
- [Usage](#usage)
  - [Use as a Command-line Tool](#use-as-a-command-line-tool)
    - [Available options](#available-options)
    - [Example](#example)
  - [Use as a .NET Library](#use-as-a-net-library)
    - [Creating a SIP](#creating-a-sip)
    - [Parsing a SIP](#parsing-a-sip)
- [Contributing](#contributing)
- [FAQ](#faq)
- [License](#license)
- [Credits](#credits)

## Introduction

This repository provides a command-line interface (CLI) and a .NET library to **create and read** E-ARK Submission Information Packages.

The _E-ARK Information Package_ specifications are maintained by the [DILCIS Board](http://www.dilcis.eu/). The DILCIS Board is an international group of experts dedicated to developing and maintaining interoperability specifications for the long-term preservation of digital content.

These specifications have been sponsored and are promoted by the European Commission under the [eArchiving Initiative](https://digital-strategy.ec.europa.eu/en/activities/earchiving).

## Why .NET E-ARK SIP?

- **Automation**: Easily script and automate the creation _and ingestion_ of E-ARK SIPs.
- **Integration**: Embed this library into your applications to implement powerful export, transfer, _and ingest_ processes between business applications and digital archives (OAIS). The library is open-source, but not viral!
- **Standards-Compliant**: Ensures compliance with DILCIS Board specifications.
- **CLI & Library**: Use it stand-alone or integrate into your .NET projects.
- **Improvise, adapt, overcome**: You are free to modify and enhance this library to meet your needs. Just remember, forks must be open source as well.

## Features

- **CLI Tool**: Easily create SIPs via a command-line interface.
- **.NET Library**: Integrate with your own projects by referencing the NuGet package or local DLL.
- **Create E-ARK SIPs**: Produce valid SIPs for E-ARK 2.0.4, 2.1.0, and 2.2.0.
- **Read E-ARK SIPs**: Parse existing ZIP-packaged SIPs back into the in-memory model (2.0.4 and 2.1.0; auto-detected from `METS/@PROFILE`).
- **Validation report**: Every issue encountered while parsing (bad checksum, missing file, malformed METS, unsupported algorithm, …) is recorded as an `ERROR` / `WARN` / `INFO` entry on the SIP — parsing never throws for content-level issues.
- **Checksums**: Automatically computes checksums (SHA-256 by default) when creating, and verifies them against METS when reading.
- **Multiple metadata files**: Add and read descriptive, preservation, technical, source, rights, and other metadata, including external schemas.
- **Documentation**: Bundle documentation files within your SIP to better describe the contents of the submission.
- **Configurability**: Override default schemas, specify representation types, set custom IDs, and more.

## What this fork adds

A new read pipeline that mirrors the existing write pipeline:

| Concern | Upstream (write) | This fork (read) |
| --- | --- | --- |
| Entry point | `EARKSIP.Build(...)` | `EARKSIP.Parse(source[, destinationDirectory])` |
| Strategy pattern | `IWriteStrategy` / `ZipWriteStrategy` / factory | `IReadStrategy` / `ZipReadStrategy` / factory |
| Version-specific METS | `EARKMETSCreator204` / `EARKMETSCreator210` (via `METSGeneratorFactory.GetGenerator`) | `EARKMETSParser204` / `EARKMETSParser210` (via `METSGeneratorFactory.GetParser`) |
| Orchestration | `EARKUtils.Add*ToZipAndMETS()` | `EARKReadUtils.Process*()` |
| Error model | exceptions on invalid input | `ValidationReport` accumulates `ERROR` / `WARN` / `INFO` entries; `sip.IsValid()` flips false on errors |

Round-trip safety is verified by tests that build a SIP and parse it back, asserting structural equivalence.

## Requirements

- .NET Standard 2.0 (or higher)
- Windows, Linux, or macOS

## Usage

You can use **dotnet-eark-sip** as a **command-line tool** or as a **.NET library** (by referencing it in your project).

### Use as a Command-line Tool

The CLI exposes the create path. Run:

```bash
dotnet-eark-sip-cli create
```

#### Available options

- **create**: Creates an E-ARK SIP (this is the default action, so it can be omitted).
- **-d, --documentation**: Path(s) to folder(s)/file(s) to add to the SIP's documentation section.
- **-p, --path**: Path to save the SIP.
- **-a, --ancestors**: ID(s) of the SIP's ancestors.
- **-C, --checksum**: Checksum algorithm (default is SHA-256).
- **-T, --target-only**: Adds only the files for the representations.
- **-v, --version**: E-ARK SIP specification version (default: 2.1.0).
- **--submitter-name**: The name of the submitter agent.
- **--submitter-id**: The identification code of the submitter agent.
- **--sip-id**: ID of the SIP.
- **--override-schema**: Overrides default schema.
- **-s, --strategy**: Write strategy (default: Zip).

##### Descriptive Metadata Options

- **--metadata-files**: Path(s) to metadata file(s), comma-separated. _Required if_ `--representation-data-lists` _is not set_.
- **--metadata-types**: Metadata type(s), comma-separated. _Required if_ `--metadata-files` _is set_.
- **--metadata-schemas**: Path(s) to metadata schema file(s), comma-separated.
- **--metadata-versions**: Metadata version(s), comma-separated.

##### Representation Options

- **--representation-data-lists**: Path(s) to file(s) for representation, comma-separated. _Required if_ `--metadata-files` _is not set_.
- **--representation-id**: Representation identifier(s), comma-separated. Defaults to `rep<number>` if not provided.
- **--representation-type**: Representation type(s), comma-separated.

#### Example

```bash
dotnet-eark-sip-cli create --metadata-files metadata.xml --metadata-types ead --metadata-schemas ead2002.xsd \
--representation-data-lists dataFile1.pdf,dataFolder1,dataFile2.png \
--sip-id sip1 --ancestors sip2,sip3 --documentation documentation1,documentationFolder \
--path outputFolder --submitter-name agent1 --submitter-id 123
```

### Use as a .NET Library

> **Note**: If you do not plan to use this as a .NET library, you can skip this section.

#### Install the package via NuGet

```bash
dotnet add package dotnet-eark-sip
```

#### Creating a SIP

```csharp
// Import dependencies
using IP;
using Mets;

// Start creating a SIP
SIP sip = new EARKSIP("SIP_1", IPContentType.GetMIXED(), IPContentInformationType.GetMIXED(), "2.2.0");

// Set the name of the software that is submitting the SIP (mandatory)
sip.AddSubmitterAgent("my-application", "my-application-id");

// Set optional human-readable description
sip.SetDescription("A full E-ARK SIP");

// Add descriptive metadata (SIP level)
string metadataPath = ".\\Resources\\EARK\\metadata_descriptive_dc.xml";
IPDescriptiveMetadata descriptiveMetadata = new(
   new IPFile(metadataPath),
   new MetadataType(IMetadataMdtype.DC),
   null
);
sip.AddDescriptiveMetadata(descriptiveMetadata);

// Add xml schema (SIP level)
string schemaPath = ".\\Resources\\EARK\\schema.xsd";
sip.AddSchema(new IPFile(schemaPath));

// Add a representation (status will be set to the default value, i.e. ORIGINAL)
IPRepresentation representation1 = new("representation 1");
IPContentInformationType newContentType = new("PDF/A");
representation1.ContentInformationType = newContentType;
sip.AddRepresentation(representation1);

// Add a file to the representation
string representationFilePath = ".\\Resources\\EARK\\documentation.pdf";
IPFile representationFile = new(representationFilePath);
representationFile.SetRenameTo("data_.pdf");
representation1.AddFile(representationFile);

// Set optional related information about ancestors
sip.SetAncestors(["b6f24059-8973-4582-932d-eb0b2cb48f28"]);

// Build the SIP
ZipWriteStrategyFactory zipWriteStrategyFactory = new();
IWriteStrategy writeStrategy = zipWriteStrategyFactory.Create(outputPath);
string zipSIP = sip.Build(writeStrategy);
```

#### Parsing a SIP

```csharp
using IP;

// Parse an E-ARK SIP ZIP. The library extracts it to a temp directory
// (use Parse(source, destinationDirectory) to control the extraction location).
SIP sip = EARKSIP.Parse(".\\path\\to\\SIP_1.zip");

// Inspect the result. Parsing never throws for content-level issues — it records them on a
// ValidationReport. Check IsValid() and the report before trusting the model.
if (!sip.IsValid())
{
    foreach (ValidationEntry entry in sip.GetValidationReport().GetEntries(ValidationEntryLevel.ERROR))
    {
        Console.WriteLine(entry); // e.g. [ERROR] FILE-002: Checksum mismatch using SHA-256 (at ...)
    }
    return;
}

// The full model is populated: ids, agents, dates, content type, every metadata section,
// schemas, documentation, ancestors, and representations (each with its files and metadata).
Console.WriteLine("Parsed SIP {0} with {1} representation(s)", sip.GetId(), sip.GetRepresentations().Count);

foreach (IPDescriptiveMetadata md in sip.GetDescriptiveMetadata())
{
    Console.WriteLine("  descriptive: {0} (type={1})", md.GetMetadata().GetFileName(), md.GetMetadataType()._GetType());
}

foreach (IPRepresentation rep in sip.GetRepresentations())
{
    Console.WriteLine("  representation {0}: {1} file(s)", rep.RepresentationID, rep.Data.Count);
}
```

The parser auto-detects the spec version from `METS/@PROFILE` (supports **2.0.4** and **2.1.0**), verifies every referenced file's checksum against METS, and walks `dmdSec` / `amdSec` / `fileSec` / the E-ARK structMap to populate the IP model.

##### Validation entry IDs

`ValidationEntry.Id` uses stable string identifiers (see `ValidationConstants`) so consumers can match on them programmatically:

| ID prefix | Examples |
| --- | --- |
| `PKG-*` | `SOURCE_NOT_FOUND`, `NOT_A_ZIP`, `ZIP_EXTRACTION_FAILED` |
| `METS-*` | `METS_FILE_NOT_FOUND`, `METS_UNMARSHAL_FAILED`, `METS_UNKNOWN_PROFILE`, `METS_STRUCTMAP_MISSING` |
| `FILE-*` | `FILE_NOT_FOUND`, `CHECKSUM_MISMATCH`, `UNSUPPORTED_CHECKSUM_ALGORITHM`, `CHECKSUM_NOT_DECLARED` |
| `META-*` | `MDREF_INCOMPLETE`, `METADATA_READ_FAILED` |
| `REP-*` | `REPRESENTATION_METS_MISSING`, `REPRESENTATION_METS_INVALID` |

#### In-depth usage

Look into the repository's [dotnet-eark-sip-examples](./dotnet-eark-sip-examples) folder for more in-depth usage patterns and advanced features. `Example1` through `Example4` demonstrate creating SIPs; `Example5` demonstrates parsing.

## Contributing

Contributions are welcome! Here are the basic steps:

1. **Fork** the repository and clone your fork.
2. **Create** a new feature branch.
3. **Commit** your changes, ensuring you follow the existing style and conventions.
4. **Open a Pull Request (PR)** describing the changes you've made and why they're needed.

<!-- For more details, see our [CONTRIBUTING.md](CONTRIBUTING.md). Please also be aware of our [Code of Conduct](CODE_OF_CONDUCT.md). -->

## FAQ

1. **Does this work on all platforms?**
   Yes, it is built for .NET Standard 2.0, so it should work on Windows, macOS, and Linux.

2. **Are older versions of .NET supported?**
   .NET Standard 2.0 is compatible with .NET 5 (and higher), .NET Core 2.0 (and higher), and .NET Framework 4.6.1 (with some limitations).

3. **What if I need an E-ARK version not listed?**
   Feel free to open an issue or contribute via pull request.

4. **Does parsing support AIPs?**
   No. This library is scoped to SIPs. AIP support is out of scope for this fork.

5. **Which checksum algorithms does the read path support?**
   Whatever the .NET runtime's `HashAlgorithm.Create(name)` resolves for the algorithm declared in METS. The library does not _filter_ algorithms — that's a consumer-level policy decision. If your ingestion service only accepts MD5 and SHA-256, enforce that on the parsed result by inspecting the algorithm declared on each `IPFile` / `IPMetadata`.

## License

This project is licensed under the **European Union Public Licence (EUPL) version 1.2**. The EUPL grants you the following rights:

1. **Use and access**: You are free to download and use this software, in whole or in part, for any lawful purpose, subject to the terms of the licence.
2. **Modification**: You can modify the source code to suit your needs, and you are encouraged to contribute your improvements back to the community.
3. **Distribution**: You can redistribute the original code or your modified version(s) to others. When you do, you **must** share it under the EUPL or a compatible licence, making the source code available under equivalent conditions.

### Limitations and Requirements

- **Licence continuity**: If you distribute copies or substantial portions of this software, modified or unmodified, you must retain the original licence text and grant the same rights to the recipients.
- **No warranty**: The software is provided "as is", without warranty of any kind. The licensor disclaims all liability for damages arising out of its use to the fullest extent permitted by law.
- **Attribution**: You must keep all copyright notices and attribution statements intact in the source files and any accompanying documentation.

For full details, please refer to the [EUPL licence text](https://joinup.ec.europa.eu/collection/eupl/eupl-text-eupl-12).

## Credits

- Developer: José Boticas (KEEP SOLUTIONS)
- Project manager: Paulo Lima (KEEP SOLUTIONS)
- Consultants: Luís Faria and Miguel Ferreira (KEEP SOLUTIONS)
- Upstream project: [igfej-justica-gov-pt/dotnet-eark-sip](https://github.com/igfej-justica-gov-pt/dotnet-eark-sip/)
