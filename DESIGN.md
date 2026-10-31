# Existing interface contract

Owner instruction, 2026-10-31: **all new buttons, windows and other interface elements must use the existing product's style and design**. This records the current design language; it does not authorize a redesign.

## Source of truth

- [Shared styles](NetworkProfileManager/Themes/Styles.xaml), WPF-UI 4.2.0 theme/control dictionaries in [App.xaml](NetworkProfileManager/App.xaml).
- [Main window](NetworkProfileManager/MainWindow.xaml): Mica navigation, content surface, title-bar tools.
- [Profile editor](NetworkProfileManager/Views/AddProfileDialog.xaml): labelled inputs, scrollable body, fixed footer actions.
- [Settings](NetworkProfileManager/Views/ThemeSettingsWindow.xaml) and [scanner](NetworkProfileManager/Views/IpScannerWindow.xaml): secondary windows, cards, controls and command hierarchy.
- [Adapter list](NetworkProfileManager/Views/AdapterListPanel.xaml) and [profile cards](NetworkProfileManager/Views/ProfilePanel.xaml): navigation selection, spacing and action patterns.

## Reuse rules

| Element | Existing contract |
|---|---|
| Windows | `ui:FluentWindow`, `WindowBackdropType="Mica"`, extended title bar and `ui:TitleBar`. Secondary windows center on their owner; choose resize behavior to fit the existing task pattern. Keep content scrollable and essential footer actions reachable at minimum size. |
| Buttons | Use `ui:Button`; title-bar/toolbar/low-emphasis actions reuse `SubtleButton`. Text toolbar padding follows the existing `10,6` pattern; compact icon tools follow existing `4`/`6` padding. Secondary actions use the default WPF-UI button. Use `Appearance="Primary"` only for the main action in a group, without custom accent fills. Dialog/footer actions follow existing `MinWidth="96"` and 8-unit gaps. Preserve built-in hover, pressed, disabled and focus states. |
| Icons | Reuse WPF-UI `SymbolIcon` (normally 16/20) as the current code does. No separate icon pack, copied vendor assets or custom decorative symbols. Icon-only actions need localized tooltip and accessible name. |
| Text | Reuse `PageTitleText`, `SectionHeaderText`, `CaptionText`, `FieldLabel`; existing ramp is 20/14/12. Labels precede fields. Use regular product wording, no implementation details in the normal flow. |
| Inputs | Existing `ui:TextBox` plus shared validation template; standard themed ComboBox/CheckBox where already used. Provide `AutomationProperties.Name`. Distinguish empty, invalid and disabled states with text and control state. |
| Lists | Navigation lists reuse `NavigationListItem`, extracted without changing the existing adapter template: subtle selection fill, accent pill and existing keyboard focus visual. Keep long names trimmed with a tooltip and prevent horizontal overflow. Use existing card/table patterns for other list roles. |
| Surfaces | Reuse `Card` and `FooterBar`. Use WPF-UI semantic brushes (`TextFillColor*`, `Card*`, `LayerFill*`, `DividerStroke*`, `AccentFill*`, `SystemFill*`) and `ControlCornerRadius`; no feature-specific hardcoded colors, shadows, gradients or corner shapes. |
| Layout | Preserve current overall layout. Typical secondary-window outer inset is 20, field gaps 12, group gaps 16, command gaps 8. Derive any localized layout dimensions from existing task surfaces; do not introduce a parallel token system. |
| Themes/localization | Respect System/Light/Dark and high-contrast intent through existing resources. All visible copy lives in matching En/Tr dictionaries with DynamicResource bindings where language can change. No new motion scheme. |
| Messages | Keep errors readable and localized, wrap long text, retain the user's draft on failure. Confirm destructive/overwrite actions deliberately. Never expose credentials or secret-bearing URL text in errors. |

## Project window application

Project is a localized addition: existing title-bar tool style, a separate FluentWindow, existing navigation-list selection, the existing profile-form idiom and fixed `FooterBar`. New/Open/Choose Folder are subtle commands; Save As is the main file action; profile creation and retention use standard buttons. No new primitive or visual identity is introduced. Legacy adapter/profile UI keeps its established behavior.

## Verification for UI changes

Build and resource-key checks; exercise new data bindings, disabled/selected/error/empty states and keyboard reachability. Render at normal and minimum supported dimensions, in both themes and En/Tr. The isolated `scripts/ProjectUiSmoke` harness renders the Project component without launching the application's startup/network services. Its PNGs composite native Mica transparency over the existing application surface; they do not certify the actual desktop backdrop or assistive-technology behavior. Record checks actually run in [test evidence](docs/testing/TEST_MATRIX.md); never claim complete visual/accessibility certification from source inspection.
