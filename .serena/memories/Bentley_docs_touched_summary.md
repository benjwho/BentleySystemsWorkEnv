# Bentley docs touched summary (2026-02-11)

This memory consolidates all Bentley documentation explored so far in this session.

## MicroStation Help 2025.0.1
Base hub:
- https://docs.bentley.com/LiveContent/web/MicroStation-v2025.0.1/Help/en/topics/6200/GUID-288FAFD8-1107-4FCB-9843-8BECC9099A06.html

### Programmed Customizations -> MDL Applications
- Path: MicroStation Help -> Programmed Customizations -> MDL Applications.
- MDL (MicroStation Development Libraries) is a primary customization/extension mechanism inside MicroStation.
- Multiple MDL apps can be loaded at once; many standard MicroStation features are implemented in MDL.
- MDL apps can register menus and key-ins; some key-ins are only available after a specific MDL is loaded (example noted in doc: BATCHPROCESS key-ins via MDL LOAD).
- MDL apps built for earlier MicroStation editions must be rebuilt for the current edition (SDK guidance referenced).

Links:
- Programmed Customizations: https://docs.bentley.com/LiveContent/web/MicroStation-v2025.0.1/Help/en/topics/123008/GUID-79991C3F-6A91-81E3-16C5-346EE031C4CA.html
- MDL Applications: https://docs.bentley.com/LiveContent/web/MicroStation-v2025.0.1/Help/en/topics/123008/GUID-0F6F7AE4-58DD-9B95-5406-040963287EA9.html

### Configuration hub + linked subpages (full coverage)
Hub:
- https://docs.bentley.com/LiveContent/web/MicroStation-v2025.0.1/Help/en/topics/123015/GUID-69DA9741-015A-3EAC-6F23-4694F3C85CC4.html

#### Configuration Concepts
- Configuration = resources + config files + config variables + WorkSpaces + WorkSets.
- Variables define resource paths and behavior; they are defined in configuration files.
- Hierarchy/levels: System → Application → Organization → WorkSpace → WorkSet → Role → User.
- System/Application levels are vendor-provided defaults; users should not modify.
- Organization level holds company standards; MicroStation provides example structure and standards.cfg.
- WorkSpace: grouping for broad context (client/asset/department); UI label can be customized.
- WorkSet: project-level grouping; can override/augment Organization/WorkSpace standards.
- Role: optional per-role config.
- User: per-user settings stored in .ucf.

#### Managing Your Configuration
- Primarily a link hub to tasks such as finding active WorkSpace/WorkSet, configuration window, sample WorkSets, sharing configuration, and configuration variables.

#### Creating and Managing Configurations
- Use Manage Configuration tool to create/manage configurations.
- Supports creating custom configs and editing existing config settings.

#### Customizing the User Interface
- UI customization includes ribbon, tools, toolboxes, tasks, main tasks, context menus, icons.
- Work page / File Open dialog used to select/activate WorkSpace/WorkSet.
- Prior customizations can be imported into DGN libraries for reuse.
- Additional customization includes keyboard shortcuts, function key menus, and button assignments.

#### Migrating Legacy Configuration
- Configuration organization/processing changes in recent versions.
- Configuration Assistant is recommended for migrating legacy (V8i) WorkSpaces.
- Converts .ucf to WorkSpace .cfg, .pcf to WorkSet .cfg.
- MDL apps in MS_DGNAPPS in legacy .pcf may cause loader errors if missing in newer versions.

#### Migrating DWG Folders to MicroStation Configuration
- DWG Configuration Assistant migrates AutoCAD folder structures into the WorkSpace/WorkSet model to align standards and simplify mixed CAD workflows.

#### Configuration Assistant
- Identifies installed MicroStation-based products and default WorkSpaces.
- Converts .ucf → WorkSpace .cfg, .pcf → WorkSet .cfg.
- Moves Site config to Organization level.
- Optionally copies project data files.
- Access: Work page (WorkSpace dropdown) or File > Settings > Configuration > Configuration Assistant.
- Wizard flow: select V8i WorkSpace (conversion type), select users/projects, destination (default C:\Temp\Configuration), migration results.

#### Batch Migration from V7 to V8 Format
- Use Batch Converter to upgrade V7 DGN/cell libraries to V8.
- Unit definitions can be controlled by units.def; MS_CUSTOMUNITDEF points to unit def file.
- Custom unit definitions override standard units if conflicts exist.

#### Directory Structure
- Default install root: C:\Program Files\Bentley.
- User data: %LOCALAPPDATA%\Bentley\<product>\<version> (defined by _USTN_HOMEROOT). Recommended to stay local.
- Documentation: C:\ProgramData\Bentley\Documentation\MicroStation\<version>\<language>\htm
- Program directory contains system subfolders (Assemblies, config, Default, Docs, ECSchemas, en, GeoCoordinateData, Mdlapps, Mdlsys, Ribbon, etc.).
- Configuration directory (WorkSpaces/WorkSets): C:\ProgramData\Bentley\<product> <version>.

