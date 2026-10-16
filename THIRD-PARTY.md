# Third-party components and build tools

- Microsoft .NET Framework 4.8: system runtime; not redistributed in the installer.
- Microsoft.Net.Compilers.Toolset 4.8.0 / .NET SDK: build-time tooling; not included in the delivered source or application.
- Inno Setup 6.5.3: installer compiler and setup/uninstall runtime. See https://jrsoftware.org/ and the applicable Inno Setup license. Compiler tools are not included in the source archive. Commercial distribution may require an Inno Setup commercial license; review its terms before commercial use.
- `installer/ChineseSimplified.isl`: Chinese translation obtained from https://github.com/jrsoftware/issrc/blob/main/Files/Languages/ChineseSimplified.isl ; original attribution comments retained.
- Times New Roman and SimSun: requested system fonts; no font files are redistributed. Availability/fallback depends on the destination Windows installation.

No open-source license has been assigned to the application's own source. The owner can select one before publishing to GitHub.
