# MicroStation 2025.0.1 environment management (full coverage)

Focus: configuration file system, variable syntax, processing flow, WorkSpace/WorkSet storage, and tools used to manage and debug environment setup. This builds on earlier configuration summaries.

## Configuration levels and variables
- Levels (priority low->high): System(0), Application(1), Organization(2), WorkSpace(3), WorkSet(4), Role(5), User(6).
  Source: https://docs.bentley.com/LiveContent/web/MicroStation-v2025.0.1/Help/en/topics/123015/GUID-2CAFDE0B-2DC9-C62E-BB7B-793BF345DEFE.html
- Variable families:
  - Framework variables (_USTN_) for configuration structure/paths.
  - Operational variables (MS_) for runtime behavior and resource search paths.
  Source: https://docs.bentley.com/LiveContent/web/MicroStation-v2025.0.1/Help/en/topics/123015/GUID-EABCA351-6E56-D0B2-A2F2-50169763EF54.html

## Configuration files: basics
- Config files are .cfg text files; system cfgs live under program ..\config; user cfgs under ..\Configuration or user-specified paths.
- Use key-in SHOW CONFIGURATION to dump current values to a text file.
  Source: https://docs.bentley.com/LiveContent/web/MicroStation-v2025.0.1/Help/en/topics/123015/GUID-FFE1082C-2596-4984-94D6-DA77DFC2DD81.html

## Configuration file syntax
- Statement types: flow directives, variable directives, assignment statements, expressions/operators.
- Paths use forward slashes; directory values should end with a trailing slash.
- Variable expansion:
  - $(VAR) stored verbatim for later evaluation.
  - ${VAR} evaluated immediately (VAR must already be defined).
  Source: https://docs.bentley.com/LiveContent/web/MicroStation-v2025.0.1/Help/en/topics/123015/GUID-C59A1F56-F723-853A-0469-83F5859385EC.html

### Flow directives
- %include filespec (optional level) to include cfgs.
- %if/%ifdef/%ifndef with %elif/%else/%endif for conditional processing.
- %echo for output, %error to stop with message.
  Source: https://docs.bentley.com/LiveContent/web/MicroStation-v2025.0.1/Help/en/topics/123015/GUID-4B1D4DEF-A362-9896-05C3-D487EF7346C7.html

### Variable directives
- %lock VAR locks a variable.
- %undef VAR clears value.
- %level <System|Application|Organization|WorkSpace|WorkSet|Role|User> sets the assignment level.
  Source: https://docs.bentley.com/LiveContent/web/MicroStation-v2025.0.1/Help/en/topics/123015/GUID-4B1D4DEF-A362-9896-05C3-D487EF7346C7.html

### Assignment operators
- = set always, : set only if undefined, > append path, < prepend path, + append without separator.
  Source: https://docs.bentley.com/LiveContent/web/MicroStation-v2025.0.1/Help/en/topics/123015/GUID-1DB07761-231A-41E5-86D0-BFD841C78F18.html

### Operators
- String/path helpers: basename, concat, devdir, dev, dir, ext, filename, first/last, parentdir, noext, etc.
- Registry: registryread, readregistryvalue.
  Source: https://docs.bentley.com/LiveContent/web/MicroStation-v2025.0.1/Help/en/topics/123015/GUID-EB4FD647-44DA-92B4-1415-C42044BDDD20.html

## Configuration file processing flow
- Processing begins at mslocal.cfg -> includes msdir.cfg -> includes msconfig.cfg.
- msconfig.cfg sets _USTN_BENTLEYROOT and framework defaults, then includes:
  - System cfgs: %include $(_USTN_SYSTEM)*.cfg level System
  - Application cfgs: %include $(_USTN_APPL)*.cfg level Application
- _USTN_CONFIGURATION defaults to ${_USTN_BENTLEYROOT}Configuration/ (curly brace => immediate evaluation).
  Source: https://docs.bentley.com/LiveContent/web/MicroStation-v2025.0.1/Help/en/topics/123015/GUID-9D4A6729-EF79-E270-FE82-42EB8D0FD944.html
- ConfigurationSetup.cfg is included when present; sets _USTN_CUSTOM_CONFIGURATION and _USTN_CONFIGURATION based on installation selection.
  - Do not edit manually; updated by installer or configuration switching.
  Source: https://docs.bentley.com/LiveContent/web/MicroStation-v2025.0.1/Help/en/topics/123015/GUID-749D0784-C39E-4DCD-9DAA-6AB8DE78E83C.html
- WorkSpaceSetup.cfg is included if present in _USTN_CONFIGURATION.
  - Recommended: copy delivered file into custom configuration and edit.
  - Primary uses: set _USTN_WORKSPACELABEL, redirect _USTN_ORGANIZATION and _USTN_WORKSPACESROOT.
  Source: https://docs.bentley.com/LiveContent/web/MicroStation-v2025.0.1/Help/en/topics/123015/GUID-F3546369-327A-4FF0-9BFA-98C21DCBC254.html
- Organization cfgs: all *.cfg under _USTN_ORGANIZATION are included (alphabetical order).
  Source: https://docs.bentley.com/LiveContent/web/MicroStation-v2025.0.1/Help/en/topics/123015/GUID-3119AFC2-5B3F-4CD1-94E5-3AE382381073.html
