# MicroStation 2025.0.1 configuration variables and tools

Source pages:
- Configuration Variables: https://docs.bentley.com/LiveContent/web/MicroStation-v2025.0.1/Help/en/topics/123015/GUID-EABCA351-6E56-D0B2-A2F2-50169763EF54.html
- Working with Configuration Variables: https://docs.bentley.com/LiveContent/web/MicroStation-v2025.0.1/Help/en/topics/123015/GUID-62FEC6B9-BEA9-328D-67F4-ABA8FB3DBA84.html
- Find active WorkSpace/WorkSet location: https://docs.bentley.com/LiveContent/web/MicroStation-v2025.0.1/Help/en/topics/123016/GUID-7742579E-58AC-EE1A-5181-BDF7D54D1AE5.html
- About Configuration window: https://docs.bentley.com/LiveContent/web/MicroStation-v2025.0.1/Help/en/topics/123017/GUID-7A19E8ED-844B-9665-4B51-D826D70AB9C3.html
- Manage Configuration dialog: https://docs.bentley.com/LiveContent/web/MicroStation-v2025.0.1/Help/en/topics/123017/GUID-F2FE5CDB-5150-4FF3-A621-16CB1AE9DEB5.html
- Typical Configuration Scenario: https://docs.bentley.com/LiveContent/web/MicroStation-v2025.0.1/Help/en/topics/123015/GUID-DD8818B9-CA8C-402C-B5A1-F2663DD26625.html

## Configuration Variables (types and purpose)
- Two main families:
  - Framework variables (prefix _USTN_) for configuration file structure and core paths.
  - Operational variables (prefix MS_) for runtime behavior and resource search paths.
- Framework variables often default based on install directory but can be overridden in user configuration files.
- Variables are defined in configuration files and processed by configuration levels (System/App/Org/WorkSpace/WorkSet/Role/User).

## Working with Configuration Variables
- User-level variables can be set via the Configuration Variables dialog.
- Org/WorkSpace/WorkSet level variables must be edited in text configuration files (use configuration file syntax docs).
- Linked tasks include setting path/directory/filename/keyword variables and editing variables.

## Find active WorkSpace and WorkSet locations
- Use: File > Settings > Configuration > About Configuration.
- The About Configuration window lists active WorkSpace and WorkSet and the path to each component.
- If a path is truncated, hover to see full path via tooltip.

## About Configuration window
- Purpose: shows active configuration components and active workmode.
- Access: File > Settings > Configuration > About Configuration.
- Key-in: DIALOG ABOUTCONFIGURATION.
- Fields listed include:
  - WORKSPACE: active WorkSpace name and configuration file path.
  - WORKSET: active WorkSet name and configuration file path.
  - PREFERENCES: user preferences file name and path.
  - CONNECTED PROJECT: ProjectWise project tied to the WorkSet (if any).
  - ABOUT WORKMODE: active workmode name/description.

## Manage Configuration dialog
- Used to create new configurations or edit existing configuration settings.
- Access from WorkPage (icons/commands shown in the doc).
- Key actions listed:
  - (Technology Preview) Create a new configuration folder (opens New Configuration dialog).
  - Connect to a configuration folder.
  - Edit configuration (settings of selected configuration).
  - Delete configuration.
  - Move to top/up/down/bottom in the list.
- Configuration list shows name, description, type, folder, and visibility.

## Typical Configuration Scenario (example)
- Example organization uses shared servers for standards and client-specific data.
- Admin points MicroStation custom configuration to a shared configuration root (defines _USTN_CONFIGURATION).
- Admin edits WorkSpaceSetup.cfg to set:
  - _USTN_WORKSPACELABEL (e.g., Client)
  - _USTN_ORGANIZATION to org standards share
  - _USTN_WORKSPACESROOT to WorkSpace share
- Admin copies delivered NoWorkSpace/Template directories to new _USTN_WORKSPACESROOT.
- Org standards: copy delivered Standards.cfg into org standards path; because variables are relative to _USTN_ORGANIZATION, no edits needed.
- WorkSpace for a client can be created by copying Example.cfg and redirecting _USTN_WORKSPACEROOT to client share.
- Client-specific standards can be appended to MS_DGNLIBLIST, MS_GUIDGNLIBLIST, MS_CELL with ">" (append) syntax.
- A WorkSet is created under _USTN_WORKSETSROOT, with standard subfolders for standards and design files.

## Relevance to INWC_RH
- INWC_RH config aligns with _USTN_ and MS_ variable patterns, WorkSpace/WorkSet selection, and Configuration dialog for verification.
- The About Configuration window is the fastest way for users to verify the active WorkSpace/WorkSet and associated config files match repo expectations.
