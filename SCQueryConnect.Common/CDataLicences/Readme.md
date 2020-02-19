This folder should contain licence data for CData modules, which is read by
[`CDataLicenceService.cs`](https://github.com/SharpCloud/SCQueryConnect/blob/cdata/SCQueryConnect.Common/Services/CDataLicenceService.cs).
A file named `CDataLicences.txt` should be created, and its contents should
follow the pattern of `ConnectorName=Key`. For example:

```
Access=1234567890123456789012345678901234567890
Excel=1234567890123456789012345678901234567890
SharePoint=1234567890123456789012345678901234567890
```

A template licence file can be created by running `CreateLicenceFile.bat`. This
automatically runs as a pre-bulid step of `SCQueryConnect.Common`, and only
creates the template file if an existing `CDataLicences.txt` is not present.