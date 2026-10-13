# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

`eark-sip-4net` is a fork of [dotnet-eark-sip](https://github.com/igfej-justica-gov-pt/dotnet-eark-sip) and starts its own version history here. For earlier history, see the [upstream changelog](https://github.com/igfej-justica-gov-pt/dotnet-eark-sip/blob/main/CHANGELOG.md).

## [1.0.0] - Unreleased

### Added

- Creation of E-ARK SIPs (CSIP 2.0.4, 2.1.0 and 2.2.0) as a .NET library and as a command-line tool (`eark-sip-4net-cli`).
- Support for descriptive, preservation, technical, source, and rights metadata at both package and representation levels.
- E-ARK SIP parser: support for reading and validating existing E-ARK SIP packages from ZIP archives, via `EARKSIP.Parse`, returning a populated `SIP` with a `ValidationReport`.
- Version-specific METS parsers (`EARKMETSParser204`, `EARKMETSParser210`) and `EARKReadUtils` to populate the `IP`/`SIP`/`IPRepresentation` object graph, including checksum verification.
- `ValidationEntry`, `ValidationReport` and `ValidationConstants` for collecting and reporting parse/validation issues.
- Read strategies (`IReadStrategy` / `ZipReadStrategy`) and their factories for ZIP extraction.
- Examples (including `Example5` for parsing) and tests covering metadata, parsing and create/parse roundtrips.

### Changed

- Renamed from `dotnet-eark-sip`: NuGet package ID `eark-sip-4net`, namespaces `eark_sip_4net*`, and project folders `eark-sip-4net*`.