- User Configuration File: Personal.ucf in preferences directory stores user prefs and last WorkSpace/WorkSet.
  Source: https://docs.bentley.com/LiveContent/web/MicroStation-v2025.0.1/Help/en/topics/123015/GUID-BD279123-1BAE-4EA4-8559-754CDAC70C76.html
- WorkSpace cfg selection:
  - One WorkSpace cfg is processed, determined by _USTN_WORKSPACENAME (remembered by MicroStation).
  - WorkSpace cfg defines _USTN_WORKSPACEROOT, _USTN_WORKSPACESTANDARDS, _USTN_WORKSETSROOT.
  - Optional extra cfgs within _USTN_WORKSPACEROOT are included.
  Source: https://docs.bentley.com/LiveContent/web/MicroStation-v2025.0.1/Help/en/topics/123015/GUID-73CEB803-2983-441F-8E14-D8867C3098B9.html
- WorkSet cfg selection:
  - One WorkSet cfg is included based on _USTN_WORKSETCFG (MicroStation sets _USTN_WORKSETNAME from last used).
  - Default _USTN_WORKSETSROOT = $(_USTN_WORKSPACEROOT)WorkSets/ (can be changed).
  Source: https://docs.bentley.com/LiveContent/web/MicroStation-v2025.0.1/Help/en/topics/123015/GUID-D479C253-908E-4BBE-8583-C09F5DF60C53.html
- Role cfg:
  - If _USTN_ROLECFG defined, include that file; no default exists.
  Source: https://docs.bentley.com/LiveContent/web/MicroStation-v2025.0.1/Help/en/topics/123015/GUID-0C640902-892A-44E0-8CCD-B22027F559B6.html

## Debugging configuration
- Use MicroStation.exe -debug=n (n=1..5) to log config processing to msdebug.txt in TMP.
- Level 4 is default (final translations at end); level 5 shows final values at each level.
  Source: https://docs.bentley.com/LiveContent/web/MicroStation-v2025.0.1/Help/en/topics/123015/GUID-D5F85C78-38FB-09E8-5E90-AA98FA33761F.html

## WorkSet files and upgrade process
- Creating a WorkSet generates:
  - <WorkSetName>.cfg (config)
  - <WorkSetName>.dgnws (workset properties, link set, sheet index, custom properties)
- DGNWS files stored under ..\Configuration\WorkSpaces\<WorkSpace>\WorkSets\ by default.
- Upgrade from V8i .pcf to WorkSet:
  - Use key-in PROJECT UPGRADEALLTOWORKSETS after setting _USTN_WORKSETSROOT to folder holding .pcf.
  - Auto renames PROJECT -> WORKSET variables and creates .dgnws.
- Manual upgrade: rename .pcf to .cfg and replace PROJECT -> WORKSET variables, update _USTN_PROJECTDATA -> _USTN_WORKSETROOT.
  Source: https://docs.bentley.com/LiveContent/web/MicroStation-v2025.0.1/Help/en/topics/123015/GUID-9B0F78CB-DEFE-41E7-BC3A-4E7B47D3F381.html

## WorkSet root and DGNWS location
- Change WorkSet root by setting _USTN_WORKSETSROOT in a WorkSpace cfg to desired path (e.g., W:/WorkSets/).
  Source: https://docs.bentley.com/LiveContent/web/MicroStation-v2025.0.1/Help/en/topics/123015/GUID-2E4D0CFC-6F3B-4922-BB9A-3C2C832E3799.html
- Change DGNWS file location with _USTN_WORKSETSDGNWSROOT in WorkSpace cfg.
  - Options include placing all .dgnws under WorkSets root, a common DGNWS folder, or each WorkSet folder.
  Source: https://docs.bentley.com/LiveContent/web/MicroStation-v2025.0.1/Help/en/topics/123015/GUID-EC3DA021-035B-471B-8DDD-D5D12637B7C3.html

## WorkSet templates
- _USTN_WORKSPACETEMPLATE and _USTN_WORKSETTEMPLATE define template files for new WorkSpaces/WorkSets.
- WorkSet template can clone folder structure, optional files, custom properties, sheet index, link set.
- Template selection is available in Create WorkSet dialog.
  Source: https://docs.bentley.com/LiveContent/web/MicroStation-v2025.0.1/Help/en/topics/123015/GUID-3370F63D-11F5-489F-B772-2DE6091A2334.html

## Create WorkSet dialog (UI mappings to variables)
- Name/Description/Template; optional "Create folders only".
- Root folder sets _USTN_WORKSETROOT.
- Design files sets MS_DEF.
- Standard files sets _USTN_WORKSETSTANDARDS; subfolders set _USTN_WORKSETSTANDARDSUBDIRS.
- ProjectWise Project selection links WorkSet to PW project.
  Source: https://docs.bentley.com/LiveContent/web/MicroStation-v2025.0.1/Help/en/topics/122973/GUID-2E224C75-6CB1-4A30-B673-E46EA7A734EC.html

## Notes for INWC_RH integration
- INWC_RH config files should align with _USTN_ and MS_ usage, with Organization cfgs under Organization, WorkSpace cfgs under WorkSpaces, WorkSet cfgs under WorkSets.
- WorkSpaceSetup.cfg and ConfigurationSetup.cfg are the recommended entry points for redirecting org and workspace roots to network or repo-managed paths.
- Use SHOW CONFIGURATION and -debug=n for diagnosing integration drift and to confirm expected variable values.
- For ProjectWise integration, About Configuration window and Create WorkSet dialog both expose ProjectWise linkage for WorkSets.
