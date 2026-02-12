# MicroStation 2025.0.1 Configuration section (Bentley docs)

Source hub:
- https://docs.bentley.com/LiveContent/web/MicroStation-v2025.0.1/Help/en/topics/123015/GUID-69DA9741-015A-3EAC-6F23-4694F3C85CC4.html

This memory covers the Configuration hub page and all subpages linked from that hub:
- Configuration Concepts
- Managing Your Configuration
- Creating and Managing Configurations
- Customizing the User Interface
- Migrating Legacy Configuration
- Migrating DWG Folders to MicroStation Configuration
- Configuration Assistant
- Batch Migration from V7 to V8 Format
- Directory Structure

## Configuration (hub)
- The hub page itself is a navigation page pointing to the topics above. It does not contain additional technical content beyond the links.

## Configuration Concepts
https://docs.bentley.com/LiveContent/web/MicroStation-v2025.0.1/Help/en/topics/123015/GUID-F3850D56-A1B9-56D9-C430-4315A94AB8AF.html
- MicroStation Configuration = resources + configuration files + configuration variables + WorkSpaces + WorkSets.
- Configuration variables define resource locations (levels, fonts, cells) and behavior flags; they are defined in configuration files.
- Configuration is hierarchical; variables can be defined/overridden at different levels:
  - System: supplied by MicroStation, sets defaults; should not be modified by users.
  - Application: supplied by MicroStation or layered apps; should not be modified by users.
  - Organization: organization-wide standards; MicroStation provides example folder structure and standards.cfg.
  - WorkSpace: container grouping WorkSets and standards for a broad context (client/asset/department). UI label for WorkSpace can be customized; default name is "WorkSpace". Each WorkSpace has one or more config files defining locations for WorkSpace standards and WorkSets.
  - WorkSet: project-level grouping owned by one WorkSpace; WorkSet config files can override/augment Org/WorkSpace standards.
  - Role: optional role-based config to override/augment standards for certain user roles.
  - User: per-user settings stored in user config file (.ucf).

## Managing Your Configuration
https://docs.bentley.com/LiveContent/web/MicroStation-v2025.0.1/Help/en/topics/123015/GUID-4BD88E7C-3D04-40C1-AFA2-9C16ECB5C2A5.html
- This page is primarily a link hub for operational tasks and references:
  - Typical Configuration Scenario
  - Find the location of active WorkSpace/WorkSets
  - About the Configuration window
  - Supplied sample WorkSets
  - Sharing an existing MicroStation configuration
  - Working with configuration variables

## Creating and Managing Configurations
https://docs.bentley.com/LiveContent/web/MicroStation-v2025.0.1/Help/en/topics/123015/GUID-FAE702CC-F92E-4009-9A7E-CEB1B35E6974.html
- Use the Manage Configuration tool to create and manage multiple configurations.
- Manage Configuration supports creating custom configurations and editing existing configurations.
- Links to the Manage Configuration dialog and related configuration variables reference.

## Customizing the User Interface
https://docs.bentley.com/LiveContent/web/MicroStation-v2025.0.1/Help/en/topics/123015/GUID-800D2BB2-28B9-FE26-43EC-72BC32FA619B.html
- UI customization is a key configuration activity; can customize:
  - Ribbon, tools, toolboxes, tasks, main tasks, context menus, icons.
- Work page / File Open dialog selects and activates WorkSpace and WorkSet.
- Prior customizations from earlier versions can be imported into DGN libraries.
- Additional UI customization areas: keyboard shortcuts, function key menus, button assignments.
- The page links to detailed subtopics like tools/toolboxes/tasks, context menus, icons, customize dialog, docking preferences, and action strings.

