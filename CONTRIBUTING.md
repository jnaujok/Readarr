# How to Contribute

We're always looking for people to help make Readarr even better, there are a number of ways to contribute.

## Documentation

Operator documentation for this fork is the HTML user guide in [`docs/user-guide/index.html`](docs/user-guide/index.html). Open that file in a browser; it does not need a build step. Architecture and behaviour diagrams are in [`DIAGRAMS.md`](DIAGRAMS.md).

The historical Servarr wiki remains at [wiki.servarr.com/readarr](https://wiki.servarr.com/readarr) for upstream *arr patterns (indexers, download clients). Prefer the local guide for metadata, wanted formats, naming tokens, and other behaviour that changed in this rewrite.

## Development

- Solution: `src/Readarr.sln`
- Default branch: `develop`
- Unit tests: `src/NzbDrone.Core.Test`
- House style: `NzbDrone.*` namespaces, NUnit, FluentAssertions, Moq, Newtonsoft.Json, DryIoc, StyleCop
- Coverage gate for new slices: Coverlet ≥ 80%

```bash
export PATH="$HOME/.dotnet:$PATH"
dotnet build src/Readarr.sln -c Release
dotnet test src/NzbDrone.Core.Test/NzbDrone.Core.Test.csproj -c Release --filter "FullyQualifiedName!~Integration"
```
