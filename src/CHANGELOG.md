# Easy.Template.XCS Changelog

*Please do not commit changes to this file, it is maintained by the repo owner.*

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/).

## [1.0.0] - 2026-09-21

First complete port of [easy-template-x](https://github.com/alonrbar/easy-template-x) (v7.2.x feature set, excluding the chart plugin).

### Added

- Loop plugin: content, paragraph, list, table rows and table columns strategies, `loopOver` tag option, conditions and nested conditions.
- Image plugin: inline images and image placeholder (alt text) replacement, transparency, alt text, media de-duplication.
- Link plugin.
- Raw xml plugin (`Xml` as string or list, `ReplaceParagraph`).
- Tags in attributes (image alt text).
- Tag options (`{#loop [loopOver: "row"]}`).
- Extensions (`BeforeCompilation` / `AfterCompilation`).
- Custom scope data resolvers.
- Headers and footers processing.
- Template data support for anonymous objects, POCOs, dictionaries and `System.Text.Json`.
- Typed exceptions (`Easy.Template.XCS.Errors`).
- Unique bookmark ids for repeated loop content.

### Changed

- Target frameworks: `net8.0` and `net10.0` (was `netstandard2.0` / `net6.0`).
- DocumentFormat.OpenXml 3.5.1 (was 2.20.0).
- Tests: xunit.v3 on Microsoft.Testing.Platform; the original easy-template-x fixture documents are used end-to-end.
- `TemplateHandler` API: `ProcessAsync`, `ParseTagsAsync`, `GetTextAsync`, `GetXmlAsync` (with `byte[]` and `Stream` overloads).
- Removed the dependency on `Microsoft.CSharp` / `dynamic`.