## Migrating Legacy Configuration
https://docs.bentley.com/LiveContent/web/MicroStation-v2025.0.1/Help/en/topics/123015/GUID-BD595DA3-B0AD-4C29-8D32-1B0DE51F62FB.html
- MicroStation changed how configuration files are organized/processed; legacy (V8i) WorkSpaces should be migrated.
- Configuration Assistant is the recommended tool for migration.
- It converts legacy user (.ucf) to WorkSpace (.cfg) and project (.pcf) to WorkSet (.cfg).
- Note: MDL apps referenced via MS_DGNAPPS in legacy .pcf may cause MDL loader errors if those apps are missing in the new MicroStation version.

## Migrating DWG Folders to MicroStation Configuration
https://docs.bentley.com/LiveContent/web/MicroStation-v2025.0.1/Help/en/topics/123015/GUID-90FE00DE-DD52-4BFC-921B-929FA70B94DF.html
- For hybrid AutoCAD + MicroStation workflows, DWG folder structures can be migrated.
- DWG Configuration Assistant helps migrate DWG folders into MicroStation configuration using the WorkSpace/WorkSet model.
- Intended to ease locating AutoCAD standards and align DWG data with MicroStation configuration.

## Configuration Assistant
https://docs.bentley.com/LiveContent/web/MicroStation-v2025.0.1/Help/en/topics/123015/GUID-6039B8C0-2601-42E5-8DEF-C03FA6E1FFE1.html
- Purpose: migrate legacy (V8i) WorkSpaces to MicroStation 2025 configuration.
- Operations:
  - Identify installed MicroStation and MicroStation-based products and their default WorkSpaces.
  - Convert .ucf to WorkSpace .cfg and .pcf to WorkSet .cfg.
  - Move Site level configuration to Organization level (..\Configuration\Organization).
  - Optionally copy project data files.
- Access:
  - Work page -> WorkSpace dropdown -> Configuration Assistant
  - File > Settings > Configuration > Configuration Assistant
- Dialog flow (Next button steps):
  - Select V8i WorkSpace (choose workspace + conversion type: configuration+data files or config only)
  - Select V8i Users (all or selected projects)
  - Select Destination (default C:\Temp\Configuration; creates subfolders)
  - Migration Process (shows results)

## Batch Migration from V7 to V8 Format
https://docs.bentley.com/LiveContent/web/MicroStation-v2025.0.1/Help/en/topics/123015/GUID-6B3E5DE0-6B85-E702-9C59-477699D2D6DE.html
- Use Batch Converter to upgrade V7 design files and/or cell libraries to V8 DGN.
- Working units can be explicitly set via a unit definition text file.
- units.def example file lives under ..\Default\Data\ in the program directory.
- MS_CUSTOMUNITDEF config variable defines the unit definition file location.
- If a custom unit conflicts with standard units, the custom unit wins.

## Directory Structure
https://docs.bentley.com/LiveContent/web/MicroStation-v2025.0.1/Help/en/topics/123015/GUID-3D56382F-4235-A3DC-CFDA-628FBF1F7D85.html
- Default install root: C:\Program Files\Bentley.
- User data location: %LOCALAPPDATA%\Bentley\<product_name>\<product_version>\ defined by _USTN_HOMEROOT.
  - Recommended: keep user files local (avoid network shares for user data).
- Documentation default: C:\ProgramData\Bentley\Documentation\MicroStation\<product_version>\<language>\htm
- Program directory contains system files (Assemblies, config, Default, Docs, ECSchemas, en, GeoCoordinateData, Mdlapps, Mdlsys, Ribbon, etc.).
- Configuration directory (WorkSpaces/WorkSets): C:\ProgramData\Bentley\<product_name> <product_version>.

## Quick implications for INWC_RH workspace/workset model
- INWC_RH aligns with Org -> WorkSpace -> WorkSet layering, where Organization config holds global standards, WorkSpace defines workspace-level standards, WorkSet defines project-specific overrides.
- Migration tools (Configuration Assistant / DWG Assistant) provide a UI-based path for converting older configs; useful if legacy V8i or DWG structures are still present.
- The default ProgramData Configuration directory corresponds to the path INWC currently mirrors in repo under Configuration/.
