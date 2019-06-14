# CleanUpNetCoreSdk

A tool to remove outdated .NET Core SDKs.

Keeps only the latest version for each minor version.

For instance, if you have the following SDK versions:

- 2.1.200
- 2.1.500
- 2.1.501
- 2.1.700
- 2.2.101
- 2.2.107
- 2.2.300
- 3.0.100-preview2
- 3.0.100-preview3
- 3.0.100-preview5

The tool will remove everything except:
- The latest 2.1.x version
- The latest 2.2.x version
- The latest 3.0.x preview version

Different bitness (x86/x64) are considered separately, as well as prerelease and stable versions.

The tool will show what it's going to do and give you an opportunity to proceed before actually making any change.