Links:
- Configuration Concepts: https://docs.bentley.com/LiveContent/web/MicroStation-v2025.0.1/Help/en/topics/123015/GUID-F3850D56-A1B9-56D9-C430-4315A94AB8AF.html
- Managing Your Configuration: https://docs.bentley.com/LiveContent/web/MicroStation-v2025.0.1/Help/en/topics/123015/GUID-4BD88E7C-3D04-40C1-AFA2-9C16ECB5C2A5.html
- Creating and Managing Configurations: https://docs.bentley.com/LiveContent/web/MicroStation-v2025.0.1/Help/en/topics/123015/GUID-FAE702CC-F92E-4009-9A7E-CEB1B35E6974.html
- Customizing the User Interface: https://docs.bentley.com/LiveContent/web/MicroStation-v2025.0.1/Help/en/topics/123015/GUID-800D2BB2-28B9-FE26-43EC-72BC32FA619B.html
- Migrating Legacy Configuration: https://docs.bentley.com/LiveContent/web/MicroStation-v2025.0.1/Help/en/topics/123015/GUID-BD595DA3-B0AD-4C29-8D32-1B0DE51F62FB.html
- Migrating DWG Folders: https://docs.bentley.com/LiveContent/web/MicroStation-v2025.0.1/Help/en/topics/123015/GUID-90FE00DE-DD52-4BFC-921B-929FA70B94DF.html
- Configuration Assistant: https://docs.bentley.com/LiveContent/web/MicroStation-v2025.0.1/Help/en/topics/123015/GUID-6039B8C0-2601-42E5-8DEF-C03FA6E1FFE1.html
- Batch Migration V7->V8: https://docs.bentley.com/LiveContent/web/MicroStation-v2025.0.1/Help/en/topics/123015/GUID-6B3E5DE0-6B85-E702-9C59-477699D2D6DE.html
- Directory Structure: https://docs.bentley.com/LiveContent/web/MicroStation-v2025.0.1/Help/en/topics/123015/GUID-3D56382F-4235-A3DC-CFDA-628FBF1F7D85.html

### Configuration variables and tooling (operational details)
#### Configuration Variables (types)
- Framework variables: prefix _USTN_ for configuration structure/paths.
- Operational variables: prefix MS_ for runtime behavior and resource paths.
- Levels of variables and file processing are handled through configuration files and levels.

#### Working with Configuration Variables
- User-level variables can be edited through the Configuration Variables dialog.
- Org/WorkSpace/WorkSet variables require editing configuration files directly (text editor) using config file syntax.

#### About Configuration window / finding active WorkSpace + WorkSet
- Use File > Settings > Configuration > About Configuration.
- Shows active WorkSpace/WorkSet names and paths, user preferences, and ProjectWise connected project (if any).
- Key-in: DIALOG ABOUTCONFIGURATION.

#### Manage Configuration dialog
- Used to create/connect/edit/delete configurations; list ordering controls (move up/down/top/bottom).
- Shows configuration name/description/type/folder/visibility.

#### Typical Configuration Scenario (example)
- Example scenario defines _USTN_CONFIGURATION to a shared config root; uses WorkSpaceSetup.cfg to set _USTN_WORKSPACELABEL, _USTN_ORGANIZATION, _USTN_WORKSPACESROOT.
- Delivered Standards.cfg can be reused if org directory structure matches example.
- Client WorkSpace overrides _USTN_WORKSPACEROOT and appends client DGNLIBs/cell library paths using append syntax.
- WorkSet creation uses templates under _USTN_WORKSETSROOT and creates Standards/Dgn subfolders.

Links:
- Configuration Variables: https://docs.bentley.com/LiveContent/web/MicroStation-v2025.0.1/Help/en/topics/123015/GUID-EABCA351-6E56-D0B2-A2F2-50169763EF54.html
- Working with Configuration Variables: https://docs.bentley.com/LiveContent/web/MicroStation-v2025.0.1/Help/en/topics/123015/GUID-62FEC6B9-BEA9-328D-67F4-ABA8FB3DBA84.html
- Find active WorkSpace/WorkSet: https://docs.bentley.com/LiveContent/web/MicroStation-v2025.0.1/Help/en/topics/123016/GUID-7742579E-58AC-EE1A-5181-BDF7D54D1AE5.html
- About Configuration window: https://docs.bentley.com/LiveContent/web/MicroStation-v2025.0.1/Help/en/topics/123017/GUID-7A19E8ED-844B-9665-4B51-D826D70AB9C3.html
- Manage Configuration dialog: https://docs.bentley.com/LiveContent/web/MicroStation-v2025.0.1/Help/en/topics/123017/GUID-F2FE5CDB-5150-4FF3-A621-16CB1AE9DEB5.html
- Typical Configuration Scenario: https://docs.bentley.com/LiveContent/web/MicroStation-v2025.0.1/Help/en/topics/123015/GUID-DD8818B9-CA8C-402C-B5A1-F2663DD26625.html
